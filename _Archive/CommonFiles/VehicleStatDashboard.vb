Option Strict Off

Imports VB = Microsoft.VisualBasic

Imports System.IO
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Speech.Synthesis

Public Class VehicleStatDashboard

    'This form provides lots of information about each in-vehicle PC running CLEVIR.  Provides real-time updates from every CLEVIR PC connected to the network
    'whenever CLEVIR is running. Allows every GM_ResidentClient.log file to be copied from the share drive to the admins local drive and processes the contents
    'of each file to calculate initialization times, record start / stop delay times, and analyzes the contents of each log file to identify potential user issues
    'such as hangups, abnormal exits, initialization issues, etc.  This information can be used to identify bugs in the CLEVIR software or potential software
    'fixes or improvements.  This dashboard is only available to CLEVIR administrator, or if running in debug mode out of the VS design environment, intended for
    'use only by the CLEVIR admin.

    'This form is accessible via a yes/no message that is displayed at startup, only if running in debug mode, or if the user has logged in as administrator.
    'If a no response is given to message, CLEVIR will start normally as for any other user...

    'Login in as an administrator is handled using a file called adminPCs.txt in the install folder (or development folder). If the PC hostname is listed in
    'this file, a message will be displayed asking for the administrator password which is "poc".  If correct password is entered, user has administrator
    'usage privileges and the yes/no messsage for accessing the VehicleStatDashboard will be displayed...

    Public LocalLogFilePaths() As String
    Public DisableListbox3Select As Boolean

    Private VehicleNumberChanged As Boolean

    Public ReadOnly mySavepathprefix As String = My.Application.Info.DirectoryPath & "\CLEVIR_Vehicle_Status_Info"
    Private mypathprefix As String = "\\Nam.corp.gm.com\tcws-dfs\Project\CSV\CSAV2"

    Private InhibitListboxAction As Boolean

    Private EventDateTime As String '= "01/01/2020"
    Private StartDateTime As String '= "01/01/2020"

    Private SearchString() As String = Nothing

    'Private VehicleNumber As String
    Private VersionNumber As String
    Private AdditionalSearchCriterian As String

    Private ErrorsList As New List(Of String)
    Private DelayTimesList As New List(Of String)
    Private InitDelayTimesList As New List(Of String)
    Private SoftwareVersionsList As New List(Of String)

    Private NumVersionsSelected As Integer
    Public saveposition As Integer = 0

    Private VehicleID As String

    Private LogList As New List(Of String)

    Private _BackgroundTasks As BackgroundTasks

    Private Delegate Sub BackgroundTasks()

    Private EnableMyBackgroundTasks As Boolean
    Private CopyingLogFiles As Boolean

    Private SaveDirectoryName As String
    Private ReadOnly SignalListDirectory As String = My.Application.Info.DirectoryPath & "\SignalLists\"

    Private TurnOffShowNetworkConnectedPCs As Boolean

    Private LogFileType As String
    Private Sub ShowNetworkConnectedPCs()

        'Called from MyBackgroundTasks (currently every 15 minutes) and also can be called from the drop down utilities menu...
        'Shows any PC that is currently connected to the GM Network and running CLEVIR.  There will be false positives in this
        'list however, because the status is only upodated from the PCs when CLEVIR is launched or exited...

        Dim sourcedirname As String = NetworkDriveMapping & ClevirBaseDir & "\Development\PC_HostNameLogFiles"

        If TurnOffShowNetworkConnectedPCs = True Then
            Exit Sub
        End If

        Me.Cursor = Cursors.WaitCursor
        Me.Refresh()

        If Directory.Exists(sourcedirname) Then

            Dim dir As New DirectoryInfo(sourcedirname)
            Dim dirs As DirectoryInfo() = dir.GetDirectories()

            ListBox8.Items.Clear()

            For Each subdir In dirs

                If CheckAvailability(subdir.FullName) = True Then
                    ListBox8.Items.Add(subdir.Name)
                End If

                'System.Windows.Forms.Application.DoEvents()

            Next

            If ListBox8.Items.Count > 0 Then

                GroupBox5.Text = "Network Connected PCs"

                If GroupBox5.Visible = False Then
                    GroupBox5.Visible = True
                    GroupBox5.BringToFront()
                    ListBox8.Visible = True
                    GroupBox5.Refresh()
                End If

            Else
                GroupBox5.Visible = False
            End If

        End If

        Me.Cursor = Cursors.Arrow
        Me.Refresh()


    End Sub

    Private Sub CheckForAssistanceRequests()

        'Called from myBackgroundTasks if the availability button on the main VehicleStatDashboard has been pressed and indicates "available".
        'Monitors for assistance requestes from network connected PCs.

        'When available is true, all PCs that are on the network will have the "Request Assistance" button displayed and if the user presses the button and
        'enters their GM ID, CLEVIR will write to a file on the share drive.  The file for each PC is monitored using this routine and if anyone requests
        'assistance, a message box will be displayed with the GM ID entered.  This allows the  CLEVIR administrator to determine who is in the vehicle
        'and contact them to support.

        Dim sourcedirname As String = NetworkDriveMapping & ClevirBaseDir & "\Development\PC_HostNameLogFiles"
        Dim fnum As Integer
        Dim textline As String

        If Directory.Exists(sourcedirname) Then

            Dim dir As DirectoryInfo = New DirectoryInfo(sourcedirname)
            Dim dirs As DirectoryInfo() = dir.GetDirectories()

            For Each subdir In dirs

                For Each file In subdir.GetFiles
                    If InStr(UCase(file.Name), "REQUESTID") > 0 Then

                        fnum = FreeFile()

                        If Not FileInUse(file.FullName) Then

                            FileOpen(fnum, file.FullName, OpenMode.Input)
                            textline = LineInput(fnum)
                            FileClose(fnum)

                            MsgBox(textline & " is requesting assistance - PC = " & subdir.Name)

                            file.Delete()

                        End If

                    End If
                Next

            Next

        End If
    End Sub

    Private Sub RemoveUnusedFoldersFromLocal(ByVal sourcepath As String, ByVal targetpath As String, Optional ByVal FolderDeleteStrings As List(Of String) = Nothing)

        'Called from myBackgoundTasks once an hour. Or can be called from a utilities menu drop down item.  Used in conjunction with CopyFilesFromShareToLocal
        'We maintain a mirror copy of the CLEVIR share drive contents on the local drive.  This so that should someone accidentally move the CLEVIR on the share drive
        '(which as been done several times by accident) folder, we can re-create it.  This is a two step process, first CopyFilesFromShareToLocal is called which
        'updates all existing CLEVIR files on the local drive, next this routine is called to remove any unused folders that may have been removed on the
        'share drive.

        Dim SourceDriveLetter As String
        Dim FolderToDelete As String = ""

        Try

            If Not Directory.Exists(sourcepath) And sourcepath = "Q:\CSAV2 Tools\CLEVIR" Then
                Exit Sub
            End If
            System.Windows.Forms.Application.DoEvents()

            SourceDriveLetter = Mid(sourcepath, 1, 3)

            If Directory.Exists(targetpath) Then

                If Not Directory.Exists(sourcepath) Then

                    'here we need to delete all files in targetpath and delete the directory...
                    For Each targetfilepath As String In Directory.GetFiles(targetpath)
                        HandleUserMessageLogging("GMRC", "RemoveUnusedFoldersFromLocal: Deleting File " & targetfilepath,,,, ListBox9)
                        System.IO.File.Delete(targetfilepath)
                    Next
                    HandleUserMessageLogging("GMRC", "RemoveUnusedFoldersFromLocal: " & targetpath,,,, ListBox9)
                    FolderToDelete = targetpath

                End If

                For Each targetdir As String In Directory.GetDirectories(targetpath)

                    RemoveUnusedFoldersFromLocal(SourceDriveLetter & Mid(targetdir, 4, Len(targetdir)), targetdir)
                Next

            End If

            If Len(FolderToDelete) > 0 Then
                System.IO.Directory.Delete(FolderToDelete)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "RemoveUnusedFoldersFromLocal: " & ex.Message,,,, ListBox9)
        End Try

    End Sub

    Private Sub RemoveOldFilesFromLocal(ByVal sourcepath As String, ByVal targetpath As String)

        'Called from CopyFilesFromShareToLocal - removes CLEVIR files on local drive that are no longer on share drive...

        Dim found As Boolean

        Try

            If Directory.Exists(sourcepath) Then

                If Directory.Exists(targetpath) Then

                    For Each targetfilepath As String In Directory.GetFiles(targetpath)

                        For Each sourcefilepath As String In Directory.GetFiles(sourcepath)
                            If Path.GetFileName(sourcefilepath) = Path.GetFileName(targetfilepath) Then
                                found = True
                                Exit For
                            End If
                        Next

                        If found = False Then
                            HandleUserMessageLogging("GMRC", "RemoveOldFilesFromLocal: Deleting " & targetfilepath,,,, ListBox9)
                            File.Delete(targetfilepath)
                        Else
                            found = False
                        End If

                    Next

                End If

            Else 'this else condition will never be reached right now...

                If Directory.Exists(targetpath) Then
                    'here we need to delete all files in targetpath and delete the directory...
                    For Each targetfilepath As String In Directory.GetFiles(targetpath)
                        HandleUserMessageLogging("GMRC", "RemoveOldFilesFromLocal: Deleting File " & targetfilepath,,,, ListBox9)
                        System.IO.File.Delete(targetfilepath)
                    Next
                    HandleUserMessageLogging("GMRC", "RemoveOldFilesFromLocal: Deleting Folder " & targetpath,,,, ListBox9)
                    System.IO.Directory.Delete(targetpath)
                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "RemoveOldFilesFromLocal: " & ex.Message,,,, ListBox9)
        End Try

    End Sub

    Private Sub HandlePCBasedLogFiles()

        'Called from Copy PC Logs from Q - Button on main screen...
        'Copies the PC Specific GM_ResidentClient.log files on the share drive to the local drive...
        'Parses the information from the files and displays the information gathered in the appropriate
        'list boxes on the main form.

        'Parsed information is written to various .csv files.  These files are then read in after the
        'parsing is complete and the information is stored in string list global variables, one string
        'list associated with each .csv file.  The information in the lists is then accessed to display
        'the information in list boxes on the main screen...

        'The copying and parsing is handled in CopyPCBasedLogFilesFromShareToLocal routine...

        'Display of infomration is handled by the LoadLists routine...

        Dim sourcepath As String = "Q:\CSAV2 Tools\CLEVIR\Development\PC_HostNameLogFiles"
        Dim targetpath As String = "W:\CSAV2 Tools\CLEVIR\Development\PC_HostNameLogFiles"
        Dim fnum As Integer
        Dim fnum2 As Integer
        Dim textline As String
        Dim TextlineArray() As String
        Dim UpdatedPCInfoArray As ArrayList
        Dim LastUploadTimeArray As ArrayList
        Dim UpdatedDisplayArray As ArrayList
        Dim TextLineArray2() As String

        CopyPCBasedLogFilesFromShareToLocal(sourcepath, targetpath)

        UpdatedPCInfoArray = New ArrayList
        LastUploadTimeArray = New ArrayList
        UpdatedDisplayArray = New ArrayList

        fnum2 = FreeFile()
        FileOpen(fnum2, mypathprefix & ClevirBaseDir & "\PCNetworkConnectStatus\Updated_PCInfoSAVE.csv", OpenMode.Input)

        Do While Not EOF(fnum2)

            textline = LineInput(fnum2)
            TextlineArray = Split(textline, ",")
            UpdatedPCInfoArray.Add(TextlineArray(1) & "," & TextlineArray(0) & "," & TextlineArray(2)) 'C121N030, USMPGTNCSV0057, 1 / 13 / 2021

        Loop

        FileClose(fnum2)

        UpdatedPCInfoArray.Sort()

        fnum = FreeFile()
        FileOpen(fnum, mySavepathprefix & "\Reports\HostnameBasedReports\LastUploadTime.csv", OpenMode.Input)

        Do While Not EOF(fnum)

            textline = LineInput(fnum)
            LastUploadTimeArray.Add(textline)

        Loop

        FileClose(fnum)

        LastUploadTimeArray.Sort()

        GoTo bypassMerge

        'Merge UpdatedPCInfoArray info with LastUploadTimeArray info into UpdatedDisplayArray

        Dim FoundMatch As Boolean

        For x = 0 To LastUploadTimeArray.Count - 1
            FoundMatch = False
            TextlineArray = Split(LastUploadTimeArray(x), ",")
            For y = 0 To UpdatedPCInfoArray.Count - 1
                TextLineArray2 = Split(UpdatedPCInfoArray(y), ",")
                If InStr(UpdatedPCInfoArray(y).ToString, TextlineArray(0)) > 0 Then
                    UpdatedDisplayArray.Add(LastUploadTimeArray(x) & "," & TextLineArray2(1) & "," & TextLineArray2(2))
                    FoundMatch = True
                    Exit For
                End If
            Next
            If FoundMatch = False Then
                UpdatedDisplayArray.Add(LastUploadTimeArray(x).ToString & ",NA,NA")
            End If

        Next

