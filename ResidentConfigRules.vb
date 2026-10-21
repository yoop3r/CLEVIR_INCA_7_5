Option Strict Off
Option Explicit On

''' <summary>
''' Pure configuration decisions extracted from GmResidentClient.ReadConfiguration.
''' </summary>
Friend NotInheritable Class ResidentConfigRules

    Friend Const DefaultSignalRegistrationMode As String = "DISPLAYS"

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Returns <paramref name="mode"/> when it is a recognized (already upper-cased) signal
    ''' registration mode; otherwise "DISPLAYS".
    ''' </summary>
    Friend Shared Function NormalizeSignalRegistrationMode(mode As String) As String
        Select Case mode
            Case "FULL", "DISPLAYS", "GO/NOGO", "NEW FULL"
                Return mode
            Case Else
                Return DefaultSignalRegistrationMode
        End Select
    End Function

    ''' <summary>
    ''' True when the TimeMachine provider was selected but its configuration is disabled,
    ''' in which case time sync falls back to OXTS.
    ''' </summary>
    Friend Shared Function ShouldFallBackToOxts(providerType As String, timeMachineEnabled As Boolean) As Boolean
        Return providerType = "TIMEMACHINE" AndAlso Not timeMachineEnabled
    End Function
End Class
