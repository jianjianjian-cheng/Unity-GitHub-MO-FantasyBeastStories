namespace Controllers.Character
{
    /// <summary>
    /// 属性查询/注册时使用的键名常量
    /// 替代原 EventNames 中的 PlayerAttribute_Main / PlayerAttribute_Current
    /// </summary>
    public static class AttributeKeyConst
    {
        /// <summary> 主玩家属性键 </summary>
        public const string Main = "MainPlayer";

        /// <summary> 当前玩家属性键 </summary>
        public const string Current = "CurrentPlayer";
    }
}