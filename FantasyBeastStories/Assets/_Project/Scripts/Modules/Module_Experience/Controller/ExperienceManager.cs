using System.Collections;
using System.Collections.Generic;
using Core;
using Core.SharedModel;
using Core.Channels.Game;
using Core.Channels.General;
using Core.Channels.Player;
using Controllers.Item;
using Core.Contracts;
using Core.Network;
using NetworkTarget = Controllers.Network;
using Controllers.Network;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Managers;

namespace Managers
{
    /// <summary>
    /// 经验控制器 — 薄层 MonoBehaviour，持有 ExperienceModel 实例。
    ///
    /// 职责：
    /// - 生命周期管理（单例 + DontDestroyOnLoad）
    /// - 处理外部依赖（RPC / ObjectPool / MatchStatisticsManager / QuestTaskManager / Coroutine）
    /// - 业务逻辑委托给 ExperienceModel
    /// - 经验球 GameObject 生命周期管理（activeExpBalls 字典）
    /// </summary>
    public class ExperienceManager : MonoBehaviour
    {
        

        /// <summary>经验模型实例（纯 C#，可单测）</summary>
        public ExperienceModel Model { get; private set; }

        /// <summary>当前活跃的本地经验球映射 ballId → GameObject（View 层引用，不放 Model）</summary>
        private readonly Dictionary<uint, GameObject> _activeExpBalls = new();

        void Awake()
        {
                  ServiceLocator.Register(this);
            Model = new ExperienceModel();
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister<ExperienceManager>();
        }

        void OnEnable()
        {
            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
                Model.Initialize();

            SceneManager.sceneLoaded += OnSceneLoaded;
            EventChannelLocator.MainContainer.gameActionChannel.RegisterListener(OnGameActionReceived);
            EventChannelLocator.MainContainer.experienceChannel.RegisterListener(OnExperienceReceived);
            EventChannelLocator.MainContainer.skillQueryChannel.RegisterListener(OnSkillQuery);
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventChannelLocator.MainContainer.gameActionChannel.UnregisterListener(OnGameActionReceived);
            EventChannelLocator.MainContainer.experienceChannel.UnregisterListener(OnExperienceReceived);
            EventChannelLocator.MainContainer.skillQueryChannel.UnregisterListener(OnSkillQuery);

            _activeExpBalls.Clear();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex > 1)
                Model.Initialize();
        }

        private void OnSkillQuery(SkillQueryData data)
        {
            if (data.queryType == SkillQueryType.GetUpgradeExperience)
                data.intValue = Model.UpgradeExperience;
        }

        private void OnGameActionReceived(GameActionType actionType)
        {
            if (actionType == GameActionType.UpgradeAllConfirmed)
                OnPlayerUpgradeChoiceConfirmed();
        }

        private void OnExperienceReceived(int experience)
        {
            AddExperience(experience);
        }

        // ──────────────────────────────────
        //  经验获取
        // ──────────────────────────────────

        public void AddExperience(int experience)
        {
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest
                && !NetworkServiceLocator.PlayerService.IsMasterClient)
                return;

            var newLevels = Model.AddExperience(experience);

            // 联动外部系统
            ServiceLocator.Get<MatchStatisticsManager>()?.RecordExperience(experience);
            ServiceLocator.Get<QuestTaskManager>()?.RecordExp();

