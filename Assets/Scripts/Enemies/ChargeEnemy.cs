using PlayerSystem;
using UnityEngine;

/// <summary>
/// Charge Enemy: stands still and charges periodically.
///   Idle     stand still, turn toward the Player         (chargeInterval sec; firstChargeDelay after spawn)
///   Windup   stop turning, telegraph the charge           (windupTime sec)
///   Charging rush straight in the locked direction, then back to Idle
/// Charge length is picked when the Windup starts:
///   - Player within one normal charge (chargeSpeed * chargeDuration): normal charge.
///   - Player farther than that: far charge to the Player + farChargeOvershoot, at most farChargeMaxDistance.
/// The Player can sidestep or hide behind a pillar during Windup.
/// Knockback: while Charging, a strong push to the side of the charge line (the side the Player is on);
/// otherwise the normal push away, same as other enemies.
/// Telegraph (optional): a flat child object shown only during Windup, marking the charge path.
/// It is resized to chargeTelegraphWidth x this charge's length each Windup,
/// so use a mesh 1 unit wide and long (default Cube or Quad) under the unscaled root.
/// While shown it is kept on the ground (its local Y at Awake = height above the ground), even if the
/// enemy is knocked into the air.
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
    private float firstChargeDelay = 1f; // seconds from spawn until the first charge starts (includes windup)

    [SerializeField]
    private float windupTime = 0.6f;

    [SerializeField]
    private float chargeSpeed = 18f;

    [SerializeField]
    private float chargeDuration = 0.8f; // normal charge: distance = chargeSpeed * chargeDuration (~14.4m)

    [SerializeField]
    private float farChargeOvershoot = 2f; // far charge: distance to the Player + this

    [SerializeField]
    private float farChargeMaxDistance = 22.5f; // far charge cap, ~3/4 of the 30m combat arena

    [SerializeField]
    private float chargeKnockbackSpeed = 20f; // hit while Charging: strongest knockback of all enemies, pushes to the side

    [SerializeField]
    private GameObject chargeTelegraph; // optional, shown during Windup

    [SerializeField]
    private float chargeTelegraphWidth = 1.5f; // ~ body width + contact range

    private Phase phase;
    private float timer;
    private float telegraphLift; // telegraph height above the ground
    private Vector3 chargeDirection;
    private float chargeLength; // distance of the current charge

    private float NormalChargeLength => chargeSpeed * chargeDuration;

    protected override void Awake()
    {
        base.Awake();
        if (chargeTelegraph != null)
            telegraphLift = chargeTelegraph.transform.localPosition.y;
        SetTelegraph(false);
    }

    protected override void Start()
    {
        base.Start();
        phase = Phase.Idle;
        timer = Mathf.Max(0f, firstChargeDelay - windupTime);
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
                    chargeLength = PickChargeLength();
                    SizeTelegraph(chargeLength);
                    SetTelegraph(true);
                    SetHeldAnim(EnemyVisual.Held.Windup);
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
                    SetHeldAnim(EnemyVisual.Held.Action);
                    phase = Phase.Charging;
                    timer = chargeLength / chargeSpeed;
                }
                break;

            case Phase.Charging:
                Move(chargeDirection, chargeSpeed);
                if (timer <= 0f)
                {
                    StopMoving();
                    SetHeldAnim(EnemyVisual.Held.None);
                    phase = Phase.Idle;
                    timer = chargeInterval;
                }
                break;
        }
    }

    // Normal charge if it can reach the Player, otherwise a far charge just past the Player (capped).
    private float PickChargeLength()
    {
        Vector3 d = Target.transform.position - transform.position;
        d.y = 0f;
        float distance = d.magnitude;
        if (distance <= NormalChargeLength)
            return NormalChargeLength;
        return Mathf.Min(distance + farChargeOvershoot, farChargeMaxDistance);
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

    // Strip from the enemy's feet forward, as long as this charge goes.
    private void SizeTelegraph(float length)
    {
        if (chargeTelegraph == null)
            return;

        Transform t = chargeTelegraph.transform;
        t.SetLocalPositionAndRotation(
            new Vector3(0f, telegraphLift, length * 0.5f),
            Quaternion.identity
        );
        t.localScale = new Vector3(chargeTelegraphWidth, t.localScale.y, length);
    }

    private void SetTelegraph(bool on)
    {
        if (chargeTelegraph != null)
            chargeTelegraph.SetActive(on);
    }

    // After physics moved the body, so it never lags a frame behind.
    private void LateUpdate()
    {
        if (chargeTelegraph != null && chargeTelegraph.activeSelf)
            PlaceOnGround(chargeTelegraph.transform, telegraphLift);
    }
}
