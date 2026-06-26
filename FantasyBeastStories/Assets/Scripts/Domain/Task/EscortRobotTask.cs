using System;
using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Domain.Event.Channels.Combat;
using Domain.Character.Pets;
using Domain.Services;
using UnityEngine;

namespace Domain.Task
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
                NetworkServiceLocator.ObjectService.GetViewID(other.gameObject)
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
                portalParticle.Play();
                foreach (var obj in currentObjects)
                {
                    yield return new WaitForSeconds(1f);
                    obj.GetComponent<BallRobot_Blue>().StartTransfer();
                }
            }
        }

        void OnDestroy()
        {
            foreach (var obj in currentObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
    }
}