// 基类，放在 Domain.CardData 命名空间下
namespace Domain.CardData
{
    [System.Serializable]
    public class CardConfigBase
    {
        public string Name;
        public string Content;
        public int Value;
        public string Quality;
    }
}
