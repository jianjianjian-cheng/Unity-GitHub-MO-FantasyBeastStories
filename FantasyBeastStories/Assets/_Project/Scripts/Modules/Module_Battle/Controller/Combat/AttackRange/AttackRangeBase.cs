using System.Collections;
using System.Collections.Generic;
using Controllers.Character;
using Core;
using Controllers.Player;
using Controllers.Battle;
using Controllers.Network;
using UnityEngine;
using Core.Audio;
using Core.SharedModel;
using Controllers.Battle;

namespace Controllers.Battle
{
    /// <summary>
    /// 攻击范围基类：负责检测范围内的敌人，攻击逻辑由子类实现或 Lua 回调驱动
    /// </summary>
    public class AttackRangeBase : TriggerBase
    {
        protected INetworkFireballCaster _networkCaster;
        [SerializeField] protected NetworkIdentityBase _network;

        [Header("纯数据")]
        [SerializeField] private AttackRangeData attackRangeData;

        private AttributePlayerBase _attributePlayerBase;
        /// <summary>懒加载获取玩家属性（热更后 attributePlayer 可能延迟创建）</summary>
        protected AttributePlayerBase attributePlayerBase
        {
            get
            {
                if (_attributePlayerBase == null)
                    _attributePlayerBase = GetLocalPlayerAttribute();
                return _attributePlayerBase;
            }
            set => _attributePlayerBase = value;
        }

        // 使用 HashSet 替代 List，提供 O(1) 的增删查操作
        protected readonly HashSet<GameObject> _enemySet = new HashSet<GameObject>();
        // 缓存 EnemyBase 组件，避免重复 GetComponent
        protected readonly Dictionary<GameObject, EnemyBase> _enemyCache = new Dictionary<GameObject, EnemyBase>();
        // 脏标记：仅当列表发生变化时才重新计算目标
        private bool _enemiesDirty;
        protected GameObject targetEnemy;

        /// <summary>是否正在连射中（连射期间不再触发新攻击）</summary>
        private bool _isAttacking;

        // ==================== Phase 3: Lua bridge ====================
        private AttackLuaBridge _attackLuaBridge;

        public override void Start()
        {
            attackRangeData = new AttackRangeData();

            if (_network == null)
                _network = GetComponent<NetworkIdentityBase>();
            if (_network == null)
                Debug.LogError("[AttackRangeBase] NetworkIdentityBase 未赋值，请在预制体 Inspector 中绑定或确保组件存在", this);

            base.Start();
            // attributePlayerBase 改为懒加载，不再在 Start 中赋值（因为 PlayerController.attributePlayer 可能尚未创建）
            _isTest = EventChannelLocator.MainContainer?.gameSettings?.IsTest ?? false;

            // Phase 6: 初始化网络投射物广播器（原由子类 Start 中赋值）
            _networkCaster = ComponentFactory.GetOrCreateNetworkCaster(gameObject);

            // Phase 3: 按角色名加载攻击行为 Lua
            var playerController = GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                string charName = playerController.GetCharacterName();
                if (!string.IsNullOrEmpty(charName))
                    _attackLuaBridge = new AttackLuaBridge(charName);
            }
        }

        private AttributePlayerBase GetLocalPlayerAttribute()
        {
            if (ServiceLocator.Get<PlayerManager>() != null)
                return ServiceLocator.Get<PlayerManager>().GetLocalPlayerAttribute(AttributeKeyConst.Main);
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

            // 延迟加载攻击行为 Lua（OnPhotonInstantiate 可能晚于 Start 执行）
            if (_attackLuaBridge == null)
            {
                var pc = GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    string charName = pc.GetCharacterName();
                    if (!string.IsNullOrEmpty(charName))
                        _attackLuaBridge = new AttackLuaBridge(charName);
                }
            }

            base.Update();

            // 检查当前目标是否已失效（死亡/销毁/回收），如果是则标记需要重新选择目标
            if (targetEnemy != null)
            {
                if (!_enemyCache.TryGetValue(targetEnemy, out var targetBase) || targetBase == null || targetBase.IsDeadOrDying())
                {
                    _enemiesDirty = true;
                }
            }
            else if (_enemySet.Count > 0)
            {
                _enemiesDirty = true;
            }

