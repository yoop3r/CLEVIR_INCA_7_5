Public Class TargetStatusDisplay

    'This is a custom screen which provides the user with specific target status information.
    'This screen is referred to by the user community as the "Secret Squirrel" screen and has been
    'provided to replace a similar screen that was implemented in the CLIR system, which this
    'application is replacing.

    'The display controls on this screen are associated with specific inputs using special entries
    'in the INCAVariableFile excel file using information from the "AlsoAssociatedWith" column. 

    'For more information on this functionality, search the code as well as the INCAVariableFile
    'for "CS_ACC_GAPSETTING".  Any input variable or signal which has "CS_" as part of its AlsoAssociatedWith
    'information is used on a C ustom S creen.  "CS_ACC_GAPSETTING" is one of several custom screen
    'inputs associated with the TargetStatusDisplay screen.

    Private Sub TargetStatusDisplay_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        If GmResidentClient.MyTdGraphicsContainer.TopMost = False Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = True
            GmResidentClient.MyTdGraphicsContainer.Show()
        End If

        PositionTopDown()

    End Sub

    Private Sub TargetStatusDisplay_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Click
        If GmResidentClient.MyTdGraphicsContainer.TopMost = False Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = True
            GmResidentClient.MyTdGraphicsContainer.Show()
        End If

        PositionTopDown()

    End Sub

    Private Sub TargetStatusDisplay_Enter(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Enter
    End Sub

    Private Sub TargetStatusDisplay_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed

    End Sub

    Private Sub TargetStatusDisplay_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        GmResidentClient.MyTdGraphicsContainer.myExitButton.Visible = True

        GmResidentClient.MyTdGraphicsContainer.Hide()
        GmResidentClient.MyTdGraphicsContainer.TopMost = False
        GmResidentClient.MyTdGraphicsContainer.Hide()
    End Sub

    Private Sub TargetStatusDisplay_Leave(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Leave
    End Sub

    Private Sub TargetStatusDisplay_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Me.Top = 0
        Me.Left = 0

        GmResidentClient.MyTdGraphicsContainer.ControlBox = False
        GmResidentClient.MyTdGraphicsContainer.Show()
        GmResidentClient.MyTdGraphicsContainer.TopMost = True
        Me.Width = GmResidentClient.DefaultFormWidth800

        GmResidentClient.MyTdGraphicsContainer.myExitButton.Visible = False

    End Sub

    Private Sub TargetStatusDisplay_LocationChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LocationChanged

    End Sub

    Private Sub TargetStatusDisplay_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LostFocus

    End Sub

    Private Sub PositionTopDown()

        GmResidentClient.MyTdGraphicsContainer.Height = Me.Height - 50
        GmResidentClient.MyTdGraphicsContainer.Width = Me.Width - (Me.GroupBox2.Left + Me.GroupBox2.Width) - 20

        GmResidentClient.MyTdGraphicsContainer.Left = (Me.Left + Me.Width) - GmResidentClient.MyTdGraphicsContainer.Width - 10

        GmResidentClient.MyTdGraphicsContainer.Top = Me.Top + 30

    End Sub
    Private Sub TargetStatusDisplay_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp
        GmResidentClient.MyTdGraphicsContainer.BringToFront()
    End Sub

    Private Sub TargetStatusDisplay_Move(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Move

        PositionTopDown()

    End Sub

    Private Sub Form7_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint

        If FormDisplayed = True Then
            GmResidentClient.MyTdGraphicsContainer.Show()
            GmResidentClient.MyTdGraphicsContainer.BringToFront()
        End If

        PositionTopDown()

    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        'ListBox4.Items.Clear()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        ListBox1.Items.Clear()
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        ListBox2.Items.Clear()
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        'ListBox3.Items.Clear()
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Button5_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Me.Close()
    End Sub

    Private Sub Label2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label2.Click

    End Sub

    Private Sub ListBox3_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub
End Class