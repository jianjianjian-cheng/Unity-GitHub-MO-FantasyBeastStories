using UnityEngine;
using Core;

namespace Controllers.Combat
{
    public class EnemyRollTrigger : MonoBehaviour
    {
        protected ColliderTriggerManager.TriggerInfo triggerInfo;
        private bool hasHit = false;
        private ColliderTriggerManager CTM;

        public void Setup(ColliderTriggerManager.TriggerInfo info)
        {
            triggerInfo = info;
            CTM = ColliderTriggerManager.Instance;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            if (other.CompareTag("Player"))
            {
                HandlePlayerHit(other.gameObject);
            }
        }

        private void HandlePlayerHit(GameObject player)
        {
            if (triggerInfo.damageEventArgs == null)
            {
                triggerInfo.damageEventArgs = new DamageEventArgs(
                    triggerInfo.element,
                    triggerInfo.triggerGameObject,
                    player,
                    triggerInfo.damage,
                    false,
                    0f
                );
            }

            EventChannelLocator.MainContainer.playerDamageEventChannel.Raise(triggerInfo.damageEventArgs);
        }
    }
}
