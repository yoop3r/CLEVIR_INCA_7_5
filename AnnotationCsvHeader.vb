Option Strict On
Option Explicit On

Imports System.Linq
Imports System.Text

''' <summary>
''' Column header for annotation rows, shared by the per-session ANNO file and the per-vehicle
''' aggregate annotations file. The aggregate file lives across sessions, so its header can be
''' older than the current LiDAR configuration; <see cref="GetUpgradedHeader"/> widens or relabels it.
''' </summary>
Friend NotInheritable Class AnnotationCsvHeader

    Friend Const BaseColumns As String = "0,Anno Type ID,Anno Type,Anno Value ID,Anno Value,Anno Enum Type,Anno Enum,Start Seq#,Start (ms),End Seq#,End (ms),Point Seq#,MDA Point (sec),Thumbnail,WAV,MF4 Filename,Mileage,LAT POS,LON POS,LaneClass_Crnt"

    Private Sub New()
    End Sub

    ''' <summary>Header with numbered LiDAR columns (LiDAR[1]Frame ...), used when no orientations are known.</summary>
    Friend Shared Function Build(ByVal lidarCount As Integer) As String
        Return Build(NumberedLabels(lidarCount))
    End Function

    Friend Shared Function Build(ByVal lidarLabels As IList(Of String)) As String
        Dim sb As New StringBuilder(BaseColumns)
        For Each label In lidarLabels
            sb.Append(",LiDAR[").Append(label).Append("]Frame")
        Next
        Return sb.ToString()
    End Function

    ''' <summary>
    ''' One column label per LiDAR device, in device order: the mounting orientation (FRONT, REAR, ...) when
    ''' configured, otherwise the 1-based position. Duplicates get the position appended (FRONT_1, FRONT_2).
    ''' </summary>
    Friend Shared Function LidarColumnLabels(ByVal orientations As IList(Of String)) As String()
        Dim labels(orientations.Count - 1) As String
        For i As Integer = 0 To orientations.Count - 1
            Dim cleaned As String = SanitizeLabel(orientations(i))
            labels(i) = If(cleaned.Length = 0, (i + 1).ToString(), cleaned)
        Next

        Dim duplicated As HashSet(Of String) = labels.GroupBy(Function(l) l, StringComparer.Ordinal).
            Where(Function(g) g.Count() > 1).Select(Function(g) g.Key).ToHashSet(StringComparer.Ordinal)
        For i As Integer = 0 To labels.Length - 1
            If duplicated.Contains(labels(i)) Then labels(i) &= "_" & (i + 1).ToString()
        Next
        Return labels
    End Function

    Friend Shared Function GetUpgradedHeader(ByVal existingHeader As String, ByVal lidarCount As Integer) As String
        Return GetUpgradedHeader(existingHeader, NumberedLabels(lidarCount))
    End Function

    ''' <summary>
    ''' Returns the header that should replace <paramref name="existingHeader"/>, or Nothing when it is
    ''' already correct or is not an annotation header. LiDAR columns are never removed: if the file already
    ''' has more LiDAR columns than devices are configured, its LiDAR headings are kept as they are. Otherwise
    ''' the LiDAR headings are rewritten from the current devices - widening, and renaming e.g. LiDAR[1]Frame
    ''' to LiDAR[FRONT]Frame (the file is per vehicle, so column order matches device order). Stray trailing
    ''' commas are dropped.
    ''' </summary>
    Friend Shared Function GetUpgradedHeader(ByVal existingHeader As String, ByVal lidarLabels As IList(Of String)) As String
        If existingHeader Is Nothing OrElse Not existingHeader.StartsWith(BaseColumns, StringComparison.Ordinal) Then
            Return Nothing
        End If

        Const prefix As String = "LiDAR["
        Const suffix As String = "]Frame"
        Dim existingLabels As List(Of String) = existingHeader.Split(","c).
            Where(Function(f) f.StartsWith(prefix, StringComparison.Ordinal) AndAlso f.EndsWith(suffix, StringComparison.Ordinal)).
            Select(Function(f) f.Substring(prefix.Length, f.Length - prefix.Length - suffix.Length)).
            ToList()

        Dim upgraded As String = Build(If(existingLabels.Count > lidarLabels.Count, existingLabels, lidarLabels))
        Return If(String.Equals(upgraded, existingHeader, StringComparison.Ordinal), Nothing, upgraded)
    End Function

    Private Shared Function NumberedLabels(ByVal count As Integer) As String()
        Return Enumerable.Range(1, Math.Max(0, count)).Select(Function(i) i.ToString()).ToArray()
    End Function

    Private Shared Function SanitizeLabel(ByVal orientation As String) As String
        If String.IsNullOrWhiteSpace(orientation) Then Return String.Empty
        Dim sb As New StringBuilder()
        For Each c As Char In orientation.Trim().ToUpperInvariant()
            sb.Append(If(Char.IsLetterOrDigit(c) OrElse c = "_"c OrElse c = "-"c, c, "_"c))
        Next
        Return sb.ToString()
    End Function

End Class
