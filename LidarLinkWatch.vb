Option Strict On
Option Explicit On

''' <summary>What the latest look at one LiDAR's packet stream means for the driver.</summary>
Friend Enum LidarLinkEvent
    None
    ''' <summary>No packets for the lost-after period while capturing - announce the loss.</summary>
    Lost
    ''' <summary>Still no packets - repeat the loss announcement.</summary>
    StillLost
    ''' <summary>Packets are arriving again after an announced loss.</summary>
    Restored
    ''' <summary>The link is up but packet loss is critical.</summary>
    PacketLoss
End Enum

''' <summary>
''' Turns one LiDAR's packet timestamps into lost/restored events for the driver alert.
''' Silence is measured from the latest of: the last packet, the start of the current capture, and the moment
''' monitoring began - so a device that never sends a packet is caught, and a capture restart (MF4 rollover
''' resets the packet timestamp) neither hides nor fakes a loss. An announced loss stays outstanding across
''' capture restarts until packets are seen again, so the recovery is always announced.
''' </summary>
Friend NotInheritable Class LidarLinkWatch

    Public Const PacketLossMinimumPackets As Long = 100
    Public Const PacketLossCriticalPercent As Double = 20.0

    Private ReadOnly _lostAfter As TimeSpan
    Private ReadOnly _repeatEvery As TimeSpan
    Private _monitoringSince As DateTime?
    Private _isLost As Boolean
    Private _silentSince As DateTime
    Private _lastLostAnnouncement As DateTime
    Private _lastPacketLossAnnouncement As DateTime = DateTime.MinValue

    Public Sub New(lostAfter As TimeSpan, repeatEvery As TimeSpan)
        If lostAfter <= TimeSpan.Zero Then Throw New ArgumentOutOfRangeException(NameOf(lostAfter))
        If repeatEvery <= TimeSpan.Zero Then Throw New ArgumentOutOfRangeException(NameOf(repeatEvery))
        _lostAfter = lostAfter
        _repeatEvery = repeatEvery
    End Sub

    ''' <summary>True while an announced loss is outstanding.</summary>
    Public ReadOnly Property IsLost As Boolean
        Get
            Return _isLost
        End Get
    End Property

    ''' <summary>Start of the silence behind the outstanding loss (meaningful while <see cref="IsLost"/>).</summary>
    Public ReadOnly Property SilentSince As DateTime
        Get
            Return _silentSince
        End Get
    End Property

    ''' <param name="monitoring">Capture is running for this device, so packets are expected.</param>
    ''' <param name="lastPacket">When the last packet arrived (Nothing if none since the capture started).</param>
    ''' <param name="captureStartedAt">When the current capture started (Nothing if it never did).</param>
    ''' <param name="packets">Packets received in the current capture.</param>
    ''' <param name="dropped">Packets dropped in the current capture.</param>
    Public Function Update(monitoring As Boolean, lastPacket As DateTime?, captureStartedAt As DateTime?,
                           packets As Long, dropped As Long, now As DateTime) As LidarLinkEvent
        If Not monitoring Then
            ' Nothing is expected between captures; an announced loss stays outstanding.
            _monitoringSince = Nothing
            Return LidarLinkEvent.None
        End If
        If Not _monitoringSince.HasValue Then _monitoringSince = now

        Dim silentSince As DateTime = _monitoringSince.Value
        If captureStartedAt.HasValue AndAlso captureStartedAt.Value > silentSince Then silentSince = captureStartedAt.Value
        If lastPacket.HasValue AndAlso lastPacket.Value > silentSince Then silentSince = lastPacket.Value
        Dim silent As Boolean = now - silentSince >= _lostAfter

        If _isLost Then
            ' Only a packet newer than the lost-after window proves the link is back; a capture restart does not.
            If lastPacket.HasValue AndAlso now - lastPacket.Value < _lostAfter Then
                _isLost = False
                Return LidarLinkEvent.Restored
            End If
            If silent AndAlso now - _lastLostAnnouncement >= _repeatEvery Then
                _lastLostAnnouncement = now
                Return LidarLinkEvent.StillLost
            End If
            Return LidarLinkEvent.None
        End If

        If silent Then
            _isLost = True
            _silentSince = silentSince
            _lastLostAnnouncement = now
            Return LidarLinkEvent.Lost
        End If

        If IsPacketLossCritical(packets, dropped) AndAlso now - _lastPacketLossAnnouncement >= _repeatEvery Then
            _lastPacketLossAnnouncement = now
            Return LidarLinkEvent.PacketLoss
        End If

        Return LidarLinkEvent.None
    End Function

    Public Shared Function IsPacketLossCritical(packets As Long, dropped As Long) As Boolean
        Dim total As Long = packets + dropped
        If total <= PacketLossMinimumPackets Then Return False
        Return CDbl(dropped) / CDbl(total) * 100.0 >= PacketLossCriticalPercent
    End Function

End Class
