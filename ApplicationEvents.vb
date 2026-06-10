Imports System.Security.Principal

Namespace My
    Partial Friend Class MyApplication

        ' ======================================================================
        ' Startup
        ' Guard: Npcap packet capture requires the process to run elevated.
        ' If the user launches without admin rights we show a clear message
        ' and abort before any UI loads, rather than letting it crash later
        ' inside SharpPcap with a cryptic driver error.
        ' ======================================================================
        Private Sub MyApplication_Startup(sender As Object,
                e As ApplicationServices.StartupEventArgs) _
                Handles Me.Startup

            Dim identity = WindowsIdentity.GetCurrent()
            Dim principal = New WindowsPrincipal(identity)
            If Not principal.IsInRole(WindowsBuiltInRole.Administrator) Then
                MessageBox.Show(
                    "CLEVIR must be run as Administrator." & Environment.NewLine &
                    "Right-click the shortcut and choose 'Run as administrator'.",
                    "Elevation Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                e.Cancel = True   ' Abort startup — no forms will be shown
            End If
        End Sub

        ' ======================================================================
        ' StartupNextInstance
        ' When a second instance is launched (SingleInstance = true), bring the
        ' already-running window to the foreground instead of silently ignoring
        ' the launch attempt.
        ' ======================================================================
        Private Sub MyApplication_StartupNextInstance(sender As Object,
                e As ApplicationServices.StartupNextInstanceEventArgs) _
                Handles Me.StartupNextInstance
            e.BringToForeground = True
        End Sub

        ' ======================================================================
        ' Shutdown
        ' Safety net for paths that bypass ExitApp() — Alt+F4, the window
        ' close button, or an external process kill that still allows a clean
        ' CLR shutdown (i.e. not a hard TerminateProcess).
        ' ExitApp() already calls ShutdownAllLidarDevices(); this handler covers
        ' the cases where ExitApp() was never reached so NIC handles and Hesai
        ' SDK state are always released before the process exits.
        ' ======================================================================
        Private Sub MyApplication_Shutdown(sender As Object,
                e As EventArgs) _
                Handles Me.Shutdown
            Try
                ' Only act if ExitApp did not already run the full shutdown path
                If Not exitInProgress Then
                    HandleUserMessageLogging("GMRC",
                        "ApplicationEvents.Shutdown: ExitApp was not called — running emergency LiDAR shutdown")
                    ShutdownAllLidarDevices()
                End If
            Catch ex As Exception
                ' Best-effort — do not re-throw during process teardown
                HandleUserMessageLogging("GMRC",
                    $"ApplicationEvents.Shutdown: {ex.Message}")
            End Try
        End Sub

        ' ======================================================================
        ' UnhandledException
        ' Last-resort handler for any exception that escapes all Try/Catch
        ' blocks, including background capture threads.  Logs the full exception,
        ' attempts a safe LiDAR shutdown (to release NIC handles and flush open
        ' PCAP files), then allows the default CLR behaviour (crash dialog / WER)
        ' by leaving e.ExitApplication = True so the process does not limp on
        ' in an unknown state.
        ' ======================================================================
        Private Sub MyApplication_UnhandledException(sender As Object,
                e As ApplicationServices.UnhandledExceptionEventArgs) _
                Handles Me.UnhandledException

            Dim msg As String =
                $"Unhandled exception: {e.Exception.GetType().Name}: {e.Exception.Message}" &
                Environment.NewLine & e.Exception.StackTrace

            ' Log first — before any further code that could throw
            Try
                HandleUserMessageLogging("GMRC", $"FATAL: {msg}")
            Catch
                ' If logging itself fails, write a bare fallback to the event log
                Try
                    System.Diagnostics.EventLog.WriteEntry(
                        "CLEVIR", msg, System.Diagnostics.EventLogEntryType.Error)
                Catch
                End Try
            End Try

            ' Attempt to release NIC handles and flush PCAP files
            Try
                ShutdownAllLidarDevices()
            Catch ex As Exception
                HandleUserMessageLogging("GMRC",
                    $"ApplicationEvents.UnhandledException: LiDAR shutdown error — {ex.Message}")
            End Try

            ' Show a user-friendly message before the crash dialog appears
            Try
                MessageBox.Show(
                    "CLEVIR encountered an unexpected error and must close." &
                    Environment.NewLine & Environment.NewLine &
                    e.Exception.Message,
                    "Unexpected Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)
            Catch
            End Try

            ' e.ExitApplication defaults to True — let the process terminate cleanly
        End Sub

    End Class
End Namespace
