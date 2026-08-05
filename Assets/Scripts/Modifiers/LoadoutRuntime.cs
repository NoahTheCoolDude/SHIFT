using System;
using Shift.Shared;
using UnityEngine;

namespace Shift.Modifiers
{
    /// <summary>
    /// The player's four equipped modifiers and which one is live.
    /// </summary>
    /// <remarks>
    /// Contains no input handling on purpose. When netcode lands, <see cref="Equip"/> is what a
    /// server RPC calls; keeping input out of it now is the difference between a port and a
    /// rewrite. <see cref="LoadoutHotkeys"/> supplies input separately.
    /// </remarks>
    [RequireComponent(typeof(PrimitiveCarrier))]
    public class LoadoutRuntime : MonoBehaviour
    {
        public const int SlotCount = 4;

        [SerializeField] private ModifierDefinition[] _slots = new ModifierDefinition[SlotCount];

        private PrimitiveCarrier _carrier;
        private int _activeSlot = -1;

        /// <summary>-1 means baseline: no modifier equipped.</summary>
        public int ActiveSlot => _activeSlot;

        public ModifierDefinition Active =>
            _activeSlot >= 0 && _activeSlot < _slots.Length ? _slots[_activeSlot] : null;

        public event Action<int, ModifierDefinition> SlotChanged;

        private void Awake()
        {
            _carrier = GetComponent<PrimitiveCarrier>();

            if (_slots.Length == SlotCount) return;
            Array.Resize(ref _slots, SlotCount);
        }

        private void Start()
        {
            Apply();
        }

        public ModifierDefinition GetSlot(int slot)
        {
            return slot >= 0 && slot < _slots.Length ? _slots[slot] : null;
        }

        /// <summary>Equips a slot, or pass -1 to drop back to the unmodified baseline.</summary>
        public void Equip(int slot)
        {
            int clamped = slot >= 0 && slot < _slots.Length && _slots[slot] != null ? slot : -1;
            if (clamped == _activeSlot) return;

            _activeSlot = clamped;
            Apply();
            SlotChanged?.Invoke(_activeSlot, Active);
        }

        private void Apply()
        {
            if (_carrier != null) _carrier.SetModifier(Active);
        }
    }
}
