using Core;
using Core.Channels;
using UI.Framework.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经验条 Widget：通过 experienceUpdateChannel 监听经验/等级变化，
/// 自动更新 Slider 进度与等级文字（纯数字）。
/// </summary>
public class EXPBar : UIWidget
{
    [Header("EXP Bar")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private Text levelText;

    // ──────────────────────────────────────────────
    //  AutoBindComponents
    // ──────────────────────────────────────────────

    protected override void AutoBindComponents()
    {
        // 未手动绑定时自动查找
        if (expSlider == null)
            expSlider = GetComponentInChildren<Slider>();

        if (levelText == null)
            levelText = GetComponentInChildren<Text>();

        // 初始化 Slider
        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
            expSlider.value = 0f;
        }

        // 关闭所有图片的射线检测，避免挡住后面 UI 的点击
        foreach (var img in GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
    }

    // ──────────────────────────────────────────────
    //  事件订阅 / 取消
    // ──────────────────────────────────────────────

    protected override void SubscribeEvents()
    {
        if (EventChannelLocator.MainContainer != null)
            EventChannelLocator.MainContainer.experienceUpdateChannel.RegisterListener(OnExperienceUpdated);
    }

    protected override void UnsubscribeEvents()
    {
        if (EventChannelLocator.MainContainer != null)
            EventChannelLocator.MainContainer.experienceUpdateChannel.UnregisterListener(OnExperienceUpdated);
    }

    // ──────────────────────────────────────────────
    //  经验更新回调
    // ──────────────────────────────────────────────

    private void OnExperienceUpdated(ExperienceUpdateData data)
    {
        if (expSlider != null)
            expSlider.value = data.SliderProgress;

        if (levelText != null)
            levelText.text = data.CurrentLevel.ToString();
    }
}