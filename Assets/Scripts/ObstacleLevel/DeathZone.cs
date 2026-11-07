using PlayerSystem;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// The death zone area that kills the Player when the player stands on it.
    /// /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DeathZone : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true; // the Player falls into it
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Player player))
            {
                player.Kill();
            }
        }
    }
}
