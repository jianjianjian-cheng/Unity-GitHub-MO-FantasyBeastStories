using UnityEngine;
using System;

namespace Core.Channels.Base
{
    public abstract class BaseEventChannelSO : ScriptableObject
    {
        [SerializeField] private bool enableDebugLog;
        public event Action OnEventRaised;

        public virtual void Raise()
        {
            if (enableDebugLog)
                Debug.Log($"[Event] {name} raised");

            if (OnEventRaised == null) return;
            foreach (Delegate listener in OnEventRaised.GetInvocationList())
            {
                try
                {
                    ((Action)listener)?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Event] {name} listener error: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        public void RegisterListener(Action listener) => OnEventRaised += listener;
        public void UnregisterListener(Action listener) => OnEventRaised -= listener;
    }

    public abstract class BaseEventChannelSO<T> : ScriptableObject
    {
        [SerializeField] private bool enableDebugLog;
        public event Action<T> OnEventRaised;

        public virtual void Raise(T value)
        {
            if (enableDebugLog)
                Debug.Log($"[Event] {name} raised: {value}");

            if (OnEventRaised == null) return;
            foreach (Delegate listener in OnEventRaised.GetInvocationList())
            {
                try
                {
                    ((Action<T>)listener)?.Invoke(value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Event] {name} listener error: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        public void RegisterListener(Action<T> listener) => OnEventRaised += listener;
        public void UnregisterListener(Action<T> listener) => OnEventRaised -= listener;
    }

    public abstract class BaseEventChannelSO<T1, T2> : ScriptableObject
    {
        [SerializeField] private bool enableDebugLog;
        public event Action<T1, T2> OnEventRaised;

        public virtual void Raise(T1 value1, T2 value2)
        {
            if (enableDebugLog)
                Debug.Log($"[Event] {name} raised: ({value1}, {value2})");

            if (OnEventRaised == null) return;
            foreach (Delegate listener in OnEventRaised.GetInvocationList())
            {
                try
                {
                    ((Action<T1, T2>)listener)?.Invoke(value1, value2);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Event] {name} listener error: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        public void RegisterListener(Action<T1, T2> listener) => OnEventRaised += listener;
        public void UnregisterListener(Action<T1, T2> listener) => OnEventRaised -= listener;
    }
}