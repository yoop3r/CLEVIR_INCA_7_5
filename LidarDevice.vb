Option Strict On
Imports System.IO
Imports System.Speech.Synthesis
Imports System.Threading
Imports System.Threading.Tasks
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

    ' ✅ REMOVED: UpdateFromSdk() doesn't belong here!

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
''' ✅ NEW: Hesai AT128 packet information structure
''' Parsed directly from UDP payload for health monitoring
''' </summary>
Public Structure HesaiPacketInfo
    Public IsValid As Boolean
    Public UdpSequence As UInteger
    Public OperationalState As Byte  ' 0=High Res, 1=Shutdown, 2=Standard, 3=Energy Saving
    Public ReturnMode As Byte
    Public MotorSpeed As UShort      ' RPM
    Public HasFunctionalSafety As Boolean
    Public HasIMU As Boolean
    Public HasSignature As Boolean
    Public LidarState As Byte        ' From Functional Safety (if present)
    Public FaultCode As UShort       ' From Functional Safety (if present)
End Structure

''' <summary>
''' Represents a single LiDAR sensor device with independent capture state
''' </summary>
Public Class LidarDevice

    ' ====================================================================
    ' Instance-level configuration (each LiDAR has its own settings)
    ' ====================================================================
    Public Property LidarAdapterGuid As String = ""
    Public Property LidarIpAddress As String = "10.5.55.14"
    Public Property LidarDataPort As UShort = 2311
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
    Private _droppedPackets As Long = 0

    ' ✅ NEW: Add Hesai SDK stats properties HERE (in LidarDevice, not LidarEventLogger)
    Private _checksumErrors As Long = 0
    Private _outOfOrderPackets As Long = 0
    Private _lastHesaiSequence As UInteger = 0
    Private _hesaiSequenceInitialized As Boolean = False
    Private _lastHesaiInfo As HesaiPacketInfo

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

    ' ✅ NEW: Store Hesai config for lazy registration
    Public Property HesaiConfig As HesaiInterop.HesaiDeviceConfig
    Public Property HasHesaiConfig As Boolean = False
    Public Property IsHesaiRegistered As Boolean = False

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
            Return Interlocked.Read(_packetCount)
        End Get
    End Property

    ''' <summary>
    ''' Total number of dropped packets (packet loss indicator).
    ''' Updated from PacketCommunicator statistics or Hesai SDK.
    ''' </summary>
    Public ReadOnly Property DroppedPackets As Long
        Get
            Return Interlocked.Read(_droppedPackets)
        End Get
    End Property

    ''' <summary>
    ''' ✅ NEW: Checksum errors from Hesai SDK
    ''' </summary>
    Public ReadOnly Property ChecksumErrors As Long
        Get
            Return Interlocked.Read(_checksumErrors)
        End Get
    End Property

    ''' <summary>
    ''' ✅ NEW: Out-of-order packets from Hesai SDK
    ''' </summary>
    Public ReadOnly Property OutOfOrderPackets As Long
        Get
            Return Interlocked.Read(_outOfOrderPackets)
        End Get
    End Property

    Public ReadOnly Property TotalBytes As Long
        Get
            Return Interlocked.Read(_totalBytes)
        End Get
    End Property

    Public ReadOnly Property MarkerCount As Long
        Get
            Return Interlocked.Read(_markerCounter)
        End Get
    End Property

    Public Property LastPacketTimestamp As DateTime?

    ''' <summary>
    ''' ✅ NEW: Last parsed Hesai packet info (for health monitoring)
    ''' </summary>
    Public ReadOnly Property LastHesaiInfo As HesaiPacketInfo
        Get
            Return _lastHesaiInfo
        End Get
    End Property

    ''' <summary>
    ''' Event marker structure
    ''' </summary>
    Private Structure EventMarker
        Public EventType As String
        Public EventId As Long
        Public Timestamp As DateTime
        Public Message As String
        Public SequenceNumber As Integer
    End Structure

    ' Add to LidarDevice class (around line 50)
    Private _oxtsInterface As OxtsNcomInterface ' Reference to shared OXTS listener

    Public Sub SetOxtsInterface(oxts As OxtsNcomInterface)
        _oxtsInterface = oxts
    End Sub

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
    ''' ✅ DEPRECATED: Use UpdateStatistics() instead (called automatically during capture)
    ''' This method is kept for backward compatibility but redirects to UpdateStatistics()
    ''' </summary>
    Public Sub UpdateFromSdk()
        UpdateStatistics() ' Redirect to the main statistics update method
    End Sub

    ''' <summary>
    ''' ✅ NEW: Test LiDAR + OXTS integration (static development)
    ''' </summary>
    Public Sub TestLidarOxtsIntegration()
        HandleUserMessageLogging("GMRC", "=== LiDAR + OXTS Integration Test ===")
        HandleUserMessageLogging("GMRC", $"Device: {DeviceId}")
        HandleUserMessageLogging("GMRC", $"LiDAR IP: {LidarIpAddress}")
        HandleUserMessageLogging("GMRC", $"Capturing: {_isCapturing}")
        HandleUserMessageLogging("GMRC", $"Packets: {_packetCount:N0}")
        HandleUserMessageLogging("GMRC", $"Dropped: {_droppedPackets:N0}")
        HandleUserMessageLogging("GMRC", $"Checksum Errors: {_checksumErrors:N0}")
        HandleUserMessageLogging("GMRC", $"Out-of-Order: {_outOfOrderPackets:N0}")
        HandleUserMessageLogging("GMRC", $"Markers: {_markerCounter}")
        HandleUserMessageLogging("GMRC", $"Frames: {_frameCounter}")

        If _oxtsInterface IsNot Nothing Then
            HandleUserMessageLogging("GMRC", "")
            HandleUserMessageLogging("GMRC", "=== OXTS Status ===")
            HandleUserMessageLogging("GMRC", $"GPS Lock: {_oxtsInterface.IsGpsLocked}")
            HandleUserMessageLogging("GMRC", $"GPS Time Available: {_oxtsInterface.LastGpsTime.HasValue}")

            If _oxtsInterface.LastGpsTime.HasValue Then
                Dim syncTime = _oxtsInterface.GetSynchronizedTimestamp()
                HandleUserMessageLogging("GMRC", $"Synchronized Time: {syncTime:yyyy-MM-dd HH:mm:ss.fff}")
            End If
        Else
            HandleUserMessageLogging("GMRC", "⚠️ OXTS interface not connected!")
        End If

        HandleUserMessageLogging("GMRC", "=== Test Complete ===")
    End Sub

    ''' <summary>
    ''' ✅ NEW: Inject test marker with OXTS data
    ''' </summary>
    Public Sub InjectTestMarkerWithOxtsData()
        If Not _isCapturing Then
            HandleUserMessageLogging("GMRC", "Cannot inject marker - not capturing")
            Return
        End If

        Dim message As String = "TEST MARKER"

        If _oxtsInterface IsNot Nothing Then
            Dim pos = _oxtsInterface.GetCurrentPosition()
            message &= $" | Lat:{pos.Latitude:F6} Lon:{pos.Longitude:F6} Alt:{pos.Altitude:F2}m"
        End If

        InjectEventMarker("TEST", message, 999)
        HandleUserMessageLogging("GMRC", $"Injected test marker: {message}")
    End Sub

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
            Return $"LiDAR {DeviceId} has stopped responding"
        End If

        Dim totalPackets As Long = PacketCount + DroppedPackets
        If totalPackets > 100 Then
            Dim lossPercent As Double = (CDbl(DroppedPackets) / CDbl(totalPackets)) * 100.0
            If lossPercent >= 20.0 Then
                Return $"LiDAR {DeviceId} critical packet loss detected"
            End If
        End If

        Return $"LiDAR {DeviceId} performance degraded"
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
            _droppedPackets = 0
            _checksumErrors = 0
            _outOfOrderPackets = 0
            _hesaiSequenceInitialized = False  ' Reset sequence tracking for new capture
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

            ' ════════════════════════════════════════════════════════════════
            ' ✅ VALIDATION-ONLY MODE: Register with Hesai SDK for stats tracking
            '    WITHOUT binding to UDP ports (PcapDotNet handles capture)
            ' ════════════════════════════════════════════════════════════════
            If HesaiInterop.IsAvailable() AndAlso Not IsHesaiRegistered Then
                Task.Run(Sub()
                             Try
                                 ' ✅ ALWAYS use validation-only mode to avoid UDP port conflicts
                                 HandleUserMessageLogging("GMRC", $"{logPrefix}: Registering with Hesai SDK in VALIDATION-ONLY mode...")

                                 If HesaiInterop.RegisterDeviceValidationOnly(DeviceId, LidarIpAddress, CInt(LidarDataPort)) Then
                                     IsHesaiRegistered = True
                                     HandleUserMessageLogging("GMRC", $"{logPrefix}: ✅ Registered with Hesai SDK (validation-only, no UDP bind)")
                                 Else
                                     HandleUserMessageLogging("GMRC", $"{logPrefix}: ⚠️ Hesai SDK validation-only registration failed")
                                 End If

                             Catch hesaiEx As Exception
                                 HandleUserMessageLogging("GMRC", $"{logPrefix}: Hesai SDK registration error: {hesaiEx.Message}")
                             End Try
                         End Sub)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}", DisplayMsgBox)
            CleanupResources()
            _isCapturing = False
        End Try
    End Sub
    ''' <summary>
    ''' Updates statistics from Hesai SDK (if available) or uses PcapDotNet fallback
    ''' Called periodically during capture (every 1000 packets)
    ''' </summary>
    Private Sub UpdateStatistics()
        Try
            ' ✅ PRIORITY 1: Try to get stats from Hesai SDK
            If HesaiInterop.IsAvailable() Then
                Dim stats = HesaiInterop.GetDeviceStats(DeviceId)

                ' Only update if SDK is returning valid data
                If stats.packets_received > 0 Then
                    ' Sync dropped packets from SDK (most accurate)
                    Interlocked.Exchange(_droppedPackets, CLng(stats.packets_dropped))

                    ' Sync checksum and out-of-order errors
                    Interlocked.Exchange(_checksumErrors, CLng(stats.checksum_errors))
                    Interlocked.Exchange(_outOfOrderPackets, CLng(stats.out_of_order_packets))

                    ' Optional: Sync total bytes if SDK provides it
                    If stats.total_bytes > 0 Then
                        Interlocked.Exchange(_totalBytes, CLng(stats.total_bytes))
                    End If

                    Return ' Success - SDK stats updated
                End If
            End If

            ' ✅ FALLBACK: If Hesai SDK not available, dropped packets remain at 0
            ' True packet loss can be analyzed post-capture using:
            ' editcap -C <offset> input.pcap output.pcap (to check for gaps)
            ' or Wireshark's "Expert Info" analysis

        Catch ex As Exception
            ' Don't crash capture thread if stats update fails
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] UpdateStatistics error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Stops packet capture for this LiDAR device.
    ''' ✅ NOTE: This does NOT unregister from Hesai SDK - SDK stays running between sequences.
    ''' Use ShutdownDevice() to fully release the SDK when done with all recording.
    ''' </summary>
    Public Sub StopCapture()
        Dim logPrefix As String = $"DeviceID: [{DeviceId}] LiDARStopCapture"
        Dim currentActiveSequence As String = GetCurrentActiveSequence()
        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

        Try
            ' ✅ CHANGED: Do NOT unregister SDK here - keep it running between sequences
            ' The SDK will continue to track packet loss statistics across sequences
            ' Use ShutdownDevice() when you want to fully release the SDK

            If Not _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Not currently capturing")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Stopping capture (SDK stays registered for next sequence)...")

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
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} LiDAR DeviceID: {DeviceId} stopped - {_packetCount:N0} pkts, {_markerCounter} markers", True)

            ' Close event logger (duplicate call removed - already closed above)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}")
            HandleUserMessageLogging("GMRC", $"LidarDevice.StopCapture ({DeviceId}): {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Fully shuts down the device, including unregistering from Hesai SDK.
    ''' Call this when:
    '''   - Application is closing
    '''   - User manually stops ALL recording (not just between sequences)
    '''   - Device configuration changes require SDK restart
    ''' </summary>
    Public Sub ShutdownDevice()
        Dim logPrefix As String = $"DeviceID: [{DeviceId}] LiDARShutdownDevice"

        Try
            ' First, stop any active capture
            If _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Stopping active capture first...")
                StopCapture()
            End If

            ' Now unregister from Hesai SDK
            If HesaiInterop.IsAvailable() AndAlso IsHesaiRegistered Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Unregistering from Hesai SDK...")
                HesaiInterop.UnregisterDevice(DeviceId)
                IsHesaiRegistered = False
                HandleUserMessageLogging("GMRC", $"{logPrefix}: ✅ Unregistered from Hesai SDK")
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Device fully shut down")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}")
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
            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR Queued marker #{marker.EventId} - {eventType}: {message}")
            Return markerFrame

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR InjectEventMarker: {ex.Message}")
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
            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR Capture thread started")

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

                            ' ✅ NEW: Parse Hesai packet for sequence tracking (every 100th packet to reduce overhead)
                            If _packetCount Mod 100 = 0 Then
                                Try
                                    Dim eth = packet.Ethernet
                                    If eth IsNot Nothing AndAlso eth.IpV4 IsNot Nothing Then
                                        Dim udp = eth.IpV4.Udp
                                        If udp IsNot Nothing AndAlso udp.Payload.Length > 0 Then
                                            Dim udpPayload As Byte() = udp.Payload.ToArray()
                                            Dim info As HesaiPacketInfo = ParseHesaiPacket(udpPayload)

                                            If info.IsValid Then
                                                ' Store last packet info
                                                _lastHesaiInfo = info

                                                ' Check for sequence gaps (packet loss detection)
                                                If _hesaiSequenceInitialized Then
                                                    ' Calculate expected sequence (wraps at UInteger.MaxValue)
                                                    Dim expectedSeq As UInteger = _lastHesaiSequence + 100UI

                                                    ' Handle wrap-around
                                                    Dim gap As Long
                                                    If info.UdpSequence >= expectedSeq Then
                                                        gap = CLng(info.UdpSequence) - CLng(expectedSeq)
                                                    Else
                                                        ' Wrapped around
                                                        gap = (CLng(UInteger.MaxValue) - CLng(expectedSeq)) + CLng(info.UdpSequence) + 1
                                                    End If

                                                    ' Only count reasonable gaps (not sensor restarts)
                                                    If gap > 0 AndAlso gap < 10000 Then
                                                        Interlocked.Add(_droppedPackets, gap)
                                                        Interlocked.Add(_outOfOrderPackets, 1)
                                                    End If
                                                Else
                                                    _hesaiSequenceInitialized = True
                                                End If

                                                _lastHesaiSequence = info.UdpSequence
                                            End If
                                        End If
                                    End If
                                Catch parseEx As Exception
                                    ' Don't crash capture on parse errors
                                    If _packetCount Mod 10000 = 0 Then
                                        HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR Packet parse error: {parseEx.Message}")
                                    End If
                                End Try
                            End If

                            ' Update stats every 1000 packets
                            If _packetCount Mod 1000 = 0 Then
                                UpdateStatistics()
                            End If
                        End If

                    Case PacketCommunicatorReceiveResult.Timeout
                        ' Timeout is normal, continue waiting
                        Continue While

                    Case PacketCommunicatorReceiveResult.Eof
                        HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR Unexpected EOF")
                        Exit While

                    Case Else
                        HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR Error receiving packet: {result}")
                        ' Increment dropped packet counter on errors
                        Interlocked.Increment(_droppedPackets)
                        Exit While
                End Select
            End While

            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR Capture thread exiting normally")

        Catch ex As ThreadAbortException
            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR Thread aborted")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR CapturePacketsLoop exception: {ex.Message}")

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

            ' Use GPS time if available, otherwise system time
            Dim timestamp As DateTime = marker.Timestamp
            If _oxtsInterface IsNot Nothing AndAlso _oxtsInterface.LastGpsTime.HasValue Then
                timestamp = _oxtsInterface.GetSynchronizedTimestamp()
            End If

            ' Build marker payload (pipe-delimited format)
            Dim payload As String = $"{marker.EventType}|{marker.Message}|{marker.SequenceNumber:D2}|GPS:{timestamp:yyyy-MM-dd HH:mm:ss.fff}"
            Dim payloadBytes() As Byte = System.Text.Encoding.UTF8.GetBytes(payload)

            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR Injecting marker payload: {payload}")

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
            Dim markerPacket As Packet = builder.Build(timestamp) ' Use GPS timestamp here

            _dumpFile.Dump(markerPacket)

            ' Increment frame counter for the injected marker packet
            Interlocked.Increment(_frameCounter)

            ' Directly log to sidecar .txt file (don't wait to read it back from network)
            _eventLogger?.LogEvent(_frameCounter, timestamp, marker.EventType, marker.Message, marker.SequenceNumber)

            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] LiDAR Marker logged: Frame {_frameCounter} - {marker.EventType}: {marker.Message}")

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
                HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] Failed to access network adapters. Ensure Npcap is installed. Error: {driverEx.Message}")
                Return False
            End Try

            If allDevices.Count = 0 Then
                HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] No network adapters found")
                Return False
            End If

            ' PRIORITY 1: Try to find adapter by GUID if specified
            If Not String.IsNullOrWhiteSpace(LidarAdapterGuid) Then
                HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] Searching for adapter with GUID: {LidarAdapterGuid}")

                ' Remove braces from config GUID for comparison
                Dim guidToMatch As String = LidarAdapterGuid.Replace("{", "").Replace("}", "").ToUpper()

                For Each device In allDevices
                    HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] Checking: {device.Name}")

                    ' Check if device name contains the GUID (with or without braces)
                    If device.Name.ToUpper().Contains(guidToMatch) Then
                        _captureDevice = device
                        HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] Selected by GUID: {device.Description}")
                        Return True
                    End If
                Next

                HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] GUID not found, trying IP-based detection")
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
                            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] Selected by subnet: {device.Description} ({addrStr})")
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
                            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] Using fallback: {device.Description} ({addrStr})")
                            Return True
                        End If
                    End If
                Next
            Next

            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] No suitable adapter found")
            Return False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] FindNetworkAdapter: {ex.Message}")
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
            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] CleanupResources: {ex.Message}")
        End Try
    End Sub

    ' ====================================================================
    ' ✅ NEW: Hesai Packet Parser
    ' ====================================================================

    ''' <summary>
    ''' ✅ NEW: Parse Hesai AT128 UDP packet for health monitoring
    ''' Based on Hesai AT128 User Manual packet structure
    ''' </summary>
    Private Function ParseHesaiPacket(udpPayload As Byte()) As HesaiPacketInfo
        Dim info As New HesaiPacketInfo With {.IsValid = False}

        Try
            ' Minimum size check (Pre-Header + Header + Body minimum)
            If udpPayload.Length < 800 Then Return info

            ' Verify Pre-Header magic bytes (0xEE 0xFF)
            If udpPayload(0) <> &HEE OrElse udpPayload(1) <> &HFF Then Return info

            ' Read Protocol Version (bytes 2-3)
            Dim versionMajor As Byte = udpPayload(2)
            Dim versionMinor As Byte = udpPayload(3)

            ' Read Header Flags (byte 11 - last byte of Header section)
            Dim flags As Byte = udpPayload(11)
            info.HasSignature = (flags And &H8) <> 0      ' Bit 3
            info.HasFunctionalSafety = (flags And &H4) <> 0  ' Bit 2
            info.HasIMU = (flags And &H2) <> 0            ' Bit 1
            ' Bit 0 is UDP sequence flag (should always be 1)

            ' Calculate Tail offset
            ' Pre-Header(6) + Header(6) + Body(776) = 788
            Dim tailOffset As Integer = 788

            ' Add Functional Safety section if present (17 bytes)
            If info.HasFunctionalSafety Then
                If udpPayload.Length < tailOffset + 17 Then Return info

                ' Parse Functional Safety data
                Dim fsVersion As Byte = udpPayload(tailOffset)
                Dim lidarStateAndFlags As Byte = udpPayload(tailOffset + 1)
                info.LidarState = CByte((lidarStateAndFlags >> 5) And &H7)  ' Bits [7:5]

                ' Fault code is 2 bytes at offset +4
                If udpPayload.Length >= tailOffset + 6 Then
                    info.FaultCode = BitConverter.ToUInt16(udpPayload, tailOffset + 4)
                End If

                tailOffset += 17
            End If

            ' Ensure we have enough data for Tail section
            If udpPayload.Length < tailOffset + 30 Then Return info

            ' Parse Tail section
            ' Skip Reserved (9 bytes) + Azimuth State (2 bytes)
            Dim tailDataOffset As Integer = tailOffset + 11

            ' Operational State (1 byte)
            info.OperationalState = udpPayload(tailDataOffset)
            tailDataOffset += 1

            ' Return Mode (1 byte)
            info.ReturnMode = udpPayload(tailDataOffset)
            tailDataOffset += 1

            ' Motor Speed (2 bytes, little-endian)
            info.MotorSpeed = BitConverter.ToUInt16(udpPayload, tailDataOffset)
            tailDataOffset += 2

            ' Skip Date & Time (6 bytes) + Timestamp (4 bytes) + Factory Info (1 byte)
            tailDataOffset += 11

            ' UDP Sequence (4 bytes, little-endian)
            If udpPayload.Length < tailDataOffset + 4 Then Return info
            info.UdpSequence = BitConverter.ToUInt32(udpPayload, tailDataOffset)

            info.IsValid = True

        Catch ex As Exception
            ' Parsing failed - return invalid
            HandleUserMessageLogging("GMRC", $"DeviceID: [{DeviceId}] ParseHesaiPacket error: {ex.Message}")
        End Try

        Return info
    End Function

End Class

