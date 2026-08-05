using Shift.Modifiers;
using UnityEngine;
using UnityEngine.UI;

namespace Shift.UI
{
    /// <summary>Four slot chips along the bottom, showing which modifier is live.</summary>
    public class LoadoutBarView : MonoBehaviour
    {
        [SerializeField] private LoadoutRuntime _loadout;
        [SerializeField] private Vector2 _chipSize = new Vector2(120f, 34f);
        [SerializeField] private float _spacing = 8f;
        [SerializeField] private float _heightAboveBottom = 150f;
        [SerializeField] private Color _emptyColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Color _idleColor = new Color(0f, 0f, 0f, 0.55f);

        private readonly Image[] _chips = new Image[LoadoutRuntime.SlotCount];
        private readonly Text[] _labels = new Text[LoadoutRuntime.SlotCount];

        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            if (_loadout == null) _loadout = Object.FindFirstObjectByType<LoadoutRuntime>();
            if (_loadout == null) return;

            for (int slot = 0; slot < _chips.Length; slot++)
            {
                ModifierDefinition modifier = _loadout.GetSlot(slot);
                bool isActive = _loadout.ActiveSlot == slot;

                _labels[slot].text = modifier != null
                    ? $"{slot + 1}  {modifier.DisplayName}"
                    : $"{slot + 1}  —";

                if (modifier == null) _chips[slot].color = _emptyColor;
                else _chips[slot].color = isActive ? modifier.Tint : _idleColor;

                _labels[slot].color = isActive ? Color.black : new Color(1f, 1f, 1f, 0.8f);
            }
        }

        private void Build()
        {
            float totalWidth = _chips.Length * _chipSize.x + (_chips.Length - 1) * _spacing;
            float left = -totalWidth * 0.5f + _chipSize.x * 0.5f;

            for (int slot = 0; slot < _chips.Length; slot++)
            {
                GameObject chip = new GameObject("Slot" + (slot + 1), typeof(RectTransform));
                chip.transform.SetParent(transform, false);

                RectTransform rect = (RectTransform)chip.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(left + slot * (_chipSize.x + _spacing), _heightAboveBottom);
                rect.sizeDelta = _chipSize;

                _chips[slot] = chip.AddComponent<Image>();
                _chips[slot].sprite = UiSprites.Solid();
                _chips[slot].color = _emptyColor;
                _chips[slot].raycastTarget = false;

                _labels[slot] = HudText.Create(chip.transform, 15, TextAnchor.MiddleCenter);
            }
        }
    }
}
