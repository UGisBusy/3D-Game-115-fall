namespace PlayerSystem
{
    /// <summary>
    /// Dead: no movement. Space raises RespawnRequested; LevelManager decides where and calls Player.Spawn().
    /// </summary>
    public class DeadState : PlayerState
    {
        public DeadState(Player player)
            : base(player, "Dead") { }

        protected override void OnEnter()
        {
            Motor.Stop();
            // TODO: play death anim / hide model / disable collider if needed.
        }

        public override void Tick(float dt)
        {
            if (Input.JumpPressed)
                PlayerEvents.RaiseRespawnRequested();
        }
    }
}
