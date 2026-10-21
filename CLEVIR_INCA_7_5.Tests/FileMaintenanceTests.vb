Imports System.Globalization
Imports System.IO
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class FileMaintenanceTests

    Private _savedCulture As CultureInfo
    Private _root As String

    <TestInitialize>
    Public Sub Setup()
        _savedCulture = CultureInfo.CurrentCulture
        CultureInfo.CurrentCulture = New CultureInfo("en-US")
        _root = Path.Combine(Path.GetTempPath(), "FileMaintenanceTests_" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(_root)
    End Sub

    <TestCleanup>
    Public Sub Cleanup()
        CultureInfo.CurrentCulture = _savedCulture
        If Directory.Exists(_root) Then Directory.Delete(_root, recursive:=True)
    End Sub

    Private Function Touch(ParamArray parts As String()) As String
        Dim p As String = Path.Combine(_root, Path.Combine(parts))
        Directory.CreateDirectory(Path.GetDirectoryName(p))
        File.WriteAllText(p, "x")
        Return p
    End Function

    <TestMethod>
    <DataRow("C:\HB", "1234", "C:\HB\Data\gmcsv1234")>
    <DataRow("C:\HB\", "V1", "C:\HB\Data\gmcsvV1")>
    <DataRow("D:\Base", Nothing, "D:\Base\Data\gmcsv")>
    Public Sub VehicleDataRoot_CombinesDataAndGmcsvVehicleFolder(basePath As String, vehicle As String, expected As String)
        Assert.AreEqual(expected, FileMaintenance.VehicleDataRoot(basePath, vehicle))
    End Sub

    <TestMethod>
    <DataRow("C:\d\run1.mf4", "C:\d\run1.zip")>
    <DataRow("C:\d\run1.pcap", "C:\d\run1.zip")>
    <DataRow("C:\d.v2\cap", "C:\d.v2\cap.zip")>
    Public Sub ZipPathFor_ReplacesExtensionWithZip(dataFile As String, expected As String)
        Assert.AreEqual(expected, FileMaintenance.ZipPathFor(dataFile))
    End Sub

    <TestMethod>
    Public Sub SessionCompressionFolder_ExistingSessionUnderVehicleRoot_ReturnsSession()
        Dim vehicleRoot = Path.Combine(_root, "Data", "gmcsv1234")
        Dim session = Path.Combine(vehicleRoot, "20261021_153000_jdoe")
        Directory.CreateDirectory(session)

        Assert.AreEqual(session, FileMaintenance.SessionCompressionFolder(session, vehicleRoot))
    End Sub

    <TestMethod>
    <DataRow("")>
    <DataRow("\")>
    <DataRow("UPPER")>
    Public Sub SessionCompressionFolder_SessionIsVehicleRoot_ReturnsNothing(form As String)
        Dim vehicleRoot = Path.Combine(_root, "Data", "gmcsv1234")
        Directory.CreateDirectory(vehicleRoot)
        Dim session = If(form = "UPPER", vehicleRoot.ToUpperInvariant(), vehicleRoot & form)

        Assert.IsNull(FileMaintenance.SessionCompressionFolder(session, vehicleRoot))
    End Sub

    <TestMethod>
    Public Sub SessionCompressionFolder_SessionIsAncestorOfVehicleRoot_ReturnsNothing()
        Dim vehicleRoot = Path.Combine(_root, "Data", "gmcsv1234")
        Directory.CreateDirectory(vehicleRoot)

        Assert.IsNull(FileMaintenance.SessionCompressionFolder(Path.Combine(_root, "Data"), vehicleRoot))
        Assert.IsNull(FileMaintenance.SessionCompressionFolder(_root, vehicleRoot))
        Assert.IsNull(FileMaintenance.SessionCompressionFolder(Path.GetPathRoot(_root), vehicleRoot))
    End Sub

    <TestMethod>
    Public Sub SessionCompressionFolder_SiblingWithCommonPrefix_IsNotTreatedAsAncestor()
        Dim vehicleRoot = Path.Combine(_root, "Data", "gmcsv12")
        Dim sibling = Path.Combine(_root, "Data", "gmcsv1")
        Directory.CreateDirectory(sibling)

        Assert.AreEqual(sibling, FileMaintenance.SessionCompressionFolder(sibling, vehicleRoot))
    End Sub

    <TestMethod>
    <DataRow(Nothing)>
    <DataRow("")>
    <DataRow("   ")>
    Public Sub SessionCompressionFolder_NoSessionPath_ReturnsNothing(sessionPath As String)
        Assert.IsNull(FileMaintenance.SessionCompressionFolder(sessionPath, Path.Combine(_root, "Data", "gmcsv1")))
    End Sub

    <TestMethod>
    Public Sub SessionCompressionFolder_MissingSessionFolder_ReturnsNothing()
        Dim vehicleRoot = Path.Combine(_root, "Data", "gmcsv1")

        Assert.IsNull(FileMaintenance.SessionCompressionFolder(Path.Combine(vehicleRoot, "gone"), vehicleRoot))
    End Sub

    <TestMethod>
    Public Sub SessionCompressionFolder_NoVehicleRoot_ReturnsExistingSession()
        Assert.AreEqual(_root, FileMaintenance.SessionCompressionFolder(_root, Nothing))
    End Sub

    <TestMethod>
    Public Sub FindOrphanedFiles_ReturnsMatchingFilesWithoutSiblingZipRecursively()
        Dim orphanTop = Touch("a.mf4")
        Dim orphanNested = Touch("s1", "s2", "b.mf4")
        Touch("c.mf4")
        Touch("c.zip")
        Touch("d.pcap")
        Touch("e.mf4.bak")

        Dim found = FileMaintenance.FindOrphanedFiles(_root, "*.mf4").OrderBy(Function(f) f).ToList()

        CollectionAssert.AreEqual({orphanTop, orphanNested}.OrderBy(Function(f) f).ToList(), found)
    End Sub

    <TestMethod>
    Public Sub FindOrphanedFiles_ZipFromAnotherExtensionCountsAsArchived()
        Touch("run.mf4")
        Touch("run.zip")
        Touch("run.pcap")

        Assert.AreEqual(0, FileMaintenance.FindOrphanedFiles(_root, "*.pcap").Count())
    End Sub

    <TestMethod>
    Public Sub FindOrphanedFiles_ChecksEachZipOnlyWhenThatFileIsReached()
        Dim a = Touch("a.vsb")
        Dim b = Touch("b.vsb")
        Dim yielded As New List(Of String)

        For Each f In FileMaintenance.FindOrphanedFiles(_root, "*.vsb")
            yielded.Add(f)
            Dim other = If(f = a, b, a)
            File.WriteAllText(FileMaintenance.ZipPathFor(other), "z")
        Next

        Assert.AreEqual(1, yielded.Count)
    End Sub

    <TestMethod>
    Public Sub FindOrphanedFiles_MissingDirectory_ThrowsWhenEnumerated()
        Dim missing = Path.Combine(_root, "nope")
        Dim sequence = FileMaintenance.FindOrphanedFiles(missing, "*.mf4")

        Assert.ThrowsExactly(Of DirectoryNotFoundException)(Sub() sequence.ToList())
    End Sub

    <TestMethod>
    Public Sub CommLogArchiveFileName_UsesYyyMMddAnd12HourClock()
        Assert.AreEqual("20261021_020509_GM_INCA_Comm.log",
                        FileMaintenance.CommLogArchiveFileName(New Date(2026, 10, 21, 14, 5, 9)))
        Assert.AreEqual("20260102_120000_GM_INCA_Comm.log",
                        FileMaintenance.CommLogArchiveFileName(New Date(2026, 1, 2, 0, 0, 0)))
    End Sub

    <TestMethod>
    Public Sub CommLogFallbackFileName_UsesMMddyyyyAnd12HourClock()
        Assert.AreEqual("10212026_020509_GM_INCA_Comm.log",
                        FileMaintenance.CommLogFallbackFileName(New Date(2026, 10, 21, 14, 5, 9)))
    End Sub

    <TestMethod>
    Public Sub WaitUntilFileReady_UnlockedFile_ReturnsTrueWithoutLogging()
        Dim f = Touch("ready.xlsx")
        Dim log As New List(Of String)

        Assert.IsTrue(FileMaintenance.WaitUntilFileReady(f, 3, 0, AddressOf log.Add))
        Assert.AreEqual(0, log.Count)
    End Sub

    <TestMethod>
    Public Sub WaitUntilFileReady_LockedFile_LogsEachTryAndReturnsFalse()
        Dim f = Touch("locked.xlsx")
        Dim log As New List(Of String)

        Using holder As New FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Assert.IsFalse(FileMaintenance.WaitUntilFileReady(f, 3, 0, AddressOf log.Add))
        End Using

        Assert.AreEqual(3, log.Count)
        Assert.IsTrue(log.All(Function(m) m.StartsWith($"WaitUntilFileReady: {f} not ready - ")))
    End Sub

    <TestMethod>
    Public Sub WaitUntilFileReady_MissingFile_ReturnsFalse()
        Dim log As New List(Of String)

        Assert.IsFalse(FileMaintenance.WaitUntilFileReady(Path.Combine(_root, "missing.xlsx"), 2, 0, AddressOf log.Add))
        Assert.AreEqual(2, log.Count)
    End Sub

    <TestMethod>
    <DataRow(0)>
    <DataRow(-4)>
    Public Sub WaitUntilFileReady_NonPositiveTries_ReturnsFalseWithoutTrying(tries As Integer)
        Dim f = Touch("ready.xlsx")
        Dim log As New List(Of String)

        Assert.IsFalse(FileMaintenance.WaitUntilFileReady(f, tries, 0, AddressOf log.Add))
        Assert.AreEqual(0, log.Count)
    End Sub

    <TestMethod>
    Public Sub CopyWithRetries_OverwritesDestinationAndLogsSuccess()
        Dim src = Touch("src.xlsx")
        File.WriteAllText(src, "new")
        Dim dest = Touch("out", "dest.xlsx")
        Dim log As New List(Of String)

        Assert.IsTrue(FileMaintenance.CopyWithRetries(src, dest, 5, 0, AddressOf log.Add))

        Assert.AreEqual("new", File.ReadAllText(dest))
        CollectionAssert.AreEqual({$"Copied Driver_Anno_Cons.xlsx to {dest}"}, log)
    End Sub

    <TestMethod>
    Public Sub CopyWithRetries_MissingSource_RetriesMaxTimesThenReturnsFalse()
        Dim log As New List(Of String)

        Assert.IsFalse(FileMaintenance.CopyWithRetries(Path.Combine(_root, "missing"), Path.Combine(_root, "d"), 3, 0, AddressOf log.Add))

        Assert.AreEqual(3, log.Count)
        Assert.IsTrue(log(0).StartsWith("Retry 1/3 - Copy failed: "))
        Assert.IsTrue(log(2).StartsWith("Retry 3/3 - Copy failed: "))
    End Sub

    <TestMethod>
    Public Sub CopyWithRetries_ZeroRetries_StillAttemptsOnce()
        Dim log As New List(Of String)

        Assert.IsFalse(FileMaintenance.CopyWithRetries(Path.Combine(_root, "missing"), Path.Combine(_root, "d"), 0, 0, AddressOf log.Add))

        Assert.AreEqual(1, log.Count)
        Assert.IsTrue(log(0).StartsWith("Retry 1/0 - Copy failed: "))
    End Sub
End Class
