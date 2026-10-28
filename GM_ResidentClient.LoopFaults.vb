Option Strict Off
Option Explicit On

Imports System.IO

' Fault handling for a single failed pass of MyBackgroundTasks (plan §11 #22).
' A failed pass is logged
' (eyes stay on the road), a toast is shown for later review, and the GO/NOGO indicators are grayed
' so a stale GREEN is never displayed.
Partial Public Class GmResidentClient

    Private Const LoopFaultEscalateSeconds As Integer = 5
    Private Const LoopFaultVoiceRepeatSeconds As Integer = 120
    Private Const LoopFaultLogRepeatSeconds As Integer = 30
    Private Const LoopFaultBackoffMs As Integer = 250
    Private Const LoopFaultEscalatedBackoffMs As Integer = 1000

    Private Const LoopFaultVoiceAlert As String = "Monitoring Fault Alert"
    Private Const LoopFaultVoiceRestored As String = "Monitoring Restored"

    Private ReadOnly _loopFaults As New LoopFaultTracker(TimeSpan.FromSeconds(LoopFaultEscalateSeconds),
                                                         TimeSpan.FromSeconds(LoopFaultVoiceRepeatSeconds),
                                                         TimeSpan.FromSeconds(LoopFaultLogRepeatSeconds))

    ''' <summary>Called from MyBackgroundTasks after a pass threw. Returns the back-off delay before the next pass.</summary>
    Private Function HandleBackgroundPassFault(ex As Exception, whereAmI As String) As Integer
        Dim exType As String = ex.GetType().Name
        Dim signature As String = $"{whereAmI}|{exType}|{ex.TargetSite?.Name}"
        Dim decision As LoopFaultDecision = _loopFaults.RecordFailure(signature, DateTime.Now)

        If decision.Has(LoopFaultActions.LogDetail) Then
            Dim suppressed As String = If(decision.SuppressedSinceLastLog > 0, $", {decision.SuppressedSinceLastLog} similar not logged", "")
            HandleUserMessageLogging("GMRC", $"MyBackgroundTasks: pass failed at '{whereAmI}' - {exType}: {ex.Message} " &
                                             $"(consecutive={decision.ConsecutiveFailures}{suppressed}) - loop continues. At: {FirstStackFrame(ex)}")
        End If

        If decision.Has(LoopFaultActions.Escalate) Then
            HandleUserMessageLogging("GMRC", $"MyBackgroundTasks: failing for {decision.FailingFor.TotalSeconds:F1}s " &
                                             $"({decision.ConsecutiveFailures} passes) - GO/NOGO grayed, driver alerted")
            GrayGoNoGoLabelsForLoopFault()
            SpeakDriverAlert(LoopFaultVoiceAlert)
            StatusNotifier.ToastError($"Background monitoring is failing - GO/NOGO indicators are not current.{vbCrLf}{whereAmI}: {exType}",
                                      "Monitoring Fault", durationMs:=12000, ensureMainOnTop:=False)
        ElseIf decision.Has(LoopFaultActions.RepeatAlert) Then
            HandleUserMessageLogging("GMRC", $"MyBackgroundTasks: still failing after {decision.FailingFor.TotalSeconds:F0}s ({decision.ConsecutiveFailures} passes)")
            SpeakDriverAlert(LoopFaultVoiceAlert)
        End If

        Return If(_loopFaults.IsEscalated, LoopFaultEscalatedBackoffMs, LoopFaultBackoffMs)
    End Function

    ''' <summary>Called from MyBackgroundTasks after every pass that completed without an exception.</summary>
    Private Sub HandleBackgroundPassSucceeded()
        Dim recovery As LoopFaultRecovery = _loopFaults.RecordSuccess(DateTime.Now)
        If recovery Is Nothing Then Return

        If recovery.ShouldLog Then
            HandleUserMessageLogging("GMRC", $"MyBackgroundTasks: recovered after {recovery.FailedPasses} failed pass(es) over {recovery.FailedFor.TotalSeconds:F1}s")
        End If

        If recovery.WasEscalated Then
            ' The grayed GO/NOGO labels no longer match their requested color, so the next pass repaints them.
            SpeakDriverAlert(LoopFaultVoiceRestored)
            StatusNotifier.Toast("Background monitoring has recovered.", StatusNotifier.ToastKind.Success,
                                 "Monitoring Restored", durationMs:=6000, ensureMainOnTop:=False)
        End If
    End Sub

    Private Sub GrayGoNoGoLabelsForLoopFault()
        ' Direct BackColor writes on purpose: UpdateGonogoLabelColor would also clear the comm/stale-data flags
        ' that ProcessKiller relies on.
        Try
            If MyLabel Is Nothing Then Return
            For Each lbl In MyLabel
                If lbl IsNot Nothing AndAlso lbl.BackColor <> Color.Gray Then lbl.BackColor = Color.Gray
            Next
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GrayGoNoGoLabelsForLoopFault: " & ex.Message)
        End Try
    End Sub

    Private Sub SpeakDriverAlert(message As String)
        DriverVoice.Speak(message)
    End Sub

    Private Shared Function FirstStackFrame(ex As Exception) As String
        Dim trace As String = ex.StackTrace
        If String.IsNullOrEmpty(trace) Then Return "(no stack trace)"
        Dim firstLine As String = trace.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)(0).Trim()
        Return firstLine
    End Function

#If DEBUG Then
    Private Const LoopFaultInjectionFileName As String = "CLEVIR_INJECT_LOOP_FAULT"
    Private _loopFaultInjectionCheckedAt As DateTime = DateTime.MinValue
    Private _loopFaultInjectionActive As Boolean

    ''' <summary>
    ''' Debug builds only: while a file named CLEVIR_INJECT_LOOP_FAULT exists in the application folder, every
    ''' background pass throws, to bench-test the fault handling. Delete the file to recover.
    ''' </summary>
    Private Sub ThrowIfLoopFaultInjected()
        If (DateTime.Now - _loopFaultInjectionCheckedAt).TotalSeconds >= 1 Then
            _loopFaultInjectionCheckedAt = DateTime.Now
            _loopFaultInjectionActive = File.Exists(Path.Combine(My.Application.Info.DirectoryPath, LoopFaultInjectionFileName))
        End If
        If _loopFaultInjectionActive Then
            Throw New InvalidOperationException("Injected background loop fault (debug)")
        End If
    End Sub
#End If

End Class
