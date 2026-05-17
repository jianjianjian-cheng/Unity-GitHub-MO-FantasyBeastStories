using System.Collections;
using System.Collections.Generic;
using Manager;
using UnityEngine;

namespace FX
{
    public class ImpactCannonCommonBase : MonoBehaviour
    {
        void OnEnable()
        {
            Invoke("ReturnToPool", 0.5f);
        }

        private void ReturnToPool()
        {
            ObjectPoolManager.instance.ReturnToPool(ObjectPoolConst.ImpactCannonCommonPool, gameObject);
        }
    }
}
