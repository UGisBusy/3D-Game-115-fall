using UnityEngine;

/// <summary>
/// The enemy's "Glow" light: lights only this enemy, and follows its animated model.
///  - Layer: the Glow light only affects rendering layer EnemyRenderingLayer; in Awake every renderer of the
///    monster model ("Visual", not the shield) joins that layer (and keeps its own, so the moon and fireflies still light it).
///  - Follow: place "Glow" in the prefab where it should be (e.g. below and in front of the face).
///    In Awake it is attached to followBone, keeping that spot, so it moves and turns with the animation.
///    followBone empty = the model's bone closest to where Glow is placed.
/// Runtime, so it still works if an enemy's model ("Visual") is swapped.
/// On EnemyBase.prefab, together with its "Glow" child light.
/// </summary>
public class EnemyGlow : MonoBehaviour
{
    // "Light Layer 1" in Project Settings > Tags and Layers > Rendering Layers (rename it to "Enemy" if you like).
    public const int EnemyRenderingLayer = 1;
    public const uint EnemyRenderingLayerMask = 1u << EnemyRenderingLayer;

    [SerializeField]
    private Light glow;

    [Tooltip(
        "Bone of the model the Glow follows. Empty = the bone closest to where Glow is placed."
    )]
    [SerializeField]
    private Transform followBone;

    private void Awake()
    {
        if (glow == null)
        {
            Transform t = transform.Find("Glow");
            glow = t != null ? t.GetComponent<Light>() : null;
        }

        // Only the monster model ("Visual") is lit, not attachments like the ShieldEnemy's shield:
        // the shield doesn't follow the model's animation, so the moving light would slide a highlight over it.
        // No "Visual" (placeholder model) = light the whole enemy.
        Transform visual = transform.Find("Visual");
        foreach (
            Renderer r in (visual != null ? visual : transform).GetComponentsInChildren<Renderer>(
                true
            )
        )
            r.renderingLayerMask |= EnemyRenderingLayerMask;

        if (glow == null)
            return;

        // The bones are still in the pose the prefab shows, so "closest" matches what was placed.
        Transform bone =
            followBone != null ? followBone : FindClosestBone(visual, glow.transform.position);
        if (bone != null)
            glow.transform.SetParent(bone, true); // keep the placed spot, follow the bone from now on
    }

    // Closest bone of the model (the "Visual" child); null if there is no model or it has no bones.
    private static Transform FindClosestBone(Transform visual, Vector3 position)
    {
        if (visual == null)
            return null;

        Transform closest = null;
        float closestSqr = float.MaxValue;
        foreach (
            SkinnedMeshRenderer skinned in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)
        )
        {
            foreach (Transform bone in skinned.bones)
            {
                if (bone == null)
                    continue;
                float sqr = (bone.position - position).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closest = bone;
                    closestSqr = sqr;
                }
            }
        }
        return closest;
    }
}
