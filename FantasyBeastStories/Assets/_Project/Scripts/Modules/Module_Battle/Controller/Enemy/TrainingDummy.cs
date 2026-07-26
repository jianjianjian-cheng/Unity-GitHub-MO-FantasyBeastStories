using Core;
using UnityEngine;

namespace Controllers.Enemy
{
    public class TrainingDummy : EnemyBase
    {
        protected override void Start()
        {
            base.Start();
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            enemyData.currentState = EnemyState.Idle;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        // 不追踪玩家
        protected override void TrackPlayer()
        {
            PlayerTarget = null;
        }

        // 始终保持在Idle状态
        protected override void UpdateIdle()
        {
            TrackPlayer();
        }

        // 防止进入其他状态（虽然TrackPlayer返回null后，基类UpdateIdle不会切换状态，但以防万一）
        protected override void UpdateRun()
        {
            TransitionToState(EnemyState.Idle);
        }

        protected override void UpdateAttack()
        {
            TransitionToState(EnemyState.Idle);
        }

        // 只记录伤害，不掉血
        public override void TakeDamage(DamageEventArgs damageEventArgs)
        {
            Debug.Log($"测试人偶受到 {damageEventArgs.finalDamageValue} 点伤害");
        }
    }
}