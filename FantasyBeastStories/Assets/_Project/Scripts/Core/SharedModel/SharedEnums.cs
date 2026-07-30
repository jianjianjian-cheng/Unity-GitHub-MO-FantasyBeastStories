namespace Core.SharedModel
{
    /// <summary>
    /// 元素类型枚举
    /// </summary>
    public enum Element
    {
        Common,
        Lightning,
        Winter,
        Grass,
        Fire,
    }

    /// <summary>
    /// 敌人状态枚举
    /// </summary>
    public enum EnemyState
    {
        Idle,
        Run,
        Attack,
        Die,
    }

    /// <summary>
    /// 敌人上报类型枚举
    /// </summary>
    public enum EnemyReportType
    {
        Kill,
        EscortArrive,
    }

    /// <summary>
    /// 卡牌品质枚举
    /// </summary>
    public enum CardQuality
    {
        Normal,
        Epic,
        Legend
    }

    /// <summary>
    /// 卡牌范围枚举
    /// </summary>
    public enum CardScope
    {
        Public,
        Exclusive
    }
}
