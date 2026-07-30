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
    public class CoreAudioAudioManagerWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(Core.Audio.AudioManager);
			Utils.BeginObjectRegister(type, L, translator, 0, 16, 0, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ReloadSoundLibrary", _m_ReloadSoundLibrary);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlayBGM", _m_PlayBGM);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "StopBGM", _m_StopBGM);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlaySFX", _m_PlaySFX);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlayUI", _m_PlayUI);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PlayAmbient", _m_PlayAmbient);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "StopAmbient", _m_StopAmbient);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "DuckBGM", _m_DuckBGM);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "UnduckBGM", _m_UnduckBGM);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetMasterVolume", _m_SetMasterVolume);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetVolume", _m_SetVolume);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetMasterVolume", _m_GetMasterVolume);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetVolume", _m_GetVolume);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "PauseAll", _m_PauseAll);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ResumeAll", _m_ResumeAll);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "StopAll", _m_StopAll);
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 1, 0);
			
			
            
			Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "Instance", _g_get_Instance);
            
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new Core.Audio.AudioManager();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to Core.Audio.AudioManager constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ReloadSoundLibrary(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.ReloadSoundLibrary(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayBGM(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& (LuaAPI.lua_isnil(L, 2) || LuaAPI.lua_type(L, 2) == LuaTypes.LUA_TSTRING)&& translator.Assignable<System.Nullable<float>>(L, 3)) 
                {
                    string _soundId = LuaAPI.lua_tostring(L, 2);
                    System.Nullable<float> _fadeDuration;translator.Get(L, 3, out _fadeDuration);
                    
                    gen_to_be_invoked.PlayBGM( _soundId, _fadeDuration );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 2&& (LuaAPI.lua_isnil(L, 2) || LuaAPI.lua_type(L, 2) == LuaTypes.LUA_TSTRING)) 
                {
                    string _soundId = LuaAPI.lua_tostring(L, 2);
                    
                    gen_to_be_invoked.PlayBGM( _soundId );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to Core.Audio.AudioManager.PlayBGM!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_StopBGM(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 2&& translator.Assignable<System.Nullable<float>>(L, 2)) 
                {
                    System.Nullable<float> _fadeDuration;translator.Get(L, 2, out _fadeDuration);
                    
                    gen_to_be_invoked.StopBGM( _fadeDuration );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 1) 
                {
                    
                    gen_to_be_invoked.StopBGM(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to Core.Audio.AudioManager.StopBGM!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlaySFX(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 4&& (LuaAPI.lua_isnil(L, 2) || LuaAPI.lua_type(L, 2) == LuaTypes.LUA_TSTRING)&& translator.Assignable<System.Nullable<UnityEngine.Vector3>>(L, 3)&& translator.Assignable<UnityEngine.Transform>(L, 4)) 
                {
                    string _soundId = LuaAPI.lua_tostring(L, 2);
                    System.Nullable<UnityEngine.Vector3> _position;translator.Get(L, 3, out _position);
                    UnityEngine.Transform _attachTarget = (UnityEngine.Transform)translator.GetObject(L, 4, typeof(UnityEngine.Transform));
                    
                    gen_to_be_invoked.PlaySFX( _soundId, _position, _attachTarget );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 3&& (LuaAPI.lua_isnil(L, 2) || LuaAPI.lua_type(L, 2) == LuaTypes.LUA_TSTRING)&& translator.Assignable<System.Nullable<UnityEngine.Vector3>>(L, 3)) 
                {
                    string _soundId = LuaAPI.lua_tostring(L, 2);
                    System.Nullable<UnityEngine.Vector3> _position;translator.Get(L, 3, out _position);
                    
                    gen_to_be_invoked.PlaySFX( _soundId, _position );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 2&& (LuaAPI.lua_isnil(L, 2) || LuaAPI.lua_type(L, 2) == LuaTypes.LUA_TSTRING)) 
                {
                    string _soundId = LuaAPI.lua_tostring(L, 2);
                    
                    gen_to_be_invoked.PlaySFX( _soundId );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to Core.Audio.AudioManager.PlaySFX!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayUI(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    string _soundId = LuaAPI.lua_tostring(L, 2);
                    
                    gen_to_be_invoked.PlayUI( _soundId );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PlayAmbient(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& (LuaAPI.lua_isnil(L, 2) || LuaAPI.lua_type(L, 2) == LuaTypes.LUA_TSTRING)&& translator.Assignable<System.Nullable<UnityEngine.Vector3>>(L, 3)) 
                {
                    string _soundId = LuaAPI.lua_tostring(L, 2);
                    System.Nullable<UnityEngine.Vector3> _position;translator.Get(L, 3, out _position);
                    
                    gen_to_be_invoked.PlayAmbient( _soundId, _position );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 2&& (LuaAPI.lua_isnil(L, 2) || LuaAPI.lua_type(L, 2) == LuaTypes.LUA_TSTRING)) 
                {
                    string _soundId = LuaAPI.lua_tostring(L, 2);
                    
                    gen_to_be_invoked.PlayAmbient( _soundId );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to Core.Audio.AudioManager.PlayAmbient!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_StopAmbient(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.StopAmbient(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_DuckBGM(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3)) 
                {
                    float _targetVolume = (float)LuaAPI.lua_tonumber(L, 2);
                    float _fadeDuration = (float)LuaAPI.lua_tonumber(L, 3);
                    
                    gen_to_be_invoked.DuckBGM( _targetVolume, _fadeDuration );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 2&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    float _targetVolume = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.DuckBGM( _targetVolume );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 1) 
                {
                    
                    gen_to_be_invoked.DuckBGM(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to Core.Audio.AudioManager.DuckBGM!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_UnduckBGM(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 2&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    float _fadeDuration = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.UnduckBGM( _fadeDuration );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 1) 
                {
                    
                    gen_to_be_invoked.UnduckBGM(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to Core.Audio.AudioManager.UnduckBGM!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetMasterVolume(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    float _volume = (float)LuaAPI.lua_tonumber(L, 2);
                    
                    gen_to_be_invoked.SetMasterVolume( _volume );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetVolume(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    Core.AudioChannelType _type;translator.Get(L, 2, out _type);
                    float _volume = (float)LuaAPI.lua_tonumber(L, 3);
                    
                    gen_to_be_invoked.SetVolume( _type, _volume );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetMasterVolume(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.GetMasterVolume(  );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetVolume(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    Core.AudioChannelType _type;translator.Get(L, 2, out _type);
                    
                        var gen_ret = gen_to_be_invoked.GetVolume( _type );
                        LuaAPI.lua_pushnumber(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_PauseAll(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.PauseAll(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ResumeAll(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.ResumeAll(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_StopAll(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                Core.Audio.AudioManager gen_to_be_invoked = (Core.Audio.AudioManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.StopAll(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Instance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.Push(L, Core.Audio.AudioManager.Instance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
		
		
		
		
    }
}
