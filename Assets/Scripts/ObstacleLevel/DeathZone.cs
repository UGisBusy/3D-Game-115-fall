using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// The death zone area under the level.
    ///   Player falls into it -> the Player dies
    /// Boxes never reach it: a falling box breaks in the air first.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DeathZone : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true; // things fall into it
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
