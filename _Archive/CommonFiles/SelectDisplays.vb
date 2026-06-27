Public Class SelectDisplays

    'This Form is accessed from the DISPLAYS button on the OnVehicleScreen display.  During initialization,
    'buttons are dynamically added to this form for each display window such that the user can switch
    'to this screen and access any of the displays with the click of a button.

    Public MyDisplaySelectButtons() As Button

    Public Shared Sub myDisplaySelectButtons_Click(ByVal sender As Button, ByVal e As System.EventArgs)

        'This sub handles the click on the buttons located on the SelectDisplays screen which is
        'accessed from the OnVehicleScreen.

        'The myDisplaySelectButtons object is dynamically created (as is the click handler) in the
        'CreateMenus routine

        Dim j As Integer

        'look through all forms and find the form with the name corresponding to the sender (which form was selected)
        If Not GmResidentClient.MyDFs(0) Is Nothing Then

            For j = 0 To GmResidentClient.MyDFs.Count - 1
                If GmResidentClient.MyDFs(j).Text = sender.Text Then
                    GmResidentClient.RegisterDisplaySignals(j)
                    If GmResidentClient.MyDFs(j).Visible = False Then
                        GmResidentClient.MyDFs(j).Left = SelectDisplays.Left
                        GmResidentClient.MyDFs(j).Top = SelectDisplays.Top ' + 45
                    End If
                    GmResidentClient.MyDFs(j).Show()
                    GmResidentClient.MyDFs(j).BringToFront()
                    GmResidentClient.MyDFs(j).ShowInTaskbar = True
                    Exit Sub
                End If 'myFormData(j).myForm.Text <> sender.ToString
            Next j
        End If

        '**********************************************

        'CUSTOM SCREENS and "Special" Screens

        GmResidentClient.ShowCustomScreen(sender.Text)

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'This is the Exit Button on the SelectDisplays display.

        Me.Hide()
    End Sub

    Private Sub SelectDisplays_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Check if the user is attempting to close the form (e.g., clicking the "X" button)
        If e.CloseReason = CloseReason.UserClosing Then
            ' Cancel the close operation and hide the form instead
            e.Cancel = True
            Me.Hide()
        End If
    End Sub

    Private Sub SelectDisplays_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class