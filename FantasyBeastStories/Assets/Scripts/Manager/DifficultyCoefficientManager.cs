using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class DifficultyCoefficientManager : MonoBehaviour
    {
        #region 单例模式
            public static DifficultyCoefficientManager instance;


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

        [Header("难度系数设置")]
        private float difficultyCoefficient = 1f;

        void OnEnable()
        {
            int playerCount = 1;
            if (!GameManager.isTest)
            {
                playerCount = PlayerManager.instance.PlayerCount;
            }
            switch (playerCount)
            {
                case 1:
                    difficultyCoefficient = 0.7f; // 单人游戏，难度系数为0.7
                    break;
                case 2:
                    difficultyCoefficient = 1f; // 双人游戏，难度系数为1
                    break;
                case 3:
                    difficultyCoefficient = 1.25f; // 三人游戏，难度系数增加50%
                    break;
                case 4:
                    difficultyCoefficient = 1.5f; // 四人游戏，难度系数翻倍
                    break;
                default:
                    difficultyCoefficient = 1f; // 默认情况，难度系数为1
                    break;
            }

            EventManager.instance.RegisterSingleFloatEvent(EventNames.TimeChangeEnemyAttribute , UpdateDifficultyCoefficient);
        }

        void OnDisable()
        {
            EventManager.instance.UnRegisterSingleFloatEvent(EventNames.TimeChangeEnemyAttribute);
        }


        public float GetDifficultyCoefficient()
        {
            return difficultyCoefficient;
        }


        /// <summary>
        /// 根据游戏进行的时间更新难度系数
        /// </summary>
        /// <param name="currentTime"></param>
        public void UpdateDifficultyCoefficient(float currentTime)
        {
            float totalTime = SyncedGameTimeManager.Instance.GetTotalGameTime();
            float progress = Mathf.Clamp01(currentTime / totalTime);
            difficultyCoefficient = 1.0f + progress * 0.5f; 
        }
    }
}
