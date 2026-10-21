Option Strict Off
Option Explicit On

Imports System.Collections.Generic
Imports System.Xml

''' <summary>
''' Point-in-time copy of the runtime settings that GmResidentClient.WriteConfiguration persists.
''' Field types intentionally mirror the source globals (e.g. RecordWAVTime is a String) so the
''' generated text is identical to the pre-extraction output.
''' </summary>
Friend NotInheritable Class ResidentConfigSnapshot
    ' INCA
    Public INCADatabase As String
    Public INCAWorkspace As String
    Public InitialINCAExperiment As String
    Public INCAVariableFile As String
    Public ETAS_USER_PATH As String
    Public SignalRegistrationMode As String

    ' Recording
    Public RecordWAVTime As String
    Public RecordFileDurationMinutes As Long
    Public MuteVoiceRecordingMessages As Boolean
    Public AudioToTextConversion As Boolean

    ' Alternate recording
    Public AlternateRecordEnabled As Boolean
    Public AlternateRecordConfig As String
    Public EnableAltRecReStartAfterRecordStop As Boolean

    ' Data storage & network
    Public BaseDataCollectionPath As String
    Public NetworkDriveLetter As String
    Public NetworkDriveMapping As String

    ' Local tool & script paths
    Public CSVScriptsPath As String
    Public SevenZipPath As String

    ' Compression
    Public CompressMF4 As Boolean
    Public CompressPCAP As Boolean
    Public CompressASC As Boolean
    Public CompressVSB As Boolean
    Public DeleteAfterCompression As Boolean
    Public CompressionLevel As Integer
    Public ZipCompressionMaxRetries As Integer
    Public ZipCompressionRetryDelay As Integer
    Public ZipFileLockTimeout As Integer

    ' Hardware
    Public NetworkAdapterDescription As String

    ' OXTS
    Public OxtsEnabled As Boolean
    Public OxtsNcomIpAddress As String
    Public OxtsNcomPort As Integer
    Public OxtsGpsLockTimeout As Integer
    Public OxtsWaitForLockOnStart As Boolean

    ' Time sync / TimeMachine
    Public TimeSyncEnabled As Boolean
    Public TimeSyncProviderType As String
    Public TimeMachineEnabled As Boolean
    Public TimeMachineIpAddress As String
    Public TimeMachinePort As Integer
    Public TimeMachinePollMs As Integer
    Public TimeMachinePtpAssumeLocked As Boolean

    ' LiDAR
    Public LidarCaptureEnabled As Boolean
    Public LidarDevices As List(Of LidarDevice)

    ' Application
    Public APICommErrorMsgDelayTime As String
End Class

''' <summary>
''' Builds the runtime-settings XML document that GmResidentClient.WriteConfiguration merges into
''' the user config file. Extracted verbatim from WriteConfiguration; the merge and save stay there.
''' </summary>
Friend NotInheritable Class ResidentConfigXmlWriter

    Private Sub New()
    End Sub

    Friend Shared Function BuildRuntimeSettingsXml(s As ResidentConfigSnapshot) As String
        Dim generated As New System.Text.StringBuilder()
        ' Use XmlWriter with proper settings for indentation and readability
        Dim settings As New XmlWriterSettings With {
            .Indent = True,
            .IndentChars = vbTab,
            .NewLineOnAttributes = False
        }

        Using writer As XmlWriter = XmlWriter.Create(generated, settings)
            writer.WriteStartDocument()
            writer.WriteStartElement("Configuration") ' Root element

            ' ════════════════════════════════════════════════════════════
            ' INCA CONFIGURATION SECTION
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" INCA Configuration ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteElementString("INCADatabase", s.INCADatabase)
            writer.WriteElementString("INCAWorkspace", s.INCAWorkspace)
            writer.WriteElementString("INCAExperiment", s.InitialINCAExperiment)
            writer.WriteElementString("INCAVariableFile", s.INCAVariableFile)
            writer.WriteElementString("ETAS_USER_PATH", s.ETAS_USER_PATH)
            writer.WriteElementString("SignalRegistrationMode", s.SignalRegistrationMode)

            ' ════════════════════════════════════════════════════════════
            ' RECORDING CONFIGURATION SECTION
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" Recording Configuration ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteElementString("RecordWAVTime", s.RecordWAVTime.ToString())
            writer.WriteElementString("RecordFileDurationMinutes", s.RecordFileDurationMinutes.ToString())
            writer.WriteElementString("MuteVoiceRecordingMessages", s.MuteVoiceRecordingMessages.ToString())
            writer.WriteElementString("AudioToTextConversion", s.AudioToTextConversion.ToString())

            ' ════════════════════════════════════════════════════════════
            ' ALTERNATE RECORDING SECTION
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" Alternate Recording (CANalyzer/VehicleSpy) ")
            writer.WriteComment(" Valid AlternateRecordConfig values: VehicleSpy, CANalyzer, None ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteElementString("AlternateRecordEnabled", s.AlternateRecordEnabled.ToString())
            writer.WriteElementString("AlternateRecordConfig", s.AlternateRecordConfig)
            writer.WriteElementString("EnableAltRecReStartAfterRecordStop", s.EnableAltRecReStartAfterRecordStop.ToString())

            ' ════════════════════════════════════════════════════════════
            ' DATA STORAGE & NETWORK SECTION
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" Data Storage & Network ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteElementString("BaseDataCollectionPath", s.BaseDataCollectionPath)

            writer.WriteElementString("NetworkDriveLetter", s.NetworkDriveLetter)

            writer.WriteElementString("NetworkDriveMapping", s.NetworkDriveMapping)

            ' ════════════════════════════════════════════════════════════
            ' LOCAL TOOL & SCRIPT PATHS SECTION
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" Local Tool & Script Paths ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteElementString("CSVScriptsPath", s.CSVScriptsPath)
            writer.WriteElementString("SevenZipPath", s.SevenZipPath)

            ' ════════════════════════════════════════════════════════════
            ' COMPRESSION CONFIGURATION SECTION (NESTED)
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" Compression Configuration ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteStartElement("Compression")

            ' File type compression switches
            writer.WriteComment(" File type compression switches ")
            writer.WriteElementString("CompressMF4", s.CompressMF4.ToString())
            writer.WriteElementString("CompressPCAP", s.CompressPCAP.ToString())
            writer.WriteElementString("CompressASC", s.CompressASC.ToString())
            writer.WriteElementString("CompressVSB", s.CompressVSB.ToString())

            ' Deletion policy
            writer.WriteComment(" Deletion policy (SAFE DEFAULT = False) ")
            writer.WriteElementString("DeleteAfterCompression", s.DeleteAfterCompression.ToString())

            ' Compression parameters
            writer.WriteComment(" Compression parameters ")
            writer.WriteElementString("CompressionLevel", s.CompressionLevel.ToString())
            writer.WriteComment(" 1=Fastest, 9=Best ")

            ' Simple XML names to match ReadConfiguration
            writer.WriteElementString("MaxRetries", s.ZipCompressionMaxRetries.ToString())
            writer.WriteElementString("RetryDelaySeconds", s.ZipCompressionRetryDelay.ToString())

            ' Lock detection
            writer.WriteComment(" Lock detection ")
            writer.WriteElementString("FileLockTimeoutSeconds", s.ZipFileLockTimeout.ToString())

            writer.WriteEndElement() ' Close <Compression>

            ' ════════════════════════════════════════════════════════════
            ' HARDWARE CONFIGURATION SECTION
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" Hardware Configuration ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteElementString("NetworkAdapterDescription", s.NetworkAdapterDescription)

            ' ════════════════════════════════════════════════════════════
            ' OXTS CONFIGURATION SECTION (GPS/INS Integration)
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" OXTS GPS/INS Configuration ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteStartElement("OxtsConfiguration")

            writer.WriteComment(" Enable/disable OXTS INS integration ")
            writer.WriteElementString("OxtsEnabled", s.OxtsEnabled.ToString())

            writer.WriteComment(" NCOM UDP listener settings ")
            writer.WriteElementString("NcomIpAddress", s.OxtsNcomIpAddress)
            writer.WriteElementString("NcomPort", s.OxtsNcomPort.ToString())

            writer.WriteComment(" GPS lock behavior ")
            writer.WriteElementString("GpsLockTimeout", s.OxtsGpsLockTimeout.ToString())
            writer.WriteElementString("WaitForLockOnStart", s.OxtsWaitForLockOnStart.ToString())

            writer.WriteEndElement() ' Close OxtsConfiguration

            writer.WriteComment(" Time Sync provider selection ")
            writer.WriteStartElement("TimeSyncConfiguration")
            writer.WriteElementString("EnableTimeSync", s.TimeSyncEnabled.ToString())
            writer.WriteElementString("Provider", s.TimeSyncProviderType)
            writer.WriteEndElement()

            writer.WriteComment(" TimeMachine Locator protocol settings ")
            writer.WriteStartElement("TimeMachineConfiguration")
            writer.WriteElementString("Enabled", s.TimeMachineEnabled.ToString())
            writer.WriteElementString("DeviceIp", s.TimeMachineIpAddress)
            writer.WriteElementString("Port", s.TimeMachinePort.ToString())
            writer.WriteElementString("PollMs", s.TimeMachinePollMs.ToString())
            writer.WriteElementString("PtpAssumeLocked", s.TimeMachinePtpAssumeLocked.ToString())
            writer.WriteEndElement()

            ' ════════════════════════════════════════════════════════════
            ' LIDAR CONFIGURATION SECTION (Legacy + Multi-Device)
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" LiDAR Configuration (Legacy + Multi-Device) ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteElementString("LidarCaptureEnabled", s.LidarCaptureEnabled.ToString())

            ' Write legacy single-device tags for backward compatibility
            writer.WriteComment(" Legacy single-device (backward compatibility) ")
            If s.LidarDevices.Count > 0 Then
                writer.WriteElementString("LidarAdapterGuid", s.LidarDevices(0).LidarAdapterGuid)
                writer.WriteElementString("LidarIpAddress", s.LidarDevices(0).LidarIpAddress)
                writer.WriteElementString("LidarDataPort", s.LidarDevices(0).LidarDataPort.ToString())
                writer.WriteElementString("LidarImuPort", s.LidarDevices(0).LidarImuPort.ToString())
            End If

            ' Write new multi-device structure
            writer.WriteComment(" Multi-device configuration ")
            writer.WriteStartElement("LidarDevices")

            If s.LidarDevices IsNot Nothing AndAlso s.LidarDevices.Count > 0 Then
                For i As Integer = 0 To s.LidarDevices.Count - 1
                    ' Write device even if Enabled=False
                    writer.WriteStartElement("Lidar")
                    writer.WriteAttributeString("id", (i + 1).ToString())
                    writer.WriteAttributeString("enabled", s.LidarDevices(i).Enabled.ToString())

                    writer.WriteElementString("AdapterGuid", s.LidarDevices(i).LidarAdapterGuid)
                    writer.WriteElementString("IpAddress", s.LidarDevices(i).LidarIpAddress)
                    writer.WriteElementString("DataPort", s.LidarDevices(i).LidarDataPort.ToString())
                    writer.WriteElementString("ImuPort", s.LidarDevices(i).LidarImuPort.ToString())

                    writer.WriteEndElement() ' Close Lidar
                Next
            End If

            writer.WriteEndElement() ' Close LidarDevices

            ' ════════════════════════════════════════════════════════════
            ' APPLICATION CONFIGURATION SECTION
            ' ════════════════════════════════════════════════════════════
            writer.WriteComment(" ================================================================ ")
            writer.WriteComment(" Application Configuration ")
            writer.WriteComment(" ================================================================ ")

            writer.WriteElementString("APICommErrorMsgDelayTime", s.APICommErrorMsgDelayTime.ToString())

            writer.WriteEndElement() ' Close root element
            writer.WriteEndDocument()
        End Using

        Return generated.ToString()
    End Function
End Class
