Option Strict Off
Option Explicit On

Public Class FusionStatusDisplay

    'This is the FusionStatusDisplay custom screen...

    Private Sub PositionTopDown()

        GmResidentClient.MyTdGraphicsContainer.Height = Me.Height - 120
        GmResidentClient.MyTdGraphicsContainer.Width = Me.Width - 470

        GmResidentClient.MyTdGraphicsContainer.Left = (Me.Left + Me.Width) - GmResidentClient.MyTdGraphicsContainer.Width - 10
        GmResidentClient.MyTdGraphicsContainer.Top = Me.Top + 30


    End Sub

    Private Sub FusionStatusDisplay_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated

        If MyIncaInterface.Recording = True Then
            Button1.Enabled = True
            Button2.Enabled = True
            Button3.Enabled = True
            Button4.Enabled = True
            Button5.Enabled = True
            Button6.Enabled = True
            Button8.Enabled = True
        Else
            Button1.Enabled = False
            Button2.Enabled = False
            Button3.Enabled = False
            Button4.Enabled = False
            Button5.Enabled = False
            Button6.Enabled = False
            Button8.Enabled = False
        End If

    End Sub

    Private Sub FusionStatusDisplay_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Click

    End Sub

    Private Sub FusionStatusDisplay_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

    End Sub

    Private Sub FusionStatusDisplay_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Me.Top = 0
        Me.Left = 0

        Me.Width = GmResidentClient.DefaultFormWidth800

    End Sub

    Private Sub FusionStatusDisplay_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp

    End Sub

    Private Sub FusionStatusDisplay_Move(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Move

    End Sub

    Private Sub FusionStatusDisplay_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        HandleAnnotationButtons(Button1, "OBJFUS")
    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        Me.Close()
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        HandleAnnotationButtons(Button2, "OBJFUS")
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        HandleAnnotationButtons(Button3, "OBJFUS")
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        HandleAnnotationButtons(Button4, "OBJFUS")
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        HandleAnnotationButtons(Button5, "OBJFUS")
    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        HandleAnnotationButtons(Button6, "OBJFUS")
    End Sub

    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click
        HandleAnnotationButtons(Button8, "OBJFUS")
    End Sub

    Private Sub Label5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label5.Click

    End Sub

    Private Sub Label66_Click(sender As Object, e As EventArgs) Handles Label66.Click

    End Sub

    Private Sub Label57_Click(sender As Object, e As EventArgs) Handles Label57.Click

    End Sub

    Private Sub Label12_Click(sender As Object, e As EventArgs) Handles Label12.Click

    End Sub
End Class