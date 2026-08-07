using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shift.Progression
{
    /// <summary>
    /// Owns the run: phase, clock, splits, and record submission. One per Zone scene.
    /// </summary>
    /// <remarks>
    /// This assembly cannot reference the player, the primitive engine, or a collider — the asmdef
    /// enforces it. That is the point: when this becomes a NetworkBehaviour it drags nothing from
    /// the gameplay layer with it, and Begin/Split/Finish/ResetRun each become one ServerRpc.
    ///
    /// No singleton and no static Active. Consumers hold a serialized reference and fall back to
    /// FindFirstObjectByType, matching how every other component in this project resolves.
    /// </remarks>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class RunDirector : MonoBehaviour
    {
        [SerializeField] private ZoneManifest _manifest;

        private readonly RunClock _clock = new RunClock();
        private RunRecordStore _store;

        public RunPhase Phase { get; private set; } = RunPhase.Idle;
        public float Elapsed => _clock.Elapsed;
        public IReadOnlyList<float> Splits => _clock.Splits;
        public ZoneManifest Manifest => _manifest;
        public RunRecord PersonalBest { get; private set; }
        public bool LastRunWasBest { get; private set; }

        public event Action<RunPhase> PhaseChanged;

        private void Awake()
        {
            _store = new RunRecordStore();
            _clock.Resize(_manifest != null ? _manifest.SplitCount : 0);
            PersonalBest = _manifest != null ? _store.GetBest(_manifest.ZoneId) : null;
        }

        private void Update()
        {
            if (Phase != RunPhase.Running) return;

            // Unscaled, and it keeps ticking while paused. A clock that stops on pause is both a
            // pause-buffer exploit and a plan-the-next-section-for-free exploit; charging real
            // time removes both without a line of anti-cheat.
            _clock.Advance(Time.unscaledDeltaTime);
        }

        public void Begin()
        {
            if (Phase != RunPhase.Idle) return;

            _clock.Reset();
            LastRunWasBest = false;
            SetPhase(RunPhase.Running);
        }

        /// <summary>Records a checkpoint. Out-of-order and skipped indices are legal — see <see cref="RunClock.Mark"/>.</summary>
        public void Split(int index)
        {
            if (Phase != RunPhase.Running) return;
            _clock.Mark(index);
        }

        public void Finish()
        {
            if (Phase != RunPhase.Running) return;

            SetPhase(RunPhase.Finished);

            if (_manifest == null) return;

            RunRecord record = new RunRecord(
                _manifest.ZoneId, _clock.Elapsed, _clock.ToArray(), DateTime.UtcNow);

            LastRunWasBest = _store.Submit(record);
            if (LastRunWasBest) PersonalBest = record;
        }

        public void ResetRun()
        {
            _clock.Reset();
            LastRunWasBest = false;
            SetPhase(RunPhase.Idle);
        }

        private void SetPhase(RunPhase phase)
        {
            if (Phase == phase) return;

            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
