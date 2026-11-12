using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Dashable Env Obj. A box that slides when the Player dashes into its side.
    ///   Player walks into it        -> blocked, the box is locked in place
    ///   Player dashes into it   -> the box slides away for pushDistance.
    ///   Can be blocked by the other object behind the box.
    ///   It can keep sliding and falls
    ///   Player dashes onto the top  -> nothing happens
    ///   Player respawns             -> the box also respawn to where it started (prevent bug)
    /// The Player can stand on it, so it works as a step to reach higher places.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DashableBox : Dashable
    {
        // Locked: can only fall. Sliding: can also move sideways.
        private const RigidbodyConstraints Locked =
            RigidbodyConstraints.FreezeRotation // Can't tip over.
            | RigidbodyConstraints.FreezePositionX
            | RigidbodyConstraints.FreezePositionZ;
        private const RigidbodyConstraints Sliding = RigidbodyConstraints.FreezeRotation;

        [SerializeField]
        private float pushDistance = 2f;

        [SerializeField]
        private float slideSpeed = 8f;

        private Rigidbody rb;
        private Collider boxCollider;
        private Vector3 startPosition;
        private Vector3 slideStart;
        private Vector3 slideDirection;
        private bool isSliding;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            boxCollider = GetComponent<Collider>();
            rb.constraints = Locked;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            startPosition = rb.position;
        }

        private void OnEnable()
        {
            PlayerEvents.Spawned += ResetToStart;
        }

        private void OnDisable()
        {
            PlayerEvents.Spawned -= ResetToStart;
        }

        protected override void OnDashed(Collision collision)
        {
            if (isSliding)
            {
                return;
            }

            // The contact normal points from the Player into the box, so that is the push direction.
            Vector3 push = Vector3.ProjectOnPlane(collision.GetContact(0).normal, Vector3.up);

            if (push.sqrMagnitude < 0.5f) // the Player hit the top, not a side
            {
                return;
            }

            slideDirection = push.normalized;
            slideStart = rb.position;
            isSliding = true;
            rb.constraints = Sliding;
            SetSlideVelocity(slideSpeed);
        }

        private void FixedUpdate()
        {
            if (!isSliding)
            {
                return;
            }

            Vector3 moved = Vector3.ProjectOnPlane(rb.position - slideStart, Vector3.up);
            Vector3 flatVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);

            bool farEnough = moved.magnitude >= pushDistance && HasFloorBelow();
            bool blocked = flatVelocity.magnitude < 0.1f; // detect if the box is blocked by another object behind it, because the speed drops to 0

            if (farEnough || blocked)
            {
                StopSliding();
                return;
            }

            SetSlideVelocity(slideSpeed); // every step, so floor friction can't slow it down
        }

        // So the box can check the floor under it, and if it is not there, it keeps sliding and falls.
        private bool HasFloorBelow()
        {
            Bounds bounds = boxCollider.bounds;
            return Physics.Raycast(
                bounds.center,
                Vector3.down,
                bounds.extents.y + 0.1f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        // Sets only the slide speed. The falling speed is left to gravity.
        private void SetSlideVelocity(float speed)
        {
            Vector3 velocity = slideDirection * speed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }

        // To only move the box in predefined distance, can't slide too much.
        private void StopSliding()
        {
            isSliding = false;
            SetSlideVelocity(0f);
            rb.constraints = Locked;
        }

        // Every respawn puts it back, in case it's stuck due to path blocked.
        private void ResetToStart()
        {
            StopSliding();
            rb.linearVelocity = Vector3.zero;
            rb.position = startPosition;
        }
    }
}
