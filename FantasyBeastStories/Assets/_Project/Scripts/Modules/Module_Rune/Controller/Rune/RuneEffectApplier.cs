using System.Collections.Generic;
using System.Reflection;
using Controllers.Character;
using Core;
using UnityEngine;
using XLua;

namespace Controllers.Rune
{
    /// <summary>
    /// 符文效果应用器。读取 RuneEquipmentSnapshot 中缓存的装备符文 ID，
    /// 从 RuneDataSO 获取数值定义，从 Lua 获取效果映射和特殊技能逻辑（可热更）。
    /// </summary>
    public static class RuneEffectApplier
    {
        // Lua 热更新模块缓存（避免每帧重复加载）
        private static LuaTable _luaMain;
        private static bool _luaLoaded;

        // RuneDatabase 缓存（通过 Addressables 加载，确保使用热更后的数据）
        private static RuneDatabaseSO _database;
        private static bool _databaseLoaded;

        /// <summary>获取 RuneDatabase（通过 Addressables 加载，确保热更生效）</summary>
        private static RuneDatabaseSO GetDatabase()
        {
            if (_databaseLoaded) return _database;
            _database = AssetLoader.TryLoadAsset<RuneDatabaseSO>("Lobby_RuneData_RuneDatabase");
            _databaseLoaded = true;
            if (_database != null)
            {
                Debug.Log($"[RuneEffectApplier] 通过 Addressables 加载 RuneDatabase 成功，符文数: {_database.allRunes.Count}");
                foreach (var rune in _database.allRunes)
                {
                    string powers = string.Join(", ", rune.powers.ConvertAll(p => $"{p.label}={p.value}"));
                    Debug.Log($"[RuneEffectApplier]   ID={rune.runeId} {rune.runeName} | powers: {powers} | special: {rune.specialPowerName}");
                }
            }
            else
            {
                Debug.LogError("[RuneEffectApplier] 通过 Addressables 加载 RuneDatabase 失败！");
            }
            return _database;
        }

        /// <summary>将已装备符文的效果应用到玩家属性上</summary>
        public static void ApplyEquippedRunes(AttributePlayerBase attributes)
        {
            var database = GetDatabase();
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
                var runeData = database.GetRuneById(runeId);
                if (runeData == null)
                {
                    Debug.LogWarning($"[RuneEffectApplier] 未在数据库中找到符文 ID: {runeId}");
                    continue;
                }

                Debug.Log($"[RuneEffectApplier] 应用符文效果: {runeData.runeName} (ID={runeId})");
                ApplyFromSO(attributes, runeData, currentCharacterType);
            }

            LogFinalAttributes(attributes);
        }

        // ────────────────────────────────────
        //  Lua 配置加载（仅加载效果映射和特殊技能逻辑）
        // ────────────────────────────────────

        private static void LoadLuaConfig()
        {
            if (_luaLoaded) return;

            try
            {
                var luaEnv = LuaEnvManager.Instance.LuaEnv;
                if (luaEnv == null)
                {
                    Debug.LogWarning("[RuneEffectApplier] LuaEnv 未初始化，效果映射将使用 C# 回退");
                    return;
                }

                var result = luaEnv.DoString("return require('Main')");
                if (result != null && result.Length > 0)
                {
                    _luaMain = result[0] as LuaTable;
                    _luaLoaded = true;
                    Debug.Log("[RuneEffectApplier] Lua 效果映射配置加载成功");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RuneEffectApplier] Lua 加载失败，效果映射将使用 C# 回退: {e.Message}");
            }
        }

        // ────────────────────────────────────
        //  公共 API：供 UI 查询符文数值（直接从 SO 读取）
        // ────────────────────────────────────

        /// <summary>获取符文的属性数值</summary>
        public static List<RunePower> GetEffectivePowers(int runeId)
        {
            var database = GetDatabase();
            return database?.GetRuneById(runeId)?.powers ?? new List<RunePower>();
        }

        /// <summary>获取符文的特殊技能名</summary>
        public static string GetEffectiveSpecialPowerName(int runeId)
        {
            var database = GetDatabase();
            return database?.GetRuneById(runeId)?.specialPowerName ?? string.Empty;
        }

        /// <summary>获取符文的特殊技能描述</summary>
        public static string GetEffectiveSpecialPowerDescription(int runeId)
        {
            var database = GetDatabase();
            return database?.GetRuneById(runeId)?.specialPowerDescription ?? string.Empty;
        }

        // ────────────────────────────────────
        //  效果应用：从 SO 读数值，从 Lua 读效果映射
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
            // 优先从 Lua 效果映射表查找方法名
            if (_luaMain != null)
            {
                var effectMap = _luaMain.Get<LuaTable>("RuneEffect");
                string methodName = effectMap?.Get<string>(power.label);

                if (!string.IsNullOrEmpty(methodName))
                {
                    var method = typeof(AttributePlayerBase).GetMethod(
                        methodName, BindingFlags.Public | BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(attr, new object[] { power.value });
                        Debug.Log($"[RuneEffectApplier] Lua映射: {power.label} → {methodName}({power.value})");
                        return;
                    }
                    Debug.LogWarning($"[RuneEffectApplier] Lua 映射的方法不存在: {methodName}，回退到 C#");
                }
            }

            // C# 回退
            ApplyPowerFallback(attr, power);
        }

        private static void ApplyPowerFallback(AttributePlayerBase attr, RunePower power)
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

            // 优先从 Lua 特殊技能表查找函数
            if (_luaMain != null)
            {
                var specialPowers = _luaMain.Get<LuaTable>("RuneSpecialPower");
                LuaFunction luaFunc = specialPowers?.Get<LuaFunction>(runeData.specialPowerName);

                if (luaFunc != null)
                {
                    luaFunc.Call(attr);
                    luaFunc.Dispose();
                    Debug.Log($"[RuneEffectApplier] Lua特殊技能: {runeData.specialPowerName} 已应用");
                    return;
                }
            }

            // C# 回退
            ApplySpecialPowerFallback(attr, runeData);
        }

        private static void ApplySpecialPowerFallback(AttributePlayerBase attr, RuneDataSO runeData)
        {
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

            string typeName = player.GetCharacterName();
            if (string.IsNullOrEmpty(typeName))
                typeName = player.GetType().Name;

            // 去掉 "Root" 后缀，使 "WizardBoyRoot" → "WizardBoy"，与 RuneDataSO.exclusiveCharacterType 匹配
            if (typeName.EndsWith("Root"))
                typeName = typeName.Substring(0, typeName.Length - 4);

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
