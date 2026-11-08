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

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            GetComponent<BoxCollider>().isTrigger = true; // the Player walks through it
            SetActive(false);
        }

        // When the player touches the checkpoint.
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Player _))
            {
                LevelEvents.RaiseCheckpointTouched(this);
            }
        }

        // When it's set active, show the color differently.
        public void SetActive(bool active)
        {
            meshRenderer.material.color = active ? activeColor : inactiveColor;
        }
    }
}
