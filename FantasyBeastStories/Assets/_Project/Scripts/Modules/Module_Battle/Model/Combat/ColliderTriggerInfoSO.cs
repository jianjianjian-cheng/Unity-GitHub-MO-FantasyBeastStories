using UnityEngine;
using Core.SharedModel;

namespace Controllers.Battle
{
    [CreateAssetMenu(fileName = "ColliderTriggerInfoSO", menuName = "Combat/ColliderTriggerInfoSO")]
    public class ColliderTriggerInfoSO : ScriptableObject
    {
        public TriggerType triggerType;
        public Element element;
        public float damage;
        public float lifeTime;
        public float scale;
        public ColliderType colliderType;
    }
}
