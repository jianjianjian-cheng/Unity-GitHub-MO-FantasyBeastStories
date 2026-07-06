using System.Collections;
using Application;
using Domain.Event;
using Domain.Event.Channels.Game;
using Domain.Services;
using Photon.Pun;
using Presentation.UI;
using UnityEngine;

namespace Infrastructure.Network
{
    /// <summary>
    /// Application 层 RPC 桥接器（Infrastructure 层）
    /// 统一持有所有 Application 层的 [PunRPC] 方法，通过静态方法委托回 Application 管理器
    /// 职责：纯粹的 RPC 转发，不包含业务逻辑
    /// </summary>
    public class AppRpcBridge : MonoBehaviourPun
    {
        public static AppRpcBridge Instance { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================================
        // GamePauseManager RPC
        // ============================================================

        [PunRPC]
        public void RPC_SetPauseState(bool pause)
        {
            GamePauseManager.HandlePauseStateRPC(pause);
        }

        // ============================================================
        // ExperienceManager RPC
        // ============================================================

        [PunRPC]
        public void OpenMagicUpgradePanel()
        {
            EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(true);
        }

        [PunRPC]
        public void OpenExMagicUpgradePanel()
        {
            if (MagicUpgradeManager.instance != null)
                MagicUpgradeManager.instance.isAllExCard = true;
            EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(true);
        }

        [PunRPC]
        public void CloseMagicUpgradePanel()
        {
            EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(false);
            ExperienceManager.HandleCloseMagicUpgradePanelRPC();
        }

        [PunRPC]
        public void RPC_SyncExperience(int syncedExp)
        {
            ExperienceManager.HandleSyncExperienceRPC(syncedExp);
        }

        [PunRPC]
        public void IncreaseLevel(int requiredExp)
        {
            ExperienceManager.HandleIncreaseLevelRPC(requiredExp);
        }

        // ============================================================
        // 经验球非网络化 RPC（方案二）
        // ============================================================

        /// <summary>房主 → 所有人：在指定位置生成一个经验球</summary>
        [PunRPC]
        public void RPC_SpawnExpBall(int ballId, Vector3 pos, int expValue)
        {
            ExperienceManager.HandleSpawnExpBallRPC((uint)ballId, pos, expValue);
        }

        /// <summary>任意客户端 → 房主：认领一个经验球</summary>
        [PunRPC]
        public void RPC_ClaimExpBall(int ballId, int expValue)
        {
            ExperienceManager.HandleClaimExpBallRPC((uint)ballId, expValue);
        }

        /// <summary>房主 → 所有人：该球已被收集，隐藏它</summary>
        [PunRPC]
        public void RPC_ExpBallCollected(int ballId)
        {
            ExperienceManager.HandleExpBallCollectedRPC((uint)ballId);
        }

        // ============================================================
        // TaskManager RPC
        // ============================================================

        [PunRPC]
        public void RPC_UpdateAllPlayerTimeUI(string time)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.UpdateTime(time));
        }

        [PunRPC]
        public void RPC_TaskFailed()
        {
            TaskManager.HandleTaskFailedRPC();
        }

        [PunRPC]
        public void RPC_SetNotice(string name, string description, int limitTime, int requeredCount)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(
                TaskUIUpdateData.ShowNotice(name, description, limitTime, requeredCount));
        }

        [PunRPC]
        public void RPC_ActivateKillTask(string taskId, int limitTime, Vector3 zoneCenter, int requiredKills)
        {
            TaskManager.HandleActivateKillTaskRPC(taskId, limitTime, zoneCenter, requiredKills);
        }

        [PunRPC]
        public void RPC_ActivateEscortTask(string taskId, int limitTime, Vector3 zoneCenter, int requiredEscorts)
        {
            TaskManager.HandleActivateEscortTaskRPC(taskId, limitTime, zoneCenter, requiredEscorts);
        }

        [PunRPC]
        public void RPC_ReportCount(Vector3 killPosition, int enemyViewID)
        {
            TaskManager.HandleReportCountRPC(killPosition, enemyViewID);
        }

        [PunRPC]
        public void RPC_UpdateProgress(string taskId, int count, bool completed)
        {
            TaskManager.HandleUpdateProgressRPC(taskId, count, completed);
        }
    }
}