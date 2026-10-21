Option Strict Off
Option Explicit On

''' <summary>
''' Display-string formatting for grid/custom-screen signal values, extracted from GmResidentClient.
''' Behavior (including Option Strict Off conversions and culture-dependent Format$) is unchanged.
''' </summary>
Friend NotInheritable Class DisplayValueFormatter

    Private Sub New()
    End Sub

    Friend Shared Function GetEnumDescription(
        ByVal MySignalEnums As List(Of SignalEnums),
        ByVal displayValue As Double,
        ByVal variableName As String,
        ByVal deviceName As String
    ) As String

        Dim result As String = "XXXX"
        Dim found As Boolean = False

        Try

            ' Only perform lookup if the list is non-null and has items
            If MySignalEnums IsNot Nothing AndAlso MySignalEnums.Count > 0 Then

                ' Loop through each SignalEnums object
                For Each signalEnum In MySignalEnums

                    ' Check if variable name doesn't contain "SDFIC"
                    If Not variableName.ToUpper().Contains("SDFIC") Then
                        If variableName.Contains(signalEnum.VariableName) Then
                            Dim idx As Integer = CInt(displayValue)

                            ' Confirm index is within the Enums list bounds
                            If idx >= 0 AndAlso idx < signalEnum.Enums.Count Then
                                Dim enumValue As String = signalEnum.Enums(idx)

                                ' Check the enumValue is not Nothing and device matches
                                If enumValue IsNot Nothing Then
                                    If signalEnum.DeviceName = deviceName OrElse
                                   signalEnum.DeviceName = "" Then
                                        result = enumValue
                                        found = True
                                        Exit For
                                    End If
                                Else
                                    result = "XXXX"
                                End If
                            End If
                        End If
                    Else
                        ' Special check for VaIDRR_e_DTC_List
                        If signalEnum.VariableName = "VaIDRR_e_DTC_List" Then
                            Dim idx As Integer = CInt(displayValue)
                            If idx >= 0 AndAlso idx < signalEnum.Enums.Count Then
                                Dim enumValue As String = signalEnum.Enums(idx)
                                If enumValue IsNot Nothing Then
                                    If signalEnum.DeviceName = deviceName OrElse
                                   signalEnum.DeviceName = "" Then
                                        result = enumValue
                                        found = True
                                        Exit For
                                    End If
                                Else
                                    result = "XXXX"
                                End If
                            End If
                        End If
                    End If
                Next
            End If

            ' If we never found a match, display numeric value instead
            If result = "XXXX" OrElse Not found Then
                result = Format(displayValue, "0")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GetEnumDescription: " & ex.Message & " - " & variableName)
            result = Format(displayValue, "0")
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Formats the string which will be displayed for a given variable. DisplayFormat is read in from the
    ''' signal list. The raw number can be displayed to the level of precision defined using a format string
    ''' such as "0" or "0.0", or other formats can be used such as "HEX" or "TRUE/FALSE", etc. If the
    ''' DisplayFormat is "ENUM", GetEnumDescription is called with <paramref name="signalEnums"/>.
    ''' </summary>
    Friend Shared Function FormatDisplayString(ByVal displayValue As Double, ByVal displayFormat As String, ByVal deviceName As String, ByVal variableName As String, ByVal signalEnums As List(Of SignalEnums)) As String

        Try
            FormatDisplayString = ""

            Select Case UCase(displayFormat)
                Case "ENUM"

                    FormatDisplayString = GetEnumDescription(signalEnums, CInt(displayValue), variableName, deviceName)
                    If Not IsNumeric(Mid(FormatDisplayString, 1, 1)) Then
                        FormatDisplayString = CStr(CInt(displayValue)) & " " & FormatDisplayString
                    End If

                Case "TRUE/FALSE"
                    FormatDisplayString = IIf(CInt(displayValue) = 0, "0 True", "1 False")
                Case "FALSE/TRUE"
                    FormatDisplayString = IIf(CInt(displayValue) = 0, "0 False", " 1 True")
                Case "INVALID/VALID"
                    FormatDisplayString = IIf(CInt(displayValue) = 0, "0 Invalid", " 1 Valid")
                Case "VALID/INVALID"
                    FormatDisplayString = IIf(CInt(displayValue) = 0, "0 Valid", "1 Invalid")
                Case "HEX"
                    FormatDisplayString = Hex(CStr(displayValue))
                Case "DEC"
                    FormatDisplayString = Format$(CInt(displayValue), "0")
                Case "BINARY"
                    FormatDisplayString = Convert.ToString(CLng(displayValue), 2).PadLeft(8, "0"c)
                Case Else 'assumes ""0"", or ""0.0"" or ""0.000"", etc....
                    FormatDisplayString = Format$(displayValue, displayFormat)
            End Select

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "FormatDisplayString: " & ex.Message & " - " & variableName)
            FormatDisplayString = "-"

        End Try

    End Function

    ''' <summary>
    ''' Returns a friendly name for common colors, otherwise returns the color's Name property.
    ''' </summary>
    Friend Shared Function GetColorName(ByVal color As Color) As String
        Select Case color
            Case Color.Red : Return "RED"
            Case Color.Green : Return "GREEN"
            Case Color.Gray : Return "GRAY"
            Case Color.Yellow : Return "YELLOW"
            Case Else : Return color.Name.ToUpper()
        End Select
    End Function

End Class
