using System;
using System.Collections.Generic;

public static class EventBus
{
    private static Dictionary<EventType, Delegate> eventTable = new Dictionary<EventType, Delegate>();
    //Si besoin de rajouter un event qui prend 3 params soit faire tableau de params soit rajouter une surcharge pour les 3 sections

    #region Subscribe Event
    public static void Subscribe<T1>(EventType eventType, Action<T1> listener)
    {
        AddListener(eventType, listener);
    }

    //Surcharge Pour deux events
    public static void Subscribe<T1, T2>(EventType eventType, Action<T1, T2> listener)
    {
        AddListener(eventType, listener);
    }
    #endregion

    #region Unsubscribe Event
    public static void Unsubscribe<T1>(EventType eventType, Action<T1> listener)
    {
        RemoveListener(eventType, listener);
    }

    //Surcharge Pour deux events
    public static void Unsubscribe<T1, T2>(EventType eventType, Action<T1, T2> listener)
    {
        RemoveListener(eventType, listener);
    }
    #endregion

    #region Publish Event
    public static void Publish<T1>(EventType eventType, T1 arg1)
    {
        if (eventTable.TryGetValue(eventType, out var del))
            (del as Action<T1>)?.Invoke(arg1);
    }

    //Surcharge Pour deux events
    public static void Publish<T1, T2>(EventType eventType, T1 arg1, T2 arg2)
    {
        if (eventTable.TryGetValue(eventType, out var del))
            (del as Action<T1, T2>)?.Invoke(arg1, arg2);
    }
    #endregion

    #region Interne Private class Helper
    private static void AddListener(EventType eventType, Delegate listener)
    {
        if (!eventTable.ContainsKey(eventType))
            eventTable[eventType] = null;

        eventTable[eventType] = Delegate.Combine(eventTable[eventType], listener);
    }

    private static void RemoveListener(EventType eventType, Delegate listener)
    {
        if (!eventTable.ContainsKey(eventType)) return;

        eventTable[eventType] = Delegate.Remove(eventTable[eventType], listener);

        if (eventTable[eventType] == null)
            eventTable.Remove(eventType);
    }
    #endregion
}
