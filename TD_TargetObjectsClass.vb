Public Class TD_TargetObjectsClass

    'This class supports the top down view screen.  Each sensor input (LRR, SRR, VIS) as well as the Fusion tracks and other top down view elements are objects
    'defined as TD_TargetObjectsClass.

    Inherits Windows.Forms.Label

    Private _IDString As String
    Private _Visible As Boolean
    'was Single
    'was Single
    Private _X_Pos() As Double 'was single
    Private _Y_Pos() As Double 'was single
    Private _TargetVisible() As Boolean
    Private _ObjectID() As String
    Private _SensorCCWRotationDegs As Double ' was Single
    Friend WithEvents ToolTip1 As ToolTip
    Private components As System.ComponentModel.IContainer

    Private MouseEntry_Y As Integer
    Private _saveSenderTop As Integer

    Public Property NumObjects As Integer

    Property TargetObjectID() As String
        Get
            Return _IDString
        End Get
        Set(ByVal value As String)
            _IDString = value
        End Set
    End Property

    Public Property MinSizePixels As Integer

    Public Property ShowFieldOfView As Boolean

    Public Property FieldOfView As Single

    Public Property DefaultHeight As Integer

    Public Property DefaultWidth As Integer

    Property SensorCCWRotationDegs() As Double ''changed from single to double
        Get
            Return _SensorCCWRotationDegs
        End Get
        Set(ByVal value As Double)
            _SensorCCWRotationDegs = value
        End Set
    End Property

    Public Property LeftOfOrigin As Double

    Public Property FwdOfOrigin As Double

    'Property Visible() As Boolean
    '    Get
    '       Return _Visible
    '   End Get
    '    Set(ByVal value As Boolean)
    '       _Visible = value
    '   End Set
    'End Property

    Public Property Fill As Boolean

    Public Property Color As System.Drawing.Color

    Public Property Style As String

    Property TargetVisible(ByVal index As Integer) As Boolean

        Get
            Return _TargetVisible(index)
        End Get

        Set(ByVal value As Boolean)
            If _TargetVisible IsNot Nothing Then
                If index >= UBound(_TargetVisible) Then
                    ReDim Preserve _TargetVisible(index)
                End If
            Else
                ReDim Preserve _TargetVisible(index)
            End If
            _TargetVisible(index) = value
        End Set

    End Property

    Property ObjectID(ByVal index As Integer) As String

        Get
            Return _ObjectID(index)
        End Get

        Set(ByVal value As String)
            If _ObjectID IsNot Nothing Then
                If index >= UBound(_ObjectID) Then
                    ReDim Preserve _ObjectID(index)
                End If
            Else
                ReDim Preserve _ObjectID(index)
            End If
            _ObjectID(index) = value
        End Set

    End Property

    Property X_Pos(ByVal index As Integer) As Double ''changed from single to double

        Get
            Return _X_Pos(index)
        End Get

        Set(ByVal value As Double)
            If _X_Pos IsNot Nothing Then
                If index >= UBound(_X_Pos) Then
                    ReDim Preserve _X_Pos(index)
                End If
            Else
                ReDim Preserve _X_Pos(index)
            End If
            _X_Pos(index) = value
        End Set

    End Property
    Property Y_Pos(ByVal index As Integer) As Double ''changed from single to double
        Get
            Return _Y_Pos(index)
        End Get

        Set(ByVal value As Double)
            If _Y_Pos IsNot Nothing Then
                If index >= UBound(_Y_Pos) Then
                    ReDim Preserve _Y_Pos(index)
                End If
            Else
                ReDim Preserve _Y_Pos(index)
            End If
            _Y_Pos(index) = value
        End Set
    End Property

    Private Function CalculateXYCoords(ByVal Axis As String, ByVal Range As Double, ByVal Azimuth As Double) As Double

        Dim temp As Double

        temp = (Azimuth + SensorCCWRotationDegs) * TDGraphicsContainerClass.F_DEG_TO_RAD
        Select Case Axis

            Case "Y"
                CalculateXYCoords = System.Math.Abs((System.Math.Cos(temp)) * Range) + FwdOfOrigin
            Case "X"
                CalculateXYCoords = ((System.Math.Sin(temp)) * Range) + LeftOfOrigin
        End Select

    End Function


    Public Function GetRelLongPos(ByVal index As Integer) As Double

        Dim z As Integer

        For z = 0 To myDGs.Count - 1
            If myDGs(z).RelatedTDObjectID = TargetObjectID Then

                If myDGs(z).TD_Y_Start_Pos > 0 Then
                    'GetRelLongPos = GmResidentClient.mySignalData(myDGs(z).SignalIndex(myDGs(z).TD_Y_Start_Pos, index))
                    GetRelLongPos = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_Y_Start_Pos, index)).SignalData

                ElseIf myDGs(z).TD_RANGE_Start_Pos > 0 And myDGs(z).TD_AZIMUTH_Start_Pos > 0 Then
                    'GetRelLongPos = CalculateXYCoords("Y", GmResidentClient.mySignalData(myDGs(z).SignalIndex(myDGs(z).TD_RANGE_Start_Pos, index)), GmResidentClient.mySignalData(myDGs(z).SignalIndex(myDGs(z).TD_AZIMUTH_Start_Pos, index)))
                    GetRelLongPos = CalculateXYCoords("Y", GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_RANGE_Start_Pos, index)).SignalData, GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_AZIMUTH_Start_Pos, index)).SignalData)

                Else
                    GetRelLongPos = 9999
                End If
                Exit For
            End If
        Next

    End Function

    Public Function GetRelLatPos(ByVal index As Integer) As Double

        Dim z As Integer

        For z = 0 To myDGs.Count - 1
            If myDGs(z).RelatedTDObjectID = TargetObjectID Then

                If myDGs(z).TD_X_Start_Pos > 0 Then
                    'GetRelLatPos = GmResidentClient.mySignalData(myDGs(z).SignalIndex(myDGs(z).TD_X_Start_Pos, index))
                    GetRelLatPos = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_X_Start_Pos, index)).SignalData

                ElseIf myDGs(z).TD_RANGE_Start_Pos > 0 And myDGs(z).TD_AZIMUTH_Start_Pos > 0 Then
                    'GetRelLatPos = CalculateXYCoords("X", GmResidentClient.mySignalData(myDGs(z).SignalIndex(myDGs(z).TD_RANGE_Start_Pos, index)), GmResidentClient.mySignalData(myDGs(z).SignalIndex(myDGs(z).TD_AZIMUTH_Start_Pos, index)))
                    GetRelLatPos = CalculateXYCoords("X", GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_RANGE_Start_Pos, index)).SignalData, GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).TD_AZIMUTH_Start_Pos, index)).SignalData)

                Else
                    GetRelLatPos = 9999
                End If

                Exit For
            End If
        Next

    End Function

    Public Function GetObjectID(ByVal index As Integer) As Double

        Dim z As Integer

        For z = 0 To myDGs.Count - 1
            If myDGs(z).RelatedTDObjectID = TargetObjectID Then

                If myDGs(z).ObjectID_Start_Pos > 0 Then
                    'GetObjectID = GmResidentClient.mySignalData(myDGs(z).SignalIndex(myDGs(z).ObjectID_Start_Pos, index))
                    GetObjectID = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).ObjectID_Start_Pos, index)).SignalData

                Else
                    GetObjectID = 0
                End If

                Exit For
            End If
        Next

    End Function

    Public Function GetObjectType(ByVal index As Integer) As Double

        Dim z As Integer

        For z = 0 To myDGs.Count - 1
            If myDGs(z).RelatedTDObjectID = TargetObjectID Then

                If myDGs(z).ObjectType_Start_Pos > 0 Then
                    GetObjectType = GmResidentClient.MySignalDataWithTime(myDGs(z).SignalIndex(myDGs(z).ObjectType_Start_Pos, index)).SignalData
                Else
                    GetObjectType = 0
                End If

                Exit For
            End If
        Next

    End Function
    Public Function GetRelLongVel(ByVal index As Integer) As Double

    End Function
    Public Function GetRelLatVel(ByVal index As Integer) As Double

    End Function

    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.SuspendLayout()
        '
        'ToolTip1
        '
        Me.ToolTip1.AutomaticDelay = 0
        Me.ToolTip1.ToolTipTitle = "Test"
        Me.ToolTip1.UseAnimation = False
        Me.ToolTip1.UseFading = False
        '
        'TD_TargetObjectsClass
        '
        Me.ResumeLayout(False)

    End Sub

    Private Sub TD_TargetObjectsClass_HandleCreated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.HandleCreated

    End Sub

    Private Sub TD_TargetObjectsClass_Invalidated(ByVal sender As Object, ByVal e As System.Windows.Forms.InvalidateEventArgs) Handles Me.Invalidated

    End Sub

    Private Sub TD_TargetObjectsClass_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDown

        If e.Button = MouseButtons.Left Then
            If Me.TargetObjectID = "Vehicle" Then
                MouseEntry_Y = e.Y + GmResidentClient.MyTdGraphicsContainer.Offset_Y
            End If
        End If

    End Sub

    Private Sub TD_TargetObjectsClass_MouseHover(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.MouseHover

    End Sub

    Private Sub ToolTip1_Popup(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PopupEventArgs) Handles ToolTip1.Popup

    End Sub

    Private Sub TD_TargetObjectsClass_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseMove


        If e.Button = MouseButtons.Left Then

            If MouseEntry_Y <> 0 Then

                Me.Top = Me.Top + (e.Y - MouseEntry_Y)
                GmResidentClient.MyTdGraphicsContainer.HostVehiclePercentFromBottom = 100 - ((Me.Top \ GmResidentClient.MyTdGraphicsContainer.ViewWindowSizeHeight) * 100)

            End If
        End If


    End Sub

    Private Sub TD_TargetObjectsClass_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp

        MouseEntry_Y = 0
        '.invalidate causes a repaint of the top down graphics container
        GmResidentClient.MyTdGraphicsContainer.Invalidate()

    End Sub

    Private Sub TD_TargetObjectsClass_ParentChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.ParentChanged

    End Sub
End Class
