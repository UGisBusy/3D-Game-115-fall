using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Dash data + cooldown. DashState does the movement; this class only owns tuning and cooldown.
    /// Raises DashStarted / DashEnded on the global PlayerEvents (Candle fuel - 1, VFX, sound).
    /// </summary>
    public class DashAbility : MonoBehaviour
    {
        [SerializeField]
        private float speed = 20f;

        [SerializeField]
        private float duration = 0.5f;

        [SerializeField]
        private float cooldown = 3f;

        private Player player;

        public float Speed => speed;
        public float Duration => duration;
        public float Cooldown => cooldown;

        public bool IsDashing { get; private set; }
        public float CooldownRemaining { get; private set; }
        public bool CanDash => !IsDashing && CooldownRemaining <= 0f;

        private void Awake()
        {
            player = GetComponent<Player>();
        }

        private void Update()
        {
            if (CooldownRemaining > 0f)
                CooldownRemaining -= Time.deltaTime;
        }

        internal void Begin(Vector3 direction)
        {
            IsDashing = true;
            CooldownRemaining = cooldown;
            PlayerEvents.RaiseDashStarted(direction);
        }

        internal void End()
        {
            IsDashing = false;
            PlayerEvents.RaiseDashEnded();
        }

        public void ResetCooldown()
        {
            IsDashing = false;
            CooldownRemaining = 0f;
        }
    }
}
