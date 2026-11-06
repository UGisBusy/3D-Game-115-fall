using System;
using UnityEngine;

/// <summary>
/// Global in-level gameplay events, shared by all levels. Anyone can listen.
/// Subscribe in OnEnable and unsubscribe in OnDisable.
/// </summary>
public static class LevelEvents
{
    // Raised once when the Player picks up a Collectible. Read Type / Amount from it.
    public static event Action<Collectible> Collected;

    // ---------- Raise ----------
    public static void RaiseCollected(Collectible collectible) => Collected?.Invoke(collectible);

    // clear old subscribers at startup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Collected = null;
    }
}
