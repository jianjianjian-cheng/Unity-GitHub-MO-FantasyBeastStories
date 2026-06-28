using Domain.Pool;
using UnityEngine;

namespace Infrastructure.FX.ImpactCannon
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