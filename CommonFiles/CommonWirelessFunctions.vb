Option Strict Off

Imports System.IO
Imports System.Net.NetworkInformation
Module CommonWirelessFunctions

    'This module contains common functions related to Wireless connection between PC and GM Network.  It is located in the GlobalCommonFiles folder
    'and is used by multiple applications...

    Public SaveNetworkAdapterDescription As String
    Public WirelessUnavailable As Boolean
    Public GMLANConnectionUnavailable As Boolean
    Public InhibitWirelessRadioButton As Boolean
    Public InhibitGmLanRadioButton As Boolean
    Public CheckForNewerSoftwareAndFiles As Boolean

    Public Function CheckForValidWirelessConnection(Optional ByRef AttemptedReEnable As Boolean = False) As Boolean

        'Called from HandleWirelessConnection on initialization and when the Wireless radio button is selected from the UploadDataScreen...

        'Here we check to see if a valid wireless connection can be found. CheckForValidWirelessConnection will set 
        'SaveNetworkAdapterDescription.  SaveNetworkAdapterDescription will either be set to the current active wireless
        'network description, and CheckForValidWirelessConnection will return True, or if no active wireless network
        'can be found, CheckForValidWirelessConnection will return false.  If NetworkAdapterDescription read from the config.xml
        'file is set to "NONE" and CheckForValidWirelessConnection returns false, then SaveNetworkAdapterDescription will also
        'be set to "NONE".  This option is avialable so that the user can bypass checking for a valid network connection when not in the
        'vacinity of the GM Network, which reduces init time.  If set to "NONE" and we are in range, we will still try to enable the
        'wireless connection if we can.  If NetworkAdapterDescription read from the config.xml file is a network description and not set
        'to "NONE" we will always try to enable the connection on start up, which takes a while if no connection is available...

        Dim networkinterfaces() As NetworkInterface
        Dim FoundWirelessInterface As Boolean

        'Here we check for valid wireless network connection, if one exists, we write the name into SaveNetworkAdapterDescription,
        'This allows us to make sure that the configuration contains the correct wireless network name if there is a wireless connection
        'available.

        'NOTE: If NetworkAdapterDesciption ="NONE" in the config.xml file, we will not write the networkinterfaces(x).Name to SaveNetworkAdapterDescription
        '"NONE" is a special setting which will allow us to bypass trying to connect, thus saving time when operating outside of the
        'range of the wireless or when operating without a wireless device...

        'Also, if we don't find a valid connection, this may mean that the wireless adapter exists, but is disabled.  In this case, if
        'NetworkAdapterDescription is not set to "NONE" we will try to enable it in HandleWirelessConnection...

        networkinterfaces = NetworkInterface.GetAllNetworkInterfaces()

        If Not networkinterfaces Is Nothing Then
            HandleUserMessageLogging("GMRC", "CheckForValidWirelessConnection: Number of network interfaces found = " & UBound(networkinterfaces))
            For x = 0 To UBound(networkinterfaces)
                'Behavior is different depending on whether we are running on Windows 7 or Windows 10 PC...
                'networkinterfaces(x).

                If (InStr(My.Computer.Info.OSFullName, "7") > 0 And InStr(networkinterfaces(x).Name, "Wireless Network Connection") > 0 And InStr(networkinterfaces(x).Name, "*") = 0) _
                    Or (InStr(My.Computer.Info.OSFullName, "10") > 0 And InStr(networkinterfaces(x).Name, "Wi-Fi") > 0) Then

                    If networkinterfaces(x).Description = NetworkAdapterDescription Then
                        HandleUserMessageLogging("GMRC", "CheckForValidWirelessConnection: " & networkinterfaces(x).Description & " found")
                        'SaveNetworkAdapterDescription = networkinterfaces(x).Description
                    End If
                    SaveNetworkAdapterDescription = networkinterfaces(x).Description

                    If networkinterfaces(x).OperationalStatus = OperationalStatus.Up Then
                        SaveNetworkAdapterDescription = networkinterfaces(x).Description
                        FoundWirelessInterface = True
                        HandleUserMessageLogging("GMRC", "CheckForValidWirelessConnection: " & networkinterfaces(x).Description & " is up. FoundWirelessInterface = True")
                        Exit For

                    End If

                End If
            Next

        Else
            HandleUserMessageLogging("GMRC", "CheckForValidWirelessConnection: " & My.Computer.Info.OSFullName & " - no networkinterfaces found...")
        End If

        If FoundWirelessInterface = False Then
            HandleUserMessageLogging("GMRC", "CheckForValidWirelessConnection: No connected wireless interfaces found...")
            If Len(SaveNetworkAdapterDescription) = 0 Then
                HandleUserMessageLogging("GMRC", "CheckForValidWirelessConnection: Wireless Disabled - Setting SaveNetworkAdapterDescription to " & NetworkAdapterDescription & "...")
                SaveNetworkAdapterDescription = NetworkAdapterDescription
            Else
                HandleUserMessageLogging("GMRC", "CheckForValidWirelessConnection: Wireless Enabled but Not Connected...")
            End If
        End If

        'If we find a valid connected wireless adapter, we will write the networkinterfaces(x).Description into NetworkAdapterDescription so the correct
        'name is saved back to the config.xml file on exit.  However we will only do this if NetworkAdapterDescription is not already set to NONE or GM_LAN
        'We do this in case the NetworkAdapterDescription in the config.xml file is incorrect as it may have come from the default config.xml file which might
        'not be the same as the actual computer on which CLEVIR is installed...
        If NetworkAdapterDescription <> "NONE" And SaveNetworkAdapterDescription <> "NONE" And NetworkAdapterDescription <> "GM_LAN" And SaveNetworkAdapterDescription <> "GM_LAN" And Len(SaveNetworkAdapterDescription) > 0 Then
            NetworkAdapterDescription = SaveNetworkAdapterDescription
            If FoundWirelessInterface = False Then

                HandleUserMessageLogging("GMRC", "CheckForValidWirelessConnection: Disabling and ReEnabling Wireless...")
                If DisableWirelessNetworkConnection() = True Then
                    FoundWirelessInterface = EnableWirelessNetworkConnection()
                    AttemptedReEnable = True
                End If

            End If
        End If

        CheckForValidWirelessConnection = FoundWirelessInterface

    End Function

    Public Function EnableWirelessNetworkConnection() As Boolean

        'Called from HandleWirelessConnection and CheckForValidWirelessConnection, also Called after data is uploaded, on exit from the upload screen, or when Wireless radio button is
        'selected from the upload data window.  Tries to enable the wireless connection...

        Dim NetworkAdapterInfoStatus As Boolean

        Dim mySaveTime As DateTime
        Dim myElapseTime As TimeSpan

        Dim objWMIService As Object
        Dim colitems As Object
        Dim strstatus As String = ""

        Const WIRELESS_ENABLE_DELAY_TIME = 30

        Try

            HandleUserMessageLogging("GMRC", "EnableWirelessNetworkConnection Called...")

            EnableWirelessNetworkConnection = False

            objWMIService = GetObject("winmgmts:\\" & "." & "\root\cimv2")

            If Not objWMIService Is Nothing Then

                HandleUserMessageLogging("GMRC", "EnableWirelessNetworkConnection: Valid return for GetObject")

                'Query status of the currently recognized (SaveNetworkAdapterDescription) Network Adapter...

                colitems = objWMIService.ExecQuery _
                ("Select * from Win32_NetworkAdapter Where Name = '" & SaveNetworkAdapterDescription & "'")

                For Each objItem In colitems

                    If Not IsDBNull(objItem.NetConnectionStatus) Then

                        NetworkAdapterInfoStatus = True

                        Select Case objItem.NetConnectionStatus
                            Case 0
                                strstatus = "Disconnected"
                            Case 1
                                strstatus = "Connecting"
                            Case 2
                                strstatus = "Connected"
                            Case 3
                                strstatus = "Disconnecting"
                            Case 4
                                strstatus = "Hardware not present"
                            Case 5
                                strstatus = "Hardware disabled"
                            Case 6
                                strstatus = "Hardware malfunction"
                            Case 7
                                strstatus = "Media disconnected"
                            Case 8
                                strstatus = "Authenticating"
                            Case 9
                                strstatus = "Authentication succeeded"
                            Case 10
                                strstatus = "Authentication failed"
                            Case 11
                                strstatus = "Invalid address"
                            Case 12
                                strstatus = "Credentials required"
                            Case Else
                                strstatus = CStr(objItem.NetConnectionStatus)
                        End Select

                        If objItem.NetEnabled = False Then

                            HandleUserMessageLogging("GMRC", "EnableWirelessNetworkConnection: Verifying Connection to Network Drive, Please wait...",,, FlashMsgOn)

                            'If device is disabled, we will check to see if we can see the network drive because we may already be connected via GM_LAN
                            'If we cant find the network drive, we will try to enable the wireless adapter...
                            If Not Directory.Exists(NetworkDriveMapping) Then

                                HandleUserMessageLogging("GMRC", "EnableWirelessNetworkConnection: Network Connection not found.  ReEnabling Wireless Adapter, Please wait...",,, FlashMsgOn)

                                objItem.Enable()

                            Else 'Directory found...
                                HandleUserMessageLogging("GMRC", "EnableWirelessNetworkConnection: Network Drive Verified, using Existing GM LAN Connection...",,, FlashMsg1Sec)
                                EnableWirelessNetworkConnection = True
                                WirelessUnavailable = True
                                GMLANConnectionUnavailable = False

                                'We will exit here because there is nothing else to do, we have a valid connection to the network via GM_LAN...
                                Exit Function
                            End If

                        Else 'Wireless Enabled...

                        End If

                        Exit For

                    Else 'Adapter description not found...
                        NetworkAdapterInfoStatus = False
                    End If

                Next

            Else 'Invalid return for GetObject...
                NetworkAdapterInfoStatus = False
            End If

            'If we have successfully enabled the wireless adapter we will verify that we can find the network drive...
            If NetworkAdapterInfoStatus = True Then

                HandleUserMessageLogging("GMRC", "Verifying Connection to Network Drive, Please wait...",,, FlashMsgOn)

                mySaveTime = DateTime.Now

                Do While Not Directory.Exists(NetworkDriveMapping) And myElapseTime.Seconds <= WIRELESS_ENABLE_DELAY_TIME

                    myElapseTime = DateTime.Now.Subtract(mySaveTime)
                    Threading.Thread.Sleep(1000)

                Loop

                If myElapseTime.Seconds <= WIRELESS_ENABLE_DELAY_TIME Then

                    EnableWirelessNetworkConnection = True
                    WirelessUnavailable = False
                    HandleUserMessageLogging("GMRC", "EnableWirelessNetworkConnection: Wireless connection verified...",,, FlashMsg1Sec)

                Else 'Enable delay time exceeded...
                    HandleUserMessageLogging("GMRC", "EnableWirelessNetworkConnection: Could not find " & NetworkDriveMapping & " no Data Upload Capability at this time.  You may still use CLEVIR to record data...",,, FlashMsg3Sec)
                    EnableWirelessNetworkConnection = False
                    WirelessUnavailable = True
                    GMLANConnectionUnavailable = True
                End If

            Else 'NetworkAdapterInfoStatus = False
                HandleUserMessageLogging("GMRC", "EnableWirelessNetworkConnection: Network Adapter Information Unavailable, no Data Upload Capability at this time.  You may still use CLEVIR to record data...",,, FlashMsg3Sec)
                EnableWirelessNetworkConnection = False
                WirelessUnavailable = True

            End If

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "EnableWirelessNetworkConnection: - " & ex.Message)
            EnableWirelessNetworkConnection = False
            WirelessUnavailable = True
            GMLANConnectionUnavailable = True

        Finally

            UserStatusInfo.Hide()

        End Try

    End Function

    Public Function DisableWirelessNetworkConnection(Optional ByVal VerifyNetworkConnection As Boolean = False) As Boolean

        'Called when the GM LAN radio button is pressed on the upload data window and from CheckForValidWirelessConnection.
        'Disables the Wireless Adapter...

        Dim objWMIService As Object = Nothing
        Dim colItems As Object = Nothing
        Dim strStatus As String = "Undetermined"
        Dim adapterFound As Boolean = False
        DisableWirelessNetworkConnection = False

        Try
            HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection: Called...")
            DisableWirelessNetworkConnection = False
            HandleUserMessageLogging("GMRC", "Disabling Wireless Connection, " & SaveNetworkAdapterDescription & " please wait...",,, FlashMsgOn)
            objWMIService = GetObject("winmgmts:\\.\root\cimv2")
            If objWMIService IsNot Nothing Then
                HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection: Valid return for GetObject")
                colItems = objWMIService.ExecQuery("Select * from Win32_NetworkAdapter Where Name = '" & SaveNetworkAdapterDescription & "'")
                For Each objItem In colItems
                    If Not IsDBNull(objItem.NetConnectionStatus) Then
                        adapterFound = True
                        HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection: Found " & SaveNetworkAdapterDescription)
                        strStatus = GetNetConnectionStatusDescription(objItem.NetConnectionStatus)
                        HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection: Net Connection Status: " & strStatus)
                        HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection: Unconditionally Disabling Wireless Adapter...")
                        objItem.Disable()
                        HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection: Wireless Adapter Disabled...")
                        DisableWirelessNetworkConnection = True
                        Exit For
                    Else
                        HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection: No connection status returned for " & SaveNetworkAdapterDescription)
                    End If
                Next

                If Not adapterFound Then
                    HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection: Adapter " & SaveNetworkAdapterDescription & " not found.")
                End If

            Else
                HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection: Network Adapter Information Unavailable...")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "DisableWirelessNetworkConnection - " & ex.Message)
        Finally

            UserStatusInfo.Hide()
            ' Release COM objects
            If colItems IsNot Nothing Then System.Runtime.InteropServices.Marshal.ReleaseComObject(colItems)
            If objWMIService IsNot Nothing Then System.Runtime.InteropServices.Marshal.ReleaseComObject(objWMIService)
        End Try
        Return DisableWirelessNetworkConnection
    End Function

    ' Helper function to get the connection status description
    Private Function GetNetConnectionStatusDescription(ByVal statusCode As Integer) As String
        Select Case statusCode
            Case 0 : Return "Disconnected"
            Case 1 : Return "Connecting"
            Case 2 : Return "Connected"
            Case 3 : Return "Disconnecting"
            Case 4 : Return "Hardware not present"
            Case 5 : Return "Hardware disabled"
            Case 6 : Return "Hardware malfunction"
            Case 7 : Return "Media disconnected"
            Case 8 : Return "Authenticating"
            Case 9 : Return "Authentication succeeded"
            Case 10 : Return "Authentication failed"
            Case 11 : Return "Invalid address"
            Case 12 : Return "Credentials required"
            Case Else : Return "Unknown status (" & statusCode.ToString() & ")"
        End Select
    End Function

    Public Function HandleWirelessConnection() As Boolean
        ' Checks wireless connection on initialization. If connected, checks for new CLEVIR version and updated support files.
        ' Calls CheckForValidWirelessConnection.
        Dim strStatus As String = ""
        Dim attemptedReEnable As Boolean
        Try
            HandleUserMessageLogging("GMRC", "HandleWirelessConnection: Called...")
            HandleWirelessConnection = True
            WirelessUnavailable = True
            If UsingFlashDrive Then
                CheckForNewerSoftwareAndFiles = True
                Return True
            End If
            If NetworkAdapterDescription = "GM_LAN" Then
                ' Handle GM_LAN-specific logic
                HandleUserMessageLogging("GMRC", "HandleWirelessConnection: Verifying Connection to Network Drive, Please wait...",,, FlashMsgOn)
                If Directory.Exists(NetworkDriveMapping) Then
                    HandleUserMessageLogging("GMRC", "HandleWirelessConnection: Network connection verified...")
                    CheckForNewerSoftwareAndFiles = True
                Else
                    HandleUserMessageLogging("GMRC", $"Could not find {NetworkDriveMapping}, no Data Upload Capability at this time. You may still use CLEVIR to record data...", UserStatusInfoTimeSec:=FlashMsg2Sec)
                End If
                UserStatusInfo.Hide()
                Return True
            End If
            ' Check for a valid wireless connection
            If Not CheckForValidWirelessConnection(attemptedReEnable) Then
                If SaveNetworkAdapterDescription <> "NONE" Then
                    If Not attemptedReEnable AndAlso EnableWirelessNetworkConnection() Then
                        CheckForNewerSoftwareAndFiles = True
                        WirelessUnavailable = False
                    Else
                        WirelessUnavailable = True
                    End If
                Else
                    HandleUserMessageLogging("GMRC", "HandleWirelessConnection: Verifying Connection to Network Drive, Please wait...",,, FlashMsgOn)
                    If Directory.Exists(NetworkDriveMapping) Then
                        HandleUserMessageLogging("GMRC", "HandleWirelessConnection: Network connection verified...")
                        CheckForNewerSoftwareAndFiles = True
                    Else
                        HandleUserMessageLogging("GMRC", $"HandleWirelessConnection: Could not find {NetworkDriveMapping}, no Data Upload Capability at this time. You may still use CLEVIR to record data...",,, FlashMsg2Sec)
                    End If
                    UserStatusInfo.Hide()
                End If
            Else
                ' Valid wireless connection found
                If Not attemptedReEnable Then
                    HandleUserMessageLogging("GMRC", "HandleWirelessConnection: Verifying Connection to Network Drive, Please wait...",,, FlashMsgOn)
                    If Directory.Exists(NetworkDriveMapping) Then
                        WirelessUnavailable = False
                        HandleUserMessageLogging("GMRC", "HandleWirelessConnection: Network connection verified...")
                        CheckForNewerSoftwareAndFiles = True
                    Else
                        HandleUserMessageLogging("GMRC", $"HandleWirelessConnection: Could not find {NetworkDriveMapping}, no Data Upload Capability at this time. You may still use CLEVIR to record data...",,, FlashMsg2Sec)
                    End If
                    UserStatusInfo.Hide()
                Else
                    CheckForNewerSoftwareAndFiles = True
                    WirelessUnavailable = False
                End If
            End If
            ' Check for newer software versions if applicable
            'If Not PATAC AndAlso CheckForNewerSoftwareAndFiles Then
            'CheckForNewerSoftwareVersions()
            'End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HandleWirelessConnection - {ex.Message}")
            HandleUserMessageLogging("GMRC", "Unable to Connect to Wireless, no Data Upload Capability at this time. You may still use CLEVIR to record data...", FlashMsg2Sec)
            Return False
        End Try
        Return HandleWirelessConnection
    End Function


End Module
