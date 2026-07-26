using System.Collections.Generic;
using Controllers.Character;

namespace Core.SharedModel
{
    /// <summary>
    /// 玩家模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 聚合已有的纯数据类：
    /// - AttributePlayerBase (HP/攻击/防御/元素/暴击...)
    /// - PlayerMovementData (速度/方向/恢复间隔...)
    /// - _unlockedElements (已解锁元素集合)
    ///
    /// 将伤害计算、生命恢复、元素切换等纯逻辑移入 Model。
    /// 外部依赖（RPC / EventChannelSO / Animator / Rigidbody）
    /// 由 Controller 处理，Model 只管理数据与计算。
    /// </summary>
    public class PlayerModel
    {
        // ──────────────────────────────────
        //  聚合的数据类
        // ──────────────────────────────────

        public AttributePlayerBase Attributes { get; private set; }
        public PlayerMovementData Movement { get; private set; }

        // ──────────────────────────────────
        //  已解锁元素
        // ──────────────────────────────────

        private readonly HashSet<Element> _unlockedElements = new();
        public IReadOnlyCollection<Element> UnlockedElements => _unlockedElements;

        public void AddUnlockedElement(Element element) => _unlockedElements.Add(element);

        // ──────────────────────────────────
        //  生命恢复计时器
        // ──────────────────────────────────

        private float _recoverTimer;

        // ──────────────────────────────────
        //  初始化
        // ──────────────────────────────────

        public PlayerModel(PlayerMovementData movementData)
        {
            Movement = movementData;
        }

        public void SetAttributes(AttributePlayerBase attributes)
        {
            Attributes = attributes;
        }

        // ──────────────────────────────────
        //  伤害计算
        // ──────────────────────────────────

        /// <summary>
        /// 计算最终伤害（扣除防御力）。
        /// </summary>
        public int CalculateFinalDamage(int baseDamage)
        {
            return baseDamage - (int)Attributes.GetDefensePower();
        }

        /// <summary>
        /// 对玩家造成伤害。
        /// 返回 DamageResult，Controller 据此决定是否刷新 UI / 发 RPC / 触发死亡。
        /// </summary>
        public PlayerDamageResult ApplyDamage(int finalDamage)
        {
            if (Attributes.GetIsDead())
                return PlayerDamageResult.AlreadyDead;

            Attributes.Damage(finalDamage);

            return new PlayerDamageResult(
                finalDamage,
                Attributes.GetCurrentHealth(),
                Attributes.GetMaxHealth(),
                Attributes.GetIsDead()
            );
        }

        // ──────────────────────────────────
        //  生命恢复
        // ──────────────────────────────────

        /// <summary>
        /// 每帧推进恢复计时器。
        /// 返回 true 表示本帧恢复了生命（Controller 需刷新 UI）。
        /// </summary>
        public bool TickHealthRecover(float deltaTime)
        {
            if (Attributes.GetIsDead()
                || Attributes.GetCurrentHealth() >= Attributes.GetMaxHealth())
                return false;

            _recoverTimer += deltaTime;
            if (_recoverTimer >= Movement.recoverInterval)
            {
                _recoverTimer = 0f;
                Attributes.AddCurrentHealth(Movement.healthRecover);
                return true;
            }

            return false;
        }

        // ──────────────────────────────────
        //  元素
        // ──────────────────────────────────

        public Element GetCurrentElement() => Attributes?.GetCurrentElement() ?? Element.Common;

        public void SetCurrentElement(Element element)
        {
            Attributes?.SetCurrentElement(element);
        }

        // ──────────────────────────────────
        //  死亡查询
        // ──────────────────────────────────

        public bool IsDead() => Attributes?.GetIsDead() ?? false;

        public int GetMaxAttackCount() => Attributes.GetMaxAttackCount();
    }

    /// <summary>玩家伤害结果，供 Controller 执行外部联动</summary>
    public struct PlayerDamageResult
    {
        public int FinalDamage;
        public int CurrentHealth;
        public int MaxHealth;
        public bool Died;
        public bool WasAlreadyDead;

        public bool IsValid => !WasAlreadyDead;

        public PlayerDamageResult(int finalDamage, float currentHealth, float maxHealth, bool died)
        {
            FinalDamage = finalDamage;
            CurrentHealth = (int)currentHealth;
            MaxHealth = (int)maxHealth;
            Died = died;
            WasAlreadyDead = false;
        }

        public static PlayerDamageResult AlreadyDead => new PlayerDamageResult
        {
            FinalDamage = 0,
            CurrentHealth = 0,
            MaxHealth = 0,
            Died = false,
            WasAlreadyDead = true
        };
    }
}
