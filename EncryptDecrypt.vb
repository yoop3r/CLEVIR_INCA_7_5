Imports System
Imports System.IO

Imports System.Threading

Module EncryptDecrypt

    'This module includes Functions to perform encryption and decryption of the data files recorded with CLEVIR...

    'This code was originally designed to perform encryptions and decryptions one at a time
    'and populate status information on a form.  The form display code has been commented out...

    '*************************
    '** Global Variables
    '*************************

    Public Sub EncryptFilesInDirectory(ByVal directoryName As String, Optional ByVal allFiles As Boolean = False)

        'Called from a thread separate from the main execution, every 10 seconds, also called when the form is exited...
        'Encrypts the appropriate files and copies them to the D: Drive, also encrypts and copies additional files that
        'are being written to throughout the recording session, so it is called after everything is finished and the user
        'is exiting the app.  Only in effect if a flash drive with a specific directory configuration is put into the USB
        'drive...

        Dim myElapseTime As TimeSpan
        Dim mySaveTime As DateTime
        Dim saveFileName As String = ""
        Static inhere As Boolean
        Dim filecount As Integer
        Dim yesterday As DateTime

        Dim dir As DirectoryInfo '= New DirectoryInfo(DirectoryName)
        Dim files As FileInfo()
        Dim dirs As DirectoryInfo()

        Dim x As Integer

        Try

            'HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory: Called...")

            If inhere = True Then
                inhere = False
                Exit Sub
            Else
                inhere = True
            End If

            'We are only going to encrypt files that were created within the last 24 hours.  Should there be older files still on the local drive
            'from days back that have not been uploaded, we do not want to encrypt those...
            yesterday = DateTime.Now.AddDays(-1)

            'Look for files in the main vehicle name directory...
            If System.IO.Directory.Exists(directoryName) Then

                dir = New DirectoryInfo(directoryName)
                files = dir.GetFiles

                For x = 0 To UBound(files)

                    If InStr(files(x).Name, Format(DateTime.Now, "yyyyMMdd")) > 0 Or InStr(files(x).Name, Format(DateTime.Now, "MMddyyyy")) > 0 Or InStr(files(x).Name, Format(yesterday, "yyyyMMdd")) > 0 Or InStr(files(x).Name, ".log") > 0 Or InStr(files(x).Name, ".csv") > 0 Then

                        saveFileName = files(x).Name

                        System.Threading.Thread.Sleep(1000)

                        mySaveTime = DateTime.Now
                        myElapseTime = DateTime.Now.Subtract(mySaveTime)

                        While FileInUse(files(x).FullName) = True And myElapseTime.Seconds < 20
                            System.Threading.Thread.Sleep(100)
                            myElapseTime = DateTime.Now.Subtract(mySaveTime)
                        End While

                        If FileInUse(files(x).FullName) = False Then
                            'AllFiles is only set to true when this sub is called on app exit...
                            If allFiles = False Then
                                'If InStr(SaveFileName, ".mf4") = 0 And InStr(SaveFileName, ".csv") = 0 And InStr(SaveFileName, ".encrypt") = 0 Then
                                If InStr(saveFileName, ".mf4") = 0 And InStr(saveFileName, ".csv") = 0 And InStr(saveFileName, ".encrypt") = 0 And InStr(saveFileName, ".log") = 0 Then

                                    TriggerEncryptAndCopy("", saveFileName, allFiles)

                                End If
                            Else
                                If InStr(saveFileName, ".mf4") = 0 And InStr(saveFileName, ".encrypt") = 0 Then

                                    TriggerEncryptAndCopy("", saveFileName, allFiles)

                                End If
                            End If

                        Else
                            HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory: " & files(x).FullName & " in use...")
                        End If

                    End If
                Next

                'AllFiles is only set to true when this sub is called on app exit. When we are exiting, we will check to make sure
                'that the files that are supposed to have been copied to the flash drive exist on the flash drive and then delete the files
                'from the main vehicle directory on  the local drive...
                If allFiles = True Then

                    System.Threading.Thread.Sleep(1000) 'was 2000

                    files = dir.GetFiles

                    For x = 0 To UBound(files)
                        If FileInUse(files(x).FullName) = False Then

                            If InStr(files(x).FullName, ".encrypt") > 0 Or InStr(files(x).Name, ".log") > 0 Then
                                If File.Exists(NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & "\" & files(x).Name) = True Then
                                    HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory: Deleting " & files(x).FullName)
                                    files(x).Delete()
                                Else
                                    HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory: " & files(x).Name & " not found on flash drive, file was not deleted.")
                                End If
                            End If

                        Else
                            HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory " & files(x).FullName & " in use...")
                        End If
                    Next

                End If

                'Look for files in session directories below the main vehicle directory...    

                dirs = dir.GetDirectories

                For x = 0 To UBound(dirs)

                    If InStr(dirs(x).Name, Format(DateTime.Now, "yyyyMMdd")) > 0 Or InStr(dirs(x).Name, Format(yesterday, "yyyyMMdd")) > 0 Then

                        files = dirs(x).GetFiles

                        For y = 0 To UBound(files)

                            saveFileName = files(y).Name

                            If InStr(saveFileName, ".mf4") = 0 And InStr(saveFileName, ".encrypt") = 0 And InStr(saveFileName, ".asc") = 0 And InStr(saveFileName, ".vsb") = 0 And InStr(saveFileName, ".mdf") = 0 Then

                                System.Threading.Thread.Sleep(1000)

                                mySaveTime = DateTime.Now
                                myElapseTime = DateTime.Now.Subtract(mySaveTime)

                                While FileInUse(files(y).FullName) = True And myElapseTime.Seconds < 20
                                    System.Threading.Thread.Sleep(100)
                                    myElapseTime = DateTime.Now.Subtract(mySaveTime)
                                End While

                                If FileInUse(files(y).FullName) = False Then

                                    If allFiles = False Then
                                        If InStr(saveFileName, ".csv") = 0 And InStr(saveFileName, "mp4_convert") = 0 And InStr(saveFileName, "mf4_attach") = 0 And InStr(saveFileName, ".log") = 0 Then 'added .log file here...

                                            TriggerEncryptAndCopy(dirs(x).Name, saveFileName, allFiles)

                                        End If
                                    Else

                                        If InStr(saveFileName, "mp4_convert") > 0 Or InStr(saveFileName, "mf4_attach") > 0 Then
                                            files(y).Delete()
                                            Continue For
                                        End If

                                        TriggerEncryptAndCopy(dirs(x).Name, saveFileName, allFiles)

                                    End If

                                Else
                                    HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory " & files(y).FullName & " in use...")
                                End If

                            End If
                        Next

                    End If

                Next

                'AllFiles is only set to true when this sub is called on app exit. When we are exiting, we will check to make sure
                'that the files that are supposed to have been copied to the flash drive exist on the flash drive and then delete the files
                'from the session folders on the local drive...
                If allFiles = True Then

                    System.Threading.Thread.Sleep(1000)

                    dirs = dir.GetDirectories

                    For x = 0 To UBound(dirs)

                        If InStr(dirs(x).Name, Format(DateTime.Now, "yyyyMMdd")) > 0 Or InStr(dirs(x).Name, Format(yesterday, "yyyyMMdd")) > 0 Then

                            files = dirs(x).GetFiles

                            For y = 0 To UBound(files)
                                If File.Exists(NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & "\" & dirs(x).Name & "\" & files(y).Name) Then
                                    If FileInUse(files(y).FullName) = False Then
                                        HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory: Deleting " & files(y).FullName)
                                        files(y).Delete()
                                    Else
                                        HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory " & files(y).FullName & " in use...")
                                    End If

                                End If
                            Next

                            filecount = 0
                            files = dirs(x).GetFiles

                            For y = 0 To UBound(files)
                                filecount = filecount + 1
                            Next

                            If filecount = 0 Then
                                HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory: " & dirs(x).Name & " is empty, deleting...")
                                dirs(x).Delete()
                            Else
                                HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory: There are still un-transferred files in " & dirs(x).Name & " - not deleting directory.")
                            End If

                        End If

                    Next

                End If

            End If
            inhere = False
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "EncryptFilesInDirectory: " & ex.Message & " - " & saveFileName)
            inhere = False
        End Try

    End Sub

    Private Sub TriggerEncryptAndCopy(ByVal subfolderName As String, ByVal savefilename As String, ByVal allFiles As Boolean)

        'Encryption has been removed (2026-07-03, explicit user decision) - files are now copied to the
        'flash drive as plaintext, matching the behavior previously used only for .log files.

        CopyFileToDDrive(subfolderName, savefilename, allFiles)

    End Sub

End Module
