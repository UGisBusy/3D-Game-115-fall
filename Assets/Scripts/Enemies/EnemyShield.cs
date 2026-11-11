using PlayerSystem;
using UnityEngine;

/// <summary>
/// Dashable shield of a ShieldEnemy. A child object in front of the enemy with a solid Collider.
///  - Arrow hits it: blocked. This is the closest IDamageable, so the enemy behind it isn't hurt.
///  - Player walks into it: blocked (keep it on a layer that collides with the Player, e.g. Default).
///  - Player dashes into it: the shield breaks and bounces the Player back (no damage, ends the dash).
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyShield : MonoBehaviour, IDamageable
{
    [SerializeField]
    private LayerMask playerMask; // the Player layer

    [SerializeField]
    private float touchPadding = 0.2f; // how close counts as touching

    [Header("Bounce on Break")]
    [SerializeField]
    private float bounceSpeed = 12f; // backward, against the dash

    [SerializeField]
    private float bounceUp = 4f;

    [SerializeField]
    private float bounceDuration = 0.3f; // Player has no control for this long

    [SerializeField]
    private float bounceInvincibleTime = 0.5f; // so the enemy behind the shield can't hit right away

    [Header("Shatter on Break")]
    [SerializeField]
    private bool shatter = true; // break into 6 flying shards (one per hexagon section)

    [SerializeField]
    private float shardSpeed = 4f; // along the break direction (the dash)

    [SerializeField]
    private float shardSpread = 3f; // outward from the shield center

    [SerializeField]
    private float shardUp = 2f;

    [SerializeField]
    private float shardSpin = 540f; // max degrees / sec

    [SerializeField]
    private float shardLifetime = 0.7f;

    // Shard shape, in mesh units. Must match Assets/Meshs/HexShield.obj (1 unit tall, front = +Z).
    [SerializeField]
    private float outerRadius = 0.5f; // rim hexagon, center to corner

    [SerializeField]
    private float innerRadius = 0.36f; // front/back face hexagon

    [SerializeField]
    private float rimHalfThickness = 0.05f;

    [SerializeField]
    private float faceHalfThickness = 0.12f;

    private static readonly Collider[] buffer = new Collider[4];
    private Collider shieldCollider;

    private void Awake()
    {
        shieldCollider = GetComponent<Collider>();
    }

    // Arrow hit: absorbed.
    public void TakeHit(HitInfo hit)
    {
        // TODO: block VFX / sound (Art)
    }

    private void FixedUpdate()
    {
        Bounds b = shieldCollider.bounds;
        int count = Physics.OverlapBoxNonAlloc(
            b.center,
            b.extents + Vector3.one * touchPadding,
            buffer,
            Quaternion.identity,
            playerMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            Player player = buffer[i].GetComponentInParent<Player>();
            if (player != null && player.CurrentState == player.Dashing)
            {
                Vector3 dashDirection = player.Dashing.FlatDirection;
                BounceBack(player);
                Break(dashDirection);
                return;
            }
        }
    }

    // Push the Player back against the dash direction.
    private void BounceBack(Player player)
    {
        Vector3 back = -player.Dashing.FlatDirection;
        player.Bounce(
            back * bounceSpeed + Vector3.up * bounceUp,
            bounceDuration,
            bounceInvincibleTime
        );
    }

    // Shards fly away from the shield's front (as if hit from the front).
    public void Break() => Break(-transform.forward);

    // direction: where the hit came from going to, e.g. the dash direction.
    public void Break(Vector3 direction)
    {
        // TODO: break sound (Art)
        if (shatter)
            SpawnShards(direction);

        ShieldEnemy owner = GetComponentInParent<ShieldEnemy>();
        if (owner != null)
            owner.OnShieldBroken(); // stun, then run faster

        Destroy(gameObject);
    }

    // ---------------- Shatter ----------------

    private void SpawnShards(Vector3 direction)
    {
        Material material = TryGetComponent(out MeshRenderer r) ? r.sharedMaterial : null;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : -transform.forward;

        for (int k = 0; k < 6; k++)
        {
            Mesh mesh = BuildSectorMesh(k, out Vector3 localCenter);

            var shard = new GameObject("ShieldShard", typeof(MeshFilter), typeof(MeshRenderer));
            shard.transform.SetPositionAndRotation(transform.TransformPoint(localCenter), transform.rotation);
            shard.transform.localScale = transform.lossyScale;

            Vector3 outward = transform.TransformDirection(new Vector3(localCenter.x, localCenter.y, 0f)).normalized;
            Vector3 velocity =
                direction * (shardSpeed * Random.Range(0.7f, 1.3f))
                + outward * (shardSpread * Random.Range(0.7f, 1.3f))
                + Vector3.up * shardUp;
            Vector3 spin = Random.onUnitSphere * Random.Range(shardSpin * 0.5f, shardSpin);

            shard.AddComponent<ShieldShard>().Init(mesh, material, velocity, spin, shardLifetime * Random.Range(0.85f, 1.15f));
        }
    }

    // One hexagon section k (between corners k and k+1) as a closed, flat-shaded solid.
    // Vertices are relative to the section's center, which is returned in shield-local mesh units.
    private Mesh BuildSectorMesh(int k, out Vector3 center)
    {
        Vector3 Corner(float radius, int i, float z)
        {
            float a = Mathf.Deg2Rad * (90f + 60f * i);
            return new Vector3(radius * Mathf.Cos(a), radius * Mathf.Sin(a), z);
        }

        int j = (k + 1) % 6;
        Vector3 cf = new(0f, 0f, faceHalfThickness);
        Vector3 cb = new(0f, 0f, -faceHalfThickness);
        Vector3 ikf = Corner(innerRadius, k, faceHalfThickness), ijf = Corner(innerRadius, j, faceHalfThickness);
        Vector3 ikb = Corner(innerRadius, k, -faceHalfThickness), ijb = Corner(innerRadius, j, -faceHalfThickness);
        Vector3 okf = Corner(outerRadius, k, rimHalfThickness), ojf = Corner(outerRadius, j, rimHalfThickness);
        Vector3 okb = Corner(outerRadius, k, -rimHalfThickness), ojb = Corner(outerRadius, j, -rimHalfThickness);

        Vector3[][] faces =
        {
            new[] { cf, ikf, ijf }, // front face
            new[] { cb, ikb, ijb }, // back face
            new[] { ikf, ijf, ojf, okf }, // front slope
            new[] { ikb, ijb, ojb, okb }, // back slope
            new[] { okf, ojf, ojb, okb }, // rim
            new[] { cf, ikf, okf, okb, ikb, cb }, // cut side at corner k
            new[] { cf, ijf, ojf, ojb, ijb, cb }, // cut side at corner j
        };

        center = (cf + cb + ikf + ijf + ikb + ijb + okf + ojf + okb + ojb) / 10f;

        var vertices = new System.Collections.Generic.List<Vector3>();
        var normals = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        foreach (Vector3[] face in faces)
        {
            // Normal from the polygon; flip so it points away from the section center (it is convex).
            Vector3 n = Vector3.zero;
            for (int i = 0; i < face.Length; i++)
                n += Vector3.Cross(face[i], face[(i + 1) % face.Length]);
            Vector3 faceCenter = Vector3.zero;
            foreach (Vector3 p in face)
                faceCenter += p;
            faceCenter /= face.Length;
            bool flip = Vector3.Dot(n, faceCenter - center) < 0f;
            n = (flip ? -n : n).normalized;

            int start = vertices.Count;
            foreach (Vector3 p in face)
            {
                vertices.Add(p - center);
                normals.Add(n);
            }
            // Fan. A triangle (a, b, c) faces where cross(b - a, c - a) points, which is n for the
            // listed order; reverse the order when n had to be flipped.
            for (int i = 1; i < face.Length - 1; i++)
            {
                if (flip)
                    triangles.AddRange(new[] { start, start + i + 1, start + i });
                else
                    triangles.AddRange(new[] { start, start + i, start + i + 1 });
            }
        }

        var mesh = new Mesh { name = "ShieldShard" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
