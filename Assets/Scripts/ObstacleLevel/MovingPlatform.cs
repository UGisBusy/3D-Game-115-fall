using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Independent Env Obj. A platform that moves along a route of points and carries what stands on it.
    ///   Route          -> start point, destination 1, destination 2, ... then straight back to the start point
    ///   At a point     -> stays there until the Player stands on it, then leaves for the next point
    ///   Travelling     -> goes to the next point with or without the Player, and carries everything standing on it
    ///   When reachs destination     -> waits for waitTime, then it is "at a point" again
    ///   Player respawns -> goes back to the start point, to prevent platform stuck.
    /// Start point     -> initial position.
    /// Carrying: can carry the player and the crates.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class MovingPlatform : MonoBehaviour
    {
        private const float RiderCheckHeight = 0.2f; // how far above the top surface still counts as standing on it

        [Tooltip("The MovingPlatform's destination points to visit.")]
        [SerializeField]
        private Transform[] destinations;

        [SerializeField]
        private float moveSpeed = 2f;

        [Tooltip("How long it waits at each point.")]
        [SerializeField]
        private float waitTime = 3f;

        private static readonly Collider[] buffer = new Collider[16];
        private readonly List<Rigidbody> riders = new(); // who stands on it in this physics step

        private Rigidbody rb;
        private Collider platformCollider;
        private Vector3[] route; // destination points the platform will move to.
        private int targetIndex = 1; // which point of the route it is going to
        private float waitTimer;
        private bool isTravelling; // between two points

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            platformCollider = GetComponent<Collider>();
            rb.isKinematic = true; // independent env obj
            rb.interpolation = RigidbodyInterpolation.Interpolate; // moves in FixedUpdate

            // The destinations are children, so they move together with the platform.
            // Their positions are copied once here, before the platform starts moving.
            route = new Vector3[destinations.Length + 1];
            route[0] = rb.position;

            for (int i = 0; i < destinations.Length; i++)
            {
                route[i + 1] = destinations[i].position;
            }
        }

        private void OnEnable()
        {
            PlayerEvents.Spawned += ResetToStart;
        }

        private void OnDisable()
        {
            PlayerEvents.Spawned -= ResetToStart;
        }

        private void FixedUpdate()
        {
            FindRiders();

            if (waitTimer > 0f)
            {
                waitTimer -= Time.fixedDeltaTime;
                return;
            }

            if (!isTravelling && IsPlayerRiding())
            {
                isTravelling = true; // leave the point
            }

            if (isTravelling)
            {
                Move();
            }
        }

        // Fills riders with every Rigidbody in a thin box right above the top surface.
        // A box check is used, not collision events, because a resting box falls asleep and sends no events.
        private void FindRiders()
        {
            riders.Clear();

            Bounds bounds = platformCollider.bounds;
            Vector3 halfSize = new(bounds.extents.x, RiderCheckHeight / 2f, bounds.extents.z);
            Vector3 center = bounds.center + Vector3.up * (bounds.extents.y + halfSize.y);

            int count = Physics.OverlapBoxNonAlloc(
                center,
                halfSize,
                buffer,
                Quaternion.identity,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < count; i++)
            {
                Rigidbody body = buffer[i].attachedRigidbody;

                // Kinematic bodies move on their own (this platform, a Gate), so they are not carried.
                if (body != null && !body.isKinematic && !riders.Contains(body))
                {
                    riders.Add(body);
                }
            }
        }

        // Returns true if the Player is standing on the platform.
        private bool IsPlayerRiding()
        {
            foreach (Rigidbody rider in riders)
            {
                if (rider.TryGetComponent(out Player _))
                {
                    return true;
                }
            }

            return false;
        }

        // Platform movement
        private void Move()
        {
            Vector3 target = route[targetIndex];
            Vector3 next = Vector3.MoveTowards(
                rb.position,
                target,
                moveSpeed * Time.fixedDeltaTime
            );
            Vector3 step = next - rb.position;

            rb.MovePosition(next);

            foreach (Rigidbody rider in riders)
            {
                Carry(rider, step);
            }

            if (next == target)
            {
                Arrive();
            }
        }

        // Moves a rider by the same step the platform moved.
        private static void Carry(Rigidbody rider, Vector3 step)
        {
            if (rider.TryGetComponent(out Player _))
            {
                rider.MovePosition(rider.position + step); // keeps the Player's movement smooth
            }
            else
            {
                rider.transform.position += step; // a box locks its X and Z, so it is placed directly from the transform, not the Rigidbody
            }
        }

        // Stops at the point it reached and picks the next one.
        private void Arrive()
        {
            isTravelling = false;
            waitTimer = waitTime;
            targetIndex++;

            if (targetIndex == route.Length) // if no destination left, the next point is the start point
            {
                targetIndex = 0;
            }
        }

        // To reset the platform when respawned, to prevent no one can reach the platform and stuck.
        private void ResetToStart()
        {
            rb.position = route[0];
            targetIndex = 1;
            waitTimer = 0f;
            isTravelling = false;
        }
    }
}
