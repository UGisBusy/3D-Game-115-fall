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
    public static event Action<Checkpoint> CheckpointTouched;

    // ---------- Raise ----------
    public static void RaiseCollected(Collectible collectible) => Collected?.Invoke(collectible);

    public static void RaiseCheckpointTouched(Checkpoint checkpoint) =>
        CheckpointTouched?.Invoke(checkpoint);

    // clear old subscribers at startup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Collected = null;
        CheckpointTouched = null;
    }
}
