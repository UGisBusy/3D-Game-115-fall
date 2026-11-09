using PlayerSystem;
using UnityEngine;

/// <summary>
/// Dashable shield of a ShieldEnemy. A child object in front of the enemy with a solid Collider.
///  - Arrow hits it: blocked. This is the closest IDamageable, so the enemy behind it isn't hurt.
///  - Player walks into it: blocked (keep it on a layer that collides with the Player, e.g. Default).
///  - Player dashes into it: the shield breaks.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyShield : MonoBehaviour, IDamageable
{
    [SerializeField]
    private LayerMask playerMask; // the Player layer

    [SerializeField]
    private float touchPadding = 0.2f; // how close counts as touching

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
                Break();
                return;
            }
        }
    }

    public void Break()
    {
        // TODO: break VFX / sound (Art)
        Destroy(gameObject);
    }
}
