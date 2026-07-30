using Core;
using Core.Channels.RedDot;
using UI.Framework.Base;
using UnityEngine;

namespace UI.RedDot
{
    /// <summary>
    /// 红点视图：挂载在需要红点的按钮上，监听 redDotChannel 控制红点显隐。
    /// Inspector 中设置 redDotKey（参见 RedDotKeys），并将红点 Image 拖入 dotObject。
    /// </summary>
    public class RedDotView : UIWidget
    {
        [Header("红点设置")]
        [SerializeField, Tooltip("红点节点 Key（参见 RedDotKeys 常量类）")]
        private string redDotKey;

        [SerializeField, Tooltip("红点图片对象（通过 SetActive 控制显隐）")]
        private GameObject dotObject;

        protected override void SubscribeEvents()
        {
            EventChannelLocator.MainContainer?.redDotChannel?.RegisterListener(OnRedDotChanged);

            // 注册时同步当前状态
            if (ServiceLocator.Get<RedDotController>() != null && !string.IsNullOrEmpty(redDotKey))
                UpdateVisual(ServiceLocator.Get<RedDotController>().IsRedDotActive(redDotKey));
        }

        protected override void UnsubscribeEvents()
        {
            EventChannelLocator.MainContainer?.redDotChannel?.UnregisterListener(OnRedDotChanged);
        }

        private void OnRedDotChanged(RedDotEventData data)
        {
            if (data.Key != redDotKey)
                return;

            UpdateVisual(data.IsActive);
        }

        private void UpdateVisual(bool active)
        {
            if (dotObject != null)
                dotObject.SetActive(active);
        }

        /// <summary>
        /// 按钮 onClick 时调用，标记已读。
        /// 可通过 Button.onClick.AddListener 或 UnityEvent 绑定。
        /// </summary>
        public void OnButtonClicked()
        {
            ServiceLocator.Get<RedDotController>()?.MarkAsRead(redDotKey);
        }
    }
}
