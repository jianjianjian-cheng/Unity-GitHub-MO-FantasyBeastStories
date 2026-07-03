using Domain.Event;
using UnityEngine;

namespace Application
{
    /// <summary>
    /// 金币系统管理器（Application 层）
    ///
    /// 职责：
    /// - 管理金币的增删查（运行时内存值）
    /// - 提供金币计算公式 CalculateCoins(kills, totalDamage)
    /// - 通过 EventChannel 与 Presentation 层通信
    ///
    /// 持久化说明：
    /// - 不再使用 PlayerPrefs，由 SaveManager 统一管理
    /// - SaveManager.LoadGame() → SetCoins() 恢复
    /// - SaveManager.SaveGame() → GetCoins() 收集
    ///
    /// 通信方式：
    /// 输出 → coinUpdateChannel（金币变化时更新 UI）
    ///
    /// 设计说明：
    /// - 纯本地单例，无需网络同步
    /// - 只含一种货币（金币）
    /// - CalculateCoins() 为纯计算方法，不修改余额
    /// </summary>
    public class CoinManager : MonoBehaviour
    {
        public static CoinManager Instance { get; private set; }

        // ========== 配置项（Inspector 可调） ==========

        [Header("金币计算参数")]
        [SerializeField] private int baseCoinPerKill = 50;
        [SerializeField] private float damageCoinFactor = 0.1f;

        // ========== 运行时状态 ==========

        private int currentCoins;

        // ========== 单例生命周期 ==========

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ========== 公开 API ==========

        /// <summary>获取当前金币总数</summary>
        public int GetCoins()
        {
            return currentCoins;
        }

        /// <summary>增加金币，并触发 UI 更新</summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            currentCoins += amount;
            RaiseCoinUpdate(amount);
        }

        /// <summary>
        /// 消费金币
        /// </summary>
        /// <param name="amount">消费金额</param>
        /// <returns>是否消费成功（余额不足返回 false）</returns>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return false;
            if (currentCoins < amount) return false;

            currentCoins -= amount;
            RaiseCoinUpdate(-amount);
            return true;
        }

        /// <summary>直接设置金币数（用于存档恢复、GM 指令等）</summary>
        public void SetCoins(int amount)
        {
            if (amount < 0) amount = 0;

            int delta = amount - currentCoins;
            currentCoins = amount;
            RaiseCoinUpdate(delta);
        }

        // ========== 金币计算公式 ==========

        /// <summary>
        /// 根据击杀数和总伤害计算应获得的金币数
        /// 公式：kills * baseCoinPerKill + floor(totalDamage * damageCoinFactor)
        /// </summary>
        /// <param name="kills">本局击杀数</param>
        /// <param name="totalDamage">本局总伤害量</param>
        /// <returns>经公式计算后应得的金币数</returns>
        /// <remarks>
        /// 此方法为纯计算，不修改余额。
        /// 调用方应使用返回值调用 AddCoins() 来实际发放金币。
        /// </remarks>
        public int CalculateCoins(int kills, int totalDamage)
        {
            int killReward = kills * baseCoinPerKill;
            int damageReward = Mathf.FloorToInt(totalDamage * damageCoinFactor);
            return killReward + damageReward;
        }

        /// <summary>重置金币为 0（用于测试）</summary>
        public void ResetCoins()
        {
            currentCoins = 0;
            RaiseCoinUpdate(0);
        }

        /// <summary>
        /// 广播当前金币数到事件通道，用于 UI 初始化。
        /// 进入大厅时调用，确保所有金币显示组件获得初始值。
        /// </summary>
        public void BroadcastCurrentCoins()
        {
            RaiseCoinUpdate(0);
        }

        // ========== 事件通信 ==========

        /// <summary>
        /// 发送金币更新事件到 Presentation 层
        /// </summary>
        private void RaiseCoinUpdate(int delta)
        {
            if (EventChannelLocator.MainContainer?.coinUpdateChannel == null) return;

            var data = new CoinUpdateData(currentCoins, delta);
            EventChannelLocator.MainContainer.coinUpdateChannel.Raise(data);
        }
    }
}