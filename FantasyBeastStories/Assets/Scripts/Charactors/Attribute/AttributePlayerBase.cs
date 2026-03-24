using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atttibute
{
    public class AttributePlayerBase
    {
        private float attackPower;
        private float defensePower;
        private float criticalMultiplier = 1;//暴击倍率,默认1倍
        private float criticalChance = 0.2f;//暴击概率,默认20%
        private float maxHealth = 100f;//最大生命值,默认100
        private float currentHealth;//当前生命值
        private float moveSpeed = 2f;//移动速度,默认2f

        public AttributePlayerBase(
            float attackPower,
            float defensePower,
            float maxHealth,
            float moveSpeed,
            float criticalMultiplier,
            float criticalChance)
        {
            //初始化最大生命值为默认值
            this.maxHealth = maxHealth;
            //初始化当前生命值为最大生命值
            this.currentHealth = maxHealth;
            //初始化移动速度为默认值
            this.moveSpeed = moveSpeed;
            //初始化攻击伤害为默认值
            this.attackPower = attackPower;
            //初始化防御伤害为默认值
            this.defensePower = defensePower;
            //初始化暴击倍率为默认值
            this.criticalMultiplier = criticalMultiplier;
            //初始化暴击概率为默认值
            this.criticalChance = criticalChance;
        }
        public void SetMaxHealth(float maxHealth)
        {
            this.maxHealth = maxHealth;
            //进行换算,根据当前生命值与最大生命值比例,更新当前生命值
            float currentHealthRatio = currentHealth / maxHealth;
            currentHealth = maxHealth * currentHealthRatio;
            //换算为整数
            currentHealth = Mathf.RoundToInt(currentHealth);
        }

        public void AddCurrentHealth(float currentHealth)
        {
            this.currentHealth += currentHealth;
        }

        public void SetMoveSpeed(float moveSpeed)
        {
            this.moveSpeed = moveSpeed;
        }

        public void SetCriticalMultiplier(float criticalMultiplier)
        {
            this.criticalMultiplier = criticalMultiplier;
        }

        public void SetCriticalChance(float criticalChance)
        {
            this.criticalChance = criticalChance;
        }

        public void SetAttackPower(float attackPower)
        {
            this.attackPower = attackPower;
        }

        public void SetDefensePower(float defensePower)
        {
            this.defensePower = defensePower;
        }

        public float GetCurrentHealth()
        {
            return currentHealth;
        }

        public float GetMaxHealth()
        {
            return maxHealth;
        }

        public float GetMoveSpeed()
        {
            return moveSpeed;
        }

        public float GetAttackPower()
        {
            return attackPower;
        }

        public float GetDefensePower()
        {
            return defensePower;
        }

        public float GetCriticalMultiplier()
        {
            return criticalMultiplier;
        }

        public float GetCriticalChance()
        {
            return criticalChance;
        }
    }
}
