Option Strict Off
Option Explicit On

Imports System.IO

' LiDAR loss-of-communication alert.
' While LiDAR capture runs, each enabled device is watched for silence. A loss is spoken ("Lost communication with
' LiDAR FRONT"), shown as a sticky toast and logged, and repeated until packets return; recovery is spoken and toasted.
' All speech goes through DriverVoice, so with two devices down FRONT and REAR are announced one after the other.
Partial Public Class GmResidentClient

    Private Const LidarLostAfterSeconds As Integer = 15
    Private Const LidarAlertRepeatSeconds As Integer = 30
    Private Const LidarAlertTitle As String = "LiDAR"

    Private ReadOnly _lidarAlertLock As New Object()
    Private ReadOnly _lidarWatches As New Dictionary(Of LidarDevice, LidarLinkWatch)
    Private ReadOnly _lidarLostToasts As New Dictionary(Of LidarDevice, Guid)

    ''' <summary>
    ''' Called from HandleKillerAudioAlerts (killer thread) about once a second.
    ''' <paramref name="audioAllowed"/> is False while the driver has chosen "Ignore All Alerts"; toasts and logs continue.
    ''' </summary>
    Private Sub CheckLidarLinks(audioAllowed As Boolean)
        Try
            Dim devices As LidarDevice() = If(LidarCaptureEnabled AndAlso LidarDevices IsNot Nothing, LidarDevices.ToArray(), Array.Empty(Of LidarDevice)())
            Dim captureRunning As Boolean = LidarCaptureStarted
            Dim now As DateTime = DateTime.Now

            SyncLock _lidarAlertLock
                For Each gone In _lidarWatches.Keys.Except(devices).ToList()
                    _lidarWatches.Remove(gone)
                    DismissLidarLostToast(gone)
                Next

                For Each lidar In devices
                    If lidar Is Nothing Then Continue For
                    Dim watch As LidarLinkWatch = Nothing
                    If Not _lidarWatches.TryGetValue(lidar, watch) Then
                        watch = New LidarLinkWatch(TimeSpan.FromSeconds(LidarLostAfterSeconds), TimeSpan.FromSeconds(LidarAlertRepeatSeconds))
                        _lidarWatches(lidar) = watch
                    End If

                    Dim monitoring As Boolean = captureRunning AndAlso lidar.Enabled
                    Dim lastPacket As DateTime? = lidar.LastPacketTimestamp
                    Dim captureStartedAt As DateTime? = lidar.CaptureStartedAt
#If DEBUG Then
                    ApplyLidarLossInjection(lidar, now, monitoring, lastPacket, captureStartedAt)
