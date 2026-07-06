using System;
using System.Collections.Generic;
using Application;
using Domain.Event;
using UnityEngine;

namespace Domain.Enemy
{
    /// <summary>
    /// 全局怪物数量监听器
    /// 挂载到场景任意 GameObject 上，在 Inspector 中配置要监控的怪物种类（池名称）。
    /// 当怪物从对象池取出（Spawn/GetFromPoolAndActivate）时对应种类数量 +1，
    /// 当怪物回收回对象池（Despawn/ReturnToPool）时对应种类数量 -1。
    /// </summary>
    public class MonsterCountMonitor : MonoBehaviour
    {
        [Serializable]
        public class MonsterTypeEntry
        {
            [Tooltip("对象池名称，与 PoolConst / NetworkObjectPoolConst 中的常量一致")]
            public string poolName;

            [Tooltip("显示名称（仅用于 Inspector 阅读）")]
            public string displayName;

            [Tooltip("该种类最大活跃数量（0 = 无限制），生成器将据此停止生成")]
            public int maxCount;

            /// <summary>当前活跃数量（运行时自动更新）</summary>
            public int currentCount;
        }

        [Header("监控的怪物种类")]
        [SerializeField]
        private List<MonsterTypeEntry> monitoredTypes = new List<MonsterTypeEntry>();

        /// <summary>池名称 → 条目 快速查找表</summary>
        private Dictionary<string, MonsterTypeEntry> _lookup;

        private void Awake()
        {
            RebuildLookup();
            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            if (EventChannelLocator.MainContainer?.poolOperationChannel != null)
            {
                EventChannelLocator.MainContainer.poolOperationChannel.RegisterListener(OnPoolOperation);
            }
        }

        private void OnDisable()
        {
            if (EventChannelLocator.MainContainer?.poolOperationChannel != null)
            {
                EventChannelLocator.MainContainer.poolOperationChannel.UnregisterListener(OnPoolOperation);
            }
        }

        /// <summary>
        /// 重新构建查找表（Inspector 中修改配置后手动调用）
        /// </summary>
        public void RebuildLookup()
        {
            _lookup = new Dictionary<string, MonsterTypeEntry>();
            foreach (var entry in monitoredTypes)
            {
                if (string.IsNullOrEmpty(entry.poolName))
                    continue;
                if (!_lookup.ContainsKey(entry.poolName))
                {
                    _lookup.Add(entry.poolName, entry);
                }
                else
                {
                    Debug.LogWarning($"[MonsterCountMonitor] 重复的池名称: {entry.poolName}，忽略", this);
                }
            }
        }

        /// <summary>
        /// 获取指定池名称的当前活跃数量，未监控时返回 -1
        /// </summary>
        public int GetCount(string poolName)
        {
            if (_lookup != null && _lookup.TryGetValue(poolName, out var entry))
                return entry.currentCount;
            return -1;
        }

        /// <summary>
        /// 获取指定池名称的最大活跃数量上限，未监控时返回 -1
        /// </summary>
        public int GetMaxCount(string poolName)
        {
            if (_lookup != null && _lookup.TryGetValue(poolName, out var entry))
                return entry.maxCount;
            return -1;
        }

        /// <summary>
        /// 获取所有监控条目的只读列表（用于 UI 显示等）
        /// </summary>
        public IReadOnlyList<MonsterTypeEntry> MonitoredTypes => monitoredTypes;

        private void OnPoolOperation(PoolOperationData data)
        {
            if (_lookup == null || _lookup.Count == 0)
                return;

            switch (data.operationType)
            {
                case PoolOperationType.Spawn:
                case PoolOperationType.GetFromPoolAndActivate:
                    Increment(data.poolName);
                    break;

                case PoolOperationType.Despawn:
                case PoolOperationType.ReturnToPool:
                    Decrement(data.poolName);
                    break;
            }
        }

        private void Increment(string poolName)
        {
            if (_lookup.TryGetValue(poolName, out var entry))
            {
                entry.currentCount++;
            }
        }

        private void Decrement(string poolName)
        {
            if (_lookup.TryGetValue(poolName, out var entry))
            {
                entry.currentCount--;
                if (entry.currentCount < 0)
                    entry.currentCount = 0; // 防御性：防止负数
            }
        }
    }
}