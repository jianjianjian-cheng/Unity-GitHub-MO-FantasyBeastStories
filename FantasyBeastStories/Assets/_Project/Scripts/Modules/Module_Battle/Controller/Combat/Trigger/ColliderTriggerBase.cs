using UnityEngine;
using Core;
using Core.SharedModel;

namespace Controllers.Battle
{
    public class ColliderTriggerBase : MonoBehaviour
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

            if (triggerInfo.triggerType == TriggerType.EnemyAttack)
            {
                if (other.CompareTag("Player"))
                {
                    hasHit = true;
                    HandlePlayerHit(other.gameObject);
                }
            }
            else if (triggerInfo.triggerType == TriggerType.Roll)
            {
                if (other.CompareTag("Player"))
                {
                    HandlePlayerHit(other.gameObject);
                }
            }
            else if (triggerInfo.triggerType == TriggerType.Bullet || triggerInfo.triggerType == TriggerType.Custom)
            {
                if (other.CompareTag("Enemy"))
                {
                    HandleEnemyHit(other.gameObject);
                }
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

        private void HandleEnemyHit(GameObject enemy)
        {
            if (triggerInfo.damageEventArgs == null)
            {
                triggerInfo.damageEventArgs = new DamageEventArgs(
                    triggerInfo.element,
                    triggerInfo.triggerGameObject,
                    enemy,
                    triggerInfo.damage,
                    false,
                    0f
                );
            }

            EventChannelLocator.MainContainer.damageEventChannel.Raise(triggerInfo.damageEventArgs);
        }
    }
}
