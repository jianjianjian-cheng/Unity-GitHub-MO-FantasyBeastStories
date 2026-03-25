using System.Collections;
using System.Collections.Generic;
using Atttibute;
using Manager;
using Photon.Pun;
using UnityEngine;

namespace Charactors
{
    public class PlayerController : MonoBehaviourPun
    {
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 2f; // 移动速度
        [SerializeField] private Rigidbody rb; // 物理组件
        [SerializeField] private Animator animator;// 动画组件

        [Header("旋转设置")]
        [SerializeField] private float rotationSpeed = 10f; // 旋转速度
        private AttributePlayerBase attributePlayerBase; // 玩家属性组件
        private Vector3 movement; // 移动方向
        private bool isRun; // 是否正在运行


        void Awake()
        {
            attributePlayerBase = new AttributePlayerBase(35, 10, 100, 3.5f, 1f, 0.2f);
        }
        void Start()
        {
            if (!photonView.IsMine)
            {
                return; // 只处理本地玩家的输入和动画
            }
            moveSpeed = attributePlayerBase.GetMoveSpeed();
            // 获取或添加Rigidbody组件
            if (rb == null)
            {
                rb = gameObject.GetComponent<Rigidbody>();
            }
            rb.useGravity = true; // 启用重力
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!photonView.IsMine)
            {
                return; // 只处理本地玩家的输入和动画
            }
            HandleInput();
        }

        void FixedUpdate()
        {
            if (!photonView.IsMine)
            {
                return; // 只处理本地玩家的输入和动画
            }
            // 物理移动
            MoveCharacter();
        }

        protected virtual void OnEnable()
        {
            EventManager.instance.RegisterAttributePlayerBase(EventNames.UpdateAttributeWizradBoy, attributePlayerBase);
        }

        protected virtual void OnDisable()
        {
            EventManager.instance.UnRegisterAttributePlayerBase(EventNames.UpdateAttributeWizradBoy);
        }

        private void HandleInput()
        {
            // 获取水平输入（A/D或左右箭头）
            float horizontal = Input.GetAxis("Horizontal");
            // 获取垂直输入（W/S或上下箭头）
            float vertical = Input.GetAxis("Vertical");
            // 计算移动方向（基于世界坐标）
            movement = new Vector3(horizontal, 0f, vertical).normalized;
        }

        private void MoveCharacter()
        {
            // 计算移动向量
            Vector3 moveVelocity = movement * moveSpeed;
            isRun = movement != Vector3.zero;
            animator.SetBool("isRun", isRun);
            // 保持Y轴速度不变（重力影响）
            moveVelocity.y = rb.velocity.y;

            // 应用速度
            rb.velocity = moveVelocity;

            // ===== 新增：角色朝向逻辑 =====
            // 只有在移动时才改变朝向
            if (movement != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                // 如果模型“面朝 +X”，要再转 90 度
                targetRotation *= Quaternion.Euler(0f, 0f, 0f);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }
}