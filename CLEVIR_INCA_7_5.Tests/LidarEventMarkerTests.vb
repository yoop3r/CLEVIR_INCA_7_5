Imports System.IO
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class LidarEventMarkerTests

    <TestMethod>
    Public Sub SnapshotFrameAt_ReadsDeviceOrderedFrames()
        Dim frames = "298151, 298154".Split(","c)
        Assert.AreEqual(298151L, LidarPcapCapture.SnapshotFrameAt(frames, 0))
        Assert.AreEqual(298154L, LidarPcapCapture.SnapshotFrameAt(frames, 1))
    End Sub

    <TestMethod>
    Public Sub SnapshotFrameAt_MissingOrNotCapturing_IsZero()
        Assert.AreEqual(0L, LidarPcapCapture.SnapshotFrameAt("-1,500".Split(","c), 0))
        Assert.AreEqual(0L, LidarPcapCapture.SnapshotFrameAt("500".Split(","c), 1))
        Assert.AreEqual(0L, LidarPcapCapture.SnapshotFrameAt({""}, 0))
        Assert.AreEqual(0L, LidarPcapCapture.SnapshotFrameAt(Nothing, 0))
    End Sub

    <TestMethod>
    Public Sub EventLog_WritesEventFrameAndMarkerFrame()
        Dim dir = Path.Combine(Path.GetTempPath(), "clevir_evt_" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(dir)
        Try
            Dim pcap = Path.Combine(dir, "x_LiDAR1.pcap")
            Dim ts = New DateTime(2026, 10, 23, 16, 25, 59, 151, DateTimeKind.Utc)
            Using logger As New LidarEventLogger(pcap)
                logger.LogEvent(298151, ts, "BUTTON FEEDBACK", "ACC Event", 1, 319619)
                logger.LogEvent(1, ts, "START", "Recording started", 1)
            End Using

            Dim lines = File.ReadAllLines(Path.ChangeExtension(pcap, ".lidar_events.txt")).Where(Function(l) Not l.StartsWith("#")).ToArray()
            CollectionAssert.AreEqual({"298151|2026-10-23 16:25:59.151|BUTTON FEEDBACK|ACC Event|1|319619",
                                      "1|2026-10-23 16:25:59.151|START|Recording started|1|1"}, lines)
        Finally
            Directory.Delete(dir, True)
        End Try
    End Sub

End Class
