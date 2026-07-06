using System.Collections;
using System.Collections.Generic;
using Domain.Data;
using Domain.Enemy;
using Domain.Event;
using Domain.Pool;
using Infrastructure.Network;
using UnityEngine;

namespace Domain.Combat.FX
{
    /// <summary>
    /// 鬼灵弹（GuiLing）抛射体系统
    /// 两阶段弹道：展开阶段沿发射方向飞行 → 追踪阶段平滑转向目标
    /// 支持速度曲线、命中/闪光 VFX、拖尾效果及碰撞处理
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GuiLingBase : MonoBehaviour
    {
        [Header("运动参数")]
        public float maxSpeed = 15f;
        public AnimationCurve speedOverTime = AnimationCurve.Constant(0, 0, 1);
        public float animationDuration = 2f;

        [Header("追踪参数")]
        [Tooltip("展开阶段持续时间（秒），期间沿发射方向飞行，产生扇形扩散效果")]
        public float spreadDuration = 0.25f;
        [Tooltip("追踪阶段转向速度（度/秒），越大弧线越急促")]
        public float rotationSpeed = 360f;
        [Tooltip("目标位置的 Y 轴偏移量，用于瞄准敌人身上某个部位而非脚底")]
        public float targetOffsetY = 0.5f;

        [Header("VFX 效果")]
        public GameObject hitVFX;
        public GameObject flashVFX;
        public List<GameObject> trails;

        [Header("生命周期")]
        [Min(1)]
        public float maxDestroyTimeAfterHit = 2f;

        [Header("丢失目标重寻")]
        [SerializeField]
        [Tooltip("目标死亡后，搜索附近敌人的范围")]
        private float searchRadiusOnLost = 4f;

        [SerializeField]
        [Tooltip("搜索附近敌人的间隔（秒）")]
        private float searchInterval = 0.3f;

        [Header("分裂参数")]
        [SerializeField]
        [Tooltip("分裂时搜索附近敌人的范围")]
        private float splitRange = 20f;

        [SerializeField]
        [Tooltip("分裂弹扇形扩散角度（度）")]
        private float splitAngle = 30f;

        [SerializeField]
        [Tooltip("分裂弹伤害倍率")]
        private float splitDamageMultiplier = 0.5f;

        private bool _isSplit;
        private int _splitCount;
        private bool _canSplit = true;

        [System.NonSerialized]
        [Tooltip("发射时由 AttackRange_BingNv 设置，命中时归还到对应对象池")]
        public string poolName;

        private Rigidbody _rb;
        private float _spawnTime;
        private LayerMask _vfxLayer;
        private bool _useVfxLayer;
        private Collider _collider;

        // 追踪相关
        private Transform _target;
        private int _targetInstanceId;
        private Vector3 _launchDirection;
        private bool _hasTarget;

        // ===== 伤害数据（由 AttackRange_BingNv 发射时传入） =====
        private bool _isMine;
        private float _damage;
        private float _criticalChance;
        private float _criticalMultiplier;
        private Element _element;
        private float _damageMultiplier = 1f; // 当前目标死亡后切换目标，伤害衰减 40%

        // ===== 目标重寻 =====
        private float _lastSearchTime;
        private int _enemyLayerMask;

        private CastNetwork _castNetwork;
        private bool _isTest;

        private void Awake()
        {
            _enemyLayerMask = LayerMask.GetMask("Enemy");
        }

        /// <summary>
        /// 由发射器（AttackRange_BingNv）在发射时调用，传入正确的 CastNetwork 实例
        /// 确保每个玩家使用自己的 CastNetwork 发送 RPC，避免因静态引用指向房主实例导致 RPC 被阻止
        /// </summary>
        public void SetCastNetwork(CastNetwork castNetwork)
        {
            _castNetwork = castNetwork;
            _isTest = EventChannelLocator.MainContainer?.gameSettings?.IsTest ?? false;
        }

        /// <summary>
        /// 由发射器调用，设置目标和发射方向
        /// 注意：对象池复用时 Start() 不会再次执行，因此在此重置 _spawnTime
        /// </summary>
        public void SetTargetAndLaunch(Transform target, Vector3 launchDirection)
        {
            _target = target;
            _targetInstanceId = target != null ? target.GetInstanceID() : 0;
            _launchDirection = launchDirection.normalized;
            _hasTarget = target != null;
            transform.forward = _launchDirection;

            // 重置时间戳，确保弹道弧线从零开始
            _spawnTime = UnityEngine.Time.time;

            // 重置伤害衰减倍率（每次发射重新开始）
            _damageMultiplier = 1f;

            // 对象池复用后需要重新获取 Rigidbody 引用
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            // 重新激活拖尾（对象池复用后拖尾处于隐藏状态）
            foreach (var trail in trails)
            {
                if (trail == null) continue;
                trail.SetActive(true);
                var ps = trail.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }
            }
        }

