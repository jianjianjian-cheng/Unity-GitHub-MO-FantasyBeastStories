using Managers;
using Core;
using UI.Framework.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音量设置 Widget — 通过四个 Slider 分别控制 Master / BGM / SFX / UI 音量
///
/// 用法：
/// 1. 在 Canvas 上创建一个 GameObject，挂载此脚本
/// 2. 在 Inspector 中将四个 Slider 拖入对应字段
/// 3. 或直接作为子对象，AutoBindComponents 会自动查找 Slider
///
/// 订阅方式：无事件通道，直接调用 AudioManager 的同步 API
/// </summary>
public class AudioSettingsWidget : UIWidget
{
    [Header("音量 Slider")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;

    [Header("音量数值文本（可选）")]
    [SerializeField] private Text masterValueText;
    [SerializeField] private Text bgmValueText;
    [SerializeField] private Text sfxValueText;
    [SerializeField] private Text uiValueText;

    [Header("标签文本（可选）")]
    [SerializeField] private Text masterLabelText;
    [SerializeField] private Text bgmLabelText;
    [SerializeField] private Text sfxLabelText;
    [SerializeField] private Text uiLabelText;

    // ——— 防递归标志 ———
    private bool _isUpdating;

    // ──────────────────────────────────────────────
    //  AutoBindComponents
    // ──────────────────────────────────────────────

    protected override void AutoBindComponents()
    {
        // 自动查找 Slider（按名称约定）
        if (masterSlider == null)
            masterSlider = FindComponentByPrefix("Master");
        if (bgmSlider == null)
            bgmSlider = FindComponentByPrefix("BGM");
        if (sfxSlider == null)
            sfxSlider = FindComponentByPrefix("SFX");
        if (uiSlider == null)
            uiSlider = FindComponentByPrefix("UI");

        // 自动查找数值文本（按名称约定）
        if (masterValueText == null)
            masterValueText = FindTextComponent("MasterValue");
        if (bgmValueText == null)
            bgmValueText = FindTextComponent("BGMValue");
        if (sfxValueText == null)
            sfxValueText = FindTextComponent("SFXValue");
        if (uiValueText == null)
            uiValueText = FindTextComponent("UIValue");

        // 自动查找标签文本（按名称约定）
        if (masterLabelText == null)
            masterLabelText = FindTextComponent("MasterLabel");
        if (bgmLabelText == null)
            bgmLabelText = FindTextComponent("BGMLabel");
        if (sfxLabelText == null)
            sfxLabelText = FindTextComponent("SFXLabel");
        if (uiLabelText == null)
            uiLabelText = FindTextComponent("UILabel");

        // 设置默认标签文字
        SetDefaultLabels();

        // 关闭所有图片的射线检测，避免挡住后面 UI 的点击
        foreach (var img in GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
    }

    /// <summary>按名称前缀查找子对象中的 Slider，支持递归查找</summary>
    private Slider FindComponentByPrefix(string prefix)
    {
        // 在所有子对象中查找名称包含指定前缀的 Slider
        foreach (var slider in GetComponentsInChildren<Slider>(true))
        {
            if (slider.name.StartsWith(prefix))
                return slider;
        }
        return null;
    }

    /// <summary>按名称查找子对象中的 Text 组件</summary>
    private Text FindTextComponent(string name)
    {
        var t = FindDeepChild(transform, name);
        return t != null ? t.GetComponent<Text>() : null;
    }

    /// <summary>深度优先查找子对象</summary>
    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            var result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private void SetDefaultLabels()
    {
        if (masterLabelText != null && string.IsNullOrEmpty(masterLabelText.text))
            masterLabelText.text = "主音量";
        if (bgmLabelText != null && string.IsNullOrEmpty(bgmLabelText.text))
            bgmLabelText.text = "背景音乐";
        if (sfxLabelText != null && string.IsNullOrEmpty(sfxLabelText.text))
            sfxLabelText.text = "音效";
        if (uiLabelText != null && string.IsNullOrEmpty(uiLabelText.text))
            uiLabelText.text = "界面音效";
    }

    // ──────────────────────────────────────────────
    //  生命周期
    // ──────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        // 每次显示时从 AudioManager 同步当前音量值
        RefreshFromAudioManager();
    }

    // ──────────────────────────────────────────────
    //  初始化 Slider 事件
    // ──────────────────────────────────────────────

    protected override void SubscribeEvents()
    {
        // 绑定 Slider 事件（先移除旧监听防止重复绑定）
        BindSlider(masterSlider, OnMasterSliderChanged);
        BindSlider(bgmSlider, OnBGMSliderChanged);
        BindSlider(sfxSlider, OnSFXSliderChanged);
        BindSlider(uiSlider, OnUISliderChanged);
    }

    protected override void UnsubscribeEvents()
    {
        UnbindSlider(masterSlider, OnMasterSliderChanged);
        UnbindSlider(bgmSlider, OnBGMSliderChanged);
        UnbindSlider(sfxSlider, OnSFXSliderChanged);
        UnbindSlider(uiSlider, OnUISliderChanged);
    }

    private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(callback);
    }

