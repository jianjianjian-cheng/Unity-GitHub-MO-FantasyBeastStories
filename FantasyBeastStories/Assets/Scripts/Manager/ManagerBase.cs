using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Manager
{
    public class ManagerBase : MonoBehaviourPun
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
