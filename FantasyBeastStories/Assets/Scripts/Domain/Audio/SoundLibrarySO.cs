using System.Collections.Generic;
using UnityEngine;

namespace Domain.Audio
{
    /// <summary>
    /// 音效库 — 集中管理所有 SoundDefinition，提供 soundId 到定义的快速查找
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrarySO : ScriptableObject
    {
        [SerializeField] private SoundDefinitionSO[] _sounds;

        private Dictionary<string, SoundDefinitionSO> _lookup;

        private void EnsureInitialized()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<string, SoundDefinitionSO>();

            if (_sounds != null)
            {
                foreach (var sound in _sounds)
                {
                    if (sound == null)
                        continue;

                    if (string.IsNullOrEmpty(sound.soundId))
                    {
                        Debug.LogWarning($"[SoundLibrary] 音效定义 {sound.name} 的 soundId 为空，已跳过");
                        continue;
                    }

                    if (_lookup.ContainsKey(sound.soundId))
                    {
                        Debug.LogWarning($"[SoundLibrary] 重复的 soundId: {sound.soundId}，后面的定义将覆盖前面的");
                    }

                    _lookup[sound.soundId] = sound;
                }
            }
        }

        /// <summary>根据 soundId 获取音效定义（未找到时打印错误并返回 null）</summary>
        public SoundDefinitionSO GetSound(string soundId)
        {
            EnsureInitialized();

            if (_lookup.TryGetValue(soundId, out var sound))
                return sound;

            Debug.LogError($"[SoundLibrary] 未找到音效定义: {soundId}");
            return null;
        }

        /// <summary>尝试获取音效定义（不打印错误）</summary>
        public bool TryGetSound(string soundId, out SoundDefinitionSO sound)
        {
            EnsureInitialized();
            return _lookup.TryGetValue(soundId, out sound);
        }

#if UNITY_EDITOR
        /// <summary>Editor 工具方法：更新音效列表</summary>
        public void Editor_SetSounds(SoundDefinitionSO[] sounds) => _sounds = sounds;
#endif
    }
}