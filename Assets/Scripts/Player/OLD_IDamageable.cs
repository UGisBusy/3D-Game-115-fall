using UnityEngine;

/// <summary>
/// The script for damageable opponent or player.
/// </summary>
public interface IDamageable
{
    public void TakeDamage(float amount, GameObject source);
}
