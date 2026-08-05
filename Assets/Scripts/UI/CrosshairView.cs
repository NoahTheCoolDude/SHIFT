using UnityEngine;
using UnityEngine.UI;

namespace Shift.UI
{
    /// <summary>
    /// Centre dot plus a contextual interaction prompt. Builds its own widgets at runtime so
    /// the HUD needs no authored sprites or prefab, which keeps it out of the un-mergeable
    /// asset files during Phase 0.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CrosshairView : MonoBehaviour
    {
        [SerializeField] private float _dotSize = 7f;
        [SerializeField] private Color _idleColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color _activeColor = new Color(1f, 0.79f, 0.28f, 1f);
        [SerializeField] private int _promptFontSize = 16;
        [SerializeField] private Vector2 _promptOffset = new Vector2(0f, -34f);

        private Image _dot;
        private Text _prompt;

        private void Awake()
        {
            BuildDot();
            BuildPrompt();
            HidePrompt();
        }

        public void ShowPrompt(string text)
        {
            if (_prompt == null) return;

            _prompt.text = text;
            _prompt.enabled = true;
            if (_dot != null) _dot.color = _activeColor;
        }

        public void HidePrompt()
        {
            if (_prompt != null) _prompt.enabled = false;
            if (_dot != null) _dot.color = _idleColor;
        }

        private void BuildDot()
        {
            GameObject dotObject = new GameObject("Dot", typeof(RectTransform));
            dotObject.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)dotObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(_dotSize, _dotSize);

            _dot = dotObject.AddComponent<Image>();
            _dot.sprite = UiSprites.Circle();
            _dot.color = _idleColor;
            _dot.raycastTarget = false;
        }

        private void BuildPrompt()
        {
            GameObject promptObject = new GameObject("Prompt", typeof(RectTransform));
            promptObject.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)promptObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = _promptOffset;
            rect.sizeDelta = new Vector2(320f, 28f);

            _prompt = promptObject.AddComponent<Text>();
            _prompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                           ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _prompt.fontSize = _promptFontSize;
            _prompt.alignment = TextAnchor.MiddleCenter;
            _prompt.color = _activeColor;
            _prompt.raycastTarget = false;
            _prompt.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }
}
