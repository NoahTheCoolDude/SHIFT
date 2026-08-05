using UnityEngine;

namespace Shift.Progression
{
    /// <summary>
    /// Metadata for one Zone — a level, in CLAUDE.md's vocabulary.
    /// </summary>
    /// <remarks>
    /// Deliberately carries no modifier references. Nothing enforces a loadout yet, so the field
    /// would be data that lies; and referencing ModifierDefinition would drag this type out of
    /// Shift.Progression, taking RunDirector and the unit tests with it. Asymmetric loadout locks
    /// belong on a separate asset the loadout screen reads, not on the one the timer reads.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewZoneManifest", menuName = "SHIFT/Zone Manifest", order = 3)]
    public class ZoneManifest : ScriptableObject
    {
        [Tooltip("Stable key for saved records. Renaming this orphans every personal best.")]
        [SerializeField] private string _zoneId = "zone00";

        [SerializeField] private string _displayName = "Unnamed Zone";

        [Tooltip("Target time shown when the player has no personal best yet.")]
        [SerializeField] private float _parSeconds = 90f;

        [Tooltip("Index-aligned with each ZoneCheckpoint's own index.")]
        [SerializeField] private string[] _splitLabels = new string[0];

        public string ZoneId => _zoneId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public float ParSeconds => _parSeconds;
        public string[] SplitLabels => _splitLabels;
        public int SplitCount => _splitLabels != null ? _splitLabels.Length : 0;

        public string SplitLabel(int index)
        {
            return _splitLabels != null && index >= 0 && index < _splitLabels.Length
                ? _splitLabels[index]
                : $"Split {index + 1}";
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_zoneId))
            {
                Debug.LogWarning($"{name}: blank ZoneId — records cannot be saved or loaded.", this);
            }

            if (_parSeconds <= 0f)
            {
                Debug.LogWarning($"{name}: par time must be positive.", this);
            }
        }
    }
}
