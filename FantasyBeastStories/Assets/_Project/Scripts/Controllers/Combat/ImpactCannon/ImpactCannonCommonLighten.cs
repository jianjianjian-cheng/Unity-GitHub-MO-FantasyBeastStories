using System.Collections;
using System.Collections.Generic;
using Core;
using Controllers.Combat;
using UnityEngine;

namespace Controllers.Combat.ImpactCannon
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