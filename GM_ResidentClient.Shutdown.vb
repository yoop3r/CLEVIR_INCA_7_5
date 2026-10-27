Option Strict Off
Option Explicit On

Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Speech.Synthesis
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml
Imports SharpPcap

Partial Public Class GmResidentClient

    ''' <summary>
    ''' Handles graceful application shutdown with proper cleanup sequencing.
    ''' Prevents race conditions using atomic flags and ensures all resources are released.
    ''' </summary>
    ''' <param name="exitParameter">Optional parameter: "" = show dialog, "Complete" = force exit</param>
    ''' <returns>True if exit completed successfully, False if cancelled by user</returns>
    Public Function ExitApp(Optional ByVal exitParameter As String = "") As Boolean
        ' ════════════════════════════════════════════════════════════════
        ' ✅ FIX 1: ATOMIC FLAG CHECK - Prevent race conditions
        ' ════════════════════════════════════════════════════════════════

        ' ✅ ATOMIC FLAG: Set to True only if it was False
        If Not System.Threading.Interlocked.CompareExchange(exitInProgress, True, False) = False Then
            ' Race condition: Another thread beat us to it
            HandleUserMessageLogging("GMRC", "ExitApp: Race detected, deferring to other thread")
            Return True
        End If

        ' Local working variables
        Dim uflags As Long = 0
        Dim selectedOption As ExitOption = ExitOption.CancelExit
        Dim teardownStarted As Boolean = False

        Try
            HandleUserMessageLogging("GMRC", $"ExitApp ({If(String.IsNullOrEmpty(exitParameter), "User Initiated", exitParameter)}) called...")

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 7: USER CONFIRMATION (IF NEEDED) - asked BEFORE any teardown so that
            ' Cancel leaves CLEVIR fully running (display forms, LiDAR, background loop).
            ' ════════════════════════════════════════════════════════════════
            If String.IsNullOrEmpty(exitParameter) Then
                selectedOption = ShowExitOptionDialog()
                If selectedOption = ExitOption.CancelExit Then
                    ExitApp = False
                    Return False
                End If
            End If

            ' ══════════════════════════════════════════════════════════════
            ' STOP BACKGROUND WORKERS FIRST
            ' ══════════════════════════════════════════════════════════════
            teardownStarted = True
            StopBackgroundWorkersForExit()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 4: SHUTDOWN ALL LIDAR DEVICES
            ' ════════════════════════════════════════════════════════════════
            ShutdownAllLidarDevices()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 5: CLOSE DISPLAY FORMS (AFTER BACKGROUND TASKS STOPPED)
            ' ════════════════════════════════════════════════════════════════
            CloseDisplayForms()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 6: HANDLE COMPRESSION (IF CONFIGURED)
            ' ════════════════════════════════════════════════════════════════
            CompressRemainingFilesOnExit()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 7b: APPLY THE SELECTED EXIT OPTION (close INCA, copy DB, ...)
            ' ════════════════════════════════════════════════════════════════
            ApplyExitOption(exitParameter, selectedOption)

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 8: VALIDATE CRITICAL PATHS
            ' ════════════════════════════════════════════════════════════════
            EnsureExitPathsValid()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 9: FILE OPERATIONS & HOUSEKEEPING
            ' ════════════════════════════════════════════════════════════════
            PerformExitHousekeeping(selectedOption)

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 10/11: AUDIO-TO-TEXT CONVERSION, FILE MANIPULATIONS FOR UPLOAD
            ' ════════════════════════════════════════════════════════════════
            ProcessSessionDataOnExit()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 12/13: OXTS INTERFACE AND INCA RESOURCES CLEANUP
            ' ════════════════════════════════════════════════════════════════
            ReleaseDeviceResourcesOnExit()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 14: WINDOWS SHUTDOWN/RESTART (IF REQUESTED)
            ' ════════════════════════════════════════════════════════════════
            HandleShutdownOrRestart(uflags)

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 15: FINAL STATUS NOTIFICATION
            ' ════════════════════════════════════════════════════════════════
            ShowExitCompleteNotification()

            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 16: CLOSE FORM BEFORE FORCEFUL EXIT
            ' ════════════════════════════════════════════════════════════════
            If Not Me.IsDisposed Then
                Me.Close()  ' Triggers FormClosing (which now allows close)
            End If

            ExitApp = True
            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ExitApp: Unexpected error - {ex.Message}")
            ExitApp = False
            Return False

        Finally
            ' ════════════════════════════════════════════════════════════════
            ' ✅ FIX 17: RESET FLAG IN FINALLY BLOCK (ALWAYS RUNS)
            ' ════════════════════════════════════════════════════════════════
            ' Once teardown has started the exit is committed for every caller (dialog option, "Complete",
            ' "User Initiated"): forced exits leave selectedOption at CancelExit, so it cannot decide this.
            If Not teardownStarted Then
                RollBackCancelledExit()
            Else
                TerminateCommittedExit()
            End If
        End Try
    End Function

    Private Sub CompressRemainingFilesOnExit()
        'Called from ExitApp after the display forms are closed.

        If AnyCompressionEnabled() Then
            CompressOrphanedFiles()      ' Queue orphaned files
            Handle7ZipProcess()          ' Wait for completion
        Else
            HandleUserMessageLogging("GMRC", "ExitApp: All compression types disabled - skipping orphaned file compression")
        End If
    End Sub

    Private Sub PerformExitHousekeeping(ByVal selectedOption As ExitOption)
        'Called from ExitApp after the exit paths are validated.

        Copy_GM_INCA_CommLog_File()
        UserStatusInfo.Hide()

        ' Quit external applications if they were started
        If CanalyzerCaptureStarted Then QuitCanalyzer()
        If VehicleSpyCaptureStarted Then QuitVehicleSpy()

        ' ════════════════════════════════════════════════════════════════
        ' ✅ FIX: Set _killProcesses based on user's selected exit option
        ' ════════════════════════════════════════════════════════════════
        ' Stop killer thread
        StopKillerThreadForExit(selectedOption)

        ' Stop recording monitor task
        StopRecordingMonitorTask()

        ' Write configuration changes
        SaveSignalListChanges()
    End Sub

    Private Sub ProcessSessionDataOnExit()
        'Called from ExitApp after housekeeping.

        ' ✅ FIX 10: AUDIO-TO-TEXT CONVERSION (IF ENABLED)
        If AudioToTextConversion Then
            RunAudioToTextConversionOnExit()
        End If

        ' ✅ FIX 11: FILE MANIPULATIONS FOR UPLOAD
        If HaveRecorded Then
            HandleFileManipulationsForUpload()
        End If
    End Sub

    Private Sub ReleaseDeviceResourcesOnExit()
        'Called from ExitApp before the optional Windows shutdown/restart.

        ' ✅ FIX 12: OXTS INTERFACE CLEANUP
        If MyOxtsInterface IsNot Nothing Then
            StopOxtsInterfaceOnExit()
        End If

        ' ✅ FIX 13: INCA RESOURCES CLEANUP
        CleanUpINCAResources()
        HandleUserMessageLogging("GMRC", "Cleaned up INCA resources.")
    End Sub

    Private Sub ShowExitCompleteNotification()
        'Called from ExitApp just before the main form is closed.

        HandleUserMessageLogging("GMRC", "CLEVIR Exit in progress, performing housekeeping...",,, FlashMsgOn)
        HandleUserMessageLogging("GMRC", "Exit Complete.",, )
        StatusNotifier.Toast("Exit Complete", "CLEVIR", durationMs:=1000, ensureMainOnTop:=False)
        Thread.Sleep(1000)
    End Sub

    Private Sub RollBackCancelledExit()
        'Called from ExitApp's Finally when the exit was cancelled before any teardown started.

        ' Only reset flag if user cancelled - allow exit to complete otherwise
        exitInProgress = False
        Cursor = Cursors.Arrow
        HandleUserMessageLogging("GMRC", "ExitApp: exitInProgress reset (user cancelled)")
        ' OnVehicleScreen's EXIT button sets ExitPressed; left True, its window X would close the form without ExitApp.
        Volatile.Write(ExitPressed, False)
        ResumeBackgroundTasksAfterCancelledExit()
    End Sub

    Private Sub TerminateCommittedExit()
        'Called from ExitApp's Finally once teardown has started: the exit is committed and the process ends here.

        HandleUserMessageLogging("GMRC", "ExitApp: exitInProgress remains True (exit committed)")
        ' Force termination (point of no return)
        Try
            Application.Exit()
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ExitApp: Application.Exit failed ({ex.GetType().Name}): {ex.Message}")
        End Try
        Environment.Exit(0)
    End Sub

    Private Sub CompressOrphanedFiles()
        ' Find orphaned files (those without corresponding .zip) in the current recording session only and compress
        ' them based on configuration settings. Earlier sessions were compressed per sequence and are not rescanned.

        Dim dataRoot = FileMaintenance.SessionCompressionFolder(FinalPathToSaveData, FileMaintenance.VehicleDataRoot(BaseDataCollectionPath, VehicleNumber))

        If dataRoot Is Nothing Then
            HandleUserMessageLogging("GMRC", $"CompressOrphanedFiles: No recording session folder ('{FinalPathToSaveData}') - skipping")
            Return
        End If

        Dim orphanedCount As Integer = 0

        ' ════════════════════════════════════════════════════════════
        ' ✅ FIX: Check CompressMF4 flag BEFORE processing .mf4 files
        ' ════════════════════════════════════════════════════════════
        If CompressMF4 Then
            orphanedCount += QueueOrphanedFiles(dataRoot, "*.mf4", "MF4")
        Else
            HandleUserMessageLogging("GMRC", "CompressOrphanedFiles: MF4 compression disabled - skipping .mf4 files")
        End If

        ' ════════════════════════════════════════════════════════════
        ' ✅ FIX: Check CompressPCAP flag BEFORE processing .pcap files
        ' ════════════════════════════════════════════════════════════
        If CompressPCAP Then
            orphanedCount += QueueOrphanedFiles(dataRoot, "*.pcap", "PCAP")
        Else
            HandleUserMessageLogging("GMRC", "CompressOrphanedFiles: PCAP compression disabled - skipping .pcap files")
        End If

        ' ════════════════════════════════════════════════════════════
        ' ✅ OPTIONAL: Add .asc and .vsb support if needed
        ' ════════════════════════════════════════════════════════════
        If CompressASC Then
            orphanedCount += QueueOrphanedFiles(dataRoot, "*.asc", "ASC")
        End If

        If CompressVSB Then
            orphanedCount += QueueOrphanedFiles(dataRoot, "*.vsb", "VSB")
        End If

        If orphanedCount > 0 Then
            HandleUserMessageLogging("GMRC", $"CompressOrphanedFiles: Queued {orphanedCount} file(s) for background compression")
        Else
            HandleUserMessageLogging("GMRC", "CompressOrphanedFiles: No orphaned files found (or all compression disabled)")
        End If
    End Sub

    ''' <summary>
    ''' Queues background compression for each orphaned file (no sibling .zip) matching <paramref name="searchPattern"/>
    ''' under <paramref name="dataRoot"/>. Returns the number queued.
    ''' </summary>
    Private Function QueueOrphanedFiles(dataRoot As String, searchPattern As String, kind As String) As Integer
        Dim count As Integer = 0
        For Each orphan In FileMaintenance.FindOrphanedFiles(dataRoot, searchPattern)
            HandleUserMessageLogging("GMRC", $"Found orphaned {kind}: {Path.GetFileName(orphan)}")
            CompressSingleFileWithRetryAsync(orphan, FileMaintenance.ZipPathFor(orphan))
            count += 1
        Next
        Return count
    End Function

    Private Sub Handle7ZipProcess()
        ' CHECK 1: Legacy 7-Zip Process
        WaitForLegacy7ZipOnExit()

        ' CHECK 2: Background Compression Tasks (New Async System)
        WaitForBackgroundCompressionOnExit()
    End Sub

    Private Sub WaitForLegacy7ZipOnExit()
        ' ════════════════════════════════════════════════════════════
        ' CHECK 1: Legacy 7-Zip Process
        ' ════════════════════════════════════════════════════════════
        If Is7ZipRunning() Then
            If Not ShutdownWindows Then
                If MsgBox("File Zipping is not yet complete. Wait for file Zipping to complete before Exiting?", vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", "ExitApp: Waiting on 7zip to finish...")
                    Do While Is7ZipRunning()
                        Thread.Sleep(2000)
                    Loop
                Else
                    HandleUserMessageLogging("GMRC", "ExitApp: User decided not to wait for Zipping to complete before exiting...")
                End If
            Else
                HandleUserMessageLogging("GMRC", "ExitApp: Waiting on 7zip to finish...")
                Do While Is7ZipRunning()
                    Thread.Sleep(2000)
                Loop
            End If
        End If
    End Sub

    Private Sub WaitForBackgroundCompressionOnExit()
        ' ════════════════════════════════════════════════════════════
        ' CHECK 2: Background Compression Tasks (New Async System)
        ' ════════════════════════════════════════════════════════════
        Dim activeTasks As List(Of Task)
        SyncLock CompressionTasksLock
            activeTasks = ActiveCompressionTasks _
            .Where(Function(t) Not t.IsCompleted) _
            .ToList()
        End SyncLock

        If activeTasks.Any() Then
            Dim taskCount As Integer = activeTasks.Count

            If Not ShutdownWindows Then
                Dim result = MsgBox(
                $"{taskCount} background compression task(s) still running. Wait for completion before exiting?",
                vbYesNo + vbQuestion,
                "CLEVIR Background Compression"
            )

                If result = vbYes Then
                    HandleUserMessageLogging("GMRC",
                    $"ExitApp: Waiting for {taskCount} compression tasks to finish...")

                    ' ✅ SHOW PROGRESS FORM (Optional enhancement)
                    Using progressForm As New Form()
                        progressForm.Text = "Waiting for Compression..."
                        progressForm.Size = New Size(400, 150)
                        progressForm.StartPosition = FormStartPosition.CenterScreen
                        progressForm.FormBorderStyle = FormBorderStyle.FixedDialog
                        progressForm.ControlBox = False

                        Dim label As New Label With {
                        .Text = $"Compressing {taskCount} file(s)... Please wait.",
                        .Dock = DockStyle.Fill,
                        .TextAlign = ContentAlignment.MiddleCenter,
                        .Font = New Font("Segoe UI", 12, FontStyle.Bold)
                    }
                        progressForm.Controls.Add(label)

                        progressForm.Show()
                        progressForm.Refresh()

                        ' ✅ WAIT FOR ALL TASKS
                        Try
                            Task.WaitAll(activeTasks.ToArray(), TimeSpan.FromMinutes(10))
                        Catch ex As Exception
                            HandleUserMessageLogging("GMRC",
                            $"Compression wait error: {ex.Message}")
                        End Try

                        progressForm.Close()
                    End Using

                    HandleUserMessageLogging("GMRC", "ExitApp: All compression tasks completed")
                Else
                    HandleUserMessageLogging("GMRC",
                    "ExitApp: User chose to exit without waiting for compression")
                End If
            Else
                ' ✅ SHUTDOWN MODE: Always wait
                HandleUserMessageLogging("GMRC",
                $"ExitApp: Waiting for {taskCount} compression tasks (shutdown mode)...")
                Try
                    Task.WaitAll(activeTasks.ToArray(), TimeSpan.FromMinutes(10))
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                    $"Compression wait error: {ex.Message}")
                End Try
            End If
        End If
    End Sub

    Private Sub Copy_GM_INCA_CommLog_File()
        Try
            Dim sourceLogFile As String = Path.Combine(My.Application.Info.DirectoryPath, "GM_INCA_Comm.log")

            If Not File.Exists(sourceLogFile) OrElse FileInUse(sourceLogFile) Then
                HandleUserMessageLogging("GMRC", "Log file not available for copying")
                Return
            End If

            ' Validate paths before using Path.Combine
            If String.IsNullOrEmpty(FinalPathToSaveData) OrElse String.IsNullOrEmpty(BaseDataCollectionPath) Then
                HandleUserMessageLogging("GMRC", "Required paths are null - cannot copy log file")
                Return
            End If

            If Len(FinalPathToSaveData) > 0 AndAlso Directory.Exists(FinalPathToSaveData) Then
                HandleUserMessageLogging("GMRC", "Copying GM_INCA_Comm.log...",,, FlashMsgOn)
                Dim destPath As String = Path.Combine(FileMaintenance.VehicleDataRoot(BaseDataCollectionPath, VehicleNumber), FileMaintenance.CommLogArchiveFileName(DateTime.Now))
                FileCopy(sourceLogFile, destPath)
            Else
                ' Fallback to application directory
                Dim info As New FileInfo(sourceLogFile)
                If info.Length > 10000 Then
                    HandleUserMessageLogging("GMRC", "Copying GM_INCA_Comm.log to fallback location...",,, FlashMsgOn)
                    Dim fallbackPath As String = Path.Combine(My.Application.Info.DirectoryPath, FileMaintenance.CommLogFallbackFileName(DateTime.Now))
                    FileCopy(sourceLogFile, fallbackPath)
                End If
            End If

            ' Clean up original log file
            HandleUserMessageLogging("GMRC", "Deleting GM_INCA_Comm.log file...",,, FlashMsgOn)
            File.Delete(sourceLogFile)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CopyLogFile error: {ex.Message}")
        End Try
    End Sub

    Private Sub SaveSignalListChanges()
        If ClevirAdministrator AndAlso GridCellPropConfig._changesMade Then
            If Not ConfigureForNewSoftwareVersion Then
                HandleUserMessageLogging("GMRC", "ExitApp: Save Signal List Changes?")
                If MsgBox("Save Signal List Changes?", vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", "ExitApp: Save Signal List Changes = Yes")
                    WriteSignalListFile(INCAVariableFile)
                Else
                    HandleUserMessageLogging("GMRC", "ExitApp: Save Signal List Changes = No")
                End If
            Else
                HandleUserMessageLogging("GMRC", "ExitApp: Saving Signal List Changes due to removal of invalid signals...", DisplayMsgBox)
                WriteSignalListFile(INCAVariableFile)
            End If
        End If
    End Sub

    ' -- REPLACE the entire routine with this version (no local helpers) --
    Private Sub HandleFileManipulationsForUpload()
        If Not HaveRecorded Then Return

        Dim sourceLogPath As String = Path.Combine(My.Application.Info.DirectoryPath, FileMaintenance.ResidentLogFileName)
        Dim aggregateAnnoFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, $"{VehicleNumber}_AggregateAnnotations.csv")
        Dim driverAnnoFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, $"{VehicleNumber}_Driver_Anno_Cons.xlsx")
        Dim vehicleDataRoot As String = FileMaintenance.VehicleDataRoot(BaseDataCollectionPath, VehicleNumber)

        ' Log destination
        Dim destinationLogPath As String = Path.Combine(vehicleDataRoot, FileMaintenance.ResidentLogFileName)

        Try
            Directory.CreateDirectory(Path.GetDirectoryName(destinationLogPath))
            File.Copy(sourceLogPath, destinationLogPath, overwrite:=True)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Copy log failed: {ex.Message}")
        End Try

        ' Rotated log (earlier entries once the active log passed the size cap)
        Dim sourceBackupLogPath As String = Path.Combine(My.Application.Info.DirectoryPath, FileMaintenance.ResidentLogBackupFileName)
        If File.Exists(sourceBackupLogPath) Then
            Try
                File.Copy(sourceBackupLogPath, Path.Combine(vehicleDataRoot, FileMaintenance.ResidentLogBackupFileName), overwrite:=True)
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"Copy rotated log failed: {ex.Message}")
            End Try
        End If

        ' Aggregate anno
        If File.Exists(aggregateAnnoFilePath) Then
            Dim aggregateDestinationPath As String = Path.Combine(vehicleDataRoot, $"{VehicleNumber}_AggregateAnnotations.csv")
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(aggregateDestinationPath))
                File.Copy(aggregateAnnoFilePath, aggregateDestinationPath, overwrite:=True)
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"Copy aggregate anno failed: {ex.Message}")
            End Try
        End If

        ' Driver anno (single, consolidated copy with wait + retries)
        If File.Exists(driverAnnoFilePath) Then
            Dim driverAnnoDestinationPath As String = Path.Combine(vehicleDataRoot, $"{VehicleNumber}_Driver_Anno_Cons.xlsx")

            ' Wait for exclusive access, then copy with retries
            If FileMaintenance.WaitUntilFileReady(driverAnnoFilePath, 10, 500, AddressOf LogShutdownMessage) Then
                If Not FileMaintenance.CopyWithRetries(driverAnnoFilePath, driverAnnoDestinationPath, maxRetries:=5, delayMs:=1000, log:=AddressOf LogShutdownMessage) Then
                    HandleUserMessageLogging("GMRC", $"Failed to copy Driver_Anno_Cons.xlsx after retries")
                End If
            Else
                HandleUserMessageLogging("GMRC", "Driver_Anno_Cons.xlsx not available after waiting - skipping copy")
            End If
        End If
    End Sub

    Private Shared Sub LogShutdownMessage(message As String)
        HandleUserMessageLogging("GMRC", message)
    End Sub

    Private Sub CloseDisplayForms()

        ' ✅ FIX: Prevent multiple calls
        Static closingForms As Boolean = False
        If closingForms Then Return

        HandleUserMessageLogging("GMRC", "ExitApp: Closing Display Forms...")

        ' ✅ Suspend event processing during shutdown
        'Application.RemoveMessageFilter(Me)  ' Prevents stray events

        If MyDFs IsNot Nothing AndAlso MyDFs.Count > 0 Then
            ' Hide rather than Close: Close() on a form that was never shown disposes it (and its grids) without
            ' raising FormClosing, which breaks the signal list save that runs later in the exit sequence.
            For Each form As FormDataClass In MyDFs
                form?.Hide()
            Next
            MyDFs.Clear()
        End If

        ' Close other forms
        ' Call the method with the name of the form you want to check and close
        CloseFormIfLoaded("DeviceStatus")
        CloseFormIfLoaded("FusionStatusDisplay")
        CloseFormIfLoaded("MyTdGraphicsContainer")
        CloseFormIfLoaded("LkaForm")
    End Sub

    Private Sub CloseFormIfLoaded(ByVal formName As String)
        ' Check if the form is loaded
        Dim formToClose As Form = Application.OpenForms.Cast(Of Form)().FirstOrDefault(Function(f) f.Name = formName)
        ' Form is loaded, close it
        formToClose?.Close()
    End Sub

    Private Sub CleanUpINCAResources()
        If myinca IsNot Nothing Then
            MyIncaInterface.RCI2_Cleanup()
            MyIncaInterface.UnlockExperiment()
            myinca.UnlockTool()
            myinca = Nothing
            MyHWC = Nothing
        End If
        InitForm.Close()
    End Sub

    Private Sub HandleShutdownOrRestart(ByRef uflags As Long)
        If ShutdownWindows OrElse RestartWindows Then
            Thread.Sleep(3000)
            AcquireShutdownPrivilege()
            Thread.Sleep(5000)
            HandleUserMessageLogging("GMRC", "ExitApp: Exiting Windows...",,)

            If Not Debugger.IsAttached Then
                uflags = If(ShutdownWindows, EwxPoweroff Or EwxShutdown Or EwxForce, EWX_REBOOT Or EwxForce)
                NativeMethods.ExitWindows(uflags, 0)
                End
            End If
        End If
    End Sub

    Private Sub ShutdownAndRestartInca(ByVal sleeptime As Integer)

        'User selection from the Actions dropdown menu, allows the user to shutdown and restart INCA
        'Can also be called in response to a communication loss between the app and INCA.  Also called
        'after we perform a FULL signal registration during initialization.

        Dim forceInit As Boolean
        Dim errorMsg As String

        Dim e As EventArgs

        Try

            e = EventArgs.Empty

            Cursor = Cursors.WaitCursor

            StopMeasurementAndCloseInca()

            RedisplayOnVehicleForm("GroupBox1")

            Thread.Sleep(sleeptime)

            errorMsg = ""

            'This flag is used to keep events from firing which are associated with
            'various controls having default text added during initialization
            _initializing = True
            HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: Initializing...")

            OnVehicleScreen.Refresh()

            'We always set the ForceInit flag to false in the initial call to InitINCA in the GmResidentClient
            'We set this to false here, we do not want to force signal registration if we are already initialized 
            'and have our signals registered, it is faster for development.

            forceInit = False

            'Here we initialize INCA based on information read in from the config.xml file
            If MyIncaInterface.InitINCA(INCADatabase, INCAWorkspace, INCAExperiment, forceInit, errorMsg, False) <> IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then

                RegisterSignalsAfterIncaInit()

            Else

                HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: INCA Initialization returned - " & errorMsg)

                If Not Debugger.IsAttached Then

                    OnVehicleScreen.TopMost = True

                    If MsgBox("INCA Initialization returned - " & errorMsg & " Do you wish to continue?", vbYesNo) = vbNo Then
                        HandleUserMessageLogging("GMRC", "INCA Initialization returned - " & errorMsg & " Do you wish to continue? No.",,)
                        ExitApp()
                    End If

                End If

                RegisterSignalsAfterIncaInit()

            End If

            FinishIncaRestart()

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: " & ex.Message, DisplayMsgBox,)
        End Try

    End Sub

    Private Sub StopBackgroundWorkersForExit()
        'Called from ExitApp. Cancels init/background work and waits (up to 10 s) for BackgroundWorker1 to stop.

        _enableMyBackgroundTasks = False
        FormDisplayed = False

        ' ════════════════════════════════════════════════════════════════
        ' ✅ FIX 2: CANCEL ONGOING OPERATIONS
        ' ════════════════════════════════════════════════════════════════
        If _initCts IsNot Nothing Then
            Try
                _initCts.Cancel()
                HandleUserMessageLogging("GMRC", "ExitApp: Cancelled ongoing initialization")
            Catch ex As Exception
                ' Ignore cancellation errors
            End Try
        End If

        ' ════════════════════════════════════════════════════════════════
        ' ✅ FIX 3: STOP BACKGROUND WORKERS
        ' ════════════════════════════════════════════════════════════════
        _enableMyBackgroundTasks = False

        ' Cancel background tasks immediately for faster shutdown
        If _backgroundTasksCts IsNot Nothing Then
            Try
                _backgroundTasksCts.Cancel()
                HandleUserMessageLogging("GMRC", "ExitApp: Cancelled background tasks via CancellationToken")
            Catch ex As Exception
                ' Ignore cancellation errors
            End Try
        End If

        FormDisplayed = False
        Cursor = Cursors.WaitCursor
        HandleUserMessageLogging("GMRC", "ExitApp: Stopping background tasks...")

        ' Wait for background worker to stop (with timeout)
        If BackgroundWorker1.IsBusy Then
            HandleUserMessageLogging("GMRC", "Waiting for background tasks to complete...")
            Dim startTime As DateTime = DateTime.Now
            Const timeoutMs As Integer = 10000 ' 10 second timeout

            While BackgroundWorker1.IsBusy AndAlso (DateTime.Now - startTime).TotalMilliseconds < timeoutMs
                Application.DoEvents()
                Thread.Sleep(100)
            End While

            If BackgroundWorker1.IsBusy Then
                HandleUserMessageLogging("GMRC", "Warning: Background tasks did not stop within timeout")
            End If
        End If

        ' Dispose the background tasks CancellationTokenSource
        If _backgroundTasksCts IsNot Nothing Then
            _backgroundTasksCts.Dispose()
            _backgroundTasksCts = Nothing
        End If
    End Sub

    Private Function ShowExitOptionDialog() As ExitOption
        'Called from ExitApp before any teardown. Returns the user's choice; CancelExit when cancelled
        'or when the dialog was closed without a selection.

        ' Display the ExitAppForm modally.
        ' ✅ WPF PILOT: ExitAppForm was migrated from WinForms to WPF (ExitAppFormWpf).
        ' System.Windows.Window is not IDisposable (unlike WinForms Form), so the
        ' previous "Using...End Using" wrapper is replaced with a plain Dim; there was
        ' no unmanaged/disposable state on the original form to clean up.
        Dim exitForm As New ExitAppFormWpf()
        exitForm.Topmost = True
        exitForm.ShowDialog()

        Dim selected As ExitOption = NormalizeExitDialogOption(exitForm.SelectedExitOption)
        If selected = ExitOption.CancelExit Then
            If exitForm.SelectedExitOption = ExitOption.CancelExit Then
                HandleUserMessageLogging("GMRC", "ExitApp: Exit Cancelled by user")
            Else
                HandleUserMessageLogging("GMRC", $"ExitApp: Exit dialog closed without a selection ({exitForm.SelectedExitOption}) - treating as cancel")
            End If
        End If
        Return selected
    End Function

    Friend Shared Function NormalizeExitDialogOption(ByVal dialogOption As ExitOption) As ExitOption
        'Only the three exit choices commit to exiting; anything else (Cancel, window closed = None) is a cancel.
        Select Case dialogOption
            Case ExitOption.ExitClevirAndCloseInca, ExitOption.ExitClevirOnly, ExitOption.ExitClevirCloseIncaShutdownWindows
                Return dialogOption
            Case Else
                Return ExitOption.CancelExit
        End Select
    End Function

    Private Sub ApplyExitOption(ByVal exitParameter As String, ByVal selectedOption As ExitOption)
        'Called from ExitApp after teardown/compression (original position of these actions), once the user has committed.

        If String.IsNullOrEmpty(exitParameter) Then
            Select Case selectedOption
                Case ExitOption.ExitClevirAndCloseInca
                    HandleUserMessageLogging("GMRC", "ExitApp: Exit CLEVIR and Close INCA selected")
                    CheckForExperiment = False
                    MyIncaInterface?.CloseINCA()
                    If Not PlaybackMode Then CopyINCADatabase()
                    ' ✅ SET FLAG: User committed to exiting
                    exitInProgress = True
                    HandleUserMessageLogging("GMRC", $"ExitApp: exitInProgress set to True for option {selectedOption}")

                Case ExitOption.ExitClevirOnly
                    HandleUserMessageLogging("GMRC", "ExitApp: Exit CLEVIR Only selected (leaving INCA running)")
                    CheckForExperiment = False
                    ' ✅ SET FLAG: User committed to exiting
                    exitInProgress = True
                    HandleUserMessageLogging("GMRC", $"ExitApp: exitInProgress set to True for option {selectedOption}")

                Case ExitOption.ExitClevirCloseIncaShutdownWindows
                    HandleUserMessageLogging("GMRC", "ExitApp: Exit CLEVIR, Close INCA and Shutdown Windows selected")
                    CheckForExperiment = False
                    MyIncaInterface?.CloseINCA()
                    If Not PlaybackMode Then CopyINCADatabase()
                    ShutdownWindows = True
                    ' ✅ SET FLAG: User committed to exiting
                    exitInProgress = True
                    HandleUserMessageLogging("GMRC", $"ExitApp: exitInProgress set to True for option {selectedOption}")
            End Select

        ElseIf exitParameter = "Complete" Then
            ' Force a complete shutdown without user interaction
            HandleUserMessageLogging("GMRC", "ExitApp: Exit complete - Closing INCA")
            CheckForExperiment = False
            MyIncaInterface?.CloseINCA()
        End If
    End Sub

    Private Sub ResumeBackgroundTasksAfterCancelledExit()
        'Called from ExitApp when the exit was cancelled before any teardown. MyBackgroundTasks ends as soon as it
        'sees exitInProgress (e.g. while the exit dialog is open), so restart it unless it is still running.

        If Volatile.Read(_backgroundLoopActive) Then Return

        _enableMyBackgroundTasks = True
        If _backgroundTasksCts Is Nothing Then _backgroundTasksCts = New CancellationTokenSource()

        If Not BackgroundWorker1.IsBusy Then
            BackgroundWorker1.RunWorkerAsync(2000)
            HandleUserMessageLogging("GMRC", "ExitApp: Background tasks resumed after cancelled exit")
        End If
    End Sub

    Private Sub EnsureExitPathsValid()
        'Called from ExitApp. Fallbacks for paths/vehicle number used by the exit housekeeping.

        If String.IsNullOrEmpty(BaseDataCollectionPath) Then
            HandleUserMessageLogging("GMRC", "BaseDataCollectionPath is null/empty during exit - using fallback")
            BaseDataCollectionPath = My.Application.Info.DirectoryPath
        End If

        If String.IsNullOrEmpty(FinalPathToSaveData) Then
            FinalPathToSaveData = FileMaintenance.VehicleDataRoot(BaseDataCollectionPath, VehicleNumber)
            HandleUserMessageLogging("GMRC", $"FinalPathToSaveData set to: {FinalPathToSaveData}")
        End If

        If String.IsNullOrEmpty(VehicleNumber) Then
            HandleUserMessageLogging("GMRC", "VehicleNumber is null/empty during exit - using fallback")
            VehicleNumber = "UNKNOWN"
        End If
    End Sub

    Private Sub StopKillerThreadForExit(ByVal selectedOption As ExitOption)
        'Called from ExitApp. Sets _killProcesses from the chosen exit option and joins the killer thread.

        If _MyKillerThread IsNot Nothing AndAlso _MyKillerThread.IsAlive Then
            ' Only kill INCA processes if user explicitly chose that option
            Select Case selectedOption
                Case ExitOption.ExitClevirOnly
                    _killProcesses = False  ' ✅ Keep INCA running
                    HandleUserMessageLogging("GMRC", "Killer thread: _killProcesses = False (Exit CLEVIR Only)")
                Case ExitOption.ExitClevirAndCloseInca, ExitOption.ExitClevirCloseIncaShutdownWindows
                    _killProcesses = True   ' ✅ Terminate INCA
                    HandleUserMessageLogging("GMRC", "Killer thread: _killProcesses = True (Close INCA)")
                Case Else
                    ' exitParameter = "Complete" or unknown scenario - default to killing INCA
                    _killProcesses = True
                    HandleUserMessageLogging("GMRC", "Killer thread: _killProcesses = True (Default)")
            End Select

            _MyKillerThread.Join(2000) ' Wait up to 2 seconds
            HandleUserMessageLogging("GMRC", "Stopped killer thread.")
        End If
    End Sub

    Private Sub RunAudioToTextConversionOnExit()
        'Called from ExitApp when AudioToTextConversion is enabled.

        Try
            Dim progressForm As New AudioToTextProgressForm()
            LoginForm.Hide()
            OnVehicleScreen.Hide()

            progressForm.Show()
            Dim conversionTask = progressForm.RunConversionAsync()

            While Not progressForm.IsCompleted
                Application.DoEvents()
                Thread.Sleep(100)
            End While

            conversionTask.Wait(TimeSpan.FromSeconds(5))
            progressForm.Close()
            progressForm.Dispose()
            Thread.Sleep(2000)

            HandleUserMessageLogging("GMRC", "Audio-to-text conversion completed successfully")
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Audio conversion error: {ex.Message}")
        End Try
    End Sub

    Private Sub StopOxtsInterfaceOnExit()
        'Called from ExitApp when MyOxtsInterface exists.

        Try
            MyOxtsInterface.OxtsStopListening()
            HandleUserMessageLogging("GMRC", "OXTS interface stopped")
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Error stopping OXTS: {ex.Message}")
        End Try
    End Sub

    Private Sub StopMeasurementAndCloseInca()
        'Called from ShutdownAndRestartInca.

        'We cant shut down until we transition out of Measure Mode

        If MyIncaInterface.GetMeasurementStatus() = "True" Then
            MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                .GetAwaiter().GetResult()
        End If

        HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: Shutting Down INCA...")

        CheckForExperiment = False
        MyIncaInterface.CloseINCA()
    End Sub

    Private Sub RegisterSignalsAfterIncaInit()
        'Called from ShutdownAndRestartInca after InitINCA (both success and continue-after-error paths).

        RedisplayOnVehicleForm("GroupBox1")

        'Here is where we register the signals from our signal list with INCA using the RCI2 interface (see the GM_INCA_Comm project).

        MyIncaInterface.RegisterSignals()

        HandleUserMessageLogging("GMRC", "ShutdownAndRestartINCA: Initialization Complete")

        OnVehicleScreen.Refresh()
    End Sub

    Private Sub FinishIncaRestart()
        'Called from ShutdownAndRestartInca. Restores normal display/experiment polling after the restart.

        Thread.Sleep(2000)

        GroupBox1.Visible = False

        FormDisplayed = True
        _initializing = False

        If PlaybackMode = False Then
            CheckForExperiment = True
        End If

        Cursor = Cursors.Arrow

        RedisplayOnVehicleForm("Main")

        OnVehicleScreen.TopMost = False

        HandleUserMessageLogging("GMRC", "INCA Restarted.",,)
    End Sub

End Class
