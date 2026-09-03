Option Strict On
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading

''' <summary>
''' ✅ Managed client for the Hesai LiDAR HTTP JSON API (pandar.cgi).
'''
''' Replaces the PTC (TCP 9347) queries previously used to source capture-manifest
''' metadata. PTC proved unusable in practice: the Pandar128E3X PTC server accepts
''' only one session at a time, is highly sensitive to connection sequencing, and
''' returned "invalid input parameter" / ret=-1 for most commands against live units.
'''
''' The HTTP API is stateless, has no single-session constraint, requires no native
''' interop, and exposes strictly more metadata than PTC did.
'''
''' All endpoints follow the pattern:
'''   http://{ip}:{port}/pandar.cgi?action=get&amp;object={object}[&amp;key={key}]
''' and return an envelope:
'''   {"Head":{"ErrorCode":"0","Message":"Success"},"Body":{ ... }}
'''
''' NOTE: Body values are JSON *strings* even when semantically numeric
''' (e.g. "LaserNum":"128"), so all numeric reads go through tolerant helpers.
''' </summary>
Public Class HesaiHttpApi

    ' ====================================================================
    ' Shared HttpClient
    ' ====================================================================

    ''' <summary>
    ''' Single reusable HttpClient. Metadata queries must never block capture
    ''' shutdown, so the timeout is deliberately short — a missing manifest field
    ''' is always preferable to a hung stop sequence.
    ''' </summary>
    Private Shared ReadOnly _client As New HttpClient() With {
        .Timeout = TimeSpan.FromSeconds(5)
    }

    ''' <summary>Default HTTP port for the pandar.cgi API.</summary>
    Public Const DefaultHttpPort As Integer = 80

    ' ====================================================================
    ' Result Types
    ' ====================================================================

    ''' <summary>
    ''' Bundled metadata read from the LiDAR over HTTP. Each section is Nothing if
    ''' its endpoint failed or was unavailable, so callers can degrade gracefully
    ''' and record accurate provenance in the manifest.
    ''' </summary>
    Public Class HesaiHttpMetadata
        ''' <summary>True if at least one endpoint responded successfully.</summary>
        Public Property AnySucceeded As Boolean

        ''' <summary>Human-readable list of endpoints that failed, for logging.</summary>
        Public Property FailedObjects As New List(Of String)

        Public Property Device As PcapEventBridge.DeviceIdentity
        Public Property Configuration As PcapEventBridge.HesaiDeviceConfiguration
        Public Property Status As PcapEventBridge.LiveStatusSnapshot

        ''' <summary>Angle correction CSV text, exactly as returned by lidar_calibration.</summary>
        Public Property AngleCorrectionCsv As String
    End Class

    ' ====================================================================
    ' Core Query Helper
    ' ====================================================================

    ''' <summary>
    ''' Issues one GET against pandar.cgi, validates the Head.ErrorCode envelope,
    ''' and returns the Body element. Returns False on any transport, HTTP, JSON,
    ''' or device-reported error — callers treat a failure as "field unavailable"
    ''' rather than fatal.
    ''' </summary>
    ''' <param name="ipAddress">LiDAR IP address</param>
    ''' <param name="port">HTTP port (0 = default 80)</param>
    ''' <param name="objectName">The 'object' query parameter</param>
    ''' <param name="key">Optional 'key' query parameter</param>
    ''' <param name="body">Receives a clone of the Body element on success</param>
    Private Shared Function TryQueryObject(ipAddress As String,
                                           port As Integer,
                                           objectName As String,
                                           key As String,
                                           ByRef body As JsonElement) As Boolean
        Dim url As String = Nothing
        Try
            Dim effectivePort As Integer = If(port > 0, port, DefaultHttpPort)
            Dim portSuffix As String = If(effectivePort = 80, "", $":{effectivePort}")
            url = $"http://{ipAddress}{portSuffix}/pandar.cgi?action=get&object={objectName}"
            If Not String.IsNullOrEmpty(key) Then
                url &= $"&key={key}"
            End If

            ' GetAwaiter().GetResult() rather than .Result so the original
            ' exception surfaces instead of an AggregateException wrapper. The
            ' manifest path runs on a background/shutdown thread, not the UI
            ' thread, so blocking here is acceptable and keeps callers simple.
            Dim response As HttpResponseMessage = _client.GetAsync(url).GetAwaiter().GetResult()
            If Not response.IsSuccessStatusCode Then
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai HTTP: {objectName} returned HTTP {CInt(response.StatusCode)} from {ipAddress}")
                Return False
            End If

            Dim json As String = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            If String.IsNullOrWhiteSpace(json) Then
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai HTTP: {objectName} returned empty response from {ipAddress}")
                Return False
            End If

            Using doc As JsonDocument = JsonDocument.Parse(json)
                Dim root As JsonElement = doc.RootElement

                ' Validate the {"Head":{"ErrorCode":"0",...}} envelope.
                Dim head As JsonElement
                If root.TryGetProperty("Head", head) Then
                    Dim errorCode As String = ReadString(head, "ErrorCode")
                    If Not String.IsNullOrEmpty(errorCode) AndAlso errorCode <> "0" Then
                        Dim message As String = ReadString(head, "Message")
                        HandleUserMessageLogging("GMRC", $"⚠️ Hesai HTTP: {objectName} error {errorCode} ({message}) from {ipAddress}")
                        Return False
                    End If
                End If

                Dim bodyElement As JsonElement
                If Not root.TryGetProperty("Body", bodyElement) Then
                    HandleUserMessageLogging("GMRC", $"⚠️ Hesai HTTP: {objectName} response had no Body from {ipAddress}")
                    Return False
                End If

                ' Clone: the JsonDocument is disposed when this Using block exits.
                body = bodyElement.Clone()
                Return True
            End Using

        Catch ex As TaskCanceledException
            HandleUserMessageLogging("GMRC", $"⚠️ Hesai HTTP: {objectName} timed out for {ipAddress}")
            Return False
        Catch ex As HttpRequestException
            HandleUserMessageLogging("GMRC", $"⚠️ Hesai HTTP: {objectName} unreachable at {ipAddress} ({ex.Message})")
            Return False
        Catch ex As JsonException
            HandleUserMessageLogging("GMRC", $"⚠️ Hesai HTTP: {objectName} returned malformed JSON from {ipAddress} ({ex.Message})")
            Return False
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiHttpApi.TryQueryObject({objectName}) [{url}]: {ex.Message}")
            Return False
        End Try
    End Function

    ' ====================================================================
    ' Tolerant Value Readers
    ' ====================================================================

    ''' <summary>
    ''' Reads a property as a string, accepting either a JSON string or a JSON
    ''' number. Returns Nothing if absent.
    ''' </summary>
    Private Shared Function ReadString(element As JsonElement, propertyName As String) As String
        Dim prop As JsonElement
        If Not element.TryGetProperty(propertyName, prop) Then Return Nothing

        Select Case prop.ValueKind
            Case JsonValueKind.String
                Return prop.GetString()
            Case JsonValueKind.Number
                Return prop.GetRawText()
            Case JsonValueKind.True
                Return "true"
            Case JsonValueKind.False
                Return "false"
            Case Else
                Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' Reads a property as an Integer. The Hesai API returns numbers as strings,
    ''' so this handles both representations. Returns the supplied default if the
    ''' property is absent or unparseable.
    ''' </summary>
    Private Shared Function ReadInt(element As JsonElement, propertyName As String, Optional defaultValue As Integer = 0) As Integer
        Dim raw As String = ReadString(element, propertyName)
        If String.IsNullOrWhiteSpace(raw) Then Return defaultValue

        Dim result As Integer
        If Integer.TryParse(raw.Trim(), result) Then Return result
        Return defaultValue
    End Function

    ''' <summary>Reads a property as a Long, tolerating string-wrapped numbers.</summary>
    Private Shared Function ReadLong(element As JsonElement, propertyName As String, Optional defaultValue As Long = 0) As Long
        Dim raw As String = ReadString(element, propertyName)
        If String.IsNullOrWhiteSpace(raw) Then Return defaultValue

        Dim result As Long
        If Long.TryParse(raw.Trim(), result) Then Return result
        Return defaultValue
    End Function

    ''' <summary>Reads a property as a Single, tolerating string-wrapped numbers.</summary>
    Private Shared Function ReadFloat(element As JsonElement, propertyName As String, Optional defaultValue As Single = 0.0F) As Single
        Dim raw As String = ReadString(element, propertyName)
        If String.IsNullOrWhiteSpace(raw) Then Return defaultValue

        Dim result As Single
        If Single.TryParse(raw.Trim(), Globalization.NumberStyles.Float,
                           Globalization.CultureInfo.InvariantCulture, result) Then
            Return result
        End If
        Return defaultValue
    End Function

    ''' <summary>
    ''' Reads a property whose value is "1"/"true"/"yes" as True. The Hesai API is
    ''' inconsistent about which representation it uses per field.
    ''' </summary>
    Private Shared Function ReadBool(element As JsonElement, propertyName As String) As Boolean
        Dim raw As String = ReadString(element, propertyName)
        If String.IsNullOrWhiteSpace(raw) Then Return False

        Select Case raw.Trim().ToLowerInvariant()
            Case "1", "true", "yes", "on", "lock", "locked"
                Return True
            Case Else
                Return False
        End Select
    End Function

    ''' <summary>
    ''' Returns the first present property from a list of candidate names. Used
    ''' where the exact key name varies by firmware/endpoint and has not been
    ''' confirmed against a live unit for every field.
    ''' </summary>
    Private Shared Function ReadFirstString(element As JsonElement, ParamArray propertyNames As String()) As String
        For Each name In propertyNames
            Dim value As String = ReadString(element, name)
            If Not String.IsNullOrWhiteSpace(value) Then Return value
        Next
        Return Nothing
    End Function

    ' ── Nullable readers ─────────────────────────────────────────────────
    ' These return Nothing when a key is absent or unparseable, so a missing
    ' field serializes as null rather than a misleading 0/false. Downstream
    ' ingestion must be able to tell "not reported" from "measured zero".

    ''' <summary>Reads an Integer, or Nothing when absent/unparseable.</summary>
    Private Shared Function ReadIntOrNull(element As JsonElement, propertyName As String) As Integer?
        Dim raw As String = ReadString(element, propertyName)
        If String.IsNullOrWhiteSpace(raw) Then Return Nothing

        Dim result As Integer
        If Integer.TryParse(raw.Trim(), result) Then Return result
        Return Nothing
    End Function

    ''' <summary>Reads a Long, or Nothing when absent/unparseable.</summary>
    Private Shared Function ReadLongOrNull(element As JsonElement, propertyName As String) As Long?
        Dim raw As String = ReadString(element, propertyName)
        If String.IsNullOrWhiteSpace(raw) Then Return Nothing

        Dim result As Long
        If Long.TryParse(raw.Trim(), result) Then Return result
        Return Nothing
    End Function

    ''' <summary>Reads a Single, or Nothing when absent/unparseable.</summary>
    Private Shared Function ReadFloatOrNull(element As JsonElement, propertyName As String) As Single?
        Dim raw As String = ReadString(element, propertyName)
        If String.IsNullOrWhiteSpace(raw) Then Return Nothing

        Dim result As Single
        If Single.TryParse(raw.Trim(), Globalization.NumberStyles.Float,
                           Globalization.CultureInfo.InvariantCulture, result) Then
            Return result
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Reads a "0"/"1" style flag, or Nothing when the key is absent. Required so
    ''' that an unreported GPS lock is not indistinguishable from "not locked".
    ''' </summary>
    Private Shared Function ReadBoolOrNull(element As JsonElement, propertyName As String) As Boolean?
        Dim raw As String = ReadString(element, propertyName)
        If String.IsNullOrWhiteSpace(raw) Then Return Nothing

        Select Case raw.Trim().ToLowerInvariant()
            Case "1", "true", "yes", "on", "lock", "locked"
                Return True
            Case "0", "false", "no", "off", "unlock", "unlocked"
                Return False
            Case Else
                Return Nothing
        End Select
    End Function

    ' ====================================================================
    ' Public API
    ' ====================================================================

    ''' <summary>
    ''' ✅ Queries all manifest-relevant metadata from the LiDAR over HTTP.
    '''
    ''' Each endpoint is queried independently and failures are isolated, so a
    ''' partial result is returned when only some objects are available. This is
    ''' the intended replacement for HesaiInterop.GetManifestInfo (PTC).
    ''' </summary>
    ''' <param name="ipAddress">LiDAR IP address</param>
    ''' <param name="httpPort">HTTP port (0 = default 80)</param>
    ''' <returns>Populated metadata, or Nothing if the device was entirely unreachable</returns>
    Public Shared Function GetMetadata(ipAddress As String, Optional httpPort As Integer = 0) As HesaiHttpMetadata
        If String.IsNullOrWhiteSpace(ipAddress) Then Return Nothing

        Dim result As New HesaiHttpMetadata()

        ' ── Device identity: device_info ──────────────────────────────────
        Dim deviceBody As JsonElement = Nothing
        If TryQueryObject(ipAddress, httpPort, "device_info", Nothing, deviceBody) Then
            result.Device = MapDeviceIdentity(deviceBody)
            result.AnySucceeded = True
        Else
            result.FailedObjects.Add("device_info")
        End If

        ' ── Configuration: lidar_config (+ lidar_mode for return mode) ────
        ' lidar_config also carries GPS lock and PTP status on this firmware, so
        ' it feeds the status snapshot as well as the configuration record.
        Dim configBody As JsonElement = Nothing
        If TryQueryObject(ipAddress, httpPort, "lidar_config", Nothing, configBody) Then
            result.Configuration = MapConfiguration(configBody)
            result.Status = EnsureStatus(result)
            ApplyConfigStatus(configBody, result.Status)
            result.AnySucceeded = True
        Else
            result.FailedObjects.Add("lidar_config")
        End If

        ' Return mode lives on its own endpoint; fold it into Configuration.
        Dim modeBody As JsonElement = Nothing
        If TryQueryObject(ipAddress, httpPort, "lidar_data", "lidar_mode", modeBody) Then
            If result.Configuration Is Nothing Then
                result.Configuration = New PcapEventBridge.HesaiDeviceConfiguration With {.Source = "Http"}
            End If
            Dim returnMode As Integer? = ReadIntOrNull(modeBody, "lidar_mode")
            If Not returnMode.HasValue Then returnMode = ReadIntOrNull(modeBody, "return_mode")
            If returnMode.HasValue Then
                result.Configuration.ReturnMode = returnMode
                result.Configuration.ReturnModeName = DecodeReturnMode(returnMode)
            End If
            result.AnySucceeded = True
        Else
            result.FailedObjects.Add("lidar_data&key=lidar_mode")
        End If

        ' ── Sync state: lidar_sync ────────────────────────────────────
        ' Confirmed live shape: {"sync":"0","syncAngle":"0"}
        Dim syncBody As JsonElement = Nothing
        If TryQueryObject(ipAddress, httpPort, "lidar_sync", Nothing, syncBody) Then
            If result.Configuration Is Nothing Then
                result.Configuration = New PcapEventBridge.HesaiDeviceConfiguration With {.Source = "Http"}
            End If
            result.Configuration.SyncEnabled = ReadBoolOrNull(syncBody, "sync")
            result.Configuration.SyncAngleHundredthsOfDegree = ReadIntOrNull(syncBody, "syncAngle")
            result.AnySucceeded = True
        Else
            result.FailedObjects.Add("lidar_sync")
        End If

        ' ── Live status: lidar_monitor (+ TimeStatistic for uptime) ───────
        Dim monitorBody As JsonElement = Nothing
        If TryQueryObject(ipAddress, httpPort, "lidar_monitor", Nothing, monitorBody) Then
            result.Status = EnsureStatus(result)
            MapStatus(monitorBody, result.Status)
            result.AnySucceeded = True
        Else
            result.FailedObjects.Add("lidar_monitor")
        End If

        Dim timeStatBody As JsonElement = Nothing
        If TryQueryObject(ipAddress, httpPort, "TimeStatistic", Nothing, timeStatBody) Then
            result.Status = EnsureStatus(result)
            MapTimeStatistics(timeStatBody, result.Status)
            result.AnySucceeded = True
        Else
            result.FailedObjects.Add("TimeStatistic")
        End If

        ' ── PTP lock offset: PTP_lock_offset ──────────────────────────────
        Dim ptpBody As JsonElement = Nothing
        If TryQueryObject(ipAddress, httpPort, "PTP_lock_offset", Nothing, ptpBody) Then
            result.Status = EnsureStatus(result)
            result.Status.PtpLockOffsetLimit = ReadString(ptpBody, "PTP_lock_offset")
            result.AnySucceeded = True
        Else
            result.FailedObjects.Add("PTP_lock_offset")
        End If

        ' ── Angle correction: lidar_calibration ───────────────────────────
        Dim calibBody As JsonElement = Nothing
        If TryQueryObject(ipAddress, httpPort, "lidar_calibration", Nothing, calibBody) Then
            result.AngleCorrectionCsv = ReadFirstString(calibBody, "lidar_calibration", "calibration", "LidarCalibration")
            result.AnySucceeded = True
        Else
            result.FailedObjects.Add("lidar_calibration")
        End If

        If Not result.AnySucceeded Then
            HandleUserMessageLogging("GMRC", $"⚠️ Hesai HTTP: no endpoints responded at {ipAddress} - device unreachable?")
            Return Nothing
        End If

        If result.FailedObjects.Count > 0 Then
            HandleUserMessageLogging("GMRC",
                $"⚠️ Hesai HTTP {ipAddress}: {result.FailedObjects.Count} endpoint(s) unavailable ({String.Join(", ", result.FailedObjects)})")
        End If

        Return result
    End Function

    ''' <summary>
    ''' ✅ Lightweight reachability probe. Queries device_info only, so it is
    ''' suitable for UI-driven connectivity checks before a full manifest run.
    ''' </summary>
    Public Shared Function IsReachable(ipAddress As String, Optional httpPort As Integer = 0) As Boolean
        Dim body As JsonElement = Nothing
        Return TryQueryObject(ipAddress, httpPort, "device_info", Nothing, body)
    End Function

    ' ====================================================================
    ' Endpoint Mappings
    ' ====================================================================

    ''' <summary>
    ''' Returns the existing status snapshot, creating it on first use. Several
    ''' endpoints contribute to a single snapshot, and any of them may be the
    ''' first to succeed.
    ''' </summary>
    Private Shared Function EnsureStatus(result As HesaiHttpMetadata) As PcapEventBridge.LiveStatusSnapshot
        If result.Status Is Nothing Then
            result.Status = New PcapEventBridge.LiveStatusSnapshot With {.Source = "Http"}
        End If
        Return result.Status
    End Function

    ''' <summary>
    ''' Maps the device_info body onto DeviceIdentity. Field names follow the
    ''' live sample response from a Pandar128E3X (SW_Ver / FW_Ver / Up_Fpga_Ver etc.).
    ''' </summary>
    Private Shared Function MapDeviceIdentity(body As JsonElement) As PcapEventBridge.DeviceIdentity
        Return New PcapEventBridge.DeviceIdentity With {
            .SerialNumber = ReadString(body, "SN"),
            .DateOfManufacture = ReadString(body, "ProdDate"),
            .Model = ReadFirstString(body, "ProdName", "Model"),
            .NumberOfChannels = ReadInt(body, "LaserNum"),
            .SoftwareVersion = ReadString(body, "SW_Ver"),
            .ControlFirmwareVersion = ReadString(body, "FW_Ver"),
            .SensorFirmwareVersion = ReadFirstString(body, "Up_Fpga_Ver", "FPGA_Ver"),
            .PartNumber = ReadString(body, "PN"),
            .MacAddress = ReadString(body, "Mac"),
            .HardwareVersion = ReadString(body, "HW_Ver"),
            .BootVersion = ReadString(body, "BOOT_Ver"),
            .AngleOffset = ReadInt(body, "AngleOffset"),
            .MotorType = ReadInt(body, "MotorType"),
            .MotorTypeName = DecodeMotorType(ReadIntOrNull(body, "MotorType")),
            .UdpSequenceEnabled = ReadBool(body, "Udp_Seq"),
            .LidarDataFormat = ReadInt(body, "LidarDataFormat"),
            .Source = "Http"
        }
    End Function

    ''' <summary>
    ''' Maps the lidar_config body onto HesaiDeviceConfiguration.
    '''
    ''' Key names are confirmed against live Pandar128E3X firmware 1.55.x, which
    ''' returns every value as a string:
    '''   {"SpinSpeed":"2","DestIp":"239.192.20.10","DestPort":"2368","GPS":"0",
    '''    "GPS_GPRMC":"0","ClockSource":"1","PTPStatus":"Locked (offset: 16282 ns)",
    '''    "RotateDirection":"0","NoiseFiltering":"0","ReflectivityMapping":"0",
    '''    "GpsPort":"10110","ClockDataFormat":"0","PTPProfile":"0",
    '''    "InterstitialPoints":"0","RetroMultiReflection":"0"}
    '''
    ''' There is no return-mode, standby-mode or trigger-method key in this
    ''' payload; those come from their own endpoints and stay null otherwise.
    ''' </summary>
    Private Shared Function MapConfiguration(body As JsonElement) As PcapEventBridge.HesaiDeviceConfiguration
        Dim spinCode As Integer? = ReadIntOrNull(body, "SpinSpeed")
        Dim clockDataFormat As Integer? = ReadIntOrNull(body, "ClockDataFormat")
        Dim rotateDirection As Integer? = ReadIntOrNull(body, "RotateDirection")
        Dim ptpProfile As Integer? = ReadIntOrNull(body, "PTPProfile")

        Return New PcapEventBridge.HesaiDeviceConfiguration With {
            .ClockSource = ReadIntOrNull(body, "ClockSource"),
            .SpinSpeedCode = spinCode,
            .SpinRateRpm = DecodeSpinRateRpm(spinCode),
            .NoiseFiltering = ReadIntOrNull(body, "NoiseFiltering"),
            .ReflectivityMapping = ReadIntOrNull(body, "ReflectivityMapping"),
            .RotateDirection = rotateDirection,
            .RotateDirectionName = DecodeRotateDirection(rotateDirection),
            .DestinationIp = ReadString(body, "DestIp"),
            .DestinationPort = ReadIntOrNull(body, "DestPort"),
            .GpsPort = ReadIntOrNull(body, "GpsPort"),
            .ClockDataFormat = clockDataFormat,
            .ClockDataFormatName = DecodeClockDataFormat(clockDataFormat),
            .PtpProfile = ptpProfile,
            .PtpProfileName = DecodePtpProfile(ptpProfile),
            .PtpConfigJson = ReadString(body, "PTPConfig"),
            .InterstitialPoints = ReadIntOrNull(body, "InterstitialPoints"),
            .RetroMultiReflection = ReadIntOrNull(body, "RetroMultiReflection"),
            .Source = "Http"
        }
    End Function

    ''' <summary>
    ''' Decodes the Spin_Speed selector into RPM, per the vendor HTTP API spec:
    '''   "2" - 600 rpm, "3" - 1200 rpm
    ''' Any undocumented code returns Nothing so the manifest records no rate at
    ''' all rather than an invented one; SpinSpeedCode always preserves the raw
    ''' value so an unknown code is still recoverable downstream.
    ''' </summary>
    Private Shared Function DecodeSpinRateRpm(spinCode As Integer?) As Integer?
        If Not spinCode.HasValue Then Return Nothing

        Select Case spinCode.Value
            Case 2 : Return 600
            Case 3 : Return 1200
            Case Else : Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' Decodes the GPS NMEA data format selector, per the vendor HTTP API spec:
    '''   "0" - GPRMC, "1" - GPGGA
    ''' Any undocumented code returns Nothing; ClockDataFormat always preserves
    ''' the raw value so an unknown code remains recoverable downstream.
    ''' </summary>
    Private Shared Function DecodeClockDataFormat(formatCode As Integer?) As String
        If Not formatCode.HasValue Then Return Nothing

        Select Case formatCode.Value
            Case 0 : Return "GPRMC"
            Case 1 : Return "GPGGA"
            Case Else : Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' Decodes the return mode, per the vendor HTTP API spec:
    '''   0 - last, 1 - strongest, 2 - dual (last + strongest),
    '''   3 - first, 4 - dual (last + first), 5 - dual (strongest + first)
    ''' Any undocumented code returns Nothing; ReturnMode preserves the raw value.
    ''' </summary>
    Private Shared Function DecodeReturnMode(returnMode As Integer?) As String
        If Not returnMode.HasValue Then Return Nothing

        Select Case returnMode.Value
            Case 0 : Return "Last"
            Case 1 : Return "Strongest"
            Case 2 : Return "Dual (Last + Strongest)"
            Case 3 : Return "First"
            Case 4 : Return "Dual (Last + First)"
            Case 5 : Return "Dual (Strongest + First)"
            Case Else : Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' Decodes the motor rotation direction: "0" - clockwise, "1" - counterclockwise.
    ''' </summary>
    Private Shared Function DecodeRotateDirection(rotateDirection As Integer?) As String
        If Not rotateDirection.HasValue Then Return Nothing

        Select Case rotateDirection.Value
            Case 0 : Return "Clockwise"
            Case 1 : Return "Counterclockwise"
            Case Else : Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' Decodes the IEEE timing/synchronization standard in use:
    '''   "0" - 1588v2 (default), "1" - 802.1AS, "2" - 802.1AS Automotive
    ''' </summary>
    Private Shared Function DecodePtpProfile(ptpProfile As Integer?) As String
        If Not ptpProfile.HasValue Then Return Nothing

        Select Case ptpProfile.Value
            Case 0 : Return "IEEE 1588v2"
            Case 1 : Return "IEEE 802.1AS"
            Case 2 : Return "IEEE 802.1AS Automotive"
            Case Else : Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' Decodes the motor type: "0" - single direction, "1" - dual direction.
    ''' </summary>
    Private Shared Function DecodeMotorType(motorType As Integer?) As String
        If Not motorType.HasValue Then Return Nothing

        Select Case motorType.Value
            Case 0 : Return "SingleDirection"
            Case 1 : Return "DualDirection"
            Case Else : Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' Applies GPS lock state and PTP status from the lidar_config body onto the
    ''' status snapshot. On this firmware these live in lidar_config rather than
    ''' lidar_monitor, which is why they are handled separately from MapStatus.
    ''' </summary>
    Private Shared Sub ApplyConfigStatus(body As JsonElement, snapshot As PcapEventBridge.LiveStatusSnapshot)
        snapshot.GpsPpsLocked = ReadBoolOrNull(body, "GPS")
        snapshot.GpsGprmcLocked = ReadBoolOrNull(body, "GPS_GPRMC")

        Dim ptpStatus As String = ReadString(body, "PTPStatus")
        If String.IsNullOrWhiteSpace(ptpStatus) Then Return

        snapshot.PtpStatus = ptpStatus
        snapshot.PtpLocked = ptpStatus.IndexOf("lock", StringComparison.OrdinalIgnoreCase) >= 0

        ' PTPStatus reads e.g. "Locked (offset: 16282 ns)" - extract the offset so
        ' downstream tooling can assess sync quality numerically.
        Dim match = Text.RegularExpressions.Regex.Match(
            ptpStatus, "offset:\s*(-?\d+)", Text.RegularExpressions.RegexOptions.IgnoreCase)
        If match.Success Then
            Dim offsetNs As Long
            If Long.TryParse(match.Groups(1).Value, offsetNs) Then
                snapshot.PtpOffsetNanoseconds = offsetNs
            End If
        End If
    End Sub

    ''' <summary>
    ''' Maps the lidar_monitor body onto LiveStatusSnapshot. On Pandar128E3X this
    ''' endpoint carries only input power telemetry and phase offset:
    '''   {"lidarInCur":"1547.06 mA","lidarInVol":"11.42 V",
    '''    "lidarInPower":"17.67 W","phaseoffset":"25"}
    ''' Rail readings are kept verbatim, units included, to avoid lossy
    ''' assumptions about scaling.
    ''' </summary>
    Private Shared Sub MapStatus(body As JsonElement, snapshot As PcapEventBridge.LiveStatusSnapshot)
        snapshot.InputCurrent = ReadString(body, "lidarInCur")
        snapshot.InputVoltage = ReadString(body, "lidarInVol")
        snapshot.InputPower = ReadString(body, "lidarInPower")
        snapshot.PhaseOffset = ReadIntOrNull(body, "phaseoffset")
    End Sub

    ''' <summary>
    ''' Folds the TimeStatistic body into an existing status snapshot. Confirmed
    ''' live shape:
    '''   {"StartupTimes":"204","CurrentTemp":"50.80","CurrentHumidity":" 23.7",
    '''    "TotalWorkingTime":"96415","SystemUptime":"1623","Time0".."Time9"}
    ''' CurrentHumidity is returned with a leading space, which the readers trim.
    ''' </summary>
    Private Shared Sub MapTimeStatistics(body As JsonElement, snapshot As PcapEventBridge.LiveStatusSnapshot)
        snapshot.SystemUptimeSeconds = ReadLongOrNull(body, "SystemUptime")
        snapshot.StartupTimes = ReadLongOrNull(body, "StartupTimes")
        snapshot.TotalOperationTimeSeconds = ReadLongOrNull(body, "TotalWorkingTime")
        snapshot.TemperatureCelsius = ReadFloatOrNull(body, "CurrentTemp")
        snapshot.HumidityPercentRh = ReadFloatOrNull(body, "CurrentHumidity")
    End Sub

End Class
