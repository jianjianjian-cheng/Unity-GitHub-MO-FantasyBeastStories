using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class MagicUpgradeManager : MonoBehaviour
    {
        #region 单例模式
        public static MagicUpgradeManager instance;

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
        GameObject GrossUpgradePanel;
        List<ParticleSystem> OneCardEffects;
        List<ParticleSystem> TwoCardEffects;
        List<ParticleSystem> ThreeCardEffects;

        void Start()
        {
            OneCardEffects = new List<ParticleSystem>();
            TwoCardEffects = new List<ParticleSystem>();
            ThreeCardEffects = new List<ParticleSystem>();
            Initialize();
        }

        private void Initialize()
        {
            GrossUpgradePanel = transform.Find("GrossUpgradePanel").gameObject;
            if (GrossUpgradePanel == null)
            {
                Debug.LogError("GrossUpgradePanel 未找到");
                return;
            }
        }
    }
}
