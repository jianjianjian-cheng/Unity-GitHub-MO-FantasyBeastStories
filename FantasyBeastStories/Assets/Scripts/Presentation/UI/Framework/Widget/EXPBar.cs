using Domain.Event;
using Domain.Event.Channels;
using Presentation.UI.Framework.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经验条 Widget：通过 experienceUpdateChannel 监听经验/等级变化，
/// 自动更新 Slider 进度与 Text 文字。
/// </summary>
public class EXPBar : UIWidget
{
    [Header("EXP Bar")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private Text levelText;
    [SerializeField] private Text expText;

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

        if (expText == null && levelText != null)
        {
            Text[] allTexts = GetComponentsInChildren<Text>();
            if (allTexts.Length >= 2 && allTexts[1] != levelText)
                expText = allTexts[1];
            else if (allTexts.Length >= 2)
                expText = allTexts[0];
        }

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
            levelText.text = $"Lv.{data.CurrentLevel}";

        if (expText != null)
            expText.text = $"{data.CurrentExperience}/{data.UpgradeExperience}";
    }
}