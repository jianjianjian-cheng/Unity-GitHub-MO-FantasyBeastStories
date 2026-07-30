using System;
using UnityEngine;

namespace Core.SharedModel
{
    /// <summary>
    /// 玩家移动与基础参数纯数据类
    /// 存储 PlayerController 的可序列化参数与运行时状态，不包含 Unity 对象引用
    ///
    /// 职责：
    /// - 持有移动速度 / 旋转速度 / 移动方向 / 生命恢复等数据
    /// - 供 PlayerController 及其子类使用
    /// </summary>
    [Serializable]
    public class PlayerMovementData
    {
        public float moveSpeed = 2.6f;
        public float rotationSpeed = 6f;
        public Vector3 movementDirection = Vector3.zero;
        public bool isRun = false;
        public int spawnPointIndex;
        public float healthRecover = 0f;
        public float recoverInterval = 1f;

        public PlayerMovementData() { }

        /// <summary>
        /// 重置运行时状态
        /// </summary>
        public void ResetRuntimeState()
        {
            movementDirection = Vector3.zero;
            isRun = false;
        }
    }
}