            // RPC 同步到所有客户端
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                NetworkServiceLocator.ObjectService.InvokeRPC(
                    ManagerRpcBridge.Instance, "RPC_SyncExperience",
                    NetworkTarget.NetworkTarget.All, Model.CurrentExperience);
            }

            if (Model.HasPendingLevelUps && !Model.IsProcessingLevelUp)
                StartLevelUpSequence();
        }

        public void SetExperience(int experience)
        {
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest
                && !NetworkServiceLocator.PlayerService.IsMasterClient)
                return;

            Model.SetExperience(experience);

            if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                NetworkServiceLocator.ObjectService.InvokeRPC(
                    ManagerRpcBridge.Instance, "RPC_SyncExperience",
                    NetworkTarget.NetworkTarget.All, Model.CurrentExperience);
            }

            if (Model.HasPendingLevelUps && !Model.IsProcessingLevelUp)
                StartLevelUpSequence();
        }

        // ──────────────────────────────────
        //  升级队列流程（协程）
        // ──────────────────────────────────

        private void StartLevelUpSequence()
        {
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest
                && !NetworkServiceLocator.PlayerService.IsMasterClient)
                return;

            Model.IsProcessingLevelUp = true;
            ProcessNextLevelUp();
        }

        private void ProcessNextLevelUp()
        {
            if (!Model.HasPendingLevelUps)
            {
                Model.IsProcessingLevelUp = false;
                return;
            }

            int levelForThisChoice = Model.DequeueLevelUp();
            StartCoroutine(OpenMagicUpgradePanelWithDelay());
        }

        IEnumerator OpenMagicUpgradePanelWithDelay()
        {
            yield return new WaitForSeconds(1f);

            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(true);
                yield break;
            }
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "OpenMagicUpgradePanel",
                NetworkTarget.NetworkTarget.All);
        }

        public void OnPlayerUpgradeChoiceConfirmed()
        {
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "CloseMagicUpgradePanel",
                NetworkTarget.NetworkTarget.All);
        }

        // ──────────────────────────────────
        //  RPC 静态 Handler（由 ManagerRpcBridge 调用，保持向后兼容）
        // ──────────────────────────────────

        public static void HandleCloseMagicUpgradePanelRPC()
        {
            EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(false);

            if (!ServiceLocator.TryGet<ExperienceManager>(out var inst)) return;

            if (!EventChannelLocator.MainContainer.gameSettings.IsTest
                && !NetworkServiceLocator.PlayerService.IsMasterClient)
                return;

            if (inst.Model.TryConsumeBonusReward())
            {
                inst.StartCoroutine(inst.OpenExUpgradePanelWithDelay());
            }
            else
            {
                inst.StartCoroutine(inst.DelayedProcessNextLevelUp());
            }
        }

        private IEnumerator DelayedProcessNextLevelUp()
        {
            yield return new WaitForSeconds(0.5f);
            ProcessNextLevelUp();
        }

        private IEnumerator OpenExUpgradePanelWithDelay()
        {
            yield return new WaitForSeconds(1f);

            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                if (ServiceLocator.Get<MagicUpgradeManager>() != null)
                    ServiceLocator.Get<MagicUpgradeManager>().isAllExCard = true;
                EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(true);
                yield break;
            }
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "OpenExMagicUpgradePanel",
                NetworkTarget.NetworkTarget.All);
        }

        public static void HandleSyncExperienceRPC(int syncedExp)
        {
            if (!ServiceLocator.TryGet<ExperienceManager>(out var inst)) return;
            inst.Model.SyncExperience(syncedExp);
        }

        public static void HandleIncreaseLevelRPC(int requiredExp)
        {
            if (!ServiceLocator.TryGet<ExperienceManager>(out var inst)) return;
            inst.Model.IncreaseLevel(requiredExp);
        }

        // ──────────────────────────────────
        //  经验球非网络化 RPC 处理
        // ──────────────────────────────────

        public static void HandleSpawnExpBallRPC(uint ballId, Vector3 position, int expValue)
        {
            if (!ServiceLocator.TryGet<ExperienceManager>(out var inst)) return;
            inst.SpawnLocalExpBall(ballId, position, expValue);
        }

        public static void HandleClaimExpBallRPC(uint ballId, int expValue)
        {
            if (!ServiceLocator.TryGet<ExperienceManager>(out var inst)) return;
            inst.ClaimLocalExpBall(ballId, expValue);
        }

        public static void HandleExpBallCollectedRPC(uint ballId)
        {
            if (!ServiceLocator.TryGet<ExperienceManager>(out var inst)) return;
            inst.HideLocalExpBall(ballId);
        }

        // ──────────────────────────────────
        //  经验球实例方法
        // ──────────────────────────────────

        public uint GenerateBallId() => Model.GenerateBallId();

        private void SpawnLocalExpBall(uint ballId, Vector3 position, int expValue)
        {
            var poolManager = ServiceLocator.Get<ObjectPoolManager>();
            if (poolManager == null)
            {
                Debug.LogWarning("[ExperienceManager] ObjectPoolManager 不可用，无法生成经验球");
                return;
            }

            var ballObj = poolManager.GetFromPoolAndActivate(PoolConst.ExperienceBall_Blue_Local, position);
            if (ballObj == null)
            {
                Debug.LogWarning($"[ExperienceManager] 本地经验球池 {PoolConst.ExperienceBall_Blue_Local} 为空");
                return;
            }

            var ball = ballObj.GetComponent<ExperienceBallBase>();
            if (ball == null)
            {
                poolManager.ReturnToPool(PoolConst.ExperienceBall_Blue_Local, ballObj);
                return;
            }

            ball.Setup(ballId, expValue);
            _activeExpBalls[ballId] = ballObj;
        }

        private void ClaimLocalExpBall(uint ballId, int expValue)
        {
            if (!Model.TryClaimBall(ballId))
            {
                Debug.Log($"[ExperienceManager] 球 {ballId} 已被认领，忽略重复请求");
                return;
            }

            AddExperience(expValue);

            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "RPC_ExpBallCollected",
                NetworkTarget.NetworkTarget.All, (int)ballId);
        }

        private void HideLocalExpBall(uint ballId)
        {
            if (!_activeExpBalls.TryGetValue(ballId, out var ballObj))
                return;

            _activeExpBalls.Remove(ballId);

            var poolManager = ServiceLocator.Get<ObjectPoolManager>();
            if (poolManager != null)
                poolManager.ReturnToPool(PoolConst.ExperienceBall_Blue_Local, ballObj);
            else
                Destroy(ballObj);
        }

        // ──────────────────────────────────
        //  便捷转发（向后兼容）
        // ──────────────────────────────────

        public int GetUpgradeExperience() => Model.UpgradeExperience;
        public int GetCurrentLevel() => Model.CurrentLevel;
        public int GetCurrentExperience() => Model.CurrentExperience;
    }
}
