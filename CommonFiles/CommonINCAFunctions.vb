Option Strict Off

Imports de.etas.cebra.toolAPI.Common
Imports de.etas.cebra.toolAPI.Inca

Module CommonIncaFunctions

    'This module contains INCA related functions that are common to all tools that use the INCA APIs. It is located in the GlobalCommonFiles folder
    'and is used by multiple applications...

    'Public CurrentIncaVersion As String

    Public Function VerifyCorrectIncaVersion(ByVal CurrentIncaVersion As String) As Boolean
        Dim requiredIncaVersion As String = ""

        If Not CurrentIncaVersion Is Nothing Then

            If InStr(My.Application.Info.AssemblyName, "INCA_7_2") > 0 And InStr(CurrentIncaVersion, "7.2") > 0 Then
                requiredIncaVersion = "7.2.x"
            ElseIf InStr(My.Application.Info.AssemblyName, "INCA_7_3") > 0 And InStr(CurrentIncaVersion, "7.3") = 0 Then
                requiredIncaVersion = "7.3.x"
            ElseIf InStr(My.Application.Info.AssemblyName, "INCA_7_4") > 0 And InStr(CurrentIncaVersion, "7.4") = 0 Then
                requiredIncaVersion = "7.4.x"
            ElseIf InStr(My.Application.Info.AssemblyName, "INCA_7_5") > 0 And InStr(CurrentIncaVersion, "7.5") = 0 Then
                requiredIncaVersion = "7.5.x"
            End If

            If Len(requiredIncaVersion) > 0 Then
                HandleUserMessageLogging("GMRC", CurrentIncaVersion & " is currently running. " & My.Application.Info.AssemblyName & " requires a " & requiredIncaVersion & " version. Closing INCA and Exiting...", DisplayMsgBox, )
                VerifyCorrectIncaVersion = False
            Else
                VerifyCorrectIncaVersion = True
            End If

        Else
            HandleUserMessageLogging("GMRC", "Unable to connect to INCA. This may be due to an incomplete shutdown of INCA from a previous session.  Please check the task manager and kill any INCA related process or applications (or shut down and restart the PC), then relaunch " & My.Application.Info.AssemblyName, DisplayMsgBox, )
            VerifyCorrectIncaVersion = False
        End If


    End Function

    Public Function CastDbItemToWorkbaseDevice(ByRef x As Object) As WorkbaseDevice

        'This recasts a generic object to an object of type WorkbaseDevice

        CastDbItemToWorkbaseDevice = x
    End Function
    Public Function CastDbItemToExpEnv(ByRef x As Object) As ExperimentEnvironment

        'This recasts a generic object to an object of type ExperimentEnvironment

        CastDbItemToExpEnv = x
    End Function

    Public Function CastItemToExpView(ByRef x As Object) As ExperimentView

        'This recasts a generic object to an object of type ExperimentView
        CastItemToExpView = x
    End Function

End Module
