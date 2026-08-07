using Shift.Player;
using Shift.Shared;
using UnityEngine;

namespace Shift.Level
{
    /// <summary>
    /// Teleports the player to a checkpoint. Owns the pose; the motor owns its own bookkeeping.
    /// </summary>
    /// <remarks>
    /// Two writers on one Rigidbody is how you get "sometimes you keep your fall speed", so this
    /// sets position and velocity and then hands off to <see cref="FirstPersonMotor.ResetMotionState"/>
    /// rather than reaching into the motor's fields.
    /// </remarks>
    public class PlayerRespawner : MonoBehaviour
    {
        [SerializeField] private Rigidbody _body;
        [SerializeField] private FirstPersonMotor _motor;
        [SerializeField] private FirstPersonLook _look;
        [SerializeField] private PrimitiveCarrier _carrier;

        [Tooltip("Where a full restart sends the player. Falls back to the spawn pose at Awake.")]
        [SerializeField] private Transform _startPose;

        private Vector3 _checkpointPosition;
        private float _checkpointYaw;
        private Vector3 _startPosition;
        private float _startYaw;

        private void Awake()
        {
            if (_body == null) _body = GetComponent<Rigidbody>();
            if (_motor == null) _motor = GetComponent<FirstPersonMotor>();
            if (_look == null) _look = GetComponent<FirstPersonLook>();
            if (_carrier == null) _carrier = GetComponent<PrimitiveCarrier>();

            _startPosition = _startPose != null ? _startPose.position : transform.position;
            _startYaw = _startPose != null ? _startPose.eulerAngles.y : transform.eulerAngles.y;

            _checkpointPosition = _startPosition;
            _checkpointYaw = _startYaw;
        }

        public void SetCheckpoint(Vector3 position, float yawDegrees)
        {
            _checkpointPosition = position;
            _checkpointYaw = yawDegrees;
        }

        public void RespawnAtCheckpoint()
        {
            Respawn(_checkpointPosition, _checkpointYaw);
        }

        public void RespawnAtStart()
        {
            _checkpointPosition = _startPosition;
            _checkpointYaw = _startYaw;
            Respawn(_startPosition, _startYaw);
        }

        public void Respawn(Vector3 position, float yawDegrees)
        {
            if (_body == null) return;

            // Interpolation renders between the last two physics poses, so teleporting with it on
            // draws one frame of the player streaking across the level.
            RigidbodyInterpolation previous = _body.interpolation;
            _body.interpolation = RigidbodyInterpolation.None;

            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;

            // Through the body, not the transform — the transform is the solver's output.
            _body.position = position;
            _body.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

            // Colliders still occupy the old pose until this runs, so the motor's next ground
            // spherecast would sample the place we just left.
            Physics.SyncTransforms();

            _body.interpolation = previous;

            if (_motor != null) _motor.ResetMotionState();
            if (_look != null) _look.SetPitch(0f);

            // OnTriggerExit is a physics step away, and that window is long enough to respawn
            // still under a low-gravity field.
            if (_carrier != null) _carrier.ClearFields();
        }
    }
}
