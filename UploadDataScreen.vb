
Option Strict Off
Option Explicit On

Imports System.Diagnostics
Imports System.IO

Public Class UploadDataScreen

    'The UploadDataScreen is the user interface for uploading data from the in vehicle PC or user laptop to the Share Drive.

    Private _searchString As String
    Private _selectedDriveLetter As String

    Private Sub RadioButton3_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton3.CheckedChanged

        'Data Associated With Annotations Only radio button...

        If RadioButton3.Checked = True Then
            'Do not delete uploaded files radio button will be checked so we can retain the data on the local drive.
            RadioButton7.Checked = True
        End If
    End Sub

    Private Sub RadioButton12_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton12.CheckedChanged

    End Sub

    Private Sub RadioButton14_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton14.CheckedChanged

        If RadioButton14.Checked = True Then
            ListBox1.Visible = False
            _searchString = "_"
            HandleUserMessageLogging("GMRC", "All Users Selected")
        End If

    End Sub

    Private Sub RadioButton13_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton13.CheckedChanged

        Dim users As ArrayList

        If RadioButton13.Checked = True Then
            HandleUserMessageLogging("GMRC", "Current User Only")
            If Len(SaveLoginId) = 0 Then

                'Users = MyIncaInterface.ReadUserIDList()
                users = LoginIdNameAndFreqAl

                ListBox1.Items.Clear()
                ListBox1.Visible = True

                If Not users Is Nothing Then
                    For x = 0 To users.Count - 1

                        ListBox1.Items.Add(Mid(users(x).ToString, 8, Len(users(x).ToString)))

                    Next x

                End If
            Else
                _searchString = SaveLoginId
            End If

        Else
            _searchString = "_"
        End If

    End Sub

    Private Sub RadioButton6_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton6.CheckedChanged
        If RadioButton6.Checked = True Then
            CheckBox1.Enabled = False
            CheckBox2.Enabled = False
            CheckBox3.Enabled = False
            CheckBox4.Enabled = False
        End If
    End Sub

    Private Sub RadioButton8_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs)


    End Sub

    Private Sub RadioButton9_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton9.CheckedChanged

        If RadioButton9.Checked = True Then
            CheckBox1.Enabled = True
            CheckBox2.Enabled = True
            CheckBox3.Enabled = True
            CheckBox4.Enabled = True
        End If

    End Sub

    Private Sub RadioButton11_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton11.CheckedChanged

        If RadioButton11.Checked = True Then
            CheckBox1.Enabled = False
            CheckBox2.Enabled = False
            CheckBox3.Enabled = False
            CheckBox4.Enabled = False
        End If

    End Sub

    Private Sub RadioButton5_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton5.CheckedChanged

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        ' This is the Upload Data button on the UploadDataScreen...

        Dim executableFile2 As String = "C:\CATS\CATS.exe"
        Dim executableFile As String = "C:\csvscripts\robocopy.exe"
        Dim roboParams As String
        Dim encryptedFilesInDir As Boolean = False
        Dim someDataNotSaved As Boolean = False
        Dim saveFileName As String
        Dim baseFolderPath As String = Path.Combine(BaseDataCollectionPath, "Data", $"gmcsv{VehicleNumber}")

        Try
            HandleUserMessageLogging("GMRC", "UPLOAD DATA Pressed.",, )

            ' Launch CATS.exe if it exists and debugger is not attached
            If Not Debugger.IsAttached AndAlso File.Exists(executableFile2) Then
                Dim p2 As New ProcessStartInfo With {
                    .WindowStyle = ProcessWindowStyle.Minimized,
                    .FileName = executableFile2
                }
                Process.Start(p2)
            End If

            Me.Cursor = Cursors.WaitCursor
            Me.Refresh()

            ' Handle RadioButton1 logic (Delete MF4 files)
            If Not RadioButton1.Checked Then
                DeleteMf4Files(baseFolderPath)
            End If

            ' Initialize Robocopy process
            Dim p As New ProcessStartInfo With {
                .WindowStyle = ProcessWindowStyle.Normal,
                .FileName = executableFile
            }

            ' Configure robocopy parameters based on RadioButton10
            roboParams = If(RadioButton10.Checked, " /R:1 /move /s", " /R:1")

            ' Iterate through subdirectories and files
            If Directory.Exists(baseFolderPath) Then
                For Each subDirPath In Directory.GetDirectories(baseFolderPath)
                    Dim subDirName = Path.GetFileName(subDirPath)

                    ' Skip encrypted files if RadioButton6 is checked
                    encryptedFilesInDir = Directory.GetFiles(subDirPath).Any(Function(file) file.EndsWith(".encrypt", StringComparison.OrdinalIgnoreCase))

                    If Not encryptedFilesInDir OrElse RadioButton6.Checked Then
                        ' Handle search strings or today's date filter
                        If Not String.IsNullOrEmpty(_searchString) Then
                            If subDirName.IndexOf(_searchString, StringComparison.OrdinalIgnoreCase) >= 0 Then
                                UploadSubdirectory(p, subDirPath, subDirName, roboParams)
                            End If
                        ElseIf subDirName.Contains(Format(DateTime.Now, "yyyyMMdd")) Then
                            UploadSubdirectory(p, subDirPath, subDirName, roboParams)
                        End If
                    End If
                Next
            End If

            ' Handle remaining files in the base folder
            For Each filePath In Directory.GetFiles(baseFolderPath)
                If Not filePath.EndsWith(".encrypt", StringComparison.OrdinalIgnoreCase) Then
                    saveFileName = Path.GetFileName(filePath)
                    HandleUserMessageLogging("GMRC", $"Uploading {saveFileName}...")

                    p.Arguments = $"{baseFolderPath} {_selectedDriveLetter}{DataUploadPath}gmcsv{VehicleNumber} {saveFileName} {roboParams}"
                    Dim myprocess = Process.Start(p)
                    myprocess.WaitForExit()

                    If Not File.Exists(Path.Combine(_selectedDriveLetter, DataUploadPath, "gmcsv", VehicleNumber, saveFileName)) Then
                        HandleUserMessageLogging("GMRC", $"{saveFileName} not found on the destination.")
                        someDataNotSaved = True
                    End If
                End If
            Next

            ' Delete startup DTC files if they exist
            Dim dtcPath As String = Path.Combine(My.Application.Info.DirectoryPath, "CANalyzer\StartupDTCs")
            If Directory.Exists(dtcPath) Then
                For Each filePath In Directory.GetFiles(dtcPath)
                    If Path.GetFileName(filePath).Contains("DTCsBeforeCodeClear") Then
                        File.Delete(filePath)
                    End If
                Next
            End If

            ' Logging completion status
            If Not someDataNotSaved Then
                HandleUserMessageLogging("GMRC", $"Data Sync Complete. Files saved to {_selectedDriveLetter}{DataUploadPath}gmcsv{VehicleNumber}.", DisplayMsgBox, )
            Else
                HandleUserMessageLogging("GMRC", $"Some data not saved to {_selectedDriveLetter}{DataUploadPath}gmcsv{VehicleNumber}.", DisplayMsgBox, )
            End If

            ' Handle post-upload logic
            GmResidentClient.Label5.Text = ""
            SaveFinalPathToSaveData = ""
            WriteFinalPathToSaveData()

            If Not CheckBox5.Checked Then
                EnableWirelessNetworkConnection()
            Else
                HandleUserMessageLogging("GMRC", "EXITING UPLOAD -- Shutdown Windows Selected.",, )
                ExitWindows(EwxPoweroff Or EwxShutdown Or EwxForce, 0)
                Me.Dispose()
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Error during upload: {ex.Message}",, )
        Finally
            Me.Cursor = Cursors.Arrow
            Me.Refresh()
        End Try
    End Sub

    ' Helper method to upload a subdirectory
    Private Sub UploadSubdirectory(p As ProcessStartInfo, subDirPath As String, subDirName As String, roboParams As String)
        Dim arguments As String = $"{subDirPath} {_selectedDriveLetter}{DataUploadPath}gmcsv{VehicleNumber}\{subDirName} {roboParams}"
        HandleUserMessageLogging("GMRC", $"Uploading directory {subDirName}...")
        Dim subProcess = Process.Start(p)
        subProcess.WaitForExit()
    End Sub


    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ListBox1_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedValueChanged
        _searchString = ListBox1.SelectedItem.ToString
    End Sub

    Private Sub RadioButton7_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton7.CheckedChanged

    End Sub

    Private Sub RadioButton1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton1.CheckedChanged
        If RadioButton1.Checked = True And (RadioButton5.Checked = True Or RadioButton17.Checked = True) Then
            RadioButton15.Checked = True
            MsgBox("When uploading to the " & NetworkDriveLetter & " drive, MF4 files WILL be deleted prior to upload.")
        End If
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        HandleUserMessageLogging("GMRC", "EXIT UPLOAD DATA Pressed")

        If NetworkAdapterDescription <> "NONE" And NetworkAdapterDescription <> "GM_LAN" And WirelessUnavailable = True Then
            EnableWirelessNetworkConnection()
        End If

        If HaveRecorded = False Then
            InitForm.Show()
        End If

        Me.Dispose()

    End Sub

    Private Sub RadioButton10_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton10.CheckedChanged

        'Delete uploaded files radio button...

        If UsingFlashDrive = False Then

            If RadioButton12.Checked = True And RadioButton10.Checked = True Then
                RadioButton10.Checked = False
                RadioButton7.Checked = True
                MsgBox("If uploading to any drive other than the " & NetworkDriveLetter & " drive, files will NOT be deleted from the In Vehicle PC.")
            End If

        Else
            If RadioButton10.Checked = False Then
                RadioButton10.Checked = True
                RadioButton7.Checked = False
                MsgBox("If data has been stored on the " & NetworkDriveLetter & " drive, it WILL be deleted after uploading to a user specified drive.")
            End If
        End If

    End Sub

    Private Sub UploadDataScreen_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        If HaveRecorded = False Then
            InitForm.Hide()
        End If

        Me.Width = 800
        Me.Height = 222
        Me.Top = 200
        Me.Left = 0

    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Button1_Click(sender, e)
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Button2_Click(sender, e)
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Me.Height = 579
        Me.Top = 0
        Button4.Visible = False
        Button3.Visible = False
        Button1.Visible = True
        Button2.Visible = True

        Button5.Visible = False
    End Sub

    Private Sub RadioButton5_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton5.Click

        'Upload to Q drive radio button...

        'If we have a valid connection to the network drive, the Upload to Q Drive button will be enabled... 

        If RadioButton5.Checked = True Then
            'If Radio Button is selected, we will do a final verification that the network drive \ DataUploadPath is valid for the vehicle type
            'if we are connected to the netowrk drive and the folder does not yet exist it will be created in VerifyNetworkMapping...
            VerifyNetworkMapping()

        End If

    End Sub

    Private Sub RadioButton12_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton12.Click

        'Specify upload drive

        If RadioButton12.Checked = True Then

            Button1.Enabled = False

            RadioButton7.Checked = True
            RadioButton1.Checked = True

            FolderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer
            FolderBrowserDialog1.ShowDialog()

            If Len(FolderBrowserDialog1.SelectedPath) > 0 Then

                _selectedDriveLetter = FolderBrowserDialog1.SelectedPath 'Q:\
                Button1.Enabled = True

                Me.Height = 579
                Me.Top = 0
                Button4.Visible = False
                Button3.Visible = False
                Button1.Visible = True
                Button2.Visible = True

                Button5.Visible = False
            Else
                HandleUserMessageLogging("GMRC", "Invalid Drive Letter selected.", DisplayMsgBox)
            End If

        End If

    End Sub

    Private Sub RadioButton8_CheckedChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton8.CheckedChanged

        Try

            If RadioButton8.Checked = True Then

                HandleUserMessageLogging("GMRC", "Upload Data Screen. GM LAN Radio Button Selected...",, )

                If InhibitGmLanRadioButton = False Then

                    If NetworkAdapterDescription <> "GM_LAN" And SaveNetworkAdapterDescription <> "NONE" Then

                        Me.Cursor = Cursors.WaitCursor

                        If DisableWirelessNetworkConnection() = False Then
                            RadioButton8.Checked = False
                            Button4.Enabled = False
                            Button1.Enabled = False
                            RadioButton5.Enabled = False
                            RadioButton17.Enabled = False
                            RadioButton18.Enabled = False
                        Else

                            HandleUserMessageLogging("GMRC", "Checking Data Upload Path Validity...",,, FlashMsgOn)
                            System.Threading.Thread.Sleep(5000)

                            If Not Directory.Exists(NetworkDriveMapping & DataUploadPath) Then
                                HandleUserMessageLogging("GMRC", "Could not find the directory " & NetworkDriveMapping & DataUploadPath & " Please verify GM LAN hardwire connection before uploading data using GM LAN.", DisplayMsgBox)

                                RadioButton8.Checked = False
                                Button4.Enabled = False
                                Button1.Enabled = False
                                RadioButton5.Enabled = False
                                RadioButton17.Enabled = False
                                RadioButton18.Enabled = False

                            Else

                                HandleUserMessageLogging("GMRC", "Directory found " & NetworkDriveMapping & DataUploadPath)

                                Me.RadioButton5.Enabled = True
                                Me.RadioButton17.Enabled = True
                                Me.RadioButton18.Enabled = True

                                If RadioButton5.Checked = True Or RadioButton17.Checked = True Then
                                    Button1.Enabled = True
                                    Button4.Enabled = True
                                End If

                            End If

                            UserStatusInfo.Hide()

                        End If

                        Me.Cursor = Cursors.Arrow

                    Else

                        HandleUserMessageLogging("GMRC", "Checking GM_LAN Data Upload Path Validity...",,, FlashMsgOn)
                        System.Threading.Thread.Sleep(1000)

                        If Not Directory.Exists(NetworkDriveMapping & DataUploadPath) Then

                            HandleUserMessageLogging("GMRC", "Could not find the directory " & NetworkDriveMapping & DataUploadPath & " Please verify GM LAN hardwire connection before uploading data using GM LAN.", DisplayMsgBox)
                            Me.RadioButton5.Enabled = False
                            Me.RadioButton17.Enabled = False
                            Me.RadioButton18.Enabled = False

                        Else

                            HandleUserMessageLogging("GMRC", "Data Upload Path Valid " & NetworkDriveMapping & DataUploadPath)

                            If WirelessUnavailable = False Then
                                HandleUserMessageLogging("GMRC", "Using available wireless connection...")
                                InhibitWirelessRadioButton = True
                                RadioButton2.Checked = True
                                RadioButton8.Checked = False
                            End If

                            Me.RadioButton5.Enabled = True
                            Me.RadioButton17.Enabled = True
                            Me.RadioButton18.Enabled = True

                        End If

                        UserStatusInfo.Hide()

                        If RadioButton5.Checked = True Or RadioButton17.Checked = True Then
                            Button1.Enabled = True
                            Button4.Enabled = True
                        End If
                    End If

                Else
                    InhibitGmLanRadioButton = False
                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GM LAN Radio Button: " & ex.Message)
        Finally
            UserStatusInfo.Hide()
        End Try

    End Sub

    Private Sub RadioButton2_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton2.CheckedChanged

        If Me.Visible = True Then

            If RadioButton2.Checked = True Then

                HandleUserMessageLogging("GMRC", "Upload Data Screen Wireless Radio Button Selected...",, )

                If InhibitWirelessRadioButton = False Then

                    If NetworkAdapterDescription <> "GM_LAN" Then

                        If SaveNetworkAdapterDescription <> "NONE" Then

                            If EnableWirelessNetworkConnection() = False Then
                                HandleUserMessageLogging("GMRC", "Enable Wireless connection failed.  Please verify that the wireless will connect to Wireless at Work.", DisplayMsgBox)
                                RadioButton5.Enabled = False
                                RadioButton17.Enabled = False
                                RadioButton18.Enabled = False
                                RadioButton2.Checked = False
                                Button1.Enabled = False
                                Button4.Enabled = False
                            Else

                                If WirelessUnavailable = True And GmlanConnectionUnavailable = False Then
                                    InhibitGmLanRadioButton = True
                                    RadioButton2.Checked = False
                                    RadioButton8.Checked = True
                                End If

                                Me.RadioButton5.Enabled = True
                                Me.RadioButton17.Enabled = True
                                Me.RadioButton18.Enabled = True

                                If RadioButton5.Checked = True Or RadioButton17.Checked = True Then
                                    Button1.Enabled = True
                                    Button4.Enabled = True
                                End If
                            End If

                        Else 'SaveNetworkAdapterDescription = "NONE"
                            HandleUserMessageLogging("GMRC", "Wireless not available.  To Upload Data, Please Connect to GM_LAN and Select GM_LAN...", DisplayMsgBox)
                            RadioButton5.Enabled = False
                            RadioButton17.Enabled = False
                            RadioButton18.Enabled = False
                            RadioButton2.Checked = False
                            Button1.Enabled = False
                            Button4.Enabled = False
                        End If

                    Else 'NetworkAdapterDescription = "GM_LAN"

                        If CheckForValidWirelessConnection() = False Then

                            HandleUserMessageLogging("GMRC", "Wireless not available.  To Upload Data, Please Connect to GM_LAN and Select GM_LAN...", DisplayMsgBox)
                            WirelessUnavailable = True
                            RadioButton5.Enabled = False
                            RadioButton17.Enabled = False
                            RadioButton18.Enabled = False
                            RadioButton2.Checked = False
                            Button1.Enabled = False
                            Button4.Enabled = False

                        Else
                            HandleUserMessageLogging("GMRC", "Checking Wireless Data Upload Path Validity...",,, FlashMsgOn)
                            System.Threading.Thread.Sleep(1000)

                            If Not Directory.Exists(NetworkDriveMapping & DataUploadPath) Then

                                HandleUserMessageLogging("GMRC", "Could not find the directory " & NetworkDriveMapping & DataUploadPath & " Please verify wireless connection before uploading data.", DisplayMsgBox)
                                Me.RadioButton5.Enabled = False
                                Me.RadioButton17.Enabled = False
                                Me.RadioButton18.Enabled = False

                            Else

                                HandleUserMessageLogging("GMRC", "Data Upload Path Valid " & NetworkDriveMapping & DataUploadPath)

                                Me.RadioButton5.Enabled = True
                                Me.RadioButton17.Enabled = True
                                Me.RadioButton18.Enabled = True

                            End If

                            UserStatusInfo.Hide()

                        End If

                    End If

                Else
                    InhibitWirelessRadioButton = False
                End If

            End If
        End If

    End Sub

    Private Sub RadioButton16_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton16.CheckedChanged

        If RadioButton16.Checked = True Then
            HandleUserMessageLogging("GMRC", "Today Only")
            ListBox1.Visible = False
            _searchString = ""
        End If

    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged

    End Sub

    Private Sub CheckBox5_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox5.CheckedChanged

    End Sub

    Private Sub RadioButton17_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton17.CheckedChanged

        'Upload HC Ride Data to Q Drive Radio Button on the UploadDataScreen...

        If RadioButton17.Checked = True Then
            'Because the RideData is a special case, we will save the original upload path and set the DataUploadPath to the \HighContent\RideData\ folder
            'We need to save the original DataUploadPath here so that if the user changes their mind, we can revert back to the standard DataUploadPath
            'for the vehicle type...

            If InStr(DataUploadPath, "TraileringData") = 0 Then
                SaveDataUploadPath = DataUploadPath
            End If

            DataUploadPath = "\HighContent\RideData\"
            HandleUserMessageLogging("GMRC", "DataUploadPath set to " & DataUploadPath)
            'If the Radio Button is selected, we will do a final verification that the network drive \ DataUploadPath is valid for the vehicle type
            'if we are connected to the netowrk drive and the folder does not yet exist it will be created in VerifyNetworkMapping...
            VerifyNetworkMapping()

        Else
            DataUploadPath = SaveDataUploadPath
            HandleUserMessageLogging("GMRC", "DataUploadPath set back to " & DataUploadPath)
        End If
    End Sub

    Private Sub RadioButton17_Click(sender As Object, e As EventArgs) Handles RadioButton17.Click

    End Sub

    Private Sub RadioButton18_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton18.CheckedChanged

        'Upload HC Trailering Data to Q Drive Radio Button on the UploadDataScreen...

        If RadioButton18.Checked = True Then
            'Because the RideData is a special case, we will save the original upload path and set the DataUploadPath to the \HighContent\RideData\ folder
            'We need to save the original DataUploadPath here so that if the user changes their mind, we can revert back to the standard DataUploadPath
            'for the vehicle type...

            If InStr(DataUploadPath, "RideData") = 0 Then
                SaveDataUploadPath = DataUploadPath
            End If

            DataUploadPath = "\HighContent\TraileringData\"
            HandleUserMessageLogging("GMRC", "DataUploadPath set to " & DataUploadPath)
            'If the Radio Button is selected, we will do a final verification that the network drive \ DataUploadPath is valid for the vehicle type
            'if we are connected to the netowrk drive and the folder does not yet exist it will be created in VerifyNetworkMapping...
            VerifyNetworkMapping()

        Else
            DataUploadPath = SaveDataUploadPath
            HandleUserMessageLogging("GMRC", "DataUploadPath set back to " & DataUploadPath)
        End If
    End Sub

    Public Sub UploadData()

        'This routine is performs some preliminary setup in preparation for uploading files to the share drive
        'or other user specified location.  Displays the UploadDataScreen...

        Dim dir As DirectoryInfo '= New DirectoryInfo(My.Application.Info.DirectoryPath & "\CANalyzer\StartupDTCs")
        Dim files As FileInfo()
        'Dim dirs As DirectoryInfo()

        Dim x As Integer

        Dim textline As String = ""

        Dim searchString As String = ""

        HandleUserMessageLogging("GMRC", "UploadData Called")

        DeleteUnusedDirectories(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber)

        If System.IO.Directory.Exists(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber) = False Then
            MsgBox(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & " not found.  No recorded data available for this vehicle.")
            Exit Sub
        End If

        AggregateAnnoFileName = My.Application.Info.DirectoryPath & "\" & VehicleNumber & "_AggregateAnnotations.csv"

        If Not Debugger.IsAttached Then
            ' Copy the GM_ResidentClient.log file

            FileCopy(My.Application.Info.DirectoryPath & "\GM_ResidentClient.log", Path.Combine(BaseDataCollectionPath, "Data", "gmcsv" & VehicleNumber, "GM_ResidentClient.log"))

            ' Check if AggregateAnnoFileName exists and copy it
            If File.Exists(AggregateAnnoFileName) Then

                FileCopy(AggregateAnnoFileName, Path.Combine(BaseDataCollectionPath, "Data", "gmcsv" & VehicleNumber, VehicleNumber & "_AggregateAnnotations.csv"))

            End If
        End If

        If Directory.Exists(My.Application.Info.DirectoryPath & "\CANalyzer\StartupDTCs") = True Then

            dir = New DirectoryInfo(My.Application.Info.DirectoryPath & "\CANalyzer\StartupDTCs")

            files = dir.GetFiles

            For x = 0 To UBound(files)

                If InStr(files(x).Name, "DTCsBeforeCodeClear") > 0 Then
                    'FileCopy(files(x).FullName, BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & "\" & files(x).Name)
                    FileCopy(files(x).FullName, BaseDataCollectionPath & "\Data\" & files(x).Name)
                End If

            Next

        End If

        'Me.RadioButton14.Checked = True
        Me.RadioButton4.Checked = True
        Me.RadioButton6.Checked = True
        Me.RadioButton10.Checked = True

        Me.Button1.Enabled = False

        If Len(SaveLoginId) > 0 Then
            Me.RadioButton13.Text = "Current User Only (" & SaveLoginId & ")"
            Me.RadioButton13.Checked = True
        Else
            Me.RadioButton13.Text = "Select User LoginID"
        End If

        If Me.Visible = False Then

            Me.ShowDialog()

        End If


    End Sub

    Private Sub VerifyNetworkMapping()

        'Verifies the existance of the UploadDataPath based on the radio button selected...

        Dim fullPath As String

        RadioButton4.Checked = True
        RadioButton6.Checked = True
        RadioButton10.Checked = True
        RadioButton15.Checked = True

        _selectedDriveLetter = NetworkDriveLetter

        If Mid(DataUploadPath, 1, 1) = "\" Then
            fullPath = _selectedDriveLetter & DataUploadPath
        Else
            fullPath = _selectedDriveLetter & "\" & DataUploadPath
        End If

        If Not Directory.Exists(fullPath) Then

            If InStr(_selectedDriveLetter, "\") = 0 Then

                HandleUserMessageLogging("GMRC", fullPath & " directory not found.  Mapping Drive...")

                If MapDrive(_selectedDriveLetter, NetworkDriveMapping) = False Then

                    HandleUserMessageLogging("GMRC", "Drive Mapping failed.  Data Cannot be Uploaded to " & fullPath & "gmcsv" & VehicleNumber & " at this time...", DisplayMsgBox, )
                    'HandleUserMessageLogging("GMRC", FullPath & " not found.  Data Cannot be Uploaded to " & FullPath & "gmcsv" & VehicleNumber & " at this time...", DisplayMsgBox, )
                    HandleUserMessageLogging("GMRC", "The NetworkDriveLetter defined in the " & My.Application.Info.DirectoryPath & "\config.xml file, currently (" & NetworkDriveLetter & "), MUST be mapped to \\Nam.corp.gm.com\tcws-dfs\project\CSV\CSAV2", DisplayMsgBox, )
                    HandleUserMessageLogging("GMRC", "Should you need to make changes to the config.xml file, Exit CLEVIR prior to making any changes...", DisplayMsgBox)

                    Exit Sub

                Else

                    If Not Directory.Exists(fullPath & "gmcsv" & VehicleNumber) Then
                        Directory.CreateDirectory(fullPath & "gmcsv" & VehicleNumber)
                    End If

                End If

            Else
                HandleUserMessageLogging("GMRC", "Directory " & fullPath & " not found.", DisplayMsgBox, )
                HandleUserMessageLogging("GMRC", "The NetworkDriveLetter information defined in the " & My.Application.Info.DirectoryPath & "\config.xml file, is currently (" & NetworkDriveLetter & ").  The required path derived from this drive mapping definition is \\Nam.corp.gm.com\tcws-dfs\project\CSV\CSAV2", DisplayMsgBox, )
                HandleUserMessageLogging("GMRC", "Please make sure that the drive mapping definition in the config.xml file is correct.  Should you need to make changes to the config.xml file, Exit CLEVIR prior to making any changes...", DisplayMsgBox)

                Exit Sub

            End If

        Else
            If Not Directory.Exists(fullPath & "gmcsv" & VehicleNumber) Then
                Directory.CreateDirectory(fullPath & "gmcsv" & VehicleNumber)
            End If
        End If

        If WirelessUnavailable = True Then
            RadioButton2.Checked = False
        End If

        If GmlanConnectionUnavailable = True Then
            RadioButton8.Checked = False
        End If

        If RadioButton2.Checked = True Or RadioButton8.Checked = True Then
            Button1.Enabled = True
            Button4.Enabled = True
        End If

    End Sub
End Class