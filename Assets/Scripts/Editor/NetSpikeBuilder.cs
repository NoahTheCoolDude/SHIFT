using System.IO;
using Shift.Net.Spike;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shift.EditorTools
{
    /// <summary>
    /// Builds the Phase 0 netcode spike scene: floor, 20 host-authoritative cubes, four spawn
    /// points, and a NetworkManager carrying both transports.
    /// </summary>
    /// <remarks>
    /// A builder rather than a hand-authored scene because every other scene here is generated, and
    /// this is the one most likely to be rebuilt as the spike changes. A hand-authored NetSpike
    /// would be the only scene nobody could regenerate.
    /// </remarks>
    public static class NetSpikeBuilder
    {
        private const string SceneFolder = "Assets/Scenes/Sandbox";
        private const string ScenePath = SceneFolder + "/NetSpike.unity";
        private const string PrefabFolder = "Assets/Scripts/Net/Spike/Prefabs";
        private const string PlayerPrefabPath = PrefabFolder + "/SpikePlayer.prefab";

        private const int CubeCount = 20;
        private const int CubeColumns = 5;

        [MenuItem("SHIFT/Build Net Spike Scene")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Overwrite Net Spike?",
                    ScenePath + " already exists and will be replaced. Any hand-tuning in it is lost.",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject playerPrefab = BuildPlayerPrefab();

            CreateFloor();
            CreateCubes();
            SpikeSpawnPoints spawns = CreateSpawnPoints();
            CreateDesyncDetector();
            CreateNetworkManager(playerPrefab);

            Directory.CreateDirectory(SceneFolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Net spike built at {ScenePath}. Press Play, then Host (UnityTransport is the default). " +
                "For a second peer, make a build and run it alongside the editor. WASD moves, space jumps, " +
                "mouse looks, Esc frees the cursor for the host/join panel, F3 toggles the stats readout. " +
                $"{CubeCount} cubes are host-authoritative; the capsule is owner-authoritative. " +
                "Stand on a cube to see the platform rider attach (toggle _riderEnabled on the player " +
                "prefab at runtime to A/B it), and watch the desync line on the client. " +
                (spawns != null ? "Four spawn points wired." : "WARNING: spawn points missing."));
        }

        private static void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(40f, 1f, 40f);
            floor.isStatic = true;

            // No NetworkObject: static geometry is identical on every peer and syncing it would be
            // pure waste.
        }

        /// <summary>
        /// Cubes are scene-placed NetworkObjects rather than spawned prefabs. NGO spawns scene
        /// objects automatically, which removes prefab registration from a throwaway scene.
        /// </summary>
        private static void CreateCubes()
        {
            int propLayer = LayerMask.NameToLayer("Prop");
            if (propLayer < 0)
            {
                Debug.LogWarning("[NetSpikeBuilder] No 'Prop' layer defined; cubes will stay on Default.");
            }

            GameObject group = new GameObject("Cubes");

            for (int i = 0; i < CubeCount; i++)
            {
                int column = i % CubeColumns;
                int row = i / CubeColumns;

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Cube_{i:00}";
                cube.transform.SetParent(group.transform);
                cube.transform.position = new Vector3(
                    (column - (CubeColumns - 1) * 0.5f) * 1.6f,
                    0.5f + row * 0.05f,
                    4f + row * 1.6f);

                if (propLayer >= 0) cube.layer = propLayer;

                Rigidbody body = cube.AddComponent<Rigidbody>();
                body.mass = 1f;
                body.interpolation = RigidbodyInterpolation.Interpolate;

                cube.AddComponent<NetworkObject>();

                NetworkTransform transform = cube.AddComponent<NetworkTransform>();
                ConfigureTransform(transform, NetworkTransform.AuthorityModes.Server);

                NetworkRigidbody networkBody = cube.AddComponent<NetworkRigidbody>();

                // Forces isKinematic on every non-authority peer. This is what makes "host simulates,
                // clients interpolate" actually true rather than merely intended.
                networkBody.AutoUpdateKinematicState = true;
                networkBody.UseRigidBodyForMotion = true;
            }
        }

        /// <summary>
        /// Shared NetworkTransform tuning. Scale is never synced (nothing rescales), and the
        /// thresholds are what make sleeping bodies fall silent: a resting rigidbody produces no
        /// delta past them, so it stops sending without any explicit "stop replicating" switch.
        /// </summary>
        private static void ConfigureTransform(NetworkTransform transform, NetworkTransform.AuthorityModes authority)
        {
            transform.AuthorityMode = authority;
            transform.Interpolate = true;

            transform.SyncScaleX = false;
            transform.SyncScaleY = false;
            transform.SyncScaleZ = false;

            transform.PositionThreshold = 0.001f;
            transform.RotAngleThreshold = 0.01f;
            transform.UseHalfFloatPrecision = true;
        }

        private static SpikeSpawnPoints CreateSpawnPoints()
        {
            GameObject root = new GameObject("SpawnPoints");
            SpikeSpawnPoints spawns = root.AddComponent<SpikeSpawnPoints>();

            Vector3[] positions =
            {
                new Vector3(-3f, 1.1f, -6f),
                new Vector3(-1f, 1.1f, -6f),
                new Vector3(1f, 1.1f, -6f),
                new Vector3(3f, 1.1f, -6f)
            };

            Transform[] points = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject point = new GameObject($"Spawn_{i}");
                point.transform.SetParent(root.transform);
                point.transform.position = positions[i];
                points[i] = point.transform;
            }

            BindArray(spawns, "_points", points);
            return spawns;
        }

        /// <summary>
        /// Scene-placed NetworkObject, so NGO spawns it automatically and its RPCs have an identity
        /// to travel on. The NetworkManager itself cannot carry them — it is not a NetworkObject.
        /// </summary>
        private static void CreateDesyncDetector()
        {
            GameObject detector = new GameObject("DesyncDetector");
            detector.AddComponent<NetworkObject>();
            detector.AddComponent<SpikeDesyncDetector>();
        }

        private static GameObject BuildPlayerPrefab()
        {
            Directory.CreateDirectory(PrefabFolder);

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "SpikePlayer";

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.mass = 70f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            player.AddComponent<NetworkObject>();

            NetworkTransform transform = player.AddComponent<NetworkTransform>();

            // Owner, not Server: the capsule simulates on the machine that controls it, so movement
            // has no input latency. The cubes stay Server-authoritative, and that split is the seam
            // pass criterion 3 exists to test.
            ConfigureTransform(transform, NetworkTransform.AuthorityModes.Owner);

            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform);
            pivot.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            Camera camera = pivot.AddComponent<Camera>();

            // Above the scene's Main Camera so the owner's view wins once spawned. No AudioListener:
            // a second one would only produce duplicate-listener warnings.
            camera.depth = 10f;
            pivot.SetActive(false);

            SpikePlayerController controller = player.AddComponent<SpikePlayerController>();
            Bind(controller, "_cameraPivot", pivot.transform);

            // Added after the controller so the RequireComponent chain is already satisfied.
            player.AddComponent<SpikePlatformRider>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);

            return prefab;
        }

        private static void CreateNetworkManager(GameObject playerPrefab)
        {
            GameObject root = new GameObject("NetworkManager");

            NetworkManager manager = root.AddComponent<NetworkManager>();
            SpikeUnityTransport unity = root.AddComponent<SpikeUnityTransport>();
            SpikeFacepunchTransport facepunch = root.AddComponent<SpikeFacepunchTransport>();
            SpikeNetworkBootstrap bootstrap = root.AddComponent<SpikeNetworkBootstrap>();
            root.AddComponent<SpikeNetStats>();

            // Mutated, not replaced: a fresh NetworkConfig would drop the field initialisers NGO
            // relies on (the NetworkPrefabs list among them) and null-reference at spawn time.
            NetworkConfig config = manager.NetworkConfig;

            // 30 is already NGO's default; set explicitly so nobody "fixes" it later. Physics stays
            // at the project's 50 Hz fixed timestep — the two are deliberately unequal.
            config.TickRate = 30;
            config.PlayerPrefab = playerPrefab;
            config.NetworkTransport = unity;
            config.EnableSceneManagement = true;

            Bind(bootstrap, "_unityTransport", unity);
            Bind(bootstrap, "_facepunchTransport", facepunch);

            EditorUtility.SetDirty(manager);
        }

        private static void RegisterInBuildSettings()
        {
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == ScenePath) return;
            }

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
            {
                // Scene-placed NetworkObjects are matched across peers during scene synchronisation,
                // which requires the scene to be in the build list.
                new EditorBuildSettingsScene(ScenePath, true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void Bind(Component target, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"[NetSpikeBuilder] {target.GetType().Name} has no serialized field '{fieldName}'.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindArray(Component target, string fieldName, Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"[NetSpikeBuilder] {target.GetType().Name} has no serialized field '{fieldName}'.");
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
