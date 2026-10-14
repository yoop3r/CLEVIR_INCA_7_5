Option Strict On

Public Module InitializationRecovery
    Public Function ReportInitializationFailure(message As String, Optional reporter As Action(Of String) = Nothing) As Boolean
        HandleUserMessageLogging("GMRC", message)
        If reporter IsNot Nothing Then
            reporter(message)
        Else
            Using dialog As New Cusmsgbox With {.StartPosition = FormStartPosition.CenterScreen}
                dialog.DisplayCusMsgBox(Nothing, message, "CLEVIR Configuration Error", "OK")
            End Using
        End If
        Return False
    End Function

    Public Function RunInitializationStage(stageName As String,
                                          operation As Func(Of Action(Of String), Boolean),
                                          Optional allowConfigEditing As Boolean = True,
                                          Optional owner As Form = Nothing) As Boolean
        Do
            Dim failure As String = Nothing
            Try
                If operation(Sub(message) failure = message) Then Return True
            Catch ex As Exception
                failure = $"{stageName}: {ex.Message}"
                HandleUserMessageLogging("GMRC", failure)
            End Try
            If StartupAbortRequested Then Return False
            If String.IsNullOrWhiteSpace(failure) Then
                failure = $"{stageName} failed. See GM_ResidentClient.log for details."
                HandleUserMessageLogging("GMRC", failure)
            End If

            Do
                Dim choice As Integer
                Using dialog As New Cusmsgbox With {.StartPosition = FormStartPosition.CenterScreen}
                    dialog.Height += 90
                    dialog.Label1.Height += 90
                    For Each button As Button In {dialog.Button1, dialog.Button2, dialog.Button3, dialog.Button4}
                        button.Top += 90
                    Next
                    choice = dialog.DisplayCusMsgBox(owner, failure, "CLEVIR Initialization - " & stageName,
                        If(allowConfigEditing, "Edit Configuration", String.Empty), "Retry", "Exit")
                End Using
                Select Case choice
                    Case 1
                        If Not allowConfigEditing Then Continue Do
                        Try
                            Using editor As New ConfigurationEditorForm With {.TopMost = True}
                                If editor.ShowDialog(owner) = DialogResult.OK Then Exit Do
                            End Using
                        Catch ex As Exception
                            failure = $"Configuration Editor: {ex.Message}"
                            HandleUserMessageLogging("GMRC", failure)
                        End Try
                        ' An unsaved/cancelled edit returns to the same failure, not initialization.
                    Case 2
                        Exit Do
                    Case Else
                        HandleUserMessageLogging("GMRC", stageName & ": user requested exit/cancel.")
                        Return False
                End Select
            Loop
        Loop
    End Function

    Public Function ValidateIncaStartup(reporter As Action(Of String)) As Boolean
        If Not ReadConfigFile(reporter) Then Return False
        If ActiveProcessorNames Is Nothing OrElse ActiveProcessorNames.Length = 0 Then
            Return ReportInitializationFailure("Select a vehicle with valid config.xml <Processors> before starting INCA.", reporter)
        End If
        If String.Equals(VehicleNumber, "6LDN4666", StringComparison.OrdinalIgnoreCase) AndAlso Not ClevirAdministrator Then
            Return ReportInitializationFailure("Vehicle 6LDN4666 requires CLEVIR Administrator privileges. Select a different vehicle.", reporter)
        End If
        If Not System.IO.File.Exists(ConfiguredDataDictionaryFile) Then
            Return ReportInitializationFailure($"config.xml <DataDictionary> file not found: {ConfiguredDataDictionaryFile}", reporter)
        End If
        If String.Equals(AlternateRecordConfig, "VehicleSpy", StringComparison.OrdinalIgnoreCase) AndAlso
            Not System.IO.File.Exists(VSpySelectedConfigFileName) Then
            Return ReportInitializationFailure($"config.xml <VehicleSpySetup> file not found: {VSpySelectedConfigFileName}", reporter)
        End If
        Dim result As String = MyIncaInterface.ConnectToInca()
        If result <> "True" Then
            Return ReportInitializationFailure("INCA connection failed: " & result, reporter)
        End If
        Return VerifyConfigFiles("Initialization recovery", reporter)
    End Function
End Module
