using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Dashable Env Obj. A big box (boulder) that only a dash can move.
    ///   Player walks into it   -> blocked
    ///   Player dashes into it  -> the box moves with the Player for the dash duration
    ///   The dash ends          -> the box glides on for glideTime, then slowing down to a stop
    /// It follows the Player's dash speed and only slows down after that, so it is never overpushed.
    /// The Player also can stand on it, made this as a step to reach higher places or as a bridge to cross gaps.
    /// </summary>
    public class DashableBox : PushableBox
    {
        [Tooltip("How long it keeps sliding after the dash ends. Longer means further.")]
        [SerializeField]
        private float glideTime = 0.3f;

        protected override float GlideTime => glideTime;

        protected override bool CanPush(Player player)
        {
            return player.CurrentState == player.Dashing;
        }
    }
}
