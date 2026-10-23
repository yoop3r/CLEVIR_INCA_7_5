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

        'The logged-in user's email address (captured on LoginForm) now identifies the current
        'INCA user, falling back to DRVR00 when no email address was provided.
        EtasDefaultUserName = If(String.IsNullOrWhiteSpace(SaveEmailAddress), "DRVR00", UCase(SaveEmailAddress))

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
        Dim sw As Stopwatch = Stopwatch.StartNew()
        Dim needsBootDelay As Boolean = Await CamerasNeedBootDelayAsync(initialWaitTime, count, sw)

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
            LogCameraScanResults(results, count, showToasts)

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

    Private Async Function CamerasNeedBootDelayAsync(ByVal initialWaitTime As Integer, ByVal count As Integer, ByVal sw As Stopwatch) As Task(Of Boolean)
        'Called from CheckForCameras. Quick 2 s pre-check: the boot delay is only needed when some configured camera
        'does not respond yet (or when the pre-check fails).

        Dim needsBootDelay As Boolean = False

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

        Return needsBootDelay
    End Function

    Private Sub LogCameraScanResults(ByVal results As Boolean(), ByVal count As Integer, ByVal showToasts As Boolean)
        'Called from CheckForCameras. Logs (and optionally toasts) the found/unreachable status of each active camera.

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
    End Sub

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
        Catch exPortCheck As Exception
            HandleUserMessageLogging("GMRC", $"Port check to {ip}:{port} failed: {exPortCheck.Message}")
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
            InitializeLidarDevices(userWantsLidar)

            ' ════════════════════════════════════════════════════════════
            ' Time Sync Provider Initialization (OXTS / TimeMachine)
            ' ════════════════════════════════════════════════════════════
            If Not InitializeTimeSyncProvider() Then Return

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

    Private Sub InitializeLidarDevices(ByVal userWantsLidar As Boolean)
        'Called from Initialize. Validates configured LiDAR devices against the detected capture adapters and
        'sets LidarCaptureEnabled (respecting the user's configuration choice).

        Try
            Dim testDevices = SharpPcap.CaptureDeviceList.Instance

            If testDevices IsNot Nothing AndAlso testDevices.Count > 0 Then
                ' LiDAR hardware is available

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
    End Sub

    Private Function InitializeTimeSyncProvider() As Boolean
        'Called from Initialize. Starts the OXTS / TimeMachine time sync provider and links it to the LiDAR devices.
        'Returns False when Initialize must stop (GPS lock failed and the user chose to exit).

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
                                    Return False
                                End If
                                OxtsEnabled = False
                            End If
                        End If
                End Select
                ' ✅ Connect OXTS to all LiDAR devices
                If LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 AndAlso MyTimeSyncProvider IsNot Nothing Then
                    For Each lidar In LidarDevices
                        lidar.SetTimeSyncProvider(MyTimeSyncProvider)
                        HandleUserMessageLogging("GMRC", $"{MyTimeSyncProvider.ProviderName} linked to {lidar.DisplayName}")
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

        Return True
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
        If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
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
    ''' </summary>
    Private Function LoginIfRequired() As Boolean
        If exitInProgress Then
            HandleUserMessageLogging("GMRC", "LoginIfRequired: Exit in progress, skipping login")
            Return False
        End If

        Try
            HandleUserMessageLogging("GMRC", "LoginIfRequired: Preparing LoginForm...")

            Dim wasAlreadyLoggedIn As Boolean = Not String.IsNullOrEmpty(SaveLoginID)

            If Not wasAlreadyLoggedIn Then
                SaveLoginID = String.Empty
            End If

            LoginForm.DialogResult = DialogResult.None
            LoginForm.TopMost = True

            HandleUserMessageLogging("GMRC", $"LoginIfRequired: Showing LoginForm (wasAlreadyLoggedIn={wasAlreadyLoggedIn})")

            LoginForm.ShowDialog(Me)

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
                Catch exToast As Exception
                    ' ignore dismissal race conditions
                    HandleUserMessageLogging("GMRC", $"DismissInitToast: dismiss failed for {id} - {exToast.Message}")
                End Try
            Next
            _initToastIds.Clear()
        End SyncLock

        ' Back-compat: also dismiss any legacy single ID if still set
        If _initToastId <> Guid.Empty Then
            Try
                StatusNotifier.ToastDismiss(_initToastId, fade:=fade)
            Catch exLegacyToast As Exception
                HandleUserMessageLogging("GMRC", $"DismissInitToast: legacy dismiss failed for {_initToastId} - {exLegacyToast.Message}")
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
        If InStr(newExpName, ".csv") > 0 Then
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

        ' Show calibrate button for all real (non-Demo) logins
        If UCase(SaveLoginID) <> "DEMO" OrElse Debugger.IsAttached Then
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

        ' Voice recognition is no longer eagerly initialized at app startup.
        ' DataDictionarySingleton.VoiceRecognitionManager.Instance is lazily created and
        ' initialized the first time the user clicks Button23 (VOICE) on OnVehicleScreen -
        ' see Module1.ActivateDeactivateVoiceCommands, "enabled" case.
    End Sub

End Class
