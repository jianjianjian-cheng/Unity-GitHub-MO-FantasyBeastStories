using Domain.Character.Attribute;
using UnityEngine;

namespace Domain.Rune
{
    /// <summary>
    /// 符文效果应用器。读取 RuneEquipmentSnapshot 中缓存的装备符文 ID，
    /// 从 RuneDatabase 获取 RuneDataSO，将其 powers 映射到 AttributePlayerBase 的属性修改。
    /// </summary>
    public static class RuneEffectApplier
    {
        /// <summary>将已装备符文的效果应用到玩家属性上</summary>
        public static void ApplyEquippedRunes(AttributePlayerBase attributes)
        {
            var database = Resources.Load<RuneDatabaseSO>("RuneData/RuneDatabase");
            if (database == null || database.allRunes == null || database.allRunes.Count == 0)
            {
                Debug.LogWarning("[RuneEffectApplier] 未找到 RuneDatabase 或数据为空");
                return;
            }

            int[] equippedIds = RuneEquipmentSnapshot.GetAllEquippedIds();
            if (equippedIds.Length == 0)
            {
                Debug.Log("[RuneEffectApplier] 未装备任何符文");
                return;
            }

            foreach (int runeId in equippedIds)
            {
                var runeData = database.GetRuneById(runeId);
                if (runeData == null)
                {
                    Debug.LogWarning($"[RuneEffectApplier] 未在数据库中找到符文 ID: {runeId}");
                    continue;
                }

                Debug.Log($"[RuneEffectApplier] 应用符文效果: {runeData.runeName} (ID={runeId})");

                foreach (var power in runeData.powers)
                {
                    ApplyPower(attributes, power);
                }

                // ── 应用特殊技能（如专属符文的专属效果） ──
                if (!string.IsNullOrEmpty(runeData.specialPowerDescription))
                    ApplySpecialPower(attributes, runeData);
            }

            LogFinalAttributes(attributes);
        }

        private static void ApplyPower(AttributePlayerBase attr, RunePower power)
        {
            switch (power.label)
            {
                case "%基础伤害":
                    attr.AddAttackPower(power.value);
                    Debug.Log($"[RuneEffectApplier] 攻击力 +{power.value}% → {attr.GetAttackPower()}");
                    break;

                case "%暴击率":
                    attr.AddCriticalChance(power.value);
                    Debug.Log($"[RuneEffectApplier] 暴击率 +{power.value}% → {attr.GetCriticalChance() * 100:F1}%");
                    break;

                case "%防御力":
                    attr.AddDefensePower(power.value);
                    Debug.Log($"[RuneEffectApplier] 防御力 +{power.value}% → {attr.GetDefensePower()}");
                    break;

                case "%攻击速度":
                    attr.ReduceAttackInterval(power.value);
                    Debug.Log($"[RuneEffectApplier] 攻击速度 +{power.value} → {attr.GetAttackSpeed()}");
                    break;

                default:
                    Debug.LogWarning($"[RuneEffectApplier] 未识别的符文效果标签: '{power.label}'");
                    break;
            }
        }

        private static void ApplySpecialPower(AttributePlayerBase attr, RuneDataSO runeData)
        {
            Debug.Log($"[RuneEffectApplier] 应用特殊技能: {runeData.specialPowerName} → {runeData.specialPowerDescription}");

            switch (runeData.specialPowerName)
            {
                case "小法师专属：":
                    attr.AddMaxAttackCount(1);
                    attr.AddComboCount(1); // 同步增加连击计数上限
                    Debug.Log($"[RuneEffectApplier] 初始发射数量 +1 → {attr.GetMaxAttackCount()}");
                    break;

                // 后续添加更多专属技能 ...

                default:
                    Debug.LogWarning($"[RuneEffectApplier] 未处理的特殊技能: '{runeData.specialPowerDescription}'");
                    break;
            }
        }

        private static void LogFinalAttributes(AttributePlayerBase attr)
        {
            Debug.Log($"[RuneEffectApplier] ── 符文应用后最终属性 ──\n" +
                      $"攻击力: {attr.GetAttackPower()}\n" +
                      $"防御力: {attr.GetDefensePower()}\n" +
                      $"暴击率: {attr.GetCriticalChance() * 100:F1}%\n" +
                      $"攻击间隔: {attr.GetAttackInterval()}\n" +
                      $"移动速度: {attr.GetMoveSpeed()}");
        }

    }
}