using System;
using UnityEngine;

namespace Shift.Modifiers
{
    /// <summary>
    /// One documented interaction with another modifier or with an environment feature.
    /// </summary>
    /// <remarks>
    /// Rule 3 says a modifier without at least three documented interactions gets cut. Carrying
    /// them as data rather than prose means OnValidate can warn, a test can fail the build, and
    /// <see cref="Expected"/> doubles as the playtest script for whoever is verifying the combo.
    /// </remarks>
    [Serializable]
    public class InteractionNote
    {
        [Tooltip("The other modifier, when the interaction is modifier-to-modifier.")]
        public ModifierDefinition WithModifier;

        [Tooltip("An environment feature instead, e.g. \"Ice\", \"SidewaysG\", \"Wind\".")]
        public string WithEnvironment;

        [TextArea(2, 4)]
        [Tooltip("What should happen. Read this aloud during playtest and check it actually does.")]
        public string Expected;

        public bool IsDocumented =>
            !string.IsNullOrWhiteSpace(Expected) &&
            (WithModifier != null || !string.IsNullOrWhiteSpace(WithEnvironment));

        public string Subject => WithModifier != null ? WithModifier.name : WithEnvironment;
    }
}
