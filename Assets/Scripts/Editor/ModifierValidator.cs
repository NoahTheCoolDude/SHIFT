using System.Collections.Generic;
using System.Text;
using Shift.Core;
using Shift.Modifiers;
using UnityEditor;
using UnityEngine;

namespace Shift.EditorTools
{
    /// <summary>
    /// Sweeps every authored modifier for rule violations.
    /// </summary>
    /// <remarks>
    /// This was meant to be an EditMode test, but ModifierDefinition lives in Assembly-CSharp
    /// (Shift.Modifiers and Shift.Shared reference each other, so neither can be split into its
    /// own assembly), and a test assembly cannot reference Assembly-CSharp. A menu command gives
    /// the same sweep without forcing a circular assembly split. Individual assets also warn on
    /// import via their own OnValidate.
    /// </remarks>
    public static class ModifierValidator
    {
        [MenuItem("SHIFT/Validate Modifier Assets")]
        public static void Validate()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(ModifierDefinition));
            List<string> problems = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModifierDefinition modifier = AssetDatabase.LoadAssetAtPath<ModifierDefinition>(path);
                if (modifier == null) continue;

                if (modifier.Override.Set == PrimitiveMask.None)
                {
                    problems.Add($"{modifier.name}: sets no primitives — equipping it does nothing.");
                }

                PrimitiveMask illegal =
                    PrimitiveSubjects.IllegalBits(PrimitiveSubject.Player, modifier.Override.Set);
                if (illegal != PrimitiveMask.None)
                {
                    problems.Add($"{modifier.name}: sets {illegal}, which does not apply to a player.");
                }

                int documented = modifier.DocumentedInteractionCount;
                if (documented < ModifierDefinition.RequiredInteractions)
                {
                    problems.Add(
                        $"{modifier.name}: {documented}/{ModifierDefinition.RequiredInteractions} " +
                        "documented interactions (rule 3).");
                }
            }

            if (guids.Length == 0)
            {
                Debug.LogWarning("No ModifierDefinition assets found.");
                return;
            }

            if (problems.Count == 0)
            {
                Debug.Log($"All {guids.Length} modifier assets pass.");
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine($"{problems.Count} problem(s) across {guids.Length} modifier assets:");
            foreach (string problem in problems) report.AppendLine("  • " + problem);

            Debug.LogWarning(report.ToString());
        }
    }
}
