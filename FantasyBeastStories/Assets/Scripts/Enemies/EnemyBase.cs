using System.Collections;
using System.Collections.Generic;
using Charactors.Attribute;
using UnityEngine;
namespace Enemies
{
    public class EnemyBase : MonoBehaviour
    {
        public enum EnemyState
        {
            Idle,
            Run,
            Attack,
            Die
        }
        [SerializeField] protected Animator animator;
        [SerializeField] protected Rigidbody rb;
        [SerializeField] protected AttributeEnemyBase attribute;

        protected GameObject PlayerTarget;
        protected EnemyState currentState;

        void Awake()
        {
            attribute = GetComponent<AttributeEnemyBase>();
        }


        protected virtual void Start()
        {
            TransitionToState(EnemyState.Idle);
        }

        protected virtual void Update()
        {
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
            if (GetIsDie())
            {
                return;
            }
            List<GameObject> players = new List<GameObject>();
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                players.Add(player);
            }
            if (players.Count > 0)
            {
                PlayerTarget = players[0];
                for (int i = 1; i < players.Count; i++)
                {
                    if (Vector3.Distance(transform.position, players[i].transform.position) <
                        Vector3.Distance(transform.position, PlayerTarget.transform.position))
                    {
                        PlayerTarget = players[i];
                    }
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
            animator.SetBool("isRun", false);
        }
        protected virtual void UpdateIdle()
        {
            if (PlayerTarget)
            {
                TransitionToState(EnemyState.Run);
            }
        }
        protected virtual void ExitIdle() { }

        // ========== Run状态 ==========
        protected virtual void EnterRun()
        {
            animator.SetBool("isRun", true);
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
                Vector3 moveDirection = (PlayerTarget.transform.position - transform.position).normalized;
                // 移动敌人
                rb.MovePosition(transform.position + moveDirection * attribute.moveSpeed * Time.deltaTime);
                // 旋转敌人朝向玩家
                transform.LookAt(PlayerTarget.transform);
            }
        }
        protected virtual void ExitRun()
        {
            animator.SetBool("isRun", false);
        }

        // ========== Attack状态 ==========
        protected virtual void EnterAttack() { }
        protected virtual void UpdateAttack() { }
        protected virtual void ExitAttack() { }

        // ========== Die状态 ==========
        protected virtual void EnterDie()
        {
            attribute.SetIsDie(true);
            attribute.SetMoveSpeed(0);
            animator.SetTrigger("die");
            Invoke(nameof(DestorySelf), 3f); // 3秒后销毁敌人对象
        }
        protected virtual void UpdateDie() { }
        protected virtual void ExitDie() { }
        #endregion

        protected virtual void DestorySelf()
        {
            Destroy(gameObject);
        }
        public virtual void TakeDamage(float damage)
        {
            attribute.TakeDamage(damage);
            if (attribute.currentHealth <= 0)
            {
                TransitionToState(EnemyState.Die);
            }
        }

        public virtual EnemyState GetCurrentState()
        {
            return currentState;
        }

        public virtual bool GetIsDie()
        {
            return attribute.GetIsDie();
        }
    }
}
