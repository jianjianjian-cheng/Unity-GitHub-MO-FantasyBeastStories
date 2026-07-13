using System.Collections;
using Domain.Item;
using UnityEngine;

namespace Domain.Combat.Trigger
{
    public class DropItemTriggerBase : MonoBehaviour
    {
        [SerializeField]
        private DropItemBase itemParent;
        private bool hasTriggered = false; // 防止重复触发

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[DropItemTrigger] OnTriggerEnter被触发！进入的对象: {other.gameObject.name}, Tag: {other.tag}");

            // 已经触发过，或者正在飞向玩家，就不再触发
            if (hasTriggered)
            {
                Debug.Log($"[DropItemTrigger] 已触发过，跳过");
                return;
            }

            if (other.CompareTag("Player"))
            {
                Debug.Log($"[DropItemTrigger] ✅ 检测到玩家！准备调用HandlePickupEnter");
                hasTriggered = true;
                itemParent.HandlePickupEnter(other.gameObject);
            }
            else
            {
                Debug.Log($"[DropItemTrigger] ❌ 不是玩家，Tag是: {other.tag}");
            }
        }

        // 如果用对象池，重置触发状态
        void OnDisable()
        {
            hasTriggered = false;
            // itemParent.ResetItem();
        }
    }
}