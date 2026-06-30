using Application;
using Domain.Event;
using Presentation.UI.Framework.Base;
using Presentation.UI.Framework.Manager;
using Presentation.UI.Framework.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对局结算面板（UIScreen）
///
/// 职责：
/// - 监听 matchStatsUpdateChannel，收到 isFinal=true 时自动弹出
/// - 展示本局击杀数、总伤害、总经验、获得金币、总金币
/// - 关闭时调用 MatchStatisticsManager.ConsumeMatchResult() 清除待结算标记
///
/// 使用方式：
///   将此脚本挂载到结算面板根节点，在 Inspector 中绑定各文本组件。
///   面板预制体放置在大厅场景中，不激活。
///
/// 订阅时机说明：
///   UIScreen.Awake() 会将 gameObject 设为 inactive，
///   这导致 OnEnable() / SubscribeEvents() 永远不会被调用。
///   因此改在 Awake() 中直接订阅，确保即使面板未激活也能收到事件。
/// </summary>
public class MatchResultPanel : UIScreen
{
    [Header("结算数据文本")]
    [SerializeField] private TextMeshProUGUI killText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI coinEarnedText;    // 本次获得
    [SerializeField] private TextMeshProUGUI totalCoinText;      // 总金币
    [SerializeField] private Button closeButton;

    private bool _hasSubscribed;

    // ──────────────────────────────────────────────
    //  UIScreen 生命周期
    // ──────────────────────────────────────────────

    protected override void Awake()
    {
        screenId = UIConstants.ScreenIds.MatchResult;
        base.Awake();   // 内部调用 gameObject.SetActive(false)

        // 注册到 UIManager
        UIManager.Instance.RegisterScreen(this);

        // 绑定关闭按钮
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        // ★ 关键：在 Awake 中直接订阅事件通道
        // 因为 base.Awake() 已设为 inactive，OnEnable 不会触发
        SubscribeToChannel();
    }

    public void OnDestroy()
    {
        UnsubscribeFromChannel();
    }

    // ──────────────────────────────────────────────
    //  事件订阅 / 取消（手动管理，不依赖 OnEnable/OnDisable）
    // ──────────────────────────────────────────────

    /// <summary>
    /// 直接订阅 matchStatsUpdateChannel（在 Awake 中调用）
    /// </summary>
    private void SubscribeToChannel()
    {
        if (_hasSubscribed) return;
        if (EventChannelLocator.MainContainer?.matchStatsUpdateChannel == null) return;

        EventChannelLocator.MainContainer.matchStatsUpdateChannel.RegisterListener(OnMatchStatsUpdated);
        _hasSubscribed = true;
    }

    private void UnsubscribeFromChannel()
    {
        if (!_hasSubscribed) return;
        if (EventChannelLocator.MainContainer?.matchStatsUpdateChannel != null)
        {
            EventChannelLocator.MainContainer.matchStatsUpdateChannel.UnregisterListener(OnMatchStatsUpdated);
        }
        _hasSubscribed = false;
    }

    // ──────────────────────────────────────────────
    //  结算数据回调
    // ──────────────────────────────────────────────

    private void OnMatchStatsUpdated(MatchStatsUpdateData data)
    {
        if (!data.IsFinal) return;

        // 填充数据
        if (killText != null)
            killText.text = data.TotalKills.ToString();

        if (damageText != null)
            damageText.text = data.TotalDamage.ToString();

        if (expText != null)
            expText.text = data.TotalExperience.ToString();

        if (timeText != null)
        {
            int minutes = data.MatchDurationSeconds / 60;
            int seconds = data.MatchDurationSeconds % 60;
            timeText.text = $"{minutes:D2}:{seconds:D2}";
        }

        if (coinEarnedText != null)
            coinEarnedText.text = $"+{data.EarnedCoins}";

        if (totalCoinText != null)
        {
            int currentCoins = CoinManager.Instance != null
                ? CoinManager.Instance.GetCoins()
                : 0;
            totalCoinText.text = currentCoins.ToString();
        }

        // 自动打开结算面板
        UIManager.Instance.Open(this);
    }

    // ──────────────────────────────────────────────
    //  关闭面板
    // ──────────────────────────────────────────────

    private void OnCloseClicked()
    {
        // 通知 MatchStatisticsManager 结算面板已展示完毕
        if (MatchStatisticsManager.Instance != null)
            MatchStatisticsManager.Instance.ConsumeMatchResult();

        // 关闭自身
        CloseSelf();
    }

    protected override void OnBeforeClose()
    {
        base.OnBeforeClose();

        // 兜底：如果通过 ESC 关闭，也清除待结算标记
        if (MatchStatisticsManager.Instance != null && MatchStatisticsManager.Instance.HasPendingMatchResult)
            MatchStatisticsManager.Instance.ConsumeMatchResult();
    }
}