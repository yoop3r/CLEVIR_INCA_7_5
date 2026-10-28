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
                        MonitorApiHealth(saveHealthMonitor, lastHealthAlertTime, lastDataAlertTime, AlertCooldownSeconds)

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

    Private Sub MonitorApiHealth(ByRef saveHealthMonitor As Integer, ByRef lastHealthAlertTime As DateTime, ByRef lastDataAlertTime As DateTime, ByVal AlertCooldownSeconds As Integer)
        'Called from ProcessKiller (killer thread) at runtime. Counts loops without a background-loop heartbeat (_healthMonitor)
        'and toasts slow/failed API; when the heartbeat moved, resets the counter and checks data freshness.

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

            ' Check data freshness now that API is healthy
            UpdateDataStatusToastBased(lastDataAlertTime, AlertCooldownSeconds)
        End If

    End Sub

    Private Function GetKillerUpdateInterval() As Integer
        'Called from ProcessKiller (killer thread). Polling interval in ms: faster when errors are present, slower when idle.

        If _healthCounter >= 10 OrElse LostDevice OrElse _MyCpuStaleData Then
            Return 500  ' Monitor more frequently when errors present
        ElseIf MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.Recording Then
            Return 1000 ' Normal frequency during recording
        Else
            Return 2000 ' Slower when idle
        End If

    End Function

    ''' <summary>
    ''' Toast-based data freshness monitoring (killer thread). Lost-device detection runs on the UI thread
    ''' (EvaluateLostDevice, from the 5 s device poll).
    ''' </summary>
    Private Sub UpdateDataStatusToastBased(ByRef lastDataAlertTime As DateTime, cooldownSeconds As Integer)
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

    ''' <summary>
    ''' True when INCA measurement or recording is active. Safe to call from the ProcessKiller thread.
    ''' </summary>
    Private Function IsMeasuringOrRecording() As Boolean
        If Volatile.Read(_inMeasureMode) Then Return True
        Dim inca = MyIncaInterface
        Return inca IsNot Nothing AndAlso inca.Recording
    End Function

    Private Sub CheckForApiCommError()
        If BackgroundLoopCounterNotUpdating OrElse VideoCameraNotUpdating OrElse LostDevice OrElse INCACommIgnoreEvents Then
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
                    HandleUserMessageLogging("GMRC", "To recover, exit using 'Exit CLEVIR And Close INCA', then RESTART CLEVIR.", DisplayMsgBox)
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

        Dim isLost As Boolean = LostDevice
        If BackgroundLoopCounterNotUpdating AndAlso Not isLost Then speak("In Valid Data Alert")
        If VideoCameraNotUpdating AndAlso Not isLost Then speak("In Valid Video Alert")
        If isLost Then speak(LostDeviceVoice)

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
    ''' Called when the ProcessKiller loop ends (_killProcesses set)
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

End Class
