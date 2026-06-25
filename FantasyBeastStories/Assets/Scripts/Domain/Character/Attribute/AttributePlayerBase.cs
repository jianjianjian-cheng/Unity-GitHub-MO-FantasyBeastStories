using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//枚举，记录当前元素

public enum Element
{
    Common,
    Lightning,
    Winter,
    Grass,
}

namespace Domain.Character.Attribute
{
    public class AttributePlayerBase
    {
        protected bool isDead = false; //是否死亡,默认false
        protected float attackPower;
        protected float defensePower = 0; //防御伤害,默认0
        protected float criticalMultiplier = 1.2f; //暴击倍率,默认1倍
        protected float criticalChance = 0.2f; //暴击概率,默认20%
        protected float maxHealth = 100f; //最大生命值,默认100
        protected float currentHealth; //当前生命值
        protected float moveSpeed = 2f; //移动速度,默认2f
        protected float attackInterval = 2; //攻击间隔
        protected float healthRecover = 0f; //生命值恢复,默认0f
        protected float attackspeed = 100f;
        protected int maxAttackCount = 1;
        protected int comboCount = 1;
        protected int empowerCharge;
        protected Element currentElement = Element.Common;

        protected bool isSplit = false;
        protected int splitCount = 0;

        public AttributePlayerBase(
            float attackPower,
            float defensePower,
            float maxHealth,
            float moveSpeed,
            float criticalMultiplier,
            float criticalChance
        )
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
            //初始化最大攻击次数为默认值
            maxAttackCount = 1;
            //初始化连击次数为默认值
            comboCount = 1;
            empowerCharge = 1;
            //初始化当前元素为默认值
            currentElement = Element.Common;

            //初始化是否分割为默认值
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
            //如果攻击间隔小于0,则设置为0
            if (attackInterval < 0.5f)
            {
                attackInterval = 0.5f;
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
            //如果暴击概率大于0.8,则设置为0.8
            if (criticalChance > 0.8f)
            {
                criticalChance = 0.8f;
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
            currentHealth -= damage;
            if (currentHealth < 0)
            {
                currentHealth = 0;
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
    }
}