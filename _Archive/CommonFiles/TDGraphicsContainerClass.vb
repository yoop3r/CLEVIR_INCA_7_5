Option Strict Off

Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Collections
Imports System.ComponentModel
Imports System.Windows.Forms
Imports System.Data
Imports System.Diagnostics

Public Class TDGraphicsContainerClass

    'This class is responsible for handling the top down view.  It is based on the standard Form 
    'base class.  The top down view displays the individual sensor track inputs for LRR, SRR, Camera
    'as symbols in an x / y coordinate frame such that the fusion output track data can be overlayed
    'with the input data and we can visualize the physical environment around the vehicle and visualize
    'where fusion thinks the targets are based on the sensor inputs.  We also display a vehicle path
    'data and sensor fields of view.  (Sensor fields of view for CSAV2 must still be changed).

    'The coordinate system on the top down view is different than the coordinate system in the EOCM.
    'The EOCM coordinate system is as follows...

    '+X Longitudinal axis forward of the vehicle...
    '+Y Lateral axis Left of the vehicle...

    'The coordinate system used in the top down view is as follows...

    'X = 0, left boundary of the top down view window - Positive direction horizontal to the right...
    'Y = 0, top boundary of the top down view windoe - Positive direction vertical down...

    'So, the coordinate system of the EOCM must be translated, X = -Y and Y = -X...

    'The input variables used to support the top down view are configured in the signal list (excel spreadsheet) which is read in
    'at runtime.

    'The signal list file contains a list of EOCM variable names, processor names, and raster names, and Key words which identify
    'how each variable will be used in conjunction with the top down view.  This information is read in during initialization.  The
    'keywords corresponding to each variable tell CLEVIR which variables are are used for which type of display, be it a point on
    'a line, or a sensor position, or a fusion track.  So, if a variable name corresponding to a particular point is changed,
    'the code does not have to change.

    'The key words are contained in the AlsoAssociatedWith column of the spreadsheet.

    Inherits Form

    Private Const PT_CENTER As Integer = 0
    Private Const PT_VEL_YAW As Integer = 1

    Public Const PT_CLOTHOID As Integer = 2
    Private Const PT_POLYNOMIAL As Integer = 3
    Public Const PT_LIST As Integer = 4

    'Public Const PT_DESIRED As Integer = 5
    'Public Const PT_BLUELINE As Integer = 6

    Private Const PT_LXCR_BLUELINE As Integer = 7

    Private Const PT_LXCR_TGTLANE As Integer = 8

    Public Const PT_LXCR_BLENDPATH As Integer = 9

    Private Const PT_BLENDPATH_LOOKAHEAD As Integer = 10

    Private Const PT_LXCR_BLENDPATH_W_COEFS As Integer = 11

    Private Const PT_LXCR_BLENDPATH_W_COORDS As Integer = 12

    Private Const DEFAULT_HALF_LANE_WIDTH As Double = 1.8

    Public Const F_DEG_TO_RAD As Single = 0.017453293
    Private Const F_PI_OVER_2 As Single = 1.570796327

    Private Const DEFAULT_WIDTH_METERS As Integer = 120
    Private Const DEFAULT_HEIGHT_METERS As Integer = 160

    Public Const DEFAULT_VIEWWIDTH_PXLS As Integer = 400
    Public Const DEFAULT_VIEWHEIGHT_PXLS As Integer = 400

    Public Const DEFAULT_HORZ_GRID_SPACING As Integer = 20
    Public Const DEFAULT_VERT_GRID_SPACING As Integer = 20

    Private Const DEFAULT_VEH_VERT_POSITION_PCT As Integer = 30

    Private Const MAX_NUM_PATH_PTS As Integer = 35

    Private Const DEFAULT_HOST_VEH_LAT_EXT As Integer = 1

    Public Const SB_SCALE As Integer = 1000
    Public Const SB_UNSCALE As Single = 0.001

    'Public lonposvalues(0 To 79) As Double
    'Public latposvalues(0 To 79) As Double

    Private lonposvalues(0 To 199) As Double
    Private latposvalues(0 To 199) As Double

    Private LonLookAhead As Double
    Private LatLookAhead As Double

    Public FusionTargetObject As TD_TargetObjectsClass
    Public LRR_Object As TD_TargetObjectsClass
    Public LSRR_Object As TD_TargetObjectsClass
    Public RSRR_Object As TD_TargetObjectsClass
    Public VIS_Object As TD_TargetObjectsClass
    Public VehicleObject As TD_TargetObjectsClass

    Private MouseRefPos_Y As Integer
    Private MouseRefPos_X As Integer

    Public Offset_Y As Integer
    Private Offset_X As Integer

    Private m_ZoomFactor As Single

    Private m_ZoomFactor_X As Single
    Private m_ZoomFactor_Y As Single

    Private StopZooming As Boolean

    Private mylabel1 As Label

    ' Controls
    Public myExitButton As Button
    Private myTDViewConfigButton As Button
    Private myFullScreenButton As Button
    Public myPictureBoxGreenLeftArrow As PictureBox
    Public myPictureBoxGreenRightArrow As PictureBox
    Public myPictureBoxGrayLeftArrow As PictureBox
    Public myPictureBoxGrayRightArrow As PictureBox
    Public myPictureBoxYellowLeftArrow As PictureBox
    Public myPictureBoxYellowRightArrow As PictureBox
    Public myPictureBoxRedLeftArrow As PictureBox
    Public myPictureBoxRedRightArrow As PictureBox
    Public myPictureBoxBlueLeftArrow As PictureBox
    Public myPictureBoxBlueRightArrow As PictureBox
    Public myALCReasonLeftLabel As Label
    Public myALCReasonRightLabel As Label
    Public myALCStatusLeftLabel As Label
    Public myALCStatusRightLabel As Label

    ' Other variables
    Private FormDisplayed As Boolean = False

    ' Static variables for control positions
    Private Shared SaveExitButtonLeft As Double
    Private Shared SaveExitButtonTop As Double
    Private Shared SaveToggleButtonLeft As Double
    Private Shared SaveToggleButtonTop As Double
    Private Shared SaveFullScreenButtonLeft As Double
    Private Shared SaveFullScreenButtonTop As Double

    ' ... Add other necessary controls and variables

    Private myMovinglabel As Label

    Private savevehheight As Single
    Private savevehwidth As Single

    Private _FullSizeViewAreaAcross As Integer
    Private _FullSizeViewAreaForeAft As Integer
    Private _GridSpacingAcross As Integer
    Private _GridSpacingForeAft As Integer
    Private _ViewWindowSizeWidth As Integer
    Private _ViewWindowSizeHeight As Integer
    Private _HostVehiclePercentFromBottom As Integer
    Private _HostVehicleDimForwardOfCG As Single
    Private _HostVehicleDimRearwardOfCG As Single
    Private _HostVehicleDimLateral As Single
    Private _HostVehicleDimLongitudinal As Single

    Private _COG_To_COV_Offset As Double

    Private m_MeterToViewHorzScale As Double
    Private m_MeterToViewVertScale As Double

    Private m_NominalVehWidth As Integer
    Private m_NominalVehLength As Integer

    Private m_HostVehLatExt As Double

    Private m_CenterHorzPxl As Integer
    Private m_BottomVertPxl As Integer
    Private m_ViewHeightPxls As Integer

    Private m_VertZeroOffset As Double
    Private m_HorzZeroOffset As Double 'will set to zero initially, no scrolling....

    Private CofGVertOffset As Double

    Private m_CofGVertOffset As Double 'Offset of veh center of gravity from bottom of full view

    Private m_bValidMarker As Boolean 'set to false for now.
    Private m_MarkerDist As Double 'set to zero, for now.

    Private m_NumPathPoints As Integer ''changed from long to integer

    Private m_RotCCwRad As Double 'this will be treated as always zero right now...
    Private m_OrgLeft As Double 'this will be treated as always zero right now...
    Private m_OrgFwd As Double 'this will be treated as always zero right now...

    Private m_fPxlPtX(MAX_NUM_PATH_PTS) As Double
    Private m_fPxlPtY(MAX_NUM_PATH_PTS) As Double
    Private m_PathPt(4, MAX_NUM_PATH_PTS) As Point

    Private m_NumLanesShown As Integer

    'pathdesc. variables

    Private fHalfLaneWidthPxl As Double
    Private BeginPosLeft As Integer
    Private BeginPosFwd As Integer
    Private InitialHeadingRad As Double
    Private Length1 As Long
    Private Length2 As Long
    Private Curv1 As Double
    Private Curv2 As Double

    Public KeLXCR_t_CDP_PathTimeStep As Double

    'LAYOUT
    Public FieldOfView_VIS_Visible As Boolean = True
    Public FieldOfView_LRR_Visible As Boolean = True
    Public FieldOfView_LFSRR_Visible As Boolean = True
    Public FieldOfView_RFSRR_Visible As Boolean = True
    Public ZoomAxis As String = "XY"
    Public DefaultPointSize As Integer = 2
    Public DangerZoneDisplay_Visible As Boolean = True
    Public DangerZoneLongAdj As Double = 0
    Public VehicleDim_WidthMeters As Single = 2
    Public VehicleDim_LengthMeters As Single = 5
    'PATHS
    Public TCPath_Visible As Boolean = True
    Public VPath_Visible As Boolean = True
    Public LXCRDesiredPath_Visible As Boolean = True
    Public BlueLinePath_Visible As Boolean = True
    Public LXCRBlueLinePath_Visible As Boolean = True
    Public LXCRTargetLanePath_Visible As Boolean = True
    Public LXCRBlendedPath_Visible As Boolean = True
    Public LXCRBlendedPath_TimeStepSec As Double = 0.1

    Private TopDownViewFunctionStatus As String
    Public FusionDisplayType As String = "Triangle"

    Public FusionTargetObject_MinSizePixels = 10
    Public LRR_Object_MinSizePixels = 5
    Public SRR_Objects_MinSizePixels = 5
    Public VIS_Object_MinSizePixels = 5



    Public Sub HandleZoom(ByVal ZoomCommand As Integer)

        'Called from TDGraphicsContainerClass_MouseDoubleClick event. Left button DblClick zooms in
        'Right button DblClick zooms out.

        'User may select Zoom X and Y simultaneously, or Zoom X or Y independently...
        'Selecting Zoom X and Y after X or Y is selected will reset window to the default zoom factor in both X and Y...

        Select Case ZoomCommand
            Case 0
                m_ZoomFactor_X = 1
                m_ZoomFactor_Y = 1
            Case -1

                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomXToolStripMenuItem.Checked Then

                    Select Case m_ZoomFactor_X
                        Case 1
                            m_ZoomFactor_X = 1.5
                        Case 1.5
                            m_ZoomFactor_X = 2
                        Case 2
                            m_ZoomFactor_X = 2.5
                        Case 2.5
                            m_ZoomFactor_X = 3
                        Case 3
                            m_ZoomFactor_X = 3.5
                        Case 3.5
                            m_ZoomFactor_X = 4
                        Case 4
                            m_ZoomFactor_X = 4.5
                        Case 4.5
                            m_ZoomFactor_X = 5
                    End Select

                End If

                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomYToolStripMenuItem.Checked Then

                    Select Case m_ZoomFactor_Y
                        Case 1
                            m_ZoomFactor_Y = 1.5
                        Case 1.5
                            m_ZoomFactor_Y = 2
                        Case 2
                            m_ZoomFactor_Y = 2.5
                        Case 2.5
                            m_ZoomFactor_Y = 3
                        Case 3
                            m_ZoomFactor_Y = 3.5
                        Case 3.5
                            m_ZoomFactor_Y = 4
                        Case 4
                            m_ZoomFactor_Y = 4.5
                        Case 4.5
                            m_ZoomFactor_Y = 5
                    End Select

                End If

            Case 1

                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomXToolStripMenuItem.Checked Then

                    Select Case m_ZoomFactor_X
                        Case 1.5
                            m_ZoomFactor_X = 1
                        Case 2
                            m_ZoomFactor_X = 1.5
                        Case 2.5
                            m_ZoomFactor_X = 2
                        Case 3
                            m_ZoomFactor_X = 2.5
                        Case 3.5
                            m_ZoomFactor_X = 3
                        Case 4
                            m_ZoomFactor_X = 3.5
                        Case 4.5
                            m_ZoomFactor_X = 4
                        Case 5
                            m_ZoomFactor_X = 4.5
                    End Select
                End If

                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomYToolStripMenuItem.Checked Then

                    Select Case m_ZoomFactor_Y
                        Case 1.5
                            m_ZoomFactor_Y = 1
                        Case 2
                            m_ZoomFactor_Y = 1.5
                        Case 2.5
                            m_ZoomFactor_Y = 2
                        Case 3
                            m_ZoomFactor_Y = 2.5
                        Case 3.5
                            m_ZoomFactor_Y = 3
                        Case 4
                            m_ZoomFactor_Y = 3.5
                        Case 4.5
                            m_ZoomFactor_Y = 4
                        Case 5
                            m_ZoomFactor_Y = 4.5
                    End Select

                End If

            Case Else
                'm_ZoomFactor = ZoomCommand \ 100 ''changed from /
                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomXToolStripMenuItem.Checked Then
                    m_ZoomFactor_X = ZoomCommand \ 100
                End If

                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomYToolStripMenuItem.Checked Then
                    m_ZoomFactor_Y = ZoomCommand \ 100
                End If

        End Select
        '.invalidate causes a repaint of the top down graphics container
        GmResidentClient.MyTdGraphicsContainer.Invalidate()


    End Sub

    Private Function CalcPath(ByVal pathtype As Integer) As Boolean

        'sets some scale values based on window size and calls appropriate path calculation
        'based on pathtype

        Dim bToRear As Boolean

        Dim Status As Boolean

        Dim errorthrown As Boolean

        Try

            ReDim m_fPxlPtX(MAX_NUM_PATH_PTS)
            ReDim m_fPxlPtY(MAX_NUM_PATH_PTS)
            ReDim m_PathPt(4, MAX_NUM_PATH_PTS)

            TopDownViewFunctionStatus = "CalcPath"

            'm_MeterToViewHorzScale = m_ZoomFactor * _ViewWindowSizeWidth / _FullSizeViewAreaAcross
            'm_MeterToViewVertScale = m_ZoomFactor * _ViewWindowSizeHeight / _FullSizeViewAreaForeAft

            m_MeterToViewHorzScale = m_ZoomFactor_X * _ViewWindowSizeWidth / _FullSizeViewAreaAcross
            m_MeterToViewVertScale = m_ZoomFactor_Y * _ViewWindowSizeHeight / _FullSizeViewAreaForeAft

            m_NominalVehWidth = CInt((2 * m_MeterToViewHorzScale)) ''changed to CInt
            m_NominalVehLength = (5 * m_NominalVehWidth) >> 1

            'bToRear and threshold filters go here...

            'path validity goes here...

            'line color, line style and marker stuff goes here...

            Select Case (pathtype)
                Case PT_CENTER
                    m_NumLanesShown = 0
                    Status = CalcTCPath(bToRear)

                Case PT_VEL_YAW
                    m_NumLanesShown = 1
                    Status = CalcVelYawPath(bToRear)

                'Case PT_DESIRED

                '    m_NumLanesShown = 0
                '    Status = CalcDesiredPath(bToRear)

                'Case PT_BLUELINE

                '    m_NumLanesShown = 0
            '    Status = CalcBlueLinePath(bToRear)

                'Add specific handling for other path types here....
                'Will require creating new routines specific to each path (PathDes and BlueLine)

                'Also look at what needs to be done to display the three lanes, left, center and right...

                'Case PT_CLOTHOID
                '    CalcClothoidPath(pDispDat, pPath, bToRear)

                Case PT_LXCR_BLUELINE

                    m_NumLanesShown = 0
                    Status = CalcLXCRBlueLinePath(bToRear)

                Case PT_LXCR_TGTLANE

                    m_NumLanesShown = 0
                    Status = CalcLXCRTargetLane(bToRear)

                'Case PT_LXCR_BLENDPATH

                '    m_NumLanesShown = 0
            '    Status = CalcLXCRBlendPath(bToRear)

                Case PT_LXCR_BLENDPATH_W_COEFS

                    m_NumLanesShown = 0
                    Status = CalcLXCRBlendPathWCoefs(bToRear)

                Case PT_LXCR_BLENDPATH_W_COORDS

                    m_NumLanesShown = 0
                    Status = CalcLXCRBlendPathWCoords(bToRear)

                Case PT_POLYNOMIAL

                    m_NumLanesShown = 3 '??????

                    'CalcPolynomialPath(pDispDat, pPath, bToRear)
                    Status = CalcPolynomialPath(bToRear)

                    'Case PT_LIST
                    '    CalcListPath(pDispDat, pPath, bToRear)
            End Select

        Catch ex As Exception

            If errorthrown = False Then
                errorthrown = True
                HandleUserMessageLogging("GMRC", "CalcPath: " & ex.Message)
            End If

        Finally

            CalcPath = Status

        End Try

    End Function

    Private Function GetVpathYawRate() As Double
        ' Initialize the return value to a default (e.g., 0.0)
        Dim result As Double = 0.0
        Dim z As Integer
        For z = 0 To myDGs.Count - 1
            ' Check if TD_VPATH_YAW_RATE is greater than 0
            If myDGs(z).TD_VPATH_YAW_RATE > 0 Then
                ' Retrieve the signal data and assign it to the result
                result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_VPATH_YAW_RATE, 1)).SignalData
                Exit For
            End If
        Next
        ' Return the result
        Return result
    End Function


    Private Function GetLMFRLaneLength() As Double
        ' Initialize a result variable to a default value.
        Dim result As Double = 0.0
        Dim z As Integer

        For z = 0 To myDGs.Count - 1
            If myDGs(z).LMFR_LANE_LENGTH > 0 Then
                ' If the condition is met, get the value and exit the loop.
                result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LMFR_LANE_LENGTH, 1)).SignalData
                Exit For
            End If
        Next

        ' Explicitly return the result.
        Return result
    End Function

    Private Function GetLXCRTargetLaneLength() As Double
        ' Initialize a result variable to a default value.
        Dim result As Double = 0.0
        Dim z As Integer

        For z = 0 To myDGs.Count - 1

            If myDGs(z).LXCR_TARGET_LANE_LENGTH > 0 Then
                ' If the condition is met, get the value and exit the loop.
                result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_TARGET_LANE_LENGTH, 1)).SignalData
                Exit For
            End If

        Next

        ' Explicitly return the result.
        Return result
    End Function

    Private Function GetLXCRBlendPathLength() As Double
        ' Initialize a result variable to a default value.
        Dim result As Double = 0.0
        Dim z As Integer

        For z = 0 To myDGs.Count - 1

            If myDGs(z).LXCR_BLEND_PATH_LENGTH > 0 Then
                ' If the condition is met, get the value and exit the loop.
                result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLEND_PATH_LENGTH, 1)).SignalData
                Exit For
            End If

        Next

        ' Explicitly return the result.
        Return result
    End Function

    Private Function GetLXCRLonVel() As Double
        ' Initialize a result variable to a default value.
        Dim result As Double = 0.0
        Dim z As Integer

        For z = 0 To myDGs.Count - 1

            If myDGs(z).LXCR_LON_VEL > 0 Then
                ' If the condition is met, get the value and exit the loop.
                result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_LON_VEL, 1)).SignalData
                Exit For
            End If

        Next

        ' Explicitly return the result.
        Return result
    End Function

    Private Function GetVpathLonVel() As Double
        ' Initialize a result variable to a default value.
        Dim result As Double = 0.0
        Dim z As Integer

        For z = 0 To myDGs.Count - 1

            If myDGs(z).TD_VPATH_LON_VEL > 0 Then
                ' If the condition is met, get the value and exit the loop.
                result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_VPATH_LON_VEL, 1)).SignalData
                Exit For
            End If

        Next

        ' Explicitly return the result.
        Return result
    End Function

    Private Function GetVpathLatVel() As Double
        ' Initialize a result variable to a default value.
        Dim result As Double = 0.0
        Dim z As Integer

        For z = 0 To myDGs.Count - 1

            If myDGs(z).TD_VPATH_LAT_VEL > 0 Then
                ' If the condition is met, get the value and exit the loop.
                result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_VPATH_LAT_VEL, 1)).SignalData

                Exit For
            End If

        Next

        ' Explicitly return the result.
        Return result
    End Function

    Private Function CalcVelYawPath(ByVal bToRear As Boolean) As Boolean

        'this is the vpath2 calculation - based on vpath yaw rate, etc...

        'void CTdDisplayWindow::CalcVelYawPath( CPathDisplayData* pDispDat, CTdPathDesc* pPath, bool bToRear )

        'P_VYAW*				pDat;
        'CPathDescriptor		pathDesc;
        Dim latVel As Double
        Dim lonVel As Double
        Dim yawRt As Double
        Dim speed As Double
        Dim curvature As Double

        'pDat = &pPath->p.vy;

        'if( IsFlaggedInvalid( &pDat->vYawRateVld, 0 ) ) {
        '	return;
        '}
        'if( !GetTdVariable( &pDat->vYawRate, 0, &yawRt ) ) {
        '	return;
        '}

        TopDownViewFunctionStatus = "CalcVelYawPath"

        yawRt = GetVpathYawRate()


        'If yawRt = 0 Then
        ''CalcVelYawPath = False
        'need to check display behavior on this....
        'Exit Function
        'End If

        yawRt = yawRt * F_DEG_TO_RAD

        'if( IsFlaggedInvalid( &pDat->vVelFwdVld, 0 ) ) {
        '	return;
        '}
        '( !GetTdVariable( &pDat->vVelFwd, 0, &lonVel ) ) {
        'return;
        '}

        lonVel = GetVpathLonVel()

        'If (lonVel = 0) Then
        'CalcVelYawPath = False
        'Exit Function
        'End If


        'if( IsFlaggedInvalid( &pDat->vVelLeftVld, 0 ) ) {
        '	return;
        '}
        'if( !GetTdVariable( &pDat->vVelLeft, 0, &latVel ) ) {
        '	return;
        '}

        latVel = GetVpathLatVel()

        If (latVel = 0) Then
            speed = lonVel
            InitialHeadingRad = 0
        Else
            speed = Math.Sqrt(lonVel * lonVel + latVel * latVel)

            If lonVel > 0 Then
                InitialHeadingRad = Math.Atan(latVel / lonVel)
            Else
                InitialHeadingRad = 0
            End If

        End If

        If speed > 0 Then
            curvature = yawRt / speed
        Else
            curvature = 0
        End If


        m_HostVehLatExt = DEFAULT_HOST_VEH_LAT_EXT

        fHalfLaneWidthPxl = m_HostVehLatExt * m_MeterToViewHorzScale
        BeginPosLeft = 0
        BeginPosFwd = 0

        If (curvature > 0.1) Then
            Length1 = CLng((F_PI_OVER_2 / curvature)) ''changed to add clng
        Else
            Length1 = 300 ' CommonFwdPath will shorten this to the display height
        End If

        Length2 = 0
        Curv1 = curvature
        Curv2 = 0

        If (bToRear) Then
            CalcVelYawPath = CommonRevPath()
        Else
            CalcVelYawPath = CommonFwdPath()
        End If

    End Function

    Public Function CalcDesiredPath(ByVal bToRear As Boolean) As Boolean

        TopDownViewFunctionStatus = "CalcDesiredPath"

        lonposvalues = GetDesiredPathLonPosValues()
        latposvalues = GetDesiredPathLatPosValues()

        'm_HostVehLatExt = DEFAULT_HOST_VEH_LAT_EXT

        'fHalfLaneWidthPxl = m_HostVehLatExt * m_MeterToViewHorzScale

        'If (bToRear) Then
        'CalcDesiredPath = CommonRevPath()
        'Else
        'CalcDesiredPath = CommonFwdPath()
        'End If

        CalcDesiredPath = True

    End Function

    Private Function CalcLXCRBlueLinePath(ByVal bToRear As Boolean) As Boolean

        Dim Coef_0 As Double
        Dim Coef_1 As Double
        Dim Coef_2 As Double
        Dim Coef_3 As Double

        Dim x As Integer

        TopDownViewFunctionStatus = "CalcLXCRBlueLinePath"

        'Currently, we are only  using array values, not coeficients...
        If TopDownViewConfiguration.LXCRBlueLinePathCalcType = "Coef" Then

            lonposvalues = GetLMFRPathLonPosValues()

            Coef_0 = GetLXCRBlueLineCoef(0)
            Coef_1 = GetLXCRBlueLineCoef(1)
            Coef_2 = GetLXCRBlueLineCoef(2)
            Coef_3 = GetLXCRBlueLineCoef(3)

            For x = 0 To 79

                latposvalues(x) = (Coef_3 * (lonposvalues(x) * lonposvalues(x) * lonposvalues(x))) +
                              (Coef_2 * (lonposvalues(x) * lonposvalues(x))) +
                              (Coef_1 * (lonposvalues(x))) +
                               Coef_0

                If lonposvalues(x) < 0 Or lonposvalues(x) > GetLMFRLaneLength() Then
                    lonposvalues(x) = 0
                    latposvalues(x) = 0
                End If

            Next

        Else
            'lonposvalues = GetLMFRPathLonPosValues()
            'latposvalues = GetLMFRPathLatPosValues()
            For x = 0 To 199
                lonposvalues(x) = x
            Next x
            latposvalues = GetLXCRPathLatPosValues()

        End If

        'm_HostVehLatExt = DEFAULT_HOST_VEH_LAT_EXT

        'fHalfLaneWidthPxl = m_HostVehLatExt * m_MeterToViewHorzScale

        'If (bToRear) Then
        'CalcBlueLinePath = CommonRevPath()
        ' Else
        'CalcBlueLinePath = = CommonFwdPath()
        'End If


        CalcLXCRBlueLinePath = True

    End Function

    Private Function CalcLXCRTargetLane(ByVal bToRear As Boolean) As Boolean

        Dim Coef_0 As Double
        Dim Coef_1 As Double
        Dim Coef_2 As Double
        Dim Coef_3 As Double

        Dim x As Integer

        TopDownViewFunctionStatus = "CalcLXCRTargetLane"

        'Currently we are only calculating points based on coefficients, we are not using X/Y coordinates...
        If TopDownViewConfiguration.LXCRTargetLanePathCalcType = "Coef" Then

            'lonposvalues = GetLXCRDesiredPathLonPosValues()

            Coef_0 = GetLXCRTargetLaneCoef(0)
            Coef_1 = GetLXCRTargetLaneCoef(1)
            Coef_2 = GetLXCRTargetLaneCoef(2)
            Coef_3 = GetLXCRTargetLaneCoef(3)

            For x = 0 To 199
                lonposvalues(x) = x
                latposvalues(x) = (Coef_3 * (lonposvalues(x) * lonposvalues(x) * lonposvalues(x))) +
                              (Coef_2 * (lonposvalues(x) * lonposvalues(x))) +
                              (Coef_1 * (lonposvalues(x))) +
                               Coef_0

                If lonposvalues(x) < 0 Or lonposvalues(x) > GetLXCRTargetLaneLength() Then
                    lonposvalues(x) = 0
                    latposvalues(x) = 0
                End If

            Next

        Else
            'Currently we are only calculating points based on coefficients, we are not using X/Y coordinates...
            lonposvalues = GetLXCRDesiredPathLonPosValues()
            latposvalues = GetLXCRDesiredPathLatPosValues()

        End If


        'm_HostVehLatExt = DEFAULT_HOST_VEH_LAT_EXT

        'fHalfLaneWidthPxl = m_HostVehLatExt * m_MeterToViewHorzScale

        'If (bToRear) Then
        'CalcBlueLinePath = CommonRevPath()
        ' Else
        ' = CommonFwdPath()
        'End If


        CalcLXCRTargetLane = True


    End Function

    Private Function CalcLXCRBlendPathWCoefs(ByVal bToRear As Boolean) As Boolean

        Dim Coef_0 As Double
        Dim Coef_1 As Double
        Dim Coef_2 As Double
        Dim Coef_3 As Double
        Dim Coef_4 As Double
        Dim Coef_5 As Double

        'Dim LXCRLonVel As Double
        Dim BlendPathLength As Double

        Dim x As Integer

        TopDownViewFunctionStatus = "CalcLXCRBlendPathWCoefs"

        'lonposvalues = GetBlendedPathLonPosValues()
        'LXCRLonVel = GetLXCRLonVel()

        Coef_0 = GetBlendedPathCoef(0)
        Coef_1 = GetBlendedPathCoef(1)
        Coef_2 = GetBlendedPathCoef(2)
        Coef_3 = GetBlendedPathCoef(3)
        Coef_4 = GetBlendedPathCoef(4)
        Coef_5 = GetBlendedPathCoef(5)

        BlendPathLength = GetLXCRBlendPathLength()

        LonLookAhead = GetLKARLatPosLA()



        'If (LXCRLonVel * 1.2) <= BlendPathLength Then
        If (LonLookAhead) <= BlendPathLength Then

            LatLookAhead = GetLXCRBPYPosLA()

            GoTo bypass

            'LonLookAhead = LXCRLonVel * 1.2
            LatLookAhead = (Coef_5 * (LonLookAhead * LonLookAhead * LonLookAhead * LonLookAhead * LonLookAhead) +
                                  (Coef_4 * LonLookAhead * LonLookAhead * LonLookAhead * LonLookAhead) +
                                  (Coef_3 * LonLookAhead * LonLookAhead * LonLookAhead) +
                                  (Coef_2 * LonLookAhead * LonLookAhead) +
                                  (Coef_1 * LonLookAhead)) +
                                   Coef_0

