using System.Collections;
using System.Collections.Generic;
using Domain.Enemy;
using Infrastructure.FX.FireBall;
using Photon.Pun;
using UnityEngine;

namespace Domain.Combat.Trigger
{
    public class TriggerBase : MonoBehaviourPun
    {
        // Start is called before the first frame update
        public virtual void Start()
        {

        }

        public virtual void Update()
        {

        }

        public virtual void OnTriggerEnter(Collider other)
        {
            var enemyBase = other.gameObject.GetComponent<EnemyBase>();
            if (!other.gameObject.CompareTag("Enemy") || enemyBase == null || enemyBase.GetIsDie())
            {
                return;
            }

        }

        public virtual void OnTriggerStay(Collider other)
        {
            var enemyBase = other.gameObject.GetComponent<EnemyBase>();
            if (!other.gameObject.CompareTag("Enemy") || enemyBase == null || enemyBase.GetIsDie())
            {
                return;
            }

        }

        public virtual void OnTriggerExit(Collider other)
        {
            var enemyBase = other.gameObject.GetComponent<EnemyBase>();
            if (!other.gameObject.CompareTag("Enemy") || enemyBase == null || enemyBase.GetIsDie())
            {
                return;
            }
        }
    }
}