using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class ManagerBase : MonoBehaviour
    {
        //单例
        public static ManagerBase instance;
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
