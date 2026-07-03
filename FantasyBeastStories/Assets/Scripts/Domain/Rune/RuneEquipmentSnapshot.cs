using System.Collections.Generic;
using UnityEngine;

namespace Domain.Rune
{
    /// <summary>
    /// 跨场景缓存装备的符文 ID
    /// 大厅出发前由 RunePanel 捕获，游戏场景中由 RuneEffectApplier 读取。
    /// GameManager 为 DontDestroyOnLoad，因此静态数据在场景切换后仍存在。
    /// </summary>
    public static class RuneEquipmentSnapshot
    {
        public static int EquippedRuneId1 { get; private set; } = -1;
        public static int EquippedRuneId2 { get; private set; } = -1;

        /// <summary>从 RunePanel 捕获当前装备的符文 ID</summary>
        public static void CaptureFrom(RunePanel panel)
        {
            EquippedRuneId1 = panel.GetEquip1()?.EquippedRuneId ?? -1;
            EquippedRuneId2 = panel.GetEquip2()?.EquippedRuneId ?? -1;
            Debug.Log($"[RuneEquipmentSnapshot] 捕获装备数据: Slot1={EquippedRuneId1}, Slot2={EquippedRuneId2}");
        }

        /// <summary>获取所有已装备的符文 ID（过滤掉未装备的 -1）</summary>
        public static int[] GetAllEquippedIds()
        {
            var ids = new List<int>(2);
            if (EquippedRuneId1 != -1) ids.Add(EquippedRuneId1);
            if (EquippedRuneId2 != -1) ids.Add(EquippedRuneId2);
            return ids.ToArray();
        }

        public static void SetBoth(int runeId1, int runeId2)
        {
            EquippedRuneId1 = runeId1;
            EquippedRuneId2 = runeId2;
        }
    }
}