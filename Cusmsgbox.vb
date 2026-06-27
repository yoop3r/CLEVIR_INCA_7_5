Public Class Cusmsgbox
    Private Shared selectedOption As Integer

    Public Function DisplayCusMsgBox(
                                     ByVal owner As IWin32Window,
                                     ByVal message As String,
                                     ByVal title As String,
                                     ByVal btn1 As String,
                                     Optional ByVal btn2 As String = "",
                                     Optional ByVal btn3 As String = "",
                                     Optional ByVal btn4 As String = ""
                                     ) As Integer

        ' Set the message and title
        Label1.Text = message
        Text = title

        ' Configure buttons
        Button1.Text = btn1
        Button2.Text = btn2
        Button3.Text = btn3
        Button4.Text = btn4

        ' --- MODIFIED CODE ---
        ' Set button visibility for ALL buttons, including Button1
        Button1.Visible = (btn1 <> "")
        Button2.Visible = (btn2 <> "")
        Button3.Visible = (btn3 <> "")
        Button4.Visible = (btn4 <> "")
        ' --- END MODIFIED CODE ---

        'Show the form as a modal dialog with an explicit owner
        Me.ShowDialog(owner)

        Return selectedOption
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        selectedOption = 1
        Me.Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        selectedOption = 2
        Me.Close()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        selectedOption = 3
        Me.Close()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        selectedOption = 4
        Me.Close()
    End Sub
End Class