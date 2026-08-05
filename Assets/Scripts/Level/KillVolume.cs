using UnityEngine;

namespace Shift.Level
{
    /// <summary>
    /// Sends the player back to their last checkpoint. The clock keeps running and the run phase
    /// does not change — falling costs time, never the attempt.
    /// </summary>
    public class KillVolume : ZoneTrigger
    {
        private void OnTriggerEnter(Collider other)
        {
            PlayerRespawner player = FindPlayer(other);
            if (player == null) return;

            player.RespawnAtCheckpoint();
        }
    }
}
