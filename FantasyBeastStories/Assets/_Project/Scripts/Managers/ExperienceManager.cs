using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Channels.Game;
using Core.Channels.General;
using Core.Channels.Player;
using Controllers.Item;
using Core;
using Controllers.Services;
using NetworkTarget = Controllers.Network.NetworkTarget;
using Controllers.Network;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
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

        // ========== 经验球非网络化（方案二） ==========
        /// <summary>下一个可用的 ballId 自增计数器（仅房主使用）</summary>
        private uint nextBallId = 1;

        /// <summary>已认领的球 ID 集合，防止重复计数（仅房主使用）</summary>
        private HashSet<uint> claimedBalls = new HashSet<uint>();

        /// <summary>当前活跃的本地经验球映射 ballId → GameObject（所有客户端使用）</summary>
        private Dictionary<uint, GameObject> activeExpBalls = new Dictionary<uint, GameObject>();

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

            // 清理经验球状态
            activeExpBalls.Clear();
            claimedBalls.Clear();
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
            upgradeExperience = 150;

            // 重置经验球去重状态
            nextBallId = 1;
            claimedBalls.Clear();

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
            QuestTaskManager.Instance?.RecordExp();

            CheckAndQueueUpgrades();
            NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "RPC_SyncExperience", NetworkTarget.All, currentExperience);

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
            NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "OpenMagicUpgradePanel", NetworkTarget.All);
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
                    NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "IncreaseLevel", NetworkTarget.All, requiredExp);
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
            NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "CloseMagicUpgradePanel", NetworkTarget.All);
        }

        /// <summary>
        /// 由 ManagerRpcBridge 在收到 RPC 后调用
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
            NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "OpenExMagicUpgradePanel", NetworkTarget.All);
        }

        /// <summary>
        /// 由 ManagerRpcBridge 在收到 RPC 后调用：同步经验值到所有客户端
        /// </summary>
        public static void HandleSyncExperienceRPC(int syncedExp)
        {
            if (Instance == null) return;
            Instance.currentExperience = syncedExp;
            Instance.RaiseExperienceUpdate();
        }

        /// <summary>
        /// 由 ManagerRpcBridge 在收到 RPC 后调用：增加等级
        /// </summary>
        public static void HandleIncreaseLevelRPC(int requiredExp)
        {
            if (Instance == null) return;
            Instance.currentExperience -= requiredExp;
            Instance.currentLevel++;
            Instance.upgradeExperience = (int)(Instance.upgradeExperience * 1.5);

            Instance.RaiseExperienceUpdate();
        }

        // ============================================================
        // 经验球非网络化 RPC 处理（方案二）
        // ============================================================

        /// <summary>
        /// 由 ManagerRpcBridge.RPC_SpawnExpBall 调用
        /// 每个客户端在本地生成一个经验球（非网络对象）
        /// </summary>
        public static void HandleSpawnExpBallRPC(uint ballId, Vector3 position, int expValue)
        {
            if (Instance == null) return;
            Instance.SpawnLocalExpBall(ballId, position, expValue);
        }

        /// <summary>
        /// 由 ManagerRpcBridge.RPC_ClaimExpBall 调用（仅房主执行）
        /// 处理经验球认领：去重检查 → 加经验 → 广播隐藏
        /// </summary>
        public static void HandleClaimExpBallRPC(uint ballId, int expValue)
        {
            if (Instance == null) return;
            Instance.ClaimLocalExpBall(ballId, expValue);
        }

        /// <summary>
        /// 由 ManagerRpcBridge.RPC_ExpBallCollected 调用
        /// 所有客户端隐藏对应的本地经验球
        /// </summary>
        public static void HandleExpBallCollectedRPC(uint ballId)
        {
            if (Instance == null) return;
            Instance.HideLocalExpBall(ballId);
        }

        // ── 实例方法 ──

        /// <summary>
        /// 生成一个全局唯一的 ballId（仅房主调用）
        /// </summary>
        public uint GenerateBallId()
        {
            return nextBallId++;
        }

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
            activeExpBalls[ballId] = ballObj;
        }

        private void ClaimLocalExpBall(uint ballId, int expValue)
        {
            // 去重检查：已认领过则忽略
            if (!claimedBalls.Add(ballId))
            {
                Debug.Log($"[ExperienceManager] 球 {ballId} 已被认领，忽略重复请求");
                return;
            }

            // 加经验（AddExperience 内部会检查 IsMasterClient）
            AddExperience(expValue);

            // 广播隐藏此球到所有客户端
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "RPC_ExpBallCollected",
                NetworkTarget.All, (int)ballId);
        }

        private void HideLocalExpBall(uint ballId)
        {
            if (!activeExpBalls.TryGetValue(ballId, out var ballObj))
            {
                // 球可能已被拾取者本地回收，属正常情况
                return;
            }

            activeExpBalls.Remove(ballId);

            var poolManager = ServiceLocator.Get<ObjectPoolManager>();
            if (poolManager != null)
            {
                poolManager.ReturnToPool(PoolConst.ExperienceBall_Blue_Local, ballObj);
            }
            else
            {
                Destroy(ballObj);
            }
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