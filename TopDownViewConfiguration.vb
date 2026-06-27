
Option Strict Off

Imports VB = Microsoft.VisualBasic

Public Class TopDownViewConfiguration

    'This is the top down view configuration screen.  This is accessed from the top down view window.
    'Allows the user to configure things such as turning on or off various display items, set zooming
    'parameters, set item colors, etc.

    Public LXCRBlueLinePathColor As Color = Color.Blue
    Public LXCRTargetLanePathColor As Color = Color.Red
    Public LXCRBlendedPathColor As Color = Color.DarkGreen
    Public LXCRBlendedPathColorWCoefs As Color = Color.DarkGreen
    Public LXCRBlendedPathColorWCoords As Color = Color.Violet

    Public LXCRBlueLinePathCalcType As String = "Points"
    Public LXCRTargetLanePathCalcType As String = "Coef"

    Public Sub WriteTopDownViewConfigFile()
        ' This routine writes data to the config.xml file. Called when user exits the application

        Dim ccon As New ColorConverter()

        ' Use Path.Combine for file path construction
        Dim filename As String = IO.Path.Combine(My.Application.Info.DirectoryPath, "TopDownViewConfig30.txt")

        Try
            ' Use modern StreamWriter with Using statement for automatic resource disposal
            Using writer As New IO.StreamWriter(filename, False) ' False = overwrite existing file
                ' Write configuration lines
                For x As Integer = 0 To 29 ' if more cases added, need to increment this number accordingly
                    Dim textline As String = String.Empty

                    Select Case x
                        Case 0
                            textline = "LAYOUT"
                        Case 1
                            textline = "FieldOfView_VIS_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.FieldOfView_VIS_Visible.ToString()
                        Case 2
                            textline = "FieldOfView_LRR_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.FieldOfView_LRR_Visible.ToString()
                        Case 3
                            textline = "FieldOfView_LFSRR_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.FieldOfView_LFSRR_Visible.ToString()
                        Case 4
                            textline = "FieldOfView_RFSRR_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.FieldOfView_RFSRR_Visible.ToString()
                        Case 5
                            textline = "ZoomAxis" & vbTab & GmResidentClient.MyTdGraphicsContainer.ZoomAxis
                        Case 6
                            textline = "DefaultPointSize" & vbTab & CStr(GmResidentClient.MyTdGraphicsContainer.DefaultPointSize)
                        Case 7
                            textline = "DangerZoneDisplay_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.DangerZoneDisplay_Visible.ToString()
                        Case 8
                            textline = "DangerZoneLongAdj" & vbTab & CStr(GmResidentClient.MyTdGraphicsContainer.DangerZoneLongAdj)
                        Case 9
                            textline = "VehicleDim_WidthMeters" & vbTab & CStr(GmResidentClient.MyTdGraphicsContainer.VehicleDim_WidthMeters)
                        Case 10
                            textline = "VehicleDim_LengthMeters" & vbTab & CStr(GmResidentClient.MyTdGraphicsContainer.VehicleDim_LengthMeters)
                        Case 11
                            textline = "PATHS"
                        Case 12
                            textline = "TCPath_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.TCPath_Visible.ToString()
                        Case 13
                            textline = "VPath_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.VPath_Visible.ToString()
                        Case 14
                            textline = "LXCRDesiredPath_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.LXCRDesiredPath_Visible.ToString()
                        Case 15
                            textline = "BlueLinePath_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.BlueLinePath_Visible.ToString()
                        Case 16
                            textline = "LXCRBlueLinePath_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.LXCRBlueLinePath_Visible.ToString()
                        Case 17
                            textline = "LXCRTargetLanePath_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.LXCRTargetLanePath_Visible.ToString()
                        Case 18
                            textline = "LXCRBlendedPath_Visible" & vbTab & GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_Visible.ToString()
                        Case 19
                            textline = "LXCRBlendedPath_TimeStepSec" & vbTab & CStr(GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_TimeStepSec)
                        Case 20
                            textline = "LXCRBlueLinePath_Color" & vbTab & DirectCast(ccon.ConvertToString(LXCRBlueLinePathColor), String)
                        Case 21
                            textline = "LXCRTargetLanePath_Color" & vbTab & DirectCast(ccon.ConvertToString(LXCRTargetLanePathColor), String)
                        Case 22
                            textline = "LXCRBlendedPath_ColorWCoefs" & vbTab & DirectCast(ccon.ConvertToString(LXCRBlendedPathColorWCoefs), String)
                        Case 23
                            textline = "LXCRBlendedPath_ColorWCoords" & vbTab & DirectCast(ccon.ConvertToString(LXCRBlendedPathColorWCoords), String)
                        Case 24
                            textline = "FusionDisplayType" & vbTab & GmResidentClient.MyTdGraphicsContainer.FusionDisplayType
                        Case 25
                            textline = "TARGETS"
                        Case 26
                            textline = "FusionTargetObject_MinSizePixels" & vbTab & CStr(GmResidentClient.MyTdGraphicsContainer.FusionTargetObject_MinSizePixels)
                        Case 27
                            textline = "LRR_Object_MinSizePixels" & vbTab & CStr(GmResidentClient.MyTdGraphicsContainer.LRR_Object_MinSizePixels)
                        Case 28
                            textline = "SRR_Objects_MinSizePixels" & vbTab & CStr(GmResidentClient.MyTdGraphicsContainer.SRR_Objects_MinSizePixels)
                        Case 29
                            textline = "VIS_Object_MinSizePixels" & vbTab & CStr(GmResidentClient.MyTdGraphicsContainer.VIS_Object_MinSizePixels)
                    End Select

                    writer.WriteLine(textline)
                Next
            End Using

        Catch ex As Exception
            ' Handle any errors during file writing
            HandleUserMessageLogging("GMRC", $"WriteTopDownViewConfigFile: Error writing configuration file - {ex.Message}")
        End Try

    End Sub

    Public Function ReadTopDownConfigFile() As Boolean
        ' Reads TopDownViewConfig file, extracts configuration information and puts it into variables. Used in conjunction with the WriteConfigFile
        ' routine. If any code is added here (if we add rows to the config file), associated code must also be added to the WriteConfigFile routine,
        ' or we will lose config data when the config.xml file is written on close.

        Dim ccon As New ColorConverter()

        HandleUserMessageLogging("GMRC", "ReadTDConfigFile Called...")

        ReadTopDownConfigFile = True

        ' Use Path.Combine for file path construction
        Dim configFileName As String = IO.Path.Combine(My.Application.Info.DirectoryPath, "TopDownViewConfig30.txt")

        If Not System.IO.File.Exists(configFileName) Then
            HandleUserMessageLogging("GMRC", "TopDownViewConfig30.txt file not found, Using defaults...")
            Return True
        End If

        Dim ctr As Integer = 0

        Try
            ' Use modern StreamReader with Using statement for automatic resource disposal
            Using reader As New IO.StreamReader(configFileName)
                ' Go line by line through TopDownViewconfig.xml file to pick out data from pre-defined lines in text file
                While Not reader.EndOfStream
                    Dim textLine As String = reader.ReadLine()

                    Select Case ctr
                        Case 0
                        ' do nothing, identifier
                        Case 1
                            GmResidentClient.MyTdGraphicsContainer.FieldOfView_VIS_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            VISFOVOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.FieldOfView_VIS_Visible
                            CheckBox8.Checked = VISFOVOnToolStripMenuItem.Checked
                        Case 2
                            GmResidentClient.MyTdGraphicsContainer.FieldOfView_LRR_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            LRRFOVOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.FieldOfView_LRR_Visible
                            CheckBox10.Checked = LRRFOVOnToolStripMenuItem.Checked
                        Case 3
                            GmResidentClient.MyTdGraphicsContainer.FieldOfView_LFSRR_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            LFSRRFOVOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.FieldOfView_LFSRR_Visible
                            CheckBox9.Checked = LFSRRFOVOnToolStripMenuItem.Checked
                        Case 4
                            GmResidentClient.MyTdGraphicsContainer.FieldOfView_RFSRR_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            RFSRRFOVOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.FieldOfView_RFSRR_Visible
                            CheckBox11.Checked = RFSRRFOVOnToolStripMenuItem.Checked
                        Case 5
                            GmResidentClient.MyTdGraphicsContainer.ZoomAxis = VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))
                            Select Case GmResidentClient.MyTdGraphicsContainer.ZoomAxis
                                Case "XY"
                                    ZoomXAndYToolStripMenuItem.Checked = True
                                    ZoomXToolStripMenuItem.Checked = False
                                    ZoomYToolStripMenuItem.Checked = False
                                Case "X"
                                    ZoomXAndYToolStripMenuItem.Checked = False
                                    ZoomXToolStripMenuItem.Checked = True
                                    ZoomYToolStripMenuItem.Checked = False
                                Case "Y"
                                    ZoomXAndYToolStripMenuItem.Checked = False
                                    ZoomXToolStripMenuItem.Checked = False
                                    ZoomYToolStripMenuItem.Checked = True
                            End Select

                            CheckBox12.Checked = ZoomXAndYToolStripMenuItem.Checked
                            CheckBox13.Checked = ZoomXToolStripMenuItem.Checked
                            CheckBox14.Checked = ZoomYToolStripMenuItem.Checked

                        Case 6
                            GmResidentClient.MyTdGraphicsContainer.DefaultPointSize = CInt(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9))))
                            ToolStripComboBox2.Text = CStr(GmResidentClient.MyTdGraphicsContainer.DefaultPointSize)
                            ComboBox1.Text = ToolStripComboBox2.Text
                        Case 7
                            GmResidentClient.MyTdGraphicsContainer.DangerZoneDisplay_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            DangerZoneDisplayOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.DangerZoneDisplay_Visible
                            CheckBox15.Checked = DangerZoneDisplayOnToolStripMenuItem.Checked
                        Case 8
                            ToolStripTextBox1.Text = VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))
                            GmResidentClient.MyTdGraphicsContainer.DangerZoneLongAdj = CDbl(ToolStripTextBox1.Text)
                            TextBox1.Text = ToolStripTextBox1.Text
                        Case 9
                            ToolStripTextBox2.Text = VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))
                            GmResidentClient.MyTdGraphicsContainer.VehicleDim_WidthMeters = CSng(ToolStripTextBox2.Text)
                            TextBox2.Text = ToolStripTextBox2.Text
                        Case 10
                            ToolStripTextBox3.Text = VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))
                            GmResidentClient.MyTdGraphicsContainer.VehicleDim_LengthMeters = CSng(ToolStripTextBox3.Text)
                            TextBox3.Text = ToolStripTextBox3.Text
                        Case 11
                        ' do nothing, identifier
                        Case 12
                            GmResidentClient.MyTdGraphicsContainer.TCPath_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            TCPathDisplayOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.TCPath_Visible
                            CheckBox1.Checked = TCPathDisplayOnToolStripMenuItem.Checked
                        Case 13
                            GmResidentClient.MyTdGraphicsContainer.VPath_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            VPathDisplayOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.VPath_Visible
                            CheckBox2.Checked = VPathDisplayOnToolStripMenuItem.Checked
                        Case 14
                            GmResidentClient.MyTdGraphicsContainer.LXCRDesiredPath_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            LXCRDesiredPathDisplayOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.LXCRDesiredPath_Visible
                            CheckBox3.Checked = LXCRDesiredPathDisplayOnToolStripMenuItem.Checked
                        Case 15
                            GmResidentClient.MyTdGraphicsContainer.BlueLinePath_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            BlueLinePathDisplayOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.BlueLinePath_Visible
                            CheckBox4.Checked = BlueLinePathDisplayOnToolStripMenuItem.Checked
                        Case 16
                            GmResidentClient.MyTdGraphicsContainer.LXCRBlueLinePath_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.LXCRBlueLinePath_Visible
                            CheckBox5.Checked = LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked
                        Case 17
                            GmResidentClient.MyTdGraphicsContainer.LXCRTargetLanePath_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            LXCRTargetLaneDisplayOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.LXCRTargetLanePath_Visible
                            CheckBox6.Checked = LXCRTargetLaneDisplayOnToolStripMenuItem.Checked
                        Case 18
                            GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_Visible = UCase(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))) = "TRUE"
                            LXCRBlendedPathDisplayOnToolStripMenuItem.Checked = GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_Visible
                            CheckBox7.Checked = LXCRBlendedPathDisplayOnToolStripMenuItem.Checked
                        Case 19
                            GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_TimeStepSec = Val(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9))))
                            ToolStripComboBox1.Text = CStr(GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_TimeStepSec)
                            ComboBox2.Text = ToolStripComboBox1.Text
                        Case 20
                            LXCRBlueLinePathColor = DirectCast(ccon.ConvertFromString(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))), Color)
                        Case 21
                            LXCRTargetLanePathColor = DirectCast(ccon.ConvertFromString(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))), Color)
                        Case 22
                            LXCRBlendedPathColorWCoefs = DirectCast(ccon.ConvertFromString(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))), Color)
                        Case 23
                            LXCRBlendedPathColorWCoords = DirectCast(ccon.ConvertFromString(VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))), Color)
                        Case 24
                            GmResidentClient.MyTdGraphicsContainer.FusionDisplayType = VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))
                            If GmResidentClient.MyTdGraphicsContainer.FusionDisplayType = "Triangle" Then
                                RadioButton1.Checked = True
                                RadioButton2.Checked = False
                            ElseIf GmResidentClient.MyTdGraphicsContainer.FusionDisplayType = "Rectangle" Then
                                RadioButton1.Checked = False
                                RadioButton2.Checked = True
                            Else
                                HandleUserMessageLogging("GMRC", "ReadTopDownConfigFile: Invalid Fusion Display Type in TopDownViewConfig30.txt file.", DisplayMsgBox)
                            End If
                        Case 25
                        ' do nothing, identifier
                        Case 26 ' FUSION
                            ToolStripComboBox3.Text = VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))
                            GmResidentClient.MyTdGraphicsContainer.FusionTargetObject_MinSizePixels = Val(ToolStripComboBox3.Text)
                            GmResidentClient.MyTdGraphicsContainer.FusionTargetObject.MinSizePixels = GmResidentClient.MyTdGraphicsContainer.FusionTargetObject_MinSizePixels
                            Me.TrackBar1.Value = GmResidentClient.MyTdGraphicsContainer.FusionTargetObject_MinSizePixels
                        Case 27 ' LRR
                            ToolStripComboBox4.Text = VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))
                            GmResidentClient.MyTdGraphicsContainer.LRR_Object_MinSizePixels = Val(ToolStripComboBox4.Text)
                            GmResidentClient.MyTdGraphicsContainer.LRR_Object.MinSizePixels = GmResidentClient.MyTdGraphicsContainer.LRR_Object_MinSizePixels
                            Me.TrackBar2.Value = GmResidentClient.MyTdGraphicsContainer.LRR_Object_MinSizePixels
                        Case 28 ' SRR
                            ToolStripComboBox5.Text = VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))
                            GmResidentClient.MyTdGraphicsContainer.SRR_Objects_MinSizePixels = Val(ToolStripComboBox5.Text)
                            GmResidentClient.MyTdGraphicsContainer.LSRR_Object.MinSizePixels = GmResidentClient.MyTdGraphicsContainer.SRR_Objects_MinSizePixels
                            GmResidentClient.MyTdGraphicsContainer.RSRR_Object.MinSizePixels = GmResidentClient.MyTdGraphicsContainer.SRR_Objects_MinSizePixels
                            Me.TrackBar4.Value = GmResidentClient.MyTdGraphicsContainer.SRR_Objects_MinSizePixels
                        Case 29 ' VIS
                            ToolStripComboBox6.Text = VB.Right(textLine, Len(textLine) - InStr(textLine, Chr(9)))
                            GmResidentClient.MyTdGraphicsContainer.VIS_Object_MinSizePixels = Val(ToolStripComboBox6.Text)
                            GmResidentClient.MyTdGraphicsContainer.VIS_Object.MinSizePixels = GmResidentClient.MyTdGraphicsContainer.VIS_Object_MinSizePixels
                            Me.TrackBar3.Value = GmResidentClient.MyTdGraphicsContainer.VIS_Object_MinSizePixels
                        Case Else
                            HandleUserMessageLogging("GMRC", "Read Config File: There appear to be extra lines in the config.xml file...")
                    End Select

                    ctr += 1
                End While
            End Using

            HandleUserMessageLogging("GMRC", "Reading TopDownViewConfig File Complete")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ReadTopDownConfigFile: Error reading configuration file - {ex.Message}")
            ReadTopDownConfigFile = False
        End Try

    End Function

    Private Sub VISFOVOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VISFOVOnToolStripMenuItem.Click

        If VISFOVOnToolStripMenuItem.Checked = True Then
            VISFOVOnToolStripMenuItem.Checked = False
        Else
            VISFOVOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.FieldOfView_VIS_Visible = VISFOVOnToolStripMenuItem.Checked
        CheckBox8.Checked = VISFOVOnToolStripMenuItem.Checked

    End Sub

    Private Sub LRRFOVOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LRRFOVOnToolStripMenuItem.Click

        If LRRFOVOnToolStripMenuItem.Checked = True Then
            LRRFOVOnToolStripMenuItem.Checked = False
        Else
            LRRFOVOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.FieldOfView_LRR_Visible = LRRFOVOnToolStripMenuItem.Checked
        CheckBox10.Checked = LRRFOVOnToolStripMenuItem.Checked
    End Sub

    Private Sub LFSRRFOVOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LFSRRFOVOnToolStripMenuItem.Click
        If LFSRRFOVOnToolStripMenuItem.Checked = True Then
            LFSRRFOVOnToolStripMenuItem.Checked = False
        Else
            LFSRRFOVOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.FieldOfView_LFSRR_Visible = LFSRRFOVOnToolStripMenuItem.Checked
        CheckBox9.Checked = LFSRRFOVOnToolStripMenuItem.Checked
    End Sub

    Private Sub RFSRRFOVOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RFSRRFOVOnToolStripMenuItem.Click
        If RFSRRFOVOnToolStripMenuItem.Checked = True Then
            RFSRRFOVOnToolStripMenuItem.Checked = False
        Else
            RFSRRFOVOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.FieldOfView_RFSRR_Visible = RFSRRFOVOnToolStripMenuItem.Checked
        CheckBox11.Checked = RFSRRFOVOnToolStripMenuItem.Checked
    End Sub

    Private Sub TCPathDisplayOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TCPathDisplayOnToolStripMenuItem.Click
        If TCPathDisplayOnToolStripMenuItem.Checked = True Then
            TCPathDisplayOnToolStripMenuItem.Checked = False
        Else
            TCPathDisplayOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.TCPath_Visible = TCPathDisplayOnToolStripMenuItem.Checked
        CheckBox1.Checked = TCPathDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub VPathDisplayOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VPathDisplayOnToolStripMenuItem.Click
        If VPathDisplayOnToolStripMenuItem.Checked = True Then
            VPathDisplayOnToolStripMenuItem.Checked = False
        Else
            VPathDisplayOnToolStripMenuItem.Checked = True
        End If
        GmResidentClient.MyTdGraphicsContainer.VPath_Visible = VPathDisplayOnToolStripMenuItem.Checked
        CheckBox2.Checked = VPathDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRDesiredPathDisplayOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRDesiredPathDisplayOnToolStripMenuItem.Click
        If LXCRDesiredPathDisplayOnToolStripMenuItem.Checked = True Then
            LXCRDesiredPathDisplayOnToolStripMenuItem.Checked = False
        Else
            LXCRDesiredPathDisplayOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.LXCRDesiredPath_Visible = LXCRDesiredPathDisplayOnToolStripMenuItem.Checked
        CheckBox3.Checked = LXCRDesiredPathDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub BlueLinePathDisplayOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles BlueLinePathDisplayOnToolStripMenuItem.Click
        If BlueLinePathDisplayOnToolStripMenuItem.Checked = True Then
            BlueLinePathDisplayOnToolStripMenuItem.Checked = False
        Else
            BlueLinePathDisplayOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.BlueLinePath_Visible = BlueLinePathDisplayOnToolStripMenuItem.Checked
        CheckBox4.Checked = BlueLinePathDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRBlueLinePathDisplayOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlueLinePathDisplayOnToolStripMenuItem.Click
        If LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked = True Then
            LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked = False
        Else
            LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.LXCRBlueLinePath_Visible = LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked
        CheckBox5.Checked = LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRTargetLaneDisplayOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRTargetLaneDisplayOnToolStripMenuItem.Click
        If LXCRTargetLaneDisplayOnToolStripMenuItem.Checked = True Then
            LXCRTargetLaneDisplayOnToolStripMenuItem.Checked = False
        Else
            LXCRTargetLaneDisplayOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.LXCRTargetLanePath_Visible = LXCRTargetLaneDisplayOnToolStripMenuItem.Checked
        CheckBox6.Checked = LXCRTargetLaneDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRBlendedPathToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlendedPathToolStripMenuItem.Click

    End Sub

    Private Sub LXCRBlendedPathDisplayOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlendedPathDisplayOnToolStripMenuItem.Click
        If LXCRBlendedPathDisplayOnToolStripMenuItem.Checked = True Then
            LXCRBlendedPathDisplayOnToolStripMenuItem.Checked = False
        Else
            LXCRBlendedPathDisplayOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_Visible = LXCRBlendedPathDisplayOnToolStripMenuItem.Checked
        CheckBox7.Checked = LXCRBlendedPathDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRBlueLinePathColorToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlueLinePathColorToolStripMenuItem.Click

        If (ColorDialog1.ShowDialog() = DialogResult.OK) Then
            LXCRBlueLinePathColor = ColorDialog1.Color
        End If

    End Sub

    Private Sub LXCRTargetLanePathColorToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRTargetLanePathColorToolStripMenuItem.Click

        If (ColorDialog1.ShowDialog() = DialogResult.OK) Then
            LXCRTargetLanePathColor = ColorDialog1.Color
        End If
    End Sub

    Private Sub LXCRBlueLinePointsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlueLinePointsToolStripMenuItem.Click
        If LXCRBlueLinePointsToolStripMenuItem.Checked = True Then
            LXCRBlueLinePointsToolStripMenuItem.Checked = False
        Else
            LXCRBlueLinePointsToolStripMenuItem.Checked = True
        End If

        LXCRBlueLineDashedPathToolStripMenuItem.Checked = Not LXCRBlueLinePointsToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRBlueLineDashedPathToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlueLineDashedPathToolStripMenuItem.Click

        If LXCRBlueLineDashedPathToolStripMenuItem.Checked = True Then
            LXCRBlueLineDashedPathToolStripMenuItem.Checked = False
        Else
            LXCRBlueLineDashedPathToolStripMenuItem.Checked = True
        End If

        LXCRBlueLinePointsToolStripMenuItem.Checked = Not LXCRBlueLineDashedPathToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRBlueLineFromXYPointsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlueLineFromXYPointsToolStripMenuItem.Click
        If LXCRBlueLineFromXYPointsToolStripMenuItem.Checked = True Then
            LXCRBlueLinePathCalcType = "Coef"
            LXCRBlueLineFromXYPointsToolStripMenuItem.Checked = False
        Else
            LXCRBlueLinePathCalcType = "Points"
            LXCRBlueLineFromXYPointsToolStripMenuItem.Checked = True
        End If

        LXCRBlueLineCalculatedFromCoefToolStripMenuItem.Checked = Not LXCRBlueLineFromXYPointsToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRBlueLineCalculatedFromCoefToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlueLineCalculatedFromCoefToolStripMenuItem.Click

        If LXCRBlueLineCalculatedFromCoefToolStripMenuItem.Checked = True Then
            LXCRBlueLinePathCalcType = "Points"
            LXCRBlueLineCalculatedFromCoefToolStripMenuItem.Checked = False
        Else
            LXCRBlueLinePathCalcType = "Coef"
            LXCRBlueLineCalculatedFromCoefToolStripMenuItem.Checked = True
        End If

        LXCRBlueLineFromXYPointsToolStripMenuItem.Checked = Not LXCRBlueLineCalculatedFromCoefToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRTargetLaneFromXYPointsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRTargetLaneFromXYPointsToolStripMenuItem.Click

        If LXCRTargetLaneFromXYPointsToolStripMenuItem.Checked = True Then
            LXCRTargetLanePathCalcType = "Coef"
            LXCRTargetLaneFromXYPointsToolStripMenuItem.Checked = False
        Else
            LXCRTargetLanePathCalcType = "Points"
            LXCRTargetLaneFromXYPointsToolStripMenuItem.Checked = True
        End If

        LXCRTargetLaneCalculatedFromCoefToolStripMenuItem.Checked = Not LXCRTargetLaneFromXYPointsToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRTargetLaneCalculatedFromCoefToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRTargetLaneCalculatedFromCoefToolStripMenuItem.Click

        If LXCRTargetLaneCalculatedFromCoefToolStripMenuItem.Checked = True Then
            LXCRTargetLanePathCalcType = "Points"
            LXCRTargetLaneCalculatedFromCoefToolStripMenuItem.Checked = False
        Else
            LXCRTargetLanePathCalcType = "Coef"
            LXCRTargetLaneCalculatedFromCoefToolStripMenuItem.Checked = True
        End If

        LXCRTargetLaneFromXYPointsToolStripMenuItem.Checked = Not LXCRTargetLaneCalculatedFromCoefToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRTargetLanePathToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRTargetLanePathToolStripMenuItem.Click

    End Sub

    Private Sub LXCRBlendedPathChooseColorToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlendedPathChooseColorToolStripMenuItem.Click

        If (ColorDialog1.ShowDialog() = DialogResult.OK) Then
            LXCRBlendedPathColor = ColorDialog1.Color
        End If

    End Sub

    Private Sub LXCRBlendedPathFromXYPointsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlendedPathFromXYPointsToolStripMenuItem.Click


        If (ColorDialog1.ShowDialog() = DialogResult.OK) Then
            LXCRBlendedPathColorWCoords = ColorDialog1.Color
        End If

        'If LXCRBlendedPathFromXYPointsToolStripMenuItem.Checked = True Then
        'LXCRTargetLanePathCalcType = "Coef"
        'LXCRBlendedPathFromXYPointsToolStripMenuItem.Checked = False
        'Else
        'LXCRTargetLanePathCalcType = "Points"
        'LXCRBlendedPathFromXYPointsToolStripMenuItem.Checked = True
        'End If

        'LXCRBlendedPathCalculatedFromCoefToolStripMenuItem.Checked = Not LXCRBlendedPathFromXYPointsToolStripMenuItem.Checked
    End Sub

    Private Sub LXCRBlendedPathCalculatedFromCoefToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LXCRBlendedPathCalculatedFromCoefToolStripMenuItem.Click

        If (ColorDialog1.ShowDialog() = DialogResult.OK) Then
            LXCRBlendedPathColorWCoefs = ColorDialog1.Color
        End If

        'If LXCRBlendedPathCalculatedFromCoefToolStripMenuItem.Checked = True Then
        'LXCRBlendedPathCalcType = "Points"
        'LXCRBlendedPathCalculatedFromCoefToolStripMenuItem.Checked = False
        'Else
        'LXCRBlendedPathCalcType = "Coef"
        'LXCRBlendedPathCalculatedFromCoefToolStripMenuItem.Checked = True
        'End If

        'LXCRBlendedPathFromXYPointsToolStripMenuItem.Checked = Not LXCRBlendedPathCalculatedFromCoefToolStripMenuItem.Checked
    End Sub

    Private Sub ZoomXAndYToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ZoomXAndYToolStripMenuItem.Click
        If ZoomXAndYToolStripMenuItem.Checked = False Then
            ZoomXAndYToolStripMenuItem.Checked = True
            ZoomXToolStripMenuItem.Checked = False
            ZoomYToolStripMenuItem.Checked = False
            GmResidentClient.MyTdGraphicsContainer.HandleZoom(0)
            GmResidentClient.MyTdGraphicsContainer.ZoomAxis = "XY"
        End If
        CheckBox12.Checked = ZoomXAndYToolStripMenuItem.Checked
        CheckBox13.Checked = ZoomXToolStripMenuItem.Checked
        CheckBox14.Checked = ZoomYToolStripMenuItem.Checked
    End Sub

    Private Sub ZoomXToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ZoomXToolStripMenuItem.Click
        If ZoomXToolStripMenuItem.Checked = True Then
            ZoomXToolStripMenuItem.Checked = False
            ZoomYToolStripMenuItem.Checked = False
            ZoomXAndYToolStripMenuItem.Checked = True
        Else
            ZoomXAndYToolStripMenuItem.Checked = False
            ZoomYToolStripMenuItem.Checked = False
            ZoomXToolStripMenuItem.Checked = True
            GmResidentClient.MyTdGraphicsContainer.ZoomAxis = "X"
        End If
        CheckBox12.Checked = ZoomXAndYToolStripMenuItem.Checked
        CheckBox13.Checked = ZoomXToolStripMenuItem.Checked
        CheckBox14.Checked = ZoomYToolStripMenuItem.Checked
    End Sub

    Private Sub ZoomYToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ZoomYToolStripMenuItem.Click

        If ZoomYToolStripMenuItem.Checked = True Then
            ZoomYToolStripMenuItem.Checked = False
            ZoomYToolStripMenuItem.Checked = False
            ZoomXAndYToolStripMenuItem.Checked = True
        Else
            ZoomXAndYToolStripMenuItem.Checked = False
            ZoomXToolStripMenuItem.Checked = False
            ZoomYToolStripMenuItem.Checked = True
            GmResidentClient.MyTdGraphicsContainer.ZoomAxis = "Y"
        End If
        CheckBox12.Checked = ZoomXAndYToolStripMenuItem.Checked
        CheckBox13.Checked = ZoomXToolStripMenuItem.Checked
        CheckBox14.Checked = ZoomYToolStripMenuItem.Checked
    End Sub

    Private Sub ToolStripComboBox1_Click(sender As Object, e As EventArgs) Handles ToolStripComboBox1.Click
        'KeLXCR_t_CDP_PathTimeStep = CDbl(ToolStripComboBox1.Text)
    End Sub

    Private Sub ToolStripComboBox1_TextChanged(sender As Object, e As EventArgs) Handles ToolStripComboBox1.TextChanged
        GmResidentClient.MyTdGraphicsContainer.KeLXCR_t_CDP_PathTimeStep = CDbl(ToolStripComboBox1.Text)
        GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_TimeStepSec = GmResidentClient.MyTdGraphicsContainer.KeLXCR_t_CDP_PathTimeStep
        ComboBox2.Text = ToolStripComboBox1.Text
    End Sub

    Private Sub ToolStripComboBox2_Click(sender As Object, e As EventArgs) Handles ToolStripComboBox2.Click

    End Sub

    Private Sub ToolStripComboBox2_TextChanged(sender As Object, e As EventArgs) Handles ToolStripComboBox2.TextChanged
        GmResidentClient.MyTdGraphicsContainer.DefaultPointSize = CInt(ToolStripComboBox2.Text)
        ComboBox1.Text = ToolStripComboBox2.Text
    End Sub

    Private Sub DangerZoneDisplayOnToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DangerZoneDisplayOnToolStripMenuItem.Click

        If DangerZoneDisplayOnToolStripMenuItem.Checked = True Then
            DangerZoneDisplayOnToolStripMenuItem.Checked = False
        Else
            DangerZoneDisplayOnToolStripMenuItem.Checked = True
        End If

        GmResidentClient.MyTdGraphicsContainer.DangerZoneDisplay_Visible = DangerZoneDisplayOnToolStripMenuItem.Checked
        CheckBox15.Checked = DangerZoneDisplayOnToolStripMenuItem.Checked

    End Sub

    Private Sub TopDownViewConfiguration_Activated(sender As Object, e As EventArgs) Handles Me.Activated

    End Sub

    Private Sub ToolStripTextBox1_Click(sender As Object, e As EventArgs) Handles ToolStripTextBox1.Click

    End Sub

    Private Sub ToolStripTextBox1_TextChanged(sender As Object, e As EventArgs) Handles ToolStripTextBox1.TextChanged
        If IsNumeric(ToolStripTextBox1.Text) Then
            GmResidentClient.MyTdGraphicsContainer.COG_To_COV_Offset = CDbl(ToolStripTextBox1.Text)
            GmResidentClient.MyTdGraphicsContainer.DangerZoneLongAdj = GmResidentClient.MyTdGraphicsContainer.COG_To_COV_Offset
            TextBox1.Text = ToolStripTextBox1.Text
        End If
    End Sub

    Private Sub ToolStripTextBox2_Click(sender As Object, e As EventArgs) Handles ToolStripTextBox2.Click

    End Sub

    Private Sub ToolStripTextBox2_TextChanged(sender As Object, e As EventArgs) Handles ToolStripTextBox2.TextChanged
        If IsNumeric(ToolStripTextBox2.Text) Then

            GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLateral = CSng(ToolStripTextBox2.Text)
            GmResidentClient.MyTdGraphicsContainer.VehicleObject.DefaultWidth = CInt(GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLateral)
            GmResidentClient.MyTdGraphicsContainer.VehicleDim_WidthMeters = GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLateral
            TextBox2.Text = ToolStripTextBox2.Text

        End If

    End Sub

    Private Sub ToolStripTextBox3_Click(sender As Object, e As EventArgs) Handles ToolStripTextBox3.Click

    End Sub

    Private Sub ToolStripTextBox3_TextChanged(sender As Object, e As EventArgs) Handles ToolStripTextBox3.TextChanged

        If IsNumeric(ToolStripTextBox3.Text) Then
            GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLongitudinal = CSng(ToolStripTextBox3.Text)
            GmResidentClient.MyTdGraphicsContainer.VehicleObject.DefaultHeight = CInt(GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLongitudinal)
            GmResidentClient.MyTdGraphicsContainer.VehicleDim_LengthMeters = GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLongitudinal
            TextBox3.Text = ToolStripTextBox3.Text
        End If
    End Sub

    Private Sub LongitudinalAdjustCOGToCOVToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LongitudinalAdjustCOGToCOVToolStripMenuItem.Click

    End Sub

    Private Sub LengthToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LengthToolStripMenuItem.Click

    End Sub

    Private Sub TopDownViewConfiguration_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub TopDownViewConfiguration_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        e.Cancel = True
        Me.Hide()
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged

    End Sub

    Private Sub TCPathDisplayOnToolStripMenuItem_CheckStateChanged(sender As Object, e As EventArgs) Handles TCPathDisplayOnToolStripMenuItem.CheckStateChanged

    End Sub

    Private Sub TCPathDisplayOnToolStripMenuItem_CheckedChanged(sender As Object, e As EventArgs) Handles TCPathDisplayOnToolStripMenuItem.CheckedChanged

    End Sub

    Private Sub CheckBox1_Click(sender As Object, e As EventArgs) Handles CheckBox1.Click

        If CheckBox1.Checked = True Then
            TCPathDisplayOnToolStripMenuItem.Checked = True
        Else
            TCPathDisplayOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.TCPath_Visible = TCPathDisplayOnToolStripMenuItem.Checked
        CheckBox1.Checked = TCPathDisplayOnToolStripMenuItem.Checked

    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged

    End Sub

    Private Sub CheckBox2_Click(sender As Object, e As EventArgs) Handles CheckBox2.Click

        If CheckBox2.Checked = True Then
            VPathDisplayOnToolStripMenuItem.Checked = True
        Else
            VPathDisplayOnToolStripMenuItem.Checked = False
        End If
        GmResidentClient.MyTdGraphicsContainer.VPath_Visible = VPathDisplayOnToolStripMenuItem.Checked
        CheckBox2.Checked = VPathDisplayOnToolStripMenuItem.Checked

    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged

    End Sub

    Private Sub CheckBox3_Click(sender As Object, e As EventArgs) Handles CheckBox3.Click

        If CheckBox3.Checked = True Then
            LXCRDesiredPathDisplayOnToolStripMenuItem.Checked = True
        Else
            LXCRDesiredPathDisplayOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.LXCRDesiredPath_Visible = LXCRDesiredPathDisplayOnToolStripMenuItem.Checked
        CheckBox3.Checked = LXCRDesiredPathDisplayOnToolStripMenuItem.Checked

    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged

    End Sub

    Private Sub CheckBox5_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox5.CheckedChanged

    End Sub

    Private Sub CheckBox5_Click(sender As Object, e As EventArgs) Handles CheckBox5.Click

        If CheckBox5.Checked = True Then
            LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked = True
        Else
            LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.LXCRBlueLinePath_Visible = LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked
        CheckBox5.Checked = LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked

    End Sub

    Private Sub CheckBox4_Click(sender As Object, e As EventArgs) Handles CheckBox4.Click

        If CheckBox4.Checked = True Then
            BlueLinePathDisplayOnToolStripMenuItem.Checked = True
        Else
            BlueLinePathDisplayOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.BlueLinePath_Visible = BlueLinePathDisplayOnToolStripMenuItem.Checked
        CheckBox4.Checked = BlueLinePathDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub CheckBox6_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox6.CheckedChanged

    End Sub

    Private Sub CheckBox6_Click(sender As Object, e As EventArgs) Handles CheckBox6.Click

        If CheckBox6.Checked = True Then
            LXCRTargetLaneDisplayOnToolStripMenuItem.Checked = True
        Else
            LXCRTargetLaneDisplayOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.LXCRTargetLanePath_Visible = LXCRTargetLaneDisplayOnToolStripMenuItem.Checked
        CheckBox6.Checked = LXCRTargetLaneDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub CheckBox7_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox7.CheckedChanged

    End Sub

    Private Sub CheckBox7_Click(sender As Object, e As EventArgs) Handles CheckBox7.Click

        If CheckBox7.Checked = True Then
            LXCRBlendedPathDisplayOnToolStripMenuItem.Checked = True
        Else
            LXCRBlendedPathDisplayOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_Visible = LXCRBlendedPathDisplayOnToolStripMenuItem.Checked
        CheckBox7.Checked = LXCRBlendedPathDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged

    End Sub

    Private Sub ComboBox1_TextChanged(sender As Object, e As EventArgs) Handles ComboBox1.TextChanged
        GmResidentClient.MyTdGraphicsContainer.DefaultPointSize = CInt(ComboBox1.Text)
        ToolStripComboBox2.Text = ComboBox1.Text
    End Sub

    Private Sub CheckBox15_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox15.CheckedChanged

    End Sub

    Private Sub CheckBox15_Click(sender As Object, e As EventArgs) Handles CheckBox15.Click

        If CheckBox15.Checked = True Then
            DangerZoneDisplayOnToolStripMenuItem.Checked = True
        Else
            DangerZoneDisplayOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.DangerZoneDisplay_Visible = DangerZoneDisplayOnToolStripMenuItem.Checked
        CheckBox15.Checked = DangerZoneDisplayOnToolStripMenuItem.Checked
    End Sub

    Private Sub CheckBox8_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox8.CheckedChanged

    End Sub

    Private Sub CheckBox8_Click(sender As Object, e As EventArgs) Handles CheckBox8.Click

        If CheckBox8.Checked = True Then
            VISFOVOnToolStripMenuItem.Checked = True
        Else
            VISFOVOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.FieldOfView_VIS_Visible = VISFOVOnToolStripMenuItem.Checked
        CheckBox8.Checked = VISFOVOnToolStripMenuItem.Checked

    End Sub

    Private Sub CheckBox10_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox10.CheckedChanged

    End Sub

    Private Sub CheckBox10_Click(sender As Object, e As EventArgs) Handles CheckBox10.Click

        If CheckBox10.Checked = True Then
            LRRFOVOnToolStripMenuItem.Checked = True
        Else
            LRRFOVOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.FieldOfView_LRR_Visible = LRRFOVOnToolStripMenuItem.Checked
        CheckBox10.Checked = LRRFOVOnToolStripMenuItem.Checked

    End Sub

    Private Sub CheckBox9_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox9.CheckedChanged

    End Sub

    Private Sub CheckBox9_Click(sender As Object, e As EventArgs) Handles CheckBox9.Click

        If CheckBox9.Checked = True Then
            LFSRRFOVOnToolStripMenuItem.Checked = True
        Else
            LFSRRFOVOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.FieldOfView_LFSRR_Visible = LFSRRFOVOnToolStripMenuItem.Checked
        CheckBox9.Checked = LFSRRFOVOnToolStripMenuItem.Checked
    End Sub

    Private Sub CheckBox11_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox11.CheckedChanged

    End Sub

    Private Sub CheckBox11_Click(sender As Object, e As EventArgs) Handles CheckBox11.Click

        If CheckBox11.Checked = True Then
            RFSRRFOVOnToolStripMenuItem.Checked = True
        Else
            RFSRRFOVOnToolStripMenuItem.Checked = False
        End If

        GmResidentClient.MyTdGraphicsContainer.FieldOfView_RFSRR_Visible = RFSRRFOVOnToolStripMenuItem.Checked
        CheckBox11.Checked = RFSRRFOVOnToolStripMenuItem.Checked
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged

        If IsNumeric(TextBox1.Text) Then
            GmResidentClient.MyTdGraphicsContainer.COG_To_COV_Offset = CDbl(TextBox1.Text)
            GmResidentClient.MyTdGraphicsContainer.DangerZoneLongAdj = GmResidentClient.MyTdGraphicsContainer.COG_To_COV_Offset
            ToolStripTextBox1.Text = TextBox1.Text
        End If

    End Sub

    Private Sub CheckBox12_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox12.CheckedChanged

    End Sub

    Private Sub CheckBox12_Click(sender As Object, e As EventArgs) Handles CheckBox12.Click

        If CheckBox12.Checked = True Then
            ZoomXAndYToolStripMenuItem.Checked = True
            ZoomXToolStripMenuItem.Checked = False
            ZoomYToolStripMenuItem.Checked = False
            GmResidentClient.MyTdGraphicsContainer.HandleZoom(0)
            GmResidentClient.MyTdGraphicsContainer.ZoomAxis = "XY"
        End If

        CheckBox12.Checked = ZoomXAndYToolStripMenuItem.Checked
        CheckBox13.Checked = ZoomXToolStripMenuItem.Checked
        CheckBox14.Checked = ZoomYToolStripMenuItem.Checked

    End Sub

    Private Sub CheckBox13_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox13.CheckedChanged

    End Sub

    Private Sub CheckBox13_Click(sender As Object, e As EventArgs) Handles CheckBox13.Click

        If CheckBox13.Checked = True Then
            ZoomXToolStripMenuItem.Checked = True
            ZoomYToolStripMenuItem.Checked = False
            ZoomXAndYToolStripMenuItem.Checked = False
        Else
            ZoomXAndYToolStripMenuItem.Checked = True
            ZoomYToolStripMenuItem.Checked = False
            ZoomXToolStripMenuItem.Checked = False
            GmResidentClient.MyTdGraphicsContainer.ZoomAxis = "X"
        End If

        CheckBox12.Checked = ZoomXAndYToolStripMenuItem.Checked
        CheckBox13.Checked = ZoomXToolStripMenuItem.Checked
        CheckBox14.Checked = ZoomYToolStripMenuItem.Checked

    End Sub

    Private Sub CheckBox14_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox14.CheckedChanged

    End Sub

    Private Sub CheckBox14_Click(sender As Object, e As EventArgs) Handles CheckBox14.Click

        If CheckBox14.Checked = True Then
            ZoomYToolStripMenuItem.Checked = True
            ZoomXToolStripMenuItem.Checked = False
            ZoomXAndYToolStripMenuItem.Checked = False
        Else
            ZoomXAndYToolStripMenuItem.Checked = True
            ZoomXToolStripMenuItem.Checked = False
            ZoomYToolStripMenuItem.Checked = False
            GmResidentClient.MyTdGraphicsContainer.ZoomAxis = "Y"
        End If

        CheckBox12.Checked = ZoomXAndYToolStripMenuItem.Checked
        CheckBox13.Checked = ZoomXToolStripMenuItem.Checked
        CheckBox14.Checked = ZoomYToolStripMenuItem.Checked

    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles TextBox2.TextChanged

        If IsNumeric(TextBox2.Text) Then

            GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLateral = CSng(TextBox2.Text)
            GmResidentClient.MyTdGraphicsContainer.VehicleObject.DefaultWidth = CInt(GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLateral)
            GmResidentClient.MyTdGraphicsContainer.VehicleDim_WidthMeters = GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLateral
            ToolStripTextBox2.Text = TextBox2.Text

        End If

    End Sub

    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs) Handles TextBox3.TextChanged

        If IsNumeric(TextBox3.Text) Then
            GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLongitudinal = CSng(TextBox3.Text)
            GmResidentClient.MyTdGraphicsContainer.VehicleObject.DefaultHeight = CInt(GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLongitudinal)
            GmResidentClient.MyTdGraphicsContainer.VehicleDim_LengthMeters = GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLongitudinal
            ToolStripTextBox3.Text = TextBox3.Text

        End If

    End Sub

    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged

    End Sub

    Private Sub ComboBox2_TextChanged(sender As Object, e As EventArgs) Handles ComboBox2.TextChanged

        GmResidentClient.MyTdGraphicsContainer.KeLXCR_t_CDP_PathTimeStep = CDbl(ComboBox2.Text)
        GmResidentClient.MyTdGraphicsContainer.LXCRBlendedPath_TimeStepSec = GmResidentClient.MyTdGraphicsContainer.KeLXCR_t_CDP_PathTimeStep
        ToolStripComboBox1.Text = ComboBox2.Text

    End Sub

    Private Sub RadioButton1_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton1.CheckedChanged
        If RadioButton1.Checked = True Then
            RadioButton2.Checked = False
            'TDGraphicsContainerClass.FusionDisplayType = "Triangle"
            GmResidentClient.MyTdGraphicsContainer.FusionDisplayType = "Triangle"
        End If
    End Sub

    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton2.CheckedChanged

        If RadioButton2.Checked = True Then
            RadioButton1.Checked = False
            ' TDGraphicsContainerClass.FusionDisplayType = "Rectangle"
            GmResidentClient.MyTdGraphicsContainer.FusionDisplayType = "Rectangle"
        End If

    End Sub
    
    Private Sub ToolStripComboBox3_Click(sender As Object, e As EventArgs) Handles ToolStripComboBox3.Click

    End Sub

    Private Sub ToolStripComboBox3_TextChanged(sender As Object, e As EventArgs) Handles ToolStripComboBox3.TextChanged
        GmResidentClient.MyTdGraphicsContainer.FusionTargetObject_MinSizePixels = Val(ToolStripComboBox3.Text)
    End Sub

    Private Sub ToolStripComboBox4_Click(sender As Object, e As EventArgs) Handles ToolStripComboBox4.Click

    End Sub

    Private Sub ToolStripComboBox4_TextChanged(sender As Object, e As EventArgs) Handles ToolStripComboBox4.TextChanged
        GmResidentClient.MyTdGraphicsContainer.LRR_Object_MinSizePixels = Val(ToolStripComboBox4.Text)
    End Sub

    Private Sub ToolStripComboBox5_Click(sender As Object, e As EventArgs) Handles ToolStripComboBox5.Click

    End Sub

    Private Sub ToolStripComboBox5_TextChanged(sender As Object, e As EventArgs) Handles ToolStripComboBox5.TextChanged
        GmResidentClient.MyTdGraphicsContainer.SRR_Objects_MinSizePixels = Val(ToolStripComboBox5.Text)
    End Sub

    Private Sub ToolStripComboBox6_TextChanged(sender As Object, e As EventArgs) Handles ToolStripComboBox6.TextChanged
        GmResidentClient.MyTdGraphicsContainer.VIS_Object_MinSizePixels = Val(ToolStripComboBox6.Text)
    End Sub

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll

    End Sub

    Private Sub TrackBar1_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar1.ValueChanged
        GmResidentClient.MyTdGraphicsContainer.FusionTargetObject_MinSizePixels = TrackBar1.Value
        ToolStripComboBox3.Text = CStr(TrackBar1.Value)
    End Sub

    Private Sub TrackBar3_Scroll(sender As Object, e As EventArgs) Handles TrackBar3.Scroll

    End Sub

    Private Sub TrackBar3_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar3.ValueChanged
        GmResidentClient.MyTdGraphicsContainer.VIS_Object_MinSizePixels = TrackBar3.Value
        ToolStripComboBox6.Text = CStr(TrackBar3.Value)
    End Sub

    Private Sub TrackBar2_Scroll(sender As Object, e As EventArgs) Handles TrackBar2.Scroll

    End Sub

    Private Sub TrackBar2_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar2.ValueChanged
        GmResidentClient.MyTdGraphicsContainer.LRR_Object_MinSizePixels = TrackBar2.Value
        ToolStripComboBox4.Text = CStr(TrackBar2.Value)
    End Sub

    Private Sub TrackBar4_Scroll(sender As Object, e As EventArgs) Handles TrackBar4.Scroll

    End Sub

    Private Sub TrackBar4_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar4.ValueChanged
        GmResidentClient.MyTdGraphicsContainer.SRR_Objects_MinSizePixels = TrackBar4.Value
        ToolStripComboBox5.Text = CStr(TrackBar4.Value)
    End Sub
End Class