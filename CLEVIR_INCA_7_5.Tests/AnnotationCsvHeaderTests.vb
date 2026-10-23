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

End Class
