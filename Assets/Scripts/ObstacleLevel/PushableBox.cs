using System.Collections.Generic;
using EffectSystem;
using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Base of the boxes the Player can push (Movable Box, Dashable Box). It owns what both boxes share:
    ///   Nobody moves it        -> locked in place, it can only fall
    ///   No floor under it      -> it falls, gravity always works on it
    ///   Something breaks it    -> it goes straight back to its start position (immediately respawns)
    ///   Player respawns        -> nothing happens, a box stays wherever it is
    /// Each type decides how the Player moves it, in OnPushed and Step.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public abstract class PushableBox : MonoBehaviour
    {
        // Locked: can only fall. Free: can also move sideways. It never tips over.
        private const RigidbodyConstraints Locked =
            RigidbodyConstraints.FreezeRotation
            | RigidbodyConstraints.FreezePositionX
            | RigidbodyConstraints.FreezePositionZ;
        private const RigidbodyConstraints Free = RigidbodyConstraints.FreezeRotation;

        // FreeDistance checks with a box this much smaller on every side
        private const float Skin = 0.05f;

        // FreeDistance also leaves out the bottom of the box up to this height
        private const float FloorClearance = 0.25f;

        private static readonly RaycastHit[] hits = new RaycastHit[8];

        [Tooltip("Plays where the box comes back after it breaks.")]
        [SerializeField]
        private Effect respawnEffect;

        private Rigidbody rb;
        private BoxCollider boxCollider;
        private Vector3 startPosition;

        // The box's size in the world.
        protected Vector3 Size => Vector3.Scale(boxCollider.size, transform.lossyScale);

        // The box's Rigidbody. Inside physics steps, read its position
        protected Rigidbody Body => rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            boxCollider = GetComponent<BoxCollider>();
            rb.constraints = Locked;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            startPosition = rb.position;
        }

        // Runs after every physics step while something touches the box.
        private void OnCollisionStay(Collision collision)
        {
            if (!collision.collider.TryGetComponent(out Player player))
            {
                return;
            }

            // The contact normal points from the Player into the box.
            Vector3 push = Vector3.ProjectOnPlane(collision.GetContact(0).normal, Vector3.up);

            if (push.sqrMagnitude > 0.5f) // only from the sides of the box
            {
                OnPushed(player, push.normalized);
            }
        }

        // The Player is pushing a side of the box, in this direction.
        protected abstract void OnPushed(Player player, Vector3 direction);

        // Runs before every physics step. The type moves the box, or keeps it in place with Lock.
        protected abstract void Step();

        private void FixedUpdate()
        {
            Step();

            // A box only ever falls. Physics can throw a squeezed box upward, so upward speed is removed.
            Vector3 velocity = rb.linearVelocity;

            if (velocity.y > 0f)
            {
                velocity.y = 0f;
                rb.linearVelocity = velocity;
            }
        }

        // Lets the box move sideways.
        protected void Unlock()
        {
            rb.constraints = Free;
        }

        // Stops the box sideways and locks it in place. The falling speed is left to gravity.
        protected void Lock()
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.constraints = Locked;
        }

        // How far the box can move in direction, up to maxDistance, before it touches something solid.
        // These don't count: the box itself, the Player (a dash presses the Player into the box).
        // Prevent clipping
        public float FreeDistance(Vector3 direction, float maxDistance, List<Rigidbody> moving)
        {
            Vector3 halfSize = Size / 2f - Vector3.one * Skin;
            halfSize.y -= FloorClearance / 2f;

            int count = Physics.BoxCastNonAlloc(
                boxCollider.bounds.center + transform.up * (FloorClearance / 2f), // the bottom is lifted
                halfSize,
                direction,
                hits,
                transform.rotation,
                maxDistance + Skin, // the cast box is Skin smaller, so it has Skin further to go
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            float free = maxDistance;

            for (int i = 0; i < count; i++)
            {
                Rigidbody body = hits[i].rigidbody; // null for the level itself (islands, walls)
                bool alreadyOverlaps = hits[i].distance <= 0f; // a cast reports these at distance 0
                bool ignored =
                    body == rb
                    || hits[i].collider.TryGetComponent(out Player _)
                    || (body != null && moving != null && moving.Contains(body))
                    || (body == null && alreadyOverlaps);

                if (!ignored)
                {
                    free = Mathf.Min(free, hits[i].distance - Skin); // the real box touches it Skin earlier
                }
            }

            return Mathf.Max(free, 0f);
        }

        // The box respawns at its start right away.
        public virtual void Break()
        {
            Lock();
            rb.linearVelocity = Vector3.zero;
            transform.position = startPosition;
            respawnEffect.Play(startPosition);
        }
    }
}
