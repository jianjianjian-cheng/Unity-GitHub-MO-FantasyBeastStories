using UnityEngine;

namespace Controllers.Battle
{
    public interface IFIreBallProjectile
    {
        void SetTargetAndDamage(Transform target, float damage);
    }
}