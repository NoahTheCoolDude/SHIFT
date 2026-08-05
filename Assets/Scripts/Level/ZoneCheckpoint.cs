using UnityEngine;

namespace Shift.Level
{
    /// <summary>
    /// Records a split and becomes the new respawn point.
    /// </summary>
    /// <remarks>
    /// Carries only an index; the label lives on the ZoneManifest so the HUD can draw the whole
    /// split table before any checkpoint has been reached.
    /// </remarks>
    public class ZoneCheckpoint : ZoneTrigger
    {
        [Tooltip("Index into ZoneManifest's split labels. Need not be reached in order.")]
        [SerializeField] private int _index;

        [Tooltip("Where respawning sends the player. Defaults to this object's transform.")]
        [SerializeField] private Transform _respawnPose;

        public int Index => _index;

        private void OnTriggerEnter(Collider other)
        {
            PlayerRespawner player = FindPlayer(other);
            if (player == null) return;

            Transform pose = _respawnPose != null ? _respawnPose : transform;
            player.SetCheckpoint(pose.position, pose.eulerAngles.y);

            if (_director != null) _director.Split(_index);
        }
    }
}
