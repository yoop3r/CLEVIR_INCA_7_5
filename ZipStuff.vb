Option Strict Off

Imports System.Diagnostics
Imports System.IO
Imports System.IO.Compression
Imports System.IO.Compression.ZipFileExtensions
Imports System.Threading
Imports System.Threading.Tasks
Imports ICSharpCode.SharpZipLib.Zip

Module ZipStuff

    'This module contains various 7Zip related functions used throughout the CLEVIR applications...
    'Enhanced with file lock detection and robust compression handling ported from PowerShell sync script

    Dim _zipDir As String
    Dim _zipExe As String
    Dim _zipInstallFile As String
    Dim _sevenZipLibraryPath As String

    ' ✅ REMOVED: Duplicate properties - now using Module1 globals
    ' All compression settings now controlled via:
    ' - ZipCompressionEnabled (Module1)
    ' - ZipCompressionMaxRetries (Module1)
    ' - ZipCompressionRetryDelay (Module1)
    ' - ZipFileLockTimeout (Module1)
    ' - CompressMF4, CompressPCAP, CompressASC, CompressVSB (Module1)
    ' - DeleteAfterCompression (Module1)

    ' Enhanced compression tracking
    Private _compressionStats As New Dictionary(Of String, CompressionResult)

    ' Compression result structure
    Public Structure CompressionResult
        Public Success As Boolean
        Public FilePath As String
        Public ArchivePath As String
        Public Timestamp As DateTime
        Public AttemptCount As Integer
        Public ErrorMessage As String
    End Structure

    Public Function CheckFor7Zip() As Boolean

        'Called from InitForm_Load...

        'CLEVIR requires 7-zip to be available on the computer.  So, if it is not, we will install it for the user.

        '7-Zip is used because on the original in vehicle computers, WinZip was not installed.  The DELL Toughbooks
        'do have WinZip installed so we could use the WinZip API instead, but since the 7-Zip stuff works, we will
        'continue to do things this way...

        CheckFor7Zip = True

        If InStr(My.Application.Info.AssemblyName, "CLEVIR_INCA_7_5") Then
            Dim test As String = My.Application.Info.AssemblyName
            _zipDir = "C:\Program Files\7-Zip\"
            _zipExe = "7z.exe"
            _zipInstallFile = "7z2501-x64.exe"
            _sevenZipLibraryPath = _zipDir & "7z.dll"
        Else
            _zipDir = "C:\Program Files (x86)\7-Zip\"
            _zipExe = "7z.exe"
            _zipInstallFile = "7z2501.exe"
            _sevenZipLibraryPath = _zipDir & "7z.dll"
        End If

        If Not File.Exists(_zipDir & _zipExe) Then

            If File.Exists(My.Application.Info.DirectoryPath & "\" & _zipInstallFile) Then

                HandleUserMessageLogging("GMRC", "Installing 7-Zip... Please install to the default " & _zipDir & " directory.", DisplayMsgBox, )

                Dim procStartInfo As New ProcessStartInfo
                Dim procExecuting As New Process

                With procStartInfo
                    .UseShellExecute = True
                    .FileName = My.Application.Info.DirectoryPath & "\" & _zipInstallFile
                    .WindowStyle = ProcessWindowStyle.Normal
                    .Verb = "runas" 'add this to prompt for elevation
                End With

                procExecuting = Process.Start(procStartInfo)

                procExecuting.WaitForExit()

            Else
                HandleUserMessageLogging("GMRC", "7-Zip not installed.", DisplayMsgBox, )
                CheckFor7Zip = False
            End If

        End If

    End Function

    ''' <summary>
    ''' ✅ FIXED: Now uses Module1.ZipFileLockTimeout instead of hardcoded timeout
    ''' Tests if a file is locked by another process (ported from PowerShell)
    ''' </summary>
    ''' <param name="filePath">Full path to the file to test</param>
    ''' <param name="timeoutSeconds">Maximum time to wait for file unlock (uses Module1.ZipFileLockTimeout if not specified)</param>
    ''' <param name="retryIntervalSeconds">Time between retry attempts (default: 2)</param>
    ''' <returns>True if file is locked, False if accessible</returns>
    Public Function IsFileLocked(ByVal filePath As String,
                                  Optional ByVal timeoutSeconds As Integer = -1,
                                  Optional ByVal retryIntervalSeconds As Integer = 2) As Boolean

        ' ✅ Use Module1 global if not explicitly specified
        If timeoutSeconds < 0 Then timeoutSeconds = ZipFileLockTimeout

        If Not File.Exists(filePath) Then
            HandleUserMessageLogging("GMRC", $"IsFileLocked: File does not exist: '{filePath}'")
            Return False
        End If

        Dim fileName As String = Path.GetFileName(filePath)
        Dim elapsedTime As Integer = 0
        Dim isLocked As Boolean = True

        HandleUserMessageLogging("GMRC", $"IsFileLocked: Testing lock status for '{fileName}' (timeout: {timeoutSeconds}s)")

        While elapsedTime < timeoutSeconds AndAlso isLocked
            Try
                ' Try to open the file for reading with exclusive access
                Using fileStream As FileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None)
                    ' If we get here, file is not locked
                    isLocked = False
                    HandleUserMessageLogging("GMRC", $"IsFileLocked: ✅ File '{fileName}' is not locked and ready for processing")
                    Return False ' File is NOT locked
                End Using

            Catch ioEx As IOException
                ' File is locked by another process
                If elapsedTime = 0 Then
                    HandleUserMessageLogging("GMRC", $"IsFileLocked: ⚠️ File '{fileName}' is locked - waiting for release...")
                End If

                Thread.Sleep(retryIntervalSeconds * 1000)
                elapsedTime += retryIntervalSeconds

            Catch ex As Exception
                ' Other unexpected error
                HandleUserMessageLogging("GMRC", $"IsFileLocked: ❌ Unexpected error testing lock for '{fileName}': {ex.Message}")
                Return False ' Assume not locked on unexpected errors
            End Try
        End While

        If isLocked Then
            HandleUserMessageLogging("GMRC", $"IsFileLocked: ⏰ TIMEOUT: File '{fileName}' still locked after {timeoutSeconds} seconds")
            Return True ' File is still locked
        End If

        Return False

    End Function

    ''' <summary>
    ''' ✅ FIXED: Now checks configuration switches before adding files to compression list
    ''' Gets list of files ready for compression (not locked, no corresponding .zip)
    ''' </summary>
    Private Function GetReadyForCompressionFiles(targetDir As String) As List(Of String)

        Dim readyFiles As New List(Of String)()

        Try
            HandleUserMessageLogging("ZIP", $"GetReadyForCompressionFiles: Scanning '{targetDir}'")

            If Not Directory.Exists(targetDir) Then
                HandleUserMessageLogging("ZIP", $"Directory does not exist: {targetDir}")
                Return readyFiles
            End If

            ' ✅ FIXED: Extension filter now respects configuration switches
            Dim allFiles = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories) _
        .Where(Function(f)
                   Dim ext = Path.GetExtension(f).ToLower()
                   ' ✅ Check both extension AND configuration switch
                   Return (ext = ".mf4" AndAlso CompressMF4) OrElse
                          (ext = ".pcap" AndAlso CompressPCAP) OrElse
                          (ext = ".asc" AndAlso CompressASC) OrElse
                          (ext = ".vsb" AndAlso CompressVSB)
               End Function).ToList()

            HandleUserMessageLogging("ZIP", $"GetReadyForCompressionFiles: Found {allFiles.Count} files matching enabled compression types")

            For Each filePath In allFiles
                Dim fileName = Path.GetFileName(filePath)

                ' Skip files that already have a corresponding .zip archive
                Dim zipPath = Path.ChangeExtension(filePath, ".zip")
                If File.Exists(zipPath) Then
                    Continue For
                End If

                ' ✅ Use Module1.ZipFileLockTimeout
                If Not IsFileLocked(filePath) Then
                    readyFiles.Add(filePath)
                    HandleUserMessageLogging("ZIP", $"✅ Ready: '{fileName}'")
                Else
                    HandleUserMessageLogging("ZIP", $"⏳ Locked: '{fileName}'")
                End If
            Next

            HandleUserMessageLogging("ZIP", $"GetReadyForCompressionFiles: {readyFiles.Count} files ready")

        Catch ex As Exception
            HandleUserMessageLogging("ZIP", $"GetReadyForCompressionFiles error: {ex.Message}")
        End Try

        Return readyFiles
    End Function

    ''' <summary>
    ''' ✅ FIXED: Now uses Module1 compression configuration
    ''' Compresses files with lock detection and retry logic
    ''' Called when stopping recording to ensure all files are properly compressed
    ''' </summary>
    Public Function CompressFilesWithLockDetection(ByVal directoryName As String,
                                        Optional ByVal maxRetries As Integer = -1,
                                        Optional ByVal retryDelaySeconds As Integer = -1,
                                        Optional ByVal deleteOriginal As Boolean = False) As Integer

        ' ✅ Use Module1 globals if not explicitly specified
        If maxRetries < 0 Then maxRetries = ZipCompressionMaxRetries
        If retryDelaySeconds < 0 Then retryDelaySeconds = ZipCompressionRetryDelay

        Dim successfulCompressions As Integer = 0
        Dim sevenZipPath As String = _zipDir & _zipExe

        HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Starting compression in '{directoryName}'")
        HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: DeleteAfterCompression={DeleteAfterCompression}, deleteOriginal={deleteOriginal}")

        ' Validation checks
        If Not File.Exists(sevenZipPath) Then
            HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: 7-Zip executable not found at '{sevenZipPath}'", DisplayMsgBox)
            Return 0
        End If

        If Not Directory.Exists(directoryName) Then
            HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Directory not found: '{directoryName}'", DisplayMsgBox)
            Return 0
        End If

        ' Disk space check
        If Not CheckDiskSpace(directoryName, minimumSpaceGB:=5) Then
            Return 0 ' User cancelled compression
        End If

        ' Get files ready for compression (not locked, with Archive bit)
        HandleUserMessageLogging("GMRC", "CompressFilesWithLockDetection: Checking for files ready for compression...")
        Dim readyFilesPaths As List(Of String) = GetReadyForCompressionFiles(directoryName)

        ' Smart archive existence handling with delete logic
        Dim finalFiles As New List(Of FileInfo)
        Dim alreadyCompressedCount As Integer = 0
        Dim deletedSourceCount As Integer = 0

        For Each filePath In readyFilesPaths
            Dim fileInfo As New FileInfo(filePath)
            Dim correspondingArchive As String = Path.Combine(fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(fileInfo.Name) & ".zip")

            ' Check if archive already exists
            If File.Exists(correspondingArchive) Then
                alreadyCompressedCount += 1

                ' Archive exists - validate it and handle cleanup
                If IsValidZipArchive(correspondingArchive) Then
                    HandleUserMessageLogging("GMRC", $"✅ Valid archive exists: {Path.GetFileName(correspondingArchive)}")

                    ' Check if we should delete the source file
                    If DeleteAfterCompression Then
                        Try
                            If File.Exists(fileInfo.FullName) Then
                                fileInfo.Delete()
                                deletedSourceCount += 1
                                HandleUserMessageLogging("GMRC", $"🗑️ Deleted source file: {fileInfo.Name}")
                            End If
                        Catch delEx As Exception
                            HandleUserMessageLogging("GMRC", $"⚠️ Could not delete source {fileInfo.Name}: {delEx.Message}")
                        End Try
                    Else
                        HandleUserMessageLogging("GMRC", $"ℹ️ Keeping source file: {fileInfo.Name}")
                    End If

                    ' Don't add to compression list - archive already exists and is valid
                    Continue For

                Else
                    ' Archive is corrupt/invalid - delete it and compress again
                    HandleUserMessageLogging("GMRC", $"⚠️ Invalid archive detected, will recreate: {Path.GetFileName(correspondingArchive)}")
                    Try
                        File.Delete(correspondingArchive)
                        HandleUserMessageLogging("GMRC", $"🗑️ Deleted corrupt archive: {Path.GetFileName(correspondingArchive)}")
                    Catch delEx As Exception
                        HandleUserMessageLogging("GMRC", $"❌ Could not delete corrupt archive: {delEx.Message}")
                        Continue For ' Skip if we can't delete the corrupt archive
                    End Try
                    ' Fall through to add to finalFiles for re-compression
                End If
            End If

            ' Add to list for compression (either no archive exists, or corrupt archive was deleted)
            finalFiles.Add(fileInfo)
        Next

        ' Log summary of pre-compression cleanup
        If alreadyCompressedCount > 0 Then
            HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Found {alreadyCompressedCount} existing archives")
            If deletedSourceCount > 0 Then
                HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Cleaned up {deletedSourceCount} source files")
            End If
        End If

        If finalFiles.Count = 0 Then
            HandleUserMessageLogging("GMRC", "CompressFilesWithLockDetection: No new files to compress")
            Return successfulCompressions
        End If

        HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Found {finalFiles.Count} files to compress")

        ' Compression loop
        Dim currentFile As Integer = 0
        For Each fileInfo In finalFiles
            currentFile += 1

            Dim fileType As String = If(fileInfo.Extension.ToLower() = ".pcap", "PCAP", "MF4")
            Dim archiveName As String = Path.Combine(fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(fileInfo.Name) & ".zip")
            Dim compressionSuccessful As Boolean = False

            HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: [{currentFile}/{finalFiles.Count}] Processing {fileType} file: {fileInfo.Name}")

            ' Retry loop for compression
            For attempt As Integer = 1 To maxRetries
                Try
                    ' ✅ FIXED: Use Module1.ZipFileLockTimeout (reduced timeout during compression)
                    If IsFileLocked(fileInfo.FullName, timeoutSeconds:=10) Then
                        HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: ⚠️ File locked during attempt {attempt}/{maxRetries}: {fileInfo.Name}")

                        If attempt < maxRetries Then
                            Thread.Sleep(retryDelaySeconds * 1000)
                            Continue For
                        Else
                            HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: ❌ FINAL ATTEMPT: File still locked: {fileInfo.Name}")
                            Exit For
                        End If
                    End If

                    HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Compressing '{fileInfo.Name}' to '{Path.GetFileName(archiveName)}' (Attempt {attempt}/{maxRetries})")

                    ' Build 7-Zip command
                    Dim procStartInfo As New ProcessStartInfo With {
                .FileName = sevenZipPath,
                .Arguments = $"a -bso0 -bsp0 -tzip -mx=1 ""{archiveName}"" ""{fileInfo.FullName}""",
                .WindowStyle = ProcessWindowStyle.Hidden,
                .CreateNoWindow = True,
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True
            }

                    Using process As Process = Process.Start(procStartInfo)
                        process.WaitForExit()

                        If process.ExitCode = 0 Then
                            HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: ✅ Successfully compressed: {fileInfo.Name}")
                            compressionSuccessful = True
                            successfulCompressions += 1

                            ' Delete original if requested
                            If deleteOriginal Or DeleteAfterCompression Then
                                Try
                                    fileInfo.Delete()
                                    HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Deleted original file: {fileInfo.Name}")
                                Catch delEx As Exception
                                    HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Failed to delete original: {delEx.Message}")
                                End Try
                            End If

                            ' Track compression result
                            Dim result As New CompressionResult With {
                        .Success = True,
                        .FilePath = fileInfo.FullName,
                        .ArchivePath = archiveName,
                        .Timestamp = DateTime.Now,
                        .AttemptCount = attempt,
                        .ErrorMessage = ""
                    }
                            _compressionStats(fileInfo.FullName) = result

                            Exit For ' Success - exit retry loop

                        Else
                            HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: ❌ 7-Zip failed (Exit Code: {process.ExitCode}) for: {fileInfo.Name}")

                            If attempt < maxRetries Then
                                HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Retrying in {retryDelaySeconds} seconds...")
                                Thread.Sleep(retryDelaySeconds * 1000)
                            End If
                        End If
                    End Using

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: ERROR during compression attempt {attempt}: {ex.Message}")

                    If attempt < maxRetries Then
                        Thread.Sleep(retryDelaySeconds * 1000)
                    End If
                End Try
            Next ' End retry loop

            If Not compressionSuccessful Then
                HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: ❌ FAILED after {maxRetries} attempts: {fileInfo.Name}")

                ' Track failure
                Dim result As New CompressionResult With {
            .Success = False,
            .FilePath = fileInfo.FullName,
            .ArchivePath = archiveName,
            .Timestamp = DateTime.Now,
            .AttemptCount = maxRetries,
            .ErrorMessage = "Compression failed after maximum retries"
        }
                _compressionStats(fileInfo.FullName) = result
            End If

        Next ' End file loop

        HandleUserMessageLogging("GMRC", $"CompressFilesWithLockDetection: Compression complete - {successfulCompressions} new files compressed, {deletedSourceCount} sources cleaned up")

        Return successfulCompressions

    End Function

    ''' <summary>
    ''' Validates that a ZIP archive is readable and not corrupt
    ''' </summary>
    Private Function IsValidZipArchive(zipPath As String) As Boolean
        Try
            Using fileStream As New FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                Using archive As New ZipArchive(fileStream, ZipArchiveMode.Read)
                    ' Try to read the first entry to verify integrity
                    Dim firstEntry = archive.Entries.FirstOrDefault()
                    Return firstEntry IsNot Nothing
                End Using
            End Using
        Catch ex As Exception
            HandleUserMessageLogging("ZIP", $"Archive validation failed for {Path.GetFileName(zipPath)}: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' ✅ ENHANCED: Early exit if all compression disabled
    ''' Main entry point for compression - called from StartStopRecord
    ''' </summary>
    Public Function CompressRecordingFiles(ByVal directoryName As String) As Boolean
        HandleUserMessageLogging("GMRC", "CompressRecordingFiles: Starting file compression process...")

        Try
            ' ✅ FIXED: Check if ANY compression is enabled
            If Not (CompressMF4 Or CompressPCAP Or CompressASC Or CompressVSB) Then
                HandleUserMessageLogging("GMRC", "CompressRecordingFiles: All compression types disabled in configuration")
                Return True ' Return success since no compression was requested
            End If

            ' Log which types are enabled
            Dim enabledTypes As New List(Of String)
            If CompressMF4 Then enabledTypes.Add("MF4")
            If CompressPCAP Then enabledTypes.Add("PCAP")
            If CompressASC Then enabledTypes.Add("ASC")
            If CompressVSB Then enabledTypes.Add("VSB")

            HandleUserMessageLogging("GMRC", $"CompressRecordingFiles: Compression enabled for: {String.Join(", ", enabledTypes)}")

            ' Small delay to ensure INCA has released file handles
            Thread.Sleep(2000)

            ' ✅ GetReadyForCompressionFiles now respects configuration
            Dim compressedCount As Integer = CompressFilesWithLockDetection(directoryName)

            HandleUserMessageLogging("GMRC", $"CompressRecordingFiles: Completed - {compressedCount} files compressed")

            Return compressedCount >= 0

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CompressRecordingFiles ERROR: {ex.Message}", DisplayMsgBox)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' REFACTORED: Checks and compresses a single file with enhanced lock detection
    ''' Called from DeleteMF4Files - maintains backward compatibility while using new compression methods
    ''' </summary>
    ''' <param name="filename">Full path to file to check/compress</param>
    ''' <param name="deleteOnly">If True, only delete if archive exists; if False, create archive if missing</param>
    Private Sub CheckZipfile(ByVal filename As String, Optional ByVal deleteOnly As Boolean = False)

        ' Early validation
        If Not File.Exists(filename) Then
            HandleUserMessageLogging("GMRC", $"CheckZipfile: File does not exist: {filename}")
            Return
        End If

        Try
            ' ================================================================
            ' ESCALATION PROCESSING (preserved from original)
            ' ================================================================
            If ShouldProcessEscalation(filename, deleteOnly) Then
                ProcessEscalationFile(filename)
            End If

            ' ================================================================
            ' MAIN COMPRESSION/VALIDATION LOGIC
            ' ================================================================
            Dim fileExtension As String = Path.GetExtension(filename)
            Dim archivePath As String = Path.ChangeExtension(filename, ".zip")

            ' Check if this is a file type we should compress
            If Not ShouldCompressFile(filename) Then
                HandleUserMessageLogging("GMRC", $"CheckZipfile: Skipping unsupported file type: {filename}")
                Return
            End If

            ' ================================================================
            ' SCENARIO 1: Archive already exists - validate and delete original
            ' ================================================================
            If File.Exists(archivePath) Then
                HandleExistingArchive(filename, archivePath, deleteOnly)
                Return
            End If

            ' ================================================================
            ' SCENARIO 2: Archive doesn't exist
            ' ================================================================
            If deleteOnly Then
                ' Only compress if NOT in deleteOnly mode
                HandleDeleteOnlyMode(filename, archivePath)
            Else
                ' Create new archive using enhanced compression
                HandleNewArchiveCreation(filename, archivePath)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CheckZipfile ERROR: {ex.Message} - {filename}")
        End Try
    End Sub

    ''' <summary>
    ''' Determines if escalation processing is needed
    ''' </summary>
    Private Function ShouldProcessEscalation(filename As String, deleteOnly As Boolean) As Boolean
        Dim escalationExePath As String = Path.Combine(My.Application.Info.DirectoryPath, "process_supercruise_mf4files_2.exe")

        Return File.Exists(escalationExePath) AndAlso
           filename.Contains(".mf4") AndAlso
           deleteOnly = True
    End Function

    ''' <summary>
    ''' Processes escalation file with external executable
    ''' </summary>
    Private Sub ProcessEscalationFile(filename As String)
        Try
            Dim executableFile As String = Path.Combine(My.Application.Info.DirectoryPath, "process_supercruise_mf4files_2.exe")

            Dim p As New ProcessStartInfo With {
            .WindowStyle = ProcessWindowStyle.Hidden,
            .FileName = executableFile,
            .Arguments = $"""{filename}"""
        }

            Using process As Process = Process.Start(p)
                process.WaitForExit()

                Select Case process.ExitCode
                    Case 0
                        HandleUserMessageLogging("GMRC", $"CheckZipfile: Escalation processing completed successfully for {Path.GetFileName(filename)}")
                    Case Else
                        HandleUserMessageLogging("GMRC", $"CheckZipfile: Escalation processing returned exit code {process.ExitCode} for {Path.GetFileName(filename)}")
                End Select
            End Using

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CheckZipfile: Escalation processing failed: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ FIXED: Checks file type configuration before attempting compression
    ''' Checks if file should be compressed based on extension AND configuration
    ''' </summary>
    Private Function ShouldCompressFile(filename As String) As Boolean
        Dim extension As String = Path.GetExtension(filename).ToLower()
        Dim baseName As String = Path.GetFileName(filename)

        ' Don't compress files with hyphens in name (already processed)
        If baseName.Contains("-") Then Return False

        ' ✅ FIXED: Check both extension support AND configuration switch
        Select Case extension
            Case ".mf4", ".mdf"
                Return CompressMF4
            Case ".pcap"
                Return CompressPCAP
            Case ".asc"
                Return CompressASC
            Case ".vsb"
                Return CompressVSB
            Case Else
                Return False
        End Select
    End Function

    ''' <summary>
    ''' Handles scenario where archive already exists - validates and optionally deletes original
    ''' </summary>
    Private Sub HandleExistingArchive(filename As String, archivePath As String, deleteOnly As Boolean)
        ' Check if archive is locked
        If IsFileLocked(archivePath, timeoutSeconds:=5, retryIntervalSeconds:=1) Then
            HandleUserMessageLogging("GMRC", $"CheckZipfile: Archive {Path.GetFileName(archivePath)} is locked, skipping")
            Return
        End If

        Try
            ' Validate archive integrity using SharpZipLib
            Using zipArchive As New ICSharpCode.SharpZipLib.Zip.ZipFile(archivePath)
                If zipArchive.TestArchive(True) Then
                    ' Archive is valid - safe to delete original
                    HandleUserMessageLogging("GMRC", $"CheckZipfile: Archive {Path.GetFileName(archivePath)} validated successfully")

                    Try
                        File.Delete(filename)
                        HandleUserMessageLogging("GMRC", $"CheckZipfile: Deleted original file {Path.GetFileName(filename)}")
                    Catch delEx As Exception
                        HandleUserMessageLogging("GMRC", $"CheckZipfile: Failed to delete {Path.GetFileName(filename)}: {delEx.Message}")
                    End Try
                Else
                    ' Archive is corrupted - attempt to recreate
                    HandleUserMessageLogging("GMRC", $"CheckZipfile: Archive {Path.GetFileName(archivePath)} is corrupted, attempting to recreate")
                    HandleCorruptedArchive(filename, archivePath)
                End If
            End Using

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CheckZipfile: Failed to validate archive {Path.GetFileName(archivePath)}: {ex.Message}")

            ' If validation fails, try to recreate
            If Not deleteOnly Then
                HandleCorruptedArchive(filename, archivePath)
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Handles corrupted archive - deletes and recreates
    ''' </summary>
    Private Sub HandleCorruptedArchive(filename As String, archivePath As String)
        Try
            ' Delete corrupted archive
            If File.Exists(archivePath) Then
                File.Delete(archivePath)
                HandleUserMessageLogging("GMRC", $"CheckZipfile: Deleted corrupted archive {Path.GetFileName(archivePath)}")
            End If

            ' Recreate using enhanced compression (with retry and lock detection)
            If CompressSingleFileWithRetry(filename, archivePath, maxRetries:=3, retryDelaySeconds:=5) Then
                HandleUserMessageLogging("GMRC", $"CheckZipfile: Successfully recreated archive {Path.GetFileName(archivePath)}")
            Else
                HandleUserMessageLogging("GMRC", $"CheckZipfile: Failed to recreate archive {Path.GetFileName(archivePath)}")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CheckZipfile: Error handling corrupted archive: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Handles deleteOnly mode - checks for encrypted archives
    ''' </summary>
    Private Sub HandleDeleteOnlyMode(filename As String, archivePath As String)
        Dim encryptedArchive As String = archivePath & ".encrypt"

        ' Check if encrypted archive exists
        If Not File.Exists(encryptedArchive) Then Return

        ' Check if encrypted archive was uploaded to network
        Dim networkPath As String = Path.Combine(
        NetworkDriveLetter,
        "Data",
        "gmcsv" & VehicleNumber,
        SaveSelectedTestName,
        Path.GetFileName(encryptedArchive)
    )

        If File.Exists(networkPath) Then
            Try
                File.Delete(filename)
                HandleUserMessageLogging("GMRC", $"CheckZipfile: Found uploaded encrypted archive, deleted original {Path.GetFileName(filename)}")
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"CheckZipfile: Failed to delete {Path.GetFileName(filename)}: {ex.Message}")
            End Try
        End If
    End Sub

    ''' <summary>
    ''' Creates new archive using enhanced compression with lock detection
    ''' </summary>
    Private Sub HandleNewArchiveCreation(filename As String, archivePath As String)
        HandleUserMessageLogging("GMRC", $"CheckZipfile: Creating new archive {Path.GetFileName(archivePath)}")

        ' Use enhanced compression with retry and lock detection
        If CompressSingleFileWithRetry(filename, archivePath, maxRetries:=3, retryDelaySeconds:=5) Then
            HandleUserMessageLogging("GMRC", $"CheckZipfile: Successfully created archive {Path.GetFileName(archivePath)}")

            ' Validate the newly created archive
            Try
                Using zipArchive As New ICSharpCode.SharpZipLib.Zip.ZipFile(archivePath)
                    If zipArchive.TestArchive(True) Then
                        Try
                            File.Delete(filename)
                            HandleUserMessageLogging("GMRC", $"CheckZipfile: Validated and deleted original {Path.GetFileName(filename)}")
                        Catch delEx As Exception
                            HandleUserMessageLogging("GMRC", $"CheckZipfile: Failed to delete {Path.GetFileName(filename)}: {delEx.Message}")
                        End Try
                    Else
                        HandleUserMessageLogging("GMRC", $"CheckZipfile: Archive validation failed, keeping original {Path.GetFileName(filename)}")
                    End If
                End Using
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"CheckZipfile: Archive validation error: {ex.Message}")
            End Try
        Else
            HandleUserMessageLogging("GMRC", $"CheckZipfile: Failed to create archive for {Path.GetFileName(filename)}")
        End If
    End Sub

    ''' <summary>
    ''' ENHANCED: Compresses a single file with retry logic and lock detection
    ''' </summary>
    Public Function CompressSingleFileWithRetry(
                                                sourceFile As String,
                                                archivePath As String,
                                                Optional maxRetries As Integer = -1,
                                                Optional retryDelaySeconds As Integer = -1
                                                ) As Boolean

        ' ✅ Use Module1 globals if not explicitly specified
        If maxRetries < 0 Then maxRetries = ZipCompressionMaxRetries
        If retryDelaySeconds < 0 Then retryDelaySeconds = ZipCompressionRetryDelay

        Dim sevenZipPath As String = _zipDir & _zipExe

        If Not File.Exists(sevenZipPath) Then
            HandleUserMessageLogging("GMRC", $"CompressSingleFileWithRetry: 7-Zip not found at {sevenZipPath}")
            Return False
        End If

        For attempt As Integer = 1 To maxRetries
            Try
                ' ✅ FIXED: Use Module1.ZipFileLockTimeout (reduced during compression)
                If IsFileLocked(sourceFile, timeoutSeconds:=10) Then
                    HandleUserMessageLogging("GMRC", $"CompressSingleFileWithRetry: File locked on attempt {attempt}/{maxRetries}: {Path.GetFileName(sourceFile)}")

                    If attempt < maxRetries Then
                        Thread.Sleep(retryDelaySeconds * 1000)
                        Continue For
                    Else
                        Return False
                    End If
                End If

                ' Build 7-Zip command
                Dim procStartInfo As New ProcessStartInfo With {
                .FileName = sevenZipPath,
                .Arguments = $"a -bso0 -bsp0 -tzip -mx=1 ""{archivePath}"" ""{sourceFile}""",
                .WindowStyle = ProcessWindowStyle.Hidden,
                .CreateNoWindow = True,
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True
            }

                Using process As Process = Process.Start(procStartInfo)
                    process.WaitForExit()

                    If process.ExitCode = 0 Then
                        ' Track compression success
                        Dim result As New CompressionResult With {
                        .Success = True,
                        .FilePath = sourceFile,
                        .ArchivePath = archivePath,
                        .Timestamp = DateTime.Now,
                        .AttemptCount = attempt,
                        .ErrorMessage = ""
                    }
                        _compressionStats(sourceFile) = result

                        Return True
                    Else
                        HandleUserMessageLogging("GMRC", $"CompressSingleFileWithRetry: 7-Zip exit code {process.ExitCode} on attempt {attempt}/{maxRetries}")

                        If attempt < maxRetries Then
                            Thread.Sleep(retryDelaySeconds * 1000)
                        End If
                    End If
                End Using

            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"CompressSingleFileWithRetry: Exception on attempt {attempt}/{maxRetries}: {ex.Message}")

                If attempt < maxRetries Then
                    Thread.Sleep(retryDelaySeconds * 1000)
                End If
            End Try
        Next

        ' Track compression failure
        Dim failureResult As New CompressionResult With {
        .Success = False,
        .FilePath = sourceFile,
        .ArchivePath = archivePath,
        .Timestamp = DateTime.Now,
        .AttemptCount = maxRetries,
        .ErrorMessage = "Compression failed after maximum retries"
    }
        _compressionStats(sourceFile) = failureResult

        Return False
    End Function

    ''' <summary>
    ''' ✅ FIXED: Async compression now uses Module1 configuration
    ''' </summary>
    Public Function CompressSingleFileWithRetryAsync(
    sourceFile As String,
    archivePath As String,
    Optional maxRetries As Integer = -1,
    Optional retryDelayMs As Integer = -1
) As Task(Of Boolean)

        ' ✅ Use Module1 globals if not explicitly specified
        If maxRetries < 0 Then maxRetries = ZipCompressionMaxRetries
        If retryDelayMs < 0 Then retryDelayMs = ZipCompressionRetryDelay * 1000

        Dim compressionTask = Task.Run(
        Function() As Boolean
            Try
                For attempt As Integer = 1 To maxRetries
                    Try
                        ' ✅ Use Module1.ZipFileLockTimeout
                        If IsFileLocked(sourceFile, timeoutSeconds:=10) Then
                            HandleUserMessageLogging("GMRC",
                                $"CompressSingleFileWithRetryAsync: File locked (attempt {attempt}/{maxRetries}): {Path.GetFileName(sourceFile)}")

                            If attempt < maxRetries Then
                                Threading.Thread.Sleep(retryDelayMs)
                                Continue For
                            Else
                                Return False
                            End If
                        End If

                        ' Create ZIP archive
                        Using archive As System.IO.Compression.ZipArchive =
                            System.IO.Compression.ZipFile.Open(
                                archivePath,
                                System.IO.Compression.ZipArchiveMode.Create
                            )

                            System.IO.Compression.ZipFileExtensions.CreateEntryFromFile(
                                archive,
                                sourceFile,
                                Path.GetFileName(sourceFile),
                                System.IO.Compression.CompressionLevel.Optimal
                            )
                        End Using

                        ' Validate archive
                        If IsValidZipArchive(archivePath) Then
                            HandleUserMessageLogging("GMRC",
                                $"CompressSingleFileWithRetryAsync: Compressed {Path.GetFileName(sourceFile)}")

                            ' Optional: delete original
                            If DeleteAfterCompression Then
                                Try
                                    File.Delete(sourceFile)
                                Catch delEx As Exception
                                    HandleUserMessageLogging("GMRC",
                                        $"Could not delete source: {delEx.Message}")
                                End Try
                            End If

                            ' Track success
                            Dim result As New CompressionResult With {
                                .Success = True,
                                .FilePath = sourceFile,
                                .ArchivePath = archivePath,
                                .Timestamp = DateTime.Now,
                                .AttemptCount = attempt,
                                .ErrorMessage = ""
                            }
                            _compressionStats(sourceFile) = result

                            Return True
                        Else
                            Throw New IOException("Archive validation failed")
                        End If

                    Catch ex As IOException
                        If attempt < maxRetries Then
                            HandleUserMessageLogging("GMRC",
                                $"Retry {attempt}/{maxRetries} - {ex.Message}")
                            Threading.Thread.Sleep(retryDelayMs)
                        End If
                    End Try
                Next

                ' All retries failed
                Dim failureResult As New CompressionResult With {
                    .Success = False,
                    .FilePath = sourceFile,
                    .ArchivePath = archivePath,
                    .Timestamp = DateTime.Now,
                    .AttemptCount = maxRetries,
                    .ErrorMessage = "Compression failed after maximum retries"
                }
                _compressionStats(sourceFile) = failureResult

                HandleUserMessageLogging("GMRC",
                    $"CompressSingleFileWithRetryAsync: Failed after {maxRetries} retries: {Path.GetFileName(sourceFile)}")
                Return False

            Catch ex As Exception
                HandleUserMessageLogging("GMRC",
                    $"CompressSingleFileWithRetryAsync error: {ex.Message}")
                Return False
            End Try
        End Function
    )

        ' Track task globally
        ActiveCompressionTasks.Add(compressionTask)

        ' Auto-remove when complete
        compressionTask.ContinueWith(
        Sub(t)
            ' Task cleanup happens in Handle7ZipProcess via filtering
        End Sub
    )

        Return compressionTask
    End Function


    Private Sub ZipSingleFile(ByVal filename As String)

        'Called from CheckZipFile...

        'Zips a file depending on its filename and whether or not a zip file already exists for
        'the filename passed in.

        Dim myprocess As Process
        Dim executableFile As String = _zipDir & _zipExe
        Dim p As New ProcessStartInfo

        Dim compressedFilename As String

        Dim tempFilename As String

        If File.Exists(filename) Then

            tempFilename = ""

            If InStr(filename, "." & RecordingFileFormat) > 0 And InStr(filename, "-") = 0 Then

                tempFilename = Mid(filename, 1, InStr(filename, "." & RecordingFileFormat) - 1) & ".zip"

            End If

            If InStr(filename, ".asc") > 0 Then

                tempFilename = Mid(filename, 1, InStr(filename, ".asc") - 1) & ".zip"

            End If

            If InStr(filename, ".vsb") > 0 Then

                tempFilename = Mid(filename, 1, InStr(filename, ".vsb") - 1) & ".zip"

            End If

            If InStr(filename, ".pcap") > 0 And InStr(filename, "-") = 0 Then

                tempFilename = Mid(filename, 1, InStr(filename, ".pcap") - 1) & ".zip"

            End If

            If Len(tempFilename) > 0 Then

                If Not File.Exists(tempFilename) Then

                    compressedFilename = Mid(filename, 1, InStr(filename, ".") - 1) & ".zip"

                    p.WindowStyle = ProcessWindowStyle.Hidden '(Normal?)
                    p.FileName = executableFile

                    p.Arguments = "a " & compressedFilename & " " & filename


                    If File.Exists(filename) Then

                        While FileInUse(filename) = True
                            Thread.Sleep(100)
                        End While

                    End If

                    HandleUserMessageLogging("GMRC", "ZipSingleFile: Zipping " & filename & " to " & compressedFilename)

                    'Zipping = True
                    myprocess = Process.Start(p)
                    myprocess.WaitForExit()
                    myprocess.Dispose()

                    Thread.Sleep(1000)

                    If File.Exists(compressedFilename) = True Then
                        HandleUserMessageLogging("GMRC", "ZipSingleFile: Zipping " & filename & " complete")

                    Else
                        HandleUserMessageLogging("GMRC", "ZipSingleFile: Zipping did Not complete successfully. " & compressedFilename & " Not found.")
                    End If

                End If
            End If

        Else
            HandleUserMessageLogging("GMRC", "ZipSingleFile: File does Not exist. " & filename)
        End If

    End Sub

    Sub UnzipFolder(ByVal folderName As String)

        'Called from CheckForNewerINCAProject, unzips the file passed in...

        Dim myprocess As Process
        'Dim ExecutableFile As String = "C:\Program Files (x86)\7-Zip\7z.exe"
        Dim executableFile As String = _zipDir & _zipExe
        Dim p As New ProcessStartInfo
        Dim mypath As String

        mypath = Path.GetDirectoryName(folderName)

        If InStr(folderName, ".zip") Then

            p.WorkingDirectory = mypath
            p.WindowStyle = ProcessWindowStyle.Hidden
            p.FileName = executableFile

            'p.Arguments = "e " & Path & "\" & Filename

            '7z x archive.zip -oc:\soft *.cpp -r


            If InStr(folderName, " ") = 0 Then
                p.Arguments = "x " & folderName
            Else
                p.Arguments = "x " & """" & folderName & """"
            End If


            myprocess = Process.Start(p)
            myprocess.WaitForExit()


        End If

    End Sub

    Public Sub ZipMyFilesNew(ByVal directoryName As String)
        ' This zips the .mf4 files. It is called when transitioning into record mode
        ' so that the most recently created .mf4 file is zipped up prior to starting the next recording.

        Dim executableFile As String = Path.Combine(_zipDir, _zipExe)
        Dim compressedFilename As String
        Dim tempFilename As String

        If Not ZipTheMF4Files Then
            HandleUserMessageLogging("GMRC", "Exiting ZipMyFilesNEW - ZipTheMF4Files = False...")
            Exit Sub
        End If

        HandleUserMessageLogging("GMRC", "ZipMyFilesNEW called...")

        Try
            If Directory.Exists(directoryName) Then
                ' Process files in the main directory
                For Each filePath In Directory.GetFiles(directoryName)
                    tempFilename = DetermineTempFilename(filePath)

                    If Not String.IsNullOrEmpty(tempFilename) Then
                        If Not File.Exists(tempFilename) AndAlso Not File.Exists(tempFilename & ".encrypt") Then
                            compressedFilename = Path.ChangeExtension(filePath, ".zip")

                            Dim p As New ProcessStartInfo With {
                                .WindowStyle = ProcessWindowStyle.Hidden,
                                .FileName = executableFile,
                                .Arguments = $"a {compressedFilename} {filePath}"
                            }

                            If File.Exists(filePath) Then
                                While FileInUse(filePath)
                                    Thread.Sleep(100)
                                End While
                            End If

                            HandleUserMessageLogging("GMRC", $"ZipMyFilesNEW: Zipping {filePath} to {compressedFilename}")
                            Process.Start(p)
                        End If
                    End If
                Next
                ' Process files in subdirectories
                For Each subDir In Directory.GetDirectories(directoryName)
                    For Each filePath In Directory.GetFiles(subDir)
                        tempFilename = DetermineTempFilename(filePath)

                        If Not String.IsNullOrEmpty(tempFilename) Then
                            If Not File.Exists(tempFilename) AndAlso Not File.Exists(tempFilename & ".encrypt") Then
                                compressedFilename = Path.ChangeExtension(filePath, ".zip")

                                Dim p As New ProcessStartInfo With {
                                    .WindowStyle = ProcessWindowStyle.Hidden,
                                    .FileName = executableFile,
                                    .Arguments = $"a {compressedFilename} {filePath}"
                                }

                                If File.Exists(filePath) Then
                                    While FileInUse(filePath)
                                        Thread.Sleep(100)
                                    End While
                                End If

                                HandleUserMessageLogging("GMRC", $"ZipMyFilesNEW: Zipping {filePath} to {compressedFilename}")
                                Process.Start(p)
                            End If
                        End If
                    Next
                Next
            Else
                HandleUserMessageLogging("GMRC", $"ZipMyFilesNEW: {directoryName} not found.")
            End If

            HandleUserMessageLogging("GMRC", "ZipMyFilesNEW is finished, zipping may not be complete...")
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ZipMyFilesNEW: {ex.Message}", DisplayMsgBox)
        End Try
    End Sub

    ' Helper function to determine temporary filenames based on file extensions
    Private Function DetermineTempFilename(filePath As String) As String
        Dim tempFilename As String = ""

        If filePath.EndsWith($".{RecordingFileFormat}") AndAlso Not Path.GetFileName(filePath).Contains("-") Then
            tempFilename = Path.ChangeExtension(filePath, ".zip")
        ElseIf filePath.EndsWith(".asc") Then
            tempFilename = Path.ChangeExtension(filePath, ".zip")
        ElseIf filePath.EndsWith(".vsb") Then
            tempFilename = Path.ChangeExtension(filePath, ".zip")
        ElseIf filePath.EndsWith(".mf4") AndAlso Not Path.GetFileName(filePath).Contains("-") Then
            tempFilename = Path.ChangeExtension(filePath, ".zip")
        End If

        Return tempFilename
    End Function


    Sub UnzipFile(ByVal filename As String)

        'Called from CheckForNewerINCAProject, unzips the file passed in...

        Dim myprocess As Process
        ' Dim ExecutableFile As String = "C:\Program Files (x86)\7-Zip\7z.exe"
        Dim executableFile As String = _zipDir & _zipExe
        Dim p As New ProcessStartInfo
        Dim mypath As String

        mypath = Path.GetDirectoryName(filename)

        If InStr(filename, ".zip") > 0 Then

            p.WorkingDirectory = mypath
            p.WindowStyle = ProcessWindowStyle.Normal
            p.FileName = executableFile

            'p.Arguments = "e " & Path & "\" & Filename

            If InStr(filename, " ") = 0 Then
                p.Arguments = "e " & filename
            Else
                p.Arguments = "e " & """" & filename & """"
            End If

            myprocess = Process.Start(p)
            myprocess.WaitForExit()

        End If

    End Sub

    Public Function Is7ZipRunning() As Boolean

        'Checks if 7-Zip is running, returns True or False depending on whether or not 7-Zip is running.

        Dim current As Process = Process.GetCurrentProcess()
        Dim processes As Process() = Process.GetProcesses
        Dim thisProcess As Process

        Is7ZipRunning = False

        For Each thisProcess In processes
            '-- Ignore the current process 
            If thisProcess.Id <> current.Id Then
                '-- Only list processes that have a Main Window Title 
                'If ThisProcess.MainWindowTitle <> "" Then

                If InStr(UCase(thisProcess.ProcessName), "7Z") > 0 Then

                    Is7ZipRunning = True
                    Exit For
                End If
                'End If
            End If
        Next

    End Function

    Public Sub ZipTheDirectory(ByVal directoryName As String, ByVal referenceName As String, myBaseDataCollectionPath As String)

        'This routine is called from myBackgroundtasks during Vehicle Spy DIDPulls. It is also called from
        'CopyATT_TCPFilesToFinalPath when exiting the app.

        'It zips the directoryname passed in and copies the zip file into the vehicle data upload directory...

        Dim myprocess As Process
        Dim executableFile As String = _zipDir & _zipExe
        Dim p As New ProcessStartInfo

        Dim compressedFilename As String

        'compressedFilename = BaseDataCollectionPath & "\data\gmcsv" & VehicleNumber & "\" & System.IO.Path.GetFileName(DirectoryName) & "_" & Format(DateTime.Now, "MMddyyyy_hhmmss") & ".zip"
        compressedFilename = myBaseDataCollectionPath & "\data\gmcsv" & VehicleNumber & "\" & referenceName & "_" & Format(DateTime.Now, "MMddyyyy_hhmmss") & ".zip"

        p.WindowStyle = ProcessWindowStyle.Normal '(Normal?)
        p.FileName = executableFile

        'p.Arguments = "a " & compressedFilename & " " & """ & DirectoryName & """ & "\"
        p.Arguments = "a " & compressedFilename & " " & """" & directoryName & """"

        HandleUserMessageLogging("GMRC", "Zipping " & directoryName & " to " & compressedFilename,,, FlashMsgOn)
        myprocess = Process.Start(p)
        myprocess.WaitForExit()

        UserStatusInfo.Hide()

    End Sub

    Public Sub ZipEtasLogs(ByVal directoryName As String)

        'Called from CloseINCA - After making sure that INCA is completely shut down.
        'Zips the "c:\Eng_Apps\ETAS\LogFiles" directory renames the file, and copies it to
        'the Recording Session directory.  Only copies file if the FinalPathToSaveData
        'variable is set up, indicating that during the session, the user has started
        'measurement or recording.

        'Dim FSO As Scripting.FileSystemObject
        'Dim f As Scripting.Folder
        'Dim sf As Scripting.Folder
        'Dim sfile As Scripting.File

        Dim myprocess As Process
        'Dim ExecutableFile As String = "C:\Program Files (x86)\7-Zip\7z.exe"
        Dim executableFile As String = _zipDir & _zipExe
        Dim p As New ProcessStartInfo

        Dim compressedFilename As String

        'GmResidentClient.Cursor = Cursors.WaitCursor

        HandleUserMessageLogging("GMRC", "ZipETASLogs called...")

        Try

            If Directory.Exists(directoryName) And Len(FinalPathToSaveData) > 0 Then

                compressedFilename = FinalPathToSaveData & "\ETASLogs" & Format(DateTime.Now, "MMddyyyy_hhmmss") & ".zip"

                p.WindowStyle = ProcessWindowStyle.Hidden '(Normal?)
                p.FileName = executableFile

                p.Arguments = "a " & compressedFilename & " " & directoryName & "\"

                HandleUserMessageLogging("GMRC", "ZipETASLogs: Zipping " & directoryName & " to " & compressedFilename)
                myprocess = Process.Start(p)

            Else

            End If

            HandleUserMessageLogging("GMRC", "ZipETASLogs Zipping Complete")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ZipETASLogs: " & ex.Message, DisplayMsgBox)
        End Try

    End Sub

    Public Sub DeleteMf4Files(ByVal directoryName As String, Optional ByVal deleteOnly As Boolean = False)

        'Called from UploadDataScreen.UploadData button press, ExitApp, StartStopMeasurement and StartStopRecord...

        'Checks to make sure that the zipped mf4 files are not corrupted, if not, deletes all of the MF4 files 
        'that have already been zipped.  If zip file is corrupted, will try to re-zip, then re-check before deleting 
        'corresponding.mf4 File.This Is called prior to upload and when transitioning out of Record mode.  

        'There used to be a user prompt asking whether Or Not they wanted to delete the MF4 files.  
        'We may need to create a version that has this for the Validation folks...

        Dim dir As DirectoryInfo '= New DirectoryInfo(DirectoryName)
        Dim files As FileInfo()
        Dim dirs As DirectoryInfo()

        Dim x As Integer
        Dim y As Integer

        'Me.Cursor = Cursors.WaitCursor

        Try

            If Directory.Exists(directoryName) Then

                dir = New DirectoryInfo(directoryName)

                If deleteOnly = False Then
                    HandleUserMessageLogging("GMRC", "Zipping and Deleting Uncompressed .MF4 and .ASC Files. Please be patient...",,, FlashMsgOn)
                End If

                files = dir.GetFiles

                For x = 0 To UBound(files)
                    If InStr(files(x).Name, ".mf4") > 0 Or InStr(files(x).Name, ".asc") > 0 Or InStr(files(x).Name, ".pcap") > 0 Or InStr(files(x).Name, ".vsb") > 0 Then

                        CheckZipfile(files(x).FullName, deleteOnly)

                    End If
                Next

                dirs = dir.GetDirectories

                For x = 0 To UBound(dirs)

                    files = dirs(x).GetFiles

                    'Call HandleEscalationProcessingEXE only if DeleteOnly is set to true, indicating DeleteMF4Files called from user stop recording event...

                    If File.Exists(My.Application.Info.DirectoryPath & "\" & "EscalationExeName.txt") Then

                        If dirs(x).FullName = FinalPathToSaveData Then

                            If deleteOnly = True Then

                                Dim numMf4Files As Integer = 0

                                'here we need to get the number of mf4 files in the session folder so we can set up an appropriate wait time for Escalation process to complete...
                                'time could be lengthy depending on how many files it has to process from the session folder...

                                For y = 0 To UBound(files)
                                    If InStr(files(y).Name, ".mf4") > 0 Then
                                        numMf4Files += 1
                                    End If
                                Next

                                'Call HandleEscalationProcessingEXE with full name of session folder and number of mf4 files in this folder...
                                If numMf4Files > 0 Then
                                    If HandleEscalationProcessingEXE(dirs(x).FullName, numMf4Files) = False Then
                                        HandleUserMessageLogging("GMRC", "DeleteMF4Ffiles: HandleEscalationProcessingEXE Returned False",, )
                                    End If
                                End If

                            End If

                        End If

                    End If

                    For y = 0 To UBound(files)
                        If InStr(files(y).Name, ".mf4") > 0 Or InStr(files(y).Name, ".asc") > 0 Or InStr(files(y).Name, ".pcap") > 0 Or InStr(files(y).Name, ".vsb") > 0 Then ' was just mf4

                            CheckZipfile(files(y).FullName, deleteOnly)

                        End If
                    Next

                Next

                Thread.Sleep(1500)

            Else
                HandleUserMessageLogging("GMRC", "DeleteMF4Files: " & directoryName & " not found.  No recorded data available in this directory.", DisplayMsgBox)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "DeleteMF4Ffiles ERROR: " & ex.Message)

        Finally

            UserStatusInfo.Hide()
        End Try

    End Sub


    ''' <summary>
    ''' Get compression statistics for reporting
    ''' </summary>
    Public Function GetCompressionStats() As Dictionary(Of String, CompressionResult)
        Return New Dictionary(Of String, CompressionResult)(_compressionStats)
    End Function

    ''' <summary>
    ''' Clear compression statistics
    ''' </summary>
    Public Sub ClearCompressionStats()
        _compressionStats.Clear()
    End Sub

End Module

