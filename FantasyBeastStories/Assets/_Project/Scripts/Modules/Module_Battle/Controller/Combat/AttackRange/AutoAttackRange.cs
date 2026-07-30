using System.Collections;
using System.Collections.Generic;
using Controllers.Battle;
using Core;
using UnityEngine;

namespace Controllers.Battle
{
    public class AutoAttackRange : MonoBehaviour
    {
        [SerializeField] private AttackableEnemy enemyBase;


        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                // enemyBase.OnHandleTriggerEnter(other.gameObject);
                Debug.LogWarning("进入攻击范围" + other.gameObject.name);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                // enemyBase.OnHandleTriggerExit(other.gameObject);
            }
        }
    }
}