namespace PlayerSystem
{
    /// <summary>
    /// Base class for Player states, built on the shared State class.
    /// Adds per-frame Tick (called by Player.Update). Transitions are Links added in Player.BuildStateMachine.
    /// A state reads input and calls Motor verbs; it never touches the Rigidbody.
    /// </summary>
    public abstract class PlayerState : State
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

        /// <summary>Can the bow be charged in this state?</summary>
        public virtual bool AllowsAttack => false;

        protected virtual void OnEnter() { }

        protected virtual void OnExit() { }

        public virtual void Tick(float dt) { }
    }
}
