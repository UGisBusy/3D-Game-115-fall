using System;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// All Player events in one place. Owned by Player (player.Events).
    /// </summary>
    public class PlayerEvents
    {
        // ---------- Gameplay: UI, VFX, Candle, LevelManager subscribe ----------
        public event Action<int, int> HpChanged;            // (hp, maxHp)
        public event Action<HitInfo> Hurt;
        public event Action Died;
        public event Action RespawnRequested;               // LevelManager answers with Player.Spawn(...)
        public event Action Spawned;
        public event Action<Vector3> DashStarted;           // dash direction
        public event Action DashEnded;
        public event Action<int> ArrowsChanged;
        public event Action<Vector3, Vector3> ArrowFired;   // (position, direction)

        // ---------- State transitions: listened to by Links in the Player state machine ----------
        public event Action DashRequested;
        public event Action DashFinished;
        public event Action KnockedBack;
        public event Action KnockbackFinished;

        // ---------- Wrappers for Link ----------
        public EventWrapper DiedWrapper { get; }
        public EventWrapper DashRequestedWrapper { get; }
        public EventWrapper DashFinishedWrapper { get; }
        public EventWrapper KnockedBackWrapper { get; }
        public EventWrapper KnockbackFinishedWrapper { get; }

        public PlayerEvents()
        {
            DiedWrapper = new EventWrapper { Subscribe = h => Died += h, Unsubscribe = h => Died -= h };
            DashRequestedWrapper = new EventWrapper { Subscribe = h => DashRequested += h, Unsubscribe = h => DashRequested -= h };
            DashFinishedWrapper = new EventWrapper { Subscribe = h => DashFinished += h, Unsubscribe = h => DashFinished -= h };
            KnockedBackWrapper = new EventWrapper { Subscribe = h => KnockedBack += h, Unsubscribe = h => KnockedBack -= h };
            KnockbackFinishedWrapper = new EventWrapper { Subscribe = h => KnockbackFinished += h, Unsubscribe = h => KnockbackFinished -= h };
        }

        // ---------- Raise ----------
        public void RaiseHpChanged(int hp, int maxHp) => HpChanged?.Invoke(hp, maxHp);
        public void RaiseHurt(HitInfo hit) => Hurt?.Invoke(hit);
        public void RaiseDied() => Died?.Invoke();
        public void RaiseRespawnRequested() => RespawnRequested?.Invoke();
        public void RaiseSpawned() => Spawned?.Invoke();
        public void RaiseDashStarted(Vector3 direction) => DashStarted?.Invoke(direction);
        public void RaiseDashEnded() => DashEnded?.Invoke();
        public void RaiseArrowsChanged(int arrows) => ArrowsChanged?.Invoke(arrows);
        public void RaiseArrowFired(Vector3 position, Vector3 direction) => ArrowFired?.Invoke(position, direction);

        public void RaiseDashRequested() => DashRequested?.Invoke();
        public void RaiseDashFinished() => DashFinished?.Invoke();
        public void RaiseKnockedBack() => KnockedBack?.Invoke();
        public void RaiseKnockbackFinished() => KnockbackFinished?.Invoke();
    }
}
