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
    Public Sub SetVariableFile_ReplacesExistingValueOnly()
        Dim doc As New XmlDocument()
        doc.LoadXml("<Configuration><INCAWorkspace>WS</INCAWorkspace><INCAVariableFile>C:\S\old.csv</INCAVariableFile></Configuration>")

        IncaConfiguration.SetVariableFile(doc, "C:\S\new_bak.csv")

        Assert.AreEqual(1, doc.DocumentElement.SelectNodes("INCAVariableFile").Count)
        Assert.AreEqual("C:\S\new_bak.csv", doc.DocumentElement.SelectSingleNode("INCAVariableFile").InnerText)
        Assert.AreEqual("WS", doc.DocumentElement.SelectSingleNode("INCAWorkspace").InnerText)
    End Sub

    <TestMethod>
    Public Sub SetVariableFile_CreatesMissingElement()
        Dim doc As New XmlDocument()
        doc.LoadXml("<Configuration><INCAWorkspace>WS</INCAWorkspace></Configuration>")

        IncaConfiguration.SetVariableFile(doc, "C:\S\list.csv")

        Assert.AreEqual("C:\S\list.csv", doc.DocumentElement.SelectSingleNode("INCAVariableFile").InnerText)
    End Sub

    <TestMethod>
    Public Sub LegacyXlsxAndMatchingCsvConfigs_HaveSameSessionSettings()
        Assert.IsTrue(Load("C:\S\list.xlsx").HasSameSessionSettings(Load("C:\S\list.csv")))
    End Sub
End Class
