using PlayerSystem;
using UnityEngine;

/// <summary>
/// Shield Enemy: chases slowly with a Dashable shield (EnemyShield child) in front.
/// It turns slowly, so the Player can circle around and shoot its side or back,
/// or dash into the shield to break it.
/// When the shield breaks (EnemyShield calls OnShieldBroken):
///  - Stunned for stunTime: stands still, harmless to touch, plays the stun animation (stretched to fit).
///  - Then chases at brokenMoveSpeed with the run animation.
/// </summary>
public class ShieldEnemy : ChaseEnemy
{
    [Header("Shield Broken")]
    [SerializeField]
    private float stunTime = 0.8f;

    [SerializeField]
    private float brokenMoveSpeed = 3.2f; // faster without the shield (Player walks 4.5)

    private float stunTimer;

    public bool ShieldBroken { get; private set; }
    public bool IsStunned => stunTimer > 0f;

    // Default values when the component is added (edit them in the Inspector after).
    private void Reset()
    {
        maxHp = 1;
        moveSpeed = 2f;
        turnSpeed = 120f;
    }

    public void OnShieldBroken()
    {
        if (ShieldBroken || IsDead)
            return;

        ShieldBroken = true;
        stunTimer = stunTime;
        moveSpeed = brokenMoveSpeed;
        StopMoving();
        SetHeldAnim(EnemyVisual.Held.Stun, stunTime);
        if (Visual != null)
            Visual.SetRunning(true);
    }

    protected override void Behave(float dt)
    {
        if (stunTimer > 0f)
        {
            stunTimer -= dt;
            StopMoving();
            if (stunTimer <= 0f)
                SetHeldAnim(EnemyVisual.Held.None);
            return;
        }

        base.Behave(dt);
    }

    // Harmless while stunned.
    protected override void HitPlayer(Player player)
    {
        if (IsStunned)
            return;
        base.HitPlayer(player);
    }
}
