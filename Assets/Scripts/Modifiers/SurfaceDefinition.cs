using Shift.Core;
using UnityEngine;

namespace Shift.Modifiers
{
    /// <summary>
    /// Surface primitives. Unlike zones and modifiers these never enter the resolution stack —
    /// they become a PhysicsMaterial and are combined by the solver instead. See
    /// <see cref="Shift.Shared.PrimitiveSurface"/> for why.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSurface", menuName = "SHIFT/Surface", order = 2)]
    public class SurfaceDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Unnamed Surface";

        [Tooltip("Surfaces may only set FRICTION, BOUNCE and CHARGE.")]
        [SerializeField] private PrimitiveOverride _override = PrimitiveOverride.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public PrimitiveOverride Override => _override;

        public float Friction =>
            _override.Sets(PrimitiveMask.Friction) ? _override.Friction : PrimitiveState.NormalFriction;

        public float Bounce =>
            _override.Sets(PrimitiveMask.Bounce) ? _override.Bounce : 0f;

        public ChargeType Charge =>
            _override.Sets(PrimitiveMask.Charge) ? _override.Charge : ChargeType.None;

        private void OnValidate()
        {
            PrimitiveMask illegal = PrimitiveSubjects.IllegalBits(PrimitiveSubject.Surface, _override.Set);
            if (illegal == PrimitiveMask.None) return;

            Debug.LogWarning(
                $"{name}: sets {illegal}, which a surface cannot apply. Only FRICTION, BOUNCE and CHARGE are legal.",
                this);
        }
    }
}
