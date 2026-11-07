using PlayerSystem;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Spawns the Player in any level. Keeps one Checkpoint active and spawns the Player there, when the level manager asks and on respawn.
    /// The first Checkpoint is active until the Player touches another one.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField]
        private Player player;

        [Tooltip("Every Checkpoint in the level. The first one is where the level starts.")]
        [SerializeField]
        private Checkpoint[] checkpoints;

        private Checkpoint activeCheckpoint;

        private void OnEnable()
        {
            PlayerEvents.RespawnRequested += SpawnPlayer;
            foreach (Checkpoint checkpoint in checkpoints)
            {
                checkpoint.Touched += Activate;
            }
        }

        private void OnDisable()
        {
            PlayerEvents.RespawnRequested -= SpawnPlayer;
            foreach (Checkpoint checkpoint in checkpoints)
            {
                checkpoint.Touched -= Activate;
            }
        }

        // Spawn the player at the active checkpoint.
        public void SpawnPlayer()
        {
            // The level's first spawn comes before any Checkpoint is touched.
            if (activeCheckpoint == null)
            {
                Activate(checkpoints[0]);
            }

            Transform point = activeCheckpoint.transform;
            player.Spawn(point.position, point.rotation);
        }

        // Activate checkpoint when the player touches it.
        private void Activate(Checkpoint checkpoint)
        {
            if (activeCheckpoint != null)
            {
                activeCheckpoint.SetActive(false);
            }

            activeCheckpoint = checkpoint;
            activeCheckpoint.SetActive(true);
        }
    }
}
