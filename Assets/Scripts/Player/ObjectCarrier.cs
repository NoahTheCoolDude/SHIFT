using Shift.Core;
using Shift.Shared;
using Shift.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shift.Player
{
    /// <summary>
    /// Left-click to pick up a loose rigidbody, left-click again to drop it. The carried body
    /// is driven by velocity toward a hold point rather than parented, so it keeps colliding
    /// with the world and can still be crushed, wedged, or knocked out of the player's hands.
    /// </summary>
    public class ObjectCarrier : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private CrosshairView _crosshair;

        [Header("Pickup")]
        [SerializeField] private float _pickupRange = 3f;
        [SerializeField] private float _holdDistance = 2f;
        [SerializeField] private float _maxCarryMass = 20f;
        [SerializeField] private LayerMask _pickupLayers = ~0;

        [Header("Carry")]
        [SerializeField] private float _followStrength = 12f;
        [SerializeField] private float _maxCarrySpeed = 12f;
        [SerializeField] private float _breakDistance = 3.5f;

        [Header("Prompts")]
        [SerializeField] private string _pickupPrompt = "[LMB]  Pick Up";
        [SerializeField] private string _dropPrompt = "[LMB]  Drop";

        private static readonly RaycastHit[] _probeHits = new RaycastHit[8];

        private Rigidbody _ownBody;
        private Rigidbody _carried;
        private Rigidbody _target;
        private RigidbodyPrimitiveApplier _carriedApplier;
        private PrimitiveCarrier _primitives;

        public bool IsCarrying => _carried != null;

        /// <summary>A heavier player carries more, as a consequence of MASS rather than a special case.</summary>
        private float CarryMassLimit =>
            _maxCarryMass * MassProfile.For(CurrentState.Mass).CarryScale;

        private PrimitiveState CurrentState =>
            _primitives != null ? _primitives.Current : PrimitiveState.Normal;

        private void Awake()
        {
            _ownBody = GetComponent<Rigidbody>();
            _primitives = GetComponent<PrimitiveCarrier>();

            // Resolved here rather than required in the inspector, so the component works on a
            // player that was assembled by hand.
            if (_cameraPivot == null)
            {
                Camera camera = GetComponentInChildren<Camera>();
                if (camera != null) _cameraPivot = camera.transform;
            }

            if (_crosshair == null) _crosshair = HudRoot.GetOrCreate();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || _cameraPivot == null) return;

            // Guarded rather than disabled: OnDisable drops whatever is being carried, and
            // pausing should not make you fumble the crate.
            if (UiInputGate.Suppressed) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (_carried != null) Drop();
                else if (_target != null) PickUp(_target);
            }

            // Probed every frame rather than only on click, since the highlight and prompt
            // have to track the crosshair continuously.
            SetTarget(_carried != null ? null : FindCandidate());
            RefreshPrompt();
        }

        private void FixedUpdate()
        {
            if (_carried == null) return;

            Vector3 target = _cameraPivot.position + _cameraPivot.forward * _holdDistance;
            Vector3 delta = target - _carried.position;

            // Something has wedged or blocked the box — let go rather than fighting the solver.
            if (delta.sqrMagnitude > _breakDistance * _breakDistance)
            {
                Drop();
                return;
            }

            _carried.linearVelocity = Vector3.ClampMagnitude(delta * _followStrength, _maxCarrySpeed);
            _carried.angularVelocity = Vector3.zero;
        }

        private void OnDisable()
        {
            SetTarget(null);
            if (_carried != null) Drop();
            if (_crosshair != null) _crosshair.HidePrompt();
        }

        /// <summary>
        /// Single definition of what counts as carryable, shared by the probe and the click.
        /// The ray starts at eye height — inside the player's own capsule — so self-hits are
        /// discarded explicitly instead of relying on how PhysX treats a ray born inside a
        /// collider. The nearest surviving hit wins even when it is not carryable, so nothing
        /// can be grabbed through a wall.
        /// </summary>
        private Rigidbody FindCandidate()
        {
            // A ghost cannot touch props, so offering to grab one would be a lie the crosshair
            // tells you.
            int layers = _pickupLayers;
            if (CurrentState.Ghost) layers &= ~ShiftLayers.PropMask;

            Ray ray = new Ray(_cameraPivot.position, _cameraPivot.forward);
            int count = Physics.RaycastNonAlloc(ray, _probeHits, _pickupRange, layers,
                QueryTriggerInteraction.Ignore);

            Rigidbody nearestBody = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _probeHits[i];
                Rigidbody body = hit.collider.attachedRigidbody;

                if (_ownBody != null && body == _ownBody) continue;
                if (hit.distance >= nearestDistance) continue;

                nearestDistance = hit.distance;
                nearestBody = body != null && !body.isKinematic && body.mass <= CarryMassLimit
                    ? body
                    : null;
            }

            return nearestBody;
        }

        private void SetTarget(Rigidbody candidate)
        {
            if (candidate == _target) return;

            SetHighlight(_target, false);
            _target = candidate;
            SetHighlight(_target, true);
        }

        private static void SetHighlight(Rigidbody body, bool visible)
        {
            if (body == null) return;

            HighlightOutline outline = body.GetComponent<HighlightOutline>();
            if (outline == null)
            {
                if (!visible) return;
                outline = body.gameObject.AddComponent<HighlightOutline>();
            }

            outline.SetVisible(visible);
        }

        private void RefreshPrompt()
        {
            if (_crosshair == null) return;

            if (_carried != null) _crosshair.ShowPrompt(_dropPrompt);
            else if (_target != null) _crosshair.ShowPrompt(_pickupPrompt);
            else _crosshair.HidePrompt();
        }

        private void PickUp(Rigidbody body)
        {
            SetTarget(null);

            _carried = body;

            // Toggling useGravity stopped meaning anything once gravity moved out of PhysX and
            // into the appliers — a prop with an applier would keep falling out of your hands.
            _carriedApplier = body.GetComponent<RigidbodyPrimitiveApplier>();
            if (_carriedApplier != null) _carriedApplier.SuspendGravity(true);
            else body.useGravity = false;
        }

        private void Drop()
        {
            if (_carried == null) return;

            if (_carriedApplier != null) _carriedApplier.SuspendGravity(false);
            else _carried.useGravity = true;

            _carriedApplier = null;
            _carried = null;
        }
    }
}
