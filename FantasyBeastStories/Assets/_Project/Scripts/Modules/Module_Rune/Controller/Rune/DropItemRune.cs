using System.Collections;
using Controllers.Experience;
using UnityEngine;

namespace Controllers.Rune
{
    /// <summary>
    /// 符文掉落物 — 从敌人身上掉落，以抛物线轨迹飞出去然后落地
    /// 玩家靠近拾取后，符文效果即时应用到玩家属性
    /// </summary>
    public class DropItemRune : DropItemBase
    {
        [Header("符文数据")]
        [SerializeField] private RuneDataSO runeData;

        [Header("抛物线参数")]
        [SerializeField] private float launchAngleMin = 50f;       // 最小发射仰角（度）
        [SerializeField] private float launchAngleMax = 70f;       // 最大发射仰角（度）
        [SerializeField] private float launchSpeed = 6f;           // 发射初速度
        [SerializeField] private float launchRandomRadius = 3f;    // 落点随机散布半径
        [SerializeField] private float spinTorque = 3f;            // 飞行中的自旋扭矩

        [Header("落地检测")]
        [SerializeField] private float landVelocityThreshold = 0.2f; // 判定落地的速度阈值
        [SerializeField] private float groundCheckDistance = 2f;     // 向下检测地面的距离
        [SerializeField] private LayerMask groundLayer = -1;         // 地面层（-1 = Everything）
        [SerializeField] private float groundYOffset = 0.05f;        // 落地后离地高度

        [Header("生命周期")]
        [SerializeField] private float maxLifetime = 30f;           // 无人拾取时自动消失的时间


        [Header("子类特效")]
        [SerializeField] private GameObject trail;
        [SerializeField] private GameObject realyFx;
        // ── 运行时状态 ──
        private bool hasLanded = false;
        private float launchTime;               // 发射时刻（防 OnCollisionEnter 误触）
        private Coroutine landCheckCoroutine;
        private Coroutine lifetimeCoroutine;

        // ======================================================================
        //  Setup — 由敌人掉落时调用，传入掉落的符文数据
        // ======================================================================

        /// <summary>设置该掉落物对应的符文数据</summary>
        public void Setup(RuneDataSO data)
        {
            runeData = data;
        }

        /// <summary>获取该掉落物携带的符文数据</summary>
        public RuneDataSO GetRuneData() => runeData;

        // ======================================================================
        //  生命周期（继承自 DropItemBase）
        // ======================================================================

        /// <summary>
        /// GameObject 被激活时调用：
        /// 基类 OnEnable() 会重置飞行状态并调用 ApplyExplosionEffect()
        /// 我们 override ApplyExplosionEffect() 来实现抛物线
        /// </summary>
        protected override void OnEnable()
        {
            hasLanded = false;

            // 清除之前的协程
            if (landCheckCoroutine != null)
            {
                StopCoroutine(landCheckCoroutine);
                landCheckCoroutine = null;
            }
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (landCheckCoroutine != null)
            {
                StopCoroutine(landCheckCoroutine);
                landCheckCoroutine = null;
            }
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }
        }

        // ======================================================================
        //  抛物线弹射（override 基类的简单爆炸）
        // ======================================================================

        /// <summary>
        /// 重写基类的 ApplyExplosionEffect：
        /// 以高抛角度将符文弹射出去，形成抛物线轨迹
        /// </summary>
        protected override void ApplyExplosionEffect()
        {
            if (Rb == null)
                return;

            // 1. 重置物理状态
            Rb.velocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            Rb.isKinematic = false;
            Rb.useGravity = true;

            // 记录发射时刻，防止 OnCollisionEnter 误触
            launchTime = UnityEngine.Time.time;

            // 2. 随机偏移落点方向（在敌人周围散布）
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            Vector3 horizontalDir = new Vector3(randomCircle.x, 0, randomCircle.y);

            // 3. 计算发射速度
            //    已知水平距离 distance，仰角 angle，初速度 launchSpeed
            //    水平分量: v_x = launchSpeed * cos(angle)
            //    垂直分量: v_y = launchSpeed * sin(angle)
            float angle = Random.Range(launchAngleMin, launchAngleMax);
            float angleRad = angle * Mathf.Deg2Rad;

            Vector3 launchVelocity = horizontalDir * launchSpeed * Mathf.Cos(angleRad)
                                   + Vector3.up * launchSpeed * Mathf.Sin(angleRad);

            // 4. 施加瞬时速度
            Rb.AddForce(launchVelocity, ForceMode.VelocityChange);

            // 5. 添加旋转（让符文在空中翻转）
            Rb.AddTorque(new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * spinTorque, ForceMode.VelocityChange);

            // 5.5 增加阻力，防止落地后滑行
            Rb.drag = 2f;
            Rb.angularDrag = 2f;

            // 6. 启动落地检测协程
            landCheckCoroutine = StartCoroutine(CheckLanding());

            // 7. 启动生命周期计时（超时自动消失）
            lifetimeCoroutine = StartCoroutine(AutoDespawnAfterDelay());
        }

