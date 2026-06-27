
Imports System.Speech.Recognition
Imports System.Speech.Synthesis

Public Class VoiceRecognitionClass
    Implements IDisposable

    Private WithEvents myRecognizer As SpeechRecognitionEngine
    Private _exitPending As Boolean
    Private _voiceActivated As Boolean
    Private ReadOnly synth As SpeechSynthesizer

    Public Property VoiceActivated() As Boolean
        Get
            Return _voiceActivated
        End Get
        Set(ByVal value As Boolean)
            _voiceActivated = value
        End Set
    End Property

    Public Property VoiceWasActivated As Boolean
    ''' <summary>
    ''' Alias for ActivateVoiceRecognition() - more intuitive naming
    ''' </summary>
    Public Sub StartListening()
        ActivateVoiceRecognition()
    End Sub

    ''' <summary>
    ''' Alias for DeactivateVoiceRecognition() - more intuitive naming
    ''' </summary>
    Public Sub StopListening()
        DeactivateVoiceRecognition()
    End Sub

    ''' <summary>
    ''' Returns current activation state
    ''' </summary>
    Public ReadOnly Property IsListening As Boolean
        Get
            Return VoiceActivated
        End Get
    End Property

    Public Sub New()
        ' Initialize SpeechSynthesizer
        synth = New SpeechSynthesizer()
        synth.SelectVoice("Microsoft Zira Desktop")
        synth.Rate = 0
    End Sub

    Public Sub DeactivateVoiceRecognition()
        If VoiceActivated AndAlso myRecognizer IsNot Nothing Then
            Try
                ' Store the current state before deactivating
                VoiceWasActivated = VoiceActivated
                _exitPending = False
                myRecognizer.RecognizeAsyncCancel()
                VoiceActivated = False
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", "Error deactivating voice recognition: " & ex.Message)
            End Try
        End If
    End Sub

    Public Sub ActivateVoiceRecognition()
        If Not VoiceActivated AndAlso myRecognizer IsNot Nothing Then
            Try
                myRecognizer.RecognizeAsync(RecognizeMode.Multiple)
                VoiceActivated = True
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", "Error activating voice recognition: " & ex.Message)
                VoiceActivated = False
            End Try
        End If
    End Sub

    ' ----------------------------------------------------------------------------------------
    ' Synchronous initialization
    ' ----------------------------------------------------------------------------------------
    Public Sub InitVoice()
        Try
            ' Ensure myRecognizer is properly initialized
            If myRecognizer IsNot Nothing Then
                myRecognizer.Dispose()
            End If

            ' Create grammar dynamically using the CreateCustomGrammar method
            Dim customGrammar As Grammar = CreateCustomGrammar()

            ' Initialize recognizer
            myRecognizer = New SpeechRecognitionEngine()
            myRecognizer.SetInputToDefaultAudioDevice()
            myRecognizer.UnloadAllGrammars()

            If customGrammar IsNot Nothing Then
                myRecognizer.LoadGrammar(customGrammar)

                ' Enable the UI element tied to voice activation
                OnVehicleScreen?.Button23?.Invoke(Sub() OnVehicleScreen.Button23.Enabled = True)

                ' Do NOT start recognition here. It will be started by ActivateVoiceRecognition
                ' when the user clicks the button.
                VoiceActivated = False

                HandleUserMessageLogging("GMRC", "Voice recognition initialized successfully.")
            Else
                HandleUserMessageLogging("GMRC", "Custom grammar is null. Disabling voice recognition.")
                OnVehicleScreen?.Button23?.Invoke(Sub() OnVehicleScreen.Button23.Enabled = False)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "Error in InitVoice: " & ex.Message)
        End Try
    End Sub

    ' ----------------------------------------------------------------------------------------
    ' Speech recognized handler
    ' ----------------------------------------------------------------------------------------
    Private Sub myRecognizer_SpeechRecognized(ByVal sender As Object, ByVal e As RecognitionEventArgs) _
        Handles myRecognizer.SpeechRecognized

        If Not _voiceActivated Then Return

        Try
            Dim commandText = e.Result.Text.Trim().ToUpperInvariant()
            HandleUserMessageLogging("GMRC", "Recognized Command: " & commandText)
            ProcessVoiceCommand(commandText)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "SpeechRecognized Error: " & ex.Message)
        End Try
    End Sub

    ' ----------------------------------------------------------------------------------------
    ' Synchronous method
    ' ----------------------------------------------------------------------------------------
    Private Sub ProcessVoiceCommand(commandText As String)
        Try
            ' Process the command
            Dim commandKey = commandText.ToLowerInvariant()
            Dim dataDictionary = DataDictionarySingleton.GetInstance()
            Dim stringCommand = dataDictionary.Commands(commandKey).CommandText

            If dataDictionary.Commands.ContainsKey(commandKey) Then
                ' Invoke the action synchronously
                dataDictionary.Commands(commandKey).Action.Invoke()

                ' Speak the recognized text synchronously
                synth.Speak(stringCommand)
            Else
                HandleUserMessageLogging("GMRC", $"Invalid Command: {commandText}")
                synth.Speak("Invalid command")
                UpdateStatusLabel($"Invalid Command: {commandText}", isError:=True)
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ProcessVoiceCommand Error: " & ex.Message)
        End Try
    End Sub

    Private Sub UpdateStatusLabel(text As String, Optional isError As Boolean = False)
        If OnVehicleScreen?.Label6 IsNot Nothing Then
            If OnVehicleScreen.Label6.InvokeRequired Then
                OnVehicleScreen.Label6.Invoke(Sub() UpdateStatusLabel(text, isError))
            Else
                With OnVehicleScreen.Label6
                    .BringToFront()
                    .BackColor = If(isError, Color.Red, Color.Green)
                    .ForeColor = Color.White
                    .Text = text
                    .Refresh()
                End With
            End If
        End If
    End Sub

    Private Sub myRecognizer_SpeechDetected(sender As Object, e As SpeechDetectedEventArgs) _
        Handles myRecognizer.SpeechDetected
        If Not _voiceActivated Then Return
        HandleUserMessageLogging("GMRC", "SpeechDetected event")
    End Sub

    Private Sub myRecognizer_SpeechRecognitionRejected(sender As Object, e As SpeechRecognitionRejectedEventArgs) _
        Handles myRecognizer.SpeechRecognitionRejected
        If Not _voiceActivated Then Return
        HandleUserMessageLogging("GMRC", $"SpeechRecognitionRejected: {e.Result.Text}")
    End Sub

    Private Sub myRecognizer_RecognizeCompleted(sender As Object, e As RecognizeCompletedEventArgs) _
        Handles myRecognizer.RecognizeCompleted

        If _exitPending Then
            ' Perform graceful shutdown
            myRecognizer.Dispose()
            synth.Dispose()
            ' Close application if needed
        End If
    End Sub

    Private Function CreateCustomGrammar() As Grammar
        ' Get the instance of DataDictionarySingleton
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        ' Extract the command texts from the Commands dictionary
        Dim voiceCommands = dataDictionary.Commands.Keys.ToArray()

        If voiceCommands.Length = 0 Then
            HandleUserMessageLogging("GMRC", "No voice commands available. Voice recognition will be disabled.")
            Return Nothing ' Or handle accordingly
        End If

        Dim grammarBuilder As New GrammarBuilder()
        grammarBuilder.Culture = New Globalization.CultureInfo("en-US")
        grammarBuilder.Append(New Choices(voiceCommands))
        Return New Grammar(grammarBuilder)

    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            myRecognizer?.Dispose()
            synth?.Dispose()
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "Dispose Error: " & ex.Message)
        End Try
    End Sub

End Class


