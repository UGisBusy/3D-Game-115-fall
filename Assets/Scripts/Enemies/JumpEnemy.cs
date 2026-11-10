using UnityEngine;

/// <summary>
/// Jump Enemy: waits on the ground facing the Player, then hops onto it, periodically.
///  - On the ground: stands still and turns toward the Player.
///  - Jump: predicts where the Player will be when it lands (current velocity x air time x leadFactor),
///    then picks the forward speed that lands it there, up to maxHopSpeed.
///    Running straight doesn't escape it; changing direction while it's in the air does.
/// Assumes it lands at the same height it jumped from (flat arena).
/// While in the air, flat arrows pass under it, so the Player has to time the shot.
/// </summary>
public class JumpEnemy : Enemy
{
    [Header("Jump")]
    [SerializeField]
    private float jumpInterval = 1.5f; // seconds on the ground between jumps

    [SerializeField]
    private float jumpVelocity = 7f; // ~2.5m high, ~1.4s in the air with default gravity

    [SerializeField]
    private float maxHopSpeed = 12f; // max forward speed in the air, ~17m reach with default jump

    [SerializeField]
    [Range(0f, 1f)]
    private float leadFactor = 1f; // 0 = aim at where the Player is, 1 = full prediction

    private float timer;
    private bool airborne;
    private Vector3 hopDirection;
    private float hopSpeed;

    protected override void Start()
    {
        base.Start();
        timer = jumpInterval;
    }

    protected override void Behave(float dt)
    {
        if (airborne)
        {
            Move(hopDirection, hopSpeed);
            TurnTowards(hopDirection, dt);

            // Landed: on the ground again and no longer going up.
            if (IsGrounded() && Rb.linearVelocity.y <= 0f)
            {
                airborne = false;
                StopMoving();
            }
            return;
        }

        StopMoving(); // keeps vertical velocity
        TurnTowards(FlatDirectionToTarget(), dt);

        timer -= dt;
        if (timer <= 0f && IsGrounded())
            Jump();
    }

    private void Jump()
    {
        AimAtLandingSpot();

        Vector3 v = hopDirection * hopSpeed;
        v.y = jumpVelocity;
        Rb.linearVelocity = v;

        airborne = true;
        timer = jumpInterval;
    }

    // Sets hopDirection and hopSpeed so the jump lands where the Player is predicted to be.
    private void AimAtLandingSpot()
    {
        hopDirection = Vector3.zero;
        hopSpeed = 0f;
        if (!HasTarget)
            return; // jump in place

        float gravity = Mathf.Abs(Physics.gravity.y);
        float airTime = gravity > 0f ? 2f * jumpVelocity / gravity : 0f;

        Vector3 playerVelocity = Target.Motor.Velocity;
        playerVelocity.y = 0f;

        Vector3 landing = Target.transform.position + playerVelocity * (airTime * leadFactor);
        Vector3 offset = landing - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude < 0.0001f || airTime <= 0f)
            return;

        hopDirection = offset.normalized;
        hopSpeed = Mathf.Min(offset.magnitude / airTime, maxHopSpeed);
    }
}
