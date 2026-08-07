using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Shift.Net.Spike
{
    /// <summary>
    /// UnityTransport that tallies outbound bytes.
    /// </summary>
    /// <remarks>
    /// NGO exposes no byte counters without the Multiplayer Tools package, and the spike's pass
    /// criteria are partly bandwidth claims — "~20 rigidbodies, no visible jitter" is not a number.
    /// Subclassing is the cheapest honest meter: <see cref="Send"/> is the single choke point every
    /// outbound payload passes through, so counting here cannot miss traffic the way sampling would.
    ///
    /// Inbound bytes are NOT counted here. Facepunch delivers received data through
    /// InvokeOnTransportEvent rather than PollEvent, so there is no symmetric override; both
    /// transports are metered for receive by subscribing to OnTransportEvent in
    /// <see cref="SpikeNetStats"/> instead.
    /// </remarks>
    public class SpikeUnityTransport : UnityTransport, ISpikeSendCounter
    {
        private long _bytesSent;

        public long BytesSent => _bytesSent;

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            _bytesSent += payload.Count;
            base.Send(clientId, payload, networkDelivery);
        }
    }
}
