Public Class ServerTargetSelect

    'This form is no longer used....

    'This form is displayed on Initialization if the ServerConnection text in the config.txt file
    'is set to "Select".  This allows the user to select from a list of PCs to connect to.  The
    'default list can be found in the file serverconnections.txt.  The list consists of a descriptive
    'name, then a <TAB> and then a url as shown in the example below...

    'SMALL PC FROM VEHICLE	<client url="http://USMPGBN0413539:4444">

    'The idea here is to allow the user to select the server they want to connect with.  Once the server
    'selection is made, it can be saved into the config.txt file in place of the "Select" text, or the
    'user can select the server to connect to each time they run the application if they choose to keep
    'the "Select" setting in the config.txt file.

    Private Sub ServerTargetSelect_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed

    End Sub

    Private Sub ServerTargetSelect_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

    End Sub

    Private Sub ServerTargetSelect_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim x As Integer

        Button1.Enabled = False
        Button2.Enabled = False

        GM_ResidentClient.ReadServerConnectionsFile()

        ListBox1.Items.Clear()

        For x = 0 To UBound(GM_ResidentClient.ServerInfo, 2)
            ListBox1.Items.Add(GM_ResidentClient.ServerInfo(0, x))
        Next

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        'This is the Exit and Do Not Save to Config File button

        'GM_ResidentClient.ServerConnection = GM_ResidentClient.ServerInfo(1, ListBox1.SelectedIndex)
        'GM_ResidentClient.ModifyAppConfigFile(GM_ResidentClient.ServerConnection)
        Me.Close()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'This is the Exit and Save to Config File button

        'GM_ResidentClient.ServerConnection = GM_ResidentClient.ServerInfo(1, ListBox1.SelectedIndex)
        'GM_ResidentClient.SaveServerConnection = GM_ResidentClient.ServerInfo(1, ListBox1.SelectedIndex)
        'GM_ResidentClient.ModifyAppConfigFile(GM_ResidentClient.SaveServerConnection)
        Me.Close()
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged
        Button1.Enabled = True
        Button2.Enabled = True
    End Sub
End Class