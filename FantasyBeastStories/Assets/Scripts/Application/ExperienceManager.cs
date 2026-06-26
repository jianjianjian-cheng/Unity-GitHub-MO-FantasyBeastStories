using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Domain.Event.Channels.Game;
using Domain.Event.Channels.General;
using Domain.Event.Channels.Player;
using Domain.Services;
using Photon.Pun; // 仅保留 [PunRPC] 属性引用，RPC 调用已通过 NetworkServiceLocator 解耦
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
        void Awake()
        {
            NetworkServiceLocator.ObjectService.EnsureView(this);
        }

        // ========== 升级队列系统 ==========
        private Queue<int> pendingLevelUps = new Queue<int>();
        private bool isProcessingLevelUp = false;

        // 经验/等级状态
        private int currentExperience;
        private int currentLevel;
        private int upgradeExperience;

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

            CheckAndQueueUpgrades();
            NetworkServiceLocator.ObjectService.InvokeRPC(this, "RPC_SyncExperience", NetworkTarget.All, currentExperience);

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
            StartCoroutine(OpenMagicUpgradePanelWithDelay());
        }

        IEnumerator OpenMagicUpgradePanelWithDelay()
        {
            yield return new WaitForSeconds(1f);
            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                OpenMagicUpgradePanel();
                yield break;
            }
            NetworkServiceLocator.ObjectService.InvokeRPC(this, "OpenMagicUpgradePanel", NetworkTarget.All);
        }

        [PunRPC]
        public void OpenMagicUpgradePanel()
        {
            EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(true);
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
                    IncreaseLevel(requiredExp);
                }
                else
                {
                    NetworkServiceLocator.ObjectService.InvokeRPC(this, "IncreaseLevel", NetworkTarget.All, requiredExp);
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
            NetworkServiceLocator.ObjectService.InvokeRPC(this, "CloseMagicUpgradePanel", NetworkTarget.All);
        }

        [PunRPC]
        private void CloseMagicUpgradePanel()
        {
            EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(false);
            StartCoroutine(ProcessNextWithDelay());
        }

        IEnumerator ProcessNextWithDelay()
        {
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
            ProcessNextLevelUp();
        }

        /// <summary>
        /// RPC：同步经验值到所有客户端（仅更新数值，UI 由事件通道处理）
        /// </summary>
        [PunRPC]
        private void RPC_SyncExperience(int syncedExp)
        {
            currentExperience = syncedExp;
            RaiseExperienceUpdate();
        }

        [PunRPC]
        private void IncreaseLevel(int requiredExp)
        {
            currentExperience -= requiredExp;
            currentLevel++;
            upgradeExperience = (int)(upgradeExperience * 1.5);

            // 通过事件通道通知 UI 更新
            RaiseExperienceUpdate();
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