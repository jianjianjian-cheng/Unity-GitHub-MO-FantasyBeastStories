using System.Collections;
using System.Collections.Generic;
using Manager;
using Trigger;
using UnityEngine;

namespace Charactors.Pets
{
    public class Charmander : PetsBase
    {
        GameObject FireFire;
        [SerializeField] private GameObject firefirePos; // 火焰生成位置
        [SerializeField] private GameObject attackFX;
        private ParticleSystem attackFXParticleSystem;
        [SerializeField] private SpawnPetsTrackRanger trackRanger;
        protected override void Awake()
        {
            base.Awake();
        }
        protected override void Update()
        {
            base.Update();
            targetEnemy = trackRanger.DepatchTargetEnemy();
            attackFXParticleSystem = attackFX.GetComponent<ParticleSystem>();
            TrackTarget(targetEnemy);
        }
        #region 状态机
        protected override void IdleEnter()
        {
            base.IdleEnter();
        }

        protected override void IdleStay()
        {
            base.IdleStay();
        }

        protected override void IdleExit()
        {
            base.IdleExit();
        }

        protected override void AttackEnter()
        {
            base.AttackEnter();
            Debug.Log("Charmander AttackEnter");
            attackFXParticleSystem.Play(true);
        }

        protected override void AttackStay()
        {
            base.AttackStay();
        }

        protected override void AttackExit()
        {
            base.AttackExit();
            attackFXParticleSystem.Stop();
        }
        protected override void RunEnter()
        {
            base.RunEnter();
        }

        protected override void RunStay()
        {
            base.RunStay();
        }

        protected override void RunExit()
        {
            base.RunExit();
        }
        #endregion
        private void TrackTarget(GameObject targetEnemy)
        {
            if (targetEnemy != null)
            {
                // 跟踪目标敌人,仅仅只在xz轴上旋转
                transform.LookAt(new Vector3(targetEnemy.transform.position.x, transform.position.y, targetEnemy.transform.position.z));
                if (Vector3.Distance(transform.position, targetEnemy.transform.position) > attackDistance)
                {
                    if (currentState != PetState.Run)
                        ChangeState(PetState.Run);
                }
                else
                {
                    if (currentState != PetState.Attack)
                        ChangeState(PetState.Attack);
                }
            }
            else
            {
                // 目标敌人不存在，切换到Idle状态
                ChangeState(PetState.Idle);
            }
        }
    }
}
