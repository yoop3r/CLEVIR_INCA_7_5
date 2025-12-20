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

    ' DLL path - use relative name so it finds the DLL in the same directory
    Private Const LibHesaiDll As String = "HesaiWrapper.dll"

    ''' <summary>
    ''' Registers a LiDAR device with the Hesai SDK
    ''' </summary>
    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_register_device(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
        <MarshalAs(UnmanagedType.LPStr)> ipAddress As String,
        dataPort As Integer
    ) As Integer
    End Function

    ''' <summary>
    ''' Unregisters a LiDAR device
    ''' </summary>
    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_unregister_device(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String
    ) As Integer
    End Function

    ''' <summary>
    ''' Gets statistics for a specific LiDAR device
    ''' </summary>
    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_get_device_stats(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String,
        ByRef stats As HesaiSdkStats
    ) As Integer
    End Function

    ''' <summary>
    ''' Resets statistics counters for a device
    ''' </summary>
    <DllImport(LibHesaiDll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Shared Function hesai_reset_device_stats(
        <MarshalAs(UnmanagedType.LPStr)> deviceId As String
    ) As Integer
    End Function

    ' ====================================================================
    ' Public Managed Wrappers
    ' ====================================================================

    ''' <summary>
    ''' Registers a device for statistics tracking
    ''' </summary>
    Public Shared Function RegisterDevice(deviceId As String, ipAddress As String, dataPort As Integer) As Boolean
        Try
            Dim result As Integer = hesai_register_device(deviceId,
                                                          ipAddress,
                                                          dataPort)
            If result = 0 Then
                HandleUserMessageLogging("GMRC", $"✅ Hesai SDK: Registered device '{deviceId}' at {ipAddress}:{dataPort}")
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
                ' Return zeros on error (device not registered or other error)
                Return New HesaiSdkStats()
            End If

            Return stats

        Catch ex As DllNotFoundException
            ' DLL not found - silently return zeros (already logged in IsAvailable)
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
    ''' Checks if Hesai SDK wrapper is available
    ''' </summary>
    Public Shared Function IsAvailable() As Boolean
        Try
            Dim testStats As New HesaiSdkStats()
            hesai_get_device_stats("", testStats)  ' Empty device ID to test DLL load
            Return True
        Catch ex As DllNotFoundException
            HandleUserMessageLogging("GMRC", "⚠️ Hesai SDK wrapper (HesaiWrapper.dll) not found - SDK statistics unavailable")
            Return False
        Catch
            ' DLL loaded successfully, function call failed (expected for empty device ID)
            Return True
        End Try
    End Function

End Class
