''' <summary>
''' NCOM packet checksum validation class based on OXTS NCOM specification (Rev. 250811).
''' Implements three-level checksum verification for Structure-A packets.
''' </summary>
''' <remarks>
''' Checksum algorithm: Simple byte summation (unsigned byte overflow wraps naturally).
''' Note: Sync byte (byte 0) is NOT included in any checksum calculation.
''' 
''' Checksum levels:
''' - Checksum 1 (byte 22): Validates bytes 1-21 (Batch A + Navigation Status)
''' - Checksum 2 (byte 61): Validates bytes 1-60 (Batch A + Batch B)
''' - Checksum 3 (byte 71): Validates bytes 1-70 (entire packet except sync)
''' </remarks>
Public Class OxtsNcomChecksum

#Region "Constants"
    ''' <summary>NCOM Structure-A packet size in bytes</summary>
    Public Const NCOM_PACKET_SIZE As Integer = 72

    ''' <summary>Sync byte value (always 0xE7)</summary>
    Public Const SYNC_BYTE As Byte = &HE7

    ''' <summary>Byte positions for checksums</summary>
    Private Const CHECKSUM1_POSITION As Integer = 22
    Private Const CHECKSUM2_POSITION As Integer = 61
    Private Const CHECKSUM3_POSITION As Integer = 71

    ''' <summary>Navigation status byte position</summary>
    Private Const NAV_STATUS_POSITION As Integer = 21

    ''' <summary>Structure-B navigation status value (packet should be ignored)</summary>
    Private Const NAV_STATUS_STRUCTURE_B As Byte = 11

    ''' <summary>Valid Structure-A navigation status values</summary>
    Private Shared ReadOnly ValidNavStatusValues As Byte() = {0, 1, 2, 3, 4, 5, 6, 7, 10, 20, 21, 22}
#End Region

#Region "Checksum Calculation"
    ''' <summary>
    ''' Calculates checksum for a range of bytes using simple summation.
    ''' </summary>
    ''' <param name="data">Packet data array</param>
    ''' <param name="startIndex">Start index (inclusive, typically 1 to skip sync)</param>
    ''' <param name="endIndex">End index (exclusive)</param>
    ''' <returns>Calculated checksum byte</returns>
    Private Shared Function CalculateChecksum(data As Byte(), startIndex As Integer, endIndex As Integer) As Byte
        Dim checksum As Byte = 0
        For i As Integer = startIndex To endIndex - 1
            ' Byte addition with automatic overflow wrapping
            checksum += data(i)
        Next
        Return checksum
    End Function

    ''' <summary>
    ''' Calculates Checksum 1 (validates Batch A and Navigation Status).
    ''' </summary>
    ''' <param name="data">NCOM packet data (minimum 23 bytes)</param>
    ''' <returns>Calculated Checksum 1 value</returns>
    ''' <remarks>Validates bytes 1-21 (skips sync byte at index 0)</remarks>
    Public Shared Function CalculateChecksum1(data As Byte()) As Byte
        If data Is Nothing OrElse data.Length < CHECKSUM1_POSITION + 1 Then
            Throw New ArgumentException($"Data must be at least {CHECKSUM1_POSITION + 1} bytes for Checksum 1", NameOf(data))
        End If
        Return CalculateChecksum(data, 1, CHECKSUM1_POSITION)
    End Function

    ''' <summary>
    ''' Calculates Checksum 2 (validates Batch A and Batch B).
    ''' </summary>
    ''' <param name="data">NCOM packet data (minimum 62 bytes)</param>
    ''' <returns>Calculated Checksum 2 value</returns>
    ''' <remarks>Validates bytes 1-60 (continuation of Checksum 1)</remarks>
    Public Shared Function CalculateChecksum2(data As Byte()) As Byte
        If data Is Nothing OrElse data.Length < CHECKSUM2_POSITION + 1 Then
            Throw New ArgumentException($"Data must be at least {CHECKSUM2_POSITION + 1} bytes for Checksum 2", NameOf(data))
        End If
        Return CalculateChecksum(data, 1, CHECKSUM2_POSITION)
    End Function

    ''' <summary>
    ''' Calculates Checksum 3 (validates entire packet).
    ''' </summary>
    ''' <param name="data">NCOM packet data (must be 72 bytes)</param>
    ''' <returns>Calculated Checksum 3 value</returns>
    ''' <remarks>Validates bytes 1-70 (entire packet except sync byte)</remarks>
    Public Shared Function CalculateChecksum3(data As Byte()) As Byte
        If data Is Nothing OrElse data.Length < NCOM_PACKET_SIZE Then
            Throw New ArgumentException($"Data must be {NCOM_PACKET_SIZE} bytes for Checksum 3", NameOf(data))
        End If
        Return CalculateChecksum(data, 1, CHECKSUM3_POSITION)
    End Function
