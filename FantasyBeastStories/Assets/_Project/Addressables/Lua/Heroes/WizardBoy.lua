-- Heroes/WizardBoy.lua
-- WizardBoy 角色行为（完整迁移自 C# WizardBoy.cs）

local M = {}

local PoolHelper = CS.Core.Lua.LuaPoolHelper
local PoolConst = CS.Core.PoolConst

local ELEMENT_POOL_MAP = {
    [CS.Element.Lightning] = {
        projectile = "ImpactCannon/ImpactCannonLighten",
        hit = "ImpactCannon/ImpactCannonHitLighten",
        projPool = PoolConst.ImpactCannonLightenPool,
        hitPool = PoolConst.ImpactCannonHitLightenPool,
    },
    [CS.Element.Winter] = {
        projectile = "ImpactCannon/ImpactCannonWinter",
        hit = "ImpactCannon/ImpactCannonHitWinter",
        projPool = PoolConst.ImpactCannonWinterPool,
        hitPool = PoolConst.ImpactCannonHitWinterPool,
    },
    [CS.Element.Grass] = {
        projectile = "ImpactCannon/ImpactCannonGrass",
        hit = "ImpactCannon/ImpactCannonHitGrass",
        projPool = PoolConst.ImpactCannonGrassPool,
        hitPool = PoolConst.ImpactCannonHitGrassPool,
    },
}

-- OnStart: 注册默认对象池 + 设置卡牌类型
function M.OnStart(player)
    -- 注册 ImpactCannon 触发器池（所有元素共用）
    PoolHelper.RegisterPool(PoolConst.ImpactCannonTriggerPool, "ImpactCannon/ImpactCannonTrigger", 10)

    -- 注册 Common 元素默认池
    PoolHelper.RegisterPool(PoolConst.ImpactCannonCommonPool, "ImpactCannon/ImpactCannonCommon", 10)
    PoolHelper.RegisterPool(PoolConst.ImpactCannonHitCommonPool, "ImpactCannon/ImpactCannonHitCommon", 20)

    if player:IsLocalPlayer() then
        local mgr = CS.UI.MagicUpgradeManager.instance
        if mgr then
            mgr:SetCurrentEventName(CS.Controllers.CardData.CharacterCardType.WizardBoy)
        end
    end
end

function M.OnSkillQuery(player, data)
    if data.queryType == CS.Core.Channels.Player.SkillQueryType.GetMaxAttackCount then
        data.intValue = player:GetAttributeBase():GetMaxAttackCount()
    end
end

function M.OnSwitchElement(player, elementInt)
    local map = ELEMENT_POOL_MAP[elementInt]
    if map == nil then return end

    PoolHelper.RegisterPool(map.projPool, map.projectile, 10)
    PoolHelper.RegisterPool(map.hitPool, map.hit, 20)

    local gameSettings = CS.Core.EventChannelLocator.MainContainer.gameSettings
    if gameSettings and not gameSettings.IsTest then
        local domainRpc = CS.Controllers.Services.NetworkServiceLocator.DomainRpcService
        if domainRpc then
            domainRpc:InvokeRPC("RPC_InitElementPool", CS.Controllers.Network.NetworkTarget.Others,
                CS.Controllers.Services.NetworkServiceLocator.ObjectService:GetViewID(player.gameObject),
                elementInt)
        end
    end
end

function M.OnInitElementPool(player, elementInt)
    local map = ELEMENT_POOL_MAP[elementInt]
    if map == nil then return end

    PoolHelper.EnsurePoolCreated(map.projPool, map.projectile, 10)
    PoolHelper.EnsurePoolCreated(map.hitPool, map.hit, 20)
end

return M