using System.Collections.Generic;

namespace Shift.Core
{
    /// <summary>
    /// Collapses the override stack into one <see cref="PrimitiveState"/>. The resolution order
    /// lives here and nowhere else.
    /// </summary>
    public static class PrimitiveResolver
    {
        /// <summary>
        /// Order: baseline → intrinsic → fields → active modifier.
        /// </summary>
        /// <remarks>
        /// Intrinsic first because it is what the object is by nature (a lead crate is heavy) —
        /// but it stays overridable, or a low-gravity room could not affect the crate. Fields next,
        /// because a field is world state you walked into; it must beat intrinsic but never the
        /// player. The active modifier wins outright: it is the choice the player made one keypress
        /// ago, and a field silently vetoing it would be an invisible, unguessable failure — which
        /// breaks rule 4 harder than any physics inconsistency. Taking a power away should require
        /// an explicit mechanism, not precedence trivia.
        /// </remarks>
        /// <param name="fields">Ascending priority. Pass null when there are none.</param>
        public static PrimitiveState Resolve(
            in PrimitiveState baseline,
            in PrimitiveOverride intrinsic,
            List<PrimitiveOverride> fields,
            in PrimitiveOverride activeModifier)
        {
            PrimitiveState state = baseline;

            intrinsic.ApplyTo(ref state);

            if (fields != null)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    fields[i].ApplyTo(ref state);
                }
            }

            activeModifier.ApplyTo(ref state);

            state.Clamp();
            return state;
        }
    }
}
