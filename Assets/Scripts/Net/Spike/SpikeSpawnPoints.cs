using UnityEngine;

namespace Shift.Net.Spike
{
    /// <summary>
    /// Scene-placed spawn positions, indexed by client id so four players do not stack on the origin.
    /// </summary>
    /// <remarks>
    /// The owner teleports itself rather than the server placing it. With an owner-authoritative
    /// NetworkTransform the owner's position is the replicated one, so a server-side placement would
    /// be overwritten by the owner's first update.
    /// </remarks>
    [DisallowMultipleComponent]
    public class SpikeSpawnPoints : MonoBehaviour
    {
        [SerializeField] private Transform[] _points;

        public static SpikeSpawnPoints Instance { get; private set; }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Spawn pose for <paramref name="index"/>, wrapping if more players than points.</summary>
        public bool TryGetSpawn(int index, out Vector3 position, out Quaternion rotation)
        {
            if (_points == null || _points.Length == 0)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            Transform point = _points[((index % _points.Length) + _points.Length) % _points.Length];
            position = point.position;
            rotation = point.rotation;
            return true;
        }
    }
}
