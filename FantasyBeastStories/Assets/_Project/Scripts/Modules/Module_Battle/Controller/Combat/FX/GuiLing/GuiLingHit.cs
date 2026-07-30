using Core;
using Core;
using UnityEngine;

namespace Controllers.Battle
{
    /// <summary>
    /// GuiLing 击中特效自动归还脚本
    ///
    /// 挂在击中特效预制体根节点上，从对象池取出后：
    /// 1. 自动播放 ParticleSystem
    /// 2. 等粒子播放完毕自动归还池
    /// </summary>
    public class GuiLingHit : MonoBehaviour
    {
        [SerializeField] private string poolName;

        private ParticleSystem _ps;

        private void Awake()
        {
            _ps = GetComponentInChildren<ParticleSystem>();
        }

        private void OnEnable()
        {
            // 重新播放粒子（对象池复用后可能已经停止）
            if (_ps != null)
            {
                _ps.time = 0f;
                _ps.Play();
            }

            // 根据粒子系统时长延迟归还，保底 2s，上限 3s（防止计算异常导致永久卡住）
            float duration = 2f;
            if (_ps != null)
            {
                float mainDuration = _ps.main.duration;
                float lifetime = _ps.main.startLifetime.constantMax;
                duration = Mathf.Clamp(Mathf.Max(mainDuration + lifetime, 0.5f), 0.5f, 3f);
            }

            CancelInvoke(nameof(ReturnToPool));
            Invoke(nameof(ReturnToPool), duration);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(ReturnToPool));
        }

        private void ReturnToPool()
        {
            if (!string.IsNullOrEmpty(poolName))
            {
                PoolHelper.Return(poolName, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}