using Shift.Core;
using UnityEngine;

namespace Shift.Modifiers
{
    /// <summary>
    /// World state a volume imposes on whatever is inside it. Reusable across scenes — one
    /// "LowGravity" asset drives every low-G room in the game.
    /// </summary>
    /// <remarks>
    /// A Field, not a Zone: in CLAUDE.md a Zone is a level. Gravity field and time-dilation field
    /// is also what these physically are.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewField", menuName = "SHIFT/Field", order = 1)]
    public class FieldDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Unnamed Field";

        [Tooltip("Fields may only set GRAVITY and TIME_RATE. See the Applies-to column in Docs/primitives.md.")]
        [SerializeField] private PrimitiveOverride _override = PrimitiveOverride.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public PrimitiveOverride Override => _override;

        private void OnValidate()
        {
            PrimitiveMask illegal = PrimitiveSubjects.IllegalBits(PrimitiveSubject.Field, _override.Set);
            if (illegal == PrimitiveMask.None) return;

            Debug.LogWarning(
                $"{name}: sets {illegal}, which a field cannot apply. Only GRAVITY and TIME_RATE are legal.",
                this);
        }
    }
}
