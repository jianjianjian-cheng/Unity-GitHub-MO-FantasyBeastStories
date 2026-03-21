using System.Collections;
using System.Collections.Generic;
using Enemies;
using FX;
using Manager;
using Trigger;
using UnityEngine;

namespace Trigger
{
    public class AttackRangeBase : TriggerBase
    {
        [SerializeField] private bool isTest = false; // 是否测试模式
        private List<GameObject> gameObjects = new List<GameObject>();
        private GameObject targetEnemy;
        private float attackTimer;

        [SerializeField] private float offsetY = 1f;

        // private GameObject ImpactCannonBase;
        // private string impactCannonBasePath = "Photon/PhotonUnityNetworking/Resources/FX/ImpactCannon";
        // private string impactCannonAttribute = "Common";
        [SerializeField] private float attackInterval = 2f;
        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
            Attack();
        }
        private void Attack()
        {
            UpdateEnemyTarget();
            if (targetEnemy == null) return;
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
                return;
            }
            attackTimer = attackInterval;
            Vector3 pos = new Vector3(transform.position.x, transform.position.y + offsetY, transform.position.z);
            if (isTest)
            {
                GameObject gameObject1 = ManagerBase.instance.GetComponent<ObjectPoolManager>().GetFromPoolAndActivate(ObjectPoolConst.TestPool, pos);
                if (targetEnemy != null)
                {
                    gameObject1.GetComponentInChildren<ParticleSystem>().Play();
                    //仅仅在x和z轴上面朝向敌人
                    Vector3 targetPos = new Vector3(targetEnemy.transform.position.x, pos.y, targetEnemy.transform.position.z);
                    gameObject1.transform.LookAt(targetPos);
                }
                return;
            }
            //触发攻击
            GameObject gameObject = ManagerBase.instance.GetComponent<ObjectPoolManager>().GetFromPoolAndActivate(ObjectPoolConst.ImpactCannonCommonPool, pos);
            if (targetEnemy != null)
            {
                gameObject.GetComponentInChildren<ParticleSystem>().Play();
                //仅仅在x和z轴上面朝向敌人
                Vector3 targetPos = new Vector3(targetEnemy.transform.position.x, pos.y, targetEnemy.transform.position.z);
                gameObject.GetComponentInChildren<ImpactCannon>().transform.LookAt(targetPos);
                gameObject.transform.LookAt(targetPos);
            }
        }

        //寻找最近的敌人
        private void UpdateEnemyTarget()
        {
            if (gameObjects.Count == 0)
            {
                return;
            }
            float minDistance = float.MaxValue;
            GameObject closestEnemy = null;
            foreach (GameObject enemy in gameObjects)
            {
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
