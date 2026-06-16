using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class EscortRobotTask : MonoBehaviour
{
    [Header("粒子系统")]
    [SerializeField]
    private ParticleSystem transferParticle;

    [SerializeField]
    private ParticleSystem portalParticle;
    [SerializeField]
    private float delayBeforeTransfer = 3f; // 传送前的延迟时间

    [Header("参数配置")]
    private int requiredCount = 3;//需要传送的机器人数量

    string robotPrefabpath = "TaskNetPrefab/Ball Robot_Blue";//机器人预制体路径
    
    

    List<GameObject> currentObjects = new List<GameObject>();//记录当前已经进入的机器人

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
        // 圆环区域：内半径1米，外半径2米
        // 空心部分不会生成，只在环内生成

        float innerRadius = 10f;  // 内圈半径
        float outerRadius = 20f;  // 外圈半径

        for (int i = 0; i < requiredCount; i++)
        {
            // 随机角度
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // 随机半径（在内圈和外圈之间）
            float radius = UnityEngine.Random.Range(innerRadius, outerRadius);


            // 计算位置
            Vector3 spawnPosition = transform.position + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            PhotonNetwork.InstantiateRoomObject(robotPrefabpath, spawnPosition, Quaternion.identity);
        }   
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("TaskNetObject"))
        {
            Debug.Log("机器人进入传送区域: " + other.gameObject.name);
            currentObjects.Add(other.gameObject);
            TaskManager.instance.ReportCount(other.gameObject.transform.position,
            other.gameObject.GetComponent<PhotonView>()
            );
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


    /// <summary>
    /// 检查是否满足传送条件
    /// </summary>
    /// <returns></returns>
    private bool CheckCount()
    {
        return currentObjects.Count >= requiredCount;
    }


    /// <summary>
    /// 数量满足要求，开始传送
    /// </summary>
    IEnumerator StartTransfer()
    {
        if (CheckCount())
        {
            yield return new WaitForSeconds(delayBeforeTransfer);
            portalParticle.Play();
           foreach (var obj in currentObjects)
            {
                yield return new WaitForSeconds(1f); // 每个机器人之间的延迟
                obj.GetComponent<BallRobot_Blue>().StartTransfer();
            }
        }
    }
}
