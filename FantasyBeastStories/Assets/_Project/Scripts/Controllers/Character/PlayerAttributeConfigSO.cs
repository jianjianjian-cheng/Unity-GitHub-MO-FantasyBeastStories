using UnityEngine;

namespace Controllers.Character
{
    [CreateAssetMenu(menuName = "Config/Player Attribute")]
    public class PlayerAttributeConfigSO : ScriptableObject
    {
        [Header("基础属性")]
        public float baseAttackPower = 300f;
        public float baseDefensePower = 0f;
        public float baseMaxHealth = 300f;
        public float baseMoveSpeed = 5f;
        public float baseCriticalMultiplier = 1.2f;
        public float baseCriticalChance = 0.2f;

        [Header("上限限制")]
        public float maxCriticalChance = 0.8f;
        public float minAttackInterval = 0.5f;
        public float maxAttackInterval = 2f;

        [Header("恢复与速度")]
        public float baseHealthRecover = 0f;
        public float baseAttackSpeed = 100f;

        [Header("攻击次数")]
        public int baseMaxAttackCount = 1;
        public int baseComboCount = 1;
        public int baseEmpowerCharge = 1;

        [Header("多目标锁定（BingNv 专属）")]
        public int baseMultiTargetCount = 3;
    }
}