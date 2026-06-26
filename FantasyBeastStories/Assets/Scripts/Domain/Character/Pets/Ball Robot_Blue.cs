using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Domain.Network;
using Photon.Pun;

namespace Domain.Character.Pets
{
  public class BallRobot_Blue : MonoBehaviour
  {
    [SerializeField] private NetworkIdentityBase _network;

    [Header("移动设置")]
    [SerializeField]
    private float moveDistance = 3f;

    [SerializeField]
    private float moveSpeed = 2f;

    [SerializeField]
    private float turnSpeed = 5f;

    [SerializeField]
    private float pushForceThreshold = 0.5f;

    [Header("行为设置")]
    private bool isTransfering = false;
    [SerializeField]
    private float turnDelay = 0.3f;

    [SerializeField]
    private float moveDelay = 0.5f;

    [SerializeField]
    private AnimationCurve turnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private UnityEngine.AI.NavMeshAgent agent;
    private Rigidbody rb;
    private Vector3 pushDirection;
    private Vector3 targetDirection;
    private bool isPushed = false;
    private bool isMovingToDestination = false;
    private bool isTurning = false;
    private float originalMoveDistance;

    [Header("传送设置")]
    [SerializeField]
    private float transferDuration = 2f;
    [SerializeField]
    private float maxRaiseSpeed = 3f;
    [SerializeField]
    private float rotateSpeed = 90f;
    [SerializeField]
    private AnimationCurve raiseCurve;

    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private bool networkIsMoving;

    [SerializeField]
    private Animator animator;

    void Start()
    {
      animator = GetComponent<Animator>();
      agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
      rb = GetComponent<Rigidbody>();

      if (_network == null)
        _network = GetComponent<NetworkIdentityBase>();

      originalMoveDistance = moveDistance;

      if (agent != null)
      {
        agent.speed = moveSpeed;
        agent.acceleration = 4f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 0.1f;
        agent.autoBraking = true;
        agent.enabled = false;
      }

      if (rb != null)
      {
        rb.mass = 2f;
        rb.drag = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.isKinematic = true;
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
        if (isPushed || isMovingToDestination)
          return;
        PlayTriggerAnimation("EnterAera");
        pushDirection = (
            transform.position - collision.gameObject.transform.position
        ).normalized;
        pushDirection.y = 0;

        float pushForce = collision.relativeVelocity.magnitude;

        if (pushForce > pushForceThreshold)
        {
          float currentMoveDistance = Mathf.Clamp(
              originalMoveDistance * (pushForce / 5f),
              1f,
              originalMoveDistance
          );

          moveDistance = currentMoveDistance;

          _network.RPC("RPC_OnPushed", NetworkTarget.All, pushDirection, moveDistance);
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
        elapsedTime += UnityEngine.Time.deltaTime;
        float t = turnCurve.Evaluate(elapsedTime / turnDuration);
        transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
        yield return null;
      }

      transform.rotation = targetRotation;
      isTurning = false;

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
      StopAllCoroutines();
      if (agent != null && agent.enabled)
      {
        agent.enabled = false;
      }
    }

    private void OnDestroy()
    {
      StopAllCoroutines();
    }

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
      _network.RPC("RPC_StartTransfer", Domain.Network.NetworkTarget.All);
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
      isPushed = false;
      isMovingToDestination = false;

      float elapsedTime = 0f;
      while (elapsedTime < transferDuration)
      {
        elapsedTime += UnityEngine.Time.deltaTime;
        float linearProgress = elapsedTime / transferDuration;
        float curvedProgress = raiseCurve.Evaluate(linearProgress);

        transform.position += Vector3.up * curvedProgress * maxRaiseSpeed * UnityEngine.Time.deltaTime;
        yield return null;
      }

      isTransfering = false;
      OnTeleportComplete();
      yield break;
    }

    private void OnTeleportComplete()
    {
      Destroy(gameObject);
    }
  }
}