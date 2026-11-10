/// <summary>
/// Light Enemy: lower life, moves fast. Chases the Player.
/// </summary>
public class LightEnemy : ChaseEnemy
{
    // Default values when the component is added (edit them in the Inspector after).
    private void Reset()
    {
        maxHp = 1;
        moveSpeed = 4f;
    }
}
