using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Domain.Character.Pets
{
  public class PetsBase : MonoBehaviour
  {
    [Header("纯数据")]
    [SerializeField] protected PetData petData;

    [SerializeField] protected GameObject hostPlayer; // 主人玩家
    [SerializeField] protected GameObject targetEnemy; // 目标敌人
    protected Animator animator; // 动画组件
    protected Rigidbody rb; // 物理组件

    protected virtual void Awake()
    {
      petData = new PetData();
      // 在Awake中初始化宠物属性
      InitializePetAttributes();
    }

    protected virtual void Start()
    {
      // 在Start中执行宠物的初始行为，例如播放Idle动画等
      IdleEnter();
    }

    #region 状态机
    protected virtual void Update()
    {
      // 根据当前状态执行相应的行为
      switch (petData.currentState)
      {
        case PetState.Idle:
          // 执行Idle状态的行为
          IdleStay();
          break;
        case PetState.Run:
          // 执行Run状态的行为
          RunStay();
          break;
        case PetState.Attack:
          // 执行Attack状态的行为
          AttackStay();
          break;
        case PetState.Die:
          // 执行Die状态的行为
          DieStay();
          break;
      }
    }

    protected virtual void FixedUpdate()
    {
      // 在FixedUpdate中处理物理相关的行为，例如移动、碰撞等
    }

    protected virtual void IdleEnter()
    {
      // 更新Idle状态进入时的行为，例如播放Idle动画、重置参数等
      animator.SetBool("isRun", false);
      animator.SetBool("isAttack", false);
      rb.velocity = Vector3.zero; // 停止移动
    }

    protected virtual void IdleStay()
    {
      // 更新Idle状态的行为，例如播放Idle动画、检测玩家距离等
    }
    protected virtual void IdleExit()
    {
      // 更新Idle状态退出时的行为，例如停止Idle动画、重置参数等
    }

    protected virtual void RunEnter()
    {
      // 更新Run状态进入时的行为，例如播放Run动画、设置移动参数等
      animator.SetBool("isRun", true);
      animator.SetBool("isAttack", false);
      //在x和z轴上移动到目标敌人位置
      if (targetEnemy != null)
      {
        Vector3 direction = (targetEnemy.transform.position - transform.position).normalized;
        direction.y = 0;
        rb.velocity = direction * petData.moveSpeed;
      }
    }
    protected virtual void RunStay()
    {
      // 更新Run状态的行为，例如播放Run动画、移动宠物等
      if (targetEnemy != null)
      {
        Vector3 direction = (targetEnemy.transform.position - transform.position).normalized;
        direction.y = 0;
        rb.velocity = direction * petData.moveSpeed;
      }
    }

    protected virtual void RunExit()
    {
      // 更新Run状态退出时的行为，例如停止Run动画、重置移动参数等
      animator.SetBool("isRun", false);
      rb.velocity = Vector3.zero;
    }

    protected virtual void AttackEnter()
    {
      // 更新Attack状态进入时的行为，例如播放Attack动画、设置攻击参数等
      animator.SetBool("isAttack", true);
    }
    protected virtual void AttackStay()
    {
      // 更新Attack状态的行为，例如播放Attack动画、攻击玩家等
    }
    protected virtual void AttackExit()
    {
      // 更新Attack状态退出时的行为，例如停止Attack动画、重置攻击参数等
      animator.SetBool("isAttack", false);
    }

    protected virtual void DieEnter()
    {
      // 更新Die状态进入时的行为，例如播放Die动画、死亡等
    }
    protected virtual void DieStay()
    {
      // 更新Die状态的行为，例如播放Die动画、死亡等
    }
    protected virtual void DieExit()
    {
      // 更新Die状态退出时的行为，例如停止Die动画、重置参数等
    }

    protected virtual void ChangeState(PetState newState)
    {
      switch (petData.currentState)
      {
        case PetState.Idle:
          IdleExit();
          break;
        case PetState.Run:
          RunExit();
          break;
        case PetState.Attack:
          AttackExit();
          break;
        case PetState.Die:
          DieExit();
          break;
      }
      petData.currentState = newState;
      switch (newState)
      {
        case PetState.Idle:
          IdleEnter();
          break;
        case PetState.Run:
          RunEnter();
          break;
        case PetState.Attack:
          AttackEnter();
          break;
        case PetState.Die:
          DieEnter();
          break;
      }
    }
    #endregion
    protected virtual void InitializePetAttributes()
    {
      // 初始化宠物属性
      animator = GetComponent<Animator>();
      rb = GetComponent<Rigidbody>();
      rb.useGravity = true; // 启用重力
    }
  }
}