Imports System.Runtime.InteropServices

''' <summary>
''' P/Invoke wrapper for simplified OXTS NCOM decoder (C DLL)
''' </summary>
Public Class OxtsNcomDecoder
    ' Path to NCOMdecoder DLL
    Private Const DllPath As String = "NCOMdecoder.dll"

    ' ✅ NCOM data structure (matches ncom_simple.c NcomData)
    <StructLayout(LayoutKind.Sequential)>
    Public Structure NcomData
        Public Latitude As Double          ' degrees
        Public Longitude As Double         ' degrees
        Public Altitude As Double          ' meters
        Public Heading As Double           ' degrees
        Public Pitch As Double             ' degrees
        Public Roll As Double              ' degrees

        ' GPS Time
        Public GpsWeek As Integer          ' GPS week number
        Public GpsTimeOfWeek As Double     ' seconds (0-59.999)

        ' Velocities (m/s, NED frame)
        Public VelocityNorth As Double
        Public VelocityEast As Double
        Public VelocityDown As Double

        ' Angular rates (rad/s, body frame)
        Public RollRate As Double          ' Wx
        Public PitchRate As Double         ' Wy
        Public YawRate As Double           ' Wz (yaw rate!)

        Public NavigationStatus As Integer ' Status byte
        Public IsValid As Integer          ' 1 if valid
    End Structure

    ' ✅ Import NCOM decoder function
    <DllImport(DllPath, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function NcomDecodePacket(<MarshalAs(UnmanagedType.LPArray)> packet As Byte(), packetLength As Integer, <Out> ByRef decoded As NcomData) As Integer
    End Function

    ''' <summary>
    ''' Decodes NCOM packet to OxtsPosition structure
    ''' </summary>
    Public Shared Function DecodePacket(data As Byte()) As OxtsNcomInterface.OxtsPosition?
        Try
            Dim ncomData As NcomData

            ' Decode the packet
            Dim result As Integer = NcomDecodePacket(data, data.Length, ncomData)

            If result = 1 AndAlso ncomData.IsValid = 1 Then ' Success
                ' Compute GPS time from week + TOW
                Dim gpsEpoch As New DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc)
                Dim gpsTime As DateTime = gpsEpoch.AddDays(ncomData.GpsWeek * 7).AddSeconds(ncomData.GpsTimeOfWeek)

                Return New OxtsNcomInterface.OxtsPosition With {
                    .Timestamp = gpsTime, ' ✅ Use actual GPS time!
                    .Latitude = ncomData.Latitude,
                    .Longitude = ncomData.Longitude,
                    .Altitude = ncomData.Altitude,
                    .Heading = ncomData.Heading,
                    .Pitch = ncomData.Pitch,
                    .Roll = ncomData.Roll,
                    .VelocityNorth = ncomData.VelocityNorth,
                    .VelocityEast = ncomData.VelocityEast,
                    .VelocityDown = ncomData.VelocityDown,
                    .YawRate = ncomData.YawRate, ' ✅ NEW!
                    .PitchRate = ncomData.PitchRate, ' ✅ NEW!
                    .RollRate = ncomData.RollRate, ' ✅ NEW!
                    .GpsMode = GetGpsModeFromStatus(ncomData.NavigationStatus),
                    .NavigationStatus = CByte(ncomData.NavigationStatus),
                    .RtkStatus = GetRtkStatusFromNavStatus(CByte(ncomData.NavigationStatus))
                }
            End If

            Return Nothing

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"NCOM decode error: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Extract GPS mode from navigation status byte
    ''' </summary>
    Private Shared Function GetGpsModeFromStatus(navStatus As Integer) As Integer
        ' Bits 0-1 of navigation status indicate GPS mode
        Return navStatus And &H3
    End Function

    ''' <summary>
    ''' Convert navigation status to RTK status string
    ''' </summary>
    Private Shared Function GetRtkStatusFromNavStatus(navStatus As Byte) As String
        ' Navigation status values:
        ' 0 = Invalid, 1 = Raw IMU, 2 = Initializing, 3 = Locking, 4 = Locked, etc.
        Select Case navStatus
            Case 4 : Return "Locked"
            Case 3 : Return "Locking"
            Case 2 : Return "Initializing"
            Case 1 : Return "Raw IMU"
            Case Else : Return "Invalid"
        End Select
    End Function
End Class
