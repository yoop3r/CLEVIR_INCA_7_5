Imports System.Text

''' <summary>
''' Decodes OXTS NCOM Status Channels (Batch S)
''' Based on OXTS NCOM Manual Rev 250811
''' </summary>
Public Module OxtsStatusChannelDecoder

    ''' <summary>
    ''' Status Channel 23: System up-time, GPS rejections, PTP Status
    ''' OXTS Manual Table 33, Page 31
    ''' </summary>
    Public Structure StatusChannel23
        Public BlendedNavLagMs As UShort        ' Bytes 0-1: Kalman filter lag time (ms)
        Public SystemUptime As UShort           ' Bytes 2-3: System uptime (non-linear scale)
        Public GpsPositionRejects As Byte       ' Byte 4: Consecutive GPS position updates rejected
        Public GpsVelocityRejects As Byte       ' Byte 5: Consecutive GPS velocity updates rejected
        Public GpsAttitudeRejects As Byte       ' Byte 6: Consecutive GPS attitude updates rejected
        Public PtpStatus As PtpStatusEnum       ' Byte 7: PTP synchronization status
        Public IsValid As Boolean               ' Indicates if data was successfully decoded
    End Structure

    ''' <summary>
    ''' PTP Status enumeration (OXTS Manual Table 33, byte 7)
    ''' </summary>
    Public Enum PtpStatusEnum As Byte
        Invalid = 0
        Initialising = 1
        Faulty = 2
        Disabled = 3
        Listening = 4
        PreMaster = 5
        Master = 6              ' ? OXTS is acting as PTP grandmaster
        Passive = 7
        Uncalibrated = 8
        Slave = 9               ' OXTS is syncing to external PTP master
        Locked = 10             ' ? Fully synchronized!
        ConfigError = 11
        CriticalError = 12
        Unknown = 13
    End Enum

    ''' <summary>
    ''' Status Channel 93: PTP configuration and timing source
    ''' OXTS Manual Table 81, Page 54
    ''' </summary>
    Public Structure StatusChannel93
        Public Reserved1 As Byte                ' Byte 0
        Public GalileoBeidouSats As Byte        ' Byte 1: Satellite counts
        Public NmeaAndPtpEpoch As Byte          ' Byte 2: NMEA output + PTP epoch type
        Public PpsStatus As Byte                ' Byte 3: PPS status (bits 0-3)
        Public LidarIpLsb As Byte               ' Byte 4: Configured LiDAR IP (LSB)
        Public TimingSource As TimingSourceEnum ' Byte 5: Primary timing source
        Public Reserved2 As UShort              ' Bytes 6-7
        Public IsValid As Boolean
    End Structure

    ''' <summary>
    ''' Timing source for OXTS system (Status Channel 93, byte 5)
    ''' </summary>
    Public Enum TimingSourceEnum As Byte
        None = 0                ' Internal SDN time (no external sync)
        PrimaryGnss = 1         ' Primary GNSS Receiver (default)
        Ptp = 2                 ' PTP (IEEE 1588)
        ExternalGnss = 3        ' External GNSS Receiver
        UserCoarse = 4          ' User coarse time from command
        GadStream = 5           ' Calculated from GAD stream
    End Enum

    ''' <summary>
    ''' Decodes Status Channel 23 from NCOM Batch S (bytes 63-70)
    ''' </summary>
    Public Function DecodeStatusChannel23(batchS As Byte()) As StatusChannel23
        Dim result As New StatusChannel23

        If batchS Is Nothing OrElse batchS.Length < 8 Then
            result.IsValid = False
            Return result
        End If

        Try
            ' Byte 0-1: Blended navigation lag time (ms)
            result.BlendedNavLagMs = BitConverter.ToUInt16(batchS, 0)

            ' Byte 2-3: System uptime (non-linear scale)
            result.SystemUptime = BitConverter.ToUInt16(batchS, 2)

            ' Byte 4: GPS position update rejects
            result.GpsPositionRejects = batchS(4)

            ' Byte 5: GPS velocity update rejects
            result.GpsVelocityRejects = batchS(5)

            ' Byte 6: GPS attitude update rejects
            result.GpsAttitudeRejects = batchS(6)

            ' Byte 7: PTP Status
            Dim ptpByte As Byte = batchS(7)
            If ptpByte <= 13 Then
                result.PtpStatus = CType(ptpByte, PtpStatusEnum)
            Else
                result.PtpStatus = PtpStatusEnum.Unknown
            End If

            result.IsValid = True

        Catch ex As Exception
            result.IsValid = False
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Decodes Status Channel 93 from NCOM Batch S (bytes 63-70)
    ''' </summary>
    Public Function DecodeStatusChannel93(batchS As Byte()) As StatusChannel93
        Dim result As New StatusChannel93

        If batchS Is Nothing OrElse batchS.Length < 8 Then
            result.IsValid = False
            Return result
        End If

        Try
            result.Reserved1 = batchS(0)
            result.GalileoBeidouSats = batchS(1)
            result.NmeaAndPtpEpoch = batchS(2)
            result.PpsStatus = CByte(batchS(3) And &HF) ' Bits 0-3 only
            result.LidarIpLsb = batchS(4)

            ' Byte 5: Timing source (bits 0-3)
            Dim timingSrc As Byte = CByte(batchS(5) And &HF)
            If timingSrc <= 5 Then
                result.TimingSource = CType(timingSrc, TimingSourceEnum)
            Else
                result.TimingSource = TimingSourceEnum.None
            End If

            result.Reserved2 = BitConverter.ToUInt16(batchS, 6)
            result.IsValid = True

        Catch ex As Exception
            result.IsValid = False
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Extracts Status Channel ID and Batch S data from NCOM packet
    ''' </summary>
    Public Function ExtractBatchS(ncomPacket As Byte()) As (ChannelId As Byte, BatchS As Byte())
        If ncomPacket Is Nothing OrElse ncomPacket.Length < 72 Then
            Return (0, Nothing)
        End If

        ' Byte 62: Status channel ID
        Dim channelId As Byte = ncomPacket(62)

        ' Bytes 63-70: Batch S (8 bytes)
        Dim batchS(7) As Byte
        Array.Copy(ncomPacket, 63, batchS, 0, 8)

        Return (channelId, batchS)
    End Function

    ''' <summary>
    ''' Returns human-readable description of PTP status
    ''' </summary>
    Public Function GetPtpStatusDescription(status As PtpStatusEnum) As String
        Select Case status
            Case PtpStatusEnum.Invalid
                Return "? Invalid"
            Case PtpStatusEnum.Initialising
                Return "?? Initializing..."
            Case PtpStatusEnum.Faulty
                Return "?? Faulty"
            Case PtpStatusEnum.Disabled
                Return "?? Disabled"
            Case PtpStatusEnum.Listening
                Return "?? Listening for master"
            Case PtpStatusEnum.PreMaster
                Return "?? Pre-master"
            Case PtpStatusEnum.Master
                Return "?? Master (Grandmaster Clock)"
            Case PtpStatusEnum.Passive
                Return "?? Passive"
            Case PtpStatusEnum.Uncalibrated
                Return "?? Uncalibrated"
            Case PtpStatusEnum.Slave
                Return "?? Slave (syncing to external)"
            Case PtpStatusEnum.Locked
                Return "? LOCKED (Fully Synchronized!)"
            Case PtpStatusEnum.ConfigError
                Return "? Configuration Error"
            Case PtpStatusEnum.CriticalError
                Return "?? CRITICAL ERROR"
            Case PtpStatusEnum.Unknown
                Return "? Unknown"
            Case Else
                Return $"?? Unexpected value: {status}"
        End Select
    End Function

    ''' <summary>
    ''' Returns human-readable description of timing source
    ''' </summary>
    Public Function GetTimingSourceDescription(source As TimingSourceEnum) As String
        Select Case source
            Case TimingSourceEnum.None
                Return "? None (Internal SDN time)"
            Case TimingSourceEnum.PrimaryGnss
                Return "??? Primary GNSS (Default)"
            Case TimingSourceEnum.Ptp
                Return "?? PTP (IEEE 1588)"
            Case TimingSourceEnum.ExternalGnss
                Return "?? External GNSS"
            Case TimingSourceEnum.UserCoarse
                Return "?? User Coarse Time"
            Case TimingSourceEnum.GadStream
                Return "?? GAD Stream"
            Case Else
                Return $"? Unknown ({source})"
        End Select
    End Function

    ''' <summary>
    ''' Decodes system uptime from non-linear scale (Status Channel 23, bytes 2-3)
    ''' </summary>
    Public Function DecodeSystemUptime(uptimeValue As UShort) As String
        If uptimeValue = &HFFFF Then
            Return "Invalid"
        ElseIf uptimeValue > 20700 Then
            ' Hours: (value - 20532)
            Dim hours As Integer = uptimeValue - 20532
            Return $"{hours:N0} hours"
        ElseIf uptimeValue > 10800 Then
            ' Minutes: (value - 10620)
            Dim minutes As Integer = uptimeValue - 10620
            Return $"{minutes:N0} minutes"
        Else
            ' Seconds: value
            Return $"{uptimeValue:N0} seconds"
        End If
    End Function

End Module
