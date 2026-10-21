Imports System.Xml
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class IncaConfigurationSignalListTests

    Private Const BaseDirectory As String = "C:\App"

    Private Shared Function Load(variableFile As String) As IncaConfiguration
        Dim doc As New XmlDocument()
        doc.LoadXml(
            "<Configuration>" &
            "<INCADatabase>DB</INCADatabase>" &
            "<INCAWorkspace>WS</INCAWorkspace>" &
            "<INCAExperiment>EXP</INCAExperiment>" &
            "<INCAVariableFile>" & variableFile & "</INCAVariableFile>" &
            "<DataDictionary>dd.xml</DataDictionary>" &
            "<AlternateRecordConfig>None</AlternateRecordConfig>" &
            "<Vehicle><VehicleNumber /></Vehicle>" &
            "</Configuration>")
        Return New IncaConfiguration(doc, BaseDirectory)
    End Function

    <TestMethod>
    Public Sub CsvSignalList_IsKeptAndHasNoLegacyEntry()
        Dim config = Load("C:\S\list.csv")

        Assert.AreEqual("C:\S\list.csv", config.VariableFile)
        Assert.IsNull(config.LegacyVariableFile)
    End Sub

    <TestMethod>
    Public Sub XlsxSignalList_MapsToSiblingCsvAndRecordsLegacyEntry()
        Dim config = Load("C:\S\list.xlsx")

        Assert.AreEqual("C:\S\list.csv", config.VariableFile)
        Assert.AreEqual("C:\S\list.xlsx", config.LegacyVariableFile)
    End Sub

    <TestMethod>
    Public Sub RelativeXlsxSignalList_ResolvesUnderSignalListsThenMapsToCsv()
        Dim config = Load("list.xlsx")

        Assert.AreEqual("C:\App\SignalLists\list.csv", config.VariableFile)
        Assert.AreEqual("C:\App\SignalLists\list.xlsx", config.LegacyVariableFile)
    End Sub

    <TestMethod>
    Public Sub LegacyXlsxAndMatchingCsvConfigs_HaveSameSessionSettings()
        Assert.IsTrue(Load("C:\S\list.xlsx").HasSameSessionSettings(Load("C:\S\list.csv")))
    End Sub
End Class