bypass:

        Else
            LonLookAhead = 0
            LatLookAhead = 0
        End If


        'For x = 0 To 79
        For x = 0 To 199

            'lonposvalues(x) = LXCRLonVel * GmResidentClient.MyTdGraphicsContainer.KeLXCR_t_CDP_PathTimeStep * x
            lonposvalues(x) = x

            latposvalues(x) = (Coef_5 * (lonposvalues(x) * lonposvalues(x) * lonposvalues(x) * lonposvalues(x) * lonposvalues(x))) +
                                  (Coef_4 * (lonposvalues(x) * lonposvalues(x) * lonposvalues(x) * lonposvalues(x))) +
                                  (Coef_3 * (lonposvalues(x) * lonposvalues(x) * lonposvalues(x))) +
                                  (Coef_2 * (lonposvalues(x) * lonposvalues(x))) +
                                  (Coef_1 * (lonposvalues(x))) +
                                   Coef_0

            If lonposvalues(x) < 0 Or lonposvalues(x) > BlendPathLength Then
                lonposvalues(x) = 0
                latposvalues(x) = 0
            End If
        Next

        CalcLXCRBlendPathWCoefs = True

    End Function

    Private Function CalcLXCRBlendPathWCoords(ByVal bToRear As Boolean) As Boolean
        Dim x As Integer
        Dim BlendPathLength As Double

        TopDownViewFunctionStatus = "CalcLXCRBlendPathWCoords"

        BlendPathLength = GetLXCRBlendPathLength()

        latposvalues = GetBlendedPathLatPosValues()

        For x = 0 To 199
            lonposvalues(x) = x
            If lonposvalues(x) > BlendPathLength Then
                lonposvalues(x) = 0
                latposvalues(x) = 0
            End If
        Next

        CalcLXCRBlendPathWCoords = True

    End Function

    Public Function CalcBlueLinePath(ByVal bToRear As Boolean) As Boolean

        Dim Coef_0 As Double
        Dim Coef_1 As Double
        Dim Coef_2 As Double
        Dim Coef_3 As Double

        Dim x As Integer

        TopDownViewFunctionStatus = "CalcBlueLinePath"

        lonposvalues = GetDesiredPathLonPosValues()

        Coef_0 = GetBlueLineCoef(0)
        Coef_1 = GetBlueLineCoef(1)
        Coef_2 = GetBlueLineCoef(2)
        Coef_3 = GetBlueLineCoef(3)

        For x = 0 To 79
            latposvalues(x) = (Coef_3 * (lonposvalues(x) * lonposvalues(x) * lonposvalues(x))) +
                              (Coef_2 * (lonposvalues(x) * lonposvalues(x))) +
                              (Coef_1 * (lonposvalues(x))) +
                               Coef_0
        Next

        'm_HostVehLatExt = DEFAULT_HOST_VEH_LAT_EXT

        'fHalfLaneWidthPxl = m_HostVehLatExt * m_MeterToViewHorzScale

        'If (bToRear) Then
        'CalcBlueLinePath = CommonRevPath()
        ' Else
        ' = CommonFwdPath()
        'End If

        CalcBlueLinePath = True

    End Function

    'void CTdDisplayWindow::CalcPolynomialPath( CPathDisplayData* pDispDat, CTdPathDesc* pPath, bool bToRear )
    Private Function CalcPolynomialPath(ByVal bToRear As Boolean) As Boolean
        Try
            TopDownViewFunctionStatus = "CalcPolynomialPath"

            If m_ZoomFactor_X = 0 Or m_ZoomFactor_Y = 0 Then
                Return False
            End If

            m_NumLanesShown = 3 ' Represents three lanes with shared markers

            ' Calculate common center and width, which sets BeginPosLeft
            CalcCommonCtrAndWidth()

            ' Initialize polynomial coefficients
            Dim coef1 As Double
            CalcCommonHdgTan(coef1) ' coef1 is passed by reference

            If m_RotCCwRad <> 0 Then
                coef1 += Math.Tan(m_RotCCwRad)
            End If

            Dim coef0 As Double = BeginPosLeft + m_OrgLeft
            Dim coef2 As Double = GetVisLaneCurvatures(0)
            Dim coef3 As Double = GetVisLaneCurvatureDrvs(0)

            ' Calculate vertical boundaries for the path
            Dim vertWindowHeight As Double = _FullSizeViewAreaForeAft / m_ZoomFactor_Y
            Dim pathLength As Double = 50.0
            Dim upperBnd As Double = vertWindowHeight + m_VertZeroOffset
            Dim lowerBnd As Double = m_VertZeroOffset

            ' Adjust bounds based on direction and path length
            If bToRear Then
                upperBnd = Math.Min(upperBnd, 0)
                lowerBnd = Math.Max(lowerBnd, -pathLength)
            Else
                upperBnd = Math.Min(upperBnd, pathLength)
                lowerBnd = Math.Max(lowerBnd, 0)
            End If

            ' Calculate the step increment for the path points
            Dim ds As Double = (upperBnd - lowerBnd) / MAX_NUM_PATH_PTS + 0.1
            Dim fwd As Double = lowerBnd
            If bToRear Then
                ds = -ds
                fwd = upperBnd
            End If

            ' Calculate path points
            For outCtr As Integer = 0 To MAX_NUM_PATH_PTS - 1
                Dim fwdCorr As Double = fwd + m_OrgFwd
                Dim fwdSq As Double = fwdCorr * fwdCorr
                Dim fwdCub As Double = fwdSq * fwdCorr

                Dim left As Double = coef0 + (fwdCorr * coef1) + (fwdSq * coef2) + (fwdCub * coef3)

                m_fPxlPtX(outCtr) = m_CenterHorzPxl + (m_MeterToViewHorzScale * (-left - m_HorzZeroOffset))
                m_fPxlPtY(outCtr) = m_BottomVertPxl - (m_MeterToViewVertScale * (fwd - m_VertZeroOffset))

                fwd += ds
            Next

            SetNumPathPts(MAX_NUM_PATH_PTS)
            CalcFinalPathPoints(fHalfLaneWidthPxl)

            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CalcPolynomialPath: " & ex.Message)
            Return False
        End Try
    End Function

    Private Sub CalcCommonCtrAndWidth()


        Dim fdat As Double
        Dim dToLeft As Double
        Dim dToRight As Double
        Dim useCurrentVar As Boolean

        Dim PF_USE_DIST_LFT_RGT As Boolean

        Dim ErrorThrown As Boolean

        Try

            TopDownViewFunctionStatus = "CalcCommonCtrAndWidth"

            '// we always start as fwd pos 0 (not really a variable)
            'pPathDesc->BeginPosFwd = 0;
            BeginPosFwd = 0

            PF_USE_DIST_LFT_RGT = True ' I think we use distance left right....

            'if( pPath->flags & CTdPathDesc::PF_USE_DIST_LFT_RGT ) {

            If PF_USE_DIST_LFT_RGT = True Then
                useCurrentVar = True

                'if( IsFlaggedInvalid( &pPath->pos.dis.vDistToLeftVld, 0 ) ) {  'Distance to left invalid
                If GetVisLaneQualities(0) < 2 Or GetVisLaneQualities(0) > 3 Then

                    useCurrentVar = False
                    dToLeft = DEFAULT_HALF_LANE_WIDTH

                End If

                If (useCurrentVar) = True Then

                    dToLeft = GetVisLaneOffsets(0)
                    If dToLeft = 0 Then
                        dToLeft = DEFAULT_HALF_LANE_WIDTH
                    End If
                End If

                useCurrentVar = True

                If GetVisLaneQualities(1) < 2 Or GetVisLaneQualities(1) > 3 Then 'Distance to right invalid

                    useCurrentVar = False
                    dToRight = DEFAULT_HALF_LANE_WIDTH

                End If

                If (useCurrentVar) = True Then

                    dToRight = GetVisLaneOffsets(1)
                    If dToRight = 0 Then
                        dToRight = DEFAULT_HALF_LANE_WIDTH
                    End If
                End If

                'pPathDesc->fHalfLaneWidthPxl = (float)0.5 * (dToLeft + dToRight) * m_MeterToViewHorzScale;
                fHalfLaneWidthPxl = 0.5 * (dToLeft + dToRight) * m_MeterToViewHorzScale

                BeginPosLeft = CInt(CDbl(0.5) * (dToLeft - dToRight))

            Else

                useCurrentVar = True
                'if( IsFlaggedInvalid( &pPath->pos.lw.vCtrOffsetVld, 0 ) ) {  'Center offset invalid
                If GetVisLaneQualities(0) < 2 Or GetVisLaneQualities(0) > 3 Then
                    useCurrentVar = False
                    BeginPosLeft = 0 ' //default
                End If


                If (useCurrentVar) = True Then
                    'if( !GetTdVariable( &pPath->pos.lw.vCtrOffset, 0, &pPathDesc->BeginPosLeft ) ) { 'center offset
                    If GetVisLaneOffsets(1) = 0 Then
                        'pPathDesc->BeginPosLeft = 0; '//default
                        BeginPosLeft = 0
                    End If

                End If

                useCurrentVar = True
                'if( IsFlaggedInvalid( &pPath->pos.lw.vLwidthVld, 0 ) ) { 'lane width valid
                If GetVisLaneQualities(0) < 2 Or GetVisLaneQualities(0) > 3 Then
                    useCurrentVar = False
                    fHalfLaneWidthPxl = DEFAULT_HALF_LANE_WIDTH * m_MeterToViewHorzScale
                End If

                If (useCurrentVar) = True Then
                    'if( !GetTdVariable( &pPath->pos.lw.vLwidth, 0, &fdat ) ) {  'lane width
                    fdat = GetVisLaneOffsets(0)  'lane offsets is lane width!  'this would have to be laneoffsets 0 - laneoffsets 1
                    If fdat = 0 Then
                        fHalfLaneWidthPxl = DEFAULT_HALF_LANE_WIDTH * m_MeterToViewHorzScale
                    Else
                        fHalfLaneWidthPxl = CDbl(0.5) * fdat * m_MeterToViewHorzScale
                    End If

                End If

            End If

        Catch ex As Exception

            If ErrorThrown = False Then
                ErrorThrown = True
                HandleUserMessageLogging("GMRC", "CalcCommonCtrAndWidth: " & ex.Message)
            End If

        End Try

    End Sub

    '    void CTdDisplayWindow::CalcCommonHdgTan( float* hdgTgt, CTdPathDesc* pPath )

    Private Sub CalcCommonHdgTan(ByRef coef1 As Double)

        TopDownViewFunctionStatus = "CalcCommonHdgTan"

        coef1 = GetVisLaneHdngAngles(0)

        'coef1 is heading which can be found in VISR_phi_CtF_LaneHdngAngle(0 - left, 1 - right, 2 - second left, 3 - second right)

        'Dim fdat As Double

        'if( pPath->flags & CTdPathDesc::PF_USE_TAN_HDG ) {

        'if( IsFlaggedInvalid( &pPath->hdg.tan.vHdgTanVld, 0 ) ) {
        '*hdgTgt = 0;
        '} else if( !GetTdVariable( &pPath->hdg.tan.vHdgTan, 0, &fdat ) ) {
        '*hdgTgt = 0;
        '} else {
        '*hdgTgt = fdat;
        '}

        '} else {

        'if( IsFlaggedInvalid( &pPath->hdg.deg.vHdgDegVld, 0 ) ) {
        '	*hdgTgt = 0;
        '} else if( !GetTdVariable( &pPath->hdg.deg.vHdgDeg, 0, &fdat ) ) {
        '	*hdgTgt = 0;
        '} else {
        '	*hdgTgt = tanf(fdat * F_DEG_TO_RAD);
        '}
        '}
        '}

    End Sub

    Private Function CalcTCPath(ByVal bToRear As Boolean) As Boolean

        'this is the vpath calculation, based on lat and lon pos, etc....

        Dim latTc As Double
        Dim lonTc As Double
        Dim radius As Double
        Dim curvature As Double
        Dim bValidLonTcExists As Boolean

        TopDownViewFunctionStatus = "CalcTCPath"

        'check validities here.....

        'get latTc value
        latTc = GetVpathLatPos()

        If latTc = 0 Then
            CalcTCPath = False
            Exit Function
        End If

        bValidLonTcExists = True

        'check validities here  = set bValidLonTcExists to false if invalid.....

        'get LonTc value
        lonTc = GetVpathLongPos()

        If bValidLonTcExists Then
            If (lonTc > 12.7) Then
                lonTc = 12.7
            ElseIf (lonTc < (-12.7)) Then
                lonTc = (-12.7)
            End If
        Else
            lonTc = 0
        End If

        radius = Math.Sqrt(lonTc * lonTc + latTc * latTc)

        If (latTc < 0) Then
            radius = -radius
        End If

        curvature = 1 / radius


        'm_HostVehLatExt = DEFAULT_HOST_VEH_LAT_EXT 'this will need to be normalized against display size....

        m_HostVehLatExt = DEFAULT_HOST_VEH_LAT_EXT

        fHalfLaneWidthPxl = m_HostVehLatExt * m_MeterToViewHorzScale

        BeginPosLeft = 0
        BeginPosFwd = 0

        If ((latTc <> 0) And (lonTc <> 0)) Then
            InitialHeadingRad = Math.Atan(-lonTc / latTc)
        Else
            InitialHeadingRad = 0
        End If

        If (curvature > 0.1) Then
            Length1 = CLng((F_PI_OVER_2 / curvature)) ''changed to add clng
        Else
            Length1 = 300 ' CommonFwdPath will shorten this to the display height
        End If

        Length2 = 0
        Curv1 = curvature
        Curv2 = 0

        If (bToRear) Then
            CalcTCPath = CommonRevPath()
        Else
            CalcTCPath = CommonFwdPath()
        End If
    End Function

    Private Function CommonRevPath() As Boolean
        CommonRevPath = True
    End Function

    Private Function CommonFwdPath() As Boolean

        'called for every path calculation, normalizes path points based on display size, zoom etc.

        'calls SetNumPathPts(outCtr) and CalcFinalPathPoints(fHalfLaneWidthPxl)

        Dim vertWindowHeight As Double
        Dim fwdUpperBnd As Double
        Dim fwdLowerBnd As Double
        Dim upperOutputLimit As Double
        Dim lowerOutputLimit As Double

        Dim dsNominal As Integer
        Dim testLength As Long
        Dim ds1 As Double
        Dim ds2 As Double
        Dim numSegs1 As Integer
        Dim numSegs2 As Integer

        Dim left As Double
        Dim fwd As Double
        Dim cumDist As Double
        Dim prevCumDist As Double
        Dim cumHeading As Double
        Dim dHeading1 As Double
        Dim dHeading2 As Double
        Dim fCenterHorzPxl As Double
        Dim alpha As Double
        Dim alphacomp As Double

        Dim loMrkrOutCtr As Integer
        Dim hiMrkrOutCtr As Integer
        Dim segCtr1 As Integer
        Dim segCtr2 As Integer
        Dim outCtr As Integer
        Dim bMarkerFound As Boolean

        TopDownViewFunctionStatus = "CommonFwdPath"

        'm_CenterHorzPxl = (DEFAULT_VIEWWIDTH_PXLS / 2)
        m_CenterHorzPxl = CInt(VehicleObject.X_Pos(0)) ''changed to add cint

        fCenterHorzPxl = m_CenterHorzPxl

        'm_CofGVertOffset = ((0.01 * GmResidentClient.MyTdGraphicsContainer.FullSizeViewAreaForeAft * GmResidentClient.MyTdGraphicsContainer.HostVehiclePercentFromBottom) / m_ZoomFactor)
        m_CofGVertOffset = ((0.01 * GmResidentClient.MyTdGraphicsContainer.FullSizeViewAreaForeAft * GmResidentClient.MyTdGraphicsContainer.HostVehiclePercentFromBottom) / m_ZoomFactor_Y)

        m_VertZeroOffset = -m_CofGVertOffset ' this is more complicated.....

        'm_VertZeroOffset = m_VertZeroOffset + (Offset_Y * GmResidentClient.MyTdGraphicsContainer.FullSizeViewAreaForeAft / GmResidentClient.MyTdGraphicsContainer.ViewWindowSizeHeight / m_ZoomFactor)
        m_VertZeroOffset = m_VertZeroOffset + (Offset_Y * GmResidentClient.MyTdGraphicsContainer.FullSizeViewAreaForeAft / GmResidentClient.MyTdGraphicsContainer.ViewWindowSizeHeight / m_ZoomFactor_Y)

        'scale_y = Format(-((CSng(e.Y) - VehicleObject.Y_Pos(0)) * sender.FullSizeViewAreaForeAft / sender.ViewWindowSizeHeight) / m_ZoomFactor, "0.00")


        ' Get starting distance. Always start the path at zero or greater.
        If (m_VertZeroOffset < 0) Then
            fwdLowerBnd = 0
        Else
            fwdLowerBnd = m_VertZeroOffset
        End If

        ' check if marker is off bottom of view  - not implemented by me...
        bMarkerFound = False
        'if( pDisp->m_bValidMarker ) {
        '	if( fwdLowerBnd >= pDisp->m_MarkerDist ) {
        '		pDisp->m_bValidMarker = false;
        '	}
        '}

        'this should not happen....
        If (m_ZoomFactor = 0) Or (m_ZoomFactor_X = 0) Or (m_ZoomFactor_Y = 0) Then
            CommonFwdPath = False
            Exit Function
        End If

        'Get the farthest distance we will calculate for the path.
        'vertWindowHeight = _FullSizeViewAreaForeAft / m_ZoomFactor
        vertWindowHeight = _FullSizeViewAreaForeAft / m_ZoomFactor_Y
        fwdUpperBnd = m_VertZeroOffset + vertWindowHeight

        ' We want to divide the displayed path into the maximum number of available segments.
        ' However we don't know the displayed length of the path since we may have zoomed in
        ' and scrolled around.  We do have an upper bound which is the sum of the path lengths.
        ' We have a rough estimate of a second upper which we take as 1.5 times the length of
        ' the vertical display size.  The smaller of these two is what we use as the path length.
        testLength = CInt(fwdUpperBnd - fwdLowerBnd) ''changed from Int to CInt

        If testLength < (Length1 + Length2) Then
            dsNominal = CInt(testLength / (MAX_NUM_PATH_PTS * 2 / 3)) ''changed from Int to CInt
        Else
            dsNominal = CInt((Length1 + Length2) / MAX_NUM_PATH_PTS) ''changed to add Cint
        End If
        If (dsNominal < 1) Then
            dsNominal = 1
        Else
            dsNominal += 1
        End If

        numSegs1 = CInt(Length1 / dsNominal) ''changed to add cint
        numSegs2 = CInt(Length2 / dsNominal) ''changed to add cint

        If (numSegs2 > 0) Then
            ds2 = Length2 / numSegs2
            dHeading2 = ds2 * Curv2
            cumHeading = InitialHeadingRad + m_RotCCwRad + (0.5 * dHeading2) 'm_RotCCwRad treated as always zero right now....
        End If
        If (numSegs1 > 0) Then ' must test after finished with setting cumHeading for seg 2
            ds1 = Length1 / numSegs1
            dHeading1 = ds1 * Curv1
            ' overwrite what we did for cumHeading above since dHeading1 is better choice now
            cumHeading = InitialHeadingRad + m_RotCCwRad + (0.5 * dHeading1)
        End If

        left = BeginPosLeft + m_OrgLeft
        fwd = BeginPosFwd + m_OrgFwd
        cumDist = 0
        prevCumDist = 0
        segCtr1 = 0
        segCtr2 = 0
        outCtr = 0

        upperOutputLimit = fwdUpperBnd + dsNominal
        lowerOutputLimit = fwdLowerBnd - dsNominal

        ' do this to account for the first point at the origin
        If (numSegs1 > 0) Then
            numSegs1 += 1
        ElseIf (numSegs2 > 0) Then
            numSegs2 += 1
        End If

        ' Begin calculations at origin but only begin display points when we reach bottom of view
        ' (Origin may be below bottom of view.)
        Do While (cumDist < lowerOutputLimit)
            If (segCtr1 < numSegs1) Then
                left += ds1 * Math.Sin(cumHeading) ' is cumheading in radians???
                fwd += ds1 * Math.Cos(cumHeading)
                cumHeading += dHeading1
                prevCumDist = cumDist
                cumDist += ds1
                segCtr1 += 1
            ElseIf (segCtr2 < numSegs2) Then
                left += ds2 * Math.Sin(cumHeading)
                fwd += ds2 * Math.Sin(cumHeading)
                cumHeading += dHeading2
                prevCumDist = cumDist
                cumDist += ds2
                segCtr2 += 1
            Else
                Exit Do
            End If
        Loop

        'setting this to false cause we dont understand this yet....
        m_bValidMarker = False
        m_MarkerDist = 0

        ' display the first segment (or what's left of it)
        Do While (segCtr1 < numSegs1)
            If (outCtr >= MAX_NUM_PATH_PTS) Then
                Exit Do
            End If

            ' marker calcs
            If (m_bValidMarker And bMarkerFound = False) Then
                If (cumDist >= m_MarkerDist) Then
                    bMarkerFound = True
                    ' marker pos is alpha*(pathPos at loMrkrOutCtr) + (1-alpha)*(pathPos at hiMrkrOutCtr)
                    alpha = (cumDist - m_MarkerDist) / (cumDist - prevCumDist)
                    hiMrkrOutCtr = outCtr
                    loMrkrOutCtr = outCtr - 1
                    If (loMrkrOutCtr < 0) Then
                        loMrkrOutCtr = 0
                    End If
                End If
            End If

            m_HorzZeroOffset = 0 ' set to zero here,will change when we implement scrolling....
            'm_ViewHeightPxls = DEFAULT_VIEWHEIGHT_PXLS
            m_ViewHeightPxls = GmResidentClient.MyTdGraphicsContainer.ViewWindowSizeHeight
            m_BottomVertPxl = m_ViewHeightPxls - 1

            m_fPxlPtX(outCtr) = fCenterHorzPxl + (m_MeterToViewHorzScale * ((-left - m_HorzZeroOffset)))
            m_fPxlPtY(outCtr) = m_BottomVertPxl - (m_MeterToViewVertScale * ((fwd - m_VertZeroOffset)))
            outCtr += 1

            ' do this after output point because we want one point to extend past top of view 
            If (fwd > upperOutputLimit) Then
                Exit Do
            End If
            left += ds1 * Math.Sin(cumHeading)
            fwd += ds1 * Math.Cos(cumHeading)
            cumHeading += dHeading1
            prevCumDist = cumDist
            cumDist += ds1
            segCtr1 += 1
        Loop

        ' display the second segment
        Do While (segCtr2 < numSegs2)
            If (outCtr >= MAX_NUM_PATH_PTS) Then
                Exit Do
            End If

            ' marker calcs
            If (m_bValidMarker And bMarkerFound = False) Then
                If (cumDist >= m_MarkerDist) Then
                    bMarkerFound = True
                    ' marker pos is alpha*(pathPos at loMrkrOutCtr) + (1-alpha)*(pathPos at hiMrkrOutCtr)
                    alpha = (cumDist - m_MarkerDist) / (cumDist - prevCumDist)
                    hiMrkrOutCtr = outCtr
                    loMrkrOutCtr = outCtr - 1
                    If (loMrkrOutCtr < 0) Then
                        loMrkrOutCtr = 0
                    End If
                End If
            End If

            m_fPxlPtX(outCtr) = fCenterHorzPxl + (m_MeterToViewHorzScale * ((-left - m_HorzZeroOffset)))
            m_fPxlPtY(outCtr) = m_BottomVertPxl - (m_MeterToViewVertScale * ((fwd - m_VertZeroOffset)))
            outCtr += 1

            ' do this after output point because we want one point to extend past top of view 
            If (fwd > upperOutputLimit) Then
                Exit Do
            End If
            left += ds2 * Math.Sin(cumHeading)
            fwd += ds2 * Math.Cos(cumHeading)
            cumHeading += dHeading2
            prevCumDist = cumDist
            cumDist += ds2
            segCtr2 += 1
        Loop

        If (bMarkerFound) Then
            alphacomp = (1.0 - alpha)
            'pDisp->m_MarkerLoc.x = (alpha*m_fPxlPtX(loMrkrOutCtr) + alphaComp*m_fPxlPtX(hiMrkrOutCtr))
            'pDisp->m_MarkerLoc.y = (alpha*m_fPxlPtY(loMrkrOutCtr) + alphaComp*m_fPxlPtY(hiMrkrOutCtr))
        Else
            m_bValidMarker = False
        End If

        SetNumPathPts(outCtr)

        CalcFinalPathPoints(fHalfLaneWidthPxl)

        CommonFwdPath = True

        'Loop

    End Function

    Public Sub PolylineEnvelope(ByRef polypts() As Point, ByVal NumPolyPt As Integer, ByRef pts() As Point, ByVal offset As Integer)

        TopDownViewFunctionStatus = "PolylineEnvelope"

        If (NumPolyPt > 1) Then
            LineVerticalOffset(polypts(0), polypts(1), polypts(0), offset, pts(0), pts(2 * NumPolyPt - 1))
            pts(2 * NumPolyPt) = pts(0)
            For k = 1 To NumPolyPt
                LineVerticalOffset(polypts(k - 1), polypts(k), polypts(k), offset, pts(k), pts(2 * NumPolyPt - 1 - k))
            Next k
        End If
    End Sub

    Private Sub LineVerticalOffset(ByVal pt0 As Point, ByVal pt1 As Point, ByVal pt As Point, ByVal offset As Integer, ByRef posPt As Point, ByRef negPt As Point)

        Dim dx As Double
        Dim dy As Double
        Dim norm As Double

        TopDownViewFunctionStatus = "LineVerticalOffset"

        dx = pt1.X - pt0.X
        dy = pt1.Y - pt0.Y
        norm = Math.Sqrt(dx * dx + dy * dy)
        dx /= norm
        dy /= norm

        posPt.X = CInt((pt.X - dy * offset)) '(long) ''changed to add cint
        posPt.Y = CInt((pt.Y + dx * offset)) '(long) ''changed to add cint

        negPt.X = CInt((pt.X + dy * offset)) '(long) ''changed to add cint
        negPt.Y = CInt((pt.Y - dx * offset)) '(long) ''changed to add cint

    End Sub

    Private Function GetLKARLatPosLA() As Double
        ' Initialize a result variable to a default value.
        Dim result As Double = 0.0
        Dim z As Integer

        TopDownViewFunctionStatus = "GetLKARLatPosLA"

        For z = 0 To myDGs.Count - 1
            If myDGs(z).LKAR_LATPOS_LA > 0 Then
                ' If the condition is met, get the value and exit the loop.
                result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LKAR_LATPOS_LA, 1)).SignalData
                Exit For
            End If
        Next

        ' Explicitly return the result.
        Return result
    End Function

    Private Function GetLXCRBPYPosLA() As Double
        ' Initialize a result variable to a default value.
        Dim result As Double = 0.0
        Dim z As Integer

        TopDownViewFunctionStatus = "GetLXCRBPYPosLA"

        For z = 0 To myDGs.Count - 1
            If myDGs(z).LXCR_BP_YPOS_LA > 0 Then
                ' If the condition is met, get the value and exit the loop.
                result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BP_YPOS_LA, 1)).SignalData
                Exit For
            End If
        Next

        ' Explicitly return the result.
        Return result
    End Function

    Private Function GetBlendedPathCoef(ByVal myCoef As Integer) As Double
        ' Initialize a result variable to a default value.
        Dim result As Double = 0.0
        Dim z As Integer

        TopDownViewFunctionStatus = "GetBlendedPathCoef"

        For z = 0 To myDGs.Count - 1

            Select Case myCoef

                Case 0

                    If myDGs(z).LXCR_BLEND_PATH_TD_COEF_0 > 0 Then
                        ' If the condition is met, get the value and exit the loop.
                        result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLEND_PATH_TD_COEF_0, 1)).SignalData
                        Exit For
                    End If

                Case 1

                    If myDGs(z).LXCR_BLEND_PATH_TD_COEF_1 > 0 Then
                        ' If the condition is met, get the value and exit the loop.
                        result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLEND_PATH_TD_COEF_1, 1)).SignalData
                        Exit For
                    End If

                Case 2

                    If myDGs(z).LXCR_BLEND_PATH_TD_COEF_2 > 0 Then
                        ' If the condition is met, get the value and exit the loop.
                        result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLEND_PATH_TD_COEF_2, 1)).SignalData
                        Exit For
                    End If

                Case 3

                    If myDGs(z).LXCR_BLEND_PATH_TD_COEF_3 > 0 Then
                        ' If the condition is met, get the value and exit the loop.
                        result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLEND_PATH_TD_COEF_3, 1)).SignalData
                        Exit For
                    End If

                Case 4

                    If myDGs(z).LXCR_BLEND_PATH_TD_COEF_4 > 0 Then
                        ' If the condition is met, get the value and exit the loop.
                        result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLEND_PATH_TD_COEF_4, 1)).SignalData
                        Exit For
                    End If

                Case 5

                    If myDGs(z).LXCR_BLEND_PATH_TD_COEF_5 > 0 Then
                        ' If the condition is met, get the value and exit the loop.
                        result = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLEND_PATH_TD_COEF_5, 1)).SignalData
                        Exit For
                    End If

            End Select

        Next

        ' Explicitly return the result.
        Return result
    End Function

    Private Function GetLXCRTargetLaneCoef(ByVal myCoef As Integer) As Double

        Dim z As Integer

        TopDownViewFunctionStatus = "GetLXCRTargetLaneCoef"

        For z = 0 To myDGs.Count - 1

            Select Case myCoef

                Case 0

                    If myDGs(z).LXCR_TGT_LANE_TD_COEF_0 > 0 Then
                        GetLXCRTargetLaneCoef = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_TGT_LANE_TD_COEF_0, 1)).SignalData
                        Exit For
                    End If

                Case 1

                    If myDGs(z).LXCR_TGT_LANE_TD_COEF_1 > 0 Then
                        GetLXCRTargetLaneCoef = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_TGT_LANE_TD_COEF_1, 1)).SignalData
                        Exit For
                    End If

                Case 2

                    If myDGs(z).LXCR_TGT_LANE_TD_COEF_2 > 0 Then
                        GetLXCRTargetLaneCoef = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_TGT_LANE_TD_COEF_2, 1)).SignalData
                        Exit For
                    End If

                Case 3

                    If myDGs(z).LXCR_TGT_LANE_TD_COEF_3 > 0 Then
                        GetLXCRTargetLaneCoef = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_TGT_LANE_TD_COEF_3, 1)).SignalData
                        Exit For
                    End If

            End Select

        Next


    End Function


    Private Function GetLXCRBlueLineCoef(ByVal myCoef As Integer) As Double

        Dim z As Integer

        TopDownViewFunctionStatus = "GetLXCRBlueLineCoef"

        For z = 0 To myDGs.Count - 1

            Select Case myCoef

                Case 0

                    'If myDGs(z).LXCR_BLUE_LINE_TD_COEF_0 > 0 Then
                    'GetLXCRBlueLineCoef = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLUE_LINE_TD_COEF_0, 1)).SignalData
                    Exit For
                    'End If

                Case 1

                    'If myDGs(z).LXCR_BLUE_LINE_TD_COEF_1 > 0 Then
                    'GetLXCRBlueLineCoef = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLUE_LINE_TD_COEF_1, 1)).SignalData
                    Exit For
                    'End If

                Case 2

                    'If myDGs(z).LXCR_BLUE_LINE_TD_COEF_2 > 0 Then
                    'GetLXCRBlueLineCoef = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLUE_LINE_TD_COEF_2, 1)).SignalData
                    Exit For
                    'End If

                Case 3

                    'If myDGs(z).LXCR_BLUE_LINE_TD_COEF_3 > 0 Then
                    'GetLXCRBlueLineCoef = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLUE_LINE_TD_COEF_3, 1)).SignalData
                    Exit For
                    'End If

            End Select

        Next


    End Function

    Private Function GetBlueLineCoef(ByVal myCoef As Integer) As Double

        Dim z As Integer

        TopDownViewFunctionStatus = "GetBlueLineCoef"

        GetBlueLineCoef = 0

        For z = 0 To myDGs.Count - 1

            Select Case myCoef

                Case 0

                    'If myDGs(z).BLUE_LINE_TD_COEF_0 > 0 Then
                    ' GetBlueLineCoef = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).BLUE_LINE_TD_COEF_0, 1)).SignalData
                    Exit For
                    'End If

                Case 1

                    'If myDGs(z).BLUE_LINE_TD_COEF_1 > 0 Then
                    'GetBlueLineCoef = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).BLUE_LINE_TD_COEF_1, 1)).SignalData
                    Exit For
                    'End If

                Case 2

                    'If myDGs(z).BLUE_LINE_TD_COEF_2 > 0 Then
                    'GetBlueLineCoef = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).BLUE_LINE_TD_COEF_2, 1)).SignalData
                    Exit For
                    'End If

                Case 3

                    'If myDGs(z).BLUE_LINE_TD_COEF_3 > 0 Then
                    'GetBlueLineCoef = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).BLUE_LINE_TD_COEF_3, 1)).SignalData
                    Exit For
                    'End If

            End Select

        Next

    End Function

    Private Function GetLXCRDangerZoneRightYPos() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 7) As Double

        TopDownViewFunctionStatus = "GetLXCRDangerZoneRightYPos"

        For x = 0 To 7
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            If myDGs(z).DANGER_ZONE_RIGHT_Y_TD_PT_0 > 0 Then

                For x = 0 To 7
                    Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).DANGER_ZONE_RIGHT_Y_TD_PT_0 + x, 1)).SignalData
                Next x

                Exit For

            End If

        Next

        'Temp for testing...

        'Values(0) = -10.0
        'Values(1) = -2.5
        'Values(2) = -2.5
        'Values(3) = 0.0
        'Values(4) = 0.0
        'Values(5) = -2.5
        'Values(6) = -2.5
        'Values(7) = -10.0

        GetLXCRDangerZoneRightYPos = Values

    End Function

    Private Function GetLXCRDangerZoneRightXPos() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 7) As Double

        TopDownViewFunctionStatus = "GetLXCRDangerZoneRightXPos"

        For x = 0 To 7
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            If myDGs(z).DANGER_ZONE_RIGHT_X_TD_PT_0 > 0 Then

                For x = 0 To 7
                    Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).DANGER_ZONE_RIGHT_X_TD_PT_0 + x, 1)).SignalData - _COG_To_COV_Offset
                Next x

                Exit For

            End If

        Next

        'Temp for testing...


        'Values(0) = 10.0
        'Values(1) = 10.0
        'Values(2) = 5.0
        'Values(3) = 2.5
        'Values(4) = -10.0
        'Values(5) = -12.5
        'Values(6) = -14.0
        'Values(7) = -14.0

        GetLXCRDangerZoneRightXPos = Values

    End Function

    Private Function GetLXCRDangerZoneLeftYPos() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 7) As Double

        TopDownViewFunctionStatus = "GetLXCRDangerZoneLeftYPos"

        For x = 0 To 7
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            If myDGs(z).DANGER_ZONE_LEFT_Y_TD_PT_0 > 0 Then

                For x = 0 To 7
                    Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).DANGER_ZONE_LEFT_Y_TD_PT_0 + x, 1)).SignalData
                Next x

                Exit For

            End If

        Next

        'Temp for testing...


        'Values(0) = 10.0
        'Values(1) = 2.5
        'Values(2) = 2.5
        'Values(3) = 0.0
        'Values(4) = 0.0
        'Values(5) = 2.5
        'Values(6) = 2.5
        'Values(7) = 10.0

        GetLXCRDangerZoneLeftYPos = Values

    End Function

    Private Function GetLXCRDangerZoneLeftXPos() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 7) As Double

        TopDownViewFunctionStatus = "GetLXCRDangerZoneLeftXPos"

        For x = 0 To 7
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            If myDGs(z).DANGER_ZONE_LEFT_X_TD_PT_0 > 0 Then

                For x = 0 To 7
                    Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).DANGER_ZONE_LEFT_X_TD_PT_0 + x, 1)).SignalData - _COG_To_COV_Offset
                Next x

                Exit For

            End If

        Next

        'Temp for testing...


        'Values(0) = 10.0
        'Values(1) = 10.0
        'Values(2) = 5.0
        'Values(3) = 2.5
        'Values(4) = -10.0
        'Values(5) = -12.5
        'Values(6) = -14.0
        'Values(7) = -14.0

        GetLXCRDangerZoneLeftXPos = Values

    End Function

    Private Function GetLXCRDesiredPathLatPosValues() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 79) As Double

        TopDownViewFunctionStatus = "GetLXCRDesiredPathLatPosValues"

        For x = 0 To 79
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            'If myDGs(z).LXCR_PATH_DESIRED_TD_LAT_POS_0 > 0 Then

            'For x = 0 To 79
            'Values(x) = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_PATH_DESIRED_TD_LAT_POS_0 + x, 1)).SignalData
            'Next x

            Exit For

            'End If

        Next

        GetLXCRDesiredPathLatPosValues = Values

    End Function

    Private Function GetBlendedPathLonPosValues() As Double()

        'need to add proper variables for blended path, they dont exist yet...

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 79) As Double

        For x = 0 To 79
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            'If myDGs(z).LXCR_PATH_DESIRED_TD_LON_POS_0 > 0 Then

            'For x = 0 To 79
            'Values(x) = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_PATH_DESIRED_TD_LON_POS_0 + x, 1)).SignalData
            'Next x

            Exit For

            'End If

        Next

        GetBlendedPathLonPosValues = Values

    End Function

    Private Function GetBlendedPathLatPosValues() As Double()

        'need to add proper variables for blended path, they dont exist yet...

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 199) As Double

        TopDownViewFunctionStatus = "GetBlendedPathLatPosValues"

        For x = 0 To 199
            Values(x) = 0.0
        Next

        ' make valid only up to this x pos VeLXCR_l_BP_Lengthb4Fit

        For z = 0 To myDGs.Count - 1

            If myDGs(z).LXCR_BLEND_PATH_TD_LAT_POS_0 > 0 Then

                For x = 0 To 199
                    Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_BLEND_PATH_TD_LAT_POS_0 + x, 1)).SignalData
                Next x

                Exit For

            End If

        Next

        GetBlendedPathLatPosValues = Values

    End Function

    Private Function GetLXCRDesiredPathLonPosValues() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 79) As Double

        TopDownViewFunctionStatus = "GetLXCRDesiredPathLonPosValues"

        For x = 0 To 79
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            'If myDGs(z).LXCR_PATH_DESIRED_TD_LON_POS_0 > 0 Then

            'For x = 0 To 79
            'Values(x) = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_PATH_DESIRED_TD_LON_POS_0 + x, 1)).SignalData
            'Next x

            Exit For

            'End If

        Next

        GetLXCRDesiredPathLonPosValues = Values

    End Function

    Private Function GetLMFRPathLonPosValues() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 79) As Double

        TopDownViewFunctionStatus = "GetLMFRPathLonPosValues"

        For x = 0 To 79
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            'If myDGs(z).LMFR_PATH_TD_LON_POS_0 > 0 Then

            'For x = 0 To 79
            'Values(x) = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LMFR_PATH_TD_LON_POS_0 + x, 1)).SignalData
            'Next x

            Exit For

            'End If

        Next

        GetLMFRPathLonPosValues = Values

    End Function

    Private Function GetLXCRPathLatPosValues() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 199) As Double

        For x = 0 To 199
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            If myDGs(z).LXCR_PATH_TD_LAT_POS_0 > 0 Then

                For x = 0 To 199
                    Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LXCR_PATH_TD_LAT_POS_0 + x, 1)).SignalData
                Next x

                Exit For

            End If

        Next

        GetLXCRPathLatPosValues = Values

    End Function

    Private Function GetLMFRPathLatPosValues() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 79) As Double

        TopDownViewFunctionStatus = "GetLMFRPathLatPosValues"

        For x = 0 To 79
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            'If myDGs(z).LMFR_PATH_TD_LAT_POS_0 > 0 Then
            '
            'For x = 0 To 79
            '        Values(x) = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).LMFR_PATH_TD_LAT_POS_0 + x, 1)).SignalData

            'Next x

            Exit For

            'End If

        Next

        GetLMFRPathLatPosValues = Values

    End Function

    Private Function GetDesiredPathLonPosValues() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 79) As Double

        TopDownViewFunctionStatus = "GetDesiredPathLonPosValues"

        For x = 0 To 79
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            'If myDGs(z).DESIRED_PATH_TD_LON_POS_0 > 0 Then

            'For x = 0 To 79
            'Values(x) = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).DESIRED_PATH_TD_LON_POS_0 + x, 1)).SignalData
            'Next x
            '
            Exit For

            'End If

        Next

        GetDesiredPathLonPosValues = Values

    End Function

    Private Function GetDesiredPathLatPosValues() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 79) As Double

        TopDownViewFunctionStatus = "GetDesiredPathLatPosValues"

        For x = 0 To 79
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            'If myDGs(z).DESIRED_PATH_TD_LAT_POS_0 > 0 Then

            'For x = 0 To 79
            'Values(x) = GmResidentClient.mySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).DESIRED_PATH_TD_LAT_POS_0 + x, 1)).SignalData
            'Next x

            Exit For

            'End If

        Next

        GetDesiredPathLatPosValues = Values

    End Function

    Private Function GetVisLaneOffsets() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 3) As Double

        TopDownViewFunctionStatus = "GetVisLaneOffsets"

        For x = 0 To 3
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            If myDGs(z).CS_LNSNS_DISTTOLLNEDGE > 0 Then

                'For x = 0 To 3
                x = 0
                Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_LNSNS_DISTTOLLNEDGE, 1)).SignalData
                'Next x

                'Exit For

            End If

            If myDGs(z).CS_LNSNS_DISTTORLNEDGE > 0 Then

                'For x = 0 To 3
                x = 1
                Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).CS_LNSNS_DISTTORLNEDGE, 1)).SignalData
                'Next x

                'Exit For

            End If

        Next

        GetVisLaneOffsets = Values

    End Function

    Private Function GetVisLaneHdngAngles() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 3) As Double

        TopDownViewFunctionStatus = "GetVisLaneHdngAngles"

        For x = 0 To 3
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            If myDGs(z).VIS_LANE_HDNG_ANGLE_0 > 0 Then

                For x = 0 To 3
                    Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).VIS_LANE_HDNG_ANGLE_0 + x, 1)).SignalData
                Next x

                Exit For

            End If

        Next

        GetVisLaneHdngAngles = Values

    End Function

    Private Function GetVisLaneCurvatures() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 3) As Double

        TopDownViewFunctionStatus = "GetVisLaneCurvatures"

        For x = 0 To 3
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            If myDGs(z).VIS_LANE_CURVATURE_0 > 0 Then

                For x = 0 To 3
                    Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).VIS_LANE_CURVATURE_0 + x, 1)).SignalData
                Next x

                Exit For

            End If

        Next

        GetVisLaneCurvatures = Values

    End Function

    Private Function GetVisLaneCurvatureDrvs() As Double()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 3) As Double

        TopDownViewFunctionStatus = "GetVisLaneCurvatureDrvs"

        For x = 0 To 3
            Values(x) = 0.0
        Next

        For z = 0 To myDGs.Count - 1

            If myDGs(z).VIS_LANE_CURVATURE_DRV_0 > 0 Then

                For x = 0 To 3
                    Values(x) = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).VIS_LANE_CURVATURE_DRV_0 + x, 1)).SignalData
                Next x

                Exit For

            End If

        Next

        GetVisLaneCurvatureDrvs = Values

    End Function

    Private Function GetVisLaneQualities() As Integer()

        Dim z As Integer
        Dim x As Integer

        Dim Values(0 To 3) As Integer

        Dim ErrorThrown As Boolean

        Try

            TopDownViewFunctionStatus = "GetVisLaneQualities"

            For x = 0 To 3
                Values(x) = 0
            Next

            For z = 0 To myDGs.Count - 1

                If myDGs(z).VIS_LANE_QUALITY_0 > 0 Then

                    For x = 0 To 3
                        Values(x) = CInt(GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).VIS_LANE_QUALITY_0, x + 1)).SignalData)
                    Next x

                    Exit For

                End If

            Next

        Catch ex As Exception

            If ErrorThrown = False Then
                ErrorThrown = True
                HandleUserMessageLogging("GMRC", "GetVisLaneQualities: " & ex.Message)
            End If

        Finally

            GetVisLaneQualities = Values

        End Try

    End Function

    Private Function GetVpathLongPos() As Double

        Dim z As Integer

        TopDownViewFunctionStatus = "GetVpathLongPos"

        For z = 0 To myDGs.Count - 1

            If myDGs(z).TD_VPATH_LONG_OFST > 0 Then
                GetVpathLongPos = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_VPATH_LONG_OFST, 1)).SignalData

                Exit For
            End If

        Next


    End Function
    Private Function GetVpathLatPos() As Double

        Dim z As Integer

        TopDownViewFunctionStatus = "GetVpathLatPos"

        For z = 0 To myDGs.Count - 1
            If myDGs(z).TD_VPATH_LAT_OFST > 0 Then
                GetVpathLatPos = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_VPATH_LAT_OFST, 1)).SignalData
                Exit For
            End If
        Next

    End Function

    Private Sub DrawPathsPE(ByVal paintevent As PaintEventArgs, ByVal pathtype As Integer)

        'This is filled in during updates and should be used to display data
        'All data is in window pixel coordinates 
        'The interprertation of m_PathPt depends on m_NumLanesShown

        'if m_NumLanesShown is LANE_STYLE_CENTERLINE then m_PathPt[0][i] describes the centerline
        'if m_NumLanesShown is LANE_STYLE_ONE_LANE then m_PathPt[0][i] and [1][i] are the L and R lanes
        'if m_NumLanesShown is LANE_STYLE_THREE_LANES then m_PathPt[0][i] thru [4][i] are (LL L R RR) lanes

        '0 m_NumLanesShown = centerline
        '1 = one lane
        '3 = three lanes

        'Dim color As Long
        Dim linestyle(2) As Integer 'UINT?
        Dim linewidth(2) As Integer 'UINT?
        Dim lineOffset(2) As Integer
        'Dim lnIdx As Integer
        Dim i As Integer
        Dim k As Integer
        Dim showLine(2) As Boolean

        Dim myPathPoints() As Point

        Dim LaneQualities(0 To 3) As Integer

        Dim Display As Boolean

        Static ErrorThrown As Boolean

        Try

            TopDownViewFunctionStatus = "DrawPathsPE"

            showLine(0) = True
            showLine(1) = False

            lineOffset(0) = 0
            lineOffset(1) = 0

            'ReDim myPathPoints((m_NumPathPoints * 2) + 1)
            ReDim myPathPoints(m_NumPathPoints - 1)

            Select Case (pathtype)

                Case PT_CENTER
                    myPathPoints(k) = m_PathPt(0, 0)

                    For k = 1 To m_NumPathPoints - 1

                        myPathPoints(k) = m_PathPt(0, k)

                    Next k

                    Dim myPen As Pen = New Pen(Color.Black) With {
                        .DashPattern = New Single() {4.0F, 4.0F, 4.0F, 4.0F}
                    }

                    If m_NumPathPoints > 2 Then
                        paintevent.Graphics.DrawLines(myPen, myPathPoints)
                    End If

                    myPen.Dispose()

                ' (m_bSelected) Then
                '		POINT *pts = new POINT[m_NumPathPoints*2+1]
                'CPenEx(Pen(PS_DOT, 1, RGB(0, 0, 0)))
                'CPenSelector(pensl(hDC, Pen))
                'CBrushSelector(bs(RGB(200, 200, 0), hDC))
                '		PolylineEnvelope(m_PathPt[0], m_NumPathPoints, pts, 3)
                'Polyline(hDC, pts, m_NumPathPoints * 2 + 1)
                '		delete [] pts
                'End If

                'break()
                Case PT_VEL_YAW

                    Dim myPen As Pen = New Pen(Color.Red) With {
                        .DashPattern = New Single() {4.0F, 4.0F, 4.0F, 4.0F}
                    }

                    For i = 0 To m_NumLanesShown
                        k = 0

                        myPathPoints(k) = m_PathPt(i, 0)


                        For k = 1 To m_NumPathPoints - 1

                            myPathPoints(k) = m_PathPt(i, k)

                        Next k

                        If m_NumPathPoints > 2 Then
                            paintevent.Graphics.DrawLines(myPen, myPathPoints)
                        End If
                    Next i

                    myPen.Dispose()

                '	for( i=0 i<2 i++ ) {
                '	MoveToEx( hDC, m_PathPt[i][0].x+lineOffset[lnIdx], m_PathPt[i][0].y, NULL)
                '		for( k=1 k<m_NumPathPoints k++ ) {
                '			LineTo( hDC, m_PathPt[i][k].x+lineOffset[lnIdx], m_PathPt[i][k].y )
                '		}
                '	if( m_bSelected ) {
                '		POINT *pts = new POINT[m_NumPathPoints*2+1]
                '  CPenEx(Pen(PS_DOT, 1, RGB(0, 0, 0)))
                '  CPenSelector(pensl(hDC, Pen))
                '  CBrushSelector(bs(RGB(200, 200, 0), hDC))
                '			PolylineEnvelope(m_PathPt[i], m_NumPathPoints, pts, 3)
                '    Polyline(hDC, pts, m_NumPathPoints * 2 + 1)
                '			delete [] pts
                '		}
                '	}
                '    break()
                '    'default:

                Case PT_POLYNOMIAL

                    '0 is 2
                    '1 is 0
                    '2 is 1
                    '3 is 3

                    Dim myPen As Pen = New Pen(Color.SaddleBrown) With {
                        .DashPattern = New Single() {2.0F, 2.0F, 2.0F, 2.0F}
                    }

                    LaneQualities = GetVisLaneQualities()

                    For i = 0 To m_NumLanesShown '0 is host lane left, 1 is host lane right, 2 is left lane left, 3 is right lane right
                        k = 0
                        myPathPoints(k) = m_PathPt(i, 0)
                        Display = False

                        Select Case i

                            Case 0
                                If LaneQualities(2) >= 2 And LaneQualities(2) <= 3 Then
                                    Display = True
                                End If
                            Case 1
                                If LaneQualities(0) >= 2 And LaneQualities(0) <= 3 Then
                                    Display = True
                                End If
                            Case 2
                                If LaneQualities(1) >= 2 And LaneQualities(1) <= 3 Then
                                    Display = True
                                End If
                            Case 3
                                If LaneQualities(3) >= 2 And LaneQualities(3) <= 3 Then
                                    Display = True
                                End If
                        End Select

                        For k = 1 To m_NumPathPoints - 1

                            If Display = True Then
                                myPathPoints(k) = m_PathPt(i, k)
                            Else
                                m_PathPt(i, k).X = CInt(CDbl(m_CenterHorzPxl) + (m_MeterToViewHorzScale * ((0 - m_HorzZeroOffset))))
                                m_PathPt(i, k).Y = CInt(CDbl(m_BottomVertPxl) - (m_MeterToViewVertScale * ((0 - m_VertZeroOffset))))
                                m_PathPt(i, 0).X = CInt(CDbl(m_CenterHorzPxl) + (m_MeterToViewHorzScale * ((0 - m_HorzZeroOffset))))
                                m_PathPt(i, 0).Y = CInt(CDbl(m_CenterHorzPxl) + (m_MeterToViewHorzScale * ((0 - m_HorzZeroOffset))))
                                myPathPoints(k) = m_PathPt(i, k)
                            End If

                        Next k

                        'If m_NumPathPoints > 2 Then
                        If m_NumPathPoints > 2 And Display = True Then
                            paintevent.Graphics.DrawLines(myPen, myPathPoints)
                        End If
                    Next i

                    myPen.Dispose()


                    '			case 3
                    '	for( i=0 i<4 i++ ) {
                    '		MoveToEx( hDC, m_PathPt[i][0].x+lineOffset[lnIdx], m_PathPt[i][0].y, NULL)
                    '		for(k=1k<m_NumPathPointsk++) {
                    '			LineTo(hDC,m_PathPt[i][k].x+lineOffset[lnIdx], m_PathPt[i][k].y)
                    '		}
                    '		if(m_bSelected) {
                    '			POINT *pts = new POINT[m_NumPathPoints*2+1]
                    '    CPenEx(Pen(PS_DOT, 1, RGB(0, 0, 0)))
                    '    CPenSelector(pensl(hDC, Pen))
                    '    CBrushSelector(bs(RGB(200, 200, 0), hDC))
                    '		PolylineEnvelope(m_PathPt[i], m_NumPathPoints, pts, 3)
                    '    Polyline(hDC, pts, m_NumPathPoints * 2 + 1)
                    '			delete [] pts
                    '		}
                    '	}
                    'break()
                    '}
                    '}
            End Select

        Catch ex As Exception

            If ErrorThrown = False Then
                ErrorThrown = True
                HandleUserMessageLogging("GMRC", "DrawPathsPE: " & ex.Message)
            End If

        End Try

    End Sub

    Private Sub SetNumPathPts(ByVal num As Integer) ''changed from as long to as integer

        ' called from CommonFwdPath

        If (num > MAX_NUM_PATH_PTS) Then
            m_NumPathPoints = MAX_NUM_PATH_PTS
        Else
            m_NumPathPoints = num
        End If
    End Sub

    Private Sub CalcFinalPathPoints(ByVal fHalfLaneWidthPxl As Double)

        ' called from CommonFwdPath

        Dim dxPxl As Double
        Dim dyPxl As Double
        Dim norm As Double
        Dim xIncrement As Double
        Dim yIncrement As Double
        Dim i As Integer
        Dim laneIdx As Integer

        Select Case (m_NumLanesShown)
            Case 0
                'for( i=0; i<pDisp->m_NumPathPoints; i++ ) {
                For i = 0 To m_NumPathPoints
                    m_PathPt(0, i).X = CInt((m_fPxlPtX(i))) ''changed to add cint
                    m_PathPt(0, i).Y = CInt((m_fPxlPtY(i))) ''changed to add cint
                Next i
                'break;

            Case 1
                If (m_NumPathPoints > 1) Then
                    For i = 0 To m_NumPathPoints - 1
                        If (i = 0) Then
                            dxPxl = m_fPxlPtX(i + 1) - m_fPxlPtX(i)
                            dyPxl = m_fPxlPtY(i) - m_fPxlPtY(i + 1)
                        ElseIf (i = m_NumPathPoints - 1) Then
                            dxPxl = m_fPxlPtX(i) - m_fPxlPtX(i - 1)
                            dyPxl = m_fPxlPtY(i - 1) - m_fPxlPtY(i)
                        Else
                            dxPxl = m_fPxlPtX(i + 1) - m_fPxlPtX(i - 1)
                            dyPxl = m_fPxlPtY(i - 1) - m_fPxlPtY(i + 1)
                        End If
                        norm = Math.Sqrt(dxPxl * dxPxl + dyPxl * dyPxl)
                        If (norm > 0) Then
                            xIncrement = fHalfLaneWidthPxl * dyPxl / norm
                            yIncrement = fHalfLaneWidthPxl * dxPxl / norm
                        Else
                            xIncrement = fHalfLaneWidthPxl
                            yIncrement = 0
                        End If
                        For laneIdx = 0 To m_NumLanesShown
                            m_PathPt(laneIdx, i).X = CInt((m_fPxlPtX(i) + (xIncrement * ((2 * laneIdx) - m_NumLanesShown)))) ''changed to add cint
                            m_PathPt(laneIdx, i).Y = CInt((m_fPxlPtY(i) + (yIncrement * ((2 * laneIdx) - m_NumLanesShown)))) ''changed to add cint
                        Next laneIdx

                    Next i
                Else
                    For i = 0 To m_NumPathPoints
                        m_PathPt(0, i).X = CInt((m_fPxlPtX(i))) ''changed to add cint
                        m_PathPt(0, i).Y = CInt((m_fPxlPtY(i))) ''changed to add cint
                    Next i
                End If
                'break;

                'default:
            Case 3
                'if( pDisp->m_NumPathPoints > 1 ) {
                If (m_NumPathPoints > 1) Then
                    For i = 0 To m_NumPathPoints - 1
                        '	for( i=0; i<pDisp->m_NumPathPoints; i++ ) {
                        If (i = 0) Then
                            dxPxl = m_fPxlPtX(i + 1) - m_fPxlPtX(i)
                            dyPxl = m_fPxlPtY(i) - m_fPxlPtY(i + 1)
                            'dxPxl = m_fPxlPtX[i+1] - m_fPxlPtX[i];
                            '		dyPxl = m_fPxlPtY[i] - m_fPxlPtY[i+1];
                        ElseIf (i = m_NumPathPoints - 1) Then
                            '	} else if( i == (pDisp->m_NumPathPoints - 1) ) {
                            dxPxl = m_fPxlPtX(i) - m_fPxlPtX(i - 1)
                            dyPxl = m_fPxlPtY(i - 1) - m_fPxlPtY(i)
                            '		dxPxl = m_fPxlPtX[i] - m_fPxlPtX[i-1];
                            '		dyPxl = m_fPxlPtY[i-1] - m_fPxlPtY[i];
                        Else
                            dxPxl = m_fPxlPtX(i + 1) - m_fPxlPtX(i - 1)
                            dyPxl = m_fPxlPtY(i - 1) - m_fPxlPtY(i + 1)
                        End If
                        '	} else {
                        '		dxPxl = m_fPxlPtX[i+1] - m_fPxlPtX[i-1];
                        '		dyPxl = m_fPxlPtY[i-1] - m_fPxlPtY[i+1];
                        '	}
                        '	norm = sqrtf( dxPxl * dxPxl + dyPxl * dyPxl );
                        norm = Math.Sqrt(dxPxl * dxPxl + dyPxl * dyPxl)
                        '	if( norm > 0 ) {
                        If (norm > 0) Then
                            xIncrement = fHalfLaneWidthPxl * dyPxl / norm
                            yIncrement = fHalfLaneWidthPxl * dxPxl / norm
                            '		xIncrement = fHalfLaneWidthPxl * dyPxl / norm;
                            '		yIncrement = fHalfLaneWidthPxl * dxPxl / norm;
                        Else
                            xIncrement = fHalfLaneWidthPxl
                            yIncrement = 0
                            '		xIncrement = fHalfLaneWidthPxl;
                            '		yIncrement = 0;
                        End If

                        '	for( laneIdx=0; laneIdx<4; laneIdx++ ) {
                        For laneIdx = 0 To m_NumLanesShown
                            m_PathPt(laneIdx, i).X = CInt((m_fPxlPtX(i) + (xIncrement * ((2 * laneIdx) - m_NumLanesShown))))
                            m_PathPt(laneIdx, i).Y = CInt((m_fPxlPtY(i) + (yIncrement * ((2 * laneIdx) - m_NumLanesShown))))
                            '		pDisp->m_PathPt[laneIdx][i].x = (int)(m_fPxlPtX[i] + (xIncrement * ((2 * laneIdx) - 3)));
                            '		pDisp->m_PathPt[laneIdx][i].y = (int)(m_fPxlPtY[i] + (yIncrement * ((2 * laneIdx) - 3)));
                        Next
                    Next i
                    '} else {
                Else
                    '	for( i=0; i<pDisp->m_NumPathPoints; i++ ) {
                    For i = 0 To m_NumPathPoints
                        m_PathPt(0, i).X = CInt((m_fPxlPtX(i)))
                        m_PathPt(0, i).Y = CInt((m_fPxlPtY(i)))
                        '		pDisp->m_PathPt[0][i].x = (int)(m_fPxlPtX[i]);
                        '		pDisp->m_PathPt[0][i].y = (int)(m_fPxlPtY[i]);
                        '	}
                    Next i
                End If

                'break;
        End Select


    End Sub

    Private Sub DrawShapes(ByVal PaintEvent As PaintEventArgs, ByVal TD_Object As TD_TargetObjectsClass, ByVal xpos As Double, ByVal ypos As Double, ByVal index As Integer)

        'draws target objects and vehicle object on the top down view when repainting is required

        Dim x As Integer
        Dim y As Integer

        Dim width As Integer
        Dim height As Integer

        Dim myPen As Pen = New Pen(Color.Black)
        Dim myBrush As SolidBrush = New SolidBrush(TD_Object.Color)

        Dim ObjectWidth As Integer
        Dim ObjectHeight As Integer

        Static ErrorThrown As Boolean

        Try

            TopDownViewFunctionStatus = "DrawShapes"

            Select Case TD_Object.TargetObjectID

                Case "Vehicle"

                    x = CInt(xpos) '* m_ZoomFactor ''changed to add cint
                    y = CInt(ypos) '* m_ZoomFactor ''changed to add cint

                    savevehwidth = CSng(VehicleObject.DefaultWidth * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross) * m_ZoomFactor_X) ''changed to add csng
                    savevehheight = CSng(VehicleObject.DefaultHeight * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft) * m_ZoomFactor_Y) ''changed to add csng

                    x = CInt(x - (savevehwidth / 2)) ''changed to add cint
                    y = CInt(y - (savevehheight / 2)) ''changed to add cint

                    width = CInt(savevehwidth) ''changed to add cint
                    height = CInt(savevehheight) ''changed to add cint

                Case "FUSION"

                    TD_Object.MinSizePixels = GmResidentClient.MyTdGraphicsContainer.FusionTargetObject_MinSizePixels

                    If FusionTargetObject.Style <> FusionDisplayType Then
                        FusionTargetObject.Style = FusionDisplayType
                    End If

                    If TD_Object.Style = "Triangle" Then

                        TD_Object.Fill = True

                        x = CInt(-((xpos + TD_Object.LeftOfOrigin) * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross) * m_ZoomFactor_X) + VehicleObject.X_Pos(0)) '* m_ZoomFactor ''changed to add cint
                        y = CInt(-((ypos + TD_Object.FwdOfOrigin) * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft) * m_ZoomFactor_Y) + VehicleObject.Y_Pos(0)) '* m_ZoomFactor ''changed to add cint

                        x = CInt(x - ((TD_Object.MinSizePixels / 2) * m_ZoomFactor_X)) '* m_ZoomFactor ''changed to add cint
                        y = CInt(y - ((TD_Object.MinSizePixels / 2) * m_ZoomFactor_Y)) '* m_ZoomFactor ''changed to add cint

                        width = CInt(TD_Object.MinSizePixels * m_ZoomFactor_X) ''changed to add cint
                        height = CInt(TD_Object.MinSizePixels * m_ZoomFactor_Y) ''changed to add cint

                        'x and y are top, left corner points, shifted to there from what would be considered actual point position based on lat and lon inputs
                        'this is done because the shape drawing routines start at top left corner.  The center of the resulting shape is intended to be
                        'the actual x, y coord pos input from the data.

                        TD_Object.X_Pos(index) = x
                        TD_Object.Y_Pos(index) = y

                    Else 'Object.Style will be set to rectangle in top down view configuration window - object type determines rectangle dimensions...

                        Select Case CInt(TD_Object.GetObjectType(index))

                            Case 0 'CeFSPR_e_ObjTypeUnknwn 0
                                ObjectWidth = 2
                                ObjectHeight = 5
                                TD_Object.Fill = False
                            Case 1 'CeFSPR_e_ObjTypeMotorcyle 1
                                ObjectWidth = 1
                                ObjectHeight = 3
                                TD_Object.Fill = True
                            Case 2 'CeFSPR_e_ObjTypeVehicle 2
                                ObjectWidth = 2
                                ObjectHeight = 5
                                TD_Object.Fill = True
                            Case 3 'CeFSPR_e_ObjTypeTruck 3
                                ObjectWidth = 4
                                ObjectHeight = 10
                                TD_Object.Fill = True
                            Case 4 'CeFSPR_e_ObjTypePedestrian 4
                                ObjectWidth = 1
                                ObjectHeight = 1
                                TD_Object.Fill = False
                            Case 5 'CeFSPR_e_ObjTypeBicycle 5
                                ObjectWidth = 1
                                ObjectHeight = 3
                                TD_Object.Fill = False
                        End Select

                        ObjectWidth = CSng(ObjectWidth * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross) * m_ZoomFactor_X) ''changed to add csng
                        ObjectHeight = CSng(ObjectHeight * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft) * m_ZoomFactor_Y) ''changed to add csng

                        x = CInt(-((xpos + TD_Object.LeftOfOrigin) * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross) * m_ZoomFactor_X) + VehicleObject.X_Pos(0)) '* m_ZoomFactor ''changed to add cint
                        y = CInt(-((ypos + TD_Object.FwdOfOrigin) * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft) * m_ZoomFactor_Y) + VehicleObject.Y_Pos(0)) '* m_ZoomFactor ''changed to add cint

                        'x and y are top, left corner points, shifted to there from what would be considered actual point position based on lat and lon inputs
                        'this is done because the shape drawing routines start at top left corner.  The center of the resulting shape is intended to be
                        'the actual x, y coord pos input from the data.  If we are' using a rectangle, the actual x / y location would be the center of the closest
                        'face of the vehicle, so it would switch from rear of vehicle if y is negative (in front of host) and front of vehicle if y is positive (behind host)

                        'x = CInt(x - (ObjectWidth / 2)) ' * m_ZoomFactor_X)) '* m_ZoomFactor
                        'y = CInt(y - (ObjectHeight / 2)) ' * m_ZoomFactor_Y)) '* m_ZoomFactor

                        'If (y - VehicleObject.Y_Pos(0)) < 0 Then
                        y = CInt(y - (ObjectHeight / 2)) ' * m_ZoomFactor_Y)) '* m_ZoomFactor
                        'Else
                        ''y = CInt(y - (ObjectHeight)) ' * m_ZoomFactor_Y)) '* m_ZoomFactor
                        'End If

                        x = CInt(x - (ObjectWidth / 2)) ' * m_ZoomFactor_X)) '* m_ZoomFactor

                        width = ObjectWidth
                        height = ObjectHeight

                        TD_Object.X_Pos(index) = x
                        TD_Object.Y_Pos(index) = y

                    End If

                Case Else

                    If TD_Object.TargetObjectID = "LRR" Then 'LRR_Object.Style = "Square"
                        TD_Object.MinSizePixels = GmResidentClient.MyTdGraphicsContainer.LRR_Object_MinSizePixels
                    ElseIf TD_Object.TargetObjectID = "LFSRR" Or TD_Object.TargetObjectID = "RFSRR" Then 'LSRR_Object.Style = "Circle", RSRR_Object.Style = "Circle"
                        TD_Object.MinSizePixels = GmResidentClient.MyTdGraphicsContainer.SRR_Objects_MinSizePixels
                    ElseIf TD_Object.TargetObjectID = "VIS" Then 'VIS_Object.Style = "Diamond"
                        TD_Object.MinSizePixels = GmResidentClient.MyTdGraphicsContainer.VIS_Object_MinSizePixels
                    Else
                        'this should never happen...
                    End If

                    x = CInt(-((xpos + TD_Object.LeftOfOrigin) * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross) * m_ZoomFactor_X) + VehicleObject.X_Pos(0)) '* m_ZoomFactor ''changed to add cint
                    y = CInt(-((ypos + TD_Object.FwdOfOrigin) * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft) * m_ZoomFactor_Y) + VehicleObject.Y_Pos(0)) '* m_ZoomFactor ''changed to add cint

                    x = CInt(x - ((TD_Object.MinSizePixels / 2) * m_ZoomFactor_X)) '* m_ZoomFactor ''changed to add cint
                    y = CInt(y - ((TD_Object.MinSizePixels / 2) * m_ZoomFactor_Y)) '* m_ZoomFactor ''changed to add cint

                    width = CInt(TD_Object.MinSizePixels * m_ZoomFactor_X) ''changed to add cint
                    height = CInt(TD_Object.MinSizePixels * m_ZoomFactor_Y) ''changed to add cint

                    'x and y are top, left corner points, shifted to there from what would be considered actual point position based on lat and lon inputs
                    'this is done because the shape drawing routines start at top left corner.  The center of the resulting shape is intended to be
                    'the actual x, y coord pos input from the data.

                    TD_Object.X_Pos(index) = x
                    TD_Object.Y_Pos(index) = y

            End Select

            'put x y at left/top of drawing box relative to actual x y location so that
            'the image drawn will be centered around the actual x y location

            Select Case TD_Object.Style
                Case "Circle"
                    PaintEvent.Graphics.DrawEllipse(myPen, x, y, width, height)
                    PaintEvent.Graphics.FillEllipse(myBrush, x, y, width, height)

                Case "Triangle"
                    Dim tripoints(2) As PointF

                    'if zoomed, it moves, if not zoomed it does not move...

                    'y = y + (TD_Object.MinSizePixels) 'This keeps image from translating when resizing, but translates on zoom
                    'if commented out,  translates when resizing but does not translate on zoom

                    y = CInt(y + (TD_Object.MinSizePixels * m_ZoomFactor_Y))

                    tripoints(0).X = x
                    tripoints(0).Y = y

                    tripoints(1).X = x + width
                    tripoints(1).Y = y

                    tripoints(2).X = CSng(x + (width / 2)) ''changed to add csng
                    tripoints(2).Y = y - height

                    PaintEvent.Graphics.DrawPolygon(myPen, tripoints)

                    'If CInt(TD_Object.GetObjectType(index)) > 3 Then
                    'TD_Object.Fill = False
                    'Else
                    'TD_Object.Fill = True
                    'End If

                    If TD_Object.Fill = True Then
                        PaintEvent.Graphics.FillPolygon(myBrush, tripoints)
                    End If


                Case "Rectangle", "Square"

                    PaintEvent.Graphics.DrawRectangle(myPen, x, y, width, height)
                    If TD_Object.Fill = True Then
                        PaintEvent.Graphics.FillRectangle(myBrush, x, y, width, height)
                    End If

                    If TD_Object.TargetObjectID = "Vehicle" Then
                        TD_Object.Width = width
                        TD_Object.Height = height

                        If TD_Object.Top <> y Or TD_Object.Left <> x Then
                            TD_Object.Top = y
                            TD_Object.Left = x
                            TD_Object.BackColor = Color.Yellow
                        End If

                    End If

                Case "Diamond"

                    Dim diapoints(3) As PointF

                    x = CInt(x + (TD_Object.MinSizePixels / 2) * m_ZoomFactor_X) ''changed to add cint ORIG
                    y = CInt(y + (TD_Object.MinSizePixels / 2) * m_ZoomFactor_Y) ''changed to add cint ORIG


                    diapoints(0).X = x
                    diapoints(0).Y = CSng(y - (height / 2)) ''changed to add csng

                    diapoints(1).X = CSng(x - (width / 2)) ''changed to add csng
                    diapoints(1).Y = y

                    diapoints(2).X = x
                    diapoints(2).Y = CSng(y + (height / 2)) ''changed to add csng

                    diapoints(3).X = CSng(x + (width / 2)) ''changed to add csng
                    diapoints(3).Y = y

                    PaintEvent.Graphics.DrawPolygon(myPen, diapoints)

                    If CInt(TD_Object.GetObjectType(index)) > 3 Then
                        TD_Object.Fill = False
                    Else
                        TD_Object.Fill = True
                    End If


                    If TD_Object.Fill = True Then
                        PaintEvent.Graphics.FillPolygon(myBrush, diapoints)
                    End If

            End Select

        Catch ex As Exception

            If ErrorThrown = False Then
                ErrorThrown = True
                HandleUserMessageLogging("GMRC", "DrawShapes: " & ex.Message)
            End If

        Finally

            myPen.Dispose()
            myBrush.Dispose()

        End Try

    End Sub

    Private Sub DrawPoints(ByVal PaintEvent As PaintEventArgs, ByVal pathtype As Integer)

        'draws individual points for desired path and blue line path

        Dim x As Integer

        Dim xpos As Integer
        Dim ypos As Integer

        Dim myPen As Pen = New Pen(Color.Black)
        Dim myBrush As SolidBrush = New SolidBrush(Color.Black)

        Static ErrorThrown As Boolean

        Try

            Select Case pathtype

                Case PT_LXCR_BLUELINE

                    Dim myBluePen As Pen = New Pen(TopDownViewConfiguration.LXCRBlueLinePathColor)
                    Dim myBlueBrush As SolidBrush = New SolidBrush(TopDownViewConfiguration.LXCRBlueLinePathColor)

                    myPen = myBluePen
                    myBrush = myBlueBrush

                Case PT_LXCR_TGTLANE

                    Dim myRedPen As Pen = New Pen(TopDownViewConfiguration.LXCRTargetLanePathColor)
                    Dim myRedBrush As SolidBrush = New SolidBrush(TopDownViewConfiguration.LXCRTargetLanePathColor)

                    myPen = myRedPen
                    myBrush = myRedBrush

                'Case PT_LXCR_BLENDPATH

                '    Dim myGreenPen As Pen = New Pen(TopDownViewConfiguration.LXCRBlendedPathColor)
                '    Dim myGreenBrush As SolidBrush = New SolidBrush(TopDownViewConfiguration.LXCRBlendedPathColor)

                '    myPen = myGreenPen
                '    myBrush = myGreenBrush

                Case PT_LXCR_BLENDPATH_W_COEFS

                    Dim myGreenPen As Pen = New Pen(TopDownViewConfiguration.LXCRBlendedPathColorWCoefs)
                    Dim myGreenBrush As SolidBrush = New SolidBrush(TopDownViewConfiguration.LXCRBlendedPathColorWCoefs)

                    myPen = myGreenPen
                    myBrush = myGreenBrush

                Case PT_LXCR_BLENDPATH_W_COORDS

                    Dim myGreenPen As Pen = New Pen(TopDownViewConfiguration.LXCRBlendedPathColorWCoords)
                    Dim myGreenBrush As SolidBrush = New SolidBrush(TopDownViewConfiguration.LXCRBlendedPathColorWCoords)

                    myPen = myGreenPen
                    myBrush = myGreenBrush

                Case PT_BLENDPATH_LOOKAHEAD

                    Dim myBlendPathPen As Pen = New Pen(Color.Green)
                    Dim myBlendPathBrush As SolidBrush = New SolidBrush(Color.Green)

                    myPen = myBlendPathPen
                    myBrush = myBlendPathBrush

                    xpos = CInt(-((LatLookAhead) * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross) * m_ZoomFactor_X) + VehicleObject.X_Pos(0)) '* m_ZoomFactor ''changed to add cint
                    ypos = CInt(-((LonLookAhead) * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft) * m_ZoomFactor_Y) + VehicleObject.Y_Pos(0)) '* m_ZoomFactor ''changed to add cint

                    PaintEvent.Graphics.DrawEllipse(myPen, (xpos - (GmResidentClient.MyTdGraphicsContainer.DefaultPointSize * 2)), (ypos - (GmResidentClient.MyTdGraphicsContainer.DefaultPointSize * 2)), GmResidentClient.MyTdGraphicsContainer.DefaultPointSize * 4, GmResidentClient.MyTdGraphicsContainer.DefaultPointSize * 4)
                    PaintEvent.Graphics.FillEllipse(myBrush, (xpos - (GmResidentClient.MyTdGraphicsContainer.DefaultPointSize * 2)), (ypos - (GmResidentClient.MyTdGraphicsContainer.DefaultPointSize * 2)), GmResidentClient.MyTdGraphicsContainer.DefaultPointSize * 4, GmResidentClient.MyTdGraphicsContainer.DefaultPointSize * 4)

                    myPen.Dispose()
                    myBrush.Dispose()

                    Exit Sub

                    'Case PT_BLUELINE

                    '    Dim myBluePen As Pen = New Pen(Color.Blue)
                    '    Dim myBlueBrush As SolidBrush = New SolidBrush(Color.Blue)

                    '    myPen = myBluePen
                    '    myBrush = myBlueBrush

                    'Case PT_DESIRED

                    '    Dim myGreenPen As Pen = New Pen(Color.Green)
                    '    Dim myGreenBrush As SolidBrush = New SolidBrush(Color.Green)

                    '    myPen = myGreenPen
                    '    myBrush = myGreenBrush

            End Select

            'For x = 0 To 79
            For x = 0 To 199

                xpos = CInt(-((latposvalues(x)) * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross) * m_ZoomFactor_X) + VehicleObject.X_Pos(0)) '* m_ZoomFactor ''changed to add cint
                ypos = CInt(-((lonposvalues(x)) * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft) * m_ZoomFactor_Y) + VehicleObject.Y_Pos(0)) '* m_ZoomFactor ''changed to add cint

                If lonposvalues(x) > 2.5 Then
                    PaintEvent.Graphics.DrawEllipse(myPen, xpos - (GmResidentClient.MyTdGraphicsContainer.DefaultPointSize \ 2), ypos - (GmResidentClient.MyTdGraphicsContainer.DefaultPointSize \ 2), GmResidentClient.MyTdGraphicsContainer.DefaultPointSize, GmResidentClient.MyTdGraphicsContainer.DefaultPointSize)
                    PaintEvent.Graphics.FillEllipse(myBrush, xpos - (GmResidentClient.MyTdGraphicsContainer.DefaultPointSize \ 2), ypos - (GmResidentClient.MyTdGraphicsContainer.DefaultPointSize \ 2), GmResidentClient.MyTdGraphicsContainer.DefaultPointSize, GmResidentClient.MyTdGraphicsContainer.DefaultPointSize)
                End If

            Next x

        Catch ex As Exception

            If ErrorThrown = False Then
                ErrorThrown = True
                HandleUserMessageLogging("GMRC", "DrawPoints: " & ex.Message)
            End If

        Finally

            myPen.Dispose()
            myBrush.Dispose()

        End Try

    End Sub

    Private Sub DrawFieldOFView(ByVal PaintEvent As PaintEventArgs, ByVal TD_Object As TD_TargetObjectsClass)

        'draws field of view lines when repainting is required

        Dim x As Single
        Dim y As Single

        Dim x2 As Single
        Dim y2 As Single

        Static ErrorThrown As Boolean

        Dim myPen As Pen = New Pen(TD_Object.Color)

        Try

            TopDownViewFunctionStatus = "DrawFieldOFView"

            PaintEvent.Graphics.SmoothingMode = SmoothingMode.AntiAlias

            myPen.DashPattern = New Single() {8.0F, 2.0F, 8.0F, 2.0F}

            x = CSng((VehicleObject.X_Pos(0) + TD_Object.LeftOfOrigin)) '* m_ZoomFactor ''changed to add csng
            y = CSng((VehicleObject.Y_Pos(0) + TD_Object.FwdOfOrigin)) '* m_ZoomFactor ''changed to add csng

            y2 = CSng(y - (300 * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft))) '* m_ZoomFactor ''changed to add csng

            x2 = CSng(x - ((Math.Tan(((TD_Object.FieldOfView / 2) + TD_Object.SensorCCWRotationDegs) * F_DEG_TO_RAD) * 300)) * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross)) '* m_ZoomFactor ''changed to add csng

            'x1, y1, x2, y2

            PaintEvent.Graphics.DrawLine(myPen, x, y, x2, y2)

            x2 = CSng(x + ((Math.Tan(((TD_Object.FieldOfView / 2) - TD_Object.SensorCCWRotationDegs) * F_DEG_TO_RAD) * 300)) * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross)) ''changed to add csng

            PaintEvent.Graphics.DrawLine(myPen, x, y, x2, y2)

        Catch ex As Exception

            If ErrorThrown = False Then
                ErrorThrown = True
                HandleUserMessageLogging("GMRC", "DrawFieldOFView: " & ex.Message)
            End If

        Finally

            myPen.Dispose()

        End Try

    End Sub

    Private Sub DrawLines(ByVal PaintEvent As PaintEventArgs)

        'draws the grid lines when repainting is required

        Dim x As Single
        Dim y As Single

        Dim z As Single

        Dim x_offset As Single
        Dim y_offset As Single

        Dim myPen As Pen = New Pen(Color.DimGray)

        Static ErrorThrown As Boolean

        Try

            TopDownViewFunctionStatus = "DrawLines"

            PaintEvent.Graphics.SmoothingMode = SmoothingMode.AntiAlias

            myPen.DashPattern = New Single() {4.0F, 4.0F, 4.0F, 4.0F}
            myPen.Width = 0.5

            x = CSng(VehicleObject.X_Pos(0)) ''changed to add csng
            y = CSng(VehicleObject.Y_Pos(0)) ''changed to add csng

            PaintEvent.Graphics.DrawLine(myPen, x, 0, x, GmResidentClient.MyTdGraphicsContainer.Height)
            PaintEvent.Graphics.DrawLine(myPen, 0, y, GmResidentClient.MyTdGraphicsContainer.Width, y)

            x_offset = CSng((_GridSpacingAcross / (_FullSizeViewAreaAcross / _ViewWindowSizeWidth)) * m_ZoomFactor_X) ''changed to add csng
            y_offset = CSng((_GridSpacingForeAft / (_FullSizeViewAreaForeAft / _ViewWindowSizeHeight)) * m_ZoomFactor_Y) ''changed to add csng

            z = x
            Do While z - x_offset >= 0
                z = z - x_offset
                PaintEvent.Graphics.DrawLine(myPen, z, 0, z, GmResidentClient.MyTdGraphicsContainer.Height)
            Loop
            z = x
            Do While z + x_offset <= _ViewWindowSizeWidth
                z = z + x_offset
                PaintEvent.Graphics.DrawLine(myPen, z, 0, z, GmResidentClient.MyTdGraphicsContainer.Height)
            Loop
            z = y
            Do While z - y_offset >= 0
                z = z - y_offset
                PaintEvent.Graphics.DrawLine(myPen, 0, z, GmResidentClient.MyTdGraphicsContainer.Width, z)
            Loop
            z = y
            Do While z + y_offset <= _ViewWindowSizeHeight
                z = z + y_offset
                PaintEvent.Graphics.DrawLine(myPen, 0, z, GmResidentClient.MyTdGraphicsContainer.Width, z)
            Loop


        Catch ex As Exception

            If ErrorThrown = False Then
                ErrorThrown = True
                HandleUserMessageLogging("GMRC", "DrawLines: " & ex.Message)
            End If

        Finally

            myPen.Dispose()

        End Try

    End Sub

    Private Sub DrawDangerZone(ByVal PaintEvent As PaintEventArgs)

        Dim xL(7) As Double
        Dim yL(7) As Double

        Dim xR(7) As Double
        Dim yR(7) As Double

        Dim polypointsL(7) As PointF
        Dim polypointsR(7) As PointF

        Static ErrorThrown As Boolean

        Dim myPen As Pen = New Pen(Color.Red)

        Try

            TopDownViewFunctionStatus = "DrawDangerZone"

            PaintEvent.Graphics.SmoothingMode = SmoothingMode.AntiAlias

            myPen.Width = 1.0

            xL = GetLXCRDangerZoneLeftXPos()
            yL = GetLXCRDangerZoneLeftYPos()

            xR = GetLXCRDangerZoneRightXPos()
            yR = GetLXCRDangerZoneRightYPos()

            For x = 0 To 7

                polypointsL(x).X = -((yL(x) * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross) * m_ZoomFactor_X)) + VehicleObject.X_Pos(0)
                polypointsL(x).Y = -((xL(x) * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft) * m_ZoomFactor_Y)) + VehicleObject.Y_Pos(0) '- _COG_To_COV_Offset

                polypointsR(x).X = -((yR(x) * (_ViewWindowSizeWidth / _FullSizeViewAreaAcross) * m_ZoomFactor_X)) + VehicleObject.X_Pos(0)
                polypointsR(x).Y = -((xR(x) * (_ViewWindowSizeHeight / _FullSizeViewAreaForeAft) * m_ZoomFactor_Y)) + VehicleObject.Y_Pos(0) '- _COG_To_COV_Offset

            Next

            PaintEvent.Graphics.DrawPolygon(myPen, polypointsL)

            PaintEvent.Graphics.DrawPolygon(myPen, polypointsR)


        Catch ex As Exception

            If ErrorThrown = False Then
                ErrorThrown = True
                HandleUserMessageLogging("GMRC", "DrawDangerZone: " & ex.Message)
            End If

        Finally
            myPen.Dispose()
        End Try

    End Sub

    Private Sub HandlePaintEvent(ByVal PaintEvent As PaintEventArgs, ByVal TD_Object As TD_TargetObjectsClass)

        'Called from the TDGrahicsContainer Paint Event.  Determines what needs to be redrawn and calls
        'the appropriate routines

        Dim x As Double 'was single
        Dim y As Double 'was single
        Dim i As Integer 'changed from short to integer

        Dim DrawIt As Boolean

        Static ErrorThrown As Boolean

        Try

            'If FixedXPosArray Is Nothing Then
            'For x = 0 To 199
            'ReDim Preserve FixedXPosArray(x)
            'FixedXPosArray(x) = x
            'Next
            'End If

            'Dim hdc As IntPtr = PaintEvent.Graphics.GetHdc()

            If TD_Object.TargetObjectID <> "Vehicle" Then

                TopDownViewFunctionStatus = "TD_Object.TargetObjectID <> Vehicle"

                Select Case TD_Object.TargetObjectID

                    Case "VIS"
                        DrawIt = TopDownViewConfiguration.VISFOVOnToolStripMenuItem.Checked
                    Case "LFSRR"
                        DrawIt = TopDownViewConfiguration.LFSRRFOVOnToolStripMenuItem.Checked
                    Case "RFSRR"
                        DrawIt = TopDownViewConfiguration.RFSRRFOVOnToolStripMenuItem.Checked
                    Case "LRR"
                        DrawIt = TopDownViewConfiguration.LRRFOVOnToolStripMenuItem.Checked

                End Select

                If DrawIt = True Then
                    DrawFieldOFView(PaintEvent, TD_Object)
                End If

            End If

            For i = 1 To TD_Object.NumObjects

                If TD_Object.TargetObjectID <> "Vehicle" Then

                    TopDownViewFunctionStatus = "Looping NumObjects: TD_Object.TargetObjectID <> Vehicle"

                    If Not GmResidentClient.MySignalDataWithTime Is Nothing Then


                        y = TD_Object.GetRelLongPos(i)
                        x = TD_Object.GetRelLatPos(i)

                        If (x - TD_Object.LeftOfOrigin) <> 0 Or (y - TD_Object.FwdOfOrigin) <> 0 Then
                            TD_Object.TargetVisible(i) = True
                        Else
                            TD_Object.TargetVisible(i) = False
                            TD_Object.X_Pos(i) = 0
                            TD_Object.Y_Pos(i) = 0
                        End If

                        If Debugger.IsAttached Then
                            TD_Object.TargetVisible(i) = True
                        End If

                        If TD_Object.TargetVisible(i) = True Then
                            DrawShapes(PaintEvent, TD_Object, x, y, i)
                        End If

                    End If

                Else

                    TopDownViewFunctionStatus = "Looping NumObjects: TD_Object.TargetObjectID = Vehicle"

                    TD_Object.X_Pos(0) = (_ViewWindowSizeWidth) / 2
                    TD_Object.Y_Pos(0) = ((_ViewWindowSizeHeight) * ((100 - _HostVehiclePercentFromBottom) / 100)) - _HostVehicleDimForwardOfCG

                    VehicleObject.X_Pos(0) = VehicleObject.X_Pos(0) + Offset_X
                    VehicleObject.Y_Pos(0) = VehicleObject.Y_Pos(0) + Offset_Y

                    x = TD_Object.X_Pos(0)
                    y = TD_Object.Y_Pos(0)

                    DrawShapes(PaintEvent, TD_Object, x, y, 0)

                End If

            Next i

            DrawLines(PaintEvent)

            If Not GmResidentClient.MySignalDataWithTime Is Nothing Then
                If UBound(GmResidentClient.MySignalDataWithTime) > 0 Then

                    If TopDownViewConfiguration.TCPathDisplayOnToolStripMenuItem.Checked Then
                        If CalcPath(PT_CENTER) = True Then
                            DrawPathsPE(PaintEvent, PT_CENTER)
                        End If
                    End If

                    If TopDownViewConfiguration.VPathDisplayOnToolStripMenuItem.Checked Then
                        If CalcPath(PT_VEL_YAW) = True Then
                            DrawPathsPE(PaintEvent, PT_VEL_YAW)
                        End If
                    End If

                    'If TopDownViewConfiguration.LXCRDesiredPathDisplayOnToolStripMenuItem.Checked Then
                    'If CalcPath(PT_DESIRED) = True Then
                    'DrawPoints(PaintEvent, PT_DESIRED)
                    'End If
                    'End If

                    'If TopDownViewConfiguration.BlueLinePathDisplayOnToolStripMenuItem.Checked Then
                    'If CalcPath(PT_BLUELINE) = True Then
                    'DrawPoints(PaintEvent, PT_BLUELINE)
                    'End If
                    'End If

                    If TopDownViewConfiguration.LXCRBlueLinePathDisplayOnToolStripMenuItem.Checked Then
                        If CalcPath(PT_LXCR_BLUELINE) = True Then
                            DrawPoints(PaintEvent, PT_LXCR_BLUELINE)
                        End If
                    End If

                    If TopDownViewConfiguration.LXCRTargetLaneDisplayOnToolStripMenuItem.Checked Then
                        If CalcPath(PT_LXCR_TGTLANE) = True Then
                            DrawPoints(PaintEvent, PT_LXCR_TGTLANE)
                        End If
                    End If

                    If TopDownViewConfiguration.LXCRBlendedPathDisplayOnToolStripMenuItem.Checked Then
                        If CalcPath(PT_LXCR_BLENDPATH_W_COEFS) = True Then

                            DrawPoints(PaintEvent, PT_LXCR_BLENDPATH_W_COEFS)
                            DrawPoints(PaintEvent, PT_BLENDPATH_LOOKAHEAD)

                        End If
                        If CalcPath(PT_LXCR_BLENDPATH_W_COORDS) = True Then

                            DrawPoints(PaintEvent, PT_LXCR_BLENDPATH_W_COORDS)

                        End If
                    End If

                    If CalcPath(PT_POLYNOMIAL) = True Then
                        DrawPathsPE(PaintEvent, PT_POLYNOMIAL)
                    End If

                    'ADD call to CalcPath using other pathtypes here....

                    If TopDownViewConfiguration.DangerZoneDisplayOnToolStripMenuItem.Checked = True Then
                        DrawDangerZone(PaintEvent)
                    End If

                End If
            End If

        Catch ex As Exception

            If ErrorThrown = False Then
                ErrorThrown = True
                HandleUserMessageLogging("GMRC", "HandlePaintEvent: " & ex.Message & " - " & TopDownViewFunctionStatus)
            End If

        End Try

    End Sub

    Private Sub SetupTopDownObjects()

        'Called from SetupTopDownView...  Initializes target object properties.

        'NOTE!!!  - If the number of objects to be displayed changes by adding to the number of grid columns in the signal list file, then
        'the .NumObjects properties will have to change accordingly...

        Dim x As Integer

        FusionTargetObject = New TD_TargetObjectsClass With {
            .TargetObjectID = "FUSION"
        }

        FusionTargetObject.Name = FusionTargetObject.TargetObjectID

        'FusionTargetObject.Style = "Triangle"
        FusionTargetObject.Style = "Rectangle"

        FusionTargetObject.Style = FusionDisplayType

        FusionTargetObject.Color = Color.Blue
        FusionTargetObject.MinSizePixels = 10
        FusionTargetObject.LeftOfOrigin = 0
        FusionTargetObject.FwdOfOrigin = 0
        FusionTargetObject.SensorCCWRotationDegs = 0
        FusionTargetObject.Fill = True
        FusionTargetObject.ShowFieldOfView = True
        FusionTargetObject.FieldOfView = 160
        FusionTargetObject.NumObjects = 10
        'NOTE!!!  - If the number of objects to be displayed changes by adding to the number of grid columns in the signal list file, then
        'the .NumObjects properties will have to change accordingly...


        For x = 0 To FusionTargetObject.NumObjects
            FusionTargetObject.X_Pos(x) = 0
            FusionTargetObject.Y_Pos(x) = 0
            FusionTargetObject.TargetVisible(x) = False
        Next x

        'LRR_Object.NumObjects = 20
        LRR_Object = New TD_TargetObjectsClass With {
            .TargetObjectID = "LRR",
            .Style = "Square",
            .Color = Color.Green,
            .MinSizePixels = 5,
            .LeftOfOrigin = 0,
            .FwdOfOrigin = 2.2,
            .SensorCCWRotationDegs = 0,
            .Fill = True,
            .ShowFieldOfView = True,
            .FieldOfView = 35,
            .NumObjects = 6
        }
        'NOTE!!!  - If the number of objects to be displayed changes by adding to the number of grid columns in the signal list file, then
        'the .NumObjects properties will have to change accordingly...


        For x = 0 To LRR_Object.NumObjects
            LRR_Object.X_Pos(x) = 0
            LRR_Object.Y_Pos(x) = 0
            LRR_Object.TargetVisible(x) = False
        Next x


        'LSRR_Object.NumObjects = 10
        LSRR_Object = New TD_TargetObjectsClass With {
            .TargetObjectID = "LFSRR",
            .Style = "Circle",
            .Color = Color.Gray,
            .MinSizePixels = 5,
            .LeftOfOrigin = 0.35,
            .FwdOfOrigin = 2.2,
            .SensorCCWRotationDegs = 10.0,
            .Fill = True,
            .ShowFieldOfView = True,
            .FieldOfView = 60,
            .NumObjects = 6
        }
        'NOTE!!!  - If the number of objects to be displayed changes by adding to the number of grid columns in the signal list file, then
        'the .NumObjects properties will have to change accordingly...


        For x = 0 To LSRR_Object.NumObjects
            LSRR_Object.X_Pos(x) = 0
            LSRR_Object.Y_Pos(x) = 0
            LSRR_Object.TargetVisible(x) = False
        Next x


        'RSRR_Object.NumObjects = 10
        RSRR_Object = New TD_TargetObjectsClass With {
            .TargetObjectID = "RFSRR",
            .Style = "Circle",
            .Color = Color.Violet,
            .MinSizePixels = 5,
            .LeftOfOrigin = -0.35,
            .FwdOfOrigin = 2.2,
            .SensorCCWRotationDegs = -10.0,
            .Fill = True,
            .ShowFieldOfView = True,
            .FieldOfView = 60,
            .NumObjects = 6
        }
        'NOTE!!!  - If the number of objects to be displayed changes by adding to the number of grid columns in the signal list file, then
        'the .NumObjects properties will have to change accordingly...


        For x = 0 To RSRR_Object.NumObjects
            RSRR_Object.X_Pos(x) = 0
            RSRR_Object.Y_Pos(x) = 0
            RSRR_Object.TargetVisible(x) = False

        Next x

        VIS_Object = New TD_TargetObjectsClass With {
            .TargetObjectID = "VIS",
            .Style = "Diamond",
            .Color = Color.Red,
            .MinSizePixels = 5,
            .LeftOfOrigin = -0.5,
            .FwdOfOrigin = 0.35,
            .SensorCCWRotationDegs = 0,
            .Fill = True,
            .ShowFieldOfView = True,
            .FieldOfView = 42
        }

        'FCM CHANGE - Changed logic here so we now check for CSAV2 and set to 10, otherwise for any other ProjectName we set to 12...
        'If ProjectName <> "LowContent" And ProjectName <> "HighContent" Then
        'VIS_Object.NumObjects = 12
        'Else
        'VIS_Object.NumObjects = 10
        'End If

        If ProjectName = "CSAV2" Then
            VIS_Object.NumObjects = 12
        Else
            VIS_Object.NumObjects = 10
        End If


        'NOTE!!!  - If the number of objects to be displayed changes by adding to the number of grid columns in the signal list file, then
        'the .NumObjects properties will have to change accordingly...  In the case of LowContent, we have limited the number of grid columns
        'for vision to 10!!!

        'VIS_Object.NumObjects = 5

        For x = 0 To VIS_Object.NumObjects
            VIS_Object.X_Pos(x) = 0
            VIS_Object.Y_Pos(x) = 0
            VIS_Object.TargetVisible(x) = False

        Next x


        'VehicleObject.DefaultHeight = CInt(_HostVehicleDimForwardOfCG + _HostVehicleDimRearwardOfCG) ''changed to add cint
        VehicleObject = New TD_TargetObjectsClass With {
            .TargetObjectID = "Vehicle",
            .Name = "VEHICLE",
            .Visible = True,
            .Style = "Rectangle",
            .Color = Color.Yellow,
            .DefaultHeight = CInt(_HostVehicleDimLongitudinal), ''changed to add cint
            .DefaultWidth = CInt(_HostVehicleDimLateral), ''changed to add cint
            .Fill = True,
            .NumObjects = 1
        }

        VehicleObject.X_Pos(0) = CInt((_ViewWindowSizeWidth) / 2) ''changed to add cint
        VehicleObject.Y_Pos(0) = CInt(((_ViewWindowSizeHeight) * ((100 - _HostVehiclePercentFromBottom) / 100)) - _HostVehicleDimForwardOfCG) ''changed to add cint

        VehicleObject.Parent = Me

        VehicleObject.Left = CInt((_ViewWindowSizeWidth) / 2) ''changed to add cint
        VehicleObject.Top = CInt(((_ViewWindowSizeHeight) * ((100 - _HostVehiclePercentFromBottom) / 100)) - _HostVehicleDimForwardOfCG) ''changed to add cint

        VehicleObject.Width = VehicleObject.DefaultWidth
        VehicleObject.Height = VehicleObject.DefaultHeight
        VehicleObject.BackColor = Color.Transparent

        VehicleObject.BringToFront()

        Dim mytooltip As New ToolTip With {
            .AutomaticDelay = 0,
            .AutoPopDelay = 0,
            .UseAnimation = False,
            .UseFading = False
        }
        mytooltip.SetToolTip(VehicleObject, VehicleObject.TargetObjectID)

    End Sub

    Public Sub SetupTopDownView()

        'Called during initialization...

        'Initializes some of the graphics container properties for the top down view display.

        Try

            m_ZoomFactor = 1
            m_ZoomFactor_X = 1
            m_ZoomFactor_Y = 1

            GmResidentClient.MyTdGraphicsContainer.Text = "Top Down View"
            GmResidentClient.MyTdGraphicsContainer.Name = "Top Down View"

            GmResidentClient.MyTdGraphicsContainer.HostVehicleDimForwardOfCG = 2.25
            GmResidentClient.MyTdGraphicsContainer.HostVehicleDimRearwardOfCG = 2.75
            GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLateral = 2.0
            GmResidentClient.MyTdGraphicsContainer.HostVehicleDimLongitudinal = 5.0

            GmResidentClient.MyTdGraphicsContainer.COG_To_COV_Offset = 0.0

            GmResidentClient.MyTdGraphicsContainer.HostVehiclePercentFromBottom = DEFAULT_VEH_VERT_POSITION_PCT

            GmResidentClient.MyTdGraphicsContainer.FullSizeViewAreaAcross = DEFAULT_WIDTH_METERS
            GmResidentClient.MyTdGraphicsContainer.FullSizeViewAreaForeAft = DEFAULT_HEIGHT_METERS

            GmResidentClient.MyTdGraphicsContainer.GridSpacingAcross = 20
            GmResidentClient.MyTdGraphicsContainer.GridSpacingForeAft = 20

            GmResidentClient.MyTdGraphicsContainer.ViewWindowSizeWidth = 400
            GmResidentClient.MyTdGraphicsContainer.ViewWindowSizeHeight = 400

            myPictureBoxGreenLeftArrow = New PictureBox() With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Location = New Point(10, 20),
                .Size = New Size(75, 25),
                .SizeMode = PictureBoxSizeMode.StretchImage,
                .Image = My.Resources.Resources.GreenLeftArrow(),
                .Visible = False
            }
            Me.Controls.Add(myPictureBoxGreenLeftArrow)

            myPictureBoxGreenRightArrow = New PictureBox With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Size = New Size(75, 25)
            }
            myPictureBoxGreenRightArrow.Location = New Point(((myPictureBoxGreenRightArrow.Parent.Width - myPictureBoxGreenRightArrow.Width) - 30), 20)
            myPictureBoxGreenRightArrow.SizeMode = PictureBoxSizeMode.StretchImage
            myPictureBoxGreenRightArrow.Image = My.Resources.Resources.GreenRightArrow()
            myPictureBoxGreenRightArrow.Visible = False
            Me.Controls.Add(myPictureBoxGreenRightArrow)

            myPictureBoxGrayLeftArrow = New PictureBox With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Location = New Point(10, 20),
                .Size = New Size(75, 25),
                .SizeMode = PictureBoxSizeMode.StretchImage,
                .Image = My.Resources.Resources.GrayLeftArrow(),
                .Visible = False
            }
            Me.Controls.Add(myPictureBoxGrayLeftArrow)

            myPictureBoxGrayRightArrow = New PictureBox With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Size = New Size(75, 25)
            }
            myPictureBoxGrayRightArrow.Location = New Point(((myPictureBoxGrayRightArrow.Parent.Width - myPictureBoxGrayRightArrow.Width) - 30), 20)
            myPictureBoxGrayRightArrow.SizeMode = PictureBoxSizeMode.StretchImage
            myPictureBoxGrayRightArrow.Image = My.Resources.Resources.GrayRightArrow()
            myPictureBoxGrayRightArrow.Visible = False
            Me.Controls.Add(myPictureBoxGrayRightArrow)

            myPictureBoxYellowLeftArrow = New PictureBox With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Location = New Point(10, 20),
                .Size = New Size(75, 25),
                .SizeMode = PictureBoxSizeMode.StretchImage,
                .Image = My.Resources.Resources.YellowLeftArrow(),
                .Visible = False
            }
            Me.Controls.Add(myPictureBoxYellowLeftArrow)

            myPictureBoxYellowRightArrow = New PictureBox With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Size = New Size(75, 25)
            }
            myPictureBoxYellowRightArrow.Location = New Point(((myPictureBoxYellowRightArrow.Parent.Width - myPictureBoxYellowRightArrow.Width) - 30), 20)
            myPictureBoxYellowRightArrow.SizeMode = PictureBoxSizeMode.StretchImage
            myPictureBoxYellowRightArrow.Image = My.Resources.Resources.YellowRightArrow()
            myPictureBoxYellowRightArrow.Visible = False
            Me.Controls.Add(myPictureBoxYellowRightArrow)

            myPictureBoxRedLeftArrow = New PictureBox With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Location = New Point(10, 20),
                .Size = New Size(75, 25),
                .SizeMode = PictureBoxSizeMode.StretchImage,
                .Image = My.Resources.Resources.RedLeftArrow(),
                .Visible = False
            }
            Me.Controls.Add(myPictureBoxRedLeftArrow)

            myPictureBoxRedRightArrow = New PictureBox With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Size = New Size(75, 25)
            }
            myPictureBoxRedRightArrow.Location = New Point(((myPictureBoxRedRightArrow.Parent.Width - myPictureBoxRedRightArrow.Width) - 30), 20)
            myPictureBoxRedRightArrow.SizeMode = PictureBoxSizeMode.StretchImage
            myPictureBoxRedRightArrow.Image = My.Resources.Resources.RedRightArrow()
            myPictureBoxRedRightArrow.Visible = False
            Me.Controls.Add(myPictureBoxRedRightArrow)

            myPictureBoxBlueRightArrow = New PictureBox With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Location = New Point(10, 20),
                .Size = New Size(75, 25),
                .SizeMode = PictureBoxSizeMode.StretchImage,
                .Image = My.Resources.Resources.BlueRightArrow(),
                .Visible = False
            }
            Me.Controls.Add(myPictureBoxBlueRightArrow)

            myPictureBoxBlueLeftArrow = New PictureBox With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .BorderStyle = BorderStyle.None,
                .BackColor = Color.Transparent,
                .Size = New Size(75, 25)
            }
            myPictureBoxBlueLeftArrow.Location = New Point(((myPictureBoxBlueLeftArrow.Parent.Width - myPictureBoxBlueLeftArrow.Width) - 30), 20)
            myPictureBoxBlueLeftArrow.SizeMode = PictureBoxSizeMode.StretchImage
            myPictureBoxBlueLeftArrow.Image = My.Resources.Resources.BlueLeftArrow()
            myPictureBoxBlueLeftArrow.Visible = False
            Me.Controls.Add(myPictureBoxBlueLeftArrow)

            myALCReasonLeftLabel = New Label With {
                .BackColor = Color.Transparent,
                .BorderStyle = BorderStyle.FixedSingle,
                .Location = New Point(10, 50),
                .Name = "ALCReasonLeftLabel",
                .Size = New Size(120, 30)
            }
            myALCReasonLeftLabel.BorderStyle = BorderStyle.FixedSingle
            myALCReasonLeftLabel.Text = "NONE"
            myALCReasonLeftLabel.TextAlign = ContentAlignment.MiddleCenter
            myALCReasonLeftLabel.Parent = GmResidentClient.MyTdGraphicsContainer
            myALCReasonLeftLabel.Font = New Font(myALCReasonLeftLabel.Font.FontFamily, 10)
            myALCReasonLeftLabel.Font = New Font(myALCReasonLeftLabel.Font, FontStyle.Bold)
            myALCReasonLeftLabel.ForeColor = Color.Black
            myALCReasonLeftLabel.Visible = False
            myALCReasonLeftLabel.Enabled = True
            Me.Controls.Add(myALCReasonLeftLabel)

            myALCReasonRightLabel = New Label With {
                .BackColor = Color.Transparent,
                .BorderStyle = BorderStyle.FixedSingle,
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .Size = New Size(120, 30)
            }
            myALCReasonRightLabel.Location = New Point(((myALCReasonRightLabel.Parent.Width - myALCReasonRightLabel.Width) - 30), 50)
            myALCReasonRightLabel.Name = "ALCReasonRightLabel"
            myALCReasonRightLabel.BorderStyle = BorderStyle.FixedSingle
            myALCReasonRightLabel.Text = "NONE"
            myALCReasonRightLabel.TextAlign = ContentAlignment.MiddleCenter
            myALCReasonRightLabel.Font = New Font(myALCReasonRightLabel.Font.FontFamily, 10)
            myALCReasonRightLabel.Font = New Font(myALCReasonRightLabel.Font, FontStyle.Bold)
            myALCReasonRightLabel.ForeColor = Color.Black
            myALCReasonRightLabel.Visible = False
            myALCReasonRightLabel.Enabled = True
            Me.Controls.Add(myALCReasonRightLabel)

            myALCStatusLeftLabel = New Label With {
                .BackColor = Color.Transparent,
                .BorderStyle = BorderStyle.FixedSingle,
                .Location = New Point(10, 90),
                .Name = "ALCStatusLeftLabel",
                .Size = New Size(120, 30)
            }
            myALCStatusLeftLabel.BorderStyle = BorderStyle.FixedSingle
            myALCStatusLeftLabel.Text = "IN-ACTIVE"
            myALCStatusLeftLabel.TextAlign = ContentAlignment.MiddleCenter
            myALCStatusLeftLabel.Parent = GmResidentClient.MyTdGraphicsContainer
            myALCStatusLeftLabel.Font = New Font(myALCStatusLeftLabel.Font.FontFamily, 10)
            myALCStatusLeftLabel.Font = New Font(myALCStatusLeftLabel.Font, FontStyle.Bold)
            myALCStatusLeftLabel.ForeColor = Color.Black
            myALCStatusLeftLabel.Visible = False
            myALCStatusLeftLabel.Enabled = True
            Me.Controls.Add(myALCStatusLeftLabel)

            myALCStatusRightLabel = New Label With {
                .BackColor = Color.Transparent,
                .BorderStyle = BorderStyle.FixedSingle,
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .Size = New Size(120, 30)
            }
            myALCStatusRightLabel.Location = New Point(((myALCStatusRightLabel.Parent.Width - myALCStatusRightLabel.Width) - 30), 90)
            myALCStatusRightLabel.Name = "ALCStatusRightLabel"
            myALCStatusRightLabel.BorderStyle = BorderStyle.FixedSingle
            myALCStatusRightLabel.Text = "IN-ACTIVE"
            myALCStatusRightLabel.TextAlign = ContentAlignment.MiddleCenter
            myALCStatusRightLabel.Font = New Font(myALCStatusRightLabel.Font.FontFamily, 10)
            myALCStatusRightLabel.Font = New Font(myALCStatusRightLabel.Font, FontStyle.Bold)
            myALCStatusRightLabel.ForeColor = Color.Black
            myALCStatusRightLabel.Visible = False
            myALCStatusRightLabel.Enabled = True
            Me.Controls.Add(myALCStatusRightLabel)

            mylabel1 = New Label With {
                .BackColor = Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(0, Byte), Integer)),
                .BorderStyle = BorderStyle.Fixed3D,
                .Location = New Point(0, 0),
                .Name = "Label1",
                .Size = New Size(200, 23) 'was 130, 23
                }
            mylabel1.BorderStyle = BorderStyle.Fixed3D

            mylabel1.TabIndex = 0
            mylabel1.Text = ""
            mylabel1.TextAlign = ContentAlignment.TopCenter
            mylabel1.Visible = False
            mylabel1.Parent = GmResidentClient.MyTdGraphicsContainer

            myExitButton = New Button With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .Visible = True,
                .Text = "EXIT"
            }

            myExitButton.Font = New Font(myExitButton.Font.FontFamily, GmResidentClient.MenuFontSize)
            myExitButton.Font = New Font(myExitButton.Font, FontStyle.Bold)

            AddHandler myExitButton.Click, AddressOf myExitButton_Click

            myExitButton.Width = 80
            myExitButton.Height = 50
            myExitButton.Left = (myExitButton.Parent.Width - myExitButton.Width) - 20
            myExitButton.Top = myExitButton.Parent.Height - myExitButton.Height - 40

            myExitButton.BringToFront()
            Me.Controls.Add(myExitButton)

            myTDViewConfigButton = New Button With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .Visible = True,
                .Text = "TOP DOWN VIEW CONFIG"
            }

            myTDViewConfigButton.Font = New Font(myTDViewConfigButton.Font.FontFamily, 10)
            myTDViewConfigButton.Font = New Font(myTDViewConfigButton.Font, FontStyle.Bold)

            AddHandler myTDViewConfigButton.Click, AddressOf myTDViewConfigButton_Click

            myTDViewConfigButton.Width = 120
            myTDViewConfigButton.Height = 50
            myTDViewConfigButton.Left = (myExitButton.Left - myTDViewConfigButton.Width) - 5
            myTDViewConfigButton.Top = myExitButton.Parent.Height - myExitButton.Height - 40

            myTDViewConfigButton.BringToFront()
            Me.Controls.Add(myTDViewConfigButton)
            myFullScreenButton = New Button With {
                .Parent = GmResidentClient.MyTdGraphicsContainer,
                .Visible = True,
                .Text = "FULL SCREEN",
                .Font = New Font(myTDViewConfigButton.Font.FontFamily, 10)
            }
            myFullScreenButton.Font = New Font(myTDViewConfigButton.Font, FontStyle.Bold)

            AddHandler myFullScreenButton.Click, AddressOf myFullScreenButton_Click

            myFullScreenButton.Width = 90
            myFullScreenButton.Height = 50
            myFullScreenButton.Left = (myTDViewConfigButton.Left - myFullScreenButton.Width) - 5
            myFullScreenButton.Top = myTDViewConfigButton.Top

            myFullScreenButton.BringToFront()
            Me.Controls.Add(myFullScreenButton)

            SetupTopDownObjects()

            ' Set the initial positions
            FormDisplayed = True

            ' Force initial layout
            TDGraphicsContainer_Resize(Me, EventArgs.Empty)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "SetupTopDownView: " & ex.Message, DisplayMsgBox)
        End Try

    End Sub

    ' Event handler for the Full Screen button
    Private Sub myFullScreenButton_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        Static SaveTop As Integer
        Static SaveLeft As Integer
        Static SaveHeight As Integer
        Static SaveWidth As Integer

        If myFullScreenButton.Text = "FULL SCREEN" Then

            SaveTop = GmResidentClient.MyTdGraphicsContainer.Top
            SaveLeft = GmResidentClient.MyTdGraphicsContainer.Left
            SaveWidth = GmResidentClient.MyTdGraphicsContainer.Width
            SaveHeight = GmResidentClient.MyTdGraphicsContainer.Height

            GmResidentClient.MyTdGraphicsContainer.Top = 0
            GmResidentClient.MyTdGraphicsContainer.Left = 0

            GmResidentClient.MyTdGraphicsContainer.Width = My.Computer.Screen.WorkingArea.Size.Width
            GmResidentClient.MyTdGraphicsContainer.Height = My.Computer.Screen.WorkingArea.Size.Height

            myFullScreenButton.Text = "NORMAL VIEW"

        Else

            GmResidentClient.MyTdGraphicsContainer.Top = SaveTop
            GmResidentClient.MyTdGraphicsContainer.Left = SaveLeft

            GmResidentClient.MyTdGraphicsContainer.Width = SaveWidth
            GmResidentClient.MyTdGraphicsContainer.Height = SaveHeight


            myFullScreenButton.Text = "FULL SCREEN"

        End If

    End Sub

    ' Event handler for the Config button
    Private Sub myTDViewConfigButton_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        TopDownViewConfiguration.Top = GmResidentClient.MyTdGraphicsContainer.Top + 30
        TopDownViewConfiguration.Left = GmResidentClient.MyTdGraphicsContainer.Left

        TopDownViewConfiguration.TopMost = True

        TopDownViewConfiguration.Show()

        TopDownViewConfiguration.BringToFront()
        TopDownViewConfiguration.Activate()

    End Sub

    ' Event handler for the Exit button
    Private Sub myExitButton_Click(ByVal sender As Object, ByVal e As EventArgs)

        'Dim thisButton As Button = DirectCast(sender, Button)

        Offset_Y = 0
        Offset_X = 0
        m_ZoomFactor_X = 1
        m_ZoomFactor_Y = 1

        TopDownViewConfiguration.Close()
        GmResidentClient.MyTdGraphicsContainer.Close()

    End Sub

    Private Property FullSizeViewAreaAcross() As Integer
        Get
            Return _FullSizeViewAreaAcross
        End Get
        Set(ByVal value As Integer)
            _FullSizeViewAreaAcross = value
        End Set
    End Property

    Private Property FullSizeViewAreaForeAft() As Integer
        Get
            Return _FullSizeViewAreaForeAft
        End Get
        Set(ByVal value As Integer)
            _FullSizeViewAreaForeAft = value
        End Set
    End Property

    Property GridSpacingAcross() As Integer
        Get
            Return _GridSpacingAcross
        End Get
        Set(ByVal value As Integer)
            _GridSpacingAcross = value
        End Set
    End Property

    Property GridSpacingForeAft() As Integer
        Get
            Return _GridSpacingForeAft
        End Get
        Set(ByVal value As Integer)
            _GridSpacingForeAft = value
        End Set
    End Property

    Private Property ViewWindowSizeWidth() As Integer
        Get
            Return _ViewWindowSizeWidth
        End Get
        Set(ByVal value As Integer)
            _ViewWindowSizeWidth = value
            Width = _ViewWindowSizeWidth + 17
        End Set
    End Property

    Property ViewWindowSizeHeight() As Integer
        Get
            Return _ViewWindowSizeHeight
        End Get
        Private Set(ByVal value As Integer)
            _ViewWindowSizeHeight = value
            Height = _ViewWindowSizeHeight + 39
        End Set
    End Property

    Property HostVehiclePercentFromBottom() As Integer
        Get
            Return _HostVehiclePercentFromBottom
        End Get
        Set(ByVal value As Integer)
            _HostVehiclePercentFromBottom = value
        End Set
    End Property

    Property HostVehicleDimForwardOfCG() As Single
        Get
            Return _HostVehicleDimForwardOfCG
        End Get
        Set(ByVal value As Single)
            _HostVehicleDimForwardOfCG = value
        End Set
    End Property

    Property HostVehicleDimRearwardOfCG() As Single
        Get
            Return _HostVehicleDimRearwardOfCG
        End Get
        Set(ByVal value As Single)
            _HostVehicleDimRearwardOfCG = value
        End Set
    End Property

    Property COG_To_COV_Offset() As Double

        Get
            Return _COG_To_COV_Offset
        End Get
        Set(ByVal value As Double)
            _COG_To_COV_Offset = value
        End Set

    End Property

    Property HostVehicleDimLongitudinal() As Single
        Get
            Return _HostVehicleDimLongitudinal
        End Get
        Set(ByVal value As Single)
            _HostVehicleDimLongitudinal = value
        End Set
    End Property

    Property HostVehicleDimLateral() As Single
        Get
            Return _HostVehicleDimLateral
        End Get
        Set(ByVal value As Single)
            _HostVehicleDimLateral = value
        End Set
    End Property

    Private Sub InitializeComponent()
        SuspendLayout()
        '
        'TDGraphicsContainerClass
        '
        BackColor = Color.White
        ClientSize = New Size(284, 262)
        DoubleBuffered = True
        Name = "TDGraphicsContainerClass"
        ShowInTaskbar = False
        StartPosition = FormStartPosition.Manual
        ResumeLayout(False)

    End Sub

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As EventArgs)

    End Sub

    Private Sub TDGraphicsContainerClass_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Click

    End Sub

    Private Sub TDGraphicsContainerClass_Disposed(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Disposed

    End Sub

    Private Sub TDGraphicsContainerClass_DoubleClick(ByVal sender As Object, ByVal e As EventArgs) Handles Me.DoubleClick

    End Sub

    Private Sub TDGraphicsContainer_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles Me.FormClosing

        If TopMost = False Then
            Hide()
            e.Cancel = True
        Else
            e.Cancel = True
        End If

        TopDownViewConfiguration.WriteTopDownViewConfigFile()

        TopDownViewConfiguration.Close()

    End Sub

    Private Sub TDGraphicsContainerClass_HandleCreated(ByVal sender As Object, ByVal e As EventArgs) Handles Me.HandleCreated

    End Sub

    Private Sub TDGraphicsContainer_Load(ByVal sender As System.Object, ByVal e As EventArgs) Handles MyBase.Load
        BackColor = Color.White

        SetStyle(ControlStyles.UserPaint, True)
        SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        SetStyle(ControlStyles.OptimizedDoubleBuffer, True)

        TopDownViewConfiguration.ReadTopDownConfigFile()

    End Sub

    Private Sub TDGraphicsContainerClass_MouseDoubleClick(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Me.MouseDoubleClick


        If e.Button = MouseButtons.Left Then

            HandleZoom(-1)

        Else

            HandleZoom(1)

        End If

    End Sub

    Private Sub TDGraphicsContainerClass_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Me.MouseDown

        'StopZooming = False

        If e.Button = MouseButtons.Left Then

            MouseRefPos_Y = e.Y
            MouseRefPos_X = e.X

        End If

        Exit Sub

        Do While StopZooming = False

            If Offset_X > 0 Or Offset_Y > 0 Then
                Exit Do
            End If

            If e.Button = MouseButtons.Left Then

                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomXToolStripMenuItem.Checked Then
                    If m_ZoomFactor_X <= 10.0 Then
                        m_ZoomFactor_X = m_ZoomFactor_X + 0.5
                    Else
                        Exit Do
                    End If
                End If

                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomYToolStripMenuItem.Checked Then

                    If m_ZoomFactor_Y <= 10.0 Then
                        m_ZoomFactor_Y = m_ZoomFactor_Y + 0.5
                    Else
                        Exit Do
                    End If
                End If

            ElseIf e.Button = MouseButtons.Right Then

                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomXToolStripMenuItem.Checked Then
                    If m_ZoomFactor_X > 1.0 Then
                        m_ZoomFactor_X = m_ZoomFactor_X - 0.5
                    Else
                        Exit Do
                    End If

                End If
                If TopDownViewConfiguration.ZoomXAndYToolStripMenuItem.Checked = True Or TopDownViewConfiguration.ZoomYToolStripMenuItem.Checked Then
                    If m_ZoomFactor_Y > 1.0 Then
                        m_ZoomFactor_Y = m_ZoomFactor_Y - 0.5
                    Else
                        Exit Do
                    End If

                End If

            End If

            'System.Windows.Forms.Application.DoEvents() 'DOEVENTS
            Threading.Thread.Sleep(100)

            GmResidentClient.MyTdGraphicsContainer.Invalidate()

        Loop

        StopZooming = False

        '.invalidate causes a repaint of the top down graphics container
        GmResidentClient.MyTdGraphicsContainer.Invalidate()

    End Sub

    Private Sub TDGraphicsContainer_MouseEnter(ByVal sender As Object, ByVal e As EventArgs) Handles Me.MouseEnter

        mylabel1.Visible = True
        mylabel1.BringToFront()

        If Not myMovinglabel Is Nothing Then
            myMovinglabel.Visible = False
            myMovinglabel = Nothing
        End If

        'myPictureBoxGreenLeftArrow.BringToFront()
        'myPictureBoxGrayLeftArrow.SendToBack()

    End Sub

    Private Sub TDGraphicsContainer_MouseLeave(ByVal sender As Object, ByVal e As EventArgs) Handles Me.MouseLeave

        mylabel1.Visible = False

        'myPictureBoxGreenLeftArrow.SendToBack()
        'myPictureBoxGrayLeftArrow.BringToFront()

    End Sub

    Private Sub TDGraphicsContainer_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Me.MouseMove

        Dim scale_x As String
        Dim scale_y As String

        Dim x As Integer
        Dim y As Integer

        Dim TD_Object As TD_TargetObjectsClass

        Dim tempstr As String
        Dim saveAddText As String

        Dim CursorInRange As Boolean

        If TopDownViewConfiguration.Visible = True Then
            Exit Sub
        End If


        If e.Button = MouseButtons.Left Then

            Offset_Y = e.Y - MouseRefPos_Y
            Offset_X = e.X - MouseRefPos_X

            Invalidate()

            Exit Sub
        End If

        saveAddText = ""

        scale_x = Format(-((e.X - VehicleObject.X_Pos(0)) *FullSizeViewAreaAcross /ViewWindowSizeWidth) / m_ZoomFactor_X, "0.00")
        scale_y = Format(-((e.Y - VehicleObject.Y_Pos(0)) *FullSizeViewAreaForeAft /ViewWindowSizeHeight) / m_ZoomFactor_Y, "0.00")

        tempstr = "left=" & scale_x & ", fwd=" & scale_y & " X = " & e.X & " Y = " & e.Y

        If Len(mylabel1.Text) = 0 Then
            mylabel1.Text = tempstr
        End If

        TD_Object = Nothing

        For x = 0 To 4
            Select Case x
                Case 0
                    TD_Object = FusionTargetObject
                Case 1
                    TD_Object = LRR_Object
                Case 2
                    TD_Object = LSRR_Object
                Case 3
                    TD_Object = RSRR_Object
                Case 4
                    TD_Object = VIS_Object
            End Select

            For y = 1 To TD_Object.NumObjects

                If TD_Object.X_Pos(y) <> 0 And TD_Object.Y_Pos(y) <> 0 Then

                    If e.X >= TD_Object.X_Pos(y) And e.X <= (TD_Object.X_Pos(y) + (TD_Object.MinSizePixels * m_ZoomFactor_X)) And
        e.Y >= TD_Object.Y_Pos(y) And e.Y <= (TD_Object.Y_Pos(y) + (TD_Object.MinSizePixels * m_ZoomFactor_Y)) Then

                        CursorInRange = True
                        saveAddText = vbCrLf & TD_Object.TargetObjectID & " ID = " & TD_Object.GetObjectID(y)
                        mylabel1.Size = New Size(200, 35)

                        If myMovinglabel Is Nothing Then
                            myMovinglabel = New Label With {
                                .Parent = Me,
                                .Left = e.X + 5,
                                .Top = e.Y + 5,
                                .Width = 90,
                                .Height = 15,
                                .BackColor = Color.Yellow,
                                .BorderStyle = BorderStyle.Fixed3D,
                                .Text = TD_Object.TargetObjectID & " ID = " & TD_Object.GetObjectID(y),
                                .Visible = True
                            }
                        Else
                            myMovinglabel.Text = TD_Object.TargetObjectID & " ID = " & TD_Object.GetObjectID(y)
                        End If

                        Exit For

                    Else
                        If Not myMovinglabel Is Nothing Then
                            myMovinglabel.Visible = False
                            myMovinglabel = Nothing

                        End If
                    End If
                End If

            Next

            If Not myMovinglabel Is Nothing Then
                If myMovinglabel.Visible = True Then
                    Exit For
                End If
            End If

            If CursorInRange = True Then
                Exit For
            End If
        Next

        If CursorInRange = False Then
            mylabel1.Text = tempstr
            mylabel1.Size = New Size(200, 23)
        Else
            mylabel1.Text = tempstr & saveAddText
            mylabel1.Size = New Size(200, 35)
        End If

        Refresh()

    End Sub

    Private Sub TDGraphicsContainerClass_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Me.MouseUp

        'StopZooming = True

        If TopDownViewConfiguration.Visible = True Then
            Exit Sub
        End If

        Offset_Y = e.Y - MouseRefPos_Y
        Offset_X = e.X - MouseRefPos_X

        Invalidate()

    End Sub

    Private Sub TDGraphicsContainer_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles Me.Paint

        'This routine is called whenever we need to repaint the top down view.  It is called when we
        'use the Invalidate method for this form

        'Dim hdc As IntPtr = e.Graphics.GetHdc()
        'e.Graphics.ReleaseHdc(hdc)

        If FormDisplayed = False Then
            Exit Sub
        End If

        HandlePaintEvent(e, VehicleObject)

        If MyIncaInterface.MeasurementStarted = True Or
           RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackRun Or
           RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepBack Or
           RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackStepFwd Or
           RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackScrolling Or
           RecordPlayback.PlaybackMode = RecordPlayback.PlaybackStates.PlaybackPause Then

            HandlePaintEvent(e, FusionTargetObject)
            HandlePaintEvent(e, LRR_Object)
            HandlePaintEvent(e, LSRR_Object)
            HandlePaintEvent(e, RSRR_Object)
            HandlePaintEvent(e, VIS_Object)

        End If

    End Sub

    ' Resize event handler
    Private Sub TDGraphicsContainer_Resize(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Resize
        If Not FormDisplayed Then Return

        ' Adjust the Exit button position
        If SaveExitButtonLeft = 0 Then
            SaveExitButtonLeft = myExitButton.Left
            SaveExitButtonTop = myExitButton.Top
        Else
            myExitButton.Left = CInt(SaveExitButtonLeft + (Width - 420))
            myExitButton.Top = CInt(SaveExitButtonTop + (Height - 440))
        End If

        ' Adjust the Config button position
        If SaveToggleButtonLeft = 0 Then
            SaveToggleButtonLeft = myTDViewConfigButton.Left
            SaveToggleButtonTop = myTDViewConfigButton.Top
        Else
            If myExitButton IsNot Nothing AndAlso myExitButton.Visible Then
                myTDViewConfigButton.Left = myExitButton.Left - myTDViewConfigButton.Width - 5
            Else
                myTDViewConfigButton.Left = myExitButton.Left - (myTDViewConfigButton.Width - myExitButton.Width)
            End If
            myTDViewConfigButton.Top = CInt(SaveToggleButtonTop + (Height - 440))
        End If

        ' Adjust the Full Screen button position
        If SaveFullScreenButtonLeft = 0 Then
            SaveFullScreenButtonLeft = myFullScreenButton.Left
            SaveFullScreenButtonTop = myFullScreenButton.Top
        Else
            myFullScreenButton.Left = myTDViewConfigButton.Left - myFullScreenButton.Width - 5
            myFullScreenButton.Top = CInt(SaveFullScreenButtonTop + (Height - 440))
        End If

        ' Update view window size
        ViewWindowSizeWidth = Width - 17
        ViewWindowSizeHeight = Height - 39

        Try
            ' Adjust PictureBox and Label positions
            ' -- Green Right Arrow
            If myPictureBoxGreenRightArrow IsNot Nothing Then
                myPictureBoxGreenRightArrow.Location = New Point(Me.ClientSize.Width - myPictureBoxGreenRightArrow.Width - 30, 20)
            End If

            ' -- Gray Right Arrow
            If myPictureBoxGrayRightArrow IsNot Nothing Then
                myPictureBoxGrayRightArrow.Location = New Point(Me.ClientSize.Width - myPictureBoxGrayRightArrow.Width - 30, 20)
            End If

            ' -- Yellow Right Arrow
            If myPictureBoxYellowRightArrow IsNot Nothing Then
                myPictureBoxYellowRightArrow.Location = New Point(Me.ClientSize.Width - myPictureBoxYellowRightArrow.Width - 30, 20)
            End If

            ' -- Red Right Arrow
            If myPictureBoxRedRightArrow IsNot Nothing Then
                myPictureBoxRedRightArrow.Location = New Point(Me.ClientSize.Width - myPictureBoxRedRightArrow.Width - 30, 20)
            End If

            ' -- Blue Left Arrow
            If myPictureBoxBlueLeftArrow IsNot Nothing Then
                myPictureBoxBlueLeftArrow.Location = New Point(Me.ClientSize.Width - myPictureBoxBlueLeftArrow.Width - 30, 20)
            End If

            ' Adjust Labels
            ' -- ALC Reason Right Label
            If myALCReasonRightLabel IsNot Nothing Then
                myALCReasonRightLabel.Location = New Point(Me.ClientSize.Width - myALCReasonRightLabel.Width - 30, 50)
            End If

            ' -- ALC Status Right Label
            If myALCStatusRightLabel IsNot Nothing Then
                myALCStatusRightLabel.Location = New Point(Me.ClientSize.Width - myALCStatusRightLabel.Width - 30, 90)
            End If

        Catch ex As Exception
            MessageBox.Show($"Error adjusting controls: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

        ' Invalidate the form to trigger a repaint
        Invalidate()
    End Sub

    ' Additional methods and event handlers can be added here
    ' ...

    Private Sub Label1_Click_1(ByVal sender As System.Object, ByVal e As EventArgs)

    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Private Sub TDGraphicsContainerClass_Move(sender As Object, e As EventArgs) Handles Me.Move

        If TopDownViewConfiguration.Visible = True Then
            TopDownViewConfiguration.Top = Top + 30
            TopDownViewConfiguration.Left = Left
            TopDownViewConfiguration.BringToFront()
            TopDownViewConfiguration.Activate()
        End If


    End Sub

    Private Sub TDGraphicsContainerClass_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp


    End Sub

    Private Sub TDGraphicsContainerClass_Activated(sender As Object, e As EventArgs) Handles Me.Activated

        If TopDownViewConfiguration.Visible = True Then
            TopDownViewConfiguration.Top = Top + 30
            TopDownViewConfiguration.Left = Left
            TopDownViewConfiguration.BringToFront()
            TopDownViewConfiguration.Activate()
        End If

    End Sub
End Class
