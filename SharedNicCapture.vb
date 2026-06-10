Option Strict Off
Imports System.IO
Imports System.Threading
Imports System.Net
Imports SharpPcap
Imports SharpPcap.LibPcap
Imports PacketDotNet
Imports PcapEventBridge

''' <summary>
''' Owns a single NIC capture handle shared by two or more LidarDevice instances
''' that are configured on the same network adapter GUID.
'''
''' Problem this solves:
'''   When multiple LidarDevice.StartCapture() calls open independent
'''   LibPcapLiveDevice handles against the same Npcap device, the driver can
'''   destabilise and stop delivering packets to either handle simultaneously.
'''   This class ensures exactly one open handle per adapter GUID and fans each
'''   arriving UDP packet out to the correct LidarDevice by source IP address.
''' </summary>
Public Class SharedNicCapture
    Implements IDisposable

    ' ================================================================
    ' Private state
    ' ================================================================
    Private ReadOnly _adapterGuid As String
    Private ReadOnly _devices As List(Of LidarDevice)
    Private ReadOnly _ipToDevice As Dictionary(Of String, LidarDevice)

    Private _captureDevice As ICaptureDevice
    Private _eventBridge As PcapEventBridge.PcapEventBridge
    Private _isCapturing As Boolean = False
    Private _disposed As Boolean = False

    ' ================================================================
    ' Diagnostics — per-IP packet counters, unknown-IP rate limiter
    ' ================================================================
    Private ReadOnly _perIpCount As New Dictionary(Of String, Long)(StringComparer.OrdinalIgnoreCase)
    Private _unknownIpCount As Long = 0
    Private _unknownIpLastLogged As Long = 0    ' Environment.TickCount64 equivalent via DateTime ticks
    Private _nullIpCount As Long = 0
    Private _totalNicPackets As Long = 0

    ' ================================================================
    ' Public properties
    ' ================================================================
    Public ReadOnly Property AdapterGuid As String
        Get
            Return _adapterGuid
        End Get
    End Property

    Public ReadOnly Property DeviceCount As Integer
        Get
            Return _devices.Count
        End Get
    End Property

    ' ================================================================
    ' Constructor
    ' ================================================================
    Public Sub New(adapterGuid As String, devices As IEnumerable(Of LidarDevice))
        _adapterGuid = adapterGuid
        _devices = New List(Of LidarDevice)(devices)
        _ipToDevice = New Dictionary(Of String, LidarDevice)(StringComparer.OrdinalIgnoreCase)
        For Each d In _devices
            If Not String.IsNullOrWhiteSpace(d.LidarIpAddress) Then
                _ipToDevice(d.LidarIpAddress.Trim()) = d
            End If
        Next
        ' Log the routing table so we can confirm both IPs are registered
        HandleUserMessageLogging("GMRC",
            $"[SharedNIC:{_adapterGuid}] Routing table: " &
            String.Join(", ", _ipToDevice.Select(Function(kv) $"{kv.Key}→[{kv.Value.DeviceId}]")))
    End Sub

    ' ================================================================
    ' Start / Stop
    ' ================================================================

    ''' <summary>
    ''' Opens the shared NIC handle, starts per-device dump files and marker pumps,
    ''' then begins capture.  pcapFilenames(i) maps to _devices(i).
    ''' </summary>
    Public Sub StartCapture(pcapFilenames As List(Of String), sequence As Integer)
        If _isCapturing Then
            HandleUserMessageLogging("GMRC", $"[SharedNIC:{_adapterGuid}] Already capturing")
            Return
        End If

        Try
            ' ----------------------------------------------------------------
            ' 1. Find the shared NIC handle
            ' ----------------------------------------------------------------
            If Not FindNetworkAdapter() Then
                Throw New InvalidOperationException(
                    $"SharedNicCapture: NIC not found for GUID {_adapterGuid}")
            End If

            ' ----------------------------------------------------------------
            ' 2. Build combined BPF filter (src host A or src host B …)
            ' ----------------------------------------------------------------
            Dim ipList = _devices.Select(Function(d) d.LidarIpAddress.Trim()).
                                  Where(Function(ip) Not String.IsNullOrWhiteSpace(ip)).
                                  Distinct().ToList()

            Dim bpf As String = "udp and greater 100 and (" &
                                String.Join(" or ", ipList.Select(Function(ip) $"src host {ip}")) &
                                ")"

            ' ----------------------------------------------------------------
            ' 3. Open NIC handle
            ' ----------------------------------------------------------------
            Dim config As New DeviceConfiguration() With {
                .Mode = DeviceModes.Promiscuous,
                .ReadTimeout = 500,
                .KernelBufferSize = 16 * 1024 * 1024,  ' 16 MB
                .Snaplen = 65535,
                .Immediate = True
            }
            _captureDevice.Open(config)
            _captureDevice.Filter = bpf

            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] NIC opened — BPF: {bpf}")

            ' ----------------------------------------------------------------
            ' 4. Start per-device dump files and marker pumps
            ' ----------------------------------------------------------------
            For i As Integer = 0 To _devices.Count - 1
                Dim filename = If(i < pcapFilenames.Count, pcapFilenames(i), $"LiDAR_{i + 1}_seq{sequence:D2}.pcap")
                _devices(i).StartCaptureShared(filename, sequence)
            Next

            ' ----------------------------------------------------------------
            ' 5. Subscribe to packet events via C# bridge (avoids VB ref-struct issue)
            ' ----------------------------------------------------------------
            _eventBridge = New PcapEventBridge.PcapEventBridge()
            AddHandler _eventBridge.PacketArrived, AddressOf OnPacketArrived
            _eventBridge.Subscribe(_captureDevice)

            AddHandler _captureDevice.OnCaptureStopped, AddressOf OnCaptureStopped

            ' ----------------------------------------------------------------
            ' 6. Start capture
            ' ----------------------------------------------------------------
            _captureDevice.StartCapture()
            _isCapturing = True

            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] Capture started for {_devices.Count} device(s): " &
                String.Join(", ", _devices.Select(Function(d) d.DeviceId)))

        Catch ex As Exception
            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] StartCapture failed: {ex.Message}")
            Cleanup()
            Throw
        End Try
    End Sub

    ''' <summary>
    ''' Stops the shared NIC handle and closes all per-device dump files.
    ''' </summary>
    Public Sub StopCapture()
        If Not _isCapturing Then Return

        Try
            HandleUserMessageLogging("GMRC", $"[SharedNIC:{_adapterGuid}] Stopping...")

            ' Signal per-device pumps to stop first (they flush marker queues)
            For Each d In _devices
                Try
                    d.StopCaptureShared()
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"[SharedNIC:{_adapterGuid}] StopCaptureShared({d.DeviceId}) error: {ex.Message}")
                End Try
            Next

            _isCapturing = False

            ' Log per-IP dispatch summary before cleanup
            Dim perIpSummary As String
            SyncLock _perIpCount
                perIpSummary = String.Join(", ", _perIpCount.Select(Function(kv) $"{kv.Key}={kv.Value:N0}"))
            End SyncLock
            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] DIAG: NIC totals — " &
                $"total={_totalNicPackets:N0}, null={_nullIpCount:N0}, unknown={_unknownIpCount:N0}, " &
                $"perIp=[{perIpSummary}]")

            Cleanup()

            HandleUserMessageLogging("GMRC", $"[SharedNIC:{_adapterGuid}] Stopped")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"[SharedNIC:{_adapterGuid}] StopCapture error: {ex.Message}")
        End Try
    End Sub

    ' ================================================================
    ' Sequence rotation helper — reopen dump files for new sequence
    ' ================================================================

    ''' <summary>
    ''' ✅ ATOMIC: Closes current per-device dump files and opens new ones for a new recording
    ''' sequence without restarting the NIC handle.  pcapFilenames(i) maps to _devices(i).
    ''' 
    ''' CRITICAL: Pauses the event handler during rotation to prevent packets from being
    ''' dispatched to devices with closed dump files (which would cause silent packet loss).
    ''' </summary>
    Public Sub RotateSequence(pcapFilenames As List(Of String), sequence As Integer)
        If Not _isCapturing Then Return

        Try
            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] Rotating to sequence {sequence:D2}...")

            ' ================================================================
            ' STEP 1: Pause packet dispatch (stop the event handler temporarily)
            ' This prevents packets from being written to the OLD dump file during rotation
            ' ================================================================
            RemoveHandler _eventBridge.PacketArrived, AddressOf OnPacketArrived
            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] Packet handler paused during rotation")

            ' Allow any inflight packets to drain from the capture thread
            Thread.Sleep(100)

            ' ================================================================
            ' STEP 2: Stop each device (flushes markers to old file)
            ' ================================================================
            For i As Integer = 0 To _devices.Count - 1
                Dim d = _devices(i)
                Try
                    d.StopCaptureShared()
                    HandleUserMessageLogging("GMRC",
                        $"[SharedNIC:{_adapterGuid}] Stopped {d.DeviceId} for rotation")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"[SharedNIC:{_adapterGuid}] Rotate stop({d.DeviceId}) error: {ex.Message}")
                End Try
            Next

            ' ================================================================
            ' STEP 3: Open new files and restart devices
            ' ================================================================
            For i As Integer = 0 To _devices.Count - 1
                Dim d = _devices(i)
                Dim filename = If(i < pcapFilenames.Count, pcapFilenames(i), $"LiDAR_{i + 1}_seq{sequence:D2}.pcap")
                Try
                    d.StartCaptureShared(filename, sequence)
                    HandleUserMessageLogging("GMRC",
                        $"[SharedNIC:{_adapterGuid}] Started {d.DeviceId} on new sequence {sequence:D2}")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"[SharedNIC:{_adapterGuid}] Rotate start({d.DeviceId}) error: {ex.Message}")
                End Try
            Next

            ' ================================================================
            ' STEP 4: Resume packet dispatch
            ' ================================================================
            AddHandler _eventBridge.PacketArrived, AddressOf OnPacketArrived
            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] Packet handler resumed — rotation to sequence {sequence:D2} complete")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] RotateSequence FATAL error: {ex.Message}")
            ' Attempt to restore packet handler even on error
            Try
                If _eventBridge IsNot Nothing Then
                    AddHandler _eventBridge.PacketArrived, AddressOf OnPacketArrived
                End If
            Catch
            End Try
        End Try
    End Sub

    ' ================================================================
    ' Packet fan-out
    ' ================================================================

    Private Sub OnPacketArrived(sender As Object, e As PacketArrivedEventArgs)
        Try
            Dim raw As RawCapture = e.Packet
            Interlocked.Increment(_totalNicPackets)

            ' Extract source IP from the Ethernet/IP layers
            Dim srcIp As String = ExtractSourceIp(raw)
            If srcIp Is Nothing Then
                Interlocked.Increment(_nullIpCount)
                Return
            End If

            ' Route to the correct LidarDevice
            Dim target As LidarDevice = Nothing
            If _ipToDevice.TryGetValue(srcIp, target) Then
                SyncLock _perIpCount
                    Dim c As Long = 0
                    _perIpCount.TryGetValue(srcIp, c)
                    _perIpCount(srcIp) = c + 1L
                End SyncLock
                target.DispatchPacket(raw)
            Else
                ' Unknown source IP (OXTS, other NIC traffic)
                Dim total = Interlocked.Increment(_unknownIpCount)
                ' Rate-limit: log first occurrence, then every 10,000
                If total = 1 OrElse total Mod 10000 = 0 Then
                    HandleUserMessageLogging("GMRC",
                        $"[SharedNIC:{_adapterGuid}] DIAG: Unknown src IP '{srcIp}' " &
                        $"(total unknown={total:N0}, totalNic={_totalNicPackets:N0}, " &
                        $"nullIp={_nullIpCount:N0}, ts={DateTime.Now:HH:mm:ss.fff})")
                End If
            End If

        Catch ex As Exception
            ' Never crash the capture thread
        End Try
    End Sub

    ''' <summary>
    ''' Returns the IPv4 source address string from a raw Ethernet frame, or Nothing.
    ''' Handles both plain Ethernet II and 802.1Q VLAN-tagged frames (0x8100 / 0x88A8).
    ''' Plain Ethernet II:  IP header starts at byte 14  → src IP at bytes 26-29
    ''' 802.1Q VLAN-tagged: 4-byte tag inserted at byte 12 → IP header at byte 18 → src IP at bytes 30-33
    ''' </summary>
    Private Shared Function ExtractSourceIp(raw As RawCapture) As String
        Try
            Dim data = raw.Data
            If data Is Nothing OrElse data.Length < 34 Then Return Nothing

            ' Determine Ethernet payload offset, skipping 802.1Q / 802.1ad VLAN tags
            Dim offset As Integer = 12  ' points at EtherType field
            Do
                Dim etherType As UShort = (CUShort(data(offset)) << 8) Or CUShort(data(offset + 1))
                If etherType = &H8100US OrElse etherType = &H88A8US Then
                    ' Skip 4-byte VLAN tag (2-byte EtherType + 2-byte TCI)
                    offset += 4
                    If offset + 2 > data.Length Then Return Nothing
                Else
                    Exit Do
                End If
            Loop

            ' At this point data(offset) and data(offset+1) are the EtherType of the IP layer
            If data.Length < offset + 22 Then Return Nothing  ' need EtherType(2) + IP hdr src offset(20)
            If data(offset) <> &H8 OrElse data(offset + 1) <> &H0 Then Return Nothing  ' Not IPv4

            ' IP src is at IP-header-start+12 → offset+2 (skip EtherType) +12
            Dim ipBase = offset + 2  ' start of IP header
            Return $"{data(ipBase + 12)}.{data(ipBase + 13)}.{data(ipBase + 14)}.{data(ipBase + 15)}"
        Catch
            Return Nothing
        End Try
    End Function

    Private Sub OnCaptureStopped(sender As Object, e As CaptureStoppedEventStatus)
        HandleUserMessageLogging("GMRC",
            $"[SharedNIC:{_adapterGuid}] DIAG: ⚠️ SharpPcap OnCaptureStopped — " &
            $"status={e}, isCapturing={_isCapturing}, " &
            $"devices={String.Join(",", _devices.Select(Function(d) $"{d.DeviceId}:{d.PacketCount:N0}pkts"))}, " &
            $"ts={DateTime.Now:HH:mm:ss.fff}")
    End Sub

    ' ================================================================
    ' NIC discovery (mirrors LidarDevice.FindNetworkAdapter)
    ' ================================================================

    Private Function FindNetworkAdapter() As Boolean
        Try
            Dim allDevices = CaptureDeviceList.Instance
            If allDevices.Count = 0 Then
                HandleUserMessageLogging("GMRC", $"[SharedNIC:{_adapterGuid}] No Npcap devices found")
                Return False
            End If

            Dim guidToMatch = _adapterGuid.Replace("{", "").Replace("}", "").ToUpper()

            For Each dev As ICaptureDevice In allDevices
                If dev.Name.ToUpper().Contains(guidToMatch) Then
                    _captureDevice = dev
                    HandleUserMessageLogging("GMRC",
                        $"[SharedNIC:{_adapterGuid}] ✅ NIC matched: {dev.Description}")
                    Return True
                End If
            Next

            ' GUID not found — fall back to subnet of the first device's IP
            If _devices.Count > 0 AndAlso Not String.IsNullOrWhiteSpace(_devices(0).LidarIpAddress) Then
                Dim subnet = _devices(0).LidarIpAddress.Trim()
                subnet = subnet.Substring(0, subnet.LastIndexOf("."c))

                For Each dev As ICaptureDevice In allDevices
                    If TypeOf dev Is LibPcapLiveDevice Then
                        Dim ld = DirectCast(dev, LibPcapLiveDevice)
                        For Each addr In ld.Addresses
                            Dim s = addr.Addr?.ToString()
                            If s IsNot Nothing AndAlso s.StartsWith(subnet & ".") AndAlso Not s.Contains(":") Then
                                _captureDevice = dev
                                HandleUserMessageLogging("GMRC",
                                    $"[SharedNIC:{_adapterGuid}] ✅ NIC matched by subnet ({subnet}.x): {dev.Description}")
                                Return True
                            End If
                        Next
                    End If
                Next
            End If

            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] ❌ NIC not found (GUID={_adapterGuid})")
            Return False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC",
                $"[SharedNIC:{_adapterGuid}] FindNetworkAdapter error: {ex.Message}")
            Return False
        End Try
    End Function

    ' ================================================================
    ' Internal cleanup
    ' ================================================================

    Private Sub Cleanup()
        Try
            If _eventBridge IsNot Nothing Then
                ' Explicitly unsubscribe before setting to Nothing
                ' This prevents event handler leaks from repeated Subscribe/Unsubscribe cycles
                _eventBridge.Unsubscribe()
                RemoveHandler _eventBridge.PacketArrived, AddressOf OnPacketArrived
                _eventBridge = Nothing
            End If
        Catch
        End Try

        Try
            If _captureDevice IsNot Nothing Then
                RemoveHandler _captureDevice.OnCaptureStopped, AddressOf OnCaptureStopped
                _captureDevice.StopCapture()
                _captureDevice.Close()
                ' Note: Intentionally NOT setting _captureDevice = Nothing
                ' to allow potential device reuse (compatible with SharpPcap patterns)
            End If
        Catch
        End Try
    End Sub

    ' ================================================================
    ' IDisposable
    ' ================================================================

    Public Sub Dispose() Implements IDisposable.Dispose
        If Not _disposed Then
            If _isCapturing Then StopCapture()
            Cleanup()
            _disposed = True
        End If
    End Sub

End Class
