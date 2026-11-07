using PlayerSystem;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Spawns the Player in any level. Keeps one Checkpoint active and spawns the Player there, at level start and on respawn.
    /// I turn off the Player's Spawn On Start we we use the checkpoint to spawn in scenes.
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

        private void Start()
        {
            Activate(checkpoints[0]);
            SpawnPlayer();
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

        // Spawn the player at the active checkpoint.
        private void SpawnPlayer()
        {
            Transform point = activeCheckpoint.transform;
            player.Spawn(point.position, point.rotation);
        }
    }
}
