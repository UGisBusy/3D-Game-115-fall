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

    [SerializeField]
    private float steering = 6f; // how fast the walk direction follows the path, higher = sharper corners

    [SerializeField]
    private float turnSmoothing = 10f; // how fast the body eases toward its facing, higher = snappier

    [Header("Navigation")]
    [SerializeField]
    private float repathInterval = 0.25f; // seconds between path updates

    [SerializeField]
    private float cornerReachDistance = 0.4f; // close enough to a path corner to go to the next one

    [SerializeField]
    private float navMeshSampleRange = 2f; // how far off the NavMesh a position may be (jumping, pushed)

    private float pauseTimer;
    private Vector3 moveDirection; // smoothed walk direction, flat, length 1 (zero before the first step)

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
            SmoothTurnTowards(FlatDirectionToTarget(), turnSmoothing, dt);
            StopMoving();
            return;
        }

        SteerTowards(ChaseDirection(dt), dt);
        SmoothTurnTowards(moveDirection, turnSmoothing, dt);
        Move(moveDirection, moveSpeed);
    }

    // Curves the walk direction toward the wanted one instead of snapping at path corners.
    private void SteerTowards(Vector3 wanted, float dt)
    {
        if (wanted.sqrMagnitude < 0.0001f)
            return;
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            moveDirection = wanted; // first step: no curve
            return;
        }

        float t = 1f - Mathf.Exp(-steering * dt);
        float maxRadians = Vector3.Angle(moveDirection, wanted) * Mathf.Deg2Rad * t;
        moveDirection = Vector3.RotateTowards(moveDirection, wanted, maxRadians, 0f);
        moveDirection.y = 0f;
        moveDirection.Normalize();
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
