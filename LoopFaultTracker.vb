Option Strict On
Option Explicit On

''' <summary>
''' What the background loop should do about a failed pass.
''' </summary>
<Flags>
Friend Enum LoopFaultActions
    None = 0
    ''' <summary>Write the detailed fault line (new signature, or the repeat interval elapsed).</summary>
    LogDetail = 1
    ''' <summary>The failure run just crossed the escalation threshold - alert the driver.</summary>
    Escalate = 2
    ''' <summary>Still escalated and the alert repeat interval elapsed - remind the driver.</summary>
    RepeatAlert = 4
End Enum

Friend Structure LoopFaultDecision
    Public Actions As LoopFaultActions
    Public ConsecutiveFailures As Integer
    Public FailingFor As TimeSpan
    ''' <summary>Failures not logged since the previous detail line (only meaningful with LogDetail).</summary>
    Public SuppressedSinceLastLog As Integer

    Public Function Has(action As LoopFaultActions) As Boolean
        Return (Actions And action) = action
    End Function
End Structure

Friend NotInheritable Class LoopFaultRecovery
    Public ReadOnly FailedPasses As Integer
    Public ReadOnly FailedFor As TimeSpan
    Public ReadOnly WasEscalated As Boolean
    ''' <summary>False when the whole failure run was suppressed from the log (intermittent repeat).</summary>
    Public ReadOnly ShouldLog As Boolean

    Public Sub New(failedPasses As Integer, failedFor As TimeSpan, wasEscalated As Boolean, shouldLog As Boolean)
        Me.FailedPasses = failedPasses
        Me.FailedFor = failedFor
        Me.WasEscalated = wasEscalated
        Me.ShouldLog = shouldLog
    End Sub
End Class

''' <summary>
''' Tracks consecutive failed passes of the GMRC background loop so a single bad pass no longer ends the loop,
''' while persistent failures are escalated to the driver and repeated failures don't flood the log.
''' Not thread-safe: used only from the background loop (UI thread).
''' </summary>
Friend NotInheritable Class LoopFaultTracker

    Private ReadOnly _escalateAfter As TimeSpan
    Private ReadOnly _alertRepeatInterval As TimeSpan
    Private ReadOnly _logRepeatInterval As TimeSpan

    Private _consecutive As Integer
    Private _runStart As DateTime
    Private _runLogged As Boolean
    Private _escalated As Boolean
    Private _lastAlertAt As DateTime

    ' Kept across failure runs so an intermittent fault (fail, succeed, fail, ...) is still rate limited.
    Private _lastLoggedSignature As String
    Private _lastLoggedAt As DateTime
    Private _suppressedSinceLog As Integer

    Public Sub New(escalateAfter As TimeSpan, alertRepeatInterval As TimeSpan, logRepeatInterval As TimeSpan)
        _escalateAfter = escalateAfter
        _alertRepeatInterval = alertRepeatInterval
        _logRepeatInterval = logRepeatInterval
    End Sub

    Public ReadOnly Property ConsecutiveFailures As Integer
        Get
            Return _consecutive
        End Get
    End Property

    Public ReadOnly Property IsEscalated As Boolean
        Get
            Return _escalated
        End Get
    End Property

    Public Function RecordFailure(signature As String, now As DateTime) As LoopFaultDecision
        If _consecutive = 0 Then
            _runStart = now
            _runLogged = False
        End If
        _consecutive += 1

        Dim decision As New LoopFaultDecision With {
            .ConsecutiveFailures = _consecutive,
            .FailingFor = now - _runStart
        }

        If _lastLoggedSignature Is Nothing OrElse
           Not String.Equals(signature, _lastLoggedSignature, StringComparison.Ordinal) OrElse
           now - _lastLoggedAt >= _logRepeatInterval Then
            decision.Actions = decision.Actions Or LoopFaultActions.LogDetail
            decision.SuppressedSinceLastLog = _suppressedSinceLog
            _suppressedSinceLog = 0
            _lastLoggedSignature = signature
            _lastLoggedAt = now
            _runLogged = True
        Else
            _suppressedSinceLog += 1
        End If

        If Not _escalated Then
            If decision.FailingFor >= _escalateAfter Then
                _escalated = True
                _lastAlertAt = now
                decision.Actions = decision.Actions Or LoopFaultActions.Escalate
            End If
        ElseIf now - _lastAlertAt >= _alertRepeatInterval Then
            _lastAlertAt = now
            decision.Actions = decision.Actions Or LoopFaultActions.RepeatAlert
        End If

        Return decision
    End Function

    ''' <summary>Returns Nothing when the previous pass also succeeded.</summary>
    Public Function RecordSuccess(now As DateTime) As LoopFaultRecovery
        If _consecutive = 0 Then Return Nothing

        Dim recovery As New LoopFaultRecovery(_consecutive, now - _runStart, _escalated, _runLogged OrElse _escalated)
        _consecutive = 0
        _escalated = False
        _runLogged = False
        Return recovery
    End Function

End Class
