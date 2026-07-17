using UnityEngine;

namespace Core
{
    /// <summary>
    /// 音频全局配置 — 默认音量、淡入淡出参数、对象池大小等
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Audio Config", fileName = "AudioConfig")]
    public class AudioConfigSO : ScriptableObject
    {
        [Header("默认音量（用户未设置时的初始值）")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float uiVolume = 1f;
        [Range(0f, 1f)] public float ambientVolume = 1f;

        [Header("BGM 淡入淡出")]
        public float bgmFadeInDuration = 1f;
        public float bgmFadeOutDuration = 1f;
        public bool bgmCrossfade = true;
        public float bgmCrossfadeDuration = 2f;

        [Header("SFX 对象池")]
        public int poolSize = 20;
        public bool poolAutoExpand = true;
    }
}