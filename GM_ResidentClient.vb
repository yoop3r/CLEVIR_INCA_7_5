
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

Public Class GmResidentClient

    ' Declare the flag at the class level
    Public _isMonitorTaskRunning As Boolean = False
    'Private syncScript As SyncScript
    ' Method to check if a process is running
    ' ✅ ADD THIS: OXTS Interface instance
    Public MyOxtsInterface As OxtsNcomInterface
    Public MyTimeSyncProvider As ITimeSyncProvider

    Private _fullyInitialized As Boolean = False

    'This was originally the main display form for the application and hence, much of the code
    'resides in here.  However, another form was added midway through the development of the app
    'called OnVehicleScreen, which is the primary display when running in the vehicle.  The 
    'GmResidentClient form became the configuration environment form which is more like a traditional
    'windows form with a dropdown menu.  The configuration environment can now be accessed from the
    'OnVehicleScreen so that certain aspects of the interface can be configured by the user.  The 
    'idea here is that in most cases, the user will be accessing this form from their desktop to create
    'custom configurations, and will not be accessing this form much when running on a actual vehicle
    'where they will be using the OnVehicleScreen display screen instead.

    'However, the GmResidentClient form still contains a significant portion of the code for this application
    'including for instance, the main background tasks loop (MyBackgroundTasks), InitializationMonitor, Initialize routine
    'and ProcessKiller thread...

    'While most of the code is shared between the CLEVIR_INCA_7_2 and CLEVIR_INCA_7_3 versions, there are different
    'GmResidentClient modules used for the two CLEVIR versions.  This is due to the fact that the handling of configured
    'displays must be different between 7.2 and 7.3, and much of the code related to the configured displays is in this
    'module.  So, whenever any changes are made to GmResidentClient, the same changes must be made to both the 7.2 version
    'which is located in FlexGridFiles and the 7.3 version which is located in in NoFlexGridFiles...

    ' ═══════════════════════════════════════════════════════════════
    ' ASYNC/AWAIT PATTERN USAGE:
    ' ═══════════════════════════════════════════════════════════════
    ' - Background tasks (MyBackgroundTasks): Async/Await
    ' - Critical shutdown paths: .GetAwaiter().GetResult() (synchronous)
    ' - User button clicks: Fire-and-forget Async (PerformReinitialization)
    ' - INCA API calls: Await where possible, block where safety required
    ' ═══════════════════════════════════════════════════════════════

    '***************************** Public / Private Variable Declarations ***************************

    ' OXTS GPS/INS Configuration
    Public OxtsEnabled As Boolean = False
    ' Default fallback value - overridden by ReadConfiguration()
    Public OxtsNcomIpAddress As String = "10.5.55.200"
    ' Default fallback value - overridden by ReadConfiguration()
    Public OxtsNcomPort As Integer = 3000
    Public OxtsGpsLockTimeout As Integer = 30000 ' milliseconds
    Public OxtsWaitForLockOnStart As Boolean = True

    ' Time sync source selection
    Public TimeSyncEnabled As Boolean = False
    Public TimeSyncProviderType As String = "OXTS" ' OXTS | TIMEMACHINE

    ' TimeMachine provider configuration
    Public TimeMachineEnabled As Boolean = False
    Public TimeMachineIpAddress As String = "255.255.255.255"
    Public TimeMachinePort As Integer = 7372
    Public TimeMachinePollMs As Integer = 1000
    Public TimeMachinePtpAssumeLocked As Boolean = True

    Enum GridUpdateActions
        ToHigh
        FromHigh
        ToLow
        FromLow
    End Enum

    Structure InvisibleSignals
        Dim DeviceName As String
        Dim RasterName As String
        Dim SignalName As String
        Dim Status As String
        Dim Value As Double
    End Structure

    Private MyInvisibleSignals() As InvisibleSignals

    Structure ButtonContainer
        Dim Parent As TabControl
        Dim ContainerName As String
        Dim Buttons() As Button
        Dim ButtonContainerHotKey() As Label
        Dim Top As Integer

    End Structure


    Private _playbackDataArray(,) As String
    Private _playbackColumnIndexMap As Dictionary(Of String, Integer) = Nothing
    Private _playbackRowCounter As Integer
    Private ReadOnly _playbackLock As New Object()

    Private _hasInitialized As Boolean = False

    Private Const ConvertToLatLon As Double = 3600000

    Private Const KilometersToMiles As Double = 0.62137119
    Private Const NorthwestCornerLat As Double = 153378676.8
    Private Const NorthwestCornerLon As Double = -301326109.2
    Private Const SouthwestCornerLat As Double = 153217998.0
    Private Const SouthwestCornerLon As Double = -301313210.4
    Private Const SoutheastCornerLat As Double = 153232621.2
    Private Const SoutheastCornerLon As Double = -301173238.8
    Private Const NortheastCornerLat As Double = 153357580.8
    Private Const NortheastCornerLon As Double = -301181194.8
    Private Const NorthMidpointLat As Double = 153381236.4
    Private Const NorthMidpointLon As Double = -301250563.2

    Private Const DefaultDataCollectionRateMsec As Integer = 50
    Private Const DefaultDisplayRefreshRateMsec As Integer = 50
    Private Const DefaultFormWidth As Integer = 800
    Private Const DefaultFormHeight As Integer = 600
    'Private Const DEFAULT_FONT_SIZE As Integer = 11
    Private Const DefaultFontSize As Integer = 10

    Private Const MainTabTop As Integer = 195
    Private Const MainTabLeft As Integer = 5

    Private Const StatusLabelHeight As Integer = 60
    Private Const StatusLabelSpacing As Integer = 2

    Private ReadOnly _mainTabWidth As Integer = DefaultFormWidth - 10
    Private ReadOnly _mainTabHeight As Integer = DefaultFormHeight - 230
    Private _statusLabelWidth As Integer = 54

    Public Const MenuFontSize As Integer = 14
    Public Const DefaultFormWidth800 As Integer = 800

    Public Const NumPredefinedDisplays As Integer = 9

    Public RecordTransitionDelay As Integer
    Public UserConfigFileName As String
    Public DataCollectionRate As Integer
    Private CheckForExperiment As Boolean
    Public VariableNameDataArray(,) As String
    Public SaveLineNumber As Integer
    Public MyLogin As ToolStripMenuItem
    ' Define MyDFs as a List(Of FormDataClass)
    Public ReadOnly MyDFs As List(Of FormDataClass) = New List(Of FormDataClass)(FormDataClass.MAX_NUM_FORMS)

    Public MyTdGraphicsContainer As TDGraphicsContainerClass
    Public MySignalDataWithTime() As IGM_INCA_Comm.TransferDataWithTime
    Public FormForGridAdd As String

    Public MyMainTabControl As TabControl
    Public MySubTabControl As TabControl
    Public MyLabel() As Label
    Public NumSignalsAdded As Integer
    Public MyTestThread As Thread
    Public StopTestProcess As Boolean
    'Public CancelCameraSearch As Boolean

    'Mileage calculation variables...
    Public StartingMileage As Double

    Public CameraIpAddresses As String() = {"192.168.40.101", "192.168.40.102", "192.168.40.103", "192.168.40.104", "192.168.40.105", "192.168.40.106", "192.168.40.107", "192.168.40.108", "192.168.40.109"}
    Public ButtonContainers() As ButtonContainer
    Public ReadOnly MyExitButtons As New List(Of Button)()
    'Public MyExitButtons() As Button

    Public MyToolStripMenuItem As ToolStripMenuItem
    Public ProgressBarEnable As Boolean

    Public ReadOnly AvailableObjectIDs() As String = {"FUSION", "LRR", "LFSRR", "RFSRR", "VIS"}
    'Private PerformanceData(,) As String
    Private _chartXIncrement As Double ' HandleLmfrStatusScreenGlobalA

    ' LKA Custom Display
    Private _saveLkaDistRtLnEdge As Double
    Private _saveLkaDistLtLnEdge As Double

    ' Target Status Display
    Private _saveFoaiVairValue As Integer
    Private _saveFoaiAwirValue As Integer
    Private _saveAutobrkreqValue As Integer
    Private _saveColprsysbrkprfreq_Target As Integer

    ' Pedestrian Status Display
    Private _savePedwarn As Integer
    Private _saveBrakingFlag As Integer
    Private _saveAlertFlag As Integer
    Private _saveNotificationFlag As Integer
    Private _saveColprsysbrkprfreq_Ped As Integer
    Private _MyMiscInfo As ToolStripMenuItem
    Private _MyRecordPlayback As ToolStripMenuItem
    Private _lccActiveStartingMileage As Double
    Private _lccActiveMileageTotal As Double  ' Total LCC Active
    Private _lccActiveMileage As Double        ' Per-session LCC Acitve (current)

    ' GO/NOGO debug tracing (temporary)
    Private Const GO_NOGO_DEBUG_VERBOSE As Boolean = False

    Private _onPropertyMileageRecording As Double
    Private _onPropertyMileageNotRecording As Double
    Private recordedMileage As Double
    Private _offPropertyMileageNotRecording As Double
    Private _unknownMileageNotRecording As Double
    Private _unknownMileageRecording As Double
    Private _onPropertyStartingOdoRecording As Double
    Private _onPropertyStartingOdoNotRecording As Double
    Private _offPropertyStartingOdoRecording As Double
    Private _offPropertyStartingOdoNotRecording As Double
    Private _unknownStartingOdoNotRecording As Double
    Private _unknownStartingOdoRecording As Double
    Private _onPropertyRecording As Boolean
    Private _onPropertyNotRecording As Boolean
    Private _offPropertyRecording As Boolean
    Private _offPropertyNotRecording As Boolean
    Private _unknownRecording As Boolean
    Private _unknownNotRecording As Boolean
    Private _healthCounter As Integer
    Private _healthMonitor As Integer
    Private _incaInitStarted As Boolean
    Private _killInca As Boolean
    Private _killProcesses As Boolean
    Private _enableMyBackgroundTasks As Boolean
    Private _initializing As Boolean
    Private _MyCreateNewDisplayMenuItem As ToolStripMenuItem
    Private _topDownSignalsRegistered As Boolean
    Private _totalNumActiveDevices As Integer
    Private _displayRefreshRate As Integer

    'Private LoginButton() As Button 5.6.2
    Private _switchToMain As Boolean
    Private _keepListBoxDisplayed As Boolean
    Private _continueExecution As Boolean
    Private _numInvalid As Integer
    Private _actualCameraIpAddresses() As String
    Private _inMeasureMode As Boolean
    Private _loadedIncaConfiguration As IncaConfiguration
    Private _MyCpuStaleData As Boolean
    Private _MyDeviceStatus As Boolean
    Private _lostDevice As Boolean
    Private _registerIntoNewBlankExp As Boolean
    Private _gonogocount As Integer
    Private _incaLaunched As Boolean
    Private _whereAmIAt As String 'Undefined, Unknown, On Property, Off Property
    Private _MyMenuStrip As MenuStrip
    'Private EnablePerformanceMonitoring As Boolean
    Private _backgroundTasks As BackgroundTasks
    Private _MyKillerThread As Thread
    Private _progressBarMax As Integer
    Private _lccActive As Integer
    Private _effectiveCameraCount As Integer = 0
    Public WhichTabAmIOn As Integer
    Public HotKeySelected As String
    ' Grow the recording buffer in chunks to avoid O(n^2) ReDim Preserve
    Private Const RecordingCapacityStep As Integer = 1024

    Public MuteVoiceRecordingMessages As Boolean
    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property SaveSwitchToName As String
    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property TotalNumSignalsDisplayed As Integer
    Private Shared lIsTargetOnWorkingPage As String = "False"
    Private Shared saveIsTargetOnWorkingPage As String

    ' Tracks the last duration value sent to the INCA comm layer to avoid redundant calls.
    Private _lastSetRecordFileDurationMinutes As Integer = -1

    'Manage the cancellation of our monitoring task
    Public EtasUserPath As String
    Public EtasDefaultUserName As String
    Private Delegate Sub BackgroundTasks()
    Private _recordingMonitorCts As CancellationTokenSource
    Private _backgroundTasksCts As CancellationTokenSource  ' Cancellation token for MyBackgroundTasks loop
    Private Shared saveStatus() As Boolean
    Private Shared videoMessageLastDisplayed As Boolean = True
    Private Shared dataMessageLastDisplayed As Boolean = True

    Private Shared _initToastId As Guid = Guid.Empty
    Private ReadOnly _initToastIds As New List(Of Guid)()
    'Private ReadOnly _gridRefreshAt As New Dictionary(Of String, DateTime)()
    Private ReadOnly _gridRefreshAt As New ConcurrentDictionary(Of String, DateTime)()
    Private ReadOnly GridMinRefresh As TimeSpan = TimeSpan.FromMilliseconds(200)
    ' Tracks grids whose DataArray is smaller than NumberOfRows/NumberOfColumns so the
    ' size-drift diagnostic is logged only once per grid instead of every ProcessGrids cycle.
    Private ReadOnly _dataArraySizeWarned As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    Private _initCts As Threading.CancellationTokenSource = Nothing

    Private _lastMeasurementCheckTime As DateTime = DateTime.MinValue
    Private Const MeasurementCheckIntervalSeconds As Integer = 2

    Private saveDtcStrings As ArrayList = Nothing
    Private loggedDtcStrings As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)  ' Track what we've logged
    Private exitInProgress As Boolean = False

    Private _suppressInitMonitorUI As Boolean = False
    Private _nanAlertInProgress As Boolean = False  ' Guard flag to prevent overlapping NAN alerts


    Private ReadOnly _recordingBufferLock As New Object()

    ''' <summary>
    ''' Ensures the recording buffer can accommodate the specified index.
    ''' Uses exponential growth (2x) to minimize reallocation overhead.
    ''' Thread-safe for concurrent recording operations.
    ''' </summary>
    Private Sub EnsureRecordingBufferCapacity(requiredIndex As Integer)
        ' Validate input
        If requiredIndex < 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(requiredIndex), "Index cannot be negative")
        End If

        ' Early exit if capacity is sufficient (no lock needed for read check)
        If VariableNameDataArray IsNot Nothing AndAlso UBound(VariableNameDataArray, 2) >= requiredIndex Then
            Return
        End If

        SyncLock _recordingBufferLock
            ' Double-check after acquiring lock (another thread may have resized)
            If VariableNameDataArray IsNot Nothing AndAlso UBound(VariableNameDataArray, 2) >= requiredIndex Then
                Return
            End If

            Try
                ' ✅ FIX: Safe null check on myDisplaySignals
                If MyIncaInterface Is Nothing OrElse MyIncaInterface.myDisplaySignals Is Nothing Then
                    HandleUserMessageLogging("GMRC", "EnsureRecordingBufferCapacity: myDisplaySignals not initialized")
                    Return
                End If

                Dim cols As Integer = UBound(MyIncaInterface.myDisplaySignals) + 2

                If VariableNameDataArray Is Nothing Then
                    ' Initial allocation
                    Dim initialCapacity As Integer = Math.Max(requiredIndex + 1, RecordingCapacityStep)
                    ReDim VariableNameDataArray(cols - 1, initialCapacity - 1)  ' ✅ FIX: Use 0-based indices
                    HandleUserMessageLogging("GMRC", $"Recording buffer initialized: {cols} columns × {initialCapacity} rows")
                Else
                    ' ✅ FIX: Only resize second dimension to preserve data
                    Dim currentRows As Integer = UBound(VariableNameDataArray, 2) + 1
                    Dim currentCols As Integer = UBound(VariableNameDataArray, 1) + 1

                    ' ✅ OPTIMIZATION: Exponential growth (2x) instead of linear
                    Dim newCapacity As Integer = currentRows * 2
                    If newCapacity < requiredIndex + 1 Then
                        newCapacity = requiredIndex + 1
                    End If

                    ' ✅ CRITICAL: Create new array and manually copy (ReDim Preserve doesn't work for first dimension)
                    If currentCols <> cols Then
                        ' Column count changed (signal list changed) - need full re-allocation
                        Dim newArray(cols - 1, newCapacity - 1) As String

                        ' Copy existing data
                        Dim copyRows As Integer = Math.Min(currentRows, newCapacity)
                        Dim copyCols As Integer = Math.Min(currentCols, cols)
                        For row As Integer = 0 To copyRows - 1
                            For col As Integer = 0 To copyCols - 1
                                newArray(col, row) = VariableNameDataArray(col, row)
                            Next
                        Next

                        VariableNameDataArray = newArray
                        HandleUserMessageLogging("GMRC", $"Recording buffer resized: {cols} columns × {newCapacity} rows (columns changed)")
                    Else
                        ' Only row count increased - can use ReDim Preserve
                        ReDim Preserve VariableNameDataArray(cols - 1, newCapacity - 1)
                        HandleUserMessageLogging("GMRC", $"Recording buffer expanded: {currentRows} → {newCapacity} rows (2x growth)")
                    End If
                End If

            Catch ex As OutOfMemoryException
                HandleUserMessageLogging("GMRC", $"EnsureRecordingBufferCapacity: Out of memory at {requiredIndex} rows - {ex.Message}", DisplayMsgBox)
                Throw
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"EnsureRecordingBufferCapacity: {ex.Message}")
                Throw
            End Try
        End SyncLock
    End Sub


    ' Helper to get current config as string for comparison
    Public Function GetCurrentConfigData() As String
        Try
            Dim sb As New System.Text.StringBuilder()

            ' Serialize current config values in a deterministic order
            sb.AppendLine($"INCAWorkspace:{INCAWorkspace}")
            sb.AppendLine($"INCADatabase:{INCADatabase}")
            sb.AppendLine($"INCAExperiment:{INCAExperiment}")
            sb.AppendLine($"INCAVariableFile:{INCAVariableFile}")
            ' Add other config values as needed

            Return sb.ToString()
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"GetCurrentConfigData: {ex.Message}")
            Return ""
        End Try
    End Function

    Public Function GetPredictedRecordingFilename() As String
        ' Returns the next/predicted recording filename that INCA will use for the next sequence.
        ' Uses existing helpers where available: GetCurrentRecordingInfo() (preferred) or MyIncaInterface.GetLastRecordingFileName().
        ' Falls back to SaveRecordingFileName if nothing else is available.
        Try
            ' Try the high-fidelity helper if it exists
            Try
                Dim recordingInfo = GetCurrentRecordingInfo()
                ' recordingInfo is a value tuple (BaseName As String, Sequence As Integer)
                If Not String.IsNullOrEmpty(recordingInfo.BaseName) Then
                    Dim baseFileName As String = recordingInfo.BaseName
                    Dim seq As Integer = Convert.ToInt32(recordingInfo.Sequence)
                    ' Predicted "next" sequence filename (INCA exposes the "current" sequence as the sequence to be written next)
                    Return String.Format("{0}_{1:D2}.mf4", baseFileName, seq)
                End If
            Catch exRecInfo As Exception
                ' If GetCurrentRecordingInfo isn't available or fails, we'll fallback to other APIs below.
                HandleUserMessageLogging("GMRC", $"GetCurrentRecordingInfo failed, falling back: {exRecInfo.Message}")
            End Try

            ' Fallback: Ask INCA for the last completed recording file, then increment its sequence.
            Dim lastCompleted As String = ""
            Try
                lastCompleted = MyIncaInterface.GetLastRecordingFileName()
            Catch exLastRec As Exception
                HandleUserMessageLogging("GMRC", $"GetLastRecordingFileName failed: {exLastRec.Message}")
                lastCompleted = String.Empty
            End Try

            If Not String.IsNullOrEmpty(lastCompleted) Then
                ' Attempt to derive base + next sequence from last completed name: e.g. BASE_01.mf4 -> next BASE_02.mf4
                Dim nameNoExt As String = Path.GetFileNameWithoutExtension(lastCompleted)
                Dim ext As String = Path.GetExtension(lastCompleted)
                Dim idxUnder As Integer = nameNoExt.LastIndexOf("_"c)
                If idxUnder > 0 Then
                    Dim baseName As String = nameNoExt.Substring(0, idxUnder)
                    Dim seqStr As String = nameNoExt.Substring(idxUnder + 1)
                    Dim seqNum As Integer
                    If Integer.TryParse(seqStr, seqNum) Then
                        Return String.Format("{0}_{1:D2}{2}", baseName, seqNum + 1, ext)
                    End If
                End If
            End If

            ' Absolute fallback: use whatever SaveRecordingFileName currently holds, or label unknown
            If Not String.IsNullOrEmpty(SaveRecordingFileName) Then Return SaveRecordingFileName
            Return "UNKNOWN_RECORDING_FILENAME.mf4"
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GetPredictedRecordingFilename: " & ex.Message)
            Return If(Not String.IsNullOrEmpty(SaveRecordingFileName), SaveRecordingFileName, "UNKNOWN_RECORDING_FILENAME.mf4")
        End Try
    End Function

    Private Sub EnsureSetLastRecordingFileName(predictedFileName As String)
        ' Calls SetLastRecordingFileName in a safe manner to keep MDA shortcuts in sync.
        Try
            If String.IsNullOrEmpty(predictedFileName) Then Return
            ' SetLastRecordingFileName is expected to be defined elsewhere (global/module). Call it to keep external shortcuts current.
            SetLastRecordingFileName(predictedFileName)
            HandleUserMessageLogging("GMRC", "EnsureSetLastRecordingFileName: Set last recording filename to " & predictedFileName)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "EnsureSetLastRecordingFileName: " & ex.Message)
        End Try
    End Sub

    Private Sub SetLastRecordingFileName(ByVal filename As String)
        ' Best-effort helper to keep the application's idea of the "last recording filename" in sync.
        ' This updates SaveRecordingFileName and will attempt to call any INCA helper method if available.
        Try
            If String.IsNullOrEmpty(filename) Then Return

            ' Update local/global copy
            SaveRecordingFileName = filename

            ' Try to call a strongly-typed API on MyIncaInterface if it exists.
            If MyIncaInterface IsNot Nothing Then
                Try
                    If MyIncaInterface.SetLastRecordingFileName(filename) Then
                        HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: Set via MyIncaInterface to " & filename)
                        Return
                    End If
                Catch ex As Exception
                    ' Log and fall back to comm object
                    HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: MyIncaInterface.SetLastRecordingFileName failed: " & ex.Message)
                End Try

                ' Try the nested MyGmIncaComm object (some versions may expose the setter there)
                Try
                    If MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                        MyIncaInterface.MyGmIncaComm.SetLastRecordingFileName(filename)
                        HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: Set via MyIncaInterface.MyGmIncaComm to " & filename)
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: MyGmIncaComm invocation failed: " & ex.Message)
                End Try
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: " & ex.Message)
        End Try
    End Sub

    Private Sub HandlePullingDiDs()

        'Called from Handle_5_SecondChecks which is called from MyBackgroundTasks.
        'Handles pulling DIDs if this functionality is enabled...

        Dim MyDidPullFiles As String()
        Static lastFileFound As Boolean
        Dim v As Integer

        Try

            If EnableDIDPull = True Then
                If EnableStartZipFileCheck = True Then

                    MyDidPullFiles = Directory.GetFiles(DefaultVSpyDataDirectory)

                    If UBound(MyDidPullFiles) >= MAX_NUM_DID_PULL_FILES - 1 Then

                        For v = 0 To UBound(MyDidPullFiles)
                            If InStr(MyDidPullFiles(v), DIDPullTriggerZippingKey) > 0 Then
                                lastFileFound = True
                                Exit For
                            End If
                        Next

                        If lastFileFound = True Then
                            lastFileFound = False

                            Do While FileInUse(MyDidPullFiles(v))
                                Thread.Sleep(100)
                            Loop

                            ZipTheDirectory(DefaultVSpyDataDirectory, "Start", BaseDataCollectionPath)

                            For v = 0 To UBound(MyDidPullFiles)
                                If InStr(MyDidPullFiles(v), "m.csv") > 0 Then
                                    File.Delete(MyDidPullFiles(v))
                                End If
                            Next

                            EnableStartZipFileCheck = False
                            StopVehicleSpy()
                            UserStatusInfo.Hide()

                            OnVehicleScreen.Button6.Enabled = True 'Start Measurement
                            OnVehicleScreen.Button14.Enabled = True 'Start Record
                        End If
                    End If

                End If

                If EnableEndZipFileCheck = True Then

                    MyDidPullFiles = Directory.GetFiles(DefaultVSpyDataDirectory)

                    If UBound(MyDidPullFiles) >= MAX_NUM_DID_PULL_FILES - 1 Then

                        For v = 0 To UBound(MyDidPullFiles)
                            If InStr(MyDidPullFiles(v), DIDPullTriggerZippingKey) > 0 Then
                                lastFileFound = True
                                Exit For
                            End If
                        Next

                        If lastFileFound = True Then

                            lastFileFound = False

                            Do While FileInUse(MyDidPullFiles(v))
                                Thread.Sleep(100)
                            Loop

                            ZipTheDirectory(DefaultVSpyDataDirectory, "End", BaseDataCollectionPath)

                            For v = 0 To UBound(MyDidPullFiles)
                                If InStr(MyDidPullFiles(v), "m.csv") > 0 Then
                                    File.Delete(MyDidPullFiles(v))
                                End If
                            Next

                            EnableEndZipFileCheck = False
                            UserStatusInfo.Hide()
                            StopVehicleSpy()
                            ExitApp()
                            Exit Sub

                        End If

                    End If

                End If
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandlePullingDIDs - DID Pull Section: " & ex.Message, DisplayMsgBox)
        End Try

    End Sub

    Private Async Function Handle_5_SecondChecks(ByVal lMeasurementStatus As String, ByVal lRecordingState As String) As Task

        'Called from MyBAckgroundTasks every 5 seconds.  Handles stuff which needs to be checked regularly
        'but is not critical to be checked every loop...


        Try

            If CheckForExperiment = True Then

                'If the GetMeasurementStatus returns "Invalid" this indicates that the INCA experiment is no longer
                'running in which case, we will terminate this application.

                'CLEVIR Cannot run without a valid INCA Experiment to latch on to!!!!

                If lMeasurementStatus = "Invalid" Then
                    ' ✅ FIX: Don't set TopMost when LoginForm is active
                    If Not OnLoginScreen Then
                        OnVehicleScreen.TopMost = True
                    End If
                    HandleUserMessageLogging("GMRC", "The INCA Experiment is not running, CLEVIR will now terminate.", DisplayMsgBox)
                    OnVehicleScreen.TopMost = False
                    ExitApp("Complete")

                End If

            End If

            If PlaybackMode = False Then

                Await GetDeviceStatus()

                lIsTargetOnWorkingPage = MyIncaInterface.IsTargetOnWorkingPage

                ' ✅ FIX: Don't manipulate OnVehicleScreen controls when LoginForm is active
                ' RadioButton.Checked can steal focus even without explicit BringToFront()
                If Not OnLoginScreen Then
                    If lIsTargetOnWorkingPage = "True" And OnVehicleScreen.RadioButton1.Checked = False Then
                        OnVehicleScreen.RadioButton1.Checked = True
                        OnVehicleScreen.RadioButton2.Checked = False
                    ElseIf lIsTargetOnWorkingPage = "False" And OnVehicleScreen.RadioButton2.Checked = False Then
                        OnVehicleScreen.RadioButton1.Checked = False
                        OnVehicleScreen.RadioButton2.Checked = True
                    End If
                End If

                'Handle DID pull using vehicle spy (if configured to do so)
                'HandlePullingDiDs()

            End If

            'If we are recording, then we need to check to see if the user changed from reference page
            'to working page, this would indicate that they may be making calibration changes during the
            'recording.  We want to capture this event, so we write an event message into the record file
            'indicating a transition between working page and reference page.

            If lRecordingState = "True" Then

                lIsTargetOnWorkingPage = MyIncaInterface.IsTargetOnWorkingPage

                If lIsTargetOnWorkingPage = "True" And saveIsTargetOnWorkingPage <> "True" Then
                    saveIsTargetOnWorkingPage = "True"
                    MyIncaInterface.WriteEventComment(Format$(DateTime.Now, "HH:mm:ss") & " " & "Detected a Switch to the Working Page")
                    HandleUserMessageLogging("GMRC", "Handle_5_SecondChecks: Detected a Switch to the Working Page")

                ElseIf lIsTargetOnWorkingPage = "False" And saveIsTargetOnWorkingPage <> "False" Then
                    saveIsTargetOnWorkingPage = "False"
                    MyIncaInterface.WriteEventComment(Format$(DateTime.Now, "HH:mm:ss") & " " & "Detected a Switch to the Reference Page")
                    HandleUserMessageLogging("GMRC", "Handle_5_SecondChecks: Detected a Switch to the Reference Page")

                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "Handle_5_SecondChecks: " & ex.Message)
        End Try

    End Function

    Private Sub HandleAutomaticStartRecord(ByVal MyMeasureTime As TimeSpan, ByRef MySaveMeasureTime As DateTime, ByRef oneShot As Boolean)

        'Called from MyBackgroundTasks...

        'if 5 minutes have passed while in measure mode and record button has not been pressed, we will display a message indicating
        'that recording will start in 10 seconds.  User may click CANCEL and stay in measuremode, if they do not cancel within 10
        'seconds, we automatically go into record mode.  This was added because some were running in measure mode, thinking they were
        'actually recording...

        Dim e As EventArgs = EventArgs.Empty

        Try

            If MyMeasureTime.Minutes >= 5 And oneShot = False Then

                oneShot = True

                RecordTransitionDelay = 0
                UserStatusInfo.Button1.Visible = True

                Do While RecordTransitionDelay >= 0 And RecordTransitionDelay <= 10
                    HandleUserMessageLogging("GMRC", "Recording will start in " & 10 - RecordTransitionDelay & " seconds unless CANCEL is pressed...",,, FlashMsgOn)
                    RecordTransitionDelay += 1
                    Thread.Sleep(1000)
                    Application.DoEvents()
                Loop

                If RecordTransitionDelay >= 10 Then
                    HandleUserMessageLogging("GMRC", "Starting Recording NOW...",,, FlashMsgOn)
                    OnVehicleScreen.Button14_Click(OnVehicleScreen.Button14, e)
                End If

                MySaveMeasureTime = DateTime.Now
                MyMeasureTime = DateTime.Now.Subtract(MySaveMeasureTime)
                RecordTransitionDelay = 0
                UserStatusInfo.Close()
                UserStatusInfo.Button1.Visible = False

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleAutomaticStartRecord: " & ex.Message)
        End Try
    End Sub

    Private Sub CheckForIncaButtonPresses(ByVal lMeasurementStatus As String, ByVal lRecordingState As String)
        Try
            If lMeasurementStatus = "True" And MyIncaInterface.MeasurementStarted = False Then
                ' ✅ FIX: Don't redisplay when LoginForm is active
                If Not OnLoginScreen Then
                    RedisplayOnVehicleForm("Main")
                End If
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Measurement/Recording Started in INCA - CLEVIR Will now Stop Measurement and/or Recording.", DisplayMsgBox)
                OnVehicleScreen.TopMost = False

                ' ✅ NEW: Log before blocking call
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Calling StartStopMeasurement (blocking)...")

                ' ✅ KEEP THIS (For now)
                MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                .GetAwaiter().GetResult()

                ' ✅ NEW: Log after completion
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: StartStopMeasurement completed.")

                InSession = False

            ElseIf lMeasurementStatus = "False" And MyIncaInterface.MeasurementStarted = True Then
                ' Handle measurement stopped in INCA
                ' ✅ FIX: Don't redisplay when LoginForm is active
                If Not OnLoginScreen Then
                    RedisplayOnVehicleForm("Main")
                End If
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Measurement Stopped in INCA - Please DO NOT USE INCA to Stop Measurement to avoid unpredictable behavior.", DisplayMsgBox)
                OnVehicleScreen.TopMost = False

                OnVehicleScreen.Button1.Enabled = True
                If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
                    OnVehicleScreen.Button4.Enabled = True
                    MyLogin.Enabled = True
                End If

                MyIncaInterface.Recording = False
                MyIncaInterface.MeasurementStarted = False
                MeasurementStarted = False
                MyIncaInterface.StopDataCollection()
                OnVehicleScreen.Button6.Text = "START MEASUREMENT"
                OnVehicleScreen.Button14.Text = "START RECORD"

                OnVehicleScreen.Button14.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button14.ForeColor = Color.Black
                OnVehicleScreen.Button6.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button6.ForeColor = Color.Black

                InSession = False

            ElseIf lRecordingState = True And MyIncaInterface.Recording = False Then

                RedisplayOnVehicleForm("Main")
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Recording Started in INCA - USE INCA NOW to STOP Recording.", DisplayMsgBox, )
                OnVehicleScreen.TopMost = False

                Do While MyIncaInterface.GetRecordingState = True
                    ActiveIncaApiCall = "Waiting For INCA to Report RecordingState False"
                    Thread.Sleep(100)
                Loop

                ActiveIncaApiCall = String.Empty

                MyIncaInterface.Recording = False
                MyIncaInterface.MeasurementStarted = False
                MeasurementStarted = False
                InSession = False

                OnVehicleScreen.Button1.Enabled = True
                If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
                    OnVehicleScreen.Button4.Enabled = True
                    MyLogin.Enabled = True
                End If

                OnVehicleScreen.Button6.Text = "START MEASUREMENT"
                OnVehicleScreen.Button14.Text = "START RECORD"

                OnVehicleScreen.Button14.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button14.ForeColor = Color.Black
                OnVehicleScreen.Button6.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button6.ForeColor = Color.Black

                'MyIncaInterface.MeasurementStarted = True
                'MyIncaInterface.Recording = True
                'MyIncaInterface.StartDataCollection(DataCollectionRate)
                'OnVehicleScreen.Button6.Text = "STOP MEASUREMENT"
                'OnVehicleScreen.Button6.BackColor = Color.Blue
                'OnVehicleScreen.Button6.ForeColor = Color.White
                'OnVehicleScreen.Button14.Text = "STOP RECORD"
                'OnVehicleScreen.Button14.BackColor = Color.Red
                'OnVehicleScreen.Button14.ForeColor = Color.White

                If RecorderStopWatch Is Nothing Then
                    RecorderStopWatch = New Stopwatch()
                End If

                RecorderStopWatch.Reset()
                RecorderStopWatch.Start()

                'Fusion Status Display, and LMFR Status Display(s) have custom annotation buttons, these
                'must be enabled if we see that we are in record mode...

                If FusionStatusDisplay.Visible = True Then
                    FusionStatusDisplay.Button1.Enabled = True
                    FusionStatusDisplay.Button2.Enabled = True
                    FusionStatusDisplay.Button3.Enabled = True
                    FusionStatusDisplay.Button4.Enabled = True
                    FusionStatusDisplay.Button5.Enabled = True
                    FusionStatusDisplay.Button6.Enabled = True
                    FusionStatusDisplay.Button8.Enabled = True
                    FusionStatusDisplay.Refresh()
                End If

                If LmfrStatusDisplayGlobalA.Visible = True Then
                    LmfrStatusDisplayGlobalA.Button1.Enabled = True
                    LmfrStatusDisplayGlobalA.Refresh()
                End If

                If LmfrStatusScreenHc.Visible = True Then
                    LmfrStatusScreenHc.Button1.Enabled = True
                    LmfrStatusScreenHc.Refresh()
                End If

            ElseIf lRecordingState = False And MyIncaInterface.Recording = True Then

                'HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Recording Stopped in INCA")
                RedisplayOnVehicleForm("Main")
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Recording Stopped in INCA - Please Use CLEVIR to Start and Stop Recording to avoid unpredictable behavior.", DisplayMsgBox, )
                OnVehicleScreen.TopMost = False

                MyIncaInterface.Recording = False
                MyIncaInterface.MeasurementStarted = False
                MeasurementStarted = False
                InSession = False
                OnVehicleScreen.Button14.Text = "START RECORD"
                OnVehicleScreen.Button6.Text = "START MEASUREMENT"
                OnVehicleScreen.Button14.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button14.ForeColor = Color.Black
                OnVehicleScreen.Button6.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button6.ForeColor = Color.Black

                ' ✅ Start background OXTS monitor (non-blocking)
                If OxtsEnabled AndAlso MyOxtsInterface IsNot Nothing Then
                    MonitorOxtsRtkStatus()
                End If

                If RecorderStopWatch Is Nothing Then
                    RecorderStopWatch = New Stopwatch()
                End If
                RecorderStopWatch?.Stop()   ' ← Preserve final duration for logging
                RecorderStopWatch.Reset()

                If MyIncaInterface IsNot Nothing Then
                    Try
                        Dim lastClosed As String = MyIncaInterface.GetLastRecordingFileName()
                        If Not String.IsNullOrEmpty(lastClosed) Then
                            'EnsureSetLastRecordingFileName(lastClosed)
                            HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Finalized last recording filename to " & lastClosed)
                        End If
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Couldn't finalize last recording filename: " & ex.Message)
                    End Try
                End If

                'Fusion Status Display, and LMFR Status Display(s) have custom annotation buttons, these
                'must be disabled if we see that we are in record mode...

                If FusionStatusDisplay.Visible = True Then
                    FusionStatusDisplay.Button1.Enabled = False
                    FusionStatusDisplay.Button2.Enabled = False
                    FusionStatusDisplay.Button3.Enabled = False
                    FusionStatusDisplay.Button4.Enabled = False
                    FusionStatusDisplay.Button5.Enabled = False
                    FusionStatusDisplay.Button6.Enabled = False
                    FusionStatusDisplay.Button8.Enabled = False
                End If

                If LmfrStatusDisplayGlobalA.Visible = True Then
                    LmfrStatusDisplayGlobalA.Button1.Enabled = False
                    LmfrStatusDisplayGlobalA.Refresh()
                End If

                If LmfrStatusScreenHc.Visible = True Then
                    LmfrStatusScreenHc.Button1.Enabled = False
                    LmfrStatusScreenHc.Refresh()
                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: " & ex.Message)
        End Try
    End Sub

    Public Sub HandleWavRecording(Optional ByVal buttonPressed As Boolean = False)

        ' This routine handles the color of the frame around the microphone to indicate whether or not a WAV recording
        ' is in progress. Red: not recording; Green: recording.
        ' Resets WAV recording duration whenever a button is pressed.

        Static recordWavElapseTime As TimeSpan
        Static saveRecordWavTime As DateTime

        Try

            If IsNumeric(RecordWAVTime) Then

                ' Reset the timer and recording state if a button is pressed
                If buttonPressed Then
                    Console.WriteLine("Button Pressed is True")
                    saveRecordWavTime = DateTime.Now
                    HandleUserMessageLogging("GMRC", "HandleWAVRecording: Reset Record WAV Time due to button press.")

                    ' Ensure the microphone UI and recording state are updated
                    If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                        OnVehicleScreen.PictureBox1.BackColor = Color.Green
                        OnVehicleScreen.PictureBox1.Image = My.Resources.Resources.mic_50_Green()
                    End If

                    'StartWAVRecord() ' Reinitiate recording if necessary
                End If

                ' Check if recording is active and duration has elapsed
                If (OnVehicleScreen.PictureBox1.BackColor = Color.Green) And Val(RecordWAVTime) > 0 Then
                    recordWavElapseTime = DateTime.Now.Subtract(saveRecordWavTime)

                    If recordWavElapseTime.TotalSeconds > Val(RecordWAVTime) Then
                        saveRecordWavTime = DateTime.Now
                        HandleUserMessageLogging("GMRC", "HandleWAVRecording: Record WAV Time has expired.")

                        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                            OnVehicleScreen.PictureBox1.BackColor = Color.Red
                            OnVehicleScreen.PictureBox1.Image = My.Resources.Resources.mic_50_Red()
                        End If

                        ' The filename to stop is the one that was stored when the recording started.
                        ' This ensures we are stopping the correct file.
                        Dim filenameToStop As String = If(Not String.IsNullOrEmpty(CurrentWAVFilename), CurrentWAVFilename, audioFilePath)

                        ' Call the refactored StopWAVRecord. It no longer requires the sequence number
                        ' as it correctly logs the stop event using the sequence from when the recording was initiated.
                        StopWAVRecord(filenameToStop)
                    End If
                End If
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleWAVRecording: " & ex.Message)
        End Try

    End Sub

    ''' <summary>
    ''' Handles recording state updates when actively recording.
    ''' Manages: duration-based file rotation, filename tracking, 
    ''' alternate recorder monitoring, and UI updates.
    ''' </summary>
    ''' <remarks>
    ''' Called from MyBackgroundTasks loop only when Recording=True.
    ''' Delegates UI updates to async StartRecordingMonitorTask.
    ''' </remarks>
    Private Sub HandleUpdatesWhenRecording()
        ' Static variables for alternate recorder monitoring
        Static MyAltRcrdElapseTime As TimeSpan
        Static MyAltRcrdSaveTime As DateTime
        Static MyCanAlyzerRcrdElapseTime As TimeSpan
        Static MyCanAlyzerRcrdSaveTime As DateTime

        ' Local working variables
        Dim durationMinutes As Integer

        ' Start the dedicated UI update task if it's not already running.
        If Not _isMonitorTaskRunning Then
            StartRecordingMonitorTask()
        End If

        Try
            ' ==================================================================
            ' SECTION 1: Inform INCA Comm about duration (only if changed)
            ' ==================================================================
            If RecordFileDurationMinutes <> _lastSetRecordFileDurationMinutes Then
                Try
                    If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                        MyIncaInterface.MyGmIncaComm.SetRecordingFileDurationMinutes(RecordFileDurationMinutes)
                        _lastSetRecordFileDurationMinutes = RecordFileDurationMinutes
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", "HandleUpdatesWhenRecording: SetRecordingFileDurationMinutes failed: " & ex.Message)
                End Try
            End If

            ' ==================================================================
            ' SECTION 2: **OPTIMIZED FILENAME CACHING WITH SEQUENCE NUMBER**
            ' ==================================================================
            ' Skipped while a sequence rotation is in flight. StopAndStartRecordingAsync
            ' awaits between the stop and the start, so this loop keeps running during
            ' the window where INCA has closed sequence N but not yet opened N+1.
            ' GetActualRecordingTimeMs/GetCurrentRecordingInfo are unreliable in that
            ' window, and caching a wrong value here would mis-stamp the sequence number
            ' written into LiDAR/OXTS event markers - a silent, post-process-only
            ' failure. StartRecordingAsync repopulates the cache authoritatively once
            ' the rotation completes.
            If OnVehicleScreen.Label5.Visible = True AndAlso
               Not (MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.SequenceRotationInProgress) Then
                Dim shouldUpdateFilename As Boolean = False

                ' ──────────────────────────────────────────────────────────────
                ' CHECK 1: Is cached filename empty/invalid?
                ' ──────────────────────────────────────────────────────────────
                If String.IsNullOrEmpty(CachedRecordingFilename) OrElse
       CachedRecordingFilename = "[Pending...]" Then
                    shouldUpdateFilename = True
                End If

                ' ──────────────────────────────────────────────────────────────
                ' CHECK 2: Has file rotated? (recording time decreased = new file)
                ' ──────────────────────────────────────────────────────────────
                If Not shouldUpdateFilename Then
                    Try
                        Dim currentTimeMs As Integer = MyIncaInterface.GetActualRecordingTimeMs()

                        ' Rotation detected if current time < last known time
                        If currentTimeMs < LastKnownRecordingTimeMs Then
                            shouldUpdateFilename = True
                            HandleUserMessageLogging("GMRC",
                    $"HandleUpdatesWhenRecording: File rotation detected (time: {currentTimeMs}ms < {LastKnownRecordingTimeMs}ms)")
                        End If

                        LastKnownRecordingTimeMs = currentTimeMs

                    Catch ex As Exception
                        ' If time query fails, use periodic refresh as fallback
                        If DateTime.Now.Subtract(LastFilenameUpdateTime).TotalSeconds > 5 Then
                            shouldUpdateFilename = True
                        End If
                    End Try
                End If

                ' ──────────────────────────────────────────────────────────────
                ' CHECK 3: Periodic safety refresh (every 10 seconds)
                ' ──────────────────────────────────────────────────────────────
                If Not shouldUpdateFilename Then
                    If DateTime.Now.Subtract(LastFilenameUpdateTime).TotalSeconds > 10 Then
                        shouldUpdateFilename = True
                    End If
                End If

                ' ══════════════════════════════════════════════════════════════
                ' PERFORM UPDATE ONLY WHEN NECESSARY
                ' ══════════════════════════════════════════════════════════════
                If shouldUpdateFilename Then
                    Try
                        Dim recordingInfo = GetCurrentRecordingInfo()

                        If Not String.IsNullOrEmpty(recordingInfo.BaseName) Then
                            Dim fullFilename As String = String.Format("{0}_{1:D2}.mf4",
                                                                       recordingInfo.BaseName,
                                                                       recordingInfo.Sequence)

                            ' ✅ CRITICAL: Normalize both strings before comparison
                            Dim normalizedFull As String = NormalizeFilename(fullFilename)
                            Dim normalizedCached As String = NormalizeFilename(CachedRecordingFilename)

                            ' Now compare the NORMALIZED strings
                            'If normalizedFull <> normalizedCached Then
                            If Not String.Equals(normalizedFull, normalizedCached, StringComparison.Ordinal) Then
                                CachedRecordingFilename = normalizedFull
                                OnVehicleScreen.Label5.Text = $"Recording Filename: {CachedRecordingFilename}"
                                SaveRecordingFileName = CachedRecordingFilename
                                LastFilenameUpdateTime = DateTime.Now
                                HandleUserMessageLogging("GMRC", $"Updated to sequence {recordingInfo.Sequence:D2}: {CachedRecordingFilename}")
                            End If
                        Else
                            ' Fallback to prediction...
                            Dim predicted As String = GetPredictedRecordingFilename()
                            If Not String.IsNullOrEmpty(predicted) AndAlso NormalizeFilename(predicted) <> NormalizeFilename(CachedRecordingFilename) Then
                                CachedRecordingFilename = NormalizeFilename(predicted)
                                OnVehicleScreen.Label5.Text = $"Recording Filename: {CachedRecordingFilename}"
                                SaveRecordingFileName = CachedRecordingFilename
                                LastFilenameUpdateTime = DateTime.Now
                            End If
                        End If

                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC",
                                                 $"HandleUpdatesWhenRecording: Filename update error - {ex.Message}")
                    End Try
                End If
            End If

            ' ==================================================================
            ' SECTION 3: Handle duration-based rotation
            ' ==================================================================
            durationMinutes = RecordFileDurationMinutes
            Try
                If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                    durationMinutes = MyIncaInterface.MyGmIncaComm.GetRecordingFileDurationMinutes()
                End If
            Catch ex As Exception
                HandleUserMessageLogging("GMRC",
                "HandleUpdatesWhenRecording: GetRecordingFileDurationMinutes failed: " & ex.Message)
            End Try

            HandleRecordingDurationRotation(durationMinutes)

            ' ==================================================================
            ' SECTION 4: Monitor VehicleSpy health
            ' ==================================================================
            If VehicleSpyCaptureStarted = True And CheckVSpyMessageDisplayed = False Then
                MyAltRcrdElapseTime = DateTime.Now.Subtract(MyAltRcrdSaveTime)
                If MyAltRcrdElapseTime.Seconds >= 5 Then
                    MyAltRcrdSaveTime = DateTime.Now
                    If IsProcessRunning("VSPY3") = False Then
                        VehicleSpyCaptureStarted = False
                        Dim currentActiveSequence As String = GetCurrentActiveSequence()
                        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

                        HandleUserMessageLogging("GMRC",
                        $"Vehicle Spy is no longer running for sequence {currentSeq:D2}. " &
                        "It is possible that it was manually exited. To re-enable Vehicle Spy, " &
                        "you must exit and re-launch CLEVIR...",,, FlashMsg4Sec)
                        OnVehicleScreen.Label3.BackColor = Color.Red
                        AlternateRecordEnabled = False
                        CheckVSpyMessageDisplayed = True
                    End If
                End If
            End If

            ' ==================================================================
            ' SECTION 5: Monitor CANalyzer health
            ' ==================================================================
            If CanalyzerCaptureStarted = True And CheckCanAlyzerMessageDisplayed = False Then
                MyCanAlyzerRcrdElapseTime = DateTime.Now.Subtract(MyCanAlyzerRcrdSaveTime)
                If MyCanAlyzerRcrdElapseTime.Seconds >= 5 Then
                    MyCanAlyzerRcrdSaveTime = DateTime.Now
                    If IsProcessRunning("CANW64") = False Then
                        CanalyzerCaptureStarted = False
                        Dim currentActiveSequence As String = GetCurrentActiveSequence()
                        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

                        HandleUserMessageLogging("GMRC",
                        $"CANalyzer is no longer running for sequence {currentSeq:D2}. " &
                        "It is possible that it was manually exited. To re-enable CANalyzer, " &
                        "you must exit and re-launch CLEVIR...",,, FlashMsg4Sec)
                        OnVehicleScreen.Label3.BackColor = Color.Red
                        AlternateRecordEnabled = False
                        CheckCanAlyzerMessageDisplayed = True
                    End If
                End If
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleUpdatesWhenRecording: " & ex.Message)
        End Try
    End Sub


    ''' <summary>
    ''' Normalizes a filename string by removing invisible characters and whitespace.
    ''' Prevents string comparison failures due to hidden Unicode artifacts.
    ''' </summary>
    Private Function NormalizeFilename(ByVal filename As String) As String
        If String.IsNullOrEmpty(filename) Then Return String.Empty

        ' Step 1: Trim leading/trailing whitespace
        Dim result As String = filename.Trim()

        ' Step 2: Remove zero-width characters (U+200B, U+FEFF, etc.)
        result = result.Replace(ChrW(&H200B), String.Empty)  ' Zero-width space
        result = result.Replace(ChrW(&HFEFF), String.Empty)  ' BOM
        result = result.Replace(ChrW(&H200C), String.Empty)  ' Zero-width non-joiner
        result = result.Replace(ChrW(&H200D), String.Empty)  ' Zero-width joiner

        ' Step 3: Remove any null characters
        result = result.Replace(vbNullChar, String.Empty)

        ' Step 4: Normalize Unicode form (combine decomposed characters)
        result = result.Normalize(NormalizationForm.FormC)  ' ← FIXED

        Return result
    End Function

    Private _rotationInProgress As Boolean = False

    Private Async Sub HandleRecordingDurationRotation(ByVal durationMinutes As Integer)
        If durationMinutes <= 0 Then
            RecorderStopWatch?.Reset()
            Return
        End If

        If RecorderStopWatch.Elapsed.TotalMilliseconds < (durationMinutes * 60000) Then Return

        ' Guard against re-entry: the background loop can fire again while the
        ' async rotation is still in progress
        If _rotationInProgress Then Return
        _rotationInProgress = True

        Try
            ' ✅ Get current sequence BEFORE rotation
            Dim currentInfo = GetCurrentRecordingInfo()
            HandleUserMessageLogging("GMRC",
                                     $"Record duration ({durationMinutes} min) expired for seq {currentInfo.Sequence:D2}")

            ' ✅ CRITICAL: Rotate FIRST, then get the new filename
            Await MyIncaInterface.StopAndStartRecordingAsync()

            ' ✅ NOW get the predicted filename (should return sequence 02 after rotation)
            Dim predictedNext = GetPredictedRecordingFilename()
            HandleUserMessageLogging("GMRC", $"Rotation complete - new sequence: {predictedNext}")
            'EnsureSetLastRecordingFileName(predictedNext)

            If Not CheckRecordingFileNameFormat(displayMsg:=False) Then
                HandleUserMessageLogging("GMRC", "HandleRecordingDurationRotation: Recording file name format check failed", DisplayMsgBox)
                Await MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6)
            End If

            RecorderStopWatch.Reset()
            RecorderStopWatch.Start()

        Catch ex As Exception
            ' Async Sub — exceptions must be handled here or they crash the process
            HandleUserMessageLogging("GMRC", $"HandleRecordingDurationRotation: {ex.Message}", DisplayMsgBox)
        Finally
            _rotationInProgress = False
        End Try
    End Sub
    Private Async Sub StartRecordingMonitorTask()
        ' Dispose of any previous CTS and create a new one.
        _recordingMonitorCts?.Dispose()
        _recordingMonitorCts = New CancellationTokenSource()
        Dim token As CancellationToken = _recordingMonitorCts.Token

        _isMonitorTaskRunning = True

        Try
            While Not token.IsCancellationRequested
                Dim timeString As String = ""
                If RecordFileDurationMinutes > 0 AndAlso RecorderStopWatch?.IsRunning Then
                    Dim elapsedTime As TimeSpan = RecorderStopWatch.Elapsed
                    ' Format to show Minutes and Seconds, padding seconds with a leading zero.
                    timeString = $"{elapsedTime.Minutes}:{elapsedTime.Seconds:00}"
                End If

                ' To minimize UI thread work, only invoke an update if the text has changed.
                If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                    If OnVehicleScreen.Label8.Text <> timeString Then
                        If OnVehicleScreen.InvokeRequired Then
                            OnVehicleScreen.Invoke(Sub() OnVehicleScreen.Label8.Text = timeString)
                        Else
                            OnVehicleScreen.Label8.Text = timeString
                        End If
                    End If
                End If

                ' Wait for 100ms or until cancellation is requested.
                Await Task.Delay(100, token)
            End While
        Catch ex As TaskCanceledException
            ' This is the expected exception when the task is cancelled.
            ' We can safely ignore it and let the method exit gracefully.
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Error in recording monitor task: {ex.Message}")
        Finally
            ' Ensure the flag is reset and resources are cleaned up when the loop exits.
            _isMonitorTaskRunning = False
            _recordingMonitorCts?.Dispose()
            _recordingMonitorCts = Nothing
        End Try
    End Sub

    Private Sub StopRecordingMonitorTask()
        ' Signal the task to cancel. The task itself will handle cleanup.
        _recordingMonitorCts?.Cancel()
    End Sub

    Private Sub HandleClevirDisplayOfClusterMsgs(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks.
        'Displays LCC ClusterMessageText in a momentary UserInfoStatus message 

        Static messageDisplayed As Boolean
        Dim lccClusterMessageText As String
        Dim lccButtonPress As Integer = 0
        Dim lccClusterMsg As Integer = 0
        Static saveLccClusterMsg As Integer = 0
        Static MySaveTime4 As DateTime
        Static MyElapseTime4 As TimeSpan


        lccClusterMsg = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LCC_CLUSTER_MSG, 1)).SignalData)
        lccButtonPress = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LCC_BUTTON_PRESS, 1)).SignalData)

        'COMMENT OUT BEFORE BUILDING!!!!!!!!!!!!!!!!!!!!!

        'LCC_CLUSTER_MSG = GetRandom(0, 25)
        'LCC_BUTTON_PRESS = GetRandom(0, 2)
        'LCC_BUTTON_PRESS = 0
        'LCC_CLUSTER_MSG = 0

        If (((saveLccClusterMsg <> lccClusterMsg) And saveLccClusterMsg <> 0) Or lccButtonPress > 0) And messageDisplayed = False Then

            saveLccClusterMsg = lccClusterMsg

            Select Case lccClusterMsg
                Case 0
                    'CeVGCR_e_LWI_NoIndication()
                    lccClusterMessageText = ""
                Case 1
                    'CeVGCR_e_LWI_LaneLines()
                    lccClusterMessageText = "Lane Lines"
                Case 2
                    'CeVGCR_e_LWI_TightCurve()
                    lccClusterMessageText = "Tight Curve"
                Case 3

                    'CeVGCR_e_LWI_FreewayEnds()
                    lccClusterMessageText = "Freeway Ends"
                Case 4

                    'CeVGCR_e_LWI_Construction()
                    lccClusterMessageText = "Construction"
                Case 5

                    'CeVGCR_e_LWI_ExcessiveSpeed()
                    lccClusterMessageText = "Excessive Speed"
                Case 6

                    'CeVGCR_e_LWI_VehicleProximity()
                    lccClusterMessageText = "Vehicle Proximity"
                Case 7

                    'CeVGCR_e_LWI_CruiseDisengaged()
                    lccClusterMessageText = "Cruise Disengaged"
                Case 8

                    'CeVGCR_e_LWI_Unavailable()
                    lccClusterMessageText = "Unavailable"
                Case 9

                    'CeVGCR_e_LWI_AttentionUnknown()
                    lccClusterMessageText = "Attention Unknown"
                Case 10

                    'CeVGCR_e_LWI_DueToWeather()
                    lccClusterMessageText = "Due To Weather"
                Case 11

                    'CeVGCR_e_LWI_TakeStr()
                    lccClusterMessageText = "Take Steering Wheel"
                Case 12

                    'CeVGCR_e_LWI_TakeVehicleCtl()
                    lccClusterMessageText = "Take Vehicle Control"
                Case 13

                    'CeVGCR_e_LWI_ServDrvAsstSystm()
                    lccClusterMessageText = "Service Driver Assist System"
                Case 14

                    'CeVGCR_e_LWI_LnFollowingLckdOut()
                    lccClusterMessageText = "Lane Following Locked Out"
                Case 15
                    'CeVGCR_e_LWI_LaneEnding()
                    lccClusterMessageText = "Lane Ending"
                Case 16
                    'CeVGCR_e_LWI_ExitLane()
                    lccClusterMessageText = "Exit Lane"
                Case 17
                    'CeVGCR_e_LWI_GM_Authority()
                    lccClusterMessageText = "GM Authority"
                Case 18
                    'CeVGCR_e_LWI_VehicleSetting()
                    lccClusterMessageText = "Vehicle Setting"
                Case 19
                    'CeVGCR_e_LWI_AdaptiveCruise()
                    lccClusterMessageText = "Adaptive Cruise"
                Case 20
                    'CeVGCR_e_LWI_ControlledAccess()
                    lccClusterMessageText = "Controlled Access"
                Case 21
                    'CeVGCR_e_LWI_DrvrAttnOffRoad()
                    lccClusterMessageText = "Driver Attention Off Road"
                Case 22
                    'CeVGCR_e_LWI_GPS_Unavailable()
                    lccClusterMessageText = "GPS Unavailable"
                Case 23
                    'CeVGCR_e_LWI_DrvrAction()
                    lccClusterMessageText = "Driver Action"
                Case 24
                    'CeVGCR_e_LWI_VehicleCenter()
                    lccClusterMessageText = "Vehicle Center"
                Case 25
                    'CeVGCR_e_LWI_SensorImpaired()
                    lccClusterMessageText = "Sensor Impaired"
                Case Else

                    lccClusterMessageText = "Undefined"
            End Select

            If Len(lccClusterMessageText) > 0 Then

                messageDisplayed = True

                HandleUserMessageLogging("GMRC", CStr(lccClusterMsg) & ": " & lccClusterMessageText,,, FlashMsgOn)

                UserStatusInfo.Hide()
                UserStatusInfo.Show()
                UserStatusInfo.BringToFront()
                UserStatusInfo.Label1.Refresh()
                UserStatusInfo.Refresh()

                MySaveTime4 = DateTime.Now

            End If

        End If

        If messageDisplayed = True Then

            MyElapseTime4 = DateTime.Now.Subtract(MySaveTime4)

            If MyElapseTime4.Seconds >= 3 Then

                UserStatusInfo.Hide()

                messageDisplayed = False
            End If

        End If

    End Sub

    Private Sub Handle_CS_WAVRECORD(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks.
        'Starts WAV Recording on button press using CAN Message Input...

        Dim Mytempval As Double
        Dim MyFormattedString As String

        'CS_WAVRECORD must be in AlsoAssociatedWith column of input in signal list for this to work...
        If MyDg.CS_WAVRECORD > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_WAVRECORD, 1)).SignalData

            'This is for testing only, have written code in label1.click (label at top right of screen) event on onvehiclescreen to test this...
            If TriggerWAVRecording = True Then
                Mytempval = 1.0 'For Testing...
                TriggerWAVRecording = False
            End If

            'We are translating input to a string using the FormatDisplayString function, assumes Mytempval will be either 1 TRUE or 0 FALSE
            MyFormattedString = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_WAVRECORD, 1), MyDg.DeviceName(MyDg.CS_WAVRECORD, 1), MyDg.VariableName(MyDg.CS_WAVRECORD, 1))

            If InStr(UCase(MyFormattedString), "TRUE") > 0 And InSession = True Then

                'Handle logic for WAV RECORD Here...

                'Currently, will only start WAV recording if we are recording data...
                'We will trigger recording a WAV file for the defined duration only...

                If OnVehicleScreen.PictureBox1.BackColor = Color.Red Then
                    MicrophoneClick(OnVehicleScreen.PictureBox1)
                    HandleUserMessageLogging("GMRC", "Starting WAV Recording...",,, FlashMsg1Sec)
                    UserStatusInfo.Label1.Text = ""
                    UserStatusInfo.Hide()
                Else
                    HandleUserMessageLogging("GMRC", "WAV Recording already in progress...",,, FlashMsg1Sec)
                    UserStatusInfo.Label1.Text = ""
                    UserStatusInfo.Hide()

                End If

            End If

        End If

    End Sub

    Private Async Sub HandleNanStatus(ByVal MyDg As GridDataClass)

        'Called from ProcessGrids (fire-and-forget async).
        'Alerts driver to a NAN fault without blocking the UI thread.

        ' Guard: If an alert is already in progress, don't start another
        If _nanAlertInProgress Then Return

        Dim Mytempval As Double
        Dim MyFormattedString As String

        If MyDg.CS_NAN_STATUS > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NAN_STATUS, 1)).SignalData

            'Mytempval = 1.0 For Testing...

            MyFormattedString = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NAN_STATUS, 1), MyDg.DeviceName(MyDg.CS_NAN_STATUS, 1), MyDg.VariableName(MyDg.CS_NAN_STATUS, 1))

            If InStr(UCase(MyFormattedString), "TRUE") > 0 And InSession = True Then

                ' Set guard flag and use non-blocking delay
                _nanAlertInProgress = True
                Try
                    Await Task.Delay(2500)
                    HandleUserMessageLogging("GMRC", "NAN ISSUE IS ACTIVE FOR THIS KEY CYCLE!  PLEASE PRESS STOP RECORD - EXIT - AND SLEEP OR SHUNT THE VEHICLE BEFORE CONTINUING!",,, FlashMsg5Sec)
                    UserStatusInfo.Label1.Text = ""
                    UserStatusInfo.Hide()
                Finally
                    _nanAlertInProgress = False
                End Try

            End If

        End If

    End Sub

    Private Sub HandleArrowDisplay(ByVal MycolorStr As String, ByVal MyDirection As String)

        'Called from HandleALCDisplayElements.  Displays indications related to Automatic Lane Change on the Top Down View...

        Select Case MyDirection

            Case "Left"

                Select Case MycolorStr

                    Case "Invisible"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Gray"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Green"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Red"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Yellow"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Blue"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.BringToFront()
                End Select

            Case "Right"

                Select Case MycolorStr

                    Case "Invisible"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Gray"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Green"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Red"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Yellow"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Blue"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.BringToFront()

                End Select

        End Select

    End Sub

    Private Sub HandleAlcDisplayElements(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks.
        'Handles display indications related to Automatic Lane Change on the Top Down View...

        'None
        'Driver
        'Merge
        'Route
        'Traffic

        'VeLFFR_e_LnChgDcsnRsn
        '0 -> 'CeLFFR_e_LnChgRsn_None'
        '1 -> 'CeLFFR_e_LnChgRsn_Driver'
        '2 -> 'CeLFFR_e_LnChgRsn_Merge'
        '3 -> 'CeLFFR_e_LnChgRsn_Route'
        '4 -> 'CeLFFR_e_LnChgRsn_Traffic'

        'VeLFFR_e_LnChgSt
        'Hex -> phys
        '0 -> 'CeLFFR_e_LnChg_Inactv' Invisible
        '1 -> 'CeLFFR_e_LnChg_Inhb'   Red
        '2 -> 'CeLFFR_e_LnChg_Standby'Gray
        '3 -> 'CeLFFR_e_LnChg_ChkLt' Yellow
        '4 -> 'CeLFFR_e_LnChg_ChkRt' Yellow
        '5 -> 'CeLFFR_e_LnChg_ChgLt' Green
        '6 -> 'CeLFFR_e_LnChg_ChgRt' Green
        '7 -> 'CeLFFR_e_LnChg_ReqCancel' Gray
        '8 -> 'CeLFFR_e_LnChg_NotFeasible' Red
        '9 -> 'CeLFFR_e_LnChg_ReqInvld' Red
        '10 -> 'CeLFFR_e_LnChg_Returning' Blue
        '11 -> 'CeLFFR_e_LnChg_Straddle'Red
        '12 -> 'CeLFFR_e_LnChg_DrvrOvrrd' Gray
        '13 -> 'CeLFFR_e_LnChg_Cmpt' Invisible

        Dim Mytempval As Double

        Static tempstr1 As String = "0"
        Dim tempstr2 As String = "0"
        Static whichSide As String = "None"

        Static reasonLeft As String = "NONE"
        Static reasonRight As String = "NONE"

        If MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN > 0 And MyDg.CS_ALC_LANE_CHANGE_STATE > 0 Then

            If MyTdGraphicsContainer.myALCStatusRightLabel.Visible = False Then

                MyTdGraphicsContainer.myALCStatusRightLabel.Visible = True
                MyTdGraphicsContainer.myALCStatusRightLabel.BringToFront()

                MyTdGraphicsContainer.myALCStatusLeftLabel.Visible = True
                MyTdGraphicsContainer.myALCStatusLeftLabel.BringToFront()

                MyTdGraphicsContainer.myALCReasonRightLabel.Visible = True
                MyTdGraphicsContainer.myALCReasonRightLabel.BringToFront()

                MyTdGraphicsContainer.myALCReasonLeftLabel.Visible = True
                MyTdGraphicsContainer.myALCReasonLeftLabel.BringToFront()

            End If

            If whichSide = "None" Then

                reasonLeft = "NONE"
                reasonRight = "NONE"

                MyTdGraphicsContainer.myALCReasonLeftLabel.Text = reasonLeft
                MyTdGraphicsContainer.myALCReasonRightLabel.Text = reasonRight

                Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1)).SignalData
                'Mytempval = GetRandom(0, 4) 'FOR TESTING ONLY!!!
                tempstr1 = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1), MyDg.DeviceName(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1), MyDg.VariableName(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1))

            End If

            If InStr(tempstr1, "CeLFFR_e_LnChgRsn_None") > 0 Or tempstr1 = "0" Then

                If whichSide = "None" Then
                    reasonLeft = "NONE"
                    reasonRight = "NONE"
                    MyTdGraphicsContainer.myALCReasonLeftLabel.Text = reasonLeft
                    MyTdGraphicsContainer.myALCReasonRightLabel.Text = reasonRight
                End If

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Driver") > 0 Or tempstr1 = "1" Then

                reasonLeft = "DRIVER"
                reasonRight = "DRIVER"

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Merge") > 0 Or tempstr1 = "2" Then

                reasonLeft = "MERGE"
                reasonRight = "MERGE"

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Route") > 0 Or tempstr1 = "3" Then

                reasonLeft = "ROUTE"
                reasonRight = "ROUTE"

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Traffic") > 0 Or tempstr1 = "4" Then

                reasonLeft = "TRAFFIC"
                reasonRight = "TRAFFIC"

            Else
                HandleUserMessageLogging("GMRC", "HandleALCDisplayElements: Invalid Enumeration for CS_ALC_LANE_CHANGE_DCSN_RSN", DisplayMsgBox)
            End If

            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ALC_LANE_CHANGE_STATE, 1)).SignalData
            'Mytempval = GetRandom(0, 13) 'FOR TESTING ONLY!!!
            tempstr2 = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ALC_LANE_CHANGE_STATE, 1), MyDg.DeviceName(MyDg.CS_ALC_LANE_CHANGE_STATE, 1), MyDg.VariableName(MyDg.CS_ALC_LANE_CHANGE_STATE, 1))

            If InStr(tempstr2, "CeLFFR_e_LnChg_Inactv") > 0 Then 'Invisible

                If whichSide = "None" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "IN-ACTIVE"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "IN-ACTIVE"

                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Inhb") > 0 Then 'Red

                If whichSide = "None" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "INHIBIT"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "INHIBIT"

                    HandleArrowDisplay("Red", "Left")
                    HandleArrowDisplay("Red", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Standby") > 0 Then 'Gray

                If whichSide = "None" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "STAND BY"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "STAND BY"

                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Gray", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChkLt") > 0 Then 'Yellow

                If whichSide <> "Right" Then

                    MyTdGraphicsContainer.myALCReasonRightLabel.Text = ""
                    MyTdGraphicsContainer.myALCReasonLeftLabel.Text = reasonLeft

                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "CHK LEFT"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = ""
                    whichSide = "Left"
                    HandleArrowDisplay("Yellow", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChkRt") > 0 Then 'Yellow

                If whichSide <> "Left" Then

                    MyTdGraphicsContainer.myALCReasonRightLabel.Text = reasonRight
                    MyTdGraphicsContainer.myALCReasonLeftLabel.Text = ""

                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = ""
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "CHK RIGHT"
                    whichSide = "Right"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Yellow", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChgLt") > 0 Then 'Green

                If whichSide <> "Right" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "CHG LEFT"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = ""
                    whichSide = "Left"
                    HandleArrowDisplay("Green", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChgRt") > 0 Then 'Green

                If whichSide <> "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = ""
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "CHG RIGHT"
                    whichSide = "Right"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Green", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ReqCancel") > 0 Then 'Gray

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "REQ CANCEL"
                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "REQ CANCEL"
                    HandleArrowDisplay("Gray", "Right")
                    HandleArrowDisplay("Invisible", "Left")
                End If

                whichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_NotFeasible") > 0 Then 'Red
                MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "NOT FEAS"
                MyTdGraphicsContainer.myALCStatusRightLabel.Text = "NOT FEAS"
                HandleArrowDisplay("Red", "Left")
                HandleArrowDisplay("Red", "Right")
                whichSide = "None"
            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ReqInvld") > 0 Then 'Red

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "REQ INVALID"
                    HandleArrowDisplay("Red", "Left")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "REQ INVALID"
                    HandleArrowDisplay("Red", "Right")
                End If

                whichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Returning") > 0 Then 'Blue

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "RETURNING"
                    HandleArrowDisplay("Blue", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "RETURNING"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Blue", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Straddle") > 0 Then 'Red

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "STRADDLE"
                    HandleArrowDisplay("Red", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "STRADDLE"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Red", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_DrvrOvrrd") > 0 Then 'Gray

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "DRVR OVRD"
                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "DRVR OVRD"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Gray", "Right")
                End If

                whichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Cmpt") > 0 Then 'Invisible

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "COMPLETE"
                    HandleArrowDisplay("Invisible", "Left")
                End If

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "COMPLETE"
                    HandleArrowDisplay("Invisible", "Right")
                End If

                whichSide = "None"
            Else
                HandleUserMessageLogging("GMRC", "HandleALCDisplayElements: Invalid Enumeration for CS_ALC_LANE_CHANGE_STATE", DisplayMsgBox)
                ' MsgBox("HandleALCDisplayElements: Invalid Enumeration for CS_ALC_LANE_CHANGE_STATE")
            End If

        End If

    End Sub

    Private Sub HandleCoPilotStatusDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks.
        'Handles copilot status display...

        Dim Mytempval As Double

        If MyDg.CS_K1C_COPR_SYSSTAT > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_K1C_COPR_SYSSTAT, 1)).SignalData
            CopilotStatusDisplay.Label3.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_K1C_COPR_SYSSTAT, 1), MyDg.DeviceName(MyDg.CS_K1C_COPR_SYSSTAT, 1), MyDg.VariableName(MyDg.CS_K1C_COPR_SYSSTAT, 1))
            CopilotStatusDisplay.Label3.BackColor = If(InStr(CopilotStatusDisplay.Label3.Text, "CeCOPR_e_Operational") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_K2C_COPR_SYSSTAT > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_K2C_COPR_SYSSTAT, 1)).SignalData
            CopilotStatusDisplay.Label4.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_K2C_COPR_SYSSTAT, 1), MyDg.DeviceName(MyDg.CS_K2C_COPR_SYSSTAT, 1), MyDg.VariableName(MyDg.CS_K2C_COPR_SYSSTAT, 1))
            CopilotStatusDisplay.Label4.BackColor = If(InStr(CopilotStatusDisplay.Label4.Text, "CeCOPR_e_Operational") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PriAutoBrkSysDrInfcStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriAutoBrkSysDrInfcStat, 1)).SignalData
            CopilotStatusDisplay.Label14.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PriAutoBrkSysDrInfcStat, 1), MyDg.DeviceName(MyDg.CS_PriAutoBrkSysDrInfcStat, 1), MyDg.VariableName(MyDg.CS_PriAutoBrkSysDrInfcStat, 1))
            CopilotStatusDisplay.Label14.BackColor = If(InStr(CopilotStatusDisplay.Label14.Text, "Driver Intervention Not Detected") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS__PriAutoBrkSysDrInfcStatRed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__PriAutoBrkSysDrInfcStatRed, 1)).SignalData
            CopilotStatusDisplay.Label13.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__PriAutoBrkSysDrInfcStatRed, 1), MyDg.DeviceName(MyDg.CS__PriAutoBrkSysDrInfcStatRed, 1), MyDg.VariableName(MyDg.CS__PriAutoBrkSysDrInfcStatRed, 1))
            CopilotStatusDisplay.Label13.BackColor = If(InStr(CopilotStatusDisplay.Label13.Text, "Driver Intervention Not Detected") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_SecAutoBrkSysDrInfcStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SecAutoBrkSysDrInfcStat, 1)).SignalData
            CopilotStatusDisplay.Label18.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_SecAutoBrkSysDrInfcStat, 1), MyDg.DeviceName(MyDg.CS_SecAutoBrkSysDrInfcStat, 1), MyDg.VariableName(MyDg.CS_SecAutoBrkSysDrInfcStat, 1))
            CopilotStatusDisplay.Label18.BackColor = If(InStr(CopilotStatusDisplay.Label18.Text, "Driver Intervention Not Detected") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS__SecAutoBrkSysDrInfcStatRed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__SecAutoBrkSysDrInfcStatRed, 1)).SignalData
            CopilotStatusDisplay.Label17.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__SecAutoBrkSysDrInfcStatRed, 1), MyDg.DeviceName(MyDg.CS__SecAutoBrkSysDrInfcStatRed, 1), MyDg.VariableName(MyDg.CS__SecAutoBrkSysDrInfcStatRed, 1))
            CopilotStatusDisplay.Label17.BackColor = If(InStr(CopilotStatusDisplay.Label17.Text, "Driver Intervention Not Detected") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_CE_AutoStrgCmndStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CE_AutoStrgCmndStat, 1)).SignalData
            CopilotStatusDisplay.Label21.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_CE_AutoStrgCmndStat, 1), MyDg.DeviceName(MyDg.CS_CE_AutoStrgCmndStat, 1), MyDg.VariableName(MyDg.CS_CE_AutoStrgCmndStat, 1))
            CopilotStatusDisplay.Label21.BackColor = If(InStr(CopilotStatusDisplay.Label21.Text, "Active") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_HS_AutoStrgCmndStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HS_AutoStrgCmndStat, 1)).SignalData
            CopilotStatusDisplay.Label26.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HS_AutoStrgCmndStat, 1), MyDg.DeviceName(MyDg.CS_HS_AutoStrgCmndStat, 1), MyDg.VariableName(MyDg.CS_HS_AutoStrgCmndStat, 1))
            CopilotStatusDisplay.Label26.BackColor = If(InStr(CopilotStatusDisplay.Label26.Text, "Active") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_AutoPropAxlTrqArbStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AutoPropAxlTrqArbStat, 1)).SignalData
            CopilotStatusDisplay.Label25.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AutoPropAxlTrqArbStat, 1), MyDg.DeviceName(MyDg.CS_AutoPropAxlTrqArbStat, 1), MyDg.VariableName(MyDg.CS_AutoPropAxlTrqArbStat, 1))
            CopilotStatusDisplay.Label25.BackColor = If(InStr(CopilotStatusDisplay.Label25.Text, "Active") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_AATPCS_PropSysStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AATPCS_PropSysStat, 1)).SignalData
            CopilotStatusDisplay.Label29.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AATPCS_PropSysStat, 1), MyDg.DeviceName(MyDg.CS_AATPCS_PropSysStat, 1), MyDg.VariableName(MyDg.CS_AATPCS_PropSysStat, 1))
            CopilotStatusDisplay.Label29.BackColor = If(InStr(CopilotStatusDisplay.Label29.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PriBrkSysStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriBrkSysStat, 1)).SignalData
            CopilotStatusDisplay.Label36.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PriBrkSysStat, 1), MyDg.DeviceName(MyDg.CS_PriBrkSysStat, 1), MyDg.VariableName(MyDg.CS_PriBrkSysStat, 1))
            CopilotStatusDisplay.Label36.BackColor = If(InStr(CopilotStatusDisplay.Label36.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS__PriBrkSysStatRed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__PriBrkSysStatRed, 1)).SignalData
            CopilotStatusDisplay.Label35.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__PriBrkSysStatRed, 1), MyDg.DeviceName(MyDg.CS__PriBrkSysStatRed, 1), MyDg.VariableName(MyDg.CS__PriBrkSysStatRed, 1))
            CopilotStatusDisplay.Label35.BackColor = If(InStr(CopilotStatusDisplay.Label35.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_SecBrkSysStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SecBrkSysStat, 1)).SignalData
            CopilotStatusDisplay.Label32.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_SecBrkSysStat, 1), MyDg.DeviceName(MyDg.CS_SecBrkSysStat, 1), MyDg.VariableName(MyDg.CS_SecBrkSysStat, 1))
            CopilotStatusDisplay.Label32.BackColor = If(InStr(CopilotStatusDisplay.Label32.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS__SecBrkSysStatRed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__SecBrkSysStatRed, 1)).SignalData
            CopilotStatusDisplay.Label31.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__SecBrkSysStatRed, 1), MyDg.DeviceName(MyDg.CS__SecBrkSysStatRed, 1), MyDg.VariableName(MyDg.CS__SecBrkSysStatRed, 1))
            CopilotStatusDisplay.Label31.BackColor = If(InStr(CopilotStatusDisplay.Label31.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PriGdVolt > 0 Then
            CopilotStatusDisplay.Label44.Text = Format(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriGdVolt, 1)).SignalData, "0.00")
            CopilotStatusDisplay.Label44.BackColor = If(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriGdVolt, 1)).SignalData >= 13.6, Color.Green, Color.Red)
        End If

        If MyDg.CS_SecGdVolt > 0 Then
            CopilotStatusDisplay.Label43.Text = Format(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriGdVolt, 1)).SignalData, "0.00")
            CopilotStatusDisplay.Label43.BackColor = If(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SecGdVolt, 1)).SignalData >= 13.6, Color.Green, Color.Red)
        End If

        If MyDg.CS_SysPwrMd > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SysPwrMd, 1)).SignalData
            CopilotStatusDisplay.Label40.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_SysPwrMd, 1), MyDg.DeviceName(MyDg.CS_SysPwrMd, 1), MyDg.VariableName(MyDg.CS_SysPwrMd, 1))
            CopilotStatusDisplay.Label40.BackColor = If(InStr(CopilotStatusDisplay.Label40.Text, "Run") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PrplsnSysAtv > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PrplsnSysAtv, 1)).SignalData
            CopilotStatusDisplay.Label39.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PrplsnSysAtv, 1), MyDg.DeviceName(MyDg.CS_PrplsnSysAtv, 1), MyDg.VariableName(MyDg.CS_PrplsnSysAtv, 1))
            CopilotStatusDisplay.Label39.BackColor = If(InStr(CopilotStatusDisplay.Label39.Text, "true") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_CE_VehMdMngrSt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CE_VehMdMngrSt, 1)).SignalData
            CopilotStatusDisplay.Label52.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_CE_VehMdMngrSt, 1), MyDg.DeviceName(MyDg.CS_CE_VehMdMngrSt, 1), MyDg.VariableName(MyDg.CS_CE_VehMdMngrSt, 1))
            CopilotStatusDisplay.Label52.BackColor = If(InStr(CopilotStatusDisplay.Label52.Text, "Autonomous Supervised") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_AutoBrkSysRdcPerDet > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AutoBrkSysRdcPerDet, 1)).SignalData
            CopilotStatusDisplay.Label48.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AutoBrkSysRdcPerDet, 1), MyDg.DeviceName(MyDg.CS_AutoBrkSysRdcPerDet, 1), MyDg.VariableName(MyDg.CS_AutoBrkSysRdcPerDet, 1))
            CopilotStatusDisplay.Label48.BackColor = If(InStr(CopilotStatusDisplay.Label48.Text, "false") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_ADIMCntrlFailed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ADIMCntrlFailed, 1)).SignalData
            CopilotStatusDisplay.Label47.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ADIMCntrlFailed, 1), MyDg.DeviceName(MyDg.CS_ADIMCntrlFailed, 1), MyDg.VariableName(MyDg.CS_ADIMCntrlFailed, 1))
            CopilotStatusDisplay.Label47.BackColor = If(InStr(CopilotStatusDisplay.Label47.Text, "false") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_RedVehMdMngrSt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RedVehMdMngrSt, 1)).SignalData
            CopilotStatusDisplay.Label64.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RedVehMdMngrSt, 1), MyDg.DeviceName(MyDg.CS_RedVehMdMngrSt, 1), MyDg.VariableName(MyDg.CS_RedVehMdMngrSt, 1))
            CopilotStatusDisplay.Label64.BackColor = If(InStr(CopilotStatusDisplay.Label64.Text, "Autonomous Supervised") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PriCoPCmdMsgStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriCoPCmdMsgStat, 1)).SignalData
            CopilotStatusDisplay.Label23.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PriCoPCmdMsgStat, 1), MyDg.DeviceName(MyDg.CS_PriCoPCmdMsgStat, 1), MyDg.VariableName(MyDg.CS_PriCoPCmdMsgStat, 1))
            CopilotStatusDisplay.Label23.BackColor = If(InStr(CopilotStatusDisplay.Label23.Text, "Communication Normal") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_RedAutoBrkSysRdcPerDet > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RedAutoBrkSysRdcPerDet, 1)).SignalData
            CopilotStatusDisplay.Label60.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RedAutoBrkSysRdcPerDet, 1), MyDg.DeviceName(MyDg.CS_RedAutoBrkSysRdcPerDet, 1), MyDg.VariableName(MyDg.CS_RedAutoBrkSysRdcPerDet, 1))
            CopilotStatusDisplay.Label60.BackColor = If(InStr(CopilotStatusDisplay.Label60.Text, "false") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_RedADIMCntrlFailed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RedADIMCntrlFailed, 1)).SignalData
            CopilotStatusDisplay.Label59.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RedADIMCntrlFailed, 1), MyDg.DeviceName(MyDg.CS_RedADIMCntrlFailed, 1), MyDg.VariableName(MyDg.CS_RedADIMCntrlFailed, 1))
            CopilotStatusDisplay.Label59.BackColor = If(InStr(CopilotStatusDisplay.Label59.Text, "false") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_VeTSTR_e_HiThreatObjType > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VeTSTR_e_HiThreatObjType, 1)).SignalData
            CopilotStatusDisplay.Label58.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_VeTSTR_e_HiThreatObjType, 1), MyDg.DeviceName(MyDg.CS_VeTSTR_e_HiThreatObjType, 1), MyDg.VariableName(MyDg.CS_VeTSTR_e_HiThreatObjType, 1))
            CopilotStatusDisplay.Label58.BackColor = If(InStr(CopilotStatusDisplay.Label58.Text, "CeFSPR_e_4_Whl_Vhcl_car_sm_trk") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_VeTSTR_t_HiThreatTTC > 0 Then
            CopilotStatusDisplay.Label24.Text = Format(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VeTSTR_t_HiThreatTTC, 1)).SignalData, "0.00")
            CopilotStatusDisplay.Label24.BackColor = If(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VeTSTR_t_HiThreatTTC, 1)).SignalData < 10, Color.Green, Color.Red)
        End If
    End Sub

    Private Sub HandleLmfrStatusScreenHc(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles LMFR Status Screen display for High Content vehicles...

        Dim Mytempval As Double

        If MyDg.CS_HostLaneInx > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneInx, 1)).SignalData
            LmfrStatusScreenHc.Label20.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneInx, 1), MyDg.DeviceName(MyDg.CS_HostLaneInx, 1), MyDg.VariableName(MyDg.CS_HostLaneInx, 1))
        End If

        If MyDg.CS_RawLaneLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneLeft, 1)).SignalData
            LmfrStatusScreenHc.Label29.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneLeft, 1), MyDg.DeviceName(MyDg.CS_RawLaneLeft, 1), MyDg.VariableName(MyDg.CS_RawLaneLeft, 1))
            LmfrStatusScreenHc.Label29.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label29.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_RawLaneRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneRight, 1)).SignalData
            LmfrStatusScreenHc.Label30.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneRight, 1), MyDg.DeviceName(MyDg.CS_RawLaneRight, 1), MyDg.VariableName(MyDg.CS_RawLaneRight, 1))
            LmfrStatusScreenHc.Label30.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label30.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_AnchorSelect > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AnchorSelect, 1)).SignalData
            LmfrStatusScreenHc.Label9.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AnchorSelect, 1), MyDg.DeviceName(MyDg.CS_AnchorSelect, 1), MyDg.VariableName(MyDg.CS_AnchorSelect, 1))
            LmfrStatusScreenHc.Label9.BackColor = If(InStr(LmfrStatusScreenHc.Label9.Text, "CeLMFR_e_NoAnchor") = 0, Color.Green, Color.White)
        End If

        If MyDg.CS_LaneInvalid > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneInvalid, 1)).SignalData
            LmfrStatusScreenHc.Label54.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneInvalid, 1), MyDg.DeviceName(MyDg.CS_LaneInvalid, 1), MyDg.VariableName(MyDg.CS_LaneInvalid, 1))
            LmfrStatusScreenHc.Label54.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label54.Text), "TRUE") > 0, Color.Red, Color.White)
        End If

        If MyDg.CS_AlertUncertainLnLines > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertUncertainLnLines, 1)).SignalData
            LmfrStatusScreenHc.Label13.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertUncertainLnLines, 1), MyDg.DeviceName(MyDg.CS_AlertUncertainLnLines, 1), MyDg.VariableName(MyDg.CS_AlertUncertainLnLines, 1))
            LmfrStatusScreenHc.Label13.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label13.Text), "TRUE") > 0, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneWgtLt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtLt, 1)).SignalData
            LmfrStatusScreenHc.Label57.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtLt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtLt, 1), MyDg.VariableName(MyDg.CS_LaneWgtLt, 1))
            LmfrStatusScreenHc.Label57.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneWgtRt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtRt, 1)).SignalData
            LmfrStatusScreenHc.Label56.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtRt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtRt, 1), MyDg.VariableName(MyDg.CS_LaneWgtRt, 1))
            LmfrStatusScreenHc.Label56.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_HPP_Wgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HPP_Wgt, 1)).SignalData
            LmfrStatusScreenHc.Label55.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HPP_Wgt, 1), MyDg.DeviceName(MyDg.CS_HPP_Wgt, 1), MyDg.VariableName(MyDg.CS_HPP_Wgt, 1))
            LmfrStatusScreenHc.Label55.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_PrevCoefWgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PrevCoefWgt, 1)).SignalData
            LmfrStatusScreenHc.Label7.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PrevCoefWgt, 1), MyDg.DeviceName(MyDg.CS_PrevCoefWgt, 1), MyDg.VariableName(MyDg.CS_PrevCoefWgt, 1))
            LmfrStatusScreenHc.Label7.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_MapWgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_MapWgt, 1)).SignalData
            LmfrStatusScreenHc.Label5.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_MapWgt, 1), MyDg.DeviceName(MyDg.CS_MapWgt, 1), MyDg.VariableName(MyDg.CS_MapWgt, 1))
            LmfrStatusScreenHc.Label5.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_IFC_HeadingWgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_IFC_HeadingWgt, 1)).SignalData
            LmfrStatusScreenHc.Label14.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_IFC_HeadingWgt, 1), MyDg.DeviceName(MyDg.CS_IFC_HeadingWgt, 1), MyDg.VariableName(MyDg.CS_IFC_HeadingWgt, 1))
            LmfrStatusScreenHc.Label14.BackColor = If(Mytempval > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_IMU_BlueLine > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_IMU_BlueLine, 1)).SignalData
            LmfrStatusScreenHc.Label11.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_IMU_BlueLine, 1), MyDg.DeviceName(MyDg.CS_IMU_BlueLine, 1), MyDg.VariableName(MyDg.CS_IMU_BlueLine, 1))
            LmfrStatusScreenHc.Label29.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label11.Text), "TRUE") > 0, Color.Green, Color.White)
        End If
    End Sub

    Private Sub HandleLmfrStatusScreenGlobalA(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles LMFR Status Screen display for Global A (CSAV2) vehicles...

        Dim Mytempval As Double

        Dim y0 As Double = 0
        Dim y1 As Double = 0
        Dim y2 As Double = 0
        Dim y3 As Double = 0

        If Oscilloscope.Visible = True Then
            If MyDg.CS_CntrlPtLatOffsetHPP > 0 Then y0 = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CntrlPtLatOffsetHPP, 1)).SignalData
            If MyDg.CS_CntrlPtLatOffsetL > 0 Then y1 = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CntrlPtLatOffsetL, 1)).SignalData
            If MyDg.CS_CntrlPtLatOffsetR > 0 Then y2 = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CntrlPtLatOffsetR, 1)).SignalData
            If MyDg.CS_CntrlPtLatOffsetPrev > 0 Then y3 = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CntrlPtLatOffsetPrev, 1)).SignalData

            If Debugger.IsAttached Then

                y0 = Oscilloscope.GetRandom(-2.0, 2.0)
                y1 = Oscilloscope.GetRandom(-2.0, 2.0)
                y2 = Oscilloscope.GetRandom(-2.0, 2.0)
                y3 = Oscilloscope.GetRandom(-2.0, 2.0)

            End If

            Oscilloscope.Chart1.Series(0).Points.AddXY(_chartXIncrement, y0)
            Oscilloscope.Chart1.Series(1).Points.AddXY(_chartXIncrement, y1)
            Oscilloscope.Chart1.Series(2).Points.AddXY(_chartXIncrement, y2)
            Oscilloscope.Chart1.Series(3).Points.AddXY(_chartXIncrement, y3)

            If (Oscilloscope.Chart1.Series(0).Points.Count > 200) Then Oscilloscope.Chart1.Series(0).Points.RemoveAt(0)
            If (Oscilloscope.Chart1.Series(1).Points.Count > 200) Then Oscilloscope.Chart1.Series(1).Points.RemoveAt(0)
            If (Oscilloscope.Chart1.Series(2).Points.Count > 200) Then Oscilloscope.Chart1.Series(2).Points.RemoveAt(0)
            If (Oscilloscope.Chart1.Series(3).Points.Count > 200) Then Oscilloscope.Chart1.Series(3).Points.RemoveAt(0)

            Oscilloscope.Chart1.ChartAreas(0).AxisX.Minimum = Oscilloscope.Chart1.Series(0).Points(0).XValue
            Oscilloscope.Chart1.ChartAreas(0).AxisX.Maximum = _chartXIncrement
            _chartXIncrement += 0.05
        Else
            _chartXIncrement = 0
        End If

        If MyDg.CS_AtGradeAnchor > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AtGradeAnchor, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label31.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AtGradeAnchor, 1), MyDg.DeviceName(MyDg.CS_AtGradeAnchor, 1), MyDg.VariableName(MyDg.CS_AtGradeAnchor, 1))
        End If

        If MyDg.CS_HostLaneIndexLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneIndexLeft, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label24.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneIndexLeft, 1), MyDg.DeviceName(MyDg.CS_HostLaneIndexLeft, 1), MyDg.VariableName(MyDg.CS_HostLaneIndexLeft, 1))
        End If

        If MyDg.CS_HostLaneIndexRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneIndexRight, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label23.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneIndexRight, 1), MyDg.DeviceName(MyDg.CS_HostLaneIndexRight, 1), MyDg.VariableName(MyDg.CS_HostLaneIndexRight, 1))
        End If

        If MyDg.CS_MapHostLaneIndex > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_MapHostLaneIndex, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label22.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_MapHostLaneIndex, 1), MyDg.DeviceName(MyDg.CS_MapHostLaneIndex, 1), MyDg.VariableName(MyDg.CS_MapHostLaneIndex, 1))
        End If

        If MyDg.CS_TargetsHostLaneIndex > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_TargetsHostLaneIndex, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label21.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_TargetsHostLaneIndex, 1), MyDg.DeviceName(MyDg.CS_TargetsHostLaneIndex, 1), MyDg.VariableName(MyDg.CS_TargetsHostLaneIndex, 1))
        End If

        If MyDg.CS_DistToNextAtGradeXing > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNextAtGradeXing, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label49.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNextAtGradeXing, 1), MyDg.DeviceName(MyDg.CS_DistToNextAtGradeXing, 1), MyDg.VariableName(MyDg.CS_DistToNextAtGradeXing, 1))
        End If

        If MyDg.CS_DistToRoadClassTrans > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToRoadClassTrans, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label52.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToRoadClassTrans, 1), MyDg.DeviceName(MyDg.CS_DistToRoadClassTrans, 1), MyDg.VariableName(MyDg.CS_DistToRoadClassTrans, 1))
        End If

        If MyDg.CS_DistToNextTrfcCntrDev > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNextTrfcCntrDev, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label53.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNextTrfcCntrDev, 1), MyDg.DeviceName(MyDg.CS_DistToNextTrfcCntrDev, 1), MyDg.VariableName(MyDg.CS_DistToNextTrfcCntrDev, 1))
        End If

        If MyDg.CS_OnFreeway > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_OnFreeway, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label42.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_OnFreeway, 1), MyDg.DeviceName(MyDg.CS_OnFreeway, 1), MyDg.VariableName(MyDg.CS_OnFreeway, 1))
            LmfrStatusDisplayGlobalA.Label42.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label42.Text), "TRUE") > 0, Color.Green, Color.Red)
        End If

        If MyDg.CS_RoadClass_Crnt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RoadClass_Crnt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label41.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RoadClass_Crnt, 1), MyDg.DeviceName(MyDg.CS_RoadClass_Crnt, 1), MyDg.VariableName(MyDg.CS_RoadClass_Crnt, 1))
        End If

        If MyDg.CS_CmplxIntrsct_Prsnt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CmplxIntrsct_Prsnt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label43.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_CmplxIntrsct_Prsnt, 1), MyDg.DeviceName(MyDg.CS_CmplxIntrsct_Prsnt, 1), MyDg.VariableName(MyDg.CS_CmplxIntrsct_Prsnt, 1))
            LmfrStatusDisplayGlobalA.Label43.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label43.Text), "TRUE") > 0, Color.Red, Color.White)
        End If

        If MyDg.CS_HostLaneInx > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneInx, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label17.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneInx, 1), MyDg.DeviceName(MyDg.CS_HostLaneInx, 1), MyDg.VariableName(MyDg.CS_HostLaneInx, 1))
        End If


        If MyDg.CS_NumLanes > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NumLanes, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label18.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NumLanes, 1), MyDg.DeviceName(MyDg.CS_NumLanes, 1), MyDg.VariableName(MyDg.CS_NumLanes, 1))

        End If

        If MyDg.CS_NextNumLanes > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NextNumLanes, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label19.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NextNumLanes, 1), MyDg.DeviceName(MyDg.CS_NextNumLanes, 1), MyDg.VariableName(MyDg.CS_NextNumLanes, 1))

        End If

        If MyDg.CS_HostLaneProbMax > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneProbMax, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label20.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneProbMax, 1), MyDg.DeviceName(MyDg.CS_HostLaneProbMax, 1), MyDg.VariableName(MyDg.CS_HostLaneProbMax, 1))
        End If

        If MyDg.CS_RawLaneLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneLeft, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label29.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneLeft, 1), MyDg.DeviceName(MyDg.CS_RawLaneLeft, 1), MyDg.VariableName(MyDg.CS_RawLaneLeft, 1))
            LmfrStatusDisplayGlobalA.Label29.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label29.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_RawLaneRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneRight, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label30.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneRight, 1), MyDg.DeviceName(MyDg.CS_RawLaneRight, 1), MyDg.VariableName(MyDg.CS_RawLaneRight, 1))
            LmfrStatusDisplayGlobalA.Label30.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label30.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_IntersectSplitMerge > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_IntersectSplitMerge, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label47.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_IntersectSplitMerge, 1), MyDg.DeviceName(MyDg.CS_IntersectSplitMerge, 1), MyDg.VariableName(MyDg.CS_IntersectSplitMerge, 1))
        End If

        If MyDg.CS_DistToNumLanesTrans > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNumLanesTrans, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label50.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNumLanesTrans, 1), MyDg.DeviceName(MyDg.CS_DistToNumLanesTrans, 1), MyDg.VariableName(MyDg.CS_DistToNumLanesTrans, 1))
        End If

        If MyDg.CS_LCC_RedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LCC_RedReq, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label35.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LCC_RedReq, 1), MyDg.DeviceName(MyDg.CS_LCC_RedReq, 1), MyDg.VariableName(MyDg.CS_LCC_RedReq, 1))
            LmfrStatusDisplayGlobalA.Label35.BackColor = If(InStr(LmfrStatusDisplayGlobalA.Label35.Text, "CeVGCR_e_LCRR_Inactive") = 0, Color.Red, Color.White)
        End If

        If MyDg.CS_LnWgtRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnWgtRedReq, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label34.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnWgtRedReq, 1), MyDg.DeviceName(MyDg.CS_LnWgtRedReq, 1), MyDg.VariableName(MyDg.CS_LnWgtRedReq, 1))
            LmfrStatusDisplayGlobalA.Label34.BackColor = If(InStr(LmfrStatusDisplayGlobalA.Label34.Text, "CeVGCR_e_LCRR_Inactive") = 0, Color.Red, Color.White)
        End If

        If MyDg.CS_MapAvailRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_MapAvailRedReq, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label33.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_MapAvailRedReq, 1), MyDg.DeviceName(MyDg.CS_MapAvailRedReq, 1), MyDg.VariableName(MyDg.CS_MapAvailRedReq, 1))
            LmfrStatusDisplayGlobalA.Label33.BackColor = If(InStr(LmfrStatusDisplayGlobalA.Label33.Text, "CeVGCR_e_LCRR_Inactive") = 0, Color.Red, Color.White)
        End If

        If MyDg.CS_TmpLnRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_TmpLnRedReq, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label32.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_TmpLnRedReq, 1), MyDg.DeviceName(MyDg.CS_TmpLnRedReq, 1), MyDg.VariableName(MyDg.CS_TmpLnRedReq, 1))
            LmfrStatusDisplayGlobalA.Label32.BackColor = If(InStr(LmfrStatusDisplayGlobalA.Label32.Text, "CeVGCR_e_LCRR_Inactive") = 0, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneWgtLt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtLt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label57.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtLt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtLt, 1), MyDg.VariableName(MyDg.CS_LaneWgtLt, 1))
            LmfrStatusDisplayGlobalA.Label57.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneWgtRt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtRt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label56.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtRt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtRt, 1), MyDg.VariableName(MyDg.CS_LaneWgtRt, 1))
            LmfrStatusDisplayGlobalA.Label56.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_HPP_Wgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HPP_Wgt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label55.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HPP_Wgt, 1), MyDg.DeviceName(MyDg.CS_HPP_Wgt, 1), MyDg.VariableName(MyDg.CS_HPP_Wgt, 1))
            LmfrStatusDisplayGlobalA.Label55.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneInvalid > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneInvalid, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label54.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneInvalid, 1), MyDg.DeviceName(MyDg.CS_LaneInvalid, 1), MyDg.VariableName(MyDg.CS_LaneInvalid, 1))
            LmfrStatusDisplayGlobalA.Label54.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label56.Text), "TRUE") > 0, Color.Red, Color.White)
        End If

        If MyDg.CS_DistToNextIntersect > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNextIntersect, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label48.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNextIntersect, 1), MyDg.DeviceName(MyDg.CS_DistToNextIntersect, 1), MyDg.VariableName(MyDg.CS_DistToNextIntersect, 1))
        End If
    End Sub

    Private Sub HandleFusionStatusDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles Fusion Status Screen display...

        Dim Mytempval As Double

        If MyDg.CS_HostLaneInx > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneInx, 1)).SignalData
            FusionStatusDisplay.Label7.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneInx, 1), MyDg.DeviceName(MyDg.CS_HostLaneInx, 1), MyDg.VariableName(MyDg.CS_HostLaneInx, 1))
        End If


        If MyDg.CS_NumLanes > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NumLanes, 1)).SignalData
            FusionStatusDisplay.Label21.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NumLanes, 1), MyDg.DeviceName(MyDg.CS_NumLanes, 1), MyDg.VariableName(MyDg.CS_NumLanes, 1))

        End If

        If MyDg.CS_NextNumLanes > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NextNumLanes, 1)).SignalData
            FusionStatusDisplay.Label23.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NextNumLanes, 1), MyDg.DeviceName(MyDg.CS_NextNumLanes, 1), MyDg.VariableName(MyDg.CS_NextNumLanes, 1))

        End If

        If MyDg.CS_HostLaneProbMax > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneProbMax, 1)).SignalData
            FusionStatusDisplay.Label48.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneProbMax, 1), MyDg.DeviceName(MyDg.CS_HostLaneProbMax, 1), MyDg.VariableName(MyDg.CS_HostLaneProbMax, 1))
        End If

        If MyDg.CS_RawLaneLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneLeft, 1)).SignalData
            FusionStatusDisplay.Label32.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneLeft, 1), MyDg.DeviceName(MyDg.CS_RawLaneLeft, 1), MyDg.VariableName(MyDg.CS_RawLaneLeft, 1))
        End If

        If MyDg.CS_RawLaneRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneRight, 1)).SignalData
            FusionStatusDisplay.Label30.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneRight, 1), MyDg.DeviceName(MyDg.CS_RawLaneRight, 1), MyDg.VariableName(MyDg.CS_RawLaneRight, 1))
        End If

        If MyDg.CS_IntersectSplitMerge > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_IntersectSplitMerge, 1)).SignalData
            FusionStatusDisplay.Label15.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_IntersectSplitMerge, 1), MyDg.DeviceName(MyDg.CS_IntersectSplitMerge, 1), MyDg.VariableName(MyDg.CS_IntersectSplitMerge, 1))
        End If

        If MyDg.CS_DistToNumLanesTrans > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNumLanesTrans, 1)).SignalData
            FusionStatusDisplay.Label27.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNumLanesTrans, 1), MyDg.DeviceName(MyDg.CS_DistToNumLanesTrans, 1), MyDg.VariableName(MyDg.CS_DistToNumLanesTrans, 1))
        End If

        If MyDg.CS_LCC_RedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LCC_RedReq, 1)).SignalData
            FusionStatusDisplay.Label46.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LCC_RedReq, 1), MyDg.DeviceName(MyDg.CS_LCC_RedReq, 1), MyDg.VariableName(MyDg.CS_LCC_RedReq, 1))
        End If

        If MyDg.CS_LnWgtRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnWgtRedReq, 1)).SignalData
            FusionStatusDisplay.Label42.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnWgtRedReq, 1), MyDg.DeviceName(MyDg.CS_LnWgtRedReq, 1), MyDg.VariableName(MyDg.CS_LnWgtRedReq, 1))

        End If

        If MyDg.CS_MapAvailRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_MapAvailRedReq, 1)).SignalData
            FusionStatusDisplay.Label40.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_MapAvailRedReq, 1), MyDg.DeviceName(MyDg.CS_MapAvailRedReq, 1), MyDg.VariableName(MyDg.CS_MapAvailRedReq, 1))
        End If

        If MyDg.CS_TmpLnRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_TmpLnRedReq, 1)).SignalData
            FusionStatusDisplay.Label36.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_TmpLnRedReq, 1), MyDg.DeviceName(MyDg.CS_TmpLnRedReq, 1), MyDg.VariableName(MyDg.CS_TmpLnRedReq, 1))
        End If

        If MyDg.CS_LaneWgtLt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtLt, 1)).SignalData
            FusionStatusDisplay.Label54.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtLt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtLt, 1), MyDg.VariableName(MyDg.CS_LaneWgtLt, 1))
        End If

        If MyDg.CS_LaneWgtRt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtRt, 1)).SignalData
            FusionStatusDisplay.Label52.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtRt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtRt, 1), MyDg.VariableName(MyDg.CS_LaneWgtRt, 1))
        End If

        If MyDg.CS_HPP_Wgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HPP_Wgt, 1)).SignalData
            FusionStatusDisplay.Label50.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HPP_Wgt, 1), MyDg.DeviceName(MyDg.CS_HPP_Wgt, 1), MyDg.VariableName(MyDg.CS_HPP_Wgt, 1))
        End If

        If MyDg.CS_LaneInvalid > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneInvalid, 1)).SignalData
            FusionStatusDisplay.Label56.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneInvalid, 1), MyDg.DeviceName(MyDg.CS_LaneInvalid, 1), MyDg.VariableName(MyDg.CS_LaneInvalid, 1))
        End If

        If MyDg.CS_DistToNextIntersect > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNextIntersect, 1)).SignalData
            FusionStatusDisplay.Label17.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNextIntersect, 1), MyDg.DeviceName(MyDg.CS_DistToNextIntersect, 1), MyDg.VariableName(MyDg.CS_DistToNextIntersect, 1))
        End If

        If MyDg.CS_HCURVE > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HCURVE, 1)).SignalData
            FusionStatusDisplay.Label9.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HCURVE, 1), MyDg.DeviceName(MyDg.CS_HCURVE, 1), MyDg.VariableName(MyDg.CS_HCURVE, 1))
        End If

        If MyDg.CS_VehPathEstCurv > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VehPathEstCurv, 1)).SignalData
            FusionStatusDisplay.Label11.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_VehPathEstCurv, 1), MyDg.DeviceName(MyDg.CS_VehPathEstCurv, 1), MyDg.VariableName(MyDg.CS_VehPathEstCurv, 1))
        End If

        If MyDg.CS_Curvature > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_Curvature, 1)).SignalData
            FusionStatusDisplay.Label13.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_Curvature, 1), MyDg.DeviceName(MyDg.CS_Curvature, 1), MyDg.VariableName(MyDg.CS_Curvature, 1))
        End If

        If MyDg.CS_SpltMrgLaneNum > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SpltMrgLaneNum, 1)).SignalData
            FusionStatusDisplay.Label19.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_SpltMrgLaneNum, 1), MyDg.DeviceName(MyDg.CS_SpltMrgLaneNum, 1), MyDg.VariableName(MyDg.CS_SpltMrgLaneNum, 1))
        End If

        If MyDg.CS__NumLanesTrans > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__NumLanesTrans, 1)).SignalData
            FusionStatusDisplay.Label25.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__NumLanesTrans, 1), MyDg.DeviceName(MyDg.CS__NumLanesTrans, 1), MyDg.VariableName(MyDg.CS__NumLanesTrans, 1))
        End If

        If MyDg.CS_PathConf > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PathConf, 1)).SignalData
            FusionStatusDisplay.Label8.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PathConf, 1), MyDg.DeviceName(MyDg.CS_PathConf, 1), MyDg.VariableName(MyDg.CS_PathConf, 1))
        End If

        If MyDg.CS_LnSplitRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnSplitRedReq, 1)).SignalData
            FusionStatusDisplay.Label44.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnSplitRedReq, 1), MyDg.DeviceName(MyDg.CS_LnSplitRedReq, 1), MyDg.VariableName(MyDg.CS_LnSplitRedReq, 1))
        End If

        If MyDg.CS_PathConfRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PathConfRedReq, 1)).SignalData
            FusionStatusDisplay.Label38.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PathConfRedReq, 1), MyDg.DeviceName(MyDg.CS_PathConfRedReq, 1), MyDg.VariableName(MyDg.CS_PathConfRedReq, 1))
        End If

        If MyDg.CS_VPMConfRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VPMConfRedReq, 1)).SignalData
            FusionStatusDisplay.Label34.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_VPMConfRedReq, 1), MyDg.DeviceName(MyDg.CS_VPMConfRedReq, 1), MyDg.VariableName(MyDg.CS_VPMConfRedReq, 1))
        End If

        If MyDg.CS_AlertUncertainLnLines > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertUncertainLnLines, 1)).SignalData
            FusionStatusDisplay.Label59.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertUncertainLnLines, 1), MyDg.DeviceName(MyDg.CS_AlertUncertainLnLines, 1), MyDg.VariableName(MyDg.CS_AlertUncertainLnLines, 1))
        End If

        If MyDg.CS_AlertExitLane > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertExitLane, 1)).SignalData
            FusionStatusDisplay.Label61.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertExitLane, 1), MyDg.DeviceName(MyDg.CS_AlertExitLane, 1), MyDg.VariableName(MyDg.CS_AlertExitLane, 1))
        End If

        If MyDg.CS_AlertLaneEnding > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertLaneEnding, 1)).SignalData
            FusionStatusDisplay.Label63.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertLaneEnding, 1), MyDg.DeviceName(MyDg.CS_AlertLaneEnding, 1), MyDg.VariableName(MyDg.CS_AlertLaneEnding, 1))
        End If

        If MyDg.CS_AlertMapUnavail > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertMapUnavail, 1)).SignalData
            FusionStatusDisplay.Label65.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertMapUnavail, 1), MyDg.DeviceName(MyDg.CS_AlertMapUnavail, 1), MyDg.VariableName(MyDg.CS_AlertMapUnavail, 1))
        End If

        If MyDg.CS_AlertOther > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertOther, 1)).SignalData
            FusionStatusDisplay.Label67.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertOther, 1), MyDg.DeviceName(MyDg.CS_AlertOther, 1), MyDg.VariableName(MyDg.CS_AlertOther, 1))
        End If

        If MyDg.CS_LnNarrowing > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnNarrowing, 1)).SignalData
            FusionStatusDisplay.Label69.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnNarrowing, 1), MyDg.DeviceName(MyDg.CS_LnNarrowing, 1), MyDg.VariableName(MyDg.CS_LnNarrowing, 1))
        End If

        If MyDg.CS_LnWidening > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnWidening, 1)).SignalData
            FusionStatusDisplay.Label71.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnWidening, 1), MyDg.DeviceName(MyDg.CS_LnWidening, 1), MyDg.VariableName(MyDg.CS_LnWidening, 1))
        End If

        If MyDg.CS_ObjectLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ObjectLeft, 1)).SignalData
            FusionStatusDisplay.Label75.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ObjectLeft, 1), MyDg.DeviceName(MyDg.CS_ObjectLeft, 1), MyDg.VariableName(MyDg.CS_ObjectLeft, 1))
            FusionStatusDisplay.Label75.BackColor = If(InStr(UCase(FusionStatusDisplay.Label75.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_ObjectRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ObjectRight, 1)).SignalData
            FusionStatusDisplay.Label73.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ObjectRight, 1), MyDg.DeviceName(MyDg.CS_ObjectRight, 1), MyDg.VariableName(MyDg.CS_ObjectRight, 1))
            FusionStatusDisplay.Label73.BackColor = If(InStr(UCase(FusionStatusDisplay.Label73.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

    End Sub

    Private Sub HandleLkaCustomDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles LKA Status Screen display...


        If MyDg.CS_LNSNS_DISTTOLLNEDGE > 0 And MyDg.CS_LNSNS_DISTTORLNEDGE > 0 Then

            _saveLkaDistRtLnEdge = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LNSNS_DISTTORLNEDGE, 1)).SignalData
            _saveLkaDistLtLnEdge = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LNSNS_DISTTOLLNEDGE, 1)).SignalData

        End If

        If LkaForm.LkaActive = True Then
            LkaForm.Label17.Text = "LKA TORQ"
            LkaForm.Label17.BackColor = Color.Green
        Else
            LkaForm.Label17.Text = "NO LKA TORQ"
            LkaForm.Label17.BackColor = Color.Red
        End If

        If MyDg.CS_LKA_OFFIND > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_OFFIND, 1)).SignalData = 0 Then
                LkaForm.Label20.Text = "LKA ON"
                LkaForm.Label20.BackColor = Color.Green
            Else
                LkaForm.Label20.Text = "LKA OFF"
                LkaForm.Label20.BackColor = Color.Red
            End If

        End If

        If MyDg.CS_LKA_STDBYIND > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_STDBYIND, 1)).SignalData > 0 Then
                LkaForm.Label19.Text = "LKA IN STDBY"
                LkaForm.Label19.BackColor = Color.Green
            Else
                LkaForm.Label19.Text = "LKA STDBY OFF"
                LkaForm.Label19.BackColor = Color.Red
            End If

        End If

        If MyDg.CS_LKA_ACTVIND > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_ACTVIND, 1)).SignalData > 0 Then
                LkaForm.Label18.Text = "LKA ACTV"
                LkaForm.Label18.BackColor = Color.Green
            Else
                LkaForm.Label18.Text = "LKA NOT ACTV"
                LkaForm.Label18.BackColor = Color.Red
            End If

        End If

        If MyDg.CS_LKA_DRVRAPPLDTRQ > 0 And MyDg.CS_LKA_TORQUE > 0 And MyDg.CS_LKA_TRQRQACT > 0 Then
            If LkaForm.LkaActive = False Then
                If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TRQRQACT, 1)).SignalData > 0 And Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData) >= 0.5 Then
                    LkaForm.LkaActiveTransitionOn = True
                    LkaForm.LkaActive = True
                    LkaForm.LkaInitDistRtLnEdge = _saveLkaDistRtLnEdge
                    LkaForm.LkaInitDistLtLnEdge = _saveLkaDistLtLnEdge
                    LkaForm.Label1.Text = Format(LkaForm.LkaInitDistRtLnEdge, "0.000")
                    LkaForm.Label3.Text = Format(LkaForm.LkaInitDistLtLnEdge, "0.000")

                    LkaForm.LkaMinDistRtLnEdge = LkaForm.LkaInitDistRtLnEdge
                    LkaForm.LkaMinDistLtLnEdge = LkaForm.LkaInitDistLtLnEdge
                    LkaForm.Label8.Text = Format(LkaForm.LkaMinDistRtLnEdge, "0.000")
                    LkaForm.Label6.Text = Format(LkaForm.LkaMinDistLtLnEdge, "0.000")

                    'Left positive, right negative

                    If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData > 0 Then
                        LkaForm.LkaMaxDrvrTorqueLeft = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData
                        LkaForm.LkaMaxDrvrTorqueRight = 0
                    ElseIf MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData < 0 Then
                        LkaForm.LkaMaxDrvrTorqueRight = Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData)
                        LkaForm.LkaMaxDrvrTorqueLeft = 0
                    Else
                        LkaForm.LkaMaxDrvrTorqueRight = 0
                        LkaForm.LkaMaxDrvrTorqueLeft = 0
                    End If

                    LkaForm.Label14.Text = Format(LkaForm.LkaMaxDrvrTorqueRight, "0.000")
                    LkaForm.Label16.Text = Format(LkaForm.LkaMaxDrvrTorqueLeft, "0.000")


                    If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData > 0 Then
                        LkaForm.LkaMaxLkaTorqueLeft = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData
                        LkaForm.LkaMaxLkaTorqueRight = 0
                    ElseIf MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData < 0 Then
                        LkaForm.LkaMaxLkaTorqueRight = Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData)
                        LkaForm.LkaMaxLkaTorqueLeft = 0
                    Else
                        LkaForm.LkaMaxLkaTorqueRight = 0
                        LkaForm.LkaMaxLkaTorqueLeft = 0
                    End If

                    LkaForm.Label10.Text = Format(LkaForm.LkaMaxLkaTorqueRight, "0.000")
                    LkaForm.Label12.Text = Format(LkaForm.LkaMaxLkaTorqueLeft, "0.000")

                End If

            Else 'LKA Active = True

                If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TRQRQACT, 1)).SignalData = 0 Or Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData) < 0.5 Then
                    LkaForm.LkaActive = False
                    LkaForm.LkaActiveTransitionOff = True

                Else

                    If _saveLkaDistRtLnEdge < LkaForm.LkaMinDistRtLnEdge Then
                        LkaForm.LkaMinDistRtLnEdge = _saveLkaDistRtLnEdge
                        LkaForm.Label8.Text = Format(LkaForm.LkaMinDistRtLnEdge, "0.000")
                    End If
                    If _saveLkaDistLtLnEdge < LkaForm.LkaMinDistLtLnEdge Then
                        LkaForm.LkaMinDistLtLnEdge = _saveLkaDistLtLnEdge
                        LkaForm.Label6.Text = Format(LkaForm.LkaMinDistLtLnEdge, "0.000")
                    End If

                    If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData > LkaForm.LkaMaxDrvrTorqueLeft Then
                        LkaForm.LkaMaxDrvrTorqueLeft = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData
                        LkaForm.Label16.Text = Format(LkaForm.LkaMaxDrvrTorqueLeft, "0.000")
                    End If

                    If Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData) > LkaForm.LkaMaxDrvrTorqueRight Then
                        LkaForm.LkaMaxDrvrTorqueRight = Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData)
                        LkaForm.Label14.Text = Format(LkaForm.LkaMaxDrvrTorqueRight, "0.000")
                    End If

                    If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData > LkaForm.LkaMaxLkaTorqueLeft Then
                        LkaForm.LkaMaxLkaTorqueLeft = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData
                        LkaForm.Label12.Text = Format(LkaForm.LkaMaxLkaTorqueLeft, "0.000")
                    End If

                    If Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData) > LkaForm.LkaMaxLkaTorqueRight Then
                        LkaForm.LkaMaxLkaTorqueRight = Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData)
                        LkaForm.Label10.Text = Format(LkaForm.LkaMaxLkaTorqueRight, "0.000")
                    End If

                    LkaForm.LkaActiveTransitionOn = False

                End If
            End If
        End If

    End Sub

    Private Sub HandleTargetStatusDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles Target Status Screen display (Secret Squirrel Screen)...

        Dim foaiVair As Integer
        Dim foaiAwir As Integer
        Dim colprsysbrkprfreq As Integer

        If MyDg.CS_FSRACC_ENGAGED > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FSRACC_ENGAGED, 1)).SignalData <> 0 Then
                TargetStatusDisplay.Label9.BackColor = Color.Green
                TargetStatusDisplay.Label9.ForeColor = Color.Black
            Else
                TargetStatusDisplay.Label9.BackColor = Color.Black
                TargetStatusDisplay.Label9.ForeColor = Color.White
            End If
        End If

        If MyDg.CS_FSRACC_BRAKE_ACTIVE > 0 Then
            TargetStatusDisplay.Label10.BackColor = If(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FSRACC_BRAKE_ACTIVE, 1)).SignalData <> 0, Color.Red, Color.Black)
        End If

        If MyDg.CS_FSRACC_ACCEL_ACTIVE > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FSRACC_ACCEL_ACTIVE, 1)).SignalData <> 0 Then
                TargetStatusDisplay.Label11.BackColor = Color.Green
                TargetStatusDisplay.Label11.ForeColor = Color.Black
            Else
                TargetStatusDisplay.Label11.BackColor = Color.Black
                TargetStatusDisplay.Label11.ForeColor = Color.White
            End If
        End If

        If MyDg.CS_CPSTOS_TTC > 0 Then 'And TargetStatusDisplay.Visible = True Then
            TargetStatusDisplay.Label1.Text = Format$(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CPSTOS_TTC, 1)).SignalData, "0.00")
        End If

        If MyDg.CS_ACC_GAPSETTING > 0 Then 'And TargetStatusDisplay.Visible = True Then
            TargetStatusDisplay.Label4.Text = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ACC_GAPSETTING, 1)).SignalData
        End If

        If MyDg.CS_FOAI_VAIR > 0 And MyDg.CS_FOAI_AWIR > 0 Then 'And TargetStatusDisplay.Visible = True Then

            foaiVair = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FOAI_VAIR, 1)).SignalData)
            foaiAwir = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FOAI_AWIR, 1)).SignalData)

            If foaiVair <> _saveFoaiVairValue Then
                If foaiAwir <> 2 And foaiAwir <> 3 Then
                    Select Case foaiVair
                        Case 0
                            TargetStatusDisplay.PictureBox1.Visible = False
                            TargetStatusDisplay.PictureBox2.Visible = False
                            TargetStatusDisplay.PictureBox3.Visible = False
                        Case 1
                            TargetStatusDisplay.PictureBox1.Visible = True
                            TargetStatusDisplay.PictureBox2.Visible = False
                            TargetStatusDisplay.PictureBox3.Visible = False
                            TargetStatusDisplay.ListBox2.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Vehicle Ahead")
                    End Select
                End If
            End If

            If foaiAwir <> _saveFoaiAwirValue Then
                Select Case foaiAwir
                    Case 2
                        TargetStatusDisplay.PictureBox1.Visible = False
                        TargetStatusDisplay.PictureBox2.Visible = True
                        TargetStatusDisplay.PictureBox3.Visible = False
                        TargetStatusDisplay.ListBox2.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Tailgate")
                    Case 3
                        TargetStatusDisplay.PictureBox1.Visible = False
                        TargetStatusDisplay.PictureBox2.Visible = False
                        TargetStatusDisplay.PictureBox3.Visible = True
                        TargetStatusDisplay.ListBox2.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Imminent")
                    Case Else
                        Select Case foaiVair
                            Case 0
                                TargetStatusDisplay.PictureBox1.Visible = False
                                TargetStatusDisplay.PictureBox2.Visible = False
                                TargetStatusDisplay.PictureBox3.Visible = False
                            Case 1
                                TargetStatusDisplay.PictureBox1.Visible = True
                                TargetStatusDisplay.PictureBox2.Visible = False
                                TargetStatusDisplay.PictureBox3.Visible = False
                        End Select
                End Select
            End If

            _saveFoaiAwirValue = foaiAwir
            _saveFoaiVairValue = foaiVair
        End If

        If MyDg.CS_AUTOBRKREQ > 0 And MyDg.CS_AUTOBRKREQTYPE > 0 And MyDg.CS_CPSCBSC_CTRLACC > 0 Then
            If CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AUTOBRKREQTYPE, 1)).SignalData) > 0 And
                        CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AUTOBRKREQTYPE, 1)).SignalData) < 5 And
                        CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AUTOBRKREQ, 1)).SignalData) > 0 And
                        _saveAutobrkreqValue = 0 And
                        CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CPSCBSC_CTRLACC, 1)).SignalData) < 0 Then

                TargetStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " CPS Event")
                _saveAutobrkreqValue = 1
            Else
                _saveAutobrkreqValue = 0
            End If
        End If

        If MyDg.CS_COLPRSYSBRKPRFREQ > 0 Then
            colprsysbrkprfreq = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_COLPRSYSBRKPRFREQ, 1)).SignalData)
            If colprsysbrkprfreq = 1 And _saveColprsysbrkprfreq_Target = 0 Then
                TargetStatusDisplay.Label12.BackColor = Color.Yellow
                TargetStatusDisplay.Label12.ForeColor = Color.Black
                TargetStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " PREFILL Event")
            ElseIf colprsysbrkprfreq = 0 Then
                TargetStatusDisplay.Label12.BackColor = Color.Black
                TargetStatusDisplay.Label12.ForeColor = Color.White
            ElseIf colprsysbrkprfreq = 1 And _saveColprsysbrkprfreq_Target = 1 Then
                TargetStatusDisplay.Label12.BackColor = Color.Yellow
                TargetStatusDisplay.Label12.ForeColor = Color.Black
            End If
            _saveColprsysbrkprfreq_Target = colprsysbrkprfreq
        End If

        If MyDg.CS_CPSTOS_X_VEL > 0 And MyDg.CS_CPSTOS_Y_VEL > 0 Then
            TargetStatusDisplay.Label3.Text = Format$(Math.Sqrt((MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CPSTOS_X_VEL, 1)).SignalData ^ 2) + (MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CPSTOS_Y_VEL, 1)).SignalData ^ 2)), "0.00")
        End If

    End Sub

    Private Sub HandlePedestrianStatusDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles Pedestrian Status Screen display...

        Dim colprsysbrkprfreq As Integer
        Dim pedwarn As Integer
        Dim brakingFlag As Integer
        Dim alertFlag As Integer
        Dim notificationFlag As Integer

        If MyDg.CS_PEDWARN > 0 Then
            pedwarn = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PEDWARN, 1)).SignalData)
            If pedwarn <> _savePedwarn Then
                Select Case pedwarn
                    Case 0
                        PedestrianStatusDisplay.PictureBox3.Visible = True
                        PedestrianStatusDisplay.PictureBox1.Visible = False
                        PedestrianStatusDisplay.PictureBox2.Visible = False
                    Case 1
                        PedestrianStatusDisplay.PictureBox3.Visible = False
                        PedestrianStatusDisplay.PictureBox1.Visible = False
                        PedestrianStatusDisplay.PictureBox2.Visible = True
                        PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Ped Warn DETECT")
                    Case 2
                        PedestrianStatusDisplay.PictureBox3.Visible = False
                        PedestrianStatusDisplay.PictureBox1.Visible = True
                        PedestrianStatusDisplay.PictureBox2.Visible = False
                        PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Ped Warn ALERT")
                End Select
                _savePedwarn = pedwarn
            End If
        End If

        If MyDg.CS_BRAKING_FLAG > 0 Then
            brakingFlag = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_BRAKING_FLAG, 1)).SignalData)
            If brakingFlag = 10 And _saveBrakingFlag <> 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Red
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " BRAKING Flag")
                HandleUserMessageLogging("GMRC", "HandlePedestrianStatusDisplay: BRAKING FLAG Set")
            ElseIf brakingFlag <> 10 And _saveBrakingFlag = 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Black
                HandleUserMessageLogging("GMRC", "HandlePedestrianStatusDisplay: BRAKING FLAG Reset")
            ElseIf brakingFlag <> 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Black
            ElseIf brakingFlag = 10 And _saveBrakingFlag = 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Red
            End If
            _saveBrakingFlag = brakingFlag
        End If

        If MyDg.CS_ALERT_FLAG > 0 Then
            alertFlag = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ALERT_FLAG, 1)).SignalData)
            If alertFlag = 1 And _saveAlertFlag = 0 Then
                PedestrianStatusDisplay.Label2.BackColor = Color.Red
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " ALERT Flag")
            ElseIf alertFlag = 0 Then
                PedestrianStatusDisplay.Label2.BackColor = Color.Black
            ElseIf alertFlag = 1 And _saveAlertFlag = 1 Then
                PedestrianStatusDisplay.Label2.BackColor = Color.Red
            End If
            _saveAlertFlag = alertFlag
        End If

        If MyDg.CS_NOTIFICATION_FLAG > 0 Then
            notificationFlag = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NOTIFICATION_FLAG, 1)).SignalData)
            If notificationFlag = 1 And _saveNotificationFlag = 0 Then
                PedestrianStatusDisplay.Label3.BackColor = Color.Red
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " NOTIFICATION Flag")
            ElseIf notificationFlag = 0 Then
                PedestrianStatusDisplay.Label3.BackColor = Color.Black
            ElseIf notificationFlag = 1 And _saveNotificationFlag = 1 Then
                PedestrianStatusDisplay.Label3.BackColor = Color.Red
            End If
            _saveNotificationFlag = notificationFlag
        End If

        If MyDg.CS_COLPRSYSBRKPRFREQ > 0 Then
            colprsysbrkprfreq = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_COLPRSYSBRKPRFREQ, 1)).SignalData)
            If colprsysbrkprfreq = 1 And _saveColprsysbrkprfreq_Ped = 0 Then
                PedestrianStatusDisplay.Label4.BackColor = Color.Yellow
                PedestrianStatusDisplay.Label4.ForeColor = Color.Black
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " PREFILL Event")
            ElseIf colprsysbrkprfreq = 0 Then
                PedestrianStatusDisplay.Label4.BackColor = Color.Black
                PedestrianStatusDisplay.Label4.ForeColor = Color.White
            ElseIf colprsysbrkprfreq = 1 And _saveColprsysbrkprfreq_Ped = 1 Then
                PedestrianStatusDisplay.Label4.BackColor = Color.Yellow
                PedestrianStatusDisplay.Label4.ForeColor = Color.Black
            End If
            _saveColprsysbrkprfreq_Ped = colprsysbrkprfreq
        End If
    End Sub
    ''' <summary>
    ''' Clears data-frozen / lost-device tracking so INCA COMM and frozen-data detection start fresh
    ''' when measurement or recording begins, rather than carrying over state from before the start.
    ''' </summary>
    Private Sub ResetDataFrozenTracking(ByVal reason As String)
        Try
            For i = 0 To myDGs.Count - 1
                If myDGs(i) Is Nothing Then Continue For

                ' Only iterate data rows (x >= 1) because UpdateGridColor uses Rows(x-1)
                Dim lastRow As Integer = Math.Max(1, myDGs(i).RowCount - 1)
                For x = 1 To lastRow
                    For y = 0 To myDGs(i).ColumnCount - 1
                        Dim wasFrozen As Boolean = myDGs(i).DataFrozen(x, y)
                        myDGs(i).DataFrozenCounter(x, y) = 0
                        myDGs(i).DataFrozen(x, y) = False
                        If wasFrozen Then UpdateGridColor(i, x, y, GridUpdateActions.FromLow)
                    Next y
                Next x
            Next i

            _lostDevice = False
            VideoCameraNotUpdating = False
            BackgroundLoopCounterNotUpdating = False
            _MyCpuStaleData = False
            SaveDataFrozenDeviceName = ""
            SaveVideoFrozenDeviceName = ""
            SaveLostDeviceName = ""

            HandleUserMessageLogging("GMRC", "ResetDataFrozenTracking: " & reason & " - data-frozen/lost-device tracking reset")
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ResetDataFrozenTracking: " & ex.Message)
        End Try
    End Sub

    Private Sub HandleNonMeasureModeUpdates(ByVal saveOdoReading As Double, ByRef saveWhereAmIAt As String, ByRef goNoGoFault() As Boolean)
        ' Called from MyBackgroundTasks
        ' Handles updates when transitioning out of measure mode...

        ' Reset location to undefined
        saveWhereAmIAt = "Undefined"

        ' UI updates
        If OnVehicleScreen.Label1.BackColor <> Color.Gray Then
            OnVehicleScreen.Label1.BackColor = Color.Gray
        End If

        If OnVehicleScreen.Label1.Text <> "" Then
            OnVehicleScreen.Label1.Text = ""
        End If

        ' Update Go/NoGo status indicators to gray and reset per-grid state
        For z = 0 To MyDFs.Count - 1
            If MyDFs(z) Is Nothing Then Continue For
            If MyDFs(z).GoNoGoIndex > -1 Then
                ' Gray the banner if needed and clear fault bit
                If MyLabel IsNot Nothing AndAlso MyDFs(z).GoNoGoIndex <= UBound(MyLabel) Then
                    If MyLabel(MyDFs(z).GoNoGoIndex).BackColor <> Color.Gray Then
                        UpdateGonogoLabelColor(MyDFs(z).GoNoGoIndex, Color.Gray)
                    End If
                End If
                goNoGoFault(MyDFs(z).GoNoGoIndex) = False

                ' Reset all grids under this form
                For i = 0 To myDGs.Count - 1
                    If myDGs(i) Is Nothing Then Continue For
                    If myDGs(i).ParentFormIndex <> z Then Continue For

                    ' Only iterate data rows (x >= 1) because UpdateGridColor uses Rows(x-1)
                    Dim lastRow As Integer = Math.Max(1, myDGs(i).RowCount - 1)
                    For x = 1 To lastRow
                        For y = 0 To myDGs(i).ColumnCount - 1
                            myDGs(i).DataFrozenCounter(x, y) = 0
                            myDGs(i).DataFrozen(x, y) = False

                            If myDGs(i).CurrentBackColor(x, y) <> myDGs(i).DefaultCellBackColor(x, y) _
                       OrElse myDGs(i).CurrentForeColor(x, y) <> myDGs(i).DefaultCellForeColor(x, y) Then
                                UpdateGridColor(i, x, y, GridUpdateActions.FromLow)
                            End If
                        Next y
                    Next x
                Next i
            End If
        Next z

        VideoCameraNotUpdating = False
        BackgroundLoopCounterNotUpdating = False

        ' Ensure UI is visible and handle MainTabControl visibility
        If WindowState <> FormWindowState.Minimized Then
            If OnVehicleScreen.Button1.Visible = False Then
                OnVehicleScreen.Button1.Visible = True
            End If

            ' Ensure MainTabControl is visible
            If MyMainTabControl IsNot Nothing AndAlso Not MyMainTabControl.Visible Then
                MyMainTabControl.Visible = True
                HandleUserMessageLogging("GMRC", "HandleNonMeasureModeUpdates: Set MyMainTabControl to visible.")
            End If

            ' Clear Distance to Target label
            If Len(OnVehicleScreen.Label17.Text) > 0 Then
                OnVehicleScreen.Label17.Text = ""
            End If
        End If
    End Sub

    'Private Async Sub MyBackgroundTasks()
    Private Async Function MyBackgroundTasks() As Task
        'This routine handles all the background tasks for the GmResidentClient.  It is responsible for making calls to
        'GM_INCA_Comm (Via the INCA_InterfaceClass) to get data from INCA and display it, it also handles updating timers, counters, display colors
        'status information etc. for the main display as well as for the various dynamically created forms, custom displays and grids.

        Dim oneShot As Boolean
        Dim whereAmI As String = "undefined"
        Dim saveWhereAmIAt As String = "Undefined"
        Dim i, j, k As Integer
        Dim MySaveTime As DateTime
        Dim MyElapseTime As TimeSpan
        Dim MySaveTime2 As DateTime
        Dim MyElapseTime2 As TimeSpan
        Dim MyMeasureTime As TimeSpan
        Dim MySaveMeasureTime As DateTime
        Dim noOfRecords As Integer
        Dim MyRci2Data As IGM_INCA_Comm.INCAData
        Dim lineNum As Integer
        Dim liveDataElapseTime As TimeSpan
        Dim saveLiveDataTime As DateTime
        Dim lTotalnumsignalsdisplayed As Integer
        Dim lMeasurementStatus As String
        Dim lRecordingState As Boolean
        Dim lSaveRecordingState As Boolean
        Dim goNoGoFault() As Boolean
        Dim retries As Integer = 0
        Dim saveOdoReading As Double = 0.0
        Dim logMsgWritten As Boolean

        Dim MySaveTime3 As DateTime
        Dim MyElapseTime3 As TimeSpan

        Const dataFrozenMaxCountMeasurementMode As Integer = 100
        Const dataFrozenMaxCountRecordMode As Integer = 200

        Dim dataFrozenMaxCount As Integer = dataFrozenMaxCountRecordMode

        Const lostDeviceDelayTime As Integer = 20 'seconds

        ' ✅ NEW: Disk space check timer
        Dim MyDiskSpaceCheckTime As DateTime = DateTime.Now
        Const DiskSpaceCheckIntervalMinutes As Integer = 5

        Try

            If Debugger.IsAttached Then
                OnVehicleScreen.CheckBox1.Visible = True 'Enable DTC Debug Checkbox on main form...
            End If

            ReDim goNoGoFault(UBound(MyLabel))

            MySaveTime = DateTime.Now
            MySaveTime2 = DateTime.Now
            noOfRecords = 1
            lineNum = -1
            lMeasurementStatus = ""
            lRecordingState = False

            While _enableMyBackgroundTasks AndAlso Not exitInProgress

                whereAmI = "Start of Loop"

                ' ════════════════════════════════════════════════════════════
                ' ✅ NEW: DISK SPACE CHECK (Every 5 minutes)
                ' ════════════════════════════════════════════════════════════
                Dim diskCheckElapsed = DateTime.Now.Subtract(MyDiskSpaceCheckTime)
                If diskCheckElapsed.TotalMinutes >= DiskSpaceCheckIntervalMinutes Then
                    MyDiskSpaceCheckTime = DateTime.Now

                    ' Check disk space on recording path
                    Dim pathToCheck As String = FinalPathToSaveData
                    If Not String.IsNullOrEmpty(pathToCheck) Then
                        CheckDiskSpace(pathToCheck,
                                       minimumSpaceGB:=10.0,
                                       showDialog:=False,
                                       allowOverride:=True)
                    End If
                End If

                If INCACommCheckStopWatch IsNot Nothing Then
                    INCACommCheckStopWatch.Reset()
                    INCACommCheckStopWatch.Start()
                    INCACommCheckWarningTime = Val(APICommErrorMsgDelayTime)
                End If

                'SwitchToMain flag is used in conjunction with INCA Status window (see ProcessKiller)
                ' ✅ FIX: Don't switch forms when LoginForm is active
                If _switchToMain = True AndAlso Not OnLoginScreen Then
                    _switchToMain = False
                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: RedisplayOnVehicleForm(Main) Called...")
                    RedisplayOnVehicleForm("Main")
                    OnVehicleScreen.TopMost = False
                ElseIf _switchToMain = True AndAlso OnLoginScreen Then
                    ' Clear the flag but don't redisplay - LoginForm has priority
                    _switchToMain = False
                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: Skipping RedisplayOnVehicleForm - LoginForm active")
                End If

                'This redimensioning would only be required if a new go/nogo label was to be created at runtime, very unlikely
                'based on current use cases...
                If UBound(MyLabel) > UBound(goNoGoFault) Then
                    ReDim Preserve goNoGoFault(UBound(MyLabel))
                End If

                ' Application.DoEvents() removed - Async/Await already yields UI thread

                If PlaybackMode = False Then

                    whereAmI = "PlaybackMode = False"

                    'Here we are updating the HealthMonitor counter which is monitored by ProcessKiller running in a separate thread.
                    'If HealthMonitor is no longer counting up, it indicates that a periodic call made  to INCA in MyBackgoundtasks
                    'has hung - This circumstance would be handled in ProcessKiller in separate thread...
                    _healthMonitor += 1
                    If _healthMonitor > 30000 Then
                        _healthMonitor = 0
                    End If

                    'Every second (currently) we check INCA Measurement status and INCA Record Status
                    'we use this info to direct logic throughout this background task...
                    MyElapseTime2 = DateTime.Now.Subtract(MySaveTime2)
                    ' Check if enough time has elapsed since last measurement/recording state check
                    If (DateTime.Now - _lastMeasurementCheckTime).TotalSeconds >= MeasurementCheckIntervalSeconds Then
                        _lastMeasurementCheckTime = DateTime.Now

                        whereAmI = "MyIncaInterface.GetMeasurementStatus"
                        lMeasurementStatus = MyIncaInterface.GetMeasurementStatus()

                        If lMeasurementStatus = "True" Then

                            If _inMeasureMode = False Then
                                _inMeasureMode = True
                                MySaveMeasureTime = DateTime.Now
                                MySaveTime = DateTime.Now
                                MySaveTime3 = DateTime.Now

                                dataFrozenMaxCount = dataFrozenMaxCountMeasurementMode
                                ResetDataFrozenTracking("Measurement started")

                            End If

                        ElseIf lMeasurementStatus = "INCA Communication Failure" Or lMeasurementStatus = "GetMeasurementStatus Failure" Then

                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: L_MeasurementStatus = " & lMeasurementStatus)

                        Else
                            _inMeasureMode = False 'InMeasureMode is also monitored by ProcessKiller thread...
                            oneShot = False 'OneShot flag is passed to HandleAutomaticStartRecord, below... 

                        End If

                        whereAmI = "MyIncaInterface.GetRecordingState"
                        lRecordingState = MyIncaInterface.GetRecordingState()
                        If lSaveRecordingState = False And lRecordingState = True Then
                            dataFrozenMaxCount = dataFrozenMaxCountRecordMode
                            ' Discard counts accumulated during measure-only so INCA COMM/data-frozen
                            ' detection for the recording starts from the moment recording begins.
                            ResetDataFrozenTracking("Recording started")
                            MySaveTime3 = DateTime.Now
                        End If
                        lSaveRecordingState = lRecordingState

                        'This routine handles the case where measurement or record button has been pressed on INCA display
                        'rather than on CLEVIR display...
                        CheckForIncaButtonPresses(lMeasurementStatus, lRecordingState)

                    End If

                    If _inMeasureMode = True And lRecordingState = False Then

                        MyMeasureTime = DateTime.Now.Subtract(MySaveMeasureTime)

                        'Handle case where user has been in meaure mode for 5 minutes without pressing record.  HandleAutomaticStartRecord will
                        'automatically start recording unless user responds to prompt...
                        HandleAutomaticStartRecord(MyMeasureTime, MySaveMeasureTime, oneShot)

                    Else
                        MySaveMeasureTime = DateTime.Now
                    End If

                    'This section below will make periodic calls (every 5 seconds right now) to the Handle_5_SecondChecks routine.  This routine
                    'checks to see if there is an active INCA experiment, if not CLEVIR will terminate, also checks various other statuses and
                    'reacts accordingly...
                    MyElapseTime = DateTime.Now.Subtract(MySaveTime)
                    If MyElapseTime.Seconds >= 5 Then
                        MySaveTime = DateTime.Now

                        Await Handle_5_SecondChecks(lMeasurementStatus, lRecordingState)

                    End If 'If MyElapseTime.Seconds >= 5

                End If

                ' Communication/data alerts only apply once measurement or recording is active.
                If Not (_inMeasureMode OrElse lRecordingState) Then
                    MySaveTime3 = DateTime.Now
                End If

                MyElapseTime3 = DateTime.Now.Subtract(MySaveTime3)
                If MyElapseTime3.Seconds >= lostDeviceDelayTime Then 'was 10 changed to 15 08/30/2020, now 20 sec...
                    'This routine handles the case where communication to processors has been lost or invalid data or invalid video.  Allows user to
                    'reinitialilze CLEVIR and INCA...
                    Await CheckForLostDevice()
                    MySaveTime3 = DateTime.Now
                End If

                'If measurement started = true (or we are playing back CLEVIR recorded data), we make calls to get data, either live or recorded.
                'We also update the display grids by updating the data values for all signals and also go through the logic associated with high threshold and
                'low threshold, etc. to determine background color of each grid cell.

                whereAmI = "Before If MyIncaInterface.MeasurementStarted = True"

                If MyIncaInterface.MeasurementStarted = True Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackRun Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepBack Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackScrolling Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepFwd Then

                    whereAmI = "Before HandleRecordingProgressBar"

                    If Visible = True Or RecordPlayback.Visible = True Then
                        'This routine handles the green recording progress bar that is displayed either on the RecordPlayback form
                        'or on the main CLEVIR screen...
                        HandleRecordingProgressBar(RecordPlayback.RecordMode, RecordPlayback.ProgressBar1)
                    Else
                        ' OnVehicleScreen-only mode: hide EXIT button so Label17 (distance-to-object) can occupy that slot
                        If OnVehicleScreen.Button1.Visible Then
                            OnVehicleScreen.Button1.Visible = False
                        End If
                        'This routine handles the green recording progress bar that is displayed either on the RecordPlayback form
                        'or on the main CLEVIR screen...
                        'HandleRecordingProgressBar(MyIncaInterface.Recording, OnVehicleScreen.ProgressBar1)
                    End If

                    '********************************Here is where we are refreshing top down view **************************************

                    whereAmI = "Before If Not MyTdGraphicsContainer.IsDisposed"

                    If Not MyTdGraphicsContainer.IsDisposed Then

                        If MyTdGraphicsContainer.Visible = True Then
                            MyTdGraphicsContainer.Invalidate() 'this triggers paint event on topdown view...

                        End If
                    End If

                    '**********************************************************************************************************

                    whereAmI = "Before WAV Recording"
                    'This routine handles the color of the frame around the microphone to indicate whether a WAV recording
                    'is in progress.  Red, we are not recording WAV, Green we are...
                    'There is a preset amount of time that the WAV recording is enabled, and it needs to shut off after that time.
                    'This routine handles that...
                    HandleWavRecording()

                    If lRecordingState = True Then

                        'This routine handles, among other things, stopping and starting the INCA recording based
                        'on the time interval set...
                        HandleUpdatesWhenRecording()

                    Else 'L_RecordingState = False
                        ' When recording stops, ensure the monitor task is stopped as well.
                        If _isMonitorTaskRunning Then
                            StopRecordingMonitorTask()
                        End If

                    End If

                    'Here we are initializing the data array that will be used to save CLEVIR recorded data if the
                    'CLEVIR data recorder is set to RecordMode.  Not to be confused with recording INCA mf4 data.
                    'This recording creates a spreadsheet that CLEVIR can read back in so that it can play back
                    'data.  This would be used primarily to test CLEVIR or be used for DEMOs.

                    If RecordPlayback.RecordMode = True Then
                        whereAmI = "RecordPlayback.RecordMode = True"
                        k = 0
                        lineNum += 1
                        'ReDim Preserve VariableNameDataArray(UBound(MyIncaInterface.MyDisplaySignals) + 2, lineNum)
                        EnsureRecordingBufferCapacity(lineNum)

                        If lineNum = 0 Then
                            VariableNameDataArray(0, 0) = "Time"
                            VariableNameDataArray(1, 0) = "TimeStamp"
                        Else
                            VariableNameDataArray(0, lineNum) = ""
                            VariableNameDataArray(1, lineNum) = ""
                        End If

                    End If

                    'This section executes if we are processing live data....
                    If RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStop Then
                        If saveLiveDataTime = Nothing Then
                            liveDataElapseTime = DateTime.Now.Subtract(DateTime.Now)
                        Else
                            liveDataElapseTime = DateTime.Now.Subtract(saveLiveDataTime)
                        End If

                        whereAmI = "MyIncaInterface.GetSignalDataWithTime"
                        MySignalDataWithTime = MyIncaInterface.GetSignalDataWithTime() 'here is where we get all live data from INCA...

                        If MySignalDataWithTime IsNot Nothing Then

                            saveLiveDataTime = DateTime.Now

                            For j = 0 To UBound(MySignalDataWithTime)

                                'We can record live data to be played back in CLEVIR, here is where we capture that
                                'live data if we are recording...
                                If RecordPlayback.RecordMode = True And lineNum >= 0 Then
                                    If lineNum = 0 Then
                                        VariableNameDataArray(k + 2, 0) = MyIncaInterface.myDisplaySignals(j).SignalName
                                    Else
                                        If Len(VariableNameDataArray(0, lineNum)) = 0 Then
                                            VariableNameDataArray(0, lineNum) = Format$(liveDataElapseTime.Milliseconds, "0.000")
                                            VariableNameDataArray(1, lineNum) = Format$(MySignalDataWithTime(j).TimeStamp, "0.000")
                                        End If
                                        VariableNameDataArray(k + 2, lineNum) = MySignalDataWithTime(j).SignalData
                                    End If
                                    k += 1
                                End If

                            Next j

                        End If

                    Else 'This else case executes if we are playing back CLEVIR recorded data....

                        whereAmI = "This else case executes if we are playing back recorded data...."

                        ReDim MySignalDataWithTime(0)

                        MyRci2Data = GetRecordedData(UBound(MyIncaInterface.myDisplaySignals) + 2, RecordPlayback.PlaybackMode)

                        For i = 0 To UBound(MyRci2Data.myData)
                            If i > UBound(MySignalDataWithTime) Then
                                ReDim Preserve MySignalDataWithTime(i)
                            End If

                            MySignalDataWithTime(i).SignalData = MyRci2Data.myData(i)
                            MySignalDataWithTime(i).TimeStamp = MyRci2Data.myTime(i)
                        Next i

                    End If

                    'If we have valid data (live or recorded) this section executes...
                    If MySignalDataWithTime IsNot Nothing Then

                        lTotalnumsignalsdisplayed = 0
                        ProcessGrids(saveOdoReading, saveWhereAmIAt, goNoGoFault, dataFrozenMaxCount, EventArgs.Empty, lRecordingState)

                    End If 'Check for MySignalDataWithTime Is Nothing

                    ' Delay with cancellation token for immediate exit response
                    Dim delayMs As Integer = If(lRecordingState, Math.Max(100, Val(_displayRefreshRate)), Val(_displayRefreshRate))
                    Try
                        Await Task.Delay(delayMs, If(_backgroundTasksCts?.Token, CancellationToken.None))
                    Catch ex As TaskCanceledException
                        ' Exit requested - break out of the main loop
                        Exit While
                    End Try

                    'Here is where we save the CLEVIR record file if we transitioned out of RecordPlayBack.RecordMode = True
                    If RecordPlayback.RecordMode = False And lineNum > 0 Then
                        SaveRecordFile(lineNum)
                        lineNum = -1
                    End If

                    If RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepFwd Or
                    RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepBack Then
                        RecordPlayback.GroupBox2.Text = "PLAYBACK (Pause)"
                    End If

                    TotalNumSignalsDisplayed = lTotalnumsignalsdisplayed

                    whereAmI = "Finished in MeasurementStatus = True"

                Else 'Measurement not started and not playing back CLEVIR Recording....

                    HandleNonMeasureModeUpdates(saveOdoReading, saveWhereAmIAt, goNoGoFault)

                    'Save CLEVIR Recording when transitioning out of measure mode if we were recording...
                    If RecordPlayback.RecordMode = True Then
                        If lineNum > 0 Then
                            SaveRecordFile(lineNum)
                        End If
                        RecordPlayback.RecordMode = False
                        lineNum = -1
                    End If
                    Try
                        Await Task.Delay(100, If(_backgroundTasksCts?.Token, CancellationToken.None))
                    Catch ex As TaskCanceledException
                        Exit While
                    End Try
                    ' Application.DoEvents() removed - Task.Delay already yields UI thread

                End If

                retries = 0

            End While

        Catch ex As Exception

            If logMsgWritten = False Then
                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & whereAmI & " - " & ex.Message)
                logMsgWritten = True
            End If

        End Try

    End Function

    Private Sub ProcessGrids(
                                  ByVal saveOdoReading As Double,
                                  ByRef saveWhereAmIAt As String,
                                  ByRef goNoGoFault() As Boolean,
                                  dataFrozenMaxCount As Integer,
                                  e As EventArgs,
                                  ByVal lRecordingState As Boolean
                                  )

        Dim whereAmI As String = "undefined"
        Dim i As Integer
        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim gridUpdateAction As GridUpdateActions
        Dim Mystr As String
        Dim Mytempstr As String
        Dim Mytempval As Double

        Dim lTotalnumsignalsdisplayed As Integer

        Dim logMsgWritten As Boolean

        Dim saveHealthCounter As Integer

        Dim MyDtCsSaveTime As DateTime
        Dim MyDtCsElapseTime As TimeSpan
        Dim activeDtc As Boolean
        Dim dtcTimerActive As Boolean

        'Dim saveDtcStrings As ArrayList = Nothing

        Dim acpDebugOneShot As Boolean

        Try

            If myDGs(0) IsNot Nothing Then

                'Here we will loop through all of the grids on all of the forms...
                For z = 0 To myDGs.Count - 1

                    whereAmI = "Go Thru All Displayed Grids"

                    'need to go thru all grids which are displayed, or are associated with gonogo, or are associated with a custom screen (CS_)....
                    If myDGs(z).Parent.Visible = True Or MyDFs(myDGs(z).ParentFormIndex).AlsoAssociatedWith = "GO/NOGO" Or MyDFs(myDGs(z).ParentFormIndex).AlsoAssociatedWith = "AUTOANNO" Or
                (InStr(MyDFs(myDGs(z).ParentFormIndex).AlsoAssociatedWith, "CS_") > 0) Then

                        whereAmI = "Grid Visible or GO/NOGO or CS_"

                        Mystr = ""
                        Mytempstr = ""
                        Mytempval = 0

                        If lRecordingState = True Then

                            If myDGs(z).CS_NAN_STATUS > 0 Then
                                HandleNanStatus(myDGs(z))
                            End If

                            If myDGs(z).CS_WAVRECORD > 0 Then
                                If OnVehicleScreen.PictureBox1.BackColor = Color.Red Then
                                    Handle_CS_WAVRECORD(myDGs(z))
                                End If
                            End If

                        End If

                        'This Routine handles all things related to vehicle mileage and where the vehicle is,
                        'on grounds or off grounds, etc. Will not be called unless CS_ODOMETER is defined in signallist...
                        If myDGs(z).CS_ODOMETER > 0 Then
                            HandleOdometerRelatedStatus(myDGs(z), saveOdoReading, saveWhereAmIAt, lRecordingState)
                        End If

                        'This Routine handles CLEVIR display of cluster message...
                        'Nothing happens unless CS_LCC_CLUSTER_MSG and CS_LCC_BUTTON_PRESS are defined in signal list...
                        If myDGs(z).CS_LCC_CLUSTER_MSG > 0 And myDGs(z).CS_LCC_BUTTON_PRESS > 0 Then
                            HandleClevirDisplayOfClusterMsgs(myDGs(z))
                        End If

                        'this next section handles custom displays.  This is where data is put into the
                        'labels on the custom displays.  Background colors for labels are updated here based on values
                        'of variables associated with the various labels on the custom screens.  Whenever a custom 
                        'screen is added, code must be added here to handle updating. Code is only executed if custom

                        'ADD CUSTOM SCREEN HANDLING HERE....

                        UpdateCustomDisplays(myDGs(z))

                        If myDGs(z).CS_CPSTOS_X_POS > 0 And myDGs(z).CS_CPSTOS_Y_POS > 0 Then

                            If TargetStatusDisplay.Visible = True Then
                                TargetStatusDisplay.Label2.Text = Format$(Math.Abs(Math.Sqrt((MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_X_POS, 1)).SignalData * MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_X_POS, 1)).SignalData) +
                        (MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData * MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData))), "0.00")
                            End If

                            Dim xPos As Double = MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_X_POS, 1)).SignalData
                            Dim yPos As Double = MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData
                            Dim distText As String = Format$(Math.Abs(Math.Sqrt((xPos * xPos) + (yPos * yPos))), "0.00")
                            OnVehicleScreen.Label17.Text = distText

                        End If

                        If PedestrianStatusDisplay.Visible = True Then HandlePedestrianStatusDisplay(myDGs(z))

                        ' ============================================================
                        ' SUSPEND LAYOUT FOR BATCHED GRID UPDATES
                        ' ============================================================
                        myDGs(z).SuspendLayout()
                        Try
                            whereAmI = "Rows and Columns"
                            'For each grid, we must go through all configured rows and columns to update data display,
                            'background and foreground colors, etc.
                            'DataArray may be smaller than NumberOfRows/NumberOfColumns when those values were grown
                            'after FinalizeGridDisplays allocated it; capture its real bounds to guard the writes below.
                            Dim dataArrayMaxRow As Integer = -1
                            Dim dataArrayMaxCol As Integer = -1
                            If myDGs(z).DataArray IsNot Nothing Then
                                dataArrayMaxRow = myDGs(z).DataArray.GetUpperBound(0)
                                dataArrayMaxCol = myDGs(z).DataArray.GetUpperBound(1)
                            End If

                            ' One-time-per-grid diagnostic: if NumberOfRows/NumberOfColumns grew past the
                            ' DataArray allocated in FinalizeGridDisplays, log which grid drifted and by how much.
                            If (myDGs(z).NumberOfRows - 2) > dataArrayMaxRow OrElse (myDGs(z).NumberOfColumns - 1) > dataArrayMaxCol Then
                                Dim driftKey As String = myDGs(z).ParentFormName & "/" & myDGs(z).Name
                                If _dataArraySizeWarned.Add(driftKey) Then
                                    HandleUserMessageLogging("GMRC", "processGrids: DataArray size drift on '" & driftKey &
                                        "' - NumberOfRows=" & myDGs(z).NumberOfRows & " NumberOfColumns=" & myDGs(z).NumberOfColumns &
                                        " but DataArray bounds=(" & dataArrayMaxRow & "," & dataArrayMaxCol & "). Display writes past the array are skipped.")
                                End If
                            End If

                            For x = 0 To myDGs(z).NumberOfRows - 1

                                For y = 0 To myDGs(z).NumberOfColumns - 1

                                    Mytempstr = ""
                                    If myDGs(z).SignalIndex(x, y) >= 0 And
                                Len(myDGs(z).VariableName(x, y)) > 0 And
                                myDGs(z).Registered(x, y) = True And
                                myDGs(z).VariableName(x, y) <> "undefined" Then

                                        'This section handles putting dashes in all cells if object ID is 0...
                                        'assumes the "blanker" is the first row that must be "blanked" and all subsequent rows in the associated grid are "blanked"
                                        If (myDGs(z).ObjectID_Start_Pos <> 0 And
                                     MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).ObjectID_Start_Pos, y)).SignalData = 0) _
                                    AndAlso x > myDGs(z).ObjectID_Start_Pos Then

                                            Mytempstr = "-"
                                            If myDGs(z).CurrentBackColor(x, y) <> myDGs(z).DefaultCellBackColor(x, y) Then
                                                UpdateGridColor(z, x, y, GridUpdateActions.FromLow)
                                            End If

                                        Else
                                            'HealtCounter is maintained in ProcessKiller thread.  A HealthCounter value of 0 indicates
                                            'a healthy system...
                                            If _healthCounter = 0 Then

                                                If saveHealthCounter > 1 Then
                                                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: HealthCounter recovered from " & saveHealthCounter)
                                                End If
                                                saveHealthCounter = 0

                                                If Debugger.IsAttached Then

                                                    If OnVehicleScreen.CheckBox1.Checked = True Then
                                                        If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 AndAlso IsConfiguredProcessorDevice(myDGs(z).DeviceName(x, y)) AndAlso String.Equals(myDGs(z).DeviceName(x, y), PrimaryProcessorDeviceName, StringComparison.OrdinalIgnoreCase) Then
                                                            If acpDebugOneShot = False Then
                                                                MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = GetRandom(0, 10)
                                                                acpDebugOneShot = True
                                                            Else
                                                                MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                                                                acpDebugOneShot = False
                                                            End If
                                                        End If
                                                    Else
                                                        If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 AndAlso IsConfiguredProcessorDevice(myDGs(z).DeviceName(x, y)) AndAlso String.Equals(myDGs(z).DeviceName(x, y), PrimaryProcessorDeviceName, StringComparison.OrdinalIgnoreCase) Then
                                                            acpDebugOneShot = False
                                                            MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                                                        End If
                                                    End If

                                                End If

                                                'The CheckForDataChange property is associated with the same column in the
                                                'signal configuration spreadsheet.  This allows us to set up specific signals to watch
                                                'to see if they are changing.  If they do not change over a certain period of time,
                                                'this would indicate a communication issue....

                                                If myDGs(z).CheckForDataChange(x, y) = True And MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex > -1 And
                                                (myDGs(z).Name <> "INCA Video Status Items" Or (myDGs(z).Name = "INCA Video Status Items" And lRecordingState = True)) Then
                                                    If myDGs(z).SaveLastValue(x, y) = MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData Then

                                                        If myDGs(z).DataFrozen(x, y) = False Then 'ADDED 04/14/2020
                                                            myDGs(z).DataFrozenCounter(x, y) = myDGs(z).DataFrozenCounter(x, y) + 1
                                                        End If

                                                        'the number DataFrozenMaxCount is somewhat arbitrary, this should be based on time because we 
                                                        'count up faster when we are waiting less time between cycles through this code
                                                        If myDGs(z).DataFrozenCounter(x, y) > dataFrozenMaxCount And myDGs(z).DataFrozen(x, y) = False Then

                                                            myDGs(z).DataFrozenCounter(x, y) = 0
                                                            myDGs(z).DataFrozen(x, y) = True

                                                            CheckForExperiment = False

                                                            If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).HighThreshBackColor(x, y)) Then
                                                                gridUpdateAction = GridUpdateActions.ToHigh
                                                                UpdateGridColor(z, x, y, gridUpdateAction)

                                                                If lRecordingState = True Then
                                                                    MyIncaInterface.WriteEventComment(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - First Instance Data Frozen", True)
                                                                End If

                                                            Else

                                                                If lRecordingState = True Then
                                                                    MyIncaInterface.WriteEventComment(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Not Updating", True)
                                                                End If

                                                            End If

                                                            'Here we set the go/nogo label color to red and save the device names for those
                                                            'devices with frozen data.  These FrozenDeviceNames are used when displaying
                                                            'user message indicating a data or video device issue...

                                                            If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then

                                                                goNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                                UpdateGonogoLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)

                                                                If myDGs(z).Name <> "INCA Video Status Items" Then

                                                                    If Len(SaveDataFrozenDeviceName) = 0 Then
                                                                        SaveDataFrozenDeviceName = myDGs(z).DeviceName(x, y)
                                                                    ElseIf InStr(SaveDataFrozenDeviceName, myDGs(z).DeviceName(x, y)) = 0 Then
                                                                        SaveDataFrozenDeviceName = SaveDataFrozenDeviceName & "," & myDGs(z).DeviceName(x, y)
                                                                    End If

                                                                Else

                                                                    If Len(SaveVideoFrozenDeviceName) = 0 Then
                                                                        SaveVideoFrozenDeviceName = myDGs(z).DeviceName(x, y)
                                                                    ElseIf InStr(SaveVideoFrozenDeviceName, myDGs(z).DeviceName(x, y)) = 0 Then
                                                                        SaveVideoFrozenDeviceName = SaveVideoFrozenDeviceName & "," & myDGs(z).DeviceName(x, y)
                                                                    End If

                                                                End If

                                                            End If

                                                        End If
                                                    Else 'Value has changed...

                                                        'If any of the frozen data starts to change, we reset the frozen data counter and frozen data state here...
                                                        If myDGs(z).CheckForDataChange(x, y) = True And MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex > -1 Then
                                                            myDGs(z).DataFrozenCounter(x, y) = 0

                                                            If myDGs(z).DataFrozen(x, y) = True Then
                                                                myDGs(z).DataFrozen(x, y) = False

                                                                If lRecordingState = True Then
                                                                    MyIncaInterface.WriteEventComment(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Resumed Updating", True)
                                                                Else
                                                                    HandleUserMessageLogging("GMRC", myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Resumed Updating")
                                                                End If

                                                                If myDGs(z).Name <> "INCA Video Status Items" Then
                                                                    If InStr(SaveDataFrozenDeviceName, ",") > 0 Then
                                                                        Dim tempstr() As String
                                                                        tempstr = Split(SaveDataFrozenDeviceName, ",")
                                                                        SaveDataFrozenDeviceName = ""
                                                                        For i = 0 To UBound(tempstr)
                                                                            If tempstr(i) <> myDGs(z).DeviceName(x, y) Then
                                                                                If Len(SaveDataFrozenDeviceName) = 0 Then
                                                                                    SaveDataFrozenDeviceName = tempstr(i)
                                                                                Else
                                                                                    SaveDataFrozenDeviceName = SaveDataFrozenDeviceName & "," & tempstr(i)
                                                                                End If

                                                                            End If
                                                                        Next i

                                                                    Else
                                                                        SaveDataFrozenDeviceName = ""
                                                                    End If

                                                                Else
                                                                    If InStr(SaveVideoFrozenDeviceName, ",") > 0 Then
                                                                        Dim tempstr() As String
                                                                        tempstr = Split(SaveVideoFrozenDeviceName, ",")
                                                                        SaveVideoFrozenDeviceName = ""
                                                                        For i = 0 To UBound(tempstr)
                                                                            If tempstr(i) <> myDGs(z).DeviceName(x, y) Then
                                                                                If Len(SaveVideoFrozenDeviceName) = 0 Then
                                                                                    SaveVideoFrozenDeviceName = tempstr(i)
                                                                                Else
                                                                                    SaveVideoFrozenDeviceName = SaveVideoFrozenDeviceName & "," & tempstr(i)
                                                                                End If

                                                                            End If
                                                                        Next i

                                                                    Else
                                                                        SaveVideoFrozenDeviceName = ""
                                                                    End If

                                                                End If

                                                            End If

                                                        End If

                                                        If PlaybackMode = False Then CheckForExperiment = True

                                                        If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)) Then
                                                            gridUpdateAction = GridUpdateActions.FromHigh
                                                            UpdateGridColor(z, x, y, gridUpdateAction)
                                                        End If

                                                    End If
                                                End If

                                            Else 'HealthCounter > 0 indicates that we have most likely waiting on an INCA API Call...

                                                'If we are waiting on an INCA API Call, we should reset the frozen counter...
                                                If _healthCounter > 10 Then myDGs(z).DataFrozenCounter(x, y) = 0
                                                If PlaybackMode = False Then CheckForExperiment = True

                                                If saveHealthCounter = 0 Then
                                                    If _healthCounter > 1 Then
                                                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: HealthCounter = " & _healthCounter)
                                                    End If
                                                End If

                                                saveHealthCounter = _healthCounter

                                            End If

                                            whereAmI = "Set Mytempval"

                                            Mytempval = MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData

                                            whereAmI = "Format Display String"

                                            ' Cache formatted string to avoid redundant FormatDisplayString calls
                                            ' In ProcessGrids - Handle first-time initialization
                                            If myDGs(z).SaveFormattedString(x, y) Is Nothing OrElse String.IsNullOrEmpty(myDGs(z).SaveFormattedString(x, y)) Then
                                                ' First time accessing this cell - format and cache
                                                Mytempstr = FormatDisplayString(Mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y))
                                                myDGs(z).SaveFormattedString(x, y) = Mytempstr
                                                myDGs(z).SaveLastValue(x, y) = Mytempval
                                            ElseIf myDGs(z).SaveLastValue(x, y) = Mytempval Then
                                                ' Value unchanged - reuse cached formatted string
                                                Mytempstr = myDGs(z).SaveFormattedString(x, y)
                                            Else
                                                ' Value changed - format and cache the new string
                                                Mytempstr = FormatDisplayString(Mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y))
                                                myDGs(z).SaveFormattedString(x, y) = Mytempstr

                                                ' Update SaveLastValue (except for AUTOANNO)
                                                If myDGs(z).AlsoAssociatedWith(x, y) <> "AUTOANNO" Then
                                                    myDGs(z).SaveLastValue(x, y) = Mytempval
                                                End If
                                            End If

                                            'This section handles updating back color, fore color etc. based on settings
                                            'in excel configuration file

                                            whereAmI = "Update Grid Colors"

                                            'Here is where we handle updating grid cell colors based on data values in the variovus cells
                                            'We also update go/nogo label colors here if necessary.  This is done here for those variables
                                            'that are not frozen...

                                            If myDGs(z).DataFrozen(x, y) = False Then

                                                whereAmI = "HighThresh Check"
                                                If Mytempval > myDGs(z).HighThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
                                                    If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).HighThreshBackColor(x, y)) Then

                                                        If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).HighThresh(x, y),,,,, 1)
                                                        Else
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).HighThresh(x, y),,,,, 2)
                                                        End If

                                                        gridUpdateAction = GridUpdateActions.ToHigh
                                                        UpdateGridColor(z, x, y, gridUpdateAction)

                                                        If myDGs(z).Name = "DTCs" Then

                                                            If activeDtc = False Then
                                                                ' First DTC detected - initialize the collection and log it
                                                                saveDtcStrings = New ArrayList From {
            myDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(Mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y))
        }

                                                                Dim firstDtc As String = CStr(saveDtcStrings(0))

                                                                ' Only log if we haven't logged this exact DTC yet
                                                                If Not loggedDtcStrings.Contains(firstDtc) Then
                                                                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: Active DTC = " & firstDtc)
                                                                    loggedDtcStrings.Add(firstDtc)
                                                                End If

                                                                activeDtc = True

                                                                ' Start the timer for potential DTC clear detection
                                                                MyDtCsSaveTime = DateTime.Now
                                                                dtcTimerActive = True
                                                            Else
                                                                ' Already have active DTCs - check if this is a new unique DTC
                                                                Dim currentDtc As String = myDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(Mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y))

                                                                If saveDtcStrings IsNot Nothing Then
                                                                    ' Add to active list if not already there
                                                                    If Not saveDtcStrings.Contains(currentDtc) Then
                                                                        saveDtcStrings.Add(currentDtc)
                                                                    End If

                                                                    ' Log only if we haven't logged this specific DTC yet
                                                                    If Not loggedDtcStrings.Contains(currentDtc) Then
                                                                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: Active DTC = " & currentDtc)
                                                                        loggedDtcStrings.Add(currentDtc)
                                                                    End If
                                                                End If

                                                                ' Reset the timer since we still have active DTCs
                                                                MyDtCsSaveTime = DateTime.Now
                                                                dtcTimerActive = True
                                                            End If

                                                            ' Set GO/NOGO status to red while DTCs are present
                                                            If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                                goNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                                UpdateGonogoLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                                                            End If

                                                        End If
                                                    End If

                                                ElseIf Mytempval <= myDGs(z).HighThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 And myDGs(z).Name = "DTCs" Then

                                                    ' DTC value has dropped below threshold - check if timer has expired
                                                    If dtcTimerActive = True Then
                                                        MyDtCsElapseTime = DateTime.Now.Subtract(MyDtCsSaveTime)

                                                        If MyDtCsElapseTime.Seconds > 5 Then
                                                            ' 5 seconds have passed with no active DTCs - log the clear status once
                                                            If activeDtc = True Then
                                                                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: No Active DTCs")
                                                                activeDtc = False
                                                            End If

                                                            dtcTimerActive = False
                                                            MyDtCsSaveTime = DateTime.Now

                                                            ' Clear both the active DTC list and the logged set
                                                            saveDtcStrings?.Clear()

                                                            loggedDtcStrings.Clear()  ' ← Reset the "already logged" tracker

                                                            ' Reset GO/NOGO status to green
                                                            goNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = False
                                                            UpdateGonogoLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Green)
                                                        End If
                                                    End If
                                                End If
                                                whereAmI = "LowThresh Check"
                                                If Mytempval < myDGs(z).LowThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
                                                    If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).LowThreshBackColor(x, y)) Then
                                                        If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).LowThresh(x, y),,,,, 1)
                                                        Else
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).LowThresh(x, y),,,,, 2)
                                                        End If
                                                        gridUpdateAction = GridUpdateActions.ToLow
                                                        UpdateGridColor(z, x, y, gridUpdateAction)
                                                    End If
                                                    If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                        goNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                        UpdateGonogoLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                                                    End If

                                                End If
                                                whereAmI = "Reset Check HIGH"
                                                If Mytempval <= myDGs(z).HighThresh(x, y) And Mytempval >= myDGs(z).LowThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
                                                    If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)) Then
                                                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).HighThresh(x, y),,,,, 2)
                                                        gridUpdateAction = GridUpdateActions.FromHigh
                                                        UpdateGridColor(z, x, y, gridUpdateAction)

                                                    End If
                                                End If
                                                whereAmI = "Reset Check LOW"
                                                If Mytempval >= myDGs(z).LowThresh(x, y) And Mytempval <= myDGs(z).HighThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
                                                    If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).LowThreshBackColor(x, y)) Then
                                                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).LowThresh(x, y),,,,, 2)
                                                        gridUpdateAction = GridUpdateActions.FromLow
                                                        UpdateGridColor(z, x, y, gridUpdateAction)
                                                    End If
                                                End If
                                                whereAmI = "Equal To Check"
                                                If Len(myDGs(z).EqualTo(x, y)) > 0 And myDGs(z).CheckForDataChange(x, y) = False Then
                                                    If Mytempval = Val(myDGs(z).EqualTo(x, y)) Then
                                                        If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).LowThreshBackColor(x, y)) Then
                                                            If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Equals " & Val(myDGs(z).EqualTo(x, y)),,,,, 1)
                                                            Else
                                                                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Equals " & Val(myDGs(z).EqualTo(x, y)),,,,, 2)
                                                            End If
                                                            gridUpdateAction = GridUpdateActions.ToLow
                                                            UpdateGridColor(z, x, y, gridUpdateAction)
                                                        End If
                                                        If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                            goNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                            UpdateGonogoLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                                                        End If
                                                    Else
                                                        If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).LowThreshBackColor(x, y)) Then
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Not Equal To " & Val(myDGs(z).EqualTo(x, y)),,,,, 2)
                                                            gridUpdateAction = GridUpdateActions.FromLow
                                                            UpdateGridColor(z, x, y, gridUpdateAction)
                                                        End If
                                                    End If
                                                End If
                                            End If
                                        End If
                                    Else
                                        Mytempstr = "UnRgstrd"
                                    End If
                                    whereAmI = "Build Display Array for Grid"
                                    If myDGs(z).Parent.Visible = True Then
                                        If x > 0 And y >= 1 AndAlso (x - 1) <= dataArrayMaxRow AndAlso y <= dataArrayMaxCol Then
                                            myDGs(z).DataArray(x - 1, y) = Mytempstr
                                        End If
                                        whereAmI = "Total Num Signals Displayed Calculation..."
                                        lTotalnumsignalsdisplayed += 1
                                    End If
                                Next y
                            Next x
                        Finally
                            ' RESUME LAYOUT - Single redraw at end
                            myDGs(z).ResumeLayout(True)
                        End Try
                        ' ============================================================
                        ' END SUSPEND LAYOUT BLOCK
                        ' ============================================================

                        whereAmI = "Out of X Y Loops - Ready to Refresh Grid..."

                        ' Throttled refresh using the existing dictionary-based timing
                        Dim key = myDGs(z).Name
                        Dim nowT = DateTime.UtcNow
                        Dim nextAt As DateTime
                        If Not _gridRefreshAt.TryGetValue(key, nextAt) OrElse nowT >= nextAt Then
                            myDGs(z).Refresh()
                            _gridRefreshAt(key) = nowT + GridMinRefresh
                        End If
                    End If
                Next z

                whereAmI = "Out of Z Loop..."

                'Because there may be multiple grid cells that can influence go/nogo label colors, we must
                'reconcile what the actual color of the label should be here, after going through all forms
                'grids and cells above.
                For z = 0 To MyDFs.Count - 1
                    If MyDFs(z).GoNoGoIndex > -1 Then
                        Dim goNoGoIndex As Integer = MyDFs(z).GoNoGoIndex
                        Dim reasons As List(Of String) = If(GO_NOGO_DEBUG_VERBOSE, New List(Of String)(), Nothing)
                        Dim existingFault As Boolean = goNoGoFault(goNoGoIndex)
                        Dim finalFault As Boolean = EvaluateGoNoGoFault(z, existingFault, reasons)

                        If finalFault Then
                            UpdateGonogoLabelColor(goNoGoIndex, Color.Red)
                        Else
                            UpdateGonogoLabelColor(goNoGoIndex, Color.Green)
                        End If

                        If GO_NOGO_DEBUG_VERBOSE Then
                            Dim reasonText As String = If(reasons IsNot Nothing AndAlso reasons.Count > 0, String.Join(" | ", reasons), "none")
                            HandleUserMessageLogging("GMRC", $"GO/NOGO TRACE: Form='{MyDFs(z).Name}' Index={goNoGoIndex} ExistingFault={existingFault} FinalFault={finalFault} Reasons={reasonText}")
                        End If

                        ' Reset transient fault bit each cycle; recomputed each pass from current state
                        goNoGoFault(goNoGoIndex) = False
                    End If
                Next z
            End If 'Check for MyDGs(0) Is Nothing
        Catch ex As Exception

            If logMsgWritten = False Then
                HandleUserMessageLogging("GMRC", "processGrids: " & whereAmI & " - " & ex.Message)
                logMsgWritten = True
            End If

        End Try
    End Sub


    Private Sub UpdateCustomDisplays(ByVal MyDg As GridDataClass)
        ' Extract all the HandleXXXDisplay calls into one method
        If MyTdGraphicsContainer.Visible Then HandleAlcDisplayElements(MyDg)
        If CopilotStatusDisplay.Visible Then HandleCoPilotStatusDisplay(MyDg)
        If LmfrStatusScreenHc.Visible Then HandleLmfrStatusScreenHc(MyDg)
        If LmfrStatusDisplayGlobalA.Visible Then HandleLmfrStatusScreenGlobalA(MyDg)
        If FusionStatusDisplay.Visible Then HandleFusionStatusDisplay(MyDg)
        If LkaForm.Visible Then HandleLkaCustomDisplay(MyDg)
        If TargetStatusDisplay.Visible Then HandleTargetStatusDisplay(MyDg)
        If PedestrianStatusDisplay.Visible Then HandlePedestrianStatusDisplay(MyDg)
    End Sub

    Private Function EvaluateGoNoGoFault(ByVal formIndex As Integer, ByVal existingFault As Boolean, ByRef reasons As List(Of String)) As Boolean
        Dim hasFrozenGoNoGo As Boolean = False

        For i As Integer = 0 To myDGs.Count - 1
            If myDGs(i).ParentFormIndex <> formIndex Then Continue For

            For x As Integer = 0 To myDGs(i).NumberOfRows - 1
                For y As Integer = 0 To myDGs(i).NumberOfColumns - 1
                    If InStr(myDGs(i).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 AndAlso myDGs(i).DataFrozen(x, y) Then
                        hasFrozenGoNoGo = True
                        If reasons IsNot Nothing Then
                            reasons.Add($"FROZEN {myDGs(i).DeviceName(x, y)}.{myDGs(i).VariableName(x, y)}")
                        End If
                    End If
                Next
            Next
        Next

        Return existingFault OrElse hasFrozenGoNoGo
    End Function

    Private Function GetEnumDescription(
        ByVal MySignalEnums As List(Of SignalEnums),
        ByVal displayValue As Double,
        ByVal variableName As String,
        ByVal deviceName As String
    ) As String

        Dim result As String = "XXXX"
        Dim found As Boolean = False

        Try

            ' Only perform lookup if the list is non-null and has items
            If MySignalEnums IsNot Nothing AndAlso MySignalEnums.Count > 0 Then

                ' Loop through each SignalEnums object
                For Each signalEnum In MySignalEnums

                    ' Check if variable name doesn't contain "SDFIC"
                    If Not variableName.ToUpper().Contains("SDFIC") Then
                        If variableName.Contains(signalEnum.VariableName) Then
                            Dim idx As Integer = CInt(displayValue)

                            ' Confirm index is within the Enums list bounds
                            If idx >= 0 AndAlso idx < signalEnum.Enums.Count Then
                                Dim enumValue As String = signalEnum.Enums(idx)

                                ' Check the enumValue is not Nothing and device matches
                                If enumValue IsNot Nothing Then
                                    If signalEnum.DeviceName = deviceName OrElse
                                   signalEnum.DeviceName = "" Then
                                        result = enumValue
                                        found = True
                                        Exit For
                                    End If
                                Else
                                    result = "XXXX"
                                End If
                            End If
                        End If
                    Else
                        ' Special check for VaIDRR_e_DTC_List
                        If signalEnum.VariableName = "VaIDRR_e_DTC_List" Then
                            Dim idx As Integer = CInt(displayValue)
                            If idx >= 0 AndAlso idx < signalEnum.Enums.Count Then
                                Dim enumValue As String = signalEnum.Enums(idx)
                                If enumValue IsNot Nothing Then
                                    If signalEnum.DeviceName = deviceName OrElse
                                   signalEnum.DeviceName = "" Then
                                        result = enumValue
                                        found = True
                                        Exit For
                                    End If
                                Else
                                    result = "XXXX"
                                End If
                            End If
                        End If
                    End If
                Next
            End If

            ' If we never found a match, display numeric value instead
            If result = "XXXX" OrElse Not found Then
                result = Format(displayValue, "0")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GetEnumDescription: " & ex.Message & " - " & variableName)
            result = Format(displayValue, "0")
        End Try

        Return result
    End Function

    Private Function FormatDisplayString(ByVal displayValue As Double, ByVal displayFormat As String, ByVal deviceName As String, ByVal variableName As String) As String

        'Called from MyBackgroundTasks:
        'Formats the string which will be displayed for a given variable.  DisplayFormat is read in from excel spreadsheet.  The raw number can 
        'be displayed to the level of precision defined using a format string such as "0"  or "0.0", or other formats can be used such as
        '"HEX" or "TRUE/FALSE", etc.  If the DisplayFormat is "ENUM", GetEnumDescription is called.

        Try
            FormatDisplayString = ""

            Select Case UCase(displayFormat)
                Case "ENUM"

                    FormatDisplayString = GetEnumDescription(GlobalSignalEnums, CInt(displayValue), variableName, deviceName)
                    If Not IsNumeric(Mid(FormatDisplayString, 1, 1)) Then
                        FormatDisplayString = CStr(CInt(displayValue)) & " " & FormatDisplayString
                    End If

                Case "TRUE/FALSE"
                    FormatDisplayString = IIf(CInt(displayValue) = 0, "0 True", "1 False")
                Case "FALSE/TRUE"
                    FormatDisplayString = IIf(CInt(displayValue) = 0, "0 False", " 1 True")
                Case "INVALID/VALID"
                    FormatDisplayString = IIf(CInt(displayValue) = 0, "0 Invalid", " 1 Valid")
                Case "VALID/INVALID"
                    FormatDisplayString = IIf(CInt(displayValue) = 0, "0 Valid", "1 Invalid")
                Case "HEX"
                    FormatDisplayString = Hex(CStr(displayValue))
                Case "DEC"
                    FormatDisplayString = Format$(CInt(displayValue), "0")
                Case "BINARY"
                    FormatDisplayString = Convert.ToString(CLng(displayValue), 2).PadLeft(8, "0"c)
                Case Else 'assumes ""0"", or ""0.0"" or ""0.000"", etc....
                    FormatDisplayString = Format$(displayValue, displayFormat)
            End Select

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "FormatDisplayString: " & ex.Message & " - " & variableName)
            FormatDisplayString = "-"

        End Try

    End Function

    Private Sub HandleRecordingProgressBar(ByVal status As Boolean, ByVal progressBar As ProgressBar)
        ' HandleRecordingProgressBar is called from MyBackgroundTasks routine and from StartStopRecording and StartStopMeasurement
        ' This routine handles the green recording progress bar that is displayed either on the RecordPlayback form
        ' or on the main CLEVIR screen. The progressbar to operate on (OnVehicleScreen progressbar or RecordPlayback progressbar) 
        ' is passed in as ProgressBar.

        Try
            ' Ensure thread safety when updating the progress bar
            If progressBar.InvokeRequired Then
                progressBar.Invoke(New Action(Of Boolean, ProgressBar)(AddressOf HandleRecordingProgressBar), status, progressBar)
            Else
                If status Then
                    StartOrUpdateProgressBar(progressBar)
                Else
                    ResetProgressBar(progressBar)
                End If
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HandleRecordingProgressBar: {ex.Message}", DisplayMsgBox)
        End Try
    End Sub

    Private Sub StartOrUpdateProgressBar(ByVal progressBar As ProgressBar)
        ' Start or update the progress bar
        If progressBar.Value < progressBar.Maximum Then
            If progressBar.Value = 0 Then
                progressBar.Visible = True
            End If
            progressBar.Value += 5
        Else
            progressBar.Value = 0
        End If
    End Sub

    Private Sub ResetProgressBar(ByVal progressBar As ProgressBar)
        ' Reset the progress bar
        progressBar.Value = 0
        progressBar.Visible = False
    End Sub

    Private Sub SaveRecordFile(ByVal lineNum As Integer)

        'This routine will save the APPLICATION record file when the stop record button is pressed on the RecordPlayback form.  This should
        'not be confused with INCA recording.  This recording file is a .csv file which can then be 
        'played back by the resident client to visualize previous live data.

        Dim fnum As Integer
        Dim filename As String
        Dim x As Integer
        Dim y As Integer
        Dim savestring As String = ""

        fnum = FreeFile()
        filename = Mid(INCAVariableFile, 1, InStr(INCAVariableFile, ".xlsx") - 1) & ".csv"
        FileOpen(fnum, filename, OpenMode.Output)

        For x = 0 To lineNum
            For y = 0 To UBound(VariableNameDataArray, 1)
                If y = 0 Then
                    savestring = VariableNameDataArray(y, x)
                Else
                    savestring = savestring & "," & VariableNameDataArray(y, x)
                End If
            Next
            PrintLine(fnum, savestring)
            savestring = ""
        Next

        FileClose(fnum)

        MsgBox("Please Specify a Filename for the CLEVIR Record .csv file...")

        SaveFileDialog1.DefaultExt = ".csv"
        SaveFileDialog1.FileName = filename
        SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
        SaveFileDialog1.ShowDialog()

        If Len(SaveFileDialog1.FileName) > 0 And SaveFileDialog1.FileName <> filename Then
            FileCopy(filename, SaveFileDialog1.FileName)

        End If

    End Sub

    Private Function GetRecordedData(ByVal numValidVars As Integer,
                                           ByVal playbackMode As RecordPlayback.PlaybackStates) _
        As IGM_INCA_Comm.INCAData

        ' This function is called if we are playing back a recording.
        ' The first time it is called (when ReadNewDataFile = True), 
        ' it reads the entire file into memory and prepares an array for quick access.

        Dim MyIncaData As New IGM_INCA_Comm.INCAData

        ' We store playback data in this 2D array:
        '   Dimension(0..(NumSignals+1), 0..(NumRows-1))
        '   - Row dimension = each line in the file
        '   - Column dimension = 0 => Date/Time, 1 => Timestamp, 2.. => Signals-
        'Static MyDataArray(,) As String

        ' Because we only want to build these once, keep them Static.
        'Static columnIndexMap As Dictionary(Of String, Integer) = Nothing
        'Static x As Integer
        Dim y As Integer, i As Integer
        Dim direction As Integer

        Try
            If ReadNewDataFile Then
                SyncLock _playbackLock ' Ensure thread-safe access
                    ReadNewDataFile = False

                    ' Get filename and update UI through proper marshaling
                    Dim fileName As String = Nothing
                    Dim needCursorChange As Boolean = False

                    If RecordPlayback.InvokeRequired Then
                        RecordPlayback.Invoke(New Action(Sub()
                                                             fileName = RecordPlayback.PlayBackFileName
                                                             Cursor = Cursors.WaitCursor
                                                             RecordPlayback.Label2.Visible = True
                                                             RecordPlayback.Refresh()
                                                         End Sub))
                        needCursorChange = True
                    Else
                        fileName = RecordPlayback.PlayBackFileName
                        Cursor = Cursors.WaitCursor
                        RecordPlayback.Label2.Visible = True
                        RecordPlayback.Refresh()
                    End If

                    ' Read all lines from the file at once.
                    Dim lines As New List(Of String)
                    Using sr As New StreamReader(fileName)
                        While Not sr.EndOfStream
                            lines.Add(sr.ReadLine())
                        End While
                    End Using

                    ' Figure out how many rows total in the file
                    Dim totalLines As Integer = lines.Count
                    ' We need at least one line for headers
                    If totalLines = 0 Then
                        Throw New Exception("Playback file is empty.")
                    End If

                    ' Create a 2D array sized to (NumSignals+2) columns and totalLines rows
                    ReDim _playbackDataArray(UBound(MyIncaInterface.MyDisplaySignals) + 2, totalLines - 1)
                    _playbackColumnIndexMap = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                    ' Parse the header line to build a column index map for signals
                    Dim header As String() = lines(0).Split(","c)
                    _playbackColumnIndexMap = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

                    ' First 2 columns in the CSV are presumably date/time or similar:
                    '   column 0 -> MyDataArray(0, x)
                    '   column 1 -> MyDataArray(1, x)
                    ' So we only map signal columns (which start at index 2 in the CSV if present)
                    For col As Integer = 2 To header.Length - 1
                        Dim colName As String = header(col).Trim
                        If Not _playbackColumnIndexMap.ContainsKey(colName) Then
                            _playbackColumnIndexMap.Add(colName, col)
                        End If
                    Next

                    ' Fill MyDataArray with the lines (after the header)
                    ' Note: line 0 is the header, so start at line index=1
                    For row As Integer = 1 To totalLines - 1
                        Dim parts As String() = lines(row).Split(","c)

                        ' Defensive check: skip empty/short lines
                        If parts.Length < 2 Then Continue For

                        ' Store first two columns directly
                        _playbackDataArray(0, row) = parts(0)
                        _playbackDataArray(1, row) = parts(1)

                        ' Map each signal in MyIncaInterface.MyDisplaySignals
                        For sigIndex As Integer = 0 To UBound(MyIncaInterface.MyDisplaySignals)
                            Dim signalName As String = MyIncaInterface.MyDisplaySignals(sigIndex).SignalName
                            ' If our header dictionary knows this signal name, assign directly
                            If _playbackColumnIndexMap.ContainsKey(signalName) Then
                                Dim colIdx = _playbackColumnIndexMap(signalName)
                                If colIdx < parts.Length Then
                                    ' Validate and sanitize the data
                                    Dim value As String = parts(colIdx).Trim()
                                    If Double.TryParse(value, Nothing) Then
                                        _playbackDataArray(sigIndex + 2, row) = value
                                    Else
                                        ' If invalid, set to "0.0"
                                        _playbackDataArray(sigIndex + 2, row) = "0.0"
                                    End If
                                Else
                                    ' If column index is out of range, set 0.0
                                    _playbackDataArray(sigIndex + 2, row) = "0.0"
                                End If
                            Else
                                ' If not found, set 0.0
                                _playbackDataArray(sigIndex + 2, row) = "0.0"
                            End If
                        Next
                    Next

                    ' Number of rows we've read in
                    _playbackRowCounter = totalLines

                    ' Update UI elements through proper marshaling
                    If RecordPlayback.InvokeRequired Then
                        RecordPlayback.Invoke(New Action(Sub()
                                                             RecordPlayback.HScrollBar1.Maximum = _playbackRowCounter - 1
                                                             RecordPlayback.HScrollBar1.Minimum = 1
                                                             If needCursorChange Then
                                                                 Cursor = Cursors.Arrow
                                                             End If
                                                             RecordPlayback.Label2.Visible = False
                                                             RecordPlayback.Refresh()
                                                         End Sub))
                    Else
                        RecordPlayback.HScrollBar1.Maximum = _playbackRowCounter - 1
                        RecordPlayback.HScrollBar1.Minimum = 1
                        Cursor = Cursors.Arrow
                        RecordPlayback.Label2.Visible = False
                        RecordPlayback.Refresh()
                    End If
                End SyncLock
            End If

            ' Build the data to return
            ReDim MyIncaData.MyData(numValidVars - 1)
            ReDim MyIncaData.MyTime(numValidVars - 1)

            ' +/- direction based on playback state
            Select Case playbackMode
                Case RecordPlayback.PlaybackStates.PlaybackRun,
             RecordPlayback.PlaybackStates.PlaybackStepFwd
                    direction = 1
                Case RecordPlayback.PlaybackStates.PlaybackStepBack
                    direction = -1
                Case RecordPlayback.PlaybackStates.PlaybackScrolling
                    direction = 0
            End Select

            ' Handle boundary conditions
            If SaveLineNumber + direction = -1 Then
                SaveLineNumber = 2
            End If

            Dim maxValue As Integer = 0
            If RecordPlayback.InvokeRequired Then
                RecordPlayback.Invoke(New Action(Sub()
                                                     maxValue = RecordPlayback.HScrollBar1.Maximum
                                                 End Sub))
            Else
                maxValue = RecordPlayback.HScrollBar1.Maximum
            End If

            If SaveLineNumber + direction > maxValue Then
                SaveLineNumber = maxValue - direction
            End If

            ' Fill the IGM_INCA_Comm.INCAData structure
            i = 0
            For y = 2 To (numValidVars)
                MyIncaData.MyData(i) = _playbackDataArray(y, SaveLineNumber + direction)
                MyIncaData.MyTime(i) = _playbackDataArray(1, SaveLineNumber + direction)
                MyIncaData.MyStatus = True
                i += 1
            Next y

            ' Update the scrollbar and SaveLineNumber through proper marshaling
            If y = UBound(MyIncaInterface.MyDisplaySignals) + 3 Then
                Dim newLineNumber As Integer = SaveLineNumber + direction

                If RecordPlayback.InvokeRequired Then
                    RecordPlayback.Invoke(New Action(Sub()
                                                         RecordPlayback.HScrollBar1.Value = newLineNumber

                                                         ' Enable/disable buttons based on boundaries
                                                         If newLineNumber = RecordPlayback.HScrollBar1.Maximum Then
                                                             RecordPlayback.StepForward.Enabled = False
                                                             RecordPlayback.StepBack.Enabled = True
                                                             RecordPlayback.Reset.Enabled = True
                                                         End If

                                                         If newLineNumber = RecordPlayback.HScrollBar1.Minimum Then
                                                             RecordPlayback.PlayPauseButton.Enabled = True
                                                             RecordPlayback.StepForward.Enabled = True
                                                             RecordPlayback.StepBack.Enabled = False
                                                             RecordPlayback.Reset.Enabled = True
                                                         End If

                                                         RecordPlayback.Label1.Text = $"{newLineNumber} of {RecordPlayback.HScrollBar1.Maximum}"
                                                     End Sub))
                Else
                    RecordPlayback.HScrollBar1.Value = newLineNumber

                    ' Enable/disable buttons based on boundaries
                    If newLineNumber = RecordPlayback.HScrollBar1.Maximum Then
                        RecordPlayback.StepForward.Enabled = False
                        RecordPlayback.StepBack.Enabled = True
                        RecordPlayback.Reset.Enabled = True
                    End If

                    If newLineNumber = RecordPlayback.HScrollBar1.Minimum Then
                        RecordPlayback.PlayPauseButton.Enabled = True
                        RecordPlayback.StepForward.Enabled = True
                        RecordPlayback.StepBack.Enabled = False
                        RecordPlayback.Reset.Enabled = True
                    End If

                    RecordPlayback.Label1.Text = $"{newLineNumber} of {RecordPlayback.HScrollBar1.Maximum}"
                End If

                SaveLineNumber = newLineNumber
            End If

            Return MyIncaData

        Catch ex As Exception
            ' On error, return whatever data we have, and log the error
            HandleUserMessageLogging("GMRC", "GetRecordedData: " & ex.Message)
            Return MyIncaData
        End Try

    End Function

    Private Sub UpdateGonogoLabelColor(ByVal MyGridIndex As Integer, ByVal MyColor As Color)
        ' Called from MyBackgroundTasks routine - updates GO/NOGO label colors on the main screen.
        ' This method respects user suppression flags to prevent flashing after "Ignore All" is pressed.

        Static saveBackColor(0 To 15) As Color

        Try
            ' Initialize array if needed
            If saveBackColor Is Nothing Then
                ReDim saveBackColor(0 To 15)
            End If

            ' Validate index
            If MyGridIndex < 0 OrElse MyGridIndex > UBound(MyLabel) Then
                Return
            End If

            ' ====================================================================================
            ' OPTIMIZATION: Skip UI update if color hasn't changed (reduces UI overhead)
            ' ====================================================================================
            If saveBackColor(MyGridIndex) = MyColor Then
                Return ' Color unchanged - no UI update needed
            End If

            ' ====================================================================================
            ' Core Color Update Logic
            ' ====================================================================================
            MyLabel(MyGridIndex).BackColor = MyColor

            ' ====================================================================================
            ' Handle Special Label-Specific Logic
            ' ====================================================================================
            Select Case MyLabel(MyGridIndex).Text
                Case "INCA COMM Status"
                    HandleIncaCommStatusLabel(MyColor)

                Case "INCA Video Status"
                    HandleIncaVideoStatusLabel(MyGridIndex, MyColor)

                Case "CPU Status"
                    HandleCpuStatusLabel(MyColor)
            End Select

            ' ====================================================================================
            ' Logging: Log color change (we already know it changed - passed early return check)
            ' ====================================================================================
            If MyColor <> Color.Gray AndAlso MyLabel(MyGridIndex).Text <> "EOCM DTCs" Then
                Dim colorName As String = GetColorName(MyColor)
                HandleUserMessageLogging("GMRC",
                    $"UpdateGONOGOLabelColor: {MyLabel(MyGridIndex).Text} GO/NOGO is ----------------- {colorName}")
            End If

            ' Save current color for next comparison
            saveBackColor(MyGridIndex) = MyColor

        Catch ex As Exception
            HandleUpdateGonogoException(ex)
        End Try
    End Sub

    ' ====================================================================================
    ' HELPER METHODS: Extracted for clarity and maintainability
    ' ====================================================================================

    Private Sub HandleIncaCommStatusLabel(ByVal MyColor As Color)
        ' Stale-data state always tracks the label: RED while comms are lost, GREEN once re-established.
        ' The ignore flag only suppresses raising the operator alert.
        If MyColor = Color.Red Then
            If Not IgnoreLostDeviceForThisDrive AndAlso Not OverrideCommFailureInDebugMode Then
                BackgroundLoopCounterNotUpdating = True
            End If
            _MyCpuStaleData = True
        Else
            BackgroundLoopCounterNotUpdating = False
            _MyCpuStaleData = False
        End If
    End Sub

    Private Sub HandleIncaVideoStatusLabel(ByVal MyGridIndex As Integer, ByVal MyColor As Color)
        If Not MyIncaInterface.Recording Then
            ' Not recording - set to gray regardless of input color
            MyLabel(MyGridIndex).BackColor = Color.Gray
        Else
            ' Recording - apply the requested color
            MyLabel(MyGridIndex).BackColor = MyColor

            ' Only update video camera flags if alerts are not being ignored
            If Not IgnoreLostDeviceForThisDrive AndAlso Not IgnoreLostDeviceUntilNextRecordingSession Then
                If MyColor = Color.Red AndAlso Not OverrideCommFailureInDebugMode Then
                    VideoCameraNotUpdating = True
                Else
                    VideoCameraNotUpdating = False
                End If
            End If
        End If
    End Sub

    Private Sub HandleCpuStatusLabel(ByVal MyColor As Color)
        ' Update CPU stale data flag based on color
        _MyCpuStaleData = (MyColor = Color.Red)
    End Sub

    Private Function GetColorName(ByVal color As Color) As String
        ' Returns a friendly name for common colors, otherwise returns the color's Name property
        Select Case color
            Case Color.Red : Return "RED"
            Case Color.Green : Return "GREEN"
            Case Color.Gray : Return "GRAY"
            Case Color.Yellow : Return "YELLOW"
            Case Else : Return color.Name.ToUpper()
        End Select
    End Function

    Private Sub HandleUpdateGonogoException(ByVal ex As Exception)
        ' Centralized exception handling for UpdateGonogoLabelColor
        If _continueExecution = False Then
            Dim result As DialogResult
            Using topmostOwner As New Form() With {
                .TopMost = True,
                .Size = New System.Drawing.Size(1, 1),
                .StartPosition = FormStartPosition.CenterScreen,
                .ShowInTaskbar = False,
                .FormBorderStyle = FormBorderStyle.None,
                .Opacity = 0
            }
                topmostOwner.Show()
                result = MessageBox.Show(
                    topmostOwner,
                    $"UpdateGONOGOLabelColor: {ex.Message}",
                    "Update GONOGO Error",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Warning)
            End Using

            If result = DialogResult.Retry Then
                _continueExecution = True
                HandleUserMessageLogging("GMRC", $"UpdateGONOGOLabelColor: {ex.Message}")
            Else
                HandleUserMessageLogging("GMRC", $"UpdateGONOGOLabelColor: {ex.Message} - User selected cancel.")
                ExitApp("Complete")
            End If
        End If
    End Sub

    Private Sub UpdateGridColor(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal action As GridUpdateActions)

        'This routine is called from ProcessGrids (which already has SuspendLayout/ResumeLayout wrapping).
        'It handles setting the back color of individual cells based on threshold settings.
        'NOTE: SuspendLayout removed here - parent loop already handles layout suspension.

        Try

            Select Case action
                Case GridUpdateActions.FromHigh, GridUpdateActions.FromLow

                    'MyDGs(z).CellBackColor = MyDGs(z).DefaultCellBackColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    'MyDGs(z).CellForeColor = MyDGs(z).DefaultCellForeColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    myDGs(z).CurrentBackColor(x, y) = myDGs(z).DefaultCellBackColor(x, y)
                    myDGs(z).CurrentForeColor(x, y) = myDGs(z).DefaultCellForeColor(x, y)

                    myDGs(z).Rows(x - 1).Cells(y).Style.BackColor = myDGs(z).DefaultCellBackColor(x, y)
                    myDGs(z).Rows(x - 1).Cells(y).Style.ForeColor = myDGs(z).DefaultCellForeColor(x, y)


                Case GridUpdateActions.ToHigh

                    'MyDGs(z).CellBackColor = MyDGs(z).HighThreshBackColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    'MyDGs(z).CellForeColor = MyDGs(z).HighThreshForeColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)
                    myDGs(z).CurrentForeColor(x, y) = myDGs(z).HighThreshForeColor(x, y)

                    myDGs(z).Rows(x - 1).Cells(y).Style.BackColor = myDGs(z).HighThreshBackColor(x, y)
                    myDGs(z).Rows(x - 1).Cells(y).Style.ForeColor = myDGs(z).HighThreshForeColor(x, y)

                Case GridUpdateActions.ToLow

                    'MyDGs(z).CellBackColor = MyDGs(z).LowThreshBackColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    'MyDGs(z).CellForeColor = MyDGs(z).LowThreshForeColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    myDGs(z).CurrentBackColor(x, y) = myDGs(z).LowThreshBackColor(x, y)
                    myDGs(z).CurrentForeColor(x, y) = myDGs(z).LowThreshForeColor(x, y)

                    myDGs(z).Rows(x - 1).Cells(y).Style.BackColor = myDGs(z).LowThreshBackColor(x, y)
                    myDGs(z).Rows(x - 1).Cells(y).Style.ForeColor = myDGs(z).LowThreshForeColor(x, y)

            End Select

        Catch ex As Exception

            If _continueExecution = False Then
                Dim result As DialogResult
                Using topmostOwner As New Form() With {
                    .TopMost = True,
                    .Size = New System.Drawing.Size(1, 1),
                    .StartPosition = FormStartPosition.CenterScreen,
                    .ShowInTaskbar = False,
                    .FormBorderStyle = FormBorderStyle.None,
                    .Opacity = 0
                }
                    topmostOwner.Show()
                    result = MessageBox.Show(
                        topmostOwner,
                        "UpdateGridColor: " & ex.Message,
                        "Grid Update Error",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Warning)
                End Using
                If result = DialogResult.Retry Then
                    _continueExecution = True
                    HandleUserMessageLogging("GMRC", "UpdateGridColor: " & ex.Message)
                Else
                    HandleUserMessageLogging("GMRC", "UpdateGridColor: " & ex.Message & " User selected cancel.")
                    ExitApp("Complete")
                End If
            End If
        End Try

    End Sub

    Private Sub GmResidentClient_Activated(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Activated

    End Sub

    Private Sub GmResidentClient_BackgroundImageChanged(ByVal sender As Object, ByVal e As EventArgs) Handles Me.BackgroundImageChanged
    End Sub

    Private Sub GmResidentClient_CausesValidationChanged(ByVal sender As Object, ByVal e As EventArgs) Handles Me.CausesValidationChanged
    End Sub

    Private Sub GmResidentClient_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Click

        'When you click in the GmResidentClient form (this form is the container for the OnVehicleScreen and other forms when
        'not emulating an in vehicle PC), this code re-displays whatever other form was visible at the same location as the
        'GmResidentClient form and brings the form to the front.

        'This supports moving the CLEVIR display like a regular movable window when running on a laptop for instance, rather then having
        'the display stationary in the top left corner of the screen...

        If OperatingMode <> OperatingModes.ResOnVpc Then

            OnVehicleScreen.Top = Top + 60
            OnVehicleScreen.Left = Left + 12
            OnVehicleScreen.Activate()
            OnVehicleScreen.BringToFront()

            LoginForm.Top = Top + 60
            LoginForm.Left = Left + 12

            SelectDisplays.Top = Top + 60
            SelectDisplays.Left = Left + 12

        End If

    End Sub

    Private Sub GmResidentClient_Enter(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Enter

    End Sub

    Private Sub GmResidentClient_FormClosed(ByVal sender As Object, ByVal e As FormClosedEventArgs) Handles Me.FormClosed

        If MyTimeSyncProvider IsNot Nothing Then
            Try
                MyTimeSyncProvider.Stop()
            Catch exStopSync As Exception
                HandleUserMessageLogging("GMRC", $"GmResidentClient_FormClosed: MyTimeSyncProvider.Stop failed - {exStopSync.Message}")
            End Try
        End If

        ShutdownEventProcessing()

    End Sub

    'Private Sub BackgroundWorker1_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles BackgroundWorker1.DoWork

    '    'the background worker handles the main execution loop

    '    _backgroundTasks = New BackgroundTasks(AddressOf MyBackgroundTasks)
    '    BeginInvoke(_backgroundTasks)

    'End Sub

    Private Sub BackgroundWorker1_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        ' Check for cancellation before starting the task
        If BackgroundWorker1.CancellationPending Then
            e.Cancel = True
            Return
        End If

        ' Fire-and-forget async execution on UI thread with cancellation check
        BeginInvoke(New Action(Async Sub()
                                   Try
                                       ' Run the background tasks
                                       Await MyBackgroundTasks()
                                   Catch ex As Exception
                                       ' Log or handle exceptions if needed
                                       If TypeOf ex Is TaskCanceledException Then
                                           e.Cancel = True
                                           HandleUserMessageLogging("GMRC", "Background task was canceled.")
                                       Else
                                           HandleUserMessageLogging("GMRC", "Background task error: " & ex.Message)
                                       End If
                                   End Try
                               End Sub))
    End Sub
    Private Sub GmResidentClient_FormClosing(ByVal sender As Object,
                                             ByVal e As FormClosingEventArgs) _
        Handles Me.FormClosing
        ' ══════════════════════════════════════════════════════════════
        ' ✅ FIX 1: Check if exit is already in progress (prevent recursion)
        ' ══════════════════════════════════════════════════════════════
        If exitInProgress Then
            HandleUserMessageLogging("GMRC", "FormClosing: Exit in progress, allowing close")
            e.Cancel = False  ' ← Allow form to close
            Return
        End If

        ' ══════════════════════════════════════════════════════════════
        ' ✅ FIX 2: Special handling for in-vehicle PC (hide on first close)
        ' ══════════════════════════════════════════════════════════════
        If OperatingMode = OperatingModes.ResOnVpc AndAlso Me.Visible Then
            HandleUserMessageLogging("GMRC", "FormClosing: Hiding form (in-vehicle mode)")
            Me.Hide()
            e.Cancel = True  ' ← Prevent actual close
            Return
        End If

        ' ══════════════════════════════════════════════════════════════
        ' ✅ FIX 3: Normal exit path (all other cases)
        ' ══════════════════════════════════════════════════════════════
        HandleUserMessageLogging("GMRC", "FormClosing: Starting exit sequence")

        If ExitApp() Then
            e.Cancel = False  ' ← Allow close on success
        Else
            e.Cancel = True   ' ← Cancel close if user cancelled exit
        End If
    End Sub


    Private Sub GmResidentClient_GotFocus(ByVal sender As Object, ByVal e As EventArgs) Handles Me.GotFocus

    End Sub

    Private Sub GmResidentClient_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles Me.KeyDown

    End Sub

    Private Async Sub GmResidentClient_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        If _hasInitialized Then
            HandleUserMessageLogging("GMRC", "Form_Load: Already initialized, skipping")
            Return
        End If

        If exitInProgress Then
            HandleUserMessageLogging("GMRC", "Form_Load: Exit in progress, aborting load")
            Return
        End If

        ' ═══════════════════════════════════════════════════════════════
        ' STEP 1: Handle operating mode toggle text
        ' ═══════════════════════════════════════════════════════════════
        Dim modeFile As String = Path.Combine(My.Application.Info.DirectoryPath, "OperatingMode.txt")
        ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)"

        Try
            If File.Exists(modeFile) Then
                Dim firstLine As String = File.ReadLines(modeFile).FirstOrDefault()
                Dim textstr As String = If(firstLine, String.Empty).Trim().ToUpperInvariant()

                If textstr.Contains("DEVELOPMENT") Then
                    ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)"
                Else
                    ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Development)"
                End If
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GmResidentClient_Load: Failed reading OperatingMode.txt - " & ex.Message)
        End Try

        ' ════════════════════════════════════════════════════════════
        ' ✅ FIX: Start initialization ONCE (which includes login)
        ' ════════════════════════════════════════════════════════════
        Try
            Await Initialize()  ' This handles login internally
            _hasInitialized = True  ' ← Mark as initialized AFTER success
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Init failed: {ex.Message}", DisplayMsgBox)
            ExitApp("Complete")
            Return  ' ← Don't continue after failed init
        End Try

        InitializeEventProcessing()
        InitializationMonitor()

    End Sub

    Private Sub GmResidentClient_LocationChanged(ByVal sender As Object, ByVal e As EventArgs) Handles Me.LocationChanged

    End Sub

    Private Sub GmResidentClient_LostFocus(ByVal sender As Object, ByVal e As EventArgs) Handles Me.LostFocus

    End Sub

    Private Sub GmResidentClient_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Me.MouseDown

    End Sub

    Private Sub GmResidentClient_MouseEnter(ByVal sender As Object, ByVal e As EventArgs) Handles Me.MouseEnter

        'If we are not resident on a pc that is connected to the vehicle and are not running on a suitcase logger connected
        'to a vehicle, we will display the OnVehicleScreen within the GmResidentClient window whenever the mouse enters
        'the GmResidentClient window. So, the OnVehicleScreen is superimposed on the GmResidentClient window.

        If _initializing = False Then

            If OperatingMode <> OperatingModes.ResOnVpc Then

                OnVehicleScreen.Top = Top + 60
                OnVehicleScreen.Left = Left + 12
                OnVehicleScreen.Activate()
                OnVehicleScreen.BringToFront()

                LoginForm.Top = Top + 60
                LoginForm.Left = Left + 12

                SelectDisplays.Top = Top + 60
                SelectDisplays.Left = Left + 12

                If LoginForm.Visible = True Then
                    LoginForm.Activate()
                    LoginForm.BringToFront()
                End If

                If SelectDisplays.Visible = True Then
                    SelectDisplays.Activate()
                    SelectDisplays.BringToFront()
                End If

            End If

        End If
    End Sub

    Private Sub GmResidentClient_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Me.MouseUp

    End Sub

    Private Sub GmResidentClient_Move(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Move

        'If we are not resident on a pc that is connected to the vehicle and are not running on a suitcase logger connected
        'to a vehicle, when we move the GmResidentClient window, the superimposed, OnVehicleScreen must move with it.

        If OperatingMode <> OperatingModes.ResOnVpc Then

            OnVehicleScreen.Top = Top + 60
            OnVehicleScreen.Left = Left + 12

            LoginForm.Top = Top + 60
            LoginForm.Left = Left + 12

            SelectDisplays.Top = Top + 60
            SelectDisplays.Left = Left + 12

        End If

    End Sub

    Private Sub GmResidentClient_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles Me.Paint

        'If we are not resident on a pc that is connected to the vehicle and are not running on a suitcase logger connected
        'to a vehicle, when the GmResidentClient window is repainted, we make sure that the superimposed, OnVehicleScreen is
        'properly located within the boundaries of the GmResidentClient window.

        If OperatingMode <> OperatingModes.ResOnVpc Then

            OnVehicleScreen.Top = Top + 60
            OnVehicleScreen.Left = Left + 12

            LoginForm.Top = Top + 60
            LoginForm.Left = Left + 12

            SelectDisplays.Top = Top + 60
            SelectDisplays.Left = Left + 12

        End If
    End Sub

    Private Sub GmResidentClient_Resize(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Resize

    End Sub

    Private Sub GmResidentClient_ResizeEnd(ByVal sender As Object, ByVal e As EventArgs) Handles Me.ResizeEnd

    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Public Sub ShowCustomScreen(ByVal screenName As String)

        'Called from various places when a custom screen is to be displayed.  Displays, positions and sizes the proper form
        'based on the custom screen selection.

        Select Case screenName
            Case "LKA Screen"
                LkaForm.Top = 45
                LkaForm.Show()
                LkaForm.BringToFront()

                If MyTdGraphicsContainer IsNot Nothing Then
                    MyTdGraphicsContainer.Show()
                    MyTdGraphicsContainer.Left = (LkaForm.Left + LkaForm.Width) - MyTdGraphicsContainer.Width - 10
                    MyTdGraphicsContainer.Top = (LkaForm.Top + LkaForm.Height) - MyTdGraphicsContainer.Height - 40
                    MyTdGraphicsContainer.BringToFront()
                End If

            Case "Secret Squirrel Screen"
                TargetStatusDisplay.Top = 45
                TargetStatusDisplay.Show()
                TargetStatusDisplay.BringToFront()

                If MyTdGraphicsContainer IsNot Nothing Then
                    MyTdGraphicsContainer.Show()
                    MyTdGraphicsContainer.Left = (TargetStatusDisplay.Left + TargetStatusDisplay.Width) - MyTdGraphicsContainer.Width - 10
                    MyTdGraphicsContainer.Top = (TargetStatusDisplay.Top + TargetStatusDisplay.Height) - MyTdGraphicsContainer.Height - 40
                    MyTdGraphicsContainer.BringToFront()
                End If
            Case "Pedestrian Status Display"
                PedestrianStatusDisplay.Top = 45
                PedestrianStatusDisplay.Show()
                PedestrianStatusDisplay.BringToFront()

                If MyTdGraphicsContainer IsNot Nothing Then
                    MyTdGraphicsContainer.Show()
                    MyTdGraphicsContainer.Left = (PedestrianStatusDisplay.Left + PedestrianStatusDisplay.Width) - MyTdGraphicsContainer.Width - 10
                    MyTdGraphicsContainer.Top = (PedestrianStatusDisplay.Top + PedestrianStatusDisplay.Height) - MyTdGraphicsContainer.Height - 40
                    MyTdGraphicsContainer.BringToFront()
                End If
            Case "Fusion Status Display"
                FusionStatusDisplay.Top = 45
                FusionStatusDisplay.Show()
                FusionStatusDisplay.BringToFront()
            Case "CoPilot Status Display"
                CopilotStatusDisplay.Top = 45
                CopilotStatusDisplay.Show()
                CopilotStatusDisplay.BringToFront()

            Case "LMFR Global A Status Display"
                LmfrStatusDisplayGlobalA.Top = 45
                LmfrStatusDisplayGlobalA.Show()
                LmfrStatusDisplayGlobalA.BringToFront()

            Case "LMFR High Content Status Display"
                LmfrStatusScreenHc.Top = 45
                LmfrStatusScreenHc.Show()
                LmfrStatusScreenHc.BringToFront()

            Case "INCA Hardware Status"
                DeviceStatus.Top = 45
                DeviceStatus.Show()
                DeviceStatus.BringToFront()
            Case "Top Down View"
                If MyTdGraphicsContainer.Visible = False Then
                    MyTdGraphicsContainer.Left = Left
                    MyTdGraphicsContainer.Top = Top
                End If
                MyTdGraphicsContainer.ControlBox = True
                MyTdGraphicsContainer.Show()
                MyTdGraphicsContainer.BringToFront()
            Case "Create New Display"
                Dim newDisplayName As String
                newDisplayName = InputBox("Enter Display Name", "Display Configuration")
            Case Else
                HandleUserMessageLogging("GMRC", "ShorCustomScreen: Invalid Screen Name", DisplayMsgBox)
        End Select

    End Sub

    Private Sub GmResidentClient_Shown(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Shown

        If OperatingMode <> OperatingModes.ResOnVpc Then

            OnVehicleScreen.Top = Top + 60
            OnVehicleScreen.Left = Left + 12
            OnVehicleScreen.Activate()
            OnVehicleScreen.BringToFront()

            LoginForm.Top = Top + 60
            LoginForm.Left = Left + 12

            SelectDisplays.Top = Top + 60
            SelectDisplays.Left = Left + 12

        End If

    End Sub

    Private Sub GmResidentClient_SizeChanged(ByVal sender As Object, ByVal e As EventArgs) Handles Me.SizeChanged

    End Sub

    Private Sub GmResidentClient_HandleCreated(sender As Object, e As EventArgs) Handles Me.HandleCreated

    End Sub

    Private Sub GmResidentClient_MarginChanged(sender As Object, e As EventArgs) Handles Me.MarginChanged

    End Sub

End Class

