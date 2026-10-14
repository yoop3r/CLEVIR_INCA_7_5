Option Strict Off

Imports System.Diagnostics
'Imports VB = Microsoft.VisualBasic
Imports System.Security.Principal
'Imports System.Threading
Imports System.Threading.Tasks
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System

'The InitForm is the startup form for the application.  The code in the InitForm_Load routine is the
'first code executed when the application starts. The InitForm is the first form displayed when CLEVIR
'is launched.  

'See InitForm_Load, first code executed When CLEVIR starts up...

Public Class InitForm

    ' ✅ DELETED: myThread and HowLongHaveIBeenUp (obsolete timer - see commit notes)
    Private myform As Form

    Public VehicleNumbersList As List(Of String)

    ' Module-level variables to track original state
    Private _originalConfigData As String = ""

    <DllImport("user32.dll")>
    Private Shared Function ShowWindow(hWnd As IntPtr, nCmdShow As Integer) As Boolean
    End Function

    Private Const SW_MINIMIZE As Integer = 6

    Private Sub MinimizeInca()
        Dim processes() As Process = Nothing
        Try
            processes = Process.GetProcessesByName("INCA") ' replace with actual process name

            ' GetProcessesByName can match more than one process; check each until one with a
            ' usable window handle is found instead of assuming the first entry is the right one.
            For Each proc As Process In processes
                Dim hwnd = proc.MainWindowHandle
                If Not hwnd.Equals(IntPtr.Zero) Then
                    ShowWindow(hwnd, SW_MINIMIZE)
                    Exit For
                End If
            Next
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Failed to minimize INCA: {ex.Message}")
        Finally
            ' Process instances returned by GetProcessesByName wrap native OS handles and should
            ' be disposed explicitly rather than left for the finalizer.
            If processes IsNot Nothing Then
                For Each proc As Process In processes
                    proc.Dispose()
                Next
            End If
        End Try
    End Sub

    'Private Function CurrentDomain_AssemblyResolve(ByVal sender As Object, ByVal args As ResolveEventArgs) As System.Reflection.Assembly
    'Return EmbeddedAssembly.[Get](args.Name)
    'End Function

    Private Function Process_CLEVIR_INI_File() As Boolean
        ' The CLEVIR.ini file is used to differentiate between running as GM use case or PATAC use case.
        ' If the file is not in the CLEVIR install directory, we default to running as PATAC.
        ' If the file exists and the first line contains both "G" and "M", we run as GM use case.
        Dim myPATAC As Boolean = True ' Default to PATAC
        Dim iniFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "CLEVIR.ini")
        Try
            ' Check if the CLEVIR.ini file exists
            If File.Exists(iniFilePath) Then
                ' Use StreamReader to read the first line of the file
                Using reader As New StreamReader(iniFilePath)
                    Dim textline As String = reader.ReadLine()
                    If Not String.IsNullOrEmpty(textline) AndAlso textline.Contains("G") AndAlso textline.Contains("M") Then
                        myPATAC = False ' Set to GM use case
                    End If
                End Using
            End If
            ' Allow administrator to override the setting
            If ClevirAdministrator Then
                Dim newPATACValue As Boolean = Not myPATAC
                If MsgBox($"PATAC Version = {myPATAC}. Change to {newPATACValue}?", vbYesNo) = vbYes Then
                    myPATAC = newPATACValue
                End If
            End If
            ' Update the application title if running as PATAC
            If myPATAC Then
                If InvokeRequired Then
                    Invoke(Sub() Text = Mid(Text, 1, Len(Text) - 1) & " PATAC)")
                Else
                    Text = Mid(Text, 1, Len(Text) - 1) & " PATAC)"
                End If
            End If
            ' Return the determined value
            Return myPATAC
        Catch ex As Exception
            ' Log the exception and return False as a fallback
            HandleUserMessageLogging("GMRC", $"Process_CLEVIR_INI_File: {ex.Message}", DisplayMsgBox)
            Return False
        End Try
    End Function


    Public Sub PopulateVehicleNumbersList()
        ' Populate the in-memory vehicle numbers list from the configuration file.
        VehicleNumbersList = New List(Of String)

        ' ── XML-first path ──────────────────────────────────────────────────
        Dim configPath As String = Path.Combine(My.Application.Info.DirectoryPath, "config.xml")
        Dim loadedFromXml As Boolean = False
        If File.Exists(configPath) Then
            Try
                Dim xmlDoc As New System.Xml.XmlDocument()
                xmlDoc.Load(configPath)
                Dim xmlVehicles As List(Of VehicleConfig) = ReadVehiclesFromXml(xmlDoc)
                If xmlVehicles IsNot Nothing Then
                    For Each v As VehicleConfig In xmlVehicles
                        If Not String.IsNullOrWhiteSpace(v.VehicleNumber) Then
                            Try
                                Dim processors As ProcessorIdentity() = ProcessorIdentity.ParseList(v.Processors, v.VehicleNumber)
                                AddVehicleToList(v.VehicleNumber, processors(0).VariantName)
                            Catch ex As FormatException
                                HandleUserMessageLogging("GMRC", ex.Message)
                                AddVehicleToList(v.VehicleNumber, "Invalid")
                            End Try
                        End If
                    Next
                    loadedFromXml = True
                    HandleUserMessageLogging("GMRC",
                        $"PopulateVehicleNumbersList: loaded {xmlVehicles.Count} vehicle(s) from config.xml")
                End If
            Catch ex As Exception
                HandleUserMessageLogging("GMRC",
                    "PopulateVehicleNumbersList: XML read failed: " & ex.Message)
            End Try
        End If

        If Not loadedFromXml Then
            HandleUserMessageLogging("GMRC", "PopulateVehicleNumbersList: no <Vehicles> section found in config.xml.")
        End If

        VehicleNumbersList.Sort()
    End Sub


    Private Sub AddVehicleToList(ByVal vehicleNumber As String, ByVal projectName As String)
        Dim added As Boolean = False

        ' Handle case where PTP lookup info doesn't exist
        If VehiclePTPLookupInfo Is Nothing Then
            VehicleNumbersList.Add(vehicleNumber & "," & projectName)
            Return
        End If

        ' Try to find a match in the PTP lookup data
        For y As Integer = 0 To VehiclePTPLookupInfo.Count - 1
            Dim lookupArray() As String = Split(VehiclePTPLookupInfo(y).ToString, ",")

            If lookupArray(0) = vehicleNumber Then
                VehicleNumbersList.Add(vehicleNumber & "," & projectName & "," & lookupArray(1))
                Return
            End If
        Next

        ' If no match found in PTP lookup, add with just project name
        VehicleNumbersList.Add(vehicleNumber & "," & projectName & ",")
    End Sub


    Private Function GetVehicleNumber() As String
        ' Called from InitForm_Load and when Save Vehicle Number Change button is pressed.
        ' Reads the selected vehicle number from <SelectedVehicleNumber> in config.xml.
        ' Falls back to "UNDEFINED" when the element is absent (first run, fresh install).
        If String.IsNullOrEmpty(VehicleNumber) Then
            VehicleNumber = ReadSelectedVehicleNumber()
        End If
        If UCase(VehicleNumber) = "6LDN4666" AndAlso Not ClevirAdministrator Then
            HandleUserMessageLogging("GMRC", "GetVehicleNumber: Vehicle number 6LDN4666 is not a valid number. You must select a valid vehicle number to continue.", DisplayMsgBox, )
            Button1.Enabled = False
        ElseIf String.IsNullOrEmpty(VehicleNumber) OrElse
               String.Equals(VehicleNumber, "UNDEFINED", StringComparison.OrdinalIgnoreCase) OrElse
               ActiveProcessorNames Is Nothing OrElse ActiveProcessorNames.Length = 0 Then
            ' No valid vehicle selected yet.
            Button1.Enabled = False
        Else
            Button1.Enabled = True
        End If
        Return VehicleNumber
    End Function

    Private Sub CreateSubDirectories()

        'Called from InitForm_Load:  Creates sub-directories in the CLEVIR install directory if they do not already exist...

        'CreateSubDirectories creates the subdirectories in GmResidentClient install directory for signal lists, experiments, LABfiles, etc.
        '(If necessary the first time CLEVIR is executed, but will only be done once), as these are not created when the software is installed...)

        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\INCAProjects") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\INCAProjects")
            HandleUserMessageLogging("GMRC", "CreateSubDirectories: " & My.Application.Info.DirectoryPath & "\INCAProjects created")
        End If

        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\A2L") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\A2L")
            HandleUserMessageLogging("GMRC", "CreateSubDirectories: " & My.Application.Info.DirectoryPath & "\A2L created")
        End If

        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\SignalLists") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\SignalLists")
            HandleUserMessageLogging("GMRC", "CreateSubDirectories: " & My.Application.Info.DirectoryPath & "\SignalLists created")
        End If

        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\PTP") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\PTP")
            HandleUserMessageLogging("GMRC", "CreateSubDirectories: " & My.Application.Info.DirectoryPath & "\PTP created")
        End If

        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\Experiments") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\Experiments")
            HandleUserMessageLogging("GMRC", "CreateSubDirectories: " & My.Application.Info.DirectoryPath & "\Experiments created")
        End If

        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\Workspaces") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\Workspaces")
            HandleUserMessageLogging("GMRC", "CreateSubDirectories: " & My.Application.Info.DirectoryPath & "\Workspaces created")
        End If

        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\LABFiles") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\LABFiles")
            HandleUserMessageLogging("GMRC", "CreateSubDirectories: " & My.Application.Info.DirectoryPath & "\LabFiles created")
        End If

        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\CANalyzer") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\CANalyzer")
            HandleUserMessageLogging("GMRC", "CreateSubDirectories: " & My.Application.Info.DirectoryPath & "\CANalyzer created")
        End If

        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\VehicleSpy") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\VehicleSpy")
            HandleUserMessageLogging("GMRC", "CreateSubDirectories: " & My.Application.Info.DirectoryPath & "\VehicleSpy created")
        End If

    End Sub

    Public Sub Drive()
        Try
            If Not RunInitializationStage("INCA configuration", AddressOf ValidateIncaStartup) Then
                Button3_Click(Me, EventArgs.Empty)
                Return
            End If
            MinimizeInca()
            GetVehicleNumber()

            ' Continue with normal Drive flow...
            Me.ShowInTaskbar = False
            Me.WindowState = FormWindowState.Minimized
            Me.Hide()

            GmResidentClient.Show()

            If OperatingMode = OperatingModes.ResOnVpc Then
                GmResidentClient.Label2.Text = "Please Wait..."
                GmResidentClient.WindowState = FormWindowState.Minimized
                GmResidentClient.ShowInTaskbar = True
            End If

        Catch ex As Exception
            ReportInitializationFailure("InitForm.Drive: " & ex.Message)
            Button3_Click(Me, EventArgs.Empty)
        End Try
    End Sub

    Private Sub ButtonConfigEditor_Click()
        Try
            HandleUserMessageLogging("GMRC", "Configuration Editor button pressed...")

            Dim editor As New ConfigurationEditorForm()
            If editor.ShowDialog(Me) = DialogResult.OK Then
                ' User saved changes - offer to reload
                If MsgBox("Configuration updated successfully. Reload settings now?", vbYesNo + vbQuestion) = vbYes Then
                    If ReadConfigFile() Then
                        GetVehicleNumber()
                        HandleUserMessageLogging("GMRC", "Configuration reloaded after editing")
                    End If
                End If
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ConfigEditor launch failed: {ex.Message}", DisplayMsgBox)
        End Try
    End Sub

    Public Sub SaveVehicleNumber(ByVal l_VehicleNumber As String)
        ' Saves the selected vehicle number to <SelectedVehicleNumber> in config.xml.
        ' Called from the Save and continue button on InitForm.
        If l_VehicleNumber <> " VEHICLE ID NOT IN LIST" Then
            HandleUserMessageLogging("GMRC", "SaveVehicleNumber: Vehicle Number changed from " & VehicleNumber & " to " & l_VehicleNumber)
            If Not SaveSelectedVehicleNumber(l_VehicleNumber) Then Return
        End If

        VehicleNumber = l_VehicleNumber

        'Here we re-call ReadVehicleConfigsFile which sets internal parameters based on newly saved vehicle number...
        If ReadVehicleConfigsFile() = False Then
            Close()
            End
        End If

        GmResidentClient.Text = "Vehicle " & VehicleNumber
        GmResidentClient.Text = GmResidentClient.Text & " - ON VEHICLE MODE"

    End Sub


    Private Function CheckForCLEVIRRunning() As Boolean

        'Called from InitForm_Load...

        'Here we check to see if there is already an instance of CLEVIR running.  If so, we exit immediately...

        'Added for PATAC, but commented out for now...

        'Dim ExpirationDate As New DateTime(2021, 11, 15, 0, 0, 0)
        'Dim ExpirationDate As New DateTime(2021, 3, 29, 0, 0, 0)

        'If DateTime.Compare(DateTime.Now, ExpirationDate) > 0 Then
        'MsgBox("The CLEVIR trial period has expired.  CLEVIR cannot be used after " & ExpirationDate & " Exiting...")
        'End
        'End If

        'If running from the design environment, we must bypass this...
        If Not Debugger.IsAttached Then

            Dim current As Process = Process.GetCurrentProcess()
            Dim processes As Process() = Process.GetProcesses
            Dim ThisProcess As Process

            For Each ThisProcess In processes
                '-- Ignore the current process 
                If ThisProcess.Id <> current.Id Then
                    '-- Only list processes that have a Main Window Title 
                    If InStr(UCase(ThisProcess.ProcessName), "CLEVIR") > 0 Then
                        CheckForCLEVIRRunning = True
                        Exit Function
                    End If
                End If
            Next

        End If

        CheckForCLEVIRRunning = False

    End Function

    Private Sub CheckCLEVIRFlavor()
        ' Called from InitForm_Load...
        ' Determines the CLEVIR Flavor by reading the OperatingMode.txt file.
        Dim textstr As String
        Dim userMessage As String
        Dim operatingModePath As String = Path.Combine(My.Application.Info.DirectoryPath, "OperatingMode.txt")
        HandleUserMessageLogging("GMRC", "") ' Add a blank line to separate log messages.
        ' Check if OperatingMode.txt exists
        If File.Exists(operatingModePath) Then
            ' Read the content of the file using StreamReader
            Using reader As New StreamReader(operatingModePath)
                textstr = UCase(reader.ReadLine())
            End Using
            ' Determine the CLEVIR Flavor based on the file content
            If textstr.Contains("DATALOGGING") Then
                If MsgBox("CLEVIR is currently set up to run in DATALOGGING mode. In this mode, the init screen and login screen will be bypassed, and CLEVIR will immediately initialize using the current configuration. Continue in DATALOGGING mode?", vbYesNo) = vbNo Then
                    CLEVIRFlavor = "DEVELOPMENT"
                    userMessage = $"{hostname} Initializing {My.Application.Info.AssemblyName}{Mid(Text, InStr(Text, " "), InStr(Text, ")"))} for Development."
                    If MsgBox("DATALOGGING Operating mode is now OFF. Do you wish to save this change for subsequent CLEVIR sessions?", vbYesNo) = vbYes Then
                        ' Save the updated flavor to the file
                        Using writer As New StreamWriter(operatingModePath, False)
                            writer.WriteLine(CLEVIRFlavor)
                        End Using
                    End If
                Else
                    CLEVIRFlavor = textstr
                    userMessage = $"{hostname} Initializing {My.Application.Info.AssemblyName}{Mid(Text, InStr(Text, " "), InStr(Text, ")"))} for Data Logging."
                    DebugMode = True
                    If textstr.Contains("WITHUPLOAD") Then
                        UploadDataOnExit = True
                    End If
                End If
            Else
                CLEVIRFlavor = "DEVELOPMENT"
                userMessage = $"{hostname} Initializing {My.Application.Info.AssemblyName}{Mid(Text, InStr(Text, " "), InStr(Text, ")"))} for Development."
            End If
        Else
            ' Default state if OperatingMode.txt does not exist
            CLEVIRFlavor = "DEVELOPMENT"
            userMessage = $"{hostname} Initializing {My.Application.Info.AssemblyName}{Mid(Text, InStr(Text, " "), InStr(Text, ")"))} for Development."
        End If
        ' Log the user message
        If Not PATAC Then
            HandleUserMessageLogging("GMRC", userMessage,,, FlashMsg1Sec)
        Else
            HandleUserMessageLogging("GMRC", userMessage)
        End If
        HandleUserMessageLogging("GMRC", "") ' Add a blank line to separate log messages.
    End Sub

    Private Sub Myform_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs)
        myform = Nothing
    End Sub

    Private Sub myReqAssistanceButton_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'This is the button click event for the dynaically created form which can be used by the user to request assistance...

        Dim inputtext As String
        Dim invalidentry As Boolean
        Dim fnum As Integer

        LiveSupportAvailability = CheckAvailability(NetworkDriveMapping & CLEVIRBaseDir & "\Development\PC_HostNameLogFiles")

        If LiveSupportAvailability = True Then

            inputtext = InputBox("Please enter six character GM ID")

            If Len(inputtext) = 6 Then
                If Mid(UCase(inputtext), 2, 1) = "Z" Then
                    fnum = FreeFile()
                    FileOpen(fnum, NetworkDriveMapping & CLEVIRBaseDir & "\Development\PC_HostNameLogFiles\" & hostname & "\RequestID.txt", OpenMode.Output)
                    PrintLine(fnum, inputtext)
                    FileClose(fnum)
                Else
                    invalidentry = True
                End If

            Else
                invalidentry = True
            End If

            If invalidentry = True Then
                MsgBox("Invalid EDSNET ID Entered")
            End If

        Else
            MsgBox("Sorry, Live Assistance is now offline.")
            myform.Close()
        End If

    End Sub

    ' ✅ DELETED: myTimer() method - obsolete timer thread (see cleanup patch notes)

    Private Sub InitForm_Activated(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Activated

    End Sub


    Private Sub InitForm_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Click

        'Revisions...

        If PATAC = True Then
            Exit Sub
        End If

        MsgBox("Changes to 7.5.1")

    End Sub

    Private Sub InitForm_FormClosed(ByVal sender As Object, ByVal e As FormClosedEventArgs) Handles Me.FormClosed

    End Sub

    Private Sub InitForm_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles Me.FormClosing
        ' ✅ SIMPLIFIED: myThread cleanup removed (obsolete timer)
        ' CLEVIRAvailability tracking also removed (obsolete)
    End Sub

    Private Sub InitForm_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles Me.KeyDown

    End Sub

    Private Sub InitForm_Leave(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Leave

    End Sub

    Private Sub InitForm_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Call async initialization without awaiting (fire-and-forget with error handling)
        ' Use UI synchronization context to handle exceptions properly
        Dim uiContext = Threading.SynchronizationContext.Current

        InitializeAsync().ContinueWith(
            Sub(task)
                If task.IsFaulted Then
                    ' Handle exceptions on UI thread using synchronization context
                    Dim action As Action = Sub()
                                               HandleUserMessageLogging("GMRC", $"InitForm Load Error: {task.Exception?.GetBaseException()?.Message}", DisplayMsgBox)
                                           End Sub

                    If uiContext IsNot Nothing Then
                        uiContext.Post(Sub(state) action(), Nothing)
                    Else
                        ' Fallback to Invoke if no context available
                        If InvokeRequired Then
                            Invoke(action)
                        Else
                            action()
                        End If
                    End If
                End If
            End Sub
            )
    End Sub

    ''' <summary>
    ''' ✅ NEW: Reusable method to run async operations with splash progress feedback
    ''' </summary>
    Private Async Function RunWithProgressAsync(
                                                operations As Func(Of InitProgressSplash, Task),
                                                allowCancel As Boolean,
                                                anchor As InitProgressSplash.SplashAnchor
                                                ) As Task

        Dim splash As InitProgressSplash = Nothing
        Dim cts As Threading.CancellationTokenSource = Nothing

        Try
            ' Create and configure splash
            splash = New InitProgressSplash(showCancel:=allowCancel)

            If allowCancel Then
                cts = New Threading.CancellationTokenSource()
                AddHandler splash.CancelRequested, Sub()
                                                       Try
                                                           cts?.Cancel()
                                                       Catch exCancel As Exception
                                                           HandleUserMessageLogging("GMRC", $"InitProgressSplash CancelRequested: {exCancel.Message}")
                                                       End Try
                                                   End Sub
            End If

            splash.PositionOnActiveScreen(anchor, margin:=16)
            splash.Show()
            Application.DoEvents()

            ' Run the operations (passing splash for status updates)
            Await operations(splash)

            ' Success - close splash
            splash.SetStatus("Complete.")
            Await Task.Delay(300)
            splash.Close()

        Finally
            cts?.Dispose()
            If splash IsNot Nothing AndAlso Not splash.IsDisposed Then
                Try : splash.Close() : Catch : End Try
            End If
        End Try
    End Function

    Private Async Function InitializeAsync() As Task
        Try
            ' ✅ CRITICAL: Set DLL search path FIRST before any other operations
            ' This ensures HesaiWrapper.dll dependencies can be found
            HesaiInterop.SetDllSearchPath()

            ' Initialize UI state early
            Width = 310
            StartPosition = FormStartPosition.CenterScreen
            Text = My.Application.Info.AssemblyName & " " & Text
            Opacity = 0
            ShowInTaskbar = False

            ' ✅ Use reusable progress wrapper
            Await RunWithProgressAsync(
            Async Function(splash As InitProgressSplash)
                splash.SetStatus("Checking for another running instance...")
                If Not Debugger.IsAttached AndAlso IsAnotherClevirInstanceRunning() Then
                    splash.SetStatus("Another instance detected. Exiting...")
                    Await Task.Delay(600)
                    Close()
                    End
                End If

                ' Initialize core system information
                hostname = Environment.MachineName
                Using identity = WindowsIdentity.GetCurrent()
                    GmResidentClient.EtasDefaultUserName = identity.Name.Split("\"c).LastOrDefault()?.ToUpper()
                End Using

                splash.SetStatus("Loading settings and environment...")
                CreateSubDirectories()
                OperatingMode = OperatingModes.UNDEFINED
                LogInitializationStart()

                splash.SetStatus("Initializing admin and PATAC settings...")
                Await InitializeAdminAndPatacSettingsAsync()

                splash.SetStatus("Reading configuration files...")
                splash.Hide()
                If Not RunInitializationStage("Load configuration", Function(report) ReadConfigFile(report)) Then
                    Throw New OperationCanceledException("Startup cancelled during configuration loading.")
                End If
                CaptureInitialConfigState()
                splash.Show()

                splash.SetStatus("Starting timers, reading vehicle info...")
                ' ✅ DELETED: StartTimerThread() call - obsolete timer thread removed
                GetVehicleNumber()

                splash.SetStatus("Validating dependencies...")
                splash.Hide()
                If Not RunInitializationStage("Required tools",
                    Function(report)
                        If Not ReadConfigFile(report) Then Return False
                        If PATAC Then Return True
                        If Not CheckFor7Zip() Then
                            Return ReportInitializationFailure($"7-Zip is required at '{SevenZipPath}'. Install it or correct the configured path.", report)
                        End If
                        If Not CheckForRoboCopyFolder() Then
                            Return ReportInitializationFailure($"RoboCopy is required in '{CSVScriptsPath}'. Restore it or correct the configured path.", report)
                        End If
                        Return True
                    End Function) Then
                    Throw New OperationCanceledException("Startup cancelled during dependency validation.")
                End If
                splash.Show()

                splash.SetStatus("Determining operating mode...")
                splash.Hide()
                If Not RunInitializationStage("Operating mode",
                    Function(report)
                        CheckCLEVIRFlavor()
                        Return True
                    End Function, allowConfigEditing:=False) Then
                    Throw New OperationCanceledException("Startup cancelled during operating mode setup.")
                End If
                If Not RunInitializationStage("Vehicle configuration",
                    Function(report) ReadConfigFile(report) AndAlso ReadVehicleConfigsFile(report)) Then
                    Throw New OperationCanceledException("Startup cancelled during vehicle setup.")
                End If

                splash.SetStatus("Initializing INCA interface...")
                If Not RunInitializationStage("INCA interface",
                    Function(report)
                        InitializeIncaInterface()
                        Return True
                    End Function, allowConfigEditing:=False) Then
                    Throw New OperationCanceledException("Startup cancelled during INCA interface setup.")
                End If
                splash.Show()

                splash.SetStatus("Finalizing INCA...")
                ConfigureOperatingMode()
            End Function,
            allowCancel:=True,
            anchor:=InitProgressSplash.SplashAnchor.BottomRight
        )

            ' Continue to login or the configured datalogging mode.
            InitFormAfterInitialization()

        Catch ex As OperationCanceledException
            Button3_Click(Me, EventArgs.Empty)
        Catch ex As Exception
            ReportInitializationFailure($"InitForm Load: {ex.Message}")
            Button3_Click(Me, EventArgs.Empty)
        End Try
    End Function

    ''' <summary>
    ''' Continues startup without exposing InitForm.
    ''' </summary>
    Private Sub InitFormAfterInitialization()
        Try
            ' ✅ CRITICAL: Add logging to see what we're working with
            HandleUserMessageLogging("GMRC", $"InitFormInitialization: CLEVIRFlavor='{CLEVIRFlavor}' CurrentVehicleUsage='{CurrentVehicleUsage}'")

            ' ✅ ONLY bypass InitForm if DATALOGGING mode is EXPLICITLY enabled
            If CLEVIRFlavor.ToUpper().Contains("DATALOGGING") Then
                HandleUserMessageLogging("GMRC", "DATALOGGING mode detected - bypassing InitForm")
                HandleDataLoggingMode()
                Return
            End If

            ' For DEVELOPMENT/VALIDATION, auto-drive directly to LoginForm
            HandleUserMessageLogging("GMRC", "Initialization complete - attempting auto-drive to LoginForm")
            Drive()

        Catch ex As Exception
            ReportInitializationFailure($"InitFormAfterInitialization: {ex.Message}")
            Button3_Click(Me, EventArgs.Empty)
        End Try
    End Sub

    ''' <summary>
    ''' ✅ Helper method to handle DATALOGGING mode (bypasses InitForm)
    ''' </summary>
    Private Sub HandleDataLoggingMode()
        Try
            ' Hide InitForm completely
            Left = -5000
            ShowInTaskbar = False
            Hide()
            SendToBack()

            ' ✅ DELETED: myThread cleanup (obsolete timer thread removed)
            TerminateInitThread = True

            If Not RunInitializationStage("INCA configuration", AddressOf ValidateIncaStartup) Then
                Button3_Click(Me, EventArgs.Empty)
                Return
            End If

            ' Show main client form
            GmResidentClient.Show()

            ' In on-vehicle mode, hide the main form too (VPC mode)
            If OperatingMode = OperatingModes.ResOnVpc Then
                GmResidentClient.Visible = False
            End If

            HandleUserMessageLogging("GMRC", "DATALOGGING mode: Bypassing InitForm, starting main client.")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HandleDataLoggingMode: {ex.Message}")
        End Try
    End Sub
    Private Function IsAnotherClevirInstanceRunning() As Boolean
        'Here we check to see if there is already an instance of CLEVIR running.  If so, we exit immediately...

        Dim currentProcess = Process.GetCurrentProcess()
        Return Process.GetProcesses() _
            .Where(Function(p) p.Id <> currentProcess.Id AndAlso
                               p.ProcessName.ToUpper().Contains("CLEVIR")) _
            .Any()
    End Function

    Private Async Function InitializeAdminAndPatacSettingsAsync() As Task
        ' Handle administrator mode setup
        If Not Debugger.IsAttached Then
            If CheckAdminPCsList() Then
                If MsgBox("CLEVIR Administrator?", vbYesNo) = vbYes Then
                    Dim password = InputBox("Please enter ClevirAdministrator password or OK to continue without Admin privileges...", "USER INPUT", "")
                    If password = AdminPassword Then
                        HandleUserMessageLogging("GMRC", $"{hostname} You now have CLEVIR Administrator Privileges.", DisplayMsgBox)
                        ClevirAdministrator = True
                    Else
                        HandleUserMessageLogging("GMRC", $"{hostname} Password entered is invalid. Continuing without ClevirAdministrator Privileges.", DisplayMsgBox)
                    End If
                End If
            End If
        Else
            ClevirAdministrator = True
        End If

        ' Initialize PATAC settings - run on background thread to avoid blocking UI
        Await Task.Run(Sub()
                           PATAC = Process_CLEVIR_INI_File()
                           If PATAC Then
                               SendLiveUpdate = False
                           End If
                       End Sub)

    End Function

    Private Sub LogInitializationStart()
        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", $"{hostname} Initializing {My.Application.Info.AssemblyName}{Mid(Text, InStr(Text, " "), InStr(Text, ")"))}")
        HandleUserMessageLogging("GMRC", " ")
    End Sub

    Private Async Function InitializeVehiclePtpLookupAsync() As Task
        'Here we are reading in the VehiclePTPLookup.csv file and putting its contents into the VehiclePTPLookupInfo string array for future use...

        If VehiclePTPLookupInfo Is Nothing Then
            VehiclePTPLookupInfo = New List(Of String)

            ' Read PTP lookup file on background thread
            VehiclePTPLookupInfo = Await Task.Run(Function() ReadVehiclePtpLookupFile())

            If VehiclePTPLookupInfo Is Nothing Then
                HandleUserMessageLogging("GMRC", "Invalid VehiclePTPLookupInfo. Exiting CLEVIR...", DisplayMsgBox, )
                Close()
                End
            End If
        End If
    End Function

    Private Sub InitializeIncaInterface()
        HandleUserMessageLogging("GMRC", "InitForm_Load: Creating new INCA Interface...")
        MyIncaInterface = New INCA_InterfaceClass
        HandleUserMessageLogging("GMRC", "InitForm_Load: New INCA Interface created.")

        MyIncaInterface.MyGmIncaComm = New GM_INCA_CommClass

        ' Schedule INCA minimization after connection
        Task.Run(Async Function()
                     ' Wait a bit for INCA to fully load
                     Await Task.Delay(3000)
                     ' Minimize on UI thread
                     If InvokeRequired Then
                         Invoke(New Action(AddressOf MinimizeInca))
                     Else
                         MinimizeInca()
                     End If
                 End Function)
    End Sub

    Private Sub ConfigureOperatingMode()
        'Configure operating mode based on user name and CLEVIR flavor

        If Not CLEVIRFlavor.Contains("DATALOGGING") Then
            OperatingMode = OperatingModes.ResOnVpc
            GmResidentClient.Text = $"Vehicle {VehicleNumber} - ON VEHICLE MODE"

            If VehicleNumber.ToUpper() = "UNDEFINED" Then
                Button1.Enabled = False 'Disable Drive button
            End If
        Else
            ' DATALOGGING mode
            If Not GmResidentClient.EtasDefaultUserName.Contains("VEHTESTFIDCSV") Then
                GmResidentClient.EtasDefaultUserName = "VEHTESTFIDCSV"
            End If
            OperatingMode = OperatingModes.ResOnVpc
            GmResidentClient.Text = $"Vehicle {VehicleNumber} - ON VEHICLE MODE"
        End If

        UserStatusInfo.Hide()
        ConfigureMenuItems()

        ' Ensure VALIDATION is the active mode
        CurrentVehicleUsage = "VALIDATION"
        ZipTheMF4Files = False
        HandleUserMessageLogging("GMRC", "InitForm: Vehicle usage defaulted to VALIDATION")
    End Sub

    Private Sub ConfigureMenuItems()
        GmResidentClient.ChangeVehicleNumberToolStripMenuItem.Visible = True
        GmResidentClient.SetDataCollectionRatemsecToolStripMenuItem.Visible = True
        GmResidentClient.SwitchINCAUserToolStripMenuItem.Visible = True
        GmResidentClient.RestartINCAToolStripMenuItem.Visible = True
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button3.Click
        'This is the EXIT Button on the InitForm

        Try
            HandleUserMessageLogging("GMRC", " ")
            HandleUserMessageLogging("GMRC", "InitForm: EXIT Button Pressed...")

            ' ✅ Only write files if content has changed
            If HasConfigChanged() Then
                ' WriteConfigFile() 
            Else
                HandleUserMessageLogging("GMRC", "Skipping WriteConfigFile() - no changes detected")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "InitForm Exit Button: " & ex.Message)

        Finally
            HandleUserMessageLogging("GMRC", "CLEVIR Exited from Init Form.")

            ' Run CloseINCA on a background thread with a 5-second timeout.
            ' CloseINCA makes blocking COM calls (UnlockTool / CloseTool) that can
            ' hang indefinitely if INCA is unresponsive; we must not block the UI thread.
            If MyIncaInterface IsNot Nothing Then
                Dim closeTask As System.Threading.Tasks.Task =
                    System.Threading.Tasks.Task.Run(Sub() MyIncaInterface.CloseINCA())
                If Not closeTask.Wait(TimeSpan.FromSeconds(5)) Then
                    HandleUserMessageLogging("GMRC", "CloseINCA timed out after 5 s — forcing exit.")
                End If
            End If

            Close()
            Environment.Exit(0)
        End Try
    End Sub



    ' ✅ Call this after ReadConfigFile() to capture baseline
    Private Sub CaptureInitialConfigState()
        Try
            Dim configPath As String = Path.Combine(My.Application.Info.DirectoryPath, "Config.xml")
            If File.Exists(configPath) Then
                _originalConfigData = File.ReadAllText(configPath)
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CaptureInitialConfigState: {ex.Message}")
        End Try
    End Sub

    ' ✅ Check if config has changed
    Private Function HasConfigChanged() As Boolean
        Try
            Dim currentConfig As String = GmResidentClient.GetCurrentConfigData()
            Return Not String.Equals(_originalConfigData, currentConfig, StringComparison.Ordinal)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HasConfigChanged: {ex.Message}")
            Return True ' Write on error to be safe
        End Try
    End Function

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button1.Click

        'This is the Drive Button on the InitForm.  When the Drive Button is pressed, we begin the main initialization sequence...

        HandleUserMessageLogging("GMRC", "")
        HandleUserMessageLogging("GMRC", "DRIVE Pressed")
        HandleUserMessageLogging("GMRC", "")

        Drive()

    End Sub

    Private Sub Button1_GotFocus(sender As Object, e As EventArgs) Handles Button1.GotFocus

    End Sub

    Private Sub InitForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown

        HandleUserMessageLogging("GMRC", "InitForm Shown...")

        If VehicleNumber = "UNDEFINED" Then
            HandleUserMessageLogging("GMRC", "InitForm: VehicleNumber is UNDEFINED. You must select a valid vehicle number to continue... ", DisplayMsgBox, )
        End If

    End Sub

    Private Function CheckAdminPCsList() As Boolean

        'Called from InitForm_Load. Reads adminPCs.txt file to see if it contains the host name of the
        'computer currently running CLEVIR.

        'If this routine returns TRUE, the user will be asked if they want to run as
        'ClevirAdministrator and asked to enter a password "poc" is the password.  So, only those PCs that
        'have this file and only if it contains their computer host name, will they be able to run
        'as ClevirAdministrator, which provides access to various functions not available to the
        'standard user.

        Dim fnum As Integer
        Dim filename As String = My.Application.Info.DirectoryPath & "\adminPCs.txt"

        CheckAdminPCsList = False

        If File.Exists(filename) = True Then
            fnum = FreeFile()
            FileOpen(fnum, filename, OpenMode.Input)
            Do While Not EOF(fnum)
                If LineInput(fnum) = hostname Then
                    CheckAdminPCsList = True
                    Exit Do
                End If
            Loop
            FileClose(fnum)
        End If

    End Function

    'Private Async Function InitializeWithProgressAsync() As Task
    '    ' Show progress indicator
    '    Dim progress As New Progress(Of String)(Sub(message)
    '                                                HandleUserMessageLogging("GMRC", message,,, FlashMsgOn)
    '                                            End Sub)

    '    progress.Report("Initializing admin settings...")
    '    Await InitializeAdminAndPatacSettingsAsync()

    '    progress.Report("Configuring network access...")
    '    Await ConfigureNetworkAccessAsync()

    '    progress.Report("Validating dependencies...")
    '    Await ValidateRequiredDependenciesAsync()

    '    ' Continue with other initialization steps...
    'End Function

    Private Sub InitForm_MinimumSizeChanged(sender As Object, e As EventArgs) Handles Me.MinimumSizeChanged

    End Sub


End Class
