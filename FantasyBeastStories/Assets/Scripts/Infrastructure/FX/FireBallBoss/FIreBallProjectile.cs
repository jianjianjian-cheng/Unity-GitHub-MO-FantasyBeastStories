using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Domain.Manager;
using UnityEngine;

namespace Infrastructure.FX.FireBallBoss
{
    public class FIreBallProjectile : MonoBehaviour
    {
        [Header("运动相关参数")]
        [SerializeField]
        private float moveSpeed = 10f;
        [SerializeField]
        private float minMoveSpeed = 5f;           // 最小速度
        [SerializeField]
        private float maxMoveSpeed = 30f;          // 最大速度
        [SerializeField]
        private float steerStrength = 5f;
        [SerializeField]
        private float maxLifeTime = 5f;

        [Header("抛物线参数")]
        [SerializeField]
        private float arcHeight = 3f;              // 抛物线最高点高度
        [SerializeField]
        private bool useParabola = true;          // 是否使用抛物线模式
        [SerializeField]
        private bool adjustSpeedToTarget = true;  // 是否根据目标距离调整速度

        [Header("命中检测")]
        [SerializeField]
        private Transform target;
        [SerializeField]
        private float hitRadius = 0.5f;           // 判定命中距离
        [SerializeField]
        private GameObject hitEffect;             // 命中特效预制体
        [SerializeField]
        private float damage = 30f;               // 伤害
        [SerializeField]
        private LayerMask collisionMask;          // 碰撞检测层

        [Header("轨迹绘制")]
        [SerializeField]
        private bool showTrajectory = true;       // 是否显示轨迹
        [SerializeField]
        private Color trajectoryColor = new Color(1f, 0.5f, 0f, 0.8f); // 轨迹颜色
        [SerializeField]
        private float trajectoryDuration = 1f;    // 轨迹显示持续时间
        [SerializeField]
        private int trajectoryResolution = 30;    // 轨迹精度（点数）
        [SerializeField]
        private Material trajectoryMaterial;      // 轨迹材质

        private Rigidbody rb;
        private Vector3 startPosition;
        private Vector3 targetPositionAtLaunch;
        private float launchTime;
        private bool hasLaunched = false;
        private bool hasHit = false;
        private float actualMoveSpeed;            // 实际使用的移动速度

        // 轨迹相关
        private LineRenderer trajectoryLine;
        private Vector3[] trajectoryPoints;
        private float trajectoryDisplayTimer;

        void Start()
        {
            Invoke(nameof(DestroySelf), maxLifeTime);
            rb = GetComponent<Rigidbody>();

            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.useGravity = true;

            // 初始化轨迹绘制组件
            InitializeTrajectoryRenderer();

            // 如果已经有目标，立即发射
            if (target != null)
            {
                InitializeLaunch();
            }
        }

        private void InitializeTrajectoryRenderer()
        {
            // 创建LineRenderer组件
            trajectoryLine = gameObject.AddComponent<LineRenderer>();
            trajectoryLine.startWidth = 0.1f;
            trajectoryLine.endWidth = 0.05f;
            trajectoryLine.material = trajectoryMaterial != null ? trajectoryMaterial : new Material(Shader.Find("Sprites/Default"));
            trajectoryLine.startColor = trajectoryColor;
            trajectoryLine.endColor = new Color(trajectoryColor.r, trajectoryColor.g, trajectoryColor.b, 0f);
            trajectoryLine.positionCount = 0;
            trajectoryLine.enabled = false;
        }

        public void SetTargetAndDamage(Transform newTarget, float newDamage)
        {
            target = newTarget;
            damage = newDamage;

            // 重新初始化发射参数
            if (!hasLaunched)
            {
                InitializeLaunch();
            }
        }

        private void InitializeLaunch()
        {
            startPosition = transform.position;
            targetPositionAtLaunch = GetTargetPosition();
            launchTime = Time.time;

            if (useParabola)
            {
                // 根据距离计算实际速度
                CalculateActualSpeed();
                LaunchProjectile();
                // 发射时绘制完整轨迹
                DrawTrajectory();
            }

            hasLaunched = true;
        }

