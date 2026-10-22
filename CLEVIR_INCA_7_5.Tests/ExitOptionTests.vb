Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ExitOptionTests

    <TestMethod>
    <DataRow(ExitOption.ExitClevirAndCloseInca)>
    <DataRow(ExitOption.ExitClevirOnly)>
    <DataRow(ExitOption.ExitClevirCloseIncaShutdownWindows)>
    Public Sub ExitChoices_AreKept(choice As ExitOption)
        Assert.AreEqual(choice, GmResidentClient.NormalizeExitDialogOption(choice))
    End Sub

    <TestMethod>
    Public Sub Cancel_IsCancel()
        Assert.AreEqual(ExitOption.CancelExit, GmResidentClient.NormalizeExitDialogOption(ExitOption.CancelExit))
    End Sub

    <TestMethod>
    Public Sub DialogClosedWithoutSelection_IsCancel()
        ' Closing the exit dialog with the window X leaves SelectedExitOption = None; this used to force-exit CLEVIR.
        Assert.AreEqual(ExitOption.CancelExit, GmResidentClient.NormalizeExitDialogOption(ExitOption.None))
    End Sub

End Class
