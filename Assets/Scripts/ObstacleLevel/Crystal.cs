using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Pickup items that the Player must collect to make the Star appear.
    /// When the Player touches it, it calls the ObstacleLevelEvents.CrystalCollected and disappears.
    /// The level manager counts Crystals and decides when the Star appears.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Crystal : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true; // the Player walks through it
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Player _))
            {
                ObstacleLevelEvents.RaiseCrystalCollected();
                Destroy(gameObject); // a collected Crystal stays collected, also after a respawn
            }
        }
    }
}
