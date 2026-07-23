using Core;
using Core.Channels.Combat;
using UI.Framework.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 监听 BossHPUpdateEventChannelSO 事件并更新 UI
    /// 职责：仅处理 UI 更新，不包含任何业务逻辑
    ///
    /// 使用方式：
    ///   将此脚本挂载到 BossHPUI 根节点上，UIWidget 框架会自动完成
    ///   组件查找与事件订阅。
    /// </summary>
    public class BossHPUIListener : UIWidget
    {
        [Header("Boss血量UI引用")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI hpValueText;

        // 用 CanvasGroup 控制显隐，避免 SetActive(false) 打断事件订阅
        private CanvasGroup _canvasGroup;

        // 订阅重试标记（事件通道可能因初始化顺序暂未就绪）
        private bool _hasSubscribed;

        // ──────────────────────────────────────────────
        //  AutoBindComponents：自动查找子组件
        // ──────────────────────────────────────────────

        protected override void AutoBindComponents()
        {
            if (hpSlider == null)
                hpSlider = GetComponentInChildren<Slider>(true);

            if (bossNameText == null)
            {
                // 优先按名字查找，避免跟 hpValueText 混淆
                bossNameText = transform.Find("BossNameText")?.GetComponent<TextMeshProUGUI>();
                if (bossNameText == null)
                    bossNameText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (hpValueText == null)
            {
                hpValueText = transform.Find("BossHPValueText")?.GetComponent<TextMeshProUGUI>();
            }

            // 用 CanvasGroup 控制显隐（不阻断事件订阅生命周期）
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;

            // 初始化 Slider 默认值，避免在 Boss 事件到来前显示异常
            if (hpSlider != null)
            {
                hpSlider.minValue = 0f;
                hpSlider.maxValue = 1f;   // 临时默认值，Boss 生成后由事件更新为实际血量
                hpSlider.value = 0f;
            }

            // 文本初始为空，避免显示 "0 / 0"
            if (hpValueText != null)
                hpValueText.text = "";
            if (bossNameText != null)
                bossNameText.text = "";
        }

        // ──────────────────────────────────────────────
        //  事件订阅 / 取消（UIWidget 生命周期自动管理）
        // ──────────────────────────────────────────────

        protected override void SubscribeEvents()
        {
            if (_hasSubscribed) return;
            if (EventChannelLocator.MainContainer?.bossHPUpdateChannel == null) return;

            EventChannelLocator.MainContainer.bossHPUpdateChannel.RegisterListener(OnBossHPUpdate);
            _hasSubscribed = true;
        }

        protected override void UnsubscribeEvents()
        {
            if (!_hasSubscribed) return;
            if (EventChannelLocator.MainContainer?.bossHPUpdateChannel != null)
            {
                EventChannelLocator.MainContainer.bossHPUpdateChannel.UnregisterListener(OnBossHPUpdate);
            }
            _hasSubscribed = false;
        }

        /// <summary>
        /// Update 重试：如果事件通道尚未就绪，每帧重试订阅
        /// </summary>
        private void Update()
        {
            if (!_hasSubscribed)
                SubscribeEvents();
        }

        // ──────────────────────────────────────────────
        //  Boss 血量更新回调
        // ──────────────────────────────────────────────

        private void OnBossHPUpdate(BossHPUpdateData data)
        {
            if (data.isInitialized)
            {
                // 初始化：显示血条、设置最大值、Boss 名称
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;

                if (hpSlider != null)
                {
                    hpSlider.maxValue = data.maxHealth;
                    hpSlider.value = data.currentHealth;
                }

                if (bossNameText != null)
                {
                    bossNameText.text = data.bossName;
                }

                if (hpValueText != null)
                {
                    hpValueText.text = $"{data.currentHealth} / {data.maxHealth}";
                }
            }
            else
            {
                // 仅更新血量数值与进度条
                if (hpSlider != null)
                {
                    hpSlider.value = data.currentHealth;
                }

                if (hpValueText != null)
                {
                    hpValueText.text = $"{data.currentHealth} / {data.maxHealth}";
                }
            }
        }
    }
}