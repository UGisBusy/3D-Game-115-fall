using PlayerSystem;
using UnityEngine;

/// <summary>
/// Charge Enemy: stands still and charges periodically.
///   Idle     stand still, turn toward the Player         (chargeInterval sec)
///   Windup   stop turning, telegraph the charge           (windupTime sec)
///   Charging rush straight in the locked direction        (chargeDuration sec), then back to Idle
/// The Player can sidestep or hide behind a pillar during Windup.
/// Knockback: while Charging, a strong push to the side of the charge line (the side the Player is on);
/// otherwise the normal push away, same as other enemies.
/// Telegraph (optional): a flat child object shown only during Windup, marking the charge path.
/// On Awake it is sized to chargeTelegraphWidth x charge distance and placed in front of the enemy,
/// so use a mesh 1 unit wide and long (default Cube or Quad) under the unscaled root.
/// </summary>
public class ChargeEnemy : Enemy
{
    private enum Phase
    {
        Idle,
        Windup,
        Charging,
    }

    [Header("Charge")]
    [SerializeField]
    private float chargeInterval = 3f;

    [SerializeField]
    private float windupTime = 0.6f;

    [SerializeField]
    private float chargeSpeed = 18f;

    [SerializeField]
    private float chargeDuration = 0.8f; // distance = chargeSpeed * chargeDuration (~14.4m)

    [SerializeField]
    private float chargeKnockbackSpeed = 20f; // hit while Charging: strongest knockback of all enemies, pushes to the side

    [SerializeField]
    private GameObject chargeTelegraph; // optional, shown during Windup

    [SerializeField]
    private float chargeTelegraphWidth = 1.5f; // ~ body width + contact range

    private Phase phase;
    private float timer;
    private Vector3 chargeDirection;

    protected override void Awake()
    {
        base.Awake();

        if (chargeTelegraph != null)
        {
            // Strip from the enemy's feet forward, as long as the charge goes.
            float length = chargeSpeed * chargeDuration;
            Transform t = chargeTelegraph.transform;
            t.SetLocalPositionAndRotation(
                new Vector3(0f, t.localPosition.y, length * 0.5f),
                Quaternion.identity
            );
            t.localScale = new Vector3(chargeTelegraphWidth, t.localScale.y, length);
        }
        SetTelegraph(false);
    }

    protected override void Start()
    {
        base.Start();
        phase = Phase.Idle;
        timer = chargeInterval;
    }

    protected override void Behave(float dt)
    {
        timer -= dt;

        switch (phase)
        {
            case Phase.Idle:
                StopMoving();
                TurnTowards(FlatDirectionToTarget(), dt);
                if (timer <= 0f && HasTarget)
                {
                    phase = Phase.Windup;
                    timer = windupTime;
                    SetTelegraph(true);
                    // TODO: more telegraph (Art), e.g. flash or shake
                }
                break;

            case Phase.Windup:
                StopMoving();
                if (timer <= 0f)
                {
                    chargeDirection = transform.forward;
                    chargeDirection.y = 0f;
                    chargeDirection.Normalize();
                    SetTelegraph(false);
                    phase = Phase.Charging;
                    timer = chargeDuration;
                }
                break;

            case Phase.Charging:
                Move(chargeDirection, chargeSpeed);
                if (timer <= 0f)
                {
                    StopMoving();
                    phase = Phase.Idle;
                    timer = chargeInterval;
                }
                break;
        }
    }

    protected override Vector3 GetKnockbackVelocity(Player player)
    {
        if (phase != Phase.Charging)
            return base.GetKnockbackVelocity(player);

        // Push sideways out of the charge line, toward the side the Player is on.
        Vector3 side = Vector3.Cross(Vector3.up, chargeDirection); // right of the charge
        Vector3 offset = player.transform.position - transform.position;
        if (Vector3.Dot(offset, side) < 0f)
            side = -side;

        return side * chargeKnockbackSpeed + Vector3.up * knockbackUp;
    }

    private void SetTelegraph(bool on)
    {
        if (chargeTelegraph != null)
            chargeTelegraph.SetActive(on);
    }
}
