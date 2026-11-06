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
    protected float knockbackSpeed = 8f;

    [SerializeField]
    protected float knockbackUp = 3f;

    [SerializeField]
    protected float knockbackDuration = 0.3f;

    private static readonly Collider[] contactBuffer = new Collider[4];

    public int Hp { get; private set; }
    public bool IsDead => Hp <= 0;

    protected Rigidbody Rb { get; private set; }
    protected Player Target { get; private set; }
    protected bool HasTarget => Target != null && !Target.IsDead;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        Rb.freezeRotation = true;
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
        Behave(Time.fixedDeltaTime);
        CheckContact();
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

    // Player handles invincibility (dash, after hurt), so this can be called every step.
    protected virtual void HitPlayer(Player player)
    {
        Vector3 away = player.transform.position - transform.position;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : transform.forward;

        player.TakeHit(
            HitInfo.Create(
                contactDamage,
                gameObject,
                knockbackVelocity: away * knockbackSpeed + Vector3.up * knockbackUp,
                knockbackDuration: knockbackDuration
            )
        );
    }

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

    protected void TurnTowards(Vector3 flatDirection, float dt)
    {
        if (flatDirection.sqrMagnitude < 0.0001f)
            return;
        Quaternion target = Quaternion.LookRotation(flatDirection);
        Rb.MoveRotation(Quaternion.RotateTowards(Rb.rotation, target, turnSpeed * dt));
    }

    protected bool IsGrounded() =>
        Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            0.2f,
            ~contactMask,
            QueryTriggerInteraction.Ignore
        );

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(contactOffset), contactRadius);
    }
}
