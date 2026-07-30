using UnityEngine;

namespace Controllers.Battle
{
    public interface IFireBallBase
    {
        void SetTarget(GameObject target);
        void HandleEnemyCollisionEnter(Collider enemy);
        void HandleEnemyCollisionStay(Collider enemy);
        void HandleEnemyCollisionExit(Collider enemy);
    }
}