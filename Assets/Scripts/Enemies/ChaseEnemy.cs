using UnityEngine;

/// <summary>
/// Walks straight toward the Player. Base for Light, Heavy and Shield enemies.
/// After hurting the Player it stands still for hitPauseTime (still turning to face the Player),
/// so it doesn't follow the knockback, overlap the Player and hit again when invincibility ends.
/// TODO: use NavMesh if enemies get stuck on pillars.
/// </summary>
public class ChaseEnemy : Enemy
{
    [Header("Chase")]
    [SerializeField]
    protected float hitPauseTime = 1f; // seconds to stand still after hitting the Player

    private float pauseTimer;

    protected bool IsPaused => pauseTimer > 0f;

    protected override void Behave(float dt)
    {
        if (!HasTarget)
        {
            StopMoving();
            return;
        }

        Vector3 dir = FlatDirectionToTarget();
        TurnTowards(dir, dt);

        if (pauseTimer > 0f)
        {
            pauseTimer -= dt;
            StopMoving();
            return;
        }

        Move(dir, moveSpeed);
    }

    protected override void OnHitPlayer() => pauseTimer = hitPauseTime;
}
