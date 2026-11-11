using UnityEngine;

/// <summary>
/// Projectile fired by BowAbility. Flies straight (no gravity) and breaks on the first thing it hits:
///  - has IDamageable (Enemy): hurt it
///  - has a non-kinematic Rigidbody (Movable Env Obj): push it
///  - anything else (Independent, Dashable Env Obj): just break
/// Moves by sphere-casting ahead every frame, so a fast arrow can't skip through thin objects.
/// Trigger colliders (collectibles, checkpoints) are ignored.
///
/// Prefab setup: this component + a visual child pointing along +Z. No Rigidbody or Collider needed.
/// Visual: low poly fireball (Assets/Fireball). On a hit it spawns impactPrefab; the trail children
/// in detachOnBreak are left behind to fade out instead of vanishing.
/// </summary>
public class Arrow : MonoBehaviour
{
    [SerializeField]
    private float speed = 15f;

    [SerializeField]
    private float maxLifetime = 3f; // breaks after this many seconds if it hits nothing

    [SerializeField]
    private int damage = 1;

    [SerializeField]
    private float pushImpulse = 5f; // heavier objects move less

    [SerializeField]
    private float radius = 0.1f; // thickness used for the hit check

    [SerializeField]
    private LayerMask hitMask = ~0;

    [Header("Visual")]
    [SerializeField]
    private GameObject impactPrefab; // spawned where it hits something (not when it times out)

    [SerializeField]
    private Transform[] detachOnBreak; // particle children (trail, sparks) that keep fading after the break

    [SerializeField]
    private float detachedLifetime = 2f;

    private static readonly RaycastHit[] hitBuffer = new RaycastHit[8];

    private Vector3 direction;
    private GameObject owner;
    private float lifetime;

    private void Awake()
    {
        // Defaults in case Launch is never called.
        direction = transform.forward;
        lifetime = maxLifetime;
    }

    // Called by BowAbility right after spawning. owner is never hit by this arrow.
    public void Launch(Vector3 direction, GameObject owner)
    {
        this.direction = direction.normalized;
        this.owner = owner;
        lifetime = maxLifetime;
        transform.rotation = Quaternion.LookRotation(this.direction);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        lifetime -= dt;
        if (lifetime <= 0f)
        {
            Break();
            return;
        }

        float step = speed * dt;
        if (FindHit(step, out RaycastHit hit))
        {
            // A cast that starts inside a collider reports distance 0 and no usable point.
            bool startedInside = hit.distance <= 0f;
            Vector3 point = startedInside ? transform.position : hit.point;
            Vector3 normal = startedInside ? -direction : hit.normal;
            transform.position += direction * hit.distance;

            HandleHit(hit);
            SpawnImpact(point, normal);
            Break();
            return;
        }

        transform.position += direction * step;
    }

    // Closest hit along this frame's movement, skipping the arrow itself and its owner.
    private bool FindHit(float distance, out RaycastHit closest)
    {
        int count = Physics.SphereCastNonAlloc(
            transform.position,
            radius,
            direction,
            hitBuffer,
            distance,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        closest = default;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (IsIgnored(h.collider))
                continue;
            if (!found || h.distance < closest.distance)
            {
                closest = h;
                found = true;
            }
        }
        return found;
    }

    private bool IsIgnored(Collider col)
    {
        Transform t = col.transform;
        if (t.IsChildOf(transform))
            return true;
        return owner != null && t.IsChildOf(owner.transform);
    }

    private void HandleHit(RaycastHit hit)
    {
        var damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeHit(HitInfo.Create(damage, owner));
            return;
        }

        Rigidbody body = hit.rigidbody;
        if (body != null && !body.isKinematic)
            body.AddForce(direction * pushImpulse, ForceMode.Impulse);
    }

    private void SpawnImpact(Vector3 point, Vector3 normal)
    {
        // TODO: impact sound (Art)
        if (impactPrefab != null)
            Instantiate(impactPrefab, point + normal * 0.05f, Quaternion.LookRotation(normal));
    }

    private void Break()
    {
        if (detachOnBreak != null)
        {
            foreach (Transform t in detachOnBreak)
            {
                if (t == null)
                    continue;
                t.SetParent(null, true);
                foreach (var ps in t.GetComponentsInChildren<ParticleSystem>())
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                foreach (var trail in t.GetComponentsInChildren<TrailRenderer>())
                    trail.emitting = false;
                Destroy(t.gameObject, detachedLifetime);
            }
        }
        Destroy(gameObject);
    }
}
