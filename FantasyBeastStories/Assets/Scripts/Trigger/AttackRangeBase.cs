using System.Collections;
using System.Collections.Generic;
using Atttibute;
using Enemies;
using FX;
using Manager;
using Photon.CastSciprt;
using Trigger;
using UnityEngine;

namespace Trigger
{
    public class AttackRangeBase : TriggerBase
    {
        private AttributePlayerBase attributePlayerBase;
        private CastNetwork _networkCaster;
        private bool isTest = false; // 是否测试模式
        private List<GameObject> gameObjects = new List<GameObject>();
        private GameObject targetEnemy;
        private float attackTimer;

        [SerializeField] private float offsetY = 1f;

        [SerializeField] private float attackInterval = 2f;
        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            _networkCaster = GetComponent<CastNetwork>();
            isTest = GameManager.instance != null && GameManager.isTest;
            if (_networkCaster == null && !isTest)
            {
                _networkCaster = gameObject.AddComponent<CastNetwork>();
            }
            attributePlayerBase = EventManager.instance.GetLocalPlayerAttribute(EventNames.PlayerAttribute_Main);
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
            Attack();
        }
        /// <summary>
        /// 攻击逻辑：检查目标、控制间隔、发射火球
        /// </summary>
        private void Attack()
        {
            UpdateEnemyTarget();
            if (targetEnemy == null) return;

            // 攻击间隔控制
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
                return;
            }
            attackTimer = attackInterval;

            // 计算发射位置和方向
            Vector3 pos = new Vector3(transform.position.x, transform.position.y + offsetY, transform.position.z);
            Vector3 targetPos = new Vector3(targetEnemy.transform.position.x, pos.y, targetEnemy.transform.position.z);
            Vector3 direction = (targetEnemy.transform.position - pos).normalized;

            if (isTest)
            {
                // ===== 测试模式：纯本地生成 =====
                SpawnFireballLocal(pos, direction, isMine: true);
            }
            else
            {
                // ===== 联机模式：本地先行 + 网络广播 =====

                // 步骤1：本地立即生成火球（零延迟，手感最佳）
                SpawnFireballLocal(pos, direction, isMine: true);

                // 步骤2：通知其他玩家生成火球
                _networkCaster?.RequestFireball(pos, direction, 10f);
            }
        }

        /// <summary>
        /// 纯本地生成火球（视觉特效 + 碰撞触发器）
        /// </summary>
        /// <param name="spawnPos">发射位置</param>
        /// <param name="direction">发射方向</param>
        /// <param name="isMine">是否由本炮塔发射（决定是否负责伤害判定）</param>
        private void SpawnFireballLocal(Vector3 spawnPos, Vector3 direction, bool isMine = true)
        {
            string visualPool = isTest ? ObjectPoolConst.TestPool : ObjectPoolConst.ImpactCannonCommonPool;
            string triggerPool = ObjectPoolConst.ImpactCannonTriggerPool;

            // 1. 生成视觉特效（纯表现，不参与逻辑）
            GameObject visualObj = ObjectPoolManager.instance.GetFromPoolAndActivate(visualPool, spawnPos);
            if (visualObj != null)
            {
                visualObj.GetComponentInChildren<ParticleSystem>()?.Play();
                visualObj.transform.rotation = Quaternion.LookRotation(direction);
            }

            // 2. 生成碰撞触发器（负责物理移动和伤害判定）
            GameObject triggerObj = ObjectPoolManager.instance.GetFromPoolAndActivate(triggerPool, spawnPos);
            if (triggerObj != null)
            {
                ImpactCannon cannon = triggerObj.GetComponent<ImpactCannon>();
                if (cannon == null)
                {
                    cannon = triggerObj.AddComponent<ImpactCannon>();
                }
                // 传入 isMine 参数，决定这个火球是否负责伤害判定
                cannon.StartShoot(direction, isMine);
                cannon.GetAttributeFromPlayer(attributePlayerBase);
            }
        }

        //寻找最近的敌人
        private void UpdateEnemyTarget()
        {
            if (gameObjects.Count == 0)
            {
                targetEnemy = null;
                return;
            }

            // 清理无效目标（已销毁/已死亡/非敌人）
            for (int i = gameObjects.Count - 1; i >= 0; i--)
            {
                var enemyGo = gameObjects[i];
                if (enemyGo == null)
                {
                    gameObjects.RemoveAt(i);
                    continue;
                }

                var enemyBase = enemyGo.GetComponent<EnemyBase>();
                if (enemyBase == null || enemyBase.GetIsDie())
                {
                    gameObjects.RemoveAt(i);
                }
            }

            if (gameObjects.Count == 0)
            {
                targetEnemy = null;
                return;
            }

            float minDistance = float.MaxValue;
            GameObject closestEnemy = null;
            foreach (GameObject enemy in gameObjects)
            {
                if (enemy == null)
                    continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }

            targetEnemy = closestEnemy;
        }

        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            gameObjects.Add(other.gameObject);
        }

        public override void OnTriggerStay(Collider other)
        {
            base.OnTriggerStay(other);
            if (other.gameObject.GetComponent<EnemyBase>() == null)
            {
                gameObjects.Remove(other.gameObject);
                return;
            }
            if (other.gameObject.GetComponent<EnemyBase>().GetIsDie())
            {
                gameObjects.Remove(other.gameObject);
            }
        }

        public override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
            gameObjects.Remove(other.gameObject);
        }
    }
}
