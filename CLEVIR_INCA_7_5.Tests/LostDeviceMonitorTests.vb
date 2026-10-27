Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class LostDeviceMonitorTests

    Private Shared ReadOnly T0 As New DateTime(2026, 10, 28, 9, 0, 0)

    Private Shared Function NewMonitor() As LostDeviceMonitor
        Return New LostDeviceMonitor(TimeSpan.FromSeconds(25))
    End Function

    ''' <summary>Polls every 5 s from <paramref name="fromSecond"/> to <paramref name="toSecond"/> and returns the transitions seen.</summary>
    Private Shared Function Poll(monitor As LostDeviceMonitor, fromSecond As Integer, toSecond As Integer,
                                 devicesOk As Boolean, Optional monitoring As Boolean = True,
                                 Optional alertsAllowed As Boolean = True) As List(Of LostDeviceTransition)
        Dim seen As New List(Of LostDeviceTransition)
        For s = fromSecond To toSecond Step 5
            Dim t = monitor.Update(devicesOk, monitoring, alertsAllowed, T0.AddSeconds(s))
            If t <> LostDeviceTransition.None Then seen.Add(t)
        Next
        Return seen
    End Function

    <TestMethod>
    Public Sub FaultShorterThanDebounce_IsNotRaised()
        Dim monitor = NewMonitor()

        Assert.AreEqual(0, Poll(monitor, 0, 20, devicesOk:=False).Count)
        Assert.AreEqual(0, Poll(monitor, 25, 25, devicesOk:=True).Count)
        Assert.IsFalse(monitor.IsRaised)
    End Sub

    <TestMethod>
    Public Sub PersistentFault_RaisesOnceAtDebounce()
        Dim monitor = NewMonitor()

        Assert.AreEqual(0, Poll(monitor, 0, 20, devicesOk:=False).Count)
        Assert.AreEqual(LostDeviceTransition.Raised, monitor.Update(False, True, True, T0.AddSeconds(25)))
        Assert.AreEqual(0, Poll(monitor, 30, 300, devicesOk:=False).Count)
        Assert.IsTrue(monitor.IsRaised)
        Assert.AreEqual(T0, monitor.EpisodeStartedAt)
    End Sub

    <TestMethod>
    Public Sub FaultWhileNotMeasuring_IsNotRaised_AndDebounceStartsWhenMeasuringStarts()
        Dim monitor = NewMonitor()

        Assert.AreEqual(0, Poll(monitor, 0, 300, devicesOk:=False, monitoring:=False).Count)
        Assert.AreEqual(0, Poll(monitor, 305, 325, devicesOk:=False).Count)
        Assert.AreEqual(LostDeviceTransition.Raised, monitor.Update(False, True, True, T0.AddSeconds(330)))
    End Sub

    <TestMethod>
    Public Sub RaisedFault_SurvivesRecordingRestartOrRollover()
        Dim monitor = NewMonitor()
        Poll(monitor, 0, 25, devicesOk:=False)

        ' A measurement/recording restart (or MF4 rollover) briefly reports not monitoring.
        Poll(monitor, 30, 30, devicesOk:=False, monitoring:=False)

        Assert.IsTrue(monitor.IsRaised)
        Assert.AreEqual(0, Poll(monitor, 35, 120, devicesOk:=False).Count)
    End Sub

    <TestMethod>
    Public Sub Recovery_AfterRaise_IsAnnouncedOnce()
        Dim monitor = NewMonitor()
        Poll(monitor, 0, 25, devicesOk:=False)

        Assert.AreEqual(LostDeviceTransition.Restored, monitor.Update(True, True, True, T0.AddSeconds(30)))
        Assert.IsFalse(monitor.IsRaised)
        Assert.AreEqual(0, Poll(monitor, 35, 120, devicesOk:=True).Count)
    End Sub

    <TestMethod>
    Public Sub Recovery_WithoutRaise_IsNotAnnounced()
        Dim monitor = NewMonitor()
        Poll(monitor, 0, 20, devicesOk:=False)

        Assert.AreEqual(LostDeviceTransition.None, monitor.Update(True, True, True, T0.AddSeconds(25)))
    End Sub

    <TestMethod>
    Public Sub Acknowledge_SilencesThenRaisesAgainAfterDebounce_KeepingEpisodeStart()
        Dim monitor = NewMonitor()
        Poll(monitor, 0, 25, devicesOk:=False)

        monitor.Acknowledge(T0.AddSeconds(30))

        Assert.IsFalse(monitor.IsRaised)
        Assert.AreEqual(0, Poll(monitor, 35, 50, devicesOk:=False).Count)
        Assert.AreEqual(LostDeviceTransition.Raised, monitor.Update(False, True, True, T0.AddSeconds(55)))
        Assert.AreEqual(T0, monitor.EpisodeStartedAt)
    End Sub

    <TestMethod>
    Public Sub Acknowledge_ThenRecovery_IsStillAnnounced()
        Dim monitor = NewMonitor()
        Poll(monitor, 0, 25, devicesOk:=False)
        monitor.Acknowledge(T0.AddSeconds(30))

        Assert.AreEqual(LostDeviceTransition.Restored, monitor.Update(True, True, True, T0.AddSeconds(35)))
    End Sub

    <TestMethod>
    Public Sub AlertsSuppressed_ClearsStateWithoutAnnouncingRecovery()
        Dim monitor = NewMonitor()
        Poll(monitor, 0, 25, devicesOk:=False)

        Assert.AreEqual(0, Poll(monitor, 30, 300, devicesOk:=False, alertsAllowed:=False).Count)
        Assert.IsFalse(monitor.IsRaised)
        Assert.AreEqual(LostDeviceTransition.None, monitor.Update(True, True, True, T0.AddSeconds(305)))
    End Sub

    <TestMethod>
    Public Sub NewEpisode_AfterRecovery_HasItsOwnStartTime()
        Dim monitor = NewMonitor()
        Poll(monitor, 0, 25, devicesOk:=False)
        Poll(monitor, 30, 30, devicesOk:=True)

        Poll(monitor, 100, 125, devicesOk:=False)

        Assert.IsTrue(monitor.IsRaised)
        Assert.AreEqual(T0.AddSeconds(100), monitor.EpisodeStartedAt)
    End Sub

End Class
