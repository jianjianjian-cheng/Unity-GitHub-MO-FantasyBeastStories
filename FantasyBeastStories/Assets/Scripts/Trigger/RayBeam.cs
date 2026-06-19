using System.Collections;
using System.Collections.Generic;
using Events;
using Manager;
using Unity.VisualScripting;
using UnityEngine;

namespace Trigger
{
public class RayBeam : MonoBehaviour
    {
        private GameObject owner;
        private float rayBeamDamage;

        public void SetOwnerAndAttribute(GameObject owner, float rayBeamDamage)
        {
            this.owner = owner;
            this.rayBeamDamage = rayBeamDamage;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                if(owner != null)
                {
                    Debug.Log("RayBeam Damage Player");
                        DamageEventArgs damageEventArgs = new DamageEventArgs(
                        Element.Common,
                        owner,
                        other.gameObject,
                        rayBeamDamage,
                        false,
                        1f
                    );

                    EventManager.instance.TriggerEventComplex(
                        EventNames.DamageReceiverPlayer,
                        damageEventArgs
                    );
                }
            }
        }

        void OnTriggerStay(Collider other)
        {
            
        }

        void OnTriggerExit(Collider other)
        {
            
        }
    }
}