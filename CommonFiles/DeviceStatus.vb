
Public Class DeviceStatus

    'This is a display form that shows the connection status of all devices defined in the INCA
    'workspace.  

    'This form is accessible from one of the selections on the SelectDisplays screen. 

    'We used to automatically display the screen if the status of any device transitioned from connected = TRUE to
    'connected = FALSE (provided that all devices were connected prior to losing connection of a device), or when we
    'transitioned from any false to all true, we no longer do this...

    Private Sub DeviceStatus_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

    End Sub

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label1.Click

    End Sub

    Private Sub Label24_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label24.Click

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'This is the EXIT button

        Me.Close()
    End Sub
End Class