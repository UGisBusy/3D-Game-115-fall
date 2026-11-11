using System;
using UnityEngine;

namespace ObstacleLevel
{
    /// <summary>
    /// The events of the obstacle level only. Anyone in this level can listen.
    /// I don't use the shared LevelEvents for this because the Crystal count is only relevant in this level.
    /// Subscribe in OnEnable and unsubscribe in OnDisable.
    /// </summary>
    public static class ObstacleLevelEvents
    {
        // Raised by a Crystal when the Player picks it up.
        public static event Action CrystalCollected;

        // Raised by the level manager when the Crystal count changes: (collected, total).
        public static event Action<int, int> CrystalCountChanged;

        // ---------- Raise ----------
        public static void RaiseCrystalCollected()
        {
            CrystalCollected?.Invoke();
        }

        public static void RaiseCrystalCountChanged(int collected, int total)
        {
            CrystalCountChanged?.Invoke(collected, total);
        }

        // clear old subscribers at startup
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            CrystalCollected = null;
            CrystalCountChanged = null;
        }
    }
}
