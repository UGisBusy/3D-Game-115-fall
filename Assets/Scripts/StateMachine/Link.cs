using System;
using UnityEngine;

public class Link : ILink
{
    Action<State> outputListener;
    EventWrapper sourceWrapper;
    State nextState;

    public Link(State nextState, EventWrapper sourceWrapper)
    {
        this.nextState = nextState;
        this.sourceWrapper = sourceWrapper;
        outputListener = null;
    }

    public void Enable()
    {
        sourceWrapper.Subscribe(OnSourceEventRaised);
    }

    public void Disable()
    {
        sourceWrapper.Unsubscribe(OnSourceEventRaised);
    }

    public void SetOutputListener(Action<State> outputListener)
    {
        this.outputListener = outputListener;
    }

    private void OnSourceEventRaised()
    {
        if (outputListener == null)
        {
            Debug.LogWarning($"{nameof(Link)} has no outputListener");
        }

        outputListener?.Invoke(nextState);
    }
}
