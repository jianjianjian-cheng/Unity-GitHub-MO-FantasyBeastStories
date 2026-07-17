using System.Collections;
using System.Collections.Generic;
using Controllers.Character;
using Core;
using Controllers.Player;
using Controllers.Enemy;
using Controllers.Network;
using UnityEngine;
using Managers;

namespace Controllers.Combat
{
    /// <summary>
    /// 攻击范围基类：负责检测范围内的敌人，攻击逻辑由子类实现
    /// </summary>
    public abstract class AttackRangeBase : TriggerBase
    {
        protected INetworkFireballCaster _networkCaster;
        [SerializeField] protected NetworkIdentityBase _network;

        [Header("纯数据")]
        [SerializeField] private AttackRangeData attackRangeData;

        protected AttributePlayerBase attributePlayerBase;

        // 使用 HashSet 替代 List，提供 O(1) 的增删查操作
        protected readonly HashSet<GameObject> _enemySet = new HashSet<GameObject>();
        // 缓存 EnemyBase 组件，避免重复 GetComponent
        protected readonly Dictionary<GameObject, EnemyBase> _enemyCache = new Dictionary<GameObject, EnemyBase>();
        // 脏标记：仅当列表发生变化时才重新计算目标
        private bool _enemiesDirty;
        protected GameObject targetEnemy;

        /// <summary>是否正在连射中（连射期间不再触发新攻击）</summary>
        private bool _isAttacking;

        public override void Start()
        {
            attackRangeData = new AttackRangeData();

            if (_network == null)
                _network = GetComponent<NetworkIdentityBase>();
            if (_network == null)
                Debug.LogError("[AttackRangeBase] NetworkIdentityBase 未赋值，请在预制体 Inspector 中绑定或确保组件存在", this);

            base.Start();
            attributePlayerBase = GetLocalPlayerAttribute();
        }

        private AttributePlayerBase GetLocalPlayerAttribute()
        {
            if (PlayerManager.instance != null)
                return PlayerManager.instance.GetLocalPlayerAttribute(AttributeKeyConst.Main);
            return null;
        }

        public override void Update()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!_network.IsMine)
            {
                return;
            }
            if (attributePlayerBase == null)
                attributePlayerBase = GetLocalPlayerAttribute();
            if (attributePlayerBase == null)
                return;

            base.Update();

            // 仅当敌人列表发生变化时才重新计算目标，避免每帧遍历
            if (_enemiesDirty)
            {
                UpdateEnemyTarget();
                _enemiesDirty = false;
            }

