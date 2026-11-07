/// <summary>
/// Shield Enemy: chases slowly with a Dashable shield (EnemyShield child) in front.
/// It turns slowly, so the Player can circle around and shoot its side or back,
/// or dash into the shield to break it.
/// </summary>
public class ShieldEnemy : ChaseEnemy
{
    // Default values when the component is added (edit them in the Inspector after).
    private void Reset()
    {
        maxHp = 1;
        moveSpeed = 2f;
        turnSpeed = 120f;
    }
}
