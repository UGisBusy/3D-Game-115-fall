using System;

/// <summary>
/// A State that also runs every frame. The owner calls Tick (e.g. from Update)
/// while this is the current state.
/// </summary>
public abstract class TickState : State
{
    protected TickState(string name = "Unnamed", Action enter = null, Action exit = null)
        : base(name, enter, exit) { }

    public abstract void Tick(float dt);
}
