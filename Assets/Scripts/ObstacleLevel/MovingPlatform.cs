using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Independent Env Obj. A platform that keeps moving along a route of points and carries what stands on it.
    ///   Level starts   -> waits for startDelay at the start point, then starts its route
    ///   Route          -> start point, destination 1, destination 2, ... then straight back to the start point, again and again
    ///   At each point  -> waits for waitTime, then travels to the next point at moveSpeed
    ///   Travelling     -> carries everything standing on it, with or without the Player
    /// Start point -> initial position.
    /// Its position is worked out from a clock, not added up step by step, so it never drifts.
    /// Platforms whose loops take the same time stay in step forever, and startDelay lines them up
    /// for jumps from one platform to the next.
    /// Carrying: can carry the player and the boxes. A wall in a box's way stops the box, the platform goes on without it.
    /// The Player is carried through PlayerMotor.Carry (the platform's speed is added to the Player's own movement),
    /// not by moving the Player's Rigidbody from here.
    /// Path preview: dots along the whole route, with bigger dots where it stops, so the Player can plan the ride.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(-10)] // before PlayerMotor, so the Player is carried in the same physics step the platform moves
    public class MovingPlatform : MonoBehaviour
    {
        private const float RiderCheckHeight = 0.2f; // how far above the top surface that counts as standing on it

        [Tooltip("The MovingPlatform's destination points to visit.")]
        [SerializeField]
        private Transform[] destinations;

        [SerializeField]
        private float moveSpeed = 4f;

        [Tooltip("How long it waits at each point.")]
        [SerializeField]
        private float waitTime = 1f;

        [Tooltip("How long it waits at the start point before it moves for the first time.")]
        [SerializeField]
        private float startDelay;

        [Header("Path Preview")]
        [Tooltip(
            "A small mark placed along the route, so the Player sees where the platform goes."
        )]
        [SerializeField]
        private GameObject pathDot;

        [SerializeField]
        private float dotSpacing = 1f;

        [Tooltip("The points where the platform stops get a mark this many times bigger.")]
        [SerializeField]
        private float stopDotScale = 2.5f;

        private static readonly Collider[] buffer = new Collider[16];
        private readonly List<Rigidbody> riders = new(); // who stands on it in this physics step

        private Rigidbody rb;
        private Collider platformCollider; // the round top the riders stand on
        private Vector3[] route; // destination points the platform will move to.
        private float[] travelTimes; // how long each move takes: from route[i] to the next point
        private float loopTime; // how long the whole route takes, waits included
        private float clock; // seconds since the level started

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            platformCollider = GetComponentInChildren<Collider>(); // the round top (a child), so the cloud has no square corners
            rb.isKinematic = true; // independent env obj
            rb.interpolation = RigidbodyInterpolation.Interpolate; // moves in FixedUpdate

            // The destinations are the platform's children.
            // Their positions are copied once here, before the platform starts moving.
            route = new Vector3[destinations.Length + 1];
            route[0] = rb.position;

            for (int i = 0; i < destinations.Length; i++)
            {
                route[i + 1] = destinations[i].position;
            }

            travelTimes = new float[route.Length];

            for (int i = 0; i < route.Length; i++)
            {
                travelTimes[i] = Vector3.Distance(route[i], NextPoint(i)) / moveSpeed;
                loopTime += waitTime + travelTimes[i];
            }

            ShowPath();
        }

        // Dots along every move of the route, evenly spaced, and a big dot at each stop.
        // They go under the platform's parent, so they stay in place while the platform moves.
        private void ShowPath()
        {
            for (int i = 0; i < route.Length; i++)
            {
                PlaceDot(route[i], stopDotScale);

                int gaps = Mathf.Max(
                    1,
                    Mathf.RoundToInt(Vector3.Distance(route[i], NextPoint(i)) / dotSpacing)
                );

                for (int k = 1; k < gaps; k++)
                {
                    PlaceDot(Vector3.Lerp(route[i], NextPoint(i), (float)k / gaps), 1f);
                }
            }
        }

        private void PlaceDot(Vector3 position, float scale)
        {
            GameObject dot = Instantiate(pathDot, position, Quaternion.identity, transform.parent);
            dot.transform.localScale *= scale;
        }

        private void FixedUpdate()
        {
            clock += Time.fixedDeltaTime;
            Vector3 next = PositionAt(clock);
            Vector3 step = next - rb.position;

            if (step == Vector3.zero)
            {
                return; // waiting at a point
            }

            rb.MovePosition(next);
            FindRiders();

            foreach (Rigidbody rider in riders)
            {
                Carry(rider, step);
            }
        }

        // Where the route puts the platform at time t. At each point it first waits, then travels to the next one.
        private Vector3 PositionAt(float t)
        {
            if (t < startDelay)
            {
                return route[0];
            }

            t = (t - startDelay) % loopTime; // time into the current loop

            for (int i = 0; i < route.Length; i++)
            {
                if (t < waitTime)
                {
                    return route[i];
                }

                t -= waitTime;

                if (t < travelTimes[i])
                {
                    return Vector3.Lerp(route[i], NextPoint(i), t / travelTimes[i]);
                }

                t -= travelTimes[i];
            }

            return route[0];
        }

        // The point after route[i]. After the last destination it is the start point.
        private Vector3 NextPoint(int i)
        {
            return route[(i + 1) % route.Length];
        }

        // Fills riders with everything standing on the platform.
        private void FindRiders()
        {
            riders.Clear();
            AddRidersOn(platformCollider);

            // Player can also stand on the object that is on the platform (e.g., a crate).
            // The list grows while it is walked, so those are checked as well.
            for (int i = 0; i < riders.Count; i++)
            {
                AddRidersOn(riders[i].GetComponent<Collider>());
            }
        }

        // Adds every Rigidbody in a thin box right above the top surface of ground.
        // A box check is used, not collision events, because a resting box falls asleep and sends no events.
        private void AddRidersOn(Collider ground)
        {
            Bounds bounds = ground.bounds;
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

                // Kinematic bodies move on their own (e.g., this platform, a Gate), so they are not carried.
                if (body != null && !body.isKinematic && !riders.Contains(body))
                {
                    riders.Add(body);
                }
            }
        }

        // Moves the rider by the same step the platform moved.
        private void Carry(Rigidbody rider, Vector3 step)
        {
            // The Player is moved by its own PlayerMotor (Carry), so only one script moves the Player's Rigidbody.
            if (rider.TryGetComponent(out Player player))
            {
                player.Motor.Carry(step / Time.fixedDeltaTime);
                return;
            }

            // A box is placed directly, which skips collisions.
            Vector3 sideways = new(step.x, 0f, step.z);

            if (
                sideways != Vector3.zero
                && rider.TryGetComponent(out PushableBox box)
                && box.FreeDistance(sideways.normalized, sideways.magnitude, riders)
                    < sideways.magnitude
            )
            {
                step -= sideways;
            }

            rider.transform.position += step; // a box locks its X and Z, so it is placed directly from the transform, not the Rigidbody
        }
    }
}
