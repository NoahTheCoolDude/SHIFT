using Shift.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Shift.Modifiers
{
    /// <summary>
    /// Development-only swap input: 1-4 equip a slot, 0 drops to the unmodified baseline.
    /// </summary>
    /// <remarks>
    /// The 0 key is not a convenience. Instant A/B against baseline feel is the only reliable way
    /// to notice that a modifier has quietly broken the motor, so it stays even when real
    /// bindings arrive.
    /// </remarks>
    [RequireComponent(typeof(LoadoutRuntime))]
    public class LoadoutHotkeys : MonoBehaviour
    {
        private LoadoutRuntime _loadout;

        private void Awake()
        {
            _loadout = GetComponent<LoadoutRuntime>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || UiInputGate.Suppressed) return;

            // Numpad too — a key that silently does nothing reads as a broken feature.
            if (Pressed(keyboard.digit0Key, keyboard.numpad0Key)) _loadout.Equip(-1);
            else if (Pressed(keyboard.digit1Key, keyboard.numpad1Key)) _loadout.Equip(0);
            else if (Pressed(keyboard.digit2Key, keyboard.numpad2Key)) _loadout.Equip(1);
            else if (Pressed(keyboard.digit3Key, keyboard.numpad3Key)) _loadout.Equip(2);
            else if (Pressed(keyboard.digit4Key, keyboard.numpad4Key)) _loadout.Equip(3);
        }

        private static bool Pressed(KeyControl row, KeyControl numpad)
        {
            return row.wasPressedThisFrame || numpad.wasPressedThisFrame;
        }
    }
}
