using System;
using System.Collections.Generic;
using Shift.Core;
using Shift.Shared;
using UnityEngine;

namespace Shift.Level
{
    /// <summary>
    /// A pressure plate that sums the mass resting on it.
    /// </summary>
    /// <remarks>
    /// Reads a player's weight from <see cref="MassProfile"/>, not from Rigidbody.mass. The
    /// player's rigidbody mass is a hardcoded 70 that never tracks the MASS primitive, so reading
    /// it would make Heavy weigh exactly as much as Feather and the plate would be unsolvable.
    /// Props are the other way round — their applier does keep Rigidbody.mass in sync.
    ///
    /// Sums rather than requiring one heavy object, so a co-op team can simply stand on it. That
    /// is the free faster route CLAUDE.md rule 7 asks every Zone to have.
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public class MassPlate : MonoBehaviour
    {
        [SerializeField] private float _thresholdKilograms = 30f;

        [Tooltip("Once pressed, stays pressed. Puzzle doors should not re-close behind you.")]
        [SerializeField] private bool _latching = true;

        private readonly List<Collider> _resting = new List<Collider>();

        public bool IsPressed { get; private set; }
        public float RestingKilograms { get; private set; }
        public float ThresholdKilograms => _thresholdKilograms;

        public event Action<bool> PressedChanged;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_resting.Contains(other)) _resting.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            _resting.Remove(other);
        }

        private void FixedUpdate()
        {
            RestingKilograms = SumMass();

            bool pressed = RestingKilograms >= _thresholdKilograms || (_latching && IsPressed);
            if (pressed == IsPressed) return;

            IsPressed = pressed;
            PressedChanged?.Invoke(IsPressed);
        }

        private float SumMass()
        {
            float total = 0f;

            for (int i = _resting.Count - 1; i >= 0; i--)
            {
                Collider collider = _resting[i];
                if (collider == null || !collider.gameObject.activeInHierarchy)
                {
                    _resting.RemoveAt(i);
                    continue;
                }

                total += MassOf(collider);
            }

            return total;
        }

        private static float MassOf(Collider collider)
        {
            PrimitiveCarrier carrier = collider.GetComponentInParent<PrimitiveCarrier>();

            if (carrier != null && carrier.Subject == PrimitiveSubject.Player)
            {
                return MassProfile.For(carrier.Current.Mass).Kilograms;
            }

            Rigidbody body = collider.attachedRigidbody;
            return body != null && !body.isKinematic ? body.mass : 0f;
        }
    }
}
