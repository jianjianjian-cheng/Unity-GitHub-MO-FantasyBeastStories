using System.Collections;
using System.Collections.Generic;
using TMPro;
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
        #region 全局变量
        GameObject GrossUpgradePanel;
        List<ParticleSystem> OneCardEffects;
        List<ParticleSystem> TwoCardEffects;
        List<ParticleSystem> ThreeCardEffects;

        //魔法升级面板，以及面板下卡片的文字变量
        GameObject MagicUpgradePanel;
        GameObject Card_1;
        TextMeshProUGUI NameText_1;
        TextMeshProUGUI ContentText_1;
        TextMeshProUGUI QualityText_1;
        GameObject Card_2;
        TextMeshProUGUI NameText_2;
        TextMeshProUGUI ContentText_2;
        TextMeshProUGUI QualityText_2;
        GameObject Card_3;
        TextMeshProUGUI NameText_3;
        TextMeshProUGUI ContentText_3;
        TextMeshProUGUI QualityText_3;
        #endregion
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
            #region 寻找特效
            //寻找卡片一位置的特效
            GameObject oneCard = GrossUpgradePanel.transform.Find("OneCardEffect").gameObject;
            if (oneCard == null)
            {
                Debug.LogError("OneCardEffect 未找到");
                return;
            }
            else
            {
                //寻找该该卡片位置下的所有特效
                foreach (Transform child in oneCard.transform)
                {
                    if (child.GetComponent<ParticleSystem>() != null)
                    {
                        OneCardEffects.Add(child.GetComponent<ParticleSystem>());
                    }
                }
            }

            //寻找卡片二位置的特效
            GameObject twoCard = GrossUpgradePanel.transform.Find("TwoCardEffect").gameObject;
            if (twoCard == null)
            {
                Debug.LogError("TwoCardEffect 未找到");
                return;
            }
            else
            {
                //寻找该该卡片位置下的所有特效
                foreach (Transform child in twoCard.transform)
                {
                    if (child.GetComponent<ParticleSystem>() != null)
                    {
                        TwoCardEffects.Add(child.GetComponent<ParticleSystem>());
                    }
                }
            }

            //寻找卡片三位置的特效
            GameObject threeCard = GrossUpgradePanel.transform.Find("ThreeCardEffect").gameObject;
            if (threeCard == null)
            {
                Debug.LogError("ThreeCardEffect 未找到");
                return;
            }
            else
            {
                //寻找该该卡片位置下的所有特效
                foreach (Transform child in threeCard.transform)
                {
                    if (child.GetComponent<ParticleSystem>() != null)
                    {
                        ThreeCardEffects.Add(child.GetComponent<ParticleSystem>());
                    }
                }
            }
            #endregion

            #region 寻找卡片
            //寻找魔法升级面板
            MagicUpgradePanel = GrossUpgradePanel.transform.Find("MagicUpgradePanel").gameObject;
            if (MagicUpgradePanel == null)
            {
                Debug.LogError("MagicUpgradePanel 未找到");
                return;
            }

            //寻找第一卡片
            Card_1 = MagicUpgradePanel.transform.Find("Card_1").gameObject;
            if (Card_1 == null)
            {
                Debug.LogError("Card_1 未找到");
                return;
            }
            else
            {
                //寻找该卡片卡片位置下的所有文字
                NameText_1 = Card_1.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                if (NameText_1 == null)
                {
                    Debug.LogError("NameText_1 未找到");
                    return;
                }
                ContentText_1 = Card_1.transform.Find("ContentText").GetComponent<TextMeshProUGUI>();
                if (ContentText_1 == null)
                {
                    Debug.LogError("ContentText_1 未找到");
                    return;
                }
                QualityText_1 = Card_1.transform.Find("QualityText").GetComponent<TextMeshProUGUI>();
                if (QualityText_1 == null)
                {
                    Debug.LogError("QualityText_1 未找到");
                    return;
                }
            }

            //寻找第二卡片
            Card_2 = MagicUpgradePanel.transform.Find("Card_2").gameObject;

            if (Card_2 == null)
            {
                Debug.LogError("Card_2 未找到");
                return;
            }
            else
            {
                //寻找该卡片卡片位置下的所有文字
                NameText_2 = Card_2.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                if (NameText_2 == null)
                {
                    Debug.LogError("NameText_2 未找到");
                    return;
                }
                ContentText_2 = Card_2.transform.Find("ContentText").GetComponent<TextMeshProUGUI>();
                if (ContentText_2 == null)
                {
                    Debug.LogError("ContentText_2 未找到");
                    return;
                }
                QualityText_2 = Card_2.transform.Find("QualityText").GetComponent<TextMeshProUGUI>();
                if (QualityText_2 == null)
                {
                    Debug.LogError("QualityText_2 未找到");
                    return;
                }
            }



            //寻找第三卡片
            Card_3 = MagicUpgradePanel.transform.Find("Card_3").gameObject;
            if (Card_3 == null)
            {
                Debug.LogError("Card_3 未找到");
                return;
            }
            else
            {
                //寻找该卡片卡片位置下的所有文字
                NameText_3 = Card_3.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                if (NameText_3 == null)
                {
                    Debug.LogError("NameText_3 未找到");
                    return;
                }
                ContentText_3 = Card_3.transform.Find("ContentText").GetComponent<TextMeshProUGUI>();
                if (ContentText_3 == null)
                {
                    Debug.LogError("ContentText_3 未找到");
                    return;
                }
                QualityText_3 = Card_3.transform.Find("QualityText").GetComponent<TextMeshProUGUI>();
                if (QualityText_3 == null)
                {
                    Debug.LogError("QualityText_3 未找到");
                    return;
                }
            }
            #endregion
        }

        //打开魔法升级面板
        public void OpenMagicUpgradePanel()
        {
            Debug.Log("打开魔法升级面板");
            GrossUpgradePanel.SetActive(true);
        }

        //关闭魔法升级面板
        public void CloseMagicUpgradePanel()
        {
            Debug.Log("关闭魔法升级面板");
            GrossUpgradePanel.SetActive(false);
        }
    }
}
