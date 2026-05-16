Imports System
Imports System.Diagnostics
Imports System.ComponentModel

Namespace MSHFlexGridReplace.Data
    Public Class ArrayPropertyDescriptor
        Inherits PropertyDescriptor

        Private ReadOnly _index As Integer

        Public Sub New(ByVal name As String, ByVal type As Type, ByVal index As Integer)
            MyBase.New(name, Nothing)
            DisplayName = name
            PropertyType = type
            _index = index
        End Sub

        Public Overrides ReadOnly Property DisplayName As String

        Public Overrides ReadOnly Property ComponentType As Type
            Get
                Return GetType(ArrayRowView)
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property PropertyType As Type

        Public Overrides Function GetValue(ByVal component As Object) As Object
            Try
                Return (CType(component, ArrayRowView)).GetColumn(_index)
            Catch e As Exception
                Debug.WriteLine(e)
            End Try

            Debug.Assert(False)
            Return Nothing
        End Function

        Public Overrides Sub SetValue(ByVal component As Object, ByVal value As Object)
            Try
                '((ArrayRowView)component).SetColumnValue(_index,value)
                '(CType(component, ArrayRowView)).SetColumnValue(_index, value)
                CType(component, ArrayRowView).SetColumnValue(_index, value)
            Catch e As Exception
                Debug.WriteLine(e)
                Debug.Assert(False)
            End Try
        End Sub

        Public Overrides Function CanResetValue(ByVal component As Object) As Boolean
            Return False
        End Function

        Public Overrides Sub ResetValue(ByVal component As Object)
        End Sub

        Public Overrides Function ShouldSerializeValue(ByVal component As Object) As Boolean
            Return False
        End Function
    End Class
End Namespace
