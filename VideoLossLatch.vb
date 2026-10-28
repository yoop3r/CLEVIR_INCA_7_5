Option Strict On
Option Explicit On

''' <summary>
''' Cameras whose video was lost during the current recording. INCA cannot re-synchronize a camera's frames with the
''' MF4 data after a loss (ETAS localizes the video before it is written to the session folder), so a timecode that
''' starts updating again is not a recovery: the loss stands until a new recording or a Re-Initialize. An MF4
''' sequence rollover continues the same recording and keeps the loss.
''' A new recording (Stop Record, reconnect the camera, Start Record) restores the video.
''' The driver can choose to continue without a camera's video (a dead camera is worth less than the vehicle data):
''' that camera's loss is still latched (GO/NOGO red) but no longer alerted - across new recordings - until its video
''' is seen updating again in a later recording, or Re-Initialize. Other cameras, processors and data keep alerting.
''' Thread-safe: the ProcessKiller thread reads it for the voice alerts.
''' </summary>
Friend NotInheritable Class VideoLossLatch

    Private ReadOnly _gate As New Object()
    Private ReadOnly _devices As New List(Of String)
    Private ReadOnly _accepted As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    Public ReadOnly Property IsLatched As Boolean
        Get
            SyncLock _gate
                Return _devices.Count > 0
            End SyncLock
        End Get
    End Property

    Public ReadOnly Property DeviceList As String
        Get
            SyncLock _gate
                Return String.Join(",", _devices)
            End SyncLock
        End Get
    End Property

    ''' <summary>Records a camera whose video froze while recording. Returns True if it was not already latched.</summary>
    Public Function Record(deviceName As String) As Boolean
        If String.IsNullOrWhiteSpace(deviceName) Then Return False
        Dim name As String = deviceName.Trim()
        SyncLock _gate
            If IsLatchedLocked(name) Then Return False
            _devices.Add(name)
            Return True
        End SyncLock
    End Function

    ''' <summary>Data-frozen tracking was reset. Returns True if the latch was cleared (a new recording, not a rollover).</summary>
    Public Function OnTrackingReset(sequenceRotation As Boolean) As Boolean
        SyncLock _gate
            If sequenceRotation OrElse _devices.Count = 0 Then Return False
            _devices.Clear()
            Return True
        End SyncLock
    End Function

    ''' <summary>Re-Initialize: clears the loss and every continue-without choice. Returns True if anything was set.</summary>
    Public Function Clear() As Boolean
        SyncLock _gate
            If _devices.Count = 0 AndAlso _accepted.Count = 0 Then Return False
            _devices.Clear()
            _accepted.Clear()
            Return True
        End SyncLock
    End Function

    ''' <summary>The driver chose to continue without these cameras' video (comma-separated).</summary>
    Public Sub Accept(deviceList As String)
        SyncLock _gate
            For Each name As String In Split(deviceList)
                _accepted.Add(name)
            Next
        End SyncLock
    End Sub

    ''' <summary>
    ''' The camera's video is updating while recording. If the driver had chosen to continue without it and its loss is
    ''' not latched (i.e. this is a later recording), its video is back: its alerts are re-enabled. Returns True then.
    ''' </summary>
    Public Function Restored(deviceName As String) As Boolean
        If String.IsNullOrWhiteSpace(deviceName) Then Return False
        Dim name As String = deviceName.Trim()
        SyncLock _gate
            If IsLatchedLocked(name) Then Return False
            Return _accepted.Remove(name)
        End SyncLock
    End Function

    Public ReadOnly Property AcceptedList As String
        Get
            SyncLock _gate
                Return String.Join(",", _accepted)
            End SyncLock
        End Get
    End Property

    ''' <summary>
    ''' The cameras in deviceList (comma-separated) that should still be alerted, i.e. not accepted. "" when all are
    ''' accepted. An empty deviceList (camera not yet identified) is returned as is - it cannot have been accepted.
    ''' </summary>
    Public Function Unaccepted(deviceList As String) As String
        SyncLock _gate
            Return String.Join(",", Split(deviceList).Where(Function(d) Not _accepted.Contains(d)))
        End SyncLock
    End Function

    ''' <summary>True when deviceList names at least one camera and every one of them has been accepted.</summary>
    Public Function AllAccepted(deviceList As String) As Boolean
        Dim names As List(Of String) = Split(deviceList)
        SyncLock _gate
            Return names.Count > 0 AndAlso names.All(Function(d) _accepted.Contains(d))
        End SyncLock
    End Function

    Private Function IsLatchedLocked(name As String) As Boolean
        Return _devices.Exists(Function(d) String.Equals(d, name, StringComparison.OrdinalIgnoreCase))
    End Function

    Private Shared Function Split(deviceList As String) As List(Of String)
        If String.IsNullOrWhiteSpace(deviceList) Then Return New List(Of String)()
        Return deviceList.Split(","c).Select(Function(d) d.Trim()).Where(Function(d) d <> "").
                          Distinct(StringComparer.OrdinalIgnoreCase).ToList()
    End Function

End Class
