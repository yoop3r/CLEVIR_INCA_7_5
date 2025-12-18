Imports System.Text

''' <summary>
''' Diagnostic tool to dump NCOM packet bytes for analysis
''' Add this to Module1.vb or a test form
''' </summary>
Public Module NcomDiagnostics

    ''' <summary>
    ''' Dumps NCOM packet bytes in hex format for debugging
    ''' Call this from OxtsNcomInterface.ParseNcomPacket before decoding
    ''' </summary>
    Public Sub DumpNcomPacket(data As Byte(), Optional maxBytes As Integer = 72)
        If data Is Nothing OrElse data.Length = 0 Then
            Console.WriteLine("NCOM: Empty packet")
            Return
        End If

        Dim sb As New StringBuilder()
        sb.AppendLine("=== NCOM Packet Dump ===")
        sb.AppendLine($"Total length: {data.Length} bytes")
        sb.AppendLine()

        ' Header
        sb.AppendLine("Offset | Hex Data                                         | ASCII")
        sb.AppendLine("-------|--------------------------------------------------|-------")

        Dim bytesToShow As Integer = Math.Min(data.Length, maxBytes)

        For i As Integer = 0 To bytesToShow - 1 Step 16
            ' Offset
            sb.Append($"{i:D5}  | ")

            ' Hex bytes (16 per line)
            For j As Integer = 0 To 15
                If i + j < bytesToShow Then
                    sb.Append($"{data(i + j):X2} ")
                Else
                    sb.Append("   ")
                End If
            Next

            sb.Append("| ")

            ' ASCII representation
            For j As Integer = 0 To 15
                If i + j < bytesToShow Then
                    Dim c As Char = Chr(data(i + j))
                    If Char.IsControl(c) Then
                        sb.Append(".")
                    Else
                        sb.Append(c)
                    End If
                End If
            Next

            sb.AppendLine()
        Next

        sb.AppendLine()
        sb.AppendLine("=== Key Field Offsets ===")

        If data.Length >= 72 Then
            ' Position
            sb.AppendLine($"Lat  [7-9]:   {data(7):X2} {data(8):X2} {data(9):X2}")
            sb.AppendLine($"Lon [10-12]:  {data(10):X2} {data(11):X2} {data(12):X2}")
            sb.AppendLine($"Alt [13-15]:  {data(13):X2} {data(14):X2} {data(15):X2}")
            sb.AppendLine()

            ' Velocities
            sb.AppendLine($"VN  [16-18]:  {data(16):X2} {data(17):X2} {data(18):X2}")
            sb.AppendLine($"VE  [19-21]:  {data(19):X2} {data(20):X2} {data(21):X2}")
            sb.AppendLine($"VD  [22-24]:  {data(22):X2} {data(23):X2} {data(24):X2}")
            sb.AppendLine()

            ' Orientation
            sb.AppendLine($"Pitch [27]:   {data(27):X2}")
            sb.AppendLine($"Roll  [28]:   {data(28):X2}")
            sb.AppendLine()

            ' Angular rates
            sb.AppendLine($"Wx  [32-33]:  {data(32):X2} {data(33):X2}")
            sb.AppendLine($"Wy  [34-35]:  {data(34):X2} {data(35):X2}")
            sb.AppendLine($"Wz  [36-37]:  {data(36):X2} {data(37):X2}")
            sb.AppendLine()

            ' Heading
            sb.AppendLine($"Hdg [39-40]:  {data(39):X2} {data(40):X2}")
        End If

        Console.WriteLine(sb.ToString())
        HandleUserMessageLogging("GMRC", sb.ToString())
    End Sub

    ''' <summary>
    ''' Manually decode specific fields to verify against DLL output
    ''' </summary>
    Public Sub VerifyNcomDecoding(data As Byte())
        If data Is Nothing OrElse data.Length < 72 Then Return

        Console.WriteLine("=== Manual Field Verification ===")

        ' Velocity North (bytes 16-18, signed 24-bit, scale 0.0001)
        Dim vn_raw As Integer = data(16) Or (data(17) << 8) Or (data(18) << 16)
        If (vn_raw And &H800000) <> 0 Then
            vn_raw = vn_raw Or &HFF000000 ' Sign extend
        End If
        Dim vn As Double = vn_raw * 0.0001
        Console.WriteLine($"VN Raw: 0x{vn_raw:X6} = {vn_raw} ? {vn:F4} m/s")

        ' Heading (bytes 39-40, unsigned 16-bit, scale 0.0001 rad)
        Dim hdg_raw As UShort = CUShort(data(39) Or (data(40) << 8))
        Dim hdg_rad As Double = hdg_raw * 0.0001
        Dim hdg_deg As Double = hdg_rad * (180.0 / Math.PI)
        Console.WriteLine($"Heading Raw: 0x{hdg_raw:X4} = {hdg_raw} ? {hdg_rad:F4} rad ? {hdg_deg:F2}°")

        ' Yaw Rate (bytes 36-37, signed 16-bit, scale 0.00001 rad/s)
        Dim wz_raw As Short = CShort(data(36) Or (data(37) << 8))
        Dim wz As Double = wz_raw * 0.00001
        Console.WriteLine($"Wz Raw: 0x{wz_raw:X4} = {wz_raw} ? {wz:F5} rad/s")

        Console.WriteLine()
    End Sub

End Module
