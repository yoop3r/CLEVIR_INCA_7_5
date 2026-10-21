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
        Return ConfigValueReader.ReadBoolean(parentNode, nodeName, defaultValue, AddressOf LogConfigMessage)
    End Function

    ''' <summary>
    ''' Safely reads a String config value from XML with logging.
    ''' </summary>
    Private Function ReadStringConfig(parentNode As XmlNode,
                                      nodeName As String,
                                      defaultValue As String) As String
        Return ConfigValueReader.ReadString(parentNode, nodeName, defaultValue, AddressOf LogConfigMessage)
    End Function

    ''' <summary>
    ''' Safely reads an Integer config value from XML with logging.
    ''' </summary>
    Private Function ReadIntegerConfig(parentNode As XmlNode,
                                       nodeName As String,
                                       defaultValue As Integer) As Integer
        Return ConfigValueReader.ReadInteger(parentNode, nodeName, defaultValue, AddressOf LogConfigMessage)
    End Function

    Private Shared Sub LogConfigMessage(message As String)
        HandleUserMessageLogging("GMRC", message)
    End Sub
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
            ' Unrecognized modes are normalized to DISPLAYS
            Dim signalRegMode = ReadStringConfig(root, "SignalRegistrationMode", "DISPLAYS").ToUpper()
            SignalRegistrationMode = ResidentConfigRules.NormalizeSignalRegistrationMode(signalRegMode)
            SaveSignalRegistrationMode = SignalRegistrationMode

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

            If ResidentConfigRules.ShouldFallBackToOxts(TimeSyncProviderType, TimeMachineEnabled) Then
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
                            Dim extrinsic As PcapEventBridge.ExtrinsicRecord = LidarExtrinsicParser.Parse(extrinsicNode)
                            device.ExtrinsicConfig = extrinsic
                            device.HasExtrinsicConfig = True

                            If extrinsic.IsCalibrated Then
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
            Dim generatedXml As String = ResidentConfigXmlWriter.BuildRuntimeSettingsXml(CaptureConfigSnapshot())

            Dim updates As New XmlDocument()
            updates.LoadXml(generatedXml)
            IncaConfiguration.MergeRuntimeSettings(existing, updates, master)
            existing.Save(filePath)
            HandleUserMessageLogging("GMRC", $"WriteConfiguration: Configuration file written to {filePath}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"WriteConfiguration: Error writing to '{filePath}' - {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Copies the globals persisted by WriteConfiguration into a ResidentConfigSnapshot.
    ''' </summary>
    Private Function CaptureConfigSnapshot() As ResidentConfigSnapshot
        Return New ResidentConfigSnapshot With {
            .INCADatabase = INCADatabase,
            .INCAWorkspace = INCAWorkspace,
            .InitialINCAExperiment = InitialINCAExperiment,
            .INCAVariableFile = INCAVariableFile,
            .ETAS_USER_PATH = ETAS_USER_PATH,
            .SignalRegistrationMode = SignalRegistrationMode,
            .RecordWAVTime = RecordWAVTime,
            .RecordFileDurationMinutes = RecordFileDurationMinutes,
            .MuteVoiceRecordingMessages = MuteVoiceRecordingMessages,
            .AudioToTextConversion = AudioToTextConversion,
            .AlternateRecordEnabled = AlternateRecordEnabled,
            .AlternateRecordConfig = AlternateRecordConfig,
            .EnableAltRecReStartAfterRecordStop = EnableAltRecReStartAfterRecordStop,
            .BaseDataCollectionPath = BaseDataCollectionPath,
            .NetworkDriveLetter = NetworkDriveLetter,
            .NetworkDriveMapping = NetworkDriveMapping,
            .CSVScriptsPath = CSVScriptsPath,
            .SevenZipPath = SevenZipPath,
            .CompressMF4 = CompressMF4,
            .CompressPCAP = CompressPCAP,
            .CompressASC = CompressASC,
            .CompressVSB = CompressVSB,
            .DeleteAfterCompression = DeleteAfterCompression,
            .CompressionLevel = CompressionLevel,
            .ZipCompressionMaxRetries = ZipCompressionMaxRetries,
            .ZipCompressionRetryDelay = ZipCompressionRetryDelay,
            .ZipFileLockTimeout = ZipFileLockTimeout,
            .NetworkAdapterDescription = NetworkAdapterDescription,
            .OxtsEnabled = OxtsEnabled,
            .OxtsNcomIpAddress = OxtsNcomIpAddress,
            .OxtsNcomPort = OxtsNcomPort,
            .OxtsGpsLockTimeout = OxtsGpsLockTimeout,
            .OxtsWaitForLockOnStart = OxtsWaitForLockOnStart,
            .TimeSyncEnabled = TimeSyncEnabled,
            .TimeSyncProviderType = TimeSyncProviderType,
            .TimeMachineEnabled = TimeMachineEnabled,
            .TimeMachineIpAddress = TimeMachineIpAddress,
            .TimeMachinePort = TimeMachinePort,
            .TimeMachinePollMs = TimeMachinePollMs,
            .TimeMachinePtpAssumeLocked = TimeMachinePtpAssumeLocked,
            .LidarCaptureEnabled = LidarCaptureEnabled,
            .LidarDevices = LidarDevices,
            .APICommErrorMsgDelayTime = APICommErrorMsgDelayTime
        }
    End Function

End Class
