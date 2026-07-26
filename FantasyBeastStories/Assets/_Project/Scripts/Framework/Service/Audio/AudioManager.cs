using System.Collections;
using System.Collections.Generic;
using Managers;
using Controllers;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio
{
    /// <summary>
    /// 音频管理器 — 核心控制器
    /// 职责：BGM 播放/切换/交叉淡入淡出、SFX/UI 音效播放、环境音播放、
    ///       音量管理（AudioMixer 集成）、暂停恢复
    /// 生命周期：懒加载单例，DontDestroyOnLoad 跨场景
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /* ==================== 单例 ==================== */

        private static AudioManager _instance;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[AudioManager]");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /* ==================== 配置资源（通过 Addressables 加载，确保热更生效） ==================== */

        private SoundLibrarySO _soundLibrary;
        private AudioConfigSO _config;
        private AudioMixer _audioMixer;

        /* ==================== 运行时组件 ==================== */

        // ——— BGM（双源交叉淡入淡出） ———
        private AudioSource _bgmSource;          // 主 BGM 通道
        private AudioSource _bgmSource2;         // 辅 BGM 通道（用于交叉淡入淡出）

        // ——— 环境音 ———
        private AudioSource _ambientSource;      // 环境音专用通道

        // ——— SFX 对象池 ———
        private AudioSourcePool _sfxPool;

        // ——— Mixer 控制器 ———
        private AudioMixerController _mixerController;

        /* ==================== 运行时状态 ==================== */

        private string _currentBGMId;              // 当前 BGM 的 soundId
        private Coroutine _bgmFadeJob;             // BGM 淡入淡出协程
        private float _masterVolume = 1f;          // 总音量（回退值）
        private Dictionary<AudioChannelType, float> _volumes;  // 各类型音量（回退值）
        private bool _useMixer;                    // 是否正在使用 AudioMixer

        // ——— BGM Ducking ———
        private Coroutine _duckJob;                 // Ducking 协程
        private float _bgmVolumeBeforeDuck = 1f;    // Ducking 前的 BGM 音量

        // ——— 卡牌选择面板激活时跳过音频暂停 ———
        private bool _skipAudioPause;

        /* ==================== PlayerPrefs 持久化键 ==================== */

        private const string PREF_MASTER = "Audio_Master";
        private const string PREF_BGM = "Audio_BGM";
        private const string PREF_SFX = "Audio_SFX";
        private const string PREF_UI = "Audio_UI";
        private const string PREF_AMBIENT = "Audio_Ambient";

        /* ==================== 初始化 ==================== */

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ServiceLocator.Register(this);
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            // 通过 Addressables 加载配置（确保热更后使用最新数据）
            _config = AssetLoader.LoadAsset<AudioConfigSO>("Local_Audio_Config_AudioConfig");

            _soundLibrary = AssetLoader.LoadAsset<SoundLibrarySO>("Local_Audio_Libraries_MainSoundLibrary");

            Debug.Log($"[AudioManager] 初始化: _soundLibrary={(_soundLibrary != null ? _soundLibrary.name : "NULL")}, _config={(_config != null ? _config.name : "NULL")}, _audioMixer={(_audioMixer != null ? _audioMixer.name : "NULL")}");

            _audioMixer = AssetLoader.LoadAsset<AudioMixer>("Local_Audio_MainMixer");

            // ——— 初始化 AudioMixer 控制器 ———
            _mixerController = new AudioMixerController(_audioMixer);
            _useMixer = _mixerController.IsValid;

            if (_useMixer)
                Debug.Log("[AudioManager] AudioMixer 已加载，音量将通过 Mixer 控制");
            else
                Debug.Log("[AudioManager] 未找到 AudioMixer，将回退为直接控制 AudioSource.volume");

            // ——— 创建 BGM 双通道 ———
            _bgmSource = CreateAudioSource("BGM_Primary", AudioChannelType.BGM, true);
            _bgmSource2 = CreateAudioSource("BGM_Secondary", AudioChannelType.BGM, true);

            // ——— 创建环境音通道 ———
            _ambientSource = CreateAudioSource("Ambient", AudioChannelType.Ambient, true);

            // ——— 创建 SFX 对象池 ———
            int poolSize = _config != null ? _config.poolSize : 50;
            bool autoExpand = _config != null ? _config.poolAutoExpand : true;
            AudioMixerGroup sfxGroup = _useMixer ? _mixerController.GetGroup(AudioChannelType.SFX) : null;
            _sfxPool = new AudioSourcePool(poolSize, autoExpand, transform, sfxGroup);

            // ——— 初始化音量 ———
            _masterVolume = PlayerPrefs.GetFloat(PREF_MASTER, _config != null ? _config.masterVolume : 1f);
            _volumes = new Dictionary<AudioChannelType, float>
            {
                [AudioChannelType.BGM] = PlayerPrefs.GetFloat(PREF_BGM, _config != null ? _config.bgmVolume : 1f),
                [AudioChannelType.SFX] = PlayerPrefs.GetFloat(PREF_SFX, _config != null ? _config.sfxVolume : 1f),
                [AudioChannelType.UI] = PlayerPrefs.GetFloat(PREF_UI, _config != null ? _config.uiVolume : 1f),
                [AudioChannelType.Ambient] = PlayerPrefs.GetFloat(PREF_AMBIENT, _config != null ? _config.ambientVolume : 1f),
            };

            // 同步到 Mixer
            if (_useMixer)
            {
                _mixerController.SetMasterVolume(_masterVolume);
                _mixerController.SetGroupVolume(AudioChannelType.BGM, _volumes[AudioChannelType.BGM]);
                _mixerController.SetGroupVolume(AudioChannelType.SFX, _volumes[AudioChannelType.SFX]);
                _mixerController.SetGroupVolume(AudioChannelType.UI, _volumes[AudioChannelType.UI]);
                _mixerController.SetGroupVolume(AudioChannelType.Ambient, _volumes[AudioChannelType.Ambient]);
            }

            Debug.Log("[AudioManager] 初始化完成" + (_useMixer ? "（AudioMixer 模式）" : "（基础模式）"));
        }

        /// <summary>热更后重新加载 SoundLibrary（清除缓存 + 重新通过 Addressables 加载）</summary>
        public void ReloadSoundLibrary()
        {
            if (_soundLibrary != null)
            {
                _soundLibrary.ClearCache();
                Debug.Log("[AudioManager] 已清除旧 SoundLibrary 缓存");
            }

            _soundLibrary = AssetLoader.LoadAsset<SoundLibrarySO>("Local_Audio_Libraries_MainSoundLibrary");

            if (_soundLibrary != null)
            {
                Debug.Log($"[AudioManager] 热更后重载 SoundLibrary 成功: {_soundLibrary.name}");
                if (_soundLibrary.TryGetSound("sfx_wizard_hit", out var wizardHit))
                    Debug.Log($"[AudioManager]   sfx_wizard_hit → {(wizardHit.clip != null ? wizardHit.clip.name : "NULL")}");
                if (_soundLibrary.TryGetSound("sfx_GuiLingHit", out var guiLingHit))
                    Debug.Log($"[AudioManager]   sfx_GuiLingHit → {(guiLingHit.clip != null ? guiLingHit.clip.name : "NULL")}");
            }
            else
            {
                Debug.LogError("[AudioManager] 热更后重载 SoundLibrary 失败！");
            }
        }

        /// <summary>创建并配置一个 AudioSource，自动分配到对应的 Mixer Group</summary>
        private AudioSource CreateAudioSource(string name, AudioChannelType AudioChannelType, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;

            // 分配到 Mixer Group
            if (_useMixer)
            {
                var group = _mixerController.GetGroup(AudioChannelType);
                if (group != null)
                    source.outputAudioMixerGroup = group;
            }

            return source;
        }

        private void OnEnable()
        {
            EventChannelLocator.MainContainer.pauseStateChannel?.RegisterListener(OnPauseStateChanged);
            EventChannelLocator.MainContainer.magicUpgradeChannel?.RegisterListener(OnMagicUpgradeRequested);
        }

        private void OnDisable()
        {
            EventChannelLocator.MainContainer.pauseStateChannel?.UnregisterListener(OnPauseStateChanged);
            EventChannelLocator.MainContainer.magicUpgradeChannel?.UnregisterListener(OnMagicUpgradeRequested);
        }

        private void OnMagicUpgradeRequested(bool isOpen)
        {
            _skipAudioPause = isOpen;
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (_skipAudioPause)
                return;

            if (isPaused)
                PauseAll();
            else
                ResumeAll();
        }

        /* ==================== BGM 管理 ==================== */

        /// <summary>播放背景音乐（自动处理交叉淡入淡出切换）</summary>
        /// <param name="soundId">BGM 音效定义 ID</param>
        /// <param name="fadeDuration">淡入淡出时长（秒），默认使用配置值</param>
        public void PlayBGM(string soundId, float? fadeDuration = null)
        {
            if (string.IsNullOrEmpty(soundId))
            {
                Debug.LogWarning("[AudioManager] PlayBGM: soundId 为空");
                return;
            }

            if (soundId == _currentBGMId)
                return;

            if (!_soundLibrary.TryGetSound(soundId, out var def))
            {
                Debug.LogError($"[AudioManager] 未找到 BGM 定义: {soundId}");
                return;
            }

            _currentBGMId = soundId;

            if (_bgmFadeJob != null)
                StopCoroutine(_bgmFadeJob);

            _bgmFadeJob = StartCoroutine(PlayBGMInternal(def, fadeDuration));
        }

        /// <summary>
        /// BGM 播放协程 — 使用双 AudioSource 实现真正的同步交叉淡入淡出
        /// 旧 BGM 在新 BGM 开始播放的同时淡出，保证音乐无缝衔接
        /// </summary>
        private IEnumerator PlayBGMInternal(SoundDefinitionSO def, float? fadeDuration)
        {
            float duration = fadeDuration ?? (_config != null ? _config.bgmCrossfadeDuration : 1f);

            // 确定新旧两个源
            bool hasCurrent = _bgmSource.clip != null && _bgmSource.isPlaying;

            // 配置新源（始终使用 _bgmSource2 作为" incoming"）
            AudioSource incoming = _bgmSource2;
            AudioSource outgoing = hasCurrent ? _bgmSource : null;

            incoming.clip = def.clip;
            incoming.pitch = def.pitch;
            incoming.spatialBlend = 0f;
            incoming.volume = 0f;

            // 等待一帧再 Play，避免在 sceneLoaded 回调（首帧之前）调用 Play 无效
            yield return null;
            incoming.Play();

            // 同步交叉淡入淡出
            float targetVol = def.volume * (_useMixer ? 1f : GetFinalVolume(AudioChannelType.BGM));
            float startVol = hasCurrent ? outgoing.volume : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                incoming.volume = Mathf.Lerp(0f, targetVol, t);

                if (hasCurrent && outgoing != null)
                    outgoing.volume = Mathf.Lerp(startVol, 0f, t);

                yield return null;
            }

            // 停止旧源
            if (hasCurrent && outgoing != null)
            {
                outgoing.Stop();
                outgoing.clip = null;
                outgoing.volume = 0f;
            }

            // 交换：新源成为主源，旧源变为备用
            // 必须保存旧的 _bgmSource 引用，否则当 outgoing 为 null 时
            // _bgmSource2 会指向 incoming（与 _bgmSource 相同），导致下次切换时
            // incoming 和 outgoing 是同一个 AudioSource，Play 后立即被 Stop
            AudioSource oldPrimary = _bgmSource;
            _bgmSource = incoming;
            _bgmSource2 = oldPrimary;

            _bgmFadeJob = null;
        }

        /// <summary>停止背景音乐（淡出）</summary>
        /// <param name="fadeDuration">淡出时长（秒），默认使用配置值</param>
        public void StopBGM(float? fadeDuration = null)
        {
            if (_bgmFadeJob != null)
                StopCoroutine(_bgmFadeJob);

            _bgmFadeJob = StartCoroutine(StopBGMInternal(fadeDuration ?? (_config != null ? _config.bgmFadeOutDuration : 1f)));
        }

        private IEnumerator StopBGMInternal(float fadeOut)
        {
            AudioSource target = _bgmSource;

            if (target != null && target.isPlaying)
            {
                float startVol = target.volume;
                float elapsed = 0f;

                while (elapsed < fadeOut)
                {
                    elapsed += Time.unscaledDeltaTime;
                    target.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeOut);
                    yield return null;
                }

                target.Stop();
                target.volume = 0f;
            }

            _currentBGMId = null;
            _bgmFadeJob = null;
        }

        /* ==================== SFX 管理 ==================== */

        /// <summary>播放 2D / 3D 音效</summary>
        /// <param name="soundId">音效 ID</param>
        /// <param name="position">3D 空间中的播放位置（null = 2D）</param>
        /// <param name="attachTarget">跟随的目标 Transform（不为 null 时忽略 position）</param>
        public void PlaySFX(string soundId, Vector3? position = null, Transform attachTarget = null)
        {
            if (!_soundLibrary.TryGetSound(soundId, out var def))
            {
                Debug.LogError($"[AudioManager] 未找到 SFX 定义: {soundId}");
                return;
            }

            var source = _sfxPool.Get();
            if (source == null)
            {
                Debug.LogWarning($"[AudioManager] SFX 对象池已满，无法播放: {soundId}");
                return;
            }

            source.clip = def.clip;
            source.volume = def.volume * (_useMixer ? 1f : GetFinalVolume(AudioChannelType.SFX));
            source.pitch = def.pitch;
            source.spatialBlend = attachTarget != null || position.HasValue ? 1f : def.spatialBlend;
            source.minDistance = def.minDistance;
            source.maxDistance = def.maxDistance;
            source.loop = false;

            if (attachTarget != null)
            {
                source.transform.SetParent(attachTarget);
                source.transform.localPosition = Vector3.zero;
            }
            else if (position.HasValue)
            {
                source.transform.position = position.Value;
            }

            source.Play();

            StartCoroutine(ReturnToPoolAfterPlay(source, def.clip != null ? def.clip.length + 0.1f : 0.5f));
        }

        private IEnumerator ReturnToPoolAfterPlay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source.transform.SetParent(transform);
                _sfxPool.Return(source);
            }
        }

        /* ==================== UI 音效 ==================== */

        /// <summary>播放 UI 音效（2D，不受空间音频影响）</summary>
        /// <param name="soundId">音效 ID</param>
        public void PlayUI(string soundId)
        {
            if (!_soundLibrary.TryGetSound(soundId, out var def))
            {
                Debug.LogError($"[AudioManager] 未找到 UI 音效定义: {soundId}");
                return;
            }

            var source = _sfxPool.Get();
            if (source == null)
            {
                Debug.LogWarning($"[AudioManager] SFX 对象池已满，无法播放 UI 音效: {soundId}");
                return;
            }

            source.clip = def.clip;
            source.volume = def.volume * (_useMixer ? 1f : GetFinalVolume(AudioChannelType.UI));
            source.pitch = def.pitch;
            source.spatialBlend = 0f;
            source.loop = false;
            source.Play();

            StartCoroutine(ReturnToPoolAfterPlay(source, def.clip != null ? def.clip.length + 0.1f : 0.5f));
        }

        /* ==================== 环境音 ==================== */

        /// <summary>播放环境音（循环，可设置 3D 位置）</summary>
        /// <param name="soundId">环境音音效 ID</param>
        /// <param name="position">3D 空间位置（null = 2D 环境音）</param>
        public void PlayAmbient(string soundId, Vector3? position = null)
        {
            if (string.IsNullOrEmpty(soundId))
            {
                Debug.LogWarning("[AudioManager] PlayAmbient: soundId 为空");
                return;
            }

            if (!_soundLibrary.TryGetSound(soundId, out var def))
            {
                Debug.LogError($"[AudioManager] 未找到 Ambient 定义: {soundId}");
                return;
            }

            _ambientSource.clip = def.clip;
            _ambientSource.volume = def.volume * (_useMixer ? 1f : GetFinalVolume(AudioChannelType.Ambient));
            _ambientSource.pitch = def.pitch;
            _ambientSource.spatialBlend = position.HasValue ? 1f : def.spatialBlend;
            _ambientSource.minDistance = def.minDistance;
            _ambientSource.maxDistance = def.maxDistance;
            _ambientSource.loop = true;

            if (position.HasValue)
                _ambientSource.transform.position = position.Value;

            _ambientSource.Play();
        }

        /// <summary>停止环境音</summary>
        public void StopAmbient()
        {
            if (_ambientSource != null && _ambientSource.isPlaying)
            {
                _ambientSource.Stop();
            }
        }

        /* ==================== BGM Ducking（音量闪避） ==================== */

        /// <summary>
        /// 临时降低 BGM 音量（Ducking），用于重要音效/语音播放时避免冲突
        /// 例如：Boss 吼叫、剧情对话时自动闪避背景音乐
        /// </summary>
        /// <param name="targetVolume">目标音量 (0~1)，默认 0.2</param>
        /// <param name="fadeDuration">淡入淡出时长（秒），默认 0.3</param>
        public void DuckBGM(float targetVolume = 0.2f, float fadeDuration = 0.3f)
        {
            if (_duckJob != null)
                StopCoroutine(_duckJob);

            _duckJob = StartCoroutine(DuckBGMInternal(targetVolume, fadeDuration));
        }

        /// <summary>恢复 BGM 音量到 Ducking 之前的值</summary>
        /// <param name="fadeDuration">恢复时长（秒），默认 1.0</param>
        public void UnduckBGM(float fadeDuration = 1f)
        {
            if (_duckJob != null)
                StopCoroutine(_duckJob);

            _duckJob = StartCoroutine(UnduckBGMInternal(fadeDuration));
        }

        private IEnumerator DuckBGMInternal(float targetVolume, float fadeDuration)
        {
            AudioSource target = _bgmSource;
            if (target == null || !target.isPlaying)
            {
                _duckJob = null;
                yield break;
            }

            _bgmVolumeBeforeDuck = target.volume;
            float startVol = target.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.volume = Mathf.Lerp(startVol, targetVolume, elapsed / fadeDuration);
                yield return null;
            }

            target.volume = targetVolume;
            _duckJob = null;
        }

        private IEnumerator UnduckBGMInternal(float fadeDuration)
        {
            AudioSource target = _bgmSource;
            if (target == null || !target.isPlaying)
            {
                _duckJob = null;
                yield break;
            }

            float startVol = target.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.volume = Mathf.Lerp(startVol, _bgmVolumeBeforeDuck, elapsed / fadeDuration);
                yield return null;
            }

            target.volume = _bgmVolumeBeforeDuck;
            _duckJob = null;
        }

        /* ==================== 音量管理 ==================== */

        /// <summary>设置总音量</summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);

            if (_useMixer)
                _mixerController.SetMasterVolume(_masterVolume);
            else
                ApplyVolumeToActiveSources();

            PlayerPrefs.SetFloat(PREF_MASTER, _masterVolume);
            PlayerPrefs.Save();
        }

        /// <summary>设置指定类型的音量</summary>
        public void SetVolume(AudioChannelType type, float volume)
        {
            if (!_volumes.ContainsKey(type))
            {
                Debug.LogWarning($"[AudioManager] 不支持的音频类型: {type}");
                return;
            }

            _volumes[type] = Mathf.Clamp01(volume);

            // 通过 Mixer 控制（优先）或直接控制 AudioSource
            if (_useMixer)
                _mixerController.SetGroupVolume(type, _volumes[type]);
            else
                ApplyVolumeToActiveSources();

            // 持久化
            string key = type switch
            {
                AudioChannelType.BGM => PREF_BGM,
                AudioChannelType.SFX => PREF_SFX,
                AudioChannelType.UI => PREF_UI,
                AudioChannelType.Ambient => PREF_AMBIENT,
                _ => null,
            };

            if (key != null)
            {
                PlayerPrefs.SetFloat(key, _volumes[type]);
                PlayerPrefs.Save();
            }
        }

        /// <summary>获取总音量</summary>
        public float GetMasterVolume() => _useMixer ? _mixerController.GetMasterVolume() : _masterVolume;

        /// <summary>获取指定类型的音量</summary>
        public float GetVolume(AudioChannelType type) => _useMixer ? _mixerController.GetGroupVolume(type) : _volumes.GetValueOrDefault(type, 1f);

        /// <summary>计算最终播放音量（Master × 类型音量，仅非 Mixer 模式使用）</summary>
        private float GetFinalVolume(AudioChannelType type)
        {
            return _masterVolume * _volumes.GetValueOrDefault(type, 1f);
        }

        /// <summary>将当前音量设置应用到所有正在播放的 AudioSource（非 Mixer 模式回退方案）</summary>
        private void ApplyVolumeToActiveSources()
        {
            if (_bgmSource != null && _bgmSource.isPlaying)
                _bgmSource.volume = GetFinalVolume(AudioChannelType.BGM);

            _sfxPool?.ApplyVolume(GetFinalVolume(AudioChannelType.SFX));
        }

        /* ==================== 生命周期控制 ==================== */

        /// <summary>暂停所有音频</summary>
        public void PauseAll()
        {
            if (_bgmSource != null) _bgmSource.Pause();
            if (_ambientSource != null) _ambientSource.Pause();
            _sfxPool?.PauseAll();
        }

        /// <summary>恢复所有音频</summary>
        public void ResumeAll()
        {
            if (_bgmSource != null) _bgmSource.UnPause();
            if (_ambientSource != null) _ambientSource.UnPause();
            _sfxPool?.ResumeAll();
        }

        /// <summary>停止所有音频</summary>
        public void StopAll()
        {
            StopBGM(0f);
            StopAmbient();
            _sfxPool?.StopAll();
        }

        /* ==================== 内部类：AudioSource 对象池 ==================== */

        /// <summary>
        /// AudioSource 对象池 — 复用 AudioSource 实例，避免频繁创建/销毁
        /// </summary>
        private class AudioSourcePool
        {
            private AudioSource[] _pool;
            private readonly Queue<int> _available;
            private readonly Transform _root;
            private readonly bool _autoExpand;

            public AudioSourcePool(int size, bool autoExpand, Transform root, AudioMixerGroup mixerGroup)
            {
                _pool = new AudioSource[size];
                _available = new Queue<int>(size);
                _root = root;
                _autoExpand = autoExpand;

                for (int i = 0; i < size; i++)
                {
                    CreateSource(i, mixerGroup);
                    _available.Enqueue(i);
                }
            }

            private void CreateSource(int index, AudioMixerGroup mixerGroup)
            {
                var go = new GameObject($"SFX_Pool_{index}");
                go.transform.SetParent(_root);
                go.SetActive(false);

                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = mixerGroup;
                _pool[index] = source;
            }

            public AudioSource Get()
            {
                if (_available.Count == 0)
                {
                    if (_autoExpand)
                    {
                        int newIndex = _pool.Length;
                        System.Array.Resize(ref _pool, newIndex + 1);
                        CreateSource(newIndex, null);
                        _pool[newIndex].gameObject.SetActive(true);
                        return _pool[newIndex];
                    }

                    return null;
                }

                int idx = _available.Dequeue();
                _pool[idx].gameObject.SetActive(true);
                return _pool[idx];
            }

            public void Return(AudioSource source)
            {
                for (int i = 0; i < _pool.Length; i++)
                {
                    if (_pool[i] == source)
                    {
                        source.gameObject.SetActive(false);
                        source.transform.SetParent(_root);
                        source.transform.localPosition = Vector3.zero;
                        _available.Enqueue(i);
                        return;
                    }
                }
            }

            public void ApplyVolume(float volume)
            {
                foreach (var source in _pool)
                {
                    if (source != null && source.isPlaying)
                        source.volume = volume;
                }
            }

            public void PauseAll()
            {
                foreach (var source in _pool)
                {
                    if (source != null && source.isPlaying)
                        source.Pause();
                }
            }

            public void ResumeAll()
            {
                foreach (var source in _pool)
                {
                    if (source != null && source.clip != null)
                        source.UnPause();
                }
            }

            public void StopAll()
            {
                foreach (var source in _pool)
                {
                    if (source != null && source.isPlaying)
                    {
                        source.Stop();
                        source.clip = null;
                        source.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}