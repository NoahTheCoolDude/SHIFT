using System;

namespace Shift.Core
{
    /// <summary>
    /// Which primitives a <see cref="PrimitiveOverride"/> actually sets. This is the "live
    /// columns" of one row in Docs/primitives.md.
    /// </summary>
    /// <remarks>
    /// These values are serialized into .asset files. Renumbering them silently rewrites every
    /// authored modifier, so add new bits at the end and never reorder.
    /// </remarks>
    [Flags]
    public enum PrimitiveMask : ushort
    {
        None = 0,
        Mass = 1 << 0,
        Friction = 1 << 1,
        Bounce = 1 << 2,

        /// <summary>Covers direction and strength together — they are meaningless apart.</summary>
        Gravity = 1 << 3,

        Adhesion = 1 << 4,
        Phase = 1 << 5,
        Charge = 1 << 6,
        TimeRate = 1 << 7,

        All = Mass | Friction | Bounce | Gravity | Adhesion | Phase | Charge | TimeRate
    }
}
