using System.Collections;
using System.Collections.Generic;
using FX;
using Manager;
using UnityEngine;

public class ImpactCannonCommonLighten : ImpactCannonCommonBase
{
    protected override void Awake()
    {
        baseScale = transform.localScale;
        SetPoolName();
    }

    protected override void SetPoolName()
    {
        poolName = ObjectPoolConst.ImpactCannonLightenPool;
    }

    protected override void ReturnToPool()
    {
        base.ReturnToPool();
    }
}
