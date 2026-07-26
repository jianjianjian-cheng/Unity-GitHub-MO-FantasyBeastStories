namespace UI.Framework.Utils
{
    /// <summary>
    /// 红点系统 Key 常量。使用点分路径表达父子聚合关系。
    /// 父节点在任一子节点激活时自动激活。
    /// </summary>
    public static class RedDotKeys
    {
        // ===== 根聚合节点 =====
        public const string Root = "root";

        // ===== 任务系统 =====
        /// <summary>任务导航按钮（聚合）</summary>
        public const string Mission     = "mission";
        /// <summary>有新任务进度未查看</summary>
        public const string MissionNew  = "mission.new";

        // ===== 符文背包 =====
        /// <summary>符文导航按钮（聚合）</summary>
        public const string Rune        = "rune";
        /// <summary>有新符文未查看</summary>
        public const string RuneNew     = "rune.new";

        // ===== 商店 =====
        /// <summary>商店导航按钮（聚合）</summary>
        public const string Shop        = "shop";
    }
}
