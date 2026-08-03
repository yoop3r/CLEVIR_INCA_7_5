Imports System.IO

Public Class RevisionHistoryForm

    Private Sub RevisionHistoryForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Dim historyPath As String = Path.Combine(My.Application.Info.DirectoryPath, "RevisionHistory.txt")

            If File.Exists(historyPath) Then
                TextBoxHistory.Text = File.ReadAllText(historyPath)
                TextBoxHistory.SelectionStart = 0
                TextBoxHistory.ScrollToCaret()
            Else
                TextBoxHistory.Text = "Revision history is not available." & vbCrLf &
                    "Expected file: " & historyPath
                HandleUserMessageLogging("GMRC", $"RevisionHistoryForm: RevisionHistory.txt not found at {historyPath}")
            End If

        Catch ex As Exception
            TextBoxHistory.Text = $"Failed to load revision history: {ex.Message}"
            HandleUserMessageLogging("GMRC", $"RevisionHistoryForm_Load: {ex.Message}")
        End Try
    End Sub

    Private Sub ButtonClose_Click(sender As Object, e As EventArgs) Handles ButtonClose.Click
        Me.Close()
    End Sub

End Class
