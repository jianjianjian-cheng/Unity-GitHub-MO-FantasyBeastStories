using Core.SharedModel;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class RuneEquipArgs : EventArgsBase
    {
        public int slotId;
        public string runeName;
        public List<RunePower> runePowers;
        public string specialPowerName;
        public string specialPowerDescription;

        public RuneEquipArgs(int slotId, string runeName, List<RunePower> runePowers, string specialPowerName, string specialPowerDescription)
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
