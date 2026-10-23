Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class AnnotationCsvHeaderTests

    <TestMethod>
    Public Sub Build_MatchesLegacyHeaderLayout()
        Assert.AreEqual(AnnotationCsvHeader.BaseColumns, AnnotationCsvHeader.Build(0))
        Assert.AreEqual(AnnotationCsvHeader.BaseColumns & ",LiDAR[1]Frame,LiDAR[2]Frame", AnnotationCsvHeader.Build(2))
        StringAssert.StartsWith(AnnotationCsvHeader.BaseColumns, "0,Anno Type ID,Anno Type,")
        StringAssert.EndsWith(AnnotationCsvHeader.BaseColumns, ",LAT POS,LON POS,LaneClass_Crnt")
    End Sub

    <TestMethod>
    Public Sub StaleOneLidarHeader_IsWidenedToCurrentCount()
        Dim stale = AnnotationCsvHeader.Build(1)
        Assert.AreEqual(AnnotationCsvHeader.Build(2), AnnotationCsvHeader.GetUpgradedHeader(stale, 2))
    End Sub

    <TestMethod>
    Public Sub ExcelTrailingComma_IsRemovedWhileWidening()
        Dim excelSaved = AnnotationCsvHeader.Build(1) & ","
        Assert.AreEqual(AnnotationCsvHeader.Build(2), AnnotationCsvHeader.GetUpgradedHeader(excelSaved, 2))
    End Sub

    <TestMethod>
    Public Sub CurrentHeader_NeedsNoChange()
        Assert.IsNull(AnnotationCsvHeader.GetUpgradedHeader(AnnotationCsvHeader.Build(2), 2))
        Assert.IsNull(AnnotationCsvHeader.GetUpgradedHeader(AnnotationCsvHeader.Build(0), 0))
    End Sub

    <TestMethod>
    Public Sub FewerDevicesNow_KeepsExistingLidarColumns()
        Assert.IsNull(AnnotationCsvHeader.GetUpgradedHeader(AnnotationCsvHeader.Build(2), 1))
    End Sub

    <TestMethod>
    Public Sub NonHeaderOrMissingLine_IsLeftAlone()
        Assert.IsNull(AnnotationCsvHeader.GetUpgradedHeader(Nothing, 2))
        Assert.IsNull(AnnotationCsvHeader.GetUpgradedHeader("3,1000,BUTTON FEEDBACK,3060,ACC Event", 2))
    End Sub

    <TestMethod>
    Public Sub LidarColumnLabels_UseOrientationAndFallBackToPosition()
        CollectionAssert.AreEqual({"FRONT", "REAR"}, AnnotationCsvHeader.LidarColumnLabels({"FRONT", "REAR"}))
        CollectionAssert.AreEqual({"FRONT", "2"}, AnnotationCsvHeader.LidarColumnLabels({" front ", ""}))
        CollectionAssert.AreEqual({"1", "2"}, AnnotationCsvHeader.LidarColumnLabels({Nothing, "  "}))
    End Sub

    <TestMethod>
    Public Sub LidarColumnLabels_DisambiguatesDuplicatesAndSanitizes()
        CollectionAssert.AreEqual({"FRONT_1", "FRONT_2", "REAR"}, AnnotationCsvHeader.LidarColumnLabels({"FRONT", "Front", "REAR"}))
        CollectionAssert.AreEqual({"LEFT_REAR"}, AnnotationCsvHeader.LidarColumnLabels({"Left,Rear"}))
    End Sub

    <TestMethod>
    Public Sub Build_WithOrientationLabels()
        Assert.AreEqual(AnnotationCsvHeader.BaseColumns & ",LiDAR[FRONT]Frame,LiDAR[REAR]Frame",
                        AnnotationCsvHeader.Build({"FRONT", "REAR"}))
    End Sub

    <TestMethod>
    Public Sub NumberedHeader_IsRelabeledToOrientations()
        Dim expected = AnnotationCsvHeader.Build({"FRONT", "REAR"})
        Assert.AreEqual(expected, AnnotationCsvHeader.GetUpgradedHeader(AnnotationCsvHeader.Build(2), {"FRONT", "REAR"}))
        Assert.AreEqual(expected, AnnotationCsvHeader.GetUpgradedHeader(AnnotationCsvHeader.Build(1) & ",", {"FRONT", "REAR"}))
        Assert.IsNull(AnnotationCsvHeader.GetUpgradedHeader(expected, {"FRONT", "REAR"}))
    End Sub

    <TestMethod>
    Public Sub MoreExistingColumnsThanDevices_KeepsExistingLabels()
        Dim existing = AnnotationCsvHeader.Build({"FRONT", "REAR"})
        Assert.IsNull(AnnotationCsvHeader.GetUpgradedHeader(existing, {"FRONT"}))
    End Sub

End Class
