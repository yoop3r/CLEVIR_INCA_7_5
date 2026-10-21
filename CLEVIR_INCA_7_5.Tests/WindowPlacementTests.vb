Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class WindowPlacementTests

    Private Shared ReadOnly AnchorAt As New Point(120, 45)
    Private Shared ReadOnly Fallback As New Point(0, 0)

    <TestMethod>
    <DataRow(FormWindowState.Normal)>
    <DataRow(FormWindowState.Maximized)>
    Public Sub VisibleAnchor_OpensAtAnchor(state As FormWindowState)
        Assert.AreEqual(AnchorAt, OpenAtAnchor(True, state, AnchorAt, Fallback))
    End Sub

    <TestMethod>
    Public Sub MinimizedAnchor_UsesFallback()
        ' A minimized form reports (-32000, -32000); the Top Down View opened there from SelectDisplays in vehicle mode.
        Assert.AreEqual(Fallback, OpenAtAnchor(True, FormWindowState.Minimized, New Point(-32000, -32000), Fallback))
    End Sub

    <TestMethod>
    Public Sub HiddenAnchor_UsesFallback()
        Assert.AreEqual(Fallback, OpenAtAnchor(False, FormWindowState.Normal, AnchorAt, Fallback))
    End Sub

End Class
