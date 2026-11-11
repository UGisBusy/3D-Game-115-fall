using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;

/// <summary>
/// Heavy Enemy: higher life, moves slow. Chases the Player.
///  - Ground slam: when the Player is within slamTriggerRange, it stops and winds up (slamWindupTime),
///    then hurts and knocks back the Player if within slamRadius.
///    The slam only hits along the ground: a flat disk slamRadius wide and slamHeight tall, so a
///    Player (or enemy) in the air above it is safe. Then it recovers, and can't slam
///    again for slamCooldown. The Player can back off during the windup, or dash through it (invincible).
///  - The slam also pushes other enemies in range (on this enemy's layer) away, without hurting them.
///  - Gets faster as it loses HP: moveSpeed + speedPerHpLost for each HP lost.
/// Telegraph (optional): a flat child object shown only during the windup. Its X/Z scale is set to
/// slamRadius * 2 on Awake, so use a mesh 1 unit wide (default Cylinder or Quad) under the unscaled root.
/// While shown it is kept on the ground (its local Y at Awake = height above the ground), even if the
/// enemy is knocked into the air.
/// </summary>
public class HeavyEnemy : ChaseEnemy
{
    private enum Phase
    {
        Chase,
        Windup,
        Recover,
    }

    [Header("Slam")]
    [SerializeField]
    private float slamTriggerRange = 3f; // start the windup when the Player is this close

    [SerializeField]
    private float slamRadius = 6f; // hit range of the slam, bigger than the trigger range

    [SerializeField]
    private float slamHeight = 0.3f; // the slam disk's thickness above the ground; jump over it

    [SerializeField]
    private float slamWindupTime = 0.8f;

    [SerializeField]
    private float slamRecoverTime = 0.6f; // stands still after the slam

    [SerializeField]
    private float slamCooldown = 2f; // from slam to the next possible windup

    [SerializeField]
    private int slamDamage = 1;

    [SerializeField]
    private float slamKnockbackSpeed = 18f;

    [SerializeField]
    private float slamKnockbackUp = 6f;

    [SerializeField]
    private float enemyPushSpeed = 12f; // push on other enemies, no damage

    [SerializeField]
    private float enemyPushUp = 4f;

    [SerializeField]
    private float enemyPushDuration = 0.5f; // other enemies can't move for this long

    [SerializeField]
    private GameObject slamTelegraph; // optional, shown during the windup

    [Header("Enrage")]
    [SerializeField]
    private float speedPerHpLost = 1f; // 3 HP: 1.5 -> 2.5 -> 3.5 m/s

    private static readonly Collider[] slamBuffer = new Collider[32];
    private static readonly List<Enemy> pushedBuffer = new();

    private Phase phase;
    private float timer;
    private float telegraphLift; // telegraph height above the ground
    private float cooldownLeft;
    private float baseMoveSpeed;

    // Default values when the component is added (edit them in the Inspector after).
    private void Reset()
    {
        maxHp = 3;
        moveSpeed = 1.5f;
    }

    protected override void Awake()
    {
        base.Awake();
        baseMoveSpeed = moveSpeed;

        if (slamTelegraph != null)
        {
            telegraphLift = slamTelegraph.transform.localPosition.y;
            Vector3 s = slamTelegraph.transform.localScale;
            slamTelegraph.transform.localScale = new Vector3(slamRadius * 2f, s.y, slamRadius * 2f);
        }
        SetTelegraph(false);
    }

    protected override void Behave(float dt)
    {
        if (cooldownLeft > 0f)
            cooldownLeft -= dt;

        switch (phase)
        {
            case Phase.Chase:
                if (CanStartSlam())
                {
                    phase = Phase.Windup;
                    timer = slamWindupTime;
                    StopMoving();
                    SetTelegraph(true);
                    SetHeldAnim(EnemyVisual.Held.Windup, slamWindupTime); // taunt, stretched until the slam
                    return;
                }
                base.Behave(dt);
                break;

            case Phase.Windup:
                StopMoving();
                timer -= dt;
                if (timer <= 0f)
                {
                    Slam();
                    SetTelegraph(false);
                    SetHeldAnim(EnemyVisual.Held.None);
                    if (Visual != null)
                        Visual.PlaySpecial(); // the slam itself, plays once
                    phase = Phase.Recover;
                    timer = slamRecoverTime;
                    cooldownLeft = slamCooldown;
                }
                break;

            case Phase.Recover:
                StopMoving();
                timer -= dt;
                if (timer <= 0f)
                    phase = Phase.Chase;
                break;
        }
    }

