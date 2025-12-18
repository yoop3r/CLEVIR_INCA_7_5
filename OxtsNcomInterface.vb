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
    Public Property NcomIpAddress As String = "10.5.55.200" ' OXTS IP
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

    ' ✅ NEW: Velocity data (m/s, NED frame)
    Public Property VelocityNorth As Double = 0
    Public Property VelocityEast As Double = 0
    Public Property VelocityDown As Double = 0

    ' ✅ NEW: Angular rate data (rad/s, body frame)
    Public Property YawRate As Double = 0
    Public Property PitchRate As Double = 0
    Public Property RollRate As Double = 0

    ' Status
    Public Property IsGpsLocked As Boolean = False
    Public Property LastPacketTime As DateTime?
    Public Property AllowNoGpsLock As Boolean = False
    Public Property DiagnosticLoggingEnabled As Boolean = False

    ' ✅ NEW: PTP Synchronization Status
    Public Property PtpStatus As OxtsStatusChannelDecoder.PtpStatusEnum = OxtsStatusChannelDecoder.PtpStatusEnum.Unknown
    Public Property PtpTimingSource As OxtsStatusChannelDecoder.TimingSourceEnum = OxtsStatusChannelDecoder.TimingSourceEnum.None
    Public Property LastPtpStatusTime As DateTime?
    Public Property SystemUptime As String = "Unknown"

    ' ✅ NEW: Status channel tracking (cycles through ~200 packets)
    Private _statusChannelHistory As New Dictionary(Of Byte, DateTime)

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

        ' ✅ NEW: Velocities (m/s, NED frame)
        Public VelocityNorth As Double
        Public VelocityEast As Double
        Public VelocityDown As Double

        ' ✅ NEW: Angular rates (rad/s, body frame)
        Public YawRate As Double      ' Wz (rotation about vertical/down axis)
        Public PitchRate As Double    ' Wy (rotation about lateral axis)
        Public RollRate As Double     ' Wx (rotation about longitudinal axis)

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
    ''' Finds the network interface on the 10.5.55.x subnet (OXTS subnet)
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
                        ' Match the OXTS subnet (10.5.55.x)
                        If ipStr.StartsWith("10.5.55.") Then
                            HandleUserMessageLogging("GMRC", $"OXTS: Found interface on OXTS subnet: {ipStr} ({nic.Name})")
                            Return ip
                        End If
                    End If
                Next
            Next

            ' ✅ Fallback: bind to any interface (0.0.0.0)
            HandleUserMessageLogging("GMRC", "OXTS: No interface on 10.5.55.x subnet found, binding to 0.0.0.0")
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
                    'If _packetCount <= 10 OrElse (_packetCount Mod 100 = 0) Then
                    '    HandleUserMessageLogging("GMRC", $"✅ OXTS: Packet #{_packetCount} from {remoteEndPoint}, {ncomData.Length} bytes, GPS Lock: {IsGpsLocked}")
                    'End If

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

            ' ✅ NEW: Extract and process Status Channel (Batch S)
            Dim statusInfo = OxtsStatusChannelDecoder.ExtractBatchS(data)
            ProcessStatusChannel(statusInfo.ChannelId, statusInfo.BatchS)

            ' ✅ DEBUG: Dump packet for analysis (comment out after verification)
            Static packetCount As Integer = 0
            packetCount += 1
            If packetCount <= 3 Then ' Only dump first 3 packets
                'Console.WriteLine($"=== NCOM Packet #{packetCount} ===")
                'NcomDiagnostics.DumpNcomPacket(data)
                'NcomDiagnostics.VerifyNcomDecoding(data)
            End If

            ' ✅ Use OXTS NCOM Decoder to decode packet
            Dim position = OxtsNcomDecoder.DecodePacket(data)

            If position.HasValue Then
                ' Position and orientation
                Latitude = position.Value.Latitude
                Longitude = position.Value.Longitude
                Altitude = position.Value.Altitude
                Heading = position.Value.Heading
                Pitch = position.Value.Pitch
                Roll = position.Value.Roll

                ' ✅ NEW: Store velocities
                VelocityNorth = position.Value.VelocityNorth
                VelocityEast = position.Value.VelocityEast
                VelocityDown = position.Value.VelocityDown

                ' ✅ NEW: Store angular rates
                YawRate = position.Value.YawRate
                PitchRate = position.Value.PitchRate
                RollRate = position.Value.RollRate

                ' ✅ FIXED: Use actual GPS time from NCOM packet instead of system time
                ' GPS time is provided in the position structure (if decoder extracts it)
                ' For now, compute from GPS Week + Time of Week
                If GpsWeek > 0 AndAlso GpsTimeOfWeek >= 0 Then
                    ' GPS epoch: January 6, 1980 00:00:00 UTC
                    Dim gpsEpoch As New DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc)
                    LastGpsTime = gpsEpoch.AddDays(GpsWeek * 7).AddSeconds(GpsTimeOfWeek)
                Else
                    ' Fallback: use system time if GPS time not available yet
                    LastGpsTime = DateTime.UtcNow
                End If

                ' Extract navigation status
                Dim navStatus As Byte = If(data.Length > 40, data(40), data(21))
                IsGpsLocked = (navStatus And &H1) <> 0

                UpdateLatestPosition(navStatus)
                Return True
            End If

            Return False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"NCOM parse error: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' ✅ NEW: Processes NCOM Status Channels to extract PTP sync status
    ''' Status channels cycle through ~200 packets, so we track when each appears
    ''' </summary>
    Private Sub ProcessStatusChannel(channelId As Byte, batchS As Byte())
        If batchS Is Nothing Then Return

        ' Track when we last saw this channel
        SyncLock _statusChannelHistory
            _statusChannelHistory(channelId) = DateTime.UtcNow
        End SyncLock

        ' Process specific status channels
        Select Case channelId
            Case 23 ' PTP Status + System Uptime
                Dim status23 = OxtsStatusChannelDecoder.DecodeStatusChannel23(batchS)
                If status23.IsValid Then
                    PtpStatus = status23.PtpStatus
                    SystemUptime = OxtsStatusChannelDecoder.DecodeSystemUptime(status23.SystemUptime)
                    LastPtpStatusTime = DateTime.UtcNow

                    ' Log PTP status changes
                    Static lastPtpStatus As OxtsStatusChannelDecoder.PtpStatusEnum = OxtsStatusChannelDecoder.PtpStatusEnum.Unknown
                    If status23.PtpStatus <> lastPtpStatus Then
                        Dim statusDesc = OxtsStatusChannelDecoder.GetPtpStatusDescription(status23.PtpStatus)
                        HandleUserMessageLogging("GMRC", $"🔄 OXTS PTP Status: {statusDesc}")
                        lastPtpStatus = status23.PtpStatus
                    End If
                End If

            Case 93 ' Timing Source + PTP Configuration
                Dim status93 = OxtsStatusChannelDecoder.DecodeStatusChannel93(batchS)
                If status93.IsValid Then
                    PtpTimingSource = status93.TimingSource

                    ' Log timing source changes
                    Static lastTimingSource As OxtsStatusChannelDecoder.TimingSourceEnum = OxtsStatusChannelDecoder.TimingSourceEnum.None
                    If status93.TimingSource <> lastTimingSource Then
                        Dim sourceDesc = OxtsStatusChannelDecoder.GetTimingSourceDescription(status93.TimingSource)
                        HandleUserMessageLogging("GMRC", $"⏱️ OXTS Timing Source: {sourceDesc}")
                        lastTimingSource = status93.TimingSource
                    End If
                End If
        End Select
    End Sub

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
                .VelocityNorth = Me.VelocityNorth,
                .VelocityEast = Me.VelocityEast,
                .VelocityDown = Me.VelocityDown,
                .YawRate = Me.YawRate,
                .PitchRate = Me.PitchRate,
                .RollRate = Me.RollRate,
                .GpsMode = navStatus And &H3,
                .NavigationStatus = navStatus,
                .RtkStatus = GetRtkStatusFromMode(navStatus And &H3)
                }
            _latestPositionTime = DateTime.UtcNow

            'Dim pos = GetGpsPosition()
            'If pos.HasValue Then
            '    ' Position
            '    Console.WriteLine($"Lat: {pos.Value.Latitude:F8}°")
            '    Console.WriteLine($"Lon: {pos.Value.Longitude:F8}°")
            '    Console.WriteLine($"Alt: {pos.Value.Altitude:F2} m")

            '    ' Orientation
            '    Console.WriteLine($"Heading: {pos.Value.Heading:F2}°")
            '    Console.WriteLine($"Pitch: {pos.Value.Pitch:F2}°")
            '    Console.WriteLine($"Roll: {pos.Value.Roll:F2}°")

            '    ' ✅ NEW: Velocities
            '    Console.WriteLine($"Velocity North: {pos.Value.VelocityNorth:F3} m/s")
            '    Console.WriteLine($"Velocity East: {pos.Value.VelocityEast:F3} m/s")
            '    Console.WriteLine($"Velocity Down: {pos.Value.VelocityDown:F3} m/s")

            '    ' ✅ NEW: Angular rates
            '    Console.WriteLine($"Yaw Rate: {pos.Value.YawRate:F4} rad/s")
            '    Console.WriteLine($"Pitch Rate: {pos.Value.PitchRate:F4} rad/s")
            '    Console.WriteLine($"Roll Rate: {pos.Value.RollRate:F4} rad/s")
            'End If

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
                    .VelocityNorth = Me.VelocityNorth,
                    .VelocityEast = Me.VelocityEast,
                    .VelocityDown = Me.VelocityDown,
                    .YawRate = Me.YawRate,
                    .PitchRate = Me.PitchRate,
                    .RollRate = Me.RollRate,
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
        HandleUserMessageLogging("GMRC", "")
        HandleUserMessageLogging("GMRC", "=== PTP Synchronization Status ===")
        HandleUserMessageLogging("GMRC", $"PTP Status: {OxtsStatusChannelDecoder.GetPtpStatusDescription(PtpStatus)}")
        HandleUserMessageLogging("GMRC", $"Timing Source: {OxtsStatusChannelDecoder.GetTimingSourceDescription(PtpTimingSource)}")
        HandleUserMessageLogging("GMRC", $"System Uptime: {SystemUptime}")
        HandleUserMessageLogging("GMRC", $"Last PTP Update: {If(LastPtpStatusTime.HasValue, LastPtpStatusTime.Value.ToString("HH:mm:ss.fff"), "N/A")}")
    End Sub

    ''' <summary>
    ''' ✅ NEW: Checks if PTP is properly synchronized
    ''' </summary>
    Public Function IsPtpSynchronized() As Boolean
        Return PtpStatus = OxtsStatusChannelDecoder.PtpStatusEnum.Locked OrElse
               PtpStatus = OxtsStatusChannelDecoder.PtpStatusEnum.Master
    End Function

    ''' <summary>
    ''' ✅ NEW: Gets PTP synchronization quality (0-100%)
    ''' </summary>
    Public Function GetPtpSyncQuality() As Integer
        Select Case PtpStatus
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Locked
                Return 100 ' Perfect sync
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Master
                Return 95  ' Acting as grandmaster (very good)
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Slave
                Return 75  ' Syncing but not locked yet
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Listening
                Return 50  ' Searching for master
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Initialising
                Return 25  ' Starting up
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Disabled
                Return 0   ' Not enabled
            Case Else
                Return 0   ' Error or unknown
        End Select
    End Function
End Class