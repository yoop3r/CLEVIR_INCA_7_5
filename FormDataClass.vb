
Public Class FormDataClass

    'This is the class which is used to define all of the user defined display forms.
    'We inherit the form class and add a few things specific to our needs....

    'Information about the user defined display forms comes from the INCAVariableFile Excel spreadsheet (or .csv file)
    'which is read in during initialization...

    Inherits Form

    Public Const MAX_NUM_FORMS As Integer = 50
    Public myContextMenuStrip As ContextMenuStrip
    Public Property SignalsRegistered As Boolean
    Public Property DisplayWindowSize As String
    Public Property AlsoAssociatedWith As String
    Public Property GoNoGoIndex As Integer
    Public Property DefaultHeight As Integer
    Public Property DefaultWidth As Integer

    Private Sub FormDataClass_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated

    End Sub

    Private Sub FormDataClass_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Click


    End Sub

    Private Sub FormDataClass_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.DoubleClick

    End Sub

    Private Sub FormDataClass_EnabledChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.EnabledChanged

    End Sub

    Private Sub FormDataClass_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        'We will hide the form rather than close it, so it is retained in memory for the duration of the user session....

        Me.Hide()
        e.Cancel = True
    End Sub

    Private Sub FormDataClass_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub
    Private Sub InitializeComponent()
        Me.SuspendLayout()
        '
        'FormDataClass
        '
        Me.ClientSize = New System.Drawing.Size(579, 527)
        Me.Name = "FormDataClass"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.ResumeLayout(False)

    End Sub

    Private Sub FormDataClass_MouseClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseClick

    End Sub

    Private Sub FormDataClass_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDown

        'An alternative method for creating a new grid.  There are also other form configuration related selections available from this
        'context menu which is made visible on a right mouse click.

        If e.Button = MouseButtons.Right Then

            GmResidentClient.FormForGridAdd = Me.Name

            myContextMenuStrip.Show(MousePosition)

        End If

    End Sub

    Private Sub FormDataClass_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize

        'When we resize the form, we need to save the Width and Height so it retains these values for subsequent user sessions....
        'We save in a specific string format representation  which is W (width) (value), H (Height) (value), so a width and height
        'of 400 becomes "W400 H400", this is the string that is saved in the spreadsheet, and parsed on initialization the next
        'time we start up....

        Dim x As Integer

        If FormDisplayed = True Then
            'Me.DisplayWindowSize = "W" & Me.Width & ",H" & Me.Height
            Me.DisplayWindowSize = "W" & Me.Width & " H" & Me.Height
            GridCellPropConfig._changesMade = True

            For x = 0 To GmResidentClient.MyDFs.Count - 1
                If GmResidentClient.MyDFs(x).Name = Me.Text Then
                    GmResidentClient.MyExitButtons(x).Left = GmResidentClient.MyDFs(x).Width - GmResidentClient.MyExitButtons(x).Width - 20
                    Exit For
                End If
            Next

        End If

    End Sub

    Private Sub FormDataClass_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown

    End Sub
End Class