            // 仅当敌人列表发生变化或目标失效时才重新计算目标
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
            // 如果有 Lua 攻击行为（如 BingNv 的多目标逻辑），即使 targetEnemy 为 null 也允许攻击
            // Lua 的 PerformAttack 会自己通过 GetSortedTargets 获取目标列表
            if (_attackLuaBridge != null && _enemySet.Count > 0)
            {
                // Lua 模式：有敌人在范围内就可以攻击，不依赖 targetEnemy
            }
            else if (targetEnemy == null)
            {
                return;
            }

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
        /// 具体攻击逻辑。先走 Lua 回调，未处理时走 C# 子类 override。
        /// </summary>
        protected virtual void PerformAttack()
        {
            _attackLuaBridge?.TryPerformAttack(this, targetEnemy);
        }

        /// <summary>
        /// 寻找最近的敌人（使用 sqrMagnitude 避免开平方开销）
        /// </summary>
        protected virtual void UpdateEnemyTarget()
        {
            // Phase 3: 先尝试 Lua 多目标逻辑（如 BingNv），未处理时走 C# 默认单目标逻辑
            if (_attackLuaBridge != null && _attackLuaBridge.TryUpdateEnemyTarget(this))
                return;

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

        // ==================== Phase 2: Public API for Lua bridge ====================

        public GameObject CurrentTarget => targetEnemy;
        public AttributePlayerBase AttributeSource => attributePlayerBase;
        public Element CurrentElement => attributePlayerBase?.GetCurrentElement() ?? Element.Common;
        public INetworkFireballCaster GetNetworkCaster() => _networkCaster;
        public Vector3 GetMuzzlePosition() => GetSpawnPosition();
        public Vector3 GetTargetDirectionPublic() => GetTargetDirection();
        public bool IsCharged() => attackRangeData.isCharged;
        public int GetComboCounter() => attackRangeData.comboCounter;
        public AttackRangeData GetAttackRangeData() => attackRangeData;
        public IReadOnlyCollection<GameObject> GetAllTargets() => _enemySet;

        /// <summary>供 Lua 调用：获取 PlayerController 的已解锁元素列表</summary>
        public System.Collections.Generic.List<Element> GetUnlockedElementsForLua()
        {
            var pc = GetComponentInParent<Controllers.Character.PlayerController>();
            if (pc == null) return null;
            var collection = pc.GetUnlockedElements();
            if (collection == null) return null;
            return new System.Collections.Generic.List<Element>(collection);
        }

        // ==================== Phase 4: Fireball spawn helpers for Lua ====================

        private bool _isTest;

        /// <summary>是否测试模式</summary>
        public bool IsTest => _isTest;

        /// <summary>根据元素获取 ImpactCannon 视觉池名</summary>
        public static string GetImpactCannonPoolByElement(Element element)
        {
            switch (element)
            {
                case Element.Lightning: return PoolConst.ImpactCannonLightenPool;
                case Element.Winter: return PoolConst.ImpactCannonWinterPool;
                case Element.Grass: return PoolConst.ImpactCannonGrassPool;
                default: return PoolConst.ImpactCannonCommonPool;
            }
        }

        /// <summary>根据元素获取 ImpactCannon 击中特效池名</summary>
        public static string GetImpactCannonHitPoolByElement(Element element)
        {
            switch (element)
            {
                case Element.Lightning: return PoolConst.ImpactCannonHitLightenPool;
                case Element.Winter: return PoolConst.ImpactCannonHitWinterPool;
                case Element.Grass: return PoolConst.ImpactCannonHitGrassPool;
                default: return PoolConst.ImpactCannonHitCommonPool;
            }
        }

        /// <summary>
        /// 生成 ImpactCannon 火球（本地）— 供 Lua PerformAttack 调用。
        /// 复刻原 AttackRange_WizardBoy.SpawnFireballLocal 的逻辑。
        /// </summary>
        public void SpawnImpactCannon(Vector3 spawnPos, Vector3 direction, bool isMine)
        {
            if (attributePlayerBase == null) return;

            var element = attributePlayerBase.GetCurrentElement();
            string visualPool = GetImpactCannonPoolByElement(element);
            string triggerPool = PoolConst.ImpactCannonTriggerPool;

            AudioManager.Instance?.PlaySFX("sfx_wizard_fire", spawnPos);

            GameObject visualObj = PoolHelper.Get(visualPool, spawnPos);
            GameObject triggerObj = PoolHelper.Get(triggerPool, spawnPos);

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
                var cannon = triggerObj.GetComponent<IImpactCannon>();
                if (cannon == null)
                    cannon = ComponentFactory.GetOrCreateImpactCannon(triggerObj);
                if (cannon != null)
                {
                    cannon.SetToken(token);
                    cannon.SetAttributeFromPlayer(attributePlayerBase);
                    cannon.StartShoot(direction, isMine);
                }
            }
        }

