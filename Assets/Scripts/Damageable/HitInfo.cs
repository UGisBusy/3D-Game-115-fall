using UnityEngine;

public struct HitInfo
{
    public int Damage;
    public GameObject Source;

    // is forced => ignore Invincibility
    public bool IsForced;

    // zero = no knockback
    public Vector3 KnockbackVelocity;

    // how long input is disabled
    public float KnockbackDuration;

    public static HitInfo Create(
        int damage = 1,
        GameObject source = null,
        bool isForced = false,
        Vector3 knockbackVelocity = default, // default = Vector3.zero
        float knockbackDuration = 0f
    )
    {
        return new HitInfo
        {
            Damage = damage,
            Source = source,
            IsForced = isForced,
            KnockbackVelocity = knockbackVelocity,
            KnockbackDuration = knockbackDuration,
        };
    }
}
