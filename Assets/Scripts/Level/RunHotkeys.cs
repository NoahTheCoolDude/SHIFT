using Shift.Progression;
using Shift.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shift.Level
{
    /// <summary>
    /// R respawns at the last checkpoint; Shift+R restarts the run.
    /// </summary>
    /// <remarks>
    /// Input kept separate from the verbs, exactly as LoadoutHotkeys does — when netcode lands,
    /// the calls become RPCs and this component is unchanged.
    /// </remarks>
    [RequireComponent(typeof(PlayerRespawner))]
    public class RunHotkeys : MonoBehaviour
    {
        [SerializeField] private RunDirector _director;

        private PlayerRespawner _respawner;

        private void Awake()
        {
            _respawner = GetComponent<PlayerRespawner>();
            if (_director == null) _director = FindFirstObjectByType<RunDirector>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || UiInputGate.Suppressed) return;
            if (!keyboard.rKey.wasPressedThisFrame) return;

            bool restart = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            if (restart) RestartRun();
            else _respawner.RespawnAtCheckpoint();
        }

        public void RestartRun()
        {
            // Director first: PropResetter listens for the Idle phase, so props are back on their
            // start poses before the player lands next to them.
            if (_director != null) _director.ResetRun();
            _respawner.RespawnAtStart();
        }
    }
}
