Public Class DataDictionarySingleton
    ' Static instance for Singleton pattern
    Private Shared ReadOnly Instance As New DataDictionarySingleton()

    ' Constants for button and tab configurations
    Public Const DefaultButtonWidth As Integer = 128
    Public Const DefaultButtonHeight As Integer = 60
    Public Const HorizButtonSpacing As Integer = 2
    Public Const VertButtonSpacing As Integer = 2
    Public Const NumButtonsAcross As Integer = 6
    Public Const DefaultButtonTop As Integer = 5
    Public Shared ReadOnly DefaultButtonLeft As Integer = HorizButtonSpacing

    ' Dynamic tab width (based on number of tabs)
    Private Shared ReadOnly DefaultTabMaxWidth As Integer = 180
    Private Shared ReadOnly DefaultTabMinWidth As Integer = 80

    ' Property to dynamically calculate tab width
    Public Shared ReadOnly Property DynamicTabWidth As Integer
        Get
            ' Renamed the local variable to avoid shadowing the Instance field
            Dim singletonInstance As DataDictionarySingleton = GetInstance()
            Dim numberOfTabs As Integer = singletonInstance.SubTabs.Count
            If numberOfTabs = 0 Then
                Return DefaultTabMaxWidth
            End If
            Return Math.Max(DefaultTabMinWidth, Math.Min(DefaultTabMaxWidth, 1000 \ numberOfTabs))
        End Get
    End Property

    ' Properties to hold parsed data
    Public Property EnumerationTypeRecords As New List(Of EnumerationTypeRecord)
    Public Property AnnotationTypeRecords As New List(Of AnnotationTypeRecord)
    Public Property AnnotationValueRecords As New List(Of AnnotationValueRecord) ' Main AnnotationValue list
    Public Property SubTabs As New Dictionary(Of String, SubTab)


    ' Public method to access Singleton instance
    Public Shared Function GetInstance() As DataDictionarySingleton
        Return Instance
    End Function

    ' Define structures within the Singleton class
    Public Structure EnumerationTypeRecord
        Public RecordType As Integer
        Public Id As Integer
        Public EnumerationDesc(,) As String
        Public HotKeyAssignment() As String
    End Structure

    Public Structure AnnotationTypeRecord
        Public RecordType As Integer
        Public DisplayOrder As Integer
        Public Id As Integer
        Public System As Integer
        Public Description As String
    End Structure

    Public Structure AnnotationValueRecord ' Main AnnotationValue list structure
        Public RecordType As Integer
        Public TypeId As Integer
        Public Id As Integer
        Public EnumerationType As Integer
        Public Description As String
        Public SubTabName As String ' Added SubTabName to support the new data dictionary structure
        Public SaveCustomAnnoFileName As String
        Public SaveCustomAnnotationText() As String
        Public SaveTextString As String
    End Structure

    ' Classes for Sub-tab, Event Button, Sub-categories, and commands
    Public Class EventButton
        Public Property ButtonName As String
        Public Property ButtonId As Integer
        Public Property SubCategories As List(Of String)

        ' NEW: Add a reference to the actual UI button
        Public Property UIButton As Button

        Public Sub New(name As String, id As Integer, subCategories As List(Of String))
            ButtonName = name
            ButtonId = id
            Me.SubCategories = subCategories
        End Sub
    End Class

    Public Class SubTab
        Public Property TabName As String
        Public Property TabId As Integer
        Public Property EventButtons As List(Of EventButton)

        Public Sub New(name As String, id As Integer)
            TabName = name
            TabId = id
            EventButtons = New List(Of EventButton)
        End Sub
    End Class

    ' Properties to hold parsed data
    Public Property Commands As New Dictionary(Of String, Command)

    'Centralized control of VoiceRecognition Class
    Public Class VoiceRecognitionManager
        ' Private shared instance of the class
        Private Shared _instance As VoiceRecognitionClass
        ' Public shared property to access the single instance
        Public Shared ReadOnly Property Instance As VoiceRecognitionClass
            Get
                SyncLock GetType(VoiceRecognitionManager)
                    If _instance Is Nothing Then
                        _instance = New VoiceRecognitionClass()
                    End If
                End SyncLock
                Return _instance
            End Get
        End Property

        ' Private constructor to prevent direct instantiation
        Private Sub New()
        End Sub
    End Class

    Public Class Command
        Public Property CommandText As String
        Public Property Action As Action
    End Class

    ' Add basic commands during initialization
    Private Sub New()
        ' Clear existing commands if any
        Commands.Clear()

        ' Add core commands
        Commands.Add("start recording", New Command With {
                        .CommandText = "recording started",
                        .Action = Sub() OnVehicleScreen.Button14_Click(OnVehicleScreen.Button14, EventArgs.Empty)
                        })
        Commands.Add("stop recording", New Command With {
                        .CommandText = "recording stopped",
                        .Action = Sub() OnVehicleScreen.Button14_Click(OnVehicleScreen.Button14, EventArgs.Empty)
                        })
        Commands.Add("audio recording", New Command With {
                        .CommandText = "recording audio",
                        .Action = Sub() OnVehicleScreen.PictureBox1_Click(OnVehicleScreen.PictureBox1, EventArgs.Empty)
                        })
        Commands.Add("disable", New Command With {
                        .CommandText = "disable commands",
                        .Action = Sub() OnVehicleScreen.Button23_Click(OnVehicleScreen.Button23, EventArgs.Empty)
                        })
        Commands.Add("start measurement", New Command With {
                        .CommandText = "measurement started",
                        .Action = Sub() MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                        .GetAwaiter().GetResult()
                        })
        Commands.Add("stop measurement", New Command With {
                        .CommandText = "measurement stopped",
                        .Action = Sub() MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6) _
                        .GetAwaiter().GetResult()
                        })
    End Sub
End Class


