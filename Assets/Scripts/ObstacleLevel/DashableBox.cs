using EffectSystem;
using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Dashable Env Obj. A big box (boulder) that only a dash can move, on a grid counted from its start.
    ///   Player walks into it        -> blocked
    ///   Player dashes into a side   -> it slides up to moveDistance away to a grid point, and the Player bounces back
    ///   Something is closer         -> it slides flush against it, never into it
    ///   It can't move that way      -> it slides back toward the Player instead, this is to prevent it from getting stuck in a corner
    ///   One dash                    -> moves it once at most, even if the Player keeps dashing into it
    ///   It stops over a gap         -> it falls
    /// Stopping on grid points keeps every boulder lined up with the level's grid, so it drops into a 1-cell pit.
    /// The Player also can stand on it, made this as a step to reach higher places.
    /// </summary>
    public class DashableBox : PushableBox
    {
        [Tooltip("How far one dash moves it, a whole number of grid cells.")]
        [SerializeField]
        private float moveDistance = 2f;

        [Tooltip(
            "The level's grid cell: pits and boulder spots are this wide. A boulder stops on grid points counted from its start."
        )]
        [SerializeField]
        private float gridSize = 2f;

        [Tooltip("The effect that plays when a dash starts moving it.")]
        [SerializeField]
        private Effect hitEffect;

        [SerializeField]
        private DashBounce bounce;

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

            bounce.Apply(player); // also when it is blocked, the dash ends here

            Vector3 moveDirection = ToGridDirection(direction);
            float distance = SlideDistance(moveDirection);

            if (distance < 0.01f) // stuck against something, so it comes back toward the Player instead
            {
                moveDirection = -moveDirection; // opposite direction, toward the Player
                distance = SlideDistance(moveDirection);
            }

            if (distance < 0.01f)
            {
                return; // blocked both ways
            }

            canBePushed = false; // this dash has moved it, the next push needs a new dash
            target = Body.position + moveDirection * distance;
            slideSpeed = player.Dash.Speed; // moves as fast as the dash
            isSliding = true;
            hitEffect.Play(Body.position, Quaternion.LookRotation(moveDirection)); // faces the way it slides

            // fixed movement
            Body.isKinematic = true;
            Unlock(); // a locked X and Z would stop the slide too
        }

        // To the furthest grid point within moveDistance.
        private float SlideDistance(Vector3 moveDirection)
        {
            float fromStart = Vector3.Dot(Body.position - StartPosition, moveDirection); // distance from the start and move direction
            float toGridPoint =
                Mathf.Floor((fromStart + moveDistance + 0.01f) / gridSize) * gridSize - fromStart; // the next grid point in the move direction
            return FreeDistance(moveDirection, toGridPoint, null); // the distance to the next grid point
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
