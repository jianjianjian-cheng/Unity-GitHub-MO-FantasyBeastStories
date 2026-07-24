-- WizardBoy

local M = {}

-- 投射物速度
local PROJECTILE_SPEED = 10

-- PerformAttack: 对应原 AttackRange_WizardBoy.PerformAttack()
-- range: C# AttackRangeBase 实例
-- target: 当前锁定目标 GameObject
function M.PerformAttack(range, target)
    if target == nil then return end

    local pos = range:GetMuzzlePosition()
    local dir = range:GetTargetDirectionPublic()
    local isTest = range.IsTest

    -- 本地生成 ImpactCannon 火球
    range:SpawnImpactCannon(pos, dir, true)

    -- 联机模式：网络广播
    if not isTest then
        range:BroadcastFireball(pos, dir, PROJECTILE_SPEED)
    end
end

return M
