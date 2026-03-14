using System.Collections;
using System.Collections.Generic;
using Trigger;
using UnityEngine;

namespace Trigger
{
    public class FlyBallTrigger : TriggerBase
    {
        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
        }
        public override void OnTriggerStay(Collider other)
        {
            base.OnTriggerStay(other);
        }
        public override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
        }

    }
}
