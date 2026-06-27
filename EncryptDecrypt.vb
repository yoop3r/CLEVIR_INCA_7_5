Imports System
Imports System.IO
Imports System.Security
Imports System.Security.Cryptography

Imports System.Threading

Module EncryptDecrypt

    'This module includes Functions to perform encryption and decryption of the data files recorded with CLEVIR...

    'This code was originally designed to perform encryptions and decryptions one at a time
    'and populate status information on a form.  The form display code has been commented out...

    '*************************
    '** Global Variables
    '*************************

    'This is the encrypt/decrypt password...
    Const Password = "CLEVIR_ED"

    Dim _txtDestinationDecryptText As String
    Dim _txtDestinationEncryptText As String

    'Public strFileToEncrypt As String
    Public StrFileToDecrypt As String
    Dim _strOutputEncrypt As String
    Dim _strOutputDecrypt As String
    Private FsInput As System.IO.FileStream
    Private FsOutput As System.IO.FileStream

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

                                    TriggerEncryptAndCopy("", saveFileName, files(x).FullName, allFiles)

                                End If
                            Else
                                If InStr(saveFileName, ".mf4") = 0 And InStr(saveFileName, ".encrypt") = 0 Then

                                    TriggerEncryptAndCopy("", saveFileName, files(x).FullName, allFiles)

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

                                            TriggerEncryptAndCopy(dirs(x).Name, saveFileName, files(y).FullName, allFiles)

                                        End If
                                    Else

                                        If InStr(saveFileName, "mp4_convert") > 0 Or InStr(saveFileName, "mf4_attach") > 0 Then
                                            files(y).Delete()
                                            Continue For
                                        End If

                                        TriggerEncryptAndCopy(dirs(x).Name, saveFileName, files(y).FullName, allFiles)

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

    Private Sub TriggerEncryptAndCopy(ByVal subfolderName As String, ByVal savefilename As String, ByVal filenamewithpath As String, ByVal allFiles As Boolean)

        Dim tempFilename As String

        If InStr(savefilename, ".log") = 0 Then
            Encrypt(filenamewithpath)
            tempFilename = savefilename & ".encrypt"
        Else
            tempFilename = savefilename
        End If

        CopyFileToDDrive(subfolderName, tempFilename, allFiles)

    End Sub


    '*************************
    '** Create A Key
    '*************************

    Private Function CreateKey(ByVal strPassword As String) As Byte()

        'Original code called for a 256 bit encryption, we changed it to 128 bit because
        '256 bit just took way too long to encrypt and decrypt...

        'Convert strPassword to an array and store in chrData.
        Dim chrData() As Char = strPassword.ToCharArray
        'Use intLength to get strPassword size.
        Dim intLength As Integer = chrData.GetUpperBound(0)
        'Declare bytDataToHash and make it the same size as chrData.
        Dim bytDataToHash(intLength) As Byte

        'Use For Next to convert and store chrData into bytDataToHash.
        For i As Integer = 0 To chrData.GetUpperBound(0)
            bytDataToHash(i) = CByte(Asc(chrData(i)))
        Next

        'Declare what hash to use.
        Dim sha512 As New System.Security.Cryptography.SHA512Managed
        'Declare bytResult, Hash bytDataToHash and store it in bytResult.
        Dim bytResult As Byte() = sha512.ComputeHash(bytDataToHash)
        'Declare bytKey(31).  It will hold 256 bits.

        'Dim bytKey(31) As Byte

        Dim bytKey(15) As Byte

        'Use For Next to put a specific size (256 bits) of 
        'bytResult into bytKey. The 0 To 31 will put the first 256 bits
        'of 512 bits into bytKey.

        'For i As Integer = 0 To 31
        'bytKey(i) = bytResult(i)
        'Next

        For i As Integer = 0 To 15
            bytKey(i) = bytResult(i)
        Next

        Return bytKey 'Return the key.
    End Function

    '*************************
    '** Create An IV
    '*************************

    Private Function CreateIv(ByVal strPassword As String) As Byte()
        'Convert strPassword to an array and store in chrData.
        Dim chrData() As Char = strPassword.ToCharArray
        'Use intLength to get strPassword size.
        Dim intLength As Integer = chrData.GetUpperBound(0)
        'Declare bytDataToHash and make it the same size as chrData.
        Dim bytDataToHash(intLength) As Byte

        'Use For Next to convert and store chrData into bytDataToHash.
        For i As Integer = 0 To chrData.GetUpperBound(0)
            bytDataToHash(i) = CByte(Asc(chrData(i)))
        Next

        'Declare what hash to use.
        Dim sha512 As New System.Security.Cryptography.SHA512Managed
        'Declare bytResult, Hash bytDataToHash and store it in bytResult.
        Dim bytResult As Byte() = sha512.ComputeHash(bytDataToHash)
        'Declare bytIV(15).  It will hold 128 bits.

        Dim bytIv(15) As Byte

        'Use For Next to put a specific size (128 bits) of 
        'bytResult into bytIV. The 0 To 30 for bytKey used the first 256 bits.
        'of the hashed password. The 32 To 47 will put the next 128 bits into bytIV.

        'For i As Integer = 32 To 47
        'bytIV(i - 32) = bytResult(i)
        'Next

        For i As Integer = 16 To 31
            bytIv(i - 16) = bytResult(i)
        Next

        Return bytIv 'return the IV
    End Function

    '****************************
    '** Encrypt/Decrypt File
    '****************************

    Private Enum CryptoAction
        'Define the enumeration for CryptoAction.
        ActionEncrypt = 1
        ActionDecrypt = 2
    End Enum

    Private Sub EncryptOrDecryptFile(ByVal strInputFile As String,
                                     ByVal strOutputFile As String,
                                     ByVal bytKey() As Byte,
                                     ByVal bytIv() As Byte,
                                     ByVal direction As CryptoAction)
        Try 'In case of errors.

            'Setup file streams to handle input and output.
            FsInput = New System.IO.FileStream(strInputFile, FileMode.Open,
                                               FileAccess.Read)
            FsOutput = New System.IO.FileStream(strOutputFile, FileMode.OpenOrCreate,
                                                FileAccess.Write)
            FsOutput.SetLength(0) 'make sure fsOutput is empty

            'Declare variables for encrypt/decrypt process.
            Dim bytBuffer(4096) As Byte 'holds a block of bytes for processing
            Dim lngBytesProcessed As Long = 0 'running count of bytes processed
            Dim lngFileLength As Long = FsInput.Length 'the input file's length
            Dim intBytesInCurrentBlock As Integer 'current bytes being processed
            Dim csCryptoStream As CryptoStream = Nothing
            'Declare your CryptoServiceProvider.
            Dim cspRijndael As New System.Security.Cryptography.RijndaelManaged
            'Setup Progress Bar
            'pbStatus.Value = 0
            'pbStatus.Maximum = 100

            'Determine if ecryption or decryption and setup CryptoStream.
            Select Case direction
                Case CryptoAction.ActionEncrypt
                    csCryptoStream = New CryptoStream(FsOutput,
                    cspRijndael.CreateEncryptor(bytKey, bytIv),
                    CryptoStreamMode.Write)

                Case CryptoAction.ActionDecrypt
                    csCryptoStream = New CryptoStream(FsOutput,
                    cspRijndael.CreateDecryptor(bytKey, bytIv),
                    CryptoStreamMode.Write)
            End Select

            'Use While to loop until all of the file is processed.
            While lngBytesProcessed < lngFileLength
                'Read file with the input filestream.
                intBytesInCurrentBlock = FsInput.Read(bytBuffer, 0, 4096)
                'Write output file with the cryptostream.
                csCryptoStream.Write(bytBuffer, 0, intBytesInCurrentBlock)
                'Update lngBytesProcessed
                lngBytesProcessed = lngBytesProcessed + CLng(intBytesInCurrentBlock)
                'Update Progress Bar
                'pbStatus.Value = CInt((lngBytesProcessed / lngFileLength) * 100)
            End While

            'Close FileStreams and CryptoStream.
            csCryptoStream.Close()
            FsInput.Close()
            FsOutput.Close()

            'If encrypting then delete the original unencrypted file.
            If direction = CryptoAction.ActionEncrypt Then
                Dim fileOriginal As New FileInfo(strInputFile)
                fileOriginal.Delete()
            End If

            'If decrypting then delete the encrypted file.
            If direction = CryptoAction.ActionDecrypt Then
                Dim fileEncrypted As New FileInfo(StrFileToDecrypt)
                fileEncrypted.Delete()
            End If

            'Update the user when the file is done.
            Dim wrap As String = Chr(13) + Chr(10)
            If direction = CryptoAction.ActionEncrypt Then
                'MsgBox("Encryption Complete" + Wrap + Wrap +
                '        "Total bytes processed = " +
                'lngBytesProcessed.ToString,
                'MsgBoxStyle.Information, "Done")

                HandleUserMessageLogging("GMRC", "Encryption Complete for " & strInputFile)

                'Update the progress bar and textboxes.
                'pbStatus.Value = 0
                'txtFileToEncrypt.Text = "Click Browse to load file."
                'txtPassEncrypt.Text = ""
                'txtConPassEncrypt.Text = ""
                'txtDestinationEncrypt.Text = ""
                'btnChangeEncrypt.Enabled = False
                'btnEncrypt.Enabled = False

            Else
                'Update the user when the file is done.
                'MsgBox("Decryption Complete" + Wrap + Wrap +
                '       "Total bytes processed = " +
                'lngBytesProcessed.ToString,
                'MsgBoxStyle.Information, "Done")

                HandleUserMessageLogging("GMRC", "Decryption Complete for " & strInputFile)

                'Update the progress bar and textboxes.
                'pbStatus.Value = 0
                'txtFileToDecrypt.Text = "Click Browse to load file."
                'txtPassDecrypt.Text = ""
                'txtConPassDecrypt.Text = ""
                'txtDestinationDecrypt.Text = ""
                'btnChangeDecrypt.Enabled = False
                'btnDecrypt.Enabled = False
            End If


            'Catch file not found error.
        Catch When Err.Number = 53 'if file not found
            'MsgBox("Please check to make sure the path and filename" +
            '        "are correct and if the file exists.",
            'MsgBoxStyle.Exclamation, "Invalid Path or Filename")

            HandleUserMessageLogging("GMRC", "Please check to make sure the path and filename are correct for " & strInputFile)

            'Catch all other errors. And delete partial files.
        Catch ex As Exception
            FsInput.Close()
            FsOutput.Close()

            If direction = CryptoAction.ActionDecrypt Then

                'Dim fileDelete As New FileInfo(txtDestinationDecryptText)
                'fileDelete.Delete()

                'pbStatus.Value = 0
                'txtPassDecrypt.Text = ""
                'txtConPassDecrypt.Text = ""

                HandleUserMessageLogging("GMRC", "Please check to make sure that you entered the correct password. " & strInputFile)

            Else
                'Dim fileDelete As New FileInfo(txtDestinationEncryptText)
                'fileDelete.Delete()

                'pbStatus.Value = 0
                'txtPassEncrypt.Text = ""
                'txtConPassEncrypt.Text = ""

                HandleUserMessageLogging("GMRC", strInputFile & " cannot be encrypted. " & Err.Number & " - " & Err.Description)

            End If

        End Try
    End Sub


    '******************************
    '** Browse/Change Buttons
    '******************************

    Private Sub HandleEncryptFileName(ByVal strFileToEncrypt As String)

        'Private Sub btnBrowseEncrypt_Click(ByVal sender As System.Object,
        '                                   ByVal e As System.EventArgs) _
        '                                   Handles btnBrowseEncrypt.Click
        'Setup the open dialog.
        'OpenFileDialog.FileName = ""
        'OpenFileDialog.Title = "Choose a file to encrypt"
        'OpenFileDialog.InitialDirectory = "C:\"
        'OpenFileDialog.Filter = "All Files (*.*) | *.*"

        'Find out if the user chose a file.
        'If OpenFileDialog.ShowDialog = DialogResult.OK Then
        'strFileToEncrypt = OpenFileDialog.FileName
        'txtFileToEncrypt.Text = strFileToEncrypt

        Dim iPosition As Integer = 0
        Dim i As Integer = 0

        'Get the position of the last "\" in the OpenFileDialog.FileName path.
        '-1 is when the character your searching for is not there.
        'IndexOf searches from left to right.
        While strFileToEncrypt.IndexOf("\"c, i) <> -1
            iPosition = strFileToEncrypt.IndexOf("\"c, i)
            i = iPosition + 1
        End While

        'Assign strOutputFile to the position after the last "\" in the path.
        'This position is the beginning of the file name.
        _strOutputEncrypt = strFileToEncrypt.Substring(iPosition + 1)
        'Assign S the entire path, ending at the last "\".
        Dim s As String = strFileToEncrypt.Substring(0, iPosition + 1)
        'Replace the "." in the file extension with "_".
        'strOutputEncrypt = strOutputEncrypt.Replace("."c, "_"c)
        'The final file name.  XXXXX.encrypt
        _txtDestinationEncryptText = s + _strOutputEncrypt + ".encrypt"
        'Update buttons.
        'btnEncrypt.Enabled = True
        'btnChangeEncrypt.Enabled = True

        'End If

    End Sub

    Private Sub HandleDecryptFileName()

        'Private Sub btnBrowseDecrypt_Click(ByVal sender As System.Object,
        '                                   ByVal e As System.EventArgs) _
        '                                   Handles btnBrowseDecrypt.Click
        'Setup the open dialog.
        'OpenFileDialog.FileName = ""
        'OpenFileDialog.Title = "Choose a file to decrypt"
        'OpenFileDialog.InitialDirectory = "C:\"
        'OpenFileDialog.Filter = "Encrypted Files (*.encrypt) | *.encrypt"

        'Find out if the user chose a file.
        'If OpenFileDialog.ShowDialog = DialogResult.OK Then
        'strFileToDecrypt = OpenFileDialog.FileName
        'txtFileToDecrypt.Text = strFileToDecrypt
        Dim iPosition As Integer = 0
        Dim i As Integer = 0
        'Get the position of the last "\" in the OpenFileDialog.FileName path.
        '-1 is when the character your searching for is not there.
        'IndexOf searches from left to right.

        While StrFileToDecrypt.IndexOf("\"c, i) <> -1
            iPosition = StrFileToDecrypt.IndexOf("\"c, i)
            i = iPosition + 1
        End While

        'strOutputFile = the file path minus the last 8 characters (.encrypt)
        _strOutputDecrypt = StrFileToDecrypt.Substring(0, StrFileToDecrypt.Length - 8)
        'Assign S the entire path, ending at the last "\".
        Dim s As String = StrFileToDecrypt.Substring(0, iPosition + 1)
        'Assign strOutputFile to the position after the last "\" in the path.
        _strOutputDecrypt = _strOutputDecrypt.Substring((iPosition + 1))
        'Replace "_" with "."
        'txtDestinationDecryptText = S + strOutputDecrypt.Replace("_"c, "."c)
        _txtDestinationDecryptText = s + _strOutputDecrypt
        'Update buttons
        'btnDecrypt.Enabled = True
        'btnChangeDecrypt.Enabled = True

        'End If
    End Sub

    'Private Sub btnChangeEncrypt_Click(ByVal sender As System.Object,
    '                                   ByVal e As System.EventArgs) _
    '                                   Handles btnChangeEncrypt.Click
    'Setup up folder browser.
    '    FolderBrowserDialog.Description = "Select a folder to place the encrypted file in."
    'If the user selected a folder assign the path to txtDestinationEncrypt.
    'If FolderBrowserDialog.ShowDialog = DialogResult.OK Then
    '        txtDestinationEncryptText = FolderBrowserDialog.SelectedPath +
    '                                     "\" + strOutputEncrypt + ".encrypt"
    'End If
    'End Sub

    'Private Sub btnChangeDecrypt_Click(ByVal sender As System.Object,
    '                                   ByVal e As System.EventArgs) _
    '                                   Handles btnChangeDecrypt.Click
    'Setup up folder browser.
    '    FolderBrowserDialog.Description = "Select a folder for to place the decrypted file in."
    'If the user selected a folder assign the path to txtDestinationDecrypt.
    'If FolderBrowserDialog.ShowDialog = DialogResult.OK Then
    '        txtDestinationDecryptText = FolderBrowserDialog.SelectedPath +
    '                                     "\" + strOutputDecrypt.Replace("_"c, "."c)
    'End If
    'End Sub

    '******************************
    '** Encrypt/Decrypt Buttons
    '******************************

    Private Sub Encrypt(ByVal strFileToEncrypt As String)

        'Private Sub btnEncrypt_Click(ByVal sender As System.Object,
        '                             ByVal e As System.EventArgs) _
        '                             Handles btnEncrypt.Click

        HandleEncryptFileName(strFileToEncrypt)

        'Make sure the password is correct.
        'If txtConPassEncrypt.Text = txtPassEncrypt.Text Then
        'Declare variables for the key and iv.
        'The key needs to hold 256 bits and the iv 128 bits.
        Dim bytKey As Byte()
        Dim bytIv As Byte()
        'Send the password to the CreateKey function.
        'bytKey = CreateKey(txtPassEncrypt.Text)
        bytKey = CreateKey(Password)
        'Send the password to the CreateIV function.
        'bytIV = CreateIV(txtPassEncrypt.Text)
        bytIv = CreateIv(Password)
        'Start the encryption.
        EncryptOrDecryptFile(strFileToEncrypt, _txtDestinationEncryptText,
                                 bytKey, bytIv, CryptoAction.ActionEncrypt)
        'Else
        'MsgBox("Please re-enter your password.", MsgBoxStyle.Exclamation)
        'txtPassEncrypt.Text = ""
        'txtConPassEncrypt.Text = ""
        'End If
    End Sub

    Public Sub Decrypt()

        'Private Sub btnDecrypt_Click(ByVal sender As System.Object,
        '                             ByVal e As System.EventArgs) _
        '                             Handles btnDecrypt.Click

        HandleDecryptFileName()

        'Make sure the password is correct.
        'If txtConPassDecrypt.Text = txtPassDecrypt.Text Then
        'Declare variables for the key and iv.
        'The key needs to hold 256 bits and the iv 128 bits.
        Dim bytKey As Byte()
        Dim bytIv As Byte()
        'Send the password to the CreateKey function.
        'bytKey = CreateKey(txtPassDecrypt.Text)
        bytKey = CreateKey(Password)
        'Send the password to the CreateIV function.
        'bytIV = CreateIV(txtPassDecrypt.Text)
        bytIv = CreateIv(Password)
        'Start the decryption.
        EncryptOrDecryptFile(StrFileToDecrypt, _txtDestinationDecryptText,
                                 bytKey, bytIv, CryptoAction.ActionDecrypt)
        'Else
        'MsgBox("Please re-enter your password.", MsgBoxStyle.Exclamation)
        'txtPassDecrypt.Text = ""
        'txtConPassDecrypt.Text = ""
        'End If
    End Sub

End Module
