Option Strict Off
Option Explicit On

Imports VB = Microsoft.VisualBasic

Imports System.IO
Imports System.Runtime.InteropServices
Imports de.etas.cebra.toolAPI.Common
Imports de.etas.cebra.toolAPI.Inca
Imports SevenZip

Module GlobalCommonModule

    'This module contains routines that may be shared between applications.  Certain functions are applicable to both CLEVIR and FLASHOMATIC for
    'instance,  so both applications referene this code file as a link from the same location thereby allowing applicable code to be modified
    'in one place for use by multiple applications...

    Public StatusUpdatesOn As Boolean

    Public A2l_FileName As String

    Public Const ForceDisconnect As Integer = 1
    Public Const RESOURCETYPE_DISK As Long = &H1
    Public Const ERROR_BAD_NETPATH As Long = 53&
    Public Const ERROR_NETWORK_ACCESS_DENIED As Long = 65&
    Public Const ERROR_INVALID_PASSWORD As Long = 86&
    Public Const ERROR_NETWORK_BUSY As Long = 54&

    Public Const EWX_LOGOFF As Long = &H0
    Public Const EWX_SHUTDOWN As Long = &H1
    Public Const EWX_REBOOT As Long = &H2
    Public Const EWX_FORCE As Long = &H4
    Public Const EWX_POWEROFF As Long = &H8
    Public Const EWX_FORCEIFHUNG As Long = &H10

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

    Public Structure NETRESOURCE
        Public dwScope As Integer
        Public dwType As Integer
        Public dwDisplayType As Integer
        Public dwUsage As Integer
        Public lpLocalName As String
        Public lpRemoteName As String
        Public lpComment As String
        Public lpProvider As String
    End Structure

    Public Declare Function WNetAddConnection2 Lib "mpr.dll" Alias "WNetAddConnection2A" (ByRef lpNetResource As NETRESOURCE, ByVal lpPassword As String, ByVal lpUserName As String, ByVal dwFlags As Integer) As Integer
    Public Declare Function WNetCancelConnection2 Lib "mpr" Alias "WNetCancelConnection2A" (ByVal lpName As String, ByVal dwFlags As Integer, ByVal fForce As Integer) As Integer

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

    Public Declare Function FindWindow Lib "user32" Alias "FindWindowA" (ByVal lpClassName As String, ByVal lpWindowName As String) As Integer

    Public Declare Function GetWindowPlacement Lib "user32" (ByVal hwnd As Integer, ByRef lpwndpl As WINDOWPLACEMENT) As Integer

    Public Declare Function SetWindowPlacement Lib "user32" (ByVal hwnd As Integer, ByRef lpwndpl As WINDOWPLACEMENT) As Integer

    Public Const SW_SHOWMINIMIZED As Short = 2

    Public Const SW_SHOWMAXIMIZED As Short = 3

    Public Const SW_SHOWNORMAL As Short = 1

    Public InitialDirectory As String
    Public g_ModelYear As String = "??"
    Public g_SoftwareVersion As String = "???"
    Public SourceFile As String
    Public NewWorkspaceName As String

    Public ProjectName As String
    'Public FCM_SubProjectName As String

    Public Save_HCF_A2LFilename As String
    Public Save_HCS_A2LFilename As String
    Public Save_LC_A2LFilename As String
    Public Save_FCM_A2LFilename As String
    Public Save_ACP2_A2LFilename As String
    Public Save_ACP3_A2LFilename As String
    Public Save_ACP4_A2LFilename As String
    Public Save_ARXMLFilename As String

    Public ConfigureForNewSoftwareVersion As Boolean
    Public ProcessNewARXMLFile As Boolean
    Public ProcessNewDBCFiles As Boolean

    'Public SaveProjectFiles() As String = {"N/A", "N/A", "N/A", "N/A"}
    Public SaveProjectFiles() As String = {"N/A", "N/A", "N/A", "N/A", "N/A", "N/A"} 'Added two more for FCM variants...

    Public g_ProjectAbbreviation As String
    Public g_SaveINCAWorkspaceTemplateName As String
    Public WorkspaceNameSuffix As String
    Public g_SaveWorkspaceNameSuffix As String

    Public NumberOfCameras As Integer = 5
    Public CameraNames() As String = {"Front", "Rear", "Left", "Right", "Driver", "NA"}

    Public updatingVehicleList As Boolean

    Public Username As String

    Public Sub CopyVersionSpecificWorkspaceTemplate()

        '7.	Copy existing template files to new names for new software version and model year (automate)

        '154_MY22_HC_2P
        '154_MY22_LC_1P
        '153_MY22_CSAV2_3P3R
        '153_MY22_CSAV2_3P3R523
        '153_MY22_CSAV2_3P3R592

        Dim INCAWorkspaceTemplateName() As String
        Dim NewWorkspaceName() As String

        Dim myHWSystems() As HWSystem
        Dim myHWDevices() As HWDevice
        Dim y As Integer
        Dim z As Integer

        Dim myFolder As Folder

        Dim ImportRequired As Boolean
        Dim CopyRequired As Boolean
        Dim MyDatabaseItems() As DataBaseItem

        Dim x As Integer

        ReDim INCAWorkspaceTemplateName(0)
        ReDim NewWorkspaceName(0)

        'Need to add functionality here to comprehend FCMConfigName to determine how to set up the workspaces based on 
        'how many controllers we need. The templates for LC, HCS and ACP3 are set up with all FCM processors, so these
        'processors must be deleted or the correct one retained here for this to work...

        'This means that the specific vehicle number must be used when setting up the software version and model year
        'specific templates.  This also means multiple templates must be created and maintained for each possible
        'combination and that the first step in the process must always be the vehicle configuration step prior to
        'creating software version and model year templates.

        Dim FCMType As String = ""

        MsgBox(FCMConfigName)

        'Here we need to see if there is an FCM associated with the primary controller...
        'We may need to differentiate further here based on supplier...
        If InStr(FCMConfigName, "FCM") > 0 Then
            FCMType = Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1)
        End If

        Select Case g_ProjectAbbreviation

            Case "LC"
                INCAWorkspaceTemplateName(0) = g_ProjectAbbreviation & "_WorkspaceTemplate"
                If Len(FCMType) = 0 Then
                    NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_1P"
                Else
                    NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_" & FCMType & "_2P"
                End If

            Case "HC"
                INCAWorkspaceTemplateName(0) = g_ProjectAbbreviation & "_WorkspaceTemplate"
                If Len(FCMType) = 0 Then
                    NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_2P"
                Else
                    NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_" & FCMType & "_3P"
                End If

            Case "CSAV2"
                ReDim INCAWorkspaceTemplateName(2)
                INCAWorkspaceTemplateName(0) = "CSAV2_3P3R"
                INCAWorkspaceTemplateName(1) = "CSAV2_3P3R523"
                INCAWorkspaceTemplateName(2) = "CSAV2_3P3R592"
                ReDim NewWorkspaceName(2)
                NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_3P3R"
                NewWorkspaceName(1) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_3P3R523"
                NewWorkspaceName(2) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_3P3R592"

                'FCM CHANGE - Added FCM Case...
            Case "FCM", "FCM100"
                INCAWorkspaceTemplateName(0) = g_ProjectAbbreviation & "_WorkspaceTemplate"
                NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_1P" 'This is FCM or FCM100 STA (Standalone) FCM Only
            Case "ACP2"
                INCAWorkspaceTemplateName(0) = g_ProjectAbbreviation & "_WorkspaceTemplate"
                NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_1P"
            Case "ACP3"
                INCAWorkspaceTemplateName(0) = g_ProjectAbbreviation & "_WorkspaceTemplate"
                If Len(FCMType) = 0 Then
                    NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_1P"
                Else
                    NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_" & FCMType & "_2P"
                End If
            Case "ACP4"
                INCAWorkspaceTemplateName(0) = g_ProjectAbbreviation & "_WorkspaceTemplate"
                NewWorkspaceName(0) = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & g_ProjectAbbreviation & "_1P"

        End Select

        myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")

        For x = 0 To UBound(INCAWorkspaceTemplateName)

            MyDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName(x))

            'If we do not find the template workspace in INCA, we need to find the exported file somewhere, or look for the old filename format version...
            If MyDatabaseItems.Length = 0 Then

                HandleUserMessageLogging("GMRC", "Workspace Template Not found in INCA, Looking elsewhere, Please wait...",,, FLASH_MSG_ON)
                'UserStatusInfo.Label1.Text = "Workspace Template Not found in INCA, Looking elsewhere, Please wait..."

                'We look for the template in two places, first, the install directory, 
                If File.Exists(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName(x) & ".exp") Then

                    HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName(x) & " in " & myFolder.GetName & ", looking in install directory...")
                    'mylistbox.Items.Add("Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & " in " & myFolder.GetName & ", looking in install directory...")

                    ImportRequired = True
                    CopyRequired = True

                End If

            Else
                CopyRequired = True
            End If

            If ImportRequired = True Then

                ImportFileIntoINCA(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName(x) & ".exp", True, False)
                MyDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName(x))

            End If

            If CopyRequired = True Then

                HandleUserMessageLogging("GMRC", "Copying template workspace to " & NewWorkspaceName(x),,, FLASH_MSG_ON)
                'UserStatusInfo.Label1.Text = "Copying template workspace to " & NewWorkspaceName(x)

                Dim myTempDatabaseItems() As DataBaseItem

                myTempDatabaseItems = myFolder.BrowseDataBaseItem(NewWorkspaceName(x))

                If myTempDatabaseItems.Length > 0 Then

                    If MsgBox("Workspace " & NewWorkspaceName(x) & " already exists, recreate?", vbYesNo) = vbYes Then
                        myFolder.RemoveComponent(myTempDatabaseItems(0))
                        MyDatabaseItems(0).Copy(NewWorkspaceName(x))
                    End If

                Else
                    MyDatabaseItems(0).Copy(NewWorkspaceName(x))
                End If

                'Here is where we need to remove processor devices from newly copied NewWorkspaceName(x) based on if FCM, FCM100, or no FCM is being used...

                MyHWC = Get_Workspace(NewWorkspaceName(x), "CLEVIR Setup\Workspaces")

                myHWSystems = MyHWC.GetAllSystems

                HandleUserMessageLogging("GMRC", "Modifying new workspace based on " & NewWorkspaceName(x) & " contents...")

                For z = 0 To UBound(myHWSystems)

                    If InStr(myHWSystems(z).GetName, "Ethernet") > 0 Then

                        myHWDevices = myHWSystems(z).GetAllDevices()

                        Select Case g_ProjectAbbreviation

                            Case "HC"

                                If Len(FCMType) = 0 Then
                                    For y = 0 To UBound(myHWDevices)
                                        Select Case y
                                            Case 0
                                                myHWDevices(y).SetName("HCF")
                                            Case 1
                                                myHWDevices(y).SetName("HCS")
                                            Case Else
                                                MyHWC.RemoveDevice(myHWDevices(y))
                                        End Select
                                    Next
                                Else
                                    For y = 0 To UBound(myHWDevices)
                                        Select Case y
                                            Case 0
                                                myHWDevices(y).SetName("HCF")
                                            Case 1
                                                myHWDevices(y).SetName("HCS")
                                            Case 2
                                                myHWDevices(y).SetName(FCMType)
                                            Case Else
                                                MyHWC.RemoveDevice(myHWDevices(y))
                                        End Select
                                    Next
                                End If


                            Case "LC"
                                If Len(FCMType) = 0 Then
                                    For y = 0 To UBound(myHWDevices)
                                        Select Case y
                                            Case 0
                                                myHWDevices(y).SetName("XETK:1")
                                            Case Else
                                                MyHWC.RemoveDevice(myHWDevices(y))
                                        End Select
                                    Next
                                Else
                                    For y = 0 To UBound(myHWDevices)
                                        Select Case y
                                            Case 0
                                                myHWDevices(y).SetName("XETK:1")
                                            Case 1
                                                myHWDevices(y).SetName(FCMType)
                                            Case Else
                                                MyHWC.RemoveDevice(myHWDevices(y))
                                        End Select
                                    Next
                                End If

                            Case "FCM"

                                For y = 0 To UBound(myHWDevices)
                                    Select Case y
                                        Case 0
                                            myHWDevices(y).SetName("FCM")
                                        Case Else
                                            MyHWC.RemoveDevice(myHWDevices(y))
                                    End Select
                                Next

                            Case "FCM100"

                                For y = 0 To UBound(myHWDevices)
                                    Select Case y
                                        Case 0
                                            myHWDevices(y).SetName("FCM100")
                                        Case Else
                                            MyHWC.RemoveDevice(myHWDevices(y))
                                    End Select
                                Next

                            Case "ACP3"

                                If Len(FCMType) = 0 Then
                                    For y = 0 To UBound(myHWDevices)
                                        Select Case y
                                            Case 0
                                                myHWDevices(y).SetName("ACP3_MCU")
                                            Case Else
                                                MyHWC.RemoveDevice(myHWDevices(y))
                                        End Select
                                    Next
                                Else
                                    For y = 0 To UBound(myHWDevices)
                                        Select Case y
                                            Case 0
                                                myHWDevices(y).SetName("ACP3_MCU")
                                            Case 1
                                                myHWDevices(y).SetName(FCMType)
                                            Case Else
                                                MyHWC.RemoveDevice(myHWDevices(y))
                                        End Select
                                    Next
                                End If

                        End Select

                    End If

                Next z

                '8.	Prompt user to map ARXML clusters or DBC files to CAN devices (manual)

                'If ProcessNewDBCFiles = True Or ProcessNewARXMLFile = True Then
                If MsgBox("Do you wish to associate new CAN information to CAN Monitoring Channels in " & NewWorkspaceName(x) & " before proceeding?", vbYesNo) = vbYes Then

                    MsgBox("Please associate new CAN information to CAN Monitoring Channels in " & NewWorkspaceName(x) & " before proceeding...")

                    If MsgBox("Export " & NewWorkspaceName(x) & " to " & My.Application.Info.DirectoryPath & " directory now?", vbYesNo) = vbYes Then
                        myTempDatabaseItems = myFolder.BrowseDataBaseItem(NewWorkspaceName(x))
                        If myTempDatabaseItems.Length > 0 Then
                            myTempDatabaseItems(0).ExportToFile(My.Application.Info.DirectoryPath & "\" & myTempDatabaseItems(0).GetName & ".exp", False, True)
                        End If

                    End If

                End If

            End If

        Next x

        UserStatusInfo.Hide()

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

        Dim vehiclenumbers As String = ""

        Dim filename As String

        Dim NewFileFormat() As String = Nothing

        Dim fnum2 As Integer

        Try

            updatingVehicleList = True

            HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles called...")

            'NETWORK DRIVE MAPPING

            If System.IO.Directory.Exists(NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\Development\CLEVIR Vehicle Configurations") Then

                filename = My.Application.Info.DirectoryPath & "\VehicleConfigurations.csv"

                If System.IO.File.Exists(filename) Then

                    'Copy existing VehicleConfigurations.csv file...
                    System.IO.File.Copy(filename, My.Application.Info.DirectoryPath & "\VehicleConfigurations_SAVE.csv", True)

                    fnum = FreeFile()

                    'Hold open the existing file to overwrite contents with updated info from all other vehicles...
                    FileOpen(fnum, filename, OpenMode.Output)

                ElseIf System.IO.File.Exists(My.Application.Info.DirectoryPath & "\VehicleConfigurations_SAVE.csv") Then

                    HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles: Copying VehicleConfigurations_SAVE.csv to VehicleConfigurations.csv ")

                    System.IO.File.Copy(My.Application.Info.DirectoryPath & "\VehicleConfigurations_SAVE.csv", filename, True)

                    fnum = FreeFile()

                    'Hold open the existing file to overwrite contents with updated info from all other vehicles...
                    FileOpen(fnum, filename, OpenMode.Output)

                Else
                    HandleUserMessageLogging("GMRC", "vehicleconfigurations.csv file does not exist or is corrupted.  Please select VEHICLE ID NOT IN LIST and re-configure this vehicle.", DISPLAY_MSG_BOX)
                    'MsgBox("vehicleconfigurations.csv file does not exist or is corrupted.  Please select VEHICLE ID NOT IN LIST and re-configure this vehicle.")

                    Exit Sub
                End If

                Dim dir As DirectoryInfo = New DirectoryInfo(NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\Development\CLEVIR Vehicle Configurations")
                Dim files As FileInfo()
                Dim dirs As DirectoryInfo() = dir.GetDirectories()

                mylistbox.Items.Clear()

                If WhoAmI = "CLEVIR" Then
                    mylistbox.Items.Add(" VEHICLE ID NOT IN LIST")
                End If

                For x = 0 To UBound(dirs)

                    files = dirs(x).GetFiles
                    For z = 0 To UBound(files)
                        If UCase(files(z).Name) = "VEHICLECONFIGURATIONS.CSV" Then
                            fnum2 = FreeFile()
                            FileOpen(fnum2, files(z).FullName, OpenMode.Input)

                            'Copy header as first line in local file to be updated...
                            If Len(textline) = 0 Then
                                textline = LineInput(fnum2)
                                PrintLine(fnum, textline)
                            End If

                            'Go thru file from Q drive, add each line if vehicle number does not already exist in file...
                            'This method assumes that the existing entries for a given vehicle number that are common in multiple vehicles
                            'are the same.

                            Do While Not EOF(fnum2)
                                textline = LineInput(fnum2)
                                lineitems = Split(textline, ",")

                                If (UCase(dirs(x).Name) = UCase(lineitems(0))) And InStr(vehiclenumbers, lineitems(0)) = 0 Then
                                    vehiclenumbers = vehiclenumbers & "," & lineitems(0)
                                    PrintLine(fnum, textline)

                                    mylistbox.Items.Add(dirs(x).Name)
                                    mylistbox.SelectedIndex = mylistbox.Items.Count - 1
                                    mylistbox.Refresh()

                                    Exit Do
                                End If

                            Loop

                            FileClose(fnum2)
                            Exit For

                        End If

                    Next

                Next

                FileClose(fnum)

                HandleUserMessageLogging("GMRC", "Update operation complete.", DISPLAY_MSG_BOX, SEND_LIVE_UPDATE)
                'MsgBox("Update operation complete.")
                'UpdateVehicleStatus("Flashomatic Update operation complete.")


            Else
                HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles: Cannot connect to " & NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\Development\CLEVIR Vehicle Configurations directory.  Please verify network connection...", DISPLAY_MSG_BOX)
                'MsgBox("Cannot connect to " & NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\Development\CLEVIR Vehicle Configurations directory.  Please verify network connection...")
            End If

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "CheckVehicleConfigFiles: " & ex.Message, DISPLAY_MSG_BOX)
            'MsgBox(ex.Message)
        End Try

        updatingVehicleList = False

    End Sub

    Public Function CastDBItemToWorkbaseDevice(ByRef x As Object) As WorkbaseDevice

        'This recasts a generic object to an object of type WorkbaseDevice

        CastDBItemToWorkbaseDevice = x
    End Function
    Public Function CastDBItemToExpEnv(ByRef x As Object) As ExperimentEnvironment

        'This recasts a generic object to an object of type ExperimentEnvironment

        CastDBItemToExpEnv = x
    End Function

    Public Function CastItemToExpView(ByRef x As Object) As ExperimentView

        'This recasts a generic object to an object of type ExperimentView
        CastItemToExpView = x
    End Function

    Public Sub SetupCLEVIRDatabase(ByVal myActualDatabase As Object)

        'Called from HandleDatabase.  If necessary, adds the CLEVIR Setup top level folder and the Experiments and Workspaces sub-folders...

        Dim myfolder As IncaFolder
        Dim mysubfolder As IncaFolder

        myfolder = myActualDatabase.GetFolder("CLEVIR Setup")

        If Not myfolder Is Nothing Then
            mysubfolder = myfolder.GetSubFolder("Experiments")
            If mysubfolder Is Nothing Then
                mysubfolder = myfolder.AddSubFolder("Experiments")
            End If
            mysubfolder = myfolder.GetSubFolder("Workspaces")
            If mysubfolder Is Nothing Then
                mysubfolder = myfolder.AddSubFolder("Workspaces")
            End If
        Else
            myfolder = myActualDatabase.AddFolder("CLEVIR Setup")

            mysubfolder = myfolder.AddSubFolder("Experiments")
            mysubfolder = myfolder.AddSubFolder("Workspaces")

            'myinca.SetOption("MODULE: USEROPTIONS; OPTIONNAME: [Measure-General]MdfFileType; OPTIONVALUE: mdf 4.0")

        End If

    End Sub
    Public Function CreateA2LPTPProject(ByVal a2lFile As String, ByVal ptpfile As String, ByVal mylistbox As ListBox) As Asap2Project

        'Called from TwoA2lsAndPTPsToVehicleSpecificWorkspace, TwoA2lsAndPTPsToVehicleSpecificWorkspaceOLD, and
        'A2lAndPTPToVehicleSpecificWorkspace and A2lAndPTPToVehicleSpecificWorkspaceOLD

        'Creates a new project from a2l and ptp file.  Places project into CLEVIR Setup\Projects INCA folder...

        Dim VersionFolderName As String
        Dim tempstr As String
        Dim devicename As String
        Dim l_projectname As String
        Dim myfolder As IncaFolder = Nothing
        Dim mysubfolder As IncaFolder = Nothing
        Dim mysubsubfolder As IncaFolder = Nothing
        Dim mysubsubsubfolder As IncaFolder = Nothing

        Dim myAsap2Project As Asap2Project = Nothing

        Dim myCLEVIRProjectsSubfolder As IncaFolder = Nothing

        CreateA2LPTPProject = Nothing

        'Connect to INCA, or verify already connected...
        If ConnectToInca() = "True" Then

            'ASE34_LC_202013906_quasi.a2l

            HandleUserMessageLogging("GMRC", "A2l Filename: " & a2lFile)

            'Determine devicename based on a2l filename...

            tempstr = Path.GetFileName(a2lFile)

            If InStr(tempstr, "IPa") Then
                devicename = "IP"
                l_projectname = "CSAV2"
                tempstr = Mid(tempstr, InStr(tempstr, "IPa") + 3, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "IP") Then

                devicename = "IP"
                l_projectname = "CSAV2"
                tempstr = Mid(tempstr, InStr(tempstr, "IP") + 2, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "IRa") Then
                devicename = "IR"
                l_projectname = "CSAV2"
                tempstr = Mid(tempstr, InStr(tempstr, "IRa") + 3, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "IR") Then

                devicename = "IR"
                l_projectname = "CSAV2"
                tempstr = Mid(tempstr, InStr(tempstr, "IR") + 2, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "K1P") > 0 Then
                devicename = "K1P"
                l_projectname = "CSAV2"
                tempstr = Mid(tempstr, InStr(tempstr, "K1P") + 3, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "K2P") > 0 Then
                devicename = "K2P"
                l_projectname = "CSAV2"
                tempstr = Mid(tempstr, InStr(tempstr, "K2P") + 3, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)
            ElseIf InStr(tempstr, "K1R") > 0 Then
                devicename = "K1R"
                l_projectname = "CSAV2"
                tempstr = Mid(tempstr, InStr(tempstr, "K1R") + 3, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)
            ElseIf InStr(tempstr, "K2R") > 0 Then
                devicename = "K2R"
                l_projectname = "CSAV2"
                tempstr = Mid(tempstr, InStr(tempstr, "K2R") + 3, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)
            ElseIf InStr(tempstr, "_LC_") > 0 Then

                'ASE34_LC_202013906_quasi.a2l

                devicename = "ASE34"
                l_projectname = "LowContent"
                tempstr = Mid(tempstr, InStr(tempstr, "_LC_") + 4, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

                'HC CHANGE
            ElseIf InStr(tempstr, "HCF") > 0 Then

                devicename = "ASE37"
                l_projectname = "HighContent"
                tempstr = Mid(tempstr, InStr(tempstr, "_HCF_") + 5, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "HCS") > 0 Then

                devicename = "ASE37"
                l_projectname = "HighContent"
                tempstr = Mid(tempstr, InStr(tempstr, "_HCS_") + 5, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

                'FCM CHANGE - Added FCM Condition to handle creation of FCM INCA Project...
                'This will not work for FCM100 until we get an a2l file named FCM100 instead of FCM_
            ElseIf InStr(tempstr, "FCM") > 0 Then

                'FCM_STA_ZF1_222215312.a2l - FCM standalone can come from either supplier...
                'FCM_STA_VEO_232315503.a2l
                'FCM_LCM_ZF1_222215306.a2l - FCM low and high can only come from ZF...
                'FCM_LCH_ZF1_222215306.a2l
                'FCM100_STA_ZF1_222215306.a2l - FCM100 standalone can come from either supplier...
                'FCM100_LCM_ZF1_232315403.a2l
                'FCM100_LCH_ZF1_232315403.a2l - FCM100 LCH can come from either supplier...
                'FCM100_STA_VEO_222215306.a2l
                'FCM100_LCM_VEO_232315403.a2l - FCM100 LCM can come from either supplier...
                'FCM100_LCH_VEO_232315403.a2l


                Dim FCMtempstr() As String

                FCMtempstr = Split(tempstr, "_")
                l_projectname = FCMtempstr(0) & "_" & FCMtempstr(1) & "_" & FCMtempstr(2)
                'devicename = "FCM"
                devicename = FCMtempstr(0)

                VersionFolderName = Mid(FCMtempstr(3), 1, 2) & "." & Mid(FCMtempstr(3), 3, 2) & "." & Mid(FCMtempstr(3), 5, 3) & "." & Mid(FCMtempstr(3), 8, 2)

                'This elseif will not be reached, this condition is handled above.  Kept this here for reference in case something changes with a2l file naming conventions...
            ElseIf InStr(tempstr, "FCM100") > 0 Then

                Dim FCMtempstr() As String

                FCMtempstr = Split(tempstr, "_")
                l_projectname = FCMtempstr(0) & "_" & FCMtempstr(1) & "_" & FCMtempstr(2)
                devicename = "FCM100"

                VersionFolderName = Mid(FCMtempstr(3), 1, 2) & "." & Mid(FCMtempstr(3), 3, 2) & "." & Mid(FCMtempstr(3), 5, 3) & "." & Mid(FCMtempstr(3), 8, 2)

            ElseIf InStr(tempstr, "ACP2") > 0 Then

                'ASE34_LC_202013906_quasi.a2l
                'ACP30M1232315610AA_ASTA_quasi_INCA

                devicename = "ACP2"
                l_projectname = "ACP2"
                tempstr = Mid(tempstr, InStr(tempstr, "ACP2") + 7, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "ACP3") > 0 Then

                'ASE34_LC_202013906_quasi.a2l

                devicename = "ACP3"
                l_projectname = "ACP3"
                tempstr = Mid(tempstr, InStr(tempstr, "ACP3") + 7, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "ACP4") > 0 Then

                'ASE34_LC_202013906_quasi.a2l

                devicename = "ACP4"
                l_projectname = "ACP4"
                tempstr = Mid(tempstr, InStr(tempstr, "ACP4") + 7, 9)
                VersionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)


            Else
                devicename = "Invalid"
                HandleUserMessageLogging("GMRC", "Invalid a2l file selected, Project not created...", DISPLAY_MSG_BOX,,, mylistbox)
                'UserStatusInfo.Label1.Text = "Invalid a2l file selected, Project not created..."
                'mylistbox.Items.Add("Invalid a2l file selected, Project not created...")
                UserStatusInfo.Hide()
                'MsgBox("Invalid a2l file selected, Project not created...")
                Exit Function
            End If


            'Find or add folders that project will be created in...
            HandleUserMessageLogging("GMRC", "Getting folder for " & l_projectname)
            myfolder = myActualDatabase.GetFolder(l_projectname)

            If l_projectname = "CSAV2" Then

                If Not myfolder Is Nothing Then
                    HandleUserMessageLogging("GMRC", "Found " & ProjectName & " folder")
                    mysubfolder = myfolder.GetSubFolder("DV3")
                    If Not mysubfolder Is Nothing Then
                        HandleUserMessageLogging("GMRC", "Found DV3 folder")
                        mysubsubfolder = mysubfolder.GetSubFolder("Projects")
                        If Not mysubsubfolder Is Nothing Then
                            mysubsubsubfolder = mysubsubfolder.GetSubFolder(devicename)
                            If mysubsubsubfolder Is Nothing Then
                                mysubsubsubfolder = mysubsubfolder.AddSubFolder(devicename)
                                myCLEVIRProjectsSubfolder = mysubsubsubfolder.AddSubFolder(VersionFolderName)
                            Else
                                myCLEVIRProjectsSubfolder = mysubsubsubfolder.GetSubFolder(VersionFolderName)
                                If myCLEVIRProjectsSubfolder Is Nothing Then
                                    myCLEVIRProjectsSubfolder = mysubsubsubfolder.AddSubFolder(VersionFolderName)
                                End If
                            End If

                        Else
                            mysubsubfolder = mysubfolder.AddSubFolder("Projects")
                            mysubsubsubfolder = mysubsubfolder.AddSubFolder(devicename)
                            myCLEVIRProjectsSubfolder = mysubsubsubfolder.AddSubFolder(VersionFolderName)
                        End If

                    Else
                        mysubfolder = myfolder.AddSubFolder("DV3")
                        mysubsubfolder = mysubfolder.AddSubFolder("Projects")
                        mysubsubsubfolder = mysubsubfolder.AddSubFolder(devicename)
                        myCLEVIRProjectsSubfolder = mysubsubsubfolder.AddSubFolder(VersionFolderName)
                    End If

                Else
                    myfolder = myActualDatabase.AddFolder(l_projectname)
                    mysubfolder = myfolder.AddSubFolder("DV3")
                    mysubsubfolder = mysubfolder.AddSubFolder("Projects")
                    mysubsubsubfolder = mysubsubfolder.AddSubFolder(devicename)
                    myCLEVIRProjectsSubfolder = mysubsubsubfolder.AddSubFolder(VersionFolderName)

                End If

                l_projectname = l_projectname & "\DV3"

            Else 'Not CSAV2

                If Not myfolder Is Nothing Then
                    HandleUserMessageLogging("GMRC", "Found " & l_projectname & " folder")
                    mysubfolder = myfolder.GetSubFolder("Projects")
                    If Not mysubfolder Is Nothing Then
                        HandleUserMessageLogging("GMRC", "Found Projects folder - Getting " & devicename & " subfolder")
                        mysubsubfolder = mysubfolder.GetSubFolder(devicename)
                        If mysubsubfolder Is Nothing Then
                            HandleUserMessageLogging("GMRC", "Adding sub-folder for " & devicename)
                            mysubsubfolder = mysubfolder.AddSubFolder(devicename)
                            HandleUserMessageLogging("GMRC", "Adding sub-folder for " & VersionFolderName)
                            myCLEVIRProjectsSubfolder = mysubsubfolder.AddSubFolder(VersionFolderName)
                        Else
                            HandleUserMessageLogging("GMRC", "Found " & devicename & " folder")
                            myCLEVIRProjectsSubfolder = mysubsubfolder.GetSubFolder(VersionFolderName)
                            If myCLEVIRProjectsSubfolder Is Nothing Then
                                HandleUserMessageLogging("GMRC", "Adding sub-folder for " & VersionFolderName)
                                myCLEVIRProjectsSubfolder = mysubsubfolder.AddSubFolder(VersionFolderName)
                            End If
                        End If
                    Else
                        HandleUserMessageLogging("GMRC", "Adding Projects sub-folder for " & l_projectname)
                        mysubfolder = myfolder.AddSubFolder("Projects")
                        HandleUserMessageLogging("GMRC", "Adding sub-folder for " & devicename)
                        mysubsubfolder = mysubfolder.AddSubFolder(devicename)
                        myCLEVIRProjectsSubfolder = mysubsubfolder.AddSubFolder(VersionFolderName)
                    End If

                Else

                    HandleUserMessageLogging("GMRC", "Adding folder for " & l_projectname)
                    myfolder = myActualDatabase.AddFolder(l_projectname)
                    HandleUserMessageLogging("GMRC", "Adding Projects sub-folder for " & l_projectname)
                    mysubfolder = myfolder.AddSubFolder("Projects")
                    HandleUserMessageLogging("GMRC", "Adding sub-folder for " & devicename)
                    mysubsubfolder = mysubfolder.AddSubFolder(devicename)
                    HandleUserMessageLogging("GMRC", "Adding sub-folder for " & VersionFolderName)
                    myCLEVIRProjectsSubfolder = mysubsubfolder.AddSubFolder(VersionFolderName)

                End If
            End If

            'Create the project from the A2l and PTP files...

            If Not myCLEVIRProjectsSubfolder Is Nothing Then

                HandleUserMessageLogging("GMRC", "myCLEVIRProjectsSubFolder = " & myCLEVIRProjectsSubfolder.GetNameWithPath,,,, mylistbox)
                'CopyToLog("myCLEVIRProjectsSubFolder = " & myCLEVIRProjectsSubfolder.GetNameWithPath)
                HandleUserMessageLogging("GMRC", "Creating Project from A2l And ptp files...",,, FLASH_MSG_ON, mylistbox)
                'UserStatusInfo.Label1.Text = "Creating Project from A2l And ptp files..."
                HandleUserMessageLogging("GMRC", "A2l File = " & a2lFile,,,, mylistbox)
                HandleUserMessageLogging("GMRC", "PTP File = " & ptpfile,,,, mylistbox)

                'mylistbox.Items.Add("myCLEVIRProjectsSubFolder = " & myCLEVIRProjectsSubfolder.GetNameWithPath)
                'mylistbox.Items.Add("Creating Project from A2l And ptp files...")
                'mylistbox.Items.Add("A2l File = " & a2lFile)
                'mylistbox.Items.Add("PTP File = " & ptpfile)

                'myDatabaseItems = myCLEVIRProjectsSubfolder.GetAllDataBaseItems()
                'If Not myDatabaseItems Is Nothing Then
                'For x = 0 To UBound(myDatabaseItems)
                'If InStr(a2lFile, myDatabaseItems(x).GetName) > 0 Then

                ' UserStatusInfo.Label1.Text = "Project " & myDatabaseItems(x).GetName & " already exists."
                'mylistbox.Items.Add("Project " & myDatabaseItems(x).GetName & " already exists.")
                'mylistbox.SelectedIndex = mylistbox.Items.Count - 1
                'mylistbox.Refresh()
                'System.Threading.Thread.Sleep(2000)
                'UserStatusInfo.Hide()
                'CreateA2LPTPProject = myAsap2Project
                'Exit Function
                'End If
                'Next
                'End If

                'We have seen rare instances where this INCA API method call hangs up.  Do not know why...
                'If this happens, it requires killing INCA and CLEVIR processes from the Windows Task Manager...

                myAsap2Project = myCLEVIRProjectsSubfolder.ReadASAP2FileAndHexFile(a2lFile, ptpfile)

                If Not myAsap2Project Is Nothing Then
                    CreateA2LPTPProject = myAsap2Project
                Else
                    HandleUserMessageLogging("GMRC", "Could Not create project...",,, FLASH_MSG_1_SEC, mylistbox)
                    'UserStatusInfo.Label1.Text = "Could Not create project..."
                    'mylistbox.Items.Add("Could Not create project...")
                    'mylistbox.SelectedIndex = mylistbox.Items.Count - 1
                    'mylistbox.Refresh()
                    'System.Threading.Thread.Sleep(1000)
                    'UserStatusInfo.Hide()
                    Exit Function
                End If

            End If

            HandleUserMessageLogging("GMRC", "Project Creation Complete.  New Project Is in the " & l_projectname & "\Projects\" & devicename & "\" & VersionFolderName & " folder.",,, FLASH_MSG_2_SEC, mylistbox)
            'UserStatusInfo.Label1.Text = "Project Creation Complete.  New Project Is in the " & l_projectname & "\Projects\" & devicename & "\" & VersionFolderName & " folder."
            'mylistbox.Items.Add("Project Creation Complete.  New Project Is in the " & l_projectname & "\Projects\" & devicename & "\" & VersionFolderName & " folder.")
            'mylistbox.SelectedIndex = mylistbox.Items.Count - 1
            'mylistbox.Refresh()
            'System.Threading.Thread.Sleep(2000)
            'UserStatusInfo.Hide()

        Else
            HandleUserMessageLogging("GMRC", "Could Not connect to INCA, Project Not created...", DISPLAY_MSG_BOX,, FLASH_MSG_1_SEC, mylistbox)

            'UserStatusInfo.Label1.Text = "Could Not connect to INCA, Project Not created..."
            'mylistbox.Items.Add("Could Not connect to INCA, Project Not created...")
            'mylistbox.SelectedIndex = mylistbox.Items.Count - 1
            'mylistbox.Refresh()
            'System.Threading.Thread.Sleep(500)
            'UserStatusInfo.Hide()
            'MsgBox("Could Not connect to INCA, Project Not created...")
        End If

    End Function
    Public Function CopyWorkspaceTemplateNEW(ByRef NewWorkspaceName As String, ByVal mylistbox As ListBox) As HardwareConfiguration

        'Called from A2lAndCALToVehicleSpecificWorkspace - checks for existance of
        'INCAWorkspaceTemplateName, if found in INCA it copies the template to a newworkspacename that has been built based on
        'the selected PTP file and workspace template name.  If template not found, looks in app folder and on Q drive for template.  
        'If template Then found On Q drive file is copied locally and then imported into INCA and then copied to the newworkspacename.

        'This functionality requires that template files for each model year and software version be created manually and made available
        'in the application install directory.  This is due to the fact that the ARXML files may change with either a new software
        'version or for a new model year.  Once these files are created, they should be placed into the 
        'Q:\CSAV2 Tools\CLEVIR\Updated CLEVIR Files for Vehicles\UpdatedFiles\ "ProjectName" folder so that they are automatically copied
        'into the CLEVIR install directory when CLEVIR is launched...

        'If the proper files are not found, a generic template will be used to create the workspace.  This will likely cause data collection
        'issues due to the fact that the ARXML file in the workspace may not be consistent with the experiment that corresponds to the
        'software version and model year.


        Dim myFolder As Folder

        Dim ImportRequired As Boolean
        Dim CopyRequired As Boolean
        Dim MyDatabaseItems() As DataBaseItem

        CopyWorkspaceTemplateNEW = Nothing

        myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")

        'If Debugger.IsAttached Then
        If ConfigureForNewSoftwareVersion = True Then

                ReadAutosarFile()
                CopyVersionSpecificWorkspaceTemplate()

            End If
        'End If

        'check for existing templateworkspacename here...

        MyDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName)

        'If we do not find the template workspace in INCA, we need to find the exported file somewhere, or look for the old filename format version...
        If MyDatabaseItems.Length = 0 Then

            HandleUserMessageLogging("GMRC", "Template Workspace Not found in INCA, Looking elsewhere, Please wait...",,, FLASH_MSG_ON)
            'UserStatusInfo.Label1.Text = "Template Workspace Not found in INCA, Looking elsewhere, Please wait..."

            'We look for the template in two places, first, the install directory, 
            If File.Exists(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp") Then

                HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & " in " & myFolder.GetName & ", found in install directory...",,,, mylistbox)
                'mylistbox.Items.Add("Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & " in " & myFolder.GetName & ", found in install directory...")

                ImportRequired = True
                CopyRequired = True

            Else
                'Set template name back to master template file...

                'If this condition occurs, we may lose some CAN data because the master template may not have the same ARXML file
                'associated with it as the model year and software version specific experiment!!!

                HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & ", looking for Default TemplateWorkspace...",,,, mylistbox)
                'CopyToLog("Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & ", looking for Default TemplateWorkspace...")
                'mylistbox.Items.Add("Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & ", looking for Default TemplateWorkspace...")

                'FCM CHANGE - This change was made when adding FCM functionality, but this is actually a bug that would manifest itself for either FCM, LC or HC...
                'Rather than use the ProjectName _WorkspaceTemplate here, we should be using the generic workspace that was created from the WorkspaceTemplate when
                'the vehicle was first configured.  These workspaces should be made available in the CLEVIR UpdatedFiles folder on the share drive so that they
                'are always available if required...

                'INCAWorkspaceTemplateName = g_ProjectAbbreviation & "_WorkspaceTemplate"
                INCAWorkspaceTemplateName = g_SaveINCAWorkspaceTemplateName

                WorkspaceNameSuffix = g_SaveWorkspaceNameSuffix

                'NewWorkspaceName = Mid(NewWorkspaceName, 1, InStr(NewWorkspaceName, "_MY") - 4) & WorkspaceNameSuffix

                If ProjectName = "HighContent" Then
                    NewWorkspaceName = DetermineNewWorkspaceName(SaveProjectFiles(1), SaveProjectFiles(3))
                Else
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
                End If

                MyDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName)

                If MyDatabaseItems.Length = 0 Then

                    If File.Exists(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp") Then

                        HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & " in " & myFolder.GetName & ", found in install directory...",,,, mylistbox)

                        'CopyToLog("Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & " in " & myFolder.GetName & ", found in install directory...")
                        'mylistbox.Items.Add("Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & " in " & myFolder.GetName & ", found in install directory...")

                        ImportRequired = True
                        CopyRequired = True

                    Else

                        HandleUserMessageLogging("GMRC", INCAWorkspaceTemplateName & ".exp" & " Not found. Failed to Copy workspace template...", DISPLAY_MSG_BOX,, FLASH_MSG_ON, mylistbox)

                        'UserStatusInfo.Label1.Text = INCAWorkspaceTemplateName & ".exp" & " Not found. Failed to Copy workspace template..."
                        'mylistbox.Items.Add(INCAWorkspaceTemplateName & ".exp" & " Not found. Failed to Copy workspace template...")
                        'mylistbox.SelectedIndex = mylistbox.Items.Count - 1
                        'mylistbox.Refresh()
                        UserStatusInfo.Hide()
                        'MsgBox(INCAWorkspaceTemplateName & ".exp" & " Not found. Failed to Copy workspace template...")

                        Exit Function

                    End If

                Else 'if we find the old format workspace template in INCA, we use it to create the new workspace.

                    CopyRequired = True

                End If

            End If

        Else

            CopyRequired = True

        End If

        If ImportRequired = True Then

            HandleUserMessageLogging("GMRC", "Importing " & INCAWorkspaceTemplateName & ".exp",,,, mylistbox)
            'mylistbox.Items.Add("Importing " & INCAWorkspaceTemplateName & ".exp")
            ImportFileIntoINCA(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp", True, False)
            MyDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName)

        End If

        If CopyRequired = True Then
            HandleUserMessageLogging("GMRC", "Copying template workspace to " & NewWorkspaceName,,,, mylistbox)
            'UserStatusInfo.Label1.Text = "Copying template workspace to " & NewWorkspaceName
            'mylistbox.Items.Add("Copying template workspace to " & NewWorkspaceName & "...")

            Dim myTempDatabaseItems() As DataBaseItem

            myTempDatabaseItems = myFolder.BrowseDataBaseItem(NewWorkspaceName)

            If myTempDatabaseItems.Length > 0 Then
                myFolder.RemoveComponent(myTempDatabaseItems(0))
            End If

            MyDatabaseItems(0).Copy(NewWorkspaceName)
            mylistbox.Items.Add("Copying complete.")
            mylistbox.SelectedIndex = mylistbox.Items.Count - 1
            mylistbox.Refresh()

        End If

        If SetupCameraNamesInWorkspace(NewWorkspaceName) = True Then

            CopyWorkspaceTemplateNEW = Get_Workspace(NewWorkspaceName, "CLEVIR Setup\Workspaces")

        Else
            HandleUserMessageLogging("GMRC", "Camera configuration did Not complete successfully.", DISPLAY_MSG_BOX)
            'MsgBox("Camera configuration did Not complete successfully.")
        End If

        UserStatusInfo.Hide()

    End Function

    Sub UpdateVehicleStatus(ByVal UpdateText As String)

        Dim fnum As Integer
        Dim filename As String
        Dim textline As String

        'If StatusUpdatesOn = True Then

        If System.IO.Directory.Exists(NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\Development\VehicleStatusUpdates") Then

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

            UpdateText = Format(Now, "MM/dd/yyyy HH:mm:ss - ") & VehicleNumber & " " & UpdateText

            filename = NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\Development\VehicleStatusUpdates\VehicleStatusInfo.txt"
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
            CopyToLog("UpdateVehicleStatus: Directory " & NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\Development\VehicleStatusUpdates not found...")
        End If

        'End If

    End Sub

    Public Function SetupCameraNamesInWorkspace(ByVal WorkspaceName As String) As Boolean

        'This function is used as part of the Import software and cals process.  Sets up camera names in newly created workspace based on
        'the contents of the vehicleconfigurations.csv file.  This is new functionality in 5.4.11...

        'All template workspaces contain references to 6 cameras with generic names.  Names in workspace are changed to camera names associated with
        'user specified vehicle number in vehicleconfigurations.csv file.  If there are less cameras defined in file, extra ONVIF video
        'devices are removed from the newly created workspace...

        Dim myHWDevices() As HWDevice

        Dim NumCameraNamesSet As Integer

        Dim myHWSystems() As HWSystem

        SetupCameraNamesInWorkspace = False

        Try

            'MyHWC = Get_Workspace(NewWorkspaceName, "CLEVIR Setup\Workspaces")
            MyHWC = Get_Workspace(WorkspaceName, "CLEVIR Setup\Workspaces")

            myHWSystems = MyHWC.GetAllSystems

            For x = 0 To UBound(myHWSystems)

                'MsgBox(myHWSystems(x).GetName)

                If InStr(myHWSystems(x).GetName, "ONVIF") > 0 Then

                    If NumberOfCameras = 0 Then
                        MyHWC.RemoveSystem(myHWSystems(x))
                    Else

                        myHWDevices = myHWSystems(x).GetAllDevices()

                        NumCameraNamesSet = NumCameraNamesSet + 1

                        If NumCameraNamesSet <= NumberOfCameras Then

                            'String optionModule = "HWC";
                            Dim optionModule = "HWC"

                            'String optionHwcPathName = hwConfig.GetParentFolder().GetNameWithPath() + @"\" + hwConfig.GetName();
                            Dim optionHwcPathName As String = MyHWC.GetParentFolder().GetNameWithPath() & "\" & MyHWC.GetName()

                            'String optionHwItemName = HWDevice.GetName();
                            Dim optionHwItemName As String = myHWDevices(0).GetName()

                            'String optionName = "HWItemName";
                            Dim optionName = "HWItemName"

                            'String setOptionParameter =

                            'String.Format(
                            '"MODULE:{0};HWCPATHNAME:{1};HWITEMNAME:{2};OPTIONNAME:{3};OPTIONVALUE:{4}",
                            'optionModule, optionHwcPathName, optionHwItemName, optionName, deviceName);

                            Dim setOptionParameter = String.Format("MODULE:{0};HWCPATHNAME:{1};HWITEMNAME:{2};OPTIONNAME:{3};OPTIONVALUE:{4}", optionModule, optionHwcPathName, optionHwItemName, optionName, CameraNames(NumCameraNamesSet - 1))

                            myinca.SetOption(setOptionParameter)

                        Else
                            MyHWC.RemoveSystem(myHWSystems(x))
                        End If

                    End If

                End If

            Next

            SetupCameraNamesInWorkspace = True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "SetupCameraNamesInWorkspace: " & ex.Message)
        End Try



    End Function

    Public Function AddSelectedProjectToWorkspace(ByVal mySourceDataset As DataSet, ByVal mysourceProject As Asap2Project, ByVal myDestinationWorkspace As HardwareConfiguration, ByVal WorkspaceName As String, ByVal mylistbox As ListBox) As Boolean

        'Called from A2lAndCALToVehicleSpecificWorkspace. Copies myDestinationWorkspace to WorkspaceName and Changes the workspace dataset
        'of the newly created workspace to mySourceDataset from mysourceProject.
        '
        Dim ProjectDatabasePath As String
        Dim DeviceName As String
        Dim mySourceDatasetPath As String
        Dim myDatasetName As String
        Dim mysourceProjectName As String

        AddSelectedProjectToWorkspace = True

        HandleUserMessageLogging("GMRC", "Adding Project software And cals to New workspace...",,, FLASH_MSG_ON, mylistbox)
        'UserStatusInfo.Label1.Text = "Adding Project software And cals to New workspace..."
        'mylistbox.Items.Add("Adding Project software And cals to New workspace...")

        myDatasetName = mySourceDataset.GetNameWithPath
        mysourceProjectName = mysourceProject.GetName

        'Determine device name based on dataset name...

        If InStr(myDatasetName, "_IP") > 0 Or InStr(myDatasetName, "IPa") Then
            DeviceName = "IP"
        ElseIf InStr(myDatasetName, "_IR") > 0 Or InStr(myDatasetName, "IRa") Then
            DeviceName = "IR"
        ElseIf InStr(myDatasetName, "_IC") > 0 Or InStr(myDatasetName, "ICa") Then
            DeviceName = "IC"
        ElseIf InStr(myDatasetName, "K1P") > 0 Then
            DeviceName = "K1P"
        ElseIf InStr(myDatasetName, "K2P") > 0 Then
            DeviceName = "K2P"
        ElseIf InStr(myDatasetName, "K1R") > 0 Then
            DeviceName = "K1R"
        ElseIf InStr(myDatasetName, "K2R") > 0 Then
            DeviceName = "K2R"
        ElseIf InStr(myDatasetName, "K1C") > 0 Then
            DeviceName = "K1C"
        ElseIf InStr(myDatasetName, "K2C") > 0 Then
            DeviceName = "K2C"
        ElseIf InStr(myDatasetName, "ASE34") > 0 Or InStr(myDatasetName, "EOCM3") > 0 Or InStr(myDatasetName, "_LC_") > 0 Then
            DeviceName = "XETK:1"
            'HC CHANGE
        ElseIf InStr(myDatasetName, "HCF") > 0 Then
            DeviceName = "HCF"
        ElseIf InStr(myDatasetName, "HCS") > 0 Then
            DeviceName = "HCS"

            'FCM CHANGE - Added FCM Condition to set the proper DeviceName for FCM projects when adding project

            'This wont work correctly yet because we do not have any FCM100 files from which to build a project, so there will be no
            'dataset with FCM100 in the dataset name...

        ElseIf InStr(myDatasetName, "FCM_") > 0 Then
            DeviceName = "FCM"
        ElseIf InStr(myDatasetName, "FCM100") > 0 Then
            DeviceName = "FCM100"
        ElseIf InStr(myDatasetName, "ACP2") > 0 Then
            DeviceName = "ACP2_MCU"
        ElseIf InStr(myDatasetName, "ACP3") > 0 Then
            DeviceName = "ACP3_MCU"
        ElseIf InStr(myDatasetName, "ACP4") > 0 Then
            DeviceName = "ACP4_MCU"
        Else
            DeviceName = "Invalid"

            HandleUserMessageLogging("GMRC", "AddSelectedProjectToWorkspace: Invalid Device Name.  Unable to add Project to Workspace...", DISPLAY_MSG_BOX,, FLASH_MSG_ON, mylistbox)
            'UserStatusInfo.Label1.Text = "AddSelectedProjectToWorkspace: Invalid Device Name.  Unable to add Project to Workspace..."
            'mylistbox.Items.Add("AddSelectedProjectToWorkspace: Invalid Device Name.  Unable to add Project to Workspace...")
            'mylistbox.SelectedIndex = mylistbox.Items.Count - 1
            'mylistbox.Refresh()
            UserStatusInfo.Hide()
            'MsgBox("AddSelectedProjectToWorkspace: Invalid Device Name.  Unable to add Project to Workspace...")
            AddSelectedProjectToWorkspace = False
            Exit Function
        End If

        ProjectDatabasePath = mysourceProject.GetParentFolder.GetNameWithPath

        MyHWC = myDestinationWorkspace

        MyHWC.Copy(WorkspaceName)

        MyHWC = Get_Workspace(WorkspaceName)

        mySourceDatasetPath = mySourceDataset.GetNameWithPath

        AddSelectedProjectToWorkspace = ChangeWorkspaceDataset("Ethernet-System:1", DeviceName, mysourceProjectName, ProjectDatabasePath, mySourceDatasetPath)

        'Because High Content ProjectType contains two processors, this routine will be called twice, first for HCS and then for HCF
        'So, we do not want to present this message to the user until both processors are complete.  Since LowContent only has
        'a single processor not named "HCS" and only calls this routine onece, this will work for LowContent as well...

        'FCM CHANGE - Commented this out here and now handle this in the calling routine.  This because with the introduction of FCM
        'the logic below no longer works due to the different datasetname possibilities for different flavors of FCM projects...

        'If InStr(myDatasetName, "HCS") = 0 Then

        'UserStatusInfo.Label1.Text = "Project/Dataset Copy Complete."
        'mylistbox.Items.Add("Project/Dataset Copy Complete.")
        'mylistbox.SelectedIndex = mylistbox.Items.Count - 1
        'mylistbox.Refresh()
        'UserStatusInfo.Hide()

        'End If

    End Function

    Public Function DetermineNewWorkspaceName(ByVal HCS_PTP As String, ByVal HCF_PTP As String) As String

        'Called from TwoA2lsAndPTPsToVehicleSpecificWorkspace and TwoA2lsAndPTPsToVehicleSpecificWorkspaceOLD
        'Creates new workspace name based on selected ptp file names (combination of HCS and HCF ptp file names)...

        'So, this function is only applicable to High Content...

        Dim HCS_FirstPart As String
        Dim HCF_FirstPart As String
        Dim HCS_MiddlePart As String
        Dim HCF_MiddlePart As String
        Dim HCS_SubVersion As String
        Dim HCF_SubVersion As String
        Dim HCS_EndPart As String
        Dim HCF_EndPart As String

        Dim HCS_lineitems() As String
        Dim HCF_lineitems() As String

        HCS_PTP = Mid(Path.GetFileName(HCS_PTP), 1, Len(Path.GetFileName(HCS_PTP)) - 4)
        HCF_PTP = Mid(Path.GetFileName(HCF_PTP), 1, Len(Path.GetFileName(HCF_PTP)) - 4)

        HCS_lineitems = Split(HCS_PTP, "_")
        HCF_lineitems = Split(HCF_PTP, "_")

        HCS_FirstPart = Mid(HCS_PTP, 1, 6)
        HCF_FirstPart = Mid(HCF_PTP, 1, 6)

        If HCS_FirstPart <> HCF_FirstPart Then
            DetermineNewWorkspaceName = "Invalid_Controller_Naming"
        Else
            HCS_MiddlePart = Mid(HCS_PTP, 11, 7)
            HCF_MiddlePart = Mid(HCF_PTP, 11, 7)

            If HCS_MiddlePart <> HCF_MiddlePart Then
                DetermineNewWorkspaceName = "Inconsistent_HCS_HCF_Versions"
            Else
                HCS_EndPart = Mid(HCS_PTP, Len(HCS_lineitems(0)) + 1 + Len(HCS_lineitems(1)) + 1 + Len(HCS_lineitems(2)) + 1, Len(HCS_PTP))
                HCF_EndPart = Mid(HCF_PTP, Len(HCF_lineitems(0)) + 1 + Len(HCF_lineitems(1)) + 1 + Len(HCF_lineitems(2)) + 1, Len(HCF_PTP))

                HCS_SubVersion = Mid(HCS_lineitems(2), 8, Len(HCS_lineitems(2)))
                HCF_SubVersion = Mid(HCF_lineitems(2), 8, Len(HCF_lineitems(2)))

                DetermineNewWorkspaceName = HCS_FirstPart & HCS_MiddlePart & HCS_SubVersion & "_" & HCF_SubVersion & HCS_EndPart & "_" & WorkspaceNameSuffix

            End If
        End If

    End Function




    Public Sub ReadAutosarFile()

        'Reads in a new ARXML file if requested...

        '5.	Unzip ARXML File (automate)
        '6.	Add ARXML file into INCA Or Add DBC files to INCA (automate)


        Dim myincafolder As IncaFolder
        Dim filename As String = ""
        Dim zipfilename As String = ""
        Dim ZipFound As Boolean
        Dim ARXMLFound As Boolean

        Dim dir As DirectoryInfo = New DirectoryInfo(My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML")
        Dim files As FileInfo() = dir.GetFiles()


        If MsgBox("Process updated ARXML File?", vbYesNo) = vbNo Then
            ProcessNewARXMLFile = False

            files = dir.GetFiles()
            For Each file In files
                If InStr(UCase(file.Name), ".ARXML") > 0 Then
                    Save_ARXMLFilename = file.DirectoryName & "\" & file.Name
                    Exit For
                End If
            Next

        Else
            ProcessNewARXMLFile = True
            MsgBox("Please delete all files from the " & My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML folder and place the new ARXML file into this location...")

            For Each file In files
                If InStr(file.Name, ".ARXML") > 0 Then
                    ARXMLFound = True
                    ZipFound = False
                    filename = file.DirectoryName & "\" & file.Name
                    Exit For
                End If
                If InStr(file.Name, ".zip") > 0 Then
                    ZipFound = True
                    zipfilename = file.DirectoryName & "\" & file.Name
                End If
            Next

            If ZipFound = True And ARXMLFound = False Then
                UnzipFile(zipfilename)
            End If

            files = dir.GetFiles()
            For Each file In files
                If InStr(UCase(file.Name), ".ARXML") > 0 Then
                    Save_ARXMLFilename = file.DirectoryName & "\" & file.Name
                    Exit For
                End If
            Next

            If ConnectToInca() = "True" Then

                myincafolder = myActualDatabase.GetFolder("CLEVIR Setup")
                myincafolder.ReadAutosarFile(Save_ARXMLFilename)

            End If

        End If

    End Sub

    Public Sub ReadCANDBFiles()

        'Reads in new CAN DB files if requested...

        Dim myincafolder As IncaFolder
        'Dim filename As String = ""
        'Dim zipfilename As String = ""
        'Dim ZipFound As Boolean
        Dim DBCFileNames() As String = Nothing
        Dim x As Integer

        Dim dir As DirectoryInfo = New DirectoryInfo(My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\DBC")
        Dim files As FileInfo() = dir.GetFiles()


        If MsgBox("Process updated DBC Files?", vbYesNo) = vbNo Then
            ProcessNewDBCFiles = False
        Else
            ProcessNewDBCFiles = True
            MsgBox("Please delete all files from the " & My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\DBC folder and place the new DBC files into this location...")

            x = -1
            For Each file In files
                If InStr(file.Name, ".DBC") > 0 Then
                    x = x + 1
                    ReDim Preserve DBCFileNames(x)
                    DBCFileNames(x) = file.DirectoryName & "\" & file.Name
                End If
            Next

            If Not DBCFileNames Is Nothing Then

                If ConnectToInca() = "True" Then

                    myincafolder = myActualDatabase.GetFolder("CSAV2\DV3\Serial Data")
                    For x = 0 To UBound(DBCFileNames)
                        myincafolder.ReadCanDBFile(DBCFileNames(x))
                    Next x

                End If

            End If

        End If

    End Sub

    Public Sub ProcessCANDBFiles()

        'Not used...

        'Reads in new CAN DB files if requested...

        Dim myincafolder As IncaFolder
        Dim DBCFileNames() As String = Nothing
        Dim x As Integer
        Dim A2lDirectoryName As String = My.Application.Info.DirectoryPath & "\A2L\CSAV2\" & g_SoftwareVersion & " MY " & g_ModelYear


        If Not Directory.Exists(A2lDirectoryName) Then
            Directory.CreateDirectory(A2lDirectoryName)
        End If

        MsgBox("Please place the proper CAN .dbc files for the sofware version and model year into the " & A2lDirectoryName & " directory before continuing.")

        Dim dir As DirectoryInfo = New DirectoryInfo(A2lDirectoryName)
        Dim files As FileInfo() = dir.GetFiles()

        If MsgBox("Process updated DBC Files?", vbYesNo) = vbNo Then
            ProcessNewDBCFiles = False
        Else

            ProcessNewDBCFiles = True
            'MsgBox("Please delete all files from the " & My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\DBC folder and place the new DBC files into this location...")

            x = -1
            For Each file In files
                If InStr(file.Name, ".DBC") > 0 Then
                    x = x + 1
                    ReDim Preserve DBCFileNames(x)
                    DBCFileNames(x) = file.DirectoryName & "\" & file.Name
                End If
            Next

            If Not DBCFileNames Is Nothing Then

                If ConnectToInca() = "True" Then

                    myincafolder = myActualDatabase.GetFolder("CSAV2\DV3\Serial Data")
                    For x = 0 To UBound(DBCFileNames)
                        myincafolder.ReadCanDBFile(DBCFileNames(x))
                    Next x

                End If

            End If

        End If

    End Sub

    Public Function DetermineSoftwareVersion(ByVal ReferenceString As String) As String

        'Called from VerifyCLEVIRConfiguration, SelectA2lAndPTPFiles
        'Determines software version number (i.e. 152, 153) based on ReferenceString passed in...
        'Slightly different behavior based on file type in ReferenceString...

        '144_8441_MY20_LC.xlsx
        '144_8441_MY21_LC.exp
        'ASE37_HCS_212114420CB_quasi.a2l
        'ASE34_LC_202014502_quasi.a2l
        'ASE34_LC_212114400_quasi.a2l
        'ASE37_212114420_20_FSI-019__HC_2P1C
        'ASE34_LC_212114400_K10906_L87_F48_UVZ_20190315__21_21_141R4_LC_1P1C

        'FCM_STA_ZF1_222215312.a2l - FCM standalone can come from either supplier...
        'FCM_STA_VEO_232315503.a2l
        'FCM_LCM_ZF1_222215306.a2l - FCM low and high can only come from ZF...
        'FCM_LCH_ZF1_222215306.a2l
        'FCM100_STA_ZF1_222215306.a2l - FCM100 standalone can come from either supplier...
        'FCM100_LCM_ZF1_232315403.a2l
        'FCM100_LCH_ZF1_232315403.a2l - FCM100 LCH can come from either supplier...
        'FCM100_STA_VEO_222215306.a2l
        'FCM100_LCM_VEO_232315403.a2l - FCM100 LCM can come from either supplier...
        'FCM100_LCH_VEO_232315403.a2l


        Dim ReturnValue As String

        ReturnValue = "???"

        'If the ReferenceString passed in contains the full path, we will use just the filename...
        If InStr(ReferenceString, "\") > 0 Then
            ReferenceString = System.IO.Path.GetFileName(ReferenceString)
        End If

        'Parsing the ReferenceString is different depending on file type...

        If InStr(ReferenceString, ".a2l") > 0 Then

            Select Case ProjectName
                Case "LowContent"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_LC") + 8, 3)
                Case "HighContent"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_HC") + 9, 3)

                    'ACP30M1232315610AA_ASTA_quasi_INCA.a2l
                Case "ACP2"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "ACP") + 11, 3)
                Case "ACP3"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "ACP") + 11, 3)
                Case "ACP4"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "ACP") + 11, 3)
                    'FCM CHANGE - Added FCM case here to handle determining software version based on FCM a2l file...
                Case "FCM", "FCM100"

                    'Select Case FCM_SubProjectName
                    'Case "FCM_1P"

                    Dim FCMtempstr() As String

                    FCMtempstr = Split(ReferenceString, "_")
                    ReturnValue = Mid(FCMtempstr(3), 5, 3)

                    '     Case "FCM_2P"
                    ' ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_LC") + 8, 3)
                    'Case "FCM_3P"
                    ' ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_HC") + 9, 3)
                    'End Select

            End Select

        ElseIf InStr(ReferenceString, ".xlsx") > 0 Or InStr(ReferenceString, ".csv") > 0 Or InStr(ReferenceString, ".exp") > 0 Then

            ReturnValue = Mid(ReferenceString, 1, 3)

        Else 'WorkspaceName is passed in....

            Select Case ProjectName
                Case "LowContent"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_LC") + 8, 3)
                Case "HighContent"
                    ReturnValue = Mid(ReferenceString, 11, 3)
                Case "ACP2"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "ACP") + 11, 3)
                Case "ACP3"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "ACP") + 11, 3)
                Case "ACP4"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "ACP") + 11, 3)

                    'FCM CHANGE - Added FCM case here to handle determining software version based on FCM workspace name...
                Case "FCM", "FCM100"

                    'Select Case FCM_SubProjectName
                    'Case "FCM_1P", "FCM100_1P"

                    Dim FCMtempstr() As String

                    FCMtempstr = Split(ReferenceString, "_")
                    ReturnValue = Mid(FCMtempstr(3), 5, 3)

                    '     Case "FCM_2P", "FCM100_2P"
                    ' ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_LC") + 8, 3)
                    'Case "FCM_3P", "FCM100_3P"
                    'ReturnValue = Mid(ReferenceString, 11, 3)
            'End Select

                Case "CSAV2"
                    ReturnValue = Mid(ReferenceString, 5, 3)
            End Select

        End If

        If Not IsNumeric(ReturnValue) Then
            ReturnValue = "???"
        Else
            If Val(ReturnValue) < 135 Then
                ReturnValue = "???"
            End If
        End If

        DetermineSoftwareVersion = ReturnValue

    End Function

    Public Sub RunNotepad(ByVal filename As String)
        Dim Notepadprocess As New Process()
        Notepadprocess.StartInfo = New ProcessStartInfo("notepad.exe", filename)
        Notepadprocess.Start()
    End Sub

    Public Sub CopyFlashInfoTofile()

        'Changed instances of NetworkDriveLetter to NetworkDriveMapping 02/14/2021

        Dim fnum As Integer
        Dim filename As String
        'Dim SaveProjectFilesNoPath(0 To 3) As String
        Dim SaveProjectFilesNoPath(0 To 5) As String
        Dim x As Integer
        Dim FoundVehicleNumber As Boolean = False
        Dim textline As String = ""
        Dim SaveTextLine() As String = Nothing

        Try

            HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Called...")

            'FCM CHANGE - Changed for loop to allow adding flashing info for up to three devices to accommodate FCM HIGH flavor which has three processors...
            'For x = 0 To 3
            For x = 0 To 5
                If InStr(SaveProjectFiles(x), "N/A") = 0 Then
                    SaveProjectFilesNoPath(x) = Path.GetFileName(SaveProjectFiles(x))
                Else
                    SaveProjectFilesNoPath(x) = SaveProjectFiles(x)
                End If
            Next

            'NETWORK DRIVE MAPPING

            If Not Directory.Exists(NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\VehicleFlashInfo") Then
                HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Creating Directory " & NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\VehicleFlashInfo")
                Directory.CreateDirectory((NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\VehicleFlashInfo"))
            End If

            filename = NetworkDriveMapping & "\CSAV2 Tools\CLEVIR\VehicleFlashInfo\VehicleFlashInfo.csv"

            'FCM CHANGE - Changed PrintLine to print up to three processors of information (or N/A) rather than a maximum of two...
            If Not File.Exists(filename) Then

                HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Creating File " & filename)
                fnum = FreeFile()
                FileOpen(fnum, filename, OpenMode.Append)
                PrintLine(fnum, "VehicleNumber,Date,Workspace/A2l File,PTP File,A2l File,PTP File,A2l File,PTP File")
                HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Adding Flash Info to " & filename)
                'PrintLine(fnum, VehicleNumber & "," & Format(Now, "MMddyyyy") & "," & SaveProjectFilesNoPath(0) & "," & SaveProjectFilesNoPath(1) & "," & SaveProjectFilesNoPath(2) & "," & SaveProjectFilesNoPath(3))
                PrintLine(fnum, VehicleNumber & "," & Format(Now, "MMddyyyy") & "," & SaveProjectFilesNoPath(0) & "," & SaveProjectFilesNoPath(1) & "," & SaveProjectFilesNoPath(2) & "," & SaveProjectFilesNoPath(3) & "," & SaveProjectFilesNoPath(4) & "," & SaveProjectFilesNoPath(5))
                FileClose(fnum)

            Else
                HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Adding Flash Info to " & filename)
                fnum = FreeFile()

                FileOpen(fnum, filename, OpenMode.Input)
                x = 0
                Do While Not EOF(fnum)
                    textline = LineInput(fnum)
                    If InStr(textline, VehicleNumber) > 0 Then
                        FoundVehicleNumber = True
                    End If
                    ReDim Preserve SaveTextLine(x)
                    SaveTextLine(x) = textline
                    x = x + 1
                Loop

                FileClose(fnum)
                fnum = FreeFile()

                If FoundVehicleNumber = False Then

                    FileOpen(fnum, filename, OpenMode.Append)
                    'PrintLine(fnum, VehicleNumber & "," & Format(Now, "MMddyyyy") & "," & SaveProjectFilesNoPath(0) & "," & SaveProjectFilesNoPath(1) & "," & SaveProjectFilesNoPath(2) & "," & SaveProjectFilesNoPath(3))
                    PrintLine(fnum, VehicleNumber & "," & Format(Now, "MMddyyyy") & "," & SaveProjectFilesNoPath(0) & "," & SaveProjectFilesNoPath(1) & "," & SaveProjectFilesNoPath(2) & "," & SaveProjectFilesNoPath(3) & "," & SaveProjectFilesNoPath(4) & "," & SaveProjectFilesNoPath(5))

                Else
                    FileOpen(fnum, filename, OpenMode.Output)
                    For x = 0 To UBound(SaveTextLine)
                        If InStr(SaveTextLine(x), VehicleNumber) > 0 Then
                            'PrintLine(fnum, VehicleNumber & "," & Format(Now, "MMddyyyy") & "," & SaveProjectFilesNoPath(0) & "," & SaveProjectFilesNoPath(1) & "," & SaveProjectFilesNoPath(2) & "," & SaveProjectFilesNoPath(3))
                            PrintLine(fnum, VehicleNumber & "," & Format(Now, "MMddyyyy") & "," & SaveProjectFilesNoPath(0) & "," & SaveProjectFilesNoPath(1) & "," & SaveProjectFilesNoPath(2) & "," & SaveProjectFilesNoPath(3) & "," & SaveProjectFilesNoPath(4) & "," & SaveProjectFilesNoPath(5))
                        Else
                            PrintLine(fnum, SaveTextLine(x))
                        End If
                    Next x
                End If

                FileClose(fnum)

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: " & ex.Message)
        End Try

    End Sub

    Public Function DetermineModelYear(ByVal ReferenceString As String) As String

        'Called from VerifyCLEVIRConfiguration, SelectA2lAndPTPFiles
        'Determines model year (i.e. MY21, MY22) based on ReferenceString passed in...
        'Slightly different behavior based on file type in ReferenceString...

        '144_8441_MY20_LC.xlsx
        '144_8441_MY21_LC.exp
        'ASE37_HCS_212114420CB_quasi.a2l
        'ASE34_LC_202014502_quasi.a2l
        'ASE34_LC_212114400_quasi.a2l
        'ASE37_212114420_20_FSI-019__HC_2P1C
        'ASE34_LC_212114400_K10906_L87_F48_UVZ_20190315__21_21_141R4_LC_1P1C

        Dim ReturnValue As String

        ReturnValue = "??"

        'If the ReferenceString passed in contains the full path, we will use just the filename...
        If InStr(ReferenceString, "\") > 0 Then
            ReferenceString = System.IO.Path.GetFileName(ReferenceString)
        End If

        'Parsing the ReferenceString is different depending on file type...

        If InStr(ReferenceString, ".a2l") > 0 Then

            Select Case ProjectName
                Case "LowContent"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_LC") + 6, 2)
                Case "HighContent"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_HC") + 7, 2)

                    'ACP30M1232315610AA_ASTA_quasi_INCA.a2l

                Case "ACP2"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "ACP") + 9, 2)
                Case "ACP3"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "ACP") + 9, 2)
                Case "ACP4"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "ACP") + 9, 2)

                    'FCM CHANGE - Added FCM Case here to determine model year for FCM device if necessary...
                Case "FCM", "FCM100"
                    'FCM_STA_ZF1_222215312.a2l - FCM standalone can come from either supplier...
                    'FCM_STA_VEO_232315503.a2l
                    'FCM_LCM_ZF1_222215306.a2l - FCM low and high can only come from ZF...
                    'FCM_LCH_ZF1_222215306.a2l
                    'FCM100_STA_ZF1_222215306.a2l - FCM100 standalone can come from either supplier...
                    'FCM100_LCM_ZF1_232315403.a2l
                    'FCM100_LCH_ZF1_232315403.a2l - FCM100 LCH can come from either supplier...
                    'FCM100_STA_VEO_222215306.a2l
                    'FCM100_LCM_VEO_232315403.a2l - FCM100 LCM can come from either supplier...
                    'FCM100_LCH_VEO_232315403.a2l


                    'Select Case FCM_SubProjectName
                    'Case "FCM_1P"

                    Dim FCMtempstr() As String

                    FCMtempstr = Split(ReferenceString, "_")
                    ReturnValue = Mid(FCMtempstr(3), 3, 2)

                    '     Case "FCM_2P"
                    'ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_LC") + 6, 2)
                    'Case "FCM_3P"
                    'ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_HC") + 7, 2)
                    'End Select
                    'modelyear returnvalue will be based on what controller type is instr
            End Select

        ElseIf InStr(ReferenceString, ".xlsx") > 0 Or InStr(ReferenceString, ".csv") > 0 Or InStr(ReferenceString, ".exp") > 0 Then

            If InStr(ReferenceString, "_MY") > 0 Then
                ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_MY") + 3, 2)
            End If

        Else

            Select Case ProjectName
                Case "LowContent"
                    ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_LC") + 6, 2)
                Case "HighContent"
                    ReturnValue = Mid(ReferenceString, 9, 2)


                    'ACP30M1232315610AA_ACP3_1P1C
                Case "ACP2"
                    ReturnValue = Mid(ReferenceString, 10, 2)
                Case "ACP3"
                    ReturnValue = Mid(ReferenceString, 10, 2)
                Case "ACP4"
                    ReturnValue = Mid(ReferenceString, 10, 2)
                    'FCM CHANGE - Added FCM Case here to determine model year for FCM device if necessary...
                Case "FCM", "FCM100"

                    'Select Case FCM_SubProjectName
                    'Case "FCM_1P", "FCM100_1P"

                    Dim FCMtempstr() As String

                    FCMtempstr = Split(ReferenceString, "_")
                    ReturnValue = Mid(FCMtempstr(3), 3, 2)

                    '     Case "FCM_2P", "FCM100_2P"
                    ' ReturnValue = Mid(ReferenceString, InStr(ReferenceString, "_LC") + 6, 2)
                    'Case "FCM_3P", "FCM100_3P"
                    ' ReturnValue = Mid(ReferenceString, 9, 2)
            'End Select

                Case "CSAV2"
                    ReturnValue = Mid(ReferenceString, 3, 2)
            End Select

        End If

        If Not IsNumeric(ReturnValue) Then
            ReturnValue = "??"
        Else
            If Val(ReturnValue) < 17 Then
                ReturnValue = "??"
            End If
        End If

        DetermineModelYear = ReturnValue

    End Function

    Public Sub DetermineInitINCAProjectDir(ByRef InitialDirectory As String)

        'Sets the initial inca project directory based on ProjectName...

        'If there is a properly configured flash drive connected, CLEVIR will go to this drive, defined by
        'NetworkDriveLetter, to look for a2l and ptp files rather than the network share drive...

        If UsingFlashDrive = True Then
            InitialDirectory = NetworkDriveLetter & "\INCA Projects"
        Else
            'If we are looking on network share drive, we determine default folder based on project name.
            'this takes user to a location from which they can drill down to find the proper model year and
            'software version folder which contains a2l and ptp files...

            If Len(ProjectName) > 0 Then 'And FlashingStatus.RadioButton3.Checked = False Then

                'NETWORK DRIVE MAPPING

                Select Case ProjectName
                        'HC CHANGE
                    Case "HighContent"
                        InitialDirectory = NetworkDriveMapping & "\EOCM3_HC\Calibration\INCA_Projects"
                        'FCM CHANGE - Added FCM case here to set up InitialDirectory for FCM projects...
                    Case "FCM", "FCM100"
                        InitialDirectory = NetworkDriveMapping & "\FCM\Calibration\INCA_Projects" 'Need to add this folder to share drive...
                    Case "LowContent"
                        InitialDirectory = NetworkDriveMapping & "\EOCM3_lo\Calibration\INCA_Projects"
                    Case "CSAV2"
                        InitialDirectory = NetworkDriveMapping & "\Calibration\INCA Projects"
                    Case "ACP2"
                        InitialDirectory = NetworkDriveMapping & "\ACP2\Calibration\INCA_Projects"
                    Case "ACP3"
                        InitialDirectory = NetworkDriveMapping & "\ACP3\Calibration\INCA_Projects"
                    Case "ACP4"
                        InitialDirectory = NetworkDriveMapping & "\ACP4\Calibration\INCA_Projects"
                    Case Else
                        InitialDirectory = My.Application.Info.DirectoryPath
                End Select

            Else
                InitialDirectory = My.Application.Info.DirectoryPath
            End If

        End If

    End Sub
    Public Function A2lAndCALToVehicleSpecificWorkspace(ByVal myFileDialog As FileDialog, ByVal myDialog As FolderBrowserDialog, ByVal mylistbox As ListBox) As Boolean

        'Called from HandleImportSoftwareAndCals (when the GO button on the FlashingStatus screen is pressed)

        'User selects a2l and ptp files.
        'Project is created in INCA (Placed in CLEVIR Setup\Projects folder) 
        'Vehicle specific template is copied and named the same name as the ptp file minus extension with the workspace template name tacked on the end.
        'Dataset (assumes only one) is copied into the newly created workspace.

        Dim myDestinationWorkspace As HardwareConfiguration = Nothing
        Dim mysourceproject As Asap2Project
        Dim mySourceDatasets() As DataSet
        Dim projectfiles(0 To 1) As String

        Dim ProcessorName As String = ""

        Dim ErrorText As String = ""

        Dim NumPasses As Integer = 1

        Dim ManualSelect As Boolean

        A2lAndCALToVehicleSpecificWorkspace = False

        'Determine number of passes requred for a2l and ptp file selection based on ProjectName...
        Select Case ProjectName
            Case "HighContent"
                NumPasses = 2
                If InStr(FCMConfigName, "FCM") > 0 Then
                    NumPasses = 3
                End If
            Case "LowContent"
                NumPasses = 1
                If InStr(FCMConfigName, "FCM") > 0 Then
                    NumPasses = 2
                End If
            Case "FCM", "FCM100"
                NumPasses = 1
            Case "ACP2"
                NumPasses = 1
            Case "ACP3"
                NumPasses = 1
                If InStr(FCMConfigName, "FCM") > 0 Then
                    NumPasses = 2
                End If
            Case "ACP4"
                NumPasses = 1
        End Select

        'Determine if we are using automatic lookup for a2l and Calibration files, or if we are using old manual selection method...
        If File.Exists(My.Application.Info.DirectoryPath & "\VehiclePTPLookup.csv") Then
            MsgBox("Please Select Software Version Folder for a2l and Calibration files...") ' on all three a2lptp not old

            If Len(InitialDirectory) = 0 Then
                DetermineInitINCAProjectDir(InitialDirectory)
            End If
            myDialog.SelectedPath = InitialDirectory
            myDialog.Description = "Please Select a Software Version Folder"

            If myDialog.ShowDialog() = DialogResult.Cancel Then
                ErrorText = "Invalid File Selection. Operation incomplete..."
            End If
            ManualSelect = False
        Else
            ManualSelect = True
        End If

        If Len(ErrorText) = 0 Then
            'Here we make multiple passes thru the selected software version folder depending on ProjectName
            '(LowContent 1 pass, HighContent, 2 passes, FCM either 1, 2, or 3 passes depending on STA (FCM_1P), LCM (FCM_2P) Or LCH (FCM_3P)
            'On each pass we will either automatically select the correct a2l and ptp files or the user will select
            'This is based on whether or not there is a VehiclePTPLookup.csv file available (there should always be one)...
            For x = 0 To (NumPasses - 1)

                Select Case x
                    Case 0

                        Select Case ProjectName
                            Case "HighContent"
                                ProcessorName = "HCS"
                            Case "LowContent"
                                ProcessorName = "LC"
                            Case "FCM", "FCM100"
                                ProcessorName = "FCM"
                            Case "ACP2"
                                ProcessorName = "ACP2"
                            Case "ACP3"
                                ProcessorName = "ACP3"
                            Case "ACP4"
                                ProcessorName = "ACP4"
                        End Select

                    Case 1

                        Select Case ProjectName
                            Case "HighContent"
                                ProcessorName = "HCF"
                            Case "LowContent"
                                ProcessorName = Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1)
                            Case "ACP3"
                                ProcessorName = Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1)
                        End Select

                    Case 2 'three passes would either be FCM or FCM100 for HighContent...
                        'ProcessorName = "HCF"
                        ProcessorName = Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1)
                End Select

                If ManualSelect = True Then
                    MsgBox("Please Select A2l and ptp File for " & ProcessorName & " processor")
                End If

                projectfiles = SelectA2lAndCALFiles(myFileDialog, myDialog, mylistbox, ProcessorName, ManualSelect)

                If Len(projectfiles(0)) > 0 Then
                    mylistbox.Items.Add("Using " & projectfiles(0))
                Else
                    'mylistbox.Items.Add("No a2l Selected for " & ProcessorName)
                    HandleUserMessageLogging("GMRC", "No a2l Selected for " & ProcessorName,,,, mylistbox)
                    ErrorText = "Invalid File Selection. Operation incomplete..."
                    Exit For
                End If

                If Len(projectfiles(1)) > 0 Then
                    mylistbox.Items.Add("Using " & projectfiles(1))
                Else
                    'mylistbox.Items.Add("No CAL File Selected for " & ProcessorName)
                    HandleUserMessageLogging("GMRC", "No CAL File Selected for " & ProcessorName,,,, mylistbox)
                    ErrorText = "Invalid File Selection. Operation incomplete..."
                    Exit For
                End If

                mylistbox.SelectedIndex = mylistbox.Items.Count - 1
                mylistbox.Refresh()

                If Len(projectfiles(0)) > 0 And Len(projectfiles(1)) > 0 Then
                    SaveProjectFiles(x * 2) = projectfiles(0)
                    SaveProjectFiles((x * 2) + 1) = projectfiles(1)
                Else
                    ErrorText = "Invalid File Selection. Operation incomplete..."
                    Exit For
                End If

            Next

        End If

        If Len(ErrorText) = 0 Then

            'FCM CHANGE

            Select Case ProjectName
                Case "HighContent"
                    NewWorkspaceName = DetermineNewWorkspaceName(SaveProjectFiles(1), SaveProjectFiles(3))
                Case "LowContent"
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
                Case "FCM", "FCM100"

                    'NewWorkspaceName is always based on either LC namd or HCS name for FCM LCM and FCM LCH...
                    'Select Case FCM_SubProjectName
                    'Case "FCM_1P", "FCM100_1P"
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
                    '    Case "FCM_2P", "FCM100_2P"
                    'NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(3)), 1, Len(Path.GetFileName(SaveProjectFiles(3))) - 4) & "_" & WorkspaceNameSuffix
                    'Case "FCM_3P", "FCM100_3P"
                    'NewWorkspaceName = DetermineNewWorkspaceName(SaveProjectFiles(3), SaveProjectFiles(5))
                    'End Select

                Case "ACP2"
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
                Case "ACP3"
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
                Case "ACP4"
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
            End Select

            If ConnectToInca() = "True" Then

                Dim myFolder As Folder
                Dim MyDatabaseItems() As DataBaseItem

                myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")
                MyDatabaseItems = myFolder.BrowseDataBaseItem(NewWorkspaceName)

                If MyDatabaseItems.Length > 0 Then
                    If MsgBox(NewWorkspaceName & " already exists, recreate?", vbYesNo) = vbNo Then
                        A2lAndCALToVehicleSpecificWorkspace = True
                        Exit Function
                    End If
                End If

            Else
                ErrorText = "Could not connect to INCA. Operation incomplete..."
            End If

        End If

        If Len(ErrorText) = 0 Then

            For x = 0 To (NumPasses - 1)

                '0 and 1 or 2 and 3 or 4 and 5 - x = 0 or 1 or 2

                mysourceproject = CreateA2LPTPProject(SaveProjectFiles(x * 2), SaveProjectFiles((x * 2) + 1), mylistbox)
                If Not mysourceproject Is Nothing Then

                    If x = 0 Then
                        myDestinationWorkspace = CopyWorkspaceTemplateNEW(NewWorkspaceName, mylistbox)
                    End If

                    If Not myDestinationWorkspace Is Nothing Then

                        mySourceDatasets = mysourceproject.AllDataSets

                        If mySourceDatasets.Length > 0 Then

                            If AddSelectedProjectToWorkspace(mySourceDatasets(0), mysourceproject, myDestinationWorkspace, NewWorkspaceName, mylistbox) = True Then

                                If x = NumPasses - 1 Then
                                    A2lAndCALToVehicleSpecificWorkspace = True

                                    HandleUserMessageLogging("GMRC", "Project/Dataset Copy Complete.",,, FLASH_MSG_ON, mylistbox)
                                    'UserStatusInfo.Label1.Text = "Project/Dataset Copy Complete."
                                    'mylistbox.Items.Add("Project/Dataset Copy Complete.")
                                    'mylistbox.SelectedIndex = mylistbox.Items.Count - 1
                                    'mylistbox.Refresh()
                                    UserStatusInfo.Hide()
                                End If

                            Else
                                ErrorText = "AddSelectedProjectToWorkspace returned false..."
                                Exit For
                            End If

                        Else
                            ErrorText = "Could not find dataset. Operation incomplete..."
                            Exit For
                        End If

                    Else
                        ErrorText = "Could not find template workspace. Operation incomplete..."
                        Exit For
                    End If

                Else
                    ErrorText = "Could not create INCA Project. Operation incomplete..."
                    Exit For
                End If

            Next

        End If

        If Len(ErrorText) > 0 Then

            HandleUserMessageLogging("GMRC", ErrorText, DISPLAY_MSG_BOX, SEND_LIVE_UPDATE, FLASH_MSG_1_SEC, mylistbox)

            'UpdateVehicleStatus("Flashomatic " & ErrorText)
            'UserStatusInfo.Label1.Text = ErrorText
            'mylistbox.Items.Add(ErrorText)
            'mylistbox.SelectedIndex = mylistbox.Items.Count - 1
            'mylistbox.Refresh()
            'System.Threading.Thread.Sleep(500)
            'UserStatusInfo.Hide()
            'MsgBox(ErrorText)

        End If

    End Function

    Public Sub ReadARXMLMappingFileNEW(ByVal ReferenceName As String)

        'reads the ARXML mapping file (HC or LC) to determine which vspy configuration to use based on name of a2l file...

        Dim Filename As String = ""
        Dim fnum As Integer
        Dim textline As String
        Dim lineitems() As String

        Dim StartNum As Integer
        Dim TextLen As Integer = 7

        Dim SaveWorkspaceTemplateName As String = ""
        Dim SaveVSpySelectedConfigFileName As String = ""

        Dim Found As Boolean

        Select Case ProjectName

            Case "LowContent"
                Filename = My.Application.Info.DirectoryPath & "\LC_ARXML_Mapping.csv"
                StartNum = 10
            Case "HighContent"
                Filename = My.Application.Info.DirectoryPath & "\HC_ARXML_Mapping.csv"
                StartNum = 11

            'FCM CHANGE - Added FCM Case here

            Case "FCM", "FCM100"

                Filename = My.Application.Info.DirectoryPath & "\FCM_ARXML_Mapping.csv"
                'FCM CHANGE - need to differentiate here based on actual FCM .a2l file name, there will be many different flavors
                'rather than just a single one as with LC and HC - So we cant just look at version using startnum, we will set this
                'below in the do while loop so we can determine where the startnum should be based on a2l file format...

                'StartNum = 11

        End Select

        If File.Exists(Filename) = True Then

            fnum = FreeFile()

            FileOpen(fnum, Filename, OpenMode.Input)

            Do While Not EOF(fnum)
                textline = LineInput(fnum)
                lineitems = Split(textline, ",")
                'FCM CHANGE  - Added logic below to determine startnum for parsing software version from FCM a2l filename...
                If InStr(ProjectName, "FCM") > 0 Then
                    If IsNumeric(Mid(lineitems(0), 13, 1)) = True Then
                        StartNum = 13
                    Else
                        StartNum = 16
                    End If
                End If

                If InStr(ReferenceName, Mid(lineitems(0), StartNum, TextLen)) > 0 Then

                    VSpySelectedConfigFileName = lineitems(4)

                    Found = True
                    Exit Do

                Else 'if the a2l filename does not match, we will save the information here.  That way, if the a2l filename is not found,
                    'we will use the information from the last line in the file (the most recent information).

                    SaveVSpySelectedConfigFileName = lineitems(4)

                End If
            Loop

            If Found = False Then
                VSpySelectedConfigFileName = SaveVSpySelectedConfigFileName
            End If

        End If

        FileClose(fnum)

    End Sub

    Public Function ReadInVehiclePTPLookupFile() As String()

        'Called from FindPTPFileInDirectory...

        'Reads the VehiclePTPLookup.csv file and passes back a string array which is used to determine the search strings to use to identify the proper ptp file
        'to use for a particular vehicle when building a new vehicle specific workspace...

        Dim fnum As Integer
        Dim filename As String
        Dim textline As String
        Dim lineitems() As String = Nothing
        Dim returnarray() As String = Nothing

        filename = My.Application.Info.DirectoryPath & "\VehiclePTPLookup.csv"

        If File.Exists(filename) Then

            fnum = FreeFile()

            FileOpen(fnum, filename, OpenMode.Input)

            textline = LineInput(fnum)
            'lineitems = Split(textline, ",")

            Do While Not EOF(fnum)

                textline = LineInput(fnum)
                lineitems = Split(textline, ",")

                If UCase(lineitems(0)) = UCase(VehicleNumber) Then
                    returnarray = lineitems
                    Exit Do
                End If

            Loop

            FileClose(fnum)

            If returnarray Is Nothing Then
                ReDim returnarray(0)
                returnarray(0) = "Invalid"
            End If

        Else
            ReDim returnarray(0)
            returnarray(0) = "Invalid"
        End If

        ReadInVehiclePTPLookupFile = returnarray


    End Function

    Public Function FindPTPFileInDirectory(ByVal mydialog As FileDialog, ByVal SelectedDirectory As String, ByVal mylistbox As ListBox, ByVal ProcessorName As String, ByVal CALFileExtension As String) As String

        'Finds appropriate ptp file in user selected folder based on vehicle number.  Uses lookup file VehiclePTPLookup.csv which maps vehicle number
        'to ptp file based on variant and cal differentators contained in file  If file is found, it is copied to CLEVIR install folder...

        Dim dir As DirectoryInfo = New DirectoryInfo(SelectedDirectory)
        Dim files As FileInfo() = dir.GetFiles()
        Static lineitems() As String = Nothing
        Static templineitems() As String = Nothing

        Dim NumPasses As Integer = 0
        Dim NumMatches As Integer = 0
        Dim SaveFileName As String = ""

        Dim SourceFile As String = ""
        Dim DestFile As String = ""

        'Dim CALFileExtension As String = ""

        Dim x As Integer
        Dim y As Integer = 0

        'Removed due to redundancy, we do this in the calling function and pass in the CALFileExtension now...

        'Select Case ProcessorName
        'Case "HCS", "HCF", "LC"
        'CALFileExtension = "ptp"
        'Case "FCM"
        'If InStr(A2l_FileName, "ZF1") > 0 Then
        'CALFileExtension = "s19"
        'Else 'VEO
        'CALFileExtension = "s37"
        'End If
        'End Select

        FindPTPFileInDirectory = ""

        If templineitems Is Nothing Then
            templineitems = ReadInVehiclePTPLookupFile() 'gets line items associated with vehicle number, only need to do this once...
        End If

        If Not templineitems Is Nothing Then

            If templineitems(0) <> "Invalid" Then

                If lineitems Is Nothing Then
                    'Look through each item in templineitems and if item has something in it, put its contents into lineitems,
                    'this so we have an array populated only with data and not blanks...
                    For x = 1 To UBound(templineitems)
                        If Len(templineitems(x)) > 0 Then
                            y = y + 1
                            ReDim Preserve lineitems(y)

                            'if the lookup file is opened in EXCEL, a - sign screws up the data in the cell, so we use the word Minus to indicate "-" character
                            'we need to change this back to a "-" so that it comprehends the actual ptp file naming which uses a "-"...
                            If InStr(UCase(templineitems(x)), "MINUS") > 0 Then
                                templineitems(x) = "-" & Mid(templineitems(x), 6, Len(templineitems(x)))
                            End If
                            lineitems(y) = templineitems(x)
                        End If
                    Next

                End If

                'FCM CHANGE - Changed to check ProcessorType always, we now always pass ProcessorType, not ProcessorType = ""

                For NumPasses = 1 To UBound(lineitems)
                    For Each file In files
                        'old way before FCM...
                        'if processor type passed in is "", this is LC or FCM, we dont have to differentiate between HCS and HCF as with HC...
                        'If Len(ProcessorType) = 0 Then

                        'New way to accommodate FCM...
                        'processortype = LC and no FCM_SubProjectName defined means LC only, so just one set of cal files...
                        'processortype = FCM and FCM_SubProjectName = FCM_P1 means FCM only, so just one set of cal files...

                        'If (ProcessorName = "LC" And Len(FCM_SubProjectName) = 0) Or (ProcessorName = "FCM" And FCM_SubProjectName = "FCM_1P") Or (ProcessorName = "FCM" And FCM_SubProjectName = "FCM100_1P") Then
                        If (ProcessorName = "LC") Or (ProcessorName = "FCM") Or (ProcessorName = "FCM100") Or (ProcessorName = "ACP2") Or (ProcessorName = "ACP3") Or (ProcessorName = "ACP4") Then
                            y = 0
                            'We assume here that we must find a cal file that contains every one of the lineitems defined in lookup file...
                            For x = 1 To UBound(lineitems)
                                If InStr(file.Name, lineitems(x)) = 0 Then
                                    Exit For
                                Else
                                    y = y + 1
                                End If
                            Next x
                            If y = UBound(lineitems) Then
                                NumMatches = NumMatches + 1
                                SaveFileName = SelectedDirectory & "\" & file.Name
                            End If

                        Else 'If HighContent, we know that we have to look only for cal files of the proper processor type...

                            'Because there will be many cal files in the folder, and some of the differentiators will be the same between
                            'the various cal files, we must find only one that matches all defined line items.  If we dont find any, or if
                            'we find more than one, the user must manually select.  

                            'We have To make multiple passes through the files, each time addding one more differentiator, because in some
                            'cases, there may be less differentiators used for one processor than the other. If this was not the case, we
                            'could simply look for files that contained all differentiators...
                            Select Case NumPasses
                                Case 1
                                    If InStr(file.Name, ProcessorName) > 0 And InStr(file.Name, lineitems(1)) > 0 Then
                                        NumMatches = NumMatches + 1
                                        SaveFileName = SelectedDirectory & "\" & file.Name
                                    End If
                                Case 2
                                    If InStr(file.Name, ProcessorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 Then 'And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 Then
                                        NumMatches = NumMatches + 1
                                        SaveFileName = SelectedDirectory & "\" & file.Name
                                    End If
                                Case 3
                                    If InStr(file.Name, ProcessorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 Then 'And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 Then
                                        NumMatches = NumMatches + 1
                                        SaveFileName = SelectedDirectory & "\" & file.Name
                                    End If
                                Case 4
                                    If InStr(file.Name, ProcessorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 Then 'And InStr(file.Name, lineitems(5)) > 0 Then
                                        NumMatches = NumMatches + 1
                                        SaveFileName = SelectedDirectory & "\" & file.Name
                                    End If
                                Case 5
                                    If InStr(file.Name, ProcessorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 Then
                                        NumMatches = NumMatches + 1
                                        SaveFileName = SelectedDirectory & "\" & file.Name
                                    End If
                                Case 6
                                    If InStr(file.Name, ProcessorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 And InStr(file.Name, lineitems(6)) > 0 Then
                                        NumMatches = NumMatches + 1
                                        SaveFileName = SelectedDirectory & "\" & file.Name
                                    End If
                                Case 7
                                    If InStr(file.Name, ProcessorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 And InStr(file.Name, lineitems(6)) > 0 And InStr(file.Name, lineitems(7)) > 0 Then
                                        NumMatches = NumMatches + 1
                                        SaveFileName = SelectedDirectory & "\" & file.Name
                                    End If
                            End Select

                        End If
                    Next file

                    'If we dont find any matches, Or if we find more than one match, the user must manually select.  

                    If NumMatches > 1 Then
                        SaveFileName = ""
                        NumMatches = 0
                    ElseIf NumMatches = 0 Then
                        SaveFileName = ""
                        Exit For
                    ElseIf NumMatches = 1 Then
                        Exit For
                    End If

                Next NumPasses

                If Len(SaveFileName) = 0 Then 'no matches found, or too many matches found...

                    HandleUserMessageLogging("GMRC", "Auto " & CALFileExtension & " Select Failed (number of matches)... Please select " & ProcessorName & " " & CALFileExtension & " file. " & VehicleNumber, DISPLAY_MSG_BOX, SEND_LIVE_UPDATE)
                    'HandleUserMessageLogging("GMRC", "number of matches")

                    'MsgBox("Auto " & CALFileExtension & " Select Failed... Please select " & ProcessorName & " " & CALFileExtension & " file.")
                    'CopyToLog("Auto " & CALFileExtension & " Select Failed on number of matches for " & ProcessorName & " " & VehicleNumber)
                    'UpdateVehicleStatus("Auto " & CALFileExtension & " Select Failed on number of matches for " & ProcessorName & " " & VehicleNumber)
                    'SaveFileName = SelectFile(mydialog, SelectedDirectory, "ptp", True)
                    SaveFileName = SelectFile(mydialog, SelectedDirectory, CALFileExtension, True)
                End If

            Else 'vehicle number not found in lookup file...
                'MsgBox("Auto PTP Select Failed... Please select " & ProcessorType & " .ptp file.")
                'SaveFileName = SelectFile(mydialog, SelectedDirectory, "ptp", True)

                HandleUserMessageLogging("GMRC", "Auto " & CALFileExtension & " Select Failed (Vehicle Number not found)... Please select " & ProcessorName & " " & CALFileExtension & " file. " & VehicleNumber, DISPLAY_MSG_BOX, SEND_LIVE_UPDATE)

                'MsgBox("Auto " & CALFileExtension & " Select Failed... Please select " & ProcessorName & " " & CALFileExtension & " file.")
                'CopyToLog("Auto " & CALFileExtension & " Select Failed vehicle number not found for " & ProcessorName & " " & VehicleNumber)
                'UpdateVehicleStatus("Auto " & CALFileExtension & " Select Failed vehicle number not found for " & ProcessorName & " " & VehicleNumber)
                SaveFileName = SelectFile(mydialog, SelectedDirectory, CALFileExtension, True)
            End If

        Else 'Lookup file not found.  This should never happen, we actually check for existance of file before this function is called...

            HandleUserMessageLogging("GMRC", "Auto " & CALFileExtension & " Select Failed... Please select " & ProcessorName & " " & CALFileExtension & " file.", DISPLAY_MSG_BOX)
            'MsgBox("Auto " & CALFileExtension & " Select Failed... Please select " & ProcessorName & " " & CALFileExtension & " file.")
            SaveFileName = SelectFile(mydialog, SelectedDirectory, CALFileExtension, True)
        End If

        If Len(SaveFileName) > 0 Then

            DestFile = My.Application.Info.DirectoryPath & "\" & Mid(SaveFileName, InStrRev(SaveFileName, "\") + 1, Len(SaveFileName))

            If Not System.IO.File.Exists(DestFile) Then

                HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(SaveFileName) & ", Please wait...",,, FLASH_MSG_ON)
                'UserStatusInfo.Label1.Text = "Copying " & Path.GetFileName(SaveFileName) & ", Please wait..."

                RoboCopyFile(SaveFileName, My.Application.Info.DirectoryPath)
                If Not File.Exists(DestFile) Then
                    UserStatusInfo.Hide()
                    FindPTPFileInDirectory = ""
                    Exit Function
                End If

            End If

            GoTo bypassfilecheck

            If SaveFileName <> DestFile Then
                If MsgBox("File already exists.  Do you wish to replace this file?", vbYesNo) = vbYes Then

                    HandleUserMessageLogging("GMRC", "File " & DestFile & " found.  Copying over existing file.",,, FLASH_MSG_ON, mylistbox)
                    'UserStatusInfo.Label1.Text = "File " & DestFile & " found.  Copying over existing file."
                    'mylistbox.Items.Add("File " & DestFile & " found.  Copying over existing file.")

                    If Debugger.IsAttached = False Then

                        HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(SaveFileName) & ", Please wait...",,, FLASH_MSG_ON)
                        'UserStatusInfo.Label1.Text = "Copying " & Path.GetFileName(SaveFileName) & ", Please wait..."

                        'System.IO.File.Copy(SourceFile, DestFile)

                        RoboCopyFile(SaveFileName, My.Application.Info.DirectoryPath)
                        If Not File.Exists(DestFile) Then
                            FindPTPFileInDirectory = ""
                            UserStatusInfo.Hide()
                            Exit Function
                        End If

                    End If

                Else
                    HandleUserMessageLogging("GMRC", "File " & DestFile & " found.  Using existing file.",,, FLASH_MSG_ON, mylistbox)
                    'UserStatusInfo.Label1.Text = "File " & DestFile & " found.  Using existing file."
                    'mylistbox.Items.Add("File " & DestFile & " found.  Using existing file.")
                End If
            End If

bypassfilecheck:

            FindPTPFileInDirectory = DestFile

        End If

        UserStatusInfo.Hide()

    End Function

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

    Public Function FindA2lFileInDirectory(ByVal mydialog As FolderBrowserDialog, ByVal mylistbox As ListBox, ByVal ProcessorName As String) As String

        'Called from SelectA2lAndPTPFiles...

        'Finds a2l file in user selected folder and copies to CLEVIR install folder.  Assumes only one, if ProcessorName is "LC", or will find
        'a2l file that corresponds to ProcessorType (HCS or HCF) that is passed in.

        Dim dir As DirectoryInfo
        Dim SourceFile As String = ""
        Dim DestFile As String = ""

        Dim NumberOfA2lFiles As Integer


        FindA2lFileInDirectory = ""

        If Len(mydialog.SelectedPath) > 0 Then
            dir = New DirectoryInfo(mydialog.SelectedPath)
            Dim files As FileInfo() = dir.GetFiles()


            For Each file In files
                'FCM CHANGE - Changed check for len(ProcessorType) = 0 to check specifically for "LC" ProcessorType...
                'This because with FCM now, we need to be able to differentiate between LC a2l files and LCM or LCH a2l files...
                'This is done here by the fact that LC uses quasi.a2l and LCM and LCH use .a2l files with no quasi...
                If ProcessorName = "LC" Then
                    If InStr(file.Name, "quasi.a2l") > 0 And InStr(file.Name, "LC") > 0 Then
                        SourceFile = mydialog.SelectedPath & "\" & file.Name
                        NumberOfA2lFiles = NumberOfA2lFiles + 1
                    End If
                Else
                    'May need to make some FCM related changes here, need to figure out processortype...
                    If InStr(file.Name, ".a2l") > 0 And InStr(file.Name, ProcessorName) > 0 Then
                        SourceFile = mydialog.SelectedPath & "\" & file.Name
                        NumberOfA2lFiles = NumberOfA2lFiles + 1
                    End If
                End If

            Next file

            If NumberOfA2lFiles <> 1 Then
                HandleUserMessageLogging("GMRC", "Could not find a2l file for " & ProcessorName & ". Please make sure that there is one valid .a2l file per processor in " & mydialog.SelectedPath & " and retry this operation...", DISPLAY_MSG_BOX)
                Exit Function
            End If

            If Len(SourceFile) > 0 Then

                DestFile = My.Application.Info.DirectoryPath & "\" & Mid(SourceFile, InStrRev(SourceFile, "\") + 1, Len(SourceFile))

                'The Save A2LFilename variables below are used only in conjunction with the administrator task of creating new CLEVIR support files
                'for a new major software version.  These would only be used if the Configure for New Software Verion (Checkbox1) was checked
                'on the FlashingStatus form, which is only visible if running with debugger.isattached = true...

                Select Case ProcessorName
                    Case "HCF"
                        Save_HCF_A2LFilename = DestFile
                    Case "HCS"
                        Save_HCS_A2LFilename = DestFile
                        'Case Else
                        '    Save_LC_A2LFilename = DestFile
                    Case "LC"
                        Save_LC_A2LFilename = DestFile
                        'FCM CHANGE - Added FCM Case here...
                    Case "FCM"
                        Save_FCM_A2LFilename = DestFile
                    Case "ACP2"
                        Save_ACP2_A2LFilename = DestFile
                    Case "ACP3"
                        Save_ACP3_A2LFilename = DestFile
                    Case "ACP4"
                        Save_ACP4_A2LFilename = DestFile
                End Select

                If Not System.IO.File.Exists(DestFile) Then

                    HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(SourceFile) & ", Please wait...",,, FLASH_MSG_ON)
                    'UserStatusInfo.Label1.Text = "Copying " & Path.GetFileName(SourceFile) & ", Please wait..."

                    RoboCopyFile(SourceFile, My.Application.Info.DirectoryPath)
                    If Not File.Exists(DestFile) Then
                        FindA2lFileInDirectory = ""
                        UserStatusInfo.Hide()
                        Exit Function
                    End If

                End If

                GoTo bypassfilecheck

                If SourceFile <> DestFile Then
                    If MsgBox("File already exists.  Do you wish to replace this file?", vbYesNo) = vbYes Then

                        HandleUserMessageLogging("GMRC", "File " & DestFile & " found.  Copying over existing file.",,, FLASH_MSG_ON, mylistbox)
                        'UserStatusInfo.Label1.Text = "File " & DestFile & " found.  Copying over existing file."
                        'mylistbox.Items.Add("File " & DestFile & " found.  Copying over existing file.")

                        If Debugger.IsAttached = False Then

                            HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(SourceFile) & ", Please wait...",,, FLASH_MSG_ON)
                            'UserStatusInfo.Label1.Text = "Copying " & Path.GetFileName(SourceFile) & ", Please wait..."

                            RoboCopyFile(SourceFile, My.Application.Info.DirectoryPath)
                            If Not File.Exists(DestFile) Then
                                FindA2lFileInDirectory = ""
                                UserStatusInfo.Hide()
                                Exit Function
                            End If

                        End If

                    Else
                        HandleUserMessageLogging("GMRC", "File " & DestFile & " found.  Using existing file.",,, FLASH_MSG_ON, mylistbox)
                        'UserStatusInfo.Label1.Text = "File " & DestFile & " found.  Using existing file."
                        'mylistbox.Items.Add("File " & DestFile & " found.  Using existing file.")
                    End If
                End If
bypassfilecheck:

                FindA2lFileInDirectory = DestFile

            End If

        End If

        UserStatusInfo.Hide()

    End Function

    Public Function SelectA2lAndCALFiles(ByVal myFileDialog As FileDialog, ByVal mydialog As FolderBrowserDialog, ByVal mylistbox As ListBox, ByVal ProcessorName As String, ByVal ManualSelect As Boolean) As String()

        'Called from A2lAndCALToVehicleSpecificWorkspace
        'Allows user to select a2l and CAL file (.ptp, .s19 or .s37)...

        Dim CALFileExtension As String = ""
        Dim ProjectFileNames(0 To 1) As String
        Dim SetINCAWorkspaceTemplateName As Boolean

        'INCAWorkspaceTemplateName = g_SaveINCAWorkspaceTemplateName
        'WorkspaceNameSuffix = g_SaveWorkspaceNameSuffix

        If ManualSelect = True Then
            ProjectFileNames(0) = SelectFileByType(myFileDialog, "a2l", mylistbox)
        Else
            'Here we retrieve the filename of the a2l file based on ProcessorType passed in...
            ProjectFileNames(0) = FindA2lFileInDirectory(mydialog, mylistbox, ProcessorName)
        End If

        If Len(ProjectFileNames(0)) > 0 Then

            'Many files used are model year and software version specific, so we need to know which model year and software version
            'we are using.  This is based on the name of the a2l file...

            A2l_FileName = System.IO.Path.GetFileName(ProjectFileNames(0))

            'INCAWorkspaceTemplateName is built from the model year and software version obtained from the .a2l filename.
            'Which .a2l file to use to obtain the model year and software version depends on the ProjectName 

            Select Case ProjectName
                Case "HighContent"
                    If InStr(ProjectFileNames(0), "HCS") > 0 Then
                        SetINCAWorkspaceTemplateName = True
                    End If
                Case "LowContent"
                    If InStr(ProjectFileNames(0), "LC") > 0 Then
                        SetINCAWorkspaceTemplateName = True
                    End If
                Case "FCM"
                    If InStr(ProjectFileNames(0), "FCM") > 0 Then
                        SetINCAWorkspaceTemplateName = True
                    End If
                Case "ACP2"
                    If InStr(ProjectFileNames(0), "ACP2") > 0 Then
                        SetINCAWorkspaceTemplateName = True
                    End If
                Case "ACP3"
                    If InStr(ProjectFileNames(0), "ACP3") > 0 Then
                        SetINCAWorkspaceTemplateName = True
                    End If
                Case "ACP4"
                    If InStr(ProjectFileNames(0), "ACP4") > 0 Then
                        SetINCAWorkspaceTemplateName = True
                    End If
            End Select

            If SetINCAWorkspaceTemplateName = True Then

                g_ModelYear = DetermineModelYear(ProjectFileNames(0))
                g_SoftwareVersion = DetermineSoftwareVersion(ProjectFileNames(0))

                INCAWorkspaceTemplateName = g_SaveINCAWorkspaceTemplateName
                WorkspaceNameSuffix = g_SaveWorkspaceNameSuffix

                If InStr(INCAWorkspaceTemplateName, "_MY") = 0 Then
                    INCAWorkspaceTemplateName = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & INCAWorkspaceTemplateName
                    WorkspaceNameSuffix = g_SoftwareVersion & "_MY" & g_ModelYear & "_" & WorkspaceNameSuffix
                End If

            End If

            Select Case ProcessorName
                Case "HCS", "HCF", "LC", "ACP2", "ACP3", "ACP4"
                    CALFileExtension = "ptp"
                    'FCM CHANGE ??? - Added FCM case here.  Need to be able to set the CALFileExtension based on ProcessorName rather than just using .ptp
                    'due to the fact that FCM supliers use .s19 or .s37 files? not sure if this is the case yet about .s37 files..., not ptp files...

                    'This may need to change, VEO may be using s19 files also and not s37...
                Case "FCM"
                    'If InStr(A2l_FileName, "ZF1") > 0 Then
                    CALFileExtension = "s19"
                    'Else
                    'CALFileExtension = "s37"
                    'End If
                Case ""
                    CALFileExtension = "ptp"
            End Select

            If ManualSelect = True Then
                ProjectFileNames(1) = SelectFileByType(myFileDialog, CALFileExtension, mylistbox)
            Else
                'Here we look for the ptp file that corresponds to the vehiclenumber and processorname...
                ProjectFileNames(1) = FindPTPFileInDirectory(myFileDialog, mydialog.SelectedPath, mylistbox, ProcessorName, CALFileExtension)
            End If

        End If

        SelectA2lAndCALFiles = ProjectFileNames

    End Function


    Public Function Get_Workspace(ByVal workspaceName As String, Optional ByVal path As String = "") As HardwareConfiguration

        'Return workspaceName hardwareconfiguration object based on workspacename and INCA database path provided...

        Dim MyDatabaseItems() As DataBaseItem

        Get_Workspace = Nothing

        If myActualDatabase Is Nothing Then
            myActualDatabase = myinca.GetCurrentDataBase
        End If

        If Len(path) > 0 Then

            Get_Workspace = myActualDatabase.GetItemInFolder(workspaceName, path) '(CLEVIR Setup\Workspaces)

        Else
            MyDatabaseItems = myActualDatabase.BrowseItem(workspaceName)

            If Not MyDatabaseItems Is Nothing Then

                If MyDatabaseItems.Length <> 0 Then
                    Get_Workspace = MyDatabaseItems(0)
                End If

            End If

        End If

    End Function

    Public Function MapDrive(ByVal DriveLetter As String, ByVal UNCPath As String) As Boolean

        'Called when user clicks on the Update Data top menu bar selection.  Maps a network drive
        'to the name specified in the config.txt file, this is where the recorded data from the vehicle
        'well be copied.

        Dim nr As NETRESOURCE
        Dim Username As String
        Dim Password As String

        nr = New NETRESOURCE
        nr.lpRemoteName = UNCPath
        nr.lpLocalName = DriveLetter
        Username = Nothing '(add parameters to pass this if necessary)
        Password = Nothing '(add parameters to pass this if necessary)
        nr.dwType = RESOURCETYPE_DISK

        Dim result As Integer

        HandleUserMessageLogging("GMRC", "Mapping Drive - " & DriveLetter & ": " & UNCPath)

        result = WNetAddConnection2(nr, Password, Username, 0)

        'NETWORK DRIVE MAPPING

        If result = 0 Or result = 85 Then
            If System.IO.Directory.Exists(NetworkDriveLetter & "\CSAV2 Tools\CLEVIR\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\UpdatedFiles") Then
                HandleUserMessageLogging("GMRC", "Drive Mapping for Data Upload succeeded.")
                Return True
            Else
                HandleUserMessageLogging("GMRC", "Drive Mapping for Data Upload Failed - Directory not found.")
                Return False
            End If
        Else
            HandleUserMessageLogging("GMRC", "Map Network Drive Failed, Return Value = " & result)
            Return False
        End If
    End Function

    Public Function UnMapDrive(ByVal DriveLetter As String) As Boolean

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

        'This is called out of AddCustomINCASetup, creates a new directory and copyies the contents of
        'the source directory into the new directory.  This subroutine is "borrowed" from the Internet....

        ' Get the subdirectories for the specified directory. 
        Dim dir As DirectoryInfo = New DirectoryInfo(sourceDirName)
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

    Public Function ConvertToNewLineFormat(ByVal lineitems As String()) As String

        'Called from ReadVehicleConfigsFile.  This was incorporated a long time ago after the file format of the vehicleconfigurations.csv file
        'had changed.  This allowed us to use either the older file format (and convert it to the new format) or to use the new file format.
        'So, by now, this should never need to be called because all files should be of the new file format...

        Dim NewLineItem As String = ""
        Dim x As Integer

        Static BeenThru As Boolean

        'Vehicle Number	Vehicle ID	Proc 1	Proc 2	Proc 3	Proc 4	Proc 5	Proc 6	Camera 1	Camera 2	Camera 3	Camera 4	Camera 5	Camera 6	CAN Mon 1	CAN Mon 2	CAN Mon 3	Data Upload Path	CLEVIR Files Path	INCA Workspace Path	Zip MF4 Files	Config Name	ConfigNum

        For x = 0 To 16
            NewLineItem = NewLineItem & lineitems(x) & ","
        Next x

        If BeenThru = False Then
            NewLineItem = NewLineItem & "CAN Mon 4,"
            BeenThru = True
        Else
            NewLineItem = NewLineItem & "NA,"
        End If

        NewLineItem = NewLineItem & lineitems(17) & ","
        NewLineItem = NewLineItem & lineitems(18) & ","
        NewLineItem = NewLineItem & lineitems(20) & ","
        NewLineItem = NewLineItem & lineitems(21)

        ConvertToNewLineFormat = NewLineItem

    End Function
    Public Sub AcquireShutdownPrivilege()

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
            Dim luaAttr As New LUID_AND_ATTRIBUTES
            luaAttr.Luid = luid_Shutdown
            luaAttr.Attributes = SE_PRIVILEGE_ENABLED

            'Set up a TOKEN_PRIVILEGES structure containing only the shutdown privilege.
            Dim newState As New TOKEN_PRIVILEGES
            newState.PrivilegeCount = 1
            newState.Privileges = New LUID_AND_ATTRIBUTES() {luaAttr}

            'Set up a TOKEN_PRIVILEGES structure for the returned (modified) privileges.
            Dim prevState As TOKEN_PRIVILEGES = New TOKEN_PRIVILEGES
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

    Public Function SelectFile(ByVal mydialog As FileDialog, ByVal InitialDir As String, ByVal myFilter As String, Optional ByVal NoZips As Boolean = False) As String

        'Displays a file dialog box to allow user to select a file - Initial directory, filter and an optional NoZips are passed in...
        'If NoZips is true, then only the filter is an allowable extension to select, if NoZips is false, then allowable selections
        'are the filter passed in and .zip...

        SelectFile = ""

        mydialog.InitialDirectory = InitialDir
        mydialog.FileName = ""

        If NoZips = False Then
            mydialog.Filter = "zip files (*.zip) | *.zip|" & myFilter & " files (*." & myFilter & ") | *." & myFilter
        Else
            mydialog.Filter = myFilter & " | *." & myFilter
        End If

        mydialog.DefaultExt = myFilter

        mydialog.ShowDialog()

        If Len(mydialog.FileName) > 0 Then

            SelectFile = mydialog.FileName

        Else
            'MsgBox("You must select a valid file, Exiting...")
            Exit Function
        End If

    End Function

    Public Function SelectFileByType(ByVal mydialog As FileDialog, ByVal myFilter As String, ByVal mylistbox As ListBox, Optional ByRef INCAWorkspace As String = "") As String

        'Displays file dialog box using myFilter passed in.  INCAWorkspace name is optional, if provided, workspace name is
        'the filename selected minus the extension.

        'Unzips if necessary based on file selection
        'Uses projectname based on vehicle number to determine initialdirectory
        'If flash drive is plugged In, the initialdirectory is based on the directory in which the user selected files were
        'placed by the file transfer utility.

        Dim szip As SevenZipExtractor = Nothing
        Dim exreader As FileStream = Nothing
        Dim strarray() As String = Nothing

        Dim DestFile As String
        Dim ImportFileName As String

        SelectFileByType = ""

        If Len(InitialDirectory) = 0 Then 'Or InitialDirectory = My.Application.Info.DirectoryPath Then

            DetermineInitINCAProjectDir(InitialDirectory)

        End If

        'SelectFile displays a file dialog using parameters passed in, if the optional parameter INCAWorkspace has been 
        'provided to the SelectFileByType function, then the fourth parameter for SelectFile is true, indicating that
        'SelectFile should allow selection of .zip files as well as the provided filter...

        'If myFilter = "a2l" Or myFilter = "ptp" Then
        If myFilter <> "exp" Then
            'SourceFile = SelectFile(mydialog, InitialDirectory, myFilter, IIf(Len(INCAWorkspace) = 0, True, True))
            SourceFile = SelectFile(mydialog, InitialDirectory, myFilter, True)
        Else
            'SourceFile = SelectFile(mydialog, InitialDirectory, myFilter, IIf(Len(INCAWorkspace) = 0, True, False))
            SourceFile = SelectFile(mydialog, InitialDirectory, myFilter, False)
        End If

        If Len(SourceFile) > 0 Then

            InitialDirectory = Path.GetDirectoryName(SourceFile)

            If Len(SourceFile) > 0 And (InStr(SourceFile, ".zip") > 0 Or InStr(SourceFile, "." & myFilter) > 0) Then

                DestFile = My.Application.Info.DirectoryPath & "\" & Mid(SourceFile, InStrRev(SourceFile, "\") + 1, Len(SourceFile))

            Else
                HandleUserMessageLogging("GMRC", "SelectFileByType: Invalid file selected, please select a valid .zip file or " & myFilter & " file.", DISPLAY_MSG_BOX)
                'MsgBox("Invalid file selected, please select a valid .zip file or " & myFilter & " file.")
                SelectFileByType = ""
                Exit Function

            End If

            If Not System.IO.File.Exists(DestFile) Then

                HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(SourceFile) & ", Please wait...",,, FLASH_MSG_ON)
                'UserStatusInfo.Label1.Text = "Copying " & Path.GetFileName(SourceFile) & ", Please wait..."

                RoboCopyFile(SourceFile, My.Application.Info.DirectoryPath)
                If Not File.Exists(DestFile) Then
                    SelectFileByType = ""
                    UserStatusInfo.Hide()
                    Exit Function
                End If

                UserStatusInfo.Hide()

            Else

                If SourceFile <> DestFile Then
                    If MsgBox("File already exists.  Do you wish to replace this file?", vbYesNo) = vbYes Then
                        HandleUserMessageLogging("GMRC", "File " & DestFile & " found.  Copying over existing file.",,, FLASH_MSG_ON, mylistbox)
                        'UserStatusInfo.Label1.Text = "File " & DestFile & " found.  Copying over existing file."
                        'mylistbox.Items.Add("File " & DestFile & " found.  Copying over existing file.")

                        If Debugger.IsAttached = False Then

                            HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(SourceFile) & ", Please wait...",,, FLASH_MSG_ON)
                            'UserStatusInfo.Label1.Text = "Copying " & Path.GetFileName(SourceFile) & ", Please wait..."

                            RoboCopyFile(SourceFile, My.Application.Info.DirectoryPath)
                            If Not File.Exists(DestFile) Then
                                SelectFileByType = ""
                                UserStatusInfo.Hide()
                                Exit Function
                            End If

                        Else

                            HandleUserMessageLogging("GMRC", "Bypassing copy of " & Path.GetFileName(SourceFile) & ", for testing...",,, FLASH_MSG_ON)
                            'UserStatusInfo.Label1.Text = "Bypassing copy of " & Path.GetFileName(SourceFile) & ", for testing..."

                        End If

                        UserStatusInfo.Hide()

                    Else
                        HandleUserMessageLogging("GMRC", "File " & DestFile & " found.  Using existing file.",,, FLASH_MSG_ON, mylistbox)
                        'UserStatusInfo.Label1.Text = "File " & DestFile & " found.  Using existing file."
                        'mylistbox.Items.Add("File " & DestFile & " found.  Using existing file.")
                    End If
                End If

            End If

            If InStr(DestFile, ".zip") > 0 Then

                'SevenZipBase.SetLibraryPath("C:\Program Files (x86)\7-Zip\7z.dll")

                'SevenZipBase.SetLibraryPath(SevenZipLibraryPath)

                szip = New SevenZipExtractor(DestFile)
                exreader = New FileStream(DestFile, FileMode.Open)

                If Not szip Is Nothing Then

                    strarray = szip.ArchiveFileNames.ToArray()
                    ImportFileName = My.Application.Info.DirectoryPath & "\" & strarray(0)

                    INCAWorkspace = Mid(strarray(0), 1, Len(strarray(0)) - 4)

                    szip.Dispose()
                    szip = Nothing
                    exreader.Close()
                    exreader = Nothing

                Else
                    HandleUserMessageLogging("GMRC", "Zip File Processing Error. Exiting...", DISPLAY_MSG_BOX)
                    'MsgBox("Zip File Processing Error. Exiting...")
                    SelectFileByType = ""
                    Exit Function
                End If

                If Not System.IO.File.Exists(ImportFileName) Then

                    HandleUserMessageLogging("GMRC", "Unzipping File, Please wait...",,, FLASH_MSG_ON, mylistbox)
                    'UserStatusInfo.Label1.Text = "Unzipping File, Please wait..."
                    'mylistbox.Items.Add("Unzipping File, Please wait...")

                    UnzipFile(DestFile)

                Else
                    'UserStatusInfo.Label1.Text = "File " & ImportFileName & " found."
                    'Me.ListBox1.Items.Add("File " & ImportFileName & " found.")
                End If

            Else
                'UserStatusInfo.Label1.Text = "File " & DestFile & " found."
                'Me.ListBox1.Items.Add("File " & DestFile & " found.")
                ImportFileName = DestFile
                INCAWorkspace = Mid(DestFile, InStrRev(DestFile, "\") + 1, Len(DestFile) - InStrRev(DestFile, "\") - 4)
            End If

            SelectFileByType = ImportFileName

        Else
            'MsgBox("No File Selected, Exiting...")
        End If

        UserStatusInfo.Hide()

    End Function

    Public Function ChangeWorkspaceDataset(ByVal systemName As String, ByVal deviceName As String, ByVal projName As String, ByVal projPath As String, ByVal datasetFullname As String) As Boolean

        'Called from AddSelectedProjectToWorspace and from ModifyWorkspaces...

        'Changes the workspace dataset based on the parameters passed in...

        Dim hwSystem As Object
        Dim deviceObj As Object
        Dim proj As Object

        ChangeWorkspaceDataset = False

        hwSystem = MyHWC.GetSystem(systemName)

        If Not (hwSystem Is Nothing) Then
            deviceObj = hwSystem.GetDevice(deviceName)    'Obj: HWWorkbaseDevice

            If Not (deviceObj Is Nothing) Then
                proj = myActualDatabase.GetItemInFolder(projName, projPath) 'Obj: Asap2Project

                If Not (proj Is Nothing) Then
                    ChangeWorkspaceDataset = deviceObj.SetProjectAndDataSet(proj, datasetFullname)
                Else
                    HandleUserMessageLogging("GMRC", "ChangeWorkspaceDataset: INCA Project " & projPath & "\" & projName & " not found.", DISPLAY_MSG_BOX)
                    'MsgBox("ChangeWorkspaceDataset: INCA Project " & projPath & "\" & projName & " not found.")
                End If

            Else
                HandleUserMessageLogging("GMRC", "ChangeWorkspaceDataset: " & deviceName & " not found in " & systemName, DISPLAY_MSG_BOX)
                'MsgBox("ChangeWorkspaceDataset: " & deviceName & " not found in " & systemName)
            End If
        Else
            HandleUserMessageLogging("GMRC", "ChangeWorkspaceDataset: " & systemName & " not found.  This error is likely due to the Video Addon not being installed.  Please Exit CLEVIR and INCA and Install the INCA Video Addon. Then retry this operation.", DISPLAY_MSG_BOX, SEND_LIVE_UPDATE)
            'UpdateVehicleStatus("Flashomatic ChangeWorkspaceDataset: " & systemName & " not found. Failure could be due to video addon not installed.")
            'MsgBox("ChangeWorkspaceDataset: " & systemName & " not found.  This error is likely due to the Video Addon not being installed.  Please Exit CLEVIR and INCA and Install the INCA Video Addon. Then retry this operation.")
        End If

    End Function

    Public Function FileInUse(ByVal sFile As String) As Boolean

        'Checks if file in use, returns true if in use

        FileInUse = False

        If System.IO.File.Exists(sFile) Then
            Try
                Dim F As Short = FreeFile()
                FileOpen(F, sFile, OpenMode.Binary, OpenAccess.ReadWrite, OpenShare.LockReadWrite)
                FileClose(F)
            Catch
                Return True
            End Try
        End If

    End Function

    Public Function GetRandom(ByVal Min As Integer, ByVal Max As Integer) As Integer

        'Gets a random number between min and max...

        ' by making Generator static, we preserve the same instance '
        ' (i.e., do not create new instances with the same seed over and over) '
        ' between calls '
        Static Generator As System.Random = New System.Random()
        Return Generator.Next(Min, Max)
    End Function
End Module
