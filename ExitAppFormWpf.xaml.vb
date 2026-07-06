Option Strict On
Option Explicit On

Imports System.Windows
Imports System.Windows.Controls

' WPF pilot replacement for ExitAppForm (WinForms). Displayed modally from
' GmResidentClient.ExitApp when the user exits the application via the Exit button.
' Allows the user to choose between several shutdown options; the caller reads
' SelectedExitOption after ShowDialog() returns - it does not rely on
' Window.DialogResult, so that WPF property is intentionally left unset here.
' (Note: kept as a regular comment, not an XML doc comment, because the XAML-generated
' partial class (ExitAppFormWpf.g.vb) already carries its own XML doc comment on this
' same type declaration - VB does not merge XML doc comments across partial-class parts,
' so having both triggers BC42314.)
Partial Public Class ExitAppFormWpf

    ' This property stores the user's choice, same contract as the original WinForms form.
    Public Property SelectedExitOption As ExitOption = ExitOption.None

    ' Button1: "Exit CLEVIR and Shutdown Windows"
    Private Sub Button1_Click(sender As Object, e As RoutedEventArgs)
        HandleUserMessageLogging("GMRC", "ExitAppForm: Selected 'Exit CLEVIR, Close INCA, and Shutdown Windows'")
        SelectedExitOption = ExitOption.ExitClevirCloseIncaShutdownWindows
        ' Set flag: this option exits the app AND shuts down Windows.
        exitInProgress = True
        Me.Close()
    End Sub

    ' Button2: "Exit CLEVIR Only"
    Private Sub Button2_Click(sender As Object, e As RoutedEventArgs)
        HandleUserMessageLogging("GMRC", "ExitAppForm: Selected 'Exit CLEVIR Only'")
        SelectedExitOption = ExitOption.ExitClevirOnly
        ' Set flag: this option exits the app (but keeps INCA running).
        exitInProgress = True
        Me.Close()
    End Sub

    ' Button3: "Exit CLEVIR And Close INCA"
    Private Sub Button3_Click(sender As Object, e As RoutedEventArgs)
        HandleUserMessageLogging("GMRC", "ExitAppForm: Selected 'Exit CLEVIR and Close INCA'")
        SelectedExitOption = ExitOption.ExitClevirAndCloseInca
        ' Set flag: this option exits the app.
        exitInProgress = True
        Me.Close()
    End Sub

    ' Button5: "Cancel Exit"
    Private Sub Button5_Click(sender As Object, e As RoutedEventArgs)
        HandleUserMessageLogging("GMRC", "ExitAppForm: Cancel Exit selected")
        SelectedExitOption = ExitOption.CancelExit
        ' Don't set exitInProgress: user cancelled, app continues running.
        Me.Close()
    End Sub

    ' Closest WPF equivalent of the original Form.Shown handler.
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        CheckBox1.Visibility = Visibility.Visible
        CheckBox1.IsChecked = AudioToTextConversion

        HandleUserMessageLogging("GMRC", "ExitAppForm Displayed")
    End Sub

    ' Update the AudioToTextConversion module-level flag when the checkbox changes.
    Private Sub CheckBox1_CheckedChanged(sender As Object, e As RoutedEventArgs)
        AudioToTextConversion = CheckBox1.IsChecked.GetValueOrDefault()
    End Sub

End Class
