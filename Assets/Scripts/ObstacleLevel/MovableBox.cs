using EffectSystem;
using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Movable Env Obj. A small box that let the Player pushes just by walking/dashing into it.
    ///   Player pushes a side   -> the box is free to move, physics pushes it at the Player's speed
    ///   Player stops pushing   -> the box stops at once, no sliding more
    /// Its weight is the Rigidbody's mass: a heavier box takes longer to reach the Player's speed.
    /// </summary>
    public class MovableBox : PushableBox
    {
        private const float SlideSoundSpeed = 0.2f; // slower than this, it is standing still

        [Tooltip("Plays while the Player pushes it and it is moving.")]
        [SerializeField]
        private LoopingSound slideSound;

        private bool isPushed; // the Player pushed a side in the last physics step

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
            slideSound.SetPlaying(isPushed && velocity.magnitude > SlideSoundSpeed);

            isPushed = false; // OnCollisionStay sets it again if the Player is still pushing
        }
    }
}
