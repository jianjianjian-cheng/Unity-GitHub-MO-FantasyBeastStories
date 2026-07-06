using Domain.Character;
using Domain.Character.Pets;
using Domain.Enemy.Boss;
using Domain.Event;
using Domain.Item;
using Domain.Services;
using Domain.Time;
using Photon.Pun;
using UnityEngine;

namespace Infrastructure.Network
{
    /// <summary>
    /// Domain 层 RPC 桥接器（Infrastructure 层）
    /// 统一持有所有 Domain 层的 [PunRPC] 方法，通过公共方法委托回 Domain 对象
    /// 职责：纯粹的 RPC 转发，不包含业务逻辑
    /// </summary>
    public class DomainRpcBridge : MonoBehaviourPun, IDomainRpcService
    {
        public static DomainRpcBridge Instance { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 注册到 NetworkServiceLocator，供 Domain 层通过接口调用
            NetworkServiceLocator.RegisterDomainRpcService(this);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================================
        // SyncedGameTimeManager RPC
        // ============================================================

        [PunRPC]
        public void RPC_SyncStartCaTime()
        {
            SyncedGameTimeManager.HandleSyncStartCaTime();
        }

        [PunRPC]
        public void RPC_OnTimeEventTriggered(string eventId, float triggerTime)
        {
            SyncedGameTimeManager.HandleOnTimeEventTriggered(eventId, triggerTime);
        }

        [PunRPC]
        public void RPC_GameTimeFinished()
        {
            SyncedGameTimeManager.HandleGameTimeFinished();
        }

        [PunRPC]
        public void RPC_SyncSetTime(float time)
        {
            SyncedGameTimeManager.HandleSyncSetTime(time);
        }

        [PunRPC]
        public void RPC_SyncStartTime()
        {
            SyncedGameTimeManager.HandleSyncStartTime();
        }

        [PunRPC]
        public void RPC_SyncPauseTime()
        {
            SyncedGameTimeManager.HandleSyncPauseTime();
        }

        [PunRPC]
        public void RPC_SyncResetTime()
        {
            SyncedGameTimeManager.HandleSyncResetTime();
        }

        [PunRPC]
        public void RPC_BossSpawn(string bossName)
        {
            SyncedGameTimeManager.HandleBossSpawn(bossName);
        }

        // ============================================================
        // PlayerController RPC
        // ============================================================

        [PunRPC]
        public void NoticeOtherPlayerDamage(string playerId, float maxHP, float currentHP)
        {
            if (EventChannelLocator.MainContainer?.healthUpdateChannel != null)
            {
                EventChannelLocator.MainContainer.healthUpdateChannel.Raise(
                    HealthUpdateData.OtherPlayer(playerId, maxHP, currentHP));
            }
        }

        [PunRPC]
        public void RPC_SyncPlayerElement(int actorNumber, int elementInt)
        {
            PlayerController.HandleSyncPlayerElement(actorNumber, elementInt);
        }

        // ============================================================
        // WizardBoy RPC
        // ============================================================

        [PunRPC]
        public void RPC_InitElementPool(int viewID, int elementInt)
        {
            var go = NetworkServiceLocator.ObjectService.FindByViewID(viewID);
            if (go != null)
            {
                var wizardBoy = go.GetComponent<WizardBoy>();
                if (wizardBoy != null)
                {
                    wizardBoy.HandleInitElementPool(elementInt);
                }
            }
        }

        // ============================================================
        // Boss_Horror (SpiderBoss) RPC
        // ============================================================

        [PunRPC]
        public void RPC_SyncPlayerTarget(int targetViewID)
        {
            // 场景中通常只有一个 SpiderBoss，直接查找
            var spiderBosses = FindObjectsByType<SpiderBoss>(FindObjectsSortMode.None);
            if (spiderBosses.Length > 0)
            {
                spiderBosses[0].HandleSyncPlayerTarget(targetViewID);
            }
        }

        [PunRPC]
        public void RPC_SyncTriggerAnim(string animName)
        {
            var spiderBosses = FindObjectsByType<SpiderBoss>(FindObjectsSortMode.None);
            if (spiderBosses.Length > 0)
            {
                spiderBosses[0].HandleSyncTriggerAnim(animName);
            }
        }

        // ============================================================
        // Ball Robot_Blue RPC
        // ============================================================

        [PunRPC]
        public void RPC_PlayTriggerAnimation(int viewID, string triggerName)
        {
            var go = NetworkServiceLocator.ObjectService.FindByViewID(viewID);
            if (go != null)
            {
                var ball = go.GetComponent<BallRobot_Blue>();
                if (ball != null)
                {
                    ball.HandlePlayTriggerAnimation(triggerName);
                }
            }
        }

        [PunRPC]
        public void RPC_OnPushed(int viewID, Vector3 pushDir, float moveDist)
        {
            var go = NetworkServiceLocator.ObjectService.FindByViewID(viewID);
            if (go != null)
            {
                var ball = go.GetComponent<BallRobot_Blue>();
                if (ball != null)
                {
                    ball.HandleOnPushed(pushDir, moveDist);
                }
            }
        }

        [PunRPC]
        public void RPC_StartTransfer(int viewID)
        {
            var go = NetworkServiceLocator.ObjectService.FindByViewID(viewID);
            if (go != null)
            {
                var ball = go.GetComponent<BallRobot_Blue>();
                if (ball != null)
                {
                    ball.HandleStartTransfer();
                }
            }
        }

        // ============================================================
        // IDomainRpcService 实现（供 Domain 层通过接口调用，消除直接依赖）
        // ============================================================

        /// <summary>
        /// RPC：非房主端请求房主销毁指定怪物（处理房主端未检测到死亡的情况）
        /// </summary>
        [PunRPC]
        public void RPC_RequestEnemyDestroy(int viewID)
        {
            var go = NetworkServiceLocator.ObjectService.FindByViewID(viewID);
            if (go != null)
            {
                PhotonNetwork.Destroy(go);
            }
        }

        /// <summary>
        /// 通过此 Bridge 的 PhotonView 发送 RPC 到指定目标
        /// </summary>
        public void InvokeRPC(string methodName, NetworkTarget target, params object[] parameters)
        {
            if (photonView == null)
            {
                Debug.LogWarning($"[DomainRpcBridge] photonView 为空，无法发送 RPC {methodName}");
                return;
            }
            photonView.RPC(methodName, MapTarget(target), parameters);
        }

        private static RpcTarget MapTarget(NetworkTarget target)
        {
            return target switch
            {
                NetworkTarget.All => RpcTarget.All,
                NetworkTarget.Others => RpcTarget.Others,
                NetworkTarget.MasterClient => RpcTarget.MasterClient,
                NetworkTarget.AllBuffered => RpcTarget.AllBuffered,
                _ => RpcTarget.All
            };
        }
    }
}