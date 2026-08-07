using System.Text;
using Shift.Core;
using Shift.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Shift.UI
{
    /// <summary>
    /// Dumps the player's resolved primitive state field by field.
    /// </summary>
    /// <remarks>
    /// This is the instrument that makes the resolution order verifiable rather than believed.
    /// When the physics and this readout disagree, the applier is wrong, not the resolver — which
    /// is the single most useful thing to know while debugging a modifier.
    /// </remarks>
    public class PrimitiveDebugView : MonoBehaviour
    {
        [SerializeField] private PrimitiveCarrier _carrier;
        [SerializeField] private Vector2 _size = new Vector2(260f, 190f);
        [SerializeField] private Vector2 _margin = new Vector2(16f, 16f);
        [SerializeField] private Color _panelColor = new Color(0f, 0f, 0f, 0.5f);

        private readonly StringBuilder _builder = new StringBuilder(256);
        private Text _text;

        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            if (_carrier == null) _carrier = FindPlayerCarrier();
            if (_carrier == null || _text == null) return;

            PrimitiveState state = _carrier.Current;

            _builder.Clear();
            _builder.AppendLine("RESOLVED PRIMITIVES");
            _builder.AppendLine($"MASS      {state.Mass} ({MassProfile.For(state.Mass).Kilograms:0}kg)");
            _builder.AppendLine($"FRICTION  {state.Friction:0.00}");
            _builder.AppendLine($"BOUNCE    {state.Bounce:0.00}");
            _builder.AppendLine($"GRAVITY   {state.GravityStrength:0.00}x {state.GravityDirection}");
            _builder.AppendLine($"ADHESION  {(state.Adhesion ? "on" : "off")}");
            _builder.AppendLine($"PHASE     {(state.Ghost ? "ghost" : "solid")}");
            _builder.AppendLine($"CHARGE    {state.Charge}");
            _builder.AppendLine($"TIME_RATE {state.TimeRate:0.00}");
            _builder.Append($"fields    {_carrier.FieldCount}");

            _text.text = _builder.ToString();
        }

        private static PrimitiveCarrier FindPlayerCarrier()
        {
            PrimitiveCarrier[] carriers = FindObjectsByType<PrimitiveCarrier>(FindObjectsSortMode.None);
            for (int i = 0; i < carriers.Length; i++)
            {
                if (carriers[i].Subject == PrimitiveSubject.Player) return carriers[i];
            }

            return null;
        }

        private void Build()
        {
            GameObject panelObject = new GameObject("PrimitiveDebug", typeof(RectTransform));
            panelObject.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)panelObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(_margin.x, -_margin.y);
            rect.sizeDelta = _size;

            Image panel = panelObject.AddComponent<Image>();
            panel.sprite = UiSprites.Solid();
            panel.color = _panelColor;
            panel.raycastTarget = false;

            _text = HudText.Create(panelObject.transform, 14, TextAnchor.UpperLeft);
            _text.rectTransform.offsetMin = new Vector2(10f, 8f);
            _text.rectTransform.offsetMax = new Vector2(-10f, -8f);
        }
    }
}
