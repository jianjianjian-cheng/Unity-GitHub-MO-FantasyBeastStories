using System.Collections;
using UI.Framework.Animation;
using UnityEngine;

namespace UI.Framework.Base
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

        private Coroutine _selectCoroutine;
        private Coroutine _deselectCoroutine;
        private Coroutine _hoverEnterCoroutine;
        private Coroutine _hoverExitCoroutine;

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
        public void PlaySelect()
        {
            IsSelected = true;
            if (_deselectCoroutine != null)
                StopCoroutine(_deselectCoroutine);

            if (selectAnimation != null)
                _selectCoroutine = StartCoroutine(PlayAnimationCoroutine(selectAnimation));
        }

        /// <summary>播放取消选中动画，同时将 IsSelected 设为 false</summary>
        public void PlayDeselect()
        {
            IsSelected = false;
            if (_selectCoroutine != null)
                StopCoroutine(_selectCoroutine);

            if (deselectAnimation != null)
                _deselectCoroutine = StartCoroutine(PlayAnimationCoroutine(deselectAnimation));
        }

        // ──────────────────────────────────────────────
        //  悬浮动画
        // ──────────────────────────────────────────────

        /// <summary>播放鼠标进入（悬浮）动画</summary>
        public void PlayHoverEnter()
        {
            if (IsSelected)
                return;
            if (_hoverExitCoroutine != null)
                StopCoroutine(_hoverExitCoroutine);

            if (hoverEnterAnimation != null)
                _hoverEnterCoroutine = StartCoroutine(PlayAnimationCoroutine(hoverEnterAnimation));
        }

        /// <summary>播放鼠标离开动画</summary>
        public void PlayHoverExit()
        {
            if (IsSelected)
                return;
            if (_hoverEnterCoroutine != null)
                StopCoroutine(_hoverEnterCoroutine);

            if (hoverExitAnimation != null)
                _hoverExitCoroutine = StartCoroutine(PlayAnimationCoroutine(hoverExitAnimation));
        }

        // ──────────────────────────────────────────────
        //  内部协程
        // ──────────────────────────────────────────────

        private IEnumerator PlayAnimationCoroutine(UIAnimationBase animation)
        {
            yield return animation.PlayCoroutine(gameObject);
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
