'This form is displayed from the GmResidentClient.ExitApp routine which is called when the user
'exits the application using the Exit button.  This form is displayed modally from within ExitApp, so
'after the user selects the exit option, ExitApp continues to run and complete the exiting process...

'This form allows the user to select from different shutdown options, Main tabExit and Shutdown windows, Exit CLEVIR only
'leaving INCA running, or Exit CLEVIR and INCA.

Public Class ExitAppForm
    ' This property will store the user's choice.
    Public Property SelectedExitOption As ExitOption = ExitOption.None

    ' Button3: "Exit CLEVIR And Close INCA"
    Private Sub Button3_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button3.Click
        ' Log the user's selection if needed.
        HandleUserMessageLogging("GMRC", "ExitAppForm: Selected 'Exit CLEVIR and Close INCA'")
        SelectedExitOption = ExitOption.ExitClevirAndCloseInca
        ' ✅ SET FLAG: This option exits the app
        exitInProgress = True
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    ' Button2: "Exit CLEVIR Only"
    Private Sub Button2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button2.Click
        HandleUserMessageLogging("GMRC", "ExitAppForm: Selected 'Exit CLEVIR Only'")
        SelectedExitOption = ExitOption.ExitClevirOnly
        ' ✅ SET FLAG: This option exits the app (but keeps INCA running)
        exitInProgress = True
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    ' Button1: "Exit CLEVIR, Close INCA, and Shutdown Windows"
    Private Sub Button1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button1.Click
        HandleUserMessageLogging("GMRC", "ExitAppForm: Selected 'Exit CLEVIR, Close INCA, and Shutdown Windows'")
        SelectedExitOption = ExitOption.ExitClevirCloseIncaShutdownWindows
        ' ✅ SET FLAG: This option exits the app AND shuts down Windows
        exitInProgress = True
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    ' Button5: "Cancel Exit"
    Private Sub Button5_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button5.Click
        HandleUserMessageLogging("GMRC", "ExitAppForm: Cancel Exit selected")
        SelectedExitOption = ExitOption.CancelExit
        ' ✅ DON'T SET FLAG: User cancelled, app continues running
        ' exitInProgress remains False
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    ' When the form is shown, you can adjust UI elements.
    Private Sub ExitAppForm_Shown(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Shown
        ' Show the checkbox if needed (or hide it based on mode).
        CheckBox1.Visible = True

        ' Set checkbox state based on configuration value
        CheckBox1.Checked = AudioToTextConversion

        HandleUserMessageLogging("GMRC", "ExitAppForm Displayed")
    End Sub

    ' Update the AudioToTextConversion property when the checkbox changes.
    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        AudioToTextConversion = CheckBox1.Checked
    End Sub

End Class

