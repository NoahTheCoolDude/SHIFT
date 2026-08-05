using System.IO;
using Shift.Core;
using Shift.Modifiers;
using Shift.Player;
using Shift.Shared;
using Shift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shift.EditorTools
{
    /// <summary>
    /// Builds the local movement test scene from scratch. Phase 0 only — this exists so the
    /// character controller can be tested without hand-assembling a hierarchy, and so the
    /// setup is reproducible instead of living in one dev's local scene file.
    /// </summary>
    /// <remarks>
    /// Deliberately kept free of primitive-engine content. This scene is the pristine feel
    /// baseline the motor refactor is measured against; put primitive test geometry in
    /// <see cref="PrimitiveSandboxBuilder"/> instead.
    /// </remarks>
    public static class MovementSandboxBuilder
    {
        private const string SceneFolder = "Assets/Scenes/Sandbox";
        private const string ScenePath = SceneFolder + "/MovementSandbox.unity";

        [MenuItem("SHIFT/Build Movement Sandbox Scene")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Overwrite Movement Sandbox?",
                    ScenePath + " already exists and will be replaced. Any hand-tuning in it is lost.",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SandboxParts.CreateGround();
            CreateJumpTargets();
            CreateCrawlSpace();
            CreateShovableProps();

            CrosshairView crosshair = HudRoot.GetOrCreate();
            GameObject player = SandboxParts.CreatePlayer();
            SandboxParts.AttachCamera(player, crosshair);

            Directory.CreateDirectory(SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("Movement sandbox built at " + ScenePath + " — press Play. WASD moves, shift sprints, " +
                      "ctrl crouches, space jumps (again in midair for the jetpack), left click picks up " +
                      "highlighted props, Esc releases the cursor.");
        }

        /// <summary>
        /// Adds anything the open scene is missing without rebuilding it. Script edits reach
        /// existing components automatically, but newly written components have to be attached,
        /// which is the usual reason a feature appears to be missing after an update.
        /// </summary>
        [MenuItem("SHIFT/Repair Player In Open Scene")]
        public static void Repair()
        {
            FirstPersonMotor motor = Object.FindFirstObjectByType<FirstPersonMotor>();
            if (motor == null)
            {
                EditorUtility.DisplayDialog("No player found",
                    "The open scene has no FirstPersonMotor. Use SHIFT → Build Movement Sandbox Scene instead.",
                    "OK");
                return;
            }

            GameObject player = motor.gameObject;
            CrosshairView crosshair = HudRoot.GetOrCreate();

            Camera camera = player.GetComponentInChildren<Camera>();
            if (camera == null)
            {
                EditorUtility.DisplayDialog("No camera",
                    "Could not find a camera under the player. Use SHIFT → Build Movement Sandbox Scene instead.",
                    "OK");
                return;
            }

            Transform pivot = SandboxParts.EnsureCameraPivot(player, camera);
            SandboxParts.BindCameraPivot(motor, pivot);

            FirstPersonLook look = player.GetComponent<FirstPersonLook>();
            if (look == null) look = player.AddComponent<FirstPersonLook>();
            SandboxParts.BindCameraPivot(look, pivot);

            ObjectCarrier carrier = player.GetComponent<ObjectCarrier>();
            if (carrier == null) carrier = player.AddComponent<ObjectCarrier>();
            SandboxParts.BindCameraPivot(carrier, pivot);
            SandboxParts.Bind(carrier, "_crosshair", crosshair);

            EnsurePlayerPrimitives(player);
            int moved = MoveLooseRigidbodiesToPropLayer(player);

            EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log($"Player repaired — HUD present, ObjectCarrier and primitive engine attached, " +
                      $"{moved} prop(s) moved to the '{ShiftLayers.PropLayerName}' layer. Save the scene.");
        }

        /// <summary>
        /// Puts every loose rigidbody on the Prop layer, which is what PHASE excludes. Scenes
        /// built before that layer existed have their props on Default, where a ghost passes
        /// through nothing and the modifier looks broken rather than unconfigured.
        /// </summary>
        private static int MoveLooseRigidbodiesToPropLayer(GameObject player)
        {
            if (!ShiftLayers.IsConfigured)
            {
                ShiftLayers.WarnIfMissing(player);
                return 0;
            }

            Rigidbody[] bodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            int moved = 0;

            for (int i = 0; i < bodies.Length; i++)
            {
                GameObject prop = bodies[i].gameObject;
                if (prop == player || prop.layer == ShiftLayers.Prop) continue;

                prop.layer = ShiftLayers.Prop;
                moved++;
            }

            return moved;
        }

        /// <summary>Attaches the primitive stack, leaving an existing carrier's authored values alone.</summary>
        internal static PrimitiveCarrier EnsurePlayerPrimitives(GameObject player)
        {
            PrimitiveCarrier primitives = player.GetComponent<PrimitiveCarrier>();
            if (primitives == null)
            {
                primitives = player.AddComponent<PrimitiveCarrier>();
                SerializedObject serialized = new SerializedObject(primitives);
                serialized.FindProperty("_subject").intValue = (int)PrimitiveSubject.Player;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            LoadoutRuntime loadout = player.GetComponent<LoadoutRuntime>();
            if (loadout == null) loadout = player.AddComponent<LoadoutRuntime>();
            if (player.GetComponent<LoadoutHotkeys>() == null) player.AddComponent<LoadoutHotkeys>();

            FillEmptyLoadoutSlots(loadout);
            return primitives;
        }

        /// <summary>
        /// Puts the starter modifiers in any slot that is still empty. Without this the hotkeys
        /// are attached but every key equips null, which reads as "the keys do nothing" rather
        /// than "the loadout is unconfigured".
        /// </summary>
        internal static void FillEmptyLoadoutSlots(LoadoutRuntime loadout)
        {
            if (loadout == null) return;

            ModifierDefinition[] starters =
            {
                PrimitiveAssetFactory.Heavy(),
                PrimitiveAssetFactory.Frog(),
                PrimitiveAssetFactory.Ghost(),
                PrimitiveAssetFactory.Feather()
            };

            AssetDatabase.SaveAssets();

            for (int slot = 0; slot < starters.Length; slot++)
            {
                if (loadout.GetSlot(slot) != null) continue;
                SandboxParts.BindArrayElement(loadout, "_slots", slot, starters[slot]);
            }
        }

        /// <summary>Static geometry at varying heights, so jump height is readable at a glance.</summary>
        private static void CreateJumpTargets()
        {
            GameObject parent = new GameObject("JumpTargets");

            float[] heights = { 0.25f, 0.5f, 1f, 1.5f };
            for (int i = 0; i < heights.Length; i++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = "Step_" + heights[i].ToString("0.00") + "m";
                step.transform.SetParent(parent.transform);
                step.transform.localScale = new Vector3(2f, heights[i], 2f);
                step.transform.position = new Vector3(-6f + i * 2.5f, heights[i] * 0.5f, 6f);
            }
        }

        /// <summary>A gap only passable while crouched, so the crouch height is verifiable.</summary>
        private static void CreateCrawlSpace()
        {
            GameObject parent = new GameObject("CrawlSpace");

            GameObject overhang = GameObject.CreatePrimitive(PrimitiveType.Cube);
            overhang.name = "Overhang";
            overhang.transform.SetParent(parent.transform);
            overhang.transform.localScale = new Vector3(6f, 2f, 3f);
            // Underside sits at 1.25m — below standing height, above the 1.1m crouch capsule.
            overhang.transform.position = new Vector3(7f, 2.25f, 0f);

            for (int i = -1; i <= 1; i += 2)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Pillar_" + (i < 0 ? "L" : "R");
                pillar.transform.SetParent(parent.transform);
                pillar.transform.localScale = new Vector3(6f, 1.25f, 0.5f);
                pillar.transform.position = new Vector3(7f, 0.625f, i * 1.75f);
            }
        }

        /// <summary>Loose rigidbodies to shove, so physics interaction is visible immediately.</summary>
        private static void CreateShovableProps()
        {
            GameObject parent = new GameObject("Props");

            for (int i = 0; i < 8; i++)
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Prop_" + i;
                box.transform.SetParent(parent.transform);
                box.transform.position = new Vector3(-4f + i * 1.2f, 0.5f, -4f);
                if (ShiftLayers.IsConfigured) box.layer = ShiftLayers.Prop;

                Rigidbody body = box.AddComponent<Rigidbody>();
                body.mass = 5f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }
}
