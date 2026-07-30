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
    public class CoreSharedModelSkillQueryDataWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(Core.SharedModel.SkillQueryData);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 5, 5);
			
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "queryType", _g_get_queryType);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "cardType", _g_get_cardType);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "intValue", _g_get_intValue);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "cardsResult", _g_get_cardsResult);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "cardResult", _g_get_cardResult);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "queryType", _s_set_queryType);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "cardType", _s_set_cardType);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "intValue", _s_set_intValue);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "cardsResult", _s_set_cardsResult);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "cardResult", _s_set_cardResult);
            
			
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
				if(LuaAPI.lua_gettop(L) == 2 && translator.Assignable<Core.SharedModel.SkillQueryType>(L, 2))
				{
					Core.SharedModel.SkillQueryType _queryType;translator.Get(L, 2, out _queryType);
					
					var gen_ret = new Core.SharedModel.SkillQueryData(_queryType);
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				if(LuaAPI.lua_gettop(L) == 3 && translator.Assignable<Core.SharedModel.SkillQueryType>(L, 2) && (LuaAPI.lua_isnil(L, 3) || LuaAPI.lua_type(L, 3) == LuaTypes.LUA_TSTRING))
				{
					Core.SharedModel.SkillQueryType _queryType;translator.Get(L, 2, out _queryType);
					string _cardType = LuaAPI.lua_tostring(L, 3);
					
					var gen_ret = new Core.SharedModel.SkillQueryData(_queryType, _cardType);
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				if(LuaAPI.lua_gettop(L) == 3 && translator.Assignable<Core.SharedModel.SkillQueryType>(L, 2) && LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3))
				{
					Core.SharedModel.SkillQueryType _queryType;translator.Get(L, 2, out _queryType);
					int _intValue = LuaAPI.xlua_tointeger(L, 3);
					
					var gen_ret = new Core.SharedModel.SkillQueryData(_queryType, _intValue);
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to Core.SharedModel.SkillQueryData constructor!");
            
        }
        
		
        
		
        
        
        
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_queryType(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                translator.PushCoreSharedModelSkillQueryType(L, gen_to_be_invoked.queryType);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_cardType(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushstring(L, gen_to_be_invoked.cardType);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_intValue(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.intValue);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_cardsResult(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.cardsResult);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_cardResult(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.cardResult);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_queryType(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                Core.SharedModel.SkillQueryType gen_value;translator.Get(L, 2, out gen_value);
				gen_to_be_invoked.queryType = gen_value;
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_cardType(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.cardType = LuaAPI.lua_tostring(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_intValue(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.intValue = LuaAPI.xlua_tointeger(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_cardsResult(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.cardsResult = (Core.SharedModel.CardConfigSO[])translator.GetObject(L, 2, typeof(Core.SharedModel.CardConfigSO[]));
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_cardResult(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                Core.SharedModel.SkillQueryData gen_to_be_invoked = (Core.SharedModel.SkillQueryData)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.cardResult = (Core.SharedModel.CardConfigSO)translator.GetObject(L, 2, typeof(Core.SharedModel.CardConfigSO));
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
