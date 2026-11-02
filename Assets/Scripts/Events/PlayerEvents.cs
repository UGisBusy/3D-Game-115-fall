using System;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Global Player gameplay events. Anyone can listen.
    /// Subscribe in OnEnable and unsubscribe in OnDisable.
    /// Only Player-side code should call the Raise methods.
    /// </summary>
    public static class PlayerEvents
    {
        public static event Action<int, int> HpChanged; // (hp, maxHp)
        public static event Action<HitInfo> Hurt;
        public static event Action Died;
        public static event Action RespawnRequested; // LevelManager answers with Player.Spawn(...)
        public static event Action Spawned;
        public static event Action<Vector3> DashStarted; // dash direction
        public static event Action DashEnded;
        public static event Action<int> ArrowsChanged;
        public static event Action<Vector3, Vector3> ArrowFired; // (position, direction)

        // ---------- Raise ----------
        public static void RaiseHpChanged(int hp, int maxHp) => HpChanged?.Invoke(hp, maxHp);

        public static void RaiseHurt(HitInfo hit) => Hurt?.Invoke(hit);

        public static void RaiseDied() => Died?.Invoke();

        public static void RaiseRespawnRequested() => RespawnRequested?.Invoke();

        public static void RaiseSpawned() => Spawned?.Invoke();

        public static void RaiseDashStarted(Vector3 direction) => DashStarted?.Invoke(direction);

        public static void RaiseDashEnded() => DashEnded?.Invoke();

        public static void RaiseArrowsChanged(int arrows) => ArrowsChanged?.Invoke(arrows);

        public static void RaiseArrowFired(Vector3 position, Vector3 direction) =>
            ArrowFired?.Invoke(position, direction);

        // clear old subscribers at startup
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            HpChanged = null;
            Hurt = null;
            Died = null;
            RespawnRequested = null;
            Spawned = null;
            DashStarted = null;
            DashEnded = null;
            ArrowsChanged = null;
            ArrowFired = null;
        }
    }
}
