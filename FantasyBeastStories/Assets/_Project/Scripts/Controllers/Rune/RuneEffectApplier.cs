using System.Collections.Generic;
using System.Reflection;
using Controllers.Character;
using UnityEngine;
using XLua;

namespace Controllers.Rune
{
    /// <summary>
    /// 符文效果应用器。读取 RuneEquipmentSnapshot 中缓存的装备符文 ID，
    /// 优先从 Lua 热更新配置加载数值和效果映射，Lua 不可用时回退到 C# 硬编码。
    /// </summary>
    public static class RuneEffectApplier
    {
        // Lua 热更新模块缓存（避免每帧重复加载）
        private static LuaTable _luaMain;
        private static bool _luaLoaded;

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

            // 加载 Lua 热更新配置（仅首次成功后缓存）
            LoadLuaConfig();

            string currentCharacterType = DetectCurrentCharacterType();

            foreach (int runeId in equippedIds)
            {
                // ── 尝试从 Lua 热更新配置获取覆盖值 ──
                LuaTable luaRuneConfig = GetLuaRuneConfig(runeId);

                if (luaRuneConfig != null)
                {
                    Debug.Log($"[RuneEffectApplier] 应用符文效果(Lua热更新): ID={runeId}");
                    ApplyFromLuaConfig(attributes, luaRuneConfig, currentCharacterType);
                }
                else
                {
                    // 回退到 ScriptableObject 默认值
                    var runeData = database.GetRuneById(runeId);
                    if (runeData == null)
                    {
                        Debug.LogWarning($"[RuneEffectApplier] 未在数据库中找到符文 ID: {runeId}");
                        continue;
                    }

                    Debug.Log($"[RuneEffectApplier] 应用符文效果(默认): {runeData.runeName} (ID={runeId})");
                    ApplyFromSO(attributes, runeData, currentCharacterType);
                }
            }

            LogFinalAttributes(attributes);
        }

        // ────────────────────────────────────
        //  Lua 配置加载
        // ────────────────────────────────────

        private static void LoadLuaConfig()
        {
            if (_luaLoaded) return;

            try
            {
                var luaEnv = LuaEnvManager.Instance.LuaEnv;
                if (luaEnv == null)
                {
                    Debug.LogWarning("[RuneEffectApplier] LuaEnv 未初始化，使用 C# 默认配置");
                    return;
                }

                var result = luaEnv.DoString("return require('Main')");
                if (result != null && result.Length > 0)
                {
                    _luaMain = result[0] as LuaTable;
                    _luaLoaded = true;
                    Debug.Log("[RuneEffectApplier] Lua 符文配置加载成功");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RuneEffectApplier] Lua 加载失败，使用 C# 默认配置: {e.Message}");
            }
        }

        private static LuaTable GetLuaRuneConfig(int runeId)
        {
            if (_luaMain == null) return null;
            var runeConfig = _luaMain.Get<LuaTable>("RuneConfig");
            return runeConfig?.Get<int, LuaTable>(runeId);
        }

        // ────────────────────────────────────
        //  公共 API：供 UI 查询热更新后的有效数值
        // ────────────────────────────────────

        /// <summary>获取符文的有效属性数值（优先 Lua 热更新，回退 SO 默认值）</summary>
        public static List<RunePower> GetEffectivePowers(int runeId)
        {
            LoadLuaConfig();

            LuaTable luaConfig = GetLuaRuneConfig(runeId);
            if (luaConfig != null)
            {
                var powersTable = luaConfig.Get<LuaTable>("powers");
                int len = powersTable != null ? powersTable.Length : 0;
                if (len > 0)
                {
                    var result = new List<RunePower>(len);
                    for (int i = 1; i <= len; i++)
                    {
                        var power = powersTable.Get<int, LuaTable>(i);
                        if (power == null) continue;
                        result.Add(new RunePower
                        {
                            label = power.Get<string>("label"),
                            value = power.Get<int>("value")
                        });
                    }
                    if (result.Count > 0) return result;
                }
            }

            // 回退到 SO 默认值
            var database = Resources.Load<RuneDatabaseSO>("RuneData/RuneDatabase");
            return database?.GetRuneById(runeId)?.powers ?? new List<RunePower>();
        }

        /// <summary>获取符文的有效特殊技能名（优先 Lua，回退 SO）</summary>
        public static string GetEffectiveSpecialPowerName(int runeId)
        {
            LoadLuaConfig();

            var luaConfig = GetLuaRuneConfig(runeId);
            if (luaConfig != null)
            {
                string name = luaConfig.Get<string>("specialPowerName");
                if (!string.IsNullOrEmpty(name)) return name;
            }

            var database = Resources.Load<RuneDatabaseSO>("RuneData/RuneDatabase");
            return database?.GetRuneById(runeId)?.specialPowerName ?? string.Empty;
        }

        /// <summary>获取符文的有效特殊技能描述（优先 Lua，回退 SO）</summary>
        public static string GetEffectiveSpecialPowerDescription(int runeId)
        {
            LoadLuaConfig();

            var luaConfig = GetLuaRuneConfig(runeId);
            if (luaConfig != null)
            {
                string desc = luaConfig.Get<string>("specialPowerDescription");
                if (!string.IsNullOrEmpty(desc)) return desc;
            }

            var database = Resources.Load<RuneDatabaseSO>("RuneData/RuneDatabase");
            return database?.GetRuneById(runeId)?.specialPowerDescription ?? string.Empty;
        }

