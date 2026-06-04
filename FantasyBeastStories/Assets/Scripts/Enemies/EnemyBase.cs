using System.Collections;
using System.Collections.Generic;
using Charactors.Attribute;
using Events;
using Manager;
using Photon.Pun;
using UnityEngine;

namespace Enemies
{
    public class EnemyBase : MonoBehaviourPun
    {
        public enum EnemyState
        {
            Idle,
            Run,
            Attack,
            Die,
        }

        [SerializeField]
        protected Animator animator;

        [SerializeField]
        protected Rigidbody rb;

        [SerializeField]
        protected AttributeEnemyBase attribute;

        protected GameObject PlayerTarget;

        [SerializeField]
        protected EnemyState currentState;

        // 标记是否已初始化
        private bool isInitialized = false;

        // 标记是否已注册伤害事件，防止重复注册
        private bool isDamageEventRegistered = false;

        protected virtual void Awake()
        {
            attribute = new AttributeEnemyBase(30, 30, 50, 2);
        }

        protected virtual void Start()
        {
            InitializeEnemy();
        }

        // 初始化方法，从池中取出时也会调用
        protected virtual void InitializeEnemy()
        {
            if (!isInitialized)
            {
                // 首次初始化
                RegisterDamageEvent();
                isInitialized = true;
            }
            TransitionToState(EnemyState.Idle);
        }

        protected virtual void Update()
        {
            // 如果已死亡，不执行更新逻辑
            if (currentState == EnemyState.Die)
            {
                return;
            }

            // 只有拥有者（Master Client）执行AI逻辑和移动
            // 其他客户端通过Photon网络同步获取位置和动画
            // 测试模式下无Photon网络，直接执行
            if (!GameManager.isTest && !photonView.IsMine)
            {
                return;
            }

            switch (currentState)
            {
                case EnemyState.Idle:
                    UpdateIdle();
                    break;
                case EnemyState.Run:
                    UpdateRun();
                    break;
                case EnemyState.Attack:
                    UpdateAttack();
                    break;
                case EnemyState.Die:
                    UpdateDie();
                    break;
            }
        }

        //追踪最近的玩家
        protected virtual void TrackPlayer()
        {
            if (GetIsDie() || currentState == EnemyState.Die)
            {
                // 停止物理移动
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                return;
            }

            // 从 PlayerManager 获取缓存的玩家列表，避免每帧 FindGameObjectsWithTag
            IReadOnlyList<GameObject> players =
                PlayerManager.instance != null ? PlayerManager.instance.ActivePlayerObjects : null;

            if (players == null || players.Count == 0)
            {
                PlayerTarget = null;
                return;
            }

            PlayerTarget = players[0];
            for (int i = 1; i < players.Count; i++)
            {
                if (players[i] == null)
                    continue;
                if (
                    PlayerTarget == null
                    || Vector3.Distance(transform.position, players[i].transform.position)
                        < Vector3.Distance(transform.position, PlayerTarget.transform.position)
                )
                {
                    PlayerTarget = players[i];
                }
            }
        }

        #region 状态机相关代码
        protected virtual void TransitionToState(EnemyState newState)
        {
            // 退出当前状态
            switch (currentState)
            {
                case EnemyState.Idle:
                    ExitIdle();
                    break;
                case EnemyState.Run:
                    ExitRun();
                    break;
                case EnemyState.Attack:
                    ExitAttack();
                    break;
                case EnemyState.Die:
                    ExitDie();
                    break;
            }

            currentState = newState;

            // 进入新状态
            switch (currentState)
            {
                case EnemyState.Idle:
                    EnterIdle();
                    break;
                case EnemyState.Run:
                    EnterRun();
                    break;
                case EnemyState.Attack:
                    EnterAttack();
                    break;
                case EnemyState.Die:
                    EnterDie();
                    break;
            }
        }

        // ========== Idle状态 ==========
        protected virtual void EnterIdle()
        {
            TrackPlayer();
            if (animator != null)
            {
                animator.SetBool("isRun", false);
            }
        }

        protected virtual void UpdateIdle()
        {
            if (PlayerTarget)
            {
                TransitionToState(EnemyState.Run);
            }
            else
            {
                TrackPlayer();
                return;
            }
        }

        protected virtual void ExitIdle() { }

        // ========== Run状态 ==========
        protected virtual void EnterRun()
        {
            if (animator != null)
            {
                animator.SetBool("isRun", true);
            }
        }

        protected virtual void UpdateRun()
        {
            if (!PlayerTarget)
            {
                TransitionToState(EnemyState.Idle);
            }
            else
            {
                // 计算移动向量
                Vector3 moveDirection = (
                    PlayerTarget.transform.position - transform.position
                ).normalized;
                // 移动敌人
                if (rb != null)
                {
                    rb.MovePosition(
                        transform.position + moveDirection * attribute.moveSpeed * Time.deltaTime
                    );
                }
                // 旋转敌人朝向玩家,只在xz轴上旋转
                if (PlayerTarget != null)
                {
                    transform.LookAt(
                        new Vector3(
                            PlayerTarget.transform.position.x,
                            transform.position.y,
                            PlayerTarget.transform.position.z
                        )
                    );
                }
            }
        }

        protected virtual void ExitRun()
        {
            if (animator != null)
            {
                animator.SetBool("isRun", false);
            }
        }

