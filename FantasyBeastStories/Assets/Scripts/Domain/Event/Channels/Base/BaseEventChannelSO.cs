using UnityEngine;
using System;

namespace Domain.Event.Channels.Base
{
    public abstract class BaseEventChannelSO : ScriptableObject
    {
        public event Action OnEventRaised;

        public virtual void Raise() => OnEventRaised?.Invoke();

        public void RegisterListener(Action listener) => OnEventRaised += listener;
        public void UnregisterListener(Action listener) => OnEventRaised -= listener;
    }

    public abstract class BaseEventChannelSO<T> : ScriptableObject
    {
        public event Action<T> OnEventRaised;

        public virtual void Raise(T value) => OnEventRaised?.Invoke(value);

        public void RegisterListener(Action<T> listener) => OnEventRaised += listener;
        public void UnregisterListener(Action<T> listener) => OnEventRaised -= listener;
    }

    public abstract class BaseEventChannelSO<T1, T2> : ScriptableObject
    {
        public event Action<T1, T2> OnEventRaised;

        public virtual void Raise(T1 value1, T2 value2) => OnEventRaised?.Invoke(value1, value2);

        public void RegisterListener(Action<T1, T2> listener) => OnEventRaised += listener;
        public void UnregisterListener(Action<T1, T2> listener) => OnEventRaised -= listener;
    }
}
