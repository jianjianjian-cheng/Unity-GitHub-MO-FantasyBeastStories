using FX;
using Manager;
using Other;
using Photon.CastSciprt;
using UnityEngine;

namespace Trigger
{
    /// <summary>
    /// 巫师男孩攻击范围：发射火球攻击
    /// </summary>
    public class AttackRange_WizardBoy : AttackRangeBase
    {
        private CastNetwork _networkCaster;
        private bool isTest = false;

        [Header("WizardBoy Settings")]
        [SerializeField]
        private float projectileSpeed = 10f;

        public override void Start()
        {
            base.Start();

            _networkCaster = GetComponent<CastNetwork>();
            isTest = GameManager.instance != null && GameManager.isTest;

            if (_networkCaster == null && !isTest)
            {
                _networkCaster = gameObject.AddComponent<CastNetwork>();
            }
        }

        /// <summary>
        /// 实现具体的攻击逻辑：发射火球
        /// </summary>
        protected override void PerformAttack()
        {
            Vector3 pos = GetSpawnPosition();
            Vector3 direction = GetTargetDirection();

            if (isTest)
            {
                // 测试模式：纯本地生成
                SpawnFireballLocal(pos, direction, isMine: true);
            }
            else
            {
            // 联机模式：本地先行 + 网络广播
            SpawnFireballLocal(pos, direction, isMine: true);
            _networkCaster?.RequestFireball(
                pos,
                direction,
                projectileSpeed,
                attributePlayerBase.GetCurrentElement()
            );
            }
        }

        /// <summary>
        /// 纯本地生成发射物（视觉特效 + 碰撞触发器）
        /// </summary>
        private void SpawnFireballLocal(Vector3 spawnPos, Vector3 direction, bool isMine = true)
        {
            string visualPool = null;
            string triggerPool = ObjectPoolConst.ImpactCannonTriggerPool;

            // 1. 生成视觉特效
            switch (attributePlayerBase.GetCurrentElement())
            {
                case Element.Common:
                    visualPool = ObjectPoolConst.ImpactCannonCommonPool;
                    break;
                case Element.Lightning:
                    visualPool = ObjectPoolConst.ImpactCannonLightenPool;
                    break;
                case Element.Winter:
                    visualPool = ObjectPoolConst.ImpactCannonWinterPool;
                    break;
                case Element.Grass:
                    visualPool = ObjectPoolConst.ImpactCannonGrassPool;
                    break;
            }
            GameObject visualObj = ObjectPoolManager.instance.GetFromPoolAndActivate(
                visualPool,
                spawnPos
            );

            // 2. 生成碰撞触发器
            GameObject triggerObj = ObjectPoolManager.instance.GetFromPoolAndActivate(
                triggerPool,
                spawnPos
            );

            // Vector3 baseScale = visualObj.transform.localScale;
            // Vector3 triggerScale = triggerObj.transform.localScale;
            // if (isCharged)
            // {
            //     visualObj.transform.localScale = baseScale * 1.5f;
            //     triggerObj.transform.localScale = triggerScale * 1.5f;
            // }
            // 创建令牌并绑定
            AttackToken token = new AttackToken
            {
                hitCollider = triggerObj,
                vfxEffect = visualObj,
                vfxPoolName = visualPool,
            };

            if (visualObj != null)
            {
                var particle = visualObj.GetComponentInChildren<ParticleSystem>();
                particle?.Play();
                visualObj.transform.rotation = Quaternion.LookRotation(direction);
            }

            if (triggerObj != null)
            {
                ImpactCannon cannon = triggerObj.GetComponent<ImpactCannon>();
                if (cannon == null)
                {
                    cannon = triggerObj.AddComponent<ImpactCannon>();
                }
                // 绑定令牌
                cannon.SetToken(token);
                cannon.SetAttributeFromPlayer(attributePlayerBase);
                cannon.StartShoot(direction, isMine);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            // 可以添加WizardBoy特有的可视化
            if (targetEnemy != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(targetEnemy.transform.position, 0.5f);
            }
        }
    }
}
