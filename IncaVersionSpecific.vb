Imports System.IO
Imports System.Threading.Tasks

Module IncaVersionSpecific

    'This module contains shared constants and helper functions related to INCA recording behavior, including
    'monitor/init delay timings and validation/auto-increment handling for recording file names.

    Public ReadOnly InitMonitorSleepTime As Integer = 1500
    Public ReadOnly DelayForFirstInvalidTime As Integer = 1500

    Public Async Function CheckAutoIncrementFlagAsync() As Task(Of Boolean)

        If MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.GetRecordingFileAutoincrementFlag = False Then
            Await Task.Run(Sub()
                               MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.SetRecordingFileAutoincrementFlag(True)
                               MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.DisableRecordingFileDateTimeSuffix()
                               MyIncaInterface.SaveExperiment()
                           End Sub).ConfigureAwait(False)
        End If

        Return True

    End Function

    Public Function CheckRecordingFileNameFormat(Optional ByVal displayMsg As Boolean = False) As Boolean
        ' Improved function with better performance, error handling, and proper return logic

        Try
            ' Early validation: Check if directory exists
            If String.IsNullOrEmpty(FinalPathToSaveData) Then
                HandleUserMessageLogging("GMRC", "CheckRecordingFileNameFormat: FinalPathToSaveData is null or empty")
                Return True ' Assume valid if path not set
            End If

            If Not Directory.Exists(FinalPathToSaveData) Then
                HandleUserMessageLogging("GMRC", $"CheckRecordingFileNameFormat: Directory does not exist: {FinalPathToSaveData}")
                Return True ' Assume valid if directory doesn't exist yet
            End If

            ' Use more efficient file enumeration with LINQ
            Dim mf4Files As IEnumerable(Of String) = Directory.EnumerateFiles(FinalPathToSaveData, "*.mf4", SearchOption.TopDirectoryOnly)

            ' Check if any files match the invalid pattern
            Dim hasInvalidFormat As Boolean = mf4Files.Any(Function(filePath)
                                                               Dim fileName As String = Path.GetFileName(filePath)

                                                               ' Check for invalid format: contains hyphen AND hyphen appears before .mf4 extension
                                                               If fileName.Contains("-") Then
                                                                   Dim hyphenIndex As Integer = fileName.IndexOf("-", StringComparison.Ordinal)
                                                                   Dim mf4Index As Integer = fileName.IndexOf(".mf4", StringComparison.Ordinal)

                                                                   ' Invalid if hyphen exists and appears before the extension
                                                                   Return hyphenIndex >= 0 AndAlso mf4Index >= 0 AndAlso hyphenIndex < mf4Index
                                                               End If

                                                               Return False
                                                           End Function)

            ' If invalid format detected
            If hasInvalidFormat Then
                If displayMsg Then
                    ' Attempt to fix the configuration
                    Try
                        MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.SetRecordingFileAutoincrementFlag(True)
                        MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.DisableRecordingFileDateTimeSuffix()
                        MyIncaInterface.SaveExperiment()

                        InSession = False
                        StatusNotifier.Info("Incorrect file naming detected. Experiment configuration has been fixed. Please Start Recording when ready...", "INCA")

                        HandleUserMessageLogging("GMRC", "CheckRecordingFileNameFormat: Invalid format detected and corrected")
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", $"CheckRecordingFileNameFormat: Error fixing configuration - {ex.Message}")
                        ' Still return False to indicate invalid format was found
                    End Try
                Else
                    HandleUserMessageLogging("GMRC", "CheckRecordingFileNameFormat: Invalid file naming format detected (silent check)")
                End If

                Return False ' Invalid format found
            End If

            ' No invalid format detected
            Return True

        Catch ex As UnauthorizedAccessException
            HandleUserMessageLogging("GMRC", $"CheckRecordingFileNameFormat: Access denied to directory - {ex.Message}")
            Return True ' Assume valid if we can't access the directory

        Catch ex As IOException
            HandleUserMessageLogging("GMRC", $"CheckRecordingFileNameFormat: IO error - {ex.Message}")
            Return True ' Assume valid on IO errors

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CheckRecordingFileNameFormat: Unexpected error - {ex.Message}")
            Return True ' Assume valid on unexpected errors to prevent blocking
        End Try
    End Function
End Module
