using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Domain.Event;
using Domain.Pool;

namespace Infrastructure.FX.ImpactCannon
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
            StartCoroutine(DelayReturnToPool(0.5f));
        }

        protected virtual IEnumerator DelayReturnToPool(float delay)
        {
            yield return new WaitForSeconds(delay);
            transform.localScale = baseScale;
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateReturn(poolName, gameObject));
        }
    }
}