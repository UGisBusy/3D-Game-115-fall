using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Base of the boxes the Player can push (Movable Box, Dashable Box). Each type have different way to push the box, but they all share the same physics.
    ///   Player pushes a side       -> the box is free to move, physics pushes it at the Player's speed
    ///   Player stops pushing       -> the box stops. A type with a glide time slows down to a stop first
    ///   No floor under it          -> it falls, gravity always works on it
    ///   Something breaks it        -> it goes straight back to its start position (immediately respawns)
    ///   Player respawns            -> nothing happens, a box stays wherever it is
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public abstract class PushableBox : MonoBehaviour
    {
        // Locked: can only fall. Pushed: can also move sideways. It never tips over.
        private const RigidbodyConstraints Locked =
            RigidbodyConstraints.FreezeRotation
            | RigidbodyConstraints.FreezePositionX
            | RigidbodyConstraints.FreezePositionZ;
        private const RigidbodyConstraints Pushed = RigidbodyConstraints.FreezeRotation;

        private Rigidbody rb;
        private Vector3 startPosition;
        private bool isPushed; // the Player pushed a side in the last physics step
        private float glideTimer; // how much longer it keeps sliding after the push ended

        // How long this type of box keeps sliding after a push ends. 0 means it stops at once.
        protected virtual float GlideTime => 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = Locked;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            startPosition = rb.position;
        }

        // Whether the Player may push this type of box right now.
        protected abstract bool CanPush(Player player);

        // Runs after every physics step while something touches the box.
        private void OnCollisionStay(Collision collision)
        {
            if (collision.collider.TryGetComponent(out Player player) && CanPush(player))
            {
                // The contact normal points from the Player into the box.
                Vector3 push = Vector3.ProjectOnPlane(collision.GetContact(0).normal, Vector3.up);

                if (push.sqrMagnitude > 0.5f) // only from the sides of the box
                {
                    isPushed = true;
                    rb.constraints = Pushed;
                }
            }
        }

        // Runs before the physics step.
        private void FixedUpdate()
        {
            Vector3 velocity = rb.linearVelocity;

            if (isPushed)
            {
                glideTimer = GlideTime; // it can glide again once this push ends
            }
            else if (glideTimer > 0f)
            {
                // Glide: the speed the push left behind shrinks evenly to zero over the glide time.
                // The speed is only ever reduced, never set, so a box that hits something just stops there.
                float remaining = Mathf.Max(glideTimer - Time.fixedDeltaTime, 0f);
                velocity.x *= remaining / glideTimer;
                velocity.z *= remaining / glideTimer;
                glideTimer = remaining;
            }
            else
            {
                // No push and no glide left: the box stops moving sideways.
                velocity.x = 0f;
                velocity.z = 0f;
                rb.constraints = Locked;
            }

            // A box only ever falls. Physics can throw a squeezed box upward, so upward speed is removed.
            if (velocity.y > 0f)
            {
                velocity.y = 0f;
            }

            rb.linearVelocity = velocity;
            isPushed = false; // OnCollisionStay sets it again if the Player is still pushing
        }

        // This will be called by what destroys boxes (e.g., a hidden spike, the death zone, a platform reset).
        // The box respawns at its start right away.
        public void Break()
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = startPosition;
        }
    }
}
