Option Strict On

Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Public Class TimeMachineTimeSyncProvider
    Implements ITimeSyncProvider

    Private Shared ReadOnly LocatorQuery As Byte() = {&HA1, &H4, &HB2}

    Public Property DeviceIpAddress As String = "255.255.255.255"
    Public Property Port As Integer = 7372
    Public Property PollIntervalMs As Integer = 1000
    Public Property ReceiveTimeoutMs As Integer = 500
    Public Property PtpAssumeLocked As Boolean = True

    Private _workerThread As Thread
    Private _running As Boolean

    Private _lastUpdateUtc As DateTime?
    Private _lastResponseUtc As DateTime?
    Private _lastNtpSyncCount As Long = -1
    Private _currentNtpSyncCount As Long = -1
    Private _timeServerName As String = ""
    Private _deviceName As String = ""
    Private _deviceUtcTimeText As String = ""
    Private _deviceLocationText As String = ""
    Private _syncState As String = "No Data"

    Private ReadOnly _stateLock As New Object()

    Public ReadOnly Property ProviderName As String Implements ITimeSyncProvider.ProviderName
        Get
            Return "TimeMachine"
        End Get
    End Property

    Public ReadOnly Property LastUpdateUtc As DateTime? Implements ITimeSyncProvider.LastUpdateUtc
        Get
            SyncLock _stateLock
                Return _lastUpdateUtc
            End SyncLock
        End Get
    End Property

    Public Sub Start() Implements ITimeSyncProvider.Start
        SyncLock Me
            If _running Then Return
            _running = True
        End SyncLock

        _workerThread = New Thread(AddressOf PollLoop) With {
            .IsBackground = True,
            .Name = "TimeMachine_TimeSync"
        }
        _workerThread.Start()

        HandleUserMessageLogging("GMRC", $"TimeMachine provider started ({DeviceIpAddress}:{Port})")
    End Sub

    Public Sub [Stop]() Implements ITimeSyncProvider.Stop
        SyncLock Me
            _running = False
        End SyncLock

        If _workerThread IsNot Nothing AndAlso _workerThread.IsAlive Then
            _workerThread.Join(1500)
        End If

        HandleUserMessageLogging("GMRC", "TimeMachine provider stopped")
    End Sub

    Public Function GetSynchronizedTimestamp() As DateTime Implements ITimeSyncProvider.GetSynchronizedTimestamp
        Return DateTime.UtcNow
    End Function

    Public Function IsSynchronized() As Boolean Implements ITimeSyncProvider.IsSynchronized
        SyncLock _stateLock
            If Not _lastResponseUtc.HasValue Then Return False
            Return (DateTime.UtcNow - _lastResponseUtc.Value).TotalSeconds <= 5
        End SyncLock
    End Function

    Public Function GetPtpStatusText() As String Implements ITimeSyncProvider.GetPtpStatusText
        If Not IsSynchronized() Then
            Return "PTP: No TimeMachine response"
        End If

        If PtpAssumeLocked Then
            Return "PTP: LOCKED (TimeMachine source)"
        End If

        Return "PTP: Unknown (not exposed by Locator API)"
    End Function

    Public Function IsPtpSynchronized() As Boolean Implements ITimeSyncProvider.IsPtpSynchronized
        Return IsSynchronized() AndAlso PtpAssumeLocked
    End Function

    Public Function GetNtpStatusText() As String Implements ITimeSyncProvider.GetNtpStatusText
        SyncLock _stateLock
            Dim ageText As String = "n/a"
            If _lastResponseUtc.HasValue Then
                ageText = $"{(DateTime.UtcNow - _lastResponseUtc.Value).TotalSeconds:F1}s"
            End If

            Dim sourceText As String = If(String.IsNullOrWhiteSpace(_timeServerName), "unknown", _timeServerName)

            ' TM1000/TM2000 can report SyncCount as 0xFFFFFFFF (4294967295), which is not useful.
            ' In that case prefer reporting device UTC time and location fields from bytes 18..45.
            If _currentNtpSyncCount = CLng(UInteger.MaxValue) Then
                Dim utcText As String = If(String.IsNullOrWhiteSpace(_deviceUtcTimeText), "n/a", _deviceUtcTimeText)
                Dim locText As String = If(String.IsNullOrWhiteSpace(_deviceLocationText), "unknown", _deviceLocationText)
                Return $"NTP: {_syncState} | UTC={utcText} | Loc={locText} | Server={sourceText} | Age={ageText}"
            End If

            Return $"NTP: {_syncState} | SyncCount={_currentNtpSyncCount} | Server={sourceText} | Age={ageText}"
        End SyncLock
    End Function

    Private Sub PollLoop()
        While _running
            Try
                QueryAndUpdate()
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"TimeMachine poll error: {ex.Message}")
            End Try

            Dim sleepMs As Integer = Math.Max(200, PollIntervalMs)
            For i As Integer = 1 To Math.Max(1, sleepMs \ 50)
                If Not _running Then Exit While
                Thread.Sleep(50)
            Next
        End While
    End Sub

    Private Sub QueryAndUpdate()
        Using client As New UdpClient()
            client.Client.ReceiveTimeout = Math.Max(200, ReceiveTimeoutMs)

            Dim targetIp As IPAddress = IPAddress.Broadcast
            If Not String.IsNullOrWhiteSpace(DeviceIpAddress) AndAlso
               Not String.Equals(DeviceIpAddress, "broadcast", StringComparison.OrdinalIgnoreCase) Then
                If Not IPAddress.TryParse(DeviceIpAddress, targetIp) Then
                    targetIp = IPAddress.Broadcast
                End If
            End If

            client.EnableBroadcast = targetIp.Equals(IPAddress.Broadcast)

            Dim ep As New IPEndPoint(targetIp, Port)
            client.Send(LocatorQuery, LocatorQuery.Length, ep)

            Dim remoteEp As New IPEndPoint(IPAddress.Any, 0)
            Dim response As Byte() = client.Receive(remoteEp)

            ParseLocatorResponse(response)
        End Using
    End Sub

    Private Sub ParseLocatorResponse(data As Byte())
        If data Is Nothing Then Return

        Dim ntpCount As Long = -1
        Dim deviceName As String = ""
        Dim serverName As String = ""
        Dim deviceUtcText As String = ""
        Dim locationText As String = ""

        If data.Length >= 80 AndAlso (data(0) = &H4 OrElse data(0) = &H5) Then
            ' TM1000/TM2000 format
            ntpCount = ParseUInt32BigEndian(data, 14)
            deviceName = $"TM{If(data(0) = &H4, "1000", "2000")}"
            serverName = ReadNullTerminatedAscii(data, 46, 34)
            deviceUtcText = $"{data(18):D2}:{data(19):D2}:{data(20):D2}"
            locationText = ReadNullTerminatedAscii(data, 21, 25)

        ElseIf data.Length >= 40 AndAlso (data(0) = &H1 OrElse data(0) = &H2 OrElse data(0) = &H3) Then
            ' POE/WiFi/DotMatrix format
            ntpCount = ParseUInt16BigEndian(data, 13)
            deviceName = ReadNullTerminatedAscii(data, 24, 16)
            If String.IsNullOrWhiteSpace(deviceName) Then
                deviceName = GetClockTypeName(data(0))
            End If
        Else
            Return
        End If

        SyncLock _stateLock
            _lastUpdateUtc = DateTime.UtcNow
            _lastResponseUtc = _lastUpdateUtc

            _deviceName = deviceName
            _timeServerName = serverName
            _deviceUtcTimeText = deviceUtcText
            _deviceLocationText = locationText

            _lastNtpSyncCount = _currentNtpSyncCount
            _currentNtpSyncCount = ntpCount

            If _lastNtpSyncCount < 0 Then
                _syncState = "Initializing"
            ElseIf _currentNtpSyncCount > _lastNtpSyncCount Then
                _syncState = "Syncing"
            Else
                _syncState = "Stable"
            End If
        End SyncLock
    End Sub

    Private Shared Function ReadNullTerminatedAscii(data As Byte(), start As Integer, length As Integer) As String
        If data Is Nothing OrElse start < 0 OrElse length <= 0 OrElse start >= data.Length Then Return ""

        Dim maxLen As Integer = Math.Min(length, data.Length - start)
        Dim raw As Byte() = New Byte(maxLen - 1) {}
        Array.Copy(data, start, raw, 0, maxLen)

        Dim s As String = Encoding.ASCII.GetString(raw)
        Dim nullIndex As Integer = s.IndexOf(ChrW(0))
        If nullIndex >= 0 Then
            s = s.Substring(0, nullIndex)
        End If

        Return s.Trim()
    End Function

    Private Shared Function ParseUInt16BigEndian(data As Byte(), offset As Integer) As UInteger
        If data Is Nothing OrElse data.Length < offset + 2 Then Return 0UI
        Return CUInt((CUInt(data(offset)) << 8) Or CUInt(data(offset + 1)))
    End Function

    Private Shared Function ParseUInt32BigEndian(data As Byte(), offset As Integer) As UInteger
        If data Is Nothing OrElse data.Length < offset + 4 Then Return 0UI

        Dim b0 = CUInt(data(offset))
        Dim b1 = CUInt(data(offset + 1))
        Dim b2 = CUInt(data(offset + 2))
        Dim b3 = CUInt(data(offset + 3))

        Return (b0 << 24) Or (b1 << 16) Or (b2 << 8) Or b3
    End Function

    Private Shared Function GetClockTypeName(deviceType As Byte) As String
        Select Case deviceType
            Case &H1 : Return "POE"
            Case &H2 : Return "WiFi"
            Case &H3 : Return "DotMatrix"
            Case Else : Return "Unknown"
        End Select
    End Function
End Class
