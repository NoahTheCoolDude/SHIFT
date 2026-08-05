using Shift.Core;
using Shift.Modifiers;
using UnityEngine;

namespace Shift.Shared
{
    /// <summary>
    /// A trigger volume that pushes a <see cref="FieldDefinition"/> onto whatever enters it.
    /// </summary>
    /// <remarks>
    /// Must never sit on the Prop layer. PHASE works by setting excludeLayers, and excludeLayers
    /// suppresses trigger callbacks too — a ghost would pass through without this ever firing.
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public class PrimitiveField : MonoBehaviour
    {
        [SerializeField] private FieldDefinition _definition;

        [Tooltip("Higher wins when volumes overlap. Ties break by name, never by trigger order.")]
        [SerializeField] private int _priority;

        public int Priority => _priority;

        public PrimitiveOverride Override =>
            _definition != null ? _definition.Override : PrimitiveOverride.Empty;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PrimitiveCarrier carrier = FindCarrier(other);
            if (carrier != null) carrier.AddField(this);
        }

        private void OnTriggerExit(Collider other)
        {
            PrimitiveCarrier carrier = FindCarrier(other);
            if (carrier != null) carrier.RemoveField(this);
        }

        private void OnDisable()
        {
            // Destroying or disabling a field mid-play must not strand its effect on anyone still
            // standing inside it.
            PrimitiveCarrier[] carriers = FindObjectsByType<PrimitiveCarrier>(FindObjectsSortMode.None);
            for (int i = 0; i < carriers.Length; i++)
            {
                carriers[i].RemoveField(this);
            }
        }

        /// <summary>Resolves through the attached rigidbody so a child collider still finds the
        /// carrier on the body root.</summary>
        private static PrimitiveCarrier FindCarrier(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            return body != null
                ? body.GetComponent<PrimitiveCarrier>()
                : other.GetComponentInParent<PrimitiveCarrier>();
        }
    }
}
