namespace Shift.Net.Spike
{
    /// <summary>
    /// Implemented by the spike's transport subclasses so the HUD can read outbound traffic without
    /// knowing which transport is active.
    /// </summary>
    public interface ISpikeSendCounter
    {
        /// <summary>Total payload bytes handed to the transport since it was created.</summary>
        long BytesSent { get; }
    }
}
