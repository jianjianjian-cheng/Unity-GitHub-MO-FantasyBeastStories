using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class Protagonist : MonoBehaviour
    {
        [Header("角色变量设置")]
        private bool isRun;
        private bool isRight;
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 5f; // 移动速度

        private Rigidbody rb; // 物理组件
        private Animator animator;// 动画组件
        private SpriteRenderer spriteRenderer; // 渲染组件
        private Vector3 movement; // 移动方向

        // Start is called before the first frame update
        void Start()
        {
            // 获取或添加Rigidbody组件
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.useGravity = true; // 启用重力
        }

        // Update is called once per frame
        void Update()
        {
            // 获取输入
            HandleInput();
        }

        void FixedUpdate()
        {
            // 物理移动
            MoveCharacter();
        }

        /// <summary>
        /// 处理玩家输入
        /// </summary>
        private void HandleInput()
        {
            // 获取水平输入（A/D或左右箭头）
            float horizontal = Input.GetAxis("Horizontal");
            // 获取垂直输入（W/S或上下箭头）
            float vertical = Input.GetAxis("Vertical");
            // 计算移动方向（基于世界坐标）
            movement = new Vector3(horizontal, 0f, vertical).normalized;
            if (horizontal > 0)
            {
                isRight = true;
            }
            else if (horizontal < 0)
            {
                isRight = false;
            }
            // 处理角色方向
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !isRight;
            }
        }

        /// <summary>
        /// 移动角色
        /// </summary>
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
        }
    }
}