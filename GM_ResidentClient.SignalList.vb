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

    Private Sub WriteSignalListFile(ByVal signalListFileName As String)

        'Save data back to the .csv file which contains the variable and display information
        'If anything has been changed (using the online dynamic re-configuration), the changes will be saved into the
        'previously used .csv file.  So, if we re-start the GmResidentClient
        'we will be using the .csv file with changes made during the previous session.

        'This is called when we exit the main form, but only if changes have been made.

        'It is also possible to call this routine from a button press on the GM Resident Client Configuration form (GridCellPropConfig.vb)

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer
        Dim n As Integer

        Dim ccon As ColorConverter

        Dim fileExtension As String

        Dim fnum As Integer

        Dim inputline As String = ""

        Try


            If File.Exists(signalListFileName) = False Then
                HandleUserMessageLogging("GMRC", signalListFileName & " cannot be found. Exiting", DisplayMsgBox,)
                Exit Sub
            End If

            ccon = New ColorConverter()

            fileExtension = Path.GetExtension(signalListFileName)

            'Must allow for additional data to have been added so we add 'NumSignalsAdded' to the current upper bound
            'of the exceldata array which was read in at startup. NumSignalsAdded is incremented whenever we add a new signal via
            'a new grid

            '0 based array vs 1 based array, here we copy everything from the 1 based exceldata array to the 0 based exceldataforsave array.
            'exceldata is 1 based because it is derived from exceldata = MyWorkSheet.UsedRange.Value, which is Excel way of doing things,
            'in vb.net, any array that we create  must be a 0 based array...

            If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") And ValidExcelData IsNot Nothing Then

                ReDim exceldataforsave((UBound(ValidExcelData, 1) - _numInvalid) + NumSignalsAdded, UBound(exceldata, 2))

                For x = 0 To UBound(exceldataforsave, 1)

                    If Len(ValidExcelData(x, 0)) > 0 Then
                        For i = 0 To EXCEL_DATA.DisplayFormat - 1
                            exceldataforsave(x, i) = ValidExcelData(x, i)
                        Next i
                    Else
                        exceldataforsave(x, i) = Nothing
                        HandleUserMessageLogging("GMRC", "WriteSignalListFile: ValidExcelData " & x & " = ***" & ValidExcelData(x, 0) & "***")
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

            For x = 0 To myDGs.Count - 1

                For n = 0 To MyDFs.Count - 1
                    If MyDFs(n).Name = myDGs(x).Parent.Name Then
                        Exit For
                    End If
                Next n

                If n < MyDFs.Count Then

                    exceldataforsave(i, EXCEL_DATA.DisplayWindowSize - 1) = MyDFs(n).DisplayWindowSize
                    exceldataforsave(i, EXCEL_DATA.AlsoAssociatedWith - 1) = MyDFs(n).AlsoAssociatedWith
                    exceldataforsave(i, EXCEL_DATA.LocationOnForm - 1) = myDGs(x).LocationOnForm
                    exceldataforsave(i, EXCEL_DATA.GridSize - 1) = myDGs(x).GridSize

                    For y = 1 To myDGs(x).RowCount
                        For z = 1 To myDGs(x).ColumnCount - 1

                            exceldataforsave(i, EXCEL_DATA.DisplayWindowName - 1) = MyDFs(n).Name
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
                            exceldataforsave(i, EXCEL_DATA.CheckForDataChange - 1) = IIf(Len(myDGs(x).CheckForDataChange(y, z)) > 0, UCase(myDGs(x).CheckForDataChange(y, z).ToString), "FALSE")
                            exceldataforsave(i, EXCEL_DATA.Row - 1) = y
                            exceldataforsave(i, EXCEL_DATA.Col - 1) = z
                            exceldataforsave(i, EXCEL_DATA.AlsoAssociatedWith - 1) = myDGs(x).AlsoAssociatedWith(y, z)
                            exceldataforsave(i, EXCEL_DATA.DisplayFormat - 1) = IIf(Len(myDGs(x).DisplayFormat(y, z)) > 0, myDGs(x).DisplayFormat(y, z), """0.000""")

                            i += 1
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

                    For x = UBound(ValidExcelData, 1) - _numInvalid To (UBound(exceldataforsave, 1) - 1)

                        exceldataforsave(x, EXCEL_DATA.VariableName - 1) = MyIncaInterface.myAddedSignals(y).SignalName
                        exceldataforsave(x, EXCEL_DATA.DeviceName - 1) = MyIncaInterface.myAddedSignals(y).DeviceName
                        exceldataforsave(x, EXCEL_DATA.Raster - 1) = MyIncaInterface.myAddedSignals(y).RasterName
                        y += 1

                    Next
                Else
                    For x = UBound(exceldata, 1) To UBound(exceldataforsave, 1) - 1

                        exceldataforsave(x, EXCEL_DATA.VariableName - 1) = MyIncaInterface.myAddedSignals(y).SignalName
                        exceldataforsave(x, EXCEL_DATA.DeviceName - 1) = MyIncaInterface.myAddedSignals(y).DeviceName
                        exceldataforsave(x, EXCEL_DATA.Raster - 1) = MyIncaInterface.myAddedSignals(y).RasterName
                        y += 1

                    Next
                End If

            End If

            Dim savefilename As String = ""

            If ConfigureForNewSoftwareVersion = False Then

                SaveFileDialog1.DefaultExt = fileExtension
                SaveFileDialog1.FileName = INCAVariableFile
                SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
                SaveFileDialog1.Filter = Mid(fileExtension, 2, Len(fileExtension)) & " |*" & fileExtension
                SaveFileDialog1.ShowDialog()

                savefilename = SaveFileDialog1.FileName

            Else
                savefilename = Mid(INCAVariableFile, 1, InStr(INCAVariableFile, fileExtension) - 1) & "_NEW" & fileExtension
            End If

            If Len(savefilename) > 0 Then

                If savefilename = INCAVariableFile Then
                    HandleUserMessageLogging("GMRC", "WriteSignalListFile: Updating " & INCAVariableFile)
                Else
                    HandleUserMessageLogging("GMRC", "WriteSignalListFile: Creating New INCA Variable File (" & savefilename & ")")
                    If ConfigureForNewSoftwareVersion = True Then
                        MsgBox("A new signal list file will now be created called " & savefilename & " This signal list has been modified by removing the signals flagged in the InvalidSignalsLog.csv File...")
                        MsgBox("Open this file, verify that it is correct, then rename the file based on the total number of signals removing '_NEW' from the filename...")
                    End If
                End If

                fnum = FreeFile()

                FileOpen(fnum, savefilename, OpenMode.Output)

                For x = 0 To UBound(exceldataforsave, 1) - 1
                    For y = 0 To UBound(exceldataforsave, 2) - 1
                        If y = 0 Then
                            inputline = exceldataforsave(x, y)
                        Else
                            inputline = inputline & "," & exceldataforsave(x, y)
                        End If
                    Next
                    PrintLine(fnum, inputline)
                Next

                FileClose(fnum)

                HandleUserMessageLogging("GMRC", "WriteSignalListFile: Signal List File Written successfully")

                If savefilename <> INCAVariableFile Then

                    If ConfigureForNewSoftwareVersion = False Then
                        If MsgBox("Do you wish to update the configuration file to use this newly saved variable file?", vbYesNo) = vbYes Then
                            INCAVariableFile = savefilename
                        End If
                    End If

                End If

                GridCellPropConfig._changesMade = False
                NumSignalsAdded = 0

            Else
                MsgBox("No Valid Filename selected, changes will not be saved....")
            End If

            'If there is an InvalidSignalsLog.csv file, we will rename it here...
            'This file is used during signal registration to log all of the signals
            'which cannot, for whatever reason, be registered.  The intent is to keep
            'track of unregisterable signals after each initialization attempt and delete
            'such signals from the signal list so subsequent signal registration attempts
            'will not include these signals. Signals are marked for deletion in the
            'ReadInSignalList routine...

            If File.Exists(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv") Then
                File.Move(My.Application.Info.DirectoryPath & "\InvalidSignalsLog.csv", My.Application.Info.DirectoryPath & "\InvalidSignalsLog_" & Format(DateTime.Now, "MMddyyyy_hhmmss") & ".csv")
            End If

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "WriteSignalListFile: " & ex.Message, DisplayMsgBox)

        End Try

    End Sub

    Private Function ReadInSignalList(ByVal signalListFileName As String) As Boolean
        ' Reads and processes an INCA signal list from a .CSV file (the only supported signal list format).
        ' Depending on the SignalRegistrationMode, signals may be flagged for forced registration.
        ' Visible signals (with a DisplayWindowName) are handled first; invisible signals are processed
        ' later. In FULL registration mode, all signals (visible + invisible) are processed.

        ' ✅ CRITICAL: Exit immediately if application is shutting down
        If exitInProgress Then
            HandleUserMessageLogging("GMRC", "ReadInSignalList: Skipping - exit in progress")
            Return True  ' Return success to avoid cascading errors
        End If

        ' ✅ Also check cancellation token
        If _initCts?.IsCancellationRequested Then
            HandleUserMessageLogging("GMRC", "ReadInSignalList: Skipping - cancellation requested")
            Return True
        End If

        Dim invalidSignalsArray() As String = Nothing
        Dim duplicate As Boolean = False
        Dim invalidSignal As Boolean = False
        Dim fileValid As Boolean = False
        Dim cameraNumber As Integer = 0
        Dim timeCodeCounter As Integer = 0
        Dim returnStr As String = ""
        Dim tempStr As String = ""
        Dim fnum As Integer = 0
        Dim fnumInvalid As Integer = 0

        Static beenHere As Boolean
        Dim y As Integer = 0

        Try
            ReadInSignalList = True

            ' --------------------------------------------------------------------------
            ' Basic File Checks & Handling
            ' --------------------------------------------------------------------------
            HandleUserMessageLogging("GMRC", "In ReadInSignalList SignalListFileName = " & signalListFileName)
            If Not File.Exists(signalListFileName) OrElse Not SignalListFile.IsCsv(signalListFileName) Then
                If Not HandleMissingSignalListFile(signalListFileName, fileValid) Then
                    ' If user decides to abort or exit, then return immediately
                    Return False
                End If
                ' If the user selects or chooses a different file, the returned name is in INCAVariableFile
                ' or we re-assign signalListFileName. 
                signalListFileName = INCAVariableFile
            End If

            ' Make a backup copy of the signal list file
            HandleUserMessageLogging("GMRC", $"ReadInSignalList: Creating Signal List backup file: {signalListFileName}.SAVE")
            FileCopy(signalListFileName, signalListFileName & ".SAVE")

            ' --------------------------------------------------------------------------
            ' Connect to INCA if necessary
            ' --------------------------------------------------------------------------
            If Not PlaybackMode Then
                If String.IsNullOrWhiteSpace(MyIncaInterface.GetCurrentVersion) Then
                    returnStr = MyIncaInterface.ConnectToInca()
                    If returnStr <> "True" Then
                        HandleUserMessageLogging("GMRC", "ReadInSignalList: ConnecToInca returned - " & returnStr, DisplayMsgBox,)
                        ExitApp()
                    End If
                    If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
                        HandleUserMessageLogging("GMRC", "ReadInSignalList: Getting Available Experiment Names...")
                        AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames
                        HandleUserMessageLogging("GMRC", "ReadInSignalList: Available Experiment Names Retrieved...")
                        'OnVehicleScreen.Refresh()
                    End If
                End If
            End If

            ' --------------------------------------------------------------------------
            ' Clear out old forms and grids if called again
            ' --------------------------------------------------------------------------
            OnVehicleScreen.TopMost = False
            If beenHere Then
                ResetFormsAndGrids()
            End If
            beenHere = True

            ' --------------------------------------------------------------------------
            ' Open the .CSV file and load data into excelData array
            ' --------------------------------------------------------------------------
            HandleUserMessageLogging("GMRC", $"ReadInSignalList: Reading In Signal/Display Configuration File {signalListFileName}...")

            exceldata = ReadSignalListFromCsv(signalListFileName)

            ' --------------------------------------------------------------------------
            ' If FULL registration and InvalidSignalsLog.csv exists, prepare ValidExcelData
            ' --------------------------------------------------------------------------
            If InStr(UCase(SignalRegistrationMode), "FULL") > 0 AndAlso File.Exists(Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv")) Then
                HandleUserMessageLogging("GMRC", "ReadInSignalList: FULL Registration InvalidSignalsLog.csv exists...")
                ReDim ValidExcelData(UBound(exceldata, 1) - 1, EXCEL_DATA.DisplayFormat - 1)
                ' Copy headers
                For i As Integer = 1 To EXCEL_DATA.DisplayFormat
                    ValidExcelData(0, i - 1) = exceldata(1, i)
                Next
            End If

            ' Prepare to read signals
            ReDim MyIncaInterface.mySignals(0)
            MyIncaInterface.mySignals(0).DeviceName = ""
            MyIncaInterface.mySignals(0).RasterName = ""
            MyIncaInterface.mySignals(0).SignalName = ""
            MyIncaInterface.mySignals(0).Status = "Invalid"

            ' --------------------------------------------------------------------------
            ' Main loop: Process rows in excelData
            ' --------------------------------------------------------------------------
            For y = 2 To UBound(exceldata, 1)

                Dim windowName As String = Trim(exceldata(y, EXCEL_DATA.DisplayWindowName))
                Dim deviceName As String = Trim(exceldata(y, EXCEL_DATA.DeviceName))
                Dim rasterName As String = Trim(exceldata(y, EXCEL_DATA.Raster))
                Dim variableName As String = Trim(CStr(exceldata(y, EXCEL_DATA.VariableName)))
                ' Normalize in case CSV provided quotes
                variableName = variableName.Trim(""""c)

                ' Clean up any trailing "_f"
                rasterName = CleanUpRasterName(rasterName)

                If Not String.IsNullOrEmpty(windowName) Then
                    ' ------------------------------------------------------------------
                    ' Visible (display) signals
                    ' ------------------------------------------------------------------
                    If y = 2 Then
                        ' First row with a DisplayWindowName -> Create the first form
                        HandleFirstDisplaySignalRow(deviceName, rasterName, variableName)
                    Else
                        ' Subsequent displayed signals
                        If MyInvisibleSignals IsNot Nothing Then
                            ' If we've already seen invisible signals but now see a visible one -> invalid file format
                            HandleUserMessageLogging("GMRC",
                                "ReadInSignalList: Invalid file format, signals with no DisplayWindowName " &
                                "must occur in the spreadsheet after all displayed signals",
                                DisplayMsgBox)
                            Return False
                        End If
                        HandleSubsequentDisplaySignalRow(
                            deviceName,
                            rasterName,
                            variableName,
                            cameraNumber,
                            y
                        )
                    End If

                Else
                    ' ------------------------------------------------------------------
                    ' Invisible (record-only) signals
                    ' ------------------------------------------------------------------
                    If String.IsNullOrEmpty(tempStr) Then
                        ' Mark the first time we see a row with no window name
                        MyIncaInterface.myPreliminaryDisplaySignals = MyIncaInterface.mySignals
                        If Not InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then
                            ' If not in FULL mode, we are done reading signals
                            Exit For
                        End If
                        tempStr = "Processing 'Record Only' Signals..."
                        HandleUserMessageLogging("GMRC", "ReadInSignalList: " & tempStr)
                    End If
                    ' Load invalidSignalsArray if necessary
                    If ProcessingInvalidSignalsLog Then
                        If fnum = 0 Then
                            invalidSignalsArray = LoadInvalidSignalsArray(fnum)
                        End If
                    End If

                    ' Add invisible signal to MyIncaInterface.MySignals (unless invalid/duplicate)
                    If Not HandleInvisibleSignalRow(
                        deviceName,
                        rasterName,
                        variableName,
                        duplicate,
                        invalidSignal,
                        invalidSignalsArray,
                        fnumInvalid,
                        y
                    ) Then
                        ' If file format is invalid or an error occurred, return false
                        Return False
                    End If
                End If

                ' Check if this is a VIDEO_CAMERA_TIMECODE row
                If IsVideoTimecode(variableName) Then
                    timeCodeCounter += 1
                End If

            Next

            ' --------------------------------------------------------------------------
            ' Post-processing: Cameras, GO/NOGO forms, finalize grids, cleanup
            ' --------------------------------------------------------------------------
            ' Existing less-than warning remains; add a greater-than notice
            If timeCodeCounter < NumberOfCamerasInVehicle Then
                HandleUserMessageLogging("GMRC",
                                         "WARNING: Number of Cameras in the signal list (" & timeCodeCounter &
                                         ") is less than the NumberOfCamerasInVehicle (" & NumberOfCamerasInVehicle & ")." &
                                         " Some cameras may not record video and their VIDEO_CAMERA_TIMECODE signals will not be displayed in the INCA Video Status window.",
                                         DisplayMsgBox
                                         )
            ElseIf timeCodeCounter > NumberOfCamerasInVehicle Then
                HandleUserMessageLogging("GMRC",
                                         "NOTICE: Signal list contains " & timeCodeCounter &
                                         " VIDEO_CAMERA_TIMECODE rows but NumberOfCamerasInVehicle is " & NumberOfCamerasInVehicle &
                                         ". Extra VIDEO_CAMERA_TIMECODE rows will be registered, but camera name mapping stops after " &
                                         NumberOfCamerasInVehicle & ".",
                                         DisplayMsgBox
                                         )
            End If

            ' Use VIDEO_CAMERA_TIMECODE-derived camera count to limit ping cycle later
            _effectiveCameraCount = If(timeCodeCounter > 0, timeCodeCounter, 0)

            For z As Integer = 0 To MyDFs.Count - 1
                If MyDFs(z) IsNot Nothing Then
                    If Not String.IsNullOrEmpty(MyDFs(z).AlsoAssociatedWith) AndAlso
                       MyDFs(z).AlsoAssociatedWith.Contains("GO/NOGO") Then
                        _gonogocount += 1
                    End If
                End If

                MyExitButtons(z)?.BringToFront()
            Next

            If myDGs(0) IsNot Nothing Then
                HandleUserMessageLogging("GMRC", " ")
                HandleUserMessageLogging("GMRC", "ReadInSignalList: Finalizing Grid Displays...")
                GridDataClass.FinalizeGridDisplays()
                HandleUserMessageLogging("GMRC", "ReadInSignalList: Grid Displays Finalized...")
                HandleUserMessageLogging("GMRC", " ")
                HandleUserMessageLogging("GMRC", "ReadInSignalList: Auto-sizing forms to content...")
                GridDataClass.AutoSizeFormsToContent()
                HandleUserMessageLogging("GMRC", "ReadInSignalList: Forms auto-sized.")
            End If

            HandleUserMessageLogging("GMRC", "ReadInSignalList: SignalList File Processed successfully.")
            OnVehicleScreen.Refresh()
            Return True

        Catch ex As Exception
            ' ✅ Don't show error dialogs during exit
            If Not exitInProgress Then
                HandleUserMessageLogging("GMRC", "ReadInSignalList: " & ex.Message & " - y = " & y, DisplayMsgBox)
            Else
                HandleUserMessageLogging("GMRC", "ReadInSignalList: " & ex.Message & " (during exit, suppressed)")
            End If
            Return False

        Finally
            Cursor = Cursors.Arrow
        End Try
    End Function

    '========================================================================================================
    ' BELOW ARE HELPER FUNCTIONS
    '========================================================================================================

    ''' <summary>
    ''' Trim whitespace and leading/trailing quotes that may come from CSV export
    ''' </summary>
    Private Shared Function IsVideoTimecode(name As String) As Boolean
        Return ResidentClientText.IsVideoTimecode(name)
    End Function

    ''' <summary>
    ''' Loads the InvalidSignalsLog.csv into a string array.
    ''' Each element in the array is a line from the CSV file.
    ''' </summary>
    Private Function LoadInvalidSignalsArray(ByRef fnumInvalid As Integer) As String()
        ' Define the path to the InvalidSignalsLog.csv file.
        Dim logPath As String = Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv")
        Dim invalidSignals As New List(Of String)()

        ' Since we're not using file numbers with StreamReader, set fnumInvalid to -1.
        fnumInvalid = -1

        ' Use StreamReader for better file handling.
        Try
            Using reader As New StreamReader(logPath)
                Dim line As String
                While Not reader.EndOfStream
                    line = reader.ReadLine()
                    invalidSignals.Add(line)
                End While
            End Using
        Catch ex As IOException
            ' Handle exceptions such as file not found or access denied.
            ' Log the exception message if needed.
            ' For now, we return an empty array.
            Return New String() {}
        End Try

        ' Convert the list to an array and return.
        Return invalidSignals.ToArray()
    End Function

    ''' <summary>
    ''' Handles adding invisible signals (no DisplayWindowName) to MySignals in FULL registration mode,
    ''' checking for duplicates or invalid entries in InvalidSignalsLog.
    ''' </summary>
    Private Function HandleInvisibleSignalRow(
        ByVal deviceName As String,
        ByVal rasterName As String,
        ByVal variableName As String,
        ByRef duplicate As Boolean,
        ByRef invalidSignal As Boolean,
        ByVal invalidSignalsArray() As String,
        ByRef fnum2 As Integer,
        ByVal y As Integer
    ) As Boolean

        duplicate = False
        invalidSignal = False

        ' If user wants to remove invalid signals from log
        If ProcessingInvalidSignalsLog AndAlso invalidSignalsArray IsNot Nothing Then
            For i As Integer = 0 To UBound(invalidSignalsArray)
                Dim lineSplit = invalidSignalsArray(i).Split(","c)

                ' Make sure lineSplit has at least 2 columns: [signalName, deviceName, ...]
                If lineSplit.Length >= 2 Then
                    If lineSplit(0) = variableName AndAlso lineSplit(1) = deviceName Then
                        invalidSignal = True
                        _numInvalid += 1

                        ' If flagged as duplicate, mark for removal
                        If lineSplit.Length >= 4 AndAlso lineSplit(3).Contains("DUPLICATE") Then
                            invalidSignalsArray(i) = "REMOVED,,,"
                        End If
                        Exit For
                    End If
                End If
            Next
        End If

        ' Check for duplicates in existing signals
        ' For CAN signals, compare rasterName as well
        If Not invalidSignal AndAlso MyIncaInterface.mySignals IsNot Nothing Then
            For x As Integer = 0 To UBound(MyIncaInterface.mySignals)
                If MyIncaInterface.mySignals(x).SignalName = variableName AndAlso
                   MyIncaInterface.mySignals(x).DeviceName = deviceName Then

                    If deviceName.Contains("CAN-Monitoring") Then
                        If MyIncaInterface.mySignals(x).RasterName = rasterName Then
                            duplicate = True
                        End If
                    Else
                        duplicate = True
                    End If
                    If duplicate Then Exit For
                End If
            Next
        End If

        ' If not duplicate or invalid, expand the arrays
        If Not duplicate AndAlso Not invalidSignal Then
            ' Resize MySignals
            ReDim Preserve MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals) + 1)

            ' Resize MyInvisibleSignals
            If MyInvisibleSignals Is Nothing Then
                ReDim MyInvisibleSignals(0)
            Else
                ReDim Preserve MyInvisibleSignals(UBound(MyInvisibleSignals) + 1)
            End If

            ' Populate the new invisible signal (in both arrays)
            Dim newSigIdx As Integer = UBound(MyIncaInterface.mySignals)
            MyInvisibleSignals(UBound(MyInvisibleSignals)).DeviceName = deviceName
            MyInvisibleSignals(UBound(MyInvisibleSignals)).RasterName = rasterName
            MyInvisibleSignals(UBound(MyInvisibleSignals)).SignalName = variableName

            MyIncaInterface.mySignals(newSigIdx).DeviceName = deviceName
            MyIncaInterface.mySignals(newSigIdx).RasterName = rasterName
            MyIncaInterface.mySignals(newSigIdx).SignalName = variableName
            MyIncaInterface.mySignals(newSigIdx).Status = "Invalid"
            MyIncaInterface.mySignals(newSigIdx).ForceRegister = False  ' not displayed, but may still be recorded

        Else
            ' If it's a duplicate or invalid, log it if we are not removing them from invalid signals
            If duplicate OrElse invalidSignal Then
                Dim reason As String = If(duplicate, "DUPLICATE SIGNAL NAME AND DEVICE", "INVALID SIGNAL")
                HandleUserMessageLogging("GMRC",
                    $"ReadInSignalList: {reason} in Signal List File: {variableName}/{deviceName} - {rasterName}")

                If Not ProcessingInvalidSignalsLog Then
                    If fnum2 = 0 Then
                        fnum2 = FreeFile()
                        FileOpen(fnum2, Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv"), OpenMode.Output)
                    Else
                        FileOpen(fnum2, Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv"), OpenMode.Append)
                    End If
                    PrintLine(fnum2, $"{variableName},{deviceName},{rasterName},{reason}")
                    FileClose(fnum2)
                End If
            End If
        End If

        ' Return True to indicate we handled it successfully
        Return True
    End Function

    '========================================================================================================
    ' OTHER HELPER FUNCTIONS
    '========================================================================================================

    ''' <summary>
    ''' Checks for missing or invalid file and prompts the user for next steps. Returns False if user chooses to exit.
    ''' </summary>
    Private Function HandleMissingSignalListFile(
        ByRef signalListFileName As String,
        ByRef fileValid As Boolean
    ) As Boolean

        Dim fileExtension As String = SignalListFile.Extension
        Dim answer As Integer
        Using msgBox As New Cusmsgbox()
            answer = msgBox.DisplayCusMsgBox(
                OnVehicleScreen,
                "Signal List referenced in the configuration file cannot be found or is invalid.",
                "User Input Required",
                "Exit CLEVIR",
                "Use Latest Signal List and Experiment",
                "Select Signal List and Experiment",
                ""
            )
        End Using

        HandleUserMessageLogging("GMRC",
                                "ReadInSignalList: User Answered " & answer & " when Signal List referenced was invalid...")

        Select Case answer
            Case 1
                ' Exit
                Return False

            Case 2
                ' Use Latest
                Dim saveDate As Date
                Dim saveFileName As String = ""
                Dim dir As New DirectoryInfo(Path.Combine(My.Application.Info.DirectoryPath, "SignalLists"))
                Dim files As FileInfo() = dir.GetFiles()

                For Each f As FileInfo In files
                    If SignalListFile.IsSignalListCandidate(f.Name) Then

                        If File.GetLastWriteTime(f.FullName) > saveDate Then
                            saveDate = File.GetLastWriteTime(f.FullName)
                            saveFileName = f.FullName
                        End If
                    End If
                Next

                If Not String.IsNullOrEmpty(saveFileName) Then
                    signalListFileName = saveFileName
                    INCAVariableFile = signalListFileName
                    OnVehicleScreen.Label5.Text = INCAVariableFile

                    INCAExperiment = Path.GetFileNameWithoutExtension(INCAVariableFile)
                    InitialINCAExperiment = INCAExperiment

                    HandleUserMessageLogging("GMRC",
                        $"Using Signal List {INCAExperiment}{fileExtension} and Experiment {INCAExperiment}")
                    StatusNotifier.Toast($"Using Signal List {INCAExperiment}{fileExtension} and Experiment {INCAExperiment}", "INCA", 2000)
                Else
                    Return False
                End If

            Case 3
                ' Select Specific
                Do While fileValid = False
                    OpenFileDialog1.InitialDirectory = Path.Combine(My.Application.Info.DirectoryPath, "SignalLists")
                    OpenFileDialog1.DefaultExt = fileExtension
                    OpenFileDialog1.Filter = Mid(fileExtension, 2, Len(fileExtension)) & " |*" & fileExtension
                    OpenFileDialog1.Title = "Please Select a Signal Configuration File"
                    OpenFileDialog1.ShowDialog()

                    If Not String.IsNullOrEmpty(OpenFileDialog1.FileName) AndAlso
                       OpenFileDialog1.FileName.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase) Then

                        fileValid = True
                        signalListFileName = OpenFileDialog1.FileName
                        INCAVariableFile = signalListFileName
                        OnVehicleScreen.Label5.Text = INCAVariableFile

                        SelectExperiment.ListBox1.Items.Clear()
                        For i As Integer = 0 To UBound(AvailableExperimentNames)
                            Dim nameU As String = AvailableExperimentNames(i).ToUpper()
                            If Not nameU.Contains("BLANK EXPERIMENT") AndAlso Not nameU.Contains("EMPTY EXPERIMENT") Then
                                SelectExperiment.ListBox1.Items.Add(AvailableExperimentNames(i))
                            End If
                        Next
                        SelectExperiment.ShowDialog()

                        If SelectExperiment.ListBox1.SelectedIndex > -1 Then
                            INCAExperiment = SelectExperiment.ListBox1.SelectedItem.ToString()
                            InitialINCAExperiment = INCAExperiment
                        Else
                            MsgBox($"No Experiment selected, Using {INCAExperiment}")
                        End If

                        HandleUserMessageLogging("GMRC", $"Using Signal List {signalListFileName} And Experiment {INCAExperiment}")
                        StatusNotifier.Toast($"Using Signal List {signalListFileName} and Experiment {INCAExperiment}", "INCA", 2000)

                    Else
                        Dim res = MsgBox("Invalid signal configuration filename. Please select a .csv file. Try Again?", vbYesNo)
                        If res = vbNo Then
                            Return False
                        End If
                    End If
                Loop
        End Select

        Return True
    End Function

    ''' <summary>
    ''' Resets or clears out the data forms and data grids if function is called again.
    ''' </summary>
    Private Sub ResetFormsAndGrids()
        ' Clear MyDGs list
        For z As Integer = 0 To myDGs.Count - 1
            myDGs(z) = Nothing
        Next

        ' Clear MyDFs list
        For x As Integer = 0 To MyDFs.Count - 1
            MyDFs(x) = Nothing
        Next
    End Sub

    ''' <summary>
    ''' Reads the signal list from a CSV file and returns a 2D object array analogous to reading an Excel range.
    ''' Parsing lives in <see cref="SignalListCsvReader"/>; this wrapper adds logging and error context.
    ''' </summary>
    Private Function ReadSignalListFromCsv(ByVal signalListFileName As String) As Object(,)
        HandleUserMessageLogging("GMRC", $"ReadInSignalList: Opening {signalListFileName} file...")

        Try
            Dim excelData As Object(,) = SignalListCsvReader.Read(signalListFileName)

            HandleUserMessageLogging("GMRC", $"Successfully loaded {UBound(excelData, 1)} rows with {UBound(excelData, 2)} columns from CSV file")
            Return excelData

        Catch ex As FileNotFoundException
            HandleUserMessageLogging("GMRC", $"ReadSignalListFromCsv: File not found - {signalListFileName}")
            Throw New FileNotFoundException($"Signal list file not found: {signalListFileName}", ex)
        Catch ex As UnauthorizedAccessException
            HandleUserMessageLogging("GMRC", $"ReadSignalListFromCsv: Access denied - {signalListFileName}")
            Throw New UnauthorizedAccessException($"Access denied to signal list file: {signalListFileName}", ex)
        Catch ex As IOException
            HandleUserMessageLogging("GMRC", $"ReadSignalListFromCsv: IO error reading file - {ex.Message}")
            Throw New IOException($"Error reading signal list file: {signalListFileName}", ex)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ReadSignalListFromCsv: Unexpected error - {ex.Message}")
            Throw New InvalidOperationException($"Failed to read signal list from CSV: {signalListFileName}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Cleans up a raster name that might end in "_f" by converting it to "-f".
    ''' </summary>
    Private Function CleanUpRasterName(ByVal raster As String) As String
        Return ResidentClientText.CleanUpRasterName(raster)
    End Function

    ''' <summary>
    ''' Handles the very first displayed signal row, which triggers form creation and grid initialization.
    ''' </summary>
    Private Sub HandleFirstDisplaySignalRow(
        ByVal deviceName As String,
        ByVal rasterName As String,
        ByVal variableName As String
    )
        ' Because this is the very first row, we initialize MySignals(0) with the row’s data
        MyIncaInterface.mySignals(0).DeviceName = deviceName
        MyIncaInterface.mySignals(0).RasterName = rasterName
        MyIncaInterface.mySignals(0).SignalName = variableName
        MyIncaInterface.mySignals(0).Status = "Invalid"

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "ReadInSignalList: Creating Forms...")

        CreateNewForm(0,
                      exceldata(2, EXCEL_DATA.DisplayWindowName),
                      exceldata(2, EXCEL_DATA.AlsoAssociatedWith),
                      exceldata(2, EXCEL_DATA.DisplayWindowSize),
                      -5000)

        GridDataClass.InitializeFlexGrids(2, 0, 0)
        GridDataClass.FormatFlexGrids(2, 0, 0)
        PositionGridOnForm(0)

        If ShouldForceRegister(0, 1, 1, UBound(MyIncaInterface.mySignals)) Then
            MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).ForceRegister = True
            myDGs(0).Registered(1, 1) = True
            _progressBarMax += 1
        End If
    End Sub

    ''' <summary>
    ''' Handles subsequent display-signal rows, which may require creating a new form or a new grid, 
    ''' or reusing existing ones. Also handles VIDEO_CAMERA_TIMECODE camera logic.
    ''' </summary>
    Private Sub HandleSubsequentDisplaySignalRow(
    ByVal deviceName As String,
    ByVal rasterName As String,
    ByVal variableName As String,
    ByRef cameraNumber As Integer,
    ByVal rowIndex As Integer
)

        ' Expand MySignals array by 1
        ReDim Preserve MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals) + 1)
        Dim newSigIndex = UBound(MyIncaInterface.mySignals)

        ' If the variable is VIDEO_CAMERA_TIMECODE, adjust device name to camera name (bounded)
        If IsVideoTimecode(variableName) Then
            Dim mappedName As String = deviceName
            If NumberOfCamerasInVehicle > 0 AndAlso cameraNumber < NumberOfCamerasInVehicle Then
                ' Also ensure CameraNames has an entry for this index if available
                If CameraNames IsNot Nothing AndAlso cameraNumber >= 0 AndAlso cameraNumber < CameraNames.Length Then
                    mappedName = CameraNames(cameraNumber)
                End If
                cameraNumber += 1
            End If
            MyIncaInterface.mySignals(newSigIndex).DeviceName = mappedName
            exceldata(rowIndex, EXCEL_DATA.DisplayName) = mappedName
        Else
            MyIncaInterface.mySignals(newSigIndex).DeviceName = deviceName
        End If

        MyIncaInterface.mySignals(newSigIndex).RasterName = rasterName
        MyIncaInterface.mySignals(newSigIndex).SignalName = variableName
        MyIncaInterface.mySignals(newSigIndex).Status = "Invalid"

        ' Create or find form
        Dim displayWindowName As String = CStr(exceldata(rowIndex, EXCEL_DATA.DisplayWindowName))
        Dim alsoAssocWith As String = CStr(exceldata(rowIndex, EXCEL_DATA.AlsoAssociatedWith))
        Dim displayWindowSize As String = CStr(exceldata(rowIndex, EXCEL_DATA.DisplayWindowSize))

        EnsureFormExists(displayWindowName, alsoAssocWith, displayWindowSize)

        ' Ensure a grid is created or updated with the row’s data
        Dim gridName As String = CStr(exceldata(rowIndex, EXCEL_DATA.AssociatedControlName))
        UpdateOrCreateGrid(displayWindowName, gridName, rowIndex, newSigIndex)
    End Sub

    ''' <summary>
    ''' Checks whether we should force-register the signal (based on SignalRegistrationMode, etc).
    ''' </summary>
    Private Function ShouldForceRegister(
        ByVal gridIndex As Integer,
        ByVal rowVal As Integer,
        ByVal colVal As Integer,
        ByVal signalIndex As Integer
    ) As Boolean

        Dim assoc As String = myDGs(gridIndex).AlsoAssociatedWith(rowVal, colVal)
        If assoc Is Nothing Then assoc = ""

        ' "GO/NOGO" or "AUTOANNO" or "CS_" in AlsoAssociatedWith => Force registration in GO/NOGO mode
        If (assoc.Contains("GO/NOGO") OrElse assoc.Contains("AUTOANNO") OrElse assoc.Contains("CS_")) _
            AndAlso UCase(SignalRegistrationMode) = "GO/NOGO" Then
            Return True
        End If

        ' DISPLAYS => force registration
        If UCase(SignalRegistrationMode) = "DISPLAYS" Then
            Return True
        End If

        ' FULL => force registration
        If InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then
            Return True
        End If

        Return False
    End Function

    ''' <summary>
    ''' Called from various click events which are associated with displaying a particular
    ''' form which has been dynamically created based on information in the INCAVariableFile.
    '''
    ''' This routine will register the signals on a given form dynamically.
    ''' This will occur if the user has selected the GO/NOGO option on initialization, because the signals
    ''' associated with the display have not been registered until it has been displayed for the first time.
    '''
    ''' ✅ NAMING: Despite the similar name, this is NOT another copy of signal registration logic.
    ''' It only flags the signals belonging to display form "j" as ForceRegister=True in
    ''' MyIncaInterface.myDisplaySignals, then delegates the actual INCA registration work to
    ''' MyIncaInterface.RegisterSignals() (which in turn calls MyGmIncaComm.RegisterSignalsCore).
    ''' </summary>
    Public Sub RegisterDisplaySignals(ByVal j As Integer)

        Dim found As Boolean
        Dim e As EventArgs
        Dim z As Integer
        Dim x As Integer
        Dim row As Integer
        Dim col As Integer
        Dim reply As MsgBoxResult
        Dim saveBackColor As Color

        Dim signalsRegistered As Boolean
        Dim formName As String


        e = EventArgs.Empty
        reply = Nothing

        'need to handle top down view differently than other forms....

        If j <= MyDFs.Count - 1 Then
            signalsRegistered = MyDFs(j).SignalsRegistered
            formName = MyDFs(j).Text
        Else 'this is top down view case...
            signalsRegistered = _topDownSignalsRegistered
            formName = "Top Down View"
        End If

        If signalsRegistered = False And UCase(SignalRegistrationMode) = "GO/NOGO" Then

            If MyIncaInterface.GetRecordingState = True Then
                reply = MsgBox("Signals are not yet registered for " & formName & " To view this display, signals must be registered, which will require recording to be STOPPED. Stop Recording and Continue?", vbYesNo)
            ElseIf MyIncaInterface.GetMeasurementStatus = "True" Then
                reply = MsgBox("Signals are not yet registered for " & formName & " To view this display, signals must be registered, which will require Measurement to be STOPPED. Stop Measurement and Continue?", vbYesNo)
            Else
                MsgBox("Signals are not yet registered for " & formName & " Please Wait while signals are registered...")

            End If

            If reply = MsgBoxResult.No Then
                Exit Sub
            End If

            If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                OnVehicleScreen.Label5.Visible = True
                saveBackColor = OnVehicleScreen.Label5.BackColor
                OnVehicleScreen.Label5.BackColor = Color.Yellow
                OnVehicleScreen.Label5.Text = "Operation In Progress, Please Wait..."
                OnVehicleScreen.Refresh()
            End If

            If reply = MsgBoxResult.Yes Then

                If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                    OnVehicleScreen.Cursor = Cursors.WaitCursor
                    OnVehicleScreen.Button6.Text = "STOP MEASUREMENT"
                    MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                    .GetAwaiter().GetResult()

                Else
                    Cursor = Cursors.WaitCursor
                End If

                Do While MyIncaInterface.GetMeasurementStatus() = "True"
                    Thread.Sleep(100)
                Loop

            End If

            For z = 0 To myDGs.Count - 1
                If (myDGs(z).Parent.Name = formName) Or formName = "Top Down View" Then
                    found = True
                End If

                If found = True Then
                    found = False
                    For row = 1 To myDGs(z).RowCount - 1
                        For col = 1 To myDGs(z).ColumnCount - 1

                            For x = 0 To UBound(MyIncaInterface.myDisplaySignals)
                                If ((myDGs(z).AlsoAssociatedWith(row, col) <> "GO/NOGO" And
                                    myDGs(z).AlsoAssociatedWith(row, col) <> "AUTOANNO" And
                                   InStr(myDGs(z).AlsoAssociatedWith(row, col), "CS_") = 0 And
                                   formName <> "Top Down View") Or
                                   (formName = "Top Down View" And InStr(myDGs(z).AlsoAssociatedWith(row, col), "TD") > 0)) And
                                    (MyIncaInterface.myDisplaySignals(x).DeviceName = myDGs(z).DeviceName(row, col) And
                                    MyIncaInterface.myDisplaySignals(x).SignalName = myDGs(z).VariableName(row, col) And
                                    MyIncaInterface.myDisplaySignals(x).RasterName = myDGs(z).Raster(row, col)) Then
                                    MyIncaInterface.myDisplaySignals(x).ForceRegister = True
                                    myDGs(z).Registered(row, col) = True
                                    Exit For
                                End If
                            Next x

                        Next col
                    Next row
                End If

            Next z

            MyIncaInterface.myPreliminaryDisplaySignals = MyIncaInterface.myDisplaySignals

            MyIncaInterface.RegisterSignals()

            signalsRegistered = True

            If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                .GetAwaiter().GetResult()
            End If

            Cursor = Cursors.Arrow

            OnVehicleScreen.Label5.BackColor = saveBackColor

            ' Update the UI and save the filename.
            ' Use the new predicted filename helper to keep UI and MDA shortcuts consistent.
            Dim predictedFilenameForUi As String = GetPredictedRecordingFilename()
            If Not String.IsNullOrEmpty(predictedFilenameForUi) Then
                OnVehicleScreen.Label5.Text = $"Recording Filename: {predictedFilenameForUi}"
                SaveRecordingFileName = predictedFilenameForUi
                ' Keep MDA/shortcuts in sync before we start recording (idempotent if recording already started)
                'EnsureSetLastRecordingFileName(predictedFilenameForUi)
            End If
            OnVehicleScreen.Refresh()

            If j <= MyDFs.Count - 1 Then
                MyDFs(j).SignalsRegistered = signalsRegistered
            Else 'this is top down view case...
                _topDownSignalsRegistered = signalsRegistered
            End If

        End If

    End Sub

End Class
