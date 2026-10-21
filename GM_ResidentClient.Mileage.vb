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
Imports System.Windows.Forms.DataVisualization.Charting
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
        For ctr As Integer = 0 To UBound(textline)
            Select Case ctr
                Case 9
                    textline(ctr) = "1,7,End Date," & Format(snapshotTime, "MM/dd/yyyy")
                Case 10
                    textline(ctr) = "1,8,End Time," & Format(snapshotTime, "HH:mm:ss")
                Case 15
                    textline(ctr) = "1,15,RecordedMileage," & Format(recordedMileage, "0.0")
                Case 16
                    textline(ctr) = "1,20,LCCActiveMileage," & Format(_lccActiveMileage, "0.0")
            End Select
        Next
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

        ' Per-recording session mileage tracking
        Static recordingStartOdo As Double = 0
        Static previousOdo As Double = 0

        ' ====================================================================================
        ' ISSUE 1 FIX: Implement state debouncing to prevent rapid "Now Recording/Not Recording" cycling
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
                    _lccActiveMileage = 0
                    recordedMileage = 0
                    recordingStartOdo = saveOdoReading
                    previousOdo = saveOdoReading
                    HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: Recording started - Session start mileage = " & recordingStartOdo)
                Else
                    ' *** RECORDING STOPPED ***
                    recordedMileage += (saveOdoReading - previousOdo)
                    HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: Recording stopped - Session mileage = " & Format(recordedMileage, "0.0"))
                    ' Optionally, call WriteMileageToAnnoFile or save to file here if needed
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

        ' ====================================================================================
        ' 1. Read Odometer Value
        ' ====================================================================================
        If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ODOMETER, 1)).SignalData > 0 Then
            saveOdoReading = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ODOMETER, 1)).SignalData * KilometersToMiles
            CurrentMileage = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ODOMETER, 1)).SignalData

            ' Accumulate mileage during ongoing recording (only after state is stable)
            If lRecordingState AndAlso saveRecordingState Then
                recordedMileage += (saveOdoReading - previousOdo)
                previousOdo = saveOdoReading
            End If

            ' Initialize starting mileage if not set
            If StartingMileage = 0 Then
                HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: Resetting Mileage - Starting Mileage = " & saveOdoReading)
                StartingMileage = saveOdoReading
            End If
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
                Else
                    Dim segmentMileage As Double = (saveOdoReading - _lccActiveStartingMileage)
                    If segmentMileage >= 0 Then  ' Sanity check
                        _lccActiveMileage += segmentMileage
                        HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: LCC InActive - Mileage Travelled since Active Transition = " & segmentMileage)
                        HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: LCC InActive - Total Accumulated Mileage this session = " & _lccActiveMileage)
                    Else
                        HandleUserMessageLogging("GMRC", "HandleOdometerRelatedStatus: WARNING - Negative LCC mileage detected. Skipping accumulation.")
                    End If
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

End Class
