using Controllers.Player;
using UnityEngine;
using Controllers.Battle;

#if UNITY_EDITOR
using UnityEditor;
using Core;
#endif

namespace Utilities
{
    /// <summary>
    /// LOD 范围可视化调试器。
    /// 在场景中任意 GameObject 上挂载此组件，选中时即可在 Scene 视图中
    /// 以每个玩家为中心绘制 LOD 等级范围圈。
    ///
    /// 用途：场景中只需挂载一个此组件（或每个玩家挂一个），
    /// 避免每个敌人都重复绘制同一套圆圈。
    /// </summary>
    public class LODRangeDebugger : MonoBehaviour
    {
        [Header("LOD 参数（与 AttackableEnemy 联动）")]
        [SerializeField, Tooltip("从场景中引用一个 AttackableEnemy 实例以读取 LOD 参数")]
        private AttackableEnemy enemyReference;

        [Header("可视化开关")]
        [SerializeField, Tooltip("在 Scene 视图中绘制 LOD 范围圈")]
        private bool drawGizmos = true;

        [Header("范围圈颜色")]
        [SerializeField]
        private Color attackRangeColor = new Color(1f, 0f, 0f, 0.5f);

        [SerializeField]
        private Color lod0Color = new Color(0f, 1f, 0f, 0.3f);

        [SerializeField]
        private Color lod1Color = new Color(1f, 0.8f, 0f, 0.2f);

        [SerializeField]
        private Color lod2Color = new Color(1f, 0.3f, 0f, 0.1f);

        // 缓存的 LOD 参数
        private float cachedAttackRange = 2f;
        private float cachedLod0Distance = 10f;
        private float cachedLod1Distance = 30f;

        private void OnValidate()
        {
            RefreshLODParameters();
        }

        private void Awake()
        {
            RefreshLODParameters();
        }

        /// <summary>
        /// 从引用的 AttackableEnemy 同步 LOD 参数
        /// </summary>
        private void RefreshLODParameters()
        {
            if (enemyReference != null)
            {
                // 通过反射读取私有字段
                var attackRangeField = typeof(AttackableEnemy).GetField("attackRange",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (attackRangeField != null)
                    cachedAttackRange = (float)attackRangeField.GetValue(enemyReference);

                var lod0Field = typeof(AttackableEnemy).GetField("lod0Distance",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (lod0Field != null)
                    cachedLod0Distance = (float)lod0Field.GetValue(enemyReference);

                var lod1Field = typeof(AttackableEnemy).GetField("lod1Distance",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (lod1Field != null)
                    cachedLod1Distance = (float)lod1Field.GetValue(enemyReference);
            }
        }

        /// <summary>
        /// 从场景中自动查找第一个 AttackableEnemy 以获取参数
        /// </summary>
        private void AutoFindEnemyReference()
        {
            if (enemyReference != null)
                return;

            var enemy = FindObjectOfType<AttackableEnemy>();
            if (enemy != null)
            {
                enemyReference = enemy;
                RefreshLODParameters();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            // 如果未引用敌人实例，尝试自动查找
            if (enemyReference == null)
            {
                AutoFindEnemyReference();
                if (enemyReference == null)
                {
                    // 仍然没找到，用默认值继续绘制
                }
            }

            // 获取所有玩家位置
            var players = ServiceLocator.Get<PlayerManager>() != null
                ? ServiceLocator.Get<PlayerManager>().ActivePlayerObjects
                : null;

            if (players == null || players.Count == 0)
                return;

            for (int p = 0; p < players.Count; p++)
            {
                var player = players[p];
                if (player == null) continue;

                Vector3 pos = player.transform.position;

                // 1. 攻击范围（红色实线圆）
                Gizmos.color = attackRangeColor;
                Gizmos.DrawWireSphere(pos, cachedAttackRange);

                // 2. LOD 0 - 完整行为区（绿色实线圆）
                Gizmos.color = lod0Color;
                Gizmos.DrawWireSphere(pos, cachedLod0Distance);

                // 3. LOD 1 - 降级行为区（黄色实线圆）
                Gizmos.color = lod1Color;
                Gizmos.DrawWireSphere(pos, cachedLod1Distance);

                // 4. LOD 2 - 最小行为区（橙色虚线圆）
                Gizmos.color = lod2Color;
                DrawDottedCircle(pos, cachedLod1Distance * 1.5f, 48);

                // 仅第一个玩家显示文字标签
                if (p == 0)
                {
                    DrawLabel(pos + Vector3.forward * cachedAttackRange,
                        $"攻击范围 {cachedAttackRange:F1}m", attackRangeColor);
                    DrawLabel(pos + Vector3.forward * cachedLod0Distance,
                        $"LOD 0 < {cachedLod0Distance:F1}m", lod0Color);
                    DrawLabel(pos + Vector3.right * cachedLod1Distance,
                        $"LOD 1 < {cachedLod1Distance:F1}m", lod1Color);
                    DrawLabel(pos + Vector3.right * cachedLod1Distance * 1.5f,
                        $"LOD 2 ≥ {cachedLod1Distance:F1}m", lod2Color);
                }
            }
        }

        private static void DrawDottedCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i += 2)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 p1 = center + new Vector3(Mathf.Sin(angle1), 0, Mathf.Cos(angle1)) * radius;
                Vector3 p2 = center + new Vector3(Mathf.Sin(angle2), 0, Mathf.Cos(angle2)) * radius;

                Gizmos.DrawLine(p1, p2);
            }
        }

        private static void DrawLabel(Vector3 position, string text, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(position, 0.15f);

            Handles.color = color;
            Handles.Label(position, text);
        }
#endif
    }
}