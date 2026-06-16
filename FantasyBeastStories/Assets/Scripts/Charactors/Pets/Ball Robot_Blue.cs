using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class BallRobot_Blue : MonoBehaviourPun
{
    [Header("移动设置")]
    [SerializeField]
    private float moveDistance = 3f; // 被推后移动的距离

    [SerializeField]
    private float moveSpeed = 2f; // 移动速度

    [SerializeField]
    private float turnSpeed = 5f; // 转向速度（小羊慢慢转）

    [SerializeField]
    private float pushForceThreshold = 0.5f; // 推动力阈值

    [Header("行为设置")]
    private bool isTransfering = false; // 是否正在传送
    [SerializeField]
    private float turnDelay = 0.3f; // 转向前的延迟

    [SerializeField]
    private float moveDelay = 0.5f; // 开始移动前的延迟

    [SerializeField]
    private AnimationCurve turnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 转向缓动曲线

    private UnityEngine.AI.NavMeshAgent agent;
    private Rigidbody rb;
    private Vector3 pushDirection;
    private Vector3 targetDirection;
    private bool isPushed = false;
    private bool isMovingToDestination = false;
    private bool isTurning = false;
    private float originalMoveDistance; // 保存原始移动距离



    [Header("传送设置")]
    [SerializeField]
    private float transferDuration = 2f;//传送持续时间
    [SerializeField]
    private float maxRaiseSpeed = 3f;
    [SerializeField]
    private float rotateSpeed = 90f;//旋转速度
    [SerializeField]
    private AnimationCurve raiseCurve;


    // 网络同步变量
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private bool networkIsMoving;

    //组件
    [SerializeField]
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // 保存原始移动距离
        originalMoveDistance = moveDistance;

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.acceleration = 4f; // 降低加速度，更温顺
            agent.angularSpeed = 120f; // 降低角速度，慢慢转
            agent.stoppingDistance = 0.1f;
            agent.autoBraking = true;
            agent.enabled = false;
        }

        if (rb != null)
        {
            rb.mass = 2f;
            rb.drag = 2f; // 增加阻力，移动更柔和
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.isKinematic = true; // 设为运动学，不产生物理推力
        }

        if (animator != null)
        {
            animator.SetBool("isRun", false);
        }
    }

    private void Update()
    {
        if (isTransfering) return;

        if (isPushed && !isTurning && !isMovingToDestination && rb.velocity.magnitude < 0.1f)
        {
            StartCoroutine(TurnToDirection());
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Player"))
        {
            // 如果已经在移动中，忽略新的碰撞
            if (isPushed || isMovingToDestination)
                return;
            PlayTriggerAnimation("EnterAera"); 
            //计算推动方向
            pushDirection = (
                transform.position - collision.gameObject.transform.position
            ).normalized;
            pushDirection.y = 0;

            float pushForce = collision.relativeVelocity.magnitude;

            if (pushForce > pushForceThreshold)
            {
                // 根据力度计算本次移动距离，但不改变原始值
                float currentMoveDistance = Mathf.Clamp(
                    originalMoveDistance * (pushForce / 5f),
                    1f,
                    originalMoveDistance
                );

                // 将计算结果存到 moveDistance
                moveDistance = currentMoveDistance;

                // 通过 RPC 广播推动事件到所有客户端
            photonView.RPC("RPC_OnPushed", RpcTarget.All, pushDirection, moveDistance);
            }
        }
    }

    [PunRPC]
    private void PlayTriggerAnimation(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    [PunRPC]
    void RPC_OnPushed(Vector3 pushDir, float moveDist)
    {
        this.pushDirection = pushDir;
        this.moveDistance = moveDist;
        this.isPushed = true;
        
        Debug.Log($"BallRobot_Blue pushed with force, move distance: {moveDistance}");
    }

    IEnumerator TurnToDirection()
    {
        isTurning = true;

        yield return new WaitForSeconds(turnDelay);

        Vector3 targetDirection = pushDirection.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion startRotation = transform.rotation;

        float turnDuration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < turnDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = turnCurve.Evaluate(elapsedTime / turnDuration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
        isTurning = false;

        // 转向完成后开始移动
        StartCoroutine(MoveToDestination());
    }

    IEnumerator MoveToDestination()
    {
        animator.SetBool("isRun", true);
        isMovingToDestination = true;
        yield return new WaitForSeconds(moveDelay);

        Vector3 targetPosition = transform.position + pushDirection.normalized * moveDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, moveDistance, NavMesh.AllAreas))
        {
            if (agent != null)
            {
                agent.enabled = true;
                agent.SetDestination(hit.position);
                StartCoroutine(CheckArrival());
            }
        }
        else
        {
            // 目标不可达，尝试短距离
            targetPosition = transform.position + pushDirection * (moveDistance * 0.5f);
            if (
                NavMesh.SamplePosition(
                    targetPosition,
                    out hit,
                    moveDistance * 0.5f,
                    NavMesh.AllAreas
                )
            )
            {
                if (agent != null)
                {
                    agent.enabled = true;
                    agent.SetDestination(hit.position);
                    StartCoroutine(CheckArrival());
                }
            }
            else
            {
                Debug.LogWarning("BallRobot_Blue: No valid NavMesh position found");
                ReturnToIdle();
            }
        }
    }

    IEnumerator CheckArrival()
    {
        while (isMovingToDestination)
        {
            if (agent != null && agent.enabled && !agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    //到达目标点回调
                    OnArrivedAtDestination();
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void OnArrivedAtDestination()
    {
        Debug.Log("BallRobot_Blue arrived at destination");
        ReturnToIdle();
    }

    private void ReturnToIdle()
    {
        isPushed = false;
        isMovingToDestination = false;
        isTurning = false;

        // 恢复原始移动距离
        moveDistance = originalMoveDistance;

        animator.SetBool("isRun", false);

        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        Debug.Log("BallRobot_Blue returned to idle");
    }

    private void OnDisable()
    {
        // 清理协程和状态
        StopAllCoroutines();
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }
    }

    private void OnDestroy()
    {
        // 清理协程和状态
        StopAllCoroutines();
    }

    /// <summary>
    /// 场景中可视化路径
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (isMovingToDestination && agent != null && agent.enabled)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(agent.destination, 0.3f);
            Gizmos.DrawLine(transform.position, agent.destination);
        }

        if (isPushed)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, pushDirection * 2f);
        }
    }

    public void StartTransfer()
    {
        // 通过RPC同步到所有客户端
        photonView.RPC("RPC_StartTransfer", RpcTarget.All);
    }

    [PunRPC]
    void RPC_StartTransfer()
    {
        isTransfering = true;
        StopAllCoroutines();
        StartCoroutine(Transfer());
    }

    private IEnumerator Transfer()
    {
        // 停止原有移动行为
        isPushed = false;
        isMovingToDestination = false;

        // 传送动画和效果
        float elapsedTime = 0f;
        while (elapsedTime < transferDuration)
        {
            elapsedTime += Time.deltaTime;
            float linearProgress = elapsedTime/transferDuration;
            float curvedProgress = raiseCurve.Evaluate(linearProgress);

            transform.position += Vector3.up * curvedProgress * maxRaiseSpeed * Time.deltaTime;
            yield return null;
        }

        // 传送完成处理
        isTransfering = false;
        OnTeleportComplete();
        yield break;
    }

    private void OnTeleportComplete()
    {
        Destroy(gameObject);
    }
}
