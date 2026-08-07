using System;
using UnityEngine;

namespace Shift.Core
{
    /// <summary>
    /// One row of Docs/primitives.md: a set of primitive values plus a mask saying which of them
    /// are live. This is the whole of what a modifier is.
    /// </summary>
    /// <remarks>
    /// A mask rather than eight nullable fields because Unity does not serialize nullables, and
    /// the mask is one serialized line per asset instead of a bool beside every value — which
    /// matters when two people diff .asset YAML in Git.
    /// </remarks>
    [Serializable]
    public struct PrimitiveOverride
    {
        [Tooltip("Which primitives this override actually sets. Unticked ones pass through untouched.")]
        public PrimitiveMask Set;

        public MassClass Mass;
        [Range(0f, 1f)] public float Friction;
        [Range(0f, PrimitiveState.MaxBounce)] public float Bounce;
        public Vector3 GravityDirection;
        [Range(0f, PrimitiveState.MaxGravityStrength)] public float GravityStrength;
        public bool Adhesion;
        public bool Ghost;
        public ChargeType Charge;
        [Range(0f, PrimitiveState.MaxTimeRate)] public float TimeRate;

        public bool Sets(PrimitiveMask primitive) => (Set & primitive) != 0;

        /// <summary>Writes only the live columns onto <paramref name="state"/>.</summary>
        public void ApplyTo(ref PrimitiveState state)
        {
            if (Sets(PrimitiveMask.Mass)) state.Mass = Mass;
            if (Sets(PrimitiveMask.Friction)) state.Friction = Friction;
            if (Sets(PrimitiveMask.Bounce)) state.Bounce = Bounce;
            if (Sets(PrimitiveMask.Adhesion)) state.Adhesion = Adhesion;
            if (Sets(PrimitiveMask.Phase)) state.Ghost = Ghost;
            if (Sets(PrimitiveMask.Charge)) state.Charge = Charge;
            if (Sets(PrimitiveMask.TimeRate)) state.TimeRate = TimeRate;

            if (!Sets(PrimitiveMask.Gravity)) return;

            state.GravityDirection = GravityDirection;
            state.GravityStrength = GravityStrength;
        }

        /// <summary>Sensible starting values for a freshly authored asset, so an unticked column
        /// never holds a zero that would look deliberate if it were later ticked.</summary>
        public static PrimitiveOverride Empty => new PrimitiveOverride
        {
            Set = PrimitiveMask.None,
            Mass = MassClass.Normal,
            Friction = PrimitiveState.NormalFriction,
            Bounce = 0f,
            GravityDirection = Vector3.down,
            GravityStrength = 1f,
            Adhesion = false,
            Ghost = false,
            Charge = ChargeType.None,
            TimeRate = 1f
        };
    }
}
