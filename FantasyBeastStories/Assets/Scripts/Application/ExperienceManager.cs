using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Domain.Event.Channels.Game;
using Domain.Event.Channels.General;
using Domain.Event.Channels.Player;
using Domain.Services;
using Infrastructure.Network;
using Presentation.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Application
{
    /// <summary>
    /// 经验值/升级业务逻辑（Application 层）
    ///
    /// 职责：
    /// - 管理经验值增减、升级计算
    /// - 维护升级队列，逐级处理
    /// - 通过 RPC 同步经验/等级到所有客户端
    /// - 通过 EventChannel 与 Presentation 层通信
    ///
    /// 通信方式：
    /// 输入 → experienceChannel（拾取经验球）
    /// 输入 → gameActionChannel（升级确认操作）
    /// 输入 → skillQueryChannel（升级经验查询）
    /// 输出 → experienceUpdateChannel（更新 UI）
    /// 输出 → magicUpgradeChannel（开关升级面板）
    /// </summary>
    public class ExperienceManager : MonoBehaviour
    {
        public static ExperienceManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ========== 升级队列系统 ==========
        private Queue<int> pendingLevelUps = new Queue<int>();
        private bool isProcessingLevelUp = false;

        // 经验/等级状态
        private int currentExperience;
        private int currentLevel;
        private int upgradeExperience;

        // 专属卡牌升级倍数奖励：记录当前选卡对应的等级，用于判断是否触发 3 级额外奖励
        private int lastBonusCheckLevel = -1;

        void OnEnable()
        {
            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                Initialize();
            }
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
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex > 1)
            {
                Initialize();
            }
        }

        private void OnSkillQuery(SkillQueryData data)
        {
            if (data.queryType == SkillQueryType.GetUpgradeExperience)
            {
                data.intValue = upgradeExperience;
            }
        }

        private void OnGameActionReceived(GameActionType actionType)
        {
            switch (actionType)
            {
                case GameActionType.UpgradeAllConfirmed:
                    OnPlayerUpgradeChoiceConfirmed();
                    break;
            }
        }

        private void Initialize()
        {
            upgradeExperience = 100;
            RaiseExperienceUpdate();
        }

        #region 经验条相关

        public int GetUpgradeExperience()
        {
            return upgradeExperience;
        }

        private void OnExperienceReceived(int experience)
        {
            AddExperience(experience);
        }

        /// <summary>
        /// 发送经验/等级更新事件到 Presentation 层
        /// </summary>
        private void RaiseExperienceUpdate()
        {
            var data = new ExperienceUpdateData(currentExperience, upgradeExperience, currentLevel);
            EventChannelLocator.MainContainer.experienceUpdateChannel.Raise(data);
        }

        public void AddExperience(int experience)
        {
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest && !NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                return;
            }

            currentExperience += experience;

            // 记录本次获得的经验到对局统计
            MatchStatisticsManager.Instance?.RecordExperience(experience);

            CheckAndQueueUpgrades();
            NetworkServiceLocator.ObjectService.InvokeRPC(AppRpcBridge.Instance, "RPC_SyncExperience", NetworkTarget.All, currentExperience);

            if (pendingLevelUps.Count > 0 && !isProcessingLevelUp)
            {
                StartLevelUpSequence();
            }
        }

        private void StartLevelUpSequence()
        {
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                {
                    return;
                }
            }

            isProcessingLevelUp = true;
            ProcessNextLevelUp();
        }

        private void ProcessNextLevelUp()
        {
            if (pendingLevelUps.Count == 0)
            {
                CompleteAllLevelUps();
                return;
            }

            int levelForThisChoice = pendingLevelUps.Dequeue();
            lastBonusCheckLevel = levelForThisChoice; // 保存等级供面板关闭时检查倍数奖励
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
            NetworkServiceLocator.ObjectService.InvokeRPC(AppRpcBridge.Instance, "OpenMagicUpgradePanel", NetworkTarget.All);
        }

        private void CheckAndQueueUpgrades()
        {
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                {
                    return;
                }
            }

            while (currentExperience >= upgradeExperience)
            {
                int requiredExp = upgradeExperience;

                if (EventChannelLocator.MainContainer.gameSettings.IsTest)
                {
                    HandleIncreaseLevelRPC(requiredExp);
                }
                else
                {
                    NetworkServiceLocator.ObjectService.InvokeRPC(AppRpcBridge.Instance, "IncreaseLevel", NetworkTarget.All, requiredExp);
                }

                pendingLevelUps.Enqueue(currentLevel);
            }

            // 通过事件通道更新等级显示
            RaiseExperienceUpdate();
        }

        private void CompleteAllLevelUps()
        {
            isProcessingLevelUp = false;
            Debug.Log("所有升级处理完毕");
        }

        public void OnPlayerUpgradeChoiceConfirmed()
        {
            NetworkServiceLocator.ObjectService.InvokeRPC(AppRpcBridge.Instance, "CloseMagicUpgradePanel", NetworkTarget.All);
        }

        /// <summary>
        /// 由 AppRpcBridge 在收到 RPC 后调用
        /// 关闭升级面板并在 Master 客户端启动下一级升级流程
        /// 每 3 级额外触发一次专属卡牌升级（三张不重复专属卡牌）
        /// </summary>
        public static void HandleCloseMagicUpgradePanelRPC()
        {
            EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(false);
            if (Instance != null && NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                // 每升级 3 次（3、6、9...级）额外赠送一次专属卡牌升级
                if (Instance.lastBonusCheckLevel > 0 && Instance.lastBonusCheckLevel % 3 == 0)
                {
                    Instance.lastBonusCheckLevel = -1; // 防止重复触发
                    Instance.StartCoroutine(Instance.OpenExUpgradePanelWithDelay());
                }
                else
                {
                    Instance.lastBonusCheckLevel = -1;
                    Instance.StartCoroutine(Instance.DelayedProcessNextLevelUp());
                }
            }
        }

        private IEnumerator DelayedProcessNextLevelUp()
        {
            yield return new WaitForSeconds(0.5f);
            ProcessNextLevelUp();
        }

        /// <summary>
        /// 专属卡牌升级面板：延迟后打开，显示三张不重复专属卡牌
        /// </summary>
        private IEnumerator OpenExUpgradePanelWithDelay()
        {
            yield return new WaitForSeconds(1f);
            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                if (MagicUpgradeManager.instance != null)
                    MagicUpgradeManager.instance.isAllExCard = true;
                EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(true);
                yield break;
            }
            NetworkServiceLocator.ObjectService.InvokeRPC(AppRpcBridge.Instance, "OpenExMagicUpgradePanel", NetworkTarget.All);
        }

        /// <summary>
        /// 由 AppRpcBridge 在收到 RPC 后调用：同步经验值到所有客户端
        /// </summary>
        public static void HandleSyncExperienceRPC(int syncedExp)
        {
            if (Instance == null) return;
            Instance.currentExperience = syncedExp;
            Instance.RaiseExperienceUpdate();
        }

        /// <summary>
        /// 由 AppRpcBridge 在收到 RPC 后调用：增加等级
        /// </summary>
        public static void HandleIncreaseLevelRPC(int requiredExp)
        {
            if (Instance == null) return;
            Instance.currentExperience -= requiredExp;
            Instance.currentLevel++;
            Instance.upgradeExperience = (int)(Instance.upgradeExperience * 1.5);

            Instance.RaiseExperienceUpdate();
        }

        public void SetExperience(int experience)
        {
            if (!EventChannelLocator.MainContainer.gameSettings.IsTest && !NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                return;
            }

            currentExperience = experience;
            RaiseExperienceUpdate();

            CheckAndQueueUpgrades();
            if (pendingLevelUps.Count > 0 && !isProcessingLevelUp)
            {
                StartLevelUpSequence();
            }
        }

        public int GetCurrentLevel()
        {
            return currentLevel;
        }

        public int GetCurrentExperience()
        {
            return currentExperience;
        }
        #endregion
    }
}