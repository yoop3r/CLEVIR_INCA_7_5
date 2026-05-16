Option Strict Off

Imports System.IO
Imports SevenZip

Module ZipStuff

    'This module contains various 7Zip related functions used throughout the CLEVIR applications...
    'Public Zipping As Boolean
    Public Sub CheckFor7Zip()

        'Called from InitForm_Load...

        'CLEVIR requies 7-zip to be available on the computer.  So, if it is not, we will install it for the user.

        '7-Zip is used because on the original in vehicle computers, WinZip was not installed.  The DELL Toughbooks
        'do have WinZip installed so we could use the WinZip API instead, but since the 7-Zip stuff works, we will
        'continue to do things this way...

        'If Not System.IO.File.Exists("C:\Program Files (x86)\7-Zip\7z.exe") Then
        If Not System.IO.File.Exists(ZipDir & ZipExe) Then

            If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\" & ZIPInstallFile) Then

                HandleUserMessageLogging("GMRC", "Please install 7-Zip to the default " & ZipDir & " directory.", DISPLAY_MSG_BOX)

                Dim procStartInfo As New ProcessStartInfo
                Dim procExecuting As New Process

                With procStartInfo
                    .UseShellExecute = True
                    .FileName = My.Application.Info.DirectoryPath & "\" & ZIPInstallFile
                    .WindowStyle = ProcessWindowStyle.Normal
                    .Verb = "runas" 'add this to prompt for elevation
                End With

                procExecuting = Process.Start(procStartInfo)

                procExecuting.WaitForExit()

            Else
                HandleUserMessageLogging("GMRC", "7-Zip not installed.  CLEVIR Requires 7-Zip be installed into the " & ZipDir & " directory before continuing.  Exiting CLEVIR...", DISPLAY_MSG_BOX)
                End
            End If

        End If

    End Sub

    Public Sub CheckZipfile(ByVal filename As String, Optional ByVal DeleteOnly As Boolean = False)

        'Called from DeleteMF4Files - Checks to see if a zip file corresponding to the filename passed in
        'exists, if it does, it checks to make sure that it is not corrupted. If corrupted it tries to
        'zip the file again.  If the zip file is good, it deletes the corresponding unzipped file.
        'If zip file does not exist, it zips the file and checks it and deletes original file if zip is good.

        'Dim exreader As FileStream = Nothing

        Dim zipfile As SevenZipExtractor = Nothing

        Dim tempfilename As String

        Dim fileextension As String

        Try

            'GM_ResidentClient.Cursor = Cursors.WaitCursor
            'TheAnnotator.Cursor = Cursors.WaitCursor

            SevenZipExtractor.SetLibraryPath(SevenZipLibraryPath)

            fileextension = System.IO.Path.GetExtension(filename)

            tempfilename = Mid(filename, 1, InStr(filename, fileextension) - 1) & ".zip"

            If File.Exists(tempfilename) Then  'if zip file exists...

                If FileInUse(tempfilename) = False Then 'if zip file not in use...

                    'exreader = New FileStream(tempfilename, FileMode.Open)
                    'zipfile = New SevenZipExtractor(exreader)
                    zipfile = New SevenZipExtractor(tempfilename)

                    HandleUserMessageLogging("GMRC", "CheckZipFile: Checking Existing Zip file " & tempfilename & "...")
                    If zipfile.Check = True Then
                        HandleUserMessageLogging("GMRC", "CheckZipFile: The Zip file " & tempfilename & " looks good.  Deleting " & filename)
                        File.Delete(filename)

                        If Not zipfile Is Nothing Then
                            zipfile.Dispose()
                            zipfile = Nothing

                        End If
                        'If Not exreader Is Nothing Then
                        'exreader.Close()
                        'exreader = Nothing
                        'End If

                    Else
                        HandleUserMessageLogging("GMRC", "CheckZipFile: The Zip file " & tempfilename & " appears corrupted - will attempt To create a New zip file from the " & fileextension & ".  If this Is Not successful, the " & fileextension & " file will Not be deleted.")
                        ZipSingleFile(filename)

                        'exreader = New FileStream(tempfilename, FileMode.Open)
                        'zipfile = New SevenZipExtractor(exreader)
                        zipfile = New SevenZipExtractor(tempfilename)

                        HandleUserMessageLogging("GMRC", "CheckZipFile: Checking Newly created Zip file " & tempfilename & "...")
                        If zipfile.Check = True Then
                            File.Delete(filename)

                            If Not zipfile Is Nothing Then
                                zipfile.Dispose()
                                zipfile = Nothing

                            End If
                            'If Not exreader Is Nothing Then
                            'exreader.Close()
                            'exreader = Nothing
                            'End If

                        Else
                            HandleUserMessageLogging("GMRC", "CheckZipFile: Zip of " & filename & " failed.  This file will Not be deleted.")

                            If Not zipfile Is Nothing Then
                                zipfile.Dispose()
                                zipfile = Nothing

                            End If
                            'If Not exreader Is Nothing Then
                            'exreader.Close()
                            'exreader = Nothing
                            'End If

                        End If

                    End If

                End If

            Else 'Zip file does not exist...

                If DeleteOnly = False Then

                    HandleUserMessageLogging("GMRC", "CheckZipFile: The Zip file " & tempfilename & " not found.  Zipping " & filename)
                    ZipSingleFile(filename)

                    GoTo bypass

                    'exreader = New FileStream(tempfilename, FileMode.Open)
                    'zipfile = New SevenZipExtractor(exreader)
                    zipfile = New SevenZipExtractor(tempfilename)

                    HandleUserMessageLogging("GMRC", "CheckZipFile: Checking Newly created Zip file " & tempfilename & "...")
                    If zipfile.Check = True Then
                        HandleUserMessageLogging("GMRC", "CheckZipFile: The Zip file " & tempfilename & " looks good.  Deleting " & filename)
                        File.Delete(filename)

                        If Not zipfile Is Nothing Then
                            zipfile.Dispose()
                            zipfile = Nothing

                        End If
                        'If Not exreader Is Nothing Then
                        'exreader.Close()
                        'exreader = Nothing
                        'End If

                    Else
                        HandleUserMessageLogging("GMRC", "CheckZipFile: Zip of " & filename & " failed.  This file will Not be deleted.")

                        If Not zipfile Is Nothing Then
                            zipfile.Dispose()
                            zipfile = Nothing

                        End If
                        'If Not exreader Is Nothing Then
                        'exreader.Close()
                        'exreader = Nothing
                        'End If
                    End If

