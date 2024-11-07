Public Class DataDictionarySingleton
    ' Static instance for Singleton pattern
    Private Shared ReadOnly instance As New DataDictionarySingleton()

    ' Constants for button and tab configurations
    Public Shared ReadOnly DEFAULT_BUTTON_WIDTH As Integer = 128
    Public Shared ReadOnly DEFAULT_BUTTON_HEIGHT As Integer = 60
    Public Shared ReadOnly HORIZ_BUTTON_SPACING As Integer = 2
    Public Shared ReadOnly VERT_BUTTON_SPACING As Integer = 2
    Public Shared ReadOnly NUM_BUTTONS_ACROSS As Integer = 6
    Public Shared ReadOnly DEFAULT_BUTTON_TOP As Integer = 5
    Public Shared ReadOnly DEFAULT_BUTTON_LEFT As Integer = HORIZ_BUTTON_SPACING

    ' Dynamic tab width (based on number of tabs)
    Private Shared ReadOnly DEFAULT_TAB_MAX_WIDTH As Integer = 180  ' Maximum width per tab for visibility
    Private Shared ReadOnly DEFAULT_TAB_MIN_WIDTH As Integer = 80   ' Minimum width per tab
    Private Shared ReadOnly MAIN_TAB_WIDTH As Integer = 200         ' Width of the main tab

    ' Property to dynamically calculate tab width
    Public Shared ReadOnly Property DynamicTabWidth As Integer
        Get
            Dim instance = GetInstance()
            Dim numberOfTabs As Integer = instance.SubTabs.Count
            Console.WriteLine($"Calculating DynamicTabWidth. Number of tabs: {numberOfTabs}")

            If numberOfTabs = 0 Then
                Console.WriteLine("No tabs found. Returning default max width.")
                Return DEFAULT_TAB_MAX_WIDTH
            End If

            ' Calculate width within min and max bounds
            Dim calculatedWidth As Integer = Math.Max(DEFAULT_TAB_MIN_WIDTH, Math.Min(DEFAULT_TAB_MAX_WIDTH, 1000 \ numberOfTabs))
            Console.WriteLine($"Calculated DynamicTabWidth: {calculatedWidth} based on {numberOfTabs} tabs.")
            Return calculatedWidth
        End Get
    End Property

    ' Properties to hold parsed data
    Public Property EnumerationTypeRecords As New List(Of EnumerationTypeRecord)
    Public Property AnnotationTypeRecords As New List(Of AnnotationTypeRecord)
    Public Property AnnotationValueRecords As New List(Of AnnotationValueRecord) ' Main AnnotationValue list

    ' Dictionary to store SubTabs by their names, containing Event Buttons and Sub-categories
    Public Property SubTabs As New Dictionary(Of String, SubTab)

    Public Shared ReadOnly Property MAIN_TAB_WIDTH1 As Integer
        Get
            Return MAIN_TAB_WIDTH
        End Get
    End Property

    ' Private constructor to prevent instantiation
    Private Sub New()
        ' Initializations are done in property definitions
    End Sub

    ' Public method to access Singleton instance
    Public Shared Function GetInstance() As DataDictionarySingleton
        Return instance
    End Function

    ' Define structures within the Singleton class
    Public Structure EnumerationTypeRecord
        Public RecordType As Integer
        Public ID As Integer
        Public EnumerationDesc(,) As String
        Public HotKeyAssignment() As String
    End Structure

    Public Structure AnnotationTypeRecord
        Public RecordType As Integer
        Public DisplayOrder As Integer
        Public ID As Integer
        Public System As Integer
        Public Description As String
    End Structure

    Public Structure AnnotationValueRecord ' Main AnnotationValue list structure
        Public RecordType As Integer
        Public TypeID As Integer
        Public ID As Integer
        Public EnumerationType As Integer
        Public Description As String
        Public SubTabName As String ' Added SubTabName to support the new data dictionary structure
        Public SaveCustomAnnoFileName As String
        Public SaveCustomAnnotationText() As String
        Public SaveTextString As String
    End Structure

    ' Classes for Sub-tab, Event Button, and Sub-categories
    Public Class EventButton
        Public Property ButtonName As String
        Public Property ButtonID As Integer
        Public Property SubCategories As List(Of String)

        Public Sub New(name As String, id As Integer, subCategories As List(Of String))
            ButtonName = name
            ButtonID = id
            Me.SubCategories = subCategories  ' Use 'Me.' to reference the instance property
        End Sub
    End Class

    Public Class SubTab
        Public Property TabName As String
        Public Property TabID As Integer
        Public Property EventButtons As List(Of EventButton)

        Public Sub New(name As String, id As Integer)
            TabName = name
            TabID = id
            EventButtons = New List(Of EventButton)
        End Sub
    End Class
End Class


