Public Class CustomDialogForm

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
        OnVehicleScreen.SendToBack()
        Me.BringToFront()
        Me.Activate()
    End Sub

End Class