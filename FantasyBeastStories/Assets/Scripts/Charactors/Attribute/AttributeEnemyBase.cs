using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Charactors.Attribute
{
    public class AttributeEnemyBase : MonoBehaviour
    {
        //基本属性
        [SerializeField] public float maxHealth = 100f;
        [SerializeField] public float currentHealth;
        [SerializeField] public float attackPower = 10f;
        [SerializeField] public float moveSpeed = 2f;
        public bool isDead = false;
        public virtual bool GetIsDie()
        {
            return isDead;
        }
        public virtual void SetIsDie(bool value)
        {
            isDead = value;
        }

        //减少生命的方法
        public virtual void TakeDamage(float damage)
        {
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        //减少最大生命值的方法
        public virtual void TakeDamageMaxHealth(float damage)
        {
            maxHealth -= damage;
            if (maxHealth <= 0)
            {
                maxHealth = 0;
            }
        }

        public virtual void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = newMaxHealth;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
        }

        public virtual void SetAttackPower(float newAttackPower)
        {
            attackPower = newAttackPower;
        }

        public virtual void SetMoveSpeed(float newMoveSpeed)
        {
            moveSpeed = newMoveSpeed;
        }
        public virtual void Die()
        {

        }
    }
}
