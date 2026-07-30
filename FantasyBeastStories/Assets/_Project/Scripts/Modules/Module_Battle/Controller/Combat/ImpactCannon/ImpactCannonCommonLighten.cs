using System.Collections;
using System.Collections.Generic;
using Core;
using Controllers.Battle;
using UnityEngine;

namespace Controllers.Battle.ImpactCannon
{
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
    }
}