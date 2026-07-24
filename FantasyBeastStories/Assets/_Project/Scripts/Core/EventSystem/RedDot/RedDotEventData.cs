namespace Core.Channels.RedDot
{
    /// <summary>
    /// 红点事件数据：携带节点 Key 与激活状态。
    /// </summary>
    public struct RedDotEventData
    {
        /// <summary>红点节点 Key（参见 RedDotKeys）</summary>
        public string Key;

        /// <summary>是否激活</summary>
        public bool IsActive;

        public RedDotEventData(string key, bool isActive)
        {
            Key = key;
            IsActive = isActive;
        }

        public override string ToString() => $"RedDot({Key}={IsActive})";
    }
}
