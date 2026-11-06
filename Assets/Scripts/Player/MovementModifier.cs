using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// A temporary adjustment to normal movement, added and removed by its owner.
    /// The owner keeps the instance and may update it every frame.
    /// </summary>
    public class MovementModifier
    {
        public float SpeedMultiplier = 1f;
        public bool BlockJump;

        // If set, the Player faces this direction instead of the move direction
        public Vector3? FacingDirection;
    }
}
