using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.SharedModel;

namespace Core.SharedModel
{
    public class AttributePlayerBase
    {
        protected PlayerAttributeConfigSO config;

        protected bool isDead = false;
        protected float attackPower;
        protected float defensePower;
        protected float criticalMultiplier;
        protected float criticalChance;
        protected float maxHealth;
        protected float currentHealth;
        protected float moveSpeed;
        protected float attackInterval;
        protected float healthRecover;
        protected float attackspeed;
        protected int maxAttackCount;
        protected int comboCount;
        protected int empowerCharge;
        protected int multiTargetCount;
        protected Element currentElement = Element.Common;

        protected bool isSplit = false;
        protected int splitCount = 0;

        public AttributePlayerBase(PlayerAttributeConfigSO config)
        {
            this.config = config;

            maxHealth = config.baseMaxHealth;
            currentHealth = config.baseMaxHealth;
            moveSpeed = config.baseMoveSpeed;
            attackPower = config.baseAttackPower;
            defensePower = config.baseDefensePower;
            criticalMultiplier = config.baseCriticalMultiplier;
            criticalChance = config.baseCriticalChance;
            attackInterval = config.maxAttackInterval;
            healthRecover = config.baseHealthRecover;
            attackspeed = config.baseAttackSpeed;
            maxAttackCount = config.baseMaxAttackCount;
            comboCount = config.baseComboCount;
            empowerCharge = config.baseEmpowerCharge;
            multiTargetCount = config.baseMultiTargetCount;
            currentElement = Element.Common;
            isSplit = false;
            splitCount = 0;
        }

        public void SetAttackInterval(int attackInterval)
        {
            this.attackInterval = attackInterval;
        }

        //减少攻击间隔
        public void ReduceAttackInterval(int ratio)
        {
            //计算新的攻击速度
            attackspeed += ratio;
            float newRatio = ratio / 100f;
            //按百分比减少攻击间隔
            attackInterval -= (attackInterval * newRatio);
            //向下取整,保留2位小数
            attackInterval = Mathf.Round(attackInterval * 100) / 100;
            //如果攻击间隔小于最小值,则设置为最小值
            if (attackInterval < config.minAttackInterval)
            {
                attackInterval = config.minAttackInterval;
            }
        }

        //获取攻击间隔
        public float GetAttackInterval()
        {
            return attackInterval;
        }

        public float GetAttackSpeed()
        {
            return attackspeed;
        }

        public float GetHealthRecover()
        {
            return healthRecover;
        }

        public void SetIsDead(bool isDead)
        {
            this.isDead = isDead;
        }

        public bool GetIsDead()
        {
            return isDead;
        }

        public void SetMaxHealth(float maxHealth)
        {
            this.maxHealth = maxHealth;
        }

        public void AddCurrentHealth(float currentHealth)
        {
            this.currentHealth += currentHealth;
            //如果当前生命值大于最大生命值,则设置为最大生命值
            if (this.currentHealth > maxHealth)
            {
                this.currentHealth = maxHealth;
            }
        }

        public void SetMoveSpeed(float moveSpeed)
        {
            this.moveSpeed = moveSpeed;
        }

        public void AddMoveSpeed(float moveSpeed)
        {
            this.moveSpeed += moveSpeed;
        }

        public void SetCriticalMultiplier(float criticalMultiplier)
        {
            this.criticalMultiplier = criticalMultiplier;
        }

        public void AddCriticalMultiplier(float ratio)
        {
            float newRatio = ratio / 100f;
            criticalMultiplier += newRatio;
            //向下取整,保留2位小数
            criticalMultiplier = Mathf.Round(criticalMultiplier * 100) / 100;
        }

        public void SetCriticalChance(float criticalChance)
        {
            this.criticalChance = criticalChance;
        }

        public void AddCriticalChance(float ratio)
        {
            float newRatio = ratio / 100f;
            //按百分比增加暴击概率
            criticalChance += newRatio;
            //向下取整,保留2位小数
            criticalChance = Mathf.Round(criticalChance * 100) / 100;
            //如果暴击概率大于上限,则设置为上限
            if (criticalChance > config.maxCriticalChance)
            {
                criticalChance = config.maxCriticalChance;
            }
        }

        public void SetAttackPower(float attackPower)
        {
            this.attackPower = attackPower;
        }

        public void AddAttackPower(float ratio)
        {
            float newRatio = ratio / 100f;
            //按百分比增加攻击伤害
            attackPower += (attackPower * newRatio);
            //向下取整,没有小数位
            attackPower = Mathf.Round(attackPower * 1) / 1;
        }

        public void SetDefensePower(float defensePower)
        {
            this.defensePower = defensePower;
        }

        public void AddDefensePower(float ratio)
        {
            float newRatio = ratio / 100f;
            //按百分比增加防御伤害
            defensePower += (defensePower * newRatio);
            //向下取整,没有小数位
            defensePower = Mathf.Round(defensePower * 1) / 1;
        }

        public float GetCurrentHealth()
        {
            return currentHealth;
        }

        public float GetMaxHealth()
        {
            return maxHealth;
        }

        public void AddMaxHealth(float maxHealth)
        {
            this.maxHealth += maxHealth;
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

        public void SetHealthRecover(float healthRecover)
        {
            this.healthRecover = healthRecover;
        }

        public void Damage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                isDead = true;
            }
        }

        public int GetMaxAttackCount()
        {
            return maxAttackCount;
        }

        public void AddMaxAttackCount(int attackCount)
        {
            this.maxAttackCount += attackCount;
            Debug.Log($"最大攻击次数增加{attackCount}次,当前最大攻击次数为{maxAttackCount}");
        }

        public int GetComboCount()
        {
            return comboCount;
        }

        public void AddComboCount(int comboCount)
        {
            this.comboCount += comboCount;
            Debug.Log($"连击次数增加{comboCount}次,当前连击次数为{comboCount}");
        }

        public int GetEmpowerCharge()
        {
            return empowerCharge;
        }

        public Element GetCurrentElement()
        {
            return currentElement;
        }

        public void SetCurrentElement(Element element)
        {
            currentElement = element;
        }

        public bool GetSplit()
        {
            return isSplit;
        }

        public void SetSplit(bool isSplit)
        {
            this.isSplit = isSplit;
        }

        public int GetSplitCount()
        {
            return splitCount;
        }

        public void SetSplitCount(int splitCount)
        {
            this.splitCount = splitCount;
        }

        public void AddSplitCount(int count)
        {
            this.splitCount += count;
        }

        public int GetMultiTargetCount()
        {
            return multiTargetCount;
        }

        public void AddMultiTargetCount(int count)
        {
            this.multiTargetCount += count;
            Debug.Log($"多目标锁定数量增加{count}个,当前可锁定{multiTargetCount}个目标");
        }
    }
}