using PlayerSystem;
using UnityEngine;

namespace EffectSystem
{
    /// <summary>
    /// Plays effects for the shared level events, so the Player and Collectible code stay as they are.
    ///   Player is hurt / dies / spawns / dashes -> an effect on the Player
    ///   A Collectible is picked up     -> its type's effect where it was
    ///   A Checkpoint is activated      -> an effect on the Checkpoint
    /// </summary>
    public class LevelEffects : MonoBehaviour
    {
        [SerializeField]
        private Player player;

        [Header("Player")]
        [SerializeField]
        private Effect hurt;

        [SerializeField]
        private Effect died;

        [SerializeField]
        private Effect spawned;

        [SerializeField]
        private Effect dashed;

        [Header("Collectibles")]
        [SerializeField]
        private Effect heart;

        [SerializeField]
        private Effect crystal;

        [SerializeField]
        private Effect star;

        [Header("Level")]
        [SerializeField]
        private Effect checkpoint;

        private void OnEnable()
        {
            PlayerEvents.Hurt += OnHurt;
            PlayerEvents.Died += OnDied;
            PlayerEvents.Spawned += OnSpawned;
            PlayerEvents.DashStarted += OnDashStarted;
            LevelEvents.Collected += OnCollected;
            LevelEvents.CheckpointActivated += OnCheckpointActivated;
        }

        private void OnDisable()
        {
            PlayerEvents.Hurt -= OnHurt;
            PlayerEvents.Died -= OnDied;
            PlayerEvents.Spawned -= OnSpawned;
            PlayerEvents.DashStarted -= OnDashStarted;
            LevelEvents.Collected -= OnCollected;
            LevelEvents.CheckpointActivated -= OnCheckpointActivated;
        }

        private void OnHurt(HitInfo hit)
        {
            hurt.Play(PlayerCenter());
        }

        private void OnDied()
        {
            died.Play(PlayerCenter());
        }

        private void OnSpawned()
        {
            spawned.Play(PlayerCenter());
        }

        // The burst of air points backward, so the wind is left behind the Player.
        private void OnDashStarted(Vector3 direction)
        {
            dashed.Play(PlayerCenter(), Quaternion.LookRotation(-direction));
        }

        private void OnCollected(Collectible collectible)
        {
            Vector3 position = collectible.transform.position;
            switch (collectible.Type)
            {
                case CollectibleType.Heart:
                    heart.Play(position);
                    break;
                case CollectibleType.Crystal:
                    crystal.Play(position);
                    break;
                case CollectibleType.Star:
                    star.Play(position);
                    break;
            }
        }

        private void OnCheckpointActivated(LevelSystem.Checkpoint activated)
        {
            checkpoint.Play(activated.transform.position);
        }

        // The middle of the Player's body, wherever its pivot is.
        private Vector3 PlayerCenter()
        {
            return player.GetComponent<Collider>().bounds.center;
        }
    }
}
