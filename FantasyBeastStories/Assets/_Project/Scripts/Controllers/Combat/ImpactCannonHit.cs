using Photon.Pun;
using UnityEngine;
using Core;

namespace Controllers.Combat.ImpactCannon
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
            PoolHelper.Return(poolName, gameObject);
        }
    }
}