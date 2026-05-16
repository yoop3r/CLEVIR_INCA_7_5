
Option Strict Off
Imports System.IO
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
    ''' Starts capture for all configured LiDAR devices
    ''' </summary>
    Private Sub StartLidarCaptureMulti()
        Try
            If Not LidarCaptureEnabled OrElse LidarDevices.Count = 0 Then
                HandleUserMessageLogging("GMRC", "StartLidarCaptureMulti: No LiDAR devices configured")
                LidarCaptureStarted = False
                Return
            End If

            ' Get current sequence information
            Dim currentActiveSequence As String = GetCurrentActiveSequence()
            Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)
            Dim baseFileName As String = Path.GetFileNameWithoutExtension(currentActiveSequence)

            ' Use Regex to remove ONLY the trailing sequence number
            Dim baseName As String = System.Text.RegularExpressions.Regex.Replace(
                baseFileName,
                "_\d{2}$",  ' Match "_##" at end of string only
                ""
            )

            Dim successCount As Integer = 0

            ' Start each device
            For i As Integer = 0 To LidarDevices.Count - 1
                Dim device = LidarDevices(i)
                Try
                    ' Build device-specific filename
                    Dim pcapFilename As String = Path.Combine(FinalPathToSaveData,
                    $"{baseName}_{currentSeq:D2}_LiDAR{i + 1}.pcap")

                    device.StartCapture(pcapFilename, currentSeq)

                    ' Verify the device actually started
                    If device.IsCapturing Then
                        successCount += 1
                        HandleUserMessageLogging("GMRC",
                        $"StartLidarCaptureMulti: Started {device.DeviceId} - {device.LidarIpAddress}")
                    Else
                        HandleUserMessageLogging("GMRC",
                        $"StartLidarCaptureMulti: {device.DeviceId} StartCapture returned but IsCapturing=False")
                    End If

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                    $"StartLidarCaptureMulti: Failed to start {device.DeviceId} - {ex.Message}")
                End Try
            Next

            ' Only set global flag if at least one device started successfully
            If successCount > 0 Then
                LidarCaptureStarted = True
                HandleUserMessageLogging("GMRC",
                $"StartLidarCaptureMulti: {successCount} of {LidarDevices.Count} device(s) started successfully")
            Else
                LidarCaptureStarted = False
                HandleUserMessageLogging("GMRC",
                $"StartLidarCaptureMulti: FAILED - No devices started (0/{LidarDevices.Count})")
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
    ''' Stops capture for all LiDAR devices
    ''' </summary>
    Private Sub StopLidarCaptureMulti()
        Try
            If LidarDevices.Count = 0 Then Return

            For Each device In LidarDevices
                Try
                    If device.IsCapturing Then
                        device.StopCapture()
                        HandleUserMessageLogging("GMRC",
                                                 $"StopLidarCaptureMulti: Stopped {device.DeviceId}")
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                                             $"StopLidarCaptureMulti: Error stopping {device.DeviceId} - {ex.Message}")
                End Try
            Next

            LidarCaptureStarted = False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"StopLidarCaptureMulti: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Restarts capture for new sequence (file rotation)
    ''' </summary>
    Public Sub RestartLidarCaptureForNewSequence()
        Try
            If Not LidarCaptureEnabled Then Return

            HandleUserMessageLogging("GMRC", "RestartLidarCaptureForNewSequence: Restarting all devices...")

            StopLidarCaptureMulti()
            Threading.Thread.Sleep(500)
            StartLidarCaptureMulti()

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
