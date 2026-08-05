using Shift.Core;
using UnityEngine;

namespace Shift.Shared
{
    /// <summary>
    /// The prop-side consumer: turns a resolved <see cref="PrimitiveState"/> into rigidbody and
    /// collider settings. Discrete work only — it subscribes rather than polling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PrimitiveCarrier))]
    public class RigidbodyPrimitiveApplier : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private PrimitiveCarrier _carrier;
        private Collider[] _colliders;
        private PhysicsMaterial _material;
        private bool _gravitySuspended;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _carrier = GetComponent<PrimitiveCarrier>();
            _colliders = GetComponentsInChildren<Collider>();

            _material = new PhysicsMaterial("PropPrimitives")
            {
                hideFlags = HideFlags.HideAndDontSave,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Maximum
            };

            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].sharedMaterial = _material;
            }

            // Gravity is integrated manually so GRAVITY strength and direction mean something.
            _rigidbody.useGravity = false;
        }

        private void OnEnable()
        {
            _carrier.Changed += Apply;
            Apply(_carrier.Current);
        }

        private void OnDisable()
        {
            _carrier.Changed -= Apply;
        }

        private void OnDestroy()
        {
            if (_material == null) return;

            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }

        private void FixedUpdate()
        {
            if (_gravitySuspended || _rigidbody.isKinematic) return;

            PrimitiveState state = _carrier.Current;
            Vector3 gravity = state.GravityDirection * (Physics.gravity.magnitude * state.GravityStrength);
            _rigidbody.AddForce(gravity * _rigidbody.mass, ForceMode.Force);
        }

        /// <summary>
        /// Held props must not keep falling. Replaces the old useGravity toggle, which stopped
        /// meaning anything once gravity moved out of PhysX.
        /// </summary>
        public void SuspendGravity(bool suspended)
        {
            _gravitySuspended = suspended;
        }

        private void Apply(PrimitiveState state)
        {
            _rigidbody.mass = MassProfile.For(state.Mass).Kilograms;

            _material.dynamicFriction = state.Friction;
            _material.staticFriction = state.Friction;
            _material.bounciness = state.Bounce;

            // excludeLayers rather than moving the object between layers: per-instance, reversible,
            // no global collision-matrix edit, and it cannot leak onto a pooled object.
            int excluded = state.Ghost ? ShiftLayers.PropMask : 0;
            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].excludeLayers = excluded;
            }
        }
    }
}
