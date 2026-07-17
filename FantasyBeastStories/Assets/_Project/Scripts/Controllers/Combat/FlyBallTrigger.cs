using System.Collections;
using System.Collections.Generic;
using Controllers.Enemy;
using Controllers.Combat;
using Core;
using UnityEngine;

namespace Controllers.Combat
{
  public class FlyBallTrigger : TriggerBase
  {
    protected IFireBallBase ballBase;
    public override void Start()
    {
      ballBase = GetComponentInParent<IFireBallBase>();
    }
    public override void OnTriggerEnter(Collider other)
    {
      if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
      {
        return;
      }
      base.OnTriggerEnter(other);
      if (other.CompareTag("Enemy"))
        if (ballBase != null)
        {
          ballBase.HandleEnemyCollisionEnter(other);
        }
      Vector3 hitPosition = other.ClosestPoint(transform.position);
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateGet("FireBallHitEffectPool", hitPosition, null));
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateReturn("FireBallPool", gameObject.transform.parent.gameObject));
    }
    public override void OnTriggerStay(Collider other)
    {
      if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
      {
        return;
      }
      base.OnTriggerStay(other);
      if (other.CompareTag("Enemy"))
      {
        if (ballBase != null)
        {
          ballBase.HandleEnemyCollisionStay(other);
        }
      }
    }
    public override void OnTriggerExit(Collider other)
    {
      if (!other.CompareTag("Enemy") || other.GetComponent<EnemyBase>().GetIsDie())
      {
        return;
      }
      base.OnTriggerExit(other);
      if (other.CompareTag("Enemy"))
      {
        if (ballBase != null)
        {
          ballBase.HandleEnemyCollisionExit(other);
        }
      }
    }

  }
}