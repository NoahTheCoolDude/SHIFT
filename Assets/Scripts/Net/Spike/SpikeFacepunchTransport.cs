using System;
using Netcode.Transports.Facepunch;
using Unity.Netcode;

namespace Shift.Net.Spike
{
    /// <summary>
    /// FacepunchTransport that tallies outbound bytes. See <see cref="SpikeUnityTransport"/> for why
    /// the meter lives in a subclass rather than in the vendored transport itself.
    /// </summary>
    /// <remarks>
    /// Deliberately a subclass and not an edit to the embedded package: the vendored source is a
    /// fork of upstream 27d3e825ecdd, and every local change to it is a change we have to carry
    /// forever. Instrumentation that can live outside the fork should.
    /// </remarks>
    public class SpikeFacepunchTransport : FacepunchTransport, ISpikeSendCounter
    {
        private long _bytesSent;

        public long BytesSent => _bytesSent;

        public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery delivery)
        {
            _bytesSent += data.Count;
            base.Send(clientId, data, delivery);
        }
    }
}
