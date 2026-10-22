Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class AnnoHeaderUpdaterTests

    ' Header as written by Module1.CreateANNOFile (Email/Group rows shift later rows by two).
    Private Shared Function CreateTemplate() As String()
        Return {
            "0, Field ID,Field Name,Value",
            "1,98,CSV Version,0",
            "1,99,CLIHA Version,1",
            "1,1,Session Name,20261022_174115_DRIVER_6SME5384",
            "1,2,Driver,DRIVER",
            "1,16,Email,driver@example.com",
            "1,17,Group,Front Impact",
            "1,4,Country,1",
            "1,3,State,23",
            "1,5,Start Date,10/22/2026",
            "1,6,Start Time,17:41:15",
            "1,7,End Date,10/22/2026",
            "1,8,End Time,17:41:15",
            "1,9,Notes,some, notes",
            "1,10,Procedure,Development",
            "1,12,Vehicle,6SME5384",
            "1,13,Thumbnail,-1",
            "1,14,RecordedMileage,0",
            "1,15,LCCActiveMileage,0",
            "0,Anno Type ID,Anno Type,Anno Value ID,Anno Value,Anno Enum Type,Anno Enum,Start Seq#,Start (ms),End Seq#,End (ms),Point Seq#,MDA Point (sec),Thumbnail,WAV,MF4 Filename,Mileage,LAT POS,LON POS,LaneClass_Crnt",
            "3,1000,BUTTON FEEDBACK,3060,ACC Event,0,2,1,10583,1,10583,1,10.583,0,a.wav,a.mf4,11412.7,42.5867,-83.6865,0"
        }
    End Function

    <TestMethod>
    Public Sub UpdatesOnlySessionEndRows_KeepingTheirFieldIds()
        Dim lines = CreateTemplate()
        Dim original = CType(lines.Clone(), String())

        AnnoHeaderUpdater.UpdateSessionEndFields(lines, New Date(2026, 10, 23, 8, 5, 9), 12.345, 3.21)

        Assert.AreEqual("1,7,End Date,10/23/2026", lines(11))
        Assert.AreEqual("1,8,End Time,08:05:09", lines(12))
        Assert.AreEqual("1,14,RecordedMileage,12.3", lines(17))
        Assert.AreEqual("1,15,LCCActiveMileage,3.2", lines(18))

        For Each i In {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13, 14, 15, 16, 19, 20}
            Assert.AreEqual(original(i), lines(i), $"Row {i} must not change")
        Next
        Assert.AreEqual(original.Length, lines.Length)
    End Sub

    <TestMethod>
    Public Sub MissingRows_AreNotAdded_AndNothingIsSafe()
        Dim lines = {"0, Field ID,Field Name,Value", "1,1,Session Name,X"}
        AnnoHeaderUpdater.UpdateSessionEndFields(lines, Date.Now, 1, 1)
        CollectionAssert.AreEqual({"0, Field ID,Field Name,Value", "1,1,Session Name,X"}, lines)

        AnnoHeaderUpdater.UpdateSessionEndFields(Nothing, Date.Now, 1, 1)
    End Sub

End Class
