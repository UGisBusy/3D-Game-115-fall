using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Dash toward camera forward: fixed velocity, no control, no gravity, invincible.
    ///  - On the ground: horizontal or up only (the down part of the camera pitch is removed).
    ///  - In the air: full camera forward, including down.
    /// On exit keeps DashAbility.ExitSpeedRatio of the dash velocity.
    /// Raises DashFinished after DashAbility.Duration.
    /// </summary>
    public class DashState : PlayerState
    {
        private float timer;

        public DashState(Player player)
            : base(player, "Dashing") { }

        // Full 3D dash direction (length 1).
        public Vector3 Direction { get; private set; }

        // Flat dash direction, read by Dashable objects to check which side was hit.
        public Vector3 FlatDirection { get; private set; }

        protected override void OnEnter()
        {
            Direction = GetDashDirection();
            FlatDirection = Input.CameraForwardFlat;
            Vector3 flat = new(Direction.x, 0f, Direction.z);
            if (flat.sqrMagnitude > 0.0001f)
                FlatDirection = flat.normalized;

            timer = player.Dash.Duration;

            player.Dash.Begin(Direction); // starts cooldown, raises DashStarted
            Motor.SetGravityEnabled(false);
            Motor.SetVelocityOverride(Direction * player.Dash.Speed);
        }

        public override void Tick(float dt)
        {
            timer -= dt;
            if (timer <= 0f)
                Transitions.RaiseDashFinished(); // Link -> Normal
        }

        protected override void OnExit()
        {
            Motor.ClearVelocityOverride(
                Direction * (player.Dash.Speed * player.Dash.ExitSpeedRatio)
            );
            Motor.SetGravityEnabled(true);
            player.Dash.End();
        }

        private Vector3 GetDashDirection()
        {
            Vector3 dir = Input.CameraForward;
            if (Motor.IsGrounded && dir.y < 0f)
            {
                // On the ground, don't dash into the floor.
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                    return Input.CameraForwardFlat; // camera looking straight down
            }
            return dir.normalized;
        }
    }
}
