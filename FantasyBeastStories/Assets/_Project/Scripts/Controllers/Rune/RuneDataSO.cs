// Assets/Scripts/Domain/Rune/RuneDataSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rune/Rune Data")]
public class RuneDataSO : ScriptableObject
{
    public int runeId;
    public string runeName;
    public Sprite icon;            // 默认图标（包体内保底）
    [Tooltip("图标文件名，热更新时从 AssetBundle 加载（如 icon_rune_1001）")]
    public string iconName;        // 热更新图标标识
    public Rarity rarity;          // Common / Epic / Legendary / 专属（未来掉落用）
    public List<RunePower> powers; // 属性修正列表
    public string specialPowerName;
    [TextArea] public string specialPowerDescription;
    [Tooltip("专属符文绑定的角色类型（如 WizardBoy / BingNv），配合 ApplySpecialPower 判断")]
    public string exclusiveCharacterType; // 为空表示全局通用
}

[System.Serializable]
public struct RunePower
{
    public int value;      // e.g. 30
    public string label;   // e.g. "%基础伤害"
}

public enum Rarity { Common, Epic, Legendary, Exclusive }