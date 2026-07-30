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
    public class CoreNetworkNetworkServiceLocatorWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(Core.Network.NetworkServiceLocator);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 0, 0);
			
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 5, 6, 0);
			Utils.RegisterFunc(L, Utils.CLS_IDX, "Register", _m_Register_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "RegisterObjectPoolService", _m_RegisterObjectPoolService_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "RegisterGameActionService", _m_RegisterGameActionService_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "RegisterDomainRpcService", _m_RegisterDomainRpcService_xlua_st_);
            
			
            
			Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "PlayerService", _g_get_PlayerService);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "ObjectService", _g_get_ObjectService);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "ObjectPoolService", _g_get_ObjectPoolService);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "GameActionService", _g_get_GameActionService);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "DomainRpcService", _g_get_DomainRpcService);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "IsInitialized", _g_get_IsInitialized);
            
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            return LuaAPI.luaL_error(L, "Core.Network.NetworkServiceLocator does not have a constructor!");
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Register_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
                
                {
                    Core.Contracts.INetworkPlayerService _playerService = (Core.Contracts.INetworkPlayerService)translator.GetObject(L, 1, typeof(Core.Contracts.INetworkPlayerService));
                    Core.Contracts.INetworkObjectService _objectService = (Core.Contracts.INetworkObjectService)translator.GetObject(L, 2, typeof(Core.Contracts.INetworkObjectService));
                    
                    Core.Network.NetworkServiceLocator.Register( _playerService, _objectService );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_RegisterObjectPoolService_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
                
                {
                    Core.Contracts.IObjectPoolService _objectPoolService = (Core.Contracts.IObjectPoolService)translator.GetObject(L, 1, typeof(Core.Contracts.IObjectPoolService));
                    
                    Core.Network.NetworkServiceLocator.RegisterObjectPoolService( _objectPoolService );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_RegisterGameActionService_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
                
                {
                    Core.Contracts.IGameActionService _gameActionService = (Core.Contracts.IGameActionService)translator.GetObject(L, 1, typeof(Core.Contracts.IGameActionService));
                    
                    Core.Network.NetworkServiceLocator.RegisterGameActionService( _gameActionService );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_RegisterDomainRpcService_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
                
                {
                    Core.Contracts.IControllerRpcService _domainRpcService = (Core.Contracts.IControllerRpcService)translator.GetObject(L, 1, typeof(Core.Contracts.IControllerRpcService));
                    
                    Core.Network.NetworkServiceLocator.RegisterDomainRpcService( _domainRpcService );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_PlayerService(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.PushAny(L, Core.Network.NetworkServiceLocator.PlayerService);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_ObjectService(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.PushAny(L, Core.Network.NetworkServiceLocator.ObjectService);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_ObjectPoolService(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.PushAny(L, Core.Network.NetworkServiceLocator.ObjectPoolService);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_GameActionService(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.PushAny(L, Core.Network.NetworkServiceLocator.GameActionService);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_DomainRpcService(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.PushAny(L, Core.Network.NetworkServiceLocator.DomainRpcService);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_IsInitialized(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.lua_pushboolean(L, Core.Network.NetworkServiceLocator.IsInitialized);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
		
		
		
		
    }
}
