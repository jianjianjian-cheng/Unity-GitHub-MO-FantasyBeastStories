using System.Collections.Generic;
using Core;
using Core.Channels.RedDot;
using Core.Channels.UI;
using UI.Framework.Utils;
using UnityEngine;
using Managers;

namespace UI.RedDot
{
    /// <summary>
    /// 红点控制器：监听各系统事件 → 更新 Model → 通过事件通道广播变更。
    /// 外部系统（如 RuneInventory、MassionPanel）通过 Instance 调用 ActivateRedDot / MarkAsRead。
    /// </summary>
    public class RedDotController : MonoBehaviour
    {
        

        private readonly RedDotModel _model = new();

        void Awake()
        {
            ServiceLocator.Register(this);
            DontDestroyOnLoad(gameObject);

            BuildTree();
        }

        void OnEnable()
        {
            var container = EventChannelLocator.MainContainer;
            if (container != null)
            {
                container.taskUIChannel?.RegisterListener(OnTaskUIUpdate);
            }
        }

        void OnDisable()
        {
            var container = EventChannelLocator.MainContainer;
            if (container != null)
            {
                container.taskUIChannel?.UnregisterListener(OnTaskUIUpdate);
            }
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister<RedDotController>();
        }

        // ──────────────────────────────────────────────
        //  树结构注册
        // ──────────────────────────────────────────────

        private void BuildTree()
        {
            _model.RegisterChild(RedDotKeys.Root, RedDotKeys.Mission);
            _model.RegisterChild(RedDotKeys.Mission, RedDotKeys.MissionNew);

            _model.RegisterChild(RedDotKeys.Root, RedDotKeys.Rune);
            _model.RegisterChild(RedDotKeys.Rune, RedDotKeys.RuneNew);

            _model.RegisterChild(RedDotKeys.Root, RedDotKeys.Shop);
        }

        // ──────────────────────────────────────────────
        //  事件监听
        // ──────────────────────────────────────────────

        private void OnTaskUIUpdate(TaskUIUpdateData data)
        {
            if (data.eventType == TaskUIEventType.ShowNotice)
                ActivateRedDot(RedDotKeys.MissionNew);
        }

        // ──────────────────────────────────────────────
        //  公共 API
        // ──────────────────────────────────────────────

        /// <summary>
        /// 激活指定红点节点。
        /// 外部系统调用（如 RuneInventory.AddRune → ActivateRedDot(RuneNew)）。
        /// </summary>
        public void ActivateRedDot(string key)
        {
            var changed = _model.SetActive(key, true);
            BroadcastChanges(changed, true);
        }

        /// <summary>
        /// 标记红点为已读（关闭）。
        /// 面板打开时调用（如 MassionPanel.OnAfterClose → MarkAsRead(MissionNew)）。
        /// </summary>
        public void MarkAsRead(string key)
        {
            var changed = _model.SetActive(key, false);
            BroadcastChanges(changed, false);
        }

        /// <summary>查询节点是否激活（调试用）</summary>
        public bool IsRedDotActive(string key) => _model.IsActive(key);

        // ──────────────────────────────────────────────
        //  内部
        // ──────────────────────────────────────────────

        private void BroadcastChanges(List<string> changedKeys, bool active)
        {
            if (changedKeys == null || changedKeys.Count == 0)
                return;

            var channel = EventChannelLocator.MainContainer?.redDotChannel;
            if (channel == null)
            {
                Debug.LogWarning("[RedDotController] redDotChannel 未配置，无法广播红点变更");
                return;
            }

            foreach (var key in changedKeys)
            {
                // 重新查询每个节点的实际状态（聚合节点可能为 false）
                bool actualActive = _model.IsActive(key);
                channel.Raise(new RedDotEventData(key, actualActive));
            }
        }
    }
}
