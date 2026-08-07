using UnityEngine;

namespace Shift.Level
{
    /// <summary>
    /// The finish line.
    /// </summary>
    /// <remarks>
    /// Deliberately does not check that every checkpoint was reached. CLAUDE.md says to bless
    /// sequence breaks rather than patch them, and a completeness check here would outlaw all of
    /// them in one line.
    /// </remarks>
    public class ZoneGoal : ZoneTrigger
    {
        private void OnTriggerEnter(Collider other)
        {
            if (FindPlayer(other) == null || _director == null) return;

            _director.Finish();
        }
    }
}
