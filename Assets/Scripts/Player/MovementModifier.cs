using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// A temporary adjustment to normal movement, added and removed by its owner (e.g. BowAbility while charging).
    /// The owner keeps the instance and may update it every frame (e.g. FacingDirection).
    /// </summary>
    public class MovementModifier
    {
        public float SpeedMultiplier = 1f;
        public bool BlockJump;

        /// <summary>If set, the Player faces this direction instead of the move direction.</summary>
        public Vector3? FacingDirection;
    }
}
