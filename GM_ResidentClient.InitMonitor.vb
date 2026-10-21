Option Strict Off
Option Explicit On

Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Speech.Synthesis
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Forms.DataVisualization.Charting
Imports System.Xml
Imports SharpPcap

Partial Public Class GmResidentClient

    Private Async Sub InitializationMonitor()
        Dim MyForm As Form = Nothing
        Dim MyList As ListBox = Nothing
        Dim MyProgressBar As ProgressBar = Nothing
        Dim MyHeaderLabel As Label = Nothing
        Dim MyFont As Font = Nothing
        Dim startTime As DateTime = DateTime.Now
        Dim saveString As String = ""
        Dim finalStatusShown As Boolean = False

        Try
            HandleUserMessageLogging("GMRC", "InitializationMonitor: Starting Initialization...")

            While Not StopTestProcess
                ' ✅ FIX: Skip UI creation if suppressed during signal registration
                If _suppressInitMonitorUI Then
                    Await Task.Delay(100)  ' Just wait quietly
                    Continue While  ' Skip the rest of the loop
                End If

                If ProgressBarEnable Then
                    If MyForm Is Nothing Then
                        ' ✅ SIMPLIFIED: Already on UI thread, just call directly
                        InitMon_CreateUi(MyForm, MyList, MyProgressBar, MyHeaderLabel, MyFont)
                    Else
                        ' ✅ SIMPLIFIED: Update UI directly
                        Dim currentStatus() As String = StatusString
                        InitMon_UpdateProgress(currentStatus, MyProgressBar, MyList, MyFont, saveString)

                        ' Non-blocking delay
                        Await Task.Delay(100)
                    End If
                Else
                    If MyForm IsNot Nothing AndAlso Not finalStatusShown Then
                        finalStatusShown = True
                        InitMon_ShowFinalSummary(MyList, MyProgressBar, startTime)
                        StopTestProcess = True
                        Exit While
                    End If

                    Await Task.Delay(InitMonitorSleepTime)
                End If
            End While

            ' Wait for user click if needed
            While _keepListBoxDisplayed
                Await Task.Delay(100)
            End While

            OnVehicleScreen.ShowInTaskbar = True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "InitializationMonitor: " & ex.Message, DisplayMsgBox)
        End Try
    End Sub

    ' =========================
    ' InitializationMonitor helpers
    ' =========================

    Private Sub InitMon_CreateUi(ByRef MyForm As Form,
                         ByRef MyList As ListBox,
                         ByRef MyProgressBar As ProgressBar,  ' Keep as ProgressBar type (base class)
                         ByRef MyHeaderLabel As Label,
                         ByRef MyFont As Font)
        Try
            ' Ensure we're on UI thread
            If Me.InvokeRequired Then
                Throw New InvalidOperationException("InitMon_CreateUi must be called on UI thread")
            End If
            ' Determine progress max for FULL registration
            If InStr(UCase(SignalRegistrationMode), "FULL") > 0 Then
                _progressBarMax = UBound(MyIncaInterface.MySignals) + 1
            End If

            MyForm = New Form With {
        .FormBorderStyle = FormBorderStyle.None,
        .Height = 565,
        .Width = 800,
        .StartPosition = FormStartPosition.Manual,
        .Top = 0,
        .Left = 0,
        .BackColor = Color.WhiteSmoke,
        .Visible = True,
        .ShowInTaskbar = True
    }

            MyList = New ListBox With {
        .Parent = MyForm,
        .Visible = True,
        .Height = 180,
        .Top = 340,
        .Width = MyForm.Width - 10,
        .Left = 5
    }
            AddHandler MyList.Click, AddressOf Mylist_Click

            MyHeaderLabel = New Label With {
        .Parent = MyForm,
        .Height = 73,
        .Width = 800,
        .BackColor = MyForm.BackColor,
        .Top = 274,
        .Left = 20,
        .TextAlign = ContentAlignment.MiddleCenter,
        .Visible = True
    }

            Select Case UCase(SignalRegistrationMode)
                Case "FULL" : MyHeaderLabel.Text = "Performing FULL Signal Registration..."
                Case "DISPLAYS" : MyHeaderLabel.Text = "Registering DISPLAYS and GO/NOGO Signals..."
                Case "GO/NOGO" : MyHeaderLabel.Text = "Registering GO/NOGO Signals ONLY..."
                Case "NEW FULL" : MyHeaderLabel.Text = "Adding ALL Signals to NEW Blank Experiment (" & INCAExperiment & ")..."
                Case Else : MyHeaderLabel.Text = "Registering Signals..."
            End Select

            MyHeaderLabel.Font = New Font(MyHeaderLabel.Font.FontFamily, 14, FontStyle.Bold)

            ' ***** USE YOUR CUSTOM PROGRESS BAR HERE *****
            MyProgressBar = New PercentageProgressBar With {
        .Parent = MyForm,
        .Visible = True,
        .Maximum = Math.Max(1, _progressBarMax),
        .Height = 25,
        .Left = 5,
        .BackColor = Color.LightGray  ' Background color for unprogressed area
    }
            MyProgressBar.Top = (MyForm.Height - MyProgressBar.Height) - 10
            MyProgressBar.Width = MyForm.Width - 10
            MyProgressBar.Value = 0

            ' ***** NO NEED FOR GRAPHICS OR FONT ANYMORE *****
            ' The PercentageProgressBar handles its own painting
            MyFont = Nothing  ' Not needed anymore

            MyForm.TopMost = True
            ' Force form to display
            MyForm.Show()
            MyForm.BringToFront()
            MyForm.Activate()
            Application.DoEvents() ' Force immediate paint

            HandleUserMessageLogging("GMRC", "InitMon_CreateUi: Form created and displayed successfully")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"InitMon_CreateUi failed: {ex.Message}", DisplayMsgBox)
            Throw
        End Try
    End Sub

    Private Sub InitMon_UpdateProgress(ByVal currentStatus() As String,
                               ByRef MyProgressBar As ProgressBar,
                               ByVal MyList As ListBox,
                               ByVal MyFont As Font,  ' Can remove this parameter eventually
                               ByRef saveString As String)

        If currentStatus Is Nothing OrElse currentStatus.Length = 0 Then
            Exit Sub
        End If

        Dim last As String = currentStatus(UBound(currentStatus))
        Dim progressValue As Integer
        Dim progressSlice As String = If(last.Length >= 12, last.Substring(11).Trim(), String.Empty)

        If Integer.TryParse(progressSlice, progressValue) Then
            ' Progress update
            progressValue = Math.Max(0, Math.Min(progressValue, MyProgressBar.Maximum))
            If MyProgressBar.Value <> progressValue Then
                MyList.Items.Add("Processing " & progressValue & " of " & MyProgressBar.Maximum)
                MyList.SelectedIndex = MyList.Items.Count - 1
            End If

            ' Simply set the value - the PercentageProgressBar will repaint itself automatically
            MyProgressBar.Value = progressValue
            ' No need for manual graphics drawing or Refresh() call

        Else
            ' Status line update (no changes needed here)
            If saveString <> last Then
                MyList.Items.Add(last)
                MyList.SelectedIndex = MyList.Items.Count - 1
                MyList.Refresh()

                saveString = last

                If InStr(saveString, ">>> Inca <<<") > 0 Or InStr(saveString, "- INVALID -") > 0 Then
                    If _keepListBoxDisplayed = False Then
                        HandleUserMessageLogging("GMRC", "InitializationMonitor: First Signal Registration Error - " & saveString)
                        _keepListBoxDisplayed = True
                    End If

                    If InStr(UCase(saveString), "WINDOWS ERROR") > 0 Then
                        StopTestProcess = True
                        Exit Sub
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub InitMon_ShowFinalSummary(ByVal MyList As ListBox,
                                         ByVal MyProgressBar As ProgressBar,
                                         ByVal startTime As DateTime)

        MyProgressBar.Value = 0
        MyList.Items.Clear()
        MyList.Items.Add(Format(startTime, "HH:mm:ss") & " - Initialization Start Time")

        Dim allStatus() As String = StatusString
        If allStatus IsNot Nothing AndAlso allStatus.Length > 0 Then
            For i As Integer = 0 To UBound(allStatus)
                If InStr(allStatus(i), "Register Signals -") > 0 Then
                    MyList.Items.Add(allStatus(i))
                    MyList.SelectedIndex = MyList.Items.Count - 1
                End If
            Next
        End If

        MyList.Items.Add(Format(DateTime.Now, "HH:mm:ss") & " - CLEVIR Init End Time")

        If _keepListBoxDisplayed = True Then
            MyList.Items.Add("")
            MyList.Items.Add("")
            MyList.Items.Add("THERE WERE SIGNAL REGISTRATION ERRORS!  SCROLL BAR to view, CLICK HERE to CONTINUE.")
            MyList.Items.Add("The invalid signal list is in the " & My.Application.Info.DirectoryPath & " \InvalidSignalsLog.csv file.")
            MyList.Items.Add("")
            MyList.Items.Add("")
            MyList.SelectedIndex = MyList.Items.Count - 1
        Else
            ' No errors - dispose of the form after a brief delay to show completion
            Dim formToDispose As Form = MyList.FindForm()
            If formToDispose IsNot Nothing Then
                ' Show completion message briefly
                Thread.Sleep(2000)
                ' Clean up the form
                formToDispose.Close()
                formToDispose.Dispose()
            End If
        End If
    End Sub

End Class
