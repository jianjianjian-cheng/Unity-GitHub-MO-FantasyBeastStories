using UnityEngine;
using Controllers.PowerUp;

namespace Controllers.PowerUp
{
    /// <summary>
    /// 道具效果抽象基类
    /// 提供通用功能和默认实现
    /// </summary>
    public abstract class PowerUpEffectBase : MonoBehaviour, IPowerUpEffect
    {
        [Header("效果配置")]
        [SerializeField] protected string effectName = "未命名效果";
        [TextArea(3, 5)]
        [SerializeField] protected string description = "效果描述";

        [Header("特效")]
        [SerializeField] protected GameObject collectVFX;
        [SerializeField] protected AudioClip collectSFX;

        public abstract void Execute(GameObject player);

        public virtual string GetEffectName() => effectName;
        public virtual string GetDescription() => description;

        protected virtual void PlayCollectEffects(Vector3 position)
        {
            if (collectVFX != null)
                Instantiate(collectVFX, position, Quaternion.identity);

            if (collectSFX != null)
                AudioSource.PlayClipAtPoint(collectSFX, position);
        }
    }
}