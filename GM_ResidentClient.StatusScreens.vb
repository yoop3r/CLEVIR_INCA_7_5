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

    Private Sub UpdateCustomDisplays(ByVal MyDg As GridDataClass)
        ' Extract all the HandleXXXDisplay calls into one method
        If MyTdGraphicsContainer.Visible Then HandleAlcDisplayElements(MyDg)
        If FusionStatusDisplay.Visible Then HandleFusionStatusDisplay(MyDg)
        If LkaForm.Visible Then HandleLkaCustomDisplay(MyDg)
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

            Case "Fusion Status Display"
                FusionStatusDisplay.Top = 45
                FusionStatusDisplay.Show()
                FusionStatusDisplay.BringToFront()

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
