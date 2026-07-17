using System;
using UnityEngine;

namespace Controllers.Item
{
    /// <summary>
    /// 掉落物纯数据类
    /// 存储 DropItemBase 的可序列化参数与运行时状态，不包含 Unity 对象引用
    ///
    /// 职责：
    /// - 持有爆炸力 / 飞行速度 / 弹开参数等数据
    /// - 持有 isFlyingToPlayer 运行时状态
    /// </summary>
    [Serializable]
    public class DropItemData
    {
        public float explosionForce = 3f;
        public float upwardForce = 4f;
        public float lifeTime = 2f;
        public float flyToPlayerSpeed = 5f;
        public float pushBackForce = 3f;
        public float pushBackDelay = 0.2f;
        public bool isFlyingToPlayer = false;

        public DropItemData() { }

        /// <summary>
        /// 重置运行时状态
        /// </summary>
        public void ResetRuntimeState()
        {
            isFlyingToPlayer = false;
        }
    }
}