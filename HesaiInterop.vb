Option Strict On
Imports System.IO
Imports System.Runtime.InteropServices

''' <summary>
''' P/Invoke wrapper for Hesai LiDAR SDK statistics
''' Maps to functions in HesaiWrapper.dll
''' </summary>
Public Class HesaiInterop

    ' ====================================================================
    ' Win32 API for DLL Search Path
    ' ====================================================================

    ''' <summary>
    ''' ✅ Sets the DLL search directory to the application folder
    ''' Call this BEFORE any other HesaiInterop methods
    ''' </summary>
    Public Shared Sub SetDllSearchPath()
        Try
            Dim appDir As String = AppDomain.CurrentDomain.BaseDirectory
            If NativeMethods.SetDllDirectory(appDir) Then
                HandleUserMessageLogging("GMRC", $"✓ DLL search path set to: {appDir}")
            Else
                HandleUserMessageLogging("GMRC", $"⚠️ Failed to set DLL search path")
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"SetDllSearchPath error: {ex.Message}")
        End Try
    End Sub

    ' ====================================================================
    ' P/Invoke Declarations
    ' ====================================================================

    ''' <summary>
    ''' Native struct matching HesaiWrapper.h HesaiSdkStats
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure HesaiSdkStats
        Public packets_received As ULong
        Public packets_dropped As ULong
        Public checksum_errors As ULong
        Public out_of_order_packets As ULong
        Public total_bytes As ULong
        Public last_packet_timestamp As Long  ' Unix timestamp (ms)
    End Structure

    ''' <summary>
    ''' ✅ Extended configuration structure matching HesaiWrapper.h HesaiDeviceConfig
    ''' Allows per-device customization of all SDK parameters
    ''' </summary>
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)>
    Public Structure HesaiDeviceConfig
        ' Required parameters
        <MarshalAs(UnmanagedType.LPStr)> Public device_id As String
        <MarshalAs(UnmanagedType.LPStr)> Public ip_address As String
        Public data_port As Integer

        ' ✅ Optional: Calibration files (Nothing/null = use SDK embedded defaults)
        <MarshalAs(UnmanagedType.LPStr)> Public correction_file_path As String
        <MarshalAs(UnmanagedType.LPStr)> Public firetimes_path As String

        ' ✅ Optional: Network configuration (Nothing/null = use defaults)
        <MarshalAs(UnmanagedType.LPStr)> Public host_ip_address As String
        <MarshalAs(UnmanagedType.LPStr)> Public multicast_ip_address As String

        ' ✅ Optional: PTC configuration
        Public ptc_port As Integer
        <MarshalAs(UnmanagedType.Bool)> Public use_ptc_connected As Boolean

        ' ✅ Optional: Threading configuration
        <MarshalAs(UnmanagedType.Bool)> Public enable_parser_thread As Boolean
        <MarshalAs(UnmanagedType.Bool)> Public enable_udp_thread As Boolean

        ' ✅ NEW: Validation-only mode (no UDP binding)
        ' When True: SDK tracks statistics WITHOUT binding to UDP port
        ' Use this when PcapDotNet handles actual packet capture
        <MarshalAs(UnmanagedType.Bool)> Public validation_only As Boolean
    End Structure

    ''' <summary>
    ''' Native struct matching HesaiWrapper.h HesaiInventoryInfo (PTC command 0x07).
    ''' Field lengths match the P128 TCP API v1.9 PTC_COMMAND_GET_INVENTORY_INFO
    ''' response payload (228 bytes on the wire; the strings here are NUL-padded
    ''' fixed-width buffers, one byte larger than the wire field to guarantee
    ''' termination).
    ''' </summary>
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)>
    Public Structure HesaiInventoryInfo
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=19)> Public sn As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=17)> Public date_of_manufacture As String
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=6)> Public mac As Byte()
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=17)> Public sw_ver As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=17)> Public hw_ver As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=17)> Public control_fw_ver As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=17)> Public sensor_fw_ver As String
        Public angle_offset As UShort
        Public model As Byte
        Public motor_type As Byte
        Public num_of_lines As Byte
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=33)> Public pn As String
        Public customer_pn_enable As Byte
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=21)> Public customer_pn As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=10)> Public duns As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=15)> Public vpps As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=17)> Public boot_ver As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=9)> Public cruise_pn As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=9)> Public gm_sw_pn As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=9)> Public gm_hw_pn As String
    End Structure

    ''' <summary>
    ''' Native struct matching HesaiWrapper.h HesaiConfigInfo (PTC command 0x08).
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure HesaiConfigInfo
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)> Public ipaddr As Byte()
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)> Public mask As Byte()
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)> Public gateway As Byte()
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)> Public dest_ipaddr As Byte()
        Public dest_lidar_udp_port As UShort
        Public dest_gps_udp_port As UShort
        Public spin_rate As UShort
        Public sync As Byte
        Public sync_angle As UShort
        Public start_angle As UShort
        Public stop_angle As UShort
        Public clock_source As Byte
        Public trigger_method As Byte
        Public return_mode As Byte
        Public standby_mode As Byte
        Public motor_status As Byte
        Public vlan_flag As Byte
        Public vlan_id As UShort
        Public clock_data_fmt As Byte
        Public noise_filtering As Byte
        Public reflectivity_mapping As Byte
    End Structure

    ''' <summary>
    ''' Native struct matching HesaiWrapper.h HesaiLidarStatus (PTC command 0x09).
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure HesaiLidarStatus
        Public system_uptime As UInteger
        Public motor_speed As UShort
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)> Public temperature As Single()
        Public gps_pps_lock As Byte
        Public gps_gprmc_status As Byte
        Public startup_times As UInteger
        Public total_operation_time As UInteger
        Public ptp_clock_status As Byte
        Public humidity As Single
    End Structure

    ' DLL path - use relative name so it finds the DLL in the same directory
    Private Const LibHesaiDll As String = "HesaiWrapper.dll"

    ' ====================================================================
    ' P/Invoke Declarations
    ' Isolated in a NativeMethods-suffixed nested class per CA1060.
    ' ====================================================================
    Private NotInheritable Class NativeMethods
        Private Sub New()
        End Sub

        <DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Public Shared Function SetDllDirectory(lpPathName As String) As Boolean
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_register_device(
            <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
            <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
            dataPort As Integer
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl)>
        Public Shared Function hesai_register_device_ex(
            ByRef config As HesaiDeviceConfig
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_register_device_validation_only(
            <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
            <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
            dataPort As Integer
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_validate_packet(
            <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
            packetData As Byte(),
            packetLength As Integer
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_unregister_device(
            <MarshalAs(UnmanagedType.LPStr)> deviceId As String
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_get_device_stats(
            <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
            ByRef stats As HesaiSdkStats
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_reset_device_stats(
            <MarshalAs(UnmanagedType.LPStr)> deviceId As String
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl)>
        Public Shared Function hesai_initialize() As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl)>
        Public Shared Sub hesai_shutdown()
        End Sub

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_get_inventory_info(
            <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
            ptcPort As Integer,
            ByRef outInfo As HesaiInventoryInfo
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_get_config_info(
            <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
            ptcPort As Integer,
            ByRef outInfo As HesaiConfigInfo
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_get_lidar_status(
            <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
            ptcPort As Integer,
            ByRef outStatus As HesaiLidarStatus
        ) As Integer
        End Function

        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_get_correction_info(
            <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
            ptcPort As Integer,
            outBuffer As Byte(),
            bufferLength As Integer,
            ByRef outLength As Integer
        ) As Integer
        End Function

        ''' <summary>
        ''' ✅ Combined manifest query (0x07/0x08/0x09/0x05) issued over a
        ''' single persistent PTC session. See HesaiWrapper.h for rationale.
        ''' </summary>
        <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function hesai_get_manifest_info(
            <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
            ptcPort As Integer,
            ByRef outInventory As HesaiInventoryInfo,
            ByRef hasInventory As Integer,
            ByRef outConfig As HesaiConfigInfo,
            ByRef hasConfig As Integer,
            ByRef outStatus As HesaiLidarStatus,
            ByRef hasStatus As Integer,
            correctionBuffer As Byte(),
            correctionBufferLength As Integer,
            ByRef correctionLength As Integer,
            ByRef hasCorrection As Integer
        ) As Integer
        End Function
    End Class

    ' ====================================================================
    ' Public Managed Wrappers
    ' ====================================================================

    ''' <summary>
    ''' ✅ LEGACY: Simple device registration (uses sensible defaults)
    ''' Kept for backward compatibility - prefer RegisterDeviceEx
    ''' </summary>
    Public Shared Function RegisterDevice(deviceId As String, ipAddress As String, dataPort As Integer) As Boolean
        Try
            Dim result As Integer = NativeMethods.hesai_register_device(deviceId, ipAddress, dataPort)
            If result = 0 Then
                HandleUserMessageLogging("GMRC", $"✅ Hesai SDK: Registered device '{deviceId}' at {ipAddress}:{dataPort} (using defaults)")
                Return True
            Else
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai SDK: Failed to register device '{deviceId}' (error code: {result})")
                Return False
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.RegisterDevice: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' ✅ Extended device registration with full configuration
    ''' Allows per-device customization of calibration files, network settings, etc.
    ''' </summary>
    ''' <param name="config">Configuration structure (device_id, ip_address, data_port required; others optional)</param>
    ''' <returns>True if registration successful</returns>
    Public Shared Function RegisterDeviceEx(config As HesaiDeviceConfig) As Boolean
        Try
            ' ✅ Log config before calling C++ (for debugging)
            HandleUserMessageLogging("GMRC", $"Hesai SDK: Registering device '{config.device_id}'...")
            HandleUserMessageLogging("GMRC", $"  IP: {config.ip_address}:{config.data_port}")
            HandleUserMessageLogging("GMRC", $"  Host IP: {If(config.host_ip_address, "(auto)")}")
            HandleUserMessageLogging("GMRC", $"  PTC: {config.use_ptc_connected}")
            HandleUserMessageLogging("GMRC", $"  Validation Only: {config.validation_only}")

            Dim result As Integer = NativeMethods.hesai_register_device_ex(config)

            Select Case result
                Case 0
                    ' Build configuration summary for logging
                    Dim configSummary As New System.Text.StringBuilder()
                    configSummary.Append($"'{config.device_id}' at {config.ip_address}:{config.data_port}")

                    If config.validation_only Then
                        configSummary.Append(" | Mode: VALIDATION-ONLY (no UDP bind)")
                    End If

                    If Not String.IsNullOrEmpty(config.correction_file_path) Then
                        configSummary.Append($" | Correction: {System.IO.Path.GetFileName(config.correction_file_path)}")
                    End If

                    If Not String.IsNullOrEmpty(config.host_ip_address) Then
                        configSummary.Append($" | Host: {config.host_ip_address}")
                    End If

                    HandleUserMessageLogging("GMRC", $"✅ Hesai SDK: Registered {configSummary}")
                    Return True

                Case -2
                    ' ✅ Timeout error
                    HandleUserMessageLogging("GMRC", $"⚠️ Hesai SDK: Registration timeout for '{config.device_id}' - device may be unreachable")
                    HandleUserMessageLogging("GMRC", $"   Check: 1) LiDAR is powered on  2) Network cable connected  3) IP address correct")
                    Return False

                Case Else
                    HandleUserMessageLogging("GMRC", $"⚠️ Hesai SDK: Failed to register '{config.device_id}' (error code: {result})")
                    Return False
            End Select

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.RegisterDeviceEx: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' ✅ NEW: Register device in VALIDATION-ONLY mode (no UDP binding)
    ''' Use this when PcapDotNet handles packet capture and you only need SDK statistics.
    ''' The SDK will NOT bind to UDP ports, avoiding conflicts with PcapDotNet.
    ''' </summary>
    ''' <param name="deviceId">Device identifier (e.g., "LiDAR1")</param>
    ''' <param name="ipAddress">LiDAR IP address (for identification only)</param>
    ''' <param name="dataPort">UDP data port (for identification only)</param>
    ''' <returns>True if registration successful</returns>
    Public Shared Function RegisterDeviceValidationOnly(deviceId As String, ipAddress As String, dataPort As Integer) As Boolean
        Try
            HandleUserMessageLogging("GMRC", $"Hesai SDK: Registering device '{deviceId}' in VALIDATION-ONLY mode (no UDP binding)...")

            Dim result As Integer = NativeMethods.hesai_register_device_validation_only(deviceId, ipAddress, dataPort)

            If result = 0 Then
                HandleUserMessageLogging("GMRC", $"✅ Hesai SDK: Registered '{deviceId}' in VALIDATION-ONLY mode - NO UDP port binding")
                Return True
            Else
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai SDK: Failed to register '{deviceId}' in validation mode (error code: {result})")
                Return False
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.RegisterDeviceValidationOnly: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' ✅ NEW: Feed a captured packet to the SDK for validation
    ''' Use this in validation-only mode to get checksum/sequence statistics
    ''' without the SDK binding to UDP ports.
    ''' </summary>
    ''' <param name="deviceId">Device identifier</param>
    ''' <param name="packetData">Raw UDP payload bytes from PcapDotNet capture</param>
    ''' <returns>0 on success (valid packet), negative on error/invalid</returns>
    Public Shared Function ValidatePacket(deviceId As String, packetData As Byte()) As Integer
        Try
            If packetData Is Nothing OrElse packetData.Length = 0 Then
                Return -1
            End If

            Return NativeMethods.hesai_validate_packet(deviceId, packetData, packetData.Length)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.ValidatePacket: {ex.Message}")
            Return -99
        End Try
    End Function

    ''' <summary>
    ''' Unregisters a device
    ''' </summary>
    Public Shared Function UnregisterDevice(deviceId As String) As Boolean
        Try
            Dim result As Integer = NativeMethods.hesai_unregister_device(deviceId)
            If result = 0 Then
                HandleUserMessageLogging("GMRC", $"✅ Hesai SDK: Unregistered device '{deviceId}'")
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.UnregisterDevice: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Gets device statistics with error handling
    ''' </summary>
    Public Shared Function GetDeviceStats(deviceId As String) As HesaiSdkStats
        Try
            Dim stats As New HesaiSdkStats()
            Dim result As Integer = NativeMethods.hesai_get_device_stats(deviceId, stats)
            If result <> 0 Then
                Return New HesaiSdkStats()
            End If
            Return stats
        Catch ex As DllNotFoundException
            Return New HesaiSdkStats()
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.GetDeviceStats: {ex.Message}")
            Return New HesaiSdkStats()
        End Try
    End Function

    ''' <summary>
    ''' Resets device statistics counters
    ''' </summary>
    Public Shared Function ResetDeviceStats(deviceId As String) As Boolean
        Try
            Dim result As Integer = NativeMethods.hesai_reset_device_stats(deviceId)
            Return result = 0
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.ResetDeviceStats: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' ✅ NEW: Queries PTC command 0x07 (inventory info) directly from the LiDAR.
    ''' Opens a short-lived PTC connection independent of device registration state,
    ''' so this works even when the device is registered in validation-only mode.
    ''' </summary>
    ''' <param name="ipAddress">LiDAR IP address</param>
    ''' <param name="ptcPort">PTC TCP port (0 = default 9347)</param>
    ''' <returns>Inventory info, or Nothing if the query failed</returns>
    Public Shared Function GetInventoryInfo(ipAddress As String, Optional ptcPort As Integer = 0) As HesaiInventoryInfo?
        Try
            Dim info As New HesaiInventoryInfo()
            Dim result As Integer = NativeMethods.hesai_get_inventory_info(ipAddress, ptcPort, info)
            If result <> 0 Then
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetInventoryInfo failed for {ipAddress} (error code: {result})")
                Return Nothing
            End If
            Return info
        Catch ex As DllNotFoundException
            Return Nothing
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.GetInventoryInfo: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' ✅ NEW: Queries PTC command 0x08 (config info, incl. return_mode/clock_source)
    ''' directly from the LiDAR.
    ''' </summary>
    ''' <param name="ipAddress">LiDAR IP address</param>
    ''' <param name="ptcPort">PTC TCP port (0 = default 9347)</param>
    ''' <returns>Config info, or Nothing if the query failed</returns>
    Public Shared Function GetConfigInfo(ipAddress As String, Optional ptcPort As Integer = 0) As HesaiConfigInfo?
        Try
            Dim info As New HesaiConfigInfo()
            Dim result As Integer = NativeMethods.hesai_get_config_info(ipAddress, ptcPort, info)
            If result <> 0 Then
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetConfigInfo failed for {ipAddress} (error code: {result})")
                Return Nothing
            End If
            Return info
        Catch ex As DllNotFoundException
            Return Nothing
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.GetConfigInfo: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' ✅ NEW: Queries PTC command 0x09 (live status: motor speed, PTP lock,
    ''' temperature, humidity) directly from the LiDAR.
    ''' </summary>
    ''' <param name="ipAddress">LiDAR IP address</param>
    ''' <param name="ptcPort">PTC TCP port (0 = default 9347)</param>
    ''' <returns>Live status, or Nothing if the query failed</returns>
    Public Shared Function GetLidarStatus(ipAddress As String, Optional ptcPort As Integer = 0) As HesaiLidarStatus?
        Try
            Dim status As New HesaiLidarStatus()
            Dim result As Integer = NativeMethods.hesai_get_lidar_status(ipAddress, ptcPort, status)
            If result <> 0 Then
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetLidarStatus failed for {ipAddress} (error code: {result})")
                Return Nothing
            End If
            Return status
        Catch ex As DllNotFoundException
            Return Nothing
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.GetLidarStatus: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' ✅ NEW: Queries PTC command 0x05 (angle correction file) directly from
    ''' the LiDAR. The response is a plain-text CSV
    ''' ("Laser id,Elevation,Azimuth" header followed by one row per channel).
    ''' Prefer this live value over the configured CorrectionFilePath on disk
    ''' when available, since it reflects the exact unit connected rather than
    ''' a potentially stale file.
    ''' </summary>
    ''' <param name="ipAddress">LiDAR IP address</param>
    ''' <param name="ptcPort">PTC TCP port (0 = default 9347)</param>
    ''' <returns>CSV text, or Nothing if the query failed</returns>
    Public Shared Function GetCorrectionInfo(ipAddress As String, Optional ptcPort As Integer = 0) As String
        Try
            ' Angle correction payloads observed around ~2.2KB for a 128-channel unit;
            ' 16KB gives comfortable headroom without being wasteful.
            Const bufferSize As Integer = 16384
            Dim buffer(bufferSize - 1) As Byte
            Dim actualLength As Integer = 0

            Dim result As Integer = NativeMethods.hesai_get_correction_info(ipAddress, ptcPort, buffer, bufferSize, actualLength)

            If result = -2 Then
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetCorrectionInfo buffer too small for {ipAddress} (needed {actualLength} bytes)")
                Return Nothing
            ElseIf result <> 0 Then
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetCorrectionInfo failed for {ipAddress} (error code: {result})")
                Return Nothing
            End If

            Return System.Text.Encoding.ASCII.GetString(buffer, 0, actualLength)
        Catch ex As DllNotFoundException
            Return Nothing
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.GetCorrectionInfo: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Bundled result of a single combined PTC manifest query (0x07/0x08/0x09/0x05)
    ''' issued over one persistent connection. Each Has* flag indicates whether
    ''' the corresponding field is populated.
    ''' </summary>
    Public Class HesaiManifestQueryResult
        Public Property Inventory As HesaiInventoryInfo?
        Public Property Configuration As HesaiConfigInfo?
        Public Property Status As HesaiLidarStatus?
        Public Property CorrectionCsv As String
    End Class

    ''' <summary>
    ''' ✅ Queries inventory (0x07), config (0x08), status (0x09), and angle
    ''' correction (0x05) over a SINGLE persistent PTC session, instead of
    ''' four independent short-lived connections. The Pandar128E3X PTC TCP
    ''' server only accepts one connection at a time and does not tolerate
    ''' rapid reconnects between queries; issuing all manifest queries over
    ''' one open connection avoids "invalid input parameter" (return code 1)
    ''' failures observed when each query opened its own connection.
    ''' </summary>
    ''' <param name="ipAddress">LiDAR IP address</param>
    ''' <param name="ptcPort">PTC TCP port (0 = default 9347)</param>
    ''' <returns>Result bundle with whichever fields succeeded populated, or Nothing if the connection itself failed</returns>
    Public Shared Function GetManifestInfo(ipAddress As String, Optional ptcPort As Integer = 0) As HesaiManifestQueryResult
        Try
            Dim inventory As New HesaiInventoryInfo()
            Dim hasInventory As Integer = 0
            Dim config As New HesaiConfigInfo()
            Dim hasConfig As Integer = 0
            Dim status As New HesaiLidarStatus()
            Dim hasStatus As Integer = 0
            Const bufferSize As Integer = 16384
            Dim correctionBuffer(bufferSize - 1) As Byte
            Dim correctionLength As Integer = 0
            Dim hasCorrection As Integer = 0

            Dim result As Integer = NativeMethods.hesai_get_manifest_info(
                ipAddress, ptcPort,
                inventory, hasInventory,
                config, hasConfig,
                status, hasStatus,
                correctionBuffer, bufferSize, correctionLength, hasCorrection)

            If result <> 0 Then
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetManifestInfo connection failed for {ipAddress} (error code: {result})")
                Return Nothing
            End If

            Dim manifestResult As New HesaiManifestQueryResult()
            If hasInventory <> 0 Then manifestResult.Inventory = inventory
            If hasConfig <> 0 Then manifestResult.Configuration = config
            If hasStatus <> 0 Then manifestResult.Status = status
            If hasCorrection <> 0 Then manifestResult.CorrectionCsv = System.Text.Encoding.ASCII.GetString(correctionBuffer, 0, correctionLength)

            If hasInventory = 0 Then HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetManifestInfo inventory query failed for {ipAddress}")
            If hasConfig = 0 Then HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetManifestInfo config query failed for {ipAddress}")
            If hasStatus = 0 Then HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetManifestInfo status query failed for {ipAddress}")
            If hasCorrection = 0 Then HandleUserMessageLogging("GMRC", $"⚠️ Hesai PTC: GetManifestInfo correction query failed for {ipAddress}")

            Return manifestResult
        Catch ex As DllNotFoundException
            Return Nothing
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.GetManifestInfo: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' ✅ Helper to create default configuration for simple registration
    ''' </summary>
    Public Shared Function CreateDefaultConfig(deviceId As String, ipAddress As String, dataPort As Integer) As HesaiDeviceConfig
        Return New HesaiDeviceConfig With {
            .device_id = deviceId,
            .ip_address = ipAddress,
            .data_port = dataPort,
            .correction_file_path = Nothing,
            .firetimes_path = Nothing,
            .host_ip_address = Nothing,
            .multicast_ip_address = Nothing,
            .ptc_port = 9347,
            .use_ptc_connected = False,
            .enable_parser_thread = True,
            .enable_udp_thread = True,
            .validation_only = False
        }
    End Function

    ''' <summary>
    ''' ✅ NEW: Helper to create VALIDATION-ONLY configuration (no UDP binding)
    ''' </summary>
    Public Shared Function CreateValidationOnlyConfig(deviceId As String, ipAddress As String, dataPort As Integer) As HesaiDeviceConfig
        Return New HesaiDeviceConfig With {
            .device_id = deviceId,
            .ip_address = ipAddress,
            .data_port = dataPort,
            .correction_file_path = Nothing,
            .firetimes_path = Nothing,
            .host_ip_address = Nothing,
            .multicast_ip_address = Nothing,
            .ptc_port = 0,
            .use_ptc_connected = False,
            .enable_parser_thread = False,
            .enable_udp_thread = False,
            .validation_only = True
        }
    End Function

    ''' <summary>
    ''' ✅ Initializes the Hesai SDK. Called once at application startup.
    ''' </summary>
    Public Shared Function Initialize() As Boolean
        Try
            Dim result As Integer = NativeMethods.hesai_initialize()
            If result = 0 Then
                HandleUserMessageLogging("GMRC", "✅ Hesai SDK: Initialized")
                Return True
            Else
                HandleUserMessageLogging("GMRC", $"⚠️ Hesai SDK: Initialization failed (error code: {result})")
                Return False
            End If
        Catch ex As DllNotFoundException
            HandleUserMessageLogging("GMRC", "⚠️ Hesai SDK wrapper (HesaiWrapper.dll) not found")
            Return False
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.Initialize: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' ✅ Shuts down the Hesai SDK and releases all resources.
    ''' </summary>
    Public Shared Sub Shutdown()
        Try
            NativeMethods.hesai_shutdown()
            HandleUserMessageLogging("GMRC", "✅ Hesai SDK: Shutdown complete")
        Catch ex As DllNotFoundException
            ' DLL not loaded - nothing to shutdown
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.Shutdown: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Checks if Hesai SDK wrapper is available
    ''' </summary>
    Public Shared Function IsAvailable() As Boolean
        Try
            ' Check if DLL exists
            Dim dllPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HesaiWrapper.dll")

            If Not File.Exists(dllPath) Then
                HandleUserMessageLogging("GMRC", "⚠️ HesaiWrapper.dll not found - SDK statistics unavailable")
                Return False
            End If

            ' Try to call a function to verify DLL loads
            Dim testStats As New HesaiSdkStats()
            NativeMethods.hesai_get_device_stats("", testStats)
            Return True

        Catch ex As DllNotFoundException
            ' Show detailed diagnostics only on failure
            HandleUserMessageLogging("GMRC", $"⚠️ HesaiWrapper.dll load failed: {ex.Message}")
            HandleUserMessageLogging("GMRC", $"   HRESULT: 0x{Marshal.GetHRForException(ex):X8}")

            ' Check for common missing dependencies
            Dim commonDeps As String() = {"vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll", "ws2_32.dll"}
            HandleUserMessageLogging("GMRC", $"   Checking dependencies in System32:")
            For Each dep In commonDeps
                Dim depPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), dep)
                HandleUserMessageLogging("GMRC", $"     {dep}: {If(File.Exists(depPath), "✓", "✗ MISSING")}")
            Next
            Return False

        Catch ex As BadImageFormatException
            HandleUserMessageLogging("GMRC", $"❌ HesaiWrapper.dll architecture mismatch: {ex.Message}")
            HandleUserMessageLogging("GMRC", $"   Process is {If(Environment.Is64BitProcess, "64-bit", "32-bit")}")
            Return False

        Catch ex As EntryPointNotFoundException
            HandleUserMessageLogging("GMRC", $"❌ HesaiWrapper.dll missing function: {ex.Message}")
            HandleUserMessageLogging("GMRC", $"   DLL may be outdated or incorrectly built")
            Return False

        Catch ex As Exception
            ' Any other exception means DLL loaded successfully (empty device ID test fails, which is expected)
            Return True
        End Try
    End Function

End Class
