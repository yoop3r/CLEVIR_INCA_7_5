Option Strict On
Imports System.Runtime.InteropServices

''' <summary>
''' P/Invoke wrapper for Hesai LiDAR SDK statistics
''' Maps to functions in libhesai.dll (compiled from lidar.cc)
''' </summary>
Public Class HesaiInterop

    ' ====================================================================
    ' P/Invoke Declarations
    ' ====================================================================

    ''' <summary>
    ''' Native struct matching HesaiLidar_SDK_2.0-master/libhesai/Lidar/lidar.cc stats
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

    ' TODO: Update DLL path to match your actual build output
    Private Const LibHesaiDll As String = "libhesai.dll"

    ''' <summary>
    ''' Gets statistics for a specific LiDAR device
    ''' </summary>
    ''' <param name="deviceId">Device identifier (e.g., "10.5.55.14")</param>
    ''' <param name="stats">Output stats structure</param>
    ''' <returns>0 on success, -1 on error</returns>
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
    ''' Gets device statistics with error handling
    ''' </summary>
    Public Shared Function GetDeviceStats(deviceId As String) As HesaiSdkStats
        Try
            Dim stats As New HesaiSdkStats()
            Dim result As Integer = hesai_get_device_stats(deviceId, stats)

            If result <> 0 Then
                ' Return zeros on error
                HandleUserMessageLogging("GMRC", $"HesaiInterop: Failed to get stats for {deviceId}")
                Return New HesaiSdkStats()
            End If

            Return stats

        Catch ex As DllNotFoundException
            ' DLL not found - expected if SDK not compiled yet
            HandleUserMessageLogging("GMRC", $"HesaiInterop: libhesai.dll not found. Stats unavailable.")
            Return New HesaiSdkStats()

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HesaiInterop: {ex.Message}")
            Return New HesaiSdkStats()
        End Try
    End Function

    ''' <summary>
    ''' Resets device statistics
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
    ''' Checks if Hesai SDK is available
    ''' </summary>
    Public Shared Function IsAvailable() As Boolean
        Try
            Dim testStats As New HesaiSdkStats()
            hesai_get_device_stats("", testStats)  ' Empty device ID to test DLL load
            Return True
        Catch ex As DllNotFoundException
            Return False
        Catch
            Return True  ' DLL loaded, function call failed (expected)
        End Try
    End Function

End Class
