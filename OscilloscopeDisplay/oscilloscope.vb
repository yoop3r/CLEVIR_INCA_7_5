
Option Strict Off

Imports System.Windows.Forms.DataVisualization.Charting

Public Class Oscilloscope

    'The oscilloscope form is used in conjunction with the LMFR_Status_Display custom screen...
    'This form module resides in its own directory (OscilloscopeDisplay)

    Private StopMe As Boolean

    Public Function GetRandom(ByVal min As Double, ByVal max As Double) As Double

        'Gets a random number between min and max...

        ' by making Generator static, we preserve the same instance '
        ' (i.e., do not create new instances with the same seed over and over) '
        ' between calls '
        Static generator As System.Random = New System.Random()
        Return generator.Next(min, max)
    End Function
    Private Sub oscilloscope_Load(sender As Object, e As EventArgs) Handles Me.Load

        'VeLMFR_l_CntrlPtLatOffsetHPP -Blue
        'VeLMFR_l_CntrlPtLatOffsetL -Magenta
        'VeLMFR_l_CntrlPtLatOffsetR -Cyan
        'VeLMFR_l_CntrlPtLatOffsetPrev -White

        Chart1.Series("HPP").ChartType = SeriesChartType.Spline
        Chart1.Series("HPP").BorderWidth = 1
        Chart1.Series("HPP").Color = Color.Blue
        Chart1.Series("HPP").Label = ""

        'Chart1.ChartAreas(0).AxisX.LabelStyle.Format = "0.0"

        Chart1.ChartAreas(0).AxisX.LabelStyle.Enabled = False

        Chart1.ChartAreas(0).AxisY.LabelStyle.Format = "0.0"
        Chart1.ChartAreas(0).AxisY.LabelStyle.Format = "0.0"
        Chart1.ChartAreas(0).AxisY.LabelStyle.ForeColor = Color.White

        Chart1.Series("L").ChartType = SeriesChartType.Spline
        Chart1.Series("L").BorderWidth = 1
        Chart1.Series("L").Color = Color.Red
        Chart1.Series("L").Label = ""

        Chart1.Series("R").ChartType = SeriesChartType.Spline
        Chart1.Series("R").BorderWidth = 1
        Chart1.Series("R").Color = Color.LightGreen
        Chart1.Series("R").Label = ""

        Chart1.Series("Prev").ChartType = SeriesChartType.Spline
        Chart1.Series("Prev").BorderWidth = 1
        Chart1.Series("Prev").Color = Color.White
        Chart1.Series("Prev").Label = ""

        '// NO grids

        Chart1.ChartAreas(0).AxisX.MajorGrid.LineWidth = 1
        Chart1.ChartAreas(0).AxisX.MajorGrid.LineColor = Color.Gray
        Chart1.ChartAreas(0).AxisY.MajorGrid.LineWidth = 1
        Chart1.ChartAreas(0).AxisY.MajorGrid.LineColor = Color.Gray
        Chart1.ChartAreas(0).BackColor = Color.Black
        'Chart1.ChartAreas(0).BackColor = Color.White

        'Chart1.ChartAreas(0).AxisY.Minimum = -3.0
        'Chart1.ChartAreas(0).AxisY.Maximum = 3.0

    End Sub

    Private Sub Chart1_Click(sender As Object, e As EventArgs) Handles Chart1.Click

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        'This is for debug purposes and will not be called unless Start button is made visible on osilloscope form...

        Static x As Double = 0
        Static y0 As Double
        Static y1 As Double
        Static y2 As Double
        Static y3 As Double

        Try

            StopMe = False

            Do While StopMe = False

                y0 = CInt(GetRandom(-3.0, 3.0)) - 1.0
                y1 = CInt(GetRandom(-3.0, 3.0)) - 1.0
                y2 = CInt(GetRandom(-3.0, 3.0)) - 1.0
                y3 = CInt(GetRandom(-3.0, 3.0)) - 1.0

                Chart1.Series(0).Points.AddXY(x, y0)
                Chart1.Series(1).Points.AddXY(x, y1)
                Chart1.Series(2).Points.AddXY(x, y2)
                Chart1.Series(3).Points.AddXY(x, y3)

                If (Chart1.Series(0).Points.Count > 100) Then
                    Chart1.Series(0).Points.RemoveAt(0)
                End If


                If (Chart1.Series(1).Points.Count > 100) Then
                    Chart1.Series(1).Points.RemoveAt(0)
                End If


                If (Chart1.Series(2).Points.Count > 100) Then
                    Chart1.Series(2).Points.RemoveAt(0)
                End If


                If (Chart1.Series(3).Points.Count > 100) Then
                    Chart1.Series(3).Points.RemoveAt(0)
                End If

                Chart1.ChartAreas(0).AxisX.Minimum = Chart1.Series(0).Points(0).XValue
                Chart1.ChartAreas(0).AxisX.Maximum = x

                x = x + 0.1

                System.Threading.Thread.Sleep(100)
                System.Windows.Forms.Application.DoEvents()

            Loop

        Catch
            HandleUserMessageLogging("GMRC", Err.Number & " - " & Err.Description, DisplayMsgBox)
        End Try

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        StopMe = True
    End Sub

    Private Sub Chart1_Paint(sender As Object, e As PaintEventArgs) Handles Chart1.Paint


    End Sub

    Private Sub Chart1_SizeChanged(sender As Object, e As EventArgs) Handles Chart1.SizeChanged

    End Sub

    Private Sub Chart1_PrePaint(sender As Object, e As ChartPaintEventArgs) Handles Chart1.PrePaint

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs)


    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Chart1_MouseClick(sender As Object, e As MouseEventArgs) Handles Chart1.MouseClick

        If Chart1.ChartAreas(0).AxisY.Minimum <> 0 Then
            If e.Button = MouseButtons.Left Then
                Chart1.ChartAreas(0).AxisY.Minimum = Chart1.ChartAreas(0).AxisY.Minimum / 1.5
                Chart1.ChartAreas(0).AxisY.Maximum = Chart1.ChartAreas(0).AxisY.Maximum / 1.5
            End If
            If e.Button = MouseButtons.Right Then
                Chart1.ChartAreas(0).AxisY.Minimum = Chart1.ChartAreas(0).AxisY.Minimum * 1.5
                Chart1.ChartAreas(0).AxisY.Maximum = Chart1.ChartAreas(0).AxisY.Maximum * 1.5
            End If
        End If

    End Sub
End Class
