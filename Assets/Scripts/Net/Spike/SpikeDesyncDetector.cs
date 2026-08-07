using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Shift.Net.Spike
{
    /// <summary>
    /// Host broadcasts every networked rigidbody's position once a second; clients compare against
    /// their own and record the magnitude and time of any divergence.
    /// </summary>
    /// <remarks>
    /// Exists to turn Phase 0 pass criterion 4 — "no desync after 10 minutes" — into a number.
    /// Watching cubes for ten minutes and declaring them fine is not evidence, and drift small
    /// enough to miss by eye is exactly the drift that matters over a long session.
    ///
    /// Positions are sent rather than a hash. A hash answers "did it diverge" but not "by how much",
    /// and magnitude is the whole point: 2mm of interpolation lag and 2m of genuine desync must not
    /// look alike. At twenty bodies the cost is ~400 B/s, far below anything the spike cares about.
    ///
    /// Object ids are sent alongside so clients match by identity rather than assuming both peers
    /// enumerate their spawned objects in the same order.
    /// </remarks>
    [DisallowMultipleComponent]
    public class SpikeDesyncDetector : NetworkBehaviour
    {
        [Tooltip("Seconds between host broadcasts.")]
        [SerializeField] private float _interval = 1f;

        [Tooltip("Divergence above this (metres) is logged. Below it is ordinary interpolation lag, not desync.")]
        [SerializeField] private float _tolerance = 0.05f;

        private readonly List<NetworkObject> _bodies = new List<NetworkObject>();
        private float _nextBroadcast;

        /// <summary>Largest divergence seen in the most recent comparison, in metres.</summary>
        public float LastMaxDeviation { get; private set; }

        /// <summary>Largest divergence seen at any point this session, in metres.</summary>
        public float WorstDeviation { get; private set; }

        /// <summary>Server time at which <see cref="WorstDeviation"/> occurred. Negative if never.</summary>
        public double WorstAt { get; private set; } = -1d;

        /// <summary>Name of the body responsible for <see cref="WorstDeviation"/>.</summary>
        public string WorstBody { get; private set; } = "-";

        /// <summary>How many bodies the last comparison covered.</summary>
        public int LastComparedCount { get; private set; }

        /// <summary>True once at least one comparison has run. Clients only.</summary>
        public bool HasCompared { get; private set; }

        private void Update()
        {
            if (!IsSpawned || !IsServer) return;
            if (Time.time < _nextBroadcast) return;
            _nextBroadcast = Time.time + _interval;

            CollectBodies();
            if (_bodies.Count == 0) return;

            ulong[] ids = new ulong[_bodies.Count];
            Vector3[] positions = new Vector3[_bodies.Count];

            for (int i = 0; i < _bodies.Count; i++)
            {
                ids[i] = _bodies[i].NetworkObjectId;
                positions[i] = _bodies[i].transform.position;
            }

            ReportPositionsRpc(ids, positions, NetworkManager.ServerTime.Time);
        }

        /// <summary>
        /// Every spawned networked rigidbody that is not a player. NetworkRigidbody is the
        /// discriminator because the spike's capsule deliberately does not have one — it is
        /// owner-authoritative and would not be a desync signal.
        /// </summary>
        private void CollectBodies()
        {
            _bodies.Clear();

            foreach (KeyValuePair<ulong, NetworkObject> entry in NetworkManager.SpawnManager.SpawnedObjects)
            {
                NetworkObject body = entry.Value;
                if (body == null || body.IsPlayerObject) continue;
                if (body.GetComponent<NetworkRigidbody>() == null) continue;

                _bodies.Add(body);
            }

            _bodies.Sort((a, b) => a.NetworkObjectId.CompareTo(b.NetworkObjectId));
        }

        [Rpc(SendTo.NotServer)]
        private void ReportPositionsRpc(ulong[] ids, Vector3[] positions, double serverTime)
        {
            if (ids == null || positions == null || ids.Length != positions.Length) return;

            float worst = 0f;
            string worstName = "-";
            int compared = 0;

            for (int i = 0; i < ids.Length; i++)
            {
                if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(ids[i], out NetworkObject local)) continue;
                if (local == null) continue;

                float deviation = Vector3.Distance(local.transform.position, positions[i]);
                compared++;

                if (deviation <= worst) continue;
                worst = deviation;
                worstName = local.name;
            }

            LastMaxDeviation = worst;
            LastComparedCount = compared;
            HasCompared = true;

            if (worst > WorstDeviation)
            {
                WorstDeviation = worst;
                WorstAt = serverTime;
                WorstBody = worstName;
            }

            if (worst > _tolerance)
            {
                Debug.LogWarning(
                    $"[SpikeDesync] {worst:F3}m on {worstName} at server t={serverTime:F1}s " +
                    $"({compared}/{ids.Length} bodies compared, tolerance {_tolerance:F3}m).");
            }
        }
    }
}
