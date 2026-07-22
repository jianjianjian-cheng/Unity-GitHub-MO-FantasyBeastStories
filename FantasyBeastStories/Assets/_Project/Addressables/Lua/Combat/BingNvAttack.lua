-- Combat/BingNvAttack.lua
-- BingNv 攻击行为（从 C# AttackRange_BingNv.cs 迁移）
-- 多目标锁定 + 随机元素 + GuiLing 追踪弹

local M = {}

-- 攻击参数（原 AttackRange_BingNv 的字段）
local HORIZONTAL_SPREAD = 45
local VERTICAL_SPREAD = 20
local LAUNCH_OFFSET_Y = 0.5

-- UpdateEnemyTarget: 对应原 AttackRange_BingNv.UpdateEnemyTarget()
-- 多目标选择：按距离排序，取前 MaxTargetCount 个
function M.UpdateEnemyTarget(range)
    -- 基类已通过 GetSortedTargets 提供按距离排序的敌人列表
    -- 这里不需要额外操作，基类的 CleanupDeadEnemies 已在 GetSortedTargets 中调用
    -- Lua 只需要在 PerformAttack 时调用 range:GetSortedTargets() 即可
    return false  -- 返回 false 让基类走默认单目标逻辑，多目标在 PerformAttack 中处理
end

-- PerformAttack: 对应原 AttackRange_BingNv.PerformAttack()
function M.PerformAttack(range, target)
    local spawnPos = CS.UnityEngine.Vector3(
        range.transform.position.x,
        range.transform.position.y + LAUNCH_OFFSET_Y,
        range.transform.position.z
    )

    -- 获取按距离排序的目标列表
    local sorted = range:GetSortedTargets()
    if sorted == nil or sorted.Count == 0 then return end

    local attr = range.AttributeSource
    if attr == nil then return end

    -- MultiTargetCount: 锁定最近几个敌人
    -- MaxAttackCount: 每次发射几颗 GuiLing
    local maxTargets = attr:GetMultiTargetCount()
    local maxAttacks = attr:GetMaxAttackCount()
    local targetCount = math.min(maxTargets, sorted.Count)
    local totalShots = maxAttacks

    -- 从 AttackRangeBase 获取已解锁元素列表（避免在 Lua 中调用 C# 泛型方法）
    local unlockedElements = range:GetUnlockedElementsForLua()
    if unlockedElements == nil or unlockedElements.Count == 0 then
        -- 没有已解锁元素，使用默认 Winter
        M.FireAtTargets(range, sorted, targetCount, totalShots, spawnPos, {CS.Element.Winter})
        return
    end

    -- 将 List 转为 Lua table
    local elements = {}
    for i = 0, unlockedElements.Count - 1 do
        elements[#elements + 1] = unlockedElements[i]
    end

    M.FireAtTargets(range, sorted, targetCount, totalShots, spawnPos, elements)
end

-- 多目标分配 + 发射
-- totalShots 颗 GuiLing 分配给 targetCount 个敌人，最近的敌人权重更高
function M.FireAtTargets(range, sorted, targetCount, totalShots, spawnPos, elements)
    if targetCount <= 0 or totalShots <= 0 then return end

    -- 分配方案：totalShots 颗分配给 targetCount 个敌人
    -- 规则：最近的敌人优先多分，保证每个被分配的敌人至少 1 颗
    -- 当 totalShots >= targetCount：每个敌人至少 1 颗，多余从近到远追加
    -- 当 totalShots < targetCount：只给前 totalShots 个敌人各 1 颗
    local actualTargets = math.min(targetCount, totalShots)
    local allocation = {}
    for i = 0, actualTargets - 1 do
        allocation[i] = 1
    end

    local remaining = totalShots - actualTargets
    while remaining > 0 do
        for i = 0, actualTargets - 1 do
            if remaining <= 0 then break end
            allocation[i] = allocation[i] + 1
            remaining = remaining - 1
        end
    end

    -- 按分配方案发射 GuiLing
    for i = 0, targetCount - 1 do
        local enemy = sorted[i]
        if enemy ~= nil then
            local count = allocation[i] or 1
            for j = 1, count do
                -- 每颗 GuiLing 从已解锁元素中随机抽取一种
                local elementIdx = math.random(1, #elements)
                local element = elements[elementIdx]
                range:SpawnGuiLing(spawnPos, enemy, element, HORIZONTAL_SPREAD, VERTICAL_SPREAD)
            end
        end
    end
end

return M