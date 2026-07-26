using UnityEngine;

namespace Controllers.Combat
{
    public interface IFIreBallProjectile
    {
        void SetTargetAndDamage(Transform target, float damage);
    }
}