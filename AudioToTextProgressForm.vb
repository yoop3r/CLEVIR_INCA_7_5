Imports System.Diagnostics
Imports System.IO
Imports System.Threading.Tasks
Imports System.Xml
Imports System.Text.RegularExpressions
Imports System.Linq

Public Class AudioToTextProgressForm
    Private _conversionTask As Task
    Private _isCompleted As Boolean = False

    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return _isCompleted
        End Get
    End Property

    Public Async Function RunConversionAsync() As Task
        Try
            _conversionTask = Task.Run(Sub() AudioToText())
            Await _conversionTask
            _isCompleted = True
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "Audio conversion failed: " & ex.Message)
            _isCompleted = True
        End Try
    End Function

    Private Sub MarkConversionComplete()
        _isCompleted = True
        ' Close the form when conversion is done
        If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
            Try
                If Me.InvokeRequired Then
                    ' Use a safer approach that checks for disposal again inside the invoke
                    Me.BeginInvoke(New Action(Sub()
                                                  If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                      Me.Close()
                                                  End If
                                              End Sub))
                Else
                    Me.Close()
                End If
            Catch ex As ObjectDisposedException
                ' Form was disposed while trying to invoke - this is expected during shutdown
                ' Log it but don't throw
                Console.WriteLine($"Form was disposed during MarkConversionComplete: {ex.Message}")
            Catch ex As InvalidOperationException
                ' Handle case where control handle was destroyed
                Console.WriteLine($"Control handle was destroyed: {ex.Message}")
            End Try
        End If
    End Sub

    ' Added a Button named btnOK in the designer and set its Visible property to False.

    '---------------------------------------------------------------------------
    ' Create and show the progress dialog background task.
    '---------------------------------------------------------------------------
    Private Sub AudioToTextProgressForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' REMOVE THIS LINE to avoid double execution:
        ' Task.Run(Sub() AudioToText())

        ' The conversion will now be started by calling RunConversionAsync() from the ExitApp method
    End Sub

    '---------------------------------------------------------------------------
    ' This is the main routine that starts the conversion process.
    '---------------------------------------------------------------------------
    ' Global variables for progress tracking.
    Private totalFiles As Integer = 0      ' Total number of files to process.
    Private currentFile As Integer = 0     ' The file currently being processed.
    Private fileEstimatedSeconds As Integer = 40  ' Estimated seconds per file.
    Private currentFileSecondsElapsed As Integer = 0

    ' Collection to store all error messages from Python script
    Private errorMessages As New System.Collections.Concurrent.ConcurrentBag(Of String)
    Private allOutputLines As New System.Collections.Concurrent.ConcurrentBag(Of String)

    ' A timer to update the progress bar gradually.
    ' (Alternatively, drop a Timer control on the form and set its Interval to 1000.)
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

            ' Set environment variable to force Python to use UTF-8 encoding
            ' This fixes the Unicode character encoding issue when stdout/stderr are redirected
            process.StartInfo.EnvironmentVariables("PYTHONIOENCODING") = "utf-8"

            ' Also set the standard output/error encoding to UTF-8
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8
            process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8

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

            ' Cancel asynchronous read operations to stop any pending callbacks
            Try
                process.CancelOutputRead()
            Catch ex As InvalidOperationException
                ' Already completed or not started
            End Try

            Try
                process.CancelErrorRead()
            Catch ex As InvalidOperationException
                ' Already completed or not started
            End Try

            ' Give a brief moment for any pending callbacks to complete
            Threading.Thread.Sleep(100)

            ' Remove event handlers after canceling reads
            RemoveHandler process.OutputDataReceived, AddressOf Process_OutputDataReceived
            RemoveHandler process.ErrorDataReceived, AddressOf Process_ErrorDataReceived

            ' Store the exit code before any potential disposal
            Dim exitCode As Integer = process.ExitCode

            ' Write comprehensive diagnostics to log file
            WritePythonDiagnostics(pythonPath, arguments, workingDirectory, exitCode)

            ' Log the command that was executed for debugging
            Dim commandLine As String = $"{pythonPath} {arguments}"
            SafeHandleUserMessageLogging("GMRC", $"Python command executed: {commandLine}")
            SafeHandleUserMessageLogging("GMRC", $"Working directory: {workingDirectory}")
            SafeHandleUserMessageLogging("GMRC", $"Exit code: {exitCode}")

            ' Log all captured error messages
            If errorMessages.Count > 0 Then
                SafeHandleUserMessageLogging("GMRC", "=== Python Script Error Output ===")
                For Each errMsg In errorMessages
                    SafeHandleUserMessageLogging("GMRC", errMsg)
                Next
                SafeHandleUserMessageLogging("GMRC", "=== End Error Output ===")
            End If

            ' Log a sample of output messages for debugging
            If allOutputLines.Count > 0 Then
                SafeHandleUserMessageLogging("GMRC", $"Total output lines captured: {allOutputLines.Count}")
                Dim sampleCount As Integer = Math.Min(10, allOutputLines.Count)
                SafeHandleUserMessageLogging("GMRC", $"First {sampleCount} output lines:")
                For i As Integer = 0 To Math.Min(sampleCount - 1, allOutputLines.Count - 1)
                    SafeHandleUserMessageLogging("GMRC", allOutputLines(i))
                Next
            End If

            ' Update the UI on the UI thread based on the process outcome.
            If exitCode = 0 Then
                ' Check if form is not disposed before invoking
                If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                    Try
                        Me.Invoke(Sub()
                                      UpdateUI("Conversion completed successfully.", 100)
                                  End Sub)
                    Catch ex As ObjectDisposedException
                        ' Form was disposed while trying to invoke - this is expected during shutdown
                        Console.WriteLine($"Form was disposed during success message update: {ex.Message}")
                    Catch ex As InvalidOperationException
                        ' Handle case where control handle was destroyed
                        Console.WriteLine($"Control handle was destroyed during success message update: {ex.Message}")
                    End Try
                End If
                ' Pause briefly so the user can see the success message
                Threading.Thread.Sleep(1500) ' 1.5-second delay

                MarkConversionComplete()
            Else
                ' Check if form is not disposed before invoking
                If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                    Try
                        Me.Invoke(Sub()
                                      ' Build a detailed error message
                                      Dim errorSummary As String = "Conversion completed with errors."
                                      If errorMessages.Count > 0 Then
                                          errorSummary &= vbCrLf & "Recent errors:" & vbCrLf
                                          Dim errorCount As Integer = 0
                                          For Each errMsg In errorMessages.Take(5) ' Show first 5 errors
                                              errorSummary &= "• " & errMsg & vbCrLf
                                              errorCount += 1
                                          Next
                                          If errorMessages.Count > 5 Then
                                              errorSummary &= $"... and {errorMessages.Count - 5} more errors"
                                          End If
                                      End If

                                      UpdateUI(errorSummary, 100)
                                      ' Only in the error case, display the OK button.
                                      btnOK.Visible = True
                                  End Sub)
                    Catch ex As ObjectDisposedException
                        ' Form was disposed while trying to invoke - this is expected during shutdown
                        Console.WriteLine($"Form was disposed during error message update: {ex.Message}")
                    Catch ex As InvalidOperationException
                        ' Handle case where control handle was destroyed
                        Console.WriteLine($"Control handle was destroyed during error message update: {ex.Message}")
                    End Try
                End If

                MarkConversionComplete()
            End If

            process.Dispose()

        Catch ex As Exception
            If Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
                Try
                    Me.Invoke(Sub()
                                  UpdateUI("Error: " & ex.Message, Math.Max(0, Math.Min(100, progressBar.Value)))
                                  ' Show the OK button if an exception occurs.
                                  btnOK.Visible = True
                              End Sub)
                Catch disposedException As ObjectDisposedException
                    ' Form was disposed while trying to invoke - this is expected during shutdown
                    Console.WriteLine($"Form was disposed during exception handling: {disposedException.Message}")
                Catch invalidOpException As InvalidOperationException
                    ' Handle case where control handle was destroyed
                    Console.WriteLine($"Control handle was destroyed during exception handling: {invalidOpException.Message}")
                End Try
                SafeHandleUserMessageLogging("GMRC", $"Audio to Text Conversion ERROR: {ex.Message}")
            End If

            MarkConversionComplete()
        End Try
    End Sub

    '---------------------------------------------------------------------------
    ' Timer tick event: updates the progress bar for the currently processing file.
    '---------------------------------------------------------------------------
    Private Sub progressTimer_Tick(sender As Object, e As EventArgs) Handles progressTimer.Tick
        ' Only update progress if we have valid file counts
        If totalFiles > 0 AndAlso currentFile > 0 Then
            currentFileSecondsElapsed += 1
            Dim currentFileFraction As Double = Math.Min(1.0, currentFileSecondsElapsed / fileEstimatedSeconds)

            ' Overall progress is the sum of:
            '   (completed files) + (current file progress)
            Dim completedPortion As Double = (currentFile - 1) * (100.0 / totalFiles)
            Dim currentPortion As Double = currentFileFraction * (100.0 / totalFiles)
            Dim overallProgress As Double = completedPortion + currentPortion

            ' Ensure we don't exceed 100%
            Dim clampedProgress As Integer = Math.Min(100, CInt(Math.Round(overallProgress)))

            UpdateUI(lblStatus.Text, clampedProgress)
        End If
    End Sub

    '---------------------------------------------------------------------------
    ' Process asynchronous output.
    '---------------------------------------------------------------------------
    Private Sub Process_OutputDataReceived(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            ' Store all output lines for debugging
            allOutputLines.Add(e.Data)

            Dim message As String = CleanOutput(e.Data)
            If Not String.IsNullOrEmpty(message) Then
                ' Add disposal check before UI updates
                If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                    ' Check for key messages.
                    If message.Contains("Total Number of files for conversion") Then
                        ' Extract and store the total number of files.
                        Dim parts() As String = message.Split(":"c)
                        If parts.Length > 1 Then
                            Integer.TryParse(parts(1).Trim(), totalFiles)

                            ' If no files to process, handle this case specifically
                            If totalFiles = 0 Then
                                UpdateUI("No audio files found to convert.", 0)
                                Return
                            End If
                        End If
                        UpdateUI(message, 0)
                    ElseIf message.StartsWith("Converting") AndAlso message.Contains("Ended") Then
                        ' When a file conversion ends, parse the file number.
                        Dim regexEnded As New Regex("Converting\s+(\d+)\s+of\s+(\d+)\s+Ended", RegexOptions.IgnoreCase)
                        Dim match As Match = regexEnded.Match(message)
                        If match.Success Then
                            currentFile = Integer.Parse(match.Groups(1).Value)
                            ' Stop the timer since the file is done.
                            If progressTimer.Enabled Then progressTimer.Stop()

                            ' Update progress to the completed fraction - ensure it doesn't exceed 100
                            ' Only calculate progress if totalFiles > 0 to avoid division by zero
                            If totalFiles > 0 Then
                                Dim overallProgress As Double = (currentFile * 100.0 / totalFiles)
                                Dim clampedProgress As Integer = Math.Min(100, CInt(Math.Round(overallProgress)))
                                UpdateUI(message, clampedProgress)
                            Else
                                UpdateUI(message, 0)
                            End If
                        End If
                    ElseIf message.StartsWith("Converting") AndAlso message.Contains("Started") Then
                        ' When a file conversion starts, parse the file number and start the timer
                        Dim regexStarted As New Regex("Converting\s+(\d+)\s+of\s+(\d+)\s+Started", RegexOptions.IgnoreCase)
                        Dim match As Match = regexStarted.Match(message)
                        If match.Success Then
                            currentFile = Integer.Parse(match.Groups(1).Value)
                            currentFileSecondsElapsed = 0
                            ' Start the timer for this file
                            If Not progressTimer.Enabled Then progressTimer.Start()
                            UpdateUI(message, progressBar.Value)
                        End If
                    Else
                        ' For any other messages, simply update the UI.
                        UpdateUI(message, progressBar.Value)
                    End If
                End If
            End If
        End If
    End Sub

    '---------------------------------------------------------------------------
    ' This event handler processes asynchronous error lines from the conversion process.
    '---------------------------------------------------------------------------
    Private Sub Process_ErrorDataReceived(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            ' Store all error messages
            errorMessages.Add(e.Data)

            ' Also log to file/console for debugging
            SafeHandleUserMessageLogging("GMRC", $"Python Error: {e.Data}")

            ' Add disposal check before UI updates
            If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                If e.Data.Contains("No such file or directory") Then
                    UpdateUI("Warning: " & e.Data, progressBar.Value)
                Else
                    UpdateUI("Error: " & e.Data, progressBar.Value)
                End If
            End If
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
        ' Output lines are of the form:
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
        If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
            If Me.InvokeRequired Then
                Try
                    Me.Invoke(New Action(Of String, Integer)(AddressOf UpdateUI), newStatus, newProgress)
                Catch ex As ObjectDisposedException
                    ' Form was disposed while trying to invoke - this is expected during shutdown
                    Console.WriteLine($"Form was disposed during UpdateUI: {ex.Message}")
                Catch ex As InvalidOperationException
                    ' Handle case where control handle was destroyed
                    Console.WriteLine($"Control handle was destroyed during UpdateUI: {ex.Message}")
                End Try
            Else
                Try
                    lblStatus.Text = newStatus

                    ' Ensure progress value is within valid range (0-100)
                    Dim clampedProgress As Integer = Math.Max(0, Math.Min(100, newProgress))
                    progressBar.Value = clampedProgress

                    ' Log if we had to clamp the value for debugging
                    If newProgress <> clampedProgress Then
                        SafeHandleUserMessageLogging("GMRC", $"Progress value clamped from {newProgress} to {clampedProgress}")
                    End If
                Catch ex As ObjectDisposedException
                    ' Form was disposed while trying to update UI - this is expected during shutdown
                    Console.WriteLine($"Form was disposed during direct UI update: {ex.Message}")
                Catch ex As InvalidOperationException
                    ' Handle case where control handle was destroyed
                    Console.WriteLine($"Control handle was destroyed during direct UI update: {ex.Message}")
                End Try
            End If
        End If
    End Sub

    '---------------------------------------------------------------------------
    ' Safe wrapper for HandleUserMessageLogging to avoid issues if it doesn't exist
    '---------------------------------------------------------------------------
    Private Sub SafeHandleUserMessageLogging(category As String, message As String)
        Try
            ' Try to call the logging function if it exists
            HandleUserMessageLogging(category, message)
        Catch ex As Exception
            ' If HandleUserMessageLogging doesn't exist or fails, just ignore it
            ' This prevents the exception from propagating up
            Console.WriteLine($"Logging failed: {message}")
        End Try
    End Sub

    '---------------------------------------------------------------------------
    ' Write detailed diagnostics to a log file
    '---------------------------------------------------------------------------
    Private Sub WritePythonDiagnostics(pythonPath As String, arguments As String, workingDirectory As String, exitCode As Integer)
        Try
            Dim logFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "AudioToText_Debug.log")
            Using writer As New StreamWriter(logFilePath, True) ' Append mode
                writer.WriteLine($"=== Audio To Text Conversion Log - {DateTime.Now} ===")
                writer.WriteLine($"Python Path: {pythonPath}")
                writer.WriteLine($"Arguments: {arguments}")
                writer.WriteLine($"Working Directory: {workingDirectory}")
                writer.WriteLine($"Exit Code: {exitCode}")
                writer.WriteLine()

                If errorMessages.Count > 0 Then
                    writer.WriteLine($"=== Error Messages ({errorMessages.Count}) ===")
                    For Each errMsg In errorMessages
                        writer.WriteLine(errMsg)
                    Next
                    writer.WriteLine()
                Else
                    writer.WriteLine("No error messages captured.")
                    writer.WriteLine()
                End If

                If allOutputLines.Count > 0 Then
                    writer.WriteLine($"=== Standard Output ({allOutputLines.Count} lines) ===")
                    For Each line In allOutputLines
                        writer.WriteLine(line)
                    Next
                    writer.WriteLine()
                Else
                    writer.WriteLine("No output lines captured.")
                    writer.WriteLine()
                End If

                writer.WriteLine("=== End of Log Entry ===")
                writer.WriteLine()
            End Using

            ' Notify user about the log file
            SafeHandleUserMessageLogging("GMRC", $"Detailed diagnostics written to: {logFilePath}")
        Catch ex As Exception
            Console.WriteLine($"Failed to write diagnostics: {ex.Message}")
        End Try
    End Sub

    '---------------------------------------------------------------------------
    ' OK Button click event: Closes the form when clicked.
    '---------------------------------------------------------------------------
    Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        Me.Close()
    End Sub

End Class

