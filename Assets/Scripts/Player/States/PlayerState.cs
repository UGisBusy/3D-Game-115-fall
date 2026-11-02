namespace PlayerSystem
{
    /// <summary>
    /// Base class for Player states, built on the shared TickState class.
    /// Tick is called every frame by Player.Update. Transitions are Links added in Player.BuildStateMachine.
    /// A state reads input and calls Motor verbs; it never touches the Rigidbody.
    /// </summary>
    public abstract class PlayerState : TickState
    {
        protected readonly Player player;
        protected PlayerMotor Motor => player.Motor;
        protected PlayerInputReader Input => player.Input;
        protected PlayerEvents Events => player.Events;

        protected PlayerState(Player player, string name)
            : base(name)
        {
            this.player = player;
            SetEnter(OnEnter);
            SetExit(OnExit);
        }

        public virtual bool AllowsAttack => false;

        protected virtual void OnEnter() { }

        protected virtual void OnExit() { }
    }
}
