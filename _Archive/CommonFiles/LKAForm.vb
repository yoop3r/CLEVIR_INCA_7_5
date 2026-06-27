Public Class LkaForm

    'This is a custom screen developed for the LKA feature on the controller.  

    'The display controls on this screen are associated with specific inputs using special entries
    'in the INCAVariableFile excel file using information from the "AlsoAssociatedWith" column. 

    'For more information on this functionality, search the code as well as the INCAVariableFile
    'for "CS_LKA".  Any input variable or signal which has "CS_" as part of its AlsoAssociatedWith
    'information is used on a C ustom S creen.

    Public LkaActive As Boolean
    Public LkaActiveTransitionOn As Boolean
    Public LkaActiveTransitionOff As Boolean
    Public LkaInitDistRtLnEdge As Double
    Public LkaInitDistLtLnEdge As Double
    Public LkaMinDistRtLnEdge As Double
    Public LkaMinDistLtLnEdge As Double
    Public LkaMaxLkaTorqueRight As Double
    Public LkaMaxLkaTorqueLeft As Double
    Public LkaMaxDrvrTorqueRight As Double
    Public LkaMaxDrvrTorqueLeft As Double


    Private Sub LkaForm_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated

        'As part of the LKA custom screen, we are displaying the top down view.  When the LkaForm is
        'activated we handle this by showing and moving and sizing the top down view to take a position
        'within the LkaForm Custom screen.

        If GmResidentClient.MyTdGraphicsContainer.TopMost = False Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = True
            GmResidentClient.MyTdGraphicsContainer.Show()
        End If

        PositionTopDown()

    End Sub

    Private Sub LkaForm_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Click


        If GmResidentClient.MyTdGraphicsContainer.TopMost = False Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = True
            GmResidentClient.MyTdGraphicsContainer.Show()
        End If

        PositionTopDown()
    End Sub

    Private Sub LkaForm_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing


        If Not GmResidentClient.MyTdGraphicsContainer Is Nothing Then
            GmResidentClient.MyTdGraphicsContainer.myExitButton.Visible = True

            GmResidentClient.MyTdGraphicsContainer.Hide()
            GmResidentClient.MyTdGraphicsContainer.TopMost = False
            GmResidentClient.MyTdGraphicsContainer.Hide()
        End If


    End Sub

    Private Sub LkaForm_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'When the form is loaded, we set some defaults and show the top down view which is the
        'MyTdGraphicsContainer form.

        Me.Top = 0
        Me.Left = 0

        GmResidentClient.MyTdGraphicsContainer.ControlBox = False
        GmResidentClient.MyTdGraphicsContainer.Show()
        GmResidentClient.MyTdGraphicsContainer.TopMost = True
        Me.Width = GmResidentClient.DefaultFormWidth800

        GmResidentClient.MyTdGraphicsContainer.myExitButton.Visible = False

        Label1.Text = Format(LkaInitDistRtLnEdge, "0.000")
        Label3.Text = Format(LkaInitDistLtLnEdge, "0.000")
        Label6.Text = Format(LkaMinDistLtLnEdge, "0.000")
        Label8.Text = Format(LkaMinDistRtLnEdge, "0.000")
        Label14.Text = Format(LkaMaxDrvrTorqueRight, "0.000")
        Label16.Text = Format(LkaMaxDrvrTorqueLeft, "0.000")
        Label10.Text = Format(LkaMaxLkaTorqueRight, "0.000")
        Label12.Text = Format(LkaMaxLkaTorqueLeft, "0.000")


    End Sub

    Private Sub PositionTopDown()
        Try
            ' Check if MyTdGraphicsContainer is not Nothing
            If GmResidentClient.MyTdGraphicsContainer IsNot Nothing Then
                ' Ensure the form dimensions are valid before setting the properties
                If Me.Height > 50 And Me.Width > 470 Then
                    GmResidentClient.MyTdGraphicsContainer.Height = Me.Height - 50
                    GmResidentClient.MyTdGraphicsContainer.Width = Me.Width - 470

                    GmResidentClient.MyTdGraphicsContainer.Left = (Me.Left + Me.Width) - GmResidentClient.MyTdGraphicsContainer.Width - 10
                    GmResidentClient.MyTdGraphicsContainer.Top = Me.Top + 30
                Else
                    HandleUserMessageLogging("GMRC", "LkaForm dimensions are too small to position MyTdGraphicsContainer.", DisplayMsgBox)
                End If
            Else
                HandleUserMessageLogging("GMRC", "MyTdGraphicsContainer is not initialized.", DisplayMsgBox)
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"PositionTopDown: {ex.Message}", DisplayMsgBox)
        End Try
    End Sub

    Private Sub LkaForm_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp
        GmResidentClient.MyTdGraphicsContainer.BringToFront()
    End Sub

    Private Sub LkaForm_Move(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Move
        PositionTopDown()
    End Sub

    Private Sub LkaForm_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint

        If FormDisplayed = True Then
            GmResidentClient.MyTdGraphicsContainer.Show()
            GmResidentClient.MyTdGraphicsContainer.BringToFront()
        End If

        PositionTopDown()

    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Me.Close()
    End Sub
End Class