using Shift.Progression;
using UnityEngine;

namespace Shift.Level
{
    /// <summary>
    /// Shared plumbing for the level's trigger volumes: resolving the director and recognising
    /// the player.
    /// </summary>
    /// <remarks>
    /// A base class rather than composition — the alternative is the same eight lines copied into
    /// five components, and there is no behaviour here to compose, only lookup.
    ///
    /// NEVER put a subclass on the Prop layer. PHASE works by setting excludeLayers, and
    /// excludeLayers suppresses trigger callbacks too — a ghost would walk through the finish
    /// line, or fall past the kill volume forever.
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public abstract class ZoneTrigger : MonoBehaviour
    {
        [SerializeField] protected RunDirector _director;

        protected virtual void Awake()
        {
            if (_director == null) _director = FindFirstObjectByType<RunDirector>();
        }

        protected virtual void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        /// <summary>The respawner doubles as the "is this the player" test — only they have one.</summary>
        protected static PlayerRespawner FindPlayer(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            return body != null
                ? body.GetComponent<PlayerRespawner>()
                : other.GetComponentInParent<PlayerRespawner>();
        }
    }
}
