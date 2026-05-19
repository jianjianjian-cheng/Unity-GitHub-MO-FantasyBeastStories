using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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


        //背景阴影
        private Image shadowImage;


        //卡片动画相关位置
        private GameObject AnimPoints;
        private Transform StartPoint;
        private Transform[] EndPoints;
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
            #region 寻找特效
            //寻找卡片一位置的特效
            GameObject oneCard = MagicUpgradePanel.transform.Find("OneCardEffect").gameObject;
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
            GameObject twoCard = MagicUpgradePanel.transform.Find("TwoCardEffect").gameObject;
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
            GameObject threeCard = MagicUpgradePanel.transform.Find("ThreeCardEffect").gameObject;
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
            #region 其他元素
            //寻找背景阴影
            shadowImage = GrossUpgradePanel.transform.Find("Shadow").GetComponent<Image>();
            if (shadowImage == null)
            {
                Debug.LogError("ShadowImage 未找到");
                return;
            }

            //寻找卡片动画相关位置
            AnimPoints = GrossUpgradePanel.transform.Find("AnimPoints").gameObject;
            if (AnimPoints == null)
            {
                Debug.LogError("AnimPoints 未找到");
                return;
            }
            EndPoints = AnimPoints.GetComponentsInChildren<Transform>()
            .Where
            (x => x.name != "EndPoint").ToArray();
            if (EndPoints.Length < 3)
            {
                Debug.LogError("EndPoints 数量不足");
                return;
            }
            StartPoint = AnimPoints.transform.Find("StartPoint").transform;
            if (StartPoint == null)
            {
                Debug.LogError("StartPoint 未找到");
                return;
            }
            #endregion
        }

        #region 动画处理
        private void OpenMagicUpgradePanelAnim()
        {
            StartCoroutine(AnimateShadowAlpha());
        }

        //移动卡片到指定位置
        IEnumerator MoveCardEndToPosition()
        {
            Sequence seq = DOTween.Sequence();
            float moveTime = 0.3f;
            // ===== 第1步：发第1张牌 =====
            seq.Append(Card_1.transform.DOMove(EndPoints[0].position, moveTime));


            // ===== 第2步：发第2张牌 =====
            seq.Append(Card_2.transform.DOMove(EndPoints[1].position, moveTime));


            // ===== 第3步：发第3张牌 =====
            seq.Append(Card_3.transform.DOMove(EndPoints[2].position, moveTime));


            yield return seq.WaitForCompletion();
            OnCardMoveToEndPositionComplete();
        }

        //卡片移动到end位置回调
        private void OnCardMoveToEndPositionComplete()
        {
            Debug.Log("卡片移动到end位置");
        }

        //阴影背景1秒内透明度变化，协程
        private IEnumerator AnimateShadowAlpha()
        {
            // 阴影背景1秒内透明度变化，协程
            float alpha = 0;
            while (alpha < 0.7f)
            {
                alpha += Time.deltaTime / 1;
                shadowImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            StartCoroutine(MoveCardEndToPosition());
        }
        //阴影背景1秒内透明度变化，协程
        private IEnumerator AnimateShadowAlphaBack()
        {
            // 阴影背景1秒内透明度变化，协程
            float alpha = 0.7f;
            while (alpha > 0)
            {
                alpha -= Time.deltaTime / 1;
                shadowImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
        }
        #endregion

        //打开魔法升级面板
        public void OpenMagicUpgradePanel()
        {
            Debug.Log("打开魔法升级面板");
            GrossUpgradePanel.SetActive(true);
            OpenMagicUpgradePanelAnim();
        }

        //关闭魔法升级面板
        public void CloseMagicUpgradePanel()
        {
            Debug.Log("关闭魔法升级面板");
            StartCoroutine(AnimateShadowAlphaBack());
            GrossUpgradePanel.SetActive(false);
        }
    }
}