#End Region

#Region "Validation Methods"
    ''' <summary>
    ''' Validates the sync byte.
    ''' </summary>
    Public Shared Function ValidateSyncByte(data As Byte()) As Boolean
        If data Is Nothing OrElse data.Length < 1 Then Return False
        Return data(0) = SYNC_BYTE
    End Function

    ''' <summary>
    ''' Validates the navigation status byte and determines packet structure.
    ''' </summary>
    ''' <param name="data">NCOM packet data</param>
    ''' <param name="isStructureB">Output: True if Structure-B (should be ignored)</param>
    ''' <returns>True if navigation status is valid Structure-A</returns>
    Public Shared Function ValidateNavigationStatus(data As Byte(), ByRef isStructureB As Boolean) As Boolean
        If data Is Nothing OrElse data.Length < NAV_STATUS_POSITION + 1 Then
            isStructureB = False
            Return False
        End If

        Dim navStatus As Byte = data(NAV_STATUS_POSITION)

        ' Check for Structure-B (internal use only)
        If navStatus = NAV_STATUS_STRUCTURE_B Then
            isStructureB = True
            Return False
        End If

        ' Check for valid Structure-A values
        isStructureB = False
        Return ValidNavStatusValues.Contains(navStatus)
    End Function

    ''' <summary>
    ''' Validates Checksum 1 against packet data.
    ''' </summary>
    Public Shared Function ValidateChecksum1(data As Byte()) As Boolean
        Try
            Dim calculated As Byte = CalculateChecksum1(data)
            Dim received As Byte = data(CHECKSUM1_POSITION)
            Return calculated = received
        Catch ex As ArgumentException
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Validates Checksum 2 against packet data.
    ''' </summary>
    Public Shared Function ValidateChecksum2(data As Byte()) As Boolean
        Try
            Dim calculated As Byte = CalculateChecksum2(data)
            Dim received As Byte = data(CHECKSUM2_POSITION)
            Return calculated = received
        Catch ex As ArgumentException
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Validates Checksum 3 against packet data.
    ''' </summary>
    Public Shared Function ValidateChecksum3(data As Byte()) As Boolean
        Try
            Dim calculated As Byte = CalculateChecksum3(data)
            Dim received As Byte = data(CHECKSUM3_POSITION)
            Return calculated = received
        Catch ex As ArgumentException
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Performs complete validation of an NCOM Structure-A packet.
    ''' </summary>
    ''' <param name="data">Complete 72-byte NCOM packet</param>
    ''' <param name="validationResult">Detailed validation result</param>
    ''' <returns>True if packet is valid and usable</returns>
    Public Shared Function ValidatePacket(data As Byte(), ByRef validationResult As NcomValidationResult) As Boolean
        validationResult = New NcomValidationResult()

        ' Check packet size
        If data Is Nothing Then
            validationResult.ErrorMessage = "Packet data is null"
            Return False
        End If

        If data.Length <> NCOM_PACKET_SIZE Then
            validationResult.ErrorMessage = $"Invalid packet size: {data.Length} bytes (expected {NCOM_PACKET_SIZE})"
            Return False
        End If

        ' Validate sync byte
        validationResult.SyncByteValid = ValidateSyncByte(data)
        If Not validationResult.SyncByteValid Then
            validationResult.ErrorMessage = $"Invalid sync byte: 0x{data(0):X2} (expected 0x{SYNC_BYTE:X2})"
            Return False
        End If

        ' Validate navigation status
        Dim isStructureB As Boolean = False
        validationResult.NavigationStatusValid = ValidateNavigationStatus(data, isStructureB)
        validationResult.NavigationStatus = data(NAV_STATUS_POSITION)

        If isStructureB Then
            validationResult.ErrorMessage = "Structure-B packet detected (internal use only, should be ignored)"
            Return False
        End If

        If Not validationResult.NavigationStatusValid Then
            validationResult.ErrorMessage = $"Invalid navigation status: {data(NAV_STATUS_POSITION)} (reserved value)"
            Return False
        End If

        ' Validate all three checksums
        validationResult.Checksum1Valid = ValidateChecksum1(data)
        validationResult.Checksum2Valid = ValidateChecksum2(data)
        validationResult.Checksum3Valid = ValidateChecksum3(data)

        ' Build error message for checksum failures
        If Not validationResult.Checksum3Valid Then
            Dim calc As Byte = CalculateChecksum3(data)
            Dim recv As Byte = data(CHECKSUM3_POSITION)
            validationResult.ErrorMessage = $"Checksum 3 failed: calculated=0x{calc:X2}, received=0x{recv:X2} (entire packet corrupted)"
            Return False
        End If

        If Not validationResult.Checksum2Valid Then
            Dim calc As Byte = CalculateChecksum2(data)
            Dim recv As Byte = data(CHECKSUM2_POSITION)
            validationResult.ErrorMessage = $"Checksum 2 failed: calculated=0x{calc:X2}, received=0x{recv:X2} (Batch B corrupted)"
            Return False
        End If

        If Not validationResult.Checksum1Valid Then
            Dim calc As Byte = CalculateChecksum1(data)
            Dim recv As Byte = data(CHECKSUM1_POSITION)
            validationResult.ErrorMessage = $"Checksum 1 failed: calculated=0x{calc:X2}, received=0x{recv:X2} (Batch A corrupted)"
            Return False
        End If

        ' All validations passed
        validationResult.IsValid = True
        Return True
    End Function

    ''' <summary>
    ''' Quick validation for low-latency applications (Batch A only).
    ''' </summary>
    ''' <param name="data">NCOM packet data (minimum 23 bytes)</param>
    ''' <returns>True if Batch A data is valid and can be used immediately</returns>
    ''' <remarks>
    ''' For time-critical applications, this allows using inertial measurements
    ''' without waiting for the entire packet. See NCOM spec page 6.
    ''' </remarks>
    Public Shared Function ValidateBatchA(data As Byte()) As Boolean
        If Not ValidateSyncByte(data) Then Return False

        Dim isStructureB As Boolean = False
        If Not ValidateNavigationStatus(data, isStructureB) Then Return False

        Return ValidateChecksum1(data)
    End Function

    ''' <summary>
    ''' Medium-latency validation for navigation solution (Batch A + Batch B).
    ''' </summary>
    ''' <param name="data">NCOM packet data (minimum 62 bytes)</param>
    ''' <returns>True if full navigation solution is valid</returns>
    ''' <remarks>
    ''' Allows using position, velocity, and orientation without waiting
    ''' for status channels. See NCOM spec page 13.
    ''' </remarks>
    Public Shared Function ValidateBatchAB(data As Byte()) As Boolean
        If Not ValidateSyncByte(data) Then Return False

        Dim isStructureB As Boolean = False
        If Not ValidateNavigationStatus(data, isStructureB) Then Return False

        Return ValidateChecksum2(data)
    End Function
