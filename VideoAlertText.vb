Option Strict On
Option Explicit On

''' <summary>
''' Wording for the invalid-video alert. Lost video cannot be recovered within a recording: ETAS localizes the camera
''' frames before writing them to the session folder, and frames after a loss would not line up with the MF4 data.
''' So even if the camera comes back the driver is told to Re-Initialize. When the INCA Target Server recently
''' reported a camera problem (e.g. "REAR: VideoCamera Add-On - Camera Connection"), the driver is first told to
''' reconnect the camera, since Re-Initialize cannot restore video from a camera that is still unplugged.
''' </summary>
Friend Module VideoAlertText

    Friend Const UnidentifiedDevice As String = "device not yet identified"

    Private Const VideoLostText As String =
        "Video from this camera is lost for the rest of this recording and will not re-synchronize with the data, even if the camera comes back. "

    Private Const ReinitializeText As String =
        "Re-Initialize NOW (INCA will be restarted, approx 3 minutes) to restore video recording."

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

        If Not recentTargetServer Then
            Return $"INVALID VIDEO ALERT! ({device}) " & VideoLostText & ReinitializeText
        End If

        Dim reported As String = If(String.IsNullOrWhiteSpace(targetServerText), targetServerTitle, targetServerText).Trim()
        Dim action As String = If(IsCameraMessage(targetServerTitle, targetServerText),
                                  "Reconnect the camera, then ",
                                  "")
        Return $"INVALID VIDEO ALERT! ({device}) INCA Target Server reported: ""{reported}"" " &
               VideoLostText & action & ReinitializeText
    End Function

End Module
