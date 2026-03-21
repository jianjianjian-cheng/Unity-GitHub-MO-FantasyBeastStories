using System.Collections;
using System.Collections.Generic;
using Enemies;
using FX;
using Photon.Pun;
using UnityEngine;

namespace Trigger
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
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }

        }

        public virtual void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }

        }

        public virtual void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
            {
                return;
            }
        }

    }
}
