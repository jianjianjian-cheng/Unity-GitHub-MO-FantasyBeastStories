using UnityEngine;

namespace Domain.Combat.FX
{
    public interface IFIreBallProjectile
    {
        void SetTargetAndDamage(Transform target, float damage);
    }
}