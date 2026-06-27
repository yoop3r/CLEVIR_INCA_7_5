Imports System.IO
Public Class CustomAnnotation

    'This form is displayed when the user selects the "Custom Annotation" button on any annotation screen...
    'The user may select from a preloaded list of phrases listed in the listbox, or may enter a new string in the text box.
    'When either of the two buttons on this form are pressed, the form is hidden.

    Private _wastopmost As Boolean

    ' Function to read the custom annotation file and return an array of strings
    Public Function ReadCustomAnnotationsFile(ByVal filename As String) As String()
        Dim savedCustomAnnotations As New List(Of String)()

        Try
            If File.Exists(filename) Then
                Using sr As New StreamReader(filename)
                    While Not sr.EndOfStream
                        savedCustomAnnotations.Add(sr.ReadLine())
                    End While
                End Using
            End If
        Catch ex As IOException
            ' Handle file I/O error
            Console.WriteLine("An I/O error occurred: " & ex.Message)
        End Try

        Return savedCustomAnnotations.ToArray()
    End Function

    ' Function to write custom annotations to a file and update mySavedCustomAnnotations array
    Public Sub WriteCustomAnnotationFile(ByVal filename As String, ByVal myStringArray() As String)
        Try
            ' Initialize mySavedCustomAnnotations if it's not already initialized
            If MySavedCustomAnnotations Is Nothing Then
                MySavedCustomAnnotations = New SavedCustomAnnotations() {}
            End If

            ' Update or add to the mySavedCustomAnnotations array
            Dim found As Boolean = False
            For i As Integer = 0 To MySavedCustomAnnotations.Length - 1
                If MySavedCustomAnnotations(i).Filename.Equals(filename, StringComparison.OrdinalIgnoreCase) Then
                    MySavedCustomAnnotations(i).CustomAnnotations = myStringArray
                    found = True
                    Exit For
                End If
            Next

            If Not found Then
                ' Add a new entry if the filename wasn't found
                Dim newAnnotation As New SavedCustomAnnotations With {
                    .Filename = filename,
                    .CustomAnnotations = myStringArray
                }
                ' Resize the array to accommodate the new entry
                Array.Resize(MySavedCustomAnnotations, MySavedCustomAnnotations.Length + 1)
                MySavedCustomAnnotations(MySavedCustomAnnotations.Length - 1) = newAnnotation
            End If

            ' Write to the physical file
            Using sw As New StreamWriter(filename)
                For Each line As String In myStringArray
                    ' Remove any carriage return (vbCr) and line feed (vbLf) characters
                    Dim cleanedLine As String = line.Replace(vbCr, "").Replace(vbLf, "")
                    sw.WriteLine(cleanedLine)
                Next
            End Using
        Catch ex As IOException
            ' Handle file I/O error
            Console.WriteLine("An I/O error occurred: " & ex.Message)
        End Try
    End Sub

    ' Helper function to hide the form and restore top-down view if necessary
    Private Sub HideAndShowTopView()
        Try
            Hide()

            If _wastopmost AndAlso
               GmResidentClient IsNot Nothing AndAlso
               GmResidentClient.MyTdGraphicsContainer IsNot Nothing Then
                GmResidentClient.MyTdGraphicsContainer.TopMost = True
                GmResidentClient.MyTdGraphicsContainer.Show()
            End If
        Catch ex As Exception
            ' Log the exception or handle appropriately
            HandleUserMessageLogging("CustomAnnotation", $"HideAndShowTopView error: {ex.Message}")
        End Try
    End Sub

    ' Cancel button click event
    Private Sub Button2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button2.Click
        Try
            'Clear the textbox
            If TextBox1 IsNot Nothing Then
                TextBox1.Text = ""
            End If

            ' Set the form's DialogResult to Cancel
            Me.DialogResult = DialogResult.Cancel
            Module1.recordingAllowed = True

            'Call method with error handling
            HideAndShowTopView()

            ' Close the form
            Me.Close()
        Catch ex As Exception
            ' Log the exception or handle appropriately
            HandleUserMessageLogging("CustomAnnotation", $"Button2_Click error: {ex.Message}")
        End Try
    End Sub

    ' Write custom event text button click event
    Private Sub Button1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button1.Click
        Try
            ' Set the form's DialogResult to OK
            Me.DialogResult = DialogResult.OK
            Module1.recordingAllowed = True

            'Call method with error handling
            HideAndShowTopView()

            ' Close the form
            Me.Close()
        Catch ex As Exception
            ' Log the exception or handle appropriately
            HandleUserMessageLogging("CustomAnnotation", $"Button1_Click error: {ex.Message}")
        End Try
    End Sub

    ' Update the textbox when a listbox item is selected
    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ListBox1.SelectedIndexChanged
        If ListBox1.SelectedItem IsNot Nothing Then
            TextBox1.Text = ListBox1.SelectedItem.ToString()
        End If
    End Sub

    ' Handle form activation, hiding top-down view if it's on top
    Private Sub CustomAnnotation_Activated(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Activated
        If GmResidentClient?.MyTdGraphicsContainer?.TopMost Then
            GmResidentClient.MyTdGraphicsContainer.TopMost = False
            GmResidentClient.MyTdGraphicsContainer.Hide()
            _wastopmost = True
        End If
    End Sub

    ' Clear list button click event, also clears relevant annotation data
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Try
            ' Get the DataDictionarySingleton instance
            Dim dataDictionary = DataDictionarySingleton.GetInstance()

            If TextBox1 IsNot Nothing Then
                TextBox1.Text = ""
            End If

            If ListBox1 IsNot Nothing Then
                ListBox1.Refresh()
            End If

            ' Null checks before accessing properties
            If dataDictionary IsNot Nothing AndAlso dataDictionary.AnnotationValueRecords IsNot Nothing Then
                ' Loop through each annotation in AnnotationValueRecords
                For Each annotation As DataDictionarySingleton.AnnotationValueRecord In dataDictionary.AnnotationValueRecords
                    ' Remove the IsNot Nothing check since annotation is a value type (Structure)
                    If Not String.IsNullOrEmpty(annotation.Description) AndAlso
                       Not String.IsNullOrEmpty(Me.Text) AndAlso
                       Me.Text.Contains(annotation.Description) Then
                        annotation.SaveCustomAnnotationText = Nothing
                        annotation.SaveTextString = ""
                        Exit For
                    End If
                Next
            End If
        Catch ex As Exception
            ' Log the exception or handle appropriately
            HandleUserMessageLogging("CustomAnnotation", $"Button3_Click error: {ex.Message}")
        End Try
    End Sub

    Private Sub TextBox1_KeyPress(ByVal sender As Object, ByVal e As KeyPressEventArgs) Handles TextBox1.KeyPress
        ' Check if the pressed key is Enter (carriage return)
        If e.KeyChar = Chr(13) Then
            ' Suppress the key so it doesn't get added to the TextBox
            e.Handled = True

            ' Call the Button1_Click event handler directly when Enter is pressed
            Button1_Click(sender, EventArgs.Empty)
        End If
    End Sub

    ' Placeholder event handlers for TextChanged, LostFocus, and Load events
    Private Sub TextBox1_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles TextBox1.TextChanged
        ' Handle TextChanged event if necessary
    End Sub

    Private Sub CustomAnnotation_Load(sender As Object, e As EventArgs) Handles Me.Load
        ' Handle Load event if necessary
    End Sub

    Private Sub CustomAnnotation_LostFocus(sender As Object, e As EventArgs) Handles Me.LostFocus
        ' Handle LostFocus event if necessary
    End Sub

    'Private Sub CustomAnnotation_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles Me.FormClosing
    '    If e.CloseReason = CloseReason.UserClosing Then
    '        'Exit the application if the user closes the login form
    '        Button2_Click(sender, e)
    '        e.Cancel = True
    '    End If
    'End Sub

End Class
