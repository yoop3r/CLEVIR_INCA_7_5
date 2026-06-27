Option Strict Off

Imports de.etas.cebra.toolAPI.Common
Imports System.IO

Public Class Form1

    'This is the calibration access form.  It allows the user to create custom calibration displays in INCA. The button that allows
    'access to this form is located at the upper left of the main screen and is only visible when running out of the design environment
    'or if the user logs in with their own loginID.  Also, it Is typically covered by the movable INCA Status Window. 

    'This functionality
    'is not really used or commonly known about by the users.  In order for this functionality to work, all signals and calibrations
    'must be read in so the selection lists are populated, this takes a very long time.  Initially, the thought was to have files
    'created to save all available variables and calibrations, but this is not really realistic given the fact that there are so many
    'different configurations to keep track of and because the software changes so frequently...

    Public WhichDisplayIsInFront As String

    Private Function GetCalibrationElementInDevice(ByVal myCalVarName As String, ByVal myExperimentDevice As ExperimentDevice, ByRef calibrationElement As Object, ByRef calibrationValue As Object) As Boolean

        'Called from AddCalDisplayToInca - Returns True if myCalVarName is found in the device...

        GetCalibrationElementInDevice = False

        calibrationElement = MyIncaInterface.MyGmIncaComm.MyIncaOnlineExperiment.GetCalibrationElementInDevice(myCalVarName, myExperimentDevice)
        If calibrationElement Is Nothing Then
            MsgBox("Element " & myCalVarName & " not found.")
            Exit Function
        End If

        calibrationValue = calibrationElement.GetValue

        If calibrationValue Is Nothing Then
            MsgBox("Could not retrieve CalibrationValue from  " & myCalVarName)
            Exit Function
        End If

        GetCalibrationElementInDevice = True

    End Function

    Private Function GetMeasureElementInDevice(ByVal myMeasVarName As String, ByVal myExperimentDevice As ExperimentDevice, ByRef measureElement As Object, ByRef measureValue As Object) As Boolean

        'Called from AddMeasDisplayToInca - Returns True if myMeasVarName is found in the device...

        GetMeasureElementInDevice = False

        measureElement = MyIncaInterface.MyGmIncaComm.MyIncaOnlineExperiment.GetMeasureElementInDevice(myMeasVarName, myExperimentDevice)
        If measureElement Is Nothing Then
            MsgBox("Element " & myMeasVarName & " not found.")
            Exit Function
        End If

        measureValue = measureElement.GetValue

        If measureValue Is Nothing Then
            MsgBox("Could not retrieve CalibrationValue from  " & myMeasVarName)
            Exit Function
        End If

        GetMeasureElementInDevice = True

    End Function

    Private Sub AddMeasDisplayToInca(ByVal myString As String)

        'Adds a measure display window to inca based on the contents of the string passed in.  String contains
        'a measurement variable name and a device name.

        Dim measObject As Object = Nothing

        'Dim MeasureElement As Object = Nothing
        Dim myMeasureElement As MeasureElement = Nothing

        Dim measName As String
        Dim deviceName As String
        Dim myExperimentDevice As ExperimentDevice

        Dim x As Boolean
        Static cnt As Integer = 0

        measName = Trim(Mid(myString, InStr(myString, Chr(9)) + 1, Len(myString)))
        deviceName = Trim(Mid(myString, 1, InStr(myString, Chr(9)) - 1))

        myExperimentDevice = MyIncaInterface.MyGmIncaComm.MyIncaOnlineExperiment.GetDevice(deviceName)

        If myExperimentDevice IsNot Nothing Then

            If GetMeasureElementInDevice(measName, myExperimentDevice, myMeasureElement, measObject) = True Then

                'x = myMeasureElement.OpenView
                'x = myMeasureElement.OpenViewWithMaxLabels(10)

                If IsNumeric(TextBox2.Text) And Val(TextBox2.Text) >= 1 And Val(TextBox2.Text) <= 20 Then
                    x = myMeasureElement.OpenViewWithMaxLabels(Val(TextBox2.Text))
                Else
                    x = myMeasureElement.OpenViewWithMaxLabels(20)
                End If


            Else
                MsgBox("INCA Measure Element " & measName & " not found.")
            End If

        Else
            MsgBox("INCA Device " & deviceName & " not found.")
        End If
    End Sub

    Private Sub AddCalDisplayToInca(ByVal myString As String)

        'Adds a cabliration display window to inca based on the contents of the string passed in.  String contains
        'a calibration variable name and a device name.


        Dim calObject As Object = Nothing

        Dim myCalibrationElement As CalibrationElement = Nothing

        Dim calName As String
        Dim deviceName As String
        Dim myExperimentDevice As ExperimentDevice

        Dim x As Boolean
        Static cnt As Integer = 0

        calName = Trim(Mid(myString, InStr(myString, Chr(9)) + 1, Len(myString)))
        deviceName = Trim(Mid(myString, 1, InStr(myString, Chr(9)) - 1))

        myExperimentDevice = MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.GetDevice(deviceName)

        If myExperimentDevice IsNot Nothing Then

            If GetCalibrationElementInDevice(calName, myExperimentDevice, myCalibrationElement, calObject) = True Then

                x = myCalibrationElement.OpenView

            End If
        Else
            MsgBox("INCA Device " & deviceName & " not found.")
        End If
    End Sub

    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles Me.FormClosing
        OnVehicleScreen.Show()
    End Sub

    Private Sub Form1_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Called when the calibration form (Form1) is loaded

        Dim experimentDisplaysPath As String = Path.Combine(My.Application.Info.DirectoryPath, "ExperimentDisplays")

        ' Ensure the ExperimentDisplays directory exists
        If Not Directory.Exists(experimentDisplaysPath) Then
            Directory.CreateDirectory(experimentDisplaysPath)
        Else
            ' Populate ListBox4 with file names from the directory
            ListBox4.Items.Clear()
            For Each filePath In Directory.GetFiles(experimentDisplaysPath)
                ListBox4.Items.Add(Path.GetFileName(filePath))
            Next
        End If
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button1.Click

        'This is the Toggle Display button...

        If WhichDisplayIsInFront = "Calibrate" Then
            SendToBack()
            OnVehicleScreen.Hide()
            WhichDisplayIsInFront = "INCA"
        ElseIf WhichDisplayIsInFront = "INCA" Then
            OnVehicleScreen.Show()
            OnVehicleScreen.Refresh()
            WhichDisplayIsInFront = "CLEVIR"
        End If

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button2.Click

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

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button4.Click

        'This is the clear list button...

        ListBox2.Items.Clear()
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button5.Click

        'This is the Calibration Items button...

        Dim x As Integer
        Dim fnum As Integer

        Dim textline As String

        Try

            Label1.Text = "Calibration Item List"

            TextBox1.Text = ""

            ListBox1.Visible = True
            ListBox3.Visible = False

            Cursor = Cursors.WaitCursor

            Refresh()

            If ListBox1.Items.Count = 0 Then

                If myCalInfo IsNot Nothing Then

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

        Catch ex As Exception

            MsgBox("Calibration Items Button: " & ex.Message)
        End Try

        Cursor = Cursors.Arrow


    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button6.Click

        'This is the measurement items button...

        Dim x As Integer
        Dim fnum As Integer
        Dim textline As String

        Try

            Label1.Text = "Measurement Variable List"

            TextBox1.Text = ""
            ListBox3.Visible = True
            ListBox1.Visible = False

            Cursor = Cursors.WaitCursor

            Refresh()

            If ListBox3.Items.Count = 0 Then

                If myMeasInfo IsNot Nothing Then

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

        Catch ex As Exception
            MsgBox("Measure Items Button: " & ex.Message)
        End Try

        Cursor = Cursors.Arrow

    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Private Sub ListBox3_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As EventArgs) Handles ListBox3.SelectedIndexChanged

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button3.Click
        ' This is the Save Item List As button

        Dim experimentDisplaysPath As String = Path.Combine(My.Application.Info.DirectoryPath, "ExperimentDisplays")

        ' Configure SaveFileDialog
        SaveFileDialog1.InitialDirectory = experimentDisplaysPath
        SaveFileDialog1.DefaultExt = ".txt"
        SaveFileDialog1.FileName = Path.Combine(experimentDisplaysPath, $"{SaveLoginID}.txt")
        SaveFileDialog1.Filter = "Text Files (*.txt)|*.txt"

        ' Show the Save File dialog
        If SaveFileDialog1.ShowDialog() = DialogResult.OK Then
            Dim saveFilePath As String = SaveFileDialog1.FileName

            ' Ensure the file name contains ".txt"
            If saveFilePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) Then
                ' Write items from ListBox2 to the selected file
                Using writer As New StreamWriter(saveFilePath, False)
                    For Each item As String In ListBox2.Items
                        writer.WriteLine(item)
                    Next
                End Using

                ' Refresh ListBox4 to show updated file list
                ListBox4.Items.Clear()
                For Each filePath In Directory.GetFiles(experimentDisplaysPath)
                    ListBox4.Items.Add(Path.GetFileName(filePath))
                Next
            End If
        End If
    End Sub

    Private Sub ListBox4_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As EventArgs) Handles ListBox4.SelectedIndexChanged

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

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button7.Click

        'This is the Display Items in INCA List button...

        Dim x As Integer

        MyIncaInterface.MyGmIncaComm.myExpEnvView.CloseAllAPICalibrationViews()
        MyIncaInterface.MyGmIncaComm.myExpEnvView.CloseAllAPIMeasureViews()

        For x = 0 To ListBox2.Items.Count - 1
            If Mid(ListBox2.Items(x).ToString, InStr(ListBox2.Items(x).ToString, Chr(9)) + 1, 1) = "K" Then
                AddCalDisplayToInca(ListBox2.Items(x).ToString)
            Else
                AddMeasDisplayToInca(ListBox2.Items(x).ToString)
            End If
        Next

        SendToBack()
        OnVehicleScreen.Hide()
        WhichDisplayIsInFront = "INCA"
        MyIncaInterface.MyGmIncaComm.myExpEnvView.BringToFront()

    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As EventArgs) Handles TextBox1.TextChanged

        Dim x As Integer
        Dim myListbox As ListBox = Nothing
        Dim listboxType As String = ""
        Dim likeString As String

        If ListBox1.Visible = True Then
            myListbox = ListBox1
            listboxType = "Calibration"
        End If
        If ListBox3.Visible = True Then
            myListbox = ListBox3
            listboxType = "Measurement"
        End If

        myListbox.Items.Clear()

        If Len(TextBox1.Text) = 0 Then

            Select Case listboxType

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
                likeString = TextBox1.Text & "*"
            Else
                likeString = TextBox1.Text
            End If

            Select Case listboxType

                Case "Calibration"

                    For x = 0 To UBound(myCalInfo)

                        'If InStr(UCase(myCalInfo(x).CalName), UCase(TextBox1.Text)) > 0 Then
                        If UCase(myCalInfo(x).CalName) Like UCase(likeString) Then
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
                        If UCase(myMeasInfo(x).MeasName) Like UCase(likeString) Then

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