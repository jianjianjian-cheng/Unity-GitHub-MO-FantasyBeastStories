// Assets/Scripts/Domain/Rune/RuneDataSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rune/Rune Data")]
public class RuneDataSO : ScriptableObject
{
    public int runeId;
    public string runeName;
    public Sprite icon;
    public Rarity rarity;          // Common / Epic / Legendary / 专属（未来掉落用）
    public List<RunePower> powers; // 属性修正列表
    public string specialPowerName;
    [TextArea] public string specialPowerDescription;
}

[System.Serializable]
public struct RunePower
{
    public int value;      // e.g. 30
    public string label;   // e.g. "%基础伤害"
}

public enum Rarity { Common, Epic, Legendary, Exclusive }