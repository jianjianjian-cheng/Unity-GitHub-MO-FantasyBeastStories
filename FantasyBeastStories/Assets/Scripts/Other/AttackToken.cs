using Manager;
using UnityEngine;

namespace Other
{
    public class AttackToken
    {
        public GameObject hitCollider;   // 碰撞器实例
        public GameObject vfxEffect;     // 特效实例

        // 同时回收两者
        public void RecycleAll()
        {
            if (hitCollider != null)
                ObjectPoolManager.instance.ReturnToPool(ObjectPoolConst.ImpactCannonTriggerPool, hitCollider);
            if (vfxEffect != null)
                ObjectPoolManager.instance.ReturnToPool(ObjectPoolConst.ImpactCannonCommonPool, vfxEffect);

            hitCollider = null;
            vfxEffect = null;
        }
    }
}