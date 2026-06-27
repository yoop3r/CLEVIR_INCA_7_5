Public Class NewGridCreation

    'This is the Create New Grid form.  This form is displayed when the "Create New Grid" Selection
    'is made from the form handling context menu.  Allows the user to define the number of rows and
    'columns for the new grid or, if the grid is to be a single column list, allows the user to
    'retrieve a signal list from the controller device(s) and select from the list to populate a
    'two columns by X rows grid.

    'While most of the code is shared between the CLEVIR_INCA_7_2 and CLEVIR_INCA_7_3 versions, there are different
    'NewGridCreation modules used for the two CLEVIR versions.  This is due to the fact that the handling of configured
    'displays must be different between 7.2 and 7.3, and much of the code related to the configured displays is in this
    'module .So, whenever any changes are made to NewGridCreation, the same changes must be made to both the 7.2 version
    'which is located in FlexGridFiles and the 7.3 version which is located in in NoFlexGridFiles...

    Public MyGridTitle As String

    Private Sub GridTitle_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'The grid title text box must be changed to a user defined title. When this is done, the button to create a new grid is
        'Enabled....

        If Len(GridTitle.Text) > 0 And GridTitle.Text <> "undefined" And IsNumeric(TextBox1.Text) And IsNumeric(TextBox2.Text) Then
            Button1.Enabled = True

        Else
            Button1.Enabled = False

        End If
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'Here we check to make sure tha the text entered into the number of cols text box is valid.

        If Val(TextBox1.Text) > GridDataClass.MaxNumColsPerGrid Then
            MsgBox("Please enter a number < " & GridDataClass.MaxNumColsPerGrid)
            Button1.Enabled = False
            Exit Sub
        End If

        If Len(GridTitle.Text) > 0 And IsNumeric(TextBox1.Text) And IsNumeric(TextBox2.Text) Then
            Button1.Enabled = True
        Else
            Button1.Enabled = False
        End If


    End Sub

    Private Sub TextBox2_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

        'Here we check to make sure tha the text entered into the number of rows text box is valid.

        If Val(TextBox2.Text) > GridDataClass.MaxNumRowsPerGrid Then
            MsgBox("Please enter a number <= " & GridDataClass.MaxNumRowsPerGrid)
            Button1.Enabled = False
            Exit Sub
        End If

        If Len(GridTitle.Text) > 0 And IsNumeric(TextBox1.Text) And IsNumeric(TextBox2.Text) Then
            Button1.Enabled = True
        Else
            Button1.Enabled = False
        End If

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)


    End Sub

    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub ListBox1_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub ListBox3_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub NewGridCreation_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Async Sub NewGridCreation_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        'This sub is called when the NewGridCreation form is first loaded.  Initializes the controls
        'on the grid and ads the devices from the available devices in INCA to listbox1.

        Dim mydevices() As IGM_INCA_Comm.INCADeviceStatus
        Dim x As Integer

        Label14.Visible = False

        If Len(MyGridTitle) > 0 And MyGridTitle <> "undefined" Then

            GridTitle.Text = MyGridTitle

            ListBox1.Items.Clear()
            ListBox3.Items.Clear()

            ComboBox1.Items.Clear()

            mydevices = Await MyIncaInterface.GetAvailableDevicesAsync(False)

            If mydevices IsNot Nothing Then
                For x = 0 To UBound(mydevices)
                    ListBox1.Items.Add(mydevices(x).myName)
                Next x
            End If
        Else
            MsgBox("Invalid Grid Title.  Exiting...")
            Me.Close()
        End If
    End Sub

    Private Sub Button2_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Async Sub ListBox1_SelectedIndexChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged

        'If there is a selection made from the devices list (listbox1), the associated device aquisition rates
        '(raster rates) are loaded from the INCA device into CombBox1, as well as all available signals
        '/variables in the device.  Note:  In the case of the CSAV2 controller devices, only the "Ve*" and
        '"Va*" variables are added.

        Dim y As Integer

        Dim tempstrarray() As String

        If Me.Label14.Visible = False Then

            GmResidentClient.Cursor = Cursors.WaitCursor

            Label14.Text = "Performing Variable Browsing Operation..."
            Label14.Visible = True

            Me.Refresh()

            ComboBox1.Items.Clear()
            ComboBox1.Text = ""

            Me.Cursor = Cursors.WaitCursor
            Label1.Text = ""
            'ListBox2.Items.Clear()
            ComboBox1.Items.Clear()
            ListBox3.Items.Clear()

            If MyIncaInterface.DeviceDataRetrieved = False Then
                Await MyIncaInterface.GetDeviceAcquisitionRatesAsync()
            End If

            ReDim tempstrarray(0)

            If MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).rasters IsNot Nothing Then
                For y = 0 To UBound(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).rasters)
                    'ListBox2.Items.Add(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).rasters(y).rastername)
                    ReDim Preserve tempstrarray(y)
                    tempstrarray(y) = MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).rasters(y).rastername
                Next y

                ComboBox1.Items.AddRange(tempstrarray)
            End If

            ReDim tempstrarray(0)

            If MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo IsNot Nothing Then
                For y = 0 To UBound(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo)
                    'ListBox3.Items.Add(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo(y).variablename)
                    ReDim Preserve tempstrarray(y)
                    tempstrarray(y) = MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo(y).variablename
                Next y

                ListBox3.Items.AddRange(tempstrarray)
            End If

            Me.Cursor = Cursors.Arrow

        End If

        Me.Label14.Visible = False

    End Sub

    Private Sub Button1_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'This is the Create button on the NewGridCreation screen.  

        'This button is used if the user selects a number of columns and number of rows at the top part 
        'of the screen.   In this case,  the bottom part of the display is not used, there is no 
        'opportunity for the user to populate actual signal / variable names in to each grid cell.  
        'This must instead be done using the GridCellPropConfig form after the grid is created.

        Dim rows As Integer
        Dim cols As Integer

        rows = CInt(TextBox2.Text)

        If rows > 50 Then
            MsgBox("Maximum allowable number of signals per grid (50) has been exceeded, please enter no more than 50 rows.")
            Exit Sub
        End If

        cols = CInt(TextBox1.Text)

        'If we are creating a new grid by specifying rows and columns, then we must keep track of
        'the number of signals added.  We do this with the NumSignalsAdded variable

        GmResidentClient.NumSignalsAdded = GmResidentClient.NumSignalsAdded + (rows * cols)

        GmResidentClient.CreateNewGrid(rows, cols)

        Me.Close()

    End Sub

    Private Sub Button3_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click

        'This is the Populate grid button which is accessed from the NewGridCreation form...

        'This routine will create a new two column, x row grid and copy the signal names highlighted in 
        'listbox3 and add them to the newly created grid (In column 0).

        'Note:  Display Name in column 0 and Variable Name associated with column 1, for each signal, 
        'will be the same.

        Dim x As Integer
        Dim z As Integer
        Dim y As Integer
        Dim rows As Integer
        Dim cols As Integer

        cols = 1

        For x = 0 To myDGs.Count - 1
            If GridToModify = myDGs(x).Name Then
                Exit For
            End If
        Next x

        For z = 0 To ListBox3.Items.Count - 1

            If ListBox3.GetSelected(z) = True Then

                rows = rows + 1

            End If
        Next

        If rows > 50 Then
            MsgBox("Maximum allowable number of signals per grid (50) has been exceeded, please reduce the number of signals selected.")
            Exit Sub
        End If

        GmResidentClient.NumSignalsAdded = GmResidentClient.NumSignalsAdded + rows

        GmResidentClient.CreateNewGrid(rows, cols)

        y = 0
        For z = 0 To ListBox3.Items.Count - 1

            If ListBox3.GetSelected(z) = True Then
                y = y + 1
                myDGs(x).DefaultCellBackColor(y, 1) = System.Drawing.Color.White

                myDGs(x).VariableName(y, 1) = ListBox3.Items(z).ToString
                myDGs(x).DisplayName(y, 1) = ListBox3.Items(z).ToString
                myDGs(x).DeviceName(y, 1) = ListBox1.Items(ListBox1.SelectedIndex).ToString

                'HC CHANGE
                If InStr(ListBox1.Items(ListBox1.SelectedIndex).ToString, "ETK") = 0 And InStr(ListBox1.SelectedItem.ToString, "IP") = 0 And InStr(ListBox1.SelectedItem.ToString, "IR") = 0 And InStr(ListBox1.SelectedItem.ToString, "IC") = 0 _
                And InStr(ListBox1.SelectedItem.ToString, "K1") = 0 And InStr(ListBox1.SelectedItem.ToString, "K2") = 0 And InStr(ListBox1.SelectedItem.ToString, "XCP:1") = 0 _
                And InStr(ListBox1.SelectedItem.ToString, "HC") = 0 And InStr(ListBox1.SelectedItem.ToString, "ACP") = 0 Then
                    myDGs(x).Raster(y, 1) = MyIncaInterface.GetDefaultRasterForMeasureElementInDevice(ListBox1.Items(ListBox1.SelectedIndex).ToString, ListBox3.Items(z).ToString)
                Else
                    myDGs(x).Raster(y, 1) = ComboBox1.Text
                End If

                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals) - rows + y).DeviceName = myDGs(x).DeviceName(y, 1)
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals) - rows + y).RasterName = myDGs(x).Raster(y, 1)
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals) - rows + y).SignalName = myDGs(x).VariableName(y, 1)

                'myDGs(x).Row = y
                'myDGs(x).Col = 0

                myDGs(x).CurrentCell = myDGs(x)(y, 0)

                myDGs(x).Text = myDGs(x).DisplayName(y, 1)

                'myDGs(x).Col = 1

                myDGs(x).CurrentCell = myDGs(x)(y, 1)

                myDGs(x).Text = myDGs(x).VariableName(y, 1)

            End If
        Next

        Me.Close()

        SignalRegistrationMode = "DISPLAYS"
        MyIncaInterface.RegisterSignals()

    End Sub

    Private Sub ListBox3_SelectedIndexChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox3.SelectedIndexChanged
        Button3.Enabled = True
    End Sub

    Private Sub GridTitle_TextChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GridTitle.TextChanged

    End Sub

    Private Sub ListBox2_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox2.SelectedIndexChanged

    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged

        'The user may use ComboBox1 to select the raster to be used for the signals.  This functionality
        'is different depending on the device that is selected in listbox1.  If the device is a controller
        'making a selection in this combobox means that any signal selected will have the selected
        'raster rate associated with it.  If the device is a CAN monitoring channel for instance, then
        'all signals available in that associated CAN monitoring "raster" will be displayed in the

        'HC CHANGE
        If InStr(ListBox1.SelectedItem.ToString, "ACP") > 0 Or
            InStr(ListBox1.SelectedItem.ToString, "XCP:1") > 0 Or
            InStr(ListBox1.SelectedItem.ToString, "ETK") > 0 Then

            Exit Sub
        End If

        'Listbox3 for further selection by the user.

        If Me.Label14.Visible = False Then

            GmResidentClient.Cursor = Cursors.WaitCursor

            Label14.Text = "Performing Variable Browsing Operation..."
            Label14.Visible = True

            Me.Refresh()

            Me.Cursor = Cursors.WaitCursor

            ListBox3.Items.Clear()

            Me.Refresh()

            For z = 0 To UBound(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo)

                If MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo(z).defaultrastername = ComboBox1.Text Then

                    ListBox3.Items.Add(MyIncaInterface.deviceinfo(ListBox1.SelectedIndex).variableinfo(z).variablename)
                    ListBox3.Refresh()

                End If

            Next z

        End If

        Me.Cursor = Cursors.Arrow
        Label14.Visible = False

    End Sub

    Private Sub TextBox2_TextChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox2.TextChanged

    End Sub

    Private Sub Button2_Click_2(ByVal sender As System.Object, ByVal e As System.EventArgs)
        MyIncaInterface.Cancelit = True
    End Sub

    Private Sub Label14_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label14.Click

    End Sub
End Class