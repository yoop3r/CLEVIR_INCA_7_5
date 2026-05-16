Imports Microsoft.Win32.TaskScheduler

Module TaskScheduler

    Public Function HandleCLEVIRSynchronizationTask(ByVal EnableDisableAction As Boolean) As Boolean

        'Called when CurrentVehicleUsage changes between VALIDATION and DEVELOPMENT...

        'Enables or disables CLEVIR Synchronization task in task scheduler.
        'If Enable, user prompted to re-start the computer...

        Try

            Dim tService As New TaskService()
            Dim myTaskState As TaskState
            Dim tTask As Task = tService.GetTask("CLEVIR Synchronization")

            Dim myProcess As Process()

            If tTask Is Nothing Then

                HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: tTask is nothing - returning false.")
                Return False

            End If

            If tTask.Enabled <> EnableDisableAction Then
                HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: Changing Task to Enabled = " & EnableDisableAction & "...")

                myTaskState = tTask.State

                If EnableDisableAction = True Then
                    tTask.Enabled = True
                    HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: Message asking to re-start computer displayed...")
                    If MsgBox("Do you wish to re-start the computer now to re-enable CLEVIR Synchronization task for VALIDATION USE?", vbYesNo) = vbYes Then
                        HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: User Answered Yes...")
                        RestartWindows = True
                        GM_ResidentClient.ExitApp("Complete")
                    End If
                    HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: User Answered No...")
                Else
                    If myTaskState = TaskState.Running Then
                        HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: Stopping running task...")
                        tTask.Stop()

                        If IsProcessRunning("ROBOCOPY") = True Then

                            Try

                                myProcess = Process.GetProcessesByName("Robocopy")

                                If UBound(myProcess) = 0 Then
                                    HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: Kill windows command processor...")
                                    myProcess(0).Kill()
                                    HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: windows command processor killed.")
                                End If

                            Catch
                                HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: windows command processor already shut down.")
                            End Try

                        End If

                    End If
                    HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: Disabling task...")
                    tTask.Enabled = False
                End If

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleCLEVIRSynchronizationTask: " & ex.Message, DISPLAY_MSG_BOX)
            Return False
        End Try

    End Function

End Module
