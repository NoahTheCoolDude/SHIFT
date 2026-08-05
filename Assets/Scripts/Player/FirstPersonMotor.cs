using Shift.Core;
using Shift.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shift.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class FirstPersonMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PrimitiveCarrier _carrier;
        [SerializeField] private Transform _cameraPivot;

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 8.5f;

        [Tooltip("Control authority at FRICTION 0. Above zero so ice is slippery, not immovable.")]
        [SerializeField] private float _minimumGrip = 0.05f;

        [Header("Jump")]
        [SerializeField] private float _jumpHeight = 1.3f;
        [SerializeField] private float _fallGravityMultiplier = 2.6f;
        [SerializeField] private float _lowJumpGravityMultiplier = 2.2f;
        [SerializeField] private float _coyoteTime = 0.12f;
        [SerializeField] private float _jumpBufferTime = 0.12f;

        [Tooltip("Impact speed below which BOUNCE is ignored, so a bouncy player still settles.")]
        [SerializeField] private float _bounceThreshold = 2f;

        [Header("Jetpack")]
        [SerializeField] private float _jetpackAcceleration = 26f;
        [SerializeField] private float _jetpackDuration = 0.35f;
        [SerializeField] private float _jetpackMaxRiseSpeed = 5.5f;
        [SerializeField] private float _jetpackRefillSeconds = 1.4f;
        [SerializeField] private float _jetpackMinimumFuel = 0.08f;

        [Header("Crouch")]
        [SerializeField] private float _crouchHeight = 1.1f;
        [SerializeField] private float _crouchSpeed = 2.4f;
        [SerializeField] private float _crouchTransitionSpeed = 8f;

        [Header("Ground")]
        [SerializeField] private float _groundCheckDistance = 0.15f;
        [SerializeField] private LayerMask _groundLayers = ~0;

        private static readonly RaycastHit[] _groundHits = new RaycastHit[8];
        private static readonly Collider[] _headroomHits = new Collider[8];

        private Rigidbody _rigidbody;
        private CapsuleCollider _capsule;
        private PrimitiveState _state = PrimitiveState.Normal;
        private MassProfile _mass = MassProfile.For(MassClass.Normal);
        private float _standHeight;
        private float _standEyeHeight;
        private float _currentHeight;
        private bool _isCrouched;
        private Vector2 _moveInput;
        private bool _sprintHeld;
        private bool _crouchHeld;
        private bool _jumpHeld;
        private float _jumpBufferTimer;
        private float _coyoteTimer;
        private float _jetpackFuel;
        private bool _jetpackActive;
        private bool _isGrounded;
        private bool _wasGrounded;
        private float _previousVerticalSpeed;
        private bool _ghostApplied;

        /// <summary>The ground's PhysicsMaterial, or null when airborne or on unmarked geometry.</summary>
        private PhysicsMaterial _groundMaterial;

        private float _effectiveFriction = PrimitiveState.NormalFriction;
        private float _effectiveBounce;

        /// <summary>Only a deliberate jump may be cut short by releasing the key.</summary>
        private bool _jumpCutEligible;

        public bool IsGrounded => _isGrounded;
        public bool IsJetpackActive => _jetpackActive;
        public bool IsCrouched => _isCrouched;
        public PrimitiveState State => _state;

        /// <summary>Remaining thrust as 0–1, for HUD display.</summary>
        public float JetpackFuelNormalized =>
            _jetpackDuration <= 0f ? 0f : Mathf.Clamp01(_jetpackFuel / _jetpackDuration);

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();
            _rigidbody.freezeRotation = true;

            // Gravity is integrated here rather than by PhysX, so the GRAVITY primitive can change
            // its strength per entity. Everything downstream assumes this.
            _rigidbody.useGravity = false;

            if (_carrier == null) _carrier = GetComponent<PrimitiveCarrier>();

            _jetpackFuel = _jetpackDuration;
            _standHeight = _capsule.height;
            _currentHeight = _standHeight;

            // Resolved here as well as in the inspector, so crouch lowers the view even on a
            // player whose pivot was never wired up.
            if (_cameraPivot == null)
            {
                Camera camera = GetComponentInChildren<Camera>();
                if (camera != null)
                {
                    _cameraPivot = camera.transform.parent != null && camera.transform.parent != transform
                        ? camera.transform.parent
                        : camera.transform;
                }
            }

            _standEyeHeight = _cameraPivot != null ? _cameraPivot.localPosition.y : 0f;
        }

        /// <summary>
        /// Clears everything a respawn must not carry across. The two that matter most are
        /// <c>_previousVerticalSpeed</c> and <c>_wasGrounded</c>: without them a Frog player who
        /// respawns after a long fall lands on the spawn pad carrying the old impact speed and
        /// gets launched straight back off it.
        /// </summary>
        public void ResetMotionState()
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            _jetpackFuel = _jetpackDuration;
            _jetpackActive = false;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _jumpCutEligible = false;
            _previousVerticalSpeed = 0f;
            _wasGrounded = false;
            _groundMaterial = null;

            ClearInput();
        }

        private void ClearInput()
        {
            _moveInput = Vector2.zero;
            _sprintHeld = false;
            _crouchHeld = false;
            _jumpHeld = false;
            _jumpBufferTimer = 0f;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Must clear rather than simply return: these fields latch, so a bare early return
            // leaves the last frame's input held and a paused player keeps walking.
            if (UiInputGate.Suppressed)
            {
                ClearInput();
                return;
            }

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            _moveInput = new Vector2(x, z).normalized;

            _sprintHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            _crouchHeld = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            _jumpHeld = keyboard.spaceKey.isPressed;

            // Buffered so a press slightly before landing still fires, which is most of what
            // separates a jump that feels responsive from one that feels dropped.
            if (keyboard.spaceKey.wasPressedThisFrame)
                _jumpBufferTimer = _jumpBufferTime;
        }

        private void FixedUpdate()
        {
            // Polled rather than event-driven: the motor's maths is continuous, and polling
            // sidesteps any question of whether resolution ran before this frame.
            _state = _carrier != null ? _carrier.Current : PrimitiveState.Normal;
            _mass = MassProfile.For(_state.Mass);

            // TIME_RATE scales the motor's own integration. Zone and prop time dilation are
            // plumbed through the struct but not applied — per-entity time in one shared PhysX
            // scene has no native support, and faking it here would hide that.
            float timeRate = Mathf.Max(_state.TimeRate, 0.01f);
            float deltaTime = Time.fixedDeltaTime * timeRate;

            ApplyPhase();

            _isGrounded = CheckGrounded();
            if (_isGrounded)
            {
                _coyoteTimer = _coyoteTime;

                // Refills over time rather than instantly, so chaining ground jump into
                // jetpack costs you something.
                float refillPerSecond = _jetpackDuration / Mathf.Max(_jetpackRefillSeconds, 0.01f);
                _jetpackFuel = Mathf.Min(_jetpackFuel + refillPerSecond * deltaTime, _jetpackDuration);
            }
            else
            {
                _coyoteTimer -= deltaTime;
            }

            UpdateEffectiveSurface();
            ApplyLandingBounce();
            UpdateCrouch(deltaTime);
            ApplyMove(timeRate);
            ResolveJumpInput();
            ApplyJetpack(deltaTime);
            ApplyGravity(deltaTime, timeRate);

            _jumpBufferTimer -= deltaTime;
            _wasGrounded = _isGrounded;
            _previousVerticalSpeed = _rigidbody.linearVelocity.y;
        }

        /// <summary>
        /// PHASE via excludeLayers rather than by moving the player between layers: reversible,
        /// per-instance, and it leaves the ground raycast alone — so a ghost still stands on the
        /// floor while passing through props, which is what ghost means.
        /// </summary>
        private void ApplyPhase()
        {
            if (_state.Ghost == _ghostApplied) return;

            _ghostApplied = _state.Ghost;
            int excluded = _state.Ghost ? ShiftLayers.PropMask : 0;

            // Set on both: when a collider is attached to a rigidbody, Unity unions the two
            // exclusion masks, and setting only the collider is the less reliable half.
            _capsule.excludeLayers = excluded;
            _rigidbody.excludeLayers = excluded;
        }

        private bool CheckGrounded()
        {
            // A ghost still stands on the floor, but must not stand on a prop it is falling
            // through — the two halves have to agree or the player cannot predict either.
            int layers = _groundLayers;
            if (_state.Ghost) layers &= ~ShiftLayers.PropMask;

            Vector3 origin = transform.position + Vector3.up * (_capsule.radius + 0.05f);
            float castDistance = 0.05f + _groundCheckDistance;
            int count = Physics.SphereCastNonAlloc(origin, _capsule.radius * 0.9f, Vector3.down,
                _groundHits, castDistance, layers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                // The cast starts inside our own capsule, so self-hits must be discarded
                // or we would report grounded while airborne.
                if (_groundHits[i].collider.attachedRigidbody == _rigidbody) continue;

                _groundMaterial = _groundHits[i].collider.sharedMaterial;
                return true;
            }

            _groundMaterial = null;
            return false;
        }

        /// <summary>
        /// Combines the surface's material with the player's own primitives. The motor overwrites
        /// velocity every step, so PhysX friction on the capsule never gets a say — without this,
        /// standing on ice would feel identical to standing on concrete.
        /// </summary>
        /// <remarks>
        /// Unmarked geometry has no material and falls through to the raw primitive, which is what
        /// keeps the baseline sandbox behaving exactly as it did before surfaces existed.
        /// </remarks>
        private void UpdateEffectiveSurface()
        {
            _effectiveFriction = _state.Friction;
            _effectiveBounce = _state.Bounce;

            if (!_isGrounded || _groundMaterial == null) return;

            // Mirrors the combine modes the surface materials themselves declare.
            _effectiveFriction = (_state.Friction + _groundMaterial.dynamicFriction) * 0.5f;
            _effectiveBounce = Mathf.Max(_state.Bounce, _groundMaterial.bounciness);
        }

        /// <summary>
        /// BOUNCE has to be handled here rather than by the capsule's PhysicsMaterial, because
        /// ApplyMove overwrites velocity on the ground and would eat PhysX's restitution.
        /// </summary>
        private void ApplyLandingBounce()
        {
            if (!_isGrounded || _wasGrounded || _effectiveBounce <= 0.01f) return;

            float impactSpeed = -_previousVerticalSpeed;
            if (impactSpeed < _bounceThreshold) return;

            // Falls are accelerated but rises are not, so reflecting raw speed would return more
            // height than the drop had and every bounce would grow. Dividing out that asymmetry
            // makes BOUNCE mean the height ratio it looks like it means.
            float riseSpeed = impactSpeed * _effectiveBounce / Mathf.Sqrt(_fallGravityMultiplier);

            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.y = riseSpeed;
            _rigidbody.linearVelocity = velocity;

            // A passive bounce is not a jump, so releasing the jump key must not cut it short.
            _jumpCutEligible = false;

            // Leaving the ground again this step, so the jetpack and coyote bookkeeping agree
            // with what the player can see.
            _isGrounded = false;
        }

        private void UpdateCrouch(float deltaTime)
        {
            // Standing back up is refused while something is overhead, otherwise the capsule
            // would grow into geometry and get launched by the solver.
            _isCrouched = _crouchHeld || (_isCrouched && !HasHeadroomToStand());

            float targetHeight = _isCrouched ? _crouchHeight : _standHeight;
            _currentHeight = Mathf.MoveTowards(_currentHeight, targetHeight,
                _crouchTransitionSpeed * deltaTime);

            _capsule.height = _currentHeight;
            _capsule.center = new Vector3(0f, _currentHeight * 0.5f, 0f);

            if (_cameraPivot == null) return;

            Vector3 pivotPosition = _cameraPivot.localPosition;
            pivotPosition.y = _standEyeHeight * (_currentHeight / _standHeight);
            _cameraPivot.localPosition = pivotPosition;
        }

        private bool HasHeadroomToStand()
        {
            float radius = _capsule.radius * 0.95f;
            Vector3 bottom = transform.position + Vector3.up * _capsule.radius;
            Vector3 top = transform.position + Vector3.up * (_standHeight - _capsule.radius);

            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, _headroomHits,
                _groundLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (_headroomHits[i].attachedRigidbody == _rigidbody) continue;
                return false;
            }

            return true;
        }

        private void ApplyMove(float timeRate)
        {
            float speed = _isCrouched ? _crouchSpeed : (_sprintHeld ? _sprintSpeed : _walkSpeed);
            speed *= _mass.SpeedScale * timeRate;

            Vector3 direction = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            Vector3 targetVelocity = direction * speed;

            // At NORMAL friction this is exactly 1 and the lerp becomes the outright overwrite the
            // motor has always done. Lower friction hands control back to momentum.
            float grip = Mathf.Lerp(_minimumGrip, 1f,
                Mathf.Clamp01(_effectiveFriction / PrimitiveState.NormalFriction));

            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, grip);
            velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, grip);
            _rigidbody.linearVelocity = velocity;
        }

        private void ResolveJumpInput()
        {
            if (_jumpBufferTimer <= 0f) return;

            if (_coyoteTimer > 0f)
            {
                ApplyJump();
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }
            else if (_jetpackFuel >= _jetpackMinimumFuel && !_jetpackActive)
            {
                _jetpackActive = true;
                _jumpBufferTimer = 0f;
            }
        }

        private void ApplyJump()
        {
            // Solved against BASE gravity, not the resolved strength — so the launch speed is
            // constant and a low-gravity zone naturally carries you higher, which is the whole
            // point of standing in one.
            float height = _jumpHeight * _mass.JumpScale * (1f + _state.Bounce);
            float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * height);

            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.y = jumpVelocity;
            _rigidbody.linearVelocity = velocity;

            _jumpCutEligible = true;
        }

        private void ApplyJetpack(float deltaTime)
        {
            if (!_jetpackActive) return;

            if (!_jumpHeld || _jetpackFuel <= 0f)
            {
                _jetpackActive = false;
                return;
            }

            _jetpackFuel -= deltaTime;

            // Thrust is a force, so a heavier player accelerates less for the same engine.
            float acceleration = _jetpackAcceleration * _mass.ThrustScale;

            Vector3 velocity = _rigidbody.linearVelocity;
            if (velocity.y < _jetpackMaxRiseSpeed)
            {
                velocity.y = Mathf.Min(velocity.y + acceleration * deltaTime, _jetpackMaxRiseSpeed);
                _rigidbody.linearVelocity = velocity;
            }
        }

        /// <summary>
        /// Unity's gravity alone produces a floaty arc. Falling is accelerated, and releasing
        /// the key mid-rise cuts the jump short, which is what makes height feel controllable.
        /// </summary>
        /// <remarks>
        /// GRAVITY direction is honoured for the force itself, but the jump arc and ground cast
        /// still assume world-down. Full re-orientation lands with ADHESION.
        /// </remarks>
        private void ApplyGravity(float deltaTime, float timeRate)
        {
            // Baseline gravity always applies, including while grounded and while thrusting —
            // the multipliers are the only thing the airborne state changes.
            float multiplier = 1f;
            if (!_isGrounded && !_jetpackActive)
            {
                if (_rigidbody.linearVelocity.y < 0f)
                {
                    multiplier = _fallGravityMultiplier;
                    _jumpCutEligible = false;
                }
                else if (!_jumpHeld && _jumpCutEligible)
                {
                    multiplier = _lowJumpGravityMultiplier;
                }
            }

            // Acceleration scales with the square of time rate, so half-speed time produces a
            // consistently half-speed arc rather than a floaty one.
            Vector3 gravity = _state.GravityDirection *
                              (Physics.gravity.magnitude * _state.GravityStrength * multiplier * timeRate);

            _rigidbody.linearVelocity += gravity * deltaTime;
        }
    }
}
