using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Movable Env Obj. A small box that the Player pushes just by moving into it.
    ///   Player moves or dashes into a side -> the box is free to move, physics pushes it at the Player's speed
    ///   Player stops touching the side     -> the box stops and locks in place, so it never slides on alone
    ///   Player stands on the top           -> nothing happens
    ///   No floor under it                  -> it falls, gravity always works on it
    ///   Player respawns                    -> the box also respawns to where it started (prevent bug)
    /// The box follows the player's speed, so the box can stick to the player (not overpushed).
    /// Its weight is the Rigidbody's mass: a heavier box takes longer to reach the Player's speed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MovableBox : MonoBehaviour
    {
        // Locked: can only fall. Pushed: can also move sideways. It never tips over.
        private const RigidbodyConstraints Locked =
            RigidbodyConstraints.FreezeRotation
            | RigidbodyConstraints.FreezePositionX
            | RigidbodyConstraints.FreezePositionZ;
        private const RigidbodyConstraints Pushed = RigidbodyConstraints.FreezeRotation;

        private Rigidbody rb;
        private Vector3 startPosition;
        private bool isPushed; // the Player touched a side in the last physics step

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
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

        // Runs after every physics step while something touches the box.
        private void OnCollisionStay(Collision collision)
        {
            if (collision.collider.TryGetComponent(out Player _))
            {
                // The contact normal points from the Player into the box.
                Vector3 push = Vector3.ProjectOnPlane(collision.GetContact(0).normal, Vector3.up);

                if (push.sqrMagnitude > 0.5f) // only from the sides of the box.
                {
                    isPushed = true;
                    rb.constraints = Pushed;
                }
            }
        }

        // Runs before the physics step. If the Player didn't touch a side in the last step, stop the box.
        private void FixedUpdate()
        {
            if (!isPushed)
            {
                Lock(); // lock/stop the box and lock it in place, so it never slides on alone.
            }

            isPushed = false; // OnCollisionStay sets it again if the Player is still pushing
        }

        // Removes the sideways speed and keeps the falling speed.
        private void Lock()
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.constraints = Locked;
        }

        // Every respawn puts it back, in case it fell or got stuck.
        private void ResetToStart()
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = startPosition;
        }
    }
}
