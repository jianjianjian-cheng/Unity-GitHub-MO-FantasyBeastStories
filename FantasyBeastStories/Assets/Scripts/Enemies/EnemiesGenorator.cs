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

            if (!isPhotonReady)
            {
                canGenorate = GameManager.isTest || (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient);
                isPhotonReady = true;
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
