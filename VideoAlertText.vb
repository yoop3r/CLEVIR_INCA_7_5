Option Strict On
Option Explicit On

''' <summary>
''' Wording for the invalid-video alert. When the INCA Target Server recently reported a device problem (typically
''' "REAR: VideoCamera Add-On - Camera Connection" / "The camera appears to be disconnected"), the frozen video is
''' that disconnect, and INCA normally recovers on its own once the camera is back - so the driver is pointed at the
''' camera first and Re-Initialize becomes the fallback.
''' </summary>
Friend Module VideoAlertText

    Friend Const UnidentifiedDevice As String = "device not yet identified"

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
            Return $"INVALID VIDEO ALERT! ({device}) CLEVIR/INCA should be re-initialized (INCA will be restarted) to insure video recording capability. This will take approx 3 minutes."
        End If

        Dim reported As String = If(String.IsNullOrWhiteSpace(targetServerText), targetServerTitle, targetServerText).Trim()
        Dim action As String = If(IsCameraMessage(targetServerTitle, targetServerText),
                                  "Check that the camera is connected (reconnect it if needed). ",
                                  "")
        Return $"INVALID VIDEO ALERT! ({device}) INCA Target Server recently reported: ""{reported}"" " &
               action &
               "Video normally resumes on its own within a minute - this alert will show RESOLVED when it does. " &
               "Re-Initialize (INCA will be restarted, approx 3 minutes) only if video does not resume."
    End Function

End Module
