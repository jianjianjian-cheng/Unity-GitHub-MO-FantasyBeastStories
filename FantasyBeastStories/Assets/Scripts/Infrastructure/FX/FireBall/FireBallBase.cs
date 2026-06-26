using System.Collections;
using System.Collections.Generic;
using Domain.Enemy;
using Domain.Combat.FX;
using UnityEngine;

namespace Infrastructure.FX.FireBall
{
  public class FireBallBase : MonoBehaviour, IFireBallBase
  {
    protected GameObject tagetEnemy;
    [SerializeField] protected float moveSpeed = 4f; // 移动速度
                                                     // Update is called once per frame
    public virtual void Update()
    {
      //自动追踪敌人
      if (tagetEnemy == null)
      {
        return;
      }
      //如果敌人已经死亡，火球以当前速度和方向直线飞行
      if (tagetEnemy.GetComponent<EnemyBase>().GetIsDie())
      {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        return;
      }
      // 获取敌人位置
      Transform getHitPos = tagetEnemy.transform.Find("GetHitPos");
      transform.LookAt(getHitPos != null ? getHitPos : tagetEnemy.transform);
      // 移动 towards the enemy
      transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    public virtual void SetTarget(GameObject target)
    {
      tagetEnemy = target;
    }

    public virtual void HandleEnemyCollisionEnter(Collider enemy)
    {

    }

    public virtual void HandleEnemyCollisionStay(Collider enemy)
    {

    }

    public virtual void HandleEnemyCollisionExit(Collider enemy)
    {

    }
  }
}
