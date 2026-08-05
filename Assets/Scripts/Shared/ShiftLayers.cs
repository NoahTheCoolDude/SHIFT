using UnityEngine;

namespace Shift.Shared
{
    /// <summary>
    /// Layer lookups resolved by name, so a missing project layer degrades to a warning instead
    /// of silently colliding with whatever happens to occupy that index.
    /// </summary>
    public static class ShiftLayers
    {
        public const string PropLayerName = "Prop";

        private static int _propLayer = -1;

        /// <summary>The layer PHASE excludes. -1 when the project has not defined it yet.</summary>
        public static int Prop
        {
            get
            {
                if (_propLayer == -1) _propLayer = LayerMask.NameToLayer(PropLayerName);
                return _propLayer;
            }
        }

        public static int PropMask => Prop < 0 ? 0 : 1 << Prop;

        public static bool IsConfigured => Prop >= 0;

        public static void WarnIfMissing(Object context)
        {
            if (IsConfigured) return;

            Debug.LogWarning(
                $"No '{PropLayerName}' layer defined. PHASE (ghost) cannot pass through props until " +
                "one is added in Project Settings → Tags and Layers.", context);
        }
    }
}
