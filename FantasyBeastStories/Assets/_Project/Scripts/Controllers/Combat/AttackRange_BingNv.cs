using System.Collections.Generic;
using System.Linq;
using Controllers.Character;
using Controllers.Combat;
using Core;
using Controllers.Network;
using Core;
using Controllers.Network;
using Photon.Pun;
using UnityEngine;

namespace Controllers.Combat
{
    /// <summary>
    /// 冰女攻击范围 — GuiLing（鬼灵弹）发射器
    /// 负责索敌、分配目标并发射多发追踪弹
    /// </summary>
    public class AttackRange_BingNv : AttackRangeBase
    {
        [Header("GuiLing 发射参数")]
        [SerializeField]
        [Tooltip("水平扇形扩散角度（度），每枚导弹在该范围内随机偏移")]
        private float horizontalSpreadAngle = 45f;

        [SerializeField]
        [Tooltip("垂直扇形扩散角度（度），每枚导弹随机上下俯仰，增加弹道立体感")]
        private float verticalSpreadAngle = 20f;

        [SerializeField]
        [Tooltip("生成位置的 Y 轴偏移量")]
        private float launchOffsetY = 0.5f;

        [Header("Player")]
        [SerializeField]
        private BingNv bingNv;

        /// <summary>
        /// 已解锁元素列表缓存（每次攻击前刷新），避免频繁 LINQ 操作
        /// </summary>
        private Element[] _cachedUnlockedElements;

        private bool _isTest;

        public override void Start()
        {
            base.Start();

            // 初始化网络投射物广播器（与 WizardBoy 一致）
            _networkCaster = ComponentFactory.GetOrCreateNetworkCaster(gameObject);
            _isTest = EventChannelLocator.MainContainer != null
                      && EventChannelLocator.MainContainer.gameSettings != null
                      && EventChannelLocator.MainContainer.gameSettings.IsTest;

            if (_networkCaster == null && !_isTest)
            {
                Debug.LogError("[AttackRange_BingNv] 无法获取或创建 INetworkFireballCaster，请检查 ComponentFactory 是否已注册");
            }

            // Inspector 未拖拽绑定时，自动从父级查找
            if (bingNv == null)
                bingNv = GetComponentInParent<BingNv>();

            if (bingNv == null)
                Debug.LogError("[AttackRange_BingNv] 未找到 BingNv 组件，请在 Inspector 中绑定或确保挂载在 BingNv 层级下", this);
        }

        /// <summary>
        /// 当前最大可锁定目标数（从玩家属性读取，由卡牌效果升级）
        /// </summary>
        public int MaxTargetCount => attributePlayerBase?.GetMultiTargetCount() ?? 3;

        /// <summary>
        /// 本次攻击锁定的多个目标（按距离排序，最近优先）
        /// </summary>
        private readonly List<GameObject> _targets = new List<GameObject>();

        /// <summary>
        /// 获取本次锁定的多个目标（外部只读访问）
        /// </summary>
        public IReadOnlyList<GameObject> Targets => _targets;

        /// <summary>
        /// 缓存已解锁元素数组并返回数量
        /// </summary>
        private int RefreshUnlockedElements()
        {
            if (bingNv == null) return 0;
            _cachedUnlockedElements = bingNv.UnlockedElements.ToArray();
            return _cachedUnlockedElements.Length;
        }

        /// <summary>
        /// 从已解锁元素中随机选择一个
        /// </summary>
        private Element GetRandomElement()
        {
            if (_cachedUnlockedElements == null || _cachedUnlockedElements.Length == 0)
                return Element.Winter; // 保底：默认 Winter
            return _cachedUnlockedElements[Random.Range(0, _cachedUnlockedElements.Length)];
        }

        /// <summary>
        /// 根据元素获取对应的 GuiLing 对象池名称
        /// </summary>
        private static string GetGuiLingPoolByElement(Element element)
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
        /// 重写目标更新：先让基类清理无效敌人并设置最近目标（targetEnemy），
        /// 再从 _enemySet 中选取最多 MaxTargetCount 个目标存入 _targets
        /// </summary>
        protected override void UpdateEnemyTarget()
        {
            // 基类负责清理已死亡/销毁的敌人，并设置 targetEnemy 为最近敌人
            base.UpdateEnemyTarget();

            // 清空并重新填充多目标列表
            _targets.Clear();
            if (_enemySet.Count == 0)
                return;

            // 对范围内的敌人按距离排序，取前 MaxTargetCount 个
            // 使用 sqrMagnitude 避免 sqrt 计算
            Vector3 myPos = transform.position;
            var sorted = new List<GameObject>(_enemySet);
            sorted.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                float sqrDistA = (a.transform.position - myPos).sqrMagnitude;
                float sqrDistB = (b.transform.position - myPos).sqrMagnitude;
                return sqrDistA.CompareTo(sqrDistB);
            });

