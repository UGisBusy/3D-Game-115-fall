using System;

/// <summary>
/// Level Manager state machine events, shared by all level managers.
/// Only Links in the level's state machine listen; other systems use the global LevelEvents.
/// </summary>
public class LevelTransitions
{
    public event Action StartFinished;
    public event Action PauseRequested;
    public event Action ResumeRequested;
    public event Action Completed;
    public event Action ExitRequested;

    // ---------- Wrappers for Link ----------
    public EventWrapper StartFinishedWrapper { get; }
    public EventWrapper PauseRequestedWrapper { get; }
    public EventWrapper ResumeRequestedWrapper { get; }
    public EventWrapper CompletedWrapper { get; }
    public EventWrapper ExitRequestedWrapper { get; }

    public LevelTransitions()
    {
        StartFinishedWrapper = new EventWrapper
        {
            Subscribe = h => StartFinished += h,
            Unsubscribe = h => StartFinished -= h,
        };
        PauseRequestedWrapper = new EventWrapper
        {
            Subscribe = h => PauseRequested += h,
            Unsubscribe = h => PauseRequested -= h,
        };
        ResumeRequestedWrapper = new EventWrapper
        {
            Subscribe = h => ResumeRequested += h,
            Unsubscribe = h => ResumeRequested -= h,
        };
        CompletedWrapper = new EventWrapper
        {
            Subscribe = h => Completed += h,
            Unsubscribe = h => Completed -= h,
        };
        ExitRequestedWrapper = new EventWrapper
        {
            Subscribe = h => ExitRequested += h,
            Unsubscribe = h => ExitRequested -= h,
        };
    }

    // ---------- Raise ----------
    public void RaiseStartFinished() => StartFinished?.Invoke();

    public void RaisePauseRequested() => PauseRequested?.Invoke();

    public void RaiseResumeRequested() => ResumeRequested?.Invoke();

    public void RaiseCompleted() => Completed?.Invoke();

    public void RaiseExitRequested() => ExitRequested?.Invoke();
}
