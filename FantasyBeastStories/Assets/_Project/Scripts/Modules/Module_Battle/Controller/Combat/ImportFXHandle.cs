using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Controllers.Battle
{
    public class ImportFXHandle : MonoBehaviour
    {
        [SerializeField] private string poolName;
        void OnDisable()
        {
            PoolHelper.Return(poolName, gameObject);
        }
    }
}
