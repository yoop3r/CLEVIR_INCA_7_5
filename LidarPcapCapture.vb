
Option Strict Off
Imports System.IO
Imports System.Linq
Imports System.Threading
Imports SharpPcap
Imports SharpPcap.LibPcap
Imports PacketDotNet

''' <summary>
''' ✅ MIGRATED: LiDAR PCAP Capture Module (SharpPcap)
''' Provides wrapper functions for multi-device LiDAR capture
''' Actual device instances are managed in Module1.LidarDevices
''' </summary>
Module LidarPcapCapture

    ''' <summary>
    ''' Starts LiDAR capture for all enabled devices
    ''' Legacy wrapper for single-device compatibility
    ''' </summary>
    Public Sub StartLidarCapture()
        StartLidarCaptureMulti()
    End Sub

    ''' <summary>
    ''' Starts capture for all configured LiDAR devices.
    ''' Devices that share the same adapter GUID are routed through a single
    ''' SharedNicCapture instance to avoid fighting over the same Npcap handle.
    ''' Devices with a unique GUID each get their own handle (legacy path).
    ''' </summary>
    Private Sub StartLidarCaptureMulti()
        Try
            If Not LidarCaptureEnabled OrElse LidarDevices.Count = 0 Then
                HandleUserMessageLogging("GMRC", "StartLidarCaptureMulti: No LiDAR devices configured")
                LidarCaptureStarted = False
                Return
            End If

            ' ----------------------------------------------------------------
            ' Build sequence / filename components
            ' ----------------------------------------------------------------
            Dim currentActiveSequence As String = GetCurrentActiveSequence()
            Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)
            Dim baseName As String = System.Text.RegularExpressions.Regex.Replace(
                Path.GetFileNameWithoutExtension(currentActiveSequence), "_\d{2}$", "")

            ' ----------------------------------------------------------------
            ' Group devices by normalised adapter GUID
            ' ----------------------------------------------------------------
            Dim groups = LidarDevices.
                GroupBy(Function(d) d.LidarAdapterGuid.Replace("{", "").Replace("}", "").ToUpper()).
                ToList()

            Dim successCount As Integer = 0

            For Each grp In groups
                Dim guidKey As String = grp.Key
                Dim groupDevices = grp.ToList()

                If groupDevices.Count = 1 Then
                    ' --------------------------------------------------------
                    ' SOLO path — unique NIC, use existing per-device capture
                    ' --------------------------------------------------------
                    Dim device = groupDevices(0)
                    Dim idx = LidarDevices.IndexOf(device)
                    Dim pcapFilename As String = Path.Combine(FinalPathToSaveData,
                        $"{baseName}_{currentSeq:D2}_LiDAR{idx + 1}.pcap")
                    device.LastCaptureFilePath = pcapFilename
                    Try
                        device.StartCapture(pcapFilename, currentSeq)
                        If device.IsCapturing Then
                            successCount += 1
                            HandleUserMessageLogging("GMRC",
                                $"StartLidarCaptureMulti: Started {device.DeviceId} (solo NIC)")
                        Else
                            HandleUserMessageLogging("GMRC",
                                $"StartLidarCaptureMulti: {device.DeviceId} IsCapturing=False after start")
                        End If
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC",
                            $"StartLidarCaptureMulti: Failed {device.DeviceId} — {ex.Message}")
                    End Try

                Else
                    ' --------------------------------------------------------
                    ' SHARED path — multiple devices on the same NIC
                    ' --------------------------------------------------------
                    Dim filenames As New List(Of String)
                    For Each device In groupDevices
                        Dim idx = LidarDevices.IndexOf(device)
                        Dim sharedPath As String = Path.Combine(FinalPathToSaveData,
                            $"{baseName}_{currentSeq:D2}_LiDAR{idx + 1}.pcap")
                        device.LastCaptureFilePath = sharedPath
                        filenames.Add(sharedPath)
                    Next

                    Try
                        Dim sharedNic As New SharedNicCapture(guidKey, groupDevices)
                        sharedNic.StartCapture(filenames, currentSeq)

                        ' Count devices that started
                        Dim started As Integer = groupDevices.Where(Function(d) d.IsCapturing).Count()
                        If started > 0 Then
                            SharedNicCaptures(guidKey) = sharedNic
                            successCount += started
                            HandleUserMessageLogging("GMRC",
                                $"StartLidarCaptureMulti: SharedNicCapture started for GUID {guidKey} " &
                                $"({started}/{groupDevices.Count} devices)")
                        Else
                            HandleUserMessageLogging("GMRC",
                                $"StartLidarCaptureMulti: SharedNicCapture for GUID {guidKey} — no devices started")
                        End If
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC",
                            $"StartLidarCaptureMulti: SharedNicCapture failed for GUID {guidKey} — {ex.Message}")
                    End Try
                End If
            Next

            If successCount > 0 Then
                LidarCaptureStarted = True
                HandleUserMessageLogging("GMRC",
                    $"StartLidarCaptureMulti: {successCount}/{LidarDevices.Count} device(s) started")
            Else
                LidarCaptureStarted = False
                HandleUserMessageLogging("GMRC",
                    $"StartLidarCaptureMulti: FAILED — no devices started")
            End If

        Catch ex As Exception
            LidarCaptureStarted = False
            HandleUserMessageLogging("GMRC", $"StartLidarCaptureMulti: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Injects event marker into all active captures
    ''' </summary>
    Public Sub InjectEventMarker(eventType As String, message As String, currentSeq As Integer)
        For Each device In LidarDevices
            If device.IsCapturing Then
                device.InjectEventMarker(eventType, message, currentSeq)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Stops LiDAR capture for all devices
    ''' </summary>
    Public Sub StopLidarCapture()
        StopLidarCaptureMulti()
    End Sub

    ''' <summary>
    ''' Stops capture for all LiDAR devices.
    ''' Shared-NIC groups are stopped via their SharedNicCapture instance;
    ''' solo devices are stopped directly.
    ''' </summary>
    Private Sub StopLidarCaptureMulti()
        Try
            If LidarDevices.Count = 0 Then Return

            ' ----------------------------------------------------------------
            ' Stop shared captures (covers all devices in each group)
            ' ----------------------------------------------------------------
            For Each kvp In SharedNicCaptures.ToList()
                Try
                    kvp.Value.StopCapture()
                    HandleUserMessageLogging("GMRC",
                        $"StopLidarCaptureMulti: SharedNicCapture stopped (GUID {kvp.Key})")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"StopLidarCaptureMulti: SharedNicCapture stop error (GUID {kvp.Key}): {ex.Message}")
                End Try
            Next
            SharedNicCaptures.Clear()

            ' ----------------------------------------------------------------
            ' Stop any solo devices still capturing
            ' ----------------------------------------------------------------
            For Each device In LidarDevices
                Try
                    If device.IsCapturing Then
                        device.StopCapture()
                        HandleUserMessageLogging("GMRC",
                            $"StopLidarCaptureMulti: Stopped {device.DeviceId}")
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"StopLidarCaptureMulti: Error stopping {device.DeviceId} — {ex.Message}")
                End Try
            Next

            LidarCaptureStarted = False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"StopLidarCaptureMulti: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Restarts capture for a new sequence (file rotation).
    ''' For shared-NIC groups the NIC handle is kept alive; only the dump files
    ''' are rotated via RotateSequence.  Solo devices stop/start as before.
    ''' </summary>
    Public Sub RestartLidarCaptureForNewSequence()
        Try
            If Not LidarCaptureEnabled Then Return

            Dim currentActiveSequence As String = GetCurrentActiveSequence()
            Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)
            Dim baseName As String = System.Text.RegularExpressions.Regex.Replace(
                Path.GetFileNameWithoutExtension(currentActiveSequence), "_\d{2}$", "")

            HandleUserMessageLogging("GMRC",
                $"RestartLidarCaptureForNewSequence: Rotating to sequence {currentSeq:D2}...")

            ' ----------------------------------------------------------------
            ' Shared-NIC groups: rotate dump files without dropping the NIC handle
            ' ----------------------------------------------------------------
            For Each kvp In SharedNicCaptures
                Dim rotGuidKey = kvp.Key
                Dim sharedNic = kvp.Value

                ' Rebuild filename list for this group
                Dim groupFilenames As New List(Of String)
                For Each device In LidarDevices
                    If device.LidarAdapterGuid.Replace("{", "").Replace("}", "").ToUpper() = rotGuidKey Then
                        Dim idx = LidarDevices.IndexOf(device)
                        Dim rotatedPath As String = Path.Combine(FinalPathToSaveData,
                            $"{baseName}_{currentSeq:D2}_LiDAR{idx + 1}.pcap")
                        device.LastCaptureFilePath = rotatedPath
                        groupFilenames.Add(rotatedPath)
                    End If
                Next

                Try
                    sharedNic.RotateSequence(groupFilenames, currentSeq)
                    HandleUserMessageLogging("GMRC",
                        $"RestartLidarCaptureForNewSequence: SharedNicCapture rotated (GUID {rotGuidKey})")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"RestartLidarCaptureForNewSequence: Rotate error (GUID {rotGuidKey}): {ex.Message}")
                End Try
            Next

            ' ----------------------------------------------------------------
            ' Solo devices: full stop → start cycle
            ' ----------------------------------------------------------------
            For Each device In LidarDevices
                Dim guidKey = device.LidarAdapterGuid.Replace("{", "").Replace("}", "").ToUpper()
                If SharedNicCaptures.ContainsKey(guidKey) Then Continue For  ' Already handled above

                Dim idx = LidarDevices.IndexOf(device)
                Dim pcapFilename As String = Path.Combine(FinalPathToSaveData,
                    $"{baseName}_{currentSeq:D2}_LiDAR{idx + 1}.pcap")
                device.LastCaptureFilePath = pcapFilename
                Try
                    If device.IsCapturing Then device.StopCapture()
                    Threading.Thread.Sleep(200)
                    device.StartCapture(pcapFilename, currentSeq)
                    HandleUserMessageLogging("GMRC",
                        $"RestartLidarCaptureForNewSequence: Restarted {device.DeviceId} (solo NIC)")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC",
                        $"RestartLidarCaptureForNewSequence: Error restarting {device.DeviceId} — {ex.Message}")
                End Try
            Next

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"RestartLidarCaptureForNewSequence: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Decodes a Hesai AT128 return-mode byte to a human-readable string.
    ''' </summary>
    Private Function GetHesaiReturnModeName(mode As Byte) As String
        Select Case mode
            Case &H33 : Return "First"
            Case &H37 : Return "Strongest"
            Case &H38 : Return "Last"
            Case &H39 : Return "Dual(Last+Strong)"
            Case &H3B : Return "Dual(Last+First)"
            Case &H3C : Return "Dual(First+Strong)"
            Case Else : Return $"Mode 0x{mode:X2}"
        End Select
    End Function

    ''' <summary>
    ''' Post-processes a sealed PCAP file using SharpPcap offline reading.
    ''' Computes capture-quality diagnostics equivalent to diagnose_pcap:
    '''   Tier 1 — Hesai/non-Hesai counts, source IPs, return mode,
    '''            UDP sequence gap detection (CAPTURE_LOSS_GAP), 65-pkt slices.
    '''   Tier 2 — Phase drift analysis (PHASE_DRIFT) using PTP sensor clock
    '''            vs PCAP wall-clock timestamps.
    ''' Generates a .lidar_events.txt sidecar if one does not already exist.
    ''' Zero impact on live capture — reads only sealed files.
    ''' </summary>
    Public Sub ProcessLidarPcapFile(pcapFilePath As String)
        Try
            If Not File.Exists(pcapFilePath) Then
                HandleUserMessageLogging("GMRC", $"ProcessLidarPcapFile: File not found: {pcapFilePath}")
                Return
            End If

            Dim fileName As String = Path.GetFileName(pcapFilePath)
            Dim eventLogPath As String = Path.ChangeExtension(pcapFilePath, ".lidar_events.txt")
            Dim eventLogExists As Boolean = File.Exists(eventLogPath)

            HandleUserMessageLogging("GMRC", $"== ProcessLidarPcapFile: {fileName} ==")

            Dim offlineDevice As New CaptureFileReaderDevice(pcapFilePath)
            offlineDevice.Open()

            ' --- Frame / byte counters ---
            Dim frameNumber As Long = 0
            Dim totalBytes As Long = 0
            Dim hesaiPacketCount As Long = 0
            Dim nonHesaiUdpCount As Long = 0
            Dim markerCount As Integer = 0
            Dim firstTimestamp As DateTime? = Nothing
            Dim lastTimestamp As DateTime? = Nothing

            ' --- Sequence gap tracking (CAPTURE_LOSS_GAP) ---
            Dim lastUdpSeq As UInteger = 0
            Dim seqInitialized As Boolean = False
            Dim gapEvents As Long = 0
            Dim estimatedLostPackets As Long = 0
            Dim gapLog As New System.Text.StringBuilder()  ' per-gap detail for report

            ' --- Phase jitter tracking (PHASE_DRIFT) ---
            ' The AT128 tail timestamp and the PCAP host clock use different references,
            ' so a constant offset (clock skew) is expected and normal.
            ' What matters is how much the offset *varies* from its own mean (jitter).
            ' We use Welford's online algorithm to compute mean and σ in a single pass.
            Dim phaseN As Long = 0
            Dim phaseMeanUs As Double = 0.0     ' running mean of (pcapUs − sensorUs)
            Dim phaseM2Us As Double = 0.0       ' Welford M2 accumulator → variance
            Dim phaseMaxJitterUs As Double = 0.0 ' max |deviation from running mean|
            Dim phaseJitterEvents As Long = 0
            Const PhaseJitterTolUs As Double = 16000.0  ' 16 ms jitter tolerance

            ' --- Source IPs and return mode ---
            Dim sourceIps As New Dictionary(Of String, Long)()
            Dim returnModeStr As String = String.Empty

            ' --- Optional event-log generation ---
            Dim postProcessLogger As LidarEventLogger = Nothing
            If Not eventLogExists Then
                postProcessLogger = New LidarEventLogger(pcapFilePath)
                HandleUserMessageLogging("GMRC", "  Generating event log during post-processing...")
            End If

            ' === Main packet loop ===
            Dim packetStatus As GetPacketStatus
            Dim packetCapture As Object = Nothing
            Dim malformedCount As Long = 0

            Do
                packetStatus = offlineDevice.GetNextPacket(packetCapture)
                If packetStatus <> GetPacketStatus.PacketRead Then Exit Do

                Try
                    Dim getPacketMethod = packetCapture.GetType().GetMethod("GetPacket")
                    Dim rawPacket As RawCapture = DirectCast(getPacketMethod.Invoke(packetCapture, Nothing), RawCapture)

                    ' Skip zero-length or undersized frames (can occur at rotation boundary)
                    If rawPacket Is Nothing OrElse rawPacket.Data Is Nothing OrElse rawPacket.Data.Length < 42 Then
                        malformedCount += 1
                        Continue Do
                    End If

                    frameNumber += 1
                    totalBytes += rawPacket.Data.Length

                    Dim packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data)
                    Dim ethPacket = TryCast(packet, EthernetPacket)

                    If ethPacket IsNot Nothing Then
                        Dim ipPacket = TryCast(ethPacket.PayloadPacket, IPv4Packet)
                        If ipPacket IsNot Nothing Then
                            Dim udpPacket = TryCast(ipPacket.PayloadPacket, UdpPacket)
                            If udpPacket IsNot Nothing Then

                                ' --- Event marker ---
                                If udpPacket.DestinationPort = LidarDevice.MarkerDestPort AndAlso
                               udpPacket.SourcePort = LidarDevice.MarkerSourcePort Then
                                    markerCount += 1
                                    Dim markerData As String = System.Text.Encoding.UTF8.GetString(udpPacket.PayloadData)
                                    Dim parts = markerData.Split("|"c)
                                    If parts.Length >= 3 Then
                                        postProcessLogger?.LogEvent(frameNumber, rawPacket.Timeval.Date,
                                                                parts(0), parts(1), Integer.Parse(parts(2)))
                                        HandleUserMessageLogging("GMRC", $"  Frame {frameNumber}: {parts(0)} - {parts(1)}")
                                    Else
                                        HandleUserMessageLogging("GMRC", $"  Frame {frameNumber}: {markerData}")
                                    End If

                                ElseIf udpPacket.PayloadData IsNot Nothing Then
                                    ' --- Attempt Hesai parse ---
                                    Dim info As HesaiPacketInfo = LidarDevice.ParseHesaiPacket(udpPacket.PayloadData)

                                    If info.IsValid Then
                                        hesaiPacketCount += 1

                                        ' Source IP (Hesai packets only — mirrors diagnose_pcap)
                                        Dim srcIp As String = ipPacket.SourceAddress.ToString()
                                        If sourceIps.ContainsKey(srcIp) Then
                                            sourceIps(srcIp) += 1
                                        Else
                                            sourceIps(srcIp) = 1
                                        End If

                                        ' Return mode from first valid packet
                                        If returnModeStr = String.Empty Then
                                            returnModeStr = GetHesaiReturnModeName(info.ReturnMode)
                                        End If

                                        ' --- Sequence gap detection ---
                                        If seqInitialized Then
                                            Dim delta As Long = CLng(info.UdpSequence) - CLng(lastUdpSeq)
                                            If delta < 0L Then delta += 4294967296L  ' UInt32 wrap
                                            If delta > 1L Then
                                                gapEvents += 1
                                                Dim lostHere As Long = delta - 1L
                                                estimatedLostPackets += lostHere
                                                ' Record when in the file this gap occurred
                                                Dim gapOffset As TimeSpan = If(firstTimestamp.HasValue,
                                                rawPacket.Timeval.Date - firstTimestamp.Value,
                                                TimeSpan.Zero)
                                                gapLog.AppendLine(
                                                $"    gap #{gapEvents}: +{gapOffset.TotalSeconds:F2} s into file — " &
                                                $"{lostHere:N0} pkt(s) lost (seq {lastUdpSeq} → {info.UdpSequence})")
                                            End If
                                        Else
                                            seqInitialized = True
                                        End If
                                        lastUdpSeq = info.UdpSequence

                                        ' --- Phase jitter (sensor PTP clock vs PCAP host clock) ---
                                        ' Compute raw offset; wrap ±500 ms to handle second-boundary crossings.
                                        Dim pcapUs As Long = rawPacket.Timeval.MicroSeconds  ' 0–999,999
                                        Dim sensorUs As Long = CLng(info.TailTimestampUs)    ' 0–999,999
                                        Dim rawOffsetUs As Long = pcapUs - sensorUs
                                        If rawOffsetUs > 500000L Then rawOffsetUs -= 1000000L
                                        If rawOffsetUs < -500000L Then rawOffsetUs += 1000000L

                                        ' Welford online update
                                        phaseN += 1
                                        Dim wDelta As Double = CDbl(rawOffsetUs) - phaseMeanUs
                                        phaseMeanUs += wDelta / CDbl(phaseN)
                                        Dim wDelta2 As Double = CDbl(rawOffsetUs) - phaseMeanUs
                                        phaseM2Us += wDelta * wDelta2

                                        ' Jitter = deviation from the running mean
                                        Dim jitterUs As Double = Math.Abs(CDbl(rawOffsetUs) - phaseMeanUs)
                                        If jitterUs > phaseMaxJitterUs Then phaseMaxJitterUs = jitterUs
                                        If jitterUs > PhaseJitterTolUs Then phaseJitterEvents += 1

                                    Else
                                        nonHesaiUdpCount += 1
                                    End If
                                End If
                            End If
                        End If
                    End If

                    If firstTimestamp Is Nothing Then firstTimestamp = rawPacket.Timeval.Date
                    lastTimestamp = rawPacket.Timeval.Date

                Catch pkEx As Exception
                    malformedCount += 1
                    If malformedCount <= 5 Then
                        HandleUserMessageLogging("GMRC",
                            $"  [WARN] Frame {frameNumber + 1}: skipped — {pkEx.GetType().Name}: {pkEx.Message}")
                    End If
                End Try
            Loop

            offlineDevice.Close()
            postProcessLogger?.Close()

            ' === Report ===
            If Not firstTimestamp.HasValue OrElse Not lastTimestamp.HasValue Then
                HandleUserMessageLogging("GMRC", "ProcessLidarPcapFile: No packets found in file")
                Return
            End If

            Dim duration As TimeSpan = lastTimestamp.Value - firstTimestamp.Value
            Dim avgRate As Double = If(duration.TotalSeconds > 0, hesaiPacketCount / duration.TotalSeconds, 0)
            Dim totalExpected As Long = hesaiPacketCount + estimatedLostPackets
            Dim lossPercent As Double = If(totalExpected > 0, (CDbl(estimatedLostPackets) / CDbl(totalExpected)) * 100.0, 0)
            Dim clockOffsetMs As Double = phaseMeanUs / 1000.0   ' constant clock skew — informational
            Dim phaseStdDevMs As Double = If(phaseN > 1, Math.Sqrt(phaseM2Us / CDbl(phaseN - 1)) / 1000.0, 0)
            Dim maxJitterMs As Double = phaseMaxJitterUs / 1000.0
            Dim slices As Long = hesaiPacketCount \ 65L
            Dim lastSlice As Long = hesaiPacketCount Mod 65L

            HandleUserMessageLogging("GMRC", $"  hesai packets : {hesaiPacketCount:N0}  (non-hesai UDP: {nonHesaiUdpCount:N0}{If(malformedCount > 0, $", malformed/skipped: {malformedCount:N0}", "")})")
            If returnModeStr <> String.Empty Then
                HandleUserMessageLogging("GMRC", $"  return mode   : {returnModeStr}")
            End If
            For Each kvp In sourceIps
                HandleUserMessageLogging("GMRC", $"  source IP     : {kvp.Key} → {kvp.Value:N0} pkts")
            Next
            HandleUserMessageLogging("GMRC", $"  duration      : {duration.TotalSeconds:F2} s  ({avgRate:F0} pkt/s)")
            HandleUserMessageLogging("GMRC", $"  est loss      : {estimatedLostPackets:N0} pkts / {lossPercent:F2}%  over {gapEvents:N0} gap(s)")
            HandleUserMessageLogging("GMRC", $"  phase-lock    : clock offset {If(clockOffsetMs >= 0, "+", "")}{clockOffsetMs:F1} ms (INFO),  jitter σ={phaseStdDevMs:F2} ms,  peak={maxJitterMs:F2} ms  (tol 16 ms)")
            HandleUserMessageLogging("GMRC", $"  65-pkt slices : {slices:N0}  (last slice: {lastSlice} pkts)")
            HandleUserMessageLogging("GMRC", $"  event markers : {markerCount:N0}")
            HandleUserMessageLogging("GMRC", $"  event log     : {If(eventLogExists, "Validated", "Generated")} — {Path.GetFileName(eventLogPath)}")

            ' --- VERDICT ---
            ' Loss thresholds:
            '   ERROR   > 0.10%  — significant data loss, investigate
            '   WARNING  0.01–0.10% — minor network micro-bursts, acceptable
            '   OK      < 0.01%  — negligible, within normal tolerance
            HandleUserMessageLogging("GMRC", "")
            HandleUserMessageLogging("GMRC", "== VERDICT ==")
            Dim errorCount As Integer = 0
            Dim warnCount As Integer = 0

            If gapEvents > 0 Then
                If lossPercent > 0.1 Then
                    errorCount += 1
                    HandleUserMessageLogging("GMRC", $"[X] ERROR   CAPTURE_LOSS_GAP  ({gapEvents} event(s), {lossPercent:F2}%)")
                ElseIf lossPercent > 0.01 Then
                    warnCount += 1
                    HandleUserMessageLogging("GMRC", $"[!] WARNING CAPTURE_LOSS_GAP  ({gapEvents} event(s), {lossPercent:F2}%)")
                Else
                    HandleUserMessageLogging("GMRC", $"[~] INFO    CAPTURE_LOSS_GAP  ({gapEvents} event(s), {lossPercent:F2}% — negligible)")
                End If
                HandleUserMessageLogging("GMRC", gapLog.ToString().TrimEnd())
            End If
            If phaseJitterEvents > 0 Then
                warnCount += 1
                HandleUserMessageLogging("GMRC", $"[!] WARNING PHASE_DRIFT       ({phaseJitterEvents} event(s) jitter > 16 ms, peak {maxJitterMs:F2} ms, σ={phaseStdDevMs:F2} ms)")
            End If
            If errorCount = 0 AndAlso warnCount = 0 Then
                HandleUserMessageLogging("GMRC", "[OK] No errors or warnings — capture looks clean")
            End If

            ' Write summary to INCA event log
            MyIncaInterface?.WriteEventComment(
                $"LiDAR PCAP: {hesaiPacketCount:N0} pkts, loss {lossPercent:F2}% ({gapEvents} gaps), " &
                $"jitter peak={maxJitterMs:F2} ms σ={phaseStdDevMs:F2} ms, {duration.TotalSeconds:F2} s", True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ProcessLidarPcapFile: {ex.Message}")
        End Try
    End Sub

End Module
