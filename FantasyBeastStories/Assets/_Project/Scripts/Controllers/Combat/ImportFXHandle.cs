using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Controllers.Combat
{
    public class ImportFXHandle : MonoBehaviour
    {
        [SerializeField] private string poolName;
        void OnDisable()
        {
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateReturn(poolName, gameObject));
        }
    }
}
