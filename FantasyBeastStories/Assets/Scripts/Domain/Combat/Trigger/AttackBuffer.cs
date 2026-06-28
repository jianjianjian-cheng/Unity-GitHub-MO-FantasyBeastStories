using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Domain.Event;

namespace Domain.Combat.Trigger
{
    public class AttackBuffer : MonoBehaviour
    {
        private string WeaponType;
        private float weaponPassiveVal1;
        private float weaponPassiveVal2;
        private ColliderTriggerManager CTM;

        void Start()
        {
            CTM = ColliderTriggerManager.Instance;
            WeaponType = CTM.WeaponType;
            weaponPassiveVal1 = CTM.weaponPassiveVal1;
            weaponPassiveVal2 = CTM.weaponPassiveVal2;
        }
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                if (WeaponType != null && WeaponType == "ImpactCannon")
                {
                    Vector3 forceDirection = (other.transform.position - transform.position).normalized;
                    forceDirection.y = 0; // 保持水平方向
                    other.attachedRigidbody?.AddForce(forceDirection * weaponPassiveVal1 + Vector3.up * 2f, ForceMode.Impulse);
                }
            }
        }
    }
}