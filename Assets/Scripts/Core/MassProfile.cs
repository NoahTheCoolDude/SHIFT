namespace Shift.Core
{
    /// <summary>
    /// The consequences of a MASS value. This is the one place in the engine where a primitive
    /// becomes numbers.
    /// </summary>
    /// <remarks>
    /// Deliberately code, not an asset. Rule 1 bans modifiers from authoring stat multipliers;
    /// one shared table that derives consistent consequences from a primitive value is what makes
    /// that rule workable. The moment this becomes per-modifier tuning, every modifier can smuggle
    /// in "+10% speed" and the rule is dead.
    /// </remarks>
    public readonly struct MassProfile
    {
        /// <summary>Matches the 70kg the sandbox player has always used.</summary>
        public const float NormalKilograms = 70f;

        public readonly float Kilograms;
        public readonly float SpeedScale;
        public readonly float JumpScale;

        /// <summary>Jetpack thrust is a force, so acceleration falls off as 1/m.</summary>
        public readonly float ThrustScale;

        public readonly float CarryScale;

        private MassProfile(float kilograms, float speedScale, float jumpScale, float carryScale)
        {
            Kilograms = kilograms;
            SpeedScale = speedScale;
            JumpScale = jumpScale;
            CarryScale = carryScale;
            ThrustScale = NormalKilograms / kilograms;
        }

        private static readonly MassProfile[] _table =
        {
            //                    kg     speed  jump   carry
            new MassProfile(   8f,  1.25f, 1.30f, 0.25f), // Tiny
            new MassProfile(  30f,  1.12f, 1.15f, 0.55f), // Light
            new MassProfile(  70f,  1.00f, 1.00f, 1.00f), // Normal
            new MassProfile( 140f,  0.85f, 0.80f, 2.00f), // Heavy
            new MassProfile( 400f,  0.65f, 0.55f, 4.00f)  // Giant
        };

        public static MassProfile For(MassClass mass)
        {
            int index = (int)mass;
            return index >= 0 && index < _table.Length ? _table[index] : _table[(int)MassClass.Normal];
        }
    }
}
