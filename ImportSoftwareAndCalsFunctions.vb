Option Strict Off
Option Explicit On

Imports System.Diagnostics
Imports System.IO
Imports de.etas.cebra.toolAPI.Common
Imports de.etas.cebra.toolAPI.Inca
'Imports SevenZip


Module ImportSoftwareAndCalsFunctions

    'This module contains functions associated with importing software and calibrations and creating custom workspace.  This is
    'a GlobalCommonModule and is used by CLEVIR and FLASHOMATIC...  

    Public A2LFileName As String

    Public NumberOfCamerasInVehicle As Integer = 5
    Public CameraNames() As String = {"FRONT", "Rear", "Left", "Right", "Driver", "NA", "NA", "NA"}

    Public GModelYear As String = "??"
    Public GSoftwareVersion As String = "???"
    Public GSpecificArxml As String = ""
    Public GWorkspaceIsForRtk As Boolean

    Public GProjectAbbreviation As String
    Public GSaveIncaWorkspaceTemplateName As String
    Public WorkspaceNameSuffix As String
    Public GSaveWorkspaceNameSuffix As String

    Public InitialDirectory As String

    Public NewWorkspaceName As String
    Public SourceFile As String

    Public ProjectName As String

    Public SaveHcfA2LFilename As String
    Public SaveHcsA2LFilename As String
    Public SaveLcA2LFilename As String
    Public SaveFcmA2LFilename As String
    Public SaveAcp2A2LFilename As String
    Public SaveAcp3A2LFilename As String
    Public SaveAcp4A2LFilename As String
    Public SaveArxmlFilename() As String

    Public ConfigureForNewSoftwareVersion As Boolean
    Public ProcessNewArxmlFile As Boolean
    Public ProcessNewDbcFiles As Boolean

    Public ReadOnly SaveProjectFiles() As String = {"N/A", "N/A", "N/A", "N/A", "N/A", "N/A"} 'Added two more for FCM variants...

    Public StatusUpdatesOn As Boolean
    Public CheckForNewerSignalListComplete As Boolean

    'Public ManualSelect As Boolean
    Public FileSelectionMethod As String

    Sub CheckForNewerSignalListOld(Optional ByVal selectOption As Boolean = False)

        'Called from VerifyCLEVIRConfiguration which is called from HandleLogin.  Also called from Save and Continue button on
        'the softwareversionselect form.

        'There are two routines currently that do similar things, CheckForNewerSignalListOLD and CheckForNewerSignalListNEW. 
        'CheckForNewerSignalListOLD is called if CLEVIR is unable to determine both model year and software version from the
        'workspace filename...

        'Checks to see if there is a more up to date Signal List .xlsx file on the Q drive (for the latest major rev).
        'if it finds a Signal List .xlsx file with a newer date, it copies it into the GmResidentClient\SignalLists directory.
        'Also looks for the associated INCA experiment (.exp file) and imports it into INCA. Also updates the current active
        'CLEVIR configuration file with the newest signal list name and experiment name.

        Dim dirname As String
        Dim latestVersionOnQDrive As Integer

        Dim latestVersionOnPc As Integer

        Dim dir As DirectoryInfo
        Dim dir2 As DirectoryInfo
        Dim files2 As FileInfo()
        Dim files As FileInfo()
        Dim dirs As DirectoryInfo()
        Dim x As Integer
        Dim y As Integer
        Dim z As Integer

        Dim fileFound As Boolean = False
        Dim versionFound As Boolean = False

        Dim found As Boolean = False

        Dim saveLatestVersion As Integer

        Dim saveFileName As String = ""

        Dim latestWriteTimeOnPc As Date

        Dim csvFileInFolder As Boolean

        Dim answer As MsgBoxResult = vbNo

        Try

            HandleUserMessageLogging("GMRC", "CheckForNewerSignalListOLD Called.   CheckForNewerSignalListComplete = " & CheckForNewerSignalListComplete)

            If CheckForNewerSignalListComplete = True Or ConfigureForNewSoftwareVersion = True Then
                Exit Sub
            End If

            If Debugger.IsAttached Then
                If MsgBox("Check For newer signal list And experiment?", vbYesNo) = vbNo Then
                    CheckForNewerSignalListComplete = True
                    Exit Sub
                End If
            End If

            'Get most recent file write date and time from SignalLists directory on PC...
            If Not System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\SignalLists") Then
                System.IO.Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\SignalLists")
            End If

            dir = New DirectoryInfo(My.Application.Info.DirectoryPath & "\SignalLists")

            files = dir.GetFiles

            For x = 0 To UBound(files)

                If InStr(files(x).Name, "SAVE") = 0 And InStr(files(x).Name, "~") = 0 Then
                    If System.IO.File.GetLastWriteTime(files(x).FullName) > latestWriteTimeOnPc Then
                        latestWriteTimeOnPc = System.IO.File.GetLastWriteTime(files(x).FullName)
                    End If
                End If

            Next

            'NETWORK DRIVE MAPPING 5.6.2

            If UsingFlashDrive = True Then
                dirname = NetworkDriveLetter & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath
            Else
                dirname = NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath
            End If

            If System.IO.Directory.Exists(dirname) Then

                dir = New DirectoryInfo(dirname)
                dirs = dir.GetDirectories

                'Determine latest version folder on Q drive based on CLEVIRFilesPath...
                For x = 0 To UBound(dirs)

                    If Val(Mid(dirs(x).Name, 1, 3)) > latestVersionOnQDrive Then
                        latestVersionOnQDrive = Val(Mid(dirs(x).Name, 1, 3))
                    End If

                Next

                'Determine file with the highest version number in SignalLists directory...

                dir = New DirectoryInfo(My.Application.Info.DirectoryPath & "\SignalLists")
                files = dir.GetFiles

                For x = 0 To UBound(files)

                    If Val(Mid(files(x).Name, 1, 3)) > latestVersionOnPc Then
                        latestVersionOnPc = Val(Mid(files(x).Name, 1, 3))
                    End If

                Next

                If latestVersionOnPc = 0 Then
                    latestVersionOnPc = 145
                End If

                Do While latestVersionOnPc <= latestVersionOnQDrive

                    If latestVersionOnPc < latestVersionOnQDrive Then

                        saveLatestVersion = latestVersionOnQDrive

                    Else
                        saveLatestVersion = 0
                    End If

                    HandleUserMessageLogging("GMRC", "Checking " & NetworkDriveLetter & " Drive For Updated Experiment And Signal List For Software Version " & latestVersionOnPc & "...",,, FlashMsg1Sec)

                    For x = 0 To UBound(dirs)

                        'here we will be in a Q drive subfolder which matches the LatestVersion on the PC

                        If Val(Mid(dirs(x).Name, 1, 3)) = latestVersionOnPc Then 'we are looking in the LatestVersion versionnumber folder

                            files = dirs(x).GetFiles

                            csvFileInFolder = False

                            For y = 0 To UBound(files)

                                If (InStr(files(y).Name, ".xlsx") > 0 Or InStr(files(y).Name, ".csv") > 0) And InStr(files(y).Name, "~") = 0 Then

                                    If System.IO.File.GetLastWriteTime(files(y).FullName) > latestWriteTimeOnPc Then

                                        If InStr(files(y).Name, ".csv") > 0 Then
                                            csvFileInFolder = True
                                        End If

                                        HandleUserMessageLogging("GMRC", "New Signal List Found For version " & Mid(files(y).Name, 1, 3) & ", Copying...",,, FlashMsg1Sec)

                                        HandleUserMessageLogging("GMRC", "CheckForNewerSignalListOLD: Copying New Signal List " & files(y).FullName & " to " & My.Application.Info.DirectoryPath & "\SignalLists\" & files(y).Name)
                                        FileCopy(files(y).FullName, My.Application.Info.DirectoryPath & "\SignalLists\" & files(y).Name)

                                    End If

                                End If

                            Next

                            For y = 0 To UBound(files)

                                If InStr(files(y).Name, ".exp") > 0 Then

                                    If Not System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\Experiments") Then
                                        System.IO.Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\Experiments")
                                    End If

                                    dir2 = New DirectoryInfo(My.Application.Info.DirectoryPath & "\Experiments")

                                    files2 = dir2.GetFiles

                                    For z = 0 To UBound(files2)

                                        If files2(z).Name = files(y).Name Then
                                            If System.IO.File.GetLastWriteTime(files2(z).FullName) = System.IO.File.GetLastWriteTime(files(y).FullName) Then
                                                fileFound = True
                                                Exit For
                                            End If

                                        End If

                                    Next

                                    'We will copy the experiment to the PC if the file write time of the experiment file is not equal, or if the
                                    'experiment file is not found on the PC.

                                    If fileFound = False Then

                                        HandleUserMessageLogging("GMRC", "Copying Experiment to " & My.Application.Info.DirectoryPath & "\Experiments Directory...",,, FlashMsg1Sec)

                                        FileCopy(files(y).FullName, dir2.FullName & "\" & files(y).Name)

                                        found = True
                                        HandleUserMessageLogging("GMRC", "Importing Experiment into INCA...",,, FlashMsg1Sec)

                                        UpdateINCAWithLatestExperiment(dir2.FullName & "\" & files(y).Name, csvFileInFolder)

                                    Else
                                        fileFound = False
                                    End If

                                End If

                            Next

                        End If

                    Next

                    If saveLatestVersion > 0 Then
                        latestVersionOnPc += 1
                        answer = vbNo
                        Do While answer = vbNo And latestVersionOnPc <= latestVersionOnQDrive
                            answer = MsgBox("Update files to software version " & latestVersionOnPc & "?", vbYesNo)
                            If answer = vbNo Then
                                latestVersionOnPc += 1
                            End If
                        Loop

                    Else
                        Exit Do
                    End If

                Loop

            End If

            If found = False Then

                HandleUserMessageLogging("GMRC", "No Updates Found...",,, FlashMsg1Sec)

                'OnVehicleScreen.Refresh()

            End If

            CheckForNewerSignalListComplete = True

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "CheckForNewerSignalListOLD " & ex.Message)

        End Try

    End Sub

    Sub CheckForNewerSignalListNew(Optional ByRef mismatch As Boolean = False)

        'Called from VerifyCLEVIRConfiguration which is called from HandleLogin.  

        'In FLASHOMATIC, there are two routines currently that do similar things, CheckForNewerSignalListOLD and CheckForNewerSignalListNEW. 
        'CheckForNewerSignalListNEW is called if CLEVIR IS able to determine both model year and software version from the
        'workspace filename. The implementation is different in CLEVIR which only uses CheckForNewerSignalListNEW...

        'Need to reinvestigate this and make FLASHOMATIC behavior the same as CLEVIR here if possible...

        'Checks to see if there is a more up to date Signal List .xlsx file on the Q drive (for the latest major rev).
        'if it finds a Signal List .xlsx file with a newer date, it copies it into the GmResidentClient\SignalLists directory.
        'Also looks for the associated INCA experiment (.exp file) and imports it into INCA. Also updates the current active
        'CLEVIR configuration file with the newest signal list name and experiment name.

        Dim dirname As String
        Dim fileFound As Boolean = False
        Dim versionFound As Boolean = False
        Dim found As Boolean = False
        Dim saveFileName As String = ""
        Dim latestWriteTimeOnPc As Date
        Dim csvFileInFolder As Boolean
        Dim answer As MsgBoxResult = vbNo
        Dim saveWriteTime As Date = Nothing
        Dim saveExpFileNameWithPath As String = ""
        Dim saveExpFileName As String = ""

        Dim dir As DirectoryInfo
        Dim files As FileInfo()
        Dim dirs As DirectoryInfo()
        Dim x As Integer
        Dim y As Integer

        Dim saveLatestSignalListFileName As String = ""

        Try

            HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW Called.   CheckForNewerSignalListComplete = " & CheckForNewerSignalListComplete)

            'If we have already checked, or if we are configuring for a new software version, we can bypass this...
            If CheckForNewerSignalListComplete = True Or ConfigureForNewSoftwareVersion = True Then
                Exit Sub
            End If

            'If running out of the debugger, we will allow user to decide if they want to check for newer files...
            If Debugger.IsAttached Then
                If MsgBox("Check for newer signal list And experiment?", vbYesNo) = vbNo Then
                    CheckForNewerSignalListComplete = True
                    Exit Sub
                End If
            End If

            HandleUserMessageLogging("GMRC", "Checking Share Drive for Updated Signal List And Experiment...",,, FlashMsgOn)

            'Get most recent file write date and time for the applicable signal list from SignalLists directory on the PC...
            'Assumption here is that the LastWriteTimeOnPC for the applicable signal list will be similar to that of the corresponding
            'experiment since they are created by the CLEVIR administrator at about the same time, so we are only checking the latest 
            'signal List update date and time, and do not also have to check the experiment in the experiments directory...

            If Not System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\SignalLists") Then
                System.IO.Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\SignalLists")
            End If

            dir = New DirectoryInfo(My.Application.Info.DirectoryPath & "\SignalLists")

            files = dir.GetFiles

            Dim signalListType As String

            If GProjectAbbreviation <> "CSAV2" Then
                signalListType = GProjectAbbreviation
            Else
                signalListType = "US"
            End If

            For x = 0 To UBound(files)

                If GWorkspaceIsForRtk = False Then

                    If (InStr(files(x).Name, "SAVE") = 0 And InStr(files(x).Name, "~") = 0) And
                            ((InStr(files(x).Name, "MY" & GModelYear) > 0) And (InStr(files(x).Name, signalListType) > 0) And (InStr(files(x).Name, "RTK") = 0) And
                            (Mid(files(x).Name, 1, 3) = GSoftwareVersion) And ((Len(GSpecificArxml) = 0 And InStr(files(x).Name, "XML") = 0) Or (Len(GSpecificArxml) > 0 And InStr(files(x).Name, GSpecificArxml) > 0))) Then
                        If System.IO.File.GetLastWriteTime(files(x).FullName) > latestWriteTimeOnPc Then
                            latestWriteTimeOnPc = System.IO.File.GetLastWriteTime(files(x).FullName)
                            saveLatestSignalListFileName = files(x).FullName
                        End If
                    End If

                Else

                    If (InStr(files(x).Name, "SAVE") = 0 And InStr(files(x).Name, "~") = 0) And
                            ((InStr(files(x).Name, "MY" & GModelYear) > 0) And (InStr(files(x).Name, signalListType) > 0) And (InStr(files(x).Name, "RTK") > 0) And
                            (Mid(files(x).Name, 1, 3) = GSoftwareVersion) And ((Len(GSpecificArxml) = 0 And InStr(files(x).Name, "XML") = 0) Or (Len(GSpecificArxml) > 0 And InStr(files(x).Name, GSpecificArxml) > 0))) Then
                        If System.IO.File.GetLastWriteTime(files(x).FullName) > latestWriteTimeOnPc Then
                            latestWriteTimeOnPc = System.IO.File.GetLastWriteTime(files(x).FullName)
                            saveLatestSignalListFileName = files(x).FullName
                        End If
                    End If

                End If

            Next

            'Here we need to determine where to look for updated files, we will either be looking on the share drive, or on the flash drive
            'if it is in play...
            If UsingFlashDrive = True Then
                dirname = NetworkDriveLetter & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath
            Else
                dirname = NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath

                If NetworkDrivePermission = False Then
                    HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW: Could not access " & NetworkDriveMapping & CLEVIRBaseDir & ". Exiting...")
                    Exit Sub
                End If

            End If

            'CKEVIRFilesPath determines where we will start looking.  This is based on the information in the VehicleConfigurations.csv file that corresponds to
            'the selected vehicle Number.  Example below for LowContent vehicle...

            'dirname = "\\Nam.corp.gm.com\tcws-dfs\project\CSV\CSAV2\CSAV2 Tools\CLEVIR\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\Current\LowContent"

            'IF the directory dirname exists, we will look in all of the sub-directories for the applicable files.  
            If System.IO.Directory.Exists(dirname) Then

                dir = New DirectoryInfo(dirname)

                'Subdirectories are software version subdirectories such as 163, 164, 165 etc....
                dirs = dir.GetDirectories

                For x = 0 To UBound(dirs)

                    If IsNumeric(GSoftwareVersion) Then

                        'We use the software version that has been extracted from the current workspace name, then find the
                        'corresponding subdirectory for that version...
                        If Val(Mid(dirs(x).Name, 1, 3)) = GSoftwareVersion Then

                            'If there is a specific XML version indicated in the workspace name, for example, MY23 with 22XML designation
                            'we will need to go one more level deep to find the files...
                            If Len(GSpecificArxml) > 0 Then
                                dirname = dirs(x).FullName & "\" & GSoftwareVersion & "_" & "MY" & GModelYear & GSpecificArxml

                                If Directory.Exists(dirname) Then

                                    dir = New DirectoryInfo(dirname)
                                    files = dir.GetFiles
                                Else
                                    found = False
                                    Exit For
                                End If

                            Else 'No XML designation so we just read the files from the base software version directory corresponding to the
                                'vehicle type...
                                files = dirs(x).GetFiles
                            End If

                            csvFileInFolder = False

                            'Go through all of the files in the directory and look for a match for the signal list names based on the information
                            'extracted from the workspace name...

                            If GWorkspaceIsForRtk = False Then

                                For y = 0 To UBound(files)

                                    If (InStr(files(y).Name, ".xlsx") > 0 Or InStr(files(y).Name, ".csv") > 0) And (InStr(files(y).Name, "~") = 0) And (InStr(files(y).Name, "RTK") = 0) And
                                       ((InStr(files(y).Name, "MY" & GModelYear) > 0) And
                                       (Mid(files(y).Name, 1, 3) = GSoftwareVersion) And (Len(GSpecificArxml) = 0 Or InStr(files(y).Name, GSpecificArxml) > 0)) Then

                                        'If a match is found and if the file is newer than the newest corresponding file on the PC, copy it.  This will copy both the
                                        'csv and the .xlsx signal list files if newer...
                                        If System.IO.File.GetLastWriteTime(files(y).FullName) > latestWriteTimeOnPc Then

                                            If InStr(files(y).Name, ".csv") > 0 Then
                                                csvFileInFolder = True
                                            End If

                                            HandleUserMessageLogging("GMRC", "New Signal List Found for version " & Mid(files(y).Name, 1, 3) & ", Copying...",,, FlashMsg1Sec)

                                            HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW: Copying New Signal List " & files(y).FullName & " to " & My.Application.Info.DirectoryPath & "\SignalLists\" & files(y).Name)
                                            FileCopy(files(y).FullName, My.Application.Info.DirectoryPath & "\SignalLists\" & files(y).Name)
                                            HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW: Copy Complete.",,, FlashMsg1Sec)
                                        End If

                                    End If

                                Next

                                'Here we are grabbing the information for the corresponding experiment (.exp) file...
                                For y = 0 To UBound(files)
                                    If (InStr(files(y).Name, ".exp") > 0) And (InStr(files(y).Name, "RTK") = 0) And
                                   ((InStr(files(y).Name, "MY" & GModelYear) > 0) And
                                   (Mid(files(y).Name, 1, 3) = GSoftwareVersion) And (Len(GSpecificArxml) = 0 Or InStr(files(y).Name, GSpecificArxml) > 0)) Then

                                        If Not saveWriteTime = Nothing Then
                                            If System.IO.File.GetLastWriteTime(files(y).FullName) > saveWriteTime Then
                                                saveWriteTime = System.IO.File.GetLastWriteTime(files(y).FullName)
                                                saveExpFileNameWithPath = files(y).FullName
                                                saveExpFileName = files(y).Name
                                            End If
                                        Else
                                            saveWriteTime = System.IO.File.GetLastWriteTime(files(y).FullName)
                                            saveExpFileNameWithPath = files(y).FullName
                                            saveExpFileName = files(y).Name
                                        End If

                                    End If
                                Next

                            Else 'GWorkspaceIsForRtk = True...

                                For y = 0 To UBound(files)

                                    If (InStr(files(y).Name, ".xlsx") > 0 Or InStr(files(y).Name, ".csv") > 0) And (InStr(files(y).Name, "~") = 0) And
                                       ((InStr(files(y).Name, "MY" & GModelYear) > 0) And (InStr(files(y).Name, "RTK") > 0) And
                                       (Mid(files(y).Name, 1, 3) = GSoftwareVersion) And (Len(GSpecificArxml) = 0 Or InStr(files(y).Name, GSpecificArxml) > 0)) Then

                                        'If a match is found and if the file is newer than the newest corresponding file on the PC, copy it.  This will copy both the
                                        'csv and the .xlsx signal list files if newer...
                                        If System.IO.File.GetLastWriteTime(files(y).FullName) > latestWriteTimeOnPc Then

                                            If InStr(files(y).Name, ".csv") > 0 Then
                                                csvFileInFolder = True
                                            End If

                                            HandleUserMessageLogging("GMRC", "New Signal List Found for version " & Mid(files(y).Name, 1, 3) & ", Copying...",,, FlashMsg1Sec)

                                            HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW: Copying New Signal List " & files(y).FullName & " to " & My.Application.Info.DirectoryPath & "\SignalLists\" & files(y).Name)
                                            FileCopy(files(y).FullName, My.Application.Info.DirectoryPath & "\SignalLists\" & files(y).Name)
                                            HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW: Copy Complete.",,, FlashMsg1Sec)
                                        End If

                                    End If

                                Next

                                'Here we are grabbing the information for the corresponding experiment (.exp) file...
                                For y = 0 To UBound(files)
                                    If (InStr(files(y).Name, ".exp") > 0) And
                                   ((InStr(files(y).Name, "MY" & GModelYear) > 0) And (InStr(files(y).Name, "RTK") > 0) And
                                   (Mid(files(y).Name, 1, 3) = GSoftwareVersion) And (Len(GSpecificArxml) = 0 Or InStr(files(y).Name, GSpecificArxml) > 0)) Then

                                        If Not saveWriteTime = Nothing Then
                                            If System.IO.File.GetLastWriteTime(files(y).FullName) > saveWriteTime Then
                                                saveWriteTime = System.IO.File.GetLastWriteTime(files(y).FullName)
                                                saveExpFileNameWithPath = files(y).FullName
                                                saveExpFileName = files(y).Name
                                            End If
                                        Else
                                            saveWriteTime = System.IO.File.GetLastWriteTime(files(y).FullName)
                                            saveExpFileNameWithPath = files(y).FullName
                                            saveExpFileName = files(y).Name
                                        End If

                                    End If
                                Next

                            End If

                            If Not saveWriteTime = Nothing Then

                                If Not System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\Experiments") Then
                                    System.IO.Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\Experiments")
                                End If

                                dir = New DirectoryInfo(My.Application.Info.DirectoryPath & "\Experiments")

                                files = dir.GetFiles

                                'Here we are checking to see if the corresponding experiment already exists on the PC with the same
                                'last write time, if it does, we will not do anything, if it does not, we will need to copy if from the
                                'share drive (or flash drive) and import it into INCA...
                                For y = 0 To UBound(files)

                                    If files(y).Name = saveExpFileName Then
                                        If System.IO.File.GetLastWriteTime(saveExpFileNameWithPath) = System.IO.File.GetLastWriteTime(files(y).FullName) Then
                                            fileFound = True
                                            Exit For
                                        End If

                                    End If

                                Next

                                'We will copy the experiment to the PC if the file write time of the experiment file is not equal, or if the
                                'experiment file is not found on the PC.  After copying, we will import the experiment into INCA by calling
                                'UpdateINCAWithLatestExperiment...

                                If fileFound = False Then

                                    HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW: Copying Experiment to " & My.Application.Info.DirectoryPath & "\Experiments Directory...",,, FlashMsg1Sec)

                                    FileCopy(saveExpFileNameWithPath, dir.FullName & "\" & saveExpFileName)

                                    found = True
                                    HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW: Experiment Copy Complete.",,, FlashMsg1Sec)

                                    UpdateINCAWithLatestExperiment(dir.FullName & "\" & saveExpFileName, csvFileInFolder, mismatch)

                                Else
                                    fileFound = False
                                End If

                            Else
                                HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW: No corresponding experiment found in " & dirs(x).FullName & " for MY" & GModelYear & GSpecificArxml,, )
                            End If

                            Exit For

                        End If

                    End If

                Next x

            End If

            If found = False Then

                HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW:  No Updates Found...",,, FlashMsg1Sec)

                'Here we will check to see if there are newer files available on the PC than the files referenced in the config.xml file.
                'This can happen if the user chose not to update the config file with the newer signal list and experiment that were
                'found and copied during a previous session...

                'If we get here, we should have found a signal list that corresponds to the information in the workspace, otherwise
                'we would have updated and Found would be set to True, so this would be bypassed...
                If Len(saveLatestSignalListFileName) > 0 Then

                    Dim filenameWithoutExtenstion As String
                    Dim foundExperiment As Boolean
                    Dim tempExperimentFileName As String = ""
                    Dim tempExperimentName As String = ""

                    filenameWithoutExtenstion = System.IO.Path.GetFileNameWithoutExtension(saveLatestSignalListFileName)

                    'If the base file names (without .csv or .xlsx extensions considered) don't match, this would indicate that we found a different file than
                    'the one referenced in the config.xml file. We compare without extension because the INCAVariableFile may be a .csv file, whereas the
                    'newest file may be the .xlsx file. There should always be a .csv and .xlsx signal list file for the same software version and model year
                    'combination, but the last write time of the .xlsx file might be newer than the .csv file. If that were the case, the full names would not
                    'match but they may be for the same software version model year combination, but would not compare here... We only want to pass through this
                    'code if the base file names do not match...
                    If filenameWithoutExtenstion <> System.IO.Path.GetFileNameWithoutExtension(INCAVariableFile) Then

                        'Make sure that the latest signal list file name is newer than the INCAVariableFile being used...
                        If System.IO.File.GetLastWriteTime(saveLatestSignalListFileName) > System.IO.File.GetLastWriteTime(INCAVariableFile) Then

                            'We want to use the .csv file, not the.xlsx file...
                            If System.IO.File.Exists(Mid(saveLatestSignalListFileName, 1, InStr(saveLatestSignalListFileName, ".") - 1) & ".csv") Then

                                'Make sure that the corresponding experiment file exists (it should)...
                                tempExperimentFileName = My.Application.Info.DirectoryPath & "\Experiments\" & Mid(System.IO.Path.GetFileName(saveLatestSignalListFileName), 1, InStr(System.IO.Path.GetFileName(saveLatestSignalListFileName), ".")) & "exp"
                                If System.IO.File.Exists(tempExperimentFileName) Then

                                    'Now check to see if the experiment has been imported into INCA (it should have been when the files were initially updated from the source)...
                                    tempExperimentName = System.IO.Path.GetFileNameWithoutExtension(Mid(System.IO.Path.GetFileName(saveLatestSignalListFileName), 1, InStr(System.IO.Path.GetFileName(saveLatestSignalListFileName), ".") - 1))

                                    If AvailableExperimentNames Is Nothing Then
                                        AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames
                                    End If

                                    For x = 0 To UBound(AvailableExperimentNames)
                                        If AvailableExperimentNames(x) = tempExperimentName Then
                                            foundExperiment = True
                                            Exit For
                                        End If
                                    Next

                                    If foundExperiment = True Then

                                        'Only if we have found everything in the right place, do we ask if the user wants to update.  We do not want to be asking and then
                                        'not be able to make the change because the files do not exists...

                                        If MsgBox("There are newer Experiment / Signal list files available on this PC than the ones that are referenced in the CLEVIR configuration file.  Do you want to use the latest files?", vbYesNo) = vbYes Then
                                            HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW:  User indicated that they want to use the LATEST files...",, )

                                            INCAExperiment = tempExperimentName
                                            InitialINCAExperiment = INCAExperiment
                                            INCAVariableFile = Mid(saveLatestSignalListFileName, 1, InStr(saveLatestSignalListFileName, ".") - 1) & ".csv"

                                        Else
                                            HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW:  User indicated that they want to stay with the OLDER files...",, )
                                        End If

                                    Else
                                        HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW: Files found but experient " & tempExperimentName & " not found in INCA",, )
                                    End If
                                Else
                                    HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW:  Experiment " & tempExperimentFileName & " not found...",, )
                                End If
                            Else
                                HandleUserMessageLogging("GMRC", "CheckForNewerSignalListNEW:  .csv Version of Signal List not found...",, )
                            End If

                        End If

                    End If

                End If

            End If

            CheckForNewerSignalListComplete = True

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "Checking for Newer Signal List And Experiment - Error: " & ex.Message, DisplayMsgBox)

        End Try

    End Sub

    Public Function AddNewWorkspaceFromTemplateOld(ByVal myTemplateWorkspaceName As String) As Boolean

        'Called from SaveVehicleConfigChanges which is tied to a button on the VehicleConfigurationsEditor form.
        'Handles creating a workspace from the template workspace for a given program and from the user choices
        'made on the VehicleConfigurationsEditor form.  Also called from CopyWorkspaceTemplateNEW which is part of the
        'automated workspace creation process when the user selects software and calibrations to create a software version
        'specific workspace...

        'It should be noted here that depending on where this function is being called from,  the workspaces that are created
        'will either be created for reference purposes without sofware version specific software (when called from SaveVehicleConfigChanges)
        'or will be used to create software version specific templates which are used to create the actual workspaces that are
        'used in vehicle for data collection (when called from CopyWorkspaceTemplateNEW)...

        Dim myFolder As Folder

        Dim myHwSystems() As HWSystem
        Dim myDatabaseItems() As DataBaseItem

        Dim x As Integer
        Dim y As Integer
        Dim i As Integer

        Dim myHwDevices() As HWDevice

        Dim myHwSystemSystems() As HWSystem
        Dim myHwSystemSystemsSystems() As HWSystem

        Dim myBlueBoxInfo(3) As String
        Dim myNumberOfCameras As Integer
        Dim myNumControllers As Integer

        Dim returnStr As String = ""

        HandleUserMessageLogging("GMRC", "AddNewWorkspaceFromTemplate Called...")

        returnStr = MyIncaInterface.ConnectToInca()

        If returnStr = "True" Then

            myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")

            'check for existing templateworkspacename in INCA here and remove it if it exists...
            myDatabaseItems = myFolder.BrowseDataBaseItem(myTemplateWorkspaceName)
            If UBound(myDatabaseItems) = 0 Then
                myFolder.RemoveComponent(myDatabaseItems(0))
            End If

            'Different behavior based on vehicle type configured by the user...

            'Workspace Templates will have to be different depending on if processors are to be used with FCMs or not. This based on differences in how CAN needs
            'to be handled.  HighContent witn no FCM for instance, will use the HCS ARXML file for specific software, whereas HighContent with FCM will need
            'to use globalB System Description file?

            'So, to handle this we need to create a bunch more workspace templates and we need to differentiate further on ComboBox1.text to also
            'determine if it is using FCM or not so we know the right template workspace with the proper CAN configuration to choose...

            'No FCM vs FCM vs FCM100

            'Currently, FCM, FCM100 are not being used, either as stand alone or in conjunction with another controller type, so all FCM and FCM100 stuff
            'used throughout can basically be ignored at this point.  There is lots of code related to FCM and FCM100 based on initial understanding of
            'what would be required for instrumented FCM and FCM100 and if instrumented FCMs were going to be used in conjunction with other controllers, however...

            'here just putting global variables into local variables, really not necessary...
            myBlueBoxInfo = BlueBoxInfo
            myNumberOfCameras = NumberOfCamerasInVehicle
            myNumControllers = NumControllers

            'The MasterTemplateName is a global that is set in ReadVehicleConfigsFile which is called when changes are made in the VehicleConfigurationsEditor...
            'There are different Master Templates for each vehicle type which contain all of the device references that a particular vehicle type might be
            'configured with.  Should a new device be required for a vehicle type that was not comprehended when this design was done, its master template and
            'the code associated with using the master template will need to change...

            'The idea behind a master template is to have a super set of all possible devices and then based on what is read in the vehicle confiugration file,
            'remove the devices that are not applicable, leaving only those devices that are defined in the vehicle configuration file for that vehicle...
            'That is what is done in this routine...
            HandleUserMessageLogging("GMRC", "AddNewWorkspaceFromTemplate: Operating on " & MasterTemplateName & " vehicle Type...")

            'here we are looking to see if master template already exists in INCA
            myDatabaseItems = myFolder.BrowseDataBaseItem(MasterTemplateName)

            'If master template does not exist, we will copy it from the CLEVIR install path.  All possible master templates should be in the CLEVIR install path
            'because they are in the UpdatedFiles folder on the share drive that CLEVIR checks at startup.  If new files are found, they are copied into install path
            'by CLEVIR automatically...
            If myDatabaseItems.Length = 0 Then
                If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\" & MasterTemplateName & ".exp") Then
                    ImportFileIntoINCA(My.Application.Info.DirectoryPath & "\" & MasterTemplateName & ".exp", True, False)
                    myDatabaseItems = myFolder.BrowseDataBaseItem(MasterTemplateName)
                End If

            End If

            'IF something goes wrong and we cant find or import the master template, we have to bail here...
            If myDatabaseItems.Length = 0 Then
                HandleUserMessageLogging("GMRC", "Could Not find template workspace for " & MasterTemplateName & " - Add New Workspace from Template could not be completed...", DisplayMsgBox, )
                Exit Function
            End If

            'Next, we take the master template and copy it to myTemplateWorkspaceName which is passed in from caller...
            HandleUserMessageLogging("GMRC", "Copying " & MasterTemplateName)
            myDatabaseItems(0).Copy(myTemplateWorkspaceName)

            'Here we are creating the Hardware Configuration object using the newly copied template workspace...
            MyHWC = Get_Workspace(myTemplateWorkspaceName, "CLEVIR Setup\Workspaces")

            'Bail if we were unable to create the hardware configuration from the workspace. This should really never happen at this point unless the INCA API copy function has failed
            'for some reason or sometihng goes wrong in Get_Workspace...
            If MyHWC Is Nothing Then
                HandleUserMessageLogging("GMRC", "AddNewWorkspaceFromTemplate: Get_Workspace could not find " & myTemplateWorkspaceName,, )
                Exit Function
            End If

            'myHWSystems will contain all of the hardware sytems defined in the template workspace, MCU Devices, ETAS Blue Box devices, etc...
            myHwSystems = MyHWC.GetAllSystems

            'This assumes that everyone has a 595 and 593 or a 595 and 592 or a 523 only...
            HandleUserMessageLogging("GMRC", "Modifying new workspace based on " & myTemplateWorkspaceName & " contents...")

            'Here we are going through all of the hardware systems found in the template workspace and removing those that are not applicable
            'based on the information obtained when we read the vehicle configuration file (vehiclecongfigurations.csv)
            For x = 0 To UBound(myHwSystems)

                'MsgBox(myHWSystems(x).GetName)
                'ES8xx
                If InStr(myHwSystems(x).GetName, "ES8xx") > 0 Then

                    If InStr(MasterTemplateName, "ACP2") = 0 Then

                        If myBlueBoxInfo(0) <> "886" Then
                            MyHWC.RemoveSystem(myHwSystems(x))
                        Else
                            myHwSystemSystems = myHwSystems(x).GetAllSystems
                            For y = 0 To UBound(myHwSystemSystems)
                                myHwSystemSystemsSystems = myHwSystemSystems(y).GetAllSystems
                                For i = 0 To UBound(myHwSystemSystemsSystems)

                                    myHwSystemSystemsSystems(i).SetName("CAN:" & CStr(i + 1))
                                    myHwDevices = myHwSystemSystemsSystems(i).GetAllDevices
                                    myHwDevices(0).SetName("CAN-Monitoring:" & CStr(i + 1))

                                Next
                            Next
                        End If

                    Else
                        If myBlueBoxInfo(1) <> "886" Then
                            MyHWC.RemoveSystem(myHwSystems(x))
                        Else
                            myHwSystemSystems = myHwSystems(x).GetAllSystems
                            For y = 0 To UBound(myHwSystemSystems)
                                myHwSystemSystemsSystems = myHwSystemSystems(y).GetAllSystems
                                For i = 0 To UBound(myHwSystemSystemsSystems)

                                    myHwSystemSystemsSystems(i).SetName("CAN:" & CStr(i + 2))
                                    myHwDevices = myHwSystemSystemsSystems(i).GetAllDevices
                                    myHwDevices(0).SetName("CAN-Monitoring:" & CStr(i + 2))

                                Next
                            Next
                        End If
                    End If

                End If

                'The assumption we are making here is that whenever we are using a 595, it is always defined as being associated with the first CAN Channel
                'based on original instrumentation design decisions...
                If InStr(myHwSystems(x).GetName, "595") > 0 Then
                    If myBlueBoxInfo(0) <> "595" Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    Else

                    End If
                End If

                'If a 593 is used, it is always associated with the second CAN Channel based on original instrumentation design decisions...
                If InStr(myHwSystems(x).GetName, "593") > 0 Then
                    If myBlueBoxInfo(1) <> "593" Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    Else
                        myHwSystemSystems = myHwSystems(x).GetAllSystems

                        For y = 0 To UBound(myHwSystemSystems)
                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 2))
                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 2))
                        Next
                    End If
                End If

                'It is possible that a 592 might have been used instead of a 593 on some vehicles, but will always be used with the second CAN Channel if used...
                If InStr(myHwSystems(x).GetName, "592") > 0 Then
                    If myBlueBoxInfo(1) <> "592" Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    Else
                        myHwSystemSystems = myHwSystems(x).GetAllSystems

                        For y = 0 To UBound(myHwSystemSystems)
                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 2))
                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 2))

                        Next
                    End If
                End If

                'If a 523 module is used, it would be used for up to four CAN channels and take the place of the 593/595 combination that was used on CSAV2 vehicles, so this
                'is handled a bit differently...
                If InStr(myHwSystems(x).GetName, "523") > 0 Then
                    'IF there is no CAN, that is "NA" for every CAN Channel defined in the configuration file, we will remove the ES523 device from the workspace entirely...
                    If InStr(myBlueBoxInfo(0), "523") = 0 And InStr(myBlueBoxInfo(1), "523") = 0 And InStr(myBlueBoxInfo(2), "523") = 0 And InStr(myBlueBoxInfo(3), "523") = 0 Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    Else
                        myHwSystemSystems = myHwSystems(x).GetAllSystems

                        'If there is at least one CAN channel defined in the vehicle configuration file for the specified vehicle, we need to loop through the blueboxinfo to see
                        'which CAN channels are being used.  In most cases, all four CAN Channels will be defined in the vehicle configuration file, however, there is a new
                        'configuration interoduced for ACP4, which would allow for an RTK to be connected on any one of the four CAN channels with the other three CAN channels
                        'being disconnected.  The code below handles this possible scenario...
                        For y = 0 To UBound(myHwSystemSystems)

                            If InStr(MasterTemplateName, "ACP2") = 0 Then

                                Select Case y
                                    Case 0

                                        If InStr(myBlueBoxInfo(0), "523") > 0 Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 1
                                        If InStr(myBlueBoxInfo(1), "523") > 0 Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 2
                                        If InStr(myBlueBoxInfo(2), "523") > 0 Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 3
                                        If InStr(myBlueBoxInfo(3), "523") > 0 Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                End Select

                            Else

                                Select Case y
                                    Case 0

                                        If InStr(myBlueBoxInfo(1), "523") > 0 Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 2))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 2))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 1
                                        If InStr(myBlueBoxInfo(2), "523") > 0 Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 2))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 2))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If

                                End Select

                            End If

                        Next

                    End If

                End If

                'Ethernet in the HWSystem name indicates an MCU Type device...
                If InStr(myHwSystems(x).GetName, "Ethernet") > 0 Then

                    myHwDevices = myHwSystems(x).GetAllDevices()

                    Select Case MasterTemplateName 'use mastertemplatename instead of combobox1.text...

                        Case "ACP2_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP2_MCU")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "ACP3_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP3_MCU")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "ACP3_FCM_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP3_MCU")
                                    Case 1
                                        myHwDevices(y).SetName("FCM")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "ACP3_FCM100_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP3_MCU")
                                    Case 1
                                        myHwDevices(y).SetName("FCM100")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "ACP4_WorkspaceTemplate", "ACP4_WorkspaceTemplate_RTK"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP4_MCU")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next
                    End Select

                End If

            Next

            HandleUserMessageLogging("GMRC", myTemplateWorkspaceName & " has been created in INCA.",, )

            'After creating the generic workspace from the template, we will create the workspace with the number of cameras defined
            'in the vehicleconfigurations.csv file for the selected vehicle...
            myDatabaseItems = myFolder.BrowseDataBaseItem(myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C")

            'Remove workspace if it already exists, otherwise copy command will not create a new copy with the same name...
            If myDatabaseItems.Length > 0 Then
                myFolder.RemoveComponent(myDatabaseItems(0))
            End If

            myDatabaseItems = myFolder.BrowseDataBaseItem(myTemplateWorkspaceName)

            HandleUserMessageLogging("GMRC", "Copying " & myTemplateWorkspaceName)
            myDatabaseItems(0).Copy(myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C")

            'Here we call the routine that handles setting up the cameras, same deal as above, template contains maximum 6 cameras, we name the cameras based
            'on the contents of the vehicleconfigurations.csv file and remove the unused ones...
            If SetupCameraNamesInWorkspace(myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C") = True Then

                HandleUserMessageLogging("GMRC", myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C" & " has been created in INCA.", DisplayMsgBox)
                AddNewWorkspaceFromTemplateOld = True

            Else
                HandleUserMessageLogging("GMRC", myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C" & " Camera name setup failed...", DisplayMsgBox, )
            End If

        Else 'ConnectToInca returned error string...
            HandleUserMessageLogging("GMRC", "AddNewWorkspaceFromTemplate: ConnectToInca returned - " & returnStr, DisplayMsgBox, )
        End If

    End Function

    Public Function AddNewWorkspaceFromTemplate(ByVal myTemplateWorkspaceName As String) As Boolean

        'Called from SaveVehicleConfigChanges which is tied to a button on the VehicleConfigurationsEditor form.
        'Handles creating a workspace from the template workspace for a given program and from the user choices
        'made on the VehicleConfigurationsEditor form.  

        'Also called from CopyWorkspaceTemplateNEW which Is part of the
        'automated workspace creation process when the user selects software and calibrations to create a software version
        'specific workspace...

        'It should be noted here that depending on where this function is being called from,  the workspaces that are created
        'will either be created for reference purposes without sofware version specific software (when called from SaveVehicleConfigChanges)
        'or will be used to create software version specific templates which are used to create the actual workspaces that are
        'used in vehicle for data collection (when called from CopyWorkspaceTemplateNEW)...

        Dim myFolder As Folder

        Dim myHwSystems() As HWSystem
        Dim myDatabaseItems() As DataBaseItem

        Dim x As Integer
        Dim y As Integer
        Dim i As Integer

        Dim myHwDevices() As HWDevice

        Dim myHwSystemSystems() As HWSystem
        Dim myHwSystemSystemsSystems() As HWSystem

        Dim myBlueBoxInfo(3) As String
        Dim myNumberOfCameras As Integer
        Dim myNumControllers As Integer

        Dim returnStr As String = ""

        HandleUserMessageLogging("GMRC", "AddNewWorkspaceFromTemplate Called with " & myTemplateWorkspaceName & "...")

        returnStr = MyIncaInterface.ConnectToInca()

        If returnStr = "True" Then

            myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")

            'check for existing templateworkspacename in INCA here and remove it if it exists...
            myDatabaseItems = myFolder.BrowseDataBaseItem(myTemplateWorkspaceName)
            If UBound(myDatabaseItems) = 0 Then
                myFolder.RemoveComponent(myDatabaseItems(0))
            End If

            'Different behavior based on vehicle type configured by the user...

            'Workspace Templates will have to be different depending on if processors are to be used with FCMs or not. This based on differences in how CAN needs
            'to be handled.  HighContent witn no FCM for instance, will use the HCS ARXML file for specific software, whereas HighContent with FCM will need
            'to use globalB System Description file?

            'So, to handle this we need to create a bunch more workspace templates and we need to differentiate further on ComboBox1.text to also
            'determine if it is using FCM or not so we know the right template workspace with the proper CAN configuration to choose...

            'No FCM vs FCM vs FCM100

            'Currently, FCM, FCM100 are not being used, either as stand alone or in conjunction with another controller type, so all FCM and FCM100 stuff
            'used throughout can basically be ignored at this point.  There is lots of code related to FCM and FCM100 based on initial understanding of
            'what would be required for instrumented FCM and FCM100 and if instrumented FCMs were going to be used in conjunction with other controllers, however...

            'here just putting global variables into local variables, really not necessary...
            myBlueBoxInfo = BlueBoxInfo
            myNumberOfCameras = NumberOfCamerasInVehicle
            myNumControllers = NumControllers

            'The MasterTemplateName is a global that is set in ReadVehicleConfigsFile which is called when changes are made in the VehicleConfigurationsEditor...
            'There are different Master Templates for each vehicle type which contain all of the device references that a particular vehicle type might be
            'configured with.  Should a new device be required for a vehicle type that was not comprehended when this design was done, its master template and
            'the code associated with using the master template will need to change...

            'The idea behind a master template is to have a super set of all possible devices and then based on what is read in the vehicle confiugration file,
            'remove the devices that are not applicable, leaving only those devices that are defined in the vehicle configuration file for that vehicle...
            'That is what is done in this routine...


            If InStr(myTemplateWorkspaceName, "RTK") > 0 Then
                MasterTemplateName = GProjectAbbreviation & "_WorkspaceTemplate_RTK"
            End If

            HandleUserMessageLogging("GMRC", "AddNewWorkspaceFromTemplate: Operating on " & MasterTemplateName & " vehicle Type...")

            'here we are looking to see if master template already exists in INCA
            myDatabaseItems = myFolder.BrowseDataBaseItem(MasterTemplateName)

            'If master template does not exist, we will copy it from the CLEVIR install path.  All possible master templates should be in the CLEVIR install path
            'because they are in the UpdatedFiles folder on the share drive that CLEVIR checks at startup.  If new files are found, they are copied into install path
            'by CLEVIR automatically...
            If myDatabaseItems.Length = 0 Then
                If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\" & MasterTemplateName & ".exp") Then
                    ImportFileIntoINCA(My.Application.Info.DirectoryPath & "\" & MasterTemplateName & ".exp", True, False)
                    myDatabaseItems = myFolder.BrowseDataBaseItem(MasterTemplateName)
                End If

            Else
                If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\" & MasterTemplateName & ".exp") Then
                    myFolder.RemoveComponent(myDatabaseItems(0))
                    ImportFileIntoINCA(My.Application.Info.DirectoryPath & "\" & MasterTemplateName & ".exp", True, False)
                    myDatabaseItems = myFolder.BrowseDataBaseItem(MasterTemplateName)
                End If

            End If

            'IF something goes wrong and we cant find or import the master template, we have to bail here...
            If myDatabaseItems.Length = 0 Then
                HandleUserMessageLogging("GMRC", "Could Not find template workspace for " & MasterTemplateName & " - Add New Workspace from Template could not be completed...", DisplayMsgBox, )
                Exit Function
            End If

            'Next, we take the master template and copy it to myTemplateWorkspaceName which is passed in from caller...
            HandleUserMessageLogging("GMRC", "Copying " & MasterTemplateName)
            myDatabaseItems(0).Copy(myTemplateWorkspaceName)

            'Here we are creating the Hardware Configuration object using the newly copied template workspace...
            MyHWC = Get_Workspace(myTemplateWorkspaceName, "CLEVIR Setup\Workspaces")

            'Bail if we were unable to create the hardware configuration from the workspace. This should really never happen at this point unless the INCA API copy function has failed
            'for some reason or sometihng goes wrong in Get_Workspace...
            If MyHWC Is Nothing Then
                HandleUserMessageLogging("GMRC", "AddNewWorkspaceFromTemplate: Get_Workspace could not find " & myTemplateWorkspaceName,, )
                Exit Function
            End If

            'myHWSystems will contain all of the hardware sytems defined in the template workspace, MCU Devices, ETAS Blue Box devices, etc...
            myHwSystems = MyHWC.GetAllSystems

            HandleUserMessageLogging("GMRC", "Modifying new workspace based on " & myTemplateWorkspaceName & " contents...")

            'Here we are going through all of the hardware systems found in the template workspace and removing those that are not applicable
            'based on the information obtained when we read the vehicle configuration file (vehiclecongfigurations.csv)
            For x = 0 To UBound(myHwSystems)

                'MsgBox(myHWSystems(x).GetName)
                'ES8xx

                If MaxCameras = 6 Then
                    If InStr(myHwSystems(x).GetName, "ONVIF system:7") > 0 Or InStr(myHwSystems(x).GetName, "ONVIF system:8") > 0 Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    End If
                End If

                If InStr(myHwSystems(x).GetName, "ES8xx") > 0 Then

                    If InStr(MasterTemplateName, "ACP2") = 0 And InStr(MasterTemplateName, "ACP4") = 0 Then

                        If InStr(myTemplateWorkspaceName, "886") = 0 Then
                            MyHWC.RemoveSystem(myHwSystems(x))
                        Else
                            myHwSystemSystems = myHwSystems(x).GetAllSystems
                            For y = 0 To UBound(myHwSystemSystems)
                                myHwSystemSystemsSystems = myHwSystemSystems(y).GetAllSystems
                                For i = 0 To UBound(myHwSystemSystemsSystems)

                                    myHwSystemSystemsSystems(i).SetName("CAN:" & CStr(i + 1))
                                    myHwDevices = myHwSystemSystemsSystems(i).GetAllDevices
                                    myHwDevices(0).SetName("CAN-Monitoring:" & CStr(i + 1))

                                Next
                            Next
                        End If

                    ElseIf InStr(MasterTemplateName, "ACP2") > 0 Then
                        'If myBlueBoxInfo(1) <> "886" Then
                        If InStr(myTemplateWorkspaceName, "886") = 0 Then
                            MyHWC.RemoveSystem(myHwSystems(x))
                        Else
                            myHwSystemSystems = myHwSystems(x).GetAllSystems
                            For y = 0 To UBound(myHwSystemSystems)
                                myHwSystemSystemsSystems = myHwSystemSystems(y).GetAllSystems
                                For i = 0 To UBound(myHwSystemSystemsSystems)

                                    myHwSystemSystemsSystems(i).SetName("CAN:" & CStr(i + 2))
                                    myHwDevices = myHwSystemSystemsSystems(i).GetAllDevices
                                    myHwDevices(0).SetName("CAN-Monitoring:" & CStr(i + 2))

                                Next
                            Next
                        End If

                    ElseIf InStr(MasterTemplateName, "ACP4") > 0 Then

                        If InStr(myTemplateWorkspaceName, "886") = 0 Then
                            MyHWC.RemoveSystem(myHwSystems(x))
                        Else
                            myHwSystemSystems = myHwSystems(x).GetAllSystems
                            For y = 0 To UBound(myHwSystemSystems)
                                myHwSystemSystemsSystems = myHwSystemSystems(y).GetAllSystems
                                For i = 0 To UBound(myHwSystemSystemsSystems)

                                    If i < 2 Then
                                        myHwSystemSystemsSystems(i).SetName("CAN:" & CStr(i + 1))
                                        myHwDevices = myHwSystemSystemsSystems(i).GetAllDevices
                                        myHwDevices(0).SetName("CAN-Monitoring:" & CStr(i + 1))
                                    Else
                                        myHwSystemSystemsSystems(i).SetName("CAN:4")
                                        myHwDevices = myHwSystemSystemsSystems(i).GetAllDevices
                                        If InStr(MasterTemplateName, "RTK") = 0 Then
                                            myHwDevices(0).SetName("CAN-Monitoring:4")
                                        Else
                                            myHwDevices(0).SetName("CAN-Monitoring RTK")
                                        End If

                                    End If
                                Next
                            Next
                        End If

                    End If

                End If

                'The assumption we are making here is that whenever we are using a 595, it is always defined as being associated with the first CAN Channel
                'based on original instrumentation design decisions...
                If InStr(myHwSystems(x).GetName, "595") > 0 Then
                    'If myBlueBoxInfo(0) <> "595" Then
                    If InStr(myTemplateWorkspaceName, "595") = 0 Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    Else

                    End If
                End If

                'If a 593 is used, it is always associated with the second CAN Channel based on original instrumentation design decisions...
                If InStr(myHwSystems(x).GetName, "593") > 0 Then
                    'If myBlueBoxInfo(1) <> "593" Then
                    If InStr(myTemplateWorkspaceName, "593") = 0 Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    Else
                        myHwSystemSystems = myHwSystems(x).GetAllSystems

                        For y = 0 To UBound(myHwSystemSystems)
                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 2))
                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 2))
                        Next
                    End If
                End If

                'It is possible that a 592 might have been used instead of a 593 on some vehicles, but will always be used with the second CAN Channel if used...
                If InStr(myHwSystems(x).GetName, "592") > 0 Then
                    'If myBlueBoxInfo(1) <> "592" Then
                    If InStr(myTemplateWorkspaceName, "592") = 0 Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    Else
                        myHwSystemSystems = myHwSystems(x).GetAllSystems

                        For y = 0 To UBound(myHwSystemSystems)
                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 2))
                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 2))

                        Next
                    End If
                End If

                'If a 523 module is used, it would be used for up to four CAN channels and take the place of the 593/595 combination that was used on CSAV2 vehicles, so this
                'is handled a bit differently...
                If InStr(myHwSystems(x).GetName, "523") > 0 Then
                    'IF there is no CAN, that is "NA" for every CAN Channel defined in the configuration file, we will remove the ES523 device from the workspace entirely...
                    'If InStr(myBlueBoxInfo(0), "523") = 0 And InStr(myBlueBoxInfo(1), "523") = 0 And InStr(myBlueBoxInfo(2), "523") = 0 And InStr(myBlueBoxInfo(3), "523") = 0 Then
                    If InStr(myTemplateWorkspaceName, "NOCAN") > 0 Or InStr(myTemplateWorkspaceName, "886") > 0 Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    Else
                        myHwSystemSystems = myHwSystems(x).GetAllSystems

                        'If there is at least one CAN channel defined in the vehicle configuration file for the specified vehicle, we need to loop through the blueboxinfo to see
                        'which CAN channels are being used.  In most cases, all four CAN Channels will be defined in the vehicle configuration file, however, there is a new
                        'configuration interoduced for ACP4, which would allow for an RTK to be connected on any one of the four CAN channels with the other three CAN channels
                        'being disconnected.  The code below handles this possible scenario...
                        For y = 0 To UBound(myHwSystemSystems)

                            If InStr(MasterTemplateName, "ACP2") = 0 And InStr(MasterTemplateName, "ACP4") = 0 Then

                                Select Case y
                                    Case 0

                                        If InStr(myBlueBoxInfo(0), "523") > 0 Or ConfigureForNewSoftwareVersion = True Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 1
                                        If InStr(myBlueBoxInfo(1), "523") > 0 Or ConfigureForNewSoftwareVersion = True Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 2
                                        If InStr(myBlueBoxInfo(2), "523") > 0 Or ConfigureForNewSoftwareVersion = True Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 3
                                        If InStr(myBlueBoxInfo(3), "523") > 0 Or ConfigureForNewSoftwareVersion = True Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                End Select

                            ElseIf InStr(MasterTemplateName, "ACP2") > 0 Then 'ACP2 is handled differently because only CAN Channels 2 and 3 are used...

                                Select Case y
                                    Case 0

                                        If InStr(myBlueBoxInfo(1), "523") > 0 Or ConfigureForNewSoftwareVersion = True Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 2))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 2))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 1
                                        If InStr(myBlueBoxInfo(2), "523") > 0 Or ConfigureForNewSoftwareVersion = True Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 2))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 2))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If

                                End Select

                            ElseIf InStr(MasterTemplateName, "ACP4") > 0 Then

                                Select Case y
                                    Case 0

                                        If InStr(myBlueBoxInfo(0), "523") > 0 Or ConfigureForNewSoftwareVersion = True Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 1
                                        If InStr(myBlueBoxInfo(1), "523") > 0 Or ConfigureForNewSoftwareVersion = True Then
                                            myHwSystemSystems(y).SetName("CAN:" & CStr(y + 1))
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            myHwDevices(0).SetName("CAN-Monitoring:" & CStr(y + 1))
                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If
                                    Case 2
                                        If InStr(myBlueBoxInfo(3), "523") > 0 Or ConfigureForNewSoftwareVersion = True Then
                                            myHwSystemSystems(y).SetName("CAN:4")
                                            myHwDevices = myHwSystemSystems(y).GetAllDevices
                                            If InStr(MasterTemplateName, "RTK") = 0 Then
                                                myHwDevices(0).SetName("CAN-Monitoring:4")
                                            Else
                                                myHwDevices(0).SetName("CAN-Monitoring RTK")
                                            End If

                                        Else
                                            MyHWC.RemoveSystem(myHwSystemSystems(y))
                                        End If

                                End Select

                            End If

                        Next

                    End If

                End If

                'Ethernet in the HWSystem name indicates an MCU Type device...
                If InStr(myHwSystems(x).GetName, "Ethernet") > 0 Then

                    myHwDevices = myHwSystems(x).GetAllDevices()

                    Select Case MasterTemplateName 'use mastertemplatename instead of combobox1.text...

                        Case "HC_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("HCF")
                                    Case 1
                                        myHwDevices(y).SetName("HCS")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "CSAV2_WorkspaceTemplate"
                            Select Case myNumControllers 'Use myNumControllers here...

                                Case 0
                                    For y = 0 To UBound(myHwDevices)
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                    Next
                                Case 3
                                    For y = 0 To UBound(myHwDevices)
                                        Select Case y
                                            Case 0
                                                myHwDevices(y).SetName("IP")
                                            Case 1
                                                myHwDevices(y).SetName("K1P")
                                            Case 2
                                                myHwDevices(y).SetName("K2P")
                                            Case Else
                                                MyHWC.RemoveDevice(myHwDevices(y))
                                        End Select
                                    Next

                                Case 6
                                    'Don't change anything, template contains 6 processors, all of which are required...
                            End Select

                        Case "LC_WorkspaceTemplate"
                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("XETK:1")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "FCM_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("FCM")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "LC_FCM_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("XETK:1")
                                    Case 1
                                        myHwDevices(y).SetName("FCM")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "HC_FCM_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("HCF")
                                    Case 1
                                        myHwDevices(y).SetName("HCS")
                                    Case 2
                                        myHwDevices(y).SetName("FCM")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "FCM100_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("FCM100")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "LC_FCM100_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("XETK:1")
                                    Case 1
                                        myHwDevices(y).SetName("FCM100")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "HC_FCM100_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("HCF")
                                    Case 1
                                        myHwDevices(y).SetName("HCS")
                                    Case 2
                                        myHwDevices(y).SetName("FCM100")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "ACP2_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP2_MCU")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "ACP3_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP3_MCU")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "ACP3_FCM_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP3_MCU")
                                    Case 1
                                        myHwDevices(y).SetName("FCM")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "ACP3_FCM100_WorkspaceTemplate"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP3_MCU")
                                    Case 1
                                        myHwDevices(y).SetName("FCM100")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next

                        Case "ACP4_WorkspaceTemplate", "ACP4_WorkspaceTemplate_RTK"

                            For y = 0 To UBound(myHwDevices)
                                Select Case y
                                    Case 0
                                        myHwDevices(y).SetName("ACP4_MCU")
                                    Case Else
                                        MyHWC.RemoveDevice(myHwDevices(y))
                                End Select
                            Next
                    End Select

                End If

            Next

            HandleUserMessageLogging("GMRC", myTemplateWorkspaceName & " has been created in INCA.",, )

            If ConfigureForNewSoftwareVersion = False Then

                'After creating the generic workspace from the template, we will create the workspace with the number of cameras defined
                'in the vehicleconfigurations.csv file for the selected vehicle...
                myDatabaseItems = myFolder.BrowseDataBaseItem(myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C")

                'Remove workspace if it already exists, otherwise copy command will not create a new copy with the same name...
                If myDatabaseItems.Length > 0 Then
                    myFolder.RemoveComponent(myDatabaseItems(0))
                End If

                myDatabaseItems = myFolder.BrowseDataBaseItem(myTemplateWorkspaceName)

                HandleUserMessageLogging("GMRC", "Copying " & myTemplateWorkspaceName)
                myDatabaseItems(0).Copy(myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C")

                'Here we call the routine that handles setting up the cameras, same deal as above, template contains maximum 6 cameras, we name the cameras based
                'on the contents of the vehicleconfigurations.csv file and remove the unused ones...
                If SetupCameraNamesInWorkspace(myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C") = True Then

                    'HandleUserMessageLogging("GMRC", myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C" & " has been created in INCA.", DisplayMsgBox)
                    HandleUserMessageLogging("GMRC", myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C" & " has been created in INCA.", DisplayMsgBox, )
                    AddNewWorkspaceFromTemplate = True

                Else
                    HandleUserMessageLogging("GMRC", myTemplateWorkspaceName & CStr(myNumberOfCameras) & "C" & " Camera name setup failed...", DisplayMsgBox, )
                End If

            Else
                AddNewWorkspaceFromTemplate = True
            End If

        Else 'ConnectToInca returned error string...
            HandleUserMessageLogging("GMRC", "AddNewWorkspaceFromTemplateNEW: ConnectToInca returned - " & returnStr, DisplayMsgBox, )
        End If

    End Function

    Public Function A2LAndCalToVehicleSpecificWorkspace(ByVal myFileDialog As FileDialog, ByVal myDialog As FolderBrowserDialog, ByVal mylistbox As ListBox) As Boolean

        'Called from HandleImportSoftwareAndCals (when the Import button on the FlashingStatus screen is pressed)

        'User selects a2l and ptp files.
        'Project is created in INCA (Placed in CLEVIR Setup\Projects folder) 
        'Vehicle specific template is copied and named the same name as the ptp file minus extension with the workspace template name tacked on the end.
        'Dataset (assumes only one) is copied into the newly created workspace.

        Dim myDestinationWorkspace As HardwareConfiguration = Nothing
        Dim mysourceproject As Asap2Project
        Dim mySourceDatasets() As DataSet
        Dim projectfiles(0 To 1) As String

        Dim processorName As String = ""

        Dim errorText As String = ""
        Dim returnStr As String = ""

        Dim numPasses As Integer = 1

        A2LAndCalToVehicleSpecificWorkspace = False

        'Determine number of passes requred for a2l and ptp file selection based on ProjectName...
        Select Case ProjectName
            Case "HighContent"
                numPasses = 2
                If InStr(FCMConfigName, "FCM") > 0 Then
                    numPasses = 3
                End If
            Case "LowContent"
                numPasses = 1
                If InStr(FCMConfigName, "FCM") > 0 Then
                    numPasses = 2
                End If
            Case "FCM", "FCM100"
                numPasses = 1
            Case "ACP2"
                numPasses = 1
            Case "ACP3"
                numPasses = 1
                If InStr(FCMConfigName, "FCM") > 0 Then
                    numPasses = 2
                End If
            Case "ACP4"
                numPasses = 1
        End Select

        'Determine if we are using automatic lookup for a2l and Calibration files, or if we are using old manual selection method...

        If FileSelectionMethod <> "Manual" Then

            If VehiclePTPLookupInfo IsNot Nothing Then
                MsgBox("Please Select Software Version Folder for a2l and Calibration files...") ' on all three a2lptp not old

                If Len(InitialDirectory) = 0 Then
                    DetermineInitIncaProjectDir(InitialDirectory)
                End If
                myDialog.SelectedPath = InitialDirectory
                myDialog.Description = "Please Select a Software Version Folder"

                If myDialog.ShowDialog() = DialogResult.Cancel Then
                    errorText = "Invalid File Selection. Operation incomplete..."
                Else

                    HandleUserMessageLogging("GMRC", "Selected Folder = " & myDialog.SelectedPath,, )

                End If
            Else
                FileSelectionMethod = "Manual"
            End If

        End If

        If Len(errorText) = 0 Then
            'Here we make multiple passes thru the selected software version folder depending on ProjectName
            '(LowContent 1 pass, HighContent, 2 passes, FCM either 1, 2, or 3 passes depending on STA (FCM_1P), LCM (FCM_2P) Or LCH (FCM_3P)
            'On each pass we will either automatically select the correct a2l and ptp files or the user will select
            'This is based on whether or not there is a VehiclePTPLookup.csv file available (there should always be one)...
            For x = 0 To (numPasses - 1)

                Select Case x
                    Case 0

                        Select Case ProjectName
                            Case "HighContent"
                                processorName = "HCS"
                            Case "LowContent"
                                processorName = "LC"
                            Case "FCM", "FCM100"
                                processorName = "FCM"
                            Case "ACP2"
                                processorName = "ACP2"
                            Case "ACP3"
                                processorName = "ACP3"
                            Case "ACP4"
                                processorName = "ACP4"
                        End Select

                    Case 1

                        Select Case ProjectName
                            Case "HighContent"
                                processorName = "HCF"
                            Case "LowContent"
                                processorName = Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1)
                            Case "ACP3"
                                processorName = Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1)
                        End Select

                    Case 2 'three passes would either be FCM or FCM100 for HighContent...
                        processorName = Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1)
                End Select

                If FileSelectionMethod = "Manual" Then
                    MsgBox("Please Select A2l and ptp File for " & processorName & " processor")
                End If

                projectfiles = SelectA2LAndCalFiles(myFileDialog, myDialog, mylistbox, processorName, FileSelectionMethod)

                If Len(projectfiles(0)) > 0 Then
                    mylistbox.Items.Add("Using " & projectfiles(0))
                Else
                    HandleUserMessageLogging("GMRC", "No a2l Selected for " & processorName,,,, mylistbox)
                    errorText = "Invalid File Selection. Operation incomplete..."
                    Exit For
                End If

                If Len(projectfiles(1)) > 0 Then
                    mylistbox.Items.Add("Using " & projectfiles(1))
                Else
                    HandleUserMessageLogging("GMRC", "No CAL File Selected for " & processorName,,,, mylistbox)
                    errorText = "Invalid File Selection. Operation incomplete..."
                    Exit For
                End If

                mylistbox.SelectedIndex = mylistbox.Items.Count - 1
                mylistbox.Refresh()

                If Len(projectfiles(0)) > 0 And Len(projectfiles(1)) > 0 Then
                    SaveProjectFiles(x * 2) = projectfiles(0)
                    SaveProjectFiles((x * 2) + 1) = projectfiles(1)
                Else
                    errorText = "Invalid File Selection. Operation incomplete..."
                    Exit For
                End If

            Next

        End If

        If Len(errorText) = 0 Then

            Select Case ProjectName
                Case "HighContent"
                    NewWorkspaceName = DetermineNewWorkspaceName(SaveProjectFiles(1), SaveProjectFiles(3))
                Case "LowContent"
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
                Case "FCM", "FCM100"

                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix

                Case "ACP2"
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
                Case "ACP3"
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
                Case "ACP4"
                    NewWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
            End Select

            returnStr = MyIncaInterface.ConnectToInca()

            If returnStr = "True" Then 'In this context, we should already be connected to INCA, so this may not be necessary - no harm in keeping it here though...

                Dim myFolder As Folder
                Dim myDatabaseItems() As DataBaseItem

                myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")
                myDatabaseItems = myFolder.BrowseDataBaseItem(NewWorkspaceName)

                If myDatabaseItems.Length > 0 Then
                    If MsgBox(NewWorkspaceName & " already exists, recreate?", vbYesNo) = vbNo Then
                        A2LAndCalToVehicleSpecificWorkspace = True
                        Exit Function
                    End If
                End If

            Else
                errorText = "ConnectToInca returned " & returnStr & " Could not connect to INCA. Operation incomplete..."
            End If

        End If

        If Len(errorText) = 0 Then

            For x = 0 To (numPasses - 1)

                '0 and 1 or 2 and 3 or 4 and 5 - x = 0 or 1 or 2

                'First we need to create the project in INCA from the selected a2l and ptp file(s)...
                mysourceproject = CreateA2LptpProject(SaveProjectFiles(x * 2), SaveProjectFiles((x * 2) + 1), mylistbox)
                If mysourceproject IsNot Nothing Then

                    If x = 0 Then
                        'Here we need to copy the workspace template into a new workspace and give it a new name based on the a2l and ptp file selected...
                        'We only need to do this on the first pass if there is more than one project as with High Content vehicles...
                        myDestinationWorkspace = CopyWorkspaceTemplateNew(NewWorkspaceName, mylistbox)
                    End If

                    If myDestinationWorkspace IsNot Nothing Then

                        mySourceDatasets = mysourceproject.AllDataSets

                        If mySourceDatasets.Length > 0 Then

                            If AddSelectedProjectToWorkspace(mySourceDatasets(0), mysourceproject, myDestinationWorkspace, NewWorkspaceName, mylistbox) = True Then

                                If x = numPasses - 1 Then
                                    A2LAndCalToVehicleSpecificWorkspace = True

                                    HandleUserMessageLogging("GMRC", "Project/Dataset Copy Complete.",,, FlashMsgOn, mylistbox)
                                    UserStatusInfo.Hide()
                                End If

                            Else
                                errorText = "AddSelectedProjectToWorkspace returned false..."
                                Exit For
                            End If

                        Else
                            errorText = "Could not find dataset. Operation incomplete..."
                            Exit For
                        End If

                    Else
                        'ErrorText = "Could not find template workspace. Operation incomplete..."
                        errorText = "A2lAndCALToVehicleSpecificWorkspace: Copy Workspace Template Failed (" & INCAWorkspaceTemplateName & " to " & NewWorkspaceName & "). Operation incomplete..."
                        Exit For
                    End If

                Else
                    errorText = "Could not create INCA Project. Operation incomplete..."
                    Exit For
                End If

            Next

        End If

        If Len(errorText) > 0 Then

            HandleUserMessageLogging("GMRC", errorText, DisplayMsgBox,, FlashMsg1Sec, mylistbox)
            ConfigureForNewSoftwareVersion = False

        End If

    End Function

    Private Function AddSelectedProjectToWorkspace(ByVal mySourceDataset As DataSet, ByVal mysourceProject As Asap2Project, ByVal myDestinationWorkspace As HardwareConfiguration, ByVal workspaceName As String, ByVal mylistbox As ListBox) As Boolean

        'Called from A2lAndCALToVehicleSpecificWorkspace. Copies myDestinationWorkspace to WorkspaceName and Changes the workspace dataset
        'of the newly created workspace to mySourceDataset from mysourceProject.
        '
        Dim projectDatabasePath As String
        Dim deviceName As String
        Dim mySourceDatasetPath As String
        Dim myDatasetName As String
        Dim mysourceProjectName As String

        AddSelectedProjectToWorkspace = True

        HandleUserMessageLogging("GMRC", "Adding Project software And cals to New workspace...",,, FlashMsgOn, mylistbox)

        myDatasetName = mySourceDataset.GetNameWithPath
        mysourceProjectName = mysourceProject.GetName

        'Determine device name based on dataset name...
        'FCM CHANGE - Added FCM Condition to set the proper DeviceName for FCM projects when adding project
        'This wont work correctly yet because we do not have any FCM100 files from which to build a project, so there will be no
        'dataset with FCM100 in the dataset name...

        If InStr(myDatasetName, "FCM100") > 0 Then
            deviceName = "FCM100"
        ElseIf InStr(myDatasetName, "FCM_") > 0 Then
            deviceName = "FCM"
        ElseIf InStr(myDatasetName, "ACP2") > 0 Then
            deviceName = "ACP2_MCU"
        ElseIf InStr(myDatasetName, "ACP3") > 0 Then
            deviceName = "ACP3_MCU"
        ElseIf InStr(myDatasetName, "ACP4") > 0 Then
            deviceName = "ACP4_MCU"
        Else
            deviceName = "Invalid"

            HandleUserMessageLogging("GMRC", "AddSelectedProjectToWorkspace: Invalid Device Name.  Unable to add Project to Workspace...", DisplayMsgBox,, FlashMsgOn, mylistbox)
            UserStatusInfo.Hide()
            AddSelectedProjectToWorkspace = False
            Exit Function
        End If

        projectDatabasePath = mysourceProject.GetParentFolder.GetNameWithPath

        MyHWC = myDestinationWorkspace

        MyHWC.Copy(workspaceName) 'Copy myDestinationWorkspace to WorkspaceName

        MyHWC = Get_Workspace(workspaceName) 'Set Hardware Configuration to WorkspaceName

        If MyHWC Is Nothing Then
            HandleUserMessageLogging("GMRC", "AddSelectedProjectToWorkspace: Get_Workspace could not find " & workspaceName,, )
            UserStatusInfo.Hide()
            AddSelectedProjectToWorkspace = False
            Exit Function
        End If

        mySourceDatasetPath = mySourceDataset.GetNameWithPath

        AddSelectedProjectToWorkspace = ChangeWorkspaceDataset("Ethernet-System:1", deviceName, mysourceProjectName, projectDatabasePath, mySourceDatasetPath)

    End Function

    Public Function ChangeWorkspaceDataset(ByVal systemName As String, ByVal deviceName As String, ByVal projName As String, ByVal projPath As String, ByVal datasetFullname As String) As Boolean

        'Called from AddSelectedProjectToWorspace and from ModifyWorkspaces (ModfiyWorkspaces is used for CSAV2)...

        'Changes the workspace dataset based on the parameters passed in.  This is where we bring the dataset from project created from a2l and ptp files
        'or copied from selected workspace in the case of CSAV2, into the newly created workspace that is specific to the vehicle instrumentation...

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
                    HandleUserMessageLogging("GMRC", "ChangeWorkspaceDataset: INCA Project " & projPath & "\" & projName & " not found.", DisplayMsgBox)
                End If

            Else
                HandleUserMessageLogging("GMRC", "ChangeWorkspaceDataset: " & deviceName & " not found in " & systemName, DisplayMsgBox)
            End If
        Else
            HandleUserMessageLogging("GMRC", "ChangeWorkspaceDataset: " & systemName & " not found.  This error is likely due to the Video Addon not being installed.  Please Exit CLEVIR and INCA and Install the INCA Video Addon. Then retry this operation.", DisplayMsgBox, )
        End If

    End Function

    Public Sub CopyFlashInfoTofile()

        'Changed instances of NetworkDriveLetter to NetworkDriveMapping 02/14/2021

        Dim fnum As Integer
        Dim filename As String
        'Dim SaveProjectFilesNoPath(0 To 3) As String
        Dim saveProjectFilesNoPath(0 To 5) As String
        Dim x As Integer
        Dim foundVehicleNumber As Boolean = False
        Dim textline As String = ""
        Dim saveTextLine() As String = Nothing

        Try

            HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Called...")

            If NetworkDrivePermission = False Then
                HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Could not access " & NetworkDriveMapping & CLEVIRBaseDir & ". Exiting...")
                Exit Sub
            End If

            'FCM CHANGE - Changed for loop to allow adding flashing info for up to three devices to accommodate FCM HIGH flavor which has three processors...
            'For x = 0 To 3
            For x = 0 To 5
                If InStr(SaveProjectFiles(x), "N/A") = 0 Then
                    saveProjectFilesNoPath(x) = Path.GetFileName(SaveProjectFiles(x))
                Else
                    saveProjectFilesNoPath(x) = SaveProjectFiles(x)
                End If
            Next

            If Not Directory.Exists(NetworkDriveMapping & CLEVIRBaseDir & "\VehicleFlashInfo") Then
                HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Creating Directory " & NetworkDriveMapping & CLEVIRBaseDir & "\VehicleFlashInfo")
                Directory.CreateDirectory((NetworkDriveMapping & CLEVIRBaseDir & "\VehicleFlashInfo"))
            End If

            filename = NetworkDriveMapping & CLEVIRBaseDir & "\VehicleFlashInfo\VehicleFlashInfo.csv"

            'FCM CHANGE - Changed PrintLine to print up to three processors of information (or N/A) rather than a maximum of two...
            If Not File.Exists(filename) Then

                HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Creating File " & filename)
                fnum = FreeFile()
                FileOpen(fnum, filename, OpenMode.Append)
                PrintLine(fnum, "VehicleNumber,Date,Workspace/A2l File,PTP File,A2l File,PTP File,A2l File,PTP File")
                HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Adding Flash Info to " & filename)
                'PrintLine(fnum, VehicleNumber & "," & Format(DateTime.Now, "MMddyyyy") & "," & SaveProjectFilesNoPath(0) & "," & SaveProjectFilesNoPath(1) & "," & SaveProjectFilesNoPath(2) & "," & SaveProjectFilesNoPath(3))
                PrintLine(fnum, VehicleNumber & "," & Format(DateTime.Now, "MMddyyyy") & "," & saveProjectFilesNoPath(0) & "," & saveProjectFilesNoPath(1) & "," & saveProjectFilesNoPath(2) & "," & saveProjectFilesNoPath(3) & "," & saveProjectFilesNoPath(4) & "," & saveProjectFilesNoPath(5))
                FileClose(fnum)

            Else
                HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: Adding Flash Info to " & filename)
                fnum = FreeFile()

                FileOpen(fnum, filename, OpenMode.Input)
                x = 0
                Do While Not EOF(fnum)
                    textline = LineInput(fnum)
                    If InStr(textline, VehicleNumber) > 0 Then
                        foundVehicleNumber = True
                    End If
                    ReDim Preserve saveTextLine(x)
                    saveTextLine(x) = textline
                    x += 1
                Loop

                FileClose(fnum)
                fnum = FreeFile()

                If foundVehicleNumber = False Then

                    FileOpen(fnum, filename, OpenMode.Append)
                    'PrintLine(fnum, VehicleNumber & "," & Format(DateTime.Now, "MMddyyyy") & "," & SaveProjectFilesNoPath(0) & "," & SaveProjectFilesNoPath(1) & "," & SaveProjectFilesNoPath(2) & "," & SaveProjectFilesNoPath(3))
                    PrintLine(fnum, VehicleNumber & "," & Format(DateTime.Now, "MMddyyyy") & "," & saveProjectFilesNoPath(0) & "," & saveProjectFilesNoPath(1) & "," & saveProjectFilesNoPath(2) & "," & saveProjectFilesNoPath(3) & "," & saveProjectFilesNoPath(4) & "," & saveProjectFilesNoPath(5))

                Else
                    FileOpen(fnum, filename, OpenMode.Output)
                    For x = 0 To UBound(saveTextLine)
                        If InStr(saveTextLine(x), VehicleNumber) > 0 Then
                            'PrintLine(fnum, VehicleNumber & "," & Format(DateTime.Now, "MMddyyyy") & "," & SaveProjectFilesNoPath(0) & "," & SaveProjectFilesNoPath(1) & "," & SaveProjectFilesNoPath(2) & "," & SaveProjectFilesNoPath(3))
                            PrintLine(fnum, VehicleNumber & "," & Format(DateTime.Now, "MMddyyyy") & "," & saveProjectFilesNoPath(0) & "," & saveProjectFilesNoPath(1) & "," & saveProjectFilesNoPath(2) & "," & saveProjectFilesNoPath(3) & "," & saveProjectFilesNoPath(4) & "," & saveProjectFilesNoPath(5))
                        Else
                            PrintLine(fnum, saveTextLine(x))
                        End If
                    Next x
                End If

                FileClose(fnum)

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CopyFlashInfoToFile: " & ex.Message)
        End Try

    End Sub

    Public Sub CopyVersionSpecificWorkspaceTemplate()

        'Called from CopyWorkspaceTemplateNEW and ModifyWorkspaces.  This routine is only called if the CLEVIR administrator needs
        'to create New templates for a new software version or model year...

        'Copies (and modifies as necessary), existing master template files to new names for new software version and model year
        'for the selected vehicle type.

        '154_MY22_HC_2P
        '154_MY22_LC_1P
        '153_MY22_CSAV2_3P3R
        '153_MY22_CSAV2_3P3R523
        '153_MY22_CSAV2_3P3R592

        Dim INCAWorkspaceTemplateName() As String
        Dim newWorkspaceName() As String

        Dim myFolder As Folder

        Dim importRequired As Boolean
        Dim copyRequired As Boolean
        Dim myDatabaseItems() As DataBaseItem

        Dim x As Integer

        Dim keepOriginal As Boolean

        ReDim INCAWorkspaceTemplateName(0)
        ReDim newWorkspaceName(0)

        'Need to add functionality here to comprehend FCMConfigName to determine how to set up the workspaces based on 
        'how many controllers we need. The templates for LC, HCS and ACP3 are set up with all FCM processors, so these
        'processors must be deleted or the correct one retained here for this to work...

        'This means that the specific vehicle number must be used when setting up the software version and model year
        'specific templates.  This also means multiple templates must be created and maintained for each possible
        'combination and that the first step in the process must always be the vehicle configuration step prior to
        'creating software version and model year templates.

        Dim fcmType As String = ""

        'Here we need to see if there is an FCM associated with the primary controller...
        'We may need to differentiate further here based on supplier...
        If InStr(FCMConfigName, "FCM") > 0 Then
            fcmType = Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1)
        End If

        'Here we determine the workspace templates and template names that will need to be created based on the vehicle type selected...

        Select Case GProjectAbbreviation 'Indicates vehicle type.  This global was set based on the vehicle number selected when we read in the vehicleconfigurations.csv file on startup...

            Case "LC"
                INCAWorkspaceTemplateName(0) = GProjectAbbreviation & "_WorkspaceTemplate"

                If Len(fcmType) = 0 Then
                    newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_1P"
                Else
                    newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_" & fcmType & "_2P"
                End If

            Case "HC"
                INCAWorkspaceTemplateName(0) = GProjectAbbreviation & "_WorkspaceTemplate"
                If Len(fcmType) = 0 Then
                    newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_2P"
                Else
                    newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_" & fcmType & "_3P"
                End If

            Case "CSAV2"
                ReDim INCAWorkspaceTemplateName(2)
                INCAWorkspaceTemplateName(0) = "CSAV2_3P3R"
                INCAWorkspaceTemplateName(1) = "CSAV2_3P3R523"
                INCAWorkspaceTemplateName(2) = "CSAV2_3P3R592"
                ReDim newWorkspaceName(2)
                newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & "_" & GProjectAbbreviation & "_3P3R"
                newWorkspaceName(1) = GSoftwareVersion & "_MY" & GModelYear & "_" & GProjectAbbreviation & "_3P3R523"
                newWorkspaceName(2) = GSoftwareVersion & "_MY" & GModelYear & "_" & GProjectAbbreviation & "_3P3R592"

                'FCM CHANGE - Added FCM Case...
            Case "FCM", "FCM100"
                INCAWorkspaceTemplateName(0) = GProjectAbbreviation & "_WorkspaceTemplate"
                newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_1P" 'This is FCM or FCM100 STA (Standalone) FCM Only
            Case "ACP2"
                ReDim INCAWorkspaceTemplateName(1)
                INCAWorkspaceTemplateName(0) = GProjectAbbreviation & "_WorkspaceTemplate"
                INCAWorkspaceTemplateName(1) = GProjectAbbreviation & "_1P886"
                ReDim newWorkspaceName(1)
                newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_1P"
                newWorkspaceName(1) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_1P886"
            Case "ACP3"
                ReDim INCAWorkspaceTemplateName(1)
                INCAWorkspaceTemplateName(0) = GProjectAbbreviation & "_WorkspaceTemplate"
                INCAWorkspaceTemplateName(1) = GProjectAbbreviation & "_1P886"
                ReDim newWorkspaceName(1)
                If Len(fcmType) = 0 Then
                    newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_1P"
                    newWorkspaceName(1) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_1P886"
                Else
                    newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_" & fcmType & "_2P"
                    newWorkspaceName(1) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_" & fcmType & "_2P886"
                End If
            Case "ACP4"
                'ReDim INCAWorkspaceTemplateName(1)
                ReDim INCAWorkspaceTemplateName(3)
                INCAWorkspaceTemplateName(0) = GProjectAbbreviation & "_WorkspaceTemplate"
                INCAWorkspaceTemplateName(1) = GProjectAbbreviation & "_1P886"
                INCAWorkspaceTemplateName(2) = GProjectAbbreviation & "_WorkspaceTemplate_RTK"
                INCAWorkspaceTemplateName(3) = GProjectAbbreviation & "_RTK_1P886"
                'ReDim NewWorkspaceName(1)
                ReDim newWorkspaceName(3)
                newWorkspaceName(0) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_1P"
                newWorkspaceName(1) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_1P886"
                newWorkspaceName(2) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_RTK_1P"
                newWorkspaceName(3) = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & GProjectAbbreviation & "_RTK_1P886"

        End Select

        myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")

        For x = 0 To UBound(INCAWorkspaceTemplateName)

            keepOriginal = False
            myDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName(x))

            'If we do not find the template workspace in INCA, we need to find the exported file somewhere, or look for the old filename format version...
            If myDatabaseItems.Length = 0 Then

                'We must have the WorkspaceTemplate somewhere, either in INCA, or in the CLEVIR Install directory, otherwise we will exit...
                If InStr(INCAWorkspaceTemplateName(x), "_WorkspaceTemplate") > 0 Then

                    HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName(x) & " in " & myFolder.GetName & ", looking in install directory...",,, FlashMsgOn)

                    'We look for the template in the install directory, 
                    If File.Exists(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName(x) & ".exp") Then

                        importRequired = True
                        copyRequired = True

                    Else
                        HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName(x) & " in install directory.  This workspace is required to create new CLEVIR configuration files. Exiting...", DisplayMsgBox, )
                        UserStatusInfo.Hide()
                        ConfigureForNewSoftwareVersion = False
                        Exit Sub
                    End If

                Else ' If we want to use a hardware specific template, such as an 886, if it does not exist, as is the case here, we can build if from the _WorkspaceTemplate
                    ' Which we do here, before creating the version specific one. We always check for the _WorkspaceTemplate first, so if we get this far, we know it exists...           

                    If AddNewWorkspaceFromTemplate(INCAWorkspaceTemplateName(x)) = True Then

                        myDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName(x))

                        copyRequired = True
                    Else
                        HandleUserMessageLogging("GMRC", "CopyVersionSpecificWorkspaceTemplate: AddNewWorkspaceFromTemplateNEW returned False, Exiting...", DisplayMsgBox, )
                        UserStatusInfo.Hide()
                        ConfigureForNewSoftwareVersion = False
                        Exit Sub
                    End If
                End If

            Else
                copyRequired = True
            End If

            UserStatusInfo.Hide()

            If importRequired = True Then

                ImportFileIntoINCA(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName(x) & ".exp", True, False)
                myDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName(x))

            End If

            If copyRequired = True Then

                Dim myTempDatabaseItems() As DataBaseItem

                myTempDatabaseItems = myFolder.BrowseDataBaseItem(newWorkspaceName(x))

                If myTempDatabaseItems.Length > 0 Then

                    If MsgBox("Workspace " & newWorkspaceName(x) & " already exists, recreate?", vbYesNo) = vbYes Then
                        HandleUserMessageLogging("GMRC", "Copying template workspace to " & newWorkspaceName(x),,, FlashMsgOn)

                        myFolder.RemoveComponent(myTempDatabaseItems(0))
                        myDatabaseItems(0).Copy(newWorkspaceName(x))
                    Else
                        keepOriginal = True
                    End If

                Else
                    HandleUserMessageLogging("GMRC", "Copying template workspace to " & newWorkspaceName(x),,, FlashMsgOn)
                    myDatabaseItems(0).Copy(newWorkspaceName(x))
                End If

                If keepOriginal = False Then

                    'Here is where we need to remove processor devices from newly copied NewWorkspaceName(x) based on if FCM, FCM100, or no FCM is being used...

                    HandleUserMessageLogging("GMRC", "Modifying new workspace based on " & newWorkspaceName(x) & " contents...",, )

                    If AddNewWorkspaceFromTemplate(newWorkspaceName(x)) = False Then
                        HandleUserMessageLogging("GMRC", "CopyVersionSpecificWorkspaceTemplate: AddNewWorkspaceFromTemplateNEW returned False, Exiting...", DisplayMsgBox, )
                        UserStatusInfo.Hide()
                        ConfigureForNewSoftwareVersion = False
                        Exit Sub
                    End If

                    'Prompt user to map ARXML clusters or DBC files to CAN devices (This has to be a manual operation, have not figured out how to automate this step...)

                    If MsgBox("Do you wish to associate new CAN information to CAN Monitoring Channels in " & newWorkspaceName(x) & " before proceeding?", vbYesNo) = vbYes Then

                        HandleUserMessageLogging("GMRC", "Please associate new CAN information to CAN Monitoring Channels in " & newWorkspaceName(x) & " before proceeding...", DisplayMsgBox, )

                        If MsgBox("Export " & newWorkspaceName(x) & " to " & My.Application.Info.DirectoryPath & " directory now?", vbYesNo) = vbYes Then
                            HandleUserMessageLogging("GMRC", "Export " & newWorkspaceName(x) & "? yes.",, )
                            myTempDatabaseItems = myFolder.BrowseDataBaseItem(newWorkspaceName(x))
                            If myTempDatabaseItems.Length > 0 Then
                                myTempDatabaseItems(0).ExportToFile(My.Application.Info.DirectoryPath & "\" & myTempDatabaseItems(0).GetName & ".exp", False, True)
                            End If

                        Else
                            HandleUserMessageLogging("GMRC", "Export " & newWorkspaceName(x) & "? no.",, )

                        End If

                    Else
                        HandleUserMessageLogging("GMRC", "Associate new CAN information? No.",, )
                    End If

                End If

            End If

        Next x

        UserStatusInfo.Hide()

    End Sub

    Private Function CopyWorkspaceTemplateNew(ByRef newWorkspaceName As String, ByVal mylistbox As ListBox) As HardwareConfiguration

        'Called from A2lAndCALToVehicleSpecificWorkspace - checks for existance of
        'INCAWorkspaceTemplateName, if found in INCA it copies the template to a newworkspacename that has been built based on
        'the selected PTP file and workspace template name.  If template not found, looks in app folder for the software verison specific template  
        'If template found Then it is imported into INCA and then copied to the newworkspacename.

        'This functionality requires that template files for each model year and software version be created manually and made available
        'in the application install directory.  This is due to the fact that the ARXML files may change with either a new software
        'version or for a new model year.  Once these files are created, they should be placed into the 
        'Q:" & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\UpdatedFiles folder so that they are automatically copied
        'into the CLEVIR install directory when CLEVIR is launched...

        'If the proper files are not found, a generic template will be used to create the workspace.  This will likely cause data collection
        'issues due to the fact that the ARXML file in the workspace may not be consistent with the experiment that corresponds to the
        'software version and model year.

        Dim myFolder As Folder

        Dim importRequired As Boolean
        Dim copyRequired As Boolean
        Dim myDatabaseItems() As DataBaseItem

        CopyWorkspaceTemplateNew = Nothing

        myFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")

        'If we are configuring the files for a new major software release, or new model year, we will have some additonal work to do
        'in conjunction with creating the new workspace.  This option (ConfigureForNewSoftwareVersion = True) is only available if
        'running from the design environment or running as CLEVIR Administrator and is not intended for the typical user...
        If ConfigureForNewSoftwareVersion = True Then

            ReadAutosarFile()
            'If we are creating new CLEVIR configuraiton files for a new software version or model year, we will first have to
            'create the software version / model year specific template from the master template that corresponds to the vehicle type
            'selected...
            CopyVersionSpecificWorkspaceTemplate()

        End If

        'check for existing templateworkspacename here...

        myDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName) 'This is the model year/software version specific template, which should be in the install folder...

        'If we do not find the template workspace in INCA, we need to look in install folder, or look for the generic version that is not updated to the latest software...
        If myDatabaseItems.Length = 0 Then

            HandleUserMessageLogging("GMRC", INCAWorkspaceTemplateName & " Not found in INCA, Looking in install folder, Please wait...",,, FlashMsgOn)

            'We look for the template in the install directory, 
            If File.Exists(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp") Then

                HandleUserMessageLogging("GMRC", "Found " & INCAWorkspaceTemplateName & " in install directory...",,,, mylistbox)

                UserStatusInfo.Hide()
                importRequired = True
                copyRequired = True

            Else 'Did not find INCAWorkspaceTemplateName in install folder...

                UserStatusInfo.Hide()

                If InStr(ProjectName, "ACP") = 0 Or (InStr(ProjectName, "ACP") > 0 And InStr(INCAWorkspaceTemplateName, "NOCAN") = 0) Then

                    'If the INCAWorkspaceTemplateName does not exist in the install folder we will use the generic template to create the workspace...
                    'Set template name back to saved template file name which is established in ReadVehicleConfigsFile before we know the software version, model year, etc...

                    'If this condition occurs, we may lose some CAN data because the master template may not have the same ARXML file
                    'associated with it as the model year and software version specific experiment!!!

                    HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & ", looking for Default Workspace Template...",,,, mylistbox)

                    INCAWorkspaceTemplateName = GSaveIncaWorkspaceTemplateName
                    WorkspaceNameSuffix = GSaveWorkspaceNameSuffix

                    'High content vehicles workspace names, because they use two processors are made up of a combination of HCS and HCF ptp filenames, so
                    'we call DetermineNewWorkspaceName to build the workspace name here, but only for High Content vehicles...
                    If ProjectName = "HighContent" Then
                        newWorkspaceName = DetermineNewWorkspaceName(SaveProjectFiles(1), SaveProjectFiles(3)) 'DetermineNewWorkspaceName adds & "_" & WorkspaceNameSuffix
                    Else
                        newWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix
                    End If

                    myDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName)

                    If myDatabaseItems.Length = 0 Then

                        If File.Exists(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp") Then

                            HandleUserMessageLogging("GMRC", "Found " & INCAWorkspaceTemplateName & " in install directory...",,,, mylistbox)
                            importRequired = True
                            copyRequired = True

                        Else

                            'can't assume that the default template name will be found.  here we should use the ProjectName_WorkspaceTemplate to create one with the name GSaveIncaWorkspaceTemplateName...

                            'If AddNewWorkspaceFromTemplate(INCAWorkspaceTemplateName) = False Then
                            If AddNewWorkspaceFromTemplate(INCAWorkspaceTemplateName) = False Then

                                HandleUserMessageLogging("GMRC", INCAWorkspaceTemplateName & ".exp" & " Not found. Failed to Copy workspace template...", DisplayMsgBox,, FlashMsgOn, mylistbox)
                                UserStatusInfo.Hide()
                                Exit Function

                            Else
                                UserStatusInfo.Hide()
                                myDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName)
                                copyRequired = True
                            End If

                        End If

                    Else 'if we find the old format workspace template in INCA, we use it to create the new workspace.
                        copyRequired = True
                    End If

                Else 'ProjectName is ACP project and workspace template defined with NOCAN, so we have to handle NOCAN condition...

                    HandleUserMessageLogging("GMRC", "Did Not find INCA Workspace - " & INCAWorkspaceTemplateName & ", creating NOCAN template...",,,, mylistbox)

                    newWorkspaceName = Mid(Path.GetFileName(SaveProjectFiles(1)), 1, Len(Path.GetFileName(SaveProjectFiles(1))) - 4) & "_" & WorkspaceNameSuffix

                    'can't assume that the default template name will be found.  here we should use the ProjectName_WorkspaceTemplate to create one with the name GSaveIncaWorkspaceTemplateName...

                    'If AddNewWorkspaceFromTemplate(INCAWorkspaceTemplateName) = False Then
                    If AddNewWorkspaceFromTemplate(INCAWorkspaceTemplateName) = False Then

                        HandleUserMessageLogging("GMRC", INCAWorkspaceTemplateName & ".exp" & " Not found. Failed to Copy workspace template...", DisplayMsgBox,, FlashMsgOn, mylistbox)
                        UserStatusInfo.Hide()
                        Exit Function

                    Else
                        UserStatusInfo.Hide()
                        myDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName)
                        copyRequired = True
                    End If

                End If

            End If

        Else 'software version specific template already exists in INCA...
            copyRequired = True
        End If

        If importRequired = True Then

            HandleUserMessageLogging("GMRC", "Importing " & INCAWorkspaceTemplateName & ".exp",,,, mylistbox)
            ImportFileIntoINCA(My.Application.Info.DirectoryPath & "\" & INCAWorkspaceTemplateName & ".exp", True, False)
            myDatabaseItems = myFolder.BrowseDataBaseItem(INCAWorkspaceTemplateName)

        End If

        If copyRequired = True Then
            HandleUserMessageLogging("GMRC", "Copying template workspace to " & newWorkspaceName,,,, mylistbox)

            Dim myTempDatabaseItems() As DataBaseItem

            myTempDatabaseItems = myFolder.BrowseDataBaseItem(newWorkspaceName)

            If myTempDatabaseItems.Length > 0 Then
                myFolder.RemoveComponent(myTempDatabaseItems(0))
            End If

            myDatabaseItems(0).Copy(newWorkspaceName)

            HandleUserMessageLogging("GMRC", "Copying complete.",,,, mylistbox)

        End If

        If SetupCameraNamesInWorkspace(newWorkspaceName) = True Then

            CopyWorkspaceTemplateNew = Get_Workspace(newWorkspaceName, "CLEVIR Setup\Workspaces")

        Else
            HandleUserMessageLogging("GMRC", "Camera configuration did Not complete successfully.", DisplayMsgBox, )
        End If

        UserStatusInfo.Hide()

    End Function

    Private Function CreateA2LptpProject(ByVal a2LFile As String, ByVal ptpfile As String, ByVal mylistbox As ListBox) As Asap2Project

        'Called from A2lAndCalToVehicleSpecificWorkspace

        'Creates a new project from a2l and ptp file.  Places project into CLEVIR Setup\Projects INCA folder...

        Dim versionFolderName As String
        Dim tempstr As String
        Dim devicename As String
        Dim lProjectname As String
        Dim myfolder As IncaFolder = Nothing
        Dim mysubfolder As IncaFolder = Nothing
        Dim mysubsubfolder As IncaFolder = Nothing
        Dim mysubsubsubfolder As IncaFolder = Nothing

        Dim myAsap2Project As Asap2Project = Nothing

        Dim myClevirProjectsSubfolder As IncaFolder = Nothing

        Dim returnStr As String = ""

        Dim myDatabaseItems() As DataBaseItem
        Dim createProject As Boolean

        CreateA2LptpProject = Nothing

        'Connect to INCA, or verify already connected...
        returnStr = MyIncaInterface.ConnectToInca()

        If returnStr = "True" Then

            HandleUserMessageLogging("GMRC", "A2l Filename: " & a2LFile)

            'Determine devicename based on a2l filename...

            tempstr = Path.GetFileName(a2LFile)



            If InStr(tempstr, "ACP2") > 0 Then

                devicename = "ACP2"
                lProjectname = "ACP2"
                tempstr = Mid(tempstr, InStr(tempstr, "ACP2") + 7, 9)
                versionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "ACP3") > 0 Then

                devicename = "ACP3"
                lProjectname = "ACP3"
                tempstr = Mid(tempstr, InStr(tempstr, "ACP3") + 7, 9)
                versionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            ElseIf InStr(tempstr, "ACP4") > 0 Then

                devicename = "ACP4"
                lProjectname = "ACP4"
                tempstr = Mid(tempstr, InStr(tempstr, "ACP4") + 7, 9)
                versionFolderName = Mid(tempstr, 1, 2) & "." & Mid(tempstr, 3, 2) & "." & Mid(tempstr, 5, 3) & "." & Mid(tempstr, 8, 2)

            Else
                devicename = "Invalid"
                HandleUserMessageLogging("GMRC", "Invalid a2l file selected, Project not created...", DisplayMsgBox,,, mylistbox)
                UserStatusInfo.Hide()
                Exit Function
            End If


            'Find or add folders that project will be created in...
            HandleUserMessageLogging("GMRC", "Getting folder for " & lProjectname)
            myfolder = myActualDatabase.GetFolder(lProjectname)

            If lProjectname = "CSAV2" Then

                If myfolder IsNot Nothing Then
                    HandleUserMessageLogging("GMRC", "Found " & ProjectName & " folder")
                    mysubfolder = myfolder.GetSubFolder("DV3")
                    If mysubfolder IsNot Nothing Then
                        HandleUserMessageLogging("GMRC", "Found DV3 folder")
                        mysubsubfolder = mysubfolder.GetSubFolder("Projects")
                        If mysubsubfolder IsNot Nothing Then
                            mysubsubsubfolder = mysubsubfolder.GetSubFolder(devicename)
                            If mysubsubsubfolder Is Nothing Then
                                mysubsubsubfolder = mysubsubfolder.AddSubFolder(devicename)
                                myClevirProjectsSubfolder = mysubsubsubfolder.AddSubFolder(versionFolderName)
                            Else
                                myClevirProjectsSubfolder = If(mysubsubsubfolder.GetSubFolder(versionFolderName), mysubsubsubfolder.AddSubFolder(versionFolderName))
                            End If

                        Else
                            mysubsubfolder = mysubfolder.AddSubFolder("Projects")
                            mysubsubsubfolder = mysubsubfolder.AddSubFolder(devicename)
                            myClevirProjectsSubfolder = mysubsubsubfolder.AddSubFolder(versionFolderName)
                        End If

                    Else
                        mysubfolder = myfolder.AddSubFolder("DV3")
                        mysubsubfolder = mysubfolder.AddSubFolder("Projects")
                        mysubsubsubfolder = mysubsubfolder.AddSubFolder(devicename)
                        myClevirProjectsSubfolder = mysubsubsubfolder.AddSubFolder(versionFolderName)
                    End If

                Else
                    myfolder = myActualDatabase.AddFolder(lProjectname)
                    mysubfolder = myfolder.AddSubFolder("DV3")
                    mysubsubfolder = mysubfolder.AddSubFolder("Projects")
                    mysubsubsubfolder = mysubsubfolder.AddSubFolder(devicename)
                    myClevirProjectsSubfolder = mysubsubsubfolder.AddSubFolder(versionFolderName)

                End If

                lProjectname &= "\DV3"

            Else 'Not CSAV2

                If myfolder IsNot Nothing Then
                    HandleUserMessageLogging("GMRC", "Found " & lProjectname & " folder")
                    mysubfolder = myfolder.GetSubFolder("Projects")
                    If mysubfolder IsNot Nothing Then
                        HandleUserMessageLogging("GMRC", "Found Projects folder - Getting " & devicename & " subfolder")
                        mysubsubfolder = mysubfolder.GetSubFolder(devicename)
                        If mysubsubfolder Is Nothing Then
                            HandleUserMessageLogging("GMRC", "Adding sub-folder for " & devicename)
                            mysubsubfolder = mysubfolder.AddSubFolder(devicename)
                            HandleUserMessageLogging("GMRC", "Adding sub-folder for " & versionFolderName)
                            myClevirProjectsSubfolder = mysubsubfolder.AddSubFolder(versionFolderName)
                        Else
                            HandleUserMessageLogging("GMRC", "Found " & devicename & " folder")
                            myClevirProjectsSubfolder = mysubsubfolder.GetSubFolder(versionFolderName)
                            If myClevirProjectsSubfolder Is Nothing Then
                                HandleUserMessageLogging("GMRC", "Adding sub-folder for " & versionFolderName)
                                myClevirProjectsSubfolder = mysubsubfolder.AddSubFolder(versionFolderName)
                            End If
                        End If
                    Else
                        HandleUserMessageLogging("GMRC", "Adding Projects sub-folder for " & lProjectname)
                        mysubfolder = myfolder.AddSubFolder("Projects")
                        HandleUserMessageLogging("GMRC", "Adding sub-folder for " & devicename)
                        mysubsubfolder = mysubfolder.AddSubFolder(devicename)
                        myClevirProjectsSubfolder = mysubsubfolder.AddSubFolder(versionFolderName)
                    End If

                Else

                    HandleUserMessageLogging("GMRC", "Adding folder for " & lProjectname)
                    myfolder = myActualDatabase.AddFolder(lProjectname)
                    HandleUserMessageLogging("GMRC", "Adding Projects sub-folder for " & lProjectname)
                    mysubfolder = myfolder.AddSubFolder("Projects")
                    HandleUserMessageLogging("GMRC", "Adding sub-folder for " & devicename)
                    mysubsubfolder = mysubfolder.AddSubFolder(devicename)
                    HandleUserMessageLogging("GMRC", "Adding sub-folder for " & versionFolderName)
                    myClevirProjectsSubfolder = mysubsubfolder.AddSubFolder(versionFolderName)

                End If
            End If

            'Create the project from the A2l and PTP files...

            If myClevirProjectsSubfolder IsNot Nothing Then

                'If we are creating new files for a new software version, we will check to see if the a2l file has already been used to create a project
                'if so, we will not create a new project. Saves time, we do not really care about which ptp file is used, so dataset does not really matter here...
                If ConfigureForNewSoftwareVersion = True Then

                    Dim tempname As String
                    tempname = Mid(System.IO.Path.GetFileName(a2LFile), 1, InStr(System.IO.Path.GetFileName(a2LFile), ".a2l") - 1)

                    myDatabaseItems = myClevirProjectsSubfolder.BrowseDataBaseItem(tempname)
                    If myDatabaseItems.Length <> 0 Then
                        createProject = False
                    Else
                        createProject = True
                    End If

                    If createProject = True Then

                        HandleUserMessageLogging("GMRC", "myCLEVIRProjectsSubFolder = " & myClevirProjectsSubfolder.GetNameWithPath,,,, mylistbox)
                        HandleUserMessageLogging("GMRC", "Creating Project from A2l And ptp files...",,, FlashMsgOn, mylistbox)
                        HandleUserMessageLogging("GMRC", "A2l File = " & a2LFile,,,, mylistbox)
                        HandleUserMessageLogging("GMRC", "PTP File = " & ptpfile,,,, mylistbox)

                        'We have seen rare instances where this INCA API method call hangs up.  Do not know why...
                        'If this happens, it requires killing INCA and CLEVIR processes from the Windows Task Manager...

                        myAsap2Project = myClevirProjectsSubfolder.ReadASAP2FileAndHexFile(a2LFile, ptpfile)

                        If myAsap2Project IsNot Nothing Then
                            CreateA2LptpProject = myAsap2Project
                        Else
                            HandleUserMessageLogging("GMRC", "Could Not create project...",,, FlashMsg1Sec, mylistbox)
                            Exit Function
                        End If

                        HandleUserMessageLogging("GMRC", "Project Creation Complete.  New Project Is in the " & lProjectname & "\Projects\" & devicename & "\" & versionFolderName & " folder.",,, FlashMsg2Sec, mylistbox)

                    Else
                        HandleUserMessageLogging("GMRC", "Project with the same name already exists. User chose not to create new project " & tempname,,,, mylistbox)
                        CreateA2LptpProject = myDatabaseItems(0)
                    End If

                Else 'if we are not creating new CLEVIR major version support files, we will always create a new project...

                    HandleUserMessageLogging("GMRC", "myCLEVIRProjectsSubFolder = " & myClevirProjectsSubfolder.GetNameWithPath,,,, mylistbox)
                    HandleUserMessageLogging("GMRC", "Creating Project from A2l And ptp files...",,, FlashMsgOn, mylistbox)
                    HandleUserMessageLogging("GMRC", "A2l File = " & a2LFile,,,, mylistbox)
                    HandleUserMessageLogging("GMRC", "PTP File = " & ptpfile,,,, mylistbox)

                    'We have seen rare instances where this INCA API method call hangs up.  Do not know why...
                    'If this happens, it requires killing INCA and CLEVIR processes from the Windows Task Manager...

                    myAsap2Project = myClevirProjectsSubfolder.ReadASAP2FileAndHexFile(a2LFile, ptpfile)

                    If myAsap2Project IsNot Nothing Then
                        CreateA2LptpProject = myAsap2Project
                    Else
                        HandleUserMessageLogging("GMRC", "Could Not create project...",,, FlashMsg1Sec, mylistbox)
                        Exit Function
                    End If

                    HandleUserMessageLogging("GMRC", "Project Creation Complete.  New Project Is in the " & lProjectname & "\Projects\" & devicename & "\" & versionFolderName & " folder.",,, FlashMsg2Sec, mylistbox)

                End If

            End If

        Else
            HandleUserMessageLogging("GMRC", "CreateA2lPTPProject: ConnectToInca returned - " & returnStr & " Could Not connect to INCA, Project Not created...", DisplayMsgBox,, FlashMsg1Sec, mylistbox)
        End If

    End Function


    Private Sub DetermineInitIncaProjectDir(ByRef initialDirectory As String)

        'Sets the initial inca project directory based on ProjectName...

        'If there is a properly configured flash drive connected, CLEVIR will go to this drive, defined by
        'NetworkDriveLetter, to look for a2l and ptp files rather than the network share drive...

        If UsingFlashDrive = True Then
            initialDirectory = NetworkDriveLetter & "\INCA Projects"
        Else
            'If we are looking on network share drive, we determine default folder based on project name.
            'this takes user to a location from which they can drill down to find the proper model year and
            'software version folder which contains a2l and ptp files...

            If PATAC = True Then
                initialDirectory = My.Application.Info.DirectoryPath & "\INCAProjects"
                Exit Sub
            End If

            If NetworkDrivePermission = False Then
                HandleUserMessageLogging("GMRC", "DetermineInitINCAProjectDir: Could not access " & NetworkDriveMapping & CLEVIRBaseDir & ". Exiting...")
                initialDirectory = My.Application.Info.DirectoryPath
                Exit Sub
            End If

            If Len(ProjectName) > 0 Then 'And FlashingStatus.RadioButton3.Checked = False Then

                'NETWORK DRIVE MAPPING

                Select Case ProjectName
                        'HC CHANGE
                    Case "HighContent"
                        initialDirectory = NetworkDriveMapping & "\EOCM3_HC\Calibration\INCA_Projects"
                        'FCM CHANGE - Added FCM case here to set up InitialDirectory for FCM projects...
                    Case "FCM", "FCM100"
                        initialDirectory = NetworkDriveMapping & "\FCM\Calibration\INCA_Projects" 'Need to add this folder to share drive...
                    Case "LowContent"
                        initialDirectory = NetworkDriveMapping & "\EOCM3_lo\Calibration\INCA_Projects"
                    Case "CSAV2"
                        initialDirectory = NetworkDriveMapping & "\Calibration\INCA Projects"
                    Case "ACP2"
                        initialDirectory = NetworkDriveMapping & "\ACP2\Calibration\INCA_Projects"
                    Case "ACP3"
                        initialDirectory = NetworkDriveMapping & "\ACP3\Calibration\INCA_Projects"
                    Case "ACP4"
                        initialDirectory = NetworkDriveMapping & "\ACP4\Calibration\INCA_Projects"
                    Case Else
                        initialDirectory = My.Application.Info.DirectoryPath
                End Select

            Else
                initialDirectory = My.Application.Info.DirectoryPath
            End If

        End If

    End Sub

    Public Function DetermineIfUsingDifferentArxml(ByVal referenceString As String) As String

        'GSpecificArxml = DetermineIfUsingDifferentArxml

        'Workspace, Experiment, VariableFile or A2l file...

        'Called from VerifyCLEVIRConfiguration, SelectA2lAndPTPFiles
        'Determines if there is an alternate ARXML file being used in the software based on ReferenceString passed in...
        'Slightly different behavior based on file type in ReferenceString...

        '144_8441_MY20_LC.xlsx
        '144_8441_MY21_LC.exp
        'ASE37_HCS_212114420CB_quasi.a2l
        'ASE34_LC_202014502_quasi.a2l
        'ASE34_LC_212114400_quasi.a2l
        'ASE37_212114420_20_FSI-019__HC_2P1C
        'ASE34_LC_212114400_K10906_L87_F48_UVZ_20190315__21_21_141R4_LC_1P1C
        'ACP30M1232315610AA_ASTA_quasi_INCA.a2l

        'ASE37_232315810AA_10AA_22XML_ASTA_10906_RWL_ChevGMC_L87_CLNNT411_04012021__158_MY23_HC_2P1C

        'If the ReferenceString passed in contains the full path, we will use just the filename...

        If InStr(referenceString, "XML_") = 0 Then
            DetermineIfUsingDifferentArxml = ""
            Exit Function
        Else

            If InStr(referenceString, "\") > 0 Then
                referenceString = System.IO.Path.GetFileName(referenceString)
            End If

            If InStr(referenceString, "XML_") >= 4 Then

                If IsNumeric(Mid(referenceString, InStr(referenceString, "XML_") - 2, 2)) And InStr(Mid(referenceString, InStr(referenceString, "XML_") - 3, 1), "_") > 0 Then
                    DetermineIfUsingDifferentArxml = "_" & Mid(referenceString, InStr(referenceString, "XML_") - 2, 5)
                Else
                    DetermineIfUsingDifferentArxml = ""
                    Exit Function
                End If

            Else
                DetermineIfUsingDifferentArxml = ""
                Exit Function
            End If

        End If

    End Function

    Public Function DetermineModelYear(ByVal referenceString As String) As String
        ' Robustly extract 2-digit model year (e.g., "26" from "180_MY26_ACP3_IP").
        ' Works for filenames and workspace names; never throws on null arrays.
        Try
            Dim nameOnly As String = referenceString
            If InStr(nameOnly, "\") > 0 Then
                nameOnly = Path.GetFileName(nameOnly)
            End If

            Dim upper As String = UCase(nameOnly)

            ' 1) Preferred pattern: _MYxx (or start/anywhere as "..._MY26_...")
            Dim idx As Integer = InStr(upper, "_MY")
            If idx > 0 AndAlso idx + 3 <= Len(nameOnly) Then
                Dim yy As String = Mid(nameOnly, idx + 3, 2)
                If IsNumeric(yy) Then Return yy
            End If

            ' 2) Any token like "MYxx"
            Dim parts() As String = nameOnly.Split("_"c)
            For Each p In parts
                If Len(p) >= 4 AndAlso UCase(Mid(p, 1, 2)) = "MY" AndAlso IsNumeric(Mid(p, 3, 2)) Then
                    Return Mid(p, 3, 2)
                End If
            Next

            ' 3) Fallback: look for a 2-digit numeric token that looks like a year (15..40)
            For Each p In parts
                If Len(p) = 2 AndAlso IsNumeric(p) Then
                    Dim v As Integer = Val(p)
                    If v >= 15 AndAlso v <= 40 Then
                        Return p
                    End If
                End If
            Next

            ' No match
            Return String.Empty
        Catch
            Return String.Empty
        End Try
    End Function

    Private Function DetermineNewWorkspaceName(ByVal hcsPtp As String, ByVal hcfPtp As String) As String

        'Called from TwoA2lsAndPTPsToVehicleSpecificWorkspace and TwoA2lsAndPTPsToVehicleSpecificWorkspaceOLD
        'Creates new workspace name based on selected ptp file names (combination of HCS and HCF ptp file names)...

        'So, this function is only applicable to High Content...

        Dim hcsFirstPart As String
        Dim hcfFirstPart As String
        Dim hcsMiddlePart As String
        Dim hcfMiddlePart As String
        Dim hcsSubVersion As String
        Dim hcfSubVersion As String
        Dim hcsEndPart As String
        Dim hcfEndPart As String

        Dim hcsLineitems() As String
        Dim hcfLineitems() As String

        hcsPtp = Mid(Path.GetFileName(hcsPtp), 1, Len(Path.GetFileName(hcsPtp)) - 4)
        hcfPtp = Mid(Path.GetFileName(hcfPtp), 1, Len(Path.GetFileName(hcfPtp)) - 4)

        hcsLineitems = Split(hcsPtp, "_")
        hcfLineitems = Split(hcfPtp, "_")

        hcsFirstPart = Mid(hcsPtp, 1, 6)
        hcfFirstPart = Mid(hcfPtp, 1, 6)

        If hcsFirstPart <> hcfFirstPart Then
            DetermineNewWorkspaceName = "Invalid_Controller_Naming"
        Else
            hcsMiddlePart = Mid(hcsPtp, 11, 7)
            hcfMiddlePart = Mid(hcfPtp, 11, 7)

            If hcsMiddlePart <> hcfMiddlePart Then
                DetermineNewWorkspaceName = "Inconsistent_HCS_HCF_Versions"
            Else
                hcsEndPart = Mid(hcsPtp, Len(hcsLineitems(0)) + 1 + Len(hcsLineitems(1)) + 1 + Len(hcsLineitems(2)) + 1, Len(hcsPtp))
                hcfEndPart = Mid(hcfPtp, Len(hcfLineitems(0)) + 1 + Len(hcfLineitems(1)) + 1 + Len(hcfLineitems(2)) + 1, Len(hcfPtp))

                hcsSubVersion = Mid(hcsLineitems(2), 8, Len(hcsLineitems(2)))
                hcfSubVersion = Mid(hcfLineitems(2), 8, Len(hcfLineitems(2)))

                DetermineNewWorkspaceName = hcsFirstPart & hcsMiddlePart & hcsSubVersion & "_" & hcfSubVersion & hcsEndPart & "_" & WorkspaceNameSuffix

            End If
        End If

    End Function

    Public Function DetermineSoftwareVersion(ByVal referenceString As String) As String
        ' Robustly extract 3-digit software version (e.g., "180" from "180_MY26_ACP3_IP").
        ' Handles:
        ' - Workspace/exp/csv/xlsx names starting with "###_"
        ' - FCM/FCM100 a2l names (uses digits in the 4th token, positions 5..7)
        ' - Any standalone "###" token
        ' - Fallback: take positions 5..7 from all digits if long enough
        Try
            Dim nameOnly As String = referenceString
            If InStr(nameOnly, "\") > 0 Then
                nameOnly = Path.GetFileName(nameOnly)
            End If

            Dim upper As String = UCase(nameOnly)

            ' 1) Leading "###_" pattern (common for workspaces and signal lists)
            If Len(nameOnly) >= 4 AndAlso IsNumeric(Mid(nameOnly, 1, 3)) AndAlso Mid(nameOnly, 4, 1) = "_" Then
                Return Mid(nameOnly, 1, 3)
            End If

            ' 2) FCM/FCM100 a2l format: take digits from 4th token, then positions 5..7
            If InStr(upper, ".A2L") > 0 AndAlso (InStr(upper, "FCM") > 0) Then
                Dim parts() As String = Split(nameOnly, "_"c)
                If UBound(parts) >= 3 Then
                    Dim s As String = parts(3)
                    Dim d As String = ""
                    Dim i As Integer
                    For i = 1 To Len(s)
                        Dim ch As String = Mid(s, i, 1)
                        If ch >= "0" AndAlso ch <= "9" Then d &= ch
                    Next
                    If Len(d) >= 7 Then
                        Return Mid(d, 5, 3)
                    End If
                End If
            End If

            ' 3) Any standalone 3-digit token
            Dim toks() As String = Split(nameOnly, "_"c)
            Dim t As String
            For Each t In toks
                If Len(t) = 3 AndAlso IsNumeric(t) Then
                    Return t
                End If
            Next

            ' 4) If it starts with digits (but no underscore), still accept the first three
            If Len(nameOnly) >= 3 AndAlso IsNumeric(Mid(nameOnly, 1, 3)) Then
                Return Mid(nameOnly, 1, 3)
            End If

            ' 5) Fallback: extract all digits and use positions 5..7 if long enough
            Dim allDigits As String = ""
            Dim k As Integer
            For k = 1 To Len(nameOnly)
                Dim ch As String = Mid(nameOnly, k, 1)
                If ch >= "0" AndAlso ch <= "9" Then allDigits &= ch
            Next
            If Len(allDigits) >= 7 Then
                Return Mid(allDigits, 5, 3)
            End If

            Return String.Empty
        Catch
            Return String.Empty
        End Try
    End Function

    Private Function FindA2LFileInDirectory(ByVal mydialog As FolderBrowserDialog, ByVal mylistbox As ListBox, ByVal processorName As String, ByVal myFileDialog As FileDialog) As String

        'Called from SelectA2lAndPTPFiles...

        'Finds a2l file in user selected folder and copies to CLEVIR install folder.  Assumes only one, if ProcessorName is "LC", or will find
        'a2l file that corresponds to ProcessorType (HCS or HCF) that is passed in.

        Dim dir As DirectoryInfo
        Dim sourceFile As String = ""
        Dim destFile As String = ""

        Dim numberOfA2LFiles As Integer

        Dim x As Integer
        Dim myfiles() As String

        FindA2LFileInDirectory = ""

        If Len(mydialog.SelectedPath) > 0 Then

            If FileSelectionMethod = "Semi-Automatic" Then

                If processorName <> "HCF" Then 'HCF processor is the second processor for which to find a2l file when configuring High Content vehicle and we do not want this msg to be displayed twice...

                    mylistbox.SelectionMode = SelectionMode.None

                    mylistbox.Items.Clear()

                    myfiles = Directory.GetFiles(mydialog.SelectedPath)

                    For x = 0 To UBound(myfiles)
                        mylistbox.Items.Add(Path.GetFileName(myfiles(x)))
                    Next

                    mylistbox.Refresh()

                    Using msgBox As New Cusmsgbox()
                        If msgBox.DisplayCusMsgBox(Nothing, "The selected folder contains the files indicated. CLEVIR will auto-select the .a2l file(s) and attempt to auto-select the .ptp (or .s19) file(s) based on the vehicle number selected. Is this folder selection contain the files you want to use?", "USER INPUT REQUIRED", "NO", "YES") = DialogResult.Yes Then
                            ' User clicked "YES", so continue.
                        Else
                            ' User clicked "NO" or closed the dialog.
                            FindA2LFileInDirectory = ""
                            mylistbox.Items.Clear()
                            mylistbox.SelectionMode = SelectionMode.One
                            Exit Function
                        End If
                    End Using

                    mylistbox.SelectionMode = SelectionMode.One

                    mylistbox.Items.Clear()

                End If

            End If


            'HandleUserMessageLogging("GMRC", "FindA2lFileInDirectory " & mydialog.SelectedPath,, )

            dir = New DirectoryInfo(mydialog.SelectedPath)
            Dim files As FileInfo() = dir.GetFiles()


            For Each file In files
                'FCM CHANGE - Changed check for len(ProcessorType) = 0 to check specifically for "LC" ProcessorType...
                'This because with FCM now, we need to be able to differentiate between LC a2l files and LCM or LCH a2l files...
                'This is done here by the fact that LC uses quasi.a2l and LCM and LCH use .a2l files with no quasi...
                If processorName = "LC" Then
                    If InStr(file.Name, "quasi.a2l") > 0 And InStr(file.Name, "LC") > 0 Then
                        sourceFile = mydialog.SelectedPath & "\" & file.Name
                        numberOfA2LFiles += 1
                    End If
                Else
                    'May need to make some FCM related changes here, need to figure out processortype...
                    If InStr(file.Name, ".a2l") > 0 And InStr(file.Name, processorName) > 0 Then
                        sourceFile = mydialog.SelectedPath & "\" & file.Name
                        numberOfA2LFiles += 1
                    End If
                End If

            Next file

            If numberOfA2LFiles = 0 Then
                HandleUserMessageLogging("GMRC", "Could not find a2l file for " & processorName & ". Please make sure that there is one valid .a2l file per processor in " & mydialog.SelectedPath & " and retry this operation...", DisplayMsgBox, )
                Exit Function
            ElseIf numberOfA2LFiles > 1 Then
                HandleUserMessageLogging("GMRC", "There are multiple a2l files for " & processorName & ". Please select the .a2l file you wish to use.", DisplayMsgBox, )
                InitialDirectory = mydialog.SelectedPath
                sourceFile = SelectFileByType(myFileDialog, "a2l", mylistbox)
            End If

            If Len(sourceFile) > 0 Then

                destFile = My.Application.Info.DirectoryPath & "\" & Mid(sourceFile, InStrRev(sourceFile, "\") + 1, Len(sourceFile))

                'The Save A2LFilename variables below are used only in conjunction with the administrator task of creating new CLEVIR support files
                'for a new major software version.  These would only be used if the Configure for New Software Verion (Checkbox1) was checked
                'on the FlashingStatus form, which is only visible if running with debugger.isattached = true...

                Select Case processorName
                    Case "HCF"
                        SaveHcfA2LFilename = destFile
                    Case "HCS"
                        SaveHcsA2LFilename = destFile
                    Case "LC"
                        SaveLcA2LFilename = destFile
                        'FCM CHANGE - Added FCM Case here...
                    Case "FCM" 'In this context, FCM could be either FCM or FCM100 processor...
                        SaveFcmA2LFilename = destFile
                    Case "ACP2"
                        SaveAcp2A2LFilename = destFile
                    Case "ACP3"
                        SaveAcp3A2LFilename = destFile
                    Case "ACP4"
                        SaveAcp4A2LFilename = destFile
                End Select

                If Not System.IO.File.Exists(destFile) Then

                    HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(sourceFile) & ", Please wait...",,, FlashMsgOn)

                    RoboCopyFile(sourceFile, My.Application.Info.DirectoryPath)
                    If Not File.Exists(destFile) Then
                        FindA2LFileInDirectory = ""
                        UserStatusInfo.Hide()
                        Exit Function
                    End If

                End If

                FindA2LFileInDirectory = destFile

            End If

        End If

        UserStatusInfo.Hide()

    End Function

    Private Function FindPtpFileInDirectory(ByVal mydialog As FileDialog, ByVal selectedDirectory As String, ByVal mylistbox As ListBox, ByVal processorName As String, ByVal calFileExtension As String) As String

        'Finds appropriate ptp file in user selected folder based on vehicle number.  Uses lookup file VehiclePTPLookup.csv which maps vehicle number
        'to ptp file based on variant and cal differentators contained in file. If ptp file is found or selected by user, it is copied to CLEVIR install folder...

        Dim dir As DirectoryInfo = New DirectoryInfo(selectedDirectory)
        Dim files As FileInfo() = dir.GetFiles()
        Static lineitems() As String = Nothing
        Static templineitems() As String = Nothing

        Static saveVehicleNumber As String

        Dim numPasses As Integer = 0
        Dim numMatches As Integer = 0
        Dim saveFileName As String = ""

        Dim sourceFile As String = ""
        Dim destFile As String = ""

        Dim x As Integer
        Dim y As Integer = 0

        Const maxNumDifferentiators = 7

        'If vehicle number changes after initial PTP lookup file read, we will need to read file again for different vehiclenumber...
        If saveVehicleNumber <> VehicleNumber Then
            templineitems = Nothing
            lineitems = Nothing
            saveVehicleNumber = VehicleNumber
        End If

        FindPtpFileInDirectory = ""

        'Will only read the file if we have not done so already or if user has changed vehicle number...
        If templineitems Is Nothing Then
            'templineitems = ReadInVehiclePTPLookupFileOLD(My.Application.Info.DirectoryPath & "\VehiclePTPLookup.csv") 'gets line items associated with vehicle number, only need to do this once...
            templineitems = ProcessVehiclePtpLookupFileInfo()
        End If

        If templineitems IsNot Nothing Then

            If templineitems(0) <> "Invalid" Then

                If lineitems Is Nothing Then
                    'Look through each item in templineitems and if item has something in it, put its contents into lineitems,
                    'this so we have an array populated only with data and not blanks...
                    'For x = 1 To UBound(templineitems)
                    For x = 1 To maxNumDifferentiators
                        If Len(templineitems(x)) > 0 Then
                            y += 1
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

                For numPasses = 1 To UBound(lineitems)
                    For Each file In files

                        If ((processorName = "LC") Or (processorName = "FCM") Or (processorName = "FCM100") Or (processorName = "ACP2") Or (processorName = "ACP3") Or (processorName = "ACP4")) Then

                            If InStr(file.Name, "." & calFileExtension) > 0 Then
                                y = 0
                                'We assume here that we must find a cal file that contains every one of the lineitems (differentiators) defined in lookup file...
                                For x = 1 To UBound(lineitems)
                                    If InStr(file.Name, lineitems(x)) = 0 Then
                                        Exit For
                                    Else
                                        y += 1
                                    End If
                                Next x
                                If y = UBound(lineitems) Then
                                    numMatches += 1
                                    saveFileName = selectedDirectory & "\" & file.Name
                                End If

                            End If

                        Else 'If HighContent, we know that we have to look only for cal files of the proper processor type...

                            'Because there will be many cal files in the folder, and some of the differentiators will be the same between
                            'the various cal files, we must find only one that matches all defined line items.  If we dont find any, or if
                            'we find more than one, the user must manually select.  

                            'We have To make multiple passes through the files, each time adding one more differentiator, because in some
                            'cases, there may be less differentiators used for one processor than the other. If this was not the case, we
                            'could simply look for files that contained all differentiators...
                            Select Case numPasses
                                Case 1
                                    If InStr(file.Name, processorName) > 0 And InStr(file.Name, lineitems(1)) > 0 Then
                                        numMatches += 1
                                        saveFileName = selectedDirectory & "\" & file.Name
                                    End If
                                Case 2
                                    If InStr(file.Name, processorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 Then 'And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 Then
                                        numMatches += 1
                                        saveFileName = selectedDirectory & "\" & file.Name
                                    End If
                                Case 3
                                    If InStr(file.Name, processorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 Then 'And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 Then
                                        numMatches += 1
                                        saveFileName = selectedDirectory & "\" & file.Name
                                    End If
                                Case 4
                                    If InStr(file.Name, processorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 Then 'And InStr(file.Name, lineitems(5)) > 0 Then
                                        numMatches += 1
                                        saveFileName = selectedDirectory & "\" & file.Name
                                    End If
                                Case 5
                                    If InStr(file.Name, processorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 Then
                                        numMatches += 1
                                        saveFileName = selectedDirectory & "\" & file.Name
                                    End If
                                Case 6
                                    If InStr(file.Name, processorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 And InStr(file.Name, lineitems(6)) > 0 Then
                                        numMatches += 1
                                        saveFileName = selectedDirectory & "\" & file.Name
                                    End If
                                Case 7
                                    If InStr(file.Name, processorName) > 0 And InStr(file.Name, lineitems(1)) > 0 And InStr(file.Name, lineitems(2)) > 0 And InStr(file.Name, lineitems(3)) > 0 And InStr(file.Name, lineitems(4)) > 0 And InStr(file.Name, lineitems(5)) > 0 And InStr(file.Name, lineitems(6)) > 0 And InStr(file.Name, lineitems(7)) > 0 Then
                                        numMatches += 1
                                        saveFileName = selectedDirectory & "\" & file.Name
                                    End If
                            End Select

                        End If
                    Next file

                    'If we dont find any matches, Or if we find more than one match, the user must manually select.  

                    If numMatches > 1 Then
                        saveFileName = ""
                        numMatches = 0
                    ElseIf numMatches = 0 Then
                        saveFileName = ""
                        Exit For
                    ElseIf numMatches = 1 Then
                        Exit For
                    End If

                Next numPasses

                If Len(saveFileName) = 0 Then 'no matches found, or too many matches found...

                    'HandleUserMessageLogging("GMRC", "Auto " & CALFileExtension & " Select Failed (number of matches)... Please select " & ProcessorName & " " & CALFileExtension & " file. " & VehicleNumber, DisplayMsgBox, )
                    HandleUserMessageLogging("GMRC", "Auto " & calFileExtension & " Select could not match a " & calFileExtension & " file for " & processorName & " processor to vehicle " & VehicleNumber & ". Please select " & processorName & " " & calFileExtension & " file.", DisplayMsgBox, )

                    saveFileName = SelectFile(mydialog, selectedDirectory, calFileExtension, True)
                End If

            Else 'vehicle number not found in lookup file...

                'HandleUserMessageLogging("GMRC", "Auto " & CALFileExtension & " Select Failed (Vehicle Number not found)... Please select " & ProcessorName & " " & CALFileExtension & " file. " & VehicleNumber, DisplayMsgBox, )
                HandleUserMessageLogging("GMRC", "Auto " & calFileExtension & " Select did not find Vehicle Number " & VehicleNumber & " in lookup table. Please select " & processorName & " " & calFileExtension & " file.", DisplayMsgBox, )

                saveFileName = SelectFile(mydialog, selectedDirectory, calFileExtension, True)
            End If

        Else 'Lookup file not found.  This should never happen, we actually check for existance of file before this function is called...

            HandleUserMessageLogging("GMRC", "Auto " & calFileExtension & " Select Failed... Please select " & processorName & " " & calFileExtension & " file.", DisplayMsgBox)
            saveFileName = SelectFile(mydialog, selectedDirectory, calFileExtension, True)
        End If

        If Len(saveFileName) > 0 Then

            destFile = My.Application.Info.DirectoryPath & "\" & Mid(saveFileName, InStrRev(saveFileName, "\") + 1, Len(saveFileName))

            If Not System.IO.File.Exists(destFile) Then

                HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(saveFileName) & ", Please wait...",,, FlashMsgOn)

                RoboCopyFile(saveFileName, My.Application.Info.DirectoryPath)
                If Not File.Exists(destFile) Then
                    UserStatusInfo.Hide()
                    FindPtpFileInDirectory = ""
                    Exit Function
                End If

            End If

            FindPtpFileInDirectory = destFile

        End If

        UserStatusInfo.Hide()

    End Function

    Public Function Get_Workspace(ByVal workspaceName As String, Optional ByVal path As String = "") As HardwareConfiguration

        'Return workspaceName hardwareconfiguration object based on workspacename and INCA database path provided...

        Dim myDatabaseItems() As DataBaseItem

        Try

            Get_Workspace = Nothing

            If myActualDatabase Is Nothing Then
                myActualDatabase = MyIncaInterface.MyGmIncaComm.IncaInstance.GetCurrentDataBase
            End If

            If Len(path) > 0 Then

                Get_Workspace = myActualDatabase.GetItemInFolder(workspaceName, path) '(CLEVIR Setup\Workspaces)

            Else
                myDatabaseItems = myActualDatabase.BrowseItem(workspaceName)

                If myDatabaseItems IsNot Nothing Then

                    If myDatabaseItems.Length <> 0 Then
                        Get_Workspace = myDatabaseItems(0)
                    End If

                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "Get_Workspace: " & ex.Message, DisplayMsgBox, )
            Get_Workspace = Nothing
        End Try

    End Function

    Public Sub ReadArxmlMappingFileNew(ByVal referenceName As String)

        'reads the ARXML mapping file (HC or LC) to determine which vspy configuration to use based on name of a2l file...

        Dim filename As String = ""
        Dim fnum As Integer
        Dim textline As String
        Dim lineitems() As String

        Dim startNum As Integer
        Dim textLen As Integer = 7

        Dim saveWorkspaceTemplateName As String = ""
        Dim saveVSpySelectedConfigFileName As String = ""

        Dim found As Boolean

        Select Case ProjectName

            Case "LowContent"
                filename = My.Application.Info.DirectoryPath & "\LC_ARXML_Mapping.csv"
                startNum = 10
            Case "HighContent"
                filename = My.Application.Info.DirectoryPath & "\HC_ARXML_Mapping.csv"
                startNum = 11

            'FCM CHANGE - Added FCM Case here

            Case "FCM", "FCM100"

                filename = My.Application.Info.DirectoryPath & "\FCM_ARXML_Mapping.csv"
                'FCM CHANGE - need to differentiate here based on actual FCM .a2l file name, there will be many different flavors
                'rather than just a single one as with LC and HC - So we cant just look at version using startnum, we will set this
                'below in the do while loop so we can determine where the startnum should be based on a2l file format...

                'StartNum = 11

        End Select

        If File.Exists(filename) = True Then

            fnum = FreeFile()

            FileOpen(fnum, filename, OpenMode.Input)

            Do While Not EOF(fnum)
                textline = LineInput(fnum)
                lineitems = Split(textline, ",")
                'FCM CHANGE  - Added logic below to determine startnum for parsing software version from FCM a2l filename...
                If InStr(ProjectName, "FCM") > 0 Then
                    If IsNumeric(Mid(lineitems(0), 13, 1)) = True Then
                        startNum = 13
                    Else
                        startNum = 16
                    End If
                End If

                If InStr(referenceName, Mid(lineitems(0), startNum, textLen)) > 0 Then

                    VSpySelectedConfigFileName = lineitems(4)

                    found = True
                    Exit Do

                Else 'if the a2l filename does not match, we will save the information here.  That way, if the a2l filename is not found,
                    'we will use the information from the last line in the file (the most recent information).

                    saveVSpySelectedConfigFileName = lineitems(4)

                End If
            Loop

            If found = False Then
                VSpySelectedConfigFileName = saveVSpySelectedConfigFileName
            End If

            FileClose(fnum)

        End If



    End Sub

    Public Function ReadVehiclePtpLookupFile() As List(Of String)

        Dim fnum As Integer
        Dim textline As String
        Dim lineitems() As String = Nothing
        Dim returnlist As List(Of String) = Nothing
        Dim filename = My.Application.Info.DirectoryPath & "\VehiclePTPLookup.csv"

        If File.Exists(filename) Then

            returnlist = New List(Of String)

            fnum = FreeFile()

            FileOpen(fnum, filename, OpenMode.Input)

            textline = LineInput(fnum)

            Do While Not EOF(fnum)

                textline = LineInput(fnum)

                returnlist.Add(textline)

            Loop

            FileClose(fnum)

        End If

        ReadVehiclePtpLookupFile = returnlist

    End Function

    Public Function ReadInVehiclePtpLookupFileOld(ByVal filename As String) As String()

        'Called from FindPTPFileInDirectory...

        'Reads the VehiclePTPLookup.csv file and passes back a string array which is used to determine the search strings to use to identify the proper ptp file
        'to use for a particular vehicle when building a new vehicle specific workspace...

        Dim fnum As Integer
        Dim textline As String
        Dim lineitems() As String = Nothing
        Dim returnarray() As String = Nothing

        If File.Exists(filename) Then

            fnum = FreeFile()

            FileOpen(fnum, filename, OpenMode.Input)

            textline = LineInput(fnum)

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

        ReadInVehiclePtpLookupFileOld = returnarray

    End Function

    Private Function ProcessVehiclePtpLookupFileInfo() As String()

        'Called from FindPTPFileInDirectory...

        'Reads the VehiclePTPLookup.csv file and passes back a string array which is used to determine the search strings to use to identify the proper ptp file
        'to use for a particular vehicle when building a new vehicle specific workspace...

        Dim lineitems() As String = Nothing
        Dim returnarray() As String = Nothing
        Dim x As Integer

        For x = 0 To VehiclePTPLookupInfo.Count - 1
            lineitems = Split(VehiclePTPLookupInfo(x).ToString, ",")
            If UCase(lineitems(0)) = UCase(VehicleNumber) Then
                returnarray = lineitems
                Exit For
            End If
        Next x

        If returnarray Is Nothing Then
            ReDim returnarray(0)
            returnarray(0) = "Invalid"
        End If

        ProcessVehiclePtpLookupFileInfo = returnarray

    End Function

    Private Function SelectA2LAndCalFiles(ByVal myFileDialog As FileDialog, ByVal mydialog As FolderBrowserDialog, ByVal mylistbox As ListBox, ByVal processorName As String, ByVal fileSelectionMethod As String) As String()

        'Called from A2lAndCALToVehicleSpecificWorkspace
        'Allows user to select a2l and CAL file (.ptp, .s19 or .s37)...

        Dim calFileExtension As String = ""
        Dim projectFileNames(0 To 1) As String
        Dim setIncaWorkspaceTemplateName As Boolean

        If fileSelectionMethod = "Manual" Then
            projectFileNames(0) = SelectFileByType(myFileDialog, "a2l", mylistbox)
        Else
            'Here we retrieve the filename of the a2l file based on ProcessorType passed in...
            projectFileNames(0) = FindA2LFileInDirectory(mydialog, mylistbox, processorName, myFileDialog)

        End If

        If Len(projectFileNames(0)) > 0 Then

            'Many files used are model year and software version specific, so we need to know which model year and software version
            'we are using.  This is based on the name of the a2l file...

            A2LFileName = System.IO.Path.GetFileName(projectFileNames(0))

            'INCAWorkspaceTemplateName is built from the model year and software version obtained from the .a2l filename.
            'Which .a2l file to use to obtain the model year and software version depends on the ProjectName 

            Select Case ProjectName
                Case "HighContent"
                    If InStr(projectFileNames(0), "HCS") > 0 Then
                        setIncaWorkspaceTemplateName = True
                    End If
                Case "LowContent"
                    If InStr(projectFileNames(0), "LC") > 0 Then
                        setIncaWorkspaceTemplateName = True
                    End If
                Case "FCM", "FCM100" 'Added for 5.6.2
                    If InStr(projectFileNames(0), "FCM") > 0 Then
                        setIncaWorkspaceTemplateName = True
                    End If
                Case "ACP2"
                    If InStr(projectFileNames(0), "ACP2") > 0 Then
                        setIncaWorkspaceTemplateName = True
                    End If
                Case "ACP3"
                    If InStr(projectFileNames(0), "ACP3") > 0 Then
                        setIncaWorkspaceTemplateName = True
                    End If
                Case "ACP4"
                    If InStr(projectFileNames(0), "ACP4") > 0 Then
                        setIncaWorkspaceTemplateName = True
                    End If
            End Select

            If setIncaWorkspaceTemplateName = True Then

                GModelYear = DetermineModelYear(projectFileNames(0))
                GSoftwareVersion = DetermineSoftwareVersion(projectFileNames(0))

                GSpecificArxml = DetermineIfUsingDifferentArxml(projectFileNames(0))

                INCAWorkspaceTemplateName = GSaveIncaWorkspaceTemplateName
                WorkspaceNameSuffix = GSaveWorkspaceNameSuffix

                If InStr(INCAWorkspaceTemplateName, "_MY") = 0 Then

                    INCAWorkspaceTemplateName = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & INCAWorkspaceTemplateName
                    WorkspaceNameSuffix = GSoftwareVersion & "_MY" & GModelYear & GSpecificArxml & "_" & WorkspaceNameSuffix

                End If

            End If

            Select Case processorName
                Case "HCS", "HCF", "LC", "ACP2", "ACP3", "ACP4"
                    calFileExtension = "ptp"
                    'FCM CHANGE ??? - Added FCM case here.  Need to be able to set the CALFileExtension based on ProcessorName rather than just using .ptp
                    'due to the fact that FCM supliers use .s19 or .s37 files? not sure if this is the case yet about .s37 files..., not ptp files...

                    'This may need to change, VEO may be using s19 files also and not s37...
                Case "FCM"
                    'If InStr(A2LFileName, "ZF1") > 0 Then
                    calFileExtension = "s19"
                    'Else
                    'CALFileExtension = "s37"
                    'End If
                Case ""
                    calFileExtension = "ptp"
            End Select

            If fileSelectionMethod = "Manual" Then
                projectFileNames(1) = SelectFileByType(myFileDialog, calFileExtension, mylistbox)
            Else
                'Here we look for the ptp file that corresponds to the vehiclenumber and processorname...
                projectFileNames(1) = FindPtpFileInDirectory(myFileDialog, mydialog.SelectedPath, mylistbox, processorName, calFileExtension)
            End If

        End If

        SelectA2LAndCalFiles = projectFileNames

    End Function

    Public Function SelectFile(ByVal mydialog As FileDialog, ByVal initialDir As String, ByVal myFilter As String, Optional ByVal noZips As Boolean = False) As String

        'Displays a file dialog box to allow user to select a file - Initial directory, filter and an optional NoZips are passed in...
        'If NoZips is true, then only the filter is an allowable extension to select, if NoZips is false, then allowable selections
        'are the filter passed in and .zip...

        SelectFile = ""

        mydialog.InitialDirectory = initialDir
        mydialog.FileName = ""

        If InStr(myFilter, "exp") = 0 Then
            If noZips = False Then
                mydialog.Filter = "zip files (*.zip) | *.zip|" & myFilter & " files (*." & myFilter & ") | *." & myFilter
            Else
                mydialog.Filter = myFilter & " | *." & myFilter
            End If
        Else
            If noZips = False Then
                mydialog.Filter = "zip files (*.zip) | *.zip|" & myFilter & " files (*." & myFilter & ") | *." & myFilter & "|" & myFilter & "64 files (*." & myFilter & "64" & ") | *." & myFilter & "64"
            Else
                mydialog.Filter = myFilter & " | *." & myFilter & "|" & myFilter & "64 | *." & myFilter & "64"
            End If
        End If


        mydialog.DefaultExt = myFilter

        mydialog.ShowDialog()

        If Len(mydialog.FileName) > 0 Then

            If InStr(System.IO.Path.GetFileName(mydialog.FileName), " ") = 0 Then
                SelectFile = mydialog.FileName
            Else
                HandleUserMessageLogging("GMRC", "SelectFile: Invalid filename selected. There cannot be a space character in the filename.", DisplayMsgBox, )
            End If

        Else
            'MsgBox("You must select a valid file, Exiting...")
            Exit Function
        End If

    End Function

    Public Function SelectFileByType(ByVal mydialog As FileDialog, ByVal myFilter As String, ByVal mylistbox As ListBox, Optional ByRef incaWorkspace As String = "") As String

        'Displays file dialog box using myFilter passed in.  INCAWorkspace name is optional, if provided, workspace name is
        'the filename selected minus the extension.

        'Unzips if necessary based on file selection
        'Uses projectname based on vehicle number to determine initialdirectory
        'If flash drive is plugged In, the initialdirectory is based on the directory in which the user selected files were
        'placed by the file transfer utility.

        'Dim szip As SevenZipExtractor = Nothing
        Dim szip As ICSharpCode.SharpZipLib.Zip.ZipFile = Nothing
        Dim szipEntry As ICSharpCode.SharpZipLib.Zip.ZipEntry = Nothing
        'Dim exreader As FileStream = Nothing
        Dim tempstr As String = Nothing

        Dim destFile As String
        Dim importFileName As String

        SelectFileByType = ""

        If Len(InitialDirectory) = 0 Then 'Or InitialDirectory = My.Application.Info.DirectoryPath Then

            DetermineInitIncaProjectDir(InitialDirectory)

        End If

        'SelectFile displays a file dialog using parameters passed in, if the optional parameter INCAWorkspace has been 
        'provided to the SelectFileByType function, then the fourth parameter for SelectFile is true, indicating that
        'SelectFile should allow selection of .zip files as well as the provided filter...

        If myFilter <> "exp" Then
            SourceFile = SelectFile(mydialog, InitialDirectory, myFilter, True)
        Else
            SourceFile = SelectFile(mydialog, InitialDirectory, myFilter, False)
        End If

        If Len(SourceFile) > 0 Then

            InitialDirectory = Path.GetDirectoryName(SourceFile)

            If Len(SourceFile) > 0 And (InStr(SourceFile, ".zip") > 0 Or InStr(SourceFile, "." & myFilter) > 0) Then

                destFile = My.Application.Info.DirectoryPath & "\" & Mid(SourceFile, InStrRev(SourceFile, "\") + 1, Len(SourceFile))

            Else
                HandleUserMessageLogging("GMRC", "SelectFileByType: Invalid file selected, please select a valid .zip file or " & myFilter & " file.", DisplayMsgBox)
                SelectFileByType = ""
                Exit Function

            End If

            If Not System.IO.File.Exists(destFile) Then

                HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(SourceFile) & ", Please wait...",,, FlashMsgOn)

                RoboCopyFile(SourceFile, My.Application.Info.DirectoryPath)
                If Not File.Exists(destFile) Then
                    SelectFileByType = ""
                    UserStatusInfo.Hide()
                    Exit Function
                End If

                UserStatusInfo.Hide()

            Else

                If SourceFile <> destFile Then
                    If MsgBox("File already exists.  Do you wish to replace this file?", vbYesNo) = vbYes Then
                        HandleUserMessageLogging("GMRC", "File " & destFile & " found.  Copying over existing file.",,, FlashMsgOn, mylistbox)

                        If Not Debugger.IsAttached Then

                            HandleUserMessageLogging("GMRC", "Copying " & Path.GetFileName(SourceFile) & ", Please wait...",,, FlashMsgOn)

                            RoboCopyFile(SourceFile, My.Application.Info.DirectoryPath)
                            If Not File.Exists(destFile) Then
                                SelectFileByType = ""
                                UserStatusInfo.Hide()
                                Exit Function
                            End If

                        Else

                            HandleUserMessageLogging("GMRC", "Bypassing copy of " & Path.GetFileName(SourceFile) & ", for testing...",,, FlashMsgOn)

                        End If

                        UserStatusInfo.Hide()

                    Else
                        HandleUserMessageLogging("GMRC", "File " & destFile & " found.  Using existing file.",,, FlashMsgOn, mylistbox)
                    End If
                End If

            End If

            If InStr(destFile, ".zip") > 0 Then

                szip = New ICSharpCode.SharpZipLib.Zip.ZipFile(destFile)

                If szip IsNot Nothing Then

                    For Each szipEntry In szip
                        tempstr = szipEntry.Name
                    Next

                    importFileName = My.Application.Info.DirectoryPath & "\" & tempstr
                    incaWorkspace = Mid(tempstr, 1, Len(tempstr) - 4)

                    szip = Nothing

                Else
                    HandleUserMessageLogging("GMRC", "Zip File Processing Error. Exiting...", DisplayMsgBox)
                    SelectFileByType = ""
                    Exit Function
                End If

                If Not System.IO.File.Exists(importFileName) Then

                    HandleUserMessageLogging("GMRC", "Unzipping File, Please wait...",,, FlashMsgOn, mylistbox)

                    UnzipFile(destFile)

                Else
                    'UserStatusInfo.Label1.Text = "File " & ImportFileName & " found."
                    'Me.ListBox1.Items.Add("File " & ImportFileName & " found.")
                End If

            Else
                importFileName = destFile
                incaWorkspace = Mid(destFile, InStrRev(destFile, "\") + 1, Len(destFile) - InStrRev(destFile, "\") - 4)
            End If

            SelectFileByType = importFileName

        Else
            'MsgBox("No File Selected, Exiting...")
        End If

        UserStatusInfo.Hide()

    End Function

    Public Function SetupCameraNamesInWorkspace(ByVal workspaceName As String) As Boolean

        'This function is used as part of the Import software and cals process.  Sets up camera names in newly created workspace based on
        'the contents of the vehicleconfigurations.csv file.  This is new functionality in 5.4.11...

        'All template workspaces contain references to 6 cameras with generic names.  Names in workspace are changed to camera names associated with
        'user specified vehicle number in vehicleconfigurations.csv file.  If there are less cameras defined in file, extra ONVIF video
        'devices are removed from the newly created workspace...

        Dim myHwDevices() As HWDevice

        Dim numCameraNamesSet As Integer

        Dim myHwSystems() As HWSystem

        SetupCameraNamesInWorkspace = False

        Try

            MyHWC = Get_Workspace(workspaceName, "CLEVIR Setup\Workspaces")

            If MyHWC Is Nothing Then
                HandleUserMessageLogging("GMRC", "SetupCameraNamesInWorkspace: Get_Workspace could not find " & workspaceName,, )
                Exit Function
            End If

            myHwSystems = MyHWC.GetAllSystems

            If myHwSystems Is Nothing Then
                HandleUserMessageLogging("GMRC", "SetupCameraNamesInWorkspace: MyHWC.GetAllSystems returned nothing for " & workspaceName,, )
                Exit Function
            End If

            For x = 0 To UBound(myHwSystems)

                If InStr(myHwSystems(x).GetName, "ONVIF") > 0 Then

                    If NumberOfCamerasInVehicle = 0 Then
                        MyHWC.RemoveSystem(myHwSystems(x))
                    Else

                        myHwDevices = myHwSystems(x).GetAllDevices()

                        numCameraNamesSet += 1

                        If numCameraNamesSet <= NumberOfCamerasInVehicle Then

                            'String optionModule = "HWC";
                            Dim optionModule = "HWC"

                            'String optionHwcPathName = hwConfig.GetParentFolder().GetNameWithPath() + @"\" + hwConfig.GetName();
                            Dim optionHwcPathName As String = MyHWC.GetParentFolder().GetNameWithPath() & "\" & MyHWC.GetName()

                            'String optionHwItemName = HWDevice.GetName();
                            Dim optionHwItemName As String = myHwDevices(0).GetName()

                            'String optionName = "HWItemName";
                            Dim optionName = "HWItemName"

                            'String setOptionParameter =

                            'String.Format(
                            '"MODULE:{0};HWCPATHNAME:{1};HWITEMNAME:{2};OPTIONNAME:{3};OPTIONVALUE:{4}",
                            'optionModule, optionHwcPathName, optionHwItemName, optionName, deviceName);

                            Dim setOptionParameter = String.Format("MODULE:{0};HWCPATHNAME:{1};HWITEMNAME:{2};OPTIONNAME:{3};OPTIONVALUE:{4}", optionModule, optionHwcPathName, optionHwItemName, optionName, CameraNames(numCameraNamesSet - 1))

                            MyIncaInterface.MyGmIncaComm.IncaInstance.SetOption(setOptionParameter)

                        Else
                            MyHWC.RemoveSystem(myHwSystems(x))
                        End If

                    End If

                End If

            Next

            SetupCameraNamesInWorkspace = True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "SetupCameraNamesInWorkspace: " & ex.Message,, )
        End Try

    End Function

    Public Sub SetupClevirDatabase(ByVal myActualDatabase As Object)

        'Called from HandleDatabase.  If necessary, adds the CLEVIR Setup top level folder and the Experiments and Workspaces sub-folders...

        Dim myfolder As IncaFolder
        Dim mysubfolder As IncaFolder

        myfolder = myActualDatabase.GetFolder("CLEVIR Setup")

        If myfolder IsNot Nothing Then
            mysubfolder = If(myfolder.GetSubFolder("Experiments"), myfolder.AddSubFolder("Experiments"))
            mysubfolder = If(myfolder.GetSubFolder("Workspaces"), myfolder.AddSubFolder("Workspaces"))
        Else
            myfolder = myActualDatabase.AddFolder("CLEVIR Setup")

            mysubfolder = myfolder.AddSubFolder("Experiments")
            mysubfolder = myfolder.AddSubFolder("Workspaces")

            'myinca.SetOption("MODULE: USEROPTIONS; OPTIONNAME: [Measure-General]MdfFileType; OPTIONVALUE: mdf 4.0")

        End If

        'MODULE: USEROPTIONS; OPTIONNAME: [General]Maximizedwindows

        If MyIncaInterface.MyGmIncaComm.IncaInstance.GetOption("MODULE: USEROPTIONS; OPTIONNAME: [Measure-General]MdfFileType") <> "mdf 4.0" Then
            MyIncaInterface.MyGmIncaComm.IncaInstance.SetOption("MODULE: USEROPTIONS; OPTIONNAME: [Measure-General]MdfFileType; OPTIONVALUE: mdf 4.0")
        End If

        If MyIncaInterface.MyGmIncaComm.IncaInstance.GetOption("MODULE: USEROPTIONS; OPTIONNAME: [General]AllowECUReset") <> "true" Then
            MyIncaInterface.MyGmIncaComm.IncaInstance.SetOption("MODULE: USEROPTIONS; OPTIONNAME: [General]AllowECUReset; OPTIONVALUE: true")
        End If


    End Sub

    Private Sub ReadAutosarFile()

        'Reads in a new ARXML file if requested...

        '5.	Unzip ARXML File (automate)
        '6.	Add ARXML file into INCA Or Add DBC files to INCA (automate)

        Dim myincafolder As IncaFolder
        Dim filename As String = ""
        Dim compressedFilename As String = ""
        Dim x As Integer = -1

        Dim dir As DirectoryInfo = New DirectoryInfo(My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML")
        Dim files As FileInfo() = dir.GetFiles()

        Dim returnStr As String = ""

        If MsgBox("Process updated ARXML File(s)?", vbYesNo) = vbNo Then
            ProcessNewArxmlFile = False

        Else
            ProcessNewArxmlFile = True
            'MsgBox("Please delete all files from the " & My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML folder and place the new ARXML file(s) into this location...")

            HandleArxmlFileDirectory()

            returnStr = MyIncaInterface.ConnectToInca()

            If returnStr = "True" Then

                myincafolder = myActualDatabase.GetFolder("CLEVIR Setup")
                For x = 0 To UBound(SaveArxmlFilename)
                    myincafolder.ReadAutosarFile(SaveArxmlFilename(x))
                Next x

            Else
                MsgBox("ReadAutosarFile: ConnectToInca returned - " & returnStr)
            End If

        End If

    End Sub

    Public Sub HandleArxmlFileDirectory()

        'Used only when creating files for a new software version / model year by the CLEVIR Administrator.
        'Looks in the ARXML folder (CLEVIR Admin copies a file or files into this folder prior to initiating the
        'process of creating new modelyear / software version specific CLEVIR support files).  Unzips files
        'if required, then places filenames into an array which is used later in this process...

        Dim x As Integer = -1
        Dim compressedFilename As String = ""
        Dim dir As DirectoryInfo = New DirectoryInfo(My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\ARXML")
        Dim files As FileInfo() = dir.GetFiles()

        Dim arxmlFileProjectId As String
        Dim processFile As Boolean

        Select Case ProjectName
            Case "HighContent"
                arxmlFileProjectId = "EOCM_H"
            Case "FCM"
                arxmlFileProjectId = "FCM_"
            Case "FCM100"
                arxmlFileProjectId = "FCM100_"
            Case "LowContent"
                arxmlFileProjectId = "EOCM_"
            'Case "CSAV2"
            Case "ACP2"
                arxmlFileProjectId = "ACP2_MCU"
            Case "ACP3"
                arxmlFileProjectId = "ACP3_MCU"
            Case "ACP4"
                arxmlFileProjectId = "ACP4_MCU"
            Case Else
                arxmlFileProjectId = ""
                SaveArxmlFilename = Nothing
                Exit Sub
        End Select

        For Each file In files

            If InStr(file.Name, ".zip") > 0 Then

                If InStr(ProjectName, "LowContent") = 0 Then
                    If InStr(file.Name, arxmlFileProjectId) > 0 Then
                        processFile = True
                    End If
                Else
                    If InStr(file.Name, arxmlFileProjectId) > 0 And InStr(file.Name, "EOCM_H") = 0 Then
                        processFile = True
                    End If
                End If

                If processFile = True Then
                    compressedFilename = file.DirectoryName & "\" & file.Name
                    UnzipFile(compressedFilename)
                    processFile = False
                End If

            End If

        Next

        x = -1

        files = dir.GetFiles()
        For Each file In files

            If InStr(ProjectName, "LowContent") = 0 Then
                If InStr(file.Name, arxmlFileProjectId) > 0 And InStr(UCase(file.Name), ".ARXML") > 0 Then
                    processFile = True
                End If
            Else
                If InStr(file.Name, arxmlFileProjectId) > 0 And InStr(file.Name, "EOCM_H") = 0 And InStr(UCase(file.Name), ".ARXML") > 0 Then
                    processFile = True
                End If
            End If

            'If InStr(UCase(file.Name), ".ARXML") > 0 Then
            If processFile = True Then
                x += 1
                ReDim Preserve SaveArxmlFilename(x)
                SaveArxmlFilename(x) = file.DirectoryName & "\" & file.Name
                processFile = False
            End If
        Next

    End Sub

    Public Sub ReadCandbFiles()

        'Reads in new CAN DB files if requested...

        Dim myincafolder As IncaFolder
        Dim dbcFileNames() As String = Nothing
        Dim x As Integer

        Dim dir As DirectoryInfo = New DirectoryInfo(My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\DBC")
        Dim files As FileInfo() = dir.GetFiles()

        Dim returnStr As String = ""


        If MsgBox("Process updated DBC Files?", vbYesNo) = vbNo Then
            ProcessNewDbcFiles = False
        Else
            ProcessNewDbcFiles = True
            MsgBox("Please delete all files from the " & My.Application.Info.DirectoryPath & "\UpdatesForNewSoftwareVersion\DBC folder and place the new DBC files into this location...")

            x = -1
            For Each file In files
                If InStr(file.Name, ".DBC") > 0 Then
                    x += 1
                    ReDim Preserve dbcFileNames(x)
                    dbcFileNames(x) = file.DirectoryName & "\" & file.Name
                End If
            Next

            If dbcFileNames IsNot Nothing Then

                returnStr = MyIncaInterface.ConnectToInca()

                If returnStr = "True" Then

                    myincafolder = myActualDatabase.GetFolder("CSAV2\DV3\Serial Data")
                    For x = 0 To UBound(dbcFileNames)
                        myincafolder.ReadCanDBFile(dbcFileNames(x))
                    Next x

                Else
                    MsgBox("ReadCANDBFiles: ConnectToInca returned - " & returnStr)
                End If

            End If

        End If

    End Sub

    Public Function FlashController(ByVal deviceName As String, ByVal flashType As String, deviceObj As ExperimentDevice, mylistbox As ListBox) As Boolean

        'Flashes controller or controllers.

        Dim flowControl As String = ""

        Dim myWorkbaseDevice As WorkbaseDevice

        Dim fnum As Integer
        Dim filename As String = My.Application.Info.DirectoryPath & "\ProfsInfo.csv"

        Dim lineitems() As String
        Dim foundDevice As Boolean
        Dim profsDirectory As String = ""
        Dim profsString1 As String = ""
        Dim profsString2 As String = ""
        Dim supplierName As String = ""

        Try

            FlashController = False

            MyHWC = Get_Workspace(INCAWorkspace)

            If MyHWC IsNot Nothing Then

                If Not (deviceObj Is Nothing) Then

                    If deviceObj.IsWorkbaseDevice = True Then

                        HandleUserMessageLogging("GMRC", "Flashing " & deviceObj.GetName & ".  Please be patient...",,,, mylistbox)

                        myWorkbaseDevice = CastDbItemToWorkbaseDevice(deviceObj)

                        'Flash the contents of the hexfile to a target by using a specified control string.
                        'The Hex file must be created using the method CreateHexFileForReferencePageAndCode() Or CreateHexFileForWorkPageAndCode().
                        'The controlFlow must be a string which contains the rules for flashing.
                        'The Control string was typically built by the PROF toolbox in a user dialog.
                        'The controlFlow depends on the used target. A sample could be get by reading the target.log file after flashing with the tool.

                        'Getting the control flow
                        '1. Flash from the experiment.
                        '2. To select a HEX file, choose “Hardware” - “Manage memory pages”. On the “Enhanced” tab, select the “Flash programming” action And apply it to “Data: ECU flash”.
                        '3. Important: Select Case the desired flash parameters In Prof And flash the ECU.
                        '4. Via the text editor, open the file under C:\Users\Public\Documents\ETAS\LOGFILES\ProcessLogsV2\TGTSVR.log - or C:\eng_apps\ETAS\LogFiles or C:\ProgramData\ETAS
                        '5. Look for the entry StorePermanentlyToTarget.
                        '6. For example, the following entry Is displayed: “StorePermanentlyToTarget(KWP2000:1, C:\APPLIK~1\PROJEKTE\MS6_\P0516500.HEX seed2key 3 prog convert C:\ETASDATA\PROF2.1\MS6\ms6)”
                        '7. Enter the parameters into the Tool-API command as follows: Invoke(Device_ID, 'FlashHexFileWithControlFlow', 'C:\APPLIK~1\PROJEKTE\MS6_\P0516500.HEX', 'seed2key 3 prog convert C:\ETASDATA\PROF2.1\MS6\ms6')
                        '- In the path of the HEX file, the DOS convention Is Not necessary.
                        '- In the path to the Prof control flow, the DOS convention Is absolutely necessary, e.g. the path must Not contain any spaces.

                        myWorkbaseDevice.CreateHexFileForReferencePageAndCode("C:\temp\etas\test.hex")

                        '"C:\USERS\PUBLIC\DOCUMENTS\ETAS\PROF\FCM_ZF_V1.1_XETKS20_TC36X_BDR_Prof\XETKS20_BDR"

                        'FCM_ZF_V1.1_XETKS20_TC36X_BDR_Prof(V1.1)

                        If File.Exists(filename) Then ' check for ProfsInfo.csv file in CLEVIR install directory...

                            fnum = FreeFile()
                            FileOpen(fnum, filename, OpenMode.Input)
                            LineInput(fnum)
                            Do While Not EOF(fnum)
                                lineitems = Split(LineInput(fnum), ",")
                                If UBound(lineitems) = 4 Then

                                    If InStr(deviceName, "FCM") = 0 Then
                                        If lineitems(0) = deviceName Then
                                            foundDevice = True
                                            profsDirectory = lineitems(1)
                                            profsString1 = lineitems(2)
                                            profsString2 = lineitems(3)
                                            supplierName = lineitems(4)
                                            Exit Do
                                        End If
                                    Else
                                        If lineitems(0) = deviceName And InStr(CLEVIRFilesPath, lineitems(4)) > 0 Then
                                            foundDevice = True
                                            profsDirectory = lineitems(1)
                                            profsString1 = lineitems(2)
                                            profsString2 = lineitems(3)
                                            supplierName = lineitems(4)
                                            Exit Do
                                        End If

                                    End If

                                Else
                                    HandleUserMessageLogging("GMRC", "Profs lookup file " & filename & " is contains INVALID information, Exiting...", DisplayMsgBox, )
                                    FileClose(fnum)
                                    Exit Function
                                End If
                            Loop

                            FileClose(fnum)

                            If foundDevice = False Then
                                HandleUserMessageLogging("GMRC", "Device " & deviceName & " not found in Profs lookup file, Exiting...", DisplayMsgBox, )
                                Exit Function
                            End If
                        Else
                            HandleUserMessageLogging("GMRC", "Profs lookup file " & filename & " does not exist, Exiting...", DisplayMsgBox, )
                            Exit Function
                        End If

                        Select Case deviceName
                            Case "ACP2_MCU"

                                If System.IO.Directory.Exists(profsDirectory) Then
                                    flowControl = profsString1 & " " & flashType & " " & profsString2
                                Else
                                    HandleUserMessageLogging("GMRC", "PROFS directory " & profsDirectory & " not found for " & deviceObj.GetName, DisplayMsgBox, )
                                    Exit Function
                                End If

                            Case "ACP3_MCU"

                                If System.IO.Directory.Exists(profsDirectory) Then
                                    flowControl = profsString1 & " " & flashType & " " & profsString2
                                Else
                                    HandleUserMessageLogging("GMRC", "PROFS directory " & profsDirectory & " not found for " & deviceObj.GetName, DisplayMsgBox, )
                                    Exit Function
                                End If

                            Case "ACP4_MCU"

                                If System.IO.Directory.Exists(profsDirectory) Then
                                    flowControl = profsString1 & " " & flashType & " " & profsString2
                                Else
                                    HandleUserMessageLogging("GMRC", "PROFS directory " & profsDirectory & " not found for " & deviceObj.GetName, DisplayMsgBox, )
                                    Exit Function
                                End If

                            Case "FCM", "FCM100"

                                If System.IO.Directory.Exists(profsDirectory) Then
                                    flowControl = profsString1 & " " & flashType & " " & profsString2
                                Else
                                    HandleUserMessageLogging("GMRC", "PROFS directory " & profsDirectory & " not found for " & deviceObj.GetName, DisplayMsgBox, )
                                    Exit Function
                                End If

                            Case "HCF", "HCS"

                                If System.IO.Directory.Exists(profsDirectory) Then
                                    flowControl = profsString1 & " " & flashType & " " & profsString2
                                ElseIf System.IO.Directory.Exists("C:\USERS\PUBLIC\DOCUMENTS\ETAS\PROF\EOCM3_V4.5_XETKS20_BDR_Prof") Then
                                    flowControl = "basic_options " & deviceName & " PN3 3 1 BOOT " & flashType & " C:\USERS\PUBLIC\DOCUMENTS\ETAS\PROF\EOCM3_V4.5_XETKS20_BDR_Prof\XETKS20_BDR"
                                Else
                                    HandleUserMessageLogging("GMRC", "PROFS directory " & profsDirectory & " not found for " & deviceObj.GetName, DisplayMsgBox, )
                                    Exit Function
                                End If

                            Case "XETK:1"

                                If System.IO.Directory.Exists(profsDirectory) Then

                                    flowControl = profsString1 & " " & flashType & " " & profsString2

                                ElseIf System.IO.Directory.Exists("C:\USERS\PUBLIC\DOCUMENTS\ETAS\PROF\EOCM3_V4.5_XETKS20_BDR_Prof") Then
                                    flowControl = "basic_options LC PN1 1 1 BOOT " & flashType & " C:\USERS\PUBLIC\DOCUMENTS\ETAS\PROF\EOCM3_V4.5_XETKS20_BDR_Prof\XETKS20_BDR"
                                Else
                                    HandleUserMessageLogging("GMRC", "PROFS directory " & profsDirectory & " not found for " & deviceObj.GetName, DisplayMsgBox, )
                                    Exit Function
                                End If


                            Case "IP", "IR", "K1P", "K1R", "K2P", "K2R"

                                If System.IO.Directory.Exists(profsDirectory) Then
                                    flowControl = profsString1 & " " & flashType & " " & profsString2
                                Else
                                    HandleUserMessageLogging("GMRC", "PROFS directory " & profsDirectory & " not found for " & deviceObj.GetName, DisplayMsgBox, )
                                    Exit Function
                                End If

                        End Select

                        HandleUserMessageLogging("GMRC", "FlowControl = " & flowControl)

                        If System.IO.File.Exists("c:\temp\etas\RestoreDeviceCpuDesc.xml") = True Then
                            System.IO.File.Delete("c:\temp\etas\RestoreDeviceCpuDesc.xml")
                        End If

                        FlashController = myWorkbaseDevice.FlashHexFileWithControlFlow("c:\temp\ETAS\test.hex", flowControl)

                        If FlashController = True Then

                            If myWorkbaseDevice.CheckDataPagesConform() = False Then
                                HandleUserMessageLogging("GMRC", deviceName & " Controller " & flashType & " Checksum Mismatch!!!", DisplayMsgBox,,, mylistbox)
                                FlashController = False
                            End If
                        End If

                    Else
                        HandleUserMessageLogging("GMRC", deviceObj.GetName & " is not a WorkbaseDevice", DisplayMsgBox, )
                    End If

                End If
            Else

                HandleUserMessageLogging("GMRC", "FlashController: Get_Workspace could not find " & IncaWorkspace,, )
                Exit Function

            End If

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "FlashController: " & ex.Message, DisplayMsgBox, )

        Finally

        End Try

    End Function
End Module
