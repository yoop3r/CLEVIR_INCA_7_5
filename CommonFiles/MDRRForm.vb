Option Strict Off
Option Explicit On

Imports VB6 = Microsoft.VisualBasic.Compatibility.VB6

Friend Class MDRRForm
    Inherits System.Windows.Forms.Form


    Private Sub Form3_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load


        Me.Top = 0
        Me.Left = 0

        'MSChart1.chartType = MSChart20Lib.VtChChartType.VtChChartType2dXY
        'MSChart1.RowCount = 1
        'MSChart1.ColumnCount = GM_ResidentClient.Chart1MaxPoints

        'AxMSChart1.chartType = MSChart20Lib.VtChChartType.VtChChartType2dXY
        'AxMSChart1.RowCount = 1
        'AxMSChart1.ColumnCount = GM_ResidentClient.Chart2MaxPoints

        'AxMSChart2.chartType = MSChart20Lib.VtChChartType.VtChChartType2dXY
        'AxMSChart2.RowCount = 1
        'AxMSChart2.ColumnCount = GM_ResidentClient.Chart3MaxPoints

        'AxMSChart3.chartType = MSChart20Lib.VtChChartType.VtChChartType2dXY
        'AxMSChart3.RowCount = 1
        'AxMSChart3.ColumnCount = GM_ResidentClient.Chart4MaxPoints


    End Sub

    Public Sub ChartData(ByVal MSChart As AxMSChart20Lib.AxMSChart, ByVal values(,) As Single)

        MSChart.ChartData = values

    End Sub

End Class