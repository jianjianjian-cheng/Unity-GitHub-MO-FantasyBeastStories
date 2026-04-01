using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Trigger;
using UnityEngine;

namespace Other
{
    public class SpawnPoint : MonoBehaviourPun
    {
        public bool isEmpty = true; // 是否为空闲的生成点
        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                isEmpty = false;
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                isEmpty = true;
            }
        }
    }
}
