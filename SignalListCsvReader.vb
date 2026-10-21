Option Strict On
Option Explicit On

Imports System.IO
Imports Microsoft.VisualBasic.FileIO

''' <summary>
''' Parses a CSV signal list into the 1-based Object(,) layout the signal list code indexes with EXCEL_DATA.
''' </summary>
Friend NotInheritable Class SignalListCsvReader

    ''' <summary>Minimum column count so every EXCEL_DATA index is addressable.</summary>
    Friend Const MinimumColumns As Integer = 23

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Reads <paramref name="filePath"/>. The delimiter (tab, semicolon or comma) is detected from the first
    ''' non-blank line; fields are trimmed, quoted fields honored, and fully empty rows skipped.
    ''' Result is (0..rows, 0..columns) with data at (1..rows, 1..columns); short rows are padded with "".
    ''' </summary>
    ''' <exception cref="InvalidDataException">The file is empty or contains no data rows.</exception>
    Friend Shared Function Read(filePath As String) As Object(,)
        Dim firstNonEmpty As String = File.ReadLines(filePath).FirstOrDefault(Function(l) Not String.IsNullOrWhiteSpace(l))
        If firstNonEmpty Is Nothing Then Throw New InvalidDataException("CSV file is empty")

        Dim delim As String = DetectDelimiter(firstNonEmpty)

        Dim rows As New List(Of String())()
        Dim maxColumns As Integer = 0

        Using parser As New TextFieldParser(filePath)
            parser.TextFieldType = FieldType.Delimited
            parser.SetDelimiters(delim)
            parser.HasFieldsEnclosedInQuotes = True
            parser.TrimWhiteSpace = False

            While Not parser.EndOfData
                Dim fields As String() = parser.ReadFields()
                If fields Is Nothing Then Continue While

                For i As Integer = 0 To fields.Length - 1
                    If fields(i) IsNot Nothing Then fields(i) = fields(i).Trim()
                Next

                If fields.All(Function(f) String.IsNullOrEmpty(f)) Then Continue While

                rows.Add(fields)
                If fields.Length > maxColumns Then maxColumns = fields.Length
            End While
        End Using

        If rows.Count = 0 Then Throw New InvalidDataException("CSV contained no data rows")

        maxColumns = Math.Max(maxColumns, MinimumColumns)

        Dim data(rows.Count, maxColumns) As Object
        For r As Integer = 0 To rows.Count - 1
            Dim fields = rows(r)
            For c As Integer = 0 To maxColumns - 1
                If c < fields.Length Then
                    data(r + 1, c + 1) = fields(c)
                Else
                    data(r + 1, c + 1) = String.Empty
                End If
            Next
        Next

        Return data
    End Function

    ''' <summary>Most frequent of tab, semicolon, comma; ties resolve in that order.</summary>
    Friend Shared Function DetectDelimiter(line As String) As String
        Dim tabCount = line.Count(Function(ch) ch = ControlChars.Tab)
        Dim commaCount = line.Count(Function(ch) ch = ","c)
        Dim semiCount = line.Count(Function(ch) ch = ";"c)
        If tabCount >= commaCount AndAlso tabCount >= semiCount Then
            Return vbTab
        ElseIf semiCount >= commaCount AndAlso semiCount >= tabCount Then
            Return ";"
        Else
            Return ","
        End If
    End Function
End Class
