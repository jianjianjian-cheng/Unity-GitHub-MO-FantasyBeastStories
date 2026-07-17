using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Character
{
    /// <summary>
    /// 角色信息配置 —— 用于角色选择面板的展示内容。
    /// 在 Project 窗口中右键 → Create → Config/Character Info 创建实例。
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Character Info")]
    public class CharacterInfoSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("角色显示名称")]
        public string characterName;

        [Tooltip("角色图标")]
        public Sprite characterIcon;

        [Header("能力介绍")]
        [Tooltip("角色能力介绍列表，每条为一段描述")]
        public List<string> abilityDescriptions = new List<string>();
    }
}