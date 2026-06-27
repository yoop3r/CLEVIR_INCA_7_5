Imports System
Imports System.ComponentModel

Namespace MSHFlexGridReplace.Data
    Public Class ArrayRowView
        Implements ICustomTypeDescriptor, IEditableObject, IDataErrorInfo

        Private ReadOnly _owner As ArrayDataView
        Private ReadOnly _index As Integer
        Private _error As String

        Friend Sub New(ByVal owner As ArrayDataView, ByVal index As Integer)
            _owner = owner
            _index = index
        End Sub

        Friend Function GetColumn(ByVal index As Integer) As Object
            Return _owner._data.GetValue(_index, index)
        End Function

        Friend Sub SetColumnValue(ByVal index As Integer, ByVal value As Object)
            Try
                _owner._data.SetValue(value, _index, index)
            Catch e As Exception
                _error = e.ToString()
            End Try
        End Sub

        Public Function GetConverter() As TypeConverter
            Return Nothing
        End Function

        Public Function GetEvents(ByVal attributes As Attribute()) As EventDescriptorCollection
            Return EventDescriptorCollection.Empty
        End Function

        Private Function GetEvents() As EventDescriptorCollection
            Return EventDescriptorCollection.Empty
        End Function

        Public Function GetComponentName() As String
            Return Nothing
        End Function

        Public Function GetPropertyOwner(ByVal pd As PropertyDescriptor) As Object
            Return _owner
        End Function

        Public Function GetAttributes() As AttributeCollection
            Return AttributeCollection.Empty
        End Function

        Private Function GetProperties(ByVal attributes As Attribute()) As PropertyDescriptorCollection
            Dim col As Integer = _owner._data.GetLength(1)
            Dim type As Type = _owner._data.[GetType]().GetElementType()
            Dim prop As PropertyDescriptor() = New PropertyDescriptor(col - 1) {}

            For i As Integer = 0 To col - 1
                prop(i) = New ArrayPropertyDescriptor(_owner.ColumnNames(i), type, i)
            Next

            Return New PropertyDescriptorCollection(prop)
        End Function

        Private Function GetProperties() As PropertyDescriptorCollection
            Return GetProperties(Nothing)
        End Function

        Public Function GetEditor(ByVal editorBaseType As Type) As Object
            Return Nothing
        End Function

        Public Function GetDefaultProperty() As PropertyDescriptor
            Return Nothing
        End Function

        Public Function GetDefaultEvent() As EventDescriptor
            Return Nothing
        End Function

        Public Function GetClassName() As String
            Return [GetType]().Name
        End Function

        Public Sub EndEdit()
        End Sub

        Public Sub CancelEdit()
        End Sub

        Public Sub BeginEdit()
        End Sub

        Private Sub IEditableObject_BeginEdit() Implements IEditableObject.BeginEdit
            'Throw New NotImplementedException()
        End Sub

        Private Sub IEditableObject_EndEdit() Implements IEditableObject.EndEdit
            'Throw New NotImplementedException()
        End Sub

        Private Sub IEditableObject_CancelEdit() Implements IEditableObject.CancelEdit
            'Throw New NotImplementedException()
        End Sub

        Private Function ICustomTypeDescriptor_GetAttributes() As AttributeCollection Implements ICustomTypeDescriptor.GetAttributes
            'Throw New NotImplementedException()
            Return AttributeCollection.Empty
        End Function

        Private Function ICustomTypeDescriptor_GetClassName() As String Implements ICustomTypeDescriptor.GetClassName
            'Throw New NotImplementedException()
            Return [GetType]().Name
        End Function

        Private Function ICustomTypeDescriptor_GetComponentName() As String Implements ICustomTypeDescriptor.GetComponentName
            'Throw New NotImplementedException()
            Return Nothing
        End Function

        Private Function ICustomTypeDescriptor_GetConverter() As TypeConverter Implements ICustomTypeDescriptor.GetConverter
            'Throw New NotImplementedException()
            Return Nothing
        End Function

        Private Function ICustomTypeDescriptor_GetDefaultEvent() As EventDescriptor Implements ICustomTypeDescriptor.GetDefaultEvent
            'Throw New NotImplementedException()
            Return Nothing
        End Function

        Private Function ICustomTypeDescriptor_GetDefaultProperty() As PropertyDescriptor Implements ICustomTypeDescriptor.GetDefaultProperty
            'Throw New NotImplementedException()
            Return Nothing
        End Function

        Private Function ICustomTypeDescriptor_GetEditor(editorBaseType As Type) As Object Implements ICustomTypeDescriptor.GetEditor
            'Throw New NotImplementedException()
            Return Nothing
        End Function

        Private Function ICustomTypeDescriptor_GetEvents() As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            'Throw New NotImplementedException()
            Return EventDescriptorCollection.Empty
        End Function

        Private Function ICustomTypeDescriptor_GetEvents1(attributes() As Attribute) As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            'Throw New NotImplementedException()
            Return EventDescriptorCollection.Empty
        End Function

        Private Function ICustomTypeDescriptor_GetProperties() As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            'Throw New NotImplementedException()
            Return GetProperties(Nothing)
        End Function

        Private Function ICustomTypeDescriptor_GetProperties1(attributes() As Attribute) As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            'Throw New NotImplementedException()
            Dim col As Integer = _owner._data.GetLength(1)
            Dim type As Type = _owner._data.[GetType]().GetElementType()
            Dim prop As PropertyDescriptor() = New PropertyDescriptor(col - 1) {}

            For i As Integer = 0 To col - 1
                prop(i) = New ArrayPropertyDescriptor(_owner.ColumnNames(i), type, i)
            Next

            Return New PropertyDescriptorCollection(prop)
        End Function

        Private Function ICustomTypeDescriptor_GetPropertyOwner(pd As PropertyDescriptor) As Object Implements ICustomTypeDescriptor.GetPropertyOwner
            'Throw New NotImplementedException()
            Return _owner
        End Function

        Default Public ReadOnly Property Item(ByVal columnName As String) As String
            Get
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property [Error] As String
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IDataErrorInfo_Item(columnName As String) As String Implements IDataErrorInfo.Item
            Get
                'Throw New NotImplementedException()
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IDataErrorInfo_Error As String Implements IDataErrorInfo.Error
            Get
                'Throw New NotImplementedException()
                Return Nothing
            End Get
        End Property
    End Class
End Namespace
