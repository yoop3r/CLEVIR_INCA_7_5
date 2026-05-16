Option Strict On
Imports System.IO
Imports System.Threading
Imports System.Runtime
Imports SharpPcap
Imports SharpPcap.LibPcap
Imports PacketDotNet
Imports System.Net.NetworkInformation

''' <summary>
''' Captures OXTS NCOM UDP packets to PCAP file for post-processing ground truth validation
''' Provides time-synchronized 6-DOF pose data alongside LiDAR point clouds
''' ✅ MIGRATED: Now uses SharpPcap with native Npcap NDIS 6 LWF for optimal performance
''' </summary>
Public Class OxtsNcomCaptureDevice

    ' ====================================================================
    ' Configuration (set from XML or defaults)
    ' ====================================================================
    Public Property OxtsIpAddress As String = "10.5.55.200"
    Public Property OxtsNcomPort As UShort = 3000
    Public Property NetworkAdapterGuid As String = ""  ' Same as LiDAR adapter
    Public Property DeviceId As String = "OXTS_NCOM"
    Public Property Enabled As Boolean = True

    ' ====================================================================
    ' ✅ CHANGED: SharpPcap capture device types
    ' ====================================================================
    Private _captureDevice As ICaptureDevice = Nothing
    Private _dumpFile As CaptureFileWriterDevice = Nothing
    Private _captureThread As Thread = Nothing
    Private _isCapturing As Boolean = False
    Private _packetCount As Long = 0
    Private _totalBytes As Long = 0
    Private _droppedPackets As Long = 0
    Private _eventMarkerCounter As Long = 0
    Private _frameCounter As Long = 0
    Private _previousGcLatencyMode As GCLatencyMode = GCLatencyMode.Interactive
    Private Const CaptureBufferSize As Integer = 65536  ' 64KB
    Private Const ReadTimeoutMs As Integer = 1000       ' 1 second
    Public Const MarkerDestPort As UShort = 65002       ' Unique port for event markers (different from LiDAR)
    Public Const MarkerSourcePort As UShort = 65003

    ' Event logger for sidecar text file
    Private _eventLogger As OxtsEventLogger = Nothing

    ' Event marker structure
    Private Structure EventMarker
        Public Timestamp As DateTime
        Public EventType As String
        Public Message As String
        Public SequenceNumber As Integer
    End Structure

    Private ReadOnly _markerQueue As New Concurrent.ConcurrentQueue(Of EventMarker)

    ' ====================================================================
    ' Public Properties
    ' ====================================================================
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

    Public ReadOnly Property TotalBytes As Long
        Get
            Return Interlocked.Read(_totalBytes)
        End Get
    End Property

    Public ReadOnly Property DroppedPackets As Long
        Get
            Return Interlocked.Read(_droppedPackets)
        End Get
    End Property

    Public ReadOnly Property CurrentFrameNumber As Long
        Get
            Return Interlocked.Read(_frameCounter)
        End Get
    End Property

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(adapterGuid As String, oxtsIp As String, ncomPort As UShort, Optional deviceId As String = "OXTS_NCOM")
        Me.NetworkAdapterGuid = adapterGuid  ' ✅ Explicit instance member
        Me.OxtsIpAddress = oxtsIp            ' ✅ Consistent style
        Me.OxtsNcomPort = ncomPort           ' ✅ Clear intent
        Me.DeviceId = deviceId               ' ✅ Professional code
    End Sub

    ''' <summary>
    ''' ✅ NEW VERSION: StartCapture using SharpPcap (native Npcap NDIS 6)
    ''' </summary>
    Public Sub StartCapture(pcapFilename As String, sequence As Integer)
        Dim logPrefix As String = $"[{DeviceId}] StartCapture"

        Try
            If _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Already active")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Initializing NCOM capture to {Path.GetFileName(pcapFilename)}...")

            ' Suppress Gen2 GC collections for the duration of the capture session
            _previousGcLatencyMode = GCSettings.LatencyMode
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency
            HandleUserMessageLogging("GMRC", $"{logPrefix}: GC mode set to SustainedLowLatency (was {_previousGcLatencyMode})")

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 1: Find Network Adapter (SharpPcap API)
            ' ═══════════════════════════════════════════════════════════════════
            If Not FindNetworkAdapter() Then
                Throw New Exception($"Network adapter not found (GUID: {NetworkAdapterGuid})")
            End If

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 2: Configure Device (kernel buffer, immediate mode, snaplen)
            ' ═══════════════════════════════════════════════════════════════════
            Dim config As New DeviceConfiguration() With {
                .Mode = DeviceModes.Promiscuous,
                .ReadTimeout = ReadTimeoutMs,
                .KernelBufferSize = CaptureBufferSize,
                .Snaplen = 65535,
                .Immediate = True
            }
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Config set - KernelBuffer={CaptureBufferSize \ 1024}KB, SnapLen=65535, Immediate=True")

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 3: Open Device with Configuration
            ' ═══════════════════════════════════════════════════════════════════
            _captureDevice.Open(config)
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Device opened with optimized configuration")

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 4: Create Event Logger
            ' ═══════════════════════════════════════════════════════════════════
            Try
                _eventLogger = New OxtsEventLogger(pcapFilename)
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Event logger created")
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Event logger creation failed: {ex.Message}")
            End Try

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 5: Apply BPF Filter
            ' ═══════════════════════════════════════════════════════════════════
            Dim filter As String = $"udp and src host {OxtsIpAddress} and src port {OxtsNcomPort}"
            _captureDevice.Filter = filter
            HandleUserMessageLogging("GMRC", $"{logPrefix}: Filter applied: {filter}")

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 6: Open PCAP Dump File
            ' ═══════════════════════════════════════════════════════════════════
            _dumpFile = New CaptureFileWriterDevice(pcapFilename, FileMode.Create)
            _dumpFile.Open()

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 7: Reset Statistics
            ' ═══════════════════════════════════════════════════════════════════
            _packetCount = 0
            _totalBytes = 0
            _eventMarkerCounter = 0
            _frameCounter = 0
            _droppedPackets = 0

            Dim discardedMarker As EventMarker
            While _markerQueue.TryDequeue(discardedMarker)
            End While

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 8: Register Packet Handler (Event-Driven)
            ' ═══════════════════════════════════════════════════════════════════
            AddHandler _captureDevice.OnPacketArrival, AddressOf OnPacketArrival

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 9: Start SharpPcap Capture
            ' ═══════════════════════════════════════════════════════════════════
            _isCapturing = True
            _captureDevice.StartCapture()

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 10: Start Marker Pump Thread
            ' ═══════════════════════════════════════════════════════════════════
            _captureThread = New Thread(AddressOf CaptureLoop) With {
                .IsBackground = True,
                .Name = $"OxtsMarkerPump_{DeviceId}",
                .Priority = ThreadPriority.Normal
            }
            _captureThread.Start()

            ' ═══════════════════════════════════════════════════════════════════
            ' STEP 11: Success Logging
            ' ═══════════════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", $"{logPrefix}: NCOM capture started (seq {sequence:D2})")
            InjectEventMarker("START", $"OXTS NCOM recording started", sequence)

            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} OXTS NCOM capture started (seq {sequence:D2})", True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}", DisplayMsgBox)
            CleanupResources()
            _isCapturing = False
        End Try
    End Sub

    ''' <summary>
    ''' ✅ UPDATED: StopCapture using SharpPcap
    ''' </summary>
    Public Sub StopCapture()
        Dim logPrefix As String = $"[{DeviceId}] StopCapture"

        Try
            If Not _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Not capturing")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Stopping capture...")

            ' Signal capture thread to stop and stop SharpPcap capture
            _isCapturing = False

            If _captureDevice IsNot Nothing Then
                Try
                    _captureDevice.StopCapture()
                Catch
                    ' Ignore - device may already be stopped
                End Try
            End If

            ' Wait for marker pump thread to finish (with timeout)
            If _captureThread IsNot Nothing AndAlso _captureThread.IsAlive Then
                If Not _captureThread.Join(TimeSpan.FromSeconds(3)) Then
                    HandleUserMessageLogging("GMRC", $"{logPrefix}: Capture thread did not stop gracefully")
                    Try
                        _captureThread.Abort()
                    Catch
                        ' Ignore abort exceptions
                    End Try
                End If
            End If

            ' Remove event handler
            If _captureDevice IsNot Nothing Then
                Try
                    RemoveHandler _captureDevice.OnPacketArrival, AddressOf OnPacketArrival
                Catch
                    ' Handler may not be registered
                End Try
            End If

            ' Close resources
            CleanupResources()

            ' Restore GC latency mode now that the capture session has ended
            GCSettings.LatencyMode = _previousGcLatencyMode
            HandleUserMessageLogging("GMRC", $"{logPrefix}: GC mode restored to {_previousGcLatencyMode}")

            ' Write event to INCA
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} OXTS NCOM capture stopped ({Interlocked.Read(_packetCount):N0} packets)", True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ VERIFIED: Injects an event marker into the PCAP stream (no changes needed)
    ''' </summary>
    Public Sub InjectEventMarker(eventType As String, message As String, sequenceNumber As Integer)
        Try
            Dim marker As New EventMarker With {
                .Timestamp = DateTime.Now,
                .EventType = eventType,
                .Message = message,
                .SequenceNumber = sequenceNumber
            }

            _markerQueue.Enqueue(marker)

            ' Log to sidecar file
            If _eventLogger IsNot Nothing Then
                _eventLogger.LogEvent(Interlocked.Read(_packetCount), marker.Timestamp, eventType, message, sequenceNumber)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] InjectEventMarker: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Event-driven packet handler (SharpPcap native)
    ''' </summary>
    Private Sub OnPacketArrival(sender As Object, e As Object)
        Try
            ' Use reflection to access PacketCapture.GetPacket() (VB.NET can't use ref structs)
            Dim getPacketMethod = e.GetType().GetMethod("GetPacket")
            Dim rawPacket As RawCapture = DirectCast(getPacketMethod.Invoke(e, Nothing), RawCapture)

            ' Write to PCAP dump file
            If _dumpFile IsNot Nothing AndAlso _isCapturing Then
                _dumpFile.Write(rawPacket)

                ' Update counters (thread-safe)
                Interlocked.Increment(_frameCounter)
                Interlocked.Increment(_packetCount)
                Interlocked.Add(_totalBytes, rawPacket.Data.Length)
            End If

        Catch ex As Exception
            ' Don't crash capture on single packet errors
            If _packetCount Mod 10000 = 0 Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] Packet handler error: {ex.Message}")
            End If
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Simplified capture loop (SharpPcap handles packet reception via events)
    ''' This loop only handles event marker injection
    ''' </summary>
    Private Sub CaptureLoop()
        Try
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Marker pump thread started")

            Dim lastStatsUpdate As DateTime = DateTime.Now

            While _isCapturing
                ' Process Pending Event Markers
                Dim marker As EventMarker
                While _markerQueue.TryDequeue(marker)
                    InjectMarkerPacket(marker)
                End While

                ' Update statistics from device (every second)
                If DateTime.Now.Subtract(lastStatsUpdate).TotalSeconds >= 1.0 Then
                    UpdateDeviceStatistics()
                    lastStatsUpdate = DateTime.Now
                End If

                ' Sleep to avoid busy-waiting
                Thread.Sleep(100)
            End While

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Marker pump thread exiting normally")

        Catch ex As ThreadAbortException
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Capture thread aborted")
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Capture loop error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Update statistics from SharpPcap device
    ''' </summary>
    Private Sub UpdateDeviceStatistics()
        Try
            If TypeOf _captureDevice Is LibPcapLiveDevice Then
                Dim liveDevice = DirectCast(_captureDevice, LibPcapLiveDevice)
                Dim stats = liveDevice.Statistics

                ' NDIS 6 LWF provides accurate drop counts at kernel level
                Interlocked.Exchange(_droppedPackets, CLng(stats.DroppedPackets))
            End If

        Catch ex As Exception
            ' Don't crash capture on stats update failures
            If _packetCount Mod 10000 = 0 Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] Stats update error: {ex.Message}")
            End If
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Inject marker packet using PacketDotNet
    ''' </summary>
    Private Sub InjectMarkerPacket(marker As EventMarker)
        Try
            If _dumpFile Is Nothing OrElse Not _isCapturing Then Return

            Interlocked.Increment(_eventMarkerCounter)

            ' Build marker payload: "OXTS_EVENT|TYPE|SEQ|MSG"
            Dim payload As String = $"OXTS_EVENT|{marker.EventType}|{marker.SequenceNumber:D2}|{marker.Message}"
            Dim payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload)

            ' Build Ethernet Frame using PacketDotNet
            Dim srcMac = PhysicalAddress.Parse("02-00-00-00-00-03")
            Dim dstMac = PhysicalAddress.Parse("02-00-00-00-00-04")

            Dim ethPacket As New EthernetPacket(srcMac, dstMac, EthernetType.IPv4)

            ' Build IP Packet
            Dim srcIp = Net.IPAddress.Parse("192.168.40.200")
            Dim dstIp = Net.IPAddress.Parse("192.168.40.255")  ' Broadcast

            Dim ipPacket As New IPv4Packet(srcIp, dstIp) With {
                .TimeToLive = 128,
                .Protocol = ProtocolType.Udp
            }

            ' Build UDP Packet
            Dim udpPacket As New UdpPacket(MarkerSourcePort, MarkerDestPort) With {
                .PayloadData = payloadBytes
            }

            ' Link Packet Layers
            ipPacket.PayloadPacket = udpPacket
            ethPacket.PayloadPacket = ipPacket

            ' Update checksums
            udpPacket.UpdateUdpChecksum()
            ipPacket.UpdateIPChecksum()

            ' Write to PCAP Dump File
            Dim posixTime As New PosixTimeval(marker.Timestamp)
            Dim rawPacket As New RawCapture(LinkLayers.Ethernet, posixTime, ethPacket.Bytes)

            _dumpFile.Write(rawPacket)

            ' Update counters
            Interlocked.Increment(_frameCounter)

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] ✅ Marker #{_eventMarkerCounter}: {marker.EventType} - {marker.Message}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] InjectMarkerPacket error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Find network adapter using SharpPcap API
    ''' </summary>
    Private Function FindNetworkAdapter() As Boolean
        Try
            If String.IsNullOrEmpty(NetworkAdapterGuid) Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] Network adapter GUID not configured")
                Return False
            End If

            Dim devices = CaptureDeviceList.Instance

            If devices.Count = 0 Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] No network adapters found")
                Return False
            End If

            ' Match by GUID
            Dim guidToMatch = NetworkAdapterGuid.Replace("{", "").Replace("}", "").ToUpper()

            For Each device As ICaptureDevice In devices
                If device.Name.ToUpper().Contains(guidToMatch) Then
                    _captureDevice = device
                    HandleUserMessageLogging("GMRC", $"[{DeviceId}] Found adapter: {device.Description}")
                    Return True
                End If
            Next

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Adapter not found (GUID: {NetworkAdapterGuid})")
            Return False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] FindNetworkAdapter: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' ✅ UPDATED: CleanupResources for SharpPcap
    ''' </summary>
    Private Sub CleanupResources()
        Try
            ' Close PCAP dump file
            If _dumpFile IsNot Nothing Then
                Try
                    _dumpFile.Close()
                    _dumpFile.Dispose()
                Catch
                    ' Ignore disposal errors
                End Try
                _dumpFile = Nothing
            End If

            ' Close capture device
            If _captureDevice IsNot Nothing Then
                Try
                    If _captureDevice.Started Then
                        _captureDevice.StopCapture()
                    End If
                    _captureDevice.Close()
                Catch
                    ' Ignore close errors
                End Try
                _captureDevice = Nothing
            End If

            ' Close event logger
            If _eventLogger IsNot Nothing Then
                Try
                    _eventLogger.Close()
                Catch
                    ' Ignore close errors
                End Try
                _eventLogger = Nothing
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] CleanupResources: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ VERIFIED: Gets capture statistics (thread-safe reads)
    ''' </summary>
    Public Function GetStatistics() As String
        Return $"Packets: {Interlocked.Read(_packetCount):N0} | Bytes: {Interlocked.Read(_totalBytes):N0} | Dropped: {Interlocked.Read(_droppedPackets):N0} | Markers: {Interlocked.Read(_eventMarkerCounter):N0} | Frame: {Interlocked.Read(_frameCounter):N0}"
    End Function

