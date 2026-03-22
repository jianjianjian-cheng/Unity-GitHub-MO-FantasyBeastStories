using System.Collections;
using System.Collections.Generic;
using Manager;
using Photon.Pun;
using UnityEngine;

namespace FX
{
    public class ImpactCannonHit : MonoBehaviourPun
    {
        void OnEnable()
        {
            Invoke(nameof(ReturnPool), 2f);
        }

        protected virtual void ReturnPool()
        {
            ObjectPoolManager.instance.ReturnToPool("ImpactCannonHitCommonPool", gameObject);
        }
    }
}