#End If
                    Dim linkEvent As LidarLinkEvent = watch.Update(monitoring, lastPacket, captureStartedAt, lidar.PacketCount, lidar.DroppedPackets, now)
                    AnnounceLidarLinkEvent(lidar, watch, linkEvent, audioAllowed, now)
                Next
            End SyncLock
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckLidarLinks: " & ex.Message)
        End Try
    End Sub

    Private Sub AnnounceLidarLinkEvent(lidar As LidarDevice, watch As LidarLinkWatch, linkEvent As LidarLinkEvent, audioAllowed As Boolean, now As DateTime)
        Dim name As String = lidar.SpokenName
        Select Case linkEvent
            Case LidarLinkEvent.Lost
                Dim silentFor As Double = (now - watch.SilentSince).TotalSeconds
                HandleUserMessageLogging("GMRC", $"[{lidar.DisplayName}] LiDAR communication lost - no packets for {silentFor:F0}s. {lidar.GetLinkDiagnostics()}")
                DismissLidarLostToast(lidar)
                _lidarLostToasts(lidar) = StatusNotifier.ToastSticky(
                    $"Lost communication with {name}.{vbCrLf}No LiDAR data is being recorded from this device.",
                    LidarAlertTitle, StatusNotifier.ToastKind.Error, ensureMainOnTop:=False)
                If audioAllowed Then DriverVoice.Speak($"Lost communication with {name}")

            Case LidarLinkEvent.StillLost
                If audioAllowed Then DriverVoice.Speak($"Lost communication with {name}")

            Case LidarLinkEvent.Restored
                HandleUserMessageLogging("GMRC", $"[{lidar.DisplayName}] LiDAR communication restored after {(now - watch.SilentSince).TotalSeconds:F0}s")
                DismissLidarLostToast(lidar)
                StatusNotifier.Toast($"{name} communication restored.", StatusNotifier.ToastKind.Success,
                                     LidarAlertTitle, durationMs:=8000, ensureMainOnTop:=False)
                If audioAllowed Then DriverVoice.Speak($"{name} communication restored")

            Case LidarLinkEvent.PacketLoss
                HandleUserMessageLogging("GMRC", $"[{lidar.DisplayName}] LiDAR critical packet loss - pkts={lidar.PacketCount:N0}, dropped={lidar.DroppedPackets:N0}")
                StatusNotifier.Toast($"{name}: critical packet loss.", StatusNotifier.ToastKind.Warning,
                                     LidarAlertTitle, durationMs:=8000, ensureMainOnTop:=False)
                If audioAllowed Then DriverVoice.Speak($"{name} critical packet loss")
        End Select
    End Sub

    Private Sub DismissLidarLostToast(lidar As LidarDevice)
        Dim id As Guid
        If _lidarLostToasts.TryGetValue(lidar, id) Then
            _lidarLostToasts.Remove(lidar)
            StatusNotifier.ToastDismiss(id)
        End If
    End Sub

    ''' <summary>Called at shutdown: removes any outstanding LiDAR loss toasts.</summary>
    Private Sub DismissLidarLinkAlerts()
        SyncLock _lidarAlertLock
            For Each lidar In _lidarLostToasts.Keys.ToList()
                DismissLidarLostToast(lidar)
            Next
            _lidarWatches.Clear()
        End SyncLock
    End Sub

#If DEBUG Then
    Private Const LidarLossInjectionFileName As String = "CLEVIR_INJECT_LIDAR_LOSS"
    Private ReadOnly _lidarInjectedSince As New Dictionary(Of LidarDevice, DateTime)

    ''' <summary>
    ''' Debug builds only: while a file named CLEVIR_INJECT_LIDAR_LOSS exists in the application folder, the LiDARs named
    ''' on its lines (DeviceId or orientation, e.g. FRONT; an empty file means all) are treated as capturing but silent,
    ''' even when no capture is running. Deleting the file simulates packets arriving again.
    ''' </summary>
    Private Sub ApplyLidarLossInjection(lidar As LidarDevice, now As DateTime, ByRef monitoring As Boolean,
                                        ByRef lastPacket As DateTime?, ByRef captureStartedAt As DateTime?)
        Dim injected As Boolean = False
        Try
            Dim injectionFile As String = Path.Combine(My.Application.Info.DirectoryPath, LidarLossInjectionFileName)
            If File.Exists(injectionFile) AndAlso lidar.Enabled Then
                Dim names = File.ReadLines(injectionFile).Select(Function(l) l.Trim()).Where(Function(l) l <> "").ToList()
                injected = names.Count = 0 OrElse names.Any(Function(n) String.Equals(n, lidar.DeviceId, StringComparison.OrdinalIgnoreCase) OrElse
                                                                       String.Equals(n, lidar.Orientation?.Trim(), StringComparison.OrdinalIgnoreCase))
            End If
        Catch
            injected = False
        End Try

        Dim since As DateTime
        If injected Then
            If Not _lidarInjectedSince.TryGetValue(lidar, since) Then
                since = now
                _lidarInjectedSince(lidar) = since
                HandleUserMessageLogging("GMRC", $"[{lidar.DisplayName}] DEBUG: LiDAR loss injected")
            End If
            monitoring = True
            lastPacket = since
            captureStartedAt = Nothing
        ElseIf _lidarInjectedSince.TryGetValue(lidar, since) Then
            _lidarInjectedSince.Remove(lidar)
            HandleUserMessageLogging("GMRC", $"[{lidar.DisplayName}] DEBUG: LiDAR loss injection removed")
            monitoring = True
            lastPacket = now
        End If
    End Sub
#End If

End Class
