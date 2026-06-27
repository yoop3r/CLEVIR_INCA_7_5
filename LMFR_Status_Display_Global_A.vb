Option Strict Off
Option Explicit On


Public Class LmfrStatusDisplayGlobalA

    'This is the LMFR_Status_Display custom screen...

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        HandleAnnotationButtons(Button1, "System")
    End Sub

    Private Sub LmfrStatusDisplayGlobalA_Activated(sender As Object, e As EventArgs) Handles Me.Activated

        If MyIncaInterface.Recording = True Then
            Button1.Enabled = True
        Else
            Button1.Enabled = False
        End If

    End Sub

    Private Sub LmfrStatusDisplayGlobalA_Load(sender As Object, e As EventArgs) Handles Me.Load

        Me.Top = 0
        Me.Left = 0

    End Sub

    Private Sub LmfrStatusDisplayGlobalA_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        oscilloscope.Close()
    End Sub

    Private Sub LmfrStatusDisplayGlobalA_Shown(sender As Object, e As EventArgs) Handles Me.Shown

        oscilloscope.Show()
        oscilloscope.BringToFront()
        oscilloscope.Activate()

    End Sub

    Private Sub LmfrStatusDisplayGlobalA_BackColorChanged(sender As Object, e As EventArgs) Handles Me.BackColorChanged

    End Sub
End Class
