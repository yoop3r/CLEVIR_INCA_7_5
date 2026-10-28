Public Class Cusmsgbox
    ' Per instance: a modeless alert can be open while another box is shown modally.
    Private _selectedOption As Integer

    ''' <summary>The button the user clicked (1-4), or 0 if the box was closed without a button.</summary>
    Public ReadOnly Property SelectedOption As Integer
        Get
            Return _selectedOption
        End Get
    End Property

    Public Function DisplayCusMsgBox(
                                     ByVal owner As IWin32Window,
                                     ByVal message As String,
                                     ByVal title As String,
                                     ByVal btn1 As String,
                                     Optional ByVal btn2 As String = "",
                                     Optional ByVal btn3 As String = "",
                                     Optional ByVal btn4 As String = ""
                                     ) As Integer

        _selectedOption = 0
        SetContent(message, title, btn1, btn2, btn3, btn4)

        ' Ensure the dialog is displayed in front of a TopMost owner (e.g. OnVehicleScreen)
        Me.TopMost = True

        'Show the form as a modal dialog with an explicit owner
        Me.ShowDialog(owner)

        Return _selectedOption
    End Function

    ''' <summary>
    ''' Shows the box without blocking the caller. The form closes (and is disposed) when a button is
    ''' clicked; read <see cref="SelectedOption"/> from a FormClosed handler.
    ''' </summary>
    Public Sub ShowModeless(ByVal owner As IWin32Window,
                            ByVal message As String,
                            ByVal title As String,
                            ByVal btn1 As String,
                            Optional ByVal btn2 As String = "",
                            Optional ByVal btn3 As String = "",
                            Optional ByVal btn4 As String = "")
        _selectedOption = 0
        SetContent(message, title, btn1, btn2, btn3, btn4)
        Me.TopMost = True
        Me.Show(owner)
    End Sub

    ''' <summary>Sets the message, title and buttons; also used to update a box that is already showing.</summary>
    Public Sub SetContent(ByVal message As String,
                          ByVal title As String,
                          ByVal btn1 As String,
                          Optional ByVal btn2 As String = "",
                          Optional ByVal btn3 As String = "",
                          Optional ByVal btn4 As String = "")
        Label1.Text = message
        Text = title

        Button1.Text = btn1
        Button2.Text = btn2
        Button3.Text = btn3
        Button4.Text = btn4

        Button1.Visible = (btn1 <> "")
        Button2.Visible = (btn2 <> "")
        Button3.Visible = (btn3 <> "")
        Button4.Visible = (btn4 <> "")
    End Sub

    ''' <summary>Text of button 1-4, or "" for any other value.</summary>
    Public Function ButtonText(ByVal buttonNumber As Integer) As String
        Select Case buttonNumber
            Case 1 : Return Button1.Text
            Case 2 : Return Button2.Text
            Case 3 : Return Button3.Text
            Case 4 : Return Button4.Text
            Case Else : Return ""
        End Select
    End Function

    Private Sub Cusmsgbox_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Me.TopMost = True
        Me.BringToFront()
        Me.Activate()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        _selectedOption = 1
        Me.Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        _selectedOption = 2
        Me.Close()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        _selectedOption = 3
        Me.Close()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        _selectedOption = 4
        Me.Close()
    End Sub
End Class
