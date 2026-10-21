Imports System.IO
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class SignalListCsvReaderTests

    Private _tempFile As String

    <TestInitialize>
    Public Sub CreateTempPath()
        _tempFile = Path.Combine(Path.GetTempPath(), "SignalListCsvReaderTests_" & Guid.NewGuid().ToString("N") & ".csv")
    End Sub

    <TestCleanup>
    Public Sub DeleteTempFile()
        If File.Exists(_tempFile) Then File.Delete(_tempFile)
    End Sub

    Private Function ReadText(content As String) As Object(,)
        File.WriteAllText(_tempFile, content)
        Return SignalListCsvReader.Read(_tempFile)
    End Function

    <TestMethod>
    Public Sub Read_CommaDelimited_ReturnsOneBasedArrayPaddedToMinimumColumns()
        Dim data = ReadText("Window,Device,Signal" & vbCrLf & "Main,ECU1,VehSpd" & vbCrLf)

        Assert.AreEqual(2, data.GetUpperBound(0))
        Assert.AreEqual(SignalListCsvReader.MinimumColumns, data.GetUpperBound(1))
        Assert.AreEqual("Window", data(1, 1))
        Assert.AreEqual("Signal", data(1, 3))
        Assert.AreEqual("ECU1", data(2, 2))
        Assert.AreEqual("VehSpd", data(2, 3))
        Assert.AreEqual(String.Empty, data(2, 4))
        Assert.AreEqual(String.Empty, data(2, SignalListCsvReader.MinimumColumns))
        Assert.IsNull(data(0, 0))
        Assert.IsNull(data(1, 0))
        Assert.IsNull(data(0, 1))
    End Sub

    <TestMethod>
    Public Sub Read_TabDelimited_SplitsOnTabsNotCommas()
        Dim data = ReadText("A" & vbTab & "B,1" & vbTab & "C" & vbCrLf & "x" & vbTab & "y" & vbTab & "z")

        Assert.AreEqual("B,1", data(1, 2))
        Assert.AreEqual("z", data(2, 3))
    End Sub

    <TestMethod>
    Public Sub Read_SemicolonDelimited_SplitsOnSemicolons()
        Dim data = ReadText("A;B;C" & vbCrLf & "1,5;2;3")

        Assert.AreEqual("1,5", data(2, 1))
        Assert.AreEqual("3", data(2, 3))
    End Sub

    <TestMethod>
    Public Sub Read_QuotedFields_KeepEmbeddedDelimitersAndUnescapeDoubledQuotes()
        Dim data = ReadText("A,B,C" & vbCrLf & """x, y"",""""""0.000"""""",z")

        Assert.AreEqual("x, y", data(2, 1))
        Assert.AreEqual("""0.000""", data(2, 2))
        Assert.AreEqual("z", data(2, 3))
    End Sub

    <TestMethod>
    Public Sub Read_TrimsFieldsAndSkipsBlankAndAllEmptyRows()
        Dim data = ReadText(vbCrLf & "  A , B " & vbCrLf & vbCrLf & ",," & vbCrLf & "   " & vbCrLf & " 1 ,2" & vbCrLf)

        Assert.AreEqual(2, data.GetUpperBound(0))
        Assert.AreEqual("A", data(1, 1))
        Assert.AreEqual("B", data(1, 2))
        Assert.AreEqual("1", data(2, 1))
    End Sub

    <TestMethod>
    Public Sub Read_RowWiderThanMinimum_ExtendsColumnsAndPadsShorterRows()
        Dim wide As String = String.Join(",", Enumerable.Range(1, 25).Select(Function(i) "c" & i))
        Dim data = ReadText("h1,h2" & vbCrLf & wide)

        Assert.AreEqual(25, data.GetUpperBound(1))
        Assert.AreEqual("c25", data(2, 25))
        Assert.AreEqual(String.Empty, data(1, 25))
    End Sub

    <TestMethod>
    Public Sub Read_EmptyFile_ThrowsInvalidDataException()
        Dim ex = Assert.ThrowsExactly(Of InvalidDataException)(Sub() ReadText(""))
        Assert.AreEqual("CSV file is empty", ex.Message)
    End Sub

    <TestMethod>
    Public Sub Read_WhitespaceOnlyFile_ThrowsInvalidDataException()
        Assert.ThrowsExactly(Of InvalidDataException)(Sub() ReadText("   " & vbCrLf & vbTab & vbCrLf))
    End Sub

    <TestMethod>
    Public Sub Read_OnlyDelimiterRows_ThrowsNoDataRows()
        Dim ex = Assert.ThrowsExactly(Of InvalidDataException)(Sub() ReadText(",,," & vbCrLf & ",," & vbCrLf))
        Assert.AreEqual("CSV contained no data rows", ex.Message)
    End Sub

    <TestMethod>
    Public Sub Read_MissingFile_ThrowsFileNotFoundException()
        Assert.ThrowsExactly(Of FileNotFoundException)(Sub() SignalListCsvReader.Read(_tempFile))
    End Sub

    <TestMethod>
    <DataRow("a,b,c", ",")>
    <DataRow("a;b;c", ";")>
    <DataRow("a" & vbTab & "b", vbTab)>
    <DataRow("abc", vbTab)>
    <DataRow("a;b,c", ";")>
    <DataRow("a" & vbTab & "b;c", vbTab)>
    <DataRow("a,b,c;d", ",")>
    Public Sub DetectDelimiter_PicksMostFrequentWithTabThenSemicolonTieBreak(line As String, expected As String)
        Assert.AreEqual(expected, SignalListCsvReader.DetectDelimiter(line))
    End Sub
End Class
