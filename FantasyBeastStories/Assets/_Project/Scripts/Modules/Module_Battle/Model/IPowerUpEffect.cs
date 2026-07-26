using UnityEngine;

namespace Controllers.PowerUp
{
    /// <summary>
    /// 道具效果接口 - 策略模式核心
    /// 每种道具效果实现此接口，便于扩展新道具
    /// </summary>
    public interface IPowerUpEffect
    {
        void Execute(GameObject player);
        string GetEffectName();
        string GetDescription();
    }
}