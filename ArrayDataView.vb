Namespace MSHFlexGridReplace.Data
    Public Class ArrayDataView
        Implements IBindingList

        Private ReadOnly _rows As ArrayRowView()
        Friend ReadOnly _data As Array
        Private _colnames As String()

        Private Sub New(ByVal array As Array)
            If array.Rank <> 2 Then Throw New ArgumentException("Supports only two dimentional arrays", "array")
            _data = array
            _rows = New ArrayRowView(array.GetLength(0) - 1) {}

            For i As Integer = 0 To _rows.Length - 1
                _rows(i) = New ArrayRowView(Me, i)
            Next
        End Sub

        Public Sub New(ByVal array As Array, ByVal colnames As Object())
            Me.New(array)
            If colnames.Length <> array.GetLength(1) Then Throw New ArgumentException("column names must correspond to array columns.", "colnames")
            _colnames = New String(colnames.Length - 1) {}

            For i As Integer = 0 To colnames.Length - 1
                _colnames(i) = colnames(i).ToString()
            Next
        End Sub

        Friend ReadOnly Property ColumnNames As String()
            Get

                If _colnames Is Nothing Then
                    _colnames = New String(_data.GetLength(1) - 1) {}

                    For i As Integer = 0 To _colnames.Length - 1
                        _colnames(i) = i.ToString()
                    Next
                End If

                Return _colnames
            End Get
        End Property

        Public Sub Reset()
            OnListChanged(New ListChangedEventArgs(ListChangedType.Reset, -1))
        End Sub

        Public Sub AddIndex(ByVal [property] As PropertyDescriptor)
        End Sub

        Public ReadOnly Property AllowNew As Boolean
            Get
                Return False
            End Get
        End Property

        Public Sub ApplySort(ByVal [property] As PropertyDescriptor, ByVal direction As ListSortDirection)
        End Sub

        Public ReadOnly Property SortProperty As PropertyDescriptor
            Get
                Return Nothing
            End Get
        End Property

        Public Function Find(ByVal [property] As PropertyDescriptor, ByVal key As Object) As Integer
            Return 0
        End Function

        Public ReadOnly Property SupportsSorting As Boolean
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property IsSorted As Boolean
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property AllowRemove As Boolean
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property SupportsSearching As Boolean
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property SortDirection As ListSortDirection
            Get
                Return New ListSortDirection()
            End Get
        End Property

        Public Event ListChanged As ListChangedEventHandler
        Private Event IBindingList_ListChanged As ListChangedEventHandler Implements IBindingList.ListChanged

        Private Sub OnListChanged(ByVal e As ListChangedEventArgs)
            RaiseEvent ListChanged(Me, e)
        End Sub

        Public ReadOnly Property SupportsChangeNotification As Boolean
            Get
                Return True
            End Get
        End Property

        Public Sub RemoveSort()
        End Sub

        Public Function AddNew() As Object
            Return Nothing
        End Function

        Public ReadOnly Property AllowEdit As Boolean
            Get
                Return True
            End Get
        End Property

        Public Sub RemoveIndex(ByVal [property] As PropertyDescriptor)
        End Sub

        Public ReadOnly Property IsReadOnly As Boolean
            Get
                Return True
            End Get
        End Property

        Default Public Property Item(ByVal index As Integer) As Object
            Get
                Return _rows(index)
            End Get
            Set(ByVal value As Object)
            End Set
        End Property

        Public Sub RemoveAt(ByVal index As Integer)
        End Sub

        Public Sub Insert(ByVal index As Integer, ByVal value As Object)
        End Sub

        Public Sub Remove(ByVal value As Object)
        End Sub

        Public Function Contains(ByVal value As Object) As Boolean
            Return False
        End Function

        Public Sub Clear()
        End Sub

        Public Function IndexOf(ByVal value As Object) As Integer
            Return 0
        End Function

        Public Function Add(ByVal value As Object) As Integer
            Return 0
        End Function

        Public ReadOnly Property IsFixedSize As Boolean
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property IsSynchronized As Boolean
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property Count As Integer
            Get
                Return _rows.Length
            End Get
        End Property

        Public Sub CopyTo(ByVal array As Array, ByVal index As Integer)
        End Sub

        Public ReadOnly Property SyncRoot As Object
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IBindingList_AllowNew As Boolean Implements IBindingList.AllowNew
            Get
                'Throw New NotImplementedException()
                Return False
            End Get
        End Property

        Private ReadOnly Property IBindingList_AllowEdit As Boolean Implements IBindingList.AllowEdit
            Get
                'Throw New NotImplementedException()
                Return True
            End Get
        End Property

        Private ReadOnly Property IBindingList_AllowRemove As Boolean Implements IBindingList.AllowRemove
            Get
                'Throw New NotImplementedException()
                Return False
            End Get
        End Property

        Private ReadOnly Property IBindingList_SupportsChangeNotification As Boolean Implements IBindingList.SupportsChangeNotification
            Get
                'Throw New NotImplementedException()
                Return True
            End Get
        End Property

        Private ReadOnly Property IBindingList_SupportsSearching As Boolean Implements IBindingList.SupportsSearching
            Get
                'Throw New NotImplementedException()
                Return False
            End Get
        End Property

        Private ReadOnly Property IBindingList_SupportsSorting As Boolean Implements IBindingList.SupportsSorting
            Get
                'Throw New NotImplementedException()
                Return False
            End Get
        End Property

        Private ReadOnly Property IBindingList_IsSorted As Boolean Implements IBindingList.IsSorted
            Get
                'Throw New NotImplementedException()
                Return False
            End Get
        End Property

        Private ReadOnly Property IBindingList_SortProperty As PropertyDescriptor Implements IBindingList.SortProperty
            Get
                'Throw New NotImplementedException()
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IBindingList_SortDirection As ListSortDirection Implements IBindingList.SortDirection
            Get
                'Throw New NotImplementedException()
                Return New ListSortDirection()
            End Get
        End Property

        Private Property IList_Item(index As Integer) As Object Implements IList.Item

            Get
                Return _rows(index)
            End Get
            Set(ByVal value As Object)
            End Set

            'Get
            'Throw New NotImplementedException()
            'End Get
            'Set(value As Object)
            'Throw New NotImplementedException()
            'End Set
        End Property

        Private ReadOnly Property IList_IsReadOnly As Boolean Implements IList.IsReadOnly
            Get
                'Throw New NotImplementedException()
                Return True
            End Get
        End Property

        Private ReadOnly Property IList_IsFixedSize As Boolean Implements IList.IsFixedSize
            Get
                'Throw New NotImplementedException()
                Return True
            End Get
        End Property

        Private ReadOnly Property ICollection_Count As Integer Implements ICollection.Count
            Get
                'Throw New NotImplementedException()
                Return _rows.Length
            End Get
        End Property

        Private ReadOnly Property ICollection_SyncRoot As Object Implements ICollection.SyncRoot
            Get
                'Throw New NotImplementedException()
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ICollection_IsSynchronized As Boolean Implements ICollection.IsSynchronized
            Get
                'Throw New NotImplementedException()
                Return False
            End Get
        End Property

        Public Function GetEnumerator() As IEnumerator
            Return _rows.GetEnumerator()
        End Function

        Private Function IBindingList_AddNew() As Object Implements IBindingList.AddNew
            'Throw New NotImplementedException()
            Return Nothing
        End Function

        Private Sub IBindingList_AddIndex([property] As PropertyDescriptor) Implements IBindingList.AddIndex
            'Throw New NotImplementedException()
        End Sub

        Private Sub IBindingList_ApplySort([property] As PropertyDescriptor, direction As ListSortDirection) Implements IBindingList.ApplySort
            'Throw New NotImplementedException()
        End Sub

        Private Function IBindingList_Find([property] As PropertyDescriptor, key As Object) As Integer Implements IBindingList.Find
            'Throw New NotImplementedException()
            Return 0
        End Function

        Private Sub IBindingList_RemoveIndex([property] As PropertyDescriptor) Implements IBindingList.RemoveIndex
            'Throw New NotImplementedException()
        End Sub

        Private Sub IBindingList_RemoveSort() Implements IBindingList.RemoveSort
            'Throw New NotImplementedException()
        End Sub

        Private Function IList_Add(value As Object) As Integer Implements IList.Add
            'Throw New NotImplementedException()
            Return 0
        End Function

        Private Function IList_Contains(value As Object) As Boolean Implements IList.Contains
            'Throw New NotImplementedException()
            Return False
        End Function

        Private Sub IList_Clear() Implements IList.Clear
            'Throw New NotImplementedException()
        End Sub

        Private Function IList_IndexOf(value As Object) As Integer Implements IList.IndexOf
            'Throw New NotImplementedException()
            Return 0
        End Function

        Private Sub IList_Insert(index As Integer, value As Object) Implements IList.Insert
            'Throw New NotImplementedException()
        End Sub

        Private Sub IList_Remove(value As Object) Implements IList.Remove
            'Throw New NotImplementedException()
        End Sub

        Private Sub IList_RemoveAt(index As Integer) Implements IList.RemoveAt
            'Throw New NotImplementedException()
        End Sub

        Private Sub ICollection_CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
            'Throw New NotImplementedException()
        End Sub

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            'Throw New NotImplementedException()
            Return _rows.GetEnumerator()
        End Function
    End Class
End Namespace
