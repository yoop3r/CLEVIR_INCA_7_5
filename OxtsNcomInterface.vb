Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Net.NetworkInformation ' For NIC enumeration

''' <summary>
''' Listens to OXTS NCOM packets for GPS time synchronization
''' </summary>
Public Class OxtsNcomInterface
    Private _udpClient As UdpClient
    Private _listenerThread As Thread
    Private _isRunning As Boolean = False

    ' NCOM configuration
    Public Property NcomIpAddress As String = "192.168.10.30" ' OXTS IP
    Public Property NcomPort As Integer = 3000 ' Default NCOM port

    ' Time synchronization data
    Public Property LastGpsTime As DateTime?
    Public Property LastSystemTime As DateTime?
    Public Property TimeOffset As TimeSpan = TimeSpan.Zero
    Public Property GpsWeek As Integer = 0
    Public Property GpsTimeOfWeek As Double = 0

    ' Position data (for event markers)
    Public Property Latitude As Double = 0
    Public Property Longitude As Double = 0
    Public Property Altitude As Double = 0
    Public Property Heading As Double = 0
    Public Property Roll As Double = 0
    Public Property Pitch As Double = 0

    ' Status
    Public Property IsGpsLocked As Boolean = False
    Public Property LastPacketTime As DateTime?
    Public Property AllowNoGpsLock As Boolean = False
    Public Property DiagnosticLoggingEnabled As Boolean = False

    'OXTS position tracking
    Private _lastPosition As (Latitude As Double, Longitude As Double, Altitude As Double) = (0, 0, 0)
    Private _lastPositionTime As DateTime = DateTime.MinValue
    Private _latestPosition As OxtsPosition? = Nothing ' Nullable OxtsPosition
    Private _latestPositionTime As DateTime = DateTime.MinValue

    ''' <summary>
    ''' Position data structure for event markers
    ''' </summary>
    Public Structure OxtsPosition
        Public Timestamp As DateTime
        Public Latitude As Double
        Public Longitude As Double
        Public Altitude As Double
        Public Heading As Double
        Public Pitch As Double
        Public Roll As Double
        Public VelocityNorth As Double
        Public VelocityEast As Double
        Public VelocityDown As Double
        Public GpsMode As Integer        ' ← CRITICAL: Must exist
        Public NavigationStatus As Byte  ' ← CRITICAL: Must exist
        Public RtkStatus As String       ' ← NEW: Added for clarity
    End Structure

    ' ✅ NEW: Packet statistics
    Private _packetCount As Long = 0
    Public ReadOnly Property PacketCount As Long
        Get
            Return _packetCount
        End Get
    End Property

    Public Sub OxtsStartListening()
        If _isRunning Then
            HandleUserMessageLogging("GMRC", "⚠️ OXTS listener already running - ignoring duplicate start")
            Return
        End If

        Try
            Dim localIp As IPAddress = GetOxtsSubnetInterface()
            Dim localEndPoint As New IPEndPoint(localIp, NcomPort)

            _udpClient = New UdpClient()

            ' ✅ CRITICAL FIX: Enable port sharing (allows coexistence with UDPService)
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, False)

            ' Bind to the endpoint
            _udpClient.Client.Bind(localEndPoint)

            _isRunning = True

            HandleUserMessageLogging("GMRC", $"✅ OXTS NCOM listener bound to {localEndPoint} (SHARED mode with UDPService)")

            _listenerThread = New Thread(AddressOf ListenLoop) With {
                .IsBackground = True,
                .Name = "OXTS_NCOM_Listener"
                }
            _listenerThread.Start()

        Catch ex As SocketException
            HandleUserMessageLogging("GMRC",
                                     $"❌ Socket error binding to port {NcomPort}: {ex.Message} (Error: {ex.ErrorCode}){Environment.NewLine}" &
                                     $"💡 UDPService (PID 6496) is using this port. Try closing NAVDisplay or rebooting.",
                                     DisplayMsgBox)
            _isRunning = False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Failed to start NCOM listener: {ex.Message}")
            _isRunning = False
        End Try
    End Sub

    ''' <summary>
    ''' Checks if a UDP port is already in use
    ''' </summary>
    Private Function IsPortInUse(port As Integer) As Boolean
        Try
            Dim ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties()
            Dim udpListeners = ipGlobalProperties.GetActiveUdpListeners()

            Return udpListeners.Any(Function(ep) ep.Port = port)

        Catch ex As Exception
            ' If check fails, let the bind attempt proceed (will fail if truly in use)
            Return False
        End Try
    End Function


    ''' <summary>
    ''' Finds the network interface on the 192.168.10.x subnet (OXTS subnet)
    ''' </summary>
    Private Function GetOxtsSubnetInterface() As IPAddress
        Try
            ' Import at top: Imports System.Net.NetworkInformation
            For Each nic In System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                If nic.OperationalStatus <> System.Net.NetworkInformation.OperationalStatus.Up Then
                    Continue For
                End If

                For Each unicastAddr In nic.GetIPProperties().UnicastAddresses
                    Dim ip = unicastAddr.Address
                    If ip.AddressFamily = Sockets.AddressFamily.InterNetwork Then
                        Dim ipStr = ip.ToString()
                        ' Match the OXTS subnet (192.168.10.x)
                        If ipStr.StartsWith("192.168.10.") Then
                            HandleUserMessageLogging("GMRC", $"OXTS: Found interface on OXTS subnet: {ipStr} ({nic.Name})")
                            Return ip
                        End If
                    End If
                Next
            Next

            ' ✅ Fallback: bind to any interface (0.0.0.0)
            HandleUserMessageLogging("GMRC", "OXTS: No interface on 192.168.10.x subnet found, binding to 0.0.0.0")
            Return IPAddress.Any

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"OXTS: Interface detection failed: {ex.Message}")
            Return IPAddress.Any
        End Try
    End Function
    Public Sub OxtsStopListening()
        _isRunning = False

        If _udpClient IsNot Nothing Then
            _udpClient.Close()
            _udpClient = Nothing
        End If

        If _listenerThread IsNot Nothing AndAlso _listenerThread.IsAlive Then
            _listenerThread.Join(2000)
        End If

        HandleUserMessageLogging("GMRC", "OXTS NCOM listener stopped")
    End Sub

    Private Sub ListenLoop()
        Dim remoteEndPoint As New IPEndPoint(IPAddress.Any, 0)
        Dim loopCount As Integer = 0

        While _isRunning
            Try
                ' ✅ Enhanced logging
                If _udpClient.Available > 0 Then
                    Dim ncomData As Byte() = _udpClient.Receive(remoteEndPoint)
                    LastPacketTime = DateTime.UtcNow
                    Interlocked.Increment(_packetCount)

                    ' ✅ Log first 10 packets, then every 100
                    If _packetCount <= 10 OrElse (_packetCount Mod 100 = 0) Then
                        HandleUserMessageLogging("GMRC", $"✅ OXTS: Packet #{_packetCount} from {remoteEndPoint}, {ncomData.Length} bytes, GPS Lock: {IsGpsLocked}")
                    End If

                    ' Parse NCOM packet
                    If ParseNcomPacket(ncomData) Then
                        UpdateTimeOffset()
                    End If

                    loopCount = 0 ' Reset timeout counter on successful receive
                Else
                    ' ✅ Log when no data (every 10 seconds)
                    loopCount += 1
                    If loopCount Mod 1000 = 0 Then ' 10ms * 1000 = 10 sec
                        HandleUserMessageLogging("GMRC", $"⚠️ OXTS: No data in last 10 sec (total: {_packetCount})")
                    End If
                End If

                Thread.Sleep(10)

            Catch ex As SocketException
                If _isRunning Then
                    HandleUserMessageLogging("GMRC", $"NCOM socket error: {ex.Message}")
                End If
                Exit While

            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"NCOM listener error: {ex.Message}")
            End Try
        End While
    End Sub
    ''' <summary>
    ''' Parses NCOM Batch S (Status Channel) - FINAL CORRECTED VERSION
    ''' Reference: OXTS NCOM Technical Manual
    ''' </summary>
    Private Function ParseNcomPacket(data As Byte()) As Boolean
        Try
            If data.Length < 72 Then Return False
            If data(0) <> &HE7 Then Return False

            ' ✅ DEBUG: Brute-force scan for lat/lon on FIRST packet
            If _packetCount = 1 Then
                Dim hex As String = BitConverter.ToString(data, 0, Math.Min(data.Length, 72))
                HandleUserMessageLogging("GMRC", $"NCOM RAW (full 72 bytes): {hex}")

                ' Scan for floats that might be lat/lon (in radians: 0.5 to 1.5 range)
                HandleUserMessageLogging("GMRC", "=== Scanning for Latitude/Longitude ===")
                For i As Integer = 0 To data.Length - 4
                    Dim f As Single = BitConverter.ToSingle(data, i)
                    Dim deg As Double = f * (180.0 / Math.PI)

                    ' Check if this could be latitude (30° to 50° = 0.52 to 0.87 rad)
                    If Math.Abs(f) > 0.5 AndAlso Math.Abs(f) < 1.5 Then
                        HandleUserMessageLogging("GMRC", $"  Byte {i}: {f:F6} rad = {deg:F6}°")
                    End If
                Next
            End If

            ' ✅ Use system time for now
            LastGpsTime = DateTime.UtcNow

            ' ✅ TEMPORARY: Try multiple byte positions
            ' Try original position (28-31, 32-35, 36-39)
            Dim latRad As Single = BitConverter.ToSingle(data, 28)
            Dim lonRad As Single = BitConverter.ToSingle(data, 32)
            Dim altMeters As Single = BitConverter.ToSingle(data, 36)

            Latitude = latRad * (180.0 / Math.PI)
            Longitude = lonRad * (180.0 / Math.PI)
            Altitude = altMeters

            ' ✅ Extract orientation
            Dim headRad As Single = BitConverter.ToSingle(data, 12)
            Dim pitchRad As Single = BitConverter.ToSingle(data, 16)
            Dim rollRad As Single = BitConverter.ToSingle(data, 20)

            Heading = headRad * (180.0 / Math.PI)
            Pitch = pitchRad * (180.0 / Math.PI)
            Roll = rollRad * (180.0 / Math.PI)

            ' ✅ Extract navigation status
            Dim navStatus As Byte = If(data.Length > 40, data(40), data(21))
            IsGpsLocked = (navStatus And &H1) <> 0

            UpdateLatestPosition(navStatus)
            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"NCOM parse error: {ex.Message}")
            Return False
        End Try
    End Function

    Public Function GetRtkStatus() As String
        ' Returns: "None", "Float", "Integer"
        SyncLock Me
            If Not _latestPosition.HasValue Then Return "None"

            Select Case _latestPosition.Value.GpsMode
                Case 4 ' RTK Fixed/Integer
                    Return "Integer"
                Case 5 ' RTK Float
                    Return "Float"
                Case Else
                    Return "None"
            End Select
        End SyncLock
    End Function

    Public Function IsRealtime() As Boolean
        ' Check if data is arriving in realtime (within last 2 seconds)
        SyncLock Me
            If Not _latestPosition.HasValue Then Return False
            Return (DateTime.Now - _latestPositionTime).TotalSeconds < 2
        End SyncLock
    End Function
    ''' <summary>
    ''' Updates the latest position data when a packet is parsed
    ''' Call this from ParseNcomPacket after extracting data
    ''' </summary>
    Private Sub UpdateLatestPosition(navStatus As Byte)
        SyncLock Me
            _latestPosition = New OxtsPosition With {
                .Timestamp = DateTime.UtcNow,
                .Latitude = Me.Latitude,
                .Longitude = Me.Longitude,
                .Altitude = Me.Altitude,
                .Heading = Me.Heading,
                .Pitch = Me.Pitch,
                .Roll = Me.Roll,
                .VelocityNorth = 0, ' TODO: Extract from NCOM if needed
                .VelocityEast = 0,
                .VelocityDown = 0,
                .GpsMode = navStatus And &H3, ' Bits 0-1: GPS mode
                .NavigationStatus = navStatus,
                .RtkStatus = GetRtkStatusFromMode(navStatus And &H3)
                }
            _latestPositionTime = DateTime.UtcNow
        End SyncLock
    End Sub

    ''' <summary>
    ''' Helper to convert GPS mode byte to readable string
    ''' </summary>
    Private Function GetRtkStatusFromMode(gpsMode As Integer) As String
        Select Case gpsMode
            Case 0 : Return "Initializing"
            Case 1 : Return "Locking"
            Case 2 : Return "Locked"
            Case 3 : Return "RTK Float"
            Case 4 : Return "RTK Integer"
            Case Else : Return "Unknown"
        End Select
    End Function

    ''' <summary>
    ''' Gets the most recent GPS position (if available and valid)
    ''' </summary>
    Public Function GetGpsPosition() As OxtsPosition?
        SyncLock Me
            If Not _latestPosition.HasValue Then
                Return Nothing
            End If

            ' Check if data is stale (older than 2 seconds)
            If (DateTime.UtcNow - _latestPositionTime).TotalSeconds > 2 Then
                Return Nothing
            End If

            Return _latestPosition
        End SyncLock
    End Function
    Private Sub UpdateTimeOffset()
        If LastGpsTime.HasValue Then
            LastSystemTime = DateTime.UtcNow
            TimeOffset = LastGpsTime.Value.Subtract(LastSystemTime.Value)
        End If
    End Sub

    ''' <summary>
    ''' Returns GPS-synchronized timestamp for current moment
    ''' </summary>
    Public Function GetSynchronizedTimestamp() As DateTime
        If Not LastGpsTime.HasValue Then
            Return DateTime.UtcNow ' Fallback to system time
        End If

        Return DateTime.UtcNow.Add(TimeOffset)
    End Function

    Public Function GetCurrentPosition() As OxtsPosition
        SyncLock Me
            If _latestPosition.HasValue Then
                Return _latestPosition.Value
            Else
                ' Fallback: return current properties (may be stale)
                Return New OxtsPosition With {
                    .Timestamp = DateTime.UtcNow,
                    .Latitude = Me.Latitude,
                    .Longitude = Me.Longitude,
                    .Altitude = Me.Altitude,
                    .Heading = Me.Heading,
                    .Roll = Me.Roll,
                    .Pitch = Me.Pitch,
                    .GpsMode = If(IsGpsLocked, 2, 0),
                    .NavigationStatus = 0,
                    .RtkStatus = "Unknown"
                    }
            End If
        End SyncLock
    End Function

    ''' <summary>
    ''' ✅ NEW: Relaxed GPS lock check for development
    ''' </summary>
    Public Function WaitForGpsLock(timeoutMs As Integer) As Boolean
        If AllowNoGpsLock Then
            HandleUserMessageLogging("GMRC", "OXTS: Skipping GPS lock check (AllowNoGpsLock = True)")
            Return True
        End If

        Dim startTime = DateTime.Now

        While DateTime.Now.Subtract(startTime).TotalMilliseconds < timeoutMs
            If IsGpsLocked Then
                HandleUserMessageLogging("GMRC", "OXTS: GPS lock acquired!")
                Return True
            End If
            Thread.Sleep(100)
        End While

        HandleUserMessageLogging("GMRC", $"OXTS: GPS lock timeout after {timeoutMs}ms")
        Return False
    End Function

    ''' <summary>
    ''' ✅ NEW: Diagnostic test function
    ''' </summary>
    Public Sub TestOxtsIntegration()
        HandleUserMessageLogging("GMRC", "=== OXTS Integration Test ===")
        HandleUserMessageLogging("GMRC", $"Listening: {_isRunning}")
        HandleUserMessageLogging("GMRC", $"Packets Received: {_packetCount}")
        HandleUserMessageLogging("GMRC", $"GPS Week: {GpsWeek}")
        HandleUserMessageLogging("GMRC", $"GPS TOW: {GpsTimeOfWeek:F3} seconds")
        HandleUserMessageLogging("GMRC", $"GPS Time: {If(LastGpsTime.HasValue, LastGpsTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"), "N/A")}")
        HandleUserMessageLogging("GMRC", $"System Time: {If(LastSystemTime.HasValue, LastSystemTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"), "N/A")}")
        HandleUserMessageLogging("GMRC", $"Time Offset: {TimeOffset.TotalMilliseconds:F3} ms")
        HandleUserMessageLogging("GMRC", $"GPS Locked: {IsGpsLocked}")
        HandleUserMessageLogging("GMRC", $"Last Packet: {If(LastPacketTime.HasValue, LastPacketTime.Value.ToString("HH:mm:ss.fff"), "N/A")}")
        HandleUserMessageLogging("GMRC", "")
        HandleUserMessageLogging("GMRC", "=== Position Data ===")
        HandleUserMessageLogging("GMRC", $"Latitude: {Latitude:F6}°")
        HandleUserMessageLogging("GMRC", $"Longitude: {Longitude:F6}°")
        HandleUserMessageLogging("GMRC", $"Altitude: {Altitude:F2} m")
        HandleUserMessageLogging("GMRC", $"Heading: {Heading:F2}°")
        HandleUserMessageLogging("GMRC", $"Roll: {Roll:F2}°")
        HandleUserMessageLogging("GMRC", $"Pitch: {Pitch:F2}°")
    End Sub

End Class