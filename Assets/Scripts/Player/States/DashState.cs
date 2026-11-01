using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Dash toward camera forward: fixed velocity, no control, no gravity, invincible (see Player.IsInvincible).
    /// Raises DashFinished after DashAbility.Duration.
    /// </summary>
    public class DashState : PlayerState
    {
        private float timer;

        public DashState(Player player) : base(player, "Dashing") { }

        /// <summary>Flat dash direction, read by Dashable objects to check which side was hit.</summary>
        public Vector3 Direction { get; private set; }

        protected override void OnEnter()
        {
            Direction = Input.CameraForwardFlat;
            timer = player.Dash.Duration;

            player.Dash.Begin(Direction);   // starts cooldown, raises DashStarted (candle, VFX)
            Motor.SetGravityEnabled(false);
            Motor.SetVelocityOverride(Direction * player.Dash.Speed);
        }

        public override void Tick(float dt)
        {
            timer -= dt;
            if (timer <= 0f)
                Events.RaiseDashFinished();   // Link -> Normal
        }

        protected override void OnExit()
        {
            Motor.ClearVelocityOverride();
            Motor.SetGravityEnabled(true);
            player.Dash.End();
        }
    }
}
