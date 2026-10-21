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
Imports System.Windows.Forms.DataVisualization.Charting
Imports System.Xml
Imports SharpPcap

Partial Public Class GmResidentClient
    ''' <summary>
    ''' Ensures the recording buffer can accommodate the specified index.
    ''' Uses exponential growth (2x) to minimize reallocation overhead.
    ''' Thread-safe for concurrent recording operations.
    ''' </summary>
    Private Sub EnsureRecordingBufferCapacity(requiredIndex As Integer)
        ' Validate input
        If requiredIndex < 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(requiredIndex), "Index cannot be negative")
        End If

        ' Early exit if capacity is sufficient (no lock needed for read check)
        If VariableNameDataArray IsNot Nothing AndAlso UBound(VariableNameDataArray, 2) >= requiredIndex Then
            Return
        End If

        SyncLock _recordingBufferLock
            ' Double-check after acquiring lock (another thread may have resized)
            If VariableNameDataArray IsNot Nothing AndAlso UBound(VariableNameDataArray, 2) >= requiredIndex Then
                Return
            End If

            Try
                ' ✅ FIX: Safe null check on myDisplaySignals
                If MyIncaInterface Is Nothing OrElse MyIncaInterface.myDisplaySignals Is Nothing Then
                    HandleUserMessageLogging("GMRC", "EnsureRecordingBufferCapacity: myDisplaySignals not initialized")
                    Return
                End If

                Dim cols As Integer = UBound(MyIncaInterface.myDisplaySignals) + 2

                If VariableNameDataArray Is Nothing Then
                    ' Initial allocation
                    Dim initialCapacity As Integer = Math.Max(requiredIndex + 1, RecordingCapacityStep)
                    ReDim VariableNameDataArray(cols - 1, initialCapacity - 1)  ' ✅ FIX: Use 0-based indices
                    HandleUserMessageLogging("GMRC", $"Recording buffer initialized: {cols} columns × {initialCapacity} rows")
                Else
                    ' ✅ FIX: Only resize second dimension to preserve data
                    Dim currentRows As Integer = UBound(VariableNameDataArray, 2) + 1
                    Dim currentCols As Integer = UBound(VariableNameDataArray, 1) + 1

                    ' ✅ OPTIMIZATION: Exponential growth (2x) instead of linear
                    Dim newCapacity As Integer = currentRows * 2
                    If newCapacity < requiredIndex + 1 Then
                        newCapacity = requiredIndex + 1
                    End If

                    ' ✅ CRITICAL: Create new array and manually copy (ReDim Preserve doesn't work for first dimension)
                    If currentCols <> cols Then
                        ' Column count changed (signal list changed) - need full re-allocation
                        Dim newArray(cols - 1, newCapacity - 1) As String

                        ' Copy existing data
                        Dim copyRows As Integer = Math.Min(currentRows, newCapacity)
                        Dim copyCols As Integer = Math.Min(currentCols, cols)
                        For row As Integer = 0 To copyRows - 1
                            For col As Integer = 0 To copyCols - 1
                                newArray(col, row) = VariableNameDataArray(col, row)
                            Next
                        Next

                        VariableNameDataArray = newArray
                        HandleUserMessageLogging("GMRC", $"Recording buffer resized: {cols} columns × {newCapacity} rows (columns changed)")
                    Else
                        ' Only row count increased - can use ReDim Preserve
                        ReDim Preserve VariableNameDataArray(cols - 1, newCapacity - 1)
                        HandleUserMessageLogging("GMRC", $"Recording buffer expanded: {currentRows} → {newCapacity} rows (2x growth)")
                    End If
                End If

            Catch ex As OutOfMemoryException
                HandleUserMessageLogging("GMRC", $"EnsureRecordingBufferCapacity: Out of memory at {requiredIndex} rows - {ex.Message}", DisplayMsgBox)
                Throw
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"EnsureRecordingBufferCapacity: {ex.Message}")
                Throw
            End Try
        End SyncLock
    End Sub

    Public Function GetPredictedRecordingFilename() As String
        ' Returns the next/predicted recording filename that INCA will use for the next sequence.
        ' Uses existing helpers where available: GetCurrentRecordingInfo() (preferred) or MyIncaInterface.GetLastRecordingFileName().
        ' Falls back to SaveRecordingFileName if nothing else is available.
        Try
            ' Try the high-fidelity helper if it exists
            Try
                Dim recordingInfo = GetCurrentRecordingInfo()
                ' recordingInfo is a value tuple (BaseName As String, Sequence As Integer)
                If Not String.IsNullOrEmpty(recordingInfo.BaseName) Then
                    Dim baseFileName As String = recordingInfo.BaseName
                    Dim seq As Integer = Convert.ToInt32(recordingInfo.Sequence)
                    ' Predicted "next" sequence filename (INCA exposes the "current" sequence as the sequence to be written next)
                    Return String.Format("{0}_{1:D2}.mf4", baseFileName, seq)
                End If
            Catch exRecInfo As Exception
                ' If GetCurrentRecordingInfo isn't available or fails, we'll fallback to other APIs below.
                HandleUserMessageLogging("GMRC", $"GetCurrentRecordingInfo failed, falling back: {exRecInfo.Message}")
            End Try

            ' Fallback: Ask INCA for the last completed recording file, then increment its sequence.
            Dim lastCompleted As String = ""
            Try
                lastCompleted = MyIncaInterface.GetLastRecordingFileName()
            Catch exLastRec As Exception
                HandleUserMessageLogging("GMRC", $"GetLastRecordingFileName failed: {exLastRec.Message}")
                lastCompleted = String.Empty
            End Try

            If Not String.IsNullOrEmpty(lastCompleted) Then
                ' Attempt to derive base + next sequence from last completed name: e.g. BASE_01.mf4 -> next BASE_02.mf4
                Dim nameNoExt As String = Path.GetFileNameWithoutExtension(lastCompleted)
                Dim ext As String = Path.GetExtension(lastCompleted)
                Dim idxUnder As Integer = nameNoExt.LastIndexOf("_"c)
                If idxUnder > 0 Then
                    Dim baseName As String = nameNoExt.Substring(0, idxUnder)
                    Dim seqStr As String = nameNoExt.Substring(idxUnder + 1)
                    Dim seqNum As Integer
                    If Integer.TryParse(seqStr, seqNum) Then
                        Return String.Format("{0}_{1:D2}{2}", baseName, seqNum + 1, ext)
                    End If
                End If
            End If

            ' Absolute fallback: use whatever SaveRecordingFileName currently holds, or label unknown
            If Not String.IsNullOrEmpty(SaveRecordingFileName) Then Return SaveRecordingFileName
            Return "UNKNOWN_RECORDING_FILENAME.mf4"
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GetPredictedRecordingFilename: " & ex.Message)
            Return If(Not String.IsNullOrEmpty(SaveRecordingFileName), SaveRecordingFileName, "UNKNOWN_RECORDING_FILENAME.mf4")
        End Try
    End Function

    Private Sub EnsureSetLastRecordingFileName(predictedFileName As String)
        ' Calls SetLastRecordingFileName in a safe manner to keep MDA shortcuts in sync.
        Try
            If String.IsNullOrEmpty(predictedFileName) Then Return
            ' SetLastRecordingFileName is expected to be defined elsewhere (global/module). Call it to keep external shortcuts current.
            SetLastRecordingFileName(predictedFileName)
            HandleUserMessageLogging("GMRC", "EnsureSetLastRecordingFileName: Set last recording filename to " & predictedFileName)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "EnsureSetLastRecordingFileName: " & ex.Message)
        End Try
    End Sub

    Private Sub SetLastRecordingFileName(ByVal filename As String)
        ' Best-effort helper to keep the application's idea of the "last recording filename" in sync.
        ' This updates SaveRecordingFileName and will attempt to call any INCA helper method if available.
        Try
            If String.IsNullOrEmpty(filename) Then Return

            ' Update local/global copy
            SaveRecordingFileName = filename

            ' Try to call a strongly-typed API on MyIncaInterface if it exists.
            If MyIncaInterface IsNot Nothing Then
                Try
                    If MyIncaInterface.SetLastRecordingFileName(filename) Then
                        HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: Set via MyIncaInterface to " & filename)
                        Return
                    End If
                Catch ex As Exception
                    ' Log and fall back to comm object
                    HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: MyIncaInterface.SetLastRecordingFileName failed: " & ex.Message)
                End Try

                ' Try the nested MyGmIncaComm object (some versions may expose the setter there)
                Try
                    If MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                        MyIncaInterface.MyGmIncaComm.SetLastRecordingFileName(filename)
                        HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: Set via MyIncaInterface.MyGmIncaComm to " & filename)
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: MyGmIncaComm invocation failed: " & ex.Message)
                End Try
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "SetLastRecordingFileName: " & ex.Message)
        End Try
    End Sub

    Private Sub HandleAutomaticStartRecord(ByVal MyMeasureTime As TimeSpan, ByRef MySaveMeasureTime As DateTime, ByRef oneShot As Boolean)

        'Called from MyBackgroundTasks...

        'if 5 minutes have passed while in measure mode and record button has not been pressed, we will display a message indicating
        'that recording will start in 10 seconds.  User may click CANCEL and stay in measuremode, if they do not cancel within 10
        'seconds, we automatically go into record mode.  This was added because some were running in measure mode, thinking they were
        'actually recording...

        Dim e As EventArgs = EventArgs.Empty

        Try

            If MyMeasureTime.Minutes >= 5 And oneShot = False Then

                oneShot = True

                RecordTransitionDelay = 0
                UserStatusInfo.Button1.Visible = True

                Do While RecordTransitionDelay >= 0 And RecordTransitionDelay <= 10
                    HandleUserMessageLogging("GMRC", "Recording will start in " & 10 - RecordTransitionDelay & " seconds unless CANCEL is pressed...",,, FlashMsgOn)
                    RecordTransitionDelay += 1
                    Thread.Sleep(1000)
                    Application.DoEvents()
                Loop

                If RecordTransitionDelay >= 10 Then
                    HandleUserMessageLogging("GMRC", "Starting Recording NOW...",,, FlashMsgOn)
                    OnVehicleScreen.Button14_Click(OnVehicleScreen.Button14, e)
                End If

                MySaveMeasureTime = DateTime.Now
                MyMeasureTime = DateTime.Now.Subtract(MySaveMeasureTime)
                RecordTransitionDelay = 0
                UserStatusInfo.Close()
                UserStatusInfo.Button1.Visible = False

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleAutomaticStartRecord: " & ex.Message)
        End Try
    End Sub

    Private Sub CheckForIncaButtonPresses(ByVal lMeasurementStatus As String, ByVal lRecordingState As String)
        Try
            If lMeasurementStatus = "True" And MyIncaInterface.MeasurementStarted = False Then
                ' ✅ FIX: Don't redisplay when LoginForm is active
                If Not OnLoginScreen Then
                    RedisplayOnVehicleForm("Main")
                End If
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Measurement/Recording Started in INCA - CLEVIR Will now Stop Measurement and/or Recording.", DisplayMsgBox)
                OnVehicleScreen.TopMost = False

                ' ✅ NEW: Log before blocking call
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Calling StartStopMeasurement (blocking)...")

                ' ✅ KEEP THIS (For now)
                MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                .GetAwaiter().GetResult()

                ' ✅ NEW: Log after completion
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: StartStopMeasurement completed.")

                InSession = False

            ElseIf lMeasurementStatus = "False" And MyIncaInterface.MeasurementStarted = True Then
                ' Handle measurement stopped in INCA
                ' ✅ FIX: Don't redisplay when LoginForm is active
                If Not OnLoginScreen Then
                    RedisplayOnVehicleForm("Main")
                End If
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Measurement Stopped in INCA - Please DO NOT USE INCA to Stop Measurement to avoid unpredictable behavior.", DisplayMsgBox)
                OnVehicleScreen.TopMost = False

                OnVehicleScreen.Button1.Enabled = True
                If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
                    OnVehicleScreen.Button4.Enabled = True
                    MyLogin.Enabled = True
                End If

                MyIncaInterface.Recording = False
                MyIncaInterface.MeasurementStarted = False
                MeasurementStarted = False
                MyIncaInterface.StopDataCollection()
                OnVehicleScreen.Button6.Text = "START MEASUREMENT"
                OnVehicleScreen.Button14.Text = "START RECORD"

                OnVehicleScreen.Button14.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button14.ForeColor = Color.Black
                OnVehicleScreen.Button6.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button6.ForeColor = Color.Black

                InSession = False

            ElseIf lRecordingState = True And MyIncaInterface.Recording = False Then

                RedisplayOnVehicleForm("Main")
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Recording Started in INCA - USE INCA NOW to STOP Recording.", DisplayMsgBox, )
                OnVehicleScreen.TopMost = False

                Do While MyIncaInterface.GetRecordingState = True
                    ActiveIncaApiCall = "Waiting For INCA to Report RecordingState False"
                    Thread.Sleep(100)
                Loop

                ActiveIncaApiCall = String.Empty

                MyIncaInterface.Recording = False
                MyIncaInterface.MeasurementStarted = False
                MeasurementStarted = False
                InSession = False

                OnVehicleScreen.Button1.Enabled = True
                If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
                    OnVehicleScreen.Button4.Enabled = True
                    MyLogin.Enabled = True
                End If

                OnVehicleScreen.Button6.Text = "START MEASUREMENT"
                OnVehicleScreen.Button14.Text = "START RECORD"

                OnVehicleScreen.Button14.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button14.ForeColor = Color.Black
                OnVehicleScreen.Button6.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button6.ForeColor = Color.Black

                'MyIncaInterface.MeasurementStarted = True
                'MyIncaInterface.Recording = True
                'MyIncaInterface.StartDataCollection(DataCollectionRate)
                'OnVehicleScreen.Button6.Text = "STOP MEASUREMENT"
                'OnVehicleScreen.Button6.BackColor = Color.Blue
                'OnVehicleScreen.Button6.ForeColor = Color.White
                'OnVehicleScreen.Button14.Text = "STOP RECORD"
                'OnVehicleScreen.Button14.BackColor = Color.Red
                'OnVehicleScreen.Button14.ForeColor = Color.White

                If RecorderStopWatch Is Nothing Then
                    RecorderStopWatch = New Stopwatch()
                End If

                RecorderStopWatch.Reset()
                RecorderStopWatch.Start()

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

                If LmfrStatusDisplayGlobalA.Visible = True Then
                    LmfrStatusDisplayGlobalA.Button1.Enabled = True
                    LmfrStatusDisplayGlobalA.Refresh()
                End If

                If LmfrStatusScreenHc.Visible = True Then
                    LmfrStatusScreenHc.Button1.Enabled = True
                    LmfrStatusScreenHc.Refresh()
                End If

            ElseIf lRecordingState = False And MyIncaInterface.Recording = True Then

                'HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Recording Stopped in INCA")
                RedisplayOnVehicleForm("Main")
                HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Recording Stopped in INCA - Please Use CLEVIR to Start and Stop Recording to avoid unpredictable behavior.", DisplayMsgBox, )
                OnVehicleScreen.TopMost = False

                MyIncaInterface.Recording = False
                MyIncaInterface.MeasurementStarted = False
                MeasurementStarted = False
                InSession = False
                OnVehicleScreen.Button14.Text = "START RECORD"
                OnVehicleScreen.Button6.Text = "START MEASUREMENT"
                OnVehicleScreen.Button14.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button14.ForeColor = Color.Black
                OnVehicleScreen.Button6.BackColor = Color.WhiteSmoke
                OnVehicleScreen.Button6.ForeColor = Color.Black

                ' ✅ Start background OXTS monitor (non-blocking)
                If OxtsEnabled AndAlso MyOxtsInterface IsNot Nothing Then
                    MonitorOxtsRtkStatus()
                End If

                If RecorderStopWatch Is Nothing Then
                    RecorderStopWatch = New Stopwatch()
                End If
                RecorderStopWatch?.Stop()   ' ← Preserve final duration for logging
                RecorderStopWatch.Reset()

                If MyIncaInterface IsNot Nothing Then
                    Try
                        Dim lastClosed As String = MyIncaInterface.GetLastRecordingFileName()
                        If Not String.IsNullOrEmpty(lastClosed) Then
                            'EnsureSetLastRecordingFileName(lastClosed)
                            HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Finalized last recording filename to " & lastClosed)
                        End If
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: Couldn't finalize last recording filename: " & ex.Message)
                    End Try
                End If

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

                If LmfrStatusDisplayGlobalA.Visible = True Then
                    LmfrStatusDisplayGlobalA.Button1.Enabled = False
                    LmfrStatusDisplayGlobalA.Refresh()
                End If

                If LmfrStatusScreenHc.Visible = True Then
                    LmfrStatusScreenHc.Button1.Enabled = False
                    LmfrStatusScreenHc.Refresh()
                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckForINCAButtonPresses: " & ex.Message)
        End Try
    End Sub

    Public Sub HandleWavRecording(Optional ByVal buttonPressed As Boolean = False)

        ' This routine handles the color of the frame around the microphone to indicate whether or not a WAV recording
        ' is in progress. Red: not recording; Green: recording.
        ' Resets WAV recording duration whenever a button is pressed.

        Static recordWavElapseTime As TimeSpan
        Static saveRecordWavTime As DateTime

        Try

            If IsNumeric(RecordWAVTime) Then

                ' Reset the timer and recording state if a button is pressed
                If buttonPressed Then
                    Console.WriteLine("Button Pressed is True")
                    saveRecordWavTime = DateTime.Now
                    HandleUserMessageLogging("GMRC", "HandleWAVRecording: Reset Record WAV Time due to button press.")

                    ' Ensure the microphone UI and recording state are updated
                    If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                        OnVehicleScreen.PictureBox1.BackColor = Color.Green
                        OnVehicleScreen.PictureBox1.Image = My.Resources.Resources.mic_50_Green()
                    End If

                    'StartWAVRecord() ' Reinitiate recording if necessary
                End If

                ' Check if recording is active and duration has elapsed
                If (OnVehicleScreen.PictureBox1.BackColor = Color.Green) And Val(RecordWAVTime) > 0 Then
                    recordWavElapseTime = DateTime.Now.Subtract(saveRecordWavTime)

                    If recordWavElapseTime.TotalSeconds > Val(RecordWAVTime) Then
                        saveRecordWavTime = DateTime.Now
                        HandleUserMessageLogging("GMRC", "HandleWAVRecording: Record WAV Time has expired.")

                        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                            OnVehicleScreen.PictureBox1.BackColor = Color.Red
                            OnVehicleScreen.PictureBox1.Image = My.Resources.Resources.mic_50_Red()
                        End If

                        ' The filename to stop is the one that was stored when the recording started.
                        ' This ensures we are stopping the correct file.
                        Dim filenameToStop As String = If(Not String.IsNullOrEmpty(CurrentWAVFilename), CurrentWAVFilename, audioFilePath)

                        ' Call the refactored StopWAVRecord. It no longer requires the sequence number
                        ' as it correctly logs the stop event using the sequence from when the recording was initiated.
                        StopWAVRecord(filenameToStop)
                    End If
                End If
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleWAVRecording: " & ex.Message)
        End Try

    End Sub

    ''' <summary>
    ''' Handles recording state updates when actively recording.
    ''' Manages: duration-based file rotation, filename tracking, 
    ''' alternate recorder monitoring, and UI updates.
    ''' </summary>
    ''' <remarks>
    ''' Called from MyBackgroundTasks loop only when Recording=True.
    ''' Delegates UI updates to async StartRecordingMonitorTask.
    ''' </remarks>
    Private Sub HandleUpdatesWhenRecording()
        ' Static variables for alternate recorder monitoring
        Static MyAltRcrdElapseTime As TimeSpan
        Static MyAltRcrdSaveTime As DateTime
        Static MyCanAlyzerRcrdElapseTime As TimeSpan
        Static MyCanAlyzerRcrdSaveTime As DateTime

        ' Local working variables
        Dim durationMinutes As Integer

        ' Start the dedicated UI update task if it's not already running.
        If Not _isMonitorTaskRunning Then
            StartRecordingMonitorTask()
        End If

        Try
            ' ==================================================================
            ' SECTION 1: Inform INCA Comm about duration (only if changed)
            ' ==================================================================
            If RecordFileDurationMinutes <> _lastSetRecordFileDurationMinutes Then
                Try
                    If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                        MyIncaInterface.MyGmIncaComm.SetRecordingFileDurationMinutes(RecordFileDurationMinutes)
                        _lastSetRecordFileDurationMinutes = RecordFileDurationMinutes
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", "HandleUpdatesWhenRecording: SetRecordingFileDurationMinutes failed: " & ex.Message)
                End Try
            End If

            ' ==================================================================
            ' SECTION 2: **OPTIMIZED FILENAME CACHING WITH SEQUENCE NUMBER**
            ' ==================================================================
            ' Skipped while a sequence rotation is in flight. StopAndStartRecordingAsync
            ' awaits between the stop and the start, so this loop keeps running during
            ' the window where INCA has closed sequence N but not yet opened N+1.
            ' GetActualRecordingTimeMs/GetCurrentRecordingInfo are unreliable in that
            ' window, and caching a wrong value here would mis-stamp the sequence number
            ' written into LiDAR/OXTS event markers - a silent, post-process-only
            ' failure. StartRecordingAsync repopulates the cache authoritatively once
            ' the rotation completes.
            If OnVehicleScreen.Label5.Visible = True AndAlso
               Not (MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.SequenceRotationInProgress) Then
                Dim shouldUpdateFilename As Boolean = False

                ' ──────────────────────────────────────────────────────────────
                ' CHECK 1: Is cached filename empty/invalid?
                ' ──────────────────────────────────────────────────────────────
                If String.IsNullOrEmpty(CachedRecordingFilename) OrElse
       CachedRecordingFilename = "[Pending...]" Then
                    shouldUpdateFilename = True
                End If

                ' ──────────────────────────────────────────────────────────────
                ' CHECK 2: Has file rotated? (recording time decreased = new file)
                ' ──────────────────────────────────────────────────────────────
                If Not shouldUpdateFilename Then
                    Try
                        Dim currentTimeMs As Integer = MyIncaInterface.GetActualRecordingTimeMs()

                        ' Rotation detected if current time < last known time
                        If currentTimeMs < LastKnownRecordingTimeMs Then
                            shouldUpdateFilename = True
                            HandleUserMessageLogging("GMRC",
                    $"HandleUpdatesWhenRecording: File rotation detected (time: {currentTimeMs}ms < {LastKnownRecordingTimeMs}ms)")
                        End If

                        LastKnownRecordingTimeMs = currentTimeMs

                    Catch ex As Exception
                        ' If time query fails, use periodic refresh as fallback
                        If DateTime.Now.Subtract(LastFilenameUpdateTime).TotalSeconds > 5 Then
                            shouldUpdateFilename = True
                        End If
                    End Try
                End If

                ' ──────────────────────────────────────────────────────────────
                ' CHECK 3: Periodic safety refresh (every 10 seconds)
                ' ──────────────────────────────────────────────────────────────
                If Not shouldUpdateFilename Then
                    If DateTime.Now.Subtract(LastFilenameUpdateTime).TotalSeconds > 10 Then
                        shouldUpdateFilename = True
                    End If
                End If

                ' ══════════════════════════════════════════════════════════════
                ' PERFORM UPDATE ONLY WHEN NECESSARY
                ' ══════════════════════════════════════════════════════════════
                If shouldUpdateFilename Then
                    Try
                        Dim recordingInfo = GetCurrentRecordingInfo()

                        If Not String.IsNullOrEmpty(recordingInfo.BaseName) Then
                            Dim fullFilename As String = String.Format("{0}_{1:D2}.mf4",
                                                                       recordingInfo.BaseName,
                                                                       recordingInfo.Sequence)

                            ' ✅ CRITICAL: Normalize both strings before comparison
                            Dim normalizedFull As String = NormalizeFilename(fullFilename)
                            Dim normalizedCached As String = NormalizeFilename(CachedRecordingFilename)

                            ' Now compare the NORMALIZED strings
                            'If normalizedFull <> normalizedCached Then
                            If Not String.Equals(normalizedFull, normalizedCached, StringComparison.Ordinal) Then
                                CachedRecordingFilename = normalizedFull
                                OnVehicleScreen.Label5.Text = $"Recording Filename: {CachedRecordingFilename}"
                                SaveRecordingFileName = CachedRecordingFilename
                                LastFilenameUpdateTime = DateTime.Now
                                HandleUserMessageLogging("GMRC", $"Updated to sequence {recordingInfo.Sequence:D2}: {CachedRecordingFilename}")
                            End If
                        Else
                            ' Fallback to prediction...
                            Dim predicted As String = GetPredictedRecordingFilename()
                            If Not String.IsNullOrEmpty(predicted) AndAlso NormalizeFilename(predicted) <> NormalizeFilename(CachedRecordingFilename) Then
                                CachedRecordingFilename = NormalizeFilename(predicted)
                                OnVehicleScreen.Label5.Text = $"Recording Filename: {CachedRecordingFilename}"
                                SaveRecordingFileName = CachedRecordingFilename
                                LastFilenameUpdateTime = DateTime.Now
                            End If
                        End If

                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC",
                                                 $"HandleUpdatesWhenRecording: Filename update error - {ex.Message}")
                    End Try
                End If
            End If

            ' ==================================================================
            ' SECTION 3: Handle duration-based rotation
            ' ==================================================================
            durationMinutes = RecordFileDurationMinutes
            Try
                If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                    durationMinutes = MyIncaInterface.MyGmIncaComm.GetRecordingFileDurationMinutes()
                End If
            Catch ex As Exception
                HandleUserMessageLogging("GMRC",
                "HandleUpdatesWhenRecording: GetRecordingFileDurationMinutes failed: " & ex.Message)
            End Try

            HandleRecordingDurationRotation(durationMinutes)

            ' ==================================================================
            ' SECTION 4: Monitor VehicleSpy health
            ' ==================================================================
            If VehicleSpyCaptureStarted = True And CheckVSpyMessageDisplayed = False Then
                MyAltRcrdElapseTime = DateTime.Now.Subtract(MyAltRcrdSaveTime)
                If MyAltRcrdElapseTime.Seconds >= 5 Then
                    MyAltRcrdSaveTime = DateTime.Now
                    If IsProcessRunning("VSPY3") = False Then
                        VehicleSpyCaptureStarted = False
                        Dim currentActiveSequence As String = GetCurrentActiveSequence()
                        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

                        HandleUserMessageLogging("GMRC",
                        $"Vehicle Spy is no longer running for sequence {currentSeq:D2}. " &
                        "It is possible that it was manually exited. To re-enable Vehicle Spy, " &
                        "you must exit and re-launch CLEVIR...",,, FlashMsg4Sec)
                        OnVehicleScreen.Label3.BackColor = Color.Red
                        AlternateRecordEnabled = False
                        CheckVSpyMessageDisplayed = True
                    End If
                End If
            End If

            ' ==================================================================
            ' SECTION 5: Monitor CANalyzer health
            ' ==================================================================
            If CanalyzerCaptureStarted = True And CheckCanAlyzerMessageDisplayed = False Then
                MyCanAlyzerRcrdElapseTime = DateTime.Now.Subtract(MyCanAlyzerRcrdSaveTime)
                If MyCanAlyzerRcrdElapseTime.Seconds >= 5 Then
                    MyCanAlyzerRcrdSaveTime = DateTime.Now
                    If IsProcessRunning("CANW64") = False Then
                        CanalyzerCaptureStarted = False
                        Dim currentActiveSequence As String = GetCurrentActiveSequence()
                        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

                        HandleUserMessageLogging("GMRC",
                        $"CANalyzer is no longer running for sequence {currentSeq:D2}. " &
                        "It is possible that it was manually exited. To re-enable CANalyzer, " &
                        "you must exit and re-launch CLEVIR...",,, FlashMsg4Sec)
                        OnVehicleScreen.Label3.BackColor = Color.Red
                        AlternateRecordEnabled = False
                        CheckCanAlyzerMessageDisplayed = True
                    End If
                End If
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleUpdatesWhenRecording: " & ex.Message)
        End Try
    End Sub


    ''' <summary>
    ''' Normalizes a filename string by removing invisible characters and whitespace.
    ''' Prevents string comparison failures due to hidden Unicode artifacts.
    ''' </summary>
    Private Function NormalizeFilename(ByVal filename As String) As String
        If String.IsNullOrEmpty(filename) Then Return String.Empty

        ' Step 1: Trim leading/trailing whitespace
        Dim result As String = filename.Trim()

        ' Step 2: Remove zero-width characters (U+200B, U+FEFF, etc.)
        result = result.Replace(ChrW(&H200B), String.Empty)  ' Zero-width space
        result = result.Replace(ChrW(&HFEFF), String.Empty)  ' BOM
        result = result.Replace(ChrW(&H200C), String.Empty)  ' Zero-width non-joiner
        result = result.Replace(ChrW(&H200D), String.Empty)  ' Zero-width joiner

        ' Step 3: Remove any null characters
        result = result.Replace(vbNullChar, String.Empty)

        ' Step 4: Normalize Unicode form (combine decomposed characters)
        result = result.Normalize(NormalizationForm.FormC)  ' ← FIXED

        Return result
    End Function
    Private Async Sub HandleRecordingDurationRotation(ByVal durationMinutes As Integer)
        If durationMinutes <= 0 Then
            RecorderStopWatch?.Reset()
            Return
        End If

        If RecorderStopWatch.Elapsed.TotalMilliseconds < (durationMinutes * 60000) Then Return

        ' Guard against re-entry: the background loop can fire again while the
        ' async rotation is still in progress
        If _rotationInProgress Then Return
        _rotationInProgress = True

        Try
            ' ✅ Get current sequence BEFORE rotation
            Dim currentInfo = GetCurrentRecordingInfo()
            HandleUserMessageLogging("GMRC",
                                     $"Record duration ({durationMinutes} min) expired for seq {currentInfo.Sequence:D2}")

            ' ✅ CRITICAL: Rotate FIRST, then get the new filename
            Await MyIncaInterface.StopAndStartRecordingAsync()

            ' ✅ NOW get the predicted filename (should return sequence 02 after rotation)
            Dim predictedNext = GetPredictedRecordingFilename()
            HandleUserMessageLogging("GMRC", $"Rotation complete - new sequence: {predictedNext}")
            'EnsureSetLastRecordingFileName(predictedNext)

            If Not CheckRecordingFileNameFormat(displayMsg:=False) Then
                HandleUserMessageLogging("GMRC", "HandleRecordingDurationRotation: Recording file name format check failed", DisplayMsgBox)
                Await MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6)
            End If

            RecorderStopWatch.Reset()
            RecorderStopWatch.Start()

        Catch ex As Exception
            ' Async Sub — exceptions must be handled here or they crash the process
            HandleUserMessageLogging("GMRC", $"HandleRecordingDurationRotation: {ex.Message}", DisplayMsgBox)
        Finally
            _rotationInProgress = False
        End Try
    End Sub
    Private Async Sub StartRecordingMonitorTask()
        ' Dispose of any previous CTS and create a new one.
        _recordingMonitorCts?.Dispose()
        _recordingMonitorCts = New CancellationTokenSource()
        Dim token As CancellationToken = _recordingMonitorCts.Token

        _isMonitorTaskRunning = True

        Try
            While Not token.IsCancellationRequested
                Dim timeString As String = ""
                If RecordFileDurationMinutes > 0 AndAlso RecorderStopWatch?.IsRunning Then
                    Dim elapsedTime As TimeSpan = RecorderStopWatch.Elapsed
                    ' Format to show Minutes and Seconds, padding seconds with a leading zero.
                    timeString = $"{elapsedTime.Minutes}:{elapsedTime.Seconds:00}"
                End If

                ' To minimize UI thread work, only invoke an update if the text has changed.
                If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                    If OnVehicleScreen.Label8.Text <> timeString Then
                        If OnVehicleScreen.InvokeRequired Then
                            OnVehicleScreen.Invoke(Sub() OnVehicleScreen.Label8.Text = timeString)
                        Else
                            OnVehicleScreen.Label8.Text = timeString
                        End If
                    End If
                End If

                ' Wait for 100ms or until cancellation is requested.
                Await Task.Delay(100, token)
            End While
        Catch ex As TaskCanceledException
            ' This is the expected exception when the task is cancelled.
            ' We can safely ignore it and let the method exit gracefully.
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Error in recording monitor task: {ex.Message}")
        Finally
            ' Ensure the flag is reset and resources are cleaned up when the loop exits.
            _isMonitorTaskRunning = False
            _recordingMonitorCts?.Dispose()
            _recordingMonitorCts = Nothing
        End Try
    End Sub

    Private Sub StopRecordingMonitorTask()
        ' Signal the task to cancel. The task itself will handle cleanup.
        _recordingMonitorCts?.Cancel()
    End Sub

    Private Sub Handle_CS_WAVRECORD(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks.
        'Starts WAV Recording on button press using CAN Message Input...

        Dim Mytempval As Double
        Dim MyFormattedString As String

        'CS_WAVRECORD must be in AlsoAssociatedWith column of input in signal list for this to work...
        If MyDg.CS_WAVRECORD > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_WAVRECORD, 1)).SignalData

            'This is for testing only, have written code in label1.click (label at top right of screen) event on onvehiclescreen to test this...
            If TriggerWAVRecording = True Then
                Mytempval = 1.0 'For Testing...
                TriggerWAVRecording = False
            End If

            'We are translating input to a string using the FormatDisplayString function, assumes Mytempval will be either 1 TRUE or 0 FALSE
            MyFormattedString = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_WAVRECORD, 1), MyDg.DeviceName(MyDg.CS_WAVRECORD, 1), MyDg.VariableName(MyDg.CS_WAVRECORD, 1))

            If InStr(UCase(MyFormattedString), "TRUE") > 0 And InSession = True Then

                'Handle logic for WAV RECORD Here...

                'Currently, will only start WAV recording if we are recording data...
                'We will trigger recording a WAV file for the defined duration only...

                If OnVehicleScreen.PictureBox1.BackColor = Color.Red Then
                    MicrophoneClick(OnVehicleScreen.PictureBox1)
                    HandleUserMessageLogging("GMRC", "Starting WAV Recording...",,, FlashMsg1Sec)
                    UserStatusInfo.Label1.Text = ""
                    UserStatusInfo.Hide()
                Else
                    HandleUserMessageLogging("GMRC", "WAV Recording already in progress...",,, FlashMsg1Sec)
                    UserStatusInfo.Label1.Text = ""
                    UserStatusInfo.Hide()

                End If

            End If

        End If

    End Sub

    Private Sub HandleRecordingProgressBar(ByVal status As Boolean, ByVal progressBar As ProgressBar)
        ' HandleRecordingProgressBar is called from MyBackgroundTasks routine and from StartStopRecording and StartStopMeasurement
        ' This routine handles the green recording progress bar that is displayed either on the RecordPlayback form
        ' or on the main CLEVIR screen. The progressbar to operate on (OnVehicleScreen progressbar or RecordPlayback progressbar) 
        ' is passed in as ProgressBar.

        Try
            ' Ensure thread safety when updating the progress bar
            If progressBar.InvokeRequired Then
                progressBar.Invoke(New Action(Of Boolean, ProgressBar)(AddressOf HandleRecordingProgressBar), status, progressBar)
            Else
                If status Then
                    StartOrUpdateProgressBar(progressBar)
                Else
                    ResetProgressBar(progressBar)
                End If
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HandleRecordingProgressBar: {ex.Message}", DisplayMsgBox)
        End Try
    End Sub

    Private Sub StartOrUpdateProgressBar(ByVal progressBar As ProgressBar)
        ' Start or update the progress bar
        If progressBar.Value < progressBar.Maximum Then
            If progressBar.Value = 0 Then
                progressBar.Visible = True
            End If
            progressBar.Value += 5
        Else
            progressBar.Value = 0
        End If
    End Sub

    Private Sub ResetProgressBar(ByVal progressBar As ProgressBar)
        ' Reset the progress bar
        progressBar.Value = 0
        progressBar.Visible = False
    End Sub

    Private Sub SaveRecordFile(ByVal lineNum As Integer)

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

        For x = 0 To lineNum
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

    Private Function GetRecordedData(ByVal numValidVars As Integer,
                                           ByVal playbackMode As RecordPlayback.PlaybackStates) _
        As IGM_INCA_Comm.INCAData

        ' This function is called if we are playing back a recording.
        ' The first time it is called (when ReadNewDataFile = True), 
        ' it reads the entire file into memory and prepares an array for quick access.

        Dim MyIncaData As New IGM_INCA_Comm.INCAData

        ' We store playback data in this 2D array:
        '   Dimension(0..(NumSignals+1), 0..(NumRows-1))
        '   - Row dimension = each line in the file
        '   - Column dimension = 0 => Date/Time, 1 => Timestamp, 2.. => Signals-
        'Static MyDataArray(,) As String

        ' Because we only want to build these once, keep them Static.
        'Static columnIndexMap As Dictionary(Of String, Integer) = Nothing
        'Static x As Integer
        Dim y As Integer, i As Integer
        Dim direction As Integer

        Try
            If ReadNewDataFile Then
                SyncLock _playbackLock ' Ensure thread-safe access
                    ReadNewDataFile = False

                    ' Get filename and update UI through proper marshaling
                    Dim fileName As String = Nothing
                    Dim needCursorChange As Boolean = False

                    If RecordPlayback.InvokeRequired Then
                        RecordPlayback.Invoke(New Action(Sub()
                                                             fileName = RecordPlayback.PlayBackFileName
                                                             Cursor = Cursors.WaitCursor
                                                             RecordPlayback.Label2.Visible = True
                                                             RecordPlayback.Refresh()
                                                         End Sub))
                        needCursorChange = True
                    Else
                        fileName = RecordPlayback.PlayBackFileName
                        Cursor = Cursors.WaitCursor
                        RecordPlayback.Label2.Visible = True
                        RecordPlayback.Refresh()
                    End If

                    ' Read all lines from the file at once.
                    Dim lines As New List(Of String)
                    Using sr As New StreamReader(fileName)
                        While Not sr.EndOfStream
                            lines.Add(sr.ReadLine())
                        End While
                    End Using

                    ' Figure out how many rows total in the file
                    Dim totalLines As Integer = lines.Count
                    ' We need at least one line for headers
                    If totalLines = 0 Then
                        Throw New Exception("Playback file is empty.")
                    End If

                    ' Create a 2D array sized to (NumSignals+2) columns and totalLines rows
                    ReDim _playbackDataArray(UBound(MyIncaInterface.MyDisplaySignals) + 2, totalLines - 1)
                    _playbackColumnIndexMap = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                    ' Parse the header line to build a column index map for signals
                    Dim header As String() = lines(0).Split(","c)
                    _playbackColumnIndexMap = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

                    ' First 2 columns in the CSV are presumably date/time or similar:
                    '   column 0 -> MyDataArray(0, x)
                    '   column 1 -> MyDataArray(1, x)
                    ' So we only map signal columns (which start at index 2 in the CSV if present)
                    For col As Integer = 2 To header.Length - 1
                        Dim colName As String = header(col).Trim
                        If Not _playbackColumnIndexMap.ContainsKey(colName) Then
                            _playbackColumnIndexMap.Add(colName, col)
                        End If
                    Next

                    ' Fill MyDataArray with the lines (after the header)
                    ' Note: line 0 is the header, so start at line index=1
                    For row As Integer = 1 To totalLines - 1
                        Dim parts As String() = lines(row).Split(","c)

                        ' Defensive check: skip empty/short lines
                        If parts.Length < 2 Then Continue For

                        ' Store first two columns directly
                        _playbackDataArray(0, row) = parts(0)
                        _playbackDataArray(1, row) = parts(1)

                        ' Map each signal in MyIncaInterface.MyDisplaySignals
                        For sigIndex As Integer = 0 To UBound(MyIncaInterface.MyDisplaySignals)
                            Dim signalName As String = MyIncaInterface.MyDisplaySignals(sigIndex).SignalName
                            ' If our header dictionary knows this signal name, assign directly
                            If _playbackColumnIndexMap.ContainsKey(signalName) Then
                                Dim colIdx = _playbackColumnIndexMap(signalName)
                                If colIdx < parts.Length Then
                                    ' Validate and sanitize the data
                                    Dim value As String = parts(colIdx).Trim()
                                    If Double.TryParse(value, Nothing) Then
                                        _playbackDataArray(sigIndex + 2, row) = value
                                    Else
                                        ' If invalid, set to "0.0"
                                        _playbackDataArray(sigIndex + 2, row) = "0.0"
                                    End If
                                Else
                                    ' If column index is out of range, set 0.0
                                    _playbackDataArray(sigIndex + 2, row) = "0.0"
                                End If
                            Else
                                ' If not found, set 0.0
                                _playbackDataArray(sigIndex + 2, row) = "0.0"
                            End If
                        Next
                    Next

                    ' Number of rows we've read in
                    _playbackRowCounter = totalLines

                    ' Update UI elements through proper marshaling
                    If RecordPlayback.InvokeRequired Then
                        RecordPlayback.Invoke(New Action(Sub()
                                                             RecordPlayback.HScrollBar1.Maximum = _playbackRowCounter - 1
                                                             RecordPlayback.HScrollBar1.Minimum = 1
                                                             If needCursorChange Then
                                                                 Cursor = Cursors.Arrow
                                                             End If
                                                             RecordPlayback.Label2.Visible = False
                                                             RecordPlayback.Refresh()
                                                         End Sub))
                    Else
                        RecordPlayback.HScrollBar1.Maximum = _playbackRowCounter - 1
                        RecordPlayback.HScrollBar1.Minimum = 1
                        Cursor = Cursors.Arrow
                        RecordPlayback.Label2.Visible = False
                        RecordPlayback.Refresh()
                    End If
                End SyncLock
            End If

            ' Build the data to return
            ReDim MyIncaData.MyData(numValidVars - 1)
            ReDim MyIncaData.MyTime(numValidVars - 1)

            ' +/- direction based on playback state
            Select Case playbackMode
                Case RecordPlayback.PlaybackStates.PlaybackRun,
             RecordPlayback.PlaybackStates.PlaybackStepFwd
                    direction = 1
                Case RecordPlayback.PlaybackStates.PlaybackStepBack
                    direction = -1
                Case RecordPlayback.PlaybackStates.PlaybackScrolling
                    direction = 0
            End Select

            ' Handle boundary conditions
            If SaveLineNumber + direction = -1 Then
                SaveLineNumber = 2
            End If

            Dim maxValue As Integer = 0
            If RecordPlayback.InvokeRequired Then
                RecordPlayback.Invoke(New Action(Sub()
                                                     maxValue = RecordPlayback.HScrollBar1.Maximum
                                                 End Sub))
            Else
                maxValue = RecordPlayback.HScrollBar1.Maximum
            End If

            If SaveLineNumber + direction > maxValue Then
                SaveLineNumber = maxValue - direction
            End If

            ' Fill the IGM_INCA_Comm.INCAData structure
            i = 0
            For y = 2 To (numValidVars)
                MyIncaData.MyData(i) = _playbackDataArray(y, SaveLineNumber + direction)
                MyIncaData.MyTime(i) = _playbackDataArray(1, SaveLineNumber + direction)
                MyIncaData.MyStatus = True
                i += 1
            Next y

            ' Update the scrollbar and SaveLineNumber through proper marshaling
            If y = UBound(MyIncaInterface.MyDisplaySignals) + 3 Then
                Dim newLineNumber As Integer = SaveLineNumber + direction

                If RecordPlayback.InvokeRequired Then
                    RecordPlayback.Invoke(New Action(Sub()
                                                         RecordPlayback.HScrollBar1.Value = newLineNumber

                                                         ' Enable/disable buttons based on boundaries
                                                         If newLineNumber = RecordPlayback.HScrollBar1.Maximum Then
                                                             RecordPlayback.StepForward.Enabled = False
                                                             RecordPlayback.StepBack.Enabled = True
                                                             RecordPlayback.Reset.Enabled = True
                                                         End If

                                                         If newLineNumber = RecordPlayback.HScrollBar1.Minimum Then
                                                             RecordPlayback.PlayPauseButton.Enabled = True
                                                             RecordPlayback.StepForward.Enabled = True
                                                             RecordPlayback.StepBack.Enabled = False
                                                             RecordPlayback.Reset.Enabled = True
                                                         End If

                                                         RecordPlayback.Label1.Text = $"{newLineNumber} of {RecordPlayback.HScrollBar1.Maximum}"
                                                     End Sub))
                Else
                    RecordPlayback.HScrollBar1.Value = newLineNumber

                    ' Enable/disable buttons based on boundaries
                    If newLineNumber = RecordPlayback.HScrollBar1.Maximum Then
                        RecordPlayback.StepForward.Enabled = False
                        RecordPlayback.StepBack.Enabled = True
                        RecordPlayback.Reset.Enabled = True
                    End If

                    If newLineNumber = RecordPlayback.HScrollBar1.Minimum Then
                        RecordPlayback.PlayPauseButton.Enabled = True
                        RecordPlayback.StepForward.Enabled = True
                        RecordPlayback.StepBack.Enabled = False
                        RecordPlayback.Reset.Enabled = True
                    End If

                    RecordPlayback.Label1.Text = $"{newLineNumber} of {RecordPlayback.HScrollBar1.Maximum}"
                End If

                SaveLineNumber = newLineNumber
            End If

            Return MyIncaData

        Catch ex As Exception
            ' On error, return whatever data we have, and log the error
            HandleUserMessageLogging("GMRC", "GetRecordedData: " & ex.Message)
            Return MyIncaData
        End Try

    End Function

End Class
