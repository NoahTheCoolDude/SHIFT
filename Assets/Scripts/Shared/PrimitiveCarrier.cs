using System;
using System.Collections.Generic;
using Shift.Core;
using Shift.Modifiers;
using UnityEngine;

namespace Shift.Shared
{
    /// <summary>
    /// Holds an entity's override stack and exposes the resolved <see cref="PrimitiveState"/>.
    /// This is the seam between the primitive engine and everything that consumes it.
    /// </summary>
    /// <remarks>
    /// Consumers split two ways, deliberately. Continuous consumers — the motor's per-step maths —
    /// poll <see cref="Current"/> and never subscribe, which sidesteps event-ordering entirely.
    /// Discrete consumers — rigidbody mass, physics materials, collider exclusions — subscribe to
    /// <see cref="Changed"/> and apply once per change rather than every frame. Execution order
    /// -100 guarantees resolution happens before any FixedUpdate that reads it.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class PrimitiveCarrier : MonoBehaviour
    {
        [SerializeField] private PrimitiveSubject _subject = PrimitiveSubject.Prop;

        [Tooltip("What this object is by nature, before any field or modifier. A lead crate is heavy.")]
        [SerializeField] private PrimitiveOverride _intrinsic = PrimitiveOverride.Empty;

        private readonly List<PrimitiveField> _fields = new List<PrimitiveField>();
        private readonly List<PrimitiveOverride> _fieldScratch = new List<PrimitiveOverride>();

        private PrimitiveState _current = PrimitiveState.Normal;
        private ModifierDefinition _activeModifier;
        private bool _dirty = true;

        public PrimitiveState Current => _current;
        public PrimitiveSubject Subject => _subject;
        public ModifierDefinition ActiveModifier => _activeModifier;
        public int FieldCount => _fields.Count;

        public event Action<PrimitiveState> Changed;

        private void Awake()
        {
            Resolve();
        }

        private void OnEnable()
        {
            MarkDirty();
        }

        private void OnDisable()
        {
            // A player teleported, disabled, or destroyed inside a trigger never fires
            // OnTriggerExit, and would otherwise keep that field's primitives forever.
            ClearFields();
        }

        private void FixedUpdate()
        {
            if (_dirty) Resolve();
        }

        public void SetModifier(ModifierDefinition modifier)
        {
            if (_activeModifier == modifier) return;

            _activeModifier = modifier;
            MarkDirty();
        }

        public void AddField(PrimitiveField field)
        {
            if (field == null || _fields.Contains(field)) return;

            _fields.Add(field);
            SortFields();
            MarkDirty();
        }

        public void RemoveField(PrimitiveField field)
        {
            if (field == null || !_fields.Remove(field)) return;

            MarkDirty();
        }

        /// <summary>
        /// Drops every field immediately. Respawn needs this: OnTriggerExit is a physics step
        /// away, and that window is long enough to respawn still under 0.3g.
        /// </summary>
        public void ClearFields()
        {
            if (_fields.Count == 0) return;

            _fields.Clear();
            MarkDirty();
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>Priority ascending, ties broken by name so ordering never depends on the
        /// order triggers happened to fire in.</summary>
        private void SortFields()
        {
            _fields.Sort((a, b) =>
            {
                int byPriority = a.Priority.CompareTo(b.Priority);
                return byPriority != 0
                    ? byPriority
                    : string.CompareOrdinal(a.name, b.name);
            });
        }

        private void Resolve()
        {
            _dirty = false;

            _fieldScratch.Clear();
            for (int i = 0; i < _fields.Count; i++)
            {
                if (_fields[i] == null) continue;
                _fieldScratch.Add(_fields[i].Override);
            }

            PrimitiveState resolved = PrimitiveResolver.Resolve(
                PrimitiveState.Normal,
                _intrinsic,
                _fieldScratch,
                _activeModifier != null ? _activeModifier.Override : PrimitiveOverride.Empty);

            if (resolved.Equals(_current)) return;

            _current = resolved;
            Changed?.Invoke(_current);
        }
    }
}
