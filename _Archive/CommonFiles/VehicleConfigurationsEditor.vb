Option Strict Off
Option Explicit On


Imports de.etas.cebra.toolAPI.Inca
Imports de.etas.cebra.toolAPI.Common

Imports System.Threading
Imports System.Runtime.InteropServices

Public Class VehicleConfigurationsEditor

    'This form is displayed if the user selects VEHICLE NUMBER NOT IN LIST on InitForm after indicating they wish to change the
    'vehicle number at startup. Allows the user to configure a new vehicle number.  This involves entering a vehicle number,
    'selecting vehicle type (CSAV2, LowContent, HighContent, etc.), number of cameras, camera names, etc.  Information is saved
    'to local VehicleConfigurations.csv file and some information is also copied to Share drive.

    Private _labelSelected As Label
    Private _selected592 As Boolean
    Private _selected523 As Boolean
    Private _selected886 As Boolean
    'Private INCAWorkspacePath As String
    'Private TemplateWorkspaceName As String

    Private _zipRecordedFiles As String
    Private _autoCameraChange As Boolean
    Private _selectedVehicleType As String
    Private Sub HandleVehicleTypeSelection()

        HandleUserMessageLogging("GMRC", "VehicleConfigurationsEditor ComboBox1 Selected Value Changed: User Selected " & ComboBox1.Text)

        _selectedVehicleType = ComboBox1.Text

        _autoCameraChange = False

        Label16.Text = "NA"
        Label17.Text = "NA"
        Label18.Text = "NA"
        Label19.Text = "NA"
        Label20.Text = "NA"
        Label21.Text = "NA"

        Label5.Text = "NA"
        Label6.Text = "NA"
        Label7.Text = "NA"
        Label8.Text = "NA"
        Label9.Text = "NA"
        Label10.Text = "NA"
        Label28.Text = "NA"
        Label29.Text = "NA"

        'If ComboBox1.Text <> "LowContent" And InStr(ComboBox1.Text, "FCM") = 0 Then
        If InStr(ComboBox1.Text, "LowContent") = 0 And InStr(ComboBox1.Text, "FCM_") = 0 Then
            If Label22.Text = "523" Or Label22.Text = "NA" Then
                Label22.Text = ""
            End If
            If Label23.Text = "523" Or Label23.Text = "NA" Then
                Label23.Text = ""
            End If
            If Label24.Text = "523" Or Label24.Text = "NA" Then
                Label24.Text = ""
            End If
            If Label27.Text = "523" Or Label27.Text = "NA" Then
                Label27.Text = ""
            End If
        End If

        ComboBox3.Items.Clear()
        ComboBox3.Text = ""

        Label22.Enabled = True
        Label23.Enabled = True
        Label24.Enabled = True
        Label27.Enabled = True

        Select Case ComboBox1.Text
            'HC CHANGE
            'Case "HighContent"
            Case "HighContent No FCM"

                ComboBox3.Text = "2"

                Label22.Text = "523"
                Label23.Text = "523"
                Label24.Text = "523"
                Label27.Text = "523"

                Label22.Enabled = False
                Label23.Enabled = False
                Label24.Enabled = False
                Label27.Enabled = False

                _autoCameraChange = True

                ComboBox2.Text = "1"
                Label5.Text = "Front"

            Case "CSAV2"
                ComboBox3.Text = "6"

                ComboBox2.Text = "1"
                Label5.Text = "Front"

            Case "CSAV2 Primary Only"
                ComboBox3.Text = "3"

                ComboBox2.Text = "1"
                Label5.Text = "Front"
            'Case "LowContent"
            Case "LowContent No FCM"
                ComboBox3.Text = "1"

                Label22.Text = "523"
                Label23.Text = "523"
                Label24.Text = "523"
                Label27.Text = "523"

                Label22.Enabled = False
                Label23.Enabled = False
                Label24.Enabled = False
                Label27.Enabled = False

                _autoCameraChange = True

                ComboBox2.Text = 1
                Label5.Text = "Front"

            Case "ACP3 No FCM", "ACP3_MCU"

                ComboBox3.Text = "1"

                Label22.Text = "523"
                Label23.Text = "523"
                Label24.Text = "523"
                Label27.Text = "523"

                'Change for ACP3 886 Functionality...

                'Label22.Enabled = False
                'Label23.Enabled = False
                'Label24.Enabled = False
                'Label27.Enabled = False

                _autoCameraChange = True

                ComboBox2.Text = 1
                Label5.Text = "Front"

            Case "ACP2_MCU No CAN", "ACP3_MCU No CAN", "ACP4_MCU No CAN"

                ComboBox3.Text = "1"

                Label22.Text = "NA"
                Label23.Text = "NA"
                Label24.Text = "NA"
                Label27.Text = "NA"

                Label22.Enabled = False
                Label23.Enabled = False
                Label24.Enabled = False
                Label27.Enabled = False

                _autoCameraChange = True

                ComboBox2.Text = 1
                Label5.Text = "Front"


            Case "ACP3 w ZF1 FCM", "ACP3 w ZF1 FCM100", "ACP3 w VEO FCM100"

                ComboBox3.Text = "2"

                Label22.Text = "523"
                Label23.Text = "523"
                Label24.Text = "523"
                Label27.Text = "523"

                'Label22.Enabled = False
                'Label23.Enabled = False
                'Label24.Enabled = False
                'Label27.Enabled = False
                _autoCameraChange = True

                ComboBox2.Text = 1
                Label5.Text = "Front"

            Case "ACP2_MCU"
                'TBD
                ComboBox3.Text = "1"

                Label22.Text = "NA"
                Label23.Text = "523"
                Label24.Text = "523"
                Label27.Text = "NA"

                Label22.Enabled = False
                'Label23.Enabled = False
                'Label24.Enabled = False
                Label27.Enabled = False

                _autoCameraChange = True

                ComboBox2.Text = 1
                Label5.Text = "Front"

            Case "ACP4_MCU"

                ComboBox3.Text = "1"

                Label22.Text = "523"
                Label23.Text = "523"
                Label24.Text = "NA"
                Label27.Text = "523"

                Button4.Visible = True

                'Change for RTK Functionality...

                'Label22.Enabled = False
                'Label23.Enabled = False
                Label24.Enabled = False
                'Label27.Enabled = False

                _autoCameraChange = True

                'ComboBox2.Text = 1
                ComboBox2.Text = 0
                'Label5.Text = "Front"

                Label12.Text = "CAN Mon 1"
                Label13.Text = "CAN Mon 2"
                Label15.Text = "CAN Mon 3"
                Label14.Text = "CAN Mon 4"

            Case "FCM_STA_ZF1", "FCM_STA_VEO", "FCM100_STA_ZF1", "FCM100_STA_VEO"

                ComboBox3.Text = "1"

                Label22.Text = "523"
                Label23.Text = "523"
                Label24.Text = "523"
                Label27.Text = "523"

                Label22.Enabled = False
                Label23.Enabled = False
                Label24.Enabled = False
                Label27.Enabled = False

                _autoCameraChange = True

                ComboBox2.Text = 1
                Label5.Text = "Front"

            Case "LowContent w ZF1 FCM", "LowContent w ZF1 FCM100", "LowContent w VEO FCM100"

                ComboBox3.Text = "2"

                Label22.Text = "523"
                Label23.Text = "523"
                Label24.Text = "523"
                Label27.Text = "523"

                Label22.Enabled = False
                Label23.Enabled = False
                Label24.Enabled = False
                Label27.Enabled = False

                _autoCameraChange = True

                ComboBox2.Text = 1
                Label5.Text = "Front"

            Case "HighContent w ZF1 FCM", "HighContent w ZF1 FCM100", "HighContent w VEO FCM100"

                ComboBox3.Text = "3"

                Label22.Text = "523"
                Label23.Text = "523"
                Label24.Text = "523"
                Label27.Text = "523"

                Label22.Enabled = False
                Label23.Enabled = False
                Label24.Enabled = False
                Label27.Enabled = False

                _autoCameraChange = True

                ComboBox2.Text = 1
                Label5.Text = "Front"

        End Select

    End Sub

    Private Function SaveVehicleConfigChanges() As Boolean

        'Tied to a button on the VehicleConfigurationsEditor form.  Saves the information that the user entered in the VehicleConfigurationsEditor
        'to the vehicleconfigurations.csv file.  This his how the user creates a new vehicle configuration for use by CLEVIR. The information entered
        'lets CLEVIR know how the instrumentation is set up for the vehicle based on vehcile number.  This configuration information is copied to the
        'share drive, so that it can be available to any in-vehicle PC, so any PC can be used with any vehicle by updating the vehicle information
        'available from the share drive...

        Dim templateWorkspaceName As String = ""

        Dim fnum As Integer
        Dim saveString As String
        Dim processors As String
        Dim cameras As String
        Dim canMon As String = ""
        Dim exiting As Boolean
        Dim textline As String
        Dim overwrite As Integer
        Dim saveRows() As String
        Dim x As Integer
        Dim tempstr As String
        Dim filename As String

        ReDim saveRows(0)

        Me.Cursor = Cursors.WaitCursor

        SaveVehicleConfigChanges = False

        'First we make sure that all pertinent information has been entered before we save...

        If Len(TextBox1.Text) = 0 Then
            MsgBox("Please enter a vehicle number.")
            exiting = True
        End If

        If Len(ComboBox1.Text) = 0 Then
            MsgBox("Please enter a vehicle type.")
            exiting = True
        End If

        If Len(ComboBox2.Text) = 0 Then
            MsgBox("Please enter the number of cameras and associated camera names or 0 for no cameras.")
            exiting = True
        End If

        If Len(ComboBox3.Text) = 0 Then
            MsgBox("Please enter the number of processors.")
            exiting = True
        End If

        If Len(Label22.Text) = 0 Then
            MsgBox("Please enter ETAS hardware for CAN Mon 1.")
            exiting = True
        End If

        If Len(Label23.Text) = 0 Then
            MsgBox("Please enter ETAS hardware for CAN Mon 2.")
            exiting = True
        End If

        If Len(Label24.Text) = 0 Then
            MsgBox("Please enter ETAS hardware for CAN Mon 3.")
            exiting = True
        End If

        If Len(Label27.Text) = 0 Then
            MsgBox("Please enter ETAS hardware for CAN Mon 4.")
            exiting = True
        End If
        If exiting = True Then
            Me.Cursor = Cursors.Arrow
            HandleUserMessageLogging("GMRC", "SaveVehicleConfigChanges: Exiting, incomplete information...")
            Exit Function
        End If

        'Now we begin setting up the string variables that will be used to build the save string
        'based on what the user has set in the various input boxes, etc. on the VehicleConfigurationsEditor form...

        ClevirFilesPath = "Current"

        If CheckBox3.Checked = True Then
            _zipRecordedFiles = "TRUE"
        Else
            _zipRecordedFiles = "FALSE"
        End If

        Select Case ComboBox1.Text

            Case "HighContent No FCM"
                DataUploadPath = "HighContent\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\HighContent"
                FcmConfigName = ""
                templateWorkspaceName = "HC_2P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "LowContent No FCM"
                DataUploadPath = "LowContent\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\LowContent"
                FcmConfigName = ""
                templateWorkspaceName = "LC_1P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "CSAV2", "CSAV2 Primary Only"

                DataUploadPath = ""
                FcmConfigName = ""
                If ComboBox3.Text = "0" Then
                    templateWorkspaceName = "CSAV2_0P"
                End If
                If ComboBox3.Text = "3" Then
                    ClevirFilesPath = ClevirFilesPath & "\PrimaryEOCMOnly"
                    templateWorkspaceName = "CSAV2_3P"
                End If
                If ComboBox3.Text = "6" Then
                    templateWorkspaceName = "CSAV2_3P3R"
                End If
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & "NA"

                'FCM CHANGE - Added FCM Cases, not sure of naming yet. Right now we handle the names used in the combobox as well as other names, so this works for now...

            Case "FCM_STA_ZF1", "FCM_STA_VEO"
                DataUploadPath = "FCM\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\FCM\" & ComboBox1.Text
                FcmConfigName = ComboBox1.Text
                templateWorkspaceName = "FCM_1P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "FCM100_STA_ZF1", "FCM100_STA_VEO"
                DataUploadPath = "FCM\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\FCM\" & ComboBox1.Text
                FcmConfigName = ComboBox1.Text
                templateWorkspaceName = "FCM100_1P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "LowContent w ZF1 FCM"
                DataUploadPath = "LowContent\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\LowContent"
                FcmConfigName = "FCM_LCM_ZF1"
                templateWorkspaceName = "LC_FCM_2P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "HighContent w ZF1 FCM"
                DataUploadPath = "HighContent\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\HighContent"
                FcmConfigName = "FCM_LCH_ZF1"
                templateWorkspaceName = "HC_FCM_3P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "LowContent w ZF1 FCM100", "LowContent w VEO FCM100"
                DataUploadPath = "LowContent\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\LowContent"
                If InStr(ComboBox1.Text, "ZF1") > 0 Then
                    FcmConfigName = "FCM100_LC_ZF1"
                Else
                    FcmConfigName = "FCM100_LC_VEO"
                End If

                templateWorkspaceName = "LC_FCM100_2P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "HighContent w ZF1 FCM100", "HighContent w VEO FCM100"
                DataUploadPath = "HighContent\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\HighContent"
                If InStr(ComboBox1.Text, "ZF1") > 0 Then
                    FcmConfigName = "FCM100_LC_ZF1"
                Else
                    FcmConfigName = "FCM100_LC_VEO"
                End If
                templateWorkspaceName = "HC_FCM100_3P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "ACP2_MCU"
                DataUploadPath = "ACP2\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\ACP2"
                FcmConfigName = ""
                templateWorkspaceName = "ACP2_1P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "ACP2_MCU No CAN"
                DataUploadPath = "ACP2\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\ACP2"
                FcmConfigName = ""
                templateWorkspaceName = "ACP2_NOCAN_1P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "ACP3 No FCM", "ACP3_MCU"
                DataUploadPath = "ACP3\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\ACP3"
                FcmConfigName = ""
                templateWorkspaceName = "ACP3_1P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "ACP3_MCU No CAN"
                DataUploadPath = "ACP3\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\ACP3"
                FcmConfigName = ""
                templateWorkspaceName = "ACP3_NOCAN_1P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

            Case "ACP3 w ZF1 FCM"
                DataUploadPath = "ACP3\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\ACP3"
                FcmConfigName = "FCM_LCH_ZF1"
                templateWorkspaceName = "ACP3_FCM_2P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text
            Case "ACP3 w ZF1 FCM100", "ACP3 w VEO FCM100"
                DataUploadPath = "ACP3\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\ACP3"
                If InStr(ComboBox1.Text, "ZF1") > 0 Then
                    FcmConfigName = "FCM100_LC_ZF1"
                Else
                    FcmConfigName = "FCM100_LC_VEO"
                End If
                templateWorkspaceName = "ACP3_FCM100_2P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text
            Case "ACP4_MCU"
                DataUploadPath = "ACP4\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\ACP4"
                FcmConfigName = ""
                templateWorkspaceName = "ACP4_1P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

                If InStr(canMon, "RTK") > 0 Then
                    templateWorkspaceName = "ACP4_RTK_1P"
                End If

            Case "ACP4_MCU No CAN"
                DataUploadPath = "ACP4\VehicleData\"
                ClevirFilesPath = ClevirFilesPath & "\ACP4"
                FcmConfigName = ""
                templateWorkspaceName = "ACP4_NOCAN_1P"
                canMon = Label22.Text & "," & Label23.Text & "," & Label24.Text & "," & Label27.Text

        End Select

        If _selected592 = True Then
            templateWorkspaceName = templateWorkspaceName & "592"
        ElseIf _selected523 = True And instr(ComboBox1.Text, "CSAV2") > 0 Then
            templateWorkspaceName = templateWorkspaceName & "523"
        ElseIf _selected886 = True And (InStr(ComboBox1.text, "ACP2") > 0 Or InStr(ComboBox1.text, "ACP3") > 0 Or InStr(ComboBox1.text, "ACP4") > 0) Then
            templateWorkspaceName = templateWorkspaceName & "886"
        End If

        processors = Label16.Text & "," & Label17.Text & "," & Label18.Text & "," & Label19.Text & "," & Label20.Text & "," & Label21.Text

        If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\vehicleconfigurationsNF.csv") Then
            filename = My.Application.Info.DirectoryPath & "\vehicleconfigurationsNF.csv"
            cameras = Label5.Text & "," & Label6.Text & "," & Label7.Text & "," & Label8.Text & "," & Label9.Text & "," & Label10.Text & "," & Label28.Text & "," & Label29.Text
        Else
            filename = My.Application.Info.DirectoryPath & "\vehicleconfigurations.csv"
            cameras = Label5.Text & "," & Label6.Text & "," & Label7.Text & "," & Label8.Text & "," & Label9.Text & "," & Label10.Text
        End If

        saveString = TextBox1.Text & "," & TextBox1.Text & "," & processors & "," & cameras & "," & canMon & "," & DataUploadPath & "," & ClevirFilesPath & "," & _zipRecordedFiles & "," & FcmConfigName & ","

        If HandleVehicleConfigurationsFile(filename) = False Then
            Me.Close()
            End
        End If

        'Here we will check if an entry in the vehicleconfigurations.csv file already exists with the same vehicle name
        'if so, we will prompt the user if they want to reconfigure the same vehicle name...

        fnum = FreeFile()

        FileOpen(fnum, filename, OpenMode.Input)

        x = 0
        Do While Not EOF(fnum)
            textline = LineInput(fnum)
            ReDim Preserve saveRows(x)
            saveRows(x) = textline
            tempstr = UCase(Mid(textline, 1, InStr(textline, ",") - 1))

            If tempstr = UCase(TextBox1.Text) Then

                HandleUserMessageLogging("GMRC", "Vehicle Number already exists in " & System.IO.Path.GetFileName(filename) & " file. Reconfigure with updated information?")
                overwrite = MsgBox("Vehicle Number already exists in " & System.IO.Path.GetFileName(filename) & " file. Reconfigure with updated information?", vbYesNo)

                'If the user decides that they do not want to overwrite the new information to an existing vehicle, we will exit
                'Otherwise we will continue through the file and save all info to SaveRows...
                If overwrite = vbNo Then
                    HandleUserMessageLogging("GMRC", "User Answered No...")
                    FileClose(fnum)
                    SaveVehicleConfigChanges = False
                    Me.Cursor = Cursors.Arrow
                    Exit Function
                End If
            End If
            x = x + 1
        Loop
        FileClose(fnum)

        'If we make it to here, and the user wants to overwrite, we will re-create the file and include
        'all rows except the modified row for the specified vehicle number. 
        If overwrite = vbYes Then
            HandleUserMessageLogging("GMRC", "User Answered Yes...")
            FileOpen(fnum, filename, OpenMode.Output)
            For x = 0 To UBound(saveRows)
                If UCase(Mid(saveRows(x), 1, InStr(saveRows(x), ",") - 1)) <> UCase(TextBox1.Text) Then
                    PrintLine(fnum, saveRows(x))
                End If
            Next
            FileClose(fnum)
        End If

        'Here we append the new vehicle information into the vehicleconfigurations.csv file, which will either be
        'an entry for a new vehicle or the modified entry for the existing vehicle number...

        FileOpen(fnum, filename, OpenMode.Append)

        'CTF11,CTF11,IP,K1P,K2P,NA,NA,NA,FRONT,HMI,LEFTSIDE,RIGHTSIDE,DRIVER,FOOTWELL,595,593,595,,Current\PrimaryEOCMOnly,INCA Project Files\CTF11\current,TRUE,VAL_3P6C,4
        PrintLine(fnum, saveString)

        FileClose(fnum)

        'need to add vehicle directory to Q:" & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\INCA Project Files directory
        'need to add subdirectory TemplateWorkspace to vehicle directory

        InitForm.SaveVehicleNumber(TextBox1.Text)

        HandleUserMessageLogging("GMRC", System.IO.Path.GetFileName(filename) & " file has been updated.  vehicleconfig.txt VehicleNumber has been changed to " & TextBox1.Text & ".", DisplayMsgBox)

        'CopyVehicleConfigToCLEVIRFolder(filename)

        'ReadVehicleConfigsFile() 'don't need to call this here, it is called in SaveVehicleNumber...

        'If AddNewWorkspaceFromTemplate(TemplateWorkspaceName) = True Then
        If AddNewWorkspaceFromTemplate(templateWorkspaceName) = True Then
            HandleUserMessageLogging("GMRC", "PC has just been configured as " & ComboBox1.Text,, )
        Else
            HandleUserMessageLogging("GMRC", "Invalid Configuration. Changes not saved for " & ComboBox1.Text,, )
        End If

        SaveVehicleConfigChanges = True
        CheckForNewerSignalListComplete = False
        CurrentVehicleUsage = "DEVELOPMENT"
        Me.Cursor = Cursors.Arrow
    End Function

    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged

    End Sub

    Private Sub ComboBox1_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedValueChanged

        ListBox3.Visible = False
        ListBox2.Visible = False
        ListBox1.Visible = False
        Button4.Visible = False

        If ComboBox1.SelectedIndex > -1 Then

            If InStr(ComboBox1.SelectedItem.ToString, " w ") > 0 Then
                HandleUserMessageLogging("GMRC", "VehicleConfigurationsEditor Vehicle Type Selection " & ComboBox1.SelectedItem.ToString & " is not currently supported.", DisplayMsgBox)
                ComboBox1.SelectedIndex = -1
                'ComboBox1.Text = ""
                Exit Sub
            End If


            HandleVehicleTypeSelection()

        End If


    End Sub

    Private Sub ComboBox2_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox2.SelectedIndexChanged

    End Sub

    Private Sub ComboBox2_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ComboBox2.SelectedValueChanged

        If _autoCameraChange = False Then

            Label5.Text = "NA"
            Label6.Text = "NA"
            Label7.Text = "NA"
            Label8.Text = "NA"
            Label9.Text = "NA"
            Label10.Text = "NA"
            Label28.Text = "NA"
            Label29.Text = "NA"

            Select Case ComboBox2.Text
                Case "1"
                    Label5.Text = ""
                Case "2"
                    Label5.Text = ""
                    Label6.Text = ""
                Case "3"
                    Label5.Text = ""
                    Label6.Text = ""
                    Label7.Text = ""
                Case "4"
                    Label5.Text = ""
                    Label6.Text = ""
                    Label7.Text = ""
                    Label8.Text = ""
                Case "5"
                    Label5.Text = ""
                    Label6.Text = ""
                    Label7.Text = ""
                    Label8.Text = ""
                    Label9.Text = ""
                Case "6"
                    Label5.Text = ""
                    Label6.Text = ""
                    Label7.Text = ""
                    Label8.Text = ""
                    Label9.Text = ""
                    Label10.Text = ""
                Case "7"
                    Label5.Text = ""
                    Label6.Text = ""
                    Label7.Text = ""
                    Label8.Text = ""
                    Label9.Text = ""
                    Label10.Text = ""
                    Label28.Text = ""
                Case "8"
                    Label5.Text = ""
                    Label6.Text = ""
                    Label7.Text = ""
                    Label8.Text = ""
                    Label9.Text = ""
                    Label10.Text = ""
                    Label28.Text = ""
                    Label29.Text = ""
            End Select

        Else
            Select Case ComboBox2.Text
                Case "0"
                    Label5.Text = "NA"
                    Label6.Text = "NA"
                    Label7.Text = "NA"
                    Label8.Text = "NA"
                    Label9.Text = "NA"
                    Label10.Text = "NA"
                    Label28.Text = "NA"
                    Label29.Text = "NA"
                Case "1"
                    Label5.Text = "Front"
                    Label6.Text = "NA"
                    Label7.Text = "NA"
                    Label8.Text = "NA"
                    Label9.Text = "NA"
                    Label10.Text = "NA"
                    Label28.Text = "NA"
                    Label29.Text = "NA"
                Case "2"
                    Label5.Text = "Front"
                    Label6.Text = "Driver"
                    Label7.Text = "NA"
                    Label8.Text = "NA"
                    Label9.Text = "NA"
                    Label10.Text = "NA"
                    Label28.Text = "NA"
                    Label29.Text = "NA"
                Case "3"
                    Label5.Text = "Front"
                    Label6.Text = "Left"
                    Label7.Text = "Right"
                    Label8.Text = "NA"
                    Label9.Text = "NA"
                    Label10.Text = "NA"
                    Label28.Text = "NA"
                    Label29.Text = "NA"
            End Select

            'AutoCameraChange = False
        End If

        _labelSelected = Label5

    End Sub

    Private Sub ComboBox3_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox3.SelectedIndexChanged

    End Sub

    Private Sub ComboBox3_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ComboBox3.SelectedValueChanged


    End Sub

    Private Sub Label5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label5.Click

        If Not _labelSelected Is Nothing Then
            If _labelSelected.Text <> "Define New" Then

                ListBox2.Visible = False
                _labelSelected = Label5

                ListBox1.Top = Label5.Top
                ListBox1.Left = Label5.Left + Label5.Width
                ListBox1.Visible = True

            Else
                _labelSelected = Label5
            End If
        End If

    End Sub

    Private Sub Label6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label6.Click
        If Not _labelSelected Is Nothing Then
            If _labelSelected.Text <> "Define New" Then

                ListBox2.Visible = False
                _labelSelected = Label6
                ListBox1.Top = Label6.Top
                ListBox1.Left = Label6.Left + Label6.Width
                ListBox1.Visible = True

            Else
                _labelSelected = Label6
            End If

        End If
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ListBox1_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedValueChanged

        If ListBox1.Visible = True Then
            ListBox1.Visible = False
            If ListBox1.SelectedItem.ToString <> "Define New" Then
                _labelSelected.Text = ListBox1.SelectedItem.ToString
            Else
                _labelSelected.Text = "Define New"
                GroupBox1.Visible = True
            End If
        End If


    End Sub

    Private Sub Label7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label7.Click
        If Not _labelSelected Is Nothing Then
            If _labelSelected.Text <> "Define New" Then

                ListBox2.Visible = False
                _labelSelected = Label7
                ListBox1.Top = Label7.Top
                ListBox1.Left = Label7.Left + Label7.Width
                ListBox1.Visible = True

            Else
                _labelSelected = Label7
            End If
        End If

    End Sub

    Private Sub Label8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label8.Click

        If Not _labelSelected Is Nothing Then
            If _labelSelected.Text <> "Define New" Then

                ListBox2.Visible = False
                _labelSelected = Label8
                ListBox1.Top = Label8.Top
                ListBox1.Left = Label8.Left + Label8.Width
                ListBox1.Visible = True

            Else
                _labelSelected = Label8
            End If
        End If
    End Sub

    Private Sub Label9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label9.Click

        If Not _labelSelected Is Nothing Then

            If _labelSelected.Text <> "Define New" Then

                ListBox2.Visible = False
                _labelSelected = Label9
                ListBox1.Top = Label9.Top
                ListBox1.Left = Label9.Left + Label9.Width
                ListBox1.Visible = True

            Else
                _labelSelected = Label9
            End If

        End If

    End Sub

    Private Sub Label10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label10.Click

        If Not _labelSelected Is Nothing Then

            If _labelSelected.Text <> "Define New" Then

                ListBox2.Visible = False
                _labelSelected = Label10
                ListBox1.Top = Label10.Top
                ListBox1.Left = Label10.Left + Label10.Width
                ListBox1.Visible = True

            Else
                _labelSelected = Label10

            End If

        End If

    End Sub

    Private Sub Label22_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label22.Click

        If Len(ComboBox1.Text) > 0 Then
            ListBox1.Visible = False
            _labelSelected = Label22

            ListBox2.Top = Label22.Top
            ListBox2.Left = Label22.Left + Label22.Width
            ListBox2.Visible = True
        Else
            MsgBox("Please select Vehicle Type before selecting CAN Monitoring information.")
        End If

    End Sub

    Private Sub Label23_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label23.Click

        If Len(ComboBox1.Text) > 0 Then
            ListBox1.Visible = False
            _labelSelected = Label23

            ListBox2.Top = Label23.Top
            ListBox2.Left = Label23.Left + Label23.Width
            ListBox2.Visible = True
        Else
            MsgBox("Please select Vehicle Type before selecting CAN Monitoring information.")
        End If
    End Sub

    Private Sub Label24_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label24.Click
        If Len(ComboBox1.Text) > 0 Then

            ListBox1.Visible = False
            _labelSelected = Label24

            ListBox2.Top = Label24.Top
            ListBox2.Left = Label24.Left + Label24.Width
            ListBox2.Visible = True
        Else
            MsgBox("Please select Vehicle Type before selecting CAN Monitoring information.")
        End If
    End Sub

    Private Sub Label27_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label27.Click
        If Len(ComboBox1.Text) > 0 Then
            ListBox1.Visible = False
            _labelSelected = Label27

            ListBox2.Top = Label27.Top
            ListBox2.Left = Label27.Left + Label27.Width
            ListBox2.Visible = True
        Else
            MsgBox("Please select Vehicle Type before selecting CAN Monitoring information.")
        End If
    End Sub

    Private Sub ListBox2_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox2.SelectedIndexChanged

    End Sub

    Private Sub ListBox2_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ListBox2.SelectedValueChanged

        ListBox2.Visible = False

        'If using a 523 or an 886, the assumption is that you would only be using one or the other and would not be using both
        'so all CAN Channels are set to either one or the other.  Any selection other than 523 or 886 allows you to choose individual
        'blue boxes for each CAN Channel as was done for CSAV2...

        If ListBox2.SelectedItem.ToString = "523" Or ListBox2.SelectedItem.ToString = "886" Then

            If InStr(_selectedVehicleType, "ACP2") = 0 And InStr(_selectedVehicleType, "ACP4") = 0 Then
                Label22.Text = ListBox2.SelectedItem.ToString
                Label23.Text = ListBox2.SelectedItem.ToString
                Label24.Text = ListBox2.SelectedItem.ToString
                Label27.Text = ListBox2.SelectedItem.ToString
            ElseIf InStr(_selectedVehicleType, "ACP2") > 0 Then
                Label22.Text = "NA"
                Label23.Text = ListBox2.SelectedItem.ToString
                Label24.Text = ListBox2.SelectedItem.ToString
                Label27.Text = "NA"
            ElseIf InStr(_selectedVehicleType, "ACP4") > 0 Then

                Label22.Text = ListBox2.SelectedItem.ToString
                Label23.Text = ListBox2.SelectedItem.ToString
                Label24.Text = "NA"
                Label27.Text = ListBox2.SelectedItem.ToString

            End If

        Else
            _labelSelected.Text = ListBox2.SelectedItem.ToString
        End If


    End Sub

    Private Sub Label22_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label22.TextChanged

        If Label22.Text = "592" Then
            _selected592 = True
        Else
            If Label23.Text <> "592" And Label24.Text <> "592" And Label27.Text <> "592" Then
                _selected592 = False
            End If
        End If

        If Label22.Text = "523" Then
            _selected523 = True
        Else
            If Label23.Text <> "523" And Label24.Text <> "523" And Label27.Text <> "523" Then
                _selected523 = False
            End If
        End If

        If Label22.Text = "886" Then
            _selected886 = True
        Else
            If Label23.Text <> "886" And Label24.Text <> "886" And Label27.Text <> "886" Then
                _selected886 = False
            End If
        End If

    End Sub

    Private Sub Label23_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label23.TextChanged

        If Label23.Text = "592" Then
            _selected592 = True
        Else
            If Label22.Text <> "592" And Label24.Text <> "592" And Label27.Text <> "592" Then
                _selected592 = False
            End If
        End If

        If Label23.Text = "886" Then
            _selected886 = True
        Else
            If Label22.Text <> "886" And Label24.Text <> "886" And Label27.Text <> "886" Then
                _selected886 = False
            End If
        End If


    End Sub

    Private Sub Label24_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label24.TextChanged

        If Label24.Text = "592" Then
            _selected592 = True
        Else
            If Label22.Text <> "592" And Label23.Text <> "592" And Label27.Text <> "592" Then
                _selected592 = False
            End If
        End If

        If Label24.Text = "886" Then
            _selected886 = True
        Else
            If Label22.Text <> "886" And Label23.Text <> "886" And Label27.Text <> "886" Then
                _selected886 = False
            End If
        End If
    End Sub

    Private Sub Label27_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label27.TextChanged

        If Label27.Text = "592" Then
            _selected592 = True
        Else
            If Label22.Text <> "592" And Label24.Text <> "592" And Label23.Text <> "592" Then
                _selected592 = False
            End If
        End If

        If Label27.Text = "886" Then
            _selected886 = True
        Else
            If Label22.Text <> "886" And Label24.Text <> "886" And Label23.Text <> "886" Then
                _selected886 = False
            End If
        End If
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged


    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        'Save...
        HandleUserMessageLogging("GMRC", "VehicleConfigurationsEditor Button1 Click: Save New Vehicle Information")
        SaveVehicleConfigChanges()


    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        'Save and exit...
        HandleUserMessageLogging("GMRC", "VehicleConfigurationsEditor Button2 Click: Save and Exit")
        If SaveVehicleConfigChanges() = True Then
            Me.Close()
        End If


    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click

        HandleUserMessageLogging("GMRC", "VehicleConfigurationsEditor Button3 Click: Save and Exit")
        Me.Close()
    End Sub

    Private Sub ComboBox3_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ComboBox3.TextChanged

        'FCM CHANGE - Added FCM Cases...

        Label16.Text = "NA"
        Label17.Text = "NA"
        Label18.Text = "NA"
        Label19.Text = "NA"
        Label20.Text = "NA"
        Label21.Text = "NA"

        Select Case ComboBox3.Text
            Case "0"

            Case "1"
                If ComboBox1.Text = "LowContent No FCM" Then
                    Label16.Text = "XETK:1"

                ElseIf InStr(ComboBox1.Text, "FCM_") > 0 Then

                    Label16.Text = "FCM"

                ElseIf InStr(ComboBox1.Text, "FCM100_") > 0 Then

                    Label16.Text = "FCM100"

                ElseIf ComboBox1.Text = "ACP2_MCU" Or ComboBox1.Text = "ACP2_MCU No CAN" Then

                    Label16.Text = "ACP2_MCU"

                ElseIf ComboBox1.Text = "ACP4_MCU" Or ComboBox1.Text = "ACP4_MCU No CAN" Then

                    Label16.Text = "ACP4_MCU"

                ElseIf ComboBox1.Text = "ACP3 No FCM" Or ComboBox1.Text = "ACP3_MCU" Or ComboBox1.Text = "ACP3_MCU No CAN" Then
                    Label16.Text = "ACP3_MCU"
                Else
                    ComboBox3.Text = ""
                    HandleUserMessageLogging("GMRC", "ComboBox3_TextChanged Invalid Entry", DisplayMsgBox)
                End If

                Label16.Visible = True

            Case "2"

                If ComboBox1.Text = "HighContent No FCM" Then

                    Label16.Text = "HCF"
                    Label17.Text = "HCS"

                ElseIf ComboBox1.Text = "LowContent w ZF1 FCM" Then

                    Label16.Text = "XETK:1"
                    Label17.Text = "FCM"

                ElseIf ComboBox1.Text = "LowContent w ZF1 FCM100" Or ComboBox1.Text = "LowContent w VEO FCM100" Then

                    Label16.Text = "XETK:1"
                    Label17.Text = "FCM100"

                ElseIf ComboBox1.Text = "ACP3 w ZF1 FCM" Or ComboBox1.Text = "ACP3 w VEO FCM" Then
                    Label16.Text = "ACP3_MCU"
                    Label17.Text = "FCM"

                ElseIf ComboBox1.Text = "ACP3 w ZF1 FCM100" Or ComboBox1.Text = "ACP3 w VEO FCM100" Then
                    Label16.Text = "ACP3_MCU"
                    Label17.Text = "FCM100"
                Else
                    ComboBox3.Text = ""

                    HandleUserMessageLogging("GMRC", "VehicleConfigurationsEditor: ComboBox3_TextChanged - Invalid Entry", DisplayMsgBox)
                End If

                Label16.Visible = True
                Label17.Visible = True

            Case "3"

                If ComboBox1.Text = "HighContent w ZF1 FCM" Then
                    Label16.Text = "HCF"
                    Label17.Text = "HCS"
                    Label18.Text = "FCM"
                ElseIf ComboBox1.Text = "HighContent w ZF1 FCM100" Or ComboBox1.Text = "HighContent w VEO FCM100" Then
                    Label16.Text = "HCF"
                    Label17.Text = "HCS"
                    Label18.Text = "FCM100"
                ElseIf ComboBox1.Text = "CSAV2 Primary Only" Then
                    Label16.Text = "IP"
                    Label17.Text = "K1P"
                    Label18.Text = "K2P"
                Else
                    ComboBox3.Text = ""
                    HandleUserMessageLogging("GMRC", "VehicleConfigurationsEditor: ComboBox3_TextChanged - Invalid Entry", DisplayMsgBox)
                End If

                Label16.Visible = True
                Label17.Visible = True
                Label18.Visible = True

            Case "6"

                Label16.Text = "IP"
                Label17.Text = "K1P"
                Label18.Text = "K2P"
                Label19.Text = "IR"
                Label20.Text = "K1R"
                Label21.Text = "K2R"

                Label16.Visible = True
                Label17.Visible = True
                Label18.Visible = True
                Label19.Visible = True
                Label20.Visible = True
                Label21.Visible = True

        End Select
    End Sub

    Private Sub VehicleConfigurationsEditor_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed

    End Sub

    Private Sub VehicleConfigurationsEditor_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'Copy the modified ONVIFSystemConfigurationDialog.exe.config file (which contains the camera names that are commonly used
        'with the CLEVIR setup) to the appropriate directory.  There is one directory for INCA 7.2 and a different one for INCA 7.3.
        'On EDWS computers, the "C:\Program Files (x86)\Common Files\ETAS\ETASShared12\Devices\Video" folder is protected so the copy
        'does not work and will throw and error.

        'Note to self - Try Robocopy here or perhaps make a new app that just copies files and can be launched as a separate
        'process and elevated with runas???

        Dim x As Integer

        Try

            If InStr(My.Application.Info.AssemblyName, "7_2") > 0 Then

                If System.IO.Directory.Exists("C:\Program Files (x86)\Common Files\ETAS\ETASShared12\Devices\Video") Then
                    If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\ONVIFSystemConfigurationDialog.exe.config") Then
                        If System.IO.File.GetLastWriteTime(My.Application.Info.DirectoryPath & "\ONVIFSystemConfigurationDialog.exe.config") <> System.IO.File.GetLastWriteTime("C:\Program Files (x86)\Common Files\ETAS\ETASShared12\Devices\Video\ONVIFSystemConfigurationDialog.exe.config") Then
                            'System.IO.File.Copy(My.Application.Info.DirectoryPath & "\ONVIFSystemConfigurationDialog.exe.config", "C:\Program Files (x86)\Common Files\ETAS\ETASShared12\Devices\Video\ONVIFSystemConfigurationDialog.exe.config", True)
                            RoboCopyFile(My.Application.Info.DirectoryPath & "\ONVIFSystemConfigurationDialog.exe.config", "C:\Program Files (x86)\Common Files\ETAS\ETASShared12\Devices\Video", True)
                        End If
                    End If
                End If

            ElseIf InStr(My.Application.Info.AssemblyName, "7_3") > 0 Then

                If System.IO.Directory.Exists("C:\Program Files\Common Files\ETAS\ETASShared13\Devices\Video") Then
                    If System.IO.File.Exists(My.Application.Info.DirectoryPath & "\ONVIFSystemConfigurationDialog.exe.config") Then
                        If System.IO.File.GetLastWriteTime(My.Application.Info.DirectoryPath & "\ONVIFSystemConfigurationDialog.exe.config") <> System.IO.File.GetLastWriteTime("C:\Program Files\Common Files\ETAS\ETASShared13\Devices\Video\ONVIFSystemConfigurationDialog.exe.config") Then
                            'System.IO.File.Copy(My.Application.Info.DirectoryPath & "\ONVIFSystemConfigurationDialog.exe.config", "C:\Program Files\Common Files\ETAS\ETASShared13\Devices\Video\ONVIFSystemConfigurationDialog.exe.config", True)
                            RoboCopyFile(My.Application.Info.DirectoryPath & "\ONVIFSystemConfigurationDialog.exe.config", "C:\Program Files\Common Files\ETAS\ETASShared13\Devices\Video", True)
                        End If
                    End If
                End If
            End If

            ComboBox2.Items.Clear()

            If MaxCameras = 6 Then
                Label28.Visible = False
                Label29.Visible = False
            End If

            For x = 0 To MaxCameras
                ComboBox2.Items.Add(x)
            Next

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "VehicleConfigurationsEditor_Load: " & ex.Message)
        End Try

    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles TextBox2.TextChanged
        _labelSelected.Text = TextBox2.Text
    End Sub

    Private Sub TextBox2_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox2.KeyDown
        If e.KeyCode = Keys.Enter Then
            GroupBox1.Visible = False
        End If
    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged

    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged
        HandleUserMessageLogging("GMRC", "Zip Recorded Files Checked Changed - " & CheckBox3.Checked.ToString)
    End Sub

    Private Sub Label16_Click(sender As Object, e As EventArgs) Handles Label16.Click

    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        If Button4.Text = "Add RTK" Then

            If InStr(Label27.Text, "NA") = 0 Then
                If InStr(Label27.Text, "RTK") = 0 Then
                    Button4.Text = "NO RTK"
                    Label27.Text = Mid(Label27.Text, 1, 3) & " RTK"
                End If
            End If
        Else
            Button4.Text = "Add RTK"
            If InStr(Label27.Text, "RTK") > 0 Then
                Label27.Text = Mid(Label27.Text, 1, 3)
            End If
        End If

        Exit Sub

        'Do not allow user to select which CAN channel for RTK, CLEVIR must have it fixed at CAN 4
        'so only one RTK template per software version is necessary.  No way to automate mapping of files to CAN channels using API
        'so RTK .dbc file must be fixed to a default channel which has been agreed on to be CAN 4 for ACP4...
        '-----------------------------------------------------------------------

        ListBox3.Items.Clear()
        ListBox3.Visible = False

        If InStr(Label22.Text, "NA") = 0 Then
            ListBox3.Items.Add("CAN Mon 1")
        End If
        If InStr(Label23.Text, "NA") = 0 Then
            ListBox3.Items.Add("CAN Mon 2")
        End If
        If InStr(Label24.Text, "NA") = 0 Then
            ListBox3.Items.Add("CAN Mon 3")
        End If
        If InStr(Label27.Text, "NA") = 0 Then
            ListBox3.Items.Add("CAN Mon 4")
        End If

        Select Case ListBox3.Items.Count
            Case 0
                MsgBox("No CAN Channels Defined.  Please set the ETAS Device for at least one CAN Channel prior to setting up for RTK")
            Case 1
                Select Case ListBox3.Items(0).ToString
                    Case "CAN Mon 1"
                        Label22.Text = "523 RTK"
                    Case "CAN Mon 2"
                        Label23.Text = "523 RTK"
                    Case "CAN Mon 3"
                        Label24.Text = "523 RTK"
                    Case "CAN Mon 4"
                        Label27.Text = "523 RTK"
                End Select
            Case Else
                ListBox3.Items.Add("No RTK")
                ListBox3.Visible = True
        End Select

    End Sub

    Private Sub ListBox3_SelectedIndexChanged_2(sender As Object, e As EventArgs) Handles ListBox3.SelectedIndexChanged

        'This is no longer applicable, we do not  display listbox3 anymore when Buttom4 (Add RTK) is pressed on vehicleconfigurationseditor form...

        If ListBox3.SelectedIndex >= 0 Then

            If InStr(Label22.Text, " RTK") > 0 Then
                Label22.Text = Mid(Label22.Text, 1, Len(Label22.Text) - 4)
            End If
            If InStr(Label23.Text, " RTK") > 0 Then
                Label23.Text = Mid(Label23.Text, 1, Len(Label23.Text) - 4)
            End If
            If InStr(Label24.Text, " RTK") > 0 Then
                Label24.Text = Mid(Label24.Text, 1, Len(Label24.Text) - 4)
            End If
            If InStr(Label27.Text, " RTK") > 0 Then
                Label27.Text = Mid(Label27.Text, 1, Len(Label27.Text) - 4)
            End If

            Select Case ListBox3.Items(ListBox3.SelectedIndex).ToString
                Case "CAN Mon 1"
                    If InStr(Label22.Text, "RTK") = 0 And InStr(Label22.Text, "NA") = 0 Then
                        Label22.Text = Label22.Text & " RTK"
                    End If
                Case "CAN Mon 2"
                    If InStr(Label23.Text, "RTK") = 0 And InStr(Label23.Text, "NA") = 0 Then
                        Label23.Text = Label23.Text & " RTK"
                    End If
                Case "CAN Mon 3"
                    If InStr(Label24.Text, "RTK") = 0 And InStr(Label24.Text, "NA") = 0 Then
                        Label24.Text = Label24.Text & " RTK"
                    End If
                Case "CAN Mon 4"
                    If InStr(Label27.Text, "RTK") = 0 And InStr(Label27.Text, "NA") = 0 Then
                        Label27.Text = Label27.Text & " RTK"
                    End If

                Case Else
                    ListBox3.Visible = False
            End Select

            'If InStr(Label22.Text, "RTK") > 0 Or InStr(Label23.Text, "RTK") > 0 Or InStr(Label24.Text, "RTK") > 0 Then
            'If MsgBox("ACP4 vehicles typically connect RTK to CAN 4.  If you want to configure a different CAN channel for RTK you must associate the ARXML or DBC files to each CAN channel manually. Continue with this selection?", vbYesNo) = vbYes Then
            'HandleUserMessageLogging("GMRC", "ACP4 vehicles typically connect RTK to CAN 4.  If you want to configure a different CAN channel for RTK you must associate the ARXML or DBC files to each CAN channel manually. User selected yes continue")
            'MsgBox("CLEVIR automation does not support using a CAN Channel other than CAN 4 for an RTK.")
            'Else
            'ListBox3.SelectedIndex = -1
            'End If

            'End If

        End If

    End Sub

    Private Sub Label28_Click(sender As Object, e As EventArgs) Handles Label28.Click

        If Not _labelSelected Is Nothing Then
            If _labelSelected.Text <> "Define New" Then

                ListBox2.Visible = False
                _labelSelected = Label28

                ListBox1.Top = Label28.Top
                ListBox1.Left = Label28.Left + Label28.Width
                ListBox1.Visible = True

            Else
                _labelSelected = Label28
            End If
        End If

    End Sub

    Private Sub Label29_Click(sender As Object, e As EventArgs) Handles Label29.Click

        If Not _labelSelected Is Nothing Then
            If _labelSelected.Text <> "Define New" Then

                ListBox2.Visible = False
                _labelSelected = Label29

                ListBox1.Top = Label29.Top
                ListBox1.Left = Label29.Left + Label29.Width
                ListBox1.Visible = True

            Else
                _labelSelected = Label29
            End If
        End If

    End Sub

    Private Sub VehicleConfigurationsEditor_Click(sender As Object, e As EventArgs) Handles Me.Click
        ListBox1.Visible = False
    End Sub
End Class