using UnityEngine;

namespace TrapSystem
{
    /// <summary>
    /// Floor trap. Owns its active state, when that state toggles, and showing or hiding the cube.
    /// Does not own invincibility: it hits every physics step while active, and Player.TakeHit ignores hits during invincibility.
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

        private MeshRenderer meshRenderer;
        private bool isActive;
        private float toggleTimer;

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

        // Stay instead of Enter: also hurts a Player who stays on the spike or is already there when it activates.
        private void OnTriggerStay(Collider other)
        {
            if (isActive && other.TryGetComponent(out IDamageable target))
            {
                target.TakeHit(HitInfo.Create(damage, gameObject));
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
