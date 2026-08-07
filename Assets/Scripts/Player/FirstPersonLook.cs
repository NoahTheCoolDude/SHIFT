using Shift.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shift.Player
{
    public class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private float _mouseSensitivity = 0.1f;
        [SerializeField] private float _minPitch = -85f;
        [SerializeField] private float _maxPitch = 85f;

        private float _pitch;

        /// <summary>
        /// Yaw lives in the body transform and comes back for free when a respawn sets the
        /// rigidbody's rotation. Pitch does not — without this you respawn staring at the floor
        /// you just fell through.
        /// </summary>
        public void SetPitch(float pitch)
        {
            _pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
            if (_cameraPivot != null) _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || _cameraPivot == null) return;
            if (UiInputGate.Suppressed) return;

            Vector2 delta = mouse.delta.ReadValue() * _mouseSensitivity;

            transform.Rotate(Vector3.up * delta.x);

            _pitch = Mathf.Clamp(_pitch - delta.y, _minPitch, _maxPitch);
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
