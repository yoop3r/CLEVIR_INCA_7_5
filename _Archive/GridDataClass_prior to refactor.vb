
Option Strict Off

Imports System.ComponentModel
Imports System.Threading.Tasks

<TypeConverter(GetType(ExpandableObjectConverter))>
Public Class GridDataClass

    'When grids are dynamically created from the INCAVariableFile excel spreadsheet, they are created
    'using the GridDataClass.  This class is a custom class built off of the base class\
    'DataGridView.  We add a bunch of stuff to the base class to allow us
    'to use the grids created, as we wish, based on our application requirements.

    'While most of the code is shared between the CLEVIR_INCA_7_2 and CLEVIR_INCA_7_3 versions, there are different
    'GridDataClass modules used for the two CLEVIR versions.  This is due to the fact that the handling of configured
    'displays must be different between 7.2 and 7.3, and much of the code related to the configured displays is in this
    'module .So, whenever any changes are made to GridDataClass, the same changes must be made to both the 7.2 version
    'which is located in FlexGridFiles and the 7.3 version which is located in in NoFlexGridFiles...

    Inherits DataGridView

    Private GridResizeActive As Boolean
    Private CellResizeActive As Boolean

    'Public myMommoArrayDataView As MSHFlexGridReplace.Data.ArrayDataView
    'Public myMommoArrayPropoertyDesriptor As MSHFlexGridReplace.Data.ArrayPropertyDescriptor
    'Public myMommoArrayRowView As MSHFlexGridReplace.Data.ArrayRowView

    Friend WithEvents GridHeader As Label
    Private _SaveFormattedString(,) As String
    Public DataArray(,) As String
    Private ColumnNames() As String

    Public Const MaxNumRowsPerGrid As Integer = 200
    Public Const MaxNumColsPerGrid As Integer = 40
    Public Const MAX_GRIDS_PER_FORM As Integer = 8
    Private Const GRID_COL_WIDTH_SIZING_MULTIPLIER As Integer = 1
    Public Const GRID_WIDTH_SIZING_MULTIPLIER As Double = 1
    Private Const GRID_HEIGHT_SIZING_MULTIPLIER As Double = 1 ' was 0.15 'was .16

    'Public Const DEFAULT_COL_ONE_WIDTH As Integer = 1650
    Private Const DEFAULT_COL_ONE_WIDTH As Integer = 100
    Public Const DEFAULT_COL_WIDTH As Integer = 45
    'Public Const DEFAULT_ROW_HEIGHT As Integer = 20
    Public Const DEFAULT_ROW_HEIGHT As Integer = 15
    Public Const DEFAULT_SEPARATION As Integer = 15

    Public myContextMenuStrip As ContextMenuStrip
    Private mycurrentrow As Integer
    Private currentcol As Integer
    Public myToolTip As ToolTip
    'Public Shared myDGs As New List(Of GridDataClass)
    Private MouseEntry_X As Integer
    Private MouseEntry_Y As Integer

    Private GridMouseEntry_X As Integer
    Private GridMouseEntry_Y As Integer

    Private SaveGridHeight As Integer
    Private SaveGridWidth As Integer

    Private SaveColWidthZero As Integer

    Private SaveCurrentRow As Integer
    Private SaveCurrentCol As Integer

    Private _Registered(,) As Boolean

    Private _CheckForDataChange(,) As Boolean
    Private _SaveLastValue(,) As Double
    Private _DataFrozen(,) As Boolean
    Private _DataFrozenCounter(,) As Integer
    Private _VariableName(,) As String
    Private _DisplayName(,) As String
    Private _VariableIndex(,) As Integer
    Private _SignalIndex(,) As Integer
    Private _DeviceName(,) As String
    Private _Raster(,) As String
    Private _CurrentBackColor(,) As Color
    Private _CurrentForeColor(,) As Color
    Private _DefaultCellBackColor(,) As Color
    Private _DefaultCellForeColor(,) As Color
    Private _HighThreshBackColor(,) As Color
    Private _LowThreshBackColor(,) As Color
    Private _HighThreshForeColor(,) As Color
    Private _LowThreshForeColor(,) As Color
    Private _HighThresh(,) As Double
    Private _LowThresh(,) As Double
    Private _EqualTo(,) As String
    Private _DisplayFormat(,) As String
    Private _RelatedTDObjectID As String
    Private _TD_X_Start_Pos As Integer
    Private _TD_Y_Start_Pos As Integer
    Private _TD_RANGE_Start_Pos As Integer
    Private _TD_AZIMUTH_Start_Pos As Integer
    Private _ObjectID_Start_Pos As Integer
    Private _ObjectType_Start_Pos As Integer

    'Private _LXCR_BLUE_LINE_TD_COEF_0 As Integer
    'Private _LXCR_BLUE_LINE_TD_COEF_1 As Integer
    'Private _LXCR_BLUE_LINE_TD_COEF_2 As Integer
    'Private _LXCR_BLUE_LINE_TD_COEF_3 As Integer

    Private _LKAR_LATPOS_LA As Integer
    Private _LXCR_BP_YPOS_LA As Integer
    Private _LXCR_PATH_TD_LAT_POS_0 As Integer
    Private _LXCR_BLEND_PATH_TD_LAT_POS_0 As Integer

    'Private _LMFR_PATH_TD_LON_POS_0 As Integer
    'Private _LMFR_PATH_TD_LAT_POS_0 As Integer

    Private _LXCR_TGT_LANE_TD_COEF_0 As Integer
    Private _LXCR_TGT_LANE_TD_COEF_1 As Integer
    Private _LXCR_TGT_LANE_TD_COEF_2 As Integer
    Private _LXCR_TGT_LANE_TD_COEF_3 As Integer

    'Private _LXCR_PATH_DESIRED_TD_LON_POS_0 As Integer
    'Private _LXCR_PATH_DESIRED_TD_LAT_POS_0 As Integer

    Private _DANGER_ZONE_LEFT_X_TD_PT_0 As Integer
    Private _DANGER_ZONE_LEFT_Y_TD_PT_0 As Integer
    Private _DANGER_ZONE_RIGHT_X_TD_PT_0 As Integer
    Private _DANGER_ZONE_RIGHT_Y_TD_PT_0 As Integer

    Private _LXCR_BLEND_PATH_TD_COEF_0 As Integer
    Private _LXCR_BLEND_PATH_TD_COEF_1 As Integer
    Private _LXCR_BLEND_PATH_TD_COEF_2 As Integer
    Private _LXCR_BLEND_PATH_TD_COEF_3 As Integer
    Private _LXCR_BLEND_PATH_TD_COEF_4 As Integer
    Private _LXCR_BLEND_PATH_TD_COEF_5 As Integer

    Private _LXCR_LON_VEL As Integer

    Private _LXCR_BLEND_PATH_LENGTH As Integer
    Private _LXCR_TARGET_LANE_LENGTH As Integer
    Private _LMFR_LANE_LENGTH As Integer

    'Private _BLUE_LINE_TD_COEF_0 As Integer
    'Private _BLUE_LINE_TD_COEF_1 As Integer
    'Private _BLUE_LINE_TD_COEF_2 As Integer
    'Private _BLUE_LINE_TD_COEF_3 As Integer

    'Private _DESIRED_PATH_TD_LON_POS_0 As Integer
    'Private _DESIRED_PATH_TD_LAT_POS_0 As Integer

    Private _VIS_LANE_OFFSET_0 As Integer
    Private _VIS_LANE_HDNG_ANGLE_0 As Integer
    Private _VIS_LANE_CURVATURE_0 As Integer
    Private _VIS_LANE_CURVATURE_DRV_0 As Integer
    Private _VIS_LANE_QUALITY_0 As Integer

    Private _TD_VPATH_LONG_OFST As Integer
    Private _TD_VPATH_LAT_OFST As Integer
    Private _TD_VPATH_YAW_RATE As Integer
    Private _TD_VPATH_LAT_VEL As Integer
    Private _TD_VPATH_LON_VEL As Integer

    'CUSTOM SCREEN

    Private _CS_ALC_LANE_CHANGE_STATE As Integer
    Private _CS_ALC_LANE_CHANGE_DCSN_RSN As Integer

    Private _CS_LaneInvalid As Integer
    Private _CS_LaneWgtLt As Integer
    Private _CS_LaneWgtRt As Integer
    Private _CS_HPP_Wgt As Integer
    Private _CS_HostLaneProbMax As Integer

    Private _CS_AlertUncertainLnLines As Integer
    Private _CS_AlertExitLane As Integer
    Private _CS_AlertLaneEnding As Integer
    Private _CS_AlertMapUnavail As Integer
    Private _CS_AlertOther As Integer
    Private _CS_LnNarrowing As Integer
    Private _CS_LnWidening As Integer

    Private _CS_ObjectLeft As Integer
    Private _CS_ObjectRight As Integer

    Private _CS_K1C_COPR_SYSSTAT As Integer
    Private _CS_K2C_COPR_SYSSTAT As Integer

    Private _CS_PriAutoBrkSysDrInfcStat As Integer
    Private _CS__PriAutoBrkSysDrInfcStatRed As Integer
    Private _CS_SecAutoBrkSysDrInfcStat As Integer
    Private _CS__SecAutoBrkSysDrInfcStatRed As Integer
    Private _CS_CE_AutoStrgCmndStat As Integer
    Private _CS_HS_AutoStrgCmndStat As Integer
    Private _CS_AutoPropAxlTrqArbStat As Integer
    Private _CS_ESADSS_CntrlStat As Integer
    Private _CS_AATPCS_PropSysStat As Integer
    Private _CS_PriBrkSysStat As Integer
    Private _CS__PriBrkSysStatRed As Integer
    Private _CS_SecBrkSysStat As Integer
    Private _CS__SecBrkSysStatRed As Integer
    Private _CS_PriGdVolt As Integer
    Private _CS_SecGdVolt As Integer
    Private _CS_SysPwrMd As Integer
    Private _CS_PrplsnSysAtv As Integer
    Private _CS_CE_VehMdMngrSt As Integer
    Private _CS_CE_VehHlthMngrSt As Integer
    Private _CS_AutoBrkSysRdcPerDet As Integer
    Private _CS_ADIMCntrlFailed As Integer
    Private _CS_HS_VehMdMngrSt As Integer
    Private _CS_HS_VehHlthMngrSt As Integer
    Private _CS_RedVehMdMngrSt As Integer
    Private _CS_PriCoPCmdMsgStat As Integer
    Private _CS_RedAutoBrkSysRdcPerDet As Integer
    Private _CS_RedADIMCntrlFailed As Integer

    Private _CS_VeTSTR_e_HiThreatObjType As Integer
    Private _CS_VeTSTR_Cnt_HiThreatObjID As Integer
    Private _CS_VeTSTR_t_HiThreatTTC As Integer

    Private _CS_AtGradeAnchor As Integer
    Private _CS_AnchorSelect As Integer

    Private _CS_HostLaneIndexLeft As Integer
    Private _CS_HostLaneIndexRight As Integer
    Private _CS_MapHostLaneIndex As Integer
    Private _CS_TargetsHostLaneIndex As Integer
    Private _CS_DistToNextAtGradeXing As Integer
    Private _CS_DistToRoadClassTrans As Integer
    Private _CS_DistToNextTrfcCntrDev As Integer
    Private _CS_OnFreeway As Integer
    Private _CS_RoadClass_Crnt As Integer
    Private _CS_CmplxIntrsct_Prsnt As Integer
    Private _CS_CntrlPtLatOffsetHPP As Integer
    Private _CS_CntrlPtLatOffsetL As Integer
    Private _CS_CntrlPtLatOffsetR As Integer
    Private _CS_CntrlPtLatOffsetPrev As Integer

    Private _CS_IFC_HeadingWgt As Integer
    Private _CS_PrevCoefWgt As Integer
    Private _CS_MapWgt As Integer
    Private _CS_IMU_BlueLine As Integer
    Private _CS_AlertIUncertainLnLines As Integer

    Private _CS_LCC_CLUSTER_MSG As Integer
    Private _CS_LCC_BUTTON_PRESS As Integer
    Private _CS_HostLaneInx As Integer
    Private _CS_HCURVE As Integer
    Private _CS_VehPathEstCurv As Integer
    Private _CS_SpltMrgLaneNum As Integer
    Private _CS_IntersectSplitMerge As Integer
    Private _CS_DistToNextIntersect As Integer
    Private _CS_Curvature As Integer
    Private _CS_NumLanes As Integer
    Private _CS_NextNumLanes As Integer
    Private _CS__NumLanesTrans As Integer
    Private _CS_DistToNumLanesTrans As Integer
    Private _CS_RawLaneLeft As Integer
    Private _CS_RawLaneRight As Integer
    Private _CS_PathConf As Integer
    Private _CS_LCC_RedReq As Integer
    Private _CS_LnSplitRedReq As Integer
    Private _CS_LnWgtRedReq As Integer
    Private _CS_MapAvailRedReq As Integer
    Private _CS_PathConfRedReq As Integer
    Private _CS_TmpLnRedReq As Integer
    Private _CS_VPMConfRedReq As Integer

    Private _CS_LCC_FEATURE_STATUS As Integer

    'Add LKA Related CS Custom Signals....

    Private _CS_LKA_TRQRQACT As Integer
    Private _CS_LKA_TORQUE As Integer
    Private _CS_LKA_DRVRAPPLDTRQ As Integer
    Private _CS_LNSNS_DISTTOLLNEDGE As Integer
    Private _CS_LNSNS_DISTTORLNEDGE As Integer

    Private _CS_LKA_OFFIND As Integer
    Private _CS_LKA_STDBYIND As Integer
    Private _CS_LKA_ACTVIND As Integer

    Private _CS_FOAI_VAIR As Integer
    Private _CS_FOAI_AWIR As Integer
    'Private _CS_CPS_AUTOBRKREQ As Integer

    Private _CS_AUTOBRKREQ As Integer
    Private _CS_AUTOBRKREQTYPE As Integer
    Private _CS_CPSCBSC_CTRLACC As Integer

    Private _CS_COLPRSYSBRKPRFREQ As Integer

    Private _CS_PEDWARN As Integer
    Private _CS_BRAKING_FLAG As Integer
    Private _CS_ALERT_FLAG As Integer
    Private _CS_NOTIFICATION_FLAG As Integer

    Private _CS_ACC_GAPSETTING As Integer
    Private _CS_CPSTOS_TTC As Integer
    'Private _CS_CPSTOS_RANGE As Integer
    Private _CS_CPSTOS_X_POS As Integer
    Private _CS_CPSTOS_Y_POS As Integer
    'Private _CS_CPSTOS_VEL As Integer
    Private _CS_CPSTOS_X_VEL As Integer
    Private _CS_CPSTOS_Y_VEL As Integer

    Private _CS_FSRACC_ENGAGED As Integer
    Private _CS_FSRACC_BRAKE_ACTIVE As Integer
    Private _CS_FSRACC_ACCEL_ACTIVE As Integer
    Private _CS_ODOMETER As Integer
    Private _CS_LCC_CONTROL_ACTIVE As Integer
    Private _AUTOANNO As Integer
    Private _CS_GPS_LAT As Integer
    Private _CS_GPS_LON As Integer
    Private _CS_LaneClass_Crnt As Integer

    Private _CS_NAN_STATUS As Integer
    Private _CS_WAVRECORD As Integer

    Private _AlsoAssociatedWith(,) As String

    Private components As IContainer

    Private _isDoubleClickBusy As Boolean = False

    Friend WithEvents ContextMenuStrip1 As ContextMenuStrip
    Friend WithEvents ToolTip1 As ToolTip

    Public Property NumberOfRows As Integer

    Public Property NumberOfColumns As Integer

    Public Property ParentFormIndex As Integer
    Public Property ParentFormName As String

    Property CS_NAN_STATUS() As Integer
        Get
            Return _CS_NAN_STATUS

        End Get
        Private Set(ByVal value As Integer)
            _CS_NAN_STATUS = value
        End Set
    End Property

    Property CS_WAVRECORD() As Integer
        Get
            Return _CS_WAVRECORD

        End Get
        Private Set(ByVal value As Integer)
            _CS_WAVRECORD = value
        End Set
    End Property


    Property CS_LCC_CLUSTER_MSG() As Integer
        Get
            Return _CS_LCC_CLUSTER_MSG

        End Get
        Private Set(ByVal value As Integer)
            _CS_LCC_CLUSTER_MSG = value
        End Set
    End Property

    Property CS_LCC_BUTTON_PRESS() As Integer
        Get
            Return _CS_LCC_BUTTON_PRESS

        End Get
        Private Set(ByVal value As Integer)
            _CS_LCC_BUTTON_PRESS = value
        End Set
    End Property


    Property CS_ALC_LANE_CHANGE_STATE() As Integer
        Get
            Return _CS_ALC_LANE_CHANGE_STATE

        End Get
        Private Set(ByVal value As Integer)
            _CS_ALC_LANE_CHANGE_STATE = value
        End Set
    End Property

    'Private _CS_ALC_LANE_CHANGE_DCSN_RSN As Integer
    'Private _CS_ALC_LANE_CHANGE_STATE As Integer


    Property CS_ALC_LANE_CHANGE_DCSN_RSN() As Integer
        Get
            Return _CS_ALC_LANE_CHANGE_DCSN_RSN

        End Get
        Private Set(ByVal value As Integer)
            _CS_ALC_LANE_CHANGE_DCSN_RSN = value
        End Set
    End Property

    Property CS_LaneInvalid() As Integer
        Get
            Return _CS_LaneInvalid

        End Get
        Private Set(ByVal value As Integer)
            _CS_LaneInvalid = value
        End Set
    End Property

    Property CS_LaneWgtLt() As Integer
        Get
            Return _CS_LaneWgtLt

        End Get
        Private Set(ByVal value As Integer)
            _CS_LaneWgtLt = value
        End Set
    End Property

    Property CS_LaneWgtRt() As Integer
        Get
            Return _CS_LaneWgtRt

        End Get
        Private Set(ByVal value As Integer)
            _CS_LaneWgtRt = value
        End Set
    End Property

    Property CS_HPP_Wgt() As Integer
        Get
            Return _CS_HPP_Wgt

        End Get
        Private Set(ByVal value As Integer)
            _CS_HPP_Wgt = value
        End Set
    End Property

    Property CS_HostLaneProbMax() As Integer
        Get
            Return _CS_HostLaneProbMax

        End Get
        Private Set(ByVal value As Integer)
            _CS_HostLaneProbMax = value
        End Set
    End Property

    Property CS_AlertUncertainLnLines() As Integer
        Get
            Return _CS_AlertUncertainLnLines

        End Get
        Private Set(ByVal value As Integer)
            _CS_AlertUncertainLnLines = value
        End Set
    End Property
    Property CS_AlertExitLane() As Integer
        Get
            Return _CS_AlertExitLane

        End Get
        Private Set(ByVal value As Integer)
            _CS_AlertExitLane = value
        End Set
    End Property
    Property CS_AlertLaneEnding() As Integer
        Get
            Return _CS_AlertLaneEnding

        End Get
        Private Set(ByVal value As Integer)
            _CS_AlertLaneEnding = value
        End Set
    End Property
    Property CS_AlertMapUnavail() As Integer
        Get
            Return _CS_AlertMapUnavail

        End Get
        Private Set(ByVal value As Integer)
            _CS_AlertMapUnavail = value
        End Set
    End Property
    Property CS_AlertOther() As Integer
        Get
            Return _CS_AlertOther

        End Get
        Private Set(ByVal value As Integer)
            _CS_AlertOther = value
        End Set
    End Property
    Property CS_LnNarrowing() As Integer
        Get
            Return _CS_LnNarrowing

        End Get
        Private Set(ByVal value As Integer)
            _CS_LnNarrowing = value
        End Set
    End Property
    Property CS_LnWidening() As Integer
        Get
            Return _CS_LnWidening

        End Get
        Private Set(ByVal value As Integer)
            _CS_LnWidening = value
        End Set
    End Property

    Property CS_ObjectLeft() As Integer
        Get
            Return _CS_ObjectLeft

        End Get
        Private Set(ByVal value As Integer)
            _CS_ObjectLeft = value
        End Set
    End Property

    Property CS_ObjectRight() As Integer
        Get
            Return _CS_ObjectRight

        End Get
        Private Set(ByVal value As Integer)
            _CS_ObjectRight = value
        End Set
    End Property

    Property CS_K1C_COPR_SYSSTAT() As Integer
        Get
            Return _CS_K1C_COPR_SYSSTAT

        End Get
        Private Set(ByVal value As Integer)
            _CS_K1C_COPR_SYSSTAT = value
        End Set
    End Property

    Property CS_K2C_COPR_SYSSTAT() As Integer
        Get
            Return _CS_K2C_COPR_SYSSTAT

        End Get
        Private Set(ByVal value As Integer)
            _CS_K2C_COPR_SYSSTAT = value
        End Set
    End Property


    Property CS_PriAutoBrkSysDrInfcStat() As Integer
        Get
            Return _CS_PriAutoBrkSysDrInfcStat

        End Get
        Private Set(ByVal value As Integer)
            _CS_PriAutoBrkSysDrInfcStat = value
        End Set
    End Property
    Property CS__PriAutoBrkSysDrInfcStatRed() As Integer
        Get
            Return _CS__PriAutoBrkSysDrInfcStatRed

        End Get
        Private Set(ByVal value As Integer)
            _CS__PriAutoBrkSysDrInfcStatRed = value
        End Set
    End Property
    Property CS_SecAutoBrkSysDrInfcStat() As Integer
        Get
            Return _CS_SecAutoBrkSysDrInfcStat

        End Get
        Private Set(ByVal value As Integer)
            _CS_SecAutoBrkSysDrInfcStat = value
        End Set
    End Property
    Property CS__SecAutoBrkSysDrInfcStatRed() As Integer
        Get
            Return _CS__SecAutoBrkSysDrInfcStatRed

        End Get
        Private Set(ByVal value As Integer)
            _CS__SecAutoBrkSysDrInfcStatRed = value
        End Set
    End Property
    Property CS_CE_AutoStrgCmndStat() As Integer
        Get
            Return _CS_CE_AutoStrgCmndStat

        End Get
        Private Set(ByVal value As Integer)
            _CS_CE_AutoStrgCmndStat = value
        End Set
    End Property
    Property CS_HS_AutoStrgCmndStat() As Integer
        Get
            Return _CS_HS_AutoStrgCmndStat

        End Get
        Private Set(ByVal value As Integer)
            _CS_HS_AutoStrgCmndStat = value
        End Set
    End Property


    Property CS_AutoPropAxlTrqArbStat() As Integer
        Get
            Return _CS_AutoPropAxlTrqArbStat

        End Get
        Private Set(ByVal value As Integer)
            _CS_AutoPropAxlTrqArbStat = value
        End Set
    End Property
    Property CS_ESADSS_CntrlStat() As Integer
        Get
            Return _CS_ESADSS_CntrlStat

        End Get
        Set(ByVal value As Integer)
            _CS_ESADSS_CntrlStat = value
        End Set
    End Property
    Property CS_AATPCS_PropSysStat() As Integer
        Get
            Return _CS_AATPCS_PropSysStat

        End Get
        Private Set(ByVal value As Integer)
            _CS_AATPCS_PropSysStat = value
        End Set
    End Property
    Property CS_PriBrkSysStat() As Integer
        Get
            Return _CS_PriBrkSysStat

        End Get
        Private Set(ByVal value As Integer)
            _CS_PriBrkSysStat = value
        End Set
    End Property
    Property CS__PriBrkSysStatRed() As Integer
        Get
            Return _CS__PriBrkSysStatRed

        End Get
        Private Set(ByVal value As Integer)
            _CS__PriBrkSysStatRed = value
        End Set
    End Property


    Property CS_SecBrkSysStat() As Integer
        Get
            Return _CS_SecBrkSysStat

        End Get
        Private Set(ByVal value As Integer)
            _CS_SecBrkSysStat = value
        End Set
    End Property
    Property CS__SecBrkSysStatRed() As Integer
        Get
            Return _CS__SecBrkSysStatRed

        End Get
        Private Set(ByVal value As Integer)
            _CS__SecBrkSysStatRed = value
        End Set
    End Property
    Property CS_PriGdVolt() As Integer
        Get
            Return _CS_PriGdVolt

        End Get
        Private Set(ByVal value As Integer)
            _CS_PriGdVolt = value
        End Set
    End Property
    Property CS_SecGdVolt() As Integer
        Get
            Return _CS_SecGdVolt

        End Get
        Private Set(ByVal value As Integer)
            _CS_SecGdVolt = value
        End Set
    End Property
    Property CS_SysPwrMd() As Integer
        Get
            Return _CS_SysPwrMd

        End Get
        Private Set(ByVal value As Integer)
            _CS_SysPwrMd = value
        End Set
    End Property
    Property CS_PrplsnSysAtv() As Integer
        Get
            Return _CS_PrplsnSysAtv

        End Get
        Private Set(ByVal value As Integer)
            _CS_PrplsnSysAtv = value
        End Set
    End Property
    Property CS_CE_VehMdMngrSt() As Integer
        Get
            Return _CS_CE_VehMdMngrSt

        End Get
        Private Set(ByVal value As Integer)
            _CS_CE_VehMdMngrSt = value
        End Set
    End Property
    Property CS_CE_VehHlthMngrSt() As Integer
        Get
            Return _CS_CE_VehHlthMngrSt

        End Get
        Set(ByVal value As Integer)
            _CS_CE_VehHlthMngrSt = value
        End Set
    End Property

    Property CS_AutoBrkSysRdcPerDet() As Integer
        Get
            Return _CS_AutoBrkSysRdcPerDet

        End Get
        Private Set(ByVal value As Integer)
            _CS_AutoBrkSysRdcPerDet = value
        End Set
    End Property
    Property CS_ADIMCntrlFailed() As Integer
        Get
            Return _CS_ADIMCntrlFailed

        End Get
        Private Set(ByVal value As Integer)
            _CS_ADIMCntrlFailed = value
        End Set
    End Property
    Property CS_HS_VehMdMngrSt() As Integer
        Get
            Return _CS_HS_VehMdMngrSt

        End Get
        Set(ByVal value As Integer)
            _CS_HS_VehMdMngrSt = value
        End Set
    End Property

    Property CS_HS_VehHlthMngrSt() As Integer
        Get
            Return _CS_HS_VehHlthMngrSt

        End Get
        Set(ByVal value As Integer)
            _CS_HS_VehHlthMngrSt = value
        End Set
    End Property


    Property CS_RedVehMdMngrSt() As Integer
        Get
            Return _CS_RedVehMdMngrSt

        End Get
        Private Set(ByVal value As Integer)
            _CS_RedVehMdMngrSt = value
        End Set
    End Property
    Property CS_RedAutoBrkSysRdcPerDet() As Integer
        Get
            Return _CS_RedAutoBrkSysRdcPerDet

        End Get
        Private Set(ByVal value As Integer)
            _CS_RedAutoBrkSysRdcPerDet = value
        End Set
    End Property
    Property CS_PriCoPCmdMsgStat() As Integer
        Get
            Return _CS_PriCoPCmdMsgStat

        End Get
        Private Set(ByVal value As Integer)
            _CS_PriCoPCmdMsgStat = value
        End Set
    End Property
    Property CS_RedADIMCntrlFailed() As Integer
        Get
            Return _CS_RedADIMCntrlFailed

        End Get
        Private Set(ByVal value As Integer)
            _CS_RedADIMCntrlFailed = value
        End Set
    End Property

    Property CS_VeTSTR_e_HiThreatObjType() As Integer
        Get
            Return _CS_VeTSTR_e_HiThreatObjType

        End Get
        Private Set(ByVal value As Integer)
            _CS_VeTSTR_e_HiThreatObjType = value
        End Set
    End Property

    Property CS_VeTSTR_Cnt_HiThreatObjID() As Integer
        Get
            Return _CS_VeTSTR_Cnt_HiThreatObjID

        End Get
        Set(ByVal value As Integer)
            _CS_VeTSTR_Cnt_HiThreatObjID = value
        End Set
    End Property

    Property CS_VeTSTR_t_HiThreatTTC() As Integer
        Get
            Return _CS_VeTSTR_t_HiThreatTTC

        End Get
        Private Set(ByVal value As Integer)
            _CS_VeTSTR_t_HiThreatTTC = value
        End Set
    End Property

    Property CS_AtGradeAnchor() As Integer
        Get
            Return _CS_AtGradeAnchor

        End Get
        Private Set(ByVal value As Integer)
            _CS_AtGradeAnchor = value
        End Set
    End Property

    Property CS_AnchorSelect() As Integer
        Get
            Return _CS_AnchorSelect

        End Get
        Private Set(ByVal value As Integer)
            _CS_AnchorSelect = value
        End Set
    End Property

    Property CS_HostLaneIndexLeft() As Integer
        Get
            Return _CS_HostLaneIndexLeft

        End Get
        Private Set(ByVal value As Integer)
            _CS_HostLaneIndexLeft = value
        End Set
    End Property

    Property CS_HostLaneIndexRight() As Integer
        Get
            Return _CS_HostLaneIndexRight

        End Get
        Private Set(ByVal value As Integer)
            _CS_HostLaneIndexRight = value
        End Set
    End Property

    Property CS_MapHostLaneIndex() As Integer
        Get
            Return _CS_MapHostLaneIndex

        End Get
        Private Set(ByVal value As Integer)
            _CS_MapHostLaneIndex = value
        End Set
    End Property

    Property CS_TargetsHostLaneIndex() As Integer
        Get
            Return _CS_TargetsHostLaneIndex

        End Get
        Private Set(ByVal value As Integer)
            _CS_TargetsHostLaneIndex = value
        End Set
    End Property

    Property CS_DistToNextAtGradeXing() As Integer
        Get
            Return _CS_DistToNextAtGradeXing

        End Get
        Private Set(ByVal value As Integer)
            _CS_DistToNextAtGradeXing = value
        End Set
    End Property

    Property CS_DistToRoadClassTrans() As Integer
        Get
            Return _CS_DistToRoadClassTrans

        End Get
        Private Set(ByVal value As Integer)
            _CS_DistToRoadClassTrans = value
        End Set
    End Property

    Property CS_DistToNextTrfcCntrDev() As Integer
        Get
            Return _CS_DistToNextTrfcCntrDev

        End Get
        Private Set(ByVal value As Integer)
            _CS_DistToNextTrfcCntrDev = value
        End Set
    End Property

    Property CS_OnFreeway() As Integer
        Get
            Return _CS_OnFreeway

        End Get
        Private Set(ByVal value As Integer)
            _CS_OnFreeway = value
        End Set
    End Property

    Property CS_RoadClass_Crnt() As Integer
        Get
            Return _CS_RoadClass_Crnt

        End Get
        Private Set(ByVal value As Integer)
            _CS_RoadClass_Crnt = value
        End Set
    End Property

    Property CS_CntrlPtLatOffsetHPP() As Integer
        Get
            Return _CS_CntrlPtLatOffsetHPP

        End Get
        Private Set(ByVal value As Integer)
            _CS_CntrlPtLatOffsetHPP = value
        End Set
    End Property

    Property CS_CntrlPtLatOffsetL() As Integer
        Get
            Return _CS_CntrlPtLatOffsetL

        End Get
        Private Set(ByVal value As Integer)
            _CS_CntrlPtLatOffsetL = value
        End Set
    End Property

    Property CS_CntrlPtLatOffsetR() As Integer
        Get
            Return _CS_CntrlPtLatOffsetR

        End Get
        Private Set(ByVal value As Integer)
            _CS_CntrlPtLatOffsetR = value
        End Set
    End Property

    Property CS_CntrlPtLatOffsetPrev() As Integer
        Get
            Return _CS_CntrlPtLatOffsetPrev

        End Get
        Private Set(ByVal value As Integer)
            _CS_CntrlPtLatOffsetPrev = value
        End Set
    End Property

    'Private _CS_IFC_HeadingWgt
    'Private _CS_PrevCoefWgt
    'Private _CS_MapWgt
    'Private _CS_IMU_BlueLine
    '_CS_AlertIUncertainLnLines

    Property CS_AlertIUncertainLnLines() As Integer
        Get
            Return _CS_AlertIUncertainLnLines

        End Get
        Set(ByVal value As Integer)
            _CS_AlertIUncertainLnLines = value
        End Set
    End Property

    Property CS_IFC_HeadingWgt() As Integer
        Get
            Return _CS_IFC_HeadingWgt

        End Get
        Private Set(ByVal value As Integer)
            _CS_IFC_HeadingWgt = value
        End Set
    End Property

    Property CS_PrevCoefWgt() As Integer
        Get
            Return _CS_PrevCoefWgt

        End Get
        Private Set(ByVal value As Integer)
            _CS_PrevCoefWgt = value
        End Set
    End Property

    Property CS_MapWgt() As Integer
        Get
            Return _CS_MapWgt

        End Get
        Private Set(ByVal value As Integer)
            _CS_MapWgt = value
        End Set
    End Property

    Property CS_IMU_BlueLine() As Integer
        Get
            Return _CS_IMU_BlueLine

        End Get
        Private Set(ByVal value As Integer)
            _CS_IMU_BlueLine = value
        End Set
    End Property

    Property CS_CmplxIntrsct_Prsnt() As Integer
        Get
            Return _CS_CmplxIntrsct_Prsnt

        End Get
        Private Set(ByVal value As Integer)
            _CS_CmplxIntrsct_Prsnt = value
        End Set
    End Property

    Property CS_HostLaneInx() As Integer
        Get
            Return _CS_HostLaneInx

        End Get
        Private Set(ByVal value As Integer)
            _CS_HostLaneInx = value
        End Set
    End Property

    Property CS_HCURVE() As Integer
        Get
            Return _CS_HCURVE

        End Get
        Private Set(ByVal value As Integer)
            _CS_HCURVE = value
        End Set
    End Property

    Property CS_VehPathEstCurv() As Integer
        Get
            Return _CS_VehPathEstCurv

        End Get
        Private Set(ByVal value As Integer)
            _CS_VehPathEstCurv = value
        End Set
    End Property

    Property CS_SpltMrgLaneNum() As Integer
        Get
            Return _CS_SpltMrgLaneNum

        End Get
        Private Set(ByVal value As Integer)
            _CS_SpltMrgLaneNum = value
        End Set
    End Property

    Property CS_IntersectSplitMerge() As Integer
        Get
            Return _CS_IntersectSplitMerge

        End Get
        Private Set(ByVal value As Integer)
            _CS_IntersectSplitMerge = value
        End Set
    End Property

    Property CS_DistToNextIntersect() As Integer
        Get
            Return _CS_DistToNextIntersect

        End Get
        Private Set(ByVal value As Integer)
            _CS_DistToNextIntersect = value
        End Set
    End Property

    Property CS_Curvature() As Integer
        Get
            Return _CS_Curvature

        End Get
        Private Set(ByVal value As Integer)
            _CS_Curvature = value
        End Set
    End Property

    Property CS_NumLanes() As Integer
        Get
            Return _CS_NumLanes

        End Get
        Private Set(ByVal value As Integer)
            _CS_NumLanes = value
        End Set
    End Property

    Property CS_NextNumLanes() As Integer
        Get
            Return _CS_NextNumLanes

        End Get
        Private Set(ByVal value As Integer)
            _CS_NextNumLanes = value
        End Set
    End Property

    Property CS__NumLanesTrans() As Integer
        Get
            Return _CS__NumLanesTrans

        End Get
        Private Set(ByVal value As Integer)
            _CS__NumLanesTrans = value
        End Set
    End Property

    Property CS_DistToNumLanesTrans() As Integer
        Get
            Return _CS_DistToNumLanesTrans

        End Get
        Private Set(ByVal value As Integer)
            _CS_DistToNumLanesTrans = value
        End Set
    End Property

    Property CS_RawLaneLeft() As Integer
        Get
            Return _CS_RawLaneLeft

        End Get
        Private Set(ByVal value As Integer)
            _CS_RawLaneLeft = value
        End Set
    End Property

    Property CS_RawLaneRight() As Integer
        Get
            Return _CS_RawLaneRight

        End Get
        Private Set(ByVal value As Integer)
            _CS_RawLaneRight = value
        End Set
    End Property

    Property CS_PathConf() As Integer
        Get
            Return _CS_PathConf

        End Get
        Private Set(ByVal value As Integer)
            _CS_PathConf = value
        End Set
    End Property

    Property CS_LCC_RedReq() As Integer
        Get
            Return _CS_LCC_RedReq

        End Get
        Private Set(ByVal value As Integer)
            _CS_LCC_RedReq = value
        End Set
    End Property

    Property CS_LnSplitRedReq() As Integer
        Get
            Return _CS_LnSplitRedReq

        End Get
        Private Set(ByVal value As Integer)
            _CS_LnSplitRedReq = value
        End Set
    End Property

    Property CS_LnWgtRedReq() As Integer
        Get
            Return _CS_LnWgtRedReq

        End Get
        Private Set(ByVal value As Integer)
            _CS_LnWgtRedReq = value
        End Set
    End Property

    Property CS_MapAvailRedReq() As Integer
        Get
            Return _CS_MapAvailRedReq

        End Get
        Private Set(ByVal value As Integer)
            _CS_MapAvailRedReq = value
        End Set
    End Property

    Property CS_PathConfRedReq() As Integer
        Get
            Return _CS_PathConfRedReq

        End Get
        Private Set(ByVal value As Integer)
            _CS_PathConfRedReq = value
        End Set
    End Property

    Property CS_TmpLnRedReq() As Integer
        Get
            Return _CS_TmpLnRedReq

        End Get
        Private Set(ByVal value As Integer)
            _CS_TmpLnRedReq = value
        End Set
    End Property

    Property CS_VPMConfRedReq() As Integer
        Get
            Return _CS_VPMConfRedReq

        End Get
        Private Set(ByVal value As Integer)
            _CS_VPMConfRedReq = value
        End Set
    End Property

    Property CS_LCC_FEATURE_STATUS() As Integer
        Get
            Return _CS_LCC_FEATURE_STATUS

        End Get
        Set(ByVal value As Integer)
            _CS_LCC_FEATURE_STATUS = value
        End Set
    End Property


    Property CS_LKA_TRQRQACT() As Integer
        Get
            Return _CS_LKA_TRQRQACT
        End Get
        Private Set(ByVal value As Integer)
            _CS_LKA_TRQRQACT = value
        End Set
    End Property

    Property CS_LKA_TORQUE() As Integer
        Get
            Return _CS_LKA_TORQUE
        End Get
        Private Set(ByVal value As Integer)
            _CS_LKA_TORQUE = value
        End Set
    End Property

    Property CS_LKA_DRVRAPPLDTRQ() As Integer
        Get
            Return _CS_LKA_DRVRAPPLDTRQ
        End Get
        Private Set(ByVal value As Integer)
            _CS_LKA_DRVRAPPLDTRQ = value
        End Set
    End Property

    Property CS_LNSNS_DISTTOLLNEDGE() As Integer
        Get
            Return _CS_LNSNS_DISTTOLLNEDGE
        End Get
        Private Set(ByVal value As Integer)
            _CS_LNSNS_DISTTOLLNEDGE = value
        End Set
    End Property

    Property CS_LNSNS_DISTTORLNEDGE() As Integer
        Get
            Return _CS_LNSNS_DISTTORLNEDGE
        End Get
        Private Set(ByVal value As Integer)
            _CS_LNSNS_DISTTORLNEDGE = value
        End Set
    End Property

    Property CS_LKA_OFFIND() As Integer
        Get
            Return _CS_LKA_OFFIND
        End Get
        Private Set(ByVal value As Integer)
            _CS_LKA_OFFIND = value
        End Set
    End Property

    Property CS_LKA_STDBYIND() As Integer
        Get
            Return _CS_LKA_STDBYIND
        End Get
        Private Set(ByVal value As Integer)
            _CS_LKA_STDBYIND = value
        End Set
    End Property

    Property CS_LKA_ACTVIND() As Integer
        Get
            Return _CS_LKA_ACTVIND
        End Get
        Private Set(ByVal value As Integer)
            _CS_LKA_ACTVIND = value
        End Set
    End Property

    Property CS_FOAI_VAIR() As Integer
        Get
            Return _CS_FOAI_VAIR
        End Get
        Private Set(ByVal value As Integer)
            _CS_FOAI_VAIR = value
        End Set
    End Property

    Property CS_FOAI_AWIR() As Integer
        Get
            Return _CS_FOAI_AWIR
        End Get
        Private Set(ByVal value As Integer)
            _CS_FOAI_AWIR = value
        End Set
    End Property

    'Private _CS_PEDWARN As Integer

    Property CS_PEDWARN() As Integer
        Get
            Return _CS_PEDWARN
        End Get
        Private Set(ByVal value As Integer)
            _CS_PEDWARN = value
        End Set
    End Property

    Property CS_BRAKING_FLAG() As Integer
        Get
            Return _CS_BRAKING_FLAG
        End Get
        Private Set(ByVal value As Integer)
            _CS_BRAKING_FLAG = value
        End Set
    End Property

    Property CS_ALERT_FLAG() As Integer
        Get
            Return _CS_ALERT_FLAG
        End Get
        Private Set(ByVal value As Integer)
            _CS_ALERT_FLAG = value
        End Set
    End Property

    Property CS_NOTIFICATION_FLAG() As Integer
        Get
            Return _CS_NOTIFICATION_FLAG
        End Get
        Private Set(ByVal value As Integer)
            _CS_NOTIFICATION_FLAG = value
        End Set
    End Property


    Property CS_COLPRSYSBRKPRFREQ() As Integer
        Get
            Return _CS_COLPRSYSBRKPRFREQ
        End Get
        Private Set(ByVal value As Integer)
            _CS_COLPRSYSBRKPRFREQ = value
        End Set
    End Property

    Property CS_AUTOBRKREQ() As Integer
        Get
            Return _CS_AUTOBRKREQ
        End Get
        Private Set(ByVal value As Integer)
            _CS_AUTOBRKREQ = value
        End Set
    End Property

    Property CS_AUTOBRKREQTYPE() As Integer
        Get
            Return _CS_AUTOBRKREQTYPE
        End Get
        Private Set(ByVal value As Integer)
            _CS_AUTOBRKREQTYPE = value
        End Set
    End Property

    Property CS_CPSCBSC_CTRLACC() As Integer
        Get
            Return _CS_CPSCBSC_CTRLACC
        End Get
        Private Set(ByVal value As Integer)
            _CS_CPSCBSC_CTRLACC = value
        End Set
    End Property

    Property CS_ACC_GAPSETTING() As Integer
        Get
            Return _CS_ACC_GAPSETTING
        End Get
        Private Set(ByVal value As Integer)
            _CS_ACC_GAPSETTING = value
        End Set
    End Property

    Property CS_CPSTOS_TTC() As Integer
        Get
            Return _CS_CPSTOS_TTC
        End Get
        Private Set(ByVal value As Integer)
            _CS_CPSTOS_TTC = value
        End Set
    End Property

    Property LXCR_BLEND_PATH_LENGTH() As Integer
        Get
            Return _LXCR_BLEND_PATH_LENGTH
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_BLEND_PATH_LENGTH = value
        End Set
    End Property
    Property LXCR_TARGET_LANE_LENGTH() As Integer
        Get
            Return _LXCR_TARGET_LANE_LENGTH
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_TARGET_LANE_LENGTH = value
        End Set
    End Property
    Property LMFR_LANE_LENGTH() As Integer
        Get
            Return _LMFR_LANE_LENGTH
        End Get
        Private Set(ByVal value As Integer)
            _LMFR_LANE_LENGTH = value
        End Set
    End Property

    Property LXCR_LON_VEL() As Integer
        Get
            Return _LXCR_LON_VEL
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_LON_VEL = value
        End Set
    End Property

    Property CS_CPSTOS_X_POS() As Integer
        Get
            Return _CS_CPSTOS_X_POS
        End Get
        Private Set(ByVal value As Integer)
            _CS_CPSTOS_X_POS = value
        End Set
    End Property

    Property CS_CPSTOS_Y_POS() As Integer
        Get
            Return _CS_CPSTOS_Y_POS
        End Get
        Private Set(ByVal value As Integer)
            _CS_CPSTOS_Y_POS = value
        End Set
    End Property

    'Property CS_CPSTOS_VEL() As Integer
    '    Get
    '        Return _CS_CPSTOS_VEL
    '    End Get
    '    Set(ByVal value As Integer)
    '        _CS_CPSTOS_VEL = value
    '    End Set
    'End Property

    Property CS_CPSTOS_X_VEL() As Integer
        Get
            Return _CS_CPSTOS_X_VEL
        End Get
        Private Set(ByVal value As Integer)
            _CS_CPSTOS_X_VEL = value
        End Set
    End Property

    Property CS_CPSTOS_Y_VEL() As Integer
        Get
            Return _CS_CPSTOS_Y_VEL
        End Get
        Private Set(ByVal value As Integer)
            _CS_CPSTOS_Y_VEL = value
        End Set
    End Property

    Property CS_FSRACC_ENGAGED() As Integer
        Get
            Return _CS_FSRACC_ENGAGED
        End Get
        Private Set(ByVal value As Integer)
            _CS_FSRACC_ENGAGED = value
        End Set
    End Property

    Property CS_FSRACC_BRAKE_ACTIVE() As Integer
        Get
            Return _CS_FSRACC_BRAKE_ACTIVE
        End Get
        Private Set(ByVal value As Integer)
            _CS_FSRACC_BRAKE_ACTIVE = value
        End Set
    End Property

    Property CS_FSRACC_ACCEL_ACTIVE() As Integer
        Get
            Return _CS_FSRACC_ACCEL_ACTIVE
        End Get
        Private Set(ByVal value As Integer)
            _CS_FSRACC_ACCEL_ACTIVE = value
        End Set
    End Property

    Property CS_LaneClass_Crnt() As Integer
        Get
            Return _CS_LaneClass_Crnt
        End Get
        Private Set(ByVal value As Integer)
            _CS_LaneClass_Crnt = value
        End Set
    End Property

    Property CS_GPS_LAT() As Integer
        Get
            Return _CS_GPS_LAT
        End Get
        Private Set(ByVal value As Integer)
            _CS_GPS_LAT = value
        End Set
    End Property

    Property CS_GPS_LON() As Integer
        Get
            Return _CS_GPS_LON
        End Get
        Private Set(ByVal value As Integer)
            _CS_GPS_LON = value
        End Set
    End Property

    Property CS_ODOMETER() As Integer
        Get
            Return _CS_ODOMETER
        End Get
        Private Set(ByVal value As Integer)
            _CS_ODOMETER = value
        End Set
    End Property

    Property CS_LCC_CONTROL_ACTIVE() As Integer
        Get
            Return _CS_LCC_CONTROL_ACTIVE
        End Get
        Private Set(ByVal value As Integer)
            _CS_LCC_CONTROL_ACTIVE = value
        End Set
    End Property

    Property AUTOANNO() As Integer
        Get
            Return _AUTOANNO
        End Get
        Set(ByVal value As Integer)
            _AUTOANNO = value
        End Set
    End Property

    Property TD_VPATH_LON_VEL() As Integer
        Get
            Return _TD_VPATH_LON_VEL
        End Get
        Private Set(ByVal value As Integer)
            _TD_VPATH_LON_VEL = value
        End Set
    End Property

    Property TD_VPATH_LAT_VEL() As Integer
        Get
            Return _TD_VPATH_LAT_VEL
        End Get
        Private Set(ByVal value As Integer)
            _TD_VPATH_LAT_VEL = value
        End Set
    End Property
    Property TD_VPATH_YAW_RATE() As Integer
        Get
            Return _TD_VPATH_YAW_RATE
        End Get
        Private Set(ByVal value As Integer)
            _TD_VPATH_YAW_RATE = value
        End Set
    End Property

    Property TD_VPATH_LAT_OFST() As Integer
        Get
            Return _TD_VPATH_LAT_OFST
        End Get
        Private Set(ByVal value As Integer)
            _TD_VPATH_LAT_OFST = value
        End Set
    End Property
    Property TD_VPATH_LONG_OFST() As Integer
        Get
            Return _TD_VPATH_LONG_OFST
        End Get
        Private Set(ByVal value As Integer)
            _TD_VPATH_LONG_OFST = value
        End Set
    End Property
    Property ObjectID_Start_Pos() As Integer
        Get
            Return _ObjectID_Start_Pos
        End Get
        Private Set(ByVal value As Integer)
            _ObjectID_Start_Pos = value
        End Set
    End Property

    Property ObjectType_Start_Pos() As Integer
        Get
            Return _ObjectType_Start_Pos
        End Get
        Private Set(ByVal value As Integer)
            _ObjectType_Start_Pos = value
        End Set
    End Property

    'Property LXCR_BLUE_LINE_TD_COEF_0() As Integer
    ' Get
    ' Return _LXCR_BLUE_LINE_TD_COEF_0
    'End Get
    'Set(ByVal value As Integer)
    '        _LXCR_BLUE_LINE_TD_COEF_0 = value
    'End Set
    'End Property

    'Property LXCR_BLUE_LINE_TD_COEF_1() As Integer
    'Get
    'Return _LXCR_BLUE_LINE_TD_COEF_1
    'End Get
    'Set(ByVal value As Integer)
    '        _LXCR_BLUE_LINE_TD_COEF_1 = value
    'End Set
    'End Property

    'Property LXCR_BLUE_LINE_TD_COEF_2() As Integer
    'Get
    'Return _LXCR_BLUE_LINE_TD_COEF_2
    'End Get
    'Set(ByVal value As Integer)
    '       _LXCR_BLUE_LINE_TD_COEF_2 = value
    'End Set
    'End Property

    'Property LXCR_BLUE_LINE_TD_COEF_3() As Integer
    'Get
    'Return _LXCR_BLUE_LINE_TD_COEF_3
    'End Get
    'Set(ByVal value As Integer)
    '        _LXCR_BLUE_LINE_TD_COEF_3 = value
    'End Set
    'End Property

    'Property LMFR_PATH_TD_LON_POS_0() As Integer
    'Get
    'Return _LMFR_PATH_TD_LON_POS_0
    'End Get
    'Set(ByVal value As Integer)
    '        _LMFR_PATH_TD_LON_POS_0 = value
    'End Set
    'End Property

    Property LXCR_BLEND_PATH_TD_LAT_POS_0() As Integer
        Get
            Return _LXCR_BLEND_PATH_TD_LAT_POS_0
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_BLEND_PATH_TD_LAT_POS_0 = value
        End Set
    End Property

    Property LKAR_LATPOS_LA() As Integer
        Get
            Return _LKAR_LATPOS_LA
        End Get
        Private Set(ByVal value As Integer)
            _LKAR_LATPOS_LA = value
        End Set
    End Property

    Property LXCR_BP_YPOS_LA() As Integer
        Get
            Return _LXCR_BP_YPOS_LA
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_BP_YPOS_LA = value
        End Set
    End Property

    Property LXCR_PATH_TD_LAT_POS_0() As Integer
        Get
            Return _LXCR_PATH_TD_LAT_POS_0
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_PATH_TD_LAT_POS_0 = value
        End Set
    End Property

    Property LXCR_TGT_LANE_TD_COEF_0() As Integer
        Get
            Return _LXCR_TGT_LANE_TD_COEF_0
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_TGT_LANE_TD_COEF_0 = value
        End Set
    End Property

    Property LXCR_TGT_LANE_TD_COEF_1() As Integer
        Get
            Return _LXCR_TGT_LANE_TD_COEF_1
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_TGT_LANE_TD_COEF_1 = value
        End Set
    End Property

    Property LXCR_TGT_LANE_TD_COEF_2() As Integer
        Get
            Return _LXCR_TGT_LANE_TD_COEF_2
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_TGT_LANE_TD_COEF_2 = value
        End Set
    End Property

    Property LXCR_TGT_LANE_TD_COEF_3() As Integer
        Get
            Return _LXCR_TGT_LANE_TD_COEF_3
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_TGT_LANE_TD_COEF_3 = value
        End Set
    End Property

    'Property LXCR_PATH_DESIRED_TD_LON_POS_0() As Integer
    'Get
    'Return _LXCR_PATH_DESIRED_TD_LON_POS_0
    'End Get
    'Set(ByVal value As Integer)
    '        _LXCR_PATH_DESIRED_TD_LON_POS_0 = value
    'End Set
    'End Property

    'Property LXCR_PATH_DESIRED_TD_LAT_POS_0() As Integer
    'Get
    'Return _LXCR_PATH_DESIRED_TD_LAT_POS_0
    'End Get
    'Set(ByVal value As Integer)
    '        _LXCR_PATH_DESIRED_TD_LAT_POS_0 = value
    'End Set
    'End Property

    Property LXCR_BLEND_PATH_TD_COEF_0() As Integer
        Get
            Return _LXCR_BLEND_PATH_TD_COEF_0
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_BLEND_PATH_TD_COEF_0 = value
        End Set
    End Property

    Property LXCR_BLEND_PATH_TD_COEF_1() As Integer
        Get
            Return _LXCR_BLEND_PATH_TD_COEF_1
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_BLEND_PATH_TD_COEF_1 = value
        End Set
    End Property

    Property LXCR_BLEND_PATH_TD_COEF_2() As Integer
        Get
            Return _LXCR_BLEND_PATH_TD_COEF_2
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_BLEND_PATH_TD_COEF_2 = value
        End Set
    End Property

    Property LXCR_BLEND_PATH_TD_COEF_3() As Integer
        Get
            Return _LXCR_BLEND_PATH_TD_COEF_3
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_BLEND_PATH_TD_COEF_3 = value
        End Set
    End Property

    Property LXCR_BLEND_PATH_TD_COEF_4() As Integer
        Get
            Return _LXCR_BLEND_PATH_TD_COEF_4
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_BLEND_PATH_TD_COEF_4 = value
        End Set
    End Property

    Property LXCR_BLEND_PATH_TD_COEF_5() As Integer
        Get
            Return _LXCR_BLEND_PATH_TD_COEF_5
        End Get
        Private Set(ByVal value As Integer)
            _LXCR_BLEND_PATH_TD_COEF_5 = value
        End Set
    End Property

    Property DANGER_ZONE_LEFT_X_TD_PT_0() As Integer
        Get
            Return _DANGER_ZONE_LEFT_X_TD_PT_0
        End Get
        Private Set(ByVal value As Integer)
            _DANGER_ZONE_LEFT_X_TD_PT_0 = value
        End Set
    End Property

    Property DANGER_ZONE_LEFT_Y_TD_PT_0() As Integer
        Get
            Return _DANGER_ZONE_LEFT_Y_TD_PT_0
        End Get
        Private Set(ByVal value As Integer)
            _DANGER_ZONE_LEFT_Y_TD_PT_0 = value
        End Set
    End Property

    Property DANGER_ZONE_RIGHT_X_TD_PT_0() As Integer
        Get
            Return _DANGER_ZONE_RIGHT_X_TD_PT_0
        End Get
        Private Set(ByVal value As Integer)
            _DANGER_ZONE_RIGHT_X_TD_PT_0 = value
        End Set
    End Property

    Property DANGER_ZONE_RIGHT_Y_TD_PT_0() As Integer
        Get
            Return _DANGER_ZONE_RIGHT_Y_TD_PT_0
        End Get
        Private Set(ByVal value As Integer)
            _DANGER_ZONE_RIGHT_Y_TD_PT_0 = value
        End Set
    End Property

    'Property BLUE_LINE_TD_COEF_0() As Integer
    'Get
    'Return _BLUE_LINE_TD_COEF_0
    'End Get
    'Set(ByVal value As Integer)
    '        _BLUE_LINE_TD_COEF_0 = value
    'End Set
    'End Property

    'Property BLUE_LINE_TD_COEF_1() As Integer
    'Get
    'Return _BLUE_LINE_TD_COEF_1
    'End Get
    'Set(ByVal value As Integer)
    '        _BLUE_LINE_TD_COEF_1 = value
    'End Set
    'End Property
    'Property BLUE_LINE_TD_COEF_2() As Integer
    'Get
    'Return _BLUE_LINE_TD_COEF_2
    'End Get
    'Set(ByVal value As Integer)
    '        _BLUE_LINE_TD_COEF_2 = value
    'End Set
    'End Property

    'Property DESIRED_PATH_TD_LON_POS_0() As Integer
    'Get
    'Return _DESIRED_PATH_TD_LON_POS_0
    'End Get
    'Set(ByVal value As Integer)
    '       _DESIRED_PATH_TD_LON_POS_0 = value
    'End Set
    'End Property

    'Property DESIRED_PATH_TD_LAT_POS_0() As Integer
    'Get
    'Return _DESIRED_PATH_TD_LAT_POS_0
    'End Get
    'Set(ByVal value As Integer)
    '        _DESIRED_PATH_TD_LAT_POS_0 = value
    'End Set
    'End Property

    'VIS_LANE_OFFSET_0

    Property VIS_LANE_OFFSET_0() As Integer
        Get
            Return _VIS_LANE_OFFSET_0
        End Get
        Set(ByVal value As Integer)
            _VIS_LANE_OFFSET_0 = value
        End Set
    End Property

    'VIS_LANE_HDNG_ANGLE_0

    Property VIS_LANE_HDNG_ANGLE_0() As Integer
        Get
            Return _VIS_LANE_HDNG_ANGLE_0
        End Get
        Private Set(ByVal value As Integer)
            _VIS_LANE_HDNG_ANGLE_0 = value
        End Set
    End Property

    'VIS_LANE_CURVATURE_0

    Property VIS_LANE_CURVATURE_0() As Integer
        Get
            Return _VIS_LANE_CURVATURE_0
        End Get
        Private Set(ByVal value As Integer)
            _VIS_LANE_CURVATURE_0 = value
        End Set
    End Property

    'VIS_LANE_CURVATURE_DRV_0

    Property VIS_LANE_CURVATURE_DRV_0() As Integer
        Get
            Return _VIS_LANE_CURVATURE_DRV_0
        End Get
        Private Set(ByVal value As Integer)
            _VIS_LANE_CURVATURE_DRV_0 = value
        End Set
    End Property

    'VIS_LANE_QUALITY_0

    Property VIS_LANE_QUALITY_0() As Integer
        Get
            Return _VIS_LANE_QUALITY_0
        End Get
        Private Set(ByVal value As Integer)
            _VIS_LANE_QUALITY_0 = value
        End Set
    End Property

    'Property BLUE_LINE_TD_COEF_3() As Integer
    'Get
    'Return _BLUE_LINE_TD_COEF_3
    'End Get
    'Set(ByVal value As Integer)
    '        _BLUE_LINE_TD_COEF_3 = value
    'End Set
    'End Property

    Public Property GridSize As String

    Public Property LocationOnForm As String

    ''' <summary>
    ''' When True, all data columns are rendered extra-wide (e.g. for DTC or enumeration grids).
    ''' Replaces hard-coded form-name checks inside FinalizeGridDisplays.
    ''' </summary>
    Public Property UseWideColumns As Boolean = False

    ''' <summary>
    ''' Background colour used for the GridHeader label. Defaults to Blue.
    ''' Override to theme the header without touching FinalizeGridHeader.
    ''' </summary>
    Public Property GridHeaderBackColor As Color = Color.Blue

    ''' <summary>
    ''' Foreground colour used for the GridHeader label. Defaults to White.
    ''' </summary>
    Public Property GridHeaderForeColor As Color = Color.White

    Property AlsoAssociatedWith(ByVal row As Integer, ByVal col As Integer) As String
        Get
            Return _AlsoAssociatedWith(row, col)
        End Get
        Set(ByVal value As String)
            If _AlsoAssociatedWith IsNot Nothing Then
                If col >= UBound(_AlsoAssociatedWith, 2) Then
                    ReDim Preserve _AlsoAssociatedWith(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _AlsoAssociatedWith(MaxNumRowsPerGrid, col)
            End If

            _AlsoAssociatedWith(row, col) = value

        End Set
    End Property

    Property DisplayFormat(ByVal row As Integer, ByVal col As Integer) As String
        Get
            Return _DisplayFormat(row, col)
        End Get
        Set(ByVal value As String)
            If _DisplayFormat IsNot Nothing Then
                If col >= UBound(_DisplayFormat, 2) Then
                    ReDim Preserve _DisplayFormat(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _DisplayFormat(MaxNumRowsPerGrid, col)
            End If

            _DisplayFormat(row, col) = value

        End Set
    End Property

    Property Registered(ByVal row As Integer, ByVal col As Integer) As Boolean

        Get
            Return _Registered(row, col)
        End Get

        Set(ByVal value As Boolean)
            If _Registered IsNot Nothing Then
                If col >= UBound(_Registered, 2) Then
                    ReDim Preserve _Registered(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _Registered(MaxNumRowsPerGrid, col)
            End If


            _Registered(row, col) = value

        End Set

    End Property
    Property DataFrozen(ByVal row As Integer, ByVal col As Integer) As Boolean

        Get
            Return _DataFrozen(row, col)
        End Get

        Set(ByVal value As Boolean)
            If _DataFrozen IsNot Nothing Then
                If col >= UBound(_DataFrozen, 2) Then
                    ReDim Preserve _DataFrozen(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _DataFrozen(MaxNumRowsPerGrid, col)
            End If


            _DataFrozen(row, col) = value

        End Set

    End Property

    Property EqualTo(ByVal row As Integer, ByVal col As Integer) As String

        Get
            Return _EqualTo(row, col)
        End Get

        Set(ByVal value As String)
            If _EqualTo IsNot Nothing Then
                If col >= UBound(_EqualTo, 2) Then
                    ReDim Preserve _EqualTo(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _EqualTo(MaxNumRowsPerGrid, col)
            End If


            _EqualTo(row, col) = value

        End Set

    End Property
    Property LowThresh(ByVal row As Integer, ByVal col As Integer) As Double

        Get
            Return _LowThresh(row, col)
        End Get

        Set(ByVal value As Double)
            If _LowThresh IsNot Nothing Then
                If col >= UBound(_LowThresh, 2) Then
                    ReDim Preserve _LowThresh(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _LowThresh(MaxNumRowsPerGrid, col)
            End If


            _LowThresh(row, col) = value

        End Set

    End Property

    Property SaveLastValue(ByVal row As Integer, ByVal col As Integer) As Double

        Get
            Return _SaveLastValue(row, col)
        End Get

        Set(ByVal value As Double)
            If _SaveLastValue IsNot Nothing Then
                If col >= UBound(_SaveLastValue, 2) Then
                    ReDim Preserve _SaveLastValue(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _SaveLastValue(MaxNumRowsPerGrid, col)
            End If


            _SaveLastValue(row, col) = value

        End Set

    End Property

    Property SaveFormattedString(ByVal row As Integer, ByVal col As Integer) As String

        Get
            If _SaveFormattedString Is Nothing Then
                ReDim _SaveFormattedString(MaxNumRowsPerGrid, MaxNumColsPerGrid)
            ElseIf col > UBound(_SaveFormattedString, 2) Then
                ReDim Preserve _SaveFormattedString(MaxNumRowsPerGrid, col)
            End If
            Return _SaveFormattedString(row, col)
        End Get

        Set(ByVal value As String)
            If _SaveFormattedString Is Nothing Then
                ReDim _SaveFormattedString(MaxNumRowsPerGrid, MaxNumColsPerGrid)
            ElseIf col > UBound(_SaveFormattedString, 2) Then
                ReDim Preserve _SaveFormattedString(MaxNumRowsPerGrid, col)
            End If

            _SaveFormattedString(row, col) = value
        End Set

    End Property

    Property HighThresh(ByVal row As Integer, ByVal col As Integer) As Double

        Get
            Return _HighThresh(row, col)
        End Get

        Set(ByVal value As Double)
            If _HighThresh IsNot Nothing Then
                If col >= UBound(_HighThresh, 2) Then
                    ReDim Preserve _HighThresh(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _HighThresh(MaxNumRowsPerGrid, col)
            End If


            _HighThresh(row, col) = value

        End Set

    End Property
    Property LowThreshForeColor(ByVal row As Integer, ByVal col As Integer) As Color

        Get
            Return _LowThreshForeColor(row, col)
        End Get

        Set(ByVal value As Color)
            If _LowThreshForeColor IsNot Nothing Then
                If col >= UBound(_LowThreshForeColor, 2) Then
                    ReDim Preserve _LowThreshForeColor(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _LowThreshForeColor(MaxNumRowsPerGrid, col)
            End If


            _LowThreshForeColor(row, col) = value

        End Set

    End Property
    Property HighThreshForeColor(ByVal row As Integer, ByVal col As Integer) As Color

        Get
            Return _HighThreshForeColor(row, col)
        End Get

        Set(ByVal value As Color)
            If _HighThreshForeColor IsNot Nothing Then
                If col >= UBound(_HighThreshForeColor, 2) Then
                    ReDim Preserve _HighThreshForeColor(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _HighThreshForeColor(MaxNumRowsPerGrid, col)
            End If


            _HighThreshForeColor(row, col) = value

        End Set

    End Property
    Property LowThreshBackColor(ByVal row As Integer, ByVal col As Integer) As Color

        Get
            Return _LowThreshBackColor(row, col)
        End Get

        Set(ByVal value As Color)
            If _LowThreshBackColor IsNot Nothing Then
                If col >= UBound(_LowThreshBackColor, 2) Then
                    ReDim Preserve _LowThreshBackColor(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _LowThreshBackColor(MaxNumRowsPerGrid, col)
            End If


            _LowThreshBackColor(row, col) = value

        End Set

    End Property
    Property HighThreshBackColor(ByVal row As Integer, ByVal col As Integer) As Color

        Get
            Return _HighThreshBackColor(row, col)
        End Get

        Set(ByVal value As Color)
            If _HighThreshBackColor IsNot Nothing Then
                If col >= UBound(_HighThreshBackColor, 2) Then
                    ReDim Preserve _HighThreshBackColor(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _HighThreshBackColor(MaxNumRowsPerGrid, col)
            End If


            _HighThreshBackColor(row, col) = value

        End Set

    End Property
    Property DefaultCellForeColor(ByVal row As Integer, ByVal col As Integer) As Color

        Get
            Return _DefaultCellForeColor(row, col)
        End Get

        Set(ByVal value As Color)
            If _DefaultCellForeColor IsNot Nothing Then
                If col >= UBound(_DefaultCellForeColor, 2) Then
                    ReDim Preserve _DefaultCellForeColor(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _DefaultCellForeColor(MaxNumRowsPerGrid, col)
            End If


            _DefaultCellForeColor(row, col) = value

        End Set

    End Property
    Property DefaultCellBackColor(ByVal row As Integer, ByVal col As Integer) As Color

        Get
            Return _DefaultCellBackColor(row, col)
        End Get

        Set(ByVal value As Color)
            If _DefaultCellBackColor IsNot Nothing Then
                If col >= UBound(_DefaultCellBackColor, 2) Then
                    ReDim Preserve _DefaultCellBackColor(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _DefaultCellBackColor(MaxNumRowsPerGrid, col)
            End If


            _DefaultCellBackColor(row, col) = value

        End Set

    End Property
    Property CurrentBackColor(ByVal row As Integer, ByVal col As Integer) As Color

        Get
            Return _CurrentBackColor(row, col)
        End Get

        Set(ByVal value As Color)
            If _CurrentBackColor IsNot Nothing Then
                If col >= UBound(_CurrentBackColor, 2) Then
                    ReDim Preserve _CurrentBackColor(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _CurrentBackColor(MaxNumRowsPerGrid, col)
            End If


            _CurrentBackColor(row, col) = value

        End Set

    End Property

    Property CurrentForeColor(ByVal row As Integer, ByVal col As Integer) As Color

        Get
            Return _CurrentForeColor(row, col)
        End Get

        Set(ByVal value As Color)
            If _CurrentForeColor IsNot Nothing Then
                If col >= UBound(_CurrentForeColor, 2) Then
                    ReDim Preserve _CurrentForeColor(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _CurrentForeColor(MaxNumRowsPerGrid, col)
            End If


            _CurrentForeColor(row, col) = value

        End Set

    End Property

    Property Raster(ByVal row As Integer, ByVal col As Integer) As String

        Get
            Return _Raster(row, col)
        End Get

        Set(ByVal value As String)
            If _Raster IsNot Nothing Then
                If col >= UBound(_Raster, 2) Then
                    ReDim Preserve _Raster(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _Raster(MaxNumRowsPerGrid, col)
            End If


            _Raster(row, col) = value

        End Set

    End Property
    Property DeviceName(ByVal row As Integer, ByVal col As Integer) As String

        Get
            Return _DeviceName(row, col)
        End Get

        Set(ByVal value As String)
            If _DeviceName IsNot Nothing Then
                If col >= UBound(_DeviceName, 2) Then
                    ReDim Preserve _DeviceName(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _DeviceName(MaxNumRowsPerGrid, col)
            End If


            _DeviceName(row, col) = value

        End Set

    End Property

    Property VariableName(ByVal row As Integer, ByVal col As Integer) As String

        Get
            Return _VariableName(row, col)
        End Get

        Set(ByVal value As String)
            If _VariableName IsNot Nothing Then
                If col >= UBound(_VariableName, 2) Then
                    ReDim Preserve _VariableName(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _VariableName(MaxNumRowsPerGrid, col)
            End If


            _VariableName(row, col) = value

        End Set

    End Property

    Property DisplayName(ByVal row As Integer, ByVal col As Integer) As String

        Get
            Return _DisplayName(row, col)
        End Get

        Set(ByVal value As String)
            If _DisplayName IsNot Nothing Then
                If col >= UBound(_DisplayName, 2) Then
                    ReDim Preserve _DisplayName(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _DisplayName(MaxNumRowsPerGrid, col)
            End If


            _DisplayName(row, col) = value

        End Set

    End Property



    Property DataFrozenCounter(ByVal row As Integer, ByVal col As Integer) As Integer

        Get
            Return _DataFrozenCounter(row, col)
        End Get

        Set(ByVal value As Integer)
            If _DataFrozenCounter IsNot Nothing Then
                If col >= UBound(_DataFrozenCounter, 2) Then
                    ReDim Preserve _DataFrozenCounter(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _DataFrozenCounter(MaxNumRowsPerGrid, col)
            End If

            _DataFrozenCounter(row, col) = value

        End Set


    End Property

    Property VariableIndex(ByVal row As Integer, ByVal col As Integer) As Integer

        Get
            Return _VariableIndex(row, col)
        End Get

        Set(ByVal value As Integer)
            If _VariableIndex IsNot Nothing Then
                If col >= UBound(_VariableIndex, 2) Then
                    ReDim Preserve _VariableIndex(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _VariableIndex(MaxNumRowsPerGrid, col)
            End If

            _VariableIndex(row, col) = value

        End Set

    End Property

    Property SignalIndex(ByVal row As Integer, ByVal col As Integer) As Integer

        Get
            Return _SignalIndex(row, col)
        End Get

        Set(ByVal value As Integer)
            If _SignalIndex IsNot Nothing Then
                If col >= UBound(_SignalIndex, 2) Then
                    ReDim Preserve _SignalIndex(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _SignalIndex(MaxNumRowsPerGrid, col)
            End If

            _SignalIndex(row, col) = value

        End Set

    End Property

    Property CheckForDataChange(ByVal row As Integer, ByVal col As Integer) As Boolean

        Get
            Return _CheckForDataChange(row, col)
        End Get

        Set(ByVal value As Boolean)
            If _CheckForDataChange IsNot Nothing Then
                If col >= UBound(_CheckForDataChange, 2) Then
                    ReDim Preserve _CheckForDataChange(MaxNumRowsPerGrid, col)
                End If
            Else
                ReDim Preserve _CheckForDataChange(MaxNumRowsPerGrid, col)
            End If


            _CheckForDataChange(row, col) = value

        End Set

    End Property
    <CategoryAttribute("myCategory"),
              Browsable(True),
              [ReadOnly](False),
              BindableAttribute(False),
              DesignOnly(False),
              Description("Enter a number")>
    Public Property DefaultColumnOneWidth As Integer

    Public Property TD_X_Start_Pos() As Integer
        Get
            Return _TD_X_Start_Pos
        End Get
        Private Set(ByVal value As Integer)
            _TD_X_Start_Pos = value
        End Set
    End Property

    Public Property TD_Y_Start_Pos() As Integer
        Get
            Return _TD_Y_Start_Pos
        End Get
        Private Set(ByVal value As Integer)
            _TD_Y_Start_Pos = value
        End Set
    End Property

    Public Property TD_RANGE_Start_Pos() As Integer
        Get
            Return _TD_RANGE_Start_Pos
        End Get
        Private Set(ByVal value As Integer)
            _TD_RANGE_Start_Pos = value
        End Set
    End Property

    Public Property TD_AZIMUTH_Start_Pos() As Integer
        Get
            Return _TD_AZIMUTH_Start_Pos
        End Get
        Private Set(ByVal value As Integer)
            _TD_AZIMUTH_Start_Pos = value
        End Set
    End Property

    <CategoryAttribute("myCategory"),
          Browsable(True),
          [ReadOnly](False),
          BindableAttribute(True),
          DesignOnly(False),
          Description("Enter a number")>
    Private Property DefaultRowHeight As Integer

    <CategoryAttribute("myCategory"),
              Browsable(True),
              [ReadOnly](False),
              BindableAttribute(False),
              DesignOnly(False),
              Description("Enter a number")>
    Private Property DefaultColumnWidth As Integer

    Public Property RelatedTDObjectID() As String
        Get
            Return _RelatedTDObjectID
        End Get
        Private Set(ByVal value As String)
            _RelatedTDObjectID = value
        End Set
    End Property

    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.GridHeader = New System.Windows.Forms.Label()
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        CType(Me, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'GridHeader
        '
        Me.GridHeader.BackColor = System.Drawing.Color.Blue
        Me.GridHeader.ForeColor = System.Drawing.Color.White
        Me.GridHeader.Name = "GridHeader"
        Me.GridHeader.Size = New System.Drawing.Size(100, 23)
        Me.GridHeader.TabIndex = 0
        Me.GridHeader.Text = "Label1"
        '
        'ContextMenuStrip1
        '
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        Me.ContextMenuStrip1.Size = New System.Drawing.Size(61, 4)
        AddHandler Me.ContextMenuStrip1.Opening, AddressOf Me.ContextMenuStrip1_Opening
        '
        'ToolTip1
        '
        Me.ToolTip1.AutomaticDelay = 0
        Me.ToolTip1.UseFading = False
        '
        'GridDataClass
        '
        Me.RowTemplate.Height = 28
        Me.Size = New System.Drawing.Size(100, 50)
        CType(Me, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Shared Sub InitializeNewGrid(ByVal y As Integer, ByVal j As Integer, ByVal z As Integer)
        ' Called from CreateNewGrid. Creates a new, user-defined grid and sets the default properties.

        ' 1) Create a new GridDataClass object
        Dim newGrid As New GridDataClass() With {
                .Parent = GmResidentClient.MyDFs(j),
                .ParentFormIndex = j,
                .Name = NewGridCreation.GridTitle.Text,
                .RowHeadersVisible = False,
                .LocationOnForm = "X0 Y20",    ' Example location - adjust as needed
                .DefaultColumnOneWidth = DEFAULT_COL_ONE_WIDTH,
                .DefaultColumnWidth = DEFAULT_COL_WIDTH,
                .DefaultRowHeight = DEFAULT_ROW_HEIGHT,
                .GridHeader = New Label With {
                .Text = NewGridCreation.GridTitle.Text
                }
                }

        ' 2) If you're still using a 2D array or similar in AlsoAssociatedWith,
        '    set default values as before.
        '    (Adjust indexes or switch to a List-based approach, if desired.)
        newGrid.AlsoAssociatedWith(1, 1) = ""

        ' 3) Initialize the grid’s context menu if needed
        InitializeGridContextMenu(newGrid)

        ' 4) Add the newly created grid to the myDGs list
        myDGs.Add(newGrid)

        ' 5) (Optional) If you need to do anything else with the new grid...
        ' newGrid.NumberOfRows = 2
        ' newGrid.NumberOfColumns = 2
        ' etc...

    End Sub

    Private Shared Sub GridMenu_Click(ByVal sender As Object, ByVal e As EventArgs)
        ' Convert the sender to a ToolStripMenuItem to read its .Text property
        Dim menuItem As ToolStripMenuItem = TryCast(sender, ToolStripMenuItem)
        If menuItem Is Nothing Then Exit Sub

        ' The ContextMenuStrip that owns this menu item
        Dim contextMenu As ContextMenuStrip = TryCast(menuItem.Owner, ContextMenuStrip)
        If contextMenu Is Nothing Then Exit Sub

        ' Try to find the GridDataClass object from the context menu's SourceControl
        Dim gridIndex As Integer = -1
        Dim clickedGrid As Object = contextMenu.SourceControl

        ' === Option A: Identify the grid by SourceControl ===
        If clickedGrid IsNot Nothing Then
            ' Find the matching index in myDGs (List(Of GridDataClass))
            For i As Integer = 0 To myDGs.Count - 1
                If myDGs(i) IsNot Nothing AndAlso ReferenceEquals(myDGs(i), clickedGrid) Then
                    gridIndex = i
                    Exit For
                End If
            Next
        End If

        ' === Option B: Fallback to the legacy GridToModify (if still needed) ===
        If gridIndex < 0 AndAlso Not String.IsNullOrEmpty(GridToModify) Then
            For i As Integer = 0 To myDGs.Count - 1
                If myDGs(i) IsNot Nothing AndAlso myDGs(i).Name = GridToModify Then
                    gridIndex = i
                    Exit For
                End If
            Next
        End If

        ' If we still didn't find the grid, exit
        If gridIndex < 0 Then Exit Sub

        ' Now we have the correct grid index in 'gridIndex'
        Select Case menuItem.Text

            Case "Add Grid Row"
                myDGs(gridIndex).RowCount += 1

                Dim rowIndex As Integer = myDGs(gridIndex).RowCount - 1
                For col = 1 To myDGs(gridIndex).ColumnCount - 1
                    myDGs(gridIndex).VariableName(rowIndex, col) = "undefined"
                    myDGs(gridIndex).DisplayName(rowIndex, col) = "undefined"
                    myDGs(gridIndex).DeviceName(rowIndex, col) = "undefined"
                    myDGs(gridIndex).Raster(rowIndex, col) = "undefined"
                    myDGs(gridIndex).AlsoAssociatedWith(rowIndex, col) = ""
                    myDGs(gridIndex).DisplayFormat(rowIndex, col) = """0.000"""

                    myDGs(gridIndex).DefaultCellBackColor(rowIndex, col) = Color.White
                    myDGs(gridIndex).DefaultCellForeColor(rowIndex, col) = Color.Black
                    myDGs(gridIndex).HighThreshBackColor(rowIndex, col) = Color.Red
                    myDGs(gridIndex).LowThreshBackColor(rowIndex, col) = Color.Red
                    myDGs(gridIndex).HighThreshForeColor(rowIndex, col) = Color.White
                    myDGs(gridIndex).LowThreshForeColor(rowIndex, col) = Color.White
                    myDGs(gridIndex).HighThresh(rowIndex, col) = 10000000
                    myDGs(gridIndex).LowThresh(rowIndex, col) = -10000000
                    myDGs(gridIndex).EqualTo(rowIndex, col) = ""
                    myDGs(gridIndex).CheckForDataChange(rowIndex, col) = False
                Next

            Case "Add Grid Column"
                myDGs(gridIndex).ColumnCount += 1
                myDGs(gridIndex).CurrentCell = myDGs(gridIndex)(0, myDGs(gridIndex).ColumnCount)
                myDGs(gridIndex).Text = myDGs(gridIndex).ColumnCount

                Dim newColIndex As Integer = myDGs(gridIndex).ColumnCount
                For row = 1 To myDGs(gridIndex).RowCount - 1
                    myDGs(gridIndex).VariableName(row, newColIndex) = "undefined"
                    myDGs(gridIndex).DisplayName(row, newColIndex) = "undefined"
                    myDGs(gridIndex).DeviceName(row, newColIndex) = "undefined"
                    myDGs(gridIndex).Raster(row, newColIndex) = "undefined"
                    myDGs(gridIndex).AlsoAssociatedWith(row, newColIndex) = ""
                    myDGs(gridIndex).DisplayFormat(row, newColIndex) = """0.000"""

                    myDGs(gridIndex).DefaultCellBackColor(row, newColIndex) = Color.White
                    myDGs(gridIndex).DefaultCellForeColor(row, newColIndex) = Color.Black
                    myDGs(gridIndex).HighThreshBackColor(row, newColIndex) = Color.Red
                    myDGs(gridIndex).LowThreshBackColor(row, newColIndex) = Color.Red
                    myDGs(gridIndex).HighThreshForeColor(row, newColIndex) = Color.White
                    myDGs(gridIndex).LowThreshForeColor(row, newColIndex) = Color.White
                    myDGs(gridIndex).HighThresh(row, newColIndex) = 10000000
                    myDGs(gridIndex).LowThresh(row, newColIndex) = -10000000
                    myDGs(gridIndex).EqualTo(row, newColIndex) = ""
                    myDGs(gridIndex).CheckForDataChange(row, newColIndex) = False
                Next

            Case "Delete Bottom Grid Row"
                If myDGs(gridIndex).RowCount >= 3 Then
                    myDGs(gridIndex).RowCount -= 1
                Else
                    MsgBox("The grid must have at least 2 rows...")
                End If

            Case "Delete Right-most Grid Column"
                If myDGs(gridIndex).ColumnCount >= 3 Then
                    myDGs(gridIndex).ColumnCount -= 1
                Else
                    MsgBox("The grid must have at least 2 columns...")
                End If

            Case "Delete Grid"
                If MsgBox("Are you sure you want to delete the " & myDGs(gridIndex).Name & " Data Grid?", vbYesNo) = vbYes Then

                    ' Hide it first, if desired
                    myDGs(gridIndex).Visible = False
                    myDGs(gridIndex).GridHeader.Visible = False

                    ' Rather than shifting and ReDim, just remove it from the list
                    myDGs.RemoveAt(gridIndex)

                    GridCellPropConfig._changesMade = True
                End If

            Case "Change Width of Grid Columns 2 - n"
                Dim gridColWidth As Integer = myDGs(gridIndex).DefaultColumnWidth * 12
                Dim input As String = InputBox("Enter New Grid Column Width",, gridColWidth.ToString())
                If Integer.TryParse(input, gridColWidth) AndAlso gridColWidth > 0 Then
                    For col = 1 To myDGs(gridIndex).ColumnCount - 1
                        myDGs(gridIndex).Columns(col).Width = gridColWidth
                    Next
                End If

            Case "Change Height of Rows 1 - n"
                Dim gridRowHeight As Integer = myDGs(gridIndex).DefaultRowHeight
                Dim input As String = InputBox("Enter New Grid Row Height",, gridRowHeight.ToString())
                If Integer.TryParse(input, gridRowHeight) AndAlso gridRowHeight > 0 Then
                    For row = 1 To myDGs(gridIndex).RowCount - 1
                        myDGs(gridIndex).Rows(row).Height = gridRowHeight
                    Next
                    myDGs(gridIndex).RowTemplate.Height = myDGs(gridIndex).DefaultRowHeight
                End If

            Case "Change Grid Text Font Size"
                GmResidentClient.FontDialog1.ShowDialog()
                myDGs(gridIndex).Font = New Font(GmResidentClient.FontDialog1.Font.FontFamily.Name,
                                                 GmResidentClient.FontDialog1.Font.SizeInPoints)

                If GmResidentClient.FontDialog1.Font.Bold Then
                    myDGs(gridIndex).Font = New Font(myDGs(gridIndex).Font, FontStyle.Bold)
                Else
                    myDGs(gridIndex).Font = New Font(myDGs(gridIndex).Font, FontStyle.Regular)
                End If
        End Select

    End Sub


    Private Shared Sub InitializeGridContextMenu(ByVal MyObject As Object)
        ' Called from InitializeFlexGrids, CreateNewForm, and InitializeNewGrid.
        ' Creates context menus for GridDataClass-based objects (and could be adapted for forms).

        ' Initialize a new ContextMenuStrip for this object
        MyObject.myContextMenuStrip = New ContextMenuStrip()
        MyObject.myToolTip = New ToolTip()

        ' Build a top-level "Grid Handling" menu item
        Dim gridMenu As New ToolStripMenuItem("Grid Handling") With {
        .BackColor = Color.White,
        .ForeColor = Color.Black,
        .Text = "Grid Handling",
        .Font = New Font("Georgia", 10),
        .TextAlign = ContentAlignment.BottomRight
    }

        ' Build the sub-items using a List(Of ToolStripMenuItem)
        Dim menuItems As New List(Of ToolStripMenuItem)

        ' 1. "Add Grid Row"
        Dim addRowItem As New ToolStripMenuItem("Add Grid Row")
        AddHandler addRowItem.Click, AddressOf GridMenu_Click
        menuItems.Add(addRowItem)

        ' 2. "Add Grid Column"
        Dim addColumnItem As New ToolStripMenuItem("Add Grid Column")
        AddHandler addColumnItem.Click, AddressOf GridMenu_Click
        menuItems.Add(addColumnItem)

        ' 3. "Delete Bottom Grid Row"
        Dim deleteBottomRowItem As New ToolStripMenuItem("Delete Bottom Grid Row")
        AddHandler deleteBottomRowItem.Click, AddressOf GridMenu_Click
        menuItems.Add(deleteBottomRowItem)

        ' 4. "Delete Right-most Grid Column"
        Dim deleteRightmostColItem As New ToolStripMenuItem("Delete Right-most Grid Column")
        AddHandler deleteRightmostColItem.Click, AddressOf GridMenu_Click
        menuItems.Add(deleteRightmostColItem)

        ' 5. "Delete Grid"
        Dim deleteGridItem As New ToolStripMenuItem("Delete Grid")
        AddHandler deleteGridItem.Click, AddressOf GridMenu_Click
        menuItems.Add(deleteGridItem)

        ' 6. "Change Width of Grid Columns 2 - n"
        Dim changeColWidthItem As New ToolStripMenuItem("Change Width of Grid Columns 2 - n")
        AddHandler changeColWidthItem.Click, AddressOf GridMenu_Click
        menuItems.Add(changeColWidthItem)

        ' 7. "Change Height of Rows 2 - n"
        Dim changeRowHeightItem As New ToolStripMenuItem("Change Height of Rows 2 - n")
        AddHandler changeRowHeightItem.Click, AddressOf GridMenu_Click
        menuItems.Add(changeRowHeightItem)

        ' 8. "Change Grid Text Font Size"
        Dim changeFontSizeItem As New ToolStripMenuItem("Change Grid Text Font Size")
        AddHandler changeFontSizeItem.Click, AddressOf GridMenu_Click
        menuItems.Add(changeFontSizeItem)

        ' Add all sub-items in one go
        gridMenu.DropDownItems.AddRange(menuItems.ToArray())

        ' Finally, add the "Grid Handling" top-level item to the context menu
        MyObject.myContextMenuStrip.Items.Add(gridMenu)
    End Sub

    Shared Sub FinalizeGridDisplays()

        ' Called from ReadInSignalList.
        ' Sets the final grid display properties for each display grid once
        ' we know how many rows and columns are in each grid.

        Try
            HandleUserMessageLogging("GMRC", "FinalizeGridDisplays: Finalizing Displays...")

            For Each grid In myDGs

                If grid Is Nothing Then Continue For

                ' ── 1. Allocate arrays ──────────────────────────────────────────────────
                Dim rowBound As Integer = grid.NumberOfRows - 2
                Dim colBound As Integer = grid.NumberOfColumns - 1
                ReDim grid.DataArray(rowBound, colBound)
                ReDim grid.ColumnNames(colBound)

                ' ── 2. Populate DataArray and column names in a single row pass ─────────
                Dim isLCC As Boolean = grid.Parent.Name.StartsWith("LCC", StringComparison.OrdinalIgnoreCase)
                For x As Integer = 0 To rowBound
                    grid.DataArray(x, 0) = grid.DisplayName(x + 1, 1)
                    For y As Integer = 1 To colBound
                        grid.DataArray(x, y) = " "
                    Next
                Next

                grid.ColumnNames(0) = " "
                For y As Integer = 1 To colBound
                    grid.ColumnNames(y) = If(isLCC, $"Col{y}", CStr(y))
                Next

                ' ── 3. Data source and base style ────────────────────────────────────────
                grid.DataSource = New MSHFlexGridReplace.Data.ArrayDataView(grid.DataArray, grid.ColumnNames)
                grid.RowHeadersVisible = False
                grid.RowTemplate.Height = grid.DefaultRowHeight
                grid.DefaultCellStyle.BackColor = grid.DefaultCellBackColor(1, 1)
                grid.DefaultCellStyle.ForeColor = grid.DefaultCellForeColor(1, 1)
                grid.DefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.BackColor
                grid.DefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.ForeColor

                ' ── 4. Column widths — single pass measuring content with the actual font ─
                Dim dataCols As Integer = grid.ColumnCount - 1  ' excludes row-label col
                Dim baseWidth As Integer = DEFAULT_COL_WIDTH
                If dataCols > 15 Then
                    baseWidth = CInt(baseWidth * (15.0 / dataCols))
                ElseIf dataCols < 5 Then
                    baseWidth *= 2
                End If

                For y As Integer = 0 To grid.ColumnCount - 1
                    Dim colWidth As Integer = baseWidth
                    For x As Integer = 0 To rowBound
                        If y <= colBound Then
                            Dim cellText As String = grid.DataArray(x, y)
                            If Not String.IsNullOrEmpty(cellText) Then
                                Dim measured As Integer = TextRenderer.MeasureText(cellText, grid.Font).Width
                                colWidth = Math.Max(colWidth, measured)
                            End If
                        End If
                    Next

                    ' UseWideColumns: set on grids that need extra-wide data columns (e.g. DTC grids).
                    ' For 2-column grids, also widen ENUM-formatted cells.
                    If y > 0 Then
                        If grid.UseWideColumns Then
                            colWidth = Math.Max(colWidth, DEFAULT_COL_WIDTH * 5)
                        ElseIf grid.ColumnCount = 2 AndAlso grid.DisplayFormat(1, y) = "ENUM" Then
                            colWidth = Math.Max(colWidth, DEFAULT_COL_WIDTH * 4)
                        End If
                    End If

                    grid.Columns(y).Width = colWidth
                Next

                ' ── 5. Overall grid size ─────────────────────────────────────────────────
                ' Always derive size from content first; use any registered GridSize as a minimum.
                Dim contentWidth As Integer = 0
                For y As Integer = 0 To grid.ColumnCount - 1
                    contentWidth += grid.Columns(y).Width
                Next
                Dim contentHeight As Integer = grid.RowCount * grid.DefaultRowHeight

                Dim minW As Integer = 0
                Dim minH As Integer = 0
                If Not String.IsNullOrEmpty(grid.GridSize) Then
                    ' Parse "W<width> H<height>" or "W<width>H<height>" as a registered minimum.
                    Dim parts() As String = grid.GridSize.TrimStart("W"c).Split("H"c)
                    If parts.Length = 2 Then
                        Integer.TryParse(parts(0).Trim(), minW)
                        Integer.TryParse(parts(1).Trim(), minH)
                    Else
                        HandleUserMessageLogging("GMRC",
                            $"FinalizeGridDisplays: Could not parse GridSize '{grid.GridSize}' for grid '{grid.Name}'. Content size will be used.")
                    End If
                End If

                grid.Width = Math.Max(contentWidth, minW)
                grid.Height = Math.Max(contentHeight, minH)

                ' Keep GridSize in sync with the resolved dimensions.
                grid.GridSize = $"W{grid.Width} H{grid.Height}"

                ' ── 6. Finalize header label ─────────────────────────────────────────────
                FinalizeGridHeader(grid)

            Next

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"FinalizeGridDisplays ERROR: {ex.Message}")
        End Try

    End Sub

    ''' <summary>
    ''' Resizes each host <see cref="FormDataClass"/> so that its client area fits all of its
    ''' grids (including their header labels) with a small padding border.
    ''' Called once from <c>ReadInSignalList</c> immediately after <c>FinalizeGridDisplays</c>.
    ''' The registered <c>DisplayWindowSize</c> is respected as a minimum — content only grows
    ''' the form, never shrinks it below the registration value.
    ''' <c>FormDataClass.DefaultWidth</c> and <c>DefaultHeight</c> are updated to match.
    ''' </summary>
    Shared Sub AutoSizeFormsToContent()

        Const FORM_PADDING As Integer = 5   ' pixels around the outermost grid edge

        Try
            For formIdx As Integer = 0 To GmResidentClient.MyDFs.Count - 1

                Dim hostForm As FormDataClass = GmResidentClient.MyDFs(formIdx)
                If hostForm Is Nothing Then Continue For

                ' Collect all grids that belong to this form.
                Dim formGrids As IEnumerable(Of GridDataClass) =
                    myDGs.Where(Function(g) g IsNot Nothing AndAlso g.ParentFormIndex = formIdx)

                If Not formGrids.Any() Then Continue For

                ' Compute the bounding rectangle that encloses every grid + its header.
                Dim maxRight As Integer = 0
                Dim maxBottom As Integer = 0

                For Each grid As GridDataClass In formGrids
                    Dim currentGrid = grid  ' Capture to avoid closure issue with iteration variable
                    ' Right edge of the grid body.
                    Dim right As Integer = currentGrid.Left + currentGrid.Width

                    ' Bottom edge: grid body bottom.
                    Dim bottom As Integer = grid.Top + grid.Height

                    ' Top of the header label (may be above grid.Top for multi-column grids).
                    Dim headerTop As Integer
                    If grid.ColumnCount > 2 Then
                        headerTop = grid.Top - DEFAULT_SEPARATION
                    Else
                        headerTop = grid.Top
                    End If

                    ' The effective top of the whole widget is headerTop; but for bounding box
                    ' purposes we only need the maximum right and bottom extents.
                    maxRight = Math.Max(maxRight, right)
                    maxBottom = Math.Max(maxBottom, bottom)
                Next

                ' Required client size: content + padding; reserve EXIT_BTN_H at the top.
                Dim requiredW As Integer = maxRight + FORM_PADDING
                Dim requiredH As Integer = maxBottom + FORM_PADDING

                ' Use DisplayWindowSize as a registered minimum.
                Dim minFormW As Integer = 0
                Dim minFormH As Integer = 0
                If Not String.IsNullOrEmpty(hostForm.DisplayWindowSize) Then
                    For Each part As String In hostForm.DisplayWindowSize.Split(" "c)
                        If part.StartsWith("W", StringComparison.OrdinalIgnoreCase) Then
                            Integer.TryParse(part.Substring(1), minFormW)
                        ElseIf part.StartsWith("H", StringComparison.OrdinalIgnoreCase) Then
                            Integer.TryParse(part.Substring(1), minFormH)
                        End If
                    Next
                End If

                Dim newW As Integer = Math.Max(requiredW, minFormW)
                Dim newH As Integer = Math.Max(requiredH, minFormH)

                hostForm.ClientSize = New System.Drawing.Size(newW, newH)

                ' Keep DefaultWidth/DefaultHeight in sync for save-back logic.
                hostForm.DefaultWidth = newW
                hostForm.DefaultHeight = newH

                ' Update DisplayWindowSize so it is persisted correctly on next save.
                hostForm.DisplayWindowSize = $"W{newW} H{newH}"

                ' Reposition the EXIT button to the top-right of the resized form.
                Dim exitBtn As Button = GmResidentClient.MyExitButtons(formIdx)
                If exitBtn IsNot Nothing Then
                    exitBtn.Left = newW - exitBtn.Width - FORM_PADDING
                    exitBtn.Top = 5
                End If

            Next

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"AutoSizeFormsToContent ERROR: {ex.Message}")
        End Try

    End Sub

    Shared Sub FormatFlexGrids(ByVal y As Integer, ByVal z As Integer, ByVal SignalIndex As Integer)

        'Called out of ReadInSignalList.  Populates the myDGs (GridDataClass) objects with information from
        'the excel spreadsheet

        Dim x As Integer

        Dim ccon As ColorConverter
        ccon = New ColorConverter()

        myDGs(z).AlsoAssociatedWith(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = exceldata(y, EXCEL_DATA.AlsoAssociatedWith)

        If Val(exceldata(y, EXCEL_DATA.Col)) = 1 Then
            myDGs(z).DisplayName(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = exceldata(y, EXCEL_DATA.DisplayName)
        End If

        myDGs(z).VariableName(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = exceldata(y, EXCEL_DATA.VariableName)
        myDGs(z).DeviceName(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = exceldata(y, EXCEL_DATA.DeviceName)
        myDGs(z).Raster(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = exceldata(y, EXCEL_DATA.Raster)

        myDGs(z).SignalIndex(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = SignalIndex

        'myDGs(z).HighThresh(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = Val(exceldata(y, EXCEL_DATA.HighThresh))
        'myDGs(z).LowThresh(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = Val(exceldata(y, EXCEL_DATA.LowThresh))

        myDGs(z).HighThresh(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = IIf(Val(exceldata(y, EXCEL_DATA.HighThresh)) = 1000000, 10000000, Val(exceldata(y, EXCEL_DATA.HighThresh)))
        myDGs(z).LowThresh(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = IIf(Val(exceldata(y, EXCEL_DATA.LowThresh)) = -1000000, -10000000, Val(exceldata(y, EXCEL_DATA.LowThresh)))

        myDGs(z).LowThreshBackColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = ccon.ConvertFromString(exceldata(y, EXCEL_DATA.LowThreshBackColor))
        myDGs(z).LowThreshForeColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = ccon.ConvertFromString(exceldata(y, EXCEL_DATA.LowThreshForeColor))
        myDGs(z).EqualTo(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = exceldata(y, EXCEL_DATA.EqualTo)

        myDGs(z).CheckForDataChange(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = Not (UCase(exceldata(y, EXCEL_DATA.CheckForDataChange)) = "FALSE")

        myDGs(z).SaveLastValue(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = 0

        myDGs(z).DataFrozen(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = False

        myDGs(z).DataFrozenCounter(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = 0

        myDGs(z).HighThreshBackColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = ccon.ConvertFromString(exceldata(y, EXCEL_DATA.HighThreshBackColor))
        myDGs(z).HighThreshForeColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = ccon.ConvertFromString(exceldata(y, EXCEL_DATA.HighThreshForeColor))
        myDGs(z).DefaultCellBackColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = ccon.ConvertFromString(exceldata(y, EXCEL_DATA.DefaultBackColor))
        myDGs(z).DefaultCellForeColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = ccon.ConvertFromString(exceldata(y, EXCEL_DATA.DefaultForeColor))
        'myDGs(z).CurrentBackColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = myDGs(z).CellBackColor 'REMOVED FOR 64BIT COMPILE
        'myDGs(z).CurrentForeColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = myDGs(z).CellForeColor 'REMOVED FOR 64BIT COMPILE

        myDGs(z).CurrentBackColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = myDGs(z).DefaultCellBackColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col)))
        myDGs(z).CurrentForeColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = myDGs(z).DefaultCellForeColor(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col)))

        'Added this for .csv files, because when creating .csv files from .xlsx files, all quoted entries in Excel spreadsheet go from ""0"" to """""0""""" in .csv file
        'this creates a problem when formatting the string for display, so we have to remove the extra quotation marks which exist if using a .csv file, we do that here...

        If InStr(exceldata(y, EXCEL_DATA.DisplayFormat), """""""") > 0 Then
            exceldata(y, EXCEL_DATA.DisplayFormat) = Mid(exceldata(y, EXCEL_DATA.DisplayFormat), 4, Len(exceldata(y, EXCEL_DATA.DisplayFormat)) - 6)
        End If

        myDGs(z).DisplayFormat(Val(exceldata(y, EXCEL_DATA.Row)), Val(exceldata(y, EXCEL_DATA.Col))) = exceldata(y, EXCEL_DATA.DisplayFormat)

        'This section handles the AlsoAssociatedWith spreadsheet column.  Also Associated With is a method used to
        'associate specific signals with specific logic built into the CLEVIR code.  For example, the top down
        'view needs to know where to get the X and Y coordinate information for fusion, LRR, VIS etc.  This is how we
        'accomplish that.  The key string "TD X" for example tells the system that the signal is associated with the
        'Top Down X position.  The actual key strings used in the spreadsheet also indicate which subsystem such as
        'for fusion the key string is "FUSION TD X"

        If InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "TD X") > 0 Then
            myDGs(z).TD_X_Start_Pos = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "TD Y") > 0 Then
            myDGs(z).TD_Y_Start_Pos = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "TD RANGE") > 0 Then
            myDGs(z).TD_RANGE_Start_Pos = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "TD AZIMUTH") > 0 Then
            myDGs(z).TD_AZIMUTH_Start_Pos = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "OBJID") > 0 Then
            myDGs(z).ObjectID_Start_Pos = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "OBJTYPE") > 0 Then
            myDGs(z).ObjectType_Start_Pos = exceldata(y, EXCEL_DATA.Row)

            '--------------------------------------------------------------------------------------------------------------------

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLUE LINE TD COEF 0") > 0 Then
            '    myDGs(z).LXCR_BLUE_LINE_TD_COEF_0 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLUE LINE TD COEF 1") > 0 Then
            '    myDGs(z).LXCR_BLUE_LINE_TD_COEF_1 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLUE LINE TD COEF 2") > 0 Then
            '    myDGs(z).LXCR_BLUE_LINE_TD_COEF_2 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLUE LINE TD COEF 3") > 0 Then
            '    myDGs(z).LXCR_BLUE_LINE_TD_COEF_3 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR TGT LANE TD COEF 0") > 0 Then
            myDGs(z).LXCR_TGT_LANE_TD_COEF_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR TGT LANE TD COEF 1") > 0 Then
            myDGs(z).LXCR_TGT_LANE_TD_COEF_1 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR TGT LANE TD COEF 2") > 0 Then
            myDGs(z).LXCR_TGT_LANE_TD_COEF_2 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR TGT LANE TD COEF 3") > 0 Then
            myDGs(z).LXCR_TGT_LANE_TD_COEF_3 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLEND PATH TD COEF 0") > 0 Then
            myDGs(z).LXCR_BLEND_PATH_TD_COEF_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLEND PATH TD COEF 1") > 0 Then
            myDGs(z).LXCR_BLEND_PATH_TD_COEF_1 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLEND PATH TD COEF 2") > 0 Then
            myDGs(z).LXCR_BLEND_PATH_TD_COEF_2 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLEND PATH TD COEF 3") > 0 Then
            myDGs(z).LXCR_BLEND_PATH_TD_COEF_3 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLEND PATH TD COEF 4") > 0 Then
            myDGs(z).LXCR_BLEND_PATH_TD_COEF_4 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLEND PATH TD COEF 5") > 0 Then
            myDGs(z).LXCR_BLEND_PATH_TD_COEF_5 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR LON VEL") > 0 Then
            myDGs(z).LXCR_LON_VEL = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLEND PATH LENGTH") > 0 Then
            myDGs(z).LXCR_BLEND_PATH_LENGTH = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR TARGET LANE LENGTH") > 0 Then
            myDGs(z).LXCR_TARGET_LANE_LENGTH = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LMFR LANE LENGTH") > 0 Then
            myDGs(z).LMFR_LANE_LENGTH = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LKAR LATPOS LA") > 0 Then
            myDGs(z).LKAR_LATPOS_LA = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BP YPOS LA") > 0 Then
            myDGs(z).LXCR_BP_YPOS_LA = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR PATH TD LAT POS 0") > 0 Then
            myDGs(z).LXCR_PATH_TD_LAT_POS_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR BLEND PATH TD LAT POS 0") > 0 Then
            myDGs(z).LXCR_BLEND_PATH_TD_LAT_POS_0 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LMFR PATH TD LON POS 0") > 0 Then
            '    myDGs(z).LMFR_PATH_TD_LON_POS_0 = exceldata(y, EXCEL_DATA.Row)


            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR PATH DESIRED TD LON POS 0") > 0 Then
            '    myDGs(z).LXCR_PATH_DESIRED_TD_LON_POS_0 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LXCR PATH DESIRED TD LAT POS 0") > 0 Then
            '    myDGs(z).LXCR_PATH_DESIRED_TD_LAT_POS_0 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "DESIRED PATH TD LON POS 0") > 0 Then
            '    myDGs(z).DESIRED_PATH_TD_LON_POS_0 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "DESIRED PATH TD LAT POS 0") > 0 Then
            '    myDGs(z).DESIRED_PATH_TD_LAT_POS_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "DANGER ZONE LEFT X TD PT 0") > 0 Then
            myDGs(z).DANGER_ZONE_LEFT_X_TD_PT_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "DANGER ZONE LEFT Y TD PT 0") > 0 Then
            myDGs(z).DANGER_ZONE_LEFT_Y_TD_PT_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "DANGER ZONE RIGHT X TD PT 0") > 0 Then
            myDGs(z).DANGER_ZONE_RIGHT_X_TD_PT_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "DANGER ZONE RIGHT Y TD PT 0") > 0 Then
            myDGs(z).DANGER_ZONE_RIGHT_Y_TD_PT_0 = exceldata(y, EXCEL_DATA.Row)

            '---------------------------------------------------------------------------------------------------------------------

            ' ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "BLUE LINE TD COEF 0") > 0 Then
            '     myDGs(z).BLUE_LINE_TD_COEF_0 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "BLUE LINE TD COEF 1") > 0 Then
            '    myDGs(z).BLUE_LINE_TD_COEF_1 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "BLUE LINE TD COEF 2") > 0 Then
            '    myDGs(z).BLUE_LINE_TD_COEF_2 = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "BLUE LINE TD COEF 3") > 0 Then
            '    myDGs(z).BLUE_LINE_TD_COEF_3 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LANE HDNG ANGLE 0") > 0 Then
            myDGs(z).VIS_LANE_HDNG_ANGLE_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LANE CURVATURE 0") > 0 Then
            myDGs(z).VIS_LANE_CURVATURE_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LANE CURVATURE DRV 0") > 0 Then
            myDGs(z).VIS_LANE_CURVATURE_DRV_0 = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "LANE QUALITY 0") > 0 Then
            myDGs(z).VIS_LANE_QUALITY_0 = exceldata(y, EXCEL_DATA.Row)


        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "VPATH LONG OFST") > 0 Then
            myDGs(z).TD_VPATH_LONG_OFST = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "VPATH LAT OFST") > 0 Then
            myDGs(z).TD_VPATH_LAT_OFST = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "VPATH YAW RATE") > 0 Then
            myDGs(z).TD_VPATH_YAW_RATE = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "VPATH LONG VEL") > 0 Then
            myDGs(z).TD_VPATH_LON_VEL = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "VPATH LAT VEL") > 0 Then
            myDGs(z).TD_VPATH_LAT_VEL = exceldata(y, EXCEL_DATA.Row)
        End If

        'The CS_ key string construct indicates a custom screen requires information to display
        'So this is how we map specific signals to custom displays.  If we needed to change the
        'signal name that is associated with the CPS_AUTOBRKREQ for example, we would just have to
        'move the reference to this key's string to a different signal in the configuration file.

        'CUSTOM SCREEN

        ConfigureCustomScreenVariableReferences(y, z)

        For x = 0 To UBound(GmResidentClient.AvailableObjectIDs)
            If InStr(myDGs(z).AlsoAssociatedWith(exceldata(y, EXCEL_DATA.Row), exceldata(y, EXCEL_DATA.Col)), GmResidentClient.AvailableObjectIDs(x)) > 0 Then
                myDGs(z).RelatedTDObjectID = GmResidentClient.AvailableObjectIDs(x)
                Exit For
            End If
        Next x

    End Sub

    Private Shared Sub ConfigureCustomScreenVariableReferences(ByVal y As Integer, ByVal z As Integer)

        'Called out of FormatFlexGrids - Translates Custom Screen context information from the signal list 
        'and gives each mydgs(z).CS_xxx entry a specific index number.  Variable data associated with a particlar
        'index can then be used in conjunction with custom screen displays.

        If InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LaneInvalid") > 0 Then
            myDGs(z).CS_LaneInvalid = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LaneWgtLt") > 0 Then
            myDGs(z).CS_LaneWgtLt = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LaneWgtRt") > 0 Then
            myDGs(z).CS_LaneWgtRt = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_HPP_Wgt") > 0 Then
            myDGs(z).CS_HPP_Wgt = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_HostLaneProbMax") > 0 Then
            myDGs(z).CS_HostLaneProbMax = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AlertUncertainLnLines") > 0 Then
            myDGs(z).CS_AlertUncertainLnLines = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AlertExitLane") > 0 Then
            myDGs(z).CS_AlertExitLane = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AlertLaneEnding") > 0 Then
            myDGs(z).CS_AlertLaneEnding = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AlertMapUnavail") > 0 Then
            myDGs(z).CS_AlertMapUnavail = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AlertOther") > 0 Then
            myDGs(z).CS_AlertOther = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LnNarrowing") > 0 Then
            myDGs(z).CS_LnNarrowing = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LnWidening") > 0 Then
            myDGs(z).CS_LnWidening = exceldata(y, EXCEL_DATA.Row)


        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_ObjectLeft") > 0 Then
            myDGs(z).CS_ObjectLeft = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_ObjectRight") > 0 Then
            myDGs(z).CS_ObjectRight = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_K1C_COPR_SYSSTAT") > 0 Then
            myDGs(z).CS_K1C_COPR_SYSSTAT = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_K2C_COPR_SYSSTAT") > 0 Then
            myDGs(z).CS_K2C_COPR_SYSSTAT = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_PriAutoBrkSysDrInfcStat") > 0 Then
            myDGs(z).CS_PriAutoBrkSysDrInfcStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS__PriAutoBrkSysDrInfcStatRed") > 0 Then
            myDGs(z).CS__PriAutoBrkSysDrInfcStatRed = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_SecAutoBrkSysDrInfcStat") > 0 Then
            myDGs(z).CS_SecAutoBrkSysDrInfcStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS__SecAutoBrkSysDrInfcStatRed") > 0 Then
            myDGs(z).CS__SecAutoBrkSysDrInfcStatRed = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CE_AutoStrgCmndStat") > 0 Then
            myDGs(z).CS_CE_AutoStrgCmndStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_HS_AutoStrgCmndStat") > 0 Then
            myDGs(z).CS_HS_AutoStrgCmndStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AutoPropAxlTrqArbStat") > 0 Then
            myDGs(z).CS_AutoPropAxlTrqArbStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_ESADSS_CntrlStat") > 0 Then
            myDGs(z).CS_ESADSS_CntrlStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AATPCS_PropSysStat") > 0 Then
            myDGs(z).CS_AATPCS_PropSysStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_PriBrkSysStat") > 0 Then
            myDGs(z).CS_PriBrkSysStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS__PriBrkSysStatRed") > 0 Then
            myDGs(z).CS__PriBrkSysStatRed = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_SecBrkSysStat") > 0 Then
            myDGs(z).CS_SecBrkSysStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS__SecBrkSysStatRed") > 0 Then
            myDGs(z).CS__SecBrkSysStatRed = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_PriGdVolt") > 0 Then
            myDGs(z).CS_PriGdVolt = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_SecGdVolt") > 0 Then
            myDGs(z).CS_SecGdVolt = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_SysPwrMd") > 0 Then
            myDGs(z).CS_SysPwrMd = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_PrplsnSysAtv") > 0 Then
            myDGs(z).CS_PrplsnSysAtv = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CE_VehMdMngrSt") > 0 Then
            myDGs(z).CS_CE_VehMdMngrSt = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CE_VehHlthMngrSt") > 0 Then
            myDGs(z).CS_CE_VehHlthMngrSt = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AutoBrkSysRdcPerDet") > 0 Then
            myDGs(z).CS_AutoBrkSysRdcPerDet = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_ADIMCntrlFailed") > 0 Then
            myDGs(z).CS_ADIMCntrlFailed = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_HS_VehMdMngrSt") > 0 Then
            myDGs(z).CS_HS_VehMdMngrSt = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_HS_VehHlthMngrSt") > 0 Then
            myDGs(z).CS_HS_VehHlthMngrSt = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_RedVehMdMngrSt") > 0 Then
            myDGs(z).CS_RedVehMdMngrSt = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_PriCoPCmdMsgStat") > 0 Then
            myDGs(z).CS_PriCoPCmdMsgStat = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_RedAutoBrkSysRdcPerDet") > 0 Then
            myDGs(z).CS_RedAutoBrkSysRdcPerDet = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_RedADIMCntrlFailed") > 0 Then
            myDGs(z).CS_RedADIMCntrlFailed = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_VeTSTR_e_HiThreatObjType") > 0 Then
            myDGs(z).CS_VeTSTR_e_HiThreatObjType = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_VeTSTR_Cnt_HiThreatObjID") > 0 Then
            myDGs(z).CS_VeTSTR_Cnt_HiThreatObjID = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_VeTSTR_t_HiThreatTTC") > 0 Then
            myDGs(z).CS_VeTSTR_t_HiThreatTTC = exceldata(y, EXCEL_DATA.Row)
            '***********************************************************

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AtGradeAnchor") > 0 Then
            myDGs(z).CS_AtGradeAnchor = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AnchorSelect") > 0 Then
            myDGs(z).CS_AnchorSelect = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_HostLaneIndexLeft") > 0 Then
            myDGs(z).CS_HostLaneIndexLeft = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_HostLaneIndexRight") > 0 Then
            myDGs(z).CS_HostLaneIndexRight = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_MapHostLaneIndex") > 0 Then
            myDGs(z).CS_MapHostLaneIndex = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_TargetsHostLaneIndex") > 0 Then
            myDGs(z).CS_TargetsHostLaneIndex = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_DistToNextAtGradeXing") > 0 Then
            myDGs(z).CS_DistToNextAtGradeXing = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_DistToRoadClassTrans") > 0 Then
            myDGs(z).CS_DistToRoadClassTrans = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_DistToNextTrfcCntrDev") > 0 Then
            myDGs(z).CS_DistToNextTrfcCntrDev = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_OnFreeway") > 0 Then
            myDGs(z).CS_OnFreeway = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_RoadClass_Crnt") > 0 Then
            myDGs(z).CS_RoadClass_Crnt = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CmplxIntrsct_Prsnt") > 0 Then
            myDGs(z).CS_CmplxIntrsct_Prsnt = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CntrlPtLatOffsetHPP") > 0 Then
            myDGs(z).CS_CntrlPtLatOffsetHPP = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CntrlPtLatOffsetL") > 0 Then
            myDGs(z).CS_CntrlPtLatOffsetL = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CntrlPtLatOffsetR") > 0 Then
            myDGs(z).CS_CntrlPtLatOffsetR = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CntrlPtLatOffsetPrev") > 0 Then
            myDGs(z).CS_CntrlPtLatOffsetPrev = exceldata(y, EXCEL_DATA.Row)

            'CS_IFC_HeadingWgt
            'CS_PrevCoefWgt
            'CS_MapWgt
            'CS_IMU_BlueLine

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_IFC_HeadingWgt") > 0 Then
            myDGs(z).CS_IFC_HeadingWgt = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_PrevCoefWgt") > 0 Then
            myDGs(z).CS_PrevCoefWgt = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_MapWgt") > 0 Then
            myDGs(z).CS_MapWgt = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_IMU_BlueLine") > 0 Then
            myDGs(z).CS_IMU_BlueLine = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AlertIUncertainLnLines") > 0 Then
            myDGs(z).CS_AlertIUncertainLnLines = exceldata(y, EXCEL_DATA.Row)


            '*******************************************************************************
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LCC_BUTTON_PRESS") > 0 Then
            myDGs(z).CS_LCC_BUTTON_PRESS = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LCC_CLUSTER_MSG") > 0 Then
            myDGs(z).CS_LCC_CLUSTER_MSG = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_HostLaneInx") > 0 Then
            myDGs(z).CS_HostLaneInx = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_HCURVE") > 0 Then
            myDGs(z).CS_HCURVE = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_VehPathEstCurv") > 0 Then
            myDGs(z).CS_VehPathEstCurv = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_Curvature") > 0 Then
            myDGs(z).CS_Curvature = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_IntersectSplitMerge") > 0 Then
            myDGs(z).CS_IntersectSplitMerge = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_DistToNextIntersect") > 0 Then
            myDGs(z).CS_DistToNextIntersect = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_SpltMrgLaneNum") > 0 Then
            myDGs(z).CS_SpltMrgLaneNum = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_NumLanes") > 0 Then
            myDGs(z).CS_NumLanes = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_NextNumLanes") > 0 Then
            myDGs(z).CS_NextNumLanes = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS__NumLanesTrans") > 0 Then
            myDGs(z).CS__NumLanesTrans = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_DistToNumLanesTrans") > 0 Then
            myDGs(z).CS_DistToNumLanesTrans = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_RawLaneLeft") > 0 Then
            myDGs(z).CS_RawLaneLeft = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_RawLaneRight") > 0 Then
            myDGs(z).CS_RawLaneRight = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_PathConf") > 0 Then
            myDGs(z).CS_PathConf = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LCC_RedReq") > 0 Then
            myDGs(z).CS_LCC_RedReq = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LnSplitRedReq") > 0 Then
            myDGs(z).CS_LnSplitRedReq = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LnWgtRedReq") > 0 Then
            myDGs(z).CS_LnWgtRedReq = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_MapAvailRedReq") > 0 Then
            myDGs(z).CS_MapAvailRedReq = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_PathConfRedReq") > 0 Then
            myDGs(z).CS_PathConfRedReq = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_TmpLnRedReq") > 0 Then
            myDGs(z).CS_TmpLnRedReq = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_VPMConfRedReq") > 0 Then
            myDGs(z).CS_VPMConfRedReq = exceldata(y, EXCEL_DATA.Row)


        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LCC_FEATURE_STATUS") > 0 Then
            myDGs(z).CS_LCC_FEATURE_STATUS = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LKA_TRQRQACT") > 0 Then
            myDGs(z).CS_LKA_TRQRQACT = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LKA_TORQUE") > 0 Then
            myDGs(z).CS_LKA_TORQUE = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LKA_DRVRAPPLDTRQ") > 0 Then
            myDGs(z).CS_LKA_DRVRAPPLDTRQ = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LNSNS_DISTTOLLNEDGE") > 0 Then
            myDGs(z).CS_LNSNS_DISTTOLLNEDGE = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LNSNS_DISTTORLNEDGE") > 0 Then
            myDGs(z).CS_LNSNS_DISTTORLNEDGE = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LKA_OFFIND") > 0 Then
            myDGs(z).CS_LKA_OFFIND = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LKA_STDBYIND") > 0 Then
            myDGs(z).CS_LKA_STDBYIND = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LKA_ACTVIND") > 0 Then
            myDGs(z).CS_LKA_ACTVIND = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_FOAI_VAIR") > 0 Then
            myDGs(z).CS_FOAI_VAIR = exceldata(y, EXCEL_DATA.Row)
        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_FOAI_AWIR") > 0 Then
            myDGs(z).CS_FOAI_AWIR = exceldata(y, EXCEL_DATA.Row)
            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CPS_AUTOBRKREQ") > 0 Then
            '    myDGs(z).CS_CPS_AUTOBRKREQ = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AUTOBRKREQ") > 0 Then
            myDGs(z).CS_AUTOBRKREQ = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_AUTOBRKREQTYPE") > 0 Then
            myDGs(z).CS_AUTOBRKREQTYPE = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CPSCBSC_CTRLACC") > 0 Then
            myDGs(z).CS_CPSCBSC_CTRLACC = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_COLPRSYSBRKPRFREQ") > 0 Then
            myDGs(z).CS_COLPRSYSBRKPRFREQ = exceldata(y, EXCEL_DATA.Row)


        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_PEDWARN") > 0 Then
            myDGs(z).CS_PEDWARN = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_BRAKING_FLAG") > 0 Then
            myDGs(z).CS_BRAKING_FLAG = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_ALERT_FLAG") > 0 Then
            myDGs(z).CS_ALERT_FLAG = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_NOTIFICATION_FLAG") > 0 Then
            myDGs(z).CS_NOTIFICATION_FLAG = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_ACC_GAPSETTING") > 0 Then
            myDGs(z).CS_ACC_GAPSETTING = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CPSTOS_TTC") > 0 Then
            myDGs(z).CS_CPSTOS_TTC = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CPSTOS_RANGE") > 0 Then
            '    myDGs(z).CS_CPSTOS_RANGE = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CPSTOS_X_POS") > 0 Then
            myDGs(z).CS_CPSTOS_X_POS = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CPSTOS_Y_POS") > 0 Then
            myDGs(z).CS_CPSTOS_Y_POS = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CPSTOS_X_VEL") > 0 Then
            myDGs(z).CS_CPSTOS_X_VEL = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CPSTOS_Y_VEL") > 0 Then
            myDGs(z).CS_CPSTOS_Y_VEL = exceldata(y, EXCEL_DATA.Row)

            'ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_CPSTOS_VEL") > 0 Then
            '    myDGs(z).CS_CPSTOS_VEL = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_FSRACC_ENGAGED") > 0 Then
            myDGs(z).CS_FSRACC_ENGAGED = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_FSRACC_BRAKE_ACTIVE") > 0 Then
            myDGs(z).CS_FSRACC_BRAKE_ACTIVE = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_FSRACC_ACCEL_ACTIVE") > 0 Then
            myDGs(z).CS_FSRACC_ACCEL_ACTIVE = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_GPS_LAT") > 0 Then
            myDGs(z).CS_GPS_LAT = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_GPS_LON") > 0 Then
            myDGs(z).CS_GPS_LON = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_ODOMETER") > 0 Then
            myDGs(z).CS_ODOMETER = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LaneClass_Crnt") > 0 Then
            myDGs(z).CS_LaneClass_Crnt = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_LCC_CONTROL_ACTIVE") > 0 Then
            myDGs(z).CS_LCC_CONTROL_ACTIVE = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "AUTOANNO") > 0 Then
            myDGs(z).AUTOANNO = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_NAN_STATUS") > 0 Then
            myDGs(z).CS_NAN_STATUS = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_WAVRECORD") > 0 Then
            myDGs(z).CS_WAVRECORD = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_ALC_LANE_CHANGE_DCSN_RSN") > 0 Then
            myDGs(z).CS_ALC_LANE_CHANGE_DCSN_RSN = exceldata(y, EXCEL_DATA.Row)

        ElseIf InStr(exceldata(y, EXCEL_DATA.AlsoAssociatedWith), "CS_ALC_LANE_CHANGE_STATE") > 0 Then
            myDGs(z).CS_ALC_LANE_CHANGE_STATE = exceldata(y, EXCEL_DATA.Row)
        End If

    End Sub

    ' Public Shared myDGs As New List(Of GridDataClass)
    ' and that exceldata(...) is a 2D array of spreadsheet data

    Shared Sub InitializeFlexGrids(
                                   ByVal rowIndex As Integer,
                                   ByVal formIndex As Integer,
                                   ByVal gridIndex As Integer
                                   )
        ' 1) Create the new GridDataClass
        Dim newGrid As New GridDataClass() With {
                .Parent = GmResidentClient.MyDFs(formIndex),
                .ParentFormIndex = formIndex,
                .Name = exceldata(rowIndex, EXCEL_DATA.AssociatedControlName),
                .ParentFormName = GmResidentClient.MyDFs(formIndex).Name,
                .LocationOnForm = exceldata(rowIndex, EXCEL_DATA.LocationOnForm),
                .DefaultColumnOneWidth = DEFAULT_COL_ONE_WIDTH,
                .DefaultColumnWidth = DEFAULT_COL_WIDTH,
                .DefaultRowHeight = DEFAULT_ROW_HEIGHT,
                .GridHeader = New Label()
                }

        ' 2) Insert or add to the myDGs list at the specific index
        '    If gridIndex is equal to myDGs.Count, we do .Add(newGrid) 
        '    Otherwise, we Insert at the specified index.
        If gridIndex >= myDGs.Count Then
            myDGs.Add(newGrid)
        Else
            myDGs.Insert(gridIndex, newGrid)
        End If

        ' 3) Adjust the row/column count to accommodate excel data
        Dim neededRows As Integer = Val(exceldata(rowIndex, EXCEL_DATA.Row)) + 1
        If newGrid.NumberOfRows < neededRows Then
            newGrid.NumberOfRows = neededRows
        End If

        Dim neededCols As Integer = Val(exceldata(rowIndex, EXCEL_DATA.Col)) + 1
        If newGrid.NumberOfColumns < neededCols Then
            newGrid.NumberOfColumns = neededCols
        End If

        ' 4) Set the grid size from the spreadsheet
        newGrid.GridSize = exceldata(rowIndex, EXCEL_DATA.GridSize)

        ' 5) Initialize the context menu, etc.
        InitializeGridContextMenu(newGrid)

        ' 6) Optional: do more initialization if needed

    End Sub

    Shared Sub FinalizeGridHeader(ByVal DataGrid As GridDataClass)

        ' Sets the GridHeader (Label) properties.
        ' The header is sized to the grid width and positioned:
        '   - above the grid (multi-column grids)
        '   - overlapping row 0 (single-column grids)
        ' Header colours are driven by GridHeaderBackColor / GridHeaderForeColor so
        ' callers can theme individual grids without touching this method.

        DataGrid.GridHeader.Parent = GmResidentClient.MyDFs(DataGrid.ParentFormIndex)
        DataGrid.GridHeader.AutoSize = False
        DataGrid.GridHeader.BorderStyle = BorderStyle.FixedSingle
        DataGrid.GridHeader.BackColor = DataGrid.GridHeaderBackColor
        DataGrid.GridHeader.ForeColor = DataGrid.GridHeaderForeColor
        DataGrid.GridHeader.Font = New Font(DataGrid.GridHeader.Font, FontStyle.Bold)
        DataGrid.GridHeader.Left = DataGrid.Left
        DataGrid.GridHeader.Width = DataGrid.Width

        If DataGrid.ColumnCount > 2 Then
            DataGrid.GridHeader.Top = DataGrid.Top - DEFAULT_SEPARATION
            DataGrid.GridHeader.Height = DEFAULT_ROW_HEIGHT
        Else
            DataGrid.GridHeader.Top = DataGrid.Top
            DataGrid.GridHeader.Height = DEFAULT_ROW_HEIGHT + 5
        End If

        DataGrid.GridHeader.Text = DataGrid.Name
        DataGrid.GridHeader.BringToFront()
        DataGrid.GridHeader.Visible = True

    End Sub

    Private Sub GridDataClass_HandleCreated(ByVal sender As Object, ByVal e As EventArgs) Handles Me.HandleCreated

    End Sub

    Private Sub GridDataClass_CellMouseDown(ByVal sender As Object, ByVal e As DataGridViewCellMouseEventArgs) Handles Me.CellMouseDown

        'If the left button is pressed, we capture the initial mouse entry position on the grid in preparation for resizing.
        'If it is the right button, we will display the contextmenustrip.

        'If e.Button = 1 Then ' for some unknown reason, using mousebuttons.left here does not work, very strange....
        If e.Button = MouseButtons.Left Then

            If GridResizeActive = False Then

                GridMouseEntry_X = e.X
                GridMouseEntry_Y = e.Y
                SaveGridWidth = Width
                SaveGridHeight = Height

                SaveColWidthZero = Columns(0).Width

                CellResizeActive = True

            End If

        Else
            GridToModify = Name
            myContextMenuStrip.Show(MousePosition)

        End If


    End Sub

    Private Sub GridHeader_Click(ByVal sender As Object, ByVal e As EventArgs) Handles GridHeader.Click

    End Sub

    Private Sub GridHeader_DoubleClick(ByVal sender As Object, ByVal e As EventArgs) Handles GridHeader.DoubleClick

    End Sub

    Private Sub GridHeader_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles GridHeader.MouseDown

        'For grids, the interaction between the mouse and the grid object is very important because we want
        'to be able to dynamically change grid size properties using the mouse.

        'So when we get a mousedown event in the grid header (which is actually a label overlayed on top
        'of the grid), we want to capture the initial x / y position of the mouse on the label.

        'We will use this information later to perform relative calculations between the initial mouse
        'position and the new mouseposition, which will allow us to move the label control by dragging
        'and releasing the mouse.

        Dim thisLabel As Label = DirectCast(sender, Label)

        If e.Button = MouseButtons.Left Then

            MouseEntry_X = e.X
            MouseEntry_Y = e.Y

        End If

    End Sub

    Private Sub GridHeader_MouseEnter(ByVal sender As Object, ByVal e As EventArgs) Handles GridHeader.MouseEnter

    End Sub

    Private Sub GridHeader_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles GridHeader.MouseMove

        'With the left mouse button down, when we move the mouse, we are dragging not only the
        'gridheader (label control) but the grid that is associated with it as well.

        Dim x As Integer

        Dim thisLabel As Label = DirectCast(sender, Label)

        If e.Button = MouseButtons.Left Then

            thisLabel.Top = thisLabel.Top + (e.Y - MouseEntry_Y)
            thisLabel.Left = thisLabel.Left + (e.X - MouseEntry_X)

            For x = 0 To myDGs.Count - 1
                If myDGs(x).Name = thisLabel.Text Then
                    If myDGs(x).ColumnCount > 2 Then
                        myDGs(x).Top = thisLabel.Top + thisLabel.Height
                    Else
                        myDGs(x).Top = thisLabel.Top
                    End If
                    myDGs(x).Left = thisLabel.Left
                    Exit For
                End If
            Next

            GridCellPropConfig._changesMade = True
        End If

    End Sub

    Private Sub GridHeader_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs) Handles GridHeader.MouseUp

        'After moving, when we release the mouse, the MouseUp event is triggered.  Now we want to save
        'the new position so that we can save it back to the INCAVariableFile excel spreadsheet.

        'We do this by setting the myDGs(x).LocationOnForm value as shown below...

        Dim x As Integer

        Dim thisLabel As Label = DirectCast(sender, Label)

        If e.Button = MouseButtons.Left Then

            For x = 0 To myDGs.Count - 1
                If myDGs(x).Name = thisLabel.Text Then
                    ' The control has already been moved by GridHeader_MouseMove.
                    ' We only need to record its final position.
                    myDGs(x).LocationOnForm = "X" & thisLabel.Left & " Y" & myDGs(x).Top
                    Exit For
                End If
            Next
        End If


    End Sub

    Private Sub GridDataClass_CellMouseMove(ByVal sender As Object, ByVal e As DataGridViewCellMouseEventArgs) Handles Me.CellMouseMove

        'If the left mouse button is being held down and we move the mouse, we will change the width
        'of the grid cell in which the mouse is located.  We may also then change the width of the grid
        'based on what we are doing with the grid column width.

        'If we are not holding down the mouse, then we will display the enumerated value of the grid cell
        'contents in the tooltip text associated with the grid.
        Dim x As Integer

        'If e.Button = 1 Then
        If e.Button = MouseButtons.Left And GridResizeActive = False Then

            For x = 0 To myDGs.Count - 1
                If myDGs(x).GridHeader.Text = Name Then

                    myDGs(x).GridSize = "W" & Width & " H" & Height

                    myDGs(x).GridHeader.Width = Width

                    If e.ColumnIndex = 0 Then
                        myDGs(x).Columns(e.ColumnIndex).Width = SaveColWidthZero + ((e.X - GridMouseEntry_X) * GRID_COL_WIDTH_SIZING_MULTIPLIER)

                        Width = SaveGridWidth + (e.X - GridMouseEntry_X)
                        myDGs(x).GridHeader.Width = Width

                        GridCellPropConfig._changesMade = True

                    ElseIf e.ColumnIndex = 1 And myDGs(x).ColumnCount = 2 Then

                        myDGs(x).Columns(e.ColumnIndex).Width = SaveColWidthZero + ((e.X - GridMouseEntry_X) * GRID_COL_WIDTH_SIZING_MULTIPLIER)

                        Width = (myDGs(x).Columns(0).Width \ GRID_COL_WIDTH_SIZING_MULTIPLIER) + (myDGs(x).Columns(1).Width \ GRID_COL_WIDTH_SIZING_MULTIPLIER) + 20
                        myDGs(x).GridSize = "W" & Width & " H" & Height
                        myDGs(x).GridHeader.Width = Width
                        GridCellPropConfig._changesMade = True

                    Else

                        Height = SaveGridHeight + (e.Y - GridMouseEntry_Y)
                        Width = SaveGridWidth + (e.X - GridMouseEntry_X)

                    End If

                    Exit Sub
                End If
            Next

        End If

    End Sub

    Private Sub GridDataClass_CellMouseUp(ByVal sender As Object, ByVal e As DataGridViewCellMouseEventArgs) Handles Me.CellMouseUp
        CellResizeActive = False
        GridResizeActive = False
    End Sub

    Private Sub ContextMenuStrip1_Click1(ByVal sender As Object, ByVal e As EventArgs) Handles ContextMenuStrip1.Click

    End Sub

    Private Sub GridDataClass_CellMouseEnter(sender As Object, e As DataGridViewCellEventArgs) Handles Me.CellMouseEnter
        'GridResizeActive = False
    End Sub

    Private Sub GridDataClass_MouseDown(sender As Object, e As MouseEventArgs) Handles Me.MouseDown

        Dim ht As HitTestInfo

        ht = HitTest(e.X, e.Y)

        If ht.Type <> DataGridViewHitTestType.None Then
            Exit Sub
        End If

        If e.Button = MouseButtons.Left Then

            If CellResizeActive = False Then
                GridMouseEntry_X = e.X
                GridMouseEntry_Y = e.Y
                SaveGridWidth = Width
                SaveGridHeight = Height

                SaveColWidthZero = Columns(0).Width

                GridResizeActive = True

            End If


        End If
    End Sub

    Private Sub GridDataClass_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove

        If e.Button = MouseButtons.Left And CellResizeActive = False Then

            For x = 0 To myDGs.Count - 1
                If myDGs(x).GridHeader.Text = Name Then

                    myDGs(x).GridSize = "W" & Width & " H" & Height

                    myDGs(x).GridHeader.Width = Width

                    Height = SaveGridHeight + (e.Y - GridMouseEntry_Y)
                    Width = SaveGridWidth + (e.X - GridMouseEntry_X)

                    GridCellPropConfig._changesMade = True

                    Exit Sub
                End If
            Next

        End If
    End Sub

    Private Sub GridDataClass_MouseUp(sender As Object, e As MouseEventArgs) Handles Me.MouseUp
        GridResizeActive = False
        CellResizeActive = False
    End Sub

    Private Sub GridDataClass_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs) Handles Me.CellFormatting

        'Dim mytempstr As String
        'Dim mytempval As Double

        Dim myPropertyIndex_X As Integer
        Dim tooltiptext As String

        Static inhere As Boolean

        If inhere = True Then
            Exit Sub
        End If

        inhere = True

        'data is 0 based, datagridview is 0 based, 0 column is name, 0 row is data, first data is 0,1

        myPropertyIndex_X = e.RowIndex + 1

        'If Me.VariableName(myPropertyIndex_X, e.ColumnIndex) = "PPSMd" Then
        'MsgBox(Me.VariableName(myPropertyIndex_X, e.ColumnIndex))
        'End If

        If Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString = " " Or Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString = "-" Then
            inhere = False
            Exit Sub
        End If

        If DisplayFormat(myPropertyIndex_X, e.ColumnIndex) IsNot Nothing Then

            If e.ColumnIndex > 0 Then

                If DisplayFormat(myPropertyIndex_X, e.ColumnIndex) = "ENUM" Then

                    tooltiptext = Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString

                    Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = tooltiptext

                End If

                'Else
                '   tooltiptext = Me.VariableName(myPropertyIndex_X, e.ColumnIndex)

                '   Me.Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = tooltiptext

            End If

        ElseIf e.ColumnIndex = 0 Then
            tooltiptext = VariableName(myPropertyIndex_X, e.ColumnIndex + 1)
            Rows(e.RowIndex).Cells(e.ColumnIndex).ToolTipText = tooltiptext
        End If

        If SignalIndex(myPropertyIndex_X, e.ColumnIndex) >= 0 And
                                               Len(VariableName(myPropertyIndex_X, e.ColumnIndex)) > 0 And Registered(myPropertyIndex_X, e.ColumnIndex) = True And VariableName(myPropertyIndex_X, e.ColumnIndex) <> "undefined" Then

            'WhereAmI = "Update Grid Colors"

            If DataFrozen(myPropertyIndex_X, e.ColumnIndex) = False Then

                'WhereAmI = "HighThresh Check"
                'If mytempval > Me.HighThresh(e.RowIndex, e.ColumnIndex) And Len(Me.EqualTo(e.RowIndex, e.ColumnIndex)) = 0 Then
                If Val(Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString) > HighThresh(myPropertyIndex_X, e.ColumnIndex) And Len(EqualTo(myPropertyIndex_X, e.ColumnIndex)) = 0 Then

                    If (CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) <> HighThreshBackColor(myPropertyIndex_X, e.ColumnIndex)) Then

                        If InStr(AlsoAssociatedWith(myPropertyIndex_X, e.ColumnIndex), "GO/NOGO") > 0 Then
                            CopyToLog(DeviceName(myPropertyIndex_X, e.ColumnIndex) & " - " & VariableName(myPropertyIndex_X, e.ColumnIndex) & " - Current Value: " & Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString & " Threshold Value: " & HighThresh(myPropertyIndex_X, e.ColumnIndex), 1)
                        Else
                            CopyToLog(DeviceName(myPropertyIndex_X, e.ColumnIndex) & " - " & VariableName(myPropertyIndex_X, e.ColumnIndex) & " - Current Value: " & Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString & " Threshold Value: " & HighThresh(myPropertyIndex_X, e.ColumnIndex), 2)
                        End If

                        CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) = HighThreshBackColor(myPropertyIndex_X, e.ColumnIndex)
                        CurrentForeColor(myPropertyIndex_X, e.ColumnIndex) = HighThreshForeColor(myPropertyIndex_X, e.ColumnIndex)

                        Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = HighThreshBackColor(myPropertyIndex_X, e.ColumnIndex)
                        Rows(e.RowIndex).Cells(e.ColumnIndex).Style.ForeColor = HighThreshForeColor(myPropertyIndex_X, e.ColumnIndex)

                    End If

                    'If InStr(Me.AlsoAssociatedWith(myPropertyIndex_X, e.ColumnIndex), "GO/NOGO") > 0 Then
                    'GoNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                    'UpdateGONOGOLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                    'End If

                End If
                'WhereAmI = "LowThresh Check"
                If Val(Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString) < LowThresh(myPropertyIndex_X, e.ColumnIndex) And Len(EqualTo(myPropertyIndex_X, e.ColumnIndex)) = 0 Then

                    If (CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) <> LowThreshBackColor(myPropertyIndex_X, e.ColumnIndex)) Then

                        If InStr(AlsoAssociatedWith(myPropertyIndex_X, e.ColumnIndex), "GO/NOGO") > 0 Then
                            CopyToLog(DeviceName(myPropertyIndex_X, e.ColumnIndex) & " - " & VariableName(myPropertyIndex_X, e.ColumnIndex) & " - Current Value: " & Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString & " Threshold Value: " & LowThresh(myPropertyIndex_X, e.ColumnIndex), 1)
                        Else
                            CopyToLog(DeviceName(myPropertyIndex_X, e.ColumnIndex) & " - " & VariableName(myPropertyIndex_X, e.ColumnIndex) & " - Current Value: " & Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString & " Threshold Value: " & LowThresh(myPropertyIndex_X, e.ColumnIndex), 2)
                        End If

                        CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) = LowThreshBackColor(myPropertyIndex_X, e.ColumnIndex)
                        CurrentForeColor(myPropertyIndex_X, e.ColumnIndex) = LowThreshForeColor(myPropertyIndex_X, e.ColumnIndex)

                        Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = LowThreshBackColor(myPropertyIndex_X, e.ColumnIndex)
                        Rows(e.RowIndex).Cells(e.ColumnIndex).Style.ForeColor = LowThreshForeColor(myPropertyIndex_X, e.ColumnIndex)

                    End If

                    'If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                    'GoNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                    'UpdateGONOGOLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                    'End If

                End If
                'WhereAmI = "Reset Check HIGH"
                If Val(Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString) <= HighThresh(myPropertyIndex_X, e.ColumnIndex) And Val(Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString) >= LowThresh(myPropertyIndex_X, e.ColumnIndex) And Len(EqualTo(myPropertyIndex_X, e.ColumnIndex)) = 0 Then
                    If (CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) = HighThreshBackColor(myPropertyIndex_X, e.ColumnIndex)) Then
                        CopyToLog(DeviceName(myPropertyIndex_X, e.ColumnIndex) & " - " & VariableName(myPropertyIndex_X, e.ColumnIndex) & " - Current Value: " & Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString & " Threshold Value: " & HighThresh(myPropertyIndex_X, e.ColumnIndex), 2)

                        CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) = DefaultCellBackColor(myPropertyIndex_X, e.ColumnIndex)
                        CurrentForeColor(myPropertyIndex_X, e.ColumnIndex) = DefaultCellForeColor(myPropertyIndex_X, e.ColumnIndex)

                        Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = DefaultCellBackColor(myPropertyIndex_X, e.ColumnIndex)
                        Rows(e.RowIndex).Cells(e.ColumnIndex).Style.ForeColor = DefaultCellForeColor(myPropertyIndex_X, e.ColumnIndex)

                    End If

                    'GridUpdateAction = GridUpdateActions.FROM_HIGH
                    'UpdateGridColor(z, x, y, GridUpdateAction)

                End If
                'WhereAmI = "Reset Check LOW"
                If Val(Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString) >= LowThresh(myPropertyIndex_X, e.ColumnIndex) And Val(Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString) <= HighThresh(myPropertyIndex_X, e.ColumnIndex) And Len(EqualTo(myPropertyIndex_X, e.ColumnIndex)) = 0 Then
                    If (CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) = LowThreshBackColor(myPropertyIndex_X, e.ColumnIndex)) Then
                        CopyToLog(DeviceName(myPropertyIndex_X, e.ColumnIndex) & " - " & VariableName(myPropertyIndex_X, e.ColumnIndex) & " - Current Value: " & Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString & " Threshold Value: " & LowThresh(myPropertyIndex_X, e.ColumnIndex), 2)

                        CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) = DefaultCellBackColor(myPropertyIndex_X, e.ColumnIndex)
                        CurrentForeColor(myPropertyIndex_X, e.ColumnIndex) = DefaultCellForeColor(myPropertyIndex_X, e.ColumnIndex)

                        Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = DefaultCellBackColor(myPropertyIndex_X, e.ColumnIndex)
                        Rows(e.RowIndex).Cells(e.ColumnIndex).Style.ForeColor = DefaultCellForeColor(myPropertyIndex_X, e.ColumnIndex)


                    End If

                End If
                'WhereAmI = "Equal To Check"
                If Len(EqualTo(myPropertyIndex_X, e.ColumnIndex)) > 0 Then
                    If Val(Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString) = Val(EqualTo(myPropertyIndex_X, e.ColumnIndex)) Then
                        If (CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) <> LowThreshBackColor(myPropertyIndex_X, e.ColumnIndex)) Then

                            If InStr(AlsoAssociatedWith(myPropertyIndex_X, e.ColumnIndex), "GO/NOGO") > 0 Then
                                CopyToLog(DeviceName(myPropertyIndex_X, e.ColumnIndex) & " - " & VariableName(myPropertyIndex_X, e.ColumnIndex) & " - Current Value: " & Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString & " Equals " & Val(EqualTo(myPropertyIndex_X, e.ColumnIndex)), 1)
                            Else
                                CopyToLog(DeviceName(myPropertyIndex_X, e.ColumnIndex) & " - " & VariableName(myPropertyIndex_X, e.ColumnIndex) & " - Current Value: " & Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString & " Equals " & Val(EqualTo(myPropertyIndex_X, e.ColumnIndex)), 2)
                            End If

                            CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) = LowThreshBackColor(myPropertyIndex_X, e.ColumnIndex)
                            CurrentForeColor(myPropertyIndex_X, e.ColumnIndex) = LowThreshForeColor(myPropertyIndex_X, e.ColumnIndex)

                            Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = LowThreshBackColor(myPropertyIndex_X, e.ColumnIndex)
                            Rows(e.RowIndex).Cells(e.ColumnIndex).Style.ForeColor = LowThreshForeColor(myPropertyIndex_X, e.ColumnIndex)

                        End If

                        'If InStr(myDGs(z).AlsoAssociatedWith(x, y), "GO/NOGO") > 0 Then
                        'GoNoGoFault(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex) = True
                        'UpdateGONOGOLabelColor(MyDFs(myDGs(z).ParentFormIndex).GoNoGoIndex, Color.Red)
                        'End If

                    Else
                        If (CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) = LowThreshBackColor(myPropertyIndex_X, e.ColumnIndex)) Then

                            CopyToLog(DeviceName(myPropertyIndex_X, e.ColumnIndex) & " - " & VariableName(myPropertyIndex_X, e.ColumnIndex) & " - Current Value: " & Rows(e.RowIndex).Cells(e.ColumnIndex).FormattedValue.ToString & " Not Equal To " & Val(EqualTo(myPropertyIndex_X, e.ColumnIndex)), 2)

                            CurrentBackColor(myPropertyIndex_X, e.ColumnIndex) = DefaultCellBackColor(myPropertyIndex_X, e.ColumnIndex)
                            CurrentForeColor(myPropertyIndex_X, e.ColumnIndex) = DefaultCellForeColor(myPropertyIndex_X, e.ColumnIndex)

                            Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = DefaultCellBackColor(myPropertyIndex_X, e.ColumnIndex)
                            Rows(e.RowIndex).Cells(e.ColumnIndex).Style.ForeColor = DefaultCellForeColor(myPropertyIndex_X, e.ColumnIndex)

                        End If

                    End If
                End If

            End If

            'Else
            '    mytempstr = "UnRgstrd"
        End If

        inhere = False
    End Sub

    Private Async Sub GridDataClass_CellMouseDoubleClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles Me.CellMouseDoubleClick

        'When the user doubleclicks in a grid cell, we populate the fields in the GridCellPropConfig form
        'and display it.

        ' Prevent re-entrancy if the user double-clicks again while this is running.
        If _isDoubleClickBusy Then Return
        _isDoubleClickBusy = True

        Try
            Dim myDeviceIdx As Integer
            Dim x As Integer
            Dim y As Integer
            Dim mydevices() As IGM_INCA_Comm.INCADeviceStatus
            Dim measureelementnames() As String
            Dim row As Integer
            Dim col As Integer
            Dim ccon As ColorConverter

            If MyIncaInterface.MeasurementStarted = True Then
                MsgBox("You must stop measurement prior to performing Grid Cell Configuration ...")
                Exit Sub
            End If

            GridToModify = Name

            ccon = New ColorConverter()

            measureelementnames = Nothing

            myDeviceIdx = -1

            row = CurrentCell.RowIndex
            'col = Me.Col
            col = CurrentCell.ColumnIndex

            GmResidentClient.Cursor = Cursors.WaitCursor

            GridCellPropConfig.DisplayWindowName.Text = Parent.Text
            GridCellPropConfig.ControlName.Text = Name

            GridCellPropConfig.DisplayName.Text = DisplayName(row, 1)

            GridCellPropConfig.HighThreshold.Text = CStr(HighThresh(row, col))
            GridCellPropConfig.LowThreshold.Text = CStr(LowThresh(row, col))

            GridCellPropConfig.CheckForDataChange.Text = CStr(IIf(CheckForDataChange(row, col) = False, "False", "True"))

            GridCellPropConfig.EqualTo.Text = EqualTo(row, col)

            GridCellPropConfig.DefaultBackColorCombo.Text = ccon.ConvertToString(DefaultCellBackColor(row, col))
            GridCellPropConfig.DefaultForeColorCombo.Text = ccon.ConvertToString(DefaultCellForeColor(row, col))
            GridCellPropConfig.HighThreshBackColor.Text = ccon.ConvertToString(HighThreshBackColor(row, col))
            GridCellPropConfig.HighThreshForeColor.Text = ccon.ConvertToString(HighThreshForeColor(row, col))
            GridCellPropConfig.LowThreshBackColor.Text = ccon.ConvertToString(LowThreshBackColor(row, col))
            GridCellPropConfig.LowThreshForeColor.Text = ccon.ConvertToString(LowThreshForeColor(row, col))

            GridCellPropConfig.AlsoAssocWith.Text = AlsoAssociatedWith(row, col)
            GridCellPropConfig.DisplayFormat.Text = DisplayFormat(row, col)

            If GridCellPropConfig.DeviceName.Text <> DeviceName(row, col) Then

                GridCellPropConfig.DeviceName.Items.Clear()

                mydevices = Await MyIncaInterface.GetAvailableDevicesAsync(False)

                If MyIncaInterface.DeviceDataRetrieved = False Then
                    Await MyIncaInterface.GetDeviceAcquisitionRatesAsync()
                End If

                If mydevices IsNot Nothing Then
                    For x = 0 To UBound(mydevices)
                        GridCellPropConfig.DeviceName.Items.Add(mydevices(x).myName)

                        If mydevices(x).myName = DeviceName(row, col) Then
                            GridCellPropConfig.RasterName.Items.Clear()

                            If MyIncaInterface.deviceinfo(x).rasters IsNot Nothing Then
                                For y = 0 To UBound(MyIncaInterface.deviceinfo(x).rasters)
                                    GridCellPropConfig.RasterName.Items.Add(MyIncaInterface.deviceinfo(x).rasters(y).rastername)
                                Next y
                            End If
                        End If

                        If mydevices(x).myName = DeviceName(row, col) Then
                            myDeviceIdx = x
                        End If
                    Next x
                End If
            End If

            GridCellPropConfig.MySenderObject = Me
            GridCellPropConfig.Show()
            GridCellPropConfig.BringToFront()

            If GridCellPropConfig.DeviceName.Text <> DeviceName(row, col) And GridCellPropConfig.Label14.Visible = False Then

                GridCellPropConfig.Label14.Text = "Performing Variable Browsing Operation..."
                GridCellPropConfig.Label14.Visible = True

                GridCellPropConfig.Refresh()

                ' Wrap potentially long-running synchronous calls in Task.Run
                measureelementnames = Await Task.Run(Function()
                                                         If InStr(DeviceName(row, col), "ACP") > 0 Or InStr(DeviceName(row, col), "XCP:1") > 0 Then
                                                             Return MyIncaInterface.BrowseMeasureElementsInDeviceIdxAsync(GridCellPropConfig.TextBox1.Text, myDeviceIdx)
                                                         ElseIf myDeviceIdx >= 0 Then
                                                             Return MyIncaInterface.GetAllMeasureElementNamesInDeviceIdxAsync(myDeviceIdx)
                                                         End If
                                                         Return Nothing
                                                     End Function)

                If measureelementnames IsNot Nothing Then
                    GridCellPropConfig.VariableName.Items.Clear()
                    GridCellPropConfig.VariableName.Items.AddRange(measureelementnames)
                End If

            End If

            If VariableName(row, col) <> "undefined" Then
                GridCellPropConfig.VariableName.Text = VariableName(row, col)
                GridCellPropConfig.DeviceName.Text = DeviceName(row, col)
                GridCellPropConfig.RasterName.Text = Raster(row, col)
            Else
                GridCellPropConfig.VariableName.Text = ""
                GridCellPropConfig.DeviceName.Text = ""
                GridCellPropConfig.RasterName.Text = ""
            End If

        Finally
            ' Ensure the UI is cleaned up, even if an error occurs.
            GmResidentClient.Cursor = Cursors.Arrow
            GridCellPropConfig.Label14.Visible = False
            _isDoubleClickBusy = False
        End Try
    End Sub

    Private Sub ContextMenuStrip1_Opening(sender As Object, e As CancelEventArgs)

    End Sub

End Class

