using PlayerSystem;
using UnityEngine;

namespace TrapSystem
{
    /// <summary>
    /// Floor trap. Owns its active state, when that state toggles, showing or hiding the cube, and its own hit cooldown.
    /// Does not own invincibility: it reads Player.IsInvincible so a blocked hit doesn't start the cooldown.
    /// </summary>
    [RequireComponent(typeof(BoxCollider), typeof(MeshRenderer))]
    public class Spike : MonoBehaviour
    {
        private enum ToggleType
        {
            Periodic,
            OnPlayerTouch,
            AlwaysActive,
        }

        [Header("Toggle")]
        [SerializeField]
        private ToggleType toggleType = ToggleType.Periodic;

        // Only used by Periodic
        [SerializeField]
        private float toggleInterval = 2f;

        [Header("Damage")]
        [SerializeField]
        private int damage = 1;

        [Tooltip("Seconds before this spike can hurt the Player again.")]
        [SerializeField]
        private float hitCooldown = 4f;

        private MeshRenderer meshRenderer;
        private bool isActive;
        private float toggleTimer;
        private float nextHitTime;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            GetComponent<BoxCollider>().isTrigger = true; // a solid cube would be an invisible wall while hidden

            toggleTimer = toggleInterval;
            SetActive(toggleType == ToggleType.AlwaysActive);
        }

        private void Update()
        {
            if (toggleType != ToggleType.Periodic)
            {
                return;
            }

            toggleTimer -= Time.deltaTime;

            if (toggleTimer <= 0f)
            {
                toggleTimer = toggleInterval;
                SetActive(!isActive);
            }
        }

        // When the player stays inside the spike's trigger, hurt on every hitCooldown seconds. Right now I set it 4 seconds cooldown.
        private void OnTriggerStay(Collider other)
        {
            if (!isActive || Time.time < nextHitTime)
            {
                return;
            }

            if (other.TryGetComponent(out Player player) && !player.IsInvincible)
            {
                player.TakeHit(HitInfo.Create(damage, gameObject));
                nextHitTime = Time.time + hitCooldown;
            }
        }

        // Flips on step-off so an active spike hurts before it hides, and a hidden one never hurts the Player standing on it.
        private void OnTriggerExit(Collider other)
        {
            if (toggleType == ToggleType.OnPlayerTouch && other.TryGetComponent(out IDamageable _))
            {
                SetActive(!isActive);
            }
        }

        // Only the renderer: disabling the GameObject would also stop its collider and Update.
        private void SetActive(bool active)
        {
            isActive = active;
            meshRenderer.enabled = active;
        }
    }
}
