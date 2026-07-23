using System;
using UnityEngine;

namespace Controllers.Combat
{
    /// <summary>
    /// 攻击范围纯数据类
    /// 存储 AttackRangeBase 的可序列化参数与运行时状态，不包含 Unity 对象引用
    ///
    /// 职责：
    /// - 持有攻击间隔 / 搜索半径 / Y轴偏移 / 连击计数等数据
    /// - 供 AttackRangeBase 及其子类使用
    /// </summary>
    [Serializable]
    public class AttackRangeData
    {
        public float offsetY = 0.5f;
        public float attackInterval = 2f;
        public float searchRadius = 5f;

        // 运行时状态
        public float attackTimer;
        public int comboCounter = 1;
        public int empowerChargeCounter = 1;
        public bool isCharged;

        public AttackRangeData() { }

        /// <summary>
        /// 重置运行时状态
        /// </summary>
        public void ResetRuntimeState()
        {
            attackTimer = 0f;
            comboCounter = 1;
            empowerChargeCounter = 1;
            isCharged = false;
        }
    }
}