Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class LidarLinkWatchTests

    Private Shared ReadOnly T0 As New DateTime(2026, 10, 28, 9, 0, 0)

    Private Shared Function NewWatch() As LidarLinkWatch
        Return New LidarLinkWatch(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30))
    End Function

    Private Shared Function At(second As Integer) As DateTime
        Return T0.AddSeconds(second)
    End Function

    ''' <summary>
    ''' Simulated LiDAR: capture runs over [captureStart, captureStop) and packets arrive every second over
    ''' [packetsFrom, packetsUntil). Like LidarDevice, the packet timestamp is cleared when a capture starts.
    ''' </summary>
    Private NotInheritable Class FakeLidar
        Public ReadOnly Captures As New List(Of (StartAt As Integer, StopAt As Integer))
        Public ReadOnly PacketSpans As New List(Of (FromS As Integer, UntilS As Integer))

        Public Function Monitoring(s As Integer) As Boolean
            Return Captures.Exists(Function(c) s >= c.StartAt AndAlso s < c.StopAt)
        End Function

        Public Function CaptureStartedAt(s As Integer) As DateTime?
            Dim started = Captures.Where(Function(c) c.StartAt <= s).Select(Function(c) c.StartAt).DefaultIfEmpty(-1).Max()
            Return If(started < 0, CType(Nothing, DateTime?), At(started))
        End Function

        Public Function LastPacket(s As Integer) As DateTime?
            Dim started = CaptureStartedAt(s)
            Dim last As Integer = -1
            For Each span In PacketSpans
                Dim upTo As Integer = Math.Min(s, span.UntilS - 1)
                If upTo >= span.FromS AndAlso upTo > last Then last = upTo
            Next
            If last < 0 Then Return Nothing
            If started.HasValue AndAlso At(last) < started.Value Then Return Nothing
            Return At(last)
        End Function
    End Class

    ''' <summary>Ticks once a second over [fromS, toS] and returns (second, event) for every non-None event.</summary>
    Private Shared Function Run(watch As LidarLinkWatch, lidar As FakeLidar, fromS As Integer, toS As Integer,
                                Optional packets As Long = 1000, Optional dropped As Long = 0) As List(Of (Second As Integer, Evt As LidarLinkEvent))
        Dim seen As New List(Of (Second As Integer, Evt As LidarLinkEvent))
        For s = fromS To toS
            Dim e = watch.Update(lidar.Monitoring(s), lidar.LastPacket(s), lidar.CaptureStartedAt(s), packets, dropped, At(s))
            If e <> LidarLinkEvent.None Then seen.Add((s, e))
        Next
        Return seen
    End Function

    <TestMethod>
    Public Sub HealthyStream_RaisesNothing()
        Dim lidar As New FakeLidar()
        lidar.Captures.Add((0, 600))
        lidar.PacketSpans.Add((1, 600))

        Assert.IsEmpty(Run(NewWatch(), lidar, 0, 599))
    End Sub

    <TestMethod>
    Public Sub DeviceThatNeverSendsAPacket_IsLostAfterTheThreshold()
        Dim lidar As New FakeLidar()
        lidar.Captures.Add((0, 600))
        Dim watch = NewWatch()

        Dim seen = Run(watch, lidar, 0, 15)

        Assert.HasCount(1, seen)
        Assert.AreEqual((15, LidarLinkEvent.Lost), seen(0))
        Assert.IsTrue(watch.IsLost)
        Assert.AreEqual(At(0), watch.SilentSince)
    End Sub

    <TestMethod>
    Public Sub DroppedLink_IsAnnouncedRemindedAndRestored()
        Dim lidar As New FakeLidar()
        lidar.Captures.Add((0, 600))
        lidar.PacketSpans.Add((0, 21))   ' last packet at 20 s
        lidar.PacketSpans.Add((100, 600))
        Dim watch = NewWatch()

        Dim seen = Run(watch, lidar, 0, 120)

        CollectionAssert.AreEqual(
            {(35, LidarLinkEvent.Lost), (65, LidarLinkEvent.StillLost), (95, LidarLinkEvent.StillLost), (100, LidarLinkEvent.Restored)},
            seen.ToArray())
        Assert.IsFalse(watch.IsLost)
    End Sub

    <TestMethod>
    Public Sub CaptureRestartWhileLost_DoesNotFakeARecovery()
        ' Rollover at 50 s: capture stops for 3 s and restarts, clearing the packet timestamp.
        Dim lidar As New FakeLidar()
        lidar.Captures.Add((0, 50))
        lidar.Captures.Add((53, 600))
        lidar.PacketSpans.Add((0, 11))   ' last packet at 10 s
        Dim watch = NewWatch()

        Dim seen = Run(watch, lidar, 0, 120)

        ' Lost at 25 s. After the restart the silence is measured from 53 s, so the reminder waits until 68 s
        ' (silent again) rather than firing at 55 s, and the restart itself is not mistaken for a recovery.
        CollectionAssert.AreEqual(
            {(25, LidarLinkEvent.Lost), (68, LidarLinkEvent.StillLost), (98, LidarLinkEvent.StillLost)},
            seen.ToArray())
        Assert.IsTrue(watch.IsLost)
    End Sub

    <TestMethod>
    Public Sub CaptureRestartWhileHealthy_IsNotALoss()
        ' Rollover at 300 s; packets resume 3 s into the new capture.
        Dim lidar As New FakeLidar()
        lidar.Captures.Add((0, 300))
        lidar.Captures.Add((302, 900))
        lidar.PacketSpans.Add((1, 300))
        lidar.PacketSpans.Add((305, 900))

        Assert.IsEmpty(Run(NewWatch(), lidar, 0, 899))
    End Sub

    <TestMethod>
    Public Sub NothingIsRaisedWhileNotCapturing()
        Dim lidar As New FakeLidar()

        Assert.IsEmpty(Run(NewWatch(), lidar, 0, 300))
    End Sub

    <TestMethod>
    Public Sub LossOutstandingAtRecordingStop_IsRestoredWhenTheNextRecordingGetsPackets()
        Dim lidar As New FakeLidar()
        lidar.Captures.Add((0, 40))      ' recording stops at 40 s
        lidar.Captures.Add((200, 600))   ' next recording
        lidar.PacketSpans.Add((0, 11))
        lidar.PacketSpans.Add((202, 600))
        Dim watch = NewWatch()

        Dim seen = Run(watch, lidar, 0, 260)

        CollectionAssert.AreEqual({(25, LidarLinkEvent.Lost), (202, LidarLinkEvent.Restored)}, seen.ToArray())
    End Sub

    <TestMethod>
    Public Sub CriticalPacketLoss_IsAnnouncedAndRepeatedOnlyWhileTheLinkIsUp()
        Dim lidar As New FakeLidar()
        lidar.Captures.Add((0, 600))
        lidar.PacketSpans.Add((0, 600))
        Dim watch = NewWatch()

        Dim seen = Run(watch, lidar, 0, 60, packets:=700, dropped:=300)

        CollectionAssert.AreEqual(
            {(0, LidarLinkEvent.PacketLoss), (30, LidarLinkEvent.PacketLoss), (60, LidarLinkEvent.PacketLoss)},
            seen.ToArray())
    End Sub

    <TestMethod>
    Public Sub PacketLossThreshold()
        Assert.IsFalse(LidarLinkWatch.IsPacketLossCritical(packets:=50, dropped:=50))   ' too few to judge
        Assert.IsFalse(LidarLinkWatch.IsPacketLossCritical(packets:=900, dropped:=199))
        Assert.IsTrue(LidarLinkWatch.IsPacketLossCritical(packets:=800, dropped:=200))
    End Sub

End Class
