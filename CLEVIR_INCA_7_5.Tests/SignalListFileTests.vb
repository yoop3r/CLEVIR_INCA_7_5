Imports System.IO
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class SignalListFileTests

    <TestMethod>
    <DataRow("C:\S\list.csv", True)>
    <DataRow("C:\S\LIST.CSV", True)>
    <DataRow("list.Csv", True)>
    <DataRow("C:\S\list.xlsx", False)>
    <DataRow("C:\S\list.csv.SAVE", False)>
    <DataRow("C:\S\list", False)>
    <DataRow("", False)>
    <DataRow(Nothing, False)>
    Public Sub IsCsv_MatchesCsvExtensionCaseInsensitively(path As String, expected As Boolean)
        Assert.AreEqual(expected, SignalListFile.IsCsv(path))
    End Sub

    <TestMethod>
    <DataRow("C:\S\list.xlsx", "C:\S\list.csv")>
    <DataRow("C:\S\list.XLSX", "C:\S\list.csv")>
    <DataRow("C:\S\list", "C:\S\list.csv")>
    <DataRow("C:\v1.2\list.xlsx", "C:\v1.2\list.csv")>
    <DataRow("C:\S\list.csv", "C:\S\list.csv")>
    <DataRow("C:\S\LIST.CSV", "C:\S\LIST.CSV")>
    <DataRow("", "")>
    <DataRow(Nothing, Nothing)>
    Public Sub ToCsvPath_MapsLegacyPathsAndLeavesCsvUnchanged(path As String, expected As String)
        Assert.AreEqual(expected, SignalListFile.ToCsvPath(path))
    End Sub

    <TestMethod>
    <DataRow("C:\S\list.csv", "C:\S\list_RecordFile.csv")>
    <DataRow("C:\v1.2\list.csv", "C:\v1.2\list_RecordFile.csv")>
    <DataRow("C:\S\184_25927_MY26_ACP3.csv", "C:\S\184_25927_MY26_ACP3_RecordFile.csv")>
    <DataRow("list.csv", "list_RecordFile.csv")>
    Public Sub RecordFilePathFor_AppendsRecordFileSuffixInSameFolder(signalList As String, expected As String)
        Assert.AreEqual(expected, SignalListFile.RecordFilePathFor(signalList))
    End Sub

    <TestMethod>
    Public Sub RecordFilePathFor_NeverEqualsTheSignalList()
        Dim signalList As String = "C:\S\list.csv"
        Assert.AreNotEqual(signalList, SignalListFile.RecordFilePathFor(signalList), ignoreCase:=True)
    End Sub

    <TestMethod>
    <DataRow("list.csv", True)>
    <DataRow("LIST.CSV", True)>
    <DataRow("list_NEW.csv", True)>
    <DataRow("list.xlsx", False)>
    <DataRow("list.csv.SAVE", False)>
    <DataRow("listSAVE.csv", False)>
    <DataRow("~$list.csv", False)>
    <DataRow("list_RecordFile.csv", False)>
    <DataRow("list_recordfile.CSV", False)>
    <DataRow("", False)>
    Public Sub IsSignalListCandidate_ExcludesBackupsLockFilesRecordFilesAndNonCsv(fileName As String, expected As Boolean)
        Assert.AreEqual(expected, SignalListFile.IsSignalListCandidate(fileName))
    End Sub
End Class
