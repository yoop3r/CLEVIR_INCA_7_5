Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

''' <summary>
''' Listens to OXTS NCOM packets for GPS time synchronization
''' </summary>
Public Class OxtsNcomInterface
    Private _udpClient As UdpClient
    Private _listenerThread As Thread
    Private _isRunning As Boolean = False

    ' NCOM configuration
    Public Property NcomIpAddress As String = "192.168.25.10" ' OXTS IP
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

    Public Sub StartListening()
        If _isRunning Then Return

        Try
            _udpClient = New UdpClient(NcomPort)
            _isRunning = True

            _listenerThread = New Thread(AddressOf ListenLoop) With {
                .IsBackground = True,
                .Name = "OXTS_NCOM_Listener"
            }
            _listenerThread.Start()

            HandleUserMessageLogging("GMRC", $"OXTS NCOM listener started on port {NcomPort}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Failed to start NCOM listener: {ex.Message}")
            _isRunning = False
        End Try
    End Sub

    Public Sub StopListening()
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

        While _isRunning
            Try
                ' Receive NCOM packet (non-blocking with timeout)
                If _udpClient.Available > 0 Then
                    Dim ncomData As Byte() = _udpClient.Receive(remoteEndPoint)
                    LastPacketTime = DateTime.UtcNow

                    ' Parse NCOM packet
                    If ParseNcomPacket(ncomData) Then
                        ' Update time offset for LiDAR synchronization
                        UpdateTimeOffset()
                    End If
                End If

                Thread.Sleep(10) ' Prevent CPU spinning

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
    ''' Parses NCOM Status packet (Batch S) - simplest format with time/position
    ''' NCOM packet structure: https://www.oxts.com/technical-notes/
    ''' </summary>
    Private Function ParseNcomPacket(data As Byte()) As Boolean
        Try
            ' NCOM packet minimum size check
            If data.Length < 72 Then Return False

            ' Check sync byte (0xE7 for Status packet)
            If data(0) <> &HE7 Then Return False

            ' Extract GPS time (bytes 4-5: GPS week, bytes 24-27: GPS time of week in ms)
            GpsWeek = BitConverter.ToUInt16(data, 4)
            Dim timeOfWeekMs As UInteger = BitConverter.ToUInt32(data, 24)
            GpsTimeOfWeek = timeOfWeekMs / 1000.0 ' Convert to seconds

            ' Convert GPS week/TOW to DateTime
            Dim gpsEpoch As New DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc)
            LastGpsTime = gpsEpoch.AddDays(GpsWeek * 7).AddSeconds(GpsTimeOfWeek)

            ' Extract position (bytes 28-39: lat/lon/alt as 32-bit floats)
            Latitude = BitConverter.ToSingle(data, 28) * (180.0 / Math.PI) ' Convert radians to degrees
            Longitude = BitConverter.ToSingle(data, 32) * (180.0 / Math.PI)
            Altitude = BitConverter.ToSingle(data, 36)

            ' Extract orientation (bytes 12-23: roll/pitch/heading as 32-bit floats)
            Roll = BitConverter.ToSingle(data, 12) * (180.0 / Math.PI)
            Pitch = BitConverter.ToSingle(data, 16) * (180.0 / Math.PI)
            Heading = BitConverter.ToSingle(data, 20) * (180.0 / Math.PI)

            ' Extract navigation status (byte 68)
            Dim navStatus As Byte = data(68)
            IsGpsLocked = (navStatus And &H3) >= 2 ' Bit 0-1: 0=Init, 1=Locking, 2=Locked, 3=Fixed

            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"NCOM parse error: {ex.Message}")
            Return False
        End Try
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
        Return New OxtsPosition With {
            .GpsTime = GetSynchronizedTimestamp(),
            .Latitude = Me.Latitude,
            .Longitude = Me.Longitude,
            .Altitude = Me.Altitude,
            .Heading = Me.Heading,
            .Roll = Me.Roll,
            .Pitch = Me.Pitch
        }
    End Function

    ''' <summary>
    ''' Waits for GPS lock with timeout
    ''' </summary>
    Public Function WaitForGpsLock(timeoutMs As Integer) As Boolean
        Dim startTime = DateTime.Now

        While DateTime.Now.Subtract(startTime).TotalMilliseconds < timeoutMs
            If IsGpsLocked Then Return True
            Thread.Sleep(100)
        End While

        Return False
    End Function
End Class

''' <summary>
''' Position data structure for event markers
''' </summary>
Public Structure OxtsPosition
    Public GpsTime As DateTime
    Public Latitude As Double
    Public Longitude As Double
    Public Altitude As Double
    Public Heading As Double
    Public Roll As Double
    Public Pitch As Double
End Structure
