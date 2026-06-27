Option Strict Off

Public Class DisplayLogFile

    'This form is used in conjunction with the VehicleStatDashboard.  Displays status information obtained when
    'parsing GM_ResidentClient.log files.  Also displays contents of GM_ResidentClient.log files when requested...

    Private Function CopyFile(ByVal filename As String, ByVal defaultExt As String) As String

        'Copies the file passed in to the user selected path using the SaveFileDialog...
        'This function will also allow the user to have the camera files which correspond
        'to the data filename copied to the same location if desired...

        Dim baseFileName As String
        Dim answer As Integer
        Dim cameraSuffix As String
        Dim baseSaveFileName As String

        Dim filenameNoPath As String
        Dim vehicleDirectory As String

        CopyFile = ""

        baseFileName = Mid(filename, 1, Len(filename) - 4)

        If InStr(filename, "gmcsv") > 0 Then
            vehicleDirectory = "\" & Mid(filename, InStr(filename, "gmcsv"), 13)
        Else
            vehicleDirectory = ""
        End If

        filenameNoPath = Mid(filename, InStrRev(filename, "\") + 1, Len(filename))

        If Not System.IO.Directory.Exists(BaseLocalDataPath & vehicleDirectory) Then
            System.IO.Directory.CreateDirectory(BaseLocalDataPath & vehicleDirectory)
        Else

            If System.IO.File.Exists(BaseLocalDataPath & vehicleDirectory & "\" & filenameNoPath) Then

                If System.IO.File.Exists(BaseLocalDataPath & vehicleDirectory & "\" & Mid(filenameNoPath, 1, Len(filenameNoPath) - 4) & ".mf4") Then
                    CopyFile = BaseLocalDataPath & vehicleDirectory & "\" & Mid(filenameNoPath, 1, Len(filenameNoPath) - 4) & ".mf4"
                Else
                    CopyFile = BaseLocalDataPath & vehicleDirectory & "\" & filenameNoPath
                End If

                Exit Function
            End If
        End If

        CopyFile = BaseLocalDataPath & vehicleDirectory & "\" & filenameNoPath

        FileCopy(filename, BaseLocalDataPath & vehicleDirectory & "\" & filenameNoPath)

        Exit Function

        SaveFileDialog1.DefaultExt = defaultExt
        SaveFileDialog1.FileName = BaseLocalDataPath & vehicleDirectory & "\" & filenameNoPath
        SaveFileDialog1.InitialDirectory = BaseLocalDataPath & vehicleDirectory

        SaveFileDialog1.ShowDialog()

        If Len(SaveFileDialog1.FileName) > 0 And SaveFileDialog1.FileName <> filename Then

            HandleUserMessageLogging("GMRC", "Copying File, Please wait...",,, FlashMsgOn)

            FileCopy(filename, SaveFileDialog1.FileName)

            UserStatusInfo.Hide()

            baseSaveFileName = Mid(SaveFileDialog1.FileName, 1, Len(SaveFileDialog1.FileName) - 4)

            CopyFile = SaveFileDialog1.FileName

            answer = Cusmsgbox.DisplayCusMsgBox(Me, "Please Select Associated Camera Files to copy", "Custom User Input", "NONE", "FRONT ONLY", "ALL", "")

            Select Case answer

                Case 1 'None

                Case 2 'Front Only

                    cameraSuffix = "_Front.mp4"

                    If System.IO.File.Exists(baseFileName & cameraSuffix) Then

                        HandleUserMessageLogging("GMRC", "Copying FRONT only File, Please wait...",,, FlashMsgOn)

                        FileCopy(baseFileName & cameraSuffix, baseSaveFileName & cameraSuffix)

                    Else
                        UserStatusInfo.Hide()
                        MsgBox("No Corresponding Front Camera File Found")
                    End If

                Case 3 ' All

                    Dim x As Integer
                    cameraSuffix = ""

                    For x = 0 To 4
                        Select Case x
                            Case 0
                                cameraSuffix = "_Front.mp4"
                            Case 1
                                cameraSuffix = "_Driver.mp4"
                            Case 2
                                cameraSuffix = "_Left.mp4"
                            Case 3
                                cameraSuffix = "_Right.mp4"
                            Case 4
                                cameraSuffix = "_Rear.mp4"

                        End Select

                        If System.IO.File.Exists(baseFileName & cameraSuffix) Then

                            HandleUserMessageLogging("GMRC", "Copying ALL Camera Files, Please wait...",,, FlashMsgOn)

                            FileCopy(baseFileName & cameraSuffix, baseSaveFileName & cameraSuffix)

                        Else
                            UserStatusInfo.Hide()
                            MsgBox("No " & cameraSuffix & " Camera File Found.")
                        End If
                    Next

            End Select

        Else
            'TBD

        End If

        UserStatusInfo.Hide()

    End Function

    Private Sub LaunchXTool(ByVal filename As String, Optional ByVal startMsec As String = "")

        'Launches XTool with the passed in filename and optionally, the start_msec value 

        Dim dummy As Long
        Dim appstring As String
        Dim launchstring As String

        Dim xToolParams As String = "-a 7"
        Dim xToolInstallPath As String = "C:\Data\XMW\Xtool"
        Dim xToolConfigPath As String = "\MlabConfigFiles\V6\XtoolConfig.bin"


        'These three commented lines are here for reference from a previous similar application...
        'Start in C:\data\XMW\Xtool\MlabConfigFiles\V4
        'C:\Data\XMW\Xtool\Xtool.exe -c MlabConfigFiles\V4\XtoolConfig.bin -a 7
        'XToolInstallPath = "C:\Data\XMW\Xtool"

        Try

            If System.IO.File.Exists(filename) Then

                'appstring = """" & XToolInstallPath & "\XTool" & """" & " -c " & """" & XToolInstallPath & XToolConfigPath & """" '& " -a 7"
                appstring = """" & xToolInstallPath & "\XTool" & """" & " -c " & """" & xToolInstallPath & xToolConfigPath & """" & " " & xToolParams

                If Len(startMsec) > 0 Then
                    launchstring = appstring & " -f " & """" & filename & """" & " -t " & startMsec
                Else
                    launchstring = appstring & " -f " & """" & filename & """"
                End If

                dummy = Shell(launchstring, AppWinStyle.NormalFocus)

            Else
                MsgBox(filename & " not found, Exiting...")
            End If

        Catch ex As Exception
            MsgBox("LaunchXTool: " & ex.Message)
        End Try

    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged
        VehicleStatDashboard.Saveposition = ListBox1.SelectedIndex
    End Sub

    Private Sub ListBox1_DoubleClick(sender As Object, e As EventArgs) Handles ListBox1.DoubleClick


    End Sub

    Private Sub DisplayLogFile_DoubleClick(sender As Object, e As EventArgs) Handles Me.DoubleClick

    End Sub

    Public Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ListBox1.BringToFront()
        ListBox2.SendToBack()
        ListBox3.SendToBack()

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ListBox2.BringToFront()
        ListBox1.SendToBack()
        ListBox3.SendToBack()
    End Sub

    Private Sub ListBox2_DoubleClick(sender As Object, e As EventArgs) Handles ListBox2.DoubleClick

        Dim filename As String

        Dim tempstr() As String
        Dim myvehiclenumber As String
        Dim dateandtime As String

        Dim mySavePath As String = ""

        Dim fnum As String

        Dim textline As String

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer

        Dim foundFile As Boolean

        Dim myDirectories() As String
        Dim myFiles() As String

        Dim saveFileName As String

        If Me.Text = "Display Log File Status Info" Then

            If InStr(ListBox2.Items(ListBox2.SelectedIndex), "\\Nam") > 0 Then

                filename = Mid(ListBox2.Items(ListBox2.SelectedIndex), InStr(ListBox2.Items(ListBox2.SelectedIndex), "\\Nam"), Len(ListBox2.Items(ListBox2.SelectedIndex)))

                If System.IO.File.Exists(filename) Then

                    'filename = Mid(tempstr, 1, InStr(tempstr, ",") - 1)
                    'tempstr = Mid(tempstr, InStr(tempstr, ",") + 1, Len(tempstr))

                    'If InStr(tempstr, ",") > 0 Then
                    'numMsec = Mid(tempstr, 1, InStr(tempstr, ",") - 1)
                    'Else
                    'numMsec = tempstr
                    'End If

                    'Launch XTool if .mf4 file

                    If InStr(filename, ".mf4") > 0 Then
                        'If Val(numMsec) > 0 Then
                        'LaunchXTool(filename, numMsec)
                        'Else
                        LaunchXTool(filename)
                        'End If
                    End If

                    'Unzip and launch XTool if .zip file
                    If InStr(filename, ".zip") > 0 Then

                        'Choices.Button1.Text = "Unzip Selected File"
                        'Choices.ShowDialog()

                        Me.Cursor = Cursors.WaitCursor
                        saveFileName = CopyFile(filename, ".zip")
                        Me.Cursor = Cursors.Arrow

                        If Len(saveFileName) > 0 Then

                            If InStr(saveFileName, ".zip") > 0 Then

                                UnzipFile(saveFileName)

                                'If Val(numMsec) > 0 Then
                                'LaunchXTool(Mid(SaveFileName, 1, Len(SaveFileName) - 4) & ".mf4", numMsec)
                                'Else
                                LaunchXTool(Mid(saveFileName, 1, Len(saveFileName) - 4) & ".mf4")
                                'End If

                            Else
                                ' If Val(numMsec) > 0 Then
                                'LaunchXTool(SaveFileName, numMsec)
                                'Else
                                LaunchXTool(saveFileName)
                                'End If

                            End If

                        End If

                    End If

                Else
                    MsgBox(filename & " Does not exist...")
                End If

            Else

                'If InhibitListboxAction = True Then
                'InhibitListboxAction = False
                'Exit Sub
                'End If

                ListBox1.Items.Clear()

                tempstr = Split(ListBox2.SelectedItem.ToString, " - ")

                myvehiclenumber = tempstr(1)
                dateandtime = tempstr(0)

                For y = 0 To 6

                    Select Case y

                        Case 0
                            mySavePath = VehicleStatDashboard.MySavepathprefix & "\LogFiles\CSAV2\"
                        Case 1
                            mySavePath = VehicleStatDashboard.MySavepathprefix & "\LogFiles\LowContent\"
                        Case 2
                            mySavePath = VehicleStatDashboard.MySavepathprefix & "\LogFiles\HighContent\"
                        Case 3
                            mySavePath = VehicleStatDashboard.MySavepathprefix & "\LogFiles\ACP2\"
                        Case 4
                            mySavePath = VehicleStatDashboard.MySavepathprefix & "\LogFiles\ACP3\"
                        Case 5
                            mySavePath = VehicleStatDashboard.MySavepathprefix & "\LogFiles\ACP4\"
                        Case 6
                            mySavePath = VehicleStatDashboard.MySavepathprefix & "\LogFiles\FCM\"
                    End Select

                    myDirectories = System.IO.Directory.GetDirectories(mySavePath)

                    For x = 0 To UBound(myDirectories)
                        If InStr(myDirectories(x), myvehiclenumber) > 0 Then
                            myFiles = System.IO.Directory.GetFiles(myDirectories(x))
                            For z = 0 To UBound(myFiles)
                                If InStr(myFiles(z), "GM_ResidentClient.log") > 0 Then

                                    foundFile = True
                                    fnum = FreeFile()
                                    FileOpen(fnum, myFiles(z), OpenMode.Input)
                                    Do While Not EOF(fnum)
                                        textline = LineInput(fnum)
                                        ListBox1.Items.Add(textline)
                                    Loop
                                    FileClose(fnum)

                                    For i = 0 To ListBox1.Items.Count - 1

                                        If InStr(ListBox1.Items(i).ToString, dateandtime) > 0 Then
                                            ListBox1.SelectedIndex = i
                                            Exit For
                                        End If
                                    Next i

                                    Exit For
                                End If
                            Next z
                            Exit For
                        End If

                        If foundFile = True Then
                            Exit For
                        End If
                    Next x
                    If foundFile = True Then
                        Exit For
                    End If
                Next y

                If foundFile = True Then
                    'Me.Text = vehiclenumber
                    'DisplayLogFile.Show()
                    ListBox1.BringToFront()
                    ListBox2.SendToBack()
                End If

            End If

        End If

    End Sub

    Private Sub ListBox2_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedValueChanged

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        ListBox3.BringToFront()
        ListBox2.SendToBack()
        ListBox1.SendToBack()
    End Sub

    Private Sub ListBox3_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox3.SelectedIndexChanged

    End Sub

    Private Sub ListBox3_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox3.SelectedValueChanged

        Dim cnt As Integer
        Dim tempstr() As String
        Dim mySym As String = ""

        '
        If VehicleStatDashboard.DisableListbox3Select = True Then
            Exit Sub
        End If

        mySym = Mid(ListBox3.SelectedItem.ToString, InStr(ListBox3.SelectedItem.ToString, ",") + 1, Len(ListBox3.SelectedItem.ToString))

        Clipboard.SetText(mySym)

        If InStr(ListBox3.SelectedItem.ToString, "USMPG") > 0 Then
            VehicleStatDashboard.GetLocalFilePaths("W:\CSAV2 Tools\CLEVIR\Development\PC_HostNameLogFiles")
        Else
            VehicleStatDashboard.GetLocalFilePaths()
        End If

        For cnt = 0 To UBound(VehicleStatDashboard.LocalLogFilePaths)

            tempstr = Split(ListBox3.SelectedItem.ToString, ",")

            If InStr(VehicleStatDashboard.LocalLogFilePaths(cnt), tempstr(0)) > 0 Then
                RunNotepad(VehicleStatDashboard.LocalLogFilePaths(cnt) & "\GM_ResidentClient.log")
                Exit Sub
            End If

        Next cnt
    End Sub

    Private Sub DisplayLogFile_Load(sender As Object, e As EventArgs) Handles Me.Load

    End Sub

    Private Sub DisplayLogFile_Shown(sender As Object, e As EventArgs) Handles Me.Shown

    End Sub
End Class