Public Class GridCellPropConfig

    'This is the Grid Cell Properties Configuration form.  This is accessed by a double click in any
    'grid cell on any of the display grids that have been dynamically created based on the contents of
    'the INCAVariableFile excel spreadsheet.  This allows the user to change the information associated
    'with the selected grid cell.

    Public ChangesMade As Boolean
    Public mySenderObject As GridDataClass

    Private DisplayWindowNameTextChanged As Boolean
    Private AddSignalAsArray As Boolean

    Private Sub formloadsub()

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

    Private Sub AddNewSignalToList(ByVal Column As Integer)

        'Called from SaveChangesToGridObject - This is only called if the devicename, rastername or variablename
        'has been changed during the editing session.  This routine adds a new signal, in the
        'proper location, to the internal myPreliminaryDisplaySignals array.  This information will
        'be saved into the variable / display configuration excel spreadsheet upon exiting the app.

        ReDim Preserve myINCAInterface.myPreliminaryDisplaySignals(UBound(myINCAInterface.myPreliminaryDisplaySignals) + 1)
        myINCAInterface.myPreliminaryDisplaySignals(UBound(myINCAInterface.myPreliminaryDisplaySignals)).DeviceName = DeviceName.Text
        myINCAInterface.myPreliminaryDisplaySignals(UBound(myINCAInterface.myPreliminaryDisplaySignals)).RasterName = RasterName.Text
        If AddSignalAsArray = True Then
            myINCAInterface.myPreliminaryDisplaySignals(UBound(myINCAInterface.myPreliminaryDisplaySignals)).SignalName = VariableName.Text & "_[" & Column - 1 & "]"
        Else
            myINCAInterface.myPreliminaryDisplaySignals(UBound(myINCAInterface.myPreliminaryDisplaySignals)).SignalName = VariableName.Text
        End If

        myINCAInterface.myPreliminaryDisplaySignals(UBound(myINCAInterface.myPreliminaryDisplaySignals)).Status = "Invalid"
        myINCAInterface.myPreliminaryDisplaySignals(UBound(myINCAInterface.myPreliminaryDisplaySignals)).ForceRegister = True

        mySenderObject.SignalIndex(mySenderObject.CurrentCell.RowIndex, Column) = UBound(myINCAInterface.myPreliminaryDisplaySignals)

    End Sub

    Private Function SaveChangesToGridObject() As Boolean

        'This routine takes the values in the text boxes (which may or may not have been changed by the user during
        're-configuration) and copies the values into the grid object.  This routine is called from 
        'both the [Save Changes], [Save and Exit], and [Save, Register, Exit] buttons.

        Dim ccon As ColorConverter
        Dim z As Integer
        Dim x As Integer
        Dim SignalChanged As Boolean

        Dim MaxColumn As Integer

        SaveChangesToGridObject = False

        ccon = New ColorConverter()

        If Len(DeviceName.Text) = 0 Or Len(RasterName.Text) = 0 Or Len(VariableName.Text) = 0 Then
            MsgBox("Please make sure that you have a valid DeviceName, RasterName and VariableName associated with this grid cell.")
        Else

            'commented this out for now, not sure how to handle this...

            'For x = 0 To UBound(myINCAInterface.myPreliminaryDisplaySignals)
            'If myINCAInterface.myPreliminaryDisplaySignals(x).DeviceName = DeviceName.Text And _
            '    myINCAInterface.myPreliminaryDisplaySignals(x).SignalName = VariableName.Text Then

            'MsgBox("The Signal/Device pair " & VariableName.Text & "/" & DeviceName.Text & " is being used elsewhere, please select a unique VariableName/Devicename pair for this grid cell.")

            ' Exit Function
            'End If
            'Next

            If mySenderObject.VariableName(mySenderObject.CurrentCell.RowIndex, mySenderObject.CurrentCell.ColumnIndex) <> Me.VariableName.Text Or
               mySenderObject.DeviceName(mySenderObject.CurrentCell.RowIndex, mySenderObject.CurrentCell.ColumnIndex) <> Me.DeviceName.Text Or
               mySenderObject.Raster(mySenderObject.CurrentCell.RowIndex, mySenderObject.CurrentCell.ColumnIndex) <> Me.RasterName.Text Then

                SignalChanged = True

            End If

            If AddSignalAsArray = True Then
                MaxColumn = mySenderObject.ColumnCount - 1
                'mySenderObject.Col = 1

                mySenderObject.CurrentCell = mySenderObject(mySenderObject.CurrentCell.RowIndex, 1)

            Else
                MaxColumn = mySenderObject.CurrentCell.ColumnIndex
            End If

            For x = mySenderObject.CurrentCell.ColumnIndex To MaxColumn

                mySenderObject.AlsoAssociatedWith(mySenderObject.CurrentCell.RowIndex, x) = Me.AlsoAssocWith.Text
                mySenderObject.DisplayFormat(mySenderObject.CurrentCell.RowIndex, x) = Me.DisplayFormat.Text

                If InStr(Me.VariableName.Text, "[x]") = 0 Then
                    mySenderObject.VariableName(mySenderObject.CurrentCell.RowIndex, x) = Me.VariableName.Text
                Else
                    mySenderObject.VariableName(mySenderObject.CurrentCell.RowIndex, x) = Me.VariableName.Text & "_[" & x - 1 & "]"
                End If

                mySenderObject.DisplayName(mySenderObject.CurrentCell.RowIndex, x) = Me.DisplayName.Text
                mySenderObject.HighThresh(mySenderObject.CurrentCell.RowIndex, x) = CDbl(Me.HighThreshold.Text)
                mySenderObject.LowThresh(mySenderObject.CurrentCell.RowIndex, x) = CDbl(Me.LowThreshold.Text)

                mySenderObject.Parent.Text = Me.DisplayWindowName.Text
                mySenderObject.Parent.Name = Me.DisplayWindowName.Text
                mySenderObject.Name = Me.ControlName.Text

                mySenderObject.CheckForDataChange(mySenderObject.CurrentCell.RowIndex, x) = CBool(IIf(Me.CheckForDataChange.Text = "False", False, True))

                mySenderObject.EqualTo(mySenderObject.CurrentCell.RowIndex, x) = Me.EqualTo.Text
                mySenderObject.HighThreshBackColor(mySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.HighThreshBackColor.Text), Color)
                mySenderObject.HighThreshForeColor(mySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.HighThreshForeColor.Text), Color)
                mySenderObject.LowThreshBackColor(mySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.LowThreshBackColor.Text), Color)
                mySenderObject.LowThreshForeColor(mySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.LowThreshForeColor.Text), Color)

                mySenderObject.DeviceName(mySenderObject.CurrentCell.RowIndex, x) = Me.DeviceName.Text
                mySenderObject.Raster(mySenderObject.CurrentCell.RowIndex, x) = Me.RasterName.Text

                If mySenderObject.DisplayName(1, 1) <> "undefined" Then
                    mySenderObject.DefaultCellBackColor(mySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.DefaultBackColorCombo.Text), Color)
                    mySenderObject.DefaultCellForeColor(mySenderObject.CurrentCell.RowIndex, x) = DirectCast(ccon.ConvertFromString(Me.DefaultForeColorCombo.Text), Color)

                    If mySenderObject.CurrentCell.ColumnIndex > 0 Then
                        'mySenderObject.CellBackColor = mySenderObject.DefaultCellBackColor(mySenderObject.CurrentCell.RowIndex, x) 'REMOVED FOR 64BIT COMPILE
                        'mySenderObject.CellForeColor = mySenderObject.DefaultCellForeColor(mySenderObject.CurrentCell.RowIndex, x)  'REMOVED FOR 64BIT COMPILE
                    End If

                End If

                If DisplayWindowNameTextChanged = True Then
                    DisplayWindowNameTextChanged = False
                    For z = 0 To UBound(GM_ResidentClient.myDFs)
                        If GM_ResidentClient.myDFs(z).Name = Me.DisplayWindowName.Text Then
                            If GM_ResidentClient.myDFs(z).GoNoGoIndex > -1 Then
                                GM_ResidentClient.myLabel(GM_ResidentClient.myDFs(z).GoNoGoIndex).Text = Me.DisplayWindowName.Text
                            End If
                            GM_ResidentClient.myToolStripMenuItem.DropDownItems(z + GM_ResidentClient.NUM_PREDEFINED_DISPLAYS).Text = Me.DisplayWindowName.Text
                            Exit For
                        End If
                    Next z

                End If

                If SignalChanged = True Then
                    AddNewSignalToList(x)
                End If

            Next x

            'mySenderObject.Col = 0

            mySenderObject.CurrentCell = mySenderObject(mySenderObject.CurrentCell.RowIndex, 0)


            mySenderObject.Text = Me.DisplayName.Text

            ChangesMade = True
            SaveChangesToGridObject = True

            Me.Button1.Enabled = False
            Me.Button3.Enabled = False
            Me.Button5.Enabled = False

        End If

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

        formloadsub()

    End Sub

    Private Sub GridCellPropConfig_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDoubleClick

    End Sub

    Private Sub GridCellPropConfig_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp

    End Sub

    Private Sub GridCellPropConfig_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize

        'TBD

        Static SaveMyWidth As Integer
        Static SaveMyHeight As Integer

        If SaveMyWidth = 0 Then
            SaveMyWidth = Me.Width
        Else
            Me.Width = SaveMyWidth
        End If
        If SaveMyHeight = 0 Then
            SaveMyHeight = Me.Height
        End If

        If Me.Height > SaveMyHeight Then
            Me.Height = SaveMyHeight
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

    Private Sub DeviceName_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DeviceName.SelectedValueChanged

        'When a new devicename selection is made on the configuration form (GridCellPropConfig), 
        'we must re-populate the available rasternames and variable names based on the newly selected
        'device name....

        Dim y As Integer
        Dim tempstrarray() As String

        If Me.Label14.Visible = False Then

            RasterName.Text = ""
            VariableName.Text = ""

            GM_ResidentClient.Cursor = Cursors.WaitCursor

            Label14.Text = "Performing Variable Browsing Operation..."
            Label14.Visible = True

            Me.Refresh()

            Me.RasterName.Items.Clear()
            Me.VariableName.Items.Clear()

            If myINCAInterface.DeviceDataRetrieved = False Then
                myINCAInterface.GetDeviceAquisitionRates()
            End If

            ReDim tempstrarray(0)

            For y = 0 To UBound(myINCAInterface.deviceinfo(DeviceName.SelectedIndex).rasters)
                ReDim Preserve tempstrarray(y)
                tempstrarray(y) = myINCAInterface.deviceinfo(DeviceName.SelectedIndex).rasters(y).rastername

            Next y

            RasterName.Items.AddRange(tempstrarray)

            ReDim tempstrarray(0)

            For y = 0 To UBound(myINCAInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo)
                ReDim Preserve tempstrarray(y)
                tempstrarray(y) = myINCAInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo(y).variablename

            Next y

            VariableName.Items.AddRange(tempstrarray)

            Me.Label14.Visible = False
            GM_ResidentClient.Cursor = Cursors.Arrow

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
        If InStr(DeviceName.Text, "IP") > 0 Or
            InStr(DeviceName.Text, "IR") > 0 Or
            InStr(DeviceName.Text, "IC") > 0 Or
            InStr(DeviceName.Text, "K1") > 0 Or
            InStr(DeviceName.Text, "K2") > 0 Or
            InStr(DeviceName.Text, "HC") > 0 Or
            InStr(DeviceName.Text, "XCP:1") > 0 Or
            InStr(DeviceName.Text, "ETK") > 0 Then

            Exit Sub
        End If

        VariableName.Items.Clear()

        Me.Refresh()

        For z = 0 To UBound(myINCAInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo)

            If myINCAInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo(z).defaultrastername = RasterName.SelectedItem.ToString Then

                VariableName.Items.Add(myINCAInterface.deviceinfo(DeviceName.SelectedIndex).variableinfo(z).variablename)
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

        AddSignalAsArray = False

        If Len(Me.VariableName.Text) > 0 Then

            Me.RasterName.Text = myINCAInterface.GetDefaultRasterForMeasureElementInDevice(Me.DeviceName.Text, Me.VariableName.Text)
            Me.DisplayName.Text = Me.VariableName.Text

            For x = 0 To UBound(myDGs)
                If GridToModify = myDGs(x).Name Then

                    If myDGs(x).ColumnCount >= 3 And InStr(Me.VariableName.Text, "[x]") > 0 And myDGs(x).CurrentCell.ColumnIndex = 1 Then
                        'MsgBox(myDGs(x).Row & " - " & myDGs(x).Col)
                        AddSignalAsArray = True

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
        DisplayWindowNameTextChanged = True

    End Sub

    Private Sub CheckForDataChange_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckForDataChange.Click
        Me.Button1.Enabled = True
        Me.Button3.Enabled = True
        Me.Button5.Enabled = True

    End Sub

    Private Sub CheckForDataChange_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckForDataChange.SelectedIndexChanged

    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click

        'This is the Save, Register, Exit button...
        GM_ResidentClient.Cursor = Cursors.WaitCursor

        If SaveChangesToGridObject() = True Then

            Me.Label14.Text = "Performing Signal Registration..."
            Me.Label14.Visible = True

            SignalRegistrationMode = "DISPLAYS"

            myINCAInterface.RegisterSignals()

            Me.Label14.Visible = False
        End If

        Me.Hide()

        GM_ResidentClient.Cursor = Cursors.Arrow

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