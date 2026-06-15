using System.Collections;
using System.Collections.Generic;
using Enemies;
using Events;
using Manager.TimeSystem;
using Photon.Pun;
using UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Manager
{
    public class GamePlayingManager : MonoBehaviourPunCallbacks
    {
        #region 单例模式
        public static GamePlayingManager instance;

        //测试用
        private Button testCardEffect;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                // 如果已有实例，销毁当前重复实例
                Destroy(gameObject);
            }
        }
        #endregion
        // ========== 升级队列系统 ==========
        // 存储待处理的升级等级
        private Queue<int> pendingLevelUps = new Queue<int>();

        // 是否正在处理升级（有面板打开）
        private bool isProcessingLevelUp = false;

        [SerializeField]
        private Slider experienceSlider;

        [SerializeField]
        private Text levelText; //等级

        //当前经验值
        private int currentExperience;

        //当前等级
        private int currentLevel;

        //升级需要的经验值
        private int upgradeExperience;

        // 平滑过渡相关
        private Coroutine smoothSliderCoroutine;

        [SerializeField]
        private float smoothSpeed = 5f; // 过渡速度，可在Inspector中调整

        void Start() { }

        void OnEnable()
        {
            if (GameManager.isTest)
            {
                Initlize();
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
            EventManager.instance?.RegisterEventComplex(
                EventNames.TimeEventTriggered,
                OnTimeEventReceived
            );
            // 2. 监听游戏结束事件（时间走完了）
            EventManager.instance?.RegisterEvent(EventNames.GameTimeFinished, OnGameFinished);
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventManager.instance?.UnRegisterEventComplex(
                EventNames.TimeEventTriggered,
                OnTimeEventReceived
            );
            // 2. 取消监听游戏结束事件（时间走完了）
            EventManager.instance?.UnRegisterEvent(EventNames.GameTimeFinished, OnGameFinished);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex > 1)
            {
                Initlize();
            }
        }

        private void Initlize()
        {
            if (levelText == null)
            {
                levelText = GameObject.Find("LevelText").GetComponent<Text>();
            }

            if (experienceSlider == null)
            {
                experienceSlider = GameObject.Find("ExperienceSlider").GetComponent<Slider>();
            }
            experienceSlider.maxValue = 1;
            experienceSlider.value = 0;
            levelText.text = "0";
            upgradeExperience = 100;
        }

        #region  经验条相关

        public int GetUpgradeExperience()
        {
            return upgradeExperience;
        }

        // 增加当前经验值
        public void AddExperience(int experience)
        {
            currentExperience += experience;

            // 检查升级并生成队列
            CheckAndQueueUpgrades();
            // 启动平滑过渡
            photonView.RPC("UpdateSliderSmooth", RpcTarget.All, currentExperience);
            // 如果有待处理的升级且当前没有面板打开，开始处理队列
            if (pendingLevelUps.Count > 0 && !isProcessingLevelUp)
            {
                StartLevelUpSequence();
            }
        }

        // 开始处理升级队列
        private void StartLevelUpSequence()
        {
            if (!GameManager.isTest)
            {
                //其他玩家并不处理升级
                if (!PhotonNetwork.IsMasterClient)
                {
                    return;
                }
            }

            isProcessingLevelUp = true;
            // 处理第一个升级
            ProcessNextLevelUp();
        }

        // 处理队列中的下一个升级
        private void ProcessNextLevelUp()
        {
            // 队列为空，所有升级处理完毕
            if (pendingLevelUps.Count == 0)
            {
                CompleteAllLevelUps();
                return;
            }

            // 取出下一个待处理的升级等级
            int levelForThisChoice = pendingLevelUps.Dequeue();
            // 打开卡片选择面板
            StartCoroutine(OpenMagicUpgradePanelWithDelay());
        }

        IEnumerator OpenMagicUpgradePanelWithDelay()
        {
            // 等待一小段时间让过渡更流畅
            yield return new WaitForSeconds(1f);
            //所有人打开面板
            if (GameManager.isTest)
            {
                OpenMagicUpgradePanel();
                yield break;
            }
            photonView.RPC("OpenMagicUpgradePanel", RpcTarget.All);
        }

        //打开卡片选择面板
        [PunRPC]
        public void OpenMagicUpgradePanel()
        {
            MagicUpgradeManager.instance.OpenMagicUpgradePanel();
        }

        //检查并生成升级队列
        private void CheckAndQueueUpgrades()
        {
            if (!GameManager.isTest)
            {
                //保护措施，防止其他玩家增加经验值导致的错误
                if (!PhotonNetwork.IsMasterClient)
                {
                    return;
                }
            }

            while (currentExperience >= upgradeExperience)
            {
                photonView.RPC("IncreaseLevel", RpcTarget.All);

                // 将升级事件加入队列
                pendingLevelUps.Enqueue(currentLevel);
            }

            // 更新等级显示
            levelText.text = currentLevel.ToString();
        }

        // 所有升级处理完毕
        private void CompleteAllLevelUps()
        {
            isProcessingLevelUp = false;
            Debug.Log("所有升级处理完毕");

            // 在这里可以添加升级完成后的逻辑
            // 比如通知其他系统、恢复游戏等
        }

        // 玩家完成卡片选择后调用此方法
        // 此方法应该由你的卡片选择面板在玩家确认选择后调用
        public void OnPlayerUpgradeChoiceConfirmed()
        {
            //通知所有人关闭选择面板
            photonView.RPC("CloseMagicUpgradePanel", RpcTarget.All);
        }

        //关闭卡片选择面板
        [PunRPC]
        private void CloseMagicUpgradePanel()
        {
            // 关闭卡片选择面板
            MagicUpgradeManager.instance.CloseMagicUpgradePanel();
            // 短暂延迟后处理下一个升级（让过渡更流畅）
            StartCoroutine(ProcessNextWithDelay());
        }

        // 延迟处理下一个升级
        IEnumerator ProcessNextWithDelay()
        {
            //其他玩家不处理
            if (!PhotonNetwork.IsMasterClient)
            {
                yield break;
            }
            // 等待一小段时间让面板关闭动画播放
            yield return new WaitForSeconds(0.5f);

            // 处理队列中的下一个升级
            ProcessNextLevelUp();
        }

        // 平滑更新Slider
        [PunRPC]
        private void UpdateSliderSmooth(int curExp)
        {
            float targetValue = (float)curExp / upgradeExperience;

            // 如果已有协程在运行，先停止
            if (smoothSliderCoroutine != null)
            {
                StopCoroutine(smoothSliderCoroutine);
            }

            // 启动新的平滑过渡
            smoothSliderCoroutine = StartCoroutine(SmoothSlider(targetValue));
        }

        // 平滑过渡协程
        private IEnumerator SmoothSlider(float targetValue)
        {
            float startValue = experienceSlider.value;
            float elapsedTime = 0f;
            float duration = 1f; // 过渡持续时间

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime * smoothSpeed;
                experienceSlider.value = Mathf.Lerp(startValue, targetValue, elapsedTime);
                yield return null;
            }

            // 确保最终值精确
            experienceSlider.value = targetValue;
        }

        // 可选：直接设置经验值（用于初始化等场景，不需要平滑过渡）
        public void SetExperience(int experience)
        {
            currentExperience = experience;
            experienceSlider.value = (float)currentExperience / upgradeExperience;
            // 检查是否升级
            CheckAndQueueUpgrades();
            // 如果有待处理升级且没有面板打开，启动队列
            if (pendingLevelUps.Count > 0 && !isProcessingLevelUp)
            {
                StartLevelUpSequence();
            }
        }

        //增加等级
        [PunRPC]
        private void IncreaseLevel()
        {
            // 扣除经验，提升等级
            currentExperience -= upgradeExperience;
            currentLevel++;
            levelText.text = currentLevel.ToString();
            // 更新升级所需经验
            upgradeExperience = (int)(upgradeExperience * 1.5);
        }

        // 获取当前等级（外部查询用）
        public int GetCurrentLevel()
        {
            return currentLevel;
        }

        // 获取当前经验值（外部查询用）
        public int GetCurrentExperience()
        {
            return currentExperience;
        }
        #endregion

        #region 时间管理相关

        void OnTimeEventTriggered(TimeEventData eventData)
        {
            Debug.Log($"事件触发: {eventData.eventName} at {eventData.triggerTime}秒");

            switch (eventData.eventId)
            {
                case "KillSacrifice":
                    KillSacrifice(eventData);
                    break;
                case "EscortRobot":
                    EscortRobot(eventData);
                    break;
            }
        }

        /// <summary>
        /// 灵魂献祭任务
        /// </summary>
        /// <param name="eventData"></param>
        private void KillSacrifice(TimeEventData eventData)
        {
            Debug.Log($"任务激活: {eventData.eventName}" + eventData.description);
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            Vector3 randomPosition = GetRandomPlayerPosition();

            TaskManager.instance.SetNotice(
                eventData.eventName,
                eventData.description,
                eventData.limittime
            );

            KillTask killTask = new KillTask(
                TaskConst.KillSacrifice,
                randomPosition,
                7f,
                10,
                eventData.limittime
            );

            //激活任务
            TaskManager.instance.ActivateTask(killTask);
        }

        /// <summary>
        /// 机器人护送
        /// </summary>
        /// <param name="eventData"></param>
        private void EscortRobot(TimeEventData eventData)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            Debug.Log($"任务激活: {eventData.eventName}" + eventData.description);

            Vector3 randomPosition = GetRandomPlayerPosition();

            TaskManager.instance.SetNotice(
                eventData.eventName,
                eventData.description,
                eventData.limittime
            );

            EscortTask escortTask = new EscortTask(
                eventData.eventId,
                randomPosition,
                3,
                6,
                eventData.limittime
            );

            TaskManager.instance.ActivateTask(escortTask);
        }

        /// <summary>
        /// 获取随机一位玩家附近位置
        /// </summary>
        private Vector3 GetRandomPlayerPosition()
        {
            //获取一个随机玩家的位置
            IReadOnlyList<GameObject> players =
                PlayerManager.instance != null ? PlayerManager.instance.ActivePlayerObjects : null;
            if (players == null || players.Count == 0)
            {
                Debug.LogError("没有玩家在线，无法激活任务");
            }

            // 随机选择一个玩家
            GameObject randomPlayer = players[Random.Range(0, players.Count)];
            // 在玩家位置250-300范围内随机生成任务
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0; // 去掉y轴分量，只在xz平面生成方向
            randomDirection.Normalize(); // 归一化为单位向量

            float randomDistance = Random.Range(20f, 30f);
            Vector3 randomPosition =
                randomPlayer.transform.position + randomDirection * randomDistance;

            // 只取xz平面，固定y轴高度
            randomPosition.y = 0.5f;
            return randomPosition;
        }

        void OnTimeEventReceived(EventArgsBase args)
        {
            var timeArgs = args as TimeEventArgs;
            if (timeArgs != null)
            {
                Debug.Log($"收到事件: {timeArgs.eventData.eventName}");
                // 执行游戏逻辑
                OnTimeEventTriggered(timeArgs.eventData);
            }
        }

        void OnGameFinished()
        {
            Debug.Log("游戏时间结束！");
            // 显示结算界面等
        }

        void OnTimeUpdated(float currentTime)
        {
            // 每秒更新一次显示
        }
        #endregion
    }
}
