Public Class UserStatusInfo
    Private _wasTopMost As Boolean

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label1.Click
        If StatusNotifier.Confirm("Switch to CLEVIR Development Mode?", "CLEVIR") Then
            CLEVIRFlavor = "DEVELOPMENT"
            StatusNotifier.Toast("Initializing CLEVIR for Development...", "CLEVIR", durationMs:=2500, ensureMainOnTop:=False)
        End If
    End Sub

    ' With StatusNotifier handling user messaging, avoid auto-toasting on any label text change.
    ' If you still set Label1.Text elsewhere for legacy reasons and want a toast, guard it narrowly.
    Private Sub Label1_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label1.TextChanged
        UserStatusInfoText = Label1.Text
        ' Optional: uncomment only if you need a toast for this specific source and it is infrequent.
        'StatusNotifier.Toast(UserStatusInfoText, "CLEVIR", durationMs:=2000, ensureMainOnTop:=False)
    End Sub

    Private Sub UserStatusInfo_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        ' Keep legacy z-order handling for cases where this form is explicitly shown (e.g., Cancel button flow).
        Me.TopMost = True
        If Not GmResidentClient.MyTdGraphicsContainer Is Nothing Then
            If GmResidentClient.MyTdGraphicsContainer.TopMost = True AndAlso GmResidentClient.MyTdGraphicsContainer.Visible = True Then
                GmResidentClient.MyTdGraphicsContainer.TopMost = False
                GmResidentClient.MyTdGraphicsContainer.Hide()
                _wasTopMost = True
            End If
        End If
    End Sub

    Private Sub UserStatusInfo_Deactivate(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Deactivate
        If _wasTopMost Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = True
            GmResidentClient.MyTdGraphicsContainer.Show()
            _wasTopMost = False
        End If
        Me.TopMost = False
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        ' Cancel auto-transition
        GmResidentClient.RecordTransitionDelay = -2
        Button1.Visible = False
        Me.Close()
        StatusNotifier.Toast("Auto transition canceled.", "CLEVIR", durationMs:=1000, ensureMainOnTop:=False)
    End Sub
End Class