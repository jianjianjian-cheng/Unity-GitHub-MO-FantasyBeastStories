using UnityEngine;

namespace Core
{
    /// <summary>
    /// MonoBehaviour基类 - 提供生命周期钩子和常用功能
    /// </summary>
    public abstract class MonoBehaviourBase : MonoBehaviour
    {
        [SerializeField] private bool _logEnabled = false;

        protected virtual void Awake() { }
        protected virtual void Start() { }
        protected virtual void Update() { }
        protected virtual void LateUpdate() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }

        protected void Log(string message)
        {
            if (_logEnabled)
            {
                Debug.Log($"[{GetType().Name}] {message}");
            }
        }

        protected void LogWarning(string message)
        {
            if (_logEnabled)
            {
                Debug.LogWarning($"[{GetType().Name}] {message}");
            }
        }

        protected void LogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }

        /// <summary>
        /// 安全获取组件
        /// </summary>
        protected T GetComponentSafe<T>() where T : class
        {
            if (TryGetComponent<T>(out var component))
            {
                return component;
            }
            LogError($"Component {typeof(T).Name} not found on {gameObject.name}");
            return null;
        }

        /// <summary>
        /// 安全获取子物体组件
        /// </summary>
        protected T GetComponentInChildrenSafe<T>(bool includeInactive = false) where T : class
        {
            var component = GetComponentInChildren<T>(includeInactive);
            if (component == null)
            {
                LogError($"Component {typeof(T).Name} not found in children of {gameObject.name}");
            }
            return component;
        }
    }
}
