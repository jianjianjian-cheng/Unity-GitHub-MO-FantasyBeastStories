using System.Collections.Generic;
using Controllers.Character;
using Core.Channels.Player;

namespace Controllers.CardData
{
    /// <summary>
    /// 卡牌效果接口 — 每种效果以独立 [Serializable] 类实现，
    /// 通过 [SerializeReference] 内联在 CardConfigSO.Effects 列表中。
    /// </summary>
    public interface ICardEffect
    {
        void Apply(ICardEffectContext context);
    }

    /// <summary>
    /// 卡牌效果执行上下文 — 由 PlayerController 实现，
    /// 将效果所需的角色能力抽象为接口，消除效果与具体控制器的耦合。
    /// </summary>
    public interface ICardEffectContext
    {
        AttributePlayerBase Attributes { get; }
        PlayerMovementData Movement { get; }

        void SwitchElement(Element element);
        void UnlockElement(Element element);
        void RefreshHPUI();
        void RaiseSkillQuery(SkillQueryData data);
    }
}
