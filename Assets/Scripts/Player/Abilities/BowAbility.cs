using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Bow charge/fire (combat level only; add this component only there).
    /// While charging it adds a MovementModifier: slow move, no jump, face camera forward.
    /// Works in any state that AllowsAttack, so it is not a state itself.
    /// Raises ArrowsChanged / ArrowFired on the global PlayerEvents.
    /// </summary>
    public class BowAbility : MonoBehaviour
    {
        [SerializeField]
        private int maxArrows = 5;

        [SerializeField]
        private float chargeTime = 1f; // seconds to full charge

        [SerializeField]
        private float chargeDecayTime = 0.5f; // seconds to drain from full when not held

        [SerializeField]
        private float chargeMoveMultiplier = 0.4f;

        [SerializeField]
        private Transform firePoint;

        [SerializeField]
        private GameObject arrowPrefab; // TODO: replace with Arrow component/pool

        private Player player;
        private readonly MovementModifier chargeModifier = new() { BlockJump = true };
        private bool modifierActive;

        public int Arrows { get; private set; }
        public int MaxArrows => maxArrows;
        public float Charge01 { get; private set; }
        public bool IsCharging => modifierActive;

        private void Awake()
        {
            player = GetComponent<Player>();
            chargeModifier.SpeedMultiplier = chargeMoveMultiplier;
        }

        // Called by Player every frame.
        internal void Tick(float dt, bool allowed)
        {
            var input = player.Input;
            bool wantsCharge = allowed && input.AttackHeld && Arrows > 0;

            if (wantsCharge)
            {
                SetModifier(true);
                chargeModifier.FacingDirection = input.CameraForwardFlat;
                Charge01 = Mathf.Min(1f, Charge01 + dt / chargeTime);
                return;
            }

            // Released this frame with a full charge -> fire.
            if (modifierActive && input.AttackReleased && Charge01 >= 1f && allowed)
                Fire();

            SetModifier(false);
            Charge01 = Mathf.Max(0f, Charge01 - dt / chargeDecayTime);
        }

        private void Fire()
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();

            Vector3 pos = firePoint != null ? firePoint.position : transform.position;
            if (arrowPrefab != null)
                Instantiate(arrowPrefab, pos, Quaternion.LookRotation(dir));

            Charge01 = 0f;
            Arrows--;
            PlayerEvents.RaiseArrowsChanged(Arrows);
            PlayerEvents.RaiseArrowFired(pos, dir);
        }

        public void AddArrows(int amount)
        {
            Arrows = Mathf.Min(maxArrows, Arrows + amount);
            PlayerEvents.RaiseArrowsChanged(Arrows);
        }

        public void ResetBow()
        {
            SetModifier(false);
            Charge01 = 0f;
            // Arrow count on respawn is a design decision; keep current value for now.
        }

        private void SetModifier(bool active)
        {
            if (active == modifierActive)
                return;
            modifierActive = active;
            if (active)
                player.Motor.AddModifier(chargeModifier);
            else
                player.Motor.RemoveModifier(chargeModifier);
        }
    }
}
