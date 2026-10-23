Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class LoopFaultTrackerTests

    Private Shared ReadOnly T0 As New DateTime(2026, 10, 23, 9, 0, 0)

    Private Shared Function NewTracker() As LoopFaultTracker
        Return New LoopFaultTracker(escalateAfter:=TimeSpan.FromSeconds(5),
                                    alertRepeatInterval:=TimeSpan.FromSeconds(120),
                                    logRepeatInterval:=TimeSpan.FromSeconds(30))
    End Function

    <TestMethod>
    Public Sub FirstFailure_IsLoggedButNotEscalated()
        Dim tracker = NewTracker()

        Dim d = tracker.RecordFailure("A", T0)

        Assert.AreEqual(LoopFaultActions.LogDetail, d.Actions)
        Assert.AreEqual(1, d.ConsecutiveFailures)
        Assert.IsFalse(tracker.IsEscalated)
    End Sub

    <TestMethod>
    Public Sub RepeatedSameFailure_IsSuppressedUntilLogIntervalThenReportsCount()
        Dim tracker = NewTracker()
        tracker.RecordFailure("A", T0)

        For i = 1 To 10
            Assert.IsFalse(tracker.RecordFailure("A", T0.AddMilliseconds(250 * i)).Has(LoopFaultActions.LogDetail))
        Next

        Dim d = tracker.RecordFailure("A", T0.AddSeconds(30))
        Assert.IsTrue(d.Has(LoopFaultActions.LogDetail))
        Assert.AreEqual(10, d.SuppressedSinceLastLog)
        Assert.AreEqual(12, d.ConsecutiveFailures)
    End Sub

    <TestMethod>
    Public Sub DifferentSignature_IsLoggedImmediately()
        Dim tracker = NewTracker()
        tracker.RecordFailure("A", T0)

        Assert.IsTrue(tracker.RecordFailure("B", T0.AddMilliseconds(250)).Has(LoopFaultActions.LogDetail))
    End Sub

    <TestMethod>
    Public Sub PersistentFailure_EscalatesOnceThenRepeatsAlertAtInterval()
        Dim tracker = NewTracker()
        tracker.RecordFailure("A", T0)
        Assert.IsFalse(tracker.RecordFailure("A", T0.AddSeconds(4.9)).Has(LoopFaultActions.Escalate))

        Dim escalated = tracker.RecordFailure("A", T0.AddSeconds(5))
        Assert.IsTrue(escalated.Has(LoopFaultActions.Escalate))
        Assert.IsTrue(tracker.IsEscalated)

        Dim afterEscalation = tracker.RecordFailure("A", T0.AddSeconds(6))
        Assert.IsFalse(afterEscalation.Has(LoopFaultActions.Escalate))
        Assert.IsFalse(afterEscalation.Has(LoopFaultActions.RepeatAlert))

        Assert.IsFalse(tracker.RecordFailure("A", T0.AddSeconds(124)).Has(LoopFaultActions.RepeatAlert))
        Dim reminder = tracker.RecordFailure("A", T0.AddSeconds(125))
        Assert.IsTrue(reminder.Has(LoopFaultActions.RepeatAlert))
        Assert.IsFalse(reminder.Has(LoopFaultActions.Escalate))
    End Sub

    <TestMethod>
    Public Sub Success_AfterEscalation_ReportsRecoveryAndResets()
        Dim tracker = NewTracker()
        tracker.RecordFailure("A", T0)
        tracker.RecordFailure("A", T0.AddSeconds(5))
        tracker.RecordFailure("A", T0.AddSeconds(6))

        Dim recovery = tracker.RecordSuccess(T0.AddSeconds(7))

        Assert.IsNotNull(recovery)
        Assert.AreEqual(3, recovery.FailedPasses)
        Assert.AreEqual(TimeSpan.FromSeconds(7), recovery.FailedFor)
        Assert.IsTrue(recovery.WasEscalated)
        Assert.IsTrue(recovery.ShouldLog)
        Assert.IsFalse(tracker.IsEscalated)
        Assert.AreEqual(0, tracker.ConsecutiveFailures)
        Assert.IsNull(tracker.RecordSuccess(T0.AddSeconds(8)))
    End Sub

    <TestMethod>
    Public Sub NewFailureRun_AfterRecovery_MustPersistAgainBeforeEscalating()
        Dim tracker = NewTracker()
        tracker.RecordFailure("A", T0)
        tracker.RecordFailure("A", T0.AddSeconds(5))
        tracker.RecordSuccess(T0.AddSeconds(6))

        Assert.IsFalse(tracker.RecordFailure("A", T0.AddSeconds(7)).Has(LoopFaultActions.Escalate))
        Assert.IsTrue(tracker.RecordFailure("A", T0.AddSeconds(12)).Has(LoopFaultActions.Escalate))
    End Sub

    <TestMethod>
    Public Sub IntermittentFailure_IsRateLimitedAcrossRunsAndNeverEscalates()
        Dim tracker = NewTracker()
        Dim loggedFailures = 0
        Dim loggedRecoveries = 0

        ' Fails every other pass for 20 seconds.
        For i = 0 To 79
            Dim now = T0.AddMilliseconds(250 * i)
            If i Mod 2 = 0 Then
                Dim d = tracker.RecordFailure("A", now)
                Assert.IsFalse(d.Has(LoopFaultActions.Escalate))
                If d.Has(LoopFaultActions.LogDetail) Then loggedFailures += 1
            Else
                If tracker.RecordSuccess(now).ShouldLog Then loggedRecoveries += 1
            End If
        Next

        Assert.AreEqual(1, loggedFailures)
        Assert.AreEqual(1, loggedRecoveries)
    End Sub

End Class
