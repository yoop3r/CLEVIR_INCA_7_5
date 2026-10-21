Imports System.Globalization
Imports System.Xml
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class LidarExtrinsicParserTests

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

    Private Shared Function Node(xml As String) As XmlNode
        Dim doc As New XmlDocument()
        doc.LoadXml(xml)
        Return doc.DocumentElement
    End Function

    <TestMethod>
    Public Sub Parse_FullNode_PopulatesEveryField()
        Dim record = LidarExtrinsicParser.Parse(Node(
            "<Extrinsic IsCalibrated=""True"">" &
            "<CalibrationId>CAL-1</CalibrationId>" &
            "<DatePerformed>2025-03-04T05:06:07</DatePerformed>" &
            "<Method>Target</Method>" &
            "<Translation>1 2.5 -3</Translation>" &
            "<RotationQuaternion>0 0 0.7071 0.7071</RotationQuaternion>" &
            "<ResidualError>0.012</ResidualError>" &
            "</Extrinsic>"))

        Assert.IsTrue(record.IsCalibrated)
        Assert.AreEqual("CAL-1", record.CalibrationId)
        Assert.AreEqual(New DateTime(2025, 3, 4, 5, 6, 7), record.DatePerformed)
        Assert.AreEqual("Target", record.Method)
        CollectionAssert.AreEqual(New Double() {1, 2.5, -3}, record.TranslationMeters)
        CollectionAssert.AreEqual(New Double() {0, 0, 0.7071, 0.7071}, record.RotationQuaternion)
        Assert.AreEqual(0.012, record.ResidualError)
    End Sub

    <TestMethod>
    Public Sub Parse_EmptyNode_YieldsUncalibratedIdentity()
        Dim record = LidarExtrinsicParser.Parse(Node("<Extrinsic/>"))

        Assert.IsFalse(record.IsCalibrated)
        Assert.IsNull(record.CalibrationId)
        Assert.IsNull(record.Method)
        Assert.IsNull(record.DatePerformed)
        Assert.IsNull(record.ResidualError)
        CollectionAssert.AreEqual(New Double() {0, 0, 0}, record.TranslationMeters)
        CollectionAssert.AreEqual(New Double() {0, 0, 0, 1}, record.RotationQuaternion)
    End Sub

    <TestMethod>
    Public Sub Parse_MalformedValues_FallBackIndividually()
        Dim record = LidarExtrinsicParser.Parse(Node(
            "<Extrinsic IsCalibrated="""">" &
            "<DatePerformed>not a date</DatePerformed>" &
            "<Translation>1 2</Translation>" &
            "<RotationQuaternion>0 0 x 1</RotationQuaternion>" &
            "<ResidualError>n/a</ResidualError>" &
            "</Extrinsic>"))

        Assert.IsFalse(record.IsCalibrated)
        Assert.IsNull(record.DatePerformed)
        Assert.IsNull(record.ResidualError)
        CollectionAssert.AreEqual(New Double() {0, 0, 0}, record.TranslationMeters)
        CollectionAssert.AreEqual(New Double() {0, 0, 0, 1}, record.RotationQuaternion)
    End Sub

    <TestMethod>
    Public Sub Parse_IsCalibratedIsCaseInsensitive()
        Assert.IsTrue(LidarExtrinsicParser.Parse(Node("<Extrinsic IsCalibrated=""true""/>")).IsCalibrated)
        Assert.IsFalse(LidarExtrinsicParser.Parse(Node("<Extrinsic IsCalibrated=""FALSE""/>")).IsCalibrated)
    End Sub

    <TestMethod>
    Public Sub Parse_NonBooleanIsCalibrated_Throws()
        Assert.ThrowsExactly(Of FormatException)(
            Sub() LidarExtrinsicParser.Parse(Node("<Extrinsic IsCalibrated=""yes""/>")))
    End Sub

End Class
