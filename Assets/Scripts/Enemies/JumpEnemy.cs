using UnityEngine;

/// <summary>
/// Jump Enemy: waits on the ground facing the Player, then hops toward it, periodically.
///  - On the ground: stands still and turns toward the Player.
///  - Jump: aims at the Player, then moves forward at hopSpeed until it lands. hopSpeed 0 = jump in place.
/// While in the air, flat arrows pass under it, so the Player has to time the shot.
/// </summary>
public class JumpEnemy : Enemy
{
    [Header("Jump")]
    [SerializeField]
    private float jumpInterval = 2f; // seconds on the ground between jumps

    [SerializeField]
    private float jumpVelocity = 7f; // ~2.5m high with default gravity

    [SerializeField]
    private float hopSpeed = 4f; // forward speed in the air, 0 = jump in place

    private float timer;
    private bool airborne;
    private Vector3 hopDirection;

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
        hopDirection = HasTarget ? FlatDirectionToTarget() : Vector3.zero;

        Vector3 v = hopDirection * hopSpeed;
        v.y = jumpVelocity;
        Rb.linearVelocity = v;

        airborne = true;
        timer = jumpInterval;
    }
}
