Option Strict On
Imports System.Runtime.InteropServices

''' <summary>
''' P/Invoke wrapper for Hesai LiDAR SDK statistics
''' Maps to functions in HesaiWrapper.dll
''' </summary>
Public Class HesaiInterop

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

    ' DLL path - use relative name so it finds the DLL in the same directory
    Private Const LibHesaiDll As String = "HesaiWrapper.dll"

    ' ====================================================================
    ' P/Invoke Declarations
    ' ====================================================================

    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_register_device(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
        <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
        dataPort As Integer
    ) As Integer
    End Function

    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function hesai_register_device_ex(
        ByRef config As HesaiDeviceConfig
    ) As Integer
    End Function

    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_register_device_validation_only(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
        <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
        dataPort As Integer
    ) As Integer
    End Function

    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_validate_packet(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
        packetData As Byte(),
        packetLength As Integer
    ) As Integer
    End Function

    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_unregister_device(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String
    ) As Integer
    End Function

    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_get_device_stats(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
        ByRef stats As HesaiSdkStats
    ) As Integer
    End Function

    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_reset_device_stats(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String
    ) As Integer
    End Function

    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function hesai_initialize() As Integer
    End Function

    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub hesai_shutdown()
    End Sub

    ' ====================================================================
    ' Public Managed Wrappers
    ' ====================================================================

    ''' <summary>
    ''' ✅ LEGACY: Simple device registration (uses sensible defaults)
    ''' Kept for backward compatibility - prefer RegisterDeviceEx
    ''' </summary>
    Public Shared Function RegisterDevice(deviceId As String, ipAddress As String, dataPort As Integer) As Boolean
        Try
            Dim result As Integer = hesai_register_device(deviceId, ipAddress, dataPort)
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

            Dim result As Integer = hesai_register_device_ex(config)

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

            Dim result As Integer = hesai_register_device_validation_only(deviceId, ipAddress, dataPort)

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

            Return hesai_validate_packet(deviceId, packetData, packetData.Length)

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
            Dim result As Integer = hesai_unregister_device(deviceId)
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
            Dim result As Integer = hesai_get_device_stats(deviceId, stats)
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
            Dim result As Integer = hesai_reset_device_stats(deviceId)
            Return result = 0
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop.ResetDeviceStats: {ex.Message}")
            Return False
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
            Dim result As Integer = hesai_initialize()
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
            hesai_shutdown()
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
            Dim testStats As New HesaiSdkStats()
            hesai_get_device_stats("", testStats)
            Return True
        Catch ex As DllNotFoundException
            HandleUserMessageLogging("GMRC", "⚠️ Hesai SDK wrapper (HesaiWrapper.dll) not found - SDK statistics unavailable")
            Return False
        Catch
            Return True
        End Try
    End Function

End Class