    private static void UnbindSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveListener(callback);
    }

    // ──────────────────────────────────────────────
    //  Slider 事件处理
    // ──────────────────────────────────────────────

    private void OnMasterSliderChanged(float value)
    {
        if (_isUpdating) return;
        AudioManager.Instance.SetMasterVolume(value);
        UpdateValueText(masterValueText, value);
    }

    private void OnBGMSliderChanged(float value)
    {
        if (_isUpdating) return;
        AudioManager.Instance.SetVolume(AudioChannelType.BGM, value);
        UpdateValueText(bgmValueText, value);
    }

    private void OnSFXSliderChanged(float value)
    {
        if (_isUpdating) return;
        AudioManager.Instance.SetVolume(AudioChannelType.SFX, value);
        UpdateValueText(sfxValueText, value);
    }

    private void OnUISliderChanged(float value)
    {
        if (_isUpdating) return;
        AudioManager.Instance.SetVolume(AudioChannelType.UI, value);
        UpdateValueText(uiValueText, value);
    }

    // ──────────────────────────────────────────────
    //  刷新显示
    // ──────────────────────────────────────────────

    /// <summary>
    /// 从 AudioManager 读取当前音量并同步到 Slider 和文本
    /// </summary>
    public void RefreshFromAudioManager()
    {
        if (AudioManager.Instance == null) return;

        _isUpdating = true;

        SetSliderValue(masterSlider, masterValueText, AudioManager.Instance.GetMasterVolume());
        SetSliderValue(bgmSlider, bgmValueText, AudioManager.Instance.GetVolume(AudioChannelType.BGM));
        SetSliderValue(sfxSlider, sfxValueText, AudioManager.Instance.GetVolume(AudioChannelType.SFX));
        SetSliderValue(uiSlider, uiValueText, AudioManager.Instance.GetVolume(AudioChannelType.UI));

        _isUpdating = false;
    }

    private void SetSliderValue(Slider slider, Text valueText, float volume)
    {
        if (slider != null)
        {
            slider.value = Mathf.Clamp01(volume);
            // 如果 Slider 的 wholeNumbers 为 true，需要特殊处理
            if (slider.wholeNumbers)
                slider.value = Mathf.RoundToInt(volume * 100f);
        }

        UpdateValueText(valueText, volume);
    }

    private static void UpdateValueText(Text valueText, float volume)
    {
        if (valueText != null)
        {
            // 显示为百分比格式，如 "80%"
            int percent = Mathf.RoundToInt(volume * 100f);
            valueText.text = $"{percent}%";
        }
    }

    // ──────────────────────────────────────────────
    //  公共方法（供外部调用，如重置按钮）
    // ──────────────────────────────────────────────

    /// <summary>将所有音量重置为默认值（1.0）</summary>
    public void ResetToDefault()
    {
        AudioManager.Instance.SetMasterVolume(1f);
        AudioManager.Instance.SetVolume(AudioChannelType.BGM, 1f);
        AudioManager.Instance.SetVolume(AudioChannelType.SFX, 1f);
        AudioManager.Instance.SetVolume(AudioChannelType.UI, 1f);

        RefreshFromAudioManager();
    }
}