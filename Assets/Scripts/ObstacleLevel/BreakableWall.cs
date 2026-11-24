using EffectSystem;
using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Dashable Env Obj. A wall that blocks the path, but can be destroyed by the Player's dash.
    ///   Player walks into it   -> blocked, because it has a solid Collider
    ///   Player dashes into it  -> the wall is destroyed
    ///   Player respawns        -> the wall stays destroyed
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BreakableWall : MonoBehaviour
    {
        [SerializeField]
        private Effect breakEffect;

        private bool isBroken; // Destroy waits for the end of the frame, and physics can run again before it

        // Using OnCollisionStay in case the Player already touching it when the dash starts.
        private void OnCollisionStay(Collision collision)
        {
            if (isBroken)
            {
                return;
            }

            if (
                collision.collider.TryGetComponent(out Player player)
                && player.CurrentState == player.Dashing
            )
            {
                isBroken = true;
                breakEffect.Play(transform.position);
                Destroy(gameObject);
            }
        }
    }
}
