Option Strict Off
Option Explicit On

Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks

' Lost-device / invalid-data / invalid-video alerting.
' Detection runs on the UI thread from the 5 s device poll (debounced by LostDeviceMonitor). The alert is a
' single modeless box, so grids, GO/NOGO and frozen-data detection keep running while the driver decides.
' A click only records the answer; MyBackgroundTasks acts on it between passes, so Re-Initialize never
' overlaps ProcessGrids. The ProcessKiller thread repeats the voice alert while LostDevice is set.
Partial Public Class GmResidentClient

    Private Const LostDeviceDebounceSeconds As Integer = 25

    ' Cameras whose video froze while recording - held until a new recording or Re-Initialize (see VideoLossLatch).
    Private ReadOnly _videoLoss As New VideoLossLatch()

    Private Enum DeviceAlertKind
        None
        InvalidData
        InvalidVideo
        LostDevice
    End Enum

    Private ReadOnly _lostDeviceMonitor As New LostDeviceMonitor(TimeSpan.FromSeconds(LostDeviceDebounceSeconds))

    ' Read by the ProcessKiller thread - access through LostDevice / LostDeviceVoice only.
    Private _lostDevice As Boolean
    Private _lostDeviceVoice As String = "Processor Communication Alert"

    ' Raw result of the latest GetDeviceStatus poll (ignore flags not applied).
    Private _deviceHealthKnown As Boolean
    Private _devicesHealthy As Boolean = True
    Private _lostDeviceNames As String = ""
    Private _lostDeviceIsProcessor As Boolean = True
    Private _alertedDeviceIsProcessor As Boolean = True

    Private _deviceAlert As Cusmsgbox
    Private _deviceAlertKind As DeviceAlertKind
    Private _deviceAlertResolved As Boolean
    Private _deviceAlertResponse As Integer
    Private _deviceAlertResponseText As String = ""

    ' Cameras named on the open invalid-video alert, and on the alert the driver last answered.
    Private _deviceAlertVideoDevices As String = ""
    Private _deviceAlertResponseKind As DeviceAlertKind
    Private _deviceAlertResponseVideoDevices As String = ""

    Private Property LostDevice As Boolean
        Get
            Return Volatile.Read(_lostDevice)
        End Get
        Set(value As Boolean)
            Volatile.Write(_lostDevice, value)
        End Set
    End Property

    Private Property LostDeviceVoice As String
        Get
            Return Volatile.Read(_lostDeviceVoice)
        End Get
        Set(value As String)
            Volatile.Write(_lostDeviceVoice, value)
        End Set
    End Property

    ''' <summary>Called from GetDeviceStatus with the raw outcome of a successful device read.</summary>
    Private Sub RecordDeviceHealth(healthy As Boolean, lostNames As IEnumerable(Of String), anyProcessorLost As Boolean)
        _deviceHealthKnown = True
        _devicesHealthy = healthy
        _lostDeviceNames = String.Join(",", lostNames)
        _lostDeviceIsProcessor = anyProcessorLost OrElse _lostDeviceNames = ""
    End Sub

    ''' <summary>Called from Handle_5_SecondChecks right after GetDeviceStatus (UI thread).</summary>
    Private Sub EvaluateLostDevice()
        Try
            If Not _deviceHealthKnown Then Return ' transient read failure - keep the current state

            Dim now As DateTime = DateTime.Now
            Select Case _lostDeviceMonitor.Update(_devicesHealthy, IsMeasuringOrRecording(), AreLostDeviceAlertsAllowed(), now)
                Case LostDeviceTransition.Raised
                    RaiseLostDeviceAlert()
                Case LostDeviceTransition.Restored
                    AnnounceLostDeviceRestored(now)
            End Select

            LostDevice = _lostDeviceMonitor.IsRaised
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "EvaluateLostDevice: " & ex.Message)
        End Try
    End Sub

    Private Function AreLostDeviceAlertsAllowed() As Boolean
        If IgnoreLostDeviceForThisDrive OrElse IgnoreLostDeviceUntilNextRecordingSession Then Return False
        ' IDE sessions usually have no hardware attached; the debug injection file opts back in.
        Return Not Debugger.IsAttached OrElse IsLostDeviceInjectionActive()
    End Function

    Private Sub RaiseLostDeviceAlert()
        _alertedDeviceIsProcessor = _lostDeviceIsProcessor
        SaveLostDeviceName = If(_lostDeviceNames = "", "unknown device", _lostDeviceNames)
        LostDeviceVoice = If(_alertedDeviceIsProcessor, "Processor Communication Alert", "Camera Communication Alert")

        Dim since As DateTime = _lostDeviceMonitor.EpisodeStartedAt
        HandleUserMessageLogging("GMRC", $"Lost device alert: {LostDeviceAlertTitle()} ({SaveLostDeviceName}) - no communication since {since:HH:mm:ss}")
        StatusNotifier.ToastError($"Lost communication with {SaveLostDeviceName} since {since:HH:mm:ss} - data may not be recorded.",
                                  LostDeviceAlertTitle(), durationMs:=12000, ensureMainOnTop:=False)
        ShowDeviceAlert(DeviceAlertKind.LostDevice)
    End Sub

    Private Sub AnnounceLostDeviceRestored(now As DateTime)
        Dim since As DateTime = _lostDeviceMonitor.EpisodeStartedAt
        Dim names As String = If(String.IsNullOrEmpty(SaveLostDeviceName), "device", SaveLostDeviceName)
        Dim outage As TimeSpan = now - since
        Dim outageText As String = $"{Math.Floor(outage.TotalMinutes):0}:{outage.Seconds:00}"

        HandleUserMessageLogging("GMRC", $"Lost device restored: communication with {names} re-established at {now:HH:mm:ss} after {outageText} (min:sec)")

        ' Video lost during the outage cannot be re-synchronized with the MF4 data, so a camera always needs a Re-Initialize.
        Dim restoredText As String
        If _alertedDeviceIsProcessor Then
            SpeakDriverAlert("Processor Communication Restored")
            StatusNotifier.Toast($"Communication with {names} restored. Data between {since:HH:mm:ss} and {now:HH:mm:ss} may be incomplete.",
                                 StatusNotifier.ToastKind.Success, "Communication Restored", durationMs:=10000, ensureMainOnTop:=False)
            restoredText = $"Data recorded between {since:HH:mm:ss} and {now:HH:mm:ss} may be incomplete. Re-initialize only if problems persist."
        Else
            SpeakDriverAlert("Camera Communication Restored. Stop and restart recording to restore video.")
            StatusNotifier.Toast($"Communication with {names} restored, but video since {since:HH:mm:ss} is lost for this recording. " &
                                 "To restore it: Stop Record, then Start Record (Re-Initialize if video does not return).",
                                 StatusNotifier.ToastKind.Warning, "Communication Restored", durationMs:=12000, ensureMainOnTop:=False)
            restoredText = $"Video from {names} since {since:HH:mm:ss} is lost for the rest of this recording and will not re-synchronize with the data. " &
                           "To restore it: Stop Record, then Start Record. If video does not return, choose Re-Initialize NOW (INCA will be restarted, approx 3 minutes)."
        End If

        ' Leave the alert up, marked resolved, so a driver who missed the voice still sees what happened.
        If IsDeviceAlertOpen() AndAlso _deviceAlertKind = DeviceAlertKind.LostDevice AndAlso Not _deviceAlertResolved Then
            _deviceAlertResolved = True
            _deviceAlert.SetContent($"RESOLVED {now:HH:mm:ss}: communication with {names} restored after {outageText} (min:sec). " & restoredText,
                                    $"CLEVIR ALERT - RESOLVED {now:HH:mm:ss}", "OK", "Re-Initialize NOW")
        End If

        SaveLostDeviceName = ""
    End Sub

    Private Function LostDeviceAlertTitle() As String
        Return If(_alertedDeviceIsProcessor, "PROCESSOR COMMUNICATION ALERT", "CAMERA COMMUNICATION ALERT")
    End Function

    ''' <summary>
    ''' Called from MyBackgroundTasks every 20 s of measuring/recording. Shows the alert for an outstanding
    ''' condition if none is showing; never blocks.
    ''' </summary>
    Private Sub CheckForLostDevice()
        Try
            If IgnoreLostDeviceUntilNextRecordingSession OrElse IgnoreLostDeviceForThisDrive Then Return

            ' Video the driver chose to continue without, or held back behind an open Target Server message, must not
            ' take the video turn and starve the data alert.
            Dim videoPending As Boolean = IsVideoAlertPending()

            If LostDevice Then
                ShowDeviceAlert(DeviceAlertKind.LostDevice)

            ElseIf BackgroundLoopCounterNotUpdating AndAlso (Not videoPending OrElse videoMessageLastDisplayed) Then
                If ShowDeviceAlert(DeviceAlertKind.InvalidData) Then
                    videoMessageLastDisplayed = False
                    dataMessageLastDisplayed = True
                End If

            ElseIf videoPending AndAlso (Not BackgroundLoopCounterNotUpdating OrElse dataMessageLastDisplayed) Then
                If ShowDeviceAlert(DeviceAlertKind.InvalidVideo) Then
                    videoMessageLastDisplayed = True
                    dataMessageLastDisplayed = False
                End If
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckForLostDevice: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' One alert at a time. A lost device takes over an open data/video alert; a resolved notice is replaced by
    ''' any new alert; otherwise an open alert is left alone. Returns True if the alert was shown or replaced.
    ''' </summary>
    Private Function ShowDeviceAlert(kind As DeviceAlertKind) As Boolean
        If IsDeviceAlertOpen() AndAlso Not _deviceAlertResolved Then
            If _deviceAlertKind = kind OrElse kind <> DeviceAlertKind.LostDevice Then Return False
        End If

        Dim message As String
        Dim buttons As String()
        Select Case kind
            Case DeviceAlertKind.InvalidData
                message = "INVALID DATA ALERT! (" & NamedOrUnidentified(SaveDataFrozenDeviceName) & ") To ensure data quality, CLEVIR/INCA should be re-initialized (INCA will be restarted). This will take approx 3 minutes"
                buttons = {"CANCEL", "Re-Initialize NOW", "Ignore All Alerts for This Drive", "Ignore All Alerts for Recording Session"}
            Case DeviceAlertKind.InvalidVideo
                ' The Target Server alert (toast, voice, window raised) is answered first - INCA is blocked until then.
                ' Re-checked every 20 s, so this alert follows once the message is closed.
                If IsTargetServerMessageShowing() Then Return False
                Dim lostVideo As String = VideoLossDeviceNames()
                If _videoLoss.AllAccepted(lostVideo) Then Return False
                Dim alertVideo As String = _videoLoss.Unaccepted(lostVideo)
                Dim tsTitle As String = Nothing
                Dim tsText As String = Nothing
                TryGetRecentTargetServerMessage(tsTitle, tsText)
                message = VideoAlertText.InvalidVideoMessage(alertVideo, tsTitle, tsText)
                ' A dead camera is worth less than the vehicle data: let the driver drop just that camera's video
                ' alerts and keep processor/data/other-camera alerts. Needs a named camera.
                buttons = {"CANCEL", "Re-Initialize NOW", "Ignore All Alerts for This Drive",
                           If(alertVideo = "", "Ignore All Alerts for Recording Session", VideoAlertText.ContinueWithoutButton(alertVideo))}
                _deviceAlertVideoDevices = alertVideo
            Case Else
                message = $"{LostDeviceAlertTitle()}! ({SaveLostDeviceName}) No communication since {_lostDeviceMonitor.EpisodeStartedAt:HH:mm:ss}. " &
                          "CLEVIR/INCA should be re-initialized (INCA will be restarted) to insure data and video recording capability. This will take approx 3 minutes."
                buttons = {"CANCEL", "Re-Initialize NOW", "Ignore All Alerts for This Drive", ""}
        End Select

        HandleUserMessageLogging("GMRC", "CheckForLostDevice: " & message)

        _deviceAlertKind = kind
        _deviceAlertResolved = False

        If IsDeviceAlertOpen() Then
            _deviceAlert.SetContent(message, "CLEVIR ALERT", buttons(0), buttons(1), buttons(2), buttons(3))
            _deviceAlert.BringToFront()
            Return True
        End If

        RedisplayOnVehicleForm("Main")
        Dim alert As New Cusmsgbox()
        AddHandler alert.FormClosed, AddressOf DeviceAlert_FormClosed
        _deviceAlert = alert
        alert.ShowModeless(OnVehicleScreen, message, "CLEVIR ALERT", buttons(0), buttons(1), buttons(2), buttons(3))
        Return True
    End Function

    Private Function IsDeviceAlertOpen() As Boolean
        Return _deviceAlert IsNot Nothing AndAlso Not _deviceAlert.IsDisposed
    End Function

    ' A GO/NOGO can stay red across ResetDataFrozenTracking (recording start) before the cell re-freezes and records its device.
    Private Shared Function NamedOrUnidentified(deviceNames As String) As String
        Return If(String.IsNullOrWhiteSpace(deviceNames), VideoAlertText.UnidentifiedDevice, deviceNames)
    End Function

    ''' <summary>Called from MarkCellDataFrozen when a video GO/NOGO cell freezes while recording.</summary>
    Private Sub RecordVideoLoss(deviceName As String)
        If _videoLoss.Record(deviceName) Then
            HandleUserMessageLogging("GMRC", $"Video lost: {deviceName} - video GO/NOGO held RED until Re-Initialize or a new recording")
            ' The video GO/NOGO may already be red (another camera), which would not re-arm the alert by itself.
            If Not _videoLoss.AllAccepted(deviceName) AndAlso
               Not IgnoreLostDeviceForThisDrive AndAlso Not IgnoreLostDeviceUntilNextRecordingSession Then
                VideoCameraNotUpdating = True
            End If
        End If
    End Sub

    ''' <summary>Called from ResetDataFrozenTracking. An MF4 sequence rollover continues the recording and keeps the loss.</summary>
    Private Sub ResetVideoLoss(reason As String, sequenceRotation As Boolean)
        If _videoLoss.OnTrackingReset(sequenceRotation) Then
            HandleUserMessageLogging("GMRC", $"Video loss cleared: {reason}")
        ElseIf sequenceRotation AndAlso _videoLoss.IsLatched Then
            HandleUserMessageLogging("GMRC", $"Video loss kept across sequence rollover: {_videoLoss.DeviceList}")
        End If
    End Sub

    Private Sub ClearVideoLoss(reason As String)
        If _videoLoss.Clear() Then HandleUserMessageLogging("GMRC", $"Video loss cleared: {reason}")
    End Sub

    Private Function IsVideoLost() As Boolean
        Return _videoLoss.IsLatched
    End Function

    Private Function VideoLossDeviceNames() As String
        Return If(_videoLoss.IsLatched, _videoLoss.DeviceList, SaveVideoFrozenDeviceName)
    End Function

    ''' <summary>
    ''' The driver chose to keep collecting vehicle data without these cameras' video. Their GO/NOGO stays red; only
    ''' their video alerts stop, until their video is seen updating in a later recording, or Re-Initialize.
    ''' </summary>
    Private Sub ContinueWithoutVideo(devices As String)
        _videoLoss.Accept(devices)
        HandleUserMessageLogging("GMRC", $"Continuing without {devices} video - video alerts for {devices} off until its video returns in a new recording or Re-Initialize; all other alerts stay on")
        StatusNotifier.Toast(VideoAlertText.ContinuingWithoutToast(devices),
                             StatusNotifier.ToastKind.Warning, "Video", durationMs:=12000, ensureMainOnTop:=False)
    End Sub

    ''' <summary>
    ''' Invalid-video alert (and its voice) is due: a video GO/NOGO went red, the driver has not chosen to continue
    ''' without those cameras, and no Target Server message is showing (it is answered first). Thread-safe.
    ''' </summary>
    Private Function IsVideoAlertPending() As Boolean
        Return VideoCameraNotUpdating AndAlso Not IsTargetServerMessageShowing() AndAlso
               Not _videoLoss.AllAccepted(VideoLossDeviceNames())
    End Function

    ''' <summary>Called from MarkCellDataResumed when a camera's video timecode updates while recording.</summary>
    Private Sub OnVideoUpdating(deviceName As String)
        If Not _videoLoss.Restored(deviceName) Then Return
        HandleUserMessageLogging("GMRC", $"{deviceName} video is back in this recording - video alerts for {deviceName} on again")
        StatusNotifier.Toast($"{deviceName} video is back. Video alerts for {deviceName} are on again.",
                             StatusNotifier.ToastKind.Success, "Video", durationMs:=8000, ensureMainOnTop:=False)
    End Sub

    Private Sub DeviceAlert_FormClosed(sender As Object, e As FormClosedEventArgs)
        Dim alert As Cusmsgbox = TryCast(sender, Cusmsgbox)
        If alert Is Nothing Then Return
        RemoveHandler alert.FormClosed, AddressOf DeviceAlert_FormClosed
        If alert IsNot _deviceAlert Then Return ' closed by CloseDeviceAlert - no answer to act on

        _deviceAlert = Nothing
        _deviceAlertResponseKind = _deviceAlertKind
        _deviceAlertResponseVideoDevices = _deviceAlertVideoDevices
        _deviceAlertKind = DeviceAlertKind.None
        Dim choice As Integer = alert.SelectedOption
        If choice = 0 Then choice = 1 ' closed without a button (e.g. Alt+F4) = CANCEL / OK
        _deviceAlertResponseText = alert.ButtonText(choice)
        _deviceAlertResponse = choice
    End Sub

    ''' <summary>Closes the alert without treating it as answered (re-initialization, exit).</summary>
    Private Sub CloseDeviceAlert()
        Try
            Dim alert As Cusmsgbox = _deviceAlert
            _deviceAlert = Nothing
            _deviceAlertKind = DeviceAlertKind.None
            _deviceAlertResolved = False
            If alert IsNot Nothing AndAlso Not alert.IsDisposed Then alert.Close()
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CloseDeviceAlert: " & ex.Message)
        End Try
    End Sub

    ''' <summary>Called from MyBackgroundTasks every pass: acts on the driver's answer to the alert, if any.</summary>
    Private Async Function HandleDeviceAlertResponseIfAny() As Task
        Dim choice As Integer = _deviceAlertResponse
        If choice = 0 Then Return
        _deviceAlertResponse = 0

        Try
            HandleUserMessageLogging("GMRC", "CheckForLostDevice: User Answered " & _deviceAlertResponseText)

            Select Case choice
                Case 2 ' Re-Initialize NOW
                    Await PerformReinitialization()
                Case 3 ' Ignore All Alerts for This Drive
                    IgnoreLostDeviceForThisDrive = True
                    OnVehicleScreen.TopMost = False
                Case 4
                    If _deviceAlertResponseKind = DeviceAlertKind.InvalidVideo AndAlso _deviceAlertResponseVideoDevices <> "" Then
                        ContinueWithoutVideo(_deviceAlertResponseVideoDevices)
                    Else ' Ignore All Alerts for Recording Session
                        IgnoreLostDeviceUntilNextRecordingSession = True
                    End If
                    OnVehicleScreen.TopMost = False
                Case Else ' CANCEL / OK
                    OnVehicleScreen.TopMost = False
            End Select

            ' Answered: silence the current alarm. A device that is still lost is raised again after the debounce.
            _lostDeviceMonitor.Acknowledge(DateTime.Now)
            LostDevice = _lostDeviceMonitor.IsRaised
            VideoCameraNotUpdating = False
            BackgroundLoopCounterNotUpdating = False
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleDeviceAlertResponseIfAny: " & ex.Message)
        End Try
    End Function

    Private Function IsLostDeviceInjected(deviceName As String) As Boolean
#If DEBUG Then
        Dim target As String = LostDeviceInjectionTarget()
        Return target IsNot Nothing AndAlso String.Equals(deviceName, target, StringComparison.OrdinalIgnoreCase)
#Else
        Return False
#End If
    End Function

    Private Function IsLostDeviceInjectionActive() As Boolean
#If DEBUG Then
        Return LostDeviceInjectionTarget() IsNot Nothing
#Else
        Return False
#End If
    End Function

#If DEBUG Then
    Private Const LostDeviceInjectionFileName As String = "CLEVIR_INJECT_LOST_DEVICE"
    Private _lostDeviceInjectionCheckedAt As DateTime = DateTime.MinValue
    Private _lostDeviceInjectionTarget As String

    ''' <summary>
    ''' Debug builds only: while a file named CLEVIR_INJECT_LOST_DEVICE exists in the application folder,
    ''' GetDeviceStatus reports one device as lost - the device named on the file's first non-blank line, else
    ''' the first configured processor - and alerts are allowed even under the debugger. Delete the file to
    ''' restore. Returns Nothing when no injection is active.
    ''' </summary>
    Private Function LostDeviceInjectionTarget() As String
        If (DateTime.Now - _lostDeviceInjectionCheckedAt).TotalSeconds >= 1 Then
            _lostDeviceInjectionCheckedAt = DateTime.Now
            Try
                Dim injectionFile As String = Path.Combine(My.Application.Info.DirectoryPath, LostDeviceInjectionFileName)
                If File.Exists(injectionFile) Then
                    Dim named As String = File.ReadLines(injectionFile).Select(Function(l) l.Trim()).FirstOrDefault(Function(l) l <> "")
                    _lostDeviceInjectionTarget = If(named, If(ActiveProcessorNames?.FirstOrDefault(), ""))
                Else
                    _lostDeviceInjectionTarget = Nothing
                End If
            Catch ex As Exception
                _lostDeviceInjectionTarget = Nothing
            End Try
        End If
        Return _lostDeviceInjectionTarget
    End Function
#End If

End Class
