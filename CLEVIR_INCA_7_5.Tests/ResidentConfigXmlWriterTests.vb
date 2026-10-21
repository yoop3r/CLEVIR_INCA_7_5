Imports System.Collections.Generic
Imports System.Globalization
Imports System.Xml
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ResidentConfigXmlWriterTests

    Private Const Banner As String = "<!-- ================================================================ -->"

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

    Private Shared Function SampleSnapshot() As ResidentConfigSnapshot
        Return New ResidentConfigSnapshot With {
            .INCADatabase = "DB1",
            .INCAWorkspace = "WS1",
            .InitialINCAExperiment = "EXP_INIT",
            .INCAVariableFile = "C:\Signals\list.xlsx",
            .ETAS_USER_PATH = "C:\ETAS",
            .SignalRegistrationMode = "FULL",
            .RecordWAVTime = "30",
            .RecordFileDurationMinutes = 5L,
            .MuteVoiceRecordingMessages = True,
            .AudioToTextConversion = False,
            .AlternateRecordEnabled = True,
            .AlternateRecordConfig = "VehicleSpy",
            .EnableAltRecReStartAfterRecordStop = False,
            .BaseDataCollectionPath = "D:\HB",
            .NetworkDriveLetter = "Q:",
            .NetworkDriveMapping = "\\server\share",
            .CSVScriptsPath = "C:\CSVScripts",
            .SevenZipPath = "C:\Program Files\7-Zip\",
            .CompressMF4 = True,
            .CompressPCAP = False,
            .CompressASC = True,
            .CompressVSB = False,
            .DeleteAfterCompression = False,
            .CompressionLevel = 3,
            .ZipCompressionMaxRetries = 7,
            .ZipCompressionRetryDelay = 11,
            .ZipFileLockTimeout = 31,
            .NetworkAdapterDescription = "Intel(R) Ethernet",
            .OxtsEnabled = True,
            .OxtsNcomIpAddress = "10.5.55.200",
            .OxtsNcomPort = 3000,
            .OxtsGpsLockTimeout = 30000,
            .OxtsWaitForLockOnStart = True,
            .TimeSyncEnabled = True,
            .TimeSyncProviderType = "TIMEMACHINE",
            .TimeMachineEnabled = True,
            .TimeMachineIpAddress = "10.0.0.5",
            .TimeMachinePort = 7372,
            .TimeMachinePollMs = 1000,
            .TimeMachinePtpAssumeLocked = False,
            .LidarCaptureEnabled = True,
            .LidarDevices = New List(Of LidarDevice) From {
                New LidarDevice("{A}", "10.5.55.14", 2368US, 8308US, "LiDAR1") With {.Enabled = True},
                New LidarDevice("{B}", "10.5.55.15", 2369US, 8309US, "LiDAR2") With {.Enabled = False}
            },
            .APICommErrorMsgDelayTime = "90"
        }
    End Function

    Private Shared Function L(depth As Integer, text As String) As String
        Return New String(ControlChars.Tab, depth) & text
    End Function

    Private Shared Function Section(title As String) As String()
        Return {L(1, Banner), L(1, $"<!-- {title} -->"), L(1, Banner)}
    End Function

    <TestMethod>
    Public Sub BuildRuntimeSettingsXml_MatchesGoldenOutput()
        Dim expected As New List(Of String) From {
            "<?xml version=""1.0"" encoding=""utf-16""?>",
            "<Configuration>"
        }
        expected.AddRange(Section("INCA Configuration"))
        expected.AddRange({
            L(1, "<INCADatabase>DB1</INCADatabase>"),
            L(1, "<INCAWorkspace>WS1</INCAWorkspace>"),
            L(1, "<INCAExperiment>EXP_INIT</INCAExperiment>"),
            L(1, "<INCAVariableFile>C:\Signals\list.xlsx</INCAVariableFile>"),
            L(1, "<ETAS_USER_PATH>C:\ETAS</ETAS_USER_PATH>"),
            L(1, "<SignalRegistrationMode>FULL</SignalRegistrationMode>")})
        expected.AddRange(Section("Recording Configuration"))
        expected.AddRange({
            L(1, "<RecordWAVTime>30</RecordWAVTime>"),
            L(1, "<RecordFileDurationMinutes>5</RecordFileDurationMinutes>"),
            L(1, "<MuteVoiceRecordingMessages>True</MuteVoiceRecordingMessages>"),
            L(1, "<AudioToTextConversion>False</AudioToTextConversion>"),
            L(1, Banner),
            L(1, "<!-- Alternate Recording (CANalyzer/VehicleSpy) -->"),
            L(1, "<!-- Valid AlternateRecordConfig values: VehicleSpy, CANalyzer, None -->"),
            L(1, Banner),
            L(1, "<AlternateRecordEnabled>True</AlternateRecordEnabled>"),
            L(1, "<AlternateRecordConfig>VehicleSpy</AlternateRecordConfig>"),
            L(1, "<EnableAltRecReStartAfterRecordStop>False</EnableAltRecReStartAfterRecordStop>")})
        expected.AddRange(Section("Data Storage & Network"))
        expected.AddRange({
            L(1, "<BaseDataCollectionPath>D:\HB</BaseDataCollectionPath>"),
            L(1, "<NetworkDriveLetter>Q:</NetworkDriveLetter>"),
            L(1, "<NetworkDriveMapping>\\server\share</NetworkDriveMapping>")})
        expected.AddRange(Section("Local Tool & Script Paths"))
        expected.AddRange({
            L(1, "<CSVScriptsPath>C:\CSVScripts</CSVScriptsPath>"),
            L(1, "<SevenZipPath>C:\Program Files\7-Zip\</SevenZipPath>")})
        expected.AddRange(Section("Compression Configuration"))
        expected.AddRange({
            L(1, "<Compression>"),
            L(2, "<!-- File type compression switches -->"),
            L(2, "<CompressMF4>True</CompressMF4>"),
            L(2, "<CompressPCAP>False</CompressPCAP>"),
            L(2, "<CompressASC>True</CompressASC>"),
            L(2, "<CompressVSB>False</CompressVSB>"),
            L(2, "<!-- Deletion policy (SAFE DEFAULT = False) -->"),
            L(2, "<DeleteAfterCompression>False</DeleteAfterCompression>"),
            L(2, "<!-- Compression parameters -->"),
            L(2, "<CompressionLevel>3</CompressionLevel>"),
            L(2, "<!-- 1=Fastest, 9=Best -->"),
            L(2, "<MaxRetries>7</MaxRetries>"),
            L(2, "<RetryDelaySeconds>11</RetryDelaySeconds>"),
            L(2, "<!-- Lock detection -->"),
            L(2, "<FileLockTimeoutSeconds>31</FileLockTimeoutSeconds>"),
            L(1, "</Compression>")})
        expected.AddRange(Section("Hardware Configuration"))
        expected.Add(L(1, "<NetworkAdapterDescription>Intel(R) Ethernet</NetworkAdapterDescription>"))
        expected.AddRange(Section("OXTS GPS/INS Configuration"))
        expected.AddRange({
            L(1, "<OxtsConfiguration>"),
            L(2, "<!-- Enable/disable OXTS INS integration -->"),
            L(2, "<OxtsEnabled>True</OxtsEnabled>"),
            L(2, "<!-- NCOM UDP listener settings -->"),
            L(2, "<NcomIpAddress>10.5.55.200</NcomIpAddress>"),
            L(2, "<NcomPort>3000</NcomPort>"),
            L(2, "<!-- GPS lock behavior -->"),
            L(2, "<GpsLockTimeout>30000</GpsLockTimeout>"),
            L(2, "<WaitForLockOnStart>True</WaitForLockOnStart>"),
            L(1, "</OxtsConfiguration>"),
            L(1, "<!-- Time Sync provider selection -->"),
            L(1, "<TimeSyncConfiguration>"),
            L(2, "<EnableTimeSync>True</EnableTimeSync>"),
            L(2, "<Provider>TIMEMACHINE</Provider>"),
            L(1, "</TimeSyncConfiguration>"),
            L(1, "<!-- TimeMachine Locator protocol settings -->"),
            L(1, "<TimeMachineConfiguration>"),
            L(2, "<Enabled>True</Enabled>"),
            L(2, "<DeviceIp>10.0.0.5</DeviceIp>"),
            L(2, "<Port>7372</Port>"),
            L(2, "<PollMs>1000</PollMs>"),
            L(2, "<PtpAssumeLocked>False</PtpAssumeLocked>"),
            L(1, "</TimeMachineConfiguration>")})
        expected.AddRange(Section("LiDAR Configuration (Legacy + Multi-Device)"))
        expected.AddRange({
            L(1, "<LidarCaptureEnabled>True</LidarCaptureEnabled>"),
            L(1, "<!-- Legacy single-device (backward compatibility) -->"),
            L(1, "<LidarAdapterGuid>{A}</LidarAdapterGuid>"),
            L(1, "<LidarIpAddress>10.5.55.14</LidarIpAddress>"),
            L(1, "<LidarDataPort>2368</LidarDataPort>"),
            L(1, "<LidarImuPort>8308</LidarImuPort>"),
            L(1, "<!-- Multi-device configuration -->"),
            L(1, "<LidarDevices>"),
            L(2, "<Lidar id=""1"" enabled=""True"">"),
            L(3, "<AdapterGuid>{A}</AdapterGuid>"),
            L(3, "<IpAddress>10.5.55.14</IpAddress>"),
            L(3, "<DataPort>2368</DataPort>"),
            L(3, "<ImuPort>8308</ImuPort>"),
            L(2, "</Lidar>"),
            L(2, "<Lidar id=""2"" enabled=""False"">"),
            L(3, "<AdapterGuid>{B}</AdapterGuid>"),
            L(3, "<IpAddress>10.5.55.15</IpAddress>"),
            L(3, "<DataPort>2369</DataPort>"),
            L(3, "<ImuPort>8309</ImuPort>"),
            L(2, "</Lidar>"),
            L(1, "</LidarDevices>")})
        expected.AddRange(Section("Application Configuration"))
        expected.AddRange({
            L(1, "<APICommErrorMsgDelayTime>90</APICommErrorMsgDelayTime>"),
            "</Configuration>"})

        Dim actual As String = ResidentConfigXmlWriter.BuildRuntimeSettingsXml(SampleSnapshot())

        Assert.AreEqual(String.Join(Environment.NewLine, expected), actual)
    End Sub

    <TestMethod>
    Public Sub BuildRuntimeSettingsXml_NoLidarDevices_OmitsLegacyTagsAndWritesEmptyList()
        Dim snapshot = SampleSnapshot()
        snapshot.LidarDevices = New List(Of LidarDevice)()

        Dim doc As New XmlDocument()
        doc.LoadXml(ResidentConfigXmlWriter.BuildRuntimeSettingsXml(snapshot))
        Dim root = doc.DocumentElement

        Assert.IsNull(root.SelectSingleNode("LidarAdapterGuid"))
        Assert.IsNull(root.SelectSingleNode("LidarIpAddress"))
        Assert.IsNotNull(root.SelectSingleNode("LidarDevices"))
        Assert.AreEqual(0, root.SelectNodes("LidarDevices/Lidar").Count)
    End Sub

    <TestMethod>
    Public Sub BuildRuntimeSettingsXml_NullLidarDevices_Throws()
        ' WriteConfiguration catches and logs this; it never saves a partial file.
        Dim snapshot = SampleSnapshot()
        snapshot.LidarDevices = Nothing

        Assert.ThrowsExactly(Of NullReferenceException)(
            Sub() ResidentConfigXmlWriter.BuildRuntimeSettingsXml(snapshot))
    End Sub

    <TestMethod>
    Public Sub BuildRuntimeSettingsXml_NullStrings_WriteEmptyElements()
        Dim snapshot = SampleSnapshot()
        snapshot.NetworkDriveMapping = Nothing
        snapshot.ETAS_USER_PATH = ""

        Dim xml = ResidentConfigXmlWriter.BuildRuntimeSettingsXml(snapshot)

        StringAssert.Contains(xml, "<NetworkDriveMapping />")
        StringAssert.Contains(xml, "<ETAS_USER_PATH />")
    End Sub

    <TestMethod>
    Public Sub BuildRuntimeSettingsXml_EscapesMarkupInValues()
        Dim snapshot = SampleSnapshot()
        snapshot.NetworkAdapterDescription = "A & B <x>"

        Dim doc As New XmlDocument()
        doc.LoadXml(ResidentConfigXmlWriter.BuildRuntimeSettingsXml(snapshot))

        Assert.AreEqual("A & B <x>", doc.DocumentElement.SelectSingleNode("NetworkAdapterDescription").InnerText)
    End Sub

    <TestMethod>
    Public Sub BuildRuntimeSettingsXml_RoundTripsThroughReaderNodeNames()
        ' Guards the writer/reader name contract (e.g. Compression/MaxRetries, OxtsConfiguration/NcomPort).
        Dim doc As New XmlDocument()
        doc.LoadXml(ResidentConfigXmlWriter.BuildRuntimeSettingsXml(SampleSnapshot()))
        Dim root = doc.DocumentElement
        Dim ignore As Action(Of String) = Sub(m)
                                          End Sub

        Dim compression = root.SelectSingleNode("Compression")
        Assert.AreEqual(3, ConfigValueReader.ReadInteger(compression, "CompressionLevel", 1, ignore))
        Assert.AreEqual(7, ConfigValueReader.ReadInteger(compression, "MaxRetries", 5, ignore))
        Assert.AreEqual(11, ConfigValueReader.ReadInteger(compression, "RetryDelaySeconds", 10, ignore))
        Assert.AreEqual(31, ConfigValueReader.ReadInteger(compression, "FileLockTimeoutSeconds", 30, ignore))
        Assert.IsTrue(ConfigValueReader.ReadBoolean(compression, "CompressASC", False, ignore))

        Dim oxts = root.SelectSingleNode("OxtsConfiguration")
        Assert.IsTrue(ConfigValueReader.ReadBoolean(oxts, "OxtsEnabled", False, ignore))
        Assert.AreEqual(3000, ConfigValueReader.ReadInteger(oxts, "NcomPort", 0, ignore))

        Dim timeSync = root.SelectSingleNode("TimeSyncConfiguration")
        Assert.AreEqual("TIMEMACHINE", ConfigValueReader.ReadString(timeSync, "Provider", "OXTS", ignore))

        Dim timeMachine = root.SelectSingleNode("TimeMachineConfiguration")
        Assert.IsFalse(ConfigValueReader.ReadBoolean(timeMachine, "PtpAssumeLocked", True, ignore))

        Assert.AreEqual(30, ConfigValueReader.ReadInteger(root, "RecordWAVTime", 0, ignore))
        Assert.AreEqual("D:\HB", ConfigValueReader.ReadString(root, "BaseDataCollectionPath", "", ignore))
    End Sub

End Class
