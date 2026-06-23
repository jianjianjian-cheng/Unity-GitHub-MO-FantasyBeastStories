using System.Collections;
using System.Collections.Generic;
using Manager;
using Photon.Pun;
using UnityEngine;

namespace Enemies
{
    public class EnemiesGenorator : MonoBehaviourPun
    {
        [SerializeField]
        GameObject testPrefab;
        private bool isPhotonReady = false; // Photon是否准备就绪
        private float spawnInterval; // 生成间隔
        private float timer = 0f; // 计时器
        bool canGenorate = false;

        void Start()
        {
            spawnInterval = Random.Range(4f, 4f); // 随机生成间隔
        }

        void Update()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!PhotonNetwork.IsMasterClient)
                return; // 只有房主执行生成逻辑
            if (!isPhotonReady)
            {
                // 更清晰的方式
                if (GameManager.isTest)
                {
                    canGenorate = true; // 测试模式：无条件允许
                }
                else
                {
                    canGenorate =
                        PhotonNetwork.IsConnectedAndReady
                        && PhotonNetwork.InRoom
                        && PhotonNetwork.IsMasterClient;
                    if (canGenorate)
                    {
                        isPhotonReady = true;
                    }
                }
            }
            if (canGenorate)
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

        private void SpawnEnemy()
        {
            if (GamePauseManager.isPaused)
                return;
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            // 生成敌人
            NetworkObjectPoolManager.instance.Spawn(
                NetworkObjectPoolConst.Skeleton,
                transform.position
            );
        }
    }
}
