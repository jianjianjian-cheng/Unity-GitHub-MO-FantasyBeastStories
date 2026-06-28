using UnityEngine;

namespace Presentation.UI.Framework.Base
{
    public abstract class UIWidget : MonoBehaviour
    {
        [Header("UIWidget 设置")]
        [SerializeField] protected bool autoBind = true;

        protected virtual void Awake()
        {
            if (autoBind) 
                AutoBindComponents();
        }

        protected virtual void OnEnable()
        {
            SubscribeEvents();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeEvents();
        }

        protected virtual void AutoBindComponents() { }

        protected virtual void SubscribeEvents() { }
        protected virtual void UnsubscribeEvents() { }

        public virtual void OnScreenOpened() { }
        public virtual void OnScreenClosed() { }

        protected T FindComponent<T>(string path = "") where T : Component
        {
            if (string.IsNullOrEmpty(path))
                return GetComponent<T>();
            
            Transform target = transform.Find(path);
            if (target == null)
            {
                Debug.LogWarning($"UIWidget: 未找到路径 {path} 下的组件 {typeof(T).Name}");
                return null;
            }
            return target.GetComponent<T>();
        }

        protected T FindComponentInChildren<T>(bool includeInactive = false) where T : Component
        {
            return GetComponentInChildren<T>(includeInactive);
        }
    }
}