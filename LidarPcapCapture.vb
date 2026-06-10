
Option Strict Off
Imports System.IO
Imports System.Linq
Imports System.Threading
Imports SharpPcap
Imports SharpPcap.LibPcap
Imports PacketDotNet

''' <summary>
''' ✅ MIGRATED: LiDAR PCAP Capture Module (SharpPcap)
''' Provides wrapper functions for multi-device LiDAR capture
''' Actual device instances are managed in Module1.LidarDevices
''' </summary>
Module LidarPcapCapture

    ''' <summary>
    ''' Starts LiDAR capture for all enabled devices
    ''' Legacy wrapper for single-device compatibility
    ''' </summary>
    Public Sub StartLidarCapture()
        StartLidarCaptureMulti()
    End Sub

    ''' <summary>
    ''' Starts capture for all configured LiDAR devices.
    ''' Devices that share the same adapter GUID are routed through a single
    ''' SharedNicCapture instance to avoid fighting over the same Npcap handle.
    ''' Devices with a unique GUID each get their own handle (legacy path).
    ''' </summary>
    Private Sub StartLidarCaptureMulti()
        Try
            If Not LidarCaptureEnabled OrElse LidarDevices.Count = 0 Then
                HandleUserMessageLogging("GMRC", "StartLidarCaptureMulti: No LiDAR devices configured")
                LidarCaptureStarted = False
                Return
            End If

            ' ----------------------------------------------------------------
            ' Build sequence / filename components
            ' ----------------------------------------------------------------
            Dim currentActiveSequence As String = GetCurrentActiveSequence()
            Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)
            Dim baseName As String = System.Text.RegularExpressions.Regex.Replace(
                Path.GetFileNameWithoutExtension(currentActiveSequence), "_\d{2}$", "")

            ' ----------------------------------------------------------------
            ' Group devices by normalised adapter GUID
            ' ----------------------------------------------------------------
            Dim groups = LidarDevices.
                GroupBy(Function(d) d.LidarAdapterGuid.Replace("{", "").Replace("}", "").ToUpper()).
                ToList()

            Dim successCount As Integer = 0

            For Each grp In groups
                Dim guidKey As String = grp.Key
                Dim groupDevices = grp.ToList()

                If groupDevices.Count = 1 Then
                    ' --------------------------------------------------------
                    ' SOLO path — unique NIC, use existing per-device capture
                    ' --------------------------------------------------------
                    Dim device = groupDevices(0)
                    Dim idx = LidarDevices.IndexOf(device)
                    Dim pcapFilename As String = Path.Combine(FinalPathToSaveData,
                        $"{baseName}_{currentSeq:D2}_LiDAR{idx + 1}.pcap")
                    Try
                        device.StartCapture(pcapFilename, currentSeq)
                        If device.IsCapturing Then
                            successCount += 1
                            HandleUserMessageLogging("GMRC",
                                $"StartLidarCaptureMulti: Started {device.DeviceId} (solo NIC)")
                        Else
                            HandleUserMessageLogging("GMRC",
                                $"StartLidarCaptureMulti: {device.DeviceId} IsCapturing=False after start")
                        End If
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC",
                            $"StartLidarCaptureMulti: Failed {device.DeviceId} — {ex.Message}")
                    End Try

                Else
                    ' --------------------------------------------------------
                    ' SHARED path — multiple devices on the same NIC
                    ' --------------------------------------------------------
                    Dim filenames As New List(Of String)
                    For Each device In groupDevices
                        Dim idx = LidarDevices.IndexOf(device)
                        filenames.Add(Path.Combine(FinalPathToSaveData,
                            $"{baseName}_{currentSeq:D2}_LiDAR{idx + 1}.pcap"))
                    Next

                    Try
                        Dim sharedNic As New SharedNicCapture(guidKey, groupDevices)
                        sharedNic.StartCapture(filenames, currentSeq)

                        ' Count devices that started
                        Dim started As Integer = groupDevices.Where(Function(d) d.IsCapturing).Count()
                        If started > 0 Then
                            SharedNicCaptures(guidKey) = sharedNic
                            successCount += started
                            HandleUserMessageLogging("GMRC",
                                $"StartLidarCaptureMulti: SharedNicCapture started for GUID {guidKey} " &
                                $"({started}/{groupDevices.Count} devices)")
                        Else
                            HandleUserMessageLogging("GMRC",
                                $"StartLidarCaptureMulti: SharedNicCapture for GUID {guidKey} — no devices started")
                        End If
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC",
                            $"StartLidarCaptureMulti: SharedNicCapture failed for GUID {guidKey} — {ex.Message}")
                    End Try
                End If
            Next

            If successCount > 0 Then
                LidarCaptureStarted = True
                HandleUserMessageLogging("GMRC",
                    $"StartLidarCaptureMulti: {successCount}/{LidarDevices.Count} device(s) started")
            Else
                LidarCaptureStarted = False
                HandleUserMessageLogging("GMRC",
                    $"StartLidarCaptureMulti: FAILED — no devices started")
            End If

        Catch ex As Exception
            LidarCaptureStarted = False
            HandleUserMessageLogging("GMRC", $"StartLidarCaptureMulti: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Injects event marker into all active captures
    ''' </summary>
    Public Sub InjectEventMarker(eventType As String, message As String, currentSeq As Integer)
        For Each device In LidarDevices
            If device.IsCapturing Then
                device.InjectEventMarker(eventType, message, currentSeq)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Stops LiDAR capture for all devices
    ''' </summary>
    Public Sub StopLidarCapture()
        StopLidarCaptureMulti()
    End Sub

    ''' <summary>
    ''' Stops capture for all LiDAR devices.
    ''' Shared-NIC groups are stopped via their SharedNicCapture instance;
    ''' solo devices are stopped directly.
    ''' </summary>
    Private Sub StopLidarCaptureMulti()
        Try
            If LidarDevices.Count = 0 Then Return

            ' ----------------------------------------------------------------
            ' Stop shared captures (covers all devices in each group)
            ' ----------------------------------------------------------------
            For Each kvp In SharedNicCaptures.ToList()
                Try
                    kvp.Value.StopCapture()
                    HandleUserMessageLogging("GMRC",
                        $"StopLidarCaptureMulti: SharedNicCapture stopped (GUID {kvp.Key})")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"StopLidarCaptureMulti: SharedNicCapture stop error (GUID {kvp.Key}): {ex.Message}")
                End Try
            Next
            SharedNicCaptures.Clear()

            ' ----------------------------------------------------------------
            ' Stop any solo devices still capturing
            ' ----------------------------------------------------------------
            For Each device In LidarDevices
                Try
                    If device.IsCapturing Then
                        device.StopCapture()
                        HandleUserMessageLogging("GMRC",
                            $"StopLidarCaptureMulti: Stopped {device.DeviceId}")
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"StopLidarCaptureMulti: Error stopping {device.DeviceId} — {ex.Message}")
                End Try
            Next

            LidarCaptureStarted = False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"StopLidarCaptureMulti: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Restarts capture for a new sequence (file rotation).
    ''' For shared-NIC groups the NIC handle is kept alive; only the dump files
    ''' are rotated via RotateSequence.  Solo devices stop/start as before.
    ''' </summary>
    Public Sub RestartLidarCaptureForNewSequence()
        Try
            If Not LidarCaptureEnabled Then Return

            Dim currentActiveSequence As String = GetCurrentActiveSequence()
            Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)
            Dim baseName As String = System.Text.RegularExpressions.Regex.Replace(
                Path.GetFileNameWithoutExtension(currentActiveSequence), "_\d{2}$", "")

            HandleUserMessageLogging("GMRC",
                $"RestartLidarCaptureForNewSequence: Rotating to sequence {currentSeq:D2}...")

            ' ----------------------------------------------------------------
            ' Shared-NIC groups: rotate dump files without dropping the NIC handle
            ' ----------------------------------------------------------------
            For Each kvp In SharedNicCaptures
                Dim rotGuidKey = kvp.Key
                Dim sharedNic = kvp.Value

                ' Rebuild filename list for this group
                Dim groupFilenames As New List(Of String)
                For Each device In LidarDevices
                    If device.LidarAdapterGuid.Replace("{", "").Replace("}", "").ToUpper() = rotGuidKey Then
                        Dim idx = LidarDevices.IndexOf(device)
                        groupFilenames.Add(Path.Combine(FinalPathToSaveData,
                            $"{baseName}_{currentSeq:D2}_LiDAR{idx + 1}.pcap"))
                    End If
                Next

                Try
                    sharedNic.RotateSequence(groupFilenames, currentSeq)
                    HandleUserMessageLogging("GMRC",
                        $"RestartLidarCaptureForNewSequence: SharedNicCapture rotated (GUID {rotGuidKey})")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"RestartLidarCaptureForNewSequence: Rotate error (GUID {rotGuidKey}): {ex.Message}")
                End Try
            Next

            ' ----------------------------------------------------------------
            ' Solo devices: full stop → start cycle
            ' ----------------------------------------------------------------
            For Each device In LidarDevices
                Dim guidKey = device.LidarAdapterGuid.Replace("{", "").Replace("}", "").ToUpper()
                If SharedNicCaptures.ContainsKey(guidKey) Then Continue For  ' Already handled above

                Dim idx = LidarDevices.IndexOf(device)
                Dim pcapFilename As String = Path.Combine(FinalPathToSaveData,
                    $"{baseName}_{currentSeq:D2}_LiDAR{idx + 1}.pcap")
                Try
                    If device.IsCapturing Then device.StopCapture()
                    Threading.Thread.Sleep(200)
                    device.StartCapture(pcapFilename, currentSeq)
                    HandleUserMessageLogging("GMRC",
                        $"RestartLidarCaptureForNewSequence: Restarted {device.DeviceId} (solo NIC)")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"RestartLidarCaptureForNewSequence: Error restarting {device.DeviceId} — {ex.Message}")
                End Try
            Next

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"RestartLidarCaptureForNewSequence: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ UPDATED: Post-processes PCAP file using SharpPcap offline reading
    ''' </summary>
    Public Sub ProcessLidarPcapFile(pcapFilePath As String)
        Try
            If Not File.Exists(pcapFilePath) Then
                HandleUserMessageLogging("GMRC", $"ProcessLidarPcapFile: File not found: {pcapFilePath}")
                Return
            End If

            Dim eventLogPath As String = Path.ChangeExtension(pcapFilePath, ".lidar_events.txt")
            Dim eventLogExists As Boolean = File.Exists(eventLogPath)

            HandleUserMessageLogging("GMRC", $"ProcessLidarPcapFile: Analyzing {Path.GetFileName(pcapFilePath)}...")

            ' Open PCAP file for offline reading (SharpPcap)
            Dim offlineDevice As New CaptureFileReaderDevice(pcapFilePath)
            offlineDevice.Open()

            Dim frameNumber As Long = 0
            Dim packetCount As Integer = 0
            Dim totalBytes As Long = 0
            Dim markerCount As Integer = 0
            Dim firstTimestamp As DateTime? = Nothing
            Dim lastTimestamp As DateTime? = Nothing

            ' If event log doesn't exist, create it during post-processing
            Dim postProcessLogger As LidarEventLogger = Nothing
            If Not eventLogExists Then
                postProcessLogger = New LidarEventLogger(pcapFilePath)
                HandleUserMessageLogging("GMRC", "  Generating event log during post-processing...")
            End If

            ' Read all packets
            Dim packetStatus As GetPacketStatus
            Dim packetCapture As Object = Nothing  ' Will be PacketCapture (ref struct)

            Do
                packetStatus = offlineDevice.GetNextPacket(packetCapture)
                If packetStatus <> GetPacketStatus.PacketRead Then
                    Exit Do
                End If

                ' Use reflection to get RawCapture from PacketCapture
                Dim getPacketMethod = packetCapture.GetType().GetMethod("GetPacket")
                Dim rawPacket As RawCapture = DirectCast(getPacketMethod.Invoke(packetCapture, Nothing), RawCapture)

                frameNumber += 1
                packetCount += 1
                totalBytes += rawPacket.Data.Length

                ' Parse packet using PacketDotNet
                Dim packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data)
                Dim ethPacket = TryCast(packet, EthernetPacket)

                If ethPacket IsNot Nothing Then
                    Dim ipPacket = TryCast(ethPacket.PayloadPacket, IPv4Packet)
                    If ipPacket IsNot Nothing Then
                        Dim udpPacket = TryCast(ipPacket.PayloadPacket, UdpPacket)

                        ' Check if this is an event marker packet
                        If udpPacket IsNot Nothing AndAlso
                           udpPacket.DestinationPort = LidarDevice.MarkerDestPort AndAlso
                           udpPacket.SourcePort = LidarDevice.MarkerSourcePort Then

                            markerCount += 1
                            Dim markerData As String = System.Text.Encoding.UTF8.GetString(udpPacket.PayloadData)

                            ' Parse marker data
                            Dim parts = markerData.Split("|"c)
                            If parts.Length >= 3 Then
                                Dim eventType As String = parts(0)
                                Dim message As String = parts(1)
                                Dim seqNum As Integer = Integer.Parse(parts(2))

                                ' Log if creating file during post-processing
                                postProcessLogger?.LogEvent(frameNumber, rawPacket.Timeval.Date,
                                                        eventType, message, seqNum)

                                HandleUserMessageLogging("GMRC",
                                $"  Frame {frameNumber}: {eventType} - {message}")
                            Else
                                HandleUserMessageLogging("GMRC",
                                $"  Frame {frameNumber}: {markerData}")
                            End If
                        End If
                    End If
                End If

                If firstTimestamp Is Nothing Then
                    firstTimestamp = rawPacket.Timeval.Date
                End If
                lastTimestamp = rawPacket.Timeval.Date
            Loop

            offlineDevice.Close()
            postProcessLogger?.Close()

            ' Calculate and log statistics
            If firstTimestamp.HasValue AndAlso lastTimestamp.HasValue Then
                Dim duration As TimeSpan = lastTimestamp.Value - firstTimestamp.Value
                Dim avgRate As Double = If(duration.TotalSeconds > 0, packetCount / duration.TotalSeconds, 0)

                HandleUserMessageLogging("GMRC", $"ProcessLidarPcapFile: Statistics for {Path.GetFileName(pcapFilePath)}:")
                HandleUserMessageLogging("GMRC", $"  Total frames: {frameNumber:N0}")
                HandleUserMessageLogging("GMRC", $"  LiDAR packets: {packetCount - markerCount:N0}")
                HandleUserMessageLogging("GMRC", $"  Event markers: {markerCount:N0}")
                HandleUserMessageLogging("GMRC", $"  Total bytes: {totalBytes:N0}")
                HandleUserMessageLogging("GMRC", $"  Duration: {duration.TotalSeconds:F2} seconds")
                HandleUserMessageLogging("GMRC", $"  Avg packet rate: {avgRate:F2} pkt/sec")
                HandleUserMessageLogging("GMRC", $"  Event log: {If(eventLogExists, "Validated", "Generated")} - {eventLogPath}")

                ' Write summary to INCA event log
                MyIncaInterface?.WriteEventComment(
                $"LiDAR PCAP: {frameNumber:N0} frames, {markerCount} events, {totalBytes:N0} bytes, " &
                $"{duration.TotalSeconds:F2}s, {avgRate:F2} pkt/s", True)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ProcessLidarPcapFile: {ex.Message}")
        End Try
    End Sub

End Module
