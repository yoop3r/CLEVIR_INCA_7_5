Option Strict On
Option Explicit On

''' <summary>
''' Wording for the invalid-video alert and video toasts. Lost video cannot be recovered within a recording: ETAS
''' localizes the camera frames before writing them to the session folder, and frames after a loss would not line up
''' with the MF4 data. A new recording restores it: Stop Record, reconnect the camera, Start Record. Re-Initialize is
''' the fallback when the video does not return. When the INCA Target Server recently reported a camera problem (e.g.
''' "REAR: VideoCamera Add-On - Camera Connection"), the alert quotes it.
''' </summary>
Friend Module VideoAlertText

    Friend Const UnidentifiedDevice As String = "device not yet identified"

    Private Const VideoLostText As String =
        "Video from this camera is lost for the rest of this recording and will not re-synchronize with the data, even if the camera comes back. "

    Private Const ReinitializeFallbackText As String =
        "If video does not return, choose Re-Initialize NOW (INCA will be restarted, approx 3 minutes)."

    ''' <summary>Fourth button of the invalid-video alert, e.g. "Continue Without REAR Video".</summary>
    Friend Function ContinueWithoutButton(deviceList As String) As String
        Return $"Continue Without {deviceList.Replace(",", " + ")} Video"
    End Function

    ''' <summary>"REAR,FRONT" -> "REAR and FRONT" for prose.</summary>
    Friend Function SpokenList(deviceList As String) As String
        Dim names As String() = deviceList.Split(","c).Select(Function(d) d.Trim()).Where(Function(d) d <> "").ToArray()
        If names.Length <= 1 Then Return String.Join("", names)
        Return String.Join(", ", names.Take(names.Length - 1)) & " and " & names.Last()
    End Function

    ''' <summary>The quick fix first (a new recording), Re-Initialize as the fallback.</summary>
    Friend Function RestoreSteps(deviceList As String) As String
        If String.IsNullOrWhiteSpace(deviceList) Then
            Return "To restore video: Stop Record, reconnect the camera, then Start Record. " & ReinitializeFallbackText
        End If
        Dim names As String = SpokenList(deviceList)
        Dim cameras As String = If(deviceList.Contains(","c), "cameras", "camera")
        Return $"To restore {names} video: Stop Record, reconnect the {names} {cameras}, then Start Record. " & ReinitializeFallbackText
    End Function

    Friend Function ContinuingWithoutToast(deviceList As String) As String
        Dim names As String = SpokenList(deviceList)
        Return $"Continuing without {names} video. Video GO/NOGO stays red; all other alerts stay on. " &
               $"To restore it later: Stop Record, reconnect the {names} {If(deviceList.Contains(","c), "cameras", "camera")}, then Start Record."
    End Function

    Friend Function TargetServerClosedToast(targetServerTitle As String) As String
        Dim device As String = DeviceFromTargetServerTitle(targetServerTitle)
        Dim what As String = If(device = "", "Video from that camera", device & " video")
        Return $"Target Server message closed. {what} is lost for this recording. " &
               "To restore it: Stop Record, reconnect the camera, then Start Record (Re-Initialize if video does not return). " &
               "Or continue without it from the video alert."
    End Function

    ''' <summary>"REAR: VideoCamera Add-On - Camera Connection" -> "REAR"; "" when the title has no device prefix.</summary>
    Friend Function DeviceFromTargetServerTitle(title As String) As String
        If String.IsNullOrWhiteSpace(title) Then Return ""
        Dim colon As Integer = title.IndexOf(":"c)
        If colon <= 0 Then Return ""
        Dim prefix As String = title.Substring(0, colon).Trim()
        If prefix.Length = 0 OrElse prefix.Length > 30 Then Return ""
        Return prefix
    End Function

    Friend Function IsCameraMessage(title As String, text As String) As Boolean
        Return (If(title, "") & " " & If(text, "")).IndexOf("camera", StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    ''' <summary>
    ''' Invalid-video alert text. Pass targetServerTitle/Text = Nothing when no Target Server message was seen recently.
    ''' </summary>
    Friend Function InvalidVideoMessage(frozenDeviceNames As String, targetServerTitle As String, targetServerText As String) As String
        Dim recentTargetServer As Boolean = targetServerTitle IsNot Nothing OrElse targetServerText IsNot Nothing

        Dim device As String = frozenDeviceNames
        If String.IsNullOrWhiteSpace(device) AndAlso recentTargetServer Then device = DeviceFromTargetServerTitle(targetServerTitle)
        If String.IsNullOrWhiteSpace(device) Then device = UnidentifiedDevice

        Dim body As String
        If Not recentTargetServer Then
            body = $"INVALID VIDEO ALERT! ({device}) " & VideoLostText
        Else
            Dim reported As String = If(String.IsNullOrWhiteSpace(targetServerText), targetServerTitle, targetServerText).Trim()
            body = $"INVALID VIDEO ALERT! ({device}) INCA Target Server reported: ""{reported}"" " & VideoLostText
        End If
        body &= RestoreSteps(If(device = UnidentifiedDevice, "", device))

        ' The continue-without button is only offered for cameras identified by their frozen video cells.
        If String.IsNullOrWhiteSpace(frozenDeviceNames) Then Return body
        Return body & $" Or, if the camera cannot be restored, choose ""{ContinueWithoutButton(frozenDeviceNames)}"" to keep recording vehicle data without it."
    End Function

End Module