End Class

''' <summary>
''' Event logger for OXTS capture (creates sidecar .oxts_events.txt file)
''' </summary>
Public Class OxtsEventLogger
    Private eventLogPath As String
    Private eventLogWriter As StreamWriter
    Private syncLockObj As New Object()

    Public Sub New(pcapFilePath As String)
        ' Create sidecar log file (e.g., "Recording_01_OXTS.pcap" -> "Recording_01_OXTS.oxts_events.txt")
        eventLogPath = Path.ChangeExtension(pcapFilePath, ".oxts_events.txt")
        eventLogWriter = New StreamWriter(eventLogPath, append:=False)

        ' Write header
        eventLogWriter.WriteLine("# OXTS NCOM Event Marker Log")
        eventLogWriter.WriteLine($"# PCAP File: {Path.GetFileName(pcapFilePath)}")
        eventLogWriter.WriteLine($"# Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
        eventLogWriter.WriteLine("#")
        eventLogWriter.WriteLine("# Format: PacketNumber | Timestamp | EventType | Message | SequenceNumber")
        eventLogWriter.WriteLine("# -------------------------------------------------------------------------")
        eventLogWriter.Flush()
    End Sub

    Public Sub LogEvent(packetNumber As Long, timestamp As DateTime, eventType As String, message As String, sequenceNumber As Integer)
        SyncLock syncLockObj
            Try
                Dim line As String = $"{packetNumber}|{timestamp:yyyy-MM-dd HH:mm:ss.fff}|{eventType}|{message}|{sequenceNumber}"
                eventLogWriter.WriteLine(line)
                eventLogWriter.Flush()
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"OxtsEventLogger.LogEvent: {ex.Message}")
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
