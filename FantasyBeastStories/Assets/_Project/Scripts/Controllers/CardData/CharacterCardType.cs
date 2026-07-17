namespace Controllers.CardData
{
    /// <summary>
    /// 角色卡牌类型标识符
    /// 替代原 EventNames 中的 OnReceiveCard_XXX 常量
    /// </summary>
    public static class CharacterCardType
    {
        /// <summary> WizardBoy 角色卡牌 </summary>
        public const string WizardBoy = "OnReceiveCard_WizardBoy";

        /// <summary> BingNv 角色卡牌 </summary>
        public const string BingNv = "OnReceiveCard_BingNv";
    }
}