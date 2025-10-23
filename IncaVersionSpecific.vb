Imports System.IO
Imports System.Threading.Tasks

Module IncaVersionSpecific

    'This module is used to differentiate INCA 7.1 Version from INCA 7.2 Version. If there are any version specific code differences that would not allow the
    'use of the same code interchangeably between versions, they would be contained in this module.  There is a specific IncaVersionSpecific.vb file for each
    'version.  All other modules and forms are used by both versions and are found in the CommonFiles folder in W:\CLEVIR VS 2017 NET 4.6.1.

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

        CheckRecordingFileNameFormat = True

        Dim directoryInfo As New DirectoryInfo(FinalPathToSaveData)

        For Each fileInfo As FileInfo In directoryInfo.GetFiles()
            If _
                (fileInfo.Name.Contains("-") And fileInfo.Name.EndsWith(".mf4")) And
                (fileInfo.Name.IndexOf("-", StringComparison.Ordinal) < fileInfo.Name.IndexOf(".mf4", StringComparison.Ordinal)) Then
                CheckRecordingFileNameFormat = False

                If displayMsg = True Then
                    MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.SetRecordingFileAutoincrementFlag(True)
                    MyIncaInterface.MyGmIncaComm.myIncaOnlineExperiment.DisableRecordingFileDateTimeSuffix()
                    MyIncaInterface.SaveExperiment()

                    InSession = False
                    StatusNotifier.Info("Incorrect file naming detected. Experiment configuration has been fixed. Please Start Recording when ready...", "INCA")
                End If

                Exit Function
            End If
        Next

    End Function

End Module
