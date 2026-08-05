using Shift.Modifiers;
using UnityEngine;

namespace Shift.Shared
{
    /// <summary>
    /// Applies a <see cref="SurfaceDefinition"/> as a PhysicsMaterial on every collider here.
    /// </summary>
    /// <remarks>
    /// Surfaces deliberately stay out of the resolution stack — PhysX combines them instead.
    /// Friction averages: Maximum would make ice inert, since max(0.6, 0.0) is just normal
    /// friction and only a already-slippery entity would ever notice. Averaging gives
    /// ice+normal 0.30 (slippery) and ice+Heavy 0.45 (noticeably grippier), so a modifier can
    /// meaningfully answer a surface without cancelling it. Bounce stays Maximum because one
    /// bouncy party is enough to make a bounce, which is how bouncing reads in the real world.
    /// </remarks>
    public class PrimitiveSurface : MonoBehaviour
    {
        [SerializeField] private SurfaceDefinition _definition;

        private PhysicsMaterial _material;

        private void Awake()
        {
            Apply();
        }

        private void OnDestroy()
        {
            if (_material == null) return;

            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }

        private void Apply()
        {
            if (_definition == null) return;

            _material = new PhysicsMaterial(_definition.DisplayName)
            {
                hideFlags = HideFlags.HideAndDontSave,
                dynamicFriction = _definition.Friction,
                staticFriction = _definition.Friction,
                bounciness = _definition.Bounce,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Maximum
            };

            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].sharedMaterial = _material;
            }
        }
    }
}
