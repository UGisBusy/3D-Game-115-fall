using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputLink : ILink
{
    Action<State> outputListener;
    InputAction inputAction;
    State nextState;

    public InputLink(State nextState, InputAction inputAction)
    {
        this.nextState = nextState;
        this.inputAction = inputAction;
        outputListener = null;

        this.inputAction.Enable();
    }

    public void Enable()
    {
        inputAction.performed += OnSourceEventRaised;
    }

    public void Disable()
    {
        inputAction.performed -= OnSourceEventRaised;
    }

    public void SetOutputListener(Action<State> outputListener)
    {
        this.outputListener = outputListener;
    }

    private void OnSourceEventRaised(InputAction.CallbackContext context)
    {
        if (outputListener == null)
        {
            Debug.LogWarning($"{nameof(Link)} has no outputListener");
        }

        outputListener?.Invoke(nextState);
    }
}