        // ────────────────────────────────────
        //  Lua 路径：从 Lua 配置应用效果
        // ────────────────────────────────────

        private static void ApplyFromLuaConfig(AttributePlayerBase attr, LuaTable luaConfig, string currentCharType)
        {
            // 1. 应用基础 powers（数值 + 效果映射均来自 Lua）
            var powersTable = luaConfig.Get<LuaTable>("powers");
            int powerLen = powersTable != null ? powersTable.Length : 0;
            for (int i = 0; i < powerLen; i++)
            {
                var power = powersTable.Get<int, LuaTable>(i + 1); // Lua 索引从 1 开始
                if (power == null) continue;

                string label = power.Get<string>("label");
                int value = power.Get<int>("value");

                ApplyPowerFromLua(attr, label, value);
            }

            // 2. 应用特殊技能
            string specialName = luaConfig.Get<string>("specialPowerName");
            if (!string.IsNullOrEmpty(specialName))
            {
                ApplySpecialPowerFromLua(attr, luaConfig, specialName, currentCharType);
            }
        }

        private static void ApplyPowerFromLua(AttributePlayerBase attr, string label, int value)
        {
            if (_luaMain == null)
            {
                // 回退到 C# switch-case
                ApplyPower(attr, new RunePower { label = label, value = value });
                return;
            }

            var effectMap = _luaMain.Get<LuaTable>("RuneEffect");
            string methodName = effectMap?.Get<string>(label);

            if (string.IsNullOrEmpty(methodName))
            {
                Debug.LogWarning($"[RuneEffectApplier] Lua 未识别的符文效果标签: '{label}'，回退到 C#");
                ApplyPower(attr, new RunePower { label = label, value = value });
                return;
            }

            // 反射调用 AttributePlayerBase 的方法
            var method = typeof(AttributePlayerBase).GetMethod(
                methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(attr, new object[] { value });
                Debug.Log($"[RuneEffectApplier] Lua映射: {label} → {methodName}({value})");
            }
            else
            {
                Debug.LogWarning($"[RuneEffectApplier] Lua 映射的方法不存在: {methodName}");
            }
        }

        private static void ApplySpecialPowerFromLua(AttributePlayerBase attr, LuaTable luaConfig,
            string specialName, string currentCharType)
        {
            // 角色匹配检查
            string exclusiveType = luaConfig.Get<string>("exclusiveCharacterType");
            if (!string.IsNullOrEmpty(exclusiveType) && exclusiveType != currentCharType)
            {
                Debug.Log($"[RuneEffectApplier] 跳过专属符文效果 [{specialName}]：角色不匹配");
                return;
            }

            if (_luaMain == null) return;

            var specialPowers = _luaMain.Get<LuaTable>("RuneSpecialPower");
            LuaFunction luaFunc = specialPowers?.Get<LuaFunction>(specialName);

            if (luaFunc != null)
            {
                luaFunc.Call(attr);
                luaFunc.Dispose();
                Debug.Log($"[RuneEffectApplier] Lua特殊技能: {specialName} 已应用");
            }
            else
            {
                Debug.LogWarning($"[RuneEffectApplier] Lua 未定义的特殊技能: '{specialName}'");
            }
        }

        // ────────────────────────────────────
        //  C# 回退路径：原有逻辑保持不变
        // ────────────────────────────────────

        private static void ApplyFromSO(AttributePlayerBase attr, RuneDataSO runeData, string currentCharType)
        {
            foreach (var power in runeData.powers)
            {
                ApplyPower(attr, power);
            }

            if (!string.IsNullOrEmpty(runeData.specialPowerDescription))
                ApplySpecialPower(attr, runeData, currentCharType);
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

        private static void ApplySpecialPower(AttributePlayerBase attr, RuneDataSO runeData, string currentCharacterType)
        {
            if (!string.IsNullOrEmpty(runeData.exclusiveCharacterType))
            {
                if (runeData.exclusiveCharacterType != currentCharacterType)
                {
                    Debug.Log($"[RuneEffectApplier] 跳过专属符文效果 [{runeData.specialPowerName}]："
                              + $"角色 {currentCharacterType} 与符文专属角色 {runeData.exclusiveCharacterType} 不匹配");
                    return;
                }
                Debug.Log($"[RuneEffectApplier] 专属符文效果匹配！角色 {currentCharacterType} ✅");
            }

            Debug.Log($"[RuneEffectApplier] 应用特殊技能: {runeData.specialPowerName} → {runeData.specialPowerDescription}");

            switch (runeData.specialPowerName)
            {
                case "小法师专属：":
                    attr.AddMaxAttackCount(1);
                    attr.AddComboCount(1);
                    Debug.Log($"[RuneEffectApplier] 初始发射数量 +1 → {attr.GetMaxAttackCount()}");
                    break;

                default:
                    Debug.LogWarning($"[RuneEffectApplier] 未处理的特殊技能: '{runeData.specialPowerDescription}'");
                    break;
            }
        }

        // ────────────────────────────────────
        //  工具方法
        // ────────────────────────────────────

        private static string DetectCurrentCharacterType()
        {
            var player = Object.FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[RuneEffectApplier] 未找到场景中的 PlayerController，无法检测角色类型");
                return string.Empty;
            }

            string typeName = player.GetType().Name;
            Debug.Log($"[RuneEffectApplier] 检测到当前角色类型: {typeName}");
            return typeName;
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