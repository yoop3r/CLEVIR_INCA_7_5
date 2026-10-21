Option Strict On
Option Explicit On

Imports System.IO
Imports System.Threading

''' <summary>
''' Form-independent shutdown file housekeeping: vehicle data paths, orphaned-file discovery for compression,
''' GM_INCA_Comm.log archive names and wait/copy-with-retry helpers. Logging goes through a caller-supplied delegate.
''' </summary>
Friend NotInheritable Class FileMaintenance

    Friend Const CommLogFileName As String = "GM_INCA_Comm.log"

    Private Sub New()
    End Sub

    ''' <summary>"{baseDataCollectionPath}\Data\gmcsv{vehicleNumber}".</summary>
    Friend Shared Function VehicleDataRoot(baseDataCollectionPath As String, vehicleNumber As String) As String
        Return Path.Combine(baseDataCollectionPath, "Data", "gmcsv" & vehicleNumber)
    End Function

    ''' <summary>Archive path a data file is compressed to: same folder and name, .zip extension.</summary>
    Friend Shared Function ZipPathFor(dataFile As String) As String
        Return Path.ChangeExtension(dataFile, ".zip")
    End Function

    ''' <summary>
    ''' Files under <paramref name="dataRoot"/> (recursive) matching <paramref name="searchPattern"/> that have no
    ''' sibling .zip. Lazy: each file's .zip is checked only when that file is reached, so archives created while
    ''' the caller processes earlier files are seen.
    ''' </summary>
    Friend Shared Iterator Function FindOrphanedFiles(dataRoot As String, searchPattern As String) As IEnumerable(Of String)
        For Each dataFile In Directory.GetFiles(dataRoot, searchPattern, SearchOption.AllDirectories)
            If Not File.Exists(ZipPathFor(dataFile)) Then
                Yield dataFile
            End If
        Next
    End Function

    ''' <summary>Archived log name in the vehicle data folder: "{yyyMMdd_hhmmss}_GM_INCA_Comm.log" (12-hour clock).</summary>
    Friend Shared Function CommLogArchiveFileName(timestamp As Date) As String
        Return $"{Format(timestamp, "yyyMMdd_hhmmss")}_{CommLogFileName}"
    End Function

    ''' <summary>Archived log name in the application folder fallback: "{MMddyyyy_hhmmss}_GM_INCA_Comm.log" (12-hour clock).</summary>
    Friend Shared Function CommLogFallbackFileName(timestamp As Date) As String
        Return $"{Format(timestamp, "MMddyyyy_hhmmss")}_{CommLogFileName}"
    End Function

    ''' <summary>
    ''' Tries up to <paramref name="tries"/> times to open <paramref name="filePath"/> exclusively, sleeping
    ''' <paramref name="delayMs"/> after each failure. Returns True as soon as an exclusive open succeeds.
    ''' </summary>
    Friend Shared Function WaitUntilFileReady(filePath As String, tries As Integer, delayMs As Integer, log As Action(Of String)) As Boolean
        For i = 1 To Math.Max(0, tries)
            Try
                Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None)
                    Return True
                End Using
            Catch exWait As Exception
                log($"WaitUntilFileReady: {filePath} not ready - {exWait.Message}")
                Thread.Sleep(Math.Max(0, delayMs))
            End Try
        Next
        Return False
    End Function

    ''' <summary>
    ''' Copies <paramref name="src"/> over <paramref name="dest"/>, retrying up to <paramref name="maxRetries"/>
    ''' times (at least once) with <paramref name="delayMs"/> between failures.
    ''' </summary>
    Friend Shared Function CopyWithRetries(src As String, dest As String, maxRetries As Integer, delayMs As Integer, log As Action(Of String)) As Boolean
        For i = 1 To Math.Max(1, maxRetries)
            Try
                File.Copy(src, dest, overwrite:=True)
                log($"Copied Driver_Anno_Cons.xlsx to {dest}")
                Return True
            Catch ex As Exception
                log($"Retry {i}/{maxRetries} - Copy failed: {ex.Message}")
                Thread.Sleep(Math.Max(0, delayMs))
            End Try
        Next
        Return False
    End Function
End Class
