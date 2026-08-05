using System.Collections.Generic;
using Shift.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Shift.UI
{
    /// <summary>
    /// The run clock and split table, top-centre.
    /// </summary>
    /// <remarks>
    /// Splits are shown cumulative rather than per-segment: cumulative is what a comparison
    /// against a personal best needs, and segment times are derivable from it. The reverse is not.
    /// </remarks>
    public class RunTimerView : MonoBehaviour
    {
        [SerializeField] private RunDirector _director;
        [SerializeField] private float _topMargin = 18f;
        [SerializeField] private int _clockFontSize = 34;
        [SerializeField] private int _splitFontSize = 15;
        [SerializeField] private Color _aheadColor = new Color(0.4f, 0.85f, 0.45f);
        [SerializeField] private Color _behindColor = new Color(0.9f, 0.42f, 0.4f);
        [SerializeField] private Color _neutralColor = new Color(1f, 1f, 1f, 0.65f);

        private Text _clock;
        private readonly List<Text> _splitRows = new List<Text>();
        private RectTransform _splitPanel;
        private int _builtRowCount = -1;

        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            if (_director == null) _director = Object.FindFirstObjectByType<RunDirector>();
            if (_director == null || _clock == null) return;

            _clock.text = RunTimeFormat.Format(_director.Elapsed);
            _clock.color = _director.Phase == RunPhase.Running ? Color.white : _neutralColor;

            RebuildRowsIfNeeded();
            RefreshSplits();
        }

        private void RebuildRowsIfNeeded()
        {
            int wanted = _director.Manifest != null ? _director.Manifest.SplitCount : 0;
            if (wanted == _builtRowCount) return;

            for (int i = 0; i < _splitRows.Count; i++) Destroy(_splitRows[i].gameObject);
            _splitRows.Clear();

            for (int i = 0; i < wanted; i++)
            {
                Text row = HudText.Create(_splitPanel, _splitFontSize, TextAnchor.UpperCenter);
                row.rectTransform.anchorMin = new Vector2(0f, 1f);
                row.rectTransform.anchorMax = new Vector2(1f, 1f);
                row.rectTransform.pivot = new Vector2(0.5f, 1f);
                row.rectTransform.sizeDelta = new Vector2(0f, 20f);
                row.rectTransform.anchoredPosition = new Vector2(0f, -i * 20f);
                _splitRows.Add(row);
            }

            _builtRowCount = wanted;
        }

        private void RefreshSplits()
        {
            ZoneManifest manifest = _director.Manifest;
            if (manifest == null) return;

            IReadOnlyList<float> splits = _director.Splits;
            RunRecord best = _director.PersonalBest;

            for (int i = 0; i < _splitRows.Count; i++)
            {
                float time = i < splits.Count ? splits[i] : RunClock.Unreached;
                string label = manifest.SplitLabel(i).PadRight(14);
                string delta = best != null ? RunTimeFormat.Delta(time, best.SplitAt(i)) : string.Empty;

                _splitRows[i].text = $"{label}{RunTimeFormat.Format(time),9}   {delta}";

                if (string.IsNullOrEmpty(delta)) _splitRows[i].color = _neutralColor;
                else _splitRows[i].color = delta[0] == '-' ? _aheadColor : _behindColor;
            }
        }

        private void Build()
        {
            GameObject clockObject = new GameObject("RunClock", typeof(RectTransform));
            clockObject.transform.SetParent(transform, false);

            RectTransform clockRect = (RectTransform)clockObject.transform;
            clockRect.anchorMin = clockRect.anchorMax = new Vector2(0.5f, 1f);
            clockRect.pivot = new Vector2(0.5f, 1f);
            clockRect.anchoredPosition = new Vector2(0f, -_topMargin);
            clockRect.sizeDelta = new Vector2(320f, 44f);

            _clock = HudText.Create(clockRect, _clockFontSize, TextAnchor.UpperCenter);

            GameObject panelObject = new GameObject("Splits", typeof(RectTransform));
            panelObject.transform.SetParent(transform, false);

            _splitPanel = (RectTransform)panelObject.transform;
            _splitPanel.anchorMin = _splitPanel.anchorMax = new Vector2(0.5f, 1f);
            _splitPanel.pivot = new Vector2(0.5f, 1f);
            _splitPanel.anchoredPosition = new Vector2(0f, -(_topMargin + 48f));
            _splitPanel.sizeDelta = new Vector2(340f, 120f);
        }
    }
}
