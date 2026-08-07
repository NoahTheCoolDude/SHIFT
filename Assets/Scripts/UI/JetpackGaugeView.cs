using Shift.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Shift.UI
{
    /// <summary>
    /// Horizontal bar showing remaining jetpack thrust. Sits low on screen so it reads in
    /// peripheral vision without competing with the crosshair.
    /// </summary>
    public class JetpackGaugeView : MonoBehaviour
    {
        [SerializeField] private FirstPersonMotor _motor;
        [SerializeField] private Vector2 _size = new Vector2(280f, 12f);
        [SerializeField] private float _heightAboveBottom = 200f;
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color _readyColor = new Color(1f, 0.79f, 0.28f, 0.95f);
        [SerializeField] private Color _thrustingColor = new Color(1f, 0.42f, 0.2f, 1f);
        [SerializeField] private Color _rechargingColor = new Color(0.45f, 0.62f, 0.85f, 0.9f);

        private Image _fill;

        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            if (_motor == null) _motor = Object.FindFirstObjectByType<FirstPersonMotor>();
            if (_motor == null || _fill == null) return;

            float fuel = _motor.JetpackFuelNormalized;
            _fill.fillAmount = fuel;

            if (_motor.IsJetpackActive) _fill.color = _thrustingColor;
            else if (fuel < 1f) _fill.color = _rechargingColor;
            else _fill.color = _readyColor;
        }

        private void Build()
        {
            GameObject background = NewBar("JetpackGauge", transform, _size);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = UiSprites.Solid();
            backgroundImage.color = _backgroundColor;
            backgroundImage.raycastTarget = false;

            // Inset by a pixel on each edge so the background reads as a frame around the fill.
            GameObject fill = NewBar("Fill", background.transform, _size - new Vector2(2f, 2f));
            _fill = fill.AddComponent<Image>();
            _fill.sprite = UiSprites.Solid();
            _fill.color = _readyColor;
            _fill.raycastTarget = false;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.fillAmount = 1f;
        }

        private GameObject NewBar(string name, Transform parent, Vector2 size)
        {
            GameObject barObject = new GameObject(name, typeof(RectTransform));
            barObject.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)barObject.transform;
            bool isRoot = parent == transform;

            rect.anchorMin = rect.anchorMax = isRoot ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = isRoot ? new Vector2(0f, _heightAboveBottom) : Vector2.zero;
            rect.sizeDelta = size;

            return barObject;
        }
    }
}
