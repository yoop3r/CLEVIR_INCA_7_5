Option Strict On
Option Explicit On

''' <summary>
''' Decides when frozen video has really recovered. While a camera is unplugged INCA's timecode can tick again for
''' ~20 s and then freeze (bench 10/28 09:56:58 and 11:50:22), so a single "Resumed Updating" is not enough:
''' video must stay healthy for the whole confirmation period.
''' </summary>
Friend NotInheritable Class VideoResumeWatch

    Private ReadOnly _confirmAfter As TimeSpan
    Private _healthySince As DateTime = DateTime.MinValue

    Public Sub New(confirmAfter As TimeSpan)
        _confirmAfter = confirmAfter
    End Sub

    ''' <summary>Feed every poll; returns True once video has been healthy continuously for the confirmation period.</summary>
    Public Function Update(videoHealthy As Boolean, now As DateTime) As Boolean
        If Not videoHealthy Then
            _healthySince = DateTime.MinValue
            Return False
        End If
        If _healthySince = DateTime.MinValue Then _healthySince = now
        Return now - _healthySince >= _confirmAfter
    End Function

    Public Sub Reset()
        _healthySince = DateTime.MinValue
    End Sub

End Class
