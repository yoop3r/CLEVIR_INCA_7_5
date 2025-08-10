
Imports System.IO
Imports System.Threading.Tasks
Imports System.Xml
Imports System.Text.RegularExpressions

Public Class AudioToTextProgressForm

    '---------------------------------------------------------------------------
    ' Create and show the progress dialog background task.
    '---------------------------------------------------------------------------
    Private Sub AudioToTextProgressForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        If HaveRecorded = False Then
            'InitForm.Hide()
        End If
        ' Ensure the handle is created before starting the background task
        'Dim handle = lblStatus.Handle
        Task.Run(Sub() AudioToText())
    End Sub

    '---------------------------------------------------------------------------
    ' This is the main routine that starts the conversion process.
    '---------------------------------------------------------------------------
    ' Global variables for progress tracking.
    Private totalFiles As Integer = 0      ' Total number of files to process.
    Private currentFile As Integer = 0     ' The file currently being processed.
    Private fileEstimatedSeconds As Integer = 40  ' Estimated seconds per file.
    Private currentFileSecondsElapsed As Integer = 0

    ' A timer to update the progress bar gradually.
    ' (Alternatively, you could drop a Timer control on your form and set its Interval to 1000.)
    Private WithEvents progressTimer As New Timer With {.Interval = 1000} ' 1-second interval

    '---------------------------------------------------------------------------
    ' Main routine that starts the conversion process.
    '---------------------------------------------------------------------------
    Private Sub AudioToText()
        Try
            ' Define paths and parameters.
            Dim configFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "AudioTotextConfig.xml")
            ' Ensure the file exists.
            If Not File.Exists(configFilePath) Then
                Throw New FileNotFoundException($"Configuration file '{configFilePath}' not found.")
            End If

            ' Load the configuration XML.
            Dim xmlDoc As New XmlDocument()
            xmlDoc.Load(configFilePath)

            ' Read parameters from the XML file.
            Dim pythonPath As String = xmlDoc.SelectSingleNode("//PythonPath")?.InnerText
            Dim scriptName As String = xmlDoc.SelectSingleNode("//ScriptName")?.InnerText
            Dim workingDirectory As String = xmlDoc.SelectSingleNode("//WorkingDirectory")?.InnerText
            Dim intakeDir As String = xmlDoc.SelectSingleNode("//IntakeDir")?.InnerText
            Dim configPath As String = xmlDoc.SelectSingleNode("//ConfigPath")?.InnerText
            Dim configSheetName As String = xmlDoc.SelectSingleNode("//ConfigSheetName")?.InnerText
            Dim runValue As String = xmlDoc.SelectSingleNode("//RunValue")?.InnerText

            ' Construct the command-line arguments.
            Dim arguments As String = $"{scriptName} --intake_dir={intakeDir} --config_path={configPath} --Configsheet_name={configSheetName} --RUN={runValue}"

            ' Set up the process.
            Dim process As New Process()
            process.StartInfo.FileName = pythonPath
            process.StartInfo.Arguments = arguments
            process.StartInfo.WorkingDirectory = workingDirectory
            process.StartInfo.UseShellExecute = False
            process.StartInfo.RedirectStandardOutput = True
            process.StartInfo.RedirectStandardError = True
            process.StartInfo.CreateNoWindow = True

            ' Update the UI before starting.
            UpdateUI("Audio-to-text conversion, please wait...", 0)

            ' Set up asynchronous event handlers.
            AddHandler process.OutputDataReceived, AddressOf Process_OutputDataReceived
            AddHandler process.ErrorDataReceived, AddressOf Process_ErrorDataReceived

            ' Start the process and begin asynchronous reading.
            process.Start()
            process.BeginOutputReadLine()
            process.BeginErrorReadLine()

            ' Wait for the process to complete.
            process.WaitForExit()

            ' Update the UI after process completion.
            Me.Invoke(Sub()
                          If process.ExitCode = 0 Then
                              UpdateUI("Conversion completed successfully.", 100)
                          Else
                              UpdateUI("Conversion completed with errors.", 100)
                          End If
                      End Sub)

            process.Dispose()

        Catch ex As Exception
            If Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
                Me.Invoke(Sub()
                              UpdateUI("Error: " & ex.Message, progressBar.Value)
                          End Sub)
            End If
        Finally
            If Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
                Me.Invoke(Sub()
                              Me.Close()
                          End Sub)
            End If
        End Try
    End Sub

    '---------------------------------------------------------------------------
    ' Timer tick event: updates the progress bar for the currently processing file.
    '---------------------------------------------------------------------------
    Private Sub progressTimer_Tick(sender As Object, e As EventArgs) Handles progressTimer.Tick
        If totalFiles > 0 AndAlso currentFile > 0 Then
            currentFileSecondsElapsed += 1
            Dim currentFileFraction As Double = currentFileSecondsElapsed / fileEstimatedSeconds
            If currentFileFraction > 1 Then currentFileFraction = 1

            ' Overall progress is the sum of:
            '   (completed files) + (current file progress)
            Dim completedPortion As Double = (currentFile - 1) * (100 / totalFiles)
            Dim currentPortion As Double = currentFileFraction * (100 / totalFiles)
            Dim overallProgress As Double = completedPortion + currentPortion

            UpdateUI(lblStatus.Text, CInt(overallProgress))
        End If
    End Sub

    '---------------------------------------------------------------------------
    ' Process asynchronous output.
    '---------------------------------------------------------------------------
    Private Sub Process_OutputDataReceived(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            Dim message As String = CleanOutput(e.Data)
            If Not String.IsNullOrEmpty(message) Then
                ' Check for key messages.
                If message.Contains("Total Number of files for conversion") Then
                    ' Extract and store the total number of files.
                    Dim parts() As String = message.Split(":"c)
                    If parts.Length > 1 Then
                        Integer.TryParse(parts(1).Trim(), totalFiles)
                    End If
                    UpdateUI(message, 0)
                ElseIf message.StartsWith("Converting") AndAlso message.Contains("Started") Then
                    ' When a file conversion starts, parse the file number.
                    Dim regexStarted As New Regex("Converting\s+(\d+)\s+of\s+(\d+)\s+Started", RegexOptions.IgnoreCase)
                    Dim match As Match = regexStarted.Match(message)
                    If match.Success Then
                        currentFile = Integer.Parse(match.Groups(1).Value)
                        currentFileSecondsElapsed = 0  ' Reset the elapsed time for this file.
                        ' Start the timer to update progress gradually.
                        If Not progressTimer.Enabled Then progressTimer.Start()
                    End If
                    UpdateUI(message, progressBar.Value)
                ElseIf message.StartsWith("Converting") AndAlso message.Contains("Ended") Then
                    ' When a file conversion ends, parse the file number.
                    Dim regexEnded As New Regex("Converting\s+(\d+)\s+of\s+(\d+)\s+Ended", RegexOptions.IgnoreCase)
                    Dim match As Match = regexEnded.Match(message)
                    If match.Success Then
                        currentFile = Integer.Parse(match.Groups(1).Value)
                        ' Stop the timer since the file is done.
                        If progressTimer.Enabled Then progressTimer.Stop()
                        ' Update progress to the completed fraction.
                        Dim overallProgress As Double = (currentFile * 100.0 / totalFiles)
                        UpdateUI(message, CInt(overallProgress))
                    End If
                Else
                    ' For any other messages, simply update the UI.
                    UpdateUI(message, progressBar.Value)
                End If
            End If
        End If
    End Sub

    '---------------------------------------------------------------------------
    ' This event handler processes asynchronous error lines from the conversion process.
    '---------------------------------------------------------------------------
    Private Sub Process_ErrorDataReceived(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            UpdateUI("Error: " & e.Data, progressBar.Value)
        End If
    End Sub

    '---------------------------------------------------------------------------
    ' Helper function to filter and clean raw output.
    ' It returns only the messages of interest:
    ' - "Total Number of files for conversion : 7"
    ' - "Converting X of Y Started"
    ' - "Converting X of Y Ended"
    '---------------------------------------------------------------------------
    Private Function CleanOutput(ByVal rawLine As String) As String
        ' Assume output lines are of the form:
        ' [2025-02-01 08:21:12] [INFO] >>>> Your message here...
        Dim pattern As String = "^\[.*\]\s+\[.*\]\s+>>>>\s*(.*)$"
        Dim m As Match = Regex.Match(rawLine, pattern)
        If m.Success Then
            Dim message As String = m.Groups(1).Value.Trim()

            ' Return only the messages we want.
            If message.Contains("Total Number of files for conversion") Then
                Return message
            ElseIf message.StartsWith("Converting") AndAlso message.Contains("Started") Then
                ' Optionally remove trailing dashes.
                Return message.Replace("---------", "").Trim()
            ElseIf message.StartsWith("Time Consumed for") Then
                ' Convert "Time Consumed" lines to an "Ended" message.
                Dim regexTime As New Regex("Time Consumed for\s+(\d+)\s+of\s+(\d+)")
                Dim match As Match = regexTime.Match(message)
                If match.Success Then
                    Dim currentFileNum As String = match.Groups(1).Value
                    Dim totalFileNum As String = match.Groups(2).Value
                    Return $"Converting {currentFileNum} of {totalFileNum} Ended"
                End If
            End If
        End If
        Return String.Empty
    End Function

    '---------------------------------------------------------------------------
    ' Helper method to update the UI.
    ' This method ensures thread-safe calls.
    '---------------------------------------------------------------------------

    Private Sub UpdateUI(ByVal newStatus As String, ByVal newProgress As Integer)
        If Me.InvokeRequired Then
            Me.Invoke(New Action(Of String, Integer)(AddressOf UpdateUI), newStatus, newProgress)
        Else
            lblStatus.Text = newStatus
            progressBar.Value = newProgress
        End If
    End Sub


    'Private Sub AudioToText()
    '    Try
    '        ' Define paths and parameters.
    '        Dim configFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "AudioTotextConfig.xml")
    '        ' Ensure the file exists
    '        If Not File.Exists(configFilePath) Then
    '            Throw New FileNotFoundException($"Configuration file '{configFilePath}' not found.")
    '        End If

    '        ' Load the XML document
    '        Dim xmlDoc As New XmlDocument()
    '        xmlDoc.Load(configFilePath)

    '        ' Read values from the XML file
    '        Dim pythonPath As String = xmlDoc.SelectSingleNode("//PythonPath")?.InnerText
    '        Dim scriptName As String = xmlDoc.SelectSingleNode("//ScriptName")?.InnerText
    '        Dim workingDirectory As String = xmlDoc.SelectSingleNode("//WorkingDirectory")?.InnerText
    '        Dim intakeDir As String = xmlDoc.SelectSingleNode("//IntakeDir")?.InnerText
    '        Dim configPath As String = xmlDoc.SelectSingleNode("//ConfigPath")?.InnerText
    '        Dim configSheetName As String = xmlDoc.SelectSingleNode("//ConfigSheetName")?.InnerText
    '        Dim runValue As String = xmlDoc.SelectSingleNode("//RunValue")?.InnerText

    '        ' Construct the arguments.
    '        Dim arguments As String = $"{scriptName} --intake_dir={intakeDir} --config_path={configPath} --Configsheet_name={configSheetName} --RUN={runValue}"

    '        ' Start the process.
    '        Dim process As New Process()
    '        process.StartInfo.FileName = pythonPath
    '        process.StartInfo.Arguments = arguments
    '        process.StartInfo.WorkingDirectory = workingDirectory
    '        process.StartInfo.UseShellExecute = False
    '        process.StartInfo.RedirectStandardOutput = True
    '        process.StartInfo.RedirectStandardError = True
    '        process.StartInfo.CreateNoWindow = True

    '        ' Update the UI before starting.
    '        UpdateUI("Audio-to-text conversion, please wait...", 0)

    '        ' Set up asynchronous event handlers to update UI during processing.
    '        AddHandler process.OutputDataReceived, Sub(sender, e)
    '                                                   If Not String.IsNullOrEmpty(e.Data) Then
    '                                                       ' For example, update the status and increment the progress.
    '                                                       UpdateUI("Processing: " & e.Data, Math.Min(progressBar.Value + 5, 100))
    '                                                   End If
    '                                               End Sub

    '        AddHandler process.ErrorDataReceived, Sub(sender, e)
    '                                                  If Not String.IsNullOrEmpty(e.Data) Then
    '                                                      UpdateUI("Error: " & e.Data, progressBar.Value)
    '                                                  End If
    '                                              End Sub

    '        ' Start the process and begin asynchronous reading.
    '        process.Start()
    '        process.BeginOutputReadLine()
    '        process.BeginErrorReadLine()

    '        ' Wait for the process to complete.
    '        process.WaitForExit()

    '        ' Update the UI after process completion.
    '        Me.Invoke(Sub()
    '                      If process.ExitCode = 0 Then
    '                          UpdateUI("Conversion completed successfully.", 100)
    '                      Else
    '                          UpdateUI("Conversion completed with errors.", 100)
    '                      End If
    '                  End Sub)

    '        process.Dispose()

    '    Catch ex As Exception
    '        If Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
    '            Me.Invoke(Sub()
    '                          UpdateUI("Error: " & ex.Message, progressBar.Value)
    '                      End Sub)
    '        End If

    '    Finally
    '        If Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
    '            Me.Invoke(Sub()
    '                          Me.Close()
    '                      End Sub)
    '        End If
    '    End Try
    'End Sub

    'Private Sub UpdateUI(ByVal newStatus As String, ByVal newProgress As Integer)
    '    If Me.InvokeRequired Then
    '        Me.Invoke(New Action(Of String, Integer)(AddressOf UpdateUI), newStatus, newProgress)
    '    Else
    '        lblStatus.Text = newStatus
    '        progressBar.Value = newProgress
    '    End If
    'End Sub

    'AddHandler process.OutputDataReceived, Sub(sender, e)
    'If Not String.IsNullOrEmpty(e.Data) Then
    '' Clean and filter the output
    'Dim message As String = CleanOutput(e.Data)
    'If Not String.IsNullOrEmpty(message) Then
    '        ' Update the UI (you might decide what to do with the progress bar)
    '        UpdateUI(message, progressBar.Value)
    '    End If
    'End If
    'End Sub

    '''' <summary>
    '''' Filters and cleans up the raw output line.
    '''' </summary>
    'Private Function CleanOutput(ByVal rawLine As String) As String
    '    ' Assume the raw line is like:
    '    ' [2025-02-01 08:21:12] [INFO] >>>> Some Message Here
    '    ' First, remove the timestamp and "[INFO]" (everything before ">>>>")
    '    Dim pattern As String = "^\[.*\]\s+\[.*\]\s+>>>>\s*(.*)$"
    '    Dim m As Match = Regex.Match(rawLine, pattern)
    '    If m.Success Then
    '        Dim message As String = m.Groups(1).Value.Trim()

    '        ' Look for the "Total Number of files" message.
    '        If message.Contains("Total Number of files for conversion") Then
    '            ' This is what you want to show.
    '            Return message

    '            ' Look for the conversion start message.
    '        ElseIf message.StartsWith("Converting") AndAlso message.Contains("Started") Then
    '            ' We want to ignore extra details such as the "Conversion of audio" line.
    '            If message.Contains("Conversion of audio") Then
    '                ' Skip this line.
    '                Return String.Empty
    '            Else
    '                ' Remove any extra trailing characters (like the dashes) and return.
    '                Return message.Replace("---------", "").Trim()
    '            End If

    '            ' Look for the "Time Consumed" message (which indicates the end).
    '        ElseIf message.StartsWith("Time Consumed for") Then
    '            ' We want to display this as "Converting X of Y Ended"
    '            ' Extract the file numbers from the message.
    '            Dim pattern2 As String = "Time Consumed for (\d+)\s+of\s+(\d+)"
    '            Dim m2 As Match = Regex.Match(message, pattern2)
    '            If m2.Success Then
    '                Dim currentFile As String = m2.Groups(1).Value
    '                Dim totalFiles As String = m2.Groups(2).Value
    '                Return $"Converting {currentFile} of {totalFiles} Ended"
    '            End If
    '        End If
    '    End If

    '    ' For all other lines, return an empty string (i.e. do not update the UI).
    '    Return String.Empty
    'End Function
End Class

