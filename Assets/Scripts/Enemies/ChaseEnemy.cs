using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Chases the Player. Base for Light, Heavy and Shield enemies.
///  - Pathing: every repathInterval it asks the baked NavMesh for a path to the Player and walks
///    toward the next corner, so it goes around pillars instead of getting stuck on them.
///    It still moves with its Rigidbody (knockback, pushes, slam all keep working); the NavMesh only
///    gives the direction. No NavMesh baked, or no path found: walks straight at the Player.
///  - After hurting the Player it stands still for hitPauseTime (still turning to face the Player),
///    so it doesn't follow the knockback, overlap the Player and hit again when invincibility ends.
/// Scene setup: a NavMeshSurface (AI Navigation package) baked from the arena's colliders,
/// with the Player and Enemy layers excluded so they don't cut holes in it.
/// </summary>
public class ChaseEnemy : Enemy
{
    [Header("Chase")]
    [SerializeField]
    protected float hitPauseTime = 1f; // seconds to stand still after hitting the Player

    [Header("Navigation")]
    [SerializeField]
    private float repathInterval = 0.25f; // seconds between path updates

    [SerializeField]
    private float cornerReachDistance = 0.4f; // close enough to a path corner to go to the next one

    [SerializeField]
    private float navMeshSampleRange = 2f; // how far off the NavMesh a position may be (jumping, pushed)

    private float pauseTimer;

    private NavMeshPath path;
    private readonly Vector3[] corners = new Vector3[32];
    private int cornerCount;
    private int cornerIndex;
    private float repathTimer;

    protected bool IsPaused => pauseTimer > 0f;

    protected override void Awake()
    {
        base.Awake();
        path = new NavMeshPath();
        repathTimer = Random.Range(0f, repathInterval); // spread path queries over frames
    }

    protected override void Behave(float dt)
    {
        if (!HasTarget)
        {
            StopMoving();
            return;
        }

        if (pauseTimer > 0f)
        {
            pauseTimer -= dt;
            TurnTowards(FlatDirectionToTarget(), dt);
            StopMoving();
            return;
        }

        Vector3 dir = ChaseDirection(dt);
        TurnTowards(dir, dt);
        Move(dir, moveSpeed);
    }

    protected override void OnHitPlayer() => pauseTimer = hitPauseTime;

    // ---------------- Navigation ----------------

    // Flat direction toward the next path corner, or straight at the Player if there is no path.
    private Vector3 ChaseDirection(float dt)
    {
        repathTimer -= dt;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            Repath();
        }

        // corners[0] is the start point; skip corners that are already reached.
        while (cornerIndex < cornerCount)
        {
            Vector3 to = corners[cornerIndex] - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > cornerReachDistance * cornerReachDistance)
                return to.normalized;
            cornerIndex++;
        }

        return FlatDirectionToTarget(); // last corner reached, or no path
    }

    private void Repath()
    {
        cornerCount = 0;
        cornerIndex = 0;

        if (
            !NavMesh.SamplePosition(transform.position, out NavMeshHit from, navMeshSampleRange, NavMesh.AllAreas)
            || !NavMesh.SamplePosition(Target.transform.position, out NavMeshHit to, navMeshSampleRange, NavMesh.AllAreas)
        )
            return;

        if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path))
            return;
        if (path.status == NavMeshPathStatus.PathInvalid)
            return;

        cornerCount = path.GetCornersNonAlloc(corners);
        cornerIndex = 1;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (cornerCount < 2)
            return;
        Gizmos.color = Color.cyan;
        for (int i = 1; i < cornerCount; i++)
            Gizmos.DrawLine(corners[i - 1], corners[i]);
    }
}
