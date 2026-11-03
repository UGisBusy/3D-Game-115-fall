using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Dash data + cooldown. DashState does the movement; this class only owns tuning and cooldown.
    /// Raises DashStarted / DashEnded on the global PlayerEvents.
    /// </summary>
    public class DashAbility : MonoBehaviour
    {
        [SerializeField]
        private float speed = 15f;

        [SerializeField]
        private float duration = 0.25f;

        [SerializeField]
        private float cooldown = 2f;

        [Tooltip(
            "Part of the dash velocity kept when the dash ends. 0 = stop dead, 1 = keep full speed."
        )]
        [SerializeField, Range(0f, 1f)]
        private float exitSpeedRatio = 0.2f;

        private Player player;

        public float Speed => speed;
        public float Duration => duration;
        public float Cooldown => cooldown;
        public float ExitSpeedRatio => exitSpeedRatio;

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