        /// <summary>
        /// 网络广播火球发射 — 供 Lua PerformAttack 调用。
        /// </summary>
        public void BroadcastFireball(Vector3 spawnPos, Vector3 direction, float speed)
        {
            if (_networkCaster == null) return;
            _networkCaster.RequestFireball(spawnPos, direction, speed, attributePlayerBase.GetCurrentElement());
        }

        // ==================== Phase 5: GuiLing spawn helpers for Lua (BingNv) ====================

        /// <summary>根据元素获取 GuiLing 投射物池名</summary>
        public static string GetGuiLingPoolByElement(Element element)
        {
            switch (element)
            {
                case Element.Fire: return PoolConst.GuiLingFirePool;
                case Element.Lightning: return PoolConst.GuiLingLightningPool;
                case Element.Grass: return PoolConst.GuiLingGrassPool;
                case Element.Winter:
                default: return PoolConst.GuiLingWinterPool;
            }
        }

        /// <summary>
        /// 获取按距离排序的敌人列表（供 Lua 多目标逻辑调用）。
        /// </summary>
        public List<GameObject> GetSortedTargets()
        {
            CleanupDeadEnemies();

            if (_enemySet.Count == 0)
                return new List<GameObject>();

            var sorted = new List<GameObject>(_enemySet);
            Vector3 myPos = transform.position;
            sorted.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                float distA = (a.transform.position - myPos).sqrMagnitude;
                float distB = (b.transform.position - myPos).sqrMagnitude;
                return distA.CompareTo(distB);
            });

            return sorted;
        }

        /// <summary>
        /// 生成 GuiLing 追踪弹（本地）— 供 Lua PerformAttack 调用。
        /// 复刻原 AttackRange_BingNv.FireSingleGuiLing 的逻辑。
        /// </summary>
        public void SpawnGuiLing(
            Vector3 spawnPos, GameObject enemy, Element element,
            float horizontalSpread, float verticalSpread)
        {
            if (attributePlayerBase == null || enemy == null) return;

            string poolName = GetGuiLingPoolByElement(element);
            GameObject guiLing = PoolHelper.Get(poolName, spawnPos);

            if (guiLing == null)
            {
                Debug.LogError($"[AttackRangeBase] 从对象池 {poolName} 获取 GuiLing 失败", this);
                return;
            }

            var direction = (enemy.transform.position - spawnPos).normalized;
            float randomH = UnityEngine.Random.Range(-horizontalSpread, horizontalSpread);
            float randomV = UnityEngine.Random.Range(-verticalSpread, verticalSpread);
            direction = Quaternion.Euler(randomV, randomH, 0f) * direction;

            var guiLingBase = guiLing.GetComponent<GuiLingBase>();
            guiLingBase.poolName = poolName;
            guiLingBase.SetTargetAndLaunch(enemy.transform, direction);

            if (_networkCaster is CastNetwork castNetwork)
            {
                guiLingBase.SetCastNetwork(castNetwork);
            }

            bool isMine = _network != null && _network.IsMine;
            guiLingBase.SetDamageData(
                isMine,
                attributePlayerBase.GetAttackPower(),
                attributePlayerBase.GetCriticalChance(),
                attributePlayerBase.GetCriticalMultiplier(),
                element
            );

            guiLingBase.SetSplitData(
                attributePlayerBase.GetSplit(),
                attributePlayerBase.GetSplitCount()
            );

            // 网络同步
            if (!_isTest)
            {
                var targetView = enemy.GetComponent<Photon.Pun.PhotonView>();
                if (targetView != null && _networkCaster != null)
                {
                    _networkCaster.RequestGuiLingCast(
                        spawnPos, direction, targetView.ViewID, (int)element
                    );
                }
            }
        }
    }
}