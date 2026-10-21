Imports System.Globalization
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ResidentClientTextTests

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

    ' --- IsVideoTimecode ---

    <TestMethod>
    Public Sub IsVideoTimecode_Nothing_IsFalse()
        Assert.IsFalse(ResidentClientText.IsVideoTimecode(Nothing))
    End Sub

    <TestMethod>
    Public Sub IsVideoTimecode_IgnoresCaseWhitespaceAndQuotes()
        Assert.IsTrue(ResidentClientText.IsVideoTimecode("VIDEO_CAMERA_TIMECODE"))
        Assert.IsTrue(ResidentClientText.IsVideoTimecode("  ""video_camera_timecode""  "))
    End Sub

    <TestMethod>
    Public Sub IsVideoTimecode_OtherNames_AreFalse()
        Assert.IsFalse(ResidentClientText.IsVideoTimecode("VIDEO_CAMERA_TIMECODE_2"))
        Assert.IsFalse(ResidentClientText.IsVideoTimecode(""))
    End Sub

    ' --- CleanUpRasterName ---

    <TestMethod>
    Public Sub CleanUpRasterName_TrailingUnderscoreF_BecomesDashLowerF()
        Assert.AreEqual("10ms-f", ResidentClientText.CleanUpRasterName("10ms_f"))
        Assert.AreEqual("10ms-f", ResidentClientText.CleanUpRasterName("10ms_F"))
        Assert.AreEqual("-f", ResidentClientText.CleanUpRasterName("_f"))
    End Sub

    <TestMethod>
    Public Sub CleanUpRasterName_OtherNames_Unchanged()
        Assert.AreEqual("100ms", ResidentClientText.CleanUpRasterName("100ms"))
        Assert.AreEqual("a_f_b", ResidentClientText.CleanUpRasterName("a_f_b"))
    End Sub

    <TestMethod>
    Public Sub CleanUpRasterName_Nothing_Throws()
        Assert.ThrowsExactly(Of NullReferenceException)(Sub() ResidentClientText.CleanUpRasterName(Nothing))
    End Sub

    ' --- ParseDoubleVector ---

    <TestMethod>
    Public Sub ParseDoubleVector_SpaceOrCommaSeparated_Parses()
        Dim fallback As Double() = {9, 9, 9}
        CollectionAssert.AreEqual(New Double() {1, 2, 3}, ResidentClientText.ParseDoubleVector("1 2 3", 3, fallback))
        CollectionAssert.AreEqual(New Double() {1.5, -2, 300}, ResidentClientText.ParseDoubleVector(" 1.5,-2 , 3e2 ", 3, fallback))
    End Sub

    <TestMethod>
    Public Sub ParseDoubleVector_InvalidInput_ReturnsSameFallbackInstance()
        Dim fallback As Double() = {9, 9, 9}
        Assert.AreSame(fallback, ResidentClientText.ParseDoubleVector(Nothing, 3, fallback))
        Assert.AreSame(fallback, ResidentClientText.ParseDoubleVector("   ", 3, fallback))
        Assert.AreSame(fallback, ResidentClientText.ParseDoubleVector("1 2", 3, fallback))
        Assert.AreSame(fallback, ResidentClientText.ParseDoubleVector("1 2 3 4", 3, fallback))
        Assert.AreSame(fallback, ResidentClientText.ParseDoubleVector("1 x 3", 3, fallback))
    End Sub

    ' --- NormalizeFilename ---

    <TestMethod>
    Public Sub NormalizeFilename_NothingOrEmpty_ReturnsEmpty()
        Assert.AreEqual(String.Empty, ResidentClientText.NormalizeFilename(Nothing))
        Assert.AreEqual(String.Empty, ResidentClientText.NormalizeFilename(""))
    End Sub

    <TestMethod>
    Public Sub NormalizeFilename_RemovesInvisibleCharactersAndTrims()
        Dim input As String = "  a" & ChrW(&H200B) & "b" & ChrW(&HFEFF) & "c" & ChrW(&H200C) & ChrW(&H200D) & vbNullChar & ".mf4  "
        Assert.AreEqual("abc.mf4", ResidentClientText.NormalizeFilename(input))
    End Sub

    <TestMethod>
    Public Sub NormalizeFilename_ComposesToFormC()
        Assert.AreEqual(ChrW(&HE9) & ".mf4", ResidentClientText.NormalizeFilename("e" & ChrW(&H301) & ".mf4"))
    End Sub

    <TestMethod>
    Public Sub NormalizeFilename_TrimsBeforeRemovingZeroWidth_SoInnerSpaceRemains()
        ' Characterizes existing ordering: zero-width chars are not whitespace, so a space behind one survives.
        Assert.AreEqual(" a.mf4", ResidentClientText.NormalizeFilename(ChrW(&H200B) & " a.mf4"))
    End Sub

End Class
