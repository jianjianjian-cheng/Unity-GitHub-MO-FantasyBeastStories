using System.Collections;
using System.Collections.Generic;
using Infrastructure.FX;
using UnityEngine;
using Domain.Pool;

namespace Infrastructure.FX.ImpactCannon
{
    public class ImpactCannonCommonWinter : ImpactCannonCommonBase
    {
        protected override void Awake()
        {
            baseScale = transform.localScale;
            SetPoolName();
        }

        protected override void SetPoolName()
        {
            poolName = ObjectPoolConst.ImpactCannonWinterPool;
        }
    }
}