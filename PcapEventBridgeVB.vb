Option Strict On
Imports System
Imports SharpPcap

''' <summary>
''' EventArgs wrapper that carries a RawCapture from SharpPcap.
''' VB.NET cannot use PacketCapture directly because it is a ref struct (C# 7.2+),
''' which the VB.NET compiler does not support.
''' 
''' This native VB.NET version provides the same functionality as the C# bridge
''' without requiring an external project reference.
''' </summary>
Public NotInheritable Class PacketArrivedEventArgs
    Inherits EventArgs

    Public ReadOnly Property Packet As RawCapture

    Friend Sub New(packet As RawCapture)
        Me.Packet = packet
    End Sub
End Class

''' <summary>
''' Bridges SharpPcap's ref-struct-based PacketArrivalEventHandler to a
''' standard EventHandler(Of PacketArrivedEventArgs) that VB.NET can consume natively.
''' 
''' IMPLEMENTATION NOTE: This class CANNOT directly handle the OnPacketArrival event
''' from SharpPcap because VB.NET cannot compile method signatures containing ref structs.
''' Instead, it uses late-binding via delegates created through reflection.
''' </summary>
Public NotInheritable Class VBPcapEventBridge
    Private _device As ICaptureDevice
    Private _handlerDelegate As [Delegate]

    ''' <summary>Raised on the SharpPcap capture thread for every arriving packet.</summary>
    Public Event PacketArrived As EventHandler(Of PacketArrivedEventArgs)

    ''' <summary>
    ''' Subscribe to device.OnPacketArrival using reflection-based late binding.
    ''' Call Unsubscribe() to remove the handler.
    ''' </summary>
    Public Sub Subscribe(device As ICaptureDevice)
        If _device IsNot Nothing Then
            Unsubscribe()
        End If

        _device = device

        ' Create delegate using reflection (VB.NET can't directly reference ref struct types)
        Dim handlerMethod = Me.GetType().GetMethod("OnPacketArrivalReflection",
                                                    Reflection.BindingFlags.NonPublic Or
                                                    Reflection.BindingFlags.Instance)

        ' Get PacketArrivalEventHandler type from SharpPcap assembly
        Dim eventHandlerType = GetType(ICaptureDevice).Assembly.GetType("SharpPcap.PacketArrivalEventHandler")

        ' Create delegate instance
        _handlerDelegate = [Delegate].CreateDelegate(eventHandlerType, Me, handlerMethod)

        ' Subscribe using reflection
        Dim eventInfo = _device.GetType().GetEvent("OnPacketArrival")
        eventInfo.AddEventHandler(_device, _handlerDelegate)
    End Sub

    ''' <summary>Remove the handler from the previously subscribed device.</summary>
    Public Sub Unsubscribe()
        If _device Is Nothing Then Return

        Try
            Dim eventInfo = _device.GetType().GetEvent("OnPacketArrival")
            eventInfo.RemoveEventHandler(_device, _handlerDelegate)
        Catch
            ' Ignore cleanup errors
        End Try

        _handlerDelegate = Nothing
        _device = Nothing
    End Sub

    ''' <summary>
    ''' Internal handler called via reflection from SharpPcap's event.
    ''' Signature matches PacketArrivalEventHandler: (sender As Object, capture As PacketCapture)
    ''' 
    ''' This method signature is NEVER compiled with ref struct types - it uses Object
    ''' and reflection to extract the RawCapture at runtime.
    ''' </summary>
    Private Sub OnPacketArrivalReflection(sender As Object, capture As Object)
        Try
            ' Extract RawCapture using reflection (capture is PacketCapture ref struct)
            Dim getPacketMethod = capture.GetType().GetMethod("GetPacket")
            Dim rawPacket As RawCapture = DirectCast(getPacketMethod.Invoke(capture, Nothing), RawCapture)

            ' Raise VB-friendly event
            RaiseEvent PacketArrived(sender, New PacketArrivedEventArgs(rawPacket))
        Catch ex As Exception
            ' Don't crash on single packet errors
            HandleUserMessageLogging("GMRC", $"VBPcapEventBridge: Packet extraction error: {ex.Message}")
        End Try
    End Sub
End Class
