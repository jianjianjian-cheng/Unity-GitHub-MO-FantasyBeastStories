using System.Collections;
using System.Collections.Generic;
using FX;
using Manager;
using UnityEngine;

public class ImpactCannonCommonGrass : ImpactCannonCommonBase
{
    protected override void Awake()
    {
        baseScale = transform.localScale;
        SetPoolName();
    }

    protected override void SetPoolName()
    {
        poolName = ObjectPoolConst.ImpactCannonGrassPool;
    }

    protected override void ReturnToPool()
    {
        base.ReturnToPool();
    }
}
