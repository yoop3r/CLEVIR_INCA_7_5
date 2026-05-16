Imports System.IO
Imports System.Management.Automation
Imports System.Threading

Public Class PowerShellHost
    Implements IDisposable

    Private ReadOnly _scriptPath As String
    Private _disposed As Boolean = False

    Public Sub New(scriptPath As String)
        If String.IsNullOrWhiteSpace(scriptPath) Then
            Throw New ArgumentException("PowerShell script path is null or empty.", NameOf(scriptPath))
        End If

        If Not File.Exists(scriptPath) Then
            Throw New FileNotFoundException($"PowerShell script not found: {scriptPath}", scriptPath)
        End If

        _scriptPath = scriptPath
    End Sub

    ''' <summary>
    ''' Loads the script and invokes the Invoke-ProcessSingleFile function inside an STA thread.
    ''' This avoids WinForms/Add-Type/MessageBox apartment issues and returns the first PSObject result.
    ''' </summary>
    Public Function ProcessSingleFile(fileName As String, Optional doRaidSync As Boolean = True) As PSObject
        If _disposed Then Throw New ObjectDisposedException(NameOf(PowerShellHost))

        Dim resultObj As PSObject = Nothing
        Dim workerException As Exception = Nothing
        Dim done As New ManualResetEvent(False)

        Dim threadStart As ThreadStart = Sub()
                                             Try
                                                 Using ps As PowerShell = PowerShell.Create()
                                                     ' Load script text
                                                     Dim scriptText As String = File.ReadAllText(_scriptPath)
                                                     ps.AddScript(scriptText).Invoke()
                                                     ps.Commands.Clear()

                                                     ' Check for errors during script load
                                                     If ps.Streams.Error.Count > 0 Then
                                                         Dim sb As New Text.StringBuilder()
                                                         For Each errRec As ErrorRecord In ps.Streams.Error
                                                             sb.AppendLine(errRec.ToString())
                                                         Next
                                                         Throw New InvalidOperationException("PowerShell script load error: " & sb.ToString())
                                                     End If

                                                     ' Now call the function defined in the script
                                                     ps.AddCommand("Invoke-ProcessSingleFile") _
                                                         .AddParameter("FileName", fileName) _
                                                         .AddParameter("DoRaidSync", doRaidSync) _
                                                         .AddParameter("NonInteractive", True)

                                                     Dim results = ps.Invoke()

                                                     If ps.Streams.Error.Count > 0 Then
                                                         Dim sb As New Text.StringBuilder()
                                                         For Each errRec As ErrorRecord In ps.Streams.Error
                                                             sb.AppendLine(errRec.ToString())
                                                         Next
                                                         Throw New InvalidOperationException("PowerShell execution error: " & sb.ToString())
                                                     End If

                                                     If results IsNot Nothing AndAlso results.Count > 0 Then
                                                         resultObj = results(0)
                                                     Else
                                                         resultObj = Nothing
                                                     End If
                                                 End Using
                                             Catch ex As Exception
                                                 workerException = ex
                                             Finally
                                                 done.Set()
                                             End Try
                                         End Sub

        Dim th As New Thread(threadStart)
        th.SetApartmentState(ApartmentState.STA)
        th.IsBackground = True
        th.Start()

        ' Wait for the script to finish (consider adding timeout if needed)
        done.WaitOne()

        If workerException IsNot Nothing Then
            Throw workerException
        End If

        Return resultObj
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        _disposed = True
        GC.SuppressFinalize(Me)
    End Sub
End Class