using System.Text;
using Shift.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Shift.UI
{
    /// <summary>Centre banner shown once the run is finished.</summary>
    public class RunCompleteView : MonoBehaviour
    {
        [SerializeField] private RunDirector _director;
        [SerializeField] private Vector2 _size = new Vector2(460f, 190f);
        [SerializeField] private Color _panelColor = new Color(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color _bestColor = new Color(1f, 0.79f, 0.28f);

        private readonly StringBuilder _builder = new StringBuilder(256);
        private GameObject _panel;
        private Text _text;

        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            if (_director == null) _director = Object.FindFirstObjectByType<RunDirector>();
            if (_director == null || _panel == null) return;

            bool finished = _director.Phase == RunPhase.Finished;
            if (_panel.activeSelf != finished) _panel.SetActive(finished);
            if (!finished) return;

            _text.color = _director.LastRunWasBest ? _bestColor : Color.white;
            _text.text = BuildBody();
        }

        private string BuildBody()
        {
            ZoneManifest manifest = _director.Manifest;

            _builder.Clear();
            _builder.AppendLine("ZONE CLEAR");
            _builder.AppendLine();
            _builder.AppendLine(RunTimeFormat.Format(_director.Elapsed));

            if (_director.LastRunWasBest) _builder.AppendLine("NEW BEST");
            else if (_director.PersonalBest != null)
            {
                _builder.AppendLine(
                    $"best {RunTimeFormat.Format(_director.PersonalBest.TotalSeconds)}   " +
                    $"{RunTimeFormat.Delta(_director.Elapsed, _director.PersonalBest.TotalSeconds)}");
            }

            if (manifest != null)
            {
                _builder.AppendLine(
                    $"par  {RunTimeFormat.Format(manifest.ParSeconds)}   " +
                    $"{RunTimeFormat.Delta(_director.Elapsed, manifest.ParSeconds)}");
            }

            _builder.AppendLine();
            _builder.Append("[Shift+R]  Run it again");

            return _builder.ToString();
        }

        private void Build()
        {
            _panel = new GameObject("RunComplete", typeof(RectTransform));
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

            _text = HudText.Create(_panel.transform, 20, TextAnchor.MiddleCenter);
            _panel.SetActive(false);
        }
    }
}
