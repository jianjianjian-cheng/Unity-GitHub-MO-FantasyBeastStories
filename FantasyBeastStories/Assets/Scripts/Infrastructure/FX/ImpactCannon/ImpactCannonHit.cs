using Domain.Manager;
using Photon.Pun;
using UnityEngine;
using Domain.Event;

namespace Infrastructure.FX.ImpactCannon
{
    public class ImpactCannonHit : MonoBehaviourPun
    {
        [SerializeField]
        private string poolName;

        void OnEnable()
        {
            Invoke(nameof(ReturnPool), 2f);
        }

        protected virtual void ReturnPool()
        {
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateReturn(poolName, gameObject));
        }
    }
}
