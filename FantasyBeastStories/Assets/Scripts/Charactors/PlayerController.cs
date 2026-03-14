using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Charactors
{
    public class PlayerController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 2f; // 移动速度
        [SerializeField] private Rigidbody rb; // 物理组件
        [SerializeField] private Animator animator;// 动画组件

        [Header("旋转设置")]
        [SerializeField] private float rotationSpeed = 10f; // 旋转速度

        private Vector3 movement; // 移动方向
        private bool isRun; // 是否正在运行


        // Start is called before the first frame update
        void Start()
        {
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
            HandleInput();
        }

        void FixedUpdate()
        {
            // 物理移动
            MoveCharacter();
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
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }
}