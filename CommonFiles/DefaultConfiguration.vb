
Option Strict Off

Imports System.Diagnostics

Public Class DefaultConfiguration

    'This is a display form which allows the user to modify information in the default or user specific configuration file
    '(config.xml or userID.txt).

    'This form can be accessed from a menu selection in the Configuration Environment (GmResidentClient)
    'or will be automatically displayed on initialization if there is no database name entered in the config.xml file

    'This form will also be displayed if the Edit Config File button on the SoftwareVersionSelect form is pressed.

    Private _changesMade As Boolean
    Private Loading As Boolean

    Private Sub ChangeButtonText()

        'called when anything is changed on the DefaultConfiguration page, this changes the text on the
        'exit button to "exit and save".

        If _changesMade = True And Loading = False Then
            Button5.Text = "Exit and Save"
        ElseIf _changesMade = True And Loading = True Then
            _changesMade = False
        End If
    End Sub


    Private Sub SetDisplayValues()

        'Pull configuration values read during ReadConfigFile and put them in the
        'Configuration interface screen display controls

        Loading = True

        Label1.Text = INCADatabase
        TextBox1.Text = INCAWorkspace
        TextBox2.Text = INCAExperiment
        Label5.Text = INCAVariableFile
        TextBox3.Text = RecordWAVTime
        TextBox4.Text = CStr(RecordFileDurationMinutes)
        Label9.Text = ETAS_USER_PATH
        ComboBox2.Text = EnableAltRecReStartAfterRecordStop.ToString
        Label14.Text = BaseDataCollectionPath
        TextBox5.Text = NetworkDriveLetter
        TextBox6.Text = NetworkDriveMapping
        ComboBox1.Text = SignalRegistrationMode

        Loading = False

    End Sub

    Private Sub DefaultConfiguration_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        SetDisplayValues()

        'Set the text on the top of the configuration form.

        If Len(SaveLoginID) > 0 Then
            Me.Text = SaveLoginID & " Configuration (" & SaveLoginID & ".txt)"
        End If

    End Sub

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label1.Click

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        Dim defaultpath As String = "C:\users\public\documents\etas"

        'User inputs the path to the INCA Database here....

        Dim current As Process = Process.GetCurrentProcess()

        If InStr(current.ProcessName, "7_5") > 0 Then
            defaultpath = "C:\users\public\documents\etas\INCA7.5\Database"
        End If

        FolderBrowserDialog1.SelectedPath = defaultpath

        FolderBrowserDialog1.Description = "Please Select INCA Database"
        FolderBrowserDialog1.ShowDialog()

        If Len(FolderBrowserDialog1.SelectedPath) > 0 And FolderBrowserDialog1.SelectedPath <> defaultpath Then
            _changesMade = True
            INCADatabase = FolderBrowserDialog1.SelectedPath
            Label1.Text = INCADatabase
        End If

        ChangeButtonText()

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        'User inputs the signal configuration file name here...

        OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
        OpenFileDialog1.Title = "Please Select Signal Configuration File"
        OpenFileDialog1.FileName = ""
        OpenFileDialog1.ShowDialog()

        If Len(OpenFileDialog1.FileName) > 0 Then
            _changesMade = True
            INCAVariableFile = OpenFileDialog1.FileName
            Label5.Text = INCAVariableFile
        End If

        ChangeButtonText()
    End Sub

    Private Sub TextBox3_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox3.TextChanged

        'User changes the record WAV time here...

        If Len(TextBox3.Text) > 0 Then
            _changesMade = True
            RecordWAVTime = TextBox3.Text
        End If

        ChangeButtonText()

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click

        'User can change the INCA user path here, although this probably won't have to change...

        FolderBrowserDialog1.SelectedPath = ETAS_USER_PATH
        FolderBrowserDialog1.Description = "Please Select INCA User Path"
        FolderBrowserDialog1.ShowDialog()

        If Len(FolderBrowserDialog1.SelectedPath) > 0 Then
            _changesMade = True
            ETAS_USER_PATH = FolderBrowserDialog1.SelectedPath
            Label9.Text = ETAS_USER_PATH
        End If

        ChangeButtonText()
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged

    End Sub

    Private Sub ComboBox1_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedValueChanged
        SignalRegistrationMode = ComboBox1.Text
        _changesMade = True

        ChangeButtonText()
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click

        'User sets the base data collection path here...

        FolderBrowserDialog1.SelectedPath = "C:"
        FolderBrowserDialog1.Description = "Please Select a Base Data Collection Path"
        FolderBrowserDialog1.ShowDialog()

        If Len(FolderBrowserDialog1.SelectedPath) > 0 Then

            If InStr(FolderBrowserDialog1.SelectedPath, " ") > 0 Then
                HandleUserMessageLogging("GMRC", "BaseDataCollectionPath cannot contain spaces, please select a different path...", DisplayMsgBox, )
            Else
                _changesMade = True
                BaseDataCollectionPath = FolderBrowserDialog1.SelectedPath
                Label14.Text = BaseDataCollectionPath
            End If
        End If

        ChangeButtonText()
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click

        'If any changes are made on the DefaultConfiguration form, ChangeButtonText is called which changes button text from Exit to Exit and Save.
        'DialogResult is used to convey whether changes have been made to SoftwareVersionSelect form...

        If Button5.Text = "Exit" Then
            Me.DialogResult = Windows.Forms.DialogResult.Cancel
        ElseIf Button5.Text = "Exit and Save" Then
            Me.DialogResult = Windows.Forms.DialogResult.OK
        End If

        Me.Close()
    End Sub

    Private Sub TextBox5_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox5.TextChanged
        NetworkDriveLetter = TextBox5.Text
        _changesMade = True

        ChangeButtonText()
    End Sub

    Private Sub TextBox4_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox4.TextChanged

        If IsNumeric(TextBox4.Text) And Len(TextBox4.Text) > 0 Then
            _changesMade = True
            RecordFileDurationMinutes = CLng(TextBox4.Text)
        End If

        ChangeButtonText()

    End Sub

    Private Sub Label9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label9.Click

    End Sub

    Private Sub TextBox6_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox6.TextChanged
        NetworkDriveMapping = TextBox6.Text
        _changesMade = True

        ChangeButtonText()
    End Sub

    Private Sub WebBrowser1_DocumentCompleted(ByVal sender As System.Object, ByVal e As System.Windows.Forms.WebBrowserDocumentCompletedEventArgs)

    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        System.Diagnostics.Process.Start(My.Application.Info.DirectoryPath & "\Configuration Help.pdf")
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged
        INCAWorkspace = TextBox1.Text
        _changesMade = True
        SoftwareVersionSelect.TextBox1.Text = INCAWorkspace
        ChangeButtonText()
    End Sub

    Private Sub TextBox2_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox2.TextChanged
        INCAExperiment = TextBox2.Text
        _changesMade = True
        SoftwareVersionSelect.TextBox2.Text = INCAExperiment
        ChangeButtonText()
    End Sub

    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click

    End Sub

    Private Sub Label5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label5.Click

    End Sub

    Private Sub Label5_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label5.TextChanged
        SoftwareVersionSelect.TextBox3.Text = Label5.Text

        _changesMade = True

        ChangeButtonText()
    End Sub

    Private Sub ComboBox2_SelectedValueChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedValueChanged

        EnableAltRecReStartAfterRecordStop = UCase(ComboBox2.Text) = "TRUE"

        _changesMade = True

        ChangeButtonText()
    End Sub
End Class