            int count = Mathf.Min(MaxTargetCount, sorted.Count);
            for (int i = 0; i < count; i++)
            {
                _targets.Add(sorted[i]);
            }
        }

        protected override void PerformAttack()
        {
            var spawnPos = new Vector3(
                transform.position.x,
                transform.position.y + launchOffsetY,
                transform.position.z
            );

            if (_targets.Count == 0)
                return;

            // 每次攻击前刷新已解锁元素缓存
            int unlockedCount = RefreshUnlockedElements();
            if (unlockedCount == 0)
            {
                // 没有任何已解锁元素时，用默认 Winter 保底发射一次
                Debug.LogWarning("[AttackRange_BingNv] 没有已解锁元素，使用默认 Winter 发射", this);
                FireSingleGuiLing(spawnPos, _targets[0], Element.Winter);
                return;
            }

            // 分配方案：先保证每个敌人至少 1 颗，剩余导弹按距离由近到远依次多分配 1 颗
            int maxTargetCount = MaxTargetCount;
            int[] allocation = new int[_targets.Count];
            int remaining = maxTargetCount;

            // 第 1 轮：每个敌人至少 1 颗
            for (int i = 0; i < _targets.Count && remaining > 0; i++)
            {
                allocation[i] = 1;
                remaining--;
            }

            // 第 2 轮：剩余的导弹从最近的敌人开始逐个追加
            for (int i = 0; i < _targets.Count && remaining > 0; i++)
            {
                allocation[i]++;
                remaining--;
            }

            // 按分配方案发射 GuiLing
            for (int i = 0; i < _targets.Count; i++)
            {
                var enemy = _targets[i];
                if (enemy == null) continue;

                for (int j = 0; j < allocation[i]; j++)
                {
                    // 每颗 GuiLing 从已解锁元素中随机抽取一种
                    Element randomElement = GetRandomElement();
                    FireSingleGuiLing(spawnPos, enemy, randomElement);
                }
            }
        }

        /// <summary>
        /// 发射一颗指定元素的 GuiLing（从对象池获取）
        /// </summary>
        private void FireSingleGuiLing(Vector3 spawnPos, GameObject enemy, Element element)
        {
            string poolName = GetGuiLingPoolByElement(element);

            GameObject guiLing = PoolHelper.Get(poolName, spawnPos);

            if (guiLing == null)
            {
                Debug.LogError($"[AttackRange_BingNv] 从对象池 {poolName} 获取 GuiLing 失败", this);
                return;
            }

            var direction = (enemy.transform.position - spawnPos).normalized;
            // 随机水平偏移 + 随机垂直俯仰，让每枚导弹的弓形弧线方向各不相同
            float randomHorizontal = Random.Range(-horizontalSpreadAngle, horizontalSpreadAngle);
            float randomVertical = Random.Range(-verticalSpreadAngle, verticalSpreadAngle);
            direction = Quaternion.Euler(randomVertical, randomHorizontal, 0f) * direction;

            var guiLingBase = guiLing.GetComponent<GuiLingBase>();
            // 设置池名称，命中时归还到对应对象池
            guiLingBase.poolName = poolName;
            guiLingBase.SetTargetAndLaunch(enemy.transform, direction);

            // 传入 CastNetwork（使用自己的网络发射器，而非静态引用）
            // 避免非房主的分裂弹因静态 _castNetwork 指向房主实例导致 RPC 被阻止
            if (_networkCaster is CastNetwork castNetwork)
            {
                guiLingBase.SetCastNetwork(castNetwork);
            }

            // 传入伤害数据，由 GuiLingBase 命中时自行判定
            bool isMine = _network != null && _network.IsMine;
            guiLingBase.SetDamageData(
                isMine,
                attributePlayerBase.GetAttackPower(),
                attributePlayerBase.GetCriticalChance(),
                attributePlayerBase.GetCriticalMultiplier(),
                element
            );

            // 传入分裂数据（根据玩家技能决定是否分裂）
            guiLingBase.SetSplitData(
                attributePlayerBase.GetSplit(),
                attributePlayerBase.GetSplitCount()
            );

            // ===== 网络同步：向其他客户端广播 GuiLing 发射 =====
            if (!_isTest)
            {
                PhotonView targetView = enemy.GetComponent<PhotonView>();
                if (targetView != null && _networkCaster != null)
                {
                    _networkCaster.RequestGuiLingCast(
                        spawnPos,
                        direction,
                        targetView.ViewID,
                        (int)element
                    );
                }
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            // 可以添加 BingNv 特有的可视化
            if (targetEnemy != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(targetEnemy.transform.position, 0.5f);
            }
        }
    }
}