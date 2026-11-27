using EffectSystem;
using ObstacleLevel;
using PlayerSystem;
using UnityEngine;

namespace TrapSystem
{
    /// <summary>
    /// Code for floor trap made of a group of spikes. Owns its active state, when that state toggles, and showing or hiding its cones.
    /// One trigger on this object's collider covers every spike, so the whole group hurts the Player as one trap.
    /// Active spikes hit the Player every physics step. The Player's invincibility after a hit decides how often it really hurts.
    /// OnPlayerTouch (hidden spikes):
    ///   Player or a Boulder steps on it -> a warning sound, the spikes are still hidden
    ///   touchGraceTime later            -> the spikes come out and hurt at once if the Player is still on it (they never hurt a Boulder)
    ///   Player respawns                 -> hidden again
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

        // Only used by OnPlayerTouch
        [Tooltip(
            "Variable for OnPlayerTouch: seconds between the warning and the spikes coming out. Player gets touchGraceTime second to escape the damage"
        )]
        [SerializeField]
        private float touchGraceTime = 0.5f; // can be changed anytime

        [Tooltip(
            "Variable for OnPlayerTouch: how far below their full height the cones wait during the warning, so only their tips show."
        )]
        [SerializeField]
        private float peekDepth = 0.9f;

        [Tooltip("Variable for OnPlayerTouch: plays when the Player steps on the hidden spikes.")]
        [SerializeField]
        private Effect warnEffect;

        [Tooltip("Variable for OnPlayerTouch: plays when the hidden spikes pop up.")]
        [SerializeField]
        private Effect popEffect;

        private bool isActive;
        private float toggleTimer;
        private float warnTimer; // OnPlayerTouch: above 0 while the warning plays, the spikes come out at 0
        private Vector3 conesFullPosition; // where the cones stand when fully out

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true; // when the Player walks into the collider, they never block
            conesFullPosition = cones.transform.localPosition;

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
                warnTimer = 0f;
                warnEffect.Stop(); // stop the warning sound
                SetActive(false);
            }
        }

        private void Update()
        {
            if (toggleType == ToggleType.Periodic)
            {
                TickPeriodic();
            }
            else if (toggleType == ToggleType.OnPlayerTouch && warnTimer > 0f)
            {
                TickWarning();
            }
        }

        private void TickPeriodic()
        {
            toggleTimer -= Time.deltaTime;

            if (toggleTimer <= 0f)
            {
                toggleTimer = toggleInterval; // interval for periodic spike only
                SetActive(!isActive);
            }
        }

        // The warning is over: the spikes come out. OnTriggerStay hurts the Player in the next physics step if still on it.
        // The warning sound can be longer than the grace time, so it is cut here.
        private void TickWarning()
        {
            warnTimer -= Time.deltaTime;

            if (warnTimer <= 0f)
            {
                warnEffect.Stop();
                SetActive(true);
                popEffect.Play(transform.position);
            }
        }

        // Hits every physics step while the Player is inside.
        // Player.TakeHit ignores hits while the Player is invincible, so the Player decides how often it really hurts.
        private void OnTriggerStay(Collider other)
        {
            if (isActive && other.TryGetComponent(out Player player))
            {
                player.TakeHit(HitInfo.Create(damage, gameObject));
            }
        }

        // For OnPlayerTouch spikes, the first touch of the Player or a Boulder starts the warning.
        // After it, the spike stays active until the Player respawns.
        private void OnTriggerEnter(Collider other)
        {
            if (
                toggleType == ToggleType.OnPlayerTouch
                && !isActive
                && warnTimer <= 0f
                && (other.TryGetComponent(out Player _) || other.TryGetComponent(out DashableBox _))
            )
            {
                warnTimer = touchGraceTime;
                warnEffect.Play(transform.position);
                PeekCones();
            }
        }

        // Inactive spikes hide their cones, so the Player can see the trap is safe. Active ones stand fully out.
        private void SetActive(bool active)
        {
            isActive = active;
            cones.SetActive(active);
            cones.transform.localPosition = conesFullPosition;
        }

        // During the warning only the tips of the spikes show.
        private void PeekCones()
        {
            cones.SetActive(true);
            cones.transform.localPosition = conesFullPosition + Vector3.down * peekDepth;
        }
    }
}
