using System.Collections.Generic;
using Domain.Data;
using Domain.Rune;
using UnityEngine;

namespace Application
{
    /// <summary>
    /// 存档总管 —— 统一管理所有数据的存/读/删。
    ///
    /// 职责：
    /// - 收集各个 Manager 的数据 → 写入硬盘
    /// - 从硬盘读取数据 → 分发给各个 Manager
    /// - 提供 SaveGame / LoadGame / DeleteGame / HasSave 接口
    ///
    /// 设计说明：
    /// - 单例 + DontDestroyOnLoad，全局唯一
    /// - 一个账号对应一个存档，文件名为 save.json
    /// - 账号系统暂未实现，暂时只有本地单存档
    /// - 与 FileDataHandler 组合使用，不直接操作文件
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("存档配置")]
        [SerializeField] private bool useEncryption = false;

        private FileDataHandler fileHandler;
        private SaveData currentSaveData;

        private const string SAVE_FILE_NAME = "save";
        private const string SAVE_VERSION = "1.0.0";

        public static int SelectedCharacterIndex { get; set; } = 0;

        // ──────────────────────────────────
        //  单例生命周期
        // ──────────────────────────────────

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                fileHandler = new FileDataHandler(UnityEngine.Application.persistentDataPath);
                currentSaveData = new SaveData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // 启动时自动加载存档，确保 SelectedCharacterIndex 等数据从文件恢复
            if (Instance == this && HasSave())
            {
                LoadGame();
            }
        }

        // ──────────────────────────────────
        //  公开 API
        // ──────────────────────────────────

        /// <summary>存档是否存在</summary>
        public bool HasSave()
        {
            return fileHandler.HasSave(SAVE_FILE_NAME);
        }

        /// <summary>
        /// 保存游戏 —— 从各个 Manager 收集数据，写入硬盘
        /// </summary>
        public void SaveGame()
        {
            CollectDataFromManagers();

            // 填入元数据
            currentSaveData.saveVersion = SAVE_VERSION;
            currentSaveData.saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 序列化
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(currentSaveData, Newtonsoft.Json.Formatting.Indented);

            // 写文件
            fileHandler.Save(SAVE_FILE_NAME, json, useEncryption);

            Debug.Log($"[SaveManager] 存档成功 ({currentSaveData.saveTimestamp})");
        }

        /// <summary>
        /// 加载游戏 —— 从硬盘读取数据，分发给各个 Manager
        /// </summary>
        public void LoadGame()
        {
            string json = fileHandler.Load(SAVE_FILE_NAME, useEncryption);

            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[SaveManager] 没有存档，使用默认数据");
                currentSaveData = new SaveData();
                return;
            }

            // 反序列化
            currentSaveData = Newtonsoft.Json.JsonConvert.DeserializeObject<SaveData>(json);

            if (currentSaveData == null)
            {
                Debug.LogError("[SaveManager] 存档解析失败，使用默认数据");
                currentSaveData = new SaveData();
                return;
            }

            // 版本兼容处理
            HandleVersionCompatibility();

            // 分发数据
            DistributeDataToManagers();

            Debug.Log($"[SaveManager] 读档成功 ← ({currentSaveData.saveTimestamp})");
        }

        /// <summary>
        /// 删除存档
        /// </summary>
        public void DeleteGame()
        {
            fileHandler.Delete(SAVE_FILE_NAME);
            currentSaveData = new SaveData();
            Debug.Log("[SaveManager] 存档已删除");
        }

        /// <summary>
        /// 新建游戏 —— 重置所有数据为默认值
        /// </summary>
        public void NewGame()
        {
            currentSaveData = new SaveData();
            DistributeDataToManagers();
            Debug.Log("[SaveManager] 新建游戏，数据已重置");
        }

        // ──────────────────────────────────
        //  生涯数据查询与累加
        // ──────────────────────────────────

        /// <summary>获取生涯累计总经验值</summary>
        public int GetTotalExperience()
        {
            return currentSaveData.totalExperience;
        }

        /// <summary>获取生涯累计金币</summary>
        public int GetTotalCoin()
        {
            return currentSaveData.coin;
        }

        /// <summary>
        /// 对局结算时调用：累加本局获得的经验到生涯总经验
        /// </summary>
        /// <param name="matchExp">本局获得的经验值</param>
        public void AddMatchExperience(int matchExp)
        {
            if (matchExp <= 0) return;
            currentSaveData.totalExperience += matchExp;
            Debug.Log($"[SaveManager] 生涯经验 +{matchExp}，累计 {currentSaveData.totalExperience}");
        }

        // ──────────────────────────────────
        //  数据收集（存档时调用）
        // ──────────────────────────────────

        private void CollectDataFromManagers()
        {
            // 玩家经济
            if (CoinManager.Instance != null)
                currentSaveData.coin = CoinManager.Instance.GetCoins();

            // 符文系统
            currentSaveData.equippedRuneId1 = RuneEquipmentSnapshot.EquippedRuneId1;
            currentSaveData.equippedRuneId2 = RuneEquipmentSnapshot.EquippedRuneId2;

            // 任务进度（这是存档的核心用途——任务需要跨对局累积）
            if (QuestTaskManager.Instance != null)
                currentSaveData.taskProgress = QuestTaskManager.Instance.GetAllProgress();

            // 累计统计
            if (MatchStatisticsManager.Instance != null)
            {
                var stats = MatchStatisticsManager.Instance.GetLifetimeStats();
                currentSaveData.lifetimeKills = stats.kills;
                currentSaveData.lifetimeDamage = stats.damage;
                currentSaveData.lifetimeMatches = stats.matches;
            }

            // 角色选择
            currentSaveData.selectedCharacterIndex = SelectedCharacterIndex;
        }

        // ──────────────────────────────────
        //  数据分发（读档时调用）
        // ──────────────────────────────────

        private void DistributeDataToManagers()
        {
            // 玩家经济
            if (CoinManager.Instance != null)
                CoinManager.Instance.SetCoins(currentSaveData.coin);

            // 符文系统
            RuneEquipmentSnapshot.SetBoth(currentSaveData.equippedRuneId1, currentSaveData.equippedRuneId2);

            // 任务进度
            if (QuestTaskManager.Instance != null)
                QuestTaskManager.Instance.SetAllProgress(currentSaveData.taskProgress);

            // 累计统计
            if (MatchStatisticsManager.Instance != null)
                MatchStatisticsManager.Instance.SetLifetimeStats(
                    currentSaveData.lifetimeKills,
                    currentSaveData.lifetimeDamage,
                    currentSaveData.lifetimeMatches
                );

            // 角色选择
            SelectedCharacterIndex = currentSaveData.selectedCharacterIndex;
        }

        // ──────────────────────────────────
        //  版本兼容
        // ──────────────────────────────────

        /// <summary>
        /// 当游戏更新后，旧存档可能缺少新字段，这里逐版本升级
        /// </summary>
        private void HandleVersionCompatibility()
        {
            // 空存档（极旧版本）
            if (string.IsNullOrEmpty(currentSaveData.saveVersion))
            {
                // 补全所有默认值
                if (currentSaveData.ownedCardIds == null)
                    currentSaveData.ownedCardIds = new List<string>();
                if (currentSaveData.ownedRuneIds == null)
                    currentSaveData.ownedRuneIds = new List<int>();
                if (currentSaveData.taskProgress == null)
                    currentSaveData.taskProgress = new Dictionary<int, int>();

                currentSaveData.saveVersion = "1.0.0";
            }

            // 后续版本升级示例：
            // if (currentSaveData.saveVersion == "1.0.0")
            // {
            //     // 1.0.0 → 1.1.0 新增了某个字段
            //     currentSaveData.xxx = 默认值;
            //     currentSaveData.saveVersion = "1.1.0";
            // }
        }

        // ──────────────────────────────────
        //  游戏退出时自动存档
        // ──────────────────────────────────

        void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}