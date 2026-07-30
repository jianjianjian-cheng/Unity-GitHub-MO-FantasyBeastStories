using System.Collections;
using System.Collections.Generic;
using Controllers.Battle;
using UnityEngine;
using Core;

namespace Controllers.Battle.ImpactCannon
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