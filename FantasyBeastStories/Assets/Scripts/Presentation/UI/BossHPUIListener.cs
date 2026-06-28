using Domain.Event;
using Domain.Event.Channels.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.UI
{
    /// <summary>
    /// 监听 BossHPUpdateEventChannelSO 事件并更新 UI
    /// 职责：仅处理 UI 更新，不包含任何业务逻辑
    /// </summary>
    public class BossHPUIListener : MonoBehaviour
    {
        [Header("Boss血量UI引用")]
        [SerializeField] private GameObject bossHPUIRoot;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI bossNameText;

        void OnEnable()
        {
            if (EventChannelLocator.MainContainer.bossHPUpdateChannel != null)
            {
                EventChannelLocator.MainContainer.bossHPUpdateChannel.RegisterListener(OnBossHPUpdate);
            }
        }

        void OnDisable()
        {
            if (EventChannelLocator.MainContainer.bossHPUpdateChannel != null)
            {
                EventChannelLocator.MainContainer.bossHPUpdateChannel.UnregisterListener(OnBossHPUpdate);
            }
        }

        private void OnBossHPUpdate(BossHPUpdateData data)
        {
            // 未序列化引用时，运行时自动查找（兼容旧场景）
            if (bossHPUIRoot == null)
            {
                bossHPUIRoot = GameObject.Find("BossHPUI");
                if (bossHPUIRoot == null) return;
            }

            if (data.isInitialized)
            {
                // 初始化显示
                bossHPUIRoot.SetActive(true);

                if (hpSlider == null)
                    hpSlider = bossHPUIRoot.GetComponentInChildren<Slider>();
                if (bossNameText == null)
                    bossNameText = bossHPUIRoot.GetComponentInChildren<TextMeshProUGUI>();

                if (hpSlider != null)
                {
                    hpSlider.maxValue = data.maxHealth;
                    hpSlider.value = data.currentHealth;
                }
                if (bossNameText != null)
                {
                    bossNameText.text = data.bossName;
                }
            }
            else
            {
                // 仅更新血量
                if (hpSlider == null)
                    hpSlider = bossHPUIRoot.GetComponentInChildren<Slider>();
                if (hpSlider != null)
                {
                    hpSlider.value = data.currentHealth;
                }
            }
        }
    }
}