using System;
using UnityEngine;

public enum LevelId
{
    Obstacle,
    Combat,
}

/// <summary>
/// Global menu events (Main Menu, Level Select Menu, Save Load Menu).
/// MenuUIManager raises them from button clicks; SequenceManager listens (mostly through Links).
/// Static so the persistent SequenceManager never needs a ref to the per-scene MenuUIManager.
/// Subscribe in OnEnable and unsubscribe in OnDisable.
/// </summary>
public static class MenuEvents
{
    // ---------- All Menu ----------
    public static event Action BackRequested;

    // ---------- Main Menu ----------
    public static event Action PlayRequested;
    public static event Action LoadRequested;
    public static event Action ExitRequested;

    // ---------- Level Select Menu ----------
    // (level, fromCheckpoint). Raised before LevelStartRequestedWrapper fires,
    // so listeners can store the chosen level before the state transition happens.
    public static event Action<LevelId, bool> LevelStartRequested;
    private static event Action levelStartTransition; // no-arg version for Link

    // ---------- Save Load Menu ----------
    public static event Action<int> SaveFileSelected; // save file index
    public static event Action<int> SaveFileDeleted; // save file index

    // ---------- Wrappers for Link ----------
    public static EventWrapper PlayRequestedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => PlayRequested += h,
            Unsubscribe = h => PlayRequested -= h,
        };

    public static EventWrapper LoadRequestedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => LoadRequested += h,
            Unsubscribe = h => LoadRequested -= h,
        };

    public static EventWrapper ExitRequestedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => ExitRequested += h,
            Unsubscribe = h => ExitRequested -= h,
        };

    public static EventWrapper BackRequestedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => BackRequested += h,
            Unsubscribe = h => BackRequested -= h,
        };

    public static EventWrapper LevelStartRequestedWrapper { get; } =
        new EventWrapper
        {
            Subscribe = h => levelStartTransition += h,
            Unsubscribe = h => levelStartTransition -= h,
        };

    // ---------- Raise ----------
    public static void RaisePlayRequested() => PlayRequested?.Invoke();

    public static void RaiseLoadRequested() => LoadRequested?.Invoke();

    public static void RaiseExitRequested() => ExitRequested?.Invoke();

    public static void RaiseBackRequested() => BackRequested?.Invoke();

    public static void RaiseLevelStartRequested(LevelId level, bool fromCheckpoint)
    {
        LevelStartRequested?.Invoke(level, fromCheckpoint);
        levelStartTransition?.Invoke();
    }

    public static void RaiseSaveFileSelected(int index) => SaveFileSelected?.Invoke(index);

    public static void RaiseSaveFileDeleted(int index) => SaveFileDeleted?.Invoke(index);

    // clear old subscribers at startup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PlayRequested = null;
        LoadRequested = null;
        ExitRequested = null;
        BackRequested = null;
        LevelStartRequested = null;
        levelStartTransition = null;
        SaveFileSelected = null;
        SaveFileDeleted = null;
    }
}
