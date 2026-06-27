Public Class PedestrianStatusDisplay

    'This is a custom screen developed for Pedestrian work...

    Private Sub PositionTopDown()

        GmResidentClient.MyTdGraphicsContainer.Height = Me.Height - 50
        GmResidentClient.MyTdGraphicsContainer.Width = Me.Width - (Me.Label1.Left + Me.Label1.Width) - 20

        GmResidentClient.MyTdGraphicsContainer.Left = (Me.Left + Me.Width) - GmResidentClient.MyTdGraphicsContainer.Width - 10

        GmResidentClient.MyTdGraphicsContainer.Top = Me.Top + 30

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        ListBox1.Items.Clear()
    End Sub

    Private Sub PedestrianStatusDisplay_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated

        If GmResidentClient.MyTdGraphicsContainer.TopMost = False Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = True
            GmResidentClient.MyTdGraphicsContainer.Show()
        End If

        PositionTopDown()

    End Sub

    Private Sub PedestrianStatusDisplay_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Click
        If GmResidentClient.MyTdGraphicsContainer.TopMost = False Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = True
            GmResidentClient.MyTdGraphicsContainer.Show()
        End If

        PositionTopDown()

    End Sub

    Private Sub PedestrianStatusDisplay_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        GmResidentClient.MyTdGraphicsContainer.myExitButton.Visible = True

        GmResidentClient.MyTdGraphicsContainer.Hide()
        GmResidentClient.MyTdGraphicsContainer.TopMost = False
        GmResidentClient.MyTdGraphicsContainer.Hide()
    End Sub

    Private Sub PedestrianStatusDisplay_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Me.Top = 0
        Me.Left = 0

        GmResidentClient.MyTdGraphicsContainer.ControlBox = False
        GmResidentClient.MyTdGraphicsContainer.Show()
        GmResidentClient.MyTdGraphicsContainer.TopMost = True
        Me.Width = GmResidentClient.DefaultFormWidth800

        GmResidentClient.MyTdGraphicsContainer.myExitButton.Visible = False

    End Sub

    Private Sub PedestrianStatusDisplay_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp
        GmResidentClient.MyTdGraphicsContainer.BringToFront()

    End Sub

    Private Sub PedestrianStatusDisplay_Move(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Move
        PositionTopDown()

    End Sub

    Private Sub PedestrianStatusDisplay_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint

        If FormDisplayed = True Then
            GmResidentClient.MyTdGraphicsContainer.Show()
            GmResidentClient.MyTdGraphicsContainer.BringToFront()
        End If

        PositionTopDown()

    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Me.Close()
    End Sub

    Private Sub PictureBox4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PictureBox4.Click
        PictureBox4.Visible = False
    End Sub
End Class