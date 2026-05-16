Option Strict Off
Option Explicit On

Imports System.Speech
Imports System.Speech.Recognition
Imports System.Speech.Synthesis

Public Class LMFR_Status_Display

    'This is the LMFR_Status_Display custom screen...

    Private Sub HandleAnnotationButtons(ByVal buttontext As String)

        'The button containers allow the same code to execute whenever a button is pressed
        'that is associated with the user annotations....

        'pressing a button will write the correctly formatted string which is required for
        'a particular button press, into the data file that is being recorded in INCA...

        'Dim thisButton As Button = DirectCast(sender, Button)
        Dim x As Integer
        Dim y As Integer
        Dim z As Integer
        Dim i As Integer

        Static SaveTextString As String

        Dim synth As New SpeechSynthesizer

        Dim EventComment As String

        Dim Fnum As Integer

        Dim found As Boolean

        Static SequenceNumber As Integer
        Dim Msec As Integer

        If Len(ANNOFileName) = 0 Then
            Exit Sub
        End If

        If Not System.IO.File.Exists(ANNOFileName) Then
            CreateANNOFile()
        End If

        Msec = RecordFileElapseTime.TotalMilliseconds - GM_ResidentClient.StartRecordDelay

        SequenceNumber = Val(Mid(GM_ResidentClient.SaveRecordingFileName, InStr(GM_ResidentClient.SaveRecordingFileName, ".mf4") - 2, 2))

        Fnum = FreeFile()
        FileOpen(Fnum, ANNOFileName, OpenMode.Append)

        EventComment = ""

        'we first need to figure out if the button pressed was in the AnnotationValueRecord array

        For y = 0 To UBound(GM_ResidentClient.AnnotationValueRecord)
            If buttontext = GM_ResidentClient.AnnotationValueRecord(y).Description Then
                Exit For
            End If
        Next

        'if the button text was not found in the AnnotationValueRecord array, the value of y
        'will be > than the upper bound of this array.  This indicates that the button
        'is a "child" of a parent button which should contain a valid AnnotationValueRecord
        'description.

        'So, we will look to the parent - in these cases, the event comment is a
        'driver feedback type of comment...

        If y > UBound(GM_ResidentClient.AnnotationValueRecord) Then

            If buttontext = "Custom Annotation" Then

                If Len(SaveTextString) > 0 Then
                    CustomAnnotation.TextBox1.Text = SaveTextString

                    If CustomAnnotation.ListBox1.Items.Count > 0 Then
                        For x = 1 To CustomAnnotation.ListBox1.Items.Count
                            If CustomAnnotation.ListBox1.Items(x - 1).ToString = SaveTextString Then
                                found = True
                            End If
                        Next

                        If found = False Then
                            CustomAnnotation.ListBox1.Items.Add(SaveTextString)
                        End If
                    Else
                        CustomAnnotation.ListBox1.Items.Add(SaveTextString)
                    End If
                End If

                CustomAnnotation.ShowDialog()

                For x = 0 To UBound(GM_ResidentClient.AnnotationValueRecord)
                    'If Me.Text = GM_ResidentClient.AnnotationValueRecord(x).Description Then
                    If InStr(Me.Text, GM_ResidentClient.AnnotationValueRecord(x).Description) > 0 Then
                        Exit For
                    End If
                Next

                If Len(CustomAnnotation.TextBox1.Text) > 0 Then

                    SaveTextString = CustomAnnotation.TextBox1.Text

                    EventComment = "3" & "," & GM_ResidentClient.AnnotationValueRecord(x).TypeID & "," & "DRIVER FEEDBACK" & "," & GM_ResidentClient.AnnotationValueRecord(x).ID & "," & GM_ResidentClient.AnnotationValueRecord(x).Description & " Event" & " - " & CustomAnnotation.TextBox1.Text & "," & GM_ResidentClient.AnnotationValueRecord(x).EnumerationType & "," & i & "," & CStr(SequenceNumber) & "," & CStr(Msec) & "," & CStr(SequenceNumber) & "," & CStr(Msec) & "," & CStr(SequenceNumber) & "," & CStr(Msec) & ",0," & CStr(SequenceNumber)

                    PrintLine(Fnum, EventComment & "," & FinalPathToSaveData & "\" & GM_ResidentClient.SaveRecordingFileName & "," & Format(CurrentMileage, "0.0") & "," & Format(CurrentLatitude, "0.0000") & "," & Format(CurrentLongitude, "0.0000"))

                End If

            Else

                For x = 0 To UBound(GM_ResidentClient.AnnotationValueRecord)
                    'If Me.Text = GM_ResidentClient.AnnotationValueRecord(x).Description Then
                    If InStr(Me.Text, GM_ResidentClient.AnnotationValueRecord(x).Description) > 0 Then
                        Exit For
                    End If
                Next

                'We assume that there will be a match, otherwise this will not work.  There should
                'always be a match because all button text is driven by the data dictionary...

                'We now set the proper enumeration type id and enumeration description, and
                'enumeration value, all of which are required to build the string that will be
                'written into the INCA data file.

                For z = 0 To UBound(GM_ResidentClient.EnumerationTypeRecord)
                    If GM_ResidentClient.EnumerationTypeRecord(z).ID = GM_ResidentClient.AnnotationValueRecord(x).EnumerationType Then
                        'For i = 0 To UBound(GM_ResidentClient.EnumerationTypeRecord(z).EnumerationDesc)
                        For i = 0 To UBound(GM_ResidentClient.EnumerationTypeRecord(z).EnumerationDesc, 2)
                            'If GM_ResidentClient.EnumerationTypeRecord(z).EnumerationDesc(i) = buttontext Then
                            If GM_ResidentClient.EnumerationTypeRecord(z).EnumerationDesc(0, i) = buttontext Then
                                Exit For
                            End If
                        Next i
                        Exit For
                    End If
                Next

                '0	Anno Type ID	Anno Type	    Anno Value ID	Anno Value	Anno Enum Type	Anno Enum	Start Seq#	Start (ms)	End Seq#	End (ms)	Point Seq#	Point (ms)	Thumbnail	WAV
                '3	        1000	DRIVER FEEDBACK	         3170	 FCA Event	        3050	        1	         1	   723453	       1	  723453	         1	    723453	        0	  1


                EventComment = "3" & "," & GM_ResidentClient.AnnotationValueRecord(x).TypeID & "," & "DRIVER FEEDBACK" & "," & GM_ResidentClient.AnnotationValueRecord(x).ID & "," & GM_ResidentClient.AnnotationValueRecord(x).Description & " Event" & " - " & buttontext & "," & GM_ResidentClient.AnnotationValueRecord(x).EnumerationType & "," & i & "," & CStr(SequenceNumber) & "," & CStr(Msec) & "," & CStr(SequenceNumber) & "," & CStr(Msec) & "," & CStr(SequenceNumber) & "," & CStr(Msec) & ",0," & CStr(SequenceNumber)

                PrintLine(Fnum, EventComment & "," & FinalPathToSaveData & "\" & GM_ResidentClient.SaveRecordingFileName & "," & Format(CurrentMileage, "0.0") & "," & Format(CurrentLatitude, "0.0000") & "," & Format(CurrentLongitude, "0.0000"))

            End If

        End If

        If myINCAInterface.Recording = True And OnVehicleScreen.PictureBox1.BackColor = Color.Red Then
            StartWAVRecord()
        End If

        FileClose(Fnum)

        If Len(EventComment) > 0 Then
            myINCAInterface.WriteEventComment(EventComment, True)
        End If

        If buttontext <> "Custom Annotation" Then
            synth.Speak(buttontext)
        End If



    End Sub
    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        HandleAnnotationButtons(Button1.Text)
    End Sub

    Private Sub LMFR_Status_Display_Activated(sender As Object, e As EventArgs) Handles Me.Activated

        If myINCAInterface.Recording = True Then
            Button1.Enabled = True
        Else
            Button1.Enabled = False
        End If

    End Sub

    Private Sub LMFR_Status_Display_Load(sender As Object, e As EventArgs) Handles Me.Load

        Me.Top = 0
        Me.Left = 0

    End Sub

    Private Sub LMFR_Status_Display_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        oscilloscope.Close()
    End Sub

    Private Sub LMFR_Status_Display_Shown(sender As Object, e As EventArgs) Handles Me.Shown

        oscilloscope.Show()
        oscilloscope.BringToFront()
        oscilloscope.Activate()

    End Sub
End Class
