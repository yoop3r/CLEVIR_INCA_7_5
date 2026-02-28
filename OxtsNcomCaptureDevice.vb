Option Strict On
Imports System.IO
Imports System.Threading
Imports PcapDotNet.Core
Imports PcapDotNet.Packets
Imports PcapDotNet.Packets.Ethernet
Imports PcapDotNet.Packets.IpV4
Imports PcapDotNet.Packets.Transport

''' <summary>
''' Captures OXTS NCOM UDP packets to PCAP file for post-processing ground truth validation
''' Provides time-synchronized 6-DOF pose data alongside LiDAR point clouds
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
    ' Capture State
    ' ====================================================================
    Private _captureDevice As LivePacketDevice = Nothing
    Private _communicator As PacketCommunicator = Nothing
    Private _dumpFile As PacketDumpFile = Nothing
    Private _captureThread As Thread = Nothing
    Private _isCapturing As Boolean = False
    Private _packetCount As Long = 0
    Private _totalBytes As Long = 0
    Private _droppedPackets As Long = 0
    Private _eventMarkerCounter As Long = 0

    ' Configuration constants
    Private Const CaptureBufferSize As Integer = 65536  ' 64KB
    Private Const CaptureTimeoutMs As Integer = 1000    ' 1 second
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
            Return _packetCount
        End Get
    End Property

    Public ReadOnly Property TotalBytes As Long
        Get
            Return _totalBytes
        End Get
    End Property

    Public ReadOnly Property DroppedPackets As Long
        Get
            Return _droppedPackets
        End Get
    End Property

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(adapterGuid As String, oxtsIp As String, ncomPort As UShort, Optional deviceId As String = "OXTS_NCOM")
        Me.NetworkAdapterGuid = adapterGuid
        Me.OxtsIpAddress = oxtsIp
        Me.OxtsNcomPort = ncomPort
        Me.DeviceId = deviceId
    End Sub

    ''' <summary>
    ''' Starts capturing OXTS NCOM packets to PCAP file
    ''' </summary>
    Public Sub StartCapture(pcapFilename As String, sequence As Integer)
        Dim logPrefix As String = $"[{DeviceId}] StartCapture"

        Try
            If _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Already active")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Initializing NCOM capture to {Path.GetFileName(pcapFilename)}...")

            ' Find network adapter
            If Not FindNetworkAdapter() Then
                Throw New Exception($"Network adapter not found (GUID: {NetworkAdapterGuid})")
            End If

            ' Open device for packet capture (promiscuous mode)
            _communicator = _captureDevice.Open(
                CaptureBufferSize,
                PacketDeviceOpenAttributes.Promiscuous,
                CaptureTimeoutMs
            )

            ' ✅ Create event logger FIRST
            Try
                _eventLogger = New OxtsEventLogger(pcapFilename)
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Event logger created")
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Event logger creation failed: {ex.Message}")
            End Try

            ' Set BPF filter: UDP from OXTS IP on NCOM port
            Dim filter As String = $"udp and src host {OxtsIpAddress} and src port {OxtsNcomPort}"
            Using bpfFilter = _communicator.CreateFilter(filter)
                _communicator.SetFilter(bpfFilter)
            End Using

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Filter applied: {filter}")

            ' Open PCAP dump file
            _dumpFile = _communicator.OpenDump(pcapFilename)

            ' Reset statistics
            _packetCount = 0
            _totalBytes = 0
            _eventMarkerCounter = 0
            While _markerQueue.TryDequeue(Nothing) ' Clear queue
            End While

            ' Start background capture thread
            _isCapturing = True
            _captureThread = New Thread(AddressOf CapturePacketsLoop) With {
                .IsBackground = True,
                .Name = $"OxtsCapture_{DeviceId}"
            }
            _captureThread.Start()

            HandleUserMessageLogging("GMRC", $"{logPrefix}: NCOM capture started (seq {sequence:D2})")
            InjectEventMarker("START", $"OXTS NCOM recording started", sequence)

            ' Write event to INCA
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} OXTS NCOM capture started (seq {sequence:D2})", True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}", DisplayMsgBox)
            CleanupResources()
            _isCapturing = False
        End Try
    End Sub

    ''' <summary>
    ''' Stops capturing and closes PCAP file
    ''' </summary>
    Public Sub StopCapture()
        Dim logPrefix As String = $"[{DeviceId}] StopCapture"

        Try
            If Not _isCapturing Then
                HandleUserMessageLogging("GMRC", $"{logPrefix}: Not capturing")
                Return
            End If

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Stopping capture...")

            ' Signal capture thread to stop
            _isCapturing = False

            ' Wait for capture thread to finish (with timeout)
            If _captureThread IsNot Nothing AndAlso _captureThread.IsAlive Then
                If Not _captureThread.Join(TimeSpan.FromSeconds(2)) Then
                    HandleUserMessageLogging("GMRC", $"{logPrefix}: Capture thread did not stop gracefully")
                    _captureThread.Abort()
                End If
            End If

            ' Close resources
            CleanupResources()

            HandleUserMessageLogging("GMRC", $"{logPrefix}: Stopped. Captured {_packetCount:N0} packets ({_totalBytes:N0} bytes)")

            ' Write event to INCA
            MyIncaInterface?.WriteEventComment($"{DateTime.Now:HH:mm:ss} OXTS NCOM capture stopped ({_packetCount:N0} packets)", True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"{logPrefix}: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Injects an event marker into the PCAP stream
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
                _eventLogger.LogEvent(_packetCount, marker.Timestamp, eventType, message, sequenceNumber)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] InjectEventMarker: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Background packet capture loop
    ''' </summary>
    Private Sub CapturePacketsLoop()
        Try
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Capture thread started")

            Dim packetHandler As HandlePacket = Sub(packet As Packet)
                                                    Try
                                                        ' Write packet to PCAP file
                                                        _dumpFile.Dump(packet)

                                                        ' Update statistics
                                                        Interlocked.Increment(_packetCount)
                                                        Interlocked.Add(_totalBytes, packet.Length)

                                                        ' Process event markers (inject synthetic packets)
                                                        ProcessEventMarkers()

                                                    Catch ex As Exception
                                                        HandleUserMessageLogging("GMRC", $"[{DeviceId}] Packet handler error: {ex.Message}")
                                                    End Try
                                                End Sub

            ' Capture packets until stopped
            _communicator.ReceivePackets(0, packetHandler)

            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Capture thread exiting normally")

        Catch ex As ThreadAbortException
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Capture thread aborted")
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] Capture thread error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Processes queued event markers and injects them as synthetic UDP packets
    ''' </summary>
    Private Sub ProcessEventMarkers()
        Dim marker As EventMarker

        While _markerQueue.TryDequeue(marker)
            Try
                Interlocked.Increment(_eventMarkerCounter)

                ' Build marker payload: "OXTS_EVENT|TYPE|SEQ|MSG"
                Dim payload As String = $"OXTS_EVENT|{marker.EventType}|{marker.SequenceNumber:D2}|{marker.Message}"
                Dim payloadBytes As Byte() = System.Text.Encoding.ASCII.GetBytes(payload)

                ' Create synthetic UDP packet (marker uses special ports)
                Dim ethernetLayer As New EthernetLayer With {
                    .Source = New MacAddress("00:00:00:00:00:01"),
                    .Destination = New MacAddress("00:00:00:00:00:02"),
                    .EtherType = EthernetType.IpV4
                }

                Dim ipLayer As New IpV4Layer With {
                    .Source = New IpV4Address("127.0.0.1"),
                    .CurrentDestination = New IpV4Address("127.0.0.1"),
                    .Ttl = 128
                }

                Dim udpLayer As New UdpLayer With {
                    .SourcePort = MarkerSourcePort,
                    .DestinationPort = MarkerDestPort
                }

                Dim payloadLayer As New PayloadLayer With {
                    .Data = New Datagram(payloadBytes)
                }

                ' Build packet
                Dim builder As New PacketBuilder(ethernetLayer, ipLayer, udpLayer, payloadLayer)
                Dim markerPacket As Packet = builder.Build(marker.Timestamp)

                ' Write marker packet to PCAP
                _dumpFile.Dump(markerPacket)

                HandleUserMessageLogging("GMRC", $"[{DeviceId}] Marker #{_eventMarkerCounter}: {marker.EventType} - {marker.Message}")

            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] ProcessEventMarkers: {ex.Message}")
            End Try
        End While
    End Sub

    ''' <summary>
    ''' Finds the network adapter based on GUID
    ''' </summary>
    Private Function FindNetworkAdapter() As Boolean
        Try
            If String.IsNullOrEmpty(NetworkAdapterGuid) Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] Network adapter GUID not configured")
                Return False
            End If

            Dim devices = LivePacketDevice.AllLocalMachine
            If devices Is Nothing OrElse devices.Count = 0 Then
                HandleUserMessageLogging("GMRC", $"[{DeviceId}] No network adapters found")
                Return False
            End If

            For Each device In devices
                If InStr(device.Name, NetworkAdapterGuid, CompareMethod.Text) > 0 Then
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
    ''' Cleans up capture resources
    ''' </summary>
    Private Sub CleanupResources()
        Try
            If _dumpFile IsNot Nothing Then
                _dumpFile.Dispose()
                _dumpFile = Nothing
            End If

            If _communicator IsNot Nothing Then
                _communicator.Dispose()
                _communicator = Nothing
            End If

            If _eventLogger IsNot Nothing Then
                _eventLogger.Close()
                _eventLogger = Nothing
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[{DeviceId}] CleanupResources: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gets capture statistics as formatted string
    ''' </summary>
    Public Function GetStatistics() As String
        Return $"Packets: {_packetCount:N0} | Bytes: {_totalBytes:N0} | Dropped: {_droppedPackets:N0} | Markers: {_eventMarkerCounter:N0}"
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
