Option Strict On
Option Explicit On

Imports System.Linq
Imports System.Text

''' <summary>
''' Column header for annotation rows, shared by the per-session ANNO file and the per-vehicle
''' aggregate annotations file. The aggregate file lives across sessions, so its header can be
''' older than the current LiDAR configuration; <see cref="GetUpgradedHeader"/> widens it.
''' </summary>
Friend NotInheritable Class AnnotationCsvHeader

    Friend Const BaseColumns As String = "0,Anno Type ID,Anno Type,Anno Value ID,Anno Value,Anno Enum Type,Anno Enum,Start Seq#,Start (ms),End Seq#,End (ms),Point Seq#,MDA Point (sec),Thumbnail,WAV,MF4 Filename,Mileage,LAT POS,LON POS,LaneClass_Crnt"

    Private Sub New()
    End Sub

    Friend Shared Function Build(ByVal lidarCount As Integer) As String
        Dim sb As New StringBuilder(BaseColumns)
        For i As Integer = 1 To lidarCount
            sb.Append(",LiDAR[").Append(i).Append("]Frame")
        Next
        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Returns the header that should replace <paramref name="existingHeader"/>, or Nothing when it is
    ''' already correct or is not an annotation header. LiDAR columns are only ever added (never removed)
    ''' so older rows with more frame values keep their headings; stray trailing commas are dropped.
    ''' </summary>
    Friend Shared Function GetUpgradedHeader(ByVal existingHeader As String, ByVal lidarCount As Integer) As String
        If existingHeader Is Nothing OrElse Not existingHeader.StartsWith(BaseColumns, StringComparison.Ordinal) Then
            Return Nothing
        End If

        Dim existingLidarColumns As Integer = existingHeader.Split(","c).
            Count(Function(f) f.StartsWith("LiDAR[", StringComparison.Ordinal) AndAlso f.EndsWith("]Frame", StringComparison.Ordinal))

        Dim upgraded As String = Build(Math.Max(existingLidarColumns, lidarCount))
        Return If(String.Equals(upgraded, existingHeader, StringComparison.Ordinal), Nothing, upgraded)
    End Function

End Class
