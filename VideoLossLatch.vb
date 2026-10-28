Option Strict On
Option Explicit On

''' <summary>
''' Cameras whose video was lost during the current recording. INCA cannot re-synchronize a camera's frames with the
''' MF4 data after a loss (ETAS localizes the video before it is written to the session folder), so a timecode that
''' starts updating again is not a recovery: the loss stands until a new recording or a Re-Initialize. An MF4
''' sequence rollover continues the same recording and keeps the loss.
''' </summary>
Friend NotInheritable Class VideoLossLatch

    Private ReadOnly _devices As New List(Of String)

    Public ReadOnly Property IsLatched As Boolean
        Get
            Return _devices.Count > 0
        End Get
    End Property

    Public ReadOnly Property DeviceList As String
        Get
            Return String.Join(",", _devices)
        End Get
    End Property

    ''' <summary>Records a camera whose video froze while recording. Returns True if it was not already latched.</summary>
    Public Function Record(deviceName As String) As Boolean
        If String.IsNullOrWhiteSpace(deviceName) Then Return False
        Dim name As String = deviceName.Trim()
        If _devices.Exists(Function(d) String.Equals(d, name, StringComparison.OrdinalIgnoreCase)) Then Return False
        _devices.Add(name)
        Return True
    End Function

    ''' <summary>Data-frozen tracking was reset. Returns True if the latch was cleared (a new recording, not a rollover).</summary>
    Public Function OnTrackingReset(sequenceRotation As Boolean) As Boolean
        If sequenceRotation OrElse _devices.Count = 0 Then Return False
        _devices.Clear()
        Return True
    End Function

    ''' <summary>Returns True if anything was latched.</summary>
    Public Function Clear() As Boolean
        If _devices.Count = 0 Then Return False
        _devices.Clear()
        Return True
    End Function

End Class
