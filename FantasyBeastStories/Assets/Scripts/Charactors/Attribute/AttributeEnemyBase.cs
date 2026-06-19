using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Charactors.Attribute
{
    public class AttributeEnemyBase
    {
        //基本属性
        public float maxHealth;
        public float currentHealth;
        public float attackPower;
        public float moveSpeed;
        public bool isDead = false;

        public AttributeEnemyBase(
            float maxHealth,
            float currentHealth,
            float attackPower,
            float moveSpeed
        )
        {
            this.maxHealth = maxHealth;
            this.currentHealth = currentHealth;
            this.attackPower = attackPower;
            this.moveSpeed = moveSpeed;
            isDead = false;
        }

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
            Debug.Log("当前生命值: " + currentHealth + " / " + maxHealth);
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
            currentHealth = maxHealth;
        }

        //受到特殊伤害的方法
        public virtual void TakeDamageSpecial(Element element)
        {
            //待写
            switch (element)
            {
                case Element.Grass:

                    break;
                case Element.Winter:

                    break;
                case Element.Lightning:

                    break;
                default:
                    break;
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
            SetIsDie(true);
        }

        public virtual void Heal(float health)
        {
            currentHealth += health;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
        }

        public virtual void ResetHealth()
        {
            currentHealth = maxHealth;
        }

        public float GetAttackPower()
        {
            return attackPower;
        }

        public void ResetAttribute()
        {
            currentHealth = maxHealth;
            isDead = false;
        }
    }
}
