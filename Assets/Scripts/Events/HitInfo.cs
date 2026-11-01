using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Data passed by anything that hurts or knocks back the Player (enemy, spike, blade, darkness).
    /// </summary>
    public struct HitInfo
    {
        public int Damage;
        public GameObject Source;
        public Vector3 KnockbackVelocity;   // zero = no knockback
        public float KnockbackDuration;     // how long input is disabled
    }
}
