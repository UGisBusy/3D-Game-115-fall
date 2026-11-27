using System;
using LevelSystem;
using UnityEngine;

/// <summary>
/// Global in-level gameplay events, shared by all levels. Anyone can listen.
/// Subscribe in OnEnable and unsubscribe in OnDisable.
/// </summary>
public static class LevelEvents
{
    // Raised once when the Player picks up a Collectible. Read Type / Amount from it.
    public static event Action<Collectible> Collected;

    // Raised when the Player touches a Checkpoint.
    public static event Action<Checkpoint> CheckpointActivated;

    // Raised by the Level Manager when its Exit state finished cleaning up.
    // SequenceManager answers by leaving the level.
    public static event Action Exited;

    // Raised by the Save Button (Pause / Fail Screen). The Level Manager saves (overwrites) if it allows saving now.
    public static event Action SaveRequested;

    // ---------- Raise ----------
    public static void RaiseCollected(Collectible collectible) => Collected?.Invoke(collectible);

    public static void RaiseCheckpointActivated(Checkpoint checkpoint) =>
        CheckpointActivated?.Invoke(checkpoint);

    public static void RaiseExited() => Exited?.Invoke();

    public static void RaiseSaveRequested() => SaveRequested?.Invoke();

    // clear old subscribers at startup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Collected = null;
        CheckpointActivated = null;
        Exited = null;
        SaveRequested = null;
    }
}
