using System;
using PlayerSystem;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// The bounce a dashing Player gets from hitting a Boulder or a Breakable Wall, set in the Inspector.
    /// It ends the dash and pushes the Player back the way they came.
    /// </summary>
    [Serializable]
    public class DashBounce
    {
        [SerializeField]
        private float backSpeed = 8f; // against the dash, fades out (about backSpeed / 8 meters)

        [SerializeField]
        private float upSpeed = 4f;

        [Tooltip("How long the Player has no control.")]
        [SerializeField]
        private float duration = 0.3f;

        // Call while the Player is still dashing: it reads the dash direction.
        public void Apply(Player player)
        {
            Vector3 back = -player.Dashing.FlatDirection;
            player.Bounce(back * backSpeed + Vector3.up * upSpeed, duration);
        }
    }
}