        // ========== Attack状态 ==========
        protected virtual void EnterAttack() { }

        protected virtual void UpdateAttack() { }

        protected virtual void ExitAttack() { }

        // ========== Die状态 ==========
        protected virtual void EnterDie()
        {
            // 停止物理移动
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (animator != null)
            {
                animator.SetTrigger("die");
            }

            DropExperience();

            // 使用对象池回收而不是直接销毁
            Invoke(nameof(ReturnToPool), 3f);
        }

        protected virtual void UpdateDie() { }

        protected virtual void ExitDie() { }
        #endregion

        // 返回对象池（子类可重写以指定池名）
        protected virtual void ReturnToPool()
        {
            NetworkObjectPoolManager.instance.Despawn(GetPoolName(), gameObject);
        }

        // 获取对象池名称（子类重写以指定自己所属的池）
        protected virtual string GetPoolName()
        {
            return NetworkObjectPoolConst.Skeleton;
        }

        public virtual EnemyState GetCurrentState()
        {
            return currentState;
        }

        public virtual bool GetIsDie()
        {
            return attribute.GetIsDie();
        }

        protected virtual void OnEnable()
        {
            // 从对象池取出时重新初始化
            if (isInitialized)
            {
                InitializeEnemy();
            }
            RegisterDamageEvent();
        }

        protected virtual void OnDisable()
        {
            // 取消所有Invoke调用
            CancelInvoke();

            // 停止所有协程
            StopAllCoroutines();

            // 取消事件注册
            UnregisterDamageEvent();
        }

        protected virtual void RegisterDamageEvent()
        {
            if (isDamageEventRegistered)
                return; // 防止重复注册
            if (EventManager.instance)
            {
                EventManager.instance.RegisterEventComplex(
                    EventNames.DamageReceived,
                    OnDamageReceived
                );
                isDamageEventRegistered = true;
            }
            else
            {
                //创建一个新的EventManager实例并注册事件
                GameObject eventManagerObj = new GameObject("EventManager");
                EventManager eventManager = eventManagerObj.AddComponent<EventManager>();
                eventManager.RegisterEventComplex(EventNames.DamageReceived, OnDamageReceived);
                EventManager.instance = eventManager;
                isDamageEventRegistered = true;
            }
        }

        protected virtual void UnregisterDamageEvent()
        {
            if (!isDamageEventRegistered)
                return; // 未注册则不需要注销
            if (EventManager.instance)
            {
                EventManager.instance.UnRegisterEventComplex(
                    EventNames.DamageReceived,
                    OnDamageReceived
                );
                isDamageEventRegistered = false;
            }
        }

        protected virtual void OnDamageReceived(EventArgsBase args)
        {
            if (attribute.GetIsDie())
            {
                return;
            }
            DamageEventArgs damageEventArgs = args as DamageEventArgs;
            if (damageEventArgs == null)
            {
                Debug.LogWarning($"期望 DamageEventArgs，但收到 {args?.GetType()}");
                return;
            }
            if (damageEventArgs.GetDamgeTarget() != gameObject)
            {
                return;
            }
            TakeDamage(damageEventArgs);
        }

        public virtual void TakeDamage(DamageEventArgs damageEventArgs)
        {
            if (attribute.GetIsDie())
            {
                return;
            }
            damageEventArgs.CalculateFinalDamageValue();
            damageEventArgs.finalDamageValue = Mathf.Ceil(damageEventArgs.finalDamageValue);
            attribute.TakeDamage(damageEventArgs.finalDamageValue);
            Debug.Log(
                "最终伤害为："
                    + damageEventArgs.finalDamageValue
                    + "是否死亡:"
                    + attribute.GetIsDie()
            );
            if (GameManager.isTest)
            {
                ObjectPoolManager
                    .instance.GetFromPoolAndActivate(
                        ObjectPoolConst.DamageNumPool,
                        transform.position
                    )
                    .GetComponent<DamageNum>()
                    .Play(
                        damageEventArgs.finalDamageValue,
                        transform.position,
                        damageEventArgs.isCritical
                    );
            }
            attribute.TakeDamageSpecial(damageEventArgs.damageType);
            if (attribute.GetIsDie())
            {
                TransitionToState(EnemyState.Die);
            }
        }

        //死亡后掉落经验
        protected virtual void DropExperience()
        {
            Debug.Log("敌人死亡，掉落经验");
        }

        // 重置状态（对象池回收时调用）
        public virtual void ResetState()
        {
            // 1. 取消所有Invoke和协程
            CancelInvoke();
            StopAllCoroutines();

            // 2. 重置状态机
            if (currentState == EnemyState.Die)
            {
                CancelInvoke(nameof(ReturnToPool));
            }
            currentState = EnemyState.Idle;

            // 3. 重置动画
            if (animator != null)
            {
                animator.ResetTrigger("die");
                animator.SetBool("isRun", false);
                animator.Rebind(); // 重置所有动画状态
                animator.Update(0f); // 立即更新动画
            }

            // 4. 重置物理
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 5. 重置属性
            if (attribute != null)
            {
                attribute.ResetAttribute();
            }

            // 6. 清除目标
            PlayerTarget = null;

            // 7. 重新注册事件（因为OnDisable中注销了）
            RegisterDamageEvent();
        }
    }
}
