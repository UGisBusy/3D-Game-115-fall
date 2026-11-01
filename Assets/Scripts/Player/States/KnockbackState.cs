using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Pushed by a hit: input disabled for a short time, motor carries the impulse.
    /// Player.TakeHit calls Setup() and then raises KnockedBack.
    /// </summary>
    public class KnockbackState : PlayerState
    {
        private Vector3 knockbackVelocity;
        private float duration;
        private float timer;

        public KnockbackState(Player player) : base(player, "Knockback") { }

        public void Setup(Vector3 velocity, float duration)
        {
            knockbackVelocity = velocity;
            this.duration = duration;
        }

        protected override void OnEnter()
        {
            timer = duration;
            Motor.SetMoveInput(Vector3.zero);
            Motor.AddImpulse(knockbackVelocity);
        }

        public override void Tick(float dt)
        {
            timer -= dt;
            if (timer <= 0f)
                Events.RaiseKnockbackFinished();   // Link -> Normal
        }
    }
}
