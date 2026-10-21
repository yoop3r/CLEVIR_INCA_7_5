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

    Private Sub HandleClevirDisplayOfClusterMsgs(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks.
        'Displays LCC ClusterMessageText in a momentary UserInfoStatus message 

        Static messageDisplayed As Boolean
        Dim lccClusterMessageText As String
        Dim lccButtonPress As Integer = 0
        Dim lccClusterMsg As Integer = 0
        Static saveLccClusterMsg As Integer = 0
        Static MySaveTime4 As DateTime
        Static MyElapseTime4 As TimeSpan


        lccClusterMsg = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LCC_CLUSTER_MSG, 1)).SignalData)
        lccButtonPress = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LCC_BUTTON_PRESS, 1)).SignalData)

        'COMMENT OUT BEFORE BUILDING!!!!!!!!!!!!!!!!!!!!!

        'LCC_CLUSTER_MSG = GetRandom(0, 25)
        'LCC_BUTTON_PRESS = GetRandom(0, 2)
        'LCC_BUTTON_PRESS = 0
        'LCC_CLUSTER_MSG = 0

        If (((saveLccClusterMsg <> lccClusterMsg) And saveLccClusterMsg <> 0) Or lccButtonPress > 0) And messageDisplayed = False Then

            saveLccClusterMsg = lccClusterMsg

            Select Case lccClusterMsg
                Case 0
                    'CeVGCR_e_LWI_NoIndication()
                    lccClusterMessageText = ""
                Case 1
                    'CeVGCR_e_LWI_LaneLines()
                    lccClusterMessageText = "Lane Lines"
                Case 2
                    'CeVGCR_e_LWI_TightCurve()
                    lccClusterMessageText = "Tight Curve"
                Case 3

                    'CeVGCR_e_LWI_FreewayEnds()
                    lccClusterMessageText = "Freeway Ends"
                Case 4

                    'CeVGCR_e_LWI_Construction()
                    lccClusterMessageText = "Construction"
                Case 5

                    'CeVGCR_e_LWI_ExcessiveSpeed()
                    lccClusterMessageText = "Excessive Speed"
                Case 6

                    'CeVGCR_e_LWI_VehicleProximity()
                    lccClusterMessageText = "Vehicle Proximity"
                Case 7

                    'CeVGCR_e_LWI_CruiseDisengaged()
                    lccClusterMessageText = "Cruise Disengaged"
                Case 8

                    'CeVGCR_e_LWI_Unavailable()
                    lccClusterMessageText = "Unavailable"
                Case 9

                    'CeVGCR_e_LWI_AttentionUnknown()
                    lccClusterMessageText = "Attention Unknown"
                Case 10

                    'CeVGCR_e_LWI_DueToWeather()
                    lccClusterMessageText = "Due To Weather"
                Case 11

                    'CeVGCR_e_LWI_TakeStr()
                    lccClusterMessageText = "Take Steering Wheel"
                Case 12

                    'CeVGCR_e_LWI_TakeVehicleCtl()
                    lccClusterMessageText = "Take Vehicle Control"
                Case 13

                    'CeVGCR_e_LWI_ServDrvAsstSystm()
                    lccClusterMessageText = "Service Driver Assist System"
                Case 14

                    'CeVGCR_e_LWI_LnFollowingLckdOut()
                    lccClusterMessageText = "Lane Following Locked Out"
                Case 15
                    'CeVGCR_e_LWI_LaneEnding()
                    lccClusterMessageText = "Lane Ending"
                Case 16
                    'CeVGCR_e_LWI_ExitLane()
                    lccClusterMessageText = "Exit Lane"
                Case 17
                    'CeVGCR_e_LWI_GM_Authority()
                    lccClusterMessageText = "GM Authority"
                Case 18
                    'CeVGCR_e_LWI_VehicleSetting()
                    lccClusterMessageText = "Vehicle Setting"
                Case 19
                    'CeVGCR_e_LWI_AdaptiveCruise()
                    lccClusterMessageText = "Adaptive Cruise"
                Case 20
                    'CeVGCR_e_LWI_ControlledAccess()
                    lccClusterMessageText = "Controlled Access"
                Case 21
                    'CeVGCR_e_LWI_DrvrAttnOffRoad()
                    lccClusterMessageText = "Driver Attention Off Road"
                Case 22
                    'CeVGCR_e_LWI_GPS_Unavailable()
                    lccClusterMessageText = "GPS Unavailable"
                Case 23
                    'CeVGCR_e_LWI_DrvrAction()
                    lccClusterMessageText = "Driver Action"
                Case 24
                    'CeVGCR_e_LWI_VehicleCenter()
                    lccClusterMessageText = "Vehicle Center"
                Case 25
                    'CeVGCR_e_LWI_SensorImpaired()
                    lccClusterMessageText = "Sensor Impaired"
                Case Else

                    lccClusterMessageText = "Undefined"
            End Select

            If Len(lccClusterMessageText) > 0 Then

                messageDisplayed = True

                HandleUserMessageLogging("GMRC", CStr(lccClusterMsg) & ": " & lccClusterMessageText,,, FlashMsgOn)

                UserStatusInfo.Hide()
                UserStatusInfo.Show()
                UserStatusInfo.BringToFront()
                UserStatusInfo.Label1.Refresh()
                UserStatusInfo.Refresh()

                MySaveTime4 = DateTime.Now

            End If

        End If

        If messageDisplayed = True Then

            MyElapseTime4 = DateTime.Now.Subtract(MySaveTime4)

            If MyElapseTime4.Seconds >= 3 Then

                UserStatusInfo.Hide()

                messageDisplayed = False
            End If

        End If

    End Sub

    Private Async Sub HandleNanStatus(ByVal MyDg As GridDataClass)

        'Called from ProcessGrids (fire-and-forget async).
        'Alerts driver to a NAN fault without blocking the UI thread.

        ' Guard: If an alert is already in progress, don't start another
        If _nanAlertInProgress Then Return

        Dim Mytempval As Double
        Dim MyFormattedString As String

        If MyDg.CS_NAN_STATUS > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NAN_STATUS, 1)).SignalData

            'Mytempval = 1.0 For Testing...

            MyFormattedString = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NAN_STATUS, 1), MyDg.DeviceName(MyDg.CS_NAN_STATUS, 1), MyDg.VariableName(MyDg.CS_NAN_STATUS, 1))

            If InStr(UCase(MyFormattedString), "TRUE") > 0 And InSession = True Then

                ' Set guard flag and use non-blocking delay
                _nanAlertInProgress = True
                Try
                    Await Task.Delay(2500)
                    HandleUserMessageLogging("GMRC", "NAN ISSUE IS ACTIVE FOR THIS KEY CYCLE!  PLEASE PRESS STOP RECORD - EXIT - AND SLEEP OR SHUNT THE VEHICLE BEFORE CONTINUING!",,, FlashMsg5Sec)
                    UserStatusInfo.Label1.Text = ""
                    UserStatusInfo.Hide()
                Finally
                    _nanAlertInProgress = False
                End Try

            End If

        End If

    End Sub

    Private Sub HandleArrowDisplay(ByVal MycolorStr As String, ByVal MyDirection As String)

        'Called from HandleALCDisplayElements.  Displays indications related to Automatic Lane Change on the Top Down View...

        Select Case MyDirection

            Case "Left"

                Select Case MycolorStr

                    Case "Invisible"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Gray"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Green"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Red"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Yellow"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = False
                    Case "Blue"
                        MyTdGraphicsContainer.myPictureBoxGreenLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedLeftArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxBlueRightArrow.BringToFront()
                End Select

            Case "Right"

                Select Case MycolorStr

                    Case "Invisible"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Gray"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Green"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Red"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Yellow"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.BringToFront()
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = False
                    Case "Blue"
                        MyTdGraphicsContainer.myPictureBoxGreenRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxGrayRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxYellowRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxRedRightArrow.Visible = False
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.Visible = True
                        MyTdGraphicsContainer.myPictureBoxBlueLeftArrow.BringToFront()

                End Select

        End Select

    End Sub

    Private Sub HandleAlcDisplayElements(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks.
        'Handles display indications related to Automatic Lane Change on the Top Down View...

        'None
        'Driver
        'Merge
        'Route
        'Traffic

        'VeLFFR_e_LnChgDcsnRsn
        '0 -> 'CeLFFR_e_LnChgRsn_None'
        '1 -> 'CeLFFR_e_LnChgRsn_Driver'
        '2 -> 'CeLFFR_e_LnChgRsn_Merge'
        '3 -> 'CeLFFR_e_LnChgRsn_Route'
        '4 -> 'CeLFFR_e_LnChgRsn_Traffic'

        'VeLFFR_e_LnChgSt
        'Hex -> phys
        '0 -> 'CeLFFR_e_LnChg_Inactv' Invisible
        '1 -> 'CeLFFR_e_LnChg_Inhb'   Red
        '2 -> 'CeLFFR_e_LnChg_Standby'Gray
        '3 -> 'CeLFFR_e_LnChg_ChkLt' Yellow
        '4 -> 'CeLFFR_e_LnChg_ChkRt' Yellow
        '5 -> 'CeLFFR_e_LnChg_ChgLt' Green
        '6 -> 'CeLFFR_e_LnChg_ChgRt' Green
        '7 -> 'CeLFFR_e_LnChg_ReqCancel' Gray
        '8 -> 'CeLFFR_e_LnChg_NotFeasible' Red
        '9 -> 'CeLFFR_e_LnChg_ReqInvld' Red
        '10 -> 'CeLFFR_e_LnChg_Returning' Blue
        '11 -> 'CeLFFR_e_LnChg_Straddle'Red
        '12 -> 'CeLFFR_e_LnChg_DrvrOvrrd' Gray
        '13 -> 'CeLFFR_e_LnChg_Cmpt' Invisible

        Dim Mytempval As Double

        Static tempstr1 As String = "0"
        Dim tempstr2 As String = "0"
        Static whichSide As String = "None"

        Static reasonLeft As String = "NONE"
        Static reasonRight As String = "NONE"

        If MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN > 0 And MyDg.CS_ALC_LANE_CHANGE_STATE > 0 Then

            If MyTdGraphicsContainer.myALCStatusRightLabel.Visible = False Then

                MyTdGraphicsContainer.myALCStatusRightLabel.Visible = True
                MyTdGraphicsContainer.myALCStatusRightLabel.BringToFront()

                MyTdGraphicsContainer.myALCStatusLeftLabel.Visible = True
                MyTdGraphicsContainer.myALCStatusLeftLabel.BringToFront()

                MyTdGraphicsContainer.myALCReasonRightLabel.Visible = True
                MyTdGraphicsContainer.myALCReasonRightLabel.BringToFront()

                MyTdGraphicsContainer.myALCReasonLeftLabel.Visible = True
                MyTdGraphicsContainer.myALCReasonLeftLabel.BringToFront()

            End If

            If whichSide = "None" Then

                reasonLeft = "NONE"
                reasonRight = "NONE"

                MyTdGraphicsContainer.myALCReasonLeftLabel.Text = reasonLeft
                MyTdGraphicsContainer.myALCReasonRightLabel.Text = reasonRight

                Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1)).SignalData
                'Mytempval = GetRandom(0, 4) 'FOR TESTING ONLY!!!
                tempstr1 = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1), MyDg.DeviceName(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1), MyDg.VariableName(MyDg.CS_ALC_LANE_CHANGE_DCSN_RSN, 1))

            End If

            If InStr(tempstr1, "CeLFFR_e_LnChgRsn_None") > 0 Or tempstr1 = "0" Then

                If whichSide = "None" Then
                    reasonLeft = "NONE"
                    reasonRight = "NONE"
                    MyTdGraphicsContainer.myALCReasonLeftLabel.Text = reasonLeft
                    MyTdGraphicsContainer.myALCReasonRightLabel.Text = reasonRight
                End If

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Driver") > 0 Or tempstr1 = "1" Then

                reasonLeft = "DRIVER"
                reasonRight = "DRIVER"

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Merge") > 0 Or tempstr1 = "2" Then

                reasonLeft = "MERGE"
                reasonRight = "MERGE"

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Route") > 0 Or tempstr1 = "3" Then

                reasonLeft = "ROUTE"
                reasonRight = "ROUTE"

            ElseIf InStr(tempstr1, "CeLFFR_e_LnChgRsn_Traffic") > 0 Or tempstr1 = "4" Then

                reasonLeft = "TRAFFIC"
                reasonRight = "TRAFFIC"

            Else
                HandleUserMessageLogging("GMRC", "HandleALCDisplayElements: Invalid Enumeration for CS_ALC_LANE_CHANGE_DCSN_RSN", DisplayMsgBox)
            End If

            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ALC_LANE_CHANGE_STATE, 1)).SignalData
            'Mytempval = GetRandom(0, 13) 'FOR TESTING ONLY!!!
            tempstr2 = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ALC_LANE_CHANGE_STATE, 1), MyDg.DeviceName(MyDg.CS_ALC_LANE_CHANGE_STATE, 1), MyDg.VariableName(MyDg.CS_ALC_LANE_CHANGE_STATE, 1))

            If InStr(tempstr2, "CeLFFR_e_LnChg_Inactv") > 0 Then 'Invisible

                If whichSide = "None" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "IN-ACTIVE"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "IN-ACTIVE"

                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Inhb") > 0 Then 'Red

                If whichSide = "None" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "INHIBIT"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "INHIBIT"

                    HandleArrowDisplay("Red", "Left")
                    HandleArrowDisplay("Red", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Standby") > 0 Then 'Gray

                If whichSide = "None" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "STAND BY"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "STAND BY"

                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Gray", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChkLt") > 0 Then 'Yellow

                If whichSide <> "Right" Then

                    MyTdGraphicsContainer.myALCReasonRightLabel.Text = ""
                    MyTdGraphicsContainer.myALCReasonLeftLabel.Text = reasonLeft

                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "CHK LEFT"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = ""
                    whichSide = "Left"
                    HandleArrowDisplay("Yellow", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChkRt") > 0 Then 'Yellow

                If whichSide <> "Left" Then

                    MyTdGraphicsContainer.myALCReasonRightLabel.Text = reasonRight
                    MyTdGraphicsContainer.myALCReasonLeftLabel.Text = ""

                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = ""
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "CHK RIGHT"
                    whichSide = "Right"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Yellow", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChgLt") > 0 Then 'Green

                If whichSide <> "Right" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "CHG LEFT"
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = ""
                    whichSide = "Left"
                    HandleArrowDisplay("Green", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ChgRt") > 0 Then 'Green

                If whichSide <> "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = ""
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "CHG RIGHT"
                    whichSide = "Right"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Green", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ReqCancel") > 0 Then 'Gray

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "REQ CANCEL"
                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "REQ CANCEL"
                    HandleArrowDisplay("Gray", "Right")
                    HandleArrowDisplay("Invisible", "Left")
                End If

                whichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_NotFeasible") > 0 Then 'Red
                MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "NOT FEAS"
                MyTdGraphicsContainer.myALCStatusRightLabel.Text = "NOT FEAS"
                HandleArrowDisplay("Red", "Left")
                HandleArrowDisplay("Red", "Right")
                whichSide = "None"
            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_ReqInvld") > 0 Then 'Red

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "REQ INVALID"
                    HandleArrowDisplay("Red", "Left")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "REQ INVALID"
                    HandleArrowDisplay("Red", "Right")
                End If

                whichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Returning") > 0 Then 'Blue

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "RETURNING"
                    HandleArrowDisplay("Blue", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "RETURNING"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Blue", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Straddle") > 0 Then 'Red

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "STRADDLE"
                    HandleArrowDisplay("Red", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If

                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "STRADDLE"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Red", "Right")
                End If

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_DrvrOvrrd") > 0 Then 'Gray

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "DRVR OVRD"
                    HandleArrowDisplay("Gray", "Left")
                    HandleArrowDisplay("Invisible", "Right")
                End If
                If whichSide = "Right" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "DRVR OVRD"
                    HandleArrowDisplay("Invisible", "Left")
                    HandleArrowDisplay("Gray", "Right")
                End If

                whichSide = "None"

            ElseIf InStr(tempstr2, "CeLFFR_e_LnChg_Cmpt") > 0 Then 'Invisible

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusLeftLabel.Text = "COMPLETE"
                    HandleArrowDisplay("Invisible", "Left")
                End If

                If whichSide = "Left" Then
                    MyTdGraphicsContainer.myALCStatusRightLabel.Text = "COMPLETE"
                    HandleArrowDisplay("Invisible", "Right")
                End If

                whichSide = "None"
            Else
                HandleUserMessageLogging("GMRC", "HandleALCDisplayElements: Invalid Enumeration for CS_ALC_LANE_CHANGE_STATE", DisplayMsgBox)
                ' MsgBox("HandleALCDisplayElements: Invalid Enumeration for CS_ALC_LANE_CHANGE_STATE")
            End If

        End If

    End Sub

    Private Sub HandleCoPilotStatusDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks.
        'Handles copilot status display...

        Dim Mytempval As Double

        If MyDg.CS_K1C_COPR_SYSSTAT > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_K1C_COPR_SYSSTAT, 1)).SignalData
            CopilotStatusDisplay.Label3.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_K1C_COPR_SYSSTAT, 1), MyDg.DeviceName(MyDg.CS_K1C_COPR_SYSSTAT, 1), MyDg.VariableName(MyDg.CS_K1C_COPR_SYSSTAT, 1))
            CopilotStatusDisplay.Label3.BackColor = If(InStr(CopilotStatusDisplay.Label3.Text, "CeCOPR_e_Operational") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_K2C_COPR_SYSSTAT > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_K2C_COPR_SYSSTAT, 1)).SignalData
            CopilotStatusDisplay.Label4.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_K2C_COPR_SYSSTAT, 1), MyDg.DeviceName(MyDg.CS_K2C_COPR_SYSSTAT, 1), MyDg.VariableName(MyDg.CS_K2C_COPR_SYSSTAT, 1))
            CopilotStatusDisplay.Label4.BackColor = If(InStr(CopilotStatusDisplay.Label4.Text, "CeCOPR_e_Operational") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PriAutoBrkSysDrInfcStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriAutoBrkSysDrInfcStat, 1)).SignalData
            CopilotStatusDisplay.Label14.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PriAutoBrkSysDrInfcStat, 1), MyDg.DeviceName(MyDg.CS_PriAutoBrkSysDrInfcStat, 1), MyDg.VariableName(MyDg.CS_PriAutoBrkSysDrInfcStat, 1))
            CopilotStatusDisplay.Label14.BackColor = If(InStr(CopilotStatusDisplay.Label14.Text, "Driver Intervention Not Detected") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS__PriAutoBrkSysDrInfcStatRed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__PriAutoBrkSysDrInfcStatRed, 1)).SignalData
            CopilotStatusDisplay.Label13.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__PriAutoBrkSysDrInfcStatRed, 1), MyDg.DeviceName(MyDg.CS__PriAutoBrkSysDrInfcStatRed, 1), MyDg.VariableName(MyDg.CS__PriAutoBrkSysDrInfcStatRed, 1))
            CopilotStatusDisplay.Label13.BackColor = If(InStr(CopilotStatusDisplay.Label13.Text, "Driver Intervention Not Detected") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_SecAutoBrkSysDrInfcStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SecAutoBrkSysDrInfcStat, 1)).SignalData
            CopilotStatusDisplay.Label18.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_SecAutoBrkSysDrInfcStat, 1), MyDg.DeviceName(MyDg.CS_SecAutoBrkSysDrInfcStat, 1), MyDg.VariableName(MyDg.CS_SecAutoBrkSysDrInfcStat, 1))
            CopilotStatusDisplay.Label18.BackColor = If(InStr(CopilotStatusDisplay.Label18.Text, "Driver Intervention Not Detected") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS__SecAutoBrkSysDrInfcStatRed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__SecAutoBrkSysDrInfcStatRed, 1)).SignalData
            CopilotStatusDisplay.Label17.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__SecAutoBrkSysDrInfcStatRed, 1), MyDg.DeviceName(MyDg.CS__SecAutoBrkSysDrInfcStatRed, 1), MyDg.VariableName(MyDg.CS__SecAutoBrkSysDrInfcStatRed, 1))
            CopilotStatusDisplay.Label17.BackColor = If(InStr(CopilotStatusDisplay.Label17.Text, "Driver Intervention Not Detected") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_CE_AutoStrgCmndStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CE_AutoStrgCmndStat, 1)).SignalData
            CopilotStatusDisplay.Label21.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_CE_AutoStrgCmndStat, 1), MyDg.DeviceName(MyDg.CS_CE_AutoStrgCmndStat, 1), MyDg.VariableName(MyDg.CS_CE_AutoStrgCmndStat, 1))
            CopilotStatusDisplay.Label21.BackColor = If(InStr(CopilotStatusDisplay.Label21.Text, "Active") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_HS_AutoStrgCmndStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HS_AutoStrgCmndStat, 1)).SignalData
            CopilotStatusDisplay.Label26.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HS_AutoStrgCmndStat, 1), MyDg.DeviceName(MyDg.CS_HS_AutoStrgCmndStat, 1), MyDg.VariableName(MyDg.CS_HS_AutoStrgCmndStat, 1))
            CopilotStatusDisplay.Label26.BackColor = If(InStr(CopilotStatusDisplay.Label26.Text, "Active") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_AutoPropAxlTrqArbStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AutoPropAxlTrqArbStat, 1)).SignalData
            CopilotStatusDisplay.Label25.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AutoPropAxlTrqArbStat, 1), MyDg.DeviceName(MyDg.CS_AutoPropAxlTrqArbStat, 1), MyDg.VariableName(MyDg.CS_AutoPropAxlTrqArbStat, 1))
            CopilotStatusDisplay.Label25.BackColor = If(InStr(CopilotStatusDisplay.Label25.Text, "Active") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_AATPCS_PropSysStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AATPCS_PropSysStat, 1)).SignalData
            CopilotStatusDisplay.Label29.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AATPCS_PropSysStat, 1), MyDg.DeviceName(MyDg.CS_AATPCS_PropSysStat, 1), MyDg.VariableName(MyDg.CS_AATPCS_PropSysStat, 1))
            CopilotStatusDisplay.Label29.BackColor = If(InStr(CopilotStatusDisplay.Label29.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PriBrkSysStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriBrkSysStat, 1)).SignalData
            CopilotStatusDisplay.Label36.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PriBrkSysStat, 1), MyDg.DeviceName(MyDg.CS_PriBrkSysStat, 1), MyDg.VariableName(MyDg.CS_PriBrkSysStat, 1))
            CopilotStatusDisplay.Label36.BackColor = If(InStr(CopilotStatusDisplay.Label36.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS__PriBrkSysStatRed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__PriBrkSysStatRed, 1)).SignalData
            CopilotStatusDisplay.Label35.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__PriBrkSysStatRed, 1), MyDg.DeviceName(MyDg.CS__PriBrkSysStatRed, 1), MyDg.VariableName(MyDg.CS__PriBrkSysStatRed, 1))
            CopilotStatusDisplay.Label35.BackColor = If(InStr(CopilotStatusDisplay.Label35.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_SecBrkSysStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SecBrkSysStat, 1)).SignalData
            CopilotStatusDisplay.Label32.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_SecBrkSysStat, 1), MyDg.DeviceName(MyDg.CS_SecBrkSysStat, 1), MyDg.VariableName(MyDg.CS_SecBrkSysStat, 1))
            CopilotStatusDisplay.Label32.BackColor = If(InStr(CopilotStatusDisplay.Label32.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS__SecBrkSysStatRed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__SecBrkSysStatRed, 1)).SignalData
            CopilotStatusDisplay.Label31.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__SecBrkSysStatRed, 1), MyDg.DeviceName(MyDg.CS__SecBrkSysStatRed, 1), MyDg.VariableName(MyDg.CS__SecBrkSysStatRed, 1))
            CopilotStatusDisplay.Label31.BackColor = If(InStr(CopilotStatusDisplay.Label31.Text, "Full Function") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PriGdVolt > 0 Then
            CopilotStatusDisplay.Label44.Text = Format(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriGdVolt, 1)).SignalData, "0.00")
            CopilotStatusDisplay.Label44.BackColor = If(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriGdVolt, 1)).SignalData >= 13.6, Color.Green, Color.Red)
        End If

        If MyDg.CS_SecGdVolt > 0 Then
            CopilotStatusDisplay.Label43.Text = Format(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriGdVolt, 1)).SignalData, "0.00")
            CopilotStatusDisplay.Label43.BackColor = If(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SecGdVolt, 1)).SignalData >= 13.6, Color.Green, Color.Red)
        End If

        If MyDg.CS_SysPwrMd > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SysPwrMd, 1)).SignalData
            CopilotStatusDisplay.Label40.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_SysPwrMd, 1), MyDg.DeviceName(MyDg.CS_SysPwrMd, 1), MyDg.VariableName(MyDg.CS_SysPwrMd, 1))
            CopilotStatusDisplay.Label40.BackColor = If(InStr(CopilotStatusDisplay.Label40.Text, "Run") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PrplsnSysAtv > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PrplsnSysAtv, 1)).SignalData
            CopilotStatusDisplay.Label39.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PrplsnSysAtv, 1), MyDg.DeviceName(MyDg.CS_PrplsnSysAtv, 1), MyDg.VariableName(MyDg.CS_PrplsnSysAtv, 1))
            CopilotStatusDisplay.Label39.BackColor = If(InStr(CopilotStatusDisplay.Label39.Text, "true") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_CE_VehMdMngrSt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CE_VehMdMngrSt, 1)).SignalData
            CopilotStatusDisplay.Label52.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_CE_VehMdMngrSt, 1), MyDg.DeviceName(MyDg.CS_CE_VehMdMngrSt, 1), MyDg.VariableName(MyDg.CS_CE_VehMdMngrSt, 1))
            CopilotStatusDisplay.Label52.BackColor = If(InStr(CopilotStatusDisplay.Label52.Text, "Autonomous Supervised") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_AutoBrkSysRdcPerDet > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AutoBrkSysRdcPerDet, 1)).SignalData
            CopilotStatusDisplay.Label48.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AutoBrkSysRdcPerDet, 1), MyDg.DeviceName(MyDg.CS_AutoBrkSysRdcPerDet, 1), MyDg.VariableName(MyDg.CS_AutoBrkSysRdcPerDet, 1))
            CopilotStatusDisplay.Label48.BackColor = If(InStr(CopilotStatusDisplay.Label48.Text, "false") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_ADIMCntrlFailed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ADIMCntrlFailed, 1)).SignalData
            CopilotStatusDisplay.Label47.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ADIMCntrlFailed, 1), MyDg.DeviceName(MyDg.CS_ADIMCntrlFailed, 1), MyDg.VariableName(MyDg.CS_ADIMCntrlFailed, 1))
            CopilotStatusDisplay.Label47.BackColor = If(InStr(CopilotStatusDisplay.Label47.Text, "false") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_RedVehMdMngrSt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RedVehMdMngrSt, 1)).SignalData
            CopilotStatusDisplay.Label64.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RedVehMdMngrSt, 1), MyDg.DeviceName(MyDg.CS_RedVehMdMngrSt, 1), MyDg.VariableName(MyDg.CS_RedVehMdMngrSt, 1))
            CopilotStatusDisplay.Label64.BackColor = If(InStr(CopilotStatusDisplay.Label64.Text, "Autonomous Supervised") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_PriCoPCmdMsgStat > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PriCoPCmdMsgStat, 1)).SignalData
            CopilotStatusDisplay.Label23.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PriCoPCmdMsgStat, 1), MyDg.DeviceName(MyDg.CS_PriCoPCmdMsgStat, 1), MyDg.VariableName(MyDg.CS_PriCoPCmdMsgStat, 1))
            CopilotStatusDisplay.Label23.BackColor = If(InStr(CopilotStatusDisplay.Label23.Text, "Communication Normal") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_RedAutoBrkSysRdcPerDet > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RedAutoBrkSysRdcPerDet, 1)).SignalData
            CopilotStatusDisplay.Label60.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RedAutoBrkSysRdcPerDet, 1), MyDg.DeviceName(MyDg.CS_RedAutoBrkSysRdcPerDet, 1), MyDg.VariableName(MyDg.CS_RedAutoBrkSysRdcPerDet, 1))
            CopilotStatusDisplay.Label60.BackColor = If(InStr(CopilotStatusDisplay.Label60.Text, "false") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_RedADIMCntrlFailed > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RedADIMCntrlFailed, 1)).SignalData
            CopilotStatusDisplay.Label59.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RedADIMCntrlFailed, 1), MyDg.DeviceName(MyDg.CS_RedADIMCntrlFailed, 1), MyDg.VariableName(MyDg.CS_RedADIMCntrlFailed, 1))
            CopilotStatusDisplay.Label59.BackColor = If(InStr(CopilotStatusDisplay.Label59.Text, "false") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_VeTSTR_e_HiThreatObjType > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VeTSTR_e_HiThreatObjType, 1)).SignalData
            CopilotStatusDisplay.Label58.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_VeTSTR_e_HiThreatObjType, 1), MyDg.DeviceName(MyDg.CS_VeTSTR_e_HiThreatObjType, 1), MyDg.VariableName(MyDg.CS_VeTSTR_e_HiThreatObjType, 1))
            CopilotStatusDisplay.Label58.BackColor = If(InStr(CopilotStatusDisplay.Label58.Text, "CeFSPR_e_4_Whl_Vhcl_car_sm_trk") = 0, Color.Red, Color.Green)
        End If

        If MyDg.CS_VeTSTR_t_HiThreatTTC > 0 Then
            CopilotStatusDisplay.Label24.Text = Format(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VeTSTR_t_HiThreatTTC, 1)).SignalData, "0.00")
            CopilotStatusDisplay.Label24.BackColor = If(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VeTSTR_t_HiThreatTTC, 1)).SignalData < 10, Color.Green, Color.Red)
        End If
    End Sub

    Private Sub HandleLmfrStatusScreenHc(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles LMFR Status Screen display for High Content vehicles...

        Dim Mytempval As Double

        If MyDg.CS_HostLaneInx > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneInx, 1)).SignalData
            LmfrStatusScreenHc.Label20.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneInx, 1), MyDg.DeviceName(MyDg.CS_HostLaneInx, 1), MyDg.VariableName(MyDg.CS_HostLaneInx, 1))
        End If

        If MyDg.CS_RawLaneLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneLeft, 1)).SignalData
            LmfrStatusScreenHc.Label29.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneLeft, 1), MyDg.DeviceName(MyDg.CS_RawLaneLeft, 1), MyDg.VariableName(MyDg.CS_RawLaneLeft, 1))
            LmfrStatusScreenHc.Label29.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label29.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_RawLaneRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneRight, 1)).SignalData
            LmfrStatusScreenHc.Label30.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneRight, 1), MyDg.DeviceName(MyDg.CS_RawLaneRight, 1), MyDg.VariableName(MyDg.CS_RawLaneRight, 1))
            LmfrStatusScreenHc.Label30.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label30.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_AnchorSelect > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AnchorSelect, 1)).SignalData
            LmfrStatusScreenHc.Label9.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AnchorSelect, 1), MyDg.DeviceName(MyDg.CS_AnchorSelect, 1), MyDg.VariableName(MyDg.CS_AnchorSelect, 1))
            LmfrStatusScreenHc.Label9.BackColor = If(InStr(LmfrStatusScreenHc.Label9.Text, "CeLMFR_e_NoAnchor") = 0, Color.Green, Color.White)
        End If

        If MyDg.CS_LaneInvalid > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneInvalid, 1)).SignalData
            LmfrStatusScreenHc.Label54.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneInvalid, 1), MyDg.DeviceName(MyDg.CS_LaneInvalid, 1), MyDg.VariableName(MyDg.CS_LaneInvalid, 1))
            LmfrStatusScreenHc.Label54.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label54.Text), "TRUE") > 0, Color.Red, Color.White)
        End If

        If MyDg.CS_AlertUncertainLnLines > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertUncertainLnLines, 1)).SignalData
            LmfrStatusScreenHc.Label13.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertUncertainLnLines, 1), MyDg.DeviceName(MyDg.CS_AlertUncertainLnLines, 1), MyDg.VariableName(MyDg.CS_AlertUncertainLnLines, 1))
            LmfrStatusScreenHc.Label13.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label13.Text), "TRUE") > 0, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneWgtLt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtLt, 1)).SignalData
            LmfrStatusScreenHc.Label57.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtLt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtLt, 1), MyDg.VariableName(MyDg.CS_LaneWgtLt, 1))
            LmfrStatusScreenHc.Label57.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneWgtRt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtRt, 1)).SignalData
            LmfrStatusScreenHc.Label56.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtRt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtRt, 1), MyDg.VariableName(MyDg.CS_LaneWgtRt, 1))
            LmfrStatusScreenHc.Label56.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_HPP_Wgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HPP_Wgt, 1)).SignalData
            LmfrStatusScreenHc.Label55.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HPP_Wgt, 1), MyDg.DeviceName(MyDg.CS_HPP_Wgt, 1), MyDg.VariableName(MyDg.CS_HPP_Wgt, 1))
            LmfrStatusScreenHc.Label55.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_PrevCoefWgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PrevCoefWgt, 1)).SignalData
            LmfrStatusScreenHc.Label7.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PrevCoefWgt, 1), MyDg.DeviceName(MyDg.CS_PrevCoefWgt, 1), MyDg.VariableName(MyDg.CS_PrevCoefWgt, 1))
            LmfrStatusScreenHc.Label7.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_MapWgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_MapWgt, 1)).SignalData
            LmfrStatusScreenHc.Label5.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_MapWgt, 1), MyDg.DeviceName(MyDg.CS_MapWgt, 1), MyDg.VariableName(MyDg.CS_MapWgt, 1))
            LmfrStatusScreenHc.Label5.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_IFC_HeadingWgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_IFC_HeadingWgt, 1)).SignalData
            LmfrStatusScreenHc.Label14.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_IFC_HeadingWgt, 1), MyDg.DeviceName(MyDg.CS_IFC_HeadingWgt, 1), MyDg.VariableName(MyDg.CS_IFC_HeadingWgt, 1))
            LmfrStatusScreenHc.Label14.BackColor = If(Mytempval > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_IMU_BlueLine > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_IMU_BlueLine, 1)).SignalData
            LmfrStatusScreenHc.Label11.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_IMU_BlueLine, 1), MyDg.DeviceName(MyDg.CS_IMU_BlueLine, 1), MyDg.VariableName(MyDg.CS_IMU_BlueLine, 1))
            LmfrStatusScreenHc.Label29.BackColor = If(InStr(UCase(LmfrStatusScreenHc.Label11.Text), "TRUE") > 0, Color.Green, Color.White)
        End If
    End Sub

    Private Sub HandleLmfrStatusScreenGlobalA(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles LMFR Status Screen display for Global A (CSAV2) vehicles...

        Dim Mytempval As Double

        Dim y0 As Double = 0
        Dim y1 As Double = 0
        Dim y2 As Double = 0
        Dim y3 As Double = 0

        If Oscilloscope.Visible = True Then
            If MyDg.CS_CntrlPtLatOffsetHPP > 0 Then y0 = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CntrlPtLatOffsetHPP, 1)).SignalData
            If MyDg.CS_CntrlPtLatOffsetL > 0 Then y1 = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CntrlPtLatOffsetL, 1)).SignalData
            If MyDg.CS_CntrlPtLatOffsetR > 0 Then y2 = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CntrlPtLatOffsetR, 1)).SignalData
            If MyDg.CS_CntrlPtLatOffsetPrev > 0 Then y3 = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CntrlPtLatOffsetPrev, 1)).SignalData

            If Debugger.IsAttached Then

                y0 = Oscilloscope.GetRandom(-2.0, 2.0)
                y1 = Oscilloscope.GetRandom(-2.0, 2.0)
                y2 = Oscilloscope.GetRandom(-2.0, 2.0)
                y3 = Oscilloscope.GetRandom(-2.0, 2.0)

            End If

            Oscilloscope.Chart1.Series(0).Points.AddXY(_chartXIncrement, y0)
            Oscilloscope.Chart1.Series(1).Points.AddXY(_chartXIncrement, y1)
            Oscilloscope.Chart1.Series(2).Points.AddXY(_chartXIncrement, y2)
            Oscilloscope.Chart1.Series(3).Points.AddXY(_chartXIncrement, y3)

            If (Oscilloscope.Chart1.Series(0).Points.Count > 200) Then Oscilloscope.Chart1.Series(0).Points.RemoveAt(0)
            If (Oscilloscope.Chart1.Series(1).Points.Count > 200) Then Oscilloscope.Chart1.Series(1).Points.RemoveAt(0)
            If (Oscilloscope.Chart1.Series(2).Points.Count > 200) Then Oscilloscope.Chart1.Series(2).Points.RemoveAt(0)
            If (Oscilloscope.Chart1.Series(3).Points.Count > 200) Then Oscilloscope.Chart1.Series(3).Points.RemoveAt(0)

            Oscilloscope.Chart1.ChartAreas(0).AxisX.Minimum = Oscilloscope.Chart1.Series(0).Points(0).XValue
            Oscilloscope.Chart1.ChartAreas(0).AxisX.Maximum = _chartXIncrement
            _chartXIncrement += 0.05
        Else
            _chartXIncrement = 0
        End If

        If MyDg.CS_AtGradeAnchor > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AtGradeAnchor, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label31.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AtGradeAnchor, 1), MyDg.DeviceName(MyDg.CS_AtGradeAnchor, 1), MyDg.VariableName(MyDg.CS_AtGradeAnchor, 1))
        End If

        If MyDg.CS_HostLaneIndexLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneIndexLeft, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label24.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneIndexLeft, 1), MyDg.DeviceName(MyDg.CS_HostLaneIndexLeft, 1), MyDg.VariableName(MyDg.CS_HostLaneIndexLeft, 1))
        End If

        If MyDg.CS_HostLaneIndexRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneIndexRight, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label23.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneIndexRight, 1), MyDg.DeviceName(MyDg.CS_HostLaneIndexRight, 1), MyDg.VariableName(MyDg.CS_HostLaneIndexRight, 1))
        End If

        If MyDg.CS_MapHostLaneIndex > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_MapHostLaneIndex, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label22.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_MapHostLaneIndex, 1), MyDg.DeviceName(MyDg.CS_MapHostLaneIndex, 1), MyDg.VariableName(MyDg.CS_MapHostLaneIndex, 1))
        End If

        If MyDg.CS_TargetsHostLaneIndex > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_TargetsHostLaneIndex, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label21.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_TargetsHostLaneIndex, 1), MyDg.DeviceName(MyDg.CS_TargetsHostLaneIndex, 1), MyDg.VariableName(MyDg.CS_TargetsHostLaneIndex, 1))
        End If

        If MyDg.CS_DistToNextAtGradeXing > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNextAtGradeXing, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label49.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNextAtGradeXing, 1), MyDg.DeviceName(MyDg.CS_DistToNextAtGradeXing, 1), MyDg.VariableName(MyDg.CS_DistToNextAtGradeXing, 1))
        End If

        If MyDg.CS_DistToRoadClassTrans > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToRoadClassTrans, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label52.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToRoadClassTrans, 1), MyDg.DeviceName(MyDg.CS_DistToRoadClassTrans, 1), MyDg.VariableName(MyDg.CS_DistToRoadClassTrans, 1))
        End If

        If MyDg.CS_DistToNextTrfcCntrDev > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNextTrfcCntrDev, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label53.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNextTrfcCntrDev, 1), MyDg.DeviceName(MyDg.CS_DistToNextTrfcCntrDev, 1), MyDg.VariableName(MyDg.CS_DistToNextTrfcCntrDev, 1))
        End If

        If MyDg.CS_OnFreeway > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_OnFreeway, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label42.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_OnFreeway, 1), MyDg.DeviceName(MyDg.CS_OnFreeway, 1), MyDg.VariableName(MyDg.CS_OnFreeway, 1))
            LmfrStatusDisplayGlobalA.Label42.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label42.Text), "TRUE") > 0, Color.Green, Color.Red)
        End If

        If MyDg.CS_RoadClass_Crnt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RoadClass_Crnt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label41.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RoadClass_Crnt, 1), MyDg.DeviceName(MyDg.CS_RoadClass_Crnt, 1), MyDg.VariableName(MyDg.CS_RoadClass_Crnt, 1))
        End If

        If MyDg.CS_CmplxIntrsct_Prsnt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CmplxIntrsct_Prsnt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label43.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_CmplxIntrsct_Prsnt, 1), MyDg.DeviceName(MyDg.CS_CmplxIntrsct_Prsnt, 1), MyDg.VariableName(MyDg.CS_CmplxIntrsct_Prsnt, 1))
            LmfrStatusDisplayGlobalA.Label43.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label43.Text), "TRUE") > 0, Color.Red, Color.White)
        End If

        If MyDg.CS_HostLaneInx > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneInx, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label17.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneInx, 1), MyDg.DeviceName(MyDg.CS_HostLaneInx, 1), MyDg.VariableName(MyDg.CS_HostLaneInx, 1))
        End If


        If MyDg.CS_NumLanes > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NumLanes, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label18.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NumLanes, 1), MyDg.DeviceName(MyDg.CS_NumLanes, 1), MyDg.VariableName(MyDg.CS_NumLanes, 1))

        End If

        If MyDg.CS_NextNumLanes > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NextNumLanes, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label19.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NextNumLanes, 1), MyDg.DeviceName(MyDg.CS_NextNumLanes, 1), MyDg.VariableName(MyDg.CS_NextNumLanes, 1))

        End If

        If MyDg.CS_HostLaneProbMax > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneProbMax, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label20.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneProbMax, 1), MyDg.DeviceName(MyDg.CS_HostLaneProbMax, 1), MyDg.VariableName(MyDg.CS_HostLaneProbMax, 1))
        End If

        If MyDg.CS_RawLaneLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneLeft, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label29.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneLeft, 1), MyDg.DeviceName(MyDg.CS_RawLaneLeft, 1), MyDg.VariableName(MyDg.CS_RawLaneLeft, 1))
            LmfrStatusDisplayGlobalA.Label29.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label29.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_RawLaneRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneRight, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label30.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneRight, 1), MyDg.DeviceName(MyDg.CS_RawLaneRight, 1), MyDg.VariableName(MyDg.CS_RawLaneRight, 1))
            LmfrStatusDisplayGlobalA.Label30.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label30.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_IntersectSplitMerge > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_IntersectSplitMerge, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label47.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_IntersectSplitMerge, 1), MyDg.DeviceName(MyDg.CS_IntersectSplitMerge, 1), MyDg.VariableName(MyDg.CS_IntersectSplitMerge, 1))
        End If

        If MyDg.CS_DistToNumLanesTrans > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNumLanesTrans, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label50.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNumLanesTrans, 1), MyDg.DeviceName(MyDg.CS_DistToNumLanesTrans, 1), MyDg.VariableName(MyDg.CS_DistToNumLanesTrans, 1))
        End If

        If MyDg.CS_LCC_RedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LCC_RedReq, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label35.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LCC_RedReq, 1), MyDg.DeviceName(MyDg.CS_LCC_RedReq, 1), MyDg.VariableName(MyDg.CS_LCC_RedReq, 1))
            LmfrStatusDisplayGlobalA.Label35.BackColor = If(InStr(LmfrStatusDisplayGlobalA.Label35.Text, "CeVGCR_e_LCRR_Inactive") = 0, Color.Red, Color.White)
        End If

        If MyDg.CS_LnWgtRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnWgtRedReq, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label34.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnWgtRedReq, 1), MyDg.DeviceName(MyDg.CS_LnWgtRedReq, 1), MyDg.VariableName(MyDg.CS_LnWgtRedReq, 1))
            LmfrStatusDisplayGlobalA.Label34.BackColor = If(InStr(LmfrStatusDisplayGlobalA.Label34.Text, "CeVGCR_e_LCRR_Inactive") = 0, Color.Red, Color.White)
        End If

        If MyDg.CS_MapAvailRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_MapAvailRedReq, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label33.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_MapAvailRedReq, 1), MyDg.DeviceName(MyDg.CS_MapAvailRedReq, 1), MyDg.VariableName(MyDg.CS_MapAvailRedReq, 1))
            LmfrStatusDisplayGlobalA.Label33.BackColor = If(InStr(LmfrStatusDisplayGlobalA.Label33.Text, "CeVGCR_e_LCRR_Inactive") = 0, Color.Red, Color.White)
        End If

        If MyDg.CS_TmpLnRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_TmpLnRedReq, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label32.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_TmpLnRedReq, 1), MyDg.DeviceName(MyDg.CS_TmpLnRedReq, 1), MyDg.VariableName(MyDg.CS_TmpLnRedReq, 1))
            LmfrStatusDisplayGlobalA.Label32.BackColor = If(InStr(LmfrStatusDisplayGlobalA.Label32.Text, "CeVGCR_e_LCRR_Inactive") = 0, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneWgtLt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtLt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label57.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtLt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtLt, 1), MyDg.VariableName(MyDg.CS_LaneWgtLt, 1))
            LmfrStatusDisplayGlobalA.Label57.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneWgtRt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtRt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label56.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtRt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtRt, 1), MyDg.VariableName(MyDg.CS_LaneWgtRt, 1))
            LmfrStatusDisplayGlobalA.Label56.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_HPP_Wgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HPP_Wgt, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label55.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HPP_Wgt, 1), MyDg.DeviceName(MyDg.CS_HPP_Wgt, 1), MyDg.VariableName(MyDg.CS_HPP_Wgt, 1))
            LmfrStatusDisplayGlobalA.Label55.BackColor = If(Mytempval < 0.1, Color.Red, Color.White)
        End If

        If MyDg.CS_LaneInvalid > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneInvalid, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label54.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneInvalid, 1), MyDg.DeviceName(MyDg.CS_LaneInvalid, 1), MyDg.VariableName(MyDg.CS_LaneInvalid, 1))
            LmfrStatusDisplayGlobalA.Label54.BackColor = If(InStr(UCase(LmfrStatusDisplayGlobalA.Label56.Text), "TRUE") > 0, Color.Red, Color.White)
        End If

        If MyDg.CS_DistToNextIntersect > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNextIntersect, 1)).SignalData
            LmfrStatusDisplayGlobalA.Label48.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNextIntersect, 1), MyDg.DeviceName(MyDg.CS_DistToNextIntersect, 1), MyDg.VariableName(MyDg.CS_DistToNextIntersect, 1))
        End If
    End Sub

    Private Sub HandleFusionStatusDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles Fusion Status Screen display...

        Dim Mytempval As Double

        If MyDg.CS_HostLaneInx > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneInx, 1)).SignalData
            FusionStatusDisplay.Label7.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneInx, 1), MyDg.DeviceName(MyDg.CS_HostLaneInx, 1), MyDg.VariableName(MyDg.CS_HostLaneInx, 1))
        End If


        If MyDg.CS_NumLanes > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NumLanes, 1)).SignalData
            FusionStatusDisplay.Label21.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NumLanes, 1), MyDg.DeviceName(MyDg.CS_NumLanes, 1), MyDg.VariableName(MyDg.CS_NumLanes, 1))

        End If

        If MyDg.CS_NextNumLanes > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NextNumLanes, 1)).SignalData
            FusionStatusDisplay.Label23.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_NextNumLanes, 1), MyDg.DeviceName(MyDg.CS_NextNumLanes, 1), MyDg.VariableName(MyDg.CS_NextNumLanes, 1))

        End If

        If MyDg.CS_HostLaneProbMax > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HostLaneProbMax, 1)).SignalData
            FusionStatusDisplay.Label48.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HostLaneProbMax, 1), MyDg.DeviceName(MyDg.CS_HostLaneProbMax, 1), MyDg.VariableName(MyDg.CS_HostLaneProbMax, 1))
        End If

        If MyDg.CS_RawLaneLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneLeft, 1)).SignalData
            FusionStatusDisplay.Label32.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneLeft, 1), MyDg.DeviceName(MyDg.CS_RawLaneLeft, 1), MyDg.VariableName(MyDg.CS_RawLaneLeft, 1))
        End If

        If MyDg.CS_RawLaneRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_RawLaneRight, 1)).SignalData
            FusionStatusDisplay.Label30.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_RawLaneRight, 1), MyDg.DeviceName(MyDg.CS_RawLaneRight, 1), MyDg.VariableName(MyDg.CS_RawLaneRight, 1))
        End If

        If MyDg.CS_IntersectSplitMerge > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_IntersectSplitMerge, 1)).SignalData
            FusionStatusDisplay.Label15.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_IntersectSplitMerge, 1), MyDg.DeviceName(MyDg.CS_IntersectSplitMerge, 1), MyDg.VariableName(MyDg.CS_IntersectSplitMerge, 1))
        End If

        If MyDg.CS_DistToNumLanesTrans > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNumLanesTrans, 1)).SignalData
            FusionStatusDisplay.Label27.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNumLanesTrans, 1), MyDg.DeviceName(MyDg.CS_DistToNumLanesTrans, 1), MyDg.VariableName(MyDg.CS_DistToNumLanesTrans, 1))
        End If

        If MyDg.CS_LCC_RedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LCC_RedReq, 1)).SignalData
            FusionStatusDisplay.Label46.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LCC_RedReq, 1), MyDg.DeviceName(MyDg.CS_LCC_RedReq, 1), MyDg.VariableName(MyDg.CS_LCC_RedReq, 1))
        End If

        If MyDg.CS_LnWgtRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnWgtRedReq, 1)).SignalData
            FusionStatusDisplay.Label42.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnWgtRedReq, 1), MyDg.DeviceName(MyDg.CS_LnWgtRedReq, 1), MyDg.VariableName(MyDg.CS_LnWgtRedReq, 1))

        End If

        If MyDg.CS_MapAvailRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_MapAvailRedReq, 1)).SignalData
            FusionStatusDisplay.Label40.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_MapAvailRedReq, 1), MyDg.DeviceName(MyDg.CS_MapAvailRedReq, 1), MyDg.VariableName(MyDg.CS_MapAvailRedReq, 1))
        End If

        If MyDg.CS_TmpLnRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_TmpLnRedReq, 1)).SignalData
            FusionStatusDisplay.Label36.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_TmpLnRedReq, 1), MyDg.DeviceName(MyDg.CS_TmpLnRedReq, 1), MyDg.VariableName(MyDg.CS_TmpLnRedReq, 1))
        End If

        If MyDg.CS_LaneWgtLt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtLt, 1)).SignalData
            FusionStatusDisplay.Label54.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtLt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtLt, 1), MyDg.VariableName(MyDg.CS_LaneWgtLt, 1))
        End If

        If MyDg.CS_LaneWgtRt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneWgtRt, 1)).SignalData
            FusionStatusDisplay.Label52.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneWgtRt, 1), MyDg.DeviceName(MyDg.CS_LaneWgtRt, 1), MyDg.VariableName(MyDg.CS_LaneWgtRt, 1))
        End If

        If MyDg.CS_HPP_Wgt > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HPP_Wgt, 1)).SignalData
            FusionStatusDisplay.Label50.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HPP_Wgt, 1), MyDg.DeviceName(MyDg.CS_HPP_Wgt, 1), MyDg.VariableName(MyDg.CS_HPP_Wgt, 1))
        End If

        If MyDg.CS_LaneInvalid > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LaneInvalid, 1)).SignalData
            FusionStatusDisplay.Label56.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LaneInvalid, 1), MyDg.DeviceName(MyDg.CS_LaneInvalid, 1), MyDg.VariableName(MyDg.CS_LaneInvalid, 1))
        End If

        If MyDg.CS_DistToNextIntersect > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_DistToNextIntersect, 1)).SignalData
            FusionStatusDisplay.Label17.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_DistToNextIntersect, 1), MyDg.DeviceName(MyDg.CS_DistToNextIntersect, 1), MyDg.VariableName(MyDg.CS_DistToNextIntersect, 1))
        End If

        If MyDg.CS_HCURVE > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_HCURVE, 1)).SignalData
            FusionStatusDisplay.Label9.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_HCURVE, 1), MyDg.DeviceName(MyDg.CS_HCURVE, 1), MyDg.VariableName(MyDg.CS_HCURVE, 1))
        End If

        If MyDg.CS_VehPathEstCurv > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VehPathEstCurv, 1)).SignalData
            FusionStatusDisplay.Label11.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_VehPathEstCurv, 1), MyDg.DeviceName(MyDg.CS_VehPathEstCurv, 1), MyDg.VariableName(MyDg.CS_VehPathEstCurv, 1))
        End If

        If MyDg.CS_Curvature > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_Curvature, 1)).SignalData
            FusionStatusDisplay.Label13.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_Curvature, 1), MyDg.DeviceName(MyDg.CS_Curvature, 1), MyDg.VariableName(MyDg.CS_Curvature, 1))
        End If

        If MyDg.CS_SpltMrgLaneNum > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_SpltMrgLaneNum, 1)).SignalData
            FusionStatusDisplay.Label19.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_SpltMrgLaneNum, 1), MyDg.DeviceName(MyDg.CS_SpltMrgLaneNum, 1), MyDg.VariableName(MyDg.CS_SpltMrgLaneNum, 1))
        End If

        If MyDg.CS__NumLanesTrans > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS__NumLanesTrans, 1)).SignalData
            FusionStatusDisplay.Label25.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS__NumLanesTrans, 1), MyDg.DeviceName(MyDg.CS__NumLanesTrans, 1), MyDg.VariableName(MyDg.CS__NumLanesTrans, 1))
        End If

        If MyDg.CS_PathConf > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PathConf, 1)).SignalData
            FusionStatusDisplay.Label8.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PathConf, 1), MyDg.DeviceName(MyDg.CS_PathConf, 1), MyDg.VariableName(MyDg.CS_PathConf, 1))
        End If

        If MyDg.CS_LnSplitRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnSplitRedReq, 1)).SignalData
            FusionStatusDisplay.Label44.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnSplitRedReq, 1), MyDg.DeviceName(MyDg.CS_LnSplitRedReq, 1), MyDg.VariableName(MyDg.CS_LnSplitRedReq, 1))
        End If

        If MyDg.CS_PathConfRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PathConfRedReq, 1)).SignalData
            FusionStatusDisplay.Label38.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_PathConfRedReq, 1), MyDg.DeviceName(MyDg.CS_PathConfRedReq, 1), MyDg.VariableName(MyDg.CS_PathConfRedReq, 1))
        End If

        If MyDg.CS_VPMConfRedReq > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_VPMConfRedReq, 1)).SignalData
            FusionStatusDisplay.Label34.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_VPMConfRedReq, 1), MyDg.DeviceName(MyDg.CS_VPMConfRedReq, 1), MyDg.VariableName(MyDg.CS_VPMConfRedReq, 1))
        End If

        If MyDg.CS_AlertUncertainLnLines > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertUncertainLnLines, 1)).SignalData
            FusionStatusDisplay.Label59.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertUncertainLnLines, 1), MyDg.DeviceName(MyDg.CS_AlertUncertainLnLines, 1), MyDg.VariableName(MyDg.CS_AlertUncertainLnLines, 1))
        End If

        If MyDg.CS_AlertExitLane > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertExitLane, 1)).SignalData
            FusionStatusDisplay.Label61.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertExitLane, 1), MyDg.DeviceName(MyDg.CS_AlertExitLane, 1), MyDg.VariableName(MyDg.CS_AlertExitLane, 1))
        End If

        If MyDg.CS_AlertLaneEnding > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertLaneEnding, 1)).SignalData
            FusionStatusDisplay.Label63.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertLaneEnding, 1), MyDg.DeviceName(MyDg.CS_AlertLaneEnding, 1), MyDg.VariableName(MyDg.CS_AlertLaneEnding, 1))
        End If

        If MyDg.CS_AlertMapUnavail > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertMapUnavail, 1)).SignalData
            FusionStatusDisplay.Label65.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertMapUnavail, 1), MyDg.DeviceName(MyDg.CS_AlertMapUnavail, 1), MyDg.VariableName(MyDg.CS_AlertMapUnavail, 1))
        End If

        If MyDg.CS_AlertOther > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AlertOther, 1)).SignalData
            FusionStatusDisplay.Label67.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_AlertOther, 1), MyDg.DeviceName(MyDg.CS_AlertOther, 1), MyDg.VariableName(MyDg.CS_AlertOther, 1))
        End If

        If MyDg.CS_LnNarrowing > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnNarrowing, 1)).SignalData
            FusionStatusDisplay.Label69.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnNarrowing, 1), MyDg.DeviceName(MyDg.CS_LnNarrowing, 1), MyDg.VariableName(MyDg.CS_LnNarrowing, 1))
        End If

        If MyDg.CS_LnWidening > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LnWidening, 1)).SignalData
            FusionStatusDisplay.Label71.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_LnWidening, 1), MyDg.DeviceName(MyDg.CS_LnWidening, 1), MyDg.VariableName(MyDg.CS_LnWidening, 1))
        End If

        If MyDg.CS_ObjectLeft > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ObjectLeft, 1)).SignalData
            FusionStatusDisplay.Label75.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ObjectLeft, 1), MyDg.DeviceName(MyDg.CS_ObjectLeft, 1), MyDg.VariableName(MyDg.CS_ObjectLeft, 1))
            FusionStatusDisplay.Label75.BackColor = If(InStr(UCase(FusionStatusDisplay.Label75.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

        If MyDg.CS_ObjectRight > 0 Then
            Mytempval = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ObjectRight, 1)).SignalData
            FusionStatusDisplay.Label73.Text = FormatDisplayString(Mytempval, MyDg.DisplayFormat(MyDg.CS_ObjectRight, 1), MyDg.DeviceName(MyDg.CS_ObjectRight, 1), MyDg.VariableName(MyDg.CS_ObjectRight, 1))
            FusionStatusDisplay.Label73.BackColor = If(InStr(UCase(FusionStatusDisplay.Label73.Text), "TRUE") > 0, Color.Green, Color.White)
        End If

    End Sub

    Private Sub HandleLkaCustomDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles LKA Status Screen display...


        If MyDg.CS_LNSNS_DISTTOLLNEDGE > 0 And MyDg.CS_LNSNS_DISTTORLNEDGE > 0 Then

            _saveLkaDistRtLnEdge = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LNSNS_DISTTORLNEDGE, 1)).SignalData
            _saveLkaDistLtLnEdge = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LNSNS_DISTTOLLNEDGE, 1)).SignalData

        End If

        If LkaForm.LkaActive = True Then
            LkaForm.Label17.Text = "LKA TORQ"
            LkaForm.Label17.BackColor = Color.Green
        Else
            LkaForm.Label17.Text = "NO LKA TORQ"
            LkaForm.Label17.BackColor = Color.Red
        End If

        If MyDg.CS_LKA_OFFIND > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_OFFIND, 1)).SignalData = 0 Then
                LkaForm.Label20.Text = "LKA ON"
                LkaForm.Label20.BackColor = Color.Green
            Else
                LkaForm.Label20.Text = "LKA OFF"
                LkaForm.Label20.BackColor = Color.Red
            End If

        End If

        If MyDg.CS_LKA_STDBYIND > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_STDBYIND, 1)).SignalData > 0 Then
                LkaForm.Label19.Text = "LKA IN STDBY"
                LkaForm.Label19.BackColor = Color.Green
            Else
                LkaForm.Label19.Text = "LKA STDBY OFF"
                LkaForm.Label19.BackColor = Color.Red
            End If

        End If

        If MyDg.CS_LKA_ACTVIND > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_ACTVIND, 1)).SignalData > 0 Then
                LkaForm.Label18.Text = "LKA ACTV"
                LkaForm.Label18.BackColor = Color.Green
            Else
                LkaForm.Label18.Text = "LKA NOT ACTV"
                LkaForm.Label18.BackColor = Color.Red
            End If

        End If

        If MyDg.CS_LKA_DRVRAPPLDTRQ > 0 And MyDg.CS_LKA_TORQUE > 0 And MyDg.CS_LKA_TRQRQACT > 0 Then
            If LkaForm.LkaActive = False Then
                If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TRQRQACT, 1)).SignalData > 0 And Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData) >= 0.5 Then
                    LkaForm.LkaActiveTransitionOn = True
                    LkaForm.LkaActive = True
                    LkaForm.LkaInitDistRtLnEdge = _saveLkaDistRtLnEdge
                    LkaForm.LkaInitDistLtLnEdge = _saveLkaDistLtLnEdge
                    LkaForm.Label1.Text = Format(LkaForm.LkaInitDistRtLnEdge, "0.000")
                    LkaForm.Label3.Text = Format(LkaForm.LkaInitDistLtLnEdge, "0.000")

                    LkaForm.LkaMinDistRtLnEdge = LkaForm.LkaInitDistRtLnEdge
                    LkaForm.LkaMinDistLtLnEdge = LkaForm.LkaInitDistLtLnEdge
                    LkaForm.Label8.Text = Format(LkaForm.LkaMinDistRtLnEdge, "0.000")
                    LkaForm.Label6.Text = Format(LkaForm.LkaMinDistLtLnEdge, "0.000")

                    'Left positive, right negative

                    If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData > 0 Then
                        LkaForm.LkaMaxDrvrTorqueLeft = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData
                        LkaForm.LkaMaxDrvrTorqueRight = 0
                    ElseIf MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData < 0 Then
                        LkaForm.LkaMaxDrvrTorqueRight = Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData)
                        LkaForm.LkaMaxDrvrTorqueLeft = 0
                    Else
                        LkaForm.LkaMaxDrvrTorqueRight = 0
                        LkaForm.LkaMaxDrvrTorqueLeft = 0
                    End If

                    LkaForm.Label14.Text = Format(LkaForm.LkaMaxDrvrTorqueRight, "0.000")
                    LkaForm.Label16.Text = Format(LkaForm.LkaMaxDrvrTorqueLeft, "0.000")


                    If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData > 0 Then
                        LkaForm.LkaMaxLkaTorqueLeft = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData
                        LkaForm.LkaMaxLkaTorqueRight = 0
                    ElseIf MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData < 0 Then
                        LkaForm.LkaMaxLkaTorqueRight = Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData)
                        LkaForm.LkaMaxLkaTorqueLeft = 0
                    Else
                        LkaForm.LkaMaxLkaTorqueRight = 0
                        LkaForm.LkaMaxLkaTorqueLeft = 0
                    End If

                    LkaForm.Label10.Text = Format(LkaForm.LkaMaxLkaTorqueRight, "0.000")
                    LkaForm.Label12.Text = Format(LkaForm.LkaMaxLkaTorqueLeft, "0.000")

                End If

            Else 'LKA Active = True

                If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TRQRQACT, 1)).SignalData = 0 Or Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData) < 0.5 Then
                    LkaForm.LkaActive = False
                    LkaForm.LkaActiveTransitionOff = True

                Else

                    If _saveLkaDistRtLnEdge < LkaForm.LkaMinDistRtLnEdge Then
                        LkaForm.LkaMinDistRtLnEdge = _saveLkaDistRtLnEdge
                        LkaForm.Label8.Text = Format(LkaForm.LkaMinDistRtLnEdge, "0.000")
                    End If
                    If _saveLkaDistLtLnEdge < LkaForm.LkaMinDistLtLnEdge Then
                        LkaForm.LkaMinDistLtLnEdge = _saveLkaDistLtLnEdge
                        LkaForm.Label6.Text = Format(LkaForm.LkaMinDistLtLnEdge, "0.000")
                    End If

                    If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData > LkaForm.LkaMaxDrvrTorqueLeft Then
                        LkaForm.LkaMaxDrvrTorqueLeft = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData
                        LkaForm.Label16.Text = Format(LkaForm.LkaMaxDrvrTorqueLeft, "0.000")
                    End If

                    If Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData) > LkaForm.LkaMaxDrvrTorqueRight Then
                        LkaForm.LkaMaxDrvrTorqueRight = Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_DRVRAPPLDTRQ, 1)).SignalData)
                        LkaForm.Label14.Text = Format(LkaForm.LkaMaxDrvrTorqueRight, "0.000")
                    End If

                    If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData > LkaForm.LkaMaxLkaTorqueLeft Then
                        LkaForm.LkaMaxLkaTorqueLeft = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData
                        LkaForm.Label12.Text = Format(LkaForm.LkaMaxLkaTorqueLeft, "0.000")
                    End If

                    If Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData) > LkaForm.LkaMaxLkaTorqueRight Then
                        LkaForm.LkaMaxLkaTorqueRight = Math.Abs(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_LKA_TORQUE, 1)).SignalData)
                        LkaForm.Label10.Text = Format(LkaForm.LkaMaxLkaTorqueRight, "0.000")
                    End If

                    LkaForm.LkaActiveTransitionOn = False

                End If
            End If
        End If

    End Sub

    Private Sub HandleTargetStatusDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles Target Status Screen display (Secret Squirrel Screen)...

        Dim foaiVair As Integer
        Dim foaiAwir As Integer
        Dim colprsysbrkprfreq As Integer

        If MyDg.CS_FSRACC_ENGAGED > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FSRACC_ENGAGED, 1)).SignalData <> 0 Then
                TargetStatusDisplay.Label9.BackColor = Color.Green
                TargetStatusDisplay.Label9.ForeColor = Color.Black
            Else
                TargetStatusDisplay.Label9.BackColor = Color.Black
                TargetStatusDisplay.Label9.ForeColor = Color.White
            End If
        End If

        If MyDg.CS_FSRACC_BRAKE_ACTIVE > 0 Then
            TargetStatusDisplay.Label10.BackColor = If(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FSRACC_BRAKE_ACTIVE, 1)).SignalData <> 0, Color.Red, Color.Black)
        End If

        If MyDg.CS_FSRACC_ACCEL_ACTIVE > 0 Then
            If MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FSRACC_ACCEL_ACTIVE, 1)).SignalData <> 0 Then
                TargetStatusDisplay.Label11.BackColor = Color.Green
                TargetStatusDisplay.Label11.ForeColor = Color.Black
            Else
                TargetStatusDisplay.Label11.BackColor = Color.Black
                TargetStatusDisplay.Label11.ForeColor = Color.White
            End If
        End If

        If MyDg.CS_CPSTOS_TTC > 0 Then 'And TargetStatusDisplay.Visible = True Then
            TargetStatusDisplay.Label1.Text = Format$(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CPSTOS_TTC, 1)).SignalData, "0.00")
        End If

        If MyDg.CS_ACC_GAPSETTING > 0 Then 'And TargetStatusDisplay.Visible = True Then
            TargetStatusDisplay.Label4.Text = MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ACC_GAPSETTING, 1)).SignalData
        End If

        If MyDg.CS_FOAI_VAIR > 0 And MyDg.CS_FOAI_AWIR > 0 Then 'And TargetStatusDisplay.Visible = True Then

            foaiVair = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FOAI_VAIR, 1)).SignalData)
            foaiAwir = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_FOAI_AWIR, 1)).SignalData)

            If foaiVair <> _saveFoaiVairValue Then
                If foaiAwir <> 2 And foaiAwir <> 3 Then
                    Select Case foaiVair
                        Case 0
                            TargetStatusDisplay.PictureBox1.Visible = False
                            TargetStatusDisplay.PictureBox2.Visible = False
                            TargetStatusDisplay.PictureBox3.Visible = False
                        Case 1
                            TargetStatusDisplay.PictureBox1.Visible = True
                            TargetStatusDisplay.PictureBox2.Visible = False
                            TargetStatusDisplay.PictureBox3.Visible = False
                            TargetStatusDisplay.ListBox2.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Vehicle Ahead")
                    End Select
                End If
            End If

            If foaiAwir <> _saveFoaiAwirValue Then
                Select Case foaiAwir
                    Case 2
                        TargetStatusDisplay.PictureBox1.Visible = False
                        TargetStatusDisplay.PictureBox2.Visible = True
                        TargetStatusDisplay.PictureBox3.Visible = False
                        TargetStatusDisplay.ListBox2.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Tailgate")
                    Case 3
                        TargetStatusDisplay.PictureBox1.Visible = False
                        TargetStatusDisplay.PictureBox2.Visible = False
                        TargetStatusDisplay.PictureBox3.Visible = True
                        TargetStatusDisplay.ListBox2.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Imminent")
                    Case Else
                        Select Case foaiVair
                            Case 0
                                TargetStatusDisplay.PictureBox1.Visible = False
                                TargetStatusDisplay.PictureBox2.Visible = False
                                TargetStatusDisplay.PictureBox3.Visible = False
                            Case 1
                                TargetStatusDisplay.PictureBox1.Visible = True
                                TargetStatusDisplay.PictureBox2.Visible = False
                                TargetStatusDisplay.PictureBox3.Visible = False
                        End Select
                End Select
            End If

            _saveFoaiAwirValue = foaiAwir
            _saveFoaiVairValue = foaiVair
        End If

        If MyDg.CS_AUTOBRKREQ > 0 And MyDg.CS_AUTOBRKREQTYPE > 0 And MyDg.CS_CPSCBSC_CTRLACC > 0 Then
            If CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AUTOBRKREQTYPE, 1)).SignalData) > 0 And
                        CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AUTOBRKREQTYPE, 1)).SignalData) < 5 And
                        CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_AUTOBRKREQ, 1)).SignalData) > 0 And
                        _saveAutobrkreqValue = 0 And
                        CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CPSCBSC_CTRLACC, 1)).SignalData) < 0 Then

                TargetStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " CPS Event")
                _saveAutobrkreqValue = 1
            Else
                _saveAutobrkreqValue = 0
            End If
        End If

        If MyDg.CS_COLPRSYSBRKPRFREQ > 0 Then
            colprsysbrkprfreq = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_COLPRSYSBRKPRFREQ, 1)).SignalData)
            If colprsysbrkprfreq = 1 And _saveColprsysbrkprfreq_Target = 0 Then
                TargetStatusDisplay.Label12.BackColor = Color.Yellow
                TargetStatusDisplay.Label12.ForeColor = Color.Black
                TargetStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " PREFILL Event")
            ElseIf colprsysbrkprfreq = 0 Then
                TargetStatusDisplay.Label12.BackColor = Color.Black
                TargetStatusDisplay.Label12.ForeColor = Color.White
            ElseIf colprsysbrkprfreq = 1 And _saveColprsysbrkprfreq_Target = 1 Then
                TargetStatusDisplay.Label12.BackColor = Color.Yellow
                TargetStatusDisplay.Label12.ForeColor = Color.Black
            End If
            _saveColprsysbrkprfreq_Target = colprsysbrkprfreq
        End If

        If MyDg.CS_CPSTOS_X_VEL > 0 And MyDg.CS_CPSTOS_Y_VEL > 0 Then
            TargetStatusDisplay.Label3.Text = Format$(Math.Sqrt((MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CPSTOS_X_VEL, 1)).SignalData ^ 2) + (MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_CPSTOS_Y_VEL, 1)).SignalData ^ 2)), "0.00")
        End If

    End Sub

    Private Sub HandlePedestrianStatusDisplay(ByVal MyDg As GridDataClass)

        'Called from MyBackgroundTasks
        'Handles Pedestrian Status Screen display...

        Dim colprsysbrkprfreq As Integer
        Dim pedwarn As Integer
        Dim brakingFlag As Integer
        Dim alertFlag As Integer
        Dim notificationFlag As Integer

        If MyDg.CS_PEDWARN > 0 Then
            pedwarn = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_PEDWARN, 1)).SignalData)
            If pedwarn <> _savePedwarn Then
                Select Case pedwarn
                    Case 0
                        PedestrianStatusDisplay.PictureBox3.Visible = True
                        PedestrianStatusDisplay.PictureBox1.Visible = False
                        PedestrianStatusDisplay.PictureBox2.Visible = False
                    Case 1
                        PedestrianStatusDisplay.PictureBox3.Visible = False
                        PedestrianStatusDisplay.PictureBox1.Visible = False
                        PedestrianStatusDisplay.PictureBox2.Visible = True
                        PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Ped Warn DETECT")
                    Case 2
                        PedestrianStatusDisplay.PictureBox3.Visible = False
                        PedestrianStatusDisplay.PictureBox1.Visible = True
                        PedestrianStatusDisplay.PictureBox2.Visible = False
                        PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " Ped Warn ALERT")
                End Select
                _savePedwarn = pedwarn
            End If
        End If

        If MyDg.CS_BRAKING_FLAG > 0 Then
            brakingFlag = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_BRAKING_FLAG, 1)).SignalData)
            If brakingFlag = 10 And _saveBrakingFlag <> 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Red
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " BRAKING Flag")
                HandleUserMessageLogging("GMRC", "HandlePedestrianStatusDisplay: BRAKING FLAG Set")
            ElseIf brakingFlag <> 10 And _saveBrakingFlag = 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Black
                HandleUserMessageLogging("GMRC", "HandlePedestrianStatusDisplay: BRAKING FLAG Reset")
            ElseIf brakingFlag <> 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Black
            ElseIf brakingFlag = 10 And _saveBrakingFlag = 10 Then
                PedestrianStatusDisplay.Label1.BackColor = Color.Red
            End If
            _saveBrakingFlag = brakingFlag
        End If

        If MyDg.CS_ALERT_FLAG > 0 Then
            alertFlag = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_ALERT_FLAG, 1)).SignalData)
            If alertFlag = 1 And _saveAlertFlag = 0 Then
                PedestrianStatusDisplay.Label2.BackColor = Color.Red
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " ALERT Flag")
            ElseIf alertFlag = 0 Then
                PedestrianStatusDisplay.Label2.BackColor = Color.Black
            ElseIf alertFlag = 1 And _saveAlertFlag = 1 Then
                PedestrianStatusDisplay.Label2.BackColor = Color.Red
            End If
            _saveAlertFlag = alertFlag
        End If

        If MyDg.CS_NOTIFICATION_FLAG > 0 Then
            notificationFlag = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_NOTIFICATION_FLAG, 1)).SignalData)
            If notificationFlag = 1 And _saveNotificationFlag = 0 Then
                PedestrianStatusDisplay.Label3.BackColor = Color.Red
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " NOTIFICATION Flag")
            ElseIf notificationFlag = 0 Then
                PedestrianStatusDisplay.Label3.BackColor = Color.Black
            ElseIf notificationFlag = 1 And _saveNotificationFlag = 1 Then
                PedestrianStatusDisplay.Label3.BackColor = Color.Red
            End If
            _saveNotificationFlag = notificationFlag
        End If

        If MyDg.CS_COLPRSYSBRKPRFREQ > 0 Then
            colprsysbrkprfreq = CInt(MySignalDataWithTime(MyDg.SignalIndex(MyDg.CS_COLPRSYSBRKPRFREQ, 1)).SignalData)
            If colprsysbrkprfreq = 1 And _saveColprsysbrkprfreq_Ped = 0 Then
                PedestrianStatusDisplay.Label4.BackColor = Color.Yellow
                PedestrianStatusDisplay.Label4.ForeColor = Color.Black
                PedestrianStatusDisplay.ListBox1.Items.Add(Format$(DateTime.Now, "HH:mm:ss") & " PREFILL Event")
            ElseIf colprsysbrkprfreq = 0 Then
                PedestrianStatusDisplay.Label4.BackColor = Color.Black
                PedestrianStatusDisplay.Label4.ForeColor = Color.White
            ElseIf colprsysbrkprfreq = 1 And _saveColprsysbrkprfreq_Ped = 1 Then
                PedestrianStatusDisplay.Label4.BackColor = Color.Yellow
                PedestrianStatusDisplay.Label4.ForeColor = Color.Black
            End If
            _saveColprsysbrkprfreq_Ped = colprsysbrkprfreq
        End If
    End Sub


    Private Sub UpdateCustomDisplays(ByVal MyDg As GridDataClass)
        ' Extract all the HandleXXXDisplay calls into one method
        If MyTdGraphicsContainer.Visible Then HandleAlcDisplayElements(MyDg)
        If CopilotStatusDisplay.Visible Then HandleCoPilotStatusDisplay(MyDg)
        If LmfrStatusScreenHc.Visible Then HandleLmfrStatusScreenHc(MyDg)
        If LmfrStatusDisplayGlobalA.Visible Then HandleLmfrStatusScreenGlobalA(MyDg)
        If FusionStatusDisplay.Visible Then HandleFusionStatusDisplay(MyDg)
        If LkaForm.Visible Then HandleLkaCustomDisplay(MyDg)
        If TargetStatusDisplay.Visible Then HandleTargetStatusDisplay(MyDg)
        If PedestrianStatusDisplay.Visible Then HandlePedestrianStatusDisplay(MyDg)
    End Sub

    Public Sub ShowCustomScreen(ByVal screenName As String)

        'Called from various places when a custom screen is to be displayed.  Displays, positions and sizes the proper form
        'based on the custom screen selection.

        Select Case screenName
            Case "LKA Screen"
                LkaForm.Top = 45
                LkaForm.Show()
                LkaForm.BringToFront()

                If MyTdGraphicsContainer IsNot Nothing Then
                    MyTdGraphicsContainer.Show()
                    MyTdGraphicsContainer.Left = (LkaForm.Left + LkaForm.Width) - MyTdGraphicsContainer.Width - 10
                    MyTdGraphicsContainer.Top = (LkaForm.Top + LkaForm.Height) - MyTdGraphicsContainer.Height - 40
                    MyTdGraphicsContainer.BringToFront()
                End If

            Case "Secret Squirrel Screen"
                TargetStatusDisplay.Top = 45
                TargetStatusDisplay.Show()
                TargetStatusDisplay.BringToFront()

                If MyTdGraphicsContainer IsNot Nothing Then
                    MyTdGraphicsContainer.Show()
                    MyTdGraphicsContainer.Left = (TargetStatusDisplay.Left + TargetStatusDisplay.Width) - MyTdGraphicsContainer.Width - 10
                    MyTdGraphicsContainer.Top = (TargetStatusDisplay.Top + TargetStatusDisplay.Height) - MyTdGraphicsContainer.Height - 40
                    MyTdGraphicsContainer.BringToFront()
                End If
            Case "Pedestrian Status Display"
                PedestrianStatusDisplay.Top = 45
                PedestrianStatusDisplay.Show()
                PedestrianStatusDisplay.BringToFront()

                If MyTdGraphicsContainer IsNot Nothing Then
                    MyTdGraphicsContainer.Show()
                    MyTdGraphicsContainer.Left = (PedestrianStatusDisplay.Left + PedestrianStatusDisplay.Width) - MyTdGraphicsContainer.Width - 10
                    MyTdGraphicsContainer.Top = (PedestrianStatusDisplay.Top + PedestrianStatusDisplay.Height) - MyTdGraphicsContainer.Height - 40
                    MyTdGraphicsContainer.BringToFront()
                End If
            Case "Fusion Status Display"
                FusionStatusDisplay.Top = 45
                FusionStatusDisplay.Show()
                FusionStatusDisplay.BringToFront()
            Case "CoPilot Status Display"
                CopilotStatusDisplay.Top = 45
                CopilotStatusDisplay.Show()
                CopilotStatusDisplay.BringToFront()

            Case "LMFR Global A Status Display"
                LmfrStatusDisplayGlobalA.Top = 45
                LmfrStatusDisplayGlobalA.Show()
                LmfrStatusDisplayGlobalA.BringToFront()

            Case "LMFR High Content Status Display"
                LmfrStatusScreenHc.Top = 45
                LmfrStatusScreenHc.Show()
                LmfrStatusScreenHc.BringToFront()

            Case "INCA Hardware Status"
                DeviceStatus.Top = 45
                DeviceStatus.Show()
                DeviceStatus.BringToFront()
            Case "Top Down View"
                If MyTdGraphicsContainer.Visible = False Then
                    MyTdGraphicsContainer.Left = Left
                    MyTdGraphicsContainer.Top = Top
                End If
                MyTdGraphicsContainer.ControlBox = True
                MyTdGraphicsContainer.Show()
                MyTdGraphicsContainer.BringToFront()
            Case "Create New Display"
                Dim newDisplayName As String
                newDisplayName = InputBox("Enter Display Name", "Display Configuration")
            Case Else
                HandleUserMessageLogging("GMRC", "ShorCustomScreen: Invalid Screen Name", DisplayMsgBox)
        End Select

    End Sub

End Class
