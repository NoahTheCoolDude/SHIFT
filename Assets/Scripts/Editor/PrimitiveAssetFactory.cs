using System.IO;
using Shift.Core;
using Shift.Modifiers;
using UnityEditor;
using UnityEngine;

namespace Shift.EditorTools
{
    /// <summary>
    /// Authors the starter modifier, field and surface assets in code.
    /// </summary>
    /// <remarks>
    /// Assets are normally made through the CreateAssetMenu — these exist so the primitive
    /// sandbox is reproducible from a clean clone rather than depending on nine .asset files
    /// someone remembered to commit. Existing assets are left alone, so hand-tuning survives.
    /// </remarks>
    internal static class PrimitiveAssetFactory
    {
        public const string PlayerFolder = "Assets/Data/Modifiers/Player";
        public const string EnvironmentFolder = "Assets/Data/Modifiers/Environment";
        public const string FieldFolder = "Assets/Data/Fields";

        public static ModifierDefinition Heavy() => Modifier(
            "Heavy",
            new Color(0.78f, 0.45f, 0.25f),
            Override(PrimitiveMask.Mass | PrimitiveMask.Friction,
                mass: MassClass.Heavy, friction: 0.9f),
            Note("Ice", "Grips where a normal player slides — friction combines by Maximum."),
            Note("Seesaw", "Tips a balance that a normal player cannot."),
            Note("Magnet", "Too massive to be pulled off a surface by a magnetic field."));

        public static ModifierDefinition Frog() => Modifier(
            "Frog",
            new Color(0.35f, 0.78f, 0.35f),
            Override(PrimitiveMask.Bounce, bounce: 0.9f),
            Note("Trampoline", "Bounce stacks with the surface — clears heights nothing else reaches."),
            Note("LowGravity", "Long floaty arcs; the bounce carries much further per hop."),
            Note("Sticky", "Sticky floor kills the bounce, so it becomes a trap for Frog."));

        public static ModifierDefinition Ghost() => Modifier(
            "Ghost",
            new Color(0.55f, 0.65f, 0.9f),
            Override(PrimitiveMask.Phase, ghost: true),
            Note("Grates", "Walks through prop geometry while still standing on the floor."),
            Note("Heavy", "Cannot carry anything for a teammate — hands pass straight through."),
            Note("Crusher", "Immune to being crushed by props, so it can hold a hazard open."));

        public static ModifierDefinition Feather() => Modifier(
            "Feather",
            new Color(0.9f, 0.85f, 0.55f),
            Override(PrimitiveMask.Mass, mass: MassClass.Tiny),
            Note("LowGravity", "Already light, so low gravity turns hops into long glides."),
            Note("Seesaw", "Too light to depress a pressure plate — needs a teammate."),
            Note("Frog", "Tiny mass makes the jetpack thrust go much further per unit of fuel."));

        public static FieldDefinition LowGravity() => Field(
            "LowGravity",
            Override(PrimitiveMask.Gravity, gravityStrength: 0.3f));

        public static FieldDefinition HalfTime() => Field(
            "HalfTime",
            Override(PrimitiveMask.TimeRate, timeRate: 0.5f));

        public static SurfaceDefinition Ice() => Surface(
            "Ice", Override(PrimitiveMask.Friction, friction: 0f));

        public static SurfaceDefinition Sticky() => Surface(
            "Sticky", Override(PrimitiveMask.Friction, friction: 1f));

        public static SurfaceDefinition Trampoline() => Surface(
            "Trampoline", Override(PrimitiveMask.Bounce, bounce: 0.9f));

        private static ModifierDefinition Modifier(
            string assetName, Color tint, PrimitiveOverride primitives, params InteractionNote[] notes)
        {
            string path = $"{PlayerFolder}/{assetName}.asset";
            ModifierDefinition existing = AssetDatabase.LoadAssetAtPath<ModifierDefinition>(path);
            if (existing != null) return existing;

            ModifierDefinition asset = ScriptableObject.CreateInstance<ModifierDefinition>();
            Create(asset, path);

            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("_displayName").stringValue = assetName;
            serialized.FindProperty("_tint").colorValue = tint;
            WriteOverride(serialized, "_override", primitives);
            WriteNotes(serialized, "_interactions", notes);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static FieldDefinition Field(string assetName, PrimitiveOverride primitives)
        {
            string path = $"{FieldFolder}/{assetName}.asset";
            FieldDefinition existing = AssetDatabase.LoadAssetAtPath<FieldDefinition>(path);
            if (existing != null) return existing;

            FieldDefinition asset = ScriptableObject.CreateInstance<FieldDefinition>();
            Create(asset, path);

            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("_displayName").stringValue = assetName;
            WriteOverride(serialized, "_override", primitives);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static SurfaceDefinition Surface(string assetName, PrimitiveOverride primitives)
        {
            string path = $"{EnvironmentFolder}/{assetName}.asset";
            SurfaceDefinition existing = AssetDatabase.LoadAssetAtPath<SurfaceDefinition>(path);
            if (existing != null) return existing;

            SurfaceDefinition asset = ScriptableObject.CreateInstance<SurfaceDefinition>();
            Create(asset, path);

            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("_displayName").stringValue = assetName;
            WriteOverride(serialized, "_override", primitives);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void Create(Object asset, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
        }

        private static InteractionNote Note(string environment, string expected)
        {
            return new InteractionNote { WithEnvironment = environment, Expected = expected };
        }

        private static PrimitiveOverride Override(
            PrimitiveMask set,
            MassClass mass = MassClass.Normal,
            float friction = PrimitiveState.NormalFriction,
            float bounce = 0f,
            float gravityStrength = 1f,
            bool ghost = false,
            float timeRate = 1f)
        {
            PrimitiveOverride result = PrimitiveOverride.Empty;
            result.Set = set;
            result.Mass = mass;
            result.Friction = friction;
            result.Bounce = bounce;
            result.GravityStrength = gravityStrength;
            result.Ghost = ghost;
            result.TimeRate = timeRate;
            return result;
        }

        private static void WriteOverride(SerializedObject serialized, string path, PrimitiveOverride value)
        {
            serialized.FindProperty(path + ".Set").intValue = (int)value.Set;
            serialized.FindProperty(path + ".Mass").intValue = (int)value.Mass;
            serialized.FindProperty(path + ".Friction").floatValue = value.Friction;
            serialized.FindProperty(path + ".Bounce").floatValue = value.Bounce;
            serialized.FindProperty(path + ".GravityDirection").vector3Value = value.GravityDirection;
            serialized.FindProperty(path + ".GravityStrength").floatValue = value.GravityStrength;
            serialized.FindProperty(path + ".Adhesion").boolValue = value.Adhesion;
            serialized.FindProperty(path + ".Ghost").boolValue = value.Ghost;
            serialized.FindProperty(path + ".Charge").intValue = (int)value.Charge;
            serialized.FindProperty(path + ".TimeRate").floatValue = value.TimeRate;
        }

        private static void WriteNotes(SerializedObject serialized, string path, InteractionNote[] notes)
        {
            SerializedProperty array = serialized.FindProperty(path);
            array.arraySize = notes.Length;

            for (int i = 0; i < notes.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("WithEnvironment").stringValue = notes[i].WithEnvironment;
                element.FindPropertyRelative("Expected").stringValue = notes[i].Expected;
            }
        }
    }
}
