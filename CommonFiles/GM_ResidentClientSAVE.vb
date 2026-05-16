
Option Strict Off
Option Explicit On

Imports VB = Microsoft.VisualBasic

Imports System
Imports System.Text
Imports System.Diagnostics

Imports System.Speech
Imports System.Speech.Recognition
Imports System.Speech.Synthesis

Imports System.Runtime.InteropServices
Imports System.Collections

Imports System.IO

Imports System.Drawing
Imports System.Drawing.Drawing2D

Imports System.Threading

Imports System.Net.NetworkInformation

Public Class GM_ResidentClient

    'This was originally the main display form for the application and hence, much of the code
    'resides in here.  However, another form was added midway through the development of the app
    'called OnVehicleScreen, which is the primary display when running in the vehicle.  The 
    'GM_ResidentClient form became the configuration environment form which is more like a traditional
    'windows form with a dropdown menu.  The configuration environment can now be accessed from the
    'OnVehicleScreen so that certain aspects of the interface can be configured by the user.  The 
    'idea here is that in most cases, the user will be accessing this form from their desktop to create
    'custom configurations, and will not be accessing this form much when running on a actual vehicle
    'where they will be using the OnVehicleScreen display screen instead.

    'However, the GM_ResidentClient form still contains a significant portion of the code for this application.

    '***************************** Public / Pivate Variable Declarations ***************************
    Structure DeviceRasterPairs
        Dim DeviceName As String
        Dim RasterName As String
        Dim Count As Integer
        Dim OneByteCount As Integer
        Dim TwoByteCount As Integer
        Dim FourByteCount As Integer
        Dim MessageDisplayed As Boolean
    End Structure

    Public DeviceRasterCount() As DeviceRasterPairs

    Enum GridUpdateActions
        TO_HIGH
        FROM_HIGH
        TO_LOW
        FROM_LOW
    End Enum

    Structure InvisibleSignals
        Dim DeviceName As String
        Dim RasterName As String
        Dim SignalName As String
        Dim Status As String
        Dim Value As Double
    End Structure

    Public myInvisibleSignals() As InvisibleSignals

    Public Structure INCA_Raster
        Dim DeviceName As String
        Dim RasterName As String
        Dim Variables() As String
    End Structure

    Public Structure INCA_Device
        Dim DeviceName As String
        Dim Rasters() As INCA_Raster
    End Structure

    Structure ButtonContainer
        Dim Parent As TabControl
        Dim ContainerName As String
        Dim Buttons() As Button
        Dim ButtonContainerHotKey() As Label
    End Structure

    Private Const CONVERT_TO_LAT_LON As Double = 3600000

    Private Const KILOMETERS_TO_MILES As Double = 0.62137119
    Private Const NORTHWEST_CORNER_LAT As Double = 153378676.8
    Private Const NORTHWEST_CORNER_LON As Double = -301326109.2
    Private Const SOUTHWEST_CORNER_LAT As Double = 153217998.0
    Private Const SOUTHWEST_CORNER_LON As Double = -301313210.4
    Private Const SOUTHEAST_CORNER_LAT As Double = 153232621.2
    Private Const SOUTHEAST_CORNER_LON As Double = -301173238.8
    Private Const NORTHEAST_CORNER_LAT As Double = 153357580.8
    Private Const NORTHEAST_CORNER_LON As Double = -301181194.8
    Private Const NORTH_MIDPOINT_LAT As Double = 153381236.4
    Private Const NORTH_MIDPOINT_LON As Double = -301250563.2

    Private Const DEFAULT_DATA_COLLECTION_RATE_MSEC As Integer = 50
    Private Const DEFAULT_DISPLAY_REFRESH_RATE_MSEC As Integer = 50
    Private Const DEFAULT_FORM_WIDTH As Integer = 800
    Private Const DEFAULT_FORM_HEIGHT_600 As Integer = 600
    Private Const DEFAULT_FORM_HEIGHT As Integer = 600
    Private Const NUM_BUTTONS_ACROSS As Integer = 6
    Private Const HORIZ_BUTTON_SPACING As Integer = 2
    Private Const VERT_BUTTON_SPACING As Integer = 2
    'Private Const DEFAULT_FONT_SIZE As Integer = 11
    Private Const DEFAULT_FONT_SIZE As Integer = 10

    Private Const MAIN_TAB_TOP As Integer = 195
    Private Const MAIN_TAB_LEFT As Integer = 0
    Private Const SUB_TAB_TOP As Integer = 10
    Private Const SUB_TAB_LEFT As Integer = 2
    Private Const DEFAULT_BUTTON_HEIGHT As Integer = 60 'was 70
    Private Const DEFAULT_BUTTON_WIDTH As Integer = 128
    Private Const DEFAULT_BUTTON_TOP As Integer = 5
    Private Const DEFAULT_BUTTON_LEFT As Integer = HORIZ_BUTTON_SPACING
    Private Const STATUS_GREEN As Integer = 0
    Private Const STATUS_RED As Integer = 1
    Private Const MAX_NUM_FORMS As Integer = 50

    Private Const GRID_COL_WIDTH_SIZING_MULTIPLIER As Integer = 15
    Private Const GRID_WIDTH_SIZING_MULTIPLIER As Double = 0.9
    Private Const GRID_HEIGHT_SIZING_MULTIPLIER As Double = 0.065
    Private Const STATUS_LABEL_HEIGHT As Integer = 60
    Private Const STATUS_LABEL_SPACING As Integer = 2

    Private MAIN_TAB_WIDTH As Integer = DEFAULT_FORM_WIDTH - 10
    Private MAIN_TAB_HEIGHT As Integer = DEFAULT_FORM_HEIGHT - 230
    'Private SUB_TAB_FONT_SIZE As Integer = 16
    Private SUB_TAB_FONT_SIZE As Integer = 14
    Private SUB_TAB_WIDTH As Integer = DEFAULT_FORM_WIDTH - 20
    Private SUB_TAB_HEIGHT As Integer = DEFAULT_FORM_HEIGHT - 340
    Private STATUS_LABEL_WIDTH As Integer = 54

    'Public Const MENU_FONT_SIZE As Integer = 16
    Public Const MENU_FONT_SIZE As Integer = 14
    Public Const DEFAULT_FORM_WIDTH_800 As Integer = 800

    Public Const NUM_PREDEFINED_DISPLAYS As Integer = 9

    Public RecordTransitionDelay As Integer
    Public UserConfigFileName As String
    Public SignalRegistrationMode As String
    Public DataCollectionRate As Integer
    Public CheckForExperiment As Boolean
    Public VariableNameDataArray(,) As String
    Public SaveLineNumber As Integer
    Public myLogin As ToolStripMenuItem
    Public myDFs(0 To FormDataClass.MAX_NUM_FORMS) As FormDataClass
    Public myTDGraphicsContainer As TDGraphicsContainerClass
    Public mySignalDataWithTime() As IGM_INCA_Comm.TransferDataWithTime
    Public BaseDataCollectionPath As String
    Public FormForGridAdd As String

    Public myMainTabControl As TabControl
    Public mySubTabControl As TabControl
    Public myLabel() As Label
    Public RecordWAVTime As String
    Public NumSignalsAdded As Integer
    Public myTestThread As Thread
    Public StopTestProcess As Boolean
    'Public CancelCameraSearch As Boolean

    'Mileage calculation variables...
    Public StartingMileage As Double

    Public CameraIPAddresses As String() = {"192.168.40.101", "192.168.40.102", "192.168.40.103", "192.168.40.104", "192.168.40.105", "192.168.40.106", "192.168.40.107", "192.168.40.108", "192.168.40.109"}
    Public ButtonContainers() As ButtonContainer

    Public myExitButtons() As Button

    Public myToolStripMenuItem As ToolStripMenuItem
    Public ProgressBarEnable As Boolean

    Private VehicleID As String
    Public AvailableObjectIDs() As String = {"FUSION", "LRR", "LFSRR", "RFSRR", "VIS"}
    Private PerformanceData(,) As String
    Private myMiscInfo As ToolStripMenuItem
    Private myUploadData As ToolStripMenuItem
    Private myRecordPlayback As ToolStripMenuItem
    Private LCCActiveMileage As Double
    Private LCCActiveStartingMileage As Double
    Private OnPropertyMileage_Recording As Double
    Private OnPropertyMileage_NotRecording As Double
    Private OffPropertyMileage_Recording As Double
    Private OffPropertyMileage_NotRecording As Double
    Private UnknownMileage_NotRecording As Double
    Private UnknownMileage_Recording As Double
    Private OnPropertyStartingODO_Recording As Double
    Private OnPropertyStartingODO_NotRecording As Double
    Private OffPropertyStartingODO_Recording As Double
    Private OffPropertyStartingODO_NotRecording As Double
    Private UnknownStartingODO_NotRecording As Double
    Private UnknownStartingODO_Recording As Double
    Private OnProperty_Recording As Boolean
    Private OnProperty_NotRecording As Boolean
    Private OffProperty_Recording As Boolean
    Private OffProperty_NotRecording As Boolean
    Private Unknown_Recording As Boolean
    Private Unknown_NotRecording As Boolean
    Private HealthCounter As Integer
    Private HealthMonitor As Integer
    Private INCAInitStarted As Boolean
    Private KillINCA As Boolean
    Private KillProcesses As Boolean
    Private SignalsRegistered As Integer
    Private EnableMyBackgroundTasks As Boolean
    Private SaveBaseDataCollectionPath As String
    Private SaveSwitchToName As String
    Private Initializing As Boolean
    Private myCreateNewDisplayMenuItem As ToolStripMenuItem
    Private TopDownSignalsRegistered As Boolean
    Private TotalNumActiveDevices As Integer
    Private DisplayRefreshRate As Integer
    Private LoginButton() As Button
    Private WasMinimized As Boolean
    Private SwitchToMain As Boolean
    Private KeepListBoxDisplayed As Boolean
    Private ContinueExecution As Boolean
    Private NumInvalid As Integer
    Private ActualCameraIPAddresses() As String
    Private InMeasureMode As Boolean
    Private myCPUStaleData As Boolean
    Private myDeviceStatus As Boolean
    Private LostDevice As Boolean
    Private RegisterIntoNewBlankExp As Boolean
    Private gonogocount As Integer
    Private INCALaunched As Boolean
    Public CheckForNewerSignalListComplete As Boolean
    Private WhereAmIAt As String 'Undefined, Unknown, On Property, Off Property
    Private myMenuStrip As MenuStrip
    Private NewMainTabWidth As Double
    Public NewMainTabHeight As Double
    Private NewSubTabHeight As Double
    Private NewSubTabWidth As Double
    Private TotalNumSignalsDisplayed As Integer
    Private EnablePerformanceMonitoring As Boolean
    Private _BackgroundTasks As BackgroundTasks
    Private myKillerThread As Thread
    Private ProgressBarMax As Integer
    Private LCCActive As Integer

    Public WhichTabAmIOn As Integer
    Public HotKeySelected As String

    Delegate Sub BackgroundTasks()

    Public Sub TriggerEncryptAndCopy(ByVal SubfolderName As String, ByVal savefilename As String, ByVal filenamewithpath As String, ByVal AllFiles As Boolean)

        Dim tempfilename As String

        If InStr(savefilename, ".log") = 0 Then
            Encrypt(filenamewithpath)
            tempfilename = savefilename & ".encrypt"
        Else
            tempfilename = savefilename
        End If

        CopyFileToDDrive(SubfolderName, tempfilename, AllFiles)

    End Sub

    Public Sub EncryptFilesInDirectory(ByVal DirectoryName As String, Optional ByVal AllFiles As Boolean = False)

        'Called from a thread separate from the main execution, every 10 seconds, also called when the form is exited...
        'Encrypts the appropriate files and copies them to the D: Drive, also encrypts and copies additional files that
        'are being written to throughout the recording session, so it is called after everything is finished and the user
        'is exiting the app.  Only in effect if a flash drive with a specific directory configuration is put into the USB
        'drive...

        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sf As Scripting.Folder
        Dim sfile As Scripting.File
        Dim myElapseTime As TimeSpan
        Dim mySaveTime As DateTime
        Dim SaveFileName As String = ""
        Static inhere As Boolean
        Dim filecount As Integer
        Dim yesterday As DateTime

        Try

            If inhere = True Then
                inhere = False
                Exit Sub
            Else
                inhere = True
            End If

            'We are only going to encrypt files that were created within the last 24 hours.  Should there be older files still on the local drive
            'from days back that have not been uploaded, we do not want to encrypt those...
            yesterday = Now.AddDays(-1)

            'Look for files in the main vehicle name directory...
            If System.IO.Directory.Exists(DirectoryName) Then

                FSO = New Scripting.FileSystemObject

                f = FSO.GetFolder(DirectoryName)

                For Each sfile In f.Files

                    If InStr(sfile.Name, Format(Now, "yyyyMMdd")) > 0 Or InStr(sfile.Name, Format(Now, "MMddyyyy")) > 0 Or InStr(sfile.Name, Format(yesterday, "yyyyMMdd")) > 0 Or InStr(sfile.Name, ".log") > 0 Or InStr(sfile.Name, ".csv") > 0 Then

                        SaveFileName = sfile.Name

                        System.Threading.Thread.Sleep(1000)

                        mySaveTime = Now
                        myElapseTime = Now.Subtract(mySaveTime)

                        While FileInUse(sfile.Path) = True And myElapseTime.Seconds < 20
                            System.Threading.Thread.Sleep(100)
                            myElapseTime = Now.Subtract(mySaveTime)
                        End While

                        If FileInUse(sfile.Path) = False Then
                            'AllFiles is only set to true when this sub is called on app exit...
                            If AllFiles = False Then
                                If InStr(SaveFileName, ".mf4") = 0 And InStr(SaveFileName, ".csv") = 0 And InStr(SaveFileName, ".encrypt") = 0 Then

                                    TriggerEncryptAndCopy("", SaveFileName, sfile.Path, AllFiles)

                                    'If InStr(SaveFileName, ".log") = 0 Then
                                    'Encrypt(sfile.Path)
                                    'tempfilename = SaveFileName & ".encrypt"
                                    'Else
                                    'tempfilename = SaveFileName
                                    'End If

                                    'CopyFileToDDrive("", tempfilename, AllFiles)

                                End If
                            Else
                                If InStr(SaveFileName, ".mf4") = 0 And InStr(SaveFileName, ".encrypt") = 0 Then

                                    TriggerEncryptAndCopy("", SaveFileName, sfile.Path, AllFiles)

                                    'If InStr(SaveFileName, ".log") = 0 Then
                                    'Encrypt(sfile.Path)
                                    'tempfilename = SaveFileName & ".encrypt"
                                    'Else
                                    'tempfilename = SaveFileName
                                    'End If

                                    'CopyFileToDDrive("", tempfilename, AllFiles)

                                End If
                            End If

                        Else
                            CopyToLog("EncryptFilesInDirectory " & sfile.Path & " in use...")
                        End If

                    End If

                Next

                'AllFiles is only set to true when this sub is called on app exit. When we are exiting, we will check to make sure
                'that the files that are supposed to have been copied to the flash drive exist on the flash drive and then delete the files
                'from the main vehicle directory on  the local drive...
                If AllFiles = True Then

                    System.Threading.Thread.Sleep(1000) 'was 2000

                    f = FSO.GetFolder(DirectoryName)

                    For Each sfile In f.Files
                        If FileInUse(sfile.Path) = False Then

                            If InStr(sfile.Path, ".encrypt") > 0 Or InStr(sfile.Name, ".log") > 0 Then
                                If File.Exists(NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & "\" & sfile.Name) = True Then
                                    CopyToLog("EncryptFilesInDirectory: Deleting " & sfile.Path)
                                    sfile.Delete()
                                Else
                                    CopyToLog("EncryptFilesInDirectory: " & sfile.Name & " not found on flash drive, file was not deleted.")
                                End If
                            End If

                        Else
                            CopyToLog("EncryptFilesInDirectory " & sfile.Path & " in use...")
                        End If

                    Next

                End If

                'Look for files in session directories below the main vehicle directory...              
                For Each sf In f.SubFolders

                    If InStr(sf.Name, Format(Now, "yyyyMMdd")) > 0 Or InStr(sf.Name, Format(yesterday, "yyyyMMdd")) > 0 Then

                        For Each sfile In sf.Files

                            SaveFileName = sfile.Name

                            If InStr(SaveFileName, ".mf4") = 0 And InStr(SaveFileName, ".encrypt") = 0 And InStr(SaveFileName, ".asc") = 0 And InStr(SaveFileName, ".vsb") = 0 And InStr(SaveFileName, ".mdf") = 0 Then

                                System.Threading.Thread.Sleep(1000)

                                mySaveTime = Now
                                myElapseTime = Now.Subtract(mySaveTime)

                                While FileInUse(sfile.Path) = True And myElapseTime.Seconds < 20
                                    System.Threading.Thread.Sleep(100)
                                    myElapseTime = Now.Subtract(mySaveTime)
                                End While

                                If FileInUse(sfile.Path) = False Then

                                    If AllFiles = False Then
                                        If InStr(SaveFileName, ".csv") = 0 And InStr(SaveFileName, "mp4_convert") = 0 And InStr(SaveFileName, "mf4_attach") = 0 Then

                                            TriggerEncryptAndCopy(sf.Name, SaveFileName, sfile.Path, AllFiles)

                                            'If InStr(SaveFileName, ".log") = 0 Then
                                            '
                                            'Encrypt(sfile.Path)
                                            'tempfilename = SaveFileName & ".encrypt"

                                            'Else
                                            'tempfilename = SaveFileName
                                            'End If

                                            'CopyFileToDDrive(sf.Name, tempfilename, AllFiles)

                                        End If
                                    Else

                                        If InStr(SaveFileName, "mp4_convert") > 0 Or InStr(SaveFileName, "mf4_attach") > 0 Then
                                            sfile.Delete()
                                            Continue For
                                        End If

                                        TriggerEncryptAndCopy(sf.Name, SaveFileName, sfile.Path, AllFiles)

                                        'If InStr(SaveFileName, ".log") = 0 Then

                                        'Encrypt(sfile.Path)
                                        'tempfilename = SaveFileName & ".encrypt"

                                        'Else
                                        'tempfilename = SaveFileName
                                        'End If

                                        'CopyFileToDDrive(sf.Name, tempfilename, AllFiles)

                                    End If

                                Else
                                    CopyToLog("EncryptFilesInDirectory " & sfile.Path & " in use...")
                                End If

                            End If

                        Next

                    End If

                Next

                'AllFiles is only set to true when this sub is called on app exit. When we are exiting, we will check to make sure
                'that the files that are supposed to have been copied to the flash drive exist on the flash drive and then delete the files
                'from the session folders on the local drive...
                If AllFiles = True Then

                    System.Threading.Thread.Sleep(1000)

                    f = FSO.GetFolder(DirectoryName)

                    For Each sf In f.SubFolders

                        If InStr(sf.Name, Format(Now, "yyyyMMdd")) > 0 Or InStr(sf.Name, Format(yesterday, "yyyyMMdd")) > 0 Then

                            For Each sfile In sf.Files
                                If File.Exists(NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & "\" & sf.Name & "\" & sfile.Name) Then
                                    If FileInUse(sfile.Path) = False Then
                                        CopyToLog("EncryptFilesInDirectory: Deleting " & sfile.Path)
                                        sfile.Delete()
                                    Else
                                        CopyToLog("EncryptFilesInDirectory " & sfile.Path & " in use...")
                                    End If

                                End If
                            Next

                            filecount = 0
                            For Each sfile In sf.Files
                                filecount = filecount + 1
                            Next

                            If filecount = 0 Then
                                CopyToLog("EncryptFilesInDirectory: " & sf.Name & " is empty, deleting...")
                                sf.Delete()
                            Else
                                CopyToLog("EncryptFilesInDirectory: There are still un-transferred files in " & sf.Name & " - not deleting directory.")
                            End If

                        End If

                    Next
                End If

            End If
            inhere = False
        Catch ex As Exception
            CopyToLog("EncryptFilesInDirectory: " & ex.Message & " - " & SaveFileName)
            inhere = False
        End Try

    End Sub

    Sub CopyFileToDDrive(ByVal DirName As String, ByVal Filename As String, ByVal AllFiles As Boolean)

        'Called from TriggerEncryptAndCopy which is called from EncryptFilesInDirectory.  Copies encrypted files to the Flash drive.
        'Note:  Drive letter designation may be something other than D: Drive...

        Dim myprocess As Process
        Dim ExecutableFile As String = "C:\csvscripts\robocopy.exe"
        Dim p As New ProcessStartInfo

        Dim RoboParams As String

        'run robocopy routine

        p.WindowStyle = ProcessWindowStyle.Hidden
        p.FileName = ExecutableFile

        'RoboParams = " /R:1 /move /s"
        'RoboParams = " /R:1 /mov"
        RoboParams = " /R:1"

        If Len(DirName) > 0 Then
            p.Arguments = BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & "\" & DirName & " " & NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & "\" & DirName & " " & Filename & RoboParams
        Else
            p.Arguments = BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & " " & NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & " " & Filename & RoboParams
        End If

        myprocess = Process.Start(p)

        'If AllFiles = True, we are exiting, so we will wait until copying is complete before exiting. If we are recording, we zip, encrypt and copy "on the fly" so 
        'we continue even if the copy is not yet complete...
        If AllFiles = True Then
            myprocess.WaitForExit()
        End If

    End Sub

    Public Function ReadVehicleConfigsFile() As Boolean

        'Called from GM_ResidentClient.HandleWirelessConnection and InitForm.SaveVehicleNumber

        'Read and parse the vehicleconfigurations.csv file.  Parses the contents of the file based on the
        'VehicleNumber (read from vehicleconfig.txt file) and puts info into variables...

        Dim fnum As Integer
        Dim textline As String = ""
        Dim filename As String = ""
        Dim NEW_FILE_FORMAT As Boolean = False
        Dim lineitems() As String

        Dim SaveToNewFormat As Boolean

        Dim PROC_START As Integer = 2
        Dim PROC_END As Integer = 7
        Dim CAMERA_START As Integer = 8
        Dim CAMERA_END As Integer = 13
        Dim CANMON_START As Integer = 14
        Dim BLUEBOX_DESIGNATION As Integer = 15
        Dim CANMON_END As Integer = 16
        Dim DATA_UPLOAD_PATH As Integer = 17
        Dim CLEVIR_FILES_PATH As Integer = 18
        Dim ZIP_MF4_FILES As Integer = 20
        Dim CONFIG_NAME As Integer = 21

        Dim x As Integer
        Dim cnt As Integer

        Dim NewFileFormat() As String = Nothing

        Dim FoundFRONT As Boolean
        Dim BlueBoxID As String = ""

        Try

            ReadVehicleConfigsFile = False
            NumControllers = 0

            filename = My.Application.Info.DirectoryPath & "\VehicleConfigurations.csv"

            If System.IO.File.Exists(filename) Then

                System.IO.File.Copy(filename, My.Application.Info.DirectoryPath & "\VehicleConfigurations_SAVE.csv", True)

            ElseIf System.IO.File.Exists(My.Application.Info.DirectoryPath & "\VehicleConfigurations_SAVE.csv") Then

                CopyToLog("ReadVehicleConfigsFile: Copying VehicleConfigurations_SAVE.csv to VehicleConfigurations.csv ")

                System.IO.File.Copy(My.Application.Info.DirectoryPath & "\VehicleConfigurations_SAVE.csv", filename, True)

            Else
                CopyToLog("ReadVehicleConfigsFile: Neither vehicleconfigurations.csv file nor VehicleConfigurations_SAVE.csv exist...")
                MsgBox("vehicleconfigurations.csv file does not exist or is corrupted, Exiting...")
                Exit Function
            End If

            If System.IO.File.Exists(filename) Then

                If Not FileInUse(filename) Then

                    fnum = FreeFile()

                    FileOpen(fnum, filename, OpenMode.Input)

                    textline = LineInput(fnum)
                    lineitems = Split(textline, ",")

                    'There was a change made to the vehicleconfigurations.csv file midstream, so this code
                    'was required to update the old file format to the new file format...

                    'Vehicle Number	Vehicle ID	Proc 1	Proc 2	Proc 3	Proc 4	Proc 5	Proc 6	Camera 1	Camera 2	Camera 3	Camera 4	Camera 5	Camera 6	CAN Mon 1	CAN Mon 2	CAN Mon 3	Data Upload Path	CLEVIR Files Path	INCA Workspace Path	Zip MF4 Files	Config Name	ConfigNum
                    If lineitems(17) <> "Data Upload Path" Then
                        CANMON_START = 14
                        BLUEBOX_DESIGNATION = 15
                        CANMON_END = 17
                        DATA_UPLOAD_PATH = 18
                        CLEVIR_FILES_PATH = 19
                    Else
                        ReDim NewFileFormat(0)
                        NewFileFormat(0) = ConvertToNewLineFormat(lineitems)
                        SaveToNewFormat = True

                    End If

                    Do While Not EOF(fnum)

                        textline = LineInput(fnum)
                        lineitems = Split(textline, ",")

                        If SaveToNewFormat = True Then
                            cnt = cnt + 1
                            ReDim Preserve NewFileFormat(cnt)
                            NewFileFormat(cnt) = ConvertToNewLineFormat(lineitems)
                        End If

                        If UCase(lineitems(0)) = UCase(VehicleNumber) Then
                            NumberOfCameras = 0
                            VehicleID = lineitems(1)
                            For x = PROC_START To ZIP_MF4_FILES 'CONFIG_NAME

                                If x >= PROC_START And x <= PROC_END Then
                                    If InStr(lineitems(x), "NA") = 0 Then
                                        NumControllers = NumControllers + 1
                                    End If
                                End If

                                If x >= CAMERA_START And x <= CAMERA_END Then
                                    CameraNames(x - CAMERA_START) = lineitems(x)

                                    If lineitems(x) <> "NA" Then 'we have a camera name defined...

                                        'FRONT	RIGHTREAR	LEFTREAR	HMI	LEFTFRONT	RIGHTFRONT
                                        If lineitems(x) = "FRONT" Then  'ALL CAPS FRONT indicates vehicle was configured for VALIDATION...
                                            FoundFRONT = True
                                        End If

                                        NumberOfCameras = NumberOfCameras + 1
                                        CopyToLog("ReadVehicleConfigsFile: Number of Cameras defined in VehicleConfigurations.csv = " & NumberOfCameras)
                                    End If
                                End If

                                'Blue Box designation of 592 at this position indicates a deviation from typical setup so we need to put "592" into template name
                                'to differentiate between normal template configuration which uses a 593 and will not contain "592" in the name...
                                If x = BLUEBOX_DESIGNATION Then
                                    BlueBoxID = lineitems(x)
                                End If

                                'Data upload path indicates the base folder where data files are to be copied.
                                Select Case x
                                    Case DATA_UPLOAD_PATH
                                        If Len(lineitems(x)) > 0 Then
                                            DataUploadPath = "\" & lineitems(x)
                                        Else
                                            DataUploadPath = ""
                                        End If

                                'The clevir files path in the vehicleconfigurations.csv file is important because it establishes
                                'what we call the ProjectName, i.e. are we CSAV2 or CoPilot, LowContent, etc.  Each project has
                                'subtle differences in terms of how CLEVIR behaves.  Here is where we make that distinction...

                                    Case CLEVIR_FILES_PATH
                                        CLEVIRFilesPath = lineitems(x)

                                        'If InStr(UCase(CLEVIRFilesPath), "COPILOT") > 0 Then
                                        'ProjectName = "CoPilot"
                                        ' g_ProjectAbbreviation = "CP"
                                        If InStr(UCase(CLEVIRFilesPath), "LOWCONTENT") > 0 Then
                                            ProjectName = "LowContent"
                                            g_ProjectAbbreviation = "LC"
                                            If BlueBoxID = "523" Then
                                                BlueBoxID = ""
                                            End If

                                            'FCM CHANGE
                                        ElseIf InStr(UCase(CLEVIRFilesPath), "FCM") > 0 Then
                                            ProjectName = "FCM"
                                            g_ProjectAbbreviation = "FCM"

                                        ElseIf InStr(UCase(CLEVIRFilesPath), "HIGHCONTENT") > 0 Then
                                            ProjectName = "HighContent"
                                            g_ProjectAbbreviation = "HC"
                                            If BlueBoxID = "523" Then
                                                BlueBoxID = ""
                                            End If
                                        Else
                                            ProjectName = "CSAV2"
                                            g_ProjectAbbreviation = "CSAV2"
                                            If BlueBoxID = "593" Then
                                                BlueBoxID = ""
                                            End If
                                        End If

                                        'The BlankExperimentName is used when creating a new experiment from scratch.  Each Project "blank" experiment
                                        'is different because it starts with all of the CAN channel signals already defined and adds to this list
                                        'the internal variables.  So, CSAV2 blank experiment contains CAN signals based on the .dbc file for the
                                        'current CSAV2 software version, whereas LowContent Blank experiment contains CAN signals based on the .arxml
                                        'file for the current LowContent software version.
                                        BlankExperimentName = ProjectName & " Blank Experiment"

                                    Case ZIP_MF4_FILES
                                        ZipTheMF4Files = IIf(UCase(lineitems(x)) = "TRUE", True, False)

                                        If FoundFRONT = True And ZipTheMF4Files = False Then
                                            OriginalVehicleConfiguration = "VALIDATION"
                                            If CurrentVehicleUsage <> "DEVELOPMENT" Then
                                                CurrentVehicleUsage = "VALIDATION"
                                            End If
                                        ElseIf FoundFRONT = False And ZipTheMF4Files = True Then
                                            OriginalVehicleConfiguration = "DEVELOPMENT"
                                        Else
                                            OriginalVehicleConfiguration = "AMBIGUOUS"
                                        End If

                                        CopyToLog("ReadVehicleConfigFile: OriginalVehicleConfiguration = " & OriginalVehicleConfiguration)

                                        If UsingFlashDrive = True Then
                                            InitForm.RadioButton1.Checked = True
                                            CopyToLog("ReadVehicleConfigFile: UsingFlashdrive = True. CurrentVehicleUsage = " & CurrentVehicleUsage & " ZipTheMF4Files set to True")
                                        End If

                                        'As of version 5.4.11, we no longer use the config name field to determine workspace template name and template to be used.
                                        'We now use templates based on software version and model year, if available, and derive INCAWorkspaceTemplateName from
                                        'this information...

                                        'Case CONFIG_NAME

                                        'INCAWorkspaceTemplateName = lineitems(x)
                                        'g_SaveINCAWorkspaceTemplateName = INCAWorkspaceTemplateName

                                End Select

                            Next x

                            If SaveToNewFormat = False Then
                                Exit Do
                            End If

                        End If

                    Loop

                    If Len(VehicleID) = 0 And UCase(VehicleNumber) <> "UNDEFINED" Then
                        MsgBox("Vehicle Number " & VehicleNumber & " not found in Vehicle Configrations File, Exiting...")
                        FileClose(fnum)
                        Exit Function
                    End If

                    'set up INCAWorkspaceTemplateName and WorkspaceNameSuffix
                    'INCAWorkspaceTemplateName is built using the projectabbreviation, and other information that comes from reading the vehicleconfiguration.csv file
                    'which tells us how many processors, which types of CAN blue boxes, etc.

                    Select Case g_ProjectAbbreviation

                        Case "CP", "CSAV2"

                            If NumControllers = 3 Then
                                INCAWorkspaceTemplateName = g_ProjectAbbreviation & "_3P" & BlueBoxID
                            ElseIf NumControllers = 6 Then
                                INCAWorkspaceTemplateName = g_ProjectAbbreviation & "_3P3R" & BlueBoxID
                            Else
                                CopyToLog("Invalid contents in vehicleconfigurations.csv file for " & VehicleNumber)
                                MsgBox("Invalid contents in vehicleconfigurations.csv file for " & VehicleNumber & " Exiting...")
                                Exit Function
                            End If

                        Case "FCM"

                            INCAWorkspaceTemplateName = g_ProjectAbbreviation & "_" & NumControllers & "P" & BlueBoxID

                        Case "LC"

                            If NumControllers = 1 Then
                                INCAWorkspaceTemplateName = g_ProjectAbbreviation & "_1P" & BlueBoxID
                            Else
                                CopyToLog("Invalid contents in vehicleconfigurations.csv file for " & VehicleNumber)
                                MsgBox("Invalid contents in vehicleconfigurations.csv file for " & VehicleNumber & " Exiting...")
                                Exit Function
                            End If

                        Case "HC"

                            If NumControllers = 2 Then
                                INCAWorkspaceTemplateName = g_ProjectAbbreviation & "_2P" & BlueBoxID
                            Else
                                CopyToLog("Invalid contents in vehicleconfigurations.csv file for " & VehicleNumber)
                                MsgBox("Invalid contents in vehicleconfigurations.csv file for " & VehicleNumber & " Exiting...")
                                Exit Function
                            End If

                    End Select

                    WorkspaceNameSuffix = INCAWorkspaceTemplateName & NumberOfCameras & "C"

                    'We need to save these names incase we have to revert back to these original names.  We would have to do this if the
                    'software version number and model year specific templates are not yet available.  

                    g_SaveINCAWorkspaceTemplateName = INCAWorkspaceTemplateName
                    g_SaveWorkspaceNameSuffix = WorkspaceNameSuffix

                    FileClose(fnum)

                    If SaveToNewFormat = True Then

                        fnum = FreeFile()
                        FileOpen(fnum, My.Application.Info.DirectoryPath & "\VehicleConfigurations.csv", OpenMode.Output)

                        For x = 0 To UBound(NewFileFormat)
                            PrintLine(fnum, NewFileFormat(x))
                        Next x
                        FileClose(fnum)
                    End If

                    ReadVehicleConfigsFile = True

                Else
                    CopyToLog("ReadVehicleConfigsFile: Vehicle Configurations file " & filename & " in use, Exiting...")
                    MsgBox("Vehicle Configurations file " & filename & " in use.  Please close the file and restart CLEVIR. Exiting...")
                End If

            Else
                CopyToLog("ReadVehicleConfigsFile: Vehicle Configurations file " & filename & " not found, Exiting...")
                MsgBox("Vehicle Configurations file " & filename & " not found, Exiting...")

            End If

        Catch ex As Exception

            CopyToLog("ReadVehicleConfigsFile: " & ex.Message)
            MsgBox("VehicleConfigurations.csv file may be corrupted, Exiting...")

        End Try

    End Function

    Public Sub KillVNC()

        'Called from ExitApp:  Stops the VNC server, called when we close the CLEVIR app...

        'If the winvnc4 process is running, we will kill it.  The winvnc process may or may not be
        'running.  This depends on whether or not the process was started on initialization, which
        'depends on whether or not the VNC server files have been placed in the C:\RealVNC4\ directory.

        Dim current As Process = Process.GetCurrentProcess()
        Dim processes As Process() = Process.GetProcesses
        Dim ThisProcess As Process

        If Debugger.IsAttached = True Then
            Exit Sub
        End If

        UserStatusInfo.Label1.Text = "Killing VNC Process..."

        For Each ThisProcess In processes
            '-- Ignore the current process 
            If ThisProcess.Id <> current.Id Then
                '-- Only list processes that have a Main Window Title 
                If InStr(ThisProcess.ProcessName, "winvnc4") > 0 Then
                    ThisProcess.Kill()
                End If
                'End If
            End If
        Next

        UserStatusInfo.Hide()
    End Sub

    Private Sub SetInitialDisplayProperties()

        'Called from initialize routine, sizes and positions various controls on
        'both the GM_ResidentClient form and the OnVehicleScreen form...

        Dim myDirectories As String()
        Dim x As Integer
        Dim tempstr As String

        OnVehicleScreen.GroupBox1.Visible = True
        OnVehicleScreen.GroupBox1.Left = 1
        OnVehicleScreen.GroupBox1.Top = 0
        OnVehicleScreen.GroupBox1.Width = DEFAULT_FORM_WIDTH - 5
        OnVehicleScreen.GroupBox1.Height = DEFAULT_FORM_HEIGHT - 38

        Me.Width = DEFAULT_FORM_WIDTH
        Me.Height = DEFAULT_FORM_HEIGHT

        OnVehicleScreen.Show()
        OnVehicleScreen.BringToFront()

        Me.GroupBox1.Visible = True
        Me.GroupBox1.Left = 1
        Me.GroupBox1.Top = 30
        Me.GroupBox1.Width = DEFAULT_FORM_WIDTH - 18
        Me.GroupBox1.Height = DEFAULT_FORM_HEIGHT - 10

        'Displays are set up differently depending on whether or not we are In Vehicle.
        'Operating modes are reflective of which PC we are running on...

        '_VPC indicates modes where we are running on a vehicle
        'VPC - Vehicle PC

        'Other modes would be configuration modes where we are running the application on
        'our laptops...  

        If OperatingMode <> OperatingModes.RES_ON_VPC And
           OperatingMode <> OperatingModes.RES_ON_SUITCASE_VPC Then
            Me.Height = DEFAULT_FORM_HEIGHT + 30
        Else
            Me.Height = DEFAULT_FORM_HEIGHT - 30
        End If

        Me.Button1.Left = Me.Width - Me.Button1.Width - 20
        Me.Button1.Top = Me.Height - Me.Button1.Height - 75

        Me.Label2.Parent = GroupBox1
        Me.Label2.Left = 5
        Me.Label2.Top = GroupBox1.Top + ((GroupBox1.Height / 2) - (Label2.Height))
        Me.Label2.Width = GroupBox1.Width - 10

        OnVehicleScreen.Label17.Left = OnVehicleScreen.Button1.Left

        RecordPlayback.GroupBox2.Text = "PLAYBACK"

        'Here is where we add the user names that are associated with each configured INCA User
        'to the toolstripcombobox selection list.  Based on current use cases, this is really no longer
        'applicable...

        ToolStripComboBox3.Items.Add("Add Custom INCA Setup")

        If System.IO.Directory.Exists(ETAS_USER_PATH) Then
            myDirectories = System.IO.Directory.GetDirectories(ETAS_USER_PATH)

            For x = 0 To UBound(myDirectories)
                tempstr = Mid(myDirectories(x), InStr(myDirectories(x), "\User\") + 6, Len(myDirectories(x)))
                ToolStripComboBox3.Items.Add(tempstr)

                If UCase(tempstr) = ETAS_DEFAULT_USER_NAME Then
                    ToolStripComboBox3.SelectedIndex = x + 1
                    ToolStripComboBox3.Text = UCase(tempstr)
                    SaveSwitchToName = ToolStripComboBox3.Text
                End If
            Next

        End If

        DisplayRefreshRate = DEFAULT_DISPLAY_REFRESH_RATE_MSEC
        ToolStripComboBox1.Text = CStr(DisplayRefreshRate)

        DataCollectionRate = DEFAULT_DATA_COLLECTION_RATE_MSEC
        ToolStripComboBox2.Text = CStr(DataCollectionRate)

    End Sub

    Public Function ExitApp(Optional ByVal ExitParameter As String = "") As Boolean

        'Called from various places when we want to exit the application and perform
        'some housekeeping...

        Dim x As Integer
        Dim uflags As Long = 0

        Try

            ExitApp = True

            CopyToLog("ExitApp (" & ExitParameter & ") called...")

            If Is7ZipRunning() = True Then

                If MsgBox("File Zipping is not yet complete. Wait for file Zipping to complete before Exiting?", vbYesNo) = vbYes Then
                    CopyToLog("ExitApp: Waiting on 7zip to finish...")
                    Do While Is7ZipRunning() = True
                        System.Threading.Thread.Sleep(2000)
                    Loop
                Else
                    CopyToLog("ExitApp: User decided not to wait for Zipping to complete before exiting...")
                End If
            End If

            Select Case ExitParameter

                Case ""

                    'Here we show the ExitAppForm, which consists of several buttons, each with a different
                    'exit scenario, i.e., exit just the resident client, exit everything including INCA, or 
                    'exit everyting and shut down windows.

                    ExitAppForm.ShowDialog()

                    If ExitAppForm.CancelExit = True Then
                        CopyToLog("ExitApp: Exit Cancelled")
                        ExitAppForm.CancelExit = False
                        ExitPressed = False
                        ExitApp = False
                        Exit Function
                    End If

                Case "Complete"

                    'If ExitParameter = "Complete" we will force a complete shutdown with no user choice from the exit menu...

                    CopyToLog("ExitApp: Exit complete Closing INCA")
                    CheckForExperiment = False
                    myINCAInterface.CloseINCA()

            End Select

            'If, during the runtime session, the user has gotten far enough to have a final path to save data defined, we will copy the current log file into this directory.
            'Otherwise, we will tack on the date and time and copy the file into the vehicle number directory.

            If Len(FinalPathToSaveData) > 0 Then
                UserStatusInfo.Label1.Text = "Copying GM_INCA_Comm.log to upload folder..."
                FileCopy(My.Application.Info.DirectoryPath & "\GM_INCA_Comm.log", FinalPathToSaveData & "\GM_INCA_Comm.log")
            Else

                Dim info As New System.IO.FileInfo(My.Application.Info.DirectoryPath & "\GM_INCA_Comm.log")

                'The GM_INCA_Comm.log file contains logging information directly related to INCA API communications, if this file
                'is less than 10000, it indicates that nothing much happened during this CLEVIR runtime session, so we won't bother with this file...
                If info.Length > 10000 Then
                    UserStatusInfo.Label1.Text = "Copying GM_INCA_Comm.log to upload folder..."
                    If Directory.Exists(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber) Then
                        FileCopy(My.Application.Info.DirectoryPath & "\GM_INCA_Comm.log", BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & "\GM_INCA_Comm_" & Format(Now, "MMddyyyy_hhmmss") & ".log")
                    Else

                        If Not Directory.Exists(BaseDataCollectionPath & "\Data") Then
                            Directory.CreateDirectory(BaseDataCollectionPath & "\Data")
                        End If
                        FileCopy(My.Application.Info.DirectoryPath & "\GM_INCA_Comm.log", BaseDataCollectionPath & "\Data\GM_INCA_Comm_" & Format(Now, "MMddyyyy_hhmmss") & ".log")

                    End If

                End If

            End If

            UserStatusInfo.Hide()

            CopyATT_TCPFilesToFinalPath()

            If CanalyzerStarted Then
                QuitCanalyzer()
            End If

            If VehicleSpyStarted Then
                QuitVehicleSpy()
            End If

            'Stop execution of the main loop which runs in myBackgroundTasks

            EnableMyBackgroundTasks = False
            FormDisplayed = False

            Me.Cursor = Cursors.WaitCursor

            UserStatusInfo.Label1.Text = "Deleting GM_INCA_Comm.log file..."

            File.Delete(My.Application.Info.DirectoryPath & "\GM_INCA_Comm.log")

            UserStatusInfo.Hide()

            'Write back to our INCAVariableFile (if changes have been made to its contents during the user session)
            'INCAVariableFile contents can change for example if the user moves grids around on displayed forms, or
            'if signals have been removed based on contents of InvalidSignalsLog.csv file during a FULL signal
            'registration or when creating a new experiment from a blank experiment...
            If UCase(CLEVIR_Flavor) = "DEVELOPMENT" Then

                If GridCellPropConfig.ChangesMade = True Then
                    CopyToLog("ExitApp: Save Signal List Changes?")
                    If MsgBox("Save Signal List Changes?", vbYesNo) = vbYes Then

                        CopyToLog("ExitApp: Save Signal List Changes = Yes")
                        WriteSignalListFile(INCAVariableFile)

                    Else
                        CopyToLog("ExitApp: Save Signal List Changes = No")
                    End If
                End If

            End If

            'Some file handling must take place to get files with the proper formats in the proper locations to be uploaded.  Here we perform some file manipulations
            'to prepare for data to be uploaded if we have recorded anything during the CLEVIR runtime session.
            'There are effectively three different mechanisms in play to upload data depending on various operating conditions.  
            'The three operating conditions that affect this file handling when exiting are...

            '1. If we are using a Flash drive
            '2. If we are running on a VALIDATION vehicle
            '3. Normal operation where files will be uploaded using the CLEVIR UploadData screen

            'This file handling will not occur if we are operating normally (no flash drive) in a DEVELOPMENT vehicle...
            'The assumption here is that we will never be using a flash drive when in a VALIDATION vehicle...
            If (UsingFlashDrive = True Or CurrentVehicleUsage = "VALIDATION") And HaveRecorded = True Then

                File.Copy(My.Application.Info.DirectoryPath & "\GM_ResidentClient.log", BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & "\GM_ResidentClient.log", True)

                AggregateAnnoFileName = My.Application.Info.DirectoryPath & "\" & VehicleNumber & "_AggregateAnnotations.csv"

                If File.Exists(AggregateAnnoFileName) Then
                    File.Copy(AggregateAnnoFileName, BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & "\" & VehicleNumber & "_AggregateAnnotations.csv", True)
                End If

                If CurrentVehicleUsage = "DEVELOPMENT" Then

                    System.Threading.Thread.Sleep(1000)
                    If ZipTheMF4Files = True Then
                        DeleteMF4Files(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber)
                    Else
                        DeleteMF4Files(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber, True)
                    End If

                    UserStatusInfo.Label1.Text = "Encrypting Files. Please be patient..."

                    System.Threading.Thread.Sleep(5000)

                    EncryptFilesInDirectory(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber, True)
                End If

            End If

            'Here, we will either save changes to the default configuration file, config.txt, or if the user has
            'logged in with a specific user id, we will save the changes to the user specific config file, which is the 6 character user id.txt

            UserStatusInfo.Label1.Text = "Writing changes to config file..."

            If Len(UserConfigFileName) = 0 Then
                WriteConfigFile()
            Else
                WriteUserConfigFile(UserConfigFileName)
            End If

            UserStatusInfo.Hide()

            KillVNC()

            If Not AnnotationValueRecord Is Nothing Then

                UserStatusInfo.Label1.Text = "Saving Custom Annotations..."

                For x = 0 To UBound(AnnotationValueRecord)

                    If Not AnnotationValueRecord(x).SaveCustomAnnotationText Is Nothing Then
                        CustomAnnotation.WriteCustomAnnotationFile(AnnotationValueRecord(x).SaveCustomAnnoFileName, AnnotationValueRecord(x).SaveCustomAnnotationText)
                    Else

                        If AnnotationValueRecord(x).EnumerationType = 3000 Or (AnnotationValueRecord(x).EnumerationType >= 3050 And AnnotationValueRecord(x).EnumerationType <= 3100) Then

                            Dim fnum As Integer
                            fnum = FreeFile()
                            FileOpen(fnum, AnnotationValueRecord(x).SaveCustomAnnoFileName, OpenMode.Output)
                            FileClose(fnum)

                        End If
                    End If

                Next

            End If

            UserStatusInfo.Hide()

            'CopySavedCustomAnnoFilesToCLEVIRFolder() 'commented out 04/09/2020, sometimes takes a long time, really dont need to do this since we are not
            'proliferating the file contents to other machines, which was the original intent...

            'If we have initiated a recording during the runtime session, we will make a check box available
            'to the user for them to indicate if they want to upload the data.  If they have checked the box,
            'on the ExitAppForm we will display the UploadData screen...

            If UploadDataOnExit = True And UsingFlashDrive = False Then

                CopyToLog("ExitApp: Upload Data Called on Exit")
                UploadDataScreen.UploadData()

            End If

            CopyToLog("ExitApp: Closing Display Forms...")

            If Not myDFs(0) Is Nothing Then
                For x = 0 To UBound(myDFs)
                    myDFs(x).Close()
                Next

            End If

            OperationHistory.Close()
            DeviceStatus.Close()
            TargetStatusDisplay.Close()
            PedestrianStatusDisplay.Close()
            FusionStatusDisplay.Close()
            CopilotStatusDisplay.Close()

            If Not LKAForm Is Nothing Then
                LKAForm.Close()
            End If

            If Not myTDGraphicsContainer Is Nothing Then
                myTDGraphicsContainer.Close()
            End If

            If Not myinca Is Nothing Then

                myINCAInterface.RCI2_Cleanup()
                myINCAInterface.UnlockExperiment()
                myinca.UnlockTool()
                myinca = Nothing
                MyHWC = Nothing

            End If

            If ShutdownWindows = True Or RestartWindows = True Then
                System.Threading.Thread.Sleep(3000)

                AcquireShutdownPrivilege()

                System.Threading.Thread.Sleep(5000)
                CopyToLog("ExitApp: Exiting Windows...")

                If Debugger.IsAttached = False Then

                    If ShutdownWindows = True Then
                        uflags = EWX_POWEROFF Or EWX_SHUTDOWN Or EWX_FORCE
                    Else
                        uflags = EWX_REBOOT Or EWX_FORCE
                    End If

                    ExitWindows(uflags, 0)

                    End

                End If

            End If

            UserStatusInfo.Label1.Text = "CLEVIR Exit in progress, performing housekeeping..."

            End

        Catch ex As Exception

            'right now, we will just copy the error to the log file and end on any shutdown error....

            CopyToLog("ExitApp: " & ex.Message)
            UserStatusInfo.Label1.Text = "Exit Complete"

            End

        End Try

    End Function
    Private Sub GetExperimentNameForUser(ByVal name As String)

        'Accessed from the Config screen.  This is legacy and not applicable any longer...

        'Each custom INCA setup (unique user name) has an experiment name associated with it.  Here we
        'read the INCAExperimentNames.txt file to extract the experiment name that is associated with
        'a given user name, (AKA custom INCA setup name).

        'Need to save the file input info here, so we can write it back after re-associating a user name
        'with an experiment name, which we do in this routine...

        Dim fnum As Integer
        Dim textline() As String
        Dim found As Boolean
        Dim x As Integer

        Dim TempExpName As String

        fnum = FreeFile()
        FileOpen(fnum, My.Application.Info.DirectoryPath & "\" & "INCAExperimentNames.txt", OpenMode.Input)

        x = 0
        ReDim textline(0)

        Do While Not EOF(fnum)
            If x > 0 Then
                ReDim Preserve textline(x)
            End If
            textline(x) = LineInput(fnum)
            If name = Mid(textline(x), 1, InStr(textline(x), "=") - 1) Then
                INCAExperiment = Mid(textline(x), InStr(textline(x), "=") + 1, Len(textline(x)))
                found = True
            End If
            x = x + 1
        Loop

        FileClose(fnum)

        If found = False Then
            MsgBox("Custom INCA Setup Name not found in INCAExperimentNames.txt file. Experiment Name not changed.")
        Else
            If MsgBox("INCA Experiment which will be used for Custom INCA Setup " & name & " is " & INCAExperiment & " - Change Experiment?", vbYesNo) = MsgBoxResult.Yes Then

                MsgBox("Please select experiment from the displayed list")

                Me.Cursor = Cursors.WaitCursor

                ListBox1.Items.Clear()

                'We load the AvailableExperimentNames array on initialization using the myINCAInterface.GetAvailableExperimentNames
                'routine.  The available experiment names are those which reside in the All  Programs\CLEVIR Setup\Experiments folder
                'in the INCA database.  We may want to change this to include all experiments, or perhaps use a different directory
                'Look in the GM_INCA_Comm code in the GetAvailableExperimentNames function for more info on this....

                If Len(AvailableExperimentNames(x)) > 0 Then

                    'add all names to a list box
                    ListBox1.Items.AddRange(AvailableExperimentNames)
                End If

                'Here we pop up a list box and wait until a user response is made...
                'This may need to be changed, because if the user navigates away from this screen
                'prior to making a selection, it can cause problems...
                ListBox1.Visible = True

                Me.Cursor = Cursors.Arrow

                Do While Len(ListBox1.SelectedItem) = 0
                    System.Windows.Forms.Application.DoEvents()
                    System.Threading.Thread.Sleep(100)
                Loop

                If UCase(ListBox1.SelectedItem) <> "INVALID" Then

                    TempExpName = ListBox1.SelectedItem

                    If Len(TempExpName) > 0 Then
                        INCAExperiment = TempExpName
                        MsgBox("Experiment " & INCAExperiment & " Will be used.")

                        fnum = FreeFile()
                        FileOpen(fnum, My.Application.Info.DirectoryPath & "\" & "INCAExperimentNames.txt", OpenMode.Output)

                        'This needs testing!!!!!!

                        For x = 0 To UBound(textline)

                            If name = Mid(textline(x), 1, InStr(textline(x), "=") - 1) Then
                                PrintLine(fnum, name & "=" & INCAExperiment)
                            Else
                                PrintLine(fnum, textline(x))
                            End If

                        Next

                        FileClose(fnum)

                    Else
                        MsgBox("Invalid Experiment Name Entered " & INCAExperiment & " Will be used.")
                    End If

                    ListBox1.Visible = False

                Else
                    MsgBox("Invalid Experiment Name Entered " & INCAExperiment & " Will be used.")
                End If
            End If

        End If

    End Sub

    Public Sub ShutdownAndRestartINCA(ByVal sleeptime As Integer)

        'User selection from the Actions dropdown menu, allows the user to shutdown and restart INCA
        'Can also be called in response to a communication loss between the app and INCA.  Also called
        'after we perform a FULL signal registration.

        Dim ForceInit As Boolean
        Dim ErrorMsg As String

        Dim e As System.EventArgs

        e = System.EventArgs.Empty

        Me.Cursor = Cursors.WaitCursor

        'We cant shut down until we transition out of Measure Mode

        If myINCAInterface.GetMeasurementStatus() = "True" Then
            myINCAInterface.StartStopMeasurement(OnVehicleScreen.Button6)
        End If

        WriteToListBox("Shutting Down INCA...")

        CheckForExperiment = False
        myINCAInterface.CloseINCA()

        RedisplayOnVehicleForm("GroupBox1")

        System.Threading.Thread.Sleep(sleeptime)

        ErrorMsg = ""

        'This flag is used to keep events from firing which are associated with
        'various controls having default text added during initialization
        Initializing = True
        WriteToListBox("Initializing...")

        OnVehicleScreen.Refresh()

        'We always set the ForceInit flag to false in the initial call to InitINCA in the GM_ResidentClient
        'We set this to false here, we do not want to force signal registration if we are already initialized 
        'and have our signals registered, it is faster for development.

        ForceInit = False

        'Here we initialize INCA based on information read in from the config.txt file
        If myINCAInterface.InitINCA(INCADatabase, INCAWorkspace, INCAExperiment, ForceInit, ErrorMsg, False) <> IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then

            RedisplayOnVehicleForm("GroupBox1")

            'Here is where we register the signals from our signal list with INCA using the RCI2 interface (see the GM_INCA_Comm project).

            myINCAInterface.RegisterSignals()

            WriteToListBox("Initialization Complete")

            OnVehicleScreen.Refresh()

        Else

            WriteToListBox("INCA Initialization returned - " & ErrorMsg)

            If Debugger.IsAttached = False Then

                If MsgBox("INCA Initialization returned - " & ErrorMsg & " Do you wish to continue?", vbYesNo) = vbNo Then
                    ExitApp()
                End If

            End If

            RedisplayOnVehicleForm("GroupBox1")

            'Here is where we register the signals from our signal list with INCA using the RCI2 interface (see the GM_INCA_Comm project).

            myINCAInterface.RegisterSignals()

            WriteToListBox("Initialization Complete")

            OnVehicleScreen.Refresh()

        End If

        System.Threading.Thread.Sleep(5000)

        Me.GroupBox1.Visible = False

        FormDisplayed = True
        Initializing = False

        If PlaybackMode = False Then
            CheckForExperiment = True
        End If

        Me.Cursor = Cursors.Arrow

        RedisplayOnVehicleForm("Main")

        OnVehicleScreen.TopMost = False

    End Sub

    Private Sub AddCustomINCASetup(ByVal name As String)

        'This is legacy, and not supported...

        'Called when the user selects Add Custom Inca Setup from the Select / Add Custom Inca Setup drop down
        'menu selection on the Actions drop down menu.

        'Here we are adding a new INCA user by copying the default INCA .ini directory into a directory
        'with a user specified name.  We also need the INCA experiment name which will be associated with
        'this custom inca setup...

        Dim fnum As Integer
        Dim ExpName As String
        Dim x As Integer
        Dim y As Integer
        Dim textline() As String
        Dim found As Boolean

        Dim ChangeExperiment As Boolean

        ExpName = ""

        'Need to check if this name already exists in the INCAExperimentNames.txt file here....
        'if so, do not add it again...

        fnum = FreeFile()
        FileOpen(fnum, My.Application.Info.DirectoryPath & "\" & "INCAExperimentNames.txt", OpenMode.Input)

        x = 0
        ReDim textline(0)

        Do While Not EOF(fnum)
            If x > 0 Then
                ReDim Preserve textline(x)
            End If
            textline(x) = LineInput(fnum)
            If name = Mid(textline(x), 1, InStr(textline(x), "=") - 1) Then
                ExpName = Mid(textline(x), InStr(textline(x), "=") + 1, Len(textline(x)))
                If MsgBox(name & " already exists.  Experiment name is " & ExpName & " Change Experiment?", vbYesNo) = MsgBoxResult.Yes Then

                    ChangeExperiment = True

                End If

                found = True
            End If
            x = x + 1
        Loop

        FileClose(fnum)

        If ChangeExperiment = True Or found = False Then

            Me.Cursor = Cursors.WaitCursor

            ListBox1.Items.Clear()

            If Len(AvailableExperimentNames(y)) > 0 Then
                ListBox1.Items.AddRange(AvailableExperimentNames)
            End If

            MsgBox("Please select an experiment from the list provided.")

            ListBox1.Visible = True

            Me.Cursor = Cursors.Arrow

            Do While Len(ListBox1.SelectedItem) = 0
                System.Windows.Forms.Application.DoEvents()
                System.Threading.Thread.Sleep(100)
            Loop

            ExpName = ListBox1.SelectedItem

            If found = False Then
                ReDim Preserve textline(x)
                textline(x) = name & "=" & ExpName

                DirectoryCopy(ETAS_USER_PATH & "\" & ETAS_DEFAULT_USER_NAME, ETAS_USER_PATH & "\" & name, False)

            End If

        End If

        If Len(ExpName) = 0 Then
            MsgBox("Invalid Experiment Name entered, Default Experiment Name " & INCAExperiment & " will be used.")
            ExpName = INCAExperiment
        End If

        fnum = FreeFile()
        FileOpen(fnum, My.Application.Info.DirectoryPath & "\" & "INCAExperimentNames.txt", OpenMode.Output)

        'This needs testing!!!!!!

        For x = 0 To UBound(textline)

            If InStr(textline(x), name) > 0 Then
                PrintLine(fnum, name & "=" & ExpName)
            Else
                PrintLine(fnum, textline(x))
            End If

        Next

        FileClose(fnum)

    End Sub

    Public Sub DeleteMF4Files(ByVal DirectoryName As String, Optional ByVal DeleteOnly As Boolean = False)

        'Called from UploadDataScreen.UploadData button press, ExitApp, StartStopMeasurement and StartStopRecord...

        'Checks to make sure that the zipped mf4 files are not corrupted, if not, deletes all of the MF4 files 
        'that have already been zipped.  If zip file is corrupted, will try to re-zip, then re-check before deleting 
        'corresponding.mf4 File.This Is called prior to upload and when transitioning out of Record mode.  

        'There used to be a user prompt asking whether Or Not they wanted to delete the MF4 files.  
        'We may need to create a version that has this for the Validation folks...

        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sf As Scripting.Folder
        Dim sfile As Scripting.File

        Me.Cursor = Cursors.WaitCursor

        Try

            If System.IO.Directory.Exists(DirectoryName) Then

                If DeleteOnly = False Then
                    UserStatusInfo.Label1.Text = "Zipping and Deleting Uncompressed .MF4 and .ASC Files. Please be patient..."
                End If

                FSO = New Scripting.FileSystemObject

                f = FSO.GetFolder(DirectoryName)

                For Each sfile In f.Files

                    If InStr(sfile.Name, ".mf4") > 0 Or InStr(sfile.Name, ".asc") > 0 Or InStr(sfile.Name, ".mdf") > 0 Or InStr(sfile.Name, ".vsb") > 0 Then

                        CheckZipfile(sfile.Path, DeleteOnly)

                    End If

                Next

                For Each sf In f.SubFolders

                    For Each sfile In sf.Files

                        If InStr(sfile.Name, ".mf4") > 0 Or InStr(sfile.Name, ".asc") > 0 Or InStr(sfile.Name, ".mdf") > 0 Or InStr(sfile.Name, ".vsb") > 0 Then ' was just mf4

                            CheckZipfile(sfile.Path, DeleteOnly)

                        End If

                    Next
                Next

                System.Threading.Thread.Sleep(1500)

                UserStatusInfo.Hide()

                FSO = Nothing
                f = Nothing
                sf = Nothing
                sfile = Nothing

            Else
                MsgBox(DirectoryName & " not found.  No recorded data available in this directory.")
                CopyToLog("DeleteMF4Files: " & DirectoryName & " not found.  No recorded data available in this directory.")
            End If

            Me.Cursor = Cursors.Arrow

        Catch ex As Exception
            CopyToLog("DeleteMF4Ffiles ERROR: " & ex.Message)
            UserStatusInfo.Hide()
            Me.Cursor = Cursors.Arrow
        End Try

    End Sub

    Public Sub DeleteUnusedDirectories(ByVal DirectoryName As String)

        'Called prior to uploading data.  Deletes any directory under the vehicle name directory path
        'that does not have any data in it.

        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sf As Scripting.Folder
        Dim sfile As Scripting.File

        Try

            If System.IO.Directory.Exists(DirectoryName) Then

                FSO = New Scripting.FileSystemObject

                f = FSO.GetFolder(DirectoryName)

                For Each sf In f.SubFolders

                    If sf.Files.Count = 0 Then
                        sf.Delete()
                    ElseIf sf.Files.Count <= 2 Then
                        For Each sfile In sf.Files
                            If InStr(sfile.Name, "_ANNO.csv") > 0 Then
                                sf.Delete()
                                Exit For
                            End If
                        Next
                    End If

                Next

            Else
                'MsgBox(DirectoryName & " not found.  No recorded data available for this vehicle.")
            End If


        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

    End Sub

    Public Function EnterVehicleNumber() As String

        'This function no longer used...

        'This routine is called on initialize depending on the mode in which we are running.  If we
        'are running on vehicle, this means that we will already have a vehicle number defined in the
        'vehicleconfig.txt file, so this routine will not be called. If however, we are not running
        'on the vehicle pc, we need to specifiy a vehicle number if we want to record data and upload
        'it, because data record locations and filenames must contain a vehicle number.

        Dim tmpVehicleNumber As String
        Dim reply As Integer

inputvehiclenumber:

        tmpVehicleNumber = InputBox("Please Enter a valid Vehicle ID (eg. 6hkn4424)", "USER INPUT REQIRED")

        If Len(tmpVehicleNumber) > 0 Then
            EnterVehicleNumber = tmpVehicleNumber
        ElseIf tmpVehicleNumber = "RES_ON_LAPTOP" Then

            EnterVehicleNumber = tmpVehicleNumber
        Else
            reply = MsgBox("Invalid Vehicle Number Entered.  Please enter a valid vehicle ID.", vbOKCancel)
            If reply = vbOK Then
                GoTo inputvehiclenumber
            Else
                EnterVehicleNumber = "0"
                MsgBox("Vehicle Number is currently 0.  This will impact the local file storage location as well as the " & NetworkDriveLetter & ":\ Drive file storage location.  The VehicleNumber should be changed to reflect the actual number of the vehicle in which this PC is installed.")
            End If

        End If

    End Function

    Public Sub CheckForCameras(ByVal InitialWaitTime As Integer, ByVal camerapingtime As Integer)

        'This routine is called during initialization.  It checks to see if the cameras specified
        'in the cameraipaddresses.txt return a valid response to a Ping.  Or, if there are no
        'camera addresses defined, it pings every address between 192.168.40.100 and 192.168.40.109
        'and looks for valid Ping responses.

        Dim myElapseTime As TimeSpan
        Dim mySaveTime As DateTime

        Dim AllGood As Boolean
        Dim ctr As Integer
        Dim ctr2 As Integer
        Dim cameraStatus() As Boolean
        Dim CameraPingCount As Integer

        Dim myPing As New Ping
        Dim myPingReply As PingReply = Nothing

        Dim ACameraWasNotFound As Boolean

        Dim WaitTime As Integer

        Dim CameraName As String = ""

        Try

            If InStr(SignalRegistrationMode, "FULL") > 0 Then
                InitialWaitTime = 1
                camerapingtime = 1
            End If

            If NumberOfCameras > 0 Then

                ReadCameraIPAddressesFile()

                WriteToListBox("Checking Camera Availability...")
                CopyToLog("CheckForCameras: Checking Camera Availability - Number of Cameras = " & NumberOfCameras & "...")
                OnVehicleScreen.Refresh()

                ctr = 0
                AllGood = False
                ReDim cameraStatus(UBound(CameraIPAddresses))
                ReDim ActualCameraIPAddresses(NumberOfCameras - 1)

                mySaveTime = Now
                myElapseTime = Now.Subtract(mySaveTime)

                Do While Not AllGood And (myElapseTime.Seconds < camerapingtime)

                    'System.Windows.Forms.Application.DoEvents()

                    'If CancelCameraSearch = True Then
                    'WriteToListBox("Camera Search Cancelled by user...")
                    'OnVehicleScreen.Refresh()
                    'System.Threading.Thread.Sleep(1000)
                    'Exit Do
                    'End If

                    WriteToListBox("Pinging Camera at " & CameraIPAddresses(ctr))
                    CopyToLog("CheckForCameras: Pinging Camera at " & CameraIPAddresses(ctr))
                    OnVehicleScreen.Refresh()

                    Try

                        myPingReply = myPing.Send(CameraIPAddresses(ctr), 100)

                        If myPingReply.Status = IPStatus.Success Then
                            cameraStatus(ctr) = True
                            WriteToListBox("Found Camera at " & CameraIPAddresses(ctr))
                            CopyToLog("CheckForCameras: Found Camera at " & CameraIPAddresses(ctr))

                            OnVehicleScreen.Refresh()
                            CameraPingCount = 0
                            For ctr2 = 0 To UBound(CameraIPAddresses)
                                If cameraStatus(ctr2) = True Then
                                    ActualCameraIPAddresses(CameraPingCount) = CameraIPAddresses(ctr2)
                                    CameraPingCount = CameraPingCount + 1
                                End If
                            Next
                            If CameraPingCount = NumberOfCameras Then
                                AllGood = True
                                myElapseTime = Now.Subtract(mySaveTime)
                                Exit Do
                            End If
                        Else

                            WriteToListBox("NO Camera Found at " & CameraIPAddresses(ctr) & " - " & myPingReply.Status.ToString)
                            CopyToLog("CheckForCameras: NO Camera Found at " & CameraIPAddresses(ctr) & " - " & myPingReply.Status.ToString)

                            OnVehicleScreen.Refresh()

                            If ACameraWasNotFound = False Then

                                ACameraWasNotFound = True

                                WaitTime = InitialWaitTime - ((InitForm.HowLongHaveIBeenUp.Minutes * 60) + InitForm.HowLongHaveIBeenUp.Seconds)

                                If WaitTime > 0 Then

                                    WriteToListBox("Waiting " & WaitTime & " seconds for Cameras to initialize...")
                                    CopyToLog("CheckForCameras: Waiting " & WaitTime & " seconds for Cameras to initialize...")
                                    OnVehicleScreen.Refresh()

                                    System.Threading.Thread.Sleep(WaitTime * 1000)

                                End If

                            End If

                        End If

                    Catch ex As Exception

                        WriteToListBox("NO Camera Found at " & CameraIPAddresses(ctr) & " - " & ex.Message)
                        CopyToLog("CheckForCameras: NO Camera Found at " & CameraIPAddresses(ctr) & " - " & ex.Message)

                        OnVehicleScreen.Refresh()

                        If ACameraWasNotFound = False Then

                            ACameraWasNotFound = True

                            WaitTime = InitialWaitTime - ((InitForm.HowLongHaveIBeenUp.Minutes * 60) + InitForm.HowLongHaveIBeenUp.Seconds)

                            If WaitTime > 0 Then

                                WriteToListBox("Waiting " & WaitTime & " seconds for Cameras to initialize...")
                                CopyToLog("CheckForCameras: Waiting " & WaitTime & " seconds for Cameras to initialize...")
                                OnVehicleScreen.Refresh()

                                System.Threading.Thread.Sleep(WaitTime * 1000)

                            End If

                        End If

                    End Try

                    If ctr = UBound(CameraIPAddresses) Then
                        CameraPingCount = 0
                        For ctr = 0 To UBound(cameraStatus)
                            If cameraStatus(ctr) = True Then
                                ActualCameraIPAddresses(CameraPingCount) = CameraIPAddresses(ctr)
                                CameraPingCount = CameraPingCount + 1
                            End If
                        Next
                        If CameraPingCount = NumberOfCameras Then
                            AllGood = True
                            myElapseTime = Now.Subtract(mySaveTime)
                            Exit Do
                        End If
                        ctr = 0
                    Else
                        ctr = ctr + 1
                    End If

                    myElapseTime = Now.Subtract(mySaveTime)

                Loop

                If AllGood = False Then
                    If CameraPingCount > 0 Then
                        WriteToListBox("NOT All Cameras are Available:")
                        CopyToLog("CheckForCameras: NOT All Cameras are Available:")
                        OnVehicleScreen.Refresh()
                        For ctr = 0 To UBound(cameraStatus)
                            If cameraStatus(ctr) = True Then
                                WriteToListBox("Camera found at " & CameraIPAddresses(ctr))
                                CopyToLog("CheckForCameras: Camera found at " & CameraIPAddresses(ctr))
                                OnVehicleScreen.Refresh()
                            Else
                                CopyToLog("CheckForCameras: NO Camera found at " & CameraIPAddresses(ctr))

                                MsgBox("IP Address " & CameraIPAddresses(ctr) & " is unreachable.  Please check Ethernet Connection.")

                            End If
                        Next ctr
                        WriteToListBox("Elapsed time for Camera Init: " & (myElapseTime.Minutes * 60) + myElapseTime.Seconds & "." & myElapseTime.Milliseconds & " sec")
                        OnVehicleScreen.Refresh()
                    Else
                        WriteToListBox("NO Cameras found. Elapsed time for Init: " & (myElapseTime.Minutes * 60) + myElapseTime.Seconds & "." & myElapseTime.Milliseconds & " sec")
                        OnVehicleScreen.Refresh()
                        System.Threading.Thread.Sleep(2000)
                    End If
                Else

                    CopyToLog("CheckForCameras: All Cameras Available...")

                    WriteToListBox(CStr(NumberOfCameras) & " Camera(s) Available: Elapsed time for Init: " & (myElapseTime.Minutes * 60) + myElapseTime.Seconds & "." & myElapseTime.Milliseconds & " sec")
                    OnVehicleScreen.Refresh()

                    WriteCameraIPAddressesFile()
                End If

            End If

        Catch ex As Exception
            WriteToListBox("CheckForCameras: " & ex.Message)
            CopyToLog("CheckForCameras: " & ex.Message)

            'Finally
            '    CancelCameraSearch = False
        End Try

    End Sub

    Private Sub HandleButtonVisibility(ByVal Visible As Boolean)
        OnVehicleScreen.Button2.Visible = Visible
        OnVehicleScreen.Button3.Visible = Visible
        OnVehicleScreen.Button4.Visible = Visible
        OnVehicleScreen.Button5.Visible = Visible
        OnVehicleScreen.Button6.Visible = Visible
        OnVehicleScreen.Button7.Visible = Visible
        OnVehicleScreen.Button14.Visible = Visible
        OnVehicleScreen.Button23.Visible = Visible
    End Sub


    Sub UpdateINCAWithLatestExperiment(ByVal ExperimentName As String, ByVal CSVFileInFolder As Boolean)

        'Called from CheckForNewerSignalListNEW and CheckForNewerSignalListOLD if an updated experiment is found: 

        'Imports ExperimentName into INCA
        'Asks user if they want to update the current configuration file with the newly imported experiment name
        'If yes, will also update the INCAVariableFile to the corresponding signal list name (.csv) is default
        'if both .csv and .xlsx versions of this file exist.

        Dim ErrorMsg As String = ""
        Dim success As Boolean
        Dim Filename As String = ""
        Dim fnum As Integer
        Dim fnum2 As Integer
        Dim textline As String
        Dim savetextline As String
        Dim ctr As Integer

        Dim FilenameToChange As String

        CopyToLog("UpdateINCAWithLatestExperiment...")

        Me.TopMost = True

        If Not InitForm.myThread Is Nothing Then
            TerminateInitThread = True
            InitForm.myThread = Nothing
        End If

        If UCase(SaveLoginID) = "DEMO" Or Len(SaveLoginID) = 0 Then
            FilenameToChange = "config.txt"
        Else
            FilenameToChange = SaveLoginID & ".txt"
        End If

        If myINCAInterface.InitINCA(INCADatabase, INCAWorkspace, "", True, ErrorMsg, False) <> IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then

            success = ImportFileIntoINCA(ExperimentName, True, False)

            If success = True Then
                OnVehicleScreen.TopMost = True
                CopyToLog("UpdateINCAWithLatestExperiment: Updated Experiment " & System.IO.Path.GetFileNameWithoutExtension(ExperimentName) & " imported into INCA.  Do you want to update the " & FilenameToChange & " configuration file? Question Asked...")
                If MsgBox("Updated Experiment " & System.IO.Path.GetFileNameWithoutExtension(ExperimentName) & " imported into INCA.  Do you want to update the " & FilenameToChange & " configuration file?", vbYesNo) = vbYes Then
                    CopyToLog("UpdateINCAWithLatestExperiment: User Responded Yes...")
                    fnum = FreeFile()
                    ctr = 0

                    FileOpen(fnum, My.Application.Info.DirectoryPath & "\" & FilenameToChange, OpenMode.Input)

                    fnum2 = FreeFile()
                    FileOpen(fnum2, My.Application.Info.DirectoryPath & "\tempfile.txt", OpenMode.Output)

                    Do While Not EOF(fnum)

                        textline = LineInput(fnum)

                        If ctr = 2 Then
                            savetextline = "INCAExperiment" & Chr(9) & System.IO.Path.GetFileNameWithoutExtension(ExperimentName)
                            INCAExperiment = System.IO.Path.GetFileNameWithoutExtension(ExperimentName)
                            InitialINCAExperiment = INCAExperiment
                        ElseIf ctr = 3 Then

                            If CSVFileInFolder = False Then
                                savetextline = "INCAVariableFile" & Chr(9) & My.Application.Info.DirectoryPath & "\SignalLists\" & Mid(System.IO.Path.GetFileName(ExperimentName), 1, InStr(System.IO.Path.GetFileName(ExperimentName), ".") - 1) & ".xlsx"
                                INCAVariableFile = My.Application.Info.DirectoryPath & "\SignalLists\" & Mid(System.IO.Path.GetFileName(ExperimentName), 1, InStr(System.IO.Path.GetFileName(ExperimentName), ".") - 1) & ".xlsx"
                            Else
                                savetextline = "INCAVariableFile" & Chr(9) & My.Application.Info.DirectoryPath & "\SignalLists\" & Mid(System.IO.Path.GetFileName(ExperimentName), 1, InStr(System.IO.Path.GetFileName(ExperimentName), ".") - 1) & ".csv"
                                INCAVariableFile = My.Application.Info.DirectoryPath & "\SignalLists\" & Mid(System.IO.Path.GetFileName(ExperimentName), 1, InStr(System.IO.Path.GetFileName(ExperimentName), ".") - 1) & ".csv"
                            End If

                        Else
                            savetextline = textline
                        End If
                        ctr = ctr + 1

                        PrintLine(fnum2, savetextline)

                    Loop

                    FileClose(fnum)
                    FileClose(fnum2)

                    FileCopy(My.Application.Info.DirectoryPath & "\tempfile.txt", My.Application.Info.DirectoryPath & "\" & FilenameToChange)
                    System.IO.File.Delete(My.Application.Info.DirectoryPath & "\tempfile.txt")

                    CopyToLog("UpdateINCAWithLatestExperiment: Config File " & FilenameToChange & " has been updated.")
                Else
                    INCAExperiment = InitialINCAExperiment
                    CopyToLog("UpdateINCAWithLatestExperiment: User Responded No: INCAExperiment = " & INCAExperiment)
                End If

                OnVehicleScreen.TopMost = False

            Else 'ImportFileIntoINCA returned false...

                CopyToLog("UpdateINCAWithLatestExperiment: Import Experiment " & ExperimentName & " into INCA Failed...")
                MsgBox("Import Experiment " & ExperimentName & " into INCA Failed...")
                Me.TopMost = False
                Exit Sub

            End If

        Else

            If InStr(ErrorMsg, "Experiment") > 0 Then
                MsgBox("Could Not initialize INCA - Please Check config.txt file For correct Experiment name. Exiting...")
            ElseIf InStr(ErrorMsg, "Workspace") > 0 Then
                MsgBox("Could Not initialize INCA - Please Check config.txt file For correct Workspace name. Exiting...")
            ElseIf InStr(ErrorMsg, "Database") > 0 Then
                MsgBox("Could Not initialize INCA - Please Check config.txt file For correct INCA Database name. Exiting...")
            Else
                MsgBox("Could Not initialize INCA - Please Check config.txt file For correct INCA Database name, Workspace name And Experiment name. Exiting...")
            End If

            ExitApp("Complete")

        End If

        Me.TopMost = False

    End Sub

    Sub CheckForNewerSignalListOLD(Optional ByVal SelectOption As Boolean = False)

        'Called from VerifyCLEVIRConfiguration which is called from HandleLogin.  Also called from Save and Continue button on
        'the softwareversionselect form.

        'There are two routines currently that do similar things, CheckForNewerSignalListOLD and CheckForNewerSignalListNEW. 
        'CheckForNewerSignalListOLD is called if CLEVIR is unable to determine both model year and software version from the
        'workspace filename...

        'Checks to see if there is a more up to date Signal List .xlsx file on the Q drive (for the latest major rev).
        'if it finds a Signal List .xlsx file with a newer date, it copies it into the GM_ResidentClient\SignalLists directory.
        'Also looks for the associated INCA experiment (.exp file) and imports it into INCA. Also updates the current active
        'CLEVIR configuration file with the newest signal list name and experiment name.

        Dim dirname As String
        Dim LatestVersionOnQDrive As Integer

        Dim LatestVersionOnPC As Integer

        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sf As Scripting.Folder
        Dim sfile As Scripting.File

        Dim FSO2 As Scripting.FileSystemObject
        Dim f2 As Scripting.Folder
        Dim sfile2 As Scripting.File

        Dim FileFound As Boolean = False
        Dim VersionFound As Boolean = False

        Dim Found As Boolean = False

        Dim SaveLatestVersion As Integer

        Dim SaveFileName As String = ""

        Dim LatestWriteTimeOnPC As Date

        Dim CSVFileInFolder As Boolean

        Dim answer As MsgBoxResult = vbNo

        Try

            CopyToLog("CheckForNewerSignalListOLD Called.   CheckForNewerSignalListComplete = " & CheckForNewerSignalListComplete)

            If CheckForNewerSignalListComplete = True Then
                Exit Sub
            End If

            If Debugger.IsAttached = True Then
                If MsgBox("Check For newer signal list And experiment?", vbYesNo) = vbNo Then
                    Exit Sub
                End If
            End If

            'Get most recent file write date and time from SignalLists directory on PC...
            If System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\SignalLists") Then
                FSO2 = New Scripting.FileSystemObject
                f2 = FSO2.GetFolder(My.Application.Info.DirectoryPath & "\SignalLists")

                For Each sfile2 In f2.Files

                    If InStr(sfile2.Name, "SAVE") = 0 And InStr(sfile2.Name, "~") = 0 Then
                        If System.IO.File.GetLastWriteTime(sfile2.Path) > LatestWriteTimeOnPC Then
                            LatestWriteTimeOnPC = System.IO.File.GetLastWriteTime(sfile2.Path)
                        End If
                    End If

                Next
            Else
                System.IO.Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\SignalLists")
            End If

            'NETWORK DRIVE MAPPING

            dirname = NetworkDriveLetter & "\CSAV2 Tools\CLEVIR\Updated CLEVIR Files For Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath

            If System.IO.Directory.Exists(dirname) Then

                FSO = New Scripting.FileSystemObject

                f = FSO.GetFolder(dirname)

                'Determine latest version folder on Q drive based on CLEVIRFilesPath...
                For Each sf In f.SubFolders

                    If Val(Mid(sf.Name, 1, 3)) > LatestVersionOnQDrive Then
                        LatestVersionOnQDrive = Val(Mid(sf.Name, 1, 3))
                    End If

                Next

                'Determine file with the highest version number in SignalLists directory...
                If System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\SignalLists") Then
                    FSO2 = New Scripting.FileSystemObject
                    f2 = FSO2.GetFolder(My.Application.Info.DirectoryPath & "\SignalLists")

                    For Each sfile2 In f2.Files

                        If Val(Mid(sfile2.Name, 1, 3)) > LatestVersionOnPC Then
                            LatestVersionOnPC = Val(Mid(sfile2.Name, 1, 3))
                        End If

                    Next

                End If

                If LatestVersionOnPC = 0 Then
                    LatestVersionOnPC = 145
                End If

                Do While LatestVersionOnPC <= LatestVersionOnQDrive

                    If LatestVersionOnPC < LatestVersionOnQDrive Then

                        SaveLatestVersion = LatestVersionOnQDrive

                    Else
                        SaveLatestVersion = 0
                    End If

                    WriteToListBox("Checking " & NetworkDriveLetter & " Drive For Updated Experiment And Signal List For Software Version " & LatestVersionOnPC & "...")

                    OnVehicleScreen.Refresh()

                    For Each sf In f.SubFolders

                        'here we will be in a Q drive subfolder which matches the LatestVersion on the PC

                        If Val(Mid(sf.Name, 1, 3)) = LatestVersionOnPC Then 'we are looking in the LatestVersion versionnumber folder

                            CSVFileInFolder = False

                            For Each sfile In sf.Files

                                If (InStr(sfile.Name, ".xlsx") > 0 Or InStr(sfile.Name, ".csv") > 0) And InStr(sfile.Name, "~") = 0 Then

                                    If System.IO.File.GetLastWriteTime(sfile.Path) > LatestWriteTimeOnPC Then

                                        If InStr(sfile.Name, ".csv") > 0 Then
                                            CSVFileInFolder = True
                                        End If

                                        WriteToListBox("New Signal List Found For version " & Mid(sfile.Name, 1, 3) & ", Copying...")
                                        CopyToLog("CheckForNewerSignalListOLD: Copying New Signal List " & sfile.Path & " to " & My.Application.Info.DirectoryPath & "\SignalLists\" & sfile.Name)
                                        OnVehicleScreen.Refresh()
                                        FileCopy(sfile.Path, My.Application.Info.DirectoryPath & "\SignalLists\" & sfile.Name)
                                    End If

                                End If

                            Next

                            For Each sfile In sf.Files

                                If InStr(sfile.Name, ".exp") > 0 Then

                                    If System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\Experiments") Then
                                        FSO2 = New Scripting.FileSystemObject
                                        f2 = FSO2.GetFolder(My.Application.Info.DirectoryPath & "\Experiments")

                                        For Each sfile2 In f2.Files
                                            If sfile2.Name = sfile.Name Then
                                                If System.IO.File.GetLastWriteTime(sfile.Path) = System.IO.File.GetLastWriteTime(sfile2.Path) Then
                                                    FileFound = True
                                                    Exit For
                                                End If

                                            End If
                                        Next

                                        'We will copy the experiment to the PC if the file write time of the experiment file is not equal, or if the
                                        'experiment file is not found on the PC.

                                        If FileFound = False Then

                                            WriteToListBox("Copying Experiment to " & My.Application.Info.DirectoryPath & "\Experiments Directory...")

                                            OnVehicleScreen.Refresh()

                                            FileCopy(sfile.Path, f2.Path & "\" & sfile.Name)

                                            Found = True
                                            WriteToListBox("Importing Experiment into INCA...")

                                            OnVehicleScreen.Refresh()

                                            UpdateINCAWithLatestExperiment(f2.Path & "\" & sfile.Name, CSVFileInFolder)

                                        Else
                                            FileFound = False
                                        End If

                                    End If

                                End If

                            Next

                        End If

                    Next

                    If SaveLatestVersion > 0 Then
                        LatestVersionOnPC = LatestVersionOnPC + 1
                        answer = vbNo
                        Do While answer = vbNo And LatestVersionOnPC <= LatestVersionOnQDrive
                            answer = MsgBox("Update files to software version " & LatestVersionOnPC & "?", vbYesNo)
                            If answer = vbNo Then
                                LatestVersionOnPC = LatestVersionOnPC + 1
                            End If
                        Loop

                    Else
                        Exit Do
                    End If

                Loop

            End If

            If Found = False Then

                CopyToLog("CheckForNewerSignalListOLD: CheckForNewerSignalListOLD: No Updates Found...")
                WriteToListBox("No Updates Found...")

                OnVehicleScreen.Refresh()

            End If

            CheckForNewerSignalListComplete = True

        Catch ex As Exception

            CopyToLog("CheckForNewerSignalListOLD " & ex.Message)

        End Try

    End Sub

    Sub CheckForNewerSignalListNEW(Optional ByVal SelectOption As Boolean = False)

        'Called from VerifyCLEVIRConfiguration which is called from HandleLogin.  Also called from Save and Continue button on
        'the softwareversionselect form.

        'There are two routines currently that do similar things, CheckForNewerSignalListOLD and CheckForNewerSignalListNEW. 
        'CheckForNewerSignalListNEW is called if CLEVIR IS able to determine both model year and software version from the
        'workspace filename...

        'Checks to see if there is a more up to date Signal List .xlsx file on the Q drive (for the latest major rev).
        'if it finds a Signal List .xlsx file with a newer date, it copies it into the GM_ResidentClient\SignalLists directory.
        'Also looks for the associated INCA experiment (.exp file) and imports it into INCA. Also updates the current active
        'CLEVIR configuration file with the newest signal list name and experiment name.

        Dim dirname As String
        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sf As Scripting.Folder
        Dim sfile As Scripting.File
        Dim FSO2 As Scripting.FileSystemObject
        Dim f2 As Scripting.Folder
        Dim sfile2 As Scripting.File
        Dim FileFound As Boolean = False
        Dim VersionFound As Boolean = False
        Dim Found As Boolean = False
        Dim SaveFileName As String = ""
        Dim LatestWriteTimeOnPC As Date
        Dim CSVFileInFolder As Boolean
        Dim answer As MsgBoxResult = vbNo

        Try

            CopyToLog("CheckForNewerSignalListNEW Called.   CheckForNewerSignalListComplete = " & CheckForNewerSignalListComplete)

            If CheckForNewerSignalListComplete = True Then
                Exit Sub
            End If

            If Debugger.IsAttached = True Then
                If CheckForNewerSignalListComplete = False Then
                    If MsgBox("Check for newer signal list And experiment?", vbYesNo) = vbNo Then
                        Exit Sub
                    End If
                Else
                    Exit Sub
                End If
            End If

            UserStatusInfo.Label1.Text = "Checking Share Drive for Updated Signal List And Experiment..."

            'Get most recent file write date and time from SignalLists directory on PC...
            If System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\SignalLists") Then
                FSO2 = New Scripting.FileSystemObject
                f2 = FSO2.GetFolder(My.Application.Info.DirectoryPath & "\SignalLists")

                For Each sfile2 In f2.Files

                    If (InStr(sfile2.Name, "SAVE") = 0 And InStr(sfile2.Name, "~") = 0) And
                        (((g_ModelYear <> "??" And InStr(sfile2.Name, "MY" & g_ModelYear) > 0) Or g_ModelYear = "??") And
                        ((g_SoftwareVersion <> "???" And Mid(sfile2.Name, 1, 3) = g_SoftwareVersion) Or g_SoftwareVersion = "???")) Then
                        If System.IO.File.GetLastWriteTime(sfile2.Path) > LatestWriteTimeOnPC Then
                            LatestWriteTimeOnPC = System.IO.File.GetLastWriteTime(sfile2.Path)
                        End If
                    End If

                Next
            Else
                System.IO.Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\SignalLists")
            End If

            'NETWORK DRIVE MAPPING

            dirname = NetworkDriveLetter & "\CSAV2 Tools\CLEVIR\Updated CLEVIR Files for Vehicles\Signal Files And Experiments\" & CLEVIRFilesPath

            If System.IO.Directory.Exists(dirname) Then

                FSO = New Scripting.FileSystemObject

                f = FSO.GetFolder(dirname)

                For Each sf In f.SubFolders

                    If Val(Mid(sf.Name, 1, 3)) = g_SoftwareVersion Then

                        CSVFileInFolder = False

                        For Each sfile In sf.Files

                            If (InStr(sfile.Name, ".xlsx") > 0 Or InStr(sfile.Name, ".csv") > 0) And (InStr(sfile.Name, "~") = 0) And
                               (((g_ModelYear <> "??" And InStr(sfile.Name, "MY" & g_ModelYear) > 0) Or g_ModelYear = "??") And
                               ((g_SoftwareVersion <> "???" And Mid(sfile.Name, 1, 3) = g_SoftwareVersion) Or g_SoftwareVersion = "???")) Then

                                If System.IO.File.GetLastWriteTime(sfile.Path) > LatestWriteTimeOnPC Then

                                    If InStr(sfile.Name, ".csv") > 0 Then
                                        CSVFileInFolder = True
                                    End If

                                    WriteToListBox("New Signal List Found for version " & Mid(sfile.Name, 1, 3) & ", Copying...")
                                    CopyToLog("CheckForNewerSignalListNEW: Copying New Signal List " & sfile.Path & " to " & My.Application.Info.DirectoryPath & "\SignalLists\" & sfile.Name)
                                    OnVehicleScreen.Refresh()
                                    FileCopy(sfile.Path, My.Application.Info.DirectoryPath & "\SignalLists\" & sfile.Name)
                                    CopyToLog("CheckForNewerSignalListNEW: Copy Complete.")
                                End If

                            End If

                        Next 'file in folder on share drive...

                        For Each sfile In sf.Files

                            If (InStr(sfile.Name, ".exp") > 0) And
                               (((g_ModelYear <> "??" And InStr(sfile.Name, "MY" & g_ModelYear) > 0) Or g_ModelYear = "??") And
                               ((g_SoftwareVersion <> "???" And Mid(sfile.Name, 1, 3) = g_SoftwareVersion) Or g_SoftwareVersion = "???")) Then

                                If System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\Experiments") Then
                                    FSO2 = New Scripting.FileSystemObject
                                    f2 = FSO2.GetFolder(My.Application.Info.DirectoryPath & "\Experiments")

                                    For Each sfile2 In f2.Files
                                        If sfile2.Name = sfile.Name Then
                                            If System.IO.File.GetLastWriteTime(sfile.Path) = System.IO.File.GetLastWriteTime(sfile2.Path) Then
                                                FileFound = True
                                                Exit For
                                            End If

                                        End If
                                    Next

                                    'We will copy the experiment to the PC if the file write time of the experiment file is not equal, or if the
                                    'experiment file is not found on the PC.

                                    If FileFound = False Then

                                        WriteToListBox("Copying Experiment to " & My.Application.Info.DirectoryPath & "\Experiments Directory...")
                                        CopyToLog("CheckForNewerSignalListNEW: Copying Experiment to " & My.Application.Info.DirectoryPath & "\Experiments Directory...")

                                        OnVehicleScreen.Refresh()

                                        FileCopy(sfile.Path, f2.Path & "\" & sfile.Name)

                                        Found = True
                                        WriteToListBox("Experiment Copy Complete.")
                                        CopyToLog("CheckForNewerSignalListNEW: Experiment Copy Complete.")

                                        OnVehicleScreen.Refresh()

                                        UpdateINCAWithLatestExperiment(f2.Path & "\" & sfile.Name, CSVFileInFolder)

                                    Else
                                        FileFound = False
                                    End If

                                End If

                            End If

                        Next 'file in folder on share drive...
                    End If

                Next

            End If

            If Found = False Then

                CopyToLog("CheckForNewerSignalListNEW:  No Updates Found...")
                WriteToListBox("No Updates Found...")

                OnVehicleScreen.Refresh()

            End If

            CheckForNewerSignalListComplete = True

        Catch ex As Exception

            CopyToLog("CheckForNewerSignalListNEW " & ex.Message)
            MsgBox("Checking for Newer Signal List And Experiment - Error: " & ex.Message)

        End Try

    End Sub

    Public Sub UpdateFromSavedCustomAnnotations()

        'Not currently used, takes too long over the network...

        'Original intent was to have CLEVIR update custom annotation files that are saved to the q drive from the various
        'vehicles such that each vehicle would have the same custom annotations.  This can be done manually on a periodic
        'basis.

        Dim textline As String = ""
        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sf As Scripting.Folder
        Dim fnum2 As Integer

        Dim x As Integer
        Dim y As Integer
        Dim n As Integer

        Dim errorstring As String

        Dim Found As Boolean

        Try

            If Debugger.IsAttached = False Then

                FSO = New Scripting.FileSystemObject

                f = FSO.GetFolder(NetworkDriveLetter & "\CSAV2 Tools\CLEVIR\Development\CLEVIR Vehicle Configurations")

                n = -1

                For Each sf In f.SubFolders

                    For Each sfile In sf.Files

                        Found = False

                        If InStr(sfile.Name, "SavedCustomAnnotations.txt") > 0 Then

                            If Not mySavedCustomAnnotations Is Nothing Then

                                For x = 0 To UBound(mySavedCustomAnnotations)
                                    If mySavedCustomAnnotations(x).Filename = My.Application.Info.DirectoryPath & "\" & sfile.Name Then
                                        Found = True
                                        Exit For
                                    End If
                                Next x

                            End If

                            If Found = True Then

                                fnum2 = FreeFile()
                                FileOpen(fnum2, sfile.Path, OpenMode.Input)

                                Do While Not EOF(fnum2)
                                    textline = LineInput(fnum2)

                                    For y = 0 To UBound(mySavedCustomAnnotations(x).CustomAnnotations)
                                        If textline = mySavedCustomAnnotations(x).CustomAnnotations(y) Then
                                            Found = True
                                            Exit For
                                        End If
                                    Next y

                                    If Found = False Then
                                        ReDim Preserve mySavedCustomAnnotations(x).CustomAnnotations(UBound(mySavedCustomAnnotations(x).CustomAnnotations) + 1)
                                        mySavedCustomAnnotations(x).CustomAnnotations(UBound(mySavedCustomAnnotations(x).CustomAnnotations)) = textline
                                    Else
                                        Found = False
                                    End If

                                    'System.Windows.Forms.Application.DoEvents()
                                Loop

                                FileClose(fnum2)

                            Else

                                n = n + 1

                                ReDim Preserve mySavedCustomAnnotations(n)

                                mySavedCustomAnnotations(n).Filename = My.Application.Info.DirectoryPath & "\" & sfile.Name

                                fnum2 = FreeFile()
                                FileOpen(fnum2, sfile.Path, OpenMode.Input)

                                Do While Not EOF(fnum2)
                                    textline = LineInput(fnum2)
                                    If mySavedCustomAnnotations(n).CustomAnnotations Is Nothing Then
                                        ReDim Preserve mySavedCustomAnnotations(n).CustomAnnotations(0)
                                    Else
                                        ReDim Preserve mySavedCustomAnnotations(n).CustomAnnotations(UBound(mySavedCustomAnnotations(n).CustomAnnotations) + 1)
                                    End If
                                    mySavedCustomAnnotations(n).CustomAnnotations(UBound(mySavedCustomAnnotations(n).CustomAnnotations)) = textline
                                    'System.Windows.Forms.Application.DoEvents()
                                Loop

                                FileClose(fnum2)

                            End If
                        End If

                    Next sfile

                Next sf

                If Not mySavedCustomAnnotations Is Nothing Then

                    For x = 0 To UBound(mySavedCustomAnnotations)

                        fnum2 = FreeFile()
                        FileOpen(fnum2, mySavedCustomAnnotations(x).Filename, OpenMode.Output)

                        For y = 0 To UBound(mySavedCustomAnnotations(x).CustomAnnotations)
                            PrintLine(fnum2, mySavedCustomAnnotations(x).CustomAnnotations(y))
                        Next y

                        FileClose(fnum2)

                    Next x

                End If

            End If

        Catch ex As Exception

            errorstring = "UpdateFromSavedCustomAnnotations: " & ex.Message

            CopyToLog(ex.Message)
            MsgBox(ex.Message)

        End Try

    End Sub

    Public Sub Initialize()

        'This is called after InitForm loads GM_ResidentClient by calling GM_ResidentClient.show ....

        'This is the primary initialization routine that runs on startup.  This runs as a result of pressing the Drive button.  This routine
        'calls the HandleLogin routine which displays the login screen.

        Dim ForceInit As Boolean
        Dim ErrorMsg As String
        Dim ExitFlag As Boolean
        Dim ExitMode As String
        Dim ReturnVal As Integer = 0
        Dim MsgString As String = ""

        Try

            ReadDebugFile() 'Reads the debug.txt file to determine the MessageLogLevel for writing into the log file.  The higher the number, the
            'more verbose we are...  Currently set to 0 so we are "running pretty quiet"

            ReferenceDataSetDataBasePaths = Nothing
            WorkingDataSetDataBasePaths = Nothing

            Me.Cursor = Cursors.WaitCursor

            ErrorMsg = ""

            'The Initializing flag is used to keep events from firing which are associated with
            'various controls having default text added during initialization
            Initializing = True
            WriteToListBox("Initializing...")

            Me.Refresh()
            Me.Cursor = Cursors.Arrow

            'Sets initial display properties and displays main window and operation history window
            SetInitialDisplayProperties()

            'Playback mode is intended only for use when playing back "CLEVIR" play back files.  Playback mode allows
            'for a much faster initialization time because we are not starting INCA and bypassing a lot of other
            'checks that would be made if initializing normally...
            If PlaybackMode = False Then

                If OperatingMode = OperatingModes.RES_ON_VPC Or
                OperatingMode = OperatingModes.RES_ON_SUITCASE_VPC Then
                    CheckForCameras(0, 3)
                End If

                'Reads the list of users which is ordered by frequency of use.   This information is copied to a list box
                'on the login form.
                ReadUserIDList()

                'Here we check for the existance of either CANalyzer or VSpy configuration files and determine if there is an alternate
                'recording capability installed on the PC.  This will influence what is displayed on the login screen...
                DetermineAlternateRecordMode()

                If UCase(CLEVIR_Flavor) = "DEVELOPMENT" And FlashingStatus.Visible = False Then

                    'Here we will check if there is an updated signallist and experiment available for CLEVIR to use.  SignalList and experiment is
                    'specific to which program (i.e. CSAV2, LowContent, CoPilot, etc.) We know which program based on vehicle number, which was obtained
                    'during initial start up, prior to the user pressing the Drive button...

                    If INCALaunched = False Then

                        WriteToListBox("Connecting to INCA...")
                        OnVehicleScreen.Refresh()

                        Dim returnstr As String

                        returnstr = myINCAInterface.ConnectToInca()
                        If returnstr <> "True" Then
                            MsgBox(returnstr)
                            ExitApp()
                        Else
                            INCALaunched = True
                        End If

                        'Here we get all of the experiment names in the current INCA project and store them in a global
                        'array (AvailableExperimentNames) for future use...
                        WriteToListBox("Getting Available Experiment Names...")
                        'Wondering if we can hide this somewhere to decrease init time...
                        AvailableExperimentNames = myINCAInterface.GetAvailableExperimentNames
                        WriteToListBox("Available Experiment Names Retrieved")

                    End If

                    'LoginForm.CheckBox1 is displayed on the login screen.  The Enabled flag is set when reading in the config.txt file.
                    LoginForm.CheckBox1.Checked = SaveCalSnapshotEnabled

                    'This will display the login screen...
                    'Initialize will pause here at this call until the user does something to exit the login screen...

                    HandleLogin()

                Else 'Not DEVELOPMENT Mode...
                    ParseA2lFile(My.Application.Info.DirectoryPath & "\Enumerations.txt")
                    SaveLoginID = "Demo"
                End If

                'We only launch CANalyzer or Vehicle Spy if AlternateRecordEnabled is true.  This flag is set by checking the appropriate box
                'on the login screen.  This is where we verify that CANalyzer or VSpy are available and working...

                If AlternateRecordEnabled = True Then
                    If AlternateRecordingMode <> "None" Then

                        WriteToListBox("Checking Alternate Record Method...")

                        If AlternateRecordingMode <> "VehicleSpy" Then
                            If CanalyzerStarted = False Then
                                LaunchCanalyzer()
                            End If

                        Else
                            If VehicleSpyStarted = False Then

                                If LaunchVSpy() = True Then

                                End If

                            End If

                        End If
                    End If
                Else
                    If CanalyzerStarted = True Then
                        QuitCanalyzer()
                    End If
                    If VehicleSpyStarted = True Then
                        QuitVehicleSpy()
                    End If

                End If

            Else 'We are in CLEVIR Playback Mode...
                ParseA2lFile(My.Application.Info.DirectoryPath & "\Enumerations.txt")
            End If


            'Read the excel file (signal list file) that contains display configurations and all signal definitions
            'we cannot proceed if there is an issue reading this file....

            If ReadInSignalList(INCAVariableFile) = False Then
                MsgBox("The GM_ResidentClient application will now terminate.")
                End
            End If

            INCAInitStarted = True

            If PlaybackMode = False Then

                'Here we start the ProcessKiller thread.  This is a separate thread designed to monitor the "Health" of this application
                'and allow the user to kill all related processes if a problem is indicated.  For more information on this functionality
                'refer to the myKillerThread code...

                myKillerThread = New Thread(AddressOf ProcessKiller)
                myKillerThread.Start()

            End If

            'SignalRegistrationMode NEW FULL can be selected on the login screen. This registration mode is used when creating a new
            'experiment from a project specific blank experiment.  All signals in the signal list will be registered using this selection.

            If SignalRegistrationMode = "NEW FULL" Then

                Dim NewExpName As String

                NewExpName = Mid(INCAVariableFile, InStrRev(INCAVariableFile, "\") + 1, Len(INCAVariableFile))
                NewExpName = Mid(NewExpName, 1, InStr(NewExpName, ".xlsx") - 1)

                INCAExperiment = NewExpName
                InitialINCAExperiment = INCAExperiment
                RegisterIntoNewBlankExp = True

            End If

            'Here we initialize INCA based on information read in from either the SaveLoginID.txt file or the config.txt file, depending on
            'how the user has logged in. We use config.txt file if Login as Demo was selected, otherwise we use the user specific config file.

            ForceInit = True

            If PlaybackMode = False Then

                If Debugger.IsAttached = False Then
                    LaunchATT_TCP()
                End If

                CopyToLog(" ")
                CopyToLog("Initialize: Initializing INCA...")
                CopyToLog(" ")

                If myINCAInterface.InitINCA(INCADatabase, INCAWorkspace, INCAExperiment, ForceInit, ErrorMsg, RegisterIntoNewBlankExp) = IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then

                    ExitFlag = True
                    ExitMode = "Complete"

                    WriteToListBox("Initialize INCA Returned - " & ErrorMsg)

                    Me.TopMost = True

                    If InStr(ErrorMsg, "CLEVIR Cannot be started with an Experiment open in INCA") Then

                        MsgBox(ErrorMsg & " - Exiting")
                        CopyToLog("Initialize: " & ErrorMsg & " - Exiting")

                    ElseIf InStr(ErrorMsg, "Experiment") > 0 Then
                        MsgBox("Could not initialize INCA - Please Check config.txt file for correct Experiment name. Exiting...")

                    ElseIf InStr(ErrorMsg, "CheckCodePageConform Returned FALSE") > 0 Then 'VBACC

                        If (OperatingMode = OperatingModes.RES_ON_VPC Or
                   OperatingMode = OperatingModes.RES_ON_SUITCASE_VPC Or
                   OperatingMode = OperatingModes.RES_ON_LAPTOP_VPC) And DebugMode = False Then 'VBACC

                            MsgBox("Detected a CODE PAGE MISMATCH between INCA project content and Flashed controller content.  Please use INCA to FLASH the controller to match the selected workspace. Exiting...")
                            CopyToLog("Initialize: Automatic Exit on CODE PAGE MISMATCH")

                        ElseIf InStr(SignalRegistrationMode, "FULL") = 0 Then 'VBACC

                            If MsgBox("Detected a CODE PAGE MISMATCH between INCA project content and Flashed controller content.  Continue anyway?", vbYesNo) = vbYes Then
                                CopyToLog("Initialize: Continuing After CODE PAGE MISMATCH")
                                ExitFlag = False
                            Else
                                CopyToLog("Initialize: NOT Continuing After CODE PAGE MISMATCH")
                            End If

                        Else
                            ExitFlag = False
                        End If

                    ElseIf InStr(ErrorMsg, "Returned FALSE") > 0 Or InStr(ErrorMsg, "Page = FAIL") > 0 Then 'VBACC

                        If (OperatingMode = OperatingModes.RES_ON_VPC Or
                       OperatingMode = OperatingModes.RES_ON_SUITCASE_VPC Or
                       OperatingMode = OperatingModes.RES_ON_LAPTOP_VPC) And DebugMode = False Then 'VBACC

                            If Not System.IO.File.Exists(My.Application.Info.DirectoryPath & "\IgnoreChecksumMismatch.txt") Then

                                MsgBox("Detected a checksum mismatch between INCA project calibration data and Flashed controller calibration data. Please use INCA to FLASH the controller to match the selected workspace. Exiting...")
                                CopyToLog("Initialize: NOT Continuing After checksum MISMATCH")

                            Else
                                ExitFlag = False
                            End If


                        ElseIf InStr(SignalRegistrationMode, "FULL") = 0 Then 'VBACC

                            If Not System.IO.File.Exists(My.Application.Info.DirectoryPath & "\IgnoreChecksumMismatch.txt") Then

                                If MsgBox("Detected a checksum mismatch between INCA project calibration data and Flashed controller calibration data.  Continue anyway?", vbYesNo) = vbYes Then
                                    CopyToLog("Initialize: Continuing After checksum MISMATCH")

                                    If MsgBox("Ignore this message in the future?", vbYesNo) = vbYes Then

                                        Dim fnum As Integer
                                        fnum = FreeFile()
                                        FileOpen(fnum, My.Application.Info.DirectoryPath & "\IgnoreChecksumMismatch.txt", OpenMode.Output)
                                        PrintLine(fnum, "Ignore Checksum Mismatch")
                                        FileClose(fnum)

                                    End If

                                    ExitFlag = False
                                Else
                                    CopyToLog("Initialize: NOT Continuing After checksum MISMATCH")
                                End If

                            Else
                                ExitFlag = False
                            End If

                        Else
                            ExitFlag = False
                        End If

                    ElseIf InStr(ErrorMsg, "NOT CONNECTED") > 0 Then

                        If (OperatingMode = OperatingModes.RES_ON_VPC Or
                            OperatingMode = OperatingModes.RES_ON_SUITCASE_VPC Or
                            OperatingMode = OperatingModes.RES_ON_LAPTOP_VPC) And DebugMode = False Then

                            MsgBox(ErrorMsg & " Please verify physical connections to XETK hardware. Exiting...")
                            CopyToLog("Initialize: NOT Continuing After " & ErrorMsg)
                        ElseIf InStr(SignalRegistrationMode, "FULL") = 0 Then

                            If Debugger.IsAttached = False Then

                                If MsgBox(ErrorMsg & " Continue anyway?", vbYesNo) = vbYes Then
                                    CopyToLog("Initialize: Continuing After " & ErrorMsg)
                                    ExitFlag = False
                                Else
                                    CopyToLog("Initialize: NOT Continuing After " & ErrorMsg)
                                End If

                            Else
                                ExitFlag = False
                            End If

                        Else
                            ExitFlag = False
                        End If

                    ElseIf InStr(ErrorMsg, "Workspace") > 0 Then
                        MsgBox("Could not initialize INCA - Please Check config.txt file for correct Workspace name. Exiting...")
                    ElseIf InStr(ErrorMsg, "Database") > 0 Then
                        MsgBox("Could not initialize INCA - Please Check config.txt file for correct INCA Database name. Exiting...")
                    Else
                        MsgBox("Could not initialize INCA - Please Check config.txt file for correct INCA Database name, Workspace name and Experiment name. Exiting...")
                    End If

                    If ExitFlag = True Then
                        CopyToLog("Initialize: Exiting - INCA Init Unsuccessful")
                        ExitApp(ExitMode)
                    End If

                Else

                    If InStr(ErrorMsg, "WARNING") > 0 Then
                        CopyToLog("Initialize: INCA WARNING Message Displayed: " & ErrorMsg)
                        Me.TopMost = True
                        MsgBox(ErrorMsg)
                    End If

                End If

                CopyToLog(" ")
                CopyToLog("Initialize: INCA Initialized.")
                CopyToLog(" ")

                'Here we get the project database path and name from INCA.  

                myINCAInterface.SetProjectDatabaseInfo()

                ReferenceDataSetDataBasePaths = myINCAInterface.GetReferenceDataSetDataBasePaths
                WorkingDataSetDataBasePaths = myINCAInterface.GetWorkingDataSetDataBasePaths

                'Here we determine which Vehicle spy configuration to load for recording
                'based on which INCA project we are running.  The software version (the name of the a2l file used in the workspace)
                'dictates the vehicle spy configuration used which is based on ARXML file version that corresponds to the
                'version of software...

                If AlternateRecordingMode = "VehicleSpy" And AlternateRecordEnabled = True And VehicleSpyStarted = True Then

                    If ProjectName = "LowContent" Then
                        ReadARXMLMappingFileNEW(ProjectDatabaseNames(0))
                    ElseIf ProjectName = "HighContent" Then
                        'If HighContent, we want to get the name of the a2l file that corresponds to the HCS, which is what we are
                        'keying off of.  Since there are two processors and we cannot guarantee which processor name will be retrieved
                        'first, we make sure that we call ReadARXMLMappingFileNEW with the HCS a2l file name.
                        If InStr(ProjectDatabaseNames(0), "HCS") > 0 Then
                            ReadARXMLMappingFileNEW(ProjectDatabaseNames(0))
                        Else
                            ReadARXMLMappingFileNEW(ProjectDatabaseNames(1))
                        End If

                    End If

                    CopyToLog("Initialize: Sending VSpy Command:  loadfile " & My.Application.Info.DirectoryPath & "\VehicleSpy\" & VSpySelectedConfigFileName)
                    SendVSpyCommand("loadfile " & My.Application.Info.DirectoryPath & "\VehicleSpy\" & VSpySelectedConfigFileName)

                    'loadfile does not return proper status, so we must just put a delay here...
                    System.Threading.Thread.Sleep(5000)

                End If

            End If 'PlaybackMode

            'which form is displayed is based on the operatingmode

            If OperatingMode = OperatingModes.RES_ON_VPC Or
                OperatingMode = OperatingModes.RES_ON_SUITCASE_VPC Then

                OnVehicleScreen.Show()
                OnVehicleScreen.BringToFront()
                OnVehicleScreen.Activate()
                OnVehicleScreen.Refresh()

            Else
                Me.Show()
                Me.BringToFront()
                Me.Activate()
                Me.Refresh()
            End If

            INCAInitStarted = False

            If Len(SaveLoginID) = 0 Then
                OnVehicleScreen.Label5.Text = "You are not logged in, you must log in to record data."
            End If

            myTDGraphicsContainer = New TDGraphicsContainerClass
            myTDGraphicsContainer.SetupTopDownView()

            'Initial menu creation is also based on things that happen in ReadInSignalList based on the contents of the signal list file...
            'CreateMenus is used for creating the dropdown menus at the top of the main GM_ResidentClient window...

            CreateMenus(0) '0 means start with form index 0, and create menus for all forms created above.

            GoTo bypass 'legacy...

            If OperatingMode <> OperatingModes.RES_ON_VPC And
                OperatingMode <> OperatingModes.RES_ON_SUITCASE_VPC Then
                Me.TopMost = True
                If MsgBox("Preload Device/Raster/Signal info for runtime configuration?", vbYesNo) = vbYes Then
                    myINCAInterface.GetDeviceAquisitionRates()
                End If

            End If
bypass:
            'This was added to check if INCA is powered up properly prior to registering signals....

            If GetDeviceStatus() = False Then
                ExitApp("Complete")
            End If

            'Here is where we read in the contents of the data dictionary and set up the user annotation
            'tabs, we also set up some voice recognition stuff in here because it is based on what is in
            'the data dictionary.
            ParseDataDictionary()

            Me.TopMost = False

            'Here is where we register the signals from our signal list with INCA using the RCI2 interface.
            'The parameter here is the SignalRegistrationMode.  This mode is contained in the configuration file
            'and can be changed from the login screen.

            OnVehicleScreen.ShowInTaskbar = False

            CopyToLog(" ")
            CopyToLog("Initialize: Registering Signals...")
            myINCAInterface.RegisterSignals()
            CopyToLog("Initialize: Signal Registration Complete.")
            CopyToLog(" ")

            OnVehicleScreen.ShowInTaskbar = True

            myVoiceRecognition = New VoiceRecognitionClass
            myVoiceRecognition.InitVoice()

            If AlternateRecordEnabled = True Then
                OnVehicleScreen.Label3.Visible = True 'This is the alt record status label at the top of the main display...
            End If

            'Disable the status thread loop and trigger it to display the final status...
            ProgressBarEnable = False

            WriteToListBox("Initialization Complete")

            Me.GroupBox1.Visible = False
            OnVehicleScreen.GroupBox1.Visible = False

            Me.Refresh()

            'Due to a bug in INCA which causes INCA to hang up at record time if we have performed a FULL registration, 
            'we must shut down And restart INCA here...
            If InStr(SignalRegistrationMode, "FULL") > 0 Then

                If myINCAInterface.SaveExperiment() = False Then
                    CopyToLog("Initialize: Save Experiment returned FALSE")
                    WriteToListBox("Save Experiment returned FALSE")
                Else
                    CopyToLog("Initialize: Save Experiment returned TRUE Restarting INCA...")
                    WriteToListBox("Restarting INCA...")

                    SignalRegistrationMode = "DISPLAYS"
                    ShutdownAndRestartINCA(18000)
                End If

            End If

            If OperatingMode = OperatingModes.RES_ON_VPC Or
               OperatingMode = OperatingModes.RES_ON_SUITCASE_VPC Then

                Me.Hide()
                OnVehicleScreen.BringToFront()
                OnVehicleScreen.Show()
                OnVehicleScreen.Activate()
                OnVehicleScreen.TopMost = False
            Else
                Me.TopMost = False
                Me.Contains(OnVehicleScreen)
                Me.Activate()
            End If

            DeleteUnusedDirectories(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber)

            If AlternateRecordingMode = "VehicleSpy" And AlternateRecordEnabled = True Then
                HandleDIDPull()
            End If

            Initializing = False

            If PlaybackMode = False Then
                CheckForExperiment = True
            End If

            'The background worker is where the main loop runs.  It primarally handles data refresh
            'It also takes care of handling the record start/stop timers and displays related to
            'recording...

            EnableMyBackgroundTasks = True
            Me.BackgroundWorker1.RunWorkerAsync(2000) 'The 2000 here is arbitrary in this 
            'context, it has nothing to do with background worker loop rate.

            Me.Cursor = Cursors.Arrow

            Me.Button1.Visible = True

            If UCase(SaveLoginID) <> "DEMO" Or Debugger.IsAttached = True Then
                OnVehicleScreen.Button8.Visible = True 'This is the CALIBRATE button.  Allows user to create a custom experiment display
                'only available if user logs in with specific user id, not available in demo mode...
            End If

            OnVehicleScreen.Label1.Visible = True
            OnVehicleScreen.PictureBox1.SendToBack()
            OnVehicleScreen.TextBox1.Visible = True
            OnVehicleScreen.TextBox1.BringToFront()
            OnVehicleScreen.RadioButton1.Visible = True
            OnVehicleScreen.RadioButton2.Visible = True

            'We do not set the FormDisplayed flag until we are finished initializing, otherwise, 
            'we get a bunch of resize events called during initialization which isn't good...

            FormDisplayed = True

        Catch ex As Exception
            CopyToLog("Initialize: " & ex.Message)
            MsgBox("Initialize: " & ex.Message)

        End Try

    End Sub

    Public Sub WriteCameraIPAddressesFile()

        'This routine is called from the CheckForCameras routine.  It updates the camera addresses text
        'file with information based on the success or failure of Ping commands to the IP Addresses.

        'So, for example, if we start out with nothing defined in the CameraIPAddresses.txt file, the
        'CheckForCameras pings between .101 and .109, and finds cameras at .101,.102, etc. then
        'it will save the information as to where it found cameras and place this information into
        'the ActualCameraIPAddresses array.  This routine will read this array and save the contents
        'to the file so that next time we run, we will ping only the addresses defined.

        Dim FileName As String
        Dim Fnum As Integer
        Dim Ctr As Integer

        WriteToListBox("Writing Camera IP Addresses File...")

        FileName = My.Application.Info.DirectoryPath & "\CameraIPAddresses.txt"

        Fnum = FreeFile()
        Ctr = 0

        FileOpen(Fnum, FileName, OpenMode.Output)

        For Ctr = 0 To NumberOfCameras - 1
            PrintLine(Fnum, ActualCameraIPAddresses(Ctr))
        Next

        FileClose(Fnum)

    End Sub
    Public Sub ReadCameraIPAddressesFile()

        'Called by CheckForCameras, reads the CameraIPAddresses.txt file (if it exists) and places
        'the information into the Global CameraIPAddresses array for use by CheckForCameras routine.

        Dim FileName As String
        Dim Fnum As Integer
        Dim Textline As String
        Dim Ctr As Integer

        Dim L_CameraIPAddresses() As String

        WriteToListBox("Reading Camera IP Addresses File...")

        FileName = My.Application.Info.DirectoryPath & "\CameraIPAddresses.txt"

        If Not System.IO.File.Exists(FileName) Then
            Exit Sub
        End If

        Fnum = FreeFile()
        Ctr = 0

        L_CameraIPAddresses = Nothing

        FileOpen(Fnum, FileName, OpenMode.Input)

        'Go line by line through the file to pick out data from pre-defined lines in text file

        Do While Not EOF(Fnum)

            ReDim Preserve L_CameraIPAddresses(Ctr)
            Textline = LineInput(Fnum)
            L_CameraIPAddresses(Ctr) = Textline
            Ctr = Ctr + 1

        Loop

        If Ctr <> NumberOfCameras Then
            FileClose(Fnum)
        Else
            CameraIPAddresses = L_CameraIPAddresses
            FileClose(Fnum)
        End If

        WriteToListBox("Reading CameraIPAddresses File Complete")


    End Sub

    Public Function ReadConfigFile() As Boolean

        'Called initially from InitForm_Load event. Also called when the SoftwareVersionSelect_Form: Exit button is pressed if changes were
        'made, to reset to the defaults since we do not save when Exit button is pressed...

        'Also called from the VehicleStatDashboard if necessary...

        'Reads config.txt file, extracts configuration information and puts it into variables.  Used in conjunction with the WriteConfigFile
        'routine.  if we add rows to the config file, associated code must also be added to the WriteConfigFile routine, 
        'or we will lose config data when the config.txt file is written on close.

        'ReadConfigFile should NEVER return false after everything is set up initially on the PC.  If it does, this indicates that 
        'someone went in manually and removed the Database Name, or the file is corrupt for some reason.

        Dim ConfigFileName As String
        Dim Fnum As Integer
        Dim Textline As String
        Dim Ctr As Integer

        CopyToLog("ReadConfigFile Called...")

        ReadConfigFile = True

        WriteToListBox("Reading Config File...")

        ConfigFileName = My.Application.Info.DirectoryPath & "\Config.txt"

        If Not System.IO.File.Exists(ConfigFileName) Then
            CopyToLog("ReadConfigFile: Config.txt file not found, Exiting...")
            MsgBox("Config.txt file not found, Exiting...")
            End
        End If

        Fnum = FreeFile()
        Ctr = 0

        FileOpen(Fnum, ConfigFileName, OpenMode.Input)

        'Go line by line through Config.txt file to pick out data from pre-defined lines in text file

        Do While Not EOF(Fnum)

            Textline = LineInput(Fnum)

            Select Case Ctr
                Case 0
                    INCADatabase = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 1
                    INCAWorkspace = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 2
                    INCAExperiment = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    InitialINCAExperiment = INCAExperiment
                Case 3
                    INCAVariableFile = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))

                    'if there is a path in the filename then use it, otherwise force path to the
                    'application path

                    If InStr(INCAVariableFile, "\") = 0 Then
                        INCAVariableFile = My.Application.Info.DirectoryPath & "\SignalLists\" & INCAVariableFile
                    End If
                Case 4

                    OnVehicleScreen.TextBox1.Text = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    RecordWAVTime = OnVehicleScreen.TextBox1.Text

                Case 5
                    OnVehicleScreen.ComboBox1.Text = Val(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))))
                    RecordFileDurationMinutes = Val(OnVehicleScreen.ComboBox1.Text)
                Case 6
                    NetworkAdapterDescription = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    'NetworkAdapterDescription = CheckForValidWirelessConnection(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))))

                Case 7
                    ETAS_USER_PATH = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 8
                    SignalRegistrationMode = UCase(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))))

                    Select Case SignalRegistrationMode
                        Case "FULL"
                            LoginForm.RadioButton1.Checked = True
                        Case "DISPLAYS"
                            LoginForm.RadioButton2.Checked = True
                        Case "GO/NOGO"
                            LoginForm.RadioButton3.Checked = True
                        Case "NEW FULL"
                            LoginForm.RadioButton4.Checked = True
                        Case Else
                            SignalRegistrationMode = "DISPLAYS"
                            LoginForm.RadioButton2.Checked = True
                    End Select

                Case 9
                    'SaveServerConnection = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 10
                    BaseDataCollectionPath = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 11
                    NetworkDriveLetter = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 12
                    NetworkDriveMapping = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 13
                    'NumberOfCameras = Val(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))))
                Case 14
                    'DisableEnableNetworkAdapter = IIf(UCase(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))) = "TRUE", True, False)
                Case 15
                    EnableAltRecReStartAfterRecordStop = IIf(UCase(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))) = "TRUE", True, False)
                Case 16
                    SaveCalSnapshotEnabled = IIf(UCase(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))) = "TRUE", True, False)
                Case 17
                    AlternateRecordEnabled = IIf(UCase(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))) = "TRUE", True, False)
                Case 18
                    CurrentVehicleUsage = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    If CurrentVehicleUsage = "DEVELOPMENT" Then
                        InitForm.RadioButton1.Checked = True
                    ElseIf CurrentVehicleUsage = "VALIDATION" Then
                        InitForm.RadioButton2.Checked = True
                    Else
                        InitForm.RadioButton1.Checked = False
                        InitForm.RadioButton2.Checked = False
                    End If
                    CopyToLog("ReadConfigFile: CurrentVehicleUsage = " & CurrentVehicleUsage)
                Case Else
                    CopyToLog("ReadConfigFile: There appear to be extra lines in the config.txt file...")

            End Select
            Ctr = Ctr + 1
        Loop

        FileClose(Fnum)

        'We have seen instances where the config.txt file becomes corrupted somehow (Haven't figured out under what circumstances
        'this happens).  Typically, the file is blank, which causes obvious issues when the file is read.  So, here we are
        'checking to see if the contents of the config.txt file appears valid (we check the first three lines for appropriate
        'content).  If the file appears okay, we save it to Config_SAVE.txt.

        'If the file appears corrupted, we will then copy the saved version to the original file name and re-call ReadConfigFile
        'with the saved contents.  This eliminates failing on ReadConfigFile and having to reconstruct the config.txt file...

        If Len(INCADatabase) > 0 And Len(INCAWorkspace) > 0 And Len(INCAExperiment) > 0 And Len(INCAVariableFile) > 0 Then
            System.IO.File.Copy(ConfigFileName, My.Application.Info.DirectoryPath & "\Config_SAVE.txt", True)
        Else
            CopyToLog("ReadConfigFile: Reading Config File - Corrupted config.txt file - copying Config_SAVE.txt to config.txt")
            System.IO.File.Copy(My.Application.Info.DirectoryPath & "\Config_SAVE.txt", ConfigFileName, True)

            ReadConfigFile()
            Exit Function
        End If

        CopyToLog("ReadConfigFile: Reading Config File Complete")
        WriteToListBox("Reading Config File Complete")

    End Function


    Public Function ReadUserConfigFile(ByVal ConfigFileName As String) As Integer

        'Called from the Initialize routine, when the user logs in with a EDSNET ID

        'In this case, the user specific configuration file will be used and not the default config.txt file

        'Reads the user specific config.txt file (userid.txt), extracts configuration information and puts it into variables.  
        'Used in conjunction with the WriteUserConfigFile routine.  If anything is added here, it must also be added to the 
        'WriteUserConfigFile routine, or we will lose config data when the config.txt file is written on close.

        Dim Fnum As Integer
        Dim Textline As String
        Dim Ctr As Integer

        Dim ReturnValue As Integer = 0

        UserConfigFileName = My.Application.Info.DirectoryPath & "\" & ConfigFileName

        WriteToListBox("Reading User Config File " & UserConfigFileName & "...")

        'If the user has never logged in, they don't have a configuration file associated with their login ID so
        'one is created ("User loginID".txt) by copying the contents of the config.txt file as a starting point...

        If Not System.IO.File.Exists(UserConfigFileName) Then
            WriteToListBox("Copying Default Config File to User Config File " & UserConfigFileName & "...")

            UserStatusInfo.Label1.Text = "Copying Default Config File to " & UserConfigFileName & "..."
            System.Threading.Thread.Sleep(2000)

            UserStatusInfo.Hide()

            FileCopy(My.Application.Info.DirectoryPath & "\config.txt", UserConfigFileName)
            System.Threading.Thread.Sleep(1000)
            Exit Function

        End If

        Fnum = FreeFile()
        Ctr = 0

        FileOpen(Fnum, UserConfigFileName, OpenMode.Input)

        'Go line by line through Config.txt file to pick out data from pre-defined lines in text file

        Do While Not EOF(Fnum)

            Textline = LineInput(Fnum)

            Select Case Ctr
                Case 0
                    If VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))) <> INCADatabase Then
                        ReturnValue = 1
                        INCADatabase = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    End If
                Case 1
                    If VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))) <> INCAWorkspace Then
                        ReturnValue = ReturnValue Or 2
                        INCAWorkspace = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    End If
                Case 2
                    If VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))) <> INCAExperiment Then
                        ReturnValue = ReturnValue Or 4
                        INCAExperiment = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    End If
                    InitialINCAExperiment = INCAExperiment
                Case 3
                    If VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))) <> INCAVariableFile Then
                        ReturnValue = ReturnValue Or 8
                        INCAVariableFile = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    End If

                    If InStr(INCAVariableFile, "\") = 0 Then
                        INCAVariableFile = My.Application.Info.DirectoryPath & "\SignalLists\" & INCAVariableFile
                    End If
                Case 4

                    OnVehicleScreen.TextBox1.Text = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    RecordWAVTime = OnVehicleScreen.TextBox1.Text
                Case 5

                    OnVehicleScreen.ComboBox1.Text = Val(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))))
                    RecordFileDurationMinutes = Val(OnVehicleScreen.ComboBox1.Text)
                Case 6
                    If VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))) <> NetworkAdapterDescription Then
                        NetworkAdapterDescription = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                    End If
                Case 7
                    ETAS_USER_PATH = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 8

                    'do something if the user configuration file signalregistrationmode is different than what is selected
                    'on the login form? (which is actually set by reading the config.txt file) which occurs before this in
                    'the initialization sequence.

                    'SignalRegistrationMode = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))

                    'Select Case SignalRegistrationMode
                    'Case "FULL"
                    'LoginForm.RadioButton1.Checked = True
                    'Case "DISPLAYS"
                    'LoginForm.RadioButton2.Checked = True
                    'Case "GO/NOGO"
                    'LoginForm.RadioButton3.Checked = True
                    'Case Else
                    'MsgBox("Invalid Signal Registration Mode in Config.txt File, Exiting.")
                    'ExitApp()
                    'End Select


                Case 9
                    'SaveServerConnection = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 10
                    BaseDataCollectionPath = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 11
                    If UsingFlashDrive = False Then
                        If VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))) <> NetworkDriveLetter Then
                            NetworkDriveLetter = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                        End If
                    End If
                Case 12
                    NetworkDriveMapping = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))
                Case 13
                    'NumberOfCameras = Val(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9))))
                Case 14
                    'DisableEnableNetworkAdapter = IIf(UCase(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))) = "TRUE", True, False)
                Case 15
                    EnableAltRecReStartAfterRecordStop = IIf(UCase(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))) = "TRUE", True, False)
                    'DefaultConfiguration.ComboBox2.Text = EnableAltRecReStartAfterRecordStop.ToString
                Case 16
                    'SaveCalSnapshotEnabled = IIf(UCase(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))) = "TRUE", True, False)
                    'SaveCalSnapshotEnabled = LoginForm.CheckBox1.Checked
                Case 17
                    'AlternateRecordEnabled = IIf(UCase(VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))) = "TRUE", True, False)
                    'AlternateRecordEnabled = LoginForm.CheckBox3.Checked
                Case 18
                    CurrentVehicleUsage = VB.Right(Textline, Len(Textline) - InStr(Textline, Chr(9)))

                    If CurrentVehicleUsage = "DEVELOPMENT" Then
                        InitForm.RadioButton1.Checked = True
                    ElseIf CurrentVehicleUsage = "VALIDATION" Then
                        InitForm.RadioButton2.Checked = True
                    Else
                        InitForm.RadioButton1.Checked = False
                        InitForm.RadioButton2.Checked = False
                    End If
                    CopyToLog("ReadUserConfigFile: CurrentVehicleUsage = " & CurrentVehicleUsage)
                Case Else
                    CopyToLog("ReadUserConfigFile: Read User Config File: More than 17 lines in file...")

            End Select
            Ctr = Ctr + 1
        Loop

        FileClose(Fnum)

        WriteToListBox("Reading User Config File Complete")

        ReadUserConfigFile = ReturnValue


    End Function

    Public Sub WriteConfigFile()

        'This routine writes data to the config.txt file.  Called when user exits the application and from other places when
        'it is prudent to save changes made to variables who's values reside in the config.txt file...

        Dim Filename As String
        Dim Fnum As Integer
        Dim Textline As String

        Dim x As Integer

        Filename = My.Application.Info.DirectoryPath & "\Config.txt"
        Textline = ""

        Fnum = FreeFile()

        FileOpen(Fnum, Filename, OpenMode.Output)

        For x = 0 To 18 'if more cases added, need to increment this number accordingly
            Select Case x
                Case 0
                    Textline = "INCADatabase" & Chr(9) & INCADatabase
                Case 1
                    Textline = "INCAWorkspace" & Chr(9) & INCAWorkspace
                Case 2
                    Textline = "INCAExperiment" & Chr(9) & InitialINCAExperiment
                Case 3
                    Textline = "INCAVariableFile" & Chr(9) & INCAVariableFile
                Case 4
                    Textline = "RecordWAVTime" & Chr(9) & RecordWAVTime
                Case 5
                    Textline = "RecordFileDurationMinutes" & Chr(9) & CStr(RecordFileDurationMinutes)
                Case 6
                    Textline = "NetworkAdapterDescription" & Chr(9) & NetworkAdapterDescription

                Case 7
                    Textline = "ETAS_USER_PATH" & Chr(9) & ETAS_USER_PATH
                Case 8
                    Textline = "SignalRegistrationMode" & Chr(9) & SignalRegistrationMode
                Case 9
                    Textline = "ServerConnection" & Chr(9) & "N/A"
                Case 10
                    If BaseDataCollectionPath = NetworkDriveLetter And InStr(SaveBaseDataCollectionPath, NetworkDriveLetter) = 0 Then
                        BaseDataCollectionPath = SaveBaseDataCollectionPath
                    End If
                    Textline = "BaseDataCollectionPath" & Chr(9) & BaseDataCollectionPath
                Case 11
                    If UsingFlashDrive = True And InStr(SaveNetworkDriveLetter, NetworkDriveLetter) = 0 Then
                        Textline = "NetworkDriveLetter" & Chr(9) & SaveNetworkDriveLetter
                    Else
                        Textline = "NetworkDriveLetter" & Chr(9) & NetworkDriveLetter
                    End If
                Case 12
                    Textline = "NetworkDriveMapping" & Chr(9) & NetworkDriveMapping
                Case 13
                    Textline = "NumberOfCameras" & Chr(9) & "N/A"
                Case 14
                    Textline = "DisableEnableNetworkAdapter" & Chr(9) & "N/A"
                Case 15
                    Textline = "EnableAltRecReStartAfterRecordStop" & Chr(9) & EnableAltRecReStartAfterRecordStop.ToString
                Case 16
                    Textline = "SaveCalSnapshotEnabled" & Chr(9) & SaveCalSnapshotEnabled.ToString
                Case 17
                    Textline = "AlternateRecordEnabled" & Chr(9) & AlternateRecordEnabled.ToString
                Case 18
                    Textline = "CurrentVehicleUsage" & Chr(9) & CurrentVehicleUsage
            End Select

            PrintLine(Fnum, Textline)
        Next

        FileClose(Fnum)

    End Sub

    Public Sub WriteUserConfigFile(ByVal Filename As String)

        'This routine writes data to the User specific config.txt file.  
        'Called when user exits the application and from other places when
        'it is prudent to save changes made to variables who's values reside in the user specific (EDSNETID.txt) file...

        Dim Fnum As Integer
        Dim Textline As String

        Dim x As Integer

        Textline = ""

        Fnum = FreeFile()

        FileOpen(Fnum, Filename, OpenMode.Output)

        For x = 0 To 18 'if more cases added, need to increment this number accordingly
            Select Case x
                Case 0
                    Textline = "INCADatabase" & Chr(9) & INCADatabase
                Case 1
                    Textline = "INCAWorkspace" & Chr(9) & INCAWorkspace
                Case 2
                    Textline = "INCAExperiment" & Chr(9) & InitialINCAExperiment
                Case 3
                    Textline = "INCAVariableFile" & Chr(9) & INCAVariableFile
                Case 4
                    Textline = "RecordWAVTime" & Chr(9) & RecordWAVTime
                Case 5
                    Textline = "RecordFileDurationMinutes" & Chr(9) & CStr(RecordFileDurationMinutes)
                Case 6
                    Textline = "NetworkAdapterDescription" & Chr(9) & NetworkAdapterDescription
                Case 7
                    Textline = "ETAS_USER_PATH" & Chr(9) & ETAS_USER_PATH
                Case 8
                    Textline = "SignalRegistrationMode" & Chr(9) & SignalRegistrationMode
                Case 9
                    Textline = "ServerConnection" & Chr(9) & "N/A"
                Case 10
                    Textline = "BaseDataCollectionPath" & Chr(9) & BaseDataCollectionPath
                Case 11

                    If UsingFlashDrive = True And InStr(SaveNetworkDriveLetter, NetworkDriveLetter) = 0 Then
                        Textline = "NetworkDriveLetter" & Chr(9) & SaveNetworkDriveLetter
                    Else
                        Textline = "NetworkDriveLetter" & Chr(9) & NetworkDriveLetter
                    End If

                Case 12
                    Textline = "NetworkDriveMapping" & Chr(9) & NetworkDriveMapping
                Case 13
                    Textline = "NumberOfCameras" & Chr(9) & "N/A"
                Case 14
                    Textline = "DisableEnableNetworkAdapter" & Chr(9) & "N/A"
                Case 15
                    Textline = "EnableAltRecReStartAfterRecordStop" & Chr(9) & EnableAltRecReStartAfterRecordStop.ToString
                Case 16
                    Textline = "SaveCalSnapshotEnabled" & Chr(9) & SaveCalSnapshotEnabled.ToString
                Case 17
                    Textline = "AlternateRecordEnabled" & Chr(9) & AlternateRecordEnabled.ToString
                Case 18
                    Textline = "CurrentVehicleUsage" & Chr(9) & CurrentVehicleUsage
            End Select

            PrintLine(Fnum, Textline)
        Next

        FileClose(Fnum)

    End Sub

    Public Sub WriteSignalListFile(ByVal SignalListFileName As String)

        'Save data back to the Excel or .csv file which contains the variable and display information
        'If anything has been changed (using the online dynamic re-configuration), the changes will be saved into the
        'previously used excel spreadsheet or .csv file.  So, if we re-start the GM_ResidentClient
        'we will be using the spreadsheet or .csv file with changes made during the previous session.

        'This is called when we exit the main form, but only if changes have been made.

        'It is also possible to call this routine from a button press on the GM Resident Client Configuration form (GridCellPropConfig.vb)

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer
        Dim n As Integer

        Dim excelApp As Object = Nothing
        Dim wrkbk As Object = Nothing
        Dim myWorkSheet As Object = Nothing

        Dim ccon As ColorConverter

        Dim FileExtension As String

        Dim fnum As Integer

        Dim Inputline As String = ""

        Try


            If System.IO.File.Exists(SignalListFileName) = False Then
                MsgBox(SignalListFileName & " cannot be found. Exiting")
                Exit Sub
            End If

            ccon = New ColorConverter()

            FileExtension = Path.GetExtension(SignalListFileName)

            If FileExtension = ".xlsx" Then
                excelApp = CreateObject("Excel.Application")
            End If

            'Must allow for additional data to have been added so we add 'NumSignalsAdded' to the current upper bound
            'of the exceldata array which was read in at startup. NumSignalsAdded is incremented whenever we add a new signal via
            'a new grid

            '0 based array vs 1 based array, here we copy everything from the 1 based exceldata array to the 0 based exceldataforsave array.
            'exceldata is 1 based because it is derived from exceldata = myWorkSheet.UsedRange.Value, which is Excel way of doing things,
            'in vb.net, any array that we create  must be a 0 based array...

            If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") And Not ValidExcelData Is Nothing Then

                ReDim exceldataforsave((UBound(ValidExcelData, 1) - NumInvalid) + NumSignalsAdded, UBound(exceldata, 2))

                For x = 0 To UBound(exceldataforsave, 1)

                    If Len(ValidExcelData(x, 0)) > 0 Then
                        For i = 0 To EXCEL_DATA.DisplayFormat - 1
                            exceldataforsave(x, i) = ValidExcelData(x, i)
                        Next i
                    Else
                        exceldataforsave(x, i) = Nothing
                        CopyToLog("WriteSignalListFile: ValidExcelData " & x & " = ***" & ValidExcelData(x, 0) & "***")
                    End If

                Next x

            Else
                ReDim exceldataforsave(UBound(exceldata, 1) + NumSignalsAdded, UBound(exceldata, 2))

                For x = 1 To UBound(exceldata, 1)

                    For i = 1 To EXCEL_DATA.DisplayFormat
                        exceldataforsave(x - 1, i - 1) = exceldata(x, i)
                    Next i

                Next x
            End If

            'now go thru all datagrids and populate the exceldataforsave array with the data associated with each datagrid

            i = 1
            For x = 0 To UBound(myDGs)

                For n = 0 To UBound(myDFs)
                    If myDFs(n).Name = myDGs(x).Parent.Name Then
                        Exit For
                    End If
                Next n

                If n <= UBound(myDFs) Then

                    exceldataforsave(i, EXCEL_DATA.DisplayWindowSize - 1) = myDFs(n).DisplayWindowSize
                    exceldataforsave(i, EXCEL_DATA.AlsoAssociatedWith - 1) = myDFs(n).AlsoAssociatedWith
                    exceldataforsave(i, EXCEL_DATA.LocationOnForm - 1) = myDGs(x).LocationOnForm
                    exceldataforsave(i, EXCEL_DATA.GridSize - 1) = myDGs(x).GridSize

                    For y = 1 To myDGs(x).RowCount
                        For z = 1 To myDGs(x).ColumnCount - 1

                            exceldataforsave(i, EXCEL_DATA.DisplayWindowName - 1) = myDFs(n).Name
                            exceldataforsave(i, EXCEL_DATA.AssociatedControlName - 1) = myDGs(x).Name

                            exceldataforsave(i, EXCEL_DATA.DisplayName - 1) = myDGs(x).DisplayName(y, 1)

                            If Len(myDGs(x).VariableName(y, z)) > 0 Then
                                exceldataforsave(i, EXCEL_DATA.VariableName - 1) = myDGs(x).VariableName(y, z)
                            Else
                                exceldataforsave(i, EXCEL_DATA.VariableName - 1) = "undefined"
                            End If

                            If Len(myDGs(x).DeviceName(y, z)) > 0 Then
                                exceldataforsave(i, EXCEL_DATA.DeviceName - 1) = myDGs(x).DeviceName(y, z)
                            Else
                                exceldataforsave(i, EXCEL_DATA.DeviceName - 1) = "undefined"
                            End If

                            If Len(myDGs(x).Raster(y, z)) > 0 Then
                                exceldataforsave(i, EXCEL_DATA.Raster - 1) = myDGs(x).Raster(y, z)
                            Else
                                exceldataforsave(i, EXCEL_DATA.Raster - 1) = "undefined"
                            End If

                            exceldataforsave(i, EXCEL_DATA.DefaultBackColor - 1) = ccon.ConvertToString(myDGs(x).DefaultCellBackColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.DefaultForeColor - 1) = ccon.ConvertToString(myDGs(x).DefaultCellForeColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.HighThreshBackColor - 1) = ccon.ConvertToString(myDGs(x).HighThreshBackColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.LowThreshBackColor - 1) = ccon.ConvertToString(myDGs(x).LowThreshBackColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.HighThreshForeColor - 1) = ccon.ConvertToString(myDGs(x).HighThreshForeColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.LowThreshForeColor - 1) = ccon.ConvertToString(myDGs(x).LowThreshForeColor(y, z))

                            exceldataforsave(i, EXCEL_DATA.HighThresh - 1) = myDGs(x).HighThresh(y, z)
                            exceldataforsave(i, EXCEL_DATA.LowThresh - 1) = myDGs(x).LowThresh(y, z)
                            exceldataforsave(i, EXCEL_DATA.EqualTo - 1) = myDGs(x).EqualTo(y, z)
                            exceldataforsave(i, EXCEL_DATA.CheckForDataChange - 1) = IIf(Len(myDGs(x).CheckForDataChange(y, z)) > 0, myDGs(x).CheckForDataChange(y, z), "FALSE")
                            exceldataforsave(i, EXCEL_DATA.Row - 1) = y
                            exceldataforsave(i, EXCEL_DATA.Col - 1) = z

                            exceldataforsave(i, EXCEL_DATA.AlsoAssociatedWith - 1) = myDGs(x).AlsoAssociatedWith(y, z)

                            exceldataforsave(i, EXCEL_DATA.DisplayFormat - 1) = IIf(Len(myDGs(x).DisplayFormat(y, z)) > 0, myDGs(x).DisplayFormat(y, z), """0.000""")

                            i = i + 1
                        Next z
                    Next y

                End If

            Next x

            'Here we must take care of all of the signals remaining in the file after the display signals
            'we must also handle if there have been new "invisible" signals added.  This code below does
            'not currently work correctly if we have initialized as DISPLAYS and we add more record only
            'or invisible signals.  This is because we are stepping on all invisible signals with the
            'newly added record only signals...

            If NumSignalsAdded > 0 Then

                y = 0

                If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") Then

                    For x = UBound(ValidExcelData, 1) - NumInvalid To (UBound(exceldataforsave, 1) - 1)

                        exceldataforsave(x, EXCEL_DATA.VariableName - 1) = myINCAInterface.myAddedSignals(y).SignalName
                        exceldataforsave(x, EXCEL_DATA.DeviceName - 1) = myINCAInterface.myAddedSignals(y).DeviceName
                        exceldataforsave(x, EXCEL_DATA.Raster - 1) = myINCAInterface.myAddedSignals(y).RasterName
                        y = y + 1

                    Next
                Else
                    For x = UBound(exceldata, 1) To UBound(exceldataforsave, 1) - 1

                        exceldataforsave(x, EXCEL_DATA.VariableName - 1) = myINCAInterface.myAddedSignals(y).SignalName
                        exceldataforsave(x, EXCEL_DATA.DeviceName - 1) = myINCAInterface.myAddedSignals(y).DeviceName
                        exceldataforsave(x, EXCEL_DATA.Raster - 1) = myINCAInterface.myAddedSignals(y).RasterName
                        y = y + 1

                    Next
                End If

            End If

            Dim savefilename As String

            SaveFileDialog1.DefaultExt = FileExtension
            SaveFileDialog1.FileName = INCAVariableFile
            SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
            SaveFileDialog1.Filter = Mid(FileExtension, 2, Len(FileExtension)) & " |*" & FileExtension
            SaveFileDialog1.ShowDialog()

            savefilename = SaveFileDialog1.FileName

            If Len(savefilename) > 0 Then

                If savefilename = INCAVariableFile Then
                    WriteToListBox("Updating " & INCAVariableFile)
                Else
                    WriteToListBox("Creating New INCA Variable File (" & savefilename & ")")
                End If

                If FileExtension = ".xlsx" Then

                    wrkbk = excelApp.Workbooks.Open(INCAVariableFile)
                    myWorkSheet = wrkbk.Sheets(1)
                    myWorkSheet.Activate()

                    If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") And Not (ValidExcelData Is Nothing) Then

                        If UBound(exceldataforsave, 1) >= UBound(exceldata, 1) Then

                            myWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1) + 1, UBound(ValidExcelData, 2) + 1).Value = exceldataforsave

                        Else

                            myWorkSheet.UsedRange.Clear()
                            myWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1) + 1, UBound(ValidExcelData, 2) + 1).Value = exceldataforsave

                        End If
                    Else

                        myWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1), UBound(exceldata, 2)).Value = exceldataforsave
                    End If

                    WriteToListBox("Excel File Written successfully")

                    If savefilename = INCAVariableFile Then
                        wrkbk.Save()
                    Else
                        wrkbk.SaveAs(savefilename)

                        If MsgBox("Do you wish to update the configuration file to use this newly saved variable file?", vbYesNo) = vbYes Then
                            INCAVariableFile = savefilename
                        End If

                    End If

                    excelApp.Quit()
                    excelApp = Nothing

                Else '.csv file...

                    fnum = FreeFile()

                    FileOpen(fnum, savefilename, OpenMode.Output)

                    For x = 0 To UBound(exceldataforsave, 1)
                        For y = 0 To UBound(exceldataforsave, 2)
                            If y = 0 Then
                                Inputline = exceldataforsave(x, y)
                            Else
                                Inputline = Inputline & "," & exceldataforsave(x, y)
                            End If
                        Next
                        PrintLine(fnum, Inputline)
                    Next

                    FileClose(fnum)

                    WriteToListBox("Signal List File Written successfully")

                    If savefilename <> INCAVariableFile Then

                        If MsgBox("Do you wish to update the configuration file to use this newly saved variable file?", vbYesNo) = vbYes Then
                            INCAVariableFile = savefilename
                        End If

                    End If

                End If

                GridCellPropConfig.ChangesMade = False
                NumSignalsAdded = 0

            Else
                MsgBox("No Valid Filename selected, changes will not be saved....")
            End If

            'If there is an InvalidSignalsLog.csv file, we will delete it here...
            'This file is used during signal registration to log all of the signals
            'which cannot, for whatever reason, be registered.  The intent is to keep
            'track of unregisterable signals after each initialization attempt and delete
            'such signals from the signal list so subsequent signal registration attempts
            'will not include these signals. Signals are marked for deletion in the
            'ReadInSignalList routine...

            If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") Then
                File.Move(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv", My.Application.Info.DirectoryPath & "\InvalidSignalsLog_" & Format(Now, "MMddyyyy_hhmmss") & ".csv")
            End If

        Catch ex As Exception

            MsgBox("WriteSignalListFile: " & ex.Message)

        End Try

    End Sub

    Public Sub WriteExcelFile(ByVal ExcelFileName As String)

        'No longer used, replaced by WriteSignalListFile...

        'Save data back to the Excel file which contains the variable and display information
        'If anything has been changed (using the online dynamic re-configuration), the changes will be saved into the
        'previously used excel spreadsheet.  So, if we re-start the GM_ResidentClient
        'we will be using the spreadsheet with changes made during the previous session.

        'This is called when we exit the main form, but only if changes have been made.

        'It is also possible to call this routine from a button press on the GM Resident Client Configuration form (GridCellPropConfig.vb)

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer
        Dim n As Integer

        Dim excelApp As Object
        Dim wrkbk As Object
        Dim myWorkSheet As Object

        Dim ccon As ColorConverter

        Try

            ccon = New ColorConverter()

            excelApp = CreateObject("Excel.Application")

            If System.IO.File.Exists(ExcelFileName) = False Then
                MsgBox(ExcelFileName & " cannot be found. Exiting")
                Exit Sub
            End If

            'Must allow for additional data to have been added so we add 'NumSignalsAdded' to the current upper bound
            'of the exceldata array which was read in at startup. NumSignalsAdded is incremented whenever we add a new signal via
            'a new grid

            '0 based array vs 1 based array, here we copy everything from the 1 based exceldata array to the 0 based exceldataforsave array.
            'exceldata is 1 based because it is derived from exceldata = myWorkSheet.UsedRange.Value, which is Excel way of doing things,
            'in vb.net, any array that we create  must be a 0 based array...

            If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") And Not ValidExcelData Is Nothing Then

                ReDim exceldataforsave((UBound(ValidExcelData, 1) - NumInvalid) + NumSignalsAdded, UBound(exceldata, 2))

                For x = 0 To UBound(exceldataforsave, 1)

                    If Len(ValidExcelData(x, 0)) > 0 Then
                        For i = 0 To EXCEL_DATA.DisplayFormat - 1
                            exceldataforsave(x, i) = ValidExcelData(x, i)
                        Next i
                    Else
                        exceldataforsave(x, i) = Nothing
                        CopyToLog("WriteSignalListFile: ValidExcelData " & x & " = ***" & ValidExcelData(x, 0) & "***")
                    End If

                Next x

            Else
                ReDim exceldataforsave(UBound(exceldata, 1) + NumSignalsAdded, UBound(exceldata, 2))

                For x = 1 To UBound(exceldata, 1)

                    For i = 1 To EXCEL_DATA.DisplayFormat
                        exceldataforsave(x - 1, i - 1) = exceldata(x, i)
                    Next i

                Next x
            End If

            'now go thru all datagrids and populate the exceldataforsave array with the data associated with each datagrid

            i = 1
            For x = 0 To UBound(myDGs)

                For n = 0 To UBound(myDFs)
                    If myDFs(n).Name = myDGs(x).Parent.Name Then
                        Exit For
                    End If
                Next n

                If n <= UBound(myDFs) Then

                    exceldataforsave(i, EXCEL_DATA.DisplayWindowSize - 1) = myDFs(n).DisplayWindowSize
                    exceldataforsave(i, EXCEL_DATA.AlsoAssociatedWith - 1) = myDFs(n).AlsoAssociatedWith
                    exceldataforsave(i, EXCEL_DATA.LocationOnForm - 1) = myDGs(x).LocationOnForm
                    exceldataforsave(i, EXCEL_DATA.GridSize - 1) = myDGs(x).GridSize

                    For y = 1 To myDGs(x).RowCount
                        For z = 1 To myDGs(x).ColumnCount - 1

                            exceldataforsave(i, EXCEL_DATA.DisplayWindowName - 1) = myDFs(n).Name
                            exceldataforsave(i, EXCEL_DATA.AssociatedControlName - 1) = myDGs(x).Name

                            exceldataforsave(i, EXCEL_DATA.DisplayName - 1) = myDGs(x).DisplayName(y, 1)

                            If Len(myDGs(x).VariableName(y, z)) > 0 Then
                                exceldataforsave(i, EXCEL_DATA.VariableName - 1) = myDGs(x).VariableName(y, z)
                            Else
                                exceldataforsave(i, EXCEL_DATA.VariableName - 1) = "undefined"
                            End If

                            If Len(myDGs(x).DeviceName(y, z)) > 0 Then
                                exceldataforsave(i, EXCEL_DATA.DeviceName - 1) = myDGs(x).DeviceName(y, z)
                            Else
                                exceldataforsave(i, EXCEL_DATA.DeviceName - 1) = "undefined"
                            End If

                            If Len(myDGs(x).Raster(y, z)) > 0 Then
                                exceldataforsave(i, EXCEL_DATA.Raster - 1) = myDGs(x).Raster(y, z)
                            Else
                                exceldataforsave(i, EXCEL_DATA.Raster - 1) = "undefined"
                            End If

                            exceldataforsave(i, EXCEL_DATA.DefaultBackColor - 1) = ccon.ConvertToString(myDGs(x).DefaultCellBackColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.DefaultForeColor - 1) = ccon.ConvertToString(myDGs(x).DefaultCellForeColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.HighThreshBackColor - 1) = ccon.ConvertToString(myDGs(x).HighThreshBackColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.LowThreshBackColor - 1) = ccon.ConvertToString(myDGs(x).LowThreshBackColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.HighThreshForeColor - 1) = ccon.ConvertToString(myDGs(x).HighThreshForeColor(y, z))
                            exceldataforsave(i, EXCEL_DATA.LowThreshForeColor - 1) = ccon.ConvertToString(myDGs(x).LowThreshForeColor(y, z))

                            exceldataforsave(i, EXCEL_DATA.HighThresh - 1) = myDGs(x).HighThresh(y, z)
                            exceldataforsave(i, EXCEL_DATA.LowThresh - 1) = myDGs(x).LowThresh(y, z)
                            exceldataforsave(i, EXCEL_DATA.EqualTo - 1) = myDGs(x).EqualTo(y, z)
                            exceldataforsave(i, EXCEL_DATA.CheckForDataChange - 1) = IIf(Len(myDGs(x).CheckForDataChange(y, z)) > 0, myDGs(x).CheckForDataChange(y, z), "FALSE")
                            exceldataforsave(i, EXCEL_DATA.Row - 1) = y
                            exceldataforsave(i, EXCEL_DATA.Col - 1) = z

                            exceldataforsave(i, EXCEL_DATA.AlsoAssociatedWith - 1) = myDGs(x).AlsoAssociatedWith(y, z)

                            exceldataforsave(i, EXCEL_DATA.DisplayFormat - 1) = IIf(Len(myDGs(x).DisplayFormat(y, z)) > 0, myDGs(x).DisplayFormat(y, z), """0.000""")

                            i = i + 1
                        Next z
                    Next y

                End If

            Next x

            'Here we must take care of all of the signals remaining in the file after the display signals
            'we must also handle if there have been new "invisible" signals added.  This code below does
            'not currently work correctly if we have initialized as DISPLAYS and we add more record only
            'or invisible signals.  This is because we are stepping on all invisible signals with the
            'newly added record only signals...

            If NumSignalsAdded > 0 Then

                y = 0

                If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") Then

                    For x = UBound(ValidExcelData, 1) - NumInvalid To (UBound(exceldataforsave, 1) - 1)

                        exceldataforsave(x, EXCEL_DATA.VariableName - 1) = myINCAInterface.myAddedSignals(y).SignalName
                        exceldataforsave(x, EXCEL_DATA.DeviceName - 1) = myINCAInterface.myAddedSignals(y).DeviceName
                        exceldataforsave(x, EXCEL_DATA.Raster - 1) = myINCAInterface.myAddedSignals(y).RasterName
                        y = y + 1

                    Next
                Else
                    For x = UBound(exceldata, 1) To UBound(exceldataforsave, 1) - 1

                        exceldataforsave(x, EXCEL_DATA.VariableName - 1) = myINCAInterface.myAddedSignals(y).SignalName
                        exceldataforsave(x, EXCEL_DATA.DeviceName - 1) = myINCAInterface.myAddedSignals(y).DeviceName
                        exceldataforsave(x, EXCEL_DATA.Raster - 1) = myINCAInterface.myAddedSignals(y).RasterName
                        y = y + 1

                    Next
                End If

            End If

            Dim savefilename As String

            SaveFileDialog1.DefaultExt = ".xlsx"
            SaveFileDialog1.FileName = INCAVariableFile
            SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
            SaveFileDialog1.Filter = "xlsx |*.xlsx"
            SaveFileDialog1.ShowDialog()

            savefilename = SaveFileDialog1.FileName

            If Len(savefilename) > 0 Then

                If savefilename = INCAVariableFile Then
                    WriteToListBox("Updating " & INCAVariableFile)
                Else
                    WriteToListBox("Creating New INCA Variable File (" & savefilename & ")")
                End If

                wrkbk = excelApp.Workbooks.Open(INCAVariableFile)

                myWorkSheet = wrkbk.Sheets(1)

                myWorkSheet.Activate()

                If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") And Not (ValidExcelData Is Nothing) Then

                    If UBound(exceldataforsave, 1) >= UBound(exceldata, 1) Then

                        myWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1) + 1, UBound(ValidExcelData, 2) + 1).Value = exceldataforsave

                    Else

                        myWorkSheet.UsedRange.Clear()
                        myWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1) + 1, UBound(ValidExcelData, 2) + 1).Value = exceldataforsave

                    End If
                Else

                    myWorkSheet.Range("A1").Resize(UBound(exceldataforsave, 1), UBound(exceldata, 2)).Value = exceldataforsave
                End If

                WriteToListBox("Excel File Written successfully")

                If savefilename = INCAVariableFile Then
                    wrkbk.Save()
                Else
                    wrkbk.SaveAs(savefilename)

                    If MsgBox("Do you wish to update the configuration file to use this newly saved variable file?", vbYesNo) = vbYes Then
                        INCAVariableFile = savefilename
                    End If

                End If

                excelApp.Quit()
                excelApp = Nothing

                GridCellPropConfig.ChangesMade = False
                NumSignalsAdded = 0

            Else
                MsgBox("No Valid Filename selected, changes will not be saved....")
            End If

            'If there is an InvalidSignalsLog.csv file, we will delete it here...
            'This file is used during signal registration to log all of the signals
            'which cannot, for whatever reason, be registered.  The intent is to keep
            'track of unregisterable signals after each initialization attempt and delete
            'such signals from the signal list so subsequent signal registration attempts
            'will not include these signals. Signals are marked for deletion in the
            'ReadInSignalList routine...

            If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") Then
                File.Move(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv", My.Application.Info.DirectoryPath & "\InvalidSignalsLog_" & Format(Now, "MMddyyyy_hhmmss") & ".csv")
            End If

        Catch ex As Exception

            MsgBox("WriteExcelFile: " & ex.Message)

        End Try

    End Sub

    Public Sub WriteMileageToANNOFile(ByVal mySender As String)

        'Called from myBackGroundTasks - main execution loop also called from StartStopRecord (when recording is stopped)...
        'Writes the vehicle mileage for the recording session to the annotation file.

        Dim fnum As Integer
        Dim fnum2 As Integer

        Dim textline() As String = Nothing
        Dim ctr As Integer

        If ANNOFileName Is Nothing Then
            Exit Sub
        End If

        Dim SnapshotTime As Date

        CopyToLog("WriteMileageToANNOFile called by " & mySender)

        SnapshotTime = Now

        'Failsafe - The Annotation file should always have been created at this point, so this should never happen...
        If Not File.Exists(ANNOFileName) Then
            CreateANNOFile()
        End If

        fnum = FreeFile()
        FileOpen(fnum, ANNOFileName, OpenMode.Input)

        ctr = 0
        Do While Not EOF(fnum)
            ReDim Preserve textline(ctr)
            textline(ctr) = LineInput(fnum)
            'If (ctr >= 15 And ctr <= 21) Or ctr = 9 Or ctr = 10 Then
            If (ctr >= 15 And ctr <= 21) Or ctr = 9 Or ctr = 10 Then 'Or ctr = 11 Then
                Select Case ctr

                    Case 9
                        textline(ctr) = "1,7,End Date," & Format(SnapshotTime, "MM/dd/yyyy")
                    Case 10
                        textline(ctr) = "1,8,End Time," & Format(SnapshotTime, "HH:mm:ss")
                        'Case 11
                    '    textline(ctr) = "1,9,Notes," & "Odo Start=" & Format(StartingMileage, "0.0") & " Odo End=" & Format$(EndingMileage, "0.0")
                    Case 15
                        textline(ctr) = "1,14,RecordedMileageOnGrounds," & Format(OnPropertyMileage_Recording, "0.0")
                    Case 16
                        textline(ctr) = "1,15,RecordedMileageOffGrounds," & Format(OffPropertyMileage_Recording, "0.0")
                    Case 17
                        textline(ctr) = "1,16,UnRecordedMileageOnGrounds," & Format(OnPropertyMileage_NotRecording, "0.0")
                    Case 18
                        textline(ctr) = "1,17,UnRecordedMileageOffGrounds," & Format(OffPropertyMileage_NotRecording, "0.0")
                    Case 19
                        textline(ctr) = "1,18,RecordedMileageUnknownLoc," & Format(UnknownMileage_Recording, "0.0")
                    Case 20
                        textline(ctr) = "1,19,UnRecordedMileageUnknownLoc," & Format(UnknownMileage_NotRecording, "0.0")
                    Case 21
                        textline(ctr) = "1,20,LCCActiveMileage," & Format(LCCActiveMileage, "0.0")

                End Select
            End If
            ctr = ctr + 1
        Loop

        FileClose(fnum)

        fnum2 = FreeFile()
        FileOpen(fnum2, ANNOFileName, OpenMode.Output)

        For x = 0 To UBound(textline)
            PrintLine(fnum2, textline(x))
        Next

        FileClose(fnum2)

    End Sub

    Public Sub PositionGridOnForm(ByVal j As Integer)

        'Called by ReadInSignalList during initialization.

        'Positions dynamically created grid on its associated form based on information in the excel configuration spreadsheet

        Dim ctr As Integer
        Dim offset As Integer

        Dim i As Integer

        ctr = -1
        offset = 0

        For i = 0 To UBound(myDGs)
            If myDGs(i).Parent.Name = myDFs(j).Name Then
                ctr = ctr + 1
                If ctr = 0 Then
                    offset = i
                End If
            End If
        Next

        myDFs(j).DefaultWidth = myDFs(j).Width
        myDFs(j).DefaultHeight = myDFs(j).Height

        If myDGs(offset + ctr).Parent.Name <> myDFs(j).Name Then

            i = offset + ctr

            Do While i <= UBound(myDGs)
                i = i + 1
                If myDGs(i).Parent.Name = myDFs(j).Name Then
                    offset = i
                    ctr = 0
                    Exit Do
                End If
            Loop

        End If

        If myDGs(offset + ctr).Parent.Name = myDFs(j).Name Then

            Select Case myDGs(offset + ctr).LocationOnForm
                Case ""
                    WriteToListBox("No Grid Location Specified on form " & myDGs(offset + ctr).Name & ". Defaults will be used for initial position.")
                    myDGs(offset + ctr).Top = GridDataClass.DEFAULT_SEPARATION
                    myDGs(offset + ctr).Left = GridDataClass.DEFAULT_SEPARATION
                Case Else
                    myDGs(offset + ctr).Left = Val(Mid(myDGs(offset + ctr).LocationOnForm, 2, InStr(myDGs(offset + ctr).LocationOnForm, "Y") - 2))
                    myDGs(offset + ctr).Top = Val(Mid(myDGs(offset + ctr).LocationOnForm, InStr(myDGs(offset + ctr).LocationOnForm, "Y") + 1, Len(myDGs(offset + ctr).LocationOnForm)))

            End Select

        Else

            CopyToLog("PositionGridOnForm: What da huh?")

        End If

    End Sub

    Public Sub LogNumberOfSignalsInRing(ByVal signalname As String, Optional ByVal gettotals As String = "")

        'Used during development to keep track of number of signals associated with each ring.  Not really using this
        'info anymore...

        Static TOFRcnt As Integer
        Static LRRRcnt As Integer
        Static VISRcnt As Integer
        Static SRRRcnt As Integer
        Static TSTRcnt As Integer
        Static FSRRcnt As Integer
        Static CPBRcnt As Integer
        Static FCARcnt As Integer
        Static LKARcnt As Integer
        Static VBRRcnt As Integer
        Static MISCcnt As Integer

        'If Len(gettotals) > 0 Then
        'MsgBox("stop")
        'End If

        Select Case Mid(signalname, 3, 4)
            Case "TOFR"
                TOFRcnt = TOFRcnt + 1
            Case "LRRR"
                LRRRcnt = LRRRcnt + 1
            Case "VISR"
                VISRcnt = VISRcnt + 1
            Case "SRRR"
                SRRRcnt = SRRRcnt + 1
            Case "TSTR"
                TSTRcnt = TSTRcnt + 1
            Case "FSRR"
                FSRRcnt = FSRRcnt + 1
            Case "CPBR"
                CPBRcnt = CPBRcnt + 1
            Case "FCAR"
                FCARcnt = FCARcnt + 1
            Case "LKAR"
                LKARcnt = LKARcnt + 1
            Case "VBRR"
                VBRRcnt = VBRRcnt + 1
            Case Else
                MISCcnt = MISCcnt + 1
        End Select

    End Sub


    Public Function ReadInSignalList(ByVal SignalListFileName As String) As Boolean

        'Reads the INCAVariable file.  This file name is defined in the config.txt (or user specific configuration) file.

        'This file contains information about each variable that is defined for display and recording.  Information in this file maps the variable name to a
        'specific Form, Group, Grid, and grid position (row, col).  This allows the user to create a file which defines multiple
        'forms, Groups, and grids in which the variables will be displayed and have the application dynamically create the forms, etc., 
        '"on the fly".

        'This is called on Initialization - it takes the contents of the signal list file and creates displays based on the content of the
        '.csv or xlsx file....

        Dim myTempArray() As String
        Dim InvalidSignalsArray() As String = Nothing

        Dim CameraNumber As Integer = 0

        Dim FileValid As Boolean

        Dim duplicate As Boolean
        Dim SaveSignalIndex As Integer

        Dim DeviceRasterPairFound As Boolean
        Dim found As Boolean
        Dim z As Integer

        Dim i As Integer

        Dim excelApp As Object = Nothing
        Dim wrkbk As Object = Nothing
        Dim myWorkSheet As Object = Nothing

        Dim x As Integer
        Dim y As Integer

        Dim tempstr As String

        Dim fnum As Integer = 0
        Dim fnum2 As Integer
        Dim invalidsignal As Boolean

        Dim AnsweredYes As Boolean

        Static beenhere As Boolean

        Dim INCA_VERSION As String

        Dim TimeCodeCounter As Integer
        Dim NumCamerasToBypass As Integer
        Dim CamerasCounted As Boolean = False

        Dim returnstr As String

        Dim FileExtension As String

        Try

            FileExtension = Path.GetExtension(SignalListFileName)

            ReadInSignalList = True

            CopyToLog("In ReadInSignalList SignalListFileName = " & SignalListFileName)

            'There are several different SignalRegistrationMode(s) each of which affect what happens in this routine.  The user may choose
            'to register only those signals associated with GO/NOGO signals, or all DISPLAY signals, or perform a FULL signal registration
            'which registers every signal in the INCAVariableFile.

            'Register signals for DISPLAY is typical.

            If InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then
                If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") Then

                    If MsgBox("Delete Signals in InvalidSignalsLog from current signal list?", vbYesNo) = vbYes Then
                        AnsweredYes = True
                    End If

                End If
            End If

            tempstr = ""

            'set all DataForms and DataGrids to nothing and start over if we have already created them

            'myDFs is a public array of Data Forms, defined as: Public myDFs(0 To FormDataClass.MAX_NUM_FORMS) As FormDataClass
            'defined in GM_ResidentClient

            'myDGs is a public array of Data Grids, defined as: Public myDGs(0 To FormDataClass.MAX_NUM_FORMS * GridDataClass.MAX_GRIDS_PER_FORM) As GridDataClass
            'defined in Module1

            If beenhere = True Then
                For x = 0 To UBound(myDFs)
                    For z = 0 To UBound(myDGs)
                        myDGs(z) = Nothing
                    Next z
                    myDFs(x) = Nothing
                Next x
            End If

            If PlaybackMode = False Then

                If Len(myINCAInterface.GetCurrentVersion) = 0 Then

                    returnstr = myINCAInterface.ConnectToInca()
                    If returnstr <> "True" Then
                        MsgBox(returnstr)
                        ExitApp()
                    End If

                    If UCase(CLEVIR_Flavor) = "DEVELOPMENT" Then 'And AvailableExperimentNames Is Nothing Then

                        'Here we get all of the experiment names in the current INCA project and store them in a global
                        'array (AvailableExperimentNames) for future use...
                        WriteToListBox("Getting Available Experiment Names...")
                        'Wondering if we can hide this somewhere to decrease init time...
                        AvailableExperimentNames = myINCAInterface.GetAvailableExperimentNames
                        WriteToListBox("Available Experiment Names Retrieved")

                        OnVehicleScreen.Refresh()

                    End If
                End If

            End If

            OnVehicleScreen.TopMost = False

            FileValid = False

            WriteToListBox("Checking SignalList file validity...")

            If System.IO.File.Exists(SignalListFileName) = False Or InStr(SignalListFileName, FileExtension) = 0 Then

                Dim Answer As Integer

                Answer = Cusmsgbox.DisplayCusMsgBox("Signal List referenced in the configuration file cannot be found or is invalid.", "User Input Required", "Exit CLEVIR", "Use Latest Signal List and Experiment", "Select Signal List and Experiment", "")

                Select Case Answer

                    Case 1 'Exit
                        ReadInSignalList = False
                        Exit Function

                    Case 2 'Use Latest

                        Dim FSO2 As Scripting.FileSystemObject
                        Dim f2 As Scripting.Folder
                        Dim sfile2 As Scripting.File

                        Dim SaveDate As Date
                        Dim SaveFileName As String = ""

                        FSO2 = New Scripting.FileSystemObject
                        f2 = FSO2.GetFolder(My.Application.Info.DirectoryPath & "\SignalLists")

                        For Each sfile2 In f2.Files
                            If InStr(sfile2.Name, "SAVE") = 0 And InStr(sfile2.Name, "~") = 0 And InStr(sfile2.Name, FileExtension) > 0 Then

                                If System.IO.File.GetLastWriteTime(sfile2.Path) > SaveDate Then

                                    SaveDate = System.IO.File.GetLastWriteTime(sfile2.Path)
                                    SaveFileName = sfile2.Path

                                End If

                            End If
                        Next

                        SignalListFileName = SaveFileName
                        INCAVariableFile = SignalListFileName
                        OnVehicleScreen.Label5.Text = INCAVariableFile

                        INCAExperiment = Mid(INCAVariableFile, InStrRev(INCAVariableFile, "\") + 1, Len(INCAVariableFile))
                        INCAExperiment = Mid(INCAExperiment, 1, InStr(INCAExperiment, FileExtension) - 1)
                        InitialINCAExperiment = INCAExperiment

                        UserStatusInfo.Label1.Text = "Using Signal List " & INCAExperiment & FileExtension & " and Experiment " & INCAExperiment
                        System.Threading.Thread.Sleep(1000)
                        UserStatusInfo.Hide()


                        If UCase(SaveLoginID) <> "DEMO" Then
                            WriteUserConfigFile(UserConfigFileName)
                        Else
                            WriteConfigFile()
                        End If

                    Case 3 'Select Specific

                        Do While FileValid = False
                            OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath & "\SignalLists"
                            OpenFileDialog1.DefaultExt = FileExtension
                            OpenFileDialog1.Filter = Mid(FileExtension, 2, Len(FileExtension)) & " |*" & FileExtension
                            OpenFileDialog1.Title = "Please Select a Signal Configuration File"
                            OpenFileDialog1.ShowDialog()

                            If Len(OpenFileDialog1.FileName) > 0 And InStr(OpenFileDialog1.FileName, FileExtension) > 0 Then
                                FileValid = True
                                SignalListFileName = OpenFileDialog1.FileName
                                INCAVariableFile = SignalListFileName
                                OnVehicleScreen.Label5.Text = INCAVariableFile

                                SelectExperiment.ListBox1.Items.Clear()

                                For i = 0 To UBound(AvailableExperimentNames)
                                    If InStr(UCase(AvailableExperimentNames(i)), "BLANK EXPERIMENT") = 0 And InStr(UCase(AvailableExperimentNames(i)), "EMPTY EXPERIMENT") = 0 Then
                                        SelectExperiment.ListBox1.Items.Add(AvailableExperimentNames(i))
                                    End If
                                Next

                                SelectExperiment.ShowDialog()

                                If SelectExperiment.ListBox1.SelectedIndex > -1 Then
                                    INCAExperiment = SelectExperiment.ListBox1.SelectedItem.ToString
                                    InitialINCAExperiment = INCAExperiment
                                Else
                                    MsgBox("No Experiment selected, Using " & INCAExperiment)
                                End If

                                UserStatusInfo.Label1.Text = "Using Signal List " & SignalListFileName & " And Experiment " & INCAExperiment
                                System.Threading.Thread.Sleep(1000)
                                UserStatusInfo.Hide()

                                If UCase(SaveLoginID) <> "DEMO" Then
                                    WriteUserConfigFile(UserConfigFileName)
                                Else
                                    WriteConfigFile()
                                End If

                            Else

                                If MsgBox("Invalid signal configuration filename.  Please Select a file With a .csv extension, Try Again?", vbYesNo) = vbNo Then
                                    ReadInSignalList = False
                                    Exit Function
                                End If

                            End If
                        Loop

                End Select

            End If

            WriteToListBox("Creating Signal List backup file...")

            FileCopy(SignalListFileName, SignalListFileName & ".SAVE")

            'create a reference to the Excel app and open the variable file spreadsheet as defined in the
            'configuration file

            WriteToListBox("Reading In Signal/Display Configuration File " & SignalListFileName & "...")

            If FileExtension = ".xlsx" Then

                CopyToLog("ReadInSignalList: Creating Excel Object...")
                excelApp = CreateObject("Excel.Application")
                CopyToLog("ReadInSignalList: Excel Object created.")
                wrkbk = excelApp.Workbooks.Open(SignalListFileName)
                myWorkSheet = wrkbk.Sheets(1)
                myWorkSheet.Activate()
                'set our excel data variable array (exceldata) to the entire used range in the spreadsheet
                exceldata = myWorkSheet.UsedRange.Value

            Else

                fnum = FreeFile()
                FileOpen(fnum, SignalListFileName, OpenMode.Input)

                Dim TempLine() As String  '0 to 22
                Dim TempArray(25000, 22) As String

                y = 0
                Do While Not EOF(fnum)
                    TempLine = Split(LineInput(fnum), ",")
                    'For x = 0 To UBound(TempLine) - 1
                    For x = 0 To 22
                        TempArray(y, x) = TempLine(x)
                    Next
                    y = y + 1
                Loop

                FileClose(fnum)

                ReDim exceldata(y, 23)
                x = 0

                For y = 1 To UBound(exceldata, 1)
                    For x = 0 To 22
                        exceldata(y, x + 1) = TempArray(y - 1, x)
                    Next
                Next y

            End If


            If InStr(SignalRegistrationMode, "FULL") > 0 Then
                If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") Then

                    ReDim ValidExcelData(UBound(exceldata, 1) - 1, EXCEL_DATA.DisplayFormat - 1)

                    For i = 1 To EXCEL_DATA.DisplayFormat
                        ValidExcelData(0, i - 1) = exceldata(1, i)
                    Next i

                End If
            End If

            SaveSignalIndex = -1

            DeviceRasterPairFound = False

            'go through all rows in the spreadsheet (exceldata(y,x)) y is rows, x is columns
            'start with row 2, column 1

            NumCamerasToBypass = 0

            INCA_VERSION = myINCAInterface.GetCurrentVersion

            For y = 2 To UBound(exceldata, 1) 'y is rows

                'DisplayWindowName must not be "" (blank) in the excel spreadsheet, otherwise the signal
                'is considered to be "invisible" and is handled differently....

                'Assumption here is that all DisplayWindowNames with a length of > 0 will be at the top of the spreadsheet, 
                'before any rows with no DisplayWindowName.  If this is not the case, there will be issues down further in this routine....

                If Len(exceldata(y, EXCEL_DATA.DisplayWindowName)) > 0 Then

                    If y = 2 Then 'the first row will have a new form reference so we create first form with index of 0

                        If Mid(exceldata(y, EXCEL_DATA.Raster), Len(exceldata(y, EXCEL_DATA.Raster)) - 1, 2) = "_f" Then
                            exceldata(y, EXCEL_DATA.Raster) = Mid(exceldata(y, EXCEL_DATA.Raster), 1, Len(exceldata(y, EXCEL_DATA.Raster)) - 2) & "-f"
                        End If

                        If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") And InStr(SignalRegistrationMode, "FULL") > 0 Then

                            For i = 1 To EXCEL_DATA.DisplayFormat
                                ValidExcelData(1, i - 1) = exceldata(y, i)
                            Next i

                        End If

                        ReDim myINCAInterface.mySignals(0)

                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).DeviceName = Trim(exceldata(y, EXCEL_DATA.DeviceName))
                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).RasterName = Trim(exceldata(y, EXCEL_DATA.Raster))
                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).SignalName = Trim(exceldata(y, EXCEL_DATA.VariableName))

                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).Status = "Invalid"

                        WriteToListBox("Creating " & exceldata(y, EXCEL_DATA.DisplayWindowName) & " form...")
                        OnVehicleScreen.Refresh()

                        CopyToLog(" ")
                        CopyToLog("ReadInSignalList: Creating Forms...")
                        CreateNewForm(0, exceldata(y, EXCEL_DATA.DisplayWindowName), exceldata(y, EXCEL_DATA.AlsoAssociatedWith), exceldata(y, EXCEL_DATA.DisplayWindowSize), -5000)
                        GridDataClass.InitializeFlexGrids(y, 0, 0)
                        GridDataClass.FormatFlexGrids(y, 0, 0)
                        PositionGridOnForm(0)

                        If (myDGs(0).AlsoAssociatedWith(1, 1) = "GO/NOGO" And
                           UCase(SignalRegistrationMode) = "GO/NOGO") Or
                            UCase(SignalRegistrationMode) = "DISPLAYS" Or
                            UCase(SignalRegistrationMode) = "AUTOANNO" Or
                           InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then
                            myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).ForceRegister = True
                            myDGs(0).Registered(1, 1) = True
                            ProgressBarMax = ProgressBarMax + 1
                        End If

                    Else 'y > 2 (after first row)

                        WriteToListBox("Processing " & exceldata(y, EXCEL_DATA.DisplayWindowName) & " Configuration Info...")

                        If Mid(exceldata(y, EXCEL_DATA.Raster), Len(exceldata(y, EXCEL_DATA.Raster)) - 1, 2) = "_f" Then
                            exceldata(y, EXCEL_DATA.Raster) = Mid(exceldata(y, EXCEL_DATA.Raster), 1, Len(exceldata(y, EXCEL_DATA.Raster)) - 2) & "-f"
                        End If

                        'If we have seen a row of data that does not have a window display name associated with it, and then see one with a window display name
                        'defined, this indicates that the signal list is not valid.  Signal lists must be set up such that all "Displayed" signals are defined
                        'in the file first, before any non-displayed or "Invisible" signals are defined.  So, if myInvisibleSignals is not "Nothing" while we
                        'are still processing "Displayed" signals, this is bad and we will terminate until the issue is fixed.

                        If Not myInvisibleSignals Is Nothing Then
                            MsgBox("Invalid file format, signals With no DisplayWindowName must occur In the spreadsheet after all signals which are associated With a Display Window")
                            WriteToListBox("Invalid file format, signals With no DisplayWindowName must occur In the spreadsheet after all signals which are associated With a Display Window")

                            WriteToListBox("Excel File Read INCOMPLETE!")

                            excelApp.Quit()
                            excelApp = Nothing

                            ReadInSignalList = False
                            Exit Function

                        End If

                        If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") And InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then
                            For i = 1 To EXCEL_DATA.DisplayFormat
                                ValidExcelData(y - 1, i - 1) = exceldata(y, i)
                            Next i
                        End If

                        'Handle number of cameras here
                        '**********************************************************************************************************************************

                        'This VIDEO_TIMECODE related code removes camera VIDEO TIMECODE signals from the final signal list based on the number of cameras
                        'defined in the configuration file (config.txt).  Every signal list will be set up assuming 5 cameras ordered as follows...

                        'Front
                        'Rear
                        'Left
                        'Right
                        'Driver

                        'So, if NumberOfCameras is defined as 2 in the config.txt file, the final three VIDEO_TIMECODE signals for Left, Right and Driver
                        'will be omitted from the list that is processed by CLEVIR.  This impacts the GO/ NOGO display.  If the number of cameras in the 
                        'signal list is greater than the actual number of cameras in the INCA workspace, the GO/NOGO button will always be red (not good).

                        'This code is necessary because some cars have all five cameras, some have just a Front camera, others have a Front and Rear only, 
                        'and others have all cameras except for the Driver camera. Doing it this way allows us to have a universal signal list with all five 
                        'cameras, and have CLEVIR "figure out" how many cameras are actually necessary, therby eliminating the need for different signal 
                        'lists for cars with different numbers of cameras.

                        If exceldata(y, EXCEL_DATA.VariableName) = "VIDEO_TIMECODE" Then

                            TimeCodeCounter = TimeCodeCounter + 1

                            If TimeCodeCounter > NumberOfCameras Then
                                Continue For
                            End If

                        End If

                        '**********************************************************************************************************************************

                        'With each row we process, we add a new myINCAInterface.mySignals structure array member...

                        ReDim Preserve myINCAInterface.mySignals(UBound(myINCAInterface.mySignals) + 1)

                        If exceldata(y, EXCEL_DATA.VariableName) = "VIDEO_TIMECODE" Then
                            myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).DeviceName = CameraNames(CameraNumber)
                            CameraNumber = CameraNumber + 1
                        Else
                            myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).DeviceName = Trim(exceldata(y, EXCEL_DATA.DeviceName))
                        End If

                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).RasterName = Trim(exceldata(y, EXCEL_DATA.Raster))
                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).SignalName = Trim(exceldata(y, EXCEL_DATA.VariableName))

                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).Status = "Invalid"

                        SaveSignalIndex = UBound(myINCAInterface.mySignals)

                        found = False

                        'The AlsoAssociatedWith tag is used to associate custom logic specific to the variable, or simply set up the variable as a GO/NOGO 
                        'variable.  There is also custom code in myBackgroundTasks set up for specific AlsoAssociatedWith text, primarily to allow for custom
                        'user screens.  "CS_" (or Custom Screen) tags in the signal list identify specific signals as having specific uses within the code.
                        'Search AlsoAssociatedWith in myBackgroundTasks routine to see how this works.

                        For z = 0 To UBound(myDFs)
                            If myDFs(z).Name = exceldata(y, EXCEL_DATA.DisplayWindowName) Then
                                found = True
                                If exceldata(y, EXCEL_DATA.AlsoAssociatedWith) = "GO/NOGO" Or InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_") > 0 Or InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "AUTOANNO") > 0 Then
                                    If InStr(myDFs(z).AlsoAssociatedWith, "GO/NOGO") = 0 Then
                                        myDFs(z).AlsoAssociatedWith = exceldata(y, EXCEL_DATA.AlsoAssociatedWith)
                                    End If
                                End If
                                Exit For
                            End If
                        Next

                        'If the myDFs form has not yet been created, we create it.

                        If found = False Then

                            WriteToListBox("Creating " & exceldata(y, EXCEL_DATA.DisplayWindowName) & " form...")
                            OnVehicleScreen.Refresh()

                            CreateNewForm(z, exceldata(y, EXCEL_DATA.DisplayWindowName), exceldata(y, EXCEL_DATA.AlsoAssociatedWith), exceldata(y, EXCEL_DATA.DisplayWindowSize), -5000)

                        End If

                        'Now we loop through all of the Data Forms created, and through all Data Grids created on each form, if we find a match, we
                        'add the signal to the correct Data Grid on the correct Form, if we do not find a match, we create a new Grid and put it on
                        'its form...

                        For j = 0 To UBound(myDFs)

                            found = False
                            For z = 0 To UBound(myDGs)
                                If myDGs(z).Name = exceldata(y, EXCEL_DATA.AssociatedControlName) And
                                   myDFs(j).Name = exceldata(y, EXCEL_DATA.DisplayWindowName) Then

                                    found = True

                                    If myDGs(z).NumberOfRows <= Val(exceldata(y, EXCEL_DATA.Row)) Then
                                        myDGs(z).NumberOfRows = Val(exceldata(y, EXCEL_DATA.Row)) + 1
                                    End If

                                    If myDGs(z).NumberOfColumns <= Val(exceldata(y, EXCEL_DATA.Col)) Then
                                        myDGs(z).NumberOfColumns = Val(exceldata(y, EXCEL_DATA.Col)) + 1
                                    End If

                                    GridDataClass.FormatFlexGrids(y, z, SaveSignalIndex)

                                    Exit For
                                End If
                            Next z

                            If found = False Then
                                If myDFs(j).Name = exceldata(y, EXCEL_DATA.DisplayWindowName) Then

                                    GridDataClass.InitializeFlexGrids(y, j, z) ' y is variable, j is form, z is grid
                                    GridDataClass.FormatFlexGrids(y, z, SaveSignalIndex)

                                    PositionGridOnForm(j)

                                End If
                            End If

                        Next j

                        If ((myDGs(z).AlsoAssociatedWith(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = "GO/NOGO" Or
                            InStr(myDGs(z).AlsoAssociatedWith(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))), "AUTOANNO") > 0 Or
                            InStr(myDGs(z).AlsoAssociatedWith(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))), "CS_") > 0) And
                            UCase(SignalRegistrationMode) = "GO/NOGO") Or
                            InStr(UCase(SignalRegistrationMode), "FULL") > 0 Or
                            UCase(SignalRegistrationMode) = "DISPLAYS" Then
                            myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).ForceRegister = True
                            myDGs(z).Registered(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = True
                            ProgressBarMax = ProgressBarMax + 1

                        Else
                            myDGs(z).Registered(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = False
                        End If

                    End If ' is y > 2 (after first row)?

                Else 'Signal does not have a DisplayWindowName associated with it.  This means
                    'that it is considered to be "invisible" from the point of view of the client
                    'An Invisible signal must still be registered so that it can be added to the
                    'INCA experiment, however, because it must be recorded by INCA to the mdf4 file.

                    'If VB.Right(exceldata(y, EXCEL_DATA.VariableName), 3) = "[x]" Then
                    'MsgBox("Invalid Signal Name - " & exceldata(y, EXCEL_DATA.VariableName))
                    'Continue For
                    'End If


                    If tempstr = "" Then

                        'The first time we encounter a row that does not have a windowdisplayname associated with
                        'it, this indicates that the signal is not going to be displayed.  All signals following this
                        'first one in the file, will be non-displayed signals.  We handle displayed and non-displayed
                        'signals differently,so we save all of the displayed signals so far which is all of the
                        'myINCAInterface.mySignals, to myINCAInterface.myPreliminaryDisplaySignals

                        myINCAInterface.myPreliminaryDisplaySignals = myINCAInterface.mySignals

                        'If we are not going to perform a "FULL" signal registration, we will exit the For loop here, we do
                        'not have to process any of the "Invisible" signals so they do not have to be part of the myINCAInterface.mySignals list

                        If InStr(UCase(SignalRegistrationMode), "FULL") = 0 Then
                            Exit For
                        End If

                        tempstr = "Processing 'Record Only' Signals..."

                        WriteToListBox(tempstr)

                        OnVehicleScreen.Refresh()

                    End If

                    'If we are in FULL registration mode, we need to look through all of the signals to this
                    'point to see if the signal listed in the non-displayed list has already been used in the
                    'displayed list, if so, we set duplicate = true and do not add it to our overall signal list

                    If Not myINCAInterface.mySignals Is Nothing Then

                        If AnsweredYes = True Then
                            'The user answered yes, that they do want to delete the invalid signals in the InvalidSignalsLog.csv file
                            'When we find the signal name that matches the signal name in the invalid signals file, we effectively bypass
                            'it using the code below.

                            If fnum = 0 Then
                                ReDim InvalidSignalsArray(0)
                                fnum = FreeFile()
                                FileOpen(fnum, My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv", OpenMode.Input)
                                Do While Not EOF(fnum)
                                    InvalidSignalsArray(UBound(InvalidSignalsArray)) = LineInput(fnum)
                                    If Not EOF(fnum) Then
                                        ReDim Preserve InvalidSignalsArray(UBound(InvalidSignalsArray) + 1)
                                    End If
                                Loop
                                FileClose(fnum)
                            End If

                            invalidsignal = False

                            For i = 0 To UBound(InvalidSignalsArray)

                                myTempArray = Split(InvalidSignalsArray(i), ",")

                                If myTempArray(0) = exceldata(y, EXCEL_DATA.VariableName) And myTempArray(1) = exceldata(y, EXCEL_DATA.DeviceName) Then
                                    invalidsignal = True
                                    NumInvalid = NumInvalid + 1
                                    GridCellPropConfig.ChangesMade = True

                                    If InStr(myTempArray(3), "DUPLICATE") > 0 Then
                                        InvalidSignalsArray(i) = "REMOVED,,,"
                                    End If
                                    Exit For
                                End If

                            Next i

                            If invalidsignal = False Then
                                For i = 1 To EXCEL_DATA.DisplayFormat
                                    ValidExcelData((y - 1) - NumInvalid, i - 1) = exceldata(y, i)
                                Next i
                            Else
                                'MsgBox("invalid signal found " & exceldata(y, EXCEL_DATA.VariableName))
                            End If

                        End If

                        For x = 0 To UBound(myINCAInterface.mySignals)
                            If exceldata(y, EXCEL_DATA.VariableName) = myINCAInterface.mySignals(x).SignalName And
                               exceldata(y, EXCEL_DATA.DeviceName) = myINCAInterface.mySignals(x).DeviceName Then
                                duplicate = True
                                Exit For
                            End If
                        Next

                        'If it is not a duplicate signal, we create a space for it in our mySignals array

                        If duplicate = False And invalidsignal = False Then
                            ReDim Preserve myINCAInterface.mySignals(UBound(myINCAInterface.mySignals) + 1)
                        End If

                    Else
                        'this should actually never happen, because "invisible" signals must all
                        'be in the excel file after all "visible" signals, this is a file formatting
                        'error.
                        MsgBox("Invalid file format, signals with no DisplayWindowName must occur in the spreadsheet after all signals which are associated with a Display Window")
                        WriteToListBox("Invalid file format, signals with no DisplayWindowName must occur in the spreadsheet after all signals which are associated with a Display Window")

                        WriteToListBox("Configuration File Read INCOMPLETE!")

                        OnVehicleScreen.Refresh()

                        excelApp.Quit()
                        excelApp = Nothing

                        ReadInSignalList = False

                        Exit Function

                    End If

                    If duplicate = False And invalidsignal = False Then

                        'Here we are creating or adding to our myInvisibleSignals array.   This will hold all of
                        'the signals which need to be registered, but not displayed.  myInvisibleSignals is a subset
                        'of the entire signal list, which is in mySignals...

                        If Not myInvisibleSignals Is Nothing Then
                            ReDim Preserve myInvisibleSignals(UBound(myInvisibleSignals) + 1)
                        Else
                            ReDim myInvisibleSignals(0)
                        End If

                        If Mid(exceldata(y, EXCEL_DATA.Raster), Len(exceldata(y, EXCEL_DATA.Raster)) - 1, 2) = "_f" Then
                            exceldata(y, EXCEL_DATA.Raster) = Mid(exceldata(y, EXCEL_DATA.Raster), 1, Len(exceldata(y, EXCEL_DATA.Raster)) - 2) & "-f"
                        End If

                        If Debugger.IsAttached Then

                            If InStr(UCase(BlankExperimentName), "LOWCONTENT") > 0 Then

                                If InStr(exceldata(y, EXCEL_DATA.DeviceName), "CAN-Monitoring") > 0 And InStr(exceldata(y, EXCEL_DATA.Raster), "-f") = 0 Then
                                    exceldata(y, EXCEL_DATA.Raster) = Trim(exceldata(y, EXCEL_DATA.Raster)) & "-f"
                                End If
                            End If

                            'HC CHANGE
                            If InStr(UCase(BlankExperimentName), "HIGHCONTENT") > 0 Then
                                'HC TBD
                            End If

                        End If

                        'myInvisibleSignals is a subset of the entire signal list, which is in mySignals, so here we are
                        'adding to both arrays...
                        myInvisibleSignals(UBound(myInvisibleSignals)).DeviceName = Trim(exceldata(y, EXCEL_DATA.DeviceName))
                        myInvisibleSignals(UBound(myInvisibleSignals)).RasterName = Trim(exceldata(y, EXCEL_DATA.Raster))
                        myInvisibleSignals(UBound(myInvisibleSignals)).SignalName = Trim(exceldata(y, EXCEL_DATA.VariableName))

                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).DeviceName = Trim(exceldata(y, EXCEL_DATA.DeviceName))
                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).RasterName = Trim(exceldata(y, EXCEL_DATA.Raster))
                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).SignalName = Trim(exceldata(y, EXCEL_DATA.VariableName))
                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).Status = "Invalid"

                        'we set the ForceRegister flag here to false for this signal because it is not in the
                        'list of signals that need to be displayed.  We do need to attempt signal registration for
                        'this signal however, if it does not yet exist in the INCA experiment default record list.
                        'This is handled later in the RegisterSignals code...

                        myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).ForceRegister = False

                    Else
                        If duplicate = True Then
                            CopyToLog("ReadInSignalList: Duplicate Signal in Excel File: " & exceldata(y, EXCEL_DATA.VariableName) & "/" & exceldata(y, EXCEL_DATA.DeviceName) & " - " & exceldata(y, EXCEL_DATA.Raster))

                            If AnsweredYes = False Then
                                If fnum2 = 0 Then
                                    fnum2 = FreeFile()
                                    FileOpen(fnum2, My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv", OpenMode.Output)
                                Else
                                    FileOpen(fnum2, My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv", OpenMode.Append)
                                End If

                                PrintLine(fnum2, exceldata(y, EXCEL_DATA.VariableName) & "," & exceldata(y, EXCEL_DATA.DeviceName) & "," & exceldata(y, EXCEL_DATA.Raster) & ",DUPLICATE SIGNAL NAME AND DEVICE")

                                FileClose(fnum2)
                            End If

                            duplicate = False
                        End If
                        If invalidsignal = True Then
                            CopyToLog("ReadInSignalList: Invalid Signal in Excel File: " & exceldata(y, EXCEL_DATA.VariableName) & "/" & exceldata(y, EXCEL_DATA.DeviceName) & " - " & exceldata(y, EXCEL_DATA.Raster))
                            invalidsignal = False
                        End If

                    End If

                End If

            Next y

            For z = 0 To UBound(myDFs)
                If InStr(myDFs(z).AlsoAssociatedWith, "GO/NOGO") > 0 Then
                    gonogocount = gonogocount + 1
                End If

                myExitButtons(z).BringToFront()
            Next

            If Not myDGs(0) Is Nothing Then
                CopyToLog(" ")
                CopyToLog("ReadInSignalList: Finalizing Grid Displays...")
                GridDataClass.FinalizeGridDisplays()
                CopyToLog("ReadInSignalList: Grid Displays Finalized...")
                CopyToLog(" ")

            End If

            If FileExtension = ".xlsx" Then
                excelApp.Quit()
                excelApp = Nothing
            End If


            WriteToListBox("SignalList File Processed successfully.")
            OnVehicleScreen.Refresh()

            beenhere = True

        Catch ex As Exception

            CopyToLog("ReadInSignalList: " & ex.Message & " - y = " & y)
            MsgBox("ReadInSignalList: " & ex.Message & " - y = " & y)
            ReadInSignalList = False
            Me.Cursor = Cursors.Arrow

        End Try

    End Function

    Public Sub myExitButtons_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'This routine is responsible for handling the Exit buttons on each of the
        'display screens that are configured from the excel spreadsheet INCAVariablefile

        'All it does, is close the form on which the button resides...

        'Whenever a new form is created from the configuration information with CreateNewForm,         
        'an exit button is added dynamically and placed on each form.  A "handler" is created
        'in code for each button so that it will allow the user to exit the display screen.

        'The handler is set up in CreateNewForm using the following construct...

        'This means, add a handler routine for the myExitButtons(index).click event, and call it myExitButtons_Click
        'this is where the code will reside to handle the click event of the exit buttons that are dynamically created
        'when creating each display form.

        'AddHandler myExitButtons(index).Click, AddressOf myExitButtons_Click

        sender.parent.close()

    End Sub

    Public Sub mylist_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'This is a handler for the dynamically created mylist, which is created in the InitializationMonitor
        'thread.  This list is used during initialization to display initialization status information
        'The list will remain on the screen after initialization is complete if there are any initialization
        'errors, otherwise it will automatically disappear.

        'If it does not disppear, then clicking in it will set the KeepListDisplayed flag to false and the
        'listbox will disappear.

        'This allows the user, if there is important information in the list, to read it prior to entering
        'the main display window.

        KeepListBoxDisplayed = False

    End Sub
    Public Sub CreateNewForm(ByVal index As Integer, ByVal DisplayWindowName As String, ByVal AlsoAssociatedWith As String, ByVal DisplayWindowSize As String, ByVal FormLocation As Integer)

        'Creates a new form either during the process of reading the excel spreadsheet, or
        'when the user selects the create new display menu option

        ReDim Preserve myDFs(index)
        myDFs(index) = New FormDataClass
        myDFs(index).Text = DisplayWindowName
        myDFs(index).Name = DisplayWindowName

        myDFs(index).ShowInTaskbar = False

        ReDim Preserve myExitButtons(index)
        myExitButtons(index) = New Button


        If AlsoAssociatedWith = "GO/NOGO" Or InStr(AlsoAssociatedWith, "CS_") > 0 Or InStr(AlsoAssociatedWith, "AUTOANNO") > 0 Then
            myDFs(index).AlsoAssociatedWith = AlsoAssociatedWith
        End If

        If DisplayWindowSize = Nothing Then
            DisplayWindowSize = "W500 H500"
        End If
        myDFs(index).DisplayWindowSize = DisplayWindowSize

        myDFs(index).Width = Val(Mid(myDFs(index).DisplayWindowSize, 2, InStr(myDFs(index).DisplayWindowSize, "H") - 2))
        myDFs(index).Height = Val(Mid(myDFs(index).DisplayWindowSize, InStr(myDFs(index).DisplayWindowSize, "H") + 1, Len(myDFs(index).DisplayWindowSize)))

        myExitButtons(index).Parent = myDFs(index)
        myExitButtons(index).Visible = True
        myExitButtons(index).Text = "EXIT"

        myExitButtons(index).Font = New Font(myExitButtons(index).Font.FontFamily, MENU_FONT_SIZE)
        myExitButtons(index).Font = New Font(myExitButtons(index).Font, FontStyle.Bold)

        AddHandler myExitButtons(index).Click, AddressOf myExitButtons_Click

        myExitButtons(index).Width = 100
        myExitButtons(index).Height = 50
        myExitButtons(index).Left = myDFs(index).Width - myExitButtons(index).Width - 20
        myExitButtons(index).Top = 5

        myDFs(index).Show()
        myDFs(index).Left = FormLocation

        InitializeFormContextMenu(myDFs(index))

    End Sub

    Private Sub InitializeFormContextMenu(ByVal myObject As Object)

        'Called from CreateNewForm.

        'Sets up the form context menu...

        Dim ContextMenu_FormHandling As New ToolStripMenuItem("Form Handling")

        myObject.myContextMenuStrip = New ContextMenuStrip

        ContextMenu_FormHandling.DropDownItems.Add("Delete Form")
        AddHandler ContextMenu_FormHandling.DropDownItems(0).Click, AddressOf ContextMenu_FormHandling_Click

        ContextMenu_FormHandling.DropDownItems.Add("Create New Grid")
        AddHandler ContextMenu_FormHandling.DropDownItems(1).Click, AddressOf ContextMenu_FormHandling_Click

        Dim ContextMenu_FormSubMenu As New ToolStripMenuItem("Set GO/NOGO Display Flag")
        ContextMenu_FormSubMenu.CheckOnClick = True

        ContextMenu_FormHandling.DropDownItems.Add(ContextMenu_FormSubMenu)

        AddHandler ContextMenu_FormHandling.DropDownItems(2).Click, AddressOf ContextMenu_FormSubMenu_Click

        ContextMenu_FormHandling.BackColor = Color.White
        ContextMenu_FormHandling.ForeColor = Color.Black
        ContextMenu_FormHandling.Text = "Form Handling"
        ContextMenu_FormHandling.Font = New Font("Georgia", 10)
        ContextMenu_FormHandling.TextAlign = ContentAlignment.BottomRight

        myObject.myContextMenuStrip.Items.Add(ContextMenu_FormHandling)

    End Sub

    Public Sub CreateMenus(ByVal StartFormIdx As Integer, Optional ByVal NewForm As Boolean = False)

        'This handles the dynamic creation of the menus at the top of the GM_ResidentClient and
        'other miscellaneous display items as defined in the INCAVariableFile.
        'It is called after the INCAVariableFile is processed to add all of the
        'menu selections to display each form defined.  

        'Also creates the GO/NOGO banner at the top of the GM_ResidentClient display and the
        'OnVehicleScreen and buttons which are associated with each "GO/NOGO" grid display.

        'Also creates all of the buttons (and handlers for) on the Displays screen which is accessible from the DISPLAYS button
        'at the top of the main screen.

        'This routine is also called when the user creates a new display form.

        Dim gonogo As Integer

        Dim ctr As Integer = -1
        Dim offset As Integer = 0

        Dim z As Integer

        Dim whatrowamion As Integer

        Dim answer As Integer

        Try

            STATUS_LABEL_WIDTH = Int((790 - ((gonogocount + 1) * STATUS_LABEL_SPACING)) \ gonogocount)

            If myToolStripMenuItem Is Nothing Then

                WriteToListBox("Creating Displays Menu")
                OnVehicleScreen.Refresh()

                myMenuStrip = Me.MenuStrip1

                myMenuStrip.Parent = Me

                myLogin = New ToolStripMenuItem("Login", Nothing, Nothing, "Login")

                myLogin.Font = New Font(myLogin.Font.FontFamily, MENU_FONT_SIZE - 2)
                myLogin.Font = New Font(myLogin.Font, FontStyle.Bold)

                myMenuStrip.Items.Add(myLogin)
                AddHandler myLogin.Click, AddressOf myLogin_Click

                If InStr(UCase(CLEVIR_Flavor), "DATALOGGING") > 0 Then
                    myLogin.Enabled = False
                End If

                myToolStripMenuItem = New ToolStripMenuItem("Displays")

                myToolStripMenuItem.Font = New Font(myToolStripMenuItem.Font.FontFamily, MENU_FONT_SIZE - 2)
                myToolStripMenuItem.Font = New Font(myToolStripMenuItem.Font, FontStyle.Bold)

                myMenuStrip.Items.Add(myToolStripMenuItem)

                myToolStripMenuItem.DropDownItems.Add(myTDGraphicsContainer.Name)
                AddHandler myToolStripMenuItem.DropDownItems(0).Click, AddressOf myToolStripMenuItem_Click

                ReDim SelectDisplays.myDisplaySelectButtons(0)
                SelectDisplays.myDisplaySelectButtons(0) = New Button

                'CUSTOM SCREENS


                myToolStripMenuItem.DropDownItems.Add("Secret Squirrel Screen")
                AddHandler myToolStripMenuItem.DropDownItems(1).Click, AddressOf myToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.myDisplaySelectButtons(1)
                SelectDisplays.myDisplaySelectButtons(1) = New Button

                myToolStripMenuItem.DropDownItems.Add("LKA Screen")
                AddHandler myToolStripMenuItem.DropDownItems(2).Click, AddressOf myToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.myDisplaySelectButtons(2)
                SelectDisplays.myDisplaySelectButtons(2) = New Button

                myToolStripMenuItem.DropDownItems.Add("Pedestrian Status Display")
                AddHandler myToolStripMenuItem.DropDownItems(3).Click, AddressOf myToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.myDisplaySelectButtons(3)
                SelectDisplays.myDisplaySelectButtons(3) = New Button

                myToolStripMenuItem.DropDownItems.Add("Fusion Status Display")
                AddHandler myToolStripMenuItem.DropDownItems(4).Click, AddressOf myToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.myDisplaySelectButtons(4)
                SelectDisplays.myDisplaySelectButtons(4) = New Button

                myToolStripMenuItem.DropDownItems.Add("CoPilot Status Display")
                AddHandler myToolStripMenuItem.DropDownItems(5).Click, AddressOf myToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.myDisplaySelectButtons(5)
                SelectDisplays.myDisplaySelectButtons(5) = New Button

                myToolStripMenuItem.DropDownItems.Add("LMFR Global A Status Display")
                AddHandler myToolStripMenuItem.DropDownItems(6).Click, AddressOf myToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.myDisplaySelectButtons(6)
                SelectDisplays.myDisplaySelectButtons(6) = New Button

                myToolStripMenuItem.DropDownItems.Add("LMFR High Content Status Display")
                AddHandler myToolStripMenuItem.DropDownItems(7).Click, AddressOf myToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.myDisplaySelectButtons(7)
                SelectDisplays.myDisplaySelectButtons(7) = New Button

                myToolStripMenuItem.DropDownItems.Add("INCA Hardware Status")
                AddHandler myToolStripMenuItem.DropDownItems(8).Click, AddressOf myToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.myDisplaySelectButtons(8)
                SelectDisplays.myDisplaySelectButtons(8) = New Button

                'Add new custom screens above this...

                myUploadData = New ToolStripMenuItem("Upload Data", Nothing, Nothing, "Upload Data")

                myUploadData.Font = New Font(myUploadData.Font.FontFamily, MENU_FONT_SIZE - 2)
                myUploadData.Font = New Font(myUploadData.Font, FontStyle.Bold)

                myMenuStrip.Items.Add(myUploadData)
                AddHandler myUploadData.Click, AddressOf myUploadData_Click
                'End If


                myRecordPlayback = New ToolStripMenuItem("Record/PlayBack", Nothing, Nothing, "Record/PlayBack")

                myRecordPlayback.Font = New Font(myRecordPlayback.Font.FontFamily, MENU_FONT_SIZE - 2)
                myRecordPlayback.Font = New Font(myRecordPlayback.Font, FontStyle.Bold)

                myMenuStrip.Items.Add(myRecordPlayback)
                AddHandler myRecordPlayback.Click, AddressOf myRecordPlayback_Click

                myMiscInfo = New ToolStripMenuItem("Misc Info", Nothing, Nothing, "Misc Info")

                myMiscInfo.Font = New Font(myMiscInfo.Font.FontFamily, MENU_FONT_SIZE - 2)
                myMiscInfo.Font = New Font(myMiscInfo.Font, FontStyle.Bold)

                myMenuStrip.Items.Add(myMiscInfo)

                myMiscInfo.DropDownItems.Add("Display Operation History")
                AddHandler myMiscInfo.DropDownItems(0).Click, AddressOf myMiscInfo_Click

                myMiscInfo.DropDownItems.Add("Display Record Path/Filename")
                AddHandler myMiscInfo.DropDownItems(1).Click, AddressOf myMiscInfo_Click

                myMiscInfo.DropDownItems.Add("Display Update Rates")
                AddHandler myMiscInfo.DropDownItems(2).Click, AddressOf myMiscInfo_Click

            Else

                myToolStripMenuItem.DropDownItems.RemoveAt(myToolStripMenuItem.DropDownItems.Count - 1)
                myCreateNewDisplayMenuItem = Nothing

            End If

            WriteToListBox("Adding item(s) to Displays Menu...")
            OnVehicleScreen.Refresh()

            If myLabel Is Nothing Then
                gonogo = StartFormIdx
            Else
                gonogo = UBound(myLabel) + 1
            End If

            If Not myDFs(0) Is Nothing Then

                For z = StartFormIdx To UBound(myDFs)

                    myToolStripMenuItem.DropDownItems.Add((myDFs(z).Name))

                    If InStr(myDFs(z).AlsoAssociatedWith, "GO/NOGO") > 0 Then

                        ReDim Preserve myLabel(gonogo)

                        myDFs(z).GoNoGoIndex = gonogo

                        myLabel(gonogo) = New Label

                        myLabel(gonogo).Parent = OnVehicleScreen.GroupBox4

                        myLabel(gonogo).Visible = True
                        myLabel(gonogo).BackColor = Color.Green
                        myLabel(gonogo).ForeColor = Color.White

                        myLabel(gonogo).BorderStyle = BorderStyle.Fixed3D

                        myLabel(gonogo).Width = STATUS_LABEL_WIDTH
                        myLabel(gonogo).Height = STATUS_LABEL_HEIGHT

                        myLabel(gonogo).Top = myLabel(gonogo).Parent.Height - myLabel(gonogo).Height - 5

                        myLabel(gonogo).TextAlign = ContentAlignment.MiddleCenter

                        myLabel(gonogo).Font = New Font(myLabel(gonogo).Font.FontFamily, DEFAULT_FONT_SIZE - 2)

                        If InStr(myDFs(z).Name, "CS_") = 0 Then
                            myLabel(gonogo).Text = myDFs(z).Name
                        Else
                            myLabel(gonogo).Text = Mid(myDFs(z).Name, 4, Len(myDFs(z).Name))
                        End If

                        AddHandler myLabel(gonogo).Click, AddressOf myLabel_Click

                        If gonogo = 0 Then
                            myLabel(gonogo).Left = STATUS_LABEL_SPACING
                        Else
                            myLabel(gonogo).Left = myLabel(gonogo - 1).Left + myLabel(gonogo - 1).Width + STATUS_LABEL_SPACING
                        End If
                        gonogo = gonogo + 1
                    Else
                        myDFs(z).GoNoGoIndex = -1
                    End If

                    'CUSTOM SCREENS,need to add 1 to NUM_PREDEFINED_DISPLAYS, every time we add a new custom screen

                    AddHandler myToolStripMenuItem.DropDownItems(z + NUM_PREDEFINED_DISPLAYS).Click, AddressOf myToolStripMenuItem_Click

                    ReDim Preserve SelectDisplays.myDisplaySelectButtons(z + NUM_PREDEFINED_DISPLAYS)
                    SelectDisplays.myDisplaySelectButtons(z + NUM_PREDEFINED_DISPLAYS) = New Button

                    If NewForm = False Then
                        myDFs(z).Hide()
                    End If

                Next z

            End If

            whatrowamion = -1

            For z = 0 To UBound(SelectDisplays.myDisplaySelectButtons)

                SelectDisplays.myDisplaySelectButtons(z).Parent = SelectDisplays

                SelectDisplays.myDisplaySelectButtons(z).Visible = True
                SelectDisplays.myDisplaySelectButtons(z).Height = 70
                SelectDisplays.myDisplaySelectButtons(z).Width = 100
                SelectDisplays.myDisplaySelectButtons(z).Text = myToolStripMenuItem.DropDownItems(z).Text
                SelectDisplays.myDisplaySelectButtons(z).Font = New Font(SelectDisplays.myDisplaySelectButtons(z).Font.FontFamily, DEFAULT_FONT_SIZE)
                SelectDisplays.myDisplaySelectButtons(z).Font = New Font(SelectDisplays.myDisplaySelectButtons(z).Font, FontStyle.Bold)

                answer = z Mod 7

                If answer = 0 Then
                    whatrowamion = whatrowamion + 1
                End If

                If whatrowamion = 0 Then
                    SelectDisplays.myDisplaySelectButtons(z).Top = 50
                Else
                    SelectDisplays.myDisplaySelectButtons(z).Top = SelectDisplays.myDisplaySelectButtons(0).Top + ((SelectDisplays.myDisplaySelectButtons(0).Height + 5) * whatrowamion)
                End If

                If z Mod 7 <> 0 Then
                    SelectDisplays.myDisplaySelectButtons(z).Left = SelectDisplays.myDisplaySelectButtons(z - 1).Left + SelectDisplays.myDisplaySelectButtons(z - 1).Width + 10
                Else
                    SelectDisplays.myDisplaySelectButtons(z).Left = 10
                End If

                AddHandler SelectDisplays.myDisplaySelectButtons(z).Click, AddressOf SelectDisplays.myDisplaySelectButtons_Click

            Next

            myCreateNewDisplayMenuItem = New ToolStripMenuItem

            myCreateNewDisplayMenuItem.Text = "Create New Display - Enter Display Name"

            myCreateNewDisplayMenuItem.DropDownItems.Add(New ToolStripTextBox())

            myCreateNewDisplayMenuItem.DropDownItems(0).Font = New Font(myCreateNewDisplayMenuItem.DropDownItems(0).Font.FontFamily, MENU_FONT_SIZE)
            myCreateNewDisplayMenuItem.DropDownItems(0).Font = New Font(myCreateNewDisplayMenuItem.DropDownItems(0).Font, FontStyle.Bold)
            myCreateNewDisplayMenuItem.DropDownItems(0).BackColor = Color.LightGray

            myCreateNewDisplayMenuItem.DropDownItems(0).AutoSize = False
            myCreateNewDisplayMenuItem.DropDownItems(0).Width = 400

            myCreateNewDisplayMenuItem.DropDownItems.Add("Click Here after entering Display Name")

            myToolStripMenuItem.DropDownItems.Add(myCreateNewDisplayMenuItem)

            AddHandler myCreateNewDisplayMenuItem.DropDownItems(1).Click, AddressOf myCreateNewDisplayMenuItem_Click

            WriteToListBox("Menu Item(s) Created")
            OnVehicleScreen.Refresh()

        Catch ex As Exception

            CopyToLog("CreateMenus: " & ex.Message)
            MsgBox("CreateMenus: " & ex.Message)

        End Try

    End Sub
    Public Sub myLogin_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'Login selection from top menu bar on GM_ResidentClient and Login button on OnVehicleScreen
        ' - displays the login form

        OnLoginScreen = True

        If SelectDisplays.Visible = True Then
            SelectDisplays.Close()
        End If

        LoginForm.Show()

    End Sub

    Public Sub myRecordPlayback_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'Displays the RecordPlayback form, which contains VCR type controls for recording and playing back
        'data through the CLEVIR interface.

        RecordPlayback.Show()
        RecordPlayback.BringToFront()

    End Sub

    Public Sub myUploadData_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'myUploadData is dynamically created by CreateMenus as is its handler.  This is one of the
        'menu selections in the Configuration Environment on the GM_ResidentClient screen.

        UploadDataScreen.UploadData()

    End Sub
    Public Sub myCreateNewDisplayMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'myCreateNewDisplayMenuItem is dynamically created by CreateMenus as is its handler.

        'Creates a new, blank, user defined form and adds it to the menu for display, as well as
        'allowing the user to add a new grid to the newly created form.

        Dim index As Integer
        Dim x As Integer

        If myINCAInterface.MeasurementStarted = False Then

            If Len(myCreateNewDisplayMenuItem.DropDownItems(0).Text) > 0 Then

                For x = 0 To UBound(myDFs)
                    If UCase(myCreateNewDisplayMenuItem.DropDownItems(0).Text) = UCase(myDFs(x).Name) Then
                        MsgBox("There is already a form with this name, please use a different form name.")
                        Exit Sub
                    End If
                Next x

                index = UBound(myDFs) + 1

                CreateNewForm(index, myCreateNewDisplayMenuItem.DropDownItems(0).Text, "", "W400,H400", 0)

                FormForGridAdd = myCreateNewDisplayMenuItem.DropDownItems(0).Text

                NewGridCreation.myGridTitle = InputBox("Enter Grid Name", "USER INPUT", "undefined")

                For x = 0 To UBound(myDGs)
                    If UCase(myDGs(x).Name) = UCase(NewGridCreation.myGridTitle) Then
                        MsgBox("There is already a grid with this name, please use a different grid name.")
                        Exit Sub
                    End If
                Next x

                NewGridCreation.Show()

                CreateMenus(UBound(myDFs), True)
            Else
                MsgBox("Invalid Display Name, Exiting...")
            End If


        Else
            MsgBox("Measureement must be STOPPED prior to adding a new form.")
        End If


    End Sub


    Public Sub CreateNewGrid(ByVal rows As Integer, ByVal cols As Integer)

        'Creates a new, user defined grid and adds it to the user defined display

        Dim j As Integer
        Dim y As Integer
        Dim z As Integer
        Dim x As Integer


        For j = 0 To UBound(myDGs)
            If UCase(myDGs(j).Name) = UCase(NewGridCreation.GridTitle.Text) Then
                MsgBox("There is already a grid with this name, please use a different grid name.")
                NewGridCreation.GridTitle.Text = ""
                Exit Sub
            End If
        Next j

        Me.Cursor = Cursors.WaitCursor

        For j = 0 To UBound(myDFs)
            If myDFs(j).Name = FormForGridAdd Then
                Exit For
            End If
        Next j

        z = UBound(myDGs) + 1

        GridDataClass.InitializeNewGrid(y, j, z)

        For i = 1 To rows
            For x = 1 To cols
                If myDGs(z).NumberOfRows <= i Then
                    myDGs(z).NumberOfRows = i + 1
                End If

                If myDGs(z).NumberOfColumns <= x Then
                    myDGs(z).NumberOfColumns = x + 1
                End If

            Next
        Next

        'myDGs(z).RowCount = myDGs(z).Rows
        'myDGs(z).ColumnCount = myDGs(z).ColumnCount

        myDGs(UBound(myDGs)).Name = NewGridCreation.GridTitle.Text
        myDGs(UBound(myDGs)).Top = GridDataClass.DEFAULT_SEPARATION

        myDGs(UBound(myDGs)).Width = myDGs(UBound(myDGs)).ColumnCount * GridDataClass.DEFAULT_COL_WIDTH + GridDataClass.DEFAULT_SEPARATION
        myDGs(UBound(myDGs)).Height = myDGs(UBound(myDGs)).RowCount * GridDataClass.DEFAULT_ROW_HEIGHT + GridDataClass.DEFAULT_SEPARATION

        myDGs(UBound(myDGs)).BringToFront()

        InitializeGridCellProperties(rows, cols, myDFs(j).Name, myDGs(UBound(myDGs)))

        GridDataClass.FinalizeGridHeader(myDGs(UBound(myDGs)))

        GridCellPropConfig.ChangesMade = True

        Me.Cursor = Cursors.Arrow


    End Sub

    Public Function GetVehicleNumber() As String

        'Called from HandleWirelessConnection and HandleWhereIsAppRunnning:  Pulls the vehicle number out of
        'the vehicleconfig.txt file.

        Dim fnum As Long
        Dim textline As String
        Dim ctr As Short

        If Len(VehicleNumber) = 0 Then

            If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\" & "vehicleconfig.txt") Then

                fnum = FreeFile()
                FileOpen(fnum, My.Application.Info.DirectoryPath & "\" & "vehicleconfig.txt", OpenMode.Input)

                Do While Not EOF(fnum)
                    textline = LineInput(fnum)
                    Select Case ctr
                        Case 0
                            VehicleNumber = VB.Right(textline, Len(textline) - InStr(textline, Chr(9)))
                            Exit Do
                        Case Else
                            'MsgBox("What da huh?")
                    End Select
                    ctr = ctr + 1
                Loop

                FileClose(fnum)

            Else
                VehicleNumber = "UNDEFINED"
            End If

        End If

        InitForm.Label2.Text = "VEHICLE NUMBER " & VehicleNumber
        GetVehicleNumber = VehicleNumber

    End Function

    Public Sub ContextMenu_FormHandling_Click(ByVal sender As Object, ByVal e As System.EventArgs)

        'This handles the context menu on the form.  The context menu is accessed with a right mouse
        'click, then a particular selection is clicked...

        Dim x As Integer
        Dim y As Integer

        Select Case sender.ToString

            Case "Delete Form"

                If MsgBox("Are you sure you want to delete the " & FormForGridAdd & " form?", vbYesNo) = vbYes Then
                    For x = 0 To UBound(myDFs)
                        If myDFs(x).Name = FormForGridAdd Then
                            GridCellPropConfig.ChangesMade = True
                            myDFs(x).Hide()
                            myDFs(x) = Nothing

                            For y = x To UBound(myDFs) - 1
                                myDFs(y) = myDFs(y + 1)
                            Next y
                            ReDim Preserve myDFs(UBound(myDFs) - 1)

                            Exit For
                        End If
                    Next

                    For x = 0 To myToolStripMenuItem.DropDownItems.Count - 1
                        If myToolStripMenuItem.DropDownItems.Item(x).Text = FormForGridAdd Then
                            myToolStripMenuItem.DropDownItems.RemoveAt(x)
                            Exit For
                        End If
                    Next x
                Else
                    MsgBox(FormForGridAdd & " form, will not be deleted.")
                End If


            Case "Create New Grid"

                If myINCAInterface.MeasurementStarted = False Then

                    NewGridCreation.myGridTitle = InputBox("Enter Grid Name", "USER INPUT", "undefined")

                    For x = 0 To UBound(myDGs)
                        If UCase(myDGs(x).Name) = UCase(NewGridCreation.myGridTitle) Then
                            MsgBox("There is already a grid with this name, please use a different grid name.")
                            Exit Sub
                        End If
                    Next x

                    NewGridCreation.Show()
                Else
                    MsgBox("Measurement must be stopped prior to adding a new grid.")
                    Exit Sub
                End If

        End Select
    End Sub
    Public Sub ContextMenu_FormSubMenu_Click(ByVal sender As Object, ByVal e As System.EventArgs)

        'This handles the "Set GO/NOGO Display Flag" selection in the form context menu.  This is either checked
        'or unchecked.  Indicates if this form is to be handled as a GO/NOGO display form or a standard display form...

        Dim x As Integer
        Dim y As Integer

        Select Case sender.ToString

            Case "Set GO/NOGO Display Flag"

                If sender.checked = True Then
                    MsgBox("This form will now be handled as a GO/NOGO form" & " - " & sender.name)
                Else
                    MsgBox("This form will NOT be handled as a GO/NOGO form")
                End If

                For x = 0 To UBound(myDFs)
                    If FormForGridAdd = myDFs(x).Name Then
                        myDFs(x).AlsoAssociatedWith = IIf(sender.checked = True, "GO/NOGO", "")
                        For y = 0 To UBound(myDGs)
                            If myDGs(y).Parent.Name = myDFs(x).Name Then
                                myDGs(y).AlsoAssociatedWith(1, 1) = myDFs(x).AlsoAssociatedWith
                                Exit For
                            End If
                        Next y
                        GridCellPropConfig.ChangesMade = True
                        Exit For
                    End If
                Next x

            Case "Add New Form to Displays Menu"

                MsgBox("not implemented...")

        End Select

    End Sub

    Sub InitializeGridCellProperties(ByVal rows As Integer, ByVal cols As Integer, ByVal FormName As String, ByVal sender As GridDataClass)

        'Called from CreateNewGrid, initializes the custom grid properties based on the number
        'of rows and columns defined by the user

        Dim row As Integer
        Dim col As Integer

        Dim ccon As ColorConverter
        ccon = New ColorConverter()

        For row = 1 To rows

            For col = 1 To cols

                ReDim Preserve myINCAInterface.mySignals(UBound(myINCAInterface.mySignals) + 1)

                myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).DeviceName = "undefined"
                myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).RasterName = "undefined"
                myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).SignalName = "undefined"
                myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).Status = "Invalid"
                myINCAInterface.mySignals(UBound(myINCAInterface.mySignals)).ForceRegister = True

                'ADDED if condition, 02262015

                If Not myInvisibleSignals Is Nothing Then
                    sender.SignalIndex(row, col) = UBound(myINCAInterface.mySignals) - (UBound(myInvisibleSignals) + 1)
                Else
                    sender.SignalIndex(row, col) = UBound(myINCAInterface.mySignals) - 1
                End If

                'sender.Row = row
                sender.CurrentCell = sender(row, col)

                sender.VariableName(row, col) = "undefined"
                sender.DisplayName(row, col) = "undefined"
                sender.HighThresh(row, col) = 10000000
                sender.LowThresh(row, col) = -10000000
                sender.EqualTo(row, col) = ""
                sender.CheckForDataChange(row, col) = False
                sender.DefaultCellBackColor(row, col) = System.Drawing.Color.White
                sender.DefaultCellForeColor(row, col) = System.Drawing.Color.Black
                sender.HighThreshBackColor(row, col) = System.Drawing.Color.Red
                sender.HighThreshForeColor(row, col) = System.Drawing.Color.White
                sender.LowThreshBackColor(row, col) = System.Drawing.Color.Red
                sender.LowThreshForeColor(row, col) = System.Drawing.Color.White
                sender.DeviceName(row, col) = "undefined"
                sender.Raster(row, col) = "undefined"
                sender.AlsoAssociatedWith(row, col) = ""
                sender.DisplayFormat(row, col) = """0.000"""

                sender.CurrentBackColor(row, col) = sender.DefaultCellBackColor(row, col)

                sender.DataFrozen(row, col) = False

                sender.DataFrozenCounter(row, col) = 0

                'sender.Col = 0

                sender.CurrentCell = sender(row, 0)

                If col = 1 Then
                    sender.Text = sender.DisplayName(row, col)
                    'sender.Row = 0
                    'sender.Col = 1
                    sender.CurrentCell = sender(0, 1)


                    sender.Text = "1"
                End If

                If col > 1 Then
                    'sender.Row = 0
                    'sender.Col = col
                    sender.CurrentCell = sender(0, col)

                    sender.Text = CStr(col)
                End If

            Next col
        Next row

    End Sub

    Public Function GetDeviceStatus() As Boolean

        'Called from Initialize, when Start Measurememnt button is pressed, from CheckForLostDevice when re-initializing and
        'from Handle_5SecondChecks out of GM_ResidentClient.myBackgroundTasks

        'Gets the status of each INCA device.  Each device status should be True, indicating that INCA is connected
        'to the device and it is available to provide data for any signal in a valid device/raster pair.  If the
        'device status is false, that means that INCA could not connect to the device during hardware initialization.

        'Green is good, Red is bad....

        Dim myDevices() As IGM_INCA_Comm.INCADeviceStatus
        Dim x As Integer
        Dim mycolor As Color

        Dim NumCameras As Integer

        Dim NumCommFails As Integer

        Dim IsRed() As Boolean

        Dim ProcessorsFalse As Integer = 0
        Static l_myDeviceStatus As Boolean

        Static SaveStatus() As Boolean

        GetDeviceStatus = True

        TotalNumActiveDevices = 0
        SaveLostDeviceName = ""

        myDevices = myINCAInterface.GetAvailableDevices(True)

        If myDevices Is Nothing Then
            OnVehicleScreen.TopMost = True
            MsgBox("GetDeviceStatus: Communication failure, Exiting CLEVIR and Closing INCA...")
            ExitApp("Complete")
        End If

        ReDim IsRed(UBound(myDevices))
        If SaveStatus Is Nothing Then
            ReDim SaveStatus(UBound(myDevices))
        End If

        For x = 0 To UBound(myDevices)

            If myDevices(x).myStatus = False Then
                mycolor = Color.Red
                IsRed(x) = True

            Else
                mycolor = Color.Green
                TotalNumActiveDevices = TotalNumActiveDevices + 1
                IsRed(x) = False
            End If

            'Here we are setting the background color of each device name displayed in the DeviceStatus window...
            Select Case x
                Case 0
                    DeviceStatus.Label1.Text = myDevices(x).myName
                    DeviceStatus.Label24.Text = myDevices(x).myStatus
                    DeviceStatus.Label24.BackColor = mycolor
                Case 1
                    DeviceStatus.Label2.Text = myDevices(x).myName
                    DeviceStatus.Label23.Text = myDevices(x).myStatus
                    DeviceStatus.Label23.BackColor = mycolor
                Case 2
                    DeviceStatus.Label3.Text = myDevices(x).myName
                    DeviceStatus.Label22.Text = myDevices(x).myStatus
                    DeviceStatus.Label22.BackColor = mycolor
                Case 3
                    DeviceStatus.Label4.Text = myDevices(x).myName
                    DeviceStatus.Label21.Text = myDevices(x).myStatus
                    DeviceStatus.Label21.BackColor = mycolor
                Case 4
                    DeviceStatus.Label5.Text = myDevices(x).myName
                    DeviceStatus.Label20.Text = myDevices(x).myStatus
                    DeviceStatus.Label20.BackColor = mycolor
                Case 5
                    DeviceStatus.Label6.Text = myDevices(x).myName
                    DeviceStatus.Label19.Text = myDevices(x).myStatus
                    DeviceStatus.Label19.BackColor = mycolor
                Case 6
                    DeviceStatus.Label7.Text = myDevices(x).myName
                    DeviceStatus.Label18.Text = myDevices(x).myStatus
                    DeviceStatus.Label18.BackColor = mycolor
                Case 7
                    DeviceStatus.Label8.Text = myDevices(x).myName
                    DeviceStatus.Label17.Text = myDevices(x).myStatus
                    DeviceStatus.Label17.BackColor = mycolor
                Case 8
                    DeviceStatus.Label9.Text = myDevices(x).myName
                    DeviceStatus.Label16.Text = myDevices(x).myStatus
                    DeviceStatus.Label16.BackColor = mycolor
                Case 9
                    DeviceStatus.Label10.Text = myDevices(x).myName
                    DeviceStatus.Label15.Text = myDevices(x).myStatus
                    DeviceStatus.Label15.BackColor = mycolor
                Case 10
                    DeviceStatus.Label11.Text = myDevices(x).myName
                    DeviceStatus.Label14.Text = myDevices(x).myStatus
                    DeviceStatus.Label14.BackColor = mycolor
                Case 11
                    DeviceStatus.Label12.Text = myDevices(x).myName
                    DeviceStatus.Label13.Text = myDevices(x).myStatus
                    DeviceStatus.Label13.BackColor = mycolor
                Case 12
                    DeviceStatus.Label26.Text = myDevices(x).myName
                    DeviceStatus.Label25.Text = myDevices(x).myStatus
                    DeviceStatus.Label25.BackColor = mycolor
                Case 13
                    DeviceStatus.Label28.Text = myDevices(x).myName
                    DeviceStatus.Label27.Text = myDevices(x).myStatus
                    DeviceStatus.Label27.BackColor = mycolor
                Case 14
                    DeviceStatus.Label30.Text = myDevices(x).myName
                    DeviceStatus.Label29.Text = myDevices(x).myStatus
                    DeviceStatus.Label29.BackColor = mycolor
                Case 15
                    DeviceStatus.Label32.Text = myDevices(x).myName
                    DeviceStatus.Label31.Text = myDevices(x).myStatus
                    DeviceStatus.Label31.BackColor = mycolor
            End Select

            'Check for processor faults.  Processors are aliased with different names based on the ProjectName
            'Here we count the number of processors in the device list that return false.  Currently, if any
            'processor returns false (ProcessorsFalse > 0) we will trigger a PROCESSOR COMMUNICATION ALERT! message
            'by setting the global variable myDeviceStatus to False.  This variable is checked in ProcessKiller thread.
            'See GM_ResidentClient.ProcessKiller to see how this is handled...
            If myDevices(x).myName = "HCF" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "HCS" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "XETK:1" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                SaveLostDeviceName = myDevices(x).myName
            End If

            If myDevices(x).myName = "IP" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "K1P" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "K2P" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "IR" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "K1R" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "K2R" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "IC" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "K1C" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

            If myDevices(x).myName = "K2C" And myDevices(x).myStatus = False Then
                ProcessorsFalse = ProcessorsFalse + 1
                If Len(SaveLostDeviceName) > 0 Then
                    SaveLostDeviceName = SaveLostDeviceName & "," & myDevices(x).myName
                Else
                    SaveLostDeviceName = myDevices(x).myName
                End If
            End If

        Next

        'GetDeviceStatus behaves differently depending on whether or not we are initializing...
        'If we are up and running after initialization, we will set myDeviceStatus to False
        'if we have at least one processor status return false...
        If Initializing = False Then
            For x = 0 To UBound(myDevices)
                If SaveStatus(x) <> IsRed(x) Then
                    If IsRed(x) = True Then
                        CopyToLog("GetDeviceStatus: Cannot Communicate with " & myDevices(x).myName)
                    ElseIf IsRed(x) = False Then
                        CopyToLog("GetDeviceStatus: Communication with " & myDevices(x).myName & " established")
                    End If
                End If
            Next


            If ProcessorsFalse > 0 And IgnoreLostDeviceForThisDrive = False Then
                l_myDeviceStatus = False
            End If

            If ProcessorsFalse = 0 Or OverrideCommFailureInDebugMode = True Then
                l_myDeviceStatus = True
            End If

            SaveStatus = IsRed

        Else 'If we are initializing, we will alert the user to a false status for any device and allow the user to decide
            'whether or not to continue with initialization...

            l_myDeviceStatus = True
            For x = 0 To UBound(myDevices)
                If IsRed(x) = True Then

                    NumCommFails = NumCommFails + 1

                    CopyToLog("GetDeviceStatus: Cannot Communicate with " & myDevices(x).myName & " at Initialization...")

                    If OperatingMode = OperatingModes.RES_ON_VPC Or OperatingMode = OperatingModes.RES_ON_SUITCASE_VPC Or OperatingMode = OperatingModes.RES_ON_LAPTOP_VPC Then

                        If InStr(SignalRegistrationMode, "FULL") = 0 And InStr(UCase(myDevices(x).myName), "CALCDEV") = 0 Then

                            If Debugger.IsAttached = False Then
                                OnVehicleScreen.TopMost = True

                                If MsgBox("Cannot Communicate with " & myDevices(x).myName & ", Continue Initialization?", vbYesNo) = vbNo Then
                                    CopyToLog("GetDeviceStatus: User chose not to continue with initialization.")
                                    GetDeviceStatus = False
                                    'Exit for
                                    Exit Function
                                Else
                                    CopyToLog("GetDeviceStatus: User chose to continue with initialization.")
                                End If
                                OnVehicleScreen.TopMost = False
                            End If

                        End If
                    End If

                ElseIf IsRed(x) = False Then
                    'CopyToLog("Communication with " & myDevices(x).myName & " established")
                End If

                If InStr(UCase(myDevices(x).myDeviceType), "VIDEO") > 0 Then
                    NumCameras = NumCameras + 1
                    CopyToLog("GetDeviceStatus: Number of Cameras in workspace = " & NumCameras)
                End If

            Next

            If NumCommFails >= (NumControllers + 1) And NumCameras <> NumberOfCameras Then

                If InStr(SignalRegistrationMode, "FULL") = 0 Then

                    If DebugMode = False Then
                        CopyToLog("GetDeviceStatus: Number of communication failures = " & NumCommFails)
                        OnVehicleScreen.TopMost = True
                        MsgBox("MULTIPLE COMMUNUCATION FAILURES ENCOUNTERED.  THE INCA CAMERA CONFIGURATION IS ALSO INCORRECT. PLEASE VERIFY THAT THE INCA WORKSPACE BEING USED IS CONSISTENT WITH THE SETUP OF THIS VEHICLE! Exiting CLEVIR.")
                        OnVehicleScreen.TopMost = False
                        GetDeviceStatus = False
                        Exit Function
                    Else

                        CopyToLog("GetDeviceStatus: Number of communication failures = " & NumCommFails)

                        If Debugger.IsAttached = False Then

                            OnVehicleScreen.TopMost = True
                            MsgBox("MULTIPLE COMMUNUCATION FAILURES ENCOUNTERED.  THE INCA CAMERA CONFIGURATION IS ALSO INCORRECT. PLEASE VERIFY THAT THE INCA WORKSPACE BEING USED IS CONSISTENT WITH THE SETUP OF THIS VEHICLE!")
                            OnVehicleScreen.TopMost = False

                        End If

                    End If

                End If


            End If

            If NumCameras < NumberOfCameras Then
                CopyToLog("GetDeviceStatus: Number of cameras does not match at Initialization...")

                If InStr(SignalRegistrationMode, "FULL") = 0 Then

                    If Debugger.IsAttached = False Then
                        OnVehicleScreen.TopMost = True
                        If MsgBox("Number of Cameras defined in Workspace is less than the number of cameras used in this vehicle, Continue Initialization?", vbYesNo) = vbNo Then

                            CopyToLog("GetDeviceStatus: User chose not to continue with initialization.")
                            GetDeviceStatus = False

                        Else
                            CopyToLog("GetDeviceStatus: User chose to continue with initialization.")
                        End If
                        OnVehicleScreen.TopMost = False
                    End If

                End If

            ElseIf NumCameras > NumberOfCameras Then

                CopyToLog("GetDeviceStatus: Number of cameras does not match at Initialization...")

                If InStr(SignalRegistrationMode, "FULL") = 0 Then

                    If Debugger.IsAttached = False Then

                        OnVehicleScreen.TopMost = True
                        If MsgBox("Number of Cameras defined in Workspace is Greater than the number of cameras used in this vehicle, Continue Initialization?", vbYesNo) = vbNo Then

                            CopyToLog("GetDeviceStatus: User chose not to continue with initialization.")
                            GetDeviceStatus = False

                        Else
                            CopyToLog("GetDeviceStatus: User chose to continue with initialization.")
                        End If
                        OnVehicleScreen.TopMost = False

                    End If

                End If

            End If

        End If

        'Set the global variable myDeviceStatus here - This variable is monitored in a separate thread, see GM_ResidentClient.ProcessKiller...
        myDeviceStatus = l_myDeviceStatus

    End Function
    Public Sub myMiscInfo_Click(ByVal sender As ToolStripMenuItem, ByVal e As System.EventArgs)

        'myMiscInfo is created in CreateMenus, as is the myMiscInfo_Click handler.

        'The myMiscInfo object represents the selections that can be made from the Misc Info drop
        'down menu selection.

        If sender.ToString = "Display Operation History" Then
            OperationHistory.Show()
            OperationHistory.BringToFront()
        End If

        If sender.ToString = "Display Record Path/Filename" Then
            If Len(Label3.Text) > 0 Then
                MsgBox(Label3.Text & Label4.Text)
            Else
                MsgBox("No Record Path/Filename defined, please select a Login ID")
            End If
        End If

        If sender.ToString = "Display Update Rates" Then
            MsgBox("Display Refresh Rate = " & DisplayRefreshRate & " Data Collection Rate = " & DataCollectionRate & " INCA Polling Rate = " & myINCAInterface.GetINCAPollingRate)
        End If

    End Sub
    Public Sub myToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) ' Handles myToolStripMenuItem1.Click

        'This sub is called when the user selects a display from the displays menu.  All information about the forms, groups, 
        'and grids variable mapping to grid position, etc. is obtained at startup from a spreadsheet which is read in using 
        'the(ReadInSignalList)which is called at startup from the Initialize sub.

        'This sub simply displays the correct form based on who the "sender" is.  This is only applicable
        'to GM_ResidentClient drop down menu.  The form display is handled differently on the OnVehicleScreen
        'form

        Dim j As Integer

        'look through all forms and find the form with the name corrsponding to the sender (which form was selected)
        If Not myDFs(0) Is Nothing Then

            For j = 0 To UBound(myDFs)
                If myDFs(j).Text = sender.ToString Then

                    If myDFs(j).Visible = False Then
                        myDFs(j).Left = Me.Left
                        myDFs(j).Top = Me.Top
                    End If

                    myDFs(j).Show()
                    myDFs(j).BringToFront()
                    Exit Sub
                End If 'myFormData(j).myForm.Text <> sender.ToString

            Next j

        End If


        '**********************************************

        'CUSTOM SCREEN

        ShowCustomScreen(sender.ToString)

    End Sub

    Public Sub RegisterDisplaySignals(ByVal j As Integer)

        'Called from various click events which are associated with displaying a particular 
        'form which has been dynamically created based on information in the INCAVariableFile.

        'This routine will register the signals on a given form dynamically.
        'This will occur if the user has selected the GO/NOGO option on initialization, because the signals
        'associated with the display have not been registered until it has been displayed for the first time.

        Dim found As Boolean
        Dim e As System.EventArgs
        Dim z As Integer
        Dim x As Integer
        Dim row As Integer
        Dim col As Integer
        Dim reply As MsgBoxResult
        Dim SaveBackColor As System.Drawing.Color

        Dim SignalsRegistered As Boolean
        Dim FormName As String


        e = System.EventArgs.Empty
        reply = Nothing

        'need to handle top down view differently than other forms....

        If j <= UBound(myDFs) Then
            SignalsRegistered = myDFs(j).SignalsRegistered
            FormName = myDFs(j).Text
        Else 'this is top down view case...
            SignalsRegistered = TopDownSignalsRegistered
            FormName = "Top Down View"
        End If

        If SignalsRegistered = False And UCase(SignalRegistrationMode) = "GO/NOGO" Then

            If myINCAInterface.GetRecordingState = True Then
                reply = MsgBox("Signals are not yet registered for " & FormName & " To view this display, signals must be registered, which will require recording to be STOPPED. Stop Recording and Continue?", vbYesNo)
            ElseIf myINCAInterface.GetMeasurementStatus = "True" Then
                reply = MsgBox("Signals are not yet registered for " & FormName & " To view this display, signals must be registered, which will require Measurement to be STOPPED. Stop Measurement and Continue?", vbYesNo)
            Else
                MsgBox("Signals are not yet registered for " & FormName & " Please Wait while signals are registered...")

            End If

            If reply = MsgBoxResult.No Then
                Exit Sub
            End If

            If OnVehicleScreen.Visible = True Then
                OnVehicleScreen.Label5.Visible = True
                SaveBackColor = OnVehicleScreen.Label5.BackColor
                OnVehicleScreen.Label5.BackColor = Color.Yellow
                OnVehicleScreen.Label5.Text = "Operation In Progress, Please Wait..."
                OnVehicleScreen.Refresh()
            End If

            If reply = MsgBoxResult.Yes Then

                If OnVehicleScreen.Visible = True Then
                    OnVehicleScreen.Cursor = Cursors.WaitCursor
                    OnVehicleScreen.Button6.Text = "STOP MEASUREMENT"
                    myINCAInterface.StartStopMeasurement(OnVehicleScreen.Button6)

                Else
                    Me.Cursor = Cursors.WaitCursor
                End If

                Do While myINCAInterface.GetMeasurementStatus = "True"
                    System.Threading.Thread.Sleep(100)
                Loop

            End If

            For z = 0 To UBound(myDGs)
                If (myDGs(z).Parent.Name = FormName) Or FormName = "Top Down View" Then
                    found = True
                End If

                If found = True Then
                    found = False
                    For row = 1 To myDGs(z).RowCount - 1
                        For col = 1 To myDGs(z).ColumnCount - 1

                            For x = 0 To UBound(myINCAInterface.myDisplaySignals)
                                If ((myDGs(z).AlsoAssociatedWith(row, col) <> "GO/NOGO" And
                                    myDGs(z).AlsoAssociatedWith(row, col) <> "AUTOANNO" And
                                   InStr(myDGs(z).AlsoAssociatedWith(row, col), "CS_") = 0 And
                                   FormName <> "Top Down View") Or
                                   (FormName = "Top Down View" And InStr(myDGs(z).AlsoAssociatedWith(row, col), "TD") > 0)) And
                                    (myINCAInterface.myDisplaySignals(x).DeviceName = myDGs(z).DeviceName(row, col) And
                                    myINCAInterface.myDisplaySignals(x).SignalName = myDGs(z).VariableName(row, col) And
                                    myINCAInterface.myDisplaySignals(x).RasterName = myDGs(z).Raster(row, col)) Then
                                    myINCAInterface.myDisplaySignals(x).ForceRegister = True
                                    myDGs(z).Registered(row, col) = True
                                    Exit For
                                End If
                            Next x

                        Next col
                    Next row
                End If

            Next z

            myINCAInterface.myPreliminaryDisplaySignals = myINCAInterface.myDisplaySignals

            myINCAInterface.RegisterSignals()

            SignalsRegistered = True

            If OnVehicleScreen.Visible = True Then
                myINCAInterface.StartStopMeasurement(OnVehicleScreen.Button6)
            End If

            Cursor = Cursors.Arrow

            OnVehicleScreen.Label5.BackColor = SaveBackColor

            Dim tempstr As String

            tempstr = myINCAInterface.GetLastRecordingFileName

            If Len(tempstr) = 0 Then
                tempstr = myINCAInterface.GetRecordingFileName
                tempstr = Mid(tempstr, 1, InStr(tempstr, ".") - 1) & "01.mf4"
                OnVehicleScreen.Label5.Text = "Recording Filename: " & tempstr
                SaveRecordingFileName = tempstr
                'AnnoFileWritten = False
            Else
                tempstr = Mid(tempstr, 1, InStr(tempstr, ".") - 3) & Format(Val(Mid(tempstr, InStr(tempstr, ".") - 2, 2)) + 1, "00") & ".mf4"
                tempstr = Mid(tempstr, InStrRev(tempstr, "\") + 1, Len(tempstr))

                OnVehicleScreen.Label5.Text = "Recording Filename: " & tempstr
                SaveRecordingFileName = tempstr
            End If

            OnVehicleScreen.Refresh()

            If j <= UBound(myDFs) Then
                myDFs(j).SignalsRegistered = SignalsRegistered
            Else 'this is top down view case...
                TopDownSignalsRegistered = SignalsRegistered
            End If

        End If

    End Sub

    Private Sub myLabel_Click(ByVal sender As Object, ByVal e As System.EventArgs)

        'Click event for the GO/NOGO labels, displays the GO/NOGO form associated with the label

        'Dim thisLabel As Label = DirectCast(sender, Label)

        Dim CompareString As String

        e = System.EventArgs.Empty

        For j = 0 To UBound(myDFs)

            If InStr(myDFs(j).Text, "CS_") = 0 Then
                CompareString = myDFs(j).Text
            Else
                CompareString = Mid(myDFs(j).Text, 4, Len(myDFs(j).Text))
            End If

            If InStr(sender.ToString, CompareString) > 0 Then

                If InStr(myDFs(j).Text, "CS_") = 0 Then

                    RegisterDisplaySignals(j)

                    If myDFs(j).Visible = False Then

                        myDFs(j).Left = 0
                        myDFs(j).Top = 30

                        myDFs(j).Show()
                        myDFs(j).BringToFront()
                        myDFs(j).Activate()

                        myDFs(j).ShowInTaskbar = True

                    Else
                        myDFs(j).Show()
                        myDFs(j).BringToFront()
                        myDFs(j).Activate()

                    End If

                Else

                    RegisterDisplaySignals(j)

                    'CompareString

                    Select Case myDFs(j).Text
                        Case "CS_CoPilot Status"

                            CopilotStatusDisplay.Left = 0
                            CopilotStatusDisplay.Top = 45

                            CopilotStatusDisplay.Show()
                            CopilotStatusDisplay.BringToFront()
                            CopilotStatusDisplay.Activate()

                    End Select

                End If

            End If 'myFormData(j).myForm.Text <> sender.ToString

        Next j
    End Sub

    Public Sub mySubTabControl_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)

        'Clicking on a sub-tab in the annotation button interface part of the main display screen will either
        'change display based on tab selected (left mouse click) or will display the AnnotationInterfaceConfigure form
        '(right mouse click)...

        If e.Button = MouseButtons.Right Then

            AnnotationInterfaceConfigure.Show()
            AnnotationInterfaceConfigure.BringToFront()

            AnnotationInterfaceConfigure.ListBox2.Visible = True
            AnnotationInterfaceConfigure.Button2.Visible = True

            For x = 0 To AnnotationInterfaceConfigure.ListBox2.Items.Count - 1
                If AnnotationInterfaceConfigure.ListBox2.Items(x).ToString = mySubTabControl.TabPages(mySubTabControl.SelectedIndex).Text Then
                    AnnotationInterfaceConfigure.ListBox2.SetSelected(x, True)
                End If
            Next


        Else

            WhichTabAmIOn = mySubTabControl.SelectedIndex

        End If

    End Sub

    Public Sub myMainTabControl_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)

        'Clicking on a main-tab in the annotation button interface part of the main display screen will either
        'change display based on tab selected (left mouse click) or will display the AnnotationInterfaceConfigure form
        '(right mouse click)...

        If e.Button = MouseButtons.Right Then

            AnnotationInterfaceConfigure.Show()
            AnnotationInterfaceConfigure.BringToFront()

            AnnotationInterfaceConfigure.ListBox2.Visible = False
            AnnotationInterfaceConfigure.Button2.Visible = False
            AnnotationInterfaceConfigure.Label2.Visible = False

            For x = 0 To AnnotationInterfaceConfigure.ListBox1.Items.Count - 1
                If AnnotationInterfaceConfigure.ListBox1.Items(x).ToString = myMainTabControl.TabPages(myMainTabControl.SelectedIndex).Text Then
                    AnnotationInterfaceConfigure.ListBox1.SetSelected(x, True)
                End If
            Next

        End If

    End Sub

    Sub SizeAnnotationButtons()

        'Called from parsedatadictionary...

        'Sizes and positions the annotation buttons for display on the main CLEVIR screen...

        Dim x As Integer
        Dim Y As Integer
        Dim z As Integer
        Dim i As Integer
        Dim j As Integer
        Dim k As Integer

        Dim SaveSubTabIndex As Integer

        Dim buttonrows As Integer
        Dim buttoncount As Integer
        Dim buttonheight As Integer

        Dim ButtonHeightMultiplier As Double
        Dim ButtonWidthMultiplier As Double

        Try

            WriteToListBox("Configuring Annotation Button Interface...")
            OnVehicleScreen.Refresh()

            AnnotationInterfaceConfigure.ListBox1.Items.Clear()
            AnnotationInterfaceConfigure.ListBox2.Items.Clear()

            If NewMainTabWidth = 0 Then

                mySubTabControl = Nothing

                myMainTabControl.ItemSize = New Size(180, 50)

                myMainTabControl.Multiline = True

                myMainTabControl.BackColor = Color.WhiteSmoke

                myMainTabControl.Top = MAIN_TAB_TOP
                myMainTabControl.Left = MAIN_TAB_LEFT

                myMainTabControl.Width = MAIN_TAB_WIDTH
                myMainTabControl.Height = MAIN_TAB_HEIGHT

                myMainTabControl.Font = New Font(myMainTabControl.Font.FontFamily, DEFAULT_FONT_SIZE)
                myMainTabControl.Font = New Font(myMainTabControl.Font, FontStyle.Bold)

                AddHandler myMainTabControl.MouseDown, AddressOf Me.myMainTabControl_MouseDown

                myMainTabControl.Visible = True

            Else

                SaveSubTabIndex = mySubTabControl.SelectedIndex

                For i = 1 To myMainTabControl.TabPages.Count - 1

                    For count = 0 To myMainTabControl.TabPages(i).Controls.Count - 1
                        myMainTabControl.TabPages(i).Controls.Remove(ButtonContainers(i + 8).Buttons(count))
                    Next count
                Next i

                mySubTabControl.Visible = False
                mySubTabControl = Nothing

                myMainTabControl.Width = NewMainTabWidth
                myMainTabControl.Height = NewMainTabHeight

                myMainTabControl.Refresh()
            End If

            For x = 0 To UBound(AnnotationTypeRecord)
                If AnnotationTypeRecord(x).ID = 1000 Then 'these will be the subtab buttons

                    AnnotationInterfaceConfigure.ListBox1.Items.Add(AnnotationTypeRecord(x).Description)

                    For Y = 0 To UBound(AnnotationValueRecord)

                        If AnnotationTypeRecord(x).ID = AnnotationValueRecord(Y).TypeID Then
                            If InStr(AnnotationValueRecord(Y).Description, "AA") = 0 And InStr(AnnotationValueRecord(Y).Description, "DELETE") = 0 And AnnotationValueRecord(Y).ID > 3140 Then

                                AnnotationInterfaceConfigure.ListBox2.Items.Add(AnnotationValueRecord(Y).Description)

                                If mySubTabControl Is Nothing Then
                                    mySubTabControl = New TabControl

                                    mySubTabControl.BackColor = Color.WhiteSmoke

                                    For i = 0 To myMainTabControl.TabCount - 1
                                        If myMainTabControl.TabPages(i).Text = AnnotationTypeRecord(x).Description Then
                                            mySubTabControl.Parent = myMainTabControl.TabPages(i)
                                            Exit For
                                        End If
                                    Next i

                                    mySubTabControl.ItemSize = New Size(185, 50)

                                    mySubTabControl.Multiline = True

                                    mySubTabControl.Top = SUB_TAB_TOP
                                    mySubTabControl.Left = SUB_TAB_LEFT

                                    If NewSubTabWidth = 0 Then
                                        mySubTabControl.Width = SUB_TAB_WIDTH
                                        mySubTabControl.Height = SUB_TAB_HEIGHT  '380
                                    Else
                                        mySubTabControl.Width = NewSubTabWidth
                                        mySubTabControl.Height = NewSubTabHeight  '380
                                    End If

                                    'mySubTabControl.Font = New Font(mySubTabControl.Font.FontFamily, 18)
                                    mySubTabControl.Font = New Font(mySubTabControl.Font.FontFamily, SUB_TAB_FONT_SIZE) '(now = 16 with addition of new LCoD tab)
                                    mySubTabControl.Font = New Font(mySubTabControl.Font, FontStyle.Bold)

                                    mySubTabControl.Visible = True

                                    mySubTabControl.TabPages.Add(z)
                                    mySubTabControl.TabPages(z).Text = AnnotationValueRecord(Y).Description

                                    AddHandler mySubTabControl.MouseDown, AddressOf Me.mySubTabControl_MouseDown

                                    ReDim ButtonContainers(k)
                                    ReDim ButtonContainers(k).Buttons(0)

                                    ButtonContainers(k).Parent = mySubTabControl
                                    ButtonContainers(k).ContainerName = mySubTabControl.TabPages(z).Text

                                Else
                                    mySubTabControl.TabPages.Add(z)
                                    mySubTabControl.TabPages(z).Text = AnnotationValueRecord(Y).Description

                                    ReDim Preserve ButtonContainers(k)
                                    ReDim Preserve ButtonContainers(k).Buttons(0)

                                    ButtonContainers(k).Parent = mySubTabControl
                                    ButtonContainers(k).ContainerName = mySubTabControl.TabPages(z).Text

                                End If

                                For i = 0 To UBound(EnumerationTypeRecord)
                                    If EnumerationTypeRecord(i).ID = AnnotationValueRecord(Y).EnumerationType Then

                                        '*****************************************************

                                        ReDim Preserve ButtonContainers(k).Buttons(0)
                                        ReDim Preserve ButtonContainers(k).ButtonContainerHotKey(0)

                                        ButtonContainers(k).Buttons(0) = New Button
                                        ButtonContainers(k).Buttons(0).Parent = mySubTabControl.TabPages(z)

                                        ButtonContainers(k).ButtonContainerHotKey(0) = New Label
                                        ButtonContainers(k).ButtonContainerHotKey(0).Parent = mySubTabControl.TabPages(z)

                                        ButtonContainers(k).ButtonContainerHotKey(0).Text = "C"
                                        ButtonContainers(k).ButtonContainerHotKey(0).AutoSize = True
                                        ButtonContainers(k).ButtonContainerHotKey(0).BorderStyle = BorderStyle.Fixed3D
                                        ButtonContainers(k).ButtonContainerHotKey(0).Visible = True
                                        ButtonContainers(k).ButtonContainerHotKey(0).Top = ButtonContainers(k).Buttons(0).Top
                                        ButtonContainers(k).ButtonContainerHotKey(0).Left = ButtonContainers(k).Buttons(0).Left

                                        ButtonContainers(k).ButtonContainerHotKey(0).Font = New Font(ButtonContainers(k).ButtonContainerHotKey(0).Font.FontFamily, MENU_FONT_SIZE - 6)
                                        ButtonContainers(k).ButtonContainerHotKey(0).Font = New Font(ButtonContainers(k).ButtonContainerHotKey(0).Font, FontStyle.Bold)

                                        ButtonContainers(k).ButtonContainerHotKey(0).BringToFront()

                                        ButtonContainers(k).ButtonContainerHotKey(0).Parent = ButtonContainers(k).Buttons(0)

                                        ButtonContainers(k).Buttons(0).Top = DEFAULT_BUTTON_TOP
                                        ButtonContainers(k).Buttons(0).Left = DEFAULT_BUTTON_LEFT

                                        ButtonContainers(k).Buttons(0).Text = "Custom Annotation"
                                        ButtonWidthMultiplier = mySubTabControl.Width / SUB_TAB_WIDTH
                                        ButtonHeightMultiplier = mySubTabControl.Height / SUB_TAB_HEIGHT

                                        ButtonContainers(k).Buttons(0).Height = DEFAULT_BUTTON_HEIGHT * ButtonHeightMultiplier
                                        ButtonContainers(k).Buttons(0).Width = DEFAULT_BUTTON_WIDTH * ButtonWidthMultiplier

                                        ButtonContainers(k).Buttons(0).Font = New Font(ButtonContainers(k).Buttons(0).Font.FontFamily, MENU_FONT_SIZE - 4)
                                        ButtonContainers(k).Buttons(0).Font = New Font(ButtonContainers(k).Buttons(0).Font, FontStyle.Bold)

                                        ButtonContainers(k).Buttons(0).Visible = True

                                        AddHandler ButtonContainers(k).Buttons(0).MouseDown, AddressOf Me.ButtonContainers_Buttons_MouseDown


                                        '*******************************************************
                                        For j = 1 To UBound(EnumerationTypeRecord(i).EnumerationDesc, 2) + 1

                                            If j >= UBound(ButtonContainers(k).Buttons) Then
                                                ReDim Preserve ButtonContainers(k).Buttons(j)
                                                ReDim Preserve ButtonContainers(k).ButtonContainerHotKey(j)

                                                If ButtonContainers(k).Buttons(j) Is Nothing Then

                                                    ButtonContainers(k).Buttons(j) = New Button
                                                    ButtonContainers(k).Buttons(j).Parent = mySubTabControl.TabPages(z)

                                                    ButtonContainers(k).ButtonContainerHotKey(j) = New Label
                                                    ButtonContainers(k).ButtonContainerHotKey(j).Parent = mySubTabControl.TabPages(z)

                                                    ButtonContainers(k).ButtonContainerHotKey(j).Text = EnumerationTypeRecord(i).HotKeyAssignment(j - 1)
                                                    ButtonContainers(k).ButtonContainerHotKey(j).AutoSize = True
                                                    ButtonContainers(k).ButtonContainerHotKey(j).BorderStyle = BorderStyle.Fixed3D
                                                    ButtonContainers(k).ButtonContainerHotKey(j).Visible = True
                                                    ButtonContainers(k).ButtonContainerHotKey(j).Top = ButtonContainers(k).Buttons(j).Top
                                                    ButtonContainers(k).ButtonContainerHotKey(j).Left = ButtonContainers(k).Buttons(j).Left

                                                    ButtonContainers(k).ButtonContainerHotKey(j).Font = New Font(ButtonContainers(k).ButtonContainerHotKey(j).Font.FontFamily, MENU_FONT_SIZE - 6)
                                                    ButtonContainers(k).ButtonContainerHotKey(j).Font = New Font(ButtonContainers(k).ButtonContainerHotKey(j).Font, FontStyle.Bold)

                                                    ButtonContainers(k).ButtonContainerHotKey(j).BringToFront()

                                                    ButtonContainers(k).ButtonContainerHotKey(j).Parent = ButtonContainers(k).Buttons(j)

                                                    If j = 0 Then
                                                        ButtonContainers(k).Buttons(j).Top = DEFAULT_BUTTON_TOP
                                                        ButtonContainers(k).Buttons(j).Left = DEFAULT_BUTTON_LEFT
                                                    ElseIf j < NUM_BUTTONS_ACROSS Then
                                                        ButtonContainers(k).Buttons(j).Top = DEFAULT_BUTTON_TOP
                                                        ButtonContainers(k).Buttons(j).Left = ButtonContainers(k).Buttons(j - 1).Left + ButtonContainers(k).Buttons(j - 1).Width + HORIZ_BUTTON_SPACING
                                                    ElseIf j = NUM_BUTTONS_ACROSS Then
                                                        ButtonContainers(k).Buttons(j).Top = ButtonContainers(k).Buttons(0).Top + ButtonContainers(k).Buttons(0).Height + VERT_BUTTON_SPACING
                                                        ButtonContainers(k).Buttons(j).Left = DEFAULT_BUTTON_LEFT

                                                    ElseIf j = (NUM_BUTTONS_ACROSS * 2) Then

                                                        ButtonContainers(k).Buttons(j).Top = ButtonContainers(k).Buttons(6).Top + ButtonContainers(k).Buttons(6).Height + VERT_BUTTON_SPACING
                                                        ButtonContainers(k).Buttons(j).Left = DEFAULT_BUTTON_LEFT

                                                    Else
                                                        ButtonContainers(k).Buttons(j).Top = ButtonContainers(k).Buttons(j - 1).Top
                                                        ButtonContainers(k).Buttons(j).Left = ButtonContainers(k).Buttons(j - 1).Left + ButtonContainers(k).Buttons(j - 1).Width + HORIZ_BUTTON_SPACING
                                                    End If

                                                    ButtonContainers(k).Buttons(j).Text = EnumerationTypeRecord(i).EnumerationDesc(0, (j - 1))

                                                    ButtonWidthMultiplier = mySubTabControl.Width / SUB_TAB_WIDTH
                                                    ButtonHeightMultiplier = mySubTabControl.Height / SUB_TAB_HEIGHT

                                                    ButtonContainers(k).Buttons(j).Height = DEFAULT_BUTTON_HEIGHT * ButtonHeightMultiplier
                                                    ButtonContainers(k).Buttons(j).Width = DEFAULT_BUTTON_WIDTH * ButtonWidthMultiplier

                                                    ButtonContainers(k).Buttons(j).Font = New Font(ButtonContainers(k).Buttons(j).Font.FontFamily, MENU_FONT_SIZE - 4)
                                                    ButtonContainers(k).Buttons(j).Font = New Font(ButtonContainers(k).Buttons(j).Font, FontStyle.Bold)

                                                    ButtonContainers(k).Buttons(j).Visible = True

                                                    AddHandler ButtonContainers(k).Buttons(j).MouseDown, AddressOf Me.ButtonContainers_Buttons_MouseDown

                                                End If

                                            End If

                                        Next j

                                    End If
                                Next i

                                z = z + 1
                                k = k + 1

                            End If
                        End If
                    Next Y

                Else 'these will be the buttons on the main tabs

                    Dim NumButtonsAcross As Integer
                    Dim DefaultButtonWidth As Integer

                    j = 0
                    buttoncount = 0

                    NumButtonsAcross = NUM_BUTTONS_ACROSS
                    DefaultButtonWidth = DEFAULT_BUTTON_WIDTH

                    If AnnotationTypeRecord(x).ID <> 1998 And AnnotationTypeRecord(x).ID <> 1999 Then
                        AnnotationInterfaceConfigure.ListBox1.Items.Add(AnnotationTypeRecord(x).Description)
                    End If

                    For Y = 0 To UBound(AnnotationValueRecord)
                        If AnnotationTypeRecord(x).ID = AnnotationValueRecord(Y).TypeID And AnnotationTypeRecord(x).ID <= 1009 Then
                            buttoncount = buttoncount + 1
                        End If
                    Next Y

                    If buttoncount > 40 Then
                        NumButtonsAcross = NUM_BUTTONS_ACROSS + 1
                        DefaultButtonWidth = DEFAULT_BUTTON_WIDTH - 25
                    End If


                    If buttoncount > 0 Then
                        buttonheight = (myMainTabControl.Height - (90 + (VERT_BUTTON_SPACING * (Math.Ceiling(buttoncount / (NumButtonsAcross)))))) / ((Math.Ceiling(buttoncount / NumButtonsAcross)) + 1)
                    Else
                        buttonheight = DEFAULT_BUTTON_HEIGHT
                    End If


                    For Y = 0 To UBound(AnnotationValueRecord)
                        If AnnotationTypeRecord(x).ID = AnnotationValueRecord(Y).TypeID And AnnotationTypeRecord(x).ID <= 1009 Then

                            If j = 0 Then
                                For i = 0 To myMainTabControl.TabCount - 1
                                    If myMainTabControl.TabPages(i).Text = AnnotationTypeRecord(x).Description Then

                                        ReDim Preserve ButtonContainers(k)
                                        ReDim Preserve ButtonContainers(k).Buttons(0)

                                        buttonrows = 0

                                        ButtonContainers(k).Parent = myMainTabControl
                                        ButtonContainers(k).ContainerName = myMainTabControl.TabPages(i).Text
                                        Exit For

                                    Else

                                    End If
                                Next i
                            End If

                            If j > UBound(ButtonContainers(k).Buttons) Then
                                ReDim Preserve ButtonContainers(k).Buttons(j)
                            End If

                            ButtonContainers(k).Buttons(j) = New Button
                            ButtonContainers(k).Buttons(j).Parent = myMainTabControl.TabPages(i)
                            ButtonContainers(k).Buttons(j).Text = AnnotationValueRecord(Y).Description

                            ButtonWidthMultiplier = myMainTabControl.Width / MAIN_TAB_WIDTH

                            ButtonContainers(k).Buttons(j).Height = buttonheight

                            ButtonContainers(k).Buttons(j).Width = DefaultButtonWidth * ButtonWidthMultiplier

                            If j = 0 Then
                                ButtonContainers(k).Buttons(j).Top = DEFAULT_BUTTON_TOP
                                ButtonContainers(k).Buttons(j).Left = HORIZ_BUTTON_SPACING
                            ElseIf j < NumButtonsAcross Then
                                ButtonContainers(k).Buttons(j).Top = DEFAULT_BUTTON_TOP
                                ButtonContainers(k).Buttons(j).Left = ButtonContainers(k).Buttons(j - 1).Left + ButtonContainers(k).Buttons(j - 1).Width + HORIZ_BUTTON_SPACING
                            ElseIf j = NumButtonsAcross * (buttonrows + 1) Then
                                ButtonContainers(k).Buttons(j).Top = ButtonContainers(k).Buttons(0).Top + ((ButtonContainers(k).Buttons(0).Height + VERT_BUTTON_SPACING) * (buttonrows + 1))
                                ButtonContainers(k).Buttons(j).Left = HORIZ_BUTTON_SPACING
                                buttonrows = buttonrows + 1
                            Else
                                ButtonContainers(k).Buttons(j).Top = ButtonContainers(k).Buttons(j - 1).Top
                                ButtonContainers(k).Buttons(j).Left = ButtonContainers(k).Buttons(j - 1).Left + ButtonContainers(k).Buttons(j - 1).Width + HORIZ_BUTTON_SPACING

                            End If

                            ButtonContainers(k).Buttons(j).Visible = True

                            AddHandler ButtonContainers(k).Buttons(j).MouseDown, AddressOf Me.ButtonContainers_Buttons_MouseDown

                            j = j + 1

                        End If
                    Next Y

                End If
                k = k + 1
            Next x

            mySubTabControl.SelectedIndex = SaveSubTabIndex

            WriteToListBox("Sizing Annotation Buttons Complete")
            OnVehicleScreen.Refresh()

        Catch ex As Exception

            MsgBox("SizeAnnotationButtons: " & ex.Message)
            CopyToLog("SizeAnnotationButtons: " & ex.Message)

        End Try

    End Sub

    Public Sub LoginButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)

        'One of the up to 10 buttons on the loginform which display the up to 10 most
        'active logins for a particular vehicle.  Sets up data logging path and filenames
        'based on user login id.

        DebugMode = DebugKey

        SaveLoginID = sender.text

        If Len(SaveLoginID) > 0 Then
            LoginForm.Hide()
            OnLoginScreen = False
            OnVehicleScreen.Label5.Text = "Logged in as " & SaveLoginID

        Else
            LoginForm.Button1.Text = "Login as Demo"
        End If

    End Sub

    Sub ReadUserIDList()

        'Called from Initialize()

        'Calls a server function which reads the UserIDList.txt file.  Uses this information
        'to build the loginform display.

        Dim x As Integer

        Dim Users As ArrayList

        Try

            WriteToListBox("Reading UserIDList File...")
            OnVehicleScreen.Refresh()

            Users = myINCAInterface.ReadUserIDList()

            LoginForm.ListBox1.Items.Clear()

            If Not Users Is Nothing Then
                For x = 0 To Users.Count - 1

                    If InStr(UCase(Mid(Users(x).ToString, 8, Len(Users(x).ToString))), "DEMO") = 0 Then

                        LoginForm.ListBox1.Items.Add(Mid(Users(x).ToString, 8, Len(Users(x).ToString)))

                    End If

                    If x < 10 Then
                        ReDim Preserve LoginButton(x)
                        LoginButton(x) = New Button
                        LoginButton(x).Parent = LoginForm
                        LoginButton(x).BackColor = Color.Gainsboro
                        LoginButton(x).UseVisualStyleBackColor = True
                        LoginButton(x).FlatStyle = FlatStyle.Standard
                        LoginButton(x).Size = New System.Drawing.Size(120, 50)

                        If x < 5 Then
                            LoginButton(x).Left = 5
                            LoginButton(x).Top = 10 + ((LoginButton(x).Height + 5) * x)
                        Else
                            LoginButton(x).Left = 130
                            LoginButton(x).Top = 10 + ((LoginButton(x).Height + 5) * (x - 5))
                        End If

                        LoginButton(x).Visible = True

                        LoginButton(x).Text = Mid(Trim(Users(x).ToString), 8, Len(Users(x).ToString) - 7)
                        LoginButton(x).Font = New Font(LoginButton(x).Font.FontFamily, 12)
                        LoginButton(x).Font = New Font(LoginButton(x).Font, FontStyle.Bold)

                        AddHandler LoginButton(x).Click, AddressOf Me.LoginButton_Click

                    End If

                Next
            End If

            WriteToListBox("Reading UserIDList File Complete")
            OnVehicleScreen.Refresh()

        Catch ex As Exception

            CopyToLog("ReadUserIDList: " & ex.Message)
            MsgBox("ReadUserIDList: " & ex.Message)

        End Try

    End Sub

    Private Sub PerformanceMonitor()

        'This was only used during initial development of the app and is no longer applicable...

        'Keeps track of percent cpu usage and memory usage. Was enabled or disabled using
        'dropdown from the main top menu bar Actions selection, this selection does not currently exist...

        Static CPUCounter As PerformanceCounter
        Static MEMCounter As PerformanceCounter

        Static SavePerformanceMonitoringStatus As Boolean

        Static RecordStatus As String

        Dim tempstr As String
        Dim z As Integer

        Static Cntr As Integer

        Do While True

            'This is not currently enabled (it would be enabled at its definition), so none of this code is executed...
            'This was used during development...
            If EnablePerformanceMonitoring = True Then

                GetDeviceStatus()
                SavePerformanceMonitoringStatus = True

                If myINCAInterface.Recording = True And myINCAInterface.StopRecordingRequested = False Then
                    RecordStatus = "True"
                ElseIf myINCAInterface.Recording = False Then
                    RecordStatus = "False"
                ElseIf myINCAInterface.StopRecordingRequested = True And myINCAInterface.Recording = True Then
                    RecordStatus = "Saving"
                End If

                If CPUCounter Is Nothing Then

                    Cntr = 0

                    ReDim PerformanceData(10, 0)

                    PerformanceData(0, Cntr) = "CPU Usage %"
                    PerformanceData(1, Cntr) = "MEM Available GB"
                    PerformanceData(2, Cntr) = "INCA Polling Rate msec"
                    PerformanceData(3, Cntr) = "Display Refresh Rate msec"
                    PerformanceData(4, Cntr) = "Server Data Collection Rate msec"
                    PerformanceData(5, Cntr) = "Recorded Variables"
                    PerformanceData(6, Cntr) = "Displayed Variables"
                    PerformanceData(7, Cntr) = "Number of Active Devices"
                    PerformanceData(8, Cntr) = "Recording Status" 'False, true, saving?
                    PerformanceData(9, Cntr) = "PC Name"
                    PerformanceData(10, Cntr) = "EOCM Type"

                    CPUCounter = New PerformanceCounter("Processor", "% Processor Time", "_Total")
                    MEMCounter = New PerformanceCounter("Memory", "Available MBytes")

                Else 'CPUCounter not nothing

                    If Cntr > 0 And (myINCAInterface.GetINCAPollingRate <> Val(PerformanceData(2, Cntr)) Or
                    DisplayRefreshRate <> Val(PerformanceData(3, Cntr)) Or
                    DataCollectionRate <> Val(PerformanceData(4, Cntr)) Or
                    UBound(myINCAInterface.mySignals) <> Val(PerformanceData(5, Cntr)) Or
                    TotalNumSignalsDisplayed <> Val(PerformanceData(6, Cntr))) Then

                        CPUCounter = Nothing
                        MEMCounter = Nothing

                        SavePerformanceMonitorData(PerformanceData)

                    End If

                    If Not CPUCounter Is Nothing Then

                        Cntr = Cntr + 1

                        ReDim Preserve PerformanceData(10, Cntr)

                        PerformanceData(0, Cntr) = Format$(CPUCounter.NextValue(), "0.00")
                        PerformanceData(1, Cntr) = Format$(Val(MEMCounter.NextValue().ToString() / 1024), "0.00")
                        PerformanceData(2, Cntr) = CStr(myINCAInterface.GetINCAPollingRate)
                        PerformanceData(3, Cntr) = CStr(DisplayRefreshRate)
                        PerformanceData(4, Cntr) = CStr(DataCollectionRate)
                        PerformanceData(5, Cntr) = CStr(UBound(myINCAInterface.mySignals))
                        PerformanceData(6, Cntr) = CStr(TotalNumSignalsDisplayed)
                        PerformanceData(7, Cntr) = CStr(TotalNumActiveDevices)
                        PerformanceData(8, Cntr) = RecordStatus
                        PerformanceData(9, Cntr) = System.Net.Dns.GetHostName
                        PerformanceData(10, Cntr) = "ESS2"

                        tempstr = ""
                        For z = 0 To 10
                            If Len(tempstr) = 0 Then
                                tempstr = PerformanceData(z, Cntr)
                            Else
                                tempstr = tempstr & "," & PerformanceData(z, Cntr)
                            End If
                        Next

                    End If

                End If

            End If

            System.Threading.Thread.Sleep(2000)

        Loop

    End Sub
    Public Sub SavePerformanceMonitorData(ByVal PerformanceData(,) As String)

        'Not really used anymore...

        'Saves performance monitor data to a file when performance monitoring is de-selected or
        'if certain performance parameters are changed such as display refresh rate, or INCA
        'Polling Cycle Time.

        Dim x As Integer
        Dim y As Integer
        Dim fnum As Long
        Dim filename As String
        Dim tempstr As String

        fnum = FreeFile()
        filename = My.Application.Info.DirectoryPath & "\PM\" & PerformanceData(9, 1) & "_IPR_" & PerformanceData(2, 1) & "_DRR_" & PerformanceData(3, 1) & "_DCR_" & PerformanceData(4, 1) & "_" & Format(Now, "yyyyMMdd_hhmmss") & ".csv"

        If System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\PM") = False Then
            System.IO.Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\PM")
        End If

        FileOpen(fnum, filename, OpenMode.Output)

        tempstr = ""
        For x = 0 To UBound(PerformanceData, 2)
            For y = 0 To UBound(PerformanceData, 1)
                If y = 0 Then
                    tempstr = PerformanceData(y, x)
                Else
                    tempstr = tempstr & "," & PerformanceData(y, x)
                End If
            Next
            PrintLine(fnum, tempstr)
        Next

        FileClose(fnum)

    End Sub

    Private Function WhereTheHeckAmIAt(ByVal LAT_POS As Double, ByVal LON_POS As Double) As String

        'Called from myBackGroundTasks:  Determines if we are on or off property based on GPS coordinates...

        Static SaveWhereTheHeckAmIAt As String
        Dim WhereAmI As String

        If LAT_POS <> 0 And LON_POS <> 0 Then

            If LAT_POS > NORTH_MIDPOINT_LAT Or LAT_POS < SOUTHWEST_CORNER_LAT Then
                WhereAmI = "Off Property"
            ElseIf LON_POS < NORTHWEST_CORNER_LON Or LON_POS > NORTHEAST_CORNER_LON Then
                WhereAmI = "Off Property"
            ElseIf LAT_POS < NORTHEAST_CORNER_LAT And LAT_POS > SOUTHEAST_CORNER_LAT And
                   LON_POS > SOUTHWEST_CORNER_LON And LON_POS < NORTHEAST_CORNER_LON Then
                WhereAmI = "On Property"
            ElseIf LAT_POS < NORTHWEST_CORNER_LAT And LAT_POS > NORTHEAST_CORNER_LAT And
                   LON_POS > NORTHWEST_CORNER_LON And LON_POS < NORTH_MIDPOINT_LON Then
                WhereAmI = "On Property"
            Else 'this handles the "bermuda triangle" at the north east corner of MPG
                'If we are on property when we get here, we stay on property, if we are
                'off property when we get here, we stay off property...

                If SaveWhereTheHeckAmIAt = "Off Property" Then
                    WhereAmI = "Off Property"
                Else
                    WhereAmI = "On Property"
                End If

            End If

            SaveWhereTheHeckAmIAt = WhereAmI
            OnVehicleScreen.Label1.BackColor = Color.Green

        Else
            If Len(SaveWhereTheHeckAmIAt) > 0 Then
                WhereAmI = SaveWhereTheHeckAmIAt
                OnVehicleScreen.Label1.BackColor = Color.Yellow
            Else
                WhereAmI = "Unknown"
                OnVehicleScreen.Label1.BackColor = Color.Red
            End If

        End If

        OnVehicleScreen.Label1.Text = WhereAmI
        WhereTheHeckAmIAt = WhereAmI

    End Function

    Private Sub CheckForLostDevice()

        'This routine handles the case where communication to processors or video cameras has been lost.  Allows user to
        'reinitialilze CLEVIR and INCA...

        Dim ReturnStr As String
        Dim OperatorMessage As String = ""
        Dim Button1Text As String = ""
        Dim Button2Text As String = ""
        Dim Button3Text As String = ""
        Dim Button4Text As String = ""

        Static VideoMessageLastDisplayed As Boolean = True
        Static DataMessageLastDisplayed As Boolean = True

        Try

            If LostDevice = True Or VideoCameraNotUpdating = True Or BackgroundLoopCounterNotUpdating = True Then

                If IgnoreLostDeviceUntilNextRecordingSession = False And IgnoreLostDeviceForThisDrive = False Then

                    CopyToLog("CheckForLostDevice: RedisplayOnVehicleForm(Main) Called...")
                    RedisplayOnVehicleForm("Main")

                    If BackgroundLoopCounterNotUpdating = True And LostDevice = False And (VideoCameraNotUpdating = False Or (VideoCameraNotUpdating = True And VideoMessageLastDisplayed = True)) Then
                        OperatorMessage = "INVALID DATA ALERT! (" & SaveDataFrozenDeviceName & ") To insure data quality, CLEVIR/INCA should be re-initialized.  This will take approx 3 minutes"
                        Button1Text = "CANCEL"
                        Button2Text = "Re-Initialize NOW"
                        Button3Text = "Ignore All Alerts for This Drive"
                        Button4Text = "Ignore All Alerts for Recording Session"
                        VideoMessageLastDisplayed = False
                        DataMessageLastDisplayed = True
                    ElseIf VideoCameraNotUpdating = True And LostDevice = False And (BackgroundLoopCounterNotUpdating = False Or (BackgroundLoopCounterNotUpdating = True And DataMessageLastDisplayed = True)) Then
                        OperatorMessage = "INVALID VIDEO ALERT! (" & SaveVideoFrozenDeviceName & ") CLEVIR/INCA should be re-initialized to insure video recording capability.  This will take approx 3 minutes."
                        Button1Text = "CANCEL"
                        Button2Text = "Re-Initialize NOW"
                        Button3Text = "Ignore All Alerts for This Drive"
                        Button4Text = "Ignore All Alerts for Recording Session"
                        VideoMessageLastDisplayed = True
                        DataMessageLastDisplayed = False
                    ElseIf LostDevice = True Then
                        OperatorMessage = "PROCESSOR COMMUNICATION ALERT! (" & SaveLostDeviceName & ") CLEVIR/INCA should be re-initialized to insure data and video recording capability.  This will take approx 3 minutes."
                        Button1Text = "CANCEL"
                        Button2Text = "Re-Initialize NOW"
                        Button3Text = "Ignore All Alerts for This Drive"
                        Button4Text = ""
                    End If

                    CopyToLog("CheckForLostDevice: " & OperatorMessage)

                    Select Case Cusmsgbox.DisplayCusMsgBox(OperatorMessage, "CLEVIR ALERT", Button1Text, Button2Text, Button3Text, Button4Text)

                        Case 1 'CANCEL
                            CopyToLog("CheckForLostDevice: User Answered " & Button1Text)

                            OnVehicleScreen.TopMost = False

                        Case 2 'Re-Initialize
                            CopyToLog("CheckForLostDevice: User Answered " & Button2Text)

                            BackgroundLoopCounterNotUpdating = False
                            VideoCameraNotUpdating = False

                            If myINCAInterface.GetRecordingState() = True Then
                                myINCAInterface.StartStopRecord(OnVehicleScreen.Button14)

                            Else
                                If myINCAInterface.GetMeasurementStatus() = True Then
                                    myINCAInterface.StartStopMeasurement(OnVehicleScreen.Button6)

                                End If
                            End If

                            HandleButtonVisibility(False)

                            INCAInitStarted = True
                            Initializing = True

                            InitForm.HowLongHaveIBeenUp = Now.Subtract(Now)

                            UserStatusInfo.Label1.Text = "Checking for Cameras..."
                            CheckForCameras(35, 5)
                            UserStatusInfo.Label1.Text = "Reinitializing INCA..."
                            myINCAInterface.RCI2_Cleanup()
                            ReturnStr = myINCAInterface.HandleWorkspace("", False)

                            If ReturnStr = "True" Or InStr(ReturnStr, "ERROR:") = 0 Then
                                UserStatusInfo.Label1.Text = "Registering Signals, please wait..."
                                myINCAInterface.RegisterSignals()
                                UserStatusInfo.Label1.Text = "Registering Signals complete."
                            Else
                                MsgBox("Reinitialiation Failed: " & ReturnStr & ", Exiting...")
                                ExitApp("Complete")
                            End If

                            UserStatusInfo.Hide()

                            OnVehicleScreen.TopMost = False

                            GetDeviceStatus()

                            INCAInitStarted = False
                            Initializing = False

                            HandleButtonVisibility(True)

                        Case 3 'Ignore All Alerts for This Drive

                            CopyToLog("CheckForLostDevice: User Answered " & Button3Text)
                            IgnoreLostDeviceForThisDrive = True

                            OnVehicleScreen.TopMost = False
                        Case 4 'Ignore All Alerts for Recording Session - Not an option if Processor Communication Alert...

                            CopyToLog("CheckForLostDevice: User Answered " & Button4Text)
                            IgnoreLostDeviceUntilNextRecordingSession = True
                            'NoReInitOnLostDevice = True

                            OnVehicleScreen.TopMost = False
                    End Select

                End If

                LostDevice = False
                VideoCameraNotUpdating = False
                BackgroundLoopCounterNotUpdating = False

            End If 'LostDevice = True Or VideoCameraNotUpdating = True Or BackgroundLoopCounterNotUpdating = True

        Catch ex As Exception
            CopyToLog("CheckForLostDevice: " & ex.Message)
        End Try

    End Sub

    Private Sub HandlePullingDIDs()

        'Called from Handle_5_SecondChecks which is called from myBackgroundTasks.
        'Handles pulling DIDs if this functionality is enabled...

        Dim myDIDPullFiles As String()
        Static LastFileFound As Boolean
        Dim v As Integer

        Try

            If EnableDIDPull = True Then
                If EnableStartZipFileCheck = True Then

                    myDIDPullFiles = Directory.GetFiles(DefaultVSpyDataDirectory)

                    If UBound(myDIDPullFiles) >= MAX_NUM_DID_PULL_FILES - 1 Then

                        For v = 0 To UBound(myDIDPullFiles)
                            If InStr(myDIDPullFiles(v), DIDPullTriggerZippingKey) > 0 Then
                                LastFileFound = True
                                Exit For
                            End If
                        Next

                        If LastFileFound = True Then
                            LastFileFound = False

                            Do While FileInUse(myDIDPullFiles(v))
                                System.Threading.Thread.Sleep(100)
                            Loop

                            ZipTheDirectory(DefaultVSpyDataDirectory, "Start")

                            For v = 0 To UBound(myDIDPullFiles)
                                If InStr(myDIDPullFiles(v), "m.csv") > 0 Then
                                    File.Delete(myDIDPullFiles(v))
                                End If
                            Next

                            EnableStartZipFileCheck = False
                            StopVehicleSpy()
                            UserStatusInfo.Hide()

                            OnVehicleScreen.Button6.Enabled = True 'Start Measurement
                            OnVehicleScreen.Button14.Enabled = True 'Start Record
                        End If
                    End If

                End If

                If EnableEndZipFileCheck = True Then

                    myDIDPullFiles = Directory.GetFiles(DefaultVSpyDataDirectory)

                    If UBound(myDIDPullFiles) >= MAX_NUM_DID_PULL_FILES - 1 Then

                        For v = 0 To UBound(myDIDPullFiles)
                            If InStr(myDIDPullFiles(v), DIDPullTriggerZippingKey) > 0 Then
                                LastFileFound = True
                                Exit For
                            End If
                        Next

                        If LastFileFound = True Then

                            LastFileFound = False

                            Do While FileInUse(myDIDPullFiles(v))
                                System.Threading.Thread.Sleep(100)
                            Loop

                            ZipTheDirectory(DefaultVSpyDataDirectory, "End")

                            For v = 0 To UBound(myDIDPullFiles)
                                If InStr(myDIDPullFiles(v), "m.csv") > 0 Then
                                    File.Delete(myDIDPullFiles(v))
                                End If
                            Next

                            EnableEndZipFileCheck = False
                            UserStatusInfo.Hide()
                            StopVehicleSpy()
                            ExitApp()
                            Exit Sub

                        End If

                    End If

                End If
            End If

        Catch ex As Exception
            CopyToLog("HandlePullingDIDs - DID Pull Section: " & ex.Message)
            MsgBox("DID Pull failed due to " & ex.Message)
        End Try

    End Sub

    Private Sub Handle_5_SecondChecks(ByVal L_MeasurementStatus As String, ByVal L_RecordingState As String)

        'Called from myBAckgroundTasks every 5 seconds.  Handles stuff which needs to be checked regularly
        'but is not critical to be checked every loop...

        Static L_IsTargetOnWorkingPage As String = "False"
        Static SaveIsTargetOnWorkingPage As String

        Try

            If CheckForExperiment = True Then

                'If the GetMeasurementStatus returns "Invalid" this indicates that the INCA experiment is no longer
                'running in which case, we will terminate this application.

                'CLEVIR Cannot run without a valid INCA Experiment to latch on to!!!!

                If L_MeasurementStatus = "Invalid" Then
                    CopyToLog("Handle_5_SecondChecks: The INCA Experiment is not running, GM_ResidentClient will now terminate.")
                    OnVehicleScreen.TopMost = True
                    MsgBox("The INCA Experiment is not running, GM_ResidentClient will now terminate.")
                    OnVehicleScreen.TopMost = False
                    ExitApp("Complete")

                End If

            End If

            If PlaybackMode = False Then

                If OperatingMode = OperatingModes.RES_ON_VPC Or
                    OperatingMode = OperatingModes.RES_ON_SUITCASE_VPC Or
                    OperatingMode = OperatingModes.RES_ON_LAPTOP_VPC Then

                    GetDeviceStatus()
                End If

                L_IsTargetOnWorkingPage = myINCAInterface.IsTargetOnWorkingPage

                If L_IsTargetOnWorkingPage = "True" And OnVehicleScreen.RadioButton1.Checked = False Then
                    OnVehicleScreen.RadioButton1.Checked = True
                    OnVehicleScreen.RadioButton2.Checked = False
                ElseIf L_IsTargetOnWorkingPage = "False" And OnVehicleScreen.RadioButton2.Checked = False Then
                    OnVehicleScreen.RadioButton1.Checked = False
                    OnVehicleScreen.RadioButton2.Checked = True
                End If

                'Handle DID pull using vehicle spy (if configured to do so)
                HandlePullingDIDs()

            End If

            'If we are recording, then we need to check to see if the user changed from reference page
            'to working page, this would indicate that they may be making calibration changes during the
            'recording.  We want to capture this event, so we write an event message into the record file
            'indicating a transition between working page and reference page.

            If L_RecordingState = "True" Then

                L_IsTargetOnWorkingPage = myINCAInterface.IsTargetOnWorkingPage

                If L_IsTargetOnWorkingPage = "True" And SaveIsTargetOnWorkingPage <> "True" Then
                    SaveIsTargetOnWorkingPage = "True"
                    myINCAInterface.WriteEventComment(Format$(Now, "hh:mm:ss") & " " & "Detected a Switch to the Working Page")
                    CopyToLog("Handle_5_SecondChecks: Detected a Switch to the Working Page")

                ElseIf L_IsTargetOnWorkingPage = "False" And SaveIsTargetOnWorkingPage <> "False" Then
                    SaveIsTargetOnWorkingPage = "False"
                    myINCAInterface.WriteEventComment(Format$(Now, "hh:mm:ss") & " " & "Detected a Switch to the Reference Page")
                    CopyToLog("Handle_5_SecondChecks: Detected a Switch to the Reference Page")

                End If

            End If

        Catch ex As Exception
            CopyToLog("Handle_5_SecondChecks: " & ex.Message)
        End Try

    End Sub

    Private Sub HandleAutomaticStartRecord(ByVal myMeasureTime As TimeSpan, ByRef mySaveMeasureTime As DateTime, ByRef OneShot As Boolean)

        'Called from myBackgroundTasks...

        'if 5 minutes have passed while in measure mode and record button has not been pressed, we will display a message indicating
        'that recording will start in 10 seconds.  User may click CANCEL and stay in measuremode, if they do not cancel within 10
        'seconds, we automatically go into record mode.  This was added because some were running in measure mode, thinking they were
        'actually recording...

        Dim e As System.EventArgs = System.EventArgs.Empty

        Try

            If myMeasureTime.Minutes >= 5 And OneShot = False Then

                OneShot = True

                RecordTransitionDelay = 0
                UserStatusInfo.Button1.Visible = True

                Do While RecordTransitionDelay >= 0 And RecordTransitionDelay <= 10
                    UserStatusInfo.Label1.Text = "Recording will start in " & 10 - RecordTransitionDelay & " seconds unless CANCEL is pressed..."
                    RecordTransitionDelay = RecordTransitionDelay + 1
                    System.Threading.Thread.Sleep(1000)
                    System.Windows.Forms.Application.DoEvents()
                Loop

                If RecordTransitionDelay >= 10 Then
                    UserStatusInfo.Label1.Text = "Starting Recording NOW..."
                    OnVehicleScreen.Button14_Click(OnVehicleScreen.Button14, e)
                End If

                mySaveMeasureTime = Now
                myMeasureTime = Now.Subtract(mySaveMeasureTime)
                RecordTransitionDelay = 0
                UserStatusInfo.Close()
                UserStatusInfo.Button1.Visible = False

            End If

        Catch ex As Exception
            CopyToLog("HandleAutomaticStartRecord: " & ex.Message)
        End Try
    End Sub

    Private Sub CheckForINCAButtonPresses(ByVal L_MeasurementStatus As String, ByVal L_RecordingState As String)

        'Called from myBackGroundTasks.
        'Checks to see if user started or stopped measurement or recording directly from INCA experiment rather than from CLEVIR interface...
        'User interaction directly from within INCA experiment can cause some screwy behavior...

        Try

            If L_MeasurementStatus = "True" And myINCAInterface.MeasurementStarted = False Then

                CopyToLog("CheckForINCAButtonPresses: Measurement Started in INCA")

                myINCAInterface.MeasurementStarted = True
                myINCAInterface.StartDataCollection(DataCollectionRate)
                OnVehicleScreen.Button6.Text = "STOP MEASUREMENT"
                OnVehicleScreen.Button6.BackColor = Color.Blue
                OnVehicleScreen.Button6.ForeColor = Color.White

            ElseIf L_MeasurementStatus = "False" And myINCAInterface.MeasurementStarted = True Then

                CopyToLog("CheckForINCAButtonPresses: Measurement Stopped in INCA")

                myINCAInterface.MeasurementStarted = False
                myINCAInterface.StopDataCollection()
                OnVehicleScreen.Button6.Text = "START MEASUREMENT"
                OnVehicleScreen.Button14.Text = "START RECORD"

                OnVehicleScreen.Button14.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button14.ForeColor = Color.Black
                OnVehicleScreen.Button6.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button6.ForeColor = Color.Black

            End If

            If L_RecordingState = True And myINCAInterface.Recording = False Then

                CopyToLog("CheckForINCAButtonPresses: Recording Started in INCA")

                myINCAInterface.MeasurementStarted = True
                myINCAInterface.Recording = True
                myINCAInterface.StartDataCollection(DataCollectionRate)
                OnVehicleScreen.Button6.Text = "STOP MEASUREMENT"
                OnVehicleScreen.Button6.BackColor = Color.Blue
                OnVehicleScreen.Button6.ForeColor = Color.White
                OnVehicleScreen.Button14.Text = "STOP RECORD"
                OnVehicleScreen.Button14.BackColor = Color.Red
                OnVehicleScreen.Button14.ForeColor = Color.White

                'Fusion Status Display, and LMFR Status Display(s) have custom annotation buttons, these
                'must be enabled if we see that we are in record mode...

                If FusionStatusDisplay.Visible = True Then
                    FusionStatusDisplay.Button1.Enabled = True
                    FusionStatusDisplay.Button2.Enabled = True
                    FusionStatusDisplay.Button3.Enabled = True
                    FusionStatusDisplay.Button4.Enabled = True
                    FusionStatusDisplay.Button5.Enabled = True
                    FusionStatusDisplay.Button6.Enabled = True
                    FusionStatusDisplay.Button8.Enabled = True
                    FusionStatusDisplay.Refresh()
                End If

                If LMFR_Status_Display_Global_A.Visible = True Then
                    LMFR_Status_Display_Global_A.Button1.Enabled = True
                    LMFR_Status_Display_Global_A.Refresh()
                End If

                If LMFR_Status_Screen_HC.Visible = True Then
                    LMFR_Status_Screen_HC.Button1.Enabled = True
                    LMFR_Status_Screen_HC.Refresh()
                End If

            ElseIf L_RecordingState = False And myINCAInterface.Recording = True Then

                CopyToLog("CheckForINCAButtonPresses: Recording Stopped in INCA")

                myINCAInterface.Recording = False
                OnVehicleScreen.Button14.Text = "START RECORD"
                OnVehicleScreen.Button14.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button14.ForeColor = Color.Black

                'Fusion Status Display, and LMFR Status Display(s) have custom annotation buttons, these
                'must be disabled if we see that we are in record mode...

                If FusionStatusDisplay.Visible = True Then
                    FusionStatusDisplay.Button1.Enabled = False
                    FusionStatusDisplay.Button2.Enabled = False
                    FusionStatusDisplay.Button3.Enabled = False
                    FusionStatusDisplay.Button4.Enabled = False
                    FusionStatusDisplay.Button5.Enabled = False
                    FusionStatusDisplay.Button6.Enabled = False
                    FusionStatusDisplay.Button8.Enabled = False
                End If

                If LMFR_Status_Display_Global_A.Visible = True Then
                    LMFR_Status_Display_Global_A.Button1.Enabled = False
                    LMFR_Status_Display_Global_A.Refresh()
                End If

                If LMFR_Status_Screen_HC.Visible = True Then
                    LMFR_Status_Screen_HC.Button1.Enabled = False
                    LMFR_Status_Screen_HC.Refresh()
                End If

            End If

        Catch ex As Exception
            CopyToLog("CheckForINCAButtonPresses: " & ex.Message)
        End Try
    End Sub

    Private Sub HandleWAVRecording()

        'This routine handles the color of the frame around the microphone to indicate whether or not a WAV recording
        'is in progress.  Red, we are not recording WAV, Green we are...
        'There is a preset amount of time that the WAV recording is enabled, and it needs to shut off after that time.
        'This routine handles that...

        Static RecordWAVElapseTime As TimeSpan
        Static SaveRecordWavTime As DateTime

        Try

            If IsNumeric(RecordWAVTime) Then

                If (OnVehicleScreen.PictureBox1.BackColor = Color.Green) And Val(RecordWAVTime) > 0 Then
                    RecordWAVElapseTime = Now.Subtract(SaveRecordWavTime)

                    If RecordWAVElapseTime.TotalSeconds > Val(RecordWAVTime) Then
                        SaveRecordWavTime = Now
                        WriteToListBox("Record WAV Time has expired")

                        If OnVehicleScreen.Visible = True Then
                            OnVehicleScreen.PictureBox1.BackColor = Color.Red
                            OnVehicleScreen.PictureBox1.Image = My.Resources.Resources.mic_50_Red()
                        End If

                        StopWAVRecord()
                    End If
                Else
                    SaveRecordWavTime = Now
                End If

            End If

        Catch ex As Exception
            CopyToLog("HandleWAVRecording: " & ex.Message)
        End Try

    End Sub

    Private Sub HandleUpdatesWhenRecording(ByRef SaveRecordFileTime As DateTime)

        'This routine handles various tasks that must be performed while we are recording...
        'Handles setting and display of proper record file name and handles stopping and starting
        'INCA recording based on user defined time interval...

        Static myAltRcrdSaveTime As DateTime
        Static myAltRcrdElapseTime As TimeSpan
        Static myCANAlyzerRcrdElapseTime As TimeSpan
        Static myCANAlyzerRcrdSaveTime As DateTime

        Dim tempstr As String

        Try

            'This is the label which rides on top of the progress bar that indicates whether or not we are recording.
            'Here we keep track of the recording file name and change it on transition into next recording...
            If OnVehicleScreen.Label5.Visible = True Then

                If OnVehicleScreen.Label5.Text <> "Recording Starting" Then

                    If OnVehicleScreen.Visible = True And InStr(OnVehicleScreen.Label5.Text, "Recording Filename:") = 0 Then

                        tempstr = myINCAInterface.GetLastRecordingFileName

                        If Len(tempstr) = 0 Then
                            tempstr = myINCAInterface.GetRecordingFileName
                            tempstr = Mid(tempstr, 1, InStr(tempstr, ".") - 1) & "01.mf4"
                            OnVehicleScreen.Label5.Text = "Recording Filename: " & tempstr
                            SaveRecordingFileName = tempstr

                        Else

                            tempstr = Mid(tempstr, 1, InStr(tempstr, ".") - 3) & Format(Val(Mid(tempstr, InStr(tempstr, ".") - 2, 2)) + 1, "00") & ".mf4"
                            tempstr = Mid(tempstr, InStrRev(tempstr, "\") + 1, Len(tempstr))
                            OnVehicleScreen.Label5.Text = "Recording Filename: " & tempstr
                            SaveRecordingFileName = tempstr
                        End If

                    End If

                Else

                    tempstr = myINCAInterface.GetRecordingFileName
                    tempstr = Mid(tempstr, 1, InStr(tempstr, ".") - 1) & "01.mf4"
                    OnVehicleScreen.Label5.Text = "Recording Filename: " & tempstr
                    SaveRecordingFileName = tempstr

                End If

            End If

            'This section handles the display of how many milliseconds into a recording we are, stops and starts recording accordingly...
            If RecordFileDurationMinutes > 0 Then

                RecordFileElapseTime = Now.Subtract(SaveRecordFileTime)

                If RecordFileElapseTime.TotalMilliseconds - StartRecordDelay > 0 Then
                    If OnVehicleScreen.Visible = True Then
                        OnVehicleScreen.Label8.Text = Format((RecordFileElapseTime.TotalMilliseconds - StartRecordDelay) / 60000, "0.0")
                    End If
                End If

                If (RecordFileElapseTime.TotalMilliseconds - StartRecordDelay) >= (RecordFileDurationMinutes * 60000) Then
                    WriteToListBox("Record File Time has expired")
                    CopyToLog("HandleUpdatesWhenRecording: Record File Time (" & RecordFileDurationMinutes & " Minutes) has expired StopAndStartRecording Called...")
                    myINCAInterface.StopAndStartRecording()

                    If CheckRecordingFileNameFormat() = False Then
                        Dim eventargs As System.EventArgs
                        eventargs = System.EventArgs.Empty
                        myINCAInterface.StartStopMeasurement(OnVehicleScreen.Button6)
                    End If

                    SaveRecordFileTime = Now
                End If
            Else
                SaveRecordFileTime = Now
            End If

            'here we are checking to make sure vehicle spy is still running if it was started at record time

            If VehicleSpyStarted = True And CheckVSpyMessageDisplayed = False Then

                myAltRcrdElapseTime = Now.Subtract(myAltRcrdSaveTime)
                If myAltRcrdElapseTime.Seconds >= 5 Then
                    myAltRcrdSaveTime = Now

                    If IsProcessRunning("VSPY3") = False Then
                        VehicleSpyStarted = False
                        UserStatusInfo.Label1.Text = "Vehicle Spy is no longer running.  It is possible that it was manually exited.  To re-enable Vehicle Spy, you must exit and re-launch CLEVIR..."
                        System.Threading.Thread.Sleep(4000)
                        UserStatusInfo.Hide()
                        OnVehicleScreen.Label3.BackColor = Color.Red
                        LoginForm.CheckBox3.Checked = False
                        CheckVSpyMessageDisplayed = True
                    End If

                End If

            End If

            'here we are checking to make sure CANalyzer is still running if it was started at record time

            If CanalyzerStarted = True And CheckCANAlyzerMessageDisplayed = False Then

                myCANAlyzerRcrdElapseTime = Now.Subtract(myCANAlyzerRcrdSaveTime)
                If myCANAlyzerRcrdElapseTime.Seconds >= 5 Then
                    myCANAlyzerRcrdSaveTime = Now

                    If IsProcessRunning("CANW64") = False Then
                        CanalyzerStarted = False
                        UserStatusInfo.Label1.Text = "CANAlyzer is no longer running.  It is possible that it was manually exited.  To re-enable CANAlyzer, you must exit and re-launch CLEVIR..."
                        System.Threading.Thread.Sleep(4000)
                        UserStatusInfo.Hide()
                        OnVehicleScreen.Label3.BackColor = Color.Red
                        LoginForm.CheckBox3.Checked = False
                        CheckCANAlyzerMessageDisplayed = True
                    End If

                End If

            End If

        Catch ex As Exception
            CopyToLog("HandleUpdatesWhenRecording: " & ex.Message)
        End Try
    End Sub

    Private Sub HandleOdometerRelatedStatus(ByVal myDG As GridDataClass, ByRef SaveODOReading As Double, ByRef SaveWhereAmIAt As String, ByVal L_RecordingState As Boolean)

        'Called from myBackgroundTasks.
        'Takes care of recording mileage based on vehicle location on or off property, etc.

        Static SaveLCCActive As Integer
        Static SaveRecordingState As Boolean

        If myDG.CS_ODOMETER > 0 Then

            If myDG.CS_GPS_LAT > 0 And myDG.CS_GPS_LON > 0 Then
                WhereAmIAt = WhereTheHeckAmIAt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_GPS_LAT, 1)).SignalData, mySignalDataWithTime(myDG.SignalIndex(myDG.CS_GPS_LON, 1)).SignalData)

                'Save lat and lon here to be used when  annotations are written, similar to currentmileage below...
                ' / CONVERT_TO_LAT_LON
                CurrentLatitude = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_GPS_LAT, 1)).SignalData

                If CurrentLatitude <> 0 Then
                    CurrentLatitude = CurrentLatitude / CONVERT_TO_LAT_LON
                End If

                CurrentLongitude = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_GPS_LON, 1)).SignalData

                If CurrentLongitude <> 0 Then
                    CurrentLongitude = CurrentLongitude / CONVERT_TO_LAT_LON
                End If

            End If

            If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ODOMETER, 1)).SignalData > 0 Then
                SaveODOReading = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ODOMETER, 1)).SignalData * KILOMETERS_TO_MILES
                CurrentMileage = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ODOMETER, 1)).SignalData
            End If

            'this is for testing when mileage is not updating (on biggie smalls)
            'SaveODOReading = (SaveODOReading + (0.01 * KILOMETERS_TO_MILES))
            'CurrentMileage = CurrentMileage + 0.01

            If SaveODOReading > 0 Then

                If StartingMileage = 0 Then

                    CopyToLog("HandleOdometerRelatedStatus: Resetting Mileage Buckets, Starting Mileage = 0")

                    StartingMileage = SaveODOReading
                    OnPropertyMileage_Recording = 0
                    OnPropertyMileage_NotRecording = 0
                    OffPropertyMileage_Recording = 0
                    OffPropertyMileage_NotRecording = 0
                    UnknownMileage_Recording = 0
                    UnknownMileage_NotRecording = 0

                    OnPropertyStartingODO_Recording = 0
                    OffPropertyStartingODO_Recording = 0
                    OnPropertyStartingODO_NotRecording = 0
                    OffPropertyStartingODO_NotRecording = 0
                    UnknownStartingODO_NotRecording = 0
                    UnknownStartingODO_Recording = 0

                    LCCActiveStartingMileage = 0
                    LCCActiveMileage = 0
                    SaveLCCActive = 0

                    OnProperty_Recording = False
                    OnProperty_NotRecording = False
                    OffProperty_Recording = False
                    OffProperty_NotRecording = False
                    Unknown_Recording = False
                    Unknown_NotRecording = False

                End If

                If myDG.CS_LCC_CONTROL_ACTIVE > 0 Then

                    'LCCActive = CInt(GetRandom(0.0, 2.0))

                    LCCActive = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LCC_CONTROL_ACTIVE, 1)).SignalData)

                    If LCCActive <> SaveLCCActive Then
                        If LCCActive = 1 Then
                            LCCActiveStartingMileage = SaveODOReading
                            CopyToLog("HandleOdometerRelatedStatus: LCC Active - Starting Mileage = " & SaveODOReading)
                        Else
                            LCCActiveMileage = LCCActiveMileage + (SaveODOReading - LCCActiveStartingMileage)
                            CopyToLog("HandleOdometerRelatedStatus: LCC InActive - Mileage Travelled since Active Transition = " & SaveODOReading - LCCActiveStartingMileage)
                            CopyToLog("HandleOdometerRelatedStatus: LCC InActive - Total Accumulated Mileage this session = " & LCCActiveMileage)
                        End If
                        SaveLCCActive = LCCActive
                    End If

                End If

                If WhereAmIAt <> SaveWhereAmIAt Then

                    If OnProperty_Recording = True Then
                        OnPropertyMileage_Recording = OnPropertyMileage_Recording + (SaveODOReading - OnPropertyStartingODO_Recording)
                        CopyToLog("HandleOdometerRelatedStatus: OnPropertyMileage_Recording = " & OnPropertyMileage_Recording & "WhereAmIAt = " & WhereAmIAt & " WhereWasIAt = " & SaveWhereAmIAt)
                    ElseIf OnProperty_NotRecording = True Then
                        OnPropertyMileage_NotRecording = OnPropertyMileage_NotRecording + (SaveODOReading - OnPropertyStartingODO_NotRecording)
                        CopyToLog("HandleOdometerRelatedStatus: OnPropertyMileage_NotRecording = " & OnPropertyMileage_NotRecording & "WhereAmIAt = " & WhereAmIAt & " WhereWasIAt = " & SaveWhereAmIAt)
                    ElseIf OffProperty_Recording = True Then
                        OffPropertyMileage_Recording = OffPropertyMileage_Recording + (SaveODOReading - OffPropertyStartingODO_Recording)
                        CopyToLog("HandleOdometerRelatedStatus: OffPropertyMileage_Recording = " & OffPropertyMileage_Recording & "WhereAmIAt = " & WhereAmIAt & " WhereWasIAt = " & SaveWhereAmIAt)

                    ElseIf OffProperty_NotRecording = True Then
                        OffPropertyMileage_NotRecording = OffPropertyMileage_NotRecording + (SaveODOReading - OffPropertyStartingODO_NotRecording)
                        CopyToLog("HandleOdometerRelatedStatus: OffPropertyMileage_NotRecording = " & OffPropertyMileage_NotRecording & "WhereAmIAt = " & WhereAmIAt & " WhereWasIAt = " & SaveWhereAmIAt)

                    ElseIf Unknown_NotRecording = True Then
                        UnknownMileage_NotRecording = UnknownMileage_NotRecording + (SaveODOReading - UnknownStartingODO_NotRecording)
                        CopyToLog("HandleOdometerRelatedStatus: UnknownMileage_NotRecording = " & UnknownMileage_NotRecording & "WhereAmIAt = " & WhereAmIAt & " WhereWasIAt = " & SaveWhereAmIAt)

                    ElseIf Unknown_Recording = True Then
                        UnknownMileage_Recording = UnknownMileage_Recording + (SaveODOReading - UnknownStartingODO_Recording)
                        CopyToLog("HandleOdometerRelatedStatus: UnknownMileage_Recording = " & UnknownMileage_Recording & "WhereAmIAt = " & WhereAmIAt & " WhereWasIAt = " & SaveWhereAmIAt)

                    End If

                    If L_RecordingState = True And WhereAmIAt = "On Property" Then

                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = True WhereAmIAt = " & WhereAmIAt)
                        OnPropertyStartingODO_Recording = SaveODOReading

                        OnProperty_Recording = True
                        OnProperty_NotRecording = False
                        OffProperty_Recording = False
                        OffProperty_NotRecording = False
                        Unknown_Recording = False
                        Unknown_NotRecording = False

                    ElseIf L_RecordingState = True And WhereAmIAt = "Off Property" Then

                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = True WhereAmIAt = " & WhereAmIAt)
                        OffPropertyStartingODO_Recording = SaveODOReading

                        OffProperty_Recording = True
                        OnProperty_Recording = False
                        OnProperty_NotRecording = False
                        OffProperty_NotRecording = False
                        Unknown_Recording = False
                        Unknown_NotRecording = False

                    ElseIf L_RecordingState = False And WhereAmIAt = "On Property" Then

                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = False WhereAmIAt = " & WhereAmIAt)
                        OnPropertyStartingODO_NotRecording = SaveODOReading

                        OnProperty_NotRecording = True
                        OffProperty_Recording = False
                        OnProperty_Recording = False
                        OffProperty_NotRecording = False
                        Unknown_Recording = False
                        Unknown_NotRecording = False


                    ElseIf L_RecordingState = False And WhereAmIAt = "Off Property" Then

                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = False WhereAmIAt = " & WhereAmIAt)
                        OffPropertyStartingODO_NotRecording = SaveODOReading

                        OffProperty_NotRecording = True
                        OnProperty_NotRecording = False
                        OnProperty_Recording = False
                        OffProperty_Recording = False
                        Unknown_Recording = False
                        Unknown_NotRecording = False

                    ElseIf L_RecordingState = False And WhereAmIAt = "Unknown" Then

                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = False WhereAmIAt = " & WhereAmIAt)
                        UnknownStartingODO_NotRecording = SaveODOReading

                        Unknown_NotRecording = True
                        Unknown_Recording = False
                        OnProperty_NotRecording = False
                        OnProperty_Recording = False
                        OffProperty_Recording = False
                        OffProperty_NotRecording = False

                    ElseIf L_RecordingState = True And WhereAmIAt = "Unknown" Then

                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = True WhereAmIAt = " & WhereAmIAt)
                        UnknownStartingODO_Recording = SaveODOReading

                        Unknown_Recording = True
                        Unknown_NotRecording = False
                        OnProperty_NotRecording = False
                        OnProperty_Recording = False
                        OffProperty_Recording = False
                        OffProperty_NotRecording = False
                    Else
                        'CopyToLog("myBackgroundTasks: Invalid location transition")
                    End If

                    SaveWhereAmIAt = WhereAmIAt

                End If

                If L_RecordingState <> SaveRecordingState Then

                    If OnProperty_Recording = True Then
                        OnPropertyMileage_Recording = OnPropertyMileage_Recording + (SaveODOReading - OnPropertyStartingODO_Recording)
                        CopyToLog("HandleOdometerRelatedStatus: OnPropertyMileage_Recording = " & OnPropertyMileage_Recording & "L_RecordingState = " & L_RecordingState)
                    ElseIf OnProperty_NotRecording = True Then
                        OnPropertyMileage_NotRecording = OnPropertyMileage_NotRecording + (SaveODOReading - OnPropertyStartingODO_NotRecording)
                        CopyToLog("HandleOdometerRelatedStatus: OnPropertyMileage_NotRecording = " & OnPropertyMileage_NotRecording & "L_RecordingState = " & L_RecordingState)
                    ElseIf OffProperty_Recording = True Then
                        OffPropertyMileage_Recording = OffPropertyMileage_Recording + (SaveODOReading - OffPropertyStartingODO_Recording)
                        CopyToLog("HandleOdometerRelatedStatus: OffPropertyMileage_Recording = " & OffPropertyMileage_Recording & "L_RecordingState = " & L_RecordingState)
                    ElseIf OffProperty_NotRecording = True Then
                        OffPropertyMileage_NotRecording = OffPropertyMileage_NotRecording + (SaveODOReading - OffPropertyStartingODO_NotRecording)
                        CopyToLog("HandleOdometerRelatedStatus: OffPropertyMileage_NotRecording = " & OffPropertyMileage_NotRecording & "L_RecordingState = " & L_RecordingState)
                    ElseIf Unknown_NotRecording = True Then
                        UnknownMileage_NotRecording = UnknownMileage_NotRecording + (SaveODOReading - UnknownStartingODO_NotRecording)
                        CopyToLog("HandleOdometerRelatedStatus: UnknownMileage_NotRecording = " & UnknownMileage_NotRecording & "L_RecordingState = " & L_RecordingState)
                    ElseIf Unknown_Recording = True Then
                        UnknownMileage_Recording = UnknownMileage_Recording + (SaveODOReading - UnknownStartingODO_Recording)
                        CopyToLog("HandleOdometerRelatedStatus: UnknownMileage_Recording = " & UnknownMileage_Recording & "L_RecordingState = " & L_RecordingState)

                    End If

                    If L_RecordingState = True And WhereAmIAt = "On Property" Then

                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = " & L_RecordingState & " WhereAmIAt = " & WhereAmIAt)
                        OnPropertyStartingODO_Recording = SaveODOReading

                        OnProperty_Recording = True
                        OnProperty_NotRecording = False
                        OffProperty_Recording = False
                        OffProperty_NotRecording = False
                        Unknown_Recording = False
                        Unknown_NotRecording = False

                    ElseIf L_RecordingState = True And WhereAmIAt = "Off Property" Then
                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = " & L_RecordingState & " WhereAmIAt = " & WhereAmIAt)
                        OffPropertyStartingODO_Recording = SaveODOReading

                        OffProperty_Recording = True
                        OnProperty_Recording = False
                        OnProperty_NotRecording = False
                        OffProperty_NotRecording = False
                        Unknown_Recording = False
                        Unknown_NotRecording = False

                    ElseIf L_RecordingState = False And WhereAmIAt = "On Property" Then
                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = " & L_RecordingState & " WhereAmIAt = " & WhereAmIAt)
                        OnPropertyStartingODO_NotRecording = SaveODOReading

                        OnProperty_NotRecording = True
                        OffProperty_Recording = False
                        OnProperty_Recording = False
                        OffProperty_NotRecording = False
                        Unknown_Recording = False
                        Unknown_NotRecording = False

                    ElseIf L_RecordingState = False And WhereAmIAt = "Off Property" Then
                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = " & L_RecordingState & " WhereAmIAt = " & WhereAmIAt)
                        OffPropertyStartingODO_NotRecording = SaveODOReading

                        OffProperty_NotRecording = True
                        OnProperty_NotRecording = False
                        OnProperty_Recording = False
                        OffProperty_Recording = False
                        Unknown_Recording = False
                        Unknown_NotRecording = False

                    ElseIf L_RecordingState = False And WhereAmIAt = "Unknown" Then
                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = " & L_RecordingState & " WhereAmIAt = " & WhereAmIAt)
                        UnknownStartingODO_NotRecording = SaveODOReading

                        Unknown_NotRecording = True
                        Unknown_Recording = False
                        OnProperty_NotRecording = False
                        OnProperty_Recording = False
                        OffProperty_Recording = False
                        OffProperty_NotRecording = False

                    ElseIf L_RecordingState = True And WhereAmIAt = "Unknown" Then
                        CopyToLog("HandleOdometerRelatedStatus: RecordingState = " & L_RecordingState & " WhereAmIAt = " & WhereAmIAt)
                        UnknownStartingODO_Recording = SaveODOReading

                        Unknown_Recording = True
                        Unknown_NotRecording = False
                        OnProperty_NotRecording = False
                        OnProperty_Recording = False
                        OffProperty_Recording = False
                        OffProperty_NotRecording = False
                    Else
                        'CopyToLog("myBackgroundTasks: Invalid Record State / location transition")
                    End If

                    SaveRecordingState = L_RecordingState

                End If

            End If

        End If  'endof mileage handling...
    End Sub

    Private Sub HandleCLEVIRDisplayOfClusterMsgs(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks.
        'Displays LCC ClusterMessageText in a momentary UserInfoStatus message 

        Static MessageDisplayed As Boolean
        Dim LCC_ClusterMessageText As String
        Dim LCC_BUTTON_PRESS As Integer = 0
        Dim LCC_CLUSTER_MSG As Integer = 0
        Static Save_LCC_CLUSTER_MSG As Integer = 0
        Static mySaveTime4 As DateTime
        Static myElapseTime4 As TimeSpan


        LCC_CLUSTER_MSG = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LCC_CLUSTER_MSG, 1)).SignalData)
        LCC_BUTTON_PRESS = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LCC_BUTTON_PRESS, 1)).SignalData)

        'COMMENT OUT BEFORE BUILDING!!!!!!!!!!!!!!!!!!!!!

        'LCC_CLUSTER_MSG = GetRandom(0, 25)
        'LCC_BUTTON_PRESS = GetRandom(0, 2)
        'LCC_BUTTON_PRESS = 0
        'LCC_CLUSTER_MSG = 0

        If (((Save_LCC_CLUSTER_MSG <> LCC_CLUSTER_MSG) And Save_LCC_CLUSTER_MSG <> 0) Or LCC_BUTTON_PRESS > 0) And MessageDisplayed = False Then

            Save_LCC_CLUSTER_MSG = LCC_CLUSTER_MSG

            Select Case LCC_CLUSTER_MSG
                Case 0
                    'CeVGCR_e_LWI_NoIndication()
                    LCC_ClusterMessageText = ""
                Case 1
                    'CeVGCR_e_LWI_LaneLines()
                    LCC_ClusterMessageText = "Lane Lines"
                Case 2
                    'CeVGCR_e_LWI_TightCurve()
                    LCC_ClusterMessageText = "Tight Curve"
                Case 3

                    'CeVGCR_e_LWI_FreewayEnds()
                    LCC_ClusterMessageText = "Freeway Ends"
                Case 4

                    'CeVGCR_e_LWI_Construction()
                    LCC_ClusterMessageText = "Construction"
                Case 5

                    'CeVGCR_e_LWI_ExcessiveSpeed()
                    LCC_ClusterMessageText = "Excessive Speed"
                Case 6

                    'CeVGCR_e_LWI_VehicleProximity()
                    LCC_ClusterMessageText = "Vehicle Proximity"
                Case 7

                    'CeVGCR_e_LWI_CruiseDisengaged()
                    LCC_ClusterMessageText = "Cruise Disengaged"
                Case 8

                    'CeVGCR_e_LWI_Unavailable()
                    LCC_ClusterMessageText = "Unavailable"
                Case 9

                    'CeVGCR_e_LWI_AttentionUnknown()
                    LCC_ClusterMessageText = "Attention Unknown"
                Case 10

                    'CeVGCR_e_LWI_DueToWeather()
                    LCC_ClusterMessageText = "Due To Weather"
                Case 11

                    'CeVGCR_e_LWI_TakeStr()
                    LCC_ClusterMessageText = "Take Steering Wheel"
                Case 12

                    'CeVGCR_e_LWI_TakeVehicleCtl()
                    LCC_ClusterMessageText = "Take Vehicle Control"
                Case 13

                    'CeVGCR_e_LWI_ServDrvAsstSystm()
                    LCC_ClusterMessageText = "Service Driver Assist System"
                Case 14

                    'CeVGCR_e_LWI_LnFollowingLckdOut()
                    LCC_ClusterMessageText = "Lane Following Locked Out"
                Case 15
                    'CeVGCR_e_LWI_LaneEnding()
                    LCC_ClusterMessageText = "Lane Ending"
                Case 16
                    'CeVGCR_e_LWI_ExitLane()
                    LCC_ClusterMessageText = "Exit Lane"
                Case 17
                    'CeVGCR_e_LWI_GM_Authority()
                    LCC_ClusterMessageText = "GM Authority"
                Case 18
                    'CeVGCR_e_LWI_VehicleSetting()
                    LCC_ClusterMessageText = "Vehicle Setting"
                Case 19
                    'CeVGCR_e_LWI_AdaptiveCruise()
                    LCC_ClusterMessageText = "Adaptive Cruise"
                Case 20
                    'CeVGCR_e_LWI_ControlledAccess()
                    LCC_ClusterMessageText = "Controlled Access"
                Case 21
                    'CeVGCR_e_LWI_DrvrAttnOffRoad()
                    LCC_ClusterMessageText = "Driver Attention Off Road"
                Case 22
                    'CeVGCR_e_LWI_GPS_Unavailable()
                    LCC_ClusterMessageText = "GPS Unavailable"
                Case 23
                    'CeVGCR_e_LWI_DrvrAction()
                    LCC_ClusterMessageText = "Driver Action"
                Case 24
                    'CeVGCR_e_LWI_VehicleCenter()
                    LCC_ClusterMessageText = "Vehicle Center"
                Case 25
                    'CeVGCR_e_LWI_SensorImpaired()
                    LCC_ClusterMessageText = "Sensor Impaired"
                Case Else

                    LCC_ClusterMessageText = "Undefined"
            End Select

            If Len(LCC_ClusterMessageText) > 0 Then

                MessageDisplayed = True

                UserStatusInfo.Label1.Text = CStr(LCC_CLUSTER_MSG) & ": " & LCC_ClusterMessageText

                UserStatusInfo.Hide()
                UserStatusInfo.Show()
                UserStatusInfo.BringToFront()
                UserStatusInfo.Label1.Refresh()
                UserStatusInfo.Refresh()

                mySaveTime4 = Now

            End If

        End If

        If MessageDisplayed = True Then

            myElapseTime4 = Now.Subtract(mySaveTime4)

            If myElapseTime4.Seconds >= 3 Then

                UserStatusInfo.Hide()

                MessageDisplayed = False
            End If

        End If

    End Sub

    Private Sub HandleNANStatus(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks.
        'Alerts driver to a NAN fault...

        Dim mytempval As Double
        Dim myFormattedString As String

        If myDG.CS_NAN_STATUS > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_NAN_STATUS, 1)).SignalData

            'mytempval = 1.0 For Testing...

            myFormattedString = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_NAN_STATUS, 1), myDG.DeviceName(myDG.CS_NAN_STATUS, 1), myDG.VariableName(myDG.CS_NAN_STATUS, 1))

            If InStr(UCase(myFormattedString), "TRUE") > 0 And InSession = True Then

                System.Threading.Thread.Sleep(2500)
                UserStatusInfo.Label1.Text = "NAN ISSUE IS ACTIVE FOR THIS KEY CYCLE!  PLEASE PRESS STOP RECORD - EXIT - AND SLEEP OR SHUNT THE VEHICLE BEFORE CONTINUING!"
                System.Threading.Thread.Sleep(5000)
                UserStatusInfo.Label1.Text = ""
                UserStatusInfo.Hide()

            End If

        End If

    End Sub

    Public Sub HandleArrowDisplay(ByVal mycolorStr As String, ByVal myDirection As String)

        'Called from HandleALCDisplayElements.  Displays indications related to Automatic Lane Change on the Top Down View...

        Select Case myDirection

            Case "Left"

                Select Case mycolorStr

                    Case "Invisible"
                        myTDGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Gray"
                        myTDGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxGrayLeftArrow.BringToFront()
                        myTDGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Green"
                        myTDGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxGreenLeftArrow.BringToFront()
                        myTDGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Red"
                        myTDGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedLeftArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxRedLeftArrow.BringToFront()
                        myTDGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Yellow"
                        myTDGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxYellowLeftArrow.BringToFront()
                        myTDGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Blue"
                        myTDGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueRightArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxBlueRightArrow.BringToFront()
                End Select

            Case "Right"

                Select Case mycolorStr

                    Case "Invisible"
                        myTDGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Gray"
                        myTDGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayRightArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxGrayRightArrow.BringToFront()
                        myTDGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Green"
                        myTDGraphicsContainer.myPictureBoxGreenRightArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxGreenRightArrow.BringToFront()
                        myTDGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Red"
                        myTDGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedRightArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxRedRightArrow.BringToFront()
                        myTDGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Yellow"
                        myTDGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowRightArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxYellowRightArrow.BringToFront()
                        myTDGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Blue"
                        myTDGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        myTDGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = True
                        myTDGraphicsContainer.myPictureBoxBlueLeftArrow.BringToFront()

                End Select

        End Select

    End Sub

    Private Sub HandleALCDisplayElements(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks.
        'Handles display indications related to Automatic Lane Change on the Top Down View...

        'None
        'Driver
        'Merge
        'Route
        'Traffic

        'VeLFFR_e_LnChgDcsnRsn
        '0 -> 'CeLFFR_e_LnChgRsn_None'
        '1 -> 'CeLFFR_e_LnChgRsn_Driver'
        '2 -> 'CeLFFR_e_LnChgRsn_Merge'
        '3 -> 'CeLFFR_e_LnChgRsn_Route'
        '4 -> 'CeLFFR_e_LnChgRsn_Traffic'

        'VeLFFR_e_LnChgSt
        'Hex -> phys
        '0 -> 'CeLFFR_e_LnChg_Inactv' Invisible
        '1 -> 'CeLFFR_e_LnChg_Inhb'   Red
        '2 -> 'CeLFFR_e_LnChg_Standby'Gray
        '3 -> 'CeLFFR_e_LnChg_ChkLt' Yellow
        '4 -> 'CeLFFR_e_LnChg_ChkRt' Yellow
        '5 -> 'CeLFFR_e_LnChg_ChgLt' Green
        '6 -> 'CeLFFR_e_LnChg_ChgRt' Green
        '7 -> 'CeLFFR_e_LnChg_ReqCancel' Gray
        '8 -> 'CeLFFR_e_LnChg_NotFeasible' Red
        '9 -> 'CeLFFR_e_LnChg_ReqInvld' Red
        '10 -> 'CeLFFR_e_LnChg_Returning' Blue
        '11 -> 'CeLFFR_e_LnChg_Straddle'Red
        '12 -> 'CeLFFR_e_LnChg_DrvrOvrrd' Gray
        '13 -> 'CeLFFR_e_LnChg_Cmpt' Invisible

        Dim mytempval As Double

        Static tempstr1 As String = "0"
        Dim tempstr2 As String = "0"
        Static WhichSide As String = "None"

        Static ReasonLeft As String = "NONE"
        Static ReasonRight As String = "NONE"

        If myDG.CS_ALC_LANE_CHANGE_DCSN_RSN > 0 And myDG.CS_ALC_LANE_CHANGE_STATE > 0 Then

            If myTDGraphicsContainer.myALCStatusRightLabel.Visible = False Then

                myTDGraphicsContainer.myALCStatusRightLabel.Visible = True
                myTDGraphicsContainer.myALCStatusRightLabel.BringToFront()

                myTDGraphicsContainer.myALCStatusLeftLabel.Visible = True
                myTDGraphicsContainer.myALCStatusLeftLabel.BringToFront()

                myTDGraphicsContainer.myALCReasonRightLabel.Visible = True
                myTDGraphicsContainer.myALCReasonRightLabel.BringToFront()

                myTDGraphicsContainer.myALCReasonLeftLabel.Visible = True
                myTDGraphicsContainer.myALCReasonLeftLabel.BringToFront()

            End If

            If WhichSide = "None" Then

                ReasonLeft = "NONE"
                ReasonRight = "NONE"

                myTDGraphicsContainer.myALCReasonLeftLabel.Text = ReasonLeft
                myTDGraphicsContainer.myALCReasonRightLabel.Text = ReasonRight

                mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ALC_LANE_CHANGE_DCSN_RSN, 1)).SignalData
                'mytempval = GetRandom(0, 4) 'FOR TESTING ONLY!!!
                tempstr1 = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_ALC_LANE_CHANGE_DCSN_RSN, 1), myDG.DeviceName(myDG.CS_ALC_LANE_CHANGE_DCSN_RSN, 1), myDG.VariableName(myDG.CS_ALC_LANE_CHANGE_DCSN_RSN, 1))

            End If

            If InStr(tempstr1, "CeLFFR_e_LnChgRsn_None") > 0 Or tempstr1 = "0" Then

                If WhichSide = "None" Then
                    ReasonLeft = "NONE"
                    ReasonRight = "NONE"
                    myTDGraphicsContainer.myALCReasonLeftLabel.Text = ReasonLeft
                    myTDGraphicsContainer.myALCReasonRightLabel.Text = ReasonRight
                End If

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Driver") > 0 Or tempstr1 = "1" Then

                ReasonLeft = "DRIVER"
                ReasonRight = "DRIVER"

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Merge") > 0 Or tempstr1 = "2" Then

                ReasonLeft = "MERGE"
                ReasonRight = "MERGE"

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Route") > 0 Or tempstr1 = "3" Then

                ReasonLeft = "ROUTE"
                ReasonRight = "ROUTE"

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Traffic") > 0 Or tempstr1 = "4" Then

                ReasonLeft = "TRAFFIC"
                ReasonRight = "TRAFFIC"

            Else
                MsgBox("HandleALCDisplayElements: Invalid Enumeration for CS_ALC_LANE_CHANGE_DCSN_RSN")
            End If

            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ALC_LANE_CHANGE_STATE, 1)).SignalData
            'mytempval = GetRandom(0, 13) 'FOR TESTING ONLY!!!
            tempstr2 = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_ALC_LANE_CHANGE_STATE, 1), myDG.DeviceName(myDG.CS_ALC_LANE_CHANGE_STATE, 1), myDG.VariableName(myDG.CS_ALC_LANE_CHANGE_STATE, 1))

            If InStr(tempstr2, "CeLFFR_e_LnChg_Inactv") > 0 Then 'Invisible

                If WhichSide = "None" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "IN-ACTIVE"
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "IN-ACTIVE"

                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Inhb") > 0 Then 'Red

                If WhichSide = "None" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "INHIBIT"
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "INHIBIT"

                    HandleArrowDisplay("Red", "Left")
                    HandleArrowDisplay("Red", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Standby") > 0 Then 'Gray

                If WhichSide = "None" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "STAND BY"
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "STAND BY"

                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Gray", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChkLt") > 0 Then 'Yellow

                If WhichSide <> "Right" Then

                    myTDGraphicsContainer.myALCReasonRightLabel.Text = ""
                    myTDGraphicsContainer.myALCReasonLeftLabel.Text = ReasonLeft

                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "CHK LEFT"
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = ""
                    WhichSide = "Left"
                    HandleArrowDisplay("Yellow", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChkRt") > 0 Then 'Yellow

                If WhichSide <> "Left" Then

                    myTDGraphicsContainer.myALCReasonRightLabel.Text = ReasonRight
                    myTDGraphicsContainer.myALCReasonLeftLabel.Text = ""

                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = ""
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "CHK RIGHT"
                    WhichSide = "Right"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Yellow", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChgLt") > 0 Then 'Green

                If WhichSide <> "Right" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "CHG LEFT"
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = ""
                    WhichSide = "Left"
                    HandleArrowDisplay("Green", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChgRt") > 0 Then 'Green

                If WhichSide <> "Left" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = ""
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "CHG RIGHT"
                    WhichSide = "Right"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Green", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ReqCancel") > 0 Then 'Gray

                If WhichSide = "Left" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "REQ CANCEL"
                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If WhichSide = "Right" Then
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "REQ CANCEL"
                    HandleArrowDisplay("Gray", "Right")
                    HandleArrowDisplay("Invisible", "Left")
                End If

                WhichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_NotFeasible") > 0 Then 'Red
                myTDGraphicsContainer.myALCStatusLeftLabel.Text = "NOT FEAS"
                myTDGraphicsContainer.myALCStatusRightLabel.Text = "NOT FEAS"
                HandleArrowDisplay("Red", "Left")
                HandleArrowDisplay("Red", "Right")
                WhichSide = "None"
            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ReqInvld") > 0 Then 'Red

                If WhichSide = "Left" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "REQ INVALID"
                    HandleArrowDisplay("Red", "Left")
                End If
                If WhichSide = "Right" Then
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "REQ INVALID"
                    HandleArrowDisplay("Red", "Right")
                End If

                WhichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Returning") > 0 Then 'Blue

                If WhichSide = "Left" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "RETURNING"
                    HandleArrowDisplay("Blue", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If WhichSide = "Right" Then
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "RETURNING"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Blue", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Straddle") > 0 Then 'Red

                If WhichSide = "Left" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "STRADDLE"
                    HandleArrowDisplay("Red", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

                If WhichSide = "Right" Then
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "STRADDLE"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Red", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_DrvrOvrrd") > 0 Then 'Gray

                If WhichSide = "Left" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "DRVR OVRD"
                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If WhichSide = "Right" Then
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "DRVR OVRD"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Gray", "Right")
                End If

                WhichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Cmpt") > 0 Then 'Invisible

                If WhichSide = "Left" Then
                    myTDGraphicsContainer.myALCStatusLeftLabel.Text = "COMPLETE"
                    HandleArrowDisplay("Invisible", "Left")
                End If

                If WhichSide = "Left" Then
                    myTDGraphicsContainer.myALCStatusRightLabel.Text = "COMPLETE"
                    HandleArrowDisplay("Invisible", "Right")
                End If

                WhichSide = "None"
            Else
                MsgBox("HandleALCDisplayElements: Invalid Enumeration for CS_ALC_LANE_CHANGE_STATE")
            End If

        End If

    End Sub

    Private Sub HandleCoPilotStatusDisplay(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks.
        'Handles copilot status display...

        Dim mytempval As Double

        If myDG.CS_K1C_COPR_SYSSTAT > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_K1C_COPR_SYSSTAT, 1)).SignalData
            CopilotStatusDisplay.Label3.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_K1C_COPR_SYSSTAT, 1), myDG.DeviceName(myDG.CS_K1C_COPR_SYSSTAT, 1), myDG.VariableName(myDG.CS_K1C_COPR_SYSSTAT, 1))

            If InStr(CopilotStatusDisplay.Label3.Text, "CeCOPR_e_Operational") = 0 Then
                CopilotStatusDisplay.Label3.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label3.BackColor = Color.Green
            End If

        End If

        If myDG.CS_K2C_COPR_SYSSTAT > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_K2C_COPR_SYSSTAT, 1)).SignalData
            CopilotStatusDisplay.Label4.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_K2C_COPR_SYSSTAT, 1), myDG.DeviceName(myDG.CS_K2C_COPR_SYSSTAT, 1), myDG.VariableName(myDG.CS_K2C_COPR_SYSSTAT, 1))

            If InStr(CopilotStatusDisplay.Label4.Text, "CeCOPR_e_Operational") = 0 Then
                CopilotStatusDisplay.Label4.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label4.BackColor = Color.Green
            End If

        End If

        If myDG.CS_PriAutoBrkSysDrInfcStat > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PriAutoBrkSysDrInfcStat, 1)).SignalData
            CopilotStatusDisplay.Label14.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_PriAutoBrkSysDrInfcStat, 1), myDG.DeviceName(myDG.CS_PriAutoBrkSysDrInfcStat, 1), myDG.VariableName(myDG.CS_PriAutoBrkSysDrInfcStat, 1))

            If InStr(CopilotStatusDisplay.Label14.Text, "Driver Intervention Not Detected") = 0 Then
                CopilotStatusDisplay.Label14.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label14.BackColor = Color.Green
            End If
        End If

        If myDG.CS__PriAutoBrkSysDrInfcStatRed > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS__PriAutoBrkSysDrInfcStatRed, 1)).SignalData
            CopilotStatusDisplay.Label13.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS__PriAutoBrkSysDrInfcStatRed, 1), myDG.DeviceName(myDG.CS__PriAutoBrkSysDrInfcStatRed, 1), myDG.VariableName(myDG.CS__PriAutoBrkSysDrInfcStatRed, 1))

            If InStr(CopilotStatusDisplay.Label13.Text, "Driver Intervention Not Detected") = 0 Then
                CopilotStatusDisplay.Label13.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label13.BackColor = Color.Green
            End If
        End If

        If myDG.CS_SecAutoBrkSysDrInfcStat > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_SecAutoBrkSysDrInfcStat, 1)).SignalData
            CopilotStatusDisplay.Label18.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_SecAutoBrkSysDrInfcStat, 1), myDG.DeviceName(myDG.CS_SecAutoBrkSysDrInfcStat, 1), myDG.VariableName(myDG.CS_SecAutoBrkSysDrInfcStat, 1))

            If InStr(CopilotStatusDisplay.Label18.Text, "Driver Intervention Not Detected") = 0 Then
                CopilotStatusDisplay.Label18.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label18.BackColor = Color.Green
            End If
        End If

        If myDG.CS__SecAutoBrkSysDrInfcStatRed > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS__SecAutoBrkSysDrInfcStatRed, 1)).SignalData
            CopilotStatusDisplay.Label17.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS__SecAutoBrkSysDrInfcStatRed, 1), myDG.DeviceName(myDG.CS__SecAutoBrkSysDrInfcStatRed, 1), myDG.VariableName(myDG.CS__SecAutoBrkSysDrInfcStatRed, 1))

            If InStr(CopilotStatusDisplay.Label17.Text, "Driver Intervention Not Detected") = 0 Then
                CopilotStatusDisplay.Label17.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label17.BackColor = Color.Green
            End If
        End If

        If myDG.CS_CE_AutoStrgCmndStat > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CE_AutoStrgCmndStat, 1)).SignalData
            CopilotStatusDisplay.Label21.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_CE_AutoStrgCmndStat, 1), myDG.DeviceName(myDG.CS_CE_AutoStrgCmndStat, 1), myDG.VariableName(myDG.CS_CE_AutoStrgCmndStat, 1))

            If InStr(CopilotStatusDisplay.Label21.Text, "Active") = 0 Then
                CopilotStatusDisplay.Label21.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label21.BackColor = Color.Green
            End If
        End If

        If myDG.CS_HS_AutoStrgCmndStat > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HS_AutoStrgCmndStat, 1)).SignalData
            CopilotStatusDisplay.Label26.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HS_AutoStrgCmndStat, 1), myDG.DeviceName(myDG.CS_HS_AutoStrgCmndStat, 1), myDG.VariableName(myDG.CS_HS_AutoStrgCmndStat, 1))

            If InStr(CopilotStatusDisplay.Label26.Text, "Active") = 0 Then
                CopilotStatusDisplay.Label26.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label26.BackColor = Color.Green
            End If
        End If

        If myDG.CS_AutoPropAxlTrqArbStat > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AutoPropAxlTrqArbStat, 1)).SignalData
            CopilotStatusDisplay.Label25.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AutoPropAxlTrqArbStat, 1), myDG.DeviceName(myDG.CS_AutoPropAxlTrqArbStat, 1), myDG.VariableName(myDG.CS_AutoPropAxlTrqArbStat, 1))

            If InStr(CopilotStatusDisplay.Label25.Text, "Active") = 0 Then
                CopilotStatusDisplay.Label25.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label25.BackColor = Color.Green
            End If
        End If

        If myDG.CS_AATPCS_PropSysStat > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AATPCS_PropSysStat, 1)).SignalData
            CopilotStatusDisplay.Label29.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AATPCS_PropSysStat, 1), myDG.DeviceName(myDG.CS_AATPCS_PropSysStat, 1), myDG.VariableName(myDG.CS_AATPCS_PropSysStat, 1))

            If InStr(CopilotStatusDisplay.Label29.Text, "Full Function") = 0 Then
                CopilotStatusDisplay.Label29.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label29.BackColor = Color.Green
            End If
        End If

        If myDG.CS_PriBrkSysStat > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PriBrkSysStat, 1)).SignalData
            CopilotStatusDisplay.Label36.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_PriBrkSysStat, 1), myDG.DeviceName(myDG.CS_PriBrkSysStat, 1), myDG.VariableName(myDG.CS_PriBrkSysStat, 1))

            If InStr(CopilotStatusDisplay.Label36.Text, "Full Function") = 0 Then
                CopilotStatusDisplay.Label36.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label36.BackColor = Color.Green
            End If
        End If

        If myDG.CS__PriBrkSysStatRed > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS__PriBrkSysStatRed, 1)).SignalData
            CopilotStatusDisplay.Label35.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS__PriBrkSysStatRed, 1), myDG.DeviceName(myDG.CS__PriBrkSysStatRed, 1), myDG.VariableName(myDG.CS__PriBrkSysStatRed, 1))

            If InStr(CopilotStatusDisplay.Label35.Text, "Full Function") = 0 Then
                CopilotStatusDisplay.Label35.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label35.BackColor = Color.Green
            End If
        End If

        If myDG.CS_SecBrkSysStat > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_SecBrkSysStat, 1)).SignalData
            CopilotStatusDisplay.Label32.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_SecBrkSysStat, 1), myDG.DeviceName(myDG.CS_SecBrkSysStat, 1), myDG.VariableName(myDG.CS_SecBrkSysStat, 1))

            If InStr(CopilotStatusDisplay.Label32.Text, "Full Function") = 0 Then
                CopilotStatusDisplay.Label32.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label32.BackColor = Color.Green
            End If
        End If

        If myDG.CS__SecBrkSysStatRed > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS__SecBrkSysStatRed, 1)).SignalData
            CopilotStatusDisplay.Label31.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS__SecBrkSysStatRed, 1), myDG.DeviceName(myDG.CS__SecBrkSysStatRed, 1), myDG.VariableName(myDG.CS__SecBrkSysStatRed, 1))

            If InStr(CopilotStatusDisplay.Label31.Text, "Full Function") = 0 Then
                CopilotStatusDisplay.Label31.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label31.BackColor = Color.Green
            End If
        End If

        If myDG.CS_PriGdVolt > 0 Then

            CopilotStatusDisplay.Label44.Text = Format(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PriGdVolt, 1)).SignalData, "0.00")
            If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PriGdVolt, 1)).SignalData >= 13.6 Then

                CopilotStatusDisplay.Label44.BackColor = Color.Green
            Else

                CopilotStatusDisplay.Label44.BackColor = Color.Red
            End If
        End If

        If myDG.CS_SecGdVolt > 0 Then
            CopilotStatusDisplay.Label43.Text = Format(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PriGdVolt, 1)).SignalData, "0.00")
            If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_SecGdVolt, 1)).SignalData >= 13.6 Then

                CopilotStatusDisplay.Label43.BackColor = Color.Green
            Else

                CopilotStatusDisplay.Label43.BackColor = Color.Red
            End If
        End If

        If myDG.CS_SysPwrMd > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_SysPwrMd, 1)).SignalData
            CopilotStatusDisplay.Label40.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_SysPwrMd, 1), myDG.DeviceName(myDG.CS_SysPwrMd, 1), myDG.VariableName(myDG.CS_SysPwrMd, 1))

            If InStr(CopilotStatusDisplay.Label40.Text, "Run") = 0 Then
                CopilotStatusDisplay.Label40.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label40.BackColor = Color.Green
            End If
        End If

        If myDG.CS_PrplsnSysAtv > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PrplsnSysAtv, 1)).SignalData
            CopilotStatusDisplay.Label39.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_PrplsnSysAtv, 1), myDG.DeviceName(myDG.CS_PrplsnSysAtv, 1), myDG.VariableName(myDG.CS_PrplsnSysAtv, 1))

            If InStr(CopilotStatusDisplay.Label39.Text, "true") = 0 Then
                CopilotStatusDisplay.Label39.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label39.BackColor = Color.Green
            End If
        End If

        If myDG.CS_CE_VehMdMngrSt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CE_VehMdMngrSt, 1)).SignalData
            CopilotStatusDisplay.Label52.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_CE_VehMdMngrSt, 1), myDG.DeviceName(myDG.CS_CE_VehMdMngrSt, 1), myDG.VariableName(myDG.CS_CE_VehMdMngrSt, 1))

            If InStr(CopilotStatusDisplay.Label52.Text, "Autonomous Supervised") = 0 Then
                CopilotStatusDisplay.Label52.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label52.BackColor = Color.Green
            End If
        End If

        If myDG.CS_AutoBrkSysRdcPerDet > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AutoBrkSysRdcPerDet, 1)).SignalData
            CopilotStatusDisplay.Label48.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AutoBrkSysRdcPerDet, 1), myDG.DeviceName(myDG.CS_AutoBrkSysRdcPerDet, 1), myDG.VariableName(myDG.CS_AutoBrkSysRdcPerDet, 1))

            If InStr(CopilotStatusDisplay.Label48.Text, "false") = 0 Then
                CopilotStatusDisplay.Label48.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label48.BackColor = Color.Green
            End If
        End If

        If myDG.CS_ADIMCntrlFailed > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ADIMCntrlFailed, 1)).SignalData
            CopilotStatusDisplay.Label47.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_ADIMCntrlFailed, 1), myDG.DeviceName(myDG.CS_ADIMCntrlFailed, 1), myDG.VariableName(myDG.CS_ADIMCntrlFailed, 1))

            If InStr(CopilotStatusDisplay.Label47.Text, "false") = 0 Then
                CopilotStatusDisplay.Label47.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label47.BackColor = Color.Green
            End If
        End If

        If myDG.CS_RedVehMdMngrSt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RedVehMdMngrSt, 1)).SignalData
            CopilotStatusDisplay.Label64.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RedVehMdMngrSt, 1), myDG.DeviceName(myDG.CS_RedVehMdMngrSt, 1), myDG.VariableName(myDG.CS_RedVehMdMngrSt, 1))

            If InStr(CopilotStatusDisplay.Label64.Text, "Autonomous Supervised") = 0 Then
                CopilotStatusDisplay.Label64.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label64.BackColor = Color.Green
            End If
        End If

        If myDG.CS_PriCoPCmdMsgStat > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PriCoPCmdMsgStat, 1)).SignalData
            CopilotStatusDisplay.Label23.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_PriCoPCmdMsgStat, 1), myDG.DeviceName(myDG.CS_PriCoPCmdMsgStat, 1), myDG.VariableName(myDG.CS_PriCoPCmdMsgStat, 1))

            If InStr(CopilotStatusDisplay.Label23.Text, "Communication Normal") = 0 Then
                CopilotStatusDisplay.Label23.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label23.BackColor = Color.Green
            End If
        End If

        If myDG.CS_RedAutoBrkSysRdcPerDet > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RedAutoBrkSysRdcPerDet, 1)).SignalData
            CopilotStatusDisplay.Label60.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RedAutoBrkSysRdcPerDet, 1), myDG.DeviceName(myDG.CS_RedAutoBrkSysRdcPerDet, 1), myDG.VariableName(myDG.CS_RedAutoBrkSysRdcPerDet, 1))

            If InStr(CopilotStatusDisplay.Label60.Text, "false") = 0 Then
                CopilotStatusDisplay.Label60.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label60.BackColor = Color.Green
            End If
        End If

        If myDG.CS_RedADIMCntrlFailed > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RedADIMCntrlFailed, 1)).SignalData
            CopilotStatusDisplay.Label59.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RedADIMCntrlFailed, 1), myDG.DeviceName(myDG.CS_RedADIMCntrlFailed, 1), myDG.VariableName(myDG.CS_RedADIMCntrlFailed, 1))

            If InStr(CopilotStatusDisplay.Label59.Text, "false") = 0 Then
                CopilotStatusDisplay.Label59.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label59.BackColor = Color.Green
            End If
        End If

        If myDG.CS_VeTSTR_e_HiThreatObjType > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_VeTSTR_e_HiThreatObjType, 1)).SignalData
            CopilotStatusDisplay.Label58.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_VeTSTR_e_HiThreatObjType, 1), myDG.DeviceName(myDG.CS_VeTSTR_e_HiThreatObjType, 1), myDG.VariableName(myDG.CS_VeTSTR_e_HiThreatObjType, 1))

            If InStr(CopilotStatusDisplay.Label58.Text, "CeFSPR_e_4_Whl_Vhcl_car_sm_trk") = 0 Then
                CopilotStatusDisplay.Label58.BackColor = Color.Red
            Else
                CopilotStatusDisplay.Label58.BackColor = Color.Green
            End If
        End If

        If myDG.CS_VeTSTR_t_HiThreatTTC > 0 Then

            CopilotStatusDisplay.Label24.Text = Format(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_VeTSTR_t_HiThreatTTC, 1)).SignalData, "0.00")
            If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_VeTSTR_t_HiThreatTTC, 1)).SignalData < 10 Then 'seconds?

                CopilotStatusDisplay.Label24.BackColor = Color.Green
            Else

                CopilotStatusDisplay.Label24.BackColor = Color.Red
            End If
        End If
    End Sub

    Private Sub HandleLMFRStatusScreenHC(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks
        'Handles LMFR Status Screen display for High Content vehicles...

        Dim mytempval As Double

        If myDG.CS_HostLaneInx > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HostLaneInx, 1)).SignalData
            LMFR_Status_Screen_HC.Label20.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HostLaneInx, 1), myDG.DeviceName(myDG.CS_HostLaneInx, 1), myDG.VariableName(myDG.CS_HostLaneInx, 1))
        End If

        If myDG.CS_RawLaneLeft > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RawLaneLeft, 1)).SignalData
            LMFR_Status_Screen_HC.Label29.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RawLaneLeft, 1), myDG.DeviceName(myDG.CS_RawLaneLeft, 1), myDG.VariableName(myDG.CS_RawLaneLeft, 1))
        End If

        If InStr(UCase(LMFR_Status_Screen_HC.Label29.Text), "TRUE") > 0 Then
            LMFR_Status_Screen_HC.Label29.BackColor = Color.Green
        Else
            LMFR_Status_Screen_HC.Label29.BackColor = Color.White
        End If

        If myDG.CS_RawLaneRight > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RawLaneRight, 1)).SignalData
            LMFR_Status_Screen_HC.Label30.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RawLaneRight, 1), myDG.DeviceName(myDG.CS_RawLaneRight, 1), myDG.VariableName(myDG.CS_RawLaneRight, 1))
        End If

        If InStr(UCase(LMFR_Status_Screen_HC.Label30.Text), "TRUE") > 0 Then
            LMFR_Status_Screen_HC.Label30.BackColor = Color.Green
        Else
            LMFR_Status_Screen_HC.Label30.BackColor = Color.White
        End If

        If myDG.CS_AnchorSelect > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AnchorSelect, 1)).SignalData
            LMFR_Status_Screen_HC.Label9.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AnchorSelect, 1), myDG.DeviceName(myDG.CS_AnchorSelect, 1), myDG.VariableName(myDG.CS_AnchorSelect, 1))
        End If

        If InStr(LMFR_Status_Screen_HC.Label9.Text, "CeLMFR_e_NoAnchor") = 0 Then
            LMFR_Status_Screen_HC.Label9.BackColor = Color.Green
        Else
            LMFR_Status_Screen_HC.Label9.BackColor = Color.White
        End If

        If myDG.CS_LaneInvalid > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LaneInvalid, 1)).SignalData
            LMFR_Status_Screen_HC.Label54.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LaneInvalid, 1), myDG.DeviceName(myDG.CS_LaneInvalid, 1), myDG.VariableName(myDG.CS_LaneInvalid, 1))
        End If

        If InStr(UCase(LMFR_Status_Screen_HC.Label54.Text), "TRUE") > 0 Then
            LMFR_Status_Screen_HC.Label54.BackColor = Color.Red
        Else
            LMFR_Status_Screen_HC.Label54.BackColor = Color.White
        End If

        'CS_AlertUncertainLnLines

        If myDG.CS_AlertUncertainLnLines > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AlertUncertainLnLines, 1)).SignalData
            LMFR_Status_Screen_HC.Label13.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AlertUncertainLnLines, 1), myDG.DeviceName(myDG.CS_AlertUncertainLnLines, 1), myDG.VariableName(myDG.CS_AlertUncertainLnLines, 1))
        End If

        If InStr(UCase(LMFR_Status_Screen_HC.Label13.Text), "TRUE") > 0 Then
            LMFR_Status_Screen_HC.Label13.BackColor = Color.Red
        Else
            LMFR_Status_Screen_HC.Label13.BackColor = Color.White
        End If

        If myDG.CS_LaneWgtLt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LaneWgtLt, 1)).SignalData
            LMFR_Status_Screen_HC.Label57.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LaneWgtLt, 1), myDG.DeviceName(myDG.CS_LaneWgtLt, 1), myDG.VariableName(myDG.CS_LaneWgtLt, 1))

            If mytempval < 0.1 Then
                LMFR_Status_Screen_HC.Label57.BackColor = Color.Red
            Else
                LMFR_Status_Screen_HC.Label57.BackColor = Color.White
            End If

        End If

        If myDG.CS_LaneWgtRt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LaneWgtRt, 1)).SignalData
            LMFR_Status_Screen_HC.Label56.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LaneWgtRt, 1), myDG.DeviceName(myDG.CS_LaneWgtRt, 1), myDG.VariableName(myDG.CS_LaneWgtRt, 1))

            If mytempval < 0.1 Then
                LMFR_Status_Screen_HC.Label56.BackColor = Color.Red
            Else
                LMFR_Status_Screen_HC.Label56.BackColor = Color.White
            End If

        End If

        If myDG.CS_HPP_Wgt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HPP_Wgt, 1)).SignalData
            LMFR_Status_Screen_HC.Label55.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HPP_Wgt, 1), myDG.DeviceName(myDG.CS_HPP_Wgt, 1), myDG.VariableName(myDG.CS_HPP_Wgt, 1))

            If mytempval < 0.1 Then
                LMFR_Status_Screen_HC.Label55.BackColor = Color.Red
            Else
                LMFR_Status_Screen_HC.Label55.BackColor = Color.White
            End If

        End If

        If myDG.CS_PrevCoefWgt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PrevCoefWgt, 1)).SignalData
            LMFR_Status_Screen_HC.Label7.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_PrevCoefWgt, 1), myDG.DeviceName(myDG.CS_PrevCoefWgt, 1), myDG.VariableName(myDG.CS_PrevCoefWgt, 1))

            If mytempval < 0.1 Then
                LMFR_Status_Screen_HC.Label7.BackColor = Color.Red
            Else
                LMFR_Status_Screen_HC.Label7.BackColor = Color.White
            End If

        End If

        If myDG.CS_MapWgt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_MapWgt, 1)).SignalData
            LMFR_Status_Screen_HC.Label5.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_MapWgt, 1), myDG.DeviceName(myDG.CS_MapWgt, 1), myDG.VariableName(myDG.CS_MapWgt, 1))

            If mytempval < 0.1 Then
                LMFR_Status_Screen_HC.Label5.BackColor = Color.Red
            Else
                LMFR_Status_Screen_HC.Label5.BackColor = Color.White
            End If

        End If

        If myDG.CS_IFC_HeadingWgt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_IFC_HeadingWgt, 1)).SignalData
            LMFR_Status_Screen_HC.Label14.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_IFC_HeadingWgt, 1), myDG.DeviceName(myDG.CS_IFC_HeadingWgt, 1), myDG.VariableName(myDG.CS_IFC_HeadingWgt, 1))

            If mytempval > 0 Then
                LMFR_Status_Screen_HC.Label14.BackColor = Color.Green
            Else
                LMFR_Status_Screen_HC.Label14.BackColor = Color.White
            End If

        End If

        If myDG.CS_IMU_BlueLine > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_IMU_BlueLine, 1)).SignalData
            LMFR_Status_Screen_HC.Label11.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_IMU_BlueLine, 1), myDG.DeviceName(myDG.CS_IMU_BlueLine, 1), myDG.VariableName(myDG.CS_IMU_BlueLine, 1))


            If InStr(UCase(LMFR_Status_Screen_HC.Label11.Text), "TRUE") > 0 Then
                LMFR_Status_Screen_HC.Label29.BackColor = Color.Green
            Else
                LMFR_Status_Screen_HC.Label29.BackColor = Color.White
            End If

        End If

    End Sub

    Private Sub HandleLMFRStatusScreenGlobalA(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks
        'Handles LMFR Status Screen display for Global A (CSAV2) vehicles...

        Dim mytempval As Double

        Dim y0 As Double = 0
        Dim y1 As Double = 0
        Dim y2 As Double = 0
        Dim y3 As Double = 0

        Static ChartXIncrement As Double = 0

        If oscilloscope.Visible = True Then

            If myDG.CS_CntrlPtLatOffsetHPP > 0 Then
                y0 = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CntrlPtLatOffsetHPP, 1)).SignalData
            End If

            If myDG.CS_CntrlPtLatOffsetL > 0 Then
                y1 = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CntrlPtLatOffsetL, 1)).SignalData
            End If

            If myDG.CS_CntrlPtLatOffsetR > 0 Then
                y2 = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CntrlPtLatOffsetR, 1)).SignalData
            End If

            If myDG.CS_CntrlPtLatOffsetPrev > 0 Then
                y3 = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CntrlPtLatOffsetPrev, 1)).SignalData
            End If

            If Debugger.IsAttached = True Then

                y0 = oscilloscope.GetRandom(-2.0, 2.0)
                y1 = oscilloscope.GetRandom(-2.0, 2.0)
                y2 = oscilloscope.GetRandom(-2.0, 2.0)
                y3 = oscilloscope.GetRandom(-2.0, 2.0)

            End If

            oscilloscope.Chart1.Series(0).Points.AddXY(ChartXIncrement, y0)
            oscilloscope.Chart1.Series(1).Points.AddXY(ChartXIncrement, y1)
            oscilloscope.Chart1.Series(2).Points.AddXY(ChartXIncrement, y2)
            oscilloscope.Chart1.Series(3).Points.AddXY(ChartXIncrement, y3)

            If (oscilloscope.Chart1.Series(0).Points.Count > 200) Then
                oscilloscope.Chart1.Series(0).Points.RemoveAt(0)
            End If

            If (oscilloscope.Chart1.Series(1).Points.Count > 200) Then
                oscilloscope.Chart1.Series(1).Points.RemoveAt(0)
            End If

            If (oscilloscope.Chart1.Series(2).Points.Count > 200) Then
                oscilloscope.Chart1.Series(2).Points.RemoveAt(0)
            End If

            If (oscilloscope.Chart1.Series(3).Points.Count > 200) Then
                oscilloscope.Chart1.Series(3).Points.RemoveAt(0)
            End If

            oscilloscope.Chart1.ChartAreas(0).AxisX.Minimum = oscilloscope.Chart1.Series(0).Points(0).XValue
            oscilloscope.Chart1.ChartAreas(0).AxisX.Maximum = ChartXIncrement

            'x = x + 0.1
            ChartXIncrement = ChartXIncrement + 0.05

        Else
            ChartXIncrement = 0
        End If

        If myDG.CS_AtGradeAnchor > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AtGradeAnchor, 1)).SignalData
            LMFR_Status_Display_Global_A.Label31.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AtGradeAnchor, 1), myDG.DeviceName(myDG.CS_AtGradeAnchor, 1), myDG.VariableName(myDG.CS_AtGradeAnchor, 1))
        End If

        If myDG.CS_HostLaneIndexLeft > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HostLaneIndexLeft, 1)).SignalData
            LMFR_Status_Display_Global_A.Label24.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HostLaneIndexLeft, 1), myDG.DeviceName(myDG.CS_HostLaneIndexLeft, 1), myDG.VariableName(myDG.CS_HostLaneIndexLeft, 1))
        End If

        If myDG.CS_HostLaneIndexRight > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HostLaneIndexRight, 1)).SignalData
            LMFR_Status_Display_Global_A.Label23.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HostLaneIndexRight, 1), myDG.DeviceName(myDG.CS_HostLaneIndexRight, 1), myDG.VariableName(myDG.CS_HostLaneIndexRight, 1))
        End If

        If myDG.CS_MapHostLaneIndex > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_MapHostLaneIndex, 1)).SignalData
            LMFR_Status_Display_Global_A.Label22.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_MapHostLaneIndex, 1), myDG.DeviceName(myDG.CS_MapHostLaneIndex, 1), myDG.VariableName(myDG.CS_MapHostLaneIndex, 1))
        End If

        If myDG.CS_TargetsHostLaneIndex > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_TargetsHostLaneIndex, 1)).SignalData
            LMFR_Status_Display_Global_A.Label21.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_TargetsHostLaneIndex, 1), myDG.DeviceName(myDG.CS_TargetsHostLaneIndex, 1), myDG.VariableName(myDG.CS_TargetsHostLaneIndex, 1))
        End If

        If myDG.CS_DistToNextAtGradeXing > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_DistToNextAtGradeXing, 1)).SignalData
            LMFR_Status_Display_Global_A.Label49.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_DistToNextAtGradeXing, 1), myDG.DeviceName(myDG.CS_DistToNextAtGradeXing, 1), myDG.VariableName(myDG.CS_DistToNextAtGradeXing, 1))
        End If

        If myDG.CS_DistToRoadClassTrans > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_DistToRoadClassTrans, 1)).SignalData
            LMFR_Status_Display_Global_A.Label52.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_DistToRoadClassTrans, 1), myDG.DeviceName(myDG.CS_DistToRoadClassTrans, 1), myDG.VariableName(myDG.CS_DistToRoadClassTrans, 1))
        End If

        If myDG.CS_DistToNextTrfcCntrDev > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_DistToNextTrfcCntrDev, 1)).SignalData
            LMFR_Status_Display_Global_A.Label53.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_DistToNextTrfcCntrDev, 1), myDG.DeviceName(myDG.CS_DistToNextTrfcCntrDev, 1), myDG.VariableName(myDG.CS_DistToNextTrfcCntrDev, 1))
        End If

        If myDG.CS_OnFreeway > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_OnFreeway, 1)).SignalData
            LMFR_Status_Display_Global_A.Label42.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_OnFreeway, 1), myDG.DeviceName(myDG.CS_OnFreeway, 1), myDG.VariableName(myDG.CS_OnFreeway, 1))
        End If

        If InStr(UCase(LMFR_Status_Display_Global_A.Label42.Text), "TRUE") > 0 Then
            LMFR_Status_Display_Global_A.Label42.BackColor = Color.Green
        Else
            LMFR_Status_Display_Global_A.Label42.BackColor = Color.Red
        End If

        If myDG.CS_RoadClass_Crnt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RoadClass_Crnt, 1)).SignalData
            LMFR_Status_Display_Global_A.Label41.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RoadClass_Crnt, 1), myDG.DeviceName(myDG.CS_RoadClass_Crnt, 1), myDG.VariableName(myDG.CS_RoadClass_Crnt, 1))
        End If

        If myDG.CS_CmplxIntrsct_Prsnt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CmplxIntrsct_Prsnt, 1)).SignalData
            LMFR_Status_Display_Global_A.Label43.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_CmplxIntrsct_Prsnt, 1), myDG.DeviceName(myDG.CS_CmplxIntrsct_Prsnt, 1), myDG.VariableName(myDG.CS_CmplxIntrsct_Prsnt, 1))
        End If

        If InStr(UCase(LMFR_Status_Display_Global_A.Label43.Text), "TRUE") > 0 Then
            LMFR_Status_Display_Global_A.Label43.BackColor = Color.Red
        Else
            LMFR_Status_Display_Global_A.Label43.BackColor = Color.White
        End If

        If myDG.CS_HostLaneInx > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HostLaneInx, 1)).SignalData
            LMFR_Status_Display_Global_A.Label17.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HostLaneInx, 1), myDG.DeviceName(myDG.CS_HostLaneInx, 1), myDG.VariableName(myDG.CS_HostLaneInx, 1))
        End If


        If myDG.CS_NumLanes > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_NumLanes, 1)).SignalData
            LMFR_Status_Display_Global_A.Label18.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_NumLanes, 1), myDG.DeviceName(myDG.CS_NumLanes, 1), myDG.VariableName(myDG.CS_NumLanes, 1))

        End If

        If myDG.CS_NextNumLanes > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_NextNumLanes, 1)).SignalData
            LMFR_Status_Display_Global_A.Label19.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_NextNumLanes, 1), myDG.DeviceName(myDG.CS_NextNumLanes, 1), myDG.VariableName(myDG.CS_NextNumLanes, 1))

        End If

        If myDG.CS_HostLaneProbMax > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HostLaneProbMax, 1)).SignalData
            LMFR_Status_Display_Global_A.Label20.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HostLaneProbMax, 1), myDG.DeviceName(myDG.CS_HostLaneProbMax, 1), myDG.VariableName(myDG.CS_HostLaneProbMax, 1))
        End If

        If myDG.CS_RawLaneLeft > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RawLaneLeft, 1)).SignalData
            LMFR_Status_Display_Global_A.Label29.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RawLaneLeft, 1), myDG.DeviceName(myDG.CS_RawLaneLeft, 1), myDG.VariableName(myDG.CS_RawLaneLeft, 1))
        End If

        If InStr(UCase(LMFR_Status_Display_Global_A.Label29.Text), "TRUE") > 0 Then
            LMFR_Status_Display_Global_A.Label29.BackColor = Color.Green
        Else
            LMFR_Status_Display_Global_A.Label29.BackColor = Color.White
        End If

        If myDG.CS_RawLaneRight > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RawLaneRight, 1)).SignalData
            LMFR_Status_Display_Global_A.Label30.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RawLaneRight, 1), myDG.DeviceName(myDG.CS_RawLaneRight, 1), myDG.VariableName(myDG.CS_RawLaneRight, 1))
        End If

        If InStr(UCase(LMFR_Status_Display_Global_A.Label30.Text), "TRUE") > 0 Then
            LMFR_Status_Display_Global_A.Label30.BackColor = Color.Green
        Else
            LMFR_Status_Display_Global_A.Label30.BackColor = Color.White
        End If

        If myDG.CS_IntersectSplitMerge > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_IntersectSplitMerge, 1)).SignalData
            LMFR_Status_Display_Global_A.Label47.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_IntersectSplitMerge, 1), myDG.DeviceName(myDG.CS_IntersectSplitMerge, 1), myDG.VariableName(myDG.CS_IntersectSplitMerge, 1))
        End If

        If myDG.CS_DistToNumLanesTrans > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_DistToNumLanesTrans, 1)).SignalData
            LMFR_Status_Display_Global_A.Label50.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_DistToNumLanesTrans, 1), myDG.DeviceName(myDG.CS_DistToNumLanesTrans, 1), myDG.VariableName(myDG.CS_DistToNumLanesTrans, 1))
        End If

        If myDG.CS_LCC_RedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LCC_RedReq, 1)).SignalData
            LMFR_Status_Display_Global_A.Label35.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LCC_RedReq, 1), myDG.DeviceName(myDG.CS_LCC_RedReq, 1), myDG.VariableName(myDG.CS_LCC_RedReq, 1))
        End If

        If InStr(LMFR_Status_Display_Global_A.Label35.Text, "CeVGCR_e_LCRR_Inactive") = 0 Then
            LMFR_Status_Display_Global_A.Label35.BackColor = Color.Red
        Else
            LMFR_Status_Display_Global_A.Label35.BackColor = Color.White
        End If

        If myDG.CS_LnWgtRedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LnWgtRedReq, 1)).SignalData
            LMFR_Status_Display_Global_A.Label34.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LnWgtRedReq, 1), myDG.DeviceName(myDG.CS_LnWgtRedReq, 1), myDG.VariableName(myDG.CS_LnWgtRedReq, 1))

        End If

        If InStr(LMFR_Status_Display_Global_A.Label34.Text, "CeVGCR_e_LCRR_Inactive") = 0 Then
            LMFR_Status_Display_Global_A.Label34.BackColor = Color.Red
        Else
            LMFR_Status_Display_Global_A.Label34.BackColor = Color.White
        End If

        If myDG.CS_MapAvailRedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_MapAvailRedReq, 1)).SignalData
            LMFR_Status_Display_Global_A.Label33.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_MapAvailRedReq, 1), myDG.DeviceName(myDG.CS_MapAvailRedReq, 1), myDG.VariableName(myDG.CS_MapAvailRedReq, 1))
        End If

        If InStr(LMFR_Status_Display_Global_A.Label33.Text, "CeVGCR_e_LCRR_Inactive") = 0 Then
            LMFR_Status_Display_Global_A.Label33.BackColor = Color.Red
        Else
            LMFR_Status_Display_Global_A.Label33.BackColor = Color.White
        End If

        If myDG.CS_TmpLnRedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_TmpLnRedReq, 1)).SignalData
            LMFR_Status_Display_Global_A.Label32.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_TmpLnRedReq, 1), myDG.DeviceName(myDG.CS_TmpLnRedReq, 1), myDG.VariableName(myDG.CS_TmpLnRedReq, 1))
        End If

        If InStr(LMFR_Status_Display_Global_A.Label32.Text, "CeVGCR_e_LCRR_Inactive") = 0 Then
            LMFR_Status_Display_Global_A.Label32.BackColor = Color.Red
        Else
            LMFR_Status_Display_Global_A.Label32.BackColor = Color.White
        End If

        If myDG.CS_LaneWgtLt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LaneWgtLt, 1)).SignalData
            LMFR_Status_Display_Global_A.Label57.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LaneWgtLt, 1), myDG.DeviceName(myDG.CS_LaneWgtLt, 1), myDG.VariableName(myDG.CS_LaneWgtLt, 1))

            If mytempval < 0.1 Then
                LMFR_Status_Display_Global_A.Label57.BackColor = Color.Red
            Else
                LMFR_Status_Display_Global_A.Label57.BackColor = Color.White
            End If

        End If

        If myDG.CS_LaneWgtRt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LaneWgtRt, 1)).SignalData
            LMFR_Status_Display_Global_A.Label56.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LaneWgtRt, 1), myDG.DeviceName(myDG.CS_LaneWgtRt, 1), myDG.VariableName(myDG.CS_LaneWgtRt, 1))

            If mytempval < 0.1 Then
                LMFR_Status_Display_Global_A.Label56.BackColor = Color.Red
            Else
                LMFR_Status_Display_Global_A.Label56.BackColor = Color.White
            End If

        End If

        If myDG.CS_HPP_Wgt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HPP_Wgt, 1)).SignalData
            LMFR_Status_Display_Global_A.Label55.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HPP_Wgt, 1), myDG.DeviceName(myDG.CS_HPP_Wgt, 1), myDG.VariableName(myDG.CS_HPP_Wgt, 1))

            If mytempval < 0.1 Then
                LMFR_Status_Display_Global_A.Label55.BackColor = Color.Red
            Else
                LMFR_Status_Display_Global_A.Label55.BackColor = Color.White
            End If

        End If

        If myDG.CS_LaneInvalid > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LaneInvalid, 1)).SignalData
            LMFR_Status_Display_Global_A.Label54.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LaneInvalid, 1), myDG.DeviceName(myDG.CS_LaneInvalid, 1), myDG.VariableName(myDG.CS_LaneInvalid, 1))
        End If

        If InStr(UCase(LMFR_Status_Display_Global_A.Label56.Text), "TRUE") > 0 Then
            LMFR_Status_Display_Global_A.Label54.BackColor = Color.Red
        Else
            LMFR_Status_Display_Global_A.Label54.BackColor = Color.White
        End If

        If myDG.CS_DistToNextIntersect > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_DistToNextIntersect, 1)).SignalData
            LMFR_Status_Display_Global_A.Label48.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_DistToNextIntersect, 1), myDG.DeviceName(myDG.CS_DistToNextIntersect, 1), myDG.VariableName(myDG.CS_DistToNextIntersect, 1))
        End If
    End Sub

    Private Sub HandleFusionStatusDisplay(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks
        'Handles Fusion Status Screen display...

        Dim mytempval As Double

        If myDG.CS_HostLaneInx > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HostLaneInx, 1)).SignalData
            FusionStatusDisplay.Label7.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HostLaneInx, 1), myDG.DeviceName(myDG.CS_HostLaneInx, 1), myDG.VariableName(myDG.CS_HostLaneInx, 1))
        End If


        If myDG.CS_NumLanes > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_NumLanes, 1)).SignalData
            FusionStatusDisplay.Label21.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_NumLanes, 1), myDG.DeviceName(myDG.CS_NumLanes, 1), myDG.VariableName(myDG.CS_NumLanes, 1))

        End If

        If myDG.CS_NextNumLanes > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_NextNumLanes, 1)).SignalData
            FusionStatusDisplay.Label23.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_NextNumLanes, 1), myDG.DeviceName(myDG.CS_NextNumLanes, 1), myDG.VariableName(myDG.CS_NextNumLanes, 1))

        End If

        If myDG.CS_HostLaneProbMax > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HostLaneProbMax, 1)).SignalData
            FusionStatusDisplay.Label48.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HostLaneProbMax, 1), myDG.DeviceName(myDG.CS_HostLaneProbMax, 1), myDG.VariableName(myDG.CS_HostLaneProbMax, 1))
        End If

        If myDG.CS_RawLaneLeft > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RawLaneLeft, 1)).SignalData
            FusionStatusDisplay.Label32.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RawLaneLeft, 1), myDG.DeviceName(myDG.CS_RawLaneLeft, 1), myDG.VariableName(myDG.CS_RawLaneLeft, 1))
        End If

        If myDG.CS_RawLaneRight > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_RawLaneRight, 1)).SignalData
            FusionStatusDisplay.Label30.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_RawLaneRight, 1), myDG.DeviceName(myDG.CS_RawLaneRight, 1), myDG.VariableName(myDG.CS_RawLaneRight, 1))
        End If

        If myDG.CS_IntersectSplitMerge > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_IntersectSplitMerge, 1)).SignalData
            FusionStatusDisplay.Label15.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_IntersectSplitMerge, 1), myDG.DeviceName(myDG.CS_IntersectSplitMerge, 1), myDG.VariableName(myDG.CS_IntersectSplitMerge, 1))
        End If

        If myDG.CS_DistToNumLanesTrans > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_DistToNumLanesTrans, 1)).SignalData
            FusionStatusDisplay.Label27.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_DistToNumLanesTrans, 1), myDG.DeviceName(myDG.CS_DistToNumLanesTrans, 1), myDG.VariableName(myDG.CS_DistToNumLanesTrans, 1))
        End If

        If myDG.CS_LCC_RedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LCC_RedReq, 1)).SignalData
            FusionStatusDisplay.Label46.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LCC_RedReq, 1), myDG.DeviceName(myDG.CS_LCC_RedReq, 1), myDG.VariableName(myDG.CS_LCC_RedReq, 1))
        End If

        If myDG.CS_LnWgtRedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LnWgtRedReq, 1)).SignalData
            FusionStatusDisplay.Label42.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LnWgtRedReq, 1), myDG.DeviceName(myDG.CS_LnWgtRedReq, 1), myDG.VariableName(myDG.CS_LnWgtRedReq, 1))

        End If

        If myDG.CS_MapAvailRedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_MapAvailRedReq, 1)).SignalData
            FusionStatusDisplay.Label40.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_MapAvailRedReq, 1), myDG.DeviceName(myDG.CS_MapAvailRedReq, 1), myDG.VariableName(myDG.CS_MapAvailRedReq, 1))
        End If

        If myDG.CS_TmpLnRedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_TmpLnRedReq, 1)).SignalData
            FusionStatusDisplay.Label36.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_TmpLnRedReq, 1), myDG.DeviceName(myDG.CS_TmpLnRedReq, 1), myDG.VariableName(myDG.CS_TmpLnRedReq, 1))
        End If

        If myDG.CS_LaneWgtLt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LaneWgtLt, 1)).SignalData
            FusionStatusDisplay.Label54.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LaneWgtLt, 1), myDG.DeviceName(myDG.CS_LaneWgtLt, 1), myDG.VariableName(myDG.CS_LaneWgtLt, 1))
        End If

        If myDG.CS_LaneWgtRt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LaneWgtRt, 1)).SignalData
            FusionStatusDisplay.Label52.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LaneWgtRt, 1), myDG.DeviceName(myDG.CS_LaneWgtRt, 1), myDG.VariableName(myDG.CS_LaneWgtRt, 1))
        End If

        If myDG.CS_HPP_Wgt > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HPP_Wgt, 1)).SignalData
            FusionStatusDisplay.Label50.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HPP_Wgt, 1), myDG.DeviceName(myDG.CS_HPP_Wgt, 1), myDG.VariableName(myDG.CS_HPP_Wgt, 1))
        End If

        If myDG.CS_LaneInvalid > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LaneInvalid, 1)).SignalData
            FusionStatusDisplay.Label56.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LaneInvalid, 1), myDG.DeviceName(myDG.CS_LaneInvalid, 1), myDG.VariableName(myDG.CS_LaneInvalid, 1))
        End If

        If myDG.CS_DistToNextIntersect > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_DistToNextIntersect, 1)).SignalData
            FusionStatusDisplay.Label17.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_DistToNextIntersect, 1), myDG.DeviceName(myDG.CS_DistToNextIntersect, 1), myDG.VariableName(myDG.CS_DistToNextIntersect, 1))
        End If

        If myDG.CS_HCURVE > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_HCURVE, 1)).SignalData
            FusionStatusDisplay.Label9.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_HCURVE, 1), myDG.DeviceName(myDG.CS_HCURVE, 1), myDG.VariableName(myDG.CS_HCURVE, 1))
        End If

        If myDG.CS_VehPathEstCurv > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_VehPathEstCurv, 1)).SignalData
            FusionStatusDisplay.Label11.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_VehPathEstCurv, 1), myDG.DeviceName(myDG.CS_VehPathEstCurv, 1), myDG.VariableName(myDG.CS_VehPathEstCurv, 1))
        End If

        If myDG.CS_Curvature > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_Curvature, 1)).SignalData
            FusionStatusDisplay.Label13.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_Curvature, 1), myDG.DeviceName(myDG.CS_Curvature, 1), myDG.VariableName(myDG.CS_Curvature, 1))
        End If

        If myDG.CS_SpltMrgLaneNum > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_SpltMrgLaneNum, 1)).SignalData
            FusionStatusDisplay.Label19.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_SpltMrgLaneNum, 1), myDG.DeviceName(myDG.CS_SpltMrgLaneNum, 1), myDG.VariableName(myDG.CS_SpltMrgLaneNum, 1))
        End If

        If myDG.CS__NumLanesTrans > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS__NumLanesTrans, 1)).SignalData
            FusionStatusDisplay.Label25.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS__NumLanesTrans, 1), myDG.DeviceName(myDG.CS__NumLanesTrans, 1), myDG.VariableName(myDG.CS__NumLanesTrans, 1))
        End If

        If myDG.CS_PathConf > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PathConf, 1)).SignalData
            FusionStatusDisplay.Label8.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_PathConf, 1), myDG.DeviceName(myDG.CS_PathConf, 1), myDG.VariableName(myDG.CS_PathConf, 1))
        End If

        If myDG.CS_LnSplitRedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LnSplitRedReq, 1)).SignalData
            FusionStatusDisplay.Label44.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LnSplitRedReq, 1), myDG.DeviceName(myDG.CS_LnSplitRedReq, 1), myDG.VariableName(myDG.CS_LnSplitRedReq, 1))
        End If

        If myDG.CS_PathConfRedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PathConfRedReq, 1)).SignalData
            FusionStatusDisplay.Label38.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_PathConfRedReq, 1), myDG.DeviceName(myDG.CS_PathConfRedReq, 1), myDG.VariableName(myDG.CS_PathConfRedReq, 1))
        End If

        If myDG.CS_VPMConfRedReq > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_VPMConfRedReq, 1)).SignalData
            FusionStatusDisplay.Label34.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_VPMConfRedReq, 1), myDG.DeviceName(myDG.CS_VPMConfRedReq, 1), myDG.VariableName(myDG.CS_VPMConfRedReq, 1))
        End If

        If myDG.CS_AlertUncertainLnLines > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AlertUncertainLnLines, 1)).SignalData
            FusionStatusDisplay.Label59.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AlertUncertainLnLines, 1), myDG.DeviceName(myDG.CS_AlertUncertainLnLines, 1), myDG.VariableName(myDG.CS_AlertUncertainLnLines, 1))
        End If

        If myDG.CS_AlertExitLane > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AlertExitLane, 1)).SignalData
            FusionStatusDisplay.Label61.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AlertExitLane, 1), myDG.DeviceName(myDG.CS_AlertExitLane, 1), myDG.VariableName(myDG.CS_AlertExitLane, 1))
        End If

        If myDG.CS_AlertLaneEnding > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AlertLaneEnding, 1)).SignalData
            FusionStatusDisplay.Label63.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AlertLaneEnding, 1), myDG.DeviceName(myDG.CS_AlertLaneEnding, 1), myDG.VariableName(myDG.CS_AlertLaneEnding, 1))
        End If

        If myDG.CS_AlertMapUnavail > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AlertMapUnavail, 1)).SignalData
            FusionStatusDisplay.Label65.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AlertMapUnavail, 1), myDG.DeviceName(myDG.CS_AlertMapUnavail, 1), myDG.VariableName(myDG.CS_AlertMapUnavail, 1))
        End If

        If myDG.CS_AlertOther > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AlertOther, 1)).SignalData
            FusionStatusDisplay.Label67.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_AlertOther, 1), myDG.DeviceName(myDG.CS_AlertOther, 1), myDG.VariableName(myDG.CS_AlertOther, 1))
        End If

        If myDG.CS_LnNarrowing > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LnNarrowing, 1)).SignalData
            FusionStatusDisplay.Label69.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LnNarrowing, 1), myDG.DeviceName(myDG.CS_LnNarrowing, 1), myDG.VariableName(myDG.CS_LnNarrowing, 1))
        End If

        If myDG.CS_LnWidening > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LnWidening, 1)).SignalData
            FusionStatusDisplay.Label71.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_LnWidening, 1), myDG.DeviceName(myDG.CS_LnWidening, 1), myDG.VariableName(myDG.CS_LnWidening, 1))
        End If

        If myDG.CS_ObjectLeft > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ObjectLeft, 1)).SignalData
            FusionStatusDisplay.Label75.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_ObjectLeft, 1), myDG.DeviceName(myDG.CS_ObjectLeft, 1), myDG.VariableName(myDG.CS_ObjectLeft, 1))

            If InStr(UCase(FusionStatusDisplay.Label75.Text), "TRUE") > 0 Then
                FusionStatusDisplay.Label75.BackColor = Color.Green
            Else
                FusionStatusDisplay.Label75.BackColor = Color.White
            End If

        End If

        If myDG.CS_ObjectRight > 0 Then
            mytempval = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ObjectRight, 1)).SignalData
            FusionStatusDisplay.Label73.Text = FormatDisplayString(mytempval, myDG.DisplayFormat(myDG.CS_ObjectRight, 1), myDG.DeviceName(myDG.CS_ObjectRight, 1), myDG.VariableName(myDG.CS_ObjectRight, 1))

            If InStr(UCase(FusionStatusDisplay.Label73.Text), "TRUE") > 0 Then
                FusionStatusDisplay.Label73.BackColor = Color.Green
            Else
                FusionStatusDisplay.Label73.BackColor = Color.White
            End If

        End If

    End Sub

    Private Sub HandleLKACustomDisplay(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks
        'Handles LKA Status Screen display...

        Static SAVE_LKA_DistRtLnEdge As Double
        Static SAVE_LKA_DistLtLnEdge As Double

        If myDG.CS_LNSNS_DISTTOLLNEDGE > 0 And
              myDG.CS_LNSNS_DISTTORLNEDGE > 0 Then

            SAVE_LKA_DistRtLnEdge = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LNSNS_DISTTORLNEDGE, 1)).SignalData
            SAVE_LKA_DistLtLnEdge = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LNSNS_DISTTOLLNEDGE, 1)).SignalData

        End If

        If LKAForm.LKA_Active = True Then
            LKAForm.Label17.Text = "LKA TORQ"
            LKAForm.Label17.BackColor = Color.Green
        Else
            LKAForm.Label17.Text = "NO LKA TORQ"
            LKAForm.Label17.BackColor = Color.Red
        End If

        If myDG.CS_LKA_OFFIND > 0 Then
            If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_OFFIND, 1)).SignalData = 0 Then
                LKAForm.Label20.Text = "LKA ON"
                LKAForm.Label20.BackColor = Color.Green
            Else
                LKAForm.Label20.Text = "LKA OFF"
                LKAForm.Label20.BackColor = Color.Red
            End If

        End If

        If myDG.CS_LKA_STDBYIND > 0 Then
            If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_STDBYIND, 1)).SignalData > 0 Then
                LKAForm.Label19.Text = "LKA IN STDBY"
                LKAForm.Label19.BackColor = Color.Green
            Else
                LKAForm.Label19.Text = "LKA STDBY OFF"
                LKAForm.Label19.BackColor = Color.Red
            End If

        End If

        If myDG.CS_LKA_ACTVIND > 0 Then
            If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_ACTVIND, 1)).SignalData > 0 Then
                LKAForm.Label18.Text = "LKA ACTV"
                LKAForm.Label18.BackColor = Color.Green
            Else
                LKAForm.Label18.Text = "LKA NOT ACTV"
                LKAForm.Label18.BackColor = Color.Red
            End If

        End If

        If myDG.CS_LKA_DRVRAPPLDTRQ > 0 And
    myDG.CS_LKA_TORQUE > 0 And
    myDG.CS_LKA_TRQRQACT > 0 Then

            If LKAForm.LKA_Active = False Then

                If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TRQRQACT, 1)).SignalData > 0 And
           System.Math.Abs(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData) >= 0.5 Then

                    LKAForm.LKA_Active_Transition_On = True
                    LKAForm.LKA_Active = True
                    LKAForm.LKA_InitDistRtLnEdge = SAVE_LKA_DistRtLnEdge
                    LKAForm.LKA_InitDistLtLnEdge = SAVE_LKA_DistLtLnEdge
                    LKAForm.Label1.Text = Format(LKAForm.LKA_InitDistRtLnEdge, "0.000")
                    LKAForm.Label3.Text = Format(LKAForm.LKA_InitDistLtLnEdge, "0.000")

                    LKAForm.LKA_MinDistRtLnEdge = LKAForm.LKA_InitDistRtLnEdge
                    LKAForm.LKA_MinDistLtLnEdge = LKAForm.LKA_InitDistLtLnEdge
                    LKAForm.Label8.Text = Format(LKAForm.LKA_MinDistRtLnEdge, "0.000")
                    LKAForm.Label6.Text = Format(LKAForm.LKA_MinDistLtLnEdge, "0.000")

                    'Left positive, right negative

                    If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData > 0 Then
                        LKAForm.LKA_MaxDRVRTorqueLeft = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData
                        LKAForm.LKA_MaxDRVRTorqueRight = 0
                    ElseIf mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData < 0 Then
                        LKAForm.LKA_MaxDRVRTorqueRight = System.Math.Abs(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData)
                        LKAForm.LKA_MaxDRVRTorqueLeft = 0
                    Else
                        LKAForm.LKA_MaxDRVRTorqueRight = 0
                        LKAForm.LKA_MaxDRVRTorqueLeft = 0
                    End If

                    LKAForm.Label14.Text = Format(LKAForm.LKA_MaxDRVRTorqueRight, "0.000")
                    LKAForm.Label16.Text = Format(LKAForm.LKA_MaxDRVRTorqueLeft, "0.000")


                    If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData > 0 Then
                        LKAForm.LKA_MaxLKATorqueLeft = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData
                        LKAForm.LKA_MaxLKATorqueRight = 0
                    ElseIf mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData < 0 Then
                        LKAForm.LKA_MaxLKATorqueRight = System.Math.Abs(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData)
                        LKAForm.LKA_MaxLKATorqueLeft = 0
                    Else
                        LKAForm.LKA_MaxLKATorqueRight = 0
                        LKAForm.LKA_MaxLKATorqueLeft = 0
                    End If

                    LKAForm.Label10.Text = Format(LKAForm.LKA_MaxLKATorqueRight, "0.000")
                    LKAForm.Label12.Text = Format(LKAForm.LKA_MaxLKATorqueLeft, "0.000")

                End If

            Else 'LKA Active = True

                If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TRQRQACT, 1)).SignalData = 0 Or
           System.Math.Abs(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData) < 0.5 Then
                    LKAForm.LKA_Active = False
                    LKAForm.LKA_Active_Transition_Off = True

                Else

                    If SAVE_LKA_DistRtLnEdge < LKAForm.LKA_MinDistRtLnEdge Then
                        LKAForm.LKA_MinDistRtLnEdge = SAVE_LKA_DistRtLnEdge
                        LKAForm.Label8.Text = Format(LKAForm.LKA_MinDistRtLnEdge, "0.000")
                    End If

                    If SAVE_LKA_DistLtLnEdge < LKAForm.LKA_MinDistLtLnEdge Then
                        LKAForm.LKA_MinDistLtLnEdge = SAVE_LKA_DistLtLnEdge
                        LKAForm.Label6.Text = Format(LKAForm.LKA_MinDistLtLnEdge, "0.000")
                    End If

                    If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData > LKAForm.LKA_MaxDRVRTorqueLeft Then
                        LKAForm.LKA_MaxDRVRTorqueLeft = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData
                        LKAForm.Label16.Text = Format(LKAForm.LKA_MaxDRVRTorqueLeft, "0.000")
                    End If

                    If System.Math.Abs(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData) > LKAForm.LKA_MaxDRVRTorqueRight Then
                        LKAForm.LKA_MaxDRVRTorqueRight = System.Math.Abs(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData)
                        LKAForm.Label14.Text = Format(LKAForm.LKA_MaxDRVRTorqueRight, "0.000")
                    End If

                    If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData > LKAForm.LKA_MaxLKATorqueLeft Then
                        LKAForm.LKA_MaxLKATorqueLeft = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData
                        LKAForm.Label12.Text = Format(LKAForm.LKA_MaxLKATorqueLeft, "0.000")
                    End If

                    If System.Math.Abs(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData) > LKAForm.LKA_MaxLKATorqueRight Then
                        LKAForm.LKA_MaxLKATorqueRight = System.Math.Abs(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_LKA_TORQUE, 1)).SignalData)
                        LKAForm.Label10.Text = Format(LKAForm.LKA_MaxLKATorqueRight, "0.000")
                    End If

                    LKAForm.LKA_Active_Transition_On = False

                End If
            End If
        End If

    End Sub

    Private Sub HandleTargetStatusDisplay(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks
        'Handles Target Status Screen display (Secret Squirrel Screen)...

        Dim FOAI_VAIR As Integer
        Dim FOAI_AWIR As Integer
        Dim COLPRSYSBRKPRFREQ As Integer

        Static Save_FOAI_VAIR_Value As Integer
        Static Save_FOAI_AWIR_Value As Integer
        Static Save_AUTOBRKREQ_Value As Integer
        Static Save_COLPRSYSBRKPRFREQ As Integer

        If myDG.CS_FSRACC_ENGAGED > 0 Then
            If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_FSRACC_ENGAGED, 1)).SignalData <> 0 Then
                TargetStatusDisplay.Label9.BackColor = Color.Green
                TargetStatusDisplay.Label9.ForeColor = Color.Black
            Else
                TargetStatusDisplay.Label9.BackColor = Color.Black
                TargetStatusDisplay.Label9.ForeColor = Color.White
            End If
        End If

        If myDG.CS_FSRACC_BRAKE_ACTIVE > 0 Then
            If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_FSRACC_BRAKE_ACTIVE, 1)).SignalData <> 0 Then
                TargetStatusDisplay.Label10.BackColor = Color.Red
            Else
                TargetStatusDisplay.Label10.BackColor = Color.Black
            End If
        End If

        If myDG.CS_FSRACC_ACCEL_ACTIVE > 0 Then
            If myDG.CS_FSRACC_ACCEL_ACTIVE > 0 Then
                If mySignalDataWithTime(myDG.SignalIndex(myDG.CS_FSRACC_ACCEL_ACTIVE, 1)).SignalData <> 0 Then
                    TargetStatusDisplay.Label11.BackColor = Color.Green
                    TargetStatusDisplay.Label11.ForeColor = Color.Black
                Else
                    TargetStatusDisplay.Label11.BackColor = Color.Black
                    TargetStatusDisplay.Label11.ForeColor = Color.White
                End If
            End If
        End If

        If myDG.CS_CPSTOS_TTC > 0 Then 'And TargetStatusDisplay.Visible = True Then
            TargetStatusDisplay.Label1.Text = Format$(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CPSTOS_TTC, 1)).SignalData, "0.00")
        End If

        If myDG.CS_ACC_GAPSETTING > 0 Then 'And TargetStatusDisplay.Visible = True Then
            TargetStatusDisplay.Label4.Text = mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ACC_GAPSETTING, 1)).SignalData
        End If

        If myDG.CS_FOAI_VAIR > 0 And myDG.CS_FOAI_AWIR > 0 Then 'And TargetStatusDisplay.Visible = True Then

            FOAI_VAIR = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_FOAI_VAIR, 1)).SignalData)
            FOAI_AWIR = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_FOAI_AWIR, 1)).SignalData)

            'FOAI_VAIR = GetRandom(0, 4)

            If FOAI_VAIR <> Save_FOAI_VAIR_Value Then
                If FOAI_AWIR <> 2 And FOAI_AWIR <> 3 Then

                    Select Case FOAI_VAIR

                        Case 0
                            TargetStatusDisplay.PictureBox1.Visible = False
                            TargetStatusDisplay.PictureBox2.Visible = False
                            TargetStatusDisplay.PictureBox3.Visible = False
                        Case 1
                            TargetStatusDisplay.PictureBox1.Visible = True
                            TargetStatusDisplay.PictureBox2.Visible = False
                            TargetStatusDisplay.PictureBox3.Visible = False
                            TargetStatusDisplay.ListBox2.Items.Add(Format$(Now, "hh:mm:ss") & " Vehicle Ahead")
                    End Select

                End If
            End If

            If FOAI_AWIR <> Save_FOAI_AWIR_Value Then

                Select Case FOAI_AWIR
                    Case 2
                        TargetStatusDisplay.PictureBox1.Visible = False
                        TargetStatusDisplay.PictureBox2.Visible = True
                        TargetStatusDisplay.PictureBox3.Visible = False
                        TargetStatusDisplay.ListBox2.Items.Add(Format$(Now, "hh:mm:ss") & " Tailgate")
                    Case 3
                        TargetStatusDisplay.PictureBox1.Visible = False
                        TargetStatusDisplay.PictureBox2.Visible = False
                        TargetStatusDisplay.PictureBox3.Visible = True
                        TargetStatusDisplay.ListBox2.Items.Add(Format$(Now, "hh:mm:ss") & " Imminent")
                    Case Else

                        Select Case FOAI_VAIR

                            Case 0
                                TargetStatusDisplay.PictureBox1.Visible = False
                                TargetStatusDisplay.PictureBox2.Visible = False
                                TargetStatusDisplay.PictureBox3.Visible = False
                            Case 1
                                TargetStatusDisplay.PictureBox1.Visible = True
                                TargetStatusDisplay.PictureBox2.Visible = False
                                TargetStatusDisplay.PictureBox3.Visible = False
                        End Select
                End Select
            End If

            Save_FOAI_AWIR_Value = FOAI_AWIR
            Save_FOAI_VAIR_Value = FOAI_VAIR

        End If

        If myDG.CS_AUTOBRKREQ > 0 And myDG.CS_AUTOBRKREQTYPE > 0 And myDG.CS_CPSCBSC_CTRLACC > 0 Then

            If CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AUTOBRKREQTYPE, 1)).SignalData) > 0 And
        CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AUTOBRKREQTYPE, 1)).SignalData) < 5 And
        CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_AUTOBRKREQ, 1)).SignalData) > 0 And
        Save_AUTOBRKREQ_Value = 0 And
        CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CPSCBSC_CTRLACC, 1)).SignalData) < 0 Then

                TargetStatusDisplay.ListBox1.Items.Add(Format$(Now, "hh:mm:ss") & " CPS Event")
                Save_AUTOBRKREQ_Value = 1
            Else
                Save_AUTOBRKREQ_Value = 0
            End If

        End If

        If myDG.CS_COLPRSYSBRKPRFREQ > 0 Then

            COLPRSYSBRKPRFREQ = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_COLPRSYSBRKPRFREQ, 1)).SignalData)

            If COLPRSYSBRKPRFREQ = 1 And Save_COLPRSYSBRKPRFREQ = 0 Then
                TargetStatusDisplay.Label12.BackColor = Color.Yellow
                TargetStatusDisplay.Label12.ForeColor = Color.Black
                TargetStatusDisplay.ListBox1.Items.Add(Format$(Now, "hh:mm:ss") & " PREFILL Event")
            ElseIf COLPRSYSBRKPRFREQ = 0 Then
                TargetStatusDisplay.Label12.BackColor = Color.Black
                TargetStatusDisplay.Label12.ForeColor = Color.White
            ElseIf COLPRSYSBRKPRFREQ = 1 And Save_COLPRSYSBRKPRFREQ = 1 Then
                TargetStatusDisplay.Label12.BackColor = Color.Yellow
                TargetStatusDisplay.Label12.ForeColor = Color.Black
            End If

            Save_COLPRSYSBRKPRFREQ = COLPRSYSBRKPRFREQ
        End If

        If myDG.CS_CPSTOS_X_VEL > 0 And myDG.CS_CPSTOS_Y_VEL > 0 Then
            TargetStatusDisplay.Label3.Text = Format$(Math.Sqrt((mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CPSTOS_X_VEL, 1)).SignalData * mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CPSTOS_X_VEL, 1)).SignalData) +
            (mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CPSTOS_Y_VEL, 1)).SignalData * mySignalDataWithTime(myDG.SignalIndex(myDG.CS_CPSTOS_Y_VEL, 1)).SignalData)), "0.00")
        End If

    End Sub

    Private Sub HandlePedestrianStatusDisplay(ByVal myDG As GridDataClass)

        'Called from myBackgroundTasks
        'Handles Pedestrian Status Screen display...

        Dim COLPRSYSBRKPRFREQ As Integer
        Dim PEDWARN As Integer
        Dim BRAKING_FLAG As Integer
        Dim ALERT_FLAG As Integer
        Dim NOTIFICATION_FLAG As Integer

        Static Save_PEDWARN As Integer
        Static Save_BRAKING_FLAG As Integer
        Static Save_ALERT_FLAG As Integer
        Static Save_NOTIFICATION_FLAG As Integer
        Static Save_COLPRSYSBRKPRFREQ As Integer

        If myDG.CS_PEDWARN > 0 Then

            PEDWARN = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_PEDWARN, 1)).SignalData)

            'PEDWARN = GetRandom(0, 3)

            If PEDWARN <> Save_PEDWARN Then

                Select Case PEDWARN
                    Case 0
                        PedestrianStatusDisplay.PictureBox3.Visible = True
                        PedestrianStatusDisplay.PictureBox1.Visible = False
                        PedestrianStatusDisplay.PictureBox2.Visible = False
                    Case 1
                        PedestrianStatusDisplay.PictureBox3.Visible = False
                        PedestrianStatusDisplay.PictureBox1.Visible = False
                        PedestrianStatusDisplay.PictureBox2.Visible = True
                        PedestrianStatusDisplay.ListBox1.Items.Add(Format$(Now, "hh:mm:ss") & " Ped Warn DETECT")
                    Case 2
                        PedestrianStatusDisplay.PictureBox3.Visible = False
                        PedestrianStatusDisplay.PictureBox1.Visible = True
                        PedestrianStatusDisplay.PictureBox2.Visible = False
                        PedestrianStatusDisplay.ListBox1.Items.Add(Format$(Now, "hh:mm:ss") & " Ped Warn ALERT")
                    Case 3

                End Select

                Save_PEDWARN = PEDWARN

            End If
        End If

        If myDG.CS_BRAKING_FLAG > 0 Then

            BRAKING_FLAG = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_BRAKING_FLAG, 1)).SignalData)

            If BRAKING_FLAG = 10 And Save_BRAKING_FLAG <> 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Red
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(Now, "hh:mm:ss") & " BRAKING Flag")
                CopyToLog("HandlePedestrianStatusDisplay: BRAKING FLAG Set")
            ElseIf BRAKING_FLAG <> 10 And Save_BRAKING_FLAG = 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Black
                CopyToLog("HandlePedestrianStatusDisplay: BRAKING FLAG Reset")
            ElseIf BRAKING_FLAG <> 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Black
            ElseIf BRAKING_FLAG = 10 And Save_BRAKING_FLAG = 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Red
            End If

            Save_BRAKING_FLAG = BRAKING_FLAG
        End If

        If myDG.CS_ALERT_FLAG > 0 Then

            ALERT_FLAG = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_ALERT_FLAG, 1)).SignalData)

            If ALERT_FLAG = 1 And Save_ALERT_FLAG = 0 Then
                PedestrianStatusDisplay.Label2.BackColor = Color.Red
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(Now, "hh:mm:ss") & " ALERT Flag")
            ElseIf ALERT_FLAG = 0 Then
                PedestrianStatusDisplay.Label2.BackColor = Color.Black
            ElseIf ALERT_FLAG = 1 And Save_ALERT_FLAG = 1 Then
                PedestrianStatusDisplay.Label2.BackColor = Color.Red
            End If

            Save_ALERT_FLAG = ALERT_FLAG
        End If

        If myDG.CS_NOTIFICATION_FLAG > 0 Then

            NOTIFICATION_FLAG = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_NOTIFICATION_FLAG, 1)).SignalData)

            If NOTIFICATION_FLAG = 1 And Save_NOTIFICATION_FLAG = 0 Then
                PedestrianStatusDisplay.Label3.BackColor = Color.Red
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(Now, "hh:mm:ss") & " NOTIFICATION Flag")
            ElseIf NOTIFICATION_FLAG = 0 Then
                PedestrianStatusDisplay.Label3.BackColor = Color.Black
            ElseIf NOTIFICATION_FLAG = 1 And Save_NOTIFICATION_FLAG = 1 Then
                PedestrianStatusDisplay.Label3.BackColor = Color.Red
            End If

            Save_NOTIFICATION_FLAG = NOTIFICATION_FLAG
        End If

        If myDG.CS_COLPRSYSBRKPRFREQ > 0 Then

            COLPRSYSBRKPRFREQ = CInt(mySignalDataWithTime(myDG.SignalIndex(myDG.CS_COLPRSYSBRKPRFREQ, 1)).SignalData)

            If COLPRSYSBRKPRFREQ = 1 And Save_COLPRSYSBRKPRFREQ = 0 Then
                PedestrianStatusDisplay.Label4.BackColor = Color.Yellow
                PedestrianStatusDisplay.Label4.ForeColor = Color.Black
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(Now, "hh:mm:ss") & " PREFILL Event")
            ElseIf COLPRSYSBRKPRFREQ = 0 Then
                PedestrianStatusDisplay.Label4.BackColor = Color.Black
                PedestrianStatusDisplay.Label4.ForeColor = Color.White
            ElseIf COLPRSYSBRKPRFREQ = 1 And Save_COLPRSYSBRKPRFREQ = 1 Then
                PedestrianStatusDisplay.Label4.BackColor = Color.Yellow
                PedestrianStatusDisplay.Label4.ForeColor = Color.Black
            End If

            Save_COLPRSYSBRKPRFREQ = COLPRSYSBRKPRFREQ
        End If

    End Sub

    Private Sub HandleNonMeasureModeUpdates(ByVal SaveODOReading As Double, ByRef SaveWhereAmIAt As String, ByRef GoNoGoFault() As Boolean)

        'Called from myBackgroundTasks
        'Handles Updates when transitioning out of measure mode...

        If OnProperty_Recording = True Then
            OnPropertyMileage_Recording = OnPropertyMileage_Recording + (SaveODOReading - OnPropertyStartingODO_Recording)
            OnProperty_Recording = False
            WriteMileageToANNOFile("OnProperty_Recording - " & OnPropertyMileage_Recording)
        ElseIf OnProperty_NotRecording = True Then
            OnPropertyMileage_NotRecording = OnPropertyMileage_NotRecording + (SaveODOReading - OnPropertyStartingODO_NotRecording)
            OnProperty_NotRecording = False
            WriteMileageToANNOFile("OnProperty_NotRecording - " & OnPropertyMileage_NotRecording)
        ElseIf OffProperty_Recording = True Then
            OffPropertyMileage_Recording = OffPropertyMileage_Recording + (SaveODOReading - OffPropertyStartingODO_Recording)
            OffProperty_Recording = False
            WriteMileageToANNOFile("OffProperty_Recording - " & OffPropertyMileage_Recording)
        ElseIf OffProperty_NotRecording = True Then
            OffPropertyMileage_NotRecording = OffPropertyMileage_NotRecording + (SaveODOReading - OffPropertyStartingODO_NotRecording)
            OffProperty_NotRecording = False
            WriteMileageToANNOFile("OffProperty_NotRecording - " & OffPropertyMileage_NotRecording)
        ElseIf Unknown_NotRecording = True Then
            UnknownMileage_NotRecording = UnknownMileage_NotRecording + (SaveODOReading - UnknownStartingODO_NotRecording)
            Unknown_NotRecording = False
            WriteMileageToANNOFile("Unknown_NotRecording - " & UnknownMileage_NotRecording)
        ElseIf Unknown_Recording = True Then
            UnknownMileage_Recording = UnknownMileage_Recording + (SaveODOReading - UnknownStartingODO_Recording)
            Unknown_Recording = False
            WriteMileageToANNOFile("Unknown_Recording - " & UnknownMileage_Recording)
        End If

        SaveWhereAmIAt = "Undefined"

        If OnVehicleScreen.Label1.BackColor <> Color.Gray Then
            OnVehicleScreen.Label1.BackColor = Color.Gray
        End If

        'Vehicle location (on property, off property or unknown is not displayed if we are not measuring or recording...
        If OnVehicleScreen.Label1.Text <> "" Then
            OnVehicleScreen.Label1.Text = ""
        End If

        'Update go/nogo status indicators color to gray...
        For z = 0 To UBound(myDFs)
            If myDFs(z).GoNoGoIndex > -1 Then
                If myLabel(myDFs(z).GoNoGoIndex).BackColor <> Color.Gray Then
                    UpdateGONOGOLabelColor(myDFs(z).GoNoGoIndex, Color.Gray)

                    GoNoGoFault(myDFs(z).GoNoGoIndex) = False

                    For i = 0 To UBound(myDGs)
                        If myDGs(i).ParentFormIndex = z Then
                            For x = 0 To myDGs(i).RowCount
                                For y = 0 To myDGs(i).ColumnCount - 1
                                    myDGs(i).DataFrozenCounter(x, y) = 0
                                    myDGs(i).DataFrozen(x, y) = False
                                Next y
                            Next x
                        End If
                    Next i

                End If
            End If
        Next z

        VideoCameraNotUpdating = False
        BackgroundLoopCounterNotUpdating = False

        If Me.WindowState <> FormWindowState.Minimized Then

            'Handle Exit Button...
            If OnVehicleScreen.Button1.Visible = False Then
                OnVehicleScreen.Button1.Visible = True
            End If

            If myMainTabControl.Visible = False Then
                myMainTabControl.Visible = True
            End If

            'Distance to Target not displayed when not in measure mode or record mode...
            If Len(OnVehicleScreen.Label17.Text) > 0 Then
                OnVehicleScreen.Label17.Text = ""
            End If

        End If
    End Sub


    Private Sub MyBackgroundTasks()

        'This routine handles all of the background tasks for the GM_ResidentClient.  It is responsible for making calls to
        'GM_INCA_Comm (Via the INCA_InterfaceClass) to get data from INCA and display it, it also handles updating timers, counters, display colors
        'status information etc. for the main display as well as for the various dynamically created forms, custom displays and grids.

        Dim OneShot As Boolean
        Dim WhereAmI As String = "undefined"
        Dim SaveWhereAmIAt As String = "Undefined"
        Dim i As Integer
        Dim j As Integer
        Dim k As Integer
        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim GridUpdateAction As GridUpdateActions
        Dim mySaveTime As DateTime
        Dim myElapseTime As TimeSpan
        Dim mySaveTime2 As DateTime
        Dim myElapseTime2 As TimeSpan
        Dim myMeasureTime As TimeSpan
        Dim mySaveMeasureTime As DateTime
        Dim mystr As String
        Dim mytempstr As String
        Dim mytempval As Double
        Dim maxNoOfRecords As Integer = 1
        Dim noOfRecords As Integer
        Dim time(maxNoOfRecords - 1) As Double
        Dim myRCI2Data As IGM_INCA_Comm.INCAData
        Dim LineNum As Integer
        Dim LiveDataElapseTime As TimeSpan
        Dim SaveLiveDataTime As DateTime
        Dim SaveRecordFileTime As DateTime = Now
        Dim L_totalnumsignalsdisplayed As Integer
        Dim L_MeasurementStatus As String
        Dim L_RecordingState As Boolean
        Dim L_SaveRecordingState As Boolean
        Dim DataFrozen As Boolean
        Dim GoNoGoFault() As Boolean
        Dim Retries As Integer = 0
        Dim SaveODOReading As Double = 0.0
        Dim e As System.EventArgs = System.EventArgs.Empty
        Dim LRRCnt As Integer = 0
        Dim VISCnt As Integer = 0
        Dim LogMsgWritten As Boolean

        Dim mySaveTime3 As DateTime
        Dim myElapseTime3 As TimeSpan

        Dim SaveHealthCounter As Integer

        Const DATA_FROZEN_MAX_COUNT_MEASUREMENT_MODE As Integer = 100
        Const DATA_FROZEN_MAX_COUNT_RECORD_MODE As Integer = 200

        Dim DataFrozenMaxCount As Integer = DATA_FROZEN_MAX_COUNT_RECORD_MODE

        Dim myDTCsSaveTime As DateTime
        Dim myDTCsElapseTime As TimeSpan
        Dim ActiveDTC As Boolean
        Dim DTCTimerActive As Boolean
        'Dim SaveDTCNumber As Integer

        Dim SaveDTCStrings As ArrayList = Nothing

        'Dim SaveDTCNumber As Integer
        'Dim SaveDTCNumber As Integer

        Dim HCSDebugOneShot As Boolean
        Dim HCFDebugOneShot As Boolean

        Const LOST_DEVICE_DELAY_TIME As Integer = 20 'seconds

        Try

            If Debugger.IsAttached = True Then
                OnVehicleScreen.CheckBox1.Visible = True
            End If

            ReDim GoNoGoFault(UBound(myLabel))

            mySaveTime = Now
            mySaveTime2 = Now
            noOfRecords = 1
            LineNum = -1
            L_MeasurementStatus = ""
            L_RecordingState = False

            Do While EnableMyBackgroundTasks = True

                WhereAmI = "Start of Loop"

                'SwitchToMain flag is used in conjunction with INCA Status window (see ProcessKiller)
                If SwitchToMain = True Then
                    SwitchToMain = False
                    CopyToLog("myBackgroundTasks: RedisplayOnVehicleForm(Main) Called...")
                    RedisplayOnVehicleForm("Main")
                    OnVehicleScreen.TopMost = False
                End If

                'This redimensioning would only be required if a new go/nogo label was to be created at runtime, very unlikely
                'based on current use cases...
                If UBound(myLabel) > UBound(GoNoGoFault) Then
                    ReDim Preserve GoNoGoFault(UBound(myLabel))
                End If

                System.Windows.Forms.Application.DoEvents()

                If PlaybackMode = False Then

                    WhereAmI = "PlaybackMode = False"

                    'Here we are updating the HealthMonitor counter which is monitored by ProcessKiller running in a separate thread.
                    'If HealthMonitor is no longer counting up, it indicates that a periodic call made  to INCA in myBackgoundtasks
                    'has hung - This circumstance would be handled in ProcessKiller in separate thread...
                    HealthMonitor = HealthMonitor + 1
                    If HealthMonitor > 30000 Then
                        HealthMonitor = 0
                    End If

                    'Every second (currently) we check INCA Measurement status and INCA Record Status
                    'we use this info to direct logic throughout this background task...
                    myElapseTime2 = Now.Subtract(mySaveTime2)
                    If myElapseTime2.Seconds >= 1 Then
                        mySaveTime2 = Now

                        WhereAmI = "myINCAInterface.GetMeasurementStatus"
                        L_MeasurementStatus = myINCAInterface.GetMeasurementStatus()

                        If L_MeasurementStatus = "True" Then

                            If InMeasureMode = False Then
                                InMeasureMode = True
                                mySaveMeasureTime = Now
                                mySaveTime = Now
                                mySaveTime3 = Now

                                DataFrozenMaxCount = DATA_FROZEN_MAX_COUNT_MEASUREMENT_MODE

                            End If

                        ElseIf L_MeasurementStatus = "INCA Communication Failure" Or L_MeasurementStatus = "GetMeasurementStatus Failure" Then

                            CopyToLog("myBackgroundTasks: L_MeasurementStatus = " & L_MeasurementStatus)

                        Else
                            InMeasureMode = False 'InMeasureMode is also monitored by ProcessKiller thread...
                            OneShot = False 'OneShot flag is passed to HandleAutomaticStartRecord, below... 

                        End If

                        WhereAmI = "myINCAInterface.GetRecordingState"
                        L_RecordingState = myINCAInterface.GetRecordingState()
                        If L_SaveRecordingState = False And L_RecordingState = True Then
                            SaveRecordFileTime = Now

                            DataFrozenMaxCount = DATA_FROZEN_MAX_COUNT_RECORD_MODE
                        End If
                        L_SaveRecordingState = L_RecordingState

                        'This routine handles the case where measurement or record button has been pressed on INCA display
                        'rather than on CLEVIR display...
                        CheckForINCAButtonPresses(L_MeasurementStatus, L_RecordingState)

                    End If

                    If InMeasureMode = True And L_RecordingState = False Then

                        myMeasureTime = Now.Subtract(mySaveMeasureTime)

                        'Handle case where user has been in meaure mode for 5 minutes without pressing record.  HandleAutomaticStartRecord will
                        'automatically start recording unless user responds to prompt...
                        HandleAutomaticStartRecord(myMeasureTime, mySaveMeasureTime, OneShot)

                    Else
                        mySaveMeasureTime = Now
                    End If

                    'This section below will make periodic calls (every 5 seconds right now) to the Handle_5_SecondChecks routine.  This routine
                    'checks to see if there is an active INCA experiment, if not CLEVIR will terminate, also checks various other statuses and
                    'reacts accordingly...                 
                    myElapseTime = Now.Subtract(mySaveTime)
                    If myElapseTime.Seconds >= 5 Then
                        mySaveTime = Now

                        Handle_5_SecondChecks(L_MeasurementStatus, L_RecordingState)

                    End If 'If myElapseTime.Seconds >= 5

                End If

                myElapseTime3 = Now.Subtract(mySaveTime3)
                If myElapseTime3.Seconds >= LOST_DEVICE_DELAY_TIME Then 'was 10 changed to 15 08/30/2020
                    'This routine handles the case where communication to processors has been lost or invalid data or invalid video.  Allows user to
                    'reinitialilze CLEVIR and INCA...
                    CheckForLostDevice()
                    mySaveTime3 = Now
                End If

                'If measurement started = true (or we are playing back CLEVIR recorded data), we make calls to get data, either live or recordded.
                'We also update the display grids by updating the data values for all signals and also go through the logic associated with high threshold and
                'low threshold, etc. to determine background color of each grid cell.

                WhereAmI = "Before If myINCAInterface.MeasurementStarted = True"

                If myINCAInterface.MeasurementStarted = True Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PLAYBACK_STATES.PlaybackRun Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PLAYBACK_STATES.PlaybackStepBack Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PLAYBACK_STATES.PlaybackScrolling Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PLAYBACK_STATES.PlaybackStepFwd Then

                    WhereAmI = "Before HandleRecordingProgressBar"

                    If Me.Visible = True Or RecordPlayback.Visible = True Then
                        'This routine handles the green recording progress bar that is displayed either on the RecordPlayback form
                        'or on the main CLEVIR screen...
                        HandleRecordingProgressBar(RecordPlayback.RecordMode, RecordPlayback.ProgressBar1)
                    Else
                        If OnVehicleScreen.Button1.Visible = True Then
                            OnVehicleScreen.Button1.Visible = False
                        End If
                        'This routine handles the green recording progress bar that is displayed either on the RecordPlayback form
                        'or on the main CLEVIR screen...
                        HandleRecordingProgressBar(myINCAInterface.Recording, OnVehicleScreen.ProgressBar1)
                    End If

                    '********************************Here is where we are refreshing top down view **************************************

                    WhereAmI = "Before If Not myTDGraphicsContainer.IsDisposed"

                    If Not myTDGraphicsContainer.IsDisposed Then

                        If myTDGraphicsContainer.Visible = True Then
                            'every other loop....
                            'ctr = ctr + 1
                            'If ctr = 2 Then
                            myTDGraphicsContainer.Invalidate() 'this triggers paint event on top down view...
                            'ctr = 0
                            'End If
                        End If
                    End If

                    '**********************************************************************************************************

                    WhereAmI = "Before WAV Recording"
                    'This routine handles the color of the frame around the microphone to indicate whether or not a WAV recording
                    'is in progress.  Red, we are not recording WAV, Green we are...
                    'There is a preset amount of time that the WAV recording is enabled, and it needs to shut off after that time.
                    'This routine handles that...
                    HandleWAVRecording()

                    If L_RecordingState = True Then

                        'This routine handles, amoung other things, stopping and starting the INCA recording based
                        'on the time interval set...
                        HandleUpdatesWhenRecording(SaveRecordFileTime)

                    Else 'L_RecordingState = False

                        'here we are setting time in recording display blank because we are no longer recording...
                        If Len(OnVehicleScreen.Label8.Text) > 0 Then
                            OnVehicleScreen.Label8.Text = ""
                        End If

                    End If

                    'Here we are initializing the data array that will be used to save CLEVIR recorded data if the
                    'CLEVIR data recorder is set to RecordMode.  Not to be confused with recording INCA mf4 data.
                    'This recording creates a spreadsheet that CLEVIR can read back in so that it can play back
                    'data.  This would be used primarily to test CLEVIR or be used for DEMOs.

                    If RecordPlayback.RecordMode = True Then
                        WhereAmI = "RecordPlayback.RecordMode = True"
                        k = 0
                        LineNum = LineNum + 1
                        ReDim Preserve VariableNameDataArray(UBound(myINCAInterface.myDisplaySignals) + 2, LineNum)

                        If LineNum = 0 Then
                            VariableNameDataArray(0, 0) = "Time"
                            VariableNameDataArray(1, 0) = "TimeStamp"
                        Else
                            VariableNameDataArray(0, LineNum) = ""
                            VariableNameDataArray(1, LineNum) = ""
                        End If

                    End If

                    'This section executes if we are processing live data....
                    If RecordPlayback.PlaybackMode = RecordPlayback.PLAYBACK_STATES.PlaybackStop Then
                        If SaveLiveDataTime = Nothing Then
                            LiveDataElapseTime = Now.Subtract(Now)
                        Else
                            LiveDataElapseTime = Now.Subtract(SaveLiveDataTime)
                        End If

                        WhereAmI = "myINCAInterface.GetSignalDataWithTime"
                        mySignalDataWithTime = myINCAInterface.GetSignalDataWithTime() 'here is where we get all live data from INCA...

                        If Not mySignalDataWithTime Is Nothing Then

                            SaveLiveDataTime = Now

                            For j = 0 To UBound(mySignalDataWithTime)

                                'We can record live data to be played back in CLEVIR, here is where we capture that
                                'live data if we are recording...
                                If RecordPlayback.RecordMode = True And LineNum >= 0 Then
                                    If LineNum = 0 Then
                                        VariableNameDataArray(k + 2, 0) = myINCAInterface.myDisplaySignals(j).SignalName
                                    Else
                                        If Len(VariableNameDataArray(0, LineNum)) = 0 Then
                                            VariableNameDataArray(0, LineNum) = Format$(LiveDataElapseTime.Milliseconds, "0.000")
                                            VariableNameDataArray(1, LineNum) = Format$(mySignalDataWithTime(j).TimeStamp, "0.000")
                                        End If
                                        VariableNameDataArray(k + 2, LineNum) = mySignalDataWithTime(j).SignalData
                                    End If
                                    k = k + 1
                                End If

                            Next j

                        End If

                    Else 'This else case executes if we are playing back CLEVIR recorded data....

                        WhereAmI = "This else case executes if we are playing back recorded data...."

                        ReDim mySignalDataWithTime(0)

                        myRCI2Data = GetRecordedData(UBound(myINCAInterface.myDisplaySignals) + 2, RecordPlayback.PlaybackMode)

                        For i = 0 To UBound(myRCI2Data.myData)
                            If i > UBound(mySignalDataWithTime) Then
                                ReDim Preserve mySignalDataWithTime(i)
                            End If

                            mySignalDataWithTime(i).SignalData = myRCI2Data.myData(i)
                            mySignalDataWithTime(i).TimeStamp = myRCI2Data.myTime(i)
                        Next i

                    End If

                    'If we have valid data (live or recorded) this section executes...
                    If Not mySignalDataWithTime Is Nothing Then

                        L_totalnumsignalsdisplayed = 0

                        If Not myDGs(0) Is Nothing Then

                            'Here we will loop through all of the grids on all of the forms...
                            For z = 0 To UBound(myDGs)

                                WhereAmI = "Go Thru All Displayed Grids"

                                'need to go thru all grids which are displayed, or are associated with gonogo, or are associated with a custom screen (CS_)....
                                If myDGs(z).Parent.Visible = True Or myDFs(myDGs(z).ParentFormIndex).AlsoAssociatedWith = "GO/NOGO" Or myDFs(myDGs(z).ParentFormIndex).AlsoAssociatedWith = "AUTOANNO" Or
                                    (InStr(myDFs(myDGs(z).ParentFormIndex).AlsoAssociatedWith, "CS_") > 0) Then

                                    WhereAmI = "Grid Visible or GO/NOGO or CS_"

                                    mystr = ""
                                    mytempstr = ""
                                    mytempval = 0

                                    If L_RecordingState = True Then

                                        If myDGs(z).CS_NAN_STATUS > 0 Then
                                            HandleNANStatus(myDGs(z))
                                        End If

                                    End If

                                    'This Routine handles all things related to vehicle mileage and where the vehicle is,
                                    'on grounds or off grounds, etc. Will not be called unless CS_ODOMETER is defined in signallist...
                                    If myDGs(z).CS_ODOMETER > 0 Then
                                        HandleOdometerRelatedStatus(myDGs(z), SaveODOReading, SaveWhereAmIAt, L_RecordingState)
                                    End If

                                    'This Routine handles CLEVIR display of cluster message...
                                    'Nothing happens unless CS_LCC_CLUSTER_MSG and CS_LCC_BUTTON_PRESS are defined in signal list...
                                    If myDGs(z).CS_LCC_CLUSTER_MSG > 0 And myDGs(z).CS_LCC_BUTTON_PRESS > 0 Then
                                        HandleCLEVIRDisplayOfClusterMsgs(myDGs(z))
                                    End If

                                    'this next section handles custom displays.  This is where data is put into the
                                    'labels on the custom displays.  Background colors for labels are updated here based on values
                                    'of variables associated with the various labels on the custom screens.  Whenever a custom 
                                    'screen is added, code must be added here to handle updating. Code is only executed if custom

                                    'ADD CUSTOM SCREEN HANDLING HERE....

                                    If myTDGraphicsContainer.Visible = True Then

                                        HandleALCDisplayElements(myDGs(z))

                                    End If

                                    If CopilotStatusDisplay.Visible = True Then

                                        HandleCoPilotStatusDisplay(myDGs(z))

                                    End If

                                    If LMFR_Status_Screen_HC.Visible = True Then

                                        HandleLMFRStatusScreenHC(myDGs(z))

                                    End If

                                    If LMFR_Status_Display_Global_A.Visible = True Then

                                        HandleLMFRStatusScreenGlobalA(myDGs(z))

                                    End If

                                    If FusionStatusDisplay.Visible = True Then

                                        HandleFusionStatusDisplay(myDGs(z))

                                    End If

                                    If LKAForm.Visible = True Then

                                        HandleLKACustomDisplay(myDGs(z))

                                    End If

                                    If TargetStatusDisplay.Visible = True Then

                                        HandleTargetStatusDisplay(myDGs(z))

                                    End If

                                    If myDGs(z).CS_CPSTOS_X_POS > 0 And myDGs(z).CS_CPSTOS_Y_POS > 0 Then

                                        If TargetStatusDisplay.Visible = True Then
                                            TargetStatusDisplay.Label2.Text = Format$(Math.Abs(Math.Sqrt((mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_X_POS, 1)).SignalData * mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_X_POS, 1)).SignalData) +
                                            (mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData * mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData))), "0.00")
                                        End If

                                        If OnVehicleScreen.Visible = True Then
                                            OnVehicleScreen.Label17.Text = Format$(Math.Abs(Math.Sqrt((mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_X_POS, 1)).SignalData * mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_X_POS, 1)).SignalData) +
                                            (mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData * mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData))), "0.00")
                                        End If

                                    End If

                                    If PedestrianStatusDisplay.Visible = True Then

                                        HandlePedestrianStatusDisplay(myDGs(z))

                                    End If

                                    Try

                                        WhereAmI = "Rows and Columns"
                                        'For each grid, we must go through all rows and columns to update data display, background and foreground
                                        'colors, etc.
                                        For x = 0 To myDGs(z).RowCount

                                            For y = 0 To myDGs(z).ColumnCount - 1

                                                mytempstr = ""
                                                If myDGs(z).SignalIndex(x, y) >= 0 And
                                                    Len(myDGs(z).VariableName(x, y)) > 0 And
                                                    myDGs(z).Registered(x, y) = True And
                                                    myDGs(z).VariableName(x, y) <> "undefined" Then

                                                    'This section handles putting dashes in all cells if object ID is 0...
                                                    'assumes the "blanker" is the first row that must be "blanked" and all subsequent rows in the associated grid are "blanked"
                                                    If (myDGs(z).ObjectID_Start_Pos <> 0 And
                                                         mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).ObjectID_Start_Pos, y)).SignalData = 0) _
                                                        And x > myDGs(z).ObjectID_Start_Pos Then

                                                        mytempstr = "-"
                                                        If myDGs(z).CurrentBackColor(x, y) <> myDGs(z).DefaultCellBackColor(x, y) Then
                                                            UpdateGridColor(z, x, y, GridUpdateActions.FROM_LOW)
                                                        End If

                                                        If myDGs(z).CurrentForeColor(x, y) <> myDGs(z).DefaultCellForeColor(x, y) Then
                                                            UpdateGridColor(z, x, y, GridUpdateActions.FROM_LOW)
                                                        End If

                                                    Else
                                                        'HealtCounter is maintained in ProcessKiller thread.  A HealthCounter value of 0 indicates
                                                        'a healthy system...
                                                        If HealthCounter = 0 Then

                                                            If SaveHealthCounter <> 0 Then
                                                                CopyToLog("myBackgroundTasks: HealthCounter = " & HealthCounter)
                                                                SaveHealthCounter = 0
                                                            End If

                                                            'BELOW OVERRIDE OF mySignalDataWithTime FOR TESTING ONLY...

                                                            'Also dont forget to make the check box visible = false before compiling for release...

                                                            'If OnVehicleScreen.CheckBox1.Checked = True Then
                                                            'If InStr(myDGs(z).VariableName(x, y), "DTC_Index") > 0 Or InStr(myDGs(z).VariableName(x, y), "VaRBSR_Cnt_BackgroundLoop") > 0 Or InStr(myDGs(z).VariableName(x, y), "VIDEO_TIMECODE") > 0 Then
                                                            'mySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = GetRandom(0, 100)
                                                            'End If
                                                            'End If
                                                            'SsDFIC_FA

                                                            If Debugger.IsAttached = True Then

                                                                If OnVehicleScreen.CheckBox1.Checked = True Then
                                                                    If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 And myDGs(z).DeviceName(x, y) = "HCS" Then
                                                                        If HCSDebugOneShot = False Then
                                                                            mySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = GetRandom(0, 10)
                                                                            HCSDebugOneShot = True
                                                                        Else
                                                                            mySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                                                                            HCSDebugOneShot = False
                                                                        End If
                                                                    End If

                                                                    If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 And myDGs(z).DeviceName(x, y) = "HCF" Then
                                                                        If HCFDebugOneShot = False Then
                                                                            mySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = GetRandom(0, 10)
                                                                            HCFDebugOneShot = True
                                                                        Else
                                                                            mySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                                                                            HCFDebugOneShot = False
                                                                        End If
                                                                    End If
                                                                Else
                                                                    If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 And myDGs(z).DeviceName(x, y) = "HCS" Then
                                                                        HCSDebugOneShot = False
                                                                        mySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                                                                    End If
                                                                    If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 And myDGs(z).DeviceName(x, y) = "HCF" Then
                                                                        HCFDebugOneShot = False
                                                                        mySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                                                                    End If
                                                                End If

                                                            End If

                                                            'The CheckForDataChange property is associated with the same column in the
                                                            'signal configuration spreadsheet.  This allows us to set up specific signals to watch
                                                            'to see if they are changing.  If they do not change over a certain period of time,
                                                            'this would indicate a communication issue....

                                                            If myDGs(z).CheckForDataChange(x, y) = True And myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex > -1 And
                                                                    (myDGs(z).Name <> "INCA Video Status Items" Or (myDGs(z).Name = "INCA Video Status Items" And L_RecordingState = True)) Then
                                                                If myDGs(z).SaveLastValue(x, y) = mySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData Then

                                                                    If myDGs(z).DataFrozen(x, y) = False Then 'ADDED 04/14/2020
                                                                        myDGs(z).DataFrozenCounter(x, y) = myDGs(z).DataFrozenCounter(x, y) + 1
                                                                        'If Debugger.IsAttached Then
                                                                        'CopyToLog(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - " & myDGs(z).DataFrozenCounter(x, y))
                                                                        'End If
                                                                    End If

                                                                    'the number DataFrozenMaxCount is somewhat arbitrary, this should be based on time because we 
                                                                    'count up faster when we are waiting less time between cycles through this code

                                                                    If myDGs(z).DataFrozenCounter(x, y) > DataFrozenMaxCount And myDGs(z).DataFrozen(x, y) = False Then 'Changed from 100 to 200 08/28/2020 - Changed to 100 from 250 06/12/2020 Changed to 250 from 50 06/12/2020 Changed back to 50 - 06/06/2020 was > 50 Changed to 25 04/15/2020 because background tasks is taking too long to run...

                                                                        'If Debugger.IsAttached Then
                                                                        'CopyToLog(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Data Frozen " & myDGs(z).DataFrozenCounter(x, y))
                                                                        'End If

                                                                        myDGs(z).DataFrozenCounter(x, y) = 0
                                                                        myDGs(z).DataFrozen(x, y) = True

                                                                        CheckForExperiment = False

                                                                        If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).HighThreshBackColor(x, y)) Then
                                                                            GridUpdateAction = GridUpdateActions.TO_HIGH
                                                                            UpdateGridColor(z, x, y, GridUpdateAction)

                                                                            If L_RecordingState = True Then
                                                                                myINCAInterface.WriteEventComment(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - First Instance Data Frozen", True)
                                                                            End If

                                                                        Else

                                                                            If L_RecordingState = True Then
                                                                                myINCAInterface.WriteEventComment(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Not Updating", True)
                                                                            End If

                                                                        End If

                                                                        'Here we set the go/nogo label color to red and save the device names for those
                                                                        'devices with frozen data.  These FrozenDeviceNames are used when displaying
                                                                        'user message indicating a data or video device issue...

                                                                        If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then

                                                                            GoNoGoFault(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                                            UpdateGONOGOLabelColor(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)

                                                                            If myDGs(z).Name <> "INCA Video Status Items" Then

                                                                                If Len(SaveDataFrozenDeviceName) = 0 Then
                                                                                    SaveDataFrozenDeviceName = myDGs(z).DeviceName(x, y)
                                                                                ElseIf InStr(SaveDataFrozenDeviceName, myDGs(z).DeviceName(x, y)) = 0 Then
                                                                                    SaveDataFrozenDeviceName = SaveDataFrozenDeviceName & "," & myDGs(z).DeviceName(x, y)
                                                                                End If

                                                                            Else

                                                                                If Len(SaveVideoFrozenDeviceName) = 0 Then
                                                                                    SaveVideoFrozenDeviceName = myDGs(z).DeviceName(x, y)
                                                                                ElseIf InStr(SaveVideoFrozenDeviceName, myDGs(z).DeviceName(x, y)) = 0 Then
                                                                                    SaveVideoFrozenDeviceName = SaveVideoFrozenDeviceName & "," & myDGs(z).DeviceName(x, y)
                                                                                End If

                                                                            End If

                                                                        End If

                                                                    End If
                                                                Else 'Value has changed...

                                                                    'If any of the frozen data starts to change, we reset the frozen data counter and frozen data state here...

                                                                    'If myDGs(z).CheckForDataChange(x, y) = True And myDGs(z).DataFrozen(x, y) = True And myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex > -1 Then
                                                                    If myDGs(z).CheckForDataChange(x, y) = True And myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex > -1 Then
                                                                        myDGs(z).DataFrozenCounter(x, y) = 0

                                                                        If myDGs(z).DataFrozen(x, y) = True Then
                                                                            myDGs(z).DataFrozen(x, y) = False
                                                                            'If Debugger.IsAttached Then
                                                                            'CopyToLog(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Data Frozen FALSE " & myDGs(z).DataFrozenCounter(x, y))
                                                                            'End If

                                                                            If myDGs(z).Name <> "INCA Video Status Items" Then
                                                                                If InStr(SaveDataFrozenDeviceName, ",") > 0 Then
                                                                                    Dim tempstr() As String
                                                                                    tempstr = Split(SaveDataFrozenDeviceName, ",")
                                                                                    SaveDataFrozenDeviceName = ""
                                                                                    For i = 0 To UBound(tempstr)
                                                                                        If tempstr(i) <> myDGs(z).DeviceName(x, y) Then
                                                                                            If Len(SaveDataFrozenDeviceName) = 0 Then
                                                                                                SaveDataFrozenDeviceName = tempstr(i)
                                                                                            Else
                                                                                                SaveDataFrozenDeviceName = SaveDataFrozenDeviceName & "," & tempstr(i)
                                                                                            End If

                                                                                        End If
                                                                                    Next i

                                                                                Else
                                                                                    SaveDataFrozenDeviceName = ""
                                                                                End If

                                                                            Else
                                                                                If InStr(SaveVideoFrozenDeviceName, ",") > 0 Then
                                                                                    Dim tempstr() As String
                                                                                    tempstr = Split(SaveVideoFrozenDeviceName, ",")
                                                                                    SaveVideoFrozenDeviceName = ""
                                                                                    For i = 0 To UBound(tempstr)
                                                                                        If tempstr(i) <> myDGs(z).DeviceName(x, y) Then
                                                                                            If Len(SaveVideoFrozenDeviceName) = 0 Then
                                                                                                SaveVideoFrozenDeviceName = tempstr(i)
                                                                                            Else
                                                                                                SaveVideoFrozenDeviceName = SaveVideoFrozenDeviceName & "," & tempstr(i)
                                                                                            End If

                                                                                        End If
                                                                                    Next i

                                                                                Else
                                                                                    SaveVideoFrozenDeviceName = ""
                                                                                End If

                                                                            End If

                                                                        End If

                                                                    End If

                                                                    If PlaybackMode = False Then
                                                                        CheckForExperiment = True
                                                                    End If

                                                                    If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)) Then
                                                                        GridUpdateAction = GridUpdateActions.FROM_HIGH
                                                                        UpdateGridColor(z, x, y, GridUpdateAction)

                                                                        If L_RecordingState = True Then
                                                                            myINCAInterface.WriteEventComment(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Resumed Updating", True)
                                                                        Else
                                                                            CopyToLog(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Resumed Updating", True)
                                                                        End If
                                                                    End If

                                                                End If
                                                            End If

                                                        Else 'HealthCounter > 0 indicates that we have most likely hung on an INCA API Call...

                                                            'If we are hung on an INCA API Call, we should reset the frozen counter...
                                                            If HealthCounter > 10 Then
                                                                myDGs(z).DataFrozenCounter(x, y) = 0
                                                            End If

                                                            If PlaybackMode = False Then
                                                                CheckForExperiment = True
                                                            End If

                                                            If SaveHealthCounter = 0 Then
                                                                CopyToLog("myBackgroundTasks: HealthCounter = " & HealthCounter)
                                                            End If

                                                            SaveHealthCounter = HealthCounter

                                                        End If

                                                        WhereAmI = "Set mytempval"

                                                        mytempval = mySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData

                                                        If myDGs(z).AlsoAssociatedWith(x, y) <> "AUTOANNO" Then
                                                            myDGs(z).SaveLastValue(x, y) = mytempval
                                                        End If

                                                        WhereAmI = "Format Display String"

                                                        'Formats the string which will be displayed for a given variable.
                                                        mytempstr = FormatDisplayString(mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y))

                                                        'This section handles updating back color, fore color etc. based on settings
                                                        'in excel configuration file

                                                        WhereAmI = "Update Grid Colors"

                                                        'Here is where we handle updating grid cell colors based on data values in the variovus cells
                                                        'We also update go/nogo label colors here if necessary.  This is done here for those variables
                                                        'that are not frozen...

                                                        If myDGs(z).DataFrozen(x, y) = False Then

                                                            WhereAmI = "HighThresh Check"
                                                            If mytempval > myDGs(z).HighThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
                                                                If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).HighThreshBackColor(x, y)) Then

                                                                    If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                                        CopyToLog("myBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & mytempval & " Threshold Value: " & myDGs(z).HighThresh(x, y), 1)
                                                                    Else
                                                                        CopyToLog("myBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & mytempval & " Threshold Value: " & myDGs(z).HighThresh(x, y), 2)
                                                                    End If

                                                                    GridUpdateAction = GridUpdateActions.TO_HIGH
                                                                    UpdateGridColor(z, x, y, GridUpdateAction)

                                                                    If myDGs(z).Name = "DTCs" Then

                                                                        If ActiveDTC = False Then
                                                                            'SaveDTCStrings = Nothing
                                                                            SaveDTCStrings = New ArrayList
                                                                            SaveDTCStrings.Add(myDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y)))
                                                                            CopyToLog("myBackgroundTasks: Active DTC = " & SaveDTCStrings.Item(0))
                                                                            ActiveDTC = True
                                                                            'SaveDTCNumber = mytempval
                                                                        Else
                                                                            If Not SaveDTCStrings Is Nothing Then
                                                                                If Not SaveDTCStrings.Contains(myDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y))) Then
                                                                                    SaveDTCStrings.Add(myDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y)))
                                                                                    CopyToLog("myBackgroundTasks: Active DTC = " & SaveDTCStrings.Item(SaveDTCStrings.Count - 1))
                                                                                End If
                                                                            End If

                                                                            'If SaveDTCNumber <> mytempval Then
                                                                            'SaveDTCNumber = mytempval
                                                                            ' CopyToLog("myBackgroundTasks: Active DTC = " & myDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y)))
                                                                            'End If
                                                                            'End If

                                                                            myDTCsSaveTime = Now
                                                                            myDTCsElapseTime = Now.Subtract(myDTCsSaveTime)
                                                                            DTCTimerActive = True
                                                                        End If

                                                                        If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then

                                                                            GoNoGoFault(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                                            UpdateGONOGOLabelColor(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)

                                                                        End If

                                                                        'ElseIf (myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)) And myDGs(z).Name = "DTCs" Then
                                                                        '    If SaveDTCNumber <> mytempval Then
                                                                        '   SaveDTCNumber = mytempval
                                                                        '    CopyToLog("myBackgroundTasks: Active DTC = " & myDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y)))
                                                                    End If
                                                                    '    myDTCsSaveTime = Now
                                                                    '    myDTCsElapseTime = Now.Subtract(myDTCsSaveTime)
                                                                    '    DTCTimerActive = True
                                                                End If

                                                            ElseIf mytempval <= myDGs(z).HighThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 And myDGs(z).Name = "DTCs" Then

                                                                If DTCTimerActive = True Then

                                                                    myDTCsElapseTime = Now.Subtract(myDTCsSaveTime)
                                                                    If myDTCsElapseTime.Seconds > 5 Then
                                                                        ActiveDTC = False
                                                                        DTCTimerActive = False
                                                                        myDTCsSaveTime = Now
                                                                        CopyToLog("myBackgroundTasks: No Active DTCs")

                                                                        GoNoGoFault(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = False
                                                                        UpdateGONOGOLabelColor(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Green)

                                                                    End If

                                                                End If

                                                            End If
                                                            WhereAmI = "LowThresh Check"
                                                            If mytempval < myDGs(z).LowThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
                                                                If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).LowThreshBackColor(x, y)) Then
                                                                    If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                                        CopyToLog("myBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & mytempval & " Threshold Value: " & myDGs(z).LowThresh(x, y), 1)
                                                                    Else
                                                                        CopyToLog("myBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & mytempval & " Threshold Value: " & myDGs(z).LowThresh(x, y), 2)
                                                                    End If

                                                                    GridUpdateAction = GridUpdateActions.TO_LOW
                                                                    UpdateGridColor(z, x, y, GridUpdateAction)
                                                                End If
                                                                If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                                    GoNoGoFault(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                                    UpdateGONOGOLabelColor(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                                                                End If

                                                            End If
                                                            WhereAmI = "Reset Check HIGH"
                                                            If mytempval <= myDGs(z).HighThresh(x, y) And mytempval >= myDGs(z).LowThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
                                                                If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)) Then
                                                                    CopyToLog("myBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & mytempval & " Threshold Value: " & myDGs(z).HighThresh(x, y), 2)
                                                                    GridUpdateAction = GridUpdateActions.FROM_HIGH
                                                                    UpdateGridColor(z, x, y, GridUpdateAction)

                                                                End If
                                                            End If
                                                            WhereAmI = "Reset Check LOW"
                                                            If mytempval >= myDGs(z).LowThresh(x, y) And mytempval <= myDGs(z).HighThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
                                                                If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).LowThreshBackColor(x, y)) Then
                                                                    CopyToLog("myBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & mytempval & " Threshold Value: " & myDGs(z).LowThresh(x, y), 2)
                                                                    GridUpdateAction = GridUpdateActions.FROM_LOW
                                                                    UpdateGridColor(z, x, y, GridUpdateAction)

                                                                End If
                                                            End If
                                                            WhereAmI = "Equal To Check"
                                                            If Len(myDGs(z).EqualTo(x, y)) > 0 And myDGs(z).CheckForDataChange(x, y) = False Then
                                                                If mytempval = Val(myDGs(z).EqualTo(x, y)) Then
                                                                    If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).LowThreshBackColor(x, y)) Then

                                                                        If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                                            CopyToLog("myBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & mytempval & " Equals " & Val(myDGs(z).EqualTo(x, y)), 1)
                                                                        Else
                                                                            CopyToLog("myBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & mytempval & " Equals " & Val(myDGs(z).EqualTo(x, y)), 2)
                                                                        End If

                                                                        GridUpdateAction = GridUpdateActions.TO_LOW
                                                                        UpdateGridColor(z, x, y, GridUpdateAction)
                                                                    End If

                                                                    If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                                                                        GoNoGoFault(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                                                                        UpdateGONOGOLabelColor(myDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                                                                    End If

                                                                Else
                                                                    If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).LowThreshBackColor(x, y)) Then

                                                                        CopyToLog("myBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & mytempval & " Not Equal To " & Val(myDGs(z).EqualTo(x, y)), 2)
                                                                        GridUpdateAction = GridUpdateActions.FROM_LOW
                                                                        UpdateGridColor(z, x, y, GridUpdateAction)

                                                                    End If

                                                                End If
                                                            End If

                                                        End If

                                                    End If

                                                Else
                                                    mytempstr = "UnRgstrd"
                                                End If

                                                WhereAmI = "Build Display Array for Grid"

                                                If myDGs(z).Parent.Visible = True Then

                                                    If x > 0 And y >= 1 Then
                                                        myDGs(z).DataArray(x - 1, y) = mytempstr
                                                    End If

                                                    WhereAmI = "Total Num Signals Displayed Calculation..."

                                                    L_totalnumsignalsdisplayed = L_totalnumsignalsdisplayed + 1

                                                End If

                                            Next y

                                        Next x

                                    Catch ex As Exception

                                        If LogMsgWritten = False Then
                                            CopyToLog("myBackgroundTasks - Grid Rows and Columns: " & ex.Message)
                                            LogMsgWritten = True
                                        End If

                                    End Try

                                    WhereAmI = "Out of X Y Loops - Ready to Refresh Grid..."

                                    If myDGs(z).Parent.Visible = True Then
                                        'If x > 0 And x < myDGs(z).Rows - 1 Then
                                        'mystr = mystr & Chr(13)
                                        'End If
                                        myDGs(z).Refresh()
                                    End If

                                End If

                            Next z

                            WhereAmI = "Out of Z Loop..."

                            'Because there may be multiple grid cells that can influence go/nogo label colors, we must
                            'reconcile what the actual color of the label should be here, after going through all forms
                            'grids and cells above.  Here we also reset the GoNoGoFault array here so nothing gets "stuck on"...
                            For z = 0 To UBound(myDFs)
                                If myDFs(z).GoNoGoIndex > -1 Then
                                    If GoNoGoFault(myDFs(z).GoNoGoIndex) = False Then
                                        UpdateGONOGOLabelColor(myDFs(z).GoNoGoIndex, Color.Green)
                                    Else
                                        UpdateGONOGOLabelColor(myDFs(z).GoNoGoIndex, Color.Red)

                                        For i = 0 To UBound(myDGs)

                                            If myDGs(i).ParentFormIndex = z Then
                                                For x = 0 To myDGs(i).RowCount
                                                    For y = 0 To myDGs(i).ColumnCount - 1
                                                        If myDGs(i).DataFrozen(x, y) = True Then 'And myDGs(i).AlsoAssociatedWith(x, y) = "GO/NOGO" Then
                                                            DataFrozen = True
                                                            Exit For
                                                        End If
                                                    Next y
                                                    If DataFrozen = True Then
                                                        Exit For
                                                    End If
                                                Next x
                                                If DataFrozen = True Then
                                                    Exit For
                                                End If
                                            End If

                                        Next i

                                        If DataFrozen = False Then
                                            GoNoGoFault(myDFs(z).GoNoGoIndex) = False
                                            If myDFs(z).Name = "INCA Video Status" Then
                                                UpdateGONOGOLabelColor(myDFs(z).GoNoGoIndex, Color.Green)
                                            ElseIf myDFs(z).Name = "INCA COMM Status" Then
                                                UpdateGONOGOLabelColor(myDFs(z).GoNoGoIndex, Color.Green)
                                            End If
                                        Else
                                            DataFrozen = False
                                        End If

                                    End If

                                End If

                            Next z

                        End If 'Check for myDGs(0) Is Nothing
                    End If 'Check for mySignalDataWithTime Is Nothing

                    'We sleep for DisplayRefreshRate msec in between loop processing here....
                    'Values can be 0 to 100 msec...

                    If Val(DisplayRefreshRate) > 0 Then
                        System.Threading.Thread.Sleep(Val(DisplayRefreshRate))
                    End If

                    'Here is where we save the CLEVIR record file if we transitioned out of RecordPlayBack.RecordMode = True
                    If RecordPlayback.RecordMode = False And LineNum > 0 Then
                        SaveRecordFile(LineNum)
                        LineNum = -1
                    End If

                    If RecordPlayback.PlaybackMode = RecordPlayback.PLAYBACK_STATES.PlaybackStepFwd Or
                    RecordPlayback.PlaybackMode = RecordPlayback.PLAYBACK_STATES.PlaybackStepBack Then
                        RecordPlayback.GroupBox2.Text = "PLAYBACK (Pause)"
                    End If

                    TotalNumSignalsDisplayed = L_totalnumsignalsdisplayed

                    WhereAmI = "Finished in MeasurementStatus = True"

                Else 'Measurement not started and not playing back CLEVIR Recording....

                    HandleNonMeasureModeUpdates(SaveODOReading, SaveWhereAmIAt, GoNoGoFault)

                    'Save CLEVIR Recording when transitioning out of measure mode if we were recording...
                    If RecordPlayback.RecordMode = True Then
                        If LineNum > 0 Then
                            SaveRecordFile(LineNum)
                        End If
                        RecordPlayback.RecordMode = False
                        LineNum = -1
                    End If

                    System.Threading.Thread.Sleep(100)
                    System.Windows.Forms.Application.DoEvents()

                End If

                Retries = 0

            Loop

        Catch ex As Exception

            If LogMsgWritten = False Then
                CopyToLog("MyBackgroundTasks: " & WhereAmI & " - " & ex.Message)
                LogMsgWritten = True
            End If

            'Resume Next

        End Try

    End Sub

    Public Function GetEnumDescription(ByVal DisplayValue As Double, ByVal VariableName As String, ByVal DeviceName As String) As String

        'Called from FormatDisplayString:
        'Gets the Enumeration Description text for a given variable.  
        'This function is called when updating the variable display if the variable is defined with a DisplayFormat of ENUM
        'Looks for the variable name in the mySignalEnums data structure and returns the enumeration based on the display value passed in.

        Dim x As Integer
        Dim found As Boolean

        Try
            GetEnumDescription = "XXXX"

            For x = 0 To UBound(mySignalEnums)

                If InStr(UCase(VariableName), "SDFIC") = 0 Then

                    If InStr(VariableName, mySignalEnums(x).VariableName) > 0 Then

                        If Int(DisplayValue) >= 0 And Int(DisplayValue) <= UBound(mySignalEnums(x).Enums) Then

                            If Not mySignalEnums(x).Enums(Int(DisplayValue)) Is Nothing Then
                                If mySignalEnums(x).DeviceName = DeviceName Or mySignalEnums(x).DeviceName = "" Then
                                    GetEnumDescription = mySignalEnums(x).Enums(Int(DisplayValue))
                                    found = True
                                    Exit For
                                End If

                            Else
                                GetEnumDescription = "XXXX"
                            End If

                        End If

                    End If
                Else
                    If mySignalEnums(x).VariableName = "DaIDRR_e_DTC_List" Then

                        If Int(DisplayValue) >= 0 And Int(DisplayValue) <= UBound(mySignalEnums(x).Enums) Then

                            If Not mySignalEnums(x).Enums(Int(DisplayValue)) Is Nothing Then
                                If mySignalEnums(x).DeviceName = DeviceName Or mySignalEnums(x).DeviceName = "" Then
                                    GetEnumDescription = mySignalEnums(x).Enums(Int(DisplayValue))
                                    found = True
                                    Exit For
                                End If

                            Else
                                GetEnumDescription = "XXXX"
                            End If

                        End If

                    End If
                End If

            Next

            If GetEnumDescription = "XXXX" Or found = False Then
                GetEnumDescription = Format(DisplayValue, "0")
            End If

        Catch ex As Exception

            CopyToLog("GetEnumDescription: " & ex.Message & " - " & VariableName)
            GetEnumDescription = Format(DisplayValue, "0")

        End Try

    End Function

    Private Function FormatDisplayString(ByVal DisplayValue As Double, ByVal DisplayFormat As String, ByVal DeviceName As String, ByVal VariableName As String) As String

        'Called from MyBackgroundTasks:
        'Formats the string which will be displayed for a given variable.  DisplayFormat is read in from excel spreadsheet.  The raw number can 
        'be displayed to the level of precision defined using a format string such as "0"  or "0.0", or other formats can be used such as
        '"HEX" or "TRUE/FALSE", etc.  If the DisplayFormat is "ENUM", GetEnumDescription is called.

        Try

            FormatDisplayString = ""

            Select Case UCase(DisplayFormat)
                Case "ENUM"

                    FormatDisplayString = GetEnumDescription(CInt(DisplayValue), VariableName, DeviceName)
                    If Not IsNumeric(Mid(FormatDisplayString, 1, 1)) Then
                        FormatDisplayString = CStr(CInt(DisplayValue)) & " " & FormatDisplayString
                    End If

                Case "TRUE/FALSE"
                    FormatDisplayString = IIf(CInt(DisplayValue) = 0, "0 True", "1 False")
                Case "FALSE/TRUE"
                    FormatDisplayString = IIf(CInt(DisplayValue) = 0, "0 False", " 1 True")
                Case "INVALID/VALID"
                    FormatDisplayString = IIf(CInt(DisplayValue) = 0, "0 Invalid", " 1 Valid")
                Case "VALID/INVALID"
                    FormatDisplayString = IIf(CInt(DisplayValue) = 0, "0 Valid", "1 Invalid")
                Case "HEX"
                    FormatDisplayString = Hex(CStr(DisplayValue))
                Case "DEC"
                    FormatDisplayString = Format$(CInt(DisplayValue), "0")
                Case "BINARY"
                    FormatDisplayString = Convert.ToString(CLng(DisplayValue), 2).PadLeft(8, "0"c)
                Case Else 'assumes ""0"", or ""0.0"" or ""0.000"", etc....
                    FormatDisplayString = Format$(DisplayValue, DisplayFormat)
            End Select

        Catch ex As Exception

            CopyToLog("FormatDisplayString: " & ex.Message & " - " & VariableName)
            FormatDisplayString = "-"

        End Try

    End Function

    Private Sub HandleLogin()

        'Called from Initialize on startup.  Shows the login form and allows the user to login as part of the CLEVIR
        'initialization sequence.  Determines the current login state.

        Dim DialogResult As Integer

        OnVehicleScreen.ShowInTaskbar = False
        LoginForm.ShowInTaskbar = True
        OnVehicleScreen.Visible = False

        LoginForm.Button44.Visible = True

        'First, we will call VerifyCLEVIRConfiguration to verify the current CLEVIR configuration as defined in the config.txt file.  
        'This verification involves two basic steps.  Step 1 is to go to the share drive and look for updated experiment and signal list 
        'files that have the same model year And software version as implied by the workspace name defined in the config.txt file.
        'If newer files are found, they are copied (the experiment is imported into INCA) and the user is given choice to update to use the
        'new files.  If no new files are found, or if the user chooses not to update, the signal list file and experiment file model year
        'and software version are checked against the workspace.  If model year and or software versions do not match, warning messages
        'are presented to the user indicating this...

        LoginForm.VerifyCLEVIRConfiguration(True)

        'If Len(SaveLoginID) = 0 Then 'we are not logged in yet...

        'Next, we display the login form modally, that is, the login form must be responded to in some manner before we continue
        'with the rest of the code in this routine...

        'We loop here because the user has various choices from the loginform.  The choices may dictate that the
        'loginform be redisplayed.  The login form is used in conjunction with the SoftwareVersionSelect form which allows
        'the user to make changes to workspace and experiment selections prior to initialization...
        Do While Len(SaveLoginID) = 0 Or DialogResult = Windows.Forms.DialogResult.Cancel

            LoginForm.ShowDialog()

            'DebugMode is set to True if the user holds down any alphabetic key when logging in.  DebugMode True causes
            'CLEVIR to allow the user to continue to the main screen if there is a mismatch between workspace contents
            'and what is flashed into the controller.  The default, DebugMode = False will cause CLEVIR to exit if there
            'is a mismatch.  

            'Debug mode also influences what happens when the user logs in using a user id other than Demo.

            If Debugger.IsAttached = True Then 'When debugging, we will choose debug mode on or off here, not with a key press...
                If MsgBox("DebugMode?", vbYesNo) = vbYes Then
                    DebugMode = True
                End If
            End If

            Me.Cursor = Cursors.WaitCursor

            'Here is where we will handle different startup scenarios based on different login ids.....
            If Len(SaveLoginID) > 0 Then
                'If SaveLoginID is not DEMO, this means that the user logged in with a different user id and will be
                'using a different configuration file...  If they have logged in when pressing an alphabetic key, this
                'means that they want to run in debug mode and trust the selections in the configuration file.  If a
                'key is not held down, then the softwareversionselect screen will be dislayed and they will have the
                'opportunity to change their selections prior to initialization...
                If InStr(UCase(SaveLoginID), "DEMO") = 0 Then

                    'If we are not logged in as "DEMO" we will read a user specific config file based on login name...
                    UserConfigFileName = SaveLoginID & ".txt"
                    ReadUserConfigFile(UserConfigFileName)

                    'The DebugKey flag will set if we hold down any alphabetic key on the keyboard at the same time we click 
                    'on a user login id either from the set of buttons on the left of the login screen, or if the selection
                    'is made in the list box.  Pressing this key when logging in bypasses the SoftwareVersionSelect screen and
                    'allows the user to continue to the main screen even if there is a mismatch between INCA and what flashed
                    'into the controller.  Pressing a DebugKey suggests that the user knows what they are doing and they
                    'know that the contents of their config file is correct...  DebugMode is set if DebugKey is held down, or
                    'if running with debugger attached (from design environment for testing)...

                    If DebugMode = False Then
                        DialogResult = SoftwareVersionSelect.ShowDialog()
                    Else
                        Exit Do
                    End If

                Else 'User pressed the login as demo button from the loginform...
                    Exit Do
                End If

            Else 'Len(SaveLoginID) = 0 indicating that the Select Different Demo Workspace / Experiment button was pressed on LoginForm...
                DialogResult = SoftwareVersionSelect.ShowDialog()
            End If

        Loop

        CopyToLog("HandleLogin: Logged in as " & SaveLoginID)

        'Handle if SaveCalSnapshot checkbox checked value was changed on handle Login form...

        If SaveCalSnapshotEnabledChanged = True Then
            SaveCalSnapshotEnabled = LoginForm.CheckBox1.Checked
        Else
            LoginForm.CheckBox1.Checked = SaveCalSnapshotEnabled
        End If

        LoginForm.Button44.Visible = False

        OnVehicleScreen.ShowInTaskbar = True
        LoginForm.ShowInTaskbar = False
        OnVehicleScreen.Visible = True

    End Sub
    Public Sub HandleRecordingProgressBar(ByVal Status As Boolean, ByVal ProgressBar As ProgressBar)

        'HandleRecordingProgressBar is called from myBackgroundTasks routine and from StartStopRecording and StartStopMeasurement

        'This routine handles the green recording progress bar that is displayed either on the RecordPlayback form
        'or on the main CLEVIR screen.  

        'The progressbar to operate on (OnVehicleScreen progressbar or RecordPlayback progressbar) 
        'is passed in as ProgressBar.

        If Status = True Then
            If ProgressBar.Value < ProgressBar.Maximum Then
                If ProgressBar.Value = 0 Then
                    ProgressBar.Visible = True
                End If
                ProgressBar.Value = ProgressBar.Value + 5
            Else
                ProgressBar.Value = 0
            End If
        Else
            ProgressBar.Value = 0
        End If

    End Sub

    Private Sub SaveRecordFile(ByVal LineNum As Integer)

        'This routine will save the APPLICATION record file when the stop record button is pressed on the RecordPlayback form.  This should
        'not be confused with INCA recording.  This recording file is a .csv file which can then be 
        'played back by the resident client to visualize previous live data.

        Dim fnum As Integer
        Dim filename As String
        Dim x As Integer
        Dim y As Integer
        Dim savestring As String = ""

        fnum = FreeFile()
        filename = Mid(INCAVariableFile, 1, InStr(INCAVariableFile, ".xlsx") - 1) & ".csv"
        FileOpen(fnum, filename, OpenMode.Output)

        For x = 0 To LineNum
            For y = 0 To UBound(VariableNameDataArray, 1)
                If y = 0 Then
                    savestring = VariableNameDataArray(y, x)
                Else
                    savestring = savestring & "," & VariableNameDataArray(y, x)
                End If
            Next
            PrintLine(fnum, savestring)
            savestring = ""
        Next

        FileClose(fnum)

        MsgBox("Please Specify a Filename for the CLEVIR Record .csv file...")

        SaveFileDialog1.DefaultExt = ".csv"
        SaveFileDialog1.FileName = filename
        SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
        SaveFileDialog1.ShowDialog()

        If Len(SaveFileDialog1.FileName) > 0 And SaveFileDialog1.FileName <> filename Then
            FileCopy(filename, SaveFileDialog1.FileName)

        End If

    End Sub
    Private Function GetRecordedData(ByVal NumValidVars As Integer, ByVal PlaybackMode As RecordPlayback.PLAYBACK_STATES) As IGM_INCA_Comm.INCAData

        'This function is called from the myBackGroundTasks routine (each execution loop) if we are playing back a recording.
        'The first time this is called, we read in the entire playback file and we map the signal names in the
        'file to the signal name order which is defined for this execution run (this order comes from the
        'excel file which contains the signal names, forms, etc.).  So, if we save a file from a different
        'configuration and play it back, it should still play properly, unless there are signals missing
        'in the playback file, in which case, these signal values will be zero.

        Dim myINCAData As IGM_INCA_Comm.INCAData = New IGM_INCA_Comm.INCAData

        Static myDataArray(,) As String
        Static x As Integer
        Dim y As Integer
        Dim i As Integer
        Dim z As Integer
        Dim direction As Integer
        Dim found As Boolean

        Dim myTempArray() As String
        Dim SaveTempArrayNames() As String

        Dim fnum As Long

        Try

            'we load the contents from the entire playback file into memory here on the first
            'instance of a playback request coming from the playback controls.

            If ReadNewDataFile = True Then

                ReadNewDataFile = False

                Me.Cursor = Cursors.WaitCursor
                RecordPlayback.Label2.Visible = True
                RecordPlayback.Refresh()

                fnum = FreeFile()
                FileOpen(fnum, RecordPlayback.PlayBackFileName, OpenMode.Input)

                ReDim myDataArray((UBound(myINCAInterface.myDisplaySignals) + 2), 0)

                x = 0
                ReDim SaveTempArrayNames(0)

                Do While Not EOF(fnum)
                    ReDim Preserve myDataArray((UBound(myINCAInterface.myDisplaySignals) + 2), x) 'col, row

                    myTempArray = Split(LineInput(fnum), ",") 'columns in row

                    For y = 0 To UBound(myINCAInterface.myDisplaySignals)

                        If x = 0 Then
                            myDataArray(0, 0) = myTempArray(0)
                            myDataArray(1, 0) = myTempArray(1)
                            For z = 0 To UBound(myTempArray)
                                found = False
                                If myINCAInterface.myDisplaySignals(y).SignalName = myTempArray(z) Then
                                    myDataArray(y + 2, x) = myTempArray(z)
                                    If z > UBound(SaveTempArrayNames) Then
                                        ReDim Preserve SaveTempArrayNames(z)
                                    End If
                                    SaveTempArrayNames(z) = myTempArray(z)
                                    found = True
                                    Exit For
                                End If
                            Next
                        Else
                            myDataArray(0, x) = myTempArray(0)
                            myDataArray(1, x) = myTempArray(1)
                            For z = 0 To UBound(SaveTempArrayNames)
                                found = False
                                If myINCAInterface.myDisplaySignals(y).SignalName = SaveTempArrayNames(z) Then
                                    myDataArray(y + 2, x) = myTempArray(z)
                                    'WriteToListBox(SaveTempArrayNames(z) & " - " & myINCAInterface.myDisplaySignals(y).SignalName & " - " & z & " - " & y+1)
                                    found = True
                                    Exit For
                                End If
                            Next
                            If found = False Then
                                myDataArray(y + 2, x) = 0.0
                                'WriteToListBox("Setting " & myINCAInterface.myDisplaySignals(y).SignalName & " to 0")
                            End If
                        End If

                    Next y

                    x = x + 1
                Loop

                RecordPlayback.HScrollBar1.Maximum = x - 1
                RecordPlayback.HScrollBar1.Minimum = 1

                FileClose(fnum)

                Me.Cursor = Cursors.Arrow
                RecordPlayback.Label2.Visible = False
                RecordPlayback.Refresh()

            End If

            ReDim myINCAData.myData(NumValidVars - 1)
            ReDim myINCAData.myTime(NumValidVars - 1)

            '+/- direction is established by the playback state...

            Select Case PlaybackMode
                Case RecordPlayback.PLAYBACK_STATES.PlaybackRun, RecordPlayback.PLAYBACK_STATES.PlaybackStepFwd
                    direction = 1
                Case RecordPlayback.PLAYBACK_STATES.PlaybackStepBack
                    direction = -1
                Case RecordPlayback.PLAYBACK_STATES.PlaybackScrolling
                    direction = 0
            End Select

            'handle boundary conditions...

            If SaveLineNumber + direction = -1 Then
                SaveLineNumber = 2
            End If

            If SaveLineNumber + direction > RecordPlayback.HScrollBar1.Maximum Then
                SaveLineNumber = RecordPlayback.HScrollBar1.Maximum - direction
            End If

            'loop thru the saved array and load the INCAData to return to caller....

            'this is done for each device/raster pair in the same manner as when
            'the "live" getdataarray is called - the data structure and organization
            'must be exactly the same in both playback and live....

            i = 0
            For y = 2 To (NumValidVars)
                myINCAData.myData(i) = myDataArray(y, SaveLineNumber + direction)
                myINCAData.myTime(i) = myDataArray(1, SaveLineNumber + direction)
                myINCAData.myStatus = True
                i = i + 1
            Next y

            'after all of the device/raster pair data has been loaded, we save the line
            'number that we are operating on for the playback functions....

            If y = UBound(myINCAInterface.myDisplaySignals) + 3 Then
                RecordPlayback.HScrollBar1.Value = SaveLineNumber + direction
                SaveLineNumber = SaveLineNumber + direction

                If SaveLineNumber = RecordPlayback.HScrollBar1.Maximum Then
                    RecordPlayback.StepForward.Enabled = False
                    RecordPlayback.StepBack.Enabled = True
                    RecordPlayback.Reset.Enabled = True
                End If

                If SaveLineNumber = RecordPlayback.HScrollBar1.Minimum Then
                    RecordPlayback.PlayPauseButton.Enabled = True
                    RecordPlayback.StepForward.Enabled = True
                    RecordPlayback.StepBack.Enabled = False
                    RecordPlayback.Reset.Enabled = True
                End If

            End If

            RecordPlayback.Label1.Text = SaveLineNumber & " of " & RecordPlayback.HScrollBar1.Maximum

            GetRecordedData = myINCAData

        Catch ex As Exception

            GetRecordedData = myINCAData
            CopyToLog("GetRecordedData: " & ex.Message)

        End Try

    End Function

    Private Sub UpdateGONOGOLabelColor(ByVal myGridIndex As Integer, ByVal myColor As Color)

        'Called out of the myBackgroundTasks routine, handles setting back color of each of the GONOGO 
        'labels on the main screen.

        Static SaveBackColor(0 To 15) As Color

        Try

            If SaveBackColor Is Nothing Then
                ReDim SaveBackColor(0 To 15)
            End If

            If myGridIndex <> -1 Then

                myLabel(myGridIndex).BackColor = myColor

                If myLabel(myGridIndex).Text = "INCA COMM Status" And IgnoreLostDeviceUntilNextRecordingSession = False And IgnoreLostDeviceForThisDrive = False Then

                    If myColor = Color.Red Then
                        If OverrideCommFailureInDebugMode = False Then
                            BackgroundLoopCounterNotUpdating = True
                        End If

                        myCPUStaleData = True
                    Else
                        BackgroundLoopCounterNotUpdating = False
                        myCPUStaleData = False
                    End If

                End If

                If myLabel(myGridIndex).Text = "INCA Video Status" Then

                    If myINCAInterface.Recording = False Then
                        myLabel(myGridIndex).BackColor = Color.Gray
                    ElseIf myINCAInterface.Recording = True Then
                        myLabel(myGridIndex).BackColor = myColor

                        If myColor = Color.Red And OverrideCommFailureInDebugMode = False And IgnoreLostDeviceUntilNextRecordingSession = False And IgnoreLostDeviceForThisDrive = False Then
                            VideoCameraNotUpdating = True
                        Else
                            VideoCameraNotUpdating = False
                        End If
                    End If

                    'Else
                    '    myLabel(myGridIndex).BackColor = myColor
                End If

                If myLabel(myGridIndex).Text = "CPU Status" Then

                    If myColor = Color.Red Then
                        myCPUStaleData = True
                    Else
                        myCPUStaleData = False
                    End If

                End If

                If (SaveBackColor(myGridIndex) <> myLabel(myGridIndex).BackColor) And myLabel(myGridIndex).BackColor <> Color.Gray Then

                    If myLabel(myGridIndex).Text <> "EOCM DTCs" Then
                        CopyToLog("UpdateGONOGOLabelColor: " & myLabel(myGridIndex).Text & " GO/NOGO is ----------------- " & UCase(myLabel(myGridIndex).BackColor.ToString))
                        'Else
                        '    If myColor = Color.Red Then
                        '    CopyToLog("UpdateGONOGOLabelColor: " & myLabel(myGridIndex).Text & " GO/NOGO is ----------------- " & UCase(myLabel(myGridIndex).BackColor.ToString))
                        'End If
                    End If

                End If

                    SaveBackColor(myGridIndex) = myLabel(myGridIndex).BackColor

            End If

        Catch ex As Exception

            If ContinueExecution = False Then
                OnVehicleScreen.TopMost = True
                If MsgBox("UpdateGONOGOLabelColor: " & ex.Message, vbRetryCancel) = vbRetry Then
                    ContinueExecution = True
                    CopyToLog("UpdateGONOGOLabelColor: " & ex.Message)
                Else
                    CopyToLog("UpdateGONOGOLabelColor: " & ex.Message & " User selected cancel.")
                    ExitApp("Complete")
                End If
            End If
            OnVehicleScreen.TopMost = False

        End Try

    End Sub

    Private Sub UpdateGridColor(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal Action As GridUpdateActions)

        'This routine is called from myBackGroundTasks. It handles setting the back color of individual cells based on 
        'the threshold settings for the variable associated with a particular grid cell....

        Try

            myDGs(z).SuspendLayout()

            Select Case Action
                Case GridUpdateActions.FROM_HIGH, GridUpdateActions.FROM_LOW

                    'myDGs(z).CellBackColor = myDGs(z).DefaultCellBackColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    'myDGs(z).CellForeColor = myDGs(z).DefaultCellForeColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    myDGs(z).CurrentBackColor(x, y) = myDGs(z).DefaultCellBackColor(x, y)
                    myDGs(z).CurrentForeColor(x, y) = myDGs(z).DefaultCellForeColor(x, y)

                    myDGs(z).Rows(x - 1).Cells(y).Style.BackColor = myDGs(z).DefaultCellBackColor(x, y)
                    myDGs(z).Rows(x - 1).Cells(y).Style.ForeColor = myDGs(z).DefaultCellForeColor(x, y)


                Case GridUpdateActions.TO_HIGH

                    'myDGs(z).CellBackColor = myDGs(z).HighThreshBackColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    'myDGs(z).CellForeColor = myDGs(z).HighThreshForeColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)
                    myDGs(z).CurrentForeColor(x, y) = myDGs(z).HighThreshForeColor(x, y)

                    myDGs(z).Rows(x - 1).Cells(y).Style.BackColor = myDGs(z).HighThreshBackColor(x, y)
                    myDGs(z).Rows(x - 1).Cells(y).Style.ForeColor = myDGs(z).HighThreshForeColor(x, y)

                Case GridUpdateActions.TO_LOW

                    'myDGs(z).CellBackColor = myDGs(z).LowThreshBackColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    'myDGs(z).CellForeColor = myDGs(z).LowThreshForeColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    myDGs(z).CurrentBackColor(x, y) = myDGs(z).LowThreshBackColor(x, y)
                    myDGs(z).CurrentForeColor(x, y) = myDGs(z).LowThreshForeColor(x, y)

                    myDGs(z).Rows(x - 1).Cells(y).Style.BackColor = myDGs(z).LowThreshBackColor(x, y)
                    myDGs(z).Rows(x - 1).Cells(y).Style.ForeColor = myDGs(z).LowThreshForeColor(x, y)

            End Select

        Catch ex As Exception

            If ContinueExecution = False Then
                OnVehicleScreen.TopMost = True
                If MsgBox("UpdateGridColor: " & ex.Message, vbRetryCancel) = vbRetry Then
                    ContinueExecution = True
                    CopyToLog("UpdateGridColor: " & ex.Message)
                Else
                    CopyToLog("UpdateGridColor: " & ex.Message & " User selected cancel.")
                    ExitApp("Complete")
                End If
            End If
            OnVehicleScreen.TopMost = False
        End Try

    End Sub

    Private Sub GM_ResidentClient_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated

    End Sub

    Private Sub GM_ResidentClient_BackgroundImageChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.BackgroundImageChanged

    End Sub

    Private Sub GM_ResidentClient_CausesValidationChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.CausesValidationChanged

    End Sub

    Public Sub GM_ResidentClient_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Click

        'When you click in the GM_ResidentClient form (this form is the container for the OnVehicleScreen and other forms when
        'not emulating an in vehicle PC), this code re-displays whatever other form was visible at the same location as the
        'GM_ResidentClient form and brings the form to the front.

        'This supports moving the CLEVIR display like a regular movable window when running on a laptop for instance, rather then having
        'the display stationary in the top left corner of the screen...

        If OperatingMode <> OperatingModes.RES_ON_VPC And
    OperatingMode <> OperatingModes.RES_ON_SUITCASE_VPC Then

            OnVehicleScreen.Top = Me.Top + 60
            OnVehicleScreen.Left = Me.Left
            OnVehicleScreen.Activate()
            OnVehicleScreen.BringToFront()

            LoginForm.Top = Me.Top + 60
            LoginForm.Left = Me.Left

            SelectDisplays.Top = Me.Top + 60
            SelectDisplays.Left = Me.Left

        End If

    End Sub

    Private Sub GM_ResidentClient_Enter(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Enter

    End Sub

    Private Sub GM_ResidentClient_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed


    End Sub

    Private Sub BackgroundWorker1_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork

        'the background worker handles the main execution loop

        _BackgroundTasks = New BackgroundTasks(AddressOf MyBackgroundTasks)
        BeginInvoke(_BackgroundTasks)

    End Sub

    Private Sub GM_ResidentClient_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        'Executed on the GM_ResidentClient.Close event.

        If (OperatingMode <> OperatingModes.RES_ON_VPC And
OperatingMode <> OperatingModes.RES_ON_SUITCASE_VPC) Then

            If ExitApp() = True Then
                Me.Hide()
            End If
        Else

            If Me.Visible = True Then
                Me.Hide()
            Else
                If ExitApp() = True Then
                    Me.Hide()
                End If
            End If
        End If

        e.Cancel = True

    End Sub

    Private Sub GM_ResidentClient_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.GotFocus

    End Sub
    Private Sub ProcessKiller()

        'This is a monitor, running on a separate thread from the myBackgroundTasks thread.  This monitors the myBackgroundTasks
        'thread make sure that it is still looping, and not hung on a call to INCA.  It also monitors the DeviceStatus to determine
        'if any devices are InActive.  Also, it monitors the global variable myCPUStaleData, which tells us if we have any "stale" data, 
        'which would indicate a data update issue with one or more devices.

        'This routine creates a small form with three labels on it, which will be displayed to alert user of comm issues.  This allows the user
        'to readily see the overall status of the application as related to INCA.  This also allows the user to click on any of the
        'labels which brings up a msgbox asking the user if they want to kill processes.  Killing processes will kill INCA and its
        'related tasks and CLEVIR, if we are hung on an INCA API call.  This would need to be done if CLEVIR hangs on a call to INCA due 
        'to an INCA issue.

        'This routine also keeps track of and displays how long it is taking for INCA to initialize during the GM_ResidentClient
        'initialization process.  During initialization, a single label is displayed.  The label back color will change from green 
        'to yellow to red depending on how long it takes to initialize.  Once initialization is complete, up to three labels may be displayed
        'depending on the comm status, "B - Blocked", "D - Device Inactive", or "S - Stale Data". 

        Dim IPMServerProc As Process()
        Dim tgtsvrProc As Process()
        Dim IncaProc As Process()

        Dim INCAInitElapseTime As TimeSpan
        Dim SaveINCAInitElapseTime As DateTime = Now

        Dim EncryptElapseTime As TimeSpan
        Dim SaveEncryptElapseTime As DateTime = Now

        Dim myForm As Form
        Dim myFont As Font
        Dim myLabel As Label

        Dim myLabel1 As Label
        Dim myLabel2 As Label
        Dim myLabel3 As Label

        Dim SaveHealthMonitor As Integer

        Dim INCAInitDurationMinutes As Single = 2.75
        Dim INCAInitDurationMinutesRed As Single = 3.5

        Dim synth As New SpeechSynthesizer
        Dim mysavetime As DateTime = Now
        Dim myElapseTime As TimeSpan

        Dim mysavetime2 As DateTime = Now
        Dim myElapseTime2 As TimeSpan

        Dim mysavetime3 As DateTime = Now
        Dim myElapseTime3 As TimeSpan

        Dim mysavetime4 As DateTime = Now
        Dim myElapseTime4 As TimeSpan

        Const AUDIO_MESSAGE_FREQUENCY As Integer = 10 'seconds

        myForm = Nothing
        myFont = Nothing
        myLabel = Nothing

        myLabel1 = Nothing
        myLabel2 = Nothing
        myLabel3 = Nothing

        synth.Rate = -2

        'we loop until the KillProcess flag is set to true by the user, indicating that they wish to shut everything down...
        Do While KillProcesses = False

            System.Windows.Forms.Application.DoEvents()

            'initially, we need to dynamically create our forms and labels
            If myForm Is Nothing And INCAInitStarted = True Then

                myForm = New Form
                myForm.FormBorderStyle = Windows.Forms.FormBorderStyle.FixedSingle
                myForm.ControlBox = False
                myForm.MaximizeBox = False
                myForm.MinimizeBox = False
                myForm.Text = "INCA Status"

                myForm.ShowInTaskbar = False

                myForm.StartPosition = FormStartPosition.Manual

                myForm.Height = 67
                myForm.Width = 84

                myForm.Top = 85
                myForm.Left = 5
                myForm.BackColor = Color.MediumSpringGreen

                myLabel = New Label

                myLabel1 = New Label
                myLabel2 = New Label
                myLabel3 = New Label

                myLabel1.Parent = myForm
                myLabel1.Top = 0
                myLabel2.Parent = myForm
                myLabel2.Top = 0
                myLabel3.Parent = myForm
                myLabel3.Top = 0

                myLabel1.Left = 0
                myLabel1.Height = myForm.Height - 33
                myLabel1.Width = 42
                myLabel1.BackColor = myForm.BackColor

                myLabel1.TextAlign = ContentAlignment.MiddleCenter

                myLabel1.Font = New Font(myLabel.Font.FontFamily, 8)
                myLabel1.Font = New Font(myLabel.Font, FontStyle.Bold)
                myLabel1.ForeColor = Color.Black

                myLabel1.Visible = False

                myLabel2.Left = 42
                myLabel2.Height = myForm.Height - 33
                myLabel2.Width = 42
                myLabel2.BackColor = myForm.BackColor

                myLabel2.TextAlign = ContentAlignment.MiddleCenter

                myLabel2.Font = New Font(myLabel.Font.FontFamily, 8)
                myLabel2.Font = New Font(myLabel.Font, FontStyle.Bold)
                myLabel2.ForeColor = Color.Black

                myLabel2.Visible = False

                myLabel3.Left = 84
                myLabel3.Height = myForm.Height - 33
                myLabel3.Width = 42
                myLabel3.BackColor = myForm.BackColor

                myLabel3.TextAlign = ContentAlignment.MiddleCenter

                myLabel3.Font = New Font(myLabel.Font.FontFamily, 8)
                myLabel3.Font = New Font(myLabel.Font, FontStyle.Bold)
                myLabel3.ForeColor = Color.Black

                myLabel3.Visible = False

                myLabel.Parent = myForm
                myLabel.Top = 0
                myLabel.Left = 0
                myLabel.Height = myForm.Height - 33
                myLabel.Width = myForm.Width
                myLabel.BackColor = myForm.BackColor

                myLabel.TextAlign = ContentAlignment.MiddleCenter

                myLabel.Font = New Font(myLabel.Font.FontFamily, 8)
                myLabel.Font = New Font(myLabel.Font, FontStyle.Bold)
                myLabel.ForeColor = Color.Black

                myForm.Visible = True

                myForm.TopMost = True
                myForm.BringToFront()

                myLabel.Visible = True

                AddHandler myLabel.Click, AddressOf myKillerLabel_Click
                AddHandler myLabel1.Click, AddressOf myKillerLabel_Click
                AddHandler myLabel2.Click, AddressOf myKillerLabel_Click
                AddHandler myLabel3.Click, AddressOf myKillerLabel_Click

                AddHandler myForm.FormClosing, AddressOf myKillerForm_FormClosing

            End If

            'INCAInitStarted indicates that we are in the initialization phase.  Here, we will be displaying a single label
            'to show initialization status...
            If INCAInitStarted = True Then

                myLabel.BackColor = Color.MediumSpringGreen
                myLabel.Visible = True
                myLabel1.Visible = False
                myLabel2.Visible = False
                myLabel3.Visible = False

                INCAInitElapseTime = Now.Subtract(SaveINCAInitElapseTime)

                If INCAInitElapseTime.TotalMilliseconds > 0 And Not myForm Is Nothing Then
                    myLabel.Text = "Init - " & Format((INCAInitElapseTime.TotalMilliseconds - StartRecordDelay) / 60000, "0.0")
                End If

                If INCAInitElapseTime.TotalMilliseconds >= INCAInitDurationMinutes * 60000 Then
                    myLabel.BackColor = Color.Yellow
                End If

                If INCAInitElapseTime.TotalMilliseconds >= INCAInitDurationMinutesRed * 60000 Then
                    HealthCounter = 10
                    myLabel.BackColor = Color.Red
                End If

            Else 'After initialization, we will begin monitoring for Blocked, Device, and Stale Data status and display
                'this information in the three side by side labels at the top left of the screen.

                SaveINCAInitElapseTime = Now
                myForm.BackColor = Color.WhiteSmoke

                If EnableMyBackgroundTasks = True Then

                    myLabel.Visible = False

                    'The section below handles the audio alerts to the user if we have any invalid data alerts that are active.
                    'We handle this here in a thread separate from the main background loop so that the audio alerts persist
                    'even if the Alert message awaiting response from the user is being displayed.

                    If IgnoreLostDeviceForThisDrive = False Or IgnoreLostDeviceUntilNextRecordingSession = False Then

                        If BackgroundLoopCounterNotUpdating = True And LostDevice = False Then

                            myElapseTime = Now.Subtract(mysavetime)
                            If myElapseTime.Seconds >= AUDIO_MESSAGE_FREQUENCY Then
                                mysavetime = Now
                                synth.Speak("In Valid Data Alert")
                            End If

                        Else
                            mysavetime = Now
                        End If

                        If VideoCameraNotUpdating = True And LostDevice = False Then

                            myElapseTime2 = Now.Subtract(mysavetime2)
                            If myElapseTime2.Seconds >= AUDIO_MESSAGE_FREQUENCY Then
                                mysavetime2 = Now
                                synth.Speak("In Valid Video Alert")
                            End If

                        Else
                            mysavetime2 = Now
                        End If

                        If LostDevice = True Then

                            myElapseTime3 = Now.Subtract(mysavetime3)
                            If myElapseTime3.Seconds >= AUDIO_MESSAGE_FREQUENCY Then
                                mysavetime3 = Now
                                synth.Speak("Processor Communication Alert")
                            End If

                        Else
                            mysavetime3 = Now
                        End If

                    End If

                    'We count up HealthMonitor in GM_ResidentClient.myBackgroundTasks, we also make periodic calls to 
                    'INCA to determine INCA status.  So, if we hang up on a call to INCA in myBackgroundTasks, the HealthMonitor
                    'counter will Not count up.  Here, because ProcessKiller is running in a separate thread, we can handle 
                    'this situation.

                    If HealthMonitor = SaveHealthMonitor Then
                        HealthCounter = HealthCounter + 1
                        myLabel1.BackColor = Color.Yellow
                        myLabel1.Visible = True
                        myLabel1.Text = "B"
                        If HealthCounter >= 10 Then
                            myLabel1.BackColor = Color.Red
                            myLabel2.BackColor = Color.Gray
                            myLabel3.BackColor = Color.Gray
                            myLabel2.Visible = True
                            myLabel3.Visible = True
                        End If
                    Else ' If HealthMonitor <> SaveHealthMonitor, we know that myBackgroundTasks is running correctly, because
                        'it is updating the HealthMonitor counter...
                        SaveHealthMonitor = HealthMonitor
                        HealthCounter = 0

                        myLabel1.BackColor = Color.MediumSpringGreen

                        myLabel1.Visible = False

                        'myBackgroundTasks calls GetDeviceStatus which sets the myDeviceStatus flag.  If there is a comm
                        'issue with the proecessors, myDeviceStatus is set to false indicating a Device Comm error...
                        If myDeviceStatus = False Then

                            myLabel2.Text = "D"
                            myLabel2.BackColor = Color.Red

                            myLabel2.Visible = True

                            'If we have a processor status other than true, this indicates a processor communication fault
                            '(myDeviceStatus = False).  If this happens, we start a 25 second timer, if the status remains
                            'false for more than 25 seconds, we set the LostDevice global flag to true.  This flag is checked
                            'in the main background loop GM_ResidentClient.myBackgroundTasks, if true, a message is displayed
                            'to the user.  See CheckForLostDevice for more details...

                            myElapseTime4 = Now.Subtract(mysavetime4)
                            If myElapseTime4.Seconds > 25 Then

                                mysavetime4 = Now

                                If Debugger.IsAttached = False Then

                                    'Setting LostDevice to True will trigger cause cusmsgbox to be displayed to allow
                                    'user to decide whether or not they wish to re-initialize.  This is handled in CheckForLostDevice
                                    'called in MyBackgroundTasks()...

                                    If IgnoreLostDeviceForThisDrive = False Then

                                        LostDevice = True

                                    End If

                                Else
                                    If TriggerCommFailureInDebugMode = True Then
                                        IgnoreLostDeviceForThisDrive = False
                                        IgnoreLostDeviceUntilNextRecordingSession = False
                                        'Setting LostDevice to True will trigger cause cusmsgbox to be displayed to allow
                                        'user to decide whether or not they wish to re-initialize.  This is handled in CheckForLostDevice
                                        'called in MyBackgroundTasks()...
                                        LostDevice = True
                                    End If
                                End If

                            End If

                        Else
                            myLabel2.Visible = False
                            myLabel2.BackColor = Color.MediumSpringGreen
                            mysavetime4 = Now
                        End If

                        If myCPUStaleData = True And InMeasureMode = True Then

                            myLabel3.Text = "S"
                            myLabel3.BackColor = Color.Red
                            myLabel3.Visible = True

                        ElseIf myCPUStaleData = False And InMeasureMode = True Then
                            myLabel3.Visible = False
                            myLabel3.BackColor = Color.MediumSpringGreen
                        ElseIf InMeasureMode = False Then
                            myLabel3.Visible = False
                            myLabel3.BackColor = Color.Gray
                        End If

                    End If

                    If OnLoginScreen = False Then

                        If myLabel1.BackColor = Color.Red Or
                           myLabel2.BackColor = Color.Red Or
                           myLabel3.BackColor = Color.Red Then
                            myForm.Visible = True
                            myForm.TopMost = True
                            myForm.BringToFront()
                        Else
                            If myForm.Visible = True Then
                                myForm.Visible = False
                                myForm.TopMost = False
                                myForm.SendToBack()
                            End If

                        End If

                    End If

                    'If we are using a flash drive for data collection, we check to see if there are any files that require
                    'encryption, every 10 seconds.  This allows data collection to continue while completed data files are
                    'encrypted.  We will not however do this if the Exit button has been pressed on the main screen, because
                    'the final data encryption is handled in the exitapp routine.

                    If UsingFlashDrive = True And ExitPressed = False Then

                        EncryptElapseTime = Now.Subtract(SaveEncryptElapseTime)

                        If EncryptElapseTime.Seconds >= 10 Then
                            'If UsingFlashDrive = True Then
                            If System.IO.Directory.Exists(NetworkDriveLetter & "\CSAV2 Tools") = True Then
                                EncryptFilesInDirectory(BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber)
                            Else
                                UserStatusInfo.Label1.Text = "No Valid CLEVIR Flash Drive Found.  Files are no longer being encrypted..."
                                System.Threading.Thread.Sleep(5000)
                                UsingFlashDrive = False
                                NetworkDriveLetter = SaveNetworkDriveLetter
                                UserStatusInfo.Hide()
                            End If

                            'End If
                            SaveEncryptElapseTime = Now
                        End If
                    End If

                Else 'This is the initialization time between INCA returning from INITInca and when the myBackGroundTasks loop is started

                    If HealthMonitor = SaveHealthMonitor Then
                        myLabel.Text = "Busy"
                        HealthCounter = HealthCounter + 1
                        myLabel.BackColor = Color.Yellow
                        If HealthCounter >= 1000 Then
                            myLabel.BackColor = Color.Red
                        End If
                    Else
                        SaveHealthMonitor = HealthMonitor
                        HealthCounter = 0
                        myLabel.Text = "INCA OK"
                        myLabel.BackColor = Color.MediumSpringGreen
                    End If

                End If

                If OnLoginScreen = True Then
                    myForm.TopMost = False
                    myForm.SendToBack()
                Else
                    If myForm.TopMost = False Then
                        myForm.TopMost = True
                        myForm.BringToFront()
                    End If
                End If

            End If

            If myForm.TopMost = True Then
                myForm.BringToFront()
            End If

            System.Threading.Thread.Sleep(1000)

        Loop

        'If we jump out of this loop, due to KillProcess flag being set to true by the user, we kill all of the processes...

        If KillINCA = True Then

            CheckForExperiment = False

            IncaProc = Process.GetProcessesByName("Inca")

            If UBound(IncaProc) = 0 Then
                System.Threading.Thread.Sleep(1000)
                CopyToLog("ProcessKiller: Killing INCA...")
                IncaProc(0).Kill()
                CopyToLog("ProcessKiller: INCA Killed.")
            End If

            Try

                IPMServerProc = Process.GetProcessesByName("IPMServer")

                If UBound(IPMServerProc) = 0 Then
                    CopyToLog("ProcessKiller: Kill IPMServer...")
                    IPMServerProc(0).Kill()
                    CopyToLog("ProcessKiller: IPMServer killed.")
                End If

            Catch
                CopyToLog("ProcessKiller: IPMServer already shut down.")
            End Try

            Try
                tgtsvrProc = Process.GetProcessesByName("tgtsvr")

                If UBound(tgtsvrProc) = 0 Then
                    CopyToLog("ProcessKiller: Kill tgtsvr...")
                    tgtsvrProc(0).Kill()
                    CopyToLog("ProcessKiller: tgtsvr Killed.")
                End If

            Catch
                CopyToLog("ProcessKiller: tgtsvr already shut down.")
            End Try

        End If

        CopyToLog("ProcessKiller: Kill Stopping Thread...")
        StopTestProcess = True
        myTestThread = Nothing

        FileCopy(My.Application.Info.DirectoryPath & "\GM_INCA_Comm.log", BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & "\GM_INCA_Comm_" & Format(Now, "MMddyyyy_hhmmss") & ".log")

        End

        Exit Sub

    End Sub

    Private Sub myKillerForm_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs)

        e.Cancel = True

    End Sub

    Private Sub myKillerLabel_Click()

        'This event is fired if the user clicks on any of the status labels in the upper left corner of the screen.
        'If none of the labels are red, indicating a potential problem, clicking brings the main on vehicle
        'screen back to the top, otherwise, it does that as well as allows the user to kill the processes if they
        'choose to do so...

        SwitchToMain = True

        If (HealthCounter >= 10 Or myDeviceStatus = False Or (myCPUStaleData = True And InMeasureMode = True)) Then
            If MsgBox("Kill INCA Processes?", vbYesNo) = vbYes Then
                KillINCA = True
                KillProcesses = True
            Else
                'If running in debug mode in the design environment, we are typically not connected to hardware
                'so we would always get a processor comm fault, so processor comm fault is only triggered if
                'we are not in debug mode.  Here, is a way to trigger a processor comm fault in the design env.
                'For testing purposes...
                If Debugger.IsAttached = True Then
                    If MsgBox("Trigger Comm Failure in Debug Mode?", vbYesNo) = vbYes Then
                        TriggerCommFailureInDebugMode = True
                        OverrideCommFailureInDebugMode = False
                    Else
                        OverrideCommFailureInDebugMode = True
                    End If

                End If
            End If

        End If

    End Sub
    Private Sub InitializationMonitor()

        'This is the routine executed when the new myTestThread is started and is active while initializing, 
        'during signal registration.

        'When the GM_ResidentClient is loaded by InitForm on startup, the myTestThread process
        'is started and this routine is called.  It is responsible for displaying initialization
        'status messages and displays the Signal Registration progress bar during initialization.
        'When initialization is complete (the StopTestProcess flag is set to True), this routine 
        'is exited (this process is terminated).

        Dim x As Integer

        Dim myForm As Form
        Dim mylist As ListBox
        Dim myProgressBar As ProgressBar

        Dim myStatusString() As String = Nothing

        Dim gr As Graphics = Nothing

        Dim myFont As Font

        Dim myLabel As Label

        Dim SaveString As String
        Dim FinalStatus As Boolean

        Dim StartTime As DateTime

        Try
            StartTime = Now

            CopyToLog("InitializationMonitor: Starting Initialization...")
            CopyToLog(" ")

            SaveString = ""
            mylist = Nothing
            myProgressBar = Nothing
            myForm = Nothing
            FinalStatus = False

            myFont = SystemFonts.DefaultFont

            myFont = New Font(myFont.FontFamily, 12)
            myFont = New Font(myFont, FontStyle.Bold)

            Do While StopTestProcess = False

                If ProgressBarEnable = True Then

                    If myForm Is Nothing Then

                        If InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then

                            'Unnecessary 5.5.0

                            'If (UBound(myINCAInterface.mySignals) - UBound(myINCAInterface.GetAllActiveMeasureLabels())) > 0 Then
                            'ProgressBarMax = (UBound(myINCAInterface.mySignals)) + 1
                            'End If

                            'If ProgressBarMax < (UBound(myINCAInterface.mySignals) - UBound(myInvisibleSignals)) Then
                            'ProgressBarMax = (UBound(myINCAInterface.mySignals) - UBound(myInvisibleSignals)) + 2
                            'End If

                            ProgressBarMax = UBound(myINCAInterface.mySignals) + 1

                        End If

                        myForm = New Form
                        myForm.FormBorderStyle = Windows.Forms.FormBorderStyle.None
                        myForm.Height = 565 'was 600
                        myForm.Width = 800
                        myForm.StartPosition = FormStartPosition.Manual
                        myForm.Top = 0
                        myForm.Left = 0
                        myForm.BackColor = Color.WhiteSmoke
                        myForm.Visible = True

                        myForm.ShowInTaskbar = True

                        mylist = New ListBox
                        mylist.Parent = myForm
                        mylist.Visible = True
                        mylist.Height = 180 ' was 200
                        mylist.Top = 340 'was 355
                        mylist.Width = myForm.Width - 10
                        mylist.Left = 5

                        myLabel = New Label
                        myLabel.Parent = myForm
                        myLabel.Height = 73
                        myLabel.Width = 800
                        myLabel.BackColor = Me.BackColor
                        myLabel.Top = 274
                        myLabel.Left = 20

                        Select Case SignalRegistrationMode
                            Case "FULL"
                                myLabel.Text = "Performing FULL Signal Registration..."
                            Case "DISPLAYS"
                                myLabel.Text = "Registering DISPLAYS and GO/NOGO Signals..."
                            Case "GO/NOGO"
                                myLabel.Text = "Registering GO/NOGO Signals ONLY..."
                            Case "NEW FULL"
                                myLabel.Text = "Adding ALL Signals to NEW Blank Experiment (" & INCAExperiment & ")..."
                        End Select

                        myLabel.TextAlign = ContentAlignment.MiddleCenter

                        myLabel.Font = New Font(myLabel.Font.FontFamily, 14)
                        myLabel.Font = New Font(myLabel.Font, FontStyle.Bold)

                        myLabel.Visible = True

                        AddHandler mylist.Click, AddressOf mylist_Click

                        myProgressBar = New ProgressBar
                        myProgressBar.Parent = myForm
                        myProgressBar.Visible = True
                        myProgressBar.Maximum = ProgressBarMax
                        myProgressBar.Height = 25
                        myProgressBar.Top = (myForm.Height - myProgressBar.Height) - 10
                        myProgressBar.Width = myForm.Width - 10
                        myProgressBar.Left = 5

                        myProgressBar.Value = 0

                        gr = myProgressBar.CreateGraphics()

                        myForm.TopMost = True
                        myForm.Show()
                        myForm.BringToFront()

                        myForm.Activate()

                        mylist.Refresh()

                    Else 'myForm has already been set up...

                        myStatusString = StatusString

                        If IsNumeric(Mid(myStatusString(UBound(myStatusString)), 12, Len(myStatusString(UBound(myStatusString))))) Then

                            If myProgressBar.Value <> Val((Mid(myStatusString(UBound(myStatusString)), 12, Len(myStatusString(UBound(myStatusString)))))) Then
                                mylist.Items.Add("Processing " & Mid(myStatusString(UBound(myStatusString)), 12, Len(myStatusString(UBound(myStatusString)))) & " of " & myProgressBar.Maximum)
                                mylist.SelectedIndex = mylist.Items.Count - 1
                            End If

                            myProgressBar.Value = Val((Mid(myStatusString(UBound(myStatusString)), 12, Len(myStatusString(UBound(myStatusString))))))

                            gr.DrawString(Format(((myProgressBar.Value / myProgressBar.Maximum) * 100), "0") & "%", myFont, Brushes.Black, New PointF(myProgressBar.Width / 2 - (gr.MeasureString(Format(((myProgressBar.Value / myProgressBar.Maximum) * 100), "0") & "%", myFont).Width / 2.0F), myProgressBar.Height / 2 - (gr.MeasureString(Format(((myProgressBar.Value / myProgressBar.Maximum) * 100), "0") & "%", myFont).Height / 2.0F)))

                        Else
                            If SaveString <> myStatusString(UBound(myStatusString)) Then
                                mylist.Items.Add(myStatusString(UBound(myStatusString)))
                                mylist.SelectedIndex = mylist.Items.Count - 1

                                mylist.Refresh()

                                SaveString = myStatusString(UBound(myStatusString))

                                If InStr(SaveString, ">>> Inca <<<") > 0 Or InStr(SaveString, "RASTER FULL") Then
                                    KeepListBoxDisplayed = True
                                End If
                            End If

                        End If

                        System.Threading.Thread.Sleep(100)

                    End If

                Else 'ProgressBarEnable = False, we are finished registering signals...

                    If Not myForm Is Nothing And FinalStatus = False Then

                        FinalStatus = True
                        myProgressBar.Value = 0
                        mylist.Items.Clear()
                        mylist.Items.Add(Format(StartTime, "hh:mm:ss") & " - Initialization Start Time")

                        myStatusString = StatusString

                        For x = 0 To UBound(myStatusString)
                            If InStr(myStatusString(x), "Register Signals -") > 0 Then
                                mylist.Items.Add(myStatusString(x))
                                mylist.SelectedIndex = mylist.Items.Count - 1
                            End If
                        Next

                        StopTestProcess = True

                        mylist.Items.Add(Format(Now, "hh:mm:ss") & " - CLEVIR Init End Time")
                        CopyToLog("InitializationMonitor: CLEVIR Initialization Complete.")

                        If KeepListBoxDisplayed = True Then
                            mylist.Items.Add("")
                            mylist.Items.Add("")
                            mylist.Items.Add("THERE WERE SIGNAL REGISTRATION ERRORS!  SCROLL BAR to view, CLICK HERE to CONTINUE.")
                            mylist.Items.Add("The invalid signal list is also available in the InvalidSignalsLog.csv file.")
                            mylist.Items.Add("")
                            mylist.Items.Add("")
                        End If

                        mylist.SelectedIndex = mylist.Items.Count - 1

                        Exit Do

                    End If

                    System.Threading.Thread.Sleep(1500)

                End If

            Loop

            While KeepListBoxDisplayed = True
                System.Windows.Forms.Application.DoEvents()
            End While

            OnVehicleScreen.ShowInTaskbar = True

            Exit Sub

        Catch ex As Exception
            CopyToLog("InitializationMonitor: " & ex.Message)
            MsgBox("InitializationMonitor: " & ex.Message)
        End Try

    End Sub

    Private Sub GM_ResidentClient_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown

    End Sub

    Private Sub GM_ResidentClient_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        'This is called after the user clicks on "DRIVE" button on InitForm which is the first form displayed...

        Dim filename As String
        Dim fnum As Integer
        Dim textstr As String

        filename = My.Application.Info.DirectoryPath & "\OperatingMode.txt"

        If File.Exists(filename) Then

            fnum = FreeFile()
            FileOpen(fnum, filename, OpenMode.Input)

            textstr = UCase(LineInput(fnum))

            If InStr(textstr, "DEVELOPMENT") > 0 Then
                ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)"
            Else
                ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Development)"
            End If

            FileClose(fnum)

        Else
            ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)"
        End If

        'This thread will allow us to create a progress bar which displays the
        'status of the signal registration process during initialization...

        myTestThread = New Thread(AddressOf InitializationMonitor)
        myTestThread.Priority = ThreadPriority.Lowest
        myTestThread.Start()

        'This is the main initialization routine...
        Initialize()

    End Sub

    Public Sub ButtonContainers_Buttons_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)

        'ButtonContainers_Buttons are the annotation buttons on the main CLEVIR screen.  These buttons are created dynamically
        'at initialization based on the contents of the datadictionary.csv file...

        'Left button click writes event into INCA data, if we are recording, Right button click opens up the
        'AnnotationInterfaceConfiguration window...

        If e.Button = MouseButtons.Left Then
            HandleAnnotationButtons(sender)
        Else
            If sender.text <> "Custom Annotation" Then
                AnnotationInterfaceConfigure.Show()
                AnnotationInterfaceConfigure.BringToFront()
            End If
        End If

    End Sub

    Sub ButtonContainers_Buttons_Click(ByVal sender As Object, ByVal e As System.EventArgs)

        'No longer used, now use the ButtonContainers_Buttons_MouseDown event...

        HandleAnnotationButtons(sender)

    End Sub

    Sub HandleAutoAnnotation(ByVal AADescr As String, ByVal AutoAnnoText As String)

        'Not currently used...


        Dim y As Integer
        Dim i As Integer

        Dim synth As New SpeechSynthesizer

        Dim EventComment As String

        Dim Fnum As Integer

        Static SequenceNumber As Integer
        Dim Msec As Integer

        If Len(ANNOFileName) = 0 Then
            Exit Sub
        End If

        'Failsafe - The Annotation file should always have been created at this point, so this should never happen...
        If Not File.Exists(ANNOFileName) Then
            CreateANNOFile()
        End If

        Msec = RecordFileElapseTime.TotalMilliseconds - StartRecordDelay

        SequenceNumber = Val(Mid(SaveRecordingFileName, InStr(SaveRecordingFileName, ".mf4") - 2, 2))

        Fnum = FreeFile()
        FileOpen(Fnum, ANNOFileName, OpenMode.Append)

        EventComment = ""

        'we first need to figure out if text passed in is in the AnnotationValueRecord array

        For y = 0 To UBound(AnnotationValueRecord)
            If AADescr = AnnotationValueRecord(y).Description Then
                Exit For
            End If
        Next

        'We assume that there will be a match, otherwise this will not work.  There should
        'always be a match or this will not work properly...

        'We now set the proper enumeration type id and enumeration description, and
        'enumeration value, all of which are required to build the string that will be
        'written into the INCA data file.

        If y > UBound(AnnotationValueRecord) Then

            'that is bad

        Else

            '0	Anno Type ID	Anno Type	    Anno Value ID	Anno Value	Anno Enum Type	Anno Enum	Start Seq#	Start (ms)	End Seq#	End (ms)	Point Seq#	Point (ms)	Thumbnail	WAV
            '3	        1000	DRIVER FEEDBACK	         3170	 FCA Event	        3050	        1	         1	   723453	       1	  723453	         1	    723453	        0	  1


            EventComment = "2" & "," & AnnotationValueRecord(y).TypeID & "," & "System" & "," & AnnotationValueRecord(y).ID & "," & AnnotationValueRecord(y).Description & " Event" & " - " & AutoAnnoText & "," & AnnotationValueRecord(y).EnumerationType & "," & i & "," & CStr(SequenceNumber) & "," & CStr(Msec) & "," & CStr(SequenceNumber) & "," & CStr(Msec) & "," & CStr(SequenceNumber) & "," & CStr(Msec) & ",0," & CStr(SequenceNumber)

            PrintLine(Fnum, EventComment & "," & FinalPathToSaveData & "\" & SaveRecordingFileName & "," & Format(CurrentMileage, "0.0") & "," & Format(CurrentLatitude, "0.0000") & "," & Format(CurrentLongitude, "0.0000"))

            WriteToAggregateAnnoFile(EventComment & "," & FinalPathToSaveData & "\" & SaveRecordingFileName & "," & Format(CurrentMileage, "0.0") & "," & Format(CurrentLatitude, "0.0000") & "," & Format(CurrentLongitude, "0.0000"))

        End If

        FileClose(Fnum)

        If Len(EventComment) > 0 Then
            myINCAInterface.WriteEventComment(EventComment, False)
        End If

    End Sub

    Private Sub GM_ResidentClient_LocationChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LocationChanged

    End Sub

    Private Sub GM_ResidentClient_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LostFocus

    End Sub

    Private Sub GM_ResidentClient_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDown

    End Sub

    Private Sub GM_ResidentClient_MouseEnter(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.MouseEnter

        'If we are not resident on a pc that is connected to the vehicle and are not running on a suitcase logger connected
        'to a vehicle, we will display the OnVehicleScreen within the GM_ResidentClient window whenever the mouse enters
        'the GM_ResidentClient window. So, the OnVehicleScreen is superimposed on the GM_ResidentClient window.

        If Initializing = False Then

            If OperatingMode <> OperatingModes.RES_ON_VPC And
                OperatingMode <> OperatingModes.RES_ON_SUITCASE_VPC Then

                OnVehicleScreen.Top = Me.Top + 60
                OnVehicleScreen.Left = Me.Left
                OnVehicleScreen.Activate()
                OnVehicleScreen.BringToFront()

                LoginForm.Top = Me.Top + 60
                LoginForm.Left = Me.Left

                SelectDisplays.Top = Me.Top + 60
                SelectDisplays.Left = Me.Left

                If LoginForm.Visible = True Then
                    LoginForm.Activate()
                    LoginForm.BringToFront()
                End If

                If SelectDisplays.Visible = True Then
                    SelectDisplays.Activate()
                    SelectDisplays.BringToFront()
                End If

            End If

        End If
    End Sub

    Private Sub GM_ResidentClient_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp

    End Sub

    Private Sub GM_ResidentClient_Move(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Move

        'If we are not resident on a pc that is connected to the vehicle and are not running on a suitcase logger connected
        'to a vehicle, when we move the GM_ResidentClient window, the superimposed, OnVehicleScreen must move with it.

        If OperatingMode <> OperatingModes.RES_ON_VPC And
    OperatingMode <> OperatingModes.RES_ON_SUITCASE_VPC Then

            OnVehicleScreen.Top = Me.Top + 60
            OnVehicleScreen.Left = Me.Left

            LoginForm.Top = Me.Top + 60
            LoginForm.Left = Me.Left

            SelectDisplays.Top = Me.Top + 60
            SelectDisplays.Left = Me.Left

        End If

    End Sub

    Private Sub GM_ResidentClient_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint

        'If we are not resident on a pc that is connected to the vehicle and are not running on a suitcase logger connected
        'to a vehicle, when the GM_ResidentClient window is repainted, we make sure that the superimposed, OnVehicleScreen is
        'properly located within the boundaries of the GM_ResidentClient window.

        If OperatingMode <> OperatingModes.RES_ON_VPC And
OperatingMode <> OperatingModes.RES_ON_SUITCASE_VPC Then

            OnVehicleScreen.Top = Me.Top + 60
            OnVehicleScreen.Left = Me.Left

            LoginForm.Top = Me.Top + 60
            LoginForm.Left = Me.Left

            SelectDisplays.Top = Me.Top + 60
            SelectDisplays.Left = Me.Left

        End If
    End Sub

    Private Sub GM_ResidentClient_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize

    End Sub

    Private Sub GM_ResidentClient_ResizeEnd(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.ResizeEnd

    End Sub

    Private Sub RecordPlaybackToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Public Sub ViewOperationStatusToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'Display the OperationHistory window.  The OperationHistory screen is available on the GM_ResidentClient screen only and is accessed
        'from this drop down menu selection...

        OperationHistory.Show()
        OperationHistory.BringToFront()

    End Sub

    Private Sub DisplayRecordToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'This displays a message box which shows the current Record File Path and Filename.  This is
        'only accessed on the GM_ResidentClient screen from a drop down menu.  It is not directly
        'accessible from the OnVehicleScreen display.

        If Len(Label3.Text) > 0 Then
            MsgBox(Label3.Text & Label4.Text)
        Else
            MsgBox("No Record Path/Filename defined, please select a Login ID")
        End If

    End Sub

    Private Sub ChangeVehicleNumberToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ChangeVehicleNumberToolStripMenuItem.Click

        'Allows the user to change the vehicle number.  The initial vehicle number is set in the VehicleConfig.txt file.

        'the assumtion here is that there will be a unique vehicle number for each
        'computer on which the application will run, but we may need to change it if
        'we move the computer to a different vehicle...

        Dim tmpVehicleNumber As String

        tmpVehicleNumber = InputBox("Please enter the 8 character Vehicle ID", "INPUT VEHICLE NUMBER")

        If Len(tmpVehicleNumber) >= 8 Then
            VehicleNumber = tmpVehicleNumber

            Me.Text = "Vehicle " & VehicleNumber

        Else
            If Len(tmpVehicleNumber) > 0 Then
                MsgBox("Invalid Vehicle Number entered, please enter a valid vehicle ID (8 characters for USA, 9 characters for China).")
            End If
        End If

    End Sub

    Private Sub SetDisplayRefreshRateToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SetDisplayRefreshRateToolStripMenuItem.Click

    End Sub

    Private Sub ToolStripComboBox1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripComboBox1.Click

    End Sub

    Private Sub ToolStripComboBox2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripComboBox2.Click


    End Sub

    Private Sub SetDataCollectionRatemsecToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SetDataCollectionRatemsecToolStripMenuItem.Click

    End Sub

    Private Sub DisplayUpdateRatesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        MsgBox("Display Refresh Rate = " & DisplayRefreshRate & " Data Collection Rate = " & DataCollectionRate & " INCA Polling Rate = " & myINCAInterface.GetINCAPollingRate)
    End Sub

    Private Sub ToolStripComboBox3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripComboBox3.Click

    End Sub

    Private Sub ToolStripComboBox3_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ToolStripComboBox3.SelectedIndexChanged

        'Called from the Select / Add Custom Inca Setup menu option on the GM_ResidentClient Actions 
        'dropdown menu.  Handles adding a new custom INCA setup, or switching to an existing custom
        'INCA setup.

        Dim reply As Boolean
        Dim CustomINCASetupName As String
        Dim ErrorMsg As String
        Dim SaveExpName As String
        Dim SaveBackColor As Color

        If Initializing = False And Not myINCAInterface Is Nothing Then

            CheckForExperiment = False

            SaveExpName = INCAExperiment
            ErrorMsg = ""

            ChangeVehicleNumberToolStripMenuItem.Visible = False
            SetDisplayRefreshRateToolStripMenuItem.Visible = False
            SetDataCollectionRatemsecToolStripMenuItem.Visible = False
            SwitchINCAUserToolStripMenuItem.Visible = False
            RestartINCAToolStripMenuItem.Visible = False
            AddRecordOnlySignalsToolStripMenuItem.Visible = False
            EditUserConfigFileToolStripMenuItem.Visible = False


            If Len(ToolStripComboBox3.Text) > 0 And ToolStripComboBox3.Text <> "Add Custom INCA Setup" And
            ToolStripComboBox3.Text <> SaveSwitchToName Then

                If MsgBox("Switch Custom INCA Setup to " & ToolStripComboBox3.Text & "?", vbYesNo) = vbYes Then

                    reply = myINCAInterface.SwitchToUserNamed(ToolStripComboBox3.Text)

                    If reply = True Then

                        ToolStripComboBox3.Visible = False

                        GetExperimentNameForUser(ToolStripComboBox3.Text)

                        Me.TopMost = True

                        SaveBackColor = Label5.BackColor
                        Label5.BackColor = Color.Yellow
                        Label5.Text = "Operation In Progress, Please Wait...."

                        Me.Refresh()

                        Me.Cursor = Cursors.WaitCursor

                        WriteToListBox("Performing RCI2 Cleanup...")

                        myINCAInterface.RCI2_Cleanup()

                        WriteToListBox("Initializing INCA using ..." & INCAExperiment & " Experiment Name")

                        If myINCAInterface.InitINCA(INCADatabase, INCAWorkspace, INCAExperiment, True, ErrorMsg, False) <> IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then

                            WriteToListBox("Registering Signals with SignalRegistrationMode set to " & SignalRegistrationMode)

                            myINCAInterface.RegisterSignals()

                            Me.Activate()
                            Me.BringToFront()
                            Me.Focus()

                            Me.Cursor = Cursors.Arrow

                            Label5.BackColor = SaveBackColor
                            Label5.Text = ""

                            Me.Refresh()

                            MsgBox("Custom INCA Setup switched to " & ToolStripComboBox3.Text & " Active Experiment is now " & INCAExperiment)
                            SaveSwitchToName = ToolStripComboBox3.Text
                        Else
                            Me.TopMost = True
                            Me.Cursor = Cursors.Arrow
                            Me.Refresh()
                            MsgBox("INCA Re-Initialization failed - " & ErrorMsg)
                            INCAExperiment = SaveExpName
                        End If

                    Else
                        Me.TopMost = True
                        MsgBox("Custom INCA Setup switch to " & ToolStripComboBox3.Text & " FAILED - Please make sure INCA Station Option - User Selection, is set to yes.")
                    End If

                Else

                End If

                ListBox1.Visible = False

            ElseIf ToolStripComboBox3.Text = "Add Custom INCA Setup" Then

                CustomINCASetupName = InputBox("Add New Custom INCA Setup Name")

                If Len(CustomINCASetupName) > 0 Then
                    ToolStripComboBox3.Items.Add(CustomINCASetupName)

                    AddCustomINCASetup(CustomINCASetupName)

                    ToolStripComboBox3.SelectedItem = CustomINCASetupName

                End If

            End If
            If PlaybackMode = False Then
                CheckForExperiment = True
            End If

            ToolStripComboBox3.Visible = True

            ChangeVehicleNumberToolStripMenuItem.Visible = True
            SetDataCollectionRatemsecToolStripMenuItem.Visible = True
            SwitchINCAUserToolStripMenuItem.Visible = True
            RestartINCAToolStripMenuItem.Visible = True

            AddRecordOnlySignalsToolStripMenuItem.Visible = True
            SetDisplayRefreshRateToolStripMenuItem.Visible = True
            EditUserConfigFileToolStripMenuItem.Visible = True

            Label5.BackColor = SaveBackColor
            Label5.Text = ""

        End If

    End Sub

    Private Sub ToolStripComboBox3_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ToolStripComboBox3.TextChanged

    End Sub

    Private Sub ToolStripComboBox1_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ToolStripComboBox1.TextChanged
        DisplayRefreshRate = Val(ToolStripComboBox1.Text)
    End Sub

    Private Sub ToolStripComboBox2_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ToolStripComboBox2.SelectedIndexChanged

        If Initializing = False And Not myINCAInterface Is Nothing Then

            DataCollectionRate = Val(ToolStripComboBox2.Text)

            If myINCAInterface.MeasurementStarted = True Then
                myINCAInterface.StartDataCollection(DataCollectionRate)
            End If

        End If

    End Sub

    Private Sub ToolStripComboBox2_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ToolStripComboBox2.TextChanged

    End Sub

    Public Sub SwitchINCAUserToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SwitchINCAUserToolStripMenuItem.Click

    End Sub

    Private Sub AddRecordOnlySignalsToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AddRecordOnlySignalsToolStripMenuItem.Click

        'This functionality is currently disabled...

        AddRecordOnlySignals.ShowDialog()
    End Sub

    Private Sub RestartINCAToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RestartINCAToolStripMenuItem.Click

        If MsgBox("Are you sure you want to restart INCA?", vbYesNo) = vbYes Then
            ShutdownAndRestartINCA(18000)
        End If

    End Sub

    Private Sub HdwrStatusToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        DeviceStatus.Show()
        DeviceStatus.BringToFront()
    End Sub

    Private Sub UploadDataToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        UploadDataScreen.UploadData()

    End Sub

    Public Sub ShowCustomScreen(ByVal ScreenName As String)

        'Called from various places when a custom screen is to be displayed.  Displays, positions and sizes the proper form
        'based on the custom screen selection.

        Select Case ScreenName
            Case "LKA Screen"
                LKAForm.Top = 45
                LKAForm.Show()
                LKAForm.BringToFront()

                If Not myTDGraphicsContainer Is Nothing Then
                    myTDGraphicsContainer.Show()
                    myTDGraphicsContainer.Left = (LKAForm.Left + LKAForm.Width) - myTDGraphicsContainer.Width - 10
                    myTDGraphicsContainer.Top = (LKAForm.Top + LKAForm.Height) - myTDGraphicsContainer.Height - 40
                    myTDGraphicsContainer.BringToFront()
                End If

            Case "Secret Squirrel Screen"
                TargetStatusDisplay.Top = 45
                TargetStatusDisplay.Show()
                TargetStatusDisplay.BringToFront()

                If Not myTDGraphicsContainer Is Nothing Then
                    myTDGraphicsContainer.Show()
                    myTDGraphicsContainer.Left = (TargetStatusDisplay.Left + TargetStatusDisplay.Width) - myTDGraphicsContainer.Width - 10
                    myTDGraphicsContainer.Top = (TargetStatusDisplay.Top + TargetStatusDisplay.Height) - myTDGraphicsContainer.Height - 40
                    myTDGraphicsContainer.BringToFront()
                End If
            Case "Pedestrian Status Display"
                PedestrianStatusDisplay.Top = 45
                PedestrianStatusDisplay.Show()
                PedestrianStatusDisplay.BringToFront()

                If Not myTDGraphicsContainer Is Nothing Then
                    myTDGraphicsContainer.Show()
                    myTDGraphicsContainer.Left = (PedestrianStatusDisplay.Left + PedestrianStatusDisplay.Width) - myTDGraphicsContainer.Width - 10
                    myTDGraphicsContainer.Top = (PedestrianStatusDisplay.Top + PedestrianStatusDisplay.Height) - myTDGraphicsContainer.Height - 40
                    myTDGraphicsContainer.BringToFront()
                End If
            Case "Fusion Status Display"
                FusionStatusDisplay.Top = 45
                FusionStatusDisplay.Show()
                FusionStatusDisplay.BringToFront()
            Case "CoPilot Status Display"
                CopilotStatusDisplay.Top = 45
                CopilotStatusDisplay.Show()
                CopilotStatusDisplay.BringToFront()

            Case "LMFR Global A Status Display"
                LMFR_Status_Display_Global_A.Top = 45
                LMFR_Status_Display_Global_A.Show()
                LMFR_Status_Display_Global_A.BringToFront()

            Case "LMFR High Content Status Display"
                LMFR_Status_Screen_HC.Top = 45
                LMFR_Status_Screen_HC.Show()
                LMFR_Status_Screen_HC.BringToFront()

            Case "INCA Hardware Status"
                DeviceStatus.Top = 45
                DeviceStatus.Show()
                DeviceStatus.BringToFront()
            Case "Top Down View"
                If myTDGraphicsContainer.Visible = False Then
                    myTDGraphicsContainer.Left = Me.Left
                    myTDGraphicsContainer.Top = Me.Top
                End If
                myTDGraphicsContainer.ControlBox = True
                myTDGraphicsContainer.Show()
                myTDGraphicsContainer.BringToFront()
            Case "Create New Display"
                Dim NewDisplayName As String
                NewDisplayName = InputBox("Enter Display Name", "Display Configuration")
            Case Else
                MsgBox("Invalid Screen Name")
        End Select

    End Sub

    Private Sub TestToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        ShowCustomScreen("Secret Squirrel Screen")

    End Sub

    Private Sub EditUserConfigFileToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles EditUserConfigFileToolStripMenuItem.Click
        DefaultConfiguration.ShowDialog()
    End Sub

    Private Sub GM_ResidentClient_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown

        If OperatingMode <> OperatingModes.RES_ON_VPC And _
    OperatingMode <> OperatingModes.RES_ON_SUITCASE_VPC Then

            OnVehicleScreen.Top = Me.Top + 60
            OnVehicleScreen.Left = Me.Left
            OnVehicleScreen.Activate()
            OnVehicleScreen.BringToFront()

            LoginForm.Top = Me.Top + 60
            LoginForm.Left = Me.Left

            SelectDisplays.Top = Me.Top + 60
            SelectDisplays.Left = Me.Left

        End If

    End Sub

    Private Sub GM_ResidentClient_SizeChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.SizeChanged

    End Sub

    Private Sub MenuStrip1_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MenuStrip1.Click

        If myTDGraphicsContainer.TopMost = True Then
            myTDGraphicsContainer.TopMost = False
            myTDGraphicsContainer.Hide()
        End If
    End Sub

    Private Sub MenuStrip1_ItemClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ToolStripItemClickedEventArgs) Handles MenuStrip1.ItemClicked
        If myTDGraphicsContainer.TopMost = True Then
            myTDGraphicsContainer.TopMost = False
            myTDGraphicsContainer.Hide()
        End If
    End Sub

    Public Sub TESTToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TESTToolStripMenuItem.Click

    End Sub

    Private Sub ListBox1_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs)

        If Len(ListBox1.SelectedItem) > 0 Then
            INCAExperiment = ListBox1.SelectedItem
        End If

        ListBox1.Visible = False

    End Sub

    Private Sub Label4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label4.Click

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Me.Close()
    End Sub

    Private Sub ListBox1_SelectedIndexChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ToggleOperatingModeToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToggleOperatingModeToolStripMenuItem.Click

        'Allows the user to toggle the operating mode from DEVELOPMENT to DATALOGGING.  This change will take effect the next time CLEVIR is started.

        Dim filename As String
        Dim fnum As Integer

        Dim textstr As String

        filename = My.Application.Info.DirectoryPath & "\OperatingMode.txt"

        If ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)" Then
            ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Development)"

            If MsgBox("With USER DATA UPLOAD on Exit Enabled?", vbYesNo) = MsgBoxResult.Yes Then
                textstr = "DATALOGGINGWITHUPLOAD"
                UploadDataOnExit = True
            Else
                textstr = "DATALOGGING"
                UploadDataOnExit = False
            End If

            fnum = FreeFile()
            FileOpen(fnum, filename, OpenMode.Output)
            PrintLine(fnum, textstr)
            FileClose(fnum)

            CLEVIR_Flavor = textstr

        Else

            fnum = FreeFile()
            ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)"
            FileOpen(fnum, filename, OpenMode.Output)
            PrintLine(fnum, "DEVELOPMENT")
            FileClose(fnum)

            CLEVIR_Flavor = "DEVELOPMENT"

        End If

    End Sub

    Private Sub GM_ResidentClient_HandleCreated(sender As Object, e As EventArgs) Handles Me.HandleCreated

    End Sub

    Private Sub GM_ResidentClient_MarginChanged(sender As Object, e As EventArgs) Handles Me.MarginChanged

    End Sub
End Class