        /// <summary>
        /// 由发射器调用，传入伤害数据，命中时据此判定伤害
        /// </summary>
        public void SetDamageData(bool isMine, float damage, float criticalChance, float criticalMultiplier, Element element)
        {
            _isMine = isMine;
            _damage = damage;
            _criticalChance = criticalChance;
            _criticalMultiplier = criticalMultiplier;
            _element = element;
        }

        /// <summary>
        /// 由发射器调用，传入分裂数据，命中时根据技能决定是否分裂
        /// </summary>
        public void SetSplitData(bool isSplit, int splitCount)
        {
            _isSplit = isSplit;
            _splitCount = splitCount;
            _canSplit = isSplit;
        }

        /// <summary>
        /// 配置 VFX 的自定义层级，并启用 VFX 层级功能
        /// </summary>
        public void SetVfxLayer(LayerMask vfxLayer)
        {
            _vfxLayer = vfxLayer;
            _useVfxLayer = true;
            _collider = GetComponent<Collider>();
        }

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _spawnTime = UnityEngine.Time.time;

            // 碰撞体设为 Trigger，只触发逻辑不产生物理碰撞，可穿墙
            GetComponent<Collider>().isTrigger = true;

            // 如果没有调用 SetTargetAndLaunch，使用当前朝向作为发射方向
            if (_launchDirection == Vector3.zero)
                _launchDirection = transform.forward;

