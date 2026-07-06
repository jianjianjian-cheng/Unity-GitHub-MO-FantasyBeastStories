using System.Collections.Generic;
using UnityEngine;

namespace Domain.Character
{
    /// <summary>
    /// 角色信息库 —— 保存所有角色的 CharacterInfoSO 引用。
    /// 角色选择面板只需引用这一个资源即可获取全部角色的展示信息。
    ///
    /// 列表顺序与 CharactorIndex 保持一致：
    ///   [0] = WiZardBoy
    ///   [1] = BingNv
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Character Info Library")]
    public class CharacterInfoLibrarySO : ScriptableObject
    {
        [Tooltip("所有角色信息，顺序与 CharactorIndex 一致：[0]=WiZardBoy, [1]=BingNv")]
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
        /// 获取角色数量
        /// </summary>
        public int Count => characterInfos.Count;
    }
}