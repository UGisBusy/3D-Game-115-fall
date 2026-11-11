using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Magic circle drawn on the ground while the Bow charges (spawned by BowAbility from Arrow.ChargeCircle).
/// The whole circle is a procedural mesh of glowing line strips, rebuilt every frame from the charge:
///   0.00 - 0.15  faint outline of the outer ring fades in
///   0.00 - 0.35  outer ring + nodes are swept in clockwise from the top
///   0.05 - 0.45  tick marks between the two outer rings
///   0.25 - 0.80  hexagram, drawn node to node by two "pens" at once
///   0.50 - 0.85  inner circle
///   0.55 - 0.90  runes inside the nodes
///   1.00         flash, then pulse until fired
/// Release (fired): flares up, grows and fades out, then destroys itself.
/// Charge drains back to 0 (not fired): the circle un-draws in reverse; BowAbility destroys it.
///
/// Prefab setup: MeshFilter + MeshRenderer (additive particle material with a soft line texture)
/// + this component. Circle lies in the local XZ plane, top = +Z.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MagicCircle : MonoBehaviour
{
    [Header("Look")]
    [SerializeField]
    private float radius = 1.3f; // world meters, outer edge

    [SerializeField, ColorUsage(false, true)]
    private Color color = new(1f, 0.45f, 0.1f); // HDR, the glow color

    [SerializeField]
    private float intensity = 2.5f; // brightness at full charge (above 1 blooms)

    [SerializeField]
    private float lineWidth = 0.14f; // glow width of the main lines, meters (the bright core is ~1/4)

    [SerializeField]
    private float spinSpeed = 20f; // degrees / sec, faster when fully charged

    [SerializeField, Range(0f, 1f)]
    private float ghostAlpha = 0.2f; // faint outline before a part is drawn

    [Header("Full Charge")]
    [SerializeField]
    private float readyFlash = 1.5f; // extra brightness the moment it is fully charged

    [SerializeField]
    private float pulseAmount = 0.2f;

    [SerializeField]
    private float pulseSpeed = 8f;

    [Header("Release")]
    [SerializeField]
    private float releaseTime = 0.35f;

    [SerializeField]
    private float releaseGrow = 0.3f; // scale + this at the end of the release

    [Header("Ground")]
    [SerializeField]
    private LayerMask groundMask = ~0; // exclude Player / Enemy so it lands on the floor

    [SerializeField]
    private float groundOffset = 0.03f; // lift above the floor to avoid z-fighting

    [SerializeField]
    private float groundCheckDistance = 3f;

    // Layout as a fraction of radius.
    private const float OuterRing = 1f;
    private const float BandRing = 0.86f;
    private const float TickInner = 0.885f;
    private const float TickOuter = 0.975f;
    private const float NodeRing = 0.93f;
    private const float NodeRadius = 0.095f;
    private const float InnerRing = 0.44f;
    private const int RingSegments = 120;
    private const int TickCount = 72;
    private const int NodeSegments = 24;

    // Rune strokes in a [-1, 1] box, +y pointing away from the circle center.
    private static readonly Vector2[][] Runes =
    {
        new[]
        {
            V(-0.3f, -0.8f),
            V(-0.3f, 0.8f),
            V(-0.3f, 0.75f),
            V(0.45f, 0.35f),
            V(-0.3f, 0.25f),
            V(0.45f, -0.15f),
        },
        new[]
        {
            V(0f, -0.8f),
            V(0f, 0.8f),
            V(0f, 0.1f),
            V(-0.5f, 0.7f),
            V(0f, 0.1f),
            V(0.5f, 0.7f),
        },
        new[]
        {
            V(-0.35f, 0.8f),
            V(0.3f, 0.3f),
            V(0.3f, 0.3f),
            V(-0.3f, -0.3f),
            V(-0.3f, -0.3f),
            V(0.35f, -0.8f),
        },
        new[]
        {
            V(-0.4f, -0.8f),
            V(-0.4f, 0.8f),
            V(0.4f, -0.8f),
            V(0.4f, 0.8f),
            V(-0.4f, 0.3f),
            V(0.4f, -0.3f),
        },
        new[]
        {
            V(0f, 0.8f),
            V(0.5f, 0.15f),
            V(0.5f, 0.15f),
            V(-0.4f, -0.8f),
            V(0f, 0.8f),
            V(-0.5f, 0.15f),
            V(-0.5f, 0.15f),
            V(0.4f, -0.8f),
        },
        new[]
        {
            V(0f, -0.8f),
            V(0f, 0.8f),
            V(0f, 0.8f),
            V(-0.5f, 0.3f),
            V(0f, 0.8f),
            V(0.5f, 0.3f),
        },
    };

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[8];

    private readonly List<Vector3> vertices = new();
    private readonly List<Vector2> uvs = new();
    private readonly List<Color> colors = new();
    private readonly List<int> triangles = new();

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;

    private Transform owner;
    private float progress;
    private float flash;
    private bool wasFull;
    private float spinAngle;
    private bool releasing;
    private float releaseTimer;
    private Quaternion groundRotation = Quaternion.identity;

    private static Vector2 V(float x, float y) => new(x, y);

    private void Awake()
    {
        mesh = new Mesh { name = "MagicCircle" };
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
        spinAngle = Random.Range(0f, 360f);
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }

    // owner: the transform to stand under (the Player). Its colliders are skipped by the ground check.
    public void Begin(Transform owner)
    {
        this.owner = owner;
        FollowOwner();
    }

    // 0..1 charge. Ignored after Release.
    public void SetProgress(float value)
    {
        if (!releasing)
            progress = Mathf.Clamp01(value);
    }

    // Fired: flare up and fade out, then destroy. Stays where it was cast.
    public void Release()
    {
        if (releasing)
            return;
        releasing = true;
        releaseTimer = 0f;
        progress = 1f;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (!releasing)
            FollowOwner();

        bool full = progress >= 1f;
        if (full && !wasFull)
            flash = 1f;
        wasFull = full;
        flash = Mathf.Max(0f, flash - dt * 4f);

        spinAngle += spinSpeed * (full ? 3f : 1f) * dt;

        float brightness = Mathf.Lerp(0.5f, 1f, progress) + flash * readyFlash;
        if (full)
            brightness += pulseAmount * Mathf.Sin(Time.time * pulseSpeed);

        float scale = 1f;
        if (releasing)
        {
            releaseTimer += dt;
            float t = Mathf.Clamp01(releaseTimer / releaseTime);
            scale = 1f + releaseGrow * (1f - (1f - t) * (1f - t));
            float fade = 1f - t;
            brightness *= (1f + 2f * fade) * fade * fade; // bright flare, then gone
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
        }

        transform.rotation = groundRotation * Quaternion.Euler(0f, spinAngle, 0f);
        transform.localScale = Vector3.one * scale;

        meshRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color * (intensity * Mathf.Max(0f, brightness)));
        meshRenderer.SetPropertyBlock(block);

        Rebuild(progress);
    }

    // ---------------- Ground ----------------

    private void FollowOwner()
    {
        if (owner == null)
            return;

        Vector3 origin = owner.position + Vector3.up * 0.5f;
        int count = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            hitBuffer,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        bool found = false;
        RaycastHit closest = default;
        for (int i = 0; i < count; i++)
        {
            if (hitBuffer[i].transform.IsChildOf(owner))
                continue;
            if (!found || hitBuffer[i].distance < closest.distance)
            {
                closest = hitBuffer[i];
                found = true;
            }
        }

        if (found)
        {
            transform.position = closest.point + closest.normal * groundOffset;
            groundRotation = Quaternion.FromToRotation(Vector3.up, closest.normal);
        }
        else
        {
            transform.position = owner.position + Vector3.up * groundOffset;
            groundRotation = Quaternion.identity;
        }
    }

    // ---------------- Drawing ----------------

    private static float Window(float p, float start, float end) =>
        Mathf.Clamp01((p - start) / (end - start));

    // Clockwise from the top. Node k sits at angle (90 - 60k) degrees.
    private static float NodeAngle(int k) => Mathf.Deg2Rad * (90f - 60f * k);

    private static Vector2 Polar(float r, float angle) =>
        new(r * Mathf.Cos(angle), r * Mathf.Sin(angle));

    // 0..1 position along a clockwise sweep that starts at the top.
    private static float SweepFraction(float angle)
    {
        float f = (Mathf.PI * 0.5f - angle) / (Mathf.PI * 2f);
        return f - Mathf.Floor(f);
    }

    private void Rebuild(float p)
    {
        vertices.Clear();
        uvs.Clear();
        colors.Clear();
        triangles.Clear();

        float R = radius;
        float w = lineWidth;
        float ghost = ghostAlpha * Window(p, 0f, 0.15f);
        float ringSweep = Window(p, 0f, 0.35f);
        float tickSweep = Window(p, 0.05f, 0.45f);
        float innerSweep = Window(p, 0.5f, 0.85f);

        float nodeR = NodeRadius * R;

        // Outer rings, with gaps where the nodes sit.
        DrawRing(OuterRing * R, ringSweep, ghost, w, nodeR);
        DrawRing(BandRing * R, ringSweep, ghost, w * 0.8f, nodeR);

        // Ticks between the rings.
        for (int i = 0; i < TickCount; i++)
        {
            float a = Mathf.PI * 0.5f - Mathf.PI * 2f * (i + 0.5f) / TickCount;
            if (NearNode(a, NodeRing * R, nodeR * 1.4f))
                continue;
            float alpha = SweepFraction(a) < tickSweep ? 0.8f : ghost * 0.5f;
            AddLine(Polar(TickInner * R, a), Polar(TickOuter * R, a), w * 0.6f, alpha);
        }

        // Nodes and their runes.
        for (int k = 0; k < 6; k++)
        {
            float a = NodeAngle(k);
            Vector2 c = Polar(NodeRing * R, a);
            float nodeAlpha = Mathf.Max(ghost, Window(ringSweep, k / 6f, k / 6f + 0.1f));
            AddArc(c, nodeR, 0f, Mathf.PI * 2f, NodeSegments, w * 0.8f, nodeAlpha);

            float runeAlpha = Window(p, 0.55f + 0.04f * k, 0.7f + 0.04f * k);
            if (runeAlpha > 0f)
                DrawRune(Runes[k], c, a, nodeR * 0.55f, w * 0.5f, runeAlpha);
        }

        // Hexagram: two pens, each walks its triangle node -> node+2 -> node+4 -> back.
        for (int edge = 0; edge < 3; edge++)
        {
            float start = 0.25f + edge * 0.18f;
            float t = Window(p, start, start + 0.2f);
            if (t <= 0f)
                continue;
            for (int pen = 0; pen < 2; pen++)
            {
                int from = (pen * 3 + edge * 2) % 6;
                int to = (from + 2) % 6;
                Vector2 a = Polar(NodeRing * R, NodeAngle(from));
                Vector2 b = Polar(NodeRing * R, NodeAngle(to));
                Vector2 dir = (b - a).normalized;
                a += dir * nodeR;
                b -= dir * nodeR;
                AddLine(a, Vector2.Lerp(a, b, t), w, 1f);
            }
        }

        // Inner circle.
        if (innerSweep > 0f)
            AddArc(
                Vector2.zero,
                InnerRing * R,
                Mathf.PI * 0.5f,
                -Mathf.PI * 2f * innerSweep,
                Mathf.CeilToInt(RingSegments * innerSweep),
                w,
                1f
            );

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        float extent = R * 1.2f;
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(extent * 2f, 0.1f, extent * 2f));
    }

    private bool NearNode(float angle, float ringRadius, float gap)
    {
        for (int k = 0; k < 6; k++)
        {
            float d =
                Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, NodeAngle(k) * Mathf.Rad2Deg))
                * Mathf.Deg2Rad;
            if (d * ringRadius < gap)
                return true;
        }
        return false;
    }

    // A full ring, segment by segment: drawn part is bright, the rest is the ghost outline.
    private void DrawRing(float r, float sweep, float ghost, float width, float nodeR)
    {
        float step = Mathf.PI * 2f / RingSegments;
        for (int i = 0; i < RingSegments; i++)
        {
            float a0 = Mathf.PI * 0.5f - step * i;
            float a1 = a0 - step;
            float mid = (a0 + a1) * 0.5f;
            // Gap where the ring passes behind a node.
            if (Mathf.Abs(r - NodeRing * radius) < nodeR * 2f && NearNode(mid, r, nodeR * 1.15f))
                continue;
            float alpha = (i + 1f) / RingSegments <= sweep ? 1f : ghost;
            AddLine(Polar(r, a0), Polar(r, a1), width, alpha);
        }
    }

    private void AddArc(
        Vector2 center,
        float r,
        float startAngle,
        float sweepAngle,
        int segments,
        float width,
        float alpha
    )
    {
        if (alpha <= 0f || segments <= 0)
            return;
        for (int i = 0; i < segments; i++)
        {
            float a0 = startAngle + sweepAngle * i / segments;
            float a1 = startAngle + sweepAngle * (i + 1) / segments;
            AddLine(center + Polar(r, a0), center + Polar(r, a1), width, alpha);
        }
    }

    // Rune strokes are pairs of points; glyph +y points away from the circle center.
    private void DrawRune(
        Vector2[] strokes,
        Vector2 center,
        float angle,
        float size,
        float width,
        float alpha
    )
    {
        Vector2 up = new(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 right = new(up.y, -up.x);
        for (int i = 0; i + 1 < strokes.Length; i += 2)
        {
            Vector2 a = center + (right * strokes[i].x + up * strokes[i].y) * size;
            Vector2 b = center + (right * strokes[i + 1].x + up * strokes[i + 1].y) * size;
            AddLine(a, b, width, alpha);
        }
    }

    // One glowing quad on the ground plane (x, z). The texture's V axis is the soft cross-section.
    // Segments only touch end to end (no overlap), since overlaps would add up into bright dots.
    private void AddLine(Vector2 a, Vector2 b, float width, float alpha)
    {
        if (alpha <= 0f)
            return;
        Vector2 d = b - a;
        float length = d.magnitude;
        if (length < 0.0001f)
            return;
        d /= length;
        Vector2 n = new Vector2(-d.y, d.x) * (width * 0.5f);

        int start = vertices.Count;
        vertices.Add(new Vector3(a.x - n.x, 0f, a.y - n.y));
        vertices.Add(new Vector3(a.x + n.x, 0f, a.y + n.y));
        vertices.Add(new Vector3(b.x + n.x, 0f, b.y + n.y));
        vertices.Add(new Vector3(b.x - n.x, 0f, b.y - n.y));
        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(0f, 1f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(1f, 0f));
        var c = new Color(1f, 1f, 1f, alpha);
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
        // Facing up (+Y). The material is double sided anyway.
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }
}
