using UnityEngine;

namespace Controllers.Combat
{
    public interface IFireBallBase
    {
        void SetTarget(GameObject target);
        void HandleEnemyCollisionEnter(Collider enemy);
        void HandleEnemyCollisionStay(Collider enemy);
        void HandleEnemyCollisionExit(Collider enemy);
    }
}