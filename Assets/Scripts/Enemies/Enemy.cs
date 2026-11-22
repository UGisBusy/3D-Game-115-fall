using PlayerSystem;
using UnityEngine;

/// <summary>
/// Base of all enemies. Owns HP, contact damage and movement helpers; each type only writes Behave().
///  - Anything that hurts it (Arrow) calls TakeHit: HP - damage, at 0 it dies and disappears.
///    EnemySpawner notices the destroyed object, so the Wave can be cleared.
///  - Touching the Player hurts it and knocks it back. A dashing Player is invincible,
///    so dashing through an enemy does nothing (spec).
///  - Moves with a Rigidbody (gravity on, rotation frozen) by setting its horizontal velocity.
///
/// Setup (prefab):
///  - Pivot at the feet, body at least ~1m tall so flat arrows (fired at ~1m) can hit it.
///  - Rigidbody + a solid Collider, on an "Enemy" layer.
///  - Project Settings > Physics > Layer Collision Matrix: turn off Player x Enemy,
///    so the Player can dash through. Contact damage uses an overlap check, so it still works.
///  - Contact Mask = the Player layer.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField]
    protected int maxHp = 1;

    [Header("Move")]
    [SerializeField]
    protected float moveSpeed = 3f;

    [SerializeField]
    protected float turnSpeed = 360f; // degrees / sec

    [Header("Contact Damage")]
    [SerializeField]
    protected int contactDamage = 1;

    [SerializeField]
    protected float contactRadius = 0.6f;

    [SerializeField]
    protected Vector3 contactOffset = new(0f, 0.9f, 0f); // center of the touch sphere, local

    [SerializeField]
    protected LayerMask contactMask; // the Player layer

    [SerializeField]
    protected float knockbackSpeed = 14f; // push distance ~ speed / Player externalDamping (8) => ~1.75m

    [SerializeField]
    protected float knockbackUp = 4f;

    [SerializeField]
    protected float knockbackDuration = 0.3f;

    [Header("Pushed")]
    [SerializeField]
    protected float pushDamping = 8f; // how fast a push on this enemy fades (same feel as the Player)

    private static readonly Collider[] contactBuffer = new Collider[4];

    private float pushedTimer;

    public int Hp { get; private set; }
    public bool IsDead => Hp <= 0;
    public bool IsPushed => pushedTimer > 0f;

    protected Rigidbody Rb { get; private set; }
    protected Player Target { get; private set; }
    protected bool HasTarget => Target != null && !Target.IsDead;

    // Optional: animates the model. Null when the enemy has no EnemyVisual.
    protected EnemyVisual Visual { get; private set; }

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        Rb.freezeRotation = true;
        Visual = GetComponent<EnemyVisual>();
        Hp = maxHp;
    }

    protected virtual void Start()
    {
        Target = FindFirstObjectByType<Player>(); // TODO: let the spawner pass the Player in
    }

    private void FixedUpdate()
    {
        if (IsDead)
            return;

        float dt = Time.fixedDeltaTime;
        if (pushedTimer > 0f)
        {
            // Flying from a push: own movement and contact damage are off, the push fades.
            pushedTimer -= dt;
            Vector3 v = Rb.linearVelocity;
            Vector3 flat = Vector3.Lerp(new Vector3(v.x, 0f, v.z), Vector3.zero, pushDamping * dt);
            Rb.linearVelocity = new Vector3(flat.x, v.y, flat.z);
            return;
        }

        Behave(dt);
        CheckContact();
    }

    // Knocked back without damage (e.g. a Heavy slam). Its own movement stops for `duration`.
    public void Push(Vector3 velocity, float duration)
    {
        if (IsDead)
            return;
        Rb.linearVelocity = velocity;
        pushedTimer = Mathf.Max(pushedTimer, duration);
        if (Visual != null)
            Visual.PlayHit();
    }

    // Movement / attack pattern of this type. Runs every physics step.
    protected abstract void Behave(float dt);

    // ---------------- Damage ----------------

    public virtual void TakeHit(HitInfo hit)
    {
        if (IsDead)
            return;

        Hp = Mathf.Max(0, Hp - hit.Damage);
        // TODO: hurt flash / sound (Art)
        if (Hp == 0)
            Die();
        else if (Visual != null)
            Visual.PlayHit();
    }

    protected virtual void Die()
    {
        // TODO: death VFX / sound (Art)
        Destroy(gameObject);
    }

    private void CheckContact()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.TransformPoint(contactOffset),
            contactRadius,
            contactBuffer,
            contactMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            Player player = contactBuffer[i].GetComponentInParent<Player>();
            if (player != null)
            {
                HitPlayer(player);
                return;
            }
        }
    }

    // Called every step while touching. An invincible Player (dash, after hurt, after spawn) is not hit,
    // so OnHitPlayer only runs for hits that land.
    protected virtual void HitPlayer(Player player)
    {
        if (player.IsDead || player.IsInvincible)
            return;

        player.TakeHit(
            HitInfo.Create(
                contactDamage,
                gameObject,
                knockbackVelocity: GetKnockbackVelocity(player),
                knockbackDuration: knockbackDuration
            )
        );
        if (Visual != null)
            Visual.PlayAttack();
        OnHitPlayer();
    }

    // Default: push the Player straight away from this enemy. Override for a different push.
    protected virtual Vector3 GetKnockbackVelocity(Player player)
    {
        Vector3 away = player.transform.position - transform.position;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : transform.forward;
        return away * knockbackSpeed + Vector3.up * knockbackUp;
    }

    // Runs after this enemy hurts the Player. Override to react (e.g. pause).
    protected virtual void OnHitPlayer() { }

    // ---------------- Helpers for subclasses ----------------

    protected Vector3 FlatDirectionToTarget()
    {
        if (Target == null)
            return Vector3.zero;
        Vector3 d = Target.transform.position - transform.position;
        d.y = 0f;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.zero;
    }

    // Sets horizontal velocity; vertical (gravity, jump) is kept.
    protected void Move(Vector3 flatDirection, float speed)
    {
        Vector3 v = flatDirection * speed;
        v.y = Rb.linearVelocity.y;
        Rb.linearVelocity = v;
    }

    protected void StopMoving() => Move(Vector3.zero, 0f);

    // Holds an animation (windup, charging, airborne) until set back to None. No-op without EnemyVisual.
    // duration > 0 stretches the clip to play once over that time.
    protected void SetHeldAnim(EnemyVisual.Held state, float duration = 0f)
    {
        if (Visual != null)
            Visual.SetHeld(state, duration);
    }

    protected void TurnTowards(Vector3 flatDirection, float dt)
    {
        if (flatDirection.sqrMagnitude < 0.0001f)
            return;
        Quaternion target = Quaternion.LookRotation(flatDirection);
        Rb.MoveRotation(Quaternion.RotateTowards(Rb.rotation, target, turnSpeed * dt));
    }

    // Like TurnTowards but eases in and out: fast when far from the target facing, slowing down
    // as it gets close. Still never faster than turnSpeed. Higher smoothing = snappier.
    protected void SmoothTurnTowards(Vector3 flatDirection, float smoothing, float dt)
    {
        if (flatDirection.sqrMagnitude < 0.0001f)
            return;
        Quaternion target = Quaternion.LookRotation(flatDirection);
        Quaternion eased = Quaternion.Slerp(Rb.rotation, target, 1f - Mathf.Exp(-smoothing * dt));
        Rb.MoveRotation(Quaternion.RotateTowards(Rb.rotation, eased, turnSpeed * dt));
    }

    protected bool IsGrounded() =>
        Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            0.2f,
            ~contactMask,
            QueryTriggerInteraction.Ignore
        );

    private static readonly RaycastHit[] groundHits = new RaycastHit[8];

    // Moves t (e.g. a telegraph child) straight down onto the ground below this enemy, lift above it,
    // so it stays on the floor even while the enemy is knocked into the air.
    // Leaves t alone if no ground is found.
    protected void PlaceOnGround(Transform t, float lift, float maxDistance = 50f)
    {
        if (!TryGetGroundY(out float groundY, maxDistance))
            return;
        Vector3 p = t.position;
        p.y = groundY + lift;
        t.position = p;
    }

    // Height of the ground straight below this enemy.
    // Skips the Player, other enemies and this enemy's own children.
    protected bool TryGetGroundY(out float groundY, float maxDistance = 50f)
    {
        int mask = ~(contactMask.value | (1 << gameObject.layer));
        int count = Physics.RaycastNonAlloc(
            transform.position + Vector3.up * 0.5f,
            Vector3.down,
            groundHits,
            maxDistance,
            mask,
            QueryTriggerInteraction.Ignore
        );

        groundY = 0f;
        bool found = false;
        float closest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = groundHits[i];
            if (h.transform.IsChildOf(transform) || h.distance >= closest)
                continue;
            closest = h.distance;
            groundY = h.point.y;
            found = true;
        }
        return found;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(contactOffset), contactRadius);
    }
}
