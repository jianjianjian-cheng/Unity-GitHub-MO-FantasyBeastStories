using Domain.Manager;
using UnityEngine;
using Domain.Event;
using Domain.Pool;

namespace Presentation.Other
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