        // ======================================================================
        //  落地检测
        // ======================================================================

        private IEnumerator CheckLanding()
        {
            // 给一个最小飞行时间，避免刚发射就被判定落地
            float minFlightTime = 0.4f;
            float timer = 0f;

            while (!hasLanded)
            {
                timer += UnityEngine.Time.deltaTime;

                // 条件 A：至少飞了 minFlightTime
                // 条件 B：速度很低
                // 条件 C：在地面附近（Raycast 检测）
                if (timer > minFlightTime && Rb.velocity.magnitude < landVelocityThreshold)
                {
                    // 向下发射射线检测地面
                    if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                            groundCheckDistance, groundLayer))
                    {
                        // 停在落地点正上方
                        transform.position = new Vector3(
                            transform.position.x,
                            hit.point.y + groundYOffset,
                            transform.position.z
                        );

                        OnLand();
                        yield break;
                    }
                    else
                    {
                        // 速度很低但不在"地面"上 — 可能卡在半空
                        // 再等一小段时间强制落地
                        yield return new WaitForSeconds(0.5f);

                        if (!hasLanded)
                        {
                            // 强制落地：使用当前位置的 Y 作为地面
                            OnLand();
                            yield break;
                        }
                    }
                }

                yield return null; // 每帧检测
            }
        }

        private void OnLand()
        {
            hasLanded = true;
            Debug.Log($"[DropItemRune] 符文落地: {runeData?.runeName ?? "未知符文"}");

            // 冻结物理，符文稳稳停住
            if (Rb != null)
            {
                Rb.velocity = Vector3.zero;
                Rb.angularVelocity = Vector3.zero;
                Rb.isKinematic = true;
                Rb.useGravity = false;
            }

            // 将 transform 摆正（防止歪着插地上）
            transform.rotation = Quaternion.identity;

            // 关闭拖尾特效
            if (trail != null)
                trail.SetActive(false);
            if (realyFx != null)
                realyFx.SetActive(true);
        }

        // ======================================================================
        //  物理碰撞落地（比协程的速度检测更可靠）
        // ======================================================================

        private void OnCollisionEnter(Collision collision)
        {
            if (hasLanded) return;

            // 最小飞行时间保护，防止刚生成就误触落地
            if (UnityEngine.Time.time - launchTime < 0.5f) return;

            // 检查碰撞的物体是否属于 groundLayer
            int layerMask = 1 << collision.gameObject.layer;
            if ((groundLayer.value & layerMask) != 0)
            {
                OnLand();
            }
        }

        // ======================================================================
        //  超时自动消失
        // ======================================================================

        private IEnumerator AutoDespawnAfterDelay()
        {
            yield return new WaitForSeconds(maxLifetime);

            // 如果还没被捡走，超时自动销毁
            if (!DropItemData.isFlyingToPlayer && hasLanded)
            {
                Debug.Log($"[DropItemRune] 符文超时消失: {runeData?.runeName ?? "未知"}");
                DespawnSelf();
            }
        }

        // ======================================================================
        //  拾取到达（override 基类）
        // ======================================================================

        /// <summary>
        /// 符文飞到玩家身边后调用 — 存入局外背包并销毁
        /// </summary>
        protected override void OnReachPlayer()
        {
            if (runeData != null)
            {
                Debug.Log($"[DropItemRune] 玩家拾取符文: {runeData.runeName} (ID={runeData.runeId})");

                // 存入局外背包（不应用局内效果）
                RuneInventory.AddRune(runeData.runeId);
            }

            DespawnSelf();
        }

        // ======================================================================
        //  回收（本地直接销毁）
        // ======================================================================

        private void DespawnSelf()
        {
            Destroy(gameObject);
        }

        // ======================================================================
        //  OnDestroy 清理协程（防止销毁后协程还在跑）
        // ======================================================================

        private void OnDestroy()
        {
            if (landCheckCoroutine != null)
            {
                StopCoroutine(landCheckCoroutine);
                landCheckCoroutine = null;
            }
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }
        }
    }
}