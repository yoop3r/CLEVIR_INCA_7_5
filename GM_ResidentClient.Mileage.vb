Option Strict Off
Option Explicit On

Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Speech.Synthesis
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml
Imports SharpPcap

Partial Public Class GmResidentClient

    Public Sub WriteMileageToAnnoFile(ByVal MySender As String)
        ' Called from MyBackGroundTasks - main execution loop also called from StartStopRecord (when recording is stopped)...
        ' Writes the vehicle mileage for the recording session to the annotation file.
        If ANNOFileName Is Nothing Then Exit Sub
        HandleUserMessageLogging("GMRC", "WriteMileageToANNOFile called by " & MySender)
        Dim snapshotTime As Date = DateTime.Now
        ' Failsafe - The Annotation file should always have been created at this point, so this should never happen...
        If Not File.Exists(ANNOFileName) Then
            CreateANNOFile()
        End If
        Dim textline() As String = ReadAnnoFile()
        UpdateTextLines(textline, snapshotTime)
        WriteAnnoFile(textline)
    End Sub

    Private Function ReadAnnoFile() As String()
        Dim textline As New List(Of String)
        ' Read all lines from the file
        Using reader As New StreamReader(ANNOFileName)
            While Not reader.EndOfStream
                textline.Add(reader.ReadLine())
            End While
        End Using
        Return textline.ToArray()
    End Function

    Private Sub UpdateTextLines(ByRef textline() As String, ByVal snapshotTime As Date)
        AnnoHeaderUpdater.UpdateSessionEndFields(textline, snapshotTime, recordedMileage, _lccActiveMileage)
    End Sub

    Private Shared Sub WriteAnnoFile(ByVal textline() As String)
        ' Write all lines to the file
        Using writer As New StreamWriter(ANNOFileName, False) ' False to overwrite the file
            For Each line As String In textline
                writer.WriteLine(line)
            Next
        End Using
    End Sub

    Private Function WhereTheHeckAmIAt(ByVal latPos As Double, ByVal lonPos As Double) As String

        'Called from MyBackGroundTasks:  Determines if we are on or off property based on GPS coordinates...

        Static saveWhereTheHeckAmIAt As String
        Dim whereAmI As String

        If latPos <> 0 And lonPos <> 0 Then

            whereAmI = PropertyGeofence.Classify(latPos, lonPos, saveWhereTheHeckAmIAt)

            saveWhereTheHeckAmIAt = whereAmI
            OnVehicleScreen.Label1.BackColor = Color.Green

        Else
            If Len(saveWhereTheHeckAmIAt) > 0 Then
                whereAmI = saveWhereTheHeckAmIAt
                OnVehicleScreen.Label1.BackColor = Color.Yellow
            Else
                whereAmI = "Unknown"
                OnVehicleScreen.Label1.BackColor = Color.Red
            End If

        End If

        OnVehicleScreen.Label1.Text = whereAmI
        WhereTheHeckAmIAt = whereAmI

    End Function

    Private Sub HandleOdometerRelatedStatus(ByVal MyDg As GridDataClass, ByRef saveOdoReading As Double, ByRef saveWhereAmIAt As String, ByVal lRecordingState As Boolean)
        ' Handles odometer-related status updates, including per-recording session mileage accumulation.
        ' Mileage is accumulated correctly using increments and reset per recording session.
        ' On recording stop, the session mileage is logged (and can be saved to annotation file if needed).
        ' Note: Only called when MyDg.CS_ODOMETER > 0, so no need to check again

        Static saveLccActive As Integer
        Static saveRecordingState As Boolean = False
        Static recordingStateChangeCount As Integer = 0
        Static recordingStateStable As Boolean = True
        Const StableThreshold As Integer = 3  ' Require 3 consecutive consistent readings

        ' All mileage (session, LCC, ANNO header and annotation rows) is in km - the odometer's native unit.

        ' The caller's saveOdoReading does not persist between loop iterations, so seed it
        ' with the last valid reading (keeps LCC segment math off a 0 odometer).
        saveOdoReading = _lastOdoReadingKm

        ' ====================================================================================
        ' 1. Read Odometer Value (before session transitions so they see the current reading)
        ' ====================================================================================
        Dim haveOdoReading As Boolean = False
        If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ODOMETER, 1)).SignalData > 0 Then
            saveOdoReading = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ODOMETER, 1)).SignalData
            CurrentMileage = saveOdoReading
            _lastOdoReadingKm = saveOdoReading
            haveOdoReading = True

            ' Initialize starting mileage if not set
            If StartingMileage = 0 Then
                HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: Resetting Mileage - Starting Mileage = " & saveOdoReading)
                StartingMileage = saveOdoReading
            End If
        End If

        ' ====================================================================================
        ' ISSUE 1 FIX: Implement state debouncing to prevent rapid "Now Recording/Not Recording" cycling
        ' Session boundaries are normally set by StartRecordingAsync / FinalizeSessionFiles; this
        ' observer only opens/closes the session when recording changed outside those paths.
        ' ====================================================================================
        If lRecordingState <> saveRecordingState Then
            ' State differs from saved state - increment debounce counter
            recordingStateChangeCount += 1
            recordingStateStable = False

            ' Only accept the state change if it's been stable for StableThreshold cycles
            If recordingStateChangeCount >= StableThreshold Then
                ' State has been consistent for multiple iterations, accept the transition
                Dim stateDescription As String = If(lRecordingState, "Recording", "Not Recording")
                HandleUserMessageLogging("GMRC", $"HandleOdometerRelatedStatus: Recording State Changed - Now {stateDescription}")

                ' Now perform the actual state transition logic
                If lRecordingState Then
                    ' *** RECORDING STARTED ***
                    If Not _mileageSessionActive Then BeginSessionMileage("HandleOdometerRelatedStatus")
                Else
                    ' *** RECORDING STOPPED ***
                    If _mileageSessionActive Then EndSessionMileage("HandleOdometerRelatedStatus")
                End If

                ' Commit the state change
                saveRecordingState = lRecordingState
                recordingStateChangeCount = 0
                recordingStateStable = True
            End If
        Else
            ' State is consistent with saved state - reset debounce counter
            recordingStateChangeCount = 0
            If Not recordingStateStable Then
                recordingStateStable = True
            End If
        End If

        ' Accumulate mileage during an open recording session
        If haveOdoReading AndAlso _mileageSessionActive Then
            AccumulateSessionMileage(saveOdoReading)
        End If

        ' ====================================================================================
        ' 2. Update LCC (Lane Centering Control) Active Mileage if applicable
        ' ====================================================================================
        If MyDg.CS_LCC_CONTROL_ACTIVE > 0 Then
            _lccActive = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LCC_CONTROL_ACTIVE, 1)).SignalData)

            If _lccActive <> saveLccActive Then
                If _lccActive = 1 Then
                    _lccActiveStartingMileage = saveOdoReading
                    HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: LCC Active - Starting Mileage = " & saveOdoReading)
                ElseIf _lccActiveStartingMileage > 0 Then
                    ' Start of 0 = no open segment (never had a valid odometer, or already counted)
                    Dim segmentMileage As Double = (saveOdoReading - _lccActiveStartingMileage)
                    If segmentMileage >= 0 Then  ' Sanity check
                        _lccActiveMileage += segmentMileage
                        HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: LCC InActive - Mileage Travelled since Active Transition = " & segmentMileage)
                        HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: LCC InActive - Total Accumulated Mileage this session = " & _lccActiveMileage)
                    Else
                        HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: WARNING - Negative LCC mileage detected. Skipping accumulation.")
                    End If
                    _lccActiveStartingMileage = 0
                End If
                saveLccActive = _lccActive
            End If
        End If

        ' ====================================================================================
        ' 3. Update GPS Coordinates (if available)
        ' ====================================================================================
        If MyDg.CS_GPS_LAT > 0 And MyDg.CS_GPS_LON > 0 Then
            CurrentLatitude = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_GPS_LAT, 1)).SignalData
            If CurrentLatitude <> 0 Then
                CurrentLatitude /= ConvertToLatLon
            End If

            CurrentLongitude = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_GPS_LON, 1)).SignalData
            If CurrentLongitude <> 0 Then
                CurrentLongitude /= ConvertToLatLon
            End If

            ' Optional: Still determine location for UI display (but don't use it for mileage tracking)
            _whereAmIAt = WhereTheHeckAmIAt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_GPS_LAT, 1)).SignalData,
                                     MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_GPS_LON, 1)).SignalData)
        End If

        ' ====================================================================================
        ' 4. Update Lane Class (if applicable)
        ' ====================================================================================
        If MyDg.CS_LaneClass_Crnt > 0 Then
            LaneClassCurrent = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneClass_Crnt, 1)).SignalData
        End If

    End Sub

    ''' <summary>
    ''' Opens a per-recording mileage session. Called when recording starts (StartRecordingAsync),
    ''' or by HandleOdometerRelatedStatus when it observes a recording start no other path reported.
    ''' </summary>
    Friend Sub BeginSessionMileage(ByVal caller As String)
        recordedMileage = 0
        _lccActiveMileage = 0
        ' Seeded from the first valid odometer reading inside the session, so a stale or zero
        ' reading from before recording can never be counted as session distance.
        _sessionPreviousOdo = 0
        If _lccActive = 1 Then
            _lccActiveStartingMileage = _lastOdoReadingKm
        End If
        _mileageSessionActive = True
        HandleUserMessageLogging("GMRC", $"BeginSessionMileage ({caller}): Recording started - session mileage reset")
    End Sub

    ''' <summary>
    ''' Closes the per-recording mileage session: adds the final odometer delta and any open LCC-active
    ''' segment. Called before the mileage is written to the ANNO file (FinalizeSessionFiles), or by
    ''' HandleOdometerRelatedStatus when it observes a recording stop no other path reported.
    ''' </summary>
    Friend Sub EndSessionMileage(ByVal caller As String)
        If Not _mileageSessionActive Then Exit Sub

        AccumulateSessionMileage(_lastOdoReadingKm)

        If _lccActive = 1 AndAlso _lccActiveStartingMileage > 0 AndAlso _lastOdoReadingKm > 0 Then
            Dim segmentMileage As Double = _lastOdoReadingKm - _lccActiveStartingMileage
            If segmentMileage >= 0 Then
                _lccActiveMileage += segmentMileage
            End If
            ' Re-base so the eventual LCC-inactive transition doesn't count this segment again
            _lccActiveStartingMileage = _lastOdoReadingKm
        End If

        _mileageSessionActive = False
        HandleUserMessageLogging("GMRC", $"EndSessionMileage ({caller}): Recording stopped - Session mileage = {Format(recordedMileage, "0.0")} km, LCC active mileage = {Format(_lccActiveMileage, "0.0")} km")
    End Sub

    Private Sub AccumulateSessionMileage(ByVal odoReadingKm As Double)
        If odoReadingKm <= 0 Then Exit Sub

        If _sessionPreviousOdo <= 0 Then
            _sessionPreviousOdo = odoReadingKm
            HandleUserMessageLogging("GMRC", "AccumulateSessionMileage: Session start odometer = " & Format(odoReadingKm, "0.0") & " km")
            Exit Sub
        End If

        Dim delta As Double = odoReadingKm - _sessionPreviousOdo
        If delta >= 0 Then
            recordedMileage += delta
        Else
            ' Odometer went backwards (e.g. replay restarted) - re-base without counting
            HandleUserMessageLogging("GMRC", "AccumulateSessionMileage: WARNING - Odometer decreased by " & Format(-delta, "0.0") & " km - re-basing")
        End If
        _sessionPreviousOdo = odoReadingKm
    End Sub

End Class
