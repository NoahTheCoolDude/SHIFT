using System.Collections.Generic;
using Shift.Progression;
using UnityEngine;

namespace Shift.Level
{
    /// <summary>
    /// Restores every child rigidbody to its authored pose when the run restarts.
    /// </summary>
    /// <remarks>
    /// Only on a full restart, never on a checkpoint respawn — otherwise the player resets puzzle
    /// state for free by walking into a kill volume. The corollary is a level-design invariant:
    /// any prop a puzzle depends on must be geometrically unable to leave its room. Walls and a
    /// lip, not code.
    /// </remarks>
    public class PropResetter : MonoBehaviour
    {
        [SerializeField] private RunDirector _director;

        private struct Pose
        {
            public Rigidbody Body;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private readonly List<Pose> _poses = new List<Pose>();

        private void Awake()
        {
            if (_director == null) _director = FindFirstObjectByType<RunDirector>();

            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
            for (int i = 0; i < bodies.Length; i++)
            {
                _poses.Add(new Pose
                {
                    Body = bodies[i],
                    Position = bodies[i].position,
                    Rotation = bodies[i].rotation
                });
            }
        }

        private void OnEnable()
        {
            if (_director != null) _director.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (_director != null) _director.PhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(RunPhase phase)
        {
            if (phase == RunPhase.Idle) ResetProps();
        }

        public void ResetProps()
        {
            for (int i = 0; i < _poses.Count; i++)
            {
                Rigidbody body = _poses[i].Body;
                if (body == null) continue;

                RigidbodyInterpolation previous = body.interpolation;
                body.interpolation = RigidbodyInterpolation.None;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = _poses[i].Position;
                body.rotation = _poses[i].Rotation;

                body.interpolation = previous;
            }

            Physics.SyncTransforms();
        }
    }
}
