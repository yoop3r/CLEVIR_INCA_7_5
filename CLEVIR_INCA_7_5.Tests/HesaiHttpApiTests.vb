Imports System.Collections.Concurrent
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class HesaiHttpApiTests

    ''' <summary>Minimal pandar.cgi stand-in. <paramref name="failFor"/> decides, per object and attempt, whether to answer HTTP 500.</summary>
    Private NotInheritable Class FakeLidar
        Implements IDisposable

        Private ReadOnly _listener As New HttpListener()
        Private ReadOnly _failFor As Func(Of String, Integer, Boolean)
        Public ReadOnly Requests As New ConcurrentDictionary(Of String, Integer)
        Public ReadOnly Port As Integer

        Public Sub New(failFor As Func(Of String, Integer, Boolean))
            _failFor = failFor
            Dim probe As New TcpListener(IPAddress.Loopback, 0)
            probe.Start()
            Port = CType(probe.LocalEndpoint, IPEndPoint).Port
            probe.Stop()
            _listener.Prefixes.Add($"http://localhost:{Port}/")
            _listener.Start()
            Dim worker As New Thread(AddressOf Serve) With {.IsBackground = True}
            worker.Start()
        End Sub

        Private Sub Serve()
            While _listener.IsListening
                Dim ctx As HttpListenerContext
                Try
                    ctx = _listener.GetContext()
                Catch
                    Return
                End Try
                Dim obj As String = ctx.Request.QueryString("object")
                Dim attempt As Integer = Requests.AddOrUpdate(obj, 1, Function(k, v) v + 1)
                Dim json As String
                If _failFor(obj, attempt) Then
                    ctx.Response.StatusCode = 500
                    json = ""
                Else
                    json = "{""Head"":{""ErrorCode"":""0"",""Message"":""Success""},""Body"":" & BodyFor(obj) & "}"
                End If
                Dim bytes = Encoding.UTF8.GetBytes(json)
                ctx.Response.ContentLength64 = bytes.Length
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length)
                ctx.Response.Close()
            End While
        End Sub

        Private Shared Function BodyFor(obj As String) As String
            Select Case obj
                Case "device_info" : Return "{""SN"":""P128TEST"",""Model"":""Pandar128E3X""}"
                Case "lidar_data" : Return "{""lidar_mode"":""1""}"
                Case "lidar_sync" : Return "{""sync"":""1"",""syncAngle"":""0""}"
                Case Else : Return "{}"
            End Select
        End Function

        Public Function Count(obj As String) As Integer
            Dim n As Integer
            Requests.TryGetValue(obj, n)
            Return n
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            _listener.Close()
        End Sub
    End Class

    <TestMethod>
    Public Sub GetMetadata_TransientFailure_RecoveredOnRetry()
        Using lidar As New FakeLidar(Function(obj, attempt) obj = "device_info" AndAlso attempt = 1)
            Dim result = HesaiHttpApi.GetMetadata("localhost", lidar.Port)

            Assert.IsNotNull(result)
            Assert.AreEqual(0, result.FailedObjects.Count)
            Assert.IsNotNull(result.Device)
            Assert.AreEqual(2, lidar.Count("device_info"))
            Assert.AreEqual(1, lidar.Count("lidar_calibration"))
        End Using
    End Sub

    <TestMethod>
    Public Sub GetMetadata_LateConfigRetry_KeepsReturnModeAndSync()
        ' lidar_config creates Configuration; recovering it on retry must not discard lidar_data/lidar_sync values.
        Using lidar As New FakeLidar(Function(obj, attempt) obj = "lidar_config" AndAlso attempt = 1)
            Dim result = HesaiHttpApi.GetMetadata("localhost", lidar.Port)

            Assert.AreEqual(0, result.FailedObjects.Count)
            Assert.AreEqual(1, result.Configuration.ReturnMode.Value)
            Assert.IsTrue(result.Configuration.SyncEnabled.Value)
        End Using
    End Sub

    <TestMethod>
    Public Sub GetMetadata_StillFailing_StopsRetryAtFirstFailure()
        Using lidar As New FakeLidar(Function(obj, attempt) obj = "device_info" OrElse obj = "lidar_config")
            Dim result = HesaiHttpApi.GetMetadata("localhost", lidar.Port)

            CollectionAssert.AreEqual({"device_info", "lidar_config"}, result.FailedObjects)
            Assert.AreEqual(2, lidar.Count("device_info"))
            Assert.AreEqual(1, lidar.Count("lidar_config"))
            Assert.IsNull(result.Device)
            Assert.AreEqual(1, result.Configuration.ReturnMode.Value)
        End Using
    End Sub

    <TestMethod>
    Public Sub GetMetadata_NothingAnswers_ReturnsNothingWithoutRetry()
        Using lidar As New FakeLidar(Function(obj, attempt) True)
            Assert.IsNull(HesaiHttpApi.GetMetadata("localhost", lidar.Port))
            Assert.AreEqual(1, lidar.Count("device_info"))
        End Using
    End Sub

End Class
