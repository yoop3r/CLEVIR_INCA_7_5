
Option Strict Off
Option Explicit On

Imports System.Speech
Imports System.Speech.Recognition
Imports System.Speech.Synthesis
Public Class LmfrStatusScreenHc

    'This is the LmfrStatusScreenHc custom screen...

    Private Sub PositionTopDown()

        GmResidentClient.MyTdGraphicsContainer.Height = Me.Height - 180
        GmResidentClient.MyTdGraphicsContainer.Width = Me.Width - (Me.Label5.Left + Me.Label5.Width) - 20

        GmResidentClient.MyTdGraphicsContainer.Left = (Me.Left + Me.Width) - GmResidentClient.MyTdGraphicsContainer.Width - 10

        GmResidentClient.MyTdGraphicsContainer.Top = Me.Top + 150

    End Sub
    Private Sub LmfrStatusScreenHc_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Top = 0
        Me.Left = 0

        GmResidentClient.MyTdGraphicsContainer.ControlBox = False
        GmResidentClient.MyTdGraphicsContainer.Show()
        GmResidentClient.MyTdGraphicsContainer.TopMost = True
        Me.Width = GmResidentClient.DefaultFormWidth800

        GmResidentClient.MyTdGraphicsContainer.myExitButton.Visible = False
    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub LmfrStatusScreenHc_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        If GmResidentClient.MyTdGraphicsContainer.TopMost = False Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = True
            GmResidentClient.MyTdGraphicsContainer.Show()
        End If

        PositionTopDown()

        If MyIncaInterface.Recording = True Then
            Button1.Enabled = True
        Else
            Button1.Enabled = False
        End If

    End Sub

    Private Sub LmfrStatusScreenHc_MouseUp(sender As Object, e As MouseEventArgs) Handles Me.MouseUp
        GmResidentClient.MyTdGraphicsContainer.BringToFront()
    End Sub

    Private Sub LmfrStatusScreenHc_Move(sender As Object, e As EventArgs) Handles Me.Move
        PositionTopDown()
    End Sub

    Private Sub LmfrStatusScreenHc_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint
        If FormDisplayed = True Then
            GmResidentClient.MyTdGraphicsContainer.Show()
            GmResidentClient.MyTdGraphicsContainer.BringToFront()
        End If

        PositionTopDown()
    End Sub

    Private Sub LmfrStatusScreenHc_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        GmResidentClient.MyTdGraphicsContainer.myExitButton.Visible = True

        GmResidentClient.MyTdGraphicsContainer.Hide()
        GmResidentClient.MyTdGraphicsContainer.TopMost = False
        GmResidentClient.MyTdGraphicsContainer.Hide()
    End Sub

    Private Sub LmfrStatusScreenHc_Click(sender As Object, e As EventArgs) Handles Me.Click
        If GmResidentClient.MyTdGraphicsContainer.TopMost = False Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = True
            GmResidentClient.MyTdGraphicsContainer.Show()
        End If

        PositionTopDown()
    End Sub

    Private Sub Button7_Click_1(sender As Object, e As EventArgs) Handles Button7.Click
        Me.Close()
    End Sub

    Private Sub Button1_Click_1(sender As Object, e As EventArgs) Handles Button1.Click

        HandleAnnotationButtons(Button1, "System")
    End Sub
End Class