Imports System.Drawing
Imports System.Globalization
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class DisplayValueFormatterTests

    Private _savedCulture As CultureInfo

    <TestInitialize>
    Public Sub UseEnUsCulture()
        _savedCulture = CultureInfo.CurrentCulture
        CultureInfo.CurrentCulture = New CultureInfo("en-US")
    End Sub

    <TestCleanup>
    Public Sub RestoreCulture()
        CultureInfo.CurrentCulture = _savedCulture
    End Sub

    Private Shared Function NewEnum(variableName As String, deviceName As String, ParamArray values As String()) As SignalEnums
        Dim e As New SignalEnums(variableName, deviceName)
        e.Enums.AddRange(values)
        Return e
    End Function

    Private Shared Function Fmt(value As Double, format As String, Optional enums As List(Of SignalEnums) = Nothing,
                                Optional deviceName As String = "DEV", Optional variableName As String = "Var") As String
        Return DisplayValueFormatter.FormatDisplayString(value, format, deviceName, variableName, enums)
    End Function

    ' --- Boolean-style formats (including existing leading-space quirks) ---

    <TestMethod>
    Public Sub TrueFalse()
        Assert.AreEqual("0 True", Fmt(0, "TRUE/FALSE"))
        Assert.AreEqual("1 False", Fmt(1, "TRUE/FALSE"))
        Assert.AreEqual("1 False", Fmt(5, "TRUE/FALSE"))
        Assert.AreEqual("0 True", Fmt(0, "true/false"))
    End Sub

    <TestMethod>
    Public Sub FalseTrue_HasLeadingSpaceForOne()
        Assert.AreEqual("0 False", Fmt(0, "FALSE/TRUE"))
        Assert.AreEqual(" 1 True", Fmt(1, "FALSE/TRUE"))
    End Sub

    <TestMethod>
    Public Sub InvalidValid_HasLeadingSpaceForOne()
        Assert.AreEqual("0 Invalid", Fmt(0, "INVALID/VALID"))
        Assert.AreEqual(" 1 Valid", Fmt(1, "INVALID/VALID"))
    End Sub

    <TestMethod>
    Public Sub ValidInvalid()
        Assert.AreEqual("0 Valid", Fmt(0, "VALID/INVALID"))
        Assert.AreEqual("1 Invalid", Fmt(1, "VALID/INVALID"))
    End Sub

    <TestMethod>
    Public Sub BooleanFormats_UseBankersRoundingViaCInt()
        Assert.AreEqual("0 True", Fmt(0.5, "TRUE/FALSE"))
        Assert.AreEqual("1 False", Fmt(1.5, "TRUE/FALSE"))
    End Sub

    ' --- Numeric formats ---

    <TestMethod>
    Public Sub HexFormat()
        Assert.AreEqual("FF", Fmt(255, "HEX"))
        Assert.AreEqual("A", Fmt(10, "hex"))
        Assert.AreEqual("0", Fmt(0, "HEX"))
    End Sub

    <TestMethod>
    Public Sub Dec_RoundsToInteger()
        Assert.AreEqual("3", Fmt(2.6, "DEC"))
        Assert.AreEqual("-7", Fmt(-7.2, "DEC"))
    End Sub

    <TestMethod>
    Public Sub Binary_PadsToEightBits()
        Assert.AreEqual("00000101", Fmt(5, "BINARY"))
        Assert.AreEqual("100101100", Fmt(300, "BINARY"))
    End Sub

    <TestMethod>
    Public Sub OtherFormats_PassThroughToFormat()
        Assert.AreEqual("3.14", Fmt(3.14159, "0.00"))
        Assert.AreEqual("12.3", Fmt(12.345, "0.0"))
        Assert.AreEqual("3", Fmt(2.6, "0"))
    End Sub

    ' --- ENUM ---

    <TestMethod>
    Public Sub Enum_Match_PrefixesNumericValue()
        Dim enums As New List(Of SignalEnums) From {NewEnum("Gear", "", "Park", "Reverse")}
        Assert.AreEqual("1 Reverse", Fmt(1, "ENUM", enums, variableName:="XYZ_Gear_abc"))
    End Sub

    <TestMethod>
    Public Sub Enum_ValueStartingWithDigit_IsNotPrefixed()
        Dim enums As New List(Of SignalEnums) From {NewEnum("Gear", "", "Park", "Reverse", "3rd")}
        Assert.AreEqual("3rd", Fmt(2, "ENUM", enums, variableName:="Gear"))
    End Sub

    <TestMethod>
    Public Sub Enum_EmptyValue_IsPrefixedWithTrailingSpace()
        Dim enums As New List(Of SignalEnums) From {NewEnum("Gear", "", "Park", "")}
        Assert.AreEqual("1 ", Fmt(1, "ENUM", enums, variableName:="Gear"))
    End Sub

    <TestMethod>
    Public Sub Enum_DeviceMustMatchUnlessBlank()
        Dim enums As New List(Of SignalEnums) From {NewEnum("Gear", "OTHER", "Park", "Reverse")}
        Assert.AreEqual("1", Fmt(1, "ENUM", enums, deviceName:="DEV", variableName:="Gear"))
        Assert.AreEqual("1 Reverse", Fmt(1, "ENUM", enums, deviceName:="OTHER", variableName:="Gear"))
    End Sub

    <TestMethod>
    Public Sub Enum_FirstMatchingEntryWins()
        Dim enums As New List(Of SignalEnums) From {
            NewEnum("Gear", "OTHER", "P0", "R0"),
            NewEnum("Gear", "DEV", "P1", "R1"),
            NewEnum("Gear", "", "P2", "R2")}
        Assert.AreEqual("1 R1", Fmt(1, "ENUM", enums, deviceName:="DEV", variableName:="Gear"))
    End Sub

    <TestMethod>
    Public Sub Enum_NoMatchOrOutOfRangeOrNothingList_ShowsNumber()
        Dim enums As New List(Of SignalEnums) From {NewEnum("Gear", "", "Park", "Reverse")}
        Assert.AreEqual("5", Fmt(5, "ENUM", enums, variableName:="Gear"))
        Assert.AreEqual("1", Fmt(1, "ENUM", enums, variableName:="Speed"))
        Assert.AreEqual("1", Fmt(1, "ENUM", Nothing, variableName:="Gear"))
        Assert.AreEqual("1", Fmt(1, "ENUM", New List(Of SignalEnums)(), variableName:="Gear"))
    End Sub

    <TestMethod>
    Public Sub Enum_SdficVariables_OnlyUseDtcList()
        Dim enums As New List(Of SignalEnums) From {
            NewEnum("SDFIC", "", "WrongA", "WrongB"),
            NewEnum("VaIDRR_e_DTC_List", "", "NoDTC", "P0100")}
        Assert.AreEqual("1 P0100", Fmt(1, "ENUM", enums, variableName:="xx_sdfic_01"))
    End Sub

    <TestMethod>
    Public Sub GetEnumDescription_IndexUsesBankersRounding()
        Dim enums As New List(Of SignalEnums) From {NewEnum("Gear", "", "A", "B", "C", "D")}
        Assert.AreEqual("C", DisplayValueFormatter.GetEnumDescription(enums, 2.5, "Gear", "DEV"))
        Assert.AreEqual("C", DisplayValueFormatter.GetEnumDescription(enums, 1.5, "Gear", "DEV"))
        Assert.AreEqual("4", DisplayValueFormatter.GetEnumDescription(enums, 3.5, "Gear", "DEV"))
    End Sub

    ' --- GetColorName ---

    <TestMethod>
    Public Sub GetColorName_KnownColors()
        Assert.AreEqual("RED", DisplayValueFormatter.GetColorName(Color.Red))
        Assert.AreEqual("GREEN", DisplayValueFormatter.GetColorName(Color.Green))
        Assert.AreEqual("GRAY", DisplayValueFormatter.GetColorName(Color.Gray))
        Assert.AreEqual("YELLOW", DisplayValueFormatter.GetColorName(Color.Yellow))
        Assert.AreEqual("BLUE", DisplayValueFormatter.GetColorName(Color.Blue))
    End Sub

    <TestMethod>
    Public Sub GetColorName_ArgbColor_IsNotTreatedAsNamedColor()
        Assert.AreEqual("FFFF0000", DisplayValueFormatter.GetColorName(Color.FromArgb(255, 0, 0)))
    End Sub

End Class
