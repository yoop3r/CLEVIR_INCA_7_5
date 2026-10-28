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
    Public Sub NoTargetServerMessage_LeadsWithNewRecording_ThenReinitialize_ThenContinueWithout()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("REAR", Nothing, Nothing)

        Assert.StartsWith("INVALID VIDEO ALERT! (REAR) ", msg)
        Assert.Contains("lost for the rest of this recording", msg)
        Dim restore As Integer = msg.IndexOf("To restore REAR video: Stop Record, reconnect the REAR camera, then Start Record.", StringComparison.Ordinal)
        Dim reinit As Integer = msg.IndexOf("If video does not return, choose Re-Initialize NOW (INCA will be restarted, approx 3 minutes).", StringComparison.Ordinal)
        Dim continueWithout As Integer = msg.IndexOf("Or, if the camera cannot be restored, choose ""Continue Without REAR Video""", StringComparison.Ordinal)
        Assert.IsTrue(restore > 0 AndAlso reinit > restore AndAlso continueWithout > reinit, msg)
        Assert.EndsWith("to keep recording vehicle data without it.", msg)
    End Sub

    <TestMethod>
    Public Sub TwoCameras_RestoreStepsReadNaturally()
        Assert.AreEqual("REAR and FRONT", VideoAlertText.SpokenList("REAR,FRONT"))
        Assert.StartsWith("To restore REAR and FRONT video: Stop Record, reconnect the REAR and FRONT cameras, then Start Record.",
                          VideoAlertText.RestoreSteps("REAR,FRONT"))
    End Sub

    <TestMethod>
    Public Sub ContinuingWithoutToast_SaysHowToRestoreLater()
        Dim toast As String = VideoAlertText.ContinuingWithoutToast("REAR")

        Assert.StartsWith("Continuing without REAR video.", toast)
        Assert.Contains("all other alerts stay on", toast)
        Assert.EndsWith("Stop Record, reconnect the REAR camera, then Start Record.", toast)
    End Sub

    <TestMethod>
    Public Sub TargetServerClosedToast_NamesCamera_AndOffersAllThreeChoices()
        Dim toast As String = VideoAlertText.TargetServerClosedToast(BenchTitle)

        Assert.StartsWith("Target Server message closed. REAR video is lost for this recording.", toast)
        Assert.Contains("Stop Record, reconnect the camera, then Start Record (Re-Initialize if video does not return)", toast)
        Assert.EndsWith("Or continue without it from the video alert.", toast)
        Assert.Contains("Video from that camera is lost", VideoAlertText.TargetServerClosedToast("Camera Connection"))
    End Sub

    <TestMethod>
    Public Sub ContinueWithoutButton_NamesEveryCamera()
        Assert.AreEqual("Continue Without REAR Video", VideoAlertText.ContinueWithoutButton("REAR"))
        Assert.AreEqual("Continue Without REAR + FRONT Video", VideoAlertText.ContinueWithoutButton("REAR,FRONT"))
    End Sub

    <TestMethod>
    Public Sub NoTargetServerMessage_NoFrozenDevice_SaysNotYetIdentified()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("", Nothing, Nothing)

        Assert.Contains("(" & VideoAlertText.UnidentifiedDevice & ")", msg)
        Assert.Contains("To restore video: Stop Record, reconnect the camera, then Start Record.", msg)
        Assert.DoesNotContain("Continue Without", msg)
    End Sub

    <TestMethod>
    Public Sub RecentCameraMessage_QuotesTargetServer_NoRecoveryPromise()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("REAR", BenchTitle, BenchText)

        Assert.StartsWith("INVALID VIDEO ALERT! (REAR) ", msg)
        Assert.Contains("""" & BenchText & """", msg)
        Assert.Contains("will not re-synchronize", msg)
        Assert.Contains("Stop Record, reconnect the REAR camera, then Start Record.", msg)
        Assert.DoesNotContain("resumes on its own", msg)
        Assert.DoesNotContain("RESOLVED", msg)
    End Sub

    <TestMethod>
    Public Sub RecentCameraMessage_NoFrozenDevice_UsesTitlePrefix()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("", BenchTitle, BenchText)

        Assert.StartsWith("INVALID VIDEO ALERT! (REAR) ", msg)
        Assert.Contains("reconnect the REAR camera", msg)
        Assert.DoesNotContain("Continue Without", msg)
    End Sub

    <TestMethod>
    Public Sub RecentNonCameraMessage_FallsBackToTitle()
        Dim msg As String = VideoAlertText.InvalidVideoMessage("FRONT", "ES820: Hardware Connection", "")

        Assert.Contains("""ES820: Hardware Connection""", msg)
        Assert.Contains("choose Re-Initialize NOW (INCA will be restarted, approx 3 minutes).", msg)
    End Sub

End Class
