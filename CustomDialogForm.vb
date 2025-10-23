Public Class CustomDialogForm

    Public Enum DialogResponse
        Option1
        Option2
        Option3
        Option4
        None
    End Enum

    Public ReadOnly Property SelectedOption As DialogResponse
        Get
            Return _selectedOption
        End Get
    End Property
    Private _selectedOption As DialogResponse = DialogResponse.None

    ''' <summary>
    ''' Gets or sets the dialog result.
    ''' </summary>
    Public Property Result As DialogResult

    ''' <summary>
    ''' Initializes a new instance of the <see cref="CustomDialogForm"/> class.
    ''' </summary>
    ''' <param name="errorMessage">The error message to display.</param>
    ''' <param name="title">The title of the dialog.</param>
    ''' <param name="informMessage">The information message used for additional details.</param>
    Public Sub New(errorMessage As String, title As String, informMessage As String)
        ' This call is required by the designer.
        InitializeComponent()

        ' Set the form title and update the message labels.
        Me.Text = title
        lblErrorMessage.Text = errorMessage
        lblInformToolMessage.Text = $"{informMessage} Initialization - "
        continueWithout.Text = $"Selecting 'Continue' will inhibit: {informMessage}"
        exitCLEVIR.Text = "Selecting 'Exit' will Exit CLEVIR"

        ' Hide the generic buttons not used by this constructor
        Button1.Visible = False
        Button2.Visible = False
        Button3.Visible = False
        Button4.Visible = False
    End Sub

    Public Sub New(message As String, title As String, b1Text As String, b2Text As String, b3Text As String, b4Text As String)
        InitializeComponent()

        Me.Text = title
        lblErrorMessage.Text = message
        lblErrorMessage.TextAlign = ContentAlignment.MiddleCenter
        lblErrorMessage.Dock = DockStyle.Fill

        ' Hide labels not used by this generic constructor
        lblInformToolMessage.Visible = False
        continueWithout.Visible = False
        exitCLEVIR.Visible = False
        btnContinue.Visible = False
        btnExit.Visible = False

        ' Configure generic buttons
        ConfigureButton(Button1, b1Text)
        ConfigureButton(Button2, b2Text)
        ConfigureButton(Button3, b3Text)
        ConfigureButton(Button4, b4Text)

        AdjustLayout()
    End Sub

    Private Sub ConfigureButton(btn As Button, text As String)
        If String.IsNullOrEmpty(text) Then
            btn.Visible = False
        Else
            btn.Text = text
            btn.Visible = True
        End If
    End Sub

    Private Sub AdjustLayout()
        Dim visibleButtons As New List(Of Button)
        For Each btn As Button In {Button1, Button2, Button3, Button4}
            If btn.Visible Then
                visibleButtons.Add(btn)
            End If
        Next

        If visibleButtons.Count = 0 Then Return

        Dim totalWidth As Integer = visibleButtons.Sum(Function(b) b.Width) + (visibleButtons.Count - 1) * 10
        Dim startX As Integer = (Me.ClientSize.Width - totalWidth) \ 2

        For Each btn As Button In visibleButtons
            btn.Left = startX
            startX += btn.Width + 10
        Next
    End Sub

    ''' <summary>
    ''' Handles the Click event of the Continue button.
    ''' Sets the dialog result to OK and closes the form.
    ''' </summary>
    Private Sub btnContinue_Click(sender As Object, e As EventArgs) Handles btnContinue.Click
        CloseDialog(DialogResult.OK)
    End Sub

    ''' <summary>
    ''' Handles the Click event of the Exit button.
    ''' Sets the dialog result to Cancel and closes the form.
    ''' </summary>
    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
        CloseDialog(DialogResult.Cancel)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        _selectedOption = DialogResponse.Option1
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        _selectedOption = DialogResponse.Option2
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        _selectedOption = DialogResponse.Option3
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        _selectedOption = DialogResponse.Option4
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    ''' <summary>
    ''' Sets the dialog result, updates the built-in DialogResult property, and closes the form.
    ''' </summary>
    ''' <param name="dialogResult">The dialog result to set.</param>
    Private Sub CloseDialog(dialogResult As DialogResult)
        Result = dialogResult
        Me.DialogResult = dialogResult
        Me.Close()
    End Sub

    ''' <summary>
    ''' Handles the Shown event of the form.
    ''' Brings the form to the front.
    ''' </summary>
    Private Sub CustomDialogForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
            OnVehicleScreen.SendToBack()
        End If
        Me.BringToFront()
        Me.Activate()
    End Sub

End Class