' Create a custom progress bar class that paints percentage internally
Public Class PercentageProgressBar
    Inherits ProgressBar

    Public Sub New()
        MyBase.New()
        Me.SetStyle(ControlStyles.UserPaint, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim rect As New Rectangle(0, 0, Me.Width, Me.Height)

        ' Draw progress background
        e.Graphics.FillRectangle(New SolidBrush(Me.BackColor), rect)

        ' Draw progress bar with gradient
        Dim progressWidth As Integer = CInt((Me.Value / CDbl(Me.Maximum)) * Me.Width)
        If progressWidth > 0 Then
            Dim progressRect As New Rectangle(0, 0, progressWidth, Me.Height)
            Using brush As New System.Drawing.Drawing2D.LinearGradientBrush(
                progressRect,
                Color.LightGreen,
                Color.DarkGreen,
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal)
                e.Graphics.FillRectangle(brush, progressRect)
            End Using
        End If

        ' Draw border
        e.Graphics.DrawRectangle(Pens.DarkGray, 0, 0, Me.Width - 1, Me.Height - 1)

        ' Draw percentage text
        Dim pctText As String = Format(((Me.Value / Math.Max(1.0, Me.Maximum)) * 100), "0") & "%"
        Using font As New Font(SystemFonts.DefaultFont.FontFamily, 12, FontStyle.Bold)
            Dim sz = e.Graphics.MeasureString(pctText, font)
            Dim x = (Me.Width - sz.Width) / 2.0F
            Dim y = (Me.Height - sz.Height) / 2.0F

            ' Draw text with shadow for better visibility
            e.Graphics.DrawString(pctText, font, Brushes.Gray, New PointF(x + 1, y + 1))
            e.Graphics.DrawString(pctText, font, Brushes.Black, New PointF(x, y))
        End Using
    End Sub
End Class
