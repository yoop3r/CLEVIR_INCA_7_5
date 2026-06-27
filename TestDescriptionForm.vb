Public Class TestDescriptionForm

    'This form will be displayed after the user logs in (if not logged in as demo).  It provides the capability to add some
    'comments about the drive or the type of testing to be performed.  Whatever is added in this
    'form will be written to the data record file, using the WriteEventComment  routine, at the 
    'start of the recording

    Private Sub TestDescriptionForm_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.TopMost = True
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        SessionComments = Me.TextBox1.Text
        SessionLocation = Me.ComboBox1.Text
        SessionRing = Me.ComboBox2.Text

        Me.TopMost = False
        Me.Close()

    End Sub
End Class