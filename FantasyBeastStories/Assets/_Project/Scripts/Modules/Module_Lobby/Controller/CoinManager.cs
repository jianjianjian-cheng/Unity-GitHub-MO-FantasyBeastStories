using Core;
using Core.SharedModel;
using Core.Save;
using UnityEngine;
using Managers;

namespace Managers
{
    /// <summary>
    /// 金币控制器 — 薄层 MonoBehaviour，持有 CoinModel 实例。
    ///
    /// 职责：
    /// - 生命周期管理（单例 + DontDestroyOnLoad）
    /// - 创建并持有 CoinModel（纯 C#，可单测）
    /// - 存档注册（ISaveable）
    ///
    /// 业务逻辑全部委托给 CoinModel。
    /// </summary>
    public class CoinManager : MonoBehaviour, ISaveable
    {
        private static CoinManager _instance;

        [Header("金币计算参数")]
        [SerializeField] private int baseCoinPerKill = 50;
        [SerializeField] private float damageCoinFactor = 0.1f;

        /// <summary>金币模型实例（纯 C#，可单测）</summary>
        public CoinModel Model { get; private set; }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ServiceLocator.Register(this);
            DontDestroyOnLoad(gameObject);
            Model = new CoinModel(baseCoinPerKill, damageCoinFactor);
        }

        void Start()
        {
            ServiceLocator.Get<SaveManager>()?.RegisterSaveable(this);
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                ServiceLocator.Unregister<CoinManager>();
                ServiceLocator.Get<SaveManager>()?.UnregisterSaveable(this);
            }
        }

        // ========== ISaveable 实现 ==========

        public string SaveId => "CoinManager";

        public void OnSave(SaveData data) => data.coin = Model.CurrentCoins;
        public void OnLoad(SaveData data) => Model.SetCoins(data.coin);

        // ========== 便捷转发（向后兼容现有调用方） ==========

        public int GetCoins() => Model.GetCoins();
        public void AddCoins(int amount) => Model.AddCoins(amount);
        public bool SpendCoins(int amount) => Model.SpendCoins(amount);
        public void SetCoins(int amount) => Model.SetCoins(amount);
        public int CalculateCoins(int kills, int totalDamage) => Model.CalculateCoins(kills, totalDamage);
        public void ResetCoins() => Model.ResetCoins();
        public void BroadcastCurrentCoins() => Model.BroadcastCurrentCoins();
    }
}
