using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shift.Net.Spike
{
    /// <summary>
    /// Owner-authoritative capsule: walks, jumps, and shoves cubes by ordinary collision.
    /// Throwaway — Dev B owns the real character controller in Scripts/Player.
    /// </summary>
    /// <remarks>
    /// Owner-authoritative rather than host-simulated because CLAUDE.md forbids re-simulation, which
    /// rules out prediction-with-reconciliation; the alternative is eating full RTT on your own
    /// movement, which is unusable for a parkour game. The cost is that this capsule and the
    /// host-authoritative cubes live in different authority domains, and that seam is precisely what
    /// pass criterion 3 ("stand on a box another player is pushing") tests.
    ///
    /// Deliberately does NOT push cubes with explicit code. Shoving must emerge from ordinary PhysX
    /// contact, or the spike would be testing bespoke code rather than the physics sync it exists to
    /// evaluate.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class SpikePlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _acceleration = 45f;
        [SerializeField] private float _jumpImpulse = 5.5f;

        [Header("Ground check")]
        [Tooltip("How far below the capsule's feet to probe for ground.")]
        [SerializeField] private float _groundProbeDistance = 0.2f;

        [Tooltip("Probe sphere radius as a fraction of the capsule radius. Under 1 so the probe cannot catch on a wall the capsule is merely brushing.")]
        [SerializeField, Range(0.1f, 1f)] private float _groundProbeRadiusScale = 0.9f;

        [Header("Look")]
        [SerializeField] private float _lookSensitivity = 0.12f;
        [SerializeField] private float _pitchLimit = 85f;
        [SerializeField] private Transform _cameraPivot;

        private Rigidbody _rigidbody;
        private CapsuleCollider _capsule;
        private readonly RaycastHit[] _groundHits = new RaycastHit[8];
        private Vector2 _moveInput;
        private bool _jumpQueued;
        private float _pitch;
        private bool _grounded;

        /// <summary>True while the capsule is standing on something. Read by the platform rider.</summary>
        public bool IsGrounded => _grounded;

        /// <summary>The collider currently underfoot, or null. Read by the platform rider.</summary>
        public Collider GroundCollider { get; private set; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();

            // Physics must never tip the capsule over; yaw is driven explicitly by look input.
            _rigidbody.freezeRotation = true;
        }

        public override void OnNetworkSpawn()
        {
            // Proxies are moved by NetworkTransform, so local simulation on them would fight the
            // network state. Only the owner simulates.
            if (!IsOwner)
            {
                _rigidbody.isKinematic = true;
                if (_cameraPivot != null) _cameraPivot.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            if (_cameraPivot != null) _cameraPivot.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            MoveToSpawnPoint();
        }

        /// <summary>
        /// Owner places itself: with owner authority the owner's transform is the replicated one, so
        /// a server-side placement would be overwritten by the owner's first update anyway.
        /// </summary>
        private void MoveToSpawnPoint()
        {
            if (SpikeSpawnPoints.Instance == null) return;
            if (!SpikeSpawnPoints.Instance.TryGetSpawn((int)OwnerClientId, out Vector3 position, out Quaternion rotation)) return;

            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner) Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            if (!IsOwner) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Escape releases the cursor so the IMGUI host/join panel stays clickable.
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
            }

            _moveInput = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));

            if (keyboard.spaceKey.wasPressedThisFrame) _jumpQueued = true;

            if (Cursor.lockState == CursorLockMode.Locked) ApplyLook();
        }

        private void ApplyLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue() * _lookSensitivity;

            transform.Rotate(0f, delta.x, 0f, Space.Self);

            if (_cameraPivot == null) return;
            _pitch = Mathf.Clamp(_pitch - delta.y, -_pitchLimit, _pitchLimit);
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            RefreshGrounded();

            Vector3 wish = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 target = wish * _moveSpeed;

            // Acceleration rather than a velocity write, so cube collisions still push back on the
            // capsule instead of being overwritten every step.
            Vector3 correction = new Vector3(target.x - velocity.x, 0f, target.z - velocity.z);
            _rigidbody.AddForce(correction * _acceleration, ForceMode.Acceleration);

            if (_jumpQueued)
            {
                _jumpQueued = false;
                if (_grounded) _rigidbody.AddForce(Vector3.up * _jumpImpulse, ForceMode.VelocityChange);
            }
        }

        /// <summary>
        /// Probes just below the capsule's feet for standable ground.
        /// </summary>
        /// <remarks>
        /// Geometry is derived from the collider rather than hardcoded. The first version assumed a
        /// feet-pivot capsule and cast from 0.55 above the pivot for 0.70 units, which bottomed out
        /// half a unit above the feet of Unity's centre-pivot primitive — it never touched the floor,
        /// so the jump branch never ran. Deriving means resizing the capsule cannot resurrect that.
        /// </remarks>
        private void RefreshGrounded()
        {
            const float clearance = 0.02f;

            float radius = _capsule.radius * _groundProbeRadiusScale;
            float feetLocalY = _capsule.center.y - _capsule.height * 0.5f;

            // Sphere starts fractionally above the feet: a capsule at rest would otherwise begin the
            // sweep already touching the floor, which SphereCast reports as a useless zero-distance
            // overlap with no usable normal.
            Vector3 origin = transform.position + transform.up * (feetLocalY + clearance + radius);
            float distance = clearance + _groundProbeDistance;

            // QueryTriggerInteraction.Ignore so trigger volumes never read as floor.
            int count = Physics.SphereCastNonAlloc(
                origin,
                radius,
                -transform.up,
                _groundHits,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            _grounded = false;
            GroundCollider = null;
            float nearest = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                // The probe sphere sits inside our own capsule, so self-hits are guaranteed. The
                // original ~0 mask never filtered them.
                if (_groundHits[i].collider == _capsule) continue;
                if (_groundHits[i].distance >= nearest) continue;

                nearest = _groundHits[i].distance;
                _grounded = true;
                GroundCollider = _groundHits[i].collider;
            }
        }
    }
}
