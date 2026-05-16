Public Class RecordPlayback

    'This form is used to record and play back application session data.  It is used primarally for 
    'debug and demos.  This form allows the user to record all of the variables visualized in CLEVIR.  This
    '"LIVE" data is saved in a comma delimited file format such that it can be played back using CLEVIR.
    'This recording is independent of the INCA mf4 file recording.   Recording data in this manner
    'allows the user to debug top down view visualization, for example.

    Public Enum PlaybackStates
        PlaybackStop = 0
        PlaybackRun
        PlaybackPause
        PlaybackStepFwd
        PlaybackStepBack
        PlaybackReset
        PlaybackScrolling
    End Enum

    Public PlaybackMode As PlaybackStates
    Public PlayBackFileName As String
    Public RecordMode As Boolean

    Private _mouseOnScrollBar As Boolean
    Private _scrolling As Boolean

    Private Sub Record_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Record.Click
        RecordMode = True
    End Sub

    Private Sub StopButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles StopButton.Click

        GroupBox2.Text = "PLAYBACK (Stop)"


    End Sub

    Private Sub PauseButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        GroupBox2.Text = "PLAYBACK (Pause)"

    End Sub

    Private Sub PlayPauseButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PlayPauseButton.Click

        If GroupBox2.Text = "PLAYBACK (Play)" Then
            GroupBox2.Text = "PLAYBACK (Pause)"
        Else
            GroupBox2.Text = "PLAYBACK (Play)"
        End If

    End Sub

    Private Sub StepForward_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles StepForward.Click

        GroupBox2.Text = "PLAYBACK (Step Fwd)"

    End Sub

    Private Sub StepBack_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles StepBack.Click

        GroupBox2.Text = "PLAYBACK (Step Back)"


    End Sub

    Private Sub Reset_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Reset.Click

        GroupBox2.Text = "PLAYBACK (Reset)"

    End Sub

    Private Sub SelectFile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SelectFile.Click

        GroupBox2.Text = "PLAYBACK (Select File)"

    End Sub

    Private Sub StopRecord_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles StopRecord.Click
        RecordMode = False
    End Sub

    Private Sub HScrollBar1_MouseEnter(ByVal sender As Object, ByVal e As System.EventArgs) Handles HScrollBar1.MouseEnter
        _mouseOnScrollBar = True
    End Sub

    Private Sub HScrollBar1_MouseHover(ByVal sender As Object, ByVal e As System.EventArgs) Handles HScrollBar1.MouseHover

    End Sub

    Private Sub HScrollBar1_MouseLeave(ByVal sender As Object, ByVal e As System.EventArgs) Handles HScrollBar1.MouseLeave

        _mouseOnScrollBar = False

        If PlaybackMode <> PlaybackStates.PlaybackRun Then
            PlaybackMode = PlaybackStates.PlaybackPause

            If GmResidentClient.SaveLineNumber > 1 Then
                StepBack.Enabled = True
            End If
            If GmResidentClient.SaveLineNumber < HScrollBar1.Maximum Then
                StepForward.Enabled = True
            End If
        End If

    End Sub

    Private Sub HScrollBar1_Move(ByVal sender As Object, ByVal e As System.EventArgs) Handles HScrollBar1.Move

    End Sub

    Private Sub HScrollBar1_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar1.Scroll
        If _mouseOnScrollBar = True Then
            _scrolling = True
            GmResidentClient.SaveLineNumber = HScrollBar1.Value
            Label1.Text = GmResidentClient.SaveLineNumber & " of " & HScrollBar1.Maximum
            PlaybackMode = PlaybackStates.PlaybackScrolling
        End If
    End Sub

    Private Sub HScrollBar1_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles HScrollBar1.ValueChanged

    End Sub

    Private Sub GroupBox2_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles GroupBox2.TextChanged

        'The textchanged event will handle the playback button presses and any other 
        'instance where the playbackmode is changed in code.  This way, we can have
        'one place to handle all things associated with the change of state of the
        'playback, either by key presses or by status changes in code.

        Dim noLabelTextDisplay As Boolean

        Select Case GroupBox2.Text
            Case "PLAYBACK (Play)"

                PlaybackMode = PlaybackStates.PlaybackRun
                StopButton.Enabled = True
                StepForward.Enabled = False
                StepBack.Enabled = False

                'PlayPauseButton.Image = My.Resources.Resources.Oxygen_Icons_org_Oxygen_Actions_media_playback_pause_1_

            Case "PLAYBACK (Step Back)"

                PlaybackMode = PlaybackStates.PlaybackStepBack
                StepForward.Enabled = True

            Case "PLAYBACK (Pause)"

                PlaybackMode = PlaybackStates.PlaybackPause
                StepForward.Enabled = True
                StepBack.Enabled = True
                Reset.Enabled = True

                'PlayPauseButton.Image = My.Resources.Resources.Oxygen_Icons_org_Oxygen_Actions_media_playback_start_1_

            Case "PLAYBACK (Step Fwd)"

                PlaybackMode = PlaybackStates.PlaybackStepFwd
                StepBack.Enabled = True
                Reset.Enabled = True

            Case "PLAYBACK (Reset)"

                PlaybackMode = PlaybackStates.PlaybackStop
                Me.HScrollBar1.Value = 1
                PlayPauseButton.Enabled = True
                Me.StopButton.Enabled = False
                StepForward.Enabled = True
                StepBack.Enabled = False
                If GmResidentClient.SaveLineNumber = 0 Then
                    noLabelTextDisplay = True
                End If

                GmResidentClient.SaveLineNumber = 1
                If noLabelTextDisplay = False Then
                    Label1.Text = GmResidentClient.SaveLineNumber & " of " & HScrollBar1.Maximum
                End If

                'PlayPauseButton.Image = My.Resources.Resources.Oxygen_Icons_org_Oxygen_Actions_media_playback_start_1_


            Case "PLAYBACK (Stop)", "PLAYBACK"

                'PlayPauseButton.Image = My.Resources.Resources.Oxygen_Icons_org_Oxygen_Actions_media_playback_start_1_
                PlaybackMode = PlaybackStates.PlaybackStop
                StopButton.Enabled = False

            Case "PLAYBACK (Select File)"

                OpenFileDialog1.InitialDirectory = My.Application.Info.DirectoryPath
                OpenFileDialog1.Filter = "csv | *.csv"
                OpenFileDialog1.DefaultExt = "csv"
                'OpenFileDialog1.FileName = My.Application.Info.DirectoryPath & "\" & Mid(GmResidentClient.INCAVariableFile, 1, InStr(GmResidentClient.INCAVariableFile, ".") - 1) & ".csv"
                OpenFileDialog1.FileName = Mid(INCAVariableFile, 1, InStr(INCAVariableFile, ".") - 1) & "_RecordFile.csv"


                If OpenFileDialog1.ShowDialog() = DialogResult.OK Then

                    If Len(OpenFileDialog1.FileName) > 0 Then

                        ReadNewDataFile = True

                        PlayBackFileName = OpenFileDialog1.FileName

                        GroupBox2.Text = "PLAYBACK (Reset)"
                        HScrollBar1.Enabled = True

                    Else
                        MsgBox("You must select a valid Playback File to continue.")
                    End If
                Else
                    MsgBox("You must select a valid Playback File to continue.")
                End If

        End Select

    End Sub

    Private Sub GroupBox2_Enter(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GroupBox2.Enter

    End Sub

    Private Sub StepBack_EnabledChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles StepBack.EnabledChanged
        If GmResidentClient.SaveLineNumber = 1 Then
            StepBack.Enabled = False
        End If
    End Sub

    Private Sub StepForward_EnabledChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles StepForward.EnabledChanged
        If GmResidentClient.SaveLineNumber = HScrollBar1.Maximum Then
            StepForward.Enabled = False
        End If
    End Sub

    Private Sub RecordPlayback_Load(sender As Object, e As EventArgs) Handles Me.Load

        If OperatingMode <> OperatingModes.ResOnVpc Then
            Me.Top = GmResidentClient.Top
            Me.Left = GmResidentClient.Left + GmResidentClient.Width
        Else
            Me.Top = GmResidentClient.Top
            Me.Left = GmResidentClient.Left
        End If

        Me.BringToFront()
        Me.Activate()
    End Sub
End Class