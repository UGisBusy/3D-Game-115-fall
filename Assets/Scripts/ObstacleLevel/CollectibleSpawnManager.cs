using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Spawns the obstacle level's Collectibles, one at each child spot.
    /// Every time the Player spawns, it despawns the Collectibles still in the level.
    /// Collectibles in this level: heart
    /// </summary>
    public class CollectibleSpawnManager : MonoBehaviour
    {
        [SerializeField]
        private Collectible collectiblePrefab;

        private readonly List<Collectible> spawned = new();

        private void OnEnable()
        {
            PlayerEvents.Spawned += SpawnAll;
        }

        private void OnDisable()
        {
            PlayerEvents.Spawned -= SpawnAll;
        }

        private void SpawnAll()
        {
            DespawnAll();

            // Every child of this object is a spot for spawn.
            foreach (Transform spot in transform)
            {
                spawned.Add(Instantiate(collectiblePrefab, spot.position, spot.rotation));
            }
        }

        private void DespawnAll()
        {
            foreach (Collectible collectible in spawned)
            {
                // A picked-up Collectible already destroyed itself.
                if (collectible != null)
                {
                    Destroy(collectible.gameObject);
                }
            }

            spawned.Clear();
        }
    }
}
