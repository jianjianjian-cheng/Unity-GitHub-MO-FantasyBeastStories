using System.Collections.Generic;
using System.Linq;
using Controllers.Item;
using UnityEngine;
using Core;
using Core;
using Core.Contracts;
using Core.Network;
using Managers;
using Controllers.PowerUp;
using NetworkTarget = Controllers.Network.NetworkTarget;
using Controllers.Network;

namespace Controllers.PowerUp
{
    /// <summary>
    /// 经验磁铁效果 - 吸收地图上所有经验球
    /// 联机模式下通过 RPC 广播到所有客户端，各自执行飞行动画
    /// </summary>
    public class ExperienceMagnetEffect : PowerUpEffectBase
    {
        [Header("经验磁铁参数")]
        [SerializeField] private float magnetRange = 100f; // 吸附范围
        [SerializeField] private float collectDelay = 0.05f; // 逐个收集间隔
        [SerializeField] private float flySpeed = 25f; // 经验球飞行速度
        [SerializeField] private bool showDebugGizmos = true;

        public override void Execute(GameObject player)
        {
            bool isTest = EventChannelLocator.MainContainer?.gameSettings?.IsTest ?? true;

            if (isTest)
            {
                // 测试模式：本地直接执行
                var host = ServiceLocator.Get<PowerUpManager>() != null ? (MonoBehaviour)ServiceLocator.Get<PowerUpManager>() : this;
                host.StartCoroutine(FlyAllBallsToCollector(player, true, collectDelay, flySpeed));
            }
            else
            {
                // 联机模式：广播 RPC 到所有客户端，各自执行飞行动画
                int collectorActorNumber = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(player.transform);
                NetworkServiceLocator.ObjectService.InvokeRPC(
                    ManagerRpcBridge.Instance, "RPC_MagnetCollectExpBalls",
                    NetworkTarget.All, collectorActorNumber, collectDelay, flySpeed);
            }

            PlayCollectEffects(player.transform.position);
        }

        /// <summary>
        /// 所有客户端执行：经验球逐个飞向拾取者
        /// </summary>
        public static System.Collections.IEnumerator FlyAllBallsToCollector(GameObject collector, bool isCollector, float delay, float speed)
        {
            var allBalls = FindObjectsOfType<ExperienceBallBase>()
                .Where(ball => ball != null && ball.gameObject.activeInHierarchy)
                .ToList();

            Debug.Log($"[PowerUp] 经验磁铁启动！发现 {allBalls.Count} 个经验球, isCollector={isCollector}");

            foreach (var ball in allBalls)
            {
                if (ball == null || !ball.gameObject.activeInHierarchy) continue;

                var host = ServiceLocator.Get<PowerUpManager>();
                if (host == null) continue;

                host.StartCoroutine(FlyBallToCollector(ball, collector, speed, isCollector));
                yield return new WaitForSeconds(delay);
            }

            Debug.Log("[PowerUp] 经验磁铁完成！");
        }

        private static System.Collections.IEnumerator FlyBallToCollector(ExperienceBallBase ball, GameObject collector, float speed, bool isCollector)
        {
            if (ball == null) yield break;

            float duration = 0f;
            Vector3 startPos = ball.transform.position;
            Quaternion startRot = ball.transform.rotation;

            while (ball != null && ball.gameObject.activeInHierarchy && duration < 1f)
            {
                if (collector == null) yield break;

                duration += UnityEngine.Time.deltaTime * speed * 0.1f;
                ball.transform.position = Vector3.Lerp(startPos, collector.transform.position, duration);
                ball.transform.rotation = Quaternion.Slerp(startRot, collector.transform.rotation, duration);

                yield return null;
            }

            if (ball != null && ball.gameObject.activeInHierarchy)
            {
                if (isCollector)
                {
                    // 拾取者客户端：走正常拾取流程（触发经验 → 上报房主 → 房主广播隐藏 → 本地回收）
                    ball.Collect();
                }
                else
                {
                    // 其他客户端：本地回收（拾取者的 Collect → 房主 RPC_ExpBallCollected 会做最终清理）
                    ServiceLocator.Get<ObjectPoolManager>()?.ReturnToPool(
                        PoolConst.ExperienceBall_Blue_Local, ball.gameObject);
                }
                Debug.Log("[PowerUp] 吸收经验球");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, magnetRange);
        }
    }
}
