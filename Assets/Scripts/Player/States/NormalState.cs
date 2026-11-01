using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Default state: WASD move, jump, can start a dash, can charge the bow.
    /// </summary>
    public class NormalState : PlayerState
    {
        public NormalState(Player player) : base(player, "Normal") { }

        public override bool AllowsAttack => true;

        public override void Tick(float dt)
        {
            Motor.SetMoveInput(Input.MoveDirection);

            if (Input.JumpPressed)
                Motor.TryJump();   // Motor refuses if in the air or a modifier blocks jumping

            if (Input.DashPressed && player.Dash.CanDash)
                Events.RaiseDashRequested();   // Link -> Dashing
        }

        protected override void OnExit()
        {
            Motor.SetMoveInput(Vector3.zero);
        }
    }
}
