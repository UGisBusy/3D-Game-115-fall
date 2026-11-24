using EffectSystem;
using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Dashable Env Obj. A big box (boulder) that only a dash can move, a fixed distance at a time, as if it is on a grid.
    ///   Player walks into it        -> blocked
    ///   Player dashes into a side   -> it slides exactly moveDistance away from the Player
    ///   Something is closer         -> it slides until it touches it and stops there, never into it
    ///   One dash                    -> moves it once at most, even if the Player keeps dashing into it
    ///   It stops over a gap         -> it falls
    /// Stopping flush against walls and other boulders packs them tightly, so a row of them fills a gap as a bridge.
    /// The Player also can stand on it, made this as a step to reach higher places.
    /// </summary>
    public class DashableBox : PushableBox
    {
        [Tooltip(
            "How far one dash moves it. Use the boulder's own size, so boulders line up like on a grid."
        )]
        [SerializeField]
        private float moveDistance = 2f;

        [Tooltip("Plays when a dash starts moving it.")]
        [SerializeField]
        private Effect hitEffect;

        private Vector3 target;
        private float slideSpeed;
        private bool isSliding;
        private bool canBePushed; // a new dash started, so one dash moves it only once

        private void OnEnable()
        {
            PlayerEvents.DashStarted += OnDashStarted;
        }

        private void OnDisable()
        {
            PlayerEvents.DashStarted -= OnDashStarted;
        }

        private void OnDashStarted(Vector3 direction)
        {
            canBePushed = true;
        }

        protected override void OnPushed(Player player, Vector3 direction)
        {
            if (isSliding || !canBePushed || player.CurrentState != player.Dashing)
            {
                return;
            }

            Vector3 moveDirection = ToGridDirection(direction);
            float distance = FreeDistance(moveDirection, moveDistance, null);

            if (distance < 0.01f)
            {
                return; // blocked
            }

            canBePushed = false; // this dash has moved it, the next push needs a new dash
            target = Body.position + moveDirection * distance;
            slideSpeed = player.Dash.Speed; // moves as fast as the dash, so the Player doesn't ram it
            isSliding = true;
            hitEffect.Play(Body.position);

            // fixed movement
            Body.isKinematic = true;
            Unlock(); // a locked X and Z would stop the slide too
        }

        // The push turned into one of the box's own sides
        private Vector3 ToGridDirection(Vector3 direction)
        {
            Vector3 local = transform.InverseTransformDirection(direction);

            if (Mathf.Abs(local.x) > Mathf.Abs(local.z))
            {
                return transform.right * Mathf.Sign(local.x);
            }

            return transform.forward * Mathf.Sign(local.z);
        }

        protected override void Step()
        {
            if (!isSliding)
            {
                Lock();
                return;
            }

            if ((Body.position - target).sqrMagnitude < 0.0001f) // arrived in the last step
            {
                StopSliding();
                return;
            }

            Body.MovePosition(
                Vector3.MoveTowards(Body.position, target, slideSpeed * Time.fixedDeltaTime)
            );
        }

        // Physics takes over again, so it falls if it stopped over a gap.
        private void StopSliding()
        {
            isSliding = false;
            Body.isKinematic = false;
            Lock();
        }

        public override void Break()
        {
            if (isSliding)
            {
                StopSliding();
            }

            base.Break();
        }
    }
}
