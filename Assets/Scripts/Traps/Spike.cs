using PlayerSystem;
using UnityEngine;

namespace TrapSystem
{
    /// <summary>
    /// Code for floor trap made of a group of spikes. Owns its active state, when that state toggles, showing or hiding its cones, and its own hit cooldown.
    /// One trigger on this object's collider covers every spike, so the whole group hurts the Player as one trap.
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

        [Tooltip("Parent of every cone or spikes.")]
        [SerializeField]
        private GameObject cones;

        [Header("Spikes Damage")]
        [SerializeField]
        private int damage = 1;

        [Tooltip("Seconds before this spike can hurt the Player again.")]
        [SerializeField]
        private float hitCooldown = 4f;

        // Only used by OnPlayerTouch
        [Tooltip(
            "Variable for OnPlayerTouch: seconds to escape after triggering it. Player gets touchGraceTime second to escape the damage"
        )]
        [SerializeField]
        private float touchGraceTime = 0.5f; // can be changed anytime

        private bool isActive;
        private float toggleTimer;
        private float nextHitTime;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true; // when the Player walks into the collider, they never block

            toggleTimer = toggleInterval;
            SetActive(toggleType == ToggleType.AlwaysActive);
        }

        private void OnEnable()
        {
            PlayerEvents.Spawned += OnPlayerSpawned;
        }

        private void OnDisable()
        {
            PlayerEvents.Spawned -= OnPlayerSpawned;
        }

        // After a respawn, OnPlayerTouch spikes hide again, so the Player has to find them again.
        private void OnPlayerSpawned()
        {
            if (toggleType == ToggleType.OnPlayerTouch)
            {
                SetActive(false);
            }
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
                toggleTimer = toggleInterval; // interval for periodic spike only
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

        // For OnPlayerTouch spikes, after the first touch the spike stays active until the Player respawns.
        private void OnTriggerEnter(Collider other)
        {
            if (
                toggleType == ToggleType.OnPlayerTouch
                && !isActive
                && other.TryGetComponent(out Player _)
            )
            {
                SetActive(true);
                nextHitTime = Time.time + touchGraceTime; // give the player a grace period to escape the spike before it can hurt them
            }
        }

        // Inactive spikes hide their cones, so the Player can see the trap is safe.
        private void SetActive(bool active)
        {
            isActive = active;
            cones.SetActive(active);
        }
    }
}