        /// <summary>
        /// 根据目标距离计算实际需要的速度
        /// </summary>
        private void CalculateActualSpeed()
        {
            Vector3 displacement = targetPositionAtLaunch - startPosition;
            Vector3 displacementXZ = new Vector3(displacement.x, 0, displacement.z);
            float distanceXZ = displacementXZ.magnitude;

            if (adjustSpeedToTarget)
            {
                //确保两秒内到达目标
                float optimalTime = distanceXZ / 2f;
                actualMoveSpeed = distanceXZ / optimalTime;

                // 限制速度范围
                actualMoveSpeed = Mathf.Clamp(actualMoveSpeed, minMoveSpeed, maxMoveSpeed);
            }
            else
            {
                actualMoveSpeed = moveSpeed;
            }
        }

        private Vector3 GetTargetPosition()
        {
            if (target == null)
            {
                // 如果没有目标，向前方发射
                return transform.position + transform.forward * 15f;
            }

            // 获取目标的实际位置（考虑碰撞体中心）
            Collider targetCollider = target.GetComponent<Collider>();
            if (targetCollider != null)
            {
                return targetCollider.bounds.center;
            }

            return target.position;
        }

        private void LaunchProjectile()
        {
            if (rb == null) return;

            // 计算水平面距离
            Vector3 displacement = targetPositionAtLaunch - startPosition;
            Vector3 displacementXZ = new Vector3(displacement.x, 0, displacement.z);
            float distanceXZ = displacementXZ.magnitude;
            float heightDifference = displacement.y;

            // 计算飞行时间（基于调整后的水平速度和距离）
            float timeToTarget = distanceXZ / actualMoveSpeed;

            // 确保有最小飞行时间
            timeToTarget = Mathf.Max(timeToTarget, 0.5f);

            // 计算需要的垂直速度
            float verticalVelocity;

            if (arcHeight > 0)
            {
                // 抛物线模式：计算能达到指定高度的垂直速度
                float baseVerticalVelocity = heightDifference / timeToTarget;
                float gravityMagnitude = Mathf.Abs(Physics.gravity.y);
                float extraVerticalVelocity = Mathf.Sqrt(2 * gravityMagnitude * arcHeight);
                verticalVelocity = baseVerticalVelocity + extraVerticalVelocity;
            }
            else
            {
                // 无抛物线模式：直接指向目标
                verticalVelocity = heightDifference / timeToTarget;
            }

            // 限制垂直速度，防止抛物线过高
            float maxVerticalVelocity = actualMoveSpeed * 2f;
            verticalVelocity = Mathf.Clamp(verticalVelocity, -maxVerticalVelocity, maxVerticalVelocity);

            // 计算水平速度
            Vector3 horizontalVelocity = displacementXZ.normalized * actualMoveSpeed;

            // 组合最终速度
            Vector3 launchVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

            // 确保初始速度不超过最大值
            if (launchVelocity.magnitude > actualMoveSpeed * 1.5f)
            {
                launchVelocity = launchVelocity.normalized * actualMoveSpeed * 1.5f;
            }

            // 设置初始速度
            rb.velocity = launchVelocity;

            Debug.Log($"发射火球 - 目标位置: {targetPositionAtLaunch}, 初速度: {launchVelocity}, " +
                     $"飞行时间估计: {timeToTarget:F2}s, 水平距离: {distanceXZ:F2}m, 实际速度: {actualMoveSpeed:F2}");
        }

