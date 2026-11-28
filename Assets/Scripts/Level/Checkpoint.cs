using PlayerSystem;
using UISystem;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Respawn location in the level, shown by a flag. Notices the Player's touch and shows whether it is active:
    ///   Inactive -> the flag's cloth (and its minimap icon) is inactiveColor
    ///   Active   -> both turn activeColor
    /// The Player respawns at this object's position and rotation.
    /// </summary>
    [RequireComponent(typeof(BoxCollider), typeof(MinimapIcon))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField]
        private Color inactiveColor = new(0.85f, 0.15f, 0.15f);

        [SerializeField]
        private Color activeColor = Color.green;

        [Tooltip("The flag's cloth. Only it changes color, not the pole.")]
        [SerializeField]
        private Renderer cloth;

        private MinimapIcon mapIcon;

        private bool isActive = false;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true; // the Player walks through it
            mapIcon = GetComponent<MinimapIcon>();
            isActive = false;
            ShowState();
        }

        private void OnEnable() => LevelEvents.CheckpointActivated += Activate;

        private void OnDisable() => LevelEvents.CheckpointActivated -= Activate;

        // When the player touches inactive checkpoint.
        private void OnTriggerEnter(Collider other)
        {
            if (isActive || !other.TryGetComponent(out Player _))
                return;

            LevelEvents.RaiseCheckpointActivated(this);
        }

        // Update active state
        private void Activate(Checkpoint cp)
        {
            isActive = cp == this;
            ShowState();
        }

        private void ShowState()
        {
            Color color;

            if (isActive)
            {
                color = activeColor;
            }
            else
            {
                color = inactiveColor;
            }

            cloth.material.color = color;
            mapIcon.Color = color;
        }
    }
}
