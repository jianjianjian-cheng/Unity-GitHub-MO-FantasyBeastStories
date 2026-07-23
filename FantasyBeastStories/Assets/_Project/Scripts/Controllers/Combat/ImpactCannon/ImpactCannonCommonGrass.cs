using System.Collections;
using System.Collections.Generic;
using Controllers.Combat;
using UnityEngine;
using Core;

namespace Controllers.Combat.ImpactCannon
{
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
    }
}