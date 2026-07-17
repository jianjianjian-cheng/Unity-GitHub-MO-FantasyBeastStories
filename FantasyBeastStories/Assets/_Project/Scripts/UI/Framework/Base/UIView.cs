using UnityEngine;

namespace UI.Framework.Base
{
    public abstract class UIView : MonoBehaviour
    {
        [Header("UIView 设置")]
        [SerializeField] protected bool isInitialized = false;

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            if (isInitialized) return;
            BindComponents();
            isInitialized = true;
        }

        protected virtual void BindComponents() { }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnShow();
        }

        public virtual void Hide()
        {
            OnHide();
            gameObject.SetActive(false);
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        protected virtual void SubscribeEvents() { }
        protected virtual void UnsubscribeEvents() { }

        protected virtual void OnEnable()
        {
            if (isInitialized)
                SubscribeEvents();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeEvents();
        }

        protected T FindComponent<T>(string path = "") where T : Component
        {
            if (string.IsNullOrEmpty(path))
                return GetComponent<T>();
            
            Transform target = transform.Find(path);
            if (target == null)
            {
                Debug.LogWarning($"UIView: 未找到路径 {path} 下的组件 {typeof(T).Name}");
                return null;
            }
            return target.GetComponent<T>();
        }
    }
}