    private bool CanStartSlam()
    {
        if (!HasTarget || IsPaused || cooldownLeft > 0f)
            return false;

        Vector3 d = Target.transform.position - transform.position;
        d.y = 0f;
        return d.sqrMagnitude <= slamTriggerRange * slamTriggerRange;
    }

    private void Slam()
    {
        // TODO: slam VFX / sound / camera shake (Art)
        int enemyMask = 1 << gameObject.layer;

        // Flat disk on the ground: a thin box, then keep only what is within slamRadius of the center.
        float groundY = TryGetGroundY(out float y) ? y : transform.position.y;
        Vector3 center = new(
            transform.position.x,
            groundY + slamHeight * 0.5f,
            transform.position.z
        );
        int count = Physics.OverlapBoxNonAlloc(
            center,
            new Vector3(slamRadius, slamHeight * 0.5f, slamRadius),
            slamBuffer,
            Quaternion.identity,
            contactMask | enemyMask,
            QueryTriggerInteraction.Ignore
        );

        bool playerHit = false;
        pushedBuffer.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider col = slamBuffer[i];
            if (!InSlamDisk(col, center))
                continue;

            Player player = col.GetComponentInParent<Player>();
            if (player != null)
            {
                if (playerHit)
                    continue; // one Player, may have several colliders
                playerHit = true;

                // Player ignores it while invincible (dash, after hurt).
                player.TakeHit(
                    HitInfo.Create(
                        slamDamage,
                        gameObject,
                        knockbackVelocity: AwayFromMe(player.transform) * slamKnockbackSpeed
                            + Vector3.up * slamKnockbackUp,
                        knockbackDuration: knockbackDuration
                    )
                );
                continue;
            }

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || enemy == this || pushedBuffer.Contains(enemy))
                continue;
            pushedBuffer.Add(enemy);

            enemy.Push(
                AwayFromMe(enemy.transform) * enemyPushSpeed + Vector3.up * enemyPushUp,
                enemyPushDuration
            );
        }
    }

    private Vector3 AwayFromMe(Transform other)
    {
        Vector3 away = other.position - transform.position;
        away.y = 0f;
        return away.sqrMagnitude > 0.0001f ? away.normalized : transform.forward;
    }

    public override void TakeHit(HitInfo hit)
    {
        base.TakeHit(hit);
        if (!IsDead)
            moveSpeed = baseMoveSpeed + (maxHp - Hp) * speedPerHpLost;
        // TODO: look angrier per HP lost (Art)
    }

    private void SetTelegraph(bool on)
    {
        if (slamTelegraph != null)
            slamTelegraph.SetActive(on);
    }

    // After physics moved the body, so it never lags a frame behind.
    private void LateUpdate()
    {
        if (slamTelegraph != null && slamTelegraph.activeSelf)
            PlaceOnGround(slamTelegraph.transform, telegraphLift);
    }

    // The collider's nearest point to the disk center is within slamRadius (flat distance).
    private bool InSlamDisk(Collider col, Vector3 center)
    {
        Vector3 offset = col.ClosestPoint(center) - center;
        offset.y = 0f;
        return offset.sqrMagnitude <= slamRadius * slamRadius;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        // Slam disk: bottom and top circles.
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Vector3 bottom = transform.position;
        DrawCircle(bottom, slamRadius);
        DrawCircle(bottom + Vector3.up * slamHeight, slamRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, slamTriggerRange);
    }

    private static void DrawCircle(Vector3 center, float radius, int segments = 48)
    {
        Vector3 previous = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
