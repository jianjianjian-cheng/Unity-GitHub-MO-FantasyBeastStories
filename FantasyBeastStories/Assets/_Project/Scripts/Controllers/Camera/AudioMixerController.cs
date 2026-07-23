using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.Audio;

namespace Controllers
{
    /// <summary>
    /// AudioMixer 控制器 — 管理 Mixer 暴露参数（音量）的读写
    /// 将 0~1 线性值映射为 -80~0 dB 对数曲线
    /// </summary>
    public class AudioMixerController
    {
        private readonly AudioMixer _mixer;

        // ——— Mixer Groups ———
        public AudioMixerGroup MasterGroup { get; }
        public AudioMixerGroup BGMGroup { get; }
        public AudioMixerGroup SFXGroup { get; }
        public AudioMixerGroup UIGroup { get; }
        public AudioMixerGroup AmbientGroup { get; }

        public bool IsValid => _mixer != null;

        // ——— Mixer 暴露参数名 ———
        private const string PARAM_MASTER = "MasterVolume";
        private static readonly Dictionary<AudioChannelType, string> GroupParams = new()
        {
            [AudioChannelType.BGM] = "BGMVolume",
            [AudioChannelType.SFX] = "SFXVolume",
            [AudioChannelType.UI] = "UIVolume",
            [AudioChannelType.Ambient] = "AmbientVolume",
        };

        public AudioMixerController(AudioMixer mixer)
        {
            _mixer = mixer;

            if (mixer == null)
            {
                Debug.LogWarning("[AudioMixerController] 未提供 AudioMixer，音量将回退为直接控制 AudioSource.volume");
                return;
            }

            // 查找 Mixer Groups（路径取决于 Mixer 层级结构）
            MasterGroup = FindGroup("Master");
            BGMGroup = FindGroup("Master/BGM");
            SFXGroup = FindGroup("Master/SFX");
            UIGroup = FindGroup("Master/UI");
            AmbientGroup = FindGroup("Master/Ambient");
        }

        private AudioMixerGroup FindGroup(string path)
        {
            var groups = _mixer.FindMatchingGroups(path);
            if (groups == null || groups.Length == 0)
            {
                Debug.LogWarning($"[AudioMixerController] 未找到 Mixer Group: {path}");
                return null;
            }
            return groups[0];
        }

        /* ==================== 音量设置 ==================== */

        /// <summary>设置总音量（MasterVolume 暴露参数）</summary>
        public void SetMasterVolume(float linearVolume)
        {
            if (!IsValid) return;
            _mixer.SetFloat(PARAM_MASTER, LinearToDB(linearVolume));
        }

        /// <summary>设置指定类型的音量组</summary>
        public void SetGroupVolume(AudioChannelType type, float linearVolume)
        {
            if (!IsValid) return;

            if (!GroupParams.TryGetValue(type, out var param))
            {
                Debug.LogWarning($"[AudioMixerController] 不支持的 AudioType: {type}");
                return;
            }

            _mixer.SetFloat(param, LinearToDB(linearVolume));
        }

        /* ==================== 音量读取 ==================== */

        /// <summary>获取总音量</summary>
        public float GetMasterVolume()
        {
            if (!IsValid) return 1f;
            return _mixer.GetFloat(PARAM_MASTER, out float dB) ? DBToLinear(dB) : 1f;
        }

        /// <summary>获取指定类型的音量</summary>
        public float GetGroupVolume(AudioChannelType type)
        {
            if (!IsValid) return 1f;

            if (!GroupParams.TryGetValue(type, out var param))
                return 1f;

            return _mixer.GetFloat(param, out float dB) ? DBToLinear(dB) : 1f;
        }

        /* ==================== Mixer Group 查询 ==================== */

        /// <summary>获取 AudioType 对应的 Mixer Group，用于给 AudioSource 分配</summary>
        public AudioMixerGroup GetGroup(AudioChannelType type)
        {
            return type switch
            {
                AudioChannelType.BGM => BGMGroup,
                AudioChannelType.SFX => SFXGroup,
                AudioChannelType.UI => UIGroup,
                AudioChannelType.Ambient => AmbientGroup,
                _ => MasterGroup,
            };
        }

        /* ==================== 工具方法 ==================== */

        /// <summary>线性值 (0~1) → 分贝值 (-80~0)</summary>
        private static float LinearToDB(float linear)
        {
            return linear > 0.001f ? Mathf.Log10(linear) * 20f : -80f;
        }

        /// <summary>分贝值 (-80~0) → 线性值 (0~1)</summary>
        private static float DBToLinear(float dB)
        {
            return Mathf.Pow(10, dB / 20f);
        }
    }
}