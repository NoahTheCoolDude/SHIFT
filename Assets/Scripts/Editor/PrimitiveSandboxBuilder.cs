using System.IO;
using Shift.Core;
using Shift.Modifiers;
using Shift.Shared;
using Shift.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shift.EditorTools
{
    /// <summary>
    /// Builds the primitive test scene. Every element here exists to falsify exactly one
    /// primitive, so a modifier that looks right but is wired wrong has somewhere to fail
    /// visibly.
    /// </summary>
    public static class PrimitiveSandboxBuilder
    {
        private const string SceneFolder = "Assets/Scenes/Sandbox";
        private const string ScenePath = SceneFolder + "/PrimitiveSandbox.unity";

        [MenuItem("SHIFT/Build Primitive Sandbox Scene")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Overwrite Primitive Sandbox?",
                    ScenePath + " already exists and will be replaced. Any hand-tuning in it is lost.",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ShiftLayers.WarnIfMissing(null);

            FieldDefinition lowGravity = PrimitiveAssetFactory.LowGravity();
            FieldDefinition halfTime = PrimitiveAssetFactory.HalfTime();
            SurfaceDefinition ice = PrimitiveAssetFactory.Ice();
            SurfaceDefinition sticky = PrimitiveAssetFactory.Sticky();
            SurfaceDefinition trampoline = PrimitiveAssetFactory.Trampoline();

            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SandboxParts.CreateGround(6f);
            CreateSurfaceStrips(ice, sticky, trampoline);
            CreateFields(lowGravity, halfTime);
            CreateCarryTest();
            CreateGhostTest();
            CreateLowGravityLedge();

            CrosshairView crosshair = HudRoot.GetOrCreate();
            GameObject player = SandboxParts.CreatePlayer();
            SandboxParts.AttachCamera(player, crosshair);

            // Attaches the carrier, loadout and hotkeys, and fills the four slots.
            PrimitiveCarrier primitives = MovementSandboxBuilder.EnsurePlayerPrimitives(player);

            Directory.CreateDirectory(SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"Primitive sandbox built at {ScenePath} (carrier subject: {primitives.Subject}). " +
                      "Press Play. 1 Heavy, 2 Frog, 3 Ghost, 4 Feather, 0 back to baseline.");
        }

        private static void CreateSurfaceStrips(
            SurfaceDefinition ice, SurfaceDefinition sticky, SurfaceDefinition trampoline)
        {
            GameObject parent = new GameObject("Surfaces");

            CreateStrip(parent.transform, "Ice", ice, new Vector3(-8f, 0.05f, 0f), Color.cyan);
            CreateStrip(parent.transform, "Sticky", sticky, new Vector3(-8f, 0.05f, 6f), Color.magenta);
            CreateStrip(parent.transform, "Trampoline", trampoline, new Vector3(-8f, 0.05f, -6f), Color.green);
        }

        private static void CreateStrip(
            Transform parent, string name, SurfaceDefinition definition, Vector3 position, Color tint)
        {
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = name;
            strip.transform.SetParent(parent);
            strip.transform.localScale = new Vector3(12f, 0.1f, 4f);
            strip.transform.position = position;

            SandboxParts.Bind(strip.AddComponent<PrimitiveSurface>(), "_definition", definition);
            ZoneParts.Tint(strip, tint);
        }

        private static void CreateFields(FieldDefinition lowGravity, FieldDefinition halfTime)
        {
            GameObject parent = new GameObject("Fields");

            CreateField(parent.transform, "LowGravityField", lowGravity,
                new Vector3(10f, 4f, 0f), new Vector3(10f, 8f, 10f));

            CreateField(parent.transform, "HalfTimeField", halfTime,
                new Vector3(10f, 4f, 14f), new Vector3(10f, 8f, 10f));
        }

        private static void CreateField(
            Transform parent, string name, FieldDefinition definition, Vector3 position, Vector3 size)
        {
            GameObject field = new GameObject(name);
            field.transform.SetParent(parent);
            field.transform.position = position;

            BoxCollider box = field.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;

            SandboxParts.Bind(field.AddComponent<PrimitiveField>(), "_definition", definition);
        }

        /// <summary>
        /// Two props straddling the carry limit: 8kg is liftable by anyone, 30kg only once MASS
        /// raises the limit — so CarryScale is testable rather than asserted.
        /// </summary>
        private static void CreateCarryTest()
        {
            GameObject parent = new GameObject("CarryTest");

            CreateProp(parent.transform, "Cube_Tiny_8kg", MassClass.Tiny,
                new Vector3(2f, 0.5f, -4f), Vector3.one);

            CreateProp(parent.transform, "Crate_Light_30kg", MassClass.Light,
                new Vector3(4f, 0.6f, -4f), new Vector3(1.2f, 1.2f, 1.2f));
        }

        private static void CreateProp(
            Transform parent, string name, MassClass mass, Vector3 position, Vector3 scale)
        {
            GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = name;
            prop.transform.SetParent(parent);
            prop.transform.position = position;
            prop.transform.localScale = scale;
            if (ShiftLayers.IsConfigured) prop.layer = ShiftLayers.Prop;

            Rigidbody body = prop.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;

            PrimitiveCarrier carrier = prop.AddComponent<PrimitiveCarrier>();
            SerializedObject serialized = new SerializedObject(carrier);
            serialized.FindProperty("_subject").intValue = (int)PrimitiveSubject.Prop;
            serialized.FindProperty("_intrinsic.Set").intValue = (int)PrimitiveMask.Mass;
            serialized.FindProperty("_intrinsic.Mass").intValue = (int)mass;
            serialized.FindProperty("_intrinsic.GravityDirection").vector3Value = Vector3.down;
            serialized.FindProperty("_intrinsic.GravityStrength").floatValue = 1f;
            serialized.FindProperty("_intrinsic.Friction").floatValue = PrimitiveState.NormalFriction;
            serialized.FindProperty("_intrinsic.TimeRate").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            prop.AddComponent<RigidbodyPrimitiveApplier>();
        }

        /// <summary>
        /// A wall on the Prop layer with open floor behind it. Ghost must pass the wall and still
        /// stand on the floor — the two halves of what PHASE means.
        /// </summary>
        private static void CreateGhostTest()
        {
            GameObject parent = new GameObject("GhostTest");

            GameObject grate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grate.name = "Grate_PropLayer";
            grate.transform.SetParent(parent.transform);
            grate.transform.localScale = new Vector3(0.4f, 3f, 6f);
            grate.transform.position = new Vector3(-4f, 1.5f, 12f);
            if (ShiftLayers.IsConfigured) grate.layer = ShiftLayers.Prop;
            ZoneParts.Tint(grate, new Color(0.55f, 0.65f, 0.9f));

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "FloorBehindGrate";
            floor.transform.SetParent(parent.transform);
            floor.transform.localScale = new Vector3(6f, 0.4f, 6f);
            floor.transform.position = new Vector3(-7.5f, 0.2f, 12f);
        }

        /// <summary>At 3.5m this is out of reach of a 1.3m jump plus jetpack, but comfortable
        /// once low gravity triples the apex.</summary>
        private static void CreateLowGravityLedge()
        {
            GameObject ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledge.name = "LowGravityLedge_3.5m";
            ledge.transform.localScale = new Vector3(4f, 0.4f, 4f);
            ledge.transform.position = new Vector3(12f, 3.5f, 0f);
            ZoneParts.Tint(ledge, new Color(0.9f, 0.85f, 0.55f));
        }

    }
}
