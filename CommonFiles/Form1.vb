Option Strict Off


Imports de.etas.cebra.toolAPI.Inca
Imports de.etas.cebra.toolAPI.Common

Public Class Form1

    'This is the calibration access form.  It allows the user to create custom calibration displays in INCA.

    Public WhichDisplayIsInFront As String

    Private Function GetCalibrationElementInDevice(ByVal myCalVarName As String, ByVal myExperimentDevice As ExperimentDevice, ByRef CalibrationElement As Object, ByRef CalibrationValue As Object) As Boolean

        'Called from AddCalDisplayToInca - Returns True if myCalVarName is found in the device...

        GetCalibrationElementInDevice = False

        CalibrationElement = myIncaOnlineExperiment.GetCalibrationElementInDevice(myCalVarName, myExperimentDevice)
        If CalibrationElement Is Nothing Then
            MsgBox("Element " & myCalVarName & " not found.")
            Exit Function
        End If

        CalibrationValue = CalibrationElement.GetValue

        If CalibrationValue Is Nothing Then
            MsgBox("Could not retrieve CalibrationValue from  " & myCalVarName)
            Exit Function
        End If

        GetCalibrationElementInDevice = True

    End Function

    Private Function GetMeasureElementInDevice(ByVal myMeasVarName As String, ByVal myExperimentDevice As ExperimentDevice, ByRef MeasureElement As Object, ByRef MeasureValue As Object) As Boolean

        'Called from AddMeasDisplayToInca - Returns True if myMeasVarName is found in the device...

        GetMeasureElementInDevice = False

        MeasureElement = myIncaOnlineExperiment.GetMeasureElementInDevice(myMeasVarName, myExperimentDevice)
        If MeasureElement Is Nothing Then
            MsgBox("Element " & myMeasVarName & " not found.")
            Exit Function
        End If

        MeasureValue = MeasureElement.GetValue

        If MeasureValue Is Nothing Then
            MsgBox("Could not retrieve CalibrationValue from  " & myMeasVarName)
            Exit Function
        End If

        GetMeasureElementInDevice = True

    End Function

    Private Sub AddMeasDisplayToInca(ByVal myString As String)

        'Adds a measure display window to inca based on the contents of the string passed in.  String contains
        'a measurement variable name and a device name.

        Dim MeasObject As Object = Nothing

        'Dim MeasureElement As Object = Nothing
        Dim myMeasureElement As MeasureElement = Nothing

        Dim MeasName As String
        Dim DeviceName As String
        Dim myExperimentDevice As ExperimentDevice = Nothing

        Dim x As Boolean
        Static cnt As Integer = 0

        MeasName = Trim(Mid(myString, InStr(myString, Chr(9)) + 1, Len(myString)))
        DeviceName = Trim(Mid(myString, 1, InStr(myString, Chr(9)) - 1))

        myExperimentDevice = myIncaOnlineExperiment.GetDevice(DeviceName)

        If Not myExperimentDevice Is Nothing Then

            If GetMeasureElementInDevice(MeasName, myExperimentDevice, myMeasureElement, MeasObject) = True Then

                'x = myMeasureElement.OpenView
                'x = myMeasureElement.OpenViewWithMaxLabels(10)

                If IsNumeric(TextBox2.Text) And Val(TextBox2.Text) >= 1 And Val(TextBox2.Text) <= 20 Then
                    x = myMeasureElement.OpenViewWithMaxLabels(Val(TextBox2.Text))
                Else
                    x = myMeasureElement.OpenViewWithMaxLabels(20)
                End If


            Else
                MsgBox("INCA Measure Element " & MeasName & " not found.")
            End If

        Else
            MsgBox("INCA Device " & DeviceName & " not found.")
        End If
    End Sub

    Private Sub AddCalDisplayToInca(ByVal myString As String)

        'Adds a cabliration display window to inca based on the contents of the string passed in.  String contains
        'a calibration variable name and a device name.


        Dim CalObject As Object = Nothing

        Dim myCalibrationElement As CalibrationElement = Nothing

        Dim CalName As String
        Dim DeviceName As String
        Dim myExperimentDevice As ExperimentDevice = Nothing

        Dim x As Boolean
        Static cnt As Integer = 0

        CalName = Trim(Mid(myString, InStr(myString, Chr(9)) + 1, Len(myString)))
        DeviceName = Trim(Mid(myString, 1, InStr(myString, Chr(9)) - 1))

        myExperimentDevice = myIncaOnlineExperiment.GetDevice(DeviceName)

        If Not myExperimentDevice Is Nothing Then

            If GetCalibrationElementInDevice(CalName, myExperimentDevice, myCalibrationElement, CalObject) = True Then

                'If CalObject.isscalar = True Then

                'x = myExpEnvView.OpenViewForExperimentElement(myCalibrationElement)

                x = myCalibrationElement.OpenView

                'CalValue = CalObject.GetDoublePhysValue.ToString
                'MsgBox(Trim(ListBox1.SelectedItem.ToString) & " = " & CalValue)

                'Else

                'If Not myExpEnvView Is Nothing Then

                'ReDim y(cnt)
                'y(cnt) = myExpEnvView.OpenViewForExperimentElement(CalibrationElement)
                'cnt = cnt + 1

                'End If

                'Me.SendToBack()
                'OnVehicleScreen.Hide()

            End If
        Else
            MsgBox("INCA Device " & DeviceName & " not found.")
        End If
    End Sub

    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        OnVehicleScreen.Show()
    End Sub

    Private Sub Form1_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'Called when the calibration form (form1) is loaded...

        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sfile As Scripting.File

        If System.IO.Directory.Exists(My.Application.Info.DirectoryPath & "\ExperimentDisplays") = False Then
            System.IO.Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\ExperimentDisplays")
        Else

            FSO = New Scripting.FileSystemObject
            f = FSO.GetFolder(My.Application.Info.DirectoryPath & "\ExperimentDisplays")

            For Each sfile In f.Files
                ListBox4.Items.Add(sfile.Name)
            Next

        End If
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'This is the Toggle Display button...

        If WhichDisplayIsInFront = "Calibrate" Then
            Me.SendToBack()
            OnVehicleScreen.Hide()
            WhichDisplayIsInFront = "INCA"
        ElseIf WhichDisplayIsInFront = "INCA" Then
            OnVehicleScreen.Show()
            OnVehicleScreen.Refresh()
            WhichDisplayIsInFront = "CLEVIR"
        End If

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        'This is the Add Selected Items to Display List button...

        Dim x As Integer

        If Label1.Text = "Calibration Item List" Then
            For x = 0 To ListBox1.Items.Count - 1
                If ListBox1.GetSelected(x) = True Then
                    ListBox2.Items.Add(ListBox1.Items(x).ToString)
                    'AddCalDisplayToInca(ListBox1.Items(x).ToString)
                End If
            Next
        Else
            For x = 0 To ListBox3.Items.Count - 1
                If ListBox3.GetSelected(x) = True Then
                    ListBox2.Items.Add(ListBox3.Items(x).ToString)
                    'AddMeasDisplayToInca(ListBox3.Items(x).ToString)
                End If
            Next
        End If

    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click

        'This is the clear list button...

        ListBox2.Items.Clear()
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click

        'This is the Calibration Items button...

        Dim x As Integer
        Dim fnum As Integer

        Dim textline As String

        Try

            Label1.Text = "Calibration Item List"

            TextBox1.Text = ""

            ListBox1.Visible = True
            ListBox3.Visible = False

            Me.Cursor = Cursors.WaitCursor

            Me.Refresh()

            If ListBox1.Items.Count = 0 Then

                If Not myCalInfo Is Nothing Then

                    For x = 0 To UBound(myCalInfo)
                        If Len(myCalInfo(x).DeviceName) = 2 Then
                            ListBox1.Items.Add(myCalInfo(x).DeviceName & " " & Chr(9) & myCalInfo(x).CalName)
                        Else
                            ListBox1.Items.Add(myCalInfo(x).DeviceName & Chr(9) & myCalInfo(x).CalName)
                        End If
                    Next

                Else
                    fnum = FreeFile()
                    FileOpen(fnum, My.Application.Info.DirectoryPath & "\MasterCalList.txt", OpenMode.Input)
                    Do While Not EOF(fnum)
                        textline = LineInput(fnum)
                        ListBox1.Items.Add(textline)
                        If myCalInfo Is Nothing Then
                            ReDim Preserve myCalInfo(0)
                        Else
                            ReDim Preserve myCalInfo(UBound(myCalInfo) + 1)
                        End If
                        myCalInfo(UBound(myCalInfo)).DeviceName = Mid(textline, 1, InStr(textline, Chr(9)) - 1)
                        myCalInfo(UBound(myCalInfo)).CalName = Mid(textline, InStr(textline, Chr(9)) + 1, Len(textline))
                    Loop

                    FileClose(fnum)
                End If

            End If

        Catch
            MsgBox(Err.Number & " - " & Err.Description)
        End Try

        Me.Cursor = Cursors.Arrow


    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click

        'This is the measurement items button...

        Dim x As Integer
        Dim fnum As Integer
        Dim textline As String

        Try

            Label1.Text = "Measurement Variable List"

            TextBox1.Text = ""
            ListBox3.Visible = True
            ListBox1.Visible = False

            Me.Cursor = Cursors.WaitCursor

            Me.Refresh()

            If ListBox3.Items.Count = 0 Then

                If Not myMeasInfo Is Nothing Then

                    For x = 0 To UBound(myMeasInfo)

                        If Mid(myMeasInfo(x).MeasName, 1, 2) <> "V_" And InStr(myMeasInfo(x).MeasName, "BrstGrp") = 0 _
                            And InStr(myMeasInfo(x).MeasName, "e_a_SPI_RxBuffer") = 0 And InStr(myMeasInfo(x).MeasName, "e_a_SPI_TxBuffer") = 0 _
                            And InStr(myMeasInfo(x).MeasName, "e_a_Data") = 0 And InStr(myMeasInfo(x).MeasName, "CeTSKR_e_TimeBased") = 0 _
                            And InStr(myMeasInfo(x).MeasName, "PduInfo") = 0 And InStr(myMeasInfo(x).MeasName, "CanHardwareObjectData") = 0 Then

                            If Len(myMeasInfo(x).DeviceName) = 2 Then
                                ListBox3.Items.Add(myMeasInfo(x).DeviceName & " " & Chr(9) & myMeasInfo(x).MeasName)
                            Else
                                ListBox3.Items.Add(myMeasInfo(x).DeviceName & Chr(9) & myMeasInfo(x).MeasName)
                            End If

                        End If

                    Next

                Else
                    fnum = FreeFile()
                    FileOpen(fnum, My.Application.Info.DirectoryPath & "\MasterMeasVarList.txt", OpenMode.Input)
                    Do While Not EOF(fnum)
                        textline = LineInput(fnum)
                        If Mid(textline, 1, 2) <> "V_" And InStr(textline, "BrstGrp") = 0 _
                            And InStr(textline, "e_a_SPI_RxBuffer") = 0 And InStr(textline, "e_a_SPI_TxBuffer") = 0 _
                            And InStr(textline, "e_a_Data") = 0 And InStr(textline, "CeTSKR_e_TimeBased") = 0 _
                            And InStr(textline, "PduInfo") = 0 And InStr(textline, "CanHardwareObjectData") = 0 Then

                            ListBox3.Items.Add(textline)
                            If myMeasInfo Is Nothing Then
                                ReDim Preserve myMeasInfo(0)
                            Else
                                ReDim Preserve myMeasInfo(UBound(myMeasInfo) + 1)
                            End If
                            myMeasInfo(UBound(myMeasInfo)).DeviceName = Mid(textline, 1, InStr(textline, Chr(9)) - 1)
                            myMeasInfo(UBound(myMeasInfo)).MeasName = Mid(textline, InStr(textline, Chr(9)) + 1, Len(textline))

                        End If

                    Loop

                    FileClose(fnum)

                End If

            End If

        Catch
            MsgBox(Err.Number & " - " & Err.Description)
        End Try

        Me.Cursor = Cursors.Arrow

    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Private Sub ListBox3_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox3.SelectedIndexChanged

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click

        'This is the Save Item List As button...

        Dim fnum As Integer
        Dim x As Integer

        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sfile As Scripting.File

        SaveFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath & "\ExperimentDisplays"
        SaveFileDialog1.DefaultExt = ".txt"
        SaveFileDialog1.FileName = SaveFileDialog1.InitialDirectory & "\" & SaveLoginID & ".txt"
        SaveFileDialog1.Filter = "txt | *.txt"

        SaveFileDialog1.ShowDialog()

        If InStr(SaveFileDialog1.FileName, ".txt") > 0 Then

            fnum = FreeFile()
            FileOpen(fnum, SaveFileDialog1.FileName, OpenMode.Output)

            For x = 0 To ListBox2.Items.Count - 1
                PrintLine(fnum, ListBox2.Items(x).ToString)
            Next

            FileClose(fnum)

            ListBox4.Items.Clear()

            FSO = New Scripting.FileSystemObject
            f = FSO.GetFolder(My.Application.Info.DirectoryPath & "\ExperimentDisplays")

            For Each sfile In f.Files
                ListBox4.Items.Add(sfile.Name)
            Next

        End If



    End Sub

    Private Sub ListBox4_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox4.SelectedIndexChanged

        Dim fnum As Integer
        Dim textline As String

        ListBox2.Items.Clear()

        fnum = FreeFile()

        FileOpen(fnum, My.Application.Info.DirectoryPath & "\ExperimentDisplays\" & ListBox4.Items(ListBox4.SelectedIndex).ToString, OpenMode.Input)

        Do While Not EOF(fnum)
            textline = LineInput(fnum)
            ListBox2.Items.Add(textline)
        Loop

        FileClose(fnum)
    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click

        'This is the Display Items in INCA List button...

        Dim x As Integer

        myExpEnvView.CloseAllAPICalibrationViews()
        myExpEnvView.CloseAllAPIMeasureViews()

        For x = 0 To ListBox2.Items.Count - 1
            If Mid(ListBox2.Items(x).ToString, InStr(ListBox2.Items(x).ToString, Chr(9)) + 1, 1) = "K" Then
                AddCalDisplayToInca(ListBox2.Items(x).ToString)
            Else
                AddMeasDisplayToInca(ListBox2.Items(x).ToString)
            End If
        Next

        'If WhichDisplayIsInFront = "Calibrate" Then
        Me.SendToBack()
        OnVehicleScreen.Hide()
        WhichDisplayIsInFront = "INCA"
        myExpEnvView.BringToFront()

        'ElseIf WhichDisplayIsInFront = "INCA" Then
        'OnVehicleScreen.Show()
        'OnVehicleScreen.Refresh()
        'WhichDisplayIsInFront = "CLEVIR"
        'End If
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged

        Dim x As Integer
        Dim myListbox As ListBox = Nothing
        Dim ListboxType As String = ""
        Dim LikeString As String

        If ListBox1.Visible = True Then
            myListbox = ListBox1
            ListboxType = "Calibration"
        End If
        If ListBox3.Visible = True Then
            myListbox = ListBox3
            ListboxType = "Measurement"
        End If

        myListbox.Items.Clear()

        If Len(TextBox1.Text) = 0 Then

            Select Case ListboxType

                Case "Calibration"

                    For x = 0 To UBound(myCalInfo)
                        If Len(myCalInfo(x).DeviceName) = 2 Then
                            myListbox.Items.Add(myCalInfo(x).DeviceName & " " & Chr(9) & myCalInfo(x).CalName)
                        Else
                            myListbox.Items.Add(myCalInfo(x).DeviceName & Chr(9) & myCalInfo(x).CalName)
                        End If
                    Next

                Case "Measurement"

                    For x = 0 To UBound(myMeasInfo)

                        If Mid(myMeasInfo(x).MeasName, 1, 2) <> "V_" And InStr(myMeasInfo(x).MeasName, "BrstGrp") = 0 _
                                        And InStr(myMeasInfo(x).MeasName, "e_a_SPI_RxBuffer") = 0 And InStr(myMeasInfo(x).MeasName, "e_a_SPI_TxBuffer") = 0 _
                                        And InStr(myMeasInfo(x).MeasName, "e_a_Data") = 0 And InStr(myMeasInfo(x).MeasName, "CeTSKR_e_TimeBased") = 0 _
                                        And InStr(myMeasInfo(x).MeasName, "PduInfo") = 0 And InStr(myMeasInfo(x).MeasName, "CanHardwareObjectData") = 0 Then

                            If Len(myMeasInfo(x).DeviceName) = 2 Then
                                myListbox.Items.Add(myMeasInfo(x).DeviceName & " " & Chr(9) & myMeasInfo(x).MeasName)
                            Else
                                myListbox.Items.Add(myMeasInfo(x).DeviceName & Chr(9) & myMeasInfo(x).MeasName)
                            End If

                        End If

                    Next

            End Select

        Else

            If Mid(Len(TextBox1.Text) - 1, 1) <> "*" Then
                LikeString = TextBox1.Text & "*"
            Else
                LikeString = TextBox1.Text
            End If

            Select Case ListboxType

                Case "Calibration"

                    For x = 0 To UBound(myCalInfo)

                        'If InStr(UCase(myCalInfo(x).CalName), UCase(TextBox1.Text)) > 0 Then
                        If UCase(myCalInfo(x).CalName) Like UCase(LikeString) Then
                            If Len(myCalInfo(x).DeviceName) = 2 Then
                                myListbox.Items.Add(myCalInfo(x).DeviceName & " " & Chr(9) & myCalInfo(x).CalName)
                            Else
                                myListbox.Items.Add(myCalInfo(x).DeviceName & Chr(9) & myCalInfo(x).CalName)
                            End If

                        End If

                    Next

                Case "Measurement"

                    For x = 0 To UBound(myMeasInfo)

                        'If InStr(UCase(myMeasInfo(x).MeasName), UCase(TextBox1.Text)) > 0 Then
                        If UCase(myMeasInfo(x).MeasName) Like UCase(LikeString) Then

                            If Mid(myMeasInfo(x).MeasName, 1, 2) <> "V_" And InStr(myMeasInfo(x).MeasName, "BrstGrp") = 0 _
                                        And InStr(myMeasInfo(x).MeasName, "e_a_SPI_RxBuffer") = 0 And InStr(myMeasInfo(x).MeasName, "e_a_SPI_TxBuffer") = 0 _
                                        And InStr(myMeasInfo(x).MeasName, "e_a_Data") = 0 And InStr(myMeasInfo(x).MeasName, "CeTSKR_e_TimeBased") = 0 _
                                        And InStr(myMeasInfo(x).MeasName, "PduInfo") = 0 And InStr(myMeasInfo(x).MeasName, "CanHardwareObjectData") = 0 Then

                                If Len(myMeasInfo(x).DeviceName) = 2 Then
                                    myListbox.Items.Add(myMeasInfo(x).DeviceName & " " & Chr(9) & myMeasInfo(x).MeasName)
                                Else
                                    myListbox.Items.Add(myMeasInfo(x).DeviceName & Chr(9) & myMeasInfo(x).MeasName)
                                End If

                            End If

                        End If

                    Next

            End Select

        End If

    End Sub
End Class