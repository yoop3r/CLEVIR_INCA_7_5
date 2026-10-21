
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

    'GmResidentClient is split across Partial Class files by responsibility. Class-level fields, constants and
    'nested types stay in this file so their initialization order is preserved.
    '  GM_ResidentClient.vb                 - fields/types, form lifecycle events, GetCurrentConfigData
    '  GM_ResidentClient.Startup.vb         - Initialize and INCA/camera/login startup sequence
    '  GM_ResidentClient.Shutdown.vb        - ExitApp, INCA shutdown/restart, file compression and upload prep
    '  GM_ResidentClient.Configuration.vb   - config.xml / user config / Lidar configuration read & write
    '  GM_ResidentClient.SignalList.vb      - signal list read (CSV), write and display signal registration
    '  GM_ResidentClient.DisplayForms.vb    - dynamic display forms, grids, menus, context menus, tab controls
    '  GM_ResidentClient.DeviceHealth.vb    - device status, lost-device detection and reinitialization
    '  GM_ResidentClient.Mileage.vb         - odometer/geofence status and annotation file mileage
    '  GM_ResidentClient.Recording.vb       - INCA/WAV/alternate recording, progress bar, CLEVIR record/playback data
    '  GM_ResidentClient.StatusScreens.vb   - custom status screens (ALC, CoPilot, LMFR, Fusion, LKA, Target, Pedestrian)
    '  GM_ResidentClient.BackgroundLoop.vb  - MyBackgroundTasks, ProcessGrids, GO/NOGO and grid color updates
    '  GM_ResidentClient.ProcessKiller.vb   - ProcessKiller watchdog thread and killer form
    '  GM_ResidentClient.InitMonitor.vb     - InitializationMonitor progress UI
    '  GM_ResidentClient.MenuHandlers.vb    - designer menu/control event handlers
    'Form-independent logic extracted into Friend helper classes (unit tested in CLEVIR_INCA_7_5.Tests):
    '  PropertyGeofence, DisplayValueFormatter, ResidentClientText, ConfigValueReader, ResidentConfigRules,
    '  ResidentConfigXmlWriter (+ ResidentConfigSnapshot), LidarExtrinsicParser, SignalListFile, SignalListCsvReader

    'Note: the former CLEVIR 7.2
    'repository; changes here no longer need to be mirrored into separate copies.

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
    ' Property geofence corner constants live in PropertyGeofence.vb.

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

    Private _rotationInProgress As Boolean = False


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

