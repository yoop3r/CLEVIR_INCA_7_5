Option Strict Off
Option Explicit On

Imports System.Diagnostics
Imports VB = Microsoft.VisualBasic
Imports System.IO
Imports System.Runtime.InteropServices

'Imports SevenZip

Module GenericAnyCLEVIRToolSuiteApp 'GlobalCommonModule

    'This module contains routines that may be shared between applications.  Certain functions are applicable to both CLEVIR and FLASHOMATIC for
    'instance, so both applications referene this code file as a link from the same location thereby allowing applicable code to be modified
    'in one place for use by multiple applications...

    Public LocalVehicleConfigFileModifyDate As Date

    Public ReadOnly DisplayMsgBox As Boolean = True
    Public SendLiveUpdate As Boolean = True
    Public ReadOnly FlashMsgOn As Short = 0
    Public ReadOnly FlashMsg1Sec As Short = 1000
    Public ReadOnly FlashMsg2Sec As Short = 2000
    Public ReadOnly FlashMsg3Sec As Short = 3000
    Public ReadOnly FlashMsg4Sec As Short = 4000
    Public ReadOnly FlashMsg5Sec As Short = 5000

    ' Error Constants:

    Private Const NO_ERROR = 0

    Private Const ERROR_ACCESS_DENIED = 5&
    Private Const ERROR_ALREADY_ASSIGNED = 85&
    Public Const ERROR_BAD_DEV_TYPE = 66&
    Public Const ERROR_BAD_DEVICE = 1200&
    Public Const ERROR_BAD_NET_NAME = 67&
    Public Const ERROR_BAD_PROFILE = 1206&
    Public Const ERROR_BAD_PROVIDER = 1204&
    Public Const ERROR_BUSY = 170&
    Public Const ERROR_CANCELLED = 1223&
    Public Const ERROR_CANNOT_OPEN_PROFILE = 1205&
    Public Const ERROR_DEVICE_ALREADY_REMEMBERED = 1202&
    Public Const ERROR_EXTENDED_ERROR = 1208&
    Private Const ERROR_INVALID_PASSWORD = 86&
    Public Const ERROR_NO_NET_OR_BAD_PATH = 1203&

    Private Const ForceDisconnect As Integer = 1
    Private Const RESOURCETYPE_DISK As Long = &H1
    Public Const ERROR_BAD_NETPATH As Long = 53&
    Public Const ERROR_NETWORK_ACCESS_DENIED As Long = 65&
    Public Const ERROR_NETWORK_BUSY As Long = 54&

    Public Const EWX_LOGOFF As Long = &H0
    Public Const EwxShutdown As Long = &H1
    Public Const EWX_REBOOT As Long = &H2
    Public Const EwxForce As Long = &H4
    Public Const EwxPoweroff As Long = &H8
    Public Const EwxForceIFHUNG As Long = &H10

    Const ANYSIZE_ARRAY As Integer = 1
    Const TOKEN_QUERY As Integer = &H8
    Const TOKEN_ADJUST_PRIVILEGES As Integer = &H20
    Const SE_SHUTDOWN_NAME As String = "SeShutdownPrivilege"
    Const SE_PRIVILEGE_ENABLED As Integer = &H2

    <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)>
    Private Structure LUID
        Public LowPart As UInt32
        Public HighPart As UInt32
    End Structure

    <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)>
    Private Structure LUID_AND_ATTRIBUTES
        Public Luid As LUID
        Public Attributes As UInt32
    End Structure

    <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)>
    Private Structure TOKEN_PRIVILEGES
        Public PrivilegeCount As UInt32
        <System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst:=ANYSIZE_ARRAY)>
        Public Privileges() As LUID_AND_ATTRIBUTES
    End Structure

    <System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError:=True)>
    Private Function LookupPrivilegeValue(
     ByVal lpSystemName As String,
     ByVal lpName As String,
     ByRef lpLuid As LUID
      ) As Boolean
    End Function

    <System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError:=True)>
    Private Function OpenProcessToken(
     ByVal ProcessHandle As IntPtr,
     ByVal DesiredAccess As Integer,
     ByRef TokenHandle As IntPtr
      ) As Boolean
    End Function

    <System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError:=True)>
    Private Function CloseHandle(ByVal hHandle As IntPtr) As Boolean
    End Function

    <System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError:=True)>
    Private Function AdjustTokenPrivileges(
       ByVal TokenHandle As IntPtr,
       ByVal DisableAllPrivileges As Boolean,
       ByRef NewState As TOKEN_PRIVILEGES,
       ByVal BufferLength As Integer,
       ByRef PreviousState As TOKEN_PRIVILEGES,
       ByRef ReturnLength As IntPtr
     ) As Boolean
    End Function

    Private Structure NETRESOURCE
        Public dwScope As Integer
        Public dwType As Integer
        Public dwDisplayType As Integer
        Public dwUsage As Integer
        Public lpLocalName As String
        Public lpRemoteName As String
        Public lpComment As String
        Public lpProvider As String
    End Structure

    Private Declare Function WNetAddConnection2 Lib "mpr.dll" Alias "WNetAddConnection2A" (ByRef lpNetResource As NETRESOURCE, ByVal lpPassword As String, ByVal lpUserName As String, ByVal dwFlags As Integer) As Integer
    Private Declare Function WNetCancelConnection2 Lib "mpr" Alias "WNetCancelConnection2A" (ByVal lpName As String, ByVal dwFlags As Integer, ByVal fForce As Integer) As Integer

    Public Declare Function ExitWindows _
        Lib "User32" Alias "ExitWindowsEx" _
        (ByVal dwOptions As Long, ByVal dwReserved As Long) As Long

    Public Declare Function mciSendString Lib "winmm.dll" Alias "mciSendStringA" (ByVal lpstrCommand As String, ByVal lpstrReturnString As String, ByVal uReturnLength As Integer, ByVal hwndCallback As Integer) As Integer

    <DllImport("user32.dll")>
    Public Function SetForegroundWindow(ByVal hWnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    Public Structure POINTAPI

        Dim X As Integer
        Dim Y As Integer

    End Structure

    Public Structure RECT

        Dim Left_Renamed As Integer
        Dim Top_Renamed As Integer
        Dim Right_Renamed As Integer
        Dim Bottom_Renamed As Integer

    End Structure

    Public Structure WINDOWPLACEMENT

        Dim length As Integer

        Dim flags As Integer

        Dim showCmd As Integer

        Dim ptMinPosition As POINTAPI

        Dim ptMaxPosition As POINTAPI

        Dim rcNormalPosition As RECT

    End Structure

    Private Structure VehicleConfigDirStatus
        Dim VehicleConfigFiles() As String
        Dim FileModifyDates() As Date
        Dim ActiveFile As String
        Dim ActiveFileDate As Date
    End Structure

    Private myVehicleConfigDirStatus() As VehicleConfigDirStatus

    Public Declare Function FindWindow Lib "user32" Alias "FindWindowA" (ByVal lpClassName As String, ByVal lpWindowName As String) As Integer

    Public Declare Function GetWindowPlacement Lib "user32" (ByVal hwnd As Integer, ByRef lpwndpl As WINDOWPLACEMENT) As Integer

    Public Declare Function SetWindowPlacement Lib "user32" (ByVal hwnd As Integer, ByRef lpwndpl As WINDOWPLACEMENT) As Integer

    Public Const SW_SHOWMINIMIZED As Short = 2

    Public Const SW_SHOWMAXIMIZED As Short = 3

    Public Const SW_SHOWNORMAL As Short = 1

    Public updatingVehicleList As Boolean

    Public Username As String

    Private Sub HandleProfsFiles(ByVal ProfsFile1 As String, ByVal ProfsFile2 As String)

        'Handles unzipping primary profs file in c:\temp folder and copying profs override files
        'over originally unzipped files, this for new ACP3 Profs...

        Dim dir As New DirectoryInfo("C:\Temp")
        Dim files As FileInfo()
        Dim dirs As DirectoryInfo() = dir.GetDirectories()
        Dim dirlist As ArrayList
        Dim x As Integer
        Dim OverrideFile As String
        Dim NewProfsDirectory As String = Nothing

        Try

            dirlist = New ArrayList

            For x = 0 To UBound(dirs)
                dirlist.Add(dirs(x).Name)
            Next

            HandleUserMessageLogging("GMRC", "HandleProfsFiles: Unzipping Profs Folder in C:\temp...",, )

            'If InStr(ProfsFile1, "Override_Files") = 1 Then
            If InStr(ProfsFile1, "_Override") > 0 Then 'changed in 5.6.6 - seemed to work, but not correct code, also changed instr compare based to accomodate new name of override file.
                OverrideFile = ProfsFile1
                UnzipFolder(ProfsFile2)

            Else
                OverrideFile = ProfsFile2
                UnzipFolder(ProfsFile1)
            End If

            dirs = dir.GetDirectories

            For x = 0 To UBound(dirs)
                If dirlist.Contains(dirs(x).Name) = False Then
                    NewProfsDirectory = dirs(x).FullName
                    Exit For
                End If
            Next

            HandleUserMessageLogging("GMRC", "HandleProfsFiles: Copying and unzipping Profs Override File...",, )

            File.Copy(OverrideFile, NewProfsDirectory & "\" & Path.GetFileName(OverrideFile))

            UnzipFile(NewProfsDirectory & "\" & Path.GetFileName(OverrideFile))

            dir = New DirectoryInfo(NewProfsDirectory)

            files = dir.GetFiles

            HandleUserMessageLogging("GMRC", "HandleProfsFiles: Replacing Profs files with Unzipped Override files...",, )

            For Each file In files
                If InStr(file.Name, ".zip") = 0 And InStr(file.Name, ".ini") = 0 Then
                    If Directory.Exists(NewProfsDirectory & "\Prof\Profe") Then
                        System.IO.File.Copy(file.FullName, NewProfsDirectory & "\Prof\Profe\" & file.Name, True)
                    ElseIf Directory.Exists(NewProfsDirectory & "\Profe") Then
                        System.IO.File.Copy(file.FullName, NewProfsDirectory & "\Profe\" & file.Name, True)
                    Else
                        HandleUserMessageLogging("GMRC", "HandleProfsFiles: Replacing Profs files with Unzipped Override files Failed, Directory not found...",, )
                    End If
                End If
            Next

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleProfsFiles: " & ex.Message,, )
        End Try

    End Sub

    Public Sub checkvehicleconfigfiles(ByVal mylistbox As ListBox, ByVal WhoAmI As String)

        'Changed instances of NetworkDriveLetter to NetworkDriveMapping 02/14/2021

        'In CLEVIR, This is called from the Update Vehicle Number List button on the InitForm...
        'In FLASHOMATIC, This is called from the Update Vehicleconfigurations.csv button 

        'checkvehicleconfigfiles goes to the share drive and finds all vehicleconfigurations.csv files from all vehicles 
        'and extracts vehicle specific contents to build an updated local copy of vehicleconfiguraitons.csv based on the
        'latest vehicle configuration info on the share drive.  It then uses this updated vehicleconfigurations.csv file
        'to re-build the list of available vehicles...

        Dim fnum As Integer
        Dim textline As String = ""
        Dim NEW_FILE_FORMAT As Boolean = False
        Dim lineitems() As String
        Dim tempstrarray() As String

        Dim vehiclenumbers As String = ""

        Dim filename As String

        Dim NewFileFormat() As String = Nothing

        Dim fnum2 As Integer

        Dim LocalVehicleConfigurationArrayList As ArrayList
        Dim UpdatedVehicleConfigurationArrayList As ArrayList

        Dim x As Integer
        Dim y As Integer

        Dim VehicleNumberCountDifference As Boolean
        Dim FileUpdateRequired As Boolean

        Dim ValidDirectoryCount As Integer
        Dim ListboxCount As Integer
        Dim AtLeastOneFileHasBeenUpdated As Boolean

        Dim UpdatedFilesList As List(Of String)
        Dim NewVehicleDirectory As List(Of String)
        Dim VehicleNumbersInLocalList As List(Of String)
        Dim tempstr() As String
        Dim NewLine As String
        Dim FoundConfigFile As Boolean

        Dim ShareDriveFileModifiedDate As Date = Nothing
        Dim FoundOrigFile As Boolean
        Dim FoundNFFile As Boolean

        Try

            If NetworkDrivePermission = False Then
                HandleUserMessageLogging("GMRC", "checkvehicleconfigfiles: Could not access " & NetworkDriveMapping & ClevirBaseDir & ". Exiting...", DisplayMsgBox)
                Exit Sub
            End If

            HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles called...",, )

            updatingVehicleList = True

            'First we need to make sure that the vehicle configurations directory exists and can be found using the defined NetworkDriveMapping...
            If System.IO.Directory.Exists(NetworkDriveMapping & ClevirBaseDir & "\Development\CLEVIR Vehicle Configurations") Then

                HandleUserMessageLogging("GMRC", "Updating Vehicle Configuration Information...",,, FlashMsgOn)

                If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\VehicleConfigurationsNF.csv") Then
                    filename = My.Application.Info.DirectoryPath & "\VehicleConfigurationsNF.csv"
                Else
                    filename = My.Application.Info.DirectoryPath & "\VehicleConfigurations.csv"
                End If

                LocalVehicleConfigurationArrayList = New ArrayList
                UpdatedVehicleConfigurationArrayList = New ArrayList

                VehicleNumbersInLocalList = New List(Of String)

                'Copy contents of local file into LocalVehicleConfigurationArrayList for use below...
                fnum = FreeFile()
                FileOpen(fnum, filename, OpenMode.Input)

                Do While Not EOF(fnum)
                    NewLine = LineInput(fnum)
                    tempstr = Split(NewLine, ",")
                    LocalVehicleConfigurationArrayList.Add(NewLine)
                    If Not InStr(tempstr(0), "Vehicle Number") > 0 Then
                        VehicleNumbersInLocalList.Add(tempstr(0))
                    End If
                Loop
                FileClose(fnum)

                Dim dir As New DirectoryInfo(NetworkDriveMapping & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations")
                Dim files As FileInfo()
                Dim dirs As DirectoryInfo() = dir.GetDirectories()

                'Get number of vehicles currently defined in local vehicleconfigurations.csv file (this vehicleconfigurations.csv file used to populate listbox on initialization)
                'This will be used later to compare the number of vehicled defined in the local file, with the number of vehicle folders that are on
                'the share drive.  If the numbers are different, we will need to modify the local file...
                'If CLEVIR calls this routine we need to subtract 1 from total count because of the " VEHICLE ID NOT IN LIST" entry in the list box...
                If WhoAmI = "CLEVIR" Then
                    ListboxCount = mylistbox.Items.Count - 1
                Else
                    'if FLASHOMATIC, which also uses this routine, calls it we do not add the VEHICLE ID NOT IN LIST on initial load of listbox,
                    'so ListboxCount does not have to be reduced by 1...
                    ListboxCount = mylistbox.Items.Count
                End If

                mylistbox.Items.Clear()

                'Add VEHICLE ID NOT IN LIST as first listbox entry (If not FLASHOMATIC), this is used only by CLEVIR when configuring a new vehicle number...
                If WhoAmI = "CLEVIR" Then
                    mylistbox.Items.Add(" VEHICLE ID NOT IN LIST")
                End If

                UpdatedFilesList = New List(Of String)
                NewVehicleDirectory = New List(Of String)

                'Get number of valid vehicle configurations in " & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations
                For x = 0 To UBound(dirs)
                    FoundConfigFile = False
                    FoundNFFile = False
                    FoundOrigFile = False
                    ShareDriveFileModifiedDate = Nothing

                    'First we determine which config files are in the directory.  Could be one or the other or both...
                    files = dirs(x).GetFiles("vehicleconfigurations.csv")
                    If files.Length = 1 Then
                        ReDim Preserve myVehicleConfigDirStatus(x)
                        ReDim Preserve myVehicleConfigDirStatus(x).VehicleConfigFiles(0)
                        ReDim Preserve myVehicleConfigDirStatus(x).FileModifyDates(0)
                        myVehicleConfigDirStatus(x).VehicleConfigFiles(0) = files(0).FullName
                        myVehicleConfigDirStatus(x).FileModifyDates(0) = files(0).LastWriteTime
                        FoundOrigFile = True
                    End If

                    'If filename = My.Application.Info.DirectoryPath & "\VehicleConfigurationsNF.csv" Then

                    files = dirs(x).GetFiles("vehicleconfigurationsNF.csv")
                    If files.Length = 1 Then
                        ReDim Preserve myVehicleConfigDirStatus(x)
                        ReDim Preserve myVehicleConfigDirStatus(x).VehicleConfigFiles(1)
                        ReDim Preserve myVehicleConfigDirStatus(x).FileModifyDates(1)
                        myVehicleConfigDirStatus(x).VehicleConfigFiles(1) = files(0).FullName
                        myVehicleConfigDirStatus(x).FileModifyDates(1) = files(0).LastWriteTime
                        HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles: NF File Found " & files(0).FullName)
                        FoundNFFile = True
                    End If

                    'End If

                    'Here, based on which files are in the directory, we will determine which file to use based on modify date...
                    If UBound(myVehicleConfigDirStatus(x).FileModifyDates) = 0 Then 'This means only vehicleconfigurations.csv file in directory...
                        ShareDriveFileModifiedDate = myVehicleConfigDirStatus(x).FileModifyDates(0)
                        myVehicleConfigDirStatus(x).ActiveFile = myVehicleConfigDirStatus(x).VehicleConfigFiles(0)
                        myVehicleConfigDirStatus(x).ActiveFileDate = myVehicleConfigDirStatus(x).FileModifyDates(0)
                    ElseIf UBound(myVehicleConfigDirStatus(x).FileModifyDates) = 1 Then
                        If Len(myVehicleConfigDirStatus(x).VehicleConfigFiles(0)) > 0 And Len(myVehicleConfigDirStatus(x).VehicleConfigFiles(1)) > 0 Then
                            If myVehicleConfigDirStatus(x).FileModifyDates(0) > myVehicleConfigDirStatus(x).FileModifyDates(1) Then
                                ShareDriveFileModifiedDate = myVehicleConfigDirStatus(x).FileModifyDates(0)
                                myVehicleConfigDirStatus(x).ActiveFile = myVehicleConfigDirStatus(x).VehicleConfigFiles(0)
                                myVehicleConfigDirStatus(x).ActiveFileDate = myVehicleConfigDirStatus(x).FileModifyDates(0)
                            Else
                                ShareDriveFileModifiedDate = myVehicleConfigDirStatus(x).FileModifyDates(1)
                                myVehicleConfigDirStatus(x).ActiveFile = myVehicleConfigDirStatus(x).VehicleConfigFiles(1)
                                myVehicleConfigDirStatus(x).ActiveFileDate = myVehicleConfigDirStatus(x).FileModifyDates(1)
                            End If
                        ElseIf Len(myVehicleConfigDirStatus(x).VehicleConfigFiles(0)) > 0 Then
                            ShareDriveFileModifiedDate = myVehicleConfigDirStatus(x).FileModifyDates(0)
                            myVehicleConfigDirStatus(x).ActiveFile = myVehicleConfigDirStatus(x).VehicleConfigFiles(0)
                            myVehicleConfigDirStatus(x).ActiveFileDate = myVehicleConfigDirStatus(x).FileModifyDates(0)
                        ElseIf Len(myVehicleConfigDirStatus(x).VehicleConfigFiles(1)) > 0 Then
                            ShareDriveFileModifiedDate = myVehicleConfigDirStatus(x).FileModifyDates(1)
                            myVehicleConfigDirStatus(x).ActiveFile = myVehicleConfigDirStatus(x).VehicleConfigFiles(1)
                            myVehicleConfigDirStatus(x).ActiveFileDate = myVehicleConfigDirStatus(x).FileModifyDates(1)
                        End If
                    End If

                    If FoundNFFile = True Or FoundOrigFile = True Then

                        'Flag if any configuration file has a date newer than the local file, this means we require a change to the local file...
                        If ShareDriveFileModifiedDate > LocalVehicleConfigFileModifyDate Then
                            AtLeastOneFileHasBeenUpdated = True
                            UpdatedFilesList.Add(dirs(x).Name)
                            HandleUserMessageLogging("GMRC", dirs(x).Name & " vehicle configuration modify date is newer than local file.")
                        End If

                        If Not VehicleNumbersInLocalList.Contains(UCase(dirs(x).Name)) And Not VehicleNumbersInLocalList.Contains(dirs(x).Name) Then
                            NewVehicleDirectory.Add(dirs(x).Name)
                        End If

                        ValidDirectoryCount += 1
                        'Here we add the vehicle number (which is the dir.name), to the vehicle numbers listbox
                        mylistbox.Items.Add(dirs(x).Name)
                        mylistbox.SelectedIndex = mylistbox.Items.Count - 1
                        mylistbox.Refresh()
                        FoundConfigFile = True

                    End If

                    If FoundConfigFile = False Then
                        UpdatedFilesList.Add(dirs(x).Name)
                    End If

                Next x

                'If the count is different, we will need to update local file...
                If ValidDirectoryCount <> ListboxCount Then
                    VehicleNumberCountDifference = True
                    HandleUserMessageLogging("GMRC", "Vehicle Number Count Difference = True",, )
                End If

                'If either of the following are true, we require a local file update, otherwise, we bypass this altogether...
                'If AtLeastOneFileHasBeenUpdated = True Or VehicleNumberCountDifference = True Then
                If AtLeastOneFileHasBeenUpdated = True Or VehicleNumberCountDifference = True Or UpdatedFilesList.Count > 0 Or NewVehicleDirectory.Count > 0 Then

                    FileUpdateRequired = True

                    'Go through all vehicle folders on the share drive and read the contents of the vehicleconfigurations.csv files for each vehicle.
                    'If any file is newer than file on local drive or if we need to create a new file for some reason or if the number of valid vehicle folders
                    'on the share drive is different than the current number of vehicles defined in our local list, we will update the information
                    'by adding to the updated vehicle array list.

                    For x = 0 To UBound(dirs)

                        If UpdatedFilesList.Contains(dirs(x).Name) Or NewVehicleDirectory.Contains(dirs(x).Name) Then

                            If (myVehicleConfigDirStatus(x).ActiveFileDate > LocalVehicleConfigFileModifyDate) Or VehicleNumberCountDifference = True Then

                                fnum2 = FreeFile()
                                FileOpen(fnum2, myVehicleConfigDirStatus(x).ActiveFile, OpenMode.Input)

                                'Copy header as first line in local file to be updated...
                                'Only the first time through here, UpdatedVehicleConfigurationArrayList.Count = 0, so first time through, we copy the header info...
                                If UpdatedVehicleConfigurationArrayList.Count = 0 Then
                                    UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(0).ToString)
                                    textline = LineInput(fnum2)
                                End If

                                'Here we go through the file and look for the row where vehicle number corresponds to the vehicle number directory name
                                'When we find it, we copy entire row to the updated vehicle configuration array list...
                                Do While Not EOF(fnum2)
                                    textline = LineInput(fnum2)
                                    lineitems = Split(textline, ",")

                                    'if the most up to date file is an old version (Not NF Version) then we will need to modify the data for any row that
                                    'we are copying into the local file which will be in the NF New Format)...

                                    If filename = My.Application.Info.DirectoryPath & "\VehicleConfigurationsNF.csv" Then
                                        If InStr(UCase(myVehicleConfigDirStatus(x).ActiveFile), "VEHICLECONFIGURATIONS.CSV") > 0 Then
                                            Dim i As Integer
                                            For i = 0 To UBound(lineitems)
                                                If i = 0 Then
                                                    textline = lineitems(i)
                                                ElseIf i <= 13 Then
                                                    textline = textline & "," & lineitems(i)
                                                ElseIf i = 14 Then
                                                    textline = textline & ",NA,NA," & lineitems(i)
                                                Else
                                                    textline = textline & "," & lineitems(i)
                                                End If
                                            Next i
                                        End If
                                        'Else
                                        'If InStr(UCase(myVehicleConfigDirStatus(x).ActiveFile), "VEHICLECONFIGURATIONS.CSV") > 0 Then
                                        'Dim i As Integer
                                        'For i = 0 To UBound(lineitems)
                                        '            If i = 0 Then
                                        '    textline = lineitems(i)
                                        '    Else
                                        '    textline = textline & "," & lineitems(i)
                                        'End If
                                        'Next i
                                        'End If
                                    End If

                                    If filename = My.Application.Info.DirectoryPath & "\VehicleConfigurations.csv" And InStr(UCase(myVehicleConfigDirStatus(x).ActiveFile), "VEHICLECONFIGURATIONSNF.CSV") > 0 Then
                                        HandleUserMessageLogging("GMRC", "Ignoring " & myVehicleConfigDirStatus(x).ActiveFile & " due to incompatible file format " & dirs(x).Name,, )
                                    Else

                                        If (UCase(dirs(x).Name) = UCase(lineitems(0))) And Not UpdatedVehicleConfigurationArrayList.Contains(textline) Then
                                            UpdatedVehicleConfigurationArrayList.Add(textline)
                                            If VehicleNumberCountDifference = False Then
                                                HandleUserMessageLogging("GMRC", "Updated information added to " & System.IO.Path.GetFileName(filename) & " file for " & dirs(x).Name,, )
                                            End If
                                            Exit Do
                                        End If

                                    End If

                                Loop

                                FileClose(fnum2)

                            Else 'If the modified date of the existing file is earlier than the modified date
                                'of the local file, we will put the local file information into the updated vehicle number array list...

                                'Only the first time through here, UpdatedVehicleConfigurationArrayList.Count = 0, so first time through, we copy the header info...
                                If UpdatedVehicleConfigurationArrayList.Count = 0 Then
                                    UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(0).ToString)
                                    textline = LineInput(fnum2)
                                End If

                                For y = 1 To LocalVehicleConfigurationArrayList.Count - 1
                                    tempstrarray = Split(LocalVehicleConfigurationArrayList(y).ToString, ",")
                                    If UCase(tempstrarray(0)) = UCase(dirs(x).Name) Then
                                        UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(y).ToString)
                                    End If
                                Next

                            End If

                        Else 'File in directory is not updated and directory is not new... (UpdatedFilesList.Contains(dirs(x).Name) Or NewVehicleDirectory.Contains(dirs(x).Name))

                            If UpdatedVehicleConfigurationArrayList.Count = 0 Then
                                UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(0).ToString)
                            End If

                            For y = 1 To LocalVehicleConfigurationArrayList.Count - 1
                                tempstrarray = Split(LocalVehicleConfigurationArrayList(y).ToString, ",")
                                If UCase(tempstrarray(0)) = UCase(dirs(x).Name) Then
                                    UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(y).ToString)
                                End If
                            Next

                        End If

                    Next x 'Next vehicle directory...

                End If 'AtLeastOneFileHasBeenUpdated = True Or VehicleNumberCountDifference = True

                If FileUpdateRequired = True Then

                    fnum = FreeFile()
                    FileOpen(fnum, filename, OpenMode.Output)

                    For x = 0 To UpdatedVehicleConfigurationArrayList.Count - 1
                        PrintLine(fnum, UpdatedVehicleConfigurationArrayList(x).ToString)
                    Next x

                    FileClose(fnum)
                    HandleUserMessageLogging("GMRC", System.IO.Path.GetFileName(filename) & " file has been updated with new information.",, )

                Else
                    HandleUserMessageLogging("GMRC", "No changes to " & System.IO.Path.GetFileName(filename) & " file required.")
                End If

                HandleUserMessageLogging("GMRC", "Update operation complete.", DisplayMsgBox, )

            Else
                HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles: Cannot connect to " & NetworkDriveMapping & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations directory.  Please verify network connection...", DisplayMsgBox, )
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles: " & ex.Message, DisplayMsgBox, )
        Finally
            updatingVehicleList = False
            UserStatusInfo.Label1.Text = ""
            UserStatusInfo.Hide()
        End Try

    End Sub

    Public Sub checkvehicleconfigfilesOLD(ByVal mylistbox As ListBox, ByVal WhoAmI As String)

        'not used, retained for reference...

        'Changed instances of NetworkDriveLetter to NetworkDriveMapping 02/14/2021

        'In CLEVIR, This is called from the Update Vehicle Number List button on the InitForm...
        'In FLASHOMATIC, This is called from the Update Vehicleconfigurations.csv button 

        'checkvehicleconfigfiles goes to the share drive and finds all vehicleconfigurations.csv files from all vehicles 
        'and extracts vehicle specific contents to build an updated local copy of vehicleconfiguraitons.csv based on the
        'latest vehicle configuration info on the share drive.  It then uses this updated vehicleconfigurations.csv file
        'to re-build the list of available vehicles...

        Dim fnum As Integer
        Dim textline As String = ""
        Dim NEW_FILE_FORMAT As Boolean = False
        Dim lineitems() As String
        Dim tempstrarray() As String

        Dim vehiclenumbers As String = ""

        Dim filename As String

        Dim NewFileFormat() As String = Nothing

        Dim fnum2 As Integer

        Dim LocalVehicleConfigurationArrayList As ArrayList
        Dim UpdatedVehicleConfigurationArrayList As ArrayList

        'Dim CreatingNewFile As Boolean
        Dim LocalFileModifiedDate As Date

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer

        Dim VehicleNumberCountDifference As Boolean
        Dim FileUpdateRequired As Boolean

        Dim ValidDirectoryCount As Integer
        Dim ListboxCount As Integer
        Dim AtLeastOneFileHasBeenUpdated As Boolean

        Dim UpdatedFilesList As List(Of String)
        Dim NewVehicleDirectory As List(Of String)
        Dim VehicleNumbersInLocalList As List(Of String)
        Dim tempstr() As String
        Dim NewLine As String
        Dim FoundConfigFile As Boolean

        Try

            If NetworkDrivePermission = False Then
                HandleUserMessageLogging("GMRC", "checkvehicleconfigfiles: Could not access " & NetworkDriveMapping & CLEVIRBaseDir & ". Exiting...", DisplayMsgBox)
                Exit Sub
            End If

            HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles called...",, )

            updatingVehicleList = True

            'First we need to make sure that the vehicle configurations directory exists and can be found using the defined NetworkDriveMapping...
            If System.IO.Directory.Exists(NetworkDriveMapping & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations") Then

                HandleUserMessageLogging("GMRC", "Updating Vehicle Configuration Information...",,, FlashMsgOn)

                filename = My.Application.Info.DirectoryPath & "\VehicleConfigurations.csv"

                If HandleVehicleConfigurationsFile(filename) = False Then
                    Exit Sub
                End If

                LocalVehicleConfigurationArrayList = New ArrayList
                UpdatedVehicleConfigurationArrayList = New ArrayList

                VehicleNumbersInLocalList = New List(Of String)

                'Copy contents of local file into LocalVehicleConfigurationArrayList for use below...
                fnum = FreeFile()
                FileOpen(fnum, filename, OpenMode.Input)

                Do While Not EOF(fnum)
                    NewLine = LineInput(fnum)
                    tempstr = Split(NewLine, ",")
                    LocalVehicleConfigurationArrayList.Add(NewLine)
                    If Not InStr(tempstr(0), "Vehicle Number") > 0 Then
                        VehicleNumbersInLocalList.Add(tempstr(0))
                    End If
                Loop
                FileClose(fnum)

                'Save the last modified date of the local file which will be compared to the modified date of each file on the share drive
                'to determine if an update to the information in the local file is necessary...
                LocalFileModifiedDate = System.IO.File.GetLastWriteTime(filename)

                Dim dir As New DirectoryInfo(NetworkDriveMapping & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations")
                Dim files As FileInfo()
                Dim dirs As DirectoryInfo() = dir.GetDirectories()

                'Get number of vehicles currently defined in local vehicleconfigurations.csv file (this vehicleconfigurations.csv file used to populate listbox on initialization)
                'This will be used later to compare the number of vehicled defined in the local file, with the number of vehicle folders that are on
                'the share drive.  If the numbers are different, we will need to modify the local file...
                'If CLEVIR calls this routine we need to subtract 1 from total count because of the " VEHICLE ID NOT IN LIST" entry in the list box...
                If WhoAmI = "CLEVIR" Then
                    ListboxCount = mylistbox.Items.Count - 1
                Else
                    'if FLASHOMATIC, which also uses this routine, calls it we do not add the VEHICLE ID NOT IN LIST on initial load of listbox,
                    'so ListboxCount does not have to be reduced by 1...
                    ListboxCount = mylistbox.Items.Count
                End If

                mylistbox.Items.Clear()

                'Add VEHICLE ID NOT IN LIST as first listbox entry (If not FLASHOMATIC), this is used only by CLEVIR when configuring a new vehicle number...
                If WhoAmI = "CLEVIR" Then
                    mylistbox.Items.Add(" VEHICLE ID NOT IN LIST")
                End If

                UpdatedFilesList = New List(Of String)
                NewVehicleDirectory = New List(Of String)

                'Get number of valid vehicle configurations in " & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations
                For x = 0 To UBound(dirs)
                    FoundConfigFile = False
                    files = dirs(x).GetFiles
                    For z = 0 To UBound(files)
                        If UCase(files(z).Name) = "VEHICLECONFIGURATIONS.CSV" Then

                            'Flag if any configuration file has a date newer than the local file, this means we require a change to the local file...
                            If files(z).LastWriteTime > LocalFileModifiedDate Then
                                AtLeastOneFileHasBeenUpdated = True
                                UpdatedFilesList.Add(dirs(x).Name)
                                HandleUserMessageLogging("GMRC", dirs(x).Name & " vehicle configuration modify date is newer than local file.")
                            End If

                            If Not VehicleNumbersInLocalList.Contains(UCase(dirs(x).Name)) And Not VehicleNumbersInLocalList.Contains(dirs(x).Name) Then
                                NewVehicleDirectory.Add(dirs(x).Name)
                            End If

                            ValidDirectoryCount += 1
                            'Here we add the vehicle number (which is the dir.name), to the vehicle numbers listbox
                            mylistbox.Items.Add(dirs(x).Name)
                            mylistbox.SelectedIndex = mylistbox.Items.Count - 1
                            mylistbox.Refresh()
                            FoundConfigFile = True
                            Exit For
                        End If
                    Next z
                    If FoundConfigFile = False Then
                        UpdatedFilesList.Add(dirs(x).Name)
                    End If
                Next x

                'If the count is different, we will need to update local file...
                If ValidDirectoryCount <> ListboxCount Then
                    VehicleNumberCountDifference = True
                    HandleUserMessageLogging("GMRC", "Vehicle Number Count Difference = True",, )
                End If

                'If either of the following are true, we require a local file update, otherwise, we bypass this altogether...
                'If AtLeastOneFileHasBeenUpdated = True Or VehicleNumberCountDifference = True Then
                If AtLeastOneFileHasBeenUpdated = True Or VehicleNumberCountDifference = True Or UpdatedFilesList.Count > 0 Or NewVehicleDirectory.Count > 0 Then

                    FileUpdateRequired = True

                    'Go through all vehicle folders on the share drive and read the contents of the vehicleconfigurations.csv files for each vehicle.
                    'If any file is newer than file on local drive or if we need to create a new file for some reason or if the number of valid vehicle folders
                    'on the share drive is different than the current number of vehicles defined in our local list, we will update the information
                    'by adding to the updated vehicle array list.

                    For x = 0 To UBound(dirs)

                        If UpdatedFilesList.Contains(dirs(x).Name) Or NewVehicleDirectory.Contains(dirs(x).Name) Then

                            files = dirs(x).GetFiles

                            'Go through each directory looking for the vehicleconfigurations.csv file.  In most cases, there should only be a single file in the directory
                            'so this should only loop once to find the file.  (If the file is not there, which is not typically the case, we will not do anything)...

                            For z = 0 To UBound(files)
                                If UCase(files(z).Name) = "VEHICLECONFIGURATIONS.CSV" Then

                                    If files(z).LastWriteTime > LocalFileModifiedDate Or VehicleNumberCountDifference = True Then

                                        fnum2 = FreeFile()
                                        FileOpen(fnum2, files(z).FullName, OpenMode.Input)

                                        'Copy header as first line in local file to be updated...
                                        'Only the first time through here, UpdatedVehicleConfigurationArrayList.Count = 0, so first time through, we copy the header info...
                                        If UpdatedVehicleConfigurationArrayList.Count = 0 Then
                                            UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(0).ToString)
                                            textline = LineInput(fnum2)
                                        End If

                                        'If Len(textline) = 0 Then
                                        'textline = LineInput(fnum2)
                                        'UpdatedVehicleConfigurationArrayList.Add(textline)
                                        'End If

                                        'Here we go through the file and look for the row where vehicle number corresponds to the vehicle number directory name
                                        'When we find it, we copy entire row to the updated vehicle configuration array list...
                                        Do While Not EOF(fnum2)
                                            textline = LineInput(fnum2)
                                            lineitems = Split(textline, ",")

                                            If (UCase(dirs(x).Name) = UCase(lineitems(0))) And Not UpdatedVehicleConfigurationArrayList.Contains(textline) Then
                                                UpdatedVehicleConfigurationArrayList.Add(textline)
                                                If VehicleNumberCountDifference = False Then
                                                    HandleUserMessageLogging("GMRC", "Updated information added to vehicleconfigurations.csv file for " & dirs(x).Name,, )
                                                End If
                                                Exit Do
                                            End If

                                        Loop

                                        FileClose(fnum2)

                                    Else 'If the modified date of the existing file is earlier than the modified date
                                        'of the local file, we will put the local file information into the updated vehicle number array list...

                                        'Only the first time through here, UpdatedVehicleConfigurationArrayList.Count = 0, so first time through, we copy the header info...
                                        If UpdatedVehicleConfigurationArrayList.Count = 0 Then
                                            UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(0).ToString)
                                            textline = LineInput(fnum2)
                                        End If

                                        For y = 1 To LocalVehicleConfigurationArrayList.Count - 1
                                            tempstrarray = Split(LocalVehicleConfigurationArrayList(y).ToString, ",")
                                            If UCase(tempstrarray(0)) = UCase(dirs(x).Name) Then
                                                UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(y).ToString)
                                            End If
                                        Next

                                    End If
                                    Exit For

                                End If

                            Next z 'File in directory, there should only be the vehicleconfigurations.csv file so we will drop through here after file is processed...

                        Else 'File in directory is not updated and directory is not new... (UpdatedFilesList.Contains(dirs(x).Name) Or NewVehicleDirectory.Contains(dirs(x).Name))

                            If UpdatedVehicleConfigurationArrayList.Count = 0 Then
                                UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(0).ToString)
                            End If

                            For y = 1 To LocalVehicleConfigurationArrayList.Count - 1
                                tempstrarray = Split(LocalVehicleConfigurationArrayList(y).ToString, ",")
                                If UCase(tempstrarray(0)) = UCase(dirs(x).Name) Then
                                    UpdatedVehicleConfigurationArrayList.Add(LocalVehicleConfigurationArrayList(y).ToString)
                                End If
                            Next

                        End If

                    Next x 'Next vehicle directory...

                End If 'AtLeastOneFileHasBeenUpdated = True Or VehicleNumberCountDifference = True

                If FileUpdateRequired = True Then

                    fnum = FreeFile()
                    FileOpen(fnum, filename, OpenMode.Output)

                    For x = 0 To UpdatedVehicleConfigurationArrayList.Count - 1
                        PrintLine(fnum, UpdatedVehicleConfigurationArrayList(x).ToString)
                    Next x

                    FileClose(fnum)
                    HandleUserMessageLogging("GMRC", "vehicleconfigurations.csv file has been updated with new information.",, )

                Else
                    HandleUserMessageLogging("GMRC", "No changes to vehicleconfigurations.csv file required.")
                End If

                HandleUserMessageLogging("GMRC", "Update operation complete.", DisplayMsgBox, )

            Else
                HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles: Cannot connect to " & NetworkDriveMapping & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations directory.  Please verify network connection...", DisplayMsgBox, )
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles: " & ex.Message, DisplayMsgBox, )
        Finally
            updatingVehicleList = False
            UserStatusInfo.Label1.Text = ""
            UserStatusInfo.Hide()
        End Try

    End Sub

    Private Sub UpdateVehicleStatus(ByVal UpdateText As String)

        'Called from HandleUserMessageLogging.  Updates the VehicleStatusInfo.txt file on the share drive.  This is used for
        'live status updates for both CLEVIR and FLASHOMATIC. Called if the SendUpdateVehicleStatus passed to HandleUserMessageLogging = True 

        'Most user information and logging information is handled by the HandleUserMessageLogging routine.  User information is always logged to a
        'log file but can also be displayed either with a user message box and/or written to a specified listbox and/or displayed as a temporary status info message, and also
        'can be copied up to a file on the share drive that can be read by CLEVIR on the CLEVIR administrators PC for live updates.

        'This routine handles the writing of a user information message to the share drive file if parameters are set accordingly for HandleUserMessageLogging ...

        Dim fnum As Integer
        Dim filename As String
        Dim textline As String

        If NetworkDrivePermission = False Then
            CopyToLog("UpdateVehicleStatus: Network Drive Access Permission = False - UpdateText = " & UpdateText)
            Exit Sub
        End If

        If System.IO.Directory.Exists(NetworkDriveMapping & CLEVIRBaseDir & "\Development\VehicleStatusUpdates") Then

            If InStr(My.Application.Info.AssemblyName, "FLASHOMATIC") > 0 Then

                Dim FlashomaticVersion As String
                FlashomaticVersion = Mid(My.Application.Info.AssemblyName, InStr(My.Application.Info.AssemblyName, "_"), Len(My.Application.Info.AssemblyName))

                If InStr(Username, "VEHTESTFIDCSV") > 0 Or Debugger.IsAttached Then
                    If Len(VehicleNumber) = 0 Then
                        If Directory.Exists("C:\CLEVIR" & FlashomaticVersion) Then
                            If File.Exists("C:\CLEVIR" & FlashomaticVersion & "\vehicleconfig.txt") Then
                                fnum = FreeFile()
                                FileOpen(fnum, "C:\CLEVIR" & FlashomaticVersion & "\vehicleconfig.txt", OpenMode.Input)
                                textline = LineInput(fnum)
                                VehicleNumber = VB.Right(textline, Len(textline) - InStr(textline, Chr(9)))
                                FileClose(fnum)

                            End If
                        End If
                    End If
                End If

            End If

            UpdateText = Format(DateTime.Now, "MM/dd/yyyy HH:mm:ss - ") & VehicleNumber & " " & UpdateText

            filename = NetworkDriveMapping & CLEVIRBaseDir & "\Development\VehicleStatusUpdates\VehicleStatusInfo.txt"
            fnum = FreeFile()
            If Not File.Exists(filename) Then
                FileOpen(fnum, filename, OpenMode.Output)
                PrintLine(fnum, UpdateText)
                FileClose(fnum)
            Else

                If Not FileInUse(filename) Then
                    FileOpen(fnum, filename, OpenMode.Append)
                    PrintLine(fnum, UpdateText)
                    FileClose(fnum)
                Else
                    CopyToLog("UpdateVehicleStatus: " & UpdateText & " File in use...")
                End If
            End If

        Else
            CopyToLog("UpdateVehicleStatus: Directory " & NetworkDriveMapping & CLEVIRBaseDir & "\Development\VehicleStatusUpdates not found...")
        End If

    End Sub

    Public Sub RunNotepad(ByVal filename As String)

        'Used by VehicleStatDashboard in CLEVIR to display the specified file contents in notepad...

        Dim Notepadprocess As New Process With {
            .StartInfo = New ProcessStartInfo("notepad.exe", filename)
        }
        Notepadprocess.Start()
    End Sub

    Public Sub HandleUserMessageLogging(ByVal LogFileType As String, ByVal MessageText As String, Optional DisplayMessageBox As Boolean = False, Optional ByVal SendUpdateVehicleStatus As Boolean = False, Optional ByVal UserStatusInfoTimeSec As Integer = -1, Optional ByVal myListBox As ListBox = Nothing, Optional ByVal MessageLogNumber As Integer = 0, Optional ByVal myLabel As Label = Nothing)
        'This routine is called any time we need to log information one of the CLEVIR log files, or if we want to display information to the user.
        'There are various mechanisms used to display information to the user, Message Box, Write into a list box on a form for status update, or display a user status info pop up
        'window for a set period of time.  Based on the arguments sent to this routine, we display the text passed in any or all of these ways.

        'Most user information and logging information is handled by this HandleUserMessageLogging routine.  User information is always logged to a
        'log file but can also be displayed as described above. Logging information may also be copied up to a file on the share drive that can be read
        'by CLEVIR on the CLEVIR administrators PC for live updates.

        Try

            'log file type passed in can be either "COMM" which will write to the GM_INCA_Comm.log file, or "GMRC" which will write to the GM_ResidentClient.log file...

            If LogFileType = "COMM" Then
                CopyToCOMMLog(MessageText)
            Else

                'If SendUpdateVehicleStatus = True Then
                'UpdateVehicleStatus(MessageText)
                'End If

                If myListBox IsNot Nothing Then
                    myListBox.Items.Add(MessageText)
                    myListBox.SelectedIndex = myListBox.Items.Count - 1
                    myListBox.Refresh()
                End If

                If myLabel IsNot Nothing Then
                    myLabel.Text = MessageText
                    myLabel.Refresh()
                End If

                CopyToLog(MessageText, MessageLogNumber)

                If UserStatusInfoTimeSec <> -1 Then
                    UserStatusInfo.Label1.Text = MessageText
                    If UserStatusInfoTimeSec <> 0 Then
                        System.Threading.Thread.Sleep(UserStatusInfoTimeSec)
                        UserStatusInfo.Hide()
                    End If
                End If

                If DisplayMessageBox = True Then
                    MsgBox(MessageText)
                    UserStatusInfo.Hide()
                End If

                If RecorderStopWatch IsNot Nothing AndAlso RecorderStopWatch.IsRunning Then
                    ' Use elapsed time for logging
                    Dim elapsed = RecorderStopWatch.Elapsed.TotalSeconds
                End If

            End If

        Catch ex As Exception
            CopyToLog("HandleUserMessageLogging: " & MessageText & " - " & ex.Message)
        End Try


    End Sub

    Sub RoboCopyFile(ByVal sourcefile As String, ByVal destDir As String, Optional ByVal Elevate As Boolean = False)

        'Called from numerous places...
        'Uses RoboCopy to copy source file to destdir - keeps the same filename in new location...

        Dim myprocess As Process
        Dim ExecutableFile As String = "C:\csvscripts\robocopy.exe"
        Dim p As New ProcessStartInfo
        'Dim sourcefile As String
        Dim destfile As String
        'Dim destdir As String
        Dim sourcedir As String

        Dim RoboParams As String

        sourcedir = Path.GetDirectoryName(sourcefile)

        destfile = Path.GetFileName(sourcefile)

        'run robocopy routine

        p.WindowStyle = ProcessWindowStyle.Normal '.Hidden
        p.FileName = ExecutableFile

        If Elevate = True Then
            p.Verb = "runas"
        End If


        'RoboParams = " /R:1 /move /s"
        'RoboParams = " /R:1 /mov"
        RoboParams = " /R:1"


        'p.Arguments = sourcedir & " " & """" & destDir & """" & " " & destfile & RoboParams

        p.Arguments = """" & sourcedir & """" & " " & """" & destDir & """" & " " & destfile & RoboParams

        myprocess = Process.Start(p)
        'If AllFiles = True Then
        myprocess.WaitForExit()
        'End If

    End Sub

    Public Function MapDrive(ByVal DriveLetter As String, ByVal UNCPath As String) As Boolean

        'Called when user makes a File Upload Destination selection on the UploadDataScreen if the upload destination cannot be found
        'Maps a network drive to the name specified in the config.xml file, this is where the recorded data from the vehicle
        'will be copied.

        Dim nr As NETRESOURCE
        Dim Username As String
        Dim Password As String

        nr = New NETRESOURCE With {
            .lpRemoteName = UNCPath,
            .lpLocalName = DriveLetter
        }
        Username = Nothing '(add parameters to pass this if necessary)
        Password = Nothing '(add parameters to pass this if necessary)
        nr.dwType = RESOURCETYPE_DISK

        Dim result As Long
        Dim resultstr As String

        HandleUserMessageLogging("GMRC", "Mapping Drive - " & DriveLetter & ": " & UNCPath)

        result = WNetAddConnection2(nr, Password, Username, 0)

        'NETWORK DRIVE MAPPING

        'Public Const ERROR_ACCESS_DENIED = 5&
        'Public Const ERROR_ALREADY_ASSIGNED = 85&

        If result = NO_ERROR Or result = ERROR_ALREADY_ASSIGNED Then
            'If System.IO.Directory.Exists(NetworkDriveLetter & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\UpdatedFiles") Then
            If System.IO.Directory.Exists(DriveLetter) Then
                HandleUserMessageLogging("GMRC", "Drive Mapping for Data Upload succeeded.")
                Return True
            Else
                HandleUserMessageLogging("GMRC", "Drive Mapping for Data Upload Failed - Directory not found.", DisplayMsgBox, )
                Return False
            End If
        Else

            Select Case result
                Case ERROR_ACCESS_DENIED
                    resultstr = "ACCESS_DENIED"
                Case ERROR_INVALID_PASSWORD
                    resultstr = "INVALID_PASSWORD"
                Case Else
                    resultstr = CStr(result)
            End Select
            HandleUserMessageLogging("GMRC", "Map Network Drive Failed, Return Value = " & resultstr, DisplayMsgBox, )
            Return False
        End If
    End Function

    Public Function UnMapDrive(ByVal DriveLetter As String) As Boolean

        'not currently used, saved in case we may need this functionality down the road...

        'Unmaps the network drive that was mapped prior to uploading data.

        Dim rc As Integer
        rc = WNetCancelConnection2(DriveLetter, 0, ForceDisconnect)

        If rc = 0 Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Sub DirectoryCopy(
            ByVal sourceDirName As String,
            ByVal destDirName As String,
            ByVal copySubDirs As Boolean)

        'This is called out of GmResidentClient.AddCustomINCASetup which is not currently supported. 
        'This routine is retained in case we need to use this functionality later for a different purpose...

        'Creates a New directory and copyies the contents of
        'the source directory into the new directory.

        ' Get the subdirectories for the specified directory. 
        Dim dir As New DirectoryInfo(sourceDirName)
        Dim dirs As DirectoryInfo() = dir.GetDirectories()

        If Not dir.Exists Then
            Throw New DirectoryNotFoundException(
                "Source directory does not exist or could not be found: " _
                + sourceDirName)
        End If

        ' If the destination directory doesn't exist, create it. 
        If Not Directory.Exists(destDirName) Then
            Directory.CreateDirectory(destDirName)

            ' Get the files in the directory and copy them to the new location. 
            Dim files As FileInfo() = dir.GetFiles()
            For Each file In files
                Dim temppath As String = Path.Combine(destDirName, file.Name)
                file.CopyTo(temppath, False)
            Next file

            ' If copying subdirectories, copy them and their contents to new location. 
            If copySubDirs Then
                For Each subdir In dirs
                    Dim temppath As String = Path.Combine(destDirName, subdir.Name)
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs)
                Next subdir
            End If
        Else
            MsgBox(destDirName & " already exists.")
        End If

    End Sub
    Public Function CheckForRoboCopyFolder() As Boolean

        'Changed instances of NetworkDriveLetter to NetworkDriveMapping 02/14/2021

        'Called from InitForm_Load in CLEVIR and Flashomatic_Load in FLASHOMATIC...

        'CLEVIR requires robocopy to be available on the computer.  Robocopy runs out of the CSVScripts folder.  If this folder
        'does not exist, we copy it and its contents to the user PC.


        Dim Failed As Boolean

        Try

            If Not System.IO.Directory.Exists("C:\CSVScripts") Then

                If NetworkDrivePermission = False Then
                    HandleUserMessageLogging("GMRC", "CheckForRoboCopyFolder: Could not access " & NetworkDriveMapping & CLEVIRBaseDir, DisplayMsgBox)
                    'CheckForRoboCopyFolder = False
                    Failed = True
                    Exit Function
                End If

                If System.IO.Directory.Exists(NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\Misc Support Files\CSVScripts") Then

                    HandleUserMessageLogging("GMRC", "CheckForRoboCopyFolder: Copying CSVScripts directory from Q drive to C drive...",, )
                    My.Computer.FileSystem.CopyDirectory(NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\Misc Support Files\CSVScripts", "C:\CSVScripts")
                Else
                    HandleUserMessageLogging("GMRC", "CheckForRoboCopyFolder: Could not find " & NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\Misc Support Files\CSVScripts.", DisplayMsgBox, )
                    Failed = True
                End If

            ElseIf System.IO.File.Exists("C:\CSVScripts\Robocopy.exe") = False Then

                If NetworkDrivePermission = False Then
                    HandleUserMessageLogging("GMRC", "CheckForRoboCopyFolder: Could not access " & NetworkDriveMapping & CLEVIRBaseDir, DisplayMsgBox)
                    'CheckForRoboCopyFolder = False
                    Failed = True
                    Exit Function
                End If

                If System.IO.Directory.Exists(NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\Misc Support Files\CSVScripts") Then

                    HandleUserMessageLogging("GMRC", "CheckForRoboCopyFolder: Copying CSVScripts files from Q drive to C drive...",, )
                    My.Computer.FileSystem.CopyDirectory(NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\Misc Support Files\CSVScripts", "C:\CSVScripts", True)
                Else
                    HandleUserMessageLogging("GMRC", "CheckForRoboCopyFolder: Could not find " & NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\Misc Support Files\CSVScripts.", DisplayMsgBox, )
                    Failed = True
                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckForRoboCopyFolder: " & ex.Message, DisplayMsgBox, )
            Failed = True

        Finally
            CheckForRoboCopyFolder = Not Failed
        End Try

    End Function
    Public Sub DeleteDirectory(path As String)

        'Called from DeleteDirectory and CopyINCADatabase...

        'Recursively deletes files in directories and subdirectories so "path" directory can be deleted.
        'At the time of implementing this, could not find a way of deleting a directory without first
        'deleting its contents...

        Try

            If Directory.Exists(path) Then

                'Delete all files from the Directory

                For Each filepath As String In Directory.GetFiles(path)

                    Try
                        File.SetAttributes(filepath, FileAttributes.Normal)
                        File.Delete(filepath)
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", "DeleteDirectory: Files For Loop: " & ex.Message)
                    End Try

                Next

                'Delete all child Directories

                For Each dir As String In Directory.GetDirectories(path)

                    Try
                        DeleteDirectory(dir)
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", "DeleteDirectory: For Loop: " & ex.Message)
                        'Continue For
                    End Try

                Next

                'Delete a Directory

                Directory.Delete(path)

            End If

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "DeleteDirectory: " & ex.Message)
        End Try

    End Sub

    Public Sub AcquireShutdownPrivilege()

        'Called from UploadDataScreen and GmResidentClient.ExitApp

        'This routine enables the Shutdown privilege for the current process, 
        'which is necessary if you want to call ExitWindowsEx.

        Dim lastWin32Error As Integer = 0

        'Get the LUID that corresponds to the Shutdown privilege, if it exists.
        Dim luid_Shutdown As LUID
        If Not LookupPrivilegeValue(Nothing, SE_SHUTDOWN_NAME, luid_Shutdown) Then
            lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error()
            Throw New System.ComponentModel.Win32Exception(lastWin32Error,
             "LookupPrivilegeValue failed with error " & lastWin32Error.ToString & ".")
        End If

        'Get the current process's token.
        Dim hProc As IntPtr = Process.GetCurrentProcess().Handle
        Dim hToken As IntPtr
        If Not OpenProcessToken(hProc, TOKEN_ADJUST_PRIVILEGES Or TOKEN_QUERY, hToken) Then
            lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error()
            Throw New System.ComponentModel.Win32Exception(lastWin32Error,
             "OpenProcessToken failed with error " & lastWin32Error.ToString & ".")
        End If

        Try

            'Set up a LUID_AND_ATTRIBUTES structure containing the Shutdown privilege, marked as enabled.
            Dim luaAttr As New LUID_AND_ATTRIBUTES With {
                .Luid = luid_Shutdown,
                .Attributes = SE_PRIVILEGE_ENABLED
            }

            'Set up a TOKEN_PRIVILEGES structure containing only the shutdown privilege.
            Dim newState As New TOKEN_PRIVILEGES With {
                .PrivilegeCount = 1,
                .Privileges = New LUID_AND_ATTRIBUTES() {luaAttr}
            }

            'Set up a TOKEN_PRIVILEGES structure for the returned (modified) privileges.
            Dim prevState As New TOKEN_PRIVILEGES
            ReDim prevState.Privileges(CInt(newState.PrivilegeCount))

            'Apply the TOKEN_PRIVILEGES structure to the current process's token.
            Dim returnLength As IntPtr
            If Not AdjustTokenPrivileges(hToken, False, newState, System.Runtime.InteropServices.Marshal.SizeOf(prevState), prevState, returnLength) Then
                lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error()
                Throw New System.ComponentModel.Win32Exception(lastWin32Error,
                 "AdjustTokenPrivileges failed with error " & lastWin32Error.ToString & ".")
            End If

        Finally
            CloseHandle(hToken)
        End Try

    End Sub

    'Public Function FileInUse(ByVal sFile As String) As Boolean

    '    'Called from multiple routines...
    '    'Checks if file in use, returns true if in use...

    '    FileInUse = False

    '    If System.IO.File.Exists(sFile) Then
    '        Try
    '            Dim F As Short = FreeFile()
    '            FileOpen(F, sFile, OpenMode.Binary, OpenAccess.ReadWrite, OpenShare.LockReadWrite)
    '            FileClose(F)
    '        Catch ex As Exception
    '            Return True
    '        End Try
    '    End If

    'End Function

    Public Function FileInUse(filePath As String) As Boolean

        'Called from multiple routines...
        'Checks if file in use, returns true if in use...
        Try
            Using fs As FileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None)
                ' If we can open exclusively, the file is not in use.
            End Using
            Return False
        Catch
            Return True
        End Try
    End Function

    Public Function GetRandom(ByVal Min As Integer, ByVal Max As Integer) As Integer

        'Called from various places, typically used for testing in the design environment...

        'Gets a random number between min and max...

        ' by making Generator static, we preserve the same instance '
        ' (i.e., do not create new instances with the same seed over and over) '
        ' between calls '
        Static Generator As New Random()
        Return Generator.Next(Min, Max)
    End Function

    Public Sub CheckForNewerSoftwareVersions()

        'Called when Save Vehicle Number Change button is pressed on InitForm and HandleWirelessConnection...

        'Checks the share drive to see if there are newer versions of the current application executables and support files.
        'If new support files are found they are copied.  If an updated .exe is found, it shells
        'out autoupdater.exe and terminates the current app.  The autoupdater then copies the new executables and shells
        'new version of the current app and kills itself...

        Dim dir As DirectoryInfo
        Dim files As FileInfo()
        Dim AppShortName As String
        Dim UpdatedFilesFullPath As String = ""
        Dim UpdatedFilesLocation As String = ""
        Dim NumberOfPasses As Integer
        Dim TargetPath As String
        Dim FileType As String = ""
        Dim x As Integer
        Dim y As Integer
        Dim ProfsFilesCopied() As String = Nothing
        Dim z As Integer

        Try

            If UsingFlashDrive = True Then
                TargetPath = NetworkDriveLetter
            Else
                TargetPath = NetworkDriveMapping

                If NetworkDrivePermission = False Then
                    HandleUserMessageLogging("GMRC", "CheckForNewerSoftwareVersions: Could not access " & NetworkDriveMapping & CLEVIRBaseDir & ". Exiting...")
                    Exit Sub
                End If
            End If

            If System.IO.Directory.Exists(TargetPath) Then
                HandleUserMessageLogging("GMRC", "CheckForNewerSoftwareVersions: Found " & TargetPath & "...",,, FlashMsg1Sec)

                AppShortName = My.Application.Info.AssemblyName

                Select Case AppShortName
                    Case "CLEVIR_INCA_7_2", "CLEVIR_INCA_7_3", "CLEVIR_INCA_7_4", "CLEVIR_INCA_7_5"
                        NumberOfPasses = 4
                    Case "THE_ANNOTATOR_INCA_7_2", "THE_ANNOTATOR_INCA_7_3", "THE_ANNOTATOR_INCA_7_4", "THE_ANNOTATOR_INCA_7_5"
                        NumberOfPasses = 7
                        FileType = "DataDictionary"
                    Case "FLASHOMATIC_INCA_7_2", "FLASHOMATIC_INCA_7_3", "FLASHOMATIC_INCA_7_4", "FLASHOMATIC_INCA_7_5"
                        NumberOfPasses = 7
                        FileType = "WorkspaceTemplates"
                    Case "CLEVIR_File_Transfer_Utility", "CameraCheck"
                        NumberOfPasses = 0
                End Select

                HandleUserMessageLogging("GMRC", "Checking for applicable vehicle type specific Support Files...",,, FlashMsgOn)

                For y = 1 To NumberOfPasses

                    If NumberOfPasses = 7 Then
                        UpdatedFilesLocation = "Updated CLEVIR Files for Vehicles\Vehicle Type Specific Support Files"
                        Select Case y
                            Case 1
                                UpdatedFilesLocation = UpdatedFilesLocation & "\CSAV2\" & FileType
                            Case 2
                                UpdatedFilesLocation = UpdatedFilesLocation & "\LowContent\" & FileType
                            Case 3
                                UpdatedFilesLocation = UpdatedFilesLocation & "\HighContent\" & FileType
                            Case 4
                                UpdatedFilesLocation = UpdatedFilesLocation & "\ACP2\" & FileType
                            Case 5
                                UpdatedFilesLocation = UpdatedFilesLocation & "\ACP3\" & FileType
                            Case 6
                                UpdatedFilesLocation = UpdatedFilesLocation & "\ACP4\" & FileType
                            Case 7
                                UpdatedFilesLocation = UpdatedFilesLocation & "\FCM\" & FileType
                        End Select

                    Else
                        UpdatedFilesLocation = "Updated CLEVIR Files for Vehicles\Vehicle Type Specific Support Files\" & ProjectName
                        Select Case y
                            Case 1
                                UpdatedFilesLocation &= "\WorkspaceTemplates"
                            Case 2
                                UpdatedFilesLocation &= "\EnumerationFiles"
                            Case 3
                                UpdatedFilesLocation &= "\DataDictionary"
                            Case 4
                                UpdatedFilesLocation &= "\VehicleSpy"
                        End Select

                    End If

                    UpdatedFilesFullPath = TargetPath & CLEVIRBaseDir & "\" & UpdatedFilesLocation

                    If InStr(UpdatedFilesFullPath, "WorkspaceTemplates") > 0 And MaxCameras = 8 Then
                        UpdatedFilesFullPath &= "\8Camera_Templates"
                    End If

                    If System.IO.Directory.Exists(UpdatedFilesFullPath) Then

                        dir = New DirectoryInfo(UpdatedFilesFullPath)
                        files = dir.GetFiles

                        'Search for files other than the main exe that may have been updated and copy them over the existing files

                        For x = 0 To UBound(files)

                            If InStr(UpdatedFilesFullPath, "VehicleSpy") = 0 Then

                                If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\" & files(x).Name) Then
                                    If System.IO.File.GetLastWriteTime(files(x).FullName) > System.IO.File.GetLastWriteTime(My.Application.Info.DirectoryPath & "\" & files(x).Name).AddMinutes(1) Then
                                        HandleUserMessageLogging("GMRC", "Copying " & files(x).Name & " to " & My.Application.Info.DirectoryPath,,, FlashMsgOn)
                                        System.IO.File.Copy(files(x).FullName, My.Application.Info.DirectoryPath & "\" & files(x).Name, True)
                                    End If

                                Else
                                    HandleUserMessageLogging("GMRC", "Copying new file " & files(x).Name & " to " & My.Application.Info.DirectoryPath,,, FlashMsgOn)
                                    System.IO.File.Copy(files(x).FullName, My.Application.Info.DirectoryPath & "\" & files(x).Name, True)
                                End If

                            Else

                                If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\VehicleSpy\" & files(x).Name) Then
                                    If System.IO.File.GetLastWriteTime(files(x).FullName) > System.IO.File.GetLastWriteTime(My.Application.Info.DirectoryPath & "\VehicleSpy\" & files(x).Name).AddMinutes(1) Then
                                        HandleUserMessageLogging("GMRC", "Copying " & files(x).Name & " to " & My.Application.Info.DirectoryPath & "\VehicleSpy",,, FlashMsgOn)
                                        System.IO.File.Copy(files(x).FullName, My.Application.Info.DirectoryPath & "\VehicleSpy\" & files(x).Name, True)
                                    End If

                                Else
                                    HandleUserMessageLogging("GMRC", "Copying new file " & files(x).Name & " to " & My.Application.Info.DirectoryPath & "\VehicleSpy",,, FlashMsgOn)
                                    System.IO.File.Copy(files(x).FullName, My.Application.Info.DirectoryPath & "\VehicleSpy\" & files(x).Name, True)
                                End If

                            End If

                        Next

                    End If

                Next y

                If InStr(AppShortName, "_INCA") = 0 Then
                    UpdatedFilesLocation = AppShortName
                Else
                    If InStr(AppShortName, "CLEVIR") = 0 Then
                        UpdatedFilesLocation = Mid(AppShortName, 1, InStr(AppShortName, "_INCA") - 1)
                    Else
                        UpdatedFilesLocation = "Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files"
                    End If

                End If

                UpdatedFilesFullPath = TargetPath & CLEVIRBaseDir & "\" & UpdatedFilesLocation & "\UpdatedFiles"

                HandleUserMessageLogging("GMRC", "Checking for Updated Executable Files and " & My.Application.Info.AssemblyName & " Support Files...",,, FlashMsgOn)

                If System.IO.Directory.Exists(UpdatedFilesFullPath) Then

                    dir = New DirectoryInfo(UpdatedFilesFullPath)
                    files = dir.GetFiles

                    'Search for files other than the main exe that may have been updated and copy them over the existing files

                    For x = 0 To UBound(files)

                        If InStr(files(x).Name, My.Application.Info.AssemblyName & ".exe") = 0 Then

                            If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\" & files(x).Name) Then
                                If System.IO.File.GetLastWriteTime(files(x).FullName) > System.IO.File.GetLastWriteTime(My.Application.Info.DirectoryPath & "\" & files(x).Name).AddMinutes(1) Then
                                    HandleUserMessageLogging("GMRC", "Copying " & files(x).Name & " to " & My.Application.Info.DirectoryPath,,, FlashMsgOn)
                                    System.IO.File.Copy(files(x).FullName, My.Application.Info.DirectoryPath & "\" & files(x).Name, True)
                                Else
                                    'Added this to handle copying of new profs files for ACP3 to temp folder, and copying modified profs files after initial profs files are unzipped.
                                    If (InStr(files(x).Name, "_Prof") > 0 Or InStr(files(x).Name, "_Override") > 0) And InStr(files(x).Name, ".zip") > 0 Then

                                        If Not File.Exists("C:\Temp\" & files(x).Name) Then
                                            File.Copy(files(x).FullName, "C:\temp\" & files(x).Name, True)
                                            ReDim Preserve ProfsFilesCopied(z)
                                            ProfsFilesCopied(z) = "C:\temp\" & files(x).Name
                                            z += 1
                                        End If

                                    End If
                                End If

                            Else
                                If InStr(files(x).Name, "_INCA_7_") = 0 Or InStr(files(x).Name, My.Application.Info.AssemblyName & ".exe.config") > 0 Then
                                    HandleUserMessageLogging("GMRC", "Copying new file " & files(x).Name & " to " & My.Application.Info.DirectoryPath,,, FlashMsgOn)
                                    System.IO.File.Copy(files(x).FullName, My.Application.Info.DirectoryPath & "\" & files(x).Name, True)

                                    'Added this to handle copying of new profs files for ACP3 to temp folder, and copying modified profs files after initial profs files are unzipped.
                                    If (InStr(files(x).Name, "_Prof") > 0 Or InStr(files(x).Name, "_Override") > 0) And InStr(files(x).Name, ".zip") > 0 Then
                                        If Not File.Exists("C:\Temp\" & files(x).Name) Then
                                            File.Copy(files(x).FullName, "C:\temp\" & files(x).Name, True)
                                            ReDim Preserve ProfsFilesCopied(z)
                                            ProfsFilesCopied(z) = "C:\temp\" & files(x).Name
                                            z += 1
                                        End If

                                    End If

                                End If

                            End If

                        End If
                    Next

                    'Added this to handle copying of new profs files for ACP3 to temp folder, and copying modified profs files after initial profs files are unzipped.
                    If ProfsFilesCopied IsNot Nothing Then
                        If UBound(ProfsFilesCopied) = 0 Then
                            UnzipFolder(ProfsFilesCopied(0))
                        Else
                            HandleProfsFiles(ProfsFilesCopied(0), ProfsFilesCopied(1))
                        End If
                    End If

                End If

                'If we found a new exe, then we need to shell out the autoupdater and exit the app so we can update to a new exe...

                If System.IO.File.GetLastWriteTime(UpdatedFilesFullPath & "\" & My.Application.Info.AssemblyName & ".exe") _
                    > System.IO.File.GetLastWriteTime(My.Application.Info.DirectoryPath & "\" & My.Application.Info.AssemblyName & ".exe").AddMinutes(1) Then

                    HandleUserMessageLogging("GMRC", "Found Newer " & My.Application.Info.AssemblyName & ".exe file...",,, FlashMsgOn)
                    If MsgBox("Found updated " & My.Application.Info.AssemblyName & " software version.  Update to new version Now?", vbYesNo) = vbYes Then
                        HandleUserMessageLogging("GMRC", "User chose to update to new version.",,, FlashMsgOn)
                        HandleUserMessageLogging("GMRC", "Found updated CLEVIR software, Launching AutoUpdater...",,, FlashMsg1Sec)

                        Shell(My.Application.Info.DirectoryPath & "\AutoUpdater.exe")
                        End

                    Else
                        HandleUserMessageLogging("GMRC", "User chose NOT to update to new version.",, )
                        UserStatusInfo.Hide()
                    End If

                Else
                    HandleUserMessageLogging("GMRC", "Currently running the latest available " & My.Application.Info.AssemblyName & " version...",,, FlashMsgOn)
                    UserStatusInfo.Hide()
                End If

            Else
                HandleUserMessageLogging("GMRC", TargetPath & " not found, no files copied...",,, FlashMsg1Sec)
            End If

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "CheckForNewerSoftwareVersions: " & ex.Message, DisplayMsgBox, )

        Finally
            UserStatusInfo.Hide()
        End Try

    End Sub
End Module
