using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Enemies
{
    public class EnemiesGenorator : MonoBehaviourPun
    {
        private bool isPhotonReady = false; // Photon是否准备就绪
        private float spawnInterval = 3f; // 生成间隔
        private float timer = 0f; // 计时器
        void Update()
        {
            if (!isPhotonReady && PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (timer >= spawnInterval)
                    {
                        SpawnEnemy();
                        timer = 0f;
                    }
                    else
                    {
                        timer += Time.deltaTime;
                    }
                }
            }
        }

        private void SpawnEnemy()
        {
            // 生成敌人
            PhotonNetwork.Instantiate("SkeletonRoot", transform.position, Quaternion.identity);
        }
    }
}
