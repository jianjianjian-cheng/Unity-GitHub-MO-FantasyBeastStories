using System;
using Controllers.Character;
using Controllers.Enemy;

namespace Core.SharedModel
{
    /// <summary>
    /// 敌人模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有：
    /// - 状态机 (EnemyState + 状态切换回调)
    /// - 属性引用 (AttributeEnemyBase)
    /// - 初始化标记
    ///
    /// 外部依赖（Animator / Rigidbody / Collider / NavMeshAgent / NetworkServiceLocator）
    /// 由 Controller 处理，Model 只管理数据与状态逻辑。
    ///
    /// 设计说明：
    /// - TransitionToState() 通过 C# event 通知 Controller 执行物理操作
    /// - TakeDamage() 返回 DamageResult，Controller 据此决定外部联动
    /// - ResetState() 只重置数据，物理重置由 Controller 调用 View 完成
    /// </summary>
    public class EnemyModel
    {
        // ──────────────────────────────────
        //  状态机
        // ──────────────────────────────────

        public EnemyState CurrentState { get; private set; }

        /// <summary>状态切换回调，Controller 订阅以执行物理操作</summary>
        public event Action<EnemyState, EnemyState> OnStateChanged;

        // ──────────────────────────────────
        //  属性
        // ──────────────────────────────────

        public AttributeEnemyBase Attribute { get; private set; }
        public bool IsInitialized { get; private set; }

        // ──────────────────────────────────
        //  初始化
        // ──────────────────────────────────

        public EnemyModel(AttributeEnemyBase attribute)
        {
            Attribute = attribute;
            CurrentState = EnemyState.Idle;
        }

        public void Initialize()
        {
            if (!IsInitialized)
                IsInitialized = true;
            SetState(EnemyState.Idle);
        }

        // ──────────────────────────────────
        //  状态切换
        // ──────────────────────────────────

        /// <summary>
        /// 切换状态。触发 OnStateChanged 回调，Controller 据此执行
        /// ExitOldState 物理操作 → 更新状态 → EnterNewState 物理操作。
        /// </summary>
        public void SetState(EnemyState newState)
        {
            if (CurrentState == newState)
                return;

            EnemyState oldState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
        }

        // ──────────────────────────────────
        //  伤害处理
        // ──────────────────────────────────

        /// <summary>
        /// 对敌人造成伤害。
        /// 返回 DamageResult，包含最终伤害值和是否死亡。
        /// Controller 据此决定外部联动（统计、掉落、状态切换）。
        /// </summary>
        public DamageResult ApplyDamage(float finalDamage, Element element)
        {
            if (Attribute.GetIsDie())
                return DamageResult.AlreadyDead;

            Attribute.TakeDamage(finalDamage);
            Attribute.TakeDamageSpecial(element);

            bool died = Attribute.GetIsDie();
            return new DamageResult(finalDamage, died);
        }

        // ──────────────────────────────────
        //  死亡查询
        // ──────────────────────────────────

        public bool GetIsDie() => Attribute.GetIsDie();

        /// <summary>
        /// 判断敌人是否真正死亡（对所有客户端生效）。
        /// 与 GetIsDie() 的区别：也会检查状态机是否在 Die 状态。
        /// </summary>
        public bool IsDeadOrDying() => GetIsDie() || CurrentState == EnemyState.Die;

        // ──────────────────────────────────
        //  重置（对象池回收时调用）
        // ──────────────────────────────────

        /// <summary>重置数据层状态（属性、状态机、初始化标记）</summary>
        public void ResetModel()
        {
            CurrentState = EnemyState.Idle;
            Attribute?.ResetAttribute();
        }

        /// <summary>对象池取出后重新设置属性（可能因 NonSerialized 丢失）</summary>
        public void EnsureAttribute(AttributeEnemyBase attribute)
        {
            if (Attribute == null)
                Attribute = attribute;
        }
    }

    /// <summary>伤害结果，供 Controller 执行外部联动</summary>
    public struct DamageResult
    {
        public float FinalDamage;
        public bool Died;
        public bool WasAlreadyDead;

        public bool IsValid => !WasAlreadyDead;

        public DamageResult(float finalDamage, bool died)
        {
            FinalDamage = finalDamage;
            Died = died;
            WasAlreadyDead = false;
        }

        public static DamageResult AlreadyDead => new DamageResult
        {
            FinalDamage = 0,
            Died = false,
            WasAlreadyDead = true
        };
    }
}
