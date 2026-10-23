Option Strict On
Option Explicit On

''' <summary>
''' Updates the session-end fields in an ANNO file header (rows "1,&lt;Field ID&gt;,&lt;Field Name&gt;,&lt;Value&gt;"
''' written by CreateANNOFile). Rows are matched by field name and keep their existing field ID,
''' so the update stays correct if header rows are added or reordered.
''' </summary>
Friend NotInheritable Class AnnoHeaderUpdater

    Friend Const EndDateField As String = "End Date"
    Friend Const EndTimeField As String = "End Time"
    Friend Const RecordedMileageField As String = "RecordedMileage"
    Friend Const LccActiveMileageField As String = "LCCActiveMileage"

    Private Sub New()
    End Sub

    Friend Shared Sub UpdateSessionEndFields(ByVal lines() As String, ByVal endTime As Date,
                                             ByVal recordedMileage As Double, ByVal lccActiveMileage As Double)
        If lines Is Nothing Then Exit Sub

        For i As Integer = 0 To lines.Length - 1
            Dim line As String = lines(i)
            If line Is Nothing OrElse Not line.StartsWith("1,", StringComparison.Ordinal) Then Continue For

            Dim parts() As String = line.Split(","c)
            If parts.Length < 3 Then Continue For

            Dim value As String
            Select Case parts(2)
                Case EndDateField
                    value = endTime.ToString("MM/dd/yyyy", Globalization.CultureInfo.InvariantCulture)
                Case EndTimeField
                    value = endTime.ToString("HH:mm:ss", Globalization.CultureInfo.InvariantCulture)
                Case RecordedMileageField
                    value = recordedMileage.ToString("0.0", Globalization.CultureInfo.InvariantCulture)
                Case LccActiveMileageField
                    value = lccActiveMileage.ToString("0.0", Globalization.CultureInfo.InvariantCulture)
                Case Else
                    Continue For
            End Select

            lines(i) = $"1,{parts(1)},{parts(2)},{value}"
        Next
    End Sub

End Class
