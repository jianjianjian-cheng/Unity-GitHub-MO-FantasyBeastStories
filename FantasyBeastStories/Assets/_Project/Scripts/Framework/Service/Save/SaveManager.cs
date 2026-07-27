using System.Collections.Generic;
using System.Linq;
using Core;
using Controllers.Rune;
using UnityEngine;
using Core.Save;

namespace Managers
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
  public class SaveManager : MonoBehaviour, ISaveable
  {
    private static SaveManager _instance;
    
    [Header("存档配置")]
    [SerializeField] private bool useEncryption = false;

    private FileDataHandler fileHandler;
    private SaveData currentSaveData;

    /// <summary>已注册的可存档系统列表</summary>
    private List<ISaveable> _saveables = new List<ISaveable>();

    private const string SAVE_FILE_NAME = "save";
    private const string SAVE_VERSION = "1.0.0";

    public static int SelectedCharacterIndex { get; set; } = 0;

    // ──────────────────────────────────
    //  ISaveable 注册/注销
    // ──────────────────────────────────

    /// <summary>注册一个可存档系统。新增系统只需调用此方法，SaveManager 自动在存档/读档时调用。</summary>
    public void RegisterSaveable(ISaveable saveable)
    {
      if (saveable == null || _saveables.Contains(saveable)) return;
      _saveables.Add(saveable);
      Debug.Log($"[SaveManager] 注册存档系统: {saveable.SaveId}");
    }

    /// <summary>注销一个可存档系统。</summary>
    public void UnregisterSaveable(ISaveable saveable)
    {
      if (saveable == null) return;
      _saveables.Remove(saveable);
    }

    // ──────────────────────────────────
    //  SaveManager 自身的 ISaveable 实现（处理生涯经验 + 角色选择）
    // ──────────────────────────────────

    public string SaveId => "SaveManager";

    public void OnSave(SaveData data)
    {
      data.totalExperience = currentSaveData.totalExperience;
      data.selectedCharacterIndex = SelectedCharacterIndex;
    }

    public void OnLoad(SaveData data)
    {
      currentSaveData.totalExperience = data.totalExperience;
      SelectedCharacterIndex = data.selectedCharacterIndex;
    }

    // ──────────────────────────────────
    //  单例生命周期
    // ──────────────────────────────────

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
        fileHandler = new FileDataHandler(UnityEngine.Application.persistentDataPath);
        currentSaveData = new SaveData();
    }

    void Start()
    {
      // SaveManager 自身也注册为 ISaveable（处理生涯经验 + 角色选择）
      RegisterSaveable(this);

      // 启动时自动加载存档，确保 SelectedCharacterIndex 等数据从文件恢复
      if (HasSave())
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

      // 校验存档数据（热更新后可能有 ID 被删除）
      ValidateSaveData();

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

    /// <summary>获取存档中所有任务的累计进度</summary>
    public Dictionary<int, int> GetTaskProgress()
    {
      return currentSaveData.taskProgress;
    }

    // ──────────────────────────────────
    //  数据收集（遍历所有已注册的 ISaveable）
    // ──────────────────────────────────

    private void CollectDataFromManagers()
    {
      foreach (var saveable in _saveables)
      {
        try
        {
          saveable.OnSave(currentSaveData);
        }
        catch (System.Exception e)
        {
          Debug.LogError($"[SaveManager] 存档失败: {saveable.SaveId} | {e.Message}");
        }
      }
    }

    // ──────────────────────────────────
    //  数据分发（遍历所有已注册的 ISaveable）
    // ──────────────────────────────────

    private void DistributeDataToManagers()
    {
      foreach (var saveable in _saveables)
      {
        try
        {
          saveable.OnLoad(currentSaveData);
        }
        catch (System.Exception e)
        {
          Debug.LogError($"[SaveManager] 读档失败: {saveable.SaveId} | {e.Message}");
        }
      }
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
    //  存档数据校验
    // ──────────────────────────────────

    /// <summary>
    /// 热更新后部分 ID 可能已被移除，需校验存档引用的合法性。
    /// 注意：不能在此处同步加载远程 SO（WaitForCompletion 会死锁主线程），
    /// 只做基本合理性检查，数据库级校验由运行时自然处理（GetRuneById == null 时跳过）。
    /// </summary>
    private void ValidateSaveData()
    {
      // ── 符文装备槽：确保 ID 合法 ──
      if (currentSaveData.equippedRuneId1 < -1)
      {
        Debug.LogWarning($"[SaveManager] 装备槽1 ID={currentSaveData.equippedRuneId1}非法，重置");
        currentSaveData.equippedRuneId1 = -1;
      }
      if (currentSaveData.equippedRuneId2 < -1)
      {
        Debug.LogWarning($"[SaveManager] 装备槽2 ID={currentSaveData.equippedRuneId2}非法，重置");
        currentSaveData.equippedRuneId2 = -1;
      }

      // ── 清理负数 ID ──
      if (currentSaveData.ownedRuneIds != null)
        currentSaveData.ownedRuneIds.RemoveAll(id => id < 0);

      if (currentSaveData.taskProgress != null)
      {
        var invalidKeys = currentSaveData.taskProgress.Keys
            .Where(id => id < 0).ToList();
        foreach (var id in invalidKeys)
          currentSaveData.taskProgress.Remove(id);
      }

      if (currentSaveData.shopPurchaseRecords != null)
      {
        var invalidKeys = currentSaveData.shopPurchaseRecords.Keys
            .Where(id => id < 0).ToList();
        foreach (var id in invalidKeys)
          currentSaveData.shopPurchaseRecords.Remove(id);
      }
    }
    // ──────────────────────────────────

    void OnApplicationQuit()
    {
      SaveGame();
    }
  }
}