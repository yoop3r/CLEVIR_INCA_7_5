Option Strict Off
Option Explicit On

Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Speech.Synthesis
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml
Imports SharpPcap

Partial Public Class GmResidentClient

    Private Sub PositionGridOnForm(ByVal j As Integer)
        ' Called by ReadInSignalList during initialization.
        '
        ' Positions dynamically created grid on its associated form based on
        ' information in the Excel configuration spreadsheet.

        Dim ctr As Integer = -1
        Dim offset As Integer = 0

        ' The name of the parent form
        Dim targetFormName As String = MyDFs(j).Name

        ' First pass: find the initial offset (index in MyDGs) for grids belonging to targetFormName.
        For i As Integer = 0 To myDGs.Count - 1
            If myDGs(i).Parent IsNot Nothing AndAlso myDGs(i).Parent.Name = targetFormName Then
                ctr += 1
                If ctr = 0 Then
                    offset = i
                End If
            End If
        Next

        ' Set the default size of the form
        MyDFs(j).DefaultWidth = MyDFs(j).Width
        MyDFs(j).DefaultHeight = MyDFs(j).Height

        ' Validate that offset + ctr is within bounds
        If offset + ctr >= myDGs.Count Then
            ' Means we found zero or not enough grids for this form, so we can exit
            Exit Sub
        End If

        ' If the grid we found doesn't match the form name, we do a second pass
        ' to adjust offset and ctr.
        If myDGs(offset + ctr).Parent.Name <> targetFormName Then

            Dim i As Integer = offset + ctr
            Do While i < myDGs.Count
                i += 1
                If i < myDGs.Count AndAlso myDGs(i).Parent IsNot Nothing AndAlso
               myDGs(i).Parent.Name = targetFormName Then
                    offset = i
                    ctr = 0
                    Exit Do
                End If
            Loop

        End If

        ' Double-check offset + ctr again after the second pass
        If offset + ctr >= myDGs.Count Then
            Exit Sub
        End If

        ' Now we should have the correct grid for the form
        If myDGs(offset + ctr).Parent IsNot Nothing AndAlso
       myDGs(offset + ctr).Parent.Name = targetFormName Then

            Select Case myDGs(offset + ctr).LocationOnForm
                Case ""
                    HandleUserMessageLogging("GMRC",
                    "PositionGridOnForm: No Grid Location Specified on form " & myDGs(offset + ctr).Name &
                    ". Defaults will be used for initial position.",
                    ,,,,, OnVehicleScreen.Label2
                )
                    myDGs(offset + ctr).Top = GridDataClass.DEFAULT_SEPARATION
                    myDGs(offset + ctr).Left = GridDataClass.DEFAULT_SEPARATION

                Case Else
                    ' Example location string: "X100 Y50"
                    ' Extract X or left from the substring after "X", up to "Y"
                    Dim locationStr As String = myDGs(offset + ctr).LocationOnForm
                    Dim yPosIndex As Integer = InStr(locationStr, "Y")

                    If yPosIndex > 0 Then
                        ' Extract X value from "X..." up to (but not including) "Y"
                        Dim xStr As String = Mid(locationStr, 2, yPosIndex - 2)  ' skip "X"
                        myDGs(offset + ctr).Left = Val(xStr)

                        ' Extract Y value from after "Y" until the end
                        Dim yStr As String = Mid(locationStr, yPosIndex + 1)
                        myDGs(offset + ctr).Top = Val(yStr)
                    Else
                        ' Fallback if format is unexpected
                        myDGs(offset + ctr).Top = GridDataClass.DEFAULT_SEPARATION
                        myDGs(offset + ctr).Left = GridDataClass.DEFAULT_SEPARATION
                    End If

            End Select

        Else
            HandleUserMessageLogging("GMRC", "PositionGridOnForm: What da huh?")
        End If

    End Sub

    ''' <summary>
    ''' Ensures the form with displayWindowName exists, creating it if not found.
    ''' Also merges the AlsoAssociatedWith info if necessary.
    ''' </summary>
    Private Sub EnsureFormExists(
                                 ByVal displayWindowName As String,
                                 ByVal alsoAssocWith As String,
                                 ByVal displayWindowSize As String
                                 )
        Dim found As Boolean = False

        For z As Integer = 0 To MyDFs.Count - 1
            If MyDFs(z) IsNot Nothing AndAlso MyDFs(z).Name = displayWindowName Then
                found = True
                ' If alsoAssocWith contains specific keywords, update AlsoAssociatedWith
                If alsoAssocWith.Contains("GO/NOGO") OrElse
                   alsoAssocWith.Contains("CS_") OrElse
                   alsoAssocWith.Contains("AUTOANNO") Then

                    If MyDFs(z).AlsoAssociatedWith IsNot Nothing AndAlso
                       Not MyDFs(z).AlsoAssociatedWith.Contains("GO/NOGO") Then
                        MyDFs(z).AlsoAssociatedWith = alsoAssocWith
                    End If
                End If
                Exit For
            End If
        Next

        If Not found Then
            Dim formIndex As Integer = GetNextAvailableFormIndex()
            HandleUserMessageLogging("GMRC", "ReadInSignalList: Creating " & displayWindowName & " form...")
            OnVehicleScreen.Refresh()
            CreateNewForm(formIndex, displayWindowName, alsoAssocWith, displayWindowSize, -5000)
        End If
    End Sub

    ''' <summary>
    ''' Locates the next available form index in MyDFs array by scanning for Nothing entries.
    ''' </summary>
    Private Function GetNextAvailableFormIndex() As Integer
        ' Check if the list is empty
        If MyDFs.Count = 0 Then
            Return 0
        End If

        ' Find the first available index
        For i As Integer = 0 To MyDFs.Count - 1
            If MyDFs(i) Is Nothing Then
                Return i
            End If
        Next

        ' All entries are occupied; return the next index
        Return MyDFs.Count
    End Function

    ''' <summary>
    ''' Either updates an existing grid or creates a new one for the specified form.
    ''' </summary>
    Private Sub UpdateOrCreateGrid(
    ByVal displayWindowName As String,
    ByVal gridName As String,
    ByVal rowIndex As Integer,
    ByVal signalIndex As Integer
)
        ' Extract the desired row/col from exceldata
        Dim rowVal As Integer = CInt(exceldata(rowIndex, EXCEL_DATA.Row))
        Dim colVal As Integer = CInt(exceldata(rowIndex, EXCEL_DATA.Col))

        Dim found As Boolean = False
        Dim gridIndex As Integer = -1

        ' Attempt to find existing grid by looping through MyDGs (a List(Of GridDataClass))
        For i As Integer = 0 To myDGs.Count - 1
            If myDGs(i) IsNot Nothing AndAlso
           myDGs(i).Name = gridName AndAlso
           myDGs(i).ParentFormName = displayWindowName Then

                found = True
                gridIndex = i

                ' Expand row/col if needed
                If myDGs(i).NumberOfRows <= rowVal Then myDGs(i).NumberOfRows = rowVal + 1
                If myDGs(i).NumberOfColumns <= colVal Then myDGs(i).NumberOfColumns = colVal + 1

                ' Format the grid
                GridDataClass.FormatFlexGrids(rowIndex, i, signalIndex)
                Exit For
            End If
        Next

        ' If no existing grid was found, create a new one
        If Not found Then
            Dim formIndex As Integer = FindFormIndex(displayWindowName)

            ' Instead of "GetNextAvailableGridIndex()", we can just set the new gridIndex to the end
            ' of the list. Will consider a certain position that can be set here later.
            gridIndex = myDGs.Count

            ' Create the new grid by calling InitializeFlexGrids
            ' (still passing 'gridIndex' for backward compatibility)
            GridDataClass.InitializeFlexGrids(rowIndex, formIndex, gridIndex)

            ' Format the newly created grid
            GridDataClass.FormatFlexGrids(rowIndex, gridIndex, signalIndex)

            ' Position the grid on the form
            PositionGridOnForm(formIndex)
        End If

        ' Force registration if needed
        If ShouldForceRegister(gridIndex, rowVal, colVal, signalIndex) Then
            MyIncaInterface.mySignals(signalIndex).ForceRegister = True
            myDGs(gridIndex).Registered(rowVal, colVal) = True
            _progressBarMax += 1
        Else
            myDGs(gridIndex).Registered(rowVal, colVal) = False
        End If
    End Sub
    ''' <summary>
    ''' Finds the form index in MyDFs array that matches displayWindowName.
    ''' </summary>
    Private Function FindFormIndex(ByVal displayWindowName As String) As Integer
        For i As Integer = 0 To MyDFs.Count - 1
            If MyDFs(i) IsNot Nothing AndAlso MyDFs(i).Name = displayWindowName Then
                Return i
            End If
        Next
        Return 0
    End Function

    Private Sub MyExitButtons_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'This routine is responsible for handling the Exit buttons on each of the
        'display screens that are configured from the excel spreadsheet INCAVariablefile

        'All it does, is close the form on which the button resides...

        'Whenever a new form is created from the configuration information with CreateNewForm,         
        'an exit button is added dynamically and placed on each form.  A "handler" is created
        'in code for each button so that it will allow the user to exit the display screen.

        'The handler is set up in CreateNewForm using the following construct...

        'This means, add a handler routine for the MyExitButtons(index).click event, and call it MyExitButtons_Click
        'this is where the code will reside to handle the click event of the exit buttons that are dynamically created
        'when creating each display form.

        'AddHandler MyExitButtons(index).Click, AddressOf MyExitButtons_Click

        sender.parent.close()

    End Sub

    Private Sub Mylist_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'This is a handler for the dynamically created Mylist, which is created in the InitializationMonitor
        'thread.  This list is used during initialization to display initialization status information
        'The list will remain on the screen after initialization is complete if there are any initialization
        'errors, otherwise it will automatically disappear.

        'If it does not disppear, then clicking in it will set the KeepListDisplayed flag to false and the
        'listbox will disappear.

        'This allows the user, if there is important information in the list, to read it prior to entering
        'the main display window.

        _keepListBoxDisplayed = False

    End Sub
    Private Sub CreateNewForm(
       ByVal index As Integer,
       ByVal displayWindowName As String,
       ByVal alsoAssociatedWith As String,
       ByVal displayWindowSize As String,
       ByVal formLocation As Integer)

        ' Ensure the lists have enough capacity
        While MyDFs.Count <= index
            MyDFs.Add(Nothing)
        End While
        While MyExitButtons.Count <= index
            MyExitButtons.Add(Nothing)
        End While

        ' Ensure the lists have enough capacity
        While MyDFs.Count <= index
            MyDFs.Add(Nothing)
        End While
        While MyExitButtons.Count <= index
            MyExitButtons.Add(Nothing)
        End While

        ' Create the new form
        MyDFs(index) = New FormDataClass With {
           .Text = displayWindowName,
           .Name = displayWindowName,
           .ShowInTaskbar = False,
           .AlsoAssociatedWith = ""  ' Initialize to empty string
       }

        ' Create the exit button
        MyExitButtons(index) = New Button With {
           .Parent = MyDFs(index),
           .Visible = True,
           .Text = "EXIT"
       }

        ' Set the font for the exit button
        MyExitButtons(index).Font = New Font(MyExitButtons(index).Font.FontFamily, MenuFontSize, FontStyle.Bold)

        ' Add the click event handler
        AddHandler MyExitButtons(index).Click, AddressOf MyExitButtons_Click

        ' Set default display window size if not provided or invalid
        If String.IsNullOrEmpty(displayWindowSize) Then
            displayWindowSize = "W500 H500"
        End If

        ' Parse the width and height from displayWindowSize
        Dim width As Integer = 500
        Dim height As Integer = 500
        Dim sizeParts() As String = displayWindowSize.Split(" "c)
        For Each part As String In sizeParts
            If part.StartsWith("W", StringComparison.OrdinalIgnoreCase) Then
                Integer.TryParse(part.Substring(1), width)
            ElseIf part.StartsWith("H", StringComparison.OrdinalIgnoreCase) Then
                Integer.TryParse(part.Substring(1), height)
            End If
        Next

        ' Set the size of the form
        MyDFs(index).Width = width
        MyDFs(index).Height = height

        ' Set the size and position of the exit button
        With MyExitButtons(index)
            .Width = 100
            .Height = 50
            .Left = MyDFs(index).Width - .Width - 20
            .Top = 5
        End With

        ' Set the AlsoAssociatedWith property if applicable
        If Not String.IsNullOrEmpty(alsoAssociatedWith) AndAlso
          (alsoAssociatedWith.Contains("GO/NOGO") OrElse
           alsoAssociatedWith.Contains("CS_") OrElse
           alsoAssociatedWith.Contains("AUTOANNO")) Then
            MyDFs(index).AlsoAssociatedWith = alsoAssociatedWith
        End If

        ' Set the DisplayWindowSize property
        MyDFs(index).DisplayWindowSize = displayWindowSize

        ' Set the form location
        MyDFs(index).Left = formLocation

        ' Initialize the form's context menu
        InitializeFormContextMenu(MyDFs(index))

        ' Bring the exit button to the front
        MyExitButtons(index)?.BringToFront()

        ' Show the form
        MyDFs(index).Hide()
    End Sub

    Private Sub InitializeFormContextMenu(ByVal MyObject As Object)
        ' Called from CreateNewForm.
        ' Sets up the form context menu using List(Of ToolStripMenuItem).

        ' Initialize a fresh context menu
        MyObject.MyContextMenuStrip = New ContextMenuStrip()

        ' Build a list of ToolStripMenuItems for the "Form Handling" submenu
        Dim formMenuItems As New List(Of ToolStripMenuItem)

        ' 1. "Delete Form"
        Dim deleteFormItem As New ToolStripMenuItem("Delete Form")
        AddHandler deleteFormItem.Click, AddressOf ContextMenu_FormHandling_Click
        formMenuItems.Add(deleteFormItem)

        ' 2. "Create New Grid"
        Dim createGridItem As New ToolStripMenuItem("Create New Grid")
        AddHandler createGridItem.Click, AddressOf ContextMenu_FormHandling_Click
        formMenuItems.Add(createGridItem)

        ' 3. "Set GO/NOGO Display Flag" (CheckOnClick = True)
        Dim goNoGoFlagItem As New ToolStripMenuItem("Set GO/NOGO Display Flag") With {
            .CheckOnClick = True
        }
        ' The handler for this item is different from the first two
        AddHandler goNoGoFlagItem.Click, AddressOf ContextMenu_FormSubMenu_Click
        formMenuItems.Add(goNoGoFlagItem)

        ' Create top-level "Form Handling" menu item
        Dim contextMenuFormHandling As New ToolStripMenuItem("Form Handling") With {
            .BackColor = Color.White,
            .ForeColor = Color.Black,
            .Font = New Font("Georgia", 10),
            .TextAlign = ContentAlignment.BottomRight
        }

        ' Add the sub-items (Delete Form, Create New Grid, GO/NOGO)
        contextMenuFormHandling.DropDownItems.AddRange(formMenuItems.ToArray())

        ' Finally, add the "Form Handling" menu to the context menu strip
        MyObject.MyContextMenuStrip.Items.Add(contextMenuFormHandling)
    End Sub


    Private Sub CreateMenus(ByVal startFormIdx As Integer, Optional ByVal newForm As Boolean = False)

        'This handles the dynamic creation of the menus at the top of the GmResidentClient and
        'other miscellaneous display items as defined in the INCAVariableFile.
        'It is called after the INCAVariableFile is processed to add all of the
        'menu selections to display each form defined.  

        'Also creates the GO/NOGO banner at the top of the GmResidentClient display and the
        'OnVehicleScreen and buttons which are associated with each "GO/NOGO" grid display.

        'Also creates all of the buttons (and handlers for) on the Displays screen which is accessible from the DISPLAYS button
        'at the top of the main screen.

        'This routine is also called when the user creates a new display form.

        Dim gonogo As Integer

        Dim ctr As Integer = -1
        Dim offset As Integer = 0

        Dim z As Integer

        Dim whatrowamion As Integer

        Dim answer As Integer

        Try

            _statusLabelWidth = Int((790 - ((_gonogocount + 1) * StatusLabelSpacing)) \ _gonogocount)

            If MyToolStripMenuItem Is Nothing Then

                HandleUserMessageLogging("GMRC", "CreateMenus: Creating Displays Menu...")
                'StatusNotifier.Toast("CreateMenus: Creating Displays Menu...", "CLEVIR", 2000)
                'OnVehicleScreen.Refresh()

                _MyMenuStrip = MenuStrip1

                _MyMenuStrip.Parent = Me

                MyLogin = New ToolStripMenuItem("Login", Nothing, Nothing, "Login")

                'MyLogin.Font = New Font(MyLogin.Font.FontFamily, MenuFontSize - 2)
                'MyLogin.Font = New Font(MyLogin.Font, FontStyle.Bold)

                '_MyMenuStrip.Items.Add(MyLogin)
                'AddHandler MyLogin.Click, AddressOf MyLogin_Click

                'If InStr(UCase(CLEVIRFlavor), "DATALOGGING") > 0 Then
                '    MyLogin.Enabled = False
                'End If

                MyToolStripMenuItem = New ToolStripMenuItem("Displays")

                MyToolStripMenuItem.Font = New Font(MyToolStripMenuItem.Font.FontFamily, MenuFontSize - 2)
                MyToolStripMenuItem.Font = New Font(MyToolStripMenuItem.Font, FontStyle.Bold)

                _MyMenuStrip.Items.Add(MyToolStripMenuItem)

                MyToolStripMenuItem.DropDownItems.Add(MyTdGraphicsContainer.Name)
                AddHandler MyToolStripMenuItem.DropDownItems(0).Click, AddressOf MyToolStripMenuItem_Click

                ReDim SelectDisplays.MyDisplaySelectButtons(0)
                SelectDisplays.MyDisplaySelectButtons(0) = New Button

                'CUSTOM SCREENS


                MyToolStripMenuItem.DropDownItems.Add("LKA Screen")
                AddHandler MyToolStripMenuItem.DropDownItems(1).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(1)
                SelectDisplays.MyDisplaySelectButtons(1) = New Button

                MyToolStripMenuItem.DropDownItems.Add("Fusion Status Display")
                AddHandler MyToolStripMenuItem.DropDownItems(2).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(2)
                SelectDisplays.MyDisplaySelectButtons(2) = New Button

                MyToolStripMenuItem.DropDownItems.Add("INCA Hardware Status")
                AddHandler MyToolStripMenuItem.DropDownItems(3).Click, AddressOf MyToolStripMenuItem_Click

                ReDim Preserve SelectDisplays.MyDisplaySelectButtons(3)
                SelectDisplays.MyDisplaySelectButtons(3) = New Button

                'Add new custom screens above this (and update NumPredefinedDisplays)...

                _MyRecordPlayback = New ToolStripMenuItem("Record/PlayBack", Nothing, Nothing, "Record/PlayBack")

                _MyRecordPlayback.Font = New Font(_MyRecordPlayback.Font.FontFamily, MenuFontSize - 2)
                _MyRecordPlayback.Font = New Font(_MyRecordPlayback.Font, FontStyle.Bold)

                _MyMenuStrip.Items.Add(_MyRecordPlayback)
                AddHandler _MyRecordPlayback.Click, AddressOf MyRecordPlayback_Click

                _MyMiscInfo = New ToolStripMenuItem("Misc Info", Nothing, Nothing, "Misc Info")

                _MyMiscInfo.Font = New Font(_MyMiscInfo.Font.FontFamily, MenuFontSize - 2)
                _MyMiscInfo.Font = New Font(_MyMiscInfo.Font, FontStyle.Bold)

                _MyMenuStrip.Items.Add(_MyMiscInfo)

                'MyMiscInfo.DropDownItems.Add("Display Operation History")
                'AddHandler MyMiscInfo.DropDownItems(0).Click, AddressOf MyMiscInfo_Click

                _MyMiscInfo.DropDownItems.Add("Display Record Path/Filename")
                AddHandler _MyMiscInfo.DropDownItems(0).Click, AddressOf MyMiscInfo_Click

                _MyMiscInfo.DropDownItems.Add("Display Update Rates")
                AddHandler _MyMiscInfo.DropDownItems(1).Click, AddressOf MyMiscInfo_Click

            Else

                MyToolStripMenuItem.DropDownItems.RemoveAt(MyToolStripMenuItem.DropDownItems.Count - 1)
                _MyCreateNewDisplayMenuItem = Nothing

            End If

            HandleUserMessageLogging("GMRC", "CreateMenus: Adding item(s) to Displays Menu...")
            'StatusNotifier.Toast("CreateMenus: Adding item(s) to Displays Menu...", "CLEVIR", 2000)
            'OnVehicleScreen.Refresh()

            If MyLabel Is Nothing Then
                gonogo = startFormIdx
            Else
                gonogo = UBound(MyLabel) + 1
            End If

            If MyDFs(0) IsNot Nothing Then

                For z = startFormIdx To MyDFs.Count - 1

                    MyToolStripMenuItem.DropDownItems.Add((MyDFs(z).Name))

                    If InStr(MyDFs(z).AlsoAssociatedWith, "GO/NOGO") > 0 Then

                        ReDim Preserve MyLabel(gonogo)

                        MyDFs(z).GoNoGoIndex = gonogo

                        MyLabel(gonogo) = New Label With {
                            .Parent = OnVehicleScreen.GroupBox4,
                            .AutoSize = False,
                            .Visible = True,
                            .BackColor = Color.Green,
                            .ForeColor = Color.White,
                            .BorderStyle = BorderStyle.Fixed3D,
                            .TabStop = False,
                            .Width = _statusLabelWidth,
                            .Height = StatusLabelHeight
                            }

                        OnVehicleScreen.GroupBox4.ResumeLayout(False)

                        MyLabel(gonogo).Top = MyLabel(gonogo).Parent.Height - MyLabel(gonogo).Height - 5

                        MyLabel(gonogo).TextAlign = ContentAlignment.MiddleCenter

                        MyLabel(gonogo).Font = New Font(MyLabel(gonogo).Font.FontFamily, DefaultFontSize - 2)

                        If InStr(MyDFs(z).Name, "CS_") = 0 Then
                            MyLabel(gonogo).Text = MyDFs(z).Name
                        Else
                            MyLabel(gonogo).Text = Mid(MyDFs(z).Name, 4, Len(MyDFs(z).Name))
                        End If

                        AddHandler MyLabel(gonogo).Click, AddressOf MyLabel_Click

                        If gonogo = 0 Then
                            MyLabel(gonogo).Left = StatusLabelSpacing
                        Else
                            MyLabel(gonogo).Left = MyLabel(gonogo - 1).Left + MyLabel(gonogo - 1).Width + StatusLabelSpacing
                        End If
                        gonogo += 1
                    Else
                        MyDFs(z).GoNoGoIndex = -1
                    End If

                    'CUSTOM SCREENS,need to add 1 to NumPredefinedDisplays, every time we add a new custom screen

                    AddHandler MyToolStripMenuItem.DropDownItems(z + NumPredefinedDisplays).Click, AddressOf MyToolStripMenuItem_Click

                    ReDim Preserve SelectDisplays.MyDisplaySelectButtons(z + NumPredefinedDisplays)
                    SelectDisplays.MyDisplaySelectButtons(z + NumPredefinedDisplays) = New Button

                    If newForm = False Then
                        MyDFs(z).Hide()
                    End If

                Next z

            End If

            whatrowamion = -1

            For z = 0 To UBound(SelectDisplays.MyDisplaySelectButtons)

                SelectDisplays.MyDisplaySelectButtons(z).Parent = SelectDisplays

                SelectDisplays.MyDisplaySelectButtons(z).Visible = True
                SelectDisplays.MyDisplaySelectButtons(z).Height = 70
                SelectDisplays.MyDisplaySelectButtons(z).Width = 100
                SelectDisplays.MyDisplaySelectButtons(z).Text = MyToolStripMenuItem.DropDownItems(z).Text
                SelectDisplays.MyDisplaySelectButtons(z).Font = New Font(SelectDisplays.MyDisplaySelectButtons(z).Font.FontFamily, DefaultFontSize)
                SelectDisplays.MyDisplaySelectButtons(z).Font = New Font(SelectDisplays.MyDisplaySelectButtons(z).Font, FontStyle.Bold)

                answer = z Mod 7

                If answer = 0 Then
                    whatrowamion += 1
                End If

                If whatrowamion = 0 Then
                    SelectDisplays.MyDisplaySelectButtons(z).Top = 50
                Else
                    SelectDisplays.MyDisplaySelectButtons(z).Top = SelectDisplays.MyDisplaySelectButtons(0).Top + ((SelectDisplays.MyDisplaySelectButtons(0).Height + 5) * whatrowamion)
                End If

                If z Mod 7 <> 0 Then
                    SelectDisplays.MyDisplaySelectButtons(z).Left = SelectDisplays.MyDisplaySelectButtons(z - 1).Left + SelectDisplays.MyDisplaySelectButtons(z - 1).Width + 10
                Else
                    SelectDisplays.MyDisplaySelectButtons(z).Left = 10
                End If

                AddHandler SelectDisplays.MyDisplaySelectButtons(z).Click, AddressOf SelectDisplays.myDisplaySelectButtons_Click

            Next

            _MyCreateNewDisplayMenuItem = New ToolStripMenuItem With {
                .Text = "Create New Display - Enter Display Name"
            }

            _MyCreateNewDisplayMenuItem.DropDownItems.Add(New ToolStripTextBox())

            _MyCreateNewDisplayMenuItem.DropDownItems(0).Font = New Font(_MyCreateNewDisplayMenuItem.DropDownItems(0).Font.FontFamily, MenuFontSize)
            _MyCreateNewDisplayMenuItem.DropDownItems(0).Font = New Font(_MyCreateNewDisplayMenuItem.DropDownItems(0).Font, FontStyle.Bold)
            _MyCreateNewDisplayMenuItem.DropDownItems(0).BackColor = Color.LightGray

            _MyCreateNewDisplayMenuItem.DropDownItems(0).AutoSize = False
            _MyCreateNewDisplayMenuItem.DropDownItems(0).Width = 400

            _MyCreateNewDisplayMenuItem.DropDownItems.Add("Click Here after entering Display Name")

            MyToolStripMenuItem.DropDownItems.Add(_MyCreateNewDisplayMenuItem)

            AddHandler _MyCreateNewDisplayMenuItem.DropDownItems(1).Click, AddressOf MyCreateNewDisplayMenuItem_Click

            HandleUserMessageLogging("GMRC", "CreateMenus:Menu Item(s) Created")
            'StatusNotifier.Toast("CreateMenus:Menu Item(s) Created", "CLEVIR", 2000)
            'OnVehicleScreen.Refresh()

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "CreateMenus: " & ex.Message, DisplayMsgBox)

        End Try

    End Sub

    Private Sub MyLogin_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'Login selection from top menu bar on GmResidentClient and Login button on OnVehicleScreen
        ' - displays the login form

        OnLoginScreen = True

        If SelectDisplays.Visible = True Then
            SelectDisplays.Close()
        End If

        LoginForm.Show()

    End Sub

    Private Sub MyRecordPlayback_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'Displays the RecordPlayback form, which contains VCR type controls for recording and playing back
        'data through the CLEVIR interface.

        RecordPlayback.Show()
        RecordPlayback.BringToFront()

    End Sub

    Private Sub MyCreateNewDisplayMenuItem_Click(ByVal sender As System.Object, ByVal e As EventArgs)

        'MyCreateNewDisplayMenuItem is dynamically created by CreateMenus as is its handler.

        'Creates a new, blank, user defined form and adds it to the menu for display, as well as
        'allowing the user to add a new grid to the newly created form.

        Dim index As Integer
        Dim x As Integer

        If MyIncaInterface.MeasurementStarted = False Then

            If Len(_MyCreateNewDisplayMenuItem.DropDownItems(0).Text) > 0 Then

                For x = 0 To MyDFs.Count - 1
                    If UCase(_MyCreateNewDisplayMenuItem.DropDownItems(0).Text) = UCase(MyDFs(x).Name) Then
                        MsgBox("There is already a form with this name, please use a different form name.")
                        Exit Sub
                    End If
                Next x

                index = MyDFs.Count

                'CreateNewForm(index, MyCreateNewDisplayMenuItem.DropDownItems(0).Text, "", "W400,H400", 0)
                CreateNewForm(index, _MyCreateNewDisplayMenuItem.DropDownItems(0).Text, "", "W400 H400", 0)

                FormForGridAdd = _MyCreateNewDisplayMenuItem.DropDownItems(0).Text

                NewGridCreation.MyGridTitle = InputBox("Enter Grid Name", "USER INPUT", "undefined")

                For x = 0 To myDGs.Count - 1
                    If UCase(myDGs(x).Name) = UCase(NewGridCreation.MyGridTitle) Then
                        MsgBox("There is already a grid with this name, please use a different grid name.")
                        Exit Sub
                    End If
                Next x

                NewGridCreation.Show()

                CreateMenus(MyDFs.Count - 1, True)
            Else
                HandleUserMessageLogging("GMRC", "Invalid Display Name, Exiting...", DisplayMsgBox)
            End If


        Else
            MsgBox("Measurement must be STOPPED prior to adding a new form.")
        End If


    End Sub

    Public Sub CreateNewGrid(ByVal rows As Integer, ByVal cols As Integer)

        'Creates a new, user defined grid and adds it to the user defined display

        Dim j As Integer
        Dim y As Integer
        Dim z As Integer
        Dim x As Integer


        For j = 0 To myDGs.Count - 1
            If UCase(myDGs(j).Name) = UCase(NewGridCreation.GridTitle.Text) Then
                MsgBox("There is already a grid with this name, please use a different grid name.")
                NewGridCreation.GridTitle.Text = ""
                Exit Sub
            End If
        Next j

        Cursor = Cursors.WaitCursor

        For j = 0 To MyDFs.Count - 1
            If MyDFs(j).Name = FormForGridAdd Then
                Exit For
            End If
        Next j

        z = myDGs.Count - 1 + 1

        GridDataClass.InitializeNewGrid(y, j, z)

        For i = 1 To rows
            For x = 1 To cols
                If myDGs(z).NumberOfRows <= i Then
                    myDGs(z).NumberOfRows = i + 1
                End If

                If myDGs(z).NumberOfColumns <= x Then
                    myDGs(z).NumberOfColumns = x + 1
                End If

            Next
        Next

        'MyDGs(z).RowCount = MyDGs(z).Rows
        'MyDGs(z).ColumnCount = MyDGs(z).ColumnCount

        myDGs(myDGs.Count - 1).Name = NewGridCreation.GridTitle.Text
        myDGs(myDGs.Count - 1).Top = GridDataClass.DEFAULT_SEPARATION

        myDGs(myDGs.Count - 1).Width = myDGs(myDGs.Count - 1).ColumnCount * GridDataClass.DEFAULT_COL_WIDTH + GridDataClass.DEFAULT_SEPARATION
        myDGs(myDGs.Count - 1).Height = myDGs(myDGs.Count - 1).RowCount * GridDataClass.DEFAULT_ROW_HEIGHT + GridDataClass.DEFAULT_SEPARATION

        myDGs(myDGs.Count - 1).BringToFront()

        InitializeGridCellProperties(rows, cols, MyDFs(j).Name, myDGs(myDGs.Count - 1))

        GridDataClass.FinalizeGridHeader(myDGs(myDGs.Count - 1))

        GridCellPropConfig._changesMade = True

        Cursor = Cursors.Arrow


    End Sub


    Private Sub ContextMenu_FormHandling_Click(ByVal sender As Object, ByVal e As EventArgs)

        'This handles the context menu on the form.  The context menu is accessed with a right mouse
        'click, then a particular selection is clicked...

        Dim x As Integer
        Dim y As Integer

        Select Case sender.ToString

            Case "Delete Form"

                If MsgBox("Are you sure you want to delete the " & FormForGridAdd & " form?", vbYesNo) = vbYes Then
                    For x = 0 To MyDFs.Count - 1
                        If MyDFs(x).Name = FormForGridAdd Then
                            GridCellPropConfig._changesMade = True
                            MyDFs(x).Hide()
                            MyDFs(x) = Nothing

                            For y = x To MyDFs.Count - 2
                                MyDFs(y) = MyDFs(y + 1)
                            Next y
                            MyDFs.RemoveAt(MyDFs.Count - 1)

                            Exit For
                        End If
                    Next

                    For x = 0 To MyToolStripMenuItem.DropDownItems.Count - 1
                        If MyToolStripMenuItem.DropDownItems.Item(x).Text = FormForGridAdd Then
                            MyToolStripMenuItem.DropDownItems.RemoveAt(x)
                            Exit For
                        End If
                    Next x
                Else
                    MsgBox(FormForGridAdd & " form, will not be deleted.")
                End If


            Case "Create New Grid"

                If MyIncaInterface.MeasurementStarted = False Then

                    NewGridCreation.MyGridTitle = InputBox("Enter Grid Name", "USER INPUT", "undefined")

                    For x = 0 To myDGs.Count - 1
                        If UCase(myDGs(x).Name) = UCase(NewGridCreation.MyGridTitle) Then
                            MsgBox("There is already a grid with this name, please use a different grid name.")
                            Exit Sub
                        End If
                    Next x

                    NewGridCreation.Show()
                Else
                    MsgBox("Measurement must be stopped prior to adding a new grid.")
                    Exit Sub
                End If

        End Select
    End Sub

    Private Sub ContextMenu_FormSubMenu_Click(ByVal sender As Object, ByVal e As EventArgs)

        'This handles the "Set GO/NOGO Display Flag" selection in the form context menu.  This is either checked
        'or unchecked.  Indicates if this form is to be handled as a GO/NOGO display form or a standard display form...

        Dim x As Integer
        Dim y As Integer

        Select Case sender.ToString

            Case "Set GO/NOGO Display Flag"

                If sender.checked = True Then
                    MsgBox("This form will now be handled as a GO/NOGO form" & " - " & sender.name)
                Else
                    MsgBox("This form will NOT be handled as a GO/NOGO form")
                End If

                For x = 0 To MyDFs.Count - 1
                    If FormForGridAdd = MyDFs(x).Name Then
                        MyDFs(x).AlsoAssociatedWith = IIf(sender.checked = True, "GO/NOGO", "")
                        For y = 0 To myDGs.Count - 1
                            If myDGs(y).Parent.Name = MyDFs(x).Name Then
                                myDGs(y).AlsoAssociatedWith(1, 1) = MyDFs(x).AlsoAssociatedWith
                                Exit For
                            End If
                        Next y
                        GridCellPropConfig._changesMade = True
                        Exit For
                    End If
                Next x

            Case "Add New Form to Displays Menu"

                MsgBox("not implemented...")

        End Select

    End Sub

    Private Sub InitializeGridCellProperties(ByVal rows As Integer, ByVal cols As Integer, ByVal formName As String, ByVal sender As GridDataClass)

        'Called from CreateNewGrid, initializes the custom grid properties based on the number
        'of rows and columns defined by the user

        Dim row As Integer
        Dim col As Integer

        Dim ccon As ColorConverter
        ccon = New ColorConverter()

        For row = 1 To rows

            For col = 1 To cols

                ReDim Preserve MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals) + 1)

                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).DeviceName = "undefined"
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).RasterName = "undefined"
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).SignalName = "undefined"
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).Status = "Invalid"
                MyIncaInterface.mySignals(UBound(MyIncaInterface.mySignals)).ForceRegister = True

                'ADDED if condition, 02262015

                If MyInvisibleSignals IsNot Nothing Then
                    sender.SignalIndex(row, col) = UBound(MyIncaInterface.mySignals) - (UBound(MyInvisibleSignals) + 1)
                Else
                    sender.SignalIndex(row, col) = UBound(MyIncaInterface.mySignals) - 1
                End If

                'sender.Row = row
                sender.CurrentCell = sender(row, col)

                sender.VariableName(row, col) = "undefined"
                sender.DisplayName(row, col) = "undefined"
                sender.HighThresh(row, col) = 10000000
                sender.LowThresh(row, col) = -10000000
                sender.EqualTo(row, col) = ""
                sender.CheckForDataChange(row, col) = False
                sender.DefaultCellBackColor(row, col) = Color.White
                sender.DefaultCellForeColor(row, col) = Color.Black
                sender.HighThreshBackColor(row, col) = Color.Red
                sender.HighThreshForeColor(row, col) = Color.White
                sender.LowThreshBackColor(row, col) = Color.Red
                sender.LowThreshForeColor(row, col) = Color.White
                sender.DeviceName(row, col) = "undefined"
                sender.Raster(row, col) = "undefined"
                sender.AlsoAssociatedWith(row, col) = ""
                sender.DisplayFormat(row, col) = """0.000"""

                sender.CurrentBackColor(row, col) = sender.DefaultCellBackColor(row, col)

                sender.DataFrozen(row, col) = False

                sender.DataFrozenCounter(row, col) = 0

                'sender.Col = 0

                sender.CurrentCell = sender(row, 0)

                If col = 1 Then
                    sender.Text = sender.DisplayName(row, col)
                    'sender.Row = 0
                    'sender.Col = 1
                    sender.CurrentCell = sender(0, 1)


                    sender.Text = "1"
                End If

                If col > 1 Then
                    'sender.Row = 0
                    'sender.Col = col
                    sender.CurrentCell = sender(0, col)

                    sender.Text = CStr(col)
                End If

            Next col
        Next row

    End Sub

    Private Sub MyMiscInfo_Click(ByVal sender As ToolStripMenuItem, ByVal e As EventArgs)

        'MyMiscInfo is created in CreateMenus, as is the MyMiscInfo_Click handler.

        'The MyMiscInfo object represents the selections that can be made from the Misc Info drop
        'down menu selection.

        ' If sender.ToString = "Display Operation History" Then
        ' OperationHistory.Show()
        ' OperationHistory.BringToFront()
        'End If

        If sender.ToString = "Display Record Path/Filename" Then
            If Len(Label3.Text) > 0 Then
                MsgBox(Label3.Text & Label4.Text)
            Else
                MsgBox("No Record Path/Filename defined, please select a Login ID")
            End If
        End If

        If sender.ToString = "Display Update Rates" Then
            MsgBox("Display Refresh Rate = " & _displayRefreshRate & " Data Collection Rate = " & DataCollectionRate & " INCA Polling Rate = " & MyIncaInterface.GetINCAPollingRate)
        End If

    End Sub

    Private Sub MyToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) ' Handles MyToolStripMenuItem1.Click

        'This sub is called when the user selects a display from the displays menu.  All information about the forms, groups, 
        'and grids variable mapping to grid position, etc. is obtained at startup from a spreadsheet which is read in using 
        'the(ReadInSignalList)which is called at startup from the Initialize sub.

        'This sub simply displays the correct form based on who the "sender" is.  This is only applicable
        'to GmResidentClient drop down menu.  The form display is handled differently on the OnVehicleScreen
        'form

        Dim j As Integer

        'look through all forms and find the form with the name corresponding to the sender (which form was selected)
        If MyDFs(0) IsNot Nothing Then

            For j = 0 To MyDFs.Count - 1
                If MyDFs(j).Text = sender.ToString Then

                    If MyDFs(j).Visible = False Then
                        MyDFs(j).Left = Left
                        MyDFs(j).Top = Top
                    End If

                    MyDFs(j).Show()
                    MyDFs(j).BringToFront()
                    Exit Sub
                End If 'MyFormData(j).MyForm.Text <> sender.ToString

            Next j

        End If


        '**********************************************

        'CUSTOM SCREEN

        ShowCustomScreen(sender.ToString)

    End Sub

    Private Sub MyLabel_Click(ByVal sender As Object, ByVal e As EventArgs)

        'Click event for the GO/NOGO labels, displays the GO/NOGO form associated with the label

        'Dim thisLabel As Label = DirectCast(sender, Label)

        Dim compareString As String

        e = EventArgs.Empty

        For j = 0 To MyDFs.Count - 1

            If InStr(MyDFs(j).Text, "CS_") = 0 Then
                compareString = MyDFs(j).Text
            Else
                compareString = Mid(MyDFs(j).Text, 4, Len(MyDFs(j).Text))
            End If

            If InStr(sender.ToString, compareString) > 0 Then

                If InStr(MyDFs(j).Text, "CS_") = 0 Then

                    RegisterDisplaySignals(j)

                    If MyDFs(j).Visible = False Then

                        MyDFs(j).Left = 0
                        MyDFs(j).Top = 30

                        MyDFs(j).Show()
                        MyDFs(j).BringToFront()
                        MyDFs(j).Activate()

                        MyDFs(j).ShowInTaskbar = True

                    Else
                        MyDFs(j).Show()
                        MyDFs(j).BringToFront()
                        MyDFs(j).Activate()

                    End If

                Else

                    RegisterDisplaySignals(j)

                End If

                For i = 0 To myDGs.Count - 1
                    If myDGs(i) Is Nothing Then Continue For
                    If myDGs(i).ParentFormIndex = j Then

                        ' Only iterate data rows (x >= 1) because UpdateGridColor uses Rows(x-1)
                        Dim lastRow As Integer = Math.Max(1, myDGs(i).RowCount)
                        For x = 1 To lastRow
                            For y = 0 To myDGs(i).ColumnCount - 1

                                If sender.backcolor = Color.Gray Then
                                    ' Reset each cell to default safely via UpdateGridColor
                                    UpdateGridColor(i, x, y, GridUpdateActions.FromLow)
                                Else
                                    ' Marshal style writes to the UI thread — DataGridView is not thread-safe.
                                    Dim gi = i, gx = x, gy = y
                                    Dim backC As Color = myDGs(gi).CurrentBackColor(gx, gy)
                                    Dim foreC As Color = myDGs(gi).CurrentForeColor(gx, gy)
                                    If myDGs(gi).InvokeRequired Then
                                        myDGs(gi).Invoke(Sub()
                                                             myDGs(gi).Rows(gx - 1).Cells(gy).Style.BackColor = backC
                                                             myDGs(gi).Rows(gx - 1).Cells(gy).Style.ForeColor = foreC
                                                         End Sub)
                                    Else
                                        myDGs(gi).Rows(gx - 1).Cells(gy).Style.BackColor = backC
                                        myDGs(gi).Rows(gx - 1).Cells(gy).Style.ForeColor = foreC
                                    End If
                                End If


                            Next y
                        Next x

                        myDGs(i).Refresh()

                    End If
                Next i

            End If 'MyFormData(j).MyForm.Text <> sender.ToString

        Next j
    End Sub

    Public Sub MySubTabControl_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)

        'Clicking on a sub-tab in the annotation button interface part of the main display screen will either
        'change display based on tab selected (left mouse click) or will display the AnnotationInterfaceConfigure form
        '(right mouse click)...

        If e.Button = MouseButtons.Right Then

            AnnotationInterfaceConfigure.Show()
            AnnotationInterfaceConfigure.BringToFront()

            AnnotationInterfaceConfigure.ListBox2.Visible = True
            AnnotationInterfaceConfigure.Button2.Visible = True

            For x = 0 To AnnotationInterfaceConfigure.ListBox2.Items.Count - 1
                If AnnotationInterfaceConfigure.ListBox2.Items(x).ToString = MySubTabControl.TabPages(MySubTabControl.SelectedIndex).Text Then
                    AnnotationInterfaceConfigure.ListBox2.SetSelected(x, True)
                End If
            Next


        Else

            WhichTabAmIOn = MySubTabControl.SelectedIndex

        End If

    End Sub

    Public Sub MyMainTabControl_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        ' Respond to right-click on the main-tab for additional actions
        If e.Button = MouseButtons.Right Then
            AnnotationInterfaceConfigure.Show()
            AnnotationInterfaceConfigure.BringToFront()

            ' Hide certain controls on the configuration interface
            AnnotationInterfaceConfigure.ListBox2.Visible = False
            AnnotationInterfaceConfigure.Button2.Visible = False
            AnnotationInterfaceConfigure.Label2.Visible = False

            ' Ensure SelectedIndex is valid before accessing TabPages
            If MyMainTabControl.SelectedIndex >= 0 AndAlso MyMainTabControl.SelectedIndex < MyMainTabControl.TabPages.Count Then
                For x = 0 To AnnotationInterfaceConfigure.ListBox1.Items.Count - 1
                    If AnnotationInterfaceConfigure.ListBox1.Items(x).ToString = MyMainTabControl.TabPages(MyMainTabControl.SelectedIndex).Text Then
                        AnnotationInterfaceConfigure.ListBox1.SetSelected(x, True)
                    End If
                Next
            End If
        End If
    End Sub

    Public Sub SizeAnnotationButtons(subTabs As Dictionary(Of String, DataDictionarySingleton.SubTab))
        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        If MyMainTabControl Is Nothing Then
            Throw New InvalidOperationException("Main tab control is not initialized.")
        End If

        ' Clear existing tabs
        MyMainTabControl.TabPages.Clear()

        ' Populate tabs and buttons
        For Each subTab In From subTabEntry In subTabs Select subTabEntry.Value
            Dim tabPage As New TabPage With {.Text = subTab.TabName, .BackColor = Color.Transparent}

            ' Button positioning
            Dim topOffset As Integer = DataDictionarySingleton.DefaultButtonTop
            Dim leftOffset As Integer = DataDictionarySingleton.DefaultButtonLeft

            For Each eventButton In subTab.EventButtons
                Dim button As New Button With {
                        .Text = eventButton.ButtonName,
                        .Width = DataDictionarySingleton.DefaultButtonWidth,
                        .Height = DataDictionarySingleton.DefaultButtonHeight,
                        .Left = leftOffset,
                        .Top = topOffset,
                        .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                        .FlatStyle = FlatStyle.System
                        }
                button.FlatAppearance.BorderColor = Color.DarkBlue
                button.FlatAppearance.BorderSize = 2
                button.FlatAppearance.MouseOverBackColor = Color.LightBlue
                button.FlatAppearance.MouseDownBackColor = Color.LightGray

                AddHandler button.Click, AddressOf Button_Click
                tabPage.Controls.Add(button)

                ' Adjust positions for the next button
                leftOffset += button.Width + DataDictionarySingleton.HorizButtonSpacing
                If leftOffset + button.Width > MyMainTabControl.Width Then
                    leftOffset = DataDictionarySingleton.DefaultButtonLeft
                    topOffset += button.Height + DataDictionarySingleton.VertButtonSpacing
                End If
            Next

            MyMainTabControl.TabPages.Add(tabPage)
        Next

        If MyMainTabControl.TabPages.Count > 0 Then
            MyMainTabControl.SelectedIndex = 0
        End If

        ' Refresh
        MyMainTabControl.Invalidate()
        MyMainTabControl.Update()
    End Sub

    Public Sub InitializeAndSetupMainTabControl()
        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        ' Ensure main tab control is initialized
        If MyMainTabControl Is Nothing Then
            Dim calculatedTabWidth As Integer = DataDictionarySingleton.DynamicTabWidth
            MyMainTabControl = New TabControl() With {
            .Location = New Point(MainTabLeft, MainTabTop),
            .Size = New Size(_mainTabWidth, _mainTabHeight),
            .Visible = True,
            .Multiline = True,
            .ItemSize = New Size(calculatedTabWidth, 50),
            .Font = New Font("Segoe UI", 12, FontStyle.Bold)
        }

            ' Add mainTabControl to the form
            If Not OnVehicleScreen.Controls.Contains(MyMainTabControl) Then
                OnVehicleScreen.Controls.Add(MyMainTabControl)
            End If
        End If

        ' Configure additional properties
        MyMainTabControl.Appearance = TabAppearance.FlatButtons
        MyMainTabControl.ItemSize = New Size(75, 50)
        MyMainTabControl.SizeMode = TabSizeMode.Normal
        MyMainTabControl.DrawMode = TabDrawMode.Normal

        ' Attach event handlers
        AddHandler MyMainTabControl.DrawItem, AddressOf DrawTab
        'AddHandler MyMainTabControl.MouseDown, AddressOf MyMainTabControl_MouseDown

        ' Refresh control
        MyMainTabControl.Invalidate()
        MyMainTabControl.Update()
    End Sub

    Private Sub DrawTab(sender As Object, e As DrawItemEventArgs)
        Dim g As Graphics = e.Graphics
        Dim tabPage As TabPage = MyMainTabControl.TabPages(e.Index)
        Dim tabBounds As Rectangle = MyMainTabControl.GetTabRect(e.Index)

        tabBounds.Inflate(-1, -1)

        ' Fill the tab background
        If e.State = DrawItemState.Selected Then
            g.FillRectangle(Brushes.LightBlue, tabBounds)
        Else
            g.FillRectangle(Brushes.LightGray, tabBounds)
        End If

        ' Draw border
        Using borderPen As New Pen(Color.Black, 2)
            borderPen.DashStyle = Drawing2D.DashStyle.Solid
            g.DrawRectangle(borderPen, tabBounds)
        End Using

        ' Draw the tab text
        Dim stringFlags As New StringFormat With {
            .Alignment = StringAlignment.Center,
            .LineAlignment = StringAlignment.Center
        }

        g.DrawString(tabPage.Text, MyMainTabControl.Font, Brushes.Black, tabBounds, stringFlags)
    End Sub

    Public Sub ButtonContainers_Buttons_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)

        'ButtonContainers_Buttons are the annotation buttons on the main CLEVIR screen.  These buttons are created dynamically
        'at initialization based on the contents of the datadictionary.csv file...

        'Left button click writes event into INCA data, if we are recording, Right button click opens up the
        'AnnotationInterfaceConfiguration window...

        If e.Button = MouseButtons.Left Then
            HandleAnnotationButtons(sender)
        Else
            If sender.text <> "ANNOTATION" Then
                AnnotationInterfaceConfigure.Show()
                AnnotationInterfaceConfigure.BringToFront()
            End If
        End If

    End Sub

End Class
