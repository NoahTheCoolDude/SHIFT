using UnityEngine;

namespace Shift.Level
{
    /// <summary>
    /// The start line. The clock begins when the player <em>leaves</em> the box, so standing on
    /// the pad deciding a loadout is free — the run starts when you commit to it.
    /// </summary>
    public class ZoneStart : ZoneTrigger
    {
        [Tooltip("Where a full restart puts the player. Defaults to this object's transform.")]
        [SerializeField] private Transform _spawnPose;

        public Vector3 SpawnPosition => _spawnPose != null ? _spawnPose.position : transform.position;
        public float SpawnYaw => _spawnPose != null ? _spawnPose.eulerAngles.y : transform.eulerAngles.y;

        private void OnTriggerEnter(Collider other)
        {
            PlayerRespawner player = FindPlayer(other);
            if (player == null) return;

            player.SetCheckpoint(SpawnPosition, SpawnYaw);
        }

        private void OnTriggerExit(Collider other)
        {
            if (FindPlayer(other) == null || _director == null) return;

            _director.Begin();
        }
    }
}
