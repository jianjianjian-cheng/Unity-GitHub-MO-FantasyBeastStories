using System.Collections.Generic;
using Enemies;
using Photon.Pun;
using UnityEngine;

namespace Manager
{
    public class GamePlayingManager : MonoBehaviour
    {
        #region 单例模式
        public static GamePlayingManager instance;

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
        #endregion
    }
}