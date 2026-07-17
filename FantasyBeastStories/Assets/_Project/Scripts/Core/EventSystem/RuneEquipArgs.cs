using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class RuneEquipArgs : EventArgsBase
    {
        public int slotId; // 符文插槽ID
        public string runeName; // 符文名称
        public Dictionary<int, string> runePowers; // 符文属性字典
        public string specialPowerName; // 特殊能力名称
        public string specialPowerDescription; // 特殊能力描述

        public RuneEquipArgs(int slotId, string runeName, Dictionary<int, string> runePowers, string specialPowerName, string specialPowerDescription)
        : base()
        {
            this.slotId = slotId;
            this.runeName = runeName;
            this.runePowers = runePowers;
            this.specialPowerName = specialPowerName;
            this.specialPowerDescription = specialPowerDescription;
        }
    }
}
