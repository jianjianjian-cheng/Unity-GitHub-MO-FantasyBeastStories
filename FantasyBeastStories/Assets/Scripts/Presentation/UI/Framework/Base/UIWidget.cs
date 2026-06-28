using Presentation.UI.Framework.Animation;
using UnityEngine;

namespace Presentation.UI.Framework.Base
{
    public abstract class UIWidget : MonoBehaviour
    {
        [Header("UIWidget 设置")]
        [SerializeField] protected bool autoBind = true;

        [Header("UIWidget 选中动画")]
        [SerializeField] protected UIAnimationBase selectAnimation;
        [SerializeField] protected UIAnimationBase deselectAnimation;

        [Header("UIWidget 悬浮动画")]
        [SerializeField] protected UIAnimationBase hoverEnterAnimation;
        [SerializeField] protected UIAnimationBase hoverExitAnimation;

        /// <summary>当前是否处于选中状态</summary>
        public bool IsSelected { get; private set; }

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

        // ──────────────────────────────────────────────
        //  选中 / 取消选中动画
        // ──────────────────────────────────────────────

        /// <summary>播放选中动画，同时将 IsSelected 设为 true</summary>
        public async void PlaySelect()
        {
            IsSelected = true;
            if (selectAnimation != null)
                await selectAnimation.PlayAsync(gameObject);
            OnSelectCompleted();
        }

        /// <summary>播放取消选中动画，同时将 IsSelected 设为 false</summary>
        public async void PlayDeselect()
        {
            IsSelected = false;
            if (deselectAnimation != null)
                await deselectAnimation.PlayAsync(gameObject);
            OnDeselectCompleted();
        }

        // ──────────────────────────────────────────────
        //  悬浮动画
        // ──────────────────────────────────────────────

        /// <summary>播放鼠标进入（悬浮）动画</summary>
        public async void PlayHoverEnter()
        {
            if (IsSelected)
                return;
            if (hoverEnterAnimation != null)
                await hoverEnterAnimation.PlayAsync(gameObject);
        }

        /// <summary>播放鼠标离开动画</summary>
        public async void PlayHoverExit()
        {
            if (IsSelected)
                return;
            if (hoverExitAnimation != null)
                await hoverExitAnimation.PlayAsync(gameObject);
        }

        // ──────────────────────────────────────────────
        //  子类可重写的回调
        // ──────────────────────────────────────────────

        /// <summary>选中动画播放完成时回调</summary>
        protected virtual void OnSelectCompleted() { }

        /// <summary>取消选中动画播放完成时回调</summary>
        protected virtual void OnDeselectCompleted() { }

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