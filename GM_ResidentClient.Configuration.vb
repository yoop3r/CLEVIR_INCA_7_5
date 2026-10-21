Option Strict Off
Option Explicit On

Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Speech.Synthesis
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Forms.DataVisualization.Charting
Imports System.Xml
Imports SharpPcap

Partial Public Class GmResidentClient

    ''' <summary>
    ''' ❌ DEPRECATED: This function is no longer used.
    ''' Camera IP addresses are now managed in config.xml and mapped via ActiveCameras.
    ''' Kept for backward compatibility but does nothing.
    ''' </summary>
    Private Sub WriteCameraIpAddressesFile()
        ' ❌ DEPRECATED: No longer writes to CameraIPAddresses.txt
        ' Camera configuration is now managed in config.xml
        HandleUserMessageLogging("GMRC", "WriteCameraIpAddressesFile: Function is deprecated - camera IPs now managed in config.xml")
    End Sub

    ''' <summary>
    ''' ❌ DEPRECATED: This function is no longer used.
    ''' Camera IP addresses are now read from config.xml at startup.
    ''' Kept for backward compatibility but does nothing.
    ''' </summary>
    Private Sub ReadCameraIpAddressesFile()
        ' ❌ DEPRECATED: No longer reads from CameraIPAddresses.txt
        ' Camera configuration is now loaded from config.xml via Module1.ReadCameraConfiguration()
        HandleUserMessageLogging("GMRC", "ReadCameraIpAddressesFile: Function is deprecated - camera IPs now loaded from config.xml")
    End Sub

    Public Function ReadUserConfigFile(ByVal configFileName As String) As Integer
        ' Called from the Initialize routine when the user logs in with a USER ID.
        ' Reads the user-specific config file (e.g., userid.xml), extracts configuration information,
        ' and assigns it to variables.

        Dim lUserConfigFileName As String = Path.Combine(My.Application.Info.DirectoryPath, configFileName)
        Dim returnValue As Integer = 0

        HandleUserMessageLogging("GMRC", "ReadUserConfigFile: Reading User Config File " & lUserConfigFileName & "...")

        If Not File.Exists(lUserConfigFileName) Then
            HandleUserMessageLogging("GMRC", "ReadUserConfigFile: Copying Default Config File to " & lUserConfigFileName & "...")
            File.Copy(Path.Combine(My.Application.Info.DirectoryPath, "config.xml"), lUserConfigFileName)
            Thread.Sleep(1000)
        End If

        ' ✅ Read configuration into variables
        If ReadConfiguration(lUserConfigFileName) Then
            HandleUserMessageLogging("GMRC", "ReadUserConfigFile: Reading User Config File Complete")

            ' ✅ NEW: Sync UI controls with loaded values
            SyncUIFromConfig()
        Else
            HandleUserMessageLogging("GMRC", "ReadUserConfigFile: Failed to read user config file.")
            returnValue = -1 ' Indicate failure
        End If

        Return returnValue
    End Function

    ''' <summary>
    ''' Safely reads a Boolean config value from XML with logging.
    ''' </summary>
    Private Function ReadBooleanConfig(parentNode As XmlNode,
                                       nodeName As String,
                                       defaultValue As Boolean) As Boolean
        Dim node = parentNode.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Dim value As Boolean = CBool(node.InnerText)
            HandleUserMessageLogging("GMRC", $"{nodeName} set to: {value}")
            Return value
        End If

        HandleUserMessageLogging("GMRC", $"{nodeName} not found, using default: {defaultValue}")
        Return defaultValue
    End Function

    ''' <summary>
    ''' Safely reads a String config value from XML with logging.
    ''' </summary>
    Private Function ReadStringConfig(parentNode As XmlNode,
                                      nodeName As String,
                                      defaultValue As String) As String
        Dim node = parentNode.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Dim value As String = node.InnerText.Trim()
            HandleUserMessageLogging("GMRC", $"{nodeName} set to: {value}")
            Return value
        End If

        HandleUserMessageLogging("GMRC", $"{nodeName} not found, using default: {defaultValue}")
        Return defaultValue
    End Function

    ''' <summary>
    ''' Safely reads an Integer config value from XML with logging.
    ''' </summary>
    Private Function ReadIntegerConfig(parentNode As XmlNode,
                                       nodeName As String,
                                       defaultValue As Integer) As Integer
        Dim node = parentNode.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Dim value As Integer = CInt(node.InnerText)
            HandleUserMessageLogging("GMRC", $"{nodeName} set to: {value}")
            Return value
        End If

        HandleUserMessageLogging("GMRC", $"{nodeName} not found, using default: {defaultValue}")
        Return defaultValue
    End Function
    Public Function ReadConfiguration(configFilePath As String, Optional failureReporter As Action(Of String) = Nothing) As Boolean
        ' Centralized helper to read any XML-based configuration file.
        ' Returns True on success, False on failure.
        If _inMeasureMode OrElse (MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.MeasurementStarted) Then
            Return ReportInitializationFailure("Stop INCA measurement/recording before reloading configuration.", failureReporter)
        End If
        If Not File.Exists(configFilePath) Then
            ClearProcessorConfiguration()
            Return ReportInitializationFailure($"ReadConfiguration: File not found at {configFilePath}", failureReporter)
        End If

        Try
            Dim xmlDoc As New XmlDocument()
            xmlDoc.Load(configFilePath)
            Dim root As XmlNode = xmlDoc.DocumentElement
            Dim masterDoc As XmlDocument = xmlDoc
            Dim masterPath As String = Path.Combine(My.Application.Info.DirectoryPath, "config.xml")
            If Not String.Equals(Path.GetFullPath(configFilePath), Path.GetFullPath(masterPath), StringComparison.OrdinalIgnoreCase) Then
                masterDoc = New XmlDocument()
                masterDoc.Load(masterPath)
            End If
            Dim configuration As New IncaConfiguration(masterDoc, My.Application.Info.DirectoryPath)
            If Initialized AndAlso _loadedIncaConfiguration IsNot Nothing AndAlso Not configuration.HasSameSessionSettings(_loadedIncaConfiguration) Then
                Return ReportInitializationFailure("INCA identity, vehicle, or session resources changed in config.xml. Restart CLEVIR to apply these changes; the current session is unchanged.", failureReporter)
            End If
            ClearProcessorConfiguration()

            BaseDataCollectionPath = ReadStringConfig(root, "BaseDataCollectionPath", "C:\HB")

            ' Local tool & script paths
            CSVScriptsPath = ReadStringConfig(root, "CSVScriptsPath", CSVScriptsPath)
            SevenZipPath = ReadStringConfig(root, "SevenZipPath", SevenZipPath)

            ' Integer configs
            RecordWAVTime = ReadIntegerConfig(root, "RecordWAVTime", 30)
            RecordFileDurationMinutes = ReadIntegerConfig(root, "RecordFileDurationMinutes", 5)
            APICommErrorMsgDelayTime = ReadIntegerConfig(root, "APICommErrorMsgDelayTime", 90)

            ' Boolean configs
            MuteVoiceRecordingMessages = ReadBooleanConfig(root, "MuteVoiceRecordingMessages", False)
            AudioToTextConversion = ReadBooleanConfig(root, "AudioToTextConversion", False)
            SkipSubscriptionOnCacheHit = ReadBooleanConfig(root, "SkipSubscriptionOnCacheHit", False)

            ' ════════════════════════════════════════════════════════════
            ' ALTERNATE RECORDING CONFIGURATION SECTION
            ' ════════════════════════════════════════════════════════════
            AlternateRecordEnabled = ReadBooleanConfig(root, "AlternateRecordEnabled", False)
            AlternateRecordConfig = ReadStringConfig(root, "AlternateRecordConfig", "CANalyzer")
            EnableAltRecReStartAfterRecordStop = ReadBooleanConfig(root, "EnableAltRecReStartAfterRecordStop", False)


            ' ================================================================
            ' ✅ NEW: Compression configuration (nested structure)
            ' ================================================================
            Dim compressionNode As XmlNode = root.SelectSingleNode("Compression")
            If compressionNode IsNot Nothing Then
                CompressMF4 = ReadBooleanConfig(compressionNode, "CompressMF4", True)
                CompressPCAP = ReadBooleanConfig(compressionNode, "CompressPCAP", True)
                CompressASC = ReadBooleanConfig(compressionNode, "CompressASC", False)
                CompressVSB = ReadBooleanConfig(compressionNode, "CompressVSB", False)
                DeleteAfterCompression = ReadBooleanConfig(compressionNode, "DeleteAfterCompression", False)
                CompressionLevel = ReadIntegerConfig(compressionNode, "CompressionLevel", 1)
                ZipCompressionMaxRetries = ReadIntegerConfig(compressionNode, "MaxRetries", 5)
                ZipCompressionRetryDelay = ReadIntegerConfig(compressionNode, "RetryDelaySeconds", 10)
                ZipFileLockTimeout = ReadIntegerConfig(compressionNode, "FileLockTimeoutSeconds", 30)
            Else
                ' ✅ BACKWARD COMPATIBILITY: Read flat structure (old config files)
                CompressMF4 = ReadBooleanConfig(root, "CompressMF4", True)
                CompressPCAP = ReadBooleanConfig(root, "CompressPCAP", True)
                CompressASC = False
                CompressVSB = False
                DeleteAfterCompression = False
                CompressionLevel = 1
                CompressionMaxRetries = 5
                CompressionRetryDelay = 10
                ZipCompressionRetryDelay = 10
                ZipFileLockTimeout = 30

                HandleUserMessageLogging("GMRC", "ReadConfiguration: Using legacy flat compression config")
            End If

            LidarCaptureEnabled = ReadBooleanConfig(root, "LidarCaptureEnabled", False)

            ' Signal registration mode (radio buttons will be set later in LoginForm_Load)
            Dim signalRegMode = ReadStringConfig(root, "SignalRegistrationMode", "DISPLAYS").ToUpper()
            SignalRegistrationMode = signalRegMode
            SaveSignalRegistrationMode = signalRegMode

            ' Validate and normalize the value
            Select Case signalRegMode
                Case "FULL", "DISPLAYS", "GO/NOGO", "NEW FULL"
                    ' Valid mode - keep as-is
                Case Else
                    ' Invalid mode - default to DISPLAYS
                    SignalRegistrationMode = "DISPLAYS"
                    SaveSignalRegistrationMode = "DISPLAYS"
            End Select

            HandleUserMessageLogging("GMRC", $"ReadConfiguration: SignalRegistrationMode set to '{SignalRegistrationMode}' (UI update deferred)")

            ' Base data collection path validation
            BaseDataCollectionPath = ReadStringConfig(root, "BaseDataCollectionPath", My.Application.Info.DirectoryPath)

            ' Fail fast: verify the drive is present, the folder exists (create it if the
            ' drive is ready) and that we can actually write there. Catching this now avoids
            ' the user discovering a dead drive only after INCA/cameras/LiDAR are fully loaded.
            '
            ' A transient failure (drive unplugged) is NOT a malformed-config problem, so it
            ' loops here with Retry/Cancel rather than returning False - returning False makes
            ' the caller offer the Configuration Editor, which is both misleading and unable
            ' to fix a disconnected drive.
            Dim dataPathStatus As DataPathStatus

            Do
                dataPathStatus = ValidateDataCollectionPath(BaseDataCollectionPath,
                                                            createIfMissing:=True,
                                                            testWritable:=True)

                If dataPathStatus.IsValid Then Exit Do

                HandleUserMessageLogging("GMRC", dataPathStatus.Message)

                If Not dataPathStatus.IsRecoverable Then
                    ' Genuine configuration error (empty, spaces, not rooted, not writable).
                    Return ReportInitializationFailure(dataPathStatus.Message, failureReporter)
                End If

                ' MsgBoxSetForeground: this runs on the background thread started by
                ' InitForm's Task.Run(ReadConfigFile) while the topmost splash screen is
                ' still up - without it the prompt can appear behind the splash and look
                ' like a hang.
                Dim retryChoice = MsgBox(
                    dataPathStatus.Message & vbCrLf & vbCrLf &
                    "Click Retry once the drive is connected." & vbCrLf & vbCrLf &
                    "Note: if you edit config.xml instead, CLEVIR must be restarted for the change to take effect.",
                    MsgBoxStyle.RetryCancel Or MsgBoxStyle.Exclamation Or MsgBoxStyle.MsgBoxSetForeground,
                    "CLEVIR - Data Collection Drive Unavailable")

                If retryChoice = MsgBoxResult.Cancel Then
                    HandleUserMessageLogging("GMRC", "User cancelled at unavailable data collection drive. Exiting...")
                    StartupAbortRequested = True
                    Return False
                End If

                HandleUserMessageLogging("GMRC", "Retrying data collection path validation...")
            Loop

            HandleUserMessageLogging("GMRC",
                $"BaseDataCollectionPath validated: {BaseDataCollectionPath} - {dataPathStatus.SpaceSummary}")

            If dataPathStatus.Severity <> DataPathSeverity.Ok Then
                HandleUserMessageLogging("GMRC", dataPathStatus.Message)
            End If

            ' Network drive configuration
            NetworkDriveLetter = ReadStringConfig(root, "NetworkDriveLetter", "Q:")
            NetworkDriveMapping = ReadStringConfig(root, "NetworkDriveMapping", "")

            ' ════════════════════════════════════════════════════════════
            ' OXTS CONFIGURATION SECTION (New GPS/INS Integration)
            ' ════════════════════════════════════════════════════════════
            Dim oxtsNode As XmlNode = root.SelectSingleNode("OxtsConfiguration")
            If oxtsNode IsNot Nothing Then
                OxtsEnabled = ReadBooleanConfig(oxtsNode, "OxtsEnabled", False)
                OxtsNcomIpAddress = ReadStringConfig(oxtsNode, "NcomIpAddress", "10.5.55.200")
                OxtsNcomPort = ReadIntegerConfig(oxtsNode, "NcomPort", 3000)
                OxtsGpsLockTimeout = ReadIntegerConfig(oxtsNode, "GpsLockTimeout", 30000)
                OxtsWaitForLockOnStart = ReadBooleanConfig(oxtsNode, "WaitForLockOnStart", True)

                HandleUserMessageLogging("GMRC", $"OXTS Configuration: Enabled={OxtsEnabled}, IP={OxtsNcomIpAddress}:{OxtsNcomPort}")
            Else
                ' Defaults when node is missing
                OxtsEnabled = False
                OxtsNcomIpAddress = "10.5.55.200"
                OxtsNcomPort = 3000
                OxtsGpsLockTimeout = 30000
                OxtsWaitForLockOnStart = True
                HandleUserMessageLogging("GMRC", "No OxtsConfiguration found - using defaults (disabled)")
            End If

            ' ════════════════════════════════════════════════════════════
            ' Time Sync Provider Selection
            ' ════════════════════════════════════════════════════════════
            Dim timeSyncNode As XmlNode = root.SelectSingleNode("TimeSyncConfiguration")
            If timeSyncNode IsNot Nothing Then
                TimeSyncEnabled = ReadBooleanConfig(timeSyncNode, "EnableTimeSync", False)
                TimeSyncProviderType = ReadStringConfig(timeSyncNode, "Provider", "OXTS").Trim().ToUpperInvariant()
            Else
                TimeSyncEnabled = OxtsEnabled
                TimeSyncProviderType = "OXTS"
            End If

            Dim tmNode As XmlNode = root.SelectSingleNode("TimeMachineConfiguration")
            If tmNode IsNot Nothing Then
                TimeMachineEnabled = ReadBooleanConfig(tmNode, "Enabled", False)
                TimeMachineIpAddress = ReadStringConfig(tmNode, "DeviceIp", "255.255.255.255")
                TimeMachinePort = ReadIntegerConfig(tmNode, "Port", 7372)
                TimeMachinePollMs = ReadIntegerConfig(tmNode, "PollMs", 1000)
                TimeMachinePtpAssumeLocked = ReadBooleanConfig(tmNode, "PtpAssumeLocked", True)
            Else
                TimeMachineEnabled = False
                TimeMachineIpAddress = "255.255.255.255"
                TimeMachinePort = 7372
                TimeMachinePollMs = 1000
                TimeMachinePtpAssumeLocked = True
            End If

            If TimeSyncProviderType = "TIMEMACHINE" AndAlso Not TimeMachineEnabled Then
                HandleUserMessageLogging("GMRC", "TimeSync provider set to TimeMachine but TimeMachineConfiguration.Enabled is False - falling back to OXTS")
                TimeSyncProviderType = "OXTS"
            End If

            HandleUserMessageLogging("GMRC", $"TimeSync Configuration: Enabled={TimeSyncEnabled}, Provider={TimeSyncProviderType}")

            ' ════════════════════════════════════════════════════════════
            ' LIDAR CONFIGURATION SECTION (Legacy + Multi-Device)
            ' ════════════════════════════════════════════════════════════
            ReadLidarConfiguration(root)

            ' ════════════════════════════════════════════════════════════
            ' ✅ NEW: CAMERA CONFIGURATION SECTION
            ' ════════════════════════════════════════════════════════════
            Dim cameraConfigNode As XmlNode = root.SelectSingleNode("CameraConfiguration")
            If cameraConfigNode IsNot Nothing Then
                ReadCameraConfiguration(cameraConfigNode)
            Else
                ' Load defaults if section missing
                LoadDefaultCameraConfig()
                HandleUserMessageLogging("GMRC", "No CameraConfiguration found - using default IP mappings")
            End If

            ' ════════════════════════════════════════════════════════════
            ' Load OXTS NCOM PCAP capture configuration
            ' ════════════════════════════════════════════════════════════
            LoadOxtsCaptureConfig(root)

            ' <CurrentVehicleUsage> is obsolete: compression is controlled by <Compression>.
            Dim legacyUsage As String = root.SelectSingleNode("CurrentVehicleUsage")?.InnerText
            If Not String.IsNullOrWhiteSpace(legacyUsage) Then
                HandleUserMessageLogging("GMRC", $"ReadConfiguration: Ignoring obsolete <CurrentVehicleUsage>{legacyUsage.Trim()}</CurrentVehicleUsage>; compression follows <Compression>.")
            End If

            ApplyIncaConfiguration(configuration, masterDoc)
            _loadedIncaConfiguration = configuration
            HandleUserMessageLogging("GMRC", "ReadConfiguration: Configuration loaded successfully")

            ' Load session metadata dropdowns from SessionMetadata.xml (non-fatal if absent)
            Dim sessionMetaPath As String = Path.Combine(My.Application.Info.DirectoryPath, "SessionMetadata.xml")
            LoadSessionMetadataConfig(sessionMetaPath)

            Return True

        Catch ex As Exception
            ClearProcessorConfiguration()
            Return ReportInitializationFailure($"ReadConfiguration: Error reading '{configFilePath}' - {ex.Message}", failureReporter)
        End Try
    End Function

    ''' <summary>
    ''' Synchronizes OnVehicleScreen UI controls with configuration values.
    ''' Call this AFTER ReadConfiguration() to ensure UI reflects loaded settings.
    ''' </summary>
    Private Sub SyncUIFromConfig()
        Try
            ' ✅ FIX: Check if form exists AND controls are initialized
            If OnVehicleScreen Is Nothing OrElse
       OnVehicleScreen.IsDisposed OrElse
       OnVehicleScreen.TextBox1 Is Nothing OrElse
       OnVehicleScreen.ComboBox1 Is Nothing Then

                HandleUserMessageLogging("GMRC", "SyncUIFromConfig: OnVehicleScreen controls not yet initialized - will sync later")
                Return
            End If

            ' ════════════════════════════════════════════════════════════
            ' SYNC RECORD WAV TIME
            ' ════════════════════════════════════════════════════════════
            OnVehicleScreen.TextBox1.Text = RecordWAVTime.ToString()
            HandleUserMessageLogging("GMRC", $"SyncUIFromConfig: Set RecordWAVTime to {RecordWAVTime}")

            ' ════════════════════════════════════════════════════════════
            ' SYNC RECORD FILE DURATION
            ' ════════════════════════════════════════════════════════════
            OnVehicleScreen.ComboBox1.Text = RecordFileDurationMinutes.ToString()
            HandleUserMessageLogging("GMRC", $"SyncUIFromConfig: Set RecordFileDurationMinutes to {RecordFileDurationMinutes}")

            ' ════════════════════════════════════════════════════════════
            ' ✅ NEW: SYNC SIGNAL LIST + EXPERIMENT TO LOGINFORM.LABEL14
            ' ════════════════════════════════════════════════════════════
            If LoginForm IsNot Nothing AndAlso Not LoginForm.IsDisposed Then
                ' Extract just the filename (without path) for cleaner display
                Dim signalListFileName As String = If(
                String.IsNullOrEmpty(INCAVariableFile),
                "[Not Set]",
                Path.GetFileName(INCAVariableFile)
            )

                ' ✅ NEW: Include experiment name
                Dim experimentName As String = If(
                String.IsNullOrEmpty(INCAExperiment),
                "[Not Set]",
                INCAExperiment
            )

                ' Format: "SignalList.xlsx / ExperimentName"
                LoginForm.Label4.Text = $"{signalListFileName} / {experimentName}"

                HandleUserMessageLogging("GMRC",
                $"SyncUIFromConfig: Set Signal List display to '{signalListFileName} / {experimentName}'")
            End If

            HandleUserMessageLogging("GMRC", "SyncUIFromConfig: UI synchronized with configuration")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"SyncUIFromConfig: Error - {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Parses a whitespace-separated vector of doubles (e.g. "0 0 0") from
    ''' config.xml, falling back to the provided default if the text is missing,
    ''' malformed, or the wrong length.
    ''' </summary>
    Private Function ParseDoubleVector(text As String, expectedLength As Integer, fallback As Double()) As Double()
        If String.IsNullOrWhiteSpace(text) Then
            Return fallback
        End If

        Dim parts As String() = text.Split(New Char() {" "c, ","c}, StringSplitOptions.RemoveEmptyEntries)
        If parts.Length <> expectedLength Then
            Return fallback
        End If

        Dim result(expectedLength - 1) As Double
        For i As Integer = 0 To expectedLength - 1
            If Not Double.TryParse(parts(i), result(i)) Then
                Return fallback
            End If
        Next

        Return result
    End Function

    Private Sub ReadLidarConfiguration(root As XmlNode)
        Try
            ' ================================================================
            ' Read master enable flag (always check this first)
            ' ================================================================
            LidarCaptureEnabled = ReadBooleanConfig(root, "LidarCaptureEnabled", False)

            ' ================================================================
            ' Read multi-device configuration (N-LiDAR support)
            ' ================================================================
            Dim lidarDevicesNode As XmlNode = root.SelectSingleNode("LidarDevices")
            If lidarDevicesNode Is Nothing Then
                HandleUserMessageLogging("GMRC", "⚠️ No LidarDevices node found in configuration")
                Return
            End If

            ' ✅ CRITICAL FIX: Clear and rebuild from XML (source of truth)
            LidarDevices.Clear()

            Dim lidarNodes = lidarDevicesNode.SelectNodes("Lidar")
            If lidarNodes Is Nothing OrElse lidarNodes.Count = 0 Then
                HandleUserMessageLogging("GMRC", "No <Lidar> nodes found - LiDAR disabled")
                Return
            End If

            ' ================================================================
            ' Parse each <Lidar> node and register with Hesai SDK
            ' ================================================================
            Dim deviceIndex As Integer = 0

            For Each lidarNode As XmlNode In lidarNodes
                Try
                    Dim deviceId As String = lidarNode.Attributes("id")?.Value
                    Dim enabledAttr As String = lidarNode.Attributes("enabled")?.Value
                    Dim enabled As Boolean = If(String.IsNullOrEmpty(enabledAttr), True, Boolean.Parse(enabledAttr))

                    If Not enabled Then
                        HandleUserMessageLogging("GMRC", $"LiDAR '{deviceId}' disabled in config")
                        Continue For
                    End If

                    ' ✅ Parse device configuration
                    Dim adapterGuid As String = lidarNode.SelectSingleNode("AdapterGuid")?.InnerText
                    Dim ipAddress As String = lidarNode.SelectSingleNode("IpAddress")?.InnerText
                    Dim dataPortStr As String = lidarNode.SelectSingleNode("DataPort")?.InnerText
                    Dim imuPortStr As String = lidarNode.SelectSingleNode("ImuPort")?.InnerText
                    Dim orientation As String = lidarNode.SelectSingleNode("Orientation")?.InnerText?.Trim()

                    Dim dataPort As UShort = If(UShort.TryParse(dataPortStr, dataPort), dataPort, 2368US)
                    Dim imuPort As UShort = If(UShort.TryParse(imuPortStr, imuPort), imuPort, 8308US)

                    ' ✅ Create device object
                    Dim device As New LidarDevice(adapterGuid, ipAddress, dataPort, imuPort, deviceId, orientation) With {
                    .Enabled = enabled
                }

                    LidarDevices.Add(device)
                    Dim orientLabel As String = If(String.IsNullOrWhiteSpace(orientation), "(no orientation)", orientation)
                    HandleUserMessageLogging("GMRC", $"✅ Loaded LiDAR: {deviceId} [{orientLabel}] at {ipAddress}:{dataPort}")

                    ' ================================================================
                    ' ✅ NEW: Parse optional <Extrinsic> mount-transform config
                    ' Absence or IsCalibrated="False" means alignment hasn't been
                    ' performed yet; downstream manifest consumers must be able to
                    ' distinguish that from a genuine identity-transform result.
                    ' ================================================================
                    Try
                        Dim extrinsicNode As XmlNode = lidarNode.SelectSingleNode("Extrinsic")
                        If extrinsicNode IsNot Nothing Then
                            Dim isCalibratedAttr As String = extrinsicNode.Attributes("IsCalibrated")?.Value
                            Dim isCalibrated As Boolean = If(String.IsNullOrEmpty(isCalibratedAttr), False, Boolean.Parse(isCalibratedAttr))

                            Dim translationText As String = extrinsicNode.SelectSingleNode("Translation")?.InnerText
                            Dim rotationText As String = extrinsicNode.SelectSingleNode("RotationQuaternion")?.InnerText
                            Dim residualText As String = extrinsicNode.SelectSingleNode("ResidualError")?.InnerText
                            Dim dateText As String = extrinsicNode.SelectSingleNode("DatePerformed")?.InnerText

                            Dim translation As Double() = ParseDoubleVector(translationText, 3, {0.0, 0.0, 0.0})
                            Dim rotation As Double() = ParseDoubleVector(rotationText, 4, {0.0, 0.0, 0.0, 1.0})

                            Dim residual As Double? = Nothing
                            Dim residualParsed As Double
                            If Double.TryParse(residualText, residualParsed) Then
                                residual = residualParsed
                            End If

                            Dim datePerformed As DateTime? = Nothing
                            Dim dateParsed As DateTime
                            If DateTime.TryParse(dateText, dateParsed) Then
                                datePerformed = dateParsed
                            End If

                            device.ExtrinsicConfig = New PcapEventBridge.ExtrinsicRecord With {
                                .CalibrationId = extrinsicNode.SelectSingleNode("CalibrationId")?.InnerText,
                                .DatePerformed = datePerformed,
                                .Method = extrinsicNode.SelectSingleNode("Method")?.InnerText,
                                .TranslationMeters = translation,
                                .RotationQuaternion = rotation,
                                .ResidualError = residual,
                                .IsCalibrated = isCalibrated
                            }
                            device.HasExtrinsicConfig = True

                            If isCalibrated Then
                                HandleUserMessageLogging("GMRC", $"✅ Extrinsic loaded for {deviceId} (calibration ID: {device.ExtrinsicConfig.CalibrationId})")
                            Else
                                HandleUserMessageLogging("GMRC", $"⚠️ Extrinsic for {deviceId} is uncalibrated (identity placeholder)")
                            End If
                        Else
                            HandleUserMessageLogging("GMRC", $"⚠️ No <Extrinsic> node for {deviceId} - manifest will flag as uncalibrated")
                        End If
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", $"Failed to parse Extrinsic for {deviceId}: {ex.Message}")
                    End Try

                    ' ================================================================
                    ' ✅ NEW: Hesai HTTP JSON API port (pandar.cgi), used to source
                    ' capture-manifest metadata. Parsed outside the SDK-availability
                    ' gate below because the HTTP path is pure managed code and does
                    ' not require HesaiWrapper.dll to be present.
                    ' ================================================================
                    Dim httpPortNode As XmlNode = lidarNode.SelectSingleNode("HesaiConfig")
                    If httpPortNode Is Nothing Then
                        httpPortNode = lidarDevicesNode.SelectSingleNode("HesaiConfig")
                    End If
                    If httpPortNode IsNot Nothing Then
                        Dim parsedHttpPort As Integer
                        If Integer.TryParse(httpPortNode.SelectSingleNode("HttpPort")?.InnerText, parsedHttpPort) AndAlso parsedHttpPort > 0 Then
                            device.HesaiHttpPort = parsedHttpPort
                        End If
                    End If

                    ' ================================================================
                    ' ✅ NEW: Register with Hesai SDK (simple or extended config)
                    ' ================================================================
                    If HesaiInterop.IsAvailable() Then
                        ' ✅ FIX: <HesaiConfig> may be nested per-<Lidar> (device-specific
                        ' override) or a single sibling node shared by all devices under
                        ' <LidarDevices> (the common case in config.xml today). Prefer the
                        ' per-device node when present, otherwise fall back to the shared one.
                        Dim hesaiConfigNode As XmlNode = lidarNode.SelectSingleNode("HesaiConfig")
                        If hesaiConfigNode Is Nothing Then
                            hesaiConfigNode = lidarDevicesNode.SelectSingleNode("HesaiConfig")
                        End If

                        If hesaiConfigNode IsNot Nothing Then
                            device.HesaiConfig = New HesaiInterop.HesaiDeviceConfig With {
                                .device_id = deviceId,
                                .ip_address = ipAddress,
                                .data_port = dataPort,
                                .correction_file_path = hesaiConfigNode.SelectSingleNode("CorrectionFilePath")?.InnerText,
                                .firetimes_path = hesaiConfigNode.SelectSingleNode("FiretimesPath")?.InnerText,
                                .host_ip_address = hesaiConfigNode.SelectSingleNode("HostIpAddress")?.InnerText,
                                .multicast_ip_address = hesaiConfigNode.SelectSingleNode("MulticastIpAddress")?.InnerText,
                                .ptc_port = Integer.Parse(If(hesaiConfigNode.SelectSingleNode("PtcPort")?.InnerText, "9347")),
                                .use_ptc_connected = Boolean.Parse(If(hesaiConfigNode.SelectSingleNode("UsePtcConnected")?.InnerText, "False")),
                                .enable_parser_thread = Boolean.Parse(If(hesaiConfigNode.SelectSingleNode("EnableParserThread")?.InnerText, "True")),
                                .enable_udp_thread = Boolean.Parse(If(hesaiConfigNode.SelectSingleNode("EnableUdpThread")?.InnerText, "True"))
                            }
                            device.HasHesaiConfig = True
                        Else
                            device.HesaiConfig = HesaiInterop.CreateDefaultConfig(deviceId, ipAddress, dataPort)
                            device.HasHesaiConfig = True
                        End If
                        HandleUserMessageLogging("GMRC", $"✅ Hesai config stored for {deviceId} (lazy registration)")
                        ' ❌ DON'T register here - happens in LidarDevice.StartCapture()
                    End If

                    deviceIndex += 1

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Failed to parse LiDAR node at index {deviceIndex}: {ex.Message}")
                End Try
            Next

            ' ================================================================
            ' Link active time sync provider to all loaded devices
            ' ================================================================
            If MyTimeSyncProvider IsNot Nothing Then
                For Each device In LidarDevices
                    device.SetTimeSyncProvider(MyTimeSyncProvider)
                Next
            End If

            HandleUserMessageLogging("GMRC", $"LiDAR config loaded: {LidarDevices.Count} devices")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ReadLidarConfiguration failed: {ex.Message}")
        End Try
    End Sub


    ''' <summary>
    ''' Helper method to create a default LiDAR device from legacy config values
    ''' </summary>
    Private Sub CreateDefaultLidarDevice(adapterGuid As String, ipAddress As String, dataPort As Integer, imuPort As Integer)
        LidarDevices.Clear()

        Dim defaultDevice As New LidarDevice With {
        .DeviceId = "LiDAR1",
        .LidarAdapterGuid = adapterGuid,
        .LidarIpAddress = ipAddress,
        .LidarDataPort = CUShort(dataPort),
        .LidarImuPort = CUShort(imuPort),
        .Enabled = True
    }

        LidarDevices.Add(defaultDevice)
        HandleUserMessageLogging("GMRC", $"Created default LiDAR device: IP={ipAddress}")
    End Sub

    Public Sub WriteUserConfigFile(ByVal filename As String)
        ' This routine writes data to the User specific config.xml file.
        ' It now acts as a simple wrapper around the centralized WriteConfiguration helper.
        Dim filePath As String = Path.Combine(My.Application.Info.DirectoryPath, filename)
        HandleUserMessageLogging("GMRC", $"WriteUserConfigFile: Writing to {filePath}...")
        WriteConfiguration(filePath)
    End Sub

    Public Sub WriteConfiguration(ByVal filePath As String)
        ' Centralized helper to write any XML-based configuration file.

        ' Validate essential configurations before writing
        If String.IsNullOrEmpty(INCADatabase) OrElse
       String.IsNullOrEmpty(INCAWorkspace) OrElse
       String.IsNullOrEmpty(INCAExperiment) OrElse
       String.IsNullOrEmpty(INCAVariableFile) Then

            HandleUserMessageLogging("GMRC", "WriteConfiguration: Cannot write config file - essential values are missing.")
            Return
        End If

        Try
            Dim master As New XmlDocument()
            master.Load(Path.Combine(My.Application.Info.DirectoryPath, "config.xml"))
            Dim existing As XmlDocument = CType(master.CloneNode(True), XmlDocument)
            If File.Exists(filePath) Then existing.Load(filePath)
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

                writer.WriteElementString("INCADatabase", INCADatabase)
                writer.WriteElementString("INCAWorkspace", INCAWorkspace)
                writer.WriteElementString("INCAExperiment", InitialINCAExperiment)
                writer.WriteElementString("INCAVariableFile", INCAVariableFile)
                writer.WriteElementString("ETAS_USER_PATH", ETAS_USER_PATH)
                writer.WriteElementString("SignalRegistrationMode", SignalRegistrationMode)

                ' ════════════════════════════════════════════════════════════
                ' RECORDING CONFIGURATION SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Recording Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("RecordWAVTime", RecordWAVTime.ToString())
                writer.WriteElementString("RecordFileDurationMinutes", RecordFileDurationMinutes.ToString())
                writer.WriteElementString("MuteVoiceRecordingMessages", MuteVoiceRecordingMessages.ToString())
                writer.WriteElementString("AudioToTextConversion", AudioToTextConversion.ToString())

                ' ════════════════════════════════════════════════════════════
                ' ALTERNATE RECORDING SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Alternate Recording (CANalyzer/VehicleSpy) ")
                writer.WriteComment(" Valid AlternateRecordConfig values: VehicleSpy, CANalyzer, None ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("AlternateRecordEnabled", AlternateRecordEnabled.ToString())
                writer.WriteElementString("AlternateRecordConfig", AlternateRecordConfig)
                writer.WriteElementString("EnableAltRecReStartAfterRecordStop", EnableAltRecReStartAfterRecordStop.ToString())

                ' ════════════════════════════════════════════════════════════
                ' DATA STORAGE & NETWORK SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Data Storage & Network ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("BaseDataCollectionPath", BaseDataCollectionPath)

                writer.WriteElementString("NetworkDriveLetter", NetworkDriveLetter)

                writer.WriteElementString("NetworkDriveMapping", NetworkDriveMapping)

                ' ════════════════════════════════════════════════════════════
                ' LOCAL TOOL & SCRIPT PATHS SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Local Tool & Script Paths ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("CSVScriptsPath", CSVScriptsPath)
                writer.WriteElementString("SevenZipPath", SevenZipPath)

                ' ════════════════════════════════════════════════════════════
                ' COMPRESSION CONFIGURATION SECTION (NESTED)
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Compression Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteStartElement("Compression")

                ' File type compression switches
                writer.WriteComment(" File type compression switches ")
                writer.WriteElementString("CompressMF4", CompressMF4.ToString())
                writer.WriteElementString("CompressPCAP", CompressPCAP.ToString())
                writer.WriteElementString("CompressASC", CompressASC.ToString())
                writer.WriteElementString("CompressVSB", CompressVSB.ToString())

                ' Deletion policy
                writer.WriteComment(" Deletion policy (SAFE DEFAULT = False) ")
                writer.WriteElementString("DeleteAfterCompression", DeleteAfterCompression.ToString())

                ' Compression parameters
                writer.WriteComment(" Compression parameters ")
                writer.WriteElementString("CompressionLevel", CompressionLevel.ToString())
                writer.WriteComment(" 1=Fastest, 9=Best ")

                ' ✅ FIX: Use simple XML names to match ReadConfiguration
                writer.WriteElementString("MaxRetries", ZipCompressionMaxRetries.ToString())
                writer.WriteElementString("RetryDelaySeconds", ZipCompressionRetryDelay.ToString())

                ' Lock detection
                writer.WriteComment(" Lock detection ")
                writer.WriteElementString("FileLockTimeoutSeconds", ZipFileLockTimeout.ToString())

                writer.WriteEndElement() ' Close <Compression>

                ' ════════════════════════════════════════════════════════════
                ' HARDWARE CONFIGURATION SECTION
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" Hardware Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("NetworkAdapterDescription", NetworkAdapterDescription)

                ' ════════════════════════════════════════════════════════════
                ' OXTS CONFIGURATION SECTION (GPS/INS Integration)
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" OXTS GPS/INS Configuration ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteStartElement("OxtsConfiguration")

                ' Write OXTS settings
                writer.WriteComment(" Enable/disable OXTS INS integration ")
                writer.WriteElementString("OxtsEnabled", OxtsEnabled.ToString())

                writer.WriteComment(" NCOM UDP listener settings ")
                writer.WriteElementString("NcomIpAddress", OxtsNcomIpAddress)
                writer.WriteElementString("NcomPort", OxtsNcomPort.ToString())

                writer.WriteComment(" GPS lock behavior ")
                writer.WriteElementString("GpsLockTimeout", OxtsGpsLockTimeout.ToString())
                writer.WriteElementString("WaitForLockOnStart", OxtsWaitForLockOnStart.ToString())

                writer.WriteEndElement() ' Close OxtsConfiguration

                writer.WriteComment(" Time Sync provider selection ")
                writer.WriteStartElement("TimeSyncConfiguration")
                writer.WriteElementString("EnableTimeSync", TimeSyncEnabled.ToString())
                writer.WriteElementString("Provider", TimeSyncProviderType)
                writer.WriteEndElement()

                writer.WriteComment(" TimeMachine Locator protocol settings ")
                writer.WriteStartElement("TimeMachineConfiguration")
                writer.WriteElementString("Enabled", TimeMachineEnabled.ToString())
                writer.WriteElementString("DeviceIp", TimeMachineIpAddress)
                writer.WriteElementString("Port", TimeMachinePort.ToString())
                writer.WriteElementString("PollMs", TimeMachinePollMs.ToString())
                writer.WriteElementString("PtpAssumeLocked", TimeMachinePtpAssumeLocked.ToString())
                writer.WriteEndElement()

                ' ════════════════════════════════════════════════════════════
                ' LIDAR CONFIGURATION SECTION (Legacy + Multi-Device)
                ' ════════════════════════════════════════════════════════════
                writer.WriteComment(" ================================================================ ")
                writer.WriteComment(" LiDAR Configuration (Legacy + Multi-Device) ")
                writer.WriteComment(" ================================================================ ")

                writer.WriteElementString("LidarCaptureEnabled", LidarCaptureEnabled.ToString())

                ' Write legacy single-device tags for backward compatibility
                writer.WriteComment(" Legacy single-device (backward compatibility) ")
                If LidarDevices.Count > 0 Then
                    writer.WriteElementString("LidarAdapterGuid", LidarDevices(0).LidarAdapterGuid)
                    writer.WriteElementString("LidarIpAddress", LidarDevices(0).LidarIpAddress)
                    writer.WriteElementString("LidarDataPort", LidarDevices(0).LidarDataPort.ToString())
                    writer.WriteElementString("LidarImuPort", LidarDevices(0).LidarImuPort.ToString())
                End If

                ' Write new multi-device structure
                writer.WriteComment(" Multi-device configuration ")
                writer.WriteStartElement("LidarDevices")

                If LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 Then
                    For i As Integer = 0 To LidarDevices.Count - 1
                        ' ✅ CRITICAL: Write device even if Enabled=False
                        writer.WriteStartElement("Lidar")
                        writer.WriteAttributeString("id", (i + 1).ToString())
                        writer.WriteAttributeString("enabled", LidarDevices(i).Enabled.ToString())

                        writer.WriteElementString("AdapterGuid", LidarDevices(i).LidarAdapterGuid)
                        writer.WriteElementString("IpAddress", LidarDevices(i).LidarIpAddress)
                        writer.WriteElementString("DataPort", LidarDevices(i).LidarDataPort.ToString())
                        writer.WriteElementString("ImuPort", LidarDevices(i).LidarImuPort.ToString())

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

                writer.WriteElementString("APICommErrorMsgDelayTime", APICommErrorMsgDelayTime.ToString())

                writer.WriteEndElement() ' Close root element
                writer.WriteEndDocument()
            End Using

            Dim updates As New XmlDocument()
            updates.LoadXml(generated.ToString())
            IncaConfiguration.MergeRuntimeSettings(existing, updates, master)
            existing.Save(filePath)
            HandleUserMessageLogging("GMRC", $"WriteConfiguration: Configuration file written to {filePath}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"WriteConfiguration: Error writing to '{filePath}' - {ex.Message}")
        End Try
    End Sub

End Class
