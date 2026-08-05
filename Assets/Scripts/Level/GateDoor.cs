using UnityEngine;

namespace Shift.Level
{
    /// <summary>
    /// A slab that retracts while its plate is pressed.
    /// </summary>
    /// <remarks>
    /// Kinematic rigidbody driven by MovePosition rather than a moving transform: a transform-only
    /// collider does not sweep, so a player standing on the door would be clipped through it
    /// instead of carried by it.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    public class GateDoor : MonoBehaviour
    {
        [SerializeField] private MassPlate _plate;

        [Tooltip("Local offset applied when open. Downward retracts into the floor.")]
        [SerializeField] private Vector3 _openOffset = new Vector3(0f, -3.2f, 0f);

        [SerializeField] private float _speed = 4f;

        private Rigidbody _body;
        private Vector3 _closedPosition;
        private Vector3 _openPosition;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;

            _closedPosition = transform.position;
            _openPosition = _closedPosition + _openOffset;
        }

        private void FixedUpdate()
        {
            bool open = _plate != null && _plate.IsPressed;
            Vector3 target = open ? _openPosition : _closedPosition;

            if (_body.position == target) return;

            _body.MovePosition(Vector3.MoveTowards(_body.position, target, _speed * Time.fixedDeltaTime));
        }
    }
}
