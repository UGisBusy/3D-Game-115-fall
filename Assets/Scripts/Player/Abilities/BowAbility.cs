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
        private int startArrows = 5; // given on every Spawn (respawn refills arrows)

        [SerializeField]
        private float chargeTime = 1f; // seconds to full charge

        [SerializeField]
        private float chargeDecayTime = 0.5f; // seconds to drain from full when not held

        [SerializeField]
        private float chargeMoveMultiplier = 0.4f;

        [SerializeField]
        private Transform firePoint;

        [SerializeField]
        [Tooltip("Projectile to shoot. Any prefab with an Arrow component: Prefabs/Combat/Arrow or Prefabs/Combat/Fireball.")]
        private Arrow arrowPrefab;

        [Header("Debug")]
        [SerializeField]
        private float debugChargeProgress;

        [SerializeField]
        private int debugArrows;

        private Player player;
        private readonly MovementModifier chargeModifier = new() { BlockJump = true };
        private bool modifierActive;
        private MagicCircle chargeCircle; // from arrowPrefab.ChargeCircle, null if it has none

        public int Arrows { get; private set; }
        public int MaxArrows => maxArrows;
        public float ChargeProgress { get; private set; } // 0 = empty, 1 = fully charged
        public bool IsCharging => modifierActive;

        private void Awake()
        {
            player = GetComponent<Player>();
            chargeModifier.SpeedMultiplier = chargeMoveMultiplier;
        }

        private void OnEnable() => LevelEvents.Collected += OnCollected;

        private void OnDisable()
        {
            LevelEvents.Collected -= OnCollected;
            ClearChargeCircle();
        }

        private void OnCollected(Collectible collectible)
        {
            if (collectible.Type == CollectibleType.Arrow)
                AddArrows(collectible.Amount);
        }

        // Inspector readout only.
        private void LateUpdate()
        {
            debugChargeProgress = ChargeProgress;
            debugArrows = Arrows;
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
                ChargeProgress = Mathf.Min(1f, ChargeProgress + dt / chargeTime);
                UpdateChargeCircle(true);
                return;
            }

            // Released this frame with a full charge -> fire.
            if (modifierActive && input.AttackReleased && ChargeProgress >= 1f && allowed)
                Fire();

            SetModifier(false);
            ChargeProgress = Mathf.Max(0f, ChargeProgress - dt / chargeDecayTime);
            UpdateChargeCircle(false);
        }

        // Spawn the projectile's charge circle when charging starts, keep it in sync,
        // and remove it once the charge has drained back to 0 (it un-draws on the way).
        private void UpdateChargeCircle(bool charging)
        {
            if (chargeCircle == null)
            {
                if (!charging || arrowPrefab == null || arrowPrefab.ChargeCircle == null)
                    return;
                chargeCircle = Instantiate(arrowPrefab.ChargeCircle);
                chargeCircle.Begin(transform);
            }

            chargeCircle.SetProgress(ChargeProgress);
            if (!charging && ChargeProgress <= 0f)
                ClearChargeCircle();
        }

        private void ClearChargeCircle()
        {
            if (chargeCircle != null)
                Destroy(chargeCircle.gameObject);
            chargeCircle = null;
        }

        private void Fire()
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();

            Vector3 pos = firePoint != null ? firePoint.position : transform.position;
            if (arrowPrefab != null)
            {
                Arrow arrow = Instantiate(arrowPrefab, pos, Quaternion.LookRotation(dir));
                arrow.Launch(dir, gameObject);
            }

            // The circle flares and fades by itself.
            if (chargeCircle != null)
                chargeCircle.Release();
            chargeCircle = null;

            ChargeProgress = 0f;
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
            ChargeProgress = 0f;
            ClearChargeCircle();
            Arrows = Mathf.Min(startArrows, maxArrows);
            PlayerEvents.RaiseArrowsChanged(Arrows);
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
