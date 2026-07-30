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
    public class CoreSharedModelPlayerAttributeConfigSOWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(Core.SharedModel.PlayerAttributeConfigSO);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 15, 15);
			
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseAttackPower", _g_get_baseAttackPower);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseDefensePower", _g_get_baseDefensePower);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseMaxHealth", _g_get_baseMaxHealth);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseMoveSpeed", _g_get_baseMoveSpeed);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseCriticalMultiplier", _g_get_baseCriticalMultiplier);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseCriticalChance", _g_get_baseCriticalChance);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "maxCriticalChance", _g_get_maxCriticalChance);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "minAttackInterval", _g_get_minAttackInterval);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "maxAttackInterval", _g_get_maxAttackInterval);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseHealthRecover", _g_get_baseHealthRecover);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseAttackSpeed", _g_get_baseAttackSpeed);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseMaxAttackCount", _g_get_baseMaxAttackCount);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseComboCount", _g_get_baseComboCount);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseEmpowerCharge", _g_get_baseEmpowerCharge);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "baseMultiTargetCount", _g_get_baseMultiTargetCount);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseAttackPower", _s_set_baseAttackPower);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseDefensePower", _s_set_baseDefensePower);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseMaxHealth", _s_set_baseMaxHealth);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseMoveSpeed", _s_set_baseMoveSpeed);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseCriticalMultiplier", _s_set_baseCriticalMultiplier);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseCriticalChance", _s_set_baseCriticalChance);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "maxCriticalChance", _s_set_maxCriticalChance);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "minAttackInterval", _s_set_minAttackInterval);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "maxAttackInterval", _s_set_maxAttackInterval);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseHealthRecover", _s_set_baseHealthRecover);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseAttackSpeed", _s_set_baseAttackSpeed);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseMaxAttackCount", _s_set_baseMaxAttackCount);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseComboCount", _s_set_baseComboCount);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseEmpowerCharge", _s_set_baseEmpowerCharge);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "baseMultiTargetCount", _s_set_baseMultiTargetCount);
            
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 0, 0);
			
			
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new Core.SharedModel.PlayerAttributeConfigSO();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to Core.SharedModel.PlayerAttributeConfigSO constructor!");
            
        }
        
		
        
		
        
        
        
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseAttackPower(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.baseAttackPower);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseDefensePower(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.baseDefensePower);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseMaxHealth(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.baseMaxHealth);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseMoveSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.baseMoveSpeed);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseCriticalMultiplier(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.baseCriticalMultiplier);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseCriticalChance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.baseCriticalChance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_maxCriticalChance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.maxCriticalChance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_minAttackInterval(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.minAttackInterval);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_maxAttackInterval(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.maxAttackInterval);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseHealthRecover(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.baseHealthRecover);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseAttackSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.baseAttackSpeed);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseMaxAttackCount(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.baseMaxAttackCount);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseComboCount(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.baseComboCount);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseEmpowerCharge(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.baseEmpowerCharge);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_baseMultiTargetCount(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.baseMultiTargetCount);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseAttackPower(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseAttackPower = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseDefensePower(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseDefensePower = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseMaxHealth(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseMaxHealth = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseMoveSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseMoveSpeed = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseCriticalMultiplier(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseCriticalMultiplier = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseCriticalChance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseCriticalChance = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_maxCriticalChance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.maxCriticalChance = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_minAttackInterval(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.minAttackInterval = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_maxAttackInterval(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.maxAttackInterval = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseHealthRecover(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseHealthRecover = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseAttackSpeed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseAttackSpeed = (float)LuaAPI.lua_tonumber(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseMaxAttackCount(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseMaxAttackCount = LuaAPI.xlua_tointeger(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseComboCount(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseComboCount = LuaAPI.xlua_tointeger(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseEmpowerCharge(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseEmpowerCharge = LuaAPI.xlua_tointeger(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_baseMultiTargetCount(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.PlayerAttributeConfigSO gen_to_be_invoked = (Core.SharedModel.PlayerAttributeConfigSO)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.baseMultiTargetCount = LuaAPI.xlua_tointeger(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
