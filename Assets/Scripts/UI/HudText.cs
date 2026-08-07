using UnityEngine;
using UnityEngine.UI;

namespace Shift.UI
{
    /// <summary>
    /// Builds the legacy Text widgets the Phase 0 HUD uses. Legacy rather than TextMeshPro
    /// because TMP needs its Essentials imported before it will render, and a debug HUD that
    /// silently shows nothing on a fresh clone is worse than a deprecated component.
    /// </summary>
    internal static class HudText
    {
        public static Text Create(Transform parent, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }
    }
}
