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
    public class CoreLuaLuaPoolHelperWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(Core.Lua.LuaPoolHelper);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 0, 0);
			
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 3, 0, 0);
			Utils.RegisterFunc(L, Utils.CLS_IDX, "RegisterPool", _m_RegisterPool_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "EnsurePoolCreated", _m_EnsurePoolCreated_xlua_st_);
            
			
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            return LuaAPI.luaL_error(L, "Core.Lua.LuaPoolHelper does not have a constructor!");
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_RegisterPool_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    string _poolName = LuaAPI.lua_tostring(L, 1);
                    string _addressablePath = LuaAPI.lua_tostring(L, 2);
                    int _count = LuaAPI.xlua_tointeger(L, 3);
                    
                    Core.Lua.LuaPoolHelper.RegisterPool( _poolName, _addressablePath, _count );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_EnsurePoolCreated_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    string _poolName = LuaAPI.lua_tostring(L, 1);
                    string _addressablePath = LuaAPI.lua_tostring(L, 2);
                    int _count = LuaAPI.xlua_tointeger(L, 3);
                    
                    Core.Lua.LuaPoolHelper.EnsurePoolCreated( _poolName, _addressablePath, _count );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        
        
		
		
		
		
    }
}
