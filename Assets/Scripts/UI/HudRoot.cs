using UnityEngine;
using UnityEngine.UI;

namespace Shift.UI
{
    /// <summary>
    /// Creates the Phase 0 HUD canvas on demand. Anything that needs the HUD asks for it here
    /// rather than depending on a scene having been rebuilt with the right objects wired up.
    /// </summary>
    public static class HudRoot
    {
        public static CrosshairView GetOrCreate()
        {
            CrosshairView existing = Object.FindFirstObjectByType<CrosshairView>();
            if (existing != null)
            {
                EnsureViews(existing.gameObject);
                return existing;
            }

            GameObject hud = new GameObject("HUD");

            Canvas canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CrosshairView crosshair = hud.AddComponent<CrosshairView>();
            EnsureViews(hud);

            return crosshair;
        }

        /// <summary>
        /// Adds any view the HUD object is missing. Called on the found path too: a scene saved
        /// before a view existed keeps its old HUD object forever, and without this the new view
        /// silently never appears — which reads as a broken feature, not a stale scene.
        /// </summary>
        internal static void EnsureViews(GameObject hud)
        {
            Ensure<CrosshairView>(hud);
            Ensure<JetpackGaugeView>(hud);
            Ensure<LoadoutBarView>(hud);
            Ensure<PrimitiveDebugView>(hud);
            Ensure<RunTimerView>(hud);
            Ensure<RunCompleteView>(hud);
            Ensure<PauseMenuView>(hud);
        }

        private static void Ensure<T>(GameObject hud) where T : Component
        {
            if (hud.GetComponent<T>() == null) hud.AddComponent<T>();
        }
    }
}
