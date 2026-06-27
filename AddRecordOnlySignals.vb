
Option Strict Off
Option Explicit On

Imports VB = Microsoft.VisualBasic

Public Class AddRecordOnlySignals

    'This is a display form which allows the user to add signals or variables to the INCAVariableFile excel spreadsheet
    'It can be accessed only from a drop down off of the Configuration environment (GmResidentClient form) ACTIONS tab.

    'This functionality is legacy and is not really used...

    Private _myexceldata(,) As Object
    Private _suppressListLoad As Boolean

    Private Sub CheckAndAddSignals(ByVal signalName As String, ByVal deviceName As String, ByVal rasterName As String)

        'responsible for adding signals to the inca variable file (.xlsx).  Based on user selected parameters,
        'determines whether or not to add signals and whether or not to prompt the user with information
        'which will help the user determine whether or not to add signals.  Used in conjunction with the
        'AddRecordOnlySignals screen.

        Dim found As Boolean

        If InStr(deviceName, "CAN-Monitoring:") = 0 Then
            If VB.Left(signalName, 2) = "Va" And InStr(signalName, "[x]") = 0 Then
                signalName &= "[x]_[0]"
            ElseIf InStr(signalName, "[x]") > 0 And InStr(signalName, "_[") = 0 Then
                signalName &= "_[0]"
            End If
        End If

        found = False

        'For each signal, we go through the entire signal list to look for the signal name in the list.  
        'If we do not find the signal, we will add it.  If we find the signal, we will compare
        'device name and raster name and depending on the user selections, we determine what to do, either add,
        'dont add, or change raster...

        For x = 2 To UBound(exceldata, 1)

            If CheckBox1.Checked = False Then 'we don't care if signal already exists in different device, we add anyway

                If exceldata(x, EXCEL_DATA.VariableName) = signalName And
                exceldata(x, EXCEL_DATA.DeviceName) = deviceName Then

                    If exceldata(x, EXCEL_DATA.Raster) = rasterName Then

                        Label6.Text = exceldata(x, EXCEL_DATA.VariableName) & "/" & exceldata(x, EXCEL_DATA.DeviceName) & " Already exists."
                        Label6.Refresh()
                        found = True
                        Exit For

                    Else

                        If RadioButton2.Checked = True Then
                            exceldata(x, EXCEL_DATA.Raster) = rasterName
                            found = True
                            Exit For
                        ElseIf RadioButton1.Checked = True Then
                            If MsgBox(exceldata(x, EXCEL_DATA.VariableName) & "/" & exceldata(x, EXCEL_DATA.DeviceName) & " Signal / Device pair already exists, but is in the " & exceldata(x, EXCEL_DATA.Raster) & " Raster.  Change Raster to " & rasterName & "?", vbYesNo) = vbYes Then
                                exceldata(x, EXCEL_DATA.Raster) = rasterName
                            End If
                            found = True
                            Exit For
                        ElseIf RadioButton3.Checked = True Then
                            found = True
                            Exit For
                        Else
                            found = False
                        End If

                    End If

                End If

                'so, if signal name not equal, or signal name equal and device name not equal, found is still false and we add...

            Else 'if Signal Name already in a different device i dont want to add it, if same device i need to check raster (if RadioButton1 is checked)...

                If exceldata(x, EXCEL_DATA.VariableName) = signalName Then
                    If exceldata(x, EXCEL_DATA.DeviceName) <> deviceName Then

                        Label6.Text = exceldata(x, EXCEL_DATA.VariableName) & " Already exists in " & exceldata(x, EXCEL_DATA.DeviceName)
                        Label6.Refresh()
                        found = True
                        Exit For

                    Else  'signal name and device name the same, so we check rasterna...

                        If exceldata(x, EXCEL_DATA.Raster) = rasterName Then

                            Label6.Text = exceldata(x, EXCEL_DATA.VariableName) & "/" & exceldata(x, EXCEL_DATA.DeviceName) & " Already exists."
                            Label6.Refresh()
                            found = True
                            Exit For

                        Else

                            If RadioButton2.Checked = True Then
                                exceldata(x, EXCEL_DATA.Raster) = rasterName
                                found = True
                                Exit For
                            ElseIf RadioButton1.Checked = True Then
                                If MsgBox(exceldata(x, EXCEL_DATA.VariableName) & "/" & exceldata(x, EXCEL_DATA.DeviceName) & " Signal / Device pair already exists, but is in the " & exceldata(x, EXCEL_DATA.Raster) & " Raster.  Change Raster to " & rasterName & "?", vbYesNo) = vbYes Then
                                    exceldata(x, EXCEL_DATA.Raster) = rasterName
                                End If
                                found = True
                                Exit For
                            ElseIf RadioButton3.Checked = True Then
                                found = True
                                Exit For
                            Else
                                found = False
                            End If

                        End If

                    End If

                End If

            End If

        Next

        If found = False Then

            GmResidentClient.NumSignalsAdded = GmResidentClient.NumSignalsAdded + 1

            If GmResidentClient.NumSignalsAdded = 1 Then
                ReDim Preserve MyIncaInterface.myAddedSignals(0)
            Else
                ReDim Preserve MyIncaInterface.myAddedSignals(UBound(MyIncaInterface.myAddedSignals) + 1)
            End If

            MyIncaInterface.myAddedSignals(UBound(MyIncaInterface.myAddedSignals)).SignalName = signalName
            MyIncaInterface.myAddedSignals(UBound(MyIncaInterface.myAddedSignals)).DeviceName = deviceName

            If Len(rasterName) > 0 Then
                MyIncaInterface.myAddedSignals(UBound(MyIncaInterface.myAddedSignals)).RasterName = rasterName
            Else
                MyIncaInterface.myAddedSignals(UBound(MyIncaInterface.myAddedSignals)).RasterName = MyIncaInterface.GetDefaultRasterForMeasureElementInDevice(deviceName, signalName)
            End If

        End If

    End Sub

    Private Sub AddUserSelectedSignalsButton()

        Dim devicePos As Integer
        Dim firstRasterPos As Integer = 0

        Dim fnum As Integer
        Dim textline As String
        Dim breakout() As String
        Dim filename As String = ""

        Cursor = Cursors.WaitCursor
        Label6.Text = "Adding Signals from Selected Signal File..."
        Label6.Refresh()

        Select Case ListBox4.SelectedItem.ToString

            Case "INCA Generated XLS File"

                For x = 2 To UBound(_myexceldata, 2)

                    If _myexceldata(1, x) = "Data Source" Then

                        devicePos = x

                    End If

                    If IsNumeric(Mid(_myexceldata(1, x), 1, 1)) And firstRasterPos = 0 Then

                        firstRasterPos = x

                    End If

                Next

                For y = 2 To UBound(_myexceldata, 1)
                    For x = firstRasterPos To UBound(_myexceldata, 2)

                        If _myexceldata(y, x) = "X" Then
                            If ComboBox1.Text = "ALL" Then

                                CheckAndAddSignals(_myexceldata(y, 1), _myexceldata(y, devicePos), _myexceldata(1, x))

                            Else

                                If InStr(_myexceldata(y, 1), "[x]") > 0 Then

                                    If Val(Mid(_myexceldata(y, 1), InStr(_myexceldata(y, 1), "[x]_[") + 5, Len(_myexceldata(y, 1)) - 1)) <= Val(ComboBox1.Text) Then

                                        CheckAndAddSignals(_myexceldata(y, 1), _myexceldata(y, devicePos), _myexceldata(1, x))
                                    End If

                                End If

                            End If

                        End If

                    Next x
                Next y

            Case "User Created XLSX File (Signal, Device, Raster)"

                For x = 2 To UBound(_myexceldata, 1)
                    If ComboBox1.Text = "ALL" Then

                        CheckAndAddSignals(_myexceldata(x, 1), _myexceldata(x, 2), _myexceldata(x, 3))

                    Else

                        If InStr(_myexceldata(x, 1), "[x]") > 0 Then

                            If Val(Mid(_myexceldata(x, 1), InStr(_myexceldata(x, 1), "[x]_[") + 5, Len(_myexceldata(x, 1)) - 1)) <= Val(ComboBox1.Text) Then

                                CheckAndAddSignals(_myexceldata(x, 1), _myexceldata(x, 2), _myexceldata(x, 3))

                            End If

                        End If

                    End If

                Next

            Case "User Created CSV File (Signal, Device, Raster)"

                fnum = FreeFile()

                FileOpen(fnum, filename, OpenMode.Input)

                Do While Not EOF(fnum)
                    textline = LineInput(fnum)
                    breakout = Split(textline, ",")
                    If ComboBox1.Text = "ALL" Then

                        CheckAndAddSignals(breakout(0), breakout(1), breakout(2))

                    Else

                        If InStr(breakout(0), "[x]") > 0 Then

                            If Val(Mid(breakout(0), InStr(breakout(0), "[x]_[") + 5, Len(breakout(0)) - 1)) <= Val(ComboBox1.Text) Then

                                CheckAndAddSignals(breakout(0), breakout(1), breakout(2))
                            End If

                        End If

                    End If
                Loop
                FileClose(fnum)

        End Select

        GridCellPropConfig._changesMade = True
        Cursor = Cursors.Arrow
        Label6.Text = "Adding Signals Complete"
        Label6.Refresh()

        Button4.Enabled = False
        Button5.Enabled = False
        ComboBox1.Enabled = False

    End Sub

    Private Sub AddRecordOnlySignalsButton()

        Dim z As Integer
        Dim y As Integer

        'HC CHANGE
        If InStr(ListBox1.SelectedItem.ToString, "ACP") > 0 Or
            InStr(ListBox1.SelectedItem.ToString, "XCP:1") > 0 Or
            InStr(ListBox1.SelectedItem.ToString, "ETK") > 0 Then

            If ListBox2.SelectedIndex < 0 Then

                MsgBox("Please select a default raster for the higlighted signals.")
                Exit Sub

            End If

        End If

        Cursor = Cursors.WaitCursor

        Label6.Text = "Adding Signals..."
        Label6.Refresh()

        y = 0
        For z = 0 To ListBox3.Items.Count - 1

            If ListBox3.GetSelected(z) = True Then

                If ListBox2.SelectedIndex > -1 Then

                    CheckAndAddSignals(ListBox3.Items(z).ToString,
                    ListBox1.Items(ListBox1.SelectedIndex).ToString,
                    ListBox2.Items(ListBox2.SelectedIndex).ToString)
                Else
                    CheckAndAddSignals(ListBox3.Items(z).ToString,
                    ListBox1.Items(ListBox1.SelectedIndex).ToString, "")
                End If


            End If

        Next

        GridCellPropConfig._changesMade = True

        Cursor = Cursors.Arrow

        Label6.Text = "Adding Signals Complete"

    End Sub

    Private Sub ListBox3_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox3.SelectedIndexChanged

        'Listbox3 is the list of signals obtained from a user selected device

        Button3.Enabled = True
        Button5.Enabled = True
        Label6.Text = ""
    End Sub

    Private Async Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged

        'Listbox1 is the list of devices available from INCA

        Dim y As Integer

        Dim tempstrarray() As String

        Try

            If ListBox1.SelectedIndex = -1 Then
                Exit Sub
            End If

            Cursor = Cursors.WaitCursor

            If MyIncaInterface.DeviceDataRetrieved = False Then

                Label6.Text = "Retrieving Device / Signal / Raster information..."
                Refresh()

                Await MyIncaInterface.GetDeviceAcquisitionRatesAsync()

            Else

                Label6.Text = "Retrieving Signal / Raster information for " & ListBox1.SelectedItem.ToString
                Refresh()

            End If

            If MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).rasters IsNot Nothing Then

                ReDim tempstrarray(0)

                For y = 0 To UBound(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).rasters)
                    ReDim Preserve tempstrarray(y)
                    tempstrarray(y) = MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).rasters(y).rastername

                Next y

                ListBox2.Items.Clear()

                ListBox2.Items.AddRange(tempstrarray)

                ReDim tempstrarray(0)

                If MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo IsNot Nothing Then

                    For y = 0 To UBound(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo)
                        ReDim Preserve tempstrarray(y)
                        tempstrarray(y) = MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo(y).variablename

                    Next y

                    If _suppressListLoad = False Then

                        ListBox3.Items.Clear()
                        ListBox3.Items.AddRange(tempstrarray)

                        Label5.Text = "Available Signals List (From Processor Query)"

                    End If

                    Label6.Text = "Retrieving Device / Signal / Raster information Complete."

                    Refresh()

                Else

                    Label6.Text = "No Variables found for " & MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).rasters(y).rastername

                    Refresh()

                End If
            Else

                Label6.Text = "No Rasters found for " & ListBox1.SelectedItem.ToString

                Refresh()

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "AddRecordOnlySignals - Listbox1.SelectedIndexChanged: " & ex.Message, DisplayMsgBox)

        Finally

            Cursor = Cursors.Arrow

        End Try

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click

        'This is the Add Signals Button

        AddRecordOnlySignalsButton()

    End Sub

    Private Async Sub AddRecordOnlySignals_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        'This is the first code executed on form load.  Clears the listboxes and gets the available devices from INCA and adds them
        'to listbox1

        Dim mydevices() As IGM_INCA_Comm.INCADeviceStatus

        ComboBox1.Text = "ALL"

        ListBox1.Items.Clear()
        ListBox2.Items.Clear()
        ListBox3.Items.Clear()

        mydevices = Await MyIncaInterface.GetAvailableDevicesAsync(False)

        If mydevices IsNot Nothing Then
            For x = 0 To UBound(mydevices)
                ListBox1.Items.Add(mydevices(x).myName)
            Next x
        End If

        ComboBox1.Enabled = False
        Button4.Enabled = False
        Button5.Enabled = False

    End Sub

    Private Sub ListBox2_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox2.SelectedIndexChanged

        'ListBox2 is the list of available rasters based on the device selected by the user

        Dim z As Integer

        'If the selected device is a processor, we dont need to do anything more.

        'HC CHANGE
        If InStr(ListBox1.SelectedItem.ToString, "ACP") > 0 Or
        InStr(ListBox1.SelectedItem.ToString, "XCP:1") > 0 Or
        InStr(ListBox1.SelectedItem.ToString, "ETK") > 0 Then

            Exit Sub
        End If

        'If the selected device is anything but a processor, we need to determine the list of signals associated with
        'the selected raster and display them in listbox3

        Label6.Text = ""
        Cursor = Cursors.WaitCursor
        Label6.Text = "Retrieving Signals"
        ListBox3.Items.Clear()
        Refresh()

        For z = 0 To UBound(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo)

            If MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo(z).defaultrastername = ListBox2.SelectedItem.ToString Then

                ListBox3.Items.Add(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo(z).variablename)
                ListBox3.Refresh()

            End If

        Next z

        Label6.Text = "Retrieving Signals Complete"
        Refresh()
        Cursor = Cursors.Arrow

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'This is the EXIT button

        Close()
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        'This is the EXIT button

        Close()
    End Sub

    Private Sub ListBox4_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox4.SelectedIndexChanged

    End Sub

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label1.Click

    End Sub

    Private Sub CheckBox1_CheckedChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox1.CheckedChanged

    End Sub

    Private Sub ListBox4_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ListBox4.SelectedValueChanged

        Dim filename As String
        Dim fnum As Integer
        Dim textline As String
        'Dim breakout() As String

        'Dim excelApp As Excel.Application
        Dim excelApp As Object
        'Dim wrkbk As Excel.Workbook
        Dim wrkbk As Object
        'Dim myWorkSheet As Excel.Worksheet
        Dim myWorkSheet As Object

        Dim x As Integer

        ListBox3.Items.Clear()

        _suppressListLoad = False
        ComboBox1.Enabled = False
        Button4.Enabled = False
        Button5.Enabled = False

        OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath

        OpenFileDialog1.FileName = ""

        Select Case ListBox4.SelectedItem.ToString

            Case "INCA Generated LAB File"
                OpenFileDialog1.Filter = "lab | *.lab"
                OpenFileDialog1.DefaultExt = "lab"
            Case "INCA Generated XLS File"
                OpenFileDialog1.Filter = "xls | *.xls"
                OpenFileDialog1.DefaultExt = "xls"
            Case "User Created XLSX File (Signal, Device, Raster)", "User Created XLSX File (Signal Name Only)"
                OpenFileDialog1.Filter = "xlsx | *.xlsx"
                OpenFileDialog1.DefaultExt = "xlsx"
            Case "User Created CSV File (Signal, Device, Raster)"
                OpenFileDialog1.Filter = "csv | *.csv"
                OpenFileDialog1.DefaultExt = "csv"

        End Select

        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then

            If Len(OpenFileDialog1.FileName) > 0 Then

                Cursor = Cursors.WaitCursor

                Refresh()

                filename = OpenFileDialog1.FileName

                Label6.Text = filename & " Selected..."

                If InStr(OpenFileDialog1.DefaultExt, "xlsx") > 0 Then

                    'excelApp = New Excel.Application
                    excelApp = CreateObject("Excel.Application")

                    wrkbk = excelApp.Workbooks.Open(filename)
                    myWorkSheet = wrkbk.Sheets(1)
                    myWorkSheet.Activate()

                    _myexceldata = myWorkSheet.UsedRange.Value

                    'signal, device , raster or
                    'signal name only...

                    If UBound(_myexceldata, 2) = 1 Then

                        ListBox3.Items.Clear()
                        Button5.Enabled = True

                        For x = 1 To UBound(_myexceldata, 1)

                            If VB.Left(_myexceldata(x, 1), 2) = "Va" And InStr(_myexceldata(x, 1), "[x]") = 0 Then
                                _myexceldata(x, 1) = _myexceldata(x, 1) & "[x]_[0]"
                            ElseIf InStr(_myexceldata(x, 1), "[x]") > 0 And InStr(_myexceldata(x, 1), "_[") = 0 Then
                                _myexceldata(x, 1) = _myexceldata(x, 1) & "_[0]"
                            End If

                            ListBox3.Items.Add(_myexceldata(x, 1))
                        Next x

                        _suppressListLoad = True

                        Label5.Text = "Signals from selected Excel Spreadsheet File"

                    Else

                        ComboBox1.Enabled = True
                        Button4.Enabled = True

                    End If

                    excelApp.Quit()
                    excelApp = Nothing

                ElseIf InStr(OpenFileDialog1.DefaultExt, "xls") > 0 Then

                    'excelApp = New Excel.Application
                    excelApp = CreateObject("Excel.Application")

                    wrkbk = excelApp.Workbooks.Open(filename)

                    excelApp = CreateObject("Excel.Application")
                    wrkbk = excelApp.Workbooks.Open(filename)

                    myWorkSheet = wrkbk.Sheets(1)
                    myWorkSheet.Activate()

                    _myexceldata = myWorkSheet.UsedRange.Value

                    If UBound(_myexceldata, 2) = 1 Then

                        ListBox3.Items.Clear()
                        Button5.Enabled = True

                        For x = 1 To UBound(_myexceldata, 1)

                            If VB.Left(_myexceldata(x, 1), 2) = "Va" And InStr(_myexceldata(x, 1), "[x]") = 0 Then
                                _myexceldata(x, 1) = _myexceldata(x, 1) & "[x]_[0]"
                            ElseIf InStr(_myexceldata(x, 1), "[x]") > 0 And InStr(_myexceldata(x, 1), "_[") = 0 Then
                                _myexceldata(x, 1) = _myexceldata(x, 1) & "_[0]"
                            End If

                            ListBox3.Items.Add(_myexceldata(x, 1))
                        Next x

                        _suppressListLoad = True

                        Label5.Text = "Signals from selected Excel Spreadsheet File"

                    Else

                        ComboBox1.Enabled = True
                        Button4.Enabled = True


                    End If

                    excelApp.Quit()
                    excelApp = Nothing

                ElseIf InStr(OpenFileDialog1.DefaultExt, "lab") > 0 Then

                    ListBox3.Items.Clear()
                    Button5.Enabled = True

                    fnum = FreeFile()

                    FileOpen(fnum, filename, OpenMode.Input)

                    LineInput(fnum)
                    Do While Not EOF(fnum)

                        If VB.Left(LineInput(fnum), 2) = "Va" And InStr(LineInput(fnum), "[x]") = 0 Then
                            textline = LineInput(fnum) & "[x]_[0]"
                        ElseIf InStr(LineInput(fnum), "[x]") > 0 And InStr(LineInput(fnum), "_[") = 0 Then
                            textline = LineInput(fnum) & "_[0]"
                        Else
                            textline = LineInput(fnum)
                        End If

                        ListBox3.Items.Add(textline)
                    Loop

                    FileClose(fnum)

                    _suppressListLoad = True

                    Label5.Text = "Signals from selected LAB File"

                ElseIf InStr(OpenFileDialog1.DefaultExt, "csv") > 0 Then

                    Cursor = Cursors.WaitCursor

                    ComboBox1.Enabled = True
                    Button4.Enabled = True

                Else
                    MsgBox("You must select a valid Signal File to continue.")
                End If
            Else
                MsgBox("You must select a valid Signal File to continue.")
            End If
        Else
            MsgBox("You must select a valid Signal File to continue.")
        End If

        Cursor = Cursors.Arrow

        Refresh()

    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click

        'This is the Add Signals Button located in the upper right half of the screen...

        AddRecordOnlySignalsButton()
    End Sub

    Private Sub Button4_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click

        'This is the Add Signals Button located in the lower center part of the screen...

        AddUserSelectedSignalsButton()

    End Sub


    Private Sub RadioButton1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton1.CheckedChanged

    End Sub
End Class