using UnityEngine;

/// <summary>
/// Charge Enemy: stands still and charges periodically.
///   Idle     stand still, turn toward the Player         (chargeInterval sec)
///   Windup   stop turning, telegraph the charge           (windupTime sec)
///   Charging rush straight in the locked direction        (chargeDuration sec), then back to Idle
/// The Player can sidestep or hide behind a pillar during Windup.
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
    private float chargeSpeed = 12f;

    [SerializeField]
    private float chargeDuration = 0.6f;

    private Phase phase;
    private float timer;
    private Vector3 chargeDirection;

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
                    // TODO: telegraph (Art), e.g. flash or shake
                }
                break;

            case Phase.Windup:
                StopMoving();
                if (timer <= 0f)
                {
                    chargeDirection = transform.forward;
                    chargeDirection.y = 0f;
                    chargeDirection.Normalize();
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
}
