using Shift.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Shift.UI
{
    /// <summary>
    /// Esc overlay. Owns the cursor, the input gate, and the panel.
    /// </summary>
    /// <remarks>
    /// Pause is a local UI mode, never a world freeze — it deliberately does not touch
    /// Time.timeScale. In host-authoritative P2P a host pausing would freeze the whole session and
    /// a client pausing would desync, so building solo pause on timeScale creates a second code
    /// path that has to be deleted at port time. The run clock keeps ticking, which also removes
    /// pause-buffering without a line of anti-cheat.
    ///
    /// Controller and view are merged because it must be a HudRoot sibling anyway and the three
    /// concerns are one feature.
    /// </remarks>
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private Vector2 _size = new Vector2(420f, 220f);
        [SerializeField] private Color _panelColor = new Color(0f, 0f, 0f, 0.8f);

        private const string Body =
            "PAUSED\n\n" +
            "[Esc]      Resume\n" +
            "[R]        Respawn at checkpoint\n" +
            "[Shift+R]  Restart run\n\n" +
            "the clock keeps running";

        private GameObject _panel;
        private bool _paused;

        private void Awake()
        {
            Build();
            SetPaused(false);
        }

        private void OnDisable()
        {
            // Exiting Play Mode while paused must not leave the static gate set for the next run.
            UiInputGate.Suppressed = false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame) SetPaused(!_paused);
        }

        private void OnApplicationFocus(bool focused)
        {
            // Windows silently drops the cursor lock on alt-tab. Without re-applying it the mouse
            // stops turning the camera until you click, which reads as a hard bug.
            if (focused && !_paused) ApplyCursor(false);
        }

        private void SetPaused(bool paused)
        {
            _paused = paused;
            UiInputGate.Suppressed = paused;
            ApplyCursor(paused);

            if (_panel != null) _panel.SetActive(paused);
        }

        private static void ApplyCursor(bool released)
        {
            Cursor.lockState = released ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = released;
        }

        private void Build()
        {
            _panel = new GameObject("PauseMenu", typeof(RectTransform));
            _panel.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)_panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = _size;

            Image panel = _panel.AddComponent<Image>();
            panel.sprite = UiSprites.Solid();
            panel.color = _panelColor;
            panel.raycastTarget = false;

            Text text = HudText.Create(_panel.transform, 18, TextAnchor.MiddleCenter);
            text.text = Body;
        }
    }
}
