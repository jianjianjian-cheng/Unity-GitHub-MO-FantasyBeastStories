namespace Core
{
    /// <summary>
    /// 音频类型枚举
    /// </summary>
    public enum AudioChannelType
    {
        /// <summary>背景音乐</summary>
        BGM,

        /// <summary>普通音效（攻击、技能、环境交互等）</summary>
        SFX,

        /// <summary>UI 音效（按钮点击、弹窗等）</summary>
        UI,

        /// <summary>环境音（风声、水流、篝火等）</summary>
        Ambient,
    }
}