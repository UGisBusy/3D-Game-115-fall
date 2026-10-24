using System;

// instantiate wrapper
// EventWrapper wrapper = new EventWrapper{
//     Subscribe = handler => ev += handler,
//     Unsubscribe = handler => ev -= handler
// };

public class EventWrapper
{
    public Action<Action> Subscribe { get; set; }
    public Action<Action> Unsubscribe { get; set; }
}

// public class EventWrapper<T>
// {
//     public Action<Action<T>> Subscribe { get; set; }
//     public Action<Action<T>> Unsubscribe { get; set; }
// }
