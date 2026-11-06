using System;

public class StateMachine
{
    public State CurrentState { get; private set; }
    Action<State> onEnterState;

    public StateMachine(Action<State> onEnterState = null)
    {
        CurrentState = null;
        this.onEnterState = onEnterState;
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
        onEnterState?.Invoke(state);
    }

    /// Ticks the current state if it is a TickState. Call from the owner's Update.
    public void Tick(float dt)
    {
        (CurrentState as TickState)?.Tick(dt);
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
    }
}
