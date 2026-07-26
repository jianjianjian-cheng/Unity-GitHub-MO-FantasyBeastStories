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

        // ===== GC 优化：共享实例，避免每次命中 new 分配 =====
        // Unity 单线程 + 事件通道同步调用，监听者只读不存引用，安全复用
        private static DamageEventArgs _shared;

        public static DamageEventArgs GetShared(
            Element element,
            GameObject damgeSource,
            GameObject damgeTarget,
            float baseDamageValue,
            bool isCritical,
            float criticalMultiplier
        )
        {
            if (_shared == null)
            {
                _shared = new DamageEventArgs(element, damgeSource, damgeTarget,
                    baseDamageValue, isCritical, criticalMultiplier);
            }
            else
            {
                _shared.element = element;
                _shared.damgeSource = damgeSource;
                _shared.damgeTarget = damgeTarget;
                _shared.baseDamageValue = baseDamageValue;
                _shared.isCritical = isCritical;
                _shared.criticalMultiplier = criticalMultiplier;
                _shared.finalDamageValue = 0;
            }
            return _shared;
        }

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
