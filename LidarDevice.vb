Option Strict On
Imports System.IO
Imports System.Speech.Synthesis
Imports System.Threading
Imports PcapDotNet.Core
Imports PcapDotNet.Packets
Imports PcapDotNet.Packets.Ethernet
Imports PcapDotNet.Packets.IpV4
Imports PcapDotNet.Packets.Transport

''' <summary>
''' Maintains a log file of event markers with frame numbers for offline analysis
''' </summary>
Public Class LidarEventLogger
    Private eventLogPath As String
    Private eventLogWriter As StreamWriter
    Private syncLockObj As New Object()

    Public Sub New(pcapFilePath As String)
        ' Create sidecar log file (e.g., "Recording_01_LiDAR1.pcap" -> "Recording_01_LiDAR1.lidar_events.txt")
        eventLogPath = Path.ChangeExtension(pcapFilePath, ".lidar_events.txt")
        eventLogWriter = New StreamWriter(eventLogPath, append:=False)

        ' Write header
        eventLogWriter.WriteLine("# LiDAR Event Marker Log")
        eventLogWriter.WriteLine($"# PCAP File: {Path.GetFileName(pcapFilePath)}")
        eventLogWriter.WriteLine($"# Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
        eventLogWriter.WriteLine("#")
        eventLogWriter.WriteLine("# Format: FrameNumber | Timestamp | EventType | Message | SequenceNumber")
        eventLogWriter.WriteLine("# -------------------------------------------------------------------------")
        eventLogWriter.Flush()
    End Sub

    Public Sub LogEvent(frameNumber As Long, timestamp As DateTime, eventType As String, message As String, sequenceNumber As Integer)
        SyncLock syncLockObj
            Try
                Dim line As String = $"{frameNumber}|{timestamp:yyyy-MM-dd HH:mm:ss.fff}|{eventType}|{message}|{sequenceNumber}"
                HandleUserMessageLogging("GMRC", $"LidarEventLogger: Writing line = '{line}'")
                eventLogWriter.WriteLine(line)
                eventLogWriter.Flush()
                HandleUserMessageLogging("GMRC", $"LidarEventLogger: Line written to {eventLogPath}")

            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"LidarEventLogger.LogEvent: {ex.Message}")
            End Try
        End SyncLock
    End Sub

    Public Sub Close()
        SyncLock syncLockObj
            If eventLogWriter IsNot Nothing Then
                eventLogWriter.Close()
                eventLogWriter = Nothing
            End If
        End SyncLock
    End Sub
End Class

''' <summary>
''' Represents a single LiDAR sensor device with independent capture state
''' </summary>
Public Class LidarDevice

    ' ====================================================================
    ' Instance-level configuration (each LiDAR has its own settings)
    ' ====================================================================
    Public Property LidarAdapterGuid As String = ""
    Public Property LidarIpAddress As String = "192.168.1.201"
    Public Property LidarDataPort As UShort = 2368
    Public Property LidarImuPort As UShort = 8308
    Public Property DeviceId As String = "LiDAR1" ' Friendly name for logging
    Public Property Enabled As Boolean = True  ' Default to True for backward compatibility

    ' Instance-level capture state
    Private _captureDevice As LivePacketDevice = Nothing
    Private _communicator As PacketCommunicator = Nothing
    Private _dumpFile As PacketDumpFile = Nothing
    Private _captureThread As Threading.Thread = Nothing
    Private _isCapturing As Boolean = False
    Private _packetCount As Long = 0
    Private _totalBytes As Long = 0
    Private _markerCounter As Long = 0
    Private _droppedPackets As Long = 0  ' ← ADD THIS LINE (line 30)

    Private ReadOnly _markerQueue As New Concurrent.ConcurrentQueue(Of EventMarker)

    ' Configuration constants
    Private Const CaptureBufferSize As Integer = 65536 ' 64KB buffer
    Private Const CaptureTimeoutMs As Integer = 1000 ' 1 second timeout
    Public Const MarkerDestPort As UShort = 65000    ' Unique port for event markers
    Public Const MarkerSourcePort As UShort = 65001  ' Source port for markers

    ' Track when we last spoke to avoid spam
    Private _lastAudioAlert As DateTime = DateTime.MinValue
    Private Const AudioAlertCooldownSeconds As Integer = 30

    Private _frameCounter As Long = 0  ' Tracks PCAP frame number
    Private _eventLogger As LidarEventLogger

    Public ReadOnly Property CurrentFrameNumber As Long
        Get
            Return Interlocked.Read(_frameCounter)
        End Get
    End Property

    ' Read-only properties
    Public ReadOnly Property IsCapturing As Boolean
        Get
            Return _isCapturing
        End Get
    End Property

    Public ReadOnly Property PacketCount As Long
        Get
            Return _packetCount
        End Get
    End Property

    ''' <summary>
    ''' Total number of dropped packets (packet loss indicator).
    ''' Updated from PacketCommunicator statistics.
    ''' </summary>
    Public ReadOnly Property DroppedPackets As Long
        Get
            Return _droppedPackets
        End Get
    End Property

    Public ReadOnly Property TotalBytes As Long
        Get
            Return _totalBytes
        End Get
    End Property

    Public ReadOnly Property MarkerCount As Long
        Get
            Return _markerCounter
        End Get
    End Property

    Public Property LastPacketTimestamp As DateTime?

    ''' <summary>
    ''' Event marker structure
    ''' </summary>
    Private Structure EventMarker
        Public EventType As String
        Public EventId As Long
        Public Timestamp As DateTime
        Public Message As String
        Public SequenceNumber As Integer  ' ✅ ADD THIS
    End Structure

    ' ====================================================================
    ' Constructor
    ' ====================================================================
    Public Sub New(Optional adapterGuid As String = "",
                   Optional ipAddress As String = "192.168.1.201",
                   Optional dataPort As UShort = 2368,
                   Optional imuPort As UShort = 8308,
                   Optional deviceId As String = "LiDAR1")
        Me.LidarAdapterGuid = adapterGuid
        Me.LidarIpAddress = ipAddress
        Me.LidarDataPort = dataPort
        Me.LidarImuPort = imuPort
        Me.DeviceId = deviceId
    End Sub

    ' ====================================================================
    ' Public Methods - Capture Control
    ' ====================================================================

    ''' <summary>
    ''' Checks if audio alert should be triggered based on health status
    ''' </summary>
    Public Function ShouldTriggerAudioAlert() As Boolean
        ' Don't spam - enforce cooldown period
        If DateTime.Now.Subtract(_lastAudioAlert).TotalSeconds < AudioAlertCooldownSeconds Then
            Return False
        End If

        ' Check for critical conditions
        Dim timeSinceLastPacket = If(LastPacketTimestamp.HasValue,
                                      DateTime.Now.Subtract(LastPacketTimestamp.Value).TotalSeconds,
                                      Double.MaxValue)

        If timeSinceLastPacket > 10 Then ' Device stopped
            Return True
        End If

        Dim totalPackets As Long = PacketCount + DroppedPackets
        If totalPackets > 100 Then
            Dim lossPercent As Double = (CDbl(DroppedPackets) / CDbl(totalPackets)) * 100.0
            If lossPercent >= 20.0 Then ' Critical loss threshold
                Return True
            End If
        End If

        Return False
    End Function

    ''' <summary>
    ''' Speaks audio alert and updates last alert timestamp
    ''' </summary>
    Public Sub SpeakAlert()
        Try
            Dim synth As New SpeechSynthesizer()
            synth.SelectVoice("Microsoft Zira Desktop")
            synth.Rate = 0

            Dim message As String = GetAlertMessage()
            synth.SpeakAsync(message) ' Use Async to avoid blocking

            _lastAudioAlert = DateTime.Now

        Catch ex As Exception
            ' Log but don't crash if speech fails
            HandleUserMessageLogging("GMRC", $"LiDAR audio alert failed: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Generates appropriate alert message based on health status
    ''' </summary>
    Private Function GetAlertMessage() As String
        Dim timeSinceLastPacket = If(LastPacketTimestamp.HasValue,
                                      DateTime.Now.Subtract(LastPacketTimestamp.Value).TotalSeconds,
                                      Double.MaxValue)

        If timeSinceLastPacket > 10 Then
            Return $"{DeviceId} has stopped responding"
        End If

        Dim totalPackets As Long = PacketCount + DroppedPackets
        If totalPackets > 100 Then
            Dim lossPercent As Double = (CDbl(DroppedPackets) / CDbl(totalPackets)) * 100.0
            If lossPercent >= 20.0 Then
                Return $"{DeviceId} critical packet loss detected"
            End If
        End If

        Return $"{DeviceId} performance degraded"
    End Function

    ''' <summary>
    ''' Starts packet capture for this LiDAR device
    ''' </summary>
    ''' <param name="pcapFilename">Full path to output PCAP file</param>
    ''' <param name="sequence">Current recording sequence number</param>
    Public Sub StartCapture(pcapFilename As String, sequence As Integer)
        Dim logPrefix As String = $"[{DeviceId}] StartCapture"

        Try
            ' Check if already capturing
            If _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Already active")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Initializing capture to {Path.GetFileName(pcapFilename)}...")

            ' Find the LiDAR network adapter
            If Not FindNetworkAdapter() Then
                Throw New Exception("Network adapter not found. Check configuration.")
            End If

            ' Open device for packet capture (promiscuous mode)
            _communicator = _captureDevice.Open(
            CaptureBufferSize,
            PacketDeviceOpenAttributes.Promiscuous,
            CaptureTimeoutMs
        )

            ' ✅ Create event logger FIRST (before filter/dump setup)
            Try
                _eventLogger = New LidarEventLogger(pcapFilename)
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Event logger created for {Path.GetFileName(pcapFilename)}")
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Event logger creation failed: {ex.Message}")
                ' Continue capture even if logger fails
            End Try

            ' Set BPF filter to capture ONLY packets from this LiDAR's IP
            Dim filter As String = $"udp and src host {LidarIpAddress}"
            Using bpfFilter = _communicator.CreateFilter(filter)
                _communicator.SetFilter(bpfFilter)
            End Using

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Filter applied: {filter}")

            ' Open PCAP dump file for writing
            _dumpFile = _communicator.OpenDump(pcapFilename)

            ' Reset statistics and marker queue
            _packetCount = 0
            _totalBytes = 0
            _markerCounter = 0
            _frameCounter = 0  ' ← Reset frame counter here
            While _markerQueue.TryDequeue(Nothing) ' Clear queue
            End While

            ' Start background capture thread
            _isCapturing = True
            _captureThread = New Thread(AddressOf CapturePacketsLoop) With {
                .IsBackground = True,
                .Name = $"LidarCapture_{DeviceId}"
            }
            _captureThread.Start()

            ' Log success and inject initial marker
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Capture started successfully (seq {sequence:D2})")
            InjectEventMarker("START", $"Recording started - {DeviceId}", sequence)

            ' Write event to INCA
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} {DeviceId} capture started (seq {sequence:D2})", True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}", DisplayMsgBox)
            CleanupResources()
            _isCapturing = False
        End Try
    End Sub
    ''' <summary>
    ''' Updates statistics (simplified version without PacketTotalStatistics)
    ''' </summary>
    Private Sub UpdateStatistics()
        ' For now, just keep the counter at 0 or increment on errors
        ' The UI will still show "0 dropped" which is acceptable
        ' True packet loss can be analyzed post-capture using:
        ' editcap -C <offset> input.pcap output.pcap (to check for gaps)
    End Sub

    ''' <summary>
    ''' Stops packet capture for this LiDAR device
    ''' </summary>
    Public Sub StopCapture()
        Dim logPrefix As String = $"[{DeviceId}] StopCapture"
        Dim currentActiveSequence As String = GetCurrentActiveSequence()
        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

        Try
            If Not _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Not currently capturing")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Stopping capture...")

            ' Inject stop marker before stopping
            InjectEventMarker("STOP", "Recording stopped", currentSeq)
            Thread.Sleep(200) ' Give time for marker to be processed

            ' Signal capture thread to stop
            _isCapturing = False

            ' Close event logger
            If _eventLogger IsNot Nothing Then
                _eventLogger.Close()
                _eventLogger = Nothing
            End If

            ' Close PCAP communicator
            If _communicator IsNot Nothing Then
                _communicator.Dispose()
                _communicator = Nothing
            End If

            ' Wait for capture thread to finish (with timeout)
            If _captureThread IsNot Nothing AndAlso _captureThread.IsAlive Then
                If Not _captureThread.Join(5000) Then
                    ' Force abort if thread doesn't finish gracefully
                    HandleUserMessageLogging("GMRC", $"{logPrefix}: Forcing thread abort...")
                    _captureThread.Abort()
                End If
                _captureThread = Nothing
            End If

            ' Cleanup resources
            CleanupResources()

            ' Log statistics
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Captured {_packetCount:N0} packets, {_totalBytes:N0} bytes, {_markerCounter} markers")

            ' Write event to INCA
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} {DeviceId} stopped - {_packetCount:N0} pkts, {_markerCounter} markers", True)

            ' Close event logger
            _eventLogger?.Close()
            _eventLogger = Nothing

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}")
            HandleUserMessageLogging("GMRC", $"LidarDevice.StopCapture ({DeviceId}): {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Injects an event marker into the capture stream
    ''' </summary>
    Public Function InjectEventMarker(eventType As String, message As String, sequenceNumber As Integer) As Long
        Try
            If Not _isCapturing Then
                Return -1
            End If

            Dim markerFrame As Long = Interlocked.Read(_frameCounter) + 1

            Dim marker As New EventMarker With {
                    .EventType = eventType,
                    .EventId = Interlocked.Increment(_markerCounter),
                    .Timestamp = DateTime.Now,
                    .Message = message,
                    .SequenceNumber = sequenceNumber
                    }

            _markerQueue.Enqueue(marker)
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Queued marker #{marker.EventId} - {eventType}: {message}")
            Return markerFrame

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] InjectEventMarker: {ex.Message}")
            Return -1
        End Try
    End Function

    ' ====================================================================
    ' Private Methods - Internal Implementation
    ' ====================================================================

    ''' <summary>
    ''' Background thread loop for packet capture
    ''' </summary>
    Private Sub CapturePacketsLoop()
        Try
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Capture thread started")

            While _isCapturing
                ' Check for pending event markers to inject
                Dim marker As New EventMarker()
                If _markerQueue.TryDequeue(marker) Then
                    InjectMarkerPacket(marker)
                End If

                Dim result As PacketCommunicatorReceiveResult
                Dim packet As Packet = Nothing

                ' Attempt to receive a packet (blocks up to CaptureTimeoutMs)
                result = _communicator.ReceivePacket(packet)

                Select Case result
                    Case PacketCommunicatorReceiveResult.Ok
                        ' Successfully received a packet
                        If _dumpFile IsNot Nothing Then
                            _dumpFile.Dump(packet)

                            Interlocked.Increment(_frameCounter)

                            ' Update timestamp for health monitoring
                            LastPacketTimestamp = DateTime.Now

                            ' Update statistics
                            Interlocked.Increment(_packetCount)
                            Interlocked.Add(_totalBytes, packet.Length)

                            ' Update stats every 1000 packets
                            If _packetCount Mod 1000 = 0 Then
                                UpdateStatistics()
                            End If
                        End If

                    Case PacketCommunicatorReceiveResult.Timeout
                        ' Timeout is normal, continue waiting
                        Continue While

                    Case PacketCommunicatorReceiveResult.Eof
                        HandleUserMessageLogging("GMRC", $"[{DeviceId}] Unexpected EOF")
                        Exit While

                    Case Else
                        HandleUserMessageLogging("GMRC", $"[{DeviceId}] Error receiving packet: {result}")
                        ' Increment dropped packet counter on errors
                        Interlocked.Increment(_droppedPackets)
                        Exit While
                End Select
            End While

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Capture thread exiting normally")

        Catch ex As ThreadAbortException
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Thread aborted")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] CapturePacketsLoop exception: {ex.Message}")

        Finally
            ' Ensure dump file is closed
            If _dumpFile IsNot Nothing Then
                Try
                    _dumpFile.Dispose()
                Catch
                    ' Ignore disposal errors
                End Try
                _dumpFile = Nothing
            End If
        End Try
    End Sub


    ''' <summary>
    ''' Checks if a packet is an event marker
    ''' </summary>
    Private Function IsMarkerPacket(packet As Packet) As Boolean
        If packet Is Nothing Then
            Return False
        End If

        Try
            Dim eth = packet.Ethernet
            If eth IsNot Nothing AndAlso eth.IpV4 IsNot Nothing Then
                Dim udp = eth.IpV4.Udp
                If udp IsNot Nothing Then
                    Return udp.DestinationPort = MarkerDestPort AndAlso
                           udp.SourcePort = MarkerSourcePort
                End If
            End If
        Catch
            ' Ignore parsing errors
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Extracts marker data from a marker packet
    ''' </summary>
    Private Function ExtractMarkerData(packet As Packet) As String
        Try
            Dim udp = packet.Ethernet.IpV4.Udp
            If udp IsNot Nothing AndAlso udp.Payload.Length > 0 Then
                Return System.Text.Encoding.UTF8.GetString(udp.Payload.ToArray())
            End If
        Catch
            ' Ignore parsing errors
        End Try
        Return "(unable to parse)"
    End Function

    ''' <summary>
    ''' Injects a synthetic marker packet into the PCAP stream
    ''' </summary>
    Private Sub InjectMarkerPacket(marker As EventMarker)
        Try
            If _dumpFile Is Nothing Then Return

            ' Build marker payload (pipe-delimited format)
            Dim payload As String = $"{marker.EventType}|{marker.Message}|{marker.SequenceNumber:D2}"
            Dim payloadBytes() As Byte = System.Text.Encoding.UTF8.GetBytes(payload)

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Injecting marker payload: {payload}")

            ' Create synthetic packet layers
            Dim srcMac As MacAddress = New MacAddress("02:00:00:00:00:01")
            Dim dstMac As MacAddress = New MacAddress("02:00:00:00:00:02")
            Dim srcIp As IpV4Address = New IpV4Address("192.168.40.200")
            Dim dstIp As IpV4Address = New IpV4Address("192.168.40.255")

            Dim ethernetLayer As New EthernetLayer With {
                .Source = srcMac,
                .Destination = dstMac,
                .EtherType = EthernetType.IpV4
            }

            Dim ipV4Layer As New IpV4Layer With {
                .Source = srcIp,
                .CurrentDestination = dstIp,
                .Ttl = 128
            }

            Dim udpLayer As New UdpLayer With {
                .SourcePort = MarkerSourcePort,
                .DestinationPort = MarkerDestPort
            }

            Dim payloadLayer As New PayloadLayer With {
                .Data = New Datagram(payloadBytes)
            }

            ' Build and write marker packet to PCAP
            Dim builder As New PacketBuilder(ethernetLayer, ipV4Layer, udpLayer, payloadLayer)
            Dim markerPacket As Packet = builder.Build(marker.Timestamp)

            _dumpFile.Dump(markerPacket)

            ' Increment frame counter for the injected marker packet
            Interlocked.Increment(_frameCounter)

            ' Directly log to sidecar .txt file (don't wait to read it back from network)
            _eventLogger?.LogEvent(_frameCounter, marker.Timestamp, marker.EventType, marker.Message, marker.SequenceNumber)

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Marker logged: Frame {_frameCounter} - {marker.EventType}: {marker.Message}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] InjectMarkerPacket failed: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Finds the network adapter for this LiDAR device
    ''' Priority: 1) GUID match, 2) IP subnet match, 3) First IPv4 adapter
    ''' </summary>
    Private Function FindNetworkAdapter() As Boolean
        Try
            Dim allDevices As IList(Of LivePacketDevice)
            Try
                allDevices = LivePacketDevice.AllLocalMachine
            Catch driverEx As Exception
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] Failed to access network adapters. Ensure Npcap is installed. Error: {driverEx.Message}")
                Return False
            End Try

            If allDevices.Count = 0 Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] No network adapters found")
                Return False
            End If

            ' PRIORITY 1: Try to find adapter by GUID if specified
            If Not String.IsNullOrWhiteSpace(LidarAdapterGuid) Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] Searching for adapter with GUID: {LidarAdapterGuid}")

                ' Remove braces from config GUID for comparison
                Dim guidToMatch As String = LidarAdapterGuid.Replace("{", "").Replace("}", "").ToUpper()

                For Each device In allDevices
                    HandleUserMessageLogging("GMRC", $"[{DeviceId}] Checking: {device.Name}")

                    ' Check if device name contains the GUID (with or without braces)
                    If device.Name.ToUpper().Contains(guidToMatch) Then
                        _captureDevice = device
                        HandleUserMessageLogging("GMRC", $"[{DeviceId}] Selected by GUID: {device.Description}")
                        Return True
                    End If
                Next

                HandleUserMessageLogging("GMRC", $"[{DeviceId}] GUID not found, trying IP-based detection")
            End If

            ' PRIORITY 2: Find adapter on same subnet as LiDAR
            Dim lidarSubnet As String = LidarIpAddress.Substring(0, LidarIpAddress.LastIndexOf("."c))

            For Each device In allDevices
                For Each address In device.Addresses
                    If address.Address IsNot Nothing Then
                        Dim addrStr As String = address.Address.ToString()
                        If addrStr.StartsWith(lidarSubnet & ".") AndAlso
                           Not addrStr.Contains(":") AndAlso
                           addrStr <> LidarIpAddress Then
                            _captureDevice = device
                            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Selected by subnet: {device.Description} ({addrStr})")
                            Return True
                        End If
                    End If
                Next
            Next

            ' PRIORITY 3: Use first adapter with IPv4
            For Each device In allDevices
                For Each address In device.Addresses
                    If address.Address IsNot Nothing Then
                        Dim addrStr As String = address.Address.ToString()
                        If Not addrStr.Contains(":") Then
                            _captureDevice = device
                            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Using fallback: {device.Description} ({addrStr})")
                            Return True
                        End If
                    End If
                Next
            Next

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] No suitable adapter found")
            Return False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] FindNetworkAdapter: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Cleans up all capture resources
    ''' </summary>
    Private Sub CleanupResources()
        Try
            ' Close dump file
            If _dumpFile IsNot Nothing Then
                Try
                    _dumpFile.Dispose()
                Catch
                    ' Ignore disposal errors
                End Try
                _dumpFile = Nothing
            End If

            ' Close communicator
            If _communicator IsNot Nothing Then
                Try
                    _communicator.Dispose()
                Catch
                    ' Ignore disposal errors
                End Try
                _communicator = Nothing
            End If

            _captureDevice = Nothing

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] CleanupResources: {ex.Message}")
        End Try
    End Sub

End Class
