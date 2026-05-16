using System;
using SharpPcap;

namespace PcapEventBridge
{
    /// <summary>
    /// EventArgs wrapper that carries a RawCapture from SharpPcap.
    /// VB.NET cannot use PacketCapture directly because it is a ref struct (C# 7.2+),
    /// which the VB.NET compiler does not support. This shim subscribes to
    /// ICaptureDevice.OnPacketArrival in C# (where ref structs are legal) and
    /// re-raises packets as a normal EventArgs-based event that VB.NET can consume.
    /// </summary>
    public sealed class PacketArrivedEventArgs : EventArgs
    {
        public RawCapture Packet { get; }

        internal PacketArrivedEventArgs(RawCapture packet)
        {
            Packet = packet;
        }
    }

    /// <summary>
    /// Bridges SharpPcap's ref-struct-based PacketArrivalEventHandler to a
    /// standard EventHandler(Of PacketArrivedEventArgs) that VB.NET can consume
    /// without reflection.
    /// </summary>
    public sealed class PcapEventBridge
    {
        private ICaptureDevice? _device;

        /// <summary>Raised on the SharpPcap capture thread for every arriving packet.</summary>
        public event EventHandler<PacketArrivedEventArgs>? PacketArrived;

        /// <summary>
        /// Subscribe to <paramref name="device"/>.OnPacketArrival.
        /// Call Unsubscribe() or Dispose() to remove the handler.
        /// </summary>
        public void Subscribe(ICaptureDevice device)
        {
            if (_device != null)
                Unsubscribe();

            _device = device;
            _device.OnPacketArrival += OnPacketArrival;
        }

        /// <summary>Remove the handler from the previously subscribed device.</summary>
        public void Unsubscribe()
        {
            if (_device == null) return;
            _device.OnPacketArrival -= OnPacketArrival;
            _device = null;
        }

        // C# can reference PacketCapture (a ref struct) directly here.
        private void OnPacketArrival(object sender, PacketCapture capture)
        {
            var raw = capture.GetPacket();
            PacketArrived?.Invoke(sender, new PacketArrivedEventArgs(raw));
        }
    }
}
