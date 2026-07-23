using System.Collections;
using Managers;
using Core;
using Core.Channels.Game;
using Controllers.PowerUp;
using Core.Contracts;
using Core.Network;
using Photon.Pun;
using UI;
using UI.Framework.Panel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Controllers.Network
{
    /// <summary>
    /// Application 层 RPC 桥接器（Infrastructure 层）
    /// 统一持有所有 Application 层的 [PunRPC] 方法，通过静态方法委托回 Application 管理器
    /// 职责：纯粹的 RPC 转发，不包含业务逻辑
    /// </summary>
    public class ManagerRpcBridge : MonoBehaviourPun
    {
        public static ManagerRpcBridge Instance { get; private set; }

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
        // PowerUpManager RPC
        // ============================================================

        /// <summary>房主 → 所有人：在指定位置生成一个道具</summary>
        [PunRPC]
        public void RPC_SpawnPowerUp(int itemId, Vector3 pos, int itemIndex)
        {
            PowerUpManager.HandleSpawnPowerUpRPC((uint)itemId, pos, itemIndex);
        }

        /// <summary>任意客户端 → 所有人：道具已被拾取，隐藏它</summary>
        [PunRPC]
        public void RPC_CollectPowerUp(int itemId)
        {
            PowerUpManager.HandleCollectPowerUpRPC((uint)itemId);
        }

        /// <summary>拾取者 → 所有人：经验磁铁启动，各客户端执行飞行动画</summary>
        [PunRPC]
        public void RPC_MagnetCollectExpBalls(int collectorActorNumber, float delay, float speed)
        {
            PowerUpManager.HandleMagnetCollectExpBallsRPC(collectorActorNumber, delay, speed);
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
        public void RPC_ReportCount(Vector3 killPosition, int enemyViewID, int reportType)
        {
            TaskManager.HandleReportCountRPC(killPosition, enemyViewID, reportType);
        }

        [PunRPC]
        public void RPC_UpdateProgress(string taskId, int count, bool completed)
        {
            TaskManager.HandleUpdateProgressRPC(taskId, count, completed);
        }

        // ============================================================
        // 返回大厅 RPC
        // ============================================================

        /// <summary>
        /// 房主 → 所有人：播放加载动画并返回大厅
        /// </summary>
        [PunRPC]
        public void RPC_ReturnToLobby()
        {
            StartCoroutine(ReturnToLobbyCoroutine());
        }

        private IEnumerator ReturnToLobbyCoroutine()
        {
            // 所有客户端都显示加载动画
            if (Loading.Instance != null)
                yield return Loading.Instance.Show();

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(1);
                Debug.Log("[ManagerRpcBridge] 房主发起切换到大厅场景");
            }
            else
            {
                Debug.Log("[ManagerRpcBridge] 非主机等待房主同步场景...");
            }
        }
    }
}