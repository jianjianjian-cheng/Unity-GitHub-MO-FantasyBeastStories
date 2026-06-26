using UnityEngine;

namespace Domain.Combat.FX
{
    public interface IFireBallBase
    {
        void SetTarget(GameObject target);
        void HandleEnemyCollisionEnter(Collider enemy);
        void HandleEnemyCollisionStay(Collider enemy);
        void HandleEnemyCollisionExit(Collider enemy);
    }
}