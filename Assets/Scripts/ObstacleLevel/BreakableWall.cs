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
        // Using OnCollisionStay in case the Player already touching it when the dash starts.
        private void OnCollisionStay(Collision collision)
        {
            if (
                collision.collider.TryGetComponent(out Player player)
                && player.CurrentState == player.Dashing
            )
            {
                Destroy(gameObject);
            }
        }
    }
}
