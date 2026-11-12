using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// Dashable Env Obj. A wall that blocks the path, but can be destroyed by the Player's dash.
    ///   Player dashes into it               -> the wall is destroyed, the Player continues moving
    ///   Player respawns                    -> the wall stays destroyed, it does not come back
    /// </summary>
    public class BreakableWall : Dashable
    {
        protected override void OnDashed(Collision collision)
        {
            Destroy(gameObject);
        }
    }
}
