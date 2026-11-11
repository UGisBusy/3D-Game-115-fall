using UnityEngine;

/// <summary>
/// One flying piece of a broken EnemyShield. Created by EnemyShield.Break; no physics or collider,
/// it moves itself: bursts out, slows down, falls, spins, flashes bright, then fades and shrinks away.
/// Owns its generated mesh and destroys it with itself.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShieldShard : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private const float Drag = 3f; // how fast the burst slows down
    private const float GravityScale = 0.6f;
    private const float FlashStrength = 4f; // extra glow at the start

    private Vector3 velocity;
    private Vector3 angularVelocity; // degrees / sec, world space
    private float lifetime;
    private float age;

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;
    private Color baseColor;
    private Color baseEmission;
    private Vector3 startScale;

    public void Init(
        Mesh shardMesh,
        Material material,
        Vector3 velocity,
        Vector3 angularVelocity,
        float lifetime
    )
    {
        mesh = shardMesh;
        this.velocity = velocity;
        this.angularVelocity = angularVelocity;
        this.lifetime = Mathf.Max(0.05f, lifetime);

        GetComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        baseColor =
            material != null && material.HasProperty(BaseColorId)
                ? material.GetColor(BaseColorId)
                : Color.white;
        baseEmission =
            material != null && material.HasProperty(EmissionColorId)
                ? material.GetColor(EmissionColorId)
                : Color.black;
        block = new MaterialPropertyBlock();
        startScale = transform.localScale;
        Apply(0f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        float t = age / lifetime;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        velocity *= Mathf.Exp(-Drag * dt);
        velocity += Physics.gravity * (GravityScale * dt);
        transform.position += velocity * dt;
        transform.Rotate(angularVelocity * dt, Space.World);

        Apply(t);
    }

    // t: 0 = just broke, 1 = gone.
    private void Apply(float t)
    {
        Color c = baseColor;
        c.a = baseColor.a * (1f - t * t); // stays visible, then fades fast at the end
        float flash = 1f + FlashStrength * Mathf.Pow(1f - Mathf.Clamp01(t * 4f), 2f); // bright for the first quarter
        block.SetColor(BaseColorId, c);
        block.SetColor(EmissionColorId, baseEmission * flash);
        meshRenderer.SetPropertyBlock(block);

        transform.localScale = startScale * Mathf.Lerp(1f, 0.4f, t * t);
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }
}
