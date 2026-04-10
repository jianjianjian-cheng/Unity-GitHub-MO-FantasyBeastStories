using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneSlot : MonoBehaviour
{
    public Dictionary<int, string> runePowers = new Dictionary<int, string>();
    [SerializeField] public int slotId; // 符文插槽ID
    public string RuneName;
    public string specialPowerName;
    public string specialPowerDescription;

    void Awake()
    {
        Intilize();
    }
    private void Intilize()
    {
        if (slotId == 0)
        {
            runePowers.Add(30, "%基础伤害");
            runePowers.Add(20, "%暴击率");
            RuneName = "太炸裂啦！";
            specialPowerName = "小法师专属：";
            specialPowerDescription = "初始发射数量+1";
        }
        else if (slotId == 1)
        {
            runePowers.Add(20, "%防御力");
            runePowers.Add(-20, "%攻击间隔");
            RuneName = "三维增幅";
            specialPowerName = " ";
            specialPowerDescription = " ";
        }
    }
}
