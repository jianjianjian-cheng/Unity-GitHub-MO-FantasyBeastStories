using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscortRobotTask : MonoBehaviour
{
    [Header("粒子系统")]
    [SerializeField]
    private ParticleSystem transferParticle;

    [SerializeField]
    private ParticleSystem portalParticle;

    [Header("参数配置")]
    private int currentCount = 0;
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
    }

    private void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("TaskNetObject"))
        {
            currentObjects.Add(other.gameObject);
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
        return currentObjects.Count >= currentCount;
    }


    /// <summary>
    /// 数量满足要求，开始传送
    /// </summary>
    private void StartTransfer()
    {
        if (CheckCount())
        {
           
        }
    }
}
