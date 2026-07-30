#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using XLua;
using System.Collections.Generic;


namespace XLua.CSObjectWrap
{
    using Utils = XLua.Utils;
    public class CorePoolConstWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(Core.PoolConst);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 0, 0);
			
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 26, 0, 0);
			
			
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Skeleton", Core.PoolConst.Skeleton);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Dragon", Core.PoolConst.Dragon);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ExperienceBall_Blue", Core.PoolConst.ExperienceBall_Blue);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ExperienceBall_Blue_Local", Core.PoolConst.ExperienceBall_Blue_Local);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "DamageNumPool", Core.PoolConst.DamageNumPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ImpactCannonCommonPool", Core.PoolConst.ImpactCannonCommonPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ImpactCannonLightenPool", Core.PoolConst.ImpactCannonLightenPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ImpactCannonHitCommonPool", Core.PoolConst.ImpactCannonHitCommonPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ImpactCannonHitLightenPool", Core.PoolConst.ImpactCannonHitLightenPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ImpactCannonWinterPool", Core.PoolConst.ImpactCannonWinterPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ImpactCannonHitWinterPool", Core.PoolConst.ImpactCannonHitWinterPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ImpactCannonGrassPool", Core.PoolConst.ImpactCannonGrassPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ImpactCannonHitGrassPool", Core.PoolConst.ImpactCannonHitGrassPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "TestPool", Core.PoolConst.TestPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ImpactCannonTriggerPool", Core.PoolConst.ImpactCannonTriggerPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "FireFirePool", Core.PoolConst.FireFirePool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GuiLingFirePool", Core.PoolConst.GuiLingFirePool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GuiLingLightningPool", Core.PoolConst.GuiLingLightningPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GuiLingWinterPool", Core.PoolConst.GuiLingWinterPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GuiLingGrassPool", Core.PoolConst.GuiLingGrassPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GuiLingHitFirePool", Core.PoolConst.GuiLingHitFirePool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GuiLingHitLightningPool", Core.PoolConst.GuiLingHitLightningPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GuiLingHitWinterPool", Core.PoolConst.GuiLingHitWinterPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GuiLingHitGrassPool", Core.PoolConst.GuiLingHitGrassPool);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "PowerUpItem", Core.PoolConst.PowerUpItem);
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            return LuaAPI.luaL_error(L, "Core.PoolConst does not have a constructor!");
        }
        
		
        
		
        
        
        
        
        
        
        
        
        
		
		
		
		
    }
}
