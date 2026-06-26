using Domain.Event;
using Domain.Pool;
using UnityEngine;

namespace Domain.Data
{
    public class AttackToken
    {
        public GameObject hitCollider;
        public GameObject vfxEffect;
        public string vfxPoolName;

        public void RecycleAll()
        {
            if (hitCollider != null)
                EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                    PoolOperationData.CreateReturn(ObjectPoolConst.ImpactCannonTriggerPool, hitCollider));
            if (vfxEffect != null)
                EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                    PoolOperationData.CreateReturn(vfxPoolName, vfxEffect));

            hitCollider = null;
            vfxEffect = null;
        }
    }
}
