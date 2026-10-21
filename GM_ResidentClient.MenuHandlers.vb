Option Strict Off
Option Explicit On

Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Speech.Synthesis
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Forms.DataVisualization.Charting
Imports System.Xml
Imports SharpPcap

Partial Public Class GmResidentClient

    Private Sub RecordPlaybackToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)

    End Sub

    Private Sub DisplayRecordToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'This displays a message box which shows the current Record File Path and Filename.  This is
        'only accessed on the GmResidentClient screen from a drop down menu.  It is not directly
        'accessible from the OnVehicleScreen display.

        If Len(Label3.Text) > 0 Then
            MsgBox(Label3.Text & Label4.Text)
        Else
            MsgBox("No Record Path/Filename defined, please select a Login ID")
        End If

    End Sub

    Private Sub ChangeVehicleNumberToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ChangeVehicleNumberToolStripMenuItem.Click

        ' Administrator only: renames the single vehicle in config.xml (<Vehicle>/<VehicleNumber>).
        ' There is one vehicle per config.xml; its <Processors> are kept unchanged.
        ' Use the Configuration Editor (Edit Vehicle) to change processors as well.

        If Not ClevirAdministrator Then Return

        Dim tmpVehicleNumber As String

        tmpVehicleNumber = InputBox("Please enter the 8 character Vehicle ID", "INPUT VEHICLE NUMBER", VehicleNumber)

        If Len(tmpVehicleNumber) >= 8 Then
            If SaveConfiguredVehicleNumber(tmpVehicleNumber) Then
                HandleUserMessageLogging("GMRC", "Vehicle selection saved. Restart CLEVIR to apply it; the current session retains its original vehicle and processors.", DisplayMsgBox)
            End If

        Else
            If Len(tmpVehicleNumber) > 0 Then
                MsgBox("Invalid Vehicle Number entered, please enter a valid vehicle ID (8 characters for USA, 9 characters for China).")
            End If
        End If

    End Sub

    Private Sub SetDisplayRefreshRateToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles SetDisplayRefreshRateToolStripMenuItem.Click

    End Sub

    Private Sub ToolStripComboBox1_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ToolStripComboBox1.Click

    End Sub

    Private Sub ToolStripComboBox2_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ToolStripComboBox2.Click


    End Sub

    Private Sub SetDataCollectionRatemsecToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles SetDataCollectionRatemsecToolStripMenuItem.Click

    End Sub

    Private Sub DisplayUpdateRatesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)
        MsgBox("Display Refresh Rate = " & _displayRefreshRate & " Data Collection Rate = " & DataCollectionRate & " INCA Polling Rate = " & MyIncaInterface.GetINCAPollingRate)
    End Sub

    Private Sub ToolStripComboBox3_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ToolStripComboBox3.Click

    End Sub


    Private Sub ToolStripComboBox3_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ToolStripComboBox3.TextChanged

    End Sub

    Private Sub ToolStripComboBox1_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ToolStripComboBox1.TextChanged
        _displayRefreshRate = Val(ToolStripComboBox1.Text)
    End Sub

    Private Sub ToolStripComboBox2_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ToolStripComboBox2.SelectedIndexChanged

        If _initializing = False And MyIncaInterface IsNot Nothing Then

            DataCollectionRate = Val(ToolStripComboBox2.Text)

            If MyIncaInterface.MeasurementStarted = True Then
                MyIncaInterface.StartDataCollection(DataCollectionRate)
            End If

        End If

    End Sub

    Private Sub ToolStripComboBox2_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ToolStripComboBox2.TextChanged

    End Sub

    Private Sub SwitchINCAUserToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles SwitchINCAUserToolStripMenuItem.Click

    End Sub

    Private Sub AddRecordOnlySignalsToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles AddRecordOnlySignalsToolStripMenuItem.Click

        'This functionality is currently disabled...

        AddRecordOnlySignals.ShowDialog()
    End Sub

    Private Sub RestartINCAToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles RestartINCAToolStripMenuItem.Click

        If MsgBox("Are you sure you want to restart INCA?", vbYesNo) = vbYes Then
            ShutdownAndRestartInca(18000)
        End If

    End Sub

    Private Sub HdwrStatusToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)
        DeviceStatus.Show()
        DeviceStatus.BringToFront()
    End Sub

    Private Sub EditUserConfigFileToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles EditUserConfigFileToolStripMenuItem.Click
        ' ✅ DefaultConfiguration is deprecated; ConfigurationEditorForm is now the supported editor.
        Dim editor As New ConfigurationEditorForm()
        If editor.ShowDialog(Me) = DialogResult.OK Then
            If MsgBox("Configuration updated successfully. Reload settings now?", vbYesNo + vbQuestion) = vbYes Then
                If ReadConfigFile() Then HandleUserMessageLogging("GMRC", "Configuration reloaded after editing")
            End If
        End If
    End Sub

    Private Sub MenuStrip1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles MenuStrip1.Click

        If MyTdGraphicsContainer.TopMost = True Then
            MyTdGraphicsContainer.TopMost = False
            MyTdGraphicsContainer.Hide()
        End If
    End Sub

    Private Sub MenuStrip1_ItemClicked(ByVal sender As System.Object, ByVal e As ToolStripItemClickedEventArgs) Handles MenuStrip1.ItemClicked
        If MyTdGraphicsContainer.TopMost = True Then
            MyTdGraphicsContainer.TopMost = False
            MyTdGraphicsContainer.Hide()
        End If
    End Sub

    Private Sub TESTToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles TESTToolStripMenuItem.Click

    End Sub

    Private Sub Label4_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Label4.Click

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button1.Click
        Close()
    End Sub

    Private Sub ListBox1_SelectedIndexChanged_1(ByVal sender As System.Object, ByVal e As EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ToggleOperatingModeToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles ToggleOperatingModeToolStripMenuItem.Click

        'Allows the user to toggle the operating mode from DEVELOPMENT to DATALOGGING.  This change will take effect the next time CLEVIR is started.

        Dim filename As String
        Dim fnum As Integer

        Dim textstr As String

        filename = My.Application.Info.DirectoryPath & "\OperatingMode.txt"

        If ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)" Then
            ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Development)"

            If MsgBox("With USER DATA UPLOAD on Exit Enabled?", vbYesNo) = MsgBoxResult.Yes Then
                textstr = "DATALOGGINGWITHUPLOAD"
                UploadDataOnExit = True
            Else
                textstr = "DATALOGGING"
                UploadDataOnExit = False
            End If

            fnum = FreeFile()
            FileOpen(fnum, filename, OpenMode.Output)
            PrintLine(fnum, textstr)
            FileClose(fnum)

            CLEVIRFlavor = textstr

        Else

            fnum = FreeFile()
            ToggleOperatingModeToolStripMenuItem.Text = "Toggle Operating Mode (To Data Logging)"
            FileOpen(fnum, filename, OpenMode.Output)
            PrintLine(fnum, "DEVELOPMENT")
            FileClose(fnum)

            CLEVIRFlavor = "DEVELOPMENT"

        End If

    End Sub

    Private Sub MuteVoiceRecordingMessagesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MuteVoiceRecordingMessagesToolStripMenuItem.Click

        If MuteVoiceRecordingMessagesToolStripMenuItem.Text = "Mute Voice Recording Status Messages" Then
            MuteVoiceRecordingMessages = True
            MuteVoiceRecordingMessagesToolStripMenuItem.Text = "Un-Mute Voice Recording Status Messages"
        Else
            MuteVoiceRecordingMessages = False
            MuteVoiceRecordingMessagesToolStripMenuItem.Text = "Mute Voice Recording Status Messages"
        End If
    End Sub

    Private Sub GroupBox1_Enter(sender As Object, e As EventArgs) Handles GroupBox1.Enter

    End Sub

    Private Sub Label5_Click(sender As Object, e As EventArgs) Handles Label5.Click

    End Sub

End Class
