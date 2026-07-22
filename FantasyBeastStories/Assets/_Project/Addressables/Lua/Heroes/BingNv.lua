-- Heroes/BingNv.lua
-- BingNv 角色行为（从 C# BingNv.cs 迁移）
-- 多元素解锁系统：BingNv 可同时拥有多种元素

local M = {}

-- C# 引用
local PoolHelper = CS.Core.Lua.LuaPoolHelper
local PoolConst = CS.Core.PoolConst
local Element = CS.Element
local NetworkServiceLocator = CS.Controllers.Services.NetworkServiceLocator
local NetworkTarget = CS.Controllers.Network.NetworkTarget

-- 各元素对应的 GuiLing 投射物 + 命中特效 Addressable 路径
local ELEMENT_POOL_MAP = {
    [CS.Element.Winter] = {
        projectile = "GuiLing/GuiLingWinter",
        hit = "GuiLing/GuiLingHitWinter",
        projPool = PoolConst.GuiLingWinterPool,
        hitPool = PoolConst.GuiLingHitWinterPool,
    },
    [CS.Element.Fire] = {
        projectile = "GuiLing/GuiLingFire",
        hit = "GuiLing/GuiLingHitFire",
        projPool = PoolConst.GuiLingFirePool,
        hitPool = PoolConst.GuiLingHitFirePool,
    },
    [CS.Element.Lightning] = {
        projectile = "GuiLing/GuiLingLightning",
        hit = "GuiLing/GuiLingHitLightning",
        projPool = PoolConst.GuiLingLightningPool,
        hitPool = PoolConst.GuiLingHitLightningPool,
    },
    [CS.Element.Grass] = {
        projectile = "GuiLing/GuiLingGrass",
        hit = "GuiLing/GuiLingHitGrass",
        projPool = PoolConst.GuiLingGrassPool,
        hitPool = PoolConst.GuiLingHitGrassPool,
    },
}

-- OnStart: 对应原 BingNv.Start()
function M.OnStart(player)
    if player:IsLocalPlayer() then
        -- 设置 MagicUpgradeManager 卡牌类型
        local mgr = CS.UI.MagicUpgradeManager.instance
        if mgr then
            mgr:SetCurrentEventName(CS.Controllers.CardData.CharacterCardType.BingNv)
        end

        -- 默认解锁 Winter（冰女初始冰霜属性）
        player:GetAttributeBase():SetCurrentElement(Element.Winter)
        M.UnlockElementInternal(player, CS.Element.Winter)
    end
end

-- OnUnlockElement: 对应原 BingNv.UnlockElement()
-- 返回 true 表示 Lua 处理了此回调
function M.OnUnlockElement(player, elementInt)
    local element = elementInt  -- int 值直接用作 table key

    -- 检查是否已解锁
    local unlocked = player:GetUnlockedElements()
    for e in unlocked do
        if e == elementInt then return true end
    end

    M.UnlockElementInternal(player, elementInt)
    return true
end

-- 内部解锁逻辑
function M.UnlockElementInternal(player, elementInt)
    -- 添加到已解锁集合
    player:AddUnlockedElement(elementInt)

    -- 注册对象池
    local map = ELEMENT_POOL_MAP[elementInt]
    if map == nil then return end

    PoolHelper.RegisterPool(map.projPool, map.projectile, 10)
    PoolHelper.RegisterPool(map.hitPool, map.hit, 20)

    -- 网络同步
    local gameSettings = CS.Core.EventChannelLocator.MainContainer.gameSettings
    if gameSettings and not gameSettings.IsTest then
        local domainRpc = NetworkServiceLocator.DomainRpcService
        if domainRpc then
            domainRpc:InvokeRPC("RPC_InitElementPool", NetworkTarget.Others,
                NetworkServiceLocator.ObjectService:GetViewID(player.gameObject),
                elementInt)
        end
    end
end

-- OnInitElementPool: 对应原 BingNv.HandleInitElementPool()
-- 在其他客户端初始化元素对象池
function M.OnInitElementPool(player, elementInt)
    local map = ELEMENT_POOL_MAP[elementInt]
    if map == nil then return end

    PoolHelper.EnsurePoolCreated(map.projPool, map.projectile, 10)
    PoolHelper.EnsurePoolCreated(map.hitPool, map.hit, 20)
end

return M