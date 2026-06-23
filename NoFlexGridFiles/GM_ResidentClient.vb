
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
    Private _MyUploadData As ToolStripMenuItem
    Private _MyRecordPlayback As ToolStripMenuItem
    Private _lccActiveStartingMileage As Double
    Private _lccActiveMileageTotal As Double  ' Total LCC Active
    Private _lccActiveMileage As Double        ' Per-session LCC Acitve (current)
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
    Public SaveBaseDataCollectionPath As String
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
    Public Property SaveSwitchToName As String
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
    Private Shared lMyDeviceStatus As Boolean = True
    Private Shared videoMessageLastDisplayed As Boolean = True
    Private Shared dataMessageLastDisplayed As Boolean = True

    Private Shared _initToastId As Guid = Guid.Empty
    Private ReadOnly _initToastIds As New List(Of Guid)()
    'Private ReadOnly _gridRefreshAt As New Dictionary(Of String, DateTime)()
    Private ReadOnly _gridRefreshAt As New ConcurrentDictionary(Of String, DateTime)()
    Private ReadOnly GridMinRefresh As TimeSpan = TimeSpan.FromMilliseconds(200)
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
    Private Sub SetInitialDisplayProperties()


        'Called from initialize routine, sizes and positions various controls on
        'both the GmResidentClient form and the OnVehicleScreen form...

        Dim MyDirectories As String()
        Dim x As Integer
        Dim tempstr As String

        OnVehicleScreen.GroupBox1.Visible = True
        OnVehicleScreen.GroupBox1.Left = 1
        OnVehicleScreen.GroupBox1.Top = 0
        OnVehicleScreen.GroupBox1.Width = DefaultFormWidth - 5
        OnVehicleScreen.GroupBox1.Height = DefaultFormHeight - 38

        ' ✅ ADD THIS: Sync UI AFTER controls are created
        SyncUIFromConfig()

        Width = DefaultFormWidth
        Height = DefaultFormHeight

        OnVehicleScreen.Hide()
        'OnVehicleScreen.BringToFront()

        GroupBox1.Visible = True
        GroupBox1.Left = 1
        GroupBox1.Top = 30
        GroupBox1.Width = DefaultFormWidth - 18
        GroupBox1.Height = DefaultFormHeight - 10

        'Displays are set up differently depending on whether or not we are In Vehicle.
        'Operating modes are reflective of which PC we are running on...

        '_VPC indicates modes where we are running on a vehicle
        'VPC - Vehicle PC

        'Other modes would be configuration modes where we are running the application on
        'our laptops...  

        If OperatingMode <> OperatingModes.ResOnVpc Then
            Height = DefaultFormHeight + 30
        Else
            Height = DefaultFormHeight - 30
        End If

        Button1.Left = Width - Button1.Width - 20
        Button1.Top = Height - Button1.Height - 75

        Label2.Parent = GroupBox1
        Label2.Left = 5
        Label2.Top = GroupBox1.Top + ((GroupBox1.Height / 2) - (Label2.Height))
        Label2.Width = GroupBox1.Width - 10

        OnVehicleScreen.Label17.Left = OnVehicleScreen.Button1.Left

        RecordPlayback.GroupBox2.Text = "PLAYBACK"

        'Here is where we add the user names that are associated with each configured INCA User
        'to the toolstripcombobox selection list.  Based on current use cases, this is really no longer
        'applicable...

        ToolStripComboBox3.Items.Add("Add Custom INCA Setup")

        If Directory.Exists(ETAS_USER_PATH) Then
            MyDirectories = Directory.GetDirectories(ETAS_USER_PATH)

            For x = 0 To UBound(MyDirectories)
                tempstr = Mid(MyDirectories(x), InStr(MyDirectories(x), "\User\") + 6, Len(MyDirectories(x)))
                ToolStripComboBox3.Items.Add(tempstr)

                If UCase(tempstr) = EtasDefaultUserName Then
                    ToolStripComboBox3.SelectedIndex = x + 1
                    ToolStripComboBox3.Text = UCase(tempstr)
                    SaveSwitchToName = ToolStripComboBox3.Text
                End If
            Next

        End If

        _displayRefreshRate = DefaultDisplayRefreshRateMsec
        ToolStripComboBox1.Text = CStr(_displayRefreshRate)

        DataCollectionRate = DefaultDataCollectionRateMsec
        ToolStripComboBox2.Text = CStr(DataCollectionRate)

    End Sub

    ''' <summary>
    ''' Handles graceful application shutdown with proper cleanup sequencing.
    ''' Prevents race conditions using atomic flags and ensures all resources are released.
    ''' </summary>
    ''' <param name="exitParameter">Optional parameter: "" = show dialog, "Complete" = force exit</param>
    ''' <returns>True if exit completed successfully, False if cancelled by user</returns>
    Public Function ExitApp(Optional ByVal exitParameter As String = "") As Boolean
        ' ════════════════════════════════════════════════════════════════
        ' ✅ FIX 1: ATOMIC FLAG CHECK - Prevent race conditions
        ' ════════════════════════════════════════════════════════════════

        ' ✅ ATOMIC FLAG: Set to True only if it was False
        If Not System.Threading.Interlocked.CompareExchange(exitInProgress, True, False) = False Then
            ' Race condition: Another thread beat us to it
            HandleUserMessageLogging("GMRC", "ExitApp: Race detected, deferring to other thread")
            Return True
        End If

        ' Local working variables
        Dim uflags As Long = 0
        Dim selectedOption As ExitOption = ExitOption.CancelExit

        Try
            HandleUserMessageLogging("GMRC", $"ExitApp ({If(String.IsNullOrEmpty(exitParameter), "User Initiated", exitParameter)}) called...")

            ' ══════════════════════════════════════════════════════════════
            ' STOP BACKGROUND WORKERS FIRST
            ' ══════════════════════════════════════════════════════════════
            _enableMyBackgroundTasks = False
            FormDisplayed = False

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 2: CANCEL ONGOING OPERATIONS
            ' ════════════════════════════════════════════════════════════════
            If _initCts IsNot Nothing Then
                Try
                    _initCts.Cancel()
                    HandleUserMessageLogging("GMRC", "ExitApp: Cancelled ongoing initialization")
                Catch ex As Exception
                    ' Ignore cancellation errors
                End Try
            End If

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 3: STOP BACKGROUND WORKERS
            ' ════════════════════════════════════════════════════════════════
            _enableMyBackgroundTasks = False

            ' Cancel background tasks immediately for faster shutdown
            If _backgroundTasksCts IsNot Nothing Then
                Try
                    _backgroundTasksCts.Cancel()
                    HandleUserMessageLogging("GMRC", "ExitApp: Cancelled background tasks via CancellationToken")
                Catch ex As Exception
                    ' Ignore cancellation errors
                End Try
            End If

            FormDisplayed = False
            Cursor = Cursors.WaitCursor
            HandleUserMessageLogging("GMRC", "ExitApp: Stopping background tasks...")

            ' Wait for background worker to stop (with timeout)
            If BackgroundWorker1.IsBusy Then
                HandleUserMessageLogging("GMRC", "Waiting for background tasks to complete...")
                Dim startTime As DateTime = DateTime.Now
                Const timeoutMs As Integer = 10000 ' 10 second timeout

                While BackgroundWorker1.IsBusy AndAlso (DateTime.Now - startTime).TotalMilliseconds < timeoutMs
                    Application.DoEvents()
                    Thread.Sleep(100)
                End While

                If BackgroundWorker1.IsBusy Then
                    HandleUserMessageLogging("GMRC", "Warning: Background tasks did not stop within timeout")
                End If
            End If

            ' Dispose the background tasks CancellationTokenSource
            If _backgroundTasksCts IsNot Nothing Then
                _backgroundTasksCts.Dispose()
                _backgroundTasksCts = Nothing
            End If

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 4: SHUTDOWN ALL LIDAR DEVICES
            ' ════════════════════════════════════════════════════════════════
            ShutdownAllLidarDevices()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 5: CLOSE DISPLAY FORMS (AFTER BACKGROUND TASKS STOPPED)
            ' ════════════════════════════════════════════════════════════════
            CloseDisplayForms()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 6: HANDLE COMPRESSION (IF CONFIGURED)
            ' ════════════════════════════════════════════════════════════════
            If CompressMF4 Or CompressPCAP Or CompressASC Or CompressVSB Then
                CompressOrphanedFiles()      ' Queue orphaned files
                Handle7ZipProcess()          ' Wait for completion
            Else
                HandleUserMessageLogging("GMRC", "ExitApp: All compression types disabled - skipping orphaned file compression")
            End If

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 7: USER CONFIRMATION (IF NEEDED)
            ' ════════════════════════════════════════════════════════════════
            If String.IsNullOrEmpty(exitParameter) Then
                ' Display the ExitAppForm modally
                Using exitForm As New ExitAppForm()
                    exitForm.TopMost = True
                    exitForm.ShowDialog()
                    selectedOption = exitForm.SelectedExitOption

                    Select Case selectedOption
                        Case ExitOption.CancelExit
                            HandleUserMessageLogging("GMRC", "ExitApp: Exit Cancelled by user")
                            ExitApp = False
                            Return False

                        Case ExitOption.ExitClevirAndCloseInca
                            HandleUserMessageLogging("GMRC", "ExitApp: Exit CLEVIR and Close INCA selected")
                            CheckForExperiment = False
                            MyIncaInterface?.CloseINCA()
                            If Not PlaybackMode Then CopyINCADatabase()
                            ' ✅ SET FLAG: User committed to exiting
                            exitInProgress = True
                            HandleUserMessageLogging("GMRC", $"ExitApp: exitInProgress set to True for option {selectedOption}")

                        Case ExitOption.ExitClevirOnly
                            HandleUserMessageLogging("GMRC", "ExitApp: Exit CLEVIR Only selected (leaving INCA running)")
                            CheckForExperiment = False
                            ' ✅ SET FLAG: User committed to exiting
                            exitInProgress = True
                            HandleUserMessageLogging("GMRC", $"ExitApp: exitInProgress set to True for option {selectedOption}")

                        Case ExitOption.ExitClevirCloseIncaShutdownWindows
                            HandleUserMessageLogging("GMRC", "ExitApp: Exit CLEVIR, Close INCA and Shutdown Windows selected")
                            CheckForExperiment = False
                            MyIncaInterface?.CloseINCA()
                            If Not PlaybackMode Then CopyINCADatabase()
                            ShutdownWindows = True
                            ' ✅ SET FLAG: User committed to exiting
                            exitInProgress = True
                            HandleUserMessageLogging("GMRC", $"ExitApp: exitInProgress set to True for option {selectedOption}")
                        Case Else
                            ' Unknown option - treat as cancel
                            ExitApp = False
                            Return False
                    End Select
                End Using

            ElseIf exitParameter = "Complete" Then
                ' Force a complete shutdown without user interaction
                HandleUserMessageLogging("GMRC", "ExitApp: Exit complete - Closing INCA")
                CheckForExperiment = False
                MyIncaInterface?.CloseINCA()
            End If

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 8: VALIDATE CRITICAL PATHS
            ' ════════════════════════════════════════════════════════════════
            If String.IsNullOrEmpty(BaseDataCollectionPath) Then
                HandleUserMessageLogging("GMRC", "BaseDataCollectionPath is null/empty during exit - using fallback")
                BaseDataCollectionPath = My.Application.Info.DirectoryPath
            End If

            If String.IsNullOrEmpty(FinalPathToSaveData) Then
                FinalPathToSaveData = Path.Combine(BaseDataCollectionPath, "Data", "gmcsv" & VehicleNumber)
                HandleUserMessageLogging("GMRC", $"FinalPathToSaveData set to: {FinalPathToSaveData}")
            End If

            If String.IsNullOrEmpty(VehicleNumber) Then
                HandleUserMessageLogging("GMRC", "VehicleNumber is null/empty during exit - using fallback")
                VehicleNumber = "UNKNOWN"
            End If

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 9: FILE OPERATIONS & HOUSEKEEPING
            ' ════════════════════════════════════════════════════════════════
            Copy_GM_INCA_CommLog_File()
            UserStatusInfo.Hide()

            ' Quit external applications if they were started
            If CanalyzerCaptureStarted Then QuitCanalyzer()
            If VehicleSpyCaptureStarted Then QuitVehicleSpy()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX: Set _killProcesses based on user's selected exit option
            ' ════════════════════════════════════════════════════════════════
            ' Stop killer thread
            If _MyKillerThread IsNot Nothing AndAlso _MyKillerThread.IsAlive Then
                ' Only kill INCA processes if user explicitly chose that option
                Select Case selectedOption
                    Case ExitOption.ExitClevirOnly
                        _killProcesses = False  ' ✅ Keep INCA running
                        HandleUserMessageLogging("GMRC", "Killer thread: _killProcesses = False (Exit CLEVIR Only)")
                    Case ExitOption.ExitClevirAndCloseInca, ExitOption.ExitClevirCloseIncaShutdownWindows
                        _killProcesses = True   ' ✅ Terminate INCA
                        HandleUserMessageLogging("GMRC", "Killer thread: _killProcesses = True (Close INCA)")
                    Case Else
                        ' exitParameter = "Complete" or unknown scenario - default to killing INCA
                        _killProcesses = True
                        HandleUserMessageLogging("GMRC", "Killer thread: _killProcesses = True (Default)")
                End Select

                _MyKillerThread.Join(2000) ' Wait up to 2 seconds
                HandleUserMessageLogging("GMRC", "Stopped killer thread.")
            End If

            ' Stop recording monitor task
            StopRecordingMonitorTask()

            ' Write configuration changes
            'HandleUserMessageLogging("GMRC", "Writing changes to signal list and user config file...")
            'WriteLoginIDListFile()
            SaveSignalListChanges()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 10: AUDIO-TO-TEXT CONVERSION (IF ENABLED)
            ' ════════════════════════════════════════════════════════════════
            If AudioToTextConversion Then
                Try
                    Dim progressForm As New AudioToTextProgressForm()
                    LoginForm.Hide()
                    OnVehicleScreen.Hide()

                    progressForm.Show()
                    Dim conversionTask = progressForm.RunConversionAsync()

                    While Not progressForm.IsCompleted
                        Application.DoEvents()
                        Thread.Sleep(100)
                    End While

                    conversionTask.Wait(TimeSpan.FromSeconds(5))
                    progressForm.Close()
                    progressForm.Dispose()
                    Thread.Sleep(2000)

                    HandleUserMessageLogging("GMRC", "Audio-to-text conversion completed successfully")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Audio conversion error: {ex.Message}")
                End Try
            End If

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 11: FILE MANIPULATIONS FOR UPLOAD
            ' ════════════════════════════════════════════════════════════════
            If HaveRecorded Then
                HandleFileManipulationsForUpload()
            End If

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 12: OXTS INTERFACE CLEANUP
            ' ════════════════════════════════════════════════════════════════
            If MyOxtsInterface IsNot Nothing Then
                Try
                    MyOxtsInterface.OxtsStopListening()
                    HandleUserMessageLogging("GMRC", "OXTS interface stopped")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Error stopping OXTS: {ex.Message}")
                End Try
            End If

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 13: INCA RESOURCES CLEANUP
            ' ════════════════════════════════════════════════════════════════
            CleanUpINCAResources()
            HandleUserMessageLogging("GMRC", "Cleaned up INCA resources.")

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 14: WINDOWS SHUTDOWN/RESTART (IF REQUESTED)
            ' ════════════════════════════════════════════════════════════════
            HandleShutdownOrRestart(uflags)

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 15: FINAL STATUS NOTIFICATION
            ' ════════════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", "CLEVIR Exit in progress, performing housekeeping...",,, FlashMsgOn)
            HandleUserMessageLogging("GMRC", "Exit Complete.",, )
            StatusNotifier.Toast("Exit Complete", "CLEVIR", durationMs:=1000, ensureMainOnTop:=False)
            Thread.Sleep(1000)

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 16: CLOSE FORM BEFORE FORCEFUL EXIT
            ' ════════════════════════════════════════════════════════════════
            If Not Me.IsDisposed Then
                Me.Close()  ' Triggers FormClosing (which now allows close)
            End If

            ExitApp = True
            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ExitApp: Unexpected error - {ex.Message}")
            ExitApp = False
            Return False

        Finally
            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 17: RESET FLAG IN FINALLY BLOCK (ALWAYS RUNS)
            ' ════════════════════════════════════════════════════════════════
            If selectedOption = ExitOption.CancelExit Then
                ' Only reset flag if user cancelled - allow exit to complete otherwise
                exitInProgress = False
                Cursor = Cursors.Arrow
                HandleUserMessageLogging("GMRC", "ExitApp: exitInProgress reset (user cancelled)")
            Else
                HandleUserMessageLogging("GMRC", "ExitApp: exitInProgress remains True (exit committed)")
                ' Force termination (point of no return)
                Application.Exit()
                Environment.Exit(0)
            End If
        End Try
    End Function

    Private Sub CompressOrphanedFiles()
        ' Find all orphaned files (those without corresponding .zip) and compress them
        ' based on configuration settings.

        Dim dataRoot = Path.Combine(BaseDataCollectionPath, "Data", "gmcsv" & VehicleNumber)

        If Not Directory.Exists(dataRoot) Then
            HandleUserMessageLogging("GMRC", "CompressOrphanedFiles: Data directory not found")
            Return
        End If

        Dim orphanedCount As Integer = 0

        ' ════════════════════════════════════════════════════════════
        ' ✅ FIX: Check CompressMF4 flag BEFORE processing .mf4 files
        ' ════════════════════════════════════════════════════════════
        If CompressMF4 Then
            For Each mf4File In Directory.GetFiles(dataRoot, "*.mf4", SearchOption.AllDirectories)
                Dim zipFile = Path.ChangeExtension(mf4File, ".zip")
                If Not File.Exists(zipFile) Then
                    HandleUserMessageLogging("GMRC", $"Found orphaned MF4: {Path.GetFileName(mf4File)}")
                    CompressSingleFileWithRetryAsync(mf4File, zipFile)
                    orphanedCount += 1
                End If
            Next
        Else
            HandleUserMessageLogging("GMRC", "CompressOrphanedFiles: MF4 compression disabled - skipping .mf4 files")
        End If

        ' ════════════════════════════════════════════════════════════
        ' ✅ FIX: Check CompressPCAP flag BEFORE processing .pcap files
        ' ════════════════════════════════════════════════════════════
        If CompressPCAP Then
            For Each pcapFile In Directory.GetFiles(dataRoot, "*.pcap", SearchOption.AllDirectories)
                Dim zipFile = Path.ChangeExtension(pcapFile, ".zip")
                If Not File.Exists(zipFile) Then
                    HandleUserMessageLogging("GMRC", $"Found orphaned PCAP: {Path.GetFileName(pcapFile)}")
                    CompressSingleFileWithRetryAsync(pcapFile, zipFile)
                    orphanedCount += 1
                End If
            Next
        Else
            HandleUserMessageLogging("GMRC", "CompressOrphanedFiles: PCAP compression disabled - skipping .pcap files")
        End If

        ' ════════════════════════════════════════════════════════════
        ' ✅ OPTIONAL: Add .asc and .vsb support if needed
        ' ════════════════════════════════════════════════════════════
        If CompressASC Then
            For Each ascFile In Directory.GetFiles(dataRoot, "*.asc", SearchOption.AllDirectories)
                Dim zipFile = Path.ChangeExtension(ascFile, ".zip")
                If Not File.Exists(zipFile) Then
                    HandleUserMessageLogging("GMRC", $"Found orphaned ASC: {Path.GetFileName(ascFile)}")
                    CompressSingleFileWithRetryAsync(ascFile, zipFile)
                    orphanedCount += 1
                End If
            Next
        End If

        If CompressVSB Then
            For Each vsbFile In Directory.GetFiles(dataRoot, "*.vsb", SearchOption.AllDirectories)
                Dim zipFile = Path.ChangeExtension(vsbFile, ".zip")
                If Not File.Exists(zipFile) Then
                    HandleUserMessageLogging("GMRC", $"Found orphaned VSB: {Path.GetFileName(vsbFile)}")
                    CompressSingleFileWithRetryAsync(vsbFile, zipFile)
                    orphanedCount += 1
                End If
            Next
        End If

        If orphanedCount > 0 Then
            HandleUserMessageLogging("GMRC", $"CompressOrphanedFiles: Queued {orphanedCount} file(s) for background compression")
        Else
            HandleUserMessageLogging("GMRC", "CompressOrphanedFiles: No orphaned files found (or all compression disabled)")
        End If
    End Sub

    Private Sub Handle7ZipProcess()
        ' ════════════════════════════════════════════════════════════
        ' CHECK 1: Legacy 7-Zip Process
        ' ════════════════════════════════════════════════════════════
        If Is7ZipRunning() Then
            If Not ShutdownWindows Then
                If MsgBox("File Zipping is not yet complete. Wait for file Zipping to complete before Exiting?", vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", "ExitApp: Waiting on 7zip to finish...")
                    Do While Is7ZipRunning()
                        Thread.Sleep(2000)
                    Loop
                Else
                    HandleUserMessageLogging("GMRC", "ExitApp: User decided not to wait for Zipping to complete before exiting...")
                End If
            Else
                HandleUserMessageLogging("GMRC", "ExitApp: Waiting on 7zip to finish...")
                Do While Is7ZipRunning()
                    Thread.Sleep(2000)
                Loop
            End If
        End If

        ' ════════════════════════════════════════════════════════════
        ' CHECK 2: Background Compression Tasks (New Async System)
        ' ════════════════════════════════════════════════════════════
        Dim activeTasks As List(Of Task)
        SyncLock CompressionTasksLock
            activeTasks = ActiveCompressionTasks _
            .Where(Function(t) Not t.IsCompleted) _
            .ToList()
        End SyncLock

        If activeTasks.Any() Then
            Dim taskCount As Integer = activeTasks.Count

            If Not ShutdownWindows Then
                Dim result = MsgBox(
                $"{taskCount} background compression task(s) still running. Wait for completion before exiting?",
                vbYesNo + vbQuestion,
                "CLEVIR Background Compression"
            )

                If result = vbYes Then
                    HandleUserMessageLogging("GMRC",
                    $"ExitApp: Waiting for {taskCount} compression tasks to finish...")

                    ' ✅ SHOW PROGRESS FORM (Optional enhancement)
                    Using progressForm As New Form()
                        progressForm.Text = "Waiting for Compression..."
                        progressForm.Size = New Size(400, 150)
                        progressForm.StartPosition = FormStartPosition.CenterScreen
                        progressForm.FormBorderStyle = FormBorderStyle.FixedDialog
                        progressForm.ControlBox = False

                        Dim label As New Label With {
                        .Text = $"Compressing {taskCount} file(s)... Please wait.",
                        .Dock = DockStyle.Fill,
                        .TextAlign = ContentAlignment.MiddleCenter,
                        .Font = New Font("Segoe UI", 12, FontStyle.Bold)
                    }
                        progressForm.Controls.Add(label)

                        progressForm.Show()
                        progressForm.Refresh()

                        ' ✅ WAIT FOR ALL TASKS
                        Try
                            Task.WaitAll(activeTasks.ToArray(), TimeSpan.FromMinutes(10))
                        Catch ex As Exception
                            HandleUserMessageLogging("GMRC",
                            $"Compression wait error: {ex.Message}")
                        End Try

                        progressForm.Close()
                    End Using

                    HandleUserMessageLogging("GMRC", "ExitApp: All compression tasks completed")
                Else
                    HandleUserMessageLogging("GMRC",
                    "ExitApp: User chose to exit without waiting for compression")
                End If
            Else
                ' ✅ SHUTDOWN MODE: Always wait
                HandleUserMessageLogging("GMRC",
                $"ExitApp: Waiting for {taskCount} compression tasks (shutdown mode)...")
                Try
                    Task.WaitAll(activeTasks.ToArray(), TimeSpan.FromMinutes(10))
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                    $"Compression wait error: {ex.Message}")
                End Try
            End If
        End If
    End Sub

    Private Sub Copy_GM_INCA_CommLog_File()
        Try
            Dim sourceLogFile As String = Path.Combine(My.Application.Info.DirectoryPath, "GM_INCA_Comm.log")

            If Not File.Exists(sourceLogFile) OrElse FileInUse(sourceLogFile) Then
                HandleUserMessageLogging("GMRC", "Log file not available for copying")
                Return
            End If

            ' Validate paths before using Path.Combine
            If String.IsNullOrEmpty(FinalPathToSaveData) OrElse String.IsNullOrEmpty(BaseDataCollectionPath) Then
                HandleUserMessageLogging("GMRC", "Required paths are null - cannot copy log file")
                Return
            End If

            If Len(FinalPathToSaveData) > 0 AndAlso Directory.Exists(FinalPathToSaveData) Then
                HandleUserMessageLogging("GMRC", "Copying GM_INCA_Comm.log...",,, FlashMsgOn)
                Dim destPath As String = Path.Combine(BaseDataCollectionPath, "Data", "gmcsv" & VehicleNumber, $"{Format(DateTime.Now, "yyyMMdd_hhmmss")}_GM_INCA_Comm.log")
                FileCopy(sourceLogFile, destPath)
            Else
                ' Fallback to application directory
                Dim info As New FileInfo(sourceLogFile)
                If info.Length > 10000 Then
                    HandleUserMessageLogging("GMRC", "Copying GM_INCA_Comm.log to fallback location...",,, FlashMsgOn)
                    Dim fallbackPath As String = Path.Combine(My.Application.Info.DirectoryPath, $"{Format(DateTime.Now, "MMddyyyy_hhmmss")}_GM_INCA_Comm.log")
                    FileCopy(sourceLogFile, fallbackPath)
                End If
            End If

            ' Clean up original log file
            HandleUserMessageLogging("GMRC", "Deleting GM_INCA_Comm.log file...",,, FlashMsgOn)
            File.Delete(sourceLogFile)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CopyLogFile error: {ex.Message}")
        End Try
    End Sub

    Private Sub SaveSignalListChanges()
        If ClevirAdministrator AndAlso GridCellPropConfig._changesMade Then
            If Not ConfigureForNewSoftwareVersion Then
                HandleUserMessageLogging("GMRC", "ExitApp: Save Signal List Changes?")
                If MsgBox("Save Signal List Changes?", vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", "ExitApp: Save Signal List Changes = Yes")
                    WriteSignalListFile(INCAVariableFile)
                Else
                    HandleUserMessageLogging("GMRC", "ExitApp: Save Signal List Changes = No")
                End If
            Else
                HandleUserMessageLogging("GMRC", "ExitApp: Saving Signal List Changes due to removal of invalid signals...", DisplayMsgBox)
                WriteSignalListFile(INCAVariableFile)
            End If
        End If
    End Sub

    ' -- REPLACE the entire routine with this version (no local helpers) --
    Private Sub HandleFileManipulationsForUpload()
        If Not HaveRecorded Then Return

        Dim sourceLogPath As String = Path.Combine(My.Application.Info.DirectoryPath, "GM_ResidentClient.log")
        Dim aggregateAnnoFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, $"{VehicleNumber}_AggregateAnnotations.csv")
        Dim driverAnnoFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, $"{VehicleNumber}_Driver_Anno_Cons.xlsx")
        Dim baseDataPath As String = Path.Combine(BaseDataCollectionPath, "Data")

        ' Log destination
        Dim destinationLogPath As String = Path.Combine(baseDataPath, "gmcsv" & VehicleNumber, "GM_ResidentClient.log")

        Try
            Directory.CreateDirectory(Path.GetDirectoryName(destinationLogPath))
            File.Copy(sourceLogPath, destinationLogPath, overwrite:=True)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Copy log failed: {ex.Message}")
        End Try

        ' Aggregate anno
        If File.Exists(aggregateAnnoFilePath) Then
            Dim aggregateDestinationPath As String = Path.Combine(baseDataPath, "gmcsv" & VehicleNumber, $"{VehicleNumber}_AggregateAnnotations.csv")
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(aggregateDestinationPath))
                File.Copy(aggregateAnnoFilePath, aggregateDestinationPath, overwrite:=True)
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"Copy aggregate anno failed: {ex.Message}")
            End Try
        End If

        ' Driver anno (single, consolidated copy with wait + retries)
        If File.Exists(driverAnnoFilePath) Then
            Dim driverAnnoDestinationPath As String = Path.Combine(baseDataPath, "gmcsv" & VehicleNumber, $"{VehicleNumber}_Driver_Anno_Cons.xlsx")

            ' Wait for exclusive access, then copy with retries
            If WaitUntilFileReady(driverAnnoFilePath, 10, 500) Then
                If Not CopyWithRetries(driverAnnoFilePath, driverAnnoDestinationPath, maxRetries:=5, delayMs:=1000) Then
                    HandleUserMessageLogging("GMRC", $"Failed to copy Driver_Anno_Cons.xlsx after retries")
                End If
            Else
                HandleUserMessageLogging("GMRC", "Driver_Anno_Cons.xlsx not available after waiting - skipping copy")
            End If
        End If
    End Sub

    ' -- Helpers at class scope (e.g., near other helper functions) --
    Private Function WaitUntilFileReady(path As String, tries As Integer, delayMs As Integer) As Boolean
        For i = 1 To Math.Max(0, tries)
            Try
                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)
                    Return True
                End Using
            Catch
                Thread.Sleep(Math.Max(0, delayMs))
            End Try
        Next
        Return False
    End Function

    Private Function CopyWithRetries(src As String, dest As String, maxRetries As Integer, delayMs As Integer) As Boolean
        For i = 1 To Math.Max(1, maxRetries)
            Try
                File.Copy(src, dest, overwrite:=True)
                HandleUserMessageLogging("GMRC", $"Copied Driver_Anno_Cons.xlsx to {dest}")
                Return True
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"Retry {i}/{maxRetries} - Copy failed: {ex.Message}")
                Thread.Sleep(Math.Max(0, delayMs))
            End Try
        Next
        Return False
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
            Catch
                ' If GetCurrentRecordingInfo isn't available or fails, we'll fallback to other APIs below.
            End Try

            ' Fallback: Ask INCA for the last completed recording file, then increment its sequence.
            Dim lastCompleted As String = ""
            Try
                lastCompleted = MyIncaInterface.GetLastRecordingFileName()
            Catch
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
    ' --- END ADDED HELPERS ---

    Private Sub CloseDisplayForms()

        ' ✅ FIX: Prevent multiple calls
        Static closingForms As Boolean = False
        If closingForms Then Return

        HandleUserMessageLogging("GMRC", "ExitApp: Closing Display Forms...")

        ' ✅ Suspend event processing during shutdown
        'Application.RemoveMessageFilter(Me)  ' Prevents stray events

        If MyDFs IsNot Nothing AndAlso MyDFs.Count > 0 Then
            For Each form As FormDataClass In MyDFs
                form?.Close()
            Next
            MyDFs.Clear()
        End If

        ' Close other forms
        ' Call the method with the name of the form you want to check and close
        CloseFormIfLoaded("DeviceStatus")
        CloseFormIfLoaded("TargetStatusDisplay")
        CloseFormIfLoaded("PedestrianStatusDisplay")
        CloseFormIfLoaded("FusionStatusDisplay")
        CloseFormIfLoaded("CopilotStatusDisplay")
        CloseFormIfLoaded("MyTdGraphicsContainer")
        CloseFormIfLoaded("LkaForm")
    End Sub

    Private Sub CloseFormIfLoaded(ByVal formName As String)
        ' Check if the form is loaded
        Dim formToClose As Form = Application.OpenForms.Cast(Of Form)().FirstOrDefault(Function(f) f.Name = formName)
        ' Form is loaded, close it
        formToClose?.Close()
    End Sub

    Private Sub CleanUpINCAResources()
        If myinca IsNot Nothing Then
            MyIncaInterface.RCI2_Cleanup()
            MyIncaInterface.UnlockExperiment()
            myinca.UnlockTool()
            myinca = Nothing
            MyHWC = Nothing
        End If
        InitForm.Close()
    End Sub

    Private Sub HandleShutdownOrRestart(ByRef uflags As Long)
        If ShutdownWindows OrElse RestartWindows Then
            Thread.Sleep(3000)
            AcquireShutdownPrivilege()
            Thread.Sleep(5000)
            HandleUserMessageLogging("GMRC", "ExitApp: Exiting Windows...",,)

            If Not Debugger.IsAttached Then
                uflags = If(ShutdownWindows, EwxPoweroff Or EwxShutdown Or EwxForce, EWX_REBOOT Or EwxForce)
                ExitWindows(uflags, 0)
                End
            End If
        End If
    End Sub

    Private Sub ShutdownAndRestartInca(ByVal sleeptime As Integer)

        'User selection from the Actions dropdown menu, allows the user to shutdown and restart INCA
        'Can also be called in response to a communication loss between the app and INCA.  Also called
        'after we perform a FULL signal registration during initialization.

        Dim forceInit As Boolean
        Dim errorMsg As String

        Dim e As EventArgs

        Try

            e = EventArgs.Empty

            Cursor = Cursors.WaitCursor

            'We cant shut down until we transition out of Measure Mode

            If MyIncaInterface.GetMeasurementStatus() = "True" Then
                MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                    .GetAwaiter().GetResult()
            End If

            HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: Shutting Down INCA...")

            CheckForExperiment = False
            MyIncaInterface.CloseINCA()

            RedisplayOnVehicleForm("GroupBox1")

            Thread.Sleep(sleeptime)

            errorMsg = ""

            'This flag is used to keep events from firing which are associated with
            'various controls having default text added during initialization
            _initializing = True
            HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: Initializing...")

            OnVehicleScreen.Refresh()

            'We always set the ForceInit flag to false in the initial call to InitINCA in the GmResidentClient
            'We set this to false here, we do not want to force signal registration if we are already initialized 
            'and have our signals registered, it is faster for development.

            forceInit = False

            'Here we initialize INCA based on information read in from the config.xml file
            If MyIncaInterface.InitINCA(INCADatabase, INCAWorkspace, INCAExperiment, forceInit, errorMsg, False) <> IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then

                RedisplayOnVehicleForm("GroupBox1")

                'Here is where we register the signals from our signal list with INCA using the RCI2 interface (see the GM_INCA_Comm project).

                MyIncaInterface.RegisterSignals()

                HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: Initialization Complete")

                OnVehicleScreen.Refresh()

            Else

                HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: INCA Initialization returned - " & errorMsg)

                If Not Debugger.IsAttached Then

                    OnVehicleScreen.TopMost = True

                    If MsgBox("INCA Initialization returned - " & errorMsg & " Do you wish to continue?", vbYesNo) = vbNo Then
                        HandleUserMessageLogging("GMRC", "INCA Initialization returned - " & errorMsg & " Do you wish to continue? No.",,)
                        ExitApp()
                    End If

                End If

                RedisplayOnVehicleForm("GroupBox1")

                'Here is where we register the signals from our signal list with INCA using the RCI2 interface (see the GM_INCA_Comm project).

                MyIncaInterface.RegisterSignals()

                HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: Initialization Complete")

                OnVehicleScreen.Refresh()

            End If

            Thread.Sleep(2000)

            GroupBox1.Visible = False

            FormDisplayed = True
            _initializing = False

            If PlaybackMode = False Then
                CheckForExperiment = True
            End If

            Cursor = Cursors.Arrow

            RedisplayOnVehicleForm("Main")

            OnVehicleScreen.TopMost = False

            HandleUserMessageLogging("GMRC", "INCA Restarted.",,)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: " & ex.Message, DisplayMsgBox,)
        End Try

    End Sub

    Private Async Function CheckForCameras(
                                           ByVal initialWaitTime As Integer,
                                           ByVal camerapingtime As Integer,
                                           ByVal showToasts As Boolean,
                                           Optional ct As CancellationToken = Nothing
                                           ) As Task

        If ct.IsCancellationRequested Then
            HandleUserMessageLogging("GMRC", "CheckForCameras: Cancellation requested before starting")
            Return
        End If

        ' ════════════════════════════════════════════════════════════════════════
        ' ✅ REFACTORED: Use ActiveCameras from Module1 (populated by ReadVehicleConfigsFile)
        ' ════════════════════════════════════════════════════════════════════════
        Dim count As Integer = ActiveCameras.Count

        If count <= 0 Then
            HandleUserMessageLogging("GMRC", "CheckForCameras: No cameras configured for this vehicle - skipping check")
            Return
        End If

        HandleUserMessageLogging("GMRC",
            $"CheckForCameras: Scanning {count} camera(s) from vehicle config: {String.Join(", ", ActiveCameras.Select(Function(c) c.Position))}")

        ' ════════════════════════════════════════════════════════════════════════
        ' 🔥 OPTIMIZATION: Quick pre-check - are cameras already responsive?
        ' ════════════════════════════════════════════════════════════════════════
        Dim needsBootDelay As Boolean = False
        Dim sw As Stopwatch = Stopwatch.StartNew()

        If initialWaitTime > 0 Then
            HandleUserMessageLogging("GMRC", "CheckForCameras: Quick pre-check for already-booted cameras (2s timeout)...")

            'If showToasts Then
            '    StatusNotifier.Toast($"Checking {count} camera(s)...", "CAMERA", durationMs:=1500, ensureMainOnTop:=False)
            'End If

            Try
                Using quickCts As New CancellationTokenSource(TimeSpan.FromSeconds(2))
                    Dim quickPingTasks = ActiveCameras _
                        .Select(Function(camera, index) PingCameraWithRetryAsync(index, camera.IpAddress, quickCts.Token)) _
                        .ToArray()

                    Dim quickResults = Await Task.WhenAll(quickPingTasks)
                    Dim foundCount As Integer = quickResults.Count(Function(r) r)

                    If foundCount = count Then
                        ' ✅ All cameras already responsive - skip boot delay!
                        HandleUserMessageLogging("GMRC", $"✅ All {count} camera(s) already responsive (checked in {sw.Elapsed.TotalSeconds:F1}s) - skipping {initialWaitTime}s boot delay")
                        needsBootDelay = False
                    Else
                        ' ❌ Some cameras not responsive - need boot delay
                        HandleUserMessageLogging("GMRC", $"⏳ {count - foundCount}/{count} camera(s) not responsive - will wait {initialWaitTime}s for boot")
                        needsBootDelay = True
                    End If
                End Using
            Catch ex As Exception
                ' Pre-check failed - assume boot delay needed
                HandleUserMessageLogging("GMRC", $"Pre-check error: {ex.Message} - assuming boot delay needed")
                needsBootDelay = True
            End Try
        Else
            ' initialWaitTime <= 0 means no boot delay configured
            HandleUserMessageLogging("GMRC", "CheckForCameras: No boot delay configured (initialWaitTime <= 0)")
            needsBootDelay = False
        End If

        ' ════════════════════════════════════════════════════════════════════════
        ' BOOT DELAY (only if cameras weren't already found)
        ' ════════════════════════════════════════════════════════════════════════
        If needsBootDelay Then
            Try
                'If showToasts Then
                'StatusNotifier.Toast($"Waiting {initialWaitTime}s for camera boot...", "CAMERA", durationMs:=2000, ensureMainOnTop:=False)
                'End If

                HandleUserMessageLogging("GMRC", $"CheckForCameras: Waiting {initialWaitTime}s for camera boot...")

                Await Task.Delay(initialWaitTime * 1000, ct)

            Catch ex As OperationCanceledException
                HandleUserMessageLogging("GMRC", "CheckForCameras: Boot delay cancelled (app exiting)")
                Return
            End Try
        End If

        ' ════════════════════════════════════════════════════════════════════════
        ' ✅ FULL CAMERA SCAN (with retries)
        ' ════════════════════════════════════════════════════════════════════════
        ' Reset stopwatch for full scan timing
        sw.Restart()
        Dim timeout As TimeSpan = TimeSpan.FromSeconds(camerapingtime)

        HandleUserMessageLogging("GMRC", $"CheckForCameras: Starting full camera scan (timeout: {camerapingtime}s)...")

        If showToasts Then
            StatusNotifier.Toast($"Scanning {count} camera(s)...", "CAMERA", durationMs:=1000, ensureMainOnTop:=False)
        End If

        ' Create cancellation token to enforce global timeout
        Using cts As New CancellationTokenSource(timeout)
            ' ✅ REFACTORED: Ping cameras from ActiveCameras list
            Dim pingTasks = ActiveCameras _
                .Select(Function(camera, index) PingCameraWithRetryAsync(index, camera.IpAddress, cts.Token)) _
                .ToArray()

            ' Wait for all pings to complete or timeout
            Dim results = Await Task.WhenAll(pingTasks)

            ' ════════════════════════════════════════════════════════════════════════
            ' PROCESS RESULTS
            ' ════════════════════════════════════════════════════════════════════════
            Dim foundCount As Integer = results.Count(Function(r) r)
            Dim allFound As Boolean = foundCount = count

            ' Log individual camera status with position names
            For i As Integer = 0 To count - 1
                Dim camera = ActiveCameras(i)
                If results(i) Then
                    HandleUserMessageLogging("GMRC", $"✅ Camera {camera.Position} found at {camera.IpAddress}")
                    If showToasts Then
                        StatusNotifier.Toast($"Camera {camera.Position} found", "CAMERA", durationMs:=2000, ensureMainOnTop:=False)
                    End If
                Else
                    HandleUserMessageLogging("GMRC", $"❌ Camera {camera.Position} UNREACHABLE at {camera.IpAddress}")
                    If showToasts Then
                        StatusNotifier.ToastError($"❌ Camera {camera.Position} UNREACHABLE", "CAMERA", ToastKind.Error)
                    End If
                End If
            Next

            ' Update _effectiveCameraCount for this session
            _effectiveCameraCount = foundCount

            ' Final summary
            HandleUserMessageLogging("GMRC", $"CheckForCameras: {foundCount}/{count} cameras found in {sw.Elapsed.TotalSeconds:F1}s")
            'If showToasts Then
            '    StatusNotifier.Toast($"CheckForCameras: {foundCount}/{count} cameras found in {sw.Elapsed.TotalSeconds:F1}s", durationMs:=2000, ensureMainOnTop:=False)
            'End If

            ' No longer writing to CameraIPAddresses.txt - that file is deprecated
        End Using
    End Function

    ' ════════════════════════════════════════════════════════════════════════════
    ' ✅ HELPER: Ping with automatic retry + TCP fallback
    ' ════════════════════════════════════════════════════════════════════════════
    Private Async Function PingCameraWithRetryAsync(index As Integer, ip As String, ct As CancellationToken) As Task(Of Boolean)
        Const maxRetries As Integer = 3
        Const pingTimeout As Integer = 1000  ' 1 second per ping

        If String.IsNullOrWhiteSpace(ip) Then
            HandleUserMessageLogging("GMRC", $"Camera {index + 1}: Invalid IP address")
            Return False
        End If

        ' ════════════════════════════════════════════════════════════════════════
        ' RETRY LOOP: 3 attempts with exponential backoff
        ' ════════════════════════════════════════════════════════════════════════
        For attempt As Integer = 1 To maxRetries

            If attempt > 1 AndAlso ct.IsCancellationRequested Then
                Return False
            End If

            Try
                ' ✅ STEP 1: Try ICMP Ping
                Using pinger As New Ping()
                    Dim reply = Await pinger.SendPingAsync(ip, pingTimeout)
                    If reply.Status = IPStatus.Success Then
                        Return True  ' ✅ Success!
                    End If
                End Using

                ' ✅ STEP 2: Fallback to TCP probe (HTTP port 80 / RTSP port 554)
                If Await TcpProbeAsync(ip, 80, 500, ct) OrElse
               Await TcpProbeAsync(ip, 554, 500, ct) Then
                    HandleUserMessageLogging("GMRC", $"Camera {index + 1}: TCP reachable (ping failed)")
                    Return True
                End If

                ' ✅ STEP 3: Exponential backoff before retry
                If attempt < maxRetries Then
                    Try
                        Await Task.Delay(500 * attempt, ct)
                    Catch ex As OperationCanceledException
                        Return False  ' Exit cleanly during backoff
                    End Try
                End If

            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"Camera {index + 1} error (attempt {attempt}): {ex.Message}")
            End Try
        Next

        Return False  ' ❌ Failed after all retries
    End Function

    ' ════════════════════════════════════════════════════════════════════════════
    ' ✅ HELPER: TCP Port Probe (async with cancellation)
    ' ════════════════════════════════════════════════════════════════════════════
    Private Async Function TcpProbeAsync(ip As String, port As Integer, timeoutMs As Integer, ct As CancellationToken) As Task(Of Boolean)
        Try
            Using client As New Net.Sockets.TcpClient()
                Dim connectTask = client.ConnectAsync(ip, port)
                Dim timeoutTask = Task.Delay(timeoutMs, ct)

                Dim completedTask = Await Task.WhenAny(connectTask, timeoutTask)

                If completedTask Is connectTask Then
                    Await connectTask  ' Check for exceptions
                    Return client.Connected
                End If

                Return False  ' Timeout
            End Using
        Catch ex As Net.Sockets.SocketException
            ' Connection refused = host reachable but port closed (still valid!)
            Return ex.SocketErrorCode = Net.Sockets.SocketError.ConnectionRefused
        Catch
            Return False
        End Try
    End Function

    Private Sub HandleButtonVisibility(ByVal areVisible As Boolean)

        'Called from CheckForLostDevice in MyBackgroundTasks.  Makes various user buttons visible or invisible
        'based on value passed in...

        OnVehicleScreen.Button2.Visible = areVisible
        OnVehicleScreen.Button3.Visible = areVisible
        OnVehicleScreen.Button4.Visible = areVisible
        OnVehicleScreen.Button5.Visible = areVisible
        OnVehicleScreen.Button6.Visible = areVisible
        OnVehicleScreen.Button7.Visible = areVisible
        OnVehicleScreen.Button10.Visible = areVisible
        OnVehicleScreen.Button14.Visible = areVisible
        OnVehicleScreen.Button23.Visible = areVisible
    End Sub

    ' Main initialization routine called from Form_Load.
    Private Async Function Initialize() As Task

        ' ══════════════════════════════════════════════════════════════
        ' ✅ FIX 1: Reset stale initialization flag
        ' ══════════════════════════════════════════════════════════════
        If _initializing Then
            HandleUserMessageLogging("GMRC", "Initialize: WARNING - _initializing flag was already true, resetting...")
            _initializing = False
        End If

        If _fullyInitialized Then
            HandleUserMessageLogging("GMRC", "Initialize: Already fully initialized, ignoring call")
            Return
        End If

        If exitInProgress Then
            HandleUserMessageLogging("GMRC", "Initialize: Exit in progress, aborting")
            Return
        End If

        ' Create cancellation token
        If _initCts Is Nothing OrElse _initCts.IsCancellationRequested Then
            _initCts?.Dispose()
            _initCts = New CancellationTokenSource()
        End If

        ' ══════════════════════════════════════════════════════════════
        ' ✅ FIX 2: Prevent concurrent initialization
        ' ══════════════════════════════════════════════════════════════
        If initializationInProgress Then
            HandleUserMessageLogging("GMRC", "Initialize: Already running, ignoring call")
            Return
        End If

        Try
            initializationInProgress = True
            HandleUserMessageLogging("GMRC", "Starting initialization...")
            _initializing = True

            ' ✅ FIX: Initialize stopwatch FIRST (before any subsystem that might log)
            If RecorderStopWatch Is Nothing Then
                RecorderStopWatch = New Stopwatch()
                HandleUserMessageLogging("GMRC", "Initialize: RecorderStopWatch initialized early")
            End If

            ' 1) Prepare environment: Read debug file, set flags, set cursors, log start
            PrepareEnvironment()

            ' Store the user's configuration preference (already loaded in PrepareEnvironment)
            Dim userWantsLidar As Boolean = LidarCaptureEnabled

            ' 2) Set up displays and check if we're in playback mode
            SetInitialDisplayProperties()

            If Not PlaybackMode Then
                DetermineAlternateRecordMode()
            End If

            ' ════════════════════════════════════════════════════════════
            ' ✅ FIX: LIDAR INITIALIZATION - Respect configuration setting
            ' ════════════════════════════════════════════════════════════
            Try
                Dim testDevices = SharpPcap.CaptureDeviceList.Instance

                If testDevices IsNot Nothing AndAlso testDevices.Count > 0 Then
                    ' LiDAR hardware is available
                    LoginForm.CheckBox_LidarCapture.Visible = True

                    ' Only initialize if user enabled LIDAR in config
                    If userWantsLidar Then
                        ' Initialize LiDAR devices from config or create default
                        If LidarDevices.Count = 0 Then
                            ' No devices configured - create default single device
                            Dim defaultLidar As New LidarDevice()
                            LidarDevices.Add(defaultLidar)
                            HandleUserMessageLogging("GMRC",
                "Initialize: Created default LiDAR device (no config found)")
                        End If

                        ' Validate each configured device has a valid adapter
                        For i As Integer = LidarDevices.Count - 1 To 0 Step -1
                            Dim lidar = LidarDevices(i)
                            Dim adapterFound As Boolean = False

                            For Each device In testDevices
                                If device.Name.Contains(lidar.LidarAdapterGuid) Then
                                    adapterFound = True
                                    Exit For
                                End If
                            Next

                            If Not adapterFound AndAlso Not String.IsNullOrEmpty(lidar.LidarAdapterGuid) Then
                                HandleUserMessageLogging("GMRC",
                    $"Initialize: LiDAR adapter {lidar.LidarAdapterGuid} not found - removing from list")
                                LidarDevices.RemoveAt(i)
                            End If
                        Next

                        ' ✅ FIX: Only enable if user wants it AND devices are available
                        If LidarDevices.Count > 0 Then
                            LidarCaptureEnabled = True
                            HandleUserMessageLogging("GMRC",
                $"Initialize: LiDAR capture ENABLED ({LidarDevices.Count} device(s) configured, {testDevices.Count} adapter(s) detected)")
                            StatusNotifier.Toast($"Initialize: LiDAR capture ENABLED ({LidarDevices.Count} device(s) configured, {testDevices.Count} adapter(s) detected)", durationMs:=2000, ensureMainOnTop:=False)
                        Else
                            LidarCaptureEnabled = False
                            HandleUserMessageLogging("GMRC",
                "Initialize: LiDAR capture requested but no valid devices found - DISABLED")
                            StatusNotifier.Toast($"Initialize: LiDAR capture requested but no valid devices found - DISABLED", durationMs:=2000, ensureMainOnTop:=False)
                        End If
                    Else
                        ' User disabled LIDAR in config - respect that choice
                        LidarCaptureEnabled = False
                        ' ✅ REMOVED: LidarDevices.Clear()  
                        ' Keep devices in config even when capture is disabled
                        HandleUserMessageLogging("GMRC", "Initialize: LiDAR capture DISABLED by configuration")
                        StatusNotifier.Toast($"Initialize: LiDAR capture DISABLED by configuration", durationMs:=2000, ensureMainOnTop:=False)

                    End If
                Else
                    ' No network adapters available
                    LidarCaptureEnabled = False
                    LoginForm.CheckBox_LidarCapture.Visible = False

                    If userWantsLidar Then
                        HandleUserMessageLogging("GMRC", "Initialize: LiDAR requested in config but no network adapters found - DISABLED")
                        StatusNotifier.Toast($"Initialize: LiDAR requested in config but no network adapters found - DISABLED", durationMs:=2000, ensureMainOnTop:=False)
                    Else
                        HandleUserMessageLogging("GMRC", "Initialize: No network adapters found for LiDAR capture")
                        StatusNotifier.Toast($"Initialize: No network adapters found for LiDAR capture", durationMs:=2000, ensureMainOnTop:=False)

                    End If
                End If

            Catch ex As Exception
                LidarCaptureEnabled = False
                LidarDevices.Clear()
                HandleUserMessageLogging("GMRC", $"Initialize: LiDAR initialization failed - {ex.Message}")
                StatusNotifier.Toast($"Initialize: LiDAR initialization failed - {ex.Message}", durationMs:=2000, ensureMainOnTop:=False)
            End Try

            ' ════════════════════════════════════════════════════════════
            ' Time Sync Provider Initialization (OXTS / TimeMachine)
            ' ════════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", $"Initialize: Time Sync check - TimeSyncEnabled={TimeSyncEnabled}, TimeSyncProviderType={TimeSyncProviderType}, OxtsEnabled={OxtsEnabled}, TimeMachineEnabled={TimeMachineEnabled}")
            If TimeSyncEnabled Then
                Try
                    MyTimeSyncProvider = Nothing
                    MyOxtsInterface = Nothing

                    Select Case TimeSyncProviderType.Trim().ToUpperInvariant()
                        Case "TIMEMACHINE"
                            Dim tmProvider As New TimeMachineTimeSyncProvider With {
                                .DeviceIpAddress = TimeMachineIpAddress,
                                .Port = TimeMachinePort,
                                .PollIntervalMs = TimeMachinePollMs,
                                .PtpAssumeLocked = TimeMachinePtpAssumeLocked
                            }
                            tmProvider.Start()
                            MyTimeSyncProvider = tmProvider
                            OxtsEnabled = False
                            HandleUserMessageLogging("GMRC", "Time sync provider started: TimeMachine")

                        Case Else
                            MyOxtsInterface = New OxtsNcomInterface With {
                                .NcomIpAddress = OxtsNcomIpAddress,
                                .NcomPort = OxtsNcomPort,
                                .AllowNoGpsLock = Not OxtsWaitForLockOnStart
                            }

                            MyOxtsInterface.OxtsStartListening()
                            MyTimeSyncProvider = MyOxtsInterface
                            OxtsEnabled = True
                            HandleUserMessageLogging("GMRC", "Time sync provider started: OXTS")

                            ' ✅ OPTIONAL: Wait for GPS lock before proceeding
                            If OxtsWaitForLockOnStart Then
                                HandleUserMessageLogging("GMRC", "Waiting for GPS lock (max 30 sec)...")
                                If Not MyOxtsInterface.WaitForGpsLock(OxtsGpsLockTimeout) Then
                                    If MsgBox("GPS lock failed. Continue without OXTS?", vbYesNo) = vbNo Then
                                        ExitApp("Complete")
                                        Return
                                    End If
                                    OxtsEnabled = False
                                End If
                            End If
                    End Select
                    ' ✅ Connect OXTS to all LiDAR devices
                    If LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 AndAlso MyTimeSyncProvider IsNot Nothing Then
                        For Each lidar In LidarDevices
                            lidar.SetTimeSyncProvider(MyTimeSyncProvider)
                            HandleUserMessageLogging("GMRC", $"{MyTimeSyncProvider.ProviderName} linked to {lidar.DeviceId}")
                        Next
                    End If

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Failed to initialize time sync provider: {ex.Message}")
                    StatusNotifier.Toast($"Failed to initialize time sync provider: {ex.Message}", durationMs:=2000, ensureMainOnTop:=False)
                    MyTimeSyncProvider = Nothing
                    MyOxtsInterface = Nothing
                    OxtsEnabled = False
                End Try
            End If

            ' 3) If Development mode, connect to INCA now before login
            If Not PlaybackMode Then
                HandleDevelopmentMode()
            End If

            ' ✅ Check cameras AFTER login (GOOD - no UI conflicts)
            If Not PlaybackMode Then
                Await CheckForCamerasIfNeeded(_initCts.Token)

                DetermineAlternateRecordMode()
            End If

            ' 5) Terminate any leftover initialization thread from "InitForm"
            KillInitThreadIfNeeded()

            ' 6) Launch alternate record software (CANalyzer or Vehicle Spy) if needed
            InitializeAlternateRecorderIfNeeded()

            ' 7) Handle creation of new experiment if SignalRegistrationMode = "NEW FULL"
            HandleNewExperimentIfNeeded()


            ' ✅ FIX 3: Check again before ReadInSignalList
            If exitInProgress OrElse (_initCts?.IsCancellationRequested) Then
                HandleUserMessageLogging("GMRC", "Initialize: Cancelled before ReadInSignalList")
                Return
            End If

            ' 8) Read the main signal list (Excel/csv). Cannot proceed if false.
            If Not ReadInSignalList(INCAVariableFile) Then
                ' Only log/exit if NOT exiting already
                If Not exitInProgress Then
                    HandleUserMessageLogging("GMRC", "ReadInSignalList returned False. Terminating...", DisplayMsgBox,)
                    StatusNotifier.Toast("Initialize: Failed to read signal list - terminating", durationMs:=2000, ensureMainOnTop:=False)
                    InitForm.Close()
                    Close()
                    End
                End If
            End If

            _incaInitStarted = True

            ' 9) (Optional) Start “ProcessKiller” thread if not in playback mode
            If Not PlaybackMode Then StartProcessKillerThread()

            ' 10) If not in debug mode, optionally launch ATT_TCP or any required external tools
            'If Not PlaybackMode Then LaunchAttTcpIfNeeded()

            ' 11) Initialize INCA
            If Not InitializeINCA() Then Return   ' If initialization failed, a message/log has already triggered an exit.

            ' 12) If using Vehicle Spy, load the correct config
            If Not PlaybackMode Then
                ConfigureVehicleSpyIfNeeded()
            End If

            ' 13) Decide which form to show
            ShowAppropriateForm()

            _incaInitStarted = False

            ' 14) Create top-down container and set up menus
            InitializeUIAndMenus()

            ' 15) Verify hardware/device status
            If Not Await GetDeviceStatus() Then
                ExitApp("Complete")
                Return
            End If

            ' 16) Initialize application: read data dictionary, set up annotation tabs, etc.
            InitializeApplication()

            TopMost = False

            ' 16b) Start ProcessKiller NOW - after UI is initialized
            'If Not PlaybackMode Then StartProcessKillerThread()

            ' 17) Register signals
            If Not RegisterAllSignals() Then Return

            ' 18) Handle “FULL” registration scenario (save experiment & possibly restart INCA)
            HandleFullRegistrationScenario()

            ' 19) Finalize UI states
            FinalizeUI()

            ' 20) Start background worker for main loop
            _backgroundTasksCts = New CancellationTokenSource()  ' Create CTS for background task cancellation
            _enableMyBackgroundTasks = True
            BackgroundWorker1.RunWorkerAsync(2000)

            ' 21) Additional UI configuration: calibrate button, escalation processing, etc.
            ConfigureAdvancedUI()
            FormDisplayed = True

            ' ══════════════════════════════════════════════════════════════
            ' ✅ FIX 3: Set flag AFTER all initialization steps complete
            ' ══════════════════════════════════════════════════════════════
            _fullyInitialized = True
            'HandleUserMessageLogging("GMRC", "Initialize: Initialization Complete")

        Catch ex As OperationCanceledException
            HandleUserMessageLogging("GMRC", "Initialize: Cancelled by user")
            Return
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "Initialize: " & ex.Message, DisplayMsgBox,)
        Finally
            initializationInProgress = False
            _initCts?.Dispose()
            _initCts = Nothing
        End Try

    End Function

    '------------------------------------------------------------------
    ' Below are the private helper methods called by the main routine.
    ' Each method focuses on a single “theme” or task.
    '------------------------------------------------------------------

    ''' <summary>
    ''' Reads debug file, resets reference variables, sets cursors, etc.
    ''' </summary>
    Private Sub PrepareEnvironment()
        ReadDebugFile()
        ReferenceDataSetDataBasePaths = Nothing
        WorkingDataSetDataBasePaths = Nothing
        Cursor = Cursors.WaitCursor

        _initializing = True
        HandleUserMessageLogging("GMRC", "Initialize: Initializing...")

        ' Ensure no earlier stickies linger before we show a new one
        DismissInitToast(False)
        _initToastIds.Add(StatusNotifier.ToastSticky("Initialize: Initializing...", "CONFIG", StatusNotifier.ToastKind.Info, ensureMainOnTop:=False))

        Refresh()
        Cursor = Cursors.Arrow
    End Sub

    ''' <summary>
    ''' If operating in VPC modes, check for cameras.
    ''' </summary>
    Private Async Function CheckForCamerasIfNeeded(Optional ct As CancellationToken = Nothing) As Task
        If NumberOfCamerasInVehicle > 0 Then
            ' ✅ Pass token to CheckForCameras
            Await CheckForCameras(
                initialWaitTime:=CameraWaitTime,
                camerapingtime:=CameraPingTime,
                showToasts:=True,
                ct:=ct  ' ← Pass cancellation token
                )
        End If
    End Function

    ''' <summary>
    ''' Handles any logic necessary for development flavor, connecting to INCA, enumerating experiments, etc.
    ''' </summary>
    Private Sub HandleDevelopmentMode()
        If UCase(CLEVIRFlavor) = "DEVELOPMENT" AndAlso Not FlashingStatus.Visible Then
            ' In Dev Mode, parse enumerations
            ParseA2lFile(Path.Combine(My.Application.Info.DirectoryPath, "Enumerations.txt"))

            If Not _incaLaunched Then
                HandleUserMessageLogging("GMRC", "Initialize: Waiting for user input...")
                ' Replace any existing init stickies instantly before showing a new one
                DismissInitToast(False)
                '_initToastIds.Add(StatusNotifier.ToastSticky("Initialize: Waiting for user input...", "CONFIG", StatusNotifier.ToastKind.Info, ensureMainOnTop:=False))
                'Thread.Sleep(3000)

                ' Only connect to INCA if not already connected from InitForm
                ' InitForm.HandleFormDisplayBasedOnFlavor already calls ConnectToInca for DEVELOPMENT mode
                ' Check if we already have a valid connection before attempting to reconnect
                Dim needsConnection As Boolean = True
                Try
                    ' Test if we already have a valid INCA connection by checking interface state
                    If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                        ' Try to call a lightweight INCA method to verify connection is alive
                        Dim testVersion As String = MyIncaInterface.GetCurrentVersion()
                        If Not String.IsNullOrEmpty(testVersion) Then
                            HandleUserMessageLogging("GMRC", "Initialize: Reusing existing INCA connection (version: " & testVersion & ")")
                            needsConnection = False
                        End If
                    End If
                Catch ex As Exception
                    ' If test call fails, we need to reconnect
                    HandleUserMessageLogging("GMRC", "Initialize: Existing connection test failed, will reconnect: " & ex.Message)
                    needsConnection = True
                End Try

                If needsConnection Then
                    HandleUserMessageLogging("GMRC", "Initialize: Establishing INCA connection...")
                    'StatusNotifier.ToastSticky("Initialize: Establishing INCA connection...", "INCA", StatusNotifier.ToastKind.Info, ensureMainOnTop:=False)
                    Dim returnstr As String = MyIncaInterface.ConnectToInca()
                    If returnstr <> "True" Then
                        HandleUserMessageLogging("GMRC", "Initialize: ConnectToInca returned - " & returnstr, DisplayMsgBox,)
                        ExitApp()
                        Return
                    End If
                End If

                _incaLaunched = True
                HandleUserMessageLogging("GMRC", "Initialize: Getting Available Experiment Names...")
                AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames()
                HandleUserMessageLogging("GMRC", "Initialize: Available Experiment Names Retrieved")
            End If

            ' Make sure login screen's CheckBox1 matches SaveCalSnapshotEnabled
            LoginForm.CheckBox1.Checked = SaveCalSnapshotEnabled
            If Not LoginIfRequired() Then
                HandleUserMessageLogging("GMRC", "Initialize: LoginIfRequired returned False. Exiting...", DisplayMsgBox)
                ExitApp("Complete")
                Return ' ← Exit the HandleDevelopmentMode sub
            End If
        Else
            SaveLoginID = "Demo"
        End If
    End Sub

    ''' <summary>
    ''' Shows LoginForm and handles user authentication.
    ''' Returns True if user logged in successfully, False if cancelled/error.
    ''' Automatically shows SoftwareVersionSelect if user clicks Button44.
    ''' </summary>
    Private Function LoginIfRequired() As Boolean
        If exitInProgress Then
            HandleUserMessageLogging("GMRC", "LoginIfRequired: Exit in progress, skipping login")
            Return False
        End If

        Try
            HandleUserMessageLogging("GMRC", "LoginIfRequired: Preparing LoginForm...")

            Dim wasAlreadyLoggedIn As Boolean = Not String.IsNullOrEmpty(SaveLoginID)
            Dim previousLoginID As String = SaveLoginID

            If Not wasAlreadyLoggedIn Then
                SaveLoginID = String.Empty
            End If

            LoginForm.DialogResult = DialogResult.None
            LoginForm.TopMost = True

            HandleUserMessageLogging("GMRC", $"LoginIfRequired: Showing LoginForm (wasAlreadyLoggedIn={wasAlreadyLoggedIn})")

            Dim loginResult As DialogResult = LoginForm.ShowDialog(Me)

            ' ════════════════════════════════════════════════════════════
            ' ✅ HANDLE WORKSPACE SELECTION (Button44 clicked)
            ' ════════════════════════════════════════════════════════════
            If loginResult = DialogResult.Retry Then
                HandleUserMessageLogging("GMRC", "LoginIfRequired: Showing workspace selection...")

                Dim result = SoftwareVersionSelect.ShowDialog(Me)

                If result = DialogResult.Cancel Then
                    HandleUserMessageLogging("GMRC", "LoginIfRequired: User cancelled workspace selection")

                    If wasAlreadyLoggedIn Then
                        SaveLoginID = previousLoginID
                        Return True
                    Else
                        Return LoginIfRequired()
                    End If

                ElseIf result = DialogResult.OK Then
                    ' ✅ CRITICAL FIX: Reset initialization flag BEFORE proceeding
                    _initializing = False
                    HandleUserMessageLogging("GMRC", "LoginIfRequired: Workspace selection completed - reset _initializing flag")

                    If wasAlreadyLoggedIn Then
                        SaveLoginID = previousLoginID
                        HandleUserMessageLogging("GMRC", $"LoginIfRequired: Keeping existing login '{SaveLoginID}' with new workspace")
                        Return True
                    Else
                        SaveLoginID = String.Empty
                        LoginForm.DialogResult = DialogResult.None
                        LoginForm.TopMost = True

                        loginResult = LoginForm.ShowDialog(Me)

                        If loginResult = DialogResult.Retry Then
                            HandleUserMessageLogging("GMRC", "LoginIfRequired: User clicked Button44 again - recursive call")
                            Return LoginIfRequired()
                        End If
                    End If

                Else
                    If wasAlreadyLoggedIn Then
                        SaveLoginID = previousLoginID
                        Return True
                    Else
                        Return False
                    End If
                End If
            End If

            If String.IsNullOrEmpty(SaveLoginID) Then
                HandleUserMessageLogging("GMRC", "LoginIfRequired: No login ID provided - exiting")
                Return False
            End If

            HandleUserMessageLogging("GMRC", $"LoginIfRequired: Login successful for '{SaveLoginID}'")
            Return True

        Catch ex As Exception
            If Not exitInProgress Then
                HandleUserMessageLogging("GMRC", $"LoginIfRequired error: {ex.Message}")
            End If
            Return False
        End Try
    End Function

    ' ✅ SIMPLIFIED: KillInitThreadIfNeeded - myThread cleanup removed (obsolete)
    ''' <summary>
    ''' Kills the leftover init thread if it exists
    ''' </summary>
    Private Sub KillInitThreadIfNeeded()
        Try
            TerminateInitThread = True
            HandleUserMessageLogging("GMRC", "Initialize: Terminating Init thread...")
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"KillInitThreadIfNeeded: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Launches (or quits) alternate record software if selected by user (CANalyzer, Vehicle Spy).
    ''' </summary>
    Private Sub InitializeAlternateRecorderIfNeeded()

        If PlaybackMode Then Return

        If AlternateRecordEnabled Then
            If AlternateRecordingMode <> "None" Then
                ' Avoid bringing the main window forward while a modal dialog may appear
                StatusNotifier.Toast("Initialize: Checking Alternate Record Method...", "CLEVIR", durationMs:=1000, ensureMainOnTop:=False)

                If AlternateRecordingMode <> "VehicleSpy" Then
                    If Not CanalyzerCaptureStarted Then
                        Dim altRecordReturnStr = LaunchCanalyzer()
                        If altRecordReturnStr <> "Success" Then
                            Dim owner As Form = GetBestModalOwner()
                            Using dialog As New CustomDialogForm(altRecordReturnStr, "CANalyzer Initialization", "CANalyzer")
                                dialog.StartPosition = FormStartPosition.CenterParent
                                dialog.TopMost = True
                                dialog.ShowInTaskbar = True
                                Dim result = If(owner IsNot Nothing, dialog.ShowDialog(owner), dialog.ShowDialog())
                                If dialog.Result = DialogResult.Cancel OrElse result = DialogResult.Cancel Then
                                    ExitApp("Complete")
                                End If
                            End Using
                        End If
                    End If
                Else
                    If Not VehicleSpyCaptureStarted Then
                        Dim altRecordReturnStr = LaunchVSpy()
                        If altRecordReturnStr <> "Success" Then
                            Dim owner As Form = GetBestModalOwner()
                            Using dialog As New CustomDialogForm(altRecordReturnStr, "VehicleSpy Initialization", "VehicleSpy")
                                dialog.StartPosition = FormStartPosition.CenterParent
                                dialog.TopMost = True
                                dialog.ShowInTaskbar = True
                                Dim result = If(owner IsNot Nothing, dialog.ShowDialog(owner), dialog.ShowDialog())
                                If dialog.Result = DialogResult.Cancel OrElse result = DialogResult.Cancel Then
                                    ExitApp("Complete")
                                End If
                            End Using
                        End If
                    End If
                End If
            End If
        Else
            ' If user chose no alternate record, but CANalyzer or VSpy is running, quit them
            If CanalyzerCaptureStarted Then QuitCanalyzer()
            If VehicleSpyCaptureStarted Then QuitVehicleSpy()
        End If
    End Sub


    Private Async Sub MonitorOxtsRtkStatus()
        ' ════════════════════════════════════════════════════════════
        ' Background task to monitor OXTS RTK status WITHOUT blocking recording
        ' ════════════════════════════════════════════════════════════

        If MyOxtsInterface Is Nothing Then Return

        Dim lastStatus As String = "Unknown"
        Dim warnedAboutLoss As Boolean = False

        Try
            While MyIncaInterface.Recording
                Await Task.Delay(2000) ' Check every 2 seconds

                If MyOxtsInterface IsNot Nothing Then
                    Dim currentStatus As String = MyOxtsInterface.GetRtkStatus() ' Returns "None", "Float", "Integer"
                    Dim isRealtime As Boolean = MyOxtsInterface.IsRealtime()

                    ' ✅ Warn if we LOSE RTK after having it
                    If lastStatus = "Integer" AndAlso currentStatus <> "Integer" AndAlso Not warnedAboutLoss Then
                        StatusNotifier.Toast($"RTK degraded: {lastStatus} → {currentStatus}", "OXTS Status")
                        HandleUserMessageLogging("GMRC", $"MonitorOxtsRtkStatus: RTK degraded from {lastStatus} to {currentStatus}")
                        OnVehicleScreen.Label3.BackColor = Color.Yellow ' Similar to LiDAR warning
                        warnedAboutLoss = True
                    End If

                    ' ✅ Celebrate when we GET RTK lock
                    If currentStatus = "Integer" AndAlso lastStatus <> "Integer" Then
                        StatusNotifier.Toast($"RTK Integer acquired! Offset: {MyOxtsInterface.TimeOffset.TotalMilliseconds:F1}ms", StatusNotifier.ToastKind.Success)
                        HandleUserMessageLogging("GMRC", $"MonitorOxtsRtkStatus: RTK Integer lock acquired (was {lastStatus})")
                        OnVehicleScreen.Label3.BackColor = Color.Green
                        warnedAboutLoss = False
                    End If

                    ' ✅ Check for non-realtime data
                    If Not isRealtime AndAlso lastStatus <> "NotRealtime" Then
                        StatusNotifier.Toast("OXTS data is NOT realtime - check network connection", "OXTS Warning")
                        HandleUserMessageLogging("GMRC", "MonitorOxtsRtkStatus: OXTS data is not realtime")
                    End If

                    lastStatus = If(isRealtime, currentStatus, "NotRealtime")
                End If
            End While

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"MonitorOxtsRtkStatus: {ex.Message}")
        End Try
    End Sub

    ' Picks the best owner for modal dialogs to ensure they stay on top of your app windows.
    Private Function GetBestModalOwner() As Form
        ' Prefer any currently open modal dialog
        If Application.OpenForms IsNot Nothing Then
            For Each f As Form In Application.OpenForms
                If f IsNot Nothing AndAlso Not f.IsDisposed AndAlso f.IsHandleCreated AndAlso f.Visible AndAlso f.Modal Then
                    Return f
                End If
            Next
        End If
        ' Next prefer the main in-vehicle screen if visible
        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed AndAlso OnVehicleScreen.IsHandleCreated AndAlso OnVehicleScreen.Visible Then
            Return OnVehicleScreen
        End If
        ' Fallback to this form
        Return Me
    End Function

    ''' <summary>
    ''' Handles the logic for "Toast Dismissal".
    ''' </summary>
    Public Sub DismissInitToast(Optional fade As Boolean = True)
        ' Dismiss every sticky toast we created during init
        SyncLock _initToastIds
            For Each id In _initToastIds
                Try
                    If id <> Guid.Empty Then StatusNotifier.ToastDismiss(id, fade:=fade)
                Catch
                    ' ignore dismissal race conditions
                End Try
            Next
            _initToastIds.Clear()
        End SyncLock

        ' Back-compat: also dismiss any legacy single ID if still set
        If _initToastId <> Guid.Empty Then
            Try
                StatusNotifier.ToastDismiss(_initToastId, fade:=fade)
            Catch
            End Try
            _initToastId = Guid.Empty
        End If
    End Sub

    ''' <summary>
    ''' Handles the logic for "NEW FULL" signal registration.
    ''' </summary>
    Private Sub HandleNewExperimentIfNeeded()
        If SignalRegistrationMode <> "NEW FULL" Then Return

        ' If we are creating a new experiment from a blank experiment...
        Dim invalidLogPath = Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv")
        If File.Exists(invalidLogPath) Then
            File.Delete(invalidLogPath)
        End If

        Dim newExpName As String = Path.GetFileName(INCAVariableFile)
        If InStr(newExpName, ".xlsx") > 0 Then
            newExpName = Mid(newExpName, 1, InStr(newExpName, ".xlsx") - 1)
        ElseIf InStr(newExpName, ".csv") > 0 Then
            newExpName = Mid(newExpName, 1, InStr(newExpName, ".csv") - 1)
        Else
            HandleUserMessageLogging("GMRC", "Invalid Signal List Name. Exiting...", DisplayMsgBox)
            InitForm.Close()
            Close()
            End
        End If

        INCAExperiment = newExpName
        InitialINCAExperiment = INCAExperiment
        _registerIntoNewBlankExp = True
    End Sub

    ''' <summary>
    ''' Spawns a separate thread to monitor the health of the application and allow user to kill processes if problems arise.
    ''' </summary>
    Private Sub StartProcessKillerThread()
        _MyKillerThread = New Thread(AddressOf ProcessKiller)
        _MyKillerThread.SetApartmentState(ApartmentState.STA)
        _MyKillerThread.Start()
    End Sub

    ''' <summary>
    ''' Launch ATT_TCP if not in debug mode.
    ''' </summary>
    Private Sub LaunchAttTcpIfNeeded()
        If Not Debugger.IsAttached Then
            LaunchATT_TCP()
        End If
    End Sub

    ''' <summary>
    ''' Initializes INCA, handles all the error messages and returns success/failure.
    ''' </summary>
    Private Function InitializeINCA() As Boolean

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", $"Initialize: Initializing INCA with Workspace={INCAWorkspace}, Experiment={INCAExperiment}...")
        HandleUserMessageLogging("GMRC", " ")
        StatusNotifier.Toast("Initialize: Initializing INCA...", "INCA", durationMs:=2000, ensureMainOnTop:=False)

        Dim forceInit As Boolean = True
        Dim errorMsg As String = ""
        Dim status = MyIncaInterface.InitINCA(INCADatabase, INCAWorkspace, INCAExperiment, forceInit, errorMsg, _registerIntoNewBlankExp, showToasts:=False)

        If status = IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then
            ' The large If/Else block that checks for errorMsg contents
            If Not HandleIncaErrorMessages(errorMsg) Then
                Return False
            End If
        Else
            If InStr(errorMsg, "WARNING") > 0 Then
                TopMost = True
                HandleUserMessageLogging("GMRC", "Initialize: INCA WARNING Message " & errorMsg, DisplayMsgBox)
            End If
        End If

        HandleUserMessageLogging("GMRC", "Initialize: INCA Initialized.")
        StatusNotifier.Toast("Initialize: INCA Initialized.", "INCA", durationMs:=2000, ensureMainOnTop:=False)

        MyIncaInterface.SetProjectDatabaseInfo()
        ReferenceDataSetDataBasePaths = MyIncaInterface.GetReferenceDataSetDataBasePaths()
        WorkingDataSetDataBasePaths = MyIncaInterface.GetWorkingDataSetDataBasePaths()

        Return True
    End Function

    ''' <summary>
    ''' Centralizes the large block of error-checking logic for INCA initialization error messages.
    ''' Returns True if we can continue, False if we must exit.
    ''' </summary>
    Private Function HandleIncaErrorMessages(ByVal errorMsg As String) As Boolean
        Dim exitFlag As Boolean = True
        Dim exitMode As String = "Complete"

        HandleUserMessageLogging("GMRC", "Initialize: Initialize INCA Returned - " & errorMsg)
        TopMost = True

        Select Case True
            Case errorMsg.Contains("CLEVIR Cannot be started with an Experiment open in INCA")
                HandleUserMessageLogging("GMRC", $"Initialize: {errorMsg} - Exiting", DisplayMsgBox)

            Case errorMsg.Contains("Experiment")
                HandleUserMessageLogging("GMRC", "Could not initialize INCA - Check config.xml for correct Experiment name. Exiting...", DisplayMsgBox)

            Case errorMsg.Contains("CheckCodePageConform Returned FALSE")
                exitFlag = Not HandleCodePageMismatch()

            Case errorMsg.Contains("Returned FALSE") OrElse errorMsg.Contains("Page = FAIL")
                exitFlag = Not HandleChecksumMismatch()

            Case errorMsg.Contains("NOT CONNECTED")
                exitFlag = Not HandleNotConnectedError(errorMsg)

            Case errorMsg.Contains("Workspace")
                HandleUserMessageLogging("GMRC", "Initialize: Could not initialize INCA - Check config.xml for correct Workspace name. Exiting...", DisplayMsgBox)

            Case errorMsg.Contains("Database")
                HandleUserMessageLogging("GMRC", "Initialize: Could not initialize INCA - Check config.xml for correct INCA Database name. Exiting...", DisplayMsgBox)

            Case Else
                HandleUserMessageLogging("GMRC", "Initialize: Could not initialize INCA - Please Check config.xml for correct database/workspace/experiment. Exiting...", DisplayMsgBox)
        End Select

        If exitFlag Then
            HandleUserMessageLogging("GMRC", "Initialize: Exiting - INCA Init Unsuccessful")
            ExitApp(exitMode)
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' Handle a code page mismatch scenario.
    ''' Return True to continue, False to exit.
    ''' </summary>
    Private Function HandleCodePageMismatch() As Boolean
        If Not DebugMode Then
            HandleUserMessageLogging("GMRC", "CODE PAGE MISMATCH - Please use INCA to FLASH controller. Exiting...", DisplayMsgBox)
            Return False
        ElseIf Not SignalRegistrationMode.Contains("FULL") Then
            If MsgBox("Detected CODE PAGE mismatch. Continue anyway?", vbYesNo) = vbYes Then
                HandleUserMessageLogging("GMRC", "Initialize: Continuing After CODE PAGE MISMATCH")
                Return True
            Else
                HandleUserMessageLogging("GMRC", "Initialize: NOT Continuing After CODE PAGE MISMATCH")
                Return False
            End If
        End If

        Return True
    End Function

    ''' <summary>
    ''' Handles the scenario of checksum mismatch. Returns True to continue, False to exit.
    ''' </summary>
    Private Function HandleChecksumMismatch() As Boolean
        Const Caption As String = "CLEVIR/INCA — Checksum Mismatch"
        Dim ignoreFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "IgnoreChecksumMismatch.txt")

        If File.Exists(ignoreFilePath) Then Return True

        Dim owner As Form = GetBestModalOwner()

        If Not DebugMode Then
            ' Production mode — hard stop, no override allowed.
            MessageBox.Show(
                If(owner IsNot Nothing AndAlso owner.IsHandleCreated AndAlso owner.Visible, owner, Nothing),
                "Checksum mismatch. Please use INCA to FLASH matching calibration. Exiting...",
                Caption, MessageBoxButtons.OK, MessageBoxIcon.Error)
            HandleUserMessageLogging("GMRC", "Initialize: NOT Continuing After checksum MISMATCH")
            Return False
        End If

        If Not SignalRegistrationMode.Contains("FULL") Then
            ' Debug / partial mode — allow the user to override.
            Dim continueResult As DialogResult = MessageBox.Show(
                If(owner IsNot Nothing AndAlso owner.IsHandleCreated AndAlso owner.Visible, owner, Nothing),
                "Checksum mismatch. Continue anyway?",
                Caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)

            If continueResult <> DialogResult.Yes Then
                HandleUserMessageLogging("GMRC", "Initialize: NOT Continuing After checksum MISMATCH")
                Return False
            End If

            HandleUserMessageLogging("GMRC", "Initialize: Continuing After checksum MISMATCH")

            ' Optionally suppress this prompt for future sessions.
            Dim suppressResult As DialogResult = MessageBox.Show(
                If(owner IsNot Nothing AndAlso owner.IsHandleCreated AndAlso owner.Visible, owner, Nothing),
                "Ignore this message in future sessions?",
                Caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)

            If suppressResult = DialogResult.Yes Then
                File.WriteAllText(ignoreFilePath, "Ignore Checksum Mismatch")
            End If
        End If

        Return True
    End Function

    ''' <summary>
    ''' Handles the “NOT CONNECTED” error. Returns True to continue, False to exit.
    ''' </summary>
    Private Function HandleNotConnectedError(ByVal errorMsg As String) As Boolean
        If Not DebugMode Then
            HandleUserMessageLogging("GMRC", errorMsg & " Please verify physical connections to XETK hardware. Exiting...", DisplayMsgBox)
            Return False
        ElseIf Not SignalRegistrationMode.Contains("FULL") Then
            If Not Debugger.IsAttached Then
                ' Ensure the message box is shown on top by using an owned modal dialog
                Dim owner As Form = GetBestModalOwner()
                Dim wasTopMost As Boolean = False
                If owner IsNot Nothing Then
                    wasTopMost = owner.TopMost
                    owner.TopMost = True
                End If

                Dim result As DialogResult
                Try
                    Dim text As String = errorMsg & " Continue anyway?"
                    Dim caption As String = "CLEVIR/INCA"
                    If owner IsNot Nothing AndAlso owner.IsHandleCreated AndAlso owner.Visible Then
                        result = MessageBox.Show(owner, text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
                    Else
                        result = MessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
                    End If
                Finally
                    If owner IsNot Nothing Then owner.TopMost = wasTopMost
                End Try

                If result = DialogResult.Yes Then
                    HandleUserMessageLogging("GMRC", "Initialize: Continuing After " & errorMsg)
                    Return True
                Else
                    HandleUserMessageLogging("GMRC", "Initialize: NOT Continuing After " & errorMsg)
                    Return False
                End If
            Else
                Return True
            End If
        End If
        Return True
    End Function

    ''' <summary>
    ''' If using Vehicle Spy, we set up the correct config file based on ARXML mapping.
    ''' </summary>
    Private Sub ConfigureVehicleSpyIfNeeded()
        If AlternateRecordingMode <> "VehicleSpy" OrElse Not AlternateRecordEnabled OrElse Not VehicleSpyCaptureStarted Then Return

        Select Case ProjectName
            Case "LowContent"
                ReadArxmlMappingFileNew(ProjectDatabaseNames(0))

            Case "HighContent"
                ' Make sure to feed in the HCS database name if there are multiple
                If ProjectDatabaseNames.Any(Function(x) x.Contains("HCS")) Then
                    Dim hcsDatabase = ProjectDatabaseNames.First(Function(x) x.Contains("HCS"))
                    ReadArxmlMappingFileNew(hcsDatabase)
                End If

            Case Else
                ' Example for FCM, or other project names:
                If ProjectName.Contains("FCM") Then
                    ' Scanning for “FCM”, “LC”, “HCS” etc.
                    If InStr(ProjectDatabaseNames(0), "FCM") > 0 And UBound(ProjectDatabaseNames) = 0 Then 'FCM Stand alone, no other processors
                        ReadArxmlMappingFileNew(ProjectDatabaseNames(0))
                    ElseIf UBound(ProjectDatabaseNames) = 1 Then 'FCM LCM, one other processor would be XETK:1, low content
                        If InStr(ProjectDatabaseNames(0), "LC") > 0 Then
                            ReadArxmlMappingFileNew(ProjectDatabaseNames(0))
                        Else
                            ReadArxmlMappingFileNew(ProjectDatabaseNames(1))
                        End If
                    ElseIf UBound(ProjectDatabaseNames) = 2 Then 'FCM LCH two addtional processors, HCS and HCF, high content. select projectdatabasename of the HCS processor...
                        If InStr(ProjectDatabaseNames(0), "HCS") > 0 Then
                            ReadArxmlMappingFileNew(ProjectDatabaseNames(0))
                        ElseIf InStr(ProjectDatabaseNames(1), "HCS") > 0 Then
                            ReadArxmlMappingFileNew(ProjectDatabaseNames(1))
                        ElseIf InStr(ProjectDatabaseNames(2), "HCS") > 0 Then
                            ReadArxmlMappingFileNew(ProjectDatabaseNames(2))
                        End If
                    Else
                        HandleUserMessageLogging("GMRC", "Initialize: Invalid ProjectDatabaseName for " & ProjectName)
                    End If

                End If
        End Select

        'HandleUserMessageLogging("GMRC", "Initialize: Sending VSpy Command: loadfile " & Path.Combine(My.Application.Info.DirectoryPath, "VehicleSpy", VSpySelectedConfigFileName))
        'SendVSpyCommand("loadfile " & Path.Combine(My.Application.Info.DirectoryPath, "VehicleSpy", VSpySelectedConfigFileName))

        ' Wait for load to complete
        'Thread.Sleep(5000)
    End Sub

    ''' <summary>
    ''' Displays the appropriate form based on current operating mode.
    ''' </summary>
    Private Sub ShowAppropriateForm()
        If OperatingMode = OperatingModes.ResOnVpc Then
            'OnVehicleScreen.Show()
            'OnVehicleScreen.BringToFront()
            OnVehicleScreen.Activate()
            OnVehicleScreen.Refresh()

        End If

        ' Fade out the sticky init status now that the form has changed
        DismissInitToast(True)
    End Sub

    ''' <summary>
    ''' Creates the top-down container and sets up the menus for forms.
    ''' </summary>
    Private Sub InitializeUIAndMenus()
        MyTdGraphicsContainer = New TDGraphicsContainerClass()
        MyTdGraphicsContainer.SetupTopDownView()
        ' 0 means start with form index 0, create menus for all
        CreateMenus(0)
    End Sub

    ''' <summary>
    ''' Registers signals with INCA. Returns False if registration fails.
    ''' </summary>
    Private Function RegisterAllSignals() As Boolean
        Try
            ' ✅ FIX: Use MyIncaInterface.MySignals instead of MyDeviceRasterSignals
            If MyIncaInterface.mySignals Is Nothing OrElse MyIncaInterface.mySignals.Length = 0 Then
                HandleUserMessageLogging("GMRC", "RegisterAllSignals: No signals loaded from signal list!", DisplayMsgBox)
                Return False
            End If

            ' ✅ CRITICAL: Suppress the old InitializationMonitor UI during registration
            _suppressInitMonitorUI = True  ' ← Prevent old form from showing

            ' Create and SHOW the new progress form BEFORE registration starts
            Dim progressForm As New SignalRegistrationProgressForm(MyIncaInterface.mySignals.Length)
            progressForm.Show()  ' Non-modal so updates work
            progressForm.BringToFront()
            progressForm.Refresh()
            Application.DoEvents()  ' Force immediate display

            ' Small delay to ensure form is fully rendered
            Thread.Sleep(100)

            ' ✅ Pass progress form to INCA_InterfaceClass wrapper
            Dim result As Boolean = MyIncaInterface.RegisterSignals(progressForm)

            If Not result Then
                progressForm.ShowCompletion(False, "Signal registration failed. Check logs for details.")
                Return False
            End If

            progressForm.ShowCompletion(True)
            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"RegisterAllSignals: {ex.Message}", DisplayMsgBox)
            Return False
        Finally
            ' ✅ Re-enable the old UI after registration completes
            _suppressInitMonitorUI = False  ' ← Restore normal behavior
        End Try
    End Function

    ''' <summary>
    ''' Handles the scenario of FULL registration by saving experiment, optionally shutting down and restarting INCA.
    ''' </summary>
    Private Sub HandleFullRegistrationScenario()
        If Not SignalRegistrationMode.Contains("FULL") Then Return

        If Not MyIncaInterface.SaveExperiment() Then
            HandleUserMessageLogging("GMRC", "Initialize: Save Experiment returned FALSE",,,,,, OnVehicleScreen.Label2)
        Else
            If Not ConfigureForNewSoftwareVersion Then
                HandleUserMessageLogging("GMRC", "Initialize: Save Experiment returned TRUE. Shutting down and Restarting INCA...",,,,,, OnVehicleScreen.Label2)

                SignalRegistrationMode = "DISPLAYS"
                ShutdownAndRestartInca(18000)
            End If
        End If
    End Sub

    ''' <summary>
    ''' Handles final UI touches before starting the background worker, e.g. showing OnVehicleScreen for VPC mode.
    ''' </summary>
    Private Sub FinalizeUI()
        If OperatingMode = OperatingModes.ResOnVpc Then
            Hide()
            OnVehicleScreen.BringToFront()
            OnVehicleScreen.Show()
            OnVehicleScreen.Activate()
            OnVehicleScreen.TopMost = False
        Else
            TopMost = False
            Contains(OnVehicleScreen)
            Activate()
        End If

        ' Clean up data directories

        DeleteUnusedDirectories(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber)

        ' If using Vehicle Spy
        If AlternateRecordingMode = "VehicleSpy" AndAlso AlternateRecordEnabled Then
            HandleDIDPull()
        End If

        _initializing = False

        If Not PlaybackMode Then
            CheckForExperiment = True
        End If

        Cursor = Cursors.Arrow
        Button1.Visible = True

        ' If certain logins, enable calibrate button
        If (UCase(SaveLoginID) <> "DEMO" AndAlso Not UCase(SaveLoginID).Contains("DRVR")) OrElse Debugger.IsAttached Then
            OnVehicleScreen.Button8.Visible = True
        End If

        OnVehicleScreen.Label1.Visible = True
        OnVehicleScreen.PictureBox1.SendToBack()
        OnVehicleScreen.TextBox1.Visible = True
        OnVehicleScreen.TextBox1.BringToFront()
        OnVehicleScreen.RadioButton1.Visible = True
        OnVehicleScreen.RadioButton2.Visible = True

        ' Escalation Processing
        OnVehicleScreen.Button9.Visible = True
        If File.Exists(Path.Combine(My.Application.Info.DirectoryPath, "EscalationExeName.txt")) AndAlso
       Directory.Exists(Path.Combine(My.Application.Info.DirectoryPath, "process_supercruise_events")) AndAlso
       Directory.Exists(Path.Combine(My.Application.Info.DirectoryPath, "assets")) Then

            OnVehicleScreen.Button9.BackColor = Color.LightGreen
            HandleUserMessageLogging("GMRC", "Initialize: Processing Escalations Enabled",,, FlashMsg2Sec)
            ProcessEscalations = True

        ElseIf File.Exists(Path.Combine(My.Application.Info.DirectoryPath, "EscalationExeName.txt")) AndAlso
           File.Exists(Path.Combine(My.Application.Info.DirectoryPath, "process_supercruise_events.zip")) AndAlso
           File.Exists(Path.Combine(My.Application.Info.DirectoryPath, "assets.zip")) Then

            OnVehicleScreen.Button9.BackColor = Color.Yellow
            HandleUserMessageLogging("GMRC", "Initialize: Processing Escalations PENDING",,, FlashMsg2Sec)
        Else
            OnVehicleScreen.Button9.BackColor = Color.Red
            HandleUserMessageLogging("GMRC", "Initialize: Processing Escalations Not Available",,)
        End If
    End Sub

    ''' <summary>
    ''' Handles final advanced UI tasks, e.g. enabling alt-record label or other finishing touches.
    ''' </summary>
    Private Sub ConfigureAdvancedUI()
        If AlternateRecordEnabled Then
            OnVehicleScreen.Label3.Visible = True  ' alt record status
        End If
        'Disable the status thread loop and trigger it to display the final status...
        ProgressBarEnable = False
        HandleUserMessageLogging("GMRC", "Initialize: Initialization Complete")
        StatusNotifier.Toast("Initialize: Initialization Complete", "CLEVIR", durationMs:=2000, ensureMainOnTop:=False)
        GroupBox1.Visible = False
        OnVehicleScreen.GroupBox1.Visible = False
        OnVehicleScreen.TextBox1.Visible = True
        Refresh()
    End Sub

    Private Sub InitializeApplication()
        ' Load and parse the data dictionary
        ParseDataDictionary()

        ' Initialize voice recognition
        Dim MyVoiceRecognition = DataDictionarySingleton.VoiceRecognitionManager.Instance

        ' Enable the UI element tied to voice activation
        MyVoiceRecognition.InitVoice()
    End Sub

    ''' <summary>
    ''' ❌ DEPRECATED: This function is no longer used.
    ''' Camera IP addresses are now managed in config.xml and mapped via ActiveCameras.
    ''' Kept for backward compatibility but does nothing.
    ''' </summary>
    Private Sub WriteCameraIpAddressesFile()
        ' ❌ DEPRECATED: No longer writes to CameraIPAddresses.txt
        ' Camera configuration is now managed in config.xml
        HandleUserMessageLogging("GMRC", "WriteCameraIpAddressesFile: Function is deprecated - camera IPs now managed in config.xml")
    End Sub

    ''' <summary>
    ''' ❌ DEPRECATED: This function is no longer used.
    ''' Camera IP addresses are now read from config.xml at startup.
    ''' Kept for backward compatibility but does nothing.
    ''' </summary>
    Private Sub ReadCameraIpAddressesFile()
        ' ❌ DEPRECATED: No longer reads from CameraIPAddresses.txt
        ' Camera configuration is now loaded from config.xml via Module1.ReadCameraConfiguration()
        HandleUserMessageLogging("GMRC", "ReadCameraIpAddressesFile: Function is deprecated - camera IPs now loaded from config.xml")
    End Sub

    Public Function ReadUserConfigFile(ByVal configFileName As String) As Integer
        ' Called from the Initialize routine when the user logs in with a USER ID.
        ' Reads the user-specific config file (e.g., userid.xml), extracts configuration information,
        ' and assigns it to variables.

        Dim lUserConfigFileName As String = Path.Combine(My.Application.Info.DirectoryPath, configFileName)
        Dim returnValue As Integer = 0

        HandleUserMessageLogging("GMRC", "ReadUserConfigFile: Reading User Config File " & lUserConfigFileName & "...")

        If Not File.Exists(lUserConfigFileName) Then
            HandleUserMessageLogging("GMRC", "ReadUserConfigFile: Copying Default Config File to " & lUserConfigFileName & "...")
            File.Copy(Path.Combine(My.Application.Info.DirectoryPath, "config.xml"), lUserConfigFileName)
            Thread.Sleep(1000)
        End If

        ' ✅ Read configuration into variables
        If ReadConfiguration(lUserConfigFileName) Then
            HandleUserMessageLogging("GMRC", "ReadUserConfigFile: Reading User Config File Complete")

            ' ✅ NEW: Sync UI controls with loaded values
            SyncUIFromConfig()
        Else
            HandleUserMessageLogging("GMRC", "ReadUserConfigFile: Failed to read user config file.")
            returnValue = -1 ' Indicate failure
        End If

        Return returnValue
    End Function

    ''' <summary>
    ''' Safely reads a Boolean config value from XML with logging.
    ''' </summary>
    Private Function ReadBooleanConfig(parentNode As XmlNode,
                                       nodeName As String,
                                       defaultValue As Boolean) As Boolean
        Dim node = parentNode.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Dim value As Boolean = CBool(node.InnerText)
            HandleUserMessageLogging("GMRC", $"{nodeName} set to: {value}")
            Return value
        End If

        HandleUserMessageLogging("GMRC", $"{nodeName} not found, using default: {defaultValue}")
        Return defaultValue
    End Function

    ''' <summary>
    ''' Safely reads a String config value from XML with logging.
    ''' </summary>
    Private Function ReadStringConfig(parentNode As XmlNode,
                                      nodeName As String,
                                      defaultValue As String) As String
        Dim node = parentNode.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Dim value As String = node.InnerText.Trim()
            HandleUserMessageLogging("GMRC", $"{nodeName} set to: {value}")
            Return value
        End If

        HandleUserMessageLogging("GMRC", $"{nodeName} not found, using default: {defaultValue}")
        Return defaultValue
    End Function

    ''' <summary>
    ''' Safely reads an Integer config value from XML with logging.
    ''' </summary>
    Private Function ReadIntegerConfig(parentNode As XmlNode,
                                       nodeName As String,
                                       defaultValue As Integer) As Integer
        Dim node = parentNode.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Dim value As Integer = CInt(node.InnerText)
            HandleUserMessageLogging("GMRC", $"{nodeName} set to: {value}")
            Return value
        End If

        HandleUserMessageLogging("GMRC", $"{nodeName} not found, using default: {defaultValue}")
        Return defaultValue
    End Function
    Public Function ReadConfiguration(configFilePath As String) As Boolean
        ' Centralized helper to read any XML-based configuration file.
        ' Returns True on success, False on failure.
        If Not File.Exists(configFilePath) Then
            HandleUserMessageLogging("GMRC", $"ReadConfiguration: File not found at {configFilePath}")
            Return False
        End If

        Try
            Dim xmlDoc As New XmlDocument()
            xmlDoc.Load(configFilePath)
            Dim root As XmlNode = xmlDoc.DocumentElement

            ' ✅ REFACTORED: Use helper functions for clean, null-safe reading
            INCADatabase = ReadStringConfig(root, "INCADatabase", "")
            INCAWorkspace = ReadStringConfig(root, "INCAWorkspace", "")
            INCAExperiment = ReadStringConfig(root, "INCAExperiment", "")
            InitialINCAExperiment = INCAExperiment
            ETAS_USER_PATH = ReadStringConfig(root, "ETAS_USER_PATH", "")
            BaseDataCollectionPath = ReadStringConfig(root, "BaseDataCollectionPath", "C:\HB")

            ' File path handling
            INCAVariableFile = ReadStringConfig(root, "INCAVariableFile", "")
            If Not INCAVariableFile.Contains("\") Then
                INCAVariableFile = Path.Combine(My.Application.Info.DirectoryPath, "SignalLists", INCAVariableFile)
            End If

            ' Integer configs
            RecordWAVTime = ReadIntegerConfig(root, "RecordWAVTime", 30)
            RecordFileDurationMinutes = ReadIntegerConfig(root, "RecordFileDurationMinutes", 5)
            APICommErrorMsgDelayTime = ReadIntegerConfig(root, "APICommErrorMsgDelayTime", 90)
            MaxCameras = ReadIntegerConfig(root, "MaxCameras", 9)

            ' Boolean configs
            MuteVoiceRecordingMessages = ReadBooleanConfig(root, "MuteVoiceRecordingMessages", False)
            AudioToTextConversion = ReadBooleanConfig(root, "AudioToTextConversion", False)
            SaveCalSnapshotEnabled = ReadBooleanConfig(root, "SaveCalSnapshotEnabled", False)

            ' ════════════════════════════════════════════════════════════
            ' ALTERNATE RECORDING CONFIGURATION SECTION
            ' ════════════════════════════════════════════════════════════
            AlternateRecordEnabled = ReadBooleanConfig(root, "AlternateRecordEnabled", False)
            AlternateRecordConfig = ReadStringConfig(root, "AlternateRecordConfig", "CANalyzer")
            EnableAltRecReStartAfterRecordStop = ReadBooleanConfig(root, "EnableAltRecReStartAfterRecordStop", False)


            ' ================================================================
            ' ✅ NEW: Compression configuration (nested structure)
            ' ================================================================
            Dim compressionNode As XmlNode = root.SelectSingleNode("Compression")
            If compressionNode IsNot Nothing Then
                CompressMF4 = ReadBooleanConfig(compressionNode, "CompressMF4", True)
                CompressPCAP = ReadBooleanConfig(compressionNode, "CompressPCAP", True)
                CompressASC = ReadBooleanConfig(compressionNode, "CompressASC", False)
                CompressVSB = ReadBooleanConfig(compressionNode, "CompressVSB", False)
                DeleteAfterCompression = ReadBooleanConfig(compressionNode, "DeleteAfterCompression", False)
                CompressionLevel = ReadIntegerConfig(compressionNode, "CompressionLevel", 1)
                ZipCompressionMaxRetries = ReadIntegerConfig(compressionNode, "MaxRetries", 5)
                ZipCompressionRetryDelay = ReadIntegerConfig(compressionNode, "RetryDelaySeconds", 10)
                ZipFileLockTimeout = ReadIntegerConfig(compressionNode, "FileLockTimeoutSeconds", 30)
            Else
                ' ✅ BACKWARD COMPATIBILITY: Read flat structure (old config files)
                CompressMF4 = ReadBooleanConfig(root, "CompressMF4", True)
                CompressPCAP = ReadBooleanConfig(root, "CompressPCAP", True)
                CompressASC = False
                CompressVSB = False
                DeleteAfterCompression = False
                CompressionLevel = 1
                CompressionMaxRetries = 5
                CompressionRetryDelay = 10
                ZipCompressionRetryDelay = 10
                ZipFileLockTimeout = 30

                HandleUserMessageLogging("GMRC", "ReadConfiguration: Using legacy flat compression config")
            End If

            LidarCaptureEnabled = ReadBooleanConfig(root, "LidarCaptureEnabled", False)

            ' Signal registration mode (radio buttons will be set later in LoginForm_Load)
            Dim signalRegMode = ReadStringConfig(root, "SignalRegistrationMode", "DISPLAYS").ToUpper()
            SignalRegistrationMode = signalRegMode
            SaveSignalRegistrationMode = signalRegMode

            ' Validate and normalize the value
            Select Case signalRegMode
                Case "FULL", "DISPLAYS", "GO/NOGO", "NEW FULL"
                    ' Valid mode - keep as-is
                Case Else
                    ' Invalid mode - default to DISPLAYS
                    SignalRegistrationMode = "DISPLAYS"
                    SaveSignalRegistrationMode = "DISPLAYS"
            End Select

            HandleUserMessageLogging("GMRC", $"ReadConfiguration: SignalRegistrationMode set to '{SignalRegistrationMode}' (UI update deferred)")

            ' Base data collection path validation
            BaseDataCollectionPath = ReadStringConfig(root, "BaseDataCollectionPath", My.Application.Info.DirectoryPath)
            If BaseDataCollectionPath.Contains(" ") Then
                HandleUserMessageLogging("GMRC", "BaseDataCollectionPath cannot contain spaces. Exiting...", DisplayMsgBox)
                Return False
            End If

            ' Network drive configuration
            NetworkDriveLetter = ReadStringConfig(root, "NetworkDriveLetter", "Q:")
            NetworkDriveMapping = ReadStringConfig(root, "NetworkDriveMapping", "")

            ' ════════════════════════════════════════════════════════════
            ' OXTS CONFIGURATION SECTION (New GPS/INS Integration)
            ' ════════════════════════════════════════════════════════════
            Dim oxtsNode As XmlNode = root.SelectSingleNode("OxtsConfiguration")
            If oxtsNode IsNot Nothing Then
                OxtsEnabled = ReadBooleanConfig(oxtsNode, "OxtsEnabled", False)
                OxtsNcomIpAddress = ReadStringConfig(oxtsNode, "NcomIpAddress", "10.5.55.200")
                OxtsNcomPort = ReadIntegerConfig(oxtsNode, "NcomPort", 3000)
                OxtsGpsLockTimeout = ReadIntegerConfig(oxtsNode, "GpsLockTimeout", 30000)
                OxtsWaitForLockOnStart = ReadBooleanConfig(oxtsNode, "WaitForLockOnStart", True)

                HandleUserMessageLogging("GMRC", $"OXTS Configuration: Enabled={OxtsEnabled}, IP={OxtsNcomIpAddress}:{OxtsNcomPort}")
            Else
                ' Defaults when node is missing
                OxtsEnabled = False
                OxtsNcomIpAddress = "10.5.55.200"
                OxtsNcomPort = 3000
                OxtsGpsLockTimeout = 30000
                OxtsWaitForLockOnStart = True
                HandleUserMessageLogging("GMRC", "No OxtsConfiguration found - using defaults (disabled)")
            End If

            ' ════════════════════════════════════════════════════════════
            ' Time Sync Provider Selection
            ' ════════════════════════════════════════════════════════════
            Dim timeSyncNode As XmlNode = root.SelectSingleNode("TimeSyncConfiguration")
            If timeSyncNode IsNot Nothing Then
                TimeSyncEnabled = ReadBooleanConfig(timeSyncNode, "EnableTimeSync", False)
                TimeSyncProviderType = ReadStringConfig(timeSyncNode, "Provider", "OXTS").Trim().ToUpperInvariant()
            Else
                TimeSyncEnabled = OxtsEnabled
                TimeSyncProviderType = "OXTS"
            End If

            Dim tmNode As XmlNode = root.SelectSingleNode("TimeMachineConfiguration")
            If tmNode IsNot Nothing Then
                TimeMachineEnabled = ReadBooleanConfig(tmNode, "Enabled", False)
                TimeMachineIpAddress = ReadStringConfig(tmNode, "DeviceIp", "255.255.255.255")
                TimeMachinePort = ReadIntegerConfig(tmNode, "Port", 7372)
                TimeMachinePollMs = ReadIntegerConfig(tmNode, "PollMs", 1000)
                TimeMachinePtpAssumeLocked = ReadBooleanConfig(tmNode, "PtpAssumeLocked", True)
            Else
                TimeMachineEnabled = False
                TimeMachineIpAddress = "255.255.255.255"
                TimeMachinePort = 7372
                TimeMachinePollMs = 1000
                TimeMachinePtpAssumeLocked = True
            End If

            If TimeSyncProviderType = "TIMEMACHINE" AndAlso Not TimeMachineEnabled Then
                HandleUserMessageLogging("GMRC", "TimeSync provider set to TimeMachine but TimeMachineConfiguration.Enabled is False - falling back to OXTS")
                TimeSyncProviderType = "OXTS"
            End If

            HandleUserMessageLogging("GMRC", $"TimeSync Configuration: Enabled={TimeSyncEnabled}, Provider={TimeSyncProviderType}")

            ' ════════════════════════════════════════════════════════════
            ' LIDAR CONFIGURATION SECTION (Legacy + Multi-Device)
            ' ════════════════════════════════════════════════════════════
            ReadLidarConfiguration(root)

            ' ════════════════════════════════════════════════════════════
            ' ✅ NEW: CAMERA CONFIGURATION SECTION
            ' ════════════════════════════════════════════════════════════
            Dim cameraConfigNode As XmlNode = root.SelectSingleNode("CameraConfiguration")
            If cameraConfigNode IsNot Nothing Then
                ReadCameraConfiguration(cameraConfigNode)
            Else
                ' Load defaults if section missing
                LoadDefaultCameraConfig()
                HandleUserMessageLogging("GMRC", "No CameraConfiguration found - using default IP mappings")
            End If

            ' ════════════════════════════════════════════════════════════
            ' Load OXTS NCOM PCAP capture configuration
            ' ════════════════════════════════════════════════════════════
            LoadOxtsCaptureConfig(root)

            ' Current vehicle usage
            Dim vehicleUsage = ReadStringConfig(root, "CurrentVehicleUsage", "DEVELOPMENT").Trim().ToUpper()
            CurrentVehicleUsage = vehicleUsage

            Select Case vehicleUsage
                Case "DEVELOPMENT" : InitForm.RadioButton1.Checked = True
                Case "VALIDATION" : InitForm.RadioButton2.Checked = True
            End Select

            HandleUserMessageLogging("GMRC", "ReadConfiguration: Configuration loaded successfully")

            ' Load session metadata dropdowns from SessionMetadata.xml (non-fatal if absent)
            Dim sessionMetaPath As String = Path.Combine(My.Application.Info.DirectoryPath, "SessionMetadata.xml")
            LoadSessionMetadataConfig(sessionMetaPath)

            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ReadConfiguration: Error reading '{configFilePath}' - {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Synchronizes OnVehicleScreen UI controls with configuration values.
    ''' Call this AFTER ReadConfiguration() to ensure UI reflects loaded settings.
    ''' </summary>
    Private Sub SyncUIFromConfig()
        Try
            ' ✅ FIX: Check if form exists AND controls are initialized
            If OnVehicleScreen Is Nothing OrElse
       OnVehicleScreen.IsDisposed OrElse
       OnVehicleScreen.TextBox1 Is Nothing OrElse
       OnVehicleScreen.ComboBox1 Is Nothing Then

                HandleUserMessageLogging("GMRC", "SyncUIFromConfig: OnVehicleScreen controls not yet initialized - will sync later")
                Return
            End If

            ' ════════════════════════════════════════════════════════════
            ' SYNC RECORD WAV TIME
            ' ════════════════════════════════════════════════════════════
            OnVehicleScreen.TextBox1.Text = RecordWAVTime.ToString()
            HandleUserMessageLogging("GMRC", $"SyncUIFromConfig: Set RecordWAVTime to {RecordWAVTime}")

            ' ════════════════════════════════════════════════════════════
            ' SYNC RECORD FILE DURATION
            ' ════════════════════════════════════════════════════════════
            OnVehicleScreen.ComboBox1.Text = RecordFileDurationMinutes.ToString()
            HandleUserMessageLogging("GMRC", $"SyncUIFromConfig: Set RecordFileDurationMinutes to {RecordFileDurationMinutes}")

            ' ════════════════════════════════════════════════════════════
            ' ✅ NEW: SYNC SIGNAL LIST + EXPERIMENT TO LOGINFORM.LABEL14
            ' ════════════════════════════════════════════════════════════
            If LoginForm IsNot Nothing AndAlso Not LoginForm.IsDisposed Then
                ' Extract just the filename (without path) for cleaner display
                Dim signalListFileName As String = If(
                String.IsNullOrEmpty(INCAVariableFile),
                "[Not Set]",
                Path.GetFileName(INCAVariableFile)
            )

                ' ✅ NEW: Include experiment name
                Dim experimentName As String = If(
                String.IsNullOrEmpty(INCAExperiment),
                "[Not Set]",
                INCAExperiment
            )

                ' Format: "SignalList.xlsx / ExperimentName"
                LoginForm.Label4.Text = $"{signalListFileName} / {experimentName}"

                HandleUserMessageLogging("GMRC",
                $"SyncUIFromConfig: Set Signal List display to '{signalListFileName} / {experimentName}'")
            End If

            HandleUserMessageLogging("GMRC", "SyncUIFromConfig: UI synchronized with configuration")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"SyncUIFromConfig: Error - {ex.Message}")
        End Try
    End Sub

    Private Sub ReadLidarConfiguration(root As XmlNode)
        Try
            ' ================================================================
            ' Read master enable flag (always check this first)
            ' ================================================================
            LidarCaptureEnabled = ReadBooleanConfig(root, "LidarCaptureEnabled", False)

            ' ================================================================
            ' Read multi-device configuration (N-LiDAR support)
            ' ================================================================
            Dim lidarDevicesNode As XmlNode = root.SelectSingleNode("LidarDevices")
            If lidarDevicesNode Is Nothing Then
                HandleUserMessageLogging("GMRC", "⚠️ No LidarDevices node found in configuration")
                Return
            End If

            ' ✅ CRITICAL FIX: Clear and rebuild from XML (source of truth)
            LidarDevices.Clear()

            Dim lidarNodes = lidarDevicesNode.SelectNodes("Lidar")
            If lidarNodes Is Nothing OrElse lidarNodes.Count = 0 Then
                HandleUserMessageLogging("GMRC", "No <Lidar> nodes found - LiDAR disabled")
                Return
            End If

            ' ================================================================
            ' Parse each <Lidar> node and register with Hesai SDK
            ' ================================================================
            Dim deviceIndex As Integer = 0

            For Each lidarNode As XmlNode In lidarNodes
                Try
                    Dim deviceId As String = lidarNode.Attributes("id")?.Value
                    Dim enabledAttr As String = lidarNode.Attributes("enabled")?.Value
                    Dim enabled As Boolean = If(String.IsNullOrEmpty(enabledAttr), True, Boolean.Parse(enabledAttr))

                    If Not enabled Then
                        HandleUserMessageLogging("GMRC", $"LiDAR '{deviceId}' disabled in config")
                        Continue For
                    End If

                    ' ✅ Parse device configuration
                    Dim adapterGuid As String = lidarNode.SelectSingleNode("AdapterGuid")?.InnerText
                    Dim ipAddress As String = lidarNode.SelectSingleNode("IpAddress")?.InnerText
                    Dim dataPortStr As String = lidarNode.SelectSingleNode("DataPort")?.InnerText
                    Dim imuPortStr As String = lidarNode.SelectSingleNode("ImuPort")?.InnerText

                    Dim dataPort As UShort = If(UShort.TryParse(dataPortStr, dataPort), dataPort, 2368US)
                    Dim imuPort As UShort = If(UShort.TryParse(imuPortStr, imuPort), imuPort, 8308US)

                    ' ✅ Create device object
                    Dim device As New LidarDevice(adapterGuid, ipAddress, dataPort, imuPort, deviceId) With {
                    .Enabled = enabled
                }

                    LidarDevices.Add(device)
                    HandleUserMessageLogging("GMRC", $"✅ Loaded LiDAR: {deviceId} at {ipAddress}:{dataPort}")

                    ' ================================================================
                    ' ✅ NEW: Register with Hesai SDK (simple or extended config)
                    ' ================================================================
                    If HesaiInterop.IsAvailable() Then
                        Dim hesaiConfigNode As XmlNode = lidarNode.SelectSingleNode("HesaiConfig")

                        If hesaiConfigNode IsNot Nothing Then
                            device.HesaiConfig = New HesaiInterop.HesaiDeviceConfig With {
                                .device_id = deviceId,
                                .ip_address = ipAddress,
                                .data_port = dataPort,
                                .correction_file_path = hesaiConfigNode.SelectSingleNode("CorrectionFilePath")?.InnerText,
                                .firetimes_path = hesaiConfigNode.SelectSingleNode("FiretimesPath")?.InnerText,
                                .host_ip_address = hesaiConfigNode.SelectSingleNode("HostIpAddress")?.InnerText,
                                .multicast_ip_address = hesaiConfigNode.SelectSingleNode("MulticastIpAddress")?.InnerText,
                                .ptc_port = Integer.Parse(If(hesaiConfigNode.SelectSingleNode("PtcPort")?.InnerText, "9347")),
                                .use_ptc_connected = Boolean.Parse(If(hesaiConfigNode.SelectSingleNode("UsePtcConnected")?.InnerText, "False")),
                                .enable_parser_thread = Boolean.Parse(If(hesaiConfigNode.SelectSingleNode("EnableParserThread")?.InnerText, "True")),
                                .enable_udp_thread = Boolean.Parse(If(hesaiConfigNode.SelectSingleNode("EnableUdpThread")?.InnerText, "True"))
                            }
                            device.HasHesaiConfig = True
                        Else
                            device.HesaiConfig = HesaiInterop.CreateDefaultConfig(deviceId, ipAddress, dataPort)
                            device.HasHesaiConfig = True
                        End If
                        HandleUserMessageLogging("GMRC", $"✅ Hesai config stored for {deviceId} (lazy registration)")
                        ' ❌ DON'T register here - happens in LidarDevice.StartCapture()
                    End If

                    deviceIndex += 1

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Failed to parse LiDAR node at index {deviceIndex}: {ex.Message}")
                End Try
            Next

            ' ================================================================
            ' Link active time sync provider to all loaded devices
            ' ================================================================
            If MyTimeSyncProvider IsNot Nothing Then
                For Each device In LidarDevices
                    device.SetTimeSyncProvider(MyTimeSyncProvider)
                Next
            End If

            HandleUserMessageLogging("GMRC", $"LiDAR config loaded: {LidarDevices.Count} devices")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ReadLidarConfiguration failed: {ex.Message}")
        End Try
    End Sub


    ''' <summary>
    ''' Helper method to create a default LiDAR device from legacy config values
    ''' </summary>
    Private Sub CreateDefaultLidarDevice(adapterGuid As String, ipAddress As String, dataPort As Integer, imuPort As Integer)
        LidarDevices.Clear()

        Dim defaultDevice As New LidarDevice With {
        .DeviceId = "LiDAR1",
        .LidarAdapterGuid = adapterGuid,
        .LidarIpAddress = ipAddress,
        .LidarDataPort = CUShort(dataPort),
        .LidarImuPort = CUShort(imuPort),
        .Enabled = True
    }

        LidarDevices.Add(defaultDevice)
        HandleUserMessageLogging("GMRC", $"Created default LiDAR device: IP={ipAddress}")
    End Sub

    Public Sub WriteUserConfigFile(ByVal filename As String)
        ' This routine writes data to the User specific config.xml file.
        ' It now acts as a simple wrapper around the centralized WriteConfiguration helper.
        Dim filePath As String = Path.Combine(My.Application.Info.DirectoryPath, filename)
        HandleUserMessageLogging("GMRC", $"WriteUserConfigFile: Writing to {filePath}...")
        WriteConfiguration(filePath)
    End Sub

    Public Sub WriteConfiguration(ByVal filePath As String)
        ' Centralized helper to write any XML-based configuration file.

        ' Validate essential configurations before writing
        If String.IsNullOrEmpty(INCADatabase) OrElse
       String.IsNullOrEmpty(INCAWorkspace) OrElse
       String.IsNullOrEmpty(INCAExperiment) OrElse
       String.IsNullOrEmpty(INCAVariableFile) Then

            HandleUserMessageLogging("GMRC", "WriteConfiguration: Cannot write config file - essential values are missing.")
            Return
        End If

        Try
            ' Use XmlWriter with proper settings for indentation and readability
            Dim settings As New XmlWriterSettings With {
            .Indent = True,
            .IndentChars = vbTab,
            .NewLineOnAttributes = False
        }

            Using writer As XmlWriter = XmlWriter.Create(filePath, settings)
                writer.WriteStartDocument()
                writer.WriteStartElement("Configuration") ' Root element

                ' ════════════════════════════════════════════════════════════
                ' INCA CONFIGURATION SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" INCA Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("INCADatabase", INCADatabase)
                writer.WriteElementString("INCAWorkspace", INCAWorkspace)
                writer.WriteElementString("INCAExperiment", InitialINCAExperiment)
                writer.WriteElementString("INCAVariableFile", INCAVariableFile)
                writer.WriteElementString("ETAS_USER_PATH", ETAS_USER_PATH)
                writer.WriteElementString("SignalRegistrationMode", SignalRegistrationMode)

                ' ════════════════════════════════════════════════════════════
                ' RECORDING CONFIGURATION SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Recording Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("RecordWAVTime", RecordWAVTime.ToString())
                writer.WriteElementString("RecordFileDurationMinutes", RecordFileDurationMinutes.ToString())
                writer.WriteElementString("MuteVoiceRecordingMessages", MuteVoiceRecordingMessages.ToString())
                writer.WriteElementString("AudioToTextConversion", AudioToTextConversion.ToString())

                ' ════════════════════════════════════════════════════════════
                ' ALTERNATE RECORDING SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Alternate Recording (CANalyzer/VehicleSpy) ")
                writer.WriteComment(" Valid AlternateRecordConfig values: VehicleSpy, CANalyzer, None ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("AlternateRecordEnabled", AlternateRecordEnabled.ToString())
                writer.WriteElementString("AlternateRecordConfig", AlternateRecordConfig)
                writer.WriteElementString("EnableAltRecReStartAfterRecordStop", EnableAltRecReStartAfterRecordStop.ToString())

                ' ════════════════════════════════════════════════════════════
                ' DATA STORAGE & NETWORK SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Data Storage & Network ")
                writer.WriteComment(" ================================================================ ")

                ' Handle BaseDataCollectionPath logic
                If BaseDataCollectionPath = NetworkDriveLetter AndAlso Not SaveBaseDataCollectionPath.Contains(NetworkDriveLetter) Then
                    BaseDataCollectionPath = SaveBaseDataCollectionPath
                End If
                writer.WriteElementString("BaseDataCollectionPath", BaseDataCollectionPath)

                ' Handle NetworkDriveLetter logic
                If UsingFlashDrive AndAlso Not SaveNetworkDriveLetter.Contains(NetworkDriveLetter) Then
                    writer.WriteElementString("NetworkDriveLetter", SaveNetworkDriveLetter)
                Else
                    writer.WriteElementString("NetworkDriveLetter", NetworkDriveLetter)
                End If

                writer.WriteElementString("NetworkDriveMapping", NetworkDriveMapping)

                ' ════════════════════════════════════════════════════════════
                ' COMPRESSION CONFIGURATION SECTION (NESTED)
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Compression Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteStartElement("Compression")

                ' File type compression switches
                writer.WriteComment(" File type compression switches ")
                writer.WriteElementString("CompressMF4", CompressMF4.ToString())
                writer.WriteElementString("CompressPCAP", CompressPCAP.ToString())
                writer.WriteElementString("CompressASC", CompressASC.ToString())
                writer.WriteElementString("CompressVSB", CompressVSB.ToString())

                ' Deletion policy
                writer.WriteComment(" Deletion policy (SAFE DEFAULT = False) ")
                writer.WriteElementString("DeleteAfterCompression", DeleteAfterCompression.ToString())

                ' Compression parameters
                writer.WriteComment(" Compression parameters ")
                writer.WriteElementString("CompressionLevel", CompressionLevel.ToString())
                writer.WriteComment(" 1=Fastest, 9=Best ")

                ' ✅ FIX: Use simple XML names to match ReadConfiguration
                writer.WriteElementString("MaxRetries", ZipCompressionMaxRetries.ToString())
                writer.WriteElementString("RetryDelaySeconds", ZipCompressionRetryDelay.ToString())

                ' Lock detection
                writer.WriteComment(" Lock detection ")
                writer.WriteElementString("FileLockTimeoutSeconds", ZipFileLockTimeout.ToString())

                writer.WriteEndElement() ' Close <Compression>

                ' ════════════════════════════════════════════════════════════
                ' HARDWARE CONFIGURATION SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Hardware Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("NetworkAdapterDescription", NetworkAdapterDescription)
                writer.WriteElementString("MaxCameras", MaxCameras.ToString())

                ' ════════════════════════════════════════════════════════════
                ' OXTS CONFIGURATION SECTION (GPS/INS Integration)
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" OXTS GPS/INS Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteStartElement("OxtsConfiguration")

                ' Write OXTS settings
                writer.WriteComment(" Enable/disable OXTS INS integration ")
                writer.WriteElementString("OxtsEnabled", OxtsEnabled.ToString())

                writer.WriteComment(" NCOM UDP listener settings ")
                writer.WriteElementString("NcomIpAddress", OxtsNcomIpAddress)
                writer.WriteElementString("NcomPort", OxtsNcomPort.ToString())

                writer.WriteComment(" GPS lock behavior ")
                writer.WriteElementString("GpsLockTimeout", OxtsGpsLockTimeout.ToString())
                writer.WriteElementString("WaitForLockOnStart", OxtsWaitForLockOnStart.ToString())

                writer.WriteEndElement() ' Close OxtsConfiguration

                writer.WriteComment(" Time Sync provider selection ")
                writer.WriteStartElement("TimeSyncConfiguration")
                writer.WriteElementString("EnableTimeSync", TimeSyncEnabled.ToString())
                writer.WriteElementString("Provider", TimeSyncProviderType)
                writer.WriteEndElement()

                writer.WriteComment(" TimeMachine Locator protocol settings ")
                writer.WriteStartElement("TimeMachineConfiguration")
                writer.WriteElementString("Enabled", TimeMachineEnabled.ToString())
                writer.WriteElementString("DeviceIp", TimeMachineIpAddress)
                writer.WriteElementString("Port", TimeMachinePort.ToString())
                writer.WriteElementString("PollMs", TimeMachinePollMs.ToString())
                writer.WriteElementString("PtpAssumeLocked", TimeMachinePtpAssumeLocked.ToString())
                writer.WriteEndElement()

                ' ════════════════════════════════════════════════════════════
                ' LIDAR CONFIGURATION SECTION (Legacy + Multi-Device)
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" LiDAR Configuration (Legacy + Multi-Device) ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("LidarCaptureEnabled", LidarCaptureEnabled.ToString())

                ' Write legacy single-device tags for backward compatibility
                writer.WriteComment(" Legacy single-device (backward compatibility) ")
                If LidarDevices.Count > 0 Then
                    writer.WriteElementString("LidarAdapterGuid", LidarDevices(0).LidarAdapterGuid)
                    writer.WriteElementString("LidarIpAddress", LidarDevices(0).LidarIpAddress)
                    writer.WriteElementString("LidarDataPort", LidarDevices(0).LidarDataPort.ToString())
                    writer.WriteElementString("LidarImuPort", LidarDevices(0).LidarImuPort.ToString())
                End If

                ' Write new multi-device structure
                writer.WriteComment(" Multi-device configuration ")
                writer.WriteStartElement("LidarDevices")

                If LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 Then
                    For i As Integer = 0 To LidarDevices.Count - 1
                        ' ✅ CRITICAL: Write device even if Enabled=False
                        writer.WriteStartElement("Lidar")
                        writer.WriteAttributeString("id", (i + 1).ToString())
                        writer.WriteAttributeString("enabled", LidarDevices(i).Enabled.ToString())

                        writer.WriteElementString("AdapterGuid", LidarDevices(i).LidarAdapterGuid)
                        writer.WriteElementString("IpAddress", LidarDevices(i).LidarIpAddress)
                        writer.WriteElementString("DataPort", LidarDevices(i).LidarDataPort.ToString())
                        writer.WriteElementString("ImuPort", LidarDevices(i).LidarImuPort.ToString())

                        writer.WriteEndElement() ' Close Lidar
                    Next
                End If

                writer.WriteEndElement() ' Close LidarDevices

                ' ════════════════════════════════════════════════════════════
                ' APPLICATION CONFIGURATION SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Application Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("CurrentVehicleUsage", CurrentVehicleUsage)
                writer.WriteElementString("SaveCalSnapshotEnabled", SaveCalSnapshotEnabled.ToString())
                writer.WriteElementString("APICommErrorMsgDelayTime", APICommErrorMsgDelayTime.ToString())

                writer.WriteEndElement() ' Close root element
                writer.WriteEndDocument()
            End Using

            HandleUserMessageLogging("GMRC", $"WriteConfiguration: Configuration file written to {filePath}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"WriteConfiguration: Error writing to '{filePath}' - {ex.Message}")
        End Try
    End Sub

    Private Sub WriteSignalListFile(ByVal signalListFileName As String)

        'Save data back to the Excel or .csv file which contains the variable and display information
        'If anything has been changed (using the online dynamic re-configuration), the changes will be saved into the
        'previously used excel spreadsheet or .csv file.  So, if we re-start the GmResidentClient
        'we will be using the spreadsheet or .csv file with changes made during the previous session.

        'This is called when we exit the main form, but only if changes have been made.

        'It is also possible to call this routine from a button press on the GM Resident Client Configuration form (GridCellPropConfig.vb)

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer
        Dim n As Integer

        Dim excelApp As Object = Nothing
        Dim wrkbk As Object = Nothing
        Dim MyWorkSheet As Object = Nothing

        Dim ccon As ColorConverter

        Dim fileExtension As String

        Dim fnum As Integer

        Dim inputline As String = ""

        Try


            If File.Exists(signalListFileName) = False Then
                HandleUserMessageLogging("GMRC", signalListFileName & " cannot be found. Exiting", DisplayMsgBox,)
                Exit Sub
            End If

            ccon = New ColorConverter()

            fileExtension = Path.GetExtension(signalListFileName)

            If fileExtension = ".xlsx" Then
                excelApp = CreateObject("Excel.Application")
            End If

            'Must allow for additional data to have been added so we add 'NumSignalsAdded' to the current upper bound
            'of the exceldata array which was read in at startup. NumSignalsAdded is incremented whenever we add a new signal via
            'a new grid

            '0 based array vs 1 based array, here we copy everything from the 1 based exceldata array to the 0 based exceldataforsave array.
            'exceldata is 1 based because it is derived from exceldata = MyWorkSheet.UsedRange.Value, which is Excel way of doing things,
            'in vb.net, any array that we create  must be a 0 based array...

            If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") And ValidExcelData IsNot Nothing Then

                ReDim exceldataforsave((UBound(ValidExcelData, 1) - _numInvalid) + NumSignalsAdded, UBound(exceldata, 2))

                For x = 0 To UBound(exceldataforsave, 1)

                    If Len(ValidExcelData(x, 0)) > 0 Then
                        For i = 0 To EXCEL_DATA.DisplayFormat - 1
                            exceldataforsave(x, i) = ValidExcelData(x, i)
                        Next i
                    Else
                        exceldataforsave(x, i) = Nothing
                        HandleUserMessageLogging("GMRC", "WriteSignalListFile: ValidExcelData " & x & " = ***" & ValidExcelData(x, 0) & "***")
                    End If

                Next x

            Else
                ReDim exceldataforsave(UBound(exceldata, 1) + NumSignalsAdded, UBound(exceldata, 2))

                For x = 1 To UBound(exceldata, 1)

                    For i = 1 To EXCEL_DATA.DisplayFormat
                        exceldataforsave(x - 1, i - 1) = exceldata(x, i)
                    Next i

                Next x
            End If

            'now go thru all datagrids and populate the exceldataforsave array with the data associated with each datagrid

            i = 1

            For x = 0 To myDGs.Count - 1

                For n = 0 To MyDFs.Count - 1
                    If MyDFs(n).Name = myDGs(x).Parent.Name Then
                        Exit For
                    End If
                Next n

                If n < MyDFs.Count Then

                    exceldataforsave(i, EXCEL_DATA.DisplayWindowSize - 1) = MyDFs(n).DisplayWindowSize
                    exceldataforsave(i, EXCEL_DATA.AlsoAssociatedWith - 1) = MyDFs(n).AlsoAssociatedWith
                    exceldataforsave(i, EXCEL_DATA.LocationOnForm - 1) = myDGs(x).LocationOnForm
                    exceldataforsave(i, EXCEL_DATA.GridSize - 1) = myDGs(x).GridSize

                    For y = 1 To myDGs(x).RowCount
                        For z = 1 To myDGs(x).ColumnCount - 1

                            exceldataforsave(i, EXCEL_DATA.DisplayWindowName - 1) = MyDFs(n).Name
                            exceldataforsave(i, EXCEL_DATA.AssociatedControlName - 1) = myDGs(x).Name

                            exceldataforsave(i, EXCEL_DATA.DisplayName - 1) = myDGs(x).DisplayName(y, 1)

                            If Len(myDGs(x).VariableName(y, z)) > 0 Then
                                exceldataforsave(i, EXCEL_DATA.VariableName - 1) = myDGs(x).VariableName(y, z)
                            Else
                                exceldataforsave(i, EXCEL_DATA.VariableName - 1) = "undefined"
                            End If

                            If Len(myDGs(x).DeviceName(y, z)) > 0 Then
                                exceldataforsave(i, EXCEL_DATA.DeviceName - 1) = myDGs(x).DeviceName(y, z)
                            Else
                                exceldataforsave(i, EXCEL_DATA.DeviceName - 1) = "undefined"
                            End If

                            If Len(myDGs(x).Raster(y, z)) > 0 Then
                                exceldataforsave(i, EXCEL_DATA.Raster - 1) = myDGs(x).Raster(y, z)
                            Else
                                exceldataforsave(i, EXCEL_DATA.Raster - 1) = "undefined"
                            End If

                            exceldataforsave(i, EXCEL_DATA.DefaultBackColor - 1) = ccon.ConvertToString(myDGs(x).DefaultCellBackColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.DefaultForeColor - 1) = ccon.ConvertToString(myDGs(x).DefaultCellForeColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.HighThreshBackColor - 1) = ccon.ConvertToString(myDGs(x).HighThreshBackColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.LowThreshBackColor - 1) = ccon.ConvertToString(myDGs(x).LowThreshBackColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.HighThreshForeColor - 1) = ccon.ConvertToString(myDGs(x).HighThreshForeColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.LowThreshForeColor - 1) = ccon.ConvertToString(myDGs(x).LowThreshForeColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.HighThresh - 1) = myDGs(x).HighThresh(y, z)
                            exceldataforsave(i, EXCEL_DATA.LowThresh - 1) = myDGs(x).LowThresh(y, z)
                            exceldataforsave(i, EXCEL_DATA.EqualTo - 1) = myDGs(x).EqualTo(y, z)
                            exceldataforsave(i, EXCEL_DATA.CheckForDataChange - 1) = IIf(Len(myDGs(x).CheckForDataChange(y, z)) > 0, UCase(myDGs(x).CheckForDataChange(y, z).ToString), "FALSE")
                            exceldataforsave(i, EXCEL_DATA.Row - 1) = y
                            exceldataforsave(i, EXCEL_DATA.Col - 1) = z
                            exceldataforsave(i, EXCEL_DATA.AlsoAssociatedWith - 1) = myDGs(x).AlsoAssociatedWith(y, z)
                            exceldataforsave(i, EXCEL_DATA.DisplayFormat - 1) = IIf(Len(myDGs(x).DisplayFormat(y, z)) > 0, myDGs(x).DisplayFormat(y, z), """0.000""")

                            i += 1
                        Next z
                    Next y

                End If

            Next x

            'Here we must take care of all of the signals remaining in the file after the display signals
            'we must also handle if there have been new "invisible" signals added.  This code below does
            'not currently work correctly if we have initialized as DISPLAYS and we add more record only
            'or invisible signals.  This is because we are stepping on all invisible signals with the
            'newly added record only signals...

            If NumSignalsAdded > 0 Then

                y = 0

                If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") Then

                    For x = UBound(ValidExcelData, 1) - _numInvalid To (UBound(exceldataforsave, 1) - 1)

                        exceldataforsave(x, EXCEL_DATA.VariableName - 1) = MyIncaInterface.myAddedSignals(y).SignalName
                        exceldataforsave(x, EXCEL_DATA.DeviceName - 1) = MyIncaInterface.myAddedSignals(y).DeviceName
                        exceldataforsave(x, EXCEL_DATA.Raster - 1) = MyIncaInterface.myAddedSignals(y).RasterName
                        y += 1

                    Next
                Else
                    For x = UBound(exceldata, 1) To UBound(exceldataforsave, 1) - 1

                        exceldataforsave(x, EXCEL_DATA.VariableName - 1) = MyIncaInterface.myAddedSignals(y).SignalName
                        exceldataforsave(x, EXCEL_DATA.DeviceName - 1) = MyIncaInterface.myAddedSignals(y).DeviceName
                        exceldataforsave(x, EXCEL_DATA.Raster - 1) = MyIncaInterface.myAddedSignals(y).RasterName
                        y += 1

                    Next
                End If

            End If

            Dim savefilename As String = ""

            If ConfigureForNewSoftwareVersion = False Then

                SaveFileDialog1.DefaultExt = fileExtension
                SaveFileDialog1.FileName = INCAVariableFile
                SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
                SaveFileDialog1.Filter = Mid(fileExtension, 2, Len(fileExtension)) & " |*" & fileExtension
                SaveFileDialog1.ShowDialog()

                savefilename = SaveFileDialog1.FileName

            Else
                savefilename = Mid(INCAVariableFile, 1, InStr(INCAVariableFile, fileExtension) - 1) & "_NEW" & fileExtension
            End If

            If Len(savefilename) > 0 Then

                If savefilename = INCAVariableFile Then
                    HandleUserMessageLogging("GMRC", "WriteSignalListFile: Updating " & INCAVariableFile)
                Else
                    HandleUserMessageLogging("GMRC", "WriteSignalListFile: Creating New INCA Variable File (" & savefilename & ")")
                    If ConfigureForNewSoftwareVersion = True Then
                        MsgBox("A new signal list file will now be created called " & savefilename & " This signal list has been modified by removing the signals flagged in the InvalidSignalsLog.csv File...")
                        MsgBox("Open this file, verify that it is correct, then rename the file based on the total number of signals removing '_NEW' from the filename.  Save both as .xlsx and .csv file type...")
                    End If
                End If

                If fileExtension = ".xlsx" Then

                    wrkbk = excelApp.Workbooks.Open(INCAVariableFile)
                    MyWorkSheet = wrkbk.Sheets(1)
                    MyWorkSheet.Activate()

                    If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") And Not (ValidExcelData Is Nothing) Then

                        If UBound(exceldataforsave, 1) >= UBound(exceldata, 1) Then

                            MyWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1) + 1, UBound(ValidExcelData, 2) + 1).Value = exceldataforsave

                        Else

                            MyWorkSheet.UsedRange.Clear()
                            MyWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1) + 1, UBound(ValidExcelData, 2) + 1).Value = exceldataforsave

                        End If
                    Else

                        MyWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1), UBound(exceldata, 2)).Value = exceldataforsave
                    End If

                    HandleUserMessageLogging("GMRC", "WriteSignalListFile: Excel File Written successfully")

                    If savefilename = INCAVariableFile Then
                        wrkbk.Save()
                    Else
                        wrkbk.SaveAs(savefilename)

                        If ConfigureForNewSoftwareVersion = False Then
                            If MsgBox("Do you wish to update the configuration file to use this newly saved variable file?", vbYesNo) = vbYes Then
                                INCAVariableFile = savefilename
                            End If
                        End If

                    End If

                    excelApp.Quit()
                    excelApp = Nothing

                Else '.csv file...

                    fnum = FreeFile()

                    FileOpen(fnum, savefilename, OpenMode.Output)

                    For x = 0 To UBound(exceldataforsave, 1) - 1
                        For y = 0 To UBound(exceldataforsave, 2) - 1
                            If y = 0 Then
                                inputline = exceldataforsave(x, y)
                            Else
                                inputline = inputline & "," & exceldataforsave(x, y)
                            End If
                        Next
                        PrintLine(fnum, inputline)
                    Next

                    FileClose(fnum)

                    HandleUserMessageLogging("GMRC", "WriteSignalListFile: Signal List File Written successfully")

                    If savefilename <> INCAVariableFile Then

                        If ConfigureForNewSoftwareVersion = False Then
                            If MsgBox("Do you wish to update the configuration file to use this newly saved variable file?", vbYesNo) = vbYes Then
                                INCAVariableFile = savefilename
                            End If
                        End If

                    End If

                End If

                GridCellPropConfig._changesMade = False
                NumSignalsAdded = 0

            Else
                MsgBox("No Valid Filename selected, changes will not be saved....")
            End If

            'If there is an InvalidSignalsLog.csv file, we will rename it here...
            'This file is used during signal registration to log all of the signals
            'which cannot, for whatever reason, be registered.  The intent is to keep
            'track of unregisterable signals after each initialization attempt and delete
            'such signals from the signal list so subsequent signal registration attempts
            'will not include these signals. Signals are marked for deletion in the
            'ReadInSignalList routine...

            If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") Then
                File.Move(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv", My.Application.Info.DirectoryPath & "\InvalidSignalsLog_" & Format(DateTime.Now, "MMddyyyy_hhmmss") & ".csv")
            End If

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "WriteSignalListFile: " & ex.Message, DisplayMsgBox)

        End Try

    End Sub

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

    Private Sub PositionGridOnForm(ByVal j As Integer)
        ' Called by ReadInSignalList during initialization.
        '
        ' Positions dynamically created grid on its associated form based on
        ' information in the Excel configuration spreadsheet.

        Dim ctr As Integer = -1
        Dim offset As Integer = 0

        ' The name of the parent form
        Dim targetFormName As String = MyDFs(j).Name

        ' First pass: find the initial offset (index in MyDGs) for grids belonging to targetFormName.
        For i As Integer = 0 To myDGs.Count - 1
            If myDGs(i).Parent IsNot Nothing AndAlso myDGs(i).Parent.Name = targetFormName Then
                ctr += 1
                If ctr = 0 Then
                    offset = i
                End If
            End If
        Next

        ' Set the default size of the form
        MyDFs(j).DefaultWidth = MyDFs(j).Width
        MyDFs(j).DefaultHeight = MyDFs(j).Height

        ' Validate that offset + ctr is within bounds
        If offset + ctr >= myDGs.Count Then
            ' Means we found zero or not enough grids for this form, so we can exit
            Exit Sub
        End If

        ' If the grid we found doesn't match the form name, we do a second pass
        ' to adjust offset and ctr.
        If myDGs(offset + ctr).Parent.Name <> targetFormName Then

            Dim i As Integer = offset + ctr
            Do While i < myDGs.Count
                i += 1
                If i < myDGs.Count AndAlso myDGs(i).Parent IsNot Nothing AndAlso
               myDGs(i).Parent.Name = targetFormName Then
                    offset = i
                    ctr = 0
                    Exit Do
                End If
            Loop

        End If

        ' Double-check offset + ctr again after the second pass
        If offset + ctr >= myDGs.Count Then
            Exit Sub
        End If

        ' Now we should have the correct grid for the form
        If myDGs(offset + ctr).Parent IsNot Nothing AndAlso
       myDGs(offset + ctr).Parent.Name = targetFormName Then

            Select Case myDGs(offset + ctr).LocationOnForm
                Case ""
                    HandleUserMessageLogging("GMRC",
                    "PositionGridOnForm: No Grid Location Specified on form " & myDGs(offset + ctr).Name &
                    ". Defaults will be used for initial position.",
                    ,,,,, OnVehicleScreen.Label2
                )
                    myDGs(offset + ctr).Top = GridDataClass.DEFAULT_SEPARATION
                    myDGs(offset + ctr).Left = GridDataClass.DEFAULT_SEPARATION

                Case Else
                    ' Example location string: "X100 Y50"
                    ' Extract X or left from the substring after "X", up to "Y"
                    Dim locationStr As String = myDGs(offset + ctr).LocationOnForm
                    Dim yPosIndex As Integer = InStr(locationStr, "Y")

                    If yPosIndex > 0 Then
                        ' Extract X value from "X..." up to (but not including) "Y"
                        Dim xStr As String = Mid(locationStr, 2, yPosIndex - 2)  ' skip "X"
                        myDGs(offset + ctr).Left = Val(xStr)

                        ' Extract Y value from after "Y" until the end
                        Dim yStr As String = Mid(locationStr, yPosIndex + 1)
                        myDGs(offset + ctr).Top = Val(yStr)
                    Else
                        ' Fallback if format is unexpected
                        myDGs(offset + ctr).Top = GridDataClass.DEFAULT_SEPARATION
                        myDGs(offset + ctr).Left = GridDataClass.DEFAULT_SEPARATION
                    End If

            End Select

        Else
            HandleUserMessageLogging("GMRC", "PositionGridOnForm: What da huh?")
        End If

    End Sub

    Private Function ReadInSignalList(ByVal signalListFileName As String) As Boolean
        ' Reads and processes an INCA signal list from either a .CSV or .XLSX file.
        ' Depending on the SignalRegistrationMode, signals may be flagged for forced registration.
        ' Visible signals (with a DisplayWindowName) are handled first; invisible signals are processed
        ' later. In FULL registration mode, all signals (visible + invisible) are processed.

        ' ✅ CRITICAL: Exit immediately if application is shutting down
        If exitInProgress Then
            HandleUserMessageLogging("GMRC", "ReadInSignalList: Skipping - exit in progress")
            Return True  ' Return success to avoid cascading errors
        End If

        ' ✅ Also check cancellation token
        If _initCts?.IsCancellationRequested Then
            HandleUserMessageLogging("GMRC", "ReadInSignalList: Skipping - cancellation requested")
            Return True
        End If

        Dim invalidSignalsArray() As String = Nothing
        Dim fileExtension As String
        Dim duplicate As Boolean = False
        Dim invalidSignal As Boolean = False
        Dim fileValid As Boolean = False
        Dim cameraNumber As Integer = 0
        Dim timeCodeCounter As Integer = 0
        Dim excelApp As Object = Nothing
        Dim wrkbk As Object = Nothing
        Dim MyWorkSheet As Object = Nothing
        Dim returnStr As String = ""
        Dim tempStr As String = ""
        Dim fnum As Integer = 0
        Dim fnumInvalid As Integer = 0

        Static beenHere As Boolean
        Dim y As Integer = 0

        Try
            fileExtension = Path.GetExtension(signalListFileName)
            ReadInSignalList = True

            ' --------------------------------------------------------------------------
            ' Basic File Checks & Handling
            ' --------------------------------------------------------------------------
            HandleUserMessageLogging("GMRC", "In ReadInSignalList SignalListFileName = " & signalListFileName)
            If Not File.Exists(signalListFileName) OrElse Not signalListFileName.Contains(fileExtension) Then
                If Not HandleMissingSignalListFile(signalListFileName, fileExtension, fileValid) Then
                    ' If user decides to abort or exit, then return immediately
                    Return False
                End If
                ' If the user selects or chooses a different file, the returned name is in INCAVariableFile
                ' or we re-assign signalListFileName. 
                signalListFileName = INCAVariableFile
            End If

            ' Make a backup copy of the signal list file
            HandleUserMessageLogging("GMRC", $"ReadInSignalList: Creating Signal List backup file: {signalListFileName}.SAVE")
            FileCopy(signalListFileName, signalListFileName & ".SAVE")

            ' --------------------------------------------------------------------------
            ' Connect to INCA if necessary
            ' --------------------------------------------------------------------------
            If Not PlaybackMode Then
                If String.IsNullOrWhiteSpace(MyIncaInterface.GetCurrentVersion) Then
                    returnStr = MyIncaInterface.ConnectToInca()
                    If returnStr <> "True" Then
                        HandleUserMessageLogging("GMRC", "ReadInSignalList: ConnecToInca returned - " & returnStr, DisplayMsgBox,)
                        ExitApp()
                    End If
                    If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
                        HandleUserMessageLogging("GMRC", "ReadInSignalList: Getting Available Experiment Names...")
                        AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames
                        HandleUserMessageLogging("GMRC", "ReadInSignalList: Available Experiment Names Retrieved...")
                        'OnVehicleScreen.Refresh()
                    End If
                End If
            End If

            ' --------------------------------------------------------------------------
            ' Clear out old forms and grids if called again
            ' --------------------------------------------------------------------------
            OnVehicleScreen.TopMost = False
            If beenHere Then
                ResetFormsAndGrids()
            End If
            beenHere = True

            ' --------------------------------------------------------------------------
            ' Open the file (.XLSX vs .CSV) and load data into excelData array
            ' --------------------------------------------------------------------------
            HandleUserMessageLogging("GMRC", $"ReadInSignalList: Reading In Signal/Display Configuration File {signalListFileName}...")

            If fileExtension.ToUpper().Equals(".XLSX") Then
                exceldata = ReadSignalListFromXlsx(signalListFileName, excelApp, wrkbk, MyWorkSheet)
            Else
                exceldata = ReadSignalListFromCsv(signalListFileName)
            End If

            ' --------------------------------------------------------------------------
            ' If FULL registration and InvalidSignalsLog.csv exists, prepare ValidExcelData
            ' --------------------------------------------------------------------------
            If InStr(UCase(SignalRegistrationMode), "FULL") > 0 AndAlso File.Exists(Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv")) Then
                HandleUserMessageLogging("GMRC", "ReadInSignalList: FULL Registration InvalidSignalsLog.csv exists...")
                ReDim ValidExcelData(UBound(exceldata, 1) - 1, EXCEL_DATA.DisplayFormat - 1)
                ' Copy headers
                For i As Integer = 1 To EXCEL_DATA.DisplayFormat
                    ValidExcelData(0, i - 1) = exceldata(1, i)
                Next
            End If

            ' Prepare to read signals
            ReDim MyIncaInterface.mySignals(0)
            MyIncaInterface.mySignals(0).DeviceName = ""
            MyIncaInterface.mySignals(0).RasterName = ""
            MyIncaInterface.mySignals(0).SignalName = ""
            MyIncaInterface.mySignals(0).Status = "Invalid"

            ' --------------------------------------------------------------------------
            ' Main loop: Process rows in excelData
            ' --------------------------------------------------------------------------
            For y = 2 To UBound(exceldata, 1)

                Dim windowName As String = Trim(exceldata(y, EXCEL_DATA.DisplayWindowName))
                Dim deviceName As String = Trim(exceldata(y, EXCEL_DATA.DeviceName))
                Dim rasterName As String = Trim(exceldata(y, EXCEL_DATA.Raster))
                Dim variableName As String = Trim(CStr(exceldata(y, EXCEL_DATA.VariableName)))
                ' Normalize in case CSV provided quotes
                variableName = variableName.Trim(""""c)

                ' Clean up any trailing "_f"
                rasterName = CleanUpRasterName(rasterName)

                If Not String.IsNullOrEmpty(windowName) Then
                    ' ------------------------------------------------------------------
                    ' Visible (display) signals
                    ' ------------------------------------------------------------------
                    If y = 2 Then
                        ' First row with a DisplayWindowName -> Create the first form
                        HandleFirstDisplaySignalRow(deviceName, rasterName, variableName)
                    Else
                        ' Subsequent displayed signals
                        If MyInvisibleSignals IsNot Nothing Then
                            ' If we've already seen invisible signals but now see a visible one -> invalid file format
                            HandleUserMessageLogging("GMRC",
                                "ReadInSignalList: Invalid file format, signals with no DisplayWindowName " &
                                "must occur in the spreadsheet after all displayed signals",
                                DisplayMsgBox)
                            Return False
                        End If
                        HandleSubsequentDisplaySignalRow(
                            deviceName,
                            rasterName,
                            variableName,
                            cameraNumber,
                            y
                        )
                    End If

                Else
                    ' ------------------------------------------------------------------
                    ' Invisible (record-only) signals
                    ' ------------------------------------------------------------------
                    If String.IsNullOrEmpty(tempStr) Then
                        ' Mark the first time we see a row with no window name
                        MyIncaInterface.myPreliminaryDisplaySignals = MyIncaInterface.mySignals
                        If Not InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then
                            ' If not in FULL mode, we are done reading signals
                            Exit For
                        End If
                        tempStr = "Processing 'Record Only' Signals..."
                        HandleUserMessageLogging("GMRC", "ReadInSignalList: " & tempStr)
                    End If
                    ' Load invalidSignalsArray if necessary
                    If ProcessingInvalidSignalsLog Then
                        If fnum = 0 Then
                            invalidSignalsArray = LoadInvalidSignalsArray(fnum)
                        End If
                    End If

                    ' Add invisible signal to MyIncaInterface.MySignals (unless invalid/duplicate)
                    If Not HandleInvisibleSignalRow(
                        deviceName,
                        rasterName,
                        variableName,
                        duplicate,
                        invalidSignal,
                        invalidSignalsArray,
                        fnumInvalid,
                        y
                    ) Then
                        ' If file format is invalid or an error occurred, return false
                        Return False
                    End If
                End If

                ' Check if this is a VIDEO_CAMERA_TIMECODE row
                If IsVideoTimecode(variableName) Then
                    timeCodeCounter += 1
                End If

            Next

            ' --------------------------------------------------------------------------
            ' Post-processing: Cameras, GO/NOGO forms, finalize grids, cleanup
            ' --------------------------------------------------------------------------
            ' Existing less-than warning remains; add a greater-than notice
            If timeCodeCounter < NumberOfCamerasInVehicle Then
                HandleUserMessageLogging("GMRC",
                                         "WARNING: Number of Cameras in the signal list (" & timeCodeCounter &
                                         ") is less than the NumberOfCamerasInVehicle (" & NumberOfCamerasInVehicle & ")." &
                                         " Some cameras may not record video and their VIDEO_CAMERA_TIMECODE signals will not be displayed in the INCA Video Status window.",
                                         DisplayMsgBox
                                         )
            ElseIf timeCodeCounter > NumberOfCamerasInVehicle Then
                HandleUserMessageLogging("GMRC",
                                         "NOTICE: Signal list contains " & timeCodeCounter &
                                         " VIDEO_CAMERA_TIMECODE rows but NumberOfCamerasInVehicle is " & NumberOfCamerasInVehicle &
                                         ". Extra VIDEO_CAMERA_TIMECODE rows will be registered, but camera name mapping stops after " &
                                         NumberOfCamerasInVehicle & ".",
                                         DisplayMsgBox
                                         )
            End If

            ' Use VIDEO_CAMERA_TIMECODE-derived camera count to limit ping cycle later
            _effectiveCameraCount = If(timeCodeCounter > 0, timeCodeCounter, 0)

            For z As Integer = 0 To MyDFs.Count - 1
                If MyDFs(z) IsNot Nothing Then
                    If Not String.IsNullOrEmpty(MyDFs(z).AlsoAssociatedWith) AndAlso
                       MyDFs(z).AlsoAssociatedWith.Contains("GO/NOGO") Then
                        _gonogocount += 1
                    End If
                End If

                MyExitButtons(z)?.BringToFront()
            Next

            If myDGs(0) IsNot Nothing Then
                HandleUserMessageLogging("GMRC", " ")
                HandleUserMessageLogging("GMRC", "ReadInSignalList: Finalizing Grid Displays...")
                GridDataClass.FinalizeGridDisplays()
                HandleUserMessageLogging("GMRC", "ReadInSignalList: Grid Displays Finalized...")
                HandleUserMessageLogging("GMRC", " ")
                HandleUserMessageLogging("GMRC", "ReadInSignalList: Auto-sizing forms to content...")
                GridDataClass.AutoSizeFormsToContent()
                HandleUserMessageLogging("GMRC", "ReadInSignalList: Forms auto-sized.")
            End If

            If fileExtension.ToUpper() = ".XLSX" Then
                If excelApp IsNot Nothing Then
                    excelApp.Quit()
                    excelApp = Nothing
                End If
            End If

            HandleUserMessageLogging("GMRC", "ReadInSignalList: SignalList File Processed successfully.")
            OnVehicleScreen.Refresh()
            Return True

        Catch ex As Exception
            ' ✅ Don't show error dialogs during exit
            If Not exitInProgress Then
                HandleUserMessageLogging("GMRC", "ReadInSignalList: " & ex.Message & " - y = " & y, DisplayMsgBox)
            Else
                HandleUserMessageLogging("GMRC", "ReadInSignalList: " & ex.Message & " (during exit, suppressed)")
            End If
            Return False

        Finally
            Cursor = Cursors.Arrow
            If excelApp IsNot Nothing Then
                Try
                    excelApp.Quit()
                Catch
                    ' suppress any cleanup error
                End Try
            End If
        End Try
    End Function

    '========================================================================================================
    ' BELOW ARE HELPER FUNCTIONS
    '========================================================================================================

    ''' <summary>
    ''' Trim whitespace and leading/trailing quotes that may come from CSV export
    ''' </summary>
    Private Shared Function IsVideoTimecode(name As String) As Boolean
        If name Is Nothing Then Return False
        ' Trim whitespace and leading/trailing quotes that may come from CSV export
        Dim s = name.Trim().Trim(""""c)
        Return s.Equals("VIDEO_CAMERA_TIMECODE", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Loads the InvalidSignalsLog.csv into a string array.
    ''' Each element in the array is a line from the CSV file.
    ''' </summary>
    Private Function LoadInvalidSignalsArray(ByRef fnumInvalid As Integer) As String()
        ' Define the path to the InvalidSignalsLog.csv file.
        Dim logPath As String = Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv")
        Dim invalidSignals As New List(Of String)()

        ' Since we're not using file numbers with StreamReader, set fnumInvalid to -1.
        fnumInvalid = -1

        ' Use StreamReader for better file handling.
        Try
            Using reader As New StreamReader(logPath)
                Dim line As String
                While Not reader.EndOfStream
                    line = reader.ReadLine()
                    invalidSignals.Add(line)
                End While
            End Using
        Catch ex As IOException
            ' Handle exceptions such as file not found or access denied.
            ' Log the exception message if needed.
            ' For now, we return an empty array.
            Return New String() {}
        End Try

        ' Convert the list to an array and return.
        Return invalidSignals.ToArray()
    End Function

    ''' <summary>
    ''' Handles adding invisible signals (no DisplayWindowName) to MySignals in FULL registration mode,
    ''' checking for duplicates or invalid entries in InvalidSignalsLog.
    ''' </summary>
    Private Function HandleInvisibleSignalRow(
        ByVal deviceName As String,
        ByVal rasterName As String,
        ByVal variableName As String,
        ByRef duplicate As Boolean,
        ByRef invalidSignal As Boolean,
        ByVal invalidSignalsArray() As String,
        ByRef fnum2 As Integer,
        ByVal y As Integer
    ) As Boolean

        duplicate = False
        invalidSignal = False

        ' If user wants to remove invalid signals from log
        If ProcessingInvalidSignalsLog AndAlso invalidSignalsArray IsNot Nothing Then
            For i As Integer = 0 To UBound(invalidSignalsArray)
                Dim lineSplit = invalidSignalsArray(i).Split(","c)

                ' Make sure lineSplit has at least 2 columns: [signalName, deviceName, ...]
                If lineSplit.Length >= 2 Then
                    If lineSplit(0) = variableName AndAlso lineSplit(1) = deviceName Then
                        invalidSignal = True
                        _numInvalid += 1

                        ' If flagged as duplicate, mark for removal
                        If lineSplit.Length >= 4 AndAlso lineSplit(3).Contains("DUPLICATE") Then
                            invalidSignalsArray(i) = "REMOVED,,,"
                        End If
                        Exit For
                    End If
                End If
            Next
        End If

        ' Check for duplicates in existing signals
        ' For CAN signals, compare rasterName as well
        If Not invalidSignal AndAlso MyIncaInterface.mySignals IsNot Nothing Then
            For x As Integer = 0 To UBound(MyIncaInterface.mySignals)
                If MyIncaInterface.mySignals(x).SignalName = variableName AndAlso
                   MyIncaInterface.mySignals(x).DeviceName = deviceName Then

                    If deviceName.Contains("CAN-Monitoring") Then
                        If MyIncaInterface.mySignals(x).RasterName = rasterName Then
                            duplicate = True
                        End If
                    Else
                        duplicate = True
                    End If
                    If duplicate Then Exit For
                End If
            Next
        End If

        ' If not duplicate or invalid, expand the arrays
        If Not duplicate AndAlso Not invalidSignal Then
            ' Resize MySignals
            ReDim Preserve MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals) + 1)

            ' Resize MyInvisibleSignals
            If MyInvisibleSignals Is Nothing Then
                ReDim MyInvisibleSignals(0)
            Else
                ReDim Preserve MyInvisibleSignals(UBound(MyInvisibleSignals) + 1)
            End If

            ' Populate the new invisible signal (in both arrays)
            Dim newSigIdx As Integer = UBound(MyIncaInterface.mySignals)
            MyInvisibleSignals(UBound(MyInvisibleSignals)).DeviceName = deviceName
            MyInvisibleSignals(UBound(MyInvisibleSignals)).RasterName = rasterName
            MyInvisibleSignals(UBound(MyInvisibleSignals)).SignalName = variableName

            MyIncaInterface.mySignals(newSigIdx).DeviceName = deviceName
            MyIncaInterface.mySignals(newSigIdx).RasterName = rasterName
            MyIncaInterface.mySignals(newSigIdx).SignalName = variableName
            MyIncaInterface.mySignals(newSigIdx).Status = "Invalid"
            MyIncaInterface.mySignals(newSigIdx).ForceRegister = False  ' not displayed, but may still be recorded

        Else
            ' If it's a duplicate or invalid, log it if we are not removing them from invalid signals
            If duplicate OrElse invalidSignal Then
                Dim reason As String = If(duplicate, "DUPLICATE SIGNAL NAME AND DEVICE", "INVALID SIGNAL")
                HandleUserMessageLogging("GMRC",
                    $"ReadInSignalList: {reason} in Signal List File: {variableName}/{deviceName} - {rasterName}")

                If Not ProcessingInvalidSignalsLog Then
                    If fnum2 = 0 Then
                        fnum2 = FreeFile()
                        FileOpen(fnum2, Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv"), OpenMode.Output)
                    Else
                        FileOpen(fnum2, Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv"), OpenMode.Append)
                    End If
                    PrintLine(fnum2, $"{variableName},{deviceName},{rasterName},{reason}")
                    FileClose(fnum2)
                End If
            End If
        End If

        ' Return True to indicate we handled it successfully
        Return True
    End Function

    '========================================================================================================
    ' OTHER HELPER FUNCTIONS
    '========================================================================================================

    ''' <summary>
    ''' Checks for missing or invalid file and prompts the user for next steps. Returns False if user chooses to exit.
    ''' </summary>
    Private Function HandleMissingSignalListFile(
        ByRef signalListFileName As String,
        ByVal fileExtension As String,
        ByRef fileValid As Boolean
    ) As Boolean

        Dim answer As Integer
        Using msgBox As New Cusmsgbox()
            answer = msgBox.DisplayCusMsgBox(
                OnVehicleScreen,
                "Signal List referenced in the configuration file cannot be found or is invalid.",
                "User Input Required",
                "Exit CLEVIR",
                "Use Latest Signal List and Experiment",
                "Select Signal List and Experiment",
                ""
            )
        End Using

        HandleUserMessageLogging("GMRC",
                                "ReadInSignalList: User Answered " & answer & " when Signal List referenced was invalid...")

        Select Case answer
            Case 1
                ' Exit
                Return False

            Case 2
                ' Use Latest
                Dim saveDate As Date
                Dim saveFileName As String = ""
                Dim dir As New DirectoryInfo(Path.Combine(My.Application.Info.DirectoryPath, "SignalLists"))
                Dim files As FileInfo() = dir.GetFiles()

                For Each f As FileInfo In files
                    If Not f.Name.Contains("SAVE") AndAlso
                       Not f.Name.Contains("~") AndAlso
                       f.Name.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase) Then

                        If File.GetLastWriteTime(f.FullName) > saveDate Then
                            saveDate = File.GetLastWriteTime(f.FullName)
                            saveFileName = f.FullName
                        End If
                    End If
                Next

                If Not String.IsNullOrEmpty(saveFileName) Then
                    signalListFileName = saveFileName
                    INCAVariableFile = signalListFileName
                    OnVehicleScreen.Label5.Text = INCAVariableFile

                    INCAExperiment = Path.GetFileNameWithoutExtension(INCAVariableFile)
                    InitialINCAExperiment = INCAExperiment

                    HandleUserMessageLogging("GMRC",
                        $"Using Signal List {INCAExperiment}{fileExtension} and Experiment {INCAExperiment}")
                    StatusNotifier.Toast($"Using Signal List {INCAExperiment}{fileExtension} and Experiment {INCAExperiment}", "INCA", 2000)
                Else
                    Return False
                End If

            Case 3
                ' Select Specific
                Do While fileValid = False
                    OpenFileDialog1.InitialDirectory = Path.Combine(My.Application.Info.DirectoryPath, "SignalLists")
                    OpenFileDialog1.DefaultExt = fileExtension
                    OpenFileDialog1.Filter = Mid(fileExtension, 2, Len(fileExtension)) & " |*" & fileExtension
                    OpenFileDialog1.Title = "Please Select a Signal Configuration File"
                    OpenFileDialog1.ShowDialog()

                    If Not String.IsNullOrEmpty(OpenFileDialog1.FileName) AndAlso
                       OpenFileDialog1.FileName.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase) Then

                        fileValid = True
                        signalListFileName = OpenFileDialog1.FileName
                        INCAVariableFile = signalListFileName
                        OnVehicleScreen.Label5.Text = INCAVariableFile

                        SelectExperiment.ListBox1.Items.Clear()
                        For i As Integer = 0 To UBound(AvailableExperimentNames)
                            Dim nameU As String = AvailableExperimentNames(i).ToUpper()
                            If Not nameU.Contains("BLANK EXPERIMENT") AndAlso Not nameU.Contains("EMPTY EXPERIMENT") Then
                                SelectExperiment.ListBox1.Items.Add(AvailableExperimentNames(i))
                            End If
                        Next
                        SelectExperiment.ShowDialog()

                        If SelectExperiment.ListBox1.SelectedIndex > -1 Then
                            INCAExperiment = SelectExperiment.ListBox1.SelectedItem.ToString()
                            InitialINCAExperiment = INCAExperiment
                        Else
                            MsgBox($"No Experiment selected, Using {INCAExperiment}")
                        End If

                        HandleUserMessageLogging("GMRC", $"Using Signal List {signalListFileName} And Experiment {INCAExperiment}")
                        StatusNotifier.Toast($"Using Signal List {signalListFileName} and Experiment {INCAExperiment}", "INCA", 2000)

                    Else
                        Dim res = MsgBox("Invalid signal configuration filename. Please select a .csv or .xlsx file. Try Again?", vbYesNo)
                        If res = vbNo Then
                            Return False
                        End If
                    End If
                Loop
        End Select

        Return True
    End Function

    ''' <summary>
    ''' Resets or clears out the data forms and data grids if function is called again.
    ''' </summary>
    Private Sub ResetFormsAndGrids()
        ' Clear MyDGs list
        For z As Integer = 0 To myDGs.Count - 1
            myDGs(z) = Nothing
        Next

        ' Clear MyDFs list
        For x As Integer = 0 To MyDFs.Count - 1
            MyDFs(x) = Nothing
        Next
    End Sub

    ''' <summary>
    ''' Reads the signal list from an XLSX file using late binding (COM).
    ''' Returns a 2D object array with Excel data.
    ''' </summary>
    Private Function ReadSignalListFromXlsx(
        ByVal signalListFileName As String,
        ByRef excelApp As Object,
        ByRef wrkbk As Object,
        ByRef MyWorkSheet As Object
    ) As Object(,)

        HandleUserMessageLogging("GMRC", "ReadInSignalList: Creating Excel Object...")
        excelApp = CreateObject("Excel.Application")
        HandleUserMessageLogging("GMRC", "ReadInSignalList: Excel Object created.")

        wrkbk = excelApp.Workbooks.Open(signalListFileName)
        MyWorkSheet = wrkbk.Sheets(1)
        MyWorkSheet.Activate()

        Dim data = MyWorkSheet.UsedRange.Value
        Return data
    End Function

    ''' <summary>
    ''' Reads the signal list from a CSV file and returns a 2D object array analogous to reading an Excel range.
    ''' Uses modern .NET 4.8 methods for better performance and error handling.
    ''' </summary>
    Private Function ReadSignalListFromCsv(ByVal signalListFileName As String) As Object(,)
        HandleUserMessageLogging("GMRC", $"ReadInSignalList: Opening {signalListFileName} file...")

        Try
            ' 1) Detect delimiter from the first non-empty line (supports comma, tab, semicolon)
            Dim firstNonEmpty As String = File.ReadLines(signalListFileName).FirstOrDefault(Function(l) Not String.IsNullOrWhiteSpace(l))
            If firstNonEmpty Is Nothing Then Throw New InvalidDataException("CSV file is empty")

            Dim delim As String = ","
            Dim tabCount = firstNonEmpty.Count(Function(ch) ch = vbTab(0))
            Dim commaCount = firstNonEmpty.Count(Function(ch) ch = ","c)
            Dim semiCount = firstNonEmpty.Count(Function(ch) ch = ";"c)
            If tabCount >= commaCount AndAlso tabCount >= semiCount Then
                delim = vbTab
            ElseIf semiCount >= commaCount AndAlso semiCount >= tabCount Then
                delim = ";"
            Else
                delim = ","
            End If

            ' 2) Parse with TextFieldParser (handles quotes and embedded delimiters correctly)
            Dim rows As New List(Of String())()
            Dim maxColumns As Integer = 0

            Using parser As New Microsoft.VisualBasic.FileIO.TextFieldParser(signalListFileName)
                parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited
                parser.SetDelimiters(delim)
                parser.HasFieldsEnclosedInQuotes = True
                parser.TrimWhiteSpace = False

                While Not parser.EndOfData
                    Dim fields As String() = parser.ReadFields()
                    If fields Is Nothing Then Continue While

                    ' Normalize whitespace; preserve inner spaces, trim around the field
                    For i As Integer = 0 To fields.Length - 1
                        If fields(i) IsNot Nothing Then fields(i) = fields(i).Trim()
                    Next

                    ' Skip fully empty lines
                    If fields.All(Function(f) String.IsNullOrEmpty(f)) Then Continue While

                    rows.Add(fields)
                    If fields.Length > maxColumns Then maxColumns = fields.Length
                End While
            End Using

            If rows.Count = 0 Then Throw New InvalidDataException("CSV contained no data rows")

            ' 3) Ensure at least expected columns (23) to match EXCEL_DATA usage
            maxColumns = Math.Max(maxColumns, 23)

            ' 4) Build a 1-based 2D array (Excel-like indexing)
            Dim excelData(rows.Count, maxColumns) As Object
            For r As Integer = 0 To rows.Count - 1
                Dim fields = rows(r)
                For c As Integer = 0 To maxColumns - 1
                    If c < fields.Length Then
                        excelData(r + 1, c + 1) = fields(c)
                    Else
                        excelData(r + 1, c + 1) = String.Empty
                    End If
                Next
            Next

            HandleUserMessageLogging("GMRC", $"Successfully loaded {rows.Count} rows with {maxColumns} columns from CSV file")
            Return excelData

        Catch ex As FileNotFoundException
            HandleUserMessageLogging("GMRC", $"ReadSignalListFromCsv: File not found - {signalListFileName}")
            Throw New FileNotFoundException($"Signal list file not found: {signalListFileName}", ex)
        Catch ex As UnauthorizedAccessException
            HandleUserMessageLogging("GMRC", $"ReadSignalListFromCsv: Access denied - {signalListFileName}")
            Throw New UnauthorizedAccessException($"Access denied to signal list file: {signalListFileName}", ex)
        Catch ex As IOException
            HandleUserMessageLogging("GMRC", $"ReadSignalListFromCsv: IO error reading file - {ex.Message}")
            Throw New IOException($"Error reading signal list file: {signalListFileName}", ex)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ReadSignalListFromCsv: Unexpected error - {ex.Message}")
            Throw New InvalidOperationException($"Failed to read signal list from CSV: {signalListFileName}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Cleans up a raster name that might end in "_f" by converting it to "-f".
    ''' </summary>
    Private Function CleanUpRasterName(ByVal raster As String) As String
        If raster.EndsWith("_f", StringComparison.OrdinalIgnoreCase) Then
            raster = raster.Substring(0, raster.Length - 2) & "-f"
        End If
        Return raster
    End Function

    ''' <summary>
    ''' Handles the very first displayed signal row, which triggers form creation and grid initialization.
    ''' </summary>
    Private Sub HandleFirstDisplaySignalRow(
        ByVal deviceName As String,
        ByVal rasterName As String,
        ByVal variableName As String
    )
        ' Because this is the very first row, we initialize MySignals(0) with the row’s data
        MyIncaInterface.mySignals(0).DeviceName = deviceName
        MyIncaInterface.mySignals(0).RasterName = rasterName
        MyIncaInterface.mySignals(0).SignalName = variableName
        MyIncaInterface.mySignals(0).Status = "Invalid"

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "ReadInSignalList: Creating Forms...")

        CreateNewForm(0,
                      exceldata(2, EXCEL_DATA.DisplayWindowName),
                      exceldata(2, EXCEL_DATA.AlsoAssociatedWith),
                      exceldata(2, EXCEL_DATA.DisplayWindowSize),
                      -5000)

        GridDataClass.InitializeFlexGrids(2, 0, 0)
        GridDataClass.FormatFlexGrids(2, 0, 0)
        PositionGridOnForm(0)

        If ShouldForceRegister(0, 1, 1, UBound(MyIncaInterface.mySignals)) Then
            MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).ForceRegister = True
            myDGs(0).Registered(1, 1) = True
            _progressBarMax += 1
        End If
    End Sub

    ''' <summary>
    ''' Handles subsequent display-signal rows, which may require creating a new form or a new grid, 
    ''' or reusing existing ones. Also handles VIDEO_CAMERA_TIMECODE camera logic.
    ''' </summary>
    Private Sub HandleSubsequentDisplaySignalRow(
    ByVal deviceName As String,
    ByVal rasterName As String,
    ByVal variableName As String,
    ByRef cameraNumber As Integer,
    ByVal rowIndex As Integer
)

        ' Expand MySignals array by 1
        ReDim Preserve MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals) + 1)
        Dim newSigIndex = UBound(MyIncaInterface.mySignals)

        ' If the variable is VIDEO_CAMERA_TIMECODE, adjust device name to camera name (bounded)
        If IsVideoTimecode(variableName) Then
            Dim mappedName As String = deviceName
            If NumberOfCamerasInVehicle > 0 AndAlso cameraNumber < NumberOfCamerasInVehicle Then
                ' Also ensure CameraNames has an entry for this index if available
                If CameraNames IsNot Nothing AndAlso cameraNumber >= 0 AndAlso cameraNumber < CameraNames.Length Then
                    mappedName = CameraNames(cameraNumber)
                End If
                cameraNumber += 1
            End If
            MyIncaInterface.mySignals(newSigIndex).DeviceName = mappedName
            exceldata(rowIndex, EXCEL_DATA.DisplayName) = mappedName
        Else
            MyIncaInterface.mySignals(newSigIndex).DeviceName = deviceName
        End If

        MyIncaInterface.mySignals(newSigIndex).RasterName = rasterName
        MyIncaInterface.mySignals(newSigIndex).SignalName = variableName
        MyIncaInterface.mySignals(newSigIndex).Status = "Invalid"

        ' Create or find form
        Dim displayWindowName As String = CStr(exceldata(rowIndex, EXCEL_DATA.DisplayWindowName))
        Dim alsoAssocWith As String = CStr(exceldata(rowIndex, EXCEL_DATA.AlsoAssociatedWith))
        Dim displayWindowSize As String = CStr(exceldata(rowIndex, EXCEL_DATA.DisplayWindowSize))

        EnsureFormExists(displayWindowName, alsoAssocWith, displayWindowSize)

        ' Ensure a grid is created or updated with the row’s data
        Dim gridName As String = CStr(exceldata(rowIndex, EXCEL_DATA.AssociatedControlName))
        UpdateOrCreateGrid(displayWindowName, gridName, rowIndex, newSigIndex)
    End Sub

    ''' <summary>
    ''' Ensures the form with displayWindowName exists, creating it if not found.
    ''' Also merges the AlsoAssociatedWith info if necessary.
    ''' </summary>
    Private Sub EnsureFormExists(
                                 ByVal displayWindowName As String,
                                 ByVal alsoAssocWith As String,
                                 ByVal displayWindowSize As String
                                 )
        Dim found As Boolean = False

        For z As Integer = 0 To MyDFs.Count - 1
            If MyDFs(z) IsNot Nothing AndAlso MyDFs(z).Name = displayWindowName Then
                found = True
                ' If alsoAssocWith contains specific keywords, update AlsoAssociatedWith
                If alsoAssocWith.Contains("GO/NOGO") OrElse
                   alsoAssocWith.Contains("CS_") OrElse
                   alsoAssocWith.Contains("AUTOANNO") Then

                    If MyDFs(z).AlsoAssociatedWith IsNot Nothing AndAlso
                       Not MyDFs(z).AlsoAssociatedWith.Contains("GO/NOGO") Then
                        MyDFs(z).AlsoAssociatedWith = alsoAssocWith
                    End If
                End If
                Exit For
            End If
        Next

        If Not found Then
            Dim formIndex As Integer = GetNextAvailableFormIndex()
            HandleUserMessageLogging("GMRC", "ReadInSignalList: Creating " & displayWindowName & " form...")
            OnVehicleScreen.Refresh()
            CreateNewForm(formIndex, displayWindowName, alsoAssocWith, displayWindowSize, -5000)
        End If
    End Sub

    ''' <summary>
    ''' Locates the next available form index in MyDFs array by scanning for Nothing entries.
    ''' </summary>
    Private Function GetNextAvailableFormIndex() As Integer
        ' Check if the list is empty
        If MyDFs.Count = 0 Then
            Return 0
        End If

        ' Find the first available index
        For i As Integer = 0 To MyDFs.Count - 1
            If MyDFs(i) Is Nothing Then
                Return i
            End If
        Next

        ' All entries are occupied; return the next index
        Return MyDFs.Count
    End Function

    ''' <summary>
    ''' Either updates an existing grid or creates a new one for the specified form.
    ''' </summary>
    Private Sub UpdateOrCreateGrid(
    ByVal displayWindowName As String,
    ByVal gridName As String,
    ByVal rowIndex As Integer,
    ByVal signalIndex As Integer
)
        ' Extract the desired row/col from exceldata
        Dim rowVal As Integer = CInt(exceldata(rowIndex, EXCEL_DATA.Row))
        Dim colVal As Integer = CInt(exceldata(rowIndex, EXCEL_DATA.Col))

        Dim found As Boolean = False
        Dim gridIndex As Integer = -1

        ' Attempt to find existing grid by looping through MyDGs (a List(Of GridDataClass))
        For i As Integer = 0 To myDGs.Count - 1
            If myDGs(i) IsNot Nothing AndAlso
           myDGs(i).Name = gridName AndAlso
           myDGs(i).ParentFormName = displayWindowName Then

                found = True
                gridIndex = i

                ' Expand row/col if needed
                If myDGs(i).NumberOfRows <= rowVal Then myDGs(i).NumberOfRows = rowVal + 1
                If myDGs(i).NumberOfColumns <= colVal Then myDGs(i).NumberOfColumns = colVal + 1

                ' Format the grid
                GridDataClass.FormatFlexGrids(rowIndex, i, signalIndex)
                Exit For
            End If
        Next

        ' If no existing grid was found, create a new one
        If Not found Then
            Dim formIndex As Integer = FindFormIndex(displayWindowName)

            ' Instead of "GetNextAvailableGridIndex()", we can just set the new gridIndex to the end
            ' of the list. Will consider a certain position that can be set here later.
            gridIndex = myDGs.Count

            ' Create the new grid by calling InitializeFlexGrids
            ' (still passing 'gridIndex' for backward compatibility)
            GridDataClass.InitializeFlexGrids(rowIndex, formIndex, gridIndex)

            ' Format the newly created grid
            GridDataClass.FormatFlexGrids(rowIndex, gridIndex, signalIndex)

            ' Position the grid on the form
            PositionGridOnForm(formIndex)
        End If

        ' Force registration if needed
        If ShouldForceRegister(gridIndex, rowVal, colVal, signalIndex) Then
            MyIncaInterface.mySignals(signalIndex).ForceRegister = True
            myDGs(gridIndex).Registered(rowVal, colVal) = True
            _progressBarMax += 1
        Else
            myDGs(gridIndex).Registered(rowVal, colVal) = False
        End If
    End Sub
    ''' <summary>
    ''' Finds the form index in MyDFs array that matches displayWindowName.
    ''' </summary>
    Private Function FindFormIndex(ByVal displayWindowName As String) As Integer
        For i As Integer = 0 To MyDFs.Count - 1
            If MyDFs(i) IsNot Nothing AndAlso MyDFs(i).Name = displayWindowName Then
                Return i
            End If
        Next
        Return 0
    End Function

    ''' <summary>
    ''' Returns the next index in MyDGs that is Nothing (i.e., not in use).
    ''' </summary>
    Private Function GetNextAvailableGridIndex() As Integer
        For i As Integer = 0 To myDGs.Count - 1
            If myDGs(i) Is Nothing Then
                Return i
            End If
        Next
        Return 0
    End Function

    ''' <summary>
    ''' Checks whether we should force-register the signal (based on SignalRegistrationMode, etc).
    ''' </summary>
    Private Function ShouldForceRegister(
        ByVal gridIndex As Integer,
        ByVal rowVal As Integer,
        ByVal colVal As Integer,
        ByVal signalIndex As Integer
    ) As Boolean

        Dim assoc As String = myDGs(gridIndex).AlsoAssociatedWith(rowVal, colVal)
        If assoc Is Nothing Then assoc = ""

        ' "GO/NOGO" or "AUTOANNO" or "CS_" in AlsoAssociatedWith => Force registration in GO/NOGO mode
        If (assoc.Contains("GO/NOGO") OrElse assoc.Contains("AUTOANNO") OrElse assoc.Contains("CS_")) _
            AndAlso UCase(SignalRegistrationMode) = "GO/NOGO" Then
            Return True
        End If

        ' DISPLAYS => force registration
        If UCase(SignalRegistrationMode) = "DISPLAYS" Then
            Return True
        End If

        ' FULL => force registration
        If InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then
            Return True
        End If

        Return False
    End Function

    Private Sub MyExitButtons_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'This routine is responsible for handling the Exit buttons on each of the
        'display screens that are configured from the excel spreadsheet INCAVariablefile

        'All it does, is close the form on which the button resides...

        'Whenever a new form is created from the configuration information with CreateNewForm,         
        'an exit button is added dynamically and placed on each form.  A "handler" is created
        'in code for each button so that it will allow the user to exit the display screen.

        'The handler is set up in CreateNewForm using the following construct...

        'This means, add a handler routine for the MyExitButtons(index).click event, and call it MyExitButtons_Click
        'this is where the code will reside to handle the click event of the exit buttons that are dynamically created
        'when creating each display form.

        'AddHandler MyExitButtons(index).Click, AddressOf MyExitButtons_Click

        sender.parent.close()

    End Sub

    Private Sub Mylist_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'This is a handler for the dynamically created Mylist, which is created in the InitializationMonitor
        'thread.  This list is used during initialization to display initialization status information
        'The list will remain on the screen after initialization is complete if there are any initialization
        'errors, otherwise it will automatically disappear.

        'If it does not disppear, then clicking in it will set the KeepListDisplayed flag to false and the
        'listbox will disappear.

        'This allows the user, if there is important information in the list, to read it prior to entering
        'the main display window.

        _keepListBoxDisplayed = False

    End Sub
    Private Sub CreateNewForm(
       ByVal index As Integer,
       ByVal displayWindowName As String,
       ByVal alsoAssociatedWith As String,
       ByVal displayWindowSize As String,
       ByVal formLocation As Integer)

        ' Ensure the lists have enough capacity
        While MyDFs.Count <= index
            MyDFs.Add(Nothing)
        End While
        While MyExitButtons.Count <= index
            MyExitButtons.Add(Nothing)
        End While

        ' Ensure the lists have enough capacity
        While MyDFs.Count <= index
            MyDFs.Add(Nothing)
        End While
        While MyExitButtons.Count <= index
            MyExitButtons.Add(Nothing)
        End While

        ' Create the new form
        MyDFs(index) = New FormDataClass With {
           .Text = displayWindowName,
           .Name = displayWindowName,
           .ShowInTaskbar = False,
           .AlsoAssociatedWith = ""  ' Initialize to empty string
       }

        ' Create the exit button
        MyExitButtons(index) = New Button With {
           .Parent = MyDFs(index),
           .Visible = True,
           .Text = "EXIT"
       }

        ' Set the font for the exit button
        MyExitButtons(index).Font = New Font(MyExitButtons(index).Font.FontFamily, MenuFontSize, FontStyle.Bold)

        ' Add the click event handler
        AddHandler MyExitButtons(index).Click, AddressOf MyExitButtons_Click

        ' Set default display window size if not provided or invalid
        If String.IsNullOrEmpty(displayWindowSize) Then
            displayWindowSize = "W500 H500"
        End If

        ' Parse the width and height from displayWindowSize
        Dim width As Integer = 500
        Dim height As Integer = 500
        Dim sizeParts() As String = displayWindowSize.Split(" "c)
        For Each part As String In sizeParts
            If part.StartsWith("W", StringComparison.OrdinalIgnoreCase) Then
                Integer.TryParse(part.Substring(1), width)
            ElseIf part.StartsWith("H", StringComparison.OrdinalIgnoreCase) Then
                Integer.TryParse(part.Substring(1), height)
            End If
        Next

        ' Set the size of the form
        MyDFs(index).Width = width
        MyDFs(index).Height = height

        ' Set the size and position of the exit button
        With MyExitButtons(index)
            .Width = 100
            .Height = 50
            .Left = MyDFs(index).Width - .Width - 20
            .Top = 5
        End With

        ' Set the AlsoAssociatedWith property if applicable
        If Not String.IsNullOrEmpty(alsoAssociatedWith) AndAlso
          (alsoAssociatedWith.Contains("GO/NOGO") OrElse
           alsoAssociatedWith.Contains("CS_") OrElse
           alsoAssociatedWith.Contains("AUTOANNO")) Then
            MyDFs(index).AlsoAssociatedWith = alsoAssociatedWith
        End If

        ' Set the DisplayWindowSize property
        MyDFs(index).DisplayWindowSize = displayWindowSize

        ' Set the form location
        MyDFs(index).Left = formLocation

        ' Initialize the form's context menu
        InitializeFormContextMenu(MyDFs(index))

        ' Bring the exit button to the front
        MyExitButtons(index)?.BringToFront()

        ' Show the form
        MyDFs(index).Hide()
    End Sub

    Private Sub InitializeFormContextMenu(ByVal MyObject As Object)
        ' Called from CreateNewForm.
        ' Sets up the form context menu using List(Of ToolStripMenuItem).

        ' Initialize a fresh context menu
        MyObject.MyContextMenuStrip = New ContextMenuStrip()

        ' Build a list of ToolStripMenuItems for the "Form Handling" submenu
        Dim formMenuItems As New List(Of ToolStripMenuItem)

        ' 1. "Delete Form"
        Dim deleteFormItem As New ToolStripMenuItem("Delete Form")
        AddHandler deleteFormItem.Click, AddressOf ContextMenu_FormHandling_Click
        formMenuItems.Add(deleteFormItem)

        ' 2. "Create New Grid"
        Dim createGridItem As New ToolStripMenuItem("Create New Grid")
        AddHandler createGridItem.Click, AddressOf ContextMenu_FormHandling_Click
        formMenuItems.Add(createGridItem)

        ' 3. "Set GO/NOGO Display Flag" (CheckOnClick = True)
        Dim goNoGoFlagItem As New ToolStripMenuItem("Set GO/NOGO Display Flag") With {
            .CheckOnClick = True
        }
        ' The handler for this item is different from the first two
        AddHandler goNoGoFlagItem.Click, AddressOf ContextMenu_FormSubMenu_Click
        formMenuItems.Add(goNoGoFlagItem)

        ' Create top-level "Form Handling" menu item
        Dim contextMenuFormHandling As New ToolStripMenuItem("Form Handling") With {
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Font = New Font("Georgia", 10),
            .TextAlign = ContentAlignment.BottomRight
        }

        ' Add the sub-items (Delete Form, Create New Grid, GO/NOGO)
        contextMenuFormHandling.DropDownItems.AddRange(formMenuItems.ToArray())

        ' Finally, add the "Form Handling" menu to the context menu strip
        MyObject.MyContextMenuStrip.Items.Add(contextMenuFormHandling)
    End Sub


    Private Sub CreateMenus(ByVal startFormIdx As Integer, Optional ByVal newForm As Boolean = False)

        'This handles the dynamic creation of the menus at the top of the GmResidentClient and
        'other miscellaneous display items as defined in the INCAVariableFile.
        'It is called after the INCAVariableFile is processed to add all of the
        'menu selections to display each form defined.  

        'Also creates the GO/NOGO banner at the top of the GmResidentClient display and the
        'OnVehicleScreen and buttons which are associated with each "GO/NOGO" grid display.

        'Also creates all of the buttons (and handlers for) on the Displays screen which is accessible from the DISPLAYS button
        'at the top of the main screen.

        'This routine is also called when the user creates a new display form.

        Dim gonogo As Integer

        Dim ctr As Integer = -1
        Dim offset As Integer = 0

        Dim z As Integer

        Dim whatrowamion As Integer

        Dim answer As Integer

        Try

            _statusLabelWidth = Int((790 - ((_gonogocount + 1) * StatusLabelSpacing)) \ _gonogocount)

            If MyToolStripMenuItem Is Nothing Then

                HandleUserMessageLogging("GMRC", "CreateMenus: Creating Displays Menu...")
                'StatusNotifier.Toast("CreateMenus: Creating Displays Menu...", "CLEVIR", 2000)
                'OnVehicleScreen.Refresh()

                _MyMenuStrip = MenuStrip1

                _MyMenuStrip.Parent = Me

                MyLogin = New ToolStripMenuItem("Login", Nothing, Nothing, "Login")

                'MyLogin.Font = New Font(MyLogin.Font.FontFamily, MenuFontSize - 2)
                'MyLogin.Font = New Font(MyLogin.Font, FontStyle.Bold)

                '_MyMenuStrip.Items.Add(MyLogin)
                'AddHandler MyLogin.Click, AddressOf MyLogin_Click

                'If InStr(UCase(CLEVIRFlavor), "DATALOGGING") > 0 Then
                '    MyLogin.Enabled = False
                'End If

                MyToolStripMenuItem = New ToolStripMenuItem("Displays")

                MyToolStripMenuItem.Font = New Font(MyToolStripMenuItem.Font.FontFamily, MenuFontSize - 2)
                MyToolStripMenuItem.Font = New Font(MyToolStripMenuItem.Font, FontStyle.Bold)

                _MyMenuStrip.Items.Add(MyToolStripMenuItem)

                MyToolStripMenuItem.DropDownItems.Add(MyTdGraphicsContainer.Name)
                AddHandler MyToolStripMenuItem.DropDownItems(0).Click, AddressOf MyToolStripMenuItem_Click

                ReDim SelectDisplays.MyDisplaySelectButtons(0)
                SelectDisplays.MyDisplaySelectButtons(0) = New Button

                'CUSTOM SCREENS


                MyToolStripMenuItem.DropDownItems.Add("Secret Squirrel Screen")
                AddHandler MyToolStripMenuItem.DropDownItems(1).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(1)
                SelectDisplays.MyDisplaySelectButtons(1) = New Button

                MyToolStripMenuItem.DropDownItems.Add("LKA Screen")
                AddHandler MyToolStripMenuItem.DropDownItems(2).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(2)
                SelectDisplays.MyDisplaySelectButtons(2) = New Button

                MyToolStripMenuItem.DropDownItems.Add("Pedestrian Status Display")
                AddHandler MyToolStripMenuItem.DropDownItems(3).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(3)
                SelectDisplays.MyDisplaySelectButtons(3) = New Button

                MyToolStripMenuItem.DropDownItems.Add("Fusion Status Display")
                AddHandler MyToolStripMenuItem.DropDownItems(4).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(4)
                SelectDisplays.MyDisplaySelectButtons(4) = New Button

                MyToolStripMenuItem.DropDownItems.Add("CoPilot Status Display")
                AddHandler MyToolStripMenuItem.DropDownItems(5).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(5)
                SelectDisplays.MyDisplaySelectButtons(5) = New Button

                MyToolStripMenuItem.DropDownItems.Add("LMFR Global A Status Display")
                AddHandler MyToolStripMenuItem.DropDownItems(6).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(6)
                SelectDisplays.MyDisplaySelectButtons(6) = New Button

                MyToolStripMenuItem.DropDownItems.Add("LMFR High Content Status Display")
                AddHandler MyToolStripMenuItem.DropDownItems(7).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(7)
                SelectDisplays.MyDisplaySelectButtons(7) = New Button

                MyToolStripMenuItem.DropDownItems.Add("INCA Hardware Status")
                AddHandler MyToolStripMenuItem.DropDownItems(8).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(8)
                SelectDisplays.MyDisplaySelectButtons(8) = New Button

                'Add new custom screens above this...

                _MyUploadData = New ToolStripMenuItem("Upload Data", Nothing, Nothing, "Upload Data")

                _MyUploadData.Font = New Font(_MyUploadData.Font.FontFamily, MenuFontSize - 2)
                _MyUploadData.Font = New Font(_MyUploadData.Font, FontStyle.Bold)

                _MyMenuStrip.Items.Add(_MyUploadData)
                AddHandler _MyUploadData.Click, AddressOf MyUploadData_Click
                'End If


                _MyRecordPlayback = New ToolStripMenuItem("Record/PlayBack", Nothing, Nothing, "Record/PlayBack")

                _MyRecordPlayback.Font = New Font(_MyRecordPlayback.Font.FontFamily, MenuFontSize - 2)
                _MyRecordPlayback.Font = New Font(_MyRecordPlayback.Font, FontStyle.Bold)

                _MyMenuStrip.Items.Add(_MyRecordPlayback)
                AddHandler _MyRecordPlayback.Click, AddressOf MyRecordPlayback_Click

                _MyMiscInfo = New ToolStripMenuItem("Misc Info", Nothing, Nothing, "Misc Info")

                _MyMiscInfo.Font = New Font(_MyMiscInfo.Font.FontFamily, MenuFontSize - 2)
                _MyMiscInfo.Font = New Font(_MyMiscInfo.Font, FontStyle.Bold)

                _MyMenuStrip.Items.Add(_MyMiscInfo)

                'MyMiscInfo.DropDownItems.Add("Display Operation History")
                'AddHandler MyMiscInfo.DropDownItems(0).Click, AddressOf MyMiscInfo_Click

                _MyMiscInfo.DropDownItems.Add("Display Record Path/Filename")
                AddHandler _MyMiscInfo.DropDownItems(0).Click, AddressOf MyMiscInfo_Click

                _MyMiscInfo.DropDownItems.Add("Display Update Rates")
                AddHandler _MyMiscInfo.DropDownItems(1).Click, AddressOf MyMiscInfo_Click

            Else

                MyToolStripMenuItem.DropDownItems.RemoveAt(MyToolStripMenuItem.DropDownItems.Count - 1)
                _MyCreateNewDisplayMenuItem = Nothing

            End If

            HandleUserMessageLogging("GMRC", "CreateMenus: Adding item(s) to Displays Menu...")
            'StatusNotifier.Toast("CreateMenus: Adding item(s) to Displays Menu...", "CLEVIR", 2000)
            'OnVehicleScreen.Refresh()

            If MyLabel Is Nothing Then
                gonogo = startFormIdx
            Else
                gonogo = UBound(MyLabel) + 1
            End If

            If MyDFs(0) IsNot Nothing Then

                For z = startFormIdx To MyDFs.Count - 1

                    MyToolStripMenuItem.DropDownItems.Add((MyDFs(z).Name))

                    If InStr(MyDFs(z).AlsoAssociatedWith, "GO/NOGO") > 0 Then

                        ReDim Preserve MyLabel(gonogo)

                        MyDFs(z).GoNoGoIndex = gonogo

                        MyLabel(gonogo) = New Label With {
                            .Parent = OnVehicleScreen.GroupBox4,
                            .AutoSize = False,
                            .Visible = True,
                            .BackColor = Color.Green,
                            .ForeColor = Color.White,
                            .BorderStyle = BorderStyle.Fixed3D,
                            .TabStop = False,
                            .Width = _statusLabelWidth,
                            .Height = StatusLabelHeight
                            }

                        OnVehicleScreen.GroupBox4.ResumeLayout(False)

                        MyLabel(gonogo).Top = MyLabel(gonogo).Parent.Height - MyLabel(gonogo).Height - 5

                        MyLabel(gonogo).TextAlign = ContentAlignment.MiddleCenter

                        MyLabel(gonogo).Font = New Font(MyLabel(gonogo).Font.FontFamily, DefaultFontSize - 2)

                        If InStr(MyDFs(z).Name, "CS_") = 0 Then
                            MyLabel(gonogo).Text = MyDFs(z).Name
                        Else
                            MyLabel(gonogo).Text = Mid(MyDFs(z).Name, 4, Len(MyDFs(z).Name))
                        End If

                        AddHandler MyLabel(gonogo).Click, AddressOf MyLabel_Click

                        If gonogo = 0 Then
                            MyLabel(gonogo).Left = StatusLabelSpacing
                        Else
                            MyLabel(gonogo).Left = MyLabel(gonogo - 1).Left + MyLabel(gonogo - 1).Width + StatusLabelSpacing
                        End If
                        gonogo += 1
                    Else
                        MyDFs(z).GoNoGoIndex = -1
                    End If

                    'CUSTOM SCREENS,need to add 1 to NumPredefinedDisplays, every time we add a new custom screen

                    AddHandler MyToolStripMenuItem.DropDownItems(z + NumPredefinedDisplays).Click, AddressOf MyToolStripMenuItem_Click

                    ReDim Preserve SelectDisplays.MyDisplaySelectButtons(z + NumPredefinedDisplays)
                    SelectDisplays.MyDisplaySelectButtons(z + NumPredefinedDisplays) = New Button

                    If newForm = False Then
                        MyDFs(z).Hide()
                    End If

                Next z

            End If

            whatrowamion = -1

            For z = 0 To UBound(SelectDisplays.MyDisplaySelectButtons)

                SelectDisplays.MyDisplaySelectButtons(z).Parent = SelectDisplays

                SelectDisplays.MyDisplaySelectButtons(z).Visible = True
                SelectDisplays.MyDisplaySelectButtons(z).Height = 70
                SelectDisplays.MyDisplaySelectButtons(z).Width = 100
                SelectDisplays.MyDisplaySelectButtons(z).Text = MyToolStripMenuItem.DropDownItems(z).Text
                SelectDisplays.MyDisplaySelectButtons(z).Font = New Font(SelectDisplays.MyDisplaySelectButtons(z).Font.FontFamily, DefaultFontSize)
                SelectDisplays.MyDisplaySelectButtons(z).Font = New Font(SelectDisplays.MyDisplaySelectButtons(z).Font, FontStyle.Bold)

                answer = z Mod 7

                If answer = 0 Then
                    whatrowamion += 1
                End If

                If whatrowamion = 0 Then
                    SelectDisplays.MyDisplaySelectButtons(z).Top = 50
                Else
                    SelectDisplays.MyDisplaySelectButtons(z).Top = SelectDisplays.MyDisplaySelectButtons(0).Top + ((SelectDisplays.MyDisplaySelectButtons(0).Height + 5) * whatrowamion)
                End If

                If z Mod 7 <> 0 Then
                    SelectDisplays.MyDisplaySelectButtons(z).Left = SelectDisplays.MyDisplaySelectButtons(z - 1).Left + SelectDisplays.MyDisplaySelectButtons(z - 1).Width + 10
                Else
                    SelectDisplays.MyDisplaySelectButtons(z).Left = 10
                End If

                AddHandler SelectDisplays.MyDisplaySelectButtons(z).Click, AddressOf SelectDisplays.myDisplaySelectButtons_Click

            Next

            _MyCreateNewDisplayMenuItem = New ToolStripMenuItem With {
                .Text = "Create New Display - Enter Display Name"
            }

            _MyCreateNewDisplayMenuItem.DropDownItems.Add(New ToolStripTextBox())

            _MyCreateNewDisplayMenuItem.DropDownItems(0).Font = New Font(_MyCreateNewDisplayMenuItem.DropDownItems(0).Font.FontFamily, MenuFontSize)
            _MyCreateNewDisplayMenuItem.DropDownItems(0).Font = New Font(_MyCreateNewDisplayMenuItem.DropDownItems(0).Font, FontStyle.Bold)
            _MyCreateNewDisplayMenuItem.DropDownItems(0).BackColor = Color.LightGray

            _MyCreateNewDisplayMenuItem.DropDownItems(0).AutoSize = False
            _MyCreateNewDisplayMenuItem.DropDownItems(0).Width = 400

            _MyCreateNewDisplayMenuItem.DropDownItems.Add("Click Here after entering Display Name")

            MyToolStripMenuItem.DropDownItems.Add(_MyCreateNewDisplayMenuItem)

            AddHandler _MyCreateNewDisplayMenuItem.DropDownItems(1).Click, AddressOf MyCreateNewDisplayMenuItem_Click

            HandleUserMessageLogging("GMRC", "CreateMenus:Menu Item(s) Created")
            'StatusNotifier.Toast("CreateMenus:Menu Item(s) Created", "CLEVIR", 2000)
            'OnVehicleScreen.Refresh()

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "CreateMenus: " & ex.Message, DisplayMsgBox)

        End Try

    End Sub

    Private Sub MyLogin_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'Login selection from top menu bar on GmResidentClient and Login button on OnVehicleScreen
        ' - displays the login form

        OnLoginScreen = True

        If SelectDisplays.Visible = True Then
            SelectDisplays.Close()
        End If

        LoginForm.Show()

    End Sub

    Private Sub MyRecordPlayback_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'Displays the RecordPlayback form, which contains VCR type controls for recording and playing back
        'data through the CLEVIR interface.

        RecordPlayback.Show()
        RecordPlayback.BringToFront()

    End Sub

    Private Sub MyUploadData_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'MyUploadData is dynamically created by CreateMenus as is its handler.  This is one of the
        'menu selections in the Configuration Environment on the GmResidentClient screen.

        'UploadDataScreen.UploadData()

    End Sub

    Private Sub MyCreateNewDisplayMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'MyCreateNewDisplayMenuItem is dynamically created by CreateMenus as is its handler.

        'Creates a new, blank, user defined form and adds it to the menu for display, as well as
        'allowing the user to add a new grid to the newly created form.

        Dim index As Integer
        Dim x As Integer

        If MyIncaInterface.MeasurementStarted = False Then

            If Len(_MyCreateNewDisplayMenuItem.DropDownItems(0).Text) > 0 Then

                For x = 0 To MyDFs.Count - 1
                    If UCase(_MyCreateNewDisplayMenuItem.DropDownItems(0).Text) = UCase(MyDFs(x).Name) Then
                        MsgBox("There is already a form with this name, please use a different form name.")
                        Exit Sub
                    End If
                Next x

                index = MyDFs.Count

                'CreateNewForm(index, MyCreateNewDisplayMenuItem.DropDownItems(0).Text, "", "W400,H400", 0)
                CreateNewForm(index, _MyCreateNewDisplayMenuItem.DropDownItems(0).Text, "", "W400 H400", 0)

                FormForGridAdd = _MyCreateNewDisplayMenuItem.DropDownItems(0).Text

                NewGridCreation.MyGridTitle = InputBox("Enter Grid Name", "USER INPUT", "undefined")

                For x = 0 To myDGs.Count - 1
                    If UCase(myDGs(x).Name) = UCase(NewGridCreation.MyGridTitle) Then
                        MsgBox("There is already a grid with this name, please use a different grid name.")
                        Exit Sub
                    End If
                Next x

                NewGridCreation.Show()

                CreateMenus(MyDFs.Count - 1, True)
            Else
                HandleUserMessageLogging("GMRC", "Invalid Display Name, Exiting...", DisplayMsgBox)
            End If


        Else
            MsgBox("Measurement must be STOPPED prior to adding a new form.")
        End If


    End Sub

    Public Sub CreateNewGrid(ByVal rows As Integer, ByVal cols As Integer)

        'Creates a new, user defined grid and adds it to the user defined display

        Dim j As Integer
        Dim y As Integer
        Dim z As Integer
        Dim x As Integer


        For j = 0 To myDGs.Count - 1
            If UCase(myDGs(j).Name) = UCase(NewGridCreation.GridTitle.Text) Then
                MsgBox("There is already a grid with this name, please use a different grid name.")
                NewGridCreation.GridTitle.Text = ""
                Exit Sub
            End If
        Next j

        Cursor = Cursors.WaitCursor

        For j = 0 To MyDFs.Count - 1
            If MyDFs(j).Name = FormForGridAdd Then
                Exit For
            End If
        Next j

        z = myDGs.Count - 1 + 1

        GridDataClass.InitializeNewGrid(y, j, z)

        For i = 1 To rows
            For x = 1 To cols
                If myDGs(z).NumberOfRows <= i Then
                    myDGs(z).NumberOfRows = i + 1
                End If

                If myDGs(z).NumberOfColumns <= x Then
                    myDGs(z).NumberOfColumns = x + 1
                End If

            Next
        Next

        'MyDGs(z).RowCount = MyDGs(z).Rows
        'MyDGs(z).ColumnCount = MyDGs(z).ColumnCount

        myDGs(myDGs.Count - 1).Name = NewGridCreation.GridTitle.Text
        myDGs(myDGs.Count - 1).Top = GridDataClass.DEFAULT_SEPARATION

        myDGs(myDGs.Count - 1).Width = myDGs(myDGs.Count - 1).ColumnCount * GridDataClass.DEFAULT_COL_WIDTH + GridDataClass.DEFAULT_SEPARATION
        myDGs(myDGs.Count - 1).Height = myDGs(myDGs.Count - 1).RowCount * GridDataClass.DEFAULT_ROW_HEIGHT + GridDataClass.DEFAULT_SEPARATION

        myDGs(myDGs.Count - 1).BringToFront()

        InitializeGridCellProperties(rows, cols, MyDFs(j).Name, myDGs(myDGs.Count - 1))

        GridDataClass.FinalizeGridHeader(myDGs(myDGs.Count - 1))

        GridCellPropConfig._changesMade = True

        Cursor = Cursors.Arrow


    End Sub


    Private Sub ContextMenu_FormHandling_Click(ByVal sender As Object, ByVal e As EventArgs)

        'This handles the context menu on the form.  The context menu is accessed with a right mouse
        'click, then a particular selection is clicked...

        Dim x As Integer
        Dim y As Integer

        Select Case sender.ToString

            Case "Delete Form"

                If MsgBox("Are you sure you want to delete the " & FormForGridAdd & " form?", vbYesNo) = vbYes Then
                    For x = 0 To MyDFs.Count - 1
                        If MyDFs(x).Name = FormForGridAdd Then
                            GridCellPropConfig._changesMade = True
                            MyDFs(x).Hide()
                            MyDFs(x) = Nothing

                            For y = x To MyDFs.Count - 2
                                MyDFs(y) = MyDFs(y + 1)
                            Next y
                            MyDFs.RemoveAt(MyDFs.Count - 1)

                            Exit For
                        End If
                    Next

                    For x = 0 To MyToolStripMenuItem.DropDownItems.Count - 1
                        If MyToolStripMenuItem.DropDownItems.Item(x).Text = FormForGridAdd Then
                            MyToolStripMenuItem.DropDownItems.RemoveAt(x)
                            Exit For
                        End If
                    Next x
                Else
                    MsgBox(FormForGridAdd & " form, will not be deleted.")
                End If


            Case "Create New Grid"

                If MyIncaInterface.MeasurementStarted = False Then

                    NewGridCreation.MyGridTitle = InputBox("Enter Grid Name", "USER INPUT", "undefined")

                    For x = 0 To myDGs.Count - 1
                        If UCase(myDGs(x).Name) = UCase(NewGridCreation.MyGridTitle) Then
                            MsgBox("There is already a grid with this name, please use a different grid name.")
                            Exit Sub
                        End If
                    Next x

                    NewGridCreation.Show()
                Else
                    MsgBox("Measurement must be stopped prior to adding a new grid.")
                    Exit Sub
                End If

        End Select
    End Sub

    Private Sub ContextMenu_FormSubMenu_Click(ByVal sender As Object, ByVal e As EventArgs)

        'This handles the "Set GO/NOGO Display Flag" selection in the form context menu.  This is either checked
        'or unchecked.  Indicates if this form is to be handled as a GO/NOGO display form or a standard display form...

        Dim x As Integer
        Dim y As Integer

        Select Case sender.ToString

            Case "Set GO/NOGO Display Flag"

                If sender.checked = True Then
                    MsgBox("This form will now be handled as a GO/NOGO form" & " - " & sender.name)
                Else
                    MsgBox("This form will NOT be handled as a GO/NOGO form")
                End If

                For x = 0 To MyDFs.Count - 1
                    If FormForGridAdd = MyDFs(x).Name Then
                        MyDFs(x).AlsoAssociatedWith = IIf(sender.checked = True, "GO/NOGO", "")
                        For y = 0 To myDGs.Count - 1
                            If myDGs(y).Parent.Name = MyDFs(x).Name Then
                                myDGs(y).AlsoAssociatedWith(1, 1) = MyDFs(x).AlsoAssociatedWith
                                Exit For
                            End If
                        Next y
                        GridCellPropConfig._changesMade = True
                        Exit For
                    End If
                Next x

            Case "Add New Form to Displays Menu"

                MsgBox("not implemented...")

        End Select

    End Sub

    Private Sub InitializeGridCellProperties(ByVal rows As Integer, ByVal cols As Integer, ByVal formName As String, ByVal sender As GridDataClass)

        'Called from CreateNewGrid, initializes the custom grid properties based on the number
        'of rows and columns defined by the user

        Dim row As Integer
        Dim col As Integer

        Dim ccon As ColorConverter
        ccon = New ColorConverter()

        For row = 1 To rows

            For col = 1 To cols

                ReDim Preserve MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals) + 1)

                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).DeviceName = "undefined"
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).RasterName = "undefined"
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).SignalName = "undefined"
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).Status = "Invalid"
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).ForceRegister = True

                'ADDED if condition, 02262015

                If MyInvisibleSignals IsNot Nothing Then
                    sender.SignalIndex(row, col) = UBound(MyIncaInterface.mySignals) - (UBound(MyInvisibleSignals) + 1)
                Else
                    sender.SignalIndex(row, col) = UBound(MyIncaInterface.mySignals) - 1
                End If

                'sender.Row = row
                sender.CurrentCell = sender(row, col)

                sender.VariableName(row, col) = "undefined"
                sender.DisplayName(row, col) = "undefined"
                sender.HighThresh(row, col) = 10000000
                sender.LowThresh(row, col) = -10000000
                sender.EqualTo(row, col) = ""
                sender.CheckForDataChange(row, col) = False
                sender.DefaultCellBackColor(row, col) = Color.White
                sender.DefaultCellForeColor(row, col) = Color.Black
                sender.HighThreshBackColor(row, col) = Color.Red
                sender.HighThreshForeColor(row, col) = Color.White
                sender.LowThreshBackColor(row, col) = Color.Red
                sender.LowThreshForeColor(row, col) = Color.White
                sender.DeviceName(row, col) = "undefined"
                sender.Raster(row, col) = "undefined"
                sender.AlsoAssociatedWith(row, col) = ""
                sender.DisplayFormat(row, col) = """0.000"""

                sender.CurrentBackColor(row, col) = sender.DefaultCellBackColor(row, col)

                sender.DataFrozen(row, col) = False

                sender.DataFrozenCounter(row, col) = 0

                'sender.Col = 0

                sender.CurrentCell = sender(row, 0)

                If col = 1 Then
                    sender.Text = sender.DisplayName(row, col)
                    'sender.Row = 0
                    'sender.Col = 1
                    sender.CurrentCell = sender(0, 1)


                    sender.Text = "1"
                End If

                If col > 1 Then
                    'sender.Row = 0
                    'sender.Col = col
                    sender.CurrentCell = sender(0, col)

                    sender.Text = CStr(col)
                End If

            Next col
        Next row

    End Sub
    ''' <summary>
    ''' Reads the device status from INCA or other hardware.
    ''' Returns False if device is not okay.
    ''' </summary>
    Private Async Function GetDeviceStatus() As Task(Of Boolean)
        ' Constants for device names
        Dim ProcessorDeviceNames As String() = {"HCF", "HCS", "XETK:1", "ACP2", "ACP3", "ACP4"}
        Dim CameraDevicePrefixes As String() = {"FCM"}
        ' Initialize variables
        Dim MyDevices() As IGM_INCA_Comm.INCADeviceStatus = Await MyIncaInterface.GetAvailableDevicesAsync(True)
        Dim isRed() As Boolean
        Dim processorsFalse As Integer = 0
        Dim numCameraDevicesInExperiment As Integer = 0
        'Static saveStatus() As Boolean
        'Static lMyDeviceStatus As Boolean = True
        ' Default return value
        Dim MyDevicesStatus As Boolean = True ' Initialize the device status
        Try
            ' Check if devices are available
            If MyDevices Is Nothing Then
                HandleCriticalError("Communication failure, Exiting CLEVIR and Closing INCA...")
                Return False ' Return False if no devices are available
            End If
            ' Initialize arrays
            ReDim isRed(UBound(MyDevices))
            If saveStatus Is Nothing Then ReDim saveStatus(UBound(MyDevices))
            ' Process each device
            For x As Integer = 0 To UBound(MyDevices)
                Dim device = MyDevices(x)
                Dim isCalcDev = InStr(UCase(device.myName), "CALCDEV") > 0
                ' Skip CALCDEV devices
                If Not isCalcDev Then
                    ' Update device status and UI
                    UpdateDeviceStatusUI(x, device, isRed)
                    ' Check processor faults
                    If ProcessorDeviceNames.Contains(device.myName) AndAlso Not device.myStatus Then
                        processorsFalse += 1
                        AppendLostDeviceName(device.myName)
                        'MyDevicesStatus = False ' Update status to False if a processor device fails
                    End If
                    ' Check camera devices
                    If CameraDevicePrefixes.Any(Function(prefix) InStr(device.myName, prefix) > 0) AndAlso Not device.myStatus Then
                        processorsFalse += 1
                        AppendLostDeviceName(device.myName)
                        'MyDevicesStatus = False ' Update status to False if a camera device fails
                    End If
                End If
            Next
            ' Handle device status based on initialization state
            If Not _initializing Then
                HandlePostInitializationStatus(MyDevices, isRed, saveStatus, processorsFalse)
            Else
                HandleInitializationStatus(MyDevices, isRed, numCameraDevicesInExperiment, MyDevicesStatus)
            End If
            ' Update global device status
            _MyDeviceStatus = lMyDeviceStatus
            ' Return the updated device status
            Return MyDevicesStatus
        Catch ex As Exception
            ' Log the exception
            HandleUserMessageLogging("GMRC", "GetDeviceStatus: " & ex.Message,,)
            ' Return False in case of an exception
            Return False
        End Try
    End Function

    ' Helper method to handle critical errors
    Private Sub HandleCriticalError(message As String)
        OnVehicleScreen.TopMost = True
        HandleUserMessageLogging("GMRC", message, DisplayMsgBox,)
        ExitApp("Complete")
    End Sub
    ' Helper method to update device status UI
    Private Sub UpdateDeviceStatusUI(index As Integer, device As IGM_INCA_Comm.INCADeviceStatus, ByRef isRed() As Boolean)
        Dim MyColor As Color = If(device.myStatus, Color.Green, Color.Red)
        isRed(index) = Not device.myStatus
        ' Update UI labels dynamically
        Dim labelName As Label = CType(DeviceStatus.Controls($"Label{index + 1}"), Label)
        Dim labelStatus As Label = CType(DeviceStatus.Controls($"Label{24 - index}"), Label)
        labelName.Text = device.myName
        labelStatus.Text = device.myStatus.ToString()
        labelStatus.BackColor = MyColor
    End Sub
    ' Helper method to append lost device names
    Private Sub AppendLostDeviceName(deviceName As String)
        If String.IsNullOrEmpty(SaveLostDeviceName) Then
            SaveLostDeviceName = deviceName
        Else
            SaveLostDeviceName &= "," & deviceName
        End If
    End Sub
    ' Helper method to handle post-initialization status
    Private Sub HandlePostInitializationStatus(MyDevices() As IGM_INCA_Comm.INCADeviceStatus, isRed() As Boolean, ByRef saveStatus() As Boolean, processorsFalse As Integer)
        For x As Integer = 0 To UBound(MyDevices)
            Dim device = MyDevices(x)
            Dim isCalcDev = InStr(UCase(device.myName), "CALCDEV") > 0
            If Not isCalcDev AndAlso saveStatus(x) <> isRed(x) Then
                If isRed(x) Then
                    HandleUserMessageLogging("GMRC", $"Cannot Communicate with {device.myName}")
                Else
                    HandleUserMessageLogging("GMRC", $"Communication with {device.myName} established")
                End If
            End If
        Next
        If processorsFalse > 0 AndAlso Not IgnoreLostDeviceForThisDrive Then
            _MyDeviceStatus = False
        ElseIf processorsFalse = 0 OrElse OverrideCommFailureInDebugMode Then
            _MyDeviceStatus = True
        End If
        saveStatus = isRed
    End Sub
    ' Helper method to handle initialization status
    Private Sub HandleInitializationStatus(MyDevices() As IGM_INCA_Comm.INCADeviceStatus, isRed() As Boolean, ByRef numCameraDevicesInExperiment As Integer, ByRef MyDevicesStatus As Boolean)
        For x As Integer = 0 To UBound(MyDevices)
            Dim device = MyDevices(x)
            Dim isCalcDev = InStr(UCase(device.myName), "CALCDEV") > 0
            If Not isCalcDev AndAlso isRed(x) Then
                HandleUserMessageLogging("GMRC", $"Cannot Communicate with {device.myName} at Initialization...")
                If Not Debugger.IsAttached Then
                    Dim dlgResult As DialogResult
                    Using topmostOwner As New Form() With {
                        .TopMost = True,
                        .Size = New System.Drawing.Size(1, 1),
                        .StartPosition = FormStartPosition.CenterScreen,
                        .ShowInTaskbar = False,
                        .FormBorderStyle = FormBorderStyle.None,
                        .Opacity = 0
                    }
                        topmostOwner.Show()
                        dlgResult = MessageBox.Show(
                            topmostOwner,
                            $"Cannot Communicate with {device.myName}, Continue Initialization?",
                            "Device Communication Warning",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning)
                    End Using
                    If dlgResult = DialogResult.No Then
                        HandleUserMessageLogging("GMRC", "User chose not to continue with initialization.")
                        MyDevicesStatus = False
                        ExitApp("Complete")
                        Return
                    Else
                        HandleUserMessageLogging("GMRC", "User chose to continue with initialization.")
                    End If
                End If
            End If
            ' Count camera devices
            If InStr(UCase(device.myDeviceType), "VIDEO") > 0 Then
                numCameraDevicesInExperiment += 1
            End If
        Next
    End Sub

    Private Sub MyMiscInfo_Click(ByVal sender As ToolStripMenuItem, ByVal e As EventArgs)

        'MyMiscInfo is created in CreateMenus, as is the MyMiscInfo_Click handler.

        'The MyMiscInfo object represents the selections that can be made from the Misc Info drop
        'down menu selection.

        ' If sender.ToString = "Display Operation History" Then
        ' OperationHistory.Show()
        ' OperationHistory.BringToFront()
        'End If

        If sender.ToString = "Display Record Path/Filename" Then
            If Len(Label3.Text) > 0 Then
                MsgBox(Label3.Text & Label4.Text)
            Else
                MsgBox("No Record Path/Filename defined, please select a Login ID")
            End If
        End If

        If sender.ToString = "Display Update Rates" Then
            MsgBox("Display Refresh Rate = " & _displayRefreshRate & " Data Collection Rate = " & DataCollectionRate & " INCA Polling Rate = " & MyIncaInterface.GetINCAPollingRate)
        End If

    End Sub

    Private Sub MyToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) ' Handles MyToolStripMenuItem1.Click

        'This sub is called when the user selects a display from the displays menu.  All information about the forms, groups, 
        'and grids variable mapping to grid position, etc. is obtained at startup from a spreadsheet which is read in using 
        'the(ReadInSignalList)which is called at startup from the Initialize sub.

        'This sub simply displays the correct form based on who the "sender" is.  This is only applicable
        'to GmResidentClient drop down menu.  The form display is handled differently on the OnVehicleScreen
        'form

        Dim j As Integer

        'look through all forms and find the form with the name corresponding to the sender (which form was selected)
        If MyDFs(0) IsNot Nothing Then

            For j = 0 To MyDFs.Count - 1
                If MyDFs(j).Text = sender.ToString Then

                    If MyDFs(j).Visible = False Then
                        MyDFs(j).Left = Left
                        MyDFs(j).Top = Top
                    End If

                    MyDFs(j).Show()
                    MyDFs(j).BringToFront()
                    Exit Sub
                End If 'MyFormData(j).MyForm.Text <> sender.ToString

            Next j

        End If


        '**********************************************

        'CUSTOM SCREEN

        ShowCustomScreen(sender.ToString)

    End Sub

    Public Sub RegisterDisplaySignals(ByVal j As Integer)

        'Called from various click events which are associated with displaying a particular 
        'form which has been dynamically created based on information in the INCAVariableFile.

        'This routine will register the signals on a given form dynamically.
        'This will occur if the user has selected the GO/NOGO option on initialization, because the signals
        'associated with the display have not been registered until it has been displayed for the first time.

        Dim found As Boolean
        Dim e As EventArgs
        Dim z As Integer
        Dim x As Integer
        Dim row As Integer
        Dim col As Integer
        Dim reply As MsgBoxResult
        Dim saveBackColor As Color

        Dim signalsRegistered As Boolean
        Dim formName As String


        e = EventArgs.Empty
        reply = Nothing

        'need to handle top down view differently than other forms....

        If j <= MyDFs.Count - 1 Then
            signalsRegistered = MyDFs(j).SignalsRegistered
            formName = MyDFs(j).Text
        Else 'this is top down view case...
            signalsRegistered = _topDownSignalsRegistered
            formName = "Top Down View"
        End If

        If signalsRegistered = False And UCase(SignalRegistrationMode) = "GO/NOGO" Then

            If MyIncaInterface.GetRecordingState = True Then
                reply = MsgBox("Signals are not yet registered for " & formName & " To view this display, signals must be registered, which will require recording to be STOPPED. Stop Recording and Continue?", vbYesNo)
            ElseIf MyIncaInterface.GetMeasurementStatus = "True" Then
                reply = MsgBox("Signals are not yet registered for " & formName & " To view this display, signals must be registered, which will require Measurement to be STOPPED. Stop Measurement and Continue?", vbYesNo)
            Else
                MsgBox("Signals are not yet registered for " & formName & " Please Wait while signals are registered...")

            End If

            If reply = MsgBoxResult.No Then
                Exit Sub
            End If

            If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                OnVehicleScreen.Label5.Visible = True
                saveBackColor = OnVehicleScreen.Label5.BackColor
                OnVehicleScreen.Label5.BackColor = Color.Yellow
                OnVehicleScreen.Label5.Text = "Operation In Progress, Please Wait..."
                OnVehicleScreen.Refresh()
            End If

            If reply = MsgBoxResult.Yes Then

                If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                    OnVehicleScreen.Cursor = Cursors.WaitCursor
                    OnVehicleScreen.Button6.Text = "STOP MEASUREMENT"
                    MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                    .GetAwaiter().GetResult()

                Else
                    Cursor = Cursors.WaitCursor
                End If

                Do While MyIncaInterface.GetMeasurementStatus() = "True"
                    Thread.Sleep(100)
                Loop

            End If

            For z = 0 To myDGs.Count - 1
                If (myDGs(z).Parent.Name = formName) Or formName = "Top Down View" Then
                    found = True
                End If

                If found = True Then
                    found = False
                    For row = 1 To myDGs(z).RowCount - 1
                        For col = 1 To myDGs(z).ColumnCount - 1

                            For x = 0 To UBound(MyIncaInterface.myDisplaySignals)
                                If ((myDGs(z).AlsoAssociatedWith(row, col) <> "GO/NOGO" And
                                    myDGs(z).AlsoAssociatedWith(row, col) <> "AUTOANNO" And
                                   InStr(myDGs(z).AlsoAssociatedWith(row, col), "CS_") = 0 And
                                   formName <> "Top Down View") Or
                                   (formName = "Top Down View" And InStr(myDGs(z).AlsoAssociatedWith(row, col), "TD") > 0)) And
                                    (MyIncaInterface.myDisplaySignals(x).DeviceName = myDGs(z).DeviceName(row, col) And
                                    MyIncaInterface.myDisplaySignals(x).SignalName = myDGs(z).VariableName(row, col) And
                                    MyIncaInterface.myDisplaySignals(x).RasterName = myDGs(z).Raster(row, col)) Then
                                    MyIncaInterface.myDisplaySignals(x).ForceRegister = True
                                    myDGs(z).Registered(row, col) = True
                                    Exit For
                                End If
                            Next x

                        Next col
                    Next row
                End If

            Next z

            MyIncaInterface.myPreliminaryDisplaySignals = MyIncaInterface.myDisplaySignals

            MyIncaInterface.RegisterSignals()

            signalsRegistered = True

            If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                .GetAwaiter().GetResult()
            End If

            Cursor = Cursors.Arrow

            OnVehicleScreen.Label5.BackColor = saveBackColor

            ' Update the UI and save the filename.
            ' Use the new predicted filename helper to keep UI and MDA shortcuts consistent.
            Dim predictedFilenameForUi As String = GetPredictedRecordingFilename()
            If Not String.IsNullOrEmpty(predictedFilenameForUi) Then
                OnVehicleScreen.Label5.Text = $"Recording Filename: {predictedFilenameForUi}"
                SaveRecordingFileName = predictedFilenameForUi
                ' Keep MDA/shortcuts in sync before we start recording (idempotent if recording already started)
                'EnsureSetLastRecordingFileName(predictedFilenameForUi)
            End If
            OnVehicleScreen.Refresh()

            If j <= MyDFs.Count - 1 Then
                MyDFs(j).SignalsRegistered = signalsRegistered
            Else 'this is top down view case...
                _topDownSignalsRegistered = signalsRegistered
            End If

        End If

    End Sub

    Private Sub MyLabel_Click(ByVal sender As Object, ByVal e As EventArgs)

        'Click event for the GO/NOGO labels, displays the GO/NOGO form associated with the label

        'Dim thisLabel As Label = DirectCast(sender, Label)

        Dim compareString As String

        e = EventArgs.Empty

        For j = 0 To MyDFs.Count - 1

            If InStr(MyDFs(j).Text, "CS_") = 0 Then
                compareString = MyDFs(j).Text
            Else
                compareString = Mid(MyDFs(j).Text, 4, Len(MyDFs(j).Text))
            End If

            If InStr(sender.ToString, compareString) > 0 Then

                If InStr(MyDFs(j).Text, "CS_") = 0 Then

                    RegisterDisplaySignals(j)

                    If MyDFs(j).Visible = False Then

                        MyDFs(j).Left = 0
                        MyDFs(j).Top = 30

                        MyDFs(j).Show()
                        MyDFs(j).BringToFront()
                        MyDFs(j).Activate()

                        MyDFs(j).ShowInTaskbar = True

                    Else
                        MyDFs(j).Show()
                        MyDFs(j).BringToFront()
                        MyDFs(j).Activate()

                    End If

                Else

                    RegisterDisplaySignals(j)

                    'CompareString

                    Select Case MyDFs(j).Text
                        Case "CS_CoPilot Status"

                            CopilotStatusDisplay.Left = 0
                            CopilotStatusDisplay.Top = 45

                            CopilotStatusDisplay.Show()
                            CopilotStatusDisplay.BringToFront()
                            CopilotStatusDisplay.Activate()

                    End Select

                End If

                For i = 0 To myDGs.Count - 1
                    If myDGs(i) Is Nothing Then Continue For
                    If myDGs(i).ParentFormIndex = j Then

                        ' Only iterate data rows (x >= 1) because UpdateGridColor uses Rows(x-1)
                        Dim lastRow As Integer = Math.Max(1, myDGs(i).RowCount)
                        For x = 1 To lastRow
                            For y = 0 To myDGs(i).ColumnCount - 1

                                If sender.backcolor = Color.Gray Then
                                    ' Reset each cell to default safely via UpdateGridColor
                                    UpdateGridColor(i, x, y, GridUpdateActions.FromLow)
                                Else
                                    ' Marshal style writes to the UI thread — DataGridView is not thread-safe.
                                    Dim gi = i, gx = x, gy = y
                                    Dim backC As Color = myDGs(gi).CurrentBackColor(gx, gy)
                                    Dim foreC As Color = myDGs(gi).CurrentForeColor(gx, gy)
                                    If myDGs(gi).InvokeRequired Then
                                        myDGs(gi).Invoke(Sub()
                                                             myDGs(gi).Rows(gx - 1).Cells(gy).Style.BackColor = backC
                                                             myDGs(gi).Rows(gx - 1).Cells(gy).Style.ForeColor = foreC
                                                         End Sub)
                                    Else
                                        myDGs(gi).Rows(gx - 1).Cells(gy).Style.BackColor = backC
                                        myDGs(gi).Rows(gx - 1).Cells(gy).Style.ForeColor = foreC
                                    End If
                                End If


                            Next y
                        Next x

                        myDGs(i).Refresh()

                    End If
                Next i

            End If 'MyFormData(j).MyForm.Text <> sender.ToString

        Next j
    End Sub

    Public Sub MySubTabControl_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)

        'Clicking on a sub-tab in the annotation button interface part of the main display screen will either
        'change display based on tab selected (left mouse click) or will display the AnnotationInterfaceConfigure form
        '(right mouse click)...

        If e.Button = MouseButtons.Right Then

            AnnotationInterfaceConfigure.Show()
            AnnotationInterfaceConfigure.BringToFront()

            AnnotationInterfaceConfigure.ListBox2.Visible = True
            AnnotationInterfaceConfigure.Button2.Visible = True

            For x = 0 To AnnotationInterfaceConfigure.ListBox2.Items.Count - 1
                If AnnotationInterfaceConfigure.ListBox2.Items(x).ToString = MySubTabControl.TabPages(MySubTabControl.SelectedIndex).Text Then
                    AnnotationInterfaceConfigure.ListBox2.SetSelected(x, True)
                End If
            Next


        Else

            WhichTabAmIOn = MySubTabControl.SelectedIndex

        End If

    End Sub

    Public Sub MyMainTabControl_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        ' Respond to right-click on the main-tab for additional actions
        If e.Button = MouseButtons.Right Then
            AnnotationInterfaceConfigure.Show()
            AnnotationInterfaceConfigure.BringToFront()

            ' Hide certain controls on the configuration interface
            AnnotationInterfaceConfigure.ListBox2.Visible = False
            AnnotationInterfaceConfigure.Button2.Visible = False
            AnnotationInterfaceConfigure.Label2.Visible = False

            ' Ensure SelectedIndex is valid before accessing TabPages
            If MyMainTabControl.SelectedIndex >= 0 AndAlso MyMainTabControl.SelectedIndex < MyMainTabControl.TabPages.Count Then
                For x = 0 To AnnotationInterfaceConfigure.ListBox1.Items.Count - 1
                    If AnnotationInterfaceConfigure.ListBox1.Items(x).ToString = MyMainTabControl.TabPages(MyMainTabControl.SelectedIndex).Text Then
                        AnnotationInterfaceConfigure.ListBox1.SetSelected(x, True)
                    End If
                Next
            End If
        End If
    End Sub

    Public Sub SizeAnnotationButtons(subTabs As Dictionary(Of String, DataDictionarySingleton.SubTab))
        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        If MyMainTabControl Is Nothing Then
            Throw New InvalidOperationException("Main tab control is not initialized.")
        End If

        ' Clear existing tabs
        MyMainTabControl.TabPages.Clear()

        ' Populate tabs and buttons
        For Each subTab In From subTabEntry In subTabs Select subTabEntry.Value
            Dim tabPage As New TabPage With {.Text = subTab.TabName, .BackColor = Color.Transparent}

            ' Button positioning
            Dim topOffset As Integer = DataDictionarySingleton.DefaultButtonTop
            Dim leftOffset As Integer = DataDictionarySingleton.DefaultButtonLeft

            For Each eventButton In subTab.EventButtons
                Dim button As New Button With {
                        .Text = eventButton.ButtonName,
                        .Width = DataDictionarySingleton.DefaultButtonWidth,
                        .Height = DataDictionarySingleton.DefaultButtonHeight,
                        .Left = leftOffset,
                        .Top = topOffset,
                        .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                        .FlatStyle = FlatStyle.System
                        }
                button.FlatAppearance.BorderColor = Color.DarkBlue
                button.FlatAppearance.BorderSize = 2
                button.FlatAppearance.MouseOverBackColor = Color.LightBlue
                button.FlatAppearance.MouseDownBackColor = Color.LightGray

                AddHandler button.Click, AddressOf Button_Click
                tabPage.Controls.Add(button)

                ' Adjust positions for the next button
                leftOffset += button.Width + DataDictionarySingleton.HorizButtonSpacing
                If leftOffset + button.Width > MyMainTabControl.Width Then
                    leftOffset = DataDictionarySingleton.DefaultButtonLeft
                    topOffset += button.Height + DataDictionarySingleton.VertButtonSpacing
                End If
            Next

            MyMainTabControl.TabPages.Add(tabPage)
        Next

        If MyMainTabControl.TabPages.Count > 0 Then
            MyMainTabControl.SelectedIndex = 0
        End If

        ' Refresh
        MyMainTabControl.Invalidate()
        MyMainTabControl.Update()
    End Sub

    Public Sub InitializeAndSetupMainTabControl()
        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        ' Ensure main tab control is initialized
        If MyMainTabControl Is Nothing Then
            Dim calculatedTabWidth As Integer = DataDictionarySingleton.DynamicTabWidth
            MyMainTabControl = New TabControl() With {
            .Location = New Point(MainTabLeft, MainTabTop),
            .Size = New Size(_mainTabWidth, _mainTabHeight),
            .Visible = True,
            .Multiline = True,
            .ItemSize = New Size(calculatedTabWidth, 50),
            .Font = New Font("Segoe UI", 12, FontStyle.Bold)
        }

            ' Add mainTabControl to the form
            If Not OnVehicleScreen.Controls.Contains(MyMainTabControl) Then
                OnVehicleScreen.Controls.Add(MyMainTabControl)
            End If
        End If

        ' Configure additional properties
        MyMainTabControl.Appearance = TabAppearance.FlatButtons
        MyMainTabControl.ItemSize = New Size(75, 50)
        MyMainTabControl.SizeMode = TabSizeMode.Normal
        MyMainTabControl.DrawMode = TabDrawMode.Normal

        ' Attach event handlers
        AddHandler MyMainTabControl.DrawItem, AddressOf DrawTab
        'AddHandler MyMainTabControl.MouseDown, AddressOf MyMainTabControl_MouseDown

        ' Refresh control
        MyMainTabControl.Invalidate()
        MyMainTabControl.Update()
    End Sub

    Private Sub DrawTab(sender As Object, e As DrawItemEventArgs)
        Dim g As Graphics = e.Graphics
        Dim tabPage As TabPage = MyMainTabControl.TabPages(e.Index)
        Dim tabBounds As Rectangle = MyMainTabControl.GetTabRect(e.Index)

        tabBounds.Inflate(-1, -1)

        ' Fill the tab background
        If e.State = DrawItemState.Selected Then
            g.FillRectangle(Brushes.LightBlue, tabBounds)
        Else
            g.FillRectangle(Brushes.LightGray, tabBounds)
        End If

        ' Draw border
        Using borderPen As New Pen(Color.Black, 2)
            borderPen.DashStyle = Drawing2D.DashStyle.Solid
            g.DrawRectangle(borderPen, tabBounds)
        End Using

        ' Draw the tab text
        Dim stringFlags As New StringFormat With {
            .Alignment = StringAlignment.Center,
            .LineAlignment = StringAlignment.Center
        }

        g.DrawString(tabPage.Text, MyMainTabControl.Font, Brushes.Black, tabBounds, stringFlags)
    End Sub

    Private Function WhereTheHeckAmIAt(ByVal latPos As Double, ByVal lonPos As Double) As String

        'Called from MyBackGroundTasks:  Determines if we are on or off property based on GPS coordinates...

        Static saveWhereTheHeckAmIAt As String
        Dim whereAmI As String

        If latPos <> 0 And lonPos <> 0 Then

            If latPos > NorthMidpointLat Or latPos < SouthwestCornerLat Then
                whereAmI = "Off Property"
            ElseIf lonPos < NorthwestCornerLon Or lonPos > NortheastCornerLon Then
                whereAmI = "Off Property"
            ElseIf latPos < NortheastCornerLat And latPos > SoutheastCornerLat And
                   lonPos > SouthwestCornerLon And lonPos < NortheastCornerLon Then
                whereAmI = "On Property"
            ElseIf latPos < NorthwestCornerLat And latPos > NortheastCornerLat And
                   lonPos > NorthwestCornerLon And lonPos < NorthMidpointLon Then
                whereAmI = "On Property"
            Else 'this handles the "bermuda triangle" at the north east corner of MPG
                'If we are on property when we get here, we stay on property, if we are
                'off property when we get here, we stay off property...

                If saveWhereTheHeckAmIAt = "Off Property" Then
                    whereAmI = "Off Property"
                Else
                    whereAmI = "On Property"
                End If

            End If

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

    Private Async Function CheckForLostDevice() As Task
        ' This routine handles the case where communication to processors or video cameras has been lost.
        ' Allows user to reinitialize CLEVIR and INCA.

        Dim operatorMessage As String = ""
        Dim button1Text As String = ""
        Dim button2Text As String = ""
        Dim button3Text As String = ""
        Dim button4Text As String = ""

        Try
            If Not (_lostDevice OrElse VideoCameraNotUpdating OrElse BackgroundLoopCounterNotUpdating) Then
                Return
            End If

            If IgnoreLostDeviceUntilNextRecordingSession OrElse IgnoreLostDeviceForThisDrive Then
                Return
            End If

            HandleUserMessageLogging("GMRC", "CheckForLostDevice: RedisplayOnVehicleForm(Main) Called...")
            RedisplayOnVehicleForm("Main")

            ' Determine which alert to show based on what triggered the check
            If BackgroundLoopCounterNotUpdating AndAlso Not _lostDevice AndAlso
           (Not VideoCameraNotUpdating OrElse (VideoCameraNotUpdating AndAlso videoMessageLastDisplayed)) Then

                operatorMessage = "INVALID DATA ALERT! (" & SaveDataFrozenDeviceName & ") To ensure data quality, CLEVIR/INCA should be re-initialized. This will take approx 3 minutes"
                button1Text = "CANCEL"
                button2Text = "Re-Initialize NOW"
                button3Text = "Ignore All Alerts for This Drive"
                button4Text = "Ignore All Alerts for Recording Session"
                videoMessageLastDisplayed = False
                dataMessageLastDisplayed = True

            ElseIf VideoCameraNotUpdating AndAlso Not _lostDevice AndAlso
               (Not BackgroundLoopCounterNotUpdating OrElse (BackgroundLoopCounterNotUpdating AndAlso dataMessageLastDisplayed)) Then

                operatorMessage = "INVALID VIDEO ALERT! (" & SaveVideoFrozenDeviceName & ") CLEVIR/INCA should be re-initialized to insure video recording capability. This will take approx 3 minutes."
                button1Text = "CANCEL"
                button2Text = "Re-Initialize NOW"
                button3Text = "Ignore All Alerts for This Drive"
                button4Text = "Ignore All Alerts for Recording Session"
                videoMessageLastDisplayed = True
                dataMessageLastDisplayed = False

            ElseIf _lostDevice Then
                operatorMessage = "PROCESSOR COMMUNICATION ALERT! (" & SaveLostDeviceName & ") CLEVIR/INCA should be re-initialized to insure data and video recording capability. This will take approx 3 minutes."
                button1Text = "CANCEL"
                button2Text = "Re-Initialize NOW"
                button3Text = "Ignore All Alerts for This Drive"
                button4Text = ""
            End If

            HandleUserMessageLogging("GMRC", "CheckForLostDevice: " & operatorMessage)

            ' Show dialog without blocking async flow
            Dim result As Integer
            Using msgBox As New Cusmsgbox()
                result = msgBox.DisplayCusMsgBox(OnVehicleScreen, operatorMessage, "CLEVIR ALERT",
                                           button1Text, button2Text, button3Text, button4Text)
            End Using

            Select Case result
                Case 1 ' CANCEL
                    HandleUserMessageLogging("GMRC", "CheckForLostDevice: User Answered " & button1Text)
                    OnVehicleScreen.TopMost = False

                Case 2 ' Re-Initialize NOW
                    HandleUserMessageLogging("GMRC", "CheckForLostDevice: User Answered " & button2Text)

                    ' Offload re-initialization to background to avoid blocking UI
                    Await Task.Run(Async Function()
                                       Await PerformReinitialization()
                                   End Function)

                Case 3 ' Ignore All Alerts for This Drive
                    HandleUserMessageLogging("GMRC", "CheckForLostDevice: User Answered " & button3Text)
                    IgnoreLostDeviceForThisDrive = True
                    OnVehicleScreen.TopMost = False

                Case 4 ' Ignore All Alerts for Recording Session
                    HandleUserMessageLogging("GMRC", "CheckForLostDevice: User Answered " & button4Text)
                    IgnoreLostDeviceUntilNextRecordingSession = True
                    OnVehicleScreen.TopMost = False
            End Select

            ' Reset flags
            _lostDevice = False
            VideoCameraNotUpdating = False
            BackgroundLoopCounterNotUpdating = False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckForLostDevice: " & ex.Message)
        End Try
    End Function

    Private Async Function PerformReinitialization() As Task
        Try
            BackgroundLoopCounterNotUpdating = False
            VideoCameraNotUpdating = False

            ' ══════════════════════════════════════════════════════════
            ' STEP 1: Stop Recording (UI thread safe)
            ' ══════════════════════════════════════════════════════════
            If MyIncaInterface.GetRecordingState() Then
                ' ✅ FIXED: Direct call on UI thread (no Invoke needed)
                MyIncaInterface.StartStopRecord(OnVehicleScreen.Button14)
            ElseIf MyIncaInterface.GetMeasurementStatus() = "True" Then
                Await MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6)
            End If

            ' ══════════════════════════════════════════════════════════
            ' STEP 2: Update UI (already on UI thread)
            ' ══════════════════════════════════════════════════════════
            HandleButtonVisibility(False)

            _incaInitStarted = True
            _initializing = True

            ' ══════════════════════════════════════════════════════════
            ' STEP 3: Camera check (non-blocking background work)
            ' ══════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", "Checking for Cameras...",,, FlashMsgOn)

            ' ✅ FIXED: Offload only the network I/O (not UI updates)
            Await CheckForCameras(initialWaitTime:=CameraWaitTime, camerapingtime:=CameraPingTime, showToasts:=True)

            ' ══════════════════════════════════════════════════════════
            ' STEP 4: INCA cleanup (COM calls must stay on UI thread)
            ' ══════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", "Reinitializing INCA...",,, FlashMsgOn)

            ' ✅ FIXED: Direct call (COM requires STA thread)
            MyIncaInterface.RCI2_Cleanup()

            ' ✅ FIXED: Async workspace handling
            Dim returnStr As String = Await Task.Run(Function() As String
                                                         MyIncaInterface.HandleWorkspace("", False)
                                                         ' Assume success if no exception thrown
                                                         Return "True"
                                                     End Function)

            ' ══════════════════════════════════════════════════════════
            ' STEP 5: Signal registration
            ' ══════════════════════════════════════════════════════════
            ' ✅ VALIDATION: Check workspace handling result before proceeding
            If String.IsNullOrEmpty(returnStr) OrElse returnStr.Contains("ERROR:") Then
                HandleUserMessageLogging("GMRC", $"Workspace handling failed: {returnStr}", DisplayMsgBox)
                ExitApp("Complete")
                Return
            End If

            ' ✅ SUCCESS PATH: Workspace handled successfully, proceed with signal registration
            HandleUserMessageLogging("GMRC", "Registering Signals, please wait...",,, FlashMsgOn)

            ' ✅ FIXED: Offload CPU-bound work
            Await Task.Run(Sub()
                               MyIncaInterface.RegisterSignals()
                           End Sub)

            HandleUserMessageLogging("GMRC", "Registering Signals complete.",,, FlashMsgOn)

            ' ══════════════════════════════════════════════════════════
            ' STEP 6: Final cleanup (UI thread)
            ' ══════════════════════════════════════════════════════════
            UserStatusInfo.Hide()

            ' ✅ Bring OnVehicleScreen to front (consistent with FinalizeUI pattern)
            OnVehicleScreen.BringToFront()
            OnVehicleScreen.Activate()
            OnVehicleScreen.TopMost = False

            Await GetDeviceStatus()

            _incaInitStarted = False
            _initializing = False

            HandleButtonVisibility(True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"PerformReinitialization: {ex.Message}")
        End Try
    End Function

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
            If OnVehicleScreen.Label5.Visible = True Then
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
                        LoginForm.CheckBox3.Checked = False
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
                        LoginForm.CheckBox3.Checked = False
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

    Private Sub HandleRecordingDurationRotation(ByVal durationMinutes As Integer)
        If durationMinutes <= 0 Then
            RecorderStopWatch?.Reset()
            Return
        End If

        If RecorderStopWatch.Elapsed.TotalMilliseconds < (durationMinutes * 60000) Then Return

        ' ✅ Get current sequence BEFORE rotation
        Dim currentInfo = GetCurrentRecordingInfo()
        HandleUserMessageLogging("GMRC",
                                 $"Record duration ({durationMinutes} min) expired for seq {currentInfo.Sequence:D2}")

        ' ✅ CRITICAL: Rotate FIRST, then get the new filename
        MyIncaInterface.StopAndStartRecording()

        ' ✅ NOW get the predicted filename (should return sequence 02 after rotation)
        Dim predictedNext = GetPredictedRecordingFilename()
        HandleUserMessageLogging("GMRC", $"Rotation complete - new sequence: {predictedNext}")
        'EnsureSetLastRecordingFileName(predictedNext)

        If Not CheckRecordingFileNameFormat(displayMsg:=False) Then
            HandleUserMessageLogging("GMRC", "HandleRecordingDurationRotation: Recording file name format check failed", DisplayMsgBox)
            MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                .GetAwaiter().GetResult()
        End If

        RecorderStopWatch.Reset()
        RecorderStopWatch.Start()
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
                        MyTdGraphicsContainer.MyPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueRightArrow.Visible = False
                    Case "Gray"
                        MyTdGraphicsContainer.MyPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayLeftArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxGrayLeftArrow.BringToFront()
                        MyTdGraphicsContainer.MyPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueRightArrow.Visible = False
                    Case "Green"
                        MyTdGraphicsContainer.MyPictureBoxGreenLeftArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxGreenLeftArrow.BringToFront()
                        MyTdGraphicsContainer.MyPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueRightArrow.Visible = False
                    Case "Red"
                        MyTdGraphicsContainer.MyPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedLeftArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxRedLeftArrow.BringToFront()
                        MyTdGraphicsContainer.MyPictureBoxBlueRightArrow.Visible = False
                    Case "Yellow"
                        MyTdGraphicsContainer.MyPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowLeftArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxYellowLeftArrow.BringToFront()
                        MyTdGraphicsContainer.MyPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueRightArrow.Visible = False
                    Case "Blue"
                        MyTdGraphicsContainer.MyPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueRightArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxBlueRightArrow.BringToFront()
                End Select

            Case "Right"

                Select Case MycolorStr

                    Case "Invisible"
                        MyTdGraphicsContainer.MyPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueLeftArrow.Visible = False
                    Case "Gray"
                        MyTdGraphicsContainer.MyPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayRightArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxGrayRightArrow.BringToFront()
                        MyTdGraphicsContainer.MyPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueLeftArrow.Visible = False
                    Case "Green"
                        MyTdGraphicsContainer.MyPictureBoxGreenRightArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxGreenRightArrow.BringToFront()
                        MyTdGraphicsContainer.MyPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueLeftArrow.Visible = False
                    Case "Red"
                        MyTdGraphicsContainer.MyPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedRightArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxRedRightArrow.BringToFront()
                        MyTdGraphicsContainer.MyPictureBoxBlueLeftArrow.Visible = False
                    Case "Yellow"
                        MyTdGraphicsContainer.MyPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowRightArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxYellowRightArrow.BringToFront()
                        MyTdGraphicsContainer.MyPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueLeftArrow.Visible = False
                    Case "Blue"
                        MyTdGraphicsContainer.MyPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.MyPictureBoxBlueLeftArrow.Visible = True
                        MyTdGraphicsContainer.MyPictureBoxBlueLeftArrow.BringToFront()

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

            If MyTdGraphicsContainer.MyALCStatusRightLabel.Visible = False Then

                MyTdGraphicsContainer.MyALCStatusRightLabel.Visible = True
                MyTdGraphicsContainer.MyALCStatusRightLabel.BringToFront()

                MyTdGraphicsContainer.MyALCStatusLeftLabel.Visible = True
                MyTdGraphicsContainer.MyALCStatusLeftLabel.BringToFront()

                MyTdGraphicsContainer.MyALCReasonRightLabel.Visible = True
                MyTdGraphicsContainer.MyALCReasonRightLabel.BringToFront()

                MyTdGraphicsContainer.MyALCReasonLeftLabel.Visible = True
                MyTdGraphicsContainer.MyALCReasonLeftLabel.BringToFront()

            End If

            If whichSide = "None" Then

                reasonLeft = "NONE"
                reasonRight = "NONE"

                MyTdGraphicsContainer.MyALCReasonLeftLabel.Text = reasonLeft
                MyTdGraphicsContainer.MyALCReasonRightLabel.Text = reasonRight

                Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1)).SignalData
                'Mytempval = GetRandom(0, 4) 'FOR TESTING ONLY!!!
                tempstr1 = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1), MyDg.DeviceName(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1), MyDg.VariableName(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1))

            End If

            If InStr(tempstr1, "CeLFFR_e_LnChgRsn_None") > 0 Or tempstr1 = "0" Then

                If whichSide = "None" Then
                    reasonLeft = "NONE"
                    reasonRight = "NONE"
                    MyTdGraphicsContainer.MyALCReasonLeftLabel.Text = reasonLeft
                    MyTdGraphicsContainer.MyALCReasonRightLabel.Text = reasonRight
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
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "IN-ACTIVE"
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "IN-ACTIVE"

                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Inhb") > 0 Then 'Red

                If whichSide = "None" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "INHIBIT"
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "INHIBIT"

                    HandleArrowDisplay("Red", "Left")
                    HandleArrowDisplay("Red", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Standby") > 0 Then 'Gray

                If whichSide = "None" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "STAND BY"
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "STAND BY"

                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Gray", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChkLt") > 0 Then 'Yellow

                If whichSide <> "Right" Then

                    MyTdGraphicsContainer.MyALCReasonRightLabel.Text = ""
                    MyTdGraphicsContainer.MyALCReasonLeftLabel.Text = reasonLeft

                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "CHK LEFT"
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = ""
                    whichSide = "Left"
                    HandleArrowDisplay("Yellow", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChkRt") > 0 Then 'Yellow

                If whichSide <> "Left" Then

                    MyTdGraphicsContainer.MyALCReasonRightLabel.Text = reasonRight
                    MyTdGraphicsContainer.MyALCReasonLeftLabel.Text = ""

                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = ""
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "CHK RIGHT"
                    whichSide = "Right"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Yellow", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChgLt") > 0 Then 'Green

                If whichSide <> "Right" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "CHG LEFT"
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = ""
                    whichSide = "Left"
                    HandleArrowDisplay("Green", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChgRt") > 0 Then 'Green

                If whichSide <> "Left" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = ""
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "CHG RIGHT"
                    whichSide = "Right"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Green", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ReqCancel") > 0 Then 'Gray

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "REQ CANCEL"
                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "REQ CANCEL"
                    HandleArrowDisplay("Gray", "Right")
                    HandleArrowDisplay("Invisible", "Left")
                End If

                whichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_NotFeasible") > 0 Then 'Red
                MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "NOT FEAS"
                MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "NOT FEAS"
                HandleArrowDisplay("Red", "Left")
                HandleArrowDisplay("Red", "Right")
                whichSide = "None"
            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ReqInvld") > 0 Then 'Red

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "REQ INVALID"
                    HandleArrowDisplay("Red", "Left")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "REQ INVALID"
                    HandleArrowDisplay("Red", "Right")
                End If

                whichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Returning") > 0 Then 'Blue

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "RETURNING"
                    HandleArrowDisplay("Blue", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "RETURNING"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Blue", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Straddle") > 0 Then 'Red

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "STRADDLE"
                    HandleArrowDisplay("Red", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

                If whichSide = "Right" Then
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "STRADDLE"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Red", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_DrvrOvrrd") > 0 Then 'Gray

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "DRVR OVRD"
                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "DRVR OVRD"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Gray", "Right")
                End If

                whichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Cmpt") > 0 Then 'Invisible

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.MyALCStatusLeftLabel.Text = "COMPLETE"
                    HandleArrowDisplay("Invisible", "Left")
                End If

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.MyALCStatusRightLabel.Text = "COMPLETE"
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
                For i = 0 To MyDGs.Count - 1
                    If MyDGs(i) Is Nothing Then Continue For
                    If MyDGs(i).ParentFormIndex <> z Then Continue For

                    ' Only iterate data rows (x >= 1) because UpdateGridColor uses Rows(x-1)
                    Dim lastRow As Integer = Math.Max(1, MyDGs(i).RowCount - 1)
                    For x = 1 To lastRow
                        For y = 0 To MyDGs(i).ColumnCount - 1
                            MyDGs(i).DataFrozenCounter(x, y) = 0
                            MyDGs(i).DataFrozen(x, y) = False

                            If MyDGs(i).CurrentBackColor(x, y) <> MyDGs(i).DefaultCellBackColor(x, y) _
                       OrElse MyDGs(i).CurrentForeColor(x, y) <> MyDGs(i).DefaultCellForeColor(x, y) Then
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

    Private Function ProcessGrids(
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

        Dim dataFrozen As Boolean
        Dim logMsgWritten As Boolean

        Dim saveHealthCounter As Integer

        Dim MyDtCsSaveTime As DateTime
        Dim MyDtCsElapseTime As TimeSpan
        Dim activeDtc As Boolean
        Dim dtcTimerActive As Boolean

        'Dim saveDtcStrings As ArrayList = Nothing

        Dim acpDebugOneShot As Boolean

        Try

            If MyDGs(0) IsNot Nothing Then

                'Here we will loop through all of the grids on all of the forms...
                For z = 0 To MyDGs.Count - 1

                    whereAmI = "Go Thru All Displayed Grids"

                    'need to go thru all grids which are displayed, or are associated with gonogo, or are associated with a custom screen (CS_)....
                    If MyDGs(z).Parent.Visible = True Or MyDFs(MyDGs(z).ParentFormIndex).AlsoAssociatedWith = "GO/NOGO" Or MyDFs(MyDGs(z).ParentFormIndex).AlsoAssociatedWith = "AUTOANNO" Or
                (InStr(MyDFs(MyDGs(z).ParentFormIndex).AlsoAssociatedWith, "CS_") > 0) Then

                        whereAmI = "Grid Visible or GO/NOGO or CS_"

                        Mystr = ""
                        Mytempstr = ""
                        Mytempval = 0

                        If lRecordingState = True Then

                            If MyDGs(z).CS_NAN_STATUS > 0 Then
                                HandleNanStatus(MyDGs(z))
                            End If

                            If MyDGs(z).CS_WAVRECORD > 0 Then
                                If OnVehicleScreen.PictureBox1.BackColor = Color.Red Then
                                    Handle_CS_WAVRECORD(MyDGs(z))
                                End If
                            End If

                        End If

                        'This Routine handles all things related to vehicle mileage and where the vehicle is,
                        'on grounds or off grounds, etc. Will not be called unless CS_ODOMETER is defined in signallist...
                        If MyDGs(z).CS_ODOMETER > 0 Then
                            HandleOdometerRelatedStatus(MyDGs(z), saveOdoReading, saveWhereAmIAt, lRecordingState)
                        End If

                        'This Routine handles CLEVIR display of cluster message...
                        'Nothing happens unless CS_LCC_CLUSTER_MSG and CS_LCC_BUTTON_PRESS are defined in signal list...
                        If MyDGs(z).CS_LCC_CLUSTER_MSG > 0 And MyDGs(z).CS_LCC_BUTTON_PRESS > 0 Then
                            HandleClevirDisplayOfClusterMsgs(MyDGs(z))
                        End If

                        'this next section handles custom displays.  This is where data is put into the
                        'labels on the custom displays.  Background colors for labels are updated here based on values
                        'of variables associated with the various labels on the custom screens.  Whenever a custom 
                        'screen is added, code must be added here to handle updating. Code is only executed if custom

                        'ADD CUSTOM SCREEN HANDLING HERE....

                        UpdateCustomDisplays(MyDGs(z))

                        If MyDGs(z).CS_CPSTOS_X_POS > 0 And MyDGs(z).CS_CPSTOS_Y_POS > 0 Then

                            If TargetStatusDisplay.Visible = True Then
                                TargetStatusDisplay.Label2.Text = Format$(Math.Abs(Math.Sqrt((MySignalDataWithTime(MyDGs(z).SignalIndex(MyDGs(z).CS_CPSTOS_X_POS, 1)).SignalData * MySignalDataWithTime(MyDGs(z).SignalIndex(MyDGs(z).CS_CPSTOS_X_POS, 1)).SignalData) +
                        (MySignalDataWithTime(MyDGs(z).SignalIndex(MyDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData * MySignalDataWithTime(MyDGs(z).SignalIndex(MyDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData))), "0.00")
                            End If

                            Dim xPos As Double = MySignalDataWithTime(MyDGs(z).SignalIndex(MyDGs(z).CS_CPSTOS_X_POS, 1)).SignalData
                            Dim yPos As Double = MySignalDataWithTime(MyDGs(z).SignalIndex(MyDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData
                            Dim distText As String = Format$(Math.Abs(Math.Sqrt((xPos * xPos) + (yPos * yPos))), "0.00")
                            OnVehicleScreen.Label17.Text = distText

                        End If

                        If PedestrianStatusDisplay.Visible = True Then HandlePedestrianStatusDisplay(MyDGs(z))

                        ' ============================================================
                        ' SUSPEND LAYOUT FOR BATCHED GRID UPDATES
                        ' ============================================================
                        MyDGs(z).SuspendLayout()
                        Try
                            whereAmI = "Rows and Columns"
                            'For each grid, we must go through all rows and columns to update data display, background and foreground
                            'colors, etc.
                            For x = 0 To MyDGs(z).RowCount

                                For y = 0 To MyDGs(z).ColumnCount - 1

                                    Mytempstr = ""
                                    If MyDGs(z).SignalIndex(x, y) >= 0 And
                                Len(MyDGs(z).VariableName(x, y)) > 0 And
                                MyDGs(z).Registered(x, y) = True And
                                MyDGs(z).VariableName(x, y) <> "undefined" Then

                                        'This section handles putting dashes in all cells if object ID is 0...
                                        'assumes the "blanker" is the first row that must be "blanked" and all subsequent rows in the associated grid are "blanked"
                                        If (MyDGs(z).ObjectID_Start_Pos <> 0 And
                                     MySignalDataWithTime(MyDGs(z).SignalIndex(MyDGs(z).ObjectID_Start_Pos, y)).SignalData = 0) _
                                    AndAlso x > MyDGs(z).ObjectID_Start_Pos Then

                                            Mytempstr = "-"
                                            If MyDGs(z).CurrentBackColor(x, y) <> MyDGs(z).DefaultCellBackColor(x, y) Then
                                                UpdateGridColor(z, x, y, GridUpdateActions.FromLow)
                                            End If

                                        Else
                                            'HealtCounter is maintained in ProcessKiller thread.  A HealthCounter value of 0 indicates
                                            'a healthy system...
                                            If _healthCounter = 0 Then

                                                If saveHealthCounter > 1 Then
                                                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: HealthCounter = " & _healthCounter)
                                                End If
                                                saveHealthCounter = 0

                                                If Debugger.IsAttached Then

                                                    If OnVehicleScreen.CheckBox1.Checked = True Then
                                                        If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 And myDGs(z).DeviceName(x, y) = "ACP3_MCU" Then
                                                            If acpDebugOneShot = False Then
                                                                MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = GetRandom(0, 10)
                                                                acpDebugOneShot = True
                                                            Else
                                                                MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                                                                acpDebugOneShot = False
                                                            End If
                                                        End If
                                                    Else
                                                        If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 And myDGs(z).DeviceName(x, y) = "ACP3_MCU" Then
                                                            acpDebugOneShot = False
                                                            MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                                                        End If
                                                    End If

                                                End If

                                                'The CheckForDataChange property is associated with the same column in the
                                                'signal configuration spreadsheet.  This allows us to set up specific signals to watch
                                                'to see if they are changing.  If they do not change over a certain period of time,
                                                'this would indicate a communication issue....

                                                If MyDGs(z).CheckForDataChange(x, y) = True And MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex > -1 And
                                                (MyDGs(z).Name <> "INCA Video Status Items" Or (MyDGs(z).Name = "INCA Video Status Items" And lRecordingState = True)) Then
                                                    If MyDGs(z).SaveLastValue(x, y) = MySignalDataWithTime(MyDGs(z).SignalIndex(x, y)).SignalData Then

                                                        If MyDGs(z).DataFrozen(x, y) = False Then 'ADDED 04/14/2020
                                                            MyDGs(z).DataFrozenCounter(x, y) = MyDGs(z).DataFrozenCounter(x, y) + 1
                                                        End If

                                                        'the number DataFrozenMaxCount is somewhat arbitrary, this should be based on time because we 
                                                        'count up faster when we are waiting less time between cycles through this code
                                                        If MyDGs(z).DataFrozenCounter(x, y) > dataFrozenMaxCount And MyDGs(z).DataFrozen(x, y) = False Then

                                                            MyDGs(z).DataFrozenCounter(x, y) = 0
                                                            MyDGs(z).DataFrozen(x, y) = True

                                                            CheckForExperiment = False

                                                            If (MyDGs(z).CurrentBackColor(x, y) <> MyDGs(z).HighThreshBackColor(x, y)) Then
                                                                gridUpdateAction = GridUpdateActions.ToHigh
                                                                UpdateGridColor(z, x, y, gridUpdateAction)

                                                                If lRecordingState = True Then
                                                                    MyIncaInterface.WriteEventComment(MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - First Instance Data Frozen", True)
                                                                End If

                                                            Else

                                                                If lRecordingState = True Then
                                                                    MyIncaInterface.WriteEventComment(MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Not Updating", True)
                                                                End If

                                                            End If

                                                            'Here we set the go/nogo label color to red and save the device names for those
                                                            'devices with frozen data.  These FrozenDeviceNames are used when displaying
                                                            'user message indicating a data or video device issue...

                                                            If InStr(MyDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then

                                                                goNoGoFault(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                                UpdateGonogoLabelColor(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)

                                                                If MyDGs(z).Name <> "INCA Video Status Items" Then

                                                                    If Len(SaveDataFrozenDeviceName) = 0 Then
                                                                        SaveDataFrozenDeviceName = MyDGs(z).DeviceName(x, y)
                                                                    ElseIf InStr(SaveDataFrozenDeviceName, MyDGs(z).DeviceName(x, y)) = 0 Then
                                                                        SaveDataFrozenDeviceName = SaveDataFrozenDeviceName & "," & MyDGs(z).DeviceName(x, y)
                                                                    End If

                                                                Else

                                                                    If Len(SaveVideoFrozenDeviceName) = 0 Then
                                                                        SaveVideoFrozenDeviceName = MyDGs(z).DeviceName(x, y)
                                                                    ElseIf InStr(SaveVideoFrozenDeviceName, MyDGs(z).DeviceName(x, y)) = 0 Then
                                                                        SaveVideoFrozenDeviceName = SaveVideoFrozenDeviceName & "," & MyDGs(z).DeviceName(x, y)
                                                                    End If

                                                                End If

                                                            End If

                                                        End If
                                                    Else 'Value has changed...

                                                        'If any of the frozen data starts to change, we reset the frozen data counter and frozen data state here...
                                                        If MyDGs(z).CheckForDataChange(x, y) = True And MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex > -1 Then
                                                            MyDGs(z).DataFrozenCounter(x, y) = 0

                                                            If MyDGs(z).DataFrozen(x, y) = True Then
                                                                MyDGs(z).DataFrozen(x, y) = False

                                                                If lRecordingState = True Then
                                                                    MyIncaInterface.WriteEventComment(MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Resumed Updating", True)
                                                                Else
                                                                    HandleUserMessageLogging("GMRC", MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Resumed Updating")
                                                                End If

                                                                If MyDGs(z).Name <> "INCA Video Status Items" Then
                                                                    If InStr(SaveDataFrozenDeviceName, ",") > 0 Then
                                                                        Dim tempstr() As String
                                                                        tempstr = Split(SaveDataFrozenDeviceName, ",")
                                                                        SaveDataFrozenDeviceName = ""
                                                                        For i = 0 To UBound(tempstr)
                                                                            If tempstr(i) <> MyDGs(z).DeviceName(x, y) Then
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
                                                                            If tempstr(i) <> MyDGs(z).DeviceName(x, y) Then
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

                                                        If (MyDGs(z).CurrentBackColor(x, y) = MyDGs(z).HighThreshBackColor(x, y)) Then
                                                            gridUpdateAction = GridUpdateActions.FromHigh
                                                            UpdateGridColor(z, x, y, gridUpdateAction)
                                                        End If

                                                    End If
                                                End If

                                            Else 'HealthCounter > 0 indicates that we have most likely waiting on an INCA API Call...

                                                'If we are waiting on an INCA API Call, we should reset the frozen counter...
                                                If _healthCounter > 10 Then MyDGs(z).DataFrozenCounter(x, y) = 0
                                                If PlaybackMode = False Then CheckForExperiment = True

                                                If saveHealthCounter = 0 Then
                                                    If _healthCounter > 1 Then
                                                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: HealthCounter = " & _healthCounter)
                                                    End If
                                                End If

                                                saveHealthCounter = _healthCounter

                                            End If

                                            whereAmI = "Set Mytempval"

                                            Mytempval = MySignalDataWithTime(MyDGs(z).SignalIndex(x, y)).SignalData

                                            whereAmI = "Format Display String"

                                            ' Cache formatted string to avoid redundant FormatDisplayString calls
                                            ' In ProcessGrids - Handle first-time initialization
                                            If MyDGs(z).SaveFormattedString(x, y) Is Nothing OrElse String.IsNullOrEmpty(MyDGs(z).SaveFormattedString(x, y)) Then
                                                ' First time accessing this cell - format and cache
                                                Mytempstr = FormatDisplayString(Mytempval, MyDGs(z).DisplayFormat(x, y), MyDGs(z).DeviceName(x, y), MyDGs(z).VariableName(x, y))
                                                MyDGs(z).SaveFormattedString(x, y) = Mytempstr
                                                MyDGs(z).SaveLastValue(x, y) = Mytempval
                                            ElseIf MyDGs(z).SaveLastValue(x, y) = Mytempval Then
                                                ' Value unchanged - reuse cached formatted string
                                                Mytempstr = MyDGs(z).SaveFormattedString(x, y)
                                            Else
                                                ' Value changed - format and cache the new string
                                                Mytempstr = FormatDisplayString(Mytempval, MyDGs(z).DisplayFormat(x, y), MyDGs(z).DeviceName(x, y), MyDGs(z).VariableName(x, y))
                                                MyDGs(z).SaveFormattedString(x, y) = Mytempstr

                                                ' Update SaveLastValue (except for AUTOANNO)
                                                If MyDGs(z).AlsoAssociatedWith(x, y) <> "AUTOANNO" Then
                                                    MyDGs(z).SaveLastValue(x, y) = Mytempval
                                                End If
                                            End If

                                            'This section handles updating back color, fore color etc. based on settings
                                            'in excel configuration file

                                            whereAmI = "Update Grid Colors"

                                            'Here is where we handle updating grid cell colors based on data values in the variovus cells
                                            'We also update go/nogo label colors here if necessary.  This is done here for those variables
                                            'that are not frozen...

                                            If MyDGs(z).DataFrozen(x, y) = False Then

                                                whereAmI = "HighThresh Check"
                                                If Mytempval > MyDGs(z).HighThresh(x, y) And Len(MyDGs(z).EqualTo(x, y)) = 0 Then
                                                    If (MyDGs(z).CurrentBackColor(x, y) <> MyDGs(z).HighThreshBackColor(x, y)) Then

                                                        If InStr(MyDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & MyDGs(z).HighThresh(x, y),,,,, 1)
                                                        Else
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & MyDGs(z).HighThresh(x, y),,,,, 2)
                                                        End If

                                                        gridUpdateAction = GridUpdateActions.ToHigh
                                                        UpdateGridColor(z, x, y, gridUpdateAction)

                                                        If MyDGs(z).Name = "DTCs" Then

                                                            If activeDtc = False Then
                                                                ' First DTC detected - initialize the collection and log it
                                                                saveDtcStrings = New ArrayList From {
            MyDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(Mytempval, MyDGs(z).DisplayFormat(x, y), MyDGs(z).DeviceName(x, y), MyDGs(z).VariableName(x, y))
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
                                                                Dim currentDtc As String = MyDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(Mytempval, MyDGs(z).DisplayFormat(x, y), MyDGs(z).DeviceName(x, y), MyDGs(z).VariableName(x, y))

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
                                                            If InStr(MyDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                                goNoGoFault(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                                UpdateGonogoLabelColor(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                                                            End If

                                                        End If
                                                    End If

                                                ElseIf Mytempval <= MyDGs(z).HighThresh(x, y) And Len(MyDGs(z).EqualTo(x, y)) = 0 And MyDGs(z).Name = "DTCs" Then

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
                                                            goNoGoFault(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex) = False
                                                            UpdateGonogoLabelColor(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex, Color.Green)
                                                        End If
                                                    End If
                                                End If
                                                whereAmI = "LowThresh Check"
                                                If Mytempval < MyDGs(z).LowThresh(x, y) And Len(MyDGs(z).EqualTo(x, y)) = 0 Then
                                                    If (MyDGs(z).CurrentBackColor(x, y) <> MyDGs(z).LowThreshBackColor(x, y)) Then
                                                        If InStr(MyDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & MyDGs(z).LowThresh(x, y),,,,, 1)
                                                        Else
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & MyDGs(z).LowThresh(x, y),,,,, 2)
                                                        End If
                                                        gridUpdateAction = GridUpdateActions.ToLow
                                                        UpdateGridColor(z, x, y, gridUpdateAction)
                                                    End If
                                                    If InStr(MyDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                        goNoGoFault(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                        UpdateGonogoLabelColor(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                                                    End If

                                                End If
                                                whereAmI = "Reset Check HIGH"
                                                If Mytempval <= MyDGs(z).HighThresh(x, y) And Mytempval >= MyDGs(z).LowThresh(x, y) And Len(MyDGs(z).EqualTo(x, y)) = 0 Then
                                                    If (MyDGs(z).CurrentBackColor(x, y) = MyDGs(z).HighThreshBackColor(x, y)) Then
                                                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & MyDGs(z).HighThresh(x, y),,,,, 2)
                                                        gridUpdateAction = GridUpdateActions.FromHigh
                                                        UpdateGridColor(z, x, y, gridUpdateAction)

                                                    End If
                                                End If
                                                whereAmI = "Reset Check LOW"
                                                If Mytempval >= MyDGs(z).LowThresh(x, y) And Mytempval <= MyDGs(z).HighThresh(x, y) And Len(MyDGs(z).EqualTo(x, y)) = 0 Then
                                                    If (MyDGs(z).CurrentBackColor(x, y) = MyDGs(z).LowThreshBackColor(x, y)) Then
                                                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & MyDGs(z).LowThresh(x, y),,,,, 2)
                                                        gridUpdateAction = GridUpdateActions.FromLow
                                                        UpdateGridColor(z, x, y, gridUpdateAction)
                                                    End If
                                                End If
                                                whereAmI = "Equal To Check"
                                                If Len(MyDGs(z).EqualTo(x, y)) > 0 And MyDGs(z).CheckForDataChange(x, y) = False Then
                                                    If Mytempval = Val(MyDGs(z).EqualTo(x, y)) Then
                                                        If (MyDGs(z).CurrentBackColor(x, y) <> MyDGs(z).LowThreshBackColor(x, y)) Then
                                                            If InStr(MyDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Equals " & Val(MyDGs(z).EqualTo(x, y)),,,,, 1)
                                                            Else
                                                                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Equals " & Val(MyDGs(z).EqualTo(x, y)),,,,, 2)
                                                            End If
                                                            gridUpdateAction = GridUpdateActions.ToLow
                                                            UpdateGridColor(z, x, y, gridUpdateAction)
                                                        End If
                                                        If InStr(MyDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                            goNoGoFault(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                            UpdateGonogoLabelColor(MyDFs(MyDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                                                        End If
                                                    Else
                                                        If (MyDGs(z).CurrentBackColor(x, y) = MyDGs(z).LowThreshBackColor(x, y)) Then
                                                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & MyDGs(z).DeviceName(x, y) & " - " & MyDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Not Equal To " & Val(MyDGs(z).EqualTo(x, y)),,,,, 2)
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
                                    If MyDGs(z).Parent.Visible = True Then
                                        If x > 0 And y >= 1 Then
                                            MyDGs(z).DataArray(x - 1, y) = Mytempstr
                                        End If
                                        whereAmI = "Total Num Signals Displayed Calculation..."
                                        lTotalnumsignalsdisplayed += 1
                                    End If
                                Next y
                            Next x
                        Finally
                            ' RESUME LAYOUT - Single redraw at end
                            MyDGs(z).ResumeLayout(True)
                        End Try
                        ' ============================================================
                        ' END SUSPEND LAYOUT BLOCK
                        ' ============================================================

                        whereAmI = "Out of X Y Loops - Ready to Refresh Grid..."

                        ' Throttled refresh using the existing dictionary-based timing
                        Dim key = MyDGs(z).Name
                        Dim nowT = DateTime.UtcNow
                        Dim nextAt As DateTime
                        If Not _gridRefreshAt.TryGetValue(key, nextAt) OrElse nowT >= nextAt Then
                            MyDGs(z).Refresh()
                            _gridRefreshAt(key) = nowT + GridMinRefresh
                        End If
                    End If
                Next z

                whereAmI = "Out of Z Loop..."

                'Because there may be multiple grid cells that can influence go/nogo label colors, we must
                'reconcile what the actual color of the label should be here, after going through all forms
                'grids and cells above.  Here we also reset the GoNoGoFault array here so nothing gets "stuck on"...
                For z = 0 To MyDFs.Count - 1
                    If MyDFs(z).GoNoGoIndex > -1 Then
                        If goNoGoFault(MyDFs(z).GoNoGoIndex) = False Then
                            UpdateGonogoLabelColor(MyDFs(z).GoNoGoIndex, Color.Green)
                        Else
                            UpdateGonogoLabelColor(MyDFs(z).GoNoGoIndex, Color.Red)

                            For i = 0 To MyDGs.Count - 1

                                If MyDGs(i).ParentFormIndex = z Then
                                    For x = 0 To MyDGs(i).RowCount
                                        For y = 0 To MyDGs(i).ColumnCount - 1
                                            If MyDGs(i).DataFrozen(x, y) = True Then
                                                dataFrozen = True
                                                Exit For
                                            End If
                                        Next y
                                        If dataFrozen = True Then
                                            Exit For
                                        End If
                                    Next x
                                    If dataFrozen = True Then
                                        Exit For
                                    End If
                                End If
                            Next i

                            If dataFrozen = False Then
                                goNoGoFault(MyDFs(z).GoNoGoIndex) = False
                                If MyDFs(z).Name = "INCA Video Status" Then
                                    UpdateGonogoLabelColor(MyDFs(z).GoNoGoIndex, Color.Green)
                                ElseIf MyDFs(z).Name = "INCA COMM Status" Then
                                    UpdateGonogoLabelColor(MyDFs(z).GoNoGoIndex, Color.Green)
                                End If
                            Else
                                dataFrozen = False
                            End If
                        End If
                    End If
                Next z
            End If 'Check for MyDGs(0) Is Nothing
        Catch ex As Exception

            If logMsgWritten = False Then
                HandleUserMessageLogging("GMRC", "processGrids: " & whereAmI & " - " & ex.Message)
                logMsgWritten = True
            End If

        End Try
        ' All code paths lead here; implicit return of Nothing (no return type declared)
    End Function


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
            ' ====================================================================================
            ' ISSUE 2 FIX: Early return if user has chosen to ignore alerts
            ' ====================================================================================
            If IgnoreLostDeviceUntilNextRecordingSession Then
                ' User chose "Ignore All Alerts for Recording Session" - don't update GO/NOGO colors
                ' This prevents RED/GREEN flashing after alert suppression
                Return
            End If

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
        ' Only update flags if alerts are not being ignored
        If Not IgnoreLostDeviceForThisDrive Then
            If MyColor = Color.Red Then
                If Not OverrideCommFailureInDebugMode Then
                    BackgroundLoopCounterNotUpdating = True
                End If
                _MyCpuStaleData = True
            Else
                BackgroundLoopCounterNotUpdating = False
                _MyCpuStaleData = False
            End If
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
            Catch
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

    ''' <summary>
    ''' ✅ REFACTORED: Toast-based system health monitor (no form, no focus stealing)
    ''' Monitors INCA initialization, API health, device status, and data freshness
    ''' </summary>
    Private Sub ProcessKiller()
        Dim saveIncaInitElapseTime As DateTime = DateTime.Now
        Dim saveHealthMonitor As Integer = 0
        Dim updateInterval As Integer = 1000

        ' Track last alert time to avoid spam (30-second cooldown)
        Dim lastHealthAlertTime As DateTime = DateTime.MinValue
        Dim lastDeviceAlertTime As DateTime = DateTime.MinValue
        Dim lastDataAlertTime As DateTime = DateTime.MinValue
        Dim lastInitAlertTime As DateTime = DateTime.MinValue
        Const AlertCooldownSeconds As Integer = 30

        Try
            Do While Not _killProcesses
                Application.DoEvents()

                If _incaInitStarted Then
                    ' ════════════════════════════════════════════════════════════════
                    ' INITIALIZATION MONITORING (before background tasks start)
                    ' ════════════════════════════════════════════════════════════════
                    Dim initElapsed = DateTime.Now.Subtract(saveIncaInitElapseTime)

                    ' Warn at 2.75 minutes (yellow threshold)
                    If initElapsed.TotalMinutes >= 2.75 AndAlso initElapsed.TotalMinutes < 3.5 Then
                        If (DateTime.Now - lastInitAlertTime).TotalSeconds > AlertCooldownSeconds Then
                            StatusNotifier.Toast(
                                $"INCA initialization taking longer than expected: {initElapsed.Minutes}:{initElapsed.Seconds:D2}",
                                ToastKind.Warning,
                                "Initialization",
                                durationMs:=8000,
                                ensureMainOnTop:=False
                            )
                            lastInitAlertTime = DateTime.Now
                        End If
                    End If

                    ' Critical alert at 3.5 minutes (red threshold)
                    If initElapsed.TotalMinutes >= 3.5 Then
                        If (DateTime.Now - lastInitAlertTime).TotalSeconds > AlertCooldownSeconds Then
                            StatusNotifier.ToastError(
                                $"INCA initialization timeout: {initElapsed.Minutes}:{initElapsed.Seconds:D2} - possible hang",
                                "Critical: Initialization",
                                durationMs:=12000,
                                ensureMainOnTop:=False
                            )
                            _healthCounter = 10  ' Trigger critical state
                            lastInitAlertTime = DateTime.Now
                        End If
                    End If

                Else
                    ' ════════════════════════════════════════════════════════════════
                    ' RUNTIME MONITORING (after initialization complete)
                    ' ════════════════════════════════════════════════════════════════
                    saveIncaInitElapseTime = DateTime.Now

                    If _enableMyBackgroundTasks AndAlso Not ExitPressed Then
                        HandleKillerAudioAlerts()

                        ' ──────────────────────────────────────────────────────────────
                        ' Monitor API Health (background loop communication)
                        ' ──────────────────────────────────────────────────────────────
                        If _healthMonitor = saveHealthMonitor Then
                            _healthCounter += 1

                            ' Warning at 10 iterations (API sluggish)
                            If _healthCounter = 10 Then
                                If (DateTime.Now - lastHealthAlertTime).TotalSeconds > AlertCooldownSeconds Then
                                    StatusNotifier.Toast(
                                        "INCA API responding slowly",
                                        ToastKind.Warning,
                                        "API Health",
                                        durationMs:=6000,
                                        ensureMainOnTop:=False
                                    )
                                    lastHealthAlertTime = DateTime.Now
                                End If
                            End If

                            ' Critical at 1000+ iterations (API likely hung)
                            If _healthCounter >= 1000 Then
                                If (DateTime.Now - lastHealthAlertTime).TotalSeconds > AlertCooldownSeconds Then
                                    StatusNotifier.ToastError(
                                        "INCA API communication failure - no response from background loop",
                                        "Critical: API Failure",
                                        durationMs:=12000,
                                        ensureMainOnTop:=False
                                    )
                                    lastHealthAlertTime = DateTime.Now
                                    CheckForApiCommError()
                                End If
                            End If
                        Else
                            ' Health monitor updated - API is responding
                            If INCACommCheckStopWatch IsNot Nothing Then INCACommCheckStopWatch = Nothing
                            saveHealthMonitor = _healthMonitor
                            _healthCounter = 0

                            ' Check device and data status now that API is healthy
                            UpdateDeviceAndDataStatusToastBased(lastDeviceAlertTime, lastDataAlertTime, AlertCooldownSeconds)
                        End If

                        ' Adjust monitoring frequency based on state
                        If _healthCounter >= 10 OrElse _lostDevice OrElse _MyCpuStaleData Then
                            updateInterval = 500  ' Monitor more frequently when errors present
                        ElseIf MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.Recording Then
                            updateInterval = 1000 ' Normal frequency during recording
                        Else
                            updateInterval = 2000 ' Slower when idle
                        End If

                        'HandleBackgroundEncryption()
                    Else
                        ' Background tasks not enabled yet - just monitor health counter
                        If _healthMonitor = saveHealthMonitor Then
                            _healthCounter += 1
                        Else
                            saveHealthMonitor = _healthMonitor
                            _healthCounter = 0
                        End If
                    End If
                End If

                ' Break sleep into smaller chunks for faster shutdown response
                For i As Integer = 0 To updateInterval Step 100
                    If _killProcesses Then Exit Do
                    Thread.Sleep(Math.Min(100, updateInterval - i))
                Next
            Loop

            TerminateIncaProcesses()

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ProcessKiller: " & ex.Message, DisplayMsgBox)
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Toast-based device and data status monitoring (replaces UpdateDeviceAndDataStatus)
    ''' </summary>
    Private Sub UpdateDeviceAndDataStatusToastBased(ByRef lastDeviceAlertTime As DateTime,
                                                     ByRef lastDataAlertTime As DateTime,
                                                     cooldownSeconds As Integer)
        Static Mysavetime4 As DateTime = DateTime.Now

        ' ──────────────────────────────────────────────────────────────
        ' Monitor Device Status (lost device detection)
        ' ──────────────────────────────────────────────────────────────
        If Not _MyDeviceStatus Then
            If DateTime.Now.Subtract(Mysavetime4).Seconds > 25 Then
                Mysavetime4 = DateTime.Now

                If Not Debugger.IsAttached AndAlso Not IgnoreLostDeviceForThisDrive Then
                    _lostDevice = True
                ElseIf TriggerCommFailureInDebugMode Then
                    IgnoreLostDeviceForThisDrive = False
                    IgnoreLostDeviceUntilNextRecordingSession = False
                    _lostDevice = True
                End If

                ' Alert user of lost device
                If _lostDevice AndAlso (DateTime.Now - lastDeviceAlertTime).TotalSeconds > cooldownSeconds Then
                    StatusNotifier.ToastError(
                        "Lost connection to INCA device - check hardware connections",
                        "Device Status",
                        durationMs:=10000,
                        ensureMainOnTop:=False
                    )
                    lastDeviceAlertTime = DateTime.Now
                End If
            End If
        Else
            Mysavetime4 = DateTime.Now
        End If

        ' ──────────────────────────────────────────────────────────────
        ' Monitor Data Freshness (stale CPU data detection)
        ' ──────────────────────────────────────────────────────────────
        If _MyCpuStaleData AndAlso _inMeasureMode Then
            If (DateTime.Now - lastDataAlertTime).TotalSeconds > cooldownSeconds Then
                StatusNotifier.ToastError(
                    "INCA data stream frozen or stale - measurement may have stopped",
                    "Data Status",
                    durationMs:=10000,
                    ensureMainOnTop:=False
                )
                lastDataAlertTime = DateTime.Now
            End If
        End If
    End Sub

    Private Sub SetupKillerForm(ByRef MyForm As Form, ByRef MyLabel As Label, ByRef MyLabel1 As Label, ByRef MyLabel2 As Label, ByRef MyLabel3 As Label)
        MyForm = New Form With {
            .FormBorderStyle = FormBorderStyle.FixedSingle,
            .ControlBox = False,
            .MaximizeBox = False,
            .MinimizeBox = True,
            .Text = "INCA Status",
            .ShowInTaskbar = True,
            .StartPosition = FormStartPosition.Manual,
            .Height = 67,
            .Width = 84,
            .Top = 85,
            .Left = 5,
            .BackColor = Color.MediumSpringGreen,
            .TopMost = True,
            .Visible = True,
            .Opacity = 0.75,
            .Owner = OnVehicleScreen
        }
        MyForm.BringToFront()

        MyLabel = New Label With {.Parent = MyForm, .Top = 0, .Left = 0, .Height = MyForm.Height - 33, .Width = MyForm.Width, .BackColor = MyForm.BackColor, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold), .ForeColor = Color.Black, .Visible = True}
        MyLabel1 = New Label With {.Parent = MyForm, .Top = 0, .Left = 0, .Height = MyForm.Height - 33, .Width = 42, .BackColor = MyForm.BackColor, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold), .ForeColor = Color.Black, .Visible = False}
        MyLabel2 = New Label With {.Parent = MyForm, .Top = 0, .Left = 42, .Height = MyForm.Height - 33, .Width = 42, .BackColor = MyForm.BackColor, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold), .ForeColor = Color.Black, .Visible = False}
        MyLabel3 = New Label With {.Parent = MyForm, .Top = 0, .Left = 84, .Height = MyForm.Height - 33, .Width = 42, .BackColor = MyForm.BackColor, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold), .ForeColor = Color.Black, .Visible = False}

        AddHandler MyLabel.Click, AddressOf MyKillerLabel_Click
        AddHandler MyLabel1.Click, AddressOf MyKillerLabel_Click
        AddHandler MyLabel2.Click, AddressOf MyKillerLabel_Click
        AddHandler MyLabel3.Click, AddressOf MyKillerLabel_Click
        AddHandler MyForm.FormClosing, AddressOf MyKillerForm_FormClosing
    End Sub

    Private Sub UpdateKillerInitStatus(ByVal MyLabel As Label, ByVal startTime As DateTime)
        Const incaInitDurationMinutes As Single = 2.75
        Const incaInitDurationMinutesRed As Single = 3.5

        MyLabel.BackColor = Color.MediumSpringGreen
        MyLabel.Visible = True

        Dim elapsedTime As TimeSpan = DateTime.Now.Subtract(startTime)
        MyLabel.Text = "Init - " & CStr(elapsedTime.Minutes) & ":" & Format(Math.Round(elapsedTime.Seconds, 2), "00")

        If elapsedTime.TotalMilliseconds >= incaInitDurationMinutes * 60000 Then
            MyLabel.BackColor = Color.Yellow
        End If

        If elapsedTime.TotalMilliseconds >= incaInitDurationMinutesRed * 60000 Then
            _healthCounter = 10
            MyLabel.BackColor = Color.Red
        End If
    End Sub

    Private Sub UpdateKillerMonitorStatus(ByVal MyLabel1 As Label, ByVal MyLabel2 As Label, ByVal MyLabel3 As Label, ByVal saveHealthMonitor As Integer)
        ' This method is called from ProcessKiller (background thread), so marshal all UI updates

        If MyLabel1.InvokeRequired Then
            ' We're on the wrong thread - marshal to UI thread
            MyLabel1.Invoke(Sub() UpdateKillerMonitorStatus(MyLabel1, MyLabel2, MyLabel3, saveHealthMonitor))
            Return
        End If

        ' Now safely on the UI thread
        If _healthMonitor = saveHealthMonitor Then
            _healthCounter += 1
            MyLabel1.BackColor = Color.Yellow
            MyLabel1.Visible = True
            MyLabel1.Text = "B"

            If _healthCounter >= 10 Then
                MyLabel1.BackColor = Color.Red
                MyLabel2.BackColor = Color.Gray
                MyLabel3.BackColor = Color.Gray
                MyLabel2.Visible = True
                MyLabel3.Visible = True
                CheckForApiCommError()
            End If
        Else
            If INCACommCheckStopWatch IsNot Nothing Then INCACommCheckStopWatch = Nothing
            saveHealthMonitor = _healthMonitor
            _healthCounter = 0
            MyLabel1.BackColor = Color.MediumSpringGreen
            MyLabel1.Visible = False
            UpdateDeviceAndDataStatus(MyLabel2, MyLabel3)
        End If
    End Sub

    Private Sub UpdateDeviceAndDataStatus(ByVal MyLabel2 As Label, ByVal MyLabel3 As Label)
        ' Marshal to UI thread if needed
        If MyLabel2.InvokeRequired Then
            MyLabel2.Invoke(Sub() UpdateDeviceAndDataStatus(MyLabel2, MyLabel3))
            Return
        End If

        Static Mysavetime4 As DateTime = DateTime.Now

        If Not _MyDeviceStatus Then
            MyLabel2.Text = "D"
            MyLabel2.BackColor = Color.Red
            MyLabel2.Visible = True
            If DateTime.Now.Subtract(Mysavetime4).Seconds > 25 Then
                Mysavetime4 = DateTime.Now
                If Not Debugger.IsAttached AndAlso Not IgnoreLostDeviceForThisDrive Then
                    _lostDevice = True
                ElseIf TriggerCommFailureInDebugMode Then
                    IgnoreLostDeviceForThisDrive = False
                    IgnoreLostDeviceUntilNextRecordingSession = False
                    _lostDevice = True
                End If
            End If
        Else
            MyLabel2.Visible = False
            MyLabel2.BackColor = Color.MediumSpringGreen
            Mysavetime4 = DateTime.Now
        End If

        If _MyCpuStaleData AndAlso _inMeasureMode Then
            MyLabel3.Text = "S"
            MyLabel3.BackColor = Color.Red
            MyLabel3.Visible = True
        Else
            MyLabel3.Visible = False
            MyLabel3.BackColor = If(_inMeasureMode, Color.MediumSpringGreen, Color.Gray)
        End If
    End Sub

    Private Sub CheckForApiCommError()
        If BackgroundLoopCounterNotUpdating OrElse VideoCameraNotUpdating OrElse _lostDevice OrElse INCACommIgnoreEvents Then
            INCACommCheckStopWatch?.Reset()
            Return
        End If

        If INCACommCheckStopWatch Is Nothing Then
            INCACommCheckStopWatch = Stopwatch.StartNew()
            INCACommCheckWarningTime = Val(APICommErrorMsgDelayTime)
        End If

        If INCACommCheckStopWatch.Elapsed.TotalSeconds >= INCACommCheckWarningTime Then
            Dim synth As New SpeechSynthesizer()
            synth.SelectVoice("Microsoft Zira Desktop")
            synth.Rate = 0
            Dim userMessage As String
            If INCACommCheckWarningTime = Val(APICommErrorMsgDelayTime) Then
                userMessage = "WARNING: Possible INCA API COMM ERROR detected. If issue persists, A RESTART of CLEVIR and INCA may be required."
                synth.Speak("WARNING: Possible INCA API communication ERROR detected")
            Else
                userMessage = If(MyIncaInterface.Recording,
                                 "INCA API COMM ERROR has been detected. A RESTART of CLEVIR and INCA may be required. If not, the current recording may continue indefinitely.",
                                 "INCA API COMM ERROR has been detected. A RESTART of CLEVIR and INCA may be required.")
                synth.Speak(If(MyIncaInterface.Recording, "INCA API communication ERROR detected during recording", "INCA API communication ERROR has been detected"))
            End If

            Dim lastAction = If(String.IsNullOrEmpty(ActiveIncaApiCall), "Unknown", ActiveIncaApiCall)
            HandleUserMessageLogging("GMRC", $"INCA API COMM ERROR has been detected. Last known Action = {lastAction} Recording = {MyIncaInterface.Recording}")
            HandleUserMessageLogging("GMRC", userMessage, DisplayMsgBox)

            If INCACommCheckWarningTime = RepeatWarningTimeInSeconds Then
                Dim commCheckResult As DialogResult
                Using topmostOwner As New Form() With {
                    .TopMost = True,
                    .Size = New System.Drawing.Size(1, 1),
                    .StartPosition = FormStartPosition.CenterScreen,
                    .ShowInTaskbar = False,
                    .FormBorderStyle = FormBorderStyle.None,
                    .Opacity = 0
                }
                    topmostOwner.Show()
                    commCheckResult = MessageBox.Show(
                        topmostOwner,
                        "Disable internal INCA Comm checking for this drive?",
                        "INCA Comm Check",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning)
                End Using
                If commCheckResult = DialogResult.Yes Then
                    INCACommIgnoreEvents = True
                    INCACommCheckStopWatch = Nothing
                    HandleUserMessageLogging("GMRC", "User Chose to disable INCA Comm checking.")
                Else
                    HandleUserMessageLogging("GMRC", "Click the B in the RED BOX (INCA Status window, Top Left) to Kill CLEVIR and INCA Processes, then RESTART CLEVIR.", DisplayMsgBox)
                End If
            End If

            If INCACommCheckStopWatch IsNot Nothing Then
                INCACommCheckWarningTime = RepeatWarningTimeInSeconds
                INCACommCheckStopWatch.Restart()
            End If
        End If
    End Sub

    Private Sub HandleKillerAudioAlerts()
        Static synth As New SpeechSynthesizer()
        Static voiceSelected As Boolean = False
        If Not voiceSelected Then
            synth.SelectVoice("Microsoft Zira Desktop")
            synth.Rate = 0
            voiceSelected = True
        End If

        Static lastSpoken As Dictionary(Of String, DateTime) = New Dictionary(Of String, DateTime)
        Const audioMessageFrequency As Integer = 10 'seconds

        If IgnoreLostDeviceForThisDrive OrElse IgnoreLostDeviceUntilNextRecordingSession Then Return

        Dim speak As Action(Of String) = Sub(key)
                                             If Not lastSpoken.ContainsKey(key) OrElse DateTime.Now.Subtract(lastSpoken(key)).TotalSeconds >= audioMessageFrequency Then
                                                 synth.SpeakAsync(key)
                                                 lastSpoken(key) = DateTime.Now
                                             End If
                                         End Sub

        If BackgroundLoopCounterNotUpdating AndAlso Not _lostDevice Then speak("In Valid Data Alert")
        If VideoCameraNotUpdating AndAlso Not _lostDevice Then speak("In Valid Video Alert")
        If _lostDevice Then speak("Processor Communication Alert")

        If LidarCaptureStarted AndAlso LidarDevices IsNot Nothing Then
            For Each lidar In LidarDevices
                If lidar.ShouldTriggerAudioAlert() Then
                    lidar.SpeakAlert()
                End If
            Next
        End If

    End Sub

    Private Sub HandleBackgroundEncryption()
        Static saveEncryptElapseTime As DateTime = DateTime.Now
        If Not UsingFlashDrive OrElse ExitPressed Then Return

        If DateTime.Now.Subtract(saveEncryptElapseTime).Seconds >= 10 Then
            If Directory.Exists(NetworkDriveLetter & "\CSAV2 Tools") Then
                EncryptFilesInDirectory(Path.Combine(BaseDataCollectionPath, "Data", "gmcsv" & VehicleNumber))
            Else
                HandleUserMessageLogging("GMRC", "No Valid CLEVIR Flash Drive Found. Files are no longer being encrypted...",,, FlashMsg5Sec)
                UsingFlashDrive = False
                NetworkDriveLetter = SaveNetworkDriveLetter
            End If
            saveEncryptElapseTime = DateTime.Now
        End If
    End Sub

    ''' <summary>
    ''' ✅ REFACTORED: Safely terminates INCA and related processes
    ''' Called from ProcessKiller when _killInca flag is set
    ''' </summary>
    Private Sub TerminateIncaProcesses()
        Try
            HandleUserMessageLogging("GMRC", "TerminateIncaProcesses: Starting INCA termination sequence...")

            ' ═══════════════════════════════════════════════════════════════
            ' STEP 1: Graceful shutdown via API (if available)
            ' ═══════════════════════════════════════════════════════════════
            If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                Try
                    HandleUserMessageLogging("GMRC", "TerminateIncaProcesses: Attempting graceful INCA shutdown via API...")

                    ' Stop any active measurement/recording
                    If MyIncaInterface.GetMeasurementStatus() = "True" Then
                        MyIncaInterface.StopMeasurement()
                        Thread.Sleep(1000) ' Allow INCA to stop cleanly
                    End If

                    ' Full shutdown via CloseINCA
                    MyIncaInterface.CloseINCA()
                    HandleUserMessageLogging("GMRC", "TerminateIncaProcesses: Graceful shutdown completed")

                    ' Wait for INCA to exit naturally
                    If WaitForIncaExit(timeoutSeconds:=15) Then
                        HandleUserMessageLogging("GMRC", "TerminateIncaProcesses: INCA exited gracefully")
                        Return ' Success - no need for forceful termination
                    End If

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: Graceful shutdown failed - {ex.Message}")
                    ' Continue to forceful termination
                End Try
            End If

            ' ═══════════════════════════════════════════════════════════════
            ' STEP 2: Forceful process termination (fallback)
            ' ═══════════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", "TerminateIncaProcesses: Proceeding with forceful process termination...")

            Dim targetProcesses As String() = {"INCA", "TGTSVR", "ETK", "XCP"}
            Dim terminatedCount As Integer = 0

            For Each processName In targetProcesses
                Dim processes() As Process = Process.GetProcessesByName(processName)

                If processes IsNot Nothing AndAlso processes.Length > 0 Then
                    HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: Found {processes.Length} '{processName}' process(es)")

                    For Each proc In processes
                        If KillProcessSafely(proc, processName) Then
                            terminatedCount += 1
                        End If
                    Next
                End If
            Next

            If terminatedCount > 0 Then
                HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: Terminated {terminatedCount} process(es)")
            Else
                HandleUserMessageLogging("GMRC", "TerminateIncaProcesses: No INCA processes found to terminate")
            End If

            ' ═══════════════════════════════════════════════════════════════
            ' STEP 3: Cleanup COM references
            ' ═══════════════════════════════════════════════════════════════
            Try
                myinca = Nothing
                MyHWC = Nothing
                Initialized = False
                HandleUserMessageLogging("GMRC", "TerminateIncaProcesses: COM references cleared")
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: COM cleanup warning - {ex.Message}")
            End Try

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: Unhandled exception - {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ Helper: Waits for INCA process to exit naturally
    ''' </summary>
    Private Function WaitForIncaExit(timeoutSeconds As Integer) As Boolean
        Dim startTime As DateTime = DateTime.Now

        Do While DateTime.Now.Subtract(startTime).TotalSeconds < timeoutSeconds
            Dim processes() As Process = Process.GetProcessesByName("INCA")

            If processes Is Nothing OrElse processes.Length = 0 Then
                Return True ' INCA has exited
            End If

            Thread.Sleep(200)
        Loop

        Return False ' Timeout - INCA still running
    End Function

    ''' <summary>
    ''' ✅ Helper: Safely kills a process with error handling
    ''' </summary>
    Private Function KillProcessSafely(proc As Process, processName As String) As Boolean
        Try
            If proc Is Nothing OrElse proc.HasExited Then
                Return False
            End If

            HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: Killing {processName} (PID: {proc.Id})...")

            proc.Kill()

            ' Wait for process to actually exit (with timeout)
            If proc.WaitForExit(5000) Then
                HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: {processName} (PID: {proc.Id}) terminated")
                Return True
            Else
                HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: {processName} (PID: {proc.Id}) did not exit within timeout")
                Return False
            End If

        Catch ex As InvalidOperationException
            ' Process already exited - this is fine
            Return False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: Failed to kill {processName} - {ex.Message}")
            Return False

        Finally
            Try
                proc?.Dispose()
            Catch
                ' Ignore disposal errors
            End Try
        End Try
    End Function

    Private Sub MyKillerForm_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs)

        e.Cancel = True

    End Sub

    Private Sub MyKillerLabel_Click(sender As Object, e As EventArgs)

        'This event is fired if the user clicks on any of the status labels in the upper left corner of the screen.
        'If none of the labels are red, indicating a potential problem, clicking brings the main on vehicle
        'screen back to the top, otherwise, it does that as well as allows the user to kill the processes if they
        'choose to do so...

        _switchToMain = True

        If (_healthCounter >= 10 Or _MyDeviceStatus = False Or (_MyCpuStaleData = True And _inMeasureMode = True)) Then
            Dim killResult As DialogResult
            Using topmostOwner As New Form() With {
                .TopMost = True,
                .Size = New System.Drawing.Size(1, 1),
                .StartPosition = FormStartPosition.CenterScreen,
                .ShowInTaskbar = False,
                .FormBorderStyle = FormBorderStyle.None,
                .Opacity = 0
            }
                topmostOwner.Show()
                killResult = MessageBox.Show(
                    topmostOwner,
                    "Kill INCA Processes?",
                    "Kill INCA",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning)
            End Using

            If killResult = DialogResult.Yes Then
                _killInca = True
                _killProcesses = True
            Else
                'If running in debug mode in the design environment, we are typically not connected to hardware
                'so we would always get a processor comm fault, so processor comm fault is only triggered if
                'we are not in debug mode.  Here, is a way to trigger a processor comm fault in the design env.
                'For testing purposes...
                If Debugger.IsAttached Then
                    Dim commResult As DialogResult
                    Using topmostOwner As New Form() With {
                        .TopMost = True,
                        .Size = New System.Drawing.Size(1, 1),
                        .StartPosition = FormStartPosition.CenterScreen,
                        .ShowInTaskbar = False,
                        .FormBorderStyle = FormBorderStyle.None,
                        .Opacity = 0
                    }
                        topmostOwner.Show()
                        commResult = MessageBox.Show(
                            topmostOwner,
                            "Trigger Comm Failure in Debug Mode?",
                            "Debug Mode",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question)
                    End Using
                    If commResult = DialogResult.Yes Then
                        TriggerCommFailureInDebugMode = True
                        OverrideCommFailureInDebugMode = False
                    Else
                        OverrideCommFailureInDebugMode = True
                    End If
                End If
            End If
        End If

    End Sub

    Private Async Sub InitializationMonitor()
        Dim MyForm As Form = Nothing
        Dim MyList As ListBox = Nothing
        Dim MyProgressBar As ProgressBar = Nothing
        Dim MyHeaderLabel As Label = Nothing
        Dim MyFont As Font = Nothing
        Dim startTime As DateTime = DateTime.Now
        Dim saveString As String = ""
        Dim finalStatusShown As Boolean = False

        Try
            HandleUserMessageLogging("GMRC", "InitializationMonitor: Starting Initialization...")

            While Not StopTestProcess
                ' ✅ FIX: Skip UI creation if suppressed during signal registration
                If _suppressInitMonitorUI Then
                    Await Task.Delay(100)  ' Just wait quietly
                    Continue While  ' Skip the rest of the loop
                End If

                If ProgressBarEnable Then
                    If MyForm Is Nothing Then
                        ' ✅ SIMPLIFIED: Already on UI thread, just call directly
                        InitMon_CreateUi(MyForm, MyList, MyProgressBar, MyHeaderLabel, MyFont)
                    Else
                        ' ✅ SIMPLIFIED: Update UI directly
                        Dim currentStatus() As String = StatusString
                        InitMon_UpdateProgress(currentStatus, MyProgressBar, MyList, MyFont, saveString)

                        ' Non-blocking delay
                        Await Task.Delay(100)
                    End If
                Else
                    If MyForm IsNot Nothing AndAlso Not finalStatusShown Then
                        finalStatusShown = True
                        InitMon_ShowFinalSummary(MyList, MyProgressBar, startTime)
                        StopTestProcess = True
                        Exit While
                    End If

                    Await Task.Delay(InitMonitorSleepTime)
                End If
            End While

            ' Wait for user click if needed
            While _keepListBoxDisplayed
                Await Task.Delay(100)
            End While

            OnVehicleScreen.ShowInTaskbar = True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "InitializationMonitor: " & ex.Message, DisplayMsgBox)
        End Try
    End Sub

    ' =========================
    ' InitializationMonitor helpers
    ' =========================

    Private Sub InitMon_CreateUi(ByRef MyForm As Form,
                         ByRef MyList As ListBox,
                         ByRef MyProgressBar As ProgressBar,  ' Keep as ProgressBar type (base class)
                         ByRef MyHeaderLabel As Label,
                         ByRef MyFont As Font)
        Try
            ' Ensure we're on UI thread
            If Me.InvokeRequired Then
                Throw New InvalidOperationException("InitMon_CreateUi must be called on UI thread")
            End If
            ' Determine progress max for FULL registration
            If InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then
                _progressBarMax = UBound(MyIncaInterface.MySignals) + 1
            End If

            MyForm = New Form With {
        .FormBorderStyle = FormBorderStyle.None,
        .Height = 565,
        .Width = 800,
        .StartPosition = FormStartPosition.Manual,
        .Top = 0,
        .Left = 0,
        .BackColor = Color.WhiteSmoke,
        .Visible = True,
        .ShowInTaskbar = True
    }

            MyList = New ListBox With {
        .Parent = MyForm,
        .Visible = True,
        .Height = 180,
        .Top = 340,
        .Width = MyForm.Width - 10,
        .Left = 5
    }
            AddHandler MyList.Click, AddressOf Mylist_Click

            MyHeaderLabel = New Label With {
        .Parent = MyForm,
        .Height = 73,
        .Width = 800,
        .BackColor = MyForm.BackColor,
        .Top = 274,
        .Left = 20,
        .TextAlign = ContentAlignment.MiddleCenter,
        .Visible = True
    }

            Select Case UCase(SignalRegistrationMode)
                Case "FULL" : MyHeaderLabel.Text = "Performing FULL Signal Registration..."
                Case "DISPLAYS" : MyHeaderLabel.Text = "Registering DISPLAYS and GO/NOGO Signals..."
                Case "GO/NOGO" : MyHeaderLabel.Text = "Registering GO/NOGO Signals ONLY..."
                Case "NEW FULL" : MyHeaderLabel.Text = "Adding ALL Signals to NEW Blank Experiment (" & INCAExperiment & ")..."
                Case Else : MyHeaderLabel.Text = "Registering Signals..."
            End Select

            MyHeaderLabel.Font = New Font(MyHeaderLabel.Font.FontFamily, 14, FontStyle.Bold)

            ' ***** USE YOUR CUSTOM PROGRESS BAR HERE *****
            MyProgressBar = New PercentageProgressBar With {
        .Parent = MyForm,
        .Visible = True,
        .Maximum = Math.Max(1, _progressBarMax),
        .Height = 25,
        .Left = 5,
        .BackColor = Color.LightGray  ' Background color for unprogressed area
    }
            MyProgressBar.Top = (MyForm.Height - MyProgressBar.Height) - 10
            MyProgressBar.Width = MyForm.Width - 10
            MyProgressBar.Value = 0

            ' ***** NO NEED FOR GRAPHICS OR FONT ANYMORE *****
            ' The PercentageProgressBar handles its own painting
            MyFont = Nothing  ' Not needed anymore

            MyForm.TopMost = True
            ' Force form to display
            MyForm.Show()
            MyForm.BringToFront()
            MyForm.Activate()
            Application.DoEvents() ' Force immediate paint

            HandleUserMessageLogging("GMRC", "InitMon_CreateUi: Form created and displayed successfully")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"InitMon_CreateUi failed: {ex.Message}", DisplayMsgBox)
            Throw
        End Try
    End Sub

    Private Sub InitMon_UpdateProgress(ByVal currentStatus() As String,
                               ByRef MyProgressBar As ProgressBar,
                               ByVal MyList As ListBox,
                               ByVal MyFont As Font,  ' Can remove this parameter eventually
                               ByRef saveString As String)

        If currentStatus Is Nothing OrElse currentStatus.Length = 0 Then
            Exit Sub
        End If

        Dim last As String = currentStatus(UBound(currentStatus))
        Dim progressValue As Integer
        Dim progressSlice As String = If(last.Length >= 12, last.Substring(11).Trim(), String.Empty)

        If Integer.TryParse(progressSlice, progressValue) Then
            ' Progress update
            progressValue = Math.Max(0, Math.Min(progressValue, MyProgressBar.Maximum))
            If MyProgressBar.Value <> progressValue Then
                MyList.Items.Add("Processing " & progressValue & " of " & MyProgressBar.Maximum)
                MyList.SelectedIndex = MyList.Items.Count - 1
            End If

            ' Simply set the value - the PercentageProgressBar will repaint itself automatically
            MyProgressBar.Value = progressValue
            ' No need for manual graphics drawing or Refresh() call

        Else
            ' Status line update (no changes needed here)
            If saveString <> last Then
                MyList.Items.Add(last)
                MyList.SelectedIndex = MyList.Items.Count - 1
                MyList.Refresh()

                saveString = last

                If InStr(saveString, ">>> Inca <<<") > 0 Or InStr(saveString, "- INVALID -") > 0 Then
                    If _keepListBoxDisplayed = False Then
                        HandleUserMessageLogging("GMRC", "InitializationMonitor: First Signal Registration Error - " & saveString)
                        _keepListBoxDisplayed = True
                    End If

                    If InStr(UCase(saveString), "WINDOWS ERROR") > 0 Then
                        StopTestProcess = True
                        Exit Sub
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub InitMon_ShowFinalSummary(ByVal MyList As ListBox,
                                         ByVal MyProgressBar As ProgressBar,
                                         ByVal startTime As DateTime)

        MyProgressBar.Value = 0
        MyList.Items.Clear()
        MyList.Items.Add(Format(startTime, "HH:mm:ss") & " - Initialization Start Time")

        Dim allStatus() As String = StatusString
        If allStatus IsNot Nothing AndAlso allStatus.Length > 0 Then
            For i As Integer = 0 To UBound(allStatus)
                If InStr(allStatus(i), "Register Signals -") > 0 Then
                    MyList.Items.Add(allStatus(i))
                    MyList.SelectedIndex = MyList.Items.Count - 1
                End If
            Next
        End If

        MyList.Items.Add(Format(DateTime.Now, "HH:mm:ss") & " - CLEVIR Init End Time")

        If _keepListBoxDisplayed = True Then
            MyList.Items.Add("")
            MyList.Items.Add("")
            MyList.Items.Add("THERE WERE SIGNAL REGISTRATION ERRORS!  SCROLL BAR to view, CLICK HERE to CONTINUE.")
            MyList.Items.Add("The invalid signal list is in the " & My.Application.Info.DirectoryPath & " \InvalidSignalsLog.csv file.")
            MyList.Items.Add("")
            MyList.Items.Add("")
            MyList.SelectedIndex = MyList.Items.Count - 1
        Else
            ' No errors - dispose of the form after a brief delay to show completion
            Dim formToDispose As Form = MyList.FindForm()
            If formToDispose IsNot Nothing Then
                ' Show completion message briefly
                Thread.Sleep(2000)
                ' Clean up the form
                formToDispose.Close()
                formToDispose.Dispose()
            End If
        End If
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

    Public Sub ButtonContainers_Buttons_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)

        'ButtonContainers_Buttons are the annotation buttons on the main CLEVIR screen.  These buttons are created dynamically
        'at initialization based on the contents of the datadictionary.csv file...

        'Left button click writes event into INCA data, if we are recording, Right button click opens up the
        'AnnotationInterfaceConfiguration window...

        If e.Button = MouseButtons.Left Then
            HandleAnnotationButtons(sender)
        Else
            If sender.text <> "ANNOTATION" Then
                AnnotationInterfaceConfigure.Show()
                AnnotationInterfaceConfigure.BringToFront()
            End If
        End If

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

    Private Sub RecordPlaybackToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)

    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Private Sub DisplayRecordToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'This displays a message box which shows the current Record File Path and Filename.  This is
        'only accessed on the GmResidentClient screen from a drop down menu.  It is not directly
        'accessible from the OnVehicleScreen display.

        If Len(Label3.Text) > 0 Then
            MsgBox(Label3.Text & Label4.Text)
        Else
            MsgBox("No Record Path/Filename defined, please select a Login ID")
        End If

    End Sub

    Private Sub ChangeVehicleNumberToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ChangeVehicleNumberToolStripMenuItem.Click

        'Allows the user to change the vehicle number.  The initial vehicle number is set in the vehicleconfig.txt file.

        'the assumtion here is that there will be a unique vehicle number for each
        'computer on which the application will run, but we may need to change it if
        'we move the computer to a different vehicle...

        Dim tmpVehicleNumber As String

        tmpVehicleNumber = InputBox("Please enter the 8 character Vehicle ID", "INPUT VEHICLE NUMBER")

        If Len(tmpVehicleNumber) >= 8 Then
            VehicleNumber = tmpVehicleNumber

            Text = "Vehicle " & VehicleNumber

        Else
            If Len(tmpVehicleNumber) > 0 Then
                MsgBox("Invalid Vehicle Number entered, please enter a valid vehicle ID (8 characters for USA, 9 characters for China).")
            End If
        End If

    End Sub

    Private Sub SetDisplayRefreshRateToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles SetDisplayRefreshRateToolStripMenuItem.Click

    End Sub

    Private Sub ToolStripComboBox1_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ToolStripComboBox1.Click

    End Sub

    Private Sub ToolStripComboBox2_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ToolStripComboBox2.Click


    End Sub

    Private Sub SetDataCollectionRatemsecToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles SetDataCollectionRatemsecToolStripMenuItem.Click

    End Sub

    Private Sub DisplayUpdateRatesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)
        MsgBox("Display Refresh Rate = " & _displayRefreshRate & " Data Collection Rate = " & DataCollectionRate & " INCA Polling Rate = " & MyIncaInterface.GetINCAPollingRate)
    End Sub

    Private Sub ToolStripComboBox3_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ToolStripComboBox3.Click

    End Sub


    Private Sub ToolStripComboBox3_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ToolStripComboBox3.TextChanged

    End Sub

    Private Sub ToolStripComboBox1_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ToolStripComboBox1.TextChanged
        _displayRefreshRate = Val(ToolStripComboBox1.Text)
    End Sub

    Private Sub ToolStripComboBox2_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ToolStripComboBox2.SelectedIndexChanged

        If _initializing = False And MyIncaInterface IsNot Nothing Then

            DataCollectionRate = Val(ToolStripComboBox2.Text)

            If MyIncaInterface.MeasurementStarted = True Then
                MyIncaInterface.StartDataCollection(DataCollectionRate)
            End If

        End If

    End Sub

    Private Sub ToolStripComboBox2_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ToolStripComboBox2.TextChanged

    End Sub

    Private Sub SwitchINCAUserToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles SwitchINCAUserToolStripMenuItem.Click

    End Sub

    Private Sub AddRecordOnlySignalsToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles AddRecordOnlySignalsToolStripMenuItem.Click

        'This functionality is currently disabled...

        AddRecordOnlySignals.ShowDialog()
    End Sub

    Private Sub RestartINCAToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles RestartINCAToolStripMenuItem.Click

        If MsgBox("Are you sure you want to restart INCA?", vbYesNo) = vbYes Then
            ShutdownAndRestartInca(18000)
        End If

    End Sub

    Private Sub HdwrStatusToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)
        DeviceStatus.Show()
        DeviceStatus.BringToFront()
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

    Private Sub EditUserConfigFileToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles EditUserConfigFileToolStripMenuItem.Click
        DefaultConfiguration.ShowDialog()
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

    Private Sub MenuStrip1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles MenuStrip1.Click

        If MyTdGraphicsContainer.TopMost = True Then
            MyTdGraphicsContainer.TopMost = False
            MyTdGraphicsContainer.Hide()
        End If
    End Sub

    Private Sub MenuStrip1_ItemClicked(ByVal sender As System.Object, ByVal e As ToolStripItemClickedEventArgs) Handles MenuStrip1.ItemClicked
        If MyTdGraphicsContainer.TopMost = True Then
            MyTdGraphicsContainer.TopMost = False
            MyTdGraphicsContainer.Hide()
        End If
    End Sub

    Private Sub TESTToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles TESTToolStripMenuItem.Click

    End Sub

    Private Sub Label4_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Label4.Click

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button1.Click
        Close()
    End Sub

    Private Sub ListBox1_SelectedIndexChanged_1(ByVal sender As System.Object, ByVal e As EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ToggleOperatingModeToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ToggleOperatingModeToolStripMenuItem.Click

        'Allows the user to toggle the operating mode from DEVELOPMENT to DATALOGGING.  This change will take effect the next time CLEVIR is started.

        Dim filename As String
        Dim fnum As Integer

        Dim textstr As String

        filename = My.Application.Info.DirectoryPath & "\OperatingMode.txt"

        If ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)" Then
            ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Development)"

            If MsgBox("With USER DATA UPLOAD on Exit Enabled?", vbYesNo) = MsgBoxResult.Yes Then
                textstr = "DATALOGGINGWITHUPLOAD"
                UploadDataOnExit = True
            Else
                textstr = "DATALOGGING"
                UploadDataOnExit = False
            End If

            fnum = FreeFile()
            FileOpen(fnum, filename, OpenMode.Output)
            PrintLine(fnum, textstr)
            FileClose(fnum)

            CLEVIRFlavor = textstr

        Else

            fnum = FreeFile()
            ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)"
            FileOpen(fnum, filename, OpenMode.Output)
            PrintLine(fnum, "DEVELOPMENT")
            FileClose(fnum)

            CLEVIRFlavor = "DEVELOPMENT"

        End If

    End Sub

    Private Sub GmResidentClient_HandleCreated(sender As Object, e As EventArgs) Handles Me.HandleCreated

    End Sub

    Private Sub GmResidentClient_MarginChanged(sender As Object, e As EventArgs) Handles Me.MarginChanged

    End Sub

    Private Sub MuteVoiceRecordingMessagesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MuteVoiceRecordingMessagesToolStripMenuItem.Click

        If MuteVoiceRecordingMessagesToolStripMenuItem.Text = "Mute Voice Recording Status Messages" Then
            MuteVoiceRecordingMessages = True
            MuteVoiceRecordingMessagesToolStripMenuItem.Text = "Un-Mute Voice Recording Status Messages"
        Else
            MuteVoiceRecordingMessages = False
            MuteVoiceRecordingMessagesToolStripMenuItem.Text = "Mute Voice Recording Status Messages"
        End If
    End Sub

    Private Sub GroupBox1_Enter(sender As Object, e As EventArgs) Handles GroupBox1.Enter

    End Sub

    Private Sub Label5_Click(sender As Object, e As EventArgs) Handles Label5.Click

    End Sub

End Class

