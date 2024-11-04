Public Class DataDictionarySingleton
    ' Static instance for Singleton pattern
    Private Shared ReadOnly instance As New DataDictionarySingleton()

    ' Properties to hold parsed data
    Public Property EnumerationTypeRecords As New List(Of EnumerationTypeRecord)
    Public Property AnnotationTypeRecords As New List(Of AnnotationTypeRecord)
    Public Property AnnotationValueRecords As New List(Of AnnotationValueRecord) ' Main AnnotationValue list

    ' Dictionary to store SubTabs by their names, containing Event Buttons and Sub-categories
    Public Property SubTabs As New Dictionary(Of String, SubTab)

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

