using System.Collections;
using System.Collections.Generic;
using Manager;
using Photon.Pun;
using UnityEngine;

namespace Enemies
{
    public class EnemiesGenorator : MonoBehaviourPun
    {
        [SerializeField] GameObject testPrefab;
        private bool isPhotonReady = false; // Photon是否准备就绪
        private float spawnInterval = 1f; // 生成间隔
        private float timer = 0f; // 计时器
        bool canGenorate = false;
        void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return; // 只有房主执行生成逻辑
            if (!isPhotonReady)
            {
                // 更清晰的方式
                if (GameManager.isTest)
                {
                    canGenorate = true;  // 测试模式：无条件允许
                }
                else
                {
                    canGenorate = PhotonNetwork.IsConnectedAndReady &&
                                  PhotonNetwork.InRoom &&
                                  PhotonNetwork.IsMasterClient;
                    if (canGenorate)
                    {
                        isPhotonReady = true;
                    }
                }
                // Debug.Log($"IsConnectedAndReady: {PhotonNetwork.IsConnectedAndReady}");
                // Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
                // Debug.Log($"IsMasterClient: {PhotonNetwork.IsMasterClient}");
                // Debug.Log($"canGenorate: {canGenorate}");
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
            if (GameManager.isTest)
            {
                if (testPrefab != null)
                {
                    Debug.Log("生成测试敌人");
                    Instantiate(testPrefab, transform.position, Quaternion.identity);
                }
                return;
            }
            // 生成敌人
            PhotonNetwork.Instantiate("SkeletonRoot", transform.position, Quaternion.identity);
        }
    }
}
