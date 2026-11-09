using PlayerSystem;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Respawn location in the level. Notices the Player's touch and showing whether it is active.
    /// The Player respawns at this object's position and rotation.
    /// </summary>
    [RequireComponent(typeof(BoxCollider), typeof(MeshRenderer))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField]
        private Color inactiveColor = Color.gray;

        [SerializeField]
        private Color activeColor = Color.green;

        private MeshRenderer meshRenderer;

        private bool isActive = false;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            GetComponent<BoxCollider>().isTrigger = true; // the Player walks through it
            isActive = false;
            meshRenderer.material.color = inactiveColor;
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
            meshRenderer.material.color = isActive ? activeColor : inactiveColor;
        }
    }
}
