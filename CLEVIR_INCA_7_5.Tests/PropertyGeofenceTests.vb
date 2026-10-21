Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class PropertyGeofenceTests

    ' Representative points (INCA GPS units = degrees * 3,600,000) chosen against the geofence corners.
    Private Const MainBoxLat As Double = 153300000.0
    Private Const MainBoxLon As Double = -301250000.0
    Private Const NorthStripLat As Double = 153370000.0
    Private Const NorthStripLon As Double = -301300000.0
    Private Const BermudaLat As Double = 153370000.0
    Private Const BermudaLon As Double = -301200000.0

    <TestMethod>
    Public Sub NorthOfNorthMidpoint_IsOffProperty()
        Assert.AreEqual("Off Property", PropertyGeofence.Classify(153400000.0, MainBoxLon, "On Property"))
    End Sub

    <TestMethod>
    Public Sub SouthOfSouthwestCorner_IsOffProperty()
        Assert.AreEqual("Off Property", PropertyGeofence.Classify(153200000.0, MainBoxLon, "On Property"))
    End Sub

    <TestMethod>
    Public Sub WestOfNorthwestCorner_IsOffProperty()
        Assert.AreEqual("Off Property", PropertyGeofence.Classify(MainBoxLat, -301400000.0, "On Property"))
    End Sub

    <TestMethod>
    Public Sub EastOfNortheastCorner_IsOffProperty()
        Assert.AreEqual("Off Property", PropertyGeofence.Classify(MainBoxLat, -301100000.0, "On Property"))
    End Sub

    <TestMethod>
    Public Sub InsideMainBox_IsOnProperty_RegardlessOfPrevious()
        Assert.AreEqual("On Property", PropertyGeofence.Classify(MainBoxLat, MainBoxLon, "Off Property"))
        Assert.AreEqual("On Property", PropertyGeofence.Classify(MainBoxLat, MainBoxLon, Nothing))
    End Sub

    <TestMethod>
    Public Sub InsideNorthStrip_IsOnProperty_RegardlessOfPrevious()
        Assert.AreEqual("On Property", PropertyGeofence.Classify(NorthStripLat, NorthStripLon, "Off Property"))
    End Sub

    <TestMethod>
    Public Sub BermudaTriangle_StaysOff_WhenPreviouslyOff()
        Assert.AreEqual("Off Property", PropertyGeofence.Classify(BermudaLat, BermudaLon, "Off Property"))
    End Sub

    <TestMethod>
    Public Sub BermudaTriangle_IsOn_ForAnyOtherPrevious()
        Assert.AreEqual("On Property", PropertyGeofence.Classify(BermudaLat, BermudaLon, "On Property"))
        Assert.AreEqual("On Property", PropertyGeofence.Classify(BermudaLat, BermudaLon, Nothing))
        Assert.AreEqual("On Property", PropertyGeofence.Classify(BermudaLat, BermudaLon, "Unknown"))
    End Sub

End Class
