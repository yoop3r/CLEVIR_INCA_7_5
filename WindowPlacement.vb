Option Strict On
Option Explicit On

Imports System.Drawing

''' <summary>Placement helpers for secondary windows opened next to another form.</summary>
Friend Module WindowPlacement

    ''' <summary>
    ''' Where to open a window that should appear at its anchor form's position. A minimized form reports (-32000, -32000)
    ''' and a hidden one may be anywhere, so in those cases <paramref name="fallback"/> is used to keep the window on screen.
    ''' </summary>
    Friend Function OpenAtAnchor(anchorVisible As Boolean, anchorState As FormWindowState, anchorLocation As Point,
                                 fallback As Point) As Point
        If anchorVisible AndAlso anchorState <> FormWindowState.Minimized Then Return anchorLocation
        Return fallback
    End Function

End Module
