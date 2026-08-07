using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shift.Net.Spike
{
    /// <summary>
    /// On-screen readout of tick rate, RTT and bandwidth. Throwaway spike instrumentation.
    /// </summary>
    /// <remarks>
    /// The Phase 0 pass criteria include "no visible jitter" and "no desync after 10 minutes", and
    /// neither can be judged by eye. This exists so the spike produces numbers instead of an
    /// impression — a spike that yields a vibe has failed at its only job.
    ///
    /// Measured ticks/sec is shown next to the configured rate on purpose: if the two diverge, the
    /// simulation is starving and every jitter observation made that session is worthless.
    /// </remarks>
    [DisallowMultipleComponent]
    public class SpikeNetStats : MonoBehaviour
    {
        [SerializeField] private bool _visible = true;
        [SerializeField] private Key _toggleKey = Key.F3;

        [Tooltip("TEMPORARY: local player's grounded state and the collider underfoot. Added to diagnose a jump that never fired; delete once the ground probe is trusted.")]
        [SerializeField] private bool _showGroundDebug = true;

        private SpikePlayerController _localPlayer;
        private SpikePlatformRider _localRider;
        private SpikeDesyncDetector _desyncDetector;

        private NetworkTransport _subscribed;
        private long _bytesReceived;
        private long _lastBytesReceived;
        private long _lastBytesSent;
        private float _windowStart;
        private float _inPerSec;
        private float _outPerSec;

        private int _ticks;
        private float _tickWindowStart;
        private float _measuredTickRate;
        private bool _tickHooked;

        private readonly StringBuilder _builder = new StringBuilder(256);

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_toggleKey].wasPressedThisFrame) _visible = !_visible;

            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null) return;

            EnsureSubscriptions(manager);
            SampleBandwidth(manager);
            SampleTickRate();
        }

        /// <summary>
        /// Hooks are (re)bound lazily because the transport is chosen at runtime and the tick system
        /// does not exist until the NetworkManager starts.
        /// </summary>
        private void EnsureSubscriptions(NetworkManager manager)
        {
            NetworkTransport transport = manager.NetworkConfig != null ? manager.NetworkConfig.NetworkTransport : null;

            if (!ReferenceEquals(transport, _subscribed))
            {
                if (_subscribed != null) _subscribed.OnTransportEvent -= OnTransportEvent;
                if (transport != null) transport.OnTransportEvent += OnTransportEvent;
                _subscribed = transport;
            }

            if (!_tickHooked && manager.IsListening && manager.NetworkTickSystem != null)
            {
                manager.NetworkTickSystem.Tick += OnTick;
                _tickHooked = true;
            }
            else if (_tickHooked && !manager.IsListening)
            {
                _tickHooked = false;
                _measuredTickRate = 0f;
            }
        }

        // Inbound is metered here rather than in the transport subclasses because Facepunch delivers
        // received data through InvokeOnTransportEvent, which has no override point.
        private void OnTransportEvent(NetworkEvent eventType, ulong clientId, System.ArraySegment<byte> payload, float receiveTime)
        {
            if (eventType == NetworkEvent.Data) _bytesReceived += payload.Count;
        }

        private void OnTick() => _ticks++;

        private void SampleBandwidth(NetworkManager manager)
        {
            if (Time.unscaledTime - _windowStart < 1f) return;

            long sent = manager.NetworkConfig?.NetworkTransport is ISpikeSendCounter counter ? counter.BytesSent : 0L;

            float span = Time.unscaledTime - _windowStart;
            _outPerSec = (sent - _lastBytesSent) / span;
            _inPerSec = (_bytesReceived - _lastBytesReceived) / span;

            _lastBytesSent = sent;
            _lastBytesReceived = _bytesReceived;
            _windowStart = Time.unscaledTime;
        }

        private void SampleTickRate()
        {
            if (Time.unscaledTime - _tickWindowStart < 1f) return;

            _measuredTickRate = _ticks / (Time.unscaledTime - _tickWindowStart);
            _ticks = 0;
            _tickWindowStart = Time.unscaledTime;
        }

        private void OnDestroy()
        {
            if (_subscribed != null) _subscribed.OnTransportEvent -= OnTransportEvent;

            NetworkManager manager = NetworkManager.Singleton;
            if (_tickHooked && manager != null && manager.NetworkTickSystem != null)
            {
                manager.NetworkTickSystem.Tick -= OnTick;
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;

            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null) return;

            _builder.Clear();
            _builder.AppendLine(manager.IsHost ? "HOST" : manager.IsServer ? "SERVER" : manager.IsClient ? "CLIENT" : "OFFLINE");
            _builder.AppendLine($"transport  {DescribeTransport(manager)}");
            _builder.AppendLine($"tick       {manager.NetworkConfig.TickRate} configured / {_measuredTickRate:F1} measured");
            _builder.AppendLine($"bytes/sec  in {_inPerSec,8:F0}   out {_outPerSec,8:F0}");

            if (manager.IsListening) AppendRtt(manager);
            if (_showGroundDebug) AppendGroundDebug(manager);
            if (manager.IsListening)
            {
                AppendRider();
                AppendDesync(manager);
            }

            GUI.Box(new Rect(10f, 10f, 400f, 186f), string.Empty);
            GUI.Label(new Rect(20f, 16f, 380f, 174f), _builder.ToString());
        }

        /// <summary>
        /// Attach latency is shown because parenting is server-authoritative: the request costs a
        /// round trip, and during that window the capsule is still in world space and will slip on a
        /// moving platform. Guessing at that cost is exactly what this line prevents.
        /// </summary>
        private void AppendRider()
        {
            if (_localRider == null)
            {
                _builder.AppendLine("platform   (no rider)");
                return;
            }

            string latency = _localRider.LastAttachMs >= 0f ? $"attach {_localRider.LastAttachMs:F0}ms" : "never attached";
            string state = _localRider.RiderEnabled ? string.Empty : "  [RIDER OFF]";
            _builder.AppendLine($"platform   {_localRider.PlatformName}   {latency}{state}");
        }

        private void AppendDesync(NetworkManager manager)
        {
            if (_desyncDetector == null) _desyncDetector = FindFirstObjectByType<SpikeDesyncDetector>();

            if (_desyncDetector == null)
            {
                _builder.AppendLine("desync     (no detector)");
                return;
            }

            // The host is the reference and SendTo.NotServer excludes it from its own broadcast, so
            // it never compares. Saying so beats "awaiting first broadcast" forever, and beats a
            // zero that would read as "verified in sync".
            if (manager.IsServer)
            {
                _builder.AppendLine("desync     broadcasting (host is the reference)");
                return;
            }

            if (!_desyncDetector.HasCompared)
            {
                _builder.AppendLine("desync     awaiting first broadcast");
                return;
            }

            _builder.AppendLine(
                $"desync     last {_desyncDetector.LastMaxDeviation:F3}m   worst {_desyncDetector.WorstDeviation:F3}m " +
                $"on {_desyncDetector.WorstBody} @ {_desyncDetector.WorstAt:F0}s  (n={_desyncDetector.LastComparedCount})");
        }

        /// <summary>
        /// TEMPORARY. Exists because a jump that silently does nothing is indistinguishable from a
        /// jump that is never requested — this separates the two by showing the ground probe's
        /// actual result rather than inviting a guess.
        /// </summary>
        private void AppendGroundDebug(NetworkManager manager)
        {
            if (_localPlayer == null && manager.IsListening && manager.SpawnManager != null)
            {
                NetworkObject player = manager.SpawnManager.GetLocalPlayerObject();
                if (player != null)
                {
                    _localPlayer = player.GetComponent<SpikePlayerController>();
                    _localRider = player.GetComponent<SpikePlatformRider>();
                }
            }

            if (_localPlayer == null)
            {
                _builder.AppendLine("ground     (no local player)");
                return;
            }

            Collider under = _localPlayer.GroundCollider;
            _builder.AppendLine(_localPlayer.IsGrounded
                ? $"ground     GROUNDED on {(under != null ? under.name : "?")}"
                : "ground     airborne");
        }

        private void AppendRtt(NetworkManager manager)
        {
            NetworkTransport transport = manager.NetworkConfig.NetworkTransport;

            // FacepunchTransport.GetCurrentRtt() is a hardcoded 0 upstream, and the real value lives
            // behind Steam's internal SteamNetworkingQuickConnectionStatus. Say "unavailable" rather
            // than print a zero somebody will later mistake for a perfect connection.
            bool rttAvailable = transport is SpikeUnityTransport;

            if (!rttAvailable)
            {
                _builder.AppendLine("rtt        unavailable on this transport");
                return;
            }

            if (manager.IsServer)
            {
                _builder.Append("rtt        ");
                if (manager.ConnectedClientsIds.Count == 0) _builder.Append("(no clients)");
                foreach (ulong id in manager.ConnectedClientsIds)
                {
                    if (id == NetworkManager.ServerClientId) continue;
                    _builder.Append($"#{id}:{transport.GetCurrentRtt(id)}ms  ");
                }
                _builder.AppendLine();
            }
            else
            {
                _builder.AppendLine($"rtt        {transport.GetCurrentRtt(NetworkManager.ServerClientId)}ms to host");
            }
        }

        private static string DescribeTransport(NetworkManager manager)
        {
            NetworkTransport transport = manager.NetworkConfig != null ? manager.NetworkConfig.NetworkTransport : null;
            if (transport == null) return "(none)";
            return transport is SpikeFacepunchTransport ? "Facepunch/SDR" : transport is SpikeUnityTransport ? "UnityTransport" : transport.GetType().Name;
        }
    }
}
