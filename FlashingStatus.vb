Option Strict Off

Imports System.IO
Imports de.etas.cebra.toolAPI.Common
Imports de.etas.cebra.toolAPI.Inca
'Imports SevenZip

Public Class FlashingStatus

    'This is the Import Software and Cals form.  It handles all functionality associated with selecting software and calibrations, building
    'custom workspaces as well as automatic flashing of newly created workspaces or flashing of a user selected workspace.  
    'Some of the code used to support the functionality on this screen is GlobalCommon code and is used by multiple applications, so some
    'code is stored in other global common modules.

    'This form Is displayed When the user selects Import Software And Cals (Or Workspace) And Flash from the Init form...

    Private Loading As Boolean

    Private Function CheckARXMLFileDirectory() As Boolean

        'Called when initiating creation of CLEVIR config files for new software version and or model year
        'Sees what is in the My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML folder and displays appropriate message

        Dim x As Integer
        Dim filenames As String = ""
        Dim msgboxtext As String

        CheckARXMLFileDirectory = False

        If Directory.Exists(My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML") Then

            HandleARXMLFileDirectory()

            'SaveArxmlFilename is a global, in this context it is set in HandleARXMLFileDirectory call above...
            If SaveArxmlFilename IsNot Nothing Then
                For x = 0 To UBound(SaveArxmlFilename)
                    If Len(filenames) = 0 Then
                        filenames = SaveArxmlFilename(x)
                    Else
                        filenames = filenames & " - " & SaveArxmlFilename(x)
                    End If
                Next x
            Else
                x = 0
            End If

            If x = 0 Then
                msgboxtext = "There are No ARXML files in the " & My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML folder. If the new software requires new ARXMLs, they must be placed into this folder.  If no new ARXML files are required, you may continue. Continue?"
            ElseIf x = 1 Then
                msgboxtext = filenames & " ARXML file found. If this is not the correct file, replace it with your desired ARXML file(s) before continuing. Continue, using this ARXML File?"
            Else
                msgboxtext = filenames & " ARXML file(s) found. If these are not the correct files, replace them with your desired ARXML files or file before continuing. Continue, using these ARXML Files?"
            End If

        Else
            Directory.CreateDirectory((My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML"))

            If Directory.Exists(My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML") = False Then
                Directory.CreateDirectory((My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\DBC"))
            End If

            If Directory.Exists(My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\SAVE") = False Then
                Directory.CreateDirectory((My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\SAVE"))
            End If

            msgboxtext = "There is No ARXML file in the " & My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML folder. If the new software requires new ARXMLs, they must be placed into this folder.  If no new ARXML files are required, you may continue. Continue?"
        End If

        If MsgBox(msgboxtext, vbYesNo) = vbYes Then
            HandleUserMessageLogging("GMRC", msgboxtext & " Yes.",, )
            CheckARXMLFileDirectory = True
        Else
            HandleUserMessageLogging("GMRC", msgboxtext & " No.",, )
        End If


    End Function

    Private Function ModifyWorkspaces() As Boolean

        'Called from ConvertUpdatedWorkspace:  Used for CSAV2 vehicles only.  Creates new workspace from vehicle specific template.
        'new workspace name is the INCAWorkspace (global) which is the imported workspace + _Template name
        'Calls ChangeWorkspaceDataset to copy datasets used in the imported workspace into the newly created vehicle specific workspace

        Dim z As Integer
        Dim i As Integer
        Dim x As Integer
        Dim myFolder As Folder
        Dim FoundFolder As Boolean

        Dim myHWSystems() As HWSystem

        Dim myHWDevices() As HWDevice
        Dim MyDatabaseItems() As DataBaseItem

        ModifyWorkspaces = True

        myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")

        FoundFolder = False

        If myFolder IsNot Nothing Then

            MyDatabaseItems = Nothing

            FoundFolder = True

            If ConfigureForNewSoftwareVersion = True Then

                'ProcessCANDBFiles()
                ReadCandbFiles()

                CopyVersionSpecificWorkspaceTemplate()

            End If

            MyDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName)

            'make sure that template workspace exists in folder before going further....
            'Copy from Q drive (or D drive) if template workspace does not exist.
            'template workspace name ( global INCAWorkspaceTemplateName) is built in 
            'ReadVehicleConfigsFile based on vehicle number and its associated information 
            'in the vehicleconfigruations.csv file. This name is set on initialization 
            'when the vehicleconfigurations.csv file is read...

            If MyDatabaseItems.Length = 0 Then

                HandleUserMessageLogging("GMRC", "Template Workspace not found, Retrieving file, Please wait...",,, FlashMsgOn)

                If File.Exists(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp") Then

                    HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & " in " & myFolder.GetName & ", found in install directory, importing...")
                    ImportFileIntoINCA(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp", True, False)

                ElseIf File.Exists(My.Application.Info.DirectoryPath & "\" & GSaveIncaWorkspaceTemplateName & ".exp") Then
                    HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & " in " & My.Application.Info.DirectoryPath & ", using generic template " & GSaveIncaWorkspaceTemplateName)
                    INCAWorkspaceTemplateName = GSaveIncaWorkspaceTemplateName
                    WorkspaceNameSuffix = GSaveWorkspaceNameSuffix
                    ImportFileIntoINCA(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp", True, False)
                Else

                    HandleUserMessageLogging("GMRC", My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp" & " Not found. Failed to Copy workspace template...", DisplayMsgBox,, FlashMsg1Sec, ListBox1)

                    ModifyWorkspaces = False
                    Exit Function

                End If

                MyDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName)

            End If

            If MyDatabaseItems.Length <> 0 Then

                HandleUserMessageLogging("GMRC", "Found Workspace " & MyDatabaseItems(0).GetName & " in " & myFolder.GetName)

                NewWorkspaceName = INCAWorkspace & "_" & WorkspaceNameSuffix

                MyDatabaseItems(0).Copy(NewWorkspaceName)

                MyHWC = Get_Workspace(NewWorkspaceName, "CLEVIR Setup\Workspaces")

                If MyHWC IsNot Nothing Then

                    myHWSystems = Nothing

                    myHWSystems = MyHWC.GetAllSystems

                    If myHWSystems IsNot Nothing Then

                        For i = 0 To UBound(myHWSystems)

                            If myHWSystems(i).GetName = "Ethernet-System:1" Then

                                myHWDevices = myHWSystems(i).GetAllDevices

                                For z = 0 To UBound(myHWDevices)

                                    Me.Cursor = Cursors.WaitCursor
                                    Me.Refresh()

                                    'below takes care of the different order of devices depending on number of devices (3) or (6) or CoPilot (3)
                                    'If we are using a 6 device workspace and converting to a 3 device workspace for CSAV2, the ordering
                                    'of the information in the ProjectDatabaseName (etc.) array(s) is IP, IR, K1P, K2P, K1R, K2R, so we need
                                    'to align the myHWDevices(z).GetName of the target 3 processor workspace which is IP, K1P, K2P, with the
                                    'source ProjectDatabaseName(x) - so 0-0, 1-2 and 2-3...
                                    If UBound(myHWDevices) = 2 And InStr(myHWDevices(z).GetName, "C") = 0 And UBound(ProjectDatabaseNames) > 2 Then
                                        Select Case z
                                            Case 0
                                                x = 0
                                            Case 1
                                                x = 2
                                            Case 2
                                                x = 3
                                        End Select

                                    ElseIf UBound(myHWDevices) > 2 And UBound(ProjectDatabaseNames) = 2 Then
                                        MsgBox("It looks like you are trying to use a workspace with only Primary EOCM for a Dual EOCM vehicle.  Please check to make sure you have selected a workspace the has the software that you need for all 6 micros...")
                                        ModifyWorkspaces = False

                                        Me.Cursor = Cursors.Arrow
                                        Me.Refresh()

                                        Exit Function
                                        'HC CHANGE - There should be no change here whether one (LC) or two (HC) processors...
                                    Else
                                        x = z
                                    End If

                                    Dim tempname As String

                                    tempname = myHWDevices(z).GetName

                                    If ChangeWorkspaceDataset(myHWSystems(i).GetName, myHWDevices(z).GetName, ProjectDatabaseNames(x), ProjectDatabasePaths(x), WorkingDataSetDataBasePaths(x)) = False Then

                                        HandleUserMessageLogging("GMRC", "ModifyWorkspaces: ChangeWorkspaceDataset returned false. Failed to Copy workspace template...", DisplayMsgBox,, FlashMsg1Sec, ListBox1)

                                        ModifyWorkspaces = False
                                        Exit Function
                                    End If

                                    System.Threading.Thread.Sleep(250)

                                Next z

                            ElseIf InStr(myHWSystems(i).GetName, "ONVIF") > 0 Then

                            Else

                            End If

                            System.Threading.Thread.Sleep(250)

                        Next i

                    Else 'MyHWC.GetAllSystems returned nothing...

                        HandleUserMessageLogging("GMRC", "ModifyWorkspaces: ERROR: No HWSystems found in " & NewWorkspaceName, DisplayMsgBox,, FlashMsg1Sec, ListBox1)

                        ModifyWorkspaces = False
                        Exit Function

                    End If

                Else 'Get_Workspace returned nothing...
                    HandleUserMessageLogging("GMRC", "ModifyWorkspaces: Get_Workspace could not find " & NewWorkspaceName, DisplayMsgBox,, FlashMsg1Sec, ListBox1)

                    ModifyWorkspaces = False
                    Exit Function
                End If

            Else 'myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName) not found...

                HandleUserMessageLogging("GMRC", "ModifyWorkspaces: Could not find " & INCAWorkspaceTemplateName, DisplayMsgBox,, FlashMsg1Sec, ListBox1)

                ModifyWorkspaces = False
                Exit Function

            End If

            If FoundFolder = False Then

                HandleUserMessageLogging("GMRC", "ModifyWorkspaces: ERROR: Did not find Folder in Workspaces List - " & myFolder.GetName, DisplayMsgBox,, FlashMsg1Sec, ListBox1)

                ModifyWorkspaces = False
                Exit Function
            End If
        Else 'myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces") not found...

            HandleUserMessageLogging("GMRC", "ModifyWorkspaces: ERROR: Did not find INCA Folder - " & "CLEVIR Setup\Workspaces" & myFolder.GetName, DisplayMsgBox,, FlashMsg1Sec, ListBox1)

            ModifyWorkspaces = False
            Exit Function
        End If

        UserStatusInfo.Hide()

    End Function


    Private Sub GetProjectDataPaths()

        'Called from ConvertUpatedWorkspace - gets the project name and project path for each XETK device used in the active experiment

        Dim x As Integer
        Dim myDevices() As ExperimentDevice
        Dim myWorkbaseDevice As WorkbaseDevice
        Dim ctr As Integer
        Dim l_ProjectDatabasePath As String
        Dim l_DeviceName As String

        ProjectDatabasePaths = Nothing
        ProjectDatabaseNames = Nothing

        myDevices = MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.GetAllDevices

        For x = 0 To UBound(myDevices)
            l_ProjectDatabasePath = ""
            If myDevices(x).IsWorkbaseDevice = True Then

                l_ProjectDatabasePath = myDevices(x).GetProjectDataBasePath

                If Len(l_ProjectDatabasePath) > 0 Then
                    ReDim Preserve ProjectDatabasePaths(ctr)
                    ReDim Preserve ProjectDatabaseNames(ctr)
                    ProjectDatabasePaths(ctr) = Mid(l_ProjectDatabasePath, 1, InStrRev(l_ProjectDatabasePath, "\") - 1)
                    ProjectDatabaseNames(ctr) = Mid(l_ProjectDatabasePath, InStrRev(l_ProjectDatabasePath, "\") + 1, Len(l_ProjectDatabasePath))

                    l_DeviceName = myDevices(x).GetName
                    HandleUserMessageLogging("GMRC", "ProjectDatabaseName( " & ctr & " ) for " & l_DeviceName & " - " & ProjectDatabaseNames(ctr))
                    HandleUserMessageLogging("GMRC", "ProjectDatabasePath( " & ctr & " ) for " & l_DeviceName & " - " & ProjectDatabasePaths(ctr))
                End If

                myWorkbaseDevice = myDevices(x)
                ReDim Preserve ReferenceDataSetDataBasePaths(ctr)
                ReferenceDataSetDataBasePaths(ctr) = myWorkbaseDevice.GetReferenceDataSetDataBasePath
                HandleUserMessageLogging("GMRC", "ReferenceDataSetDataBasePaths(" & ctr & ") = " & ReferenceDataSetDataBasePaths(ctr))
                ReDim Preserve WorkingDataSetDataBasePaths(ctr)
                WorkingDataSetDataBasePaths(ctr) = myWorkbaseDevice.GetWorkingDataSetDataBasePath
                HandleUserMessageLogging("GMRC", "WorkingDataSetDataBasePath(" & ctr & ") = " & WorkingDataSetDataBasePaths(ctr))

                ctr += 1
            End If
        Next

    End Sub

    Private Function ConvertUpdatedWorkspace(ByVal mydialog As FileDialog, ByVal mylistbox As ListBox) As Boolean

        'Called from HandleImportSoftwareAndCals (Flashing Status GO button pressed) if selected vehicle number indicates a CSAV2 type vehicle...

        'User selects workspace to be imported and converted.  Workspace is copied and unzipped if necessary and imported.
        'Template workspace based on vehicle number is copied into new workspace. workspace name is original name + _templatename
        'project information, that is software and cals, is copied from imported workspace into new workspace thereby creating
        'a vehicle specific workspace which uses the software and cals from the imported workspace.

        Dim ImportFileName As String
        Dim myFolder As Folder
        Dim ImportWorkspace As Boolean
        Dim ConvertWorkspace As Boolean
        Dim SaveINCAExperiment As String

        Dim ReturnStr As String = ""
        Dim MyDatabaseItems() As DataBaseItem

        ConvertUpdatedWorkspace = False

        'User selects file from file dialog and INCAWorkspace is passed byref and set in SelectFileByType...
        ImportFileName = SelectFileByType(mydialog, "exp", mylistbox, INCAWorkspace)

        SaveProjectFiles(0) = INCAWorkspace

        If Len(ImportFileName) > 0 Then

            HandleUserMessageLogging("GMRC", INCAWorkspace & " selected.",,, FlashMsgOn, mylistbox)

            'XML

            'Get model year and software version, derived from INCAWorkspace name...
            GModelYear = DetermineModelYear(INCAWorkspace)
            GSoftwareVersion = DetermineSoftwareVersion(INCAWorkspace)

            GSpecificArxml = DetermineIfUsingDifferentArxml(INCAWorkspace)

            'We would expect INCAWorkspaceTemplateName not to have _MY as part of the name here.  This global variable is set in
            'ReadVehicleConfigsFile which does not yet comprehend model year...
            If InStr(INCAWorkspaceTemplateName, "_MY") = 0 Then
                'We should really check here to make sure that GModelYear and GSoftwareVersionare valid and not "??" or "???" respectively...
                'If either of these values contains question marks that means that the INCAWorkspace name is not a valid format.  The
                'INCAWorkspace file name format should allow us to extract model year and software version...
                INCAWorkspaceTemplateName = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & INCAWorkspaceTemplateName
                WorkspaceNameSuffix = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & WorkspaceNameSuffix
            End If

            NewWorkspaceName = INCAWorkspace & "_" & WorkspaceNameSuffix

            HandleUserMessageLogging("GMRC", NewWorkspaceName & " will be created...",,, FlashMsgOn, mylistbox)

            HandleUserMessageLogging("GMRC", "Launching INCA, Please wait...",,, FlashMsgOn, mylistbox)

            ReturnStr = MyIncaInterface.ConnectToInca()

            If ReturnStr = "True" Then

                Me.SendToBack()
                Me.BringToFront()
                Me.Activate()

                UserStatusInfo.Hide()

                myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")
                MyDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspace)

                'Here we check if the workspace already exists in INCA database...
                'If not, we need to import and convert...
                If MyDatabaseItems.Length = 0 Then
                    ImportWorkspace = True
                    ConvertWorkspace = True
                Else ' If workspace already exists, we ask the user if they wish to re-import...

                    Me.SendToBack()
                    Me.BringToFront()
                    Me.Activate()

                    If MsgBox("Workspace " & INCAWorkspace & " already exists in INCA database, would you like to re-import?", MsgBoxStyle.YesNo, "USER RESPONSE REQUIRED") = vbYes Then
                        HandleUserMessageLogging("GMRC", "Would you like to re-import? Yes.",, )
                        ImportWorkspace = True
                        ConvertWorkspace = True

                    Else  'If user does not want to re-import, then we will just move on without importing...

                        HandleUserMessageLogging("GMRC", "Would you like to re-import? No.",, )
                        UserStatusInfo.Hide()

                    End If
                End If

                If ImportWorkspace = True Then

                    HandleUserMessageLogging("GMRC", "Importing Workspace, Please wait...",,, FlashMsgOn, mylistbox)

                    If ImportFileIntoINCA(ImportFileName, True, False) = False Then
                        HandleUserMessageLogging("GMRC", "Import Failed " & ImportFileName & " Exiting...", DisplayMsgBox,, FlashMsgOn, mylistbox)
                        UserStatusInfo.Hide()
                        Exit Function
                    End If

                    HandleUserMessageLogging("GMRC", "Importing Workspace Complete.",,,, mylistbox)

                Else

                    'Here we double check the existance of the NewWorkspaceName, if it already exists, we really don't need to do anything more...
                    MyDatabaseItems = myFolder.BrowseDataBaseItem(NewWorkspaceName)

                    If MyDatabaseItems.Length = 0 Then
                        ConvertWorkspace = True
                    Else 'If NewWorkspaceName does not exist, that means that we can use the INCAWorkspace that already exists in the INCA database
                        'and convert using NewWorkspaceName
                        ConvertWorkspace = False
                    End If

                End If

                If ConvertWorkspace = True Then

                    MyHWC = Get_Workspace(INCAWorkspace)

                    If MyHWC IsNot Nothing Then

                        HandleUserMessageLogging("GMRC", "Retrieving Workspace Project / Dataset Information, This operation will take a while. Please be patient...",,, FlashMsgOn, mylistbox)

                        AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames

                        SaveINCAExperiment = INCAExperiment
                        INCAExperiment = "Empty Experiment"

                        'In order to retrieve project and dataset information for the workspace, we need to open an empty experiment using the INCAWorkspace
                        'which has in it the software and cals which are to be placed in NewWorkspaceName...
                        ReturnStr = MyIncaInterface.HandleWorkspace("", False)

                        If ReturnStr = "True" Or InStr(ReturnStr, "ERROR:") = 0 Then

                            INCAExperiment = SaveINCAExperiment

                            Me.SendToBack()
                            Me.BringToFront()
                            Me.Activate()

                            'gets the project name and project path for each ETK device used in the active experiment,puts this info into 
                            'global String array ProjectDatabasePaths() - 1 array element for each XETK device...
                            GetProjectDataPaths()

                            MyIncaInterface.CloseExperiment()

                            HandleUserMessageLogging("GMRC", "Retrieving Workspace Project / Dataset Information Complete.",,,, mylistbox)

                            Me.Cursor = Cursors.WaitCursor
                            Me.Refresh()

                            HandleUserMessageLogging("GMRC", "Creating New Workspace with Correct Vehicle Configuration and Updated Software and Cals, Please wait...",,, FlashMsgOn, mylistbox)

                            'ModifyWorkspaces Creates New workspace from vehicle specific template.
                            'Also copies datasets used in the imported workspace into the newly created 
                            'vehicle specific workspace, NewWorkspaceName...
                            If ModifyWorkspaces() = True Then

                                HandleUserMessageLogging("GMRC", "Creating New Workspace Complete.",,,, mylistbox)

                            Else
                                HandleUserMessageLogging("GMRC", "ModifyWorkspaces returned false. Exiting.",,,, mylistbox)
                                UserStatusInfo.Hide()

                                Me.Cursor = Cursors.Arrow
                                Me.Refresh()
                                Exit Function

                            End If

                            Me.Cursor = Cursors.Arrow
                            Me.Refresh()

                            UserStatusInfo.Hide()

                        Else
                            HandleUserMessageLogging("GMRC", "ConvertUpdatedWorkspace failed due to HandleWorkspace Error - " & ReturnStr, DisplayMsgBox, )
                            UserStatusInfo.Hide()
                            Exit Function
                        End If

                    Else 'Get_Workspace(INCAWorkspace) returned nothing...

                        'HandleUserMessageLogging("GMRC", "Failed to find " & INCAWorkspace & " in CLEVIR Setup\Workspaces. Name of INCA .exp file does not match actual INCA Workspace Name.  User most likely performed a Save As operation when exporting the workspace from INCA. Exiting...", DisplayMsgBox, )
                        HandleUserMessageLogging("GMRC", "ConvertUpdatedWorkspace: Get_Workspace Could not find " & INCAWorkspace & " in CLEVIR Setup\Workspaces. Exiting...", DisplayMsgBox, )

                        UserStatusInfo.Hide()
                        Exit Function

                    End If

                    'SetupCameraNamesInWorkspace Sets up camera names in newly created workspace based on
                    'the contents of the vehicleconfigurations.csv file.
                    If SetupCameraNamesInWorkspace(NewWorkspaceName) = True Then

                        HandleUserMessageLogging("GMRC", "Changing WORKSPACE Name in CLEVIR DEMO configuration file (config.xml) to " & NewWorkspaceName,,, FlashMsg1Sec)

                        INCAWorkspace = NewWorkspaceName

                        WriteConfigFile()

                        HandleUserMessageLogging("GMRC", "Operation Complete: " & "CLEVIR has created a new workspace specific to vehicle " & VehicleNumber & " (" & NewWorkspaceName & ") CLEVIR may now be used to flash using this new workspace.",,, UserStatusInfoTimeSec:=FlashMsgOn)

                    Else
                        HandleUserMessageLogging("GMRC", "Camera configuration did Not complete successfully.  Exiting...", DisplayMsgBox, )
                        Exit Function
                    End If

                End If

                ConvertUpdatedWorkspace = True

            Else
                HandleUserMessageLogging("GMRC", "ConvertUpdatedWorkspace: ConnectToInca returned - " & ReturnStr, DisplayMsgBox, )
            End If

        Else
            MsgBox("No File Selected, Exiting...")
        End If

    End Function

    Private Function ImportSelectedWorkspace(ByVal mydialog As FileDialog, ByVal mylistbox As ListBox) As Boolean

        'Called from HandleImportSoftwareAndCals (when Import button is pressed on FlashingStatus screen) if the 
        'Import Workspace(Workspace matches vehicle hardware - Typically LowContent Vehicles) RadioButton is selected...

        'Typically we do not use this method because in most cases, LowContent vehicles will use a user selected a2l and ptp file.  This option should
        'only be used if the workspace selected matches the vehicle instrumentation hardware setup, which would not be typical...

        'User selects workspace
        'workspace is copied and unzipped if necessary
        'workspace is imported into INCA and can then be used to flash

        Dim myFolder As Folder

        Dim ImportWorkspace As Boolean
        Dim SaveDeviceType As String = ""
        Dim ImportFileName As String
        Dim MyDatabaseItems() As DataBaseItem

        ImportSelectedWorkspace = False

        ImportFileName = SelectFileByType(mydialog, "exp", mylistbox, INCAWorkspace)

        If Len(ImportFileName) > 0 Then

            SaveProjectFiles(0) = INCAWorkspace

            HandleUserMessageLogging("GMRC", "Workspace " & INCAWorkspace & " selected.",,, FlashMsgOn, mylistbox)
            HandleUserMessageLogging("GMRC", INCAWorkspace & " will be created...",,, FlashMsgOn, mylistbox)
            HandleUserMessageLogging("GMRC", "Launching INCA, Please wait...",,, FlashMsgOn, mylistbox)

            Dim returnstr As String = ""

            returnstr = MyIncaInterface.ConnectToInca()

            If returnstr = "True" Then

                'If MyIncaInterface.ConnectToInca() = "True" Then

                Me.SendToBack()
                Me.BringToFront()
                Me.Activate()

                UserStatusInfo.Hide()

                myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")
                MyDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspace)

                If MyDatabaseItems.Length = 0 Then
                    ImportWorkspace = True
                Else

                    Me.SendToBack()
                    Me.BringToFront()
                    Me.Activate()

                    If MsgBox("Workspace " & INCAWorkspace & " already exists in INCA database, would you like to re-import?", MsgBoxStyle.YesNo, "USER RESPONSE REQUIRED") = vbYes Then
                        ImportWorkspace = True
                    Else
                        HandleUserMessageLogging("GMRC", "Changing WORKSPACE Name in CLEVIR DEMO configuration file (config.xml) to " & INCAWorkspace,,, FlashMsgOn)
                        WriteConfigFile()
                        UserStatusInfo.Hide()
                    End If
                End If

                If ImportWorkspace = True Then

                    HandleUserMessageLogging("GMRC", "Importing Workspace, Please wait...",,, FlashMsgOn, mylistbox)

                    If ImportFileIntoINCA(ImportFileName, True, False) = False Then
                        HandleUserMessageLogging("GMRC", "Import Failed. Exiting...", DisplayMsgBox,, FlashMsgOn)
                        Exit Function
                    End If

                    HandleUserMessageLogging("GMRC", "Importing Workspace Complete.",,,, mylistbox)

                    HandleUserMessageLogging("GMRC", "Changing WORKSPACE Name in CLEVIR DEMO configuration file (config.xml) to " & INCAWorkspace,,, FlashMsgOn)
                    WriteConfigFile()

                    HandleUserMessageLogging("GMRC", "Operation Complete: " & "CLEVIR has imported a new workspace (" & INCAWorkspace & ") CLEVIR may now be used to flash using this new workspace.",,, FlashMsgOn)

                End If

                ImportSelectedWorkspace = True

            Else
                HandleUserMessageLogging("GMRC", "ImportSelectedWorkspace: ConnectToInca returned - " & returnstr, DisplayMsgBox,, FlashMsgOn)

            End If

        Else
            MsgBox("No File Selected, Exiting...")
        End If
        UserStatusInfo.Hide()

    End Function

    Private Sub HandleImportSoftwareAndCals()

        'Called when the Import Button on the FlashingStatus screen is pressed...
        'Behavior Is based on which radio button is selected prior to pressing Import.  The initial radio button selected when the flashing status screen is displayed, 
        'is based on the ProjectName which is based on the vehicle number and derived from information in the vehicleconfigurations.csv file which is read on CLEVIR start up.

        Dim retval As Boolean

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "HandleImportSoftwareAndCals Called...")
        HandleUserMessageLogging("GMRC", " ")

        ' Import Software and Cals implies DEVELOPMENT vehicle usage
        CurrentVehicleUsage = "DEVELOPMENT"
        ZipTheMF4Files = True
        'VALIDATION use case does not take advantage of this functionality, they do the setup of workspaces etc. in INCA manually...

        'Disable Retry Flashing button for now...
        Button3.Enabled = False

        'CheckBox1 is the Configure for New Software Version checkbox...
        'IF checked, we give the user the opportunity to set CLEVIR up for the creation of new CLEVIR configuration files for a new software version
        'or continue normally with the standard import software and cals process...
        If CheckBox1.Checked = False And CheckBox1.Visible = True Then

            HandleUserMessageLogging("GMRC", "CLEVIR Administrator.",, )

            If MsgBox("Create new CLEVIR configuration files for new Software Version and/or Model Year?", vbYesNo) = vbYes Then
                HandleUserMessageLogging("GMRC", "Create new CLEVIR configuration files, yes.",, )
                CheckBox1.Checked = True
            Else
                HandleUserMessageLogging("GMRC", "Create new CLEVIR configuration files, no.",, )
            End If
            If CheckBox1.Checked = False Then
                If MsgBox("Continue to Import Software and Cals without creating CLEVIR configuration files?", vbYesNo) = vbNo Then
                    HandleUserMessageLogging("GMRC", "Continue to Import, no.",, )
                    Exit Sub
                Else
                    HandleUserMessageLogging("GMRC", "Continue to Import, yes.",, )
                End If

            End If
        End If

        'Behavior here Depends on which radionbutton is selected prior to pressing the Import button...

        If RadioButton1.Checked = True Then 'Import and Convert Workspace for specific vehicle (Typically CSAV2 Vehicles)
            If ConvertUpdatedWorkspace(Me.OpenFileDialog1, ListBox1) = True Then
                HandleFlashAndDrive(ListBox1)
            Else
                HandleUserMessageLogging("GMRC", "ConvertUpdatedWorkspace Failed.", DisplayMsgBox, )
            End If
        ElseIf RadioButton3.Checked = True Then 'Import Workspace (Workspace matches vehicle hardware - Typically LowContent Vehicles)
            If ImportSelectedWorkspace(Me.OpenFileDialog1, ListBox1) = True Then
                HandleFlashAndDrive(ListBox1)
            Else
                HandleUserMessageLogging("GMRC", "ImportSelectedWorkspace Failed.", DisplayMsgBox, )
            End If
        Else

            retval = A2LAndCalToVehicleSpecificWorkspace(Me.OpenFileDialog1, Me.FolderBrowserDialog1, ListBox1)

            If retval = True Then

                HandleUserMessageLogging("GMRC", "Changing WORKSPACE Name in CLEVIR DEMO configuration file (config.xml) to " & NewWorkspaceName,,, FlashMsg1Sec)

                INCAWorkspace = NewWorkspaceName

                WriteConfigFile()

                HandleUserMessageLogging("GMRC", "Operation Complete: " & "CLEVIR has created a new workspace for vehicle " & VehicleNumber & " (" & NewWorkspaceName & ")",,, FlashMsg1Sec)

                Me.Cursor = Cursors.Arrow

                HandleFlashAndDrive(ListBox1)

            Else
                HandleUserMessageLogging("GMRC", "A2lAndCALToVehicleSpecificWorkspace Failed.", DisplayMsgBox, )
            End If

        End If

        'If set, after we perform the standard import software and cals to create a project and workspace, we will handle the process of
        'creating all of the necessary files that need to be created for a new software version and / or model year...

        If ConfigureForNewSoftwareVersion = True Then
            HandleNewSoftwareVersionConfig()
        End If

    End Sub

    Private Sub HandleNewEnumerationsFileCreation()

        '14.	Create New A2l  file sub-folder in A2l directory for software version specific a2l files (automate)
        '15.	Copy a2l files used when creating New workspace from app directory to New a2l directory (automate)
        '16.	Delete a2l files from a2l directory (automate)
        '17.	Prompt user to Write A2l files for ARXML Clusters (Or DBC Files) into New A2l  file sub-folder in A2l directory (manual)
        '18.	Copy a2l files from New a2l file sub-folder to a2l sub folder (automate)
        '30.	Delete Enumerations.txt file from app directory

        Dim enumFileName As String

        HandleUserMessageLogging("GMRC", "Handle New Enumerations File Creation Called...",, )

        If CopyA2lFilesToA2lFolder() = True Then
            DeleteEnumerationsFile()

            'XML

            If InStr(FCMConfigName, "FCM") = 0 Then
                enumFileName = Path.Combine(My.Application.Info.DirectoryPath, $"{GSoftwareVersion}_MY{GModelYear}{GSpecificArxml}_Enumerations_{GProjectAbbreviation}.txt")
            Else
                enumFileName = Path.Combine(My.Application.Info.DirectoryPath, $"{GSoftwareVersion}_MY{GModelYear}{GSpecificArxml}_Enumerations_{FCMConfigName}.txt")
            End If

            'Parses the A2l file so we can get our Emums for display purposes - if generic enumerations.txt file exists, it will be used.  If not, a2ls in the A2l directory
            'will be used to create an enumerations.txt file...
            Me.Cursor = Cursors.WaitCursor
            ParseA2lFile(enumFileName)
            Me.Cursor = Cursors.Arrow

        Else
            HandleUserMessageLogging("GMRC", "Copy A2l Files to A2l Folder not complete.  New enumerations file was not created.", DisplayMsgBox, )
        End If

    End Sub

    Private Sub HandleNewSoftwareVersionConfig()

        'Called from Import Button on FlashingStatus form after completion of the import software and cals is complete.  
        'This Is only called if the ConfigureForNewSoftwareVersion flag is set (by checking the Configure for New Software Version check box).
        'This check box is only made visible to the user if running in the design environment or if logged in as Administrator.  The functionality is intended to be used only
        'if we are creating new CLEVIR support files for use with a newly released major software version...

        '14.	Create New A2l  file sub-folder in A2l directory for software version specific a2l files (automate)
        '15.	Copy a2l files used when creating New workspace from app directory to New a2l directory (automate)
        '16.	Delete a2l files from a2l directory (automate)
        '17.	Prompt user to Write A2l files for ARXML Clusters (Or DBC Files) into New A2l  file sub-folder in A2l directory (manual)
        '18.	Copy a2l files from New a2l file sub-folder to a2l sub folder (automate)

        HandleUserMessageLogging("GMRC", "Handle New Software Version Config Called...",, )

        HandleNewEnumerationsFileCreation()

        '9.	Add HCS Or LC a2l filename to HC Or LC ARXML Mapping file (automate)
        '10 Add New ARXML name to ARXML mapping file (automate)
        '11 Modify column 3 with New software version designation (automate)
        '12 Copy existing vspy config file from previous row to newly created row (automate)
        '13 Save ARXML Mapping file (automate)

        HandleARXMLMappingFileUpdate()

        '19.	Open New blank experiment in workspace (automate)
        '20.	Add all CAN signals to default recorder - Pompt user, manual  operation in INCA experiment environment...
        '21.	Rename existing blank experiment for project type (automate)
        '22.	Save experiment as project type blank experiment (automate)
        '23.	Rename selected signal list based on software ver, model year, and project type (automate)
        '24.	Open newly named baseline signal list (automate)
        '25.	Delete all CAN  signals in Record only section of signal list (automate)
        '26.	Insert all CAN signals from .csv file into New signal list (automate)

        If HandleNewExperimentCreation() = True Then

            HandleUserMessageLogging("GMRC", "Phase 1 of New Software version file configuration complete.  Will now create the Experiment from the newly created Signal List...", DisplayMsgBox, )

            ListBox2.Visible = False

            Me.Close()

            InitForm.Drive()

        End If

    End Sub

    Private Sub DeleteEnumerationsFile()

        '30.	Delete Enumerations.txt file from app directory

        If File.Exists(My.Application.Info.DirectoryPath & "\Enumerations.txt") Then
            File.Delete(My.Application.Info.DirectoryPath & "\Enumerations.txt")
        End If

    End Sub

    Private Function SelectBaselineSignalList() As String

        'Manual selection of the signal list file that will be used as a template for creating the new signal list

        Dim FileExtension As String = ".xlsx"

        HandleUserMessageLogging("GMRC", "Please select the signal list you wish to use as a baseline for the new signal list...", DisplayMsgBox, )

        SelectBaselineSignalList = ""

        OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath & "\SignalLists"
        OpenFileDialog1.DefaultExt = FileExtension
        OpenFileDialog1.FileName = ""
        OpenFileDialog1.Filter = Mid(FileExtension, 2, Len(FileExtension)) & " |*" & FileExtension
        OpenFileDialog1.Title = "Please Select a Signal Configuration File"
        OpenFileDialog1.ShowDialog()

        If Len(OpenFileDialog1.FileName) > 0 Then
            SelectBaselineSignalList = OpenFileDialog1.FileName
        End If

    End Function

    Private Function HandleAutoBaselineFileSelection() As String

        'Automatic selection of the signal list file that will be used as a template for creating the new signal list, if automatic selection does not find a candidate signal list
        'we will revert to manual selection here...

        'NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath & "\" & g_SoftwareVersion

        Dim dir As DirectoryInfo
        Dim files As FileInfo()
        Dim TempSoftwareVersion As String
        Dim dirname As String
        Dim SaveFileName As String = ""
        Dim FoundExistingFile As Boolean
        Dim FoundFileToUse As Boolean
        Dim x As Integer
        Dim y As Integer

        Dim CopyFile As Boolean

        '158_19263_MY23_HC.xlsx

        HandleAutoBaselineFileSelection = ""

        'Initial dirname is based on the g_SoftwareVersion, which is based on the a2l file selected at the start of the configuration process, and
        'the CLEVIRFilesPath, which comes from the vehicleconfigurations.csv file for the vehicle number selected...

        'First, we check if a sub-folder has been created for the software version, if so, we look through the signal lists, if not, we look in the previous model year folder...
        'If we find a signal list that matches the model year and software version (and specific XML if applicable), we alert the user...
        'If we find a signal list with the same software version and earlier model year, we "remember it" and move to the previous software version folder...
        'If we find a signal list with the same model year, but previous version, this becomes the candidate, if we don't, the same software version and earlier model year is used...

        TempSoftwareVersion = GSoftwareVersion

        dirname = NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath & "\" & TempSoftwareVersion

        If Not System.IO.Directory.Exists(dirname) Then
            TempSoftwareVersion = CStr(Val(GSoftwareVersion) - 1)

            dirname = NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath & "\" & TempSoftwareVersion

            If System.IO.Directory.Exists(dirname) Then

                dir = New DirectoryInfo(dirname)
                files = dir.GetFiles

                For x = 0 To 1

                    For y = 0 To UBound(files)

                        If (InStr(files(y).Name, ".xlsx") > 0) And (InStr(files(y).Name, "~") = 0) And
                            (InStr(files(y).Name, "MY" & CStr(Val(GModelYear) - x)) > 0) And
                            (Mid(files(y).Name, 1, 3) = TempSoftwareVersion) Then 'And ((Len(GSpecificArxml) = 0 Or InStr(files(y).Name, GSpecificArxml) > 0)) Then

                            'If we find a signal list that matches the previous software version from the same model year, we have found what we are looking for...       
                            'If not, we will loop again to see if there is a file from the previous model year...
                            FoundFileToUse = True
                            SaveFileName = files(y).FullName
                            CopyFile = True
                            Exit For
                        End If

                    Next

                    If FoundFileToUse = True Then
                        Exit For
                    End If

                Next

            End If

        Else 'Starting in the existing software version folder...

            For x = 0 To 1

                FoundExistingFile = False

                TempSoftwareVersion -= x

                dirname = NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath & "\" & TempSoftwareVersion

                If System.IO.Directory.Exists(dirname) Then

                    dir = New DirectoryInfo(dirname)
                    files = dir.GetFiles

                    'If we are in the same folder as our current model year, we first look to see if a file matching the criteria already exists...

                    For y = 0 To UBound(files)

                        If (InStr(files(y).Name, ".xlsx") > 0) And (InStr(files(y).Name, "~") = 0) And
                               (InStr(files(y).Name, "MY" & GModelYear) > 0) And
                               (Mid(files(y).Name, 1, 3) = TempSoftwareVersion) And
                               ((Len(GSpecificArxml) = 0 Or InStr(files(y).Name, GSpecificArxml) > 0)) Then

                            If x = 0 Then
                                FoundExistingFile = True
                                SaveFileName = files(y).FullName
                                Exit For
                            Else
                                FoundFileToUse = True
                                SaveFileName = files(y).FullName
                                CopyFile = True
                                Exit For
                            End If

                        End If

                        If (InStr(files(y).Name, ".xlsx") > 0) And (InStr(files(y).Name, "~") = 0) And
                               (InStr(files(y).Name, "MY" & CStr(Val(GModelYear) - 1)) > 0) And
                               (Mid(files(y).Name, 1, 3) = TempSoftwareVersion) And (FoundFileToUse = False) Then 'And ((Len(GSpecificArxml) = 0 Or InStr(files(y).Name, GSpecificArxml) > 0)) Then

                            'If we find a signal list that matches the software version from the previous model year, we set SaveFileName, we will continue looking
                            'in the previous model year folder for a better candidate in the next loop (x=1)...
                            FoundFileToUse = True
                            SaveFileName = files(y).FullName
                            CopyFile = True
                            Exit For
                        End If

                    Next
                    'If we find a file that matches all of our criteria, software verison, model year and specific XML (if pertinent) we exit loop and alert user below...
                    If FoundExistingFile = True Or FoundFileToUse = True Then
                        Exit For
                    End If

                End If

            Next x

        End If

        If Len(SaveFileName) > 0 Then

            If x = 0 And FoundExistingFile = True Then

                If MsgBox(SaveFileName & " found. Files already exist for " & GSoftwareVersion & " MY" & GModelYear & " Re-create?", vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", SaveFileName & " found. Files already exist for " & GSoftwareVersion & " MY" & GModelYear & " Re-create? Yes.",, )
                    SaveFileName = SelectBaselineSignalList()
                    CopyFile = True
                Else
                    HandleAutoBaselineFileSelection = ""
                    Exit Function
                End If

            End If

            If CopyFile = True Then
                If File.Exists(My.Application.Info.DirectoryPath & "\SignalLists\" & System.IO.Path.GetFileName(SaveFileName)) Then

                    If My.Application.Info.DirectoryPath & "\SignalLists\" & System.IO.Path.GetFileName(SaveFileName) <> SaveFileName Then
                        If MsgBox(System.IO.Path.GetFileName(SaveFileName) & " already exists. Overwrite?", vbYesNo) = vbYes Then
                            File.Copy(SaveFileName, My.Application.Info.DirectoryPath & "\SignalLists\" & System.IO.Path.GetFileName(SaveFileName), True)
                        End If
                    End If

                Else
                    HandleUserMessageLogging("GMRC", "Using " & System.IO.Path.GetFileName(SaveFileName) & " as template signal list. Copying file to local drive.", DisplayMsgBox, )
                    RoboCopyFile(SaveFileName, My.Application.Info.DirectoryPath & "\SignalLists")
                End If

                HandleAutoBaselineFileSelection = My.Application.Info.DirectoryPath & "\SignalLists\" & System.IO.Path.GetFileName(SaveFileName)
            End If

        Else
            HandleUserMessageLogging("GMRC", "No file found to use as a baseline based on the criteria provided...", DisplayMsgBox, )
            HandleAutoBaselineFileSelection = SelectBaselineSignalList()
        End If

        If Len(HandleAutoBaselineFileSelection) = 0 Then
            HandleUserMessageLogging("GMRC", "Invalid file selection.  Exiting...", DisplayMsgBox, )
        End If


    End Function

    Private Function HandleNewExperimentCreation() As Boolean

        '19.	Open New blank experiment in workspace (automate)
        '20.	Add all CAN signals to default recorder (automate?) Or message to user if Not
        '21.	Rename existing blank experiment for project type (automate)
        '22.	Save experiment as project type blank experiment (automate)
        '23.	Rename selected signal list based on software ver, model year, and project type (automate)
        '24.	Open newly named baseline signal list (automate)
        '25.	Delete all CAN  signals in Record only section of signal list (automate)
        '26.	Insert all CAN signals from .csv file into New signal list (automate)

        Dim ErrorMsg As String = ""
        Dim myDatabaseItems() As DataBaseItem
        Dim myfolder As IncaFolder
        Dim myDevices() As ExperimentDevice
        Dim x As Integer
        Dim y As Integer
        Dim myMeasureElements() As String
        Dim myRasterNames() As String
        Dim mySignalsForRegistration() As IGM_INCA_Comm.DeviceRasterSignalStatus = Nothing
        Dim SignalCounter As Integer

        Dim SignalListFile As String = ""
        Dim NewSignalListFileName As String = ""

        HandleNewExperimentCreation = False

        If MsgBox("Create new Experiment and Signal List File?", vbYesNo) = vbYes Then

            CanTemplateExperimentName = GProjectAbbreviation & "_CAN_Template_Exp"

            HandleUserMessageLogging("GMRC", "Creating " & CanTemplateExperimentName & " Experiment...",,, FlashMsgOn)

            Me.Cursor = Cursors.WaitCursor

            myfolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Experiments")
            myDatabaseItems = myfolder.BrowseDataBaseItem(CanTemplateExperimentName)

            If myDatabaseItems.Length > 0 Then
                myfolder.RemoveComponent(myDatabaseItems(0))
            End If

            myDatabaseItems = myfolder.BrowseDataBaseItem("Template Experiment")

            If myDatabaseItems Is Nothing Or UBound(myDatabaseItems) < 0 Then
                myfolder.AddExperimentEnvironment("Template Experiment")
                myDatabaseItems = myfolder.BrowseDataBaseItem("Template Experiment")

                If MyIncaInterface.InitINCA(INCADatabase, INCAWorkspace, "Template Experiment", True, ErrorMsg, False) = IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then

                    Me.Activate()

                    HandleUserMessageLogging("GMRC", "Template Experiment changes are required.  Prior to continuing, Open Recorder Configuration. On the Output File Tab, Delete User name text. Uncheck the Show Output File Properties box. On MDA Tab, uncheck the Automatically Generate XDA box. Then click OK here.", DisplayMsgBox, )

                    MyIncaInterface.SaveExperiment()
                    MyIncaInterface.CloseExperiment()

                Else
                    HandleUserMessageLogging("GMRC", ErrorMsg, DisplayMsgBox, )
                    Exit Function
                End If

            End If

            myDatabaseItems(0).Copy(CanTemplateExperimentName)

            If MyIncaInterface.InitINCA(INCADatabase, INCAWorkspace, CanTemplateExperimentName, True, ErrorMsg, False) = IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then

                'Here is where we want to add all of the CAN signals to be recorded.

                'First, we get all of the valid CAN signal names...

                HandleUserMessageLogging("GMRC", "Getting List of all signal names from CAN devices...",,, FlashMsgOn)

                myDevices = MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.GetAllDevices

                For x = 0 To UBound(myDevices)
                    If InStr(myDevices(x).GetName, "CAN-Monitoring") > 0 Then

                        myMeasureElements = MyIncaInterface.MyGmIncaComm.GetAllMeasureElementNamesInDevice(myDevices(x).GetName)

                        For y = 0 To UBound(myMeasureElements)
                            If InStr(myMeasureElements(y), "PCSM") = 0 And InStr(myMeasureElements(y), "NM_SYSTEMSIGNAL") = 0 And InStr(myMeasureElements(y), "TmSyncMsg") = 0 Then

                                If Len(MyIncaInterface.MyGmIncaComm.GetDefaultRasterForMeasureElementInDevice(myDevices(x).GetName, myMeasureElements(y))) > 0 Then

                                    ReDim Preserve myRasterNames(y)
                                    myRasterNames(y) = MyIncaInterface.MyGmIncaComm.GetDefaultRasterForMeasureElementInDevice(myDevices(x).GetName, myMeasureElements(y))

                                    ReDim Preserve mySignalsForRegistration(SignalCounter)

                                    mySignalsForRegistration(SignalCounter).DeviceName = myDevices(x).GetName
                                    mySignalsForRegistration(SignalCounter).RasterName = myRasterNames(y)
                                    mySignalsForRegistration(SignalCounter).SignalName = myMeasureElements(y)
                                    mySignalsForRegistration(SignalCounter).Status = "Invalid"
                                    mySignalsForRegistration(SignalCounter).ForceRegister = True

                                    SignalCounter += 1
                                Else
                                    'MsgBox("WARNING: HandleNewExperimentCreation: Invalid RasterName " & myDevices(x).GetName & " - " & myMeasureElements(y) & "...")
                                End If

                            End If

                        Next

                    End If

                Next x

                'With older versions of INCA, it was necessary to add all CAN signals to the default recorder here, because if we tried to add all signals to
                'a blank experiment, we would overflow memory (INCA issue), with INCA versions beyond 7.2.x, this problem seems to have been fixed.  We are keeping
                'a user choice here until we determine for sure that this issue no longer exists in INCA 7.3.x and 7.4.x...

                If MsgBox("Add CAN signals to the default recorder before continuing?", vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", "Please add all CAN signals to the default recorder before continuing...", DisplayMsgBox, )
                End If

                'After adding CAN signals, save experiment to become the new current project type specific "blank" experiment with all CAN signals in it...
                MyIncaInterface.SaveExperiment()
                MyIncaInterface.CloseExperiment()

                'Now we need to select baseline signal list file to modify for new experiment creation...

                If MsgBox("Auto-select the signal list from share drive to use as the template for the new signal list? (If no, user must choose list from local signal lists.)", vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", "Auto-select the signal list to use as the template for the new signal list? Yes.",, )
                    SignalListFile = HandleAutoBaselineFileSelection()
                Else
                    SignalListFile = SelectBaselineSignalList()
                End If

                If Len(SignalListFile) = 0 Then
                    UserStatusInfo.Hide()
                    Me.Cursor = Cursors.Arrow
                    Exit Function
                End If

                '23.	Rename selected signal list based on software ver, model year, and project type (automate)

                'FCM CHANGE ??? - Need to reconcile different FCM types here also because we are going to have different signal lists and experiments based on FCM_SA, FCM_MID, FCM_HIGH...
                'FCM_STA, FCM_LCM, FCM_LCH - Different Vendors also - may need to differentiate on ClevirFilesPath?

                'XML

                If InStr(FCMConfigName, "FCM") = 0 Then
                    NewSignalListFileName = Path.GetDirectoryName(SignalListFile) & "\" & GSoftwareVersion & "_" & Mid(Path.GetFileName(SignalListFile), 5, InStr(Path.GetFileName(SignalListFile), "_MY") - 5) & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & ".xlsx"
                Else
                    NewSignalListFileName = Path.GetDirectoryName(SignalListFile) & "\" & GSoftwareVersion & "_" & Mid(Path.GetFileName(SignalListFile), 5, InStr(Path.GetFileName(SignalListFile), "_MY") - 5) & "_MY" & GModelYear & GSpecificArxml & "_" & FCMConfigName & ".xlsx"
                End If

                If NewSignalListFileName = SignalListFile Then '5.6.2
                    NewSignalListFileName = Mid(NewSignalListFileName, 1, InStr(NewSignalListFileName, ".xlsx") - 1) & "_NEW.xlsx"
                    HandleUserMessageLogging("GMRC", "Baseline Signal List and New Signal List Names are the same.  New Signal List will be renamed to " & NewSignalListFileName, DisplayMsgBox, )
                End If

                If File.Exists(NewSignalListFileName) Then
                    If MsgBox("File " & NewSignalListFileName & " already exists.  Overwrite?", vbYesNo) = vbYes Then
                        HandleUserMessageLogging("GMRC", "Copying " & SignalListFile & " to " & NewSignalListFileName,, )
                        File.Copy(SignalListFile, NewSignalListFileName, True)
                    Else
                        HandleUserMessageLogging("GMRC", "Signal List File already exists, Exiting...", DisplayMsgBox, )
                        Exit Function
                    End If

                Else

                    File.Copy(SignalListFile, NewSignalListFileName, True)

                End If

                '24.	Open newly named baseline signal list (automate)
                '25.	Delete all CAN  signals in Record only section of signal list (automate)
                '26.	Insert all CAN signals from .csv file into New signal list (automate)

                ModifyCANSignalsInList(NewSignalListFileName, mySignalsForRegistration)

            Else
                HandleUserMessageLogging("GMRC", ErrorMsg, DisplayMsgBox, )
            End If

            HandleNewExperimentCreation = True

        End If

        UserStatusInfo.Hide()
        Me.Cursor = Cursors.Arrow

    End Function

    Private Sub ModifyCANSignalsInList(ByVal signallistfile As String, ByVal mySignals() As IGM_INCA_Comm.DeviceRasterSignalStatus)

        '24.	Open newly named baseline signal list (automate)
        '25.	Delete all CAN  signals in Record only section of signal list (automate)
        '26.	Insert all CAN signals from .csv file into New signal list (automate)

        Dim excelApp As Object = Nothing
        Dim wrkbk As Object = Nothing
        Dim myWorkSheet As Object = Nothing
        Dim x As Integer
        Dim y As Integer
        Dim z As Integer

        Dim DataLengthDifference As Integer
        Dim CANMonStartRow As Integer
        Dim NumberofCANSignals As Integer
        Dim StartOfRestOfData As Integer
        Dim exceldataforsavelength As Integer

        Dim myArrayList As ArrayList
        Dim tempstr As String
        Dim ModifiedSignalList() As IGM_INCA_Comm.DeviceRasterSignalStatus = Nothing

        HandleUserMessageLogging("GMRC", "Modifying CAN Signals In New Signal List...",,, FlashMsgOn)
        Me.Cursor = Cursors.WaitCursor

        HandleUserMessageLogging("GMRC", "ModifyCANSignalsInList: Creating Excel Object...")
        excelApp = CreateObject("Excel.Application")

        'excelApp.visible = True

        HandleUserMessageLogging("GMRC", "ModifyCANSignalsInList: Excel Object created.")
        wrkbk = excelApp.Workbooks.Open(signallistfile)
        myWorkSheet = wrkbk.Sheets(1)
        myWorkSheet.Activate()

        excelApp.DisplayAlerts = False

        myWorkSheet.UsedRange.Replace(",", " ")

        'set our excel data variable array (exceldata) to the entire used range in the spreadsheet
        exceldata = myWorkSheet.UsedRange.Value

        'move to first non-displayed row (which should be CAN-Monitoring signal, is excel data

        y = 1
        myArrayList = New ArrayList
        Do While Len(exceldata(y, EXCEL_DATA.DisplayWindowName)) > 0
            myArrayList.Add(exceldata(y, EXCEL_DATA.VariableName) & "," & exceldata(y, EXCEL_DATA.DeviceName) & "," & exceldata(y, EXCEL_DATA.Raster))
            y = y + 1
        Loop

        'y should now be at first CAN-Monitoring row...
        'set can monitoring start row for later use...
        CANMonStartRow = y

        Do While InStr(exceldata(y, EXCEL_DATA.DeviceName), "CAN-Monitoring") > 0
            y = y + 1
        Loop
        'set number of can signals in exceldata...
        NumberofCANSignals = y - CANMonStartRow

        For z = 0 To UBound(mySignals)
            tempstr = mySignals(z).SignalName & "," & mySignals(z).DeviceName & "," & mySignals(z).RasterName
            If Not myArrayList.Contains(tempstr) Then
                If ModifiedSignalList Is Nothing Then
                    ReDim Preserve ModifiedSignalList(0)
                Else
                    ReDim Preserve ModifiedSignalList(UBound(ModifiedSignalList) + 1)
                End If
                ModifiedSignalList(UBound(ModifiedSignalList)) = mySignals(z)
            Else
                'HandleUserMessageLogging("GMRC", "Removing redundant signal " & mySignals(z).SignalName, DisplayMsgBox, )
                HandleUserMessageLogging("GMRC", "Removing redundant signal " & mySignals(z).SignalName)
            End If

        Next

        DataLengthDifference = NumberofCANSignals - (UBound(ModifiedSignalList) + 1)

        'Set start of rest of data after all can monitoring rows...
        StartOfRestOfData = y

        'set data length for excel data to be saved...

        exceldataforsavelength = UBound(exceldata, 1) - DataLengthDifference

        ReDim exceldataforsave(exceldataforsavelength, UBound(exceldata, 2))

        'add displayed rows to excel data for save...
        For y = 1 To CANMonStartRow - 1
            For x = 1 To UBound(exceldata, 2)
                exceldataforsave(y - 1, x - 1) = exceldata(y, x)
            Next
        Next y

        'add new can monitoring signals to excel data for save...
        For y = 0 To UBound(ModifiedSignalList)
            If Len(ModifiedSignalList(y).DeviceName) > 0 And Len(ModifiedSignalList(y).SignalName) > 0 And Len(ModifiedSignalList(y).RasterName) > 0 Then

                exceldataforsave(y + CANMonStartRow - 1, EXCEL_DATA.DeviceName - 1) = ModifiedSignalList(y).DeviceName
                exceldataforsave(y + CANMonStartRow - 1, EXCEL_DATA.VariableName - 1) = ModifiedSignalList(y).SignalName
                exceldataforsave(y + CANMonStartRow - 1, EXCEL_DATA.Raster - 1) = ModifiedSignalList(y).RasterName

            Else
                HandleUserMessageLogging("GMRC", "ModifyCANSignalsInList: Invalid signal information...", DisplayMsgBox, )
            End If

        Next

        y = y + CANMonStartRow

        For z = StartOfRestOfData To UBound(exceldata, 1)

            If y <= (UBound(exceldataforsave, 1) + 1) Then
                For x = 1 To UBound(exceldata, 2)
                    exceldataforsave(y - 1, x - 1) = exceldata(z, x)
                Next
                y = y + 1
            End If

        Next

        wrkbk.sheets.add

        myWorkSheet = wrkbk.Sheets(1)

        myWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1), UBound(exceldata, 2)).Value = exceldataforsave

        wrkbk.Sheets(2).delete
        excelApp.DisplayAlerts = True

        myWorkSheet.name = "Signals"

        wrkbk.Save()

        excelApp.Quit()
        excelApp = Nothing

        Dim fnum As Integer
        Dim fnum2 As Integer
        Dim ctr As Integer
        Dim textline As String
        Dim savetextline As String

        fnum = FreeFile()
        ctr = 0

        FileOpen(fnum, My.Application.Info.DirectoryPath & "\config.xml", OpenMode.Input)

        fnum2 = FreeFile()
        FileOpen(fnum2, My.Application.Info.DirectoryPath & "\tempfile.txt", OpenMode.Output)

        Do While Not EOF(fnum)

            textline = LineInput(fnum)

            If ctr = 3 Then

                savetextline = "INCAVariableFile" & Chr(9) & signallistfile
                INCAVariableFile = signallistfile

            Else
                savetextline = textline
            End If
            ctr = ctr + 1

            PrintLine(fnum2, savetextline)

        Loop

        FileClose(fnum)
        FileClose(fnum2)

        FileCopy(My.Application.Info.DirectoryPath & "\tempfile.txt", My.Application.Info.DirectoryPath & "\config.xml")
        System.IO.File.Delete(My.Application.Info.DirectoryPath & "\tempfile.txt")

        HandleUserMessageLogging("GMRC", "ModifyCANSignalsInList: config.xml file has been updated.",, )

        Me.Cursor = Cursors.Arrow

        UserStatusInfo.Hide()

    End Sub

    Private Sub HandleARXMLMappingFileUpdate()

        'Called from HandleNewSoftwareVersionConfig, this is only called if we are configuring for a new software version which is a CLEVIR administrator
        'task only available if running with debugger.isattached = true...

        '9.	Add HCS Or LC a2l filename to HC Or LC ARXML Mapping file (automate)
        '10 Add New ARXML name to ARXML mapping file (automate)
        '11 Modify column 3 with New software version designation (automate)
        '12 Copy existing vspy config file from previous row to newly created row (automate)
        '13 Save ARXML Mapping file (automate)

        Dim filename As String
        Dim fnum As String
        Dim x As Integer

        Dim textline As String = ""
        Dim newtextline As String = ""
        Dim A2LFileName As String = ""
        Dim lineitems() As String = Nothing
        Dim softwareversionstring As String = ""
        Dim UpdateARXMLMappingFile As Boolean

        If SaveArxmlFilename Is Nothing Then
            HandleUserMessageLogging("GMRC", "HandleARXMLMappingFileUpdate: No ARXML Files in ARXML directory, no modifications will be made to the ARXML Mapping File...", DisplayMsgBox)
            Exit Sub
        End If

        If InStr(GProjectAbbreviation, "ACP") > 0 Then
            'MsgBox("ARXML Mapping file not supported for ACP2, ACP3 or ACP4 controller types.")
            Exit Sub
        End If

        If ProcessNewArxmlFile = False Then
            If MsgBox("Update ARXML Mapping File?", vbYesNo) = vbYes Then
                UpdateARXMLMappingFile = True
            End If
        End If

        If ProcessNewArxmlFile = True Or UpdateARXMLMappingFile = True Then

            HandleUserMessageLogging("GMRC", "Updating ARXML Mapping file...", DisplayMsgBox,, FlashMsgOn)
            Me.Cursor = Cursors.WaitCursor

            'FCM CHANGE - Added FCM condition
            'If GProjectAbbreviation = "LC" Or GProjectAbbreviation = "HC" Or GProjectAbbreviation = "FCM" Or GProjectAbbreviation = "FCM100" Then
            If GProjectAbbreviation <> "CSAV2" Then

                filename = My.Application.Info.DirectoryPath & "\" & GProjectAbbreviation & "_ARXML_Mapping.csv"
                fnum = FreeFile()
                FileOpen(fnum, filename, OpenMode.Input)

                Do While Not EOF(fnum)

                    textline = LineInput(fnum)
                    If Len(textline) > 0 Then
                        lineitems = Split(textline, ",")
                    End If

                Loop

                FileClose(fnum)

                fnum = FreeFile()
                FileOpen(fnum, filename, OpenMode.Append)

                If GProjectAbbreviation = "LC" Then
                    A2LFileName = Path.GetFileName(SaveLcA2LFilename)
                    softwareversionstring = "ASE34_LC-" & Mid(A2LFileName, 10, 2) & "." & Mid(A2LFileName, 12, 2) & "." & Mid(A2LFileName, 14, 3) & "." & Mid(A2LFileName, 17, 2)
                ElseIf GProjectAbbreviation = "HC" Then
                    A2LFileName = Path.GetFileName(SaveHcsA2LFilename)
                    softwareversionstring = "ASE37_HC-" & Mid(A2LFileName, 11, 2) & "." & Mid(A2LFileName, 13, 2) & "." & Mid(A2LFileName, 15, 3) & "." & Mid(A2LFileName, 18, 2)

                    'FCM CHANGE - Added condition for FCM
                ElseIf InStr(GProjectAbbreviation, "FCM") > 0 Then

                    'FCM_STA_ZF1_222215312.a2l - FCM standalone can come from either supplier...
                    'FCM_STA_VEO_232315503.a2l
                    'FCM_LCM_ZF1_222215306.a2l - FCM low and high can only come from ZF...
                    'FCM_LCH_ZF1_222215306.a2l
                    'FCM100_STA_ZF1_222215306.a2l - FCM100 standalone can come from either supplier...
                    'FCM100_LC_ZF1_232315403.a2l
                    'FCM100_LC_ZF1_232315403.a2l - FCM100 LCH can come from either supplier...
                    'FCM100_STA_VEO_222215306.a2l
                    'FCM100_LC_VEO_232315403.a2l - FCM100 LCM can come from either supplier...
                    'FCM100_LCH_VEO_232315403.a2l

                    'FIX FCM100

                    A2LFileName = Path.GetFileName(SaveFcmA2LFilename)

                    Dim tempstr() As String

                    tempstr = Split(A2LFileName, "_")

                    softwareversionstring = "FCM-" & Mid(tempstr(3), 1, 2) & "." & Mid(tempstr(3), 3, 2) & "." & Mid(tempstr(3), 5, 3) & "." & Mid(tempstr(3), 8, 2)

                    'If InStr(A2LFileName, "FCM100") > 0 Then
                    'If InStr(A2LFileName, "FCM100_LC") > 0 Then
                    'softwareversionstring = "FCM-" & Mid(A2LFileName, 15, 2) & "." & Mid(A2LFileName, 17, 2) & "." & Mid(A2LFileName, 19, 3) & "." & Mid(A2LFileName, 22, 2)
                    'Else
                    'softwareversionstring = "FCM-" & Mid(A2LFileName, 16, 2) & "." & Mid(A2LFileName, 18, 2) & "." & Mid(A2LFileName, 20, 3) & "." & Mid(A2LFileName, 23, 2)
                    'End If
                    'Else
                    'softwareversionstring = "FCM-" & Mid(A2LFileName, 13, 2) & "." & Mid(A2LFileName, 15, 2) & "." & Mid(A2LFileName, 17, 3) & "." & Mid(A2LFileName, 20, 2)
                    'End If
                Else
                    softwareversionstring = "UNDEFINED"
                End If

                'ASE37_HCS_222215400AS_quasi.a2l,,ASE37_HC-22.22.154.00,GB_ASR_EOCM_HCP1_22_22_153_MY21ODX.arxml,Global_B_Active_Safety_150R2_20Feb2020.vs3zip
                'ASE34_LC-21.21.150.05
                'ASE37_HC-22.22.154.00

                'We may need to use multiple ARXML Files, so we need to figure out which one to use here.  Currently only High Content may do this.
                'We will use HCP1 ARXML filename here...

                If UBound(SaveArxmlFilename) = 0 Then
                    newtextline = A2LFileName & ",," & softwareversionstring & "," & Path.GetFileName(SaveArxmlFilename(0)) & "," & lineitems(4)
                Else
                    For x = 0 To UBound(SaveArxmlFilename)
                        If InStr(SaveArxmlFilename(x), "HCP1") > 0 Then
                            newtextline = A2LFileName & ",," & softwareversionstring & "," & Path.GetFileName(SaveArxmlFilename(x)) & "," & lineitems(4)
                            Exit For
                        End If
                    Next
                End If

                PrintLine(fnum, newtextline)

                FileClose(fnum)

            End If

            UserStatusInfo.Hide()
            Me.Cursor = Cursors.Arrow

            HandleUserMessageLogging("GMRC", "Updating ARXML Mapping file complete.",, )

        End If

    End Sub

    Private Function CopyA2lFilesToA2lFolder() As Boolean
        ' Handles copying A2L files to the appropriate folder based on the project and software version.
        Dim A2lDirectoryName As String = ""
        Dim SaveA2lFileName() As String = Nothing
        Dim x As Integer
        Try
            HandleUserMessageLogging("GMRC", "Copy A2l Files To A2l Folder Called...",, )
            ' Validate software version and model year
            If InStr(GSoftwareVersion, "?") > 0 OrElse InStr(GModelYear, "?") > 0 Then
                HandleUserMessageLogging("GMRC", "Invalid SoftwareVersion or ModelYear detected. Exiting...", DisplayMsgBox, )
                Return False
            End If
            HandleUserMessageLogging("GMRC", "Copying A2L Files to A2L Folder...",,, FlashMsgOn)
            Me.Cursor = Cursors.WaitCursor
            ' Determine the A2L directory and files based on the project abbreviation
            Select Case GProjectAbbreviation
                Case "LC"
                    A2lDirectoryName = $"{My.Application.Info.DirectoryPath}\A2L\LowContent\{GSoftwareVersion} MY {GModelYear}{GSpecificArxml}"
                    ReDim SaveA2lFileName(0)
                    SaveA2lFileName(0) = SaveLcA2LFilename
                Case "HC"
                    A2lDirectoryName = $"{My.Application.Info.DirectoryPath}\A2L\HighContent\{GSoftwareVersion} MY {GModelYear}{GSpecificArxml}"
                    ReDim Preserve SaveA2lFileName(1)
                    SaveA2lFileName(0) = SaveHcsA2LFilename
                    SaveA2lFileName(1) = SaveHcfA2LFilename
                Case "CSAV2"
                    A2lDirectoryName = $"{My.Application.Info.DirectoryPath}\A2L\CSAV2\{GSoftwareVersion} MY {GModelYear}{GSpecificArxml}"
                Case "FCM", "FCM100"
                    A2lDirectoryName = $"{My.Application.Info.DirectoryPath}\A2L\{GProjectAbbreviation}\{FCMConfigName}\{GSoftwareVersion} MY {GModelYear}{GSpecificArxml}"
                    If MsgBox("Do you wish to copy the FCM A2L file and have it used when generating the enumerations.txt file?", vbYesNo) = vbYes Then
                        HandleUserMessageLogging("GMRC", "Copying FCM A2L file for enumerations.txt generation...",, )
                        ReDim SaveA2lFileName(0)
                        SaveA2lFileName(0) = SaveFcmA2LFilename
                    End If
                Case "ACP2"
                    A2lDirectoryName = $"{My.Application.Info.DirectoryPath}\A2L\ACP2\{GSoftwareVersion} MY {GModelYear}{GSpecificArxml}"
                    ReDim SaveA2lFileName(0)
                    SaveA2lFileName(0) = SaveAcp2A2LFilename
                Case "ACP3"
                    A2lDirectoryName = $"{My.Application.Info.DirectoryPath}\A2L\ACP3\{GSoftwareVersion} MY {GModelYear}{GSpecificArxml}"
                    ReDim SaveA2lFileName(0)
                    SaveA2lFileName(0) = SaveAcp3A2LFilename
                Case "ACP4"
                    A2lDirectoryName = $"{My.Application.Info.DirectoryPath}\A2L\ACP4\{GSoftwareVersion} MY {GModelYear}{GSpecificArxml}"
                    ReDim SaveA2lFileName(0)
                    SaveA2lFileName(0) = SaveAcp4A2LFilename
                Case Else
                    HandleUserMessageLogging("GMRC", "Unknown project abbreviation. Exiting...", DisplayMsgBox, )
                    Return False
            End Select
            ' Create the A2L directory if it does not exist
            If Not Directory.Exists(A2lDirectoryName) Then
                Directory.CreateDirectory(A2lDirectoryName)
            End If
            ' Copy A2L files to the directory
            If SaveA2lFileName IsNot Nothing Then
                For x = 0 To UBound(SaveA2lFileName)
                    FileCopy(SaveA2lFileName(x), $"{A2lDirectoryName}\{Path.GetFileName(SaveA2lFileName(x))}")
                Next
            End If
            ' Delete old A2L files from the main directory
            Dim dir As New DirectoryInfo($"{My.Application.Info.DirectoryPath}\A2L")
            For Each file In dir.GetFiles()
                file.Delete()
            Next
            ' Prompt the user to create a new enumerations file
            If Len(A2lDirectoryName) > 0 Then
                If MsgBox("Create new enumerations file now?", vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", "Creating new enumerations file...",, )
                    dir = New DirectoryInfo(A2lDirectoryName)
                    For Each file In dir.GetFiles()
                        FileCopy($"{file.DirectoryName}\{file.Name}", $"{My.Application.Info.DirectoryPath}\A2L\{file.Name}")
                    Next
                    Return True
                Else
                    HandleUserMessageLogging("GMRC", "No new enumerations file created.", DisplayMsgBox, )
                    Return False
                End If
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Error in CopyA2lFilesToA2lFolder: {ex.Message}", DisplayMsgBox, )
            Return False
        Finally
            UserStatusInfo.Hide()
            Me.Cursor = Cursors.Arrow
        End Try
        Return False
    End Function


    Private Sub HandleFlashAndDrive(ByVal mylistbox As ListBox)

        'Called from HandleImportSoftwareAndCals and from Retry Flashing button on FlashingStatus screen...
        'Handles flashing if requested and drive if requested...

        Dim SaveINCAExperiment As String
        Dim flash As Boolean
        Dim AtLeastOneControllerFlashed As Boolean
        Dim AtLeastOneControllerFlashFailed As Boolean
        Dim deviceObj As ExperimentDevice

        Dim ReturnStr As String = ""

        If Me.CheckBox2.Checked = False Then

            HandleUserMessageLogging("GMRC", "HandleFlashAndDrive Displaying Do you wish to flash the controller message...")
            'If MsgBox("Do you wish to flash the controller from the " & NewWorkspaceName & " workspace?", vbYesNo, "USER RESPONSE REQUIRED") = vbYes Then
            If MsgBox("Do you wish to flash the controller from the " & INCAWorkspace & " workspace?", vbYesNo, "USER RESPONSE REQUIRED") = vbYes Then
                flash = True
            End If

        Else
            flash = True
        End If

        If flash = True Then

            HandleUserMessageLogging("GMRC", "Preparing to flash. Please be patient...",,,, mylistbox)

            AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames

            SaveINCAExperiment = INCAExperiment
            INCAExperiment = "Empty Experiment"

            ReturnStr = MyIncaInterface.HandleWorkspace("", False)

            If ReturnStr = "True" Or InStr(ReturnStr, "ERROR:") = 0 Then

                INCAExperiment = SaveINCAExperiment

                For x = 0 To 5

                    If FlashParameters(x).FlashType <> "Flash_NONE" And FlashParameters(x).FlashType <> "Flash_NOCONNECT" And Len(FlashParameters(x).FlashType) > 0 Then

                        Me.Cursor = Cursors.WaitCursor
                        deviceObj = MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.GetDevice(FlashParameters(x).DeviceName)

                        If FlashController(FlashParameters(x).DeviceName, FlashParameters(x).FlashType, deviceObj, mylistbox) = True Then
                            HandleUserMessageLogging("GMRC", FlashParameters(x).DeviceName & " Controller " & FlashParameters(x).FlashType & " Flash Successful!!!",,, FlashMsgOn, mylistbox)
                            AtLeastOneControllerFlashed = True
                        Else
                            HandleUserMessageLogging("GMRC", FlashParameters(x).DeviceName & " Controller " & FlashParameters(x).FlashType & " Flash Failed!!!", DisplayMsgBox,,, mylistbox)
                            AtLeastOneControllerFlashFailed = True
                        End If
                        Me.Cursor = Cursors.Arrow

                    Else

                        If FlashParameters(x).FlashType = "Flash_NONE" Then
                            HandleUserMessageLogging("GMRC", FlashParameters(x).DeviceName & " checksums match workspace, No Flashing Required.",,, FlashMsgOn, mylistbox)

                        ElseIf FlashParameters(x).FlashType = "Flash_NOCONNECT" Then

                            HandleUserMessageLogging("GMRC", FlashParameters(x).DeviceName & " NOT CONNECTED. No Flashing Possible.",,, FlashMsgOn, mylistbox)
                            AtLeastOneControllerFlashFailed = True

                        End If

                    End If

                Next x

                MyIncaInterface.CloseExperiment()

                If AtLeastOneControllerFlashed = True And AtLeastOneControllerFlashFailed = False Then
                    HandleUserMessageLogging("GMRC", "Controller Flashing Complete.",,, FlashMsgOn, mylistbox)

                    CopyFlashInfoTofile()

                ElseIf AtLeastOneControllerFlashed = False And AtLeastOneControllerFlashFailed = False Then
                    HandleUserMessageLogging("GMRC", "Checksums matched, NO Flashing was required.",,, FlashMsgOn, mylistbox)

                    CopyFlashInfoTofile()

                ElseIf AtLeastOneControllerFlashFailed = True Then
                    HandleUserMessageLogging("GMRC", "Not all controllers were flashed.",,, FlashMsgOn, mylistbox)
                End If

            Else
                HandleUserMessageLogging("GMRC", "HandleFlashAndDrive: No Flashing attempted due to HandleWorkspace Error - " & ReturnStr, DisplayMsgBox,,, mylistbox)
                MyIncaInterface.CloseExperiment()
                Button3.Enabled = True
                Me.Cursor = Cursors.Arrow
                HandleUserMessageLogging("GMRC", "Exiting HandleFlashAndDrive...")
                MyHWC = Nothing
                Exit Sub
            End If

            Button3.Enabled = True

        Else
            Button3.Enabled = False
        End If

        GmResidentClient.Button1.Text = "Login as Demo (" & INCAWorkspace & ")"

        MyHWC = Nothing

        'Login as Demo and Drive check box on FlashingStatus screen...
        If Me.CheckBox3.Checked = True Then

            If AtLeastOneControllerFlashed = True And AtLeastOneControllerFlashFailed = False Then

                HandleUserMessageLogging("GMRC", "HandleFlashAndDrive Displaying BEFORE PRESSING OK Message...")
                If MsgBox("BEFORE PRESSING OK!!!  Please Key the vehicle Off, and wait at least two minutes before turning on ignition or entring Run Mode...", vbOKCancel) = vbOK Then
                    HandleUserMessageLogging("GMRC", "User Answered OK: Closing FlashingStatus window...")
                    DebugMode = True
                    InitForm.Drive()
                    Me.Close()
                End If
                HandleUserMessageLogging("GMRC", "User Answered Cancel")

            ElseIf AtLeastOneControllerFlashed = False And AtLeastOneControllerFlashFailed = False Then

                HandleUserMessageLogging("GMRC", "Checksums matched.  No controllers were flashed!  Continuing...", DisplayMsgBox)
                DebugMode = True
                InitForm.Drive()
                Me.Close()

            ElseIf AtLeastOneControllerFlashFailed = True Then

                HandleUserMessageLogging("GMRC", "Some controllers did not flash!  Please verify your hardware connections before continuing.", DisplayMsgBox)
                Me.Close()

            End If

        End If

        Me.Cursor = Cursors.Arrow
        HandleUserMessageLogging("GMRC", "Exiting HandleFlashAndDrive...")
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "Exit FlashingStatus form button pressed...")
        HandleUserMessageLogging("GMRC", " ")

        ListBox2.Visible = False

        Me.Close()

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        'This is the Import Button on the FlashingStatus screen.

        'Calls the HandleImportSoftwareAndCals routine.  This routine handles all aspects of importing software and calibrations

        Dim SaveGroupBox2EnabledState As Boolean

        SaveGroupBox2EnabledState = GroupBox2.Enabled

        GroupBox1.Enabled = False
        GroupBox2.Enabled = False

        Button1.Enabled = False
        Button2.Enabled = False

        ListBox2.Visible = False
        Button4.Enabled = False

        CheckBox2.Enabled = False

        HandleUserMessageLogging("GMRC", "FlashingStatus Import Button Pressed.",, )

        'Behavior of this routine is based on which radio button is selected which determines the initial
        'directory presented to the user from which to drill down to find the appropriate files.

        HandleImportSoftwareAndCals()

        Button4.Enabled = True

        Button1.Enabled = True
        Button2.Enabled = True

        CheckBox2.Enabled = True

        GroupBox1.Enabled = True
        GroupBox2.Enabled = SaveGroupBox2EnabledState


    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged

        'This is the Login as Demo and Drive check box on the FlashingStatus screen...

        If CheckBox3.Checked = True Then
            HandleUserMessageLogging("GMRC", "Login as Demo and Drive checked...")
            If CheckBox2.Checked = False Then
                MsgBox("You must select Flash Controllers from new Workspace to use this feature.")
                CheckBox3.Checked = False
                Exit Sub
            End If
        Else
            HandleUserMessageLogging("GMRC", "Login as Demo and Drive UNchecked...")
        End If

    End Sub

    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton2.CheckedChanged

        'Here we will inhibit the selection of RadioButton2 if necessary based on the current ProjectName...

        'FCM CHANGE - Added inhibiting based on FCM ProjectName and FCM SubProjectNames

        If RadioButton2.Checked = True Then

            GroupBox2.Enabled = True
            RadioButton7.Checked = True

            HandleUserMessageLogging("GMRC", "Import A2L / PTP and Create Workspace (Low Content or FCM STA Vehicles) radio button checked...")

            Select Case ProjectName
                Case "CSAV2"
                    RadioButton1.Checked = True
                    RadioButton2.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)
                Case "HighContent"
                    RadioButton4.Checked = True
                    RadioButton2.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)

                Case "ACP3", "LowContent"

                    If InStr(FCMConfigName, "FCM") > 0 Then
                        RadioButton4.Checked = True
                        RadioButton2.Checked = False
                        HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles using an instrumented FCM controller.", DisplayMsgBox)
                    End If

            End Select

        End If
    End Sub

    Private Sub FlashingStatus_Load(sender As Object, e As EventArgs) Handles Me.Load

        'Here we pre-select the correct radiobutton based on the current ProjectName...

        'RadioButton2 is Import A2L / PTP and Create Workspace (LowContent Vehicles Only)

        'FCM CHANGE - Added FCM Case...

        Dim mytooltip As New ToolTip

        mytooltip.SetToolTip(RadioButton7, "User Selects Folder, CLEVIR Selects Files based on Vehicle Number")
        mytooltip.SetToolTip(RadioButton8, "User Selects Folder, Folder Contents are displayed, User Indicates if CLEVIR should continue with Automatic Selections")
        mytooltip.SetToolTip(RadioButton6, "User Selects A2l and PTP files in selected folder, one at a time.")


        If ClevirAdministrator = True Then
            CheckBox1.Visible = True 'This is the Configure For New Software Version checkbox.  This is for CLEVIR administrator only...
        End If

        Select Case ProjectName
            Case "LowContent", "ACP3"
                If InStr(FCMConfigName, "FCM") > 0 Then
                    RadioButton4.Checked = True
                Else
                    RadioButton2.Checked = True
                End If

            Case "HighContent"
                If InStr(FCMConfigName, "FCM") > 0 Then
                    RadioButton5.Checked = True
                Else
                    RadioButton4.Checked = True
                End If
            Case "CSAV2"
                RadioButton1.Checked = True
            Case "FCM"
                RadioButton2.Checked = True
            Case "FCM100"
                RadioButton2.Checked = True
            Case "ACP2"
                RadioButton2.Checked = True
            Case "ACP4"
                RadioButton2.Checked = True
            Case Else
                RadioButton1.Checked = True
        End Select

        'Override File Selection Method setting, always Manual for PATAC because they will not be using
        'the network drive and will be copying a2l and ptp files into C:\CLEVIR_INCA_7_3\INCAProjects folder...
        If PATAC = True Then
            RadioButton6.Checked = True
            GroupBox2.Enabled = False
        End If

    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
        If CheckBox2.Checked = True Then
            HandleUserMessageLogging("GMRC", "Flash Controllers from new workspace checked...")
            CheckBox3.Enabled = True
        Else
            HandleUserMessageLogging("GMRC", "Flash Controllers from new workspace UNchecked...")
            CheckBox3.Enabled = False
            CheckBox3.Checked = False
        End If
    End Sub

    Private Sub RadioButton4_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton4.CheckedChanged

        'Here we will inhibit the selection of RadioButton4 if necessary based on the current ProjectName...

        'FCM CHANGE - Added inhibiting based on FCM ProjectName and FCM SubProjectNames

        If RadioButton4.Checked = True Then

            GroupBox2.Enabled = True
            RadioButton7.Checked = True

            HandleUserMessageLogging("GMRC", "Import A2L / PTP for 2 processors and Create Workspace (High Content or FCM LC MID Vehicles) radio button checked...")

            Select Case ProjectName
                Case "CSAV2"
                    RadioButton1.Checked = True
                    RadioButton4.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)
                Case "LowContent", "ACP3"

                    If InStr(FCMConfigName, "FCM") = 0 Then

                        RadioButton2.Checked = True
                        RadioButton4.Checked = False
                        HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles without instrumented FCM.", DisplayMsgBox)

                    End If

                Case "HighContent"

                    If InStr(FCMConfigName, "FCM") > 0 Then

                        RadioButton5.Checked = True
                        RadioButton4.Checked = False
                        HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles with instrumented FCM.", DisplayMsgBox)

                    End If

                Case "FCM", "FCM100", "ACP2", "ACP4"

                    RadioButton2.Checked = True
                    RadioButton4.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)

            End Select

        End If


    End Sub

    Private Sub RadioButton3_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton3.CheckedChanged

        If RadioButton3.Checked = True Then
            HandleUserMessageLogging("GMRC", "Import Workspace (Workspace matches vehicle instrumentation hardware) radio button checked...")
            RadioButton6.Checked = True
            GroupBox2.Enabled = False
        End If

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        'This is the Retry Flashing button on the FlashingStatus screen.
        'This button is enabled after a failed flashing attempt to allow the user to re-try flashing
        'with the same workspace as was used in the failed attempt...

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "Flashing Status Retry Flashing button pressed...",, )
        HandleUserMessageLogging("GMRC", " ")

        ListBox2.Visible = False

        HandleFlashAndDrive(ListBox1)
    End Sub

    Private Sub FlashingStatus_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed

        InitForm.Show()
        InitForm.ShowInTaskbar = True
        HandleUserMessageLogging("GMRC", "FlashingStatus form Closed...")
    End Sub

    Private Sub RadioButton1_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton1.CheckedChanged
        If RadioButton1.Checked = True And Loading = False Then
            HandleUserMessageLogging("GMRC", "Import and Convert Workspace radio button selected...")

            Select Case ProjectName
                Case "LowContent", "ACP3"
                    If InStr(FCMConfigName, "FCM") = 0 Then
                        RadioButton2.Checked = True
                    Else
                        RadioButton3.Checked = True
                    End If

                    RadioButton1.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)
                Case "FCM", "FCM100", "ACP2", "ACP4"

                    RadioButton2.Checked = True
                    RadioButton1.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)
                Case "HighContent"

                    If InStr(FCMConfigName, "FCM") = 0 Then
                        RadioButton4.Checked = True
                    Else
                        RadioButton5.Checked = True
                    End If

                    RadioButton1.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)

                Case Else
                    RadioButton6.Checked = True
                    GroupBox2.Enabled = False

            End Select
        End If
    End Sub

    Private Sub FlashingStatus_Shown(sender As Object, e As EventArgs) Handles Me.Shown

    End Sub

    Private Sub FlashingStatus_Enter(sender As Object, e As EventArgs) Handles Me.Enter

    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged

        '4.	Initiate creation of new workspace from new software version (CLEVIR) by checking this box...
        Dim tempstr As String

        If CheckBox1.Checked = True Then

            If InStr(FCMConfigName, "FCM") > 0 And InStr(ProjectName, "FCM") = 0 Then
                tempstr = "You are about to create CLEVIR configuration files for a new model year and/or software version for " & ProjectName & " with an instrumented FCM, is this correct?"
            Else
                tempstr = "You are about to create CLEVIR configuration files for a new model year and/or software version for " & ProjectName & " is this correct?"
            End If

            If MsgBox(tempstr, vbYesNo) = vbNo Then
                HandleUserMessageLogging("GMRC", tempstr & " No.",, )
                CheckBox1.Checked = False
            Else
                HandleUserMessageLogging("GMRC", tempstr & " Yes.",, )
                If ProjectName <> "CSAV2" Then
                    If CheckARXMLFileDirectory() = False Then
                        CheckBox1.Checked = False
                    End If
                    'Else
                    '    HandleUserMessageLogging("GMRC", "Creating new CSAV2 files assumes no changes to .dbc files used for CAN Channel definition.  Should changes be required, this must be done manually...", DisplayMsgBox, )
                End If

            End If

        End If
        ConfigureForNewSoftwareVersion = CheckBox1.Checked
    End Sub

    Private Sub RadioButton5_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton5.CheckedChanged

        'Here we will inhibit the selection of RadioButton5 if necessary based on the current ProjectName...

        'FCM CHANGE - Added this button...

        If RadioButton5.Checked = True Then

            GroupBox2.Enabled = True
            RadioButton7.Checked = True

            HandleUserMessageLogging("GMRC", "Import A2L / PTP for 3 processors and Create Workspace (FCM LC HIGH Vehicles) radio button checked...")

            Select Case ProjectName
                Case "CSAV2"
                    RadioButton1.Checked = True
                    RadioButton5.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)
                Case "LowContent", "ACP3"
                    If InStr(FCMConfigName, "FCM") = 0 Then
                        RadioButton2.Checked = True
                    Else
                        RadioButton3.Checked = True
                    End If

                    RadioButton5.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)
                Case "FCM", "FCM100", "ACP2", "ACP4"

                    RadioButton2.Checked = True
                    RadioButton5.Checked = False
                    HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles.", DisplayMsgBox)
                Case "HighContent"

                    If InStr(FCMConfigName, "FCM") = 0 Then
                        RadioButton4.Checked = True
                        RadioButton5.Checked = False
                        HandleUserMessageLogging("GMRC", "This selection not valid for " & ProjectName & " vehicles without FCM instrumented controller.", DisplayMsgBox)
                    End If

            End Select

        End If
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        'This is the Select Existing Workspace in INCA to Flash button...

        'Here we get all available workspaces (in the CLEVIR Setup\Workspaces folder only) and display these
        'workspace names in the main listbox on the FlashingStatus form.

        Dim returnstr As String = ""

        HandleUserMessageLogging("GMRC", "Select Existing Workspace in INCA to Flash button pressed...",, )

        ListBox2.Visible = True
        CheckBox2.Checked = False
        ListBox2.Items.Clear()

        returnstr = MyIncaInterface.ConnectToInca()

        If returnstr = "True" Then

            AvailableWorkspaces = MyIncaInterface.GetAvailableWorkspaces()

            For x = 0 To UBound(AvailableWorkspaces)

                If CheckForValidParameters(AvailableWorkspaces(x)) = True Then
                    ListBox2.Items.Add(AvailableWorkspaces(x))
                End If

            Next

        Else
            HandleUserMessageLogging("GMRC", "Flash Existing Workspace Button: ConnectToInca returned - " & returnstr, DisplayMsgBox, )
        End If

    End Sub

    Private Sub ListBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedIndexChanged

    End Sub

    Private Sub ListBox2_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedValueChanged

        If Len(ListBox2.SelectedItem) > 0 Then

            If InStr(ListBox2.SelectedItem.ToString, "No Workspaces found") = 0 Then

                If Mid(ListBox2.SelectedItem.ToString, 1, 1) <> "_" Then
                    INCAWorkspace = Trim(ListBox2.SelectedItem.ToString)

                    HandleUserMessageLogging("GMRC", INCAWorkspace & " Selected for Flashing...",, )

                    If VerifyConfigFiles("FlashingStatusScreen") = True Then

                        ListBox2.Visible = False
                        Button4.Enabled = False
                        Button2.Enabled = False

                        Me.Activate()

                        HandleFlashAndDrive(ListBox1)

                        ListBox2.Visible = True
                        Button2.Enabled = True
                    Else
                        Button3.Enabled = False
                        ListBox2.Visible = True
                    End If

                Else
                    MsgBox("Please select a valid Workspace")
                End If

            End If

        End If

        Button4.Enabled = True

    End Sub

    Private Sub RadioButton7_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton7.CheckedChanged

        If RadioButton7.Checked = True Then
            FileSelectionMethod = "Automatic"
            HandleUserMessageLogging("GMRC", "FlashingStatus: FileSelectionMethod = " & FileSelectionMethod,, )
        End If

        'ManualSelect = Not RadioButton7.Checked And Not RadioButton8.Checked

        'If ManualSelect = True Then
        'HandleUserMessageLogging("GMRC", "FlashingStatus: ManualSelect = " & ManualSelect,, )
        'End If

    End Sub

    Private Sub RadioButton6_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton6.CheckedChanged

        If RadioButton6.Checked = True Then
            FileSelectionMethod = "Manual"
            HandleUserMessageLogging("GMRC", "FlashingStatus: FileSelectionMethod = " & FileSelectionMethod,, )
        End If

    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click

        ListBox2.Visible = False

        Me.Close()
        InitForm.Drive()

    End Sub

    Private Sub RadioButton8_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton8.CheckedChanged

        If RadioButton8.Checked = True Then
            FileSelectionMethod = "Semi-Automatic"
            HandleUserMessageLogging("GMRC", "FlashingStatus: FileSelectionMethod = " & FileSelectionMethod,, )
        End If

        'ManualSelect = Not RadioButton7.Checked And Not RadioButton8.Checked
    End Sub
End Class