bypass:

                Else

                    If File.Exists(tempfilename & ".encrypt") = True Then 'tempfilename is the .zip file - filename is the actual file

                        If File.Exists(NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & "\" & SaveSelectedTestName & "\" & System.IO.Path.GetFileName(tempfilename) & ".encrypt") = True Then
                            HandleUserMessageLogging("GMRC", "CheckZipFile: Found " & NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & "\" & SaveSelectedTestName & "\" & System.IO.Path.GetFileName(tempfilename) & ".encrypt - Deleting " & filename)
                            File.Delete(filename)
                        End If

                    End If

                End If

                'If File.Exists(tempfilename & ".encrypt") = True Then 'tempfilename is the .zip file - filename is the actual file
                'File.Delete(filename)
                'End If

            End If

            'GM_ResidentClient.Cursor = Cursors.Arrow

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckZipFile ERROR: " & ex.Message)
            'GM_ResidentClient.Cursor = Cursors.Arrow

            If Not zipfile Is Nothing Then
                zipfile.Dispose()
                zipfile = Nothing

            End If
            'If Not exreader Is Nothing Then
            'exreader.Close()
            'exreader = Nothing
            'End If

        End Try

    End Sub

    Sub ZipSingleFile(ByVal Filename As String)

        'Called from CheckZipFile...

        'Zips a file depending on its filename and whether or not a zip file already exists for
        'the filename passed in.

        Dim myprocess As Process
        'Dim ExecutableFile As String = "C:\Program Files (x86)\7-Zip\7z.exe"
        Dim ExecutableFile As String = ZipDir & ZipExe
        Dim p As New ProcessStartInfo

        Dim zipfilename As String

        Dim tempfilename As String

        If File.Exists(Filename) Then

            tempfilename = ""

            If InStr(Filename, "." & RecordingFileFormat) > 0 And InStr(Filename, "-") = 0 Then

                tempfilename = Mid(Filename, 1, InStr(Filename, "." & RecordingFileFormat) - 1) & ".zip"

            End If

            If InStr(Filename, ".asc") > 0 Then

                tempfilename = Mid(Filename, 1, InStr(Filename, ".asc") - 1) & ".zip"

            End If

            If InStr(Filename, ".vsb") > 0 Then

                tempfilename = Mid(Filename, 1, InStr(Filename, ".vsb") - 1) & ".zip"

            End If

            If InStr(Filename, ".mdf") > 0 And InStr(Filename, "-") = 0 Then

                tempfilename = Mid(Filename, 1, InStr(Filename, ".mdf") - 1) & ".zip"

            End If

            If Len(tempfilename) > 0 Then

                If Not File.Exists(tempfilename) Then

                    zipfilename = Mid(Filename, 1, InStr(Filename, ".") - 1) & ".zip"

                    p.WindowStyle = ProcessWindowStyle.Hidden '(Normal?)
                    p.FileName = ExecutableFile

                    p.Arguments = "a " & zipfilename & " " & Filename


                    If File.Exists(Filename) Then

                        While FileInUse(Filename) = True
                            'System.Windows.Forms.Application.DoEvents() 'DOEVENTS
                            System.Threading.Thread.Sleep(100)
                        End While

                    End If

                    HandleUserMessageLogging("GMRC", "ZipSingleFile: Zipping " & Filename & " to " & zipfilename)
                    'Zipping = True
                    myprocess = Process.Start(p)
                    myprocess.WaitForExit()
                    myprocess.Dispose()

                    System.Threading.Thread.Sleep(1000)

                    If File.Exists(zipfilename) = True Then
                        HandleUserMessageLogging("GMRC", "ZipSingleFile: Zipping " & Filename & " complete")

                    Else
                        HandleUserMessageLogging("GMRC", "ZipSingleFile: Zipping did Not complete successfully. " & zipfilename & " Not found.")
                    End If

                End If
            End If

        Else
            HandleUserMessageLogging("GMRC", "ZipSingleFile: File does Not exist. " & Filename)
        End If

    End Sub

    Sub UnzipFolder(ByVal FolderName As String)

        'Called from CheckForNewerINCAProject, unzips the file passed in...

        Dim myprocess As Process
        'Dim ExecutableFile As String = "C:\Program Files (x86)\7-Zip\7z.exe"
        Dim ExecutableFile As String = ZipDir & ZipExe
        Dim p As New ProcessStartInfo
        Dim mypath As String

        mypath = System.IO.Path.GetDirectoryName(FolderName)

        If InStr(FolderName, ".zip") Then

            p.WorkingDirectory = mypath
            p.WindowStyle = ProcessWindowStyle.Hidden
            p.FileName = ExecutableFile

            'p.Arguments = "e " & Path & "\" & Filename

            '7z x archive.zip -oc:\soft *.cpp -r


            If InStr(FolderName, " ") = 0 Then
                p.Arguments = "x " & FolderName
            Else
                p.Arguments = "x " & """" & FolderName & """"
            End If


            myprocess = Process.Start(p)
            myprocess.WaitForExit()


        End If

    End Sub

    Public Sub ZipMyFilesNEW(ByVal DirectoryName As String)

        'This zips the .mf4 files.  It is called when transitioning into record mode
        'so that the most recently created .mf4 file is zipped up prior to starting the next recording

        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sf As Scripting.Folder
        Dim sfile As Scripting.File

        Dim myprocess As Process
        'Dim ExecutableFile As String = "C:\Program Files (x86)\7-Zip\7z.exe"
        Dim ExecutableFile As String = ZipDir & ZipExe
        Dim p As New ProcessStartInfo

        Dim zipfilename As String

        Dim tempfilename As String

        If ZipTheMF4Files = False Then
            HandleUserMessageLogging("GMRC", "Exiting ZipMyFilesNEW - ZipTheMF4Files = False...")
            Exit Sub
        End If

        'GM_ResidentClient.Cursor = Cursors.WaitCursor

        HandleUserMessageLogging("GMRC", "ZipMyFilesNEW called...")

        Try

            If System.IO.Directory.Exists(DirectoryName) Then

                FSO = New Scripting.FileSystemObject

                f = FSO.GetFolder(DirectoryName)

                For Each sfile In f.Files

                    tempfilename = ""

                    If InStr(sfile.Name, "." & RecordingFileFormat) > 0 And InStr(sfile.Name, "-") = 0 Then

                        tempfilename = Mid(sfile.Path, 1, InStr(sfile.Path, "." & RecordingFileFormat) - 1) & ".zip"

                    End If

                    If InStr(sfile.Name, ".asc") > 0 Then

                        tempfilename = Mid(sfile.Path, 1, InStr(sfile.Path, ".asc") - 1) & ".zip"

                    End If

                    If InStr(sfile.Name, ".vsb") > 0 Then

                        tempfilename = Mid(sfile.Path, 1, InStr(sfile.Path, ".vsb") - 1) & ".zip"

                    End If

                    If InStr(sfile.Name, ".mdf") > 0 And InStr(sfile.Name, "-") = 0 Then

                        tempfilename = Mid(sfile.Path, 1, InStr(sfile.Path, ".mdf") - 1) & ".zip"

                    End If

                    If Len(tempfilename) > 0 Then

                        If Not File.Exists(tempfilename) And Not File.Exists(tempfilename & ".encrypt") Then

                            zipfilename = Mid(sfile.Path, 1, InStr(sfile.Path, ".") - 1) & ".zip"

                            p.WindowStyle = ProcessWindowStyle.Hidden '(Normal?)
                            p.FileName = ExecutableFile

                            p.Arguments = "a " & zipfilename & " " & sfile.Path

                            If File.Exists(sfile.Path) Then

                                While FileInUse(sfile.Path) = True
                                    'System.Windows.Forms.Application.DoEvents() 'DOEVENTS
                                    System.Threading.Thread.Sleep(100)
                                End While

                            End If

                            HandleUserMessageLogging("GMRC", "ZipMyFilesNEW: Zipping " & sfile.Path & " to " & zipfilename)
                            'Zipping = True
                            myprocess = Process.Start(p)

                        End If
                    End If


                Next

                For Each sf In f.SubFolders

                    For Each sfile In sf.Files

                        tempfilename = ""

                        If InStr(sfile.Name, ".mf4") > 0 And InStr(sfile.Name, "-") = 0 Then

                            tempfilename = Mid(sfile.Path, 1, InStr(sfile.Path, ".mf4") - 1) & ".zip"

                        End If

                        If InStr(sfile.Name, ".asc") > 0 Then

                            tempfilename = Mid(sfile.Path, 1, InStr(sfile.Path, ".asc") - 1) & ".zip"

                        End If

                        If InStr(sfile.Name, ".vsb") > 0 Then

                            tempfilename = Mid(sfile.Path, 1, InStr(sfile.Path, ".vsb") - 1) & ".zip"

                        End If

                        If InStr(sfile.Name, ".mdf") > 0 And InStr(sfile.Name, "-") = 0 Then

                            tempfilename = Mid(sfile.Path, 1, InStr(sfile.Path, ".mdf") - 1) & ".zip"

                        End If

                        If Len(tempfilename) > 0 Then

                            If Not File.Exists(tempfilename) And Not File.Exists(tempfilename & ".encrypt") Then

                                zipfilename = Mid(sfile.Path, 1, InStr(sfile.Path, ".") - 1) & ".zip"

                                p.WindowStyle = ProcessWindowStyle.Hidden '(Normal?)
                                p.FileName = ExecutableFile

                                p.Arguments = "a " & zipfilename & " " & sfile.Path


                                If File.Exists(sfile.Path) Then

                                    While FileInUse(sfile.Path) = True
                                        'System.Windows.Forms.Application.DoEvents() 'DOEVENTS
                                        System.Threading.Thread.Sleep(100)
                                    End While

                                End If

                                HandleUserMessageLogging("GMRC", "ZipMyFilesNEW: Zipping " & sfile.Path & " to " & zipfilename)
                                'Zipping = True
                                myprocess = Process.Start(p)

                            End If
                        End If

                    Next
                Next

            Else
                HandleUserMessageLogging("GMRC", "ZipMyFilesNEW: " & DirectoryName & " not found.")
            End If


            HandleUserMessageLogging("GMRC", "ZipMyFilesNEW is finished, zipping my not be...")

            'GM_ResidentClient.Cursor = Cursors.Arrow

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ZipMyFilesNEW: " & ex.Message, DISPLAY_MSG_BOX)
            'GM_ResidentClient.Cursor = Cursors.Arrow
        End Try

    End Sub

    Sub UnzipFile(ByVal Filename As String)

        'Called from CheckForNewerINCAProject, unzips the file passed in...

        Dim myprocess As Process
        ' Dim ExecutableFile As String = "C:\Program Files (x86)\7-Zip\7z.exe"
        Dim ExecutableFile As String = ZipDir & ZipExe
        Dim p As New ProcessStartInfo
        Dim mypath As String

        mypath = System.IO.Path.GetDirectoryName(Filename)

        If InStr(Filename, ".zip") > 0 Then

            p.WorkingDirectory = mypath
            p.WindowStyle = ProcessWindowStyle.Normal
            p.FileName = ExecutableFile

            'p.Arguments = "e " & Path & "\" & Filename

            If InStr(Filename, " ") = 0 Then
                p.Arguments = "e " & Filename
            Else
                p.Arguments = "e " & """" & Filename & """"
            End If

            myprocess = Process.Start(p)
            myprocess.WaitForExit()

        End If

    End Sub

    Public Function Is7ZipRunning() As Boolean

        'Checks if INCA is running, returns True or False depending on whether or not INCA is running.

        Dim current As Process = Process.GetCurrentProcess()
        Dim processes As Process() = Process.GetProcesses
        Dim ThisProcess As Process

        Is7ZipRunning = False

        For Each ThisProcess In processes
            '-- Ignore the current process 
            If ThisProcess.Id <> current.Id Then
                '-- Only list processes that have a Main Window Title 
                'If ThisProcess.MainWindowTitle <> "" Then

                If InStr(UCase(ThisProcess.ProcessName), "7Z") > 0 Then

                    Is7ZipRunning = True
                    Exit For
                End If
                'End If
            End If
        Next

    End Function

    Public Sub ZipTheDirectory(ByVal DirectoryName As String, ByVal ReferenceName As String, myBaseDataCollectionPath As String)

        'This routine is called from myBackgroundtasks during Vehicle Spy DIDPulls. It is also called from
        'CopyATT_TCPFilesToFinalPath when exiting the app.

        'It zips the directoryname passed in and copies the zip file into the vehicle data upload directory...

        Dim myprocess As Process
        Dim ExecutableFile As String = ZipDir & ZipExe
        Dim p As New ProcessStartInfo

        Dim zipfilename As String

        'GM_ResidentClient.Cursor = Cursors.WaitCursor

        'CopyToLog("ZipTheDirectory called...")

        'zipfilename = BaseDataCollectionPath & "\data\gmcsv" & VehicleNumber & "\" & System.IO.Path.GetFileName(DirectoryName) & "_" & Format(Now, "MMddyyyy_hhmmss") & ".zip"
        zipfilename = myBaseDataCollectionPath & "\data\gmcsv" & VehicleNumber & "\" & ReferenceName & "_" & Format(Now, "MMddyyyy_hhmmss") & ".zip"

        p.WindowStyle = ProcessWindowStyle.Normal '(Normal?)
        p.FileName = ExecutableFile

        'p.Arguments = "a " & zipfilename & " " & """ & DirectoryName & """ & "\"
        p.Arguments = "a " & zipfilename & " " & """" & DirectoryName & """"

        HandleUserMessageLogging("GMRC", "Zipping " & DirectoryName & " to " & zipfilename,,, FLASH_MSG_ON)
        'UserStatusInfo.Label1.Text = "Zipping " & DirectoryName & " to " & zipfilename
        'Zipping = True
        myprocess = Process.Start(p)
        myprocess.WaitForExit()

        UserStatusInfo.Hide()

    End Sub
    Public Sub ZipETASLogs(ByVal DirectoryName As String)

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
        Dim ExecutableFile As String = ZipDir & ZipExe
        Dim p As New ProcessStartInfo

        Dim zipfilename As String

        'GM_ResidentClient.Cursor = Cursors.WaitCursor

        HandleUserMessageLogging("GMRC", "ZipETASLogs called...")

        Try

            If System.IO.Directory.Exists(DirectoryName) And Len(FinalPathToSaveData) > 0 Then

                zipfilename = FinalPathToSaveData & "\ETASLogs" & Format(Now, "MMddyyyy_hhmmss") & ".zip"

                p.WindowStyle = ProcessWindowStyle.Hidden '(Normal?)
                p.FileName = ExecutableFile

                p.Arguments = "a " & zipfilename & " " & DirectoryName & "\"

                HandleUserMessageLogging("GMRC", "ZipETASLogs: Zipping " & DirectoryName & " to " & zipfilename)
                myprocess = Process.Start(p)

            Else

            End If

            HandleUserMessageLogging("GMRC", "ZipETASLogs Zipping Complete")

            'GM_ResidentClient.Cursor = Cursors.Arrow

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ZipETASLogs: " & ex.Message, DISPLAY_MSG_BOX)
            'MsgBox(ex.Message)
            'GM_ResidentClient.Cursor = Cursors.Arrow
        End Try

    End Sub
End Module
