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
    ///   Hanging over an edge   -> nothing under its middle, so it slides off the edge and falls
    ///   It falls off the level -> it breaks in the air
    ///   Something breaks it    -> a break effect, then it respawns at its start, as soon as nothing is in the way there
    ///   Its start is taken     -> it waits out of sight until the start is free, so it never respawns inside a box or the Player
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

        // How fast a box hanging over an edge slides off it
        private const float SlideOffSpeed = 3f;

        // A box falling this fast has fallen about 7 m, deeper than any drop between floors (the deepest is about 5 m),
        // so it fell off the level and breaks in the air, where the Player still sees it.
        private const float FallBreakSpeed = 12f;

        // A broken box waits this far under its start: beyond the main camera's far clip (1000 m),
        // and above the minimap's radar shapes (2000 m down), so nobody sees it.
        private const float WaitDepth = 1500f;

        private static readonly RaycastHit[] hits = new RaycastHit[8];
        private static readonly Collider[] overlaps = new Collider[8];

        [Tooltip("Plays where the box breaks.")]
        [SerializeField]
        private Effect breakEffect;

        [Tooltip("Plays where the box respawns.")]
        [SerializeField]
        private Effect respawnEffect;

        private Rigidbody rb;
        private BoxCollider boxCollider;
        private Vector3 startPosition;
        private bool waitingForSpace; // broke while its start's space was taken

        // The box's size in the world.
        protected Vector3 Size => Vector3.Scale(boxCollider.size, transform.lossyScale);

        // The box's Rigidbody. Inside physics steps, read its position
        protected Rigidbody Body => rb;

        // Where it started and where it respawns.
        protected Vector3 StartPosition => startPosition;

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
            if (waitingForSpace)
            {
                if (IsStartFree())
                {
                    Respawn();
                }

                return;
            }

            if (rb.linearVelocity.y < -FallBreakSpeed)
            {
                Break();
                return;
            }

            Step();

            if (!rb.isKinematic) // a sliding Boulder moves on its own until it stops
            {
                SlideOffEdge();
            }

            // A box only ever falls. Physics can throw a squeezed box upward, so upward speed is removed.
            Vector3 velocity = rb.linearVelocity;

            if (velocity.y > 0f && !rb.isKinematic) // a sliding Boulder is kinematic, its velocity can't be set
            {
                velocity.y = 0f;
                rb.linearVelocity = velocity;
            }
        }

        // A box whose middle is over nothing only rests on an edge with a corner.
        // It slides away from that edge and falls, so it can't hang in the air and block the way.
        private void SlideOffEdge()
        {
            Bounds box = boxCollider.bounds;

            if (
                Physics.Raycast(
                    box.center,
                    Vector3.down,
                    box.extents.y + 0.1f,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                return; // something is under its middle
            }

            // What it rests on: anything in a thin slice just under its bottom.
            int count = Physics.OverlapBoxNonAlloc(
                new Vector3(box.center.x, box.min.y - 0.05f, box.center.z),
                new Vector3(box.extents.x, 0.05f, box.extents.z),
                overlaps,
                Quaternion.identity,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < count; i++)
            {
                Vector3 away = box.center - overlaps[i].bounds.ClosestPoint(box.center);
                away.y = 0f;

                if (overlaps[i] != boxCollider && away.sqrMagnitude > 0.0001f)
                {
                    Unlock();
                    rb.linearVelocity =
                        away.normalized * SlideOffSpeed
                        + Vector3.up * Mathf.Min(rb.linearVelocity.y, 0f);
                    return;
                }
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
        // These don't count: the box itself, the Player
        // What physics ignores for this box (a Crate and the blades): a cast doesn't know about ignored pairs.
        // Prevent clipping
        public float FreeDistance(Vector3 direction, float maxDistance, List<Rigidbody> moving) // ignored rigidbodies
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
                    || Physics.GetIgnoreCollision(boxCollider, hits[i].collider) // a Crate or blades
                    || (body != null && moving != null && moving.Contains(body))
                    || (body == null && alreadyOverlaps);

                if (!ignored)
                {
                    free = Mathf.Min(free, hits[i].distance - Skin); // the real box touches it Skin earlier
                }
            }

            return Mathf.Max(free, 0f);
        }

        // The box breaks where it is and respawns at its start, as soon as nothing is in the way there.
        // While it waits, it stays far under its start, out of sight, without falling.
        public virtual void Break()
        {
            breakEffect.Play(rb.position);
            Lock();
            rb.linearVelocity = Vector3.zero;

            if (IsStartFree())
            {
                Respawn();
            }
            else
            {
                waitingForSpace = true;
                rb.isKinematic = true;
                transform.position = startPosition + Vector3.down * WaitDepth;
            }
        }

        private void Respawn()
        {
            waitingForSpace = false;
            rb.isKinematic = false;
            transform.position = startPosition;
            respawnEffect.Play(startPosition);
        }

        // Nothing solid at the start position, apart from the floor under it (the bottom is lifted like in FreeDistance).
        private bool IsStartFree()
        {
            Vector3 halfSize = Size / 2f - Vector3.one * Skin;
            halfSize.y -= FloorClearance / 2f;
            Vector3 center =
                startPosition
                + (boxCollider.bounds.center - rb.position)
                + transform.up * (FloorClearance / 2f);

            int count = Physics.OverlapBoxNonAlloc(
                center,
                halfSize,
                overlaps,
                transform.rotation,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < count; i++)
            {
                if (overlaps[i] != boxCollider)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
