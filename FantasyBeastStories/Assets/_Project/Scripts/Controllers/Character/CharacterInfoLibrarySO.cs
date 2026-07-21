using System.Collections.Generic;
using UnityEngine;

namespace Controllers.Character
{
    /// <summary>
    /// 角色信息库 —— 保存所有角色的 CharacterInfoSO 引用。
    /// 角色选择面板只需引用这一个资源即可获取全部角色的展示信息。
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Character Info Library")]
    public class CharacterInfoLibrarySO : ScriptableObject
    {
        [Tooltip("所有角色信息")]
        public List<CharacterInfoSO> characterInfos = new List<CharacterInfoSO>();

        /// <summary>
        /// 根据角色索引获取信息（索引越界时返回 null）
        /// </summary>
        public CharacterInfoSO GetInfo(int index)
        {
            if (index < 0 || index >= characterInfos.Count)
                return null;
            return characterInfos[index];
        }

        /// <summary>
        /// 根据角色索引获取 Prefab 根节点名称
        /// </summary>
        public string GetNameByIndex(int index)
        {
            var info = GetInfo(index);
            return info != null ? info.characterPrefabName : string.Empty;
        }

        /// <summary>
        /// 获取角色数量
        /// </summary>
        public int Count => characterInfos.Count;
    }
}