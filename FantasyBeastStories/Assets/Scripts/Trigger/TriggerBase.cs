using System.Collections;
using System.Collections.Generic;
using Enemies;
using FX;
using UnityEngine;

namespace Trigger
{
    public class TriggerBase : MonoBehaviour
    {
        protected FireBallBase ballBase;
        // Start is called before the first frame update
        public virtual void Start()
        {
            ballBase = GetComponentInParent<FireBallBase>();
        }

        public virtual void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }
            if (other.CompareTag("Enemy"))
                if (ballBase != null)
                {
                    ballBase.HandleEnemyCollisionEnter(other);
                }
        }

        public virtual void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }
            if (other.CompareTag("Enemy"))
            {
                if (ballBase != null)
                {
                    ballBase.HandleEnemyCollisionStay(other);
                }
            }
        }

        public virtual void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }
            if (other.CompareTag("Enemy"))
            {
                if (ballBase != null)
                {
                    ballBase.HandleEnemyCollisionExit(other);
                }
            }
        }

    }
}
