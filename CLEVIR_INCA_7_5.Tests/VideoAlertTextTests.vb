Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class VideoAlertTextTests

    ' Captured from the bench log (10/28 09:56:23).
    Private Const BenchTitle As String = "REAR: VideoCamera Add-On - Camera Connection"
    Private Const BenchText As String = "The camera appears to be disconnected. Please reconnect it."

    <TestMethod>
    Public Sub DeviceFromTitle_ReturnsPrefixBeforeColon()
        Assert.AreEqual("REAR", VideoAlertText.DeviceFromTargetServerTitle(BenchTitle))
    End Sub

    <TestMethod>
    <DataRow(Nothing)>
    <DataRow("")>
    <DataRow("Camera Connection")>
    <DataRow(": no device")>
    Public Sub DeviceFromTitle_NoPrefix_ReturnsEmpty(title As String)
        Assert.AreEqual("", VideoAlertText.DeviceFromTargetServerTitle(title))
    End Sub

    <TestMethod>
    Public Sub NoTargetServerMessage_SaysVideoLostAndReinitialize()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("REAR", Nothing, Nothing)

        Assert.StartsWith("INVALID VIDEO ALERT! (REAR) ", msg)
        Assert.Contains("lost for the rest of this recording", msg)
        Assert.EndsWith("Re-Initialize NOW (INCA will be restarted, approx 3 minutes) to restore video recording.", msg)
        Assert.DoesNotContain("Reconnect the camera", msg)
    End Sub

    <TestMethod>
    Public Sub NoTargetServerMessage_NoFrozenDevice_SaysNotYetIdentified()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("", Nothing, Nothing)

        Assert.Contains("(" & VideoAlertText.UnidentifiedDevice & ")", msg)
    End Sub

    <TestMethod>
    Public Sub RecentCameraMessage_ReconnectThenReinitialize_NoRecoveryPromise()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("REAR", BenchTitle, BenchText)

        Assert.StartsWith("INVALID VIDEO ALERT! (REAR) ", msg)
        Assert.Contains("""" & BenchText & """", msg)
        Assert.Contains("will not re-synchronize", msg)
        Assert.EndsWith("Reconnect the camera, then Re-Initialize NOW (INCA will be restarted, approx 3 minutes) to restore video recording.", msg)
        Assert.DoesNotContain("resumes on its own", msg)
        Assert.DoesNotContain("RESOLVED", msg)
    End Sub

    <TestMethod>
    Public Sub RecentCameraMessage_NoFrozenDevice_UsesTitlePrefix()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("", BenchTitle, BenchText)

        Assert.StartsWith("INVALID VIDEO ALERT! (REAR) ", msg)
    End Sub

    <TestMethod>
    Public Sub RecentNonCameraMessage_OmitsCameraAdvice_AndFallsBackToTitle()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("FRONT", "ES820: Hardware Connection", "")

        Assert.Contains("""ES820: Hardware Connection""", msg)
        Assert.DoesNotContain("Reconnect the camera", msg)
        Assert.EndsWith("Re-Initialize NOW (INCA will be restarted, approx 3 minutes) to restore video recording.", msg)
    End Sub

End Class
