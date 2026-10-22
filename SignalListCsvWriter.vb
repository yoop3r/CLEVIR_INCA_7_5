Option Strict On
Option Explicit On

''' <summary>
''' Formats signal list values as comma-separated fields that <see cref="SignalListCsvReader"/> (and Excel) read back unchanged.
''' </summary>
Friend NotInheritable Class SignalListCsvWriter

    Private Sub New()
    End Sub

    ''' <summary>
    ''' RFC 4180 field: values containing a quote, comma or line break are wrapped in quotes with embedded quotes doubled,
    ''' e.g. the DisplayFormat <c>""0""</c> becomes <c>"""""0"""""</c>. Nothing/DBNull become an empty field.
    ''' </summary>
    Friend Shared Function FormatField(value As Object) As String
        If value Is Nothing OrElse TypeOf value Is DBNull Then Return String.Empty

        Dim text As String = CStr(value)
        If text.IndexOfAny({""""c, ","c, ControlChars.Cr, ControlChars.Lf}) < 0 Then Return text

        Return """" & text.Replace("""", """""") & """"
    End Function
End Class
