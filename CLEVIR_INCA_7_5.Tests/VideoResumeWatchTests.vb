Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class VideoResumeWatchTests

    Private Shared ReadOnly T0 As New DateTime(2026, 10, 28, 11, 50, 0)

    Private Shared Function NewWatch() As VideoResumeWatch
        Return New VideoResumeWatch(TimeSpan.FromSeconds(45))
    End Function

    ''' <summary>Feeds 5 s polls over [fromS, untilS] and returns the first second that confirmed, or -1.</summary>
    Private Shared Function Poll(watch As VideoResumeWatch, fromS As Integer, untilS As Integer, healthy As Func(Of Integer, Boolean)) As Integer
        For s As Integer = fromS To untilS Step 5
            If watch.Update(healthy(s), T0.AddSeconds(s)) Then Return s
        Next
        Return -1
    End Function

    <TestMethod>
    Public Sub BenchFalseResume_WhileUnplugged_DoesNotConfirm()
        ' Bench 11:50: frozen, timecode ticks 11:50:22-11:50:46 while still unplugged, frozen again until 11:51:34.
        Dim watch = NewWatch()
        Dim confirmedAt = Poll(watch, 20, 90, Function(s) s >= 22 AndAlso s < 46)
        Assert.AreEqual(-1, confirmedAt)
    End Sub

    <TestMethod>
    Public Sub BenchRealResume_ConfirmsAfter45s()
        Dim watch = NewWatch()
        Dim confirmedAt = Poll(watch, 20, 200, Function(s) (s >= 22 AndAlso s < 46) OrElse s >= 95)
        Assert.AreEqual(140, confirmedAt)
    End Sub

    <TestMethod>
    Public Sub Reset_RestartsTheConfirmationPeriod()
        Dim watch = NewWatch()
        Assert.IsFalse(watch.Update(True, T0))
        watch.Reset()
        Assert.IsFalse(watch.Update(True, T0.AddSeconds(30)))
        Assert.IsFalse(watch.Update(True, T0.AddSeconds(70)))
        Assert.IsTrue(watch.Update(True, T0.AddSeconds(75)))
    End Sub

End Class
