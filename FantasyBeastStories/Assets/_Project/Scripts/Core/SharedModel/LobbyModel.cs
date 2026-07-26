namespace Core.SharedModel
{
    /// <summary>
    /// 大厅/连接模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有：
    /// - 连接状态标记 (isAutoCreate / isJoiningRoom / isQuittingToMenu)
    /// - 待加入房间名 (pendingRoomName)
    /// - 测试模式标记
    ///
    /// 外部依赖（PhotonNetwork / SceneManager / Coroutine / UI）
    /// 由 Controller (Launcher) 处理，Model 只管理状态数据。
    /// </summary>
    public class LobbyModel
    {
        // ──────────────────────────────────
        //  连接状态
        // ──────────────────────────────────

        public bool IsAutoCreate { get; set; }
        public bool IsJoiningRoom { get; set; }
        public bool IsQuittingToMenu { get; set; }
        public string PendingRoomName { get; set; } = "";

        // ──────────────────────────────────
        //  场景状态
        // ──────────────────────────────────

        public bool IsTest { get; private set; }

        public void SetIsTest(bool value) => IsTest = value;

        // ──────────────────────────────────
        //  状态重置
        // ──────────────────────────────────

        /// <summary>重置所有连接状态（返回主菜单时调用）</summary>
        public void ResetConnectionState()
        {
            IsQuittingToMenu = false;
            PendingRoomName = "";
            IsJoiningRoom = false;
            IsAutoCreate = false;
        }

        // ──────────────────────────────────
        //  房间切换流程
        // ──────────────────────────────────

        /// <summary>
        /// 开始切换房间流程。
        /// 返回 true 表示需要先离开当前房间。
        /// </summary>
        public bool TryStartRoomSwitch(string newRoomName)
        {
            PendingRoomName = newRoomName;
            IsJoiningRoom = true;
            return true;
        }

        /// <summary>消费待加入房间名，返回房间名并清空</summary>
        public string ConsumePendingRoomName()
        {
            string name = PendingRoomName;
            IsJoiningRoom = false;
            PendingRoomName = "";
            return name;
        }

        /// <summary>加入房间失败时重置状态</summary>
        public void ResetRoomJoinFailure()
        {
            IsJoiningRoom = false;
            PendingRoomName = "";
        }
    }
}
