Option Strict On
Imports System.IO

''' <summary>
''' Module for managing OXTS NCOM packet capture alongside LiDAR
''' Provides synchronized ground truth pose data for sensor fusion validation
''' </summary>
Module OxtsNcomPcapCapture

    ' ====================================================================
    ' Global State
    ' ====================================================================
    Public OxtsCaptureEnabled As Boolean = False
    Public OxtsCaptureStarted As Boolean = False
    Public OxtsCaptureDevice As OxtsNcomCaptureDevice = Nothing

    ' Configuration (loaded from XML)
    Public OxtsNetworkAdapterGuid As String = ""
    Public OxtsIpAddress As String = "10.5.55.200"
    Public OxtsNcomPort As UShort = 3000

    ''' <summary>
    ''' Starts OXTS NCOM packet capture to PCAP file
    ''' Called from INCA_InterfaceClass.StartRecording()
    ''' </summary>
    Public Sub StartOxtsCapture()
        Try
            If Not OxtsCaptureEnabled Then
                HandleUserMessageLogging("GMRC", "OXTS capture is disabled in configuration")
                Return
            End If

            If OxtsCaptureStarted Then
                HandleUserMessageLogging("GMRC", "OXTS capture already running")
                Return
            End If

            ' ================================================================
            ' Build PCAP filename
            ' ================================================================
            Dim currentActiveSequence As String = GetCurrentActiveSequence()
            If String.IsNullOrEmpty(currentActiveSequence) Then
                HandleUserMessageLogging("GMRC", "StartOxtsCapture: Cannot determine current recording sequence")
                Return
            End If

            Dim sequenceNumber As Integer = GetSequenceNumberFromFileName(currentActiveSequence)
            Dim baseName As String = Path.GetFileNameWithoutExtension(currentActiveSequence)
            baseName = baseName.Substring(0, baseName.LastIndexOf("_")) ' Remove sequence suffix

            Dim pcapFilename As String = Path.Combine(
                FinalPathToSaveData,
                $"{baseName}_{sequenceNumber:D2}_OXTS.pcap"
            )

            ' ================================================================
            ' Initialize capture device (if not already created)
            ' ================================================================
            If OxtsCaptureDevice Is Nothing Then
                ' Use same network adapter as LiDAR (or separate if configured)
                Dim adapterGuid As String = OxtsNetworkAdapterGuid
                If String.IsNullOrEmpty(adapterGuid) AndAlso LidarDevices.Count > 0 Then
                    ' Fallback: Use first LiDAR's adapter
                    adapterGuid = LidarDevices(0).LidarAdapterGuid
                End If

                If String.IsNullOrEmpty(adapterGuid) Then
                    HandleUserMessageLogging("GMRC", "StartOxtsCapture: Network adapter not configured", DisplayMsgBox)
                    Return
                End If

                OxtsCaptureDevice = New OxtsNcomCaptureDevice(
                    adapterGuid,
                    OxtsIpAddress,
                    OxtsNcomPort,
                    "OXTS_NCOM"
                )
            End If

            ' ================================================================
            ' Start capture
            ' ================================================================
            HandleUserMessageLogging("GMRC", $"Starting OXTS NCOM capture: {Path.GetFileName(pcapFilename)}")
            OxtsCaptureDevice.StartCapture(pcapFilename, sequenceNumber)

            If OxtsCaptureDevice.IsCapturing Then
                OxtsCaptureStarted = True
                HandleUserMessageLogging("GMRC", "✅ OXTS NCOM capture started")
                StatusNotifier.Toast("OXTS NCOM capture started", ToastKind.Info, "OXTS", 3000, True)
            Else
                HandleUserMessageLogging("GMRC", "⚠️ OXTS NCOM capture failed to start")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"StartOxtsCapture: {ex.Message}", DisplayMsgBox)
            OxtsCaptureStarted = False
        End Try
    End Sub

    ''' <summary>
    ''' Stops OXTS NCOM packet capture
    ''' Called from INCA_InterfaceClass.StopRecording()
    ''' </summary>
    Public Sub StopOxtsCapture()
        Try
            If Not OxtsCaptureStarted Then
                Return
            End If

            HandleUserMessageLogging("GMRC", "Stopping OXTS NCOM capture...")

            If OxtsCaptureDevice IsNot Nothing Then
                OxtsCaptureDevice.StopCapture()
            End If

            OxtsCaptureStarted = False
            HandleUserMessageLogging("GMRC", "✅ OXTS NCOM capture stopped")
            StatusNotifier.Toast("OXTS NCOM capture stopped", ToastKind.Info, "OXTS", 3000, True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"StopOxtsCapture: {ex.Message}")
            OxtsCaptureStarted = False
        End Try
    End Sub

    ''' <summary>
    ''' Injects an event marker into the OXTS PCAP stream
    ''' </summary>
    Public Sub InjectOxtsEventMarker(eventType As String, message As String, Optional sequenceNumber As Integer = 0)
        Try
            If OxtsCaptureStarted AndAlso OxtsCaptureDevice IsNot Nothing Then
                OxtsCaptureDevice.InjectEventMarker(eventType, message, sequenceNumber)
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"InjectOxtsEventMarker: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gets OXTS capture statistics
    ''' </summary>
    Public Function GetOxtsCaptureStatistics() As String
        If OxtsCaptureDevice Is Nothing Then
            Return "OXTS capture not initialized"
        End If

        Return OxtsCaptureDevice.GetStatistics()
    End Function

    ''' <summary>
    ''' Loads OXTS capture configuration from XML
    ''' Called from GM_ResidentClient.ReadConfiguration()
    ''' </summary>
    Public Sub LoadOxtsCaptureConfig(root As Xml.XmlNode)
        Try
            ' Check if OXTS capture is enabled
            Dim captureEnabledNode = root.SelectSingleNode("OxtsCaptureEnabled")
            If captureEnabledNode IsNot Nothing Then
                OxtsCaptureEnabled = Boolean.Parse(captureEnabledNode.InnerText)
            End If

            ' Load OXTS network configuration
            Dim oxtsConfigNode = root.SelectSingleNode("OxtsCapture")
            If oxtsConfigNode IsNot Nothing Then
                Dim adapterNode = oxtsConfigNode.SelectSingleNode("NetworkAdapterGuid")
                If adapterNode IsNot Nothing Then
                    OxtsNetworkAdapterGuid = adapterNode.InnerText
                End If

                Dim ipNode = oxtsConfigNode.SelectSingleNode("IpAddress")
                If ipNode IsNot Nothing Then
                    OxtsIpAddress = ipNode.InnerText
                End If

                Dim portNode = oxtsConfigNode.SelectSingleNode("NcomPort")
                If portNode IsNot Nothing Then
                    OxtsNcomPort = UShort.Parse(portNode.InnerText)
                End If
            End If

            HandleUserMessageLogging("GMRC",
                $"OXTS capture config: Enabled={OxtsCaptureEnabled}, IP={OxtsIpAddress}:{OxtsNcomPort}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"LoadOxtsCaptureConfig: {ex.Message}")
        End Try
    End Sub

End Module
