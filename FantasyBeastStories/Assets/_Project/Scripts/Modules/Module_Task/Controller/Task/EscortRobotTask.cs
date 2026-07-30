using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Channels.Combat;
using Controllers.Character.Pets;
using Core.Contracts;
using Core.Network;
using UnityEngine;
using Core.SharedModel;

namespace Controllers.Task
{
    public class EscortRobotTask : MonoBehaviour
    {
        [Header("粒子系统")]
        [SerializeField]
        private ParticleSystem transferParticle;

        [SerializeField]
        private ParticleSystem portalParticle;
        [SerializeField]
        private float delayBeforeTransfer = 3f;

        [Header("参数配置")]
        private int requiredCount = 3;

        string robotPrefabpath = "TaskNetPrefab/Ball Robot_Blue";

        List<GameObject> currentObjects = new List<GameObject>();

        private void Start()
        {
            if (portalParticle != null)
            {
                portalParticle.Stop();
            }

            if (transferParticle != null)
            {
                transferParticle.Play();
            }
            GenerateRobots();
        }

        void GenerateRobots()
        {
            float innerRadius = 10f;
            float outerRadius = 20f;

            for (int i = 0; i < requiredCount + 3; i++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = UnityEngine.Random.Range(innerRadius, outerRadius);

                Vector3 spawnPosition = transform.position + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                NetworkServiceLocator.ObjectService.InstantiateRoomObject(robotPrefabpath, spawnPosition, Quaternion.identity);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("TaskNetObject"))
            {
                Debug.Log("机器人进入传送区域: " + other.gameObject.name);
                currentObjects.Add(other.gameObject);
                EventChannelLocator.MainContainer.enemyReportChannel.Raise(new EnemyReportData(other.gameObject.transform.position,
                NetworkServiceLocator.ObjectService.GetViewID(other.gameObject),
                EnemyReportType.EscortArrive
                ));
                StopAllCoroutines();
                StartCoroutine(StartTransfer());
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("TaskNetObject"))
            {
                currentObjects.Remove(other.gameObject);
            }
        }

        private bool CheckCount()
        {
            return currentObjects.Count >= requiredCount;
        }

        IEnumerator StartTransfer()
        {
            if (CheckCount())
            {
                yield return new WaitForSeconds(delayBeforeTransfer);

                // 激活传送特效子物体
                var teleport = transform.Find("Teleport_5");
                if (teleport != null)
                {
                    teleport.gameObject.SetActive(true);
                    Debug.Log("[EscortRobotTask] 已激活 Teleport_5 传送特效");
                }

                if (portalParticle != null)
                    portalParticle.Play();

                foreach (var obj in currentObjects)
                {
                    yield return new WaitForSeconds(1f);
                    if (obj != null)
                        obj.GetComponent<BallRobot_Blue>()?.StartTransfer();
                }
            }
        }

        void OnDestroy()
        {
            // 不在此强制销毁机器人 — BallRobot_Blue.Transfer() 完成后会自行 Destroy
        }
    }
}