using EffectSystem;
using PlayerSystem;
using TrapSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Movable Env Obj. A small box that let the Player pushes just by walking/dashing into it.
    ///   Player pushes a side   -> the box is free to move, physics pushes it at the Player's speed
    ///   Player stops pushing   -> the box stops at once, no sliding more
    ///   A blade spins into it  -> passes through, blades only hit the Player
    ///   Stuck (e.g. in a corner) -> pushing it for StuckTime sends it back to its start
    /// Its weight is the Rigidbody's mass: a heavier box takes longer to reach the Player's speed.
    /// </summary>
    public class MovableBox : PushableBox
    {
        private const float SlideSoundSpeed = 0.2f; // slower than this, it is standing still
        private const float StuckTime = 1f; // seconds of pushing without moving before it checks if it is stuck
        private const float PlayerRoom = 1f; // free space the Player needs next to a side, to push it from there

        // The box's four sides, in its own space
        private static readonly Vector3[] Sides =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
        };

        [Tooltip("Plays while the Player pushes it and it is moving.")]
        [SerializeField]
        private LoopingSound slideSound;

        private bool isPushed; // the Player pushed a side in the last physics step
        private float pushedStillTime; // how long the Player has pushed it without it moving

        // Crates pass through blades.
        private void Start()
        {
            Collider crate = GetComponent<Collider>();

            foreach (
                SpinningBlade blade in FindObjectsByType<SpinningBlade>(FindObjectsSortMode.None)
            )
            {
                foreach (Collider part in blade.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(crate, part); // ignore the blade's colliders
                }
            }
        }

        protected override void OnPushed(Player player, Vector3 direction)
        {
            isPushed = true;
            Unlock();
        }

        protected override void Step()
        {
            if (!isPushed)
            {
                Lock();
            }

            Vector3 velocity = Body.linearVelocity;
            velocity.y = 0f; // falling is not sliding
            bool moving = velocity.magnitude > SlideSoundSpeed;
            slideSound.SetPlaying(isPushed && moving);
            CheckStuck(isPushed && !moving);

            isPushed = false; // OnCollisionStay sets it again if the Player is still pushing
        }

        // Pushed without moving for StuckTime: if it really is stuck, it goes back to its start.
        private void CheckStuck(bool pushedButStill)
        {
            if (!pushedButStill)
            {
                pushedStillTime = 0f; // reset the timer
                return;
            }

            pushedStillTime += Time.fixedDeltaTime;

            if (pushedStillTime >= StuckTime)
            {
                pushedStillTime = 0f;

                if (IsStuck())
                {
                    Break();
                }
            }
        }

        // Stuck: on every side it either can't move, or there is no room on the other side for the Player to push from.
        private bool IsStuck()
        {
            foreach (Vector3 localSide in Sides)
            {
                Vector3 side = transform.TransformDirection(localSide);
                bool canMove = FreeDistance(side, 0.5f, null) > 0.45f;
                bool roomToPush = FreeDistance(-side, PlayerRoom, null) + 0.01f >= PlayerRoom;

                if (canMove && roomToPush)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
