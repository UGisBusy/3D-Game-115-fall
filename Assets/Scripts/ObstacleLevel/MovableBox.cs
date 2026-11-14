using PlayerSystem;

namespace ObstacleLevel
{
    /// <summary>
    /// Movable Env Obj. A small box that let the Player pushes just by walking/dashing into it.
    /// The box follows the Player's speed in its movement.
    /// Its weight is the Rigidbody's mass: a heavier box takes longer to reach the Player's speed.
    /// </summary>
    public class MovableBox : PushableBox
    {
        protected override bool CanPush(Player player)
        {
            return true;
        }
    }
}
