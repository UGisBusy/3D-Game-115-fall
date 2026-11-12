using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Base of every Dashable Env Obj (Breakable Wall, Dashable Box).
    /// Player walks into it: blocked, because it has a solid Collider.
    /// Player dashes into it: OnDashed runs, and each type decides its own effect.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class Dashable : MonoBehaviour
    {
        // Using OnCollisionStay because the Player can already be touching it when the dash starts.
        private void OnCollisionStay(Collision collision)
        {
            if (
                collision.collider.TryGetComponent(out Player player)
                && player.CurrentState == player.Dashing
            )
            {
                OnDashed(collision);
            }
        }

        // The effect of this type. The collision tells which side the Player hit.
        protected abstract void OnDashed(Collision collision);
    }
}
