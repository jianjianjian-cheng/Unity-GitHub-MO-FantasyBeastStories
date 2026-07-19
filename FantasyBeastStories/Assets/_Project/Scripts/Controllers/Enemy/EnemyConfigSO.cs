using UnityEngine;

namespace Controllers.Enemy
{
    /// <summary>
    /// 普通怪物配置数据（ScriptableObject）
    /// 将怪物属性从 MonoBehaviour 硬编码中分离，实现数据驱动
    /// 参照 PlayerAttributeConfigSO / BossConfigSO 的模式
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Enemy Config")]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("基础属性")]
        public float maxHealth = 500f;
        public float attackPower = 50f;
        public float moveSpeed = 2f;

        [Header("攻击参数")]
        [Tooltip("攻击间隔（秒），两次攻击之间的冷却时间")]
        public float attackInterval = 0.7f;
        [Tooltip("攻击距离（怪物与玩家的距离小于此值时造成伤害）")]
        public float attackRange = 2f;

        [Header("经验掉落")]
        [Tooltip("经验球最小经验值")]
        public int expMin = 50;
        [Tooltip("经验球最大经验值")]
        public int expMax = 70;

        [Header("道具掉落")]
        [Range(0f, 1f)]
        [Tooltip("死亡时掉落道具的概率")]
        public float powerUpDropChance = 0.1f;
    }
}