        /// <summary>
        /// 绘制完整的抛物线轨迹
        /// </summary>
        private void DrawTrajectory()
        {
            if (!showTrajectory || trajectoryLine == null) return;

            // 计算发射时的初始速度
            Vector3 displacement = targetPositionAtLaunch - startPosition;
            Vector3 displacementXZ = new Vector3(displacement.x, 0, displacement.z);
            float distanceXZ = displacementXZ.magnitude;
            float heightDifference = displacement.y;

            // 使用实际速度计算飞行时间
            float timeToTarget = distanceXZ / actualMoveSpeed;
            timeToTarget = Mathf.Max(timeToTarget, 0.5f);

            // 重新计算初始速度（与LaunchProjectile中相同的计算）
            float verticalVelocity;
            if (arcHeight > 0)
            {
                float baseVerticalVelocity = heightDifference / timeToTarget;
                float gravityMagnitude = Mathf.Abs(Physics.gravity.y);
                float extraVerticalVelocity = Mathf.Sqrt(2 * gravityMagnitude * arcHeight);
                verticalVelocity = baseVerticalVelocity + extraVerticalVelocity;
            }
            else
            {
                verticalVelocity = heightDifference / timeToTarget;
            }

            float maxVerticalVelocity = actualMoveSpeed * 2f;
            verticalVelocity = Mathf.Clamp(verticalVelocity, -maxVerticalVelocity, maxVerticalVelocity);

            Vector3 horizontalVelocity = displacementXZ.normalized * actualMoveSpeed;
            Vector3 initialVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

            if (initialVelocity.magnitude > actualMoveSpeed * 1.5f)
            {
                initialVelocity = initialVelocity.normalized * actualMoveSpeed * 1.5f;
            }

            // 计算轨迹点
            trajectoryPoints = new Vector3[trajectoryResolution + 1];
            float timeStep = timeToTarget / trajectoryResolution;

            for (int i = 0; i <= trajectoryResolution; i++)
            {
                float t = i * timeStep;
                // 使用物理公式：位置 = 初始位置 + 初始速度*t + 0.5*重力*t²
                Vector3 point = startPosition + initialVelocity * t + 0.5f * Physics.gravity * t * t;
                trajectoryPoints[i] = point;
            }

            // 设置LineRenderer
            trajectoryLine.positionCount = trajectoryPoints.Length;
            trajectoryLine.SetPositions(trajectoryPoints);
            trajectoryLine.enabled = true;

            // 设置轨迹显示的计时器
            trajectoryDisplayTimer = trajectoryDuration;

            // 启动协程来控制轨迹的消失
            StartCoroutine(FadeTrajectory());
        }

        /// <summary>
        /// 轨迹渐隐效果
        /// </summary>
        private IEnumerator FadeTrajectory()
        {
            float elapsedTime = 0f;

            // 渐变颜色
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

            while (elapsedTime < trajectoryDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / trajectoryDuration);

                // 更新颜色和透明度
                colorKeys[0] = new GradientColorKey(trajectoryColor, 0f);
                colorKeys[1] = new GradientColorKey(trajectoryColor, 1f);
                alphaKeys[0] = new GradientAlphaKey(alpha, 0f);
                alphaKeys[1] = new GradientAlphaKey(alpha * 0.5f, 1f);

                gradient.SetKeys(colorKeys, alphaKeys);
                trajectoryLine.colorGradient = gradient;

                yield return null;
            }

            // 完全隐藏轨迹
            trajectoryLine.enabled = false;
            trajectoryLine.positionCount = 0;
        }

        /// <summary>
        /// 在运行时更新部分轨迹（仅显示已飞过的路径）
        /// </summary>
        private void UpdatePartialTrajectory()
        {
            if (!showTrajectory || trajectoryPoints == null || trajectoryPoints.Length == 0) return;

            float elapsedTime = Time.time - launchTime;
            Vector3 displacement = targetPositionAtLaunch - startPosition;
            float distanceXZ = new Vector3(displacement.x, 0, displacement.z).magnitude;
            float timeToTarget = Mathf.Max(distanceXZ / actualMoveSpeed, 0.5f);

            // 计算已经过的轨迹点数量
            int passedPoints = Mathf.FloorToInt((elapsedTime / timeToTarget) * trajectoryResolution);
            passedPoints = Mathf.Clamp(passedPoints, 0, trajectoryResolution);

            // 显示从当前火球位置到目标位置的剩余轨迹
            List<Vector3> remainingPoints = new List<Vector3>();
            remainingPoints.Add(transform.position); // 当前火球位置

            // 添加剩余的轨迹点
            for (int i = passedPoints; i < trajectoryPoints.Length; i++)
            {
                remainingPoints.Add(trajectoryPoints[i]);
            }

            trajectoryLine.positionCount = remainingPoints.Count;
            trajectoryLine.SetPositions(remainingPoints.ToArray());
            trajectoryLine.enabled = remainingPoints.Count > 1;
        }

