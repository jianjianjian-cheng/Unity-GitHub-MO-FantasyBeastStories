using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    public class Skeleton : AttackableEnemy
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            RegisterDamageEvent();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterDamageEvent();
        }
    }
}