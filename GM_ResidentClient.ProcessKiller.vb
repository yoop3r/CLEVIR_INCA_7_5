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
                    MonitorIncaInitialization(saveIncaInitElapseTime, lastInitAlertTime, AlertCooldownSeconds)

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
                        MonitorApiHealth(saveHealthMonitor, lastHealthAlertTime, lastDeviceAlertTime, lastDataAlertTime, AlertCooldownSeconds)

                        ' Adjust monitoring frequency based on state
                        updateInterval = GetKillerUpdateInterval()

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

    Private Sub MonitorIncaInitialization(ByVal saveIncaInitElapseTime As DateTime, ByRef lastInitAlertTime As DateTime, ByVal AlertCooldownSeconds As Integer)
        'Called from ProcessKiller (killer thread) while INCA initialization is in progress. Toasts a warning at 2.75 minutes
        'and a critical alert (forcing _healthCounter = 10) at 3.5 minutes, at most once per cooldown.

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

    End Sub

    Private Sub MonitorApiHealth(ByRef saveHealthMonitor As Integer, ByRef lastHealthAlertTime As DateTime, ByRef lastDeviceAlertTime As DateTime, ByRef lastDataAlertTime As DateTime, ByVal AlertCooldownSeconds As Integer)
        'Called from ProcessKiller (killer thread) at runtime. Counts loops without a background-loop heartbeat (_healthMonitor)
        'and toasts slow/failed API; when the heartbeat moved, resets the counter and checks device/data status.

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

    End Sub

    Private Function GetKillerUpdateInterval() As Integer
        'Called from ProcessKiller (killer thread). Polling interval in ms: faster when errors are present, slower when idle.

        If _healthCounter >= 10 OrElse _lostDevice OrElse _MyCpuStaleData Then
            Return 500  ' Monitor more frequently when errors present
        ElseIf MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.Recording Then
            Return 1000 ' Normal frequency during recording
        Else
            Return 2000 ' Slower when idle
        End If

    End Function

    ''' <summary>
    ''' ✅ NEW: Toast-based device and data status monitoring (replaces UpdateDeviceAndDataStatus)
    ''' </summary>
    Private Sub UpdateDeviceAndDataStatusToastBased(ByRef lastDeviceAlertTime As DateTime,
                                                     ByRef lastDataAlertTime As DateTime,
                                                     cooldownSeconds As Integer)
        Static Mysavetime4 As DateTime = DateTime.Now

        ' ──────────────────────────────────────────────────────────────
        ' Monitor Device Status (lost device detection) - only once measuring/recording
        ' ──────────────────────────────────────────────────────────────
        If Not _MyDeviceStatus AndAlso IsMeasuringOrRecording() Then
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
            If Not IsMeasuringOrRecording() Then
                ' Lost-device alerts only apply once measurement or recording is active
                Mysavetime4 = DateTime.Now
            ElseIf DateTime.Now.Subtract(Mysavetime4).Seconds > 25 Then
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

    ''' <summary>
    ''' True when INCA measurement or recording is active. Safe to call from the ProcessKiller thread.
    ''' </summary>
    Private Function IsMeasuringOrRecording() As Boolean
        If Volatile.Read(_inMeasureMode) Then Return True
        Dim inca = MyIncaInterface
        Return inca IsNot Nothing AndAlso inca.Recording
    End Function

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
            Catch exDispose As Exception
                ' Ignore disposal errors
                HandleUserMessageLogging("GMRC", $"TerminateIncaProcesses: process dispose failed - {exDispose.Message}")
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

End Class
