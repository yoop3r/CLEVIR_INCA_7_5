Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>A visible top-level window owned by a watched process.</summary>
Public NotInheritable Class ProbedWindow
    Public Sub New(handle As IntPtr, title As String, text As String)
        Me.Handle = handle
        Me.Title = title
        Me.Text = text
    End Sub

    Public ReadOnly Property Handle As IntPtr
    Public ReadOnly Property Title As String
    ''' <summary>Text of the window's static (label) children, e.g. a message box body.</summary>
    Public ReadOnly Property Text As String
End Class

''' <summary>
''' Finds visible windows of INCA's Target Server (TGTSVR). In normal operation it shows none (only a hidden main
''' window), so a visible one is a message waiting for an answer - e.g. lost communication with a camera - and INCA
''' stays blocked until the driver presses OK.
''' </summary>
Public NotInheritable Class TargetServerWindowProbe

    Public Const TargetServerProcessName As String = "TGTSVR"
    Private Const MaxTextLength As Integer = 400

    Private Sub New()
    End Sub

    Public Shared Function FindVisibleWindows(Optional processName As String = TargetServerProcessName) As List(Of ProbedWindow)
        Dim ids As New HashSet(Of Integer)
        For Each p In Process.GetProcessesByName(processName)
            Using p
                ids.Add(p.Id)
            End Using
        Next
        If ids.Count = 0 Then Return New List(Of ProbedWindow)
        Return FindVisibleWindows(ids)
    End Function

    Public Shared Function FindVisibleWindows(processIds As ICollection(Of Integer)) As List(Of ProbedWindow)
        Dim found As New List(Of ProbedWindow)
        Dim callback As EnumWindowsProc =
            Function(hWnd As IntPtr, lParam As IntPtr) As Boolean
                Dim pid As UInteger
                GetWindowThreadProcessId(hWnd, pid)
                If processIds.Contains(CInt(pid)) AndAlso IsWindowVisible(hWnd) AndAlso HasArea(hWnd) Then
                    found.Add(New ProbedWindow(hWnd, GetText(hWnd), GetStaticChildText(hWnd)))
                End If
                Return True
            End Function
        EnumWindows(callback, IntPtr.Zero)
        GC.KeepAlive(callback)
        Return found
    End Function

    ''' <summary>Raises the window above CLEVIR's top-most screens so the driver can answer it.</summary>
    Public Shared Sub BringToFront(hWnd As IntPtr)
        If IsIconic(hWnd) Then ShowWindow(hWnd, SW_RESTORE)
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_SHOWWINDOW)
        SetForegroundWindow(hWnd)
    End Sub

    Private Shared Function HasArea(hWnd As IntPtr) As Boolean
        Dim r As RECT
        Return GetWindowRect(hWnd, r) AndAlso r.Right > r.Left AndAlso r.Bottom > r.Top
    End Function

    Private Shared Function GetText(hWnd As IntPtr) As String
        Dim sb As New StringBuilder(MaxTextLength + 1)
        GetWindowText(hWnd, sb, sb.Capacity)
        Return sb.ToString().Trim()
    End Function

    Private Shared Function GetClass(hWnd As IntPtr) As String
        Dim sb As New StringBuilder(256)
        GetClassName(hWnd, sb, sb.Capacity)
        Return sb.ToString()
    End Function

    Private Shared Function GetStaticChildText(parent As IntPtr) As String
        Dim parts As New List(Of String)
        Dim callback As EnumWindowsProc =
            Function(hWnd As IntPtr, lParam As IntPtr) As Boolean
                If IsWindowVisible(hWnd) AndAlso GetClass(hWnd).Equals("Static", StringComparison.OrdinalIgnoreCase) Then
                    Dim t = GetText(hWnd)
                    If t.Length > 0 Then parts.Add(t)
                End If
                Return True
            End Function
        EnumChildWindows(parent, callback, IntPtr.Zero)
        GC.KeepAlive(callback)
        Dim joined = String.Join(" ", parts).Replace(vbCr, " ").Replace(vbLf, " ")
        Return If(joined.Length > MaxTextLength, joined.Substring(0, MaxTextLength), joined)
    End Function

    Private Delegate Function EnumWindowsProc(hWnd As IntPtr, lParam As IntPtr) As Boolean

    <StructLayout(LayoutKind.Sequential)>
    Private Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const SWP_NOSIZE As UInteger = &H1UI
    Private Const SWP_NOMOVE As UInteger = &H2UI
    Private Const SWP_SHOWWINDOW As UInteger = &H40UI
    Private Const SW_RESTORE As Integer = 9

    <DllImport("user32.dll")>
    Private Shared Function EnumWindows(callback As EnumWindowsProc, lParam As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function EnumChildWindows(parent As IntPtr, callback As EnumWindowsProc, lParam As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef processId As UInteger) As UInteger
    End Function

    <DllImport("user32.dll")>
    Private Shared Function IsWindowVisible(hWnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function IsIconic(hWnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowRect(hWnd As IntPtr, ByRef rect As RECT) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function GetWindowText(hWnd As IntPtr, text As StringBuilder, maxCount As Integer) As Integer
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function GetClassName(hWnd As IntPtr, className As StringBuilder, maxCount As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function ShowWindow(hWnd As IntPtr, cmd As Integer) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, insertAfter As IntPtr, x As Integer, y As Integer, cx As Integer, cy As Integer, flags As UInteger) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetForegroundWindow(hWnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

End Class