#End Region

End Class

''' <summary>
''' Detailed validation result for NCOM packet validation.
''' </summary>
Public Class NcomValidationResult
    ''' <summary>Overall validation result</summary>
    Public Property IsValid As Boolean = False

    ''' <summary>Sync byte validation result</summary>
    Public Property SyncByteValid As Boolean = False

    ''' <summary>Navigation status validation result</summary>
    Public Property NavigationStatusValid As Boolean = False

    ''' <summary>Navigation status value from packet</summary>
    Public Property NavigationStatus As Byte = 0

    ''' <summary>Checksum 1 (Batch A) validation result</summary>
    Public Property Checksum1Valid As Boolean = False

    ''' <summary>Checksum 2 (Batch A + B) validation result</summary>
    Public Property Checksum2Valid As Boolean = False

    ''' <summary>Checksum 3 (entire packet) validation result</summary>
    Public Property Checksum3Valid As Boolean = False

    ''' <summary>Detailed error message if validation failed</summary>
    Public Property ErrorMessage As String = String.Empty

    ''' <summary>
    ''' Returns a human-readable summary of the validation result.
    ''' </summary>
    Public Overrides Function ToString() As String
        If IsValid Then
            Return $"Valid NCOM packet (Nav Status: {NavigationStatus})"
        Else
            Return $"Invalid NCOM packet: {ErrorMessage}"
        End If
    End Function
End Class