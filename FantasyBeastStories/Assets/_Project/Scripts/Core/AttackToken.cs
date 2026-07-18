using Core;
using Core;
using UnityEngine;

namespace Core
{
    public class AttackToken
    {
        public GameObject hitCollider;
        public GameObject vfxEffect;
        public string vfxPoolName;

        public void RecycleAll()
        {
            if (hitCollider != null)
                PoolHelper.Return(ObjectPoolConst.ImpactCannonTriggerPool, hitCollider);
            if (vfxEffect != null)
                PoolHelper.Return(vfxPoolName, vfxEffect);

            hitCollider = null;
            vfxEffect = null;
        }
    }
}
