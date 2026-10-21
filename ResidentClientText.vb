Option Strict Off
Option Explicit On

Imports System.Text

''' <summary>
''' Small string/parse helpers extracted from GmResidentClient (signal list, configuration and
''' recording filename handling). Behavior is unchanged from the original form methods.
''' </summary>
Friend NotInheritable Class ResidentClientText

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Returns True if the signal name is VIDEO_CAMERA_TIMECODE, ignoring case, surrounding whitespace
    ''' and leading/trailing quotes that may come from CSV export.
    ''' </summary>
    Friend Shared Function IsVideoTimecode(name As String) As Boolean
        If name Is Nothing Then Return False
        ' Trim whitespace and leading/trailing quotes that may come from CSV export
        Dim s = name.Trim().Trim(""""c)
        Return s.Equals("VIDEO_CAMERA_TIMECODE", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Cleans up a raster name that might end in "_f" by converting it to "-f".
    ''' </summary>
    Friend Shared Function CleanUpRasterName(ByVal raster As String) As String
        If raster.EndsWith("_f", StringComparison.OrdinalIgnoreCase) Then
            raster = raster.Substring(0, raster.Length - 2) & "-f"
        End If
        Return raster
    End Function

    ''' <summary>
    ''' Parses a space- or comma-separated list of doubles from config.xml, falling back to the
    ''' provided default if the text is missing, malformed, or the wrong length.
    ''' </summary>
    Friend Shared Function ParseDoubleVector(text As String, expectedLength As Integer, fallback As Double()) As Double()
        If String.IsNullOrWhiteSpace(text) Then
            Return fallback
        End If

        Dim parts As String() = text.Split(New Char() {" "c, ","c}, StringSplitOptions.RemoveEmptyEntries)
        If parts.Length <> expectedLength Then
            Return fallback
        End If

        Dim result(expectedLength - 1) As Double
        For i As Integer = 0 To expectedLength - 1
            If Not Double.TryParse(parts(i), result(i)) Then
                Return fallback
            End If
        Next

        Return result
    End Function

    ''' <summary>
    ''' Normalizes a filename string by removing invisible characters and whitespace.
    ''' Prevents string comparison failures due to hidden Unicode artifacts.
    ''' </summary>
    Friend Shared Function NormalizeFilename(ByVal filename As String) As String
        If String.IsNullOrEmpty(filename) Then Return String.Empty

        ' Step 1: Trim leading/trailing whitespace
        Dim result As String = filename.Trim()

        ' Step 2: Remove zero-width characters (U+200B, U+FEFF, etc.)
        result = result.Replace(ChrW(&H200B), String.Empty)  ' Zero-width space
        result = result.Replace(ChrW(&HFEFF), String.Empty)  ' BOM
        result = result.Replace(ChrW(&H200C), String.Empty)  ' Zero-width non-joiner
        result = result.Replace(ChrW(&H200D), String.Empty)  ' Zero-width joiner

        ' Step 3: Remove any null characters
        result = result.Replace(vbNullChar, String.Empty)

        ' Step 4: Normalize Unicode form (combine decomposed characters)
        result = result.Normalize(NormalizationForm.FormC)

        Return result
    End Function

End Class
