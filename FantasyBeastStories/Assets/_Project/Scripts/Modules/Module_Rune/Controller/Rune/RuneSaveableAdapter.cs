using Core;
using UnityEngine;
using Core.Save;

namespace Controllers.Rune
{
    /// <summary>
    /// 符文系统存档适配器。
    /// RuneInventory 和 RuneEquipmentSnapshot 是静态类，无法实现 ISaveable 接口，
    /// 因此用此 MonoBehaviour 适配器代为注册到 SaveManager。
    /// </summary>
    public class RuneSaveableAdapter : MonoBehaviour, ISaveable
    {
        public string SaveId => "RuneSystem";

        public void OnSave(SaveData data)
        {
            data.equippedRuneId1 = RuneEquipmentSnapshot.EquippedRuneId1;
            data.equippedRuneId2 = RuneEquipmentSnapshot.EquippedRuneId2;
            data.ownedRuneIds = RuneInventory.GetAllRuneIds();
        }

        public void OnLoad(SaveData data)
        {
            RuneEquipmentSnapshot.SetBoth(data.equippedRuneId1, data.equippedRuneId2);
            RuneInventory.RestoreFromSave(data.ownedRuneIds);
        }

        void Start()
        {
            ServiceLocator.Get<SaveManager>()?.RegisterSaveable(this);
        }

        void OnDestroy()
        {
            ServiceLocator.Get<SaveManager>()?.UnregisterSaveable(this);
        }
    }
}
