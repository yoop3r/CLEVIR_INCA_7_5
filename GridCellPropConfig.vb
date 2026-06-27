
Imports System.Threading.Tasks

Public Class GridCellPropConfig

    'This is the Grid Cell Properties Configuration form.  This is accessed by a double click in any
    'grid cell on any of the display grids that have been dynamically created based on the contents of
    'the INCAVariableFile excel spreadsheet.  This allows the user to change the information associated
    'with the selected grid cell.

    'While most of the code is shared between the CLEVIR_INCA_7_2 and CLEVIR_INCA_7_3 versions, there are different
    'GridCellPropConfig modules used for the two CLEVIR versions.  This is due to the fact that the handling of configured
    'displays must be different between 7.2 and 7.3, and much of the code related to the configured displays is in this
    'module .So, whenever any changes are made to GridCellPropConfig, the same changes must be made to both the 7.2 version
    'which is located in FlexGridFiles and the 7.3 version which is located in in NoFlexGridFiles...

    Public _changesMade As Boolean
    Public MySenderObject As GridDataClass

    Private _displayWindowNameTextChanged As Boolean
    Private _addSignalAsArray As Boolean

    Private Sub Formloadsub()

        'When the form is loaded, some preliminary setup is performed.

        PictureBox2.Parent = PictureBox1
        TextBox1.Parent = PictureBox2

        Label1.Parent = PictureBox2
        Label2.Parent = PictureBox2
        Label3.Parent = PictureBox2
        Label4.Parent = PictureBox2
        Label5.Parent = PictureBox2
        Label6.Parent = PictureBox2
        Label7.Parent = PictureBox2
        Label8.Parent = PictureBox2
        Label9.Parent = PictureBox2
        Label10.Parent = PictureBox2
        Label11.Parent = PictureBox2
        Label12.Parent = PictureBox2
        Label13.Parent = PictureBox2
        'Label14.Parent = PictureBox2
        Label15.Parent = PictureBox2
        Label16.Parent = PictureBox2
        Label17.Parent = PictureBox2
        Label18.Parent = PictureBox2
        'Label19.Parent = PictureBox2
        Label20.Parent = PictureBox2

        VariableName.Parent = PictureBox2
        DeviceName.Parent = PictureBox2
        RasterName.Parent = PictureBox2
        DefaultBackColorCombo.Parent = PictureBox2
        DefaultForeColorCombo.Parent = PictureBox2
        HighThreshBackColor.Parent = PictureBox2
        LowThreshBackColor.Parent = PictureBox2
        HighThreshForeColor.Parent = PictureBox2
        LowThreshForeColor.Parent = PictureBox2
        HighThreshold.Parent = PictureBox2
        LowThreshold.Parent = PictureBox2
        'Row.Parent = PictureBox2
        'Column.Parent = PictureBox2
        EqualTo.Parent = PictureBox2
        AlsoAssocWith.Parent = PictureBox2
        DisplayFormat.Parent = PictureBox2
        CheckForDataChange.Parent = PictureBox2
        DisplayName.Parent = PictureBox2
        'DisplayGroupName.Parent = PictureBox2
        ControlName.Parent = PictureBox2
        DisplayWindowName.Parent = PictureBox2

        PictureBox2.Left = 0

        VScrollBar1.Top = PictureBox1.Top

    End Sub

    Private Sub AddNewSignalToList(ByVal column As Integer)

        'Called from SaveChangesToGridObject - This is only called if the devicename, rastername or variablename
        'has been changed during the editing session.  This routine adds a new signal, in the
        'proper location, to the internal myPreliminaryDisplaySignals array.  This information will
        'be saved into the variable / display configuration excel spreadsheet upon exiting the app.

        ReDim Preserve MyIncaInterface.myPreliminaryDisplaySignals(UBound(MyIncaInterface.myPreliminaryDisplaySignals) + 1)
        MyIncaInterface.myPreliminaryDisplaySignals(UBound(MyIncaInterface.myPreliminaryDisplaySignals)).DeviceName = DeviceName.Text
        MyIncaInterface.myPreliminaryDisplaySignals(UBound(MyIncaInterface.myPreliminaryDisplaySignals)).RasterName = RasterName.Text
        If _addSignalAsArray = True Then
            MyIncaInterface.myPreliminaryDisplaySignals(UBound(MyIncaInterface.myPreliminaryDisplaySignals)).SignalName = VariableName.Text & "_[" & column - 1 & "]"
        Else
            MyIncaInterface.myPreliminaryDisplaySignals(UBound(MyIncaInterface.myPreliminaryDisplaySignals)).SignalName = VariableName.Text
        End If

        MyIncaInterface.myPreliminaryDisplaySignals(UBound(MyIncaInterface.myPreliminaryDisplaySignals)).Status = "Invalid"
        MyIncaInterface.myPreliminaryDisplaySignals(UBound(MyIncaInterface.myPreliminaryDisplaySignals)).ForceRegister = True

        MySenderObject.SignalIndex(MySenderObject.CurrentCell.RowIndex, column) = UBound(MyIncaInterface.myPreliminaryDisplaySignals)

    End Sub

    'Private Function SaveChangesToGridObject() As Boolean

    '    'This routine takes the values in the text boxes (which may or may not have been changed by the user during
    '    're-configuration) and copies the values into the grid object.  This routine is called from 
    '    'both the [Save Changes], [Save and Exit], and [Save, Register, Exit] buttons.

    '    Dim ccon As ColorConverter
    '    Dim z As Integer
    '    Dim x As Integer
    '    Dim signalChanged As Boolean

    '    Dim maxColumn As Integer

    '    SaveChangesToGridObject = False

    '    ccon = New ColorConverter()

    '    If Len(DeviceName.Text) = 0 Or Len(RasterName.Text) = 0 Or Len(VariableName.Text) = 0 Then
    '        MsgBox("Please make sure that you have a valid DeviceName, RasterName and VariableName associated with this grid cell.")
    '    Else

    '        'commented this out for now, not sure how to handle this...

    '        'For x = 0 To UBound(MyIncaInterface.myPreliminaryDisplaySignals)
    '        'If MyIncaInterface.myPreliminaryDisplaySignals(x).DeviceName = DeviceName.Text And _
    '        '    MyIncaInterface.myPreliminaryDisplaySignals(x).SignalName = VariableName.Text Then

    '        'MsgBox("The Signal/Device pair " & VariableName.Text & "/" & DeviceName.Text & " is being used elsewhere, please select a unique VariableName/Devicename pair for this grid cell.")

    '        ' Exit Function
    '        'End If
    '        'Next

    '        If MySenderObject.VariableName(MySenderObject.CurrentCell.RowIndex, MySenderObject.CurrentCell.ColumnIndex) <> Me.VariableName.Text Or
    '           MySenderObject.DeviceName(MySenderObject.CurrentCell.RowIndex, MySenderObject.CurrentCell.ColumnIndex) <> Me.DeviceName.Text Or
    '           MySenderObject.Raster(MySenderObject.CurrentCell.RowIndex, MySenderObject.CurrentCell.ColumnIndex) <> Me.RasterName.Text Then

    '            signalChanged = True

    '        End If

    '        If _addSignalAsArray = True Then
    '            maxColumn = MySenderObject.ColumnCount - 1
    '            'mySenderObject.Col = 1

    '            MySenderObject.CurrentCell = MySenderObject(MySenderObject.CurrentCell.RowIndex, 1)

    '        Else
    '            maxColumn = MySenderObject.CurrentCell.ColumnIndex
    '        End If

    '        For x = MySenderObject.CurrentCell.ColumnIndex To maxColumn

    '            MySenderObject.AlsoAssociatedWith(MySenderObject.CurrentCell.RowIndex, x) = Me.AlsoAssocWith.Text
    '            MySenderObject.DisplayFormat(MySenderObject.CurrentCell.RowIndex, x) = Me.DisplayFormat.Text

    '            If InStr(Me.VariableName.Text, "[x]") = 0 Then
    '                MySenderObject.VariableName(MySenderObject.CurrentCell.RowIndex, x) = Me.VariableName.Text
    '            Else
    '                MySenderObject.VariableName(MySenderObject.CurrentCell.RowIndex, x) = Me.VariableName.Text & "_[" & x - 1 & "]"
    '            End If

    '            MySenderObject.DisplayName(MySenderObject.CurrentCell.RowIndex, x) = Me.DisplayName.Text
    '            MySenderObject.HighThresh(MySenderObject.CurrentCell.RowIndex, x) = CDbl(Me.HighThreshold.Text)
    '            MySenderObject.LowThresh(MySenderObject.CurrentCell.RowIndex, x) = CDbl(Me.LowThreshold.Text)

    '            MySenderObject.Parent.Text = Me.DisplayWindowName.Text
    '            MySenderObject.Parent.Name = Me.DisplayWindowName.Text
    '            MySenderObject.Name = Me.ControlName.Text

    '            MySenderObject.CheckForDataChange(MySenderObject.CurrentCell.RowIndex, x) = CBool(IIf(Me.CheckForDataChange.Text = "False", False, True))

    '            MySenderObject.EqualTo(MySenderObject.CurrentCell.RowIndex, x) = Me.EqualTo.Text
    '            MySenderObject.HighThreshBackColor(MySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.HighThreshBackColor.Text), Color)
    '            MySenderObject.HighThreshForeColor(MySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.HighThreshForeColor.Text), Color)
    '            MySenderObject.LowThreshBackColor(MySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.LowThreshBackColor.Text), Color)
    '            MySenderObject.LowThreshForeColor(MySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.LowThreshForeColor.Text), Color)

    '            MySenderObject.DeviceName(MySenderObject.CurrentCell.RowIndex, x) = Me.DeviceName.Text
    '            MySenderObject.Raster(MySenderObject.CurrentCell.RowIndex, x) = Me.RasterName.Text

    '            If MySenderObject.DisplayName(1, 1) <> "undefined" Then
    '                MySenderObject.DefaultCellBackColor(MySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.DefaultBackColorCombo.Text), Color)
    '                MySenderObject.DefaultCellForeColor(MySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.DefaultForeColorCombo.Text), Color)

    '                If MySenderObject.CurrentCell.ColumnIndex > 0 Then
    '                    'mySenderObject.CellBackColor = mySenderObject.DefaultCellBackColor(mySenderObject.CurrentCell.RowIndex, x) 'REMOVED FOR 64BIT COMPILE
    '                    'mySenderObject.CellForeColor = mySenderObject.DefaultCellForeColor(mySenderObject.CurrentCell.RowIndex, x)  'REMOVED FOR 64BIT COMPILE
    '                End If

    '            End If

    '            If _displayWindowNameTextChanged = True Then
    '                _displayWindowNameTextChanged = False
    '                For z = 0 To GmResidentClient.MyDFs.Count - 1
    '                    If GmResidentClient.MyDFs(z).Name = Me.DisplayWindowName.Text Then
    '                        If GmResidentClient.MyDFs(z).GoNoGoIndex > -1 Then
    '                            GmResidentClient.MyLabel(GmResidentClient.MyDFs(z).GoNoGoIndex).Text = Me.DisplayWindowName.Text
    '                        End If
    '                        GmResidentClient.MyToolStripMenuItem.DropDownItems(z + GmResidentClient.NumPredefinedDisplays).Text = Me.DisplayWindowName.Text
    '                        Exit For
    '                    End If
    '                Next z

    '            End If

    '            If signalChanged = True Then
    '                AddNewSignalToList(x)
    '            End If

    '        Next x

    '        'mySenderObject.Col = 0

    '        MySenderObject.CurrentCell = MySenderObject(MySenderObject.CurrentCell.RowIndex, 0)


    '        MySenderObject.Text = Me.DisplayName.Text

    '        _changesMade = True
    '        SaveChangesToGridObject = True

    '        Me.Button1.Enabled = False
    '        Me.Button3.Enabled = False
    '        Me.Button5.Enabled = False

    '    End If

    'End Function

    Private Function SaveChangesToGridObject() As Boolean
        ' Early exit if required text fields are empty
        If String.IsNullOrEmpty(DeviceName.Text) OrElse
       String.IsNullOrEmpty(RasterName.Text) OrElse
       String.IsNullOrEmpty(VariableName.Text) Then
            MsgBox("Please make sure that you have a valid DeviceName, RasterName and VariableName associated with this grid cell.")
            Return False
        End If

        Dim ccon As New ColorConverter()
        Dim rowIndex As Integer = MySenderObject.CurrentCell.RowIndex
        Dim colStart As Integer = MySenderObject.CurrentCell.ColumnIndex
        Dim maxColumn As Integer
        Dim signalChanged As Boolean = False

        ' Cache values from the form
        Dim newDeviceName As String = Me.DeviceName.Text
        Dim newRasterName As String = Me.RasterName.Text
        Dim newVariableName As String = Me.VariableName.Text
        Dim newDisplayName As String = Me.DisplayName.Text
        Dim newDisplayWindowName As String = Me.DisplayWindowName.Text
        Dim newControlName As String = Me.ControlName.Text
        Dim newAlsoAssocWith As String = Me.AlsoAssocWith.Text
        Dim newDisplayFormat As String = Me.DisplayFormat.Text
        Dim newHighThreshold As Double = CDbl(Me.HighThreshold.Text)
        Dim newLowThreshold As Double = CDbl(Me.LowThreshold.Text)
        Dim newCheckForDataChange As Boolean = (Me.CheckForDataChange.Text <> "False")
        Dim newEqualTo As String = Me.EqualTo.Text

        ' Convert colors only once
        Dim highThreshBackColor As Color = DirectCast(ccon.ConvertFromString(Me.HighThreshBackColor.Text), Color)
        Dim highThreshForeColor As Color = DirectCast(ccon.ConvertFromString(Me.HighThreshForeColor.Text), Color)
        Dim lowThreshBackColor As Color = DirectCast(ccon.ConvertFromString(Me.LowThreshBackColor.Text), Color)
        Dim lowThreshForeColor As Color = DirectCast(ccon.ConvertFromString(Me.LowThreshForeColor.Text), Color)

        Dim defaultBackColor As Color = Color.Empty
        Dim defaultForeColor As Color = Color.Empty
        If MySenderObject.DisplayName(1, 1) <> "undefined" Then
            defaultBackColor = DirectCast(ccon.ConvertFromString(Me.DefaultBackColorCombo.Text), Color)
            defaultForeColor = DirectCast(ccon.ConvertFromString(Me.DefaultForeColorCombo.Text), Color)
        End If

        ' Check if signal identifiers have changed
        If MySenderObject.VariableName(rowIndex, colStart) <> newVariableName OrElse
       MySenderObject.DeviceName(rowIndex, colStart) <> newDeviceName OrElse
       MySenderObject.Raster(rowIndex, colStart) <> newRasterName Then
            signalChanged = True
        End If

        ' Determine the loop range. If _addSignalAsArray is True, then update the entire row
        If _addSignalAsArray Then
            maxColumn = MySenderObject.ColumnCount - 1
            colStart = 1
            MySenderObject.CurrentCell = MySenderObject(rowIndex, colStart)
        Else
            maxColumn = colStart
        End If

        ' Determine whether VariableName uses an array pattern
        Dim isArraySignal As Boolean = (InStr(newVariableName, "[x]") <> 0)

        ' Update parent properties (only once)
        MySenderObject.Parent.Text = newDisplayWindowName
        MySenderObject.Parent.Name = newDisplayWindowName
        MySenderObject.Name = newControlName

        ' Loop through the applicable columns to update each cell
        For x As Integer = colStart To maxColumn
            MySenderObject.AlsoAssociatedWith(rowIndex, x) = newAlsoAssocWith
            MySenderObject.DisplayFormat(rowIndex, x) = newDisplayFormat

            If Not isArraySignal Then
                MySenderObject.VariableName(rowIndex, x) = newVariableName
            Else
                MySenderObject.VariableName(rowIndex, x) = newVariableName & "_[" & (x - 1).ToString() & "]"
            End If

            MySenderObject.DisplayName(rowIndex, x) = newDisplayName
            MySenderObject.HighThresh(rowIndex, x) = newHighThreshold
            MySenderObject.LowThresh(rowIndex, x) = newLowThreshold

            MySenderObject.CheckForDataChange(rowIndex, x) = newCheckForDataChange
            MySenderObject.EqualTo(rowIndex, x) = newEqualTo
            MySenderObject.HighThreshBackColor(rowIndex, x) = highThreshBackColor
            MySenderObject.HighThreshForeColor(rowIndex, x) = highThreshForeColor
            MySenderObject.LowThreshBackColor(rowIndex, x) = lowThreshBackColor
            MySenderObject.LowThreshForeColor(rowIndex, x) = lowThreshForeColor
            MySenderObject.DeviceName(rowIndex, x) = newDeviceName
            MySenderObject.Raster(rowIndex, x) = newRasterName

            If MySenderObject.DisplayName(1, 1) <> "undefined" Then
                MySenderObject.DefaultCellBackColor(rowIndex, x) = defaultBackColor
                MySenderObject.DefaultCellForeColor(rowIndex, x) = defaultForeColor
                ' These lines remain commented out as in the original code:
                'If colStart > 0 Then
                '    MySenderObject.CellBackColor = MySenderObject.DefaultCellBackColor(rowIndex, x)
                '    MySenderObject.CellForeColor = MySenderObject.DefaultCellForeColor(rowIndex, x)
                'End If
            End If

            ' Update display window names if they have changed (do this only once)
            If _displayWindowNameTextChanged Then
                _displayWindowNameTextChanged = False
                For z As Integer = 0 To GmResidentClient.MyDFs.Count - 1
                    If GmResidentClient.MyDFs(z).Name = newDisplayWindowName Then
                        If GmResidentClient.MyDFs(z).GoNoGoIndex > -1 Then
                            GmResidentClient.MyLabel(GmResidentClient.MyDFs(z).GoNoGoIndex).Text = newDisplayWindowName
                        End If
                        GmResidentClient.MyToolStripMenuItem.DropDownItems(z + GmResidentClient.NumPredefinedDisplays).Text = newDisplayWindowName
                        Exit For
                    End If
                Next
            End If

            ' If the signal has changed, update the list for each column as needed
            If signalChanged Then
                AddNewSignalToList(x)
            End If
        Next

        ' Reset the current cell to column 0 and update the grid's text
        MySenderObject.CurrentCell = MySenderObject(rowIndex, 0)
        MySenderObject.Text = newDisplayName

        _changesMade = True
        SaveChangesToGridObject = True

        ' Disable buttons now that changes have been saved
        Me.Button1.Enabled = False
        Me.Button3.Enabled = False
        Me.Button5.Enabled = False

        Return True

    End Function

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        'This is the exit without saving button....

        If Me.Button1.Enabled = True Then
            If MsgBox("Your Changes will not be saved!", vbOKCancel) = vbOK Then
                Me.Hide()
            End If
        Else
            Me.Hide()
        End If

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'This is the save changes button

        SaveChangesToGridObject()

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click

        'This is the exit and save button...

        SaveChangesToGridObject()

        Me.Hide()

    End Sub

    Private Sub VScrollBar1_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs)

        PictureBox2.Top = -VScrollBar1.Value
    End Sub

    Private Sub VScrollBar1_Scroll_1(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles VScrollBar1.Scroll

        If PictureBox2.Top = 0 Then
            PictureBox2.Parent = PictureBox1
            PictureBox2.Left = 0
        End If


        PictureBox2.Top = -VScrollBar1.Value
    End Sub

    Private Sub GridCellPropConfig_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated

    End Sub

    Private Sub GridCellPropConfig_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        'Once this form is opened, it should remain open and be hidden until required again.  This
        'will eliminate the need to reset the displays and re-populate the signal lists, which takes
        'a lot of time.  So, if the red X is pressed, we will ignore the close request and just hide
        'the form.

        Me.Hide()
        e.Cancel = True

    End Sub

    Private Sub GridCellPropConfig_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Formloadsub()

    End Sub

    Private Sub GridCellPropConfig_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDoubleClick

    End Sub

    Private Sub GridCellPropConfig_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp

    End Sub

    Private Sub GridCellPropConfig_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize

        'TBD

        Static saveMyWidth As Integer
        Static saveMyHeight As Integer

        If saveMyWidth = 0 Then
            saveMyWidth = Me.Width
        Else
            Me.Width = saveMyWidth
        End If
        If saveMyHeight = 0 Then
            saveMyHeight = Me.Height
        End If

        If Me.Height > saveMyHeight Then
            Me.Height = saveMyHeight
        End If

        GroupBox1.Height = Me.Height - 25
        PictureBox1.Height = GroupBox1.Height - 30
        VScrollBar1.Height = Me.Height - 95

    End Sub

    Private Sub VariableName_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub DisplayName_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Button3_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Button3.MouseUp

    End Sub

    Private Sub Button3_Move(ByVal sender As Object, ByVal e As System.EventArgs) Handles Button3.Move

    End Sub

    Private Sub DeviceName_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles DeviceName.Click

        'Enable the save buttons...

        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub DeviceName_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DeviceName.SelectedIndexChanged

    End Sub

    Private Async Sub DeviceName_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DeviceName.SelectedValueChanged

        'When a new devicename selection is made on the configuration form (GridCellPropConfig), 
        'we must re-populate the available rasternames and variable names based on the newly selected
        'device name....

        Dim y As Integer
        Dim tempstrarray() As String

        If Me.Label14.Visible = False Then

            RasterName.Text = ""
            VariableName.Text = ""

            GmResidentClient.Cursor = Cursors.WaitCursor

            Label14.Text = "Performing Variable Browsing Operation..."
            Label14.Visible = True

            Me.Refresh()

            Me.RasterName.Items.Clear()
            Me.VariableName.Items.Clear()

            If MyIncaInterface.DeviceDataRetrieved = False Then
                Await MyIncaInterface.GetDeviceAcquisitionRatesAsync()
            End If

            ReDim tempstrarray(0)

            For y = 0 To UBound(MyIncaInterface.deviceinfo(DeviceName.SelectedIndex).rasters)
                ReDim Preserve tempstrarray(y)
                tempstrarray(y) = MyIncaInterface.deviceinfo(DeviceName.SelectedIndex).rasters(y).rastername

            Next y

            RasterName.Items.AddRange(tempstrarray)

            ReDim tempstrarray(0)

            For y = 0 To UBound(MyIncaInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo)
                ReDim Preserve tempstrarray(y)
                tempstrarray(y) = MyIncaInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo(y).variablename

            Next y

            VariableName.Items.AddRange(tempstrarray)

            Me.Label14.Visible = False
            GmResidentClient.Cursor = Cursors.Arrow

        End If

    End Sub

    Private Sub VariableName_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles VariableName.Click

        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub VariableName_SelectedIndexChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles VariableName.SelectedIndexChanged

    End Sub

    Private Sub RasterName_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RasterName.Click

        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub RasterName_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RasterName.SelectedIndexChanged

    End Sub

    Private Sub RasterName_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles RasterName.SelectedValueChanged

        'HC CHANGE
        If InStr(DeviceName.Text, "ACP") > 0 Or
            InStr(DeviceName.Text, "XCP:1") > 0 Or
            InStr(DeviceName.Text, "ETK") > 0 Then

            Exit Sub
        End If

        VariableName.Items.Clear()

        Me.Refresh()

        For z = 0 To UBound(MyIncaInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo)

            If MyIncaInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo(z).defaultrastername = RasterName.SelectedItem.ToString Then

                VariableName.Items.Add(MyIncaInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo(z).variablename)
                VariableName.Refresh()

            End If

        Next z


    End Sub

    Private Sub VariableName_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles VariableName.SelectedValueChanged

        'When a new variablename is selected from the drop down list, we figure out what the default raster is for this variable in INCA
        'and populate the raster name text box with this name.  It can always be changed later, but in the case of the CAN Monitor,
        'these raster names are fixed and must be the same as the default.  This makes it so that we do not have to know the raster name
        'associated with the signal selected, it figures this out automatically using the GetDefaultRasterForMeasureElementInDevice call....

        Dim x As Integer

        _addSignalAsArray = False

        If Len(Me.VariableName.Text) > 0 Then

            Me.RasterName.Text = MyIncaInterface.GetDefaultRasterForMeasureElementInDevice(Me.DeviceName.Text, Me.VariableName.Text)
            Me.DisplayName.Text = Me.VariableName.Text

            For x = 0 To myDGs.Count - 1
                If GridToModify = myDGs(x).Name Then

                    If myDGs(x).ColumnCount >= 3 And InStr(Me.VariableName.Text, "[x]") > 0 And myDGs(x).CurrentCell.ColumnIndex = 1 Then
                        _addSignalAsArray = True

                    End If

                    Exit For
                End If
            Next x

        End If

    End Sub

    Private Sub EqualTo_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Label19_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub ControlName_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ControlName.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub ControlName_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ControlName.TextChanged

    End Sub

    Private Sub DisplayFormat_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles DisplayFormat.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DisplayFormat.SelectedIndexChanged

    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click

        'This is the restore defaults button.  Restores all values in the text box and dropdown
        'list boxes to the defaults.

        If MsgBox("Restore to Defaults may change existing information related to this variable!  Are you sure you want to restore to default settings?", vbYesNo) = vbYes Then

            VariableName.Text = "undefined"
            DisplayName.Text = "undefined"
            DeviceName.Text = "undefined"
            RasterName.Text = "undefined"
            AlsoAssocWith.Text = ""
            CheckForDataChange.Text = "0"
            DisplayFormat.Text = """0.000"""
            DefaultBackColorCombo.Text = "White"
            DefaultForeColorCombo.Text = "Black"
            HighThreshBackColor.Text = "Red"
            LowThreshBackColor.Text = "Red"
            HighThreshForeColor.Text = "White"
            LowThreshForeColor.Text = "White"
            HighThreshold.Text = "10000000"
            LowThreshold.Text = "-10000000"
            EqualTo.Text = ""

        Else
            'do nothing

        End If

    End Sub

    Private Sub DisplayWindowName_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles DisplayWindowName.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub DisplayWindowName_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DisplayWindowName.TextChanged
        _displayWindowNameTextChanged = True

    End Sub

    Private Sub CheckForDataChange_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckForDataChange.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub CheckForDataChange_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckForDataChange.SelectedIndexChanged

    End Sub

    Private Async Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click

        'This is the Save, Register, Exit button...
        GmResidentClient.Cursor = Cursors.WaitCursor

        If SaveChangesToGridObject() = True Then

            Me.Label14.Text = "Performing Signal Registration..."
            Me.Label14.Visible = True

            SignalRegistrationMode = "DISPLAYS"

            Await Task.Run(Sub() MyIncaInterface.RegisterSignals())

            Me.Label14.Visible = False
        End If

        Me.Hide()

        GmResidentClient.Cursor = Cursors.Arrow

    End Sub

    Private Sub CheckForDataChange_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckForDataChange.SelectedValueChanged

    End Sub

    Private Sub DisplayFormat_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DisplayFormat.SelectedValueChanged

    End Sub

    Private Sub AlsoAssocWith_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles AlsoAssocWith.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub AlsoAssocWith_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AlsoAssocWith.SelectedIndexChanged

    End Sub

    Private Sub AlsoAssocWith_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles AlsoAssocWith.SelectedValueChanged

    End Sub

    Private Sub EqualTo_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub LowThreshold_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles LowThreshold.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub LowThreshold_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LowThreshold.TextChanged

    End Sub

    Private Sub HighThreshold_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles HighThreshold.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub HighThreshold_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles HighThreshold.TextChanged
    End Sub

    Private Sub LowThreshForeColor_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles LowThreshForeColor.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub LowThreshForeColor_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LowThreshForeColor.SelectedIndexChanged

    End Sub

    Private Sub LowThreshForeColor_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles LowThreshForeColor.SelectedValueChanged
    End Sub

    Private Sub HighThreshForeColor_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles HighThreshForeColor.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub HighThreshForeColor_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles HighThreshForeColor.SelectedIndexChanged

    End Sub

    Private Sub HighThreshForeColor_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles HighThreshForeColor.SelectedValueChanged
    End Sub

    Private Sub LowThreshBackColor_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles LowThreshBackColor.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub LowThreshBackColor_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LowThreshBackColor.SelectedIndexChanged

    End Sub

    Private Sub LowThreshBackColor_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles LowThreshBackColor.SelectedValueChanged
    End Sub

    Private Sub HighThreshBackColor_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles HighThreshBackColor.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub HighThreshBackColor_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles HighThreshBackColor.SelectedIndexChanged

    End Sub

    Private Sub HighThreshBackColor_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles HighThreshBackColor.SelectedValueChanged
    End Sub

    Private Sub DefaultForeColorCombo_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles DefaultForeColorCombo.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub DefaultForeColorCombo_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DefaultForeColorCombo.SelectedIndexChanged

    End Sub

    Private Sub DefaultForeColorCombo_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DefaultForeColorCombo.SelectedValueChanged

    End Sub

    Private Sub DefaultBackColorCombo_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles DefaultBackColorCombo.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub DefaultBackColorCombo_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DefaultBackColorCombo.SelectedIndexChanged

    End Sub

    Private Sub DefaultBackColorCombo_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DefaultBackColorCombo.SelectedValueChanged

    End Sub

    Private Sub DisplayName_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles DisplayName.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub DisplayName_TextChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DisplayName.TextChanged

    End Sub

    Private Sub EqualTo_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles EqualTo.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True
    End Sub

    Private Sub EqualTo_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles EqualTo.TextChanged

    End Sub
End Class