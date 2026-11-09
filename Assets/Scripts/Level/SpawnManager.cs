using PlayerSystem;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Spawns the Player in any level. Keeps one Checkpoint active and spawns the Player there, when the level manager asks and on respawn.
    /// The start Checkpoint is active until the Player touches another one.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField]
        private Player player;

        private Checkpoint activeCheckpoint;

        private void OnEnable()
        {
            PlayerEvents.RespawnRequested += SpawnPlayer;
            LevelEvents.CheckpointActivated += SetActiveCheckpoint;
        }

        private void OnDisable()
        {
            PlayerEvents.RespawnRequested -= SpawnPlayer;
            LevelEvents.CheckpointActivated -= SetActiveCheckpoint;
        }

        // Spawn the player at the active checkpoint.
        public void SpawnPlayer()
        {
            Transform point = activeCheckpoint.transform;
            player.Spawn(point.position, point.rotation);
        }

        // Swtich the active checkpoint
        public void SetActiveCheckpoint(Checkpoint checkpoint)
        {
            activeCheckpoint = checkpoint;
        }
    }
}
