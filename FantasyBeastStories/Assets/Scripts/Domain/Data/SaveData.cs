using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Domain.Data
{
    /// <summary>
    /// 存档数据模型。只包含字段，不包含任何 IO 逻辑。
    /// Application/SaveManager 负责填充/读取此对象。
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        // ====== 元数据 ======
        public string saveVersion;       // 存档版本号，用于版本兼容
        public string saveTimestamp;     // 保存时间

        // ====== 玩家经济 ======
        public int coin;                 // 金币（替换 CoinManager 的 PlayerPrefs）

        // ====== 生涯总经验（每局结算时累加） ======
        public int totalExperience;      // 生涯累计总经验，ExperienceManager 里的局内变量用完即弃

        // ====== 卡牌收集 ======
        public List<string> ownedCardIds = new List<string>();   // 拥有的卡牌 ID 列表

        // ====== 符文系统 ======
        public int equippedRuneId1 = -1;         // 装备槽1 符文 ID（-1 表示未装备）
        public int equippedRuneId2 = -1;         // 装备槽2 符文 ID（-1 表示未装备）
        public List<int> ownedRuneIds = new List<int>();      // 拥有的所有符文 ID

        // ====== 任务进度 ======
        // key=任务ID, value=累计完成次数
        public Dictionary<int, int> taskProgress = new Dictionary<int, int>();

        // ====== 商店系统 ======
        // key=符文ID, value=已购买数量（用于限量商品库存管理）
        public Dictionary<int, int> shopPurchaseRecords = new Dictionary<int, int>();

        // ====== 累计统计 ======
        public int lifetimeKills;            // 累计击杀（来自 MatchStatisticsManager）
        public int lifetimeDamage;           // 累计伤害
        public int lifetimeMatches;          // 总局数

        // ====== 游戏设置 ======
        public float musicVolume = 0.8f;     // 音乐音量
        public float sfxVolume = 1.0f;       // 音效音量
        public int qualityLevel = 2;         // 画质等级

        public int selectedCharacterIndex = 0;// 选中的角色索引，默认WizardBoy
    }
}