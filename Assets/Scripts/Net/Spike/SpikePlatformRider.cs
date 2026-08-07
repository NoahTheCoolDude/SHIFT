using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Shift.Net.Spike
{
    /// <summary>
    /// Parents the capsule to whatever networked rigidbody it is standing on, so riders stay put on
    /// every machine rather than only on the one simulating them.
    /// </summary>
    /// <remarks>
    /// This is the fix for Phase 0 pass criterion 3 — "a player can stand on a box another player is
    /// pushing". Without it the owner simulates its capsule against a smoothed, one-to-two-tick-stale
    /// proxy of the crate while the host sees the capsule's world position arriving late; the two
    /// views drift and the player slides off on one screen but not the other.
    ///
    /// Parenting rather than a hand-rolled offset because it makes consistency structural: once
    /// parented with InLocalSpace, each machine reconstructs the capsule's world position from its
    /// OWN copy of the platform, so a stale platform position moves the rider with it instead of
    /// leaving it behind.
    ///
    /// Reparenting is server-authoritative in client-server mode, which is why attach goes through an
    /// RPC and cannot be applied locally. That round trip is a real cost, surfaced as
    /// <see cref="LastAttachMs"/> rather than left to guesswork.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpikePlayerController))]
    [RequireComponent(typeof(NetworkTransform))]
    public class SpikePlatformRider : NetworkBehaviour
    {
        [Tooltip("Switch off at runtime to A/B the un-ridden failure. Forces an immediate detach.")]
        [SerializeField] private bool _riderEnabled = true;

        [Tooltip("Seconds grounded on the same platform before attaching. Without dwell, bouncy contact flips grounded every frame and spams the server at tick rate.")]
        [SerializeField] private float _attachDwell = 0.1f;

        [Tooltip("Seconds off the platform before detaching. Longer than attach dwell so a single bump does not drop the rider.")]
        [SerializeField] private float _detachGrace = 0.2f;

        [Tooltip("Seconds before a request that produced no parent change is retried.")]
        [SerializeField] private float _requestTimeout = 2f;

        private SpikePlayerController _controller;
        private NetworkTransform _networkTransform;

        private NetworkObject _platform;
        private NetworkObject _candidate;
        private float _candidateSince;
        private float _ungroundedSince = -1f;
        private bool _lastEnabledState;

        private bool _requestPending;
        private float _requestSentAt = -1f;

        /// <summary>Name of the platform currently ridden, or "none". For the spike HUD.</summary>
        public string PlatformName => _platform != null ? _platform.name : "none";

        /// <summary>Round trip of the last attach, in ms: request sent to parent change observed. -1 if never attached.</summary>
        public float LastAttachMs { get; private set; } = -1f;

        public bool RiderEnabled => _riderEnabled;

        private void Awake()
        {
            _controller = GetComponent<SpikePlayerController>();
            _networkTransform = GetComponent<NetworkTransform>();
            _lastEnabledState = _riderEnabled;
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;

            // A mid-run toggle must force a detach. Leaving a stale parent behind would make the A/B
            // compare three states instead of two.
            if (_riderEnabled != _lastEnabledState)
            {
                _lastEnabledState = _riderEnabled;
                if (!_riderEnabled && _platform != null) SendDetach();
            }

            if (!_riderEnabled) return;

            // A request that never produced a parent change (platform despawned mid-flight, server
            // refused) must not wedge the rider forever.
            if (_requestPending && Time.time - _requestSentAt > _requestTimeout) _requestPending = false;
            if (_requestPending) return;

            NetworkObject beneath = ResolvePlatform();

            if (beneath != null)
            {
                _ungroundedSince = -1f;

                if (beneath == _platform)
                {
                    _candidate = null;
                    return;
                }

                if (beneath != _candidate)
                {
                    _candidate = beneath;
                    _candidateSince = Time.time;
                    return;
                }

                if (Time.time - _candidateSince >= _attachDwell) SendAttach(beneath);
                return;
            }

            _candidate = null;
            if (_platform == null) return;

            if (_ungroundedSince < 0f) _ungroundedSince = Time.time;
            if (Time.time - _ungroundedSince >= _detachGrace) SendDetach();
        }

        /// <summary>
        /// The networked rigidbody underfoot, or null. The static floor has no NetworkObject and a
        /// networked object without a rigidbody cannot move, so neither is worth a parent.
        /// </summary>
        private NetworkObject ResolvePlatform()
        {
            if (!_controller.IsGrounded) return null;

            Collider ground = _controller.GroundCollider;
            if (ground == null) return null;

            NetworkObject candidate = ground.GetComponentInParent<NetworkObject>();
            if (candidate == null || !candidate.IsSpawned) return null;
            if (candidate == NetworkObject) return null;
            if (candidate.GetComponent<NetworkRigidbody>() == null) return null;

            return candidate;
        }

        private void SendAttach(NetworkObject platform)
        {
            _candidate = null;
            _requestPending = true;
            _requestSentAt = Time.time;
            RequestAttachRpc(new NetworkObjectReference(platform));
        }

        private void SendDetach()
        {
            _requestPending = true;
            _requestSentAt = Time.time;
            _ungroundedSince = -1f;
            RequestDetachRpc();
        }

        [Rpc(SendTo.Server)]
        private void RequestAttachRpc(NetworkObjectReference platformReference)
        {
            if (!platformReference.TryGet(out NetworkObject platform)) return;
            if (!platform.IsSpawned) return;

            if (!NetworkObject.TrySetParent(platform, true))
            {
                Debug.LogWarning($"[SpikePlatformRider] Server refused parenting {name} to {platform.name}.");
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestDetachRpc()
        {
            if (transform.parent == null) return;

            if (!NetworkObject.TrySetParent((NetworkObject)null, true))
            {
                Debug.LogWarning($"[SpikePlatformRider] Server refused unparenting {name}.");
            }
        }

        /// <summary>
        /// Fires on every peer once NGO replicates the parent change (AutoObjectParentSync).
        /// </summary>
        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            _platform = parentNetworkObject;

            // Every peer must agree which space the transform is expressed in. If only the owner
            // switched, it would push local-space values that proxies would apply as world
            // coordinates and the capsule would teleport to the platform's offset from the origin.
            if (_networkTransform != null) _networkTransform.InLocalSpace = parentNetworkObject != null;

            if (!IsOwner) return;

            _requestPending = false;

            if (parentNetworkObject != null && _requestSentAt >= 0f)
            {
                LastAttachMs = (Time.time - _requestSentAt) * 1000f;
            }

            _requestSentAt = -1f;
        }

        public override void OnNetworkDespawn()
        {
            _platform = null;
            _candidate = null;
            _requestPending = false;
        }
    }
}
