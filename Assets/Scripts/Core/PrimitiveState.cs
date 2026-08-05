using System;
using UnityEngine;

namespace Shift.Core
{
    /// <summary>
    /// The fully resolved value of all 8 primitives for one entity. This is the entire vocabulary
    /// the game runs on — see Docs/primitives.md.
    /// </summary>
    /// <remarks>
    /// HARD RULE: every field must stay an unmanaged value type. No references, no strings, no
    /// arrays, no collections. Phase 0's netcode replicates this struct wholesale, and NGO's
    /// NetworkVariable&lt;T&gt; requires it to be C# `unmanaged`. Adding a GameObject reference
    /// here would quietly cost the game its replication strategy.
    /// PrimitiveStateTests enforces this by reflection.
    /// </remarks>
    [Serializable]
    public struct PrimitiveState : IEquatable<PrimitiveState>
    {
        /// <summary>Unity's stock friction. The baseline is defined to match, so an unmodified
        /// player behaves exactly as it did before the primitive engine existed.</summary>
        public const float NormalFriction = 0.6f;

        public const float MaxBounce = 0.95f;
        public const float MaxTimeRate = 2f;
        public const float MaxGravityStrength = 4f;

        public MassClass Mass;
        public float Friction;
        public float Bounce;
        public Vector3 GravityDirection;
        public float GravityStrength;
        public bool Adhesion;

        /// <summary>The PHASE primitive. True is "ghost": no collision with props.</summary>
        public bool Ghost;

        public ChargeType Charge;
        public float TimeRate;

        /// <summary>
        /// The unmodified world. Every derived expression in the motor collapses to its original
        /// arithmetic against these values, which is what makes the refactor feel-neutral.
        /// </summary>
        public static readonly PrimitiveState Normal = new PrimitiveState
        {
            Mass = MassClass.Normal,
            Friction = NormalFriction,
            Bounce = 0f,
            GravityDirection = Vector3.down,
            GravityStrength = 1f,
            Adhesion = false,
            Ghost = false,
            Charge = ChargeType.None,
            TimeRate = 1f
        };

        public void Clamp()
        {
            Friction = Mathf.Clamp01(Friction);
            Bounce = Mathf.Clamp(Bounce, 0f, MaxBounce);
            TimeRate = Mathf.Clamp(TimeRate, 0f, MaxTimeRate);
            GravityStrength = Mathf.Clamp(GravityStrength, 0f, MaxGravityStrength);

            GravityDirection = GravityDirection.sqrMagnitude < 0.0001f
                ? Vector3.down
                : GravityDirection.normalized;
        }

        public bool Equals(PrimitiveState other)
        {
            return Mass == other.Mass
                   && Mathf.Approximately(Friction, other.Friction)
                   && Mathf.Approximately(Bounce, other.Bounce)
                   && GravityDirection == other.GravityDirection
                   && Mathf.Approximately(GravityStrength, other.GravityStrength)
                   && Adhesion == other.Adhesion
                   && Ghost == other.Ghost
                   && Charge == other.Charge
                   && Mathf.Approximately(TimeRate, other.TimeRate);
        }

        public override bool Equals(object obj)
        {
            return obj is PrimitiveState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Mass;
                hash = (hash * 397) ^ Friction.GetHashCode();
                hash = (hash * 397) ^ Bounce.GetHashCode();
                hash = (hash * 397) ^ GravityDirection.GetHashCode();
                hash = (hash * 397) ^ GravityStrength.GetHashCode();
                hash = (hash * 397) ^ Adhesion.GetHashCode();
                hash = (hash * 397) ^ Ghost.GetHashCode();
                hash = (hash * 397) ^ (int)Charge;
                hash = (hash * 397) ^ TimeRate.GetHashCode();
                return hash;
            }
        }
    }
}
