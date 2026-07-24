using Controllers.Character;
using Controllers.Character.Pets;
using Controllers.Enemy;
using Core;
using Controllers.Item;
using Core.Contracts;
using Core.Network;
using Controllers.Time;
using Photon.Pun;
using UnityEngine;

namespace Controllers.Network
{
    /// <summary>
    /// controller层RPC桥接器
    /// 纯粹RPC转发,不包含业务逻辑
    /// </summary>
    public class ControllerRpcBridge : MonoBehaviourPun, IControllerRpcService
    {
        public static ControllerRpcBridge Instance { get; private set; }

        private SpiderBoss _spiderBossCache;

        /// <summary>注册场景中的 SpiderBoss 引用，避免每次 RPC 都做 FindObjectsByType</summary>
        public void RegisterSpiderBoss(SpiderBoss boss) => _spiderBossCache = boss;

        /// <summary>清除 SpiderBoss 缓存（Boss 销毁时调用）</summary>
        public void ClearSpiderBossCache() => _spiderBossCache = null;

        private SpiderBoss GetSpiderBoss()
        {
            // Unity 的 == 重载会在对象销毁后返回 true，所以这里天然支持缓存失效
            if (_spiderBossCache != null)
                return _spiderBossCache;
            var bosses = FindObjectsByType<SpiderBoss>(FindObjectsSortMode.None);
            if (bosses.Length > 0)
                _spiderBossCache = bosses[0];
            return _spiderBossCache;
        }

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

        [PunRPC]
        public void NoticePlayerDeath(int actorNumber)
        {
            PlayerController.HandlePlayerDeath(actorNumber);
        }

        // ============================================================
        // PlayerController RPC (原 WizardBoy RPC，Phase 3 通用化)
        // ============================================================

        [PunRPC]
        public void RPC_InitElementPool(int viewID, int elementInt)
        {
            var go = NetworkServiceLocator.ObjectService.FindByViewID(viewID);
            if (go != null)
            {
                // Phase 3: 改为通用 PlayerController，由 Lua 桥接器处理角色专属逻辑
                var playerController = go.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.HandleInitElementPool(elementInt);
                }
            }
        }

        // ============================================================
        // Boss_Horror (SpiderBoss) RPC
        // ============================================================

        [PunRPC]
        public void RPC_SyncPlayerTarget(int targetViewID)
        {
            var boss = GetSpiderBoss();
            if (boss != null)
            {
                boss.HandleSyncPlayerTarget(targetViewID);
            }
        }

        [PunRPC]
        public void RPC_SyncTriggerAnim(string animName)
        {
            var boss = GetSpiderBoss();
            if (boss != null)
            {
                boss.HandleSyncTriggerAnim(animName);
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
        // IControllerRpcService 实现（供 Domain 层通过接口调用，消除直接依赖）
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
                Debug.LogWarning($"[ControllerRpcBridge] photonView is null, cannot send RPC {methodName}");
                return;
            }
            photonView.RPC(methodName, NetworkTargetMapper.ToRpcTarget(target), parameters);
        }
    }
}