using System.Collections;
using System.Collections.Generic;
using Enemies;
using Photon.Pun;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Manager
{
    public class GamePlayingManager : MonoBehaviourPunCallbacks
    {
        #region 单例模式
        public static GamePlayingManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion
        // ========== 升级队列系统 ==========
        // 存储待处理的升级等级
        private Queue<int> pendingLevelUps = new Queue<int>();
        // 是否正在处理升级（有面板打开）
        private bool isProcessingLevelUp = false;

        [SerializeField] private Slider experienceSlider;
        [SerializeField] private Text levelText; //等级

        //当前经验值
        private int currentExperience;
        //当前等级
        private int currentLevel;
        //升级需要的经验值
        private int upgradeExperience;
        // 平滑过渡相关
        private Coroutine smoothSliderCoroutine;
        [SerializeField] private float smoothSpeed = 5f; // 过渡速度，可在Inspector中调整

        void Start()
        {
            experienceSlider.maxValue = 1;
            experienceSlider.value = 0;
            levelText.text = "0";
            upgradeExperience = 100;
        }

        // 增加当前经验值
        public void AddExperience(int experience)
        {

            currentExperience += experience;
            
            // 检查升级并生成队列
            CheckAndQueueUpgrades();
            // 启动平滑过渡
            UpdateSliderSmooth();
            // 如果有待处理的升级且当前没有面板打开，开始处理队列
            if (pendingLevelUps.Count > 0 && !isProcessingLevelUp)
            {
                StartLevelUpSequence();
            }
        }
        
        
        // 开始处理升级队列
        private void StartLevelUpSequence()
        {
            //其他玩家并不处理升级
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
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
            //保护措施，防止其他玩家增加经验值导致的错误
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            while (currentExperience >= upgradeExperience)
            {
                // 扣除经验，提升等级
                currentExperience -= upgradeExperience;
                currentLevel++;

                // 更新升级所需经验
                upgradeExperience = (int)(upgradeExperience * 1.5);

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
        private void UpdateSliderSmooth()
        {
            float targetValue = (float)currentExperience / upgradeExperience;

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
    }
}