Option Strict Off
Option Explicit On

Imports System.IO

' INCA Target Server (TGTSVR) message alert.
' When TGTSVR loses a device (e.g. a camera) it shows a message box and INCA stays blocked until someone presses OK.
' CLEVIR's top-most screens can hide that box, so the 5 s check raises it to the front, shows a sticky toast and
' speaks until it is answered. A lost camera's video cannot be recovered in the running recording (see
' VideoLossLatch); the invalid-video alert that follows asks for a reconnect and Re-Initialize.
Partial Public Class GmResidentClient

    Private Const TargetServerReminderSeconds As Integer = 30
    Private Const TargetServerAlertTitle As String = "INCA Target Server"

    Private Const TargetServerRecentSeconds As Integer = 120

    Private ReadOnly _targetServerMessages As New HashSet(Of IntPtr)
    Private _targetServerToastId As Guid = Guid.Empty
    Private _targetServerLastReminder As DateTime = DateTime.MinValue

    ' Last message seen (kept after it closes) - lets the invalid-video alert explain a camera disconnect.
    Private _targetServerLastTitle As String
    Private _targetServerLastText As String
    Private _targetServerLastSeenAt As DateTime = DateTime.MinValue

    ''' <summary>Called from Handle_5_SecondChecks (UI thread).</summary>
    Private Sub CheckForTargetServerMessage()
        Try
            Dim windows As List(Of ProbedWindow) = TargetServerWindowProbe.FindVisibleWindows(TargetServerProbeProcessName())

            If windows.Count = 0 Then
                If _targetServerMessages.Count > 0 Then
                    _targetServerMessages.Clear()
                    DismissTargetServerAlert()
                    HandleUserMessageLogging("GMRC", "INCA Target Server message closed")
                    If MyIncaInterface.Recording AndAlso VideoAlertText.IsCameraMessage(_targetServerLastTitle, _targetServerLastText) Then
                        StatusNotifier.Toast("Target Server message closed. Video from that camera is lost for this recording - reconnect the camera and Re-Initialize.",
                                             StatusNotifier.ToastKind.Warning, TargetServerAlertTitle, durationMs:=10000, ensureMainOnTop:=False)
                    Else
                        StatusNotifier.Toast("Target Server message closed. INCA can continue.",
                                             StatusNotifier.ToastKind.Success, TargetServerAlertTitle, durationMs:=8000, ensureMainOnTop:=False)
                    End If
                End If
                Return
            End If

            Dim newMessage As ProbedWindow = Nothing
            For Each w In windows
                If _targetServerMessages.Add(w.Handle) Then
                    HandleUserMessageLogging("GMRC", $"INCA Target Server is showing a message: title='{w.Title}' text='{w.Text}'")
                    If newMessage Is Nothing Then newMessage = w
                End If
            Next
            _targetServerMessages.IntersectWith(windows.Select(Function(w) w.Handle))

            Dim now As DateTime = DateTime.Now
            Dim latest As ProbedWindow = If(newMessage, windows(0))
            _targetServerLastTitle = If(latest.Title, "")
            _targetServerLastText = If(latest.Text, "")
            _targetServerLastSeenAt = now
            If newMessage IsNot Nothing Then
                DismissTargetServerAlert()
                Dim body As String = If(newMessage.Text <> "", newMessage.Text, newMessage.Title)
                _targetServerToastId = StatusNotifier.ToastSticky(
                    $"INCA is waiting for an answer to a Target Server message:{vbCrLf}""{body}""{vbCrLf}{vbCrLf}" &
                    "Press OK in the Target Server window - INCA is blocked until it is answered.",
                    TargetServerAlertTitle, StatusNotifier.ToastKind.Error, ensureMainOnTop:=False)
                RemindTargetServerMessage(newMessage.Handle, now)
            ElseIf (now - _targetServerLastReminder).TotalSeconds >= TargetServerReminderSeconds Then
                RemindTargetServerMessage(windows(0).Handle, now)
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckForTargetServerMessage: " & ex.Message)
        End Try
    End Sub

    Private Function IsTargetServerMessageShowing() As Boolean
        Return _targetServerMessages.Count > 0
    End Function

    ''' <summary>True (with the message) when a Target Server message is showing or closed within the last 2 minutes.</summary>
    Private Function TryGetRecentTargetServerMessage(ByRef title As String, ByRef text As String) As Boolean
        If _targetServerLastSeenAt = DateTime.MinValue OrElse
           (DateTime.Now - _targetServerLastSeenAt).TotalSeconds > TargetServerRecentSeconds Then Return False
        title = _targetServerLastTitle
        text = _targetServerLastText
        Return True
    End Function

    Private Sub RemindTargetServerMessage(handle As IntPtr, now As DateTime)
        _targetServerLastReminder = now
        OnVehicleScreen.TopMost = False
        TargetServerWindowProbe.BringToFront(handle)
        SpeakDriverAlert("INCA Target Server message. Press OK.")
    End Sub

    Private Sub DismissTargetServerAlert()
        If _targetServerToastId = Guid.Empty Then Return
        StatusNotifier.ToastDismiss(_targetServerToastId)
        _targetServerToastId = Guid.Empty
    End Sub

    Private Function TargetServerProbeProcessName() As String
#If DEBUG Then
        Dim injected As String = TargetServerInjectionProcessName()
        If injected IsNot Nothing Then Return injected
#End If
        Return TargetServerWindowProbe.TargetServerProcessName
    End Function

#If DEBUG Then
    Private Const TargetServerInjectionFileName As String = "CLEVIR_INJECT_TGTSVR_MESSAGE"

    ''' <summary>
    ''' Debug builds only: while a file named CLEVIR_INJECT_TGTSVR_MESSAGE exists in the application folder, the
    ''' process named on its first non-blank line (default "notepad") is watched instead of TGTSVR, so opening that
    ''' program simulates a Target Server message. Returns Nothing when no injection is active.
    ''' </summary>
    Private Function TargetServerInjectionProcessName() As String
        Try
            Dim injectionFile As String = Path.Combine(My.Application.Info.DirectoryPath, TargetServerInjectionFileName)
            If Not File.Exists(injectionFile) Then Return Nothing
            Dim named As String = File.ReadLines(injectionFile).Select(Function(l) l.Trim()).FirstOrDefault(Function(l) l <> "")
            Return If(named, "notepad")
        Catch
            Return Nothing
        End Try
    End Function
#End If

End Class
