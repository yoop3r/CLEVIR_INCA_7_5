Option Strict Off
Option Explicit On

Imports System.Xml

''' <summary>
''' Parses a &lt;Lidar&gt;/&lt;Extrinsic&gt; mount-transform node, extracted from
''' GmResidentClient.ReadLidarConfiguration.
''' </summary>
Friend NotInheritable Class LidarExtrinsicParser

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Builds an ExtrinsicRecord from <paramref name="extrinsicNode"/>. Missing or malformed
    ''' vectors fall back to identity (0,0,0 / 0,0,0,1); missing IsCalibrated means False.
    ''' A non-boolean IsCalibrated attribute throws FormatException.
    ''' </summary>
    Friend Shared Function Parse(extrinsicNode As XmlNode) As PcapEventBridge.ExtrinsicRecord
        Dim isCalibratedAttr As String = extrinsicNode.Attributes("IsCalibrated")?.Value
        Dim isCalibrated As Boolean = If(String.IsNullOrEmpty(isCalibratedAttr), False, Boolean.Parse(isCalibratedAttr))

        Dim translationText As String = extrinsicNode.SelectSingleNode("Translation")?.InnerText
        Dim rotationText As String = extrinsicNode.SelectSingleNode("RotationQuaternion")?.InnerText
        Dim residualText As String = extrinsicNode.SelectSingleNode("ResidualError")?.InnerText
        Dim dateText As String = extrinsicNode.SelectSingleNode("DatePerformed")?.InnerText

        Dim translation As Double() = ResidentClientText.ParseDoubleVector(translationText, 3, {0.0, 0.0, 0.0})
        Dim rotation As Double() = ResidentClientText.ParseDoubleVector(rotationText, 4, {0.0, 0.0, 0.0, 1.0})

        Dim residual As Double? = Nothing
        Dim residualParsed As Double
        If Double.TryParse(residualText, residualParsed) Then
            residual = residualParsed
        End If

        Dim datePerformed As DateTime? = Nothing
        Dim dateParsed As DateTime
        If DateTime.TryParse(dateText, dateParsed) Then
            datePerformed = dateParsed
        End If

        Return New PcapEventBridge.ExtrinsicRecord With {
            .CalibrationId = extrinsicNode.SelectSingleNode("CalibrationId")?.InnerText,
            .DatePerformed = datePerformed,
            .Method = extrinsicNode.SelectSingleNode("Method")?.InnerText,
            .TranslationMeters = translation,
            .RotationQuaternion = rotation,
            .ResidualError = residual,
            .IsCalibrated = isCalibrated
        }
    End Function
End Class
