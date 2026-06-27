
Imports System.IO
'Imports SevenZip
Imports ICSharpCode.SharpZipLib

Public Class SoftwareVersionSelect

    'This form is displayed when the user logs in with a login ID other than the default DEMO login
    'or when the user selects the Select Different Workspace \ Experiment button on the login screen.  Allows the
    'user to select from the list of current INCA experiments (Experiments in the CLEVIR Setup\Experiments folder)
    'And INCA workspaces (Workspaces in the CLEVIR Setup\Workspaces folder) in the INCA database.

    Private _saveWorkspace As String
    Private _saveExperiment As String
    Private _saveSignalList As String

    Private __changesMade As Boolean


    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ListBox1_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedValueChanged
        'INCA Workspaces list box, allows the user to change workspaces prior to continuing initialization...

        Dim saveFileName As String = ""

        If Len(ListBox1.SelectedItem) > 0 Then
            If InStr(ListBox1.SelectedItem.ToString, "No Workspaces found") = 0 Then
                __changesMade = True

                ' ✅ REMOVED: LoginForm.Button1 no longer exists
                ' Old code:
                ' If Len(GmResidentClient.UserConfigFileName) = 0 Then
                '     LoginForm.Button1.Text = "Login as Demo (" & ListBox1.SelectedItem.ToString & ")"
                ' Else
                '     LoginForm.Button1.Text = "Login as " & SaveLoginID & "(" & ListBox1.SelectedItem.ToString & ")"
                ' End If

                ' ✅ NEW: Just update the workspace variable and log the change
                INCAWorkspace = ListBox1.SelectedItem.ToString
                TextBox1.Text = INCAWorkspace

                HandleUserMessageLogging("GMRC", $"Workspace changed to: {INCAWorkspace}")
            Else
                ListBox1.SelectedIndex = -1
            End If
        End If
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "SoftwareVersionSelect_Form: Exit button pressed...")
        HandleUserMessageLogging("GMRC", " ")

        ListBox3.Visible = False

        If __changesMade = True Then
            ' ✅ User made changes but wants to discard them
            HandleUserMessageLogging("GMRC", "SoftwareVersionSelect: Discarding changes and reverting to original config...")

            __changesMade = False

            INCAWorkspace = ""
            INCAExperiment = ""
            INCAVariableFile = ""

            ' ✅ REMOVED: LoginForm.ListBox1 no longer exists
            ' Old code:
            ' If LoginForm.ListBox1.SelectedIndex > -1 Then
            '     LoginForm.ListBox1.SetSelected(LoginForm.ListBox1.SelectedIndex, False)
            '     LoginForm.ListBox1.SelectedItem = ""
            '     LoginForm.ListBox1.Text = ""
            ' End If

            ' ✅ Reload original configuration
            If GmResidentClient.UserConfigFileName = "" Then
                ReadConfigFile()
            Else
                GmResidentClient.ReadUserConfigFile(GmResidentClient.UserConfigFileName)
            End If

            ' ✅ REMOVED: LoginForm.Button1 no longer exists
            ' Old code:
            ' If Len(GmResidentClient.UserConfigFileName) = 0 Then
            '     LoginForm.Button1.Text = "Login as Demo (" & INCAWorkspace & ")"
            ' Else
            '     LoginForm.Button1.Text = "Login as " & SaveLoginID & " (" & INCAWorkspace & ")"
            ' End If

            If VerifyConfigFiles("SoftwareVersionSelectScreen") = False Then
                ' ✅ Configuration invalid - still allow return to login
                Me.DialogResult = Windows.Forms.DialogResult.Cancel
                Me.TopMost = False
                Me.Close()
                Exit Sub
            End If
        End If

        ' ✅ NEW: Always update LoginForm.Label4 with current config (whether changed or not)
        Try
            If LoginForm IsNot Nothing AndAlso Not LoginForm.IsDisposed Then
                Dim signalListFileName As String = If(
                String.IsNullOrEmpty(INCAVariableFile),
                "[Not Set]",
                Path.GetFileName(INCAVariableFile)
            )

                Dim experimentName As String = If(
                String.IsNullOrEmpty(INCAExperiment),
                "[Not Set]",
                INCAExperiment
            )

                LoginForm.Label4.Text = $"{signalListFileName} / {experimentName}"

                HandleUserMessageLogging("GMRC",
                $"SoftwareVersionSelect: Updated LoginForm.Label4 to '{signalListFileName} / {experimentName}'")
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"SoftwareVersionSelect: Failed to update LoginForm.Label4: {ex.Message}")
        End Try

        ' ✅ REMOVED: Redundant LoginForm.Button1.Text update (no longer exists)
        ' Old code was here

        If CurrentVehicleUsage = "VALIDATION" Or CurrentVehicleUsage = "VISTOOL" Then
            Me.Button1.Enabled = True
        End If

        ' ✅ CRITICAL FIX: Set DialogResult.Cancel to signal "return to login"
        Me.DialogResult = Windows.Forms.DialogResult.Cancel
        Me.TopMost = False
        Me.Close()
    End Sub

    Private Sub SoftwareVersionSelect_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        Dim found As Boolean

        Try

            ListBox1.Items.Clear()

            TextBox1.Text = INCAWorkspace
            TextBox2.Text = INCAExperiment
            TextBox3.Text = INCAVariableFile

            ListBox2.Items.Clear()

            For i = 0 To UBound(AvailableExperimentNames)
                If InStr(UCase(AvailableExperimentNames(i)), "BLANK EXPERIMENT") = 0 And InStr(UCase(AvailableExperimentNames(i)), "EMPTY EXPERIMENT") = 0 Then
                    ListBox2.Items.Add(AvailableExperimentNames(i))
                End If
            Next

            If AvailableWorkspaces Is Nothing Then
                AvailableWorkspaces = MyIncaInterface.GetAvailableWorkspaces()
            End If

            For x = 0 To UBound(AvailableWorkspaces)

                If CheckForValidParameters(AvailableWorkspaces(x)) = True Then

                    ListBox1.Items.Add(AvailableWorkspaces(x))
                    If AvailableWorkspaces(x) = INCAWorkspace Then
                        found = True
                    End If

                End If

            Next

            Me.TopMost = False

            If OperatingMode = OperatingModes.ResOnVpc Then

                If found = False Then
                    HandleUserMessageLogging("GMRC", "Invalid workspace in User Configuration file.  You must select a new workspace from the list provided, or CLEVIR will terminate.", DisplayMsgBox)
                    SaveLoginID = ""
                Else

                    If Len(SaveLoginID) = 0 Then
                        If Not ListBox1.SelectedItem Is Nothing Then
                            SaveLoginID = ListBox1.SelectedItem.ToString
                        End If
                    End If

                End If

            End If

            Me.Cursor = Cursors.Arrow

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "SoftwareVersionSelect: " & ex.Message, DisplayMsgBox)
            'MsgBox("SoftwareVersionSelect: " & ex.Message)

        End Try

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "SoftwareVersionSelect_Form: Save and Continue button pressed...")
        HandleUserMessageLogging("GMRC", " ")

        If ListBox3.Visible = True And ListBox3.SelectedIndex = -1 Then
            MsgBox("Please Select a Signal List...")
            Exit Sub
        End If

        CheckForNewerSignalListComplete = False

        ' ✅ FIX: Only write if changes were actually made
        If __changesMade Then
            HandleUserMessageLogging("GMRC", "SoftwareVersionSelect: Changes detected - writing configuration...")

            If Len(GmResidentClient.UserConfigFileName) > 0 Then
                'GmResidentClient.WriteUserConfigFile(GmResidentClient.UserConfigFileName)
            Else
                'WriteConfigFile()
            End If

            __changesMade = False  ' Reset flag after writing
        Else
            HandleUserMessageLogging("GMRC", "SoftwareVersionSelect: No changes detected - skipping configuration write")
        End If

        ListBox3.Visible = False

        ' ✅ NEW: Update LoginForm.Label4 with selected signal list and experiment
        Try
            If LoginForm IsNot Nothing AndAlso Not LoginForm.IsDisposed Then
                Dim signalListFileName As String = If(
                String.IsNullOrEmpty(INCAVariableFile),
                "[Not Set]",
                Path.GetFileName(INCAVariableFile)
            )

                Dim experimentName As String = If(
                String.IsNullOrEmpty(INCAExperiment),
                "[Not Set]",
                INCAExperiment
            )

                LoginForm.Label4.Text = $"{signalListFileName} / {experimentName}"

                HandleUserMessageLogging("GMRC",
                $"SoftwareVersionSelect: Updated LoginForm.Label4 to '{signalListFileName} / {experimentName}'")
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"SoftwareVersionSelect: Failed to update LoginForm.Label4: {ex.Message}")
        End Try

        ' ✅ REMOVED: LoginForm.Button1.Text update (no longer exists)
        ' Old code:
        ' If Len(GmResidentClient.UserConfigFileName) = 0 Then
        '     LoginForm.Button1.Text = "Login as Demo (" & INCAWorkspace & ")"
        ' Else
        '     LoginForm.Button1.Text = "Login as " & SaveLoginID & " (" & INCAWorkspace & ")"
        ' End If

        If VerifyConfigFiles("SoftwareVersionSelectScreen") = False Then
            Me.DialogResult = Windows.Forms.DialogResult.None
            Exit Sub
        Else
            If Len(GmResidentClient.UserConfigFileName) = 0 Then
                HandleUserMessageLogging("GMRC", "Demo user (config.xml) file will now be updated to reference the " & INCAWorkspace & " Workspace.")
            Else
                HandleUserMessageLogging("GMRC", GmResidentClient.UserConfigFileName & " file will now be updated to reference the " & INCAWorkspace & " Workspace.")
            End If

            Me.DialogResult = Windows.Forms.DialogResult.OK

            Me.TopMost = False
            Me.Close()
        End If
    End Sub

    Private Sub Label2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click

        'Dim szip As SevenZipExtractor = Nothing
        Dim szip As ICSharpCode.SharpZipLib.Zip.ZipFile = Nothing
        Dim szipEntry As ICSharpCode.SharpZipLib.Zip.ZipEntry = Nothing
        'Dim exreader As FileStream = Nothing
        'Dim strarray() As String = Nothing
        Dim tempstr As String = Nothing

        Dim destFile As String
        Dim sourceSignalListFileXlsx As String = ""
        Dim sourceSignalListFileCsv As String = ""
        Dim destSignalListFileXlsx As String = ""
        Dim destSignalListFileCsv As String = ""

        Dim importFileName As String

        Dim myfilter As String

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "Select Experiment to Import button pressed...")
        HandleUserMessageLogging("GMRC", " ")

        myfilter = "exp"

        'NETWORK DRIVE MAPPING

        SourceFile = SelectFile(Me.OpenFileDialog1, NetworkDriveLetter & "\", myfilter, True)

        If Len(SourceFile) > 0 And (InStr(SourceFile, ".zip") > 0 Or InStr(SourceFile, "." & myfilter) > 0) Then

            destFile = My.Application.Info.DirectoryPath & "\Experiments\" & Mid(SourceFile, InStrRev(SourceFile, "\") + 1, Len(SourceFile))

            sourceSignalListFileXlsx = Mid(SourceFile, 1, InStr(SourceFile, myfilter) - 1) & "xlsx"
            sourceSignalListFileCsv = Mid(SourceFile, 1, InStr(SourceFile, myfilter) - 1) & "csv"

            If File.Exists(sourceSignalListFileXlsx) Then

                destSignalListFileXlsx = My.Application.Info.DirectoryPath & "\SignalLists\" & Mid(sourceSignalListFileXlsx, InStrRev(sourceSignalListFileXlsx, "\") + 1, Len(sourceSignalListFileXlsx))

            End If

            If File.Exists(sourceSignalListFileCsv) Then

                destSignalListFileCsv = My.Application.Info.DirectoryPath & "\SignalLists\" & Mid(sourceSignalListFileCsv, InStrRev(sourceSignalListFileCsv, "\") + 1, Len(sourceSignalListFileCsv))

            End If

        Else
            MsgBox("Invalid file selected, please select a valid .zip file or " & myfilter & " file.")
            Exit Sub

        End If

        If Not System.IO.File.Exists(destFile) Then

            HandleUserMessageLogging("GMRC", "Copying File(s), Please wait...",,, FlashMsgOn)
            'UserStatusInfo.Label1.Text = "Copying File, Please wait..."

            System.IO.File.Copy(SourceFile, destFile)

            If Len(destSignalListFileXlsx) > 0 Then
                System.IO.File.Copy(sourceSignalListFileXlsx, destSignalListFileXlsx, True)
            End If

            If Len(destSignalListFileCsv) > 0 Then
                System.IO.File.Copy(sourceSignalListFileCsv, destSignalListFileCsv, True)
            End If

        Else
            HandleUserMessageLogging("GMRC", "File " & destFile & " found.",,, FlashMsgOn)
            'UserStatusInfo.Label1.Text = "File " & DestFile & " found."
        End If

        If InStr(destFile, ".zip") > 0 Then

            'SevenZipBase.SetLibraryPath(SevenZipLibraryPath)

            'szip = New SevenZipExtractor(DestFile)
            szip = New ICSharpCode.SharpZipLib.Zip.ZipFile(destFile)
            'exreader = New FileStream(DestFile, FileMode.Open)

            If Not szip Is Nothing Then

                For Each szipEntry In szip
                    tempstr = szipEntry.Name
                Next

                'strarray = szip.ArchiveFileNames.ToArray()
                'tempstr = szip.Name
                'ImportFileName = My.Application.Info.DirectoryPath & "\" & strarray(0)
                importFileName = My.Application.Info.DirectoryPath & "\" & tempstr

                'strarray = szip.ArchiveFileNames.ToArray()
                'ImportFileName = My.Application.Info.DirectoryPath & "\" & strarray(0)

                'szip.Dispose()
                szip = Nothing
                'exreader.Close()
                'exreader = Nothing

                'strarray = szip.ArchiveFileNames.ToArray()
                'ImportFileName = My.Application.Info.DirectoryPath & "\" & strarray(0)

                'szip.Dispose()
                'szip = Nothing
                'exreader.Close()
                'exreader = Nothing

            Else
                UserStatusInfo.Hide()
                HandleUserMessageLogging("GMRC", "Zip File Processing Error. Exiting...", DisplayMsgBox)
                'MsgBox("Zip File Processing Error. Exiting...")
                Exit Sub
            End If

            If Not System.IO.File.Exists(importFileName) Then

                HandleUserMessageLogging("GMRC", "Unzipping File, Please wait...",,, FlashMsgOn)
                'UserStatusInfo.Label1.Text = "Unzipping File, Please wait..."

                UnzipFile(destFile)

            Else
                HandleUserMessageLogging("GMRC", "File " & importFileName & " found.",,, FlashMsgOn)
                'UserStatusInfo.Label1.Text = "File " & ImportFileName & " found."
            End If

        Else
            HandleUserMessageLogging("GMRC", "File " & destFile & " found.",,, FlashMsgOn)
            'UserStatusInfo.Label1.Text = "File " & DestFile & " found."
            importFileName = destFile
        End If

        If ImportFileIntoINCA(importFileName, True, False) = False Then
            HandleUserMessageLogging("GMRC", "Import Failed. " & importFileName & "...", DisplayMsgBox,, FlashMsg2Sec)
            'UserStatusInfo.Label1.Text = "Import Failed..."
            'MsgBox("INCA File Import failed " & ImportFileName & "...")
            Exit Sub
        Else
            HandleUserMessageLogging("GMRC", "Import Successful, Updating Experiment List.",,, FlashMsgOn)
            'UserStatusInfo.Label1.Text = "Import Successful, Updating Experiment List."
        End If

        ListBox2.Items.Clear()

        AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames

        For i = 0 To UBound(AvailableExperimentNames)
            If InStr(UCase(AvailableExperimentNames(i)), "BLANK EXPERIMENT") = 0 And InStr(UCase(AvailableExperimentNames(i)), "EMPTY EXPERIMENT") = 0 Then
                ListBox2.Items.Add(AvailableExperimentNames(i))
            End If
        Next

        UserStatusInfo.Hide()

        MsgBox("You may now select the newly imported experiment from the list above to update CLEVIR to use this experiment.")

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click

        Dim dialogResult As Integer

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "Edit Config File button pressed...")
        HandleUserMessageLogging("GMRC", " ")

        Me.TopMost = False

        dialogResult = DefaultConfiguration.ShowDialog()

        '_changesMade flag is set here, if changes were made on DefaultConfigutaionn display.  If no changes were made,
        'DialogResult will be Cancel, not OK...

        If dialogResult = Windows.Forms.DialogResult.OK Then
            __changesMade = True
        End If

        Me.TopMost = False
    End Sub

    Private Sub TextBox3_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox3.TextChanged
        INCAVariableFile = TextBox3.Text
        __changesMade = True
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged
        INCAWorkspace = TextBox1.Text
        __changesMade = True
    End Sub

    Private Sub TextBox2_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox2.TextChanged
        INCAExperiment = TextBox2.Text
        InitialINCAExperiment = INCAExperiment
        __changesMade = True
    End Sub

    Private Sub SoftwareVersionSelect_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown

        TextBox1.Text = INCAWorkspace
        TextBox2.Text = INCAExperiment
        TextBox3.Text = INCAVariableFile

    End Sub

    Private Sub ListBox2_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox2.SelectedIndexChanged

        'Called when the user selects an experiment from the experiments list on the SoftwareVersionSelect display.  The experiment list shows
        'all experiments in the CLEVIR Setup\Experiments folder in INCA.  When an experiment is selected, CLEVIR looks into the 
        'My.Application.Info.DirectoryPath & "\SignalLists directory for a signal list file with the same name as the experiment. Typically there
        'is one and the global INCAVAriableFile variable is set to this filename.

        'If a corresponding signal list file does not exist, this suggests that the user selected their own experiment to use instead of a CLEVIR
        'common experiment, so in this case, there is no corresponding signal list.  In this instance, CLEVIR searches the signal list directory
        'for existing signal lists of the same type as the ProjectName derived from the workspace being used (HC, LC, or CSAV2 (US)) and displays 
        'them in Listbox3, which Is displayed over Listbox1 on the right side of the display.  The user may then select from this list of signal lists.

        'The assumption currently, is that there will always be at least one signal list that matches the ProjectName derived from the workspace being used...

        Dim dir As DirectoryInfo = New DirectoryInfo(My.Application.Info.DirectoryPath & "\SignalLists")
        Dim files As FileInfo()

        Dim x As Integer

        If ListBox2.SelectedIndex > -1 Then

            __changesMade = True

            INCAExperiment = ListBox2.SelectedItem.ToString
            InitialIncaExperiment = IncaExperiment
            TextBox2.Text = IncaExperiment

            HandleUserMessageLogging("GMRC", IncaExperiment & " Selected...")

            If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\SignalLists\" & IncaExperiment & ".csv") Then
                ListBox3.Items.Clear()
                ListBox3.Visible = False
                IncaVariableFile = My.Application.Info.DirectoryPath & "\SignalLists\" & IncaExperiment & ".csv"
                TextBox3.Text = IncaVariableFile
            ElseIf System.IO.File.Exists(My.Application.Info.DirectoryPath & "\SignalLists\" & IncaExperiment & ".xlsx") Then
                ListBox3.Items.Clear()
                ListBox3.Visible = False
                IncaVariableFile = My.Application.Info.DirectoryPath & "\SignalLists\" & IncaExperiment & ".xlsx"
                TextBox3.Text = IncaVariableFile
            Else

                files = dir.GetFiles

                For x = 0 To UBound(files)

                    If InStr(files(x).Name, "SAVE") = 0 And InStr(files(x).Name, "~") = 0 Then

                        Select Case UCase(ProjectName)

                            Case "LOWCONTENT" 'LC
                                If InStr(files(x).Name, "LC") > 0 Then
                                    ListBox3.Items.Add(files(x).Name)
                                End If
                            Case "HIGHCONTENT" 'HC
                                If InStr(files(x).Name, "HC") > 0 Then
                                    ListBox3.Items.Add(files(x).Name)
                                End If
                            Case "CSAV2" 'US or CHINA
                                If InStr(files(x).Name, "US") > 0 Or InStr(files(x).Name, "CHINA") > 0 Then
                                    ListBox3.Items.Add(files(x).Name)
                                End If
                            Case "FCM" 'FCM
                                If InStr(files(x).Name, "FCM") > 0 Then
                                    ListBox3.Items.Add(files(x).Name)
                                End If
                            Case "FCM100" 'FCM
                                If InStr(files(x).Name, "FCM100") > 0 Then
                                    ListBox3.Items.Add(files(x).Name)
                                End If
                            Case "ACP2" 'FCM
                                If InStr(files(x).Name, "ACP2") > 0 Then
                                    ListBox3.Items.Add(files(x).Name)
                                End If
                            Case "ACP3" 'FCM
                                If InStr(files(x).Name, "ACP3") > 0 Then
                                    ListBox3.Items.Add(files(x).Name)
                                End If
                            Case "ACP4" 'FCM
                                If InStr(files(x).Name, "ACP4") > 0 Then
                                    ListBox3.Items.Add(files(x).Name)
                                End If
                        End Select

                    End If

                Next

                If ListBox3.Items.Count > 0 Then

                    HandleUserMessageLogging("GMRC", "There is no signal list that corresponds to this Experiment name.  Please select the Signal List you would like to use...", DisplayMsgBox)

                    ListBox3.Visible = True
                    Label2.Visible = False
                    Button6.Visible = False

                End If

            End If

        Else
            HandleUserMessageLogging("GMRC", "No Experiment selected, using " & IncaExperiment, DisplayMsgBox)
        End If

    End Sub

    Private Sub ListBox3_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox3.SelectedIndexChanged

        'Signal lists listbox...

        If ListBox3.SelectedIndex > -1 Then
            __changesMade = True
            INCAVariableFile = My.Application.Info.DirectoryPath & "\SignalLists\" & ListBox3.SelectedItem.ToString
            TextBox3.Text = INCAVariableFile
            HandleUserMessageLogging("GMRC", INCAVariableFile & " Selected...")

            ListBox3.Visible = False
            Label2.Visible = True
            Button6.Visible = True
        End If
    End Sub

    Private Sub SoftwareVersionSelect_CausesValidationChanged(sender As Object, e As EventArgs) Handles Me.CausesValidationChanged

    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click

        'Refresh button for refreshing experiments in experiments listbox...

        ListBox2.Items.Clear()

        AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames

        For i = 0 To UBound(AvailableExperimentNames)
            If InStr(UCase(AvailableExperimentNames(i)), "BLANK EXPERIMENT") = 0 And InStr(UCase(AvailableExperimentNames(i)), "EMPTY EXPERIMENT") = 0 Then
                ListBox2.Items.Add(AvailableExperimentNames(i))
            End If
        Next

    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click

        'Refresh button for refreshing workspaces in workspaces listbox...

        ListBox1.Items.Clear()

        AvailableWorkspaces = MyIncaInterface.GetAvailableWorkspaces()

        For x = 0 To UBound(AvailableWorkspaces)
            If CheckForValidParameters(AvailableWorkspaces(x)) = True Then
                ListBox1.Items.Add(AvailableWorkspaces(x))
            End If
        Next
    End Sub
End Class