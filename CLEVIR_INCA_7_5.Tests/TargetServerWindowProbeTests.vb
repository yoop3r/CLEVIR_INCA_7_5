Imports System.Diagnostics
Imports System.Threading
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class TargetServerWindowProbeTests

    <TestMethod>
    Public Sub NativeMessageBox_IsFoundWithTitleAndBodyText()
        Dim psi As New ProcessStartInfo("powershell.exe",
            "-NoProfile -NonInteractive -Command ""Add-Type -AssemblyName System.Windows.Forms; " &
            "[System.Windows.Forms.MessageBox]::Show('Lost communication with camera REAR', 'Probe Test Server') | Out-Null""") With {
            .UseShellExecute = False,
            .CreateNoWindow = True
        }
        Using p As Process = Process.Start(psi)
            Try
                Dim found As List(Of ProbedWindow) = Nothing
                Dim deadline = DateTime.UtcNow.AddSeconds(20)
                Do
                    found = TargetServerWindowProbe.FindVisibleWindows(New HashSet(Of Integer) From {p.Id})
                    If found.Count > 0 Then Exit Do
                    Thread.Sleep(200)
                Loop While DateTime.UtcNow < deadline

                Assert.HasCount(1, found)
                Assert.AreEqual("Probe Test Server", found(0).Title)
                Assert.Contains("Lost communication with camera REAR", found(0).Text)
            Finally
                If Not p.HasExited Then p.Kill()
            End Try
        End Using
    End Sub

    <TestMethod>
    Public Sub HiddenWindow_IsIgnored_VisibleWindowIsFound()
        Dim pid As New HashSet(Of Integer) From {Environment.ProcessId}
        Dim whileHidden As List(Of ProbedWindow) = Nothing
        Dim whileShown As List(Of ProbedWindow) = Nothing
        Dim formHandle As IntPtr

        RunSta(Sub()
                   Using f As New Form With {.Text = "Probe hidden/visible test", .ShowInTaskbar = False}
                       formHandle = f.Handle
                       whileHidden = TargetServerWindowProbe.FindVisibleWindows(pid)
                       f.Show()
                       whileShown = TargetServerWindowProbe.FindVisibleWindows(pid)
                   End Using
               End Sub)

        Assert.IsFalse(whileHidden.Any(Function(w) w.Handle = formHandle))
        Assert.IsTrue(whileShown.Any(Function(w) w.Handle = formHandle AndAlso w.Title = "Probe hidden/visible test"))
    End Sub

    <TestMethod>
    Public Sub NoMatchingProcess_ReturnsEmpty()
        Assert.IsEmpty(TargetServerWindowProbe.FindVisibleWindows("CLEVIR_NO_SUCH_PROCESS_" & Guid.NewGuid().ToString("N")))
    End Sub

    Private Shared Sub RunSta(action As Action)
        Dim failure As Exception = Nothing
        Dim t As New Thread(Sub()
                                Try
                                    action()
                                Catch ex As Exception
                                    failure = ex
                                End Try
                            End Sub)
        t.SetApartmentState(ApartmentState.STA)
        t.Start()
        t.Join()
        If failure IsNot Nothing Then Throw failure
    End Sub

End Class
