Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ResidentConfigRulesTests

    <TestMethod>
    Public Sub NormalizeSignalRegistrationMode_KnownModes_AreKept()
        For Each mode In {"FULL", "DISPLAYS", "GO/NOGO", "NEW FULL"}
            Assert.AreEqual(mode, ResidentConfigRules.NormalizeSignalRegistrationMode(mode))
        Next
    End Sub

    <TestMethod>
    Public Sub NormalizeSignalRegistrationMode_UnknownModes_BecomeDisplays()
        Assert.AreEqual("DISPLAYS", ResidentConfigRules.NormalizeSignalRegistrationMode("PARTIAL"))
        Assert.AreEqual("DISPLAYS", ResidentConfigRules.NormalizeSignalRegistrationMode(""))
        Assert.AreEqual("DISPLAYS", ResidentConfigRules.NormalizeSignalRegistrationMode(" FULL"))
    End Sub

    <TestMethod>
    Public Sub NormalizeSignalRegistrationMode_IsCaseSensitive_CallerUpperCases()
        Assert.AreEqual("DISPLAYS", ResidentConfigRules.NormalizeSignalRegistrationMode("full"))
    End Sub

    <TestMethod>
    Public Sub ShouldFallBackToOxts_OnlyForDisabledTimeMachine()
        Assert.IsTrue(ResidentConfigRules.ShouldFallBackToOxts("TIMEMACHINE", False))
        Assert.IsFalse(ResidentConfigRules.ShouldFallBackToOxts("TIMEMACHINE", True))
        Assert.IsFalse(ResidentConfigRules.ShouldFallBackToOxts("OXTS", False))
        Assert.IsFalse(ResidentConfigRules.ShouldFallBackToOxts("timemachine", False))
    End Sub

End Class
