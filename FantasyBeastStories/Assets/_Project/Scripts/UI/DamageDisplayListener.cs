using Managers;
using Core;
using Core.Channels.Combat;
using Core;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 伤害数字显示监听器（Presentation 层）
    /// 监听 Domain 层广播的 DamageDisplay 事件，负责实际的 DamageNum 对象池获取与播放
    /// 职责：仅处理 UI 展示逻辑，不包含业务计算
    /// </summary>
    public class DamageDisplayListener : MonoBehaviour
    {
        [SerializeField] private DamageDisplayEventChannelSO damageDisplayChannel;
        [SerializeField] private string damageNumPoolName = PoolConst.DamageNumPool;

        void OnEnable()
        {
            if (damageDisplayChannel != null)
            {
                damageDisplayChannel.RegisterListener(OnDamageDisplayRequested);
            }
            else
            {
                // 备用方案：从全局容器获取
                var globalChannel = EventChannelLocator.MainContainer?.combat?.damageDisplayChannel;
                if (globalChannel != null)
                {
                    globalChannel.RegisterListener(OnDamageDisplayRequested);
                    damageDisplayChannel = globalChannel;
                }
            }
        }

        void OnDisable()
        {
            if (damageDisplayChannel != null)
            {
                damageDisplayChannel.UnregisterListener(OnDamageDisplayRequested);
            }
        }

        private void OnDamageDisplayRequested(DamageDisplayEventArgs args)
        {
            if (args == null) return;

            GameObject damageNumObj = ServiceLocator.Get<ObjectPoolManager>()
                ?.GetFromPoolAndActivate(
                    damageNumPoolName,
                    args.worldPosition
                );

            if (damageNumObj != null)
            {
                var damageNum = damageNumObj.GetComponent<DamageNum>();
                if (damageNum != null)
                {
                    damageNum.Play(args.damageValue, args.worldPosition, args.isCritical);
                }
            }
        }
    }
}