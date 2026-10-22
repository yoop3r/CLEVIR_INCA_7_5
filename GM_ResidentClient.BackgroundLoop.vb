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

    Private Sub HandlePullingDiDs()

        'Called from Handle_5_SecondChecks which is called from MyBackgroundTasks.
        'Handles pulling DIDs if this functionality is enabled...

        Dim MyDidPullFiles As String()
        Static lastFileFound As Boolean
        Dim v As Integer

        Try

            If EnableDIDPull = True Then
                If EnableStartZipFileCheck = True Then

                    MyDidPullFiles = Directory.GetFiles(DefaultVSpyDataDirectory)

                    If UBound(MyDidPullFiles) >= MAX_NUM_DID_PULL_FILES - 1 Then

                        For v = 0 To UBound(MyDidPullFiles)
                            If InStr(MyDidPullFiles(v), DIDPullTriggerZippingKey) > 0 Then
                                lastFileFound = True
                                Exit For
                            End If
                        Next

                        If lastFileFound = True Then
                            lastFileFound = False

                            Do While FileInUse(MyDidPullFiles(v))
                                Thread.Sleep(100)
                            Loop

                            ZipTheDirectory(DefaultVSpyDataDirectory, "Start", BaseDataCollectionPath)

                            For v = 0 To UBound(MyDidPullFiles)
                                If InStr(MyDidPullFiles(v), "m.csv") > 0 Then
                                    File.Delete(MyDidPullFiles(v))
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

                    MyDidPullFiles = Directory.GetFiles(DefaultVSpyDataDirectory)

                    If UBound(MyDidPullFiles) >= MAX_NUM_DID_PULL_FILES - 1 Then

                        For v = 0 To UBound(MyDidPullFiles)
                            If InStr(MyDidPullFiles(v), DIDPullTriggerZippingKey) > 0 Then
                                lastFileFound = True
                                Exit For
                            End If
                        Next

                        If lastFileFound = True Then

                            lastFileFound = False

                            Do While FileInUse(MyDidPullFiles(v))
                                Thread.Sleep(100)
                            Loop

                            ZipTheDirectory(DefaultVSpyDataDirectory, "End", BaseDataCollectionPath)

                            For v = 0 To UBound(MyDidPullFiles)
                                If InStr(MyDidPullFiles(v), "m.csv") > 0 Then
                                    File.Delete(MyDidPullFiles(v))
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
            HandleUserMessageLogging("GMRC", "HandlePullingDIDs - DID Pull Section: " & ex.Message, DisplayMsgBox)
        End Try

    End Sub

    Private Async Function Handle_5_SecondChecks(ByVal lMeasurementStatus As String, ByVal lRecordingState As String) As Task

        'Called from MyBAckgroundTasks every 5 seconds.  Handles stuff which needs to be checked regularly
        'but is not critical to be checked every loop...


        Try

            If CheckForExperiment = True Then

                'If the GetMeasurementStatus returns "Invalid" this indicates that the INCA experiment is no longer
                'running in which case, we will terminate this application.

                'CLEVIR Cannot run without a valid INCA Experiment to latch on to!!!!

                If lMeasurementStatus = "Invalid" Then
                    ' ✅ FIX: Don't set TopMost when LoginForm is active
                    If Not OnLoginScreen Then
                        OnVehicleScreen.TopMost = True
                    End If
                    HandleUserMessageLogging("GMRC", "The INCA Experiment is not running, CLEVIR will now terminate.", DisplayMsgBox)
                    OnVehicleScreen.TopMost = False
                    ExitApp("Complete")

                End If

            End If

            If PlaybackMode = False Then

                Await GetDeviceStatus()

                lIsTargetOnWorkingPage = MyIncaInterface.IsTargetOnWorkingPage

                ' ✅ FIX: Don't manipulate OnVehicleScreen controls when LoginForm is active
                ' RadioButton.Checked can steal focus even without explicit BringToFront()
                If Not OnLoginScreen Then
                    If lIsTargetOnWorkingPage = "True" And OnVehicleScreen.RadioButton1.Checked = False Then
                        OnVehicleScreen.RadioButton1.Checked = True
                        OnVehicleScreen.RadioButton2.Checked = False
                    ElseIf lIsTargetOnWorkingPage = "False" And OnVehicleScreen.RadioButton2.Checked = False Then
                        OnVehicleScreen.RadioButton1.Checked = False
                        OnVehicleScreen.RadioButton2.Checked = True
                    End If
                End If

                'Handle DID pull using vehicle spy (if configured to do so)
                'HandlePullingDiDs()

            End If

            'If we are recording, then we need to check to see if the user changed from reference page
            'to working page, this would indicate that they may be making calibration changes during the
            'recording.  We want to capture this event, so we write an event message into the record file
            'indicating a transition between working page and reference page.

            If lRecordingState = "True" Then

                lIsTargetOnWorkingPage = MyIncaInterface.IsTargetOnWorkingPage

                If lIsTargetOnWorkingPage = "True" And saveIsTargetOnWorkingPage <> "True" Then
                    saveIsTargetOnWorkingPage = "True"
                    MyIncaInterface.WriteEventComment(Format$(DateTime.Now, "HH:mm:ss") & " " & "Detected a Switch to the Working Page")
                    HandleUserMessageLogging("GMRC", "Handle_5_SecondChecks: Detected a Switch to the Working Page")

                ElseIf lIsTargetOnWorkingPage = "False" And saveIsTargetOnWorkingPage <> "False" Then
                    saveIsTargetOnWorkingPage = "False"
                    MyIncaInterface.WriteEventComment(Format$(DateTime.Now, "HH:mm:ss") & " " & "Detected a Switch to the Reference Page")
                    HandleUserMessageLogging("GMRC", "Handle_5_SecondChecks: Detected a Switch to the Reference Page")

                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "Handle_5_SecondChecks: " & ex.Message)
        End Try

    End Function
    ''' <summary>
    ''' Clears data-frozen / lost-device tracking so INCA COMM and frozen-data detection start fresh
    ''' when measurement or recording begins, rather than carrying over state from before the start.
    ''' </summary>
    Private Sub ResetDataFrozenTracking(ByVal reason As String)
        Try
            For i = 0 To myDGs.Count - 1
                If myDGs(i) Is Nothing Then Continue For

                ' Only iterate data rows (x >= 1) because UpdateGridColor uses Rows(x-1)
                Dim lastRow As Integer = Math.Max(1, myDGs(i).RowCount - 1)
                For x = 1 To lastRow
                    For y = 0 To myDGs(i).ColumnCount - 1
                        Dim wasFrozen As Boolean = myDGs(i).DataFrozen(x, y)
                        myDGs(i).DataFrozenCounter(x, y) = 0
                        myDGs(i).DataFrozen(x, y) = False
                        If wasFrozen Then UpdateGridColor(i, x, y, GridUpdateActions.FromLow)
                    Next y
                Next x
            Next i

            _lostDevice = False
            VideoCameraNotUpdating = False
            BackgroundLoopCounterNotUpdating = False
            _MyCpuStaleData = False
            SaveDataFrozenDeviceName = ""
            SaveVideoFrozenDeviceName = ""
            SaveLostDeviceName = ""

            HandleUserMessageLogging("GMRC", "ResetDataFrozenTracking: " & reason & " - data-frozen/lost-device tracking reset")
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ResetDataFrozenTracking: " & ex.Message)
        End Try
    End Sub

    Private Sub HandleNonMeasureModeUpdates(ByVal saveOdoReading As Double, ByRef saveWhereAmIAt As String, ByRef goNoGoFault() As Boolean)
        ' Called from MyBackgroundTasks
        ' Handles updates when transitioning out of measure mode...

        ' Reset location to undefined
        saveWhereAmIAt = "Undefined"

        ' UI updates
        If OnVehicleScreen.Label1.BackColor <> Color.Gray Then
            OnVehicleScreen.Label1.BackColor = Color.Gray
        End If

        If OnVehicleScreen.Label1.Text <> "" Then
            OnVehicleScreen.Label1.Text = ""
        End If

        ' Update Go/NoGo status indicators to gray and reset per-grid state
        For z = 0 To MyDFs.Count - 1
            If MyDFs(z) Is Nothing Then Continue For
            If MyDFs(z).GoNoGoIndex > -1 Then
                ' Gray the banner if needed and clear fault bit
                If MyLabel IsNot Nothing AndAlso MyDFs(z).GoNoGoIndex <= UBound(MyLabel) Then
                    If MyLabel(MyDFs(z).GoNoGoIndex).BackColor <> Color.Gray Then
                        UpdateGonogoLabelColor(MyDFs(z).GoNoGoIndex, Color.Gray)
                    End If
                End If
                goNoGoFault(MyDFs(z).GoNoGoIndex) = False

                ' Reset all grids under this form
                For i = 0 To myDGs.Count - 1
                    If myDGs(i) Is Nothing Then Continue For
                    If myDGs(i).ParentFormIndex <> z Then Continue For

                    ' Only iterate data rows (x >= 1) because UpdateGridColor uses Rows(x-1)
                    Dim lastRow As Integer = Math.Max(1, myDGs(i).RowCount - 1)
                    For x = 1 To lastRow
                        For y = 0 To myDGs(i).ColumnCount - 1
                            myDGs(i).DataFrozenCounter(x, y) = 0
                            myDGs(i).DataFrozen(x, y) = False

                            If myDGs(i).CurrentBackColor(x, y) <> myDGs(i).DefaultCellBackColor(x, y) _
                       OrElse myDGs(i).CurrentForeColor(x, y) <> myDGs(i).DefaultCellForeColor(x, y) Then
                                UpdateGridColor(i, x, y, GridUpdateActions.FromLow)
                            End If
                        Next y
                    Next x
                Next i
            End If
        Next z

        VideoCameraNotUpdating = False
        BackgroundLoopCounterNotUpdating = False

        ' Ensure UI is visible and handle MainTabControl visibility
        If WindowState <> FormWindowState.Minimized Then
            If OnVehicleScreen.Button1.Visible = False Then
                OnVehicleScreen.Button1.Visible = True
            End If

            ' Ensure MainTabControl is visible
            If MyMainTabControl IsNot Nothing AndAlso Not MyMainTabControl.Visible Then
                MyMainTabControl.Visible = True
                HandleUserMessageLogging("GMRC", "HandleNonMeasureModeUpdates: Set MyMainTabControl to visible.")
            End If

            ' Clear Distance to Target label
            If Len(OnVehicleScreen.Label17.Text) > 0 Then
                OnVehicleScreen.Label17.Text = ""
            End If
        End If
    End Sub

    'Private Async Sub MyBackgroundTasks()
    Private Async Function MyBackgroundTasks() As Task
        'This routine handles all the background tasks for the GmResidentClient.  It is responsible for making calls to
        'GM_INCA_Comm (Via the INCA_InterfaceClass) to get data from INCA and display it, it also handles updating timers, counters, display colors
        'status information etc. for the main display as well as for the various dynamically created forms, custom displays and grids.

        Dim oneShot As Boolean
        Dim whereAmI As String = "undefined"
        Dim saveWhereAmIAt As String = "Undefined"
        Dim i, j, k As Integer
        Dim MySaveTime As DateTime
        Dim MyElapseTime As TimeSpan
        Dim MySaveTime2 As DateTime
        Dim MyElapseTime2 As TimeSpan
        Dim MyMeasureTime As TimeSpan
        Dim MySaveMeasureTime As DateTime
        Dim noOfRecords As Integer
        Dim MyRci2Data As IGM_INCA_Comm.INCAData
        Dim lineNum As Integer
        Dim liveDataElapseTime As TimeSpan
        Dim saveLiveDataTime As DateTime
        Dim lTotalnumsignalsdisplayed As Integer
        Dim lMeasurementStatus As String
        Dim lRecordingState As Boolean
        Dim lSaveRecordingState As Boolean
        Dim goNoGoFault() As Boolean
        Dim retries As Integer = 0
        Dim saveOdoReading As Double = 0.0
        Dim logMsgWritten As Boolean

        Dim MySaveTime3 As DateTime
        Dim MyElapseTime3 As TimeSpan

        Const dataFrozenMaxCountMeasurementMode As Integer = 100
        Const dataFrozenMaxCountRecordMode As Integer = 200

        Dim dataFrozenMaxCount As Integer = dataFrozenMaxCountRecordMode

        Const lostDeviceDelayTime As Integer = 20 'seconds

        ' ✅ NEW: Disk space check timer
        Dim MyDiskSpaceCheckTime As DateTime = DateTime.Now
        Const DiskSpaceCheckIntervalMinutes As Integer = 5

        Volatile.Write(_backgroundLoopActive, True)

        Try

            If Debugger.IsAttached Then
                OnVehicleScreen.CheckBox1.Visible = True 'Enable DTC Debug Checkbox on main form...
            End If

            ReDim goNoGoFault(UBound(MyLabel))

            MySaveTime = DateTime.Now
            MySaveTime2 = DateTime.Now
            noOfRecords = 1
            lineNum = -1
            lMeasurementStatus = ""
            lRecordingState = False

            While _enableMyBackgroundTasks AndAlso Not exitInProgress

                whereAmI = "Start of Loop"

                ' ════════════════════════════════════════════════════════════
                ' ✅ NEW: DISK SPACE CHECK (Every 5 minutes)
                ' ════════════════════════════════════════════════════════════
                Dim diskCheckElapsed = DateTime.Now.Subtract(MyDiskSpaceCheckTime)
                If diskCheckElapsed.TotalMinutes >= DiskSpaceCheckIntervalMinutes Then
                    MyDiskSpaceCheckTime = DateTime.Now

                    ' Check disk space on recording path
                    Dim pathToCheck As String = FinalPathToSaveData
                    If Not String.IsNullOrEmpty(pathToCheck) Then
                        CheckDiskSpace(pathToCheck,
                                       minimumSpaceGB:=10.0,
                                       showDialog:=False,
                                       allowOverride:=True)
                    End If
                End If

                If INCACommCheckStopWatch IsNot Nothing Then
                    INCACommCheckStopWatch.Reset()
                    INCACommCheckStopWatch.Start()
                    INCACommCheckWarningTime = Val(APICommErrorMsgDelayTime)
                End If

                'SwitchToMain flag is used in conjunction with INCA Status window (see ProcessKiller)
                ' ✅ FIX: Don't switch forms when LoginForm is active
                If _switchToMain = True AndAlso Not OnLoginScreen Then
                    _switchToMain = False
                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: RedisplayOnVehicleForm(Main) Called...")
                    RedisplayOnVehicleForm("Main")
                    OnVehicleScreen.TopMost = False
                ElseIf _switchToMain = True AndAlso OnLoginScreen Then
                    ' Clear the flag but don't redisplay - LoginForm has priority
                    _switchToMain = False
                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: Skipping RedisplayOnVehicleForm - LoginForm active")
                End If

                'This redimensioning would only be required if a new go/nogo label was to be created at runtime, very unlikely
                'based on current use cases...
                If UBound(MyLabel) > UBound(goNoGoFault) Then
                    ReDim Preserve goNoGoFault(UBound(MyLabel))
                End If

                ' Application.DoEvents() removed - Async/Await already yields UI thread

                If PlaybackMode = False Then

                    whereAmI = "PlaybackMode = False"

                    'Here we are updating the HealthMonitor counter which is monitored by ProcessKiller running in a separate thread.
                    'If HealthMonitor is no longer counting up, it indicates that a periodic call made  to INCA in MyBackgoundtasks
                    'has hung - This circumstance would be handled in ProcessKiller in separate thread...
                    _healthMonitor += 1
                    If _healthMonitor > 30000 Then
                        _healthMonitor = 0
                    End If

                    'Every second (currently) we check INCA Measurement status and INCA Record Status
                    'we use this info to direct logic throughout this background task...
                    MyElapseTime2 = DateTime.Now.Subtract(MySaveTime2)
                    ' Check if enough time has elapsed since last measurement/recording state check
                    If (DateTime.Now - _lastMeasurementCheckTime).TotalSeconds >= MeasurementCheckIntervalSeconds Then
                        _lastMeasurementCheckTime = DateTime.Now

                        whereAmI = "MyIncaInterface.GetMeasurementStatus"
                        lMeasurementStatus = MyIncaInterface.GetMeasurementStatus()

                        If lMeasurementStatus = "True" Then

                            If _inMeasureMode = False Then
                                _inMeasureMode = True
                                MySaveMeasureTime = DateTime.Now
                                MySaveTime = DateTime.Now
                                MySaveTime3 = DateTime.Now

                                dataFrozenMaxCount = dataFrozenMaxCountMeasurementMode
                                ResetDataFrozenTracking("Measurement started")

                            End If

                        ElseIf lMeasurementStatus = "INCA Communication Failure" Or lMeasurementStatus = "GetMeasurementStatus Failure" Then

                            HandleUserMessageLogging("GMRC", "MyBackgroundTasks: L_MeasurementStatus = " & lMeasurementStatus)

                        Else
                            _inMeasureMode = False 'InMeasureMode is also monitored by ProcessKiller thread...
                            oneShot = False 'OneShot flag is passed to HandleAutomaticStartRecord, below... 

                        End If

                        whereAmI = "MyIncaInterface.GetRecordingState"
                        lRecordingState = MyIncaInterface.GetRecordingState()
                        If lSaveRecordingState = False And lRecordingState = True Then
                            dataFrozenMaxCount = dataFrozenMaxCountRecordMode
                            ' Discard counts accumulated during measure-only so INCA COMM/data-frozen
                            ' detection for the recording starts from the moment recording begins.
                            ResetDataFrozenTracking("Recording started")
                            MySaveTime3 = DateTime.Now
                        End If
                        lSaveRecordingState = lRecordingState

                        'This routine handles the case where measurement or record button has been pressed on INCA display
                        'rather than on CLEVIR display...
                        CheckForIncaButtonPresses(lMeasurementStatus, lRecordingState)

                    End If

                    If _inMeasureMode = True And lRecordingState = False Then

                        MyMeasureTime = DateTime.Now.Subtract(MySaveMeasureTime)

                        'Handle case where user has been in meaure mode for 5 minutes without pressing record.  HandleAutomaticStartRecord will
                        'automatically start recording unless user responds to prompt...
                        HandleAutomaticStartRecord(MyMeasureTime, MySaveMeasureTime, oneShot)

                    Else
                        MySaveMeasureTime = DateTime.Now
                    End If

                    'This section below will make periodic calls (every 5 seconds right now) to the Handle_5_SecondChecks routine.  This routine
                    'checks to see if there is an active INCA experiment, if not CLEVIR will terminate, also checks various other statuses and
                    'reacts accordingly...
                    MyElapseTime = DateTime.Now.Subtract(MySaveTime)
                    If MyElapseTime.Seconds >= 5 Then
                        MySaveTime = DateTime.Now

                        Await Handle_5_SecondChecks(lMeasurementStatus, lRecordingState)

                    End If 'If MyElapseTime.Seconds >= 5

                End If

                ' Communication/data alerts only apply once measurement or recording is active.
                If Not (_inMeasureMode OrElse lRecordingState) Then
                    MySaveTime3 = DateTime.Now
                End If

                MyElapseTime3 = DateTime.Now.Subtract(MySaveTime3)
                If MyElapseTime3.Seconds >= lostDeviceDelayTime Then 'was 10 changed to 15 08/30/2020, now 20 sec...
                    'This routine handles the case where communication to processors has been lost or invalid data or invalid video.  Allows user to
                    'reinitialilze CLEVIR and INCA...
                    Await CheckForLostDevice()
                    MySaveTime3 = DateTime.Now
                End If

                'If measurement started = true (or we are playing back CLEVIR recorded data), we make calls to get data, either live or recorded.
                'We also update the display grids by updating the data values for all signals and also go through the logic associated with high threshold and
                'low threshold, etc. to determine background color of each grid cell.

                whereAmI = "Before If MyIncaInterface.MeasurementStarted = True"

                If MyIncaInterface.MeasurementStarted = True Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackRun Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepBack Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackScrolling Or
                   RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepFwd Then

                    whereAmI = "Before HandleRecordingProgressBar"

                    If Visible = True Or RecordPlayback.Visible = True Then
                        'This routine handles the green recording progress bar that is displayed either on the RecordPlayback form
                        'or on the main CLEVIR screen...
                        HandleRecordingProgressBar(RecordPlayback.RecordMode, RecordPlayback.ProgressBar1)
                    Else
                        ' OnVehicleScreen-only mode: hide EXIT button so Label17 (distance-to-object) can occupy that slot
                        If OnVehicleScreen.Button1.Visible Then
                            OnVehicleScreen.Button1.Visible = False
                        End If
                        'This routine handles the green recording progress bar that is displayed either on the RecordPlayback form
                        'or on the main CLEVIR screen...
                        'HandleRecordingProgressBar(MyIncaInterface.Recording, OnVehicleScreen.ProgressBar1)
                    End If

                    '********************************Here is where we are refreshing top down view **************************************

                    whereAmI = "Before If Not MyTdGraphicsContainer.IsDisposed"

                    If Not MyTdGraphicsContainer.IsDisposed Then

                        If MyTdGraphicsContainer.Visible = True Then
                            MyTdGraphicsContainer.Invalidate() 'this triggers paint event on topdown view...

                        End If
                    End If

                    '**********************************************************************************************************

                    whereAmI = "Before WAV Recording"
                    'This routine handles the color of the frame around the microphone to indicate whether a WAV recording
                    'is in progress.  Red, we are not recording WAV, Green we are...
                    'There is a preset amount of time that the WAV recording is enabled, and it needs to shut off after that time.
                    'This routine handles that...
                    HandleWavRecording()

                    If lRecordingState = True Then

                        'This routine handles, among other things, stopping and starting the INCA recording based
                        'on the time interval set...
                        HandleUpdatesWhenRecording()

                    Else 'L_RecordingState = False
                        ' When recording stops, ensure the monitor task is stopped as well.
                        If _isMonitorTaskRunning Then
                            StopRecordingMonitorTask()
                        End If

                    End If

                    'Here we are initializing the data array that will be used to save CLEVIR recorded data if the
                    'CLEVIR data recorder is set to RecordMode.  Not to be confused with recording INCA mf4 data.
                    'This recording creates a spreadsheet that CLEVIR can read back in so that it can play back
                    'data.  This would be used primarily to test CLEVIR or be used for DEMOs.

                    If RecordPlayback.RecordMode = True Then
                        whereAmI = "RecordPlayback.RecordMode = True"
                        k = 0
                        lineNum += 1
                        'ReDim Preserve VariableNameDataArray(UBound(MyIncaInterface.MyDisplaySignals) + 2, lineNum)
                        EnsureRecordingBufferCapacity(lineNum)

                        If lineNum = 0 Then
                            VariableNameDataArray(0, 0) = "Time"
                            VariableNameDataArray(1, 0) = "TimeStamp"
                        Else
                            VariableNameDataArray(0, lineNum) = ""
                            VariableNameDataArray(1, lineNum) = ""
                        End If

                    End If

                    'This section executes if we are processing live data....
                    If RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStop Then
                        If saveLiveDataTime = Nothing Then
                            liveDataElapseTime = DateTime.Now.Subtract(DateTime.Now)
                        Else
                            liveDataElapseTime = DateTime.Now.Subtract(saveLiveDataTime)
                        End If

                        whereAmI = "MyIncaInterface.GetSignalDataWithTime"
                        MySignalDataWithTime = MyIncaInterface.GetSignalDataWithTime() 'here is where we get all live data from INCA...

                        If MySignalDataWithTime IsNot Nothing Then

                            saveLiveDataTime = DateTime.Now

                            For j = 0 To UBound(MySignalDataWithTime)

                                'We can record live data to be played back in CLEVIR, here is where we capture that
                                'live data if we are recording...
                                If RecordPlayback.RecordMode = True And lineNum >= 0 Then
                                    If lineNum = 0 Then
                                        VariableNameDataArray(k + 2, 0) = MyIncaInterface.myDisplaySignals(j).SignalName
                                    Else
                                        If Len(VariableNameDataArray(0, lineNum)) = 0 Then
                                            VariableNameDataArray(0, lineNum) = Format$(liveDataElapseTime.Milliseconds, "0.000")
                                            VariableNameDataArray(1, lineNum) = Format$(MySignalDataWithTime(j).TimeStamp, "0.000")
                                        End If
                                        VariableNameDataArray(k + 2, lineNum) = MySignalDataWithTime(j).SignalData
                                    End If
                                    k += 1
                                End If

                            Next j

                        End If

                    Else 'This else case executes if we are playing back CLEVIR recorded data....

                        whereAmI = "This else case executes if we are playing back recorded data...."

                        ReDim MySignalDataWithTime(0)

                        MyRci2Data = GetRecordedData(UBound(MyIncaInterface.myDisplaySignals) + 2, RecordPlayback.PlaybackMode)

                        For i = 0 To UBound(MyRci2Data.myData)
                            If i > UBound(MySignalDataWithTime) Then
                                ReDim Preserve MySignalDataWithTime(i)
                            End If

                            MySignalDataWithTime(i).SignalData = MyRci2Data.myData(i)
                            MySignalDataWithTime(i).TimeStamp = MyRci2Data.myTime(i)
                        Next i

                    End If

                    'If we have valid data (live or recorded) this section executes...
                    If MySignalDataWithTime IsNot Nothing Then

                        lTotalnumsignalsdisplayed = 0
                        ProcessGrids(saveOdoReading, saveWhereAmIAt, goNoGoFault, dataFrozenMaxCount, EventArgs.Empty, lRecordingState)

                    End If 'Check for MySignalDataWithTime Is Nothing

                    ' Delay with cancellation token for immediate exit response
                    Dim delayMs As Integer = If(lRecordingState, Math.Max(100, Val(_displayRefreshRate)), Val(_displayRefreshRate))
                    Try
                        Await Task.Delay(delayMs, If(_backgroundTasksCts?.Token, CancellationToken.None))
                    Catch ex As TaskCanceledException
                        ' Exit requested - break out of the main loop
                        Exit While
                    End Try

                    'Here is where we save the CLEVIR record file if we transitioned out of RecordPlayBack.RecordMode = True
                    If RecordPlayback.RecordMode = False And lineNum > 0 Then
                        SaveRecordFile(lineNum)
                        lineNum = -1
                    End If

                    If RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepFwd Or
                    RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepBack Then
                        RecordPlayback.GroupBox2.Text = "PLAYBACK (Pause)"
                    End If

                    TotalNumSignalsDisplayed = lTotalnumsignalsdisplayed

                    whereAmI = "Finished in MeasurementStatus = True"

                Else 'Measurement not started and not playing back CLEVIR Recording....

                    HandleNonMeasureModeUpdates(saveOdoReading, saveWhereAmIAt, goNoGoFault)

                    'Save CLEVIR Recording when transitioning out of measure mode if we were recording...
                    If RecordPlayback.RecordMode = True Then
                        If lineNum > 0 Then
                            SaveRecordFile(lineNum)
                        End If
                        RecordPlayback.RecordMode = False
                        lineNum = -1
                    End If
                    Try
                        Await Task.Delay(100, If(_backgroundTasksCts?.Token, CancellationToken.None))
                    Catch ex As TaskCanceledException
                        Exit While
                    End Try
                    ' Application.DoEvents() removed - Task.Delay already yields UI thread

                End If

                retries = 0

            End While

        Catch ex As Exception

            If logMsgWritten = False Then
                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & whereAmI & " - " & ex.Message)
                logMsgWritten = True
            End If

        Finally
            Volatile.Write(_backgroundLoopActive, False)
        End Try

    End Function

    Private Sub ProcessGrids(
                                  ByVal saveOdoReading As Double,
                                  ByRef saveWhereAmIAt As String,
                                  ByRef goNoGoFault() As Boolean,
                                  dataFrozenMaxCount As Integer,
                                  e As EventArgs,
                                  ByVal lRecordingState As Boolean
                                  )

        Dim whereAmI As String = "undefined"
        Dim z As Integer

        Dim lTotalnumsignalsdisplayed As Integer

        Dim logMsgWritten As Boolean

        Dim saveHealthCounter As Integer

        Dim acpDebugOneShot As Boolean

        Try

            If myDGs(0) IsNot Nothing Then

                'Here we will loop through all of the grids on all of the forms...
                For z = 0 To myDGs.Count - 1

                    whereAmI = "Go Thru All Displayed Grids"

                    'need to go thru all grids which are displayed, or are associated with gonogo, or are associated with a custom screen (CS_)....
                    If myDGs(z).Parent.Visible = True Or MyDFs(myDGs(z).ParentFormIndex).AlsoAssociatedWith = "GO/NOGO" Or MyDFs(myDGs(z).ParentFormIndex).AlsoAssociatedWith = "AUTOANNO" Or
                (InStr(MyDFs(myDGs(z).ParentFormIndex).AlsoAssociatedWith, "CS_") > 0) Then

                        whereAmI = "Grid Visible or GO/NOGO or CS_"

                        UpdateGridCustomStatus(z, saveOdoReading, saveWhereAmIAt, lRecordingState)

                        ' ============================================================
                        ' SUSPEND LAYOUT FOR BATCHED GRID UPDATES
                        ' ============================================================
                        myDGs(z).SuspendLayout()
                        Try
                            UpdateGridCells(z, goNoGoFault, dataFrozenMaxCount, lRecordingState, saveHealthCounter, acpDebugOneShot, lTotalnumsignalsdisplayed, whereAmI)
                        Finally
                            ' RESUME LAYOUT - Single redraw at end
                            myDGs(z).ResumeLayout(True)
                        End Try
                        ' ============================================================
                        ' END SUSPEND LAYOUT BLOCK
                        ' ============================================================

                        whereAmI = "Out of X Y Loops - Ready to Refresh Grid..."

                        RefreshGridThrottled(z)
                    End If
                Next z

                whereAmI = "Out of Z Loop..."

                ReconcileGoNoGoLabels(goNoGoFault)
            End If 'Check for MyDGs(0) Is Nothing
        Catch ex As Exception

            If logMsgWritten = False Then
                HandleUserMessageLogging("GMRC", "processGrids: " & whereAmI & " - " & ex.Message)
                logMsgWritten = True
            End If

        End Try
    End Sub

    Private Sub UpdateGridCustomStatus(ByVal z As Integer, ByVal saveOdoReading As Double, ByRef saveWhereAmIAt As String, ByVal lRecordingState As Boolean)
        If lRecordingState = True Then

            If myDGs(z).CS_NAN_STATUS > 0 Then
                HandleNanStatus(myDGs(z))
            End If

            If myDGs(z).CS_WAVRECORD > 0 Then
                If OnVehicleScreen.PictureBox1.BackColor = Color.Red Then
                    Handle_CS_WAVRECORD(myDGs(z))
                End If
            End If

        End If

        'This Routine handles all things related to vehicle mileage and where the vehicle is,
        'on grounds or off grounds, etc. Will not be called unless CS_ODOMETER is defined in signallist...
        If myDGs(z).CS_ODOMETER > 0 Then
            HandleOdometerRelatedStatus(myDGs(z), saveOdoReading, saveWhereAmIAt, lRecordingState)
        End If

        'This Routine handles CLEVIR display of cluster message...
        'Nothing happens unless CS_LCC_CLUSTER_MSG and CS_LCC_BUTTON_PRESS are defined in signal list...
        If myDGs(z).CS_LCC_CLUSTER_MSG > 0 And myDGs(z).CS_LCC_BUTTON_PRESS > 0 Then
            HandleClevirDisplayOfClusterMsgs(myDGs(z))
        End If

        'this next section handles custom displays.  This is where data is put into the
        'labels on the custom displays.  Background colors for labels are updated here based on values
        'of variables associated with the various labels on the custom screens.  Whenever a custom 
        'screen is added, code must be added here to handle updating. Code is only executed if custom

        'ADD CUSTOM SCREEN HANDLING HERE....

        UpdateCustomDisplays(myDGs(z))

        If myDGs(z).CS_CPSTOS_X_POS > 0 And myDGs(z).CS_CPSTOS_Y_POS > 0 Then

            Dim xPos As Double = MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_X_POS, 1)).SignalData
            Dim yPos As Double = MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_CPSTOS_Y_POS, 1)).SignalData
            Dim distText As String = Format$(Math.Abs(Math.Sqrt((xPos * xPos) + (yPos * yPos))), "0.00")
            OnVehicleScreen.Label17.Text = distText

        End If
    End Sub

    Private Sub UpdateGridCells(ByVal z As Integer, ByVal goNoGoFault() As Boolean, ByVal dataFrozenMaxCount As Integer, ByVal lRecordingState As Boolean,
                                ByRef saveHealthCounter As Integer, ByRef acpDebugOneShot As Boolean,
                                ByRef lTotalnumsignalsdisplayed As Integer, ByRef whereAmI As String)
        Dim x As Integer
        Dim y As Integer
        Dim Mytempstr As String

        whereAmI = "Rows and Columns"
        'For each grid, we must go through all configured rows and columns to update data display,
        'background and foreground colors, etc.
        'DataArray may be smaller than NumberOfRows/NumberOfColumns when those values were grown
        'after FinalizeGridDisplays allocated it; capture its real bounds to guard the writes below.
        Dim dataArrayMaxRow As Integer = -1
        Dim dataArrayMaxCol As Integer = -1
        If myDGs(z).DataArray IsNot Nothing Then
            dataArrayMaxRow = myDGs(z).DataArray.GetUpperBound(0)
            dataArrayMaxCol = myDGs(z).DataArray.GetUpperBound(1)
        End If

        ' One-time-per-grid diagnostic: if NumberOfRows/NumberOfColumns grew past the
        ' DataArray allocated in FinalizeGridDisplays, log which grid drifted and by how much.
        If (myDGs(z).NumberOfRows - 2) > dataArrayMaxRow OrElse (myDGs(z).NumberOfColumns - 1) > dataArrayMaxCol Then
            Dim driftKey As String = myDGs(z).ParentFormName & "/" & myDGs(z).Name
            If _dataArraySizeWarned.Add(driftKey) Then
                HandleUserMessageLogging("GMRC", "processGrids: DataArray size drift on '" & driftKey &
                    "' - NumberOfRows=" & myDGs(z).NumberOfRows & " NumberOfColumns=" & myDGs(z).NumberOfColumns &
                    " but DataArray bounds=(" & dataArrayMaxRow & "," & dataArrayMaxCol & "). Display writes past the array are skipped.")
            End If
        End If

        For x = 0 To myDGs(z).NumberOfRows - 1

            For y = 0 To myDGs(z).NumberOfColumns - 1

                Mytempstr = ProcessGridCell(z, x, y, goNoGoFault, dataFrozenMaxCount, lRecordingState, saveHealthCounter, acpDebugOneShot, whereAmI)
                whereAmI = "Build Display Array for Grid"
                If myDGs(z).Parent.Visible = True Then
                    If x > 0 And y >= 1 AndAlso (x - 1) <= dataArrayMaxRow AndAlso y <= dataArrayMaxCol Then
                        myDGs(z).DataArray(x - 1, y) = Mytempstr
                    End If
                    whereAmI = "Total Num Signals Displayed Calculation..."
                    lTotalnumsignalsdisplayed += 1
                End If
            Next y
        Next x
    End Sub

    Private Function ProcessGridCell(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal goNoGoFault() As Boolean, ByVal dataFrozenMaxCount As Integer,
                                     ByVal lRecordingState As Boolean, ByRef saveHealthCounter As Integer,
                                     ByRef acpDebugOneShot As Boolean, ByRef whereAmI As String) As String
        Dim Mytempstr As String
        Dim Mytempval As Double

        Mytempstr = ""
        If myDGs(z).SignalIndex(x, y) >= 0 And
    Len(myDGs(z).VariableName(x, y)) > 0 And
    myDGs(z).Registered(x, y) = True And
    myDGs(z).VariableName(x, y) <> "undefined" Then

            'This section handles putting dashes in all cells if object ID is 0...
            'assumes the "blanker" is the first row that must be "blanked" and all subsequent rows in the associated grid are "blanked"
            If (myDGs(z).ObjectID_Start_Pos <> 0 And
         MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).ObjectID_Start_Pos, y)).SignalData = 0) _
        AndAlso x > myDGs(z).ObjectID_Start_Pos Then

                Mytempstr = "-"
                If myDGs(z).CurrentBackColor(x, y) <> myDGs(z).DefaultCellBackColor(x, y) Then
                    UpdateGridColor(z, x, y, GridUpdateActions.FromLow)
                End If

            Else
                'HealtCounter is maintained in ProcessKiller thread.  A HealthCounter value of 0 indicates
                'a healthy system...
                If _healthCounter = 0 Then

                    If saveHealthCounter > 1 Then
                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: HealthCounter recovered from " & saveHealthCounter)
                    End If
                    saveHealthCounter = 0

                    ApplyAcpDebugSignalOverride(z, x, y, acpDebugOneShot)

                    TrackCellDataFrozenState(z, x, y, goNoGoFault, dataFrozenMaxCount, lRecordingState)

                Else 'HealthCounter > 0 indicates that we have most likely waiting on an INCA API Call...

                    HandleHealthCounterWait(z, x, y, saveHealthCounter)

                End If

                whereAmI = "Set Mytempval"

                Mytempval = MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData

                whereAmI = "Format Display String"

                Mytempstr = GetCellDisplayString(z, x, y, Mytempval)

                'This section handles updating back color, fore color etc. based on settings
                'in excel configuration file

                ApplyCellThresholdColors(z, x, y, Mytempval, goNoGoFault, whereAmI)
            End If
        Else
            Mytempstr = "UnRgstrd"
        End If

        Return Mytempstr
    End Function

    Private Sub ApplyAcpDebugSignalOverride(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByRef acpDebugOneShot As Boolean)
        If Debugger.IsAttached Then

            If OnVehicleScreen.CheckBox1.Checked = True Then
                If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 AndAlso IsConfiguredProcessorDevice(myDGs(z).DeviceName(x, y)) AndAlso String.Equals(myDGs(z).DeviceName(x, y), PrimaryProcessorDeviceName, StringComparison.OrdinalIgnoreCase) Then
                    If acpDebugOneShot = False Then
                        MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = GetRandom(0, 10)
                        acpDebugOneShot = True
                    Else
                        MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                        acpDebugOneShot = False
                    End If
                End If
            Else
                If InStr(myDGs(z).VariableName(x, y), "SsDFIC_FA") > 0 AndAlso IsConfiguredProcessorDevice(myDGs(z).DeviceName(x, y)) AndAlso String.Equals(myDGs(z).DeviceName(x, y), PrimaryProcessorDeviceName, StringComparison.OrdinalIgnoreCase) Then
                    acpDebugOneShot = False
                    MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData = 0
                End If
            End If

        End If
    End Sub

    Private Sub TrackCellDataFrozenState(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal goNoGoFault() As Boolean, ByVal dataFrozenMaxCount As Integer, ByVal lRecordingState As Boolean)
        Dim gridUpdateAction As GridUpdateActions

        'The CheckForDataChange property is associated with the same column in the
        'signal configuration spreadsheet.  This allows us to set up specific signals to watch
        'to see if they are changing.  If they do not change over a certain period of time,
        'this would indicate a communication issue....

        If myDGs(z).CheckForDataChange(x, y) = True And MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex > -1 And
        (myDGs(z).Name <> "INCA Video Status Items" Or (myDGs(z).Name = "INCA Video Status Items" And lRecordingState = True)) Then
            If myDGs(z).SaveLastValue(x, y) = MySignalDataWithTime(myDGs(z).SignalIndex(x, y)).SignalData Then

                If myDGs(z).DataFrozen(x, y) = False Then 'ADDED 04/14/2020
                    myDGs(z).DataFrozenCounter(x, y) = myDGs(z).DataFrozenCounter(x, y) + 1
                End If

                'the number DataFrozenMaxCount is somewhat arbitrary, this should be based on time because we 
                'count up faster when we are waiting less time between cycles through this code
                If myDGs(z).DataFrozenCounter(x, y) > dataFrozenMaxCount And myDGs(z).DataFrozen(x, y) = False Then
                    MarkCellDataFrozen(z, x, y, goNoGoFault, lRecordingState)
                End If
            Else 'Value has changed...

                'If any of the frozen data starts to change, we reset the frozen data counter and frozen data state here...
                If myDGs(z).CheckForDataChange(x, y) = True And MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex > -1 Then
                    MarkCellDataResumed(z, x, y, lRecordingState)
                End If

                If PlaybackMode = False Then CheckForExperiment = True

                If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)) Then
                    gridUpdateAction = GridUpdateActions.FromHigh
                    UpdateGridColor(z, x, y, gridUpdateAction)
                End If

            End If
        End If
    End Sub

    Private Sub MarkCellDataFrozen(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal goNoGoFault() As Boolean, ByVal lRecordingState As Boolean)
        Dim gridUpdateAction As GridUpdateActions

        myDGs(z).DataFrozenCounter(x, y) = 0
        myDGs(z).DataFrozen(x, y) = True

        CheckForExperiment = False

        If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).HighThreshBackColor(x, y)) Then
            gridUpdateAction = GridUpdateActions.ToHigh
            UpdateGridColor(z, x, y, gridUpdateAction)

            If lRecordingState = True Then
                MyIncaInterface.WriteEventComment(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - First Instance Data Frozen", True)
            End If

        Else

            If lRecordingState = True Then
                MyIncaInterface.WriteEventComment(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Not Updating", True)
            End If

        End If

        'Here we set the go/nogo label color to red and save the device names for those
        'devices with frozen data.  These FrozenDeviceNames are used when displaying
        'user message indicating a data or video device issue...

        If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then

            goNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
            UpdateGonogoLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)

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
    End Sub

    Private Sub MarkCellDataResumed(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal lRecordingState As Boolean)
        Dim i As Integer

        myDGs(z).DataFrozenCounter(x, y) = 0

        If myDGs(z).DataFrozen(x, y) = True Then
            myDGs(z).DataFrozen(x, y) = False

            If lRecordingState = True Then
                MyIncaInterface.WriteEventComment(myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Resumed Updating", True)
            Else
                HandleUserMessageLogging("GMRC", myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Resumed Updating")
            End If

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
    End Sub

    Private Sub HandleHealthCounterWait(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByRef saveHealthCounter As Integer)
        'If we are waiting on an INCA API Call, we should reset the frozen counter...
        If _healthCounter > 10 Then myDGs(z).DataFrozenCounter(x, y) = 0
        If PlaybackMode = False Then CheckForExperiment = True

        If saveHealthCounter = 0 Then
            If _healthCounter > 1 Then
                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: HealthCounter = " & _healthCounter)
            End If
        End If

        saveHealthCounter = _healthCounter
    End Sub

    Private Function GetCellDisplayString(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal Mytempval As Double) As String
        Dim Mytempstr As String

        ' Cache formatted string to avoid redundant FormatDisplayString calls
        ' In ProcessGrids - Handle first-time initialization
        If myDGs(z).SaveFormattedString(x, y) Is Nothing OrElse String.IsNullOrEmpty(myDGs(z).SaveFormattedString(x, y)) Then
            ' First time accessing this cell - format and cache
            Mytempstr = FormatDisplayString(Mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y))
            myDGs(z).SaveFormattedString(x, y) = Mytempstr
            myDGs(z).SaveLastValue(x, y) = Mytempval
        ElseIf myDGs(z).SaveLastValue(x, y) = Mytempval Then
            ' Value unchanged - reuse cached formatted string
            Mytempstr = myDGs(z).SaveFormattedString(x, y)
        Else
            ' Value changed - format and cache the new string
            Mytempstr = FormatDisplayString(Mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y))
            myDGs(z).SaveFormattedString(x, y) = Mytempstr

            ' Update SaveLastValue (except for AUTOANNO)
            If myDGs(z).AlsoAssociatedWith(x, y) <> "AUTOANNO" Then
                myDGs(z).SaveLastValue(x, y) = Mytempval
            End If
        End If

        Return Mytempstr
    End Function

    Private Sub ApplyCellThresholdColors(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal Mytempval As Double, ByVal goNoGoFault() As Boolean, ByRef whereAmI As String)
        whereAmI = "Update Grid Colors"

        'Here is where we handle updating grid cell colors based on data values in the variovus cells
        'We also update go/nogo label colors here if necessary.  This is done here for those variables
        'that are not frozen...

        If myDGs(z).DataFrozen(x, y) = False Then

            whereAmI = "HighThresh Check"
            ApplyHighThresholdColor(z, x, y, Mytempval, goNoGoFault)
            whereAmI = "LowThresh Check"
            ApplyLowThresholdColor(z, x, y, Mytempval, goNoGoFault)
            whereAmI = "Reset Check HIGH"
            ApplyHighThresholdReset(z, x, y, Mytempval)
            whereAmI = "Reset Check LOW"
            ApplyLowThresholdReset(z, x, y, Mytempval)
            whereAmI = "Equal To Check"
            ApplyEqualToColor(z, x, y, Mytempval, goNoGoFault)
        End If
    End Sub

    Private Sub ApplyHighThresholdColor(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal Mytempval As Double, ByVal goNoGoFault() As Boolean)
        Dim gridUpdateAction As GridUpdateActions

        If Mytempval > myDGs(z).HighThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
            If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).HighThreshBackColor(x, y)) Then

                If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).HighThresh(x, y),,,,, 1)
                Else
                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).HighThresh(x, y),,,,, 2)
                End If

                gridUpdateAction = GridUpdateActions.ToHigh
                UpdateGridColor(z, x, y, gridUpdateAction)

                If myDGs(z).Name = "DTCs" Then

                    ' Log each unique DTC once per session
                    Dim currentDtc As String = myDGs(z).DeviceName(x, y) & " - " & FormatDisplayString(Mytempval, myDGs(z).DisplayFormat(x, y), myDGs(z).DeviceName(x, y), myDGs(z).VariableName(x, y))
                    If loggedDtcStrings.Add(currentDtc) Then
                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: Active DTC = " & currentDtc)
                    End If

                    ' Set GO/NOGO status to red while DTCs are present
                    If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                        goNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                        UpdateGonogoLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                    End If

                End If
            End If
        End If
    End Sub

    Private Sub ApplyLowThresholdColor(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal Mytempval As Double, ByVal goNoGoFault() As Boolean)
        Dim gridUpdateAction As GridUpdateActions

        If Mytempval < myDGs(z).LowThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
            If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).LowThreshBackColor(x, y)) Then
                If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).LowThresh(x, y),,,,, 1)
                Else
                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).LowThresh(x, y),,,,, 2)
                End If
                gridUpdateAction = GridUpdateActions.ToLow
                UpdateGridColor(z, x, y, gridUpdateAction)
            End If
            If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                goNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                UpdateGonogoLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
            End If

        End If
    End Sub

    Private Sub ApplyHighThresholdReset(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal Mytempval As Double)
        Dim gridUpdateAction As GridUpdateActions

        If Mytempval <= myDGs(z).HighThresh(x, y) And Mytempval >= myDGs(z).LowThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
            If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)) Then
                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).HighThresh(x, y),,,,, 2)
                gridUpdateAction = GridUpdateActions.FromHigh
                UpdateGridColor(z, x, y, gridUpdateAction)

            End If
        End If
    End Sub

    Private Sub ApplyLowThresholdReset(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal Mytempval As Double)
        Dim gridUpdateAction As GridUpdateActions

        If Mytempval >= myDGs(z).LowThresh(x, y) And Mytempval <= myDGs(z).HighThresh(x, y) And Len(myDGs(z).EqualTo(x, y)) = 0 Then
            If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).LowThreshBackColor(x, y)) Then
                HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Threshold Value: " & myDGs(z).LowThresh(x, y),,,,, 2)
                gridUpdateAction = GridUpdateActions.FromLow
                UpdateGridColor(z, x, y, gridUpdateAction)
            End If
        End If
    End Sub

    Private Sub ApplyEqualToColor(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal Mytempval As Double, ByVal goNoGoFault() As Boolean)
        Dim gridUpdateAction As GridUpdateActions

        If Len(myDGs(z).EqualTo(x, y)) > 0 And myDGs(z).CheckForDataChange(x, y) = False Then
            If Mytempval = Val(myDGs(z).EqualTo(x, y)) Then
                If (myDGs(z).CurrentBackColor(x, y) <> myDGs(z).LowThreshBackColor(x, y)) Then
                    If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Equals " & Val(myDGs(z).EqualTo(x, y)),,,,, 1)
                    Else
                        HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Equals " & Val(myDGs(z).EqualTo(x, y)),,,,, 2)
                    End If
                    gridUpdateAction = GridUpdateActions.ToLow
                    UpdateGridColor(z, x, y, gridUpdateAction)
                End If
                If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                    goNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                    UpdateGonogoLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                End If
            Else
                If (myDGs(z).CurrentBackColor(x, y) = myDGs(z).LowThreshBackColor(x, y)) Then
                    HandleUserMessageLogging("GMRC", "MyBackgroundTasks: " & myDGs(z).DeviceName(x, y) & " - " & myDGs(z).VariableName(x, y) & " - Current Value: " & Mytempval & " Not Equal To " & Val(myDGs(z).EqualTo(x, y)),,,,, 2)
                    gridUpdateAction = GridUpdateActions.FromLow
                    UpdateGridColor(z, x, y, gridUpdateAction)
                End If
            End If
        End If
    End Sub

    Private Sub RefreshGridThrottled(ByVal z As Integer)
        ' Throttled refresh using the existing dictionary-based timing
        Dim key = myDGs(z).Name
        Dim nowT = DateTime.UtcNow
        Dim nextAt As DateTime
        If Not _gridRefreshAt.TryGetValue(key, nextAt) OrElse nowT >= nextAt Then
            myDGs(z).Refresh()
            _gridRefreshAt(key) = nowT + GridMinRefresh
        End If
    End Sub

    Private Sub ReconcileGoNoGoLabels(ByVal goNoGoFault() As Boolean)
        Dim z As Integer

        'Because there may be multiple grid cells that can influence go/nogo label colors, we must
        'reconcile what the actual color of the label should be here, after going through all forms
        'grids and cells above.
        For z = 0 To MyDFs.Count - 1
            If MyDFs(z).GoNoGoIndex > -1 Then
                Dim goNoGoIndex As Integer = MyDFs(z).GoNoGoIndex
                Dim reasons As List(Of String) = If(GO_NOGO_DEBUG_VERBOSE, New List(Of String)(), Nothing)
                Dim existingFault As Boolean = goNoGoFault(goNoGoIndex)
                Dim finalFault As Boolean = EvaluateGoNoGoFault(z, existingFault, reasons)

                If finalFault Then
                    UpdateGonogoLabelColor(goNoGoIndex, Color.Red)
                Else
                    UpdateGonogoLabelColor(goNoGoIndex, Color.Green)
                End If

                If GO_NOGO_DEBUG_VERBOSE Then
                    Dim reasonText As String = If(reasons IsNot Nothing AndAlso reasons.Count > 0, String.Join(" | ", reasons), "none")
                    HandleUserMessageLogging("GMRC", $"GO/NOGO TRACE: Form='{MyDFs(z).Name}' Index={goNoGoIndex} ExistingFault={existingFault} FinalFault={finalFault} Reasons={reasonText}")
                End If

                ' Reset transient fault bit each cycle; recomputed each pass from current state
                goNoGoFault(goNoGoIndex) = False
            End If
        Next z
    End Sub

    Private Function EvaluateGoNoGoFault(ByVal formIndex As Integer, ByVal existingFault As Boolean, ByRef reasons As List(Of String)) As Boolean
        Dim hasFrozenGoNoGo As Boolean = False

        For i As Integer = 0 To myDGs.Count - 1
            If myDGs(i).ParentFormIndex <> formIndex Then Continue For

            For x As Integer = 0 To myDGs(i).NumberOfRows - 1
                For y As Integer = 0 To myDGs(i).NumberOfColumns - 1
                    If InStr(myDGs(i).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 AndAlso myDGs(i).DataFrozen(x, y) Then
                        hasFrozenGoNoGo = True
                        If reasons IsNot Nothing Then
                            reasons.Add($"FROZEN {myDGs(i).DeviceName(x, y)}.{myDGs(i).VariableName(x, y)}")
                        End If
                    End If
                Next
            Next
        Next

        Return existingFault OrElse hasFrozenGoNoGo
    End Function

    Private Function GetEnumDescription(
        ByVal MySignalEnums As List(Of SignalEnums),
        ByVal displayValue As Double,
        ByVal variableName As String,
        ByVal deviceName As String
    ) As String
        Return DisplayValueFormatter.GetEnumDescription(MySignalEnums, displayValue, variableName, deviceName)
    End Function

    Private Function FormatDisplayString(ByVal displayValue As Double, ByVal displayFormat As String, ByVal deviceName As String, ByVal variableName As String) As String
        'Called from MyBackgroundTasks; implementation lives in DisplayValueFormatter.vb.
        Return DisplayValueFormatter.FormatDisplayString(displayValue, displayFormat, deviceName, variableName, GlobalSignalEnums)
    End Function

    Private Sub UpdateGonogoLabelColor(ByVal MyGridIndex As Integer, ByVal MyColor As Color)
        ' Called from MyBackgroundTasks routine - updates GO/NOGO label colors on the main screen.
        ' This method respects user suppression flags to prevent flashing after "Ignore All" is pressed.

        Static saveBackColor(0 To 15) As Color

        Try
            ' Initialize array if needed
            If saveBackColor Is Nothing Then
                ReDim saveBackColor(0 To 15)
            End If

            ' Validate index
            If MyGridIndex < 0 OrElse MyGridIndex > UBound(MyLabel) Then
                Return
            End If

            ' ====================================================================================
            ' OPTIMIZATION: Skip UI update if color hasn't changed (reduces UI overhead)
            ' ====================================================================================
            If saveBackColor(MyGridIndex) = MyColor Then
                Return ' Color unchanged - no UI update needed
            End If

            ' ====================================================================================
            ' Core Color Update Logic
            ' ====================================================================================
            MyLabel(MyGridIndex).BackColor = MyColor

            ' ====================================================================================
            ' Handle Special Label-Specific Logic
            ' ====================================================================================
            Select Case MyLabel(MyGridIndex).Text
                Case "INCA COMM Status"
                    HandleIncaCommStatusLabel(MyColor)

                Case "INCA Video Status"
                    HandleIncaVideoStatusLabel(MyGridIndex, MyColor)

                Case "CPU Status"
                    HandleCpuStatusLabel(MyColor)
            End Select

            ' ====================================================================================
            ' Logging: Log color change (we already know it changed - passed early return check)
            ' ====================================================================================
            If MyColor <> Color.Gray AndAlso MyLabel(MyGridIndex).Text <> "EOCM DTCs" Then
                Dim colorName As String = GetColorName(MyColor)
                HandleUserMessageLogging("GMRC",
                    $"UpdateGONOGOLabelColor: {MyLabel(MyGridIndex).Text} GO/NOGO is ----------------- {colorName}")
            End If

            ' Save current color for next comparison
            saveBackColor(MyGridIndex) = MyColor

        Catch ex As Exception
            HandleUpdateGonogoException(ex)
        End Try
    End Sub

    ' ====================================================================================
    ' HELPER METHODS: Extracted for clarity and maintainability
    ' ====================================================================================

    Private Sub HandleIncaCommStatusLabel(ByVal MyColor As Color)
        ' Stale-data state always tracks the label: RED while comms are lost, GREEN once re-established.
        ' The ignore flag only suppresses raising the operator alert.
        If MyColor = Color.Red Then
            If Not IgnoreLostDeviceForThisDrive AndAlso Not OverrideCommFailureInDebugMode Then
                BackgroundLoopCounterNotUpdating = True
            End If
            _MyCpuStaleData = True
        Else
            BackgroundLoopCounterNotUpdating = False
            _MyCpuStaleData = False
        End If
    End Sub

    Private Sub HandleIncaVideoStatusLabel(ByVal MyGridIndex As Integer, ByVal MyColor As Color)
        If Not MyIncaInterface.Recording Then
            ' Not recording - set to gray regardless of input color
            MyLabel(MyGridIndex).BackColor = Color.Gray
        Else
            ' Recording - apply the requested color
            MyLabel(MyGridIndex).BackColor = MyColor

            ' Only update video camera flags if alerts are not being ignored
            If Not IgnoreLostDeviceForThisDrive AndAlso Not IgnoreLostDeviceUntilNextRecordingSession Then
                If MyColor = Color.Red AndAlso Not OverrideCommFailureInDebugMode Then
                    VideoCameraNotUpdating = True
                Else
                    VideoCameraNotUpdating = False
                End If
            End If
        End If
    End Sub

    Private Sub HandleCpuStatusLabel(ByVal MyColor As Color)
        ' Update CPU stale data flag based on color
        _MyCpuStaleData = (MyColor = Color.Red)
    End Sub

    Private Function GetColorName(ByVal color As Color) As String
        Return DisplayValueFormatter.GetColorName(color)
    End Function

    Private Sub HandleUpdateGonogoException(ByVal ex As Exception)
        ' Centralized exception handling for UpdateGonogoLabelColor
        If _continueExecution = False Then
            Dim result As DialogResult
            Using topmostOwner As New Form() With {
                .TopMost = True,
                .Size = New System.Drawing.Size(1, 1),
                .StartPosition = FormStartPosition.CenterScreen,
                .ShowInTaskbar = False,
                .FormBorderStyle = FormBorderStyle.None,
                .Opacity = 0
            }
                topmostOwner.Show()
                result = MessageBox.Show(
                    topmostOwner,
                    $"UpdateGONOGOLabelColor: {ex.Message}",
                    "Update GONOGO Error",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Warning)
            End Using

            If result = DialogResult.Retry Then
                _continueExecution = True
                HandleUserMessageLogging("GMRC", $"UpdateGONOGOLabelColor: {ex.Message}")
            Else
                HandleUserMessageLogging("GMRC", $"UpdateGONOGOLabelColor: {ex.Message} - User selected cancel.")
                ExitApp("Complete")
            End If
        End If
    End Sub

    Private Sub UpdateGridColor(ByVal z As Integer, ByVal x As Integer, ByVal y As Integer, ByVal action As GridUpdateActions)

        'This routine is called from ProcessGrids (which already has SuspendLayout/ResumeLayout wrapping).
        'It handles setting the back color of individual cells based on threshold settings.
        'NOTE: SuspendLayout removed here - parent loop already handles layout suspension.

        Try

            Select Case action
                Case GridUpdateActions.FromHigh, GridUpdateActions.FromLow

                    'MyDGs(z).CellBackColor = MyDGs(z).DefaultCellBackColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    'MyDGs(z).CellForeColor = MyDGs(z).DefaultCellForeColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    myDGs(z).CurrentBackColor(x, y) = myDGs(z).DefaultCellBackColor(x, y)
                    myDGs(z).CurrentForeColor(x, y) = myDGs(z).DefaultCellForeColor(x, y)

                    myDGs(z).Rows(x - 1).Cells(y).Style.BackColor = myDGs(z).DefaultCellBackColor(x, y)
                    myDGs(z).Rows(x - 1).Cells(y).Style.ForeColor = myDGs(z).DefaultCellForeColor(x, y)


                Case GridUpdateActions.ToHigh

                    'MyDGs(z).CellBackColor = MyDGs(z).HighThreshBackColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    'MyDGs(z).CellForeColor = MyDGs(z).HighThreshForeColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    myDGs(z).CurrentBackColor(x, y) = myDGs(z).HighThreshBackColor(x, y)
                    myDGs(z).CurrentForeColor(x, y) = myDGs(z).HighThreshForeColor(x, y)

                    myDGs(z).Rows(x - 1).Cells(y).Style.BackColor = myDGs(z).HighThreshBackColor(x, y)
                    myDGs(z).Rows(x - 1).Cells(y).Style.ForeColor = myDGs(z).HighThreshForeColor(x, y)

                Case GridUpdateActions.ToLow

                    'MyDGs(z).CellBackColor = MyDGs(z).LowThreshBackColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    'MyDGs(z).CellForeColor = MyDGs(z).LowThreshForeColor(x, y) 'REMOVED FOR 64BIT COMPILE
                    myDGs(z).CurrentBackColor(x, y) = myDGs(z).LowThreshBackColor(x, y)
                    myDGs(z).CurrentForeColor(x, y) = myDGs(z).LowThreshForeColor(x, y)

                    myDGs(z).Rows(x - 1).Cells(y).Style.BackColor = myDGs(z).LowThreshBackColor(x, y)
                    myDGs(z).Rows(x - 1).Cells(y).Style.ForeColor = myDGs(z).LowThreshForeColor(x, y)

            End Select

        Catch ex As Exception

            If _continueExecution = False Then
                Dim result As DialogResult
                Using topmostOwner As New Form() With {
                    .TopMost = True,
                    .Size = New System.Drawing.Size(1, 1),
                    .StartPosition = FormStartPosition.CenterScreen,
                    .ShowInTaskbar = False,
                    .FormBorderStyle = FormBorderStyle.None,
                    .Opacity = 0
                }
                    topmostOwner.Show()
                    result = MessageBox.Show(
                        topmostOwner,
                        "UpdateGridColor: " & ex.Message,
                        "Grid Update Error",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Warning)
                End Using
                If result = DialogResult.Retry Then
                    _continueExecution = True
                    HandleUserMessageLogging("GMRC", "UpdateGridColor: " & ex.Message)
                Else
                    HandleUserMessageLogging("GMRC", "UpdateGridColor: " & ex.Message & " User selected cancel.")
                    ExitApp("Complete")
                End If
            End If
        End Try

    End Sub

    'Private Sub BackgroundWorker1_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles BackgroundWorker1.DoWork

    '    'the background worker handles the main execution loop

    '    _backgroundTasks = New BackgroundTasks(AddressOf MyBackgroundTasks)
    '    BeginInvoke(_backgroundTasks)

    'End Sub

    Private Sub BackgroundWorker1_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        ' Check for cancellation before starting the task
        If BackgroundWorker1.CancellationPending Then
            e.Cancel = True
            Return
        End If

        ' Fire-and-forget async execution on UI thread with cancellation check
        BeginInvoke(New Action(Async Sub()
                                   Try
                                       ' Run the background tasks
                                       Await MyBackgroundTasks()
                                   Catch ex As Exception
                                       ' Log or handle exceptions if needed
                                       If TypeOf ex Is TaskCanceledException Then
                                           e.Cancel = True
                                           HandleUserMessageLogging("GMRC", "Background task was canceled.")
                                       Else
                                           HandleUserMessageLogging("GMRC", "Background task error: " & ex.Message)
                                       End If
                                   End Try
                               End Sub))
    End Sub

End Class
