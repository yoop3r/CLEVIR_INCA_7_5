Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Net.NetworkInformation

''' <summary>
''' Listens to OXTS NCOM packets with integrity-first validation approach.
''' Prioritizes checksum verification over packet loss monitoring per NCOM specification.
''' </summary>
Public Class OxtsNcomInterface
    Private _udpClient As UdpClient
    Private _listenerThread As Thread
    Private _isRunning As Boolean = False

    ' NCOM configuration
    Public Property NcomIpAddress As String ' Set via configuration
    Public Property NcomPort As Integer ' Set via configuration

    ' Time synchronization data
    Public Property LastGpsTime As DateTime?
    Public Property LastSystemTime As DateTime?
    Public Property TimeOffset As TimeSpan = TimeSpan.Zero
    Public Property GpsWeek As Integer = 0
    Public Property GpsTimeOfWeek As Double = 0

    ' Position data
    Public Property Latitude As Double = 0
    Public Property Longitude As Double = 0
    Public Property Altitude As Double = 0
    Public Property Heading As Double = 0
    Public Property Roll As Double = 0
    Public Property Pitch As Double = 0

    ' Velocity data (m/s, NED frame)
    Public Property VelocityNorth As Double = 0
    Public Property VelocityEast As Double = 0
    Public Property VelocityDown As Double = 0

    ' Angular rate data (rad/s, body frame)
    Public Property YawRate As Double = 0
    Public Property PitchRate As Double = 0
    Public Property RollRate As Double = 0

    ' Status
    Public Property IsGpsLocked As Boolean = False
    Public Property LastPacketTime As DateTime?
    Public Property AllowNoGpsLock As Boolean = False
    Public Property DiagnosticLoggingEnabled As Boolean = False

    ' PTP Synchronization Status
    Public Property PtpStatus As OxtsStatusChannelDecoder.PtpStatusEnum = OxtsStatusChannelDecoder.PtpStatusEnum.Unknown
    Public Property PtpTimingSource As OxtsStatusChannelDecoder.TimingSourceEnum = OxtsStatusChannelDecoder.TimingSourceEnum.None
    Public Property LastPtpStatusTime As DateTime?
    Public Property SystemUptime As String = "Unknown"

    ' Status channel tracking
    Private _statusChannelHistory As New Dictionary(Of Byte, DateTime)

    ' Position tracking
    Private _latestPosition As OxtsPosition?
    Private _latestPositionTime As DateTime = DateTime.MinValue

    ' ✅ NEW: Integrity-focused statistics (replacing packet loss tracking)
    Private _stats As New PacketStatistics()

    ''' <summary>
    ''' ✅ NEW: Packet integrity statistics (replaces sequence number tracking)
    ''' </summary>
    Private Class PacketStatistics
        Public TotalReceived As Long = 0
        Public ValidPackets As Long = 0
        Public InvalidSyncByte As Long = 0
        Public InvalidNavStatus As Long = 0
        Public StructureBPackets As Long = 0
        Public Checksum1Failures As Long = 0
        Public Checksum2Failures As Long = 0
        Public Checksum3Failures As Long = 0
        Public CorruptedPackets As Long = 0
        Public LastResetTime As DateTime = DateTime.UtcNow

        Public ReadOnly Property IntegrityPercent As Double
            Get
                If TotalReceived = 0 Then Return 0
                Return (CDbl(ValidPackets) / CDbl(TotalReceived)) * 100.0
            End Get
        End Property

        Public ReadOnly Property CorruptionPercent As Double
            Get
                If TotalReceived = 0 Then Return 0
                Return (CDbl(CorruptedPackets) / CDbl(TotalReceived)) * 100.0
            End Get
        End Property

        Public Sub Reset()
            TotalReceived = 0
            ValidPackets = 0
            InvalidSyncByte = 0
            InvalidNavStatus = 0
            StructureBPackets = 0
            Checksum1Failures = 0
            Checksum2Failures = 0
            Checksum3Failures = 0
            CorruptedPackets = 0
            LastResetTime = DateTime.UtcNow
        End Sub

        Public Function GetSummary() As String
            Return $"Total: {TotalReceived:N0} | Valid: {ValidPackets:N0} ({IntegrityPercent:F2}%) | " &
                   $"Corrupted: {CorruptedPackets:N0} ({CorruptionPercent:F2}%) | " &
                   $"CS Failures: {Checksum1Failures + Checksum2Failures + Checksum3Failures}"
        End Function
    End Class

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
        Public YawRate As Double
        Public PitchRate As Double
        Public RollRate As Double
        Public GpsMode As Integer
        Public NavigationStatus As Byte
        Public RtkStatus As String
    End Structure

    ' ✅ SIMPLIFIED: Remove packet loss methods, add integrity reporting
    Public ReadOnly Property PacketIntegrityPercent As Double
        Get
            SyncLock _stats
                Return _stats.IntegrityPercent
            End SyncLock
        End Get
    End Property

    Public ReadOnly Property PacketCorruptionPercent As Double
        Get
            SyncLock _stats
                Return _stats.CorruptionPercent
            End SyncLock
        End Get
    End Property

    Public ReadOnly Property TotalPacketsReceived As Long
        Get
            SyncLock _stats
                Return _stats.TotalReceived
            End SyncLock
        End Get
    End Property

    Public ReadOnly Property ValidPacketsReceived As Long
        Get
            SyncLock _stats
                Return _stats.ValidPackets
            End SyncLock
        End Get
    End Property

    ''' <summary>
    ''' Starts listening for OXTS NCOM packets
    ''' </summary>
    Public Sub OxtsStartListening()
        SyncLock Me
            If _isRunning Then
                HandleUserMessageLogging("GMRC", $"⚠️ OXTS listener already running on instance {Me.GetHashCode()} - ignoring duplicate start")
                Return
            End If
            _isRunning = True
            HandleUserMessageLogging("GMRC", $"🔧 OXTS: Starting listener on instance {Me.GetHashCode()}")
        End SyncLock

        Try
            Dim localIp As IPAddress = GetOxtsSubnetInterface()
            Dim localEndPoint As New IPEndPoint(localIp, NcomPort)

            _udpClient = New UdpClient()
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, False)
            _udpClient.Client.Bind(localEndPoint)

            HandleUserMessageLogging("GMRC", $"✅ OXTS NCOM listener bound to {localEndPoint} (Instance={Me.GetHashCode()})")

            _listenerThread = New Thread(AddressOf ListenLoop) With {
                .IsBackground = True,
                .Name = $"OXTS_NCOM_{Me.GetHashCode()}"
            }
            _listenerThread.Start()

        Catch ex As SocketException
            _isRunning = False
            HandleUserMessageLogging("GMRC",
                                     $"❌ Socket error binding to port {NcomPort}: {ex.Message} (Error: {ex.ErrorCode}){Environment.NewLine}" &
                                     $"💡 Port may be in use by another process.",
                                     DisplayMsgBox)

        Catch ex As Exception
            _isRunning = False
            HandleUserMessageLogging("GMRC", $"Failed to start NCOM listener: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Finds the network interface on the 10.5.55.x subnet (OXTS subnet)
    ''' </summary>
    Private Function GetOxtsSubnetInterface() As IPAddress
        Try
            For Each nic In NetworkInterface.GetAllNetworkInterfaces()
                If nic.OperationalStatus <> OperationalStatus.Up Then Continue For

                For Each unicastAddr In nic.GetIPProperties().UnicastAddresses
                    Dim ip = unicastAddr.Address
                    If ip.AddressFamily = Sockets.AddressFamily.InterNetwork Then
                        Dim ipStr = ip.ToString()
                        If ipStr.StartsWith("10.5.55.") Then
                            HandleUserMessageLogging("GMRC", $"OXTS: Found interface on OXTS subnet: {ipStr} ({nic.Name})")
                            Return ip
                        End If
                    End If
                Next
            Next

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
        Static lastStatsLogTime As DateTime = DateTime.MinValue

        ' ✅ NEW: Connection health tracking
        Dim connectionEstablished As Boolean = False
        Dim lastPacketReceived As DateTime = DateTime.UtcNow
        Const InitialConnectionTimeout As Integer = 30000  ' 30 seconds to establish
        Const OngoingConnectionTimeout As Integer = 10000  ' 10 seconds during operation

        HandleUserMessageLogging("GMRC", $"OXTS: Waiting {InitialConnectionTimeout}ms for initial packet...")

        While _isRunning
            Try
                ' ✅ Check for graceful shutdown request
                If Not _isRunning Then Exit While

                If _udpClient.Available > 0 Then
                    Dim ncomData As Byte() = _udpClient.Receive(remoteEndPoint)
                    LastPacketTime = DateTime.UtcNow
                    lastPacketReceived = DateTime.UtcNow

                    ' Mark connection as established on first valid packet
                    If Not connectionEstablished Then
                        connectionEstablished = True
                        HandleUserMessageLogging("GMRC", $"✅ OXTS: Connection established from {remoteEndPoint}")
                    End If

                    ' ✅ CRITICAL: Validate packet BEFORE processing
                    Dim validationResult As NcomValidationResult = Nothing
                    Dim isValid As Boolean = OxtsNcomChecksum.ValidatePacket(ncomData, validationResult)

                    ' Update statistics
                    SyncLock _stats
                        _stats.TotalReceived += 1

                        If Not isValid Then
                            ' Categorize failure type
                            If Not validationResult.SyncByteValid Then
                                _stats.InvalidSyncByte += 1
                            ElseIf Not validationResult.NavigationStatusValid Then
                                If validationResult.NavigationStatus = 11 Then
                                    _stats.StructureBPackets += 1
                                Else
                                    _stats.InvalidNavStatus += 1
                                End If
                            ElseIf Not validationResult.Checksum1Valid Then
                                _stats.Checksum1Failures += 1
                                _stats.CorruptedPackets += 1
                            ElseIf Not validationResult.Checksum2Valid Then
                                _stats.Checksum2Failures += 1
                                _stats.CorruptedPackets += 1
                            ElseIf Not validationResult.Checksum3Valid Then
                                _stats.Checksum3Failures += 1
                                _stats.CorruptedPackets += 1
                            End If

                            ' Log corruption (critical issue)
                            If _stats.CorruptedPackets Mod 10 = 1 Then ' Log first and every 10th
                                HandleUserMessageLogging("GMRC", $"⚠️ OXTS: {validationResult.ErrorMessage}")
                            End If

                            Continue While ' Skip corrupted packet
                        End If

                        ' Packet is valid
                        _stats.ValidPackets += 1
                    End SyncLock

                    ' ✅ Parse validated packet
                    If ParseNcomPacket(ncomData) Then
                        UpdateTimeOffset()
                    End If

                    ' ✅ Log statistics every 10 seconds
                    'If (DateTime.UtcNow - lastStatsLogTime).TotalSeconds >= 10 Then
                    '    SyncLock _stats
                    '        HandleUserMessageLogging("GMRC", $"📊 OXTS Stats: {_stats.GetSummary()}")
                    '    End SyncLock
                    '    lastStatsLogTime = DateTime.UtcNow
                    'End If
                Else
                    ' ✅ NEW: Check for connection timeout
                    Dim timeSinceLastPacket As Double = (DateTime.UtcNow - lastPacketReceived).TotalMilliseconds

                    If Not connectionEstablished Then
                        If timeSinceLastPacket > InitialConnectionTimeout Then
                            HandleUserMessageLogging("GMRC",
                                                     $"⚠️ OXTS: No packets received after {InitialConnectionTimeout}ms - OXTS may not be connected. Stopping listener.",
                                                     DisplayMsgBox)
                            _isRunning = False
                            Exit While
                        End If
                    Else
                        ' Connection was established but went silent
                        If timeSinceLastPacket > OngoingConnectionTimeout Then
                            ' ✅ Only show message box if actively recording
                            If GmResidentClient._isMonitorTaskRunning Then
                                HandleUserMessageLogging("GMRC",
                                                         $"⚠️ OXTS: Connection lost (no packets for {OngoingConnectionTimeout}ms). Stopping listener.",
                                                         DisplayMsgBox)
                            Else
                                ' Still log but don't show message box when not recording
                                HandleUserMessageLogging("GMRC",
                                                         $"⚠️ OXTS: Connection lost (no packets for {OngoingConnectionTimeout}ms). Stopping listener.")
                            End If
                            _isRunning = False
                            Exit While
                        End If
                    End If
                End If

                ' ✅ Break sleep into 100ms chunks for faster shutdown response
                For i As Integer = 0 To 10 Step 1
                    If Not _isRunning Then Exit While
                    Thread.Sleep(10)
                Next

            Catch ex As SocketException
                If _isRunning Then
                    HandleUserMessageLogging("GMRC", $"❌ OXTS socket error: {ex.Message}")
                End If
                Exit While

            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"❌ OXTS listener error: {ex.Message}")
            End Try
        End While

        ' ✅ Cleanup notification
        HandleUserMessageLogging("GMRC", $"OXTS: Listener loop exited (Instance={Me.GetHashCode()})")
    End Sub

    ''' <summary>
    ''' ✅ SIMPLIFIED: Parses NCOM packet (assumes validation already done)
    ''' </summary>
    Private Function ParseNcomPacket(data As Byte()) As Boolean
        Try
            ' Extract and process Status Channel (Batch S)
            Dim statusInfo = OxtsStatusChannelDecoder.ExtractBatchS(data)
            ProcessStatusChannel(statusInfo.ChannelId, statusInfo.BatchS)

            ' Decode position data
            Dim position = OxtsNcomDecoder.DecodePacket(data)

            If position.HasValue Then
                ' Update properties
                Latitude = position.Value.Latitude
                Longitude = position.Value.Longitude
                Altitude = position.Value.Altitude
                Heading = position.Value.Heading
                Pitch = position.Value.Pitch
                Roll = position.Value.Roll
                VelocityNorth = position.Value.VelocityNorth
                VelocityEast = position.Value.VelocityEast
                VelocityDown = position.Value.VelocityDown
                YawRate = position.Value.YawRate
                PitchRate = position.Value.PitchRate
                RollRate = position.Value.RollRate

                ' Compute GPS time from Week + TOW
                If GpsWeek > 0 AndAlso GpsTimeOfWeek >= 0 Then
                    Dim gpsEpoch As New DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc)
                    LastGpsTime = gpsEpoch.AddDays(GpsWeek * 7).AddSeconds(GpsTimeOfWeek)
                Else
                    LastGpsTime = DateTime.UtcNow
                End If

                ' Extract navigation status
                Dim navStatus As Byte = data(21)
                IsGpsLocked = (navStatus = 4) ' Status 4 = "Locked"

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
    ''' ✅ NEW: Reports comprehensive packet integrity diagnostics
    ''' </summary>
    Public Sub ReportPacketIntegrity()
        SyncLock _stats
            HandleUserMessageLogging("GMRC", "=== OXTS Packet Integrity Report ===")
            HandleUserMessageLogging("GMRC", $"Runtime: {(DateTime.UtcNow - _stats.LastResetTime).TotalSeconds:F1}s")
            HandleUserMessageLogging("GMRC", "")
            HandleUserMessageLogging("GMRC", $"Total Received: {_stats.TotalReceived:N0}")
            HandleUserMessageLogging("GMRC", $"Valid Packets: {_stats.ValidPackets:N0} ({_stats.IntegrityPercent:F2}%)")
            HandleUserMessageLogging("GMRC", $"Corrupted: {_stats.CorruptedPackets:N0} ({_stats.CorruptionPercent:F2}%)")
            HandleUserMessageLogging("GMRC", "")
            HandleUserMessageLogging("GMRC", "=== Failure Breakdown ===")
            HandleUserMessageLogging("GMRC", $"Invalid Sync Byte: {_stats.InvalidSyncByte:N0}")
            HandleUserMessageLogging("GMRC", $"Invalid Nav Status: {_stats.InvalidNavStatus:N0}")
            HandleUserMessageLogging("GMRC", $"Structure-B (ignored): {_stats.StructureBPackets:N0}")
            HandleUserMessageLogging("GMRC", $"Checksum 1 Failures: {_stats.Checksum1Failures:N0} (Batch A)")
            HandleUserMessageLogging("GMRC", $"Checksum 2 Failures: {_stats.Checksum2Failures:N0} (Batch A+B)")
            HandleUserMessageLogging("GMRC", $"Checksum 3 Failures: {_stats.Checksum3Failures:N0} (Full packet)")
            HandleUserMessageLogging("GMRC", "")

            ' Recommendations
            If _stats.CorruptionPercent > 5 Then
                HandleUserMessageLogging("GMRC", "⚠️ WARNING: High corruption rate detected!")
                HandleUserMessageLogging("GMRC", "   Recommendations:")
                HandleUserMessageLogging("GMRC", "   - Check network cable integrity")
                HandleUserMessageLogging("GMRC", "   - Verify switch/router not overloaded")
                HandleUserMessageLogging("GMRC", "   - Ensure OXTS firmware is up to date")
            ElseIf _stats.IntegrityPercent < 95 Then
                HandleUserMessageLogging("GMRC", "⚠️ Moderate integrity issues detected")
            Else
                HandleUserMessageLogging("GMRC", "✅ Excellent data integrity")
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' ✅ NEW: Resets integrity statistics
    ''' </summary>
    Public Sub ResetIntegrityStats()
        SyncLock _stats
            _stats.Reset()
            HandleUserMessageLogging("GMRC", "OXTS integrity statistics reset")
        End SyncLock
    End Sub

    Private Sub ProcessStatusChannel(channelId As Byte, batchS As Byte())
        If batchS Is Nothing Then Return

        SyncLock _statusChannelHistory
            _statusChannelHistory(channelId) = DateTime.UtcNow
        End SyncLock

        Select Case channelId
            Case 23 ' PTP Status + System Uptime
                Dim status23 = OxtsStatusChannelDecoder.DecodeStatusChannel23(batchS)
                If status23.IsValid Then
                    PtpStatus = status23.PtpStatus
                    SystemUptime = OxtsStatusChannelDecoder.DecodeSystemUptime(status23.SystemUptime)
                    LastPtpStatusTime = DateTime.UtcNow

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
        SyncLock Me
            If Not _latestPosition.HasValue Then Return "None"

            Select Case _latestPosition.Value.GpsMode
                Case 4 : Return "Integer"
                Case 5 : Return "Float"
                Case Else : Return "None"
            End Select
        End SyncLock
    End Function

    Public Function IsRealtime() As Boolean
        SyncLock Me
            If Not _latestPosition.HasValue Then Return False
            Return (DateTime.Now - _latestPositionTime).TotalSeconds < 2
        End SyncLock
    End Function

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
        End SyncLock
    End Sub

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

    Public Function GetGpsPosition() As OxtsPosition?
        SyncLock Me
            If Not _latestPosition.HasValue Then Return Nothing
            If (DateTime.UtcNow - _latestPositionTime).TotalSeconds > 2 Then Return Nothing
            Return _latestPosition
        End SyncLock
    End Function

    Private Sub UpdateTimeOffset()
        If LastGpsTime.HasValue Then
            LastSystemTime = DateTime.UtcNow
            TimeOffset = LastGpsTime.Value.Subtract(LastSystemTime.Value)
        End If
    End Sub

    Public Function GetSynchronizedTimestamp() As DateTime
        If Not LastGpsTime.HasValue Then Return DateTime.UtcNow
        Return DateTime.UtcNow.Add(TimeOffset)
    End Function

    Public Function GetCurrentPosition() As OxtsPosition
        SyncLock Me
            If _latestPosition.HasValue Then
                Return _latestPosition.Value
            Else
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

    Public Sub TestOxtsIntegration()
        HandleUserMessageLogging("GMRC", "=== OXTS Integration Test ===")
        HandleUserMessageLogging("GMRC", $"Listening: {_isRunning}")

        SyncLock _stats
            HandleUserMessageLogging("GMRC", $"Packets: {_stats.GetSummary()}")
        End SyncLock

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

    Public Function IsPtpSynchronized() As Boolean
        Return PtpStatus = OxtsStatusChannelDecoder.PtpStatusEnum.Locked OrElse
               PtpStatus = OxtsStatusChannelDecoder.PtpStatusEnum.Master
    End Function

    Public Function GetPtpSyncQuality() As Integer
        Select Case PtpStatus
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Locked : Return 100
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Master : Return 95
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Slave : Return 75
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Listening : Return 50
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Initialising : Return 25
            Case OxtsStatusChannelDecoder.PtpStatusEnum.Disabled : Return 0
            Case Else : Return 0
        End Select
    End Function
End Class