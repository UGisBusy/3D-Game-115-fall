using System;

/// <summary>
/// A State that also runs every frame. The owner calls Tick (e.g. from Update)
/// while this is the current state.
/// Use it directly with a tick action, or subclass it and override Tick (like PlayerState).
/// </summary>
public class TickState : State
{
    private readonly Action<float> tick;

    public TickState(
        string name = "Unnamed",
        Action enter = null,
        Action exit = null,
        Action<float> tick = null
    )
        : base(name, enter, exit)
    {
        this.tick = tick;
    }

    public virtual void Tick(float dt) => tick?.Invoke(dt);
}
