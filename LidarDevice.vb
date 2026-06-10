Option Strict On
Imports System.IO
Imports System.Speech.Synthesis
Imports System.Threading
Imports System.Threading.Tasks
Imports SharpPcap
Imports SharpPcap.LibPcap
Imports PacketDotNet
Imports PcapEventBridge
Imports System.Net.NetworkInformation


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

    ' ✅ CHANGED: SharpPcap capture device types
    ' Instance-level capture state
    Private _captureDevice As ICaptureDevice = Nothing
    Private _dumpFile As CaptureFileWriterDevice = Nothing
    Private _captureThread As Threading.Thread = Nothing
    Private _eventBridge As PcapEventBridge.PcapEventBridge = Nothing

    ' Statistics (unchanged)
    Private _isCapturing As Boolean = False
    Private _captureStartedAt As Long = 0  ' Ticks; set when capture starts, used for startup grace period
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
    Private Const CaptureBufferSize As Integer = 16 * 1024 * 1024  ' 16MB - Npcap kernel ring buffer
    Private Const ReadTimeoutMs As Integer = 1000    ' 1 second read timeout for capture device
    Public Const MarkerDestPort As UShort = 65000    ' Unique port for event markers
    Public Const MarkerSourcePort As UShort = 65001  ' Source port for markers

    ' Track when we last spoke to avoid spam
    Private _lastAudioAlert As DateTime = DateTime.MinValue
    Private Const AudioAlertCooldownSeconds As Integer = 30

    Private _frameCounter As Long = 0  ' Tracks PCAP frame number
    Private _eventLogger As LidarEventLogger

    ' ✅ DIAGNOSTIC: Counters for root-cause analysis of STOPPED anomaly
    Private _gateDeniedCount As Long = 0   ' Packets arriving when write gate is closed
    Private _handlerErrorCount As Long = 0 ' Exceptions thrown inside OnPacketArrived

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

    ' Volatile backing field ensures the health-check thread always reads the latest value
    ' written by the SharpPcap capture thread without needing a full lock.
    Private _lastPacketTimestamp As Long = 0  ' Ticks; 0 = never received

    Public Property LastPacketTimestamp As DateTime?
        Get
            Dim ticks As Long = Interlocked.Read(_lastPacketTimestamp)
            If ticks = 0 Then Return Nothing
            Return New DateTime(ticks, DateTimeKind.Local)
        End Get
        Set(value As DateTime?)
            Interlocked.Exchange(_lastPacketTimestamp, If(value.HasValue, value.Value.Ticks, 0L))
        End Set
    End Property

    ''' <summary>The UTC time at which this device last started capturing (Nothing if never started).</summary>
    Public ReadOnly Property CaptureStartedAt As DateTime?
        Get
            Dim ticks As Long = Interlocked.Read(_captureStartedAt)
            If ticks = 0 Then Return Nothing
            Return New DateTime(ticks, DateTimeKind.Local)
        End Get
    End Property

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
    Private _timeSyncProvider As ITimeSyncProvider ' Reference to shared time sync provider

    Public Sub SetOxtsInterface(oxts As OxtsNcomInterface)
        _timeSyncProvider = oxts
    End Sub

    Public Sub SetTimeSyncProvider(provider As ITimeSyncProvider)
        _timeSyncProvider = provider
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

        ' Initialize the event bridge for packet capture
        _eventBridge = New PcapEventBridge.PcapEventBridge()
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

        If _timeSyncProvider IsNot Nothing Then
            HandleUserMessageLogging("GMRC", "")
            HandleUserMessageLogging("GMRC", $"=== Time Sync Status ({_timeSyncProvider.ProviderName}) ===")
            HandleUserMessageLogging("GMRC", _timeSyncProvider.GetPtpStatusText())
            HandleUserMessageLogging("GMRC", _timeSyncProvider.GetNtpStatusText())

            If _timeSyncProvider.IsSynchronized() Then
                Dim syncTime = _timeSyncProvider.GetSynchronizedTimestamp()
                HandleUserMessageLogging("GMRC", $"Synchronized Time: {syncTime:yyyy-MM-dd HH:mm:ss.fff}")
            End If
        Else
            HandleUserMessageLogging("GMRC", "⚠️ Time sync provider not connected!")
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

        If TypeOf _timeSyncProvider Is OxtsNcomInterface Then
            Dim pos = DirectCast(_timeSyncProvider, OxtsNcomInterface).GetCurrentPosition()
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

        ' Never alert for a device that has never received a packet (not started / disabled)
        If Not LastPacketTimestamp.HasValue Then
            Return False
        End If

        ' Check for critical conditions
        Dim timeSinceLastPacket = DateTime.Now.Subtract(LastPacketTimestamp.Value).TotalSeconds

        If timeSinceLastPacket > 15 Then ' Device stopped
            HandleUserMessageLogging("GMRC",
                $"[{DeviceId}] DIAG: 🔴 ALERT TRIGGER — stopped responding, " &
                $"last_pkt={timeSinceLastPacket:F1}s ago, " &
                $"pkts={PacketCount:N0}, dropped={DroppedPackets:N0}, " &
                $"isCapturing={_isCapturing}, " &
                $"dumpFile={(If(_dumpFile Is Nothing, "NULL", "open"))}, " &
                $"gateDenied={Interlocked.Read(_gateDeniedCount)}, " &
                $"handlerErrors={Interlocked.Read(_handlerErrorCount)}, " &
                $"ts={DateTime.Now:HH:mm:ss.fff}")
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
                                      -1.0)

        If timeSinceLastPacket > 15 Then
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
    ''' <param name="pcapFilename">Full path to output PCAP file</param>
    ''' <param name="sequence">Current recording sequence number</param>
    ''' ✅ NEW VERSION: StartCapture using SharpPcap
    ''' </summary>
    Public Sub StartCapture(pcapFilename As String, sequence As Integer)
        Dim logPrefix As String = $"[{DeviceId}] StartCapture"

        Try
            If _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Already active")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Initializing native Npcap capture...")

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 1: Find Network Adapter (SharpPcap API)
            ' ═══════════════════════════════════════════════════════════════════
            If Not FindNetworkAdapter() Then
                Throw New InvalidOperationException($"Network adapter not found for {DeviceId} (IP {LidarIpAddress})")
            End If

            ' Ensure output directory exists
            Dim outputDir = Path.GetDirectoryName(pcapFilename)
            If Not String.IsNullOrEmpty(outputDir) AndAlso Not Directory.Exists(outputDir) Then
                Directory.CreateDirectory(outputDir)
            End If

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 2: Configure Device (SharpPcap 6.x uses DeviceConfiguration)
            ' ═══════════════════════════════════════════════════════════════════
            Dim config As New DeviceConfiguration() With {
                .Mode = DeviceModes.Promiscuous,
                .ReadTimeout = ReadTimeoutMs,
                .KernelBufferSize = CaptureBufferSize,
                .Snaplen = 65535,
                .Immediate = True
            }
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Config set - KernelBuffer={CaptureBufferSize \ (1024 * 1024)}MB, SnapLen=65535, Immediate=True")

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 3: Open Device with Configuration
            ' ═══════════════════════════════════════════════════════════════════
            _captureDevice.Open(config)
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Device opened with optimized configuration")

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 4: Apply Optimized BPF Filter
            ' ═══════════════════════════════════════════════════════════════════
            ' Filter by source IP only — avoids dropping packets if LiDAR sends
            ' from an unexpected source port (e.g. ephemeral or firmware default)
            Dim filter As String = $"udp and src host {LidarIpAddress} and greater 100"
            _captureDevice.Filter = filter
            HandleUserMessageLogging("GMRC", $"{logPrefix}: BPF filter applied: {filter}")

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 5: Open Dump File
            ' ═══════════════════════════════════════════════════════════════════
            _dumpFile = New CaptureFileWriterDevice(pcapFilename, FileMode.Create)
            _dumpFile.Open()

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 6: Create Event Logger
            ' ═══════════════════════════════════════════════════════════════════
            Try
                _eventLogger = New LidarEventLogger(pcapFilename)
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Event logger created")
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Event logger failed: {ex.Message}")
            End Try

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 7: Reset Statistics
            ' ═══════════════════════════════════════════════════════════════════
            _packetCount = 0
            _totalBytes = 0
            _markerCounter = 0
            _frameCounter = 0
            _droppedPackets = 0
            _checksumErrors = 0
            _outOfOrderPackets = 0
            _hesaiSequenceInitialized = False
            _gateDeniedCount = 0
            _handlerErrorCount = 0
            LastPacketTimestamp = Nothing  ' Reset so alerts don't fire with stale timestamp from prior sequence

            ' Drain any leftover markers from a previous capture
            Dim discardedMarker As EventMarker = Nothing
            While _markerQueue.TryDequeue(discardedMarker)
                ' Discard
            End While

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 8: Register Packet Handler via VB.NET Bridge (eliminates VB.NET ref struct limitation)
            ' ═══════════════════════════════════════════════════════════════════
            _isCapturing = True
            Interlocked.Exchange(_captureStartedAt, DateTime.Now.Ticks)
            AddHandler _eventBridge.PacketArrived, AddressOf OnPacketArrived
            _eventBridge.Subscribe(_captureDevice)

            ' ═══════════════════════════════════════════════════════════════════
            ' DIAGNOSTIC: Subscribe to SharpPcap's OnCaptureStopped to detect
            ' silent internal halts (driver error, device reset, etc.)
            ' ═══════════════════════════════════════════════════════════════════
            AddHandler _captureDevice.OnCaptureStopped, AddressOf OnCaptureStopped

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 9: Start SharpPcap Capture (on main thread - confirms active before returning)
            ' ═══════════════════════════════════════════════════════════════════
            _captureDevice.StartCapture()

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 10: Start Marker Pump Thread
            ' Handles marker injection and periodic stats - packet I/O is done by SharpPcap internally
            ' ═══════════════════════════════════════════════════════════════════
            _captureThread = New Thread(AddressOf CaptureLoop) With {
                .IsBackground = True,
                .Name = $"LidarMarkerPump_{DeviceId}",
                .Priority = ThreadPriority.Normal
            }
            _captureThread.Start()

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 11: Success Logging & Marker Injection
            ' ═══════════════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Capture started (native Npcap NDIS 6, seq {sequence:D2})")
            InjectEventMarker("START", $"Recording started - {DeviceId}", sequence)

            StatusNotifier.Toast($"LiDAR {DeviceId} capture started (NDIS 6 LWF)", ToastKind.Info, "LiDAR", 3000, True)
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} {DeviceId} started (NDIS 6)", True)

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 12: Optional Hesai SDK Registration (Validation-Only)
            ' ═══════════════════════════════════════════════════════════════════
            If HesaiInterop.IsAvailable() AndAlso Not IsHesaiRegistered Then
                Task.Run(Sub()
                             Try
                                 If HesaiInterop.RegisterDeviceValidationOnly(DeviceId, LidarIpAddress, CInt(LidarDataPort)) Then
                                     IsHesaiRegistered = True
                                     HandleUserMessageLogging("GMRC", $"{logPrefix}: ✅ Hesai SDK registered (validation-only)")
                                 End If
                             Catch hesaiEx As Exception
                                 HandleUserMessageLogging("GMRC", $"{logPrefix}: Hesai SDK error: {hesaiEx.Message}")
                             End Try
                         End Sub)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}", DisplayMsgBox)
            Try
                If _eventBridge IsNot Nothing Then
                    _eventBridge.Unsubscribe()
                    RemoveHandler _eventBridge.PacketArrived, AddressOf OnPacketArrived
                    _eventBridge = Nothing
                End If
            Catch
                ' Ignore - handler may not have been added yet
            End Try
            CleanupResources()
            _isCapturing = False
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NATIVE: Event-driven packet handler (replaces polling loop + reflection)
    ''' Called via C# bridge - no reflection needed!
    ''' C# bridge signature: EventHandler(Of PacketArrivedEventArgs)
    ''' </summary>
    Private Sub OnPacketArrived(sender As Object, e As PacketArrivedEventArgs)
        Try
            ' ✅ NATIVE: Access RawCapture directly from strongly-typed EventArgs
            Dim rawPacket As RawCapture = e.Packet

            ' Update health timestamp FIRST — before any parsing that can throw.
            ' This ensures a PacketDotNet parse failure never makes the device appear
            ' silent to the health monitor (root cause of false "stopped responding" alerts).
            LastPacketTimestamp = DateTime.Now

            ' Parse packet using PacketDotNet
            Dim packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data)

            ' Write to PCAP dump file
            If _dumpFile IsNot Nothing AndAlso _isCapturing Then
                _dumpFile.Write(rawPacket)

                ' Update counters (thread-safe)
                Interlocked.Increment(_frameCounter)
                Interlocked.Increment(_packetCount)
                Interlocked.Add(_totalBytes, rawPacket.Data.Length)

                ' ═══════════════════════════════════════════════════════════════
                ' Parse Hesai packet for health monitoring (every 100th packet)
                ' ═══════════════════════════════════════════════════════════════
                If _packetCount Mod 100 = 0 Then
                    Try
                        UpdateStatisticsFromPacket(packet)
                    Catch parseEx As Exception
                        If _packetCount Mod 10000 = 0 Then
                            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Parse error: {parseEx.Message}")
                        End If
                    End Try
                End If
            Else
                ' ⚠️ DIAGNOSTIC: Packet received but write gate is closed — log every 50th occurrence
                Dim gateDrops = Interlocked.Increment(_gateDeniedCount)
                If gateDrops = 1 OrElse gateDrops Mod 50 = 0 Then
                    HandleUserMessageLogging("GMRC",
                        $"[{DeviceId}] DIAG: Packet gate closed (drop #{gateDrops}) — " &
                        $"_isCapturing={_isCapturing}, dumpFile={(If(_dumpFile Is Nothing, "NULL", "open"))}, " &
                        $"pkts_so_far={_packetCount:N0}, ts={DateTime.Now:HH:mm:ss.fff}")
                End If
            End If

        Catch ex As Exception
            ' Don't crash capture on single packet errors
            Dim handlerErrors = Interlocked.Increment(_handlerErrorCount)
            If handlerErrors = 1 OrElse handlerErrors Mod 100 = 0 Then
                HandleUserMessageLogging("GMRC",
                    $"[{DeviceId}] DIAG: Packet handler error #{handlerErrors}: {ex.GetType().Name}: {ex.Message} " &
                    $"(pkts={_packetCount:N0}, ts={DateTime.Now:HH:mm:ss.fff})")
            End If
        End Try
    End Sub

    ''' <summary>
    ''' DIAGNOSTIC: Fires when SharpPcap's internal capture thread stops — normal shutdown or error.
    ''' This is the primary signal for silent driver-level halts.
    ''' </summary>
    Private Sub OnCaptureStopped(sender As Object, e As CaptureStoppedEventStatus)
        HandleUserMessageLogging("GMRC",
            $"[{DeviceId}] DIAG: ⚠️ SharpPcap OnCaptureStopped fired — " &
            $"status={e}, isCapturing={_isCapturing}, " &
            $"pkts={_packetCount:N0}, gateDenied={Interlocked.Read(_gateDeniedCount)}, " &
            $"handlerErrors={Interlocked.Read(_handlerErrorCount)}, " &
            $"lastPkt={(If(LastPacketTimestamp.HasValue, $"{DateTime.Now.Subtract(LastPacketTimestamp.Value).TotalSeconds:F1}s ago", "never"))}, " &
            $"ts={DateTime.Now:HH:mm:ss.fff}")
    End Sub

    ''' <summary>
    ''' ✅ NEW: Update statistics from parsed packet
    ''' </summary>
    Private Sub UpdateStatisticsFromPacket(packet As Packet)
        Try
            ' Extract UDP payload for Hesai packet parsing
            Dim ethPacket = TryCast(packet, EthernetPacket)
            If ethPacket Is Nothing Then Return

            Dim ipPacket = TryCast(ethPacket.PayloadPacket, IPv4Packet)
            If ipPacket Is Nothing Then Return

            Dim udpPacket = TryCast(ipPacket.PayloadPacket, UdpPacket)
            If udpPacket Is Nothing OrElse udpPacket.PayloadData Is Nothing Then Return

            ' Parse Hesai packet structure
            Dim info As HesaiPacketInfo = ParseHesaiPacket(udpPacket.PayloadData)

            If info.IsValid Then
                ' Store last packet info for health monitoring
                _lastHesaiInfo = info

                ' Check for sequence gaps (packet loss detection)
                If _hesaiSequenceInitialized Then
                    Dim expectedSeq As UInteger = _lastHesaiSequence + 100UI
                    Dim gap As Long

                    If info.UdpSequence >= expectedSeq Then
                        gap = CLng(info.UdpSequence) - CLng(expectedSeq)
                    Else
                        ' Handle wrap-around at UInteger.MaxValue
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

        Catch ex As Exception
            ' Silent fail - don't log every parse error
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
    ''' ✅ NEW: Stop capture using SharpPcap API
    ''' </summary>
    Public Sub StopCapture()
        Dim logPrefix As String = $"[{DeviceId}] StopCapture"

        Try
            If Not _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Not capturing")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Stopping capture...")

            ' ═══════════════════════════════════════════════════════════════════
            ' Inject STOP marker before stopping
            ' ═══════════════════════════════════════════════════════════════════
            Dim currentActiveSequence As String = GetCurrentActiveSequence()
            Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)
            InjectEventMarker("STOP", "Recording stopped", currentSeq)

            Thread.Sleep(200) ' Allow marker to be processed

            ' ═══════════════════════════════════════════════════════════════════
            ' Signal capture loop to stop
            ' ═══════════════════════════════════════════════════════════════════
            _isCapturing = False

            ' ═══════════════════════════════════════════════════════════════════
            ' Stop SharpPcap capture (symmetric with StartCapture in StartCapture method)
            ' ═══════════════════════════════════════════════════════════════════
            If _captureDevice IsNot Nothing Then
                _captureDevice.StopCapture()
            End If

            ' ═══════════════════════════════════════════════════════════════════
            ' Remove event handler via C# bridge (prevents memory leaks)
            ' ═══════════════════════════════════════════════════════════════════
            If _eventBridge IsNot Nothing Then
                _eventBridge.Unsubscribe()
                RemoveHandler _eventBridge.PacketArrived, AddressOf OnPacketArrived
                _eventBridge = Nothing
            End If

            ' Remove diagnostic OnCaptureStopped handler
            If _captureDevice IsNot Nothing Then
                RemoveHandler _captureDevice.OnCaptureStopped, AddressOf OnCaptureStopped
            End If

            ' ═══════════════════════════════════════════════════════════════════
            ' Wait for marker pump thread to finish gracefully
            ' ═══════════════════════════════════════════════════════════════════
            If _captureThread IsNot Nothing AndAlso _captureThread.IsAlive Then
                If Not _captureThread.Join(5000) Then
                    HandleUserMessageLogging("GMRC", $"{logPrefix}: ⚠️ Thread did not exit gracefully, forcing abort")
                    _captureThread.Abort()
                End If
                _captureThread = Nothing
            End If

            ' ═══════════════════════════════════════════════════════════════════
            ' Close Resources
            ' ═══════════════════════════════════════════════════════════════════

            ' Close event logger
            If _eventLogger IsNot Nothing Then
                _eventLogger.Close()
                _eventLogger = Nothing
            End If

            ' Close dump file
            If _dumpFile IsNot Nothing Then
                _dumpFile.Close()
                _dumpFile = Nothing
            End If

            ' Close capture device
            If _captureDevice IsNot Nothing Then
                _captureDevice.Close()
                ' Don't set to Nothing - keep reference for next capture
            End If

            ' ═══════════════════════════════════════════════════════════════════
            ' Log Final Statistics
            ' ═══════════════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", $"{logPrefix}: ✅ Stopped - {_packetCount:N0} pkts, {_droppedPackets:N0} drops, {_markerCounter} markers")

            StatusNotifier.Toast($"LiDAR {DeviceId} capture stopped", ToastKind.Info, "LiDAR", 3000, True)
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} {DeviceId} stopped - {_packetCount:N0} pkts", True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Error: {ex.Message}")
        End Try
    End Sub



    ' ====================================================================
    ' Shared-NIC capture support (multiple LiDARs on the same adapter)
    ' ====================================================================

    ''' <summary>
    ''' Opens the PCAP dump file and starts the marker-pump thread for this device
    ''' when capture is managed externally by a SharedNicCapture instance.
    ''' The NIC handle is NOT opened here — SharedNicCapture owns it.
    ''' </summary>
    Public Sub StartCaptureShared(pcapFilename As String, sequence As Integer)
        Dim logPrefix As String = $"[{DeviceId}] StartCaptureShared"

        Try
            If _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Already active")
                Return
            End If

            ' Ensure output directory exists
            Dim outputDir = Path.GetDirectoryName(pcapFilename)
            If Not String.IsNullOrEmpty(outputDir) AndAlso Not Directory.Exists(outputDir) Then
                Directory.CreateDirectory(outputDir)
            End If

            ' Open dump file
            _dumpFile = New CaptureFileWriterDevice(pcapFilename, FileMode.Create)
            _dumpFile.Open()

            ' Create event logger
            Try
                _eventLogger = New LidarEventLogger(pcapFilename)
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Event logger failed: {ex.Message}")
            End Try

            ' Reset statistics
            _packetCount = 0
            _totalBytes = 0
            _markerCounter = 0
            _frameCounter = 0
            _droppedPackets = 0
            _checksumErrors = 0
            _outOfOrderPackets = 0
            _hesaiSequenceInitialized = False
            _gateDeniedCount = 0
            _handlerErrorCount = 0
            LastPacketTimestamp = Nothing  ' Reset so alerts don't fire with stale timestamp from prior sequence

            ' ✅ CRITICAL: Drain any stale markers from prior sequence before starting new capture
            ' This ensures markers from the old sequence don't leak into the new PCAP file
            Dim discardedMarker As EventMarker = Nothing
            Dim drainedCount As Integer = 0
            While _markerQueue.TryDequeue(discardedMarker)
                drainedCount += 1
            End While
            If drainedCount > 0 Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Drained {drainedCount} stale marker(s) from prior sequence")
            End If

            _isCapturing = True
            Interlocked.Exchange(_captureStartedAt, DateTime.Now.Ticks)

            ' Start marker-pump thread
            _captureThread = New Thread(AddressOf CaptureLoop) With {
                .IsBackground = True,
                .Name = $"LidarMarkerPump_{DeviceId}",
                .Priority = ThreadPriority.Normal
            }
            _captureThread.Start()

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Dump file open, marker pump started (shared NIC, seq {sequence:D2})")
            InjectEventMarker("START", $"Recording started - {DeviceId}", sequence)

            StatusNotifier.Toast($"LiDAR {DeviceId} capture started (shared NIC)", ToastKind.Info, "LiDAR", 3000, True)
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} {DeviceId} started (shared NIC)", True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}", DisplayMsgBox)
            CleanupResources()
            _isCapturing = False
        End Try
    End Sub

    ''' <summary>
    ''' Writes a raw packet (already dispatched by the SharedNicCapture fan-out) to
    ''' this device's dump file and updates health/statistics counters.
    ''' </summary>
    Public Sub DispatchPacket(rawPacket As RawCapture)
        Try
            ' Update health timestamp first — before any parse that could throw
            LastPacketTimestamp = DateTime.Now

            If _dumpFile IsNot Nothing AndAlso _isCapturing Then
                _dumpFile.Write(rawPacket)

                Interlocked.Increment(_frameCounter)
                Interlocked.Increment(_packetCount)
                Interlocked.Add(_totalBytes, rawPacket.Data.Length)

                ' Parse for statistics every 100th packet
                If _packetCount Mod 100 = 0 Then
                    Try
                        Dim parsed = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data)
                        UpdateStatisticsFromPacket(parsed)
                    Catch
                        ' Silent — timestamp already updated above
                    End Try
                End If
            Else
                Dim gateDrops = Interlocked.Increment(_gateDeniedCount)
                If gateDrops = 1 OrElse gateDrops Mod 50 = 0 Then
                    HandleUserMessageLogging("GMRC",
                        $"[{DeviceId}] DIAG: Packet gate closed (drop #{gateDrops}) — " &
                        $"_isCapturing={_isCapturing}, dumpFile={(If(_dumpFile Is Nothing, "NULL", "open"))}, " &
                        $"pkts_so_far={_packetCount:N0}, ts={DateTime.Now:HH:mm:ss.fff}")
                End If
            End If

        Catch ex As Exception
            Dim handlerErrors = Interlocked.Increment(_handlerErrorCount)
            If handlerErrors = 1 OrElse handlerErrors Mod 100 = 0 Then
                HandleUserMessageLogging("GMRC",
                    $"[{DeviceId}] DIAG: DispatchPacket error #{handlerErrors}: {ex.GetType().Name}: {ex.Message}")
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Stops the marker-pump thread and closes the dump file for this device
    ''' when capture is managed externally by a SharedNicCapture instance.
    ''' The NIC handle is NOT closed here — SharedNicCapture owns it.
    ''' </summary>
    Public Sub StopCaptureShared()
        Dim logPrefix As String = $"[{DeviceId}] StopCaptureShared"

        Try
            If Not _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Not capturing")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Stopping...")

            Dim currentSeq As Integer = GetSequenceNumberFromFileName(GetCurrentActiveSequence())
            InjectEventMarker("STOP", "Recording stopped", currentSeq)
            Thread.Sleep(200)

            _isCapturing = False

            ' Wait for marker-pump thread
            If _captureThread IsNot Nothing AndAlso _captureThread.IsAlive Then
                If Not _captureThread.Join(5000) Then
                    HandleUserMessageLogging("GMRC", $"{logPrefix}: ⚠️ Thread did not exit, forcing abort")
                    _captureThread.Abort()
                End If
                _captureThread = Nothing
            End If

            ' Close event logger
            If _eventLogger IsNot Nothing Then
                _eventLogger.Close()
                _eventLogger = Nothing
            End If

            ' Close dump file
            If _dumpFile IsNot Nothing Then
                _dumpFile.Close()
                _dumpFile = Nothing
            End If

            HandleUserMessageLogging("GMRC",
                $"{logPrefix}: ✅ Stopped — {_packetCount:N0} pkts, {_droppedPackets:N0} drops, {_markerCounter} markers")

            StatusNotifier.Toast($"LiDAR {DeviceId} capture stopped", ToastKind.Info, "LiDAR", 3000, True)
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} {DeviceId} stopped — {_packetCount:N0} pkts", True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Error: {ex.Message}")
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
    ''' Marker pump thread: drains the marker queue and updates periodic statistics.
    ''' Packet I/O is handled entirely by SharpPcap's internal thread via OnPacketArrival.
    ''' </summary>
    Private Sub CaptureLoop()
        Try
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Marker pump thread started")

            Dim lastStatsUpdate As DateTime = DateTime.Now
            Dim lastWatchdogCheck As DateTime = DateTime.Now
            Dim lastWatchdogPacketCount As Long = 0
            Dim starvationWarned As Boolean = False

            While _isCapturing
                ' ═══════════════════════════════════════════════════════════════
                ' 1. Process Pending Event Markers
                ' ═══════════════════════════════════════════════════════════════
                Dim marker As EventMarker = Nothing
                While _markerQueue.TryDequeue(marker)
                    InjectMarkerPacket(marker)
                End While

                ' ═══════════════════════════════════════════════════════════════
                ' 2. Update Statistics from Device (every second)
                ' ═══════════════════════════════════════════════════════════════
                If DateTime.Now.Subtract(lastStatsUpdate).TotalSeconds >= 1.0 Then
                    UpdateDeviceStatistics()
                    lastStatsUpdate = DateTime.Now
                End If

                ' ═══════════════════════════════════════════════════════════════
                ' 3. DIAGNOSTIC: Packet starvation watchdog (every 10s)
                '    Fires if SharpPcap's internal thread has stopped delivering packets
                ' ═══════════════════════════════════════════════════════════════
                If DateTime.Now.Subtract(lastWatchdogCheck).TotalSeconds >= 10.0 Then
                    Dim currentPktCount = Interlocked.Read(_packetCount)
                    Dim pktsDelta = currentPktCount - lastWatchdogPacketCount

                    If pktsDelta = 0 AndAlso _packetCount > 0 Then
                        ' No new packets in the last 10 seconds
                        Dim timeSinceLast = If(LastPacketTimestamp.HasValue,
                            DateTime.Now.Subtract(LastPacketTimestamp.Value).TotalSeconds,
                            Double.MaxValue)

                        HandleUserMessageLogging("GMRC",
                            $"[{DeviceId}] DIAG: ⚠️ STARVATION — 0 packets in last 10s " &
                            $"(total={currentPktCount:N0}, last_pkt={timeSinceLast:F1}s ago, " &
                            $"isCapturing={_isCapturing}, " &
                            $"dumpFile={(If(_dumpFile Is Nothing, "NULL", "open"))}, " &
                            $"gateDenied={Interlocked.Read(_gateDeniedCount)}, " &
                            $"handlerErrors={Interlocked.Read(_handlerErrorCount)}, " &
                            $"ts={DateTime.Now:HH:mm:ss.fff})")
                        starvationWarned = True
                    ElseIf starvationWarned AndAlso pktsDelta > 0 Then
                        HandleUserMessageLogging("GMRC",
                            $"[{DeviceId}] DIAG: ✅ RECOVERED — {pktsDelta:N0} pkts in last 10s " &
                            $"(total={currentPktCount:N0}, ts={DateTime.Now:HH:mm:ss.fff})")
                        starvationWarned = False
                    End If

                    lastWatchdogPacketCount = currentPktCount
                    lastWatchdogCheck = DateTime.Now
                End If

                ' Sleep to avoid busy-waiting (marker queue is not high-frequency)
                Thread.Sleep(100)  ' Check every 100ms
            End While

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Marker pump thread exiting normally (pkts={_packetCount:N0})")

        Catch ex As ThreadAbortException
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] DIAG: Marker pump thread ABORTED (pkts={_packetCount:N0})")
        Catch ex As Exception
            HandleUserMessageLogging("GMRC",
                $"[{DeviceId}] DIAG: ⚠️ Capture loop CRASHED: {ex.GetType().Name}: {ex.Message} " &
                $"(pkts={_packetCount:N0}, isCapturing={_isCapturing}, ts={DateTime.Now:HH:mm:ss.fff})")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Update statistics from SharpPcap device (NDIS 6 provides accurate stats)
    ''' </summary>
    Private Sub UpdateDeviceStatistics()
        Try
            ' PRIORITY 1: Get stats from native Npcap driver (kernel-level buffer drops)
            ' Use a monotonic update — only advance the counter, never reset it.
            ' stats.DroppedPackets is typically 0 with a 16 MB kernel buffer; an Exchange
            ' with 0 would silently wipe accumulated Hesai sequence-gap drops.
            If TypeOf _captureDevice Is LibPcapLiveDevice Then
                Dim liveDevice = DirectCast(_captureDevice, LibPcapLiveDevice)
                Dim npcapDrops = CLng(liveDevice.Statistics.DroppedPackets)
                If npcapDrops > Interlocked.Read(_droppedPackets) Then
                    Interlocked.Exchange(_droppedPackets, npcapDrops)
                End If
            End If

            ' PRIORITY 2: Hesai SDK stats — same monotonic rule for out-of-order counter.
            ' Hesai SDK out_of_order_packets is cumulative from SDK registration; an Exchange
            ' with a stale/lower value would wipe gaps already counted by UpdateStatisticsFromPacket.
            If HesaiInterop.IsAvailable() Then
                Dim hesaiStats = HesaiInterop.GetDeviceStats(DeviceId)
                If hesaiStats.packets_received > 0 Then
                    Interlocked.Exchange(_checksumErrors, CLng(hesaiStats.checksum_errors))

                    Dim sdkOoo = CLng(hesaiStats.out_of_order_packets)
                    If sdkOoo > Interlocked.Read(_outOfOrderPackets) Then
                        Interlocked.Exchange(_outOfOrderPackets, sdkOoo)
                    End If
                End If
            End If

        Catch ex As Exception
            ' Don't crash capture on stats update failures
            If _packetCount Mod 10000 = 0 Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] Stats update error: {ex.Message}")
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
            ' ✅ FIXED: Use TryCast instead of GetEncapsulated
            Dim eth = TryCast(packet, EthernetPacket)
            If eth IsNot Nothing Then
                Dim ipv4 = TryCast(eth.PayloadPacket, IPv4Packet)
                If ipv4 IsNot Nothing Then
                    Dim udp = TryCast(ipv4.PayloadPacket, UdpPacket)
                    If udp IsNot Nothing Then
                        Return udp.DestinationPort = MarkerDestPort AndAlso
                               udp.SourcePort = MarkerSourcePort
                    End If
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
            ' ✅ FIXED: Use TryCast and navigate through packet layers
            Dim eth = TryCast(packet, EthernetPacket)
            If eth IsNot Nothing Then
                Dim ipv4 = TryCast(eth.PayloadPacket, IPv4Packet)
                If ipv4 IsNot Nothing Then
                    Dim udp = TryCast(ipv4.PayloadPacket, UdpPacket)
                    If udp IsNot Nothing AndAlso udp.PayloadData IsNot Nothing AndAlso udp.PayloadData.Length > 0 Then
                        Return System.Text.Encoding.UTF8.GetString(udp.PayloadData)
                    End If
                End If
            End If
        Catch
            ' Ignore parsing errors
        End Try
        Return "(unable to parse)"
    End Function

    ''' <summary>
    ''' ✅ NEW: Inject marker packet using PacketDotNet
    ''' </summary>
    Private Sub InjectMarkerPacket(marker As EventMarker)
        Try
            If _dumpFile Is Nothing OrElse Not _isCapturing Then Return

            ' ═══════════════════════════════════════════════════════════════════
            ' Use GPS-synchronized timestamp if available
            ' ═══════════════════════════════════════════════════════════════════
            Dim timestamp As DateTime = marker.Timestamp
            If _timeSyncProvider IsNot Nothing AndAlso _timeSyncProvider.IsSynchronized() Then
                timestamp = _timeSyncProvider.GetSynchronizedTimestamp()
            End If

            ' ═══════════════════════════════════════════════════════════════════
            ' Build Marker Payload (pipe-delimited format)
            ' ═══════════════════════════════════════════════════════════════════
            Dim payload As String = $"{marker.EventType}|{marker.Message}|{marker.SequenceNumber:D2}|GPS:{timestamp:yyyy-MM-dd HH:mm:ss.fff}"
            Dim payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload)

            ' ═══════════════════════════════════════════════════════════════════
            ' Build Ethernet Frame using PacketDotNet
            ' ═══════════════════════════════════════════════════════════════════
            Dim srcMac = PhysicalAddress.Parse("02-00-00-00-00-01")
            Dim dstMac = PhysicalAddress.Parse("02-00-00-00-00-02")

            Dim ethPacket As New EthernetPacket(srcMac, dstMac, EthernetType.IPv4)

            ' ═══════════════════════════════════════════════════════════════════
            ' Build IP Packet
            ' ═══════════════════════════════════════════════════════════════════
            Dim srcIp = Net.IPAddress.Parse("192.168.40.200")
            Dim dstIp = Net.IPAddress.Parse("192.168.40.255")  ' Broadcast

            Dim ipPacket As New IPv4Packet(srcIp, dstIp) With {
            .TimeToLive = 128,
            .Protocol = ProtocolType.Udp
        }

            ' ═══════════════════════════════════════════════════════════════════
            ' Build UDP Packet
            ' ═══════════════════════════════════════════════════════════════════
            Dim udpPacket As New UdpPacket(MarkerSourcePort, MarkerDestPort) With {
            .PayloadData = payloadBytes
        }

            ' ═══════════════════════════════════════════════════════════════════
            ' Link Packet Layers
            ' ═══════════════════════════════════════════════════════════════════
            ipPacket.PayloadPacket = udpPacket
            ethPacket.PayloadPacket = ipPacket

            ' Update checksums (critical for valid PCAP)
            udpPacket.UpdateUdpChecksum()
            ipPacket.UpdateIPChecksum()

            ' ═══════════════════════════════════════════════════════════════════
            ' Write to PCAP Dump File
            ' ═══════════════════════════════════════════════════════════════════
            Dim posixTime As New PosixTimeval(timestamp)
            Dim rawPacket As New RawCapture(LinkLayers.Ethernet, posixTime, ethPacket.Bytes)

            _dumpFile.Write(rawPacket)

            ' ═══════════════════════════════════════════════════════════════════
            ' Update Counters & Log to Sidecar File
            ' ═══════════════════════════════════════════════════════════════════
            Interlocked.Increment(_frameCounter)
            _eventLogger?.LogEvent(_frameCounter, timestamp, marker.EventType, marker.Message, marker.SequenceNumber)

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] ✅ Marker injected: Frame {_frameCounter} - {marker.EventType}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] InjectMarkerPacket error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Find network adapter using SharpPcap API
    ''' Priority: 1) GUID match, 2) IP subnet match, 3) First IPv4 adapter
    ''' </summary>
    Private Function FindNetworkAdapter() As Boolean
        Try
            ' ═══════════════════════════════════════════════════════════════════
            ' Get all capture devices from SharpPcap
            ' ═══════════════════════════════════════════════════════════════════
            Dim devices = CaptureDeviceList.Instance

            If devices.Count = 0 Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] No devices found. Install Npcap.")
                Return False
            End If

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Found {devices.Count} network device(s)")

            ' ═══════════════════════════════════════════════════════════════════
            ' PRIORITY 1: Match by Network Adapter GUID
            ' ═══════════════════════════════════════════════════════════════════
            If Not String.IsNullOrWhiteSpace(LidarAdapterGuid) Then
                Dim guidToMatch = LidarAdapterGuid.Replace("{", "").Replace("}", "").ToUpper()
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] Searching for GUID: {guidToMatch}")

                For Each device As ICaptureDevice In devices
                    If device.Name.ToUpper().Contains(guidToMatch) Then
                        _captureDevice = device
                        HandleUserMessageLogging("GMRC", $"[{DeviceId}] ✅ Selected by GUID: {device.Description}")
                        Return True
                    End If
                Next

                HandleUserMessageLogging("GMRC", $"[{DeviceId}] GUID not found, trying subnet match...")
            End If

            ' ═══════════════════════════════════════════════════════════════════
            ' PRIORITY 2: Match by IP Subnet (e.g., "10.5.55.x")
            ' ═══════════════════════════════════════════════════════════════════
            Dim subnet = LidarIpAddress.Substring(0, LidarIpAddress.LastIndexOf("."c))
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Searching for subnet: {subnet}.x")

            For Each device As ICaptureDevice In devices
                If TypeOf device Is LibPcapLiveDevice Then
                    Dim liveDevice = DirectCast(device, LibPcapLiveDevice)

                    For Each addr In liveDevice.Addresses
                        If addr.Addr IsNot Nothing Then
                            Dim addrStr = addr.Addr.ToString()

                            ' Check if adapter IP is on same subnet as LiDAR
                            If addrStr.StartsWith(subnet & ".") AndAlso
                           Not addrStr.Contains(":") Then  ' Exclude IPv6
                                _captureDevice = device
                                HandleUserMessageLogging("GMRC", $"[{DeviceId}] ✅ Selected by subnet: {device.Description} ({addrStr})")
                                Return True
                            End If
                        End If
                    Next
                End If
            Next

            ' ═══════════════════════════════════════════════════════════════════
            ' PRIORITY 3: Fallback to First IPv4 Adapter
            ' ═══════════════════════════════════════════════════════════════════
            For Each device As ICaptureDevice In devices
                If TypeOf device Is LibPcapLiveDevice Then
                    Dim liveDevice = DirectCast(device, LibPcapLiveDevice)

                    For Each addr In liveDevice.Addresses
                        If addr.Addr IsNot Nothing Then
                            Dim addrStr = addr.Addr.ToString()

                            If Not addrStr.Contains(":") Then  ' IPv4 only
                                _captureDevice = device
                                HandleUserMessageLogging("GMRC", $"[{DeviceId}] ⚠️ Using fallback adapter: {device.Description} ({addrStr})")
                                Return True
                            End If
                        End If
                    Next
                End If
            Next

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] ❌ No suitable adapter found")
            Return False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] FindNetworkAdapter error: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' ✅ UPDATED: Cleanup resources (called internally and from Dispose)
    ''' </summary>
    Private Sub CleanupResources()
        Try
            ' Close dump file
            If _dumpFile IsNot Nothing Then
                Try
                    _dumpFile.Close()
                Catch
                    ' Ignore disposal errors
                End Try
                _dumpFile = Nothing
            End If

            ' Close capture device
            If _captureDevice IsNot Nothing Then
                Try
                    _captureDevice.Close()
                Catch
                    ' Ignore disposal errors
                End Try
                ' Don't set to Nothing - may reuse for next capture
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] CleanupResources: {ex.Message}")
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

