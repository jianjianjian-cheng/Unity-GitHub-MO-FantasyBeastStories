using UnityEngine;

namespace Domain.Audio
{
    /// <summary>
    /// 单个音效定义 — 将 AudioClip 与播放参数打包为一个可配置的资源
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Sound Definition", fileName = "NewSoundDefinition")]
    public class SoundDefinitionSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("唯一标识符，用于代码中播放此音效")]
        public string soundId;

        [Tooltip("音频片段")]
        public AudioClip clip;

        [Tooltip("音频类型，决定了走哪个音量通道")]
        public AudioChannelType audioType = AudioChannelType.SFX;

        [Header("播放参数")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop;

        [Header("3D 空间音效")]
        [Range(0f, 1f)] public float spatialBlend;
        [Tooltip("3D 音效最小听距（低于此距离音量不再增大）")]
        public float minDistance = 1f;
        [Tooltip("3D 音效最大听距（超过此距离音量归零）")]
        public float maxDistance = 500f;

        [Header("对象池")]
        [Tooltip("是否使用对象池复用 AudioSource；非频繁播放的音效可关闭")]
        public bool usePooling = true;
    }
}