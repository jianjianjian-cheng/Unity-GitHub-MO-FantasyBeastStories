using System.Collections.Generic;
using UnityEngine;
using Core.SharedModel;

namespace Core.SharedModel
{
    /// <summary>
    /// 统一卡牌配置（ScriptableObject）
    /// 每张卡是独立 .asset 文件，可单独热更新
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Card")]
    public class CardConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("唯一标识（自动用文件名填充）")]
        public string cardId;

        [Tooltip("卡牌显示名")]
        public string cardName;

        [TextArea(2, 4)]
        [Tooltip("卡牌描述")]
        public string description;

        [Tooltip("显示数值")]
        public int value;

        [Header("分类")]
        [Tooltip("品质：普通 / 史诗 / 传说")]
        public CardQuality quality;

        [Tooltip("范围：公用卡 / 专属卡")]
        public CardScope scope;

        [Tooltip("专属卡绑定的角色类型（公用卡留空）")]
        public string characterType;

        [Tooltip("是否可重复选取")]
        public bool stackable;

        [Header("效果")]
        [SerializeReference]
        public List<ICardEffect> Effects;
    }
}
