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
    
    public class CoreSharedModelElementWrap
    {
		public static void __Register(RealStatePtr L)
        {
		    ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
		    Utils.BeginObjectRegister(typeof(Core.SharedModel.Element), L, translator, 0, 0, 0, 0);
			Utils.EndObjectRegister(typeof(Core.SharedModel.Element), L, translator, null, null, null, null, null);
			
			Utils.BeginClassRegister(typeof(Core.SharedModel.Element), L, null, 6, 0, 0);

            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Common", Core.SharedModel.Element.Common);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Lightning", Core.SharedModel.Element.Lightning);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Winter", Core.SharedModel.Element.Winter);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Grass", Core.SharedModel.Element.Grass);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Fire", Core.SharedModel.Element.Fire);
            

			Utils.RegisterFunc(L, Utils.CLS_IDX, "__CastFrom", __CastFrom);
            
            Utils.EndClassRegister(typeof(Core.SharedModel.Element), L, translator);
        }
		
		[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CastFrom(RealStatePtr L)
		{
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			LuaTypes lua_type = LuaAPI.lua_type(L, 1);
            if (lua_type == LuaTypes.LUA_TNUMBER)
            {
                translator.PushCoreSharedModelElement(L, (Core.SharedModel.Element)LuaAPI.xlua_tointeger(L, 1));
            }
			
            else if(lua_type == LuaTypes.LUA_TSTRING)
            {

			    if (LuaAPI.xlua_is_eq_str(L, 1, "Common"))
                {
                    translator.PushCoreSharedModelElement(L, Core.SharedModel.Element.Common);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "Lightning"))
                {
                    translator.PushCoreSharedModelElement(L, Core.SharedModel.Element.Lightning);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "Winter"))
                {
                    translator.PushCoreSharedModelElement(L, Core.SharedModel.Element.Winter);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "Grass"))
                {
                    translator.PushCoreSharedModelElement(L, Core.SharedModel.Element.Grass);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "Fire"))
                {
                    translator.PushCoreSharedModelElement(L, Core.SharedModel.Element.Fire);
                }
				else
                {
                    return LuaAPI.luaL_error(L, "invalid string for Core.SharedModel.Element!");
                }

            }
			
            else
            {
                return LuaAPI.luaL_error(L, "invalid lua type for Core.SharedModel.Element! Expect number or string, got + " + lua_type);
            }

            return 1;
		}
	}
    
    public class CoreSharedModelEnemyStateWrap
    {
		public static void __Register(RealStatePtr L)
        {
		    ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
		    Utils.BeginObjectRegister(typeof(Core.SharedModel.EnemyState), L, translator, 0, 0, 0, 0);
			Utils.EndObjectRegister(typeof(Core.SharedModel.EnemyState), L, translator, null, null, null, null, null);
			
			Utils.BeginClassRegister(typeof(Core.SharedModel.EnemyState), L, null, 5, 0, 0);

            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Idle", Core.SharedModel.EnemyState.Idle);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Run", Core.SharedModel.EnemyState.Run);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Attack", Core.SharedModel.EnemyState.Attack);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Die", Core.SharedModel.EnemyState.Die);
            

			Utils.RegisterFunc(L, Utils.CLS_IDX, "__CastFrom", __CastFrom);
            
            Utils.EndClassRegister(typeof(Core.SharedModel.EnemyState), L, translator);
        }
		
		[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CastFrom(RealStatePtr L)
		{
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			LuaTypes lua_type = LuaAPI.lua_type(L, 1);
            if (lua_type == LuaTypes.LUA_TNUMBER)
            {
                translator.PushCoreSharedModelEnemyState(L, (Core.SharedModel.EnemyState)LuaAPI.xlua_tointeger(L, 1));
            }
			
            else if(lua_type == LuaTypes.LUA_TSTRING)
            {

			    if (LuaAPI.xlua_is_eq_str(L, 1, "Idle"))
                {
                    translator.PushCoreSharedModelEnemyState(L, Core.SharedModel.EnemyState.Idle);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "Run"))
                {
                    translator.PushCoreSharedModelEnemyState(L, Core.SharedModel.EnemyState.Run);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "Attack"))
                {
                    translator.PushCoreSharedModelEnemyState(L, Core.SharedModel.EnemyState.Attack);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "Die"))
                {
                    translator.PushCoreSharedModelEnemyState(L, Core.SharedModel.EnemyState.Die);
                }
				else
                {
                    return LuaAPI.luaL_error(L, "invalid string for Core.SharedModel.EnemyState!");
                }

            }
			
            else
            {
                return LuaAPI.luaL_error(L, "invalid lua type for Core.SharedModel.EnemyState! Expect number or string, got + " + lua_type);
            }

            return 1;
		}
	}
    
    public class CoreSharedModelNetworkTargetWrap
    {
		public static void __Register(RealStatePtr L)
        {
		    ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
		    Utils.BeginObjectRegister(typeof(Core.SharedModel.NetworkTarget), L, translator, 0, 0, 0, 0);
			Utils.EndObjectRegister(typeof(Core.SharedModel.NetworkTarget), L, translator, null, null, null, null, null);
			
			Utils.BeginClassRegister(typeof(Core.SharedModel.NetworkTarget), L, null, 5, 0, 0);

            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "All", Core.SharedModel.NetworkTarget.All);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "Others", Core.SharedModel.NetworkTarget.Others);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "MasterClient", Core.SharedModel.NetworkTarget.MasterClient);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "AllBuffered", Core.SharedModel.NetworkTarget.AllBuffered);
            

			Utils.RegisterFunc(L, Utils.CLS_IDX, "__CastFrom", __CastFrom);
            
            Utils.EndClassRegister(typeof(Core.SharedModel.NetworkTarget), L, translator);
        }
		
		[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CastFrom(RealStatePtr L)
		{
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			LuaTypes lua_type = LuaAPI.lua_type(L, 1);
            if (lua_type == LuaTypes.LUA_TNUMBER)
            {
                translator.PushCoreSharedModelNetworkTarget(L, (Core.SharedModel.NetworkTarget)LuaAPI.xlua_tointeger(L, 1));
            }
			
            else if(lua_type == LuaTypes.LUA_TSTRING)
            {

			    if (LuaAPI.xlua_is_eq_str(L, 1, "All"))
                {
                    translator.PushCoreSharedModelNetworkTarget(L, Core.SharedModel.NetworkTarget.All);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "Others"))
                {
                    translator.PushCoreSharedModelNetworkTarget(L, Core.SharedModel.NetworkTarget.Others);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "MasterClient"))
                {
                    translator.PushCoreSharedModelNetworkTarget(L, Core.SharedModel.NetworkTarget.MasterClient);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "AllBuffered"))
                {
                    translator.PushCoreSharedModelNetworkTarget(L, Core.SharedModel.NetworkTarget.AllBuffered);
                }
				else
                {
                    return LuaAPI.luaL_error(L, "invalid string for Core.SharedModel.NetworkTarget!");
                }

            }
			
            else
            {
                return LuaAPI.luaL_error(L, "invalid lua type for Core.SharedModel.NetworkTarget! Expect number or string, got + " + lua_type);
            }

            return 1;
		}
	}
    
    public class CoreSharedModelSkillQueryTypeWrap
    {
		public static void __Register(RealStatePtr L)
        {
		    ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
		    Utils.BeginObjectRegister(typeof(Core.SharedModel.SkillQueryType), L, translator, 0, 0, 0, 0);
			Utils.EndObjectRegister(typeof(Core.SharedModel.SkillQueryType), L, translator, null, null, null, null, null);
			
			Utils.BeginClassRegister(typeof(Core.SharedModel.SkillQueryType), L, null, 8, 0, 0);

            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GetMaxAttackCount", Core.SharedModel.SkillQueryType.GetMaxAttackCount);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GetLuckRate", Core.SharedModel.SkillQueryType.GetLuckRate);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GetRandomEXCard", Core.SharedModel.SkillQueryType.GetRandomEXCard);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GetThreeRandomEXCards", Core.SharedModel.SkillQueryType.GetThreeRandomEXCards);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GetThreeRandomCards", Core.SharedModel.SkillQueryType.GetThreeRandomCards);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "AddLuckRate", Core.SharedModel.SkillQueryType.AddLuckRate);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "GetUpgradeExperience", Core.SharedModel.SkillQueryType.GetUpgradeExperience);
            

			Utils.RegisterFunc(L, Utils.CLS_IDX, "__CastFrom", __CastFrom);
            
            Utils.EndClassRegister(typeof(Core.SharedModel.SkillQueryType), L, translator);
        }
		
		[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CastFrom(RealStatePtr L)
		{
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			LuaTypes lua_type = LuaAPI.lua_type(L, 1);
            if (lua_type == LuaTypes.LUA_TNUMBER)
            {
                translator.PushCoreSharedModelSkillQueryType(L, (Core.SharedModel.SkillQueryType)LuaAPI.xlua_tointeger(L, 1));
            }
			
            else if(lua_type == LuaTypes.LUA_TSTRING)
            {

			    if (LuaAPI.xlua_is_eq_str(L, 1, "GetMaxAttackCount"))
                {
                    translator.PushCoreSharedModelSkillQueryType(L, Core.SharedModel.SkillQueryType.GetMaxAttackCount);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "GetLuckRate"))
                {
                    translator.PushCoreSharedModelSkillQueryType(L, Core.SharedModel.SkillQueryType.GetLuckRate);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "GetRandomEXCard"))
                {
                    translator.PushCoreSharedModelSkillQueryType(L, Core.SharedModel.SkillQueryType.GetRandomEXCard);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "GetThreeRandomEXCards"))
                {
                    translator.PushCoreSharedModelSkillQueryType(L, Core.SharedModel.SkillQueryType.GetThreeRandomEXCards);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "GetThreeRandomCards"))
                {
                    translator.PushCoreSharedModelSkillQueryType(L, Core.SharedModel.SkillQueryType.GetThreeRandomCards);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "AddLuckRate"))
                {
                    translator.PushCoreSharedModelSkillQueryType(L, Core.SharedModel.SkillQueryType.AddLuckRate);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "GetUpgradeExperience"))
                {
                    translator.PushCoreSharedModelSkillQueryType(L, Core.SharedModel.SkillQueryType.GetUpgradeExperience);
                }
				else
                {
                    return LuaAPI.luaL_error(L, "invalid string for Core.SharedModel.SkillQueryType!");
                }

            }
			
            else
            {
                return LuaAPI.luaL_error(L, "invalid lua type for Core.SharedModel.SkillQueryType! Expect number or string, got + " + lua_type);
            }

            return 1;
		}
	}
    
    public class TutorialTestEnumWrap
    {
		public static void __Register(RealStatePtr L)
        {
		    ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
		    Utils.BeginObjectRegister(typeof(Tutorial.TestEnum), L, translator, 0, 0, 0, 0);
			Utils.EndObjectRegister(typeof(Tutorial.TestEnum), L, translator, null, null, null, null, null);
			
			Utils.BeginClassRegister(typeof(Tutorial.TestEnum), L, null, 3, 0, 0);

            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "E1", Tutorial.TestEnum.E1);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "E2", Tutorial.TestEnum.E2);
            

			Utils.RegisterFunc(L, Utils.CLS_IDX, "__CastFrom", __CastFrom);
            
            Utils.EndClassRegister(typeof(Tutorial.TestEnum), L, translator);
        }
		
		[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CastFrom(RealStatePtr L)
		{
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			LuaTypes lua_type = LuaAPI.lua_type(L, 1);
            if (lua_type == LuaTypes.LUA_TNUMBER)
            {
                translator.PushTutorialTestEnum(L, (Tutorial.TestEnum)LuaAPI.xlua_tointeger(L, 1));
            }
			
            else if(lua_type == LuaTypes.LUA_TSTRING)
            {

			    if (LuaAPI.xlua_is_eq_str(L, 1, "E1"))
                {
                    translator.PushTutorialTestEnum(L, Tutorial.TestEnum.E1);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "E2"))
                {
                    translator.PushTutorialTestEnum(L, Tutorial.TestEnum.E2);
                }
				else
                {
                    return LuaAPI.luaL_error(L, "invalid string for Tutorial.TestEnum!");
                }

            }
			
            else
            {
                return LuaAPI.luaL_error(L, "invalid lua type for Tutorial.TestEnum! Expect number or string, got + " + lua_type);
            }

            return 1;
		}
	}
    
    public class XLuaTestMyEnumWrap
    {
		public static void __Register(RealStatePtr L)
        {
		    ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
		    Utils.BeginObjectRegister(typeof(XLuaTest.MyEnum), L, translator, 0, 0, 0, 0);
			Utils.EndObjectRegister(typeof(XLuaTest.MyEnum), L, translator, null, null, null, null, null);
			
			Utils.BeginClassRegister(typeof(XLuaTest.MyEnum), L, null, 3, 0, 0);

            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "E1", XLuaTest.MyEnum.E1);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "E2", XLuaTest.MyEnum.E2);
            

			Utils.RegisterFunc(L, Utils.CLS_IDX, "__CastFrom", __CastFrom);
            
            Utils.EndClassRegister(typeof(XLuaTest.MyEnum), L, translator);
        }
		
		[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CastFrom(RealStatePtr L)
		{
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			LuaTypes lua_type = LuaAPI.lua_type(L, 1);
            if (lua_type == LuaTypes.LUA_TNUMBER)
            {
                translator.PushXLuaTestMyEnum(L, (XLuaTest.MyEnum)LuaAPI.xlua_tointeger(L, 1));
            }
			
            else if(lua_type == LuaTypes.LUA_TSTRING)
            {

			    if (LuaAPI.xlua_is_eq_str(L, 1, "E1"))
                {
                    translator.PushXLuaTestMyEnum(L, XLuaTest.MyEnum.E1);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "E2"))
                {
                    translator.PushXLuaTestMyEnum(L, XLuaTest.MyEnum.E2);
                }
				else
                {
                    return LuaAPI.luaL_error(L, "invalid string for XLuaTest.MyEnum!");
                }

            }
			
            else
            {
                return LuaAPI.luaL_error(L, "invalid lua type for XLuaTest.MyEnum! Expect number or string, got + " + lua_type);
            }

            return 1;
		}
	}
    
    public class TutorialDerivedClassTestEnumInnerWrap
    {
		public static void __Register(RealStatePtr L)
        {
		    ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
		    Utils.BeginObjectRegister(typeof(Tutorial.DerivedClass.TestEnumInner), L, translator, 0, 0, 0, 0);
			Utils.EndObjectRegister(typeof(Tutorial.DerivedClass.TestEnumInner), L, translator, null, null, null, null, null);
			
			Utils.BeginClassRegister(typeof(Tutorial.DerivedClass.TestEnumInner), L, null, 3, 0, 0);

            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "E3", Tutorial.DerivedClass.TestEnumInner.E3);
            
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "E4", Tutorial.DerivedClass.TestEnumInner.E4);
            

			Utils.RegisterFunc(L, Utils.CLS_IDX, "__CastFrom", __CastFrom);
            
            Utils.EndClassRegister(typeof(Tutorial.DerivedClass.TestEnumInner), L, translator);
        }
		
		[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CastFrom(RealStatePtr L)
		{
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			LuaTypes lua_type = LuaAPI.lua_type(L, 1);
            if (lua_type == LuaTypes.LUA_TNUMBER)
            {
                translator.PushTutorialDerivedClassTestEnumInner(L, (Tutorial.DerivedClass.TestEnumInner)LuaAPI.xlua_tointeger(L, 1));
            }
			
            else if(lua_type == LuaTypes.LUA_TSTRING)
            {

			    if (LuaAPI.xlua_is_eq_str(L, 1, "E3"))
                {
                    translator.PushTutorialDerivedClassTestEnumInner(L, Tutorial.DerivedClass.TestEnumInner.E3);
                }
				else if (LuaAPI.xlua_is_eq_str(L, 1, "E4"))
                {
                    translator.PushTutorialDerivedClassTestEnumInner(L, Tutorial.DerivedClass.TestEnumInner.E4);
                }
				else
                {
                    return LuaAPI.luaL_error(L, "invalid string for Tutorial.DerivedClass.TestEnumInner!");
                }

            }
			
            else
            {
                return LuaAPI.luaL_error(L, "invalid lua type for Tutorial.DerivedClass.TestEnumInner! Expect number or string, got + " + lua_type);
            }

            return 1;
		}
	}
    
}