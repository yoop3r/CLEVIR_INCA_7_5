Public Class SelectExperiment

    'This form is used to select the experiment that will be used for the current session.
    'The experiment is selected from a list of available experiments that are found in the
    'INCA database.

    'The list of available experiments is obtained from the INCA database during initialization.

    'The user selects the experiment from the list and then clicks the "Select" button.
    'The selected experiment is then used for the current session.

    ' Add a public property to store the selected index
    Public Property SelectedFormIndex As Integer = -1

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'This is the SELECT button on the Select Experiment form.

        ' Validate that an item is selected
        If ListBox1.SelectedIndex = -1 Then
            MessageBox.Show("Please select an experiment from the list.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return ' Keep the dialog open
        End If

        ' Store the selected index in our public property
        Me.SelectedFormIndex = ListBox1.SelectedIndex

        ' Set the dialog result to OK and the form will close automatically
        Me.DialogResult = DialogResult.OK
        Me.Close()

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        'This is the CANCEL button on the Select Experiment form.
        Me.DialogResult = DialogResult.Cancel
        Me.Close()

    End Sub

    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub
End Class