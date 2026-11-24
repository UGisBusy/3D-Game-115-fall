using System;
using UnityEngine;

/// <summary>
/// Level Manager state machine events, shared by all level managers (only one level is loaded at a time).
/// Static so UI buttons (Pause Menu, Fail Screen, Level Complete Screen) can raise them without a ref to the Level Manager.
/// Links in the level's state machine listen, so a request only has an effect
/// if the current state has a Link for it (e.g. Continue only from Pause, Retry only from Fail).
/// Other systems use the global LevelEvents.
/// </summary>
public static class LevelTransitions
{
    // Fired by Level Manger
    public static event Action StartFinished; // finish starting
    public static event Action Failed; // Player died
    public static event Action Completed; // Star collected

    // Fired by input or UI
    public static event Action PauseRequested; // ESC
    public static event Action ContinueRequested; // ESC or Pause Screen Continue Button
    public static event Action RetryRequested; // Space or Fail Screen Retry Button
    public static event Action ExitRequested; // Space on Complete Screen or any Back To Menu Button

    // ---------- Wrappers for Link ----------
    public static EventWrapper StartFinishedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => StartFinished += h,
            Unsubscribe = h => StartFinished -= h,
        };

    public static EventWrapper FailedWrapper { get; } =
        new EventWrapper { Subscribe = h => Failed += h, Unsubscribe = h => Failed -= h };

    public static EventWrapper CompletedWrapper { get; } =
        new EventWrapper { Subscribe = h => Completed += h, Unsubscribe = h => Completed -= h };

    public static EventWrapper PauseRequestedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => PauseRequested += h,
            Unsubscribe = h => PauseRequested -= h,
        };

    public static EventWrapper ContinueRequestedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => ContinueRequested += h,
            Unsubscribe = h => ContinueRequested -= h,
        };

    public static EventWrapper RetryRequestedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => RetryRequested += h,
            Unsubscribe = h => RetryRequested -= h,
        };

    public static EventWrapper ExitRequestedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => ExitRequested += h,
            Unsubscribe = h => ExitRequested -= h,
        };

    // ---------- Raise ----------
    public static void RaiseStartFinished() => StartFinished?.Invoke();

    public static void RaiseFailed() => Failed?.Invoke();

    public static void RaiseCompleted() => Completed?.Invoke();

    public static void RaisePauseRequested() => PauseRequested?.Invoke();

    public static void RaiseContinueRequested() => ContinueRequested?.Invoke();

    public static void RaiseRetryRequested() => RetryRequested?.Invoke();

    public static void RaiseExitRequested() => ExitRequested?.Invoke();

    // clear old subscribers at startup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        StartFinished = null;
        PauseRequested = null;
        ContinueRequested = null;
        Failed = null;
        RetryRequested = null;
        Completed = null;
        ExitRequested = null;
    }
}
