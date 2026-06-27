Public Class OperationHistory

    'This form displays the "Operation History".  During runtime operation, various status messages
    'are copied to the listbox on this form.  This form can be accessed only from the GmResidentClient
    'form.  It is intended primarally for debug purposes.  This form is only accessible from the config
    'menu on the GmResidentClient.  It is really no longer used...

    Private Sub OperationHistory_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        Me.Hide()
        e.Cancel = True

    End Sub
End Class