        void Update()
        {
            // 更新轨迹显示计时器
            if (hasLaunched && !hasHit && trajectoryLine.enabled)
            {
                UpdatePartialTrajectory();
            }
        }

        void FixedUpdate()
        {
            if (hasHit) return;

            if (useParabola)
            {
                // 抛物线模式：更新朝向并检测命中
                UpdateRotation();
                CheckHitCondition();
            }
            else
            {
                // 追踪模式：持续追踪目标
                UpdateTrackingMovement();
            }

            // 通用检测：是否已经明显偏离目标
            CheckIfOutOfRange();
        }

        private void UpdateTrackingMovement()
        {
            if (target == null) return;

            // 计算朝向目标的方向
            Vector3 directionToTarget = (GetTargetPosition() - transform.position).normalized;

            // 计算所需的速度向量（使用实际速度）
            Vector3 desiredVelocity = directionToTarget * actualMoveSpeed;

            // 计算转向力
            Vector3 steerForce = desiredVelocity - rb.velocity;
            steerForce = Vector3.ClampMagnitude(steerForce, steerStrength);

            // 施加力
            rb.AddForce(steerForce, ForceMode.Acceleration);

            // 限制最大速度
            if (rb.velocity.magnitude > actualMoveSpeed)
            {
                rb.velocity = rb.velocity.normalized * actualMoveSpeed;
            }

            UpdateRotation();
            CheckHitCondition();
        }

        private void UpdateRotation()
        {
            // 让火球始终面向飞行方向
            if (rb.velocity.magnitude > 0.1f)
            {
                transform.forward = rb.velocity.normalized;
            }
        }

        private void CheckHitCondition()
        {
            if (target == null) return;

            // 检查是否到达目标附近
            float distanceToTarget = Vector3.Distance(transform.position, GetTargetPosition());

            if (distanceToTarget < hitRadius)
            {
                OnHitTarget();
            }
        }

        private void CheckIfOutOfRange()
        {
            // 如果火球飞得太远，自动销毁
            float maxRange = Mathf.Max(50f, Vector3.Distance(startPosition, targetPositionAtLaunch) * 2f);
            if (Vector3.Distance(transform.position, startPosition) > maxRange)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // 检查是否命中玩家或地面
            if (other.CompareTag("Player") || other.CompareTag("Ground"))
            {
                OnHitTarget(other.gameObject);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;

            // 碰撞检测也触发命中
            if (collision.gameObject.CompareTag("Player") ||
                collision.gameObject.CompareTag("Ground") ||
                collision.gameObject.CompareTag("Obstacle"))
            {
                OnHitTarget(collision.gameObject);
            }
        }

        private void OnHitTarget(GameObject hitObject = null)
        {
            if (hasHit) return;
            hasHit = true;

            // 隐藏轨迹
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = false;
            }

            // 生成命中特效
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // 造成伤害
            if (hitObject != null && hitObject.CompareTag("Player"))
            {
                Debug.Log($"火球命中玩家，造成 {damage} 点伤害");
                DamageEventArgs damageEventArgs = new DamageEventArgs(
                Element.Common,
                gameObject,
                hitObject,
                damage,
                false,
                1f
            );

                EventChannelLocator.MainContainer.playerDamageEventChannel.Raise(damageEventArgs);

            }

            Destroy(gameObject);
        }

        void DestroySelf()
        {
            if (!hasHit)
            {
                Debug.Log("火球超时销毁");
            }
            Destroy(gameObject);
        }