bypassMerge:

        UpdatedDisplayArray = LastUploadTimeArray

        ListBox1.Items.Clear()

        For x = 0 To UpdatedDisplayArray.Count - 1

            'split on spaces, display 0, 3 and 4

            ListBox1.Items.Add(UpdatedDisplayArray(x).ToString)

        Next x

        '********************************************************************

        fnum = FreeFile()
        FileOpen(fnum, mySavepathprefix & "\Reports\HostnameBasedReports\Errors.csv", OpenMode.Output)

        ListBox3.Items.Clear()

        ErrorsList.Sort()

        For x = 0 To ErrorsList.Count - 1
            ListBox3.Items.Add(ErrorsList(x))
            PrintLine(fnum, ErrorsList(x))
        Next

        FileClose(fnum)

        fnum = FreeFile()
        FileOpen(fnum, mySavepathprefix & "\Reports\HostnameBasedReports\DelayTimes.csv", OpenMode.Output)

        ListBox4.Items.Clear()

        DelayTimesList.Sort()

        For x = 0 To DelayTimesList.Count - 1
            textline = DelayTimesList(x)
            If InStr(textline, "VEHNUM") > 0 Then
                ListBox4.Items.Add(textline)
            Else
                TextlineArray = Split(textline, ",")
                ListBox4.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3).PadRight(15, " ") & "," & TextlineArray(4).PadRight(14, " ") & "," & TextlineArray(5).PadRight(10, " ") & "," & TextlineArray(6).PadRight(22, " ") & "," & TextlineArray(7).PadRight(14, " ") & "," & TextlineArray(8).PadRight(26, " ") & "," & TextlineArray(9).PadRight(21, " ") & "," & TextlineArray(10).PadRight(13, " "))
                PrintLine(fnum, DelayTimesList(x))
            End If
        Next x

        FileClose(fnum)

        fnum = FreeFile()

        FileOpen(fnum, mySavepathprefix & "\Reports\HostnameBasedReports\InitDelayTimes.csv", OpenMode.Output)
        ListBox6.Items.Clear()

        InitDelayTimesList.Sort()

        For x = 0 To InitDelayTimesList.Count - 1
            textline = InitDelayTimesList(x)
            If InStr(textline, "VEHNUM") > 0 Then
                ListBox6.Items.Add(textline)
            Else
                TextlineArray = Split(textline, ",")
                ListBox6.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3))
                PrintLine(fnum, InitDelayTimesList(x))
            End If
        Next x

        FileClose(fnum)

        fnum = FreeFile()
        FileOpen(fnum, mySavepathprefix & "\Reports\HostnameBasedReports\SoftwareVersions.csv", OpenMode.Output)

        ListBox5.Items.Clear()

        SoftwareVersionsList.Sort()
        SoftwareVersionsList.Reverse()

        For x = 0 To SoftwareVersionsList.Count - 1
            ListBox5.Items.Add(SoftwareVersionsList(x))
            PrintLine(fnum, SoftwareVersionsList(x))
        Next

        FileClose(fnum)

        GetLocalFilePaths("W:\CSAV2 Tools\CLEVIR\Development\PC_HostNameLogFiles")

        'This is the Display Alerts Check box on the main VehicleStatDashboard...
        If CheckBox1.Checked = True Then

            'LogList contains a list of ALERTS such as PROC COMM ALERT, INVALID VIDEO ALERT, INVALID DATA ALERT and some additional error types which are displayed in the DisplayLogFile.ListBox2
            'We can turn on and off the display of these alerts with the Display Alerts Checkbox on the main form.  The default is not to display, checkbox unchecked...
            For x = 0 To LogList.Count - 1
                DisplayLogFile.ListBox2.Items.Add(LogList(x))
            Next

        End If

        Me.Cursor = Cursors.Arrow
        CopyingLogFiles = False

        VehicleNumber = ""

        LoadLists("HostnameBasedReports\")

    End Sub

    Private Sub CopyPCBasedLogFilesFromShareToLocal(ByVal sourcepath As String, ByVal targetpath As String)

        'Called from HandlePCBasedLogFiles, also recursively calls itself...
        'Copies the PC Specific GM_ResidentClient.log files on the share drive to the local drive...
        'Parses the information from the files.  The Parsed information is written to various .csv files.

        Dim SourceDriveLetter As String
        Dim TargetDriveLetter As String
        Dim targetdir As String

        Try
            Me.Cursor = Cursors.WaitCursor
            CopyingLogFiles = True

            System.Windows.Forms.Application.DoEvents()

            SourceDriveLetter = Mid(sourcepath, 1, 3)
            TargetDriveLetter = Mid(targetpath, 1, 3)

            If Directory.Exists(sourcepath) Then

                If Not Directory.Exists(TargetDriveLetter & Mid(sourcepath, 4, Len(sourcepath))) Then
                    HandleUserMessageLogging("GMRC", "Creating directory " & TargetDriveLetter & Mid(sourcepath, 4, Len(sourcepath)),,,, ListBox9)
                    Directory.CreateDirectory(TargetDriveLetter & Mid(sourcepath, 4, Len(sourcepath)))
                End If

                'Copy all files from the Directory

                For Each filepath As String In Directory.GetFiles(sourcepath)

                    Try

                        If InStr(filepath, "GM_ResidentClient.log") > 0 Then

                            If System.IO.File.Exists(targetpath & "\" & Path.GetFileName(filepath)) Then
                                If System.IO.File.GetLastWriteTime(filepath) > System.IO.File.GetLastWriteTime(targetpath & "\" & Path.GetFileName(filepath)) Then

                                    'Copy Existing log file to SAVE file...
                                    File.Copy(targetpath & "\GM_ResidentClient.log", targetpath & "\GM_ResidentClient.log.SAVE", True)

                                    HandleUserMessageLogging("GMRC", "Copying file " & filepath & " to " & targetpath & "\" & Path.GetFileName(filepath),,,, ListBox9)
                                    File.Copy(filepath, targetpath & "\" & Path.GetFileName(filepath), True)

                                    Dim SourceFile As New System.IO.FileInfo(filepath)
                                    Dim DestFile As New System.IO.FileInfo(targetpath & "\GM_ResidentClient.log.SAVE")

                                    If SourceFile.Length < DestFile.Length Then
                                        AppendLogFileInfo(targetpath & "\GM_ResidentClient.log")
                                    End If

                                    VehicleNumber = "UNKNOWN"
                                    ParseCLEVIRLogFileNEW(targetpath & "\GM_ResidentClient.log")

                                End If

                                'temporary for testing...
                                'VehicleNumber = "UNKNOWN"
                                'ParseCLEVIRLogFileNEW(targetpath & "\GM_ResidentClient.log")

                            Else
                                HandleUserMessageLogging("GMRC", "Copying NEW file " & filepath & " to " & targetpath & "\" & Path.GetFileName(filepath),,,, ListBox9)
                                File.Copy(filepath, targetpath & "\" & Path.GetFileName(filepath))

                                VehicleNumber = "UNKNOWN"
                                ParseCLEVIRLogFileNEW(targetpath & "\GM_ResidentClient.log")

                            End If

                        End If

                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", "CopyPCBasedLogFilesFromShareToLocal: Files For Loop: " & ex.Message,,,, ListBox9)
                    End Try

                Next

                'Copy Files from all child Directories

                For Each dir As String In Directory.GetDirectories(sourcepath)

                    Try
                        targetdir = TargetDriveLetter & Mid(dir, 4, Len(dir))

                        CopyPCBasedLogFilesFromShareToLocal(dir, targetdir)
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", "CopyPCBasedLogFilesFromShareToLocal: For Loop: " & ex.Message,,,, ListBox9)
                    End Try

                Next

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CopyPCBasedLogFilesFromShareToLocal: " & sourcepath & " - " & ex.Message, DisplayMsgBox,,, ListBox9)
        End Try


    End Sub

    Private Sub CopyFilesFromShareToLocal(ByVal sourcepath As String, ByVal targetpath As String)

        'Called from myBackgoundTasks once an hour. Or can be called from a utilities menu drop down item.  Used in conjunction with RemoveUnusedFoldersFromLocal
        'We maintain a mirror copy of the CLEVIR share drive contents on the local drive.  This so that should someone accidentally move the CLEVIR on the share drive
        '(which as been done several times by accident) folder, we can re-create it.  This is a two step process, first this routine, CopyFilesFromShareToLocal is called which
        'updates all existing CLEVIR files on the local drive, next the RemoveUnusedFoldersFromLocal routine is called to remove any unused folders that may have been removed on the
        'share drive.

        'Recursively copies files in directories and subdirectories from "sourcepath" to "targetpath"

        Dim SourceDriveLetter As String
        Dim TargetDriveLetter As String
        Dim targetdir As String

        Try

            System.Windows.Forms.Application.DoEvents()

            SourceDriveLetter = Mid(sourcepath, 1, 3)
            TargetDriveLetter = Mid(targetpath, 1, 3)

            If Directory.Exists(sourcepath) And sourcepath <> "Q:\CSAV2 Tools\CLEVIR\GLAHS\BatchProcessing" And sourcepath <> "Q:\CSAV2 Tools\CLEVIR\Development\PC_HostNameLogFiles" Then

                If Not Directory.Exists(TargetDriveLetter & Mid(sourcepath, 4, Len(sourcepath))) Then
                    HandleUserMessageLogging("GMRC", "Creating directory " & TargetDriveLetter & Mid(sourcepath, 4, Len(sourcepath)),,,, ListBox9)
                    Directory.CreateDirectory(TargetDriveLetter & Mid(sourcepath, 4, Len(sourcepath)))
                Else
                    RemoveOldFilesFromLocal(sourcepath, targetpath)
                End If

                'Copy all files from the Directory

                For Each filepath As String In Directory.GetFiles(sourcepath)

                    Try

                        If System.IO.File.Exists(targetpath & "\" & Path.GetFileName(filepath)) Then
                            If System.IO.File.GetLastWriteTime(filepath) > System.IO.File.GetLastWriteTime(targetpath & "\" & Path.GetFileName(filepath)).AddMinutes(10) Then
                                HandleUserMessageLogging("GMRC", "Copying file " & filepath & " to " & targetpath & "\" & Path.GetFileName(filepath),,,, ListBox9)
                                File.Copy(filepath, targetpath & "\" & Path.GetFileName(filepath), True)
                            End If
                        Else
                            HandleUserMessageLogging("GMRC", "Copying NEW file " & filepath & " to " & targetpath & "\" & Path.GetFileName(filepath),,,, ListBox9)
                            File.Copy(filepath, targetpath & "\" & Path.GetFileName(filepath))
                        End If

                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", "CopyFilesFromShareToLocal: Files For Loop: " & ex.Message,,,, ListBox9)
                    End Try

                Next

                'Copy Files from all child Directories

                For Each dir As String In Directory.GetDirectories(sourcepath)

                    Try
                        targetdir = TargetDriveLetter & Mid(dir, 4, Len(dir))

                        CopyFilesFromShareToLocal(dir, targetdir)
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", "CopyFilesFromShareToLocal: For Loop: " & ex.Message,,,, ListBox9)
                    End Try

                Next

            End If

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "CopyFilesFromShareToLocal: " & sourcepath & " - " & ex.Message,,,, ListBox9)
        End Try

    End Sub

    'times do not display when selecting a criterian from the bottom because delaytimes does not have that string, only version and vehicle number
    'should separate all three file types and have their own handling 

    Private Sub FindStringInAllFiles(Optional ByVal inputstr As String = "")

        'Allows us to search for a specific string (not case sensitive) in all log files available on local drive.
        'Lists all instances of string in vehicle log files by vehicle number.

        Dim mySavepath As String = ""
        Dim myfiles() As String
        Dim fnum As Integer
        Dim textline As String
        Dim y As Integer
        Dim i As Integer
        Dim SaveTextline As String = ""
        Dim MultiplesFound As Boolean
        Dim AndNotString As String = ""

        Dim DirectoryName As String = ""

        Dim SearchString As String = ""

        If Len(inputstr) = 0 Then
            'If we enter a string here, we find all instances of string in textbox2.text, as long as string does not include AndNotString...

            If Len(TextBox2.Text) = 0 Then
                MsgBox("Please enter a search string...")
                Exit Sub
            End If

            AndNotString = InputBox("Enter NOT String...")
            SearchString = TextBox2.Text
        Else
            SearchString = inputstr
        End If

        DisableListbox3Select = True

        If MsgBox("Search Local Computer Log File Directory?", vbYesNo) = vbYes Then
            GetLocalFilePaths("W:\CSAV2 Tools\CLEVIR\Development\PC_HostNameLogFiles")
        Else
            GetLocalFilePaths()
        End If

        DisplayLogFile.ListBox3.Items.Clear()

        DisplayLogFile.Show()
        DisplayLogFile.BringToFront()
        DisplayLogFile.ListBox3.BringToFront()
        DisplayLogFile.Refresh()

        Me.Cursor = Cursors.WaitCursor

        For y = 0 To UBound(LocalLogFilePaths)

            DirectoryName = System.IO.Path.GetFileName(LocalLogFilePaths(y))
            myfiles = Directory.GetFiles(LocalLogFilePaths(y))
            For i = 0 To UBound(myfiles)
                If Path.GetFileName(myfiles(i)) = "GM_ResidentClient.log" Then
                    fnum = FreeFile()
                    FileOpen(fnum, myfiles(i), OpenMode.Input)
                    Do While Not EOF(fnum)
                        textline = LineInput(fnum)

                        If Len(AndNotString) = 0 Then

                            If InStr(UCase(textline), UCase(SearchString)) > 0 And InStr(UCase(SaveTextline), UCase(SearchString)) > 0 Then
                                If MultiplesFound = False Then
                                    DisplayLogFile.ListBox3.Items.Add(DirectoryName & "," & "MultiplesFound")
                                End If
                                MultiplesFound = True
                            Else
                                If InStr(UCase(textline), UCase(SearchString)) > 0 Then
                                    If MultiplesFound = False Then
                                        DisplayLogFile.ListBox3.Items.Add(DirectoryName & "," & textline)
                                        DisplayLogFile.ListBox3.SelectedIndex = DisplayLogFile.ListBox3.Items.Count - 1
                                        DisplayLogFile.ListBox3.Refresh()
                                    End If

                                Else
                                    MultiplesFound = False
                                End If

                            End If

                        Else

                            If InStr(UCase(textline), UCase(SearchString)) > 0 And InStr(UCase(SaveTextline), UCase(SearchString)) > 0 And InStr(UCase(SaveTextline), UCase(AndNotString)) = 0 Then
                                If MultiplesFound = False Then
                                    DisplayLogFile.ListBox3.Items.Add(DirectoryName & "," & "MultiplesFound")
                                End If
                                MultiplesFound = True
                            Else
                                If InStr(UCase(textline), UCase(SearchString)) > 0 And InStr(UCase(textline), UCase(AndNotString)) = 0 Then
                                    If MultiplesFound = False Then
                                        DisplayLogFile.ListBox3.Items.Add(DirectoryName & "," & textline)
                                        DisplayLogFile.ListBox3.SelectedIndex = DisplayLogFile.ListBox3.Items.Count - 1
                                        DisplayLogFile.ListBox3.Refresh()
                                    End If

                                Else
                                    MultiplesFound = False
                                End If

                            End If

                        End If

                        SaveTextline = textline

                    Loop
                    FileClose(fnum)
                End If
            Next i
        Next y

        Me.Cursor = Cursors.Arrow

        DisableListbox3Select = False

    End Sub

    Private Sub DisplayVehicleNumbers()

        'Called when dashboard is loaded and when Display All button is pressed...
        'Populates the vehicle numbers display listbox with all vehicle numbers...

        Dim tempstr() As String

        ListBox2.Items.Clear()

        For x = 1 To InitForm.VehicleNumbersList.Count - 1
            tempstr = Split(InitForm.VehicleNumbersList(x).ToString, ",")
            ListBox2.Items.Add(tempstr(0))
        Next

        Exit Sub

        Dim y As Integer

        Dim mySavePath As String = ""

        ListBox2.Items.Clear()

        For y = 0 To 6

            Select Case y

                Case 0
                    mySavePath = mySavepathprefix & "\LogFiles\CSAV2\"
                Case 1
                    mySavePath = mySavepathprefix & "\LogFiles\LowContent\"
                Case 2
                    mySavePath = mySavepathprefix & "\LogFiles\HighContent\"
                Case 3
                    mySavePath = mySavepathprefix & "\LogFiles\ACP2\"
                Case 4
                    mySavePath = mySavepathprefix & "\LogFiles\ACP3\"
                Case 5
                    mySavePath = mySavepathprefix & "\LogFiles\ACP4\"
                Case 6
                    mySavePath = mySavepathprefix & "\LogFiles\FCM\"
            End Select

            Dim dir As New DirectoryInfo(mySavePath)
            Dim dirs As DirectoryInfo() = dir.GetDirectories()

            For x = 0 To UBound(dirs)
                ListBox2.Items.Add(dirs(x).Name)
            Next x

        Next y

        ListBox1.Refresh()
        ListBox2.Refresh()
        ListBox3.Refresh()
        ListBox4.Refresh()
        ListBox6.Refresh()
    End Sub

    Private Function Select_SignalAddList() As String

        'This is used in conjunction with the List Current Signal List Names function.  Allows the user to select
        'a properly formatted .csv file containing variable names, processor names and raster names to add to selected existing signal lists.
        'This is helpful if request is made to add a bunch of signals to several existing experiments.  We will use the modified signal lists
        'to create new experiments containing the newly requested signals...

        Dim l_Filename As String

        OpenFileDialog1.DefaultExt = ".csv"
        OpenFileDialog1.Filter = "Keep Signal List Add File (*.csv)|*.csv"

        Me.OpenFileDialog1.Title = "Please Signal List Add File .csv File"
        Me.OpenFileDialog1.FileName = ""
        If Len(SaveDirectoryName) > 0 Then
            Me.OpenFileDialog1.InitialDirectory = SaveDirectoryName
        Else
            Me.OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
        End If
        Me.OpenFileDialog1.ShowDialog()

        l_Filename = Me.OpenFileDialog1.FileName

        If Len(l_Filename) > 0 Then
            SaveDirectoryName = System.IO.Path.GetDirectoryName(l_Filename)
        End If

        Select_SignalAddList = l_Filename

    End Function

    Private Sub AddOrRemoveSignals()

        'Uses a .csv file which contains a list of variables to be added and or removed and applies this list to selected signal lists (.csv) format
        'to add and remove signals in a single or multiple signal lists as selected by the user...

        'Add Remove Signals file csv file format is...

        'VeFSRR_e_ArbSetSpdMode,, HCS, 25ms_c1
        'VeLXCR_b_DST_AutoResObjDet_F,, HCS, Remove

        Dim selectedfilename As String
        Dim fnum As Integer
        Dim signallistarray() As String = Nothing
        Dim validsignallistarray() As String = Nothing
        Dim signallistRemovearray() As String = Nothing
        Dim textline As String
        Dim i As Integer = -1
        Dim n As Integer = -1
        Dim x As Integer = -1
        Dim y As Integer = -1
        Dim z As Integer
        Dim numberofsignals As Integer
        Dim numsignalsinname As Integer
        Dim filenamesFromList() As String
        Dim FileNameParse() As String
        Dim SignalListArrayParse() As String
        Dim textlinearray() As String
        Dim newfilename As String
        Dim tempFilename As String
        Dim ProjectName As String
        Dim SignalsToRemove As Integer
        Dim SignalsToAdd As Integer
        Dim found As Boolean
        Dim savefilename As String
        Dim NewFileArray() As String = Nothing
        Dim numfound As Integer
        Dim numtoignore As Integer
        Dim NumArrayElements As Integer
        Dim FirstArrayDim As Integer = 0
        Dim SecondArrayDim As Integer = 0

        Dim j As Integer
        Dim k As Integer

        TurnOffShowNetworkConnectedPCs = True

        'This folder location may have to be changed dependng on where CLEVIR is installed...

        'Select the list we want to use to determine what to add and what to remove...
        selectedfilename = Select_SignalAddList()

        If Len(selectedfilename) > 0 Then

            'Go through the file and create an add list array and a remove list array...
            fnum = FreeFile()
            FileOpen(fnum, selectedfilename, OpenMode.Input)

            Do While Not EOF(fnum)

                NumArrayElements = 0
                FirstArrayDim = 0
                SecondArrayDim = 0

                textline = LineInput(fnum)

                textlinearray = Split(textline, ",")

                'if the variable is a Va array type variable, we will also need to determine  how many array elements are required
                'this because when requesting variables to be added, most people do not identify each individual array element in the
                'proper format, so a number in the second column of the file, indicates number of elements (0 to num elements - 1)
                'if it is a two dimensional array, it will have something like 6X8 which would indicate a 6 x 8 2 d array.  So here
                'we are calculating the number of array elements which is used later on in here...
                If Len(textlinearray(1)) > 0 Then
                    If InStr(UCase(textlinearray(1)), "X") > 0 Then
                        FirstArrayDim = CInt(Mid(textlinearray(1), 1, InStr(UCase(textlinearray(1)), "X") - 1))
                        SecondArrayDim = CInt(Mid(textlinearray(1), InStr(UCase(textlinearray(1)), "X") + 1, Len(textlinearray(1))))
                        NumArrayElements = FirstArrayDim * SecondArrayDim
                    Else
                        NumArrayElements = CInt(textlinearray(1))
                    End If

                End If

                'In the add or remove signals file we can flag for removal by adding the word remove next to the signal name...
                'we handle adding and removing differently, so we condition here...

                If InStr(UCase(textline), "REMOVE") = 0 Then

                    If NumArrayElements = 0 Then
                        x += 1
                        ReDim Preserve signallistarray(x)
                        signallistarray(x) = textline
                        SignalsToAdd += 1
                    Else

                        'Text formatting for signal as a one dimensional array...
                        If FirstArrayDim = 0 Then
                            For z = 0 To NumArrayElements - 1
                                x += 1
                                ReDim Preserve signallistarray(x)
                                signallistarray(x) = textlinearray(0) & "[x]_[" & CStr(z) & "]" & ",," & textlinearray(2) & "," & textlinearray(3)
                                SignalsToAdd += 1
                            Next
                        Else

                            'VaPLNR_Cnt_NumLnsLeft_Obj[0][x]_[0][0]
                            'VaPLNR_b_MapLnMrk_Usbl[0][x]_[0][1]
                            'VaPLNR_b_MapLnMrk_Usbl[10][x]_[0][0]

                            'Text formatting for a signal as a two dimensional array...
                            For j = 0 To FirstArrayDim - 1
                                For k = 0 To SecondArrayDim - 1
                                    x += 1
                                    ReDim Preserve signallistarray(x)
                                    signallistarray(x) = textlinearray(0) & "[" & j & "][x]_[0][" & k & "],," & textlinearray(2) & "," & textlinearray(3)
                                    SignalsToAdd += 1
                                Next
                            Next

                        End If

                    End If

                Else 'Here we are setting up to remove signals from the signal list...
                    If NumArrayElements = 0 Then
                        i += 1
                        ReDim Preserve signallistRemovearray(i)
                        signallistRemovearray(i) = textline
                        SignalsToRemove += 1
                    Else
                        If FirstArrayDim = 0 Then
                            For z = 0 To NumArrayElements - 1
                                i += 1
                                ReDim Preserve signallistRemovearray(i)
                                signallistRemovearray(i) = textlinearray(0) & "[x]_[" & CStr(z) & "]" & "," & textlinearray(1) & "," & textlinearray(2) & "," & textlinearray(3)
                                SignalsToRemove += 1
                            Next
                        Else
                            For j = 0 To FirstArrayDim - 1
                                For k = 0 To SecondArrayDim - 1
                                    i += 1
                                    ReDim Preserve signallistRemovearray(i)
                                    signallistRemovearray(i) = textlinearray(0) & "[" & j & "][x]_[0][" & k & "]," & textlinearray(1) & "," & textlinearray(2) & "," & textlinearray(3)
                                    SignalsToRemove += 1
                                Next
                            Next
                        End If

                    End If

                End If
            Loop

            FileClose(fnum)

            'We now go through the list of signal lists selected and act on the selected lists only...
            For x = 0 To ListBox8.SelectedItems.Count - 1
                y += 1
                ReDim Preserve filenamesFromList(y)
                filenamesFromList(y) = Mid(ListBox8.SelectedItems(x).ToString, 1, InStr(ListBox8.SelectedItems(x).ToString, ",") - 1)

                If File.Exists(SignalListDirectory & filenamesFromList(y)) Then

                    'Determine ProjectName from signal list file name...

                    FileNameParse = Split(filenamesFromList(y), "_")

                    'Save number of signals part of file name for use when building newfilename later...
                    numsignalsinname = CInt(FileNameParse(1))

                    'We will work with a temporary file.  We do not yet know what the name of the final file will be because we are not sure yet how many signals
                    'will actually be added and how many will be removed...

                    If InStr(filenamesFromList(y), "22XML") = 0 Then
                        ProjectName = Mid(FileNameParse(3), 1, InStr(FileNameParse(3), ".csv") - 1)
                        tempFilename = SignalListDirectory & FileNameParse(0) & "_" & CStr(numsignalsinname) & "_" & FileNameParse(2) & "_TMP_" & FileNameParse(3)
                        'Here we just create a save file name so we can get back to where we started if something goes wrong...
                        savefilename = SignalListDirectory & FileNameParse(0) & "_" & CStr(numsignalsinname) & "_" & FileNameParse(2) & "_SAVE_" & FileNameParse(3)

                    Else
                        ProjectName = Mid(FileNameParse(4), 1, InStr(FileNameParse(4), ".csv") - 1)
                        tempFilename = SignalListDirectory & FileNameParse(0) & "_" & CStr(numsignalsinname) & "_" & FileNameParse(2) & "_" & FileNameParse(3) & "_TMP_" & FileNameParse(4)
                        'Here we just create a save file name so we can get back to where we started if something goes wrong...
                        savefilename = SignalListDirectory & FileNameParse(0) & "_" & CStr(numsignalsinname) & "_" & FileNameParse(2) & "_" & FileNameParse(3) & "_SAVE_" & FileNameParse(4)

                    End If

                    If ProjectName = "US" Then
                        ProjectName = "CSAV2"
                    End If

                    'Copy the original signal list to the temp file and to a save file...
                    File.Copy(SignalListDirectory & filenamesFromList(y), savefilename, True)
                    File.Copy(SignalListDirectory & filenamesFromList(y), tempFilename, True)

                    'Add Signals in signallistarray to temp file if required...

                    If SignalsToAdd > 0 Then

                        'First, we need to see if the signal that is being added already exists in current signal list...
                        fnum = FreeFile()
                        FileOpen(fnum, tempFilename, OpenMode.Input)

                        numtoignore = 0
                        n = -1

                        Do While Not EOF(fnum)
                            textline = LineInput(fnum)
                            textlinearray = Split(textline, ",")

                            'if the variable exists in the signal list we will flag it to ignore...
                            For i = 0 To UBound(signallistarray)
                                If ProjectName = "HC" And (InStr(signallistarray(i), ",HCS,") > 0 Or InStr(signallistarray(i), ",HCF,") > 0) Then
                                    If InStr(signallistarray(i), textlinearray(0)) > 0 And InStr(signallistarray(i), textlinearray(2)) > 0 Then
                                        numtoignore += 1
                                        signallistarray(i) = "Ignore"
                                        Exit For
                                    End If
                                ElseIf InStr(signallistarray(i), textlinearray(0)) > 0 Then
                                    numtoignore += 1
                                    signallistarray(i) = "Ignore"
                                    Exit For
                                End If
                            Next

                        Loop

                        'if the variable already exists in the signal list, we will igonre it and remove any variables flagged to ignore from the final list to add...
                        For i = 0 To UBound(signallistarray)
                            If signallistarray(i) <> "Ignore" Then
                                n += 1
                                ReDim Preserve validsignallistarray(n)
                                validsignallistarray(n) = signallistarray(i)
                            End If
                        Next

                        FileClose(fnum)

                        'go through the final valid signal list array and add the variables to the end of the signal list, again, in the temp file...
                        If Not validsignallistarray Is Nothing Then

                            fnum = FreeFile()
                            FileOpen(fnum, tempFilename, OpenMode.Append)

                            For z = 0 To UBound(validsignallistarray)
                                SignalListArrayParse = Split(validsignallistarray(z), ",")

                                'This does not handle if list includes same variable name recorded from both HCS and HCF processor.  It will create redundant entries
                                'in signal list for all signal lists other than HC  - This because HC is the only signal list (except for CSAV2) that contains multiple processors...

                                'We would not use a reqest for adding CSAV2 variables to add to anything other than a different CSAV2 signal list, so it is only when someone requests
                                'that signals be added for EOCM3 HC and ACP3 for instance where this is an issue...

                                'since we may be getting a list of variables to add for a specific processor, typically we get lists based on High Content projectname, we need
                                'to change the processor name that goes into the signal list based on the projectname as determined by the signal list name.  So, for example,
                                'the same variable name may be applicable to HC type vehicles and have a processor name of HCF, but when we add to an ACP3 signal list, we must
                                'change the processor name accordingly to ACP3_MCU...
                                Select Case ProjectName
                                    Case "HC"
                                    Case "LC"
                                        SignalListArrayParse(2) = "XETK:1"
                                    Case "CSAV2"
                                    Case "FCM"
                                        MsgBox("FCM not fully implemented...")

                                        'Note ACP2 a2l file has different naming conventions for its rasters, so there will be some manual modification required if
                                        'creating ACP2 additions from a non ACP2 request list...
                                    Case "ACP2"
                                        SignalListArrayParse(2) = "ACP2_MCU"
                                    Case "ACP3"
                                        SignalListArrayParse(2) = "ACP3_MCU"
                                    Case "ACP4"
                                        SignalListArrayParse(2) = "ACP4_MCU"
                                End Select
                                PrintLine(fnum, SignalListArrayParse(0) & "," & SignalListArrayParse(1) & "," & SignalListArrayParse(2) & "," & SignalListArrayParse(3) & ",,,,,,,,,,,,,,,,,,,")
                            Next

                            FileClose(fnum)

                        End If

                    End If

                    'here we handle removing variables should there be any variables in the csv file flagged for removal...
                    'We will go through the temp file and create a temp array that will contain only those variables that are not flagged
                    'for removal.  Then we will re-create the temp file and add back in all variables minus those that were flagged for removal...
                    If SignalsToRemove > 0 Then
                        fnum = FreeFile()
                        FileOpen(fnum, tempFilename, OpenMode.Input)

                        numfound = 0
                        z = -1

                        Do While Not EOF(fnum)
                            textline = LineInput(fnum)
                            textlinearray = Split(textline, ",")

                            'We will only remove variables after the display live variables setup section of the signal list, so we check to see if there is anything
                            'in the DisplayName field of the signal list file, if there is, we will simply copy this line as is, and will not remove the variable.
                            'For variables in this section that need to be removed, we will still do this manually so we can adjust the grid row numbers for the display
                            'grids defined in this section of the signal list accordingly...

                            If Len(textlinearray(1)) > 0 Then
                                z += 1
                                ReDim Preserve NewFileArray(z)
                                NewFileArray(z) = textline
                            Else
                                For i = 0 To UBound(signallistRemovearray)
                                    If ProjectName = "HC" And (InStr(signallistRemovearray(i), ",HCS,") > 0 Or InStr(signallistRemovearray(i), ",HCF,") > 0) Then
                                        If InStr(signallistRemovearray(i), textlinearray(0)) > 0 And InStr(signallistRemovearray(i), textlinearray(2)) > 0 Then
                                            found = True
                                            numfound += 1
                                            Exit For
                                        End If
                                    ElseIf InStr(signallistRemovearray(i), textlinearray(0)) > 0 Then
                                        found = True
                                        numfound += 1
                                        Exit For
                                    End If
                                Next i
                                If found = False Then
                                    z += 1
                                    ReDim Preserve NewFileArray(z)
                                    NewFileArray(z) = textline
                                Else
                                    found = False
                                End If

                            End If
                        Loop

                        FileClose(fnum)

                        'here we are creating the new temp file with the variables flagged for removal, removed...
                        fnum = FreeFile()
                        FileOpen(fnum, tempFilename, OpenMode.Output)

                        For z = 0 To UBound(NewFileArray)
                            PrintLine(fnum, NewFileArray(z))
                        Next

                        FileClose(fnum)

                    End If

                    'numberofsignals is the net difference in total number of signals based on number of signals added and number of signals removed so we can adjust the signal list filename accordingly...
                    numberofsignals = (SignalsToAdd - numtoignore) - numfound

                    If InStr(filenamesFromList(y), "22XML") = 0 Then
                        newfilename = SignalListDirectory & FileNameParse(0) & "_" & CStr(numsignalsinname + numberofsignals) & "_" & FileNameParse(2) & "_" & FileNameParse(3)
                    Else
                        newfilename = SignalListDirectory & FileNameParse(0) & "_" & CStr(numsignalsinname + numberofsignals) & "_" & FileNameParse(2) & "_" & FileNameParse(3) & "_" & FileNameParse(4)
                    End If

                    'Copy the temp file that we have been working with to a new filename based on the new total number of signals defined in the file...
                    File.Copy(tempFilename, newfilename, True)

                    File.Delete(tempFilename)

                Else
                    MsgBox("File " & SignalListDirectory & filenamesFromList(y) & " does not exist.")
                End If

            Next 'Loop through all selected signal lists...

            ListBox8.SelectionMode = SelectionMode.One
            ListBox8.SelectedIndex = -1

        End If
    End Sub

    Private Sub ListCurrentSignalListNames()

        'Displays a list of all of the current signal lists which are on the share drive.  We then will use this list to select the signal lists that
        'we want to add signals to or remove signals from.  So, for example, if someone asks for a list of variables to be added for EOCM3 software version 162
        'model years 22 and 23, and also wants to add to all ACP3 controllers, we can select all applicable signal lists and make the additions to all of these
        'lists at the same time...

        Dim sourceDirName As String = "\\Nam.corp.gm.com\tcws-dfs\Project\CSV\CSAV2" & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\Current"

        Dim dir As DirectoryInfo = New DirectoryInfo(sourceDirName)
        Dim dirs As DirectoryInfo() = dir.GetDirectories()
        Dim files As FileInfo()

        If Not dir.Exists Then
            Throw New DirectoryNotFoundException(
                "Source directory does not exist or could not be found: " + sourceDirName)
            Exit Sub
        End If

        TurnOffShowNetworkConnectedPCs = True

        GroupBox5.Text = "Signal List Name Display"

        ListBox8.Items.Clear()

        GroupBox5.Visible = True
        GroupBox5.BringToFront()
        ListBox8.Visible = True
        GroupBox5.Refresh()


        For Each subdir In dirs

            Dim subdirs As DirectoryInfo() = subdir.GetDirectories

            Select Case subdir.Name
                Case "CSAV2", "ACP2", "ACP3", "ACP4", "FCM", "HighContent", "LowContent"
                    ListBox8.Items.Add(subdir.Name)
            End Select

            For Each subsubdir In subdirs

                Select Case subdir.Name
                    Case "CSAV2", "ACP2", "ACP3", "ACP4", "FCM", "HighContent", "LowContent"

                        ListBox8.Items.Add(subsubdir.Name)

                        files = subsubdir.GetFiles()

                        For Each file In files
                            If InStr(file.Name, ".csv") > 0 Then
                                ListBox8.Items.Add(file.Name & "," & file.LastWriteTime)

                                If Not System.IO.File.Exists(SignalListDirectory & file.Name) Then
                                    System.IO.File.Copy(file.FullName, SignalListDirectory & file.Name)
                                End If

                                ListBox8.SelectedIndex = ListBox8.Items.Count - 1
                                ListBox8.Refresh()
                            End If
                        Next

                        If subdir.Name = "HighContent" Then

                            Dim subsubdirs As DirectoryInfo() = subsubdir.GetDirectories

                            For Each Subsubsubdir In subsubdirs

                                ListBox8.Items.Add(Subsubsubdir.Name)

                                files = Subsubsubdir.GetFiles()
                                For Each file In files
                                    If InStr(file.Name, ".csv") > 0 Then
                                        ListBox8.Items.Add(file.Name & "," & file.LastWriteTime)

                                        If Not System.IO.File.Exists(SignalListDirectory & file.Name) Then
                                            System.IO.File.Copy(file.FullName, SignalListDirectory & file.Name)
                                        End If

                                        ListBox8.SelectedIndex = ListBox8.Items.Count - 1
                                        ListBox8.Refresh()
                                    End If
                                Next
                            Next

                        End If

                End Select

            Next

        Next

        If ListBox8.Items.Count > 0 Then

            ListBox8.SelectedIndex = -1
            ListBox8.SelectionMode = SelectionMode.MultiExtended
            AddSignalsToSelectedExperimentsFromFileToolStripMenuItem.Enabled = True

        End If

    End Sub

    Private Sub MergeKeepLists()

        'This routine is accessed from the utilities menu

        'The user selects a "baseline keep list" which can then be added to.
        'The user then selects a second keep list which will be merged with the first, eliminating any redundancies.
        'If more keep lists are to be merged, the user would then select the previous keep list that was created from the baseline
        'and use that one as a new baseline to be used when merging another file, etc.  So, this function would be called multiple
        'times depending on how many files were to be merged...

        Dim l_Filename As String = Nothing
        Dim fnum As Integer
        Dim fnum2 As Integer
        Dim textline As String
        Dim rasternamearray() As String = Nothing
        Dim x As Integer
        Dim BaselineKeepListFileName As String
        Dim BaselineTextArray As ArrayList = Nothing

        TurnOffShowNetworkConnectedPCs = True

        OpenFileDialog1.DefaultExt = ".csv"
        OpenFileDialog1.Filter = "Baseline Keep List file (*.csv)|*.csv"

        Me.OpenFileDialog1.Title = "Please Select Keep List CSV File to use as Baseline"
        Me.OpenFileDialog1.FileName = ""
        If Len(SaveDirectoryName) > 0 Then
            Me.OpenFileDialog1.InitialDirectory = SaveDirectoryName
        Else
            Me.OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath

        End If
        Me.OpenFileDialog1.ShowDialog()

        If Len(Me.OpenFileDialog1.FileName) > 0 Then

            l_Filename = Me.OpenFileDialog1.FileName
            SaveDirectoryName = System.IO.Path.GetDirectoryName(l_Filename)

            BaselineKeepListFileName = Mid(l_Filename, 1, InStr(l_Filename, ".csv") - 1) & "_Baseline_KeepList.csv"
            File.Copy(l_Filename, BaselineKeepListFileName, True)

            OpenFileDialog1.DefaultExt = ".csv"
            OpenFileDialog1.Filter = "Keep List file to Merge (*.csv)|*.csv"

            Me.OpenFileDialog1.Title = "Please Select Keep List CSV File to Merge with Baseline"
            Me.OpenFileDialog1.FileName = ""

            If Len(SaveDirectoryName) > 0 Then
                Me.OpenFileDialog1.InitialDirectory = SaveDirectoryName
            Else
                Me.OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
            End If
            Me.OpenFileDialog1.ShowDialog()

            If Len(Me.OpenFileDialog1.FileName) > 0 Then

                l_Filename = Me.OpenFileDialog1.FileName
                SaveDirectoryName = System.IO.Path.GetDirectoryName(l_Filename)

                fnum2 = FreeFile()
                FileOpen(fnum2, BaselineKeepListFileName, OpenMode.Input)
                textline = LineInput(fnum2)

                BaselineTextArray = New ArrayList

                Do While Not EOF(fnum2)
                    BaselineTextArray.Add(LineInput(fnum2))
                Loop
                FileClose(fnum2)

                fnum = FreeFile()
                FileOpen(fnum, l_Filename, OpenMode.Input)

                Do While Not EOF(fnum)
                    textline = LineInput(fnum)
                    If Not BaselineTextArray.Contains(textline) Then
                        BaselineTextArray.Add(textline)
                        ListBox8.Items.Add(textline & " added to baseline file")
                        ListBox8.SelectedIndex = ListBox8.Items.Count - 1
                        ListBox8.Refresh()
                    End If
                Loop

                FileClose(fnum)

                fnum2 = FreeFile()
                FileOpen(fnum2, BaselineKeepListFileName, OpenMode.Output)



                PrintLine(fnum2, "VariableName,DeviceName")

                BaselineTextArray.Sort()

                For x = 0 To BaselineTextArray.Count - 1
                    PrintLine(fnum2, BaselineTextArray(x))
                Next

                FileClose(fnum2)

            Else
                MsgBox("Invalid Filename selected, Exiting...")
                Exit Sub
            End If

        Else
            MsgBox("Invalid Filename selected, Exiting...")
        End If


    End Sub

    Private Sub CreateKeepListFromINCAGeneratedFile()

        'This functionality is accessed from the utilities menu...

        'Here we will use a .csv file that has been created by SaveAs (CSV) in Excel.  The assumption here is that the user has saved the entire variable list as a
        'Excel spreadsheet.  This spreadsheet will contain all variable names and indicate the raster that is being used to record the variable, or if no raster
        'is identified, this variable is not being recorded.  The user would then save this file as a .csv file.  This routine then takes the resulting .csv file
        'and parses it to create a keep list in the proper format for the a2l muncher to be able to use.

        'Two files are created, a keep list format file, and a file in the CLEVIR signal list format...


        Dim l_Filename As String = Nothing
        Dim fnum As Integer
        Dim fnum2 As Integer
        Dim fnum3 As Integer
        Dim textline As String
        Dim splitline() As String
        Dim rasternamearray() As String = Nothing
        Dim x As Integer
        Dim KeepListFileName As String
        Dim CLEVIRListFileName As String
        Dim found As Boolean

        OpenFileDialog1.DefaultExt = ".csv"
        OpenFileDialog1.Filter = "CSV File from INCA .xls variable file (*.csv)|*.csv"

        Me.OpenFileDialog1.Title = "Please Select CSV File from INCA .xls variable file"
        Me.OpenFileDialog1.FileName = ""
        If Len(SaveDirectoryName) > 0 Then
            Me.OpenFileDialog1.InitialDirectory = SaveDirectoryName
        Else
            Me.OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
        End If
        Me.OpenFileDialog1.ShowDialog()

        If Len(Me.OpenFileDialog1.FileName) > 0 Then

            l_Filename = Me.OpenFileDialog1.FileName

            KeepListFileName = Mid(l_Filename, 1, InStr(l_Filename, ".csv") - 1) & "_KeepList.csv"
            CLEVIRListFileName = Mid(l_Filename, 1, InStr(l_Filename, ".csv") - 1) & "_SignalListFormat.csv"

            fnum = FreeFile()
            FileOpen(fnum, l_Filename, OpenMode.Input)

            fnum2 = FreeFile()
            FileOpen(fnum2, KeepListFileName, OpenMode.Output)
            PrintLine(fnum2, "VariableName,DeviceName")

            fnum3 = FreeFile()
            FileOpen(fnum3, CLEVIRListFileName, OpenMode.Output)
            PrintLine(fnum3, "VariableName,DisplayName,DeviceName,Raster")

            textline = LineInput(fnum)
            splitline = Split(textline, ",")

            For x = 3 To UBound(splitline)
                ReDim Preserve rasternamearray(x - 3)
                rasternamearray(x - 3) = splitline(x)
            Next

            Do While Not EOF(fnum)
                textline = LineInput(fnum)
                splitline = Split(textline, ",")

                For x = 0 To UBound(splitline)
                    If splitline(x) = "X" Then
                        found = True
                        Exit For
                    End If
                Next
                If found = True Then
                    found = False
                    PrintLine(fnum2, splitline(0) & "," & splitline(1))
                    PrintLine(fnum3, splitline(0) & ",," & splitline(1) & "," & rasternamearray(x - 3))
                    ListBox8.Items.Add(textline & " - " & rasternamearray(x - 3))
                Else
                    ListBox8.Items.Add(textline)
                End If
                ListBox8.SelectedIndex = ListBox8.Items.Count - 1
                ListBox8.Refresh()
            Loop

        Else

        End If

        FileClose(fnum)
        FileClose(fnum2)
        FileClose(fnum3)

        SaveDirectoryName = System.IO.Path.GetDirectoryName(l_Filename)

    End Sub

    Private Sub CopyNewCLEVIRConfigFilesToQ()

        MsgBox("This functionality not implemented...")

        '\\Nam.corp.gm.com\tcws-dfs\Project\CSV\CSAV2" & CLEVIRBaseDir & "\Updated CLEVIR Files For Vehicles\Signal Files And Experiments\Current" + based on project name

        'My.Application.Info.DirectoryPath & "\" & "Experiments\158_20417_MY22_HC.exp"
        'My.Application.Info.DirectoryPath & "\" & "SignalLists\158_20417_MY22_HC.csv"
        'My.Application.Info.DirectoryPath & "\" & "SignalLists\158_20417_MY22_HC.xlsx"

        '\\Nam.corp.gm.com\tcws-dfs\Project\CSV\CSAV2" & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\UpdatedFiles + based on project name

        'My.Application.Info.DirectoryPath & "\" & "157_MY23_Enumerations_LC.txt"
        'My.Application.Info.DirectoryPath & "\" & "LC_ARXML_Mapping.csv"
        'My.Application.Info.DirectoryPath & "\" & "157_MY23_LC_1P.exp"



    End Sub

    Private Sub CheckForNewVehicleConfigurations()

        'called from myBackground tasks periodically. Checks to see if any new vehicle configurations have been added
        'and displays a message if any new vehicle configuration files are found on the share drive...

        Dim DefaultPath As String = "\\Nam.corp.gm.com\tcws-dfs\project\CSV\CSAV2\"
        Dim dir As New DirectoryInfo(DefaultPath & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations")
        Dim dirs As DirectoryInfo() = dir.GetDirectories()
        Dim x As Integer
        Dim fnum As Integer
        Dim filename As String = My.Application.Info.DirectoryPath & "\SavedVehicleNumberList.txt"
        Dim SavedVehicleNumberArrayList As ArrayList = Nothing
        Dim VehicleNumberArrayList As ArrayList = Nothing

        Try

            HandleUserMessageLogging("GMRC", "Checking for new vehicle configurations...",,, FlashMsgOn)

            Me.Cursor = Cursors.WaitCursor

            If File.Exists(filename) Then

                SavedVehicleNumberArrayList = New ArrayList
                fnum = FreeFile()
                FileOpen(fnum, filename, OpenMode.Input)

                Do While Not EOF(fnum)
                    SavedVehicleNumberArrayList.Add(LineInput(fnum))
                Loop
                FileClose(fnum)

            End If

            VehicleNumberArrayList = New ArrayList

            For x = 0 To UBound(dirs)
                VehicleNumberArrayList.Add(dirs(x).Name)
                If Not SavedVehicleNumberArrayList Is Nothing Then
                    If Not SavedVehicleNumberArrayList.Contains(dirs(x).Name) Then
                        HandleUserMessageLogging("GMRC", "NEW CLEVIR CONFIGURATION ADDED FOR VEHICLE " & dirs(x).Name, DisplayMsgBox)
                    End If
                End If
            Next x

            fnum = FreeFile()
            FileOpen(fnum, filename, OpenMode.Output)

            For x = 0 To VehicleNumberArrayList.Count - 1
                PrintLine(fnum, VehicleNumberArrayList(x).ToString)
            Next

            FileClose(fnum)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckForNewVehicleConfigurations: " & ex.Message)
        Finally

            Me.Cursor = Cursors.Arrow
            UserStatusInfo.Label1.Text = ""
            UserStatusInfo.Hide()

        End Try

    End Sub

    Private Sub CheckForCalibrationFolderChanges()

        'This routine runs once at startup of the VehicleStatDashboard and then every 30 minutes.  Checks all of the various Calibration folders
        'to see if any folders have been added, or contents within the folders has changed.  Displays a message with modified folder name and modified
        'date and time.  Also alerts if there is a new folder created for a new software version / model year.  We need this information
        'because we need to create new CLEVIR support files, Experiment and Signal List whenever new software version and model year combination is starting
        'to be used for any program.   This was done because we cannot rely on the lead calibrators to communicate when they have made new a2l and ptp files availble to the
        'calibration user community...

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer
        Dim InitialDirectory As String = ""
        Dim DefaultPath As String = "\\Nam.corp.gm.com\tcws-dfs\project\CSV\CSAV2\"
        Dim InfoArrayList As ArrayList = Nothing
        Dim SavedInfoArrayList As ArrayList = Nothing
        Dim SavedInfoArrayListFileName As String = My.Application.Info.DirectoryPath & "\SavedInfoArrayList.csv"

        Dim SoftwareVersionArrayList As ArrayList = Nothing
        Dim SavedSoftwareVersionArrayList As ArrayList = Nothing
        Dim SavedSoftwareVersionArrayListFileName As String = My.Application.Info.DirectoryPath & "\SavedSoftwareVersionArrayList.csv"

        Dim NumSubDirs As Integer
        'Dim UpdatedDirectory() As String = ""
        Dim fnum As Integer

        Dim ModelYear As String = ""
        Dim ControllerName As String = ""

        Dim dir As DirectoryInfo

        'These are the various folder structures for each program...

        'ACP2\ Calibration \ INCA_Projects \ MYxx \ Vehicle \ SoftwareVersion - files
        'ACP3\ Calibration \ INCA_Projects \ MYxx \ Vehicle \ SoftwareVersion - files
        'ACP4\ Calibration \ INCA_Projects \ MYxx \ Vehicle \ SoftwareVersion - files
        'EOCM3_HC\ Calibration \ INCA_Projects \ MYxx \ Vehicle \ SoftwareVersion - files
        'EOCM3_lo\ Calibration \ INCA_Projects \ MYxx \ Vehicle \ SoftwareVersion - files
        'FCM\ Calibration \ INCA_Projects \ MYxx \ Vehicle \ SoftwareVersion - files
        'FCM_LC\ Calibration \ INCA Projects \ MYxx \ Vehicle \ SoftwareVersion - files

        'CSAV2...
        'Calibration\ INCA Projects\MYxx\Vehicle\SoftwareVersion - files

        Dim maxVerNum(0 To 7) As Integer
        Dim VersionNumberInFolderName As Integer
        Dim tempstrArray() As String

        Try

            HandleUserMessageLogging("GMRC", "Checking Calibration Folders for Updates...",,, FlashMsgOn)

            Me.Cursor = Cursors.WaitCursor

            If File.Exists(SavedInfoArrayListFileName) Then
                fnum = FreeFile()
                FileOpen(fnum, SavedInfoArrayListFileName, OpenMode.Input)
                SavedInfoArrayList = New ArrayList
                Do While Not EOF(fnum)
                    SavedInfoArrayList.Add(LineInput(fnum))
                Loop
                FileClose(fnum)
            End If

            If File.Exists(SavedSoftwareVersionArrayListFileName) Then
                fnum = FreeFile()
                FileOpen(fnum, SavedSoftwareVersionArrayListFileName, OpenMode.Input)
                SavedSoftwareVersionArrayList = New ArrayList
                Do While Not EOF(fnum)
                    SavedSoftwareVersionArrayList.Add(LineInput(fnum))
                Loop
                FileClose(fnum)
            End If

            InfoArrayList = New ArrayList
            SoftwareVersionArrayList = New ArrayList

            For x = 0 To 7
                Select Case x
                    Case 0
                        InitialDirectory = DefaultPath & "ACP2\Calibration\INCA_Projects"
                        NumSubDirs = 3
                        ControllerName = "ACP2"
                    Case 1
                        InitialDirectory = DefaultPath & "ACP3\Calibration\INCA_Projects"
                        NumSubDirs = 3
                        ControllerName = "ACP3"
                    Case 2
                        InitialDirectory = DefaultPath & "ACP4\Calibration\INCA_Projects"
                        NumSubDirs = 3
                        ControllerName = "ACP4"
                    Case 3
                        InitialDirectory = DefaultPath & "EOCM3_HC\Calibration\INCA_Projects"
                        NumSubDirs = 3
                        ControllerName = "EOCM3_HC"
                    Case 4
                        InitialDirectory = DefaultPath & "EOCM3_lo\Calibration\INCA_Projects"
                        NumSubDirs = 3
                        ControllerName = "EOCM3_lo"
                    Case 5
                        InitialDirectory = DefaultPath & "FCM\Calibration\INCA_Projects"
                        NumSubDirs = 3
                        ControllerName = "FCM"
                    Case 6
                        InitialDirectory = DefaultPath & "FCM_LC\Calibration\INCA Projects"
                        NumSubDirs = 3
                        ControllerName = "FCM_LC"
                    Case 7
                        InitialDirectory = DefaultPath & "Calibration\INCA Projects"
                        NumSubDirs = 3
                        ControllerName = "CSAV2"
                End Select

                dir = New DirectoryInfo(InitialDirectory)

                If System.IO.Directory.Exists(InitialDirectory) Then

                    Dim dirs As DirectoryInfo() = dir.GetDirectories()
                    For y = 0 To UBound(dirs)

                        maxVerNum(x) = 0

                        If InStr(dirs(y).Name, "MY") > 0 Then
                            ModelYear = dirs(y).Name
                        End If

                        Dim subdirs As DirectoryInfo() = dirs(y).GetDirectories()
                        For z = 0 To UBound(subdirs)

                            'If NumSubDirs = 3 Then
                            Dim subsubdirs As DirectoryInfo() = subdirs(z).GetDirectories()
                            For i = 0 To UBound(subsubdirs)
                                If InStr(subsubdirs(i).LastWriteTime, "2020") > 0 Or InStr(subsubdirs(i).LastWriteTime, "2021") > 0 Or InStr(subsubdirs(i).LastWriteTime, "2022") > 0 Then
                                    InfoArrayList.Add(subsubdirs(i).LastWriteTime & "," & subsubdirs(i).FullName)

                                    If InStr(subsubdirs(i).Name, ".") > 0 Then
                                        tempstrArray = Split(subsubdirs(i).Name, ".")
                                        If UBound(tempstrArray) >= 2 Then
                                            If IsNumeric(tempstrArray(0)) And IsNumeric(tempstrArray(1)) And IsNumeric(tempstrArray(2)) Then
                                                VersionNumberInFolderName = CInt(tempstrArray(2))
                                                If VersionNumberInFolderName > maxVerNum(x) Then
                                                    maxVerNum(x) = VersionNumberInFolderName
                                                End If
                                            End If
                                        End If

                                    End If

                                End If
                            Next i
                            'End If
                        Next z 'Vehicles

                        SoftwareVersionArrayList.Add(ControllerName & "," & ModelYear & "," & maxVerNum(x))

                    Next y 'Model Years

                Else
                    MsgBox("Directory " & InitialDirectory & " does not exist.")
                End If

            Next x 'Controllers

            InfoArrayList.Sort()
            InfoArrayList.Reverse()

            SoftwareVersionArrayList.Sort()

            If Not SavedInfoArrayList Is Nothing Then
                For y = 0 To InfoArrayList.Count - 1
                    If Not SavedInfoArrayList.Contains(InfoArrayList(y).ToString) Then
                        HandleUserMessageLogging("GMRC", InfoArrayList(y).ToString,,, FlashMsg3Sec)
                        'MsgBox(InfoArrayList(y).ToString)
                    End If
                Next
            End If

            If Not SavedSoftwareVersionArrayList Is Nothing Then
                For y = 0 To SoftwareVersionArrayList.Count - 1
                    If Not SavedSoftwareVersionArrayList.Contains(SoftwareVersionArrayList(y).ToString) Then
                        HandleUserMessageLogging("GMRC", "NEW CLEVIR SUPPORT FILES MAY BE NEEDED FOR " & SoftwareVersionArrayList(y).ToString, DisplayMsgBox)
                    End If
                Next
            End If

            fnum = FreeFile()
            FileOpen(fnum, SavedInfoArrayListFileName, OpenMode.Output)
            For x = 0 To InfoArrayList.Count - 1
                PrintLine(fnum, InfoArrayList(x).ToString)
            Next
            FileClose(fnum)

            fnum = FreeFile()
            FileOpen(fnum, SavedSoftwareVersionArrayListFileName, OpenMode.Output)
            For x = 0 To SoftwareVersionArrayList.Count - 1
                PrintLine(fnum, SoftwareVersionArrayList(x).ToString)
            Next
            FileClose(fnum)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckForCalibrationFolderChanges: " & ex.Message)

        Finally

            Me.Cursor = Cursors.Arrow
            UserStatusInfo.Label1.Text = ""
            UserStatusInfo.Hide()

        End Try

    End Sub

    Private Function GetUserNameFromIdentityString(ByVal IDString As String) As String

        'uses a file called Usernames.txt to map EDSNET ID to actual user name.  So, when we read the
        'vehicle status info, we will translate any edsnet ID to a persons name if this info exists in
        'the lookup file, this is done only for display purposes and does not affect the information in
        'the vehiclestatusinfo.txt file itself...  This makes it easier to quickly identify the user
        'in case they may need some assistance "live"...

        Dim fnum As Integer
        Dim tmpstr As String = ""
        Dim textline As String = ""
        Dim UsersName As String = ""
        Dim SaveStringFragment As String = ""

        GetUserNameFromIdentityString = IDString
        'Pass back IDString if IDString contains VEHTESTFID, otherwise parse string and look up users name in file (usernames.txt)...
        If InStr(IDString, "VEHTESTFID") = 0 Then

            'Parse IDString, could be an edsnet ID only, or edsnet ID will be part of string...
            If Len(IDString) <> 6 Then
                '08/23/2021 07:59:14 - 6MK74530 USMPGTNCSV0062 Initializing CLEVIR_INCA_7_2 (Version 5.5.7) for Development. User VEHTESTFIDCSV002
                '09/01/2021 09:28:59 - AS22V004 USMPGWNCAT37V3P Initializing CLEVIR_INCA_7_2 (Version 5.5.6) for Development. User NZMKT0
                '11/16/2021 13:45:28 -  ZZ7CKZ:

                If InStr(IDString, " User ") > 0 Then
                    If UCase(Mid(IDString, InStr(IDString, " User ") + 7, 1)) = "Z" Then
                        tmpstr = UCase(Mid(IDString, InStr(IDString, " User ") + 6, 6))
                        SaveStringFragment = UCase(Mid(IDString, 1, InStr(IDString, " User ") + 5))
                    End If
                Else
                    If UCase(Mid(IDString, InStr(IDString, " -  ") + 5, 1)) = "Z" Then
                        tmpstr = UCase(Mid(IDString, InStr(IDString, " -  ") + 4, 6))
                        SaveStringFragment = UCase(Mid(IDString, 1, InStr(IDString, " -  ") + 3))
                    End If
                End If
            Else
                tmpstr = IDString
            End If

            If Len(tmpstr) > 0 Then
                fnum = FreeFile()
                FileOpen(fnum, My.Application.Info.DirectoryPath & "\usernames.txt", OpenMode.Input)

                Do While Not EOF(fnum)
                    textline = LineInput(fnum)
                    If UCase(Mid(textline, 1, InStr(textline, Chr(9)) - 1)) = UCase(tmpstr) Then
                        UsersName = VB.Right(textline, Len(textline) - InStr(textline, Chr(9)))
                        GetUserNameFromIdentityString = SaveStringFragment & UsersName
                        Exit Do
                    End If
                Loop
                FileClose(fnum)

            End If

        End If

    End Function

    Private Sub ReadVehicleStatusInfo(Optional ByVal inputstr As String = "")

        'Called out of myBackgroundTasks loop for VehicleStatDashboard.  If any new vehicle status information is added to the VehicleStatusInfo.txt file on the share drive
        'the new information is added to the listbox on the VehicleStatDashboard main page (Listbox7). This provides us with immediate updates from any vehicle running
        'CLEVIR that is connected to the network so we can track abnormal system behavior and see if anyone may need assistance.  We at least will know the vehicle number
        'so we may be able to provide assistance to the individual in that vehicle by referencing the SPUF.  

        Dim fnum As Integer
        Dim filename As String
        Static SaveVehicleStatusArray As ArrayList = New ArrayList
        Static LastWriteTime As Date
        Dim textline As String
        Dim synth As New SpeechSynthesizer
        synth.SelectVoice("Microsoft Zira Desktop")
        synth.Rate = 0

        Dim EventDateTime As String

        If Len(inputstr) > 0 Then
            SaveVehicleStatusArray = New ArrayList
            ListBox7.Items.Clear()
            ListBox9.Items.Clear()
        End If

        Try

            If CopyingLogFiles = True Then
                Exit Sub
            End If

            'First time through we will read the existing vehicle status info file (if it exists) and copy contents line by line
            'into the savevehiclestatusarray...

            If File.Exists(My.Application.Info.DirectoryPath & "\VehicleStatusInfo.txt") And SaveVehicleStatusArray.Count = 0 Then
                fnum = FreeFile()
                FileOpen(fnum, My.Application.Info.DirectoryPath & "\VehicleStatusInfo.txt", OpenMode.Input)

                'we loop through the whole file, and if the status array doesn not already contain the text, we will add to the array...
                Do While Not EOF(fnum)

                    textline = LineInput(fnum)

                    textline = GetUserNameFromIdentityString(textline)

                    If Not SaveVehicleStatusArray.Contains(textline) Then
                        SaveVehicleStatusArray.Add(textline)

                        If IsDate(Mid(textline, 1, 19)) Then

                            EventDateTime = Mid(textline, 1, 19)

                            'here we check the date and time of the event so that we can display only those lines that are within the
                            'num days back as indicated in Textbox3

                            If Convert.ToDateTime(EventDateTime) > DateTime.Now.AddDays(-CInt(TextBox3.Text)) Then

                                'here we are checking combobox1 to see which vehiclenumber or all vehicles to display in listbox7...
                                If ComboBox1.Text = " All" Then
                                    ListBox7.Items.Add(textline)
                                Else
                                    If InStr(textline, " - " & ComboBox1.Text) > 0 Then
                                        ListBox7.Items.Add(textline)
                                    End If
                                End If

                                ListBox7.SelectedIndex = ListBox7.Items.Count - 1
                                ListBox7.Refresh()

                                'here we are populating listbox9 to see which vehicles (PCs) are running the INCA 7.3 CLEVIR version...
                                If InStr(UCase(textline), "INITIALIZING CLEVIR_INCA_7_5") > 0 Then
                                    ListBox9.Items.Add(textline)
                                    ListBox9.Refresh()
                                    Me.Refresh()
                                End If

                            End If

                        End If

                    End If
                Loop
                FileClose(fnum)
            End If

            'here we are reading the file on the share drive any time its contents have been updated.  We will then add only the
            'new contents to listbox7, so we are constantly updating only new info in the list box when it becomes available...

            If System.IO.Directory.Exists(mypathprefix & CLEVIRBaseDir & "\Development\VehicleStatusUpdates") Then
                filename = mypathprefix & CLEVIRBaseDir & "\Development\VehicleStatusUpdates\VehicleStatusInfo.txt"
                If File.Exists(filename) Then
                    If System.IO.File.GetLastWriteTime(filename) > LastWriteTime Then
                        LastWriteTime = System.IO.File.GetLastWriteTime(filename)
                        If Not FileInUse(filename) Then
                            System.IO.File.Copy(filename, My.Application.Info.DirectoryPath & "\VehicleStatusInfo.txt", True)
                            fnum = FreeFile()
                            FileOpen(fnum, My.Application.Info.DirectoryPath & "\VehicleStatusInfo.txt", OpenMode.Input)
                            Do While Not EOF(fnum)

                                textline = LineInput(fnum)

                                textline = GetUserNameFromIdentityString(textline)

                                If Not SaveVehicleStatusArray.Contains(textline) Then

                                    If ComboBox1.Text = " All" Then
                                        SaveVehicleStatusArray.Add(textline)
                                        ListBox7.Items.Add(textline)
                                    Else
                                        If InStr(textline, " - " & ComboBox1.Text) > 0 Then
                                            SaveVehicleStatusArray.Add(textline)
                                            ListBox7.Items.Add(textline)
                                        End If
                                    End If

                                    ListBox7.SelectedIndex = ListBox7.Items.Count - 1
                                    ListBox7.Refresh()

                                    If InStr(textline, "Initializing CLEVIR_INCA_7_5") > 0 Then
                                        ListBox9.Items.Add(textline)
                                    End If

                                    'if the enable voice checkbox is checked we will read out loud each new line as it is added (minus the date and time part)...
                                    If CheckBox2.Checked = True Then
                                        'synth.SelectVoice("Microsoft Zira Desktop")
                                        'synth.Rate = 0
                                        synth.Speak(Mid(textline, InStr(textline, " - ") + 3, Len(textline)))
                                    End If

                                End If

                                System.Windows.Forms.Application.DoEvents()

                            Loop
                            FileClose(fnum)
                        Else
                            HandleUserMessageLogging("GMRC", "ReadVehicleStatusInfo: File in use...")
                        End If
                    End If

                End If
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ReadVehicleStatusInfo: " & ex.Message)
        End Try

    End Sub

    Private Sub MyBackgroundTasks()

        'Activated when the VehicleStatDashboard is displayed, calls ReadVehicleStatusInfo each loop and calls CheckForCalibrationFolderChanges every hour...

        Dim myStopWatch As Stopwatch = Nothing
        Dim myElapsedTime As TimeSpan

        Dim ShowConnectedPCsStopWatch As Stopwatch = Nothing
        'Dim myShowConnectedPCsElapsedTime As TimeSpan

        Dim TimeIntervalInMinutes As Integer = 60
        Dim TimeIntervalInSeconds As Integer = 900

        Do While EnableMyBackgroundTasks = True

            ReadVehicleStatusInfo()

            'If ShowConnectedPCsStopWatch Is Nothing Then

            'ShowConnectedPCsStopWatch = New Stopwatch()
            'ShowConnectedPCsStopWatch.Reset()
            'ShowConnectedPCsStopWatch.Start()
            'Else
            'myShowConnectedPCsElapsedTime = ShowConnectedPCsStopWatch.Elapsed
            'If myShowConnectedPCsElapsedTime.TotalSeconds > TimeIntervalInSeconds Then
            'ShowNetworkConnectedPCs()

            'ShowConnectedPCsStopWatch.Reset()
            'ShowConnectedPCsStopWatch.Start()
            'End If
            'End If

            If myStopWatch Is Nothing Then
                'CheckForCalibrationFolderChanges()
                'CheckForNewVehicleConfigurations()

                'Me.Cursor = Cursors.WaitCursor
                'CopyFilesFromShareToLocal("Q:\CSAV2 Tools\CLEVIR", "W:\CSAV2 Tools\CLEVIR")
                'RemoveUnusedFoldersFromLocal("Q:\CSAV2 Tools\CLEVIR", "W:\CSAV2 Tools\CLEVIR")
                'Me.Cursor = Cursors.Arrow

                myStopWatch = New Stopwatch()
                myStopWatch.Reset()
                myStopWatch.Start()
            Else
                myElapsedTime = myStopWatch.Elapsed
                If myElapsedTime.TotalMinutes > TimeIntervalInMinutes Then
                    CheckForCalibrationFolderChanges()
                    CheckForNewVehicleConfigurations()

                    Me.Cursor = Cursors.WaitCursor
                    CopyFilesFromShareToLocal("Q:\CSAV2 Tools\CLEVIR", "W:\CSAV2 Tools\CLEVIR")
                    RemoveUnusedFoldersFromLocal("Q:\CSAV2 Tools\CLEVIR", "W:\CSAV2 Tools\CLEVIR")
                    Me.Cursor = Cursors.Arrow

                    myStopWatch.Reset()
                    myStopWatch.Start()
                End If
            End If

            If Button11.Text = "Available" Then
                CheckForAssistanceRequests()
            End If

            System.Threading.Thread.Sleep(100)
            System.Windows.Forms.Application.DoEvents()
        Loop

    End Sub

    Private Sub HandleLabelMouseEvent(Optional ByVal myLabel As Label = Nothing)

        'Called whenever a label on the main form is clicked.  The labels display the number of parsed events for a given parsed event type. When the label is clicked,
        'the display in the Event List (Listbox3) is changed to display only those parsed events that are associated with the label which displays the number of events of that type...

        Dim x As Integer

        If Not myLabel Is Nothing Then

            Label3.BackColor = Color.White
            Label3.ForeColor = Color.Black
            Label4.BackColor = Color.White
            Label4.ForeColor = Color.Black
            Label6.BackColor = Color.White
            Label6.ForeColor = Color.Black
            Label8.BackColor = Color.White
            Label8.ForeColor = Color.Black
            Label10.BackColor = Color.White
            Label10.ForeColor = Color.Black
            Label12.BackColor = Color.White
            Label12.ForeColor = Color.Black
            Label14.BackColor = Color.White
            Label14.ForeColor = Color.Black
            Label16.BackColor = Color.White
            Label16.ForeColor = Color.Black
            Label18.BackColor = Color.White
            Label18.ForeColor = Color.Black
            Label28.BackColor = Color.White
            Label28.ForeColor = Color.Black

            If Len(AdditionalSearchCriterian) > 0 Then
                myLabel.BackColor = Color.Green
                myLabel.ForeColor = Color.White
                SearchString = Nothing
            Else
                myLabel.BackColor = Color.White
                myLabel.ForeColor = Color.Black
            End If

        End If

        NumVersionsSelected = 0

        If ListBox5.SelectedIndex > -1 Then

            For x = 0 To ListBox5.Items.Count - 1
                If ListBox5.GetSelected(x) = True Then
                    SearchString = Nothing
                    VersionNumber = ListBox5.Items(x).ToString
                    NumVersionsSelected += 1
                    If LogFileType = "PCBased" Then
                        LoadLists("HostnameBasedReports\")
                    Else
                        LoadLists()
                    End If
                End If
            Next

        Else
            SearchString = Nothing
            VersionNumber = ""
            If LogFileType = "PCBased" Then
                LoadLists("HostnameBasedReports\")
            Else
                LoadLists()
            End If

        End If

    End Sub

    Private Sub CreateAggregateAnnotationFile()

        'Called on CreateAggregateAnnotationFileToolStripMenuItem_Click event.  Creates a "VehicleNumber"_PP_AggregateAnnotations.csv file
        'from the existing "VehicleNumber"_AggregateAnnotations.csv file or from all individual ANNO.csv files if no "VehicleNumber"_AggregateAnnotations.csv exists.

        'PP stands for Post Processed.  Initially, annotations were only contained in individual recording session specific files. The "VehicleNumber"_AggregateAnnotations.csv
        'files were added later.  CLEVIR now aggregates all annotations from each session into a single vehicle specific annotations file and copies this file up
        'to the share drive when data is uploaded.  So, this file will contain all annotations from the point at which this functionality was implemented.  The PP annotations file
        'is a way to make sure that all annotations from all ANNO.csv files from before the implementation of the vehicle specific aggregate annotation file are saved into
        'a single file, this is the "VehicleNumber"_PP_AggregateAnnotations.csv file...

        '20200814_172640_Demo_ANNO.csv

        '6LG3AM14_AggregateAnnotations.csv

        '0, Field ID,Field Name,Value
        '1,98,CSV Version,0
        '1,99,CLIHA Version,1
        '1,1,Session Name,20200423_132438_Demo
        '1,2,Driver,Demo
        '1,4,Country,1
        '1,3,State,23
        '1,5,Start Date,04/23/2020
        '1,6,Start Time,13:24:44
        '1,7,End Date,04/23/2020
        '1,8,End Time,13:51:12
        '1,9,Notes,
        '1,10,Procedure,
        '1,12,Vehicle,6LG3AM14
        '1,13,Thumbnail,-1
        '1,14,RecordedMileageOnGrounds,0.0
        '1,15,RecordedMileageOffGrounds,24.1
        '1,16,UnRecordedMileageOnGrounds,0.0
        '1,17,UnRecordedMileageOffGrounds,0.0
        '1,18,RecordedMileageUnknownLoc,0.0
        '1,19,UnRecordedMileageUnknownLoc,0.0
        '1,20,LCCActiveMileage,15.1
        '0,Anno Type ID,Anno Type,Anno Value ID,Anno Value,Anno Enum Type,Anno Enum,Start Seq#,Start (ms),End Seq#,End (ms),Point Seq#,Point (ms),Thumbnail,WAV,MF4 Filename,Mileage,LAT POS,LON POS
        '3,1000,DRIVER FEEDBACK,3210,LCC Event - False Escalation - Can't See Lane Lines,3090,1,2,15272,2,15272,2,15272,0,2,C:\HB\Data\gmcsv6LG3AM14\20200423_132438_Demo\20200423_132438_Demo_6LG3AM14_02.mf4,32570.6,42.7438,-83.7641

        Dim myVehicleFolder As String
        Dim mydirectories() As String = Nothing
        Dim myfiles() As String
        Dim fnum As Integer
        'Dim fnum2 As Integer
        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer
        Dim j As Integer
        Dim myAnnotations() As String = Nothing
        Dim myExistingAnnotations() As String = Nothing
        Dim textline As String = ""
        Dim myFirstRow As String = ""
        Dim myVehicleNumber As String
        Dim myAnnoFileName As String
        Dim myAnnoFileFromVehicle As String
        'Dim ProcessAllVehicles As Boolean
        Dim mySubDirectories() As String
        Dim updatePPAnno As Boolean
        Dim NumberofVehicleTypes As Integer
        Dim NumPasses As Integer
        'Dim found As Boolean

        Dim myIndex As Integer

        FolderBrowserDialog1.SelectedPath = "Q:"
        FolderBrowserDialog1.Description = "Please Select a vehicle data folder"
        FolderBrowserDialog1.ShowDialog()

        If Len(FolderBrowserDialog1.SelectedPath) > 0 Then

            If InStr(FolderBrowserDialog1.SelectedPath, "gmcsv") > 0 Then
                myVehicleFolder = FolderBrowserDialog1.SelectedPath
                myVehicleNumber = Mid(myVehicleFolder, InStr(myVehicleFolder, "gmcsv") + 5, Len(myVehicleFolder))

                myAnnoFileName = myVehicleFolder & "\" & myVehicleNumber & "_PP_AggregateAnnotations.csv"
                myAnnoFileFromVehicle = myVehicleFolder & "\" & myVehicleNumber & "_AggregateAnnotations.csv"

                mydirectories = System.IO.Directory.GetDirectories(myVehicleFolder)

                If File.Exists(myAnnoFileName) Then
                    If MsgBox("Update PP Annotations file from existing ANNO files?", vbYesNo) = vbYes Then
                        updatePPAnno = True
                    End If
                End If

                If Not File.Exists(myAnnoFileName) Or updatePPAnno = True Then

                    For z = 0 To UBound(mydirectories)

                        myfiles = Directory.GetFiles(mydirectories(z), "*_ANNO.csv")

                        For x = 0 To UBound(myfiles)

                            If Not FileInUse(myfiles(x)) Then

                                fnum = FreeFile()
                                FileOpen(fnum, myfiles(x), OpenMode.Input)
                                Do While Not EOF(fnum)
                                    textline = LineInput(fnum)
                                    If InStr(textline, "0,Anno Type ID") > 0 And Len(myFirstRow) = 0 Then
                                        myFirstRow = textline
                                    End If
                                    If Mid(textline, 1, 1) = "3" Then
                                        ReDim Preserve myAnnotations(y)
                                        myAnnotations(y) = textline
                                        y += 1
                                    End If
                                Loop
                                FileClose(fnum)
                            End If
                        Next x

                    Next z

                    If Not myAnnotations Is Nothing Then
                        'myAnnoFileName = myVehicleFolder & "\" & myVehicleNumber & "_PP_AggregateAnnotations.csv"
                        fnum = FreeFile()
                        If Not File.Exists(myAnnoFileName) Then
                            FileOpen(fnum, myAnnoFileName, OpenMode.Output)
                            PrintLine(fnum, myFirstRow)
                            For x = 0 To UBound(myAnnotations)
                                PrintLine(fnum, myAnnotations(x))
                            Next
                            FileClose(fnum)
                        Else
                            FileOpen(fnum, myAnnoFileName, OpenMode.Input)
                            LineInput(fnum)
                            j = 0
                            Do While Not EOF(fnum)
                                ReDim Preserve myExistingAnnotations(j)
                                myExistingAnnotations(j) = LineInput(fnum)
                                j += 1
                            Loop
                            FileClose(fnum)
                            fnum = FreeFile()
                            FileOpen(fnum, myAnnoFileName, OpenMode.Append)

                            For x = 0 To UBound(myAnnotations)

                                myIndex = Array.IndexOf(myExistingAnnotations, myAnnotations(x))

                                If myIndex = -1 Then
                                    PrintLine(fnum, myAnnotations(x))
                                End If

                            Next

                            FileClose(fnum)

                        End If

                    End If

                Else 'myAnnoFileName already exists...

                    If File.Exists(myAnnoFileFromVehicle) Then
                        fnum = FreeFile()
                        FileOpen(fnum, myAnnoFileFromVehicle, OpenMode.Input)
                        LineInput(fnum)
                        j = 0
                        Do While Not EOF(fnum)
                            ReDim Preserve myAnnotations(j)
                            myAnnotations(j) = LineInput(fnum)
                            j += 1
                        Loop
                        FileClose(fnum)

                        fnum = FreeFile()
                        FileOpen(fnum, myAnnoFileName, OpenMode.Input)
                        LineInput(fnum)
                        j = 0
                        Do While Not EOF(fnum)
                            ReDim Preserve myExistingAnnotations(j)
                            myExistingAnnotations(j) = LineInput(fnum)
                            j += 1
                        Loop
                        FileClose(fnum)

                        fnum = FreeFile()
                        FileOpen(fnum, myAnnoFileName, OpenMode.Append)

                        For x = 0 To UBound(myAnnotations)

                            myIndex = Array.IndexOf(myExistingAnnotations, myAnnotations(x))

                            If myIndex = -1 Then
                                PrintLine(fnum, myAnnotations(x))
                            End If

                        Next

                        FileClose(fnum)
                    End If
                End If

            Else 'InStr(FolderBrowserDialog1.SelectedPath, "gmcsv") = 0 we selected a folder above the vehicle folder level...

                If FolderBrowserDialog1.SelectedPath = "Q:\" Then
                    If MsgBox("Process all vehicles?", vbYesNo) = vbYes Then
                        NumberofVehicleTypes = 3
                    Else
                        NumberofVehicleTypes = 1
                    End If
                End If

                For NumPasses = 1 To NumberofVehicleTypes

                    Select Case NumPasses

                        Case 1
                            mydirectories = System.IO.Directory.GetDirectories(FolderBrowserDialog1.SelectedPath, "gmcsv*", 0)
                        Case 2
                            mydirectories = System.IO.Directory.GetDirectories("Q:\HighContent\VehicleData", "gmcsv*", 0)
                        Case 3
                            mydirectories = System.IO.Directory.GetDirectories("Q:\LowContent\VehicleData", "gmcsv*", 0)
                    End Select

                    For i = 0 To UBound(mydirectories)
                        If InStr(mydirectories(i), "gmcsv") > 0 Then
                            myVehicleFolder = mydirectories(i)
                            myVehicleNumber = Mid(myVehicleFolder, InStr(myVehicleFolder, "gmcsv") + 5, Len(myVehicleFolder))

                            myAnnoFileName = myVehicleFolder & "\" & myVehicleNumber & "_PP_AggregateAnnotations.csv"
                            myAnnoFileFromVehicle = myVehicleFolder & "\" & myVehicleNumber & "_AggregateAnnotations.csv"

                            mySubDirectories = System.IO.Directory.GetDirectories(myVehicleFolder)

                            myAnnotations = Nothing
                            myExistingAnnotations = Nothing
                            y = 0

                            If Not File.Exists(myAnnoFileName) Then

                                For z = 0 To UBound(mySubDirectories)

                                    myfiles = Directory.GetFiles(mySubDirectories(z), "*_ANNO.csv")

                                    For x = 0 To UBound(myfiles)

                                        If Not FileInUse(myfiles(x)) Then

                                            fnum = FreeFile()
                                            FileOpen(fnum, myfiles(x), OpenMode.Input)
                                            Do While Not EOF(fnum)
                                                textline = LineInput(fnum)
                                                If InStr(textline, "0,Anno Type ID") > 0 And Len(myFirstRow) = 0 Then
                                                    myFirstRow = textline
                                                End If
                                                If Mid(textline, 1, 1) = "3" Then
                                                    ReDim Preserve myAnnotations(y)
                                                    myAnnotations(y) = textline
                                                    y += 1
                                                End If
                                            Loop
                                            FileClose(fnum)

                                        End If

                                    Next x

                                Next z

                                If Not myAnnotations Is Nothing Then
                                    ' myAnnoFileName = myVehicleFolder & "\" & myVehicleNumber & "_PP_AggregateAnnotations.csv"
                                    fnum = FreeFile()
                                    If Not File.Exists(myAnnoFileName) Then
                                        FileOpen(fnum, myAnnoFileName, OpenMode.Output)
                                        PrintLine(fnum, myFirstRow)
                                        For x = 0 To UBound(myAnnotations)
                                            PrintLine(fnum, myAnnotations(x))
                                        Next
                                        FileClose(fnum)
                                    Else
                                        FileOpen(fnum, myAnnoFileName, OpenMode.Input)
                                        LineInput(fnum)
                                        j = 0
                                        Do While Not EOF(fnum)
                                            ReDim Preserve myExistingAnnotations(j)
                                            myExistingAnnotations(j) = LineInput(fnum)
                                            j += 1
                                        Loop
                                        FileClose(fnum)
                                        fnum = FreeFile()
                                        FileOpen(fnum, myAnnoFileName, OpenMode.Append)

                                        For x = 0 To UBound(myAnnotations)

                                            myIndex = Array.IndexOf(myExistingAnnotations, myAnnotations(x))

                                            If myIndex = -1 Then
                                                PrintLine(fnum, myAnnotations(x))
                                            End If

                                        Next

                                        FileClose(fnum)

                                    End If

                                End If

                            Else 'myAnnoFileName already exists...

                                If File.Exists(myAnnoFileFromVehicle) Then
                                    fnum = FreeFile()
                                    FileOpen(fnum, myAnnoFileFromVehicle, OpenMode.Input)
                                    LineInput(fnum)
                                    j = 0
                                    Do While Not EOF(fnum)
                                        ReDim Preserve myAnnotations(j)
                                        myAnnotations(j) = LineInput(fnum)
                                        j += 1
                                    Loop
                                    FileClose(fnum)

                                    fnum = FreeFile()
                                    FileOpen(fnum, myAnnoFileName, OpenMode.Input)
                                    LineInput(fnum)
                                    j = 0
                                    Do While Not EOF(fnum)
                                        ReDim Preserve myExistingAnnotations(j)
                                        myExistingAnnotations(j) = LineInput(fnum)
                                        j += 1
                                    Loop
                                    FileClose(fnum)

                                    fnum = FreeFile()
                                    FileOpen(fnum, myAnnoFileName, OpenMode.Append)

                                    For x = 0 To UBound(myAnnotations)

                                        myIndex = Array.IndexOf(myExistingAnnotations, myAnnotations(x))

                                        If myIndex = -1 Then
                                            PrintLine(fnum, myAnnotations(x))
                                        End If

                                    Next

                                    FileClose(fnum)
                                End If

                            End If

                        End If
                    Next i

                Next NumPasses
            End If

        End If

    End Sub

    Private Sub Update_PP_AggregateAnnotationsFiles()

        'Called from the Actions menu.  Updates the vehicle PP_AggregateAnnotationFiles to include the most recent
        'information from the AggregateAnnotationFiles for each vehicle.  See CreateAggregateAnnotationFile routine for
        'more information on this topic...

        Dim myVehicleFolder As String
        Dim mydirectories() As String = Nothing
        'Dim myfiles() As String
        Dim fnum As Integer
        'Dim fnum2 As Integer
        Dim x As Integer
        Dim y As Integer
        'Dim z As Integer
        Dim i As Integer
        Dim j As Integer
        Dim myAnnotations() As String = Nothing
        Dim myExistingAnnotations() As String = Nothing
        Dim textline As String = ""
        Dim myFirstRow As String = ""
        Dim myVehicleNumber As String
        Dim myAnnoFileName As String
        Dim myAnnoFileFromVehicle As String
        'Dim ProcessAllVehicles As Boolean
        Dim mySubDirectories() As String
        'Dim updatePPAnno As Boolean
        Dim NumberofVehicleTypes As Integer
        Dim NumPasses As Integer
        'Dim found As Boolean

        Dim myIndex As Integer

        NumberofVehicleTypes = 3

        For NumPasses = 1 To NumberofVehicleTypes

            Select Case NumPasses

                Case 1
                    mydirectories = System.IO.Directory.GetDirectories("Q:", "gmcsv*", 0)
                Case 2
                    mydirectories = System.IO.Directory.GetDirectories("Q:\HighContent\VehicleData", "gmcsv*", 0)
                Case 3
                    mydirectories = System.IO.Directory.GetDirectories("Q:\LowContent\VehicleData", "gmcsv*", 0)
            End Select

            For i = 0 To UBound(mydirectories)
                If InStr(mydirectories(i), "gmcsv") > 0 Then
                    myVehicleFolder = mydirectories(i)
                    myVehicleNumber = Mid(myVehicleFolder, InStr(myVehicleFolder, "gmcsv") + 5, Len(myVehicleFolder))
                    HandleUserMessageLogging("GMRC", "Update_PP_AggregateAnnotationsFiles: Checking Vehicle " & myVehicleNumber)

                    myAnnoFileName = myVehicleFolder & "\" & myVehicleNumber & "_PP_AggregateAnnotations.csv"
                    myAnnoFileFromVehicle = myVehicleFolder & "\" & myVehicleNumber & "_AggregateAnnotations.csv"

                    mySubDirectories = System.IO.Directory.GetDirectories(myVehicleFolder)

                    myAnnotations = Nothing
                    myExistingAnnotations = Nothing
                    y = 0

                    If System.IO.File.Exists(myAnnoFileFromVehicle) And System.IO.File.Exists(myAnnoFileName) Then
                        fnum = FreeFile()
                        FileOpen(fnum, myAnnoFileFromVehicle, OpenMode.Input)
                        LineInput(fnum)
                        j = 0
                        Do While Not EOF(fnum)
                            ReDim Preserve myAnnotations(j)
                            myAnnotations(j) = LineInput(fnum)
                            j += 1
                        Loop
                        FileClose(fnum)

                        fnum = FreeFile()
                        FileOpen(fnum, myAnnoFileName, OpenMode.Input)
                        LineInput(fnum)
                        j = 0
                        Do While Not EOF(fnum)
                            ReDim Preserve myExistingAnnotations(j)
                            myExistingAnnotations(j) = LineInput(fnum)
                            j += 1
                        Loop
                        FileClose(fnum)

                        fnum = FreeFile()
                        FileOpen(fnum, myAnnoFileName, OpenMode.Append)

                        For x = 0 To UBound(myAnnotations)

                            myIndex = Array.IndexOf(myExistingAnnotations, myAnnotations(x))

                            If myIndex = -1 Then
                                PrintLine(fnum, myAnnotations(x))
                                HandleUserMessageLogging("GMRC", "Update_PP_AggregateAnnotationsFiles: New Annotation Found for " & myVehicleNumber)
                            End If

                        Next

                        FileClose(fnum)
                    End If

                End If

            Next i

        Next NumPasses

    End Sub
    Private Sub LoadLists(Optional ByVal ReportPath As String = "")

        'Called when Display All button is pressed and on various other user initiated events to update the listboxes with information pertinent to the user selections such as
        'vehicle number, software versions, etc.  Also updates the counters for each event type such as "CLEVIR Hung up during Recording", "CLEVIR Kill Switch Activated", etc...

        Dim filename As String = ""
        Dim fnum As Integer
        Dim x As Integer
        Dim textline As String
        Dim mylistbox As ListBox = Nothing

        Dim TextlineArray() As String

        Dim mySavePath As String = ""

        Dim TextlineInListBox As Boolean

        Dim VEHICLE_SPY_REPLAY_BLOCK_START_Cnt As Integer
        Dim CLEVIRHungupduringRecording_Cnt As Integer
        Dim CLEVIRKillSwitchActivated_Cnt As Integer
        Dim CLEVIRAppearstohaveHungup_Cnt As Integer
        Dim CAMERA_INIT_ISSUE_Cnt As Integer
        Dim QUESTIONABLE_USER_INTERACTION_Cnt As Integer
        Dim QUESTIONABLEappEXIT_Cnt As Integer
        Dim VEHICLE_SPY_READ_DID_REPLAY_BLOCK_START_Cnt As Integer
        Dim VEHICLE_SPY_VERIFICATION_ISSUE_Cnt As Integer
        Dim INITIALIZATION_COMPLETE_ISSUE_Cnt As Integer

        Static StopToStartNonZeroNumArray() As Integer = Nothing
        Static StopToStartTotalSeconds As Integer
        Dim StopToStartAverageNonZero As Integer
        Static StopToStartMaxSeconds As Integer

        Static InitNonZeroNumArray() As Integer = Nothing
        Static InitTotalSeconds As Integer
        Dim InitAverageNonZero As Integer
        Static InitMaxSeconds As Integer

        'Static FixedListsLoaded As Boolean

        'If ListBox1.Items.Count > 0 Then
        'FixedListsLoaded = True
        'End If

        If NumVersionsSelected <= 1 Then

            StopToStartNonZeroNumArray = Nothing
            StopToStartTotalSeconds = 0
            StopToStartMaxSeconds = 0

            InitNonZeroNumArray = Nothing
            InitTotalSeconds = 0
            InitMaxSeconds = 0

            ListBox1.Items.Clear()

            ListBox3.Items.Clear()
            ListBox4.Items.Clear()
            ListBox6.Items.Clear()

            ListBox5.Items.Clear()

            Label3.Text = "0"
            Label4.Text = "0"
            Label6.Text = "0"
            Label8.Text = "0"
            Label10.Text = "0"
            Label12.Text = "0"
            Label14.Text = "0"
            Label16.Text = "0"
            Label18.Text = "0"
            Label28.Text = "0"

        End If

        If Len(VehicleNumber) > 0 Then
            ReDim Preserve SearchString(0)
            SearchString(0) = VehicleNumber
        End If

        If Len(VersionNumber) > 0 Then
            If SearchString Is Nothing Then
                ReDim Preserve SearchString(0)
            Else
                ReDim Preserve SearchString(UBound(SearchString) + 1)
            End If
            SearchString(UBound(SearchString)) = VersionNumber
        End If

        If Len(AdditionalSearchCriterian) > 0 Then
            If SearchString Is Nothing Then
                ReDim Preserve SearchString(0)
            Else
                ReDim Preserve SearchString(UBound(SearchString) + 1)
            End If
            SearchString(UBound(SearchString)) = AdditionalSearchCriterian
        End If

        For x = 0 To 4

            Select Case x
                Case 0

                    filename = mySavepathprefix & "\Reports\" & ReportPath & "LastUploadTime.csv"

                    mylistbox = ListBox1
                Case 1
                    filename = mySavepathprefix & "\Reports\" & ReportPath & "Errors.csv"
                    mylistbox = ListBox3
                Case 2
                    filename = mySavepathprefix & "\Reports\" & ReportPath & "DelayTimes.csv"
                    mylistbox = ListBox4

                    If Not SearchString Is Nothing Then

                        If Len(AdditionalSearchCriterian) > 0 Then

                            Select Case UBound(SearchString)

                                Case 0
                                    SearchString = Nothing
                                Case 1
                                    ReDim Preserve SearchString(0)
                                Case 2
                                    ReDim Preserve SearchString(1)
                            End Select

                        End If

                    End If

                Case 3

                    filename = mySavepathprefix & "\Reports\" & ReportPath & "InitDelayTimes.csv"
                    mylistbox = ListBox6

                    If Not SearchString Is Nothing Then

                        If Len(AdditionalSearchCriterian) > 0 Then

                            Select Case UBound(SearchString)

                                Case 0
                                    SearchString = Nothing
                                Case 1
                                    ReDim Preserve SearchString(0)
                                Case 2
                                    ReDim Preserve SearchString(1)
                            End Select

                        End If

                    End If

                Case 4
                    filename = mySavepathprefix & "\Reports\" & ReportPath & "SoftwareVersions.csv"
                    mylistbox = ListBox5

            End Select

            If File.Exists(filename) Then

                fnum = FreeFile()

                FileOpen(fnum, filename, OpenMode.Input)

                Do While Not EOF(fnum)

                    textline = LineInput(fnum)

                    'here is where we will filter out entries in errors.csv that are no longer considered errors...

                    Do While InStr(textline, "ExitApp () called...") > 0 And (InStr(textline, "WriteMileage") > 0 Or InStr(textline, "Cannot Communicate") > 0)
                        textline = LineInput(fnum)
                    Loop

                    Do While InStr(textline, "Appears to have Hung up") > 0 And
                        (InStr(textline, "Data Sync Complete") > 0 Or
                        InStr(textline, "Today Only") > 0 Or
                        InStr(textline, "All Users Selected") > 0 Or
                        InStr(textline, "EnableWirelessNetworkConnection: Wireless connection verified") > 0 Or
                        InStr(textline, "EnableWirelessNetworkConnection: Wireless Adapter ReEnabled") > 0 Or
                        InStr(textline, "FlashingStatus form Closed") > 0 Or
                        InStr(textline, "Shutdown windows") > 0 Or
                        InStr(textline, "Current User Only") > 0)
                        textline = LineInput(fnum)
                    Loop

                    TextlineInListBox = False

                    If InStr(filename, "Errors.csv") > 0 Then

                        If IsDate(Mid(textline, 16, 19)) Then
                            'Save event date and time for each line...
                            EventDateTime = Mid(textline, 16, 19)
                        End If

                        Do While Convert.ToDateTime(EventDateTime) < Convert.ToDateTime(StartDateTime)
                            'eat up any line that has a datetime < pre-defined start time, which is currently defaulted in the datetime picker as 10/01/2020...
                            textline = LineInput(fnum)
                            If IsDate(Mid(textline, 16, 19)) Then
                                EventDateTime = Mid(textline, 16, 19)
                            End If
                        Loop

                        If Not SearchString Is Nothing Then

                            If UBound(SearchString) = 0 Then
                                If Len(VehicleNumber) > 0 Then
                                    If InStr(textline, VehicleNumber) > 0 Then
                                        mylistbox.Items.Add(textline)
                                        TextlineInListBox = True
                                    End If
                                Else
                                    If InStr(textline, SearchString(0)) > 0 Then
                                        mylistbox.Items.Add(textline)
                                        TextlineInListBox = True
                                    End If
                                End If

                            ElseIf UBound(SearchString) = 1 Then
                                If Len(VehicleNumber) > 0 Then

                                    If InStr(textline, SearchString(0)) > 0 And InStr(textline, SearchString(1)) > 0 Then
                                        mylistbox.Items.Add(textline)
                                        TextlineInListBox = True
                                    End If

                                Else
                                    If InStr(textline, SearchString(0)) > 0 And InStr(textline, SearchString(1)) > 0 Then
                                        mylistbox.Items.Add(textline)
                                        TextlineInListBox = True
                                    End If
                                End If

                            ElseIf UBound(SearchString) = 2 Then

                                If InStr(textline, SearchString(0)) > 0 And InStr(textline, SearchString(1)) > 0 And InStr(textline, SearchString(2)) > 0 Then
                                    mylistbox.Items.Add(textline)
                                    TextlineInListBox = True
                                End If
                            End If

                        Else
                            mylistbox.Items.Add(textline)
                            TextlineInListBox = True
                        End If

                        If TextlineInListBox = True Then

                            If InStr(textline, "VEHICLE SPY REPLAY BLOCK START") Then
                                VEHICLE_SPY_REPLAY_BLOCK_START_Cnt += 1
                            End If
                            If InStr(textline, "CLEVIR Hung up during Recording") Then
                                CLEVIRHungupduringRecording_Cnt += 1
                            End If
                            If InStr(textline, "CLEVIR Kill Switch Activated") Then
                                CLEVIRKillSwitchActivated_Cnt += 1
                            End If
                            If InStr(textline, "CLEVIR Appears to have Hung up") Then
                                CLEVIRAppearstohaveHungup_Cnt += 1
                            End If
                            If InStr(textline, "CAMERA INIT ISSUE") And InStr(textline, "CAN") = 0 And InStr(textline, "HC") = 0 _
                                And InStr(textline, "XETK") = 0 And InStr(textline, "IP") = 0 And InStr(textline, "IR") = 0 _
                                And InStr(textline, "K1") = 0 And InStr(textline, "K2") = 0 And InStr(textline, "ACP") = 0 Then
                                CAMERA_INIT_ISSUE_Cnt += 1
                            End If
                            If InStr(textline, "QUESTIONABLE USER INTERACTION") Then
                                QUESTIONABLE_USER_INTERACTION_Cnt += 1
                            End If
                            If InStr(textline, "QUESTIONABLE APP EXIT") Then
                                QUESTIONABLEappEXIT_Cnt += 1
                            End If
                            If InStr(textline, "VEHICLE SPY READ DID REPLAY BLOCK START") Then
                                VEHICLE_SPY_READ_DID_REPLAY_BLOCK_START_Cnt += 1
                            End If
                            If InStr(textline, "VEHICLE SPY VERIFICATION ISSUE") Then
                                VEHICLE_SPY_VERIFICATION_ISSUE_Cnt += 1
                            End If
                            If InStr(textline, "INITIALIZATION COMPLETE ISSUE") Then
                                INITIALIZATION_COMPLETE_ISSUE_Cnt += 1
                            End If

                        End If

                    ElseIf InStr(filename, "\Reports\" & ReportPath & "DelayTimes.csv") > 0 Then

                        If mylistbox.Items.Count = 0 Then
                            mylistbox.Items.Add(" VEHNUM   " & "," & "VERSION,RECSTTM              ,RECSTPTOSTDLYTM,RECINITSTDLYTM,RECSTDLYTM,RECSTPREQTORECSTPDLYTM,VSPYRECSTDLYTM,VSPYRECSTPREQTORECSTPDLYTM,ENAVSPYRECSTAFTRECSTP,DIDPULLHASRUN")
                        End If

                        If IsDate(Mid(textline, 16, 19)) Then
                            'Save event date and time for each line...
                            EventDateTime = Mid(textline, 16, 19)
                        End If

                        Do While Convert.ToDateTime(EventDateTime) < Convert.ToDateTime(StartDateTime)
                            'eat up any line that has a datetime < pre-defined start time, which is currently 01/01/2020...
                            textline = LineInput(fnum)
                            If IsDate(Mid(textline, 16, 19)) Then
                                EventDateTime = Mid(textline, 16, 19)
                            End If
                        Loop

                        If Not SearchString Is Nothing Then

                            If UBound(SearchString) = 0 Then
                                If Len(VehicleNumber) > 0 Then
                                    If InStr(textline, VehicleNumber) > 0 Then
                                        TextlineArray = Split(textline, ",")

                                        If Trim(TextlineArray(3)) <> "0" Then
                                            If StopToStartNonZeroNumArray Is Nothing Then
                                                ReDim StopToStartNonZeroNumArray(0)
                                            Else
                                                ReDim Preserve StopToStartNonZeroNumArray(UBound(StopToStartNonZeroNumArray) + 1)
                                            End If

                                            StopToStartNonZeroNumArray(UBound(StopToStartNonZeroNumArray)) = Val(Trim(TextlineArray(3)))
                                            StopToStartTotalSeconds += Val(Trim(TextlineArray(3)))
                                        End If

                                        mylistbox.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3).PadRight(15, " ") & "," & TextlineArray(4).PadRight(14, " ") & "," & TextlineArray(5).PadRight(10, " ") & "," & TextlineArray(6).PadRight(22, " ") & "," & TextlineArray(7).PadRight(14, " ") & "," & TextlineArray(8).PadRight(26, " ") & "," & TextlineArray(9).PadRight(21, " ") & "," & TextlineArray(10).PadRight(13, " "))

                                    End If
                                Else
                                    If InStr(textline, SearchString(0)) > 0 Then
                                        TextlineArray = Split(textline, ",")

                                        If Trim(TextlineArray(3)) <> "0" Then
                                            If StopToStartNonZeroNumArray Is Nothing Then
                                                ReDim StopToStartNonZeroNumArray(0)
                                            Else
                                                ReDim Preserve StopToStartNonZeroNumArray(UBound(StopToStartNonZeroNumArray) + 1)
                                            End If

                                            StopToStartNonZeroNumArray(UBound(StopToStartNonZeroNumArray)) = Val(Trim(TextlineArray(3)))
                                            StopToStartTotalSeconds += Val(Trim(TextlineArray(3)))
                                        End If

                                        mylistbox.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3).PadRight(15, " ") & "," & TextlineArray(4).PadRight(14, " ") & "," & TextlineArray(5).PadRight(10, " ") & "," & TextlineArray(6).PadRight(22, " ") & "," & TextlineArray(7).PadRight(14, " ") & "," & TextlineArray(8).PadRight(26, " ") & "," & TextlineArray(9).PadRight(21, " ") & "," & TextlineArray(10).PadRight(13, " "))

                                    End If
                                End If

                            ElseIf UBound(SearchString) = 1 Then

                                If InStr(textline, SearchString(0)) > 0 And InStr(textline, SearchString(1)) > 0 Then
                                    TextlineArray = Split(textline, ",")

                                    If Trim(TextlineArray(3)) <> "0" Then
                                        If StopToStartNonZeroNumArray Is Nothing Then
                                            ReDim StopToStartNonZeroNumArray(0)
                                        Else
                                            ReDim Preserve StopToStartNonZeroNumArray(UBound(StopToStartNonZeroNumArray) + 1)
                                        End If

                                        StopToStartNonZeroNumArray(UBound(StopToStartNonZeroNumArray)) = Val(Trim(TextlineArray(3)))
                                        StopToStartTotalSeconds += Val(Trim(TextlineArray(3)))
                                    End If

                                    mylistbox.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3).PadRight(15, " ") & "," & TextlineArray(4).PadRight(14, " ") & "," & TextlineArray(5).PadRight(10, " ") & "," & TextlineArray(6).PadRight(22, " ") & "," & TextlineArray(7).PadRight(14, " ") & "," & TextlineArray(8).PadRight(26, " ") & "," & TextlineArray(9).PadRight(21, " ") & "," & TextlineArray(10).PadRight(13, " "))

                                End If

                            End If

                        Else
                            TextlineArray = Split(textline, ",")

                            If Trim(TextlineArray(3)) <> "0" Then
                                If StopToStartNonZeroNumArray Is Nothing Then
                                    ReDim StopToStartNonZeroNumArray(0)
                                Else
                                    ReDim Preserve StopToStartNonZeroNumArray(UBound(StopToStartNonZeroNumArray) + 1)
                                End If

                                StopToStartNonZeroNumArray(UBound(StopToStartNonZeroNumArray)) = Val(Trim(TextlineArray(3)))
                                StopToStartTotalSeconds += Val(Trim(TextlineArray(3)))
                            End If


                            mylistbox.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3).PadRight(15, " ") & "," & TextlineArray(4).PadRight(14, " ") & "," & TextlineArray(5).PadRight(10, " ") & "," & TextlineArray(6).PadRight(22, " ") & "," & TextlineArray(7).PadRight(14, " ") & "," & TextlineArray(8).PadRight(26, " ") & "," & TextlineArray(9).PadRight(21, " ") & "," & TextlineArray(10).PadRight(13, " "))

                        End If

                    ElseIf InStr(filename, "InitDelayTimes.csv") > 0 Then

                        If mylistbox.Items.Count = 0 Then
                            mylistbox.Items.Add(" VEHNUM   " & "," & "VERSION,INITSTTM             ,INITDLYTM")
                        End If


                        If IsDate(Mid(textline, 16, 19)) Then
                            'Save event date and time for each line...
                            EventDateTime = Mid(textline, 16, 19)
                        End If

                        Do While Convert.ToDateTime(EventDateTime) < Convert.ToDateTime(StartDateTime)
                            'eat up any line that has a datetime < pre-defined start time, which is currently 01/01/2020...
                            textline = LineInput(fnum)
                            If IsDate(Mid(textline, 16, 19)) Then
                                EventDateTime = Mid(textline, 16, 19)
                            End If
                        Loop

                        If Not SearchString Is Nothing Then

                            If UBound(SearchString) = 0 Then
                                If Len(VehicleNumber) > 0 Then
                                    If InStr(textline, VehicleNumber) > 0 Then
                                        TextlineArray = Split(textline, ",")

                                        If Trim(TextlineArray(3)) <> "0" Then
                                            If InitNonZeroNumArray Is Nothing Then
                                                ReDim InitNonZeroNumArray(0)
                                            Else
                                                ReDim Preserve InitNonZeroNumArray(UBound(InitNonZeroNumArray) + 1)
                                            End If

                                            InitNonZeroNumArray(UBound(InitNonZeroNumArray)) = Val(Trim(TextlineArray(3)))
                                            InitTotalSeconds += Val(Trim(TextlineArray(3)))
                                        End If

                                        mylistbox.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3))

                                    End If
                                Else
                                    If InStr(textline, SearchString(0)) > 0 Then
                                        TextlineArray = Split(textline, ",")

                                        If Trim(TextlineArray(3)) <> "0" Then
                                            If InitNonZeroNumArray Is Nothing Then
                                                ReDim InitNonZeroNumArray(0)
                                            Else
                                                ReDim Preserve InitNonZeroNumArray(UBound(InitNonZeroNumArray) + 1)
                                            End If

                                            InitNonZeroNumArray(UBound(InitNonZeroNumArray)) = Val(Trim(TextlineArray(3)))
                                            InitTotalSeconds += Val(Trim(TextlineArray(3)))
                                        End If

                                        mylistbox.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3))

                                    End If
                                End If

                            ElseIf UBound(SearchString) = 1 Then

                                If InStr(textline, SearchString(0)) > 0 And InStr(textline, SearchString(1)) > 0 Then
                                    TextlineArray = Split(textline, ",")

                                    If Trim(TextlineArray(3)) <> "0" Then
                                        If InitNonZeroNumArray Is Nothing Then
                                            ReDim InitNonZeroNumArray(0)
                                        Else
                                            ReDim Preserve InitNonZeroNumArray(UBound(InitNonZeroNumArray) + 1)
                                        End If

                                        InitNonZeroNumArray(UBound(InitNonZeroNumArray)) = Val(Trim(TextlineArray(3)))
                                        InitTotalSeconds += Val(Trim(TextlineArray(3)))
                                    End If

                                    mylistbox.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3))

                                End If

                            End If

                        Else
                            TextlineArray = Split(textline, ",")

                            If Trim(TextlineArray(3)) <> "0" Then
                                If InitNonZeroNumArray Is Nothing Then
                                    ReDim InitNonZeroNumArray(0)
                                Else
                                    ReDim Preserve InitNonZeroNumArray(UBound(InitNonZeroNumArray) + 1)
                                End If

                                InitNonZeroNumArray(UBound(InitNonZeroNumArray)) = Val(Trim(TextlineArray(3)))
                                InitTotalSeconds += Val(Trim(TextlineArray(3)))
                            End If


                            mylistbox.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3))

                        End If

                    ElseIf InStr(filename, "SoftwareVersions.csv") > 0 Then

                        'If FixedListsLoaded = False Then
                        'SoftwareVersionsList.Add(textline)                 

                        mylistbox.Items.Add(textline)
                        'End If

                    Else 'LastUploadTime.csv
                        'If FixedListsLoaded = False Then
                        mylistbox.Items.Add(textline)
                        'End If

                    End If

                Loop

                FileClose(fnum)
            End If

        Next

        'SoftwareVersionsList.Sort()
        'SoftwareVersionsList.Reverse()

        'For x = 0 To SoftwareVersionsList.Count - 1
        'mylistbox.Items.Add(SoftwareVersionsList(x))
        'Next

        mylistbox.Refresh()

        If Not StopToStartNonZeroNumArray Is Nothing Then

            Array.Sort(StopToStartNonZeroNumArray)

            StopToStartMaxSeconds = StopToStartNonZeroNumArray(UBound(StopToStartNonZeroNumArray))

            Label26.Text = StopToStartMaxSeconds.ToString

            If StopToStartNonZeroNumArray.Length Mod 2 <> 0 Then 'uneven amount of numbers
                Label22.Text = StopToStartNonZeroNumArray(StopToStartNonZeroNumArray.GetUpperBound(0) \ 2).ToString
            Else 'even amount of numbers
                Dim num1 As Integer = StopToStartNonZeroNumArray(StopToStartNonZeroNumArray.Length \ 2)
                Dim num2 As Integer = StopToStartNonZeroNumArray((StopToStartNonZeroNumArray.Length \ 2) - 1)
                Dim median As Integer = (num1 + num2) \ 2
                Label22.Text = median.ToString
            End If

            StopToStartAverageNonZero = StopToStartTotalSeconds \ StopToStartNonZeroNumArray.Length
            Label24.Text = StopToStartAverageNonZero.ToString

        End If

        If Not InitNonZeroNumArray Is Nothing Then

            Array.Sort(InitNonZeroNumArray)

            InitMaxSeconds = InitNonZeroNumArray(UBound(InitNonZeroNumArray))

            Label30.Text = InitMaxSeconds.ToString

            If InitNonZeroNumArray.Length Mod 2 <> 0 Then 'uneven amount of numbers
                Label34.Text = InitNonZeroNumArray(InitNonZeroNumArray.GetUpperBound(0) \ 2).ToString
            Else 'even amount of numbers
                Dim num1 As Integer = InitNonZeroNumArray(InitNonZeroNumArray.Length \ 2)
                Dim num2 As Integer = InitNonZeroNumArray((InitNonZeroNumArray.Length \ 2) - 1)
                Dim median As Integer = (num1 + num2) \ 2
                Label34.Text = median.ToString
            End If

            InitAverageNonZero = InitTotalSeconds \ InitNonZeroNumArray.Length
            Label32.Text = InitAverageNonZero.ToString

            Label3.Text = Val(Label3.Text) + VEHICLE_SPY_REPLAY_BLOCK_START_Cnt
            Label4.Text = Val(Label4.Text) + CLEVIRHungupduringRecording_Cnt
            Label6.Text = Val(Label6.Text) + CLEVIRKillSwitchActivated_Cnt
            Label8.Text = Val(Label8.Text) + CLEVIRAppearstohaveHungup_Cnt
            Label10.Text = Val(Label10.Text) + CAMERA_INIT_ISSUE_Cnt
            Label12.Text = Val(Label12.Text) + QUESTIONABLE_USER_INTERACTION_Cnt
            Label14.Text = Val(Label14.Text) + QUESTIONABLEappEXIT_Cnt
            Label16.Text = Val(Label16.Text) + VEHICLE_SPY_READ_DID_REPLAY_BLOCK_START_Cnt
            Label18.Text = Val(Label18.Text) + VEHICLE_SPY_VERIFICATION_ISSUE_Cnt
            Label28.Text = Val(Label28.Text) + INITIALIZATION_COMPLETE_ISSUE_Cnt

        End If

        'FixedListsLoaded = True

    End Sub

    Private Sub FormatExperimentVariableOutput(mydialog As FileDialog)

        'Called from Actions menu PerformSupportFileConfigurationToolStripMenuItem_Click...

        'This routine formats the CAN variables exported from a new experiment so they can be easily
        'added to the signal list.

        'This routine is legacy from before the process of generating new files for a new software verion was more fully automated as it is now.
        'We keep this here just in case we need to perform this operation manually.  


        Dim fnum As Integer
        Dim fnum2 As Integer
        Dim filename1 As String
        Dim textline As String

        Dim cnt As Integer

        Dim TextArray(3) As String

        filename1 = SelectFile(mydialog, My.Application.Info.DirectoryPath, "txt", True)

        If Len(filename1) > 0 Then

            fnum = FreeFile()
            FileOpen(fnum, filename1, OpenMode.Input)

            fnum2 = FreeFile()
            FileOpen(fnum2, My.Application.Info.DirectoryPath & "\SaveExperimentVariables.csv", OpenMode.Output)

            LineInput(fnum)

            cnt = 0
            Do While Not EOF(fnum)

                If cnt = 3 Then cnt = 0

                If Not EOF(fnum) Then
                    textline = LineInput(fnum)
                Else
                    Exit Do
                End If

                Select Case cnt
                    Case 0
                        TextArray(2) = Mid(textline, 12, Len(textline))
                    Case 1
                        TextArray(3) = Mid(textline, 12, Len(textline))
                    Case 2
                        TextArray(0) = Mid(textline, 12, InStr(textline, "\") - 12)
                        TextArray(1) = ""

                End Select

                cnt += 1
                If cnt = 3 Then
                    PrintLine(fnum2, TextArray(0) & "," & TextArray(1) & "," & TextArray(2) & "," & TextArray(3))
                End If
            Loop

            FileClose(fnum)
            FileClose(fnum2)

        Else
            MsgBox("Please select a valid filename...")
        End If

    End Sub

    Private Sub CreateListsFromFileContent(ByVal ReportPath As String)

        'Called from Copy Vehicle Logs from Q button and Copy PC Logs from Q - Button.  Adds information from saved files into
        'lists for each type of status information used by the VehicleStatDashboard...

        Dim fnum As Integer
        Dim myList As List(Of String) = Nothing
        Dim x As Integer
        Dim filename As String = ""
        Dim textline As String

        Try

            ErrorsList = New List(Of String)
            DelayTimesList = New List(Of String)
            InitDelayTimesList = New List(Of String)
            SoftwareVersionsList = New List(Of String)

            For x = 0 To 3

                Select Case x

                    Case 0
                        filename = mySavepathprefix & "\Reports\" & ReportPath & "Errors.csv"
                        myList = ErrorsList
                    Case 1
                        filename = mySavepathprefix & "\Reports\" & ReportPath & "DelayTimes.csv"
                        myList = DelayTimesList
                    Case 2
                        filename = mySavepathprefix & "\Reports\" & ReportPath & "InitDelayTimes.csv"
                        myList = InitDelayTimesList
                    Case 3
                        filename = mySavepathprefix & "\Reports\" & ReportPath & "SoftwareVersions.csv"
                        myList = SoftwareVersionsList

                End Select

                If File.Exists(filename) Then

                    fnum = FreeFile()

                    FileOpen(fnum, filename, OpenMode.Input)

                    Do While Not EOF(fnum)
                        textline = LineInput(fnum)
                        If Not myList.Contains(textline) Then
                            myList.Add(textline)
                        End If
                    Loop

                    FileClose(fnum)
                    myList.Sort()

                End If

            Next x

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", ex.Message, DisplayMsgBox)
        End Try

    End Sub

    Private Sub AddToReport(ByVal ReportFileName As String, ByVal myVehicleNumber As String, ByVal InputStr As String, Optional ByVal ReportPath As String = "", Optional ByVal ComputerHostName As String = "")

        'Called from ParseCLEVIRLogFile, which is called from CopyLogFiles. This routine adds information from the new log files copied from the share drive
        'to the specified report file.  Report files are "Errors.csv", "LastUploadTime.csv", "DelayTimes.csv", "InitDelayTimes.csv" and "SoftwareVersions.csv".
        'These reports contain categorized information from the log files to identify various types of events that may indicate CLEVIR software issues.  Or
        'calculations of time intervals associated with start and stop delays, time to initialize, etc...

        Dim fnum As Integer
        Dim filename As String '= mySavepathprefix & "\Reports\" & ReportFileName
        'Dim found As Boolean
        Dim InputStrInfo() = Split(InputStr, ",")
        Dim textline As String
        Dim VehNumFound As Boolean
        Dim SaveTextline() As String = Nothing
        Dim x As Integer

        Try

            If Len(ReportPath) > 0 Then
                If Not System.IO.Directory.Exists(mySavepathprefix & "\Reports\" & ReportPath) Then
                    System.IO.Directory.CreateDirectory(mySavepathprefix & "\Reports\" & ReportPath)
                End If
            End If

            filename = mySavepathprefix & "\Reports\" & ReportPath & ReportFileName

            If File.Exists(filename) Then

                Select Case ReportFileName
                    Case "Errors.csv"

                        'need to check if inputstr & computerhostname exist here...

                        If Len(ComputerHostName) > 0 Then
                            If ErrorsList.Contains(InputStr & "," & ComputerHostName) = False Then
                                ErrorsList.Add(InputStr & "," & ComputerHostName)
                            End If
                        Else
                            If ErrorsList.Contains(InputStr) = False Then
                                ErrorsList.Add(InputStr)
                            End If
                        End If

                    Case "LastUploadTime.csv"

                        'check file for vehicle number and replace info related to that vehicle number with updated info...

                        Dim tempstr As String

                        If Len(ComputerHostName) > 0 Then
                            tempstr = ComputerHostName
                        Else
                            tempstr = myVehicleNumber
                        End If

                        fnum = FreeFile()
                        FileOpen(fnum, filename, OpenMode.Input)

                        Do While Not EOF(fnum)
                            textline = LineInput(fnum)

                            If SaveTextline Is Nothing Then
                                ReDim Preserve SaveTextline(0)
                            Else
                                ReDim Preserve SaveTextline(UBound(SaveTextline) + 1)
                            End If
                            SaveTextline(UBound(SaveTextline)) = textline

                            If InStr(textline, tempstr) > 0 Then
                                VehNumFound = True
                                SaveTextline(UBound(SaveTextline)) = tempstr & "," & InputStrInfo(0) & "," & InputStrInfo(1) 'LatestVersion, GetLastWriteTime of ComputerHostName...
                                'Exit Do
                            End If

                        Loop

                        If VehNumFound = False Then
                            FileClose(fnum)
                            fnum = FreeFile()
                            FileOpen(fnum, filename, OpenMode.Append)
                            PrintLine(fnum, tempstr & "," & InputStr)
                            FileClose(fnum)
                        Else
                            FileClose(fnum)
                            fnum = FreeFile()
                            FileOpen(fnum, filename, OpenMode.Output)
                            For x = 0 To UBound(SaveTextline)
                                PrintLine(fnum, SaveTextline(x))
                            Next x
                            FileClose(fnum)
                        End If


                    Case "DelayTimes.csv"

                        If DelayTimesList.Count = 0 Then
                            DelayTimesList.Add(" VEHNUM   " & "," & "VERSION,RECSTTM              ,RECSTPTOSTDLYTM,RECINITSTDLYTM,RECSTDLYTM,RECSTPREQTORECSTPDLYTM,VSPYRECSTDLYTM,VSPYRECSTPREQTORECSTPDLYTM,ENAVSPYRECSTAFTRECSTP,DIDPULLHASRUN")
                        End If

                        If DelayTimesList.Contains(InputStr) = False Then
                            DelayTimesList.Add(InputStr)
                        End If

                    Case "InitDelayTimes.csv"

                        If InitDelayTimesList.Count = 0 Then
                            InitDelayTimesList.Add(" VEHNUM   " & "," & "VERSION,INITSTTM             ,INITDLYTM")
                        End If

                        If InitDelayTimesList.Contains(InputStr) = False Then
                            InitDelayTimesList.Add(InputStr)
                        End If

                    Case "SoftwareVersions.csv"

                        If SoftwareVersionsList.Contains(InputStr) = False Then
                            SoftwareVersionsList.Add(InputStr)
                        End If

                End Select

            Else 'filename does not yet exist, we are creating a new file...

                'FileOpen(fnum, filename, OpenMode.Output)

                'MM/ dd / yyy HH:mm : ss

                If ReportFileName = "DelayTimes.csv" Then

                    DelayTimesList = New List(Of String) From {
                        " VEHNUM   " & "," & "VERSION,RECSTTM              ,RECSTPTOSTDLYTM,RECINITSTDLYTM,RECSTDLYTM,RECSTPREQTORECSTPDLYTM,VSPYRECSTDLYTM,VSPYRECSTPREQTORECSTPDLYTM,ENAVSPYRECSTAFTRECSTP,DIDPULLHASRUN"
                    }
                    fnum = FreeFile()

                    FileOpen(fnum, filename, OpenMode.Output)
                    PrintLine(fnum, InputStr)
                    FileClose(fnum)
                End If


                If ReportFileName = "InitDelayTimes.csv" Then

                    InitDelayTimesList = New List(Of String) From {
                        " VEHNUM   " & "," & "VERSION,INITSTTM             ,INITDLYTM"
                    }
                    fnum = FreeFile()

                    FileOpen(fnum, filename, OpenMode.Output)
                    PrintLine(fnum, InputStr)
                    FileClose(fnum)
                End If

                If ReportFileName = "Errors.csv" Then

                    ErrorsList = New List(Of String)

                    If Len(ComputerHostName) > 0 Then
                        ErrorsList.Add(InputStr & "," & ComputerHostName)
                    Else
                        ErrorsList.Add(InputStr)
                    End If

                    fnum = FreeFile()

                    FileOpen(fnum, filename, OpenMode.Output)

                    If Len(ComputerHostName) > 0 Then
                        PrintLine(fnum, InputStr)
                    Else
                        PrintLine(fnum, InputStr & "," & ComputerHostName)
                    End If

                    PrintLine(fnum, InputStr)
                    FileClose(fnum)
                End If

                If ReportFileName = "LastUploadTime.csv" Then

                    Dim tempstr As String

                    If Len(ComputerHostName) > 0 Then
                        tempstr = ComputerHostName
                    Else
                        tempstr = myVehicleNumber
                    End If

                    fnum = FreeFile()

                    FileOpen(fnum, filename, OpenMode.Output)
                    PrintLine(fnum, tempstr & "," & InputStr)
                    FileClose(fnum)


                End If

                If ReportFileName = "SoftwareVersions.csv" Then
                    fnum = FreeFile()

                    FileOpen(fnum, filename, OpenMode.Output)
                    PrintLine(fnum, InputStr)
                    FileClose(fnum)
                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "AddToReport: " & ex.Message, DisplayMsgBox)
        End Try

    End Sub


    Private Sub ParseCLEVIRLogFile(ByVal filename As String)

        'This routine is called by CopyLogFiles.  This is where the contents of the log files are parsed to determine if abnormal or undesired behavior is indicated from the
        'contents of the log file.  These abnormal events are categorized and the information is put into the Errors.csv file.  Also, calculations for delay times and
        'initializaiton times are made here and this information is saved to the appropriate report files...

        Dim fnum As Integer
        Dim LatestVersion As String = ""
        Dim textline As String
        Dim StartNum As Integer
        Dim SaveStartKey As String = ""
        Dim EndKey As String = ""

        Dim InitialRecordStartTime As DateTime = Nothing

        Dim RecordingStartedTime As DateTime = Nothing
        Dim RecordingStoppedTime As DateTime = Nothing
        Dim StartRecordingRequestedTime As DateTime = Nothing
        Dim StopRecordingRequestedTime As DateTime = Nothing

        Dim InitRecordStartDelayTime As TimeSpan = Nothing

        Dim RecordStartDelayTime As TimeSpan = Nothing
        Dim RecordStopToStartDelayTime As TimeSpan = Nothing
        Dim RecordStopReqToRecordStopDelayTime As TimeSpan = Nothing

        Dim VspyRecordingStartedTime As DateTime = Nothing
        Dim VspyRecordingStoppedTime As DateTime = Nothing
        Dim VspyStartRecordingRequestedTime As DateTime = Nothing
        Dim VspyStopRecordingRequestedTime As DateTime = Nothing

        Dim VSpyRecordStartDelayTime As TimeSpan = Nothing
        Dim VspyRecordStopReqToRecordStopDelayTime As TimeSpan = Nothing

        Dim VspyEnableAltRecReStartAfterRecordStopFlag As Boolean

        Dim InitStartTime As DateTime
        Dim InitEndTime As DateTime
        Dim InitTimeDelay As TimeSpan

        Dim RecordingStarted As Boolean

        Dim StartRecordButtonPressed As Boolean
        Dim StopRecordingRequested As Boolean

        Dim OperatorManualIntevention As Boolean

        Dim InDIDRead As Boolean
        Dim DIDPullHasBeenRun As Boolean

        Dim SaveTextline As String = ""

        Dim INCAEventDateTime As DateTime
        Dim CLEVIREventDateTime As DateTime

        Dim ExperimentName As String = ""

        Dim myExperiment As String = ""
        Dim myProcessor As String = ""
        Dim InvalidDataFlagged As Boolean
        Dim InvalidVideoFlagged As Boolean
        Dim ProcessorCommFault As Boolean
        Dim ProcCommFaultFlagged As Boolean
        Dim InvalidData As Boolean

        Dim StrStart As Integer
        Dim strLen As Integer

        Dim tempstr As String
        Dim myVehicleNumber As String = VehicleNumber

        Dim AddToStringForVersion As Integer

        Static FileCounter As Integer
        Static DataAlertCount As Integer '3
        Static VideoAlertCount As Integer '5
        Static ProcessorCommAlertCount As Integer '7

        Try

            If CheckBox1.Checked = True Then

                If Not DisplayLogFile.Visible = True Then
                    DisplayLogFile.Text = "Display Log File Status Info"
                    DisplayLogFile.ListBox2.BringToFront()
                    DisplayLogFile.ListBox1.SendToBack()
                    DisplayLogFile.Show()
                    DisplayLogFile.Refresh()
                End If

            End If


            fnum = FreeFile()

            If File.Exists(filename) Then

                FileOpen(fnum, filename, OpenMode.Input)

                Do While Not EOF(fnum)

                    'Start going thru log file one line at time...

                    textline = LineInput(fnum)

                    If IsDate(Mid(textline, 1, 19)) Then
                        'Save event date and time for each line...
                        EventDateTime = Mid(textline, 1, 19)
                    End If

                    Do While Convert.ToDateTime(EventDateTime) < Convert.ToDateTime(StartDateTime)
                        'eat up any line that has a datetime < pre-defined start time...
                        textline = LineInput(fnum)
                        If EOF(fnum) Then
                            Exit Do
                        End If
                        If IsDate(Mid(textline, 1, 19)) Then
                            EventDateTime = Mid(textline, 1, 19)
                        End If

                    Loop

                    'Here we are looking for when CLEVIR is initialized so we can determine the software version that is running for this part of
                    'the log file.  This info will allow us to update the version that is running as we go through the file. So, if we encounter
                    'anything abnormal, we can identify the CLEVIR version that was running when this abnormality occurred...
                    If InStr(textline, "Initializing CLEVIR") > 0 Then

                        StartNum = 0
                        AddToStringForVersion = 0

                        If InStr(textline, "CLEVIR (Version") > 0 Then
                            AddToStringForVersion = 16
                            StartNum = InStr(textline, "CLEVIR") + AddToStringForVersion
                        ElseIf InStr(textline, "CLEVIR_INCA_7_") > 0 Then
                            AddToStringForVersion = 25
                            StartNum = InStr(textline, "CLEVIR") + AddToStringForVersion
                        End If

                        If StartNum > AddToStringForVersion Then
                            LatestVersion = Mid(textline, StartNum, InStr(textline, ")") - StartNum)
                            AddToReport("SoftwareVersions.csv", VehicleNumber, LatestVersion)
                        End If

                    End If

                    'In certain cases, we only want to check for particular text in a line for newer software versions.  In the case of Object reference not set, this error was occurring
                    'much more frequently in older software versions, so this is to filter out known behavior prior to the 5.4.1 and newer versions to minimize the number of errors in the log.
                    If InStr(textline, "Object reference not set") > 0 And (InStr(LatestVersion, "5.4.1") > 0 Or InStr(LatestVersion, "5.5") > 0 Or InStr(LatestVersion, "5.6") > 0 Or InStr(LatestVersion, "6.") > 0) Then

                        If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline) = False Then
                            LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline)
                        End If
                    End If

                    'Here we determine the current vehicle number defined in the file.  This will change throughout the file because the same computer is being used in multiple vehicles...
                    If InStr(textline, "\gmcsv") > 0 Then
                        tempstr = Mid(textline, InStr(textline, "\gmcsv") + 6, Len(textline))
                        If InStr(tempstr, "\") > 0 Then
                            myVehicleNumber = Mid(tempstr, 1, InStr(tempstr, "\") - 1)
                        End If
                    End If

                    If InStr(textline, "VehicleNumber has been changed to ") > 0 Then
                        tempstr = Mid(textline, InStr(textline, "changed to") + 11, Len(textline))
                        myVehicleNumber = Mid(tempstr, 1, Len(tempstr) - 1)
                    End If

                    If myVehicleNumber <> VehicleNumber Then
                        If Len(myVehicleNumber) > 0 Then
                            VehicleNumber = myVehicleNumber
                            If ReadVehicleConfigsFile() = True Then
                                If Len(DataUploadPath) = 0 Then
                                    DataUploadPath = "\"
                                End If
                                If VehicleNumber = "UNDEFINED" Then
                                    VehicleNumber = myVehicleNumber
                                End If
                            Else
                                VehicleNumber = myVehicleNumber
                            End If
                        Else
                            MsgBox("VehicleNumber = " & VehicleNumber & " - myVehicleNumber = *" & myVehicleNumber & "*")
                        End If

                    End If

                    'Here we are capturing the experiment that is running so if we have an issue with an INVALID DATA ALERT for example, we will know what experiment
                    'was running when this event occured...
                    If InStr(textline, "StartStopRecord: INCA Workspace") > 0 Or InStr(textline, " - INCA Workspace") > 0 Then
                        myExperiment = Mid(textline, InStr(textline, "INCA Experiment") + 16, Len(textline))
                    End If

                    'Here we are capturing any error that has occured in myBackGroundTasks (CLEVIR main execution loop) and writing this to the errors.csv file...
                    If InStr(UCase(textline), "MYBACKGROUNDTASKS:") > 0 And InStr(UCase(textline), "HEALTHCOUNTER") = 0 And InStr(UCase(textline), "ACTIVE DTC") = 0 Then
                        AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",MY BACKGROUND TASKS ERROR TRAP," & Mid(textline, 20, Len(textline)))
                    End If

                    'Here we are starting various sections which are associated with certain operating windows so that we can flag issues that are occuring during certain
                    'parts of the CLEVIR execution.  The first is after the user logs in and begins the final initialization process prior to the main screen being displayed.
                    'We loop inside this if condition and will exit the loop either when it is completed successfully, or if something interrupted the initialization process,
                    'in which case we will flag what caused the interruption and log it.
                    If InStr(textline, "HandleLogin: Logged in as") > 0 Or InStr(textline, "Initialize: Initializing INCA...") > 0 Then

                        'Grab the start time so we can calculate how long the initilization takes...
                        If IsDate(Mid(textline, 1, 19)) Then
                            InitStartTime = Mid(textline, 1, 19)
                        End If

                        'Here we will loop until something interrupts a successful competion, or if we see the expected text string indicating a successful completion...
                        Do While InStr(textline, "Initialization Complete") = 0 And InStr(textline, "CLEVIR Init End Time") = 0 And InStr(UCase(textline), "USER SWITCHED TO") = 0 And
                            InStr(textline, "ExitApp") = 0 And
                            InStr(textline, "Initializing CLEVIR") = 0 And
                            InStr(textline, "Restarting INCA") = 0 And
                            InStr(textline, "Reinitializing INCA") = 0 And
                            InStr(textline, "Could not open Experiment") = 0 And
                            InStr(textline, "NOT Continuing") = 0

                            'Here we are saving each line of text such that if there is an abnormal exit, we can save the last text string before the abnormal event...
                            If Len(textline) > 22 And IsDate(Mid(textline, 1, 19)) Then
                                SaveTextline = textline
                            End If

                            'Grab the next line in the file...
                            textline = LineInput(fnum)

                            'There are various "sub plots" which will occur during an initialization, so we want to capture abnormalities that happen during these
                            'so we can narrow down what may have happended and put it into the context of the "sub plot"  In the following case, this section wants
                            'to identify any errors related to starting Vehicle Spy...
                            If InStr(textline, "Verifying VehicleSPY setup") > 0 Then

                                'We stay in this loop until an expected successful end to this "sub plot"
                                Do While InStr(textline, "VehicleSpy Measurement Stopped") = 0 And InStr(textline, "In ReadInSignalList") = 0

                                    textline = LineInput(fnum)

                                    'Should we see an abnormality before the successful end, we will save it here and handle logging the issue outside of the loop...
                                    If InStr(textline, "VehicleSpy did not Start.  VehicleSpy Setup Verification failed") > 0 _
                                        Or InStr(textline, "VehicleSpy Setup Verification failed. Could not connect to VSpy") > 0 _
                                        Or InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 _
                                        Or InStr(textline, "Initializing CLEVIR") > 0 _
                                        Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                        SaveStartKey = textline
                                        Exit Do
                                    End If

                                    If EOF(fnum) Then
                                        Exit Do
                                    End If

                                Loop

                                'Here we are adding the specific error text to the errors.csv report (In this case, if we did not exit due to Initializing CLEVIR, which is handled as
                                'a separate case...
                                If InStr(SaveStartKey, "VehicleSpy did not Start.  VehicleSpy Setup Verification failed") > 0 _
                                    Or InStr(SaveStartKey, "VehicleSpy Setup Verification failed. Could not connect to VSpy") > 0 _
                                    Or InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 _
                                    Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                                    EventDateTime = Mid(SaveStartKey, 1, 19)
                                    AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY VERIFICATION ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)))

                                    SaveStartKey = ""
                                End If

                                'In many cases, during initialization, we may see CLEVIR being initialized which indicates that there was some issue that caused the user
                                'to need to re-start CLEVIR.  This indicates an issue that should be looked into.  Also, whenever we see Initializing CLEVIR, we need to capture
                                'the CLEVIR version, so this code repeats itself in various places within this routine...
                                If InStr(SaveStartKey, "Initializing CLEVIR") > 0 Then

                                    StartNum = 0
                                    AddToStringForVersion = 0

                                    If InStr(SaveStartKey, "CLEVIR (Version") > 0 Then
                                        AddToStringForVersion = 16
                                        StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                                    ElseIf InStr(SaveStartKey, "CLEVIR_INCA_7_") > 0 Then
                                        AddToStringForVersion = 25
                                        StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                                    End If

                                    If StartNum > AddToStringForVersion Then
                                        LatestVersion = Mid(SaveStartKey, StartNum, InStr(textline, ")") - StartNum)
                                        AddToReport("SoftwareVersions.csv", VehicleNumber, LatestVersion)
                                    End If

                                    EventDateTime = Mid(SaveStartKey, 1, 19)
                                    AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY VERIFICATION ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)) & " During Initialization")

                                    SaveStartKey = ""

                                    InitialRecordStartTime = Nothing
                                    RecordingStartedTime = Nothing
                                    RecordingStoppedTime = Nothing
                                    StartRecordingRequestedTime = Nothing
                                    StopRecordingRequestedTime = Nothing
                                    InitRecordStartDelayTime = Nothing
                                    RecordStartDelayTime = Nothing
                                    RecordStopToStartDelayTime = Nothing
                                    RecordStopReqToRecordStopDelayTime = Nothing

                                    InitStartTime = Nothing
                                    InitEndTime = Nothing
                                    InitRecordStartDelayTime = Nothing

                                    VspyRecordingStartedTime = Nothing
                                    VspyRecordingStoppedTime = Nothing
                                    VspyStartRecordingRequestedTime = Nothing
                                    VspyStopRecordingRequestedTime = Nothing
                                    VSpyRecordStartDelayTime = Nothing
                                    VspyRecordStopReqToRecordStopDelayTime = Nothing

                                    VspyEnableAltRecReStartAfterRecordStopFlag = False
                                    StopRecordingRequested = False
                                    OperatorManualIntevention = False
                                    RecordingStarted = False
                                    InDIDRead = False

                                    myProcessor = ""
                                    InvalidDataFlagged = False
                                    ProcessorCommFault = False
                                    ProcCommFaultFlagged = False
                                    InvalidData = False

                                End If

                            End If

                            'Here we flag if user continued with no Camera...
                            If InStr(SaveStartKey, "Cannot Communicate with") > 0 And InStr(SaveStartKey, "at Initialization") > 0 And InStr(SaveStartKey, "CAN") = 0 And InStr(SaveStartKey, "HC") = 0 _
                                    And InStr(SaveStartKey, "XETK") = 0 And InStr(SaveStartKey, "IP") = 0 And InStr(SaveStartKey, "IR") = 0 _
                                    And InStr(SaveStartKey, "K1") = 0 And InStr(SaveStartKey, "K2") = 0 And InStr(SaveStartKey, "ACP") = 0 And InStr(SaveStartKey, "FCM") = 0 Then
                                If InStr(textline, "User chose to continue with initialization") > 0 Then
                                    EventDateTime = Mid(SaveStartKey, 1, 19)
                                    AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",CAMERA INIT ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)))

                                    SaveStartKey = ""
                                End If
                            End If

                            'Here we will flag Cannot Communicate with Front at Initialization so we can keep track of the frequency of this situation...
                            If InStr(textline, "Cannot Communicate with") > 0 And InStr(textline, "at Initialization") > 0 Then
                                SaveStartKey = textline
                            End If

                            If EOF(fnum) Then
                                Exit Do
                            End If

                        Loop 'Looping here until we either complete the initialization caused by a user login, or we abnormally exit because something unexpected occurred...

                        'If InStr(textline, "CLEVIR Initialization Complete") > 0 Or InStr(textline, "CLEVIR Init End Time") > 0 Or InStr(UCase(textline), "USER SWITCHED TO") > 0 Then
                        If InStr(textline, "Initialization Complete") > 0 Or InStr(textline, "CLEVIR Init End Time") > 0 Or InStr(UCase(textline), "USER SWITCHED TO") > 0 Then
                            If IsDate(Mid(textline, 1, 19)) Then
                                InitEndTime = Mid(textline, 1, 19)
                            End If
                            InitTimeDelay = InitEndTime.Subtract(InitStartTime)

                            AddToReport("InitDelayTimes.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & Format$(InitStartTime, "MM/dd/yyy HH:mm:ss") & "," & InitTimeDelay.TotalSeconds)

                        Else 'this indicates some sort of abnormal behavior during initialization...
                            InitEndTime = Nothing
                            InitStartTime = Nothing
                            InitTimeDelay = Nothing

                            If IsDate(Mid(textline, 1, 19)) Then
                                EventDateTime = Mid(textline, 1, 19)
                            End If

                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",CLEVIR INITIALIZATION COMPLETE ISSUE," & Mid(textline, 20, Len(textline)) & " - After " & Mid(SaveTextline, 20, Len(SaveTextline)))
                        End If

                    End If 'THIS ENDS THE CHECKS THAT ARE HAPPENING AFTER USER LOGIN OR STARTING FINAL INITIALIZATION...

                    'Add to error report if we see Kill Stopping Thread, which indicates CLEVIR was hung on call to INCA and the user
                    'clicked on the INCA Status window to kill all processes...
                    If InStr(textline, "Kill Stopping Thread") > 0 Then
                        AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",CLEVIR Kill Switch Activated," & Mid(textline, 20, Len(textline)))
                    End If

                    'Here we set initial record start time which is used to calculate time delay between initial start record button press
                    'and when we actually request recording start...
                    If InStr(textline, "START RECORD Button Pressed") > 0 Then
                        InitialRecordStartTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        StartRecordButtonPressed = True
                    End If

                    'Here we set initial record start delay time which eventually ends up in the DelayTimes.csv file...
                    If InStr(textline, "Start Recording Requested") > 0 Then
                        StartRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        If StartRecordButtonPressed = True Then
                            InitRecordStartDelayTime = StartRecordingRequestedTime.Subtract(InitialRecordStartTime)
                            StartRecordButtonPressed = False
                        End If
                    End If

                    'Lots of stuff happens after we see Recording Started.  Here we will write the saved delay values based on
                    'the previous Stop Record Requested time (if applicable)...
                    If InStr(textline, "Recording Started") > 0 And InStr(textline, "INCA") = 0 Then

                        RecordingStarted = True

                        RecordingStartedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        RecordStartDelayTime = RecordingStartedTime.Subtract(StartRecordingRequestedTime)

                        If StopRecordingRequested = True Then

                            'We will only save the time between record stop and the next record start if there has not been
                            'any manual operator intervention during the record cycle...
                            If OperatorManualIntevention = False Then
                                RecordStopToStartDelayTime = RecordingStartedTime.Subtract(StopRecordingRequestedTime)
                            Else
                                OperatorManualIntevention = False
                            End If

                            StopRecordingRequested = False

                        End If

                        AddToReport("DelayTimes.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & Format$(RecordingStartedTime, "MM/dd/yyy HH:mm:ss") & "," & RecordStopToStartDelayTime.TotalSeconds & "," & InitRecordStartDelayTime.TotalSeconds & "," & RecordStartDelayTime.TotalSeconds & "," & RecordStopReqToRecordStopDelayTime.TotalSeconds & "," & VSpyRecordStartDelayTime.TotalSeconds & "," & VspyRecordStopReqToRecordStopDelayTime.TotalSeconds & "," & VspyEnableAltRecReStartAfterRecordStopFlag.ToString & "," & DIDPullHasBeenRun.ToString)

                        'Here we reset everything so we start from scratch at the beginning of a new recording...
                        InitialRecordStartTime = Nothing
                        RecordingStartedTime = Nothing
                        RecordingStoppedTime = Nothing
                        StartRecordingRequestedTime = Nothing
                        StopRecordingRequestedTime = Nothing
                        InitRecordStartDelayTime = Nothing
                        RecordStartDelayTime = Nothing
                        RecordStopToStartDelayTime = Nothing
                        RecordStopReqToRecordStopDelayTime = Nothing

                        InitStartTime = Nothing
                        InitEndTime = Nothing
                        InitRecordStartDelayTime = Nothing

                        VspyRecordingStartedTime = Nothing
                        VspyRecordingStoppedTime = Nothing
                        VspyStartRecordingRequestedTime = Nothing
                        VspyStopRecordingRequestedTime = Nothing
                        VSpyRecordStartDelayTime = Nothing
                        VspyRecordStopReqToRecordStopDelayTime = Nothing

                        'If we have flagged any invalid data or processor fault, we will save the most recent zipped data file so that we can check it to see if the
                        'invalid data was flagged correctly.  By checking the file in XTool, we can see if the data is valid or not to verify that we are capturing the
                        'invalid data event properly.  Here is where we add the file name to the loglist which we can display in Listbox2 on the DisplayLogFile window...
                        If InvalidDataFlagged = True Or ProcessorCommFault = True Or ProcCommFaultFlagged = True Or InvalidData = True Then

                            textline = LineInput(fnum)

                            'ZipMyFilesNEW: Zipping C:\HB\Data\gmcsv6LAV4654\20200724_050341_Demo\20200724_050341_Demo_6LAV4654_01.mf4 to C:\HB\Data\gmcsv6LAV4654\20200724_050341_Demo\20200724_050341_Demo_6LAV4654_01.zip
                            'C:\HB\Data\gmcsv6NDN775\20200814_172640_Demo\20200814_172640_Demo_6NDN775_01.zip

                            If InStr(textline, "ZipMyFilesNEW called") > 0 Then
                                textline = LineInput(fnum)
                                If InStr(textline, "ZipMyFilesNEW: Zipping") > 0 Or InStr(textline, " - Zipping") > 0 Then
                                    StrStart = InStr(textline, "\g") + 1
                                    strLen = InStr(textline, ".mf4 to ") - StrStart

                                    If strLen > 0 And StrStart > 0 Then

                                        If LogList.Contains(VehicleNumber & " - " & LatestVersion & " - " & mypathprefix & DataUploadPath & Mid(textline, StrStart, strLen) & ".zip") = False Then
                                            LogList.Add(VehicleNumber & " - " & LatestVersion & " - " & mypathprefix & DataUploadPath & Mid(textline, StrStart, strLen) & ".zip")
                                            FileCounter += 1
                                        End If

                                    Else
                                        HandleUserMessageLogging("GMRC", "ParseCLEVIRLogFile: strlen = " & strLen & " strStart = " & StrStart)
                                    End If

                                End If
                            End If

                            myProcessor = ""
                            InvalidDataFlagged = False
                            ProcessorCommFault = False
                            ProcCommFaultFlagged = False
                            InvalidData = False

                        End If

                        'If we are recording, we look for things that indicate we are no longer recording here, or things that
                        'might have taken us out of recording state abnormally,such as recording stopped in INCA...

                        'We also capture various pieces of info based on what we find on each subsequent log entry after recording started
                        'is indicated...
                        Do While InStr(textline, "has expired StopAndStartRecording Called") = 0 And
                            InStr(textline, "STOP RECORD Button Pressed") = 0 And
                            InStr(textline, "STOP MEASUREMENT Button Pressed") = 0 And
                            InStr(textline, "ExitApp () called...") = 0 And
                            InStr(textline, "Initializing CLEVIR") = 0 And
                            InStr(textline, "START RECORD Button Pressed") = 0 And
                            InStr(textline, "Exit Complete") = 0

                            If Len(textline) > 22 And IsDate(Mid(textline, 1, 19)) Then
                                SaveTextline = textline
                            End If

                            textline = LineInput(fnum)

                            'NAN ISSUE IS ACTIVE

                            If ((InStr(textline, "NAN ISSUE IS ACTIVE") > 0)) Then

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline) = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline)
                                End If

                            End If

                            If ((InStr(textline, "Object reference not set") > 0) Or (InStr(textline, "MyBackgroundTasks") > 0)) And (InStr(LatestVersion, "5.4.1") > 0 Or InStr(LatestVersion, "5.5") > 0 Or InStr(LatestVersion, "5.6") > 0 Or InStr(LatestVersion, "6.") > 0) Then
                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline) = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline)
                                End If

                            End If

                            'HCF - VaRBSR_Cnt_BackgroundLoop[CeTSKR_e_CPU3] - Not Updating

                            'Cannot Communicate with
                            '07/18/2020 15:47:15 - GetDeviceStatus: Cannot Communicate with XETK:1
                            '07/18/2020 15:47:26 - GetDeviceStatus: Communication with XETK:1 established

                            If InStr(textline, "Cannot Communicate with") > 0 And InStr(UCase(textline), "CALCDEV") = 0 And InStr(UCase(textline), "CAN-MONITORING") = 0 And InStr(UCase(textline), "FRONT") = 0 Then
                                StrStart = InStr(textline, "Cannot Communicate with") + 24
                                strLen = Len(textline) - StrStart

                                If strLen > 0 Then
                                    myProcessor = Mid(textline, StrStart, strLen)
                                    ProcessorCommFault = True

                                Else
                                    HandleUserMessageLogging("GMRC", "ParseCLEVIRLogFile: strLen = " & strLen)
                                End If

                            End If

                            If InStr(textline, "Communication with") > 0 And InStr(textline, "established") > 0 And ProcessorCommFault = True Then
                                myProcessor = ""
                                ProcessorCommFault = False
                            End If

                            If (InStr(textline, "BackgroundLoop") > 0 And InStr(textline, "- Not Updating") > 0) Then
                                StrStart = InStr(textline, " - ") + 3
                                strLen = (InStr(textline, " - Va")) - StrStart

                                If strLen > 0 Then

                                    InvalidData = True
                                    myProcessor = Mid(textline, StrStart, strLen)

                                Else
                                    HandleUserMessageLogging("GMRC", "ParseCLEVIRLogFile: strLen = " & strLen)
                                End If

                            End If

                            If Len(myProcessor) > 0 Then
                                If InStr(textline, "BackgroundLoop") > 0 And InStr(textline, myProcessor) > 0 And InStr(textline, " - Resumed Updating") > 0 Then
                                    myProcessor = ""
                                    InvalidData = False
                                End If
                            End If

                            If (InStr(textline, "UpdateGONOGOLabelColor: INCA COMM Status GO/NOGO is ----------------- COLOR [RED]") > 0) Then
                                InvalidData = True
                            End If

                            If (InStr(textline, "UpdateGONOGOLabelColor: INCA COMM Status GO/NOGO is ----------------- COLOR [GREEN]") > 0) Then
                                InvalidData = False
                            End If

                            If InStr(textline, "INVALID DATA") > 0 Then
                                InvalidDataFlagged = True

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "INVALID DATA") = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "INVALID DATA")
                                    DataAlertCount += 1
                                End If
                            End If

                            If InStr(textline, "INVALID VIDEO") > 0 Then
                                InvalidVideoFlagged = True

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "INVALID VIDEO") = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "INVALID VIDEO")
                                    VideoAlertCount += 1
                                End If

                            End If

                            If InStr(UCase(textline), "PROCESSOR COMM") > 0 Then
                                ProcCommFaultFlagged = True

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "PROC COMM") = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "PROC COMM")
                                    ProcessorCommAlertCount += 1

                                End If

                            End If

                            'Here we handle tracking vspy recording status and time delays...
                            If InStr(textline, "Starting Replay Block Playback") > 0 Then

                                VspyStartRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))

                                Do While InStr(textline, "Replay Block Playback Started") = 0

                                    textline = LineInput(fnum)

                                    If InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                        SaveStartKey = textline
                                        Exit Do
                                    End If

                                    If InStr(textline, "Initializing CLEVIR") > 0 Then
                                        Exit Do
                                    End If

                                    If EOF(fnum) Then
                                        Exit Do
                                    End If

                                Loop

                                If InStr(textline, "Replay Block Playback Started") > 0 Then
                                    VspyRecordingStartedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                                    VSpyRecordStartDelayTime = VspyRecordingStartedTime.Subtract(VspyStartRecordingRequestedTime)
                                    If RecordingStarted = True Then
                                        VspyEnableAltRecReStartAfterRecordStopFlag = False
                                    Else
                                        VspyEnableAltRecReStartAfterRecordStopFlag = True
                                    End If

                                End If

                                If InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                                    EventDateTime = Mid(SaveStartKey, 1, 19)
                                    AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY REPLAY BLOCK START ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)))

                                    SaveStartKey = ""
                                End If

                            End If

                            'Here we are flagging various things that may have happened during recording, based on what log info we have seen 
                            'before recording is stopped(whatever the reason, normally Or abnormally)...

                            If InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If InStr(SaveStartKey, "VIDEO_CAMERA_TIMECODE - Not Updating") > 0 Then
                                If InStr(textline, "VIDEO_CAMERA_TIMECODE - Resumed Updating") > 0 Then
                                    SaveStartKey = ""
                                End If
                            End If

                            If InStr(textline, "VIDEO_CAMERA_TIMECODE - Not Updating") > 0 Then
                                SaveStartKey = textline
                            End If

                            If InStr(textline, "CheckForINCAButtonPresses:") > 0 Then
                                If Len(SaveTextline) > 0 Then

                                    If IsDate(Mid(SaveTextline, 1, 19)) Then
                                        CLEVIREventDateTime = Convert.ToDateTime(Mid(SaveTextline, 1, 19))

                                        If IsDate(Mid(textline, 1, 19)) Then
                                            INCAEventDateTime = Convert.ToDateTime(Mid(textline, 1, 19))

                                            SaveStartKey = textline
                                            Exit Do

                                        End If

                                    End If

                                End If
                            End If

                            If InStr(textline, "ExitApp () called...") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If InStr(textline, "Initializing CLEVIR") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If EOF(fnum) Then
                                Exit Do
                            End If

                        Loop

                        'HERE

                        If InStr(textline, "has expired StopAndStartRecording Called") > 0 Then
                            If (InvalidData = True Or InvalidDataFlagged = True Or ProcCommFaultFlagged = True Or ProcessorCommFault = True) Then

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - InvalidData = " & InvalidData.ToString & " - Flagged = " & InvalidDataFlagged.ToString & " - ProcComm = " & ProcessorCommFault.ToString & " - Flagged = " & ProcCommFaultFlagged.ToString) = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - InvalidData = " & InvalidData.ToString & " - Flagged = " & InvalidDataFlagged.ToString & " - ProcComm = " & ProcessorCommFault.ToString & " - Flagged = " & ProcCommFaultFlagged.ToString)
                                End If
                            End If
                        End If

                        If InStr(textline, "STOP RECORD Button Pressed") > 0 Or InStr(textline, "STOP MEASUREMENT Button Pressed") > 0 Then
                            If (InvalidData = True Or InvalidDataFlagged = True Or ProcCommFaultFlagged = True Or ProcessorCommFault = True) Then

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - InvalidData = " & InvalidData.ToString & " - Flagged = " & InvalidDataFlagged.ToString & " - ProcComm = " & ProcessorCommFault.ToString & " - Flagged = " & ProcCommFaultFlagged.ToString & " - STOP RECORD / STOP MEASUREMENT") = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - InvalidData = " & InvalidData.ToString & " - Flagged = " & InvalidDataFlagged.ToString & " - ProcComm = " & ProcessorCommFault.ToString & " - Flagged = " & ProcCommFaultFlagged.ToString & " - STOP RECORD / STOP MEASUREMENT")
                                End If

                                myProcessor = ""
                                InvalidDataFlagged = False
                                ProcessorCommFault = False
                                ProcCommFaultFlagged = False
                                InvalidData = False
                            End If
                        End If

                        If InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY REPLAY BLOCK START ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)))

                            SaveStartKey = ""
                        End If

                        If InStr(SaveStartKey, "Initializing CLEVIR") > 0 Then

                            StartNum = 0
                            AddToStringForVersion = 0

                            If InStr(SaveStartKey, "CLEVIR (Version") > 0 Then
                                AddToStringForVersion = 16
                                StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                            ElseIf InStr(SaveStartKey, "CLEVIR_INCA_7_") > 0 Then
                                AddToStringForVersion = 25
                                StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                            End If

                            If StartNum > AddToStringForVersion Then
                                LatestVersion = Mid(SaveStartKey, StartNum, InStr(textline, ")") - StartNum)
                                AddToReport("SoftwareVersions.csv", VehicleNumber, LatestVersion)
                            End If

                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",CLEVIR Hung up during Recording," & Mid(SaveStartKey, 20, Len(SaveStartKey)) & " After " & SaveTextline)
                            SaveStartKey = ""

                            InitialRecordStartTime = Nothing
                            RecordingStartedTime = Nothing
                            RecordingStoppedTime = Nothing
                            StartRecordingRequestedTime = Nothing
                            StopRecordingRequestedTime = Nothing
                            InitRecordStartDelayTime = Nothing
                            RecordStartDelayTime = Nothing
                            RecordStopToStartDelayTime = Nothing
                            RecordStopReqToRecordStopDelayTime = Nothing

                            InitStartTime = Nothing
                            InitEndTime = Nothing
                            InitRecordStartDelayTime = Nothing

                            VspyRecordingStartedTime = Nothing
                            VspyRecordingStoppedTime = Nothing
                            VspyStartRecordingRequestedTime = Nothing
                            VspyStopRecordingRequestedTime = Nothing
                            VSpyRecordStartDelayTime = Nothing
                            VspyRecordStopReqToRecordStopDelayTime = Nothing

                            VspyEnableAltRecReStartAfterRecordStopFlag = False
                            StopRecordingRequested = False
                            OperatorManualIntevention = False
                            RecordingStarted = False
                            InDIDRead = False

                            myProcessor = ""
                            InvalidDataFlagged = False
                            ProcessorCommFault = False
                            ProcCommFaultFlagged = False
                            InvalidData = False

                        End If

                        If InStr(SaveStartKey, "CheckForINCAButtonPresses:") > 0 Then
                            RecordingStarted = False

                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",QUESTIONABLE USER INTERACTION ISSUE WHILE RECORDING," & Mid(SaveStartKey, 20, Len(SaveStartKey)) & " After " & SaveTextline)

                            SaveStartKey = ""

                            OperatorManualIntevention = True
                            RecordStopToStartDelayTime = Nothing
                            RecordStopReqToRecordStopDelayTime = Nothing

                        End If

                        If InStr(SaveStartKey, "ExitApp () called...") > 0 And InStr(SaveTextline, "WriteMileage") = 0 And InStr(SaveTextline, "Cannot Communicate") = 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",QUESTIONABLE APP EXIT ISSUE WHILE RECORDING," & Mid(SaveStartKey, 20, Len(SaveStartKey)) & " After " & SaveTextline)

                            SaveStartKey = ""
                        End If

                        If InStr(SaveStartKey, "VIDEO_CAMERA_TIMECODE - Not Updating While Recording ") > 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)

                            SaveStartKey = ""
                        End If

                    End If 'If InStr(textline, "Recording Started") > 0 And InStr(textline, "INCA") = 0 Then

                    If InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                        EventDateTime = Mid(textline, 1, 19)
                        AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY REPLAY BLOCK START ISSUE," & Mid(textline, 20, Len(textline)))

                        SaveStartKey = ""
                    End If

                    If InStr(textline, "STOP RECORD Button Pressed") > 0 Or InStr(textline, "START RECORD Button Pressed") > 0 Then
                        OperatorManualIntevention = True
                        RecordStopToStartDelayTime = Nothing
                    End If

                    If InStr(textline, "Stop Recording Requested") > 0 Then
                        StopRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        StopRecordingRequested = True
                        RecordingStarted = False
                    End If

                    If InStr(textline, "Recording Stopped") > 0 And InStr(textline, "INCA") = 0 Then

                        RecordingStoppedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        RecordStopReqToRecordStopDelayTime = RecordingStoppedTime.Subtract(StopRecordingRequestedTime)

                    End If

                    If InStr(textline, "Stopping VehicleSpy Replay Block Playback") > 0 Then
                        VspyStopRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                    End If

                    If InStr(textline, "VehicleSpy Stopped") > 0 Then
                        VspyRecordingStoppedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        VspyRecordStopReqToRecordStopDelayTime = VspyRecordingStoppedTime.Subtract(VspyStopRecordingRequestedTime)
                    End If

                    If InStr(textline, "Verifying VehicleSPY setup") > 0 Then

                        Do While InStr(textline, "VehicleSpy Measurement Stopped") = 0 And InStr(textline, "In ReadInSignalList") = 0

                            textline = LineInput(fnum)

                            If InStr(textline, "VehicleSpy did not Start.  VehicleSpy Setup Verification failed") > 0 _
                                Or InStr(textline, "VehicleSpy Setup Verification failed. Could not connect to VSpy") > 0 _
                                Or InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 _
                                Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If EOF(fnum) Then
                                Exit Do
                            End If

                        Loop

                        If InStr(SaveStartKey, "VehicleSpy did not Start.  VehicleSpy Setup Verification failed") > 0 _
                            Or InStr(SaveStartKey, "VehicleSpy Setup Verification failed. Could not connect to VSpy") > 0 _
                            Or InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 _
                            Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY VERIFICATION ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)))

                            SaveStartKey = ""
                        End If

                    End If

                    If InStr(textline, "Preparing to read DID Information, please wait...") > 0 Then
                        InDIDRead = True
                    End If

                    If InStr(textline, "Starting Replay Block Playback") > 0 And InDIDRead = False Then

                        VspyStartRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))

                        Do While InStr(textline, "Replay Block Playback Started") = 0 And
                                InStr(textline, "STOP RECORD Button Pressed") = 0 And
                                InStr(textline, "ZipMyFilesNEW called") = 0 And
                                InStr(textline, "Initializing CLEVIR") = 0 And
                                InStr(textline, "has expired StopAndStartRecording Called") = 0

                            textline = LineInput(fnum)

                            If InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If EOF(fnum) Then
                                Exit Do
                            End If

                        Loop

                        If InStr(textline, "Replay Block Playback Started") > 0 Then
                            VspyRecordingStartedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                            VSpyRecordStartDelayTime = VspyRecordingStartedTime.Subtract(VspyStartRecordingRequestedTime)
                            If RecordingStarted = True Then
                                VspyEnableAltRecReStartAfterRecordStopFlag = False
                            Else
                                VspyEnableAltRecReStartAfterRecordStopFlag = True
                            End If

                        End If

                        If InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY READ DID REPLAY BLOCK START ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)))

                            SaveStartKey = ""
                        End If

                        If InStr(textline, "Initializing CLEVIR") > 0 Then

                            StartNum = 0
                            AddToStringForVersion = 0

                            If InStr(SaveStartKey, "CLEVIR (Version") > 0 Then
                                AddToStringForVersion = 16
                                StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                            ElseIf InStr(SaveStartKey, "CLEVIR_INCA_7_") > 0 Then
                                AddToStringForVersion = 25
                                StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                            End If

                            If StartNum > AddToStringForVersion Then
                                LatestVersion = Mid(textline, StartNum, InStr(textline, ")") - StartNum)
                                AddToReport("SoftwareVersions.csv", VehicleNumber, LatestVersion)
                            End If

                            EventDateTime = Mid(textline, 1, 19)

                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY READ DID REPLAY BLOCK START ISSUE," & Mid(textline, 20, Len(textline)))

                            SaveStartKey = ""

                            InitialRecordStartTime = Nothing
                            RecordingStartedTime = Nothing
                            RecordingStoppedTime = Nothing
                            StartRecordingRequestedTime = Nothing
                            StopRecordingRequestedTime = Nothing
                            InitRecordStartDelayTime = Nothing
                            RecordStartDelayTime = Nothing
                            RecordStopToStartDelayTime = Nothing
                            RecordStopReqToRecordStopDelayTime = Nothing

                            InitStartTime = Nothing
                            InitEndTime = Nothing
                            InitRecordStartDelayTime = Nothing

                            VspyRecordingStartedTime = Nothing
                            VspyRecordingStoppedTime = Nothing
                            VspyStartRecordingRequestedTime = Nothing
                            VspyStopRecordingRequestedTime = Nothing
                            VSpyRecordStartDelayTime = Nothing
                            VspyRecordStopReqToRecordStopDelayTime = Nothing

                            VspyEnableAltRecReStartAfterRecordStopFlag = False
                            StopRecordingRequested = False
                            OperatorManualIntevention = False
                            RecordingStarted = False
                            InDIDRead = False

                            myProcessor = ""
                            InvalidDataFlagged = False
                            ProcessorCommFault = False
                            ProcCommFaultFlagged = False
                            InvalidData = False

                        End If

                    End If

                    If InStr(textline, "File deleted") > 0 And InDIDRead = True Then
                        InDIDRead = False
                        DIDPullHasBeenRun = True
                    End If


                    If Len(textline) > 22 And IsDate(Mid(textline, 1, 19)) Then
                        SaveTextline = textline
                    End If

                Loop

                FileClose(fnum)

                If Len(LatestVersion) > 0 Then
                    If Len(VehicleNumber) = 0 Then
                        MsgBox("VehicleNumber = " & VehicleNumber)
                    End If
                    AddToReport("LastUploadTime.csv", VehicleNumber, LatestVersion & "," & System.IO.File.GetLastWriteTime(filename))
                Else
                    AddToReport("LastUploadTime.csv", VehicleNumber, "UNDF" & "," & System.IO.File.GetLastWriteTime(filename))
                End If

            End If

            If CheckBox1.Checked = True Then

                DisplayLogFile.ListBox2.SelectedIndex = DisplayLogFile.ListBox2.Items.Count - 1
                DisplayLogFile.ListBox2.Refresh()

                DisplayLogFile.Label2.Text = FileCounter
                DisplayLogFile.Label2.Refresh()

                DisplayLogFile.Label3.Text = DataAlertCount
                DisplayLogFile.Label3.Refresh()

                DisplayLogFile.Label5.Text = VideoAlertCount
                DisplayLogFile.Label5.Refresh()

                DisplayLogFile.Label7.Text = ProcessorCommAlertCount
                DisplayLogFile.Label7.Refresh()

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ParseCLEVIRLogFile: " & ex.Message, DisplayMsgBox)
        End Try

    End Sub

    Private Sub ParseCLEVIRLogFileNEW(ByVal filename As String)

        'This routine is called by CopyPCBasedLogFilesFromShareToLocal.  This is where the contents of the log files are parsed to determine if abnormal or undesired behavior is indicated from the
        'contents of the log file.  These abnormal events are categorized and the information is put into the Errors.csv file.  Also, calculations for delay times and
        'initializaiton times are made here and this information is saved to the appropriate report files...

        Dim fnum As Integer
        Dim LatestVersion As String = ""
        Dim textline As String
        Dim StartNum As Integer
        Dim SaveStartKey As String = ""
        Dim EndKey As String = ""

        Dim InitialRecordStartTime As DateTime = Nothing

        Dim RecordingStartedTime As DateTime = Nothing
        Dim RecordingStoppedTime As DateTime = Nothing
        Dim StartRecordingRequestedTime As DateTime = Nothing
        Dim StopRecordingRequestedTime As DateTime = Nothing

        Dim InitRecordStartDelayTime As TimeSpan = Nothing

        Dim RecordStartDelayTime As TimeSpan = Nothing
        Dim RecordStopToStartDelayTime As TimeSpan = Nothing
        Dim RecordStopReqToRecordStopDelayTime As TimeSpan = Nothing

        Dim VspyRecordingStartedTime As DateTime = Nothing
        Dim VspyRecordingStoppedTime As DateTime = Nothing
        Dim VspyStartRecordingRequestedTime As DateTime = Nothing
        Dim VspyStopRecordingRequestedTime As DateTime = Nothing

        Dim VSpyRecordStartDelayTime As TimeSpan = Nothing
        Dim VspyRecordStopReqToRecordStopDelayTime As TimeSpan = Nothing

        Dim VspyEnableAltRecReStartAfterRecordStopFlag As Boolean

        Dim InitStartTime As DateTime
        Dim InitEndTime As DateTime
        Dim InitTimeDelay As TimeSpan

        Dim RecordingStarted As Boolean

        Dim StartRecordButtonPressed As Boolean
        Dim StopRecordingRequested As Boolean

        Dim OperatorManualIntevention As Boolean

        Dim InDIDRead As Boolean
        Dim DIDPullHasBeenRun As Boolean

        Dim SaveTextline As String = ""

        Dim INCAEventDateTime As DateTime
        Dim CLEVIREventDateTime As DateTime

        Dim ExperimentName As String = ""

        Dim myExperiment As String = ""
        Dim myProcessor As String = ""
        Dim InvalidDataFlagged As Boolean
        Dim InvalidVideoFlagged As Boolean
        Dim ProcessorCommFault As Boolean
        Dim ProcCommFaultFlagged As Boolean
        Dim InvalidData As Boolean

        Dim StrStart As Integer
        Dim strLen As Integer

        Dim tempstr As String
        Dim myVehicleNumber As String = VehicleNumber

        Dim temparray() As String
        Dim ComputerHostName As String

        Dim AddToStringForVersion As Integer

        Static FileCounter As Integer
        Static DataAlertCount As Integer '3
        Static VideoAlertCount As Integer '5
        Static ProcessorCommAlertCount As Integer '7

        Try

            If CheckBox1.Checked = True Then

                If Not DisplayLogFile.Visible = True Then
                    DisplayLogFile.Text = "Display Log File Status Info"
                    DisplayLogFile.ListBox2.BringToFront()
                    DisplayLogFile.ListBox1.SendToBack()
                    DisplayLogFile.Show()
                    DisplayLogFile.Refresh()
                End If

            End If

            'W:\CSAV2 Tools\CLEVIR\Development\PC_HostNameLogFiles\CAMKHWNCAT769HR\GM_ResidentClient.log

            temparray = Split(filename, "\")

            ComputerHostName = temparray(5)

            fnum = FreeFile()

            If File.Exists(filename) Then

                FileOpen(fnum, filename, OpenMode.Input)

                Do While Not EOF(fnum)

                    'Start going thru log file one line at time...

                    textline = LineInput(fnum)

                    If IsDate(Mid(textline, 1, 19)) Then
                        'Save event date and time for each line...
                        EventDateTime = Mid(textline, 1, 19)
                    End If

                    Do While Convert.ToDateTime(EventDateTime) < Convert.ToDateTime(StartDateTime)
                        'eat up any line that has a datetime < pre-defined start time...
                        textline = LineInput(fnum)
                        If EOF(fnum) Then
                            Exit Do
                        End If
                        If IsDate(Mid(textline, 1, 19)) Then
                            EventDateTime = Mid(textline, 1, 19)
                        End If

                    Loop

                    'Here we are looking for when CLEVIR is initialized so we can determine the software version that is running for this part of
                    'the log file.  This info will allow us to update the version that is running as we go through the file. So, if we encounter
                    'anything abnormal, we can identify the CLEVIR version that was running when this abnormality occurred...
                    If InStr(textline, "Initializing CLEVIR") > 0 Then

                        StartNum = 0
                        AddToStringForVersion = 0

                        If InStr(textline, "CLEVIR (Version") > 0 Then
                            AddToStringForVersion = 16
                            StartNum = InStr(textline, "CLEVIR") + AddToStringForVersion
                        ElseIf InStr(textline, "CLEVIR_INCA_7_") > 0 Then
                            AddToStringForVersion = 25
                            StartNum = InStr(textline, "CLEVIR") + AddToStringForVersion
                        End If

                        If StartNum > AddToStringForVersion Then
                            LatestVersion = Mid(textline, StartNum, InStr(textline, ")") - StartNum)

                            'We pass VehicleNumber to AddToReport for consistency, but we don't use VehicleNumber in SoftwareVersions.csv file...
                            AddToReport("SoftwareVersions.csv", VehicleNumber, LatestVersion, "HostnameBasedReports\")
                        End If

                    End If

                    'In certain cases, we only want to check for particular text in a line for newer software versions.  In the case of Object reference not set, this error was occurring
                    'much more frequently in older software versions, so this is to filter out known behavior prior to the 5.4.1 and newer versions to minimize the number of errors in the log.
                    If InStr(textline, "Object reference not set") > 0 And (InStr(LatestVersion, "5.4.1") > 0 Or InStr(LatestVersion, "5.5") > 0 Or InStr(LatestVersion, "5.6") > 0 Or InStr(LatestVersion, "6.") > 0) Then

                        If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline) = False Then
                            LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline)
                        End If
                    End If

                    'Here we determine the current vehicle number defined in the file.  This will change throughout the file because the same computer is being used in multiple vehicles...
                    If InStr(textline, "\gmcsv") > 0 And InStr(textline, "ERROR") = 0 Then
                        tempstr = Mid(textline, InStr(textline, "\gmcsv") + 6, Len(textline))
                        If InStr(tempstr, "\") > 0 Then
                            myVehicleNumber = Mid(tempstr, 1, InStr(tempstr, "\") - 1)
                        End If
                    End If

                    'CLEVIR has created a new workspace for vehicle
                    If InStr(textline, "CLEVIR has created a new workspace for vehicle ") > 0 Then
                        tempstr = Mid(textline, InStr(textline, "CLEVIR has created a new workspace for vehicle") + 47, Len(textline))
                        temparray = Split(tempstr, " ")
                        tempstr = temparray(0)
                        'If InStr(tempstr, "\") > 0 Then
                        myVehicleNumber = tempstr
                        'End If
                    End If

                    'Vehicle Number changed from 6LDN4666 to 6Y87RM09

                    'please make sure that the

                    If InStr(textline, "Vehicle Number changed from ") > 0 Then
                        tempstr = Mid(textline, InStr(textline, "Vehicle Number changed from "), Len(textline))
                        temparray = Split(tempstr, " ")
                        tempstr = temparray(6)
                        myVehicleNumber = tempstr
                    End If

                    If InStr(textline, "VehicleNumber has been changed to ") > 0 Then
                        tempstr = Mid(textline, InStr(textline, "changed to") + 11, Len(textline))
                        myVehicleNumber = Mid(tempstr, 1, Len(tempstr) - 1)
                    End If

                    If myVehicleNumber <> VehicleNumber Then
                        If Len(myVehicleNumber) > 0 Then
                            VehicleNumber = myVehicleNumber
                            If ReadVehicleConfigsFile() = True Then
                                If Len(DataUploadPath) = 0 Then
                                    DataUploadPath = "\"
                                End If
                                If VehicleNumber = "UNDEFINED" Then
                                    VehicleNumber = myVehicleNumber
                                End If
                            Else
                                VehicleNumber = myVehicleNumber
                            End If
                        Else
                            MsgBox("VehicleNumber = " & VehicleNumber & " - myVehicleNumber = *" & myVehicleNumber & "*")
                        End If

                    End If

                    'Here we are capturing the experiment that is running so if we have an issue with an INVALID DATA ALERT for example, we will know what experiment
                    'was running when this event occured...
                    If InStr(textline, "StartStopRecord: INCA Workspace") > 0 Or InStr(textline, " - INCA Workspace") > 0 Then
                        myExperiment = Mid(textline, InStr(textline, "INCA Experiment") + 16, Len(textline))
                    End If

                    'Here we are capturing any error that has occured in myBackGroundTasks (CLEVIR main execution loop) and writing this to the errors.csv file...
                    If InStr(UCase(textline), "MYBACKGROUNDTASKS:") > 0 And InStr(UCase(textline), "HEALTHCOUNTER") = 0 And InStr(UCase(textline), "ACTIVE DTC") = 0 Then
                        AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",MY BACKGROUND TASKS ERROR TRAP," & Mid(textline, 20, Len(textline)), "HostnameBasedReports\", ComputerHostName)
                    End If

                    'Here we are starting various sections which are associated with certain operating windows so that we can flag issues that are occuring during certain
                    'parts of the CLEVIR execution.  The first is after the user logs in and begins the final initialization process prior to the main screen being displayed.
                    'We loop inside this if condition and will exit the loop either when it is completed successfully, or if something interrupted the initialization process,
                    'in which case we will flag what caused the interruption and log it.
                    If InStr(textline, "HandleLogin: Logged in as") > 0 Or InStr(textline, "Initialize: Initializing INCA...") > 0 Then

                        'Grab the start time so we can calculate how long the initilization takes...
                        If IsDate(Mid(textline, 1, 19)) Then
                            InitStartTime = Mid(textline, 1, 19)
                        End If

                        'Here we will loop until something interrupts a successful competion, or if we see the expected text string indicating a successful completion...
                        Do While InStr(textline, "Initialization Complete") = 0 And InStr(textline, "CLEVIR Init End Time") = 0 And InStr(UCase(textline), "USER SWITCHED TO") = 0 And
                            InStr(textline, "ExitApp") = 0 And
                            InStr(textline, "Initializing CLEVIR") = 0 And
                            InStr(textline, "Restarting INCA") = 0 And
                            InStr(textline, "Reinitializing INCA") = 0 And
                            InStr(textline, "Could not open Experiment") = 0 And
                            InStr(textline, "NOT Continuing") = 0

                            'Here we are saving each line of text such that if there is an abnormal exit, we can save the last text string before the abnormal event...
                            If Len(textline) > 22 And IsDate(Mid(textline, 1, 19)) Then
                                SaveTextline = textline
                            End If

                            'Grab the next line in the file...
                            textline = LineInput(fnum)

                            'There are various "sub plots" which will occur during an initialization, so we want to capture abnormalities that happen during these
                            'so we can narrow down what may have happended and put it into the context of the "sub plot"  In the following case, this section wants
                            'to identify any errors related to starting Vehicle Spy...
                            If InStr(textline, "Verifying VehicleSPY setup") > 0 Then

                                'We stay in this loop until an expected successful end to this "sub plot"
                                Do While InStr(textline, "VehicleSpy Measurement Stopped") = 0 And InStr(textline, "In ReadInSignalList") = 0

                                    textline = LineInput(fnum)

                                    'Should we see an abnormality before the successful end, we will save it here and handle logging the issue outside of the loop...
                                    If InStr(textline, "VehicleSpy did not Start.  VehicleSpy Setup Verification failed") > 0 _
                                        Or InStr(textline, "VehicleSpy Setup Verification failed. Could not connect to VSpy") > 0 _
                                        Or InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 _
                                        Or InStr(textline, "Initializing CLEVIR") > 0 _
                                        Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                        SaveStartKey = textline
                                        Exit Do
                                    End If

                                    If EOF(fnum) Then
                                        Exit Do
                                    End If

                                Loop

                                'Here we are adding the specific error text to the errors.csv report (In this case, if we did not exit due to Initializing CLEVIR, which is handled as
                                'a separate case...
                                If InStr(SaveStartKey, "VehicleSpy did not Start.  VehicleSpy Setup Verification failed") > 0 _
                                    Or InStr(SaveStartKey, "VehicleSpy Setup Verification failed. Could not connect to VSpy") > 0 _
                                    Or InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 _
                                    Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                                    EventDateTime = Mid(SaveStartKey, 1, 19)
                                    AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY VERIFICATION ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)), "HostnameBasedReports\", ComputerHostName)

                                    SaveStartKey = ""
                                End If

                                'In many cases, during initialization, we may see CLEVIR being initialized which indicates that there was some issue that caused the user
                                'to need to re-start CLEVIR.  This indicates an issue that should be looked into.  Also, whenever we see Initializing CLEVIR, we need to capture
                                'the CLEVIR version, so this code repeats itself in various places within this routine...
                                If InStr(SaveStartKey, "Initializing CLEVIR") > 0 Then

                                    StartNum = 0
                                    AddToStringForVersion = 0

                                    If InStr(SaveStartKey, "CLEVIR (Version") > 0 Then
                                        AddToStringForVersion = 16
                                        StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                                    ElseIf InStr(SaveStartKey, "CLEVIR_INCA_7_") > 0 Then
                                        AddToStringForVersion = 25
                                        StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                                    End If

                                    If StartNum > AddToStringForVersion Then
                                        LatestVersion = Mid(SaveStartKey, StartNum, InStr(textline, ")") - StartNum)
                                        'We pass VehicleNumber to AddToReport for consistency, but we don't use VehicleNumber in SoftwareVersions.csv file...
                                        AddToReport("SoftwareVersions.csv", VehicleNumber, LatestVersion, "HostnameBasedReports\")
                                    End If

                                    EventDateTime = Mid(SaveStartKey, 1, 19)
                                    AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY VERIFICATION ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)) & " During Initialization", "HostnameBasedReports\", ComputerHostName)

                                    SaveStartKey = ""

                                    InitialRecordStartTime = Nothing
                                    RecordingStartedTime = Nothing
                                    RecordingStoppedTime = Nothing
                                    StartRecordingRequestedTime = Nothing
                                    StopRecordingRequestedTime = Nothing
                                    InitRecordStartDelayTime = Nothing
                                    RecordStartDelayTime = Nothing
                                    RecordStopToStartDelayTime = Nothing
                                    RecordStopReqToRecordStopDelayTime = Nothing

                                    InitStartTime = Nothing
                                    InitEndTime = Nothing
                                    InitRecordStartDelayTime = Nothing

                                    VspyRecordingStartedTime = Nothing
                                    VspyRecordingStoppedTime = Nothing
                                    VspyStartRecordingRequestedTime = Nothing
                                    VspyStopRecordingRequestedTime = Nothing
                                    VSpyRecordStartDelayTime = Nothing
                                    VspyRecordStopReqToRecordStopDelayTime = Nothing

                                    VspyEnableAltRecReStartAfterRecordStopFlag = False
                                    StopRecordingRequested = False
                                    OperatorManualIntevention = False
                                    RecordingStarted = False
                                    InDIDRead = False

                                    myProcessor = ""
                                    InvalidDataFlagged = False
                                    ProcessorCommFault = False
                                    ProcCommFaultFlagged = False
                                    InvalidData = False

                                End If

                            End If

                            'Here we flag if user continued with no Camera...
                            If InStr(SaveStartKey, "Cannot Communicate with") > 0 And InStr(SaveStartKey, "at Initialization") > 0 And InStr(SaveStartKey, "CAN") = 0 And InStr(SaveStartKey, "HC") = 0 _
                                    And InStr(SaveStartKey, "XETK") = 0 And InStr(SaveStartKey, "IP") = 0 And InStr(SaveStartKey, "IR") = 0 _
                                    And InStr(SaveStartKey, "K1") = 0 And InStr(SaveStartKey, "K2") = 0 And InStr(SaveStartKey, "ACP") = 0 And InStr(SaveStartKey, "FCM") = 0 Then
                                If InStr(textline, "User chose to continue with initialization") > 0 Then
                                    EventDateTime = Mid(SaveStartKey, 1, 19)
                                    AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",CAMERA INIT ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)), "HostnameBasedReports\", ComputerHostName)

                                    SaveStartKey = ""
                                End If
                            End If

                            'Here we will flag Cannot Communicate with Front at Initialization so we can keep track of the frequency of this situation...
                            If InStr(textline, "Cannot Communicate with") > 0 And InStr(textline, "at Initialization") > 0 Then
                                SaveStartKey = textline
                            End If

                            If EOF(fnum) Then
                                Exit Do
                            End If

                        Loop 'Looping here until we either complete the initialization caused by a user login, or we abnormally exit because something unexpected occurred...

                        'If InStr(textline, "CLEVIR Initialization Complete") > 0 Or InStr(textline, "CLEVIR Init End Time") > 0 Or InStr(UCase(textline), "USER SWITCHED TO") > 0 Then
                        If InStr(textline, "Initialization Complete") > 0 Or InStr(textline, "CLEVIR Init End Time") > 0 Or InStr(UCase(textline), "USER SWITCHED TO") > 0 Then
                            If IsDate(Mid(textline, 1, 19)) Then
                                InitEndTime = Mid(textline, 1, 19)
                            End If
                            InitTimeDelay = InitEndTime.Subtract(InitStartTime)

                            AddToReport("InitDelayTimes.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & Format$(InitStartTime, "MM/dd/yyy HH:mm:ss") & "," & InitTimeDelay.TotalSeconds, "HostnameBasedReports\")

                        Else 'this indicates some sort of abnormal behavior during initialization...
                            InitEndTime = Nothing
                            InitStartTime = Nothing
                            InitTimeDelay = Nothing

                            If IsDate(Mid(textline, 1, 19)) Then
                                EventDateTime = Mid(textline, 1, 19)
                            End If

                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",CLEVIR INITIALIZATION COMPLETE ISSUE," & Mid(textline, 20, Len(textline)) & " - After " & Mid(SaveTextline, 20, Len(SaveTextline)), "HostnameBasedReports\", ComputerHostName)
                        End If

                    End If 'THIS ENDS THE CHECKS THAT ARE HAPPENING AFTER USER LOGIN OR STARTING FINAL INITIALIZATION...

                    'Add to error report if we see Kill Stopping Thread, which indicates CLEVIR was hung on call to INCA and the user
                    'clicked on the INCA Status window to kill all processes...
                    If InStr(textline, "Kill Stopping Thread") > 0 Then
                        AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",CLEVIR Kill Switch Activated," & Mid(textline, 20, Len(textline)), "HostnameBasedReports\", ComputerHostName)
                    End If

                    'Here we set initial record start time which is used to calculate time delay between initial start record button press
                    'and when we actually request recording start...
                    If InStr(textline, "START RECORD Button Pressed") > 0 Then
                        InitialRecordStartTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        StartRecordButtonPressed = True
                    End If

                    'Here we set initial record start delay time which eventually ends up in the DelayTimes.csv file...
                    If InStr(textline, "Start Recording Requested") > 0 Then
                        StartRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        If StartRecordButtonPressed = True Then
                            InitRecordStartDelayTime = StartRecordingRequestedTime.Subtract(InitialRecordStartTime)
                            StartRecordButtonPressed = False
                        End If
                    End If

                    'Lots of stuff happens after we see Recording Started.  Here we will write the saved delay values based on
                    'the previous Stop Record Requested time (if applicable)...
                    If InStr(textline, "Recording Started") > 0 And InStr(textline, "INCA") = 0 Then

                        RecordingStarted = True

                        RecordingStartedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        RecordStartDelayTime = RecordingStartedTime.Subtract(StartRecordingRequestedTime)

                        If StopRecordingRequested = True Then

                            'We will only save the time between record stop and the next record start if there has not been
                            'any manual operator intervention during the record cycle...
                            If OperatorManualIntevention = False Then
                                RecordStopToStartDelayTime = RecordingStartedTime.Subtract(StopRecordingRequestedTime)
                            Else
                                OperatorManualIntevention = False
                            End If

                            StopRecordingRequested = False

                        End If

                        AddToReport("DelayTimes.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & Format$(RecordingStartedTime, "MM/dd/yyy HH:mm:ss") & "," & RecordStopToStartDelayTime.TotalSeconds & "," & InitRecordStartDelayTime.TotalSeconds & "," & RecordStartDelayTime.TotalSeconds & "," & RecordStopReqToRecordStopDelayTime.TotalSeconds & "," & VSpyRecordStartDelayTime.TotalSeconds & "," & VspyRecordStopReqToRecordStopDelayTime.TotalSeconds & "," & VspyEnableAltRecReStartAfterRecordStopFlag.ToString & "," & DIDPullHasBeenRun.ToString, "HostnameBasedReports\")

                        'Here we reset everything so we start from scratch at the beginning of a new recording...
                        InitialRecordStartTime = Nothing
                        RecordingStartedTime = Nothing
                        RecordingStoppedTime = Nothing
                        StartRecordingRequestedTime = Nothing
                        StopRecordingRequestedTime = Nothing
                        InitRecordStartDelayTime = Nothing
                        RecordStartDelayTime = Nothing
                        RecordStopToStartDelayTime = Nothing
                        RecordStopReqToRecordStopDelayTime = Nothing

                        InitStartTime = Nothing
                        InitEndTime = Nothing
                        InitRecordStartDelayTime = Nothing

                        VspyRecordingStartedTime = Nothing
                        VspyRecordingStoppedTime = Nothing
                        VspyStartRecordingRequestedTime = Nothing
                        VspyStopRecordingRequestedTime = Nothing
                        VSpyRecordStartDelayTime = Nothing
                        VspyRecordStopReqToRecordStopDelayTime = Nothing

                        'If we have flagged any invalid data or processor fault, we will save the most recent zipped data file so that we can check it to see if the
                        'invalid data was flagged correctly.  By checking the file in XTool, we can see if the data is valid or not to verify that we are capturing the
                        'invalid data event properly.  Here is where we add the file name to the loglist which we can display in Listbox2 on the DisplayLogFile window...
                        If InvalidDataFlagged = True Or ProcessorCommFault = True Or ProcCommFaultFlagged = True Or InvalidData = True Then

                            textline = LineInput(fnum)

                            'ZipMyFilesNEW: Zipping C:\HB\Data\gmcsv6LAV4654\20200724_050341_Demo\20200724_050341_Demo_6LAV4654_01.mf4 to C:\HB\Data\gmcsv6LAV4654\20200724_050341_Demo\20200724_050341_Demo_6LAV4654_01.zip
                            'C:\HB\Data\gmcsv6NDN775\20200814_172640_Demo\20200814_172640_Demo_6NDN775_01.zip

                            If InStr(textline, "ZipMyFilesNEW called") > 0 Then
                                textline = LineInput(fnum)
                                If InStr(textline, "ZipMyFilesNEW: Zipping") > 0 Or InStr(textline, " - Zipping") > 0 Then
                                    StrStart = InStr(textline, "\g") + 1
                                    strLen = InStr(textline, ".mf4 to ") - StrStart

                                    If strLen > 0 And StrStart > 0 Then

                                        If LogList.Contains(VehicleNumber & " - " & LatestVersion & " - " & mypathprefix & DataUploadPath & Mid(textline, StrStart, strLen) & ".zip") = False Then
                                            LogList.Add(VehicleNumber & " - " & LatestVersion & " - " & mypathprefix & DataUploadPath & Mid(textline, StrStart, strLen) & ".zip")
                                            FileCounter += 1
                                        End If

                                    Else
                                        HandleUserMessageLogging("GMRC", "ParseCLEVIRLogFileNEW: strlen = " & strLen & " strStart = " & StrStart)
                                    End If

                                End If
                            End If

                            myProcessor = ""
                            InvalidDataFlagged = False
                            ProcessorCommFault = False
                            ProcCommFaultFlagged = False
                            InvalidData = False

                        End If

                        'If we are recording, we look for things that indicate we are no longer recording here, or things that
                        'might have taken us out of recording state abnormally,such as recording stopped in INCA...

                        'We also capture various pieces of info based on what we find on each subsequent log entry after recording started
                        'is indicated...
                        Do While InStr(textline, "has expired StopAndStartRecording Called") = 0 And
                            InStr(textline, "STOP RECORD Button Pressed") = 0 And
                            InStr(textline, "STOP MEASUREMENT Button Pressed") = 0 And
                            InStr(textline, "ExitApp () called...") = 0 And
                            InStr(textline, "Initializing CLEVIR") = 0 And
                            InStr(textline, "START RECORD Button Pressed") = 0 And
                            InStr(textline, "Exit Complete") = 0

                            If Len(textline) > 22 And IsDate(Mid(textline, 1, 19)) Then
                                SaveTextline = textline
                            End If

                            textline = LineInput(fnum)

                            'NAN ISSUE IS ACTIVE

                            If ((InStr(textline, "NAN ISSUE IS ACTIVE") > 0)) Then

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline) = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline)
                                End If

                            End If

                            If ((InStr(textline, "Object reference not set") > 0) Or (InStr(textline, "MyBackgroundTasks") > 0)) And (InStr(LatestVersion, "5.4.1") > 0 Or InStr(LatestVersion, "5.5") > 0 Or InStr(LatestVersion, "5.6") > 0 Or InStr(LatestVersion, "6.") > 0) Then
                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline) = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & textline)
                                End If

                            End If

                            'HCF - VaRBSR_Cnt_BackgroundLoop[CeTSKR_e_CPU3] - Not Updating

                            'Cannot Communicate with
                            '07/18/2020 15:47:15 - GetDeviceStatus: Cannot Communicate with XETK:1
                            '07/18/2020 15:47:26 - GetDeviceStatus: Communication with XETK:1 established

                            If InStr(textline, "Cannot Communicate with") > 0 And InStr(UCase(textline), "CALCDEV") = 0 And InStr(UCase(textline), "CAN-MONITORING") = 0 And InStr(UCase(textline), "FRONT") = 0 Then
                                StrStart = InStr(textline, "Cannot Communicate with") + 24
                                strLen = Len(textline) - StrStart

                                If strLen > 0 Then
                                    myProcessor = Mid(textline, StrStart, strLen)
                                    ProcessorCommFault = True

                                Else
                                    HandleUserMessageLogging("GMRC", "ParseCLEVIRLogFileNEW: strLen = " & strLen)
                                End If

                            End If

                            If InStr(textline, "Communication with") > 0 And InStr(textline, "established") > 0 And ProcessorCommFault = True Then
                                myProcessor = ""
                                ProcessorCommFault = False
                            End If

                            If (InStr(textline, "BackgroundLoop") > 0 And InStr(textline, "- Not Updating") > 0) Then
                                StrStart = InStr(textline, " - ") + 3
                                strLen = (InStr(textline, " - Va")) - StrStart

                                If strLen > 0 Then

                                    InvalidData = True
                                    myProcessor = Mid(textline, StrStart, strLen)

                                Else
                                    HandleUserMessageLogging("GMRC", "ParseCLEVIRLogFileNEW: strLen = " & strLen)
                                End If

                            End If

                            If Len(myProcessor) > 0 Then
                                If InStr(textline, "BackgroundLoop") > 0 And InStr(textline, myProcessor) > 0 And InStr(textline, " - Resumed Updating") > 0 Then
                                    myProcessor = ""
                                    InvalidData = False
                                End If
                            End If

                            If (InStr(textline, "UpdateGONOGOLabelColor: INCA COMM Status GO/NOGO is ----------------- COLOR [RED]") > 0) Then
                                InvalidData = True
                            End If

                            If (InStr(textline, "UpdateGONOGOLabelColor: INCA COMM Status GO/NOGO is ----------------- COLOR [GREEN]") > 0) Then
                                InvalidData = False
                            End If

                            If InStr(textline, "INVALID DATA") > 0 Then
                                InvalidDataFlagged = True

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "INVALID DATA") = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "INVALID DATA")
                                    DataAlertCount += 1
                                End If
                            End If

                            If InStr(textline, "INVALID VIDEO") > 0 Then
                                InvalidVideoFlagged = True

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "INVALID VIDEO") = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "INVALID VIDEO")
                                    VideoAlertCount += 1
                                End If

                            End If

                            If InStr(UCase(textline), "PROCESSOR COMM") > 0 Then
                                ProcCommFaultFlagged = True

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "PROC COMM") = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - " & "PROC COMM")
                                    ProcessorCommAlertCount += 1

                                End If

                            End If

                            'Here we handle tracking vspy recording status and time delays...
                            If InStr(textline, "Starting Replay Block Playback") > 0 Then

                                VspyStartRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))

                                Do While InStr(textline, "Replay Block Playback Started") = 0

                                    textline = LineInput(fnum)

                                    If InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                        SaveStartKey = textline
                                        Exit Do
                                    End If

                                    If InStr(textline, "Initializing CLEVIR") > 0 Then
                                        Exit Do
                                    End If

                                    If EOF(fnum) Then
                                        Exit Do
                                    End If

                                Loop

                                If InStr(textline, "Replay Block Playback Started") > 0 Then
                                    VspyRecordingStartedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                                    VSpyRecordStartDelayTime = VspyRecordingStartedTime.Subtract(VspyStartRecordingRequestedTime)
                                    If RecordingStarted = True Then
                                        VspyEnableAltRecReStartAfterRecordStopFlag = False
                                    Else
                                        VspyEnableAltRecReStartAfterRecordStopFlag = True
                                    End If

                                End If

                                If InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                                    EventDateTime = Mid(SaveStartKey, 1, 19)
                                    AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY REPLAY BLOCK START ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)), "HostnameBasedReports\", ComputerHostName)

                                    SaveStartKey = ""
                                End If

                            End If

                            'Here we are flagging various things that may have happened during recording, based on what log info we have seen 
                            'before recording is stopped(whatever the reason, normally Or abnormally)...

                            If InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If InStr(SaveStartKey, "VIDEO_CAMERA_TIMECODE - Not Updating") > 0 Then
                                If InStr(textline, "VIDEO_CAMERA_TIMECODE - Resumed Updating") > 0 Then
                                    SaveStartKey = ""
                                End If
                            End If

                            If InStr(textline, "VIDEO_CAMERA_TIMECODE - Not Updating") > 0 Then
                                SaveStartKey = textline
                            End If

                            If InStr(textline, "CheckForINCAButtonPresses:") > 0 Then
                                If Len(SaveTextline) > 0 Then

                                    If IsDate(Mid(SaveTextline, 1, 19)) Then
                                        CLEVIREventDateTime = Convert.ToDateTime(Mid(SaveTextline, 1, 19))

                                        If IsDate(Mid(textline, 1, 19)) Then
                                            INCAEventDateTime = Convert.ToDateTime(Mid(textline, 1, 19))

                                            SaveStartKey = textline
                                            Exit Do

                                        End If

                                    End If

                                End If
                            End If

                            If InStr(textline, "ExitApp () called...") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If InStr(textline, "Initializing CLEVIR") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If EOF(fnum) Then
                                Exit Do
                            End If

                        Loop

                        'HERE

                        If InStr(textline, "has expired StopAndStartRecording Called") > 0 Then
                            If (InvalidData = True Or InvalidDataFlagged = True Or ProcCommFaultFlagged = True Or ProcessorCommFault = True) Then

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - InvalidData = " & InvalidData.ToString & " - Flagged = " & InvalidDataFlagged.ToString & " - ProcComm = " & ProcessorCommFault.ToString & " - Flagged = " & ProcCommFaultFlagged.ToString) = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - InvalidData = " & InvalidData.ToString & " - Flagged = " & InvalidDataFlagged.ToString & " - ProcComm = " & ProcessorCommFault.ToString & " - Flagged = " & ProcCommFaultFlagged.ToString)
                                End If
                            End If
                        End If

                        If InStr(textline, "STOP RECORD Button Pressed") > 0 Or InStr(textline, "STOP MEASUREMENT Button Pressed") > 0 Then
                            If (InvalidData = True Or InvalidDataFlagged = True Or ProcCommFaultFlagged = True Or ProcessorCommFault = True) Then

                                If LogList.Contains(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - InvalidData = " & InvalidData.ToString & " - Flagged = " & InvalidDataFlagged.ToString & " - ProcComm = " & ProcessorCommFault.ToString & " - Flagged = " & ProcCommFaultFlagged.ToString & " - STOP RECORD / STOP MEASUREMENT") = False Then
                                    LogList.Add(Mid(textline, 1, 19) & " - " & VehicleNumber & " - " & LatestVersion & " - " & myExperiment & " - " & myProcessor & " - InvalidData = " & InvalidData.ToString & " - Flagged = " & InvalidDataFlagged.ToString & " - ProcComm = " & ProcessorCommFault.ToString & " - Flagged = " & ProcCommFaultFlagged.ToString & " - STOP RECORD / STOP MEASUREMENT")
                                End If

                                myProcessor = ""
                                InvalidDataFlagged = False
                                ProcessorCommFault = False
                                ProcCommFaultFlagged = False
                                InvalidData = False
                            End If
                        End If

                        If InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY REPLAY BLOCK START ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)), "HostnameBasedReports\", ComputerHostName)

                            SaveStartKey = ""
                        End If

                        If InStr(SaveStartKey, "Initializing CLEVIR") > 0 Then

                            StartNum = 0
                            AddToStringForVersion = 0

                            If InStr(SaveStartKey, "CLEVIR (Version") > 0 Then
                                AddToStringForVersion = 16
                                StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                            ElseIf InStr(SaveStartKey, "CLEVIR_INCA_7_") > 0 Then
                                AddToStringForVersion = 25
                                StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                            End If

                            If StartNum > AddToStringForVersion Then
                                LatestVersion = Mid(SaveStartKey, StartNum, InStr(textline, ")") - StartNum)
                                'We pass VehicleNumber to AddToReport for consistency, but we don't use VehicleNumber in SoftwareVersions.csv file...
                                AddToReport("SoftwareVersions.csv", VehicleNumber, LatestVersion, "HostnameBasedReports\")
                            End If

                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",CLEVIR Hung up during Recording," & Mid(SaveStartKey, 20, Len(SaveStartKey)) & " After " & SaveTextline, "HostnameBasedReports\", ComputerHostName)
                            SaveStartKey = ""

                            InitialRecordStartTime = Nothing
                            RecordingStartedTime = Nothing
                            RecordingStoppedTime = Nothing
                            StartRecordingRequestedTime = Nothing
                            StopRecordingRequestedTime = Nothing
                            InitRecordStartDelayTime = Nothing
                            RecordStartDelayTime = Nothing
                            RecordStopToStartDelayTime = Nothing
                            RecordStopReqToRecordStopDelayTime = Nothing

                            InitStartTime = Nothing
                            InitEndTime = Nothing
                            InitRecordStartDelayTime = Nothing

                            VspyRecordingStartedTime = Nothing
                            VspyRecordingStoppedTime = Nothing
                            VspyStartRecordingRequestedTime = Nothing
                            VspyStopRecordingRequestedTime = Nothing
                            VSpyRecordStartDelayTime = Nothing
                            VspyRecordStopReqToRecordStopDelayTime = Nothing

                            VspyEnableAltRecReStartAfterRecordStopFlag = False
                            StopRecordingRequested = False
                            OperatorManualIntevention = False
                            RecordingStarted = False
                            InDIDRead = False

                            myProcessor = ""
                            InvalidDataFlagged = False
                            ProcessorCommFault = False
                            ProcCommFaultFlagged = False
                            InvalidData = False

                        End If

                        If InStr(SaveStartKey, "CheckForINCAButtonPresses:") > 0 Then
                            RecordingStarted = False

                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",QUESTIONABLE USER INTERACTION ISSUE WHILE RECORDING," & Mid(SaveStartKey, 20, Len(SaveStartKey)) & " After " & SaveTextline, "HostnameBasedReports\", ComputerHostName)

                            SaveStartKey = ""

                            OperatorManualIntevention = True
                            RecordStopToStartDelayTime = Nothing
                            RecordStopReqToRecordStopDelayTime = Nothing

                        End If

                        If InStr(SaveStartKey, "ExitApp () called...") > 0 And InStr(SaveTextline, "WriteMileage") = 0 And InStr(SaveTextline, "Cannot Communicate") = 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",QUESTIONABLE APP EXIT ISSUE WHILE RECORDING," & Mid(SaveStartKey, 20, Len(SaveStartKey)) & " After " & SaveTextline, "HostnameBasedReports\", ComputerHostName)

                            SaveStartKey = ""
                        End If

                        If InStr(SaveStartKey, "VIDEO_CAMERA_TIMECODE - Not Updating While Recording ") > 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)

                            SaveStartKey = ""
                        End If

                    End If 'If InStr(textline, "Recording Started") > 0 And InStr(textline, "INCA") = 0 Then

                    If InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                        EventDateTime = Mid(textline, 1, 19)
                        AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY REPLAY BLOCK START ISSUE," & Mid(textline, 20, Len(textline)), "HostnameBasedReports\", ComputerHostName)

                        SaveStartKey = ""
                    End If

                    If InStr(textline, "STOP RECORD Button Pressed") > 0 Or InStr(textline, "START RECORD Button Pressed") > 0 Then
                        OperatorManualIntevention = True
                        RecordStopToStartDelayTime = Nothing
                    End If

                    If InStr(textline, "Stop Recording Requested") > 0 Then
                        StopRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        StopRecordingRequested = True
                        RecordingStarted = False
                    End If

                    If InStr(textline, "Recording Stopped") > 0 And InStr(textline, "INCA") = 0 Then

                        RecordingStoppedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        RecordStopReqToRecordStopDelayTime = RecordingStoppedTime.Subtract(StopRecordingRequestedTime)

                    End If

                    If InStr(textline, "Stopping VehicleSpy Replay Block Playback") > 0 Then
                        VspyStopRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                    End If

                    If InStr(textline, "VehicleSpy Stopped") > 0 Then
                        VspyRecordingStoppedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                        VspyRecordStopReqToRecordStopDelayTime = VspyRecordingStoppedTime.Subtract(VspyStopRecordingRequestedTime)
                    End If

                    If InStr(textline, "Verifying VehicleSPY setup") > 0 Then

                        Do While InStr(textline, "VehicleSpy Measurement Stopped") = 0 And InStr(textline, "In ReadInSignalList") = 0

                            textline = LineInput(fnum)

                            If InStr(textline, "VehicleSpy did not Start.  VehicleSpy Setup Verification failed") > 0 _
                                Or InStr(textline, "VehicleSpy Setup Verification failed. Could not connect to VSpy") > 0 _
                                Or InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 _
                                Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If EOF(fnum) Then
                                Exit Do
                            End If

                        Loop

                        If InStr(SaveStartKey, "VehicleSpy did not Start.  VehicleSpy Setup Verification failed") > 0 _
                            Or InStr(SaveStartKey, "VehicleSpy Setup Verification failed. Could not connect to VSpy") > 0 _
                            Or InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 _
                            Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY VERIFICATION ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)), "HostnameBasedReports\", ComputerHostName)

                            SaveStartKey = ""
                        End If

                    End If

                    If InStr(textline, "Preparing to read DID Information, please wait...") > 0 Then
                        InDIDRead = True
                    End If

                    If InStr(textline, "Starting Replay Block Playback") > 0 And InDIDRead = False Then

                        VspyStartRecordingRequestedTime = Convert.ToDateTime(Mid(textline, 1, 19))

                        Do While InStr(textline, "Replay Block Playback Started") = 0 And
                                InStr(textline, "STOP RECORD Button Pressed") = 0 And
                                InStr(textline, "ZipMyFilesNEW called") = 0 And
                                InStr(textline, "Initializing CLEVIR") = 0 And
                                InStr(textline, "has expired StopAndStartRecording Called") = 0

                            textline = LineInput(fnum)

                            If InStr(textline, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(textline, "No connection could be made because the target machine actively refused it") > 0 Then
                                SaveStartKey = textline
                                Exit Do
                            End If

                            If EOF(fnum) Then
                                Exit Do
                            End If

                        Loop

                        If InStr(textline, "Replay Block Playback Started") > 0 Then
                            VspyRecordingStartedTime = Convert.ToDateTime(Mid(textline, 1, 19))
                            VSpyRecordStartDelayTime = VspyRecordingStartedTime.Subtract(VspyStartRecordingRequestedTime)
                            If RecordingStarted = True Then
                                VspyEnableAltRecReStartAfterRecordStopFlag = False
                            Else
                                VspyEnableAltRecReStartAfterRecordStopFlag = True
                            End If

                        End If

                        If InStr(SaveStartKey, "An existing connection was forcibly closed by the remote host") > 0 Or InStr(SaveStartKey, "No connection could be made because the target machine actively refused it") > 0 Then
                            EventDateTime = Mid(SaveStartKey, 1, 19)
                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY READ DID REPLAY BLOCK START ISSUE," & Mid(SaveStartKey, 20, Len(SaveStartKey)), "HostnameBasedReports\", ComputerHostName)

                            SaveStartKey = ""
                        End If

                        If InStr(textline, "Initializing CLEVIR") > 0 Then

                            StartNum = 0
                            AddToStringForVersion = 0

                            If InStr(SaveStartKey, "CLEVIR (Version") > 0 Then
                                AddToStringForVersion = 16
                                StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                            ElseIf InStr(SaveStartKey, "CLEVIR_INCA_7_") > 0 Then
                                AddToStringForVersion = 25
                                StartNum = InStr(SaveStartKey, "CLEVIR") + AddToStringForVersion
                            End If

                            If StartNum > AddToStringForVersion Then
                                LatestVersion = Mid(textline, StartNum, InStr(textline, ")") - StartNum)
                                'We pass VehicleNumber to AddToReport for consistency, but we don't use VehicleNumber in SoftwareVersions.csv file...
                                AddToReport("SoftwareVersions.csv", VehicleNumber, LatestVersion, "HostnameBasedReports\")
                            End If

                            EventDateTime = Mid(textline, 1, 19)

                            AddToReport("Errors.csv", VehicleNumber, VehicleNumber & "," & LatestVersion & "," & EventDateTime & ",VEHICLE SPY READ DID REPLAY BLOCK START ISSUE," & Mid(textline, 20, Len(textline)), "HostnameBasedReports\", ComputerHostName)

                            SaveStartKey = ""

                            InitialRecordStartTime = Nothing
                            RecordingStartedTime = Nothing
                            RecordingStoppedTime = Nothing
                            StartRecordingRequestedTime = Nothing
                            StopRecordingRequestedTime = Nothing
                            InitRecordStartDelayTime = Nothing
                            RecordStartDelayTime = Nothing
                            RecordStopToStartDelayTime = Nothing
                            RecordStopReqToRecordStopDelayTime = Nothing

                            InitStartTime = Nothing
                            InitEndTime = Nothing
                            InitRecordStartDelayTime = Nothing

                            VspyRecordingStartedTime = Nothing
                            VspyRecordingStoppedTime = Nothing
                            VspyStartRecordingRequestedTime = Nothing
                            VspyStopRecordingRequestedTime = Nothing
                            VSpyRecordStartDelayTime = Nothing
                            VspyRecordStopReqToRecordStopDelayTime = Nothing

                            VspyEnableAltRecReStartAfterRecordStopFlag = False
                            StopRecordingRequested = False
                            OperatorManualIntevention = False
                            RecordingStarted = False
                            InDIDRead = False

                            myProcessor = ""
                            InvalidDataFlagged = False
                            ProcessorCommFault = False
                            ProcCommFaultFlagged = False
                            InvalidData = False

                        End If

                    End If

                    If InStr(textline, "File deleted") > 0 And InDIDRead = True Then
                        InDIDRead = False
                        DIDPullHasBeenRun = True
                    End If


                    If Len(textline) > 22 And IsDate(Mid(textline, 1, 19)) Then
                        SaveTextline = textline
                    End If

                Loop

                FileClose(fnum)

                If Len(LatestVersion) > 0 Then
                    If Len(VehicleNumber) = 0 Then
                        MsgBox("VehicleNumber = " & VehicleNumber)
                    End If
                    AddToReport("LastUploadTime.csv", VehicleNumber, LatestVersion & "," & System.IO.File.GetLastWriteTime(filename), "HostnameBasedReports\", ComputerHostName)
                Else
                    AddToReport("LastUploadTime.csv", VehicleNumber, "UNDF" & "," & System.IO.File.GetLastWriteTime(filename), "HostnameBasedReports\", ComputerHostName)
                End If

            End If

            If CheckBox1.Checked = True Then

                DisplayLogFile.ListBox2.SelectedIndex = DisplayLogFile.ListBox2.Items.Count - 1
                DisplayLogFile.ListBox2.Refresh()

                DisplayLogFile.Label2.Text = FileCounter
                DisplayLogFile.Label2.Refresh()

                DisplayLogFile.Label3.Text = DataAlertCount
                DisplayLogFile.Label3.Refresh()

                DisplayLogFile.Label5.Text = VideoAlertCount
                DisplayLogFile.Label5.Refresh()

                DisplayLogFile.Label7.Text = ProcessorCommAlertCount
                DisplayLogFile.Label7.Refresh()

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ParseCLEVIRLogFileNEW: " & ex.Message, DisplayMsgBox)
        End Try

    End Sub

    Private Sub AppendLogFileInfo(ByVal Filename As String)

        'Called from CopyLogFiles if the new log file is smaller than the log file that exists locally.  This is required because GM_ResidentClient.log files on the in-vehicle
        'PCs are purged if they become too large, so we need to make sure that we maintain the entire contents of the local log file and only add the new information from the new
        'log file copied from the share drive...

        Dim fnum As Integer
        Dim LastLogEntry As String = ""
        Dim FoundLastEntry As Boolean

        Dim i As Integer
        Dim x As Integer

        Dim OldFileTextArray As New List(Of String)
        Dim NewFileTextArray As New List(Of String)

        If MsgBox("Append Log File info?", vbYesNo) = vbYes Then

            HandleUserMessageLogging("GMRC", "Appending file info for " & Filename,,,, ListBox9)

            If Not FileInUse(Filename) And Not FileInUse(Filename & ".SAVE") Then
                FileCopy(Filename & ".SAVE", Filename & ".tmp")

                If Not FileInUse(Filename & ".tmp") Then
                    fnum = FreeFile()
                    FileOpen(fnum, Filename & ".tmp", OpenMode.Input)
                    Do While Not EOF(fnum)
                        OldFileTextArray.Add(LineInput(fnum))
                    Loop
                    FileClose(fnum)

                    LastLogEntry = OldFileTextArray(OldFileTextArray.Count - 1).ToString

                    fnum = FreeFile()
                    FileOpen(fnum, Filename, OpenMode.Input)
                    Do While Not EOF(fnum)
                        NewFileTextArray.Add(LineInput(fnum))
                    Loop
                    FileClose(fnum)

                    For i = 0 To NewFileTextArray.Count - 1
                        If NewFileTextArray(i).ToString = LastLogEntry Then
                            FoundLastEntry = True
                            Exit For
                        End If
                    Next

                    If FoundLastEntry = True Then
                        x = i + 1
                        For i = x To NewFileTextArray.Count - 1
                            OldFileTextArray.Add(NewFileTextArray(i).ToString)
                        Next
                    Else
                        For i = 0 To NewFileTextArray.Count - 1
                            OldFileTextArray.Add(NewFileTextArray(i).ToString)
                        Next
                    End If

                    fnum = FreeFile()
                    FileOpen(fnum, Filename & ".tmp", OpenMode.Output)
                    For i = 0 To OldFileTextArray.Count - 1
                        PrintLine(fnum, OldFileTextArray(i).ToString)
                    Next
                    FileClose(fnum)

                    File.Copy(Filename & ".tmp", Filename, True)

                Else
                    HandleUserMessageLogging("GMRC", Filename & ".tmp" & " file in use, no file combine performed",,,, ListBox9)
                End If

            Else
                HandleUserMessageLogging("GMRC", "There is a file in use, no file combine performed",,,, ListBox9)
            End If

        End If

    End Sub

    Private Sub CopyLogFiles(ByVal NumDaysBack As Integer)

        'This routine copies the log files from each vehicle sub-folder on the Q drive to the local drive - Calls ParseCLEVIRLogFile for each file
        'found which parses the log file contents...

        Dim mypath As String = ""
        Dim mySavePath As String = ""

        Dim myfiles() As String
        Dim x As Integer

        Dim textline As String = ""
        Dim y As Integer
        Dim z As Integer

        Dim TextlineArray() As String

        Dim myVehicleNumber As String = ""

        Dim FoundLogFile As Boolean

        Dim LatestVersion As String = "UNDF"

        Dim myDirectories() As String

        Dim fnum As Integer
        Dim fnum2 As Integer

        Dim VehicleNumbers As ArrayList = Nothing

        Try

            CopyingLogFiles = True

            Me.Cursor = Cursors.WaitCursor

            ListBox1.Items.Clear()
            ListBox2.Items.Clear()
            ListBox3.Items.Clear()
            ListBox4.Items.Clear()
            ListBox5.Items.Clear()
            ListBox6.Items.Clear()

            ListBox1.Refresh()
            ListBox2.Refresh()
            ListBox3.Refresh()
            ListBox4.Refresh()
            ListBox5.Refresh()
            ListBox6.Refresh()

            SoftwareVersionsList.Clear()

            For y = 0 To 8

                Select Case y

                    Case 0
                        mypath = mypathprefix
                        DataUploadPath = "\"
                        mySavePath = mySavepathprefix & "\LogFiles\CSAV2\"
                    Case 1
                        mypath = mypathprefix & "\LowContent\VehicleData"
                        DataUploadPath = "\LowContent\VehicleData" & "\"
                        mySavePath = mySavepathprefix & "\LogFiles\LowContent\"
                    Case 2
                        mypath = mypathprefix & "\HighContent\VehicleData"
                        DataUploadPath = "\HighContent\VehicleData" & "\"
                        mySavePath = mySavepathprefix & "\LogFiles\HighContent\"
                    Case 3
                        mypath = mypathprefix & "\HighContent\RideData"
                        DataUploadPath = "\HighContent\RideData" & "\"
                        mySavePath = mySavepathprefix & "\LogFiles\HighContent\"
                    Case 4
                        mypath = mypathprefix & "\HighContent\TraileringData"
                        DataUploadPath = "\HighContent\TraileringData" & "\"
                        mySavePath = mySavepathprefix & "\LogFiles\HighContent\"
                    Case 5
                        mypath = mypathprefix & "\ACP2\VehicleData"
                        DataUploadPath = "\ACP2\VehicleData" & "\"
                        mySavePath = mySavepathprefix & "\LogFiles\ACP2\"

                    Case 6
                        mypath = mypathprefix & "\ACP3\VehicleData"
                        DataUploadPath = "\ACP3\VehicleData" & "\"
                        mySavePath = mySavepathprefix & "\LogFiles\ACP3\"

                    Case 7
                        mypath = mypathprefix & "\ACP4\VehicleData"
                        DataUploadPath = "\ACP4\VehicleData" & "\"
                        mySavePath = mySavepathprefix & "\LogFiles\ACP4\"

                    Case 8
                        mypath = mypathprefix & "\FCM\VehicleData"
                        DataUploadPath = "\FCM\VehicleData" & "\"
                        mySavePath = mySavepathprefix & "\LogFiles\FCM\"


                End Select

                If Directory.Exists(mypath) Then

                    myDirectories = System.IO.Directory.GetDirectories(mypath)

                    For z = 0 To UBound(myDirectories)

                        If InStr(myDirectories(z), "gmcsv") > 0 Then

                            myVehicleNumber = Mid(System.IO.Path.GetFileName(myDirectories(z)), 6, Len(System.IO.Path.GetFileName(myDirectories(z))))
                            If VehicleNumbers Is Nothing Then
                                VehicleNumbers = New ArrayList From {
                                    myVehicleNumber
                                }
                            Else
                                If Not VehicleNumbers.Contains(myVehicleNumber) Then
                                    VehicleNumbers.Add(myVehicleNumber)
                                Else
                                    If InStr(mypath, "RideData") = 0 And InStr(mypath, "TraileringData") = 0 Then
                                        ListBox9.Items.Add("Vehicle Number " & myVehicleNumber & " in " & myDirectories(z) & " exists elsewhere...")
                                    End If
                                End If
                            End If
                            FoundLogFile = False

                            myfiles = Directory.GetFiles(myDirectories(z))

                            For x = 0 To UBound(myfiles)

                                If InStr(myfiles(x), "GM_ResidentClient.log") > 0 Then

                                    'Only copy those log files within the specified date...
                                    If System.IO.File.GetLastWriteTime(myfiles(x)) >= DateTime.Now.AddDays(-NumDaysBack) Then

                                        FoundLogFile = True

                                        If Not Directory.Exists(mySavePath & myVehicleNumber) Then
                                            Directory.CreateDirectory(mySavePath & myVehicleNumber)
                                        End If

                                        'If the log already exists, we will copy  it to a save file first, then determine how to copy updated log file info to it...
                                        If File.Exists(mySavePath & myVehicleNumber & "\GM_ResidentClient.log") Then

                                            'If newer write time, we need to either copy file over current local file, or we need to append its info if file is smaller size
                                            'indicating that a new file has been started...
                                            If System.IO.File.GetLastWriteTime(myfiles(x)) > System.IO.File.GetLastWriteTime(mySavePath & myVehicleNumber & "\GM_ResidentClient.log") Then

                                                'Copy Existing log file to SAVE file...
                                                File.Copy(mySavePath & myVehicleNumber & "\GM_ResidentClient.log", mySavePath & myVehicleNumber & "\GM_ResidentClient.log.SAVE", True)

                                                Dim SourceFile As New System.IO.FileInfo(myfiles(x))
                                                Dim DestFile As New System.IO.FileInfo(mySavePath & myVehicleNumber & "\GM_ResidentClient.log.SAVE")

                                                'Copy new file from share drive into GM_ResidentClient.log on local drive
                                                RoboCopyFile(myfiles(x), mySavePath & myVehicleNumber)

                                                If SourceFile.Length < DestFile.Length Then

                                                    AppendLogFileInfo(mySavePath & myVehicleNumber & "\GM_ResidentClient.log")

                                                End If

                                            End If

                                        Else
                                            RoboCopyFile(myfiles(x), mySavePath & myVehicleNumber)
                                        End If

                                    End If

                                    VehicleNumber = myVehicleNumber

                                    ParseCLEVIRLogFile(mySavePath & myVehicleNumber & "\GM_ResidentClient.log")

                                    Exit For

                                End If

                            Next

                        End If

                    Next z

                End If

            Next y

            'Add last known connection time and pc host name for each vehicle here...

            'Private mypathprefix As String = "\\Nam.corp.gm.com\tcws-dfs\Project\CSV\CSAV2"

            'Q:" & CLEVIRBaseDir & "\PCNetworkConnectStatus\Updated_PCInfoSAVE.csv

            'USMPGTNCSV0057, C121N030, 1 / 13 / 2021
            'USMPGTNCSV0068, 6MDV4068, 10 / 2 / 2020
            'USMPGTNCSV0060, 4NZN5208, 6 / 24 / 2020
            'USMPGTNCSV0067, 6MDV4980, 1 / 8 / 2021

            Dim UpdatedPCInfoArray As ArrayList
            Dim LastUploadTimeArray As ArrayList
            Dim UpdatedDisplayArray As ArrayList
            Dim TextLineArray2() As String

            UpdatedPCInfoArray = New ArrayList
            LastUploadTimeArray = New ArrayList
            UpdatedDisplayArray = New ArrayList

            fnum2 = FreeFile()
            FileOpen(fnum2, mypathprefix & CLEVIRBaseDir & "\PCNetworkConnectStatus\Updated_PCInfoSAVE.csv", OpenMode.Input)

            Do While Not EOF(fnum2)

                textline = LineInput(fnum2)
                TextlineArray = Split(textline, ",")
                UpdatedPCInfoArray.Add(TextlineArray(1) & "," & TextlineArray(0) & "," & TextlineArray(2)) 'C121N030, USMPGTNCSV0057, 1 / 13 / 2021

            Loop

            FileClose(fnum2)

            UpdatedPCInfoArray.Sort()

            fnum = FreeFile()
            FileOpen(fnum, mySavepathprefix & "\Reports\LastUploadTime.csv", OpenMode.Input)

            'C121N026, 5.4.17, 12 / 3 / 2020 10: 58:01 AM
            '6Lnn4957,5.4.1,6/5/2020 3:19:19 PM
            'C121N030, 5.4.17, 12 / 3 / 2020 10: 00:16 AM

            'ListBox1.Items.Clear()

            Do While Not EOF(fnum)

                textline = LineInput(fnum)
                LastUploadTimeArray.Add(textline)

            Loop

            FileClose(fnum)

            LastUploadTimeArray.Sort()

            GoTo bypassMerge

            'Merge UpdatedPCInfoArray info with LastUploadTimeArray info into UpdatedDisplayArray

            Dim FoundMatch As Boolean

            For x = 0 To LastUploadTimeArray.Count - 1
                FoundMatch = False
                TextlineArray = Split(LastUploadTimeArray(x), ",")
                For y = 0 To UpdatedPCInfoArray.Count - 1
                    TextLineArray2 = Split(UpdatedPCInfoArray(y), ",")
                    If InStr(UpdatedPCInfoArray(y).ToString, TextlineArray(0)) > 0 Then
                        UpdatedDisplayArray.Add(LastUploadTimeArray(x) & "," & TextLineArray2(1) & "," & TextLineArray2(2))
                        FoundMatch = True
                        Exit For
                    End If
                Next
                If FoundMatch = False Then
                    UpdatedDisplayArray.Add(LastUploadTimeArray(x).ToString & ",NA,NA")
                End If

            Next

bypassMerge:

            UpdatedDisplayArray = LastUploadTimeArray

            ListBox1.Items.Clear()

            For x = 0 To UpdatedDisplayArray.Count - 1

                'split on spaces, display 0, 3 and 4

                ListBox1.Items.Add(UpdatedDisplayArray(x).ToString)

            Next x

            '********************************************************************

            fnum = FreeFile()
            FileOpen(fnum, mySavepathprefix & "\Reports\Errors.csv", OpenMode.Output)

            ListBox3.Items.Clear()

            ErrorsList.Sort()

            For x = 0 To ErrorsList.Count - 1
                ListBox3.Items.Add(ErrorsList(x))
                PrintLine(fnum, ErrorsList(x))
            Next

            FileClose(fnum)

            fnum = FreeFile()
            FileOpen(fnum, mySavepathprefix & "\Reports\DelayTimes.csv", OpenMode.Output)

            ListBox4.Items.Clear()

            DelayTimesList.Sort()

            For x = 0 To DelayTimesList.Count - 1
                textline = DelayTimesList(x)
                If InStr(textline, "VEHNUM") > 0 Then
                    ListBox4.Items.Add(textline)
                Else
                    TextlineArray = Split(textline, ",")
                    ListBox4.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3).PadRight(15, " ") & "," & TextlineArray(4).PadRight(14, " ") & "," & TextlineArray(5).PadRight(10, " ") & "," & TextlineArray(6).PadRight(22, " ") & "," & TextlineArray(7).PadRight(14, " ") & "," & TextlineArray(8).PadRight(26, " ") & "," & TextlineArray(9).PadRight(21, " ") & "," & TextlineArray(10).PadRight(13, " "))
                    PrintLine(fnum, DelayTimesList(x))
                End If
            Next x

            FileClose(fnum)

            fnum = FreeFile()

            FileOpen(fnum, mySavepathprefix & "\Reports\InitDelayTimes.csv", OpenMode.Output)
            ListBox6.Items.Clear()

            InitDelayTimesList.Sort()

            For x = 0 To InitDelayTimesList.Count - 1
                textline = InitDelayTimesList(x)
                If InStr(textline, "VEHNUM") > 0 Then
                    ListBox6.Items.Add(textline)
                Else
                    TextlineArray = Split(textline, ",")
                    ListBox6.Items.Add(TextlineArray(0).PadRight(10, " ") & "," & TextlineArray(1).PadRight(7, " ") & "," & TextlineArray(2).PadRight(21, " ") & "," & TextlineArray(3))
                    PrintLine(fnum, InitDelayTimesList(x))
                End If
            Next x

            FileClose(fnum)

            fnum = FreeFile()
            FileOpen(fnum, mySavepathprefix & "\Reports\SoftwareVersions.csv", OpenMode.Output)

            ListBox5.Items.Clear()

            SoftwareVersionsList.Sort()
            SoftwareVersionsList.Reverse()

            For x = 0 To SoftwareVersionsList.Count - 1
                ListBox5.Items.Add(SoftwareVersionsList(x))
                PrintLine(fnum, SoftwareVersionsList(x))
            Next

            FileClose(fnum)

            GetLocalFilePaths()

            'This is the Display Alerts Check box on the main VehicleStatDashboard...
            If CheckBox1.Checked = True Then

                'LogList contains a list of ALERTS such as PROC COMM ALERT, INVALID VIDEO ALERT, INVALID DATA ALERT and some additional error types which are displayed in the DisplayLogFile.ListBox2
                'We can turn on and off the display of these alerts with the Display Alerts Checkbox on the main form.  The default is not to display, checkbox unchecked...
                For x = 0 To LogList.Count - 1
                    DisplayLogFile.ListBox2.Items.Add(LogList(x))
                Next

            End If


            Me.Cursor = Cursors.Arrow
            CopyingLogFiles = False

            VehicleNumber = ""

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CopyLogFiles: " & ex.Message, DisplayMsgBox)
        End Try


    End Sub

    Public Sub GetLocalFilePaths(Optional ByVal LocalComputerLogFilesFolder As String = "")

        'W:\CSAV2 Tools\CLEVIR\Development\PC_HostNameLogFiles

        'Populates Global variable LocalLogFilePaths with all log file folder names on local drive

        Dim mySavePath As String = ""

        'ListBox2.Items.Clear()

        ReDim LocalLogFilePaths(0)

        Dim Cnt As Integer = 0

        If Len(LocalComputerLogFilesFolder) = 0 Then

            For y = 0 To 6

                Select Case y

                    Case 0
                        mySavePath = mySavepathprefix & "\LogFiles\CSAV2\"
                    Case 1
                        mySavePath = mySavepathprefix & "\LogFiles\LowContent\"
                    Case 2
                        mySavePath = mySavepathprefix & "\LogFiles\HighContent\"
                    Case 3
                        mySavePath = mySavepathprefix & "\LogFiles\ACP2\"
                    Case 4
                        mySavePath = mySavepathprefix & "\LogFiles\ACP3\"
                    Case 5
                        mySavePath = mySavepathprefix & "\LogFiles\ACP4\"
                    Case 6
                        mySavePath = mySavepathprefix & "\LogFiles\FCM\"

                End Select

                Dim dir As New DirectoryInfo(mySavePath)
                Dim dirs As DirectoryInfo() = dir.GetDirectories()

                For x = 0 To UBound(dirs)
                    'ListBox2.Items.Add(dirs(x).Name)

                    If Cnt > 0 Then
                        ReDim Preserve LocalLogFilePaths(Cnt)
                    End If
                    LocalLogFilePaths(Cnt) = mySavePath & dirs(x).Name
                    Cnt += 1
                Next x

            Next y

        Else
            Dim dir As New DirectoryInfo(LocalComputerLogFilesFolder)
            Dim dirs As DirectoryInfo() = dir.GetDirectories()

            For x = 0 To UBound(dirs)
                'ListBox2.Items.Add(dirs(x).Name)
                If Cnt > 0 Then
                    ReDim Preserve LocalLogFilePaths(Cnt)
                End If
                LocalLogFilePaths(Cnt) = LocalComputerLogFilesFolder & "\" & dirs(x).Name
                Cnt += 1
            Next x

        End If

    End Sub

    Private Sub RepairLogFile()

        'This routine is no longer applicable.  Button has been disabled on VehicleStatDashboard form...

        Dim fnum As Integer
        Dim textline As String
        Dim found As Boolean
        Dim myVehicleNumber As String = ""

        OpenFileDialog1.InitialDirectory = "Q:" & CLEVIRBaseDir & "\VehicleLogFiles"
        OpenFileDialog1.DefaultExt = ".log"
        OpenFileDialog1.Filter = "log |*.log"
        OpenFileDialog1.Title = "Please Select a Log File"
        OpenFileDialog1.ShowDialog()

        If Len(OpenFileDialog1.FileName) > 0 And InStr(OpenFileDialog1.FileName, ".log") > 0 Then

            DisplayLogFile.Show()

            fnum = FreeFile()
            FileOpen(fnum, OpenFileDialog1.FileName, OpenMode.Input)

            Do While Not EOF(fnum)

                textline = LineInput(fnum)

                If InStr(textline, "Data\gmcsv") > 0 Then

                    If Mid(textline, InStr(textline, "Data\gmcsv") + 18, 1) = "\" Then
                        myVehicleNumber = Mid(textline, (InStr(textline, "Data\gmcsv") + 10), 8)
                    Else
                        myVehicleNumber = Mid(textline, (InStr(textline, "Data\gmcsv") + 10), 9)
                    End If

                End If

                If InStr(textline, "CLEVIR (Version") > 0 Or (InStr(textline, "CLEVIR_INCA_7_") > 0 And InStr(textline, "(Version") > 0) Or InStr(textline, "Recording Stopped") > 0 Then
                    found = False
                End If

                If InStr(textline, "Parameter name:") > 0 And found = False Then
                    found = True
                    DisplayLogFile.ListBox1.Items.Add(textline)

                    DisplayLogFile.ListBox1.SelectedIndex = DisplayLogFile.ListBox1.Items.Count - 1
                    DisplayLogFile.ListBox1.Refresh()
                    System.Windows.Forms.Application.DoEvents()
                Else
                    If found = True Then
                        If InStr(textline, "Parameter name:") = 0 And InStr(textline, "Format Display String") = 0 Then
                            DisplayLogFile.ListBox1.Items.Add(textline)

                            DisplayLogFile.ListBox1.SelectedIndex = DisplayLogFile.ListBox1.Items.Count - 1
                            DisplayLogFile.ListBox1.Refresh()
                            System.Windows.Forms.Application.DoEvents()
                        End If
                    Else
                        DisplayLogFile.ListBox1.Items.Add(textline)

                        DisplayLogFile.ListBox1.SelectedIndex = DisplayLogFile.ListBox1.Items.Count - 1
                        DisplayLogFile.ListBox1.Refresh()
                        System.Windows.Forms.Application.DoEvents()
                    End If

                End If

            Loop

            FileClose(fnum)

            'W:\CLEVIR VS 2017 NET 4.6.1\CLEVIR_INCA_7_2\bin\Debug\CLEVIR_Vehicle_Status_Info\LogFiles

            fnum = FreeFile()
            FileOpen(fnum, My.Application.Info.DirectoryPath & "\CLEVIR_Vehicle_Status_Info\LogFiles\Temp\" & myVehicleNumber & "_GM_ResidentClient.log", OpenMode.Output)

            For x = 0 To DisplayLogFile.ListBox1.Items.Count - 1
                PrintLine(fnum, DisplayLogFile.ListBox1.Items(x).ToString)
            Next

            FileClose(fnum)

        End If

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        'Copy Vehicle Logs from Q button...

        LogFileType = "VehicleBased"
        CreateListsFromFileContent("")
        CopyLogFiles(Val(TextBox1.Text)) 'Textbox1.text is num days back...

        LoadLists()

    End Sub

    Private Sub PerformSupportFileConfigurationToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PerformSupportFileConfigurationToolStripMenuItem.Click

        FormatExperimentVariableOutput(OpenFileDialog1)

    End Sub

    Private Sub ListBox3_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox3.SelectedIndexChanged

    End Sub

    Private Sub ListBox3_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox3.SelectedValueChanged

        'Initiated by clicking any row in the Event List. Searches for the time stamp of the selected event in all files.
        'This functionality was changed from the original (see below) because we can no longer assume that the PC always stays
        'with the same vehicle.  So, the event associated with a particular vehicle number may actually be in a log file
        'that is now associated with a different vehicle (and located in a different vehicle folder on the local drive,
        'based on the most recent vehicle number in the file...

        Dim textarray() As String

        textarray = Split(ListBox3.SelectedItem.ToString, ",")

        FindStringInAllFiles(textarray(2))

        Exit Sub

        'Displays the entire contents of the log file for the vehicle number in the selected row.  Highlights the
        'row in the log file that corrsponds to the selection...

        Dim tempstr() As String
        Dim vehiclenumber As String
        Dim dateandtime As String

        Dim mySavePath As String = ""

        Dim fnum As String

        Dim textline As String

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer

        Dim FoundFile As Boolean

        Dim myDirectories() As String
        Dim myFiles() As String

        If InhibitListboxAction = True Then
            InhibitListboxAction = False
            Exit Sub
        End If

        Me.Cursor = Cursors.WaitCursor


        DisplayLogFile.ListBox1.Items.Clear()

        tempstr = Split(ListBox3.SelectedItem.ToString, ",")

        vehiclenumber = tempstr(0)
        dateandtime = tempstr(2)

        For y = 0 To 6

            Select Case y

                Case 0
                    mySavePath = mySavepathprefix & "\LogFiles\CSAV2\"
                Case 1
                    mySavePath = mySavepathprefix & "\LogFiles\LowContent\"
                Case 2
                    mySavePath = mySavepathprefix & "\LogFiles\HighContent\"
                Case 3
                    mySavePath = mySavepathprefix & "\LogFiles\ACP2\"
                Case 4
                    mySavePath = mySavepathprefix & "\LogFiles\ACP3\"
                Case 5
                    mySavePath = mySavepathprefix & "\LogFiles\ACP4\"
                Case 6
                    mySavePath = mySavepathprefix & "\LogFiles\FCM\"

                    'Will need to add cases here for ACP2, ACP3, ACP4, FCM...
            End Select

            myDirectories = System.IO.Directory.GetDirectories(mySavePath)

            For x = 0 To UBound(myDirectories)
                If InStr(myDirectories(x), vehiclenumber) > 0 Then
                    myFiles = Directory.GetFiles(myDirectories(x))
                    For z = 0 To UBound(myFiles)
                        If InStr(myFiles(z), "GM_ResidentClient.log") > 0 Then

                            FoundFile = True
                            fnum = FreeFile()
                            FileOpen(fnum, myFiles(z), OpenMode.Input)
                            Do While Not EOF(fnum)
                                textline = LineInput(fnum)
                                DisplayLogFile.ListBox1.Items.Add(textline)
                            Loop
                            FileClose(fnum)

                            For i = 0 To DisplayLogFile.ListBox1.Items.Count - 1

                                If InStr(DisplayLogFile.ListBox1.Items(i).ToString, dateandtime) > 0 Then
                                    DisplayLogFile.ListBox1.SelectedIndex = i
                                    Exit For
                                End If
                            Next i

                            Exit For
                        End If
                    Next z
                    Exit For
                End If

                If FoundFile = True Then
                    Exit For
                End If
            Next x
            If FoundFile = True Then
                Exit For
            End If
        Next y

        If FoundFile = True Then
            DisplayLogFile.Text = vehiclenumber

            DisplayLogFile.Show()

            DisplayLogFile.Button1_Click(Me, e)

        End If

        Me.Cursor = Cursors.Arrow


    End Sub

    Private Sub VehicleStatDashboard_Load(sender As Object, e As EventArgs) Handles Me.Load

        'First code executed if user indicates they wish to display the Vehicle Stat Dashboard...

        SendLiveUpdate = False

        InitForm.PopulateVehicleNumbersList()

        NetworkDrivePermission = True

        DisplayVehicleNumbers()

    End Sub

    Private Sub ListBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedIndexChanged

    End Sub

    Private Sub ListBox2_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedValueChanged

        If ListBox2.SelectedIndex > -1 Then
            VehicleNumber = ListBox2.SelectedItem.ToString
            VehicleNumberChanged = True
            If LogFileType = "PCBased" Then
                LoadLists("HostnameBasedReports\")
            Else
                LoadLists()
            End If
        End If

    End Sub

    Private Sub ListBox4_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox4.SelectedIndexChanged

    End Sub

    Private Sub ListBox4_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox4.SelectedValueChanged

        'Display the contents of the log file associated with the selection on the diplaylogfile form and higlight the selected row in the display...

        Dim tempstr() As String
        Dim myVehicleNumber As String
        Dim dateandtime As String

        Dim mySavePath As String = ""

        Dim fnum As String

        Dim textline As String

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer

        Dim FoundFile As Boolean

        Dim myDirectories() As String
        Dim myFiles() As String

        If InhibitListboxAction = True Then
            InhibitListboxAction = False
            Exit Sub
        End If

        Me.Cursor = Cursors.WaitCursor

        DisplayLogFile.ListBox1.Items.Clear()

        tempstr = Split(ListBox4.SelectedItem.ToString, ",")

        myVehicleNumber = Trim(tempstr(0))
        dateandtime = Trim(tempstr(2))

        For y = 0 To 6

            Select Case y

                Case 0
                    mySavePath = mySavepathprefix & "\LogFiles\CSAV2\"
                Case 1
                    mySavePath = mySavepathprefix & "\LogFiles\LowContent\"
                Case 2
                    mySavePath = mySavepathprefix & "\LogFiles\HighContent\"
                Case 3
                    mySavePath = mySavepathprefix & "\LogFiles\ACP2\"
                Case 4
                    mySavePath = mySavepathprefix & "\LogFiles\ACP3\"
                Case 5
                    mySavePath = mySavepathprefix & "\LogFiles\ACP4\"
                Case 6
                    mySavePath = mySavepathprefix & "\LogFiles\FCM\"

            End Select

            myDirectories = System.IO.Directory.GetDirectories(mySavePath)

            For x = 0 To UBound(myDirectories)
                If InStr(myDirectories(x), myVehicleNumber) > 0 Then
                    myFiles = Directory.GetFiles(myDirectories(x))
                    For z = 0 To UBound(myFiles)
                        If InStr(myFiles(z), "GM_ResidentClient.log") > 0 Then

                            FoundFile = True
                            fnum = FreeFile()
                            FileOpen(fnum, myFiles(z), OpenMode.Input)
                            Do While Not EOF(fnum)
                                textline = LineInput(fnum)
                                DisplayLogFile.ListBox1.Items.Add(textline)
                            Loop
                            FileClose(fnum)

                            For i = 0 To DisplayLogFile.ListBox1.Items.Count - 1
                                If InStr(DisplayLogFile.ListBox1.Items(i).ToString, dateandtime) > 0 Then
                                    DisplayLogFile.ListBox1.SelectedIndex = i
                                    Exit For
                                End If
                            Next i

                            Exit For
                        End If
                    Next z
                    Exit For
                End If

                If FoundFile = True Then
                    Exit For
                End If
            Next x
            If FoundFile = True Then
                Exit For
            End If
        Next y

        If FoundFile = True Then
            DisplayLogFile.Text = myVehicleNumber

            DisplayLogFile.Show()

            DisplayLogFile.Button1_Click(Me, e)

        End If

        Me.Cursor = Cursors.Arrow


    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        'This is the Display All Button on the main VehicleStatDashboard screen...

        Dim ReportPath As String

        VehicleNumber = ""
        VersionNumber = ""
        AdditionalSearchCriterian = ""
        SearchString = Nothing

        If MsgBox("PC Hostname based Logs?", vbYesNo) = vbYes Then
            ReportPath = "HostnameBasedReports\"
            LogFileType = "PCBased"
        Else
            ReportPath = ""
            LogFileType = "VehicleBased"
        End If

        LoadLists(ReportPath)

        DisplayVehicleNumbers()

    End Sub

    Private Sub DateTimePicker1_ValueChanged(sender As Object, e As EventArgs) Handles DateTimePicker1.ValueChanged
        EventDateTime = DateTimePicker1.Value
        StartDateTime = DateTimePicker1.Value
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ListBox5_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox5.SelectedIndexChanged

    End Sub

    Private Sub ListBox5_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox5.SelectedValueChanged



    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs) Handles Label3.Click

    End Sub

    Private Sub Label4_Click(sender As Object, e As EventArgs) Handles Label4.Click

    End Sub

    Private Sub Label6_Click(sender As Object, e As EventArgs) Handles Label6.Click

    End Sub

    Private Sub Label8_Click(sender As Object, e As EventArgs) Handles Label8.Click

    End Sub

    Private Sub Label10_Click(sender As Object, e As EventArgs) Handles Label10.Click

    End Sub

    Private Sub Label12_Click(sender As Object, e As EventArgs) Handles Label12.Click

    End Sub

    Private Sub Label14_Click(sender As Object, e As EventArgs) Handles Label14.Click

    End Sub

    Private Sub Label16_Click(sender As Object, e As EventArgs) Handles Label16.Click

    End Sub

    Private Sub Label18_Click(sender As Object, e As EventArgs) Handles Label18.Click

    End Sub

    Private Sub Label3_MouseDown(sender As Object, e As MouseEventArgs) Handles Label3.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "VEHICLE SPY REPLAY BLOCK START"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label3)

    End Sub

    Private Sub Label4_MouseDown(sender As Object, e As MouseEventArgs) Handles Label4.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "CLEVIR Hung up during Recording"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label4)

    End Sub

    Private Sub Label6_MouseDown(sender As Object, e As MouseEventArgs) Handles Label6.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "CLEVIR Kill Switch Activated"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label6)

    End Sub

    Private Sub Label8_MouseDown(sender As Object, e As MouseEventArgs) Handles Label8.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "CLEVIR Appears to have Hung up"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label8)

    End Sub

    Private Sub Label10_MouseDown(sender As Object, e As MouseEventArgs) Handles Label10.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "CAMERA INIT ISSUE"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label10)

    End Sub

    Private Sub Label12_MouseDown(sender As Object, e As MouseEventArgs) Handles Label12.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "QUESTIONABLE USER INTERACTION"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label12)
    End Sub

    Private Sub Label14_MouseDown(sender As Object, e As MouseEventArgs) Handles Label14.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "QUESTIONABLE APP EXIT"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label14)
    End Sub

    Private Sub Label16_MouseDown(sender As Object, e As MouseEventArgs) Handles Label16.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "VEHICLE SPY READ DID REPLAY BLOCK START"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label16)
    End Sub

    Private Sub Label18_MouseDown(sender As Object, e As MouseEventArgs) Handles Label18.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "VEHICLE SPY VERIFICATION ISSUE"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label18)
    End Sub

    Private Sub ListBox2_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox2.MouseDown

        If e.Button = MouseButtons.Right Then

            ListBox2.SetSelected(ListBox2.SelectedIndex, False)

            SearchString = Nothing
            VehicleNumber = ""
            If LogFileType = "PCBased" Then
                LoadLists("HostnameBasedReports\")
            Else
                LoadLists()
            End If

        End If
    End Sub

    Private Sub ListBox5_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox5.MouseDown

        If e.Button = MouseButtons.Right Then

            ListBox5.SetSelected(ListBox5.SelectedIndex, False)

        End If

    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles TextBox2.TextChanged
        saveposition = 0
    End Sub

    Private Sub TextBox2_MouseDown(sender As Object, e As MouseEventArgs) Handles TextBox2.MouseDown

        If e.Button = MouseButtons.Right Then
            AdditionalSearchCriterian = ""
            TextBox2.Text = ""
            HandleLabelMouseEvent()
        End If

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        'This is the search event list button...

        If ListBox3.Items.Count > 0 Then

            If Len(TextBox2.Text) = 0 Then
                MsgBox("Please enter a search string...")
                Exit Sub
            End If

            AdditionalSearchCriterian = TextBox2.Text
            HandleLabelMouseEvent()

        Else
            MsgBox("Nothing to search... First, Display All or Copy Log Files from Share to Local...")
        End If

    End Sub

    Private Sub Label28_MouseDown(sender As Object, e As MouseEventArgs) Handles Label28.MouseDown

        If e.Button = MouseButtons.Left Then
            AdditionalSearchCriterian = "INITIALIZATION COMPLETE ISSUE"
        Else
            AdditionalSearchCriterian = ""
        End If

        HandleLabelMouseEvent(Label28)

    End Sub

    Private Sub Label28_Click(sender As Object, e As EventArgs) Handles Label28.Click

    End Sub

    Private Sub GroupBox4_Enter(sender As Object, e As EventArgs) Handles GroupBox4.Enter

    End Sub

    Private Sub Label22_Click(sender As Object, e As EventArgs) Handles Label22.Click

    End Sub

    Private Sub ListBox6_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox6.SelectedIndexChanged

    End Sub

    Private Sub ListBox6_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox6.SelectedValueChanged

        'Display the contents of the log file associated with the selection on the diplaylogfile form and higlight the selected row in the display...

        Dim tempstr() As String
        Dim myVehicleNumber As String
        Dim dateandtime As String

        Dim mySavePath As String = ""

        Dim fnum As String

        Dim textline As String

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer

        Dim FoundFile As Boolean

        Dim myDirectories() As String
        Dim myFiles() As String

        If InhibitListboxAction = True Then
            InhibitListboxAction = False
            Exit Sub
        End If

        Me.Cursor = Cursors.WaitCursor

        DisplayLogFile.ListBox1.Items.Clear()

        tempstr = Split(ListBox6.SelectedItem.ToString, ",")

        myVehicleNumber = Trim(tempstr(0))
        dateandtime = Trim(tempstr(2))

        For y = 0 To 6

            Select Case y

                Case 0
                    mySavePath = mySavepathprefix & "\LogFiles\CSAV2\"
                Case 1
                    mySavePath = mySavepathprefix & "\LogFiles\LowContent\"
                Case 2
                    mySavePath = mySavepathprefix & "\LogFiles\HighContent\"
                Case 3
                    mySavePath = mySavepathprefix & "\LogFiles\ACP2\"
                Case 4
                    mySavePath = mySavepathprefix & "\LogFiles\ACP3\"
                Case 5
                    mySavePath = mySavepathprefix & "\LogFiles\ACP4\"
                Case 6
                    mySavePath = mySavepathprefix & "\LogFiles\FCM\"

                    'Will need to add cases here for ACP2, ACP3, ACP4, FCM...
            End Select

            myDirectories = System.IO.Directory.GetDirectories(mySavePath)

            For x = 0 To UBound(myDirectories)
                If InStr(myDirectories(x), myVehicleNumber) > 0 Then
                    myFiles = Directory.GetFiles(myDirectories(x))
                    For z = 0 To UBound(myFiles)
                        If InStr(myFiles(z), "GM_ResidentClient.log") > 0 Then

                            FoundFile = True
                            fnum = FreeFile()
                            FileOpen(fnum, myFiles(z), OpenMode.Input)
                            Do While Not EOF(fnum)
                                textline = LineInput(fnum)
                                DisplayLogFile.ListBox1.Items.Add(textline)
                            Loop
                            FileClose(fnum)

                            For i = 0 To DisplayLogFile.ListBox1.Items.Count - 1
                                If InStr(DisplayLogFile.ListBox1.Items(i).ToString, dateandtime) > 0 Then
                                    DisplayLogFile.ListBox1.SelectedIndex = i
                                    Exit For
                                End If
                            Next i

                            Exit For
                        End If
                    Next z
                    Exit For
                End If

                If FoundFile = True Then
                    Exit For
                End If
            Next x
            If FoundFile = True Then
                Exit For
            End If
        Next y

        If FoundFile = True Then
            DisplayLogFile.Text = myVehicleNumber

            DisplayLogFile.Show()

            DisplayLogFile.Button1_Click(Me, e)

        End If

        Me.Cursor = Cursors.Arrow

    End Sub

    Private Sub Label26_Click(sender As Object, e As EventArgs) Handles Label26.Click

    End Sub

    Private Sub Label26_MouseDown(sender As Object, e As MouseEventArgs) Handles Label26.MouseDown

        Dim x As Integer

        If e.Button = MouseButtons.Left Then
            For x = 0 To ListBox4.Items.Count - 1
                If InStr(ListBox4.Items(x), Label26.Text) Then
                    InhibitListboxAction = False
                    ListBox4.SelectedIndex = x
                    Exit For
                End If
            Next
        Else
            InhibitListboxAction = True
            ListBox4.SelectedIndex = -1
            ListBox4.Refresh()
        End If
    End Sub

    Private Sub Label30_Click(sender As Object, e As EventArgs) Handles Label30.Click

    End Sub

    Private Sub Label30_MouseDown(sender As Object, e As MouseEventArgs) Handles Label30.MouseDown

        Dim x As Integer

        If e.Button = MouseButtons.Left Then
            For x = 0 To ListBox6.Items.Count - 1
                If InStr(ListBox6.Items(x), Label30.Text) Then
                    InhibitListboxAction = False
                    ListBox6.SelectedIndex = x
                    Exit For
                End If
            Next
        Else
            InhibitListboxAction = True
            ListBox6.SelectedIndex = -1
            ListBox6.Refresh()
        End If

    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        'This is the Versions button.  The information displayed will be changed to include only that information that is associated with which ever software versions are selected
        'in the list box above this button.  This allows us to isolate information for a particular software version (or versions) so we can see if issues that may have been
        'present in older versions of CLEVIR software have been fixed in newer versions...

        Dim x As Integer

        NumVersionsSelected = 0

        If ListBox5.SelectedIndex > -1 Then

            For x = 0 To ListBox5.Items.Count - 1
                If ListBox5.GetSelected(x) = True Then
                    SearchString = Nothing
                    VersionNumber = ListBox5.Items(x).ToString
                    NumVersionsSelected += 1
                    If LogFileType = "PCBased" Then
                        LoadLists("HostnameBasedReports\")
                    Else
                        LoadLists()
                    End If
                End If
            Next

        Else
            SearchString = Nothing
            VersionNumber = ""
            LoadLists()
        End If

    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click

        Dim x As Integer

        If VehicleNumberChanged = True Then
            VehicleNumberChanged = False
            saveposition = 0
        End If

        If DisplayLogFile.ListBox1.Items.Count > 0 Then
            For x = saveposition To DisplayLogFile.ListBox1.Items.Count - 1
                If InStr(DisplayLogFile.ListBox1.Items(x).ToString, TextBox2.Text) > 0 Then
                    DisplayLogFile.ListBox1.SetSelected(x, True)
                    saveposition = x + 1
                    Exit For
                End If
            Next

            DisplayLogFile.Activate()

        End If
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click

        'Copy PC Logs from Q - Button on main screen...

        LogFileType = "PCBased"
        CreateListsFromFileContent("HostnameBasedReports\")
        HandlePCBasedLogFiles()

    End Sub

    Private Sub DisplayVehicleHostNamesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DisplayVehicleHostNamesToolStripMenuItem.Click

        RunNotepad("Q:" & CLEVIRBaseDir & "\PCNetworkConnectStatus\Updated_PCInfoSAVE.csv")

    End Sub

    Private Sub DecryptFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DecryptFileToolStripMenuItem.Click

        StrFileToDecrypt = SelectFile(OpenFileDialog1, My.Application.Info.DirectoryPath, "encrypt", True)
        Decrypt()

        MsgBox("Decryption Complete for " & StrFileToDecrypt)

    End Sub

    Private Sub FindEncryptedFilesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FindEncryptedFilesToolStripMenuItem.Click


    End Sub

    Private Sub CopyFlashInfoToFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CopyFlashInfoToFileToolStripMenuItem.Click
        CopyFlashInfoTofile()
    End Sub

    Private Sub ReadFlashInfoFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ReadFlashInfoFileToolStripMenuItem.Click
        RunNotepad("Q:" & CLEVIRBaseDir & "\VehicleFlashInfo\VehicleFlashInfo.csv")
    End Sub

    Private Sub ReadChromeConnectedVehiclesFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ReadChromeConnectedVehiclesFileToolStripMenuItem.Click
        RunNotepad(My.Application.Info.DirectoryPath & "\Chrome Connected Vehicles.txt")
    End Sub

    Private Sub FindAndDecryptToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FindAndDecryptToolStripMenuItem.Click

        Dim x As Integer
        Dim z As Integer
        Dim myfiles() As String
        'Dim myfilesList As New List(Of String)
        Dim mydirectories() As String
        Dim mysubdirectories() As String

        Dim BaseDirectory(0 To 4) As String

        Dim DataFileCount As Integer
        Dim FrontVideoFileCount As Integer

        Dim TotalSubFolders As Long
        Dim TotalVideoFileMismatches As Long

        Try

            DisplayLogFile.Show()
            DisplayLogFile.ListBox2.BringToFront()
            DisplayLogFile.ListBox1.SendToBack()
            DisplayLogFile.ListBox2.Items.Clear()
            DisplayLogFile.Refresh()

            Me.Cursor = Cursors.WaitCursor

            If CSAV2ToolStripMenuItem.Checked = True Then
                BaseDirectory(0) = "Q:\"
            End If

            If HighContentToolStripMenuItem.Checked = True Then
                BaseDirectory(1) = "Q:\HighContent\VehicleData\"
                BaseDirectory(2) = "Q:\HighContent\RideData\"
            End If

            If LowContentToolStripMenuItem.Checked = True Then
                BaseDirectory(3) = "Q:\LowContent\VehicleData\"
            End If

            For i = 0 To 3

                If Len(BaseDirectory(i)) > 0 Then

                    mydirectories = System.IO.Directory.GetDirectories(BaseDirectory(i), "gmcsv*", 0)

                    For z = 0 To UBound(mydirectories)

                        DisplayLogFile.ListBox2.Items.Add(mydirectories(z))
                        DisplayLogFile.ListBox2.SelectedIndex = DisplayLogFile.ListBox2.Items.Count - 1
                        DisplayLogFile.ListBox2.Refresh()

                        myfiles = Directory.GetFiles(mydirectories(z), "*.encrypt")

                        For x = 0 To UBound(myfiles)

                            If InStr(myfiles(x), ".dat") = 0 Then

                                If FindAndDecryptToolStripMenuItem.Checked = True Then
                                    StrFileToDecrypt = myfiles(x)
                                    HandleUserMessageLogging("GMRC", "Decrypting " & myfiles(x),,,, DisplayLogFile.ListBox2)
                                    Decrypt()
                                    HandleUserMessageLogging("GMRC", "Decrypted " & myfiles(x),,,, DisplayLogFile.ListBox2)
                                Else
                                    HandleUserMessageLogging("GMRC", myfiles(x))
                                End If

                            End If

                        Next x

                        myfiles = Directory.GetFiles(mydirectories(z), "*.log")

                        For x = 0 To UBound(myfiles)

                            If InStr(myfiles(x), "GM_INCA_Comm") > 0 Then
                                File.Delete(myfiles(x))
                            End If

                        Next x

                        mysubdirectories = System.IO.Directory.GetDirectories(mydirectories(z))

                        For x = 0 To UBound(mysubdirectories)

                            TotalSubFolders += 1

                            If InStr(mysubdirectories(x), "2020") > 0 Or InStr(mysubdirectories(x), "2019") > 0 Then

                                DisplayLogFile.ListBox2.Items.Add(mysubdirectories(x))
                                DisplayLogFile.ListBox2.SelectedIndex = DisplayLogFile.ListBox2.Items.Count - 1
                                DisplayLogFile.ListBox2.Refresh()

                                myfiles = Directory.GetFiles(mysubdirectories(x), "*.encrypt")

                                For y = 0 To UBound(myfiles)

                                    If InStr(myfiles(y), ".dat") = 0 Then
                                        If FindAndDecryptToolStripMenuItem.Checked = True Then

                                            If Not File.Exists(Mid(myfiles(y), 1, InStr(myfiles(y), ".encrypt") - 1)) Then

                                                StrFileToDecrypt = myfiles(y)
                                                HandleUserMessageLogging("GMRC", "Decrypting " & myfiles(y),,,, DisplayLogFile.ListBox2)
                                                Decrypt()
                                                HandleUserMessageLogging("GMRC", "Decrypted " & myfiles(y),,,, DisplayLogFile.ListBox2)
                                            Else
                                                'If MsgBox("Delete " & myfiles(y) & "?", vbYesNo) = vbYes Then
                                                File.Delete(myfiles(y))
                                                ' End If
                                            End If

                                        Else
                                            HandleUserMessageLogging("GMRC", myfiles(y))
                                        End If
                                    End If

                                Next y

                                DataFileCount = 0
                                FrontVideoFileCount = 0
                                myfiles = Directory.GetFiles(mysubdirectories(x))

                                For y = 0 To UBound(myfiles)
                                    If InStr(myfiles(y), ".zip") > 0 And InStr(myfiles(y), "VehicleSpy") = 0 Then
                                        DataFileCount += 1
                                    End If
                                    If InStr(UCase(myfiles(y)), "FRONT.MP4") > 0 And InStr(myfiles(y), "convert.log") = 0 Then
                                        FrontVideoFileCount += 1
                                    End If

                                    If InStr(myfiles(y), "convert.log") > 0 Or InStr(myfiles(y), "attach.log") > 0 Then
                                        File.Delete(myfiles(y))
                                    End If
                                Next y

                                If DataFileCount > FrontVideoFileCount Then
                                    TotalVideoFileMismatches += 1
                                    HandleUserMessageLogging("GMRC", "*** Data File Count does not match Video File Count - in subfolder " & mysubdirectories(x) & " - " & TotalSubFolders & " - " & TotalVideoFileMismatches & " ***",,,, DisplayLogFile.ListBox2)
                                End If

                            End If

                            System.Windows.Forms.Application.DoEvents()

                        Next x

                    Next z

                End If

            Next i

            HandleUserMessageLogging("GMRC", "TotalSubFolders " & TotalSubFolders & " - TotalVideoFileMismatches " & TotalVideoFileMismatches,,,, DisplayLogFile.ListBox2)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "SearechToolStripMenuItem: " & ex.Message,,,, DisplayLogFile.ListBox2)
            System.Windows.Forms.Application.DoEvents()
        End Try

        Me.Cursor = Cursors.Arrow
    End Sub

    Private Sub CreateAggregateAnnotationFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CreateAggregateAnnotationFileToolStripMenuItem.Click
        CreateAggregateAnnotationFile()
    End Sub

    Private Sub UpdatePPAggregateAnnotationFilesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles UpdatePPAggregateAnnotationFilesToolStripMenuItem.Click
        Update_PP_AggregateAnnotationsFiles()
    End Sub

    Private Sub ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem1.Click

        CSAV2ToolStripMenuItem.Checked = True
        HighContentToolStripMenuItem.Checked = True
        LowContentToolStripMenuItem.Checked = True

    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        EnableMyBackgroundTasks = False
        Me.Close()
    End Sub

    Private Sub ListBox3_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox3.MouseDown

        Dim fnum As Integer
        Dim filename As String = My.Application.Info.DirectoryPath & "\DashboardEventList.csv"

        If e.Button = MouseButtons.Right Then

            fnum = FreeFile()
            FileOpen(fnum, filename, OpenMode.Output)

            For x = 0 To ListBox3.Items.Count - 1
                PrintLine(fnum, ListBox3.Items(x).ToString)
            Next

            FileClose(fnum)
        End If
    End Sub

    Private Sub ListBox1_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedValueChanged

        'Run notepad and open up the GM_ResidentClient.log file assocated with the vehicle number selected...


        Dim Cnt As Integer
        Dim tempstr() As String

        GetLocalFilePaths()

        For Cnt = 0 To UBound(LocalLogFilePaths)

            tempstr = Split(ListBox1.SelectedItem.ToString, ",")

            If InStr(LocalLogFilePaths(Cnt), tempstr(0)) > 0 Then
                RunNotepad(LocalLogFilePaths(Cnt) & "\GM_ResidentClient.log")
                Exit Sub
            End If

        Next Cnt

    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click

        'Find in all files button.

        FindStringInAllFiles()

    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click

        'Copy Listbox contents to clipboard button.

        Dim mySym As String = ""
        Dim i As Long
        ' Dim strClipText As String
        Dim strInputText As String

        For i = 0 To ListBox1.Items.Count - 1
            mySym &= ListBox1.Items(i).ToString
            mySym &= vbCrLf
        Next


        'strClipText = New DataObject
        strInputText = mySym

        'strClipText.SetText(strInputText)
        Clipboard.SetText(mySym)
        'strClipText.PutInClipboard
    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As DoWorkEventArgs) Handles BackgroundWorker1.DoWork

        _BackgroundTasks = New BackgroundTasks(AddressOf MyBackgroundTasks)
        BeginInvoke(_BackgroundTasks)

    End Sub

    Private Sub VehicleStatDashboard_Shown(sender As Object, e As EventArgs) Handles Me.Shown

        Dim tempstr() As String

        ComboBox1.Items.Clear()

        ComboBox1.Items.Add(" All")

        For y = 1 To InitForm.VehicleNumbersList.Count - 1
            tempstr = Split(InitForm.VehicleNumbersList(y), ",")
            ComboBox1.Items.Add(tempstr(0))
        Next

        Me.Refresh()

    End Sub

    Private Sub VehicleStatDashboard_Closed(sender As Object, e As EventArgs) Handles Me.Closed

    End Sub

    Private Sub VehicleStatDashboard_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        EnableMyBackgroundTasks = False
        'SetAvailability(NetworkDriveMapping & ClevirBaseDir & "\Development\PC_HostNameLogFiles", False, Button11)
    End Sub

    Private Sub VehicleStatDashboard_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing

    End Sub

    Private Sub ListBox7_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox7.SelectedIndexChanged

    End Sub

    Private Sub ToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem2.Click
        CopyNewCLEVIRConfigFilesToQ()

    End Sub

    Private Sub CreateKeepListFromINCAGeneratedFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CreateKeepListFromINCAGeneratedFileToolStripMenuItem.Click
        CreateKeepListFromINCAGeneratedFile()
    End Sub

    Private Sub ListCurrentSignalListNamesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ListCurrentSignalListNamesToolStripMenuItem.Click
        ListCurrentSignalListNames()
    End Sub

    Private Sub MergeKeepListsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MergeKeepListsToolStripMenuItem.Click
        MergeKeepLists()
    End Sub

    Private Sub AddSignalsToSelectedExperimentsFromFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AddSignalsToSelectedExperimentsFromFileToolStripMenuItem.Click
        AddOrRemoveSignals()
    End Sub

    Private Sub ListBox8_SelectedIndexChanged(sender As Object, e As EventArgs)
        If ListBox8.SelectedItems.Count > 0 Then
            AddSignalsToSelectedExperimentsFromFileToolStripMenuItem.Enabled = True
        Else
            AddSignalsToSelectedExperimentsFromFileToolStripMenuItem.Enabled = False
        End If
    End Sub

    Private Sub ListBox8_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles ListBox8.SelectedIndexChanged

    End Sub

    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
        GroupBox5.Visible = False
        ListBox8.SelectionMode = SelectionMode.One
        ListBox8.SelectedIndex = -1

    End Sub

    Private Sub VehicleStatDashboard_Activated(sender As Object, e As EventArgs) Handles Me.Activated

    End Sub

    Private Sub VehicleStatDashboard_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint

    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click

        If EnableMyBackgroundTasks = False Then

            Me.Cursor = Cursors.WaitCursor

            ComboBox1.Text = " All"

            EnableMyBackgroundTasks = True
            BackgroundWorker1.RunWorkerAsync()

            Me.Cursor = Cursors.Arrow

        End If

    End Sub

    Private Sub VehicleStatDashboard_Validated(sender As Object, e As EventArgs) Handles Me.Validated

    End Sub

    Private Sub VehicleStatDashboard_SizeChanged(sender As Object, e As EventArgs) Handles Me.SizeChanged

    End Sub

    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs) Handles TextBox3.TextChanged

    End Sub

    Private Sub TextBox3_Leave(sender As Object, e As EventArgs) Handles TextBox3.Leave

    End Sub

    Private Sub TextBox3_KeyPress(sender As Object, e As KeyPressEventArgs) Handles TextBox3.KeyPress

        If (Convert.ToInt32(e.KeyChar)) = 13 Then
            If IsNumeric(TextBox3.Text) Then
                If MsgBox("Update data from " & TextBox3.Text & " days back?", vbYesNo) = vbYes Then
                    ReadVehicleStatusInfo(TextBox3.Text)
                End If
            End If

            e.Handled = True 'This prevents the beep to sound
            Me.SelectNextControl(CType(sender, Control), True, True, True, True)

        End If
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged

    End Sub

    Private Sub ComboBox1_SelectedValueChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedValueChanged
        ReadVehicleStatusInfo(ComboBox1.Text)
    End Sub

    Private Sub ListBox7_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox7.SelectedValueChanged

    End Sub

    Private Sub ListBox7_MouseClick(sender As Object, e As MouseEventArgs) Handles ListBox7.MouseClick
        Dim tempstr() As String

        tempstr = Split(ListBox7.SelectedItem.ToString, " ")
        ComboBox1.Text = tempstr(3)
    End Sub

    Private Sub CopyFilesFromShareToLocalToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CopyFilesFromShareToLocalToolStripMenuItem.Click
        Me.Cursor = Cursors.WaitCursor
        CopyFilesFromShareToLocal("Q:\CSAV2 Tools\CLEVIR", "W:\CSAV2 Tools\CLEVIR")
        RemoveUnusedFoldersFromLocal("Q:\CSAV2 Tools\CLEVIR", "W:\CSAV2 Tools\CLEVIR")
        Me.Cursor = Cursors.Arrow
    End Sub

    Private Sub Button11_Click(sender As Object, e As EventArgs) Handles Button11.Click

        If Button11.Text = "Available" Then
            Button11.Text = "Un-Available"
            Button11.BackColor = SystemColors.Control
            'SetAvailability(NetworkDriveMapping & ClevirBaseDir & "\Development\PC_HostNameLogFiles", False, Button11)
        Else
            Button11.Text = "Available"
            Button11.BackColor = Color.LightGreen
            'SetAvailability(NetworkDriveMapping & ClevirBaseDir & "\Development\PC_HostNameLogFiles", True, Button11)
        End If

    End Sub

    Private Sub ShowNetworkConnectedPCsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ShowNetworkConnectedPCsToolStripMenuItem.Click

        TurnOffShowNetworkConnectedPCs = False
        ShowNetworkConnectedPCs()
    End Sub

    Private Sub CheckForCalibrationFolderChangesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CheckForCalibrationFolderChangesToolStripMenuItem.Click
        CheckForCalibrationFolderChanges()
    End Sub
End Class