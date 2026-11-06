using UnityEngine;

/// <summary>
/// Walks straight toward the Player. Base for Light, Heavy and Shield enemies.
/// TODO: use NavMesh if enemies get stuck on pillars.
/// </summary>
public class ChaseEnemy : Enemy
{
    protected override void Behave(float dt)
    {
        if (!HasTarget)
        {
            StopMoving();
            return;
        }

        Vector3 dir = FlatDirectionToTarget();
        TurnTowards(dir, dt);
        Move(dir, moveSpeed);
    }
}
