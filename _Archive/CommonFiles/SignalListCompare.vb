
Option Strict Off
Option Explicit On

Imports VB = Microsoft.VisualBasic

Imports Microsoft
Imports Microsoft.Office
Imports Microsoft.Office.Interop

Public Class SignalListCompare

    'This is a form used to perform compares between signal lists.  This is an activity for the developer and is not
    'accessible to the user. In order to access this functionality, there is a button (Compare Signal Lists) on the 
    'InitForm that must be made visible from the design environment...

    Private Sub CompareSignalLists()

        Dim excelApp As Excel.Application
        Dim wrkbk_1 As Excel.Workbook
        Dim wrkbk_2 As Excel.Workbook
        Dim myWorkSheet_1 As Excel.Worksheet
        Dim myWorkSheet_2 As Excel.Worksheet

        Dim varSheetA As Object
        Dim varSheetB As Object
        Dim iRow As Long

        Dim iRowJump_A As Long
        Dim iRowJump_B As Long

        Dim maxCol As Long

        Dim x As Short

        Dim A_MaxCol As Short
        Dim B_MaxCol As Short

        Dim A_MaxRow As Short
        Dim B_MaxRow As Short

        Dim FoundTotal As Boolean
        Dim FoundMatch As Boolean

        Dim filename As String
        Dim fnum As Long
        Dim textline() As String

        On Error GoTo handleit

        If Me.Label1.Text <> Me.Label2.Text Then

            iRowJump_A = 0
            iRowJump_B = 0

            Me.Cursor = Cursors.WaitCursor

            excelApp = New Excel.Application  'launch the excel app

            excelApp.Visible = True

            wrkbk_1 = excelApp.Workbooks.Open(Me.Label1.Text) 'This is the A file - compare newest version A to previous version B
            wrkbk_2 = excelApp.Workbooks.Open(Me.Label2.Text) 'This is the B file - B is compared to A

            'create worksheet object and establish the maximum range within which to work
            myWorkSheet_1 = wrkbk_1.Sheets(1)
            Dim NameFind_1 As Excel.Range = myWorkSheet_1.Range("$A$1", "$W$20000")
            myWorkSheet_2 = wrkbk_2.Sheets(1)
            Dim NameFind_2 As Excel.Range = myWorkSheet_2.Range("$A$1", "$W$20000")

            Dim currentfind_F As Excel.Range
            Dim currentfind_B As Excel.Range
            Dim findstring As String = ""


            'determine the actual range of data contained in each spreadsheet
            varSheetA = myWorkSheet_1.UsedRange.Value
            varSheetB = myWorkSheet_2.UsedRange.Value

            'maxRow is whichever spreadsheet has the most rows.

            A_MaxRow = UBound(varSheetA, 1)
            B_MaxRow = UBound(varSheetB, 1)

            'set MaxCol for each spreadsheet

            A_MaxCol = UBound(varSheetA, 2)
            B_MaxCol = UBound(varSheetB, 2)

            'just a print statement for reference

            If UBound(varSheetA, 2) > UBound(varSheetB, 2) Then
                maxCol = UBound(varSheetA, 2)
                Me.List1.Items.Add("A has more cols than B")
            ElseIf UBound(varSheetA, 2) < UBound(varSheetB, 2) Then
                maxCol = UBound(varSheetB, 2)
                Me.List1.Items.Add("B has more cols than A")
            Else
                maxCol = UBound(varSheetA, 2)
                Me.List1.Items.Add("Same number of columns in A and B")
            End If

            For iRow = 2 To A_MaxRow

                findstring = CStr(varSheetA(iRow, 1))
                currentfind_F = NameFind_2.Find(findstring, , Excel.XlFindLookIn.xlValues, Excel.XlLookAt.xlWhole, Excel.XlSearchOrder.xlByRows, Excel.XlSearchDirection.xlNext, False)

                If currentfind_F Is Nothing Then

                    List1.Items.Add("A not Found in B " & varSheetA(iRow, 1))
                    List1.SelectedIndex = List1.Items.Count - 1
                    List1.Refresh()

                Else
                    Dim A_Content As String = ""
                    Dim B_Content As String = ""

                    For x = 2 To maxCol
                        If varSheetA(iRow, x) <> varSheetB(currentfind_F.Row, x) Then
                            A_Content = A_Content & "-" & varSheetA(iRow, x)
                            B_Content = B_Content & "-" & varSheetB(currentfind_F.Row, x)
                        End If
                    Next

                    If Len(A_Content) > 0 Then
                        List1.Items.Add(" ")
                        List1.Items.Add(varSheetA(iRow, 1) & ": " & A_Content)
                        List1.Items.Add(varSheetB(currentfind_F.Row, 1) & ": " & B_Content)
                        List1.SelectedIndex = List1.Items.Count - 1
                        List1.Refresh()
                    End If

                End If

            Next

            For iRow = 2 To B_MaxRow

                findstring = CStr(varSheetB(iRow, 1))
                currentfind_F = NameFind_1.Find(findstring, , Excel.XlFindLookIn.xlValues, Excel.XlLookAt.xlWhole, Excel.XlSearchOrder.xlByRows, Excel.XlSearchDirection.xlNext, False)

                If currentfind_F Is Nothing Then

                    List1.Items.Add("B not Found in A " & varSheetA(iRow, 1))
                    List1.SelectedIndex = List1.Items.Count - 1
                    List1.Refresh()

                Else

                End If

            Next

            filename = TextBox1.Text
            fnum = FreeFile()

            FileOpen(fnum, filename, OpenMode.Output)

            For x = 0 To List1.Items.Count - 1
                PrintLine(fnum, List1.Items(x).ToString)
            Next

            FileClose(fnum)

            MsgBox("done")

        End If

        Exit Sub

handleit:
        MsgBox("CompareSignalLists: " & Err.Number & " - " & Err.Description)

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        CompareSignalLists()
    End Sub

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label1.Click

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'Select Base Signal List

        Me.OpenFileDialog1.FileName = ""
        Me.OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath & "\SignalLists"
        Me.OpenFileDialog1.ShowDialog()

        Me.Label1.Text = Me.OpenFileDialog1.FileName

        If Len(Me.TextBox1.Text) > 0 And Len(Me.Label2.Text) > 0 And Len(Me.Label1.Text) > 0 Then
            Me.Button3.Enabled = True
        Else
            Me.Button3.Enabled = False
        End If

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        'Select Compare Signal List

        Me.OpenFileDialog1.FileName = ""
        Me.OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath & "\SignalLists"
        Me.OpenFileDialog1.ShowDialog()

        Me.Label2.Text = Me.OpenFileDialog1.FileName

        If Len(Me.TextBox1.Text) > 0 And Len(Me.Label2.Text) > 0 And Len(Me.Label1.Text) > 0 Then
            Me.Button3.Enabled = True
        Else
            Me.Button3.Enabled = False
        End If


    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Form3_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed

    End Sub

    Private Sub Form3_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged
        If Len(TextBox1.Text) = 0 Then
            Button3.Enabled = False
        ElseIf Len(Me.Label2.Text) > 0 And Len(Me.Label1.Text) > 0 Then
            Button3.Enabled = True
        End If
    End Sub

    Private Sub List1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles List1.SelectedIndexChanged

    End Sub
End Class