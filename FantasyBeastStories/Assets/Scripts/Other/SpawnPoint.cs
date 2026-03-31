using System.Collections;
using System.Collections.Generic;
using Trigger;
using UnityEngine;

namespace Other
{
    public class SpawnPoint : TriggerBase
    {
        public bool isEmpty = true; // 是否为空闲的生成点
        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            isEmpty = false;
        }

        public override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
            isEmpty = true;
        }
    }
}
