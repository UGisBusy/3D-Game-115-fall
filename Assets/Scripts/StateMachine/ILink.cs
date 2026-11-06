using System;

public interface ILink
{
    void Enable();
    void Disable();
    void SetOutputListener(Action<State> outputListener);
}
