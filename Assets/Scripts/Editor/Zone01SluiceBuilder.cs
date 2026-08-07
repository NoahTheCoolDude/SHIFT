using System.Collections.Generic;
using System.IO;
using Shift.Core;
using Shift.Level;
using Shift.Modifiers;
using Shift.Progression;
using Shift.Shared;
using Shift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shift.EditorTools
{
    /// <summary>
    /// Builds Zone 01 "Sluice" — an Act 2 Combination level that cannot be finished without
    /// swapping between Frog, Heavy and Ghost.
    /// </summary>
    /// <remarks>
    /// Every gate is enforced by code that already ships: the well depth beats every jump except
    /// Frog's landing bounce, ObjectCarrier's mass limit hides the crate prompt from anyone but
    /// Heavy, and the grate sits on the Prop layer that only PHASE excludes. No new tuning
    /// constants, and no tutorial required — CLAUDE.md rule 4 for free.
    /// </remarks>
    public static class Zone01SluiceBuilder
    {
        private const string SceneFolder = "Assets/Scenes/Zones";
        private const string ScenePath = SceneFolder + "/Zone01_Sluice.unity";
        private const string ManifestPath = "Assets/Data/Zones/Zone01_Sluice.asset";

        private static readonly string[] SplitLabels = { "Well Lip", "Under-Ledge", "Vault", "Alcove" };

        [MenuItem("SHIFT/Build Zone 01 (Sluice)")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Overwrite Zone 01?",
                    ScenePath + " already exists and will be replaced. Any hand-tuning in it is lost.",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ShiftLayers.WarnIfMissing(null);

            FieldDefinition lowGravity = PrimitiveAssetFactory.LowGravity();
            ZoneManifest manifest = CreateManifest();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            RunDirector director = CreateDirector(manifest);

            Transform startPose = BuildStart(director);
            BuildWell(director);
            BuildRamp();
            GameObject props = BuildVault(director);
            BuildShaft(director, lowGravity);
            BuildKillVolume(director);

            CrosshairView crosshair = HudRoot.GetOrCreate();
            GameObject player = SandboxParts.CreatePlayer();
            player.transform.position = startPose.position;
            SandboxParts.AttachCamera(player, crosshair);
            MovementSandboxBuilder.EnsurePlayerPrimitives(player);
            AttachRunComponents(player, director, startPose);

            SandboxParts.Bind(props.AddComponent<PropResetter>(), "_director", director);

            Directory.CreateDirectory(SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings(ScenePath);
            AssetDatabase.Refresh();

            Debug.Log(
                "Zone 01 (Sluice) built at " + ScenePath + ". Press Play.\n" +
                "Route: 2 Frog to bounce out of the well, 1 Heavy to carry the crate onto the plate, " +
                "3 Ghost to pass the grate, then float the low-gravity shaft to the goal.\n" +
                "R respawns at the last checkpoint, Shift+R restarts the run, Esc pauses (the clock keeps running).");
        }

        private static ZoneManifest CreateManifest()
        {
            ZoneManifest existing = AssetDatabase.LoadAssetAtPath<ZoneManifest>(ManifestPath);
            if (existing != null) return existing;

            ZoneManifest manifest = ScriptableObject.CreateInstance<ZoneManifest>();
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath));
            AssetDatabase.CreateAsset(manifest, ManifestPath);

            SerializedObject serialized = new SerializedObject(manifest);
            serialized.FindProperty("_zoneId").stringValue = "zone01_sluice";
            serialized.FindProperty("_displayName").stringValue = "Sluice";
            serialized.FindProperty("_parSeconds").floatValue = 75f;

            SerializedProperty labels = serialized.FindProperty("_splitLabels");
            labels.arraySize = SplitLabels.Length;
            for (int i = 0; i < SplitLabels.Length; i++)
            {
                labels.GetArrayElementAtIndex(i).stringValue = SplitLabels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manifest);

            return manifest;
        }

        private static RunDirector CreateDirector(ZoneManifest manifest)
        {
            GameObject host = new GameObject("RunDirector");
            RunDirector director = host.AddComponent<RunDirector>();
            SandboxParts.Bind(director, "_manifest", manifest);

            return director;
        }

        /// <summary>Start pad. The clock starts when you leave it, so choosing a loadout is free.</summary>
        private static Transform BuildStart(RunDirector director)
        {
            Transform group = ZoneParts.Group("Start").transform;

            ZoneParts.Slab("StartFloor", group, new Vector3(0f, -0.25f, 3f),
                new Vector3(12f, 0.5f, 14f), ZoneParts.Neutral);

            Transform spawn = ZoneParts.Marker("SpawnPose", group, new Vector3(0f, 0.1f, 0f));

            GameObject start = ZoneParts.TriggerBox("ZoneStart", group, new Vector3(0f, 1.5f, 0f),
                new Vector3(6f, 3f, 6f));

            ZoneStart component = start.AddComponent<ZoneStart>();
            SandboxParts.Bind(component, "_director", director);
            SandboxParts.Bind(component, "_spawnPose", spawn);

            return spawn;
        }

        /// <summary>
        /// A 10m roofed shaft whose only forward exit is a ledge 7.5m above its floor. Jump
        /// (1.3m) plus a full jetpack burn falls well short; Frog's landing bounce returns ~81%
        /// of the drop and clears it with metres to spare. Gates on choice, not precision.
        /// </summary>
        private static void BuildWell(RunDirector director)
        {
            Transform group = ZoneParts.Group("Well").transform;

            ZoneParts.Slab("WellFloor", group, new Vector3(0f, -10.25f, 14f),
                new Vector3(8f, 0.5f, 8f), ZoneParts.Neutral);

            // Walls y -10 .. +4, so the shaft cannot simply be jumped over from the start pad.
            ZoneParts.Slab("WellWall_West", group, new Vector3(-4.25f, -3f, 14f),
                new Vector3(0.5f, 14f, 8f), ZoneParts.Neutral);
            ZoneParts.Slab("WellWall_East", group, new Vector3(4.25f, -3f, 14f),
                new Vector3(0.5f, 14f, 8f), ZoneParts.Neutral);
            ZoneParts.Slab("WellRoof", group, new Vector3(0f, 4.25f, 14f),
                new Vector3(9f, 0.5f, 8f), ZoneParts.Neutral);

            // The far wall stops at the ledge lip, leaving the exit at y = -2.5.
            ZoneParts.Slab("WellWall_Far", group, new Vector3(0f, 1f, 18.25f),
                new Vector3(8f, 6f, 0.5f), ZoneParts.Neutral);
            ZoneParts.Slab("ExitLedge", group, new Vector3(0f, -2.75f, 17f),
                new Vector3(8f, 0.5f, 3f), ZoneParts.Accent);

            Checkpoint(group, director, 0, new Vector3(0f, 1f, 9f), new Vector3(8f, 3f, 2f),
                new Vector3(0f, 0.1f, 8f));

            Checkpoint(group, director, 1, new Vector3(0f, -1.5f, 17f), new Vector3(6f, 3f, 2f),
                new Vector3(0f, -2.4f, 17f));
        }

        /// <summary>Climbs the 2.5m back to floor level over 16m — a walk, not a jump.</summary>
        private static void BuildRamp()
        {
            Transform group = ZoneParts.Group("Ramp").transform;

            GameObject ramp = ZoneParts.Slab("RampSurface", group, new Vector3(0f, -1.25f, 26f),
                new Vector3(6f, 0.5f, 16.5f), ZoneParts.Neutral);
            ramp.transform.rotation = Quaternion.Euler(-8.9f, 0f, 0f);

            ZoneParts.Slab("RampWall_West", group, new Vector3(-3.25f, -0.5f, 26f),
                new Vector3(0.5f, 4f, 16f), ZoneParts.Neutral);
            ZoneParts.Slab("RampWall_East", group, new Vector3(3.25f, -0.5f, 26f),
                new Vector3(0.5f, 4f, 16f), ZoneParts.Neutral);
        }

        /// <summary>
        /// The crate weighs 30kg. ObjectCarrier's limit is 20kg × CarryScale, so Normal and Frog
        /// get 20, Feather 5, and only Heavy's 40 clears it — and the limit is applied before
        /// targeting, so as anyone else the crosshair never even offers the prompt.
        /// </summary>
        private static GameObject BuildVault(RunDirector director)
        {
            Transform group = ZoneParts.Group("Vault").transform;

            ZoneParts.Slab("VaultFloor", group, new Vector3(0f, -0.25f, 40f),
                new Vector3(12f, 0.5f, 12f), ZoneParts.Neutral);
            ZoneParts.Slab("VaultWall_West", group, new Vector3(-6.25f, 2.5f, 40f),
                new Vector3(0.5f, 6f, 12f), ZoneParts.Neutral);
            ZoneParts.Slab("VaultWall_East", group, new Vector3(6.25f, 2.5f, 40f),
                new Vector3(0.5f, 6f, 12f), ZoneParts.Neutral);
            ZoneParts.Slab("VaultRoof", group, new Vector3(0f, 5.75f, 40f),
                new Vector3(13f, 0.5f, 12f), ZoneParts.Neutral);

            // A ledge the sequence break uses to reach the field spilling over the far wall.
            ZoneParts.Slab("PipeLedge", group, new Vector3(-5f, 3.8f, 43f),
                new Vector3(2f, 0.4f, 4f), ZoneParts.Accent);

            Checkpoint(group, director, 2, new Vector3(0f, 1.5f, 35f), new Vector3(10f, 3f, 2f),
                new Vector3(0f, 0.1f, 35f));

            GameObject pedestal = ZoneParts.Slab("Pedestal", group, new Vector3(-4f, 0.75f, 40f),
                new Vector3(2.5f, 1.5f, 2.5f), ZoneParts.Neutral);

            GameObject plateObject = ZoneParts.TriggerBox("MassPlate", pedestal.transform,
                new Vector3(-4f, 1.85f, 40f), new Vector3(2.2f, 0.6f, 2.2f));
            MassPlate plate = plateObject.AddComponent<MassPlate>();

            GameObject doorObject = ZoneParts.Slab("GateDoor", group, new Vector3(0f, 2f, 46f),
                new Vector3(2f, 4f, 0.4f), ZoneParts.Hazard);
            doorObject.AddComponent<Rigidbody>();
            SandboxParts.Bind(doorObject.AddComponent<GateDoor>(), "_plate", plate);

            // Walls either side of the doorway, so the door is the only way through.
            ZoneParts.Slab("VaultWall_Far_L", group, new Vector3(-4f, 2.5f, 46f),
                new Vector3(4f, 6f, 0.5f), ZoneParts.Neutral);
            ZoneParts.Slab("VaultWall_Far_R", group, new Vector3(4f, 2.5f, 46f),
                new Vector3(4f, 6f, 0.5f), ZoneParts.Neutral);

            GameObject grate = ZoneParts.Slab("Grate", group, new Vector3(0f, 2f, 45.4f),
                new Vector3(2f, 4f, 0.3f), ZoneParts.Ghostly);
            if (ShiftLayers.IsConfigured) grate.layer = ShiftLayers.Prop;

            GameObject props = ZoneParts.Group("Props");
            CreateCrate(props.transform, new Vector3(4f, 0.7f, 38f));

            return props;
        }

        private static void CreateCrate(Transform parent, Vector3 position)
        {
            GameObject crate = ZoneParts.Slab("Crate_30kg", parent, position,
                new Vector3(1.2f, 1.2f, 1.2f), new Color(0.78f, 0.45f, 0.25f));
            if (ShiftLayers.IsConfigured) crate.layer = ShiftLayers.Prop;

            Rigidbody body = crate.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;

            PrimitiveCarrier carrier = crate.AddComponent<PrimitiveCarrier>();
            SerializedObject serialized = new SerializedObject(carrier);
            serialized.FindProperty("_subject").intValue = (int)PrimitiveSubject.Prop;
            serialized.FindProperty("_intrinsic.Set").intValue = (int)PrimitiveMask.Mass;
            serialized.FindProperty("_intrinsic.Mass").intValue = (int)MassClass.Light;
            serialized.FindProperty("_intrinsic.GravityDirection").vector3Value = Vector3.down;
            serialized.FindProperty("_intrinsic.GravityStrength").floatValue = 1f;
            serialized.FindProperty("_intrinsic.Friction").floatValue = PrimitiveState.NormalFriction;
            serialized.FindProperty("_intrinsic.TimeRate").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            crate.AddComponent<RigidbodyPrimitiveApplier>();
        }

        /// <summary>
        /// The victory lap. The field deliberately spills 3m back over the vault roof — that
        /// overhang is the blessed sequence break, reachable by a Frog player who knows it is
        /// there and skips the crate, plate, door and grate entirely.
        /// </summary>
        private static void BuildShaft(RunDirector director, FieldDefinition lowGravity)
        {
            Transform group = ZoneParts.Group("Shaft").transform;

            ZoneParts.Slab("ShaftFloor", group, new Vector3(0f, -0.25f, 52f),
                new Vector3(10f, 0.5f, 12f), ZoneParts.Neutral);
            ZoneParts.Slab("ShaftWall_West", group, new Vector3(-5.25f, 7f, 52f),
                new Vector3(0.5f, 15f, 12f), ZoneParts.Neutral);
            ZoneParts.Slab("ShaftWall_East", group, new Vector3(5.25f, 7f, 52f),
                new Vector3(0.5f, 15f, 12f), ZoneParts.Neutral);
            ZoneParts.Slab("ShaftWall_Far", group, new Vector3(0f, 7f, 58.25f),
                new Vector3(10f, 15f, 0.5f), ZoneParts.Neutral);

            Checkpoint(group, director, 3, new Vector3(0f, 1.5f, 47.5f), new Vector3(6f, 3f, 2f),
                new Vector3(0f, 0.1f, 48f));

            GameObject fieldObject = ZoneParts.TriggerBox("LowGravityField", group,
                new Vector3(0f, 7f, 50f), new Vector3(10f, 14f, 20f));
            SandboxParts.Bind(fieldObject.AddComponent<PrimitiveField>(), "_definition", lowGravity);

            ZoneParts.Slab("GoalPlatform", group, new Vector3(0f, 5f, 55f),
                new Vector3(6f, 0.4f, 4f), ZoneParts.Accent);

            GameObject goal = ZoneParts.TriggerBox("ZoneGoal", group, new Vector3(0f, 6.5f, 55f),
                new Vector3(6f, 3f, 4f));
            SandboxParts.Bind(goal.AddComponent<ZoneGoal>(), "_director", director);
        }

        /// <summary>On the Default layer, deliberately — a ghost must not fall through it forever.</summary>
        private static void BuildKillVolume(RunDirector director)
        {
            GameObject kill = ZoneParts.TriggerBox("KillVolume", null,
                new Vector3(0f, -18f, 26f), new Vector3(80f, 8f, 100f));

            SandboxParts.Bind(kill.AddComponent<KillVolume>(), "_director", director);
        }

        private static void Checkpoint(
            Transform parent, RunDirector director, int index,
            Vector3 triggerCentre, Vector3 triggerSize, Vector3 respawnPosition)
        {
            GameObject trigger = ZoneParts.TriggerBox("Checkpoint_" + index, parent, triggerCentre, triggerSize);
            Transform pose = ZoneParts.Marker("RespawnPose", trigger.transform, respawnPosition);

            ZoneCheckpoint checkpoint = trigger.AddComponent<ZoneCheckpoint>();
            SandboxParts.Bind(checkpoint, "_director", director);
            SandboxParts.Bind(checkpoint, "_respawnPose", pose);

            SerializedObject serialized = new SerializedObject(checkpoint);
            serialized.FindProperty("_index").intValue = index;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AttachRunComponents(GameObject player, RunDirector director, Transform startPose)
        {
            PlayerRespawner respawner = player.AddComponent<PlayerRespawner>();
            SandboxParts.Bind(respawner, "_startPose", startPose);

            SandboxParts.Bind(player.AddComponent<RunHotkeys>(), "_director", director);
        }

        /// <summary>Without this the Zone exists but is unreachable in a player build.</summary>
        private static void RegisterInBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != scenePath) continue;

                scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
