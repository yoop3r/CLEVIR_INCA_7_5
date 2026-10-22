Imports System.IO
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class SignalListCsvWriterTests

    <TestMethod>
    <DataRow("""""0""""", """""""""""0""""""""""")>
    <DataRow("""0.000""", """""""0.000""""""")>
    <DataRow("x, y", """x, y""")>
    <DataRow("a" & vbCrLf & "b", """a" & vbCrLf & "b""")>
    Public Sub FormatField_ValueNeedingQuotes_IsQuotedWithDoubledQuotes(value As String, expected As String)
        Assert.AreEqual(expected, SignalListCsvWriter.FormatField(value))
    End Sub

    <TestMethod>
    <DataRow("ENUM")>
    <DataRow("W284 H208")>
    <DataRow("GO/NOGO")>
    <DataRow("")>
    Public Sub FormatField_PlainValue_IsUnchanged(value As String)
        Assert.AreEqual(value, SignalListCsvWriter.FormatField(value))
    End Sub

    <TestMethod>
    Public Sub FormatField_NothingAndDBNull_AreEmpty()
        Assert.AreEqual(String.Empty, SignalListCsvWriter.FormatField(Nothing))
        Assert.AreEqual(String.Empty, SignalListCsvWriter.FormatField(DBNull.Value))
    End Sub

    <TestMethod>
    Public Sub FormatField_NonString_UsesStringConversion()
        Assert.AreEqual("7", SignalListCsvWriter.FormatField(7))
        Assert.AreEqual("-10000000", SignalListCsvWriter.FormatField(-10000000))
    End Sub

    <TestMethod>
    Public Sub FormattedLine_RoundTripsThroughReader()
        Dim values As Object() = {"VeTOFR_Cnt_40msRollCt", "40msRollCt", "W284 H208", "GO/NOGO", """""0""""", """0.000""", "ENUM", "x, y", 3, ""}
        Dim line As String = String.Join(",", values.Select(Function(v) SignalListCsvWriter.FormatField(v)))
        Dim tempFile As String = Path.Combine(Path.GetTempPath(), "SignalListCsvWriterTests_" & Guid.NewGuid().ToString("N") & ".csv")

        Try
            File.WriteAllText(tempFile, "VariableName,DisplayName,DisplayFormat" & vbCrLf & line & vbCrLf)
            Dim data = SignalListCsvReader.Read(tempFile)

            For i As Integer = 0 To values.Length - 1
                Assert.AreEqual(CStr(values(i)), data(2, i + 1), $"column {i + 1}")
            Next
            StringAssert.Contains(line, "," & """""""""""0""""""""""" & ",")
        Finally
            If File.Exists(tempFile) Then File.Delete(tempFile)
        End Try
    End Sub
End Class
