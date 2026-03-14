using System.Collections;
using System.Collections.Generic;
using Manager;
using UnityEngine;

public class ImportFXHandle : MonoBehaviour
{
    [SerializeField] private string poolName; // 对象池名称
    void OnDisable()
    {
        Debug.Log("FX disabled, returning to pool: " + poolName);
        if (ManagerBase.instance != null)
        {
            var opm = ManagerBase.instance.GetComponent<ObjectPoolManager>();
            if (opm != null)
            {
                opm.ReturnToPool(poolName, gameObject);
            }
        }
    }
}
