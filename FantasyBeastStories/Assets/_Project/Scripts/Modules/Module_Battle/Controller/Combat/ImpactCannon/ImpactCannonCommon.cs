using Core;
using UnityEngine;

namespace Controllers.Battle.ImpactCannon
{
    public class ImpactCannonCommon : ImpactCannonCommonBase
    {
        protected override void Awake()
        {
            baseScale = transform.localScale;
            SetPoolName();
        }

        protected override void SetPoolName()
        {
            poolName = ObjectPoolConst.ImpactCannonCommonPool;
        }
    }
}