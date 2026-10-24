using System;

public class StateMachine
{
    public State CurrentState { get; private set; }
    Action<State> onStateChange;

    public StateMachine(Action<State> onStateChange = null)
    {
        CurrentState = null;
        this.onStateChange = onStateChange;
    }

    public void EnterState(State state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        ExitState();

        CurrentState = state;
        CurrentState.BindTransitionEvent(OnStateTransition);
        CurrentState.Enter();
    }

    public void ExitState()
    {
        if (CurrentState == null)
        {
            return;
        }

        CurrentState.Exit();
        CurrentState = null;
    }

    private void OnStateTransition(State nextState)
    {
        ExitState();
        EnterState(nextState);
        onStateChange?.Invoke(nextState);
    }
}
