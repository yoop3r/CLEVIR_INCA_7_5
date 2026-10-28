Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class VideoLossLatchTests

    <TestMethod>
    Public Sub NewLatch_IsNotLatched()
        Dim latch As New VideoLossLatch()

        Assert.IsFalse(latch.IsLatched)
        Assert.AreEqual("", latch.DeviceList)
    End Sub

    <TestMethod>
    Public Sub Record_LatchesEachCameraOnce()
        Dim latch As New VideoLossLatch()

        Assert.IsTrue(latch.Record("REAR"))
        Assert.IsFalse(latch.Record("rear"))
        Assert.IsTrue(latch.Record("FRONT"))
        Assert.IsFalse(latch.Record(""))

        Assert.AreEqual("REAR,FRONT", latch.DeviceList)
    End Sub

    <TestMethod>
    Public Sub SequenceRollover_KeepsTheLoss()
        Dim latch As New VideoLossLatch()
        latch.Record("REAR")

        Assert.IsFalse(latch.OnTrackingReset(sequenceRotation:=True))
        Assert.IsTrue(latch.IsLatched)
    End Sub

    <TestMethod>
    Public Sub NewRecording_ClearsTheLoss()
        Dim latch As New VideoLossLatch()
        latch.Record("REAR")

        Assert.IsTrue(latch.OnTrackingReset(sequenceRotation:=False))
        Assert.IsFalse(latch.IsLatched)
        Assert.IsFalse(latch.OnTrackingReset(sequenceRotation:=False))
    End Sub

    <TestMethod>
    Public Sub Clear_ReportsWhetherAnythingWasLatched()
        Dim latch As New VideoLossLatch()
        Assert.IsFalse(latch.Clear())

        latch.Record("REAR")
        Assert.IsTrue(latch.Clear())
        Assert.IsFalse(latch.IsLatched)
    End Sub

End Class
