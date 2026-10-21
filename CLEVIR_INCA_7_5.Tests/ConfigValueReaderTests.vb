Imports System.Collections.Generic
Imports System.Globalization
Imports System.Xml
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ConfigValueReaderTests

    Private _savedCulture As CultureInfo
    Private _log As List(Of String)

    <TestInitialize>
    Public Sub Setup()
        _savedCulture = CultureInfo.CurrentCulture
        CultureInfo.CurrentCulture = New CultureInfo("en-US")
        _log = New List(Of String)()
    End Sub

    <TestCleanup>
    Public Sub RestoreCulture()
        CultureInfo.CurrentCulture = _savedCulture
    End Sub

    Private Shared Function Root(xml As String) As XmlNode
        Dim doc As New XmlDocument()
        doc.LoadXml(xml)
        Return doc.DocumentElement
    End Function

    ' --- ReadBoolean ---

    <TestMethod>
    Public Sub ReadBoolean_Present_ReturnsValueAndLogsSetTo()
        Dim value = ConfigValueReader.ReadBoolean(Root("<c><Flag>False</Flag></c>"), "Flag", True, AddressOf _log.Add)

        Assert.IsFalse(value)
        CollectionAssert.AreEqual(New String() {"Flag set to: False"}, _log)
    End Sub

    <TestMethod>
    Public Sub ReadBoolean_Missing_ReturnsDefaultAndLogsDefault()
        Dim value = ConfigValueReader.ReadBoolean(Root("<c/>"), "Flag", True, AddressOf _log.Add)

        Assert.IsTrue(value)
        CollectionAssert.AreEqual(New String() {"Flag not found, using default: True"}, _log)
    End Sub

    <TestMethod>
    Public Sub ReadBoolean_UsesVbCBoolSemantics()
        Assert.IsTrue(ConfigValueReader.ReadBoolean(Root("<c><F>true</F></c>"), "F", False, AddressOf _log.Add))
        Assert.IsTrue(ConfigValueReader.ReadBoolean(Root("<c><F>1</F></c>"), "F", False, AddressOf _log.Add))
        Assert.IsFalse(ConfigValueReader.ReadBoolean(Root("<c><F>0</F></c>"), "F", True, AddressOf _log.Add))
    End Sub

    <TestMethod>
    Public Sub ReadBoolean_Malformed_ThrowsWithoutLogging()
        Assert.ThrowsExactly(Of InvalidCastException)(
            Sub() ConfigValueReader.ReadBoolean(Root("<c><F>yes</F></c>"), "F", False, AddressOf _log.Add))
        Assert.AreEqual(0, _log.Count)
    End Sub

    ' --- ReadString ---

    <TestMethod>
    Public Sub ReadString_Present_TrimsAndLogs()
        Dim value = ConfigValueReader.ReadString(Root("<c><Path>  C:\HB  </Path></c>"), "Path", "X", AddressOf _log.Add)

        Assert.AreEqual("C:\HB", value)
        CollectionAssert.AreEqual(New String() {"Path set to: C:\HB"}, _log)
    End Sub

    <TestMethod>
    Public Sub ReadString_EmptyElement_ReturnsEmptyNotDefault()
        Dim value = ConfigValueReader.ReadString(Root("<c><Path/></c>"), "Path", "X", AddressOf _log.Add)

        Assert.AreEqual("", value)
        CollectionAssert.AreEqual(New String() {"Path set to: "}, _log)
    End Sub

    <TestMethod>
    Public Sub ReadString_Missing_ReturnsDefaultAndLogsDefault()
        Dim value = ConfigValueReader.ReadString(Root("<c/>"), "Path", "C:\HB", AddressOf _log.Add)

        Assert.AreEqual("C:\HB", value)
        CollectionAssert.AreEqual(New String() {"Path not found, using default: C:\HB"}, _log)
    End Sub

    <TestMethod>
    Public Sub ReadString_ReadsOnlyDirectChildOfParent()
        Dim value = ConfigValueReader.ReadString(Root("<c><Compression><Level>9</Level></Compression></c>"), "Level", "1", AddressOf _log.Add)

        Assert.AreEqual("1", value)
    End Sub

    ' --- ReadInteger ---

    <TestMethod>
    Public Sub ReadInteger_Present_ReturnsValueAndLogs()
        Dim value = ConfigValueReader.ReadInteger(Root("<c><Port>7372</Port></c>"), "Port", 1, AddressOf _log.Add)

        Assert.AreEqual(7372, value)
        CollectionAssert.AreEqual(New String() {"Port set to: 7372"}, _log)
    End Sub

    <TestMethod>
    Public Sub ReadInteger_Missing_ReturnsDefaultAndLogsDefault()
        Dim value = ConfigValueReader.ReadInteger(Root("<c/>"), "Port", 3000, AddressOf _log.Add)

        Assert.AreEqual(3000, value)
        CollectionAssert.AreEqual(New String() {"Port not found, using default: 3000"}, _log)
    End Sub

    <TestMethod>
    Public Sub ReadInteger_UsesVbCIntBankersRounding()
        Assert.AreEqual(2, ConfigValueReader.ReadInteger(Root("<c><N>2.5</N></c>"), "N", 0, AddressOf _log.Add))
        Assert.AreEqual(4, ConfigValueReader.ReadInteger(Root("<c><N>3.5</N></c>"), "N", 0, AddressOf _log.Add))
        Assert.AreEqual(42, ConfigValueReader.ReadInteger(Root("<c><N> 42 </N></c>"), "N", 0, AddressOf _log.Add))
    End Sub

    <TestMethod>
    Public Sub ReadInteger_Malformed_Throws()
        Assert.ThrowsExactly(Of InvalidCastException)(
            Sub() ConfigValueReader.ReadInteger(Root("<c><N>abc</N></c>"), "N", 0, AddressOf _log.Add))
    End Sub

End Class
