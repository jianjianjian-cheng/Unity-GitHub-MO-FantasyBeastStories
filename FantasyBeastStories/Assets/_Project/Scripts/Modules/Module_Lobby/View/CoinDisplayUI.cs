using Managers;
using Core;
using UI.Framework.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 金币显示 Widget
///
/// 职责：
/// - 进入大厅时读取 CoinManager 中的当前金币数并显示
/// - 通过 coinUpdateChannel 监听金币变化，实时更新显示
///
/// 挂在 HUD Canvas 上作为常驻 Widget
/// </summary>
public class CoinDisplayUI : UIWidget
{
    [Header("Coin Display")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Image coinIcon;

    // ──────────────────────────────────────────────
    //  AutoBindComponents
    // ──────────────────────────────────────────────

    protected override void AutoBindComponents()
    {
        // 未手动绑定时自动查找
        if (coinText == null)
            coinText = GetComponentInChildren<TextMeshProUGUI>();

        if (coinIcon == null)
            coinIcon = GetComponentInChildren<Image>();

        // 关闭所有图片的射线检测
        foreach (var img in GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
    }

    // ──────────────────────────────────────────────
    //  事件订阅 / 取消
    // ──────────────────────────────────────────────

    protected override void SubscribeEvents()
    {
        if (EventChannelLocator.MainContainer != null)
            EventChannelLocator.MainContainer.coinUpdateChannel.RegisterListener(OnCoinUpdated);

        // ★ 进入大厅时立即读取当前金币数并显示
        RefreshDisplay();
    }

    protected override void UnsubscribeEvents()
    {
        if (EventChannelLocator.MainContainer != null)
            EventChannelLocator.MainContainer.coinUpdateChannel.UnregisterListener(OnCoinUpdated);
    }

    // ──────────────────────────────────────────────
    //  金币更新回调
    // ──────────────────────────────────────────────

    private void OnCoinUpdated(CoinUpdateData data)
    {
        RefreshDisplay();
    }

    /// <summary>
    /// 从 CoinManager 读取当前金币数并刷新 UI（≥1000 显示为 X.XK）
    /// </summary>
    private void RefreshDisplay()
    {
        if (coinText == null) return;

        int coins = ServiceLocator.Get<CoinManager>() != null
            ? ServiceLocator.Get<CoinManager>().GetCoins()
            : 0;

        coinText.text = FormatCoins(coins);
    }

    /// <summary>
    /// 将金币数格式化为易读形式：
    /// &lt;1000 显示原值，≥1000 显示 X.XK，≥1000000 显示 X.XM
    /// </summary>
    private static string FormatCoins(int amount)
    {
        if (amount < 1000)
            return amount.ToString();

        if (amount < 1000000)
        {
            float kValue = amount / 1000f;
            string formatted = kValue.ToString("0.#");
            return $"{formatted}K";
        }

        float mValue = amount / 1000000f;
        string mFormatted = mValue.ToString("0.#");
        return $"{mFormatted}M";
    }
}