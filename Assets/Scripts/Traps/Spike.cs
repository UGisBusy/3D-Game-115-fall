using PlayerSystem;
using UnityEngine;

namespace TrapSystem
{
    /// <summary>
    /// Floor trap. Owns its active state, when that state toggles, its active or flat shape, and its own hit cooldown.
    /// Does not own invincibility: it reads Player.IsInvincible so a blocked hit doesn't start the cooldown.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
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

        [Tooltip(
            "Height of the cube while inactive. It sits on the same floor as the active cube."
        )]
        [SerializeField]
        private float inactiveHeight = 0.1f;

        [Header("Damage")]
        [SerializeField]
        private int damage = 1;

        [Tooltip("Seconds before this spike can hurt the Player again.")]
        [SerializeField]
        private float hitCooldown = 4f;

        [Tooltip(
            "Variable for OnPlayerTouch: seconds to escape after triggering it. Player gets touchGraceTime second to escape the damage"
        )]
        [SerializeField]
        private float touchGraceTime = 1f;

        private Vector3 activeScale;
        private Vector3 activePosition;
        private bool isActive;
        private float toggleTimer;
        private float nextHitTime;

        private void Awake()
        {
            activeScale = transform.localScale;
            activePosition = transform.localPosition;
            GetComponent<BoxCollider>().isTrigger = true; // the Player walks onto it, it never blocks

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

        // For onPlayerTouch spikes, after the first touch the spike stays active forever.
        private void OnTriggerEnter(Collider other)
        {
            if (toggleType == ToggleType.OnPlayerTouch
                && !isActive
                && other.TryGetComponent(out Player _)
            )
            {
                SetActive(true);
                nextHitTime = Time.time + touchGraceTime; // give the player a grace period to escape the spike before it can hurt them
            }
        }

        // For now if the spikes inactive, the cube will be flatten, and it will be normal when spikes active.
        private void SetActive(bool active)
        {
            isActive = active;

            float height = active 
            ? activeScale.y 
            : inactiveHeight;

            transform.localScale = new Vector3(activeScale.x, height, activeScale.z);
            transform.localPosition =
                activePosition + Vector3.down * ((activeScale.y - height) / 2f); // move down 
        }
    }
}
