Option Strict On
Option Explicit On

Imports System.Speech.Synthesis

''' <summary>
''' The single voice for spoken driver alerts. Every alert goes through one synthesizer, so messages queue and are
''' spoken one after another with a pause between them instead of talking over each other. A message that is
''' already queued or being spoken is not queued again, so repeating alerts cannot build up a backlog.
''' Safe to call from any thread.
''' </summary>
Friend Module DriverVoice

    Private ReadOnly PauseAfterMessage As TimeSpan = TimeSpan.FromSeconds(3)
    ' Fallback so a prompt that never reports completion cannot silence its message for good.
    Private ReadOnly PendingExpiry As TimeSpan = TimeSpan.FromMinutes(2)

    Private ReadOnly _lock As New Object()
    Private ReadOnly _pending As New List(Of PendingMessage)
    Private _synth As SpeechSynthesizer

    Private NotInheritable Class PendingMessage
        Public ReadOnly Text As String
        Public ReadOnly Prompt As Prompt
        Public ReadOnly QueuedAt As DateTime

        Public Sub New(text As String, prompt As Prompt, queuedAt As DateTime)
            Me.Text = text
            Me.Prompt = prompt
            Me.QueuedAt = queuedAt
        End Sub
    End Class

    Public Sub Speak(message As String)
        If String.IsNullOrWhiteSpace(message) Then Return
        Try
            SyncLock _lock
                Dim now As DateTime = DateTime.Now
                _pending.RemoveAll(Function(p) p.Prompt.IsCompleted OrElse now - p.QueuedAt > PendingExpiry)
                If _pending.Exists(Function(p) String.Equals(p.Text, message, StringComparison.OrdinalIgnoreCase)) Then Return

                If _synth Is Nothing Then _synth = CreateSynthesizer()
                Dim builder As New PromptBuilder()
                builder.AppendText(message)
                builder.AppendBreak(PauseAfterMessage)
                _pending.Add(New PendingMessage(message, _synth.SpeakAsync(builder), now))
            End SyncLock
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "DriverVoice.Speak: " & ex.Message)
        End Try
    End Sub

    Private Function CreateSynthesizer() As SpeechSynthesizer
        Dim synth As New SpeechSynthesizer()
        Try
            synth.SelectVoice("Microsoft Zira Desktop")
        Catch
            ' Voice not installed - keep the default voice.
        End Try
        synth.Rate = 0
        Return synth
    End Function

End Module
