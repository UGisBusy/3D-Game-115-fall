/// <summary>
/// Heavy Enemy: higher life, moves slow. Chases the Player.
/// </summary>
public class HeavyEnemy : ChaseEnemy
{
    // Default values when the component is added (edit them in the Inspector after).
    private void Reset()
    {
        maxHp = 3;
        moveSpeed = 1.5f;
        knockbackSpeed = 12f;
    }
}
