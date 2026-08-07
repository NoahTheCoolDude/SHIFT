using Shift.Core;
using UnityEngine;

namespace Shift.Modifiers
{
    /// <summary>
    /// A Reality Modifier. Entirely data: a bundle of primitive values plus the interactions it
    /// is expected to produce. If a modifier ever needs code, the primitive system is wrong.
    /// </summary>
    [CreateAssetMenu(fileName = "NewModifier", menuName = "SHIFT/Modifier", order = 0)]
    public class ModifierDefinition : ScriptableObject
    {
        public const int RequiredInteractions = 3;

        [SerializeField] private string _displayName = "Unnamed";
        [SerializeField] private Color _tint = new Color(1f, 0.79f, 0.28f, 1f);

        [Tooltip("The row of primitive values. Untick a primitive to leave it untouched.")]
        [SerializeField] private PrimitiveOverride _override = PrimitiveOverride.Empty;

        [Tooltip("At least three, or the modifier gets cut. See rule 3.")]
        [SerializeField] private InteractionNote[] _interactions = new InteractionNote[0];

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public Color Tint => _tint;
        public PrimitiveOverride Override => _override;
        public InteractionNote[] Interactions => _interactions;

        public int DocumentedInteractionCount
        {
            get
            {
                if (_interactions == null) return 0;

                int count = 0;
                for (int i = 0; i < _interactions.Length; i++)
                {
                    if (_interactions[i] != null && _interactions[i].IsDocumented) count++;
                }

                return count;
            }
        }

        private void OnValidate()
        {
            PrimitiveMask illegal = PrimitiveSubjects.IllegalBits(PrimitiveSubject.Player, _override.Set);
            if (illegal != PrimitiveMask.None)
            {
                Debug.LogWarning($"{name}: sets {illegal}, which does not apply to a player.", this);
            }

            if (_override.Set == PrimitiveMask.None)
            {
                Debug.LogWarning($"{name}: sets no primitives, so equipping it does nothing.", this);
            }

            if (DocumentedInteractionCount < RequiredInteractions)
            {
                Debug.LogWarning(
                    $"{name}: {DocumentedInteractionCount}/{RequiredInteractions} documented interactions. " +
                    "A modifier that only works alone is dead weight (rule 3).", this);
            }
        }
    }
}
