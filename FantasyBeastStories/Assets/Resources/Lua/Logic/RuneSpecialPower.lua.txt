-- RuneSpecialPower.lua
-- 特殊技能逻辑映射表
-- 热更新时可新增特殊技能，无需改 C# 的 switch-case
-- 每个技能对应一个 function，参数为 AttributePlayerBase 实例（通过 xlua 绑定调用 C# 方法）

local RuneSpecialPower = {
    -- specialPowerName → function(attr)
    ["小法师专属："] = function(attr)
        attr:AddMaxAttackCount(1)
        attr:AddComboCount(1)
    end,

    -- 热更新可新增的特殊技能 ↓
    -- ["冰女专属·冰霜护盾"] = function(attr)
    --     attr:AddDefensePower(30)
    --     attr:AddMaxHealth(100)
    -- end,
    --
    -- ["火法专属·烈焰增幅"] = function(attr)
    --     attr:AddAttackPower(50)
    --     attr:AddCriticalChance(10)
    -- end,
}

return RuneSpecialPower