            var vfx = LaunchEffect(flashVFX, transform.position, Quaternion.identity);
            if (vfx != null)
            {
                vfx.transform.forward = transform.forward;
            }
        }

        private void FixedUpdate()
        {
            var elapsed = UnityEngine.Time.time - _spawnTime;
            var time = Mathf.Clamp01(elapsed / animationDuration);
            var curveValue = speedOverTime.Evaluate(time);
            var currentSpeed = curveValue * maxSpeed;

            if (_hasTarget)
            {
                if (elapsed < spreadDuration)
                {
                    // 阶段1：展开阶段，沿发射方向飞行，产生扇形扩散效果
                    _rb.velocity = _launchDirection * currentSpeed;
                }
                else
                {
                    // 阶段2：追踪阶段 — 先检查目标是否仍存活
                    if (_target == null || IsTargetDead())
                    {
                        TryAcquireNewTarget();
                        if (_target == null)
                            return; // 无目标可追，已在 TryAcquireNewTarget 中归还池
                    }

                    var targetPos = _target.position + Vector3.up * targetOffsetY;
                    var targetDir = (targetPos - transform.position).normalized;
                    var distance = Vector3.Distance(transform.position, targetPos);

                    if (distance < 1.5f)
                    {
                        // 近距离：直接对准目标，确保命中不再绕圈
                        transform.forward = targetDir;
                    }
                    else
                    {
                        // 远距离：平滑转向
                        var newDir = Vector3.RotateTowards(
                            transform.forward,
                            targetDir,
                            rotationSpeed * Mathf.Deg2Rad * UnityEngine.Time.fixedDeltaTime,
                            0f
                        );
                        transform.forward = newDir;
                    }

                    _rb.velocity = transform.forward * currentSpeed;
                }
            }
            else
            {
                // 无目标，沿当前朝向直线飞行
                _rb.velocity = transform.forward * currentSpeed;
            }
        }

        private GameObject LaunchEffect(GameObject prefab, Vector3 position, Quaternion rotation, bool setCustomLayer = false)
        {
            if (prefab == null)
                return null;

            var vfx = Instantiate(prefab, position, rotation);
            if (setCustomLayer)
            {
                SetLayerRecursively(vfx, GetLayerFromMask(_vfxLayer));
            }

            var ps = vfx.GetComponentInChildren<ParticleSystem>();
            var waitUntilDestroy = ps != null ? ps.main.duration : maxDestroyTimeAfterHit;
            Destroy(vfx, waitUntilDestroy);
            return vfx;
        }

        /// <summary>
        /// 从对象池播放击中特效
        /// </summary>
        private void PlayHitEffectFromPool(Vector3 position, Quaternion rotation, bool isVisible)
        {
            string hitPoolName = GetHitPoolName(poolName);
            if (string.IsNullOrEmpty(hitPoolName))
                return;

            GameObject hitEffect = null;
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateGet(hitPoolName, position, (o) => hitEffect = o));

            if (hitEffect == null)
                return;

            hitEffect.transform.position = position;
            hitEffect.transform.rotation = rotation;

            if (_useVfxLayer && isVisible)
                SetLayerRecursively(hitEffect, GetLayerFromMask(_vfxLayer));

            // GuiLingHit 脚本会在 OnEnable 中播放粒子并自动归还池
        }

        /// <summary>
        /// 根据 GuiLing 投射物池名称推导对应的击中特效池名称
        /// </summary>
        private static string GetHitPoolName(string projectilePoolName)
        {
            if (string.IsNullOrEmpty(projectilePoolName))
                return null;

            switch (projectilePoolName)
            {
                case PoolConst.GuiLingFirePool: return PoolConst.GuiLingHitFirePool;
                case PoolConst.GuiLingLightningPool: return PoolConst.GuiLingHitLightningPool;
                case PoolConst.GuiLingWinterPool: return PoolConst.GuiLingHitWinterPool;
                case PoolConst.GuiLingGrassPool: return PoolConst.GuiLingHitGrassPool;
                default:
                    Debug.LogWarning($"[GuiLingBase] 未知的投射物池名称: {projectilePoolName}，无法找到对应的击中特效池");
                    return null;
            }
        }

        /// <summary>
        /// 判断当前目标是否已死亡（GameObject 销毁 或 EnemyBase 标记为死亡）
        /// </summary>
        private bool IsTargetDead()
        {
            if (_target == null) return true;
            var enemyBase = _target.GetComponent<EnemyBase>();
            return enemyBase != null && enemyBase.IsDeadOrDying();
        }

        /// <summary>
        /// 尝试搜索附近存活敌人作为新目标
        /// 搜索间隔受 searchInterval 控制，避免每帧 OverlapSphere
        /// 若找到则更新 _target / _targetInstanceId / _hasTarget
        /// 若未找到则直接归还对象池
        /// </summary>
        private void TryAcquireNewTarget()
        {
            // 频率限制
            if (UnityEngine.Time.time - _lastSearchTime < searchInterval)
                return;
            _lastSearchTime = UnityEngine.Time.time;

            // 搜索附近敌人
            Collider[] hits = Physics.OverlapSphere(transform.position, searchRadiusOnLost, _enemyLayerMask);
            float closestDist = float.MaxValue;
            Transform closestEnemy = null;

            foreach (var hit in hits)
            {
                // 使用 GetComponentInParent 兼容子物体 Collider
                var enemyBase = hit.gameObject.GetComponentInParent<EnemyBase>();
                if (enemyBase == null || enemyBase.IsDeadOrDying()) continue;

                Transform rootTransform = enemyBase.transform;
                float dist = Vector3.Distance(transform.position, rootTransform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = rootTransform;
                }
            }

            if (closestEnemy != null)
            {
                // 找到新目标，重新锁定，伤害衰减 40%
                _damageMultiplier *= 0.6f;
                _target = closestEnemy;
                _targetInstanceId = closestEnemy.GetInstanceID();
                _hasTarget = true;
            }
            else
            {
                // 周围无存活敌人，直接归还池
                _hasTarget = false;
                ReturnToPool();
            }
        }

        /// <summary>
        /// 归还当前 GuiLing 到对象池
        /// </summary>
        private void ReturnToPool()
        {
            _rb.velocity = Vector3.zero;
            if (!string.IsNullOrEmpty(poolName))
            {
                EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                    PoolOperationData.CreateReturn(poolName, gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 从 Collider 向上查找根 EnemyBase.transform（兼容子物体 Collider 的情况）
        /// </summary>
        private Transform GetRootEnemyTransform(Collider other)
        {
            var enemyBase = other.gameObject.GetComponentInParent<EnemyBase>();
            return enemyBase != null ? enemyBase.transform : null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Enemy"))
                return;

            // 兼容子物体 Collider：找到根 EnemyBase Transform 再比对 Instance ID
            Transform rootEnemy = GetRootEnemyTransform(other);
            if (rootEnemy == null || rootEnemy.GetInstanceID() != _targetInstanceId)
                return;

            var hitPoint = other.ClosestPoint(transform.position);
            var normal = (transform.position - hitPoint).normalized;
            var spawnPos = hitPoint + normal * 0.15f;

            var isVisible = IsPointVisibleFromCamera(spawnPos);
            PlayHitEffectFromPool(spawnPos, Quaternion.FromToRotation(Vector3.up, normal), isVisible);

            if (trails.Count > 0)
            {
                foreach (var trail in trails)
                {
                    if (trail == null) continue;

                    var ps = trail.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Stop();
                    }

                    // 不销毁、不离散，保留为子物体，后续对象池复用时可重新激活
                    trail.SetActive(false);
                }
            }

            // ===== 伤害判定：只有发射者（_isMine）才处理伤害 =====
            if (_isMine)
            {
                bool isCritical = Random.Range(0f, 1f) <= _criticalChance;

                if (_isTest)
                {
                    // 测试模式：直接通过事件通道触发伤害
                    var args = new DamageEventArgs(_element, gameObject, rootEnemy.gameObject, _damage * _damageMultiplier, isCritical, _criticalMultiplier);
                    EventChannelLocator.MainContainer?.damageEventChannel?.Raise(args);
                }
                else
                {
                    // 联机模式：通过 CastNetwork 广播伤害（RPC 到所有客户端）
                    // 重要：使用 rootEnemy.gameObject 而非 other.gameObject，
                    // 确保 BroadcastDamage 中 enemyObj.GetComponent<PhotonView>() 能正确找到根物体的 PhotonView
                    _castNetwork?.BroadcastDamage(
                        rootEnemy.gameObject,
                        _damage * _damageMultiplier,
                        isCritical,
                        _criticalMultiplier,
                        hitPoint,
                        _element
                    );

                    // 同步击中特效到其他客户端（在相同位置播放）
                    _castNetwork?.BroadcastGuiLingHitVFX(
                        spawnPos,
                        normal,
                        (int)_element
                    );
                }
            }

            // ===== 分裂判定：只有发射者（_isMine）且开启了分裂才执行 =====
            if (_canSplit && _isMine)
            {
                _canSplit = false; // 防止无限分裂
                SplitToNearestEnemies(hitPoint, rootEnemy.gameObject);
            }

            // 延迟 0.15s 归还到对象池，让击中特效播放完毕后再回收
            StartCoroutine(DelayedReturnToPool(0f));
        }

        private IEnumerator DelayedReturnToPool(float delay)
        {
            yield return new WaitForSeconds(delay);
            _rb.velocity = Vector3.zero;
            if (!string.IsNullOrEmpty(poolName))
            {
                EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateReturn(poolName, gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 分裂逻辑：搜索附近其他敌人，生成分裂弹
        /// </summary>
        private void SplitToNearestEnemies(Vector3 hitPoint, GameObject hitEnemy)
        {
            // 1. 找到被命中敌人的根 EnemyBase（兼容子物体 Collider）
            var hitEnemyBase = hitEnemy.GetComponentInParent<EnemyBase>();
            GameObject hitRoot = hitEnemyBase != null ? hitEnemyBase.gameObject : hitEnemy;

            // 2. 搜索范围内的所有敌人
            Collider[] enemiesInRange = Physics.OverlapSphere(
                hitPoint,
                splitRange,
                _enemyLayerMask
            );

            // 3. 按距离排序（排除已命中的敌人）
            //    注意：使用根 EnemyBase GameObject，兼容子物体 Collider
            List<Collider> validTargets = new List<Collider>();
            foreach (var col in enemiesInRange)
            {
                // 跳过已命中的敌人（使用根 GameObject 对比）
                var enemyBase = col.gameObject.GetComponentInParent<EnemyBase>();
                if (enemyBase == null) continue;
                if (enemyBase.gameObject == hitRoot)
                    continue;
                // 跳过已死亡的敌人（非房主端 GetIsDie() 返回 false，需额外检查状态机）
                if (enemyBase.IsDeadOrDying())
                    continue;

                validTargets.Add(col);
            }
            if (validTargets.Count == 0)
                return;

            // 按距离从近到远排序（使用根 Transform 的位置）
            validTargets.Sort(
                (a, b) =>
                {
                    var baseA = a.gameObject.GetComponentInParent<EnemyBase>();
                    var baseB = b.gameObject.GetComponentInParent<EnemyBase>();
                    Vector3 posA = baseA != null ? baseA.transform.position : a.transform.position;
                    Vector3 posB = baseB != null ? baseB.transform.position : b.transform.position;
                    return Vector3
                        .Distance(hitPoint, posA)
                        .CompareTo(Vector3.Distance(hitPoint, posB));
                }
            );

            int actualSplitCount = Mathf.Min(_splitCount, validTargets.Count);
            for (int i = 0; i < actualSplitCount; i++)
            {
                var enemyBase = validTargets[i].gameObject.GetComponentInParent<EnemyBase>();
                if (enemyBase == null) continue;

                GameObject rootTarget = enemyBase.gameObject;
                Vector3 targetPos = rootTarget.transform.position;

                // 计算基础方向，只取 xz 轴方向
                Vector3 xzTargetPos = new Vector3(targetPos.x, hitPoint.y, targetPos.z);
                Vector3 baseDirection = (xzTargetPos - hitPoint).normalized;

                // 添加扇形偏移（让分裂弹看起来更自然）
                Vector3 splitDirection = GetSplitDirection(baseDirection, i, actualSplitCount);

                FireSplitGuiLing(hitPoint, splitDirection, rootTarget);
            }
        }

        /// <summary>
        /// 获取带扇形偏移的方向
        /// </summary>
        private Vector3 GetSplitDirection(Vector3 baseDirection, int index, int total)
        {
            if (total <= 1)
                return baseDirection;

            float halfAngle = splitAngle / 2f;
            float step = total > 1 ? splitAngle / (total - 1) : 0;
            float currentAngle = -halfAngle + step * index;

            return Quaternion.Euler(0, currentAngle, 0) * baseDirection;
        }

        /// <summary>
        /// 发射一颗分裂 GuiLing
        /// </summary>
        private void FireSplitGuiLing(Vector3 spawnPos, Vector3 direction, GameObject targetEnemy)
        {
            // 1. 从对象池获取 GuiLing
            GameObject splitGuiLing = null;
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateGet(poolName, spawnPos, (o) => splitGuiLing = o));

            if (splitGuiLing == null)
            {
                Debug.LogWarning($"[GuiLingBase] 从对象池 {poolName} 获取分裂弹失败");
                return;
            }

            // 2. 设置投射物参数
            var guiLingBase = splitGuiLing.GetComponent<GuiLingBase>();
            guiLingBase.poolName = poolName;
            guiLingBase.SetTargetAndLaunch(targetEnemy.transform, direction);

            // 3. 传入伤害数据（分裂弹有自己的 _damageMultiplier，从 1f 开始独立衰减）
            guiLingBase.SetDamageData(
                _isMine,
                _damage * splitDamageMultiplier,
                _criticalChance,
                _criticalMultiplier,
                _element
            );

            // 4. 分裂弹不再继续分裂，防止无限递归
            guiLingBase.SetSplitData(false, 0);

            // 5. 将当前 CastNetwork 传递给分裂弹（确保使用正确的网络发射器）
            guiLingBase.SetCastNetwork(_castNetwork);

            // ===== 网络同步：向其他客户端广播分裂 GuiLing =====
            if (_isMine && _castNetwork != null && !_isTest)
            {
                _castNetwork.BroadcastSplitGuiLingCast(
                    spawnPos,
                    direction,
                    targetEnemy,
                    (int)_element
                );
            }
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static int GetLayerFromMask(LayerMask mask)
        {
            return (int)Mathf.Log(mask.value, 2);
        }

        private bool IsPointVisibleFromCamera(Vector3 point)
        {
            var cam = Camera.main;
            if (cam == null)
                return false;

            var dir = point - cam.transform.position;
            var distance = dir.magnitude;
            dir.Normalize();

            if (!Physics.Raycast(
                    cam.transform.position,
                    dir,
                    out var hit,
                    distance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                ))
            {
                return false;
            }

            return hit.collider == _collider;
        }
    }
}