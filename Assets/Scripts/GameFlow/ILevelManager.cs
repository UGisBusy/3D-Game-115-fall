/// <summary>
/// A level scene's Level Manager. It registers itself with SequenceManager in Awake,
/// and SequenceManager calls StartLevel once the level scene finished loading (spec: Start State).
/// </summary>
public interface ILevelManager
{
    /// <param name="checkpoint">
    /// 0 = start from the beginning. Meaning of x > 0 depends on the level:
    /// Combat: start at Wave x (0-based). Obstacle: start at Checkpoint x.
    /// </param>
    void StartLevel(int checkpoint);
}
