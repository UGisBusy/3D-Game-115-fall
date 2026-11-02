using System;

namespace PlayerSystem
{
    /// <summary>
    /// Per-Player state machine events. Owned by Player.
    /// Only Links in this Player's state machine listen, other systems use the global PlayerEvents.
    /// </summary>
    public class PlayerTransitions
    {
        public event Action Died;
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

        public PlayerTransitions()
        {
            DiedWrapper = new EventWrapper
            {
                Subscribe = h => Died += h,
                Unsubscribe = h => Died -= h,
            };
            DashRequestedWrapper = new EventWrapper
            {
                Subscribe = h => DashRequested += h,
                Unsubscribe = h => DashRequested -= h,
            };
            DashFinishedWrapper = new EventWrapper
            {
                Subscribe = h => DashFinished += h,
                Unsubscribe = h => DashFinished -= h,
            };
            KnockedBackWrapper = new EventWrapper
            {
                Subscribe = h => KnockedBack += h,
                Unsubscribe = h => KnockedBack -= h,
            };
            KnockbackFinishedWrapper = new EventWrapper
            {
                Subscribe = h => KnockbackFinished += h,
                Unsubscribe = h => KnockbackFinished -= h,
            };
        }

        // ---------- Raise ----------
        public void RaiseDied() => Died?.Invoke();

        public void RaiseDashRequested() => DashRequested?.Invoke();

        public void RaiseDashFinished() => DashFinished?.Invoke();

        public void RaiseKnockedBack() => KnockedBack?.Invoke();

        public void RaiseKnockbackFinished() => KnockbackFinished?.Invoke();
    }
}
