Public Class CopilotStatusDisplay

    'This is the CopilotStatusDisplay Custom Screen

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
    End Sub

    Private Sub CopilotStatusDisplay_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Top = 0
        Me.Left = 0
    End Sub
End Class