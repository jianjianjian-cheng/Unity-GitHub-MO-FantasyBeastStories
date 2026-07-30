using UnityEngine;

namespace Core.SharedModel
{
    /// <summary>
    /// 道具数据配置 - ScriptableObject数据驱动
    /// 优势：可视化配置、复用、版本控制友好
    /// </summary>
    [CreateAssetMenu(menuName = "Power Up/Create Power Up Data")]
    public class PowerUpDataSO : ScriptableObject
    {
        [Header("基础信息")]
        public string itemId;
        public string itemName;
        [TextArea(2, 4)]
        public string itemDescription;

        [Header("图标")]
        public Sprite icon;

        [Header("效果引用")]
        public IPowerUpEffect effectPrefab; // 拖拽对应的效果Prefab

        [Header("掉落参数")]
        public float dropWeight = 1f; // 掉落权重（用于随机池）
        public bool isStackable = false; // 是否可叠加
        public int maxStackCount = 1; // 最大叠加数

        [Header("显示")]
        public Color glowColor = Color.cyan;
        public float rotateSpeed = 90f;
    }
}