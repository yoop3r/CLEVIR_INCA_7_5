Module IncaVersionSpecific

    'This module is used to differentiate INCA 7.1 Version from INCA 7.2 Version. If there are any version specific code differences that would not allow the
    'use of the same code interchangably between versions, they would be contained in this module.  There is a specific IncaVersionSpecific.vb file for each
    'version.  All other modules and forms are used by both versions and are found in the CommonFiles folder in W:\CLEVIR VS 2017 NET 4.6.1.


    Public InitMonitorSleepTime As Integer = 1500
    Public DelayForFirstInvalidTime As Integer = 1500

    Public Function CheckAutoIncrementFlag() As Boolean

        If myINCAInterface.myGM_INCA_Comm.myIncaOnlineExperiment.GetRecordingFileAutoincrementFlag = False Then
            myINCAInterface.myGM_INCA_Comm.myIncaOnlineExperiment.SetRecordingFileAutoincrementFlag(True)
            myINCAInterface.myGM_INCA_Comm.myIncaOnlineExperiment.DisableRecordingFileDateTimeSuffix()
            myINCAInterface.SaveExperiment()
        End If

        CheckAutoIncrementFlag = True

    End Function
    Public Function CheckRecordingFileNameFormat(Optional ByVal DisplayMsg As Boolean = False) As Boolean


        Dim FSO As Scripting.FileSystemObject
        Dim f As Scripting.Folder
        Dim sFile As Scripting.File

        CheckRecordingFileNameFormat = True

        FSO = New Scripting.FileSystemObject

        f = FSO.GetFolder(FinalPathToSaveData)

        For Each sFile In f.Files
            'MsgBox(sFile.Name)
            If (InStr(sFile.Name, "-") > 0 And InStr(sFile.Name, ".mf4") > 0) And (InStr(sFile.Name, "-") < InStr(sFile.Name, ".mf4")) Then
                CheckRecordingFileNameFormat = False

                If DisplayMsg = True Then

                    myINCAInterface.myGM_INCA_Comm.myIncaOnlineExperiment.SetRecordingFileAutoincrementFlag(True)
                    myINCAInterface.myGM_INCA_Comm.myIncaOnlineExperiment.DisableRecordingFileDateTimeSuffix()
                    myINCAInterface.SaveExperiment()

                    InSession = False
                    MsgBox("Incorrect file naming detected.  Experiment configuration has been fixed.  Please Start Recording when ready...")
                    'MsgBox("If you are using your own custom experiment, please make sure that the Auto Increment Flag is set to True and the Increment digits value is set to 2.  Also that the Use date/time in file name box is UNCHECKED in the Measurement Recorder Configuration.  Then Save the Experiment and Click OK")

                End If

                Exit Function
            End If
        Next

    End Function
End Module
