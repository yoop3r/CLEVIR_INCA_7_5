Option Strict Off
Option Explicit On

''' <summary>
''' On/off property classification for the Milford Proving Ground geofence, extracted from
''' GmResidentClient.WhereTheHeckAmIAt. Coordinates are in INCA GPS units (degrees * 3,600,000).
''' </summary>
Friend NotInheritable Class PropertyGeofence

    Friend Const OnProperty As String = "On Property"
    Friend Const OffProperty As String = "Off Property"

    Friend Const NorthwestCornerLat As Double = 153378676.8
    Friend Const NorthwestCornerLon As Double = -301326109.2
    Friend Const SouthwestCornerLat As Double = 153217998.0
    Friend Const SouthwestCornerLon As Double = -301313210.4
    Friend Const SoutheastCornerLat As Double = 153232621.2
    Friend Const SoutheastCornerLon As Double = -301173238.8
    Friend Const NortheastCornerLat As Double = 153357580.8
    Friend Const NortheastCornerLon As Double = -301181194.8
    Friend Const NorthMidpointLat As Double = 153381236.4
    Friend Const NorthMidpointLon As Double = -301250563.2

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Classifies a (non-zero) GPS position as "On Property" or "Off Property".
    ''' Positions in the ambiguous north-east "bermuda triangle" keep the previous classification:
    ''' they stay "Off Property" only if <paramref name="previousLocation"/> was "Off Property".
    ''' </summary>
    Friend Shared Function Classify(ByVal latPos As Double, ByVal lonPos As Double, ByVal previousLocation As String) As String
        Dim whereAmI As String

        If latPos > NorthMidpointLat Or latPos < SouthwestCornerLat Then
            whereAmI = OffProperty
        ElseIf lonPos < NorthwestCornerLon Or lonPos > NortheastCornerLon Then
            whereAmI = OffProperty
        ElseIf latPos < NortheastCornerLat And latPos > SoutheastCornerLat And
               lonPos > SouthwestCornerLon And lonPos < NortheastCornerLon Then
            whereAmI = OnProperty
        ElseIf latPos < NorthwestCornerLat And latPos > NortheastCornerLat And
               lonPos > NorthwestCornerLon And lonPos < NorthMidpointLon Then
            whereAmI = OnProperty
        Else 'this handles the "bermuda triangle" at the north east corner of MPG
            'If we are on property when we get here, we stay on property, if we are
            'off property when we get here, we stay off property...

            If previousLocation = OffProperty Then
                whereAmI = OffProperty
            Else
                whereAmI = OnProperty
            End If

        End If

        Return whereAmI
    End Function

End Class
