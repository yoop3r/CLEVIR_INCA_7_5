Public Class AnnotationInterfaceConfigure

    'This form handles configuration of the annotation interface.  It is shown when the user right clicks on any tab or
    'button in the annotation section of the main CLEVIR screen...

    'The CLEVIR annotation interface is defined in a DataDictionary.csv file which is read in during initialization.  Information
    'from this file is placed into a data structure which is used to dynamically build the annotation tab and button interface
    'that is displayed on the CLEVIR main screen.

    'The AnnotationInterfaceConfigure form allows the user to make changes to the data structure and save these changes to a
    'different, user specified, DataDictionary file which is then read back in such that the changes are processed and implemented while CLEVIR
    'is still running...

    Private SelectedEnumTypeRecordIndex As Integer
    Private SelectedEnumButtonIndex As Integer

    Private SelectedAnnoTypeRecordIndex As Integer
    Private SelectedAnnoValueRecordIndex As Integer

    Private SelectedTypeID As Integer

    Private ChangesMade As Boolean
    Private SaveSelectedItemText As String

    Private Sub ListBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedIndexChanged

        'Listbox2 displays the Driver Feedback Annotation Tabs (CPS, LKA, LCC, etc.)  It is made visible if the user
        'selects Driver Feedback from Listbox1 which displays the annotation categories...

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer

        Dim tempstr As String = ""

        'When a Driver Feedback tab selection is made, we capture the AnnotationValueRecord index and the EnumerationTypeRecord index.
        'We will need these to handle what needs to happen when the user makes a selection in Listbox3, which is populated with
        'EnumerationTypeRecord descriptions...

        Try

            If ListBox2.SelectedIndex > -1 Then

                ListBox3.Items.Clear()
                ListBox3.Visible = True
                ListBox5.Items.Clear()
                ListBox5.Visible = True
                Button3.Visible = True
                Label3.Visible = True
                Label5.Visible = True
                ListBox4.Visible = False
                Label4.Visible = False
                ListBox4.Visible = False

                For x = 0 To UBound(AnnotationValueRecord)
                    If ListBox2.SelectedItem.ToString = AnnotationValueRecord(x).Description Then

                        SelectedAnnoValueRecordIndex = x

                        For y = 0 To UBound(EnumerationTypeRecord)
                            If EnumerationTypeRecord(y).ID = AnnotationValueRecord(x).EnumerationType Then

                                SelectedEnumTypeRecordIndex = y

                                For z = 0 To UBound(EnumerationTypeRecord(y).EnumerationDesc, 2)
                                    ListBox3.Items.Add(EnumerationTypeRecord(y).EnumerationDesc(0, z))
                                    ListBox5.Items.Add(EnumerationTypeRecord(y).HotKeyAssignment(z))
                                Next z
                            End If
                        Next
                    End If
                Next

                For x = 0 To ListBox5.Items.Count - 1

                    If InStr(tempstr, ListBox5.Items(x).ToString) = 0 Then
                        tempstr = tempstr & ListBox5.Items(x).ToString & ","
                    Else
                        MsgBox("There is more than one hotkey with the same letter assignment.  Please make sure that all hotkeys are unique...")
                        Exit For
                    End If
                Next

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: " & ex.Message, DISPLAY_MSG_BOX)
            'MsgBox(ex.Message)
        End Try

    End Sub

    Private Sub ListBox3_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox3.SelectedIndexChanged

        'Listbox3 displays the button text of the buttons associated with either the annotation category selected in Listbox1, 
        'Or the Driver Feedback tab selected in Listbox2...

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer

        If ListBox3.SelectedIndex > -1 Then

            SaveSelectedItemText = ListBox3.SelectedItem.ToString

            For x = 0 To UBound(AnnotationValueRecord)

                If ListBox2.SelectedIndex > -1 Then

                    If ListBox2.SelectedItem.ToString = AnnotationValueRecord(x).Description Then

                        For y = 0 To UBound(EnumerationTypeRecord)
                            If EnumerationTypeRecord(y).ID = AnnotationValueRecord(x).EnumerationType Then
                                SelectedEnumTypeRecordIndex = y
                                For z = 0 To UBound(EnumerationTypeRecord(y).EnumerationDesc, 2)
                                    If ListBox3.SelectedItem.ToString = EnumerationTypeRecord(y).EnumerationDesc(0, z) Then

                                        SelectedEnumButtonIndex = z
                                        ListBox4.Items.Clear()
                                        ListBox4.Visible = True
                                        Label4.Visible = True

                                        For i = 1 To 5
                                            If Len(EnumerationTypeRecord(y).EnumerationDesc(i, z)) > 0 Then
                                                ListBox4.Items.Add(EnumerationTypeRecord(y).EnumerationDesc(i, z))
                                            Else
                                                ListBox4.Items.Add("Undefined")
                                            End If

                                        Next i
                                        Exit For
                                    End If
                                Next z
                            End If

                        Next
                    End If

                Else

                    If ListBox3.SelectedItem.ToString = AnnotationValueRecord(x).Description Then
                        SelectedAnnoValueRecordIndex = x
                    End If

                End If

            Next

        End If

    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged

        'Listbox1 displays the Main Annotation Categories (Driver Feedback, Road Type, etc.)

        Dim x As Integer
        Dim y As Integer

        'When a Main Annotation category tab selection is made, we must do different things based on what is selected.
        'If Driver Feedback is selected, we will display listbox2, which contains the text for each Driver Feedback tab.
        'We will also capture the AnnotationTypeRecord index which will be used when handling Listbox2 interaction...

        'If any other Main category tab is selected, we will populate listbox3 with the text from the buttons associated with 
        'the selected main category. We will also capture the AnnotationTypeRecord index, AnnotationValueRecord inded and the
        'SelectedTypeID for use when handling user interaction with listbox3...

        If ListBox1.SelectedIndex > -1 Then

            ListBox2.SelectedIndex = -1

            HandleHideControls()

            If ListBox1.SelectedItem.ToString = "Driver Feedback" Then
                ListBox2.Visible = True
                Label2.Visible = True
                Button2.Visible = True

                For x = 0 To UBound(AnnotationTypeRecord)
                    If ListBox1.SelectedItem.ToString = AnnotationTypeRecord(x).Description Then
                        SelectedAnnoTypeRecordIndex = x
                        Exit For
                    End If
                Next

            Else
                ListBox3.Visible = True
                Label3.Visible = True
                ListBox5.Visible = True
                Label5.Visible = True
                Button3.Visible = True
                ListBox3.Items.Clear()
                ListBox4.Items.Clear()
                ListBox5.Items.Clear()

                For x = 0 To UBound(AnnotationTypeRecord)
                    If ListBox1.SelectedItem.ToString = AnnotationTypeRecord(x).Description Then
                        SelectedAnnoTypeRecordIndex = x
                        For y = 0 To UBound(AnnotationValueRecord)
                            If AnnotationValueRecord(y).TypeID = AnnotationTypeRecord(x).ID Then
                                SelectedTypeID = AnnotationTypeRecord(x).ID
                                SelectedAnnoValueRecordIndex = y
                                ListBox3.Items.Add(AnnotationValueRecord(y).Description)
                            End If
                        Next
                        Exit For
                    End If
                Next

            End If

        End If

    End Sub

    Private Sub Label4_Click(sender As Object, e As EventArgs) Handles Label4.Click

    End Sub

    Private Sub ListBox4_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox4.SelectedIndexChanged

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub

    Private Sub ListBox1_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox1.MouseDown

        'Listbox1 displays the Main Annotation Categories (Driver Feedback, Road Type, etc.)...

        'If user right clicks on any item in the list, they will be asked if they wish to remove the item...

        Dim x As Integer
        Dim Deleted As Boolean

        If e.Button.ToString = "Right" Then

            x = ListBox1.Items.Count - 1
            Do While x >= 0

                If ListBox1.GetSelected(x) = True Then
                    If MsgBox("Delete Annotation Category?", vbYesNo) = vbYes Then
                        ListBox1.Items.RemoveAt(x)
                    Else
                        Exit Sub
                    End If

                End If

                x = x - 1
            Loop

            ListBox1.Refresh()

            For x = 0 To UBound(AnnotationTypeRecord) - 1
                If x = SelectedAnnoTypeRecordIndex Or Deleted = True Then
                    AnnotationTypeRecord(x) = AnnotationTypeRecord(x + 1)
                    Deleted = True
                End If
            Next

            ReDim Preserve AnnotationTypeRecord(UBound(AnnotationTypeRecord) - 1)

            ListBox3.Visible = False
            Label3.Visible = False
            ListBox5.Visible = False
            Label5.Visible = False
            Button3.Visible = False

            HandleChangesMade()

        End If
    End Sub

    Private Sub ListBox4_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox4.MouseDown

        'Listbox 4 displays the sub-categories associated with any button associated with any Driver Feedback tab...

        'If the user right clicks on any item in the list that is not "Undefined", they will be asked if they wish to remove the item...

        Dim x As Integer
        Dim i As Integer

        If e.Button.ToString = "Right" Then

            If ListBox4.SelectedItem.ToString <> "Undefined" Then
                x = ListBox4.Items.Count - 1
                Do While x >= 0

                    If ListBox4.GetSelected(x) = True Then
                        If MsgBox("Remove Sub-Category?", vbYesNo) = vbYes Then
                            ListBox4.Items.RemoveAt(x)
                        Else
                            Exit Sub
                        End If

                    End If

                    x = x - 1
                Loop

                ListBox4.Refresh()
                ListBox4.Items.Add("Undefined")

                'Undefined is displayed for any of the five sub-categories that do not have specific text associated with them.
                'However, the actual data structure contains an empty string for undefined sub-categories...

                For i = 1 To 5
                    If ListBox4.Items(i - 1).ToString <> "Undefined" Then
                        EnumerationTypeRecord(SelectedEnumTypeRecordIndex).EnumerationDesc(i, SelectedEnumButtonIndex) = ListBox4.Items(i - 1).ToString
                    Else
                        EnumerationTypeRecord(SelectedEnumTypeRecordIndex).EnumerationDesc(i, SelectedEnumButtonIndex) = ""
                    End If
                Next i

                HandleChangesMade()

            End If

        End If

    End Sub

    Private Sub ListBox2_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox2.MouseDown

        'Listbox2 displays the Driver Feedback Annotation Tabs (CPS, LKA, LCC, etc.)  It is made visible if the user
        'selects Driver Feedback from Listbox1 which displays the annotation categories...

        'If user right clicks on any item in the list, they will be asked if they wish to remove the item...

        Dim x As Integer
        Dim Deleted As Boolean

        If e.Button.ToString = "Right" Then

            x = ListBox2.Items.Count - 1
            Do While x >= 0

                If ListBox2.GetSelected(x) = True Then
                    If MsgBox("Delete Annotation Category?", vbYesNo) = vbYes Then
                        ListBox2.Items.RemoveAt(x)
                    Else
                        Exit Sub
                    End If

                End If

                x = x - 1
            Loop

            ListBox2.Refresh()

            For x = 0 To UBound(AnnotationValueRecord) - 1
                If x = SelectedAnnoValueRecordIndex Or Deleted = True Then
                    AnnotationValueRecord(x) = AnnotationValueRecord(x + 1)
                    Deleted = True
                End If
            Next

            ReDim Preserve AnnotationValueRecord(UBound(AnnotationValueRecord) - 1)

            ListBox3.Visible = False
            Label3.Visible = False
            ListBox5.Visible = False
            Label5.Visible = False
            Button3.Visible = False

            HandleChangesMade()

        End If

    End Sub

    Private Sub AnnotationInterfaceConfigure_Load(sender As Object, e As EventArgs) Handles Me.Load

        'This routine is called when the AnnotationInterfaceConfigure form is shown for the first time as a result of the
        'user right clicking on a tab or a button on the annotation interface part of the main CLEVIR screen...

        'Here we do some initialization stuff and display the name of the current Annotation Data Dictionary File 
        'in the AnnotationInterfaceConfigure form header...

        ListBox2.SelectedIndex = -1
        ListBox1.SelectedIndex = -1

        HandleHideControls()

        If Len(AnnotationDataDictionaryFile) > 0 Then
            Me.Text = "Annotation Interface Configuration - " & System.IO.Path.GetFileName(AnnotationDataDictionaryFile)
        Else
            Me.Text = "Annotation Interface Configuration - DEFAULT DataDictionary.csv"
            ListBox1.Enabled = False
            ListBox2.Enabled = False
            ListBox3.Enabled = False
            ListBox5.Enabled = False
            Button1.Enabled = False
            Button2.Enabled = False
            Button3.Enabled = False

        End If

    End Sub

    Private Sub ListBox3_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox3.MouseDown

        'Listbox3 displays the button text of the buttons associated with either the annotation category selected in Listbox1, 
        'Or the Driver Feedback tab selected in Listbox2...

        'If user right clicks on any item in the list, they will be asked if they wish to remove the item...

        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer
        Dim n As Integer
        Dim Deleted As Boolean

        If e.Button.ToString = "Right" Then

            x = ListBox3.Items.Count - 1
            Do While x >= 0

                If ListBox3.GetSelected(x) = True Then
                    If MsgBox("Delete Annotation Button?", vbYesNo) = vbYes Then
                        ListBox3.Items.RemoveAt(x)
                        ListBox5.Items.RemoveAt(x)
                    Else
                        Exit Sub
                    End If

                End If

                x = x - 1
            Loop

            ListBox3.Refresh()
            ListBox5.Refresh()

            If ListBox2.SelectedIndex = -1 Then 'This means listbox3 is displaying buttons associated with Main Annotation Categories, so associated button will be deleted...

                For x = 0 To UBound(AnnotationValueRecord) - 1
                    If (x = SelectedAnnoValueRecordIndex) Or Deleted = True Then
                        AnnotationValueRecord(x) = AnnotationValueRecord(x + 1)
                        Deleted = True
                    End If
                Next

                ReDim Preserve AnnotationValueRecord(UBound(AnnotationValueRecord) - 1)

            Else 'This means that listbox3 is displaying buttons associated with a Driver Feedback tab...

                'Instead of deleting an AnnotationValueRecord, we are deleting an EnumerationTypeRecord...
                For x = 0 To UBound(AnnotationValueRecord)
                    If ListBox2.SelectedItem.ToString = AnnotationValueRecord(x).Description Then

                        For y = 0 To UBound(EnumerationTypeRecord)
                            If EnumerationTypeRecord(y).ID = AnnotationValueRecord(x).EnumerationType Then

                                For z = 0 To UBound(EnumerationTypeRecord(y).EnumerationDesc, 2)
                                    If EnumerationTypeRecord(y).EnumerationDesc(0, z) = SaveSelectedItemText Then

                                        For i = z To UBound(EnumerationTypeRecord(y).EnumerationDesc, 2) - 1

                                            For n = 0 To 5
                                                EnumerationTypeRecord(y).EnumerationDesc(n, i) = EnumerationTypeRecord(y).EnumerationDesc(n, i + 1)
                                            Next n
                                        Next i
                                        Exit For
                                    End If
                                Next z

                                ReDim Preserve EnumerationTypeRecord(y).EnumerationDesc(5, UBound(EnumerationTypeRecord(y).EnumerationDesc, 2) - 1)

                                Exit For

                            End If
                        Next
                        Exit For
                    End If
                Next


            End If

            ListBox4.Visible = False
            Label4.Visible = False

            HandleChangesMade()

        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        'This is the Add Annotation Category button associated with Listbox1...

        Dim SaveAnnotationTypeRecorodID As Integer
        Dim SaveDisplayOrder As Integer
        Dim AnnotationCategoryName As String

        Dim x As Integer
        Dim y As Integer

        AnnotationCategoryName = InputBox("Please enter the name of the new Annotation Category", "ANNOTATION CATEGORY INPUT", "New Category Name")

        If Len(AnnotationCategoryName) > 0 Then

            For x = 0 To UBound(AnnotationTypeRecord)

                If AnnotationTypeRecord(x).ID > SaveAnnotationTypeRecorodID Then
                    If AnnotationTypeRecord(x).ID < 1998 Then
                        SaveAnnotationTypeRecorodID = AnnotationTypeRecord(x).ID
                        SaveDisplayOrder = AnnotationTypeRecord(x).DisplayOrder
                    Else

                        ReDim Preserve AnnotationTypeRecord(UBound(AnnotationTypeRecord) + 1)

                        For y = UBound(AnnotationTypeRecord) To x Step -1

                            AnnotationTypeRecord(y).Description = AnnotationTypeRecord(y - 1).Description
                            AnnotationTypeRecord(y).RecordType = 1
                            AnnotationTypeRecord(y).DisplayOrder = AnnotationTypeRecord(y - 1).DisplayOrder + 1
                            AnnotationTypeRecord(y).ID = AnnotationTypeRecord(y - 1).ID
                            AnnotationTypeRecord(y).System = AnnotationTypeRecord(y - 1).System

                        Next y

                        Exit For

                    End If
                End If

            Next x

            AnnotationTypeRecord(x).Description = AnnotationCategoryName
            AnnotationTypeRecord(x).RecordType = 1
            AnnotationTypeRecord(x).DisplayOrder = SaveDisplayOrder + 1
            AnnotationTypeRecord(x).ID = SaveAnnotationTypeRecorodID + 1
            AnnotationTypeRecord(x).System = 0

            ListBox1.Items.Clear()

            For x = 0 To UBound(AnnotationTypeRecord)
                If AnnotationTypeRecord(x).ID < 1998 Then
                    ListBox1.Items.Add(AnnotationTypeRecord(x).Description)
                End If
            Next

            HandleChangesMade()

        Else
            HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigured: Invalid Name Entered...", DISPLAY_MSG_BOX)
            'MsgBox("Invalid Name Entered...")
        End If


    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        'This is the Add Annotation Button button associated with Listbox3...

        Dim SaveAnnotationTypeRecordID As Integer
        Dim SaveAnnotationValueRecordEnumType As Integer
        Dim AnnotationButtonName As String
        Dim x As Integer
        Dim y As Integer
        Dim found As Boolean

        AnnotationButtonName = InputBox("Please enter the text for the new Annotation Button", "ANNOTATION BUTTON TEXT INPUT", "New Button Text")

        If Len(AnnotationButtonName) > 0 Then

            'This behavior is different depending on whether we are displaying Main Annotation Category button text, or Driver Feedback
            'Tab button text...

            If ListBox2.SelectedIndex > -1 Then 'We are displaying Driver Feedback tab button text...

                For x = 0 To UBound(AnnotationValueRecord)
                    If AnnotationValueRecord(x).Description = ListBox2.SelectedItem.ToString Then
                        SaveAnnotationValueRecordEnumType = AnnotationValueRecord(x).EnumerationType
                        Exit For
                    End If
                Next

                For x = 0 To UBound(EnumerationTypeRecord)
                    If EnumerationTypeRecord(x).ID = SaveAnnotationValueRecordEnumType Then
                        ReDim Preserve EnumerationTypeRecord(x).EnumerationDesc(5, UBound(EnumerationTypeRecord(x).EnumerationDesc, 2) + 1)
                        ReDim Preserve EnumerationTypeRecord(x).HotKeyAssignment(UBound(EnumerationTypeRecord(x).EnumerationDesc, 2) + 1)
                        EnumerationTypeRecord(x).EnumerationDesc(0, UBound(EnumerationTypeRecord(x).EnumerationDesc, 2)) = AnnotationButtonName
                        EnumerationTypeRecord(x).HotKeyAssignment(UBound(EnumerationTypeRecord(x).EnumerationDesc, 2)) = Mid(AnnotationButtonName, 1, 1)
                        found = True
                        Exit For
                    End If
                Next

                If found = False Then

                    ReDim Preserve EnumerationTypeRecord(UBound(EnumerationTypeRecord) + 1)

                    EnumerationTypeRecord(x).RecordType = 3
                    EnumerationTypeRecord(x).ID = SaveAnnotationValueRecordEnumType

                    ReDim Preserve EnumerationTypeRecord(x).EnumerationDesc(5, 0)
                    ReDim Preserve EnumerationTypeRecord(x).HotKeyAssignment(0)


                    EnumerationTypeRecord(x).EnumerationDesc(0, 0) = AnnotationButtonName
                    EnumerationTypeRecord(x).HotKeyAssignment(0) = Mid(AnnotationButtonName, 1, 1)


                End If

                ListBox3.Items.Clear()
                ListBox5.Items.Clear()

                For x = 0 To UBound(EnumerationTypeRecord)
                    If EnumerationTypeRecord(x).ID = SaveAnnotationValueRecordEnumType Then
                        For y = 0 To UBound(EnumerationTypeRecord(x).EnumerationDesc, 2)
                            ListBox3.Items.Add(EnumerationTypeRecord(x).EnumerationDesc(0, y))
                            ListBox5.Items.Add(EnumerationTypeRecord(x).HotKeyAssignment(y))
                        Next y
                    End If
                Next

                HandleChangesMade()

            Else 'We are displaying Main Annotation Category button text

                If ListBox1.SelectedIndex > -1 Then

                    For x = 0 To UBound(AnnotationTypeRecord)
                        If AnnotationTypeRecord(x).Description = ListBox1.SelectedItem.ToString Then
                            SaveAnnotationTypeRecordID = AnnotationTypeRecord(x).ID
                            Exit For
                        End If
                    Next

                    For x = 0 To UBound(AnnotationValueRecord)

                        If AnnotationValueRecord(x).TypeID = SaveAnnotationTypeRecordID Then
                            'now find the last one...
                            Do While AnnotationValueRecord(x).TypeID = SaveAnnotationTypeRecordID
                                x = x + 1
                            Loop
                            'x = x - 1

                            ReDim Preserve AnnotationValueRecord(UBound(AnnotationValueRecord) + 1)

                            For y = UBound(AnnotationValueRecord) To x Step -1

                                AnnotationValueRecord(y).Description = AnnotationValueRecord(y - 1).Description
                                AnnotationValueRecord(y).RecordType = 2
                                AnnotationValueRecord(y).TypeID = AnnotationValueRecord(y - 1).TypeID
                                AnnotationValueRecord(y).ID = AnnotationValueRecord(y - 1).ID
                                AnnotationValueRecord(y).EnumerationType = AnnotationValueRecord(y - 1).EnumerationType

                            Next y

                            Exit For
                        ElseIf AnnotationValueRecord(x).TypeID = 1998 Then

                            ReDim Preserve AnnotationValueRecord(UBound(AnnotationValueRecord) + 1)

                            For y = UBound(AnnotationValueRecord) To x Step -1

                                AnnotationValueRecord(y).Description = AnnotationValueRecord(y - 1).Description
                                AnnotationValueRecord(y).RecordType = 2
                                AnnotationValueRecord(y).TypeID = AnnotationValueRecord(y - 1).TypeID
                                AnnotationValueRecord(y).ID = AnnotationValueRecord(y - 1).ID
                                AnnotationValueRecord(y).EnumerationType = AnnotationValueRecord(y - 1).EnumerationType

                            Next y

                            Exit For

                        End If

                    Next

                    AnnotationValueRecord(x).Description = AnnotationButtonName
                    AnnotationValueRecord(x).RecordType = 2
                    AnnotationValueRecord(x).TypeID = SaveAnnotationTypeRecordID
                    AnnotationValueRecord(x).ID = 0 '?
                    AnnotationValueRecord(x).EnumerationType = 0

                    ListBox3.Items.Clear()
                    ListBox5.Items.Clear()

                    For x = 0 To UBound(AnnotationValueRecord)
                        If AnnotationValueRecord(x).TypeID = SaveAnnotationTypeRecordID Then
                            ListBox3.Items.Add(AnnotationValueRecord(x).Description)
                        End If
                    Next

                    HandleChangesMade()

                Else
                    MsgBox("Please Reselect Annotation Category!")
                End If

            End If

        Else
            HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: Invalid Button Text Entered...", DISPLAY_MSG_BOX)
            'MsgBox("Invalid Button Text Entered...")
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        'This is the Add Driver Feedback Tab button associated with Listbox2...

        Dim AnnotationCategoryName As String
        Dim x As Integer
        Dim y As Integer
        Dim SaveAnnotationValueRecordMaxID As Integer
        Dim SaveAnnotationValueRecordMaxEnumType As Integer

        AnnotationCategoryName = InputBox("Please enter the name of the new Driver Feedback Annotation Tab", "DRIVER FEEDBACK ANNOTATION TAB INPUT", "New Tab Name")

        If Len(AnnotationCategoryName) > 0 Then

            x = 0
            Do While AnnotationValueRecord(x).EnumerationType <> 0
                If AnnotationValueRecord(x).ID >= SaveAnnotationValueRecordMaxID Then
                    SaveAnnotationValueRecordMaxID = AnnotationValueRecord(x).ID
                End If
                If AnnotationValueRecord(x).EnumerationType >= SaveAnnotationValueRecordMaxEnumType Then
                    SaveAnnotationValueRecordMaxEnumType = AnnotationValueRecord(x).EnumerationType
                End If
                x = x + 1
            Loop
            'x = x - 1

            ReDim Preserve AnnotationValueRecord(UBound(AnnotationValueRecord) + 1)

            For y = UBound(AnnotationValueRecord) To x Step -1

                AnnotationValueRecord(y).RecordType = 2
                AnnotationValueRecord(y).TypeID = AnnotationValueRecord(y - 1).TypeID
                AnnotationValueRecord(y).ID = AnnotationValueRecord(y - 1).ID
                AnnotationValueRecord(y).EnumerationType = AnnotationValueRecord(y - 1).EnumerationType
                AnnotationValueRecord(y).Description = AnnotationValueRecord(y - 1).Description

            Next y

            AnnotationValueRecord(x).RecordType = 2
            AnnotationValueRecord(x).TypeID = 1000
            AnnotationValueRecord(x).ID = SaveAnnotationValueRecordMaxID + 1
            AnnotationValueRecord(x).EnumerationType = SaveAnnotationValueRecordMaxEnumType + 10
            AnnotationValueRecord(x).Description = AnnotationCategoryName

            ListBox2.Items.Clear()

            x = 0
            Do While AnnotationValueRecord(x).EnumerationType <> 0
                If AnnotationValueRecord(x).ID > 3140 Then
                    ListBox2.Items.Add(AnnotationValueRecord(x).Description)
                End If
                x = x + 1
            Loop

            HandleChangesMade()

        Else
            HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: Invalid Text Entered...", DISPLAY_MSG_BOX)
            'MsgBox("Invalid Text Entered...")
        End If

    End Sub

    Private Sub ListBox4_DoubleClick(sender As Object, e As EventArgs) Handles ListBox4.DoubleClick

        'Listbox 4 displays the sub-categories associated with any button associated with any Driver Feedback tab...

        'Doubleclicking on an item in this list allows the user to change the text of the item...
        'If doubleclicking on an "undefined" item, a new Sub-Category will be added...

        Dim inputstr As String

        inputstr = InputBox("Please enter new Annotation Sub-Category Description", "USER INPUT", ListBox4.SelectedItem.ToString)

        If Len(inputstr) > 0 Then
            ListBox4.Items(ListBox4.SelectedIndex) = inputstr

            For i = 1 To 5
                If ListBox4.Items(i - 1).ToString <> "Undefined" Then
                    EnumerationTypeRecord(SelectedEnumTypeRecordIndex).EnumerationDesc(i, SelectedEnumButtonIndex) = ListBox4.Items(i - 1).ToString
                Else
                    EnumerationTypeRecord(SelectedEnumTypeRecordIndex).EnumerationDesc(i, SelectedEnumButtonIndex) = ""
                End If

            Next i

            HandleChangesMade()

        End If

    End Sub

    Private Sub ListBox3_DoubleClick(sender As Object, e As EventArgs) Handles ListBox3.DoubleClick

        'Listbox3 displays the button text of the buttons associated with either the annotation category selected in Listbox1, 
        'Or the Driver Feedback tab selected in Listbox2...

        'Doubleclicking on an item in this list allows the user to change the text of the item...

        Dim inputstr As String

        inputstr = InputBox("Please enter new Button Text", "USER INPUT", ListBox3.SelectedItem.ToString)

        If Len(inputstr) > 0 Then

            If ListBox2.SelectedIndex = -1 Then

                ListBox3.Items(ListBox3.SelectedIndex) = inputstr
                AnnotationValueRecord(SelectedAnnoValueRecordIndex).Description = ListBox3.Items(ListBox3.SelectedIndex).ToString

            Else

                ListBox3.Items(ListBox3.SelectedIndex) = inputstr
                EnumerationTypeRecord(SelectedEnumTypeRecordIndex).EnumerationDesc(0, ListBox3.SelectedIndex) = ListBox3.Items(ListBox3.SelectedIndex).ToString

            End If

            HandleChangesMade()

        End If
    End Sub

    Private Sub ListBox1_DoubleClick(sender As Object, e As EventArgs) Handles ListBox1.DoubleClick

        'Listbox1 displays the Main Annotation Categories (Driver Feedback, Road Type, etc.)...

        'Doubleclicking on an item in this list allows the user to change the text of the item...

        Dim inputstr As String

        inputstr = InputBox("Please enter new Main Tab Text", "USER INPUT", ListBox1.SelectedItem.ToString)

        If Len(inputstr) > 0 Then

            ListBox1.Items(ListBox1.SelectedIndex) = inputstr
            AnnotationTypeRecord(SelectedAnnoTypeRecordIndex).Description = ListBox1.Items(ListBox1.SelectedIndex).ToString

            HandleChangesMade()

        End If
    End Sub

    Private Sub ListBox2_DoubleClick(sender As Object, e As EventArgs) Handles ListBox2.DoubleClick

        'Listbox2 displays the Driver Feedback Annotation Tabs (CPS, LKA, LCC, etc.)  It is made visible if the user
        'selects Driver Feedback from Listbox1 which displays the annotation categories...

        'Doubleclicking on an item in this list allows the user to change the text of the item...

        Dim inputstr As String

        inputstr = InputBox("Please enter new Driver Feedback Tab Text", "USER INPUT", ListBox2.SelectedItem.ToString)

        If Len(inputstr) > 0 Then

            ListBox2.Items(ListBox2.SelectedIndex) = inputstr
            AnnotationValueRecord(SelectedAnnoValueRecordIndex).Description = ListBox2.Items(ListBox2.SelectedIndex).ToString

            HandleChangesMade()

        End If

    End Sub

    Private Sub AnnotationInterfaceConfigure_Shown(sender As Object, e As EventArgs) Handles Me.Shown

    End Sub

    Private Sub AnnotationInterfaceConfigure_Activated(sender As Object, e As EventArgs) Handles Me.Activated

    End Sub

    Private Sub AnnotationInterfaceConfigure_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged

    End Sub

    Private Sub HandleChangesMade()

        'Called whenever a change is made to any part of the annotation interface...

        Dim x As Integer
        Dim tempstr As String = ""

        If ListBox5.Items.Count > 0 Then
            For x = 0 To ListBox5.Items.Count - 1

                If InStr(tempstr, ListBox5.Items(x).ToString) = 0 Then
                    tempstr = tempstr & ListBox5.Items(x).ToString & ","
                Else
                    MsgBox("There is more than one hotkey with the same letter assignment.  Please make sure that all hotkeys are unique...")
                    Exit For
                End If
            Next
        End If

        ChangesMade = True
        SaveAndUpdateToolStripMenuItem.Enabled = True
        ExitToolStripMenuItem1.Text = "Exit - Discard Changes"
        Button4.Text = "Exit - Discard Changes"

    End Sub

    Private Sub ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem1.Click

        'This is the Select Annotation Dictionary File droptown menu option...

        'User inputs the signal configuration file name here...

        OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
        OpenFileDialog1.Title = "Please Select Signal Configuration File"
        OpenFileDialog1.Filter = "DataDictionary |*DataDictionary*.csv"
        OpenFileDialog1.FileName = ""
        OpenFileDialog1.ShowDialog()

        If Len(OpenFileDialog1.FileName) > 0 Then

            If InStr(OpenFileDialog1.FileName, ".csv") > 0 And
               InStr(UCase(OpenFileDialog1.FileName), "DATADICTIONARY") > 0 Then

                If System.IO.Path.GetFileName(OpenFileDialog1.FileName) = "DataDictionary.csv" Or
                    System.IO.Path.GetFileName(OpenFileDialog1.FileName) = "CSAV2_DataDictionary.csv" Or
                    System.IO.Path.GetFileName(OpenFileDialog1.FileName) = "HighContent_DataDictionary.csv" Or
                    System.IO.Path.GetFileName(OpenFileDialog1.FileName) = "LowContent_DataDictionary.csv" Or
                    System.IO.Path.GetFileName(OpenFileDialog1.FileName) = "Copilot_DataDictionary.csv" Or
                    System.IO.Path.GetFileName(OpenFileDialog1.FileName) = "FCM_DataDictionary.csv" Then

                    HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: " & System.IO.Path.GetFileName(OpenFileDialog1.FileName) & " cannot be modified...", DISPLAY_MSG_BOX)
                    'MsgBox(System.IO.Path.GetFileName(OpenFileDialog1.FileName) & " cannot be modified...")

                    Exit Sub

                End If

                AnnotationDataDictionaryFile = OpenFileDialog1.FileName

                HandleEnableControls()

                ParseDataDictionary()

                HandleHideControls()

                Me.Text = "Annotation Interface Configuration - " & System.IO.Path.GetFileName(AnnotationDataDictionaryFile)

                Exit Sub

            End If
        End If

        HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: Invalid Annotation Data Dictionary file selected...", DISPLAY_MSG_BOX)
        'MsgBox("Invalid Annotation Data Dictionary file selected...")

    End Sub
    Private Sub HandleEnableControls()

        'Called from various places, enables the three primary listboxes and associated buttons when appropriate...

        ListBox1.Enabled = True
        ListBox2.Enabled = True
        ListBox3.Enabled = True
        ListBox5.Enabled = True
        Button1.Enabled = True
        Button2.Enabled = True
        Button3.Enabled = True

    End Sub

    Private Sub ExitToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem1.Click

        'This is the Exit (Exit - Discard Changes) drop down menu item. If changes have been made, the text changes from
        'Exit to Exit - Discard Changes.  If discarding changes, the current data dictionary file is re-loaded
        'so current changes made to the annotation data structure are not saved...

        'This routine is also called from the Exit (Exit - Discard Changes) button...

        If ChangesMade = True Then
            ParseDataDictionary()
            ChangesMade = False
            SaveAndUpdateToolStripMenuItem.Enabled = False
        End If

        Me.Hide()

    End Sub

    Private Sub SaveAndUpdateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveAndUpdateToolStripMenuItem.Click

        'This is the Update and Save drop down menu item...

        'Here we save changes to the active data dictionary file and reload it so that the changes take effect and we are running
        'with the newly changed data dictionary file...

        HandleHideControls()

        SaveDataDictionary()
        ParseDataDictionary()

        ChangesMade = False
        SaveAndUpdateToolStripMenuItem.Enabled = False

        ExitToolStripMenuItem1.Text = "Exit"
        Button4.Text = "Exit"

    End Sub

    Private Sub UpdateAndSaveAsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles UpdateAndSaveAsToolStripMenuItem.Click

        'This is the SaveAs... drop down menu item...

        'Allows the user to specify a new file name to save as.  If the user selects one of the project specific files, or the
        'default datadictionary.csv file as the save file name, they are prompted to select a different filename as these files
        'cannot be changed by the user...

        Dim savefilename As String

        SaveFileDialog1.DefaultExt = ".csv"
        SaveFileDialog1.FileName = System.IO.Path.GetFileName(AnnotationDataDictionaryFile)
        SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
        'SaveFileDialog1.Filter = "csv |*.csv"
        SaveFileDialog1.Filter = "DataDictionary |*DataDictionary*.csv"
        SaveFileDialog1.ShowDialog()

        savefilename = SaveFileDialog1.FileName

        If Len(savefilename) > 0 Then

            If System.IO.Path.GetFileName(savefilename) = "DataDictionary.csv" Or
                    System.IO.Path.GetFileName(savefilename) = "CSAV2_DataDictionary.csv" Or
                    System.IO.Path.GetFileName(savefilename) = "HighContent_DataDictionary.csv" Or
                    System.IO.Path.GetFileName(savefilename) = "LowContent_DataDictionary.csv" Or
                    System.IO.Path.GetFileName(savefilename) = "Copilot_DataDictionary.csv" Or
                    System.IO.Path.GetFileName(savefilename) = "FCM_DataDictionary.csv" Then

                HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: You may not overwrite this file.  Please select a different filename.", DISPLAY_MSG_BOX)
                'MsgBox("You may not overwrite this file.  Please select a different filename.")
                Exit Sub

            End If

            AnnotationDataDictionaryFile = savefilename

            HandleEnableControls()

            HandleHideControls()

            SaveDataDictionary()
            ParseDataDictionary()

            ChangesMade = False
            SaveAndUpdateToolStripMenuItem.Enabled = False

            ExitToolStripMenuItem1.Text = "Exit"
            Button4.Text = "Exit"

            Me.Text = "Annotation Interface Configuration - " & System.IO.Path.GetFileName(AnnotationDataDictionaryFile)

        End If


    End Sub

    Private Sub ToolStripMenuItem3_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem3.Click

        'This is the REVERT button, reverts back to the originally loaded DataDictionary file which is based on Project Type (CSAV2, HighContent, etc)...

        AnnotationDataDictionaryFile = ""
        ParseDataDictionary()
        ChangesMade = False
        SaveAndUpdateToolStripMenuItem.Enabled = False

        ListBox1.SelectedIndex = -1
        ListBox2.SelectedIndex = -1
        ListBox3.SelectedIndex = -1
        ListBox5.SelectedIndex = -1

        ListBox1.Enabled = False
        ListBox2.Enabled = False
        ListBox3.Enabled = False
        ListBox5.Enabled = False

        Button1.Enabled = False
        Button2.Enabled = False
        Button3.Enabled = False

        HandleHideControls()

        ExitToolStripMenuItem1.Text = "Exit"
        Button4.Text = "Exit"

        Me.Text = "Annotation Interface Configuration - DEFAULT DataDictionary.csv"

    End Sub

    Private Sub HandleHideControls()

        'Called from various places, handles hiding controls after certain operations, so that only approrpiate controls will be displayed based
        'on subsequent user selections...

        ListBox2.Visible = False
        Label2.Visible = False
        Button2.Visible = False

        ListBox3.Visible = False
        Label3.Visible = False
        ListBox5.Visible = False
        Label5.Visible = False
        Button3.Visible = False

        ListBox4.Visible = False
        Label4.Visible = False

    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        'This is the Exit (or Exit - Discard Changes) button.  Same functionality as the ExitToolStripMenuItem1 drop down menu item...

        ExitToolStripMenuItem1_Click(sender, e)

    End Sub

    Private Sub ListBox5_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox5.SelectedIndexChanged

    End Sub

    Private Sub ListBox5_DoubleClick(sender As Object, e As EventArgs) Handles ListBox5.DoubleClick

        'Listbox5 displays the hot keys corresponding to each button...

        'Doubleclicking on an item in this list allows the user to change the text of the item...

        Dim inputstr As String

        inputstr = InputBox("Please enter new hotkey alphabetic character (other than C)", "USER INPUT", ListBox5.SelectedItem.ToString)

        If Len(inputstr) > 0 Then

            Do While Char.IsLetter(inputstr, 0) = False Or UCase(inputstr) = "C"
                'MsgBox("Please select a letter (A-Z, except C...")
                inputstr = InputBox("Please enter new hotkey alphabetic character (A-Z, except C...)", "USER INPUT", ListBox5.SelectedItem.ToString)

                'Do While UCase(inputstr) = "C"
                'MsgBox("C is a reserved character and cannot be used...")
                'inputstr = InputBox("Please enter new hotkey alphabetic character (other than C)", "USER INPUT", ListBox5.SelectedItem.ToString)
                'Loop
            Loop

            ListBox5.Items(ListBox5.SelectedIndex) = inputstr
            EnumerationTypeRecord(SelectedEnumTypeRecordIndex).HotKeyAssignment(ListBox5.SelectedIndex) = ListBox5.Items(ListBox5.SelectedIndex).ToString

            HandleChangesMade()

        End If

    End Sub
End Class