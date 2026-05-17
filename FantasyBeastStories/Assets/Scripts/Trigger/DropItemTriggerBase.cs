using System.Collections;
using UnityEngine;
using Items;

public class DropItemTriggerBase : MonoBehaviour
{
    [SerializeField] private DropItemBase itemParent;
    private bool hasTriggered = false;  // 防止重复触发

    void OnTriggerEnter(Collider other)
    {
        // 已经触发过，或者正在飞向玩家，就不再触发
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            itemParent.HandlePickupEnter(other.gameObject);
        }
    }

    // 如果用对象池，重置触发状态
    void OnDisable()
    {
        hasTriggered = false;
        // itemParent.ResetItem();
    }
}