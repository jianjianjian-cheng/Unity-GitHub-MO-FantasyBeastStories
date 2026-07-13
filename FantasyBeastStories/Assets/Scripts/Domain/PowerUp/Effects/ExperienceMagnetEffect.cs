using System.Collections.Generic;
using System.Linq;
using Domain.Item;
using UnityEngine;
using Domain.Event;
using Domain.Pool;
using Domain.Services;
using Application;
using Domain.PowerUp.Interfaces;

namespace Domain.PowerUp.Effects
{
    /// <summary>
    /// 经验磁铁效果 - 吸收地图上所有经验球
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
            // 在持久对象上启动协程，避免被对象池回收时中断
            if (PowerUpManager.Instance != null)
                PowerUpManager.Instance.StartCoroutine(CollectAllExperienceBalls(player));
            else
                StartCoroutine(CollectAllExperienceBalls(player));

            PlayCollectEffects(player.transform.position);
        }

        private System.Collections.IEnumerator CollectAllExperienceBalls(GameObject player)
        {
            // ★ 关键：在销毁前把值存到局部变量，避免访问已销毁的对象
            float delay = collectDelay;
            float speed = flySpeed;

            var allBalls = FindObjectsOfType<ExperienceBallBase>()
                .Where(ball => ball != null && ball.gameObject.activeInHierarchy)
                .ToList();

            Debug.Log($"[PowerUp] 经验磁铁启动！发现 {allBalls.Count} 个经验球");

            foreach (var ball in allBalls)
            {
                if (ball == null || !ball.gameObject.activeInHierarchy) continue;

                // 在PowerUpManager上启动子协程（避免this.StartCoroutine访问已销毁对象）
                var host = PowerUpManager.Instance != null ? (MonoBehaviour)PowerUpManager.Instance : this;
                host.StartCoroutine(FlyBallToPlayer(ball, player, speed));
                yield return new WaitForSeconds(delay);
            }

            Debug.Log($"[PowerUp] 经验磁铁完成！共吸收所有经验球");
        }

        private System.Collections.IEnumerator FlyBallToPlayer(ExperienceBallBase ball, GameObject player, float speed)
        {
            if (ball == null) yield break;

            float duration = 0f;
            Vector3 startPos = ball.transform.position;
            Quaternion startRot = ball.transform.rotation;

            while (ball != null && ball.gameObject.activeInHierarchy && duration < 1f)
            {
                if (player == null) yield break;

                duration += UnityEngine.Time.deltaTime * speed * 0.1f;
                ball.transform.position = Vector3.Lerp(startPos, player.transform.position, duration);
                ball.transform.rotation = Quaternion.Slerp(startRot, player.transform.rotation, duration);

                yield return null;
            }

            if (ball != null && ball.gameObject.activeInHierarchy)
            {
                var reflection = ball.GetType().GetField("ExperienceValue",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (reflection != null)
                {
                    int expValue = (int)reflection.GetValue(ball);

                    EventChannelLocator.MainContainer.experienceChannel.Raise(expValue);
                    Debug.Log($"[PowerUp] 吸收经验球 +{expValue}");
                }

                ServiceLocator.Get<ObjectPoolManager>()?.ReturnToPool(
                    PoolConst.ExperienceBall_Blue_Local,
                    ball.gameObject
                );
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