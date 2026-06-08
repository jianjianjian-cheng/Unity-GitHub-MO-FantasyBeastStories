using System.Collections;
using System.Collections.Generic;
using Manager;
using Photon.Pun;
using UnityEngine;

namespace FX
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
            ObjectPoolManager.instance.ReturnToPool(poolName, gameObject);
        }
    }
}
