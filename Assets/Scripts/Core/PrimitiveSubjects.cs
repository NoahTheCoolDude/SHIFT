namespace Shift.Core
{
    /// <summary>
    /// Encodes the "Applies to" column of Docs/primitives.md so illegal authoring is caught at
    /// import time rather than showing up as a primitive that silently does nothing.
    /// </summary>
    public static class PrimitiveSubjects
    {
        public static PrimitiveMask LegalMask(PrimitiveSubject subject)
        {
            switch (subject)
            {
                case PrimitiveSubject.Field:
                    return PrimitiveMask.Gravity | PrimitiveMask.TimeRate;

                case PrimitiveSubject.Surface:
                    return PrimitiveMask.Friction | PrimitiveMask.Bounce | PrimitiveMask.Charge;

                // Adhesion is a player verb — a prop that sticks to everything is a FRICTION prop.
                case PrimitiveSubject.Prop:
                    return PrimitiveMask.All & ~PrimitiveMask.Adhesion;

                default:
                    return PrimitiveMask.All;
            }
        }

        /// <summary>The bits <paramref name="set"/> uses that <paramref name="subject"/> cannot honour.</summary>
        public static PrimitiveMask IllegalBits(PrimitiveSubject subject, PrimitiveMask set)
        {
            return set & ~LegalMask(subject);
        }
    }
}
