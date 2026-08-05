namespace Shift.Core
{
    /// <summary>
    /// The MASS primitive. Backed by byte so <see cref="PrimitiveState"/> stays small enough to
    /// replicate cheaply once netcode lands.
    /// </summary>
    public enum MassClass : byte
    {
        Tiny = 0,
        Light = 1,
        Normal = 2,
        Heavy = 3,
        Giant = 4
    }
}