        // 编辑器可视化
        void OnDrawGizmos()
        {
            // 在非运行模式下也绘制轨迹预览
            if (!Application.isPlaying && target != null && useParabola)
            {
                DrawEditorTrajectoryPreview();
            }
            // // 运行时绘制实际轨迹
            // else if (Application.isPlaying && showTrajectory && trajectoryPoints != null && trajectoryPoints.Length > 0)
            // {
            //     DrawRuntimeTrajectory();
            // }

            // // 绘制命中范围
            // if (hasLaunched && !hasHit)
            // {
            //     Gizmos.color = Color.red;
            //     Gizmos.DrawWireSphere(transform.position, hitRadius);
            // }
        }

        /// <summary>
        /// 编辑器模式下的轨迹预览
        /// </summary>
        private void DrawEditorTrajectoryPreview()
        {
            Vector3 previewStart = transform.position;
            Vector3 previewTarget = GetTargetPosition();

            // 计算轨迹点
            Vector3 displacement = previewTarget - previewStart;
            Vector3 displacementXZ = new Vector3(displacement.x, 0, displacement.z);
            float distanceXZ = displacementXZ.magnitude;
            float heightDifference = displacement.y;

            // 使用基础速度计算（编辑器中不考虑动态调整）
            float optimalTime = Mathf.Clamp(distanceXZ / moveSpeed, 0.5f, 3f);
            float previewSpeed = distanceXZ / optimalTime;
            previewSpeed = Mathf.Clamp(previewSpeed, minMoveSpeed, maxMoveSpeed);

            // 计算垂直速度
            float verticalVelocity;
            if (arcHeight > 0)
            {
                float baseVerticalVelocity = heightDifference / optimalTime;
                float gravityMagnitude = Mathf.Abs(Physics.gravity.y);
                float extraVerticalVelocity = Mathf.Sqrt(2 * gravityMagnitude * arcHeight);
                verticalVelocity = baseVerticalVelocity + extraVerticalVelocity;
            }
            else
            {
                verticalVelocity = heightDifference / optimalTime;
            }

            Vector3 horizontalVelocity = displacementXZ.normalized * previewSpeed;
            Vector3 initialVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

            // 计算并绘制轨迹点
            Vector3[] previewPoints = new Vector3[trajectoryResolution + 1];
            float timeStep = optimalTime / trajectoryResolution;

            for (int i = 0; i <= trajectoryResolution; i++)
            {
                float t = i * timeStep;
                Vector3 point = previewStart + initialVelocity * t + 0.5f * Physics.gravity * t * t;
                previewPoints[i] = point;
            }

            // 绘制轨迹线
            Gizmos.color = trajectoryColor;
            for (int i = 0; i < previewPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(previewPoints[i], previewPoints[i + 1]);
            }

            // 绘制轨迹点
            Gizmos.color = Color.yellow;
            for (int i = 0; i < previewPoints.Length; i += 3) // 每3个点绘制一个，避免太密集
            {
                float alpha = 1f - (float)i / previewPoints.Length;
                Gizmos.color = new Color(1f, 1f, 0f, alpha * 0.8f);
                Gizmos.DrawWireSphere(previewPoints[i], 0.1f);
            }

            // 绘制目标位置
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(previewTarget, hitRadius);

            // 绘制连接线
            Gizmos.color = Color.green;
            Gizmos.DrawLine(previewStart, previewTarget);
        }

        /// <summary>
        /// 运行时轨迹绘制
        /// </summary>
        private void DrawRuntimeTrajectory()
        {
            Gizmos.color = trajectoryColor;
            for (int i = 0; i < trajectoryPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(trajectoryPoints[i], trajectoryPoints[i + 1]);
            }

            // 绘制轨迹点
            Gizmos.color = Color.yellow;
            foreach (Vector3 point in trajectoryPoints)
            {
                Gizmos.DrawWireSphere(point, 0.1f);
            }
        }
    }

    // 可选的伤害接口
    public interface IDamageable
    {
        void TakeDamage(float damage);
    }
}
