using System;
using Domain.Character.Attribute;

namespace Domain.Enemy
{
    /// <summary>
    /// 敌人状态枚举（从 EnemyBase 中独立出来，供 EnemyData 使用）
    /// </summary>
    public enum EnemyState
    {
        Idle,
        Run,
        Attack,
        Die,
    }

    /// <summary>
    /// 敌人纯数据类
    /// 存储 EnemyBase 的可序列化参数与运行时状态，不包含 Unity 对象引用
    ///
    /// 职责：
    /// - 持有 currentState / isInitialized / attribute 等数据
    /// - 提供 ResetRuntimeState() 供对象池回收时快速重置
    ///
    /// 注意：attribute 字段带有 [NonSerialized]，不会由 Unity 序列化。
    /// 需在 Awake / OnEnable 中通过 new AttributeEnemyBase(...) 初始化。
    /// </summary>
    [Serializable]
    public class EnemyData
    {
        public EnemyState currentState = EnemyState.Idle;
        public bool isInitialized = false;

        [NonSerialized]
        public AttributeEnemyBase attribute;

        /// <summary>
        /// 构造 EnemyData 时传入 AttributeEnemyBase 实例
        /// </summary>
        public EnemyData(AttributeEnemyBase attribute)
        {
            this.attribute = attribute;
        }

        /// <summary>
        /// 无参构造（供 Unity 序列化使用），attribute 会在 Awake 中初始化
        /// </summary>
        public EnemyData()
        {
        }

        /// <summary>
        /// 重置运行时状态（对象池回收时调用）
        /// </summary>
        public void ResetRuntimeState()
        {
            currentState = EnemyState.Idle;
        }
    }
}