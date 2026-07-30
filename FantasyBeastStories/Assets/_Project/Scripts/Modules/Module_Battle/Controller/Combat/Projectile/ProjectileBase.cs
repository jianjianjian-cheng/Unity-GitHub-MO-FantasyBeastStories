using System;
using System.Collections.Generic;
using Controllers.Battle;
using Controllers.Network;
using Core;
using UnityEngine;
using Core.SharedModel;

namespace Controllers.Battle
{
    /// <summary>
    /// 统一投射物基类：提供公共字段、伤害判定框架、分裂逻辑和 GC 优化。
    /// 子类（ImpactCannon/GuiLingBase/新角色投射物）继承此类，按需 override。
    /// </summary>
    public class ProjectileBase : TriggerBase
    {
        protected bool _isMine;
        protected float _damage;
        protected float _critChance;
        protected float _critMultiplier;
        protected Element _element;
        protected bool _canSplit;
        protected int _splitCount;
        protected float _splitDamageMultiplier;
        protected int _targetViewId = -1;
        protected CastNetwork _castNetwork;

        protected System.Action<GameObject, float, bool, float, Vector3, Element> _damageCallback;

        // GC 优化：缓存 List 和 Sort 委托
        protected List<Collider> _validTargetsCache = new List<Collider>(16);
        protected Vector3 _sortOrigin;
        protected Comparison<Collider> _sortByDistance;

        protected virtual void Awake()
        {
            _sortByDistance = (a, b) =>
                Vector3.Distance(_sortOrigin, a.transform.position)
                    .CompareTo(Vector3.Distance(_sortOrigin, b.transform.position));
        }

        /// <summary>统一初始化接口</summary>
        public virtual void Initialize(
            Vector3 spawnPos, Vector3 direction, int targetViewId,
            float damage, float critChance, float critMultiplier,
            Element element, bool canSplit, int splitCount,
            float splitDamageMultiplier, bool isMine)
        {
            _isMine = isMine;
            _damage = damage;
            _critChance = critChance;
            _critMultiplier = critMultiplier;
            _element = element;
            _canSplit = canSplit;
            _splitCount = splitCount;
            _splitDamageMultiplier = splitDamageMultiplier;
            _targetViewId = targetViewId;

            transform.position = spawnPos;
            transform.forward = direction;

            if (_castNetwork == null)
                _castNetwork = GetComponentInParent<CastNetwork>();
        }

        public void SetDamageCallback(System.Action<GameObject, float, bool, float, Vector3, Element> callback)
        {
            _damageCallback = callback;
        }

        public void SetCastNetwork(CastNetwork castNetwork)
        {
            _castNetwork = castNetwork;
        }

        /// <summary>子类实现飞行行为（直线/追踪/抛物线等），默认空</summary>
        protected virtual void UpdateMovement() { }

        /// <summary>统一伤害判定</summary>
        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            if (!other.CompareTag("Enemy"))
                return;

            // 追踪投射物：验证目标 ID 匹配
            if (_targetViewId >= 0)
            {
                var rootEnemy = other.gameObject.GetComponentInParent<EnemyBase>();
                if (rootEnemy == null || rootEnemy.transform.GetInstanceID() != _targetViewId)
                    return;
            }

            var hitPoint = other.ClosestPoint(transform.position);

            if (_isMine)
            {
                bool isCritical = UnityEngine.Random.Range(0f, 1f) <= _critChance;
                float finalDamage = isCritical ? _damage * _critMultiplier : _damage;

                _damageCallback?.Invoke(
                    other.gameObject,
                    finalDamage,
                    isCritical,
                    _critMultiplier,
                    hitPoint,
                    _element
                );

                if (_canSplit)
                {
                    SplitToNearestEnemies(hitPoint, other.gameObject);
                    _canSplit = false;
                }
            }

            PlayHitEffect(hitPoint);
            RecycleToPool();
        }

        /// <summary>分裂逻辑</summary>
        protected virtual void SplitToNearestEnemies(Vector3 hitPoint, GameObject hitEnemy)
        {
            var enemiesInRange = Physics.OverlapSphere(
                hitPoint, 20f, LayerMask.GetMask("Enemy"));

            _validTargetsCache.Clear();
            foreach (var col in enemiesInRange)
            {
                if (col.gameObject == hitEnemy) continue;
                var enemyBase = col.gameObject.GetComponentInParent<EnemyBase>();
                if (enemyBase == null || enemyBase.IsDeadOrDying()) continue;
                _validTargetsCache.Add(col);
            }

            if (_validTargetsCache.Count == 0) return;

            _sortOrigin = hitPoint;
            _validTargetsCache.Sort(_sortByDistance);

            int actualCount = Mathf.Min(_splitCount, _validTargetsCache.Count);
            for (int i = 0; i < actualCount; i++)
            {
                Vector3 targetPos = _validTargetsCache[i].transform.position;
                Vector3 xzTarget = new Vector3(targetPos.x, hitPoint.y, targetPos.z);
                Vector3 baseDir = (xzTarget - hitPoint).normalized;

                float halfAngle = 30f / 2f;
                float step = actualCount > 1 ? 30f / (actualCount - 1) : 0;
                float angle = -halfAngle + step * i;
                Vector3 splitDir = Quaternion.Euler(0, angle, 0) * baseDir;

                CreateSplitProjectile(hitPoint, splitDir, _validTargetsCache[i].gameObject);
            }
        }

        /// <summary>创建分裂投射物（子类可 override）</summary>
        protected virtual void CreateSplitProjectile(Vector3 spawnPos, Vector3 direction, GameObject target)
        {
            // 子类实现：从对象池获取并初始化
        }

        /// <summary>播放命中特效</summary>
        protected virtual void PlayHitEffect(Vector3 hitPoint) { }

        /// <summary>归还到对象池，子类 override 实现具体回收逻辑</summary>
        protected virtual void RecycleToPool() { }
    }
}