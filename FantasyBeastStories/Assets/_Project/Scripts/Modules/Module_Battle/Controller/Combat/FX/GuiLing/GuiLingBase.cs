using System.Collections;
using System.Collections.Generic;
using Core;
using Controllers.Battle;
using Controllers.Network;
using UnityEngine;
using System;
using Core.Audio;
using Core.SharedModel;

namespace Controllers.Battle
{
    /// <summary>
    /// 鬼灵弹（GuiLing）抛射体系统
    /// 两阶段弹道：展开阶段沿发射方向飞行 → 追踪阶段平滑转向目标
    /// Phase 6: 改为继承 ProjectileBase，复用公共字段和 GC 优化。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GuiLingBase : ProjectileBase
    {
        [Header("运动参数")]
        public float maxSpeed = 15f;
        public AnimationCurve speedOverTime = AnimationCurve.Constant(0, 0, 1);
        public float animationDuration = 2f;

        [Header("追踪参数")]
        public float spreadDuration = 0.25f;
        public float rotationSpeed = 360f;
        public float targetOffsetY = 0.5f;

        [Header("VFX 效果")]
        public GameObject hitVFX;
        public List<GameObject> trails;

        [Header("生命周期")]
        [Min(1)]
        public float maxDestroyTimeAfterHit = 2f;
        public float maxLifetime = 5f;

        [Header("丢失目标重寻")]
        [SerializeField] private float searchRadiusOnLost = 4f;
        [SerializeField] private float searchInterval = 0.3f;

        [Header("分裂参数")]
        [SerializeField] private float splitRange = 20f;
        [SerializeField] private float splitAngle = 30f;

        [System.NonSerialized]
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

        // GuiLing 专属：目标死亡后切换目标，伤害衰减
        private float _damageMultiplier = 1f;
        private bool _isReturning;

        private float _lastSearchTime;
        private int _enemyLayerMask;
        private bool _isTest;

        protected override void Awake()
        {
            base.Awake();
            _enemyLayerMask = LayerMask.GetMask("Enemy");
        }

        /// <summary>由发射器调用，传入正确的 CastNetwork 实例</summary>
        public void SetCastNetwork(CastNetwork castNetwork)
        {
            _castNetwork = castNetwork;
            _isTest = EventChannelLocator.MainContainer?.gameSettings?.IsTest ?? false;
        }

        /// <summary>由发射器调用，设置目标和发射方向</summary>
        public void SetTargetAndLaunch(Transform target, Vector3 launchDirection)
        {
            _target = target;
            _targetInstanceId = target != null ? target.GetInstanceID() : 0;
            _launchDirection = launchDirection.normalized;
            _hasTarget = target != null;
            transform.forward = _launchDirection;
            _isReturning = false;
            _spawnTime = UnityEngine.Time.time;
            _damageMultiplier = 1f;

            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            foreach (var trail in trails)
            {
                if (trail == null) continue;
                trail.SetActive(true);
                var ps = trail.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();
            }
        }

        /// <summary>由发射器调用，传入伤害数据</summary>
        public void SetDamageData(bool isMine, float damage, float criticalChance, float criticalMultiplier, Element element)
        {
            _isMine = isMine;
            _damage = damage;
            _critChance = criticalChance;
            _critMultiplier = criticalMultiplier;
            _element = element;
        }

        /// <summary>由发射器调用，传入分裂数据</summary>
        public void SetSplitData(bool isSplit, int splitCount)
        {
            _canSplit = isSplit;
            _splitCount = splitCount;
            _splitDamageMultiplier = 0.3f;  // 分裂弹伤害衰减为 60%
        }

        public void SetVfxLayer(LayerMask vfxLayer)
        {
            _vfxLayer = vfxLayer;
            _useVfxLayer = true;
            _collider = GetComponent<Collider>();
        }

        public override void Start()
        {
            base.Start();
            _rb = GetComponent<Rigidbody>();
            _spawnTime = UnityEngine.Time.time;
            GetComponent<Collider>().isTrigger = true;
            if (_launchDirection == Vector3.zero)
                _launchDirection = transform.forward;
        }

        protected override void UpdateMovement() { }

        private void FixedUpdate()
        {
            if (_isReturning) return;

            if (UnityEngine.Time.time - _spawnTime > maxLifetime)
            {
                _isReturning = true;
                ReturnToPool();
                return;
            }

            var elapsed = UnityEngine.Time.time - _spawnTime;
            var time = Mathf.Clamp01(elapsed / animationDuration);
            var curveValue = speedOverTime.Evaluate(time);
            var currentSpeed = curveValue * maxSpeed;

            if (_hasTarget)
            {
                if (elapsed < spreadDuration)
                {
                    _rb.velocity = _launchDirection * currentSpeed;
                }
                else
                {
                    if (_target == null || IsTargetDead())
                    {
                        TryAcquireNewTarget();
                        if (_target == null) return;
                    }

                    var targetPos = _target.position + Vector3.up * targetOffsetY;
                    var targetDir = (targetPos - transform.position).normalized;
                    var distance = Vector3.Distance(transform.position, targetPos);

                    if (distance < 1.5f)
                        transform.forward = targetDir;
                    else
                        transform.forward = Vector3.RotateTowards(
                            transform.forward, targetDir,
                            rotationSpeed * Mathf.Deg2Rad * UnityEngine.Time.fixedDeltaTime, 0f);

                    _rb.velocity = transform.forward * currentSpeed;
                }
            }
            else
            {
                _rb.velocity = transform.forward * currentSpeed;
            }
        }

        public override void OnTriggerEnter(Collider other)
        {
            // 不调用 base.OnTriggerEnter — GuiLingBase 有自己的命中/分裂/回收逻辑
            // ProjectileBase.OnTriggerEnter 会提前执行分裂和回收，导致子类逻辑无法执行

            if (!other.CompareTag("Enemy")) return;
            if (_isReturning) return;

            Transform rootEnemy = GetRootEnemyTransform(other);
            if (rootEnemy == null || rootEnemy.GetInstanceID() != _targetInstanceId) return;

            var hitPoint = other.ClosestPoint(transform.position);
            var normal = (transform.position - hitPoint).normalized;
            var spawnPos = hitPoint + normal * 0.15f;

            var isVisible = IsPointVisibleFromCamera(spawnPos);
            PlayHitEffectFromPool(spawnPos, Quaternion.FromToRotation(Vector3.up, normal), isVisible);
            AudioManager.Instance?.PlaySFX("sfx_GuiLingHit", spawnPos);

            if (trails.Count > 0)
            {
                foreach (var trail in trails)
                {
                    if (trail == null) continue;
                    var ps = trail.GetComponent<ParticleSystem>();
                    if (ps != null)
                        ps.Stop();
                    trail.SetActive(false);
                }
            }

            if (_isMine)
            {
                bool isCritical = UnityEngine.Random.Range(0f, 1f) <= _critChance;

                if (_isTest)
                {
                    var args = DamageEventArgs.GetShared(_element, gameObject, rootEnemy.gameObject,
                        _damage * _damageMultiplier, isCritical, _critMultiplier);
                    EventChannelLocator.MainContainer?.damageEventChannel?.Raise(args);
                }
                else
                {
                    _castNetwork?.BroadcastDamage(
                        rootEnemy.gameObject, _damage * _damageMultiplier,
                        isCritical, _critMultiplier, hitPoint, _element);
                    _castNetwork?.BroadcastGuiLingHitVFX(spawnPos, normal, (int)_element);
                }
            }

            if (_canSplit && _isMine)
            {
                _canSplit = false;
                SplitToNearestEnemies(hitPoint, rootEnemy.gameObject);
            }

            _isReturning = true;
            if (gameObject.activeInHierarchy)
                StartCoroutine(DelayedReturnToPool(0f));
            else
                ReturnToPool();
        }

        protected override void SplitToNearestEnemies(Vector3 hitPoint, GameObject hitEnemy)
        {
            var hitEnemyBase = hitEnemy.GetComponentInParent<EnemyBase>();
            GameObject hitRoot = hitEnemyBase != null ? hitEnemyBase.gameObject : hitEnemy;

            Collider[] enemiesInRange = Physics.OverlapSphere(hitPoint, splitRange, _enemyLayerMask);

            _validTargetsCache.Clear();
            foreach (var col in enemiesInRange)
            {
                var enemyBase = col.gameObject.GetComponentInParent<EnemyBase>();
                if (enemyBase == null) continue;
                if (enemyBase.gameObject == hitRoot) continue;
                if (enemyBase.IsDeadOrDying()) continue;
                _validTargetsCache.Add(col);
            }
            if (_validTargetsCache.Count == 0) return;

            _sortOrigin = hitPoint;
            _validTargetsCache.Sort(_sortByDistance);

            int actualSplitCount = Mathf.Min(_splitCount, _validTargetsCache.Count);
            for (int i = 0; i < actualSplitCount; i++)
            {
                var enemyBase = _validTargetsCache[i].gameObject.GetComponentInParent<EnemyBase>();
                if (enemyBase == null) continue;

                GameObject rootTarget = enemyBase.gameObject;
                Vector3 targetPos = rootTarget.transform.position;
                Vector3 xzTargetPos = new Vector3(targetPos.x, hitPoint.y, targetPos.z);
                Vector3 baseDirection = (xzTargetPos - hitPoint).normalized;
                Vector3 splitDirection = GetSplitDirection(baseDirection, i, actualSplitCount);
                FireSplitGuiLing(hitPoint, splitDirection, rootTarget);
            }
        }

        private Vector3 GetSplitDirection(Vector3 baseDirection, int index, int total)
        {
            if (total <= 1) return baseDirection;
            float halfAngle = splitAngle / 2f;
            float step = total > 1 ? splitAngle / (total - 1) : 0;
            float currentAngle = -halfAngle + step * index;
            return Quaternion.Euler(0, currentAngle, 0) * baseDirection;
        }

        private void FireSplitGuiLing(Vector3 spawnPos, Vector3 direction, GameObject targetEnemy)
        {
            GameObject splitGuiLing = PoolHelper.Get(poolName, spawnPos);
            if (splitGuiLing == null)
            {
                Debug.LogWarning($"[GuiLingBase] 从对象池 {poolName} 获取分裂弹失败");
                return;
            }

            var guiLingBase = splitGuiLing.GetComponent<GuiLingBase>();
            guiLingBase.poolName = poolName;
            guiLingBase.SetTargetAndLaunch(targetEnemy.transform, direction);
            guiLingBase.SetDamageData(_isMine, _damage * _splitDamageMultiplier, _critChance, _critMultiplier, _element);
            guiLingBase.SetSplitData(false, 0);
            guiLingBase.SetCastNetwork(_castNetwork);

            if (_isMine && _castNetwork != null && !_isTest)
            {
                _castNetwork.BroadcastSplitGuiLingCast(spawnPos, direction, targetEnemy, (int)_element);
            }
        }

        private void PlayHitEffectFromPool(Vector3 position, Quaternion rotation, bool isVisible)
        {
            string hitPoolName = GetHitPoolName(poolName);
            if (string.IsNullOrEmpty(hitPoolName)) return;

            GameObject hitEffect = PoolHelper.Get(hitPoolName, position);
            if (hitEffect == null) return;

            hitEffect.transform.position = position;
            hitEffect.transform.rotation = rotation;

            if (_useVfxLayer && isVisible)
                SetLayerRecursively(hitEffect, GetLayerFromMask(_vfxLayer));
        }

        private static string GetHitPoolName(string projectilePoolName)
        {
            if (string.IsNullOrEmpty(projectilePoolName)) return null;
            switch (projectilePoolName)
            {
                case PoolConst.GuiLingFirePool: return PoolConst.GuiLingHitFirePool;
                case PoolConst.GuiLingLightningPool: return PoolConst.GuiLingHitLightningPool;
                case PoolConst.GuiLingWinterPool: return PoolConst.GuiLingHitWinterPool;
                case PoolConst.GuiLingGrassPool: return PoolConst.GuiLingHitGrassPool;
                default: return null;
            }
        }

        private bool IsTargetDead()
        {
            if (_target == null) return true;
            var enemyBase = _target.GetComponentInParent<EnemyBase>();
            return enemyBase != null && enemyBase.IsDeadOrDying();
        }

        private void TryAcquireNewTarget()
        {
            if (UnityEngine.Time.time - _lastSearchTime < searchInterval) return;
            _lastSearchTime = UnityEngine.Time.time;

            Collider[] hits = Physics.OverlapSphere(transform.position, searchRadiusOnLost, _enemyLayerMask);
            float closestDist = float.MaxValue;
            Transform closestEnemy = null;

            foreach (var hit in hits)
            {
                var enemyBase = hit.gameObject.GetComponentInParent<EnemyBase>();
                if (enemyBase == null || enemyBase.IsDeadOrDying()) continue;
                float dist = Vector3.Distance(transform.position, enemyBase.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = enemyBase.transform;
                }
            }

            if (closestEnemy != null)
            {
                _damageMultiplier *= 0.6f;
                _target = closestEnemy;
                _targetInstanceId = closestEnemy.GetInstanceID();
                _hasTarget = true;
            }
            else
            {
                _hasTarget = false;
                _isReturning = true;
                ReturnToPool();
            }
        }

        protected override void RecycleToPool() => ReturnToPool();

        private void ReturnToPool()
        {
            _rb.velocity = Vector3.zero;
            if (!string.IsNullOrEmpty(poolName))
                PoolHelper.Return(poolName, gameObject);
            else
                Destroy(gameObject);
        }

        private IEnumerator DelayedReturnToPool(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool();
        }

        private Transform GetRootEnemyTransform(Collider other)
        {
            var enemyBase = other.gameObject.GetComponentInParent<EnemyBase>();
            return enemyBase != null ? enemyBase.transform : null;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static int GetLayerFromMask(LayerMask mask) => (int)Mathf.Log(mask.value, 2);

        private bool IsPointVisibleFromCamera(Vector3 point)
        {
            var cam = Camera.main;
            if (cam == null) return false;
            var dir = point - cam.transform.position;
            var distance = dir.magnitude;
            dir.Normalize();
            if (!Physics.Raycast(cam.transform.position, dir, out var hit, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;
            return hit.collider == _collider;
        }
    }
}
