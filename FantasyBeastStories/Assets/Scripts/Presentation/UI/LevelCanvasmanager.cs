using Domain.Event;
using Domain.Event.Channels;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.UI
{
    /// <summary>
    /// 等级画布管理器：监听经验/等级更新事件并更新 UI
    ///
    /// 职责：
    /// - 从 experienceUpdateChannel 接收 ExperienceUpdateData
    /// - 更新 Slider（经验进度条）+ Text（等级/经验值文字）
    /// </summary>
    public class LevelCanvasmanager : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private Slider expSlider;       // 经验进度条
        [SerializeField] private Text levelText;         // 等级文字（如 "Lv.3"）
        [SerializeField] private Text expText;           // 经验值文字（如 "120/300"）

        void OnEnable()
        {
            if (EventChannelLocator.MainContainer != null)
            {
                EventChannelLocator.MainContainer.experienceUpdateChannel.RegisterListener(OnExperienceUpdated);
            }
        }

        void OnDisable()
        {
            if (EventChannelLocator.MainContainer != null)
            {
                EventChannelLocator.MainContainer.experienceUpdateChannel.UnregisterListener(OnExperienceUpdated);
            }
        }

        void Start()
        {
            // 未手动绑定时自动查找
            if (expSlider == null)
                expSlider = GetComponent<Slider>();

            if (levelText == null)
                levelText = GetComponentInChildren<Text>();

            if (expText == null && levelText != null)
            {
                // 有多个 Text 子对象时，取第二个作为经验值文字
                Text[] allTexts = GetComponentsInChildren<Text>();
                if (allTexts.Length >= 2 && allTexts[1] != levelText)
                    expText = allTexts[1];
                else if (allTexts.Length >= 2)
                    expText = allTexts[0];
            }

            // 初始化 Slider 范围
            if (expSlider != null)
            {
                expSlider.minValue = 0f;
                expSlider.maxValue = 1f;
                expSlider.value = 0f;
            }
        }

        /// <summary>
        /// 经验/等级更新回调
        /// </summary>
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
}