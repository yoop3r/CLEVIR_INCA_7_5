Option Strict On
Option Explicit On

Imports System.IO

''' <summary>
''' Signal list (INCAVariableFile) path rules. CSV is the only supported signal list format;
''' XLSX (Excel COM) signal lists are no longer read or written.
''' </summary>
Friend NotInheritable Class SignalListFile

    Friend Const Extension As String = ".csv"
    Friend Const RecordFileSuffix As String = "_RecordFile.csv"

    Private Sub New()
    End Sub

    ''' <summary>True when <paramref name="filePath"/> has a .csv extension (case-insensitive).</summary>
    Friend Shared Function IsCsv(filePath As String) As Boolean
        Return Not String.IsNullOrEmpty(filePath) AndAlso
               String.Equals(Path.GetExtension(filePath), Extension, StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Returns <paramref name="filePath"/> unchanged when it is already a .csv (or empty); otherwise the
    ''' same path with a .csv extension, so a legacy "list.xlsx" config entry maps to "list.csv".
    ''' </summary>
    Friend Shared Function ToCsvPath(filePath As String) As String
        If String.IsNullOrEmpty(filePath) OrElse IsCsv(filePath) Then Return filePath
        Return Path.ChangeExtension(filePath, Extension)
    End Function

    ''' <summary>
    ''' Default CLEVIR record (playback) file for a signal list: "{dir}\{name}_RecordFile.csv".
    ''' Never equal to the signal list itself.
    ''' </summary>
    Friend Shared Function RecordFilePathFor(signalListPath As String) As String
        Return Path.Combine(If(Path.GetDirectoryName(signalListPath), String.Empty),
                            Path.GetFileNameWithoutExtension(signalListPath) & RecordFileSuffix)
    End Function

    ''' <summary>
    ''' True when a file in the SignalLists folder may be offered as the "latest" signal list:
    ''' a .csv that is not a backup (*SAVE*), an Office lock/temp file (~), or a CLEVIR record file.
    ''' </summary>
    Friend Shared Function IsSignalListCandidate(fileName As String) As Boolean
        Return Not String.IsNullOrEmpty(fileName) AndAlso
               Not fileName.Contains("SAVE") AndAlso
               Not fileName.Contains("~") AndAlso
               fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) AndAlso
               Not fileName.EndsWith(RecordFileSuffix, StringComparison.OrdinalIgnoreCase)
    End Function
End Class
