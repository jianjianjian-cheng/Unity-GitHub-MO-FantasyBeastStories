using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public enum DamageType
    {
        normal,
        Fire,
        Ice,
        Lightning,
    }

    public class DamageEventArgs : EventArgsBase
    {
        public Element element;
        public GameObject damgeSource;
        public GameObject damgeTarget;
        public float baseDamageValue;
        public float finalDamageValue;
        public bool isCritical; //是否暴击
        public float criticalMultiplier; //暴击倍率

        public DamageEventArgs(
            Element element,
            GameObject damgeSource,
            GameObject damgeTarget,
            float baseDamageValue,
            bool isCritical,
            float criticalMultiplier
        )
            : base()
        {
            this.element = element;
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
    }
}
