using System;
using Unity.Netcode;
using UnityEngine;

namespace Shift.Net.Spike
{
    /// <summary>
    /// Picks a transport and starts a host or client. Throwaway spike UI — deleted when the real
    /// lobby flow lands.
    /// </summary>
    /// <remarks>
    /// Both transports sit on the NetworkManager and one is selected before starting, because Steam
    /// permits only one client per machine: every Facepunch test needs a second PC, so iterating on
    /// Facepunch alone would mean walking to the other computer for each change. UnityTransport over
    /// loopback carries the day-to-day loop; Facepunch is switched on for real-internet runs.
    ///
    /// The transport cannot be swapped once the NetworkManager is listening, so selection is locked
    /// out while connected rather than silently ignored.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public class SpikeNetworkBootstrap : MonoBehaviour
    {
        public enum TransportChoice
        {
            UnityTransport,
            Facepunch
        }

        [Tooltip("Overridden by the -transport unity|facepunch command line argument.")]
        [SerializeField] private TransportChoice _transport = TransportChoice.UnityTransport;

        [SerializeField] private SpikeUnityTransport _unityTransport;
        [SerializeField] private SpikeFacepunchTransport _facepunchTransport;

        private NetworkManager _manager;
        private string _targetSteamId = string.Empty;
        private string _lastError = string.Empty;

        private void Awake()
        {
            _manager = GetComponent<NetworkManager>();
            ApplyCommandLineOverride();
        }

        private void ApplyCommandLineOverride()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-transport", StringComparison.OrdinalIgnoreCase)) continue;

                string value = args[i + 1];
                if (string.Equals(value, "unity", StringComparison.OrdinalIgnoreCase))
                {
                    _transport = TransportChoice.UnityTransport;
                }
                else if (string.Equals(value, "facepunch", StringComparison.OrdinalIgnoreCase))
                {
                    _transport = TransportChoice.Facepunch;
                }
                else
                {
                    Debug.LogWarning($"[SpikeNetworkBootstrap] Unrecognised -transport '{value}'. Expected 'unity' or 'facepunch'.");
                    return;
                }

                Debug.Log($"[SpikeNetworkBootstrap] Transport set to {_transport} by command line.");
                return;
            }
        }

        /// <summary>
        /// Assigns the selected transport. Must run before StartHost/StartClient — NGO reads
        /// NetworkConfig.NetworkTransport once on start and never re-reads it.
        /// </summary>
        private bool ApplySelectedTransport()
        {
            NetworkTransport chosen = _transport == TransportChoice.Facepunch
                ? (NetworkTransport)_facepunchTransport
                : _unityTransport;

            if (chosen == null)
            {
                _lastError = $"{_transport} component is not assigned on the NetworkManager.";
                Debug.LogError("[SpikeNetworkBootstrap] " + _lastError);
                return false;
            }

            _manager.NetworkConfig.NetworkTransport = chosen;
            return true;
        }

        private void StartHost()
        {
            if (!ApplySelectedTransport()) return;
            _lastError = string.Empty;
            if (!_manager.StartHost()) _lastError = "StartHost failed — see console.";
        }

        private void StartClient()
        {
            if (!ApplySelectedTransport()) return;
            _lastError = string.Empty;

            if (_transport == TransportChoice.Facepunch)
            {
                if (!ulong.TryParse(_targetSteamId, out ulong steamId) || steamId == 0UL)
                {
                    _lastError = "Enter the host's SteamID64 before joining.";
                    return;
                }

                // App ID 480 is shared by every developer on Steam, so its lobby list is full of
                // strangers. Always join by explicit SteamID, never by browsing.
                _facepunchTransport.targetSteamId = steamId;
            }

            if (!_manager.StartClient()) _lastError = "StartClient failed — see console.";
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10f, 150f, 320f, 210f), GUI.skin.box);

            if (_manager.IsListening)
            {
                GUILayout.Label(_manager.IsHost ? "Hosting" : "Connected");
                if (GUILayout.Button("Disconnect")) _manager.Shutdown();
            }
            else
            {
                GUILayout.Label("Transport");
                _transport = GUILayout.Toggle(_transport == TransportChoice.UnityTransport, " UnityTransport (loopback)")
                    ? TransportChoice.UnityTransport
                    : TransportChoice.Facepunch;
                _transport = GUILayout.Toggle(_transport == TransportChoice.Facepunch, " Facepunch / Steam relay")
                    ? TransportChoice.Facepunch
                    : TransportChoice.UnityTransport;

                if (_transport == TransportChoice.Facepunch)
                {
                    GUILayout.Label("Host SteamID64");
                    _targetSteamId = GUILayout.TextField(_targetSteamId);
                }

                GUILayout.Space(6f);
                if (GUILayout.Button("Host")) StartHost();
                if (GUILayout.Button("Join")) StartClient();
            }

            if (!string.IsNullOrEmpty(_lastError))
            {
                GUILayout.Space(6f);
                GUILayout.Label(_lastError);
            }

            GUILayout.EndArea();
        }
    }
}
