Option Strict On
Option Explicit On

''' <summary>What the latest INCA device-status poll means for the driver.</summary>
Friend Enum LostDeviceTransition
    None
    ''' <summary>A device fault has persisted past the debounce while measuring/recording - alert the driver.</summary>
    Raised
    ''' <summary>A fault the driver was alerted to has cleared - tell the driver.</summary>
    Restored
End Enum

''' <summary>
''' Debounces the 5 s INCA device-status poll into lost/restored transitions for the lost-device alert.
''' A fault is raised only after it has persisted for the debounce period while measuring or recording.
''' Once raised it stays raised - across measurement/recording restarts and MF4 rollovers - until the
''' devices report OK again, the driver acknowledges it, or alerts are suppressed.
''' </summary>
Friend NotInheritable Class LostDeviceMonitor

    Private ReadOnly _debounce As TimeSpan
    Private _faultSince As DateTime?
    Private _raised As Boolean
    Private _alerted As Boolean
    Private _episodeStartedAt As DateTime

    Public Sub New(debounce As TimeSpan)
        If debounce < TimeSpan.Zero Then Throw New ArgumentOutOfRangeException(NameOf(debounce))
        _debounce = debounce
    End Sub

    ''' <summary>True while a raised fault is outstanding (drives the repeating voice alert).</summary>
    Public ReadOnly Property IsRaised As Boolean
        Get
            Return _raised
        End Get
    End Property

    ''' <summary>
    ''' When the fault behind the current alert episode was first seen. Kept across acknowledgements so
    ''' the driver is told how long the device has really been lost.
    ''' </summary>
    Public ReadOnly Property EpisodeStartedAt As DateTime
        Get
            Return _episodeStartedAt
        End Get
    End Property

    ''' <param name="devicesOk">Raw result of the latest poll (ignore flags not applied).</param>
    ''' <param name="monitoring">INCA is measuring or recording.</param>
    ''' <param name="alertsAllowed">False while the driver has chosen to ignore alerts.</param>
    Public Function Update(devicesOk As Boolean, monitoring As Boolean, alertsAllowed As Boolean, now As DateTime) As LostDeviceTransition
        If Not alertsAllowed Then
            Reset()
            Return LostDeviceTransition.None
        End If

        If devicesOk Then
            _faultSince = Nothing
            _raised = False
            If _alerted Then
                _alerted = False
                Return LostDeviceTransition.Restored
            End If
            Return LostDeviceTransition.None
        End If

        If Not monitoring Then
            ' Only measure the debounce while data is expected; an already raised fault stays raised.
            _faultSince = Nothing
            Return LostDeviceTransition.None
        End If

        If Not _faultSince.HasValue Then _faultSince = now

        If Not _raised AndAlso now - _faultSince.Value >= _debounce Then
            If Not _alerted Then _episodeStartedAt = _faultSince.Value
            _raised = True
            _alerted = True
            Return LostDeviceTransition.Raised
        End If

        Return LostDeviceTransition.None
    End Function

    ''' <summary>
    ''' The driver answered the alert while the fault persists: stop the alarm, and raise it again if the
    ''' device is still lost after another debounce period. Recovery is still announced.
    ''' </summary>
    Public Sub Acknowledge(now As DateTime)
        If Not _raised Then Return
        _raised = False
        _faultSince = now
    End Sub

    ''' <summary>Forget everything without announcing a recovery.</summary>
    Public Sub Reset()
        _faultSince = Nothing
        _raised = False
        _alerted = False
    End Sub

End Class
