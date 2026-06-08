using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using UnityEngine;

namespace FX
{
    public class ImpactCannonCommonBase : MonoBehaviour
    {
        protected string poolName;
        protected Vector3 baseScale;

        protected virtual void Awake()
        {
            SetPoolName();
            baseScale = transform.localScale;
        }

        protected virtual void SetPoolName()
        {
            poolName = ObjectPoolConst.ImpactCannonCommonPool;
        }

        protected virtual void OnEnable()
        {
            Invoke("ReturnToPool", 0.5f);
        }

        protected virtual void ReturnToPool()
        {
            transform.localScale = baseScale;
            ObjectPoolManager.instance.ReturnToPool(poolName, gameObject);
        }
    }
}
