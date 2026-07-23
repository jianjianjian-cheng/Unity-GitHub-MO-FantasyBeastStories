namespace Core
{
    /// <summary>
    /// 可存档接口。任何需要存档的系统实现此接口，
    /// 在 SaveManager 中注册后，存档/读档时自动被调用。
    /// 新增存档系统只需实现此接口并注册，无需修改 SaveManager。
    /// </summary>
    public interface ISaveable
    {
        /// <summary>唯一标识符，用于日志和调试</summary>
        string SaveId { get; }

        /// <summary>存档：从当前运行时状态写入到 SaveData</summary>
        void OnSave(SaveData data);

        /// <summary>读档：从 SaveData 恢复到运行时状态</summary>
        void OnLoad(SaveData data);
    }
}
