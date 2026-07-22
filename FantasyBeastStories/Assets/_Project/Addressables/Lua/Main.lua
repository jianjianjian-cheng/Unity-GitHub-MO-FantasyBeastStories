-- Main.lua
-- Lua 热更新入口，require 所有模块
-- 符文数值定义由 RuneDataSO 统一管理，Lua 只负责逻辑映射

local RuneEffect = require("Logic.RuneEffect")
local RuneSpecialPower = require("Logic.RuneSpecialPower")

return {
    RuneEffect = RuneEffect,
    RuneSpecialPower = RuneSpecialPower,
}
