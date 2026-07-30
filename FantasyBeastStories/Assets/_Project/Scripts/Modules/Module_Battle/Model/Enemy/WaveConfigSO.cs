using UnityEngine;

namespace Controllers.Battle
{
    /// <summary>
    /// 敌人生成配置（ScriptableObject）
    /// 将 EnemiesGenerator 的预制体、池配置、生成间隔等参数从 MonoBehaviour 迁移到 SO，
    /// 实现数据驱动 — 策划可在不改动代码的情况下调整波次参数。
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Wave Config")]
    public class WaveConfigSO : ScriptableObject
    {
        [Header("基础敌人")]
        [Tooltip("基础敌人预制体")]
        public GameObject enemyPrefab;

        [Tooltip("专属对象池名称（留空则使用生成器 GameObject 名称 + \"_Pool\"）")]
        public string poolName = "";

        [Tooltip("对象池预创建数量（骷髅敌人数量多建议 20-30）")]
        public int poolPreloadCount = 25;

        [Header("精英敌人（Dragon）")]
        [Tooltip("Dragon 预制体（留空则不生成 Dragon）")]
        public GameObject dragonPrefab;

        [Tooltip("Dragon 对象池预创建数量")]
        public int dragonPoolPreloadCount = 10;

        [Header("生成间隔")]
        [Tooltip("初始生成间隔（秒）")]
        public float baseSpawnInterval = 10f;

        [Tooltip("最小生成间隔（秒），数量峰值时的最快频率")]
        public float minSpawnInterval = 1.5f;
    }
}
