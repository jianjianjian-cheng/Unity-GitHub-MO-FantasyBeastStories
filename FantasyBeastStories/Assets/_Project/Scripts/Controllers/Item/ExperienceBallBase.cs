using System.Collections;
using System.Collections.Generic;
using Managers;
using Core;
using Core.Channels;
using Core;
using Core.Contracts;
using Core.Network;
using NetworkTarget = Controllers.Network.NetworkTarget;
using Controllers.Network;
using UnityEngine;
using Core.Audio;


namespace Controllers.Item
{
    public class ExperienceBallBase : DropItemBase
    {
        /// <summary>球体唯一标识，由房主在生成时分配，用于拾取去重</summary>
        public uint BallId { get; private set; }

        /// <summary>经验值</summary>
        protected int ExperienceValue;

        /// <summary>
        /// 由 RPC 接收后调用，初始化球体数据
        /// </summary>
        public void Setup(uint id, int value)
        {
            BallId = id;
            ExperienceValue = value;
        }

        protected override void OnReachPlayer()
        {
            // 拾取音效
            AudioManager.Instance?.PlaySFX("sfx_PickUp", transform.position);

            // 先触发经验获取事件（本地立刻生效，提升手感）
            EventChannelLocator.MainContainer.experienceChannel.Raise(ExperienceValue);

            bool isTest = EventChannelLocator.MainContainer.gameSettings.IsTest;
            if (isTest)
            {
                // 测试模式：直接返回本地池
                ServiceLocator.Get<ObjectPoolManager>()?.ReturnToPool(PoolConst.ExperienceBall_Blue_Local, gameObject);
                return;
            }

            // 联机模式：上报房主 + 本地立刻回收
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "RPC_ClaimExpBall",
                NetworkTarget.MasterClient, (int)BallId, ExperienceValue);

            // 本地立刻回收（不等 RPC 返回，视觉反馈无延迟）
            ServiceLocator.Get<ObjectPoolManager>()?.ReturnToPool(PoolConst.ExperienceBall_Blue_Local, gameObject);
        }
    }
}