            Attack();
        }

        /// <summary>
        /// 攻击逻辑：基类负责控制攻击间隔，具体攻击行为由子类实现
        /// </summary>
        private void Attack()
        {
            if (targetEnemy == null)
                return;

            attackRangeData.attackInterval = attributePlayerBase.GetAttackInterval();

            // 攻击间隔中 → 等待计时器归零
            if (attackRangeData.attackTimer > 0)
            {
                attackRangeData.attackTimer -= UnityEngine.Time.deltaTime;
                return;
            }

            // 计时器归零 + 不在连射中 → 启动新一轮连射
            if (!_isAttacking)
            {
                StartCoroutine(AttackSequenceCoroutine());
            }
        }

        /// <summary>
        /// 攻击序列协程：先完成连射，结束后才设置攻击间隔计时器
        /// </summary>
        private IEnumerator AttackSequenceCoroutine()
        {
            _isAttacking = true;

            // ── 阶段一：连射 ──
            while (attackRangeData.comboCounter <= attributePlayerBase.GetComboCount())
            {
                attackRangeData.isCharged = (attackRangeData.empowerChargeCounter >= attributePlayerBase.GetEmpowerCharge());

                PerformAttack();

                attackRangeData.comboCounter++;
                attackRangeData.empowerChargeCounter = attackRangeData.isCharged ? 1 : attackRangeData.empowerChargeCounter + 1;

                yield return new WaitForSeconds(0.3f);
            }
            attackRangeData.isCharged = false;
            attackRangeData.comboCounter = 1;

            // ── 阶段二：连射全部完成 → 开始计算攻击间隔 ──
            attackRangeData.attackTimer = attackRangeData.attackInterval;
            _isAttacking = false;
        }

        /// <summary>
        /// 由子类实现的具体攻击逻辑
        /// </summary>
        protected abstract void PerformAttack();

        /// <summary>
        /// 寻找最近的敌人（使用 sqrMagnitude 避免开平方开销）
        /// </summary>
        protected virtual void UpdateEnemyTarget()
        {
            // 先清理已死亡的敌人
            CleanupDeadEnemies();

            if (_enemySet.Count == 0)
            {
                targetEnemy = null;
                return;
            }

            float minSqrDistance = float.MaxValue;
            GameObject closestEnemy = null;
            Vector3 myPos = transform.position;

            foreach (GameObject enemy in _enemySet)
            {
                if (enemy == null)
                    continue;

                // 使用 sqrMagnitude 替代 Vector3.Distance，避免 sqrt 计算
                float sqrDist = (enemy.transform.position - myPos).sqrMagnitude;
                if (sqrDist < minSqrDistance)
                {
                    minSqrDistance = sqrDist;
                    closestEnemy = enemy;
                }
            }

            targetEnemy = closestEnemy;
        }

        /// <summary>
        /// 清理已死亡的敌人，仅当 _enemiesDirty 时调用
        /// </summary>
        private void CleanupDeadEnemies()
        {
            if (_enemySet.Count == 0)
                return;

            List<GameObject> deadList = null;
            foreach (var enemyGo in _enemySet)
            {
                if (enemyGo == null)
                {
                    deadList ??= new List<GameObject>();
                    deadList.Add(enemyGo);
                    continue;
                }

                // 从缓存中获取 EnemyBase，避免重复 GetComponent
                if (!_enemyCache.TryGetValue(enemyGo, out var enemyBase) || enemyBase == null || enemyBase.IsDeadOrDying())
                {
                    deadList ??= new List<GameObject>();
                    deadList.Add(enemyGo);
                }
            }

            if (deadList != null)
            {
                foreach (var go in deadList)
                {
                    _enemySet.Remove(go);
                    _enemyCache.Remove(go);
                }
            }
        }

        /// <summary>
        /// 获取目标位置（带Y轴偏移）
        /// </summary>
        protected Vector3 GetSpawnPosition()
        {
            return new Vector3(
                transform.position.x,
                transform.position.y + attackRangeData.offsetY,
                transform.position.z
            );
        }

        /// <summary>
        /// 获取目标方向（忽略Y轴）
        /// </summary>
        protected Vector3 GetTargetDirection()
        {
            if (targetEnemy == null)
                return transform.forward;

            Vector3 pos = GetSpawnPosition();
            Vector3 direction = (targetEnemy.transform.position - pos).normalized;
            direction.y = 0;
            return direction;
        }

        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            AddEnemy(other);
        }

        /// <summary>
        /// 不再使用 OnTriggerStay —— 该函数每帧为每个碰撞体触发，开销极大。
        /// 敌人进出范围由 OnTriggerEnter/OnTriggerExit 管理，死亡清理由每帧标记驱动。
        /// </summary>
        public override void OnTriggerStay(Collider other)
        {
            // 留空：所有逻辑由 Enter/Exit 和 Update 中的脏标记处理
        }

        public override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
            RemoveEnemy(other);
        }

        /// <summary>
        /// 添加敌人到 HashSet（O(1)），并缓存 EnemyBase 组件
        /// </summary>
        private void AddEnemy(Collider other)
        {
            var enemyBase = other.gameObject.GetComponentInParent<EnemyBase>();
            if (enemyBase == null || enemyBase.IsDeadOrDying())
                return;

            GameObject rootGo = enemyBase.gameObject;
            if (_enemySet.Add(rootGo)) // HashSet.Add 返回 false 表示已存在
            {
                _enemyCache[rootGo] = enemyBase;
                _enemiesDirty = true;
            }
        }

        /// <summary>
        /// 从 HashSet 移除敌人（O(1)），同时清理缓存
        /// </summary>
        private void RemoveEnemy(Collider other)
        {
            var enemyBase = other.gameObject.GetComponentInParent<EnemyBase>();
            GameObject rootGo = enemyBase != null ? enemyBase.gameObject : other.gameObject;

            if (_enemySet.Remove(rootGo))
            {
                _enemyCache.Remove(rootGo);
                _enemiesDirty = true;
            }
        }

        /// <summary>
        /// 在编辑器中可视化攻击范围
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            if (attackRangeData.searchRadius > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, attackRangeData.searchRadius);

                if (targetEnemy != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(GetSpawnPosition(), targetEnemy.transform.position);
                }
            }
        }
    }
}