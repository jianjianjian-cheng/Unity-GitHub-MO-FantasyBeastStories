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
    public class CoreSharedModelAttributePlayerBaseWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(Core.SharedModel.AttributePlayerBase);
			Utils.BeginObjectRegister(type, L, translator, 0, 43, 0, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetAttackInterval", _m_SetAttackInterval);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ReduceAttackInterval", _m_ReduceAttackInterval);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetAttackInterval", _m_GetAttackInterval);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetAttackSpeed", _m_GetAttackSpeed);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetHealthRecover", _m_GetHealthRecover);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetIsDead", _m_SetIsDead);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetIsDead", _m_GetIsDead);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetMaxHealth", _m_SetMaxHealth);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddCurrentHealth", _m_AddCurrentHealth);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetMoveSpeed", _m_SetMoveSpeed);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddMoveSpeed", _m_AddMoveSpeed);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetCriticalMultiplier", _m_SetCriticalMultiplier);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddCriticalMultiplier", _m_AddCriticalMultiplier);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetCriticalChance", _m_SetCriticalChance);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddCriticalChance", _m_AddCriticalChance);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetAttackPower", _m_SetAttackPower);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddAttackPower", _m_AddAttackPower);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetDefensePower", _m_SetDefensePower);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddDefensePower", _m_AddDefensePower);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetCurrentHealth", _m_GetCurrentHealth);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetMaxHealth", _m_GetMaxHealth);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddMaxHealth", _m_AddMaxHealth);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetMoveSpeed", _m_GetMoveSpeed);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetAttackPower", _m_GetAttackPower);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetDefensePower", _m_GetDefensePower);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetCriticalMultiplier", _m_GetCriticalMultiplier);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetCriticalChance", _m_GetCriticalChance);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetHealthRecover", _m_SetHealthRecover);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Damage", _m_Damage);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetMaxAttackCount", _m_GetMaxAttackCount);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddMaxAttackCount", _m_AddMaxAttackCount);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetComboCount", _m_GetComboCount);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddComboCount", _m_AddComboCount);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetEmpowerCharge", _m_GetEmpowerCharge);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetCurrentElement", _m_GetCurrentElement);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetCurrentElement", _m_SetCurrentElement);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetSplit", _m_GetSplit);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetSplit", _m_SetSplit);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetSplitCount", _m_GetSplitCount);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetSplitCount", _m_SetSplitCount);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddSplitCount", _m_AddSplitCount);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetMultiTargetCount", _m_GetMultiTargetCount);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddMultiTargetCount", _m_AddMultiTargetCount);
			
			
			
			
			
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
				if(LuaAPI.lua_gettop(L) == 2 && translator.Assignable<Core.SharedModel.PlayerAttributeConfigSO>(L, 2))
				{
					Core.SharedModel.PlayerAttributeConfigSO _config = (Core.SharedModel.PlayerAttributeConfigSO)translator.GetObject(L, 2, typeof(Core.SharedModel.PlayerAttributeConfigSO));
					
					var gen_ret = new Core.SharedModel.AttributePlayerBase(_config);
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to Core.SharedModel.AttributePlayerBase constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetAttackInterval(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _attackInterval = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.SetAttackInterval( _attackInterval );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ReduceAttackInterval(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _ratio = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.ReduceAttackInterval( _ratio );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetAttackInterval(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetAttackInterval(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetAttackSpeed(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetAttackSpeed(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetHealthRecover(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetHealthRecover(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetIsDead(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    bool _isDead = LuaAPI.lua_toboolean(L, 2);
                    
                    gen_to_be_invoked.SetIsDead( _isDead );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetIsDead(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetIsDead(  );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetMaxHealth(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _maxHealth = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetMaxHealth( _maxHealth );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddCurrentHealth(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _currentHealth = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.AddCurrentHealth( _currentHealth );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetMoveSpeed(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _moveSpeed = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetMoveSpeed( _moveSpeed );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddMoveSpeed(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _moveSpeed = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.AddMoveSpeed( _moveSpeed );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetCriticalMultiplier(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _criticalMultiplier = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetCriticalMultiplier( _criticalMultiplier );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddCriticalMultiplier(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _ratio = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.AddCriticalMultiplier( _ratio );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetCriticalChance(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _criticalChance = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetCriticalChance( _criticalChance );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddCriticalChance(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _ratio = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.AddCriticalChance( _ratio );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetAttackPower(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _attackPower = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetAttackPower( _attackPower );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddAttackPower(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _ratio = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.AddAttackPower( _ratio );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetDefensePower(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _defensePower = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetDefensePower( _defensePower );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddDefensePower(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _ratio = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.AddDefensePower( _ratio );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetCurrentHealth(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetCurrentHealth(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetMaxHealth(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetMaxHealth(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddMaxHealth(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _maxHealth = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.AddMaxHealth( _maxHealth );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetMoveSpeed(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetMoveSpeed(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetAttackPower(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetAttackPower(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetDefensePower(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetDefensePower(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetCriticalMultiplier(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetCriticalMultiplier(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetCriticalChance(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetCriticalChance(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetHealthRecover(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _healthRecover = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetHealthRecover( _healthRecover );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Damage(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _damage = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.Damage( _damage );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetMaxAttackCount(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetMaxAttackCount(  );
                        LuaAPI.xlua_pushinteger(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddMaxAttackCount(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _attackCount = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.AddMaxAttackCount( _attackCount );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetComboCount(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetComboCount(  );
                        LuaAPI.xlua_pushinteger(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddComboCount(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _comboCount = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.AddComboCount( _comboCount );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetEmpowerCharge(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetEmpowerCharge(  );
                        LuaAPI.xlua_pushinteger(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetCurrentElement(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetCurrentElement(  );
                        translator.PushCoreSharedModelElement(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetCurrentElement(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    Core.SharedModel.Element _element;translator.Get(L, 2, out _element);
                    
                    gen_to_be_invoked.SetCurrentElement( _element );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetSplit(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetSplit(  );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetSplit(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    bool _isSplit = LuaAPI.lua_toboolean(L, 2);
                    
                    gen_to_be_invoked.SetSplit( _isSplit );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetSplitCount(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetSplitCount(  );
                        LuaAPI.xlua_pushinteger(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetSplitCount(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _splitCount = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.SetSplitCount( _splitCount );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddSplitCount(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _count = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.AddSplitCount( _count );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetMultiTargetCount(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetMultiTargetCount(  );
                        LuaAPI.xlua_pushinteger(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddMultiTargetCount(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.SharedModel.AttributePlayerBase gen_to_be_invoked = (Core.SharedModel.AttributePlayerBase)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _count = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.AddMultiTargetCount( _count );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        
        
		
		
		
		
    }
}
