using System;
using Controllers.Character;
using Core.Channels.Player;
using UnityEngine;

namespace Controllers.CardData
{
    // ================================================================
    //  公用卡牌效果（8 种）— 覆盖全部 24 张公用卡
    // ================================================================

    [Serializable]
    public class AddAttackPowerEffect : ICardEffect
    {
        [SerializeField] private float value;
        public void Apply(ICardEffectContext context)
            => context.Attributes.AddAttackPower(value);
    }

    [Serializable]
    public class AddMaxHealthEffect : ICardEffect
    {
        [SerializeField] private float value;
        public void Apply(ICardEffectContext context)
        {
            context.Attributes.AddMaxHealth(value);
            context.RefreshHPUI();
        }
    }

    [Serializable]
    public class AddHealthRecoverEffect : ICardEffect
    {
        [SerializeField] private float value;
        public void Apply(ICardEffectContext context)
        {
            context.Movement.healthRecover += value;
            context.Attributes.SetHealthRecover(context.Movement.healthRecover);
        }
    }

    [Serializable]
    public class AddCriticalChanceEffect : ICardEffect
    {
        [SerializeField] private float value;
        public void Apply(ICardEffectContext context)
            => context.Attributes.AddCriticalChance(value);
    }

    [Serializable]
    public class AddDefensePowerEffect : ICardEffect
    {
        [SerializeField] private float value;
        public void Apply(ICardEffectContext context)
            => context.Attributes.AddDefensePower(value);
    }

    [Serializable]
    public class AddCriticalMultiplierEffect : ICardEffect
    {
        [SerializeField] private float value;
        public void Apply(ICardEffectContext context)
            => context.Attributes.AddCriticalMultiplier(value);
    }

    [Serializable]
    public class ReduceAttackIntervalEffect : ICardEffect
    {
        [SerializeField] private int value;
        public void Apply(ICardEffectContext context)
            => context.Attributes.ReduceAttackInterval(value);
    }

    [Serializable]
    public class AddLuckRateEffect : ICardEffect
    {
        [SerializeField] private int value;
        public void Apply(ICardEffectContext context)
            => context.RaiseSkillQuery(new SkillQueryData(SkillQueryType.AddLuckRate, value));
    }

    // ================================================================
    //  角色专属卡牌效果（6 种）— 覆盖 WizardBoy/BingNv 全部专属卡
    // ================================================================

    [Serializable]
    public class SwitchElementEffect : ICardEffect
    {
        [SerializeField] private Element element;
        public void Apply(ICardEffectContext context)
            => context.SwitchElement(element);
    }

    [Serializable]
    public class UnlockElementEffect : ICardEffect
    {
        [SerializeField] private Element element;
        public void Apply(ICardEffectContext context)
            => context.UnlockElement(element);
    }

    [Serializable]
    public class AddMaxAttackCountEffect : ICardEffect
    {
        [SerializeField] private int value = 1;
        public void Apply(ICardEffectContext context)
            => context.Attributes.AddMaxAttackCount(value);
    }

    [Serializable]
    public class AddComboCountEffect : ICardEffect
    {
        [SerializeField] private int value = 1;
        public void Apply(ICardEffectContext context)
            => context.Attributes.AddComboCount(value);
    }

    [Serializable]
    public class AddMultiTargetCountEffect : ICardEffect
    {
        [SerializeField] private int value = 1;
        public void Apply(ICardEffectContext context)
            => context.Attributes.AddMultiTargetCount(value);
    }

    [Serializable]
    public class SetSplitEffect : ICardEffect
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private int splitCount = 1;
        public void Apply(ICardEffectContext context)
        {
            context.Attributes.SetSplit(enabled);
            if (splitCount > 0)
                context.Attributes.AddSplitCount(splitCount);
        }
    }
}
