using System.Collections.Generic;

namespace Shift.Progression
{
    /// <summary>
    /// Elapsed time plus the cumulative time at each checkpoint. Plain C#, no MonoBehaviour, so
    /// the whole thing is unit-testable.
    /// </summary>
    public class RunClock
    {
        /// <summary>A checkpoint that was never reached. Not NaN — JsonUtility mangles NaN.</summary>
        public const float Unreached = -1f;

        private readonly List<float> _splits = new List<float>();

        public float Elapsed { get; private set; }
        public IReadOnlyList<float> Splits => _splits;

        public RunClock(int splitCount = 0)
        {
            Resize(splitCount);
        }

        public void Resize(int splitCount)
        {
            _splits.Clear();
            for (int i = 0; i < splitCount; i++) _splits.Add(Unreached);
        }

        /// <summary>The single line that becomes NetworkManager.ServerTime at port. Keep it single.</summary>
        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            Elapsed += deltaSeconds;
        }

        /// <summary>
        /// Records cumulative elapsed time at checkpoint <paramref name="index"/>.
        /// </summary>
        /// <remarks>
        /// Out-of-order and skipped indices are accepted on purpose. CLAUDE.md says to bless
        /// sequence breaks rather than patch them, and any "must follow the previous split" check
        /// here would quietly outlaw every one of them.
        /// </remarks>
        public void Mark(int index)
        {
            if (index < 0) return;

            while (_splits.Count <= index) _splits.Add(Unreached);
            if (_splits[index] >= 0f) return;

            _splits[index] = Elapsed;
        }

        public bool WasReached(int index)
        {
            return index >= 0 && index < _splits.Count && _splits[index] >= 0f;
        }

        public float[] ToArray()
        {
            return _splits.ToArray();
        }

        public void Reset()
        {
            Elapsed = 0f;
            for (int i = 0; i < _splits.Count; i++) _splits[i] = Unreached;
        }
    }
}
