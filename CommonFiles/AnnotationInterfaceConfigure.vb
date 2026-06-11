Imports System.IO

Public Class AnnotationInterfaceConfigure

    'This form handles configuration of the annotation interface.  It is shown when the user right clicks on any tab or
    'button in the annotation section of the main CLEVIR screen...

    'The CLEVIR annotation interface is defined in a DataDictionary.csv file which is read in during initialization.  Information
    'from this file is placed into a data structure which is used to dynamically build the annotation tab and button interface
    'that is displayed on the CLEVIR main screen.

    'The AnnotationInterfaceConfigure form allows the user to make changes to the data structure and save these changes to a
    'different, user specified, DataDictionary file which is then read back in such that the changes are processed and implemented while CLEVIR
    'is still running...

    Private _selectedEnumTypeRecordIndex As Integer
    Private _selectedEnumButtonIndex As Integer

    Private _selectedAnnoTypeRecordIndex As Integer
    Private _selectedAnnoValueRecordIndex As Integer

    Private _selectedTypeID As Integer

    Private _changesMade As Boolean
    Private _saveSelectedItemText As String

    Public Sub HandleAnnotationInterfaceControlsDisplay()

        'This routine is called when the AnnotationInterfaceConfigure form is shown for the first time as a result of the
        'user right clicking on a tab or a button on the annotation interface part of the main CLEVIR screen...

        'Here we do some initialization stuff and display the name of the current Annotation Data Dictionary File 
        'in the AnnotationInterfaceConfigure form header...

        Dim disableEditing As Boolean

        Try

            ListBox2.SelectedIndex = -1
            ListBox1.SelectedIndex = -1

            HandleHideControls()

            If Len(AnnotationDataDictionaryFile) > 0 Then
                Text = "Annotation Interface Configuration - " & Path.GetFileName(AnnotationDataDictionaryFile)
                If _
                    Path.GetFileName(AnnotationDataDictionaryFile) = "DataDictionary.csv" Or
                    Path.GetFileName(AnnotationDataDictionaryFile) = "CSAV2_DataDictionary.csv" Or
                    Path.GetFileName(AnnotationDataDictionaryFile) = "HighContent_DataDictionary.csv" Or
                    Path.GetFileName(AnnotationDataDictionaryFile) = "LowContent_DataDictionary.csv" Or
                    Path.GetFileName(AnnotationDataDictionaryFile) = "ACP2_DataDictionary.csv" Or
                    Path.GetFileName(AnnotationDataDictionaryFile) = "ACP3_DataDictionary.csv" Or
                    Path.GetFileName(AnnotationDataDictionaryFile) = "ACP4_DataDictionary.csv" Or
                    Path.GetFileName(AnnotationDataDictionaryFile) = "FCM100_DataDictionary.csv" Or
                    Path.GetFileName(AnnotationDataDictionaryFile) = "FCM_DataDictionary.csv" Then
                    disableEditing = True
                End If
            Else
                Text = "Annotation Interface Configuration - DEFAULT DataDictionary.csv"
                disableEditing = True
            End If

            If disableEditing = True Then
                ListBox1.Enabled = False
                ListBox2.Enabled = False
                ListBox3.Enabled = False
                ListBox5.Enabled = False
                Button1.Enabled = False
                Button2.Enabled = False
                Button3.Enabled = False
            Else
                ListBox1.Enabled = True
                ListBox2.Enabled = True
                ListBox3.Enabled = True
                ListBox5.Enabled = True
                Button1.Enabled = True
                Button2.Enabled = True
                Button3.Enabled = True
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: " & ex.Message, DisplayMsgBox)
        End Try


    End Sub

    Private Sub SaveDataDictionary()

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        Dim FilenameToChange As String = AnnotationDataDictionaryFile
        Dim tempFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "tempfile.csv")

        Using writer As New StreamWriter(tempFilePath, False)
            writer.WriteLine("Enumeration Type Record,,,,,,,,,")
            writer.WriteLine("Record Type, ID, Enumeration, Desc,,,,,,")

            ' Enumerate through the EnumerationTypeRecords from the Singleton instance
            For Each enumRecord In dataDictionary.EnumerationTypeRecords
                For y = 0 To UBound(enumRecord.EnumerationDesc, 2)
                    Dim textline As String = $"{enumRecord.RecordType},{enumRecord.Id},{y},"
                    Dim tempstr As String = ""
                    For z = 0 To UBound(enumRecord.EnumerationDesc, 1)
                        tempstr &= $"{enumRecord.EnumerationDesc(z, y)},"
                    Next

                    If Len(enumRecord.HotKeyAssignment(y)) > 0 Then
                        tempstr &= enumRecord.HotKeyAssignment(y)
                    End If

                    textline &= tempstr
                    writer.WriteLine(textline)
                Next
            Next

            writer.WriteLine("Annotation Type Record,,,,,,,,,")
            writer.WriteLine("Record Type, Display Order, ID, System, Desc,,,,,,")

            ' Enumerate through the AnnotationTypeRecords from the Singleton instance
            For Each annoTypeRecord In dataDictionary.AnnotationTypeRecords
                Dim textline As String = $"{annoTypeRecord.RecordType},{annoTypeRecord.DisplayOrder},{annoTypeRecord.Id},{annoTypeRecord.System},{annoTypeRecord.Description},,,,,"
                writer.WriteLine(textline)
            Next

            writer.WriteLine("Annotation Value Record,,,,,,,,,")
            writer.WriteLine("Record Type, Type ID, ID, Enumeration Type, Desc,,,,,,")

            ' Enumerate through the AnnotationValueRecords from the Singleton instance
            For Each annoValueRecord In dataDictionary.AnnotationValueRecords
                Dim textline As String = $"{annoValueRecord.RecordType},{annoValueRecord.TypeId},{annoValueRecord.Id},{annoValueRecord.EnumerationType},{annoValueRecord.Description},,,,,"
                writer.WriteLine(textline)
            Next
        End Using

        File.Copy(tempFilePath, FilenameToChange, True)
        File.Delete(tempFilePath)

    End Sub

    Private Sub ListBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedIndexChanged

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        Dim tempstr As String = ""

        ' Clear and hide controls initially
        Try
            If ListBox2.SelectedIndex > -1 Then
                ListBox3.Items.Clear()
                ListBox3.Visible = True
                ListBox5.Items.Clear()
                ListBox5.Visible = True
                Button3.Visible = True
                Label3.Visible = True
                Label5.Visible = True
                ListBox4_Annotations.Visible = False
                Label4.Visible = False

                ' Find the selected AnnotationValueRecord
                For Each annotationValueRecord In dataDictionary.AnnotationValueRecords
                    If ListBox2.SelectedItem.ToString = annotationValueRecord.Description Then
                        _selectedAnnoValueRecordIndex = dataDictionary.AnnotationValueRecords.IndexOf(annotationValueRecord)

                        ' Find the matching EnumerationTypeRecord
                        For Each enumerationTypeRecord In dataDictionary.EnumerationTypeRecords
                            If enumerationTypeRecord.Id = annotationValueRecord.EnumerationType Then
                                _selectedEnumTypeRecordIndex = dataDictionary.EnumerationTypeRecords.IndexOf(enumerationTypeRecord)

                                ' Populate ListBox3 and ListBox5 with EnumerationDesc and HotKeyAssignment data
                                For z As Integer = 0 To UBound(enumerationTypeRecord.EnumerationDesc, 2)
                                    ListBox3.Items.Add(enumerationTypeRecord.EnumerationDesc(0, z))
                                    ListBox5.Items.Add(enumerationTypeRecord.HotKeyAssignment(z))
                                Next z

                                ' Exit the loop once the match is found
                                Exit For
                            End If
                        Next
                    End If
                Next

                ' Ensure unique hotkey assignments
                For x As Integer = 0 To ListBox5.Items.Count - 1
                    If InStr(tempstr, ListBox5.Items(x).ToString) = 0 Then
                        tempstr &= ListBox5.Items(x).ToString & ","
                    Else
                        MsgBox("There is more than one hotkey with the same letter assignment. Please make sure that all hotkeys are unique...")
                        Exit For
                    End If
                Next

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: " & ex.Message, DisplayMsgBox)
        End Try

    End Sub

    Private Sub ListBox3_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox3.SelectedIndexChanged

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        If ListBox3.SelectedIndex > -1 Then
            _saveSelectedItemText = ListBox3.SelectedItem.ToString

            ' Iterate over AnnotationValueRecords
            For Each annotationValueRecord In dataDictionary.AnnotationValueRecords

                ' If ListBox2 is displaying Driver Feedback tabs
                If ListBox2.SelectedIndex > -1 Then

                    ' Match selected tab in ListBox2 to an AnnotationValueRecord description
                    If ListBox2.SelectedItem.ToString = annotationValueRecord.Description Then
                        _selectedAnnoValueRecordIndex = dataDictionary.AnnotationValueRecords.IndexOf(annotationValueRecord)

                        ' Iterate over EnumerationTypeRecords to find a matching EnumerationType ID
                        For Each enumerationTypeRecord In dataDictionary.EnumerationTypeRecords
                            If enumerationTypeRecord.Id = annotationValueRecord.EnumerationType Then
                                _selectedEnumTypeRecordIndex = dataDictionary.EnumerationTypeRecords.IndexOf(enumerationTypeRecord)

                                ' Match selected item in ListBox3 to EnumerationDesc entries
                                For z As Integer = 0 To UBound(enumerationTypeRecord.EnumerationDesc, 2)
                                    If ListBox3.SelectedItem.ToString = enumerationTypeRecord.EnumerationDesc(0, z) Then
                                        _selectedEnumButtonIndex = z

                                        ' Display additional entries in ListBox4
                                        ListBox4_Annotations.Items.Clear()
                                        ListBox4_Annotations.Visible = True
                                        Label4.Visible = True

                                        ' Add additional description levels (1 to 5)
                                        For i As Integer = 1 To 5
                                            If Len(enumerationTypeRecord.EnumerationDesc(i, z)) > 0 Then
                                                ListBox4_Annotations.Items.Add(enumerationTypeRecord.EnumerationDesc(i, z))
                                            Else
                                                ListBox4_Annotations.Items.Add("Undefined")
                                            End If
                                        Next i

                                        Exit For
                                    End If
                                Next z
                                Exit For
                            End If
                        Next
                    End If

                Else ' ListBox2 is not displaying Driver Feedback tabs (Main Annotation Categories)

                    ' Match selected item in ListBox3 to AnnotationValueRecord descriptions
                    If ListBox3.SelectedItem.ToString = annotationValueRecord.Description Then
                        _selectedAnnoValueRecordIndex = dataDictionary.AnnotationValueRecords.IndexOf(annotationValueRecord)
                    End If

                End If
            Next
        End If

    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        ' Ensure a valid selection
        If ListBox1.SelectedIndex > -1 Then

            ' Reset selection and hide controls
            ListBox2.SelectedIndex = -1
            HandleHideControls()

            If ListBox1.SelectedItem.ToString = "Driver Feedback" Then
                ' Show ListBox2 and related controls for Driver Feedback
                ListBox2.Visible = True
                Label2.Visible = True
                Button2.Visible = True

                ' Iterate over AnnotationTypeRecords to find the matching record
                For Each annotationTypeRecord In dataDictionary.AnnotationTypeRecords
                    If ListBox1.SelectedItem.ToString = annotationTypeRecord.Description Then
                        _selectedAnnoTypeRecordIndex = dataDictionary.AnnotationTypeRecords.IndexOf(annotationTypeRecord)
                        Exit For
                    End If
                Next

            Else
                ' Display ListBox3 and associated controls for other categories
                ListBox3.Visible = True
                Label3.Visible = True
                ListBox5.Visible = True
                Label5.Visible = True
                Button3.Visible = True
                ListBox3.Items.Clear()
                ListBox4_Annotations.Items.Clear()
                ListBox5.Items.Clear()

                ' Find the selected main category and populate ListBox3 with related buttons
                For Each annotationTypeRecord In dataDictionary.AnnotationTypeRecords
                    If ListBox1.SelectedItem.ToString = annotationTypeRecord.Description Then
                        _selectedAnnoTypeRecordIndex = dataDictionary.AnnotationTypeRecords.IndexOf(annotationTypeRecord)
                        _selectedTypeID = annotationTypeRecord.Id

                        ' Add relevant AnnotationValueRecords to ListBox3
                        For Each annotationValueRecord In dataDictionary.AnnotationValueRecords
                            If annotationValueRecord.TypeId = annotationTypeRecord.Id Then
                                _selectedAnnoValueRecordIndex = dataDictionary.AnnotationValueRecords.IndexOf(annotationValueRecord)
                                ListBox3.Items.Add(annotationValueRecord.Description)
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

    Private Sub ListBox4_Annotations_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox4_Annotations.SelectedIndexChanged

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub

    Private Sub ListBox1_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox1.MouseDown

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        Dim deletedIndex As Integer = -1

        If e.Button = MouseButtons.Right Then
            ' Loop through selected items in ListBox1 from end to start to support removal
            For i As Integer = ListBox1.Items.Count - 1 To 0 Step -1
                If ListBox1.GetSelected(i) Then
                    ' Confirm deletion
                    If MsgBox("Delete Annotation Category?", vbYesNo) = vbYes Then
                        ListBox1.Items.RemoveAt(i)
                        deletedIndex = i ' Capture deleted index
                        Exit For
                    Else
                        Exit Sub
                    End If
                End If
            Next

            ' Refresh the list
            ListBox1.Refresh()

            ' If an item was deleted, remove it from the AnnotationTypeRecords
            If deletedIndex >= 0 Then
                If deletedIndex < dataDictionary.AnnotationTypeRecords.Count Then
                    dataDictionary.AnnotationTypeRecords.RemoveAt(deletedIndex)
                End If
                Handle_changesMade()
            End If

            ' Update visibility for relevant UI controls
            ListBox3.Visible = False
            Label3.Visible = False
            ListBox5.Visible = False
            Label5.Visible = False
            Button3.Visible = False
        End If
    End Sub

    Private Sub ListBox4_Annotations_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox4_Annotations.MouseDown
        ' Updated code to use ListBox4_Annotations instead of ListBox4
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        If e.Button = MouseButtons.Right AndAlso ListBox4_Annotations.SelectedItem IsNot Nothing AndAlso ListBox4_Annotations.SelectedItem.ToString() <> "Undefined" Then
            ' Loop through ListBox4_Annotations items in reverse to support removal
            For i As Integer = ListBox4_Annotations.Items.Count - 1 To 0 Step -1
                If ListBox4_Annotations.GetSelected(i) Then
                    ' Confirm deletion
                    If MsgBox("Remove Sub-Category?", vbYesNo) = vbYes Then
                        ListBox4_Annotations.Items.RemoveAt(i)
                    Else
                        Exit Sub
                    End If
                End If
            Next

            ' Refresh ListBox4_Annotations after deletion and add "Undefined" if not present
            ListBox4_Annotations.Refresh()
            If Not ListBox4_Annotations.Items.Contains("Undefined") Then
                ListBox4_Annotations.Items.Add("Undefined")
            End If

            ' Update DataDictionary EnumerationDesc with new ListBox4_Annotations items
            For index As Integer = 0 To Math.Min(4, ListBox4_Annotations.Items.Count - 1)
                Dim itemText = ListBox4_Annotations.Items(index).ToString()
                dataDictionary.EnumerationTypeRecords(_selectedEnumTypeRecordIndex).EnumerationDesc(index + 1, _selectedEnumButtonIndex) =
                    If(itemText <> "Undefined", itemText, "")
            Next

            ' Clear remaining unused sub-categories in EnumerationDesc if ListBox4_Annotations has fewer than 5 items
            For index As Integer = ListBox4_Annotations.Items.Count To 4
                dataDictionary.EnumerationTypeRecords(_selectedEnumTypeRecordIndex).EnumerationDesc(index + 1, _selectedEnumButtonIndex) = ""
            Next

            ' Notify changes
            Handle_changesMade()
        End If
    End Sub


    Private Sub ListBox2_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox2.MouseDown

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        Dim deleted As Boolean = False

        If e.Button = MouseButtons.Right Then
            ' Loop through ListBox2 items in reverse to support removal
            For i As Integer = ListBox2.Items.Count - 1 To 0 Step -1
                If ListBox2.GetSelected(i) Then
                    ' Confirm deletion
                    If MsgBox("Delete Annotation Category?", vbYesNo) = vbYes Then
                        ListBox2.Items.RemoveAt(i)
                        deleted = True
                    Else
                        Exit Sub
                    End If
                End If
            Next

            ' Refresh ListBox2 after deletion
            ListBox2.Refresh()

            ' If an item was deleted, remove it from AnnotationValueRecords
            If deleted Then
                Dim newAnnotationValueRecords As New List(Of DataDictionarySingleton.AnnotationValueRecord)()

                ' Copy records except for the one matching the selected index
                For Each record In dataDictionary.AnnotationValueRecords
                    If record.TypeId <> _selectedAnnoValueRecordIndex Then
                        newAnnotationValueRecords.Add(record)
                    End If
                Next

                ' Update AnnotationValueRecords with the modified list
                dataDictionary.AnnotationValueRecords = newAnnotationValueRecords

                ' Hide UI elements associated with ListBox2
                ListBox3.Visible = False
                Label3.Visible = False
                ListBox5.Visible = False
                Label5.Visible = False
                Button3.Visible = False

                ' Indicate that changes have been made
                Handle_changesMade()
            End If
        End If
    End Sub

    Private Sub AnnotationInterfaceConfigure_Load(sender As Object, e As EventArgs) Handles Me.Load

        'This routine is called when the AnnotationInterfaceConfigure form is shown for the first time as a result of the
        'user right clicking on a tab or a button on the annotation interface part of the main CLEVIR screen...

        'Here we do some initialization stuff and display the name of the current Annotation Data Dictionary File 
        'in the AnnotationInterfaceConfigure form header...

        ' Get the DataDictionarySingleton instance
        'Dim dataDictionary = DataDictionarySingleton.GetInstance()

        ListBox2.SelectedIndex = -1
        ListBox1.SelectedIndex = -1

        HandleHideControls()

        If Len(AnnotationDataDictionaryFile) > 0 Then
            Text = "Annotation Interface Configuration - " & Path.GetFileName(AnnotationDataDictionaryFile)
        Else
            Text = "Annotation Interface Configuration - DEFAULT DataDictionary.csv"
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
        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        Dim deleted As Boolean = False

        If e.Button = MouseButtons.Right Then
            ' Loop through ListBox3 items in reverse to support removal
            For i As Integer = ListBox3.Items.Count - 1 To 0 Step -1
                If ListBox3.GetSelected(i) Then
                    ' Confirm deletion
                    If MsgBox("Delete Annotation Button?", vbYesNo) = vbYes Then
                        ListBox3.Items.RemoveAt(i)
                        ListBox5.Items.RemoveAt(i)
                        deleted = True
                    Else
                        Exit Sub
                    End If
                End If
            Next

            ' Refresh ListBox3 and ListBox5 after deletion
            ListBox3.Refresh()
            ListBox5.Refresh()

            ' If ListBox3 is displaying Main Annotation Categories, handle AnnotationValueRecords deletion
            If ListBox2.SelectedIndex = -1 Then
                If deleted Then
                    ' Rebuild the AnnotationValueRecords list without the deleted item
                    Dim newAnnotationValueRecords = dataDictionary.AnnotationValueRecords.
                        Where(Function(record, index) index <> _selectedAnnoValueRecordIndex).ToList()
                    dataDictionary.AnnotationValueRecords = newAnnotationValueRecords
                End If

            Else
                ' ListBox3 is displaying buttons associated with a Driver Feedback tab
                Dim annotationValueRecord = dataDictionary.AnnotationValueRecords.
                    FirstOrDefault(Function(record) record.Description = ListBox2.SelectedItem.ToString)

                If Not annotationValueRecord.Equals(Nothing) AndAlso deleted Then
                    Dim enumerationTypeRecord = dataDictionary.EnumerationTypeRecords.
                        FirstOrDefault(Function(enumRecord) enumRecord.Id = annotationValueRecord.EnumerationType)

                    If Not enumerationTypeRecord.Equals(Nothing) Then
                        ' Manually filter the EnumerationDesc rows to exclude the deleted sub-category
                        Dim rows As Integer = enumerationTypeRecord.EnumerationDesc.GetLength(0)
                        Dim columns As Integer = enumerationTypeRecord.EnumerationDesc.GetLength(1)
                        Dim filteredList As New List(Of String())

                        For row = 0 To rows - 1
                            If enumerationTypeRecord.EnumerationDesc(row, 0) <> _saveSelectedItemText Then
                                ' Create a new row array and populate it
                                Dim newRow(columns - 1) As String
                                For col = 0 To columns - 1
                                    newRow(col) = enumerationTypeRecord.EnumerationDesc(row, col)
                                Next
                                filteredList.Add(newRow)
                            End If
                        Next

                        ' Convert the filtered list back to a 2D array
                        Dim newEnumerationDesc(filteredList.Count - 1, columns - 1) As String
                        For row = 0 To filteredList.Count - 1
                            For col = 0 To columns - 1
                                newEnumerationDesc(row, col) = filteredList(row)(col)
                            Next
                        Next

                        ' Reassign the new array back to EnumerationDesc
                        enumerationTypeRecord.EnumerationDesc = newEnumerationDesc
                    End If
                End If
            End If

            ' Hide ListBox4 and its label after deletion
            ListBox4_Annotations.Visible = False
            Label4.Visible = False

            ' Indicate that changes have been made
            Handle_changesMade()
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        Dim maxAnnotationTypeRecordID As Integer = 0
        Dim maxDisplayOrder As Integer = 0
        Dim annotationCategoryName As String

        annotationCategoryName = InputBox("Please enter the name of the new Annotation Category", "ANNOTATION CATEGORY INPUT", "New Category Name")

        If Len(annotationCategoryName) > 0 Then
            ' Find the current maximum ID and display order in AnnotationTypeRecords
            For Each record In dataDictionary.AnnotationTypeRecords
                If record.Id < 1998 AndAlso record.Id > maxAnnotationTypeRecordID Then
                    maxAnnotationTypeRecordID = record.Id
                    maxDisplayOrder = record.DisplayOrder
                End If
            Next

            ' Create the new AnnotationTypeRecord with updated values
            Dim newRecord As New DataDictionarySingleton.AnnotationTypeRecord With {
                .RecordType = 1,
                .Id = maxAnnotationTypeRecordID + 1,
                .DisplayOrder = maxDisplayOrder + 1,
                .System = 0,
                .Description = annotationCategoryName
            }

            ' Add the new record to the AnnotationTypeRecords list
            dataDictionary.AnnotationTypeRecords.Add(newRecord)

            ' Clear and repopulate ListBox1 with updated AnnotationTypeRecords
            ListBox1.Items.Clear()
            For Each record In dataDictionary.AnnotationTypeRecords
                If record.Id < 1998 Then
                    ListBox1.Items.Add(record.Description)
                End If
            Next

            Handle_changesMade()

        Else
            HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: Invalid Name Entered...", DisplayMsgBox)
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        ' This is the Add Annotation Button button associated with Listbox3

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        Dim saveAnnotationTypeRecordID As Integer
        Dim saveAnnotationValueRecordEnumType As Integer
        Dim annotationButtonName As String
        Dim found As Boolean = False

        annotationButtonName = InputBox("Please enter the text for the new Annotation Button", "ANNOTATION BUTTON TEXT INPUT", "New Button Text")

        If Len(annotationButtonName) > 0 Then
            ' This behavior is different depending on whether we are displaying Main Annotation Category button text or Driver Feedback tab button text
            If ListBox2.SelectedIndex > -1 Then
                ' We are displaying Driver Feedback tab button text
                For Each annotationValueRecord In dataDictionary.AnnotationValueRecords
                    If annotationValueRecord.Description = ListBox2.SelectedItem.ToString Then
                        saveAnnotationValueRecordEnumType = annotationValueRecord.EnumerationType
                        Exit For
                    End If
                Next

                For Each enumerationTypeRecord In dataDictionary.EnumerationTypeRecords
                    If enumerationTypeRecord.Id = saveAnnotationValueRecordEnumType Then
                        ' Resize EnumerationDesc and HotKeyAssignment arrays to add a new entry
                        Dim newDescSize = enumerationTypeRecord.EnumerationDesc.GetLength(1) + 1
                        ReDim Preserve enumerationTypeRecord.EnumerationDesc(5, newDescSize - 1)
                        ReDim Preserve enumerationTypeRecord.HotKeyAssignment(newDescSize - 1)

                        ' Assign the new button name and hotkey
                        enumerationTypeRecord.EnumerationDesc(0, newDescSize - 1) = annotationButtonName
                        enumerationTypeRecord.HotKeyAssignment(newDescSize - 1) = Mid(annotationButtonName, 1, 1)
                        found = True
                        Exit For
                    End If
                Next

                ' If not found, add a new EnumerationTypeRecord
                If Not found Then
                    Dim newEnumerationTypeRecord As New DataDictionarySingleton.EnumerationTypeRecord With {
                        .RecordType = 3,
                        .Id = saveAnnotationValueRecordEnumType
                    }
                    ReDim newEnumerationTypeRecord.EnumerationDesc(5, 0)
                    ReDim newEnumerationTypeRecord.HotKeyAssignment(0)
                    newEnumerationTypeRecord.EnumerationDesc(0, 0) = annotationButtonName
                    newEnumerationTypeRecord.HotKeyAssignment(0) = Mid(annotationButtonName, 1, 1)

                    ' Add the new record to EnumerationTypeRecords
                    dataDictionary.EnumerationTypeRecords.Add(newEnumerationTypeRecord)
                End If

                ' Refresh ListBox3 and ListBox5
                ListBox3.Items.Clear()
                ListBox5.Items.Clear()
                For Each enumerationTypeRecord In dataDictionary.EnumerationTypeRecords
                    If enumerationTypeRecord.Id = saveAnnotationValueRecordEnumType Then
                        For y = 0 To enumerationTypeRecord.EnumerationDesc.GetLength(1) - 1
                            ListBox3.Items.Add(enumerationTypeRecord.EnumerationDesc(0, y))
                            ListBox5.Items.Add(enumerationTypeRecord.HotKeyAssignment(y))
                        Next
                    End If
                Next

                Handle_changesMade()
            Else
                ' We are displaying Main Annotation Category button text
                If ListBox1.SelectedIndex > -1 Then
                    ' Find the relevant AnnotationTypeRecord ID
                    For Each annotationTypeRecord In dataDictionary.AnnotationTypeRecords
                        If annotationTypeRecord.Description = ListBox1.SelectedItem.ToString Then
                            saveAnnotationTypeRecordID = annotationTypeRecord.Id
                            Exit For
                        End If
                    Next

                    ' Insert a new AnnotationValueRecord
                    Dim newAnnotationValueRecord As New DataDictionarySingleton.AnnotationValueRecord With {
                        .Description = annotationButtonName,
                        .RecordType = 2,
                        .TypeId = saveAnnotationTypeRecordID,
                        .Id = 0,
                        .EnumerationType = 0
                    }
                    dataDictionary.AnnotationValueRecords.Add(newAnnotationValueRecord)

                    ' Refresh ListBox3 with updated AnnotationValueRecords
                    ListBox3.Items.Clear()
                    ListBox5.Items.Clear()
                    For Each annotationValueRecord In dataDictionary.AnnotationValueRecords
                        If annotationValueRecord.TypeId = saveAnnotationTypeRecordID Then
                            ListBox3.Items.Add(annotationValueRecord.Description)
                        End If
                    Next

                    Handle_changesMade()
                Else
                    MsgBox("Please Reselect Annotation Category!")
                End If
            End If
        Else
            HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: Invalid Button Text Entered...", DisplayMsgBox)
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ' This is the Add Driver Feedback Tab button associated with Listbox2

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        Dim annotationCategoryName As String
        Dim maxID As Integer = 0
        Dim maxEnumType As Integer = 0

        annotationCategoryName = InputBox("Please enter the name of the new Driver Feedback Annotation Tab", "DRIVER FEEDBACK ANNOTATION TAB INPUT", "New Tab Name")

        If Len(annotationCategoryName) > 0 Then
            ' Get max ID and EnumerationType values
            maxID = dataDictionary.AnnotationValueRecords.Max(Function(record) record.Id)
            maxEnumType = dataDictionary.AnnotationValueRecords.Max(Function(record) record.EnumerationType)

            ' Create the new record and configure its properties
            Dim newAnnotationValueRecord As New DataDictionarySingleton.AnnotationValueRecord With {
                .RecordType = 2,
                .TypeId = 1000,
                .Id = maxID + 1,
                .EnumerationType = maxEnumType + 10,
                .Description = annotationCategoryName
            }

            ' Add the new record to AnnotationValueRecords
            dataDictionary.AnnotationValueRecords.Add(newAnnotationValueRecord)

            ' Refresh ListBox2 with updated AnnotationValueRecords
            ListBox2.Items.Clear()
            For Each record In dataDictionary.AnnotationValueRecords
                If record.EnumerationType > 3140 Then
                    ListBox2.Items.Add(record.Description)
                End If
            Next

            Handle_changesMade()
        Else
            HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: Invalid Text Entered...", DisplayMsgBox)
        End If
    End Sub

    Private Sub ListBox4_Annotations_DoubleClick(sender As Object, e As EventArgs) Handles ListBox4_Annotations.DoubleClick

        ' ListBox4_Annotations displays the sub-categories associated with any button associated with any Driver Feedback tab.
        ' Double-clicking an item in this list allows the user to change the text of the item.
        ' If double-clicking an "undefined" item, a new Sub-Category will be added.

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        ' Prompt user for new sub-category text
        Dim inputstr As String = InputBox("Please enter new Annotation Sub-Category Description", "USER INPUT", ListBox4_Annotations.SelectedItem.ToString())

        If Len(inputstr) > 0 Then
            ' Update the selected item in the ListBox
            ListBox4_Annotations.Items(ListBox4_Annotations.SelectedIndex) = inputstr

            ' Update the corresponding data in DataDictionary's EnumerationDesc
            For i As Integer = 1 To 5
                If i <= ListBox4_Annotations.Items.Count Then
                    Dim itemText = ListBox4_Annotations.Items(i - 1).ToString()
                    dataDictionary.EnumerationTypeRecords(_selectedEnumTypeRecordIndex).EnumerationDesc(i, _selectedEnumButtonIndex) =
                        If(itemText <> "Undefined", itemText, "")
                Else
                    ' Clear unused sub-category slots if fewer than 5 items in ListBox4_Annotations
                    dataDictionary.EnumerationTypeRecords(_selectedEnumTypeRecordIndex).EnumerationDesc(i, _selectedEnumButtonIndex) = ""
                End If
            Next i

            ' Indicate that changes were made
            Handle_changesMade()
        End If

    End Sub


    Private Sub ListBox3_DoubleClick(sender As Object, e As EventArgs) Handles ListBox3.DoubleClick
        ' Listbox3 displays the button text of the buttons associated with either the annotation category selected in Listbox1, 
        ' Or the Driver Feedback tab selected in Listbox2...

        ' Double-clicking on an item in this list allows the user to change the text of the item...

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        ' Prompt the user to enter new button text
        Dim inputStr As String = InputBox("Please enter new Button Text", "USER INPUT", ListBox3.SelectedItem.ToString)

        If Len(inputStr) > 0 Then
            ' Update the ListBox3 item text and the data dictionary property accordingly
            ListBox3.Items(ListBox3.SelectedIndex) = inputStr

            If ListBox2.SelectedIndex = -1 Then
                ' Retrieve, modify, and update the structure in AnnotationValueRecords if no sub-selection (Driver Feedback tab) is active
                Dim updatedRecord = dataDictionary.AnnotationValueRecords(_selectedAnnoValueRecordIndex)
                updatedRecord.Description = inputStr
                dataDictionary.AnnotationValueRecords(_selectedAnnoValueRecordIndex) = updatedRecord
            Else
                ' Update the EnumerationDesc for the selected EnumerationTypeRecord
                dataDictionary.EnumerationTypeRecords(_selectedEnumTypeRecordIndex).EnumerationDesc(0, ListBox3.SelectedIndex) = inputStr
            End If

            ' Indicate that changes have been made
            Handle_changesMade()
        End If
    End Sub

    Private Sub ListBox1_DoubleClick(sender As Object, e As EventArgs) Handles ListBox1.DoubleClick
        ' Listbox1 displays the Main Annotation Categories (Driver Feedback, Road Type, etc.)...

        ' Double-clicking on an item in this list allows the user to change the text of the item...

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        ' Prompt the user to enter new Main Tab Text
        Dim inputStr As String = InputBox("Please enter new Main Tab Text", "USER INPUT", ListBox1.SelectedItem.ToString)

        If Len(inputStr) > 0 Then
            ' Update the ListBox1 item text
            ListBox1.Items(ListBox1.SelectedIndex) = inputStr

            ' Retrieve, modify, and update the AnnotationTypeRecord structure
            Dim updatedRecord = dataDictionary.AnnotationTypeRecords(_selectedAnnoTypeRecordIndex)
            updatedRecord.Description = inputStr
            dataDictionary.AnnotationTypeRecords(_selectedAnnoTypeRecordIndex) = updatedRecord

            ' Indicate that changes have been made
            Handle_changesMade()
        End If
    End Sub

    Private Sub ListBox2_DoubleClick(sender As Object, e As EventArgs) Handles ListBox2.DoubleClick
        ' Listbox2 displays the Driver Feedback Annotation Tabs (CPS, LKA, LCC, etc.)
        ' It is made visible if the user selects Driver Feedback from Listbox1.
        ' Double-clicking on an item in this list allows the user to change the text of the item.

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        ' Prompt the user to enter new Driver Feedback Tab Text
        Dim inputStr As String = InputBox("Please enter new Driver Feedback Tab Text", "USER INPUT", ListBox2.SelectedItem.ToString)

        If Len(inputStr) > 0 Then
            ' Update the ListBox2 item text
            ListBox2.Items(ListBox2.SelectedIndex) = inputStr

            ' Retrieve, modify, and update the AnnotationValueRecord structure
            Dim updatedRecord = dataDictionary.AnnotationValueRecords(_selectedAnnoValueRecordIndex)
            updatedRecord.Description = inputStr
            dataDictionary.AnnotationValueRecords(_selectedAnnoValueRecordIndex) = updatedRecord

            ' Indicate that changes have been made
            Handle_changesMade()
        End If
    End Sub


    Private Sub AnnotationInterfaceConfigure_Shown(sender As Object, e As EventArgs) Handles Me.Shown

    End Sub

    Private Sub AnnotationInterfaceConfigure_Activated(sender As Object, e As EventArgs) Handles Me.Activated

    End Sub

    Private Sub AnnotationInterfaceConfigure_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged

    End Sub

    Private Sub Handle_changesMade()

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

        _changesMade = True
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

                If Path.GetFileName(OpenFileDialog1.FileName) = "DataDictionary.csv" Or Path.GetFileName(OpenFileDialog1.FileName) = "CSAV2_DataDictionary.csv" Or Path.GetFileName(OpenFileDialog1.FileName) = "HighContent_DataDictionary.csv" Or Path.GetFileName(OpenFileDialog1.FileName) = "LowContent_DataDictionary.csv" Or Path.GetFileName(OpenFileDialog1.FileName) = "Copilot_DataDictionary.csv" Or Path.GetFileName(OpenFileDialog1.FileName) = "FCM_DataDictionary.csv" Then

                    HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: " & Path.GetFileName(OpenFileDialog1.FileName) & " cannot be modified...", DisplayMsgBox)
                    'MsgBox(System.Path.GetFileName(OpenFileDialog1.FileName) & " cannot be modified...")

                    Exit Sub

                End If

                AnnotationDataDictionaryFile = OpenFileDialog1.FileName

                HandleEnableControls()

                ParseDataDictionary()

                HandleHideControls()

                Text = "Annotation Interface Configuration - " & Path.GetFileName(AnnotationDataDictionaryFile)

                Exit Sub

            End If
        End If

        HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: Invalid Annotation Data Dictionary file selected...", DisplayMsgBox)

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

        If _changesMade = True Then
            ParseDataDictionary()
            _changesMade = False
            SaveAndUpdateToolStripMenuItem.Enabled = False
        End If

        Hide()

    End Sub

    Private Sub SaveAndUpdateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveAndUpdateToolStripMenuItem.Click

        'This is the Update and Save drop down menu item...

        'Here we save changes to the active data dictionary file and reload it so that the changes take effect and we are running
        'with the newly changed data dictionary file...

        HandleHideControls()

        SaveDataDictionary()
        ParseDataDictionary()

        _changesMade = False
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
        SaveFileDialog1.FileName = Path.GetFileName(AnnotationDataDictionaryFile)
        SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
        'SaveFileDialog1.Filter = "csv |*.csv"
        SaveFileDialog1.Filter = "DataDictionary |*DataDictionary*.csv"
        SaveFileDialog1.ShowDialog()

        savefilename = SaveFileDialog1.FileName

        If Len(savefilename) > 0 Then

            If Path.GetFileName(savefilename) = "DataDictionary.csv" Or Path.GetFileName(savefilename) = "CSAV2_DataDictionary.csv" Or Path.GetFileName(savefilename) = "HighContent_DataDictionary.csv" Or Path.GetFileName(savefilename) = "LowContent_DataDictionary.csv" Or Path.GetFileName(savefilename) = "Copilot_DataDictionary.csv" Or Path.GetFileName(savefilename) = "FCM_DataDictionary.csv" Then

                HandleUserMessageLogging("GMRC", "AnnotationInterfaceConfigure: You may not overwrite this file.  Please select a different filename.", DisplayMsgBox)
                'MsgBox("You may not overwrite this file.  Please select a different filename.")
                Exit Sub

            End If

            AnnotationDataDictionaryFile = savefilename

            HandleEnableControls()

            HandleHideControls()

            SaveDataDictionary()
            ParseDataDictionary()

            _changesMade = False
            SaveAndUpdateToolStripMenuItem.Enabled = False

            ExitToolStripMenuItem1.Text = "Exit"
            Button4.Text = "Exit"

            Text = "Annotation Interface Configuration - " & Path.GetFileName(AnnotationDataDictionaryFile)

        End If


    End Sub

    Private Sub ToolStripMenuItem3_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem3.Click

        'This is the REVERT button, reverts back to the originally loaded DataDictionary file which is based on Project Type (CSAV2, HighContent, etc)...

        AnnotationDataDictionaryFile = ""
        ParseDataDictionary()
        _changesMade = False
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

        Text = "Annotation Interface Configuration - DEFAULT DataDictionary.csv"

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

        ListBox4_Annotations.Visible = False
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

        ' Get the DataDictionarySingleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

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
            dataDictionary.EnumerationTypeRecords(_selectedEnumTypeRecordIndex).HotKeyAssignment(ListBox5.SelectedIndex) = ListBox5.Items(ListBox5.SelectedIndex).ToString

            Handle_changesMade()

        End If

    End Sub
End Class