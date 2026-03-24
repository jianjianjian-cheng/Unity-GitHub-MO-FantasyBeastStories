using System.Collections;
using System.Collections.Generic;
using Events;
using UnityEngine;

public enum DamageType
{
    Fire,
    Ice,
    Lightning,
}
namespace Events
{
    public class DamageEventArgs : EventArgsBase
    {
        public DamageType damageType;
        public GameObject damgeSource;
        public GameObject damgeTarget;
        public float baseDamageValue;
        public float finalDamageValue;
        public bool isCritical;//是否暴击
        public float criticalMultiplier;//暴击倍率

        public DamageEventArgs(DamageType damageType, GameObject damgeSource, GameObject damgeTarget, float baseDamageValue, bool isCritical, float criticalMultiplier)
        : base()
        {

            this.damageType = damageType;
            this.damgeSource = damgeSource;
            this.damgeTarget = damgeTarget;
            this.baseDamageValue = baseDamageValue;
            this.isCritical = isCritical;
            this.criticalMultiplier = criticalMultiplier;
        }

        public void CalculateFinalDamageValue()
        {
            finalDamageValue = baseDamageValue;
            if (isCritical)
            {
                finalDamageValue *= criticalMultiplier;
            }
        }

        public GameObject GetDamgeTarget()
        {
            return damgeTarget;
        